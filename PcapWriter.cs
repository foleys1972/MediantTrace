using System.Buffers.Binary;
using System.Net;

namespace MediantTrace;

/// <summary>
/// Writes received UDP datagrams to a classic .pcap file. Each payload is
/// wrapped in Ethernet/IPv4/UDP headers carrying the real source and
/// destination address and port, so any pcap reader decodes it as if it had
/// been sniffed: port 925 as AudioCodes Debug Recording, 514 as syslog.
/// </summary>
public sealed class PcapWriter : IDisposable
{
    private const int EthernetLength = 14;
    private const int Ipv4Length = 20;
    private const int UdpLength = 8;
    public const int MaxPayload = 65535 - Ipv4Length - UdpLength;

    private readonly FileStream _stream;
    private ushort _ipId;

    public PcapWriter(string path)
    {
        _stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        Span<byte> header = stackalloc byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(header[0..], 0xa1b2c3d4); // microsecond timestamps
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(header[6..], 4);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[12..], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], 65535);       // snap length
        BinaryPrimitives.WriteUInt32LittleEndian(header[20..], 1);           // LINKTYPE_ETHERNET
        _stream.Write(header);
    }

    public void Write(DateTime utc, IPEndPoint source, IPEndPoint destination, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > MaxPayload) payload = payload[..MaxPayload];
        int frameLength = EthernetLength + Ipv4Length + UdpLength + payload.Length;
        byte[] record = new byte[16 + frameLength];
        Span<byte> r = record;

        long ticks = utc.Ticks - DateTime.UnixEpoch.Ticks;
        BinaryPrimitives.WriteUInt32LittleEndian(r[0..], (uint)(ticks / TimeSpan.TicksPerSecond));
        BinaryPrimitives.WriteUInt32LittleEndian(r[4..], (uint)(ticks % TimeSpan.TicksPerSecond / 10));
        BinaryPrimitives.WriteUInt32LittleEndian(r[8..], (uint)frameLength);
        BinaryPrimitives.WriteUInt32LittleEndian(r[12..], (uint)frameLength);

        Span<byte> eth = r.Slice(16, EthernetLength);
        // Locally administered placeholder MACs: the real ones are not known to a UDP listener.
        eth[0] = 0x02; eth[5] = 0x02;
        eth[6] = 0x02; eth[11] = 0x01;
        BinaryPrimitives.WriteUInt16BigEndian(eth[12..], 0x0800);

        Span<byte> ip = r.Slice(16 + EthernetLength, Ipv4Length);
        ip[0] = 0x45;
        BinaryPrimitives.WriteUInt16BigEndian(ip[2..], (ushort)(Ipv4Length + UdpLength + payload.Length));
        BinaryPrimitives.WriteUInt16BigEndian(ip[4..], _ipId++);
        ip[8] = 64;
        ip[9] = 17; // UDP
        WriteIpv4(ip[12..], source.Address);
        WriteIpv4(ip[16..], destination.Address);
        BinaryPrimitives.WriteUInt16BigEndian(ip[10..], Checksum(ip));

        Span<byte> udp = r.Slice(16 + EthernetLength + Ipv4Length, UdpLength);
        BinaryPrimitives.WriteUInt16BigEndian(udp[0..], (ushort)source.Port);
        BinaryPrimitives.WriteUInt16BigEndian(udp[2..], (ushort)destination.Port);
        BinaryPrimitives.WriteUInt16BigEndian(udp[4..], (ushort)(UdpLength + payload.Length));
        // UDP checksum 0 = not computed, which IPv4 allows.

        payload.CopyTo(r[(16 + EthernetLength + Ipv4Length + UdpLength)..]);
        _stream.Write(record);
    }

    public void Flush() => _stream.Flush();

    public void Dispose() => _stream.Dispose();

    private static void WriteIpv4(Span<byte> target, IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (!address.TryWriteBytes(target[..4], out int written) || written != 4) target[..4].Clear();
    }

    private static ushort Checksum(ReadOnlySpan<byte> header)
    {
        uint sum = 0;
        for (int i = 0; i < header.Length; i += 2) sum += BinaryPrimitives.ReadUInt16BigEndian(header[i..]);
        while (sum > 0xffff) sum = (sum & 0xffff) + (sum >> 16);
        return (ushort)~sum;
    }
}
