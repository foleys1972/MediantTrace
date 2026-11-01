using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace MediantTrace;

/// <summary>
/// Builds a WAV of one G.711 stream from the RTP packets in a capture.
/// Payload type 0 is µ-law and 8 is A-law. A missing packet becomes silence, so the timing stays put.
/// </summary>
public static class CallAudio
{
    public const int Rate = 8000;
    public const int MaxSeconds = 600;

    public static (bool ok, string message, byte[]? wav) Build(IEnumerable<byte[]> rtpPackets)
    {
        var frames = new List<Frame>();
        int extended = 0;
        ushort lastSeq = 0;
        bool haveSeq = false;
        bool otherCodec = false;
        foreach (byte[] packet in rtpPackets)
        {
            if (!TryPayload(packet, out ushort seq, out uint ts, out byte pt, out byte[] payload)) continue;
            if (pt is not 0 and not 8) { otherCodec = true; continue; }
            int ext;
            if (!haveSeq) { ext = seq; extended = seq; lastSeq = seq; haveSeq = true; }
            else
            {
                ushort step = (ushort)(seq - lastSeq);
                if (step == 0) continue;
                if (step >= 0x8000)
                    ext = extended - (ushort)(lastSeq - seq);
                else
                {
                    extended += step;
                    ext = extended;
                    lastSeq = seq;
                }
            }
            frames.Add(new Frame(ext, ts, pt, payload));
        }
        if (frames.Count == 0)
            return (false, otherCodec ? "Only G.711 audio can be played." : "No audio packets for this stream were in the capture.", null);

        frames.Sort((a, b) => a.Seq.CompareTo(b.Seq));
        var pcm = new List<short>(frames.Count * 160);
        uint clock = frames[0].Timestamp;
        int limit = Rate * MaxSeconds;
        foreach (Frame frame in frames)
        {
            if (pcm.Count >= limit) break;
            int gap = (int)(frame.Timestamp - clock);
            if (gap is > 0 and < Rate) while (gap-- > 0 && pcm.Count < limit) pcm.Add(0);
            foreach (byte sample in frame.Payload)
            {
                if (pcm.Count >= limit) break;
                pcm.Add(frame.Pt == 8 ? G711.AlawToLinear(sample) : G711.UlawToLinear(sample));
            }
            clock = frame.Timestamp + (uint)frame.Payload.Length;
        }
        if (pcm.Count == 0) return (false, "No audio packets for this stream were in the capture.", null);
        return (true, "", Wav(pcm));
    }

    /// <summary>The RTP packet for one stream, when this datagram is that stream's G.711 audio.</summary>
    public static bool TryStream(CaptureReplay.Datagram packet, string ssrc, string src, string dst, out byte[] rtp)
    {
        rtp = [];
        ReadOnlySpan<byte> payload = packet.Payload;
        Acdr.Packet? header = Acdr.Parse(payload);
        ReadOnlySpan<byte> body;
        string gotSrc, gotDst;
        if (header is { IsRtp: true })
        {
            if (header.PayloadLength <= 0) return false;
            body = payload.Slice(header.PayloadOffset, header.PayloadLength);
            gotSrc = header.SrcIp is null ? $"(SBC):{header.SrcPort}" : $"{header.SrcIp}:{header.SrcPort}";
            gotDst = header.DstIp is null ? $"(SBC):{header.DstPort}" : $"{header.DstIp}:{header.DstPort}";
        }
        else
        {
            body = payload;
            gotSrc = packet.Source.ToString();
            gotDst = packet.Destination.ToString();
        }
        if (!gotSrc.Equals(src, StringComparison.OrdinalIgnoreCase) || !gotDst.Equals(dst, StringComparison.OrdinalIgnoreCase)) return false;
        if (body.Length < 12 || body[0] >> 6 != 2) return false;
        uint got = BinaryPrimitives.ReadUInt32BigEndian(body[8..]);
        if (!got.ToString("x8").Equals(ssrc, StringComparison.OrdinalIgnoreCase)) return false;
        rtp = body.ToArray();
        return true;
    }

    private static bool TryPayload(byte[] rtp, out ushort seq, out uint timestamp, out byte pt, out byte[] payload)
    {
        seq = 0; timestamp = 0; pt = 0; payload = [];
        if (rtp.Length < 12 || rtp[0] >> 6 != 2) return false;
        int offset = 12 + (rtp[0] & 0x0F) * 4;
        if ((rtp[0] & 0x10) != 0)
        {
            if (rtp.Length < offset + 4) return false;
            offset += 4 + BinaryPrimitives.ReadUInt16BigEndian(rtp.AsSpan(offset + 2)) * 4;
        }
        int end = rtp.Length;
        if ((rtp[0] & 0x20) != 0 && end > offset) end -= rtp[end - 1];
        if (offset < 12 || end < offset || offset > rtp.Length) return false;
        seq = BinaryPrimitives.ReadUInt16BigEndian(rtp.AsSpan(2));
        timestamp = BinaryPrimitives.ReadUInt32BigEndian(rtp.AsSpan(4));
        pt = (byte)(rtp[1] & 0x7F);
        payload = rtp[offset..end];
        return payload.Length > 0;
    }

    private static byte[] Wav(List<short> samples)
    {
        int data = samples.Count * 2;
        var wav = new byte[44 + data];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(4), 36 + data);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);
        Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(24), Rate);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(28), Rate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(34), 16);
        Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(40), data);
        for (int i = 0; i < samples.Count; i++)
            BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(44 + i * 2), samples[i]);
        return wav;
    }

    private readonly record struct Frame(int Seq, uint Timestamp, byte Pt, byte[] Payload);

    /// <summary>A capture with one short A-law call, for the self-test and for checking playback.</summary>
    public static void WriteFixture(string folder)
    {
        Directory.CreateDirectory(folder);
        const string stem = "mediant-20261017-130000";
        var src = IPAddress.Parse("192.0.2.20");
        var here = IPAddress.Parse("192.0.2.9");
        string sdp = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/AVP 8\r\na=rtpmap:8 PCMA/8000\r\n";
        string sip = "<134> ---- Incoming SIP Message from 192.0.2.20:5060 to SIPInterface #0 UDP ----\r\n" +
                     "INVITE sip:100@192.0.2.10 SIP/2.0\r\nVia: SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1\r\n" +
                     "From: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>\r\nCall-ID: play@host\r\nCSeq: 1 INVITE\r\n" +
                     "Content-Type: application/sdp\r\n\r\n" + sdp;
        DateTime at = DateTime.UtcNow;
        using var writer = new PcapWriter(Path.Combine(folder, stem + ".pcap"));
        writer.Write(at, new IPEndPoint(src, 5060), new IPEndPoint(here, 5514), Encoding.ASCII.GetBytes(sip));
        writer.Write(at.AddMilliseconds(20), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrRtp(0, 0, Rtp(0, 0, 8, 0x2A, 160, 0x11111111), src, 4000, 5000));
        writer.Write(at.AddMilliseconds(60), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrRtp(2, 320, Rtp(2, 320, 8, 0x2A, 160, 0x11111111), src, 4000, 5000));
    }

    /// <summary>A capture of a TLS call whose audio is SRTP, plus one TLS record that cannot be read.</summary>
    public static void WriteSecureFixture(string folder)
    {
        Directory.CreateDirectory(folder);
        const string stem = "mediant-20261018-132900";
        var src = IPAddress.Parse("192.0.2.20");
        var here = IPAddress.Parse("192.0.2.9");
        string sdp = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/SAVP 8\r\na=rtpmap:8 PCMA/8000\r\na=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n";
        string sip = "<134> ---- Incoming SIP Message from 192.0.2.20:5061 to SIPInterface #0 TLS 5061 ----\r\n" +
                     "INVITE sip:100@192.0.2.10 SIP/2.0\r\nVia: SIP/2.0/TLS 192.0.2.20:5061;branch=z9hG4bK1\r\n" +
                     "From: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>\r\nCall-ID: secure@host\r\nCSeq: 1 INVITE\r\n" +
                     "Content-Type: application/sdp\r\n\r\n" + sdp;
        DateTime at = DateTime.UtcNow;
        byte[] tls = [0x17, 0x03, 0x03, 0x00, 0x04, 0x11, 0x22, 0x33, 0x44];
        using var writer = new PcapWriter(Path.Combine(folder, stem + ".pcap"));
        writer.Write(at, new IPEndPoint(src, 5061), new IPEndPoint(here, 5514), Encoding.ASCII.GetBytes(sip));
        writer.Write(at.AddMilliseconds(20), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrRtp(0, 0, Rtp(0, 0, 8, 0x2A, 160, 0x22222222), src, 4000, 5000));
        writer.Write(at.AddMilliseconds(40), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrPacket(Acdr.MediaSip, tls, src, 5061, 5061));
    }

    /// <summary>A capture whose audio carries two pressed digits, for checking the call ladder.</summary>
    public static void WriteDigitFixture(string folder)
    {
        Directory.CreateDirectory(folder);
        const string stem = "mediant-20261017-131600";
        var src = IPAddress.Parse("192.0.2.20");
        var here = IPAddress.Parse("192.0.2.9");
        string sdp = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/AVP 8 101\r\na=rtpmap:8 PCMA/8000\r\na=rtpmap:101 telephone-event/8000\r\n";
        string sip = "<134> ---- Incoming SIP Message from 192.0.2.20:5060 to SIPInterface #0 UDP ----\r\n" +
                     "INVITE sip:100@192.0.2.10 SIP/2.0\r\nVia: SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1\r\n" +
                     "From: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>\r\nCall-ID: digit@host\r\nCSeq: 1 INVITE\r\n" +
                     "Content-Type: application/sdp\r\n\r\n" + sdp;
        DateTime at = DateTime.UtcNow;
        using var writer = new PcapWriter(Path.Combine(folder, stem + ".pcap"));
        writer.Write(at, new IPEndPoint(src, 5060), new IPEndPoint(here, 5514), Encoding.ASCII.GetBytes(sip));
        writer.Write(at.AddMilliseconds(30), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrRtp(1, 0, Event(1, 0, 5, true, 1600), src, 4000, 5000));
        writer.Write(at.AddMilliseconds(50), new IPEndPoint(src, 925), new IPEndPoint(here, 925), DrRtp(2, 1600, Event(2, 1600, 10, true, 800), src, 4000, 5000));
    }

    private static byte[] Event(ushort seq, uint timestamp, byte ev, bool end, ushort duration)
    {
        var rtp = new byte[16];
        rtp[0] = 0x80;
        rtp[1] = 101;
        BinaryPrimitives.WriteUInt16BigEndian(rtp.AsSpan(2), seq);
        BinaryPrimitives.WriteUInt32BigEndian(rtp.AsSpan(4), timestamp);
        rtp[12] = ev;
        rtp[13] = (byte)(end ? 0x8A : 0x0A);
        BinaryPrimitives.WriteUInt16BigEndian(rtp.AsSpan(14), duration);
        return rtp;
    }

    private static byte[] Rtp(ushort seq, uint timestamp, byte pt, byte sample, int count, uint ssrc)
    {
        var rtp = new byte[12 + count];
        rtp[0] = 0x80;
        rtp[1] = pt;
        BinaryPrimitives.WriteUInt16BigEndian(rtp.AsSpan(2), seq);
        BinaryPrimitives.WriteUInt32BigEndian(rtp.AsSpan(4), timestamp);
        BinaryPrimitives.WriteUInt32BigEndian(rtp.AsSpan(8), ssrc);
        Array.Fill(rtp, sample, 12, count);
        return rtp;
    }

    /// <summary>AudioCodes Debug Recording, version 0.5, carrying an RTP packet from the far end.</summary>
    private static byte[] DrRtp(ushort seq, uint timestamp, byte[] rtp, IPAddress src, ushort srcPort, ushort dstPort) =>
        DrPacket(Acdr.MediaRtp, rtp, src, srcPort, dstPort, seq, timestamp);

    private static byte[] DrPacket(byte mediaType, byte[] payload, IPAddress src, ushort srcPort, ushort dstPort, ushort seq = 0, uint timestamp = 0)
    {
        var packet = new byte[27 + payload.Length];
        packet[0] = 0x05;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(1), timestamp);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(5), seq);
        packet[12] = 0; // from the network
        packet[13] = mediaType;
        packet[14] = 8;
        src.GetAddressBytes().CopyTo(packet, 19);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(23), srcPort);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(25), dstPort);
        payload.CopyTo(packet, 27);
        return packet;
    }
}
