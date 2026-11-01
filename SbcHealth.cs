using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Live health of each SBC for the SBC health page: version and licence, uptime,
/// load, clock, alarms (active and history), proxy sets with their keepalive
/// counts, E1 trunks and call statistics. All read with show commands in one SSH
/// session per SBC, at most once a minute and only while someone has the page open.
/// </summary>
public sealed partial class SbcHealth
{
    private readonly Settings _settings;
    private readonly ChangeManager _changes;
    private readonly Dictionary<string, Entry> _entries = new();

    private static readonly string[] Commands =
    [
        "show version", "show system uptime", "show system utilization", "show ntp",
        "show active-alarms", "show alarms-history", "show voip proxy sets status",
        "show voip interface e1-t1", "show voip calls statistics gw", "show voip calls statistics sbc",
    ];

    private sealed class Entry
    {
        public DateTime At;
        public Report? Report;
        public string Error = "";
        public bool Busy;
    }

    public sealed record ProxyAddress(string Address, long Success, long Failed);
    public sealed record ProxySet(int Id, string Mode, bool KeepAlive, string Status, List<ProxyAddress> Addresses);

    public sealed class Report
    {
        public string Board { get; set; } = "";
        public string Version { get; set; } = "";
        public string Serial { get; set; } = "";
        public string SbcSessions { get; set; } = "";
        public string DspChannels { get; set; } = "";
        public double? UptimeHours { get; set; }
        public int? Cpu { get; set; }
        public int? Memory { get; set; }
        public bool? NtpSynced { get; set; }
        public string NtpServer { get; set; } = "";
        public List<ReadyCheck.Alarm> Alarms { get; set; } = [];
        public List<ReadyCheck.Alarm> AlarmHistory { get; set; } = [];
        public List<ProxySet> ProxySets { get; set; } = [];
        public List<ReadyCheck.Trunk> Trunks { get; set; } = [];
        public Dictionary<string, string> Gateway { get; set; } = [];
        public Dictionary<string, string> Sbc { get; set; } = [];
        public string State { get; set; } = "ok";        // ok, warn, bad: the worst finding
        public List<string> Findings { get; set; } = []; // why, worst first
        public string SbcTime { get; set; } = "";        // the SBC's own clock (HH:mm:ss) when read
        public int ClockOffsetMinutes { get; set; }       // SBC clock minus this PC's, to the nearest 15 min (time zones)
    }

    public sealed record Read(string Ip, string Name, DateTime At, Report Report);

    /// <summary>The last good report of each SBC (for the timeline). Starts a read where one is due.</summary>
    public List<Read> Reports()
    {
        Snapshot();
        lock (_entries)
            return _settings.Sbcs.Where(s => _entries.TryGetValue(s.Ip, out var e) && e.Report is not null)
                .Select(s => new Read(s.Ip, s.Name, _entries[s.Ip].At, _entries[s.Ip].Report!)).ToList();
    }

    /// <summary>An SBC alarm date ("18.10.2026 21:19:21", SBC local time) as UTC on this PC's clock.</summary>
    public static DateTime? AlarmUtc(string date, int clockOffsetMinutes)
    {
        if (!DateTime.TryParseExact(date, "d.M.yyyy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var t)) return null;
        return DateTime.SpecifyKind(t.AddMinutes(-clockOffsetMinutes), DateTimeKind.Local).ToUniversalTime();
    }

    public SbcHealth(Settings settings, ChangeManager changes)
    {
        _settings = settings;
        _changes = changes;
    }

    /// <summary>Every listed SBC with its last report; starts a read where it is a minute old (or <paramref name="refresh"/>).</summary>
    public object Snapshot(string? refresh = null)
    {
        var list = new List<object>();
        foreach (SbcEntry sbc in _settings.Sbcs.Where(s => s.Ip.Length > 0))
        {
            Entry e;
            lock (_entries) { if (!_entries.TryGetValue(sbc.Ip, out e!)) _entries[sbc.Ip] = e = new Entry(); }
            if (sbc.HasLogin) Kick(sbc, e, refresh == sbc.Ip);
            lock (_entries)
                list.Add(new
                {
                    ip = sbc.Ip, name = sbc.Name, url = sbc.Url, login = sbc.HasLogin,
                    readAt = e.At == default ? (DateTime?)null : e.At, reading = e.Busy, error = e.Error, report = e.Report,
                });
        }
        return list;
    }

    private void Kick(SbcEntry sbc, Entry e, bool now)
    {
        lock (_entries)
        {
            if (e.Busy) return;
            if (!now && DateTime.UtcNow - e.At < TimeSpan.FromSeconds(60)) return;
            if (now && DateTime.UtcNow - e.At < TimeSpan.FromSeconds(5)) return;  // a double click is one read
            e.Busy = true;
        }
        _ = Task.Run(() =>
        {
            try
            {
                var outputs = _changes.Show(sbc.Ip, Commands);
                var report = Parse(c => outputs.FirstOrDefault(o => o.command == c).output ?? "");
                report.ClockOffsetMinutes = ClockOffset(report.SbcTime, DateTime.Now);
                lock (_entries) { e.Report = report; e.Error = ""; }
            }
            catch (Exception ex) { lock (_entries) e.Error = ex.Message; }
            finally { lock (_entries) { e.At = DateTime.UtcNow; e.Busy = false; } }
        });
    }

    [GeneratedRegex(@"^;?\s*([^:;\r\n]+?):\s*(.*?)\s*$", RegexOptions.Multiline)]
    private static partial Regex KeyValue();

    [GeneratedRegex(@"Uptime:\s*(\d+)\s*days?,\s*(\d+)\s*hours?,\s*(\d+)\s*minutes?", RegexOptions.IgnoreCase)]
    private static partial Regex Uptime();

    [GeneratedRegex(@"CPUs utilization:.*?Voice\s+(\d+)%", RegexOptions.IgnoreCase)]
    private static partial Regex CpuVoice();

    [GeneratedRegex(@"Used Memory:.*?Voice\s+(\d+)%", RegexOptions.IgnoreCase)]
    private static partial Regex MemVoice();

    [GeneratedRegex(@"^\s*(\d+)\s+(\S+)\s+(Enabled|Disabled)\b.*?\s(ONLINE|OFFLINE|NOT RESOLVED|NOT AVAILABLE|[A-Z][A-Z ]+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex ProxyRow();

    [GeneratedRegex(@"^\s{6,}(\S+)\s+\S+\s+\S+\s+(\d+)\s+(\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex ProxyAddressRow();

    private static Dictionary<string, string> KeyValues(string text) =>
        KeyValue().Matches(text).GroupBy(m => m.Groups[1].Value.Trim()).ToDictionary(g => g.Key, g => g.First().Groups[2].Value.Trim());

    /// <summary>Turns the command outputs into a report (public for the self-test).</summary>
    public static Report Parse(Func<string, string> output)
    {
        var r = new Report();
        string version = output("show version");
        // The licence block is one line split by ';'
        var kv = KeyValues(version.Replace(";;", "\n").Replace(";", "\n"));
        r.Board = kv.GetValueOrDefault("Board", "");
        r.Version = kv.GetValueOrDefault("Software Version", "");
        r.Serial = kv.GetValueOrDefault("Serial Number", "");
        r.SbcSessions = kv.GetValueOrDefault("Total (Actual)", "");
        r.DspChannels = kv.GetValueOrDefault("Num DSP Channels", "");

        var up = Uptime().Match(output("show system uptime"));
        if (up.Success) r.UptimeHours = int.Parse(up.Groups[1].Value) * 24 + int.Parse(up.Groups[2].Value) + int.Parse(up.Groups[3].Value) / 60.0;
        string util = output("show system utilization");
        r.SbcTime = Regex.Match(util, @"System Time\s+(\d{1,2}:\d{2}:\d{2})") is { Success: true } st ? st.Groups[1].Value : "";
        if (CpuVoice().Match(util) is { Success: true } c) r.Cpu = int.Parse(c.Groups[1].Value);
        if (MemVoice().Match(util) is { Success: true } m) r.Memory = int.Parse(m.Groups[1].Value);

        string ntp = output("show ntp");
        if (ntp.Length > 0) r.NtpSynced = ntp.Contains("NTP is synchronized", StringComparison.OrdinalIgnoreCase);
        r.NtpServer = Regex.Match(ntp, @"NTP server #1 is (\S+)") is { Success: true } n ? n.Groups[1].Value : "";

        r.Alarms = ReadyCheck.ParseAlarms(output("show active-alarms"));
        r.AlarmHistory = ReadyCheck.ParseAlarms(output("show alarms-history")).AsEnumerable().Reverse().Take(30).ToList();
        r.ProxySets = ParseProxySets(output("show voip proxy sets status"));
        r.Trunks = ReadyCheck.ParseTrunks(output("show voip interface e1-t1"));
        r.Gateway = KeyValues(output("show voip calls statistics gw")).Where(x => x.Value.Length > 0).ToDictionary(x => x.Key, x => x.Value);
        r.Sbc = KeyValues(output("show voip calls statistics sbc")).Where(x => x.Value.Length > 0).ToDictionary(x => x.Key, x => x.Value);

        // Findings, worst first.
        var bad = new List<string>();
        var warn = new List<string>();
        foreach (var a in r.Alarms)
            (a.Severity is "critical" or "major" ? bad : warn).Add($"{a.Severity} alarm: {a.Description}");
        foreach (var p in r.ProxySets.Where(p => p.KeepAlive && p.Status != "ONLINE"))
            bad.Add($"Proxy set {p.Id} ({string.Join(", ", p.Addresses.Select(x => x.Address))}) is {p.Status.ToLowerInvariant()}: the SBC's keepalives to it are failing.");
        foreach (var t in r.Trunks.Where(t => !(t.State == "active" && t.DChannel == "established" && t.AlarmsClear)))
            bad.Add($"E1 trunk {t.Port}: state {t.State}, D-channel {t.DChannel}{(t.AlarmsClear ? "" : ", line alarms " + t.Alarms)}.");
        if (r.UptimeHours is double h && h < 24) warn.Add($"Restarted {(h >= 1 ? $"{(int)h} h" : $"{(int)(h * 60)} min")} ago: unsaved changes from before then are gone.");
        if (r.Cpu is >= 85) bad.Add($"CPU {r.Cpu}%."); else if (r.Cpu is >= 70) warn.Add($"CPU {r.Cpu}%.");
        if (r.NtpSynced == false) warn.Add("Clock not in step with a time server.");
        r.Findings = bad.Concat(warn).ToList();
        r.State = bad.Count > 0 ? "bad" : warn.Count > 0 ? "warn" : "ok";
        return r;
    }

    /// <summary>SBC clock minus this PC's, rounded to 15 minutes (a time-zone difference, not drift).</summary>
    public static int ClockOffset(string sbcTime, DateTime pcNow)
    {
        if (!TimeSpan.TryParse(sbcTime, out var sbc)) return 0;
        double diff = (sbc - pcNow.TimeOfDay).TotalMinutes;
        if (diff > 720) diff -= 1440; else if (diff < -720) diff += 1440;   // across midnight
        return (int)(Math.Round(diff / 15) * 15);
    }

    public static List<ProxySet> ParseProxySets(string text)
    {
        var sets = new List<ProxySet>();
        ProxySet? current = null;
        foreach (string line in text.Split('\n'))
        {
            var row = ProxyRow().Match(line);
            if (row.Success)
            {
                current = new ProxySet(int.Parse(row.Groups[1].Value), row.Groups[2].Value, row.Groups[3].Value == "Enabled", row.Groups[4].Value.Trim(), []);
                sets.Add(current);
                continue;
            }
            var addr = ProxyAddressRow().Match(line);
            if (addr.Success && current is not null)
                current.Addresses.Add(new ProxyAddress(addr.Groups[1].Value, long.Parse(addr.Groups[2].Value), long.Parse(addr.Groups[3].Value)));
        }
        return sets;
    }
}
