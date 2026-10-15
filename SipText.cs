using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Finds and reads SIP messages. Debug Recording puts a binary header in
/// front of the SIP text; syslog carries it as plain text. Either way the
/// message starts at its request or status line.
/// </summary>
public static partial class SipText
{
    [GeneratedRegex(@"(?:(?:INVITE|ACK|BYE|CANCEL|OPTIONS|REGISTER|PRACK|UPDATE|INFO|REFER|NOTIFY|SUBSCRIBE|MESSAGE|PUBLISH) \S+ SIP/2\.0|SIP/2\.0 \d{3} )")]
    private static partial Regex StartLine();

    public sealed class Message
    {
        public string Text = "";
        public string FirstLine = "";
        public string? Method;          // request method, or null for a response
        public int Status;              // response code, or 0 for a request
        public string Reason = "";
        public string CallId = "";
        public string CSeq = "";
        public string CSeqMethod = "";
        public string From = "";
        public string To = "";
        public string RequestUri = "";
        public Sdp? Sdp;

        public bool IsRequest => Method is not null;
    }

    public sealed class Sdp
    {
        public string ConnectionIp = "";
        public int AudioPort = -1;
        public List<string> Codecs = [];
        public string Direction = "sendrecv";
        public string Raw = "";
    }

    public static Message? Find(ReadOnlySpan<byte> payload) => Find(Encoding.Latin1.GetString(payload));

    public static Message? Find(string text)
    {
        Match start = StartLine().Match(text);
        if (!start.Success) return null;
        return Parse(text[start.Index..].TrimEnd('\0', '\r', '\n', ' '));
    }

    public static Message Parse(string message)
    {
        var m = new Message { Text = message };
        string normalized = message.Replace("\r\n", "\n");
        int split = normalized.IndexOf("\n\n", StringComparison.Ordinal);
        string head = split >= 0 ? normalized[..split] : normalized;
        string body = split >= 0 ? normalized[(split + 2)..] : "";
        string[] lines = head.Split('\n');
        m.FirstLine = lines[0].Trim();

        if (m.FirstLine.StartsWith("SIP/2.0 ", StringComparison.Ordinal))
        {
            string[] parts = m.FirstLine.Split(' ', 3);
            _ = int.TryParse(parts.ElementAtOrDefault(1), out m.Status);
            m.Reason = parts.ElementAtOrDefault(2) ?? "";
        }
        else
        {
            string[] parts = m.FirstLine.Split(' ');
            m.Method = parts[0];
            m.RequestUri = parts.ElementAtOrDefault(1) ?? "";
        }

        foreach (string raw in lines.Skip(1))
        {
            int colon = raw.IndexOf(':');
            if (colon <= 0) continue;
            string name = raw[..colon].Trim().ToLowerInvariant();
            string value = raw[(colon + 1)..].Trim();
            switch (name)
            {
                case "call-id" or "i" when m.CallId.Length == 0: m.CallId = value; break;
                case "cseq" when m.CSeq.Length == 0:
                    m.CSeq = value;
                    m.CSeqMethod = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
                    break;
                case "from" or "f" when m.From.Length == 0: m.From = value; break;
                case "to" or "t" when m.To.Length == 0: m.To = value; break;
            }
        }

        if (body.Contains("m=audio", StringComparison.Ordinal) || body.StartsWith("v=0", StringComparison.Ordinal))
            m.Sdp = ParseSdp(body);
        return m;
    }

    private static Sdp ParseSdp(string body)
    {
        var sdp = new Sdp { Raw = body.Trim() };
        bool inAudio = false;
        string sessionIp = "", audioIp = "";
        string sessionDirection = "sendrecv", audioDirection = "";
        var rtpmap = new Dictionary<string, string>();
        var formats = new List<string>();

        foreach (string raw in body.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length < 2 || line[1] != '=') continue;
            string value = line[2..];
            switch (line[0])
            {
                case 'm':
                    inAudio = value.StartsWith("audio", StringComparison.Ordinal);
                    if (inAudio)
                    {
                        string[] f = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        _ = int.TryParse(f.ElementAtOrDefault(1), out sdp.AudioPort);
                        formats = f.Skip(3).ToList();
                    }
                    break;
                case 'c':
                    string ip = value.Split(' ').LastOrDefault() ?? "";
                    if (inAudio) audioIp = ip; else sessionIp = ip;
                    break;
                case 'a':
                    if (value is "sendrecv" or "sendonly" or "recvonly" or "inactive")
                    {
                        if (inAudio) audioDirection = value; else sessionDirection = value;
                    }
                    else if (inAudio && value.StartsWith("rtpmap:", StringComparison.Ordinal))
                    {
                        string[] f = value[7..].Split(' ', 2);
                        if (f.Length == 2) rtpmap[f[0]] = f[1];
                    }
                    break;
            }
        }

        sdp.ConnectionIp = audioIp.Length > 0 ? audioIp : sessionIp;
        sdp.Direction = audioDirection.Length > 0 ? audioDirection : sessionDirection;
        foreach (string pt in formats)
            sdp.Codecs.Add(rtpmap.TryGetValue(pt, out string? name) ? name : StaticCodec(pt));
        return sdp;
    }

    private static string StaticCodec(string pt) => pt switch
    {
        "0" => "PCMU/8000",
        "3" => "GSM/8000",
        "4" => "G723/8000",
        "8" => "PCMA/8000",
        "9" => "G722/8000",
        "18" => "G729/8000",
        _ => $"PT {pt}",
    };

    public static bool IsPrivate(string ip)
    {
        if (!IPAddress.TryParse(ip, out IPAddress? a) || a.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
        byte[] b = a.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168)
               || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || b[0] == 169 && b[1] == 254;
    }

    /// <summary>Printable text for a datagram that is not SIP (e.g. a syslog line).</summary>
    public static string Printable(ReadOnlySpan<byte> payload)
    {
        var sb = new StringBuilder(payload.Length);
        foreach (byte b in payload)
        {
            char ch = (char)b;
            if (ch == '\n' || ch == '\t' || (ch >= ' ' && ch < 0x7f)) sb.Append(ch);
            else if (ch == '\r' || ch == '\0') continue;
            else sb.Append('.');
        }
        return sb.ToString().TrimEnd();
    }
}
