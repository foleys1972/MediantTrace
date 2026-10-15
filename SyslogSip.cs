using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Rebuilds SIP messages from the Mediant's syslog. With debug level
/// "Detailed" each message is logged after a line such as
///   ---- Incoming SIP Message from 10.0.0.1:5060 to SIPInterface #0 (...) UDP ... ----
///   ---- Outgoing SIP Message to 10.0.0.1:5060 from SIPInterface #0 (...) UDP ... ----
/// and a long message can be split over several syslog packets. Pieces are
/// joined until the message is complete (headers ended and the body is as
/// long as Content-Length says).
/// </summary>
public sealed partial class SyslogSip
{
    [GeneratedRegex(@"-{3,}\s*(Incoming|Outgoing) SIP Message (?:from|to) ([^\s]+)[^\n]*?-{3,}", RegexOptions.IgnoreCase)]
    private static partial Regex Banner();

    [GeneratedRegex(@"^\s*<\d{1,3}>[ \t]*")]
    private static partial Regex Priority();

    [GeneratedRegex(@"\[(?:S|SID|BID|CID|EID)[=:][^\]]*\][ \t]*|\(N\s*\d+\)[ \t]*|\(lgr_[^)]*\)[ \t]*(?:\(\s*\d+\s*\)[ \t]*)?")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\[SID[=:]([^\]]*)\]")]
    private static partial Regex Sid();

    [GeneratedRegex(@"[ \t]*\[Time:[^\]]*\]\s*$")]
    private static partial Regex TimeTag();

    [GeneratedRegex(@"^(?:Content-Length|l)\s*:\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex ContentLength();

    public sealed record Assembled(SipText.Message Message, bool? Inbound, string Peer, DateTime Time, string SessionId);

    private string? _text;
    private bool? _inbound;
    private string _peer = "";
    private string _sid = "";
    private DateTime _started;

    /// <summary>Removes the [S=..] [SID=..] (N ..) tags at the start of a packet; leaves the message alone.</summary>
    private static string StripPrefix(string text)
    {
        int at = 0;
        while (true)
        {
            Match m = Tags().Match(text, at);
            if (!m.Success || m.Index != at) return text[at..];
            at += m.Length;
        }
    }

    /// <summary>The [SID=...] of a syslog packet, or "" when it has none.</summary>
    public static string SessionOf(string line) => Sid().Match(line) is { Success: true } s ? s.Groups[1].Value : "";

    /// <summary>Feed one syslog packet; returns any messages it completed.</summary>
    public List<Assembled> Feed(string line, DateTime now)
    {
        var done = new List<Assembled>();
        string sid = Sid().Match(line) is { Success: true } s ? s.Groups[1].Value : "";
        string clean = TimeTag().Replace(StripPrefix(Priority().Replace(line, "")), "");

        Match banner = Banner().Match(clean);
        if (banner.Success)
        {
            Flush(done);
            _inbound = banner.Groups[1].Value.Equals("Incoming", StringComparison.OrdinalIgnoreCase);
            _peer = banner.Groups[2].Value;
            _sid = sid;
            _started = now;
            _text = clean[(banner.Index + banner.Length)..].TrimStart('\r', '\n', ' ');
        }
        else if (_text is not null && (sid.Length == 0 || sid == _sid))
        {
            // Pieces are cut by size, not at line ends: join them as they are.
            _text += clean;
        }
        else if (SipText.Find(clean) is not null)
        {
            // A SIP message logged without the "---- Incoming/Outgoing" line: direction unknown.
            Flush(done);
            _inbound = null;
            _peer = "";
            _sid = sid;
            _started = now;
            _text = clean;
        }
        else
        {
            return done;
        }

        if (_text is not null && Complete(_text)) Flush(done);
        return done;
    }

    /// <summary>Hand over a message left waiting too long (its last piece never came).</summary>
    public List<Assembled> Expire(DateTime now)
    {
        var done = new List<Assembled>();
        if (_text is not null && now - _started > TimeSpan.FromSeconds(2)) Flush(done);
        return done;
    }

    private void Flush(List<Assembled> done)
    {
        if (_text is null) return;
        SipText.Message? m = SipText.Find(_text);
        if (m is not null) done.Add(new Assembled(m, _inbound, _peer, _started, _sid));
        _text = null;
    }

    private static bool Complete(string text)
    {
        string t = text.Replace("\r\n", "\n");
        int split = t.IndexOf("\n\n", StringComparison.Ordinal);
        if (split < 0) return false;
        int length = ContentLength().Match(t) is { Success: true } m ? int.Parse(m.Groups[1].Value) : 0;
        return t.Length - (split + 2) >= length - 2; // allow for line-ending differences
    }
}
