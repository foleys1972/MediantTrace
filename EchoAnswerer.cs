using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Answers calls that reach the test caller's SIP port and sends their audio
/// straight back. A test call that loops through an SBC (or an E1/ISDN loop)
/// and returns here then measures both directions: the tone the caller sends
/// comes back to it. Only calls from known SBC addresses are answered.
/// </summary>
public sealed partial class EchoAnswerer
{
    private readonly Func<IPAddress, bool> _allowed;
    private readonly Action<string, IPEndPoint> _send;
    private readonly Func<int> _nextRtpPort;
    private readonly Func<SipText.Message, bool> _isOwnLoop;
    private readonly Action<TestResult, short[]> _onTestLine;
    private readonly int _sipPort;
    private readonly ConcurrentDictionary<string, Leg> _legs = new();

    private sealed class Leg
    {
        public string CallId = "";
        public string ToTag = "";
        public Socket? Rtp;
        public CancellationTokenSource Stop = new();
        public DateTime Started = DateTime.UtcNow;
        public long Echoed;
        // For the test line, which hangs up itself: the dialog as the answering side sees it.
        public TestResult? Result;
        public string TheirFrom = "", OurTo = "", RemoteTarget = "";
        public List<string> Routes = [];
        public IPEndPoint? Peer;
    }

    public int Active => _legs.Count;
    public long Answered;

    public EchoAnswerer(int sipPort, Func<IPAddress, bool> allowed, Action<string, IPEndPoint> send, Func<int> nextRtpPort,
                        Func<SipText.Message, bool> isOwnLoop, Action<TestResult, short[]> onTestLine)
    {
        _sipPort = sipPort;
        _allowed = allowed;
        _send = send;
        _nextRtpPort = nextRtpPort;
        _isOwnLoop = isOwnLoop;
        _onTestLine = onTestLine;
    }

    /// <summary>Handles a SIP message that belongs to no test call. Returns false if it is not ours either.</summary>
    public bool Handle(string text, IPEndPoint from)
    {
        SipText.Message m = SipText.Parse(text.TrimEnd('\0'));
        if (!m.IsRequest)
        {
            return _legs.ContainsKey(m.CallId); // responses to our BYE: nothing to do
        }
        string callId = m.CallId;
        switch (m.Method)
        {
            case "INVITE":
                if (_legs.TryGetValue(callId, out Leg? existing))
                {
                    Reply(text, from, 200, "OK", existing.ToTag, Sdp(LocalIp(from), ((IPEndPoint)existing.Rtp!.LocalEndPoint!).Port));
                    return true;
                }
                if (!_allowed(from.Address))
                {
                    Reply(text, from, 403, "Forbidden", Rand(8));
                    Log.Write($"Echo: refused a call from {from}, which is not a listed SBC");
                    return true;
                }
                Answer(m, text, from);
                return true;
            case "ACK":
                if (_legs.TryGetValue(callId, out Leg? acked)) { Note(acked, false, m.FirstLine, text); return true; }
                return false;
            case "BYE" or "CANCEL":
                if (_legs.TryRemove(callId, out Leg? leg))
                {
                    Note(leg, false, m.FirstLine, text);
                    Reply(text, from, 200, "OK", leg.ToTag, null, leg);
                    if (leg.Result is not null) leg.Result.Findings.Add("The caller hung up before the end of the test.");
                    leg.Stop.Cancel();
                    Log.Write($"Echo: call {callId} ended after {(DateTime.UtcNow - leg.Started).TotalSeconds:0} s, {leg.Echoed} packets echoed");
                }
                else Reply(text, from, 481, "Call/Transaction Does Not Exist", Rand(8));
                return true;
            case "OPTIONS":
                Reply(text, from, 200, "OK", Rand(8));
                return true;
            default:
                if (_legs.TryGetValue(callId, out Leg? l2)) { Reply(text, from, 200, "OK", l2.ToTag); return true; }
                return false;
        }
    }

    private void Answer(SipText.Message m, string raw, IPEndPoint from)
    {
        if (m.Sdp is null || m.Sdp.AudioPort <= 0 || !IPAddress.TryParse(m.Sdp.ConnectionIp, out IPAddress? remoteIp))
        {
            Reply(raw, from, 488, "Not Acceptable Here", Rand(8));
            return;
        }
        bool echo = _isOwnLoop(m);
        string pt = m.Sdp.Codecs.Any(c => c.StartsWith("PCMU", StringComparison.OrdinalIgnoreCase)) && !m.Sdp.Codecs.Any(c => c.StartsWith("PCMA", StringComparison.OrdinalIgnoreCase)) ? "0" : "8";
        var leg = new Leg { CallId = m.CallId, ToTag = Rand(8) };
        int port = _nextRtpPort();
        leg.Rtp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try { leg.Rtp.Bind(new IPEndPoint(IPAddress.Any, port)); }
        catch (SocketException e)
        {
            leg.Rtp.Dispose();
            Reply(raw, from, 503, "Service Unavailable", leg.ToTag);
            Log.Write($"Echo: no audio port free ({e.Message})");
            return;
        }
        _legs[m.CallId] = leg;
        Interlocked.Increment(ref Answered);
        var remote = new IPEndPoint(remoteIp, m.Sdp.AudioPort);
        if (!echo)
        {
            string caller = Regex.Match(m.From, @"sips?:([^@;>]+)") is { Success: true } cm ? cm.Groups[1].Value : m.From;
            leg.Result = new TestResult
            {
                TestId = "inbound", Name = "Call to the test line", Target = $"from {caller} via {from.Address}", Trigger = "inbound",
                Started = DateTime.UtcNow, CallId = m.CallId, Codec = pt == "0" ? "PCMU" : "PCMA", Stage = "answering",
                RemoteMedia = $"{remote}", LocalMedia = $"{LocalIp(from)}:{port}", FinalStatus = 200, FinalReason = "OK (answered by MediantTrace)",
            };
            leg.TheirFrom = Headers(raw, "from", "f").FirstOrDefault() ?? "";
            string to = Headers(raw, "to", "t").FirstOrDefault() ?? "";
            leg.OurTo = TagRegex().IsMatch(to) ? to : to + ";tag=" + leg.ToTag;
            leg.RemoteTarget = Regex.Match(Headers(raw, "contact", "m").FirstOrDefault() ?? "", @"<([^>]+)>") is { Success: true } ct ? ct.Groups[1].Value : $"sip:{caller}@{from.Address}:{from.Port}";
            leg.Routes = Headers(raw, "record-route").SelectMany(h => h.Split(',')).Select(r => r.Trim()).ToList();
            leg.Peer = from;
            Note(leg, false, m.FirstLine, raw);
        }
        Reply(raw, from, 100, "Trying", "", null, leg);
        Reply(raw, from, 200, "OK", leg.ToTag, Sdp(LocalIp(from), port, pt), leg);
        if (echo)
        {
            Log.Write($"Echo: answered {m.RequestUri} from {from}, echoing audio to {remote}");
            var thread = new Thread(() => Echo(leg, remote)) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "echo-rtp" };
            thread.Start();
        }
        else
        {
            Log.Write($"Test line: answered a call {leg.Result!.Target}");
            var thread = new Thread(() => RunTestLine(leg, remote, pt == "8")) { IsBackground = true, Name = "test-line" };
            thread.Start();
        }
        // A safety net: never hold a call open for ever if the BYE is lost.
        _ = Task.Delay(TimeSpan.FromMinutes(10)).ContinueWith(_ => { if (_legs.TryRemove(leg.CallId, out var stale)) stale.Stop.Cancel(); });
    }

    /// <summary>Sends every audio packet back where it came from (symmetric RTP), else to the SDP address.</summary>
    private static void Echo(Leg leg, IPEndPoint sdpRemote)
    {
        byte[] buffer = new byte[2048];
        EndPoint any = new IPEndPoint(IPAddress.Any, 0);
        leg.Rtp!.ReceiveTimeout = 250;
        try
        {
            while (!leg.Stop.IsCancellationRequested)
            {
                int n;
                EndPoint from = any;
                try { n = leg.Rtp.ReceiveFrom(buffer, ref from); }
                catch (SocketException) { continue; }
                if (n < 12 || buffer[0] >> 6 != 2) continue;
                try { leg.Rtp.SendTo(buffer, 0, n, SocketFlags.None, from); } catch (SocketException) { }
                leg.Echoed++;
            }
        }
        catch (ObjectDisposedException) { }
        finally { leg.Rtp.Dispose(); }
        _ = sdpRemote;
    }

    private void Reply(string request, IPEndPoint to, int code, string reason, string toTag, string? sdp = null, Leg? leg = null)
    {
        var sb = new StringBuilder($"SIP/2.0 {code} {reason}\r\n");
        foreach (string via in Headers(request, "via", "v")) sb.Append($"Via: {via}\r\n");
        foreach (string rr in Headers(request, "record-route")) sb.Append($"Record-Route: {rr}\r\n");
        sb.Append($"From: {Headers(request, "from", "f").FirstOrDefault()}\r\n");
        string toHeader = Headers(request, "to", "t").FirstOrDefault() ?? "";
        if (toTag.Length > 0 && !TagRegex().IsMatch(toHeader)) toHeader += ";tag=" + toTag;
        sb.Append($"To: {toHeader}\r\n");
        sb.Append($"Call-ID: {Headers(request, "call-id", "i").FirstOrDefault()}\r\n");
        sb.Append($"CSeq: {Headers(request, "cseq").FirstOrDefault()}\r\n");
        sb.Append($"Contact: <sip:echo@{LocalIp(to)}:{_sipPort}>\r\n");
        sb.Append("User-Agent: MediantTrace echo\r\n");
        if (sdp is not null) sb.Append("Content-Type: application/sdp\r\n");
        sb.Append($"Content-Length: {(sdp is null ? 0 : Encoding.UTF8.GetByteCount(sdp))}\r\n\r\n{sdp}");
        _send(sb.ToString(), to);
        if (leg is not null) Note(leg, true, $"SIP/2.0 {code} {reason}", sb.ToString());
    }

    private static void Note(Leg leg, bool outbound, string first, string text)
    {
        if (leg.Result is null) return;
        lock (leg.Result) leg.Result.Ladder.Add(new LadderEntry { Time = DateTime.UtcNow, Outbound = outbound, First = first, Text = text });
    }

    /// <summary>The test line script, then our own hang-up (BYE as the answering side).</summary>
    private void RunTestLine(Leg leg, IPEndPoint remote, bool alaw)
    {
        var r = leg.Result!;
        var session = new TestLine.Session { Rtp = leg.Rtp!, Remote = remote, Alaw = alaw, Result = r, HungUp = leg.Stop.Token };
        try { TestLine.Run(session); }
        catch (Exception e) { r.Findings.Add($"Test line error: {e.Message}"); Log.Write($"Test line failed: {e}"); }
        bool callerHungUp = leg.Stop.IsCancellationRequested;
        if (!callerHungUp && _legs.TryRemove(leg.CallId, out _))
        {
            r.Stage = "hanging up";
            string ip = LocalIp(leg.Peer!);
            string bye = $"BYE {leg.RemoteTarget} SIP/2.0\r\nVia: SIP/2.0/UDP {ip}:{_sipPort};branch=z9hG4bK{Rand(12)};rport\r\nMax-Forwards: 70\r\n" +
                         $"From: {leg.OurTo}\r\nTo: {leg.TheirFrom}\r\nCall-ID: {leg.CallId}\r\nCSeq: 1 BYE\r\n" +
                         string.Concat(leg.Routes.Select(x => $"Route: {x}\r\n")) + "User-Agent: MediantTrace\r\nContent-Length: 0\r\n\r\n";
            _send(bye, leg.Peer!);
            Note(leg, true, $"BYE {leg.RemoteTarget} SIP/2.0", bye);
            leg.Stop.Cancel();
        }
        r.Ended = DateTime.UtcNow;
        r.TalkSeconds = Math.Round((r.Ended.Value - r.Started).TotalSeconds, 1);
        r.AnswerMs = 0;
        // Verdict: we can measure what reached us; the caller judges what reached them by ear.
        if (r.RxPackets == 0)
        {
            r.Outcome = "failed";
            r.Findings.Insert(0, "One-way audio: no audio came from the caller. They should have heard the announcement; MediantTrace heard nothing back.");
        }
        else if (r.RxAudioPercent < 5)
        {
            r.Outcome = "warning";
            r.Findings.Insert(0, $"Audio packets arrived from the caller ({r.RxPackets:N0}) but they were silent. Did the caller speak after the tone?");
        }
        else
        {
            r.Outcome = r.RxLossPercent > 2 || r.RxJitterMs > 30 || r.MosRx is < 3.6 ? "warning" : "passed";
            r.Findings.Insert(0, $"Caller's audio received: level {r.RxLevelDb} dBov, sound in {r.RxAudioPercent}% of frames, estimated MOS {r.MosRx:0.0}. The caller heard their own voice played back if the audio reached them.");
            if (r.RxLossPercent > 2) r.Findings.Add($"Packet loss on the caller's audio: {r.RxLossPercent}%.");
            if (r.RxJitterMs > 30) r.Findings.Add($"High jitter on the caller's audio: {r.RxJitterMs} ms.");
        }
        if (r.AudioFrom.Length > 0 && r.AudioFrom != r.RemoteMedia)
            r.Findings.Add($"The caller's audio came from {r.AudioFrom}, not the address in their SDP ({r.RemoteMedia}): NAT or media latching in the path.");
        r.Stage = "done";
        try { leg.Rtp?.Dispose(); } catch { }
        _onTestLine(r, session.Recording);
        Log.Write($"Test line: {r.Outcome} ({r.Findings.FirstOrDefault()})");
    }

    private static string Sdp(string ip, int port, string pt = "8")
    {
        string codec = pt == "0" ? "PCMU" : "PCMA";
        long id = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"v=0\r\no=MediantTrace {id} {id} IN IP4 {ip}\r\ns=MediantTrace echo\r\nc=IN IP4 {ip}\r\nt=0 0\r\n" +
               $"m=audio {port} RTP/AVP {pt} 101\r\na=rtpmap:{pt} {codec}/8000\r\na=rtpmap:101 telephone-event/8000\r\na=ptime:20\r\na=sendrecv\r\n";
    }

    private static string LocalIp(IPEndPoint toward)
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(toward);
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }

    private static List<string> Headers(string message, params string[] names)
    {
        var wanted = new HashSet<string>(names);
        int end = message.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        var list = new List<string>();
        foreach (string line in (end >= 0 ? message[..end] : message).Split("\r\n").Skip(1))
        {
            int colon = line.IndexOf(':');
            if (colon > 0 && wanted.Contains(line[..colon].Trim().ToLowerInvariant())) list.Add(line[(colon + 1)..].Trim());
        }
        return list;
    }

    private static string Rand(int n) => Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(n)).ToLowerInvariant()[..n];

    [GeneratedRegex(@";\s*tag=", RegexOptions.IgnoreCase)]
    private static partial Regex TagRegex();
}
