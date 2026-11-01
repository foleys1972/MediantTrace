using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace MediantTrace;

/// <summary>
/// Reads syslog saved by other tools into lines MediantTrace can show, so a log someone sends
/// can be opened like a capture. Formats, as AudioCodes Syslog Viewer opens them:
///   - Syslog Viewer's standard log ("12:28:52.794  10.1.1.1  local0.notice  [S=..] ..."), with or without a date;
///   - acSyslog and raw syslog ("&lt;157&gt;2023-03-17T13:25:58.962Z 10.1.1.1 [S=..] ...", RFC 3164 or 5424);
///   - MediantTrace's own plain log ("2026-10-14 08:27:01.154  10.1.1.1  local0.notice  ...");
///   - CSV and TSV with a header row (Splunk, Graylog, Kiwi, Azure exports), and SL4NT's CSV without one;
///   - Excel (.xlsx), first sheet, header row;
///   - JSON: Azure Log Analytics (v1 tables and v2 frames), Elastic search hits, an array of objects, or one object per line;
///   - the legacy web format from the device's own page ("... [Time:14-11@12:28:52.794]").
/// A line that does not start a new record continues the one before (SIP messages span lines).
/// </summary>
public static partial class SyslogImport
{
    public readonly record struct Line(DateTime Utc, string Source, string Text);

    public sealed class Result
    {
        public string Format = "";
        public List<Line> Lines = [];
        public int Skipped;
    }

    private const int MaxLines = 2_000_000;

    // ------------------------------------------------------------------ patterns

    [GeneratedRegex(@"^<(\d{1,3})>(?:1 )?\s*(.*)$", RegexOptions.Singleline)]
    private static partial Regex PriPrefix();

    // 2023-03-17T13:25:58.962Z, 2023-03-17 13:25:58.962+01:00, 2023-03-17 13:25:58
    [GeneratedRegex(@"^(\d{4}-\d{1,2}-\d{1,2})[T ](\d{1,2}:\d{2}:\d{2})([.,]\d{1,7})?(Z|[+-]\d{2}:?\d{2})?\s+")]
    private static partial Regex IsoStart();

    // Oct 14 08:27:01 (RFC 3164)
    [GeneratedRegex(@"^([A-Z][a-z]{2})\s+(\d{1,2})\s+(\d{2}:\d{2}:\d{2})([.,]\d{1,6})?\s+")]
    private static partial Regex BsdStart();

    // Syslog Viewer: "14/11 12:28:52.794  10.1.1.1  local0.notice  msg" (date optional)
    [GeneratedRegex(@"^(?:(\d{1,2})/(\d{1,2}) )?(\d{1,2}:\d{2}:\d{2})([.,]\d{1,6})?\s+(\d{1,3}(?:\.\d{1,3}){3}|[0-9a-fA-F:]*:[0-9a-fA-F:]+)\s+")]
    private static partial Regex ViewerStart();

    [GeneratedRegex(@"^([a-z0-9-]+)\.(emerg|alert|crit|critical|err|error|warn|warning|notice|info|debug)\s*:?\s+", RegexOptions.IgnoreCase)]
    private static partial Regex FacilitySeverity();

    [GeneratedRegex(@"^(\d{1,3}(?:\.\d{1,3}){3}|[0-9a-fA-F:]*:[0-9a-fA-F:]+|[A-Za-z][A-Za-z0-9.\-_]*)\s+")]
    private static partial Regex HostToken();

    [GeneratedRegex(@"\[Time:(\d{2})-(\d{2})@(\d{2}:\d{2}:\d{2})(\.\d{3})?\]")]
    private static partial Regex WebTime();

    [GeneratedRegex(@"^\s*\[(?:S|SID|BID)[=:]")]
    private static partial Regex WebStart();

    [GeneratedRegex(@"^\d{1,3}(?:\.\d{1,3}){3}$")]
    private static partial Regex Ipv4();

    private static readonly string[] Facilities = ["kern", "user", "mail", "daemon", "auth", "syslog", "lpr", "news", "uucp", "cron", "authpriv", "ftp", "ntp", "audit", "alert", "clock",
        "local0", "local1", "local2", "local3", "local4", "local5", "local6", "local7"];

    private static int SeverityNumber(string s) => s.ToLowerInvariant() switch
    {
        "emerg" or "emergency" or "panic" => 0,
        "alert" => 1,
        "crit" or "critical" => 2,
        "err" or "error" => 3,
        "warn" or "warning" => 4,
        "notice" => 5,
        "info" or "informational" or "information" => 6,
        "debug" => 7,
        _ => -1,
    };

    private static int FacilityNumber(string f)
    {
        f = f.ToLowerInvariant();
        if (f == "auth-priv") f = "authpriv";
        int i = Array.IndexOf(Facilities, f);
        return i;
    }

    /// <summary>"&lt;PRI&gt;" for a facility and severity, so severity colouring and filters work on imported lines.</summary>
    public static string Pri(string facility, string severity)
    {
        int sev = int.TryParse(severity, out int n) ? n & 7 : SeverityNumber(severity);
        if (sev < 0) return "";
        int fac = int.TryParse(facility, out int fn) ? fn : FacilityNumber(facility);
        if (fac < 0) fac = 16; // local0
        return $"<{fac * 8 + sev}>";
    }

    /// <summary>"local0.notice" for a PRI value, as Syslog Viewer and the plain log write it.</summary>
    public static string FacilityText(int pri)
    {
        if (pri < 0) return "user.notice";
        int fac = pri >> 3, sev = pri & 7;
        string f = fac < Facilities.Length ? Facilities[fac] : "local0";
        string s = sev switch { 0 => "emerg", 1 => "alert", 2 => "crit", 3 => "err", 4 => "warning", 5 => "notice", 6 => "info", _ => "debug" };
        return $"{f}.{s}";
    }

    // ------------------------------------------------------------------ entry

    /// <summary>Reads a saved log. The format is found from the content, not the file name.</summary>
    public static Result Read(string path, string name, DateTime fileDate)
    {
        byte[] head = new byte[8];
        using (var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            _ = probe.Read(head);
        if (head[0] == (byte)'P' && head[1] == (byte)'K')
            return Xlsx(path, fileDate);

        string text = ReadText(path);
        string trimmed = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            Result? json = Json(trimmed, fileDate);
            if (json is not null) return json;
        }
        return Text(text, name, fileDate);
    }

    /// <summary>Text pasted on the page, read the same way as a file.</summary>
    public static Result ReadPasted(string text, DateTime today)
    {
        string trimmed = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            Result? json = Json(trimmed, today);
            if (json is not null) return json;
        }
        return Text(text, "pasted", today);
    }

    private static string ReadText(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    // ------------------------------------------------------------------ line-based text

    private static Result Text(string text, string name, DateTime fileDate)
    {
        string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string first = lines.FirstOrDefault(l => l.Trim().Length > 0) ?? "";

        // CSV / TSV with a header row naming the columns.
        char delimiter = first.Count(c => c == '\t') >= 2 ? '\t' : first.Count(c => c == ',') >= 2 ? ',' : '\0';
        if (delimiter != '\0')
        {
            var rows = Csv(text, delimiter);
            if (rows.Count > 0 && Columns.From(rows[0]) is { } cols)
                return FromRows(rows.Skip(1), cols, delimiter == '\t' ? "TSV" : "CSV", fileDate);
            if (rows.Count > 0 && Sl4nt(rows[0]) is not null && !ViewerStart().IsMatch(first) && !PriPrefix().IsMatch(first))
                return Sl4ntRows(rows, delimiter, fileDate);
        }

        var result = new Result();
        var formats = new Dictionary<string, int>();
        DateTime lastTime = fileDate.ToUniversalTime();
        string? pendingSource = null;
        DateTime pendingTime = default;
        var pending = new StringBuilder();
        bool havePending = false;

        void Flush()
        {
            if (!havePending) return;
            string body = pending.ToString().TrimEnd();
            if (body.Length > 0 && result.Lines.Count < MaxLines) result.Lines.Add(new Line(pendingTime, pendingSource ?? "", body));
            pending.Clear();
            havePending = false;
        }

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd();
            if (line.Length == 0) { if (havePending) pending.Append('\n'); continue; }
            if (TryRecord(line, fileDate, lastTime, out DateTime when, out string source, out string body, out string format))
            {
                Flush();
                formats[format] = formats.GetValueOrDefault(format) + 1;
                pendingTime = when;
                lastTime = when;
                pendingSource = source;
                pending.Append(body);
                havePending = true;
                continue;
            }
            if (!havePending)
            {
                // Nothing to continue: a line on its own, with no time or sender.
                pendingTime = lastTime;
                pendingSource = "";
                pending.Append(line.Trim());
                havePending = true;
                formats["text"] = formats.GetValueOrDefault("text") + 1;
                continue;
            }
            // Syslog Viewer indents the lines that carry on a message; keep them as message lines.
            pending.Append('\n').Append(line.StartsWith("  ", StringComparison.Ordinal) ? line.TrimStart() : line);
        }
        Flush();
        result.Format = formats.Count == 0 ? "empty" : FormatName(formats.MaxBy(f => f.Value).Key);
        return result;
    }

    private static string FormatName(string key) => key switch
    {
        "viewer" => "Syslog Viewer log",
        "pri" => "acSyslog / raw syslog",
        "iso" => "Timestamped syslog log",
        "bsd" => "BSD syslog log",
        "web" => "Legacy web format",
        _ => "Plain text",
    };

    /// <summary>One line that starts a record, in any of the line formats. The text keeps a &lt;PRI&gt; prefix when the severity is known.</summary>
    internal static bool TryRecord(string line, DateTime fileDate, DateTime lastTime, out DateTime when, out string source, out string body, out string format)
    {
        when = lastTime;
        source = "";
        body = "";
        format = "";
        string rest = line;
        string pri = "";

        if (PriPrefix().Match(rest) is { Success: true } p && int.TryParse(p.Groups[1].Value, out int priValue) && priValue <= 191)
        {
            pri = $"<{priValue}>";
            rest = p.Groups[2].Value;
            format = "pri";
        }

        if (IsoStart().Match(rest) is { Success: true } iso)
        {
            when = IsoTime(iso);
            rest = rest[iso.Length..];
            if (format.Length == 0) format = "iso";
        }
        else if (BsdStart().Match(rest) is { Success: true } bsd)
        {
            if (DateTime.TryParseExact($"{bsd.Groups[1].Value} {bsd.Groups[2].Value.PadLeft(2, '0')} {fileDate.Year} {bsd.Groups[3].Value}", "MMM dd yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime t))
                when = t.AddTicks(Fraction(bsd.Groups[4].Value)).ToUniversalTime();
            rest = rest[bsd.Length..];
            if (format.Length == 0) format = "bsd";
        }
        else if (ViewerStart().Match(rest) is { Success: true } v)
        {
            DateTime day = fileDate.Date;
            if (v.Groups[1].Success && int.TryParse(v.Groups[1].Value, out int d) && int.TryParse(v.Groups[2].Value, out int mo) && mo is >= 1 and <= 12 && d is >= 1 and <= 31)
            {
                try { day = new DateTime(fileDate.Year, mo, d); } catch (ArgumentOutOfRangeException) { }
            }
            if (TimeSpan.TryParse(v.Groups[3].Value, CultureInfo.InvariantCulture, out TimeSpan ts))
                when = DateTime.SpecifyKind(day + ts, DateTimeKind.Local).AddTicks(Fraction(v.Groups[4].Value)).ToUniversalTime();
            source = v.Groups[5].Value;
            rest = rest[v.Length..];
            if (format.Length == 0) format = "viewer";
        }
        else if (format.Length == 0)
        {
            // The device's own web page: records start with [S=..] and end with [Time:dd-MM@hh:mm:ss.fff].
            if (WebStart().IsMatch(rest))
            {
                if (WebTime().Match(rest) is { Success: true } wt) when = WebWhen(wt, fileDate);
                body = rest.Trim();
                format = "web";
                return true;
            }
            return false;
        }

        // After the time: the sender and "facility.severity", either way round. "-" is a sender left blank.
        if (rest.StartsWith("- ", StringComparison.Ordinal)) rest = rest[2..].TrimStart();
        if (source.Length == 0 && HostToken().Match(rest) is { Success: true } h && !FacilitySeverity().IsMatch(rest) && LooksLikeHost(h.Groups[1].Value, rest[h.Length..]))
        {
            source = h.Groups[1].Value;
            rest = rest[h.Length..];
        }
        if (FacilitySeverity().Match(rest) is { Success: true } fs)
        {
            if (pri.Length == 0) pri = Pri(fs.Groups[1].Value, fs.Groups[2].Value);
            rest = rest[fs.Length..];
            if (source.Length == 0 && HostToken().Match(rest) is { Success: true } h2 && Ipv4().IsMatch(h2.Groups[1].Value))
            {
                source = h2.Groups[1].Value;
                rest = rest[h2.Length..];
            }
        }
        if (format == "pri" && WebTime().Match(rest) is { Success: true } tag && when == lastTime) when = WebWhen(tag, fileDate);
        body = pri + rest;
        return true;
    }

    // A host name only when what follows looks like a message, so a message's first word is not taken for the sender.
    private static bool LooksLikeHost(string token, string after) =>
        Ipv4().IsMatch(token) || token.Contains(':') && token.Count(c => c == ':') >= 2
        || after.StartsWith('[') || after.StartsWith('(') || FacilitySeverity().IsMatch(after) || token.Contains('.') && token.Any(char.IsLetter) && token.Length > 3;

    private static DateTime WebWhen(Match m, DateTime fileDate)
    {
        int day = int.Parse(m.Groups[1].Value), month = int.Parse(m.Groups[2].Value);
        DateTime date = fileDate.Date;
        try { date = new DateTime(fileDate.Year, month, day); } catch (ArgumentOutOfRangeException) { }
        TimeSpan.TryParse(m.Groups[3].Value, CultureInfo.InvariantCulture, out TimeSpan ts);
        return DateTime.SpecifyKind(date + ts, DateTimeKind.Local).AddTicks(Fraction(m.Groups[4].Value)).ToUniversalTime();
    }

    private static long Fraction(string f)
    {
        if (f.Length < 2) return 0;
        string digits = f[1..].PadRight(7, '0')[..7];
        return long.TryParse(digits, out long t) ? t : 0;
    }

    private static DateTime IsoTime(Match m)
    {
        string date = m.Groups[1].Value, time = m.Groups[2].Value, zone = m.Groups[4].Value;
        DateTime.TryParseExact($"{date} {time}", ["yyyy-M-d H:mm:ss", "yyyy-MM-dd HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t);
        t = t.AddTicks(Fraction(m.Groups[3].Value));
        if (zone == "Z") return DateTime.SpecifyKind(t, DateTimeKind.Utc);
        if (zone.Length > 0)
        {
            string z = zone.Replace(":", "");
            int sign = z[0] == '-' ? -1 : 1;
            int hours = int.Parse(z.Substring(1, 2)), minutes = int.Parse(z.Substring(3, 2));
            return DateTime.SpecifyKind(t.AddMinutes(-sign * (hours * 60 + minutes)), DateTimeKind.Utc);
        }
        return DateTime.SpecifyKind(t, DateTimeKind.Local).ToUniversalTime();
    }

    /// <summary>A time from a column: ISO, "10/14/2026, 8:27:01.154 AM" (Azure), "14/10/2026 08:27:01", Excel serial days, Unix seconds or milliseconds.</summary>
    internal static DateTime? ParseTime(string value, DateTime fileDate)
    {
        string v = value.Trim().Trim('"');
        if (v.Length == 0) return null;
        if (IsoStart().Match(v + " ") is { Success: true } iso) return IsoTime(iso);
        if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        {
            if (number is > 20000 and < 80000) return DateTime.SpecifyKind(DateTime.FromOADate(number), DateTimeKind.Local).ToUniversalTime();
            if (number is > 1e9 and < 1e10) return DateTime.UnixEpoch.AddSeconds(number);
            if (number is > 1e12 and < 1e13) return DateTime.UnixEpoch.AddMilliseconds(number);
        }
        string[] usual =
        [
            "M/d/yyyy, h:mm:ss.fff tt", "M/d/yyyy, h:mm:ss tt", "M/d/yyyy h:mm:ss.fff tt", "M/d/yyyy h:mm:ss tt",
            "d/M/yyyy HH:mm:ss.fff", "d/M/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "d.M.yyyy HH:mm:ss.fff", "d.M.yyyy HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss.fff", "yyyy/MM/dd HH:mm:ss", "MMM d HH:mm:ss", "MMM dd HH:mm:ss", "MMM d yyyy HH:mm:ss", "dd-MMM-yyyy HH:mm:ss",
        ];
        if (DateTime.TryParseExact(v, usual, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out DateTime t))
            return t.ToUniversalTime();
        if (TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out TimeSpan only) && only < TimeSpan.FromDays(1))
            return DateTime.SpecifyKind(fileDate.Date + only, DateTimeKind.Local).ToUniversalTime();
        if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out t)) return t.ToUniversalTime();
        return null;
    }

    // ------------------------------------------------------------------ tables (CSV, TSV, Excel, JSON)

    /// <summary>Which column holds what, from the header names Syslog Viewer accepts (and a few more).</summary>
    private sealed class Columns
    {
        public int Time = -1, Date = -1, Host = -1, Message = -1, Facility = -1, Severity = -1, Priority = -1, Device = -1;

        private static readonly string[] TimeNames = ["timestamp", "time", "_time", "timegenerated", "time generated", "eventtime", "event time", "messagetime", "message time", "@timestamp", "datetime", "date time", "date/time", "receivedtime", "time received", "eventreceivedtime", "logtime", "generatedtime"];
        private static readonly string[] DateNames = ["date"];
        private static readonly string[] HostNames = ["hostname", "host", "ip", "hostip", "host ip", "source", "sourceip", "source ip", "src", "computer", "fromhost", "fromhost-ip", "sender", "device ip", "deviceip", "address", "remote address"];
        private static readonly string[] MessageNames = ["syslogmessage", "syslog message", "message", "msg", "text", "txt", "raw", "_raw", "log", "event", "line"];
        private static readonly string[] FacilityNames = ["facility", "_facility", "syslog_facility", "_syslog_facility", "sc4s_syslog_facility", "syslogfacility"];
        private static readonly string[] SeverityNames = ["severity", "severitylevel", "severity level", "level", "syslogseverity", "syslog_severity", "_severity"];
        private static readonly string[] PriorityNames = ["priority", "facility.severity", "pri", "syslogpriority"];
        private static readonly string[] DeviceNames = ["devicename", "device name", "device"];

        public static Columns? From(IReadOnlyList<string> header)
        {
            var c = new Columns();
            for (int i = 0; i < header.Count; i++)
            {
                string h = header[i].Trim().Trim('"').ToLowerInvariant();
                if (c.Message < 0 && MessageNames.Contains(h)) c.Message = i;
                else if (c.Time < 0 && TimeNames.Contains(h)) c.Time = i;
                else if (c.Date < 0 && DateNames.Contains(h)) c.Date = i;
                else if (c.Host < 0 && HostNames.Contains(h)) c.Host = i;
                else if (c.Priority < 0 && PriorityNames.Contains(h)) c.Priority = i;
                else if (c.Facility < 0 && FacilityNames.Contains(h)) c.Facility = i;
                else if (c.Severity < 0 && SeverityNames.Contains(h)) c.Severity = i;
                else if (c.Device < 0 && DeviceNames.Contains(h)) c.Device = i;
            }
            if (c.Time < 0 && c.Date >= 0) { c.Time = c.Date; c.Date = -1; }
            return c.Message >= 0 ? c : null;
        }

        public static Columns? FromNames(IEnumerable<string> names) => From(names.ToList());
    }

    private static Result FromRows(IEnumerable<IReadOnlyList<string>> rows, Columns c, string format, DateTime fileDate)
    {
        var result = new Result { Format = format };
        DateTime last = fileDate.ToUniversalTime();
        foreach (var row in rows)
        {
            string Get(int i) => i >= 0 && i < row.Count ? row[i] : "";
            string message = Get(c.Message);
            if (message.Trim().Length == 0) { result.Skipped++; continue; }
            string timeText = c.Date >= 0 ? $"{Get(c.Date)} {Get(c.Time)}" : Get(c.Time);
            DateTime when = ParseTime(timeText, fileDate) ?? last;
            last = when;
            string host = Get(c.Host).Trim();
            if (host.Length == 0) host = Get(c.Device).Trim();
            result.Lines.Add(new Line(when, host, WithPri(message, Get(c.Facility), Get(c.Severity), Get(c.Priority))));
            if (result.Lines.Count >= MaxLines) break;
        }
        return result;
    }

    /// <summary>The message with a &lt;PRI&gt; prefix built from the facility/severity/priority columns, unless it has one already.</summary>
    private static string WithPri(string message, string facility, string severity, string priority)
    {
        message = message.TrimEnd();
        if (PriPrefix().IsMatch(message)) return message;
        string pri = "";
        priority = priority.Trim();
        if (priority.Length > 0)
        {
            if (int.TryParse(priority, out int pv) && pv is >= 0 and <= 191) pri = $"<{pv}>";
            else if (priority.Split('.') is [var f, var s]) pri = Pri(f, s);
            else pri = Pri(facility.Length > 0 ? facility : "local0", priority);
        }
        if (pri.Length == 0 && severity.Trim().Length > 0) pri = Pri(facility.Trim().Length > 0 ? facility.Trim() : "local0", severity.Trim());
        return pri + message;
    }

    /// <summary>SL4NT and Kiwi write CSV without a header: date, time, facility, severity, host, message (message not escaped).</summary>
    private static int[]? Sl4nt(IReadOnlyList<string> row)
    {
        // Find "facility" then "severity" next to each other, and an address after them.
        for (int i = 0; i + 2 < row.Count; i++)
        {
            string f = row[i].Trim().Trim('"'), s = row[i + 1].Trim().Trim('"');
            if (FacilityNumber(f) >= 0 && SeverityNumber(s) >= 0)
            {
                for (int h = i + 2; h < row.Count - 1 && h <= i + 3; h++)
                    if (Ipv4().IsMatch(row[h].Trim().Trim('"')) || HostToken().IsMatch(row[h].Trim().Trim('"') + " "))
                        return [i, i + 1, h];
            }
        }
        return null;
    }

    private static Result Sl4ntRows(List<IReadOnlyList<string>> rows, char delimiter, DateTime fileDate)
    {
        var result = new Result { Format = "SL4NT / headerless CSV" };
        DateTime last = fileDate.ToUniversalTime();
        foreach (var row in rows)
        {
            int[]? at = Sl4nt(row);
            if (at is null)
            {
                // Not a record: an unescaped message that ran onto the next line.
                if (result.Lines.Count > 0)
                {
                    var prev = result.Lines[^1];
                    result.Lines[^1] = prev with { Text = prev.Text + "\n" + string.Join(delimiter, row) };
                }
                else result.Skipped++;
                continue;
            }
            int fi = at[0], si = at[1], hi = at[2];
            string timeText = string.Join(" ", row.Take(fi).Select(x => x.Trim().Trim('"')));
            DateTime when = ParseTime(timeText, fileDate) ?? last;
            last = when;
            string message = string.Join(delimiter, row.Skip(hi + 1)).Trim();
            if (message.Length >= 2 && message[0] == '"' && message[^1] == '"') message = message[1..^1].Replace("\"\"", "\"");
            result.Lines.Add(new Line(when, row[hi].Trim().Trim('"'), Pri(row[fi].Trim().Trim('"'), row[si].Trim().Trim('"')) + message));
            if (result.Lines.Count >= MaxLines) break;
        }
        return result;
    }

    /// <summary>CSV with quoted fields that may hold the delimiter, doubled quotes and line breaks.</summary>
    internal static List<IReadOnlyList<string>> Csv(string text, char delimiter)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool quoted = false, fieldStarted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else quoted = false;
                }
                else field.Append(ch);
                continue;
            }
            if (ch == '"' && !fieldStarted) { quoted = true; fieldStarted = true; continue; }
            if (ch == delimiter) { row.Add(field.ToString()); field.Clear(); fieldStarted = false; continue; }
            if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear(); fieldStarted = false;
                if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
                row = [];
                if (rows.Count > MaxLines) break;
                continue;
            }
            field.Append(ch);
            fieldStarted = true;
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
        return rows;
    }

    // ------------------------------------------------------------------ Excel

    private static Result Xlsx(string path, DateTime fileDate)
    {
        using ZipArchive zip = ZipFile.OpenRead(path);
        var shared = new List<string>();
        if (zip.GetEntry("xl/sharedStrings.xml") is { } ss)
        {
            using var r = XmlReader.Create(ss.Open(), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var sb = new StringBuilder();
            bool inSi = false;
            r.Read();
            // ReadElementContentAsString moves to the next node itself, so only Read() when nothing else moved on.
            while (!r.EOF)
            {
                if (r.NodeType == XmlNodeType.Element && r.LocalName == "t" && inSi) { sb.Append(r.ReadElementContentAsString()); continue; }
                if (r.NodeType == XmlNodeType.Element && r.LocalName == "si") { sb.Clear(); if (r.IsEmptyElement) shared.Add(""); else inSi = true; }
                else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "si") { inSi = false; shared.Add(sb.ToString()); }
                r.Read();
            }
        }
        ZipArchiveEntry sheet = zip.GetEntry("xl/worksheets/sheet1.xml")
            ?? zip.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("That Excel file has no worksheet.");
        var rows = new List<IReadOnlyList<string>>();
        using (var r = XmlReader.Create(sheet.Open(), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            List<string>? row = null;
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element && r.LocalName == "row") row = [];
                else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "row" && row is not null) { rows.Add(row); row = null; if (rows.Count > MaxLines) break; }
                else if (r.NodeType == XmlNodeType.Element && r.LocalName == "c" && row is not null)
                {
                    string type = r.GetAttribute("t") ?? "";
                    int col = ColumnIndex(r.GetAttribute("r") ?? "");
                    string value = "";
                    if (!r.IsEmptyElement)
                    {
                        using XmlReader cell = r.ReadSubtree();
                        var sb = new StringBuilder();
                        cell.Read();
                        while (!cell.EOF)
                        {
                            if (cell.NodeType == XmlNodeType.Element && cell.LocalName is "v" or "t") { sb.Append(cell.ReadElementContentAsString()); continue; }
                            cell.Read();
                        }
                        value = sb.ToString();
                    }
                    if (type == "s" && int.TryParse(value, out int si) && si < shared.Count) value = shared[si];
                    if (col < 0) col = row.Count;
                    while (row.Count < col) row.Add("");
                    if (row.Count == col) row.Add(value); else row[col] = value;
                }
            }
        }
        if (rows.Count == 0) return new Result { Format = "Excel" };
        Columns cols = Columns.From(rows[0]) ?? throw new InvalidOperationException("The first row of the sheet must name the columns, with one called Message (or Msg, Text, SyslogMessage).");
        return FromRows(rows.Skip(1), cols, "Excel", fileDate);
    }

    private static int ColumnIndex(string reference)
    {
        int n = 0, i = 0;
        for (; i < reference.Length && char.IsAsciiLetterUpper(reference[i]); i++) n = n * 26 + (reference[i] - 'A' + 1);
        return i == 0 ? -1 : n - 1;
    }

    // ------------------------------------------------------------------ JSON

    private static Result? Json(string text, DateTime fileDate)
    {
        var objects = new List<JsonElement>();
        JsonDocument? doc = null;
        try { doc = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }); }
        catch (JsonException)
        {
            // One object per line (NDJSON, as Graylog, Splunk and Elastic export).
            foreach (string line in text.Split('\n'))
            {
                string l = line.Trim();
                if (l.Length == 0) continue;
                try { objects.Add(JsonDocument.Parse(l).RootElement.Clone()); }
                catch (JsonException) { return null; }
            }
            return FromObjects(objects, "JSON lines", fileDate);
        }
        using (doc)
        {
            JsonElement root = doc.RootElement;
            // Azure Log Analytics v2: an array of frames, the data in "DataTable" frames with Columns and Rows.
            if (root.ValueKind == JsonValueKind.Array && root.EnumerateArray().Any(f => f.ValueKind == JsonValueKind.Object && f.TryGetProperty("FrameType", out _)))
            {
                var result = new Result { Format = "Azure Log Analytics (JSON)" };
                foreach (JsonElement frame in root.EnumerateArray())
                    if (frame.TryGetProperty("FrameType", out var ft) && ft.GetString() == "DataTable" && (!frame.TryGetProperty("TableKind", out var tk) || tk.GetString() == "PrimaryResult"))
                        Table(frame, "Columns", "ColumnName", "Rows", result, fileDate);
                return result;
            }
            // Azure Log Analytics v1: { "tables": [ { "columns": [ { "name" } ], "rows": [ [ ... ] ] } ] }
            if (root.ValueKind == JsonValueKind.Object && (root.TryGetProperty("tables", out var tables) || root.TryGetProperty("Tables", out tables)) && tables.ValueKind == JsonValueKind.Array)
            {
                var result = new Result { Format = "Azure Log Analytics (JSON)" };
                foreach (JsonElement t in tables.EnumerateArray())
                {
                    if (t.TryGetProperty("columns", out _)) Table(t, "columns", "name", "rows", result, fileDate);
                    else Table(t, "Columns", "ColumnName", "Rows", result, fileDate);
                    if (result.Lines.Count > 0) break;
                }
                return result;
            }
            // Elastic: { "hits": { "hits": [ { "_source": { ... } } ] } }
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("hits", out var hits) && hits.ValueKind == JsonValueKind.Object && hits.TryGetProperty("hits", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement h in list.EnumerateArray())
                    objects.Add(h.TryGetProperty("_source", out var src) ? src.Clone() : h.Clone());
                return FromObjects(objects, "Elastic search (JSON)", fileDate);
            }
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement e in root.EnumerateArray()) if (e.ValueKind == JsonValueKind.Object) objects.Add(e.Clone());
                return FromObjects(objects, "JSON", fileDate);
            }
            if (root.ValueKind == JsonValueKind.Object)
            {
                // A wrapper with one array of records ("results", "data", "events", ...).
                foreach (JsonProperty p in root.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.Array && p.Value.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.Object))
                    {
                        foreach (JsonElement e in p.Value.EnumerateArray()) if (e.ValueKind == JsonValueKind.Object) objects.Add(e.Clone());
                        return FromObjects(objects, "JSON", fileDate);
                    }
                objects.Add(root.Clone());
                return FromObjects(objects, "JSON", fileDate);
            }
        }
        return null;
    }

    private static void Table(JsonElement table, string columnsName, string columnName, string rowsName, Result result, DateTime fileDate)
    {
        if (!table.TryGetProperty(columnsName, out var columns) || !table.TryGetProperty(rowsName, out var rows)) return;
        var names = columns.EnumerateArray().Select(c => c.TryGetProperty(columnName, out var n) ? n.GetString() ?? "" : "").ToList();
        Columns? cols = Columns.FromNames(names);
        if (cols is null) return;
        var data = rows.EnumerateArray().Select(r => (IReadOnlyList<string>)r.EnumerateArray().Select(Scalar).ToList());
        var part = FromRows(data, cols, result.Format, fileDate);
        result.Lines.AddRange(part.Lines);
        result.Skipped += part.Skipped;
    }

    private static Result FromObjects(List<JsonElement> objects, string format, DateTime fileDate)
    {
        var result = new Result { Format = format };
        if (objects.Count == 0) return result;
        // Flatten each object one level ("syslog": { "facility": .. } becomes "facility").
        static Dictionary<string, string> Flat(JsonElement o)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty p in o.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty q in p.Value.EnumerateObject())
                        if (q.Value.ValueKind != JsonValueKind.Object && q.Value.ValueKind != JsonValueKind.Array)
                        {
                            d.TryAdd(q.Name, Scalar(q.Value));
                            d.TryAdd($"{p.Name}.{q.Name}", Scalar(q.Value));
                        }
                }
                else if (p.Value.ValueKind != JsonValueKind.Array) d[p.Name] = Scalar(p.Value);
            }
            return d;
        }
        var flats = objects.Select(Flat).ToList();
        var names = flats.SelectMany(f => f.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Columns? cols = Columns.FromNames(names);
        if (cols is null) throw new InvalidOperationException("No field in that JSON holds the message (expected one called message, msg, text or SyslogMessage).");
        var rows = flats.Select(f => (IReadOnlyList<string>)names.Select(n => f.GetValueOrDefault(n) ?? "").ToList());
        var part = FromRows(rows, cols, format, fileDate);
        result.Lines = part.Lines;
        result.Skipped = part.Skipped;
        return result;
    }

    private static string Scalar(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? "",
        JsonValueKind.Number => e.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        _ => e.GetRawText(),
    };
}
