using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Bulk line tests: a sheet of dealerboard lines (SIP URI and line type) worked
/// through one after another. ARD and DDI lines are called through the SBC and a
/// person must pick up; the call then holds with the test tone and hangs up.
/// MRD and Hoot lines are always-on wires: calling one would replace its live
/// wire call, so instead the live wire call is checked on the SBC (its active
/// call list) and its audio measured from the capture.
/// </summary>
public sealed partial class BulkTests
{
    private readonly SipTester _tester;
    private readonly Recorder _recorder;
    private readonly ChangeManager _changes;
    private readonly Settings _settings;
    private readonly Dictionary<string, (DateTime at, List<SbcCall> calls)> _sbcCalls = new();
    private readonly object _gate = new();
    private List<BulkRun> _runs = [];
    private CancellationTokenSource? _stop;
    private static string RunsPath => Path.Combine(AppContext.BaseDirectory, "bulk-runs.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public sealed class BulkRun
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }
        public string State { get; set; } = "running"; // running, done, stopped
        public string Via { get; set; } = "";          // outbound proxy (SBC) or "" = each URI's host
        public string From { get; set; } = "";
        public List<BulkLine> Lines { get; set; } = [];
    }

    public sealed class BulkLine
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Uri { get; set; } = "";
        public string Type { get; set; } = "";
        public int HoldSeconds { get; set; }
        public int RingSeconds { get; set; }
        public string Signal { get; set; } = "";
        public string Mode { get; set; } = "call";       // "call", or "wire" (check the always-on wire's live call)
        public string Status { get; set; } = "queued";  // queued, calling, checking, ringing, connected, passed, warning, failed, skipped
        public string ResultId { get; set; } = "";
        public string Summary { get; set; } = "";
        public int SipStatus { get; set; }
        public double? AnswerSeconds { get; set; }
        public long RxPackets { get; set; }
        public double? Mos { get; set; }
    }

    public sealed class LineInput
    {
        public string? Name { get; set; }
        public string? Uri { get; set; }
        public string? Type { get; set; }
        public int? HoldSeconds { get; set; }
        public int? RingSeconds { get; set; }
        public string? Signal { get; set; }         // MRD: DTMF or INFO
        public string? Test { get; set; }           // MRD / Hoot: "wire" (default) checks the live wire, "call" dials it
    }

    public sealed class StartInput
    {
        public string? Name { get; set; }
        public string? Via { get; set; }            // "ip" or "ip:port" of the SBC to send every call to; empty = each URI's host
        public string? From { get; set; }
        public int? HoldSeconds { get; set; }       // default for lines without one
        public int? AutoRingSeconds { get; set; }   // ARD: a person picks up
        public int? ManualRingSeconds { get; set; } // MRD / DDI (Hoot when called)
        public string? MrdSignal { get; set; }      // default MRD ring signal: DTMF (default) or INFO
        public List<LineInput>? Lines { get; set; }
    }

    public BulkTests(SipTester tester, Recorder recorder, ChangeManager changes, Settings settings)
    {
        _tester = tester;
        _recorder = recorder;
        _changes = changes;
        _settings = settings;
        try { if (File.Exists(RunsPath)) _runs = JsonSerializer.Deserialize<List<BulkRun>>(File.ReadAllText(RunsPath)) ?? []; }
        catch (Exception e) { Log.Write($"bulk-runs.json could not be read: {e.Message}"); }
        foreach (var r in _runs.Where(r => r.State == "running")) { r.State = "stopped"; foreach (var l in r.Lines.Where(l => l.Status is "queued" or "calling" or "checking" or "ringing" or "connected")) l.Status = "skipped"; }
    }

    [GeneratedRegex(@"^(?:sips?:)?(?<user>[^@;:\s]+)(?:@(?<host>\[[^\]]+\]|[^:;>\s]+)(?::(?<port>\d{1,5}))?)?(?<rest>[;>].*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UriPattern();

    [GeneratedRegex(@"^[A-Za-z0-9+*#._-]{1,64}$")]
    private static partial Regex SafeUser();

    private static readonly Dictionary<string, string> TypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARD"] = "ARD", ["AUTO RINGDOWN"] = "ARD", ["AUTOMATIC RINGDOWN"] = "ARD",
        ["MRD"] = "MRD", ["MANUAL RINGDOWN"] = "MRD",
        ["HOOT"] = "HOOT", ["HOOT N HOLLER"] = "HOOT", ["HOOTNHOLLER"] = "HOOT", ["HOOT & HOLLER"] = "HOOT",
        ["DDI"] = "DDI", ["DID"] = "DDI", ["DIAL"] = "DDI", ["PHONE"] = "DDI",
    };

    /// <summary>Checks the sheet and starts calling. Returns the run, or the list of problems.</summary>
    public (BulkRun? run, List<string> errors) Start(StartInput input)
    {
        var errors = new List<string>();
        var lines = input.Lines ?? [];
        if (lines.Count == 0) errors.Add("The sheet has no lines.");
        if (lines.Count > 500) errors.Add("At most 500 lines per run.");
        lock (_gate) if (_runs.Any(r => r.State == "running")) errors.Add("A bulk run is already in progress; stop it or wait for it to finish.");

        string via = (input.Via ?? "").Trim();
        IPEndPoint? viaEp = null;
        if (via.Length > 0 && !TryEndpoint(via, 5060, out viaEp)) errors.Add($"'{via}' is not an SBC address (IP or IP:port).");
        string from = (input.From ?? "").Trim().Length > 0 ? input.From!.Trim() : "mediantrace";
        if (!SafeUser().IsMatch(from)) errors.Add("The calling number may contain digits, letters, + * # . - _ only.");
        int hold = Math.Clamp(input.HoldSeconds ?? 5, 1, 120);
        int autoRing = Math.Clamp(input.AutoRingSeconds ?? 30, 2, 120);
        int manualRing = Math.Clamp(input.ManualRingSeconds ?? 30, 5, 300);

        var run = new BulkRun
        {
            Id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(),
            Name = (input.Name ?? "").Trim() is { Length: > 0 } n ? n : $"Line test {DateTime.Now:dd MMM HH:mm}",
            Started = DateTime.UtcNow, Via = via, From = from,
        };
        var defs = new List<TestDefinition>();
        for (int i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            string row = $"Row {i + 1}";
            string uri = (l.Uri ?? "").Trim();
            var m = UriPattern().Match(uri);
            if (uri.Length == 0 || !m.Success) { errors.Add($"{row}: '{uri}' is not a SIP URI (sip:number@host[:port]) or a number."); continue; }
            string user = m.Groups["user"].Value;
            if (!SafeUser().IsMatch(user)) { errors.Add($"{row}: the user part '{user}' has characters that are not allowed."); continue; }
            string host = m.Groups["host"].Value.Trim('[', ']');
            int port = m.Groups["port"].Success ? int.Parse(m.Groups["port"].Value) : 5060;
            IPEndPoint? target = viaEp;
            if (target is null)
            {
                if (host.Length == 0) { errors.Add($"{row}: '{uri}' has no host; give the full SIP URI or an SBC to send through."); continue; }
                if (!TryEndpoint($"{host}:{port}", 5060, out target)) { errors.Add($"{row}: cannot resolve '{host}'."); continue; }
            }
            string typeIn = (l.Type ?? "").Trim();
            if (!TypeNames.TryGetValue(typeIn, out string? type)) { errors.Add($"{row}: line type '{typeIn}' must be ARD, MRD, Hoot or DDI."); continue; }
            int lineHold = Math.Clamp(l.HoldSeconds ?? hold, 1, 120);
            int ring = Math.Clamp(l.RingSeconds ?? (type == "ARD" ? autoRing : manualRing), 2, 300);
            string testIn = (l.Test ?? "").Trim().ToUpperInvariant();
            if (testIn.Length > 0 && testIn is not ("WIRE" or "CHECK" or "CALL" or "DIAL")) { errors.Add($"{row}: test '{l.Test}' must be Wire or Call."); continue; }
            string mode = type is "MRD" or "HOOT" && testIn is not ("CALL" or "DIAL") ? "wire" : "call";
            string name = (l.Name ?? "").Trim() is { Length: > 0 } nm ? nm : $"{user} ({type})";
            string sigIn = ((l.Signal ?? "").Trim().Length > 0 ? l.Signal! : input.MrdSignal ?? "DTMF").Trim().ToUpperInvariant();
            string signal = type != "MRD" ? "none" : sigIn is "INFO" or "SIP INFO" ? "info" : sigIn is "NONE" or "OFF" ? "none" : sigIn is "DTMF" or "" ? "dtmf"
                : "?";
            if (signal == "?") { errors.Add($"{row}: signal '{sigIn}' must be DTMF or INFO."); continue; }
            run.Lines.Add(new BulkLine { Index = i + 1, Name = name, Uri = uri, Type = type, HoldSeconds = lineHold, RingSeconds = ring, Signal = signal == "none" || mode == "wire" ? "" : signal.ToUpperInvariant(), Mode = mode });
            defs.Add(new TestDefinition
            {
                Id = $"bulk-{run.Id}-{i + 1}", Name = name, SbcIp = target!.Address.ToString(), SbcPort = target.Port, To = user,
                ToDomain = host.Length > 0 ? host + (m.Groups["port"].Success ? ":" + port : "") : "",
                From = from, Codec = "PCMA", DurationSeconds = lineHold, RingSeconds = ring, LineType = type, RingSignal = signal,
            });
        }
        if (errors.Count > 0) return (null, errors);

        lock (_gate)
        {
            _runs.Add(run);
            if (_runs.Count > 30) _runs.RemoveAt(0);
            Save();
        }
        _stop = new CancellationTokenSource();
        var token = _stop.Token;
        Log.Write($"Bulk line test '{run.Name}' started: {run.Lines.Count} lines{(via.Length > 0 ? " via " + via : "")}");
        _ = Task.Run(() => Execute(run, defs, token));
        return (run, errors);
    }

    private async Task Execute(BulkRun run, List<TestDefinition> defs, CancellationToken ct)
    {
        for (int i = 0; i < defs.Count; i++)
        {
            var line = run.Lines[i];
            if (ct.IsCancellationRequested) { line.Status = "skipped"; continue; }
            if (line.Mode == "wire")
            {
                line.Status = "checking";
                try { await CheckWire(line, defs[i], ct); }
                catch (OperationCanceledException) { line.Status = "skipped"; }
                catch (Exception e) { line.Status = "failed"; line.Summary = $"Wire check error: {e.Message}"; }
                lock (_gate) Save();
                continue;
            }
            line.Status = "calling";
            var watch = Task.Run(async () =>
            {
                // Mirror the live stage of the call into the line while it runs.
                while (line.Status is "calling" or "ringing" or "connected")
                {
                    await Task.Delay(500);
                    var live = _tester.Live;
                    if (live is not null && live.TestId == defs[i].Id)
                    {
                        line.ResultId = live.Id;
                        line.Status = live.Stage switch { "ringing" or "ringing (MRD)" => "ringing", "connected" or "hanging up" => "connected", _ => line.Status };
                    }
                }
            });
            TestResult r;
            try { r = await _tester.RunAndWait(defs[i], "bulk", defs[i].Id, ct); }
            catch (Exception e) { line.Status = "failed"; line.Summary = e.Message; continue; }
            line.ResultId = r.Id;
            line.Status = r.Outcome is "passed" or "warning" or "failed" ? r.Outcome : "failed";
            line.Summary = r.Findings.FirstOrDefault() ?? "";
            line.SipStatus = r.FinalStatus;
            line.AnswerSeconds = r.AnswerMs is double a ? Math.Round(a / 1000, 2) : null;
            line.RxPackets = r.RxPackets;
            line.Mos = r.MosRx;
            await watch;
            lock (_gate) Save();
            if (i < defs.Count - 1) { try { await Task.Delay(2000, ct); } catch (OperationCanceledException) { } }
        }
        run.State = ct.IsCancellationRequested ? "stopped" : "done";
        run.Ended = DateTime.UtcNow;
        lock (_gate) Save();
        Log.Write($"Bulk line test '{run.Name}' {run.State}: {run.Lines.Count(l => l.Status == "passed")} passed, {run.Lines.Count(l => l.Status == "warning")} warning, {run.Lines.Count(l => l.Status == "failed")} failed");
    }

    // ------------------------------------------------------------ always-on wires

    public sealed record SbcCall(string Caller, string Callee, string Direction, string Duration, string State, string Session);

    /// <summary>
    /// MRD / Hoot: is the wire's call up on the SBC, and does audio flow on it?
    /// Never calls the line: a new call would replace the live wire call.
    /// </summary>
    private async Task CheckWire(BulkLine line, TestDefinition def, CancellationToken ct)
    {
        string number = def.To;
        string sbcIp = def.SbcIp;
        var findings = new List<string>();
        string outcome = "passed";
        List<string> sessions = [];

        var entry = _settings.Sbcs.FirstOrDefault(s => s.Ip == sbcIp);
        List<SbcCall>? calls = null;
        string why = entry is null ? $"{sbcIp} is not in the SBC list" : !entry.HasLogin ? $"no SSH login is set for {entry.Name}" : "";
        if (entry is { HasLogin: true })
        {
            try { calls = await ActiveCalls(entry.Ip, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { why = $"the SBC's call list could not be read ({e.Message})"; }
        }

        if (calls is not null)
        {
            var legs = calls.Where(c => c.Caller == number || c.Callee == number).ToList();
            if (legs.Count == 0)
            {
                line.Status = "failed";
                line.Summary = $"The wire is down: the SBC has no call for {number} ({calls.Count} other call{(calls.Count == 1 ? "" : "s")} up).";
                return;
            }
            var first = legs[0];
            findings.Add($"Wire {first.Caller} ↔ {first.Callee} is up on the SBC: {legs.Count} leg{(legs.Count == 1 ? "" : "s")}, connected for {first.Duration}.");
            foreach (var bad in legs.Where(c => !c.State.Equals("Connected", StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add($"Leg {bad.Session} is '{bad.State}', not connected.");
                outcome = "warning";
            }
            sessions = legs.Select(c => c.Session).ToList();
        }
        else if (_recorder.Recording)
        {
            var live = _recorder.Store.LiveCallsFor(sbcIp, number);
            if (live.Count == 0)
            {
                line.Status = "warning";
                line.Summary = $"No live call for {number} in the capture, and {why}. A wire set up before the capture started is not seen here: add the SBC's SSH login on the SBCs page to read its call list.";
                return;
            }
            findings.Add($"Wire call for {number} is up in the capture ({live[0].from} → {live[0].to}, answered {live[0].answered?.ToLocalTime():HH:mm:ss}).");
            sessions = live.Select(c => c.session).ToList();
        }
        else
        {
            line.Status = "warning";
            line.Summary = $"Cannot check the wire: {why}, and no capture is running. Add the SBC login or start a capture.";
            return;
        }

        if (_recorder.Recording && sessions.Count > 0)
        {
            int seconds = Math.Clamp(line.HoldSeconds, 3, 30);
            var before = _recorder.Store.SessionMedia(sbcIp, sessions);
            await Task.Delay(seconds * 1000, ct);
            var after = _recorder.Store.SessionMedia(sbcIp, sessions);
            long fromSbc = after.Sum(a => a.FromSbc) - before.Sum(a => a.FromSbc);
            long toSbc = after.Sum(a => a.ToSbc) - before.Sum(a => a.ToSbc);
            if (after.All(a => a.FromSbc + a.ToSbc == 0))
            {
                findings.Add("No audio for this wire in the capture: check the SBC's Debug Recording includes media (Signaling & Media).");
            }
            else
            {
                findings.Add($"Audio over {seconds} s: SBC → dealerboard {toSbcText(fromSbc)}, dealerboard → SBC {toSbcText(toSbc)}.");
                if (fromSbc == 0) { findings.Add("The SBC sent no audio to the dealerboard on this wire."); outcome = "warning"; }
                if (toSbc == 0)
                {
                    findings.Add("The dealerboard sent no audio to the SBC: one-way right now. Normal if nobody has the line open; a fault if someone is talking on it.");
                    outcome = "warning";
                }
                line.RxPackets = toSbc;
                line.Mos = after.Select(a => a.Mos).Where(m => m is not null).DefaultIfEmpty(null).Min();
                double loss = after.Select(a => a.LossPercent).DefaultIfEmpty(0).Max();
                if (loss > 2) { findings.Add($"Packet loss on the wire: {loss:0.#}%."); outcome = "warning"; }
            }
        }
        else if (!_recorder.Recording)
        {
            findings.Add("Start a capture to measure the wire's audio too.");
        }

        line.Status = outcome;
        line.Summary = string.Join(" ", findings);

        static string toSbcText(long packets) => packets == 0 ? "none" : $"{packets:N0} packets";
    }

    /// <summary>The SBC's active call list, read once per 20 s per SBC (one SSH session for a whole sheet).</summary>
    private async Task<List<SbcCall>> ActiveCalls(string sbcIp, CancellationToken ct)
    {
        lock (_sbcCalls)
            if (_sbcCalls.TryGetValue(sbcIp, out var cached) && DateTime.UtcNow - cached.at < TimeSpan.FromSeconds(20))
                return cached.calls;
        var output = await Task.Run(() => _changes.Show(sbcIp, ["show voip calls active"]), ct);
        var calls = ParseActiveCalls(output.FirstOrDefault().output ?? "");
        lock (_sbcCalls) _sbcCalls[sbcIp] = (DateTime.UtcNow, calls);
        return calls;
    }

    /// <summary>Rows of "show voip calls active": | start | end point | caller | callee | direction | remote IP | duration | state | session |</summary>
    public static List<SbcCall> ParseActiveCalls(string text)
    {
        var list = new List<SbcCall>();
        foreach (string raw in text.Split('\n'))
        {
            string row = raw.Trim();
            if (!row.StartsWith('|') || row.Contains("Call Start Time", StringComparison.OrdinalIgnoreCase)) continue;
            var f = row.Split('|').Select(x => x.Trim()).ToArray();
            if (f.Length < 10) continue;
            list.Add(new SbcCall(f[3], f[4], f[5], f[7], f[8], f[9]));
        }
        return list;
    }

    public bool Stop()
    {
        lock (_gate) if (!_runs.Any(r => r.State == "running")) return false;
        _stop?.Cancel();
        return true;
    }

    public List<BulkRun> Runs()
    {
        lock (_gate) return _runs.AsEnumerable().Reverse().ToList();
    }

    private static bool TryEndpoint(string text, int defaultPort, out IPEndPoint? ep)
    {
        ep = null;
        string host = text, portText = "";
        int colon = text.LastIndexOf(':');
        if (colon > 0 && text.IndexOf(':') == colon) { host = text[..colon]; portText = text[(colon + 1)..]; }
        int port = defaultPort;
        if (portText.Length > 0 && (!int.TryParse(portText, out port) || port is < 1 or > 65535)) return false;
        if (!IPAddress.TryParse(host, out IPAddress? ip))
        {
            try { ip = Dns.GetHostAddresses(host).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork); }
            catch { ip = null; }
        }
        if (ip is null || ip.AddressFamily != AddressFamily.InterNetwork) return false;
        ep = new IPEndPoint(ip, port);
        return true;
    }

    private void Save()
    {
        try { File.WriteAllText(RunsPath, JsonSerializer.Serialize(_runs, Json)); }
        catch (Exception e) { Log.Write($"bulk-runs.json could not be saved: {e.Message}"); }
    }
}
