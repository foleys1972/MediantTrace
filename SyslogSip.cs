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
///
/// The same banners announce HTTP, DNS and VoiceAI messages the SBC exchanges for a
/// session ("---- Outgoing DNS Message to ..."); those are kept too, as non-SIP
/// messages for the ladder. IP phones log SIP after "recv &lt;--" / "send --&gt;"
/// (or "{acl_TransportMsgBuilderPrintMsg():N} &lt;--") instead of a banner.
/// </summary>
public sealed partial class SyslogSip
{
    [GeneratedRegex(@"-{3,}\s*(Incoming|Outgoing) SIP Message (?:from|to) ([^\s]+)[^\n]*?-{3,}", RegexOptions.IgnoreCase)]
    private static partial Regex Banner();

    [GeneratedRegex(@"-{3,}\s*(Incoming|Outgoing) (HTTP|DNS|VoiceAI) Message(?: (?:from|to) ([^\s]+))?[^\n]*?(?:-{3,}|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex OtherBanner();

    [GeneratedRegex(@"\{acl_TransportMsgBuilderPrintMsg\(\):\d+\}\s*(<--|-->)\s*|\b(?:pstn )?(recv|send)\s*(<--|-->)\s*(?:ARM\s*)?(\d{1,3}(?:\.\d{1,3}){3}(?::\d+)?)?\s*")]
    private static partial Regex PhoneBanner();

    [GeneratedRegex(@"\(N\s*\d+\)|\(\s*lgr_\w+\)")]
    private static partial Regex RecordTag();

    [GeneratedRegex(@"\b(UDP|TCP|TLS|SCTP)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BannerTransport();

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

    /// <summary>A message rebuilt from syslog. <see cref="Kind"/> is SIP, HTTP, DNS or VoiceAI; only SIP messages have a parsed <see cref="Message"/> with a Call-ID.</summary>
    public sealed record Assembled(SipText.Message Message, bool? Inbound, string Peer, DateTime Time, string SessionId, string Kind = "SIP");

    private string? _text;
    /// <summary>True while the next syslog lines are the rest of a message.</summary>
    public bool Assembling => _text is not null;
    private bool? _inbound;
    private string _peer = "";
    private string _sid = "";
    private string _transport = "";
    private string _kind = "SIP";
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
        // A DNS or VoiceAI dump has no length: it ends where the SBC's next log record starts.
        if (_text is not null && _kind is not ("SIP" or "HTTP") && RecordTag().IsMatch(line)) Flush(done);
        string clean = TimeTag().Replace(StripPrefix(Priority().Replace(line, "")), "");

        Match banner = Banner().Match(clean);
        Match other = banner.Success ? Match.Empty : OtherBanner().Match(clean);
        Match phone = banner.Success || other.Success ? Match.Empty : PhoneBanner().Match(clean);
        if (banner.Success)
        {
            Flush(done);
            _kind = "SIP";
            _inbound = banner.Groups[1].Value.Equals("Incoming", StringComparison.OrdinalIgnoreCase);
            _peer = banner.Groups[2].Value;
            _sid = sid;
            _transport = BannerTransport().Match(banner.Value) is { Success: true } hop ? hop.Groups[1].Value.ToUpperInvariant() : "";
            _started = now;
            _text = clean[(banner.Index + banner.Length)..].TrimStart('\r', '\n', ' ');
        }
        else if (other.Success)
        {
            Flush(done);
            string kind = other.Groups[2].Value.ToUpperInvariant();
            _kind = kind == "VOICEAI" ? "VoiceAI" : kind;
            _inbound = other.Groups[1].Value.Equals("Incoming", StringComparison.OrdinalIgnoreCase);
            _peer = other.Groups[3].Success ? other.Groups[3].Value : "";
            _sid = sid;
            _transport = "";
            _started = now;
            _text = clean[(other.Index + other.Length)..].TrimStart('\r', '\n', ' ');
        }
        else if (phone.Success && SipText.Find(clean[(phone.Index + phone.Length)..]) is not null || phone.Success && clean[(phone.Index + phone.Length)..].Trim().Length == 0)
        {
            Flush(done);
            _kind = "SIP";
            string arrow = phone.Groups[1].Success ? phone.Groups[1].Value : phone.Groups[3].Value;
            _inbound = phone.Groups[2].Success ? phone.Groups[2].Value == "recv" : arrow == "<--";
            _peer = phone.Groups[4].Success ? phone.Groups[4].Value : "";
            _sid = sid;
            _transport = "";
            _started = now;
            _text = clean[(phone.Index + phone.Length)..].TrimStart('\r', '\n', ' ');
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
            _kind = "SIP";
            _inbound = null;
            _peer = "";
            _transport = "";
            _sid = sid;
            _started = now;
            _text = clean;
        }
        else
        {
            return done;
        }

        if (_text is not null && _kind is "SIP" or "HTTP" && Complete(_text)) Flush(done);
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
        if (_kind != "SIP")
        {
            string body = _text.Trim();
            if (body.Length > 0)
            {
                string first = body.Split('\n', 2)[0].Trim();
                if (first.Length > 120) first = first[..120];
                var note = new SipText.Message { Text = body, FirstLine = $"{_kind} {first}".Trim() };
                done.Add(new Assembled(note, _inbound, _peer, _started, _sid, _kind));
            }
            _text = null;
            _transport = "";
            _kind = "SIP";
            return;
        }
        SipText.Message? m = SipText.Find(_text);
        if (m is not null)
        {
            if (_transport == "TLS") m.Transport = "TLS";
            else if (m.Transport.Length == 0) m.Transport = _transport;
            done.Add(new Assembled(m, _inbound, _peer, _started, _sid));
        }
        _text = null;
        _transport = "";
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
