using System.Text;

namespace MediantTrace;

/// <summary>
/// Picks the packets that belong to one call out of a capture, and writes them as a pcap.
/// A packet belongs when it carries the call's Call-ID or session, or its audio addresses.
/// </summary>
public static class CallExport
{
    public static bool Belongs(CaptureReplay.Datagram packet, CallStore.PacketSelect call)
    {
        if (packet.Utc < call.From || packet.Utc > call.To) return false;
        if (call.Sbc.Length > 0 && packet.Source.Address.ToString() != call.Sbc) return false;
        ReadOnlySpan<byte> payload = packet.Payload;
        foreach (string id in call.CallIds)
            if (ContainsAscii(payload, id)) return true;
        if (call.SessionId.Length > 0 && (ContainsAscii(payload, "SID=" + call.SessionId) || ContainsAscii(payload, "SID:" + call.SessionId)))
            return true;

        Acdr.Packet? parsed = Acdr.Parse(payload);
        if (parsed is null) return false;
        if (call.SessionId.Length > 0 && parsed.SessionId == call.SessionId) return true;
        if (!parsed.IsRtp) return false;
        string src = parsed.SrcIp is null ? "" : $"{parsed.SrcIp}:{parsed.SrcPort}";
        string dst = parsed.DstIp is null ? "" : $"{parsed.DstIp}:{parsed.DstPort}";
        return (src.Length > 0 && call.Endpoints.Contains(src)) || (dst.Length > 0 && call.Endpoints.Contains(dst));
    }

    public static byte[] Pcap(IEnumerable<CaptureReplay.Datagram> packets)
    {
        using var memory = new MemoryStream();
        using (var writer = new PcapWriter(memory, ownStream: false))
        {
            foreach (CaptureReplay.Datagram packet in packets)
                writer.Write(packet.Utc, packet.Source, packet.Destination, packet.Payload);
        }
        return memory.ToArray();
    }

    private static bool ContainsAscii(ReadOnlySpan<byte> data, string text)
    {
        if (text.Length == 0) return false;
        byte[] needle = Encoding.ASCII.GetBytes(text);
        if (data.Length < needle.Length) return false;
        for (int i = 0; i <= data.Length - needle.Length; i++)
            if (data.Slice(i, needle.Length).SequenceEqual(needle)) return true;
        return false;
    }
}
