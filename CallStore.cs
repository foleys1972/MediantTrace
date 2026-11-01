using System.Buffers.Binary;

namespace MediantTrace;

/// <summary>
/// Everything recorded in the current capture, organised for the web page:
/// SIP dialogs by Call-ID, RTP streams, and the raw syslog lines.
/// </summary>
public sealed class CallStore
{
    private const int MaxCalls = 3000, MaxMessagesPerCall = 400, MaxStreams = 8000, MaxSyslog = 50000;

    private readonly object _gate = new();
    private readonly Dictionary<string, Call> _calls = new();
    private readonly List<Call> _order = [];
    private readonly Dictionary<string, RtpStream> _streams = new();
    private readonly LinkedList<SyslogLine> _syslog = new();
    private readonly List<SealedPacket> _sealed = [];
    private int _nextCallId = 1;

    private readonly record struct SealedPacket(DateTime Time, string Sbc, string Kind);

    public sealed class SipEntry
    {
        public DateTime Time;
        public string Source = "";      // "DR" or "SYSLOG"
        public string Src = "", Dst = ""; // ip:port, empty when the trace doesn't say
        public bool? Inbound;            // arrived at the Mediant (true) or sent by it (false)
        public SipText.Message Message = null!;
    }

    public sealed class OtherEntry
    {
        public DateTime Time;
        public string Kind = "";        // HTTP, DNS or VoiceAI
        public string Peer = "";        // ip:port of the far end, when the banner names it
        public bool? Inbound;
        public string FirstLine = "";
        public string Text = "";
    }

    public sealed class SdpEntry
    {
        public DateTime Time;
        public int MessageIndex;
        public string Owner = "";        // who sent this SDP (where they want to receive)
        public bool? OwnerIsMediant;
        public SipText.Sdp Sdp = null!;
        public string Label = "";        // e.g. "INVITE" or "200 (INVITE)"
    }

    public sealed class Call
    {
        public int Id;
        public string Key = "";          // SBC + Call-ID
        public string Sbc = "";          // address of the SBC that reported it
        public string CallId = "";
        public string SessionId = "";   // the Mediant's SID: the same for both legs of a call
        public DateTime Started, LastSeen;
        public DateTime? Answered, Ended;
        public string From = "", To = "";
        public int FinalStatus;
        public string State = "trying";
        public List<SipEntry> Messages = [];
        public List<SdpEntry> Sdps = [];
        public List<CdrDecoder.Record> Cdrs = [];
        /// <summary>HTTP, DNS and VoiceAI messages the SBC logged for this call's session (SID), for the ladder.</summary>
        public List<OtherEntry> Others = [];
        public int DroppedMessages;
        /// <summary>Only OPTIONS keep-alives: hidden from the call list unless asked for.</summary>
        public bool IsOptions => Messages.Count > 0 && Cdrs.Count == 0 && Messages.All(m => m.Message.CSeqMethod == "OPTIONS");
    }

    public sealed class RtpStream
    {
        public string Key = "";
        public string Sbc = "";
        public string SrcIp = "", DstIp = "";
        public int SrcPort, DstPort;
        public string SessionId = "";    // the Mediant's SID from Debug Recording
        public bool? Inbound;
        public uint Ssrc;
        public int PayloadType = -1;
        public DateTime First, Last;
        public long Packets;
        // RFC 3550 sequence tracking
        public bool SeqInit;
        public ushort MaxSeq;
        public uint Cycles, BaseSeq;
        // jitter (RFC 3550), in RTP clock units
        public double Jitter;
        public bool HaveTransit;
        public double LastTransit;
        public double MaxGapMs;
        public double LastArrivalMs = -1;
        public uint LastDeviceTs;
        public double DeviceMsBase;
        // packets per second, for the audio timeline (up to two hours)
        public long FirstSecond;
        public double FirstArrivalMs = -1;
        public List<int> PerSecond = [];
        public List<int> PerSecondLost = [];
        public List<double> PerSecondJitter = [];
        public List<DtmfDigit> Digits = [];

        public sealed class DtmfDigit
        {
            public DateTime Time;
            public string Digit = "";
            public int DurationMs;
            public uint Timestamp;
            public bool Ended;
        }

        public double LossPercent => Expected > 0 ? 100.0 * Lost / Expected : 0;
        public double JitterMs => HaveTransit ? Jitter / 8.0 : 0;
        public double? Mos => Packets >= 25 ? Quality.Mos(LossPercent, JitterMs) : null;

        public long Expected => SeqInit ? (long)(Cycles + MaxSeq) - BaseSeq + 1 : 0;
        public long Lost => Math.Max(0, Expected - Packets);
    }

    /// <summary>One syslog line. <see cref="Id"/> numbers lines in arrival order, so the page can mark one and find it again.</summary>
    public sealed record SyslogLine(long Id, DateTime Time, string Source, string Text);

    private long _syslogSeq;
    private int _syslogCapacity = MaxSyslog;

    /// <summary>How many syslog lines are kept for the page (Settings → syslog buffer).</summary>
    public int SyslogCapacity
    {
        get => _syslogCapacity;
        set { lock (_gate) { _syslogCapacity = Math.Clamp(value, 1000, 1_000_000); while (_syslog.Count > _syslogCapacity) _syslog.RemoveFirst(); } }
    }

    // ---------------------------------------------------------------- input

    public void AddSip(DateTime time, SipText.Message m, string source, string src, string dst, bool? inbound, string sessionId = "", string sbc = "")
    {
        if (m.CallId.Length == 0) return;
        lock (_gate)
        {
            string callKey = $"{sbc}|{m.CallId}";
            if (!_calls.TryGetValue(callKey, out Call? call))
            {
                call = new Call { Id = _nextCallId++, Key = callKey, Sbc = sbc, CallId = m.CallId, Started = time };
                _calls[callKey] = call;
                _order.Add(call);
                if (_order.Count > MaxCalls)
                {
                    _calls.Remove(_order[0].Key);
                    _order.RemoveAt(0);
                }
            }

            // The same message seen by both Debug Recording and syslog: keep the first.
            bool duplicate = call.Messages.Any(e => e.Source != source
                && e.Message.FirstLine == m.FirstLine && e.Message.CSeq == m.CSeq
                && Math.Abs((e.Time - time).TotalSeconds) < 5
                && (e.Inbound is null || inbound is null || e.Inbound == inbound));
            if (duplicate) return;

            if (call.Messages.Count >= MaxMessagesPerCall) { call.DroppedMessages++; return; }

            var entry = new SipEntry { Time = time, Source = source, Src = src, Dst = dst, Inbound = inbound, Message = m };
            call.Messages.Add(entry);
            call.LastSeen = time;
            if (call.SessionId.Length == 0 && sessionId.Length > 0)
            {
                call.SessionId = sessionId;
                // DNS look-ups and HTTP requests for this session can come before its first SIP message.
                if (_pendingOthers.Remove($"{sbc}|{sessionId}", out var early)) call.Others.AddRange(early);
            }
            if (call.From.Length == 0) call.From = UserPart(m.From);
            if (call.To.Length == 0) call.To = UserPart(m.To);
            UpdateState(call, m, time);

            if (m.Sdp is not null)
            {
                string owner = src.Length > 0 ? HostOf(src) : "";
                call.Sdps.Add(new SdpEntry
                {
                    Time = time,
                    MessageIndex = call.Messages.Count - 1,
                    Owner = owner,
                    OwnerIsMediant = inbound is null ? null : !inbound,
                    Sdp = m.Sdp,
                    Label = m.IsRequest ? m.Method! : $"{m.Status} ({m.CSeqMethod})",
                });
            }
        }
    }

    // Non-SIP messages waiting for a call with their SID, by "sbc|sid".
    private readonly Dictionary<string, List<OtherEntry>> _pendingOthers = new();

    /// <summary>An HTTP, DNS or VoiceAI message the SBC logged for a session. Shown in the ladder of the call with that SID.</summary>
    public void AddOther(DateTime time, string kind, string firstLine, string text, bool? inbound, string peer, string sessionId, string sbc)
    {
        if (sessionId.Length == 0) return;
        var entry = new OtherEntry { Time = time, Kind = kind, Peer = peer, Inbound = inbound, FirstLine = firstLine, Text = text.Length > 20000 ? text[..20000] : text };
        lock (_gate)
        {
            var calls = _order.Where(c => c.Sbc == sbc && c.SessionId == sessionId).ToList();
            if (calls.Count > 0)
            {
                // One copy, on the leg the message most likely belongs to: the newest leg of the session.
                Call call = calls[^1];
                if (call.Others.Count < MaxMessagesPerCall) call.Others.Add(entry);
                return;
            }
            string key = $"{sbc}|{sessionId}";
            if (!_pendingOthers.TryGetValue(key, out var list))
            {
                if (_pendingOthers.Count >= 2000)
                    foreach (string old in _pendingOthers.Where(p => p.Value.Count == 0 || p.Value[0].Time < time.AddMinutes(-10)).Select(p => p.Key).ToList())
                        _pendingOthers.Remove(old);
                if (_pendingOthers.Count >= 2000) _pendingOthers.Clear();
                _pendingOthers[key] = list = [];
            }
            if (list.Count < 50) list.Add(entry);
        }
    }

    private static void UpdateState(Call call, SipText.Message m, DateTime time)
    {
        if (m.IsRequest)
        {
            if (m.Method is "BYE" or "CANCEL") { call.State = m.Method == "BYE" ? "ended" : "cancelled"; call.Ended ??= time; }
            return;
        }
        if (m.CSeqMethod != "INVITE") return;
        if (m.Status is >= 180 and < 200 && call.State == "trying") call.State = "ringing";
        else if (m.Status is >= 200 and < 300 && call.Answered is null)
        {
            call.Answered = time;
            call.FinalStatus = m.Status;
            if (call.State is "trying" or "ringing") call.State = "connected";
        }
        else if (m.Status >= 300 && call.Answered is null)
        {
            call.FinalStatus = m.Status;
            // A 487 after the caller's CANCEL is the end of a cancelled call, not a failure.
            if (!(call.State == "cancelled" && m.Status == 487)) call.State = "failed";
            call.Ended ??= time;
        }
    }

    public void AddRtp(DateTime time, uint deviceTs, bool haveDeviceTs, string srcIp, int srcPort, string dstIp, int dstPort,
                       bool? inbound, ReadOnlySpan<byte> rtp, string sbc = "", string sessionId = "")
    {
        if (rtp.Length < 12 || rtp[0] >> 6 != 2) return;
        byte pt = (byte)(rtp[1] & 0x7F);
        if (rtp[1] is >= 200 and <= 204) return; // RTCP
        ushort seq = BinaryPrimitives.ReadUInt16BigEndian(rtp[2..]);
        uint ts = BinaryPrimitives.ReadUInt32BigEndian(rtp[4..]);
        uint ssrc = BinaryPrimitives.ReadUInt32BigEndian(rtp[8..]);
        string key = $"{sbc}|{srcIp}:{srcPort}>{dstIp}:{dstPort}#{ssrc:x8}";

        lock (_gate)
        {
            if (!_streams.TryGetValue(key, out RtpStream? s))
            {
                if (_streams.Count >= MaxStreams && !EvictStreams(time)) return;
                s = new RtpStream
                {
                    Key = key, Sbc = sbc, SrcIp = srcIp, SrcPort = srcPort, DstIp = dstIp, DstPort = dstPort,
                    Inbound = inbound, Ssrc = ssrc, PayloadType = pt, First = time, SessionId = sessionId,
                };
                _streams[key] = s;
            }
            if (s.SessionId.Length == 0 && sessionId.Length > 0) s.SessionId = sessionId;
            bool dtmf = TelephoneEvent(sbc, pt);
            if (!dtmf && pt < 96 && s.PayloadType >= 96) s.PayloadType = pt;
            s.Last = time;
            if (dtmf) NoteDigit(s, time, ts, rtp);

            // Arrival time in ms: the device's own clock when Debug Recording gives one.
            double arrivalMs;
            if (haveDeviceTs)
            {
                if (s.Packets > 0 && deviceTs < s.LastDeviceTs && s.LastDeviceTs - deviceTs > 0x80000000u)
                    s.DeviceMsBase += 4294967.296; // 2^32 microseconds
                s.LastDeviceTs = deviceTs;
                arrivalMs = s.DeviceMsBase + deviceTs / 1000.0;
            }
            else
            {
                arrivalMs = (time - DateTime.UnixEpoch).TotalMilliseconds;
            }

            if (s.LastArrivalMs >= 0) s.MaxGapMs = Math.Max(s.MaxGapMs, arrivalMs - s.LastArrivalMs);
            s.LastArrivalMs = arrivalMs;
            s.Packets++;

            // Timeline slots follow the SBC's own clock (Debug Recording timestamps) when there is one,
            // so a gap shows where it happened even if packets reached us in bursts.
            if (s.FirstArrivalMs < 0)
            {
                s.FirstArrivalMs = arrivalMs;
                s.FirstSecond = (long)(time - DateTime.UnixEpoch).TotalSeconds;
            }
            long slot = (long)Math.Floor((arrivalMs - s.FirstArrivalMs) / 1000.0);
            bool inRange = slot is >= 0 and < 7200;
            if (inRange)
            {
                while (s.PerSecond.Count <= slot) { s.PerSecond.Add(0); s.PerSecondLost.Add(0); s.PerSecondJitter.Add(0); }
                s.PerSecond[(int)slot]++;
            }

            if (!s.SeqInit) { s.SeqInit = true; s.BaseSeq = seq; s.MaxSeq = seq; }
            else
            {
                ushort delta = (ushort)(seq - s.MaxSeq);
                if (delta < 3000)
                {
                    if (delta > 1 && inRange)
                    {
                        // The packets skipped over were due during the gap: spread them back over the
                        // seconds with no packets, about 50 a second, rather than piling them on this one.
                        int lost = delta - 1, k = (int)slot - 1;
                        while (lost > 0 && k >= 0 && s.PerSecond[k] == 0) { int put = Math.Min(50, lost); s.PerSecondLost[k] += put; lost -= put; k--; }
                        s.PerSecondLost[(int)slot] += lost;
                    }
                    if (seq < s.MaxSeq) s.Cycles += 65536;
                    s.MaxSeq = seq;
                }
            }

            // Jitter only where the clock rate is known for certain (static payload types).
            if (pt == s.PayloadType && pt < 96)
            {
                double transit = arrivalMs * 8.0 - ts; // 8 kHz clock: 8 units per ms
                if (s.HaveTransit)
                {
                    double d = Math.Abs(transit - s.LastTransit);
                    if (d < 8000) s.Jitter += (d - s.Jitter) / 16.0;
                }
                s.LastTransit = transit;
                s.HaveTransit = true;
                if (inRange) s.PerSecondJitter[(int)slot] = Math.Round(s.Jitter / 8.0, 1);
            }
        }
    }

    private bool TelephoneEvent(string sbc, byte pt)
    {
        foreach (Call call in _calls.Values)
        {
            if (sbc.Length > 0 && call.Sbc != sbc) continue;
            foreach (SdpEntry entry in call.Sdps)
                if (entry.Sdp.TelephoneEvents.Contains(pt)) return true;
        }
        return false;
    }

    /// <summary>RFC 2833: the same digit is repeated until the end bit. Keep one row, and its length.</summary>
    private static void NoteDigit(RtpStream stream, DateTime time, uint timestamp, ReadOnlySpan<byte> rtp)
    {
        int offset = 12 + (rtp[0] & 0x0F) * 4;
        if ((rtp[0] & 0x10) != 0)
        {
            if (rtp.Length < offset + 4) return;
            offset += 4 + BinaryPrimitives.ReadUInt16BigEndian(rtp[(offset + 2)..]) * 4;
        }
        int end = rtp.Length;
        if ((rtp[0] & 0x20) != 0 && end > offset) end -= rtp[end - 1];
        for (int i = offset; i + 4 <= end; i += 4)
        {
            byte ev = rtp[i];
            if (ev > 16) continue;
            string digit = ev switch { <= 9 => ev.ToString(), 10 => "*", 11 => "#", 12 => "A", 13 => "B", 14 => "C", 15 => "D", _ => "Flash" };
            bool ended = (rtp[i + 1] & 0x80) != 0;
            int ms = BinaryPrimitives.ReadUInt16BigEndian(rtp[(i + 2)..]) / 8;
            RtpStream.DtmfDigit? row = stream.Digits.LastOrDefault(d => d.Timestamp == timestamp && d.Digit == digit);
            if (row is null)
            {
                if (stream.Digits.Count >= 100) return;
                stream.Digits.Add(new RtpStream.DtmfDigit { Time = time, Digit = digit, DurationMs = ms, Timestamp = timestamp, Ended = ended });
            }
            else
            {
                if (ms > row.DurationMs) row.DurationMs = ms;
                if (ended) row.Ended = true;
            }
        }
    }

    public void AddCdr(string sbc, CdrDecoder.Record r)
    {
        string callId = r.Get("SIPCallId");
        if (callId.Length == 0) return;
        lock (_gate)
        {
            string key = $"{sbc}|{callId}";
            if (!_calls.TryGetValue(key, out Call? call))
            {
                // The call began before this capture, or SIP is not logged: build it from the record.
                long dur = Math.Max(0, r.Num("Durat"));
                call = new Call
                {
                    Id = _nextCallId++, Key = key, Sbc = sbc, CallId = callId,
                    Started = r.Received.AddSeconds(-dur), LastSeen = r.Received,
                    From = UserPart(r.Get("SrcPhoneNum", "Caller", "SrcURI")),
                    To = UserPart(r.Get("DstPhoneNum", "Callee", "DstURI")),
                    SessionId = r.Get("SessionId"),
                    State = r.Kind is "CALL_END" or "MEDIA_END" ? "ended" : "connected",
                };
                if (dur > 0) call.Answered = call.Started;
                _calls[key] = call;
                _order.Add(call);
                if (_order.Count > MaxCalls) { _calls.Remove(_order[0].Key); _order.RemoveAt(0); }
            }
            if (call.Cdrs.Count < 40) call.Cdrs.Add(r);
            call.LastSeen = r.Received;
            if (call.SessionId.Length == 0) call.SessionId = r.Get("SessionId");
            if (r.Kind is "CALL_END" or "MEDIA_END")
            {
                call.Ended ??= r.Received;
                if (r.Kind == "CALL_END" && call.State is "connected" or "ringing" or "trying")
                    call.State = call.Answered is null ? "failed" : "ended";
                else if (r.Kind == "MEDIA_END" && call.State is "connected" or "trying")
                    call.State = "ended";
            }
        }
    }

    /// <summary>A packet that was TLS or DTLS, so the SIP or the audio in it could not be read.</summary>
    public void NoteSealed(DateTime time, string sbc, string kind)
    {
        lock (_gate)
        {
            if (_sealed.Count >= 20000) _sealed.RemoveAt(0);
            _sealed.Add(new SealedPacket(time, sbc, kind));
        }
    }

    public void AddSyslog(DateTime time, string source, string text)
    {
        lock (_gate)
        {
            _syslog.AddLast(new SyslogLine(++_syslogSeq, time, source, text));
            if (_syslog.Count > _syslogCapacity) _syslog.RemoveFirst();
        }
    }

    /// <summary>Empties the syslog buffer only (the page's Clean), keeping calls and streams.</summary>
    public void ClearSyslog()
    {
        lock (_gate) _syslog.Clear();
    }

    public int Count { get { lock (_gate) return _order.Count; } }

    public void Clear()
    {
        lock (_gate)
        {
            _calls.Clear();
            _order.Clear();
            _streams.Clear();
            _syslog.Clear();
            _sealed.Clear();
            _pendingOthers.Clear();
            _nextCallId = 1;
            _syslogSeq = 0;
        }
    }

    // ---------------------------------------------------------------- output

    public object CallList(DateTime now, string? sbc = null, bool includeOptions = false)
    {
        lock (_gate)
        {
            return _order.AsEnumerable().Reverse().Where(c => (string.IsNullOrEmpty(sbc) || c.Sbc == sbc) && (includeOptions || !c.IsOptions)).Select(c =>
            {
                var callStreams = StreamsFor(c);
                var media = MediaFor(c, callStreams);
                var quality = SbcQualityFor(c);
                var (mos, mosSource) = HeadlineMos(callStreams, quality);
                var warnings = Checks(c, media, now);
                var issues = warnings.Where(w => w.Level is "bad" or "warn").ToList();
                return new
                {
                    id = c.Id,
                    sbc = c.Sbc,
                    callId = c.CallId,
                    session = c.SessionId,
                    started = c.Started,
                    from = c.From,
                    to = c.To,
                    state = c.State,
                    status = c.FinalStatus,
                    seconds = Duration(c, now),
                    messages = c.Messages.Count,
                    audioIn = media.Sum(m => m.FromPeer),
                    audioOut = media.Sum(m => m.ToPeer),
                    warnings = issues.Count,
                    topIssue = issues.OrderBy(w => w.Level == "bad" ? 0 : 1).FirstOrDefault()?.Text ?? "",
                    oneWay = issues.Any(w => w.Text.StartsWith("One-way", StringComparison.Ordinal)),
                    codec = c.Sdps.LastOrDefault(x => x.OwnerIsMediant != true)?.Sdp.Codecs.FirstOrDefault() ?? "",
                    mos,
                    mosSource,
                    sbcIn = LastEnd(c)?.Num("InPackets") ?? -1,
                    sbcOut = LastEnd(c)?.Num("OutPackets") ?? -1,
                    cdr = c.Cdrs.Count > 0,
                    ended = c.Ended,
                    severity = issues.Any(w => w.Level == "bad") ? "bad" : issues.Count > 0 ? "warn" : "ok",
                };
            }).ToList();
        }
    }

    public object? CallDetail(int id, DateTime now)
    {
        lock (_gate)
        {
            Call? c = _order.FirstOrDefault(x => x.Id == id);
            if (c is null) return null;
            var streams = StreamsFor(c);
            var media = MediaFor(c, streams);
            var quality = SbcQualityFor(c);
            var (mos, mosSource) = HeadlineMos(streams, quality);
            return new
            {
                id = c.Id,
                sbc = c.Sbc,
                callId = c.CallId,
                session = c.SessionId,
                otherLegs = _order.Where(o => o != c && o.Sbc == c.Sbc && o.SessionId.Length > 0 && o.SessionId == c.SessionId).Select(o => new { id = o.Id, from = o.From, to = o.To, state = o.State }).ToList(),
                from = c.From,
                to = c.To,
                state = c.State,
                status = c.FinalStatus,
                started = c.Started,
                answered = c.Answered,
                ended = c.Ended,
                seconds = Duration(c, now),
                droppedMessages = c.DroppedMessages,
                checks = Checks(c, media, now),
                signalling = SignallingOf(c),
                mediaProtection = MediaProtectionOf(c),
                mos,
                mosSource,
                messages = c.Messages.Select((e, i) => new
                {
                    index = i,
                    time = e.Time,
                    source = e.Source,
                    src = e.Src,
                    dst = e.Dst,
                    inbound = e.Inbound,
                    first = e.Message.FirstLine,
                    hasSdp = e.Message.Sdp is not null,
                    transport = e.Message.Transport,
                    text = e.Message.Text,
                    kind = "SIP",
                }).Concat(c.Others.Select((o, i) => new
                {
                    index = c.Messages.Count + i,
                    time = o.Time,
                    source = "SYSLOG",
                    src = o.Inbound == true ? o.Peer : "",
                    dst = o.Inbound == false ? o.Peer : "",
                    inbound = o.Inbound,
                    first = o.FirstLine,
                    hasSdp = false,
                    transport = "",
                    text = o.Text,
                    kind = o.Kind,
                })).OrderBy(m => m.time).ToList(),
                sdps = c.Sdps.Select(s => new
                {
                    time = s.Time,
                    label = s.Label,
                    owner = s.Owner,
                    ownerIsMediant = s.OwnerIsMediant,
                    ip = s.Sdp.ConnectionIp,
                    port = s.Sdp.AudioPort,
                    direction = s.Sdp.Direction,
                    profile = s.Sdp.Profile,
                    protection = s.Sdp.Protection,
                    codecs = s.Sdp.Codecs.ToList(),
                    messageIndex = s.MessageIndex,
                }).ToList(),
                media,
                sbcMedia = quality.Select(q => new
                {
                    kind = q.Kind,
                    codec = q.Codec,
                    remoteIp = q.RemoteIp,
                    remotePort = q.RemotePort,
                    latchedIp = q.LatchedIp,
                    latchedPort = q.LatchedPort,
                    inPackets = q.InPackets < 0 ? (long?)null : q.InPackets,
                    outPackets = q.OutPackets < 0 ? (long?)null : q.OutPackets,
                    inLost = q.InLost < 0 ? (long?)null : q.InLost,
                    outLost = q.OutLost < 0 ? (long?)null : q.OutLost,
                    inLoss = q.InLoss,
                    outLoss = q.OutLoss,
                    jitterMs = q.JitterMs,
                    remoteJitterMs = q.RemoteJitterMs,
                    delayMs = q.DelayMs,
                    mosIn = q.MosIn,
                    mosOut = q.MosOut,
                    mosFromSbc = q.MosFromSbc,
                }).ToList(),
                streams = streams.Select(x => StreamView(x, true, MediaProtectionOf(c).Length > 0)).ToList(),
                dtmf = streams.SelectMany(s => s.Digits.Select(d => new
                {
                    time = d.Time,
                    digit = d.Digit,
                    durationMs = d.DurationMs,
                    ended = d.Ended,
                    src = s.SrcIp.Length > 0 ? $"{s.SrcIp}:{s.SrcPort}" : "",
                    dst = s.DstIp.Length > 0 ? $"{s.DstIp}:{s.DstPort}" : "",
                    inbound = s.Inbound,
                })).OrderBy(x => x.time).ToList(),
                cdrs = c.Cdrs.Select(r => new { kind = r.Kind, family = r.Family, received = r.Received, fields = r.Fields.Select(f => new { k = f.Key, v = f.Value }).ToList() }).ToList(),
            };
        }
    }

    /// <summary>What to look for in a capture when saving one call's packets.</summary>
    public sealed class PacketSelect
    {
        public string Sbc = "";
        public List<string> CallIds = [];
        public string SessionId = "";
        public DateTime From, To;
        public HashSet<string> Endpoints = new(StringComparer.Ordinal);
        public string FileName = "call.pcap";
    }

    public PacketSelect? PacketSelectFor(int id, DateTime now)
    {
        lock (_gate)
        {
            Call? c = _order.FirstOrDefault(x => x.Id == id);
            if (c is null) return null;
            var legs = _order.Where(o => o == c || (c.SessionId.Length > 0 && o.Sbc == c.Sbc && o.SessionId == c.SessionId)).ToList();
            DateTime end = legs.Max(o => o.Ended ?? (o.LastSeen > now ? o.LastSeen : now));
            var select = new PacketSelect
            {
                Sbc = c.Sbc,
                CallIds = legs.Select(o => o.CallId).Where(x => x.Length > 0).Distinct().ToList(),
                SessionId = c.SessionId,
                From = legs.Min(o => o.Started).AddSeconds(-5),
                To = end.AddSeconds(30),
                FileName = PacketFileName(c),
            };
            foreach (Call leg in legs)
            {
                foreach (SdpEntry s in leg.Sdps.Where(s => s.Sdp.AudioPort > 0))
                    select.Endpoints.Add($"{s.Sdp.ConnectionIp}:{s.Sdp.AudioPort}");
                foreach (RtpStream stream in StreamsFor(leg))
                {
                    if (stream.SrcIp.Length > 0 && stream.SrcPort > 0) select.Endpoints.Add($"{stream.SrcIp}:{stream.SrcPort}");
                    if (stream.DstIp.Length > 0 && stream.DstPort > 0) select.Endpoints.Add($"{stream.DstIp}:{stream.DstPort}");
                }
            }
            return select;
        }
    }

    private static string PacketFileName(Call c)
    {
        string from = PacketStem(c.From), to = PacketStem(c.To);
        string stem = from.Length > 0 || to.Length > 0 ? $"{from}-to-{to}" : "call-" + c.Id;
        if (stem.Length > 80) stem = stem[..80];
        return stem + ".pcap";
    }

    private static string PacketStem(string value)
    {
        var stem = new System.Text.StringBuilder();
        foreach (char ch in value)
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_') stem.Append(ch);
        return stem.ToString();
    }

    public string CallText(int id)
    {
        lock (_gate)
        {
            Call? c = _order.FirstOrDefault(x => x.Id == id);
            if (c is null) return "";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"SBC {c.Sbc}   Call-ID {c.CallId}   {c.From} -> {c.To}   {c.State} {c.FinalStatus}");
            foreach (var e in c.Messages)
            {
                sb.AppendLine();
                string dir = e.Inbound switch { true => "IN ", false => "OUT", null => "   " };
                sb.AppendLine($"===== {e.Time.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} {dir} {e.Src} -> {e.Dst} ({e.Source})");
                sb.AppendLine(e.Message.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }
            return sb.ToString();
        }
    }

    public object Streams(string? sbc = null)
    {
        lock (_gate)
        {
            var owned = new HashSet<string>(_order.SelectMany(c => StreamsFor(c)).Select(s => s.Key));
            return _streams.Values.Where(s => string.IsNullOrEmpty(sbc) || s.Sbc == sbc).OrderByDescending(s => s.Last).Select(s => new
            {
                sbc = s.Sbc,
                stream = StreamView(s),
                matched = owned.Contains(s.Key),
            }).ToList();
        }
    }

    public object SyslogLines(string? query, int limit, string? sbc = null, bool errorsOnly = false)
    {
        lock (_gate)
        {
            IEnumerable<SyslogLine> lines = _syslog.Reverse();
            if (errorsOnly) lines = lines.Where(l => Severity(l.Text) is >= 0 and <= 4); // warnings and worse
            if (!string.IsNullOrEmpty(sbc)) lines = lines.Where(l => l.Source == sbc);
            if (!string.IsNullOrWhiteSpace(query))
                lines = lines.Where(l => l.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
            return lines.Take(Math.Clamp(limit, 1, 5000)).Select(l => new { id = l.Id, time = l.Time, source = l.Source, text = l.Text }).ToList();
        }
    }

    /// <summary>What the syslog viewer asks for. Text is a word or a regular expression; Exclude turns the match round.</summary>
    public sealed class SyslogQuery
    {
        public string Text = "";
        public bool Regex, CaseSensitive, WholeWord, Exclude;
        public string Sbc = "";
        public string Level = "";        // "", "errors" (0-3), "warnings" (4), "problems" (0-4)
        public string Session = "";      // only lines with this [SID=..]
        public string Kind = "";         // "", "cdr" (SBC call records), "calls" (lines with a session), "noncalls" (lines without one)
        public long After;               // only lines after this id (for a live view that appends)
        public int Limit = 2000;
    }

    /// <summary>
    /// The syslog viewer's display filter. Returns the newest matching lines (oldest first), how many lines
    /// the buffer holds, and how many matched (up to a cap). An invalid regular expression is reported, not thrown.
    /// </summary>
    public object SyslogView(SyslogQuery q)
    {
        System.Text.RegularExpressions.Regex? rx = null;
        string error = "";
        if (q.Text.Length > 0 && (q.Regex || q.WholeWord))
        {
            string pattern = q.Regex ? q.Text : System.Text.RegularExpressions.Regex.Escape(q.Text);
            if (q.WholeWord) pattern = $@"\b(?:{pattern})\b";
            try
            {
                rx = new System.Text.RegularExpressions.Regex(pattern,
                    (q.CaseSensitive ? 0 : System.Text.RegularExpressions.RegexOptions.IgnoreCase) | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (ArgumentException e) { error = "That regular expression is not valid: " + e.Message; }
        }
        var comparison = q.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        bool Hit(string text)
        {
            if (q.Text.Length == 0 || error.Length > 0) return true;
            bool found;
            try { found = rx is not null ? rx.IsMatch(text) : text.Contains(q.Text, comparison); }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException) { found = false; }
            return found != q.Exclude;
        }
        string sidTag = q.Session.Length > 0 ? q.Session : "";
        int limit = Math.Clamp(q.Limit, 1, 20000);
        lock (_gate)
        {
            var picked = new List<SyslogLine>();
            int total = _syslog.Count;
            for (var node = _syslog.Last; node is not null && picked.Count < limit; node = node.Previous)
            {
                SyslogLine l = node.Value;
                if (l.Id <= q.After) break;
                if (q.Sbc.Length > 0 && l.Source != q.Sbc) continue;
                if (q.Level.Length > 0)
                {
                    int sev = Severity(l.Text);
                    bool keep = q.Level switch { "errors" => sev is >= 0 and <= 3, "warnings" => sev == 4, "problems" => sev is >= 0 and <= 4, _ => true };
                    if (!keep) continue;
                }
                if (sidTag.Length > 0 && !l.Text.Contains("SID=" + sidTag, StringComparison.Ordinal) && !l.Text.Contains("SID:" + sidTag, StringComparison.Ordinal)) continue;
                if (q.Kind == "cdr" && !CdrDecoder.LooksLikeRecord(l.Text)) continue;
                if (q.Kind == "calls" && !HasSession(l.Text)) continue;
                if (q.Kind == "noncalls" && HasSession(l.Text)) continue;
                if (!Hit(l.Text)) continue;
                picked.Add(l);
            }
            picked.Reverse();
            return new
            {
                total,
                first = _syslog.First?.Value.Id ?? 0,
                last = _syslog.Last?.Value.Id ?? 0,
                capacity = _syslogCapacity,
                limited = picked.Count >= limit,
                error,
                lines = picked.Select(l => new { id = l.Id, time = l.Time, source = l.Source, text = l.Text }).ToList(),
            };
        }
    }

    private static bool HasSession(string text) =>
        text.Contains("[SID=", StringComparison.Ordinal) || text.Contains("[SID:", StringComparison.Ordinal) || text.Contains("SessionId", StringComparison.Ordinal);

    /// <summary>Every syslog line in the buffer as plain text, oldest first (the page's Save As).</summary>
    public string SyslogText(string? sbc, bool localTime)
    {
        var sb = new System.Text.StringBuilder();
        lock (_gate)
        {
            foreach (SyslogLine l in _syslog)
            {
                if (!string.IsNullOrEmpty(sbc) && l.Source != sbc) continue;
                sb.Append(SyslogFileLog.Format(l.Time, l.Source, l.Text, localTime)).Append("\r\n");
            }
        }
        return sb.ToString();
    }

    /// <summary>Syslog severity from the &lt;PRI&gt; prefix (0 emergency ... 3 error ... 7 debug), or -1.</summary>
    /// <summary>Syslog lines at error or worse (severity 0-3) since a time, with the newest few as examples.</summary>
    public (int count, List<string> examples) SyslogErrorsSince(DateTime since, string? sbc = null)
    {
        lock (_gate)
        {
            var hits = _syslog.Where(l => l.Time >= since && (sbc is null || l.Source == sbc) && Severity(l.Text) is >= 0 and <= 3).ToList();
            return (hits.Count, hits.TakeLast(3).Select(l => l.Text.Length > 160 ? l.Text[..160] + "…" : l.Text).ToList());
        }
    }

    public static int Severity(string line)
    {
        if (line.Length < 3 || line[0] != '<') return -1;
        int end = line.IndexOf('>');
        return end > 1 && int.TryParse(line.AsSpan(1, end - 1), out int pri) ? pri & 7 : -1;
    }

    private static CdrDecoder.Record? LastEnd(Call c) => c.Cdrs.LastOrDefault(r => r.Kind == "CALL_END");

    /// <summary>Audio on one Mediant session (one call leg), by direction.</summary>
    public sealed record LegMedia(string Session, long ToSbc, long FromSbc, double? Mos, double LossPercent);

    /// <summary>
    /// Audio totals for the Mediant sessions given. Streams carry the session ID from
    /// Debug Recording, so this works for calls set up before the capture started too.
    /// </summary>
    public List<LegMedia> SessionMedia(string sbc, IEnumerable<string> sessions)
    {
        lock (_gate)
        {
            return sessions.Where(x => x.Length > 0).Distinct().Select(sid =>
            {
                var st = _streams.Values.Where(s => s.Sbc == sbc && s.SessionId == sid).ToList();
                return new LegMedia(sid,
                    st.Where(s => s.Inbound == true).Sum(s => s.Packets),
                    st.Where(s => s.Inbound == false).Sum(s => s.Packets),
                    WorstMos(st), LossPct(st));
            }).ToList();
        }
    }

    /// <summary>
    /// The call view is fed all the time, so the stream table fills up. Make room by
    /// dropping the streams that went quiet longest ago (a tenth at a time), but never
    /// one heard in the last ten minutes. The caller holds <c>_gate</c>.
    /// </summary>
    private bool EvictStreams(DateTime now)
    {
        var old = _streams.Values.Where(s => now - s.Last > TimeSpan.FromMinutes(10))
            .OrderBy(s => s.Last).Take(MaxStreams / 10).Select(s => s.Key).ToList();
        foreach (string k in old) _streams.Remove(k);
        return old.Count > 0;
    }

    /// <summary>Audio per Mediant session for one SBC, in one pass over the stream table (the line board asks for every line at once).</summary>
    public Dictionary<string, LegMedia> MediaBySession(string sbc)
    {
        lock (_gate)
        {
            return _streams.Values.Where(s => s.Sbc == sbc && s.SessionId.Length > 0).GroupBy(s => s.SessionId)
                .ToDictionary(g => g.Key, g => new LegMedia(g.Key,
                    g.Where(s => s.Inbound == true).Sum(s => s.Packets),
                    g.Where(s => s.Inbound == false).Sum(s => s.Packets),
                    WorstMos(g), LossPct(g.ToList())));
        }
    }

    /// <summary>Calls that are up now on one SBC, by calling and called number, in one pass.</summary>
    public Dictionary<string, List<(string session, string from, string to, DateTime? answered)>> LiveCallsByNumber(string sbc)
    {
        var map = new Dictionary<string, List<(string, string, string, DateTime?)>>();
        lock (_gate)
        {
            foreach (var c in _order.Where(c => c.Sbc == sbc && c.Ended is null && c.Answered is not null && !c.IsOptions))
                foreach (string n in new[] { c.From, c.To }.Where(n => n.Length > 0).Distinct())
                {
                    if (!map.TryGetValue(n, out var list)) map[n] = list = [];
                    list.Add((c.SessionId, c.From, c.To, c.Answered));
                }
        }
        return map;
    }

    /// <summary>Calls in the capture that are up now and involve this number.</summary>
    public List<(string session, string from, string to, DateTime? answered)> LiveCallsFor(string sbc, string number)
    {
        lock (_gate)
        {
            return _order.Where(c => c.Sbc == sbc && c.Ended is null && c.Answered is not null && !c.IsOptions
                                     && (c.From == number || c.To == number))
                .Select(c => (c.SessionId, c.From, c.To, c.Answered)).ToList();
        }
    }

    public (int calls, int streams, int syslog) Counts()
    {
        lock (_gate) return (_order.Count(c => !c.IsOptions), _streams.Count, _syslog.Count);
    }

    // ---------------------------------------------------------------- analysis

    private sealed record MediaView(string Peer, string SdpAddress, string Direction, long ToPeer, long FromPeer,
                                    string ActualFrom, long Lost, double JitterMs, double MaxGapMs,
                                    double LossToPct, double LossFromPct, double? MosTo, double? MosFrom);

    private static double LossPct(List<RtpStream> streams)
    {
        long expected = streams.Sum(x => x.Expected);
        return expected > 0 ? Math.Round(100.0 * streams.Sum(x => x.Lost) / expected, 2) : 0;
    }

    private static double? WorstMos(IEnumerable<RtpStream> streams) =>
        streams.Select(x => x.Mos).Where(m => m is not null).DefaultIfEmpty(null).Min();

    /// <summary>Audio the SBC measured itself, from a MEDIA_END record or a gateway CALL_END. Used when Debug Recording did not capture the packets.</summary>
    private sealed record SbcQuality(string Kind, string Codec, string RemoteIp, string RemotePort, string LatchedIp, string LatchedPort,
        long InPackets, long OutPackets, long InLost, long OutLost, double? InLoss, double? OutLoss,
        double? JitterMs, double? RemoteJitterMs, double? DelayMs, double? MosIn, double? MosOut, bool MosFromSbc);

    /// <summary>Packet MOS when the capture has it; otherwise the worst score from the SBC's own record.</summary>
    private static (double? Mos, string Source) HeadlineMos(IEnumerable<RtpStream> streams, List<SbcQuality> quality)
    {
        double? fromPackets = WorstMos(streams);
        if (fromPackets is not null) return (fromPackets, "packets");
        var scores = quality.SelectMany(q => new[] { q.MosIn, q.MosOut }).Where(m => m is not null).Select(m => m!.Value).ToList();
        if (scores.Count == 0) return (null, "");
        return (scores.Min(), quality.Any(q => q.MosFromSbc) ? "sbc" : "counters");
    }

    private static List<SbcQuality> SbcQualityFor(Call c)
    {
        var mediaEnds = c.Cdrs.Where(r => r.Kind == "MEDIA_END").ToList();
        var rows = mediaEnds.Count > 0 ? mediaEnds : c.Cdrs.Where(r => r.Kind == "CALL_END");
        var list = new List<SbcQuality>();
        foreach (var r in rows)
            if (ReadQuality(r) is { } q) list.Add(q);
        return list;
    }

    private static SbcQuality? ReadQuality(CdrDecoder.Record r)
    {
        long inP = r.Num("InPackets", "RxPackets");
        long outP = r.Num("OutPackets", "TxPackets");
        long inLost = r.Num("PackLoss", "LocalPackLoss", "LocalPacketLoss");
        long outLost = r.Num("RemotePackLoss", "RemotePacketLoss");
        double? jitter = CdrNumber(r, "RTPjitter", "RTPJitter", "LocalRTPjitter");
        double? remoteJitter = CdrNumber(r, "RemoteRTPjitter", "TxRTPjitter");
        double? delay = CdrNumber(r, "RTPdelay", "RTPDelay", "Latency");
        double? mosInGiven = ReportedMos(r, "LocalMOSCQ", "LocalMOS", "LocalMOSLQ", "MOS");
        double? mosOutGiven = ReportedMos(r, "RemoteMOSCQ", "RemoteMOS", "RemoteMOSLQ");
        if (inP < 25 && outP < 25 && mosInGiven is null && mosOutGiven is null) return null;
        return new SbcQuality(
            r.Kind,
            r.Get("Coder"),
            r.Get("RemoteRtpIp", "RtpIp"),
            r.Get("RemoteRtpPort", "Port"),
            r.Get("LatchedRtpIp", "LatchedRTPIp"),
            r.Get("LatchedRtpPort", "LatchedRTPPort"),
            inP, outP, inLost, outLost,
            LossOf(inP, inLost), LossOf(outP, outLost),
            jitter, remoteJitter, delay,
            mosInGiven ?? EstimatedMos(inP, inLost, jitter, delay),
            mosOutGiven ?? EstimatedMos(outP, outLost, remoteJitter ?? jitter, delay),
            mosInGiven is not null || mosOutGiven is not null);
    }

    private static double? LossOf(long packets, long lost)
    {
        if (packets < 0 || lost < 0) return null;
        long total = packets + lost;
        return total > 0 ? Math.Round(100.0 * lost / total, 2) : null;
    }

    private static double? EstimatedMos(long packets, long lost, double? jitterMs, double? delayMs)
    {
        if (packets < 0) return null;
        long loss = Math.Max(0, lost);
        if (packets + loss < 25) return null;
        double pct = packets + loss == 0 ? 0 : 100.0 * loss / (packets + loss);
        return Quality.Mos(pct, jitterMs ?? 0, delayMs ?? 20);
    }

    /// <summary>AudioCodes often sends MOS times ten: 43 means 4.3. A value already between 1 and 5 is used as it is.</summary>
    private static double? ReportedMos(CdrDecoder.Record r, params string[] names)
    {
        string raw = r.Get(names);
        if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) || v <= 0)
            return null;
        if (v > 5 && v <= 50) v /= 10.0;
        return v is >= 1 and <= 5 ? Math.Round(v, 2) : null;
    }

    private static double? CdrNumber(CdrDecoder.Record r, params string[] names)
    {
        string raw = r.Get(names);
        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) && v >= 0 ? v : null;
    }

    private static object StreamView(RtpStream s) => StreamView(s, false, false);

    private static object StreamView(RtpStream s, bool timeline, bool encrypted = false) => new
    {
        startSecond = timeline ? s.FirstSecond : 0,
        perSecond = timeline ? s.PerSecond.ToList() : null,
        perSecondLost = timeline ? s.PerSecondLost.ToList() : null,
        perSecondJitter = timeline ? s.PerSecondJitter.ToList() : null,
        mos = s.Mos,
        src = s.SrcIp.Length > 0 ? $"{s.SrcIp}:{s.SrcPort}" : $"(SBC):{s.SrcPort}",
        dst = s.DstIp.Length > 0 ? $"{s.DstIp}:{s.DstPort}" : $"(SBC):{s.DstPort}",
        inbound = s.Inbound,
        ssrc = s.Ssrc.ToString("x8"),
        payloadType = s.PayloadType,
        packets = s.Packets,
        lost = s.Lost,
        lossPercent = s.Expected > 0 ? Math.Round(100.0 * s.Lost / s.Expected, 2) : 0,
        jitterMs = s.HaveTransit ? Math.Round(s.Jitter / 8.0, 1) : (double?)null,
        maxGapMs = Math.Round(s.MaxGapMs, 0),
        encrypted,
        first = s.First,
        last = s.Last,
    };

    private List<RtpStream> StreamsFor(Call c)
    {
        if (c.Sdps.Count == 0) return [];
        var endpoints = new HashSet<string>(c.Sdps.Where(s => s.Sdp.AudioPort > 0).Select(s => $"{s.Sdp.ConnectionIp}:{s.Sdp.AudioPort}"));
        var ports = new HashSet<int>(c.Sdps.Where(s => s.Sdp.AudioPort > 0).Select(s => s.Sdp.AudioPort));
        DateTime from = c.Started.AddSeconds(-2);
        DateTime to = (c.Ended ?? c.LastSeen).AddSeconds(c.Ended is null ? 3600 : 5);

        return _streams.Values.Where(s =>
            s.Sbc == c.Sbc &&
            s.Last >= from && s.First <= to &&
            (endpoints.Contains($"{s.SrcIp}:{s.SrcPort}") || endpoints.Contains($"{s.DstIp}:{s.DstPort}")
             || (s.SrcIp.Length == 0 && ports.Contains(s.SrcPort))
             || (s.DstIp.Length == 0 && ports.Contains(s.DstPort))))
            .OrderBy(s => s.First).ToList();
    }

    /// <summary>For every far end that offered or answered audio: what was sent to it and what came back.</summary>
    private static List<MediaView> MediaFor(Call c, List<RtpStream> streams)
    {
        var views = new List<MediaView>();
        foreach (var group in c.Sdps.Where(s => s.OwnerIsMediant != true && s.Sdp.AudioPort > 0)
                     .GroupBy(s => $"{s.Sdp.ConnectionIp}:{s.Sdp.AudioPort}"))
        {
            SdpEntry last = group.Last();
            string ip = last.Sdp.ConnectionIp;
            int port = last.Sdp.AudioPort;
            var toPeer = streams.Where(s => s.DstIp == ip && s.DstPort == port).ToList();
            // Audio back from that party: from the SDP address, or (NAT) from the same host or
            // to the Mediant port it was told about.
            var fromPeer = streams.Where(s => (s.SrcIp == ip && s.SrcPort == port) ||
                                              (s.SrcIp == ip) ||
                                              (last.Owner.Length > 0 && s.SrcIp == last.Owner)).ToList();
            if (fromPeer.Count == 0)
            {
                // The far end may send from an address its SDP never mentioned (NAT): match on the
                // Mediant port this call gave it, for streams arriving at the Mediant.
                var mediantPorts = c.Sdps.Where(s => s.OwnerIsMediant == true).Select(s => s.Sdp.AudioPort).ToHashSet();
                fromPeer = streams.Where(s => s.Inbound == true && mediantPorts.Contains(s.DstPort)
                                              && !toPeer.Contains(s)).ToList();
            }

            string actual = string.Join(", ", fromPeer.Select(s => $"{s.SrcIp}:{s.SrcPort}").Distinct());
            views.Add(new MediaView(
                last.Owner.Length > 0 ? last.Owner : ip,
                $"{ip}:{port}",
                last.Sdp.Direction,
                toPeer.Sum(s => s.Packets),
                fromPeer.Sum(s => s.Packets),
                actual,
                fromPeer.Sum(s => s.Lost) + toPeer.Sum(s => s.Lost),
                Math.Round(fromPeer.Concat(toPeer).Where(s => s.HaveTransit).Select(s => s.Jitter / 8.0).DefaultIfEmpty(0).Max(), 1),
                Math.Round(fromPeer.Concat(toPeer).Select(s => s.MaxGapMs).DefaultIfEmpty(0).Max(), 0),
                LossPct(toPeer), LossPct(fromPeer), WorstMos(toPeer), WorstMos(fromPeer)));
        }
        return views;
    }

    public sealed record Check(string Level, string Text);

    private List<Check> Checks(Call c, List<MediaView> media, DateTime now)
    {
        var list = new List<Check>();

        if (c.State == "failed" && c.FinalStatus > 0)
            list.Add(new Check("bad", $"Call failed: {c.FinalStatus} {c.Messages.LastOrDefault(e => e.Message.Status == c.FinalStatus)?.Message.Reason}"));
        else if (c.State == "failed")
        {
            string why = LastEnd(c)?.Get("TrmReason") ?? "";
            list.Add(new Check("warn", $"Ended before it was answered (SBC reason: {(why.Length > 0 ? why : "unknown")})."));
        }
        else if (c.State == "cancelled" && c.Answered is null)
            list.Add(new Check("warn", "Cancelled by the caller before it was answered."));

        string signalling = SignallingOf(c);
        int unreadSip = SealedDuring(c, "tls");
        if (signalling == "TLS")
        {
            string extra = unreadSip > 0 ? $" {unreadSip} encrypted packet{(unreadSip == 1 ? "" : "s")} in this capture could not be read." : "";
            list.Add(new Check("note", "Signalling is TLS. The SIP text is shown because the SBC logged it after decrypting; the packets on the wire cannot be read as SIP." + extra));
        }
        else if (unreadSip > 0)
            list.Add(new Check("note", $"{unreadSip} encrypted signalling packet{(unreadSip == 1 ? "" : "s")} could not be read as SIP."));

        string protection = MediaProtectionOf(c);
        int unreadMedia = SealedDuring(c, "dtls");
        if (protection == "DTLS-SRTP")
            list.Add(new Check("note", "Audio is encrypted (DTLS-SRTP). The key exchange and the audio cannot be decoded. Loss and jitter still come from the RTP header when it is visible."
                + (unreadMedia > 0 ? $" {unreadMedia} DTLS packet{(unreadMedia == 1 ? "" : "s")} could not be read." : "")));
        else if (protection == "SRTP")
            list.Add(new Check("note", "Audio is encrypted (SRTP). Loss and jitter still come from the RTP header. The audio itself cannot be played or decoded."
                + (unreadMedia > 0 ? $" {unreadMedia} DTLS packet{(unreadMedia == 1 ? "" : "s")} could not be read." : "")));
        else if (unreadMedia > 0)
            list.Add(new Check("note", $"{unreadMedia} media packet{(unreadMedia == 1 ? "" : "s")} were DTLS and could not be decoded."));

        foreach (SdpEntry s in c.Sdps)
        {
            var msg = c.Messages[s.MessageIndex];
            string sentTo = msg.Dst.Length > 0 ? HostOf(msg.Dst) : "";
            if (s.Sdp.AudioPort == 0)
                list.Add(new Check("bad", $"{s.Label} from {Who(s)} rejected audio (m=audio port 0)."));
            if (s.Sdp.ConnectionIp == "0.0.0.0")
                list.Add(new Check("warn", $"{s.Label} from {Who(s)} put the call on hold (c=0.0.0.0)."));
            if (s.Sdp.Direction is "sendonly" or "recvonly" or "inactive")
                list.Add(new Check("warn", $"{s.Label} from {Who(s)} says a={s.Sdp.Direction}: audio is meant to flow one way or not at all."));
            if (SipText.IsPrivate(s.Sdp.ConnectionIp) && sentTo.Length > 0 && !SipText.IsPrivate(sentTo))
                list.Add(new Check("bad", $"{s.Label} sent to {sentTo} (a public address) offers a private media address {s.Sdp.ConnectionIp}: the far end can't send audio to it (NAT)."));
        }

        // Offer and answer with nothing in common.
        var invite = c.Sdps.FirstOrDefault(s => s.Label == "INVITE");
        var answer = c.Sdps.FirstOrDefault(s => s.Label.StartsWith("200", StringComparison.Ordinal) || s.Label.StartsWith("183", StringComparison.Ordinal));
        if (invite is not null && answer is not null)
        {
            var common = invite.Sdp.Codecs.Select(Codec).Intersect(answer.Sdp.Codecs.Select(Codec)).Where(x => !x.StartsWith("TELEPHONE-EVENT")).ToList();
            if (common.Count == 0)
                list.Add(new Check("bad", $"No codec in common: offer {string.Join(", ", invite.Sdp.Codecs)}; answer {string.Join(", ", answer.Sdp.Codecs)}."));
        }

        CdrChecks(c, list, media.Any(m => m.MosTo is not null || m.MosFrom is not null));

        bool talking = c.Answered is not null && ((c.Ended ?? now) - c.Answered.Value).TotalSeconds > 3;
        foreach (MediaView m in media)
        {
            if (!talking || m.Direction == "inactive") continue;
            if (m.ToPeer > 0 && m.FromPeer == 0 && m.Direction != "sendonly")
                list.Add(new Check("bad", $"One-way audio: {m.ToPeer} packets sent to {m.SdpAddress}, none received back from {m.Peer}."));
            else if (m.FromPeer > 0 && m.ToPeer == 0 && m.Direction != "recvonly")
                list.Add(new Check("bad", $"One-way audio: {m.FromPeer} packets received from {m.Peer}, none sent to {m.SdpAddress}."));
            else if (m.FromPeer == 0 && m.ToPeer == 0 && media.Count > 0)
                list.Add(new Check("warn", $"No audio packets seen for {m.SdpAddress}. Is media included in the Debug Recording filter?"));

            if (m.ActualFrom.Length > 0 && !m.ActualFrom.Split(", ").Contains(m.SdpAddress))
                list.Add(new Check("warn", $"Audio from {m.Peer} arrives from {m.ActualFrom}, not the address in its SDP ({m.SdpAddress}): NAT in the path."));
            if (m.LossFromPct > 2)
                list.Add(new Check("warn", $"Packet loss from {m.Peer} to the SBC: {m.LossFromPct:0.0}%."));
            if (m.LossToPct > 2)
                list.Add(new Check("warn", $"Packet loss from the SBC to {m.Peer}: {m.LossToPct:0.0}% (as seen leaving the SBC)."));
            double? worst = new[] { m.MosTo, m.MosFrom }.Where(x => x is not null).DefaultIfEmpty(null).Min();
            if (worst is double mos && mos < 3.6)
                list.Add(new Check(mos < 3.1 ? "bad" : "warn", $"Poor audio quality with {m.Peer}: estimated MOS {mos:0.0}."));
            if (m.JitterMs > 30)
                list.Add(new Check("warn", $"High jitter with {m.Peer}: {m.JitterMs} ms."));
            if (m.MaxGapMs > 1000)
                list.Add(new Check("warn", $"Audio stopped for {m.MaxGapMs / 1000:0.0} s at least once with {m.Peer} (or silence suppression)."));
        }
        return list;
    }

    private static void CdrChecks(Call c, List<Check> list, bool havePacketMos)
    {
        foreach (var r in c.Cdrs.Where(r => r.Kind is "CALL_END" or "MEDIA_END"))
        {
            long inP = r.Num("InPackets"), outP = r.Num("OutPackets"), dur = r.Num("Durat");
            long loss = r.Num("PackLoss", "LocalPackLoss"), jitter = r.Num("RTPjitter");
            string remote = r.Get("RtpIp", "RemoteRtpIp");
            string port = r.Get("Port", "RemoteRtpPort");
            string who = remote.Length > 0 ? $"{remote}{(port.Length > 0 && port != "0" ? ":" + port : "")}" : "the far end";
            if (dur > 3 && outP > 0 && inP == 0)
                list.Add(new Check("bad", $"One-way audio (SBC record): the SBC sent {outP:N0} audio packets to {who} but received none back."));
            else if (dur > 30 && outP > 500 && inP >= 0 && inP < outP / 20)
                list.Add(new Check("warn", $"The SBC received far less audio than it sent to {who} (in {inP:N0} / out {outP:N0} packets): one-way audio, or the far end only sends while someone talks."));
            if (inP > 0 && loss > 0 && loss * 100.0 / (inP + loss) > 2)
                list.Add(new Check("warn", $"Packet loss measured by the SBC from {who}: {loss * 100.0 / (inP + loss):0.0}%."));
            if (jitter > 30)
                list.Add(new Check("warn", $"Jitter measured by the SBC from {who}: {jitter} ms."));
            string term = r.Get("TrmReason");
            if (term.Contains("RTP_CONN_BROKEN", StringComparison.OrdinalIgnoreCase))
                list.Add(new Check("warn", "The SBC ended the call because audio stopped arriving (RTP broken connection)."));
            else if (term.Contains("ABNORMAL", StringComparison.OrdinalIgnoreCase) || term.Contains("FAIL", StringComparison.OrdinalIgnoreCase)
                     || r.Get("TrmReasonCategory").Contains("ABNORMAL", StringComparison.OrdinalIgnoreCase))
                list.Add(new Check("warn", $"The SBC recorded an abnormal end: {term}."));
        }
        if (havePacketMos) return;
        foreach (var q in SbcQualityFor(c))
        {
            string who = q.RemoteIp.Length > 0 ? q.RemoteIp : "the far end";
            if (q.LatchedIp.Length > 0 && q.LatchedIp != "0.0.0.0" && q.RemoteIp.Length > 0
                && !q.LatchedIp.Equals(q.RemoteIp, StringComparison.OrdinalIgnoreCase))
                list.Add(new Check("warn", $"Audio from {who} arrives from {q.LatchedIp} (SBC record): NAT in the path."));
            double? worst = new[] { q.MosIn, q.MosOut }.Where(m => m is not null).DefaultIfEmpty(null).Min();
            if (worst is double mos && mos < 3.6)
                list.Add(new Check(mos < 3.1 ? "bad" : "warn", $"Poor audio quality with {who}, from the SBC's own record: MOS {mos:0.0}."));
        }
    }

    /// <summary>True when this call's audio is SRTP, so the stream must not be played as G.711.</summary>
    public bool AudioEncrypted(int id, string src, string dst)
    {
        lock (_gate)
        {
            Call? c = _order.FirstOrDefault(x => x.Id == id);
            if (c is null || MediaProtectionOf(c).Length == 0) return false;
            return StreamsFor(c).Any(s => Endpoint(s, true) == src && Endpoint(s, false) == dst);
        }
    }

    private static string Endpoint(RtpStream s, bool source) =>
        source
            ? s.SrcIp.Length > 0 ? $"{s.SrcIp}:{s.SrcPort}" : $"(SBC):{s.SrcPort}"
            : s.DstIp.Length > 0 ? $"{s.DstIp}:{s.DstPort}" : $"(SBC):{s.DstPort}";

    private static string SignallingOf(Call c)
    {
        if (c.Messages.Any(m => m.Message.Transport == "TLS")) return "TLS";
        if (c.Messages.Any(m => m.Message.Transport == "TCP")) return "TCP";
        if (c.Messages.Any(m => m.Message.Transport == "UDP")) return "UDP";
        return "";
    }

    /// <summary>The protection the call settled on. An answer wins over the offer.</summary>
    private static string MediaProtectionOf(Call c)
    {
        SdpEntry? answer = c.Sdps.FirstOrDefault(s => s.Label.StartsWith("200", StringComparison.Ordinal) || s.Label.StartsWith("183", StringComparison.Ordinal));
        if (answer is not null) return answer.Sdp.Protection;
        return c.Sdps.Select(s => s.Sdp.Protection).FirstOrDefault(p => p.Length > 0) ?? "";
    }

    private int SealedDuring(Call c, string kind)
    {
        DateTime from = c.Started.AddSeconds(-2);
        DateTime to = (c.Ended ?? c.LastSeen).AddSeconds(5);
        int n = 0;
        foreach (SealedPacket packet in _sealed)
            if (packet.Sbc == c.Sbc && packet.Kind == kind && packet.Time >= from && packet.Time <= to) n++;
        return n;
    }

    private static string Codec(string name) => name.Split('/')[0].ToUpperInvariant();

    private static string Who(SdpEntry s) => s.OwnerIsMediant == true ? "the Mediant" : s.Owner.Length > 0 ? s.Owner : "the far end";

    private static double Duration(Call c, DateTime now) =>
        c.Answered is null ? 0 : Math.Round(((c.Ended ?? now) - c.Answered.Value).TotalSeconds, 0);

    private static string HostOf(string endpoint)
    {
        int colon = endpoint.LastIndexOf(':');
        return colon > 0 ? endpoint[..colon] : endpoint;
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
}
