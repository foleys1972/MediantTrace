using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Reads a capture back: a classic .pcap of the UDP packets MediantTrace saved,
/// or the .txt (SIP blocks and syslog lines) when there is no .pcap.
/// </summary>
public static partial class CaptureReplay
{
    public readonly record struct Datagram(DateTime Utc, IPEndPoint Source, IPEndPoint Destination, byte[] Payload);

    public readonly record struct TextSip(DateTime Utc, string Source, string Sbc, string Src, string Dst, bool? Inbound, string SessionId, string Text);

    public readonly record struct TextSyslog(DateTime Utc, string Sbc, string Text);

    [GeneratedRegex(@"^===== (?<t>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})  (?<rest>.*)$")]
    private static partial Regex SipHead();

    [GeneratedRegex(@"^(?<t>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})  syslog (?<ip>\S+)  (?<line>.*)$")]
    private static partial Regex SyslogHead();

    [GeneratedRegex(@"\bSBC\s+.*\(([0-9.]+)\)|\bfrom\s+([0-9.]+)")]
    private static partial Regex SbcIp();

    [GeneratedRegex(@"(?<src>\S+)\s+->\s+(?<dst>\S+)")]
    private static partial Regex Arrow();

    [GeneratedRegex(@"\bSID\s+(\S+)")]
    private static partial Regex Sid();

    /// <summary>True when the file starts like a classic pcap or a pcapng (Wireshark's default).</summary>
    public static bool IsPacketFile(ReadOnlySpan<byte> head) =>
        head.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(head) is 0xa1b2c3d4 or 0xa1b23c4d or 0xd4c3b2a1 or 0x4d3cb2a1 or 0x0a0d0d0a;

    /// <summary>
    /// The UDP packets of a capture: MediantTrace's own .pcap, or one saved by Wireshark or tcpdump
    /// (pcap or pcapng; Ethernet with or without VLAN tags, Linux cooked, raw IP or loopback; IPv4 or IPv6).
    /// </summary>
    public static IEnumerable<Datagram> Pcap(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        byte[] magicBytes = new byte[4];
        if (stream.Read(magicBytes) != 4) throw new InvalidOperationException("That file is not a pcap.");
        stream.Position = 0;
        IEnumerable<Datagram> packets = BinaryPrimitives.ReadUInt32LittleEndian(magicBytes) == 0x0a0d0d0a ? PcapNg(stream) : Classic(stream);
        foreach (Datagram d in packets) yield return d;
    }

    private static IEnumerable<Datagram> Classic(Stream stream)
    {
        byte[] fileHeader = new byte[24];
        if (stream.Read(fileHeader) != 24) throw new InvalidOperationException("That file is not a pcap.");
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(fileHeader);
        bool swap, nanos;
        switch (magic)
        {
            case 0xa1b2c3d4: swap = false; nanos = false; break;
            case 0xa1b23c4d: swap = false; nanos = true; break;
            case 0xd4c3b2a1: swap = true; nanos = false; break;
            case 0x4d3cb2a1: swap = true; nanos = true; break;
            default: throw new InvalidOperationException("That pcap uses a format MediantTrace cannot read.");
        }
        uint U32(ReadOnlySpan<byte> b) => swap ? BinaryPrimitives.ReadUInt32BigEndian(b) : BinaryPrimitives.ReadUInt32LittleEndian(b);
        uint link = U32(fileHeader.AsSpan(20));
        if (!KnownLink(link)) throw new InvalidOperationException($"That pcap uses link type {link}, which MediantTrace cannot read.");

        byte[] recordHeader = new byte[16];
        while (true)
        {
            int read = stream.Read(recordHeader);
            if (read == 0) yield break;
            if (read < 16) throw new InvalidOperationException("The pcap ended in the middle of a packet.");
            uint seconds = U32(recordHeader.AsSpan(0, 4));
            uint fraction = U32(recordHeader.AsSpan(4, 4));
            uint included = U32(recordHeader.AsSpan(8, 4));
            if (included > 2_000_000) throw new InvalidOperationException("The pcap contains a packet that is too large.");
            byte[] frame = new byte[included];
            if (included > 0 && stream.ReadAtLeast(frame, frame.Length, false) != frame.Length) throw new InvalidOperationException("The pcap ended in the middle of a packet.");
            if (!TryUdp(link, frame, out IPEndPoint? source, out IPEndPoint? destination, out byte[]? payload) || source is null || destination is null || payload is null) continue;
            long ticks = seconds * TimeSpan.TicksPerSecond + fraction * (nanos ? 1 : 10);
            yield return new Datagram(DateTime.UnixEpoch.AddTicks(ticks), source, destination, payload);
        }
    }

    /// <summary>pcapng: section header, interface descriptions (link type, time resolution) and enhanced or simple packet blocks.</summary>
    private static IEnumerable<Datagram> PcapNg(Stream stream)
    {
        bool swap = false;
        var links = new List<(uint link, double tickPerUnit)>();
        byte[] head = new byte[8];
        while (true)
        {
            int read = stream.ReadAtLeast(head, 8, false);
            if (read == 0) yield break;
            if (read < 8) throw new InvalidOperationException("The pcapng ended in the middle of a block.");
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(head);
            if (type == 0x0a0d0d0a)
            {
                byte[] bom = new byte[4];
                stream.ReadExactly(bom);
                swap = BinaryPrimitives.ReadUInt32LittleEndian(bom) != 0x1a2b3c4d;
                links.Clear();
                uint shbLength = U32(head.AsSpan(4));
                if (shbLength < 28 || shbLength > 1_000_000) throw new InvalidOperationException("The pcapng section header is damaged.");
                stream.Seek(shbLength - 12, SeekOrigin.Current);
                continue;
            }
            uint length = U32(head.AsSpan(4));
            if (length < 12 || length > 4_000_000) throw new InvalidOperationException("The pcapng contains a damaged block.");
            byte[] body = new byte[length - 8];
            if (stream.ReadAtLeast(body, body.Length, false) != body.Length) throw new InvalidOperationException("The pcapng ended in the middle of a block.");
            type = U32(head);
            if (type == 1 && body.Length >= 8)
            {
                uint link = U16(body.AsSpan(0));
                double tick = 10; // default microseconds = 10 ticks
                int at = 8;
                while (at + 4 <= body.Length - 4)
                {
                    int code = U16(body.AsSpan(at)), len = U16(body.AsSpan(at + 2));
                    if (code == 0) break;
                    if (code == 9 && len >= 1 && at + 4 < body.Length)
                    {
                        byte r = body[at + 4];
                        double unitsPerSecond = (r & 0x80) != 0 ? Math.Pow(2, r & 0x7f) : Math.Pow(10, r & 0x7f);
                        tick = TimeSpan.TicksPerSecond / unitsPerSecond;
                    }
                    at += 4 + ((len + 3) & ~3);
                }
                links.Add((link, tick));
                continue;
            }
            if (type == 6 && body.Length >= 20)
            {
                int iface = (int)U32(body.AsSpan(0));
                if (iface >= links.Count) continue;
                ulong stamp = ((ulong)U32(body.AsSpan(4)) << 32) | U32(body.AsSpan(8));
                int captured = (int)Math.Min(U32(body.AsSpan(12)), (uint)(body.Length - 20));
                var (link, tick) = links[iface];
                if (!KnownLink(link)) continue;
                byte[] frame = body.AsSpan(20, captured).ToArray();
                if (!TryUdp(link, frame, out IPEndPoint? source, out IPEndPoint? destination, out byte[]? payload) || source is null || destination is null || payload is null) continue;
                yield return new Datagram(DateTime.UnixEpoch.AddTicks((long)(stamp * tick)), source, destination, payload);
                continue;
            }
            if (type == 3 && body.Length >= 4 && links.Count > 0 && KnownLink(links[0].link))
            {
                // Simple packet block: no timestamp.
                byte[] frame = body.AsSpan(4).ToArray();
                if (TryUdp(links[0].link, frame, out IPEndPoint? source, out IPEndPoint? destination, out byte[]? payload) && source is not null && destination is not null && payload is not null)
                    yield return new Datagram(DateTime.UnixEpoch, source, destination, payload);
            }
        }

        uint U32(ReadOnlySpan<byte> b) => swap ? BinaryPrimitives.ReadUInt32BigEndian(b) : BinaryPrimitives.ReadUInt32LittleEndian(b);
        ushort U16(ReadOnlySpan<byte> b) => swap ? BinaryPrimitives.ReadUInt16BigEndian(b) : BinaryPrimitives.ReadUInt16LittleEndian(b);
    }

    // Ethernet, BSD loopback, raw IP (three numbers), Linux cooked v1 and v2.
    private static bool KnownLink(uint link) => link is 1 or 0 or 108 or 12 or 14 or 101 or 113 or 276;

    public static void ReadText(string path, Action<TextSip> sip, Action<TextSyslog> syslog)
    {
        TextSip? pending = null;
        var body = new StringBuilder();
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.TrimEnd('\r');
            if (SipHead().Match(line) is { Success: true } head)
            {
                Flush(pending, body, sip);
                pending = Head(head);
                body.Clear();
                continue;
            }
            if (SyslogHead().Match(line) is { Success: true } logged)
            {
                Flush(pending, body, sip);
                pending = null;
                body.Clear();
                syslog(new TextSyslog(When(logged.Groups["t"].Value), logged.Groups["ip"].Value, logged.Groups["line"].Value));
                continue;
            }
            if (line.StartsWith("MediantTrace ", StringComparison.Ordinal)) continue;
            if (pending is not null)
            {
                if (body.Length > 0) body.Append('\n');
                body.Append(line);
            }
            else if (line.Length > 0)
                syslog(new TextSyslog(DateTime.UnixEpoch, "", line));
        }
        Flush(pending, body, sip);
    }

    private static void Flush(TextSip? pending, StringBuilder body, Action<TextSip> sip)
    {
        if (pending is { } block && body.Length > 0)
            sip(block with { Text = body.ToString() });
    }

    private static TextSip Head(Match head)
    {
        string rest = head.Groups["rest"].Value;
        Match ip = SbcIp().Match(rest);
        string sbc = ip.Success ? (ip.Groups[1].Success ? ip.Groups[1].Value : ip.Groups[2].Value) : "";
        string source = rest.Contains("(SYSLOG)", StringComparison.Ordinal) ? "SYSLOG" : "DR";
        bool? inbound = Regex.IsMatch(rest, @"(?:^|\s)IN(?:\s|$)") ? true : Regex.IsMatch(rest, @"(?:^|\s)OUT(?:\s|$)") ? false : null;
        Match arrow = Arrow().Match(rest);
        Match sid = Sid().Match(rest);
        return new TextSip(When(head.Groups["t"].Value), source, sbc, arrow.Success ? arrow.Groups["src"].Value : "", arrow.Success ? arrow.Groups["dst"].Value : "", inbound, sid.Success ? sid.Groups[1].Value : "", "");
    }

    private static DateTime When(string text) =>
        DateTime.ParseExact(text, "yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();

    private static bool TryUdp(uint link, byte[] frame, out IPEndPoint? source, out IPEndPoint? destination, out byte[]? payload)
    {
        source = destination = null;
        payload = null;
        // Find where the IP header starts and which IP version it is.
        int ip;
        int etherType;
        switch (link)
        {
            case 1:
                if (frame.Length < 14) return false;
                ip = 14;
                etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(12));
                while (etherType is 0x8100 or 0x88a8 && frame.Length >= ip + 4)
                {
                    etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(ip + 2));
                    ip += 4;
                }
                break;
            case 113:
                if (frame.Length < 16) return false;
                ip = 16;
                etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(14));
                break;
            case 276:
                if (frame.Length < 20) return false;
                ip = 20;
                etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(0));
                break;
            case 0 or 108:
                ip = 4;
                etherType = 0;
                break;
            default:
                ip = 0;
                etherType = 0;
                break;
        }
        if (frame.Length <= ip) return false;
        int version = frame[ip] >> 4;
        if (etherType == 0x0800 && version != 4 || etherType == 0x86dd && version != 6 || etherType is not (0 or 0x0800 or 0x86dd)) return false;

        int udp;
        IPAddress srcIp, dstIp;
        if (version == 4)
        {
            if (frame.Length < ip + 20) return false;
            int ihl = (frame[ip] & 0x0f) * 4;
            if (ihl < 20 || frame[ip + 9] != 17) return false;
            // Only the first fragment carries the UDP header.
            if ((BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(ip + 6)) & 0x1fff) != 0) return false;
            udp = ip + ihl;
            srcIp = new IPAddress(frame.AsSpan(ip + 12, 4));
            dstIp = new IPAddress(frame.AsSpan(ip + 16, 4));
        }
        else if (version == 6)
        {
            if (frame.Length < ip + 40 || frame[ip + 6] != 17) return false;
            udp = ip + 40;
            srcIp = new IPAddress(frame.AsSpan(ip + 8, 16));
            dstIp = new IPAddress(frame.AsSpan(ip + 24, 16));
        }
        else return false;

        if (frame.Length < udp + 8) return false;
        int srcPort = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(udp));
        int dstPort = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(udp + 2));
        int length = BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(udp + 4));
        int size = Math.Min(Math.Max(length - 8, 0), frame.Length - (udp + 8));
        source = new IPEndPoint(srcIp, srcPort);
        destination = new IPEndPoint(dstIp, dstPort);
        payload = frame.AsSpan(udp + 8, size).ToArray();
        return true;
    }
}
