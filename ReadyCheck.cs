using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// "Ready for business": one go / no-go check before the market opens. Looks at
/// each SBC (logs arriving, alarms, restarts, load, E1 trunks, clock, config drift),
/// the trading servers' SIP ports, the line board (wires up, lines tested) and any
/// chosen call tests, then gives one verdict with the reasons. Runs by hand or each
/// morning; every run is kept so there is a record that it was done.
/// Everything it does on an SBC is read-only (show commands).
/// </summary>
public sealed partial class ReadyCheck
{
    private readonly Settings _settings;
    private readonly Recorder _recorder;
    private readonly SipTester _tester;
    private readonly ChangeManager _changes;
    private readonly LineBoard _board;
    private readonly DriftWatch _drift;
    private readonly object _gate = new();
    private ReadyConfig _config = new();
    private List<ReadyRun> _runs = [];
    private CancellationTokenSource? _running;
    private readonly CancellationTokenSource _stop = new();
    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "ready.json");
    private static string RunsPath => Path.Combine(AppContext.BaseDirectory, "ready-runs.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public sealed class ReadyConfig
    {
        public List<string> SipTargets { get; set; } = [];     // host:port of the trading servers' SIP, as the SBC sends to them
        public List<string> CallTestIds { get; set; } = [];    // saved call tests to run (e.g. the echo test)
        public List<string> AcceptedAlarms { get; set; } = []; // alarm text that is known and accepted (e.g. "LAN port number 2")
        public string DailyAt { get; set; } = "";              // "07:00" = run then each day; "" = by hand only
        public bool WeekdaysOnly { get; set; } = true;
        public bool AlertWhenNotReady { get; set; } = true;    // send the result to the webhook / email unless it is a clean "ready"
        public int SyslogErrorsPerHour { get; set; } = 20;     // more errors than this in the last hour is a warning
    }

    public sealed class ReadyItem
    {
        public string Area { get; set; } = "";
        public string Name { get; set; } = "";
        public string State { get; set; } = "running";   // running, ok, warn, bad, skip
        public string Detail { get; set; } = "";
    }

    public sealed class ReadyRun
    {
        public string Id { get; set; } = "";
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }
        public string Trigger { get; set; } = "manual";
        public string StartedBy { get; set; } = "";
        public string Verdict { get; set; } = "running";  // running, ready, warnings, notready, stopped
        public List<ReadyItem> Items { get; set; } = [];
    }

    public ReadyCheck(Settings settings, Recorder recorder, SipTester tester, ChangeManager changes, LineBoard board, DriftWatch drift)
    {
        _settings = settings; _recorder = recorder; _tester = tester; _changes = changes; _board = board; _drift = drift;
        try { if (File.Exists(ConfigPath)) _config = JsonSerializer.Deserialize<ReadyConfig>(File.ReadAllText(ConfigPath)) ?? new(); }
        catch (Exception e) { Log.Write($"ready.json could not be read: {e.Message}"); }
        try { if (File.Exists(RunsPath)) _runs = JsonSerializer.Deserialize<List<ReadyRun>>(File.ReadAllText(RunsPath)) ?? []; }
        catch (Exception e) { Log.Write($"ready-runs.json could not be read: {e.Message}"); }
        foreach (var r in _runs.Where(r => r.Verdict == "running")) { r.Verdict = "stopped"; r.Ended ??= r.Started; }
        _ = Task.Run(() => Scheduler(_stop.Token));
    }

    public ReadyConfig Config()
    {
        lock (_gate) return JsonSerializer.Deserialize<ReadyConfig>(JsonSerializer.Serialize(_config))!;
    }

    [GeneratedRegex(@"^\d{1,2}:\d{2}$")]
    private static partial Regex TimeOfDay();

    public (bool ok, string message) SaveConfig(ReadyConfig c)
    {
        var targets = new List<string>();
        foreach (string t in (c.SipTargets ?? []).Select(x => x.Trim()).Where(x => x.Length > 0))
        {
            if (!TryTarget(t, out _)) return (false, $"'{t}' is not a SIP address: use ip:port, e.g. 192.0.2.58:5060.");
            targets.Add(t);
        }
        string at = (c.DailyAt ?? "").Trim();
        if (at.Length > 0 && (!TimeOfDay().IsMatch(at) || !TimeSpan.TryParse(at, out var tod) || tod >= TimeSpan.FromDays(1)))
            return (false, "The daily time must look like 07:00.");
        var clean = new ReadyConfig
        {
            SipTargets = targets.Distinct().Take(50).ToList(),
            CallTestIds = (c.CallTestIds ?? []).Where(id => _tester.Find(id) is not null).Distinct().Take(20).ToList(),
            AcceptedAlarms = (c.AcceptedAlarms ?? []).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().Take(50).ToList(),
            DailyAt = at, WeekdaysOnly = c.WeekdaysOnly, AlertWhenNotReady = c.AlertWhenNotReady,
            SyslogErrorsPerHour = Math.Clamp(c.SyslogErrorsPerHour, 0, 100000),
        };
        lock (_gate)
        {
            _config = clean;
            try { File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config, Json)); }
            catch (Exception e) { return (false, $"Could not save: {e.Message}"); }
        }
        return (true, at.Length > 0 ? $"Saved. The check runs at {at}{(clean.WeekdaysOnly ? " on weekdays" : " every day")}." : "Saved.");
    }

    public List<ReadyRun> Runs(int limit = 30)
    {
        lock (_gate) return _runs.AsEnumerable().Reverse().Take(limit).ToList();
    }

    public bool Running
    {
        get { lock (_gate) return _runs.Any(r => r.Verdict == "running"); }
    }

    /// <summary>Starts a check. Returns null when one is already running.</summary>
    public ReadyRun? Start(string trigger, string startedBy)
    {
        ReadyRun run;
        lock (_gate)
        {
            if (_runs.Any(r => r.Verdict == "running")) return null;
            run = new ReadyRun
            {
                Id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(),
                Started = DateTime.UtcNow, Trigger = trigger, StartedBy = startedBy,
            };
            _runs.Add(run);
            if (_runs.Count > 200) _runs.RemoveAt(0);
            _running = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            Save();
        }
        var ct = _running.Token;
        Log.Write($"Ready check started ({trigger}{(startedBy.Length > 0 ? ", " + startedBy : "")})");
        _ = Task.Run(() => Execute(run, ct));
        return run;
    }

    public bool Stop()
    {
        lock (_gate)
        {
            if (!_runs.Any(r => r.Verdict == "running")) return false;
            _running?.Cancel();
            return true;
        }
    }

    // ------------------------------------------------------------ the checks

    private ReadyItem Add(ReadyRun run, string area, string name)
    {
        var item = new ReadyItem { Area = area, Name = name };
        lock (_gate) run.Items.Add(item);
        return item;
    }

    private static void Set(ReadyItem item, string state, string detail) { item.Detail = detail; item.State = state; }

    private async Task Execute(ReadyRun run, CancellationToken ct)
    {
        ReadyConfig config = Config();
        try
        {
            foreach (SbcEntry sbc in _settings.Sbcs.Where(s => s.Ip.Length > 0))
                await CheckSbc(run, sbc, config, ct);
            await CheckServers(run, config, ct);
            await CheckLines(run, ct);
            await CheckCallTests(run, config, ct);
            CheckThisServer(run, config);
        }
        catch (OperationCanceledException)
        {
            lock (_gate) foreach (var i in run.Items.Where(i => i.State == "running")) Set(i, "skip", "Stopped before this finished.");
        }
        catch (Exception e)
        {
            Set(Add(run, "MediantTrace", "Check"), "bad", $"The check stopped with an error: {e.Message}");
            Log.Write($"Ready check error: {e}");
        }

        lock (_gate)
        {
            foreach (var i in run.Items.Where(i => i.State == "running")) Set(i, "skip", "Did not finish.");
            run.Ended = DateTime.UtcNow;
            run.Verdict = ct.IsCancellationRequested && run.Items.All(i => i.State != "bad") ? "stopped"
                : run.Items.Any(i => i.State == "bad") ? "notready"
                : run.Items.Any(i => i.State == "warn") ? "warnings" : "ready";
            Save();
        }
        Log.Write($"Ready check {run.Verdict}: {run.Items.Count(i => i.State == "bad")} problems, {run.Items.Count(i => i.State == "warn")} warnings");
        if (config.AlertWhenNotReady && run.Verdict is "notready" or "warnings")
            await TestAlerts.SendText(_settings, AlertTitle(run), AlertText(run), false);
    }

    public static string VerdictText(string v) => v switch
    {
        "ready" => "Ready for business",
        "warnings" => "Ready, with warnings",
        "notready" => "Not ready",
        "stopped" => "Stopped",
        _ => "Checking…",
    };

    private static string AlertTitle(ReadyRun run) => $"MediantTrace ready check: {VerdictText(run.Verdict)}";

    private static string AlertText(ReadyRun run)
    {
        var lines = new List<string> { AlertTitle(run), run.Started.ToLocalTime().ToString("ddd dd MMM yyyy HH:mm") };
        foreach (var i in run.Items.Where(i => i.State is "bad" or "warn").OrderBy(i => i.State == "bad" ? 0 : 1))
            lines.Add($"{(i.State == "bad" ? "PROBLEM" : "Warning")}: {i.Area} - {i.Name}: {i.Detail}");
        return string.Join("\n", lines);
    }

    // ---- SBC

    private async Task CheckSbc(ReadyRun run, SbcEntry sbc, ReadyConfig config, CancellationToken ct)
    {
        string area = $"SBC {sbc.Name} ({sbc.Ip})";

        // Logs arriving: without syslog nothing else here can see a problem as it happens.
        var feeds = _recorder.Sources().Where(s => s.Address == sbc.Ip).ToList();
        var item = Add(run, area, "Logs arriving");
        DateTime now = DateTime.UtcNow;
        var syslog = feeds.FirstOrDefault(f => f.Kind == "SYSLOG");
        var dr = feeds.FirstOrDefault(f => f.Kind == "DR");
        string Age(DateTime t) => (now - t).TotalMinutes < 2 ? "now" : $"{(int)(now - t).TotalMinutes} min ago";
        if (syslog is null || now - syslog.LastSeen > TimeSpan.FromMinutes(5))
            Set(item, "bad", syslog is null ? $"No syslog from {sbc.Ip} since MediantTrace started (port {_recorder.ListeningPort("SYSLOG")}): check the SBC's syslog server setting and the firewall."
                                            : $"No syslog from {sbc.Ip} since {Age(syslog.LastSeen)}.");
        else if (dr is null || now - dr.LastSeen > TimeSpan.FromMinutes(5))
            Set(item, "warn", $"Syslog arriving, but no Debug Recording{(dr is null ? "" : " since " + Age(dr.LastSeen))}: audio and wire measurements need it (port {_recorder.ListeningPort("DR")}).");
        else
            Set(item, "ok", "Syslog and Debug Recording arriving.");

        if (!sbc.HasLogin)
        {
            Set(Add(run, area, "Alarms, restarts, trunks"), "warn", "No SSH login for this SBC: add one on the SBCs page so alarms, restarts, E1 trunks and the clock can be checked.");
            return;
        }

        var alarms = Add(run, area, "Active alarms");
        var uptime = Add(run, area, "Restarted recently");
        var load = Add(run, area, "Load");
        var clock = Add(run, area, "Clock (NTP)");
        List<(string command, string output)> outputs;
        try
        {
            outputs = await Task.Run(() => _changes.Show(sbc.Ip,
                ["show active-alarms", "show system uptime", "show system utilization", "show ntp", "show voip interface e1-t1", "show voip proxy sets status"]), ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            foreach (var i in new[] { alarms, uptime, load, clock }) Set(i, "bad", $"Could not log in to the SBC: {e.Message}");
            return;
        }
        string Out(string cmd) => outputs.FirstOrDefault(o => o.command == cmd).output ?? "";

        // Alarms
        var rows = ParseAlarms(Out("show active-alarms"));
        var accepted = rows.Where(a => config.AcceptedAlarms.Any(x => a.Description.Contains(x, StringComparison.OrdinalIgnoreCase) || a.Source.Contains(x, StringComparison.OrdinalIgnoreCase))).ToList();
        var open = rows.Except(accepted).ToList();
        var serious = open.Where(a => a.Severity is "critical" or "major").ToList();
        string list(IEnumerable<Alarm> a) => string.Join("; ", a.Select(x => $"{x.Severity}: {x.Description} (since {x.Date})"));
        if (serious.Count > 0) Set(alarms, "bad", list(serious) + (open.Count > serious.Count ? $"; also {open.Count - serious.Count} minor" : ""));
        else if (open.Count > 0) Set(alarms, "warn", list(open));
        else Set(alarms, "ok", rows.Count == 0 ? "No active alarms." : $"No alarms apart from {accepted.Count} accepted.");

        // Uptime: a restart drops changes that were not saved.
        var up = UptimePattern().Match(Out("show system uptime"));
        if (up.Success)
        {
            var span = new TimeSpan(int.Parse(up.Groups[1].Value), int.Parse(up.Groups[2].Value), int.Parse(up.Groups[3].Value), 0);
            if (span < TimeSpan.FromHours(24))
                Set(uptime, "warn", $"The SBC restarted {(span.TotalHours >= 1 ? $"{(int)span.TotalHours} h {span.Minutes} min" : $"{span.Minutes} min")} ago. Changes that were not saved before then are gone: check recent changes are still in place.");
            else Set(uptime, "ok", $"Up for {span.Days} days.");
        }
        else Set(uptime, "skip", "The SBC did not report its uptime.");

        // Load
        var cpu = CpuPattern().Matches(Out("show system utilization")).Select(m => int.Parse(m.Groups[1].Value)).DefaultIfEmpty(-1).Max();
        if (cpu < 0) Set(load, "skip", "The SBC did not report its load.");
        else Set(load, cpu >= 85 ? "bad" : cpu >= 70 ? "warn" : "ok", $"CPU {cpu}%.");

        // Clock: call records and logs are only comparable when the clock is right.
        string ntp = Out("show ntp");
        if (ntp.Contains("NTP is synchronized", StringComparison.OrdinalIgnoreCase)) Set(clock, "ok", "In step with the time server.");
        else if (ntp.Length == 0) Set(clock, "skip", "The SBC did not report NTP.");
        else Set(clock, "warn", "The SBC's clock is not in step with a time server: timestamps on calls and logs may be wrong.");

        // Proxy sets the SBC keeps alive (the trading servers, carriers): offline means calls to them fail.
        foreach (var p in SbcHealth.ParseProxySets(Out("show voip proxy sets status")).Where(p => p.KeepAlive))
        {
            var ps = Add(run, area, $"Proxy set {p.Id} ({string.Join(", ", p.Addresses.Select(a => a.Address))})");
            Set(ps, p.Status == "ONLINE" ? "ok" : "bad", p.Status == "ONLINE" ? "Online: keepalives answered."
                : $"{p.Status.ToLowerInvariant()}: the SBC's keepalives are failing ({p.Addresses.Sum(a => a.Failed)} failed), so calls sent to it will fail.");
        }

        // E1 trunks (when there are any)
        foreach (var t in ParseTrunks(Out("show voip interface e1-t1")))
        {
            var e1 = Add(run, area, $"E1 trunk {t.Port}");
            bool good = t.State == "active" && t.DChannel == "established" && t.AlarmsClear;
            Set(e1, good ? "ok" : "bad", good ? "Active, D-channel up, no line alarms."
                : $"State {t.State}, D-channel {t.DChannel}{(t.AlarmsClear ? "" : $", line alarms: {t.Alarms}")}.");
        }

        // Config drift against the saved standard (the dashboard's check)
        var drift = Add(run, area, "Configuration matches the standard");
        DriftWatch.DriftView? d = null;
        for (int i = 0; i < 30; i++)
        {
            d = _drift.Snapshot().FirstOrDefault(x => x.Ip == sbc.Ip);
            if (d is null || d.State != "checking") break;
            await Task.Delay(2000, ct);
        }
        if (d is null || d.State == "none") Set(drift, "skip", "No standard configuration saved for this SBC (Config compare).");
        else if (d.State == "same") Set(drift, "ok", "Same as the saved standard.");
        else if (d.State == "drift") Set(drift, "warn", $"{d.Differences} difference{(d.Differences == 1 ? "" : "s")} from the saved standard: see Config compare.");
        else if (d.State == "checking") Set(drift, "skip", "Still downloading the configuration.");
        else Set(drift, "warn", $"Could not compare: {d.Message}");
    }

    public sealed record Alarm(string Source, string Severity, string Date, string Description);

    [GeneratedRegex(@"^\s*\d+\.\s+(\S+)\s+(critical|major|minor|warning|indeterminate)\s+(\d{1,2}\.\d{1,2}\.\d{4}\s*,\s*\d{1,2}:\d{2}:\d{2})\s+(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex AlarmRow();

    [GeneratedRegex(@"Uptime:\s*(\d+)\s*days?,\s*(\d+)\s*hours?,\s*(\d+)\s*minutes?", RegexOptions.IgnoreCase)]
    private static partial Regex UptimePattern();

    [GeneratedRegex(@"Voice\s+(\d+)%", RegexOptions.IgnoreCase)]
    private static partial Regex CpuPattern();

    public static List<Alarm> ParseAlarms(string text) =>
        AlarmRow().Matches(text).Select(m => new Alarm(m.Groups[1].Value, m.Groups[2].Value.ToLowerInvariant(),
            Regex.Replace(m.Groups[3].Value, @"\s*,\s*", " "), m.Groups[4].Value.Trim())).ToList();

    public sealed record Trunk(string Port, string State, string DChannel, string Alarms, bool AlarmsClear);

    public static List<Trunk> ParseTrunks(string text)
    {
        var list = new List<Trunk>();
        foreach (string block in Regex.Split(text, @"(?=module/port:)").Skip(1))
        {
            string F(string name) => Regex.Match(block, $@"^\s*{name}:\s*(.+?)\s*$", RegexOptions.Multiline) is { Success: true } m ? m.Groups[1].Value : "";
            string port = F("module/port"), alarms = F("alarm status");
            if (port.Length == 0) continue;
            bool clear = Regex.Matches(alarms, @"\d+").All(m => m.Value == "0");
            list.Add(new Trunk(port, F("state"), F("d-channel status"), alarms, clear));
        }
        return list;
    }

    // ---- trading servers

    private async Task CheckServers(ReadyRun run, ReadyConfig config, CancellationToken ct)
    {
        if (config.SipTargets.Count == 0)
        {
            Set(Add(run, "Trading servers", "SIP answering"), "skip", "No trading-server SIP addresses set: add them under Settings on this page (e.g. 192.0.2.58:5060).");
            return;
        }
        foreach (string target in config.SipTargets)
        {
            var item = Add(run, "Trading servers", $"SIP {target}");
            if (!TryTarget(target, out var ep)) { Set(item, "bad", "Not a valid address."); continue; }
            var (ok, status, ms) = await SipOptions(ep!, ct);
            Set(item, ok ? "ok" : "bad", ok ? $"Answered {status} in {ms} ms." : "No answer to a SIP OPTIONS within 3 s: the server or its port forward is down, so calls to it will fail.");
        }
    }

    private static bool TryTarget(string text, out IPEndPoint? ep)
    {
        ep = null;
        int colon = text.LastIndexOf(':');
        string host = colon > 0 ? text[..colon] : text;
        int port = 5060;
        if (colon > 0 && (!int.TryParse(text[(colon + 1)..], out port) || port is < 1 or > 65535)) return false;
        if (!IPAddress.TryParse(host, out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
        ep = new IPEndPoint(ip, port);
        return true;
    }

    /// <summary>SIP OPTIONS from a spare port: any final answer means SIP is listening. Retransmits like a SIP client.</summary>
    private static async Task<(bool ok, int status, long ms)> SipOptions(IPEndPoint target, CancellationToken ct)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        int localPort = ((IPEndPoint)socket.LocalEndPoint!).Port;
        string id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
        string local = LocalAddressFor(target.Address);
        byte[] msg = Encoding.ASCII.GetBytes(
            $"OPTIONS sip:ping@{target.Address}:{target.Port} SIP/2.0\r\nVia: SIP/2.0/UDP {local}:{localPort};branch=z9hG4bK{id};rport\r\nMax-Forwards: 70\r\n" +
            $"From: <sip:mediantrace@{local}>;tag={id}\r\nTo: <sip:ping@{target.Address}>\r\nCall-ID: {id}@mediantrace\r\nCSeq: 1 OPTIONS\r\nContent-Length: 0\r\n\r\n");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(3000);
        _ = Task.Run(async () =>
        {
            foreach (int wait in new[] { 0, 500, 1000 })
            {
                try { await Task.Delay(wait, timeout.Token); await socket.SendToAsync(msg, target); } catch { return; }
            }
        });
        var buffer = new byte[4096];
        try
        {
            while (true)
            {
                var r = await socket.ReceiveFromAsync(buffer, new IPEndPoint(IPAddress.Any, 0), timeout.Token);
                string text = Encoding.ASCII.GetString(buffer, 0, r.ReceivedBytes);
                if (!text.Contains(id)) continue;
                var m = Regex.Match(text, @"^SIP/2\.0\s+(\d{3})");
                if (m.Success && int.Parse(m.Groups[1].Value) >= 200) return (true, int.Parse(m.Groups[1].Value), watch.ElapsedMilliseconds);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return (false, 0, watch.ElapsedMilliseconds); }
        catch (SocketException) { return (false, 0, watch.ElapsedMilliseconds); }
    }

    private static string LocalAddressFor(IPAddress remote)
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(remote, 9);
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch { return "0.0.0.0"; }
    }

    // ---- lines

    private async Task CheckLines(ReadyRun run, CancellationToken ct)
    {
        var lines = _board.Views();
        if (lines.Count == 0)
        {
            Set(Add(run, "Lines", "Line board"), "skip", "No lines on the line board yet.");
            return;
        }
        var wires = Add(run, "Lines", "Always-on wires (MRD / Hoot) up");
        var tested = Add(run, "Lines", "ARD / DDI lines tested in the last 24 h");
        var problems = await _board.ReadSbcsNow(ct);
        lines = _board.Views();

        var always = lines.Where(l => LineBoard.AlwaysOn(l.Type)).ToList();
        string names(IEnumerable<LineBoard.LineView> ls) { var l = ls.ToList(); return string.Join(", ", l.Take(12).Select(x => x.Name)) + (l.Count > 12 ? $" and {l.Count - 12} more" : ""); }
        if (always.Count == 0) Set(wires, "skip", "No MRD or Hoot lines on the board.");
        else
        {
            var down = always.Where(l => l.State == "down").ToList();
            var check = always.Where(l => l.State == "warn").ToList();
            var unknown = always.Where(l => l.State is "unknown" or "idle").ToList();
            var parts = new List<string> { $"{always.Count - down.Count - unknown.Count} of {always.Count} up" };
            if (down.Count > 0) parts.Add($"down: {names(down)}");
            if (check.Count > 0) parts.Add($"audio one way right now: {names(check)}");
            if (unknown.Count > 0) parts.Add($"unknown: {names(unknown)}{(problems.Count > 0 ? $" ({string.Join("; ", problems)})" : "")}");
            Set(wires, down.Count > 0 ? "bad" : check.Count + unknown.Count > 0 ? "warn" : "ok", string.Join(". ", parts) + ".");
        }

        var called = lines.Where(l => !LineBoard.AlwaysOn(l.Type)).ToList();
        if (called.Count == 0) Set(tested, "skip", "No ARD or DDI lines on the board.");
        else
        {
            DateTime since = DateTime.UtcNow.AddHours(-24);
            var recent = called.Where(l => l.LastTest is not null && l.LastTest.At >= since).ToList();
            var failed = recent.Where(l => l.LastTest!.Status == "failed").ToList();
            var notTested = called.Except(recent).ToList();
            var parts = new List<string> { $"{recent.Count - failed.Count} of {called.Count} passed a test in the last 24 h" };
            if (failed.Count > 0) parts.Add($"failed: {names(failed)}");
            if (notTested.Count > 0) parts.Add($"not tested: {names(notTested)} (a test call needs someone to pick up: run them from the Line board)");
            Set(tested, failed.Count > 0 ? "bad" : notTested.Count > 0 ? "warn" : "ok", string.Join(". ", parts) + ".");
        }
    }

    // ---- call tests

    private async Task CheckCallTests(ReadyRun run, ReadyConfig config, CancellationToken ct)
    {
        foreach (string id in config.CallTestIds)
        {
            var def = _tester.Find(id);
            if (def is null) continue;
            var item = Add(run, "Call tests", def.Name);
            try
            {
                var r = await _tester.RunAndWait(def, "ready-check", def.Id, ct);
                string finding = r.Findings.FirstOrDefault() ?? "";
                Set(item, r.Outcome == "passed" ? "ok" : r.Outcome == "warning" ? "warn" : "bad",
                    (r.MosRx is double mos ? $"MOS {mos:0.0}. " : "") + finding);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { Set(item, "bad", $"Could not run: {e.Message}"); }
        }
    }

    // ---- this server

    private void CheckThisServer(ReadyRun run, ReadyConfig config)
    {
        var errors = Add(run, "MediantTrace", "SBC errors in the last hour");
        var (count, examples) = _recorder.Store.SyslogErrorsSince(DateTime.UtcNow.AddHours(-1));
        Set(errors, config.SyslogErrorsPerHour > 0 && count > config.SyslogErrorsPerHour ? "warn" : "ok",
            count == 0 ? "No errors in the SBC logs." : $"{count} error line{(count == 1 ? "" : "s")} in the SBC logs (limit {config.SyslogErrorsPerHour}). Latest: {string.Join(" | ", examples)}");

        var disk = Add(run, "MediantTrace", "Disk space for logs and captures");
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(_settings.LogFolderFull)!);
            double gb = drive.AvailableFreeSpace / 1024.0 / 1024 / 1024;
            Set(disk, gb < 1 ? "bad" : gb < 5 ? "warn" : "ok", $"{gb:0.#} GB free on {drive.Name}.");
        }
        catch (Exception e) { Set(disk, "skip", $"Could not read the disk: {e.Message}"); }
    }

    // ------------------------------------------------------------ daily run

    private async Task Scheduler(CancellationToken ct)
    {
        DateTime lastDay = DateTime.MinValue;
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch (OperationCanceledException) { return; }
            ReadyConfig c = Config();
            if (c.DailyAt.Length == 0 || !TimeSpan.TryParse(c.DailyAt, out var at)) continue;
            DateTime now = DateTime.Now;
            if (c.WeekdaysOnly && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (now.TimeOfDay < at || now.TimeOfDay > at + TimeSpan.FromMinutes(10) || lastDay == now.Date) continue;
            bool ranToday;
            lock (_gate) ranToday = _runs.Any(r => r.Trigger == "daily" && r.Started.ToLocalTime().Date == now.Date);
            lastDay = now.Date;
            if (!ranToday) Start("daily", "");
        }
    }

    public int PruneOlderThan(DateTime cutoff)
    {
        lock (_gate)
        {
            int n = _runs.RemoveAll(r => r.Started < cutoff && r.Verdict != "running");
            if (n > 0) Save();
            return n;
        }
    }

    private void Save()
    {
        try { File.WriteAllText(RunsPath, JsonSerializer.Serialize(_runs, Json)); }
        catch (Exception e) { Log.Write($"ready-runs.json could not be saved: {e.Message}"); }
    }

    public void Dispose() => _stop.Cancel();
}
