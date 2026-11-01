using System.Text;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Finds a number, Call-ID or session in saved captures without opening them into the call view.
/// A .txt is read when there is one; otherwise the .pcap. Newest captures are searched first.
/// </summary>
public static partial class CaptureSearch
{
    public const int MaxHits = 80;

    public sealed record Hit(string Stem, DateTime Started, string Sbc, string From, string To, string CallId, string Session);

    [GeneratedRegex(@"\bSID[=:\s]+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex SidInPacket();

    public static List<Hit> Find(string folder, string query, out bool limited)
    {
        limited = false;
        var hits = new List<Hit>();
        string q = query.Trim();
        if (q.Length < 2 || !Directory.Exists(folder)) return hits;

        foreach (string stem in Stems(folder))
        {
            foreach (Hit hit in Read(folder, stem).OrderBy(h => h.Started))
            {
                if (!Contains(hit, q)) continue;
                hits.Add(hit);
                if (hits.Count >= MaxHits) { limited = true; return hits; }
            }
        }
        return hits;
    }

    private static bool Contains(Hit hit, string query) =>
        hit.From.Contains(query, StringComparison.OrdinalIgnoreCase)
        || hit.To.Contains(query, StringComparison.OrdinalIgnoreCase)
        || hit.CallId.Contains(query, StringComparison.OrdinalIgnoreCase)
        || hit.Session.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static List<string> Stems(string folder)
    {
        var stems = new List<(string stem, DateTime started)>();
        foreach (FileInfo file in new DirectoryInfo(folder).EnumerateFiles("mediant-*.*"))
        {
            if (!file.Extension.Equals(".pcap", StringComparison.OrdinalIgnoreCase) && !file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;
            string stem = Path.GetFileNameWithoutExtension(file.Name);
            if (!CaptureFiles.TryStarted(stem, out DateTime started)) continue;
            if (stems.Any(s => s.stem.Equals(stem, StringComparison.OrdinalIgnoreCase))) continue;
            stems.Add((stem, started));
        }
        return stems.OrderByDescending(s => s.started).Select(s => s.stem).ToList();
    }

    private static List<Hit> Read(string folder, string stem)
    {
        string txt = Path.Combine(folder, stem + ".txt");
        string pcap = Path.Combine(folder, stem + ".pcap");
        var calls = new Dictionary<string, Draft>(StringComparer.Ordinal);
        try
        {
            if (File.Exists(txt)) ReadText(txt, calls);
            if (File.Exists(pcap)) ReadPcap(pcap, calls, create: !File.Exists(txt));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (InvalidOperationException) { }
        if (!CaptureFiles.TryStarted(stem, out DateTime started)) started = DateTime.MinValue;
        return calls.Values.Select(c => new Hit(stem, c.Started == default ? started : c.Started, c.Sbc, c.From, c.To, c.CallId, c.Session)).ToList();
    }

    private static void ReadText(string path, Dictionary<string, Draft> calls)
    {
        CaptureReplay.ReadText(path, sip =>
        {
            var message = SipText.Parse(sip.Text);
            if (message.CallId.Length == 0) return;
            Note(calls, sip.Sbc, message, sip.SessionId, sip.Utc);
        }, _ => { });
    }

    /// <param name="create">When a .txt was already read, the pcap only supplies the session id the binary header carries.</param>
    private static void ReadPcap(string path, Dictionary<string, Draft> calls, bool create)
    {
        foreach (CaptureReplay.Datagram packet in CaptureReplay.Pcap(path))
        {
            if (!create && calls.Count > 0 && calls.Values.All(c => c.Session.Length > 0)) break;
            SipText.Message? message = SipText.Find(packet.Payload);
            if (message is null || message.CallId.Length == 0) continue;
            string session = SessionOf(packet.Payload);
            if (create) Note(calls, packet.Source.Address.ToString(), message, session, packet.Utc);
            else if (session.Length > 0)
            {
                foreach (Draft call in calls.Values)
                    if (call.CallId == message.CallId && call.Session.Length == 0) call.Session = session;
            }
        }
    }

    private static string SessionOf(byte[] payload)
    {
        Acdr.Packet? header = Acdr.Parse(payload);
        if (header is { SessionId.Length: > 0 } && header.SessionId != "0:0:0") return header.SessionId;
        Match sid = SidInPacket().Match(Encoding.Latin1.GetString(payload));
        return sid.Success ? sid.Groups[1].Value.TrimEnd(']', ')') : "";
    }

    private static void Note(Dictionary<string, Draft> calls, string sbc, SipText.Message message, string session, DateTime at)
    {
        string key = sbc + "|" + message.CallId;
        if (!calls.TryGetValue(key, out Draft? call))
            calls[key] = call = new Draft { Sbc = sbc, CallId = message.CallId, Started = at };
        if (call.From.Length == 0) call.From = UserPart(message.From);
        if (call.To.Length == 0) call.To = UserPart(message.To);
        if (call.Session.Length == 0 && session.Length > 0) call.Session = session.Trim();
    }

    private static string UserPart(string header)
    {
        int sip = header.IndexOf("sip:", StringComparison.OrdinalIgnoreCase);
        if (sip < 0) sip = header.IndexOf("tel:", StringComparison.OrdinalIgnoreCase);
        if (sip < 0) return header;
        string rest = header[(sip + 4)..];
        int end = rest.IndexOfAny(['@', '>', ';']);
        return end > 0 ? rest[..end] : rest;
    }

    private sealed class Draft
    {
        public string Sbc = "";
        public string From = "";
        public string To = "";
        public string CallId = "";
        public string Session = "";
        public DateTime Started;
    }
}
