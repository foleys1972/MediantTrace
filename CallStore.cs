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
    private int _nextCallId = 1;

    public sealed class SipEntry
    {
        public DateTime Time;
        public string Source = "";      // "DR" or "SYSLOG"
        public string Src = "", Dst = ""; // ip:port, empty when the trace doesn't say
        public bool? Inbound;            // arrived at the Mediant (true) or sent by it (false)
        public SipText.Message Message = null!;
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

        public double LossPercent => Expected > 0 ? 100.0 * Lost / Expected : 0;
        public double JitterMs => HaveTransit ? Jitter / 8.0 : 0;
        public double? Mos => Packets >= 25 ? Quality.Mos(LossPercent, JitterMs) : null;

        public long Expected => SeqInit ? (long)(Cycles + MaxSeq) - BaseSeq + 1 : 0;
        public long Lost => Math.Max(0, Expected - Packets);
    }

    public sealed record SyslogLine(DateTime Time, string Source, string Text);

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
            if (call.SessionId.Length == 0) call.SessionId = sessionId;
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
                if (_streams.Count >= MaxStreams) return;
                s = new RtpStream
                {
                    Key = key, Sbc = sbc, SrcIp = srcIp, SrcPort = srcPort, DstIp = dstIp, DstPort = dstPort,
                    Inbound = inbound, Ssrc = ssrc, PayloadType = pt, First = time, SessionId = sessionId,
                };
                _streams[key] = s;
            }
            if (s.SessionId.Length == 0 && sessionId.Length > 0) s.SessionId = sessionId;
            s.Last = time;

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
                    State = r.Kind == "CALL_END" ? "ended" : "connected",
                };
                if (dur > 0) call.Answered = call.Started;
                _calls[key] = call;
                _order.Add(call);
                if (_order.Count > MaxCalls) { _calls.Remove(_order[0].Key); _order.RemoveAt(0); }
            }
            if (call.Cdrs.Count < 40) call.Cdrs.Add(r);
            call.LastSeen = r.Received;
            if (call.SessionId.Length == 0) call.SessionId = r.Get("SessionId");
            if (r.Kind == "CALL_END")
            {
                call.Ended ??= r.Received;
                if (call.State is "connected" or "ringing" or "trying") call.State = call.Answered is null ? "failed" : "ended";
            }
        }
    }

    public void AddSyslog(DateTime time, string source, string text)
    {
        lock (_gate)
        {
            _syslog.AddLast(new SyslogLine(time, source, text));
            if (_syslog.Count > MaxSyslog) _syslog.RemoveFirst();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _calls.Clear();
            _order.Clear();
            _streams.Clear();
            _syslog.Clear();
            _nextCallId = 1;
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
                var warnings = Checks(c, media, now);
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
                    warnings = warnings.Count,
                    topIssue = warnings.OrderBy(w => w.Level == "bad" ? 0 : 1).FirstOrDefault()?.Text ?? "",
                    oneWay = warnings.Any(w => w.Text.StartsWith("One-way", StringComparison.Ordinal)),
                    codec = c.Sdps.LastOrDefault(x => x.OwnerIsMediant != true)?.Sdp.Codecs.FirstOrDefault() ?? "",
                    mos = WorstMos(callStreams),
                    sbcIn = LastEnd(c)?.Num("InPackets") ?? -1,
                    sbcOut = LastEnd(c)?.Num("OutPackets") ?? -1,
                    cdr = c.Cdrs.Count > 0,
                    ended = c.Ended,
                    severity = warnings.Any(w => w.Level == "bad") ? "bad" : warnings.Count > 0 ? "warn" : "ok",
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
                mos = WorstMos(streams),
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
                    text = e.Message.Text,
                }).ToList(),
                sdps = c.Sdps.Select(s => new
                {
                    time = s.Time,
                    label = s.Label,
                    owner = s.Owner,
                    ownerIsMediant = s.OwnerIsMediant,
                    ip = s.Sdp.ConnectionIp,
                    port = s.Sdp.AudioPort,
                    direction = s.Sdp.Direction,
                    codecs = s.Sdp.Codecs.ToList(),
                    messageIndex = s.MessageIndex,
                }).ToList(),
                media,
                streams = streams.Select(x => StreamView(x, true)).ToList(),
                cdrs = c.Cdrs.Select(r => new { kind = r.Kind, family = r.Family, received = r.Received, fields = r.Fields.Select(f => new { k = f.Key, v = f.Value }).ToList() }).ToList(),
            };
        }
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
            return lines.Take(Math.Clamp(limit, 1, 5000)).Select(l => new { time = l.Time, source = l.Source, text = l.Text }).ToList();
        }
    }

    /// <summary>Syslog severity from the &lt;PRI&gt; prefix (0 emergency ... 3 error ... 7 debug), or -1.</summary>
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

    private static object StreamView(RtpStream s) => StreamView(s, false);

    private static object StreamView(RtpStream s, bool timeline) => new
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

    private static List<Check> Checks(Call c, List<MediaView> media, DateTime now)
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

        CdrChecks(c, list);

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

    private static void CdrChecks(Call c, List<Check> list)
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
