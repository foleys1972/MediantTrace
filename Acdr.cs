using System.Buffers.Binary;
using System.Net;

namespace MediantTrace;

/// <summary>
/// Decodes AudioCodes Debug Recording (ACDR) packets: the header the Mediant
/// puts in front of every traced SIP or RTP packet. Layout as documented by
/// Wireshark's packet-acdr dissector (versions 0.1 to 0.9).
/// </summary>
public static class Acdr
{
    public const byte MediaRtp = 1, MediaRtcp = 2, MediaSip = 10;

    // Trace points that carry media in from / out to the network.
    private static readonly HashSet<byte> FromNetwork = [0, 4, 18, 20, 27];   // Net2Dsp, Net2Host, DspDecoder, VoipDecoder, Net2DspPing
    private static readonly HashSet<byte> ToNetwork = [1, 5, 17, 19, 21, 22, 23, 24, 28];
    private const byte TraceSystem = 6;

    private const byte Ipv6Mask = 0x2, HeaderAddedMask = 0x8;

    public sealed class Packet
    {
        public int Version;
        public byte MediaType;
        public byte TracePoint;
        public uint Timestamp;          // device microseconds (wraps)
        public bool HeaderAdded;
        public IPAddress? SrcIp, DstIp;
        public int SrcPort, DstPort;
        /// <summary>True: arrived at the Mediant from the network. False: sent by it. Null: unknown.</summary>
        public bool? Inbound;
        public string SessionId = "";
        public int PayloadOffset, PayloadLength;

        public bool IsRtp => MediaType == MediaRtp || (MediaType >= 51 && MediaType <= 59) || MediaType == 27;
        public bool IsRtcp => MediaType == MediaRtcp || MediaType == 28;
        public bool IsSip => MediaType == MediaSip || MediaType == 37;
    }

    public static Packet? Parse(ReadOnlySpan<byte> d)
    {
        if (d.Length < 15) return null;
        int version = d[0];
        int minor = version & 0xF;
        if ((version >> 4) != 0 || minor > 9) return null;

        int headerLength = minor < 5 ? 15 : minor < 7 ? 19 : minor == 7 ? 23 : minor == 8 ? 24 : 28;
        int cidLength = minor >= 9 ? 4 : 2;
        if (d.Length < headerLength) return null;

        var p = new Packet { Version = version };
        int o = 1;
        if (minor <= 3) { p.Timestamp = BinaryPrimitives.ReadUInt32BigEndian(d[o..]) << 16 | BinaryPrimitives.ReadUInt16BigEndian(d[(o + 4)..]); o += 6; }
        else { p.Timestamp = BinaryPrimitives.ReadUInt32BigEndian(d[o..]); o += 4; }
        if (minor >= 4) o += 2;          // sequence number
        o += cidLength * 2;              // source and destination channel ids
        byte extra = d[o++];
        bool legacyExtra = extra == 0xAA && minor <= 3;
        p.HeaderAdded = !legacyExtra && (extra & HeaderAddedMask) != 0;
        bool ipv6 = !legacyExtra && minor >= 3 && (extra & Ipv6Mask) != 0;
        p.TracePoint = d[o++];
        p.MediaType = d[o++];
        int extLength = minor < 5 ? 0 : d[o];
        o++;
        // Session id: the same "board:reset:session" the Mediant's syslog shows as [SID=...].
        if (minor >= 7 && d.Length >= o + 8)
        {
            uint board = (uint)(d[o] << 16 | d[o + 1] << 8 | d[o + 2]);
            byte reset = d[o + 3];
            ulong session = minor >= 8 && d.Length >= o + 9
                ? (ulong)d[o + 4] << 32 | BinaryPrimitives.ReadUInt32BigEndian(d[(o + 5)..])
                : BinaryPrimitives.ReadUInt32BigEndian(d[(o + 4)..]);
            p.SessionId = $"{board:x}:{reset}:{session}";
        }

        int payload = headerLength + extLength;
        if (payload > d.Length) return null;
        if (extLength > 0) ReadExtension(p, d.Slice(headerLength, extLength), ipv6);

        p.PayloadOffset = payload;
        p.PayloadLength = d.Length - payload;
        if (p.HeaderAdded) UnwrapIp(p, d);
        return p;
    }

    private static void ReadExtension(Packet p, ReadOnlySpan<byte> e, bool ipv6)
    {
        int ipLength = ipv6 ? 16 : 4;
        if (p.IsRtp || p.IsRtcp)
        {
            if (e.Length < ipLength + 4) return;
            if (FromNetwork.Contains(p.TracePoint))
            {
                p.Inbound = true;
                p.SrcIp = new IPAddress(e[..ipLength]);
                p.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(e[ipLength..]);
                p.DstPort = BinaryPrimitives.ReadUInt16BigEndian(e[(ipLength + 2)..]);
            }
            else if (ToNetwork.Contains(p.TracePoint))
            {
                p.Inbound = false;
                p.DstIp = new IPAddress(e[..ipLength]);
                p.DstPort = BinaryPrimitives.ReadUInt16BigEndian(e[ipLength..]);
                p.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(e[(ipLength + 2)..]);
            }
        }
        else if (p.IsSip && p.TracePoint == TraceSystem && e.Length >= 14)
        {
            p.SrcIp = new IPAddress(e[..4]);
            p.DstIp = new IPAddress(e[4..8]);
            p.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(e[8..]);
            p.DstPort = BinaryPrimitives.ReadUInt16BigEndian(e[10..]);
            // byte 13: direction. Checked against real Mediant 7.20A traffic (header-added
            // packets carry the true IP header): 0 = arriving at the SBC, 1 = sent by it.
            p.Inbound = e[13] == 0;
        }
    }

    /// <summary>"Header added": the payload is a whole IPv4 packet; take addresses from it.</summary>
    private static void UnwrapIp(Packet p, ReadOnlySpan<byte> d)
    {
        var ip = d.Slice(p.PayloadOffset, p.PayloadLength);
        if (ip.Length < 20 || ip[0] >> 4 != 4) return;
        int ihl = (ip[0] & 0xF) * 4;
        if (ip.Length < ihl + 8) return;
        p.SrcIp = new IPAddress(ip.Slice(12, 4));
        p.DstIp = new IPAddress(ip.Slice(16, 4));
        int transport = ihl;
        p.SrcPort = BinaryPrimitives.ReadUInt16BigEndian(ip[transport..]);
        p.DstPort = BinaryPrimitives.ReadUInt16BigEndian(ip[(transport + 2)..]);
        int headerLength = ip[9] switch
        {
            17 => 8,                                            // UDP
            6 when ip.Length >= ihl + 20 => (ip[ihl + 12] >> 4) * 4, // TCP
            _ => -1,
        };
        if (headerLength < 0) return;
        p.PayloadOffset += ihl + headerLength;
        p.PayloadLength = Math.Max(0, p.PayloadLength - ihl - headerLength);
    }
}
