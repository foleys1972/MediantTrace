using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>A saved test: who to call, through which SBC, and how often.</summary>
public sealed class TestDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n")[..10];
    public string Name { get; set; } = "";
    public string SbcIp { get; set; } = "";        // where to send the INVITE (the SBC's SIP address)
    public int SbcPort { get; set; } = 5060;
    public string To { get; set; } = "";           // number or user to call
    public string ToDomain { get; set; } = "";     // host part of the Request-URI (default: the SBC address)
    public string From { get; set; } = "mediantrace";
    public string Codec { get; set; } = "PCMA";    // PCMA or PCMU
    public int DurationSeconds { get; set; } = 15; // talk time after answer
    public int RepeatMinutes { get; set; }         // 0 = only when run by hand
    public int RingSeconds { get; set; } = 60;     // give up (CANCEL) when not answered by then
    public string LineType { get; set; } = "";     // ARD, MRD, HOOT, DDI for bulk line tests: sets what "answered" should look like
    public string RingSignal { get; set; } = "dtmf"; // MRD only: "dtmf" (INFO dtmf-relay * / #), "info" (x-tradecom-signal ring/stop) or "none"
    public string Username { get; set; } = "";     // for trunks that ask for digest login
    public string PasswordProtected { get; set; } = ""; // DPAPI, machine scope
}

public sealed class TestResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n")[..12];
    public string TestId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public string Trigger { get; set; } = "manual";
    public DateTime Started { get; set; }
    public DateTime? Ended { get; set; }
    public string Outcome { get; set; } = "running"; // running, passed, warning, failed
    public string Stage { get; set; } = "starting";
    public List<string> Findings { get; set; } = [];
    public string CallId { get; set; } = "";
    public int FinalStatus { get; set; }
    public string FinalReason { get; set; } = "";
    public double? TryingMs { get; set; }
    public double? RingingMs { get; set; }
    public double? AnswerMs { get; set; }
    public double TalkSeconds { get; set; }
    public string LocalMedia { get; set; } = "";
    public string RemoteMedia { get; set; } = "";
    public string AudioFrom { get; set; } = "";
    public string Codec { get; set; } = "";
    public long TxPackets { get; set; }
    public long RxPackets { get; set; }
    public long RxLost { get; set; }
    public double RxLossPercent { get; set; }
    public double RxJitterMs { get; set; }
    public double RxMaxGapMs { get; set; }
    public double? FirstAudioMs { get; set; }
    public double RxAudioPercent { get; set; }     // share of received frames with sound in them
    public double RxLevelDb { get; set; } = -99;   // average level of frames with sound, dBov
    public bool ToneReturned { get; set; }         // our 1004 Hz tone came back (echo / loop)
    public double? MosRx { get; set; }
    public string SignalSent { get; set; } = "";   // MRD ring signal sent inside the call
    public int SignalResponse { get; set; }        // SIP status the line answered the ring INFO with (0 = none)
    public double? PersonAnsweredMs { get; set; }  // MRD: speech heard after the ring (a person joined the line)
    public List<int> TxPerSecond { get; set; } = [];
    public List<int> RxPerSecond { get; set; } = [];
    public List<LadderEntry> Ladder { get; set; } = [];
}

public sealed class LadderEntry
{
    public DateTime Time { get; set; }
    public bool Outbound { get; set; }
    public string First { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>
/// Places SIP test calls through an SBC: sends a 1004 Hz tone as G.711, measures
/// what comes back (packets, loss, jitter, gaps, level, tone echo) and scores it.
/// One call at a time; scheduled tests queue behind manual ones.
/// </summary>
public sealed partial class SipTester : IDisposable
{
    private const int MaxResults = 500;
    private const double ToneHz = 1004;

    private readonly Settings _settings;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _oneAtATime = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private List<TestDefinition> _tests = [];
    private List<TestResult> _results = [];
    private Socket? _sip;
    private TestCall? _active;
    private int _nextRtp;

    public string? ListenError { get; private set; }
    private EchoAnswerer? _echo;
    public int EchoActive => _echo?.Active ?? 0;
    public long EchoAnswered => _echo?.Answered ?? 0;
    public TestResult? Live => _active?.Result;

    private static string TestsPath => Path.Combine(AppContext.BaseDirectory, "tests.json");
    private static string ResultsPath => Path.Combine(AppContext.BaseDirectory, "test-results.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public SipTester(Settings settings)
    {
        _settings = settings;
        _nextRtp = settings.TestRtpPortFirst;
        try { if (File.Exists(TestsPath)) _tests = JsonSerializer.Deserialize<List<TestDefinition>>(File.ReadAllText(TestsPath)) ?? []; }
        catch (Exception e) { Log.Write($"tests.json could not be read: {e.Message}"); }
        try { if (File.Exists(ResultsPath)) _results = JsonSerializer.Deserialize<List<TestResult>>(File.ReadAllText(ResultsPath)) ?? []; }
        catch (Exception e) { Log.Write($"test-results.json could not be read: {e.Message}"); }
        foreach (var r in _results.Where(r => r.Outcome == "running")) { r.Outcome = "failed"; r.Findings.Add("The program stopped during this test."); }
    }

    public void Open()
    {
        try
        {
            _sip = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sip.Bind(new IPEndPoint(IPAddress.Any, _settings.TestSipPort));
            _echo = new EchoAnswerer(_settings.TestSipPort, KnownSbc, SendSip, NextRtpPort, IsOwnLoop, OnTestLine);
            _ = Task.Run(() => SipLoop(_stop.Token));
            _ = Task.Run(() => Scheduler(_stop.Token));
            Log.Write($"Test caller ready on UDP {_settings.TestSipPort} (RTP {_settings.TestRtpPortFirst}-{_settings.TestRtpPortFirst + _settings.TestRtpPortCount - 1})");
        }
        catch (SocketException e)
        {
            ListenError = $"Test calls unavailable: could not use UDP {_settings.TestSipPort} ({e.Message}).";
            Log.Write(ListenError);
        }
    }

    // ------------------------------------------------------------ definitions

    public List<object> Tests()
    {
        lock (_gate)
            return _tests.Select(t => (object)new
            {
                t.Id, t.Name, t.SbcIp, t.SbcPort, t.To, t.ToDomain, t.From, t.Codec, t.DurationSeconds, t.RepeatMinutes,
                t.RingSeconds, t.LineType, t.RingSignal, t.Username,
                hasPassword = t.PasswordProtected.Length > 0,
                last = _results.LastOrDefault(r => r.TestId == t.Id) is { } r ? Summary(r) : null,
                history = _results.Where(r => r.TestId == t.Id).TakeLast(30).Select(r => new { r.Id, r.Started, r.Outcome, r.MosRx, r.RxLossPercent }).ToList(),
            }).ToList();
    }

    public sealed class TestInput
    {
        public string? Id { get; set; }
        public string Name { get; set; } = "";
        public string SbcIp { get; set; } = "";
        public int SbcPort { get; set; } = 5060;
        public string To { get; set; } = "";
        public string ToDomain { get; set; } = "";
        public string From { get; set; } = "";
        public string Codec { get; set; } = "PCMA";
        public int DurationSeconds { get; set; } = 15;
        public int RepeatMinutes { get; set; }
        public int? RingSeconds { get; set; }       // null = keep the stored one
        public string? LineType { get; set; }       // null = keep; "" = a plain call
        public string? RingSignal { get; set; }     // null = keep; dtmf, info, or none
        public string Username { get; set; } = "";
        public string? Password { get; set; }       // null = keep the stored one
    }

    public (bool ok, string message, string? id) Save(TestInput input)
    {
        string name = input.Name.Trim(), to = input.To.Trim(), sbc = input.SbcIp.Trim();
        if (name.Length == 0) return (false, "Give the test a name.", null);
        if (!IPAddress.TryParse(sbc, out IPAddress? sbcAddr) || sbcAddr.AddressFamily != AddressFamily.InterNetwork) return (false, "The SBC address must be an IPv4 address.", null);
        if (input.SbcPort is < 1 or > 65535) return (false, "The SBC port must be 1-65535.", null);
        if (!SafeUser().IsMatch(to)) return (false, "The number to call may contain digits, letters, + * # . - _ only.", null);
        string from = input.From.Trim().Length > 0 ? input.From.Trim() : "mediantrace";
        if (!SafeUser().IsMatch(from)) return (false, "The calling number may contain digits, letters, + * # . - _ only.", null);
        string domain = input.ToDomain.Trim();
        if (domain.Length > 0 && !SafeHost().IsMatch(domain)) return (false, "The destination domain is not a valid host name or address.", null);
        string codec = input.Codec.ToUpperInvariant() == "PCMU" ? "PCMU" : "PCMA";
        lock (_gate)
        {
            TestDefinition? t = input.Id is null ? null : _tests.FirstOrDefault(x => x.Id == input.Id);
            if (t is null) { t = new TestDefinition(); _tests.Add(t); }
            t.Name = name; t.SbcIp = sbc; t.SbcPort = input.SbcPort; t.To = to; t.ToDomain = domain; t.From = from; t.Codec = codec;
            t.DurationSeconds = Math.Clamp(input.DurationSeconds, 3, 300);
            t.RepeatMinutes = Math.Clamp(input.RepeatMinutes, 0, 24 * 60);
            if (input.RingSeconds is int ring) t.RingSeconds = Math.Clamp(ring, 5, 300);
            if (input.LineType is not null) t.LineType = NormalizeLineType(input.LineType);
            if (input.RingSignal is not null) t.RingSignal = NormalizeRingSignal(input.RingSignal);
            t.Username = input.Username.Trim();
            if (input.Password is not null)
                t.PasswordProtected = input.Password.Length == 0 ? "" : Convert.ToBase64String(
                    ProtectedData.Protect(Encoding.UTF8.GetBytes(input.Password), null, DataProtectionScope.LocalMachine));
            SaveTests();
            return (true, "Saved.", t.Id);
        }
    }

    public bool Delete(string id)
    {
        lock (_gate)
        {
            int n = _tests.RemoveAll(t => t.Id == id);
            if (n > 0) SaveTests();
            return n > 0;
        }
    }

    public (bool ok, string message, string? resultId) Run(string id, string trigger = "manual")
    {
        if (_sip is null) return (false, ListenError ?? "The test caller is not running.", null);
        TestDefinition? t;
        lock (_gate) t = _tests.FirstOrDefault(x => x.Id == id);
        if (t is null) return (false, "No such test.", null);
        if (_active is not null && trigger == "manual") return (false, $"A test is already running ({_active.Result.Name}). Try again when it finishes.", null);
        var result = new TestResult
        {
            TestId = t.Id, Name = t.Name, Target = $"{t.To} via {t.SbcIp}:{t.SbcPort}", Trigger = trigger,
            Started = DateTime.UtcNow, Codec = t.Codec, Stage = "queued",
        };
        lock (_gate) AddResult(result);
        _ = Task.Run(async () =>
        {
            await _oneAtATime.WaitAsync(_stop.Token);
            try
            {
                result.Started = DateTime.UtcNow;
                var call = new TestCall(this, t, result);
                _active = call;
                await call.RunAsync(_stop.Token);
            }
            catch (OperationCanceledException) { result.Outcome = "failed"; result.Findings.Add("Stopped."); }
            catch (Exception e) { result.Outcome = "failed"; result.Findings.Add($"Test error: {e.Message}"); Log.Write($"Test '{t.Name}' failed: {e}"); }
            finally
            {
                result.Ended ??= DateTime.UtcNow;
                _active = null;
                _oneAtATime.Release();
                lock (_gate) SaveResults();
                Log.Write($"Test '{t.Name}': {result.Outcome}" + (result.Findings.Count > 0 ? $" ({result.Findings[0]})" : ""));
            }
        });
        return (true, "Test started.", result.Id);
    }

    /// <summary>Runs a test definition that is not saved (bulk lines) and waits for its result.</summary>
    public async Task<TestResult> RunAndWait(TestDefinition t, string trigger, string testId, CancellationToken ct)
    {
        if (_sip is null) throw new InvalidOperationException(ListenError ?? "The test caller is not running.");
        var result = new TestResult { TestId = testId, Name = t.Name, Target = $"{t.To}@{(t.ToDomain.Length > 0 ? t.ToDomain : t.SbcIp)} via {t.SbcIp}:{t.SbcPort}", Trigger = trigger, Started = DateTime.UtcNow, Codec = t.Codec, Stage = "queued" };
        lock (_gate) AddResult(result);
        await _oneAtATime.WaitAsync(ct);
        try
        {
            result.Started = DateTime.UtcNow;
            var call = new TestCall(this, t, result);
            _active = call;
            await call.RunAsync(ct);
        }
        catch (OperationCanceledException) { result.Outcome = "failed"; result.Findings.Insert(0, "Stopped."); result.Stage = "done"; }
        catch (Exception e) { result.Outcome = "failed"; result.Findings.Insert(0, $"Test error: {e.Message}"); result.Stage = "done"; }
        finally
        {
            result.Ended ??= DateTime.UtcNow;
            _active = null;
            _oneAtATime.Release();
            lock (_gate) SaveResults();
        }
        return result;
    }

    public object Results(string? testId, int limit)
    {
        lock (_gate)
            return _results.Where(r => testId is null ? !r.TestId.StartsWith("bulk-", StringComparison.Ordinal) : r.TestId == testId).Reverse().Take(Math.Clamp(limit, 1, 500)).Select(Summary).ToList();
    }

    public TestResult? Result(string id)
    {
        lock (_gate) return _results.FirstOrDefault(r => r.Id == id);
    }

    private static object Summary(TestResult r) => new
    {
        r.Id, r.TestId, r.Name, r.Target, r.Trigger, r.Started, r.Ended, r.Outcome, r.Stage, r.FinalStatus, r.FinalReason,
        r.AnswerMs, r.MosRx, r.RxLossPercent, r.RxJitterMs, r.TxPackets, r.RxPackets, r.ToneReturned, r.CallId,
        finding = r.Findings.FirstOrDefault() ?? "",
    };

    private void AddResult(TestResult r)
    {
        _results.Add(r);
        if (_results.Count > MaxResults) _results.RemoveRange(0, _results.Count - MaxResults);
    }

    private void SaveTests()
    {
        try { File.WriteAllText(TestsPath, JsonSerializer.Serialize(_tests, Json)); }
        catch (Exception e) { Log.Write($"tests.json could not be saved: {e.Message}"); }
    }

    private void SaveResults()
    {
        try { File.WriteAllText(ResultsPath, JsonSerializer.Serialize(_results, Json)); }
        catch (Exception e) { Log.Write($"test-results.json could not be saved: {e.Message}"); }
    }

    private static string NormalizeLineType(string raw)
    {
        string s = raw.Trim().ToUpperInvariant();
        return s is "ARD" or "MRD" or "HOOT" or "DDI" ? s : "";
    }

    private static string NormalizeRingSignal(string raw)
    {
        string s = raw.Trim().ToLowerInvariant();
        return s is "info" or "none" ? s : "dtmf";
    }

    [GeneratedRegex(@"^[A-Za-z0-9+*#._-]{1,64}$")]
    private static partial Regex SafeUser();

    [GeneratedRegex(@"^[A-Za-z0-9.-]{1,253}$")]
    private static partial Regex SafeHost();

    private async Task Scheduler(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(20), ct); } catch (OperationCanceledException) { return; }
            List<TestDefinition> due;
            lock (_gate)
                due = _tests.Where(t => t.RepeatMinutes > 0 &&
                    (_results.LastOrDefault(r => r.TestId == t.Id) is not { } last ||
                     DateTime.UtcNow - last.Started >= TimeSpan.FromMinutes(t.RepeatMinutes))).ToList();
            foreach (var t in due)
            {
                if (_active is not null) break;
                Run(t.Id, "scheduled");
            }
        }
    }

    // ------------------------------------------------------------ SIP transport

    private async Task SipLoop(CancellationToken ct)
    {
        byte[] buffer = new byte[65535];
        var any = new IPEndPoint(IPAddress.Any, 0);
        while (!ct.IsCancellationRequested && _sip is not null)
        {
            SocketReceiveFromResult r;
            try { r = await _sip.ReceiveFromAsync(buffer, SocketFlags.None, any, ct); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }
            string text = Encoding.UTF8.GetString(buffer, 0, r.ReceivedBytes);
            if (text.Trim().Length == 0) continue; // keep-alive
            try
            {
                // A message for the running test goes to it; anything else (a call looping back
                // through the SBC to be echoed) goes to the echo answerer.
                var from = (IPEndPoint)r.RemoteEndPoint;
                string callId = SipText.Parse(text).CallId;
                if (_active is { } a && a.CallId == callId) a.OnSip(text, from);
                else if (!(_echo?.Handle(text, from) ?? false)) _active?.OnSip(text, from);
            }
            catch (Exception e) { Log.Write($"Test caller: bad SIP from {r.RemoteEndPoint}: {e.Message}"); }
        }
    }

    private void SendSip(string text, IPEndPoint to) => _sip?.SendTo(Encoding.UTF8.GetBytes(text), to);

    private readonly Dictionary<string, byte[]> _recordings = new();

    /// <summary>A call that is our own running test coming back round a loop (same calling number): echo it.</summary>
    private bool IsOwnLoop(SipText.Message m)
    {
        var active = _active;
        if (active is null) return false;
        string from = Regex.Match(m.From, @"sips?:([^@;>]+)") is { Success: true } x ? x.Groups[1].Value : "";
        string ours = active.From;
        return from.Length > 0 && (from == ours || from.EndsWith(ours, StringComparison.Ordinal) || ours.EndsWith(from, StringComparison.Ordinal));
    }

    private void OnTestLine(TestResult r, short[] recording)
    {
        lock (_gate)
        {
            AddResult(r);
            if (recording.Length > 0)
            {
                _recordings[r.Id] = TestLine.Wav(recording);
                foreach (string old in _recordings.Keys.Where(k => !_results.Any(x => x.Id == k)).ToList()) _recordings.Remove(old);
                while (_recordings.Count > 30) _recordings.Remove(_recordings.Keys.First());
            }
            SaveResults();
        }
    }

    public byte[]? Recording(string id)
    {
        lock (_gate) return _recordings.TryGetValue(id, out byte[]? wav) ? wav : null;
    }


    /// <summary>Calls are echoed only for listed SBCs, SBCs a test points at, and this machine.</summary>
    private bool KnownSbc(IPAddress a)
    {
        if (IPAddress.IsLoopback(a)) return true;
        string ip = a.ToString();
        if (_settings.Sbcs.Any(s => s.Ip == ip)) return true;
        lock (_gate) return _tests.Any(t => t.SbcIp == ip);
    }

    private int NextRtpPort()
    {
        lock (_gate)
        {
            int port = _nextRtp;
            _nextRtp += 2;
            if (_nextRtp >= _settings.TestRtpPortFirst + _settings.TestRtpPortCount) _nextRtp = _settings.TestRtpPortFirst;
            return port;
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _sip?.Dispose();
    }

    // ------------------------------------------------------------ one call

    private sealed class TestCall
    {
        private readonly SipTester _owner;
        private readonly TestDefinition _t;
        public readonly TestResult Result;
        private readonly IPEndPoint _sbc;
        private readonly string _localIp;
        private readonly int _localSipPort;
        private readonly string _callId;
        private readonly string _fromTag = Rand(8);
        private readonly System.Collections.Concurrent.ConcurrentQueue<(string text, IPEndPoint from)> _inbox = new();
        private readonly SemaphoreSlim _signal = new(0);
        private int _cseq = 1;
        private string _branch = "";
        private string _toTag = "";
        private string _remoteTarget = "";
        private List<string> _routes = [];
        private string _lastInvite = "";
        private DateTime _t0;
        private Socket? _rtp;
        private int _rtpPort;

        public string CallId => _callId;
        public string From => _t.From;

        public TestCall(SipTester owner, TestDefinition t, TestResult result)
        {
            _owner = owner;
            _t = t;
            Result = result;
            _sbc = new IPEndPoint(IPAddress.Parse(t.SbcIp), t.SbcPort);
            _localIp = LocalAddressFor(_sbc);
            _localSipPort = owner._settings.TestSipPort;
            _callId = $"{Rand(16)}@{_localIp}";
            Result.CallId = _callId;
        }

        public void OnSip(string text, IPEndPoint from)
        {
            _inbox.Enqueue((text, from));
            _signal.Release();
        }

        private async Task<(SipText.Message m, string raw, IPEndPoint from)?> Next(TimeSpan wait, CancellationToken ct)
        {
            if (!await _signal.WaitAsync(wait, ct)) return null;
            if (!_inbox.TryDequeue(out var item)) return null;
            SipText.Message m = SipText.Parse(item.text.TrimEnd('\0'));
            Log(false, m.FirstLine, item.text);
            return (m, item.text, item.from);
        }

        private void Log(bool outbound, string first, string text)
        {
            lock (Result) Result.Ladder.Add(new LadderEntry { Time = DateTime.UtcNow, Outbound = outbound, First = first, Text = text });
        }

        private void Send(string text, IPEndPoint? to = null)
        {
            _owner.SendSip(text, to ?? _sbc);
            Log(true, text[..text.IndexOf('\r')], text);
        }

        private double Ms() => (DateTime.UtcNow - _t0).TotalMilliseconds;

        public async Task RunAsync(CancellationToken ct)
        {
            _t0 = DateTime.UtcNow;
            _rtpPort = _owner.NextRtpPort();
            _rtp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                _rtp.Bind(new IPEndPoint(IPAddress.Any, _rtpPort));
                Result.LocalMedia = $"{_localIp}:{_rtpPort}";
                await Dial(ct);
            }
            finally
            {
                _rtp.Dispose();
                Result.Ended = DateTime.UtcNow;
                Finish();
            }
        }

        private string RequestUri => $"sip:{_t.To}@{(_t.ToDomain.Length > 0 ? _t.ToDomain : _t.SbcIp + (_t.SbcPort != 5060 ? ":" + _t.SbcPort : ""))}";

        private string Sdp()
        {
            string pt = _t.Codec == "PCMU" ? "0" : "8";
            long session = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"v=0\r\no=MediantTrace {session} {session} IN IP4 {_localIp}\r\ns=MediantTrace test call\r\nc=IN IP4 {_localIp}\r\nt=0 0\r\n" +
                   $"m=audio {_rtpPort} RTP/AVP {pt} 101\r\na=rtpmap:{pt} {_t.Codec}/8000\r\na=rtpmap:101 telephone-event/8000\r\na=fmtp:101 0-15\r\na=ptime:20\r\na=sendrecv\r\n";
        }

        private string Invite(string? auth)
        {
            _branch = "z9hG4bK" + Rand(12);
            string sdp = Sdp();
            return $"INVITE {RequestUri} SIP/2.0\r\n" +
                   $"Via: SIP/2.0/UDP {_localIp}:{_localSipPort};branch={_branch};rport\r\n" +
                   "Max-Forwards: 70\r\n" +
                   $"From: \"MediantTrace\" <sip:{_t.From}@{_localIp}>;tag={_fromTag}\r\n" +
                   $"To: <{RequestUri}>\r\n" +
                   $"Call-ID: {_callId}\r\n" +
                   $"CSeq: {_cseq} INVITE\r\n" +
                   $"Contact: <sip:{_t.From}@{_localIp}:{_localSipPort}>\r\n" +
                   (auth ?? "") +
                   "Allow: INVITE, ACK, BYE, CANCEL, OPTIONS, INFO, UPDATE\r\n" +
                   "User-Agent: MediantTrace\r\n" +
                   "Content-Type: application/sdp\r\n" +
                   $"Content-Length: {Encoding.UTF8.GetByteCount(sdp)}\r\n\r\n{sdp}";
        }

        private async Task Dial(CancellationToken ct)
        {
            bool authTried = false;
            string? auth = null;
            while (true)
            {
                Result.Stage = "calling";
                _lastInvite = Invite(auth);
                Send(_lastInvite);
                DateTime sent = DateTime.UtcNow;
                double retransmit = 500;
                DateTime nextRetransmit = sent.AddMilliseconds(retransmit);
                bool provisional = false;
                DateTime ringDeadline = DateTime.MaxValue;

                while (true)
                {
                    if (!provisional && DateTime.UtcNow - sent > TimeSpan.FromSeconds(32))
                    {
                        Fail("No response to the INVITE from the SBC within 32 s. Check the address, port and that the SBC accepts calls from this server.");
                        return;
                    }
                    if (!provisional && _t.RingSeconds > 0 && _t.LineType.Length > 0 && DateTime.UtcNow - sent > TimeSpan.FromSeconds(Math.Max(_t.RingSeconds, 32)))
                    {
                        await Cancel(ct);
                        Fail($"No answer within {Math.Max(_t.RingSeconds, 32)} s; the call was cancelled.");
                        return;
                    }
                    if (provisional && DateTime.UtcNow > ringDeadline)
                    {
                        await Cancel(ct);
                        Fail(_t.LineType == "HOOT"
                            ? $"No automatic answer within {_t.RingSeconds} s: a Hoot line should answer by itself. The call was cancelled."
                            : $"Rang for {_t.RingSeconds} s but nobody picked up; the call was cancelled.");
                        return;
                    }
                    if (!provisional && DateTime.UtcNow >= nextRetransmit)
                    {
                        _owner.SendSip(_lastInvite, _sbc);
                        retransmit = Math.Min(retransmit * 2, 4000);
                        nextRetransmit = DateTime.UtcNow.AddMilliseconds(retransmit);
                    }
                    var item = await Next(TimeSpan.FromMilliseconds(200), ct);
                    if (item is not { } x) continue;
                    var (m, raw, _) = x;
                    if (m.IsRequest) { HandleRequest(m, raw, x.from); continue; }
                    if (m.CallId != _callId || m.CSeqMethod != "INVITE") continue;

                    if (m.Status < 200)
                    {
                        if (!provisional) { provisional = true; ringDeadline = _t.RingSeconds > 0 ? sent.AddSeconds(_t.RingSeconds) : DateTime.UtcNow.AddSeconds(60); }
                        if (m.Status == 100) Result.TryingMs ??= Math.Round(Ms(), 0);
                        else { Result.RingingMs ??= Math.Round(Ms(), 0); Result.Stage = "ringing"; }
                        if (m.Sdp is not null) Result.RemoteMedia = $"{m.Sdp.ConnectionIp}:{m.Sdp.AudioPort}";
                        continue;
                    }

                    Result.FinalStatus = m.Status;
                    Result.FinalReason = m.Reason;
                    if (m.Status >= 300)
                    {
                        Ack(raw, inviteTransaction: true);
                        if ((m.Status == 401 || m.Status == 407) && !authTried && _t.Username.Length > 0)
                        {
                            auth = Authorization(raw, m.Status);
                            authTried = true;
                            _cseq++;
                            if (auth is not null) break; // re-send the INVITE with credentials
                        }
                        if (m.Status == 486 && _t.LineType is "MRD" or "HOOT")
                        {
                            // An always-on line rejects a second call while it holds its wire call.
                            Result.Outcome = "passed";
                            Result.Stage = "done";
                            Result.Findings.Insert(0, $"Busy (486): the {_t.LineType} line is holding its always-on wire call, so the wire is connected.");
                            return;
                        }
                        Fail(m.Status is 401 or 407
                            ? $"The SBC asked for a login ({m.Status} {m.Reason}). {(_t.Username.Length == 0 ? "Add a username and password to the test." : "The username or password was refused.")}"
                            : $"Call rejected: {m.Status} {m.Reason}.");
                        return;
                    }

                    // Answered.
                    Result.AnswerMs = Math.Round(Ms(), 0);
                    _toTag = Tag(Header(raw, "To", "t"));
                    _remoteTarget = UriIn(Header(raw, "Contact", "m")) ?? RequestUri;
                    _routes = HeaderAll(raw, "Record-Route").SelectMany(h => h.Split(',')).Select(r => r.Trim()).Reverse().ToList();
                    if (m.Sdp is null || m.Sdp.AudioPort <= 0)
                    {
                        Ack(raw, inviteTransaction: false);
                        await Bye(ct);
                        Fail("The call was answered without usable audio in the SDP.");
                        return;
                    }
                    Result.RemoteMedia = $"{m.Sdp.ConnectionIp}:{m.Sdp.AudioPort}";
                    Ack(raw, inviteTransaction: false);
                    await Talk(new IPEndPoint(IPAddress.Parse(m.Sdp.ConnectionIp), m.Sdp.AudioPort), ct);
                    return;
                }
            }
        }

        private void HandleRequest(SipText.Message m, string raw, IPEndPoint from)
        {
            if (m.CallId != _callId) { Send(Response(raw, 481, "Call/Transaction Does Not Exist"), from); return; }
            switch (m.Method)
            {
                case "BYE":
                    Send(Response(raw, 200, "OK"), from);
                    _farEndHungUp = true;
                    break;
                case "INVITE" or "UPDATE":
                    string sdp = Sdp();
                    Send(Response(raw, 200, "OK", sdp), from);
                    break;
                case "ACK":
                    break;
                case "OPTIONS" or "INFO" or "NOTIFY":
                    Send(Response(raw, 200, "OK"), from);
                    break;
                default:
                    Send(Response(raw, 501, "Not Implemented"), from);
                    break;
            }
        }

        private volatile bool _farEndHungUp;

        private string Response(string request, int code, string reason, string? sdp = null)
        {
            var sb = new StringBuilder($"SIP/2.0 {code} {reason}\r\n");
            foreach (string via in HeaderAll(request, "Via", "v")) sb.Append($"Via: {via}\r\n");
            foreach (string rr in HeaderAll(request, "Record-Route")) sb.Append($"Record-Route: {rr}\r\n");
            sb.Append($"From: {Header(request, "From", "f")}\r\n");
            string to = Header(request, "To", "t");
            sb.Append($"To: {to}{(Tag(to).Length == 0 ? ";tag=" + _fromTag : "")}\r\n");
            sb.Append($"Call-ID: {Header(request, "Call-ID", "i")}\r\n");
            sb.Append($"CSeq: {Header(request, "CSeq")}\r\n");
            sb.Append($"Contact: <sip:{_t.From}@{_localIp}:{_localSipPort}>\r\n");
            sb.Append("User-Agent: MediantTrace\r\n");
            if (sdp is not null) sb.Append("Content-Type: application/sdp\r\n");
            sb.Append($"Content-Length: {(sdp is null ? 0 : Encoding.UTF8.GetByteCount(sdp))}\r\n\r\n{sdp}");
            return sb.ToString();
        }

        /// <summary>ACK: same transaction for a failure, a new one for a 2xx (RFC 3261 17.1.1.3, 13.2.2.4).</summary>
        private void Ack(string response, bool inviteTransaction)
        {
            string to = Header(response, "To", "t");
            string uri = inviteTransaction ? RequestUri : _remoteTarget;
            string branch = inviteTransaction ? _branch : "z9hG4bK" + Rand(12);
            string routes = inviteTransaction ? "" : string.Concat(_routes.Select(r => $"Route: {r}\r\n"));
            Send($"ACK {uri} SIP/2.0\r\nVia: SIP/2.0/UDP {_localIp}:{_localSipPort};branch={branch};rport\r\nMax-Forwards: 70\r\n" +
                 $"From: \"MediantTrace\" <sip:{_t.From}@{_localIp}>;tag={_fromTag}\r\nTo: {to}\r\nCall-ID: {_callId}\r\nCSeq: {_cseq} ACK\r\n{routes}Content-Length: 0\r\n\r\n");
        }

        private async Task Cancel(CancellationToken ct)
        {
            Send($"CANCEL {RequestUri} SIP/2.0\r\nVia: SIP/2.0/UDP {_localIp}:{_localSipPort};branch={_branch};rport\r\nMax-Forwards: 70\r\n" +
                 $"From: \"MediantTrace\" <sip:{_t.From}@{_localIp}>;tag={_fromTag}\r\nTo: <{RequestUri}>\r\nCall-ID: {_callId}\r\nCSeq: {_cseq} CANCEL\r\nContent-Length: 0\r\n\r\n");
            DateTime until = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < until)
            {
                var item = await Next(TimeSpan.FromMilliseconds(300), ct);
                if (item is { } x && !x.m.IsRequest && x.m.CSeqMethod == "INVITE" && x.m.Status >= 300) { Ack(x.raw, true); return; }
            }
        }

        /// <summary>MRD ring / stop inside the live call, the way the dealerboard sends it.</summary>
        private void SendSignal(string signal)
        {
            _cseq++;
            bool dtmf = _t.RingSignal != "info";
            string body = dtmf ? $"Signal={(signal == "ring" ? "*" : "#")}\r\nDuration=200\r\n" : $"signal={signal}\r\n";
            string type = dtmf ? "application/dtmf-relay" : "application/x-tradecom-signal";
            string routes = string.Concat(_routes.Select(r => $"Route: {r}\r\n"));
            Send($"INFO {_remoteTarget} SIP/2.0\r\nVia: SIP/2.0/UDP {_localIp}:{_localSipPort};branch=z9hG4bK{Rand(12)};rport\r\nMax-Forwards: 70\r\n" +
                 $"From: \"MediantTrace\" <sip:{_t.From}@{_localIp}>;tag={_fromTag}\r\nTo: <{RequestUri}>;tag={_toTag}\r\nCall-ID: {_callId}\r\nCSeq: {_cseq} INFO\r\n{routes}" +
                 $"Contact: <sip:{_t.From}@{_localIp}:{_localSipPort}>\r\nContent-Type: {type}\r\nContent-Length: {System.Text.Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}");
            if (signal == "ring") Result.SignalSent = dtmf ? "DTMF * (SIP INFO dtmf-relay)" : "SIP INFO signal=ring";
        }

        private async Task Bye(CancellationToken ct)
        {
            _cseq++;
            string to = $"<{RequestUri}>;tag={_toTag}";
            string routes = string.Concat(_routes.Select(r => $"Route: {r}\r\n"));
            string bye = $"BYE {_remoteTarget} SIP/2.0\r\nVia: SIP/2.0/UDP {_localIp}:{_localSipPort};branch=z9hG4bK{Rand(12)};rport\r\nMax-Forwards: 70\r\n" +
                         $"From: \"MediantTrace\" <sip:{_t.From}@{_localIp}>;tag={_fromTag}\r\nTo: {to}\r\nCall-ID: {_callId}\r\nCSeq: {_cseq} BYE\r\n{routes}User-Agent: MediantTrace\r\nContent-Length: 0\r\n\r\n";
            Send(bye);
            double wait = 500;
            DateTime until = DateTime.UtcNow.AddSeconds(8), next = DateTime.UtcNow.AddMilliseconds(wait);
            while (DateTime.UtcNow < until)
            {
                if (DateTime.UtcNow >= next) { _owner.SendSip(bye, _sbc); wait = Math.Min(wait * 2, 4000); next = DateTime.UtcNow.AddMilliseconds(wait); }
                var item = await Next(TimeSpan.FromMilliseconds(200), ct);
                if (item is not { } x) continue;
                if (x.m.IsRequest) { HandleRequest(x.m, x.raw, x.from); continue; }
                if (x.m.CSeqMethod == "BYE" && x.m.Status >= 200) return;
                if (x.m.CSeqMethod == "INVITE" && x.m.Status is >= 200 and < 300) Ack(x.raw, false); // retransmitted 200
            }
            Result.Findings.Add("The far end did not confirm the hang-up (no 200 OK to BYE).");
        }

        // ------------------------------------------------------------ media

        private async Task Talk(IPEndPoint remote, CancellationToken ct)
        {
            Result.Stage = "connected";
            var rx = new RxStats();
            DateTime answered = DateTime.UtcNow;
            using var talkCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // A dedicated, high-priority thread stamps each packet the moment it lands.
            var receive = new Thread(() => ReceiveRtp(rx, answered, talkCts.Token)) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "test-rtp" };
            receive.Start();

            bool alaw = _t.Codec != "PCMU";
            byte pt = (byte)(alaw ? 8 : 0);
            uint ssrc = (uint)Random.Shared.Next();
            ushort seq = (ushort)Random.Shared.Next(1, 30000);
            uint ts = (uint)Random.Shared.Next();
            double phase = 0, step = 2 * Math.PI * ToneHz / 8000;
            byte[] packet = new byte[12 + 160];
            var clock = System.Diagnostics.Stopwatch.StartNew();
            long frame = 0;
            int total = _t.DurationSeconds * 50;
            // MRD: the always-on line answers silently; the ring is a signal inside the call. Ring, then wait
            // for a person to join (speech coming back), hold, stop the ring and hang up.
            bool mrd = _t.LineType == "MRD" && _t.RingSignal != "none";
            short[]? voice = null;
            int voiceAt = 0;
            long personFrame = -1, lastSpeech = 0;
            if (mrd)
            {
                voice = TestLine.FloorAnnouncement();
                total = Math.Max(total, Math.Max(5, _t.RingSeconds) * 50);
                SendSignal("ring");
                Result.Stage = "voice message";
                Result.Findings.Add($"Played a {voice.Length / 8000.0:0.0} s voice message to the line for the voice team.");
            }

            // Windows' default timer ticks every ~15.6 ms; ask for 1 ms during the call so the
            // 20 ms packets leave evenly (otherwise our own sending would look like network jitter).
            TimeBeginPeriod(1);
            try
            {
            while (frame < total && !_farEndHungUp && !ct.IsCancellationRequested)
            {
                // Pace to 20 ms from the start, not from the last send, so timing does not drift.
                double due = frame * 20.0 - clock.Elapsed.TotalMilliseconds;
                if (due > 2) await Task.Delay(TimeSpan.FromMilliseconds(due - 1.5), ct);
                while (frame * 20.0 - clock.Elapsed.TotalMilliseconds > 0.05) Thread.SpinWait(50);
                packet[0] = 0x80;
                packet[1] = (byte)(pt | (frame == 0 ? 0x80 : 0)); // marker on the first packet
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), seq++);
                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4), ts);
                BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(8), ssrc);
                ts += 160;
                for (int i = 0; i < 160; i++)
                {
                    short sample;
                    if (voice is not null)
                    {
                        sample = voice[voiceAt];
                        if (++voiceAt >= voice.Length) voiceAt = 0;
                    }
                    else
                    {
                        sample = (short)(Math.Sin(phase) * 7300); // about -13 dBov
                        phase += step;
                    }
                    packet[12 + i] = alaw ? G711.LinearToAlaw(sample) : G711.LinearToUlaw(sample);
                }
                if (phase > 2 * Math.PI) phase -= 2 * Math.PI * Math.Floor(phase / (2 * Math.PI));
                try { _rtp!.SendTo(packet, remote); } catch (SocketException) { }
                Result.TxPackets++;
                int second = (int)(frame / 50);
                lock (Result) { while (Result.TxPerSecond.Count <= second) Result.TxPerSecond.Add(0); Result.TxPerSecond[second]++; }
                frame++;
                if (frame % 50 == 0) { Result.TalkSeconds = frame / 50; ApplyRx(rx); }
                if (mrd && personFrame < 0 && frame % 25 == 0)
                {
                    long speech;
                    lock (rx) speech = rx.AudioFrames - rx.ToneFrames; // sound that is not our own tone
                    if (speech - lastSpeech >= 8)                      // 160 ms of speech in half a second
                    {
                        personFrame = frame;
                        Result.PersonAnsweredMs = Math.Round((DateTime.UtcNow - answered).TotalMilliseconds, 0);
                        Result.Stage = "connected";
                        total = (int)frame + Math.Max(1, _t.DurationSeconds) * 50; // hold, then hang up
                    }
                    lastSpeech = speech;
                }

                // Answer anything the far end sends meanwhile (BYE, re-INVITE, OPTIONS).
                while (_inbox.TryPeek(out _))
                {
                    var item = await Next(TimeSpan.Zero, ct);
                    if (item is not { } x) break;
                    if (x.m.IsRequest) HandleRequest(x.m, x.raw, x.from);
                    else if (x.m.CSeqMethod == "INVITE" && x.m.Status is >= 200 and < 300) Ack(x.raw, false);
                    else if (x.m.CSeqMethod == "INFO" && x.m.Status >= 200 && Result.SignalResponse == 0) Result.SignalResponse = x.m.Status;
                }
            }
            }
            finally { TimeEndPeriod(1); }
            if (mrd && !_farEndHungUp) SendSignal("stop");

            Result.TalkSeconds = Math.Round((DateTime.UtcNow - answered).TotalSeconds, 1);
            await Task.Delay(300, ct); // let the last packets arrive
            talkCts.Cancel();
            receive.Join(1000);
            ApplyRx(rx);
            if (_farEndHungUp) Result.Findings.Add($"The far end hung up after {Result.TalkSeconds:0} s.");
            else { Result.Stage = "hanging up"; await Bye(ct); }
        }

        private sealed class RxStats
        {
            public long Packets, Frames, AudioFrames, ToneFrames;
            public double EnergyDbSum;
            public bool SeqInit;
            public ushort MaxSeq;
            public uint Cycles, BaseSeq;
            public double Jitter, LastTransit, MaxGapMs, LastArrival = -1;
            public bool HaveTransit;
            public double? FirstAudioMs;
            public string From = "";
            public List<int> PerSecond = [];
            public long Expected => SeqInit ? (long)(Cycles + MaxSeq) - BaseSeq + 1 : 0;
        }

        private void ReceiveRtp(RxStats rx, DateTime answered, CancellationToken ct)
        {
            byte[] buffer = new byte[2048];
            EndPoint any = new IPEndPoint(IPAddress.Any, 0);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            short[] pcm = new short[1024];
            _rtp!.ReceiveTimeout = 200;
            while (!ct.IsCancellationRequested)
            {
                int n;
                EndPoint from = any;
                try { n = _rtp.ReceiveFrom(buffer, ref from); }
                catch (ObjectDisposedException) { return; }
                catch (SocketException) { continue; } // timeout, or ICMP unreachable echoed back
                var r = (RemoteEndPoint: from, ReceivedBytes: n);
                if (n < 12 || buffer[0] >> 6 != 2 || buffer[1] is >= 200 and <= 204) continue;
                int pt = buffer[1] & 0x7F;
                double now = clock.Elapsed.TotalMilliseconds;
                lock (rx)
                {
                    rx.Packets++;
                    if (rx.From.Length == 0) rx.From = r.RemoteEndPoint.ToString()!;
                    ushort seq = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2));
                    uint ts = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(4));
                    if (!rx.SeqInit) { rx.SeqInit = true; rx.BaseSeq = seq; rx.MaxSeq = seq; }
                    else
                    {
                        ushort delta = (ushort)(seq - rx.MaxSeq);
                        if (delta < 3000) { if (seq < rx.MaxSeq) rx.Cycles += 65536; rx.MaxSeq = seq; }
                    }
                    if (rx.LastArrival >= 0) rx.MaxGapMs = Math.Max(rx.MaxGapMs, now - rx.LastArrival);
                    rx.LastArrival = now;
                    int second = (int)((DateTime.UtcNow - answered).TotalSeconds);
                    if (second >= 0) { while (rx.PerSecond.Count <= second) rx.PerSecond.Add(0); rx.PerSecond[second]++; }
                    if (pt is 0 or 8)
                    {
                        double transit = now * 8.0 - ts;
                        if (rx.HaveTransit) { double d = Math.Abs(transit - rx.LastTransit); if (d < 8000) rx.Jitter += (d - rx.Jitter) / 16.0; }
                        rx.LastTransit = transit; rx.HaveTransit = true;

                        // Listen to the audio: level, and whether it is our tone coming back.
                        int samples = Math.Min(n - 12, pcm.Length);
                        double sum = 0;
                        for (int i = 0; i < samples; i++)
                        {
                            pcm[i] = pt == 8 ? G711.AlawToLinear(buffer[12 + i]) : G711.UlawToLinear(buffer[12 + i]);
                            sum += (double)pcm[i] * pcm[i];
                        }
                        if (samples > 0)
                        {
                            rx.Frames++;
                            double rms = Math.Sqrt(sum / samples);
                            double db = 20 * Math.Log10(Math.Max(rms, 1) / 32768.0);
                            if (db > -50)
                            {
                                rx.AudioFrames++;
                                rx.EnergyDbSum += db;
                                rx.FirstAudioMs ??= Math.Round((DateTime.UtcNow - answered).TotalMilliseconds, 0);
                                if (ToneShare(pcm.AsSpan(0, samples), sum) > 0.6) rx.ToneFrames++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Share of the frame's energy at the test-tone frequency (Goertzel).</summary>
        private static double ToneShare(ReadOnlySpan<short> x, double totalEnergy)
        {
            double k = 2 * Math.Cos(2 * Math.PI * ToneHz / 8000);
            double s1 = 0, s2 = 0;
            foreach (short v in x) { double s0 = v + k * s1 - s2; s2 = s1; s1 = s0; }
            double power = s1 * s1 + s2 * s2 - k * s1 * s2;   // |X(f)|^2
            double toneEnergy = 2 * power / x.Length;          // energy of a sinusoid at f across the frame
            return totalEnergy > 0 ? Math.Min(1, toneEnergy / totalEnergy) : 0;
        }

        private void ApplyRx(RxStats rx)
        {
            lock (rx)
            {
                Result.RxPackets = rx.Packets;
                Result.RxLost = Math.Max(0, rx.Expected - rx.Packets);
                Result.RxLossPercent = rx.Expected > 0 ? Math.Round(100.0 * Result.RxLost / rx.Expected, 2) : 0;
                Result.RxJitterMs = Math.Round(rx.Jitter / 8.0, 1);
                Result.RxMaxGapMs = Math.Round(rx.MaxGapMs, 0);
                Result.FirstAudioMs = rx.FirstAudioMs;
                Result.AudioFrom = rx.From;
                Result.RxAudioPercent = rx.Frames > 0 ? Math.Round(100.0 * rx.AudioFrames / rx.Frames, 1) : 0;
                Result.RxLevelDb = rx.AudioFrames > 0 ? Math.Round(rx.EnergyDbSum / rx.AudioFrames, 1) : -99;
                Result.ToneReturned = rx.AudioFrames > 0 && rx.ToneFrames > rx.AudioFrames * 0.5;
                Result.RxPerSecond = [.. rx.PerSecond];
                double oneWay = (Result.TryingMs ?? 40) / 2;
                Result.MosRx = rx.Packets >= 25 ? Quality.Mos(Result.RxLossPercent, Result.RxJitterMs, oneWay) : null;
            }
        }

        private void Finish()
        {
            if (Result.Outcome is "failed") return;
            var f = Result.Findings;
            string outcome = "passed";
            if (_t.LineType == "MRD" && _t.RingSignal != "none")
            {
                string ack = Result.SignalResponse == 0 ? "no reply from the line" : $"{Result.SignalResponse} from the line";
                if (Result.SignalResponse >= 300) outcome = "failed";
                if (Result.PersonAnsweredMs is double pa)
                    f.Add($"MRD rang ({Result.SignalSent}; {ack}) and a person answered after {pa / 1000:0.0} s; held {_t.DurationSeconds} s with the test tone, ring stopped, hung up.");
                else
                {
                    f.Add($"MRD ring sent ({Result.SignalSent}; {ack}) but nobody answered within {_t.RingSeconds} s (no speech came back).");
                    if (outcome != "failed") outcome = "warning";
                }
                if (Result.SignalResponse >= 300) f.Insert(0, $"The line refused the ring signal ({Result.SignalResponse}): check the wire's ring signalling (DTMF or INFO).");
                if (Result.RxLossPercent > 2) { f.Add($"Packet loss on the audio coming back: {Result.RxLossPercent}%."); if (outcome == "passed") outcome = "warning"; }
                Result.Outcome = outcome;
                Result.Stage = "done";
                return;
            }
            if (_t.LineType.Length > 0)
            {
                string how = _t.LineType == "HOOT" ? "answered automatically" : _t.LineType == "ARD" ? "was picked up" : "was answered";
                f.Add($"{_t.LineType} line {how} after {(Result.AnswerMs ?? 0) / 1000:0.0} s; held {Result.TalkSeconds:0} s with the test tone, then hung up.");
                if (Result.RxPackets == 0)
                {
                    f.Insert(0, "Answered, but no audio came back from the line (one-way audio).");
                    outcome = "warning";
                }
                else if (Result.RxAudioPercent < 5) f.Add("Silence came back from the line: normal unless someone was speaking.");
                else f.Add($"Sound came back from the line ({Result.RxAudioPercent}% of frames, {Result.RxLevelDb} dBov).");
                if (Result.RxLossPercent > 2) { f.Add($"Packet loss on the audio coming back: {Result.RxLossPercent}%."); outcome = "warning"; }
                Result.Outcome = outcome;
                Result.Stage = "done";
                return;
            }
            if (Result.RxPackets == 0)
            {
                f.Insert(0, "One-way audio: no audio packets came back from the far end. Our audio was sent (" + Result.TxPackets + " packets).");
                outcome = "failed";
            }
            else
            {
                if (Result.RxAudioPercent < 5)
                {
                    f.Add($"Audio packets came back but they were silent ({Result.RxAudioPercent}% with sound).");
                    outcome = "warning";
                }
                if (Result.RxLossPercent > 2) { f.Add($"Packet loss on the audio coming back: {Result.RxLossPercent}%."); outcome = "warning"; }
                if (Result.RxJitterMs > 30) { f.Add($"High jitter on the audio coming back: {Result.RxJitterMs} ms."); outcome = "warning"; }
                if (Result.RxMaxGapMs > 1000) { f.Add($"Audio coming back stopped for {Result.RxMaxGapMs / 1000:0.0} s at least once."); outcome = "warning"; }
                if (Result.MosRx is double mos && mos < 3.6) { f.Add($"Estimated MOS of the audio coming back is {mos:0.0}."); outcome = "warning"; }
                string media = Result.RemoteMedia;
                if (Result.AudioFrom.Length > 0 && media.Length > 0 && Result.AudioFrom != media)
                    f.Add($"Audio came back from {Result.AudioFrom}, not the address in the SDP ({media}): NAT or media latching in the path.");
                f.Add(Result.ToneReturned
                    ? "Our test tone came back: audio is confirmed in both directions."
                    : "Our test tone did not come back, so only the far end → us direction is measured here (call an echo number to test both ways).");
            }
            Result.Outcome = outcome;
            Result.Stage = "done";
        }

        private void Fail(string reason)
        {
            Result.Outcome = "failed";
            Result.Stage = "done";
            Result.Findings.Insert(0, reason);
        }

        // ------------------------------------------------------------ digest

        private string? Authorization(string challengeResponse, int status)
        {
            string header = Header(challengeResponse, status == 407 ? "Proxy-Authenticate" : "WWW-Authenticate");
            if (!header.StartsWith("Digest", StringComparison.OrdinalIgnoreCase)) return null;
            string Param(string name) => Regex.Match(header, name + "=\"?([^\",]*)\"?", RegexOptions.IgnoreCase) is { Success: true } m ? m.Groups[1].Value : "";
            string realm = Param("realm"), nonce = Param("nonce"), opaque = Param("opaque"), qop = Param("qop"), algorithm = Param("algorithm");
            if (algorithm.Length > 0 && !algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase)) return null;
            string password = _t.PasswordProtected.Length == 0 ? "" :
                Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(_t.PasswordProtected), null, DataProtectionScope.LocalMachine));
            string uri = RequestUri;
            string ha1 = Md5($"{_t.Username}:{realm}:{password}");
            string ha2 = Md5($"INVITE:{uri}");
            string cnonce = Rand(16), nc = "00000001";
            bool useQop = qop.Split(',').Select(q => q.Trim()).Contains("auth");
            string response = useQop ? Md5($"{ha1}:{nonce}:{nc}:{cnonce}:auth:{ha2}") : Md5($"{ha1}:{nonce}:{ha2}");
            string value = $"Digest username=\"{_t.Username}\", realm=\"{realm}\", nonce=\"{nonce}\", uri=\"{uri}\", response=\"{response}\", algorithm=MD5" +
                           (useQop ? $", qop=auth, nc={nc}, cnonce=\"{cnonce}\"" : "") + (opaque.Length > 0 ? $", opaque=\"{opaque}\"" : "");
            return $"{(status == 407 ? "Proxy-Authorization" : "Authorization")}: {value}\r\n";
        }

        private static string Md5(string s) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();
    }

    // ------------------------------------------------------------ helpers

    [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint ms);

    [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint ms);

    private static string Rand(int length) => Convert.ToHexString(RandomNumberGenerator.GetBytes(length)).ToLowerInvariant()[..length];

    private static string LocalAddressFor(IPEndPoint target)
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(target); // no packet is sent; this only picks the route
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }

    private static readonly Dictionary<string, string> Compact = new(StringComparer.OrdinalIgnoreCase)
    { ["v"] = "via", ["f"] = "from", ["t"] = "to", ["i"] = "call-id", ["m"] = "contact" };

    private static IEnumerable<string> HeaderLines(string message)
    {
        int end = message.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        string head = end >= 0 ? message[..end] : message;
        return head.Split("\r\n").Skip(1);
    }

    private static string Header(string message, params string[] names) => HeaderAll(message, names).FirstOrDefault() ?? "";

    private static List<string> HeaderAll(string message, params string[] names)
    {
        var wanted = new HashSet<string>(names.Select(n => Compact.TryGetValue(n, out string? full) ? full : n.ToLowerInvariant()));
        var list = new List<string>();
        foreach (string line in HeaderLines(message))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            string name = line[..colon].Trim();
            name = Compact.TryGetValue(name, out string? full) ? full : name.ToLowerInvariant();
            if (wanted.Contains(name)) list.Add(line[(colon + 1)..].Trim());
        }
        return list;
    }

    private static string Tag(string header) => Regex.Match(header, @";\s*tag=([^;>\s]+)", RegexOptions.IgnoreCase) is { Success: true } m ? m.Groups[1].Value : "";

    private static string? UriIn(string header) => Regex.Match(header, @"<([^>]+)>") is { Success: true } m ? m.Groups[1].Value
        : header.Length > 0 ? header.Split(';')[0].Trim() : null;
}
