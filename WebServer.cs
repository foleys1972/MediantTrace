using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MediantTrace;

/// <summary>The web page and its small JSON API, on Windows' built-in HTTP server.</summary>
public sealed class WebServer : IDisposable
{
    private readonly Settings _settings;
    private readonly Recorder _recorder;
    private readonly SipTester _tester;
    private readonly ChangeManager _changes;
    private readonly McpServer _mcp;
    private readonly BulkTests _bulk;
    private readonly LineBoard _board;
    private readonly ReadyCheck _ready;
    private readonly SbcHealth _health;
    private readonly LineMonitor _monitor;
    private readonly Upgrades _upgrades;
    private readonly Timeline _timeline;
    private readonly Storage _storage;
    private readonly DriftWatch _drift;
    private readonly LabelBook _labels = new();
    private readonly int _boundWeb;

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Dictionary<string, string> _sessions = new(StringComparer.Ordinal);
    private readonly object _sessionGate = new();
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Url { get; private set; } = "";

    public WebServer(Settings settings, Recorder recorder, SipTester tester, ChangeManager changes, McpServer mcp, BulkTests bulk, LineBoard board, ReadyCheck ready, DriftWatch drift, SbcHealth health, LineMonitor monitor, Upgrades upgrades, Timeline timeline, Storage storage)
    {
        _storage = storage;
        _timeline = timeline;
        _upgrades = upgrades;
        _monitor = monitor;
        _health = health;
        _bulk = bulk;
        _board = board;
        _ready = ready;
        _settings = settings;
        _recorder = recorder;
        _tester = tester;
        _changes = changes;
        _mcp = mcp;
        _drift = drift;
        _boundWeb = settings.WebPort;
    }

    public bool Start()
    {
        string host = _settings.WebOnNetwork ? "+" : "localhost";
        _listener.Prefixes.Add($"http://{host}:{_settings.WebPort}/");
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException e) when (_settings.WebOnNetwork)
        {
            // Listening on the network needs admin rights (or a URL reservation); a service has them.
            Log.Write($"Could not open the page on the network ({e.Message}); this PC only instead. Run as a service, or as administrator, for network access.");
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://localhost:{_settings.WebPort}/");
            try { _listener.Start(); }
            catch (HttpListenerException e2) { Log.Write($"Web page could not start: {e2.Message}"); return false; }
        }
        catch (HttpListenerException e)
        {
            Log.Write($"Web page could not start on port {_settings.WebPort}: {e.Message}");
            return false;
        }
        Url = $"http://localhost:{_settings.WebPort}/";
        Log.Write($"Web page: {Url}" + (_settings.WebOnNetwork ? $" (also http://<this server>:{_settings.WebPort}/)" : ""));
        _ = Task.Run(() => Loop(_stop.Token));
        return true;
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch (Exception) when (ct.IsCancellationRequested || !_listener.IsListening) { return; }
            catch (HttpListenerException) { continue; }
            _ = Task.Run(() => Serve(ctx));
        }
    }

    private void Serve(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        try
        {
            res.Headers["Cache-Control"] = "no-store";
            res.Headers["X-Content-Type-Options"] = "nosniff";
            if ((req.Url?.AbsolutePath ?? "") == "/mcp")
            {
                ServeMcp(req, res);
                return;
            }

            string path = req.Url?.AbsolutePath ?? "/";
            string method = req.HttpMethod;
            // The page itself is public so the sign-in screen can load. Everything else waits for a session.
            if (method == "GET" && path == "/")
            {
                Text(res, Page.Html, "text/html; charset=utf-8");
                return;
            }
            // Fonts are public too (the sign-in screen uses them) and never change within a version.
            if (method == "GET" && path.StartsWith("/fonts/", StringComparison.Ordinal) && ServeFont(res, path[1..]))
                return;
            if (path is "/api/session" or "/api/session/login" or "/api/session/setup" or "/api/session/logout")
            {
                ServeSession(req, res, method, path);
                return;
            }
            if (!SignedIn(req))
            {
                res.StatusCode = 401;
                Text(res, "Sign in required", "text/plain");
                return;
            }

            // A file to open is sent as raw bytes. application/octet-stream is not a type a cross-site
            // form can send, so this keeps the same protection as the JSON rule below.
            if (method == "POST" && path == "/api/import")
            {
                ImportUpload(req, res);
                return;
            }

            // Changes only by POST from this page (a page elsewhere can't read the answer, and
            // the JSON content type keeps simple cross-site form posts out).
            if (method == "POST" && !(req.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 415;
                Text(res, "Expected JSON", "text/plain");
                return;
            }

            switch (method, path)
            {
                case ("GET", "/api/status"):
                    JsonOut(res, Status(SessionUser(req)));
                    break;
                case ("POST", "/api/start"):
                    { var (ok, msg) = _recorder.Start(); JsonOut(res, new { ok, message = msg }, ok ? 200 : 409); }
                    break;
                case ("POST", "/api/stop"):
                    { var (ok, msg) = _recorder.Stop(); JsonOut(res, new { ok, message = msg }, ok ? 200 : 409); }
                    break;
                case ("GET", "/api/calls"):
                    JsonOut(res, _recorder.Store.CallList(DateTime.UtcNow, req.QueryString["sbc"], req.QueryString["options"] == "1"));
                    break;
                case ("GET", "/api/streams"):
                    JsonOut(res, _recorder.Store.Streams(req.QueryString["sbc"]));
                    break;
                case ("GET", "/api/syslog"):
                    JsonOut(res, _recorder.Store.SyslogLines(req.QueryString["q"], int.TryParse(req.QueryString["limit"], out int l) ? l : 500, req.QueryString["sbc"], req.QueryString["errors"] == "1"));
                    break;
                case ("GET", "/api/syslog/view"):
                    {
                        var qs = req.QueryString;
                        var q = new CallStore.SyslogQuery
                        {
                            Text = qs["q"] ?? "",
                            Regex = qs["regex"] == "1",
                            CaseSensitive = qs["case"] == "1",
                            WholeWord = qs["word"] == "1",
                            Exclude = qs["exclude"] == "1",
                            Sbc = qs["sbc"] ?? "",
                            Level = qs["level"] ?? "",
                            Session = qs["sid"] ?? "",
                            Kind = qs["kind"] ?? "",
                            After = long.TryParse(qs["after"], out long after) ? after : 0,
                            Limit = int.TryParse(qs["limit"], out int lim2) ? lim2 : 2000,
                        };
                        JsonOut(res, _recorder.Store.SyslogView(q));
                    }
                    break;
                case ("GET", "/api/syslog/save"):
                    {
                        string sbc = req.QueryString["sbc"] ?? "";
                        string body = _recorder.Store.SyslogText(sbc, req.QueryString["utc"] != "1");
                        res.Headers["Content-Disposition"] = $"attachment; filename=\"syslog-{DateTime.Now:yyyyMMdd-HHmmss}.txt\"";
                        Text(res, body, "text/plain; charset=utf-8");
                    }
                    break;
                case ("POST", "/api/syslog/clean"):
                    _recorder.CleanSyslog();
                    JsonOut(res, new { ok = true, message = "Syslog view cleaned." });
                    break;
                case ("POST", "/api/paste"):
                    {
                        var b = Body<LabelBody>(req);
                        if (string.IsNullOrWhiteSpace(b?.Text)) { JsonOut(res, new { ok = false, message = "Nothing pasted." }, 400); break; }
                        var (ok, msg) = _recorder.Paste(b.Text);
                        JsonOut(res, new { ok, message = msg }, ok ? 200 : 409);
                    }
                    break;
                case ("POST", "/api/files/close"):
                    { var (ok, msg) = _recorder.BackToLive(); JsonOut(res, new { ok, message = msg }, ok ? 200 : 409); }
                    break;
                case ("POST", "/api/watch"):
                    {
                        var b = Body<LabelBody>(req);
                        var (ok, msg) = _recorder.Watch(b?.Text ?? "");
                        JsonOut(res, new { ok, message = msg }, ok ? 200 : 400);
                    }
                    break;
                case ("POST", "/api/watch/stop"):
                    _recorder.StopWatching();
                    JsonOut(res, new { ok = true, message = "Stopped following the file." });
                    break;
                case ("GET", "/api/syslog-tls.cer"):
                    try
                    {
                        byte[] der = _recorder.TlsCertificateDer();
                        res.ContentType = "application/pkix-cert";
                        res.Headers["Content-Disposition"] = "attachment; filename=\"mediantrace-syslog-tls.cer\"";
                        res.ContentLength64 = der.Length;
                        res.OutputStream.Write(der);
                    }
                    catch (Exception e) { res.StatusCode = 500; Text(res, "The certificate could not be made: " + e.Message, "text/plain; charset=utf-8"); }
                    break;
                case ("GET", "/api/files"):
                    JsonOut(res, Files());
                    break;
                case ("GET", "/api/files/search"):
                    SearchCaptures(req, res);
                    break;
                case ("POST", "/api/files/delete"):
                    DeleteCapture(req, res);
                    break;
                case ("POST", "/api/files/open"):
                    OpenCapture(req, res);
                    break;
                case ("GET", "/api/log"):
                    JsonOut(res, Log.Snapshot());
                    break;
                case ("POST", "/api/settings"):
                    SaveSettings(req, res);
                    break;
                case ("GET", "/api/changes"):
                    JsonOut(res, _changes.Plans().Take(100).Select(p => new
                    {
                        p.Id, p.SbcName, p.Sbc, p.Kind, p.Commands, p.Reason, p.RequestedBy, p.Created, p.Expires, p.Status, p.ApprovedVia, p.Applied,
                        backup = p.Backup.Length > 0 ? Path.GetFileName(p.Backup) : "", p.Errors, p.Output,
                        code = p.Status == "pending" ? p.Code : "", // people see the code; MCP clients never do
                    }).ToList());
                    break;
                case ("POST", "/api/changes/approve"):
                    {
                        var b = Body<IdBody>(req);
                        try { var p = _changes.Apply(b?.Id ?? "", null, "page"); JsonOut(res, new { ok = p.Status == "applied", message = p.Status == "applied" ? "Applied." : "Applied with errors: see the output.", status = p.Status }); }
                        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 409); }
                    }
                    break;
                case ("POST", "/api/changes/reject"):
                    {
                        var b = Body<IdBody>(req);
                        try { _changes.Reject(b?.Id ?? ""); JsonOut(res, new { ok = true, message = "Rejected." }); }
                        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 409); }
                    }
                    break;
                case ("POST", "/api/changes/plan"):
                    {
                        var b = Body<PlanBody>(req);
                        try
                        {
                            var p = b?.Kind == "save" ? _changes.Plan(b.Sbc ?? "", ["write"], b.Reason ?? "", "page", "save")
                                : _changes.Plan(b?.Sbc ?? "", (b?.Commands ?? "").Split('\n'), b?.Reason ?? "", "page");
                            JsonOut(res, new { ok = true, message = "Plan created.", id = p.Id });
                        }
                        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
                    break;
                case ("POST", "/api/sbc-login"):
                    SaveLogin(req, res);
                    break;
                case ("POST", "/api/labels/refresh"):
                    {
                        var b = Body<IdBody>(req);
                        try
                        {
                            string saved = _changes.Backup(b?.Id ?? "");
                            int n = ApplyNames(b?.Id ?? "", File.ReadAllText(saved));
                            JsonOut(res, new { ok = true, message = $"Read {n} names. Configuration saved as {Path.GetFileName(saved)}." });
                        }
                        catch (Exception e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
                    break;
                case ("POST", "/api/labels/backup"):
                    {
                        var b = Body<IdBody>(req);
                        try
                        {
                            string ip = SbcIp(b?.Id ?? "");
                            string? file = NewestBackup(ip);
                            if (file is null) { JsonOut(res, new { ok = false, message = "No saved configuration for this SBC yet." }, 404); break; }
                            int n = ApplyNames(ip, File.ReadAllText(file));
                            JsonOut(res, new { ok = true, message = $"Used {n} names from the backup {Path.GetFileName(file)}." });
                        }
                        catch (Exception e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
                    break;
                case ("POST", "/api/labels/save"):
                    SaveLabel(req, res);
                    break;
                case ("POST", "/api/labels/import"):
                    ImportLabels(req, res);
                    break;
                case ("POST", "/api/labels/delete"):
                    DeleteLabel(req, res);
                    break;
                case ("GET", "/api/standards/drift"):
                    JsonOut(res, _drift.Snapshot());
                    break;
                case ("GET", "/api/standards"):
                    JsonOut(res, IniFiles.List(_settings.Sbcs.Select(s => s.Ip)));
                    break;
                case ("GET", "/api/standards/text"):
                    StandardText(req, res);
                    break;
                case ("POST", "/api/standards/save"):
                    SaveStandard(req, res);
                    break;
                case ("POST", "/api/standards/download"):
                    DownloadIni(req, res);
                    break;
                case ("POST", "/api/sbc-login/test"):
                    {
                        var b = Body<IdBody>(req);
                        try
                        {
                            var outp = _changes.Show(b?.Id ?? "", ["show system version"]);
                            string version = outp[0].output.Split('\n').FirstOrDefault(l => l.Contains("Software Version", StringComparison.OrdinalIgnoreCase))?.Trim(' ', ';') ?? "connected";
                            JsonOut(res, new { ok = true, message = $"Login works: {version}" });
                        }
                        catch (Exception e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
                    break;
                case ("GET", "/api/health"):
                    JsonOut(res, _health.Snapshot(req.QueryString["refresh"]));
                    break;
                case ("GET", "/api/ready"):
                    JsonOut(res, new
                    {
                        config = _ready.Config(),
                        runs = _ready.Runs(30),
                        running = _ready.Running,
                        tests = _tester.Tests(),
                        alerts = _settings.AlertWebhook.Length > 0 || _settings.AlertEmailTo.Length > 0,
                    });
                    break;
                case ("POST", "/api/ready/run"):
                    {
                        var run = _ready.Start("manual", SessionUser(req));
                        JsonOut(res, run is null ? new { ok = false, message = "A ready check is already running." } : new { ok = true, message = "Ready check started." }, run is null ? 409 : 200);
                    }
                    break;
                case ("POST", "/api/ready/stop"):
                    JsonOut(res, _ready.Stop() ? new { ok = true, message = "Stopping the ready check." } : new { ok = false, message = "No ready check is running." });
                    break;
                case ("POST", "/api/ready/config"):
                    {
                        var c = Body<ReadyCheck.ReadyConfig>(req);
                        if (c is null) { JsonOut(res, new { ok = false, message = "Nothing to save" }, 400); break; }
                        var (ok, msg) = _ready.SaveConfig(c);
                        JsonOut(res, new { ok, message = msg }, ok ? 200 : 400);
                    }
                    break;
                case ("GET", "/api/storage"):
                    JsonOut(res, new
                    {
                        rows = _storage.Rows(),
                        settings = new
                        {
                            traceFolder = _settings.TraceFolder, retentionDays = _settings.RetentionDays, retentionMaxMb = _settings.RetentionMaxMb,
                            logFolder = _settings.LogFolder, logRetentionDays = _settings.LogRetentionDays, logMaxMb = _settings.LogMaxMb, logMaxTotalMb = _settings.LogMaxTotalMb,
                            backupFolder = _settings.BackupFolder, backupRetentionDays = _settings.BackupRetentionDays,
                            programLogFolder = _settings.ProgramLogFolder, programLogMaxMb = _settings.ProgramLogMaxMb, historyDays = _settings.HistoryDays,
                        },
                        programFolder = AppContext.BaseDirectory,
                    });
                    break;
                case ("GET", "/api/storage/browse"):
                    {
                        string? browse = req.QueryString["path"];
                        if (string.IsNullOrWhiteSpace(browse)) { JsonOut(res, new { path = "", parent = (string?)null, drives = Storage.Drives(), folders = Array.Empty<object>() }); break; }
                        var (full, parent, folders, error) = Storage.Browse(browse);
                        JsonOut(res, new { path = full, parent = parent ?? "", folders, error });
                    }
                    break;
                case ("POST", "/api/storage/mkdir"):
                    {
                        var b = Body<MkdirBody>(req);
                        var (made, error) = Storage.MakeFolder(b?.Parent ?? "", b?.Name ?? "");
                        JsonOut(res, made is null ? new { ok = false, message = error, path = "" } : new { ok = true, message = "Folder made.", path = made }, made is null ? 400 : 200);
                    }
                    break;
                case ("POST", "/api/storage/cleanup"):
                    JsonOut(res, new { ok = true, message = "Clean-up: " + _storage.CleanUp() + "." });
                    break;
                case ("GET", "/api/timeline"):
                    {
                        double hours = double.TryParse(req.QueryString["hours"], out double hh) ? Math.Clamp(hh, 1, 24 * 31) : 24;
                        JsonOut(res, _timeline.Since(DateTime.UtcNow.AddHours(-hours)));
                    }
                    break;
                case ("GET", "/api/upgrades"):
                    JsonOut(res, new { records = _upgrades.List(), sbcs = _settings.Sbcs.Select(x => new { x.Ip, x.Name, login = x.HasLogin }) });
                    break;
                case ("GET", "/api/upgrades/compare"):
                    try { JsonOut(res, _upgrades.Compare(req.QueryString["id"] ?? "")); }
                    catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    break;
                case ("POST", "/api/upgrades/snapshot"):
                    {
                        var b = Body<UpgradeBody>(req);
                        try
                        {
                            var (ok, msg, id) = _upgrades.Take(b?.Sbc, b?.Id, b?.Note);
                            JsonOut(res, new { ok, message = msg, id }, ok ? 200 : 400);
                        }
                        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
                    break;
                case ("POST", "/api/upgrades/delete"):
                    {
                        var b = Body<UpgradeBody>(req);
                        JsonOut(res, _upgrades.Delete(b?.Id ?? "") ? new { ok = true, message = "Removed (the config backups stay in sbc-backups)." } : new { ok = false, message = "Nothing removed." });
                    }
                    break;
                case ("GET", "/api/lines/monitor"):
                    JsonOut(res, _monitor.Snapshot(req.QueryString["line"], int.TryParse(req.QueryString["limit"], out int lim) ? Math.Clamp(lim, 1, 2000) : 200));
                    break;
                case ("POST", "/api/lines/monitor"):
                    {
                        var c = Body<LineMonitor.MonitorConfig>(req);
                        if (c is null) { JsonOut(res, new { ok = false, message = "Nothing to save" }, 400); break; }
                        JsonOut(res, new { ok = true, message = _monitor.SaveConfig(c) });
                    }
                    break;
                case ("GET", "/api/lines"):
                    JsonOut(res, _board.Snapshot());
                    break;
                case ("POST", "/api/lines/import"):
                    {
                        var b = Body<LineBoard.ImportInput>(req);
                        if (b is null) { JsonOut(res, new { ok = false, message = "Nothing to import" }, 400); break; }
                        var (added, errors) = _board.Import(b);
                        if (errors.Count > 0) JsonOut(res, new { ok = false, message = errors.Count == 1 ? errors[0] : $"{errors.Count} problems in the sheet", errors }, 400);
                        else JsonOut(res, new { ok = true, message = $"{added} line{(added == 1 ? "" : "s")} on the board." });
                    }
                    break;
                case ("POST", "/api/lines/delete"):
                    {
                        var b = Body<IdsBody>(req);
                        int n = (b?.Ids ?? []).Count(_board.Delete);
                        JsonOut(res, new { ok = n > 0, message = n > 0 ? $"Removed {n} line{(n == 1 ? "" : "s")}." : "No such line." });
                    }
                    break;
                case ("POST", "/api/lines/test"):
                    {
                        // Line test of the chosen lines (all when none given): wire checks and calls, as on Line tests.
                        var b = Body<IdsBody>(req);
                        var ids = b?.Ids ?? [];
                        var lines = _board.All().Where(l => ids.Count == 0 || ids.Contains(l.Id)).ToList();
                        if (lines.Count == 0) { JsonOut(res, new { ok = false, message = "No lines to test." }, 400); break; }
                        var (run, errors) = _bulk.Start(_board.TestInput(lines, lines.Count == 1 ? $"Board: {lines[0].Name}" : $"Board: {lines.Count} lines"));
                        if (run is null) JsonOut(res, new { ok = false, message = errors.Count == 1 ? errors[0] : string.Join(" ", errors) }, 400);
                        else JsonOut(res, new { ok = true, message = lines.Count == 1 ? $"Testing {lines[0].Name}." : $"Testing {lines.Count} lines.", id = run.Id });
                    }
                    break;
                case ("GET", "/api/bulk"):
                    JsonOut(res, _bulk.Runs().Take(20).ToList());
                    break;
                case ("POST", "/api/bulk/start"):
                    {
                        var b = Body<BulkTests.StartInput>(req);
                        if (b is null) { JsonOut(res, new { ok = false, message = "Nothing to run" }, 400); break; }
                        var (run, errors) = _bulk.Start(b);
                        if (run is null) JsonOut(res, new { ok = false, message = errors.Count == 1 ? errors[0] : $"{errors.Count} problems in the sheet", errors }, 400);
                        else JsonOut(res, new { ok = true, message = $"Calling {run.Lines.Count} lines.", id = run.Id });
                    }
                    break;
                case ("POST", "/api/bulk/stop"):
                    JsonOut(res, _bulk.Stop() ? new { ok = true, message = "Stopping after the current line." } : new { ok = false, message = "No bulk run in progress." });
                    break;
                case ("GET", "/api/tests"):
                    JsonOut(res, new { tests = _tester.Tests(), live = _tester.Live is { } lv ? LiveView(lv) : null, error = _tester.ListenError });
                    break;
                case ("POST", "/api/tests/save"):
                    {
                        var input = Body<SipTester.TestInput>(req);
                        if (input is null) { JsonOut(res, new { ok = false, message = "Nothing to save" }, 400); break; }
                        var (ok, msg, id) = _tester.Save(input);
                        JsonOut(res, new { ok, message = msg, id }, ok ? 200 : 400);
                    }
                    break;
                case ("POST", "/api/tests/delete"):
                    { var b = Body<IdBody>(req); bool ok = b?.Id is not null && _tester.Delete(b.Id); JsonOut(res, new { ok, message = ok ? "Deleted." : "No such test." }, ok ? 200 : 404); }
                    break;
                case ("POST", "/api/tests/run"):
                    {
                        var b = Body<IdBody>(req);
                        var (ok, msg, rid) = b?.Id is null ? (false, "No test given.", (string?)null) : _tester.Run(b.Id);
                        JsonOut(res, new { ok, message = msg, resultId = rid }, ok ? 200 : 409);
                    }
                    break;
                case ("GET", "/api/tests/results"):
                    JsonOut(res, _tester.Results(req.QueryString["testId"], int.TryParse(req.QueryString["limit"], out int tl) ? tl : 100));
                    break;
                case ("GET", _) when path.StartsWith("/api/tests/results/", StringComparison.Ordinal) && path.EndsWith("/recording.wav", StringComparison.Ordinal):
                    {
                        string rid = path["/api/tests/results/".Length..^"/recording.wav".Length];
                        byte[]? wav = _tester.Recording(rid);
                        if (wav is null) { res.StatusCode = 404; break; }
                        res.ContentType = "audio/wav";
                        res.ContentLength64 = wav.Length;
                        res.OutputStream.Write(wav);
                    }
                    break;
                case ("GET", _) when path.StartsWith("/api/tests/results/", StringComparison.Ordinal):
                    {
                        var r = _tester.Result(path["/api/tests/results/".Length..]);
                        if (r is null) { res.StatusCode = 404; break; }
                        lock (r) JsonOut(res, r);
                    }
                    break;
                case ("GET", _) when path.StartsWith("/api/calls/", StringComparison.Ordinal):
                    CallRoute(path, req, res);
                    break;
                case ("GET", _) when path.StartsWith("/files/", StringComparison.Ordinal):
                    Download(Uri.UnescapeDataString(path["/files/".Length..]), res);
                    break;
                default:
                    res.StatusCode = 404;
                    Text(res, "Not found", "text/plain");
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Write($"Web request failed: {e.Message}");
            try { res.StatusCode = 500; Text(res, "Error", "text/plain"); } catch { }
        }
        finally
        {
            try { res.Close(); } catch { }
        }
    }

    /// <summary>The app opens after the sign-in page, or when a client sends a Basic header for one of the people. An empty password never opens it.</summary>
    internal static bool SignedIn(string user, string password, bool webOnNetwork, bool isLocal, string? authorization, bool sessionOk)
    {
        if (sessionOk) return true;
        if (password.Length == 0) return false;
        if (!TryBasic(authorization, out string givenUser, out string givenPassword)) return false;
        return AccountMatches([(user, password)], givenUser, givenPassword);
    }

    /// <summary>True when this username and password belong to one of the people who can sign in. The username is not case sensitive.</summary>
    internal static bool AccountMatches(IEnumerable<(string User, string Password)> accounts, string user, string password)
    {
        foreach ((string name, string secret) in accounts)
        {
            if (secret.Length == 0) continue;
            if (name.Length > 0 && !name.Equals(user, StringComparison.OrdinalIgnoreCase)) continue;
            if (Same(password, secret)) return true;
        }
        return false;
    }

    private bool SignedIn(HttpListenerRequest req) =>
        HasSession(req) || (_settings.Accounts.Count > 0 && TryBasic(req.Headers["Authorization"], out string user, out string password) && AccountMatches(_settings.Accounts.Select(a => (a.User, a.Password)), user, password));

    private bool HasSession(HttpListenerRequest req)
    {
        string? token = req.Cookies["mt"]?.Value;
        if (string.IsNullOrEmpty(token)) return false;
        lock (_sessionGate) return _sessions.ContainsKey(token);
    }

    private string SessionUser(HttpListenerRequest req)
    {
        string? token = req.Cookies["mt"]?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            lock (_sessionGate)
                if (_sessions.TryGetValue(token, out string? name)) return name;
        }
        if (TryBasic(req.Headers["Authorization"], out string user, out _) && _settings.Accounts.Exists(a => a.User.Equals(user, StringComparison.OrdinalIgnoreCase)))
            return _settings.Accounts.First(a => a.User.Equals(user, StringComparison.OrdinalIgnoreCase)).User;
        return "";
    }

    private void GrantSession(HttpListenerResponse res, string user)
    {
        string token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        lock (_sessionGate) _sessions[token] = user;
        res.Headers.Add("Set-Cookie", $"mt={token}; Path=/; HttpOnly; SameSite=Strict");
    }

    private void DropUser(string user)
    {
        lock (_sessionGate)
        {
            foreach (string token in _sessions.Where(p => p.Value.Equals(user, StringComparison.OrdinalIgnoreCase)).Select(p => p.Key).ToList())
                _sessions.Remove(token);
        }
    }

    private void ServeSession(HttpListenerRequest req, HttpListenerResponse res, string method, string path)
    {
        if (method == "GET" && path == "/api/session")
        {
            bool signedIn = SignedIn(req);
            JsonOut(res, new
            {
                signedIn,
                needsSetup = _settings.Accounts.Count == 0,
                user = signedIn ? SessionUser(req) : "",
            });
            return;
        }
        if (method == "POST" && path == "/api/session/logout")
        {
            string? token = req.Cookies["mt"]?.Value;
            if (!string.IsNullOrEmpty(token)) lock (_sessionGate) _sessions.Remove(token);
            res.Headers.Add("Set-Cookie", "mt=; Path=/; HttpOnly; SameSite=Strict; Max-Age=0");
            JsonOut(res, new { ok = true });
            return;
        }
        if (method != "POST" || !(req.ContentType ?? "").StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = 415;
            Text(res, "Expected JSON", "text/plain");
            return;
        }
        var body = Body<SessionBody>(req);
        string user = (body?.User ?? "").Trim();
        string password = body?.Password ?? "";
        if (path == "/api/session/setup")
        {
            if (_settings.Accounts.Count > 0)
            { JsonOut(res, new { ok = false, message = "A sign-in is already set. Sign in with it." }, 409); return; }
            if (UserProblem(user) is string problem) { JsonOut(res, new { ok = false, message = problem }, 400); return; }
            if (password.Length == 0) { JsonOut(res, new { ok = false, message = "Type a password." }, 400); return; }
            if (password.Length > 200) { JsonOut(res, new { ok = false, message = "That password is too long." }, 400); return; }
            _settings.Accounts.Add(new PageAccount { User = user, Password = password });
            _settings.Save();
            Log.Write($"Sign-in created for {user}");
            GrantSession(res, user);
            JsonOut(res, new { ok = true });
            return;
        }
        if (path == "/api/session/login")
        {
            if (_settings.Accounts.Count == 0)
            { JsonOut(res, new { ok = false, message = "Create a username and password first." }, 400); return; }
            PageAccount? account = _settings.Accounts.FirstOrDefault(a => AccountMatches([(a.User, a.Password)], user, password));
            if (account is null)
            { JsonOut(res, new { ok = false, message = "The username or password is wrong." }, 401); return; }
            GrantSession(res, account.User);
            JsonOut(res, new { ok = true });
            return;
        }
        res.StatusCode = 404;
        Text(res, "Not found", "text/plain");
    }

    private static string? UserProblem(string user)
    {
        if (user.Length == 0) return "Type a username.";
        if (user.Contains(':') || user.Any(char.IsControl) || user.Length > 64) return "The username cannot contain a colon.";
        return null;
    }

    /// <summary>Page access. A username and password are checked for everyone. A password on its own, the older setting, still applies only off this PC.</summary>
    internal static bool AccessOk(string user, string password, bool webOnNetwork, bool isLocal, string? authorization)
    {
        bool named = user.Length > 0;
        if (!named && password.Length == 0) return true;
        if (!named && (!webOnNetwork || isLocal)) return true;
        if (authorization is null || !authorization.StartsWith("Basic ", StringComparison.Ordinal)) return false;
        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..]));
            int colon = decoded.IndexOf(':');
            string givenUser = colon >= 0 ? decoded[..colon] : "";
            string givenPassword = colon >= 0 ? decoded[(colon + 1)..] : decoded;
            if (named && !Same(givenUser, user)) return false;
            return password.Length > 0 && Same(givenPassword, password);
        }
        catch { return false; }
    }

    private static bool TryBasic(string? authorization, out string user, out string password)
    {
        user = "";
        password = "";
        if (authorization is null || !authorization.StartsWith("Basic ", StringComparison.Ordinal)) return false;
        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[6..]));
            int colon = decoded.IndexOf(':');
            user = colon >= 0 ? decoded[..colon] : "";
            password = colon >= 0 ? decoded[(colon + 1)..] : decoded;
            return true;
        }
        catch { return false; }
    }

    private static bool Same(string a, string b)
    {
        byte[] x = Encoding.UTF8.GetBytes(a), y = Encoding.UTF8.GetBytes(b);
        return x.Length == y.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(x, y);
    }

    private object Status(string signedInAs)
    {
        var (calls, streams, syslog) = _recorder.Store.Counts();
        return new
        {
            recording = _recorder.Recording,
            startedAt = _recorder.StartedAt,
            stopsAt = _recorder.StopsAt,
            now = DateTime.UtcNow,
            signedInAs,
            pcap = _recorder.PcapPath is null ? null : Path.GetFileName(_recorder.PcapPath),
            opened = _recorder.Opened,
            watching = _recorder.Watching,
            watchLines = _recorder.WatchLines,
            watchError = _recorder.WatchError,
            text = _recorder.TextPath is null ? null : Path.GetFileName(_recorder.TextPath),
            drPackets = _recorder.DrPackets,
            syslogPackets = _recorder.SyslogPackets,
            sipMessages = _recorder.SipMessages,
            rtpPackets = _recorder.RtpPackets,
            ignored = _recorder.Ignored,
            calls,
            streams,
            syslogLines = syslog,
            sources = _recorder.Sources().Select(s => new { address = s.Address, kind = s.Kind, packets = s.Packets, lastSeen = s.LastSeen }),
            listenErrors = _recorder.ListenErrors,
            sbcs = SbcStatus(),
            settings = new
            {
                onlyListedSbcs = _settings.OnlyListedSbcs,
                drPort = _settings.DebugRecordingPort,
                syslogPort = _settings.SyslogPort,
                webPort = _settings.WebPort,
                autoStopMinutes = _settings.AutoStopMinutes,
                traceFolder = _settings.TraceFolderFull,
                backupFolder = ChangeManager.BackupDir,
                logFolder = _recorder.LogFolder,
                logFile = _recorder.LogFile,
                logMaxMb = _settings.LogMaxMb,
                logRetentionDays = _settings.LogRetentionDays,
                webOnNetwork = _settings.WebOnNetwork,
                accounts = _settings.Accounts.Select(a => a.User).ToList(),
                listening = new { web = _boundWeb, syslog = _recorder.ListeningPort("SYSLOG"), dr = _recorder.ListeningPort("DR"), syslogTcp = _recorder.StreamPort("TCP"), syslogTls = _recorder.StreamPort("TLS") },
                syslogTcpPort = _settings.SyslogTcpPort,
                syslogTlsPort = _settings.SyslogTlsPort,
                syslogTlsPfx = _settings.SyslogTlsPfx,
                syslogTlsThumbprint = _recorder.TlsThumbprint,
                syslogStreamConnections = _recorder.StreamConnections(),
                syslogReorder = _settings.SyslogReorder,
                syslogBufferLines = _settings.SyslogBufferLines,
                ignoreSyslogFrom = _settings.IgnoreSyslogFrom,
                ignoreSyslogPatterns = _settings.IgnoreSyslogPatterns,
                writeLog = _settings.WriteLog,
                writeLogFolder = _settings.WriteLogFolder,
                writeLogFolderFull = _settings.WriteLogFolderFull,
                writeLogFile = _settings.WriteLogFile,
                writeLogFileMb = _settings.WriteLogFileMb,
                writeLogFileCount = _settings.WriteLogFileCount,
                writeLogPerDevice = _settings.WriteLogPerDevice,
                writeLogFolderPerDevice = _settings.WriteLogFolderPerDevice,
                writeLogUseNames = _settings.WriteLogUseNames,
                writeLogCurrent = _settings.WriteLog ? _recorder.WriteLogFile : "",
                writeLogLines = _recorder.WriteLogLines,
                writeLogError = _recorder.WriteLogError,
                // UDP feeds move as soon as the ports are saved; only the web page needs a restart.
                portsPending = _settings.WebPort != _boundWeb,
            },
            serverAddresses = LocalAddresses(),
            firewall = FirewallState(),
            mcp = new { token = _settings.McpToken, url = $"http://{(_settings.WebOnNetwork ? LocalAddresses().FirstOrDefault() ?? "localhost" : "localhost")}:{_boundWeb}/mcp" },
            changesPending = _changes.Plans().Count(p => p.Status == "pending"),
            bulkRunning = _bulk.Runs().Any(r => r.State == "running"),
            tester = new { port = _settings.TestSipPort, rtpFirst = _settings.TestRtpPortFirst, rtpCount = _settings.TestRtpPortCount, error = _tester.ListenError, running = _tester.Live?.Name, echoActive = _tester.EchoActive, echoAnswered = _tester.EchoAnswered },
            alerts = new
            {
                webhookSet = _settings.AlertWebhook.Length > 0,
                emailTo = _settings.AlertEmailTo,
                emailFrom = _settings.AlertEmailFrom,
                smtpHost = _settings.AlertSmtpHost,
                smtpPort = _settings.AlertSmtpPort,
                smtpUser = _settings.AlertSmtpUser,
                smtpPasswordSet = _settings.AlertSmtpPassword.Length > 0,
            },
            version = typeof(WebServer).Assembly.GetName().Version?.ToString(3),
            labels = _labels.All(),
        };
    }

    /// <summary>Each configured SBC with what is arriving from it, then any unlisted sender.</summary>
    private List<object> SbcStatus()
    {
        var sources = _recorder.Sources();
        var order = _recorder.SequenceStats();
        object? Seq(string ip) => order.TryGetValue(ip, out var s) ? new { received = s.Received, missing = s.Missing, outOfOrder = s.OutOfOrder, restarts = s.Restarts, lastGap = s.LastGap } : null;
        var list = new List<object>();
        foreach (SbcEntry sbc in _settings.Sbcs.ToList())
        {
            var mine = sources.Where(x => x.Address == sbc.Ip).ToList();
            list.Add(new
            {
                name = sbc.Name, ip = sbc.Ip, url = sbc.Url, listed = true,
                sshHost = sbc.SshHost, sshPort = sbc.SshPort, sshUser = sbc.SshUser, hasPassword = sbc.SshPasswordProtected.Length > 0, hasEnable = sbc.EnablePasswordProtected.Length > 0,
                sequence = Seq(sbc.Ip),
                syslog = mine.FirstOrDefault(x => x.Kind == "SYSLOG") is { } sy ? new { packets = sy.Packets, lastSeen = sy.LastSeen } : null,
                dr = mine.FirstOrDefault(x => x.Kind == "DR") is { } d ? new { packets = d.Packets, lastSeen = d.LastSeen } : null,
            });
        }
        foreach (var group in sources.Where(x => _settings.FindSbc(x.Address) is null).GroupBy(x => x.Address))
        {
            list.Add(new
            {
                name = "", ip = group.Key, url = "", listed = false,
                sequence = Seq(group.Key),
                syslog = group.FirstOrDefault(x => x.Kind == "SYSLOG") is { } sy ? new { packets = sy.Packets, lastSeen = sy.LastSeen } : null,
                dr = group.FirstOrDefault(x => x.Kind == "DR") is { } d ? new { packets = d.Packets, lastSeen = d.LastSeen } : null,
            });
        }
        return list;
    }

    private void CallRoute(string path, HttpListenerRequest req, HttpListenerResponse res)
    {
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries); // api, calls, {id}[, text]
        if (parts.Length < 3 || !int.TryParse(parts[2], out int id)) { res.StatusCode = 404; return; }
        if (parts.Length == 4 && parts[3] == "text")
        {
            string text = _recorder.Store.CallText(id);
            if (text.Length == 0) { res.StatusCode = 404; return; }
            res.Headers["Content-Disposition"] = $"attachment; filename=\"call-{id}.txt\"";
            Text(res, text, "text/plain; charset=utf-8");
            return;
        }
        if (parts.Length == 4 && parts[3] == "audio")
        {
            var (ok, message, wav) = _recorder.StreamWav(id, req.QueryString["ssrc"] ?? "", req.QueryString["src"] ?? "", req.QueryString["dst"] ?? "");
            if (!ok || wav is null) { res.StatusCode = 404; Text(res, message, "text/plain; charset=utf-8"); return; }
            res.ContentType = "audio/wav";
            res.Headers["Content-Disposition"] = "inline; filename=\"call-" + id + ".wav\"";
            res.ContentLength64 = wav.Length;
            res.OutputStream.Write(wav);
            return;
        }
        if (parts.Length == 4 && parts[3] == "pcap")
        {
            var (ok, message, bytes, name) = _recorder.CallPcap(id);
            if (!ok || bytes is null) { res.StatusCode = 404; Text(res, message, "text/plain; charset=utf-8"); return; }
            res.ContentType = "application/vnd.tcpdump.pcap";
            res.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes);
            return;
        }
        object? detail = _recorder.Store.CallDetail(id, DateTime.UtcNow);
        if (detail is null) { res.StatusCode = 404; return; }
        JsonOut(res, detail);
    }

    private object Files()
    {
        var files = CaptureFiles.List(_settings.TraceFolderFull, IsOpenCapture);
        long used = files.Sum(f => f.Size);
        var (free, drive) = CaptureFiles.FreeSpace(_settings.TraceFolderFull);
        return new
        {
            files = files.Select(f => new { f.Name, f.Size, f.Modified, f.Active }),
            used, free, drive,
            retentionDays = _settings.RetentionDays,
            retentionMaxMb = _settings.RetentionMaxMb,
        };
    }

    private void SearchCaptures(HttpListenerRequest req, HttpListenerResponse res)
    {
        string query = (req.QueryString["q"] ?? "").Trim();
        if (query.Length < 2) { JsonOut(res, new { ok = false, message = "Type at least two characters." }, 400); return; }
        var hits = CaptureSearch.Find(_settings.TraceFolderFull, query, out bool limited);
        JsonOut(res, new
        {
            query,
            limited,
            hits = hits.Select(h => new
            {
                h.Stem, h.Started, h.Sbc,
                sbcName = _settings.FindSbc(h.Sbc)?.Name ?? "",
                h.From, h.To, h.CallId, h.Session,
                recording = IsOpenCapture(Path.Combine(_settings.TraceFolderFull, h.Stem + ".pcap")) || IsOpenCapture(Path.Combine(_settings.TraceFolderFull, h.Stem + ".txt")),
            }).ToList(),
        });
    }

    private bool IsOpenCapture(string path) =>
        _recorder.Recording && (string.Equals(path, _recorder.PcapPath, StringComparison.OrdinalIgnoreCase) || string.Equals(path, _recorder.TextPath, StringComparison.OrdinalIgnoreCase));

    private void DeleteCapture(HttpListenerRequest req, HttpListenerResponse res)
    {
        var body = Body<IdBody>(req);
        try
        {
            var removed = CaptureFiles.Delete(_settings.TraceFolderFull, body?.Id ?? "", IsOpenCapture);
            Log.Write($"Deleted capture {body?.Id}: {string.Join(", ", removed)}");
            JsonOut(res, new { ok = true, message = "Capture deleted." });
        }
        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
    }

    /// <summary>A log or capture uploaded from the page, opened like a saved capture. The file is not kept.</summary>
    private void ImportUpload(HttpListenerRequest req, HttpListenerResponse res)
    {
        if (!(req.ContentType ?? "").StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = 415;
            Text(res, "Expected a file", "text/plain");
            return;
        }
        const long MaxUpload = 1024L * 1024 * 1024;
        if (req.ContentLength64 > MaxUpload) { JsonOut(res, new { ok = false, message = "That file is over 1 GB." }, 413); return; }
        string name = Path.GetFileName(req.QueryString["name"] ?? "upload.txt");
        if (name.Length == 0) name = "upload.txt";
        if (name.Length > 120) name = name[^120..];
        string temp = Path.Combine(Path.GetTempPath(), $"MediantTrace-open-{Guid.NewGuid():N}{Path.GetExtension(name)}");
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = new byte[256 * 1024];
                long total = 0;
                int n;
                while ((n = req.InputStream.Read(buffer)) > 0)
                {
                    total += n;
                    if (total > MaxUpload) { JsonOut(res, new { ok = false, message = "That file is over 1 GB." }, 413); return; }
                    file.Write(buffer, 0, n);
                }
                if (total == 0) { JsonOut(res, new { ok = false, message = "The file is empty." }, 400); return; }
            }
            var (ok, message) = _recorder.Import(temp, name);
            JsonOut(res, new { ok, message }, ok ? 200 : 409);
        }
        finally
        {
            try { File.Delete(temp); } catch (IOException) { }
        }
    }

    private void OpenCapture(HttpListenerRequest req, HttpListenerResponse res)
    {
        var body = Body<IdBody>(req);
        var (ok, message) = _recorder.OpenSaved(body?.Id ?? "");
        JsonOut(res, new { ok, message }, ok ? 200 : 409);
    }

    private void Download(string name, HttpListenerResponse res)
    {
        // Only plain file names from the trace folder.
        if (name.Length == 0 || name != Path.GetFileName(name) || !name.StartsWith("mediant-", StringComparison.Ordinal)
            || !(name.EndsWith(".pcap", StringComparison.Ordinal) || name.EndsWith(".txt", StringComparison.Ordinal)))
        {
            res.StatusCode = 404;
            return;
        }
        string full = Path.Combine(_settings.TraceFolderFull, name);
        if (!File.Exists(full)) { res.StatusCode = 404; return; }
        res.ContentType = name.EndsWith(".pcap", StringComparison.Ordinal) ? "application/vnd.tcpdump.pcap" : "text/plain; charset=utf-8";
        res.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";
        using var file = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        res.ContentLength64 = file.Length;
        file.CopyTo(res.OutputStream);
    }

    private sealed class SessionBody { public string? User { get; set; } public string? Password { get; set; } }
    private sealed class IdBody { public string? Id { get; set; } }
    private sealed class LabelBody { public string? Sbc { get; set; } public string? Ip { get; set; } public string? Name { get; set; } public string? Detail { get; set; } public string? Text { get; set; } }

    private string SbcIp(string key)
    {
        string id = key.Trim();
        return _settings.Sbcs.FirstOrDefault(s => s.Ip == id || s.Name.Equals(id, StringComparison.OrdinalIgnoreCase))?.Ip ?? id;
    }

    private static string? NewestBackup(string ip)
    {
        if (!Directory.Exists(ChangeManager.BackupDir)) return null;
        return Directory.GetFiles(ChangeManager.BackupDir, ip + "-*-running-config.txt").OrderByDescending(f => f, StringComparer.Ordinal).FirstOrDefault();
    }

    private int ApplyNames(string key, string config)
    {
        string ip = SbcIp(key);
        return _labels.Replace(ip, ConfigNames.Parse(config), DateTime.UtcNow);
    }

    private void SaveLabel(HttpListenerRequest req, HttpListenerResponse res)
    {
        var b = Body<LabelBody>(req);
        try
        {
            _labels.SaveManual(SbcIp(b?.Sbc ?? ""), b?.Ip ?? "", b?.Name ?? "", b?.Detail ?? "");
            JsonOut(res, new { ok = true, message = "Name saved." });
        }
        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
    }

    private void ImportLabels(HttpListenerRequest req, HttpListenerResponse res)
    {
        var b = Body<LabelBody>(req);
        if (b is null) { JsonOut(res, new { ok = false, message = "The file is empty or too large." }, 400); return; }
        try
        {
            var (saved, skipped) = _labels.ImportCsv(SbcIp(b.Sbc ?? ""), b.Text ?? "");
            string message = $"Saved {saved} name{(saved == 1 ? "" : "s")}.";
            if (skipped.Count > 0)
                message += $" Skipped {skipped.Count} row{(skipped.Count == 1 ? "" : "s")}: {string.Join(" ", skipped.Take(3))}";
            JsonOut(res, new { ok = true, message, saved, skipped = skipped.Count });
        }
        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
    }

    private void DeleteLabel(HttpListenerRequest req, HttpListenerResponse res)
    {
        var b = Body<LabelBody>(req);
        bool gone = _labels.Remove(SbcIp(b?.Sbc ?? ""), b?.Ip ?? "");
        JsonOut(res, new { ok = gone, message = gone ? "Name removed." : "That address is not in the book." }, gone ? 200 : 404);
    }
    private sealed class PlanBody { public string? Sbc { get; set; } public string? Commands { get; set; } public string? Reason { get; set; } public string? Kind { get; set; } }
    private sealed class LoginBody
    {
        public string? Ip { get; set; }
        public string? SshHost { get; set; }
        public int? SshPort { get; set; }
        public string? SshUser { get; set; }
        public string? Password { get; set; }       // null = keep the stored one
        public string? EnablePassword { get; set; } // null = keep
    }

    private void StandardText(HttpListenerRequest req, HttpListenerResponse res)
    {
        try
        {
            string? text = IniFiles.Read(req.QueryString["ip"] ?? "");
            if (text is null) { JsonOut(res, new { message = "No standard saved for this SBC." }, 404); return; }
            JsonOut(res, new { text, bytes = Encoding.UTF8.GetByteCount(text) });
        }
        catch (InvalidOperationException e) { JsonOut(res, new { message = e.Message }, 400); }
    }

    private void SaveStandard(HttpListenerRequest req, HttpListenerResponse res)
    {
        IniBody? b;
        try { b = JsonSerializer.Deserialize<IniBody>(ReadLimited(req, IniFiles.MaxBytes + 1024 * 1024), Json); }
        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); return; }
        catch (JsonException) { JsonOut(res, new { ok = false, message = "The file could not be read." }, 400); return; }
        try
        {
            var sbc = _changes.Find(b?.Ip ?? "");
            IniFiles.Save(sbc.Ip, b?.Text ?? "");
            Log.Write($"Standard INI saved for {sbc.Name}");
            JsonOut(res, new { ok = true, message = "Standard saved." });
        }
        catch (InvalidOperationException e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
    }

    private void DownloadIni(HttpListenerRequest req, HttpListenerResponse res)
    {
        var b = Body<IdBody>(req);
        try
        {
            var sbc = _changes.Find(b?.Id ?? "");
            if (!sbc.HasLogin) throw new InvalidOperationException($"No login is saved for {sbc.Name}. Add one on the SBCs page. The same login is used to download the INI.");
            string text = IniFiles.Download(sbc.SshTarget, sbc.SshUser, ChangeManager.Unprotect(sbc.SshPasswordProtected), sbc.Url).GetAwaiter().GetResult();
            Log.Write($"INI downloaded from {sbc.Name} ({Encoding.UTF8.GetByteCount(text):N0} bytes)");
            JsonOut(res, new { text, bytes = Encoding.UTF8.GetByteCount(text) });
        }
        catch (Exception e) { JsonOut(res, new { message = e.Message }, 400); }
    }

    private static string ReadLimited(HttpListenerRequest req, int max)
    {
        if (req.ContentLength64 > max) throw new InvalidOperationException("That file is larger than 8 MB.");
        using var mem = new MemoryStream();
        var buf = new byte[81920];
        int n, total = 0;
        while ((n = req.InputStream.Read(buf, 0, buf.Length)) > 0)
        {
            total += n;
            if (total > max) throw new InvalidOperationException("That file is larger than 8 MB.");
            mem.Write(buf, 0, n);
        }
        return Encoding.UTF8.GetString(mem.ToArray());
    }

    private sealed class IniBody { public string? Ip { get; set; } public string? Text { get; set; } }

    private void SaveLogin(HttpListenerRequest req, HttpListenerResponse res)
    {
        var b = Body<LoginBody>(req);
        var e = _settings.Sbcs.FirstOrDefault(s => s.Ip == b?.Ip);
        if (b is null || e is null) { JsonOut(res, new { ok = false, message = "No such SBC." }, 404); return; }
        string host = (b.SshHost ?? "").Trim();
        if (host.Length > 0 && !IPAddress.TryParse(host, out _) && !System.Text.RegularExpressions.Regex.IsMatch(host, @"^[A-Za-z0-9.-]{1,253}$"))
        { JsonOut(res, new { ok = false, message = "The SSH address is not a valid address." }, 400); return; }
        e.SshHost = host;
        e.SshPort = Math.Clamp(b.SshPort ?? 22, 1, 65535);
        e.SshUser = (b.SshUser ?? "").Trim();
        if (b.Password is not null) e.SshPasswordProtected = ChangeManager.Protect(b.Password);
        if (b.EnablePassword is not null) e.EnablePasswordProtected = ChangeManager.Protect(b.EnablePassword);
        _settings.Save();
        Log.Write($"SSH login for {e.Name} saved");
        JsonOut(res, new { ok = true, message = "Saved." });
    }

    /// <summary>MCP over HTTP: bearer token, Origin check against DNS rebinding, JSON responses.</summary>
    private void ServeMcp(HttpListenerRequest req, HttpListenerResponse res)
    {
        string origin = req.Headers["Origin"] ?? "";
        if (origin.Length > 0 && Uri.TryCreate(origin, UriKind.Absolute, out Uri? o) && !o.IsLoopback && o.Host != req.Url?.Host)
        { res.StatusCode = 403; Text(res, "Origin not allowed", "text/plain"); return; }
        string auth = req.Headers["Authorization"] ?? "";
        string want = "Bearer " + _settings.McpToken;
        if (_settings.McpToken.Length == 0 || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(auth), Encoding.UTF8.GetBytes(want)))
        { res.StatusCode = 401; res.Headers["WWW-Authenticate"] = "Bearer"; Text(res, "MCP token required", "text/plain"); return; }
        if (req.HttpMethod != "POST") { res.StatusCode = 405; res.Headers["Allow"] = "POST"; Text(res, "Use POST", "text/plain"); return; }
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        string body = reader.ReadToEnd();
        if (body.Length > 1024 * 1024) { res.StatusCode = 413; return; }
        string? reply = _mcp.Handle(body);
        if (reply is null) { res.StatusCode = 202; return; }
        Text(res, reply, "application/json; charset=utf-8");
    }

    private sealed class IdsBody { public List<string>? Ids { get; set; } }
    private sealed class MkdirBody { public string? Parent { get; set; } public string? Name { get; set; } }
    private sealed class UpgradeBody { public string? Sbc { get; set; } public string? Id { get; set; } public string? Note { get; set; } }

    private static T? Body<T>(HttpListenerRequest req) where T : class
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        string text = reader.ReadToEnd();
        if (text.Length > 64 * 1024) return null;
        try { return JsonSerializer.Deserialize<T>(text, Json); } catch (JsonException) { return null; }
    }

    private static object LiveView(TestResult r)
    {
        lock (r) return new { r.Id, r.TestId, r.Name, r.Stage, r.Started, r.TalkSeconds, r.TxPackets, r.RxPackets, r.CallId };
    }

    private sealed class SettingsUpdate
    {
        public List<SbcEntry>? Sbcs { get; set; }
        public bool? OnlyListedSbcs { get; set; }
        public int? AutoStopMinutes { get; set; }
        public int? RetentionDays { get; set; }
        public int? RetentionMaxMb { get; set; }
        public int? SyslogPort { get; set; }
        public int? DebugRecordingPort { get; set; }
        public int? WebPort { get; set; }
        public string? AlertWebhook { get; set; }
        public string? AlertEmailTo { get; set; }
        public string? AlertEmailFrom { get; set; }
        public string? AlertSmtpHost { get; set; }
        public int? AlertSmtpPort { get; set; }
        public string? AlertSmtpUser { get; set; }
        public string? AlertSmtpPassword { get; set; }
        public string? WebUser { get; set; }
        public string? WebPassword { get; set; }
        public string? RemoveWebUser { get; set; }
        public bool? ClearWebLogin { get; set; }
        public int? LogMaxMb { get; set; }
        public int? LogRetentionDays { get; set; }
        public int? LogMaxTotalMb { get; set; }
        public string? TraceFolder { get; set; }
        public string? LogFolder { get; set; }
        public string? BackupFolder { get; set; }
        public int? BackupRetentionDays { get; set; }
        public string? ProgramLogFolder { get; set; }
        public int? ProgramLogMaxMb { get; set; }
        public int? HistoryDays { get; set; }
        public int? SyslogTcpPort { get; set; }
        public int? SyslogTlsPort { get; set; }
        public string? SyslogTlsPfx { get; set; }
        public string? SyslogTlsPfxPassword { get; set; }
        public bool? SyslogReorder { get; set; }
        public int? SyslogBufferLines { get; set; }
        public List<string>? IgnoreSyslogFrom { get; set; }
        public List<string>? IgnoreSyslogPatterns { get; set; }
        public bool? WriteLog { get; set; }
        public string? WriteLogFolder { get; set; }
        public string? WriteLogFile { get; set; }
        public int? WriteLogFileMb { get; set; }
        public int? WriteLogFileCount { get; set; }
        public bool? WriteLogPerDevice { get; set; }
        public bool? WriteLogFolderPerDevice { get; set; }
        public bool? WriteLogUseNames { get; set; }
    }

    private void SaveSettings(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var update = JsonSerializer.Deserialize<SettingsUpdate>(reader.ReadToEnd(), Json);
        if (update is null) { JsonOut(res, new { ok = false, message = "Nothing to save" }, 400); return; }
        string? webhook = update.AlertWebhook?.Trim();
        if (webhook is { Length: > 0 } && !TestAlerts.WebhookOk(webhook))
        { JsonOut(res, new { ok = false, message = "The webhook must be an https address, with no password in it." }, 400); return; }
        string to = (update.AlertEmailTo ?? _settings.AlertEmailTo).Trim();
        string from = (update.AlertEmailFrom ?? _settings.AlertEmailFrom).Trim();
        string host = (update.AlertSmtpHost ?? _settings.AlertSmtpHost).Trim();
        if (update.AlertEmailTo is not null || update.AlertEmailFrom is not null || update.AlertSmtpHost is not null)
        {
            if (to.Length > 0 && !TestAlerts.EmailOk(to))
            { JsonOut(res, new { ok = false, message = "The email address does not look right." }, 400); return; }
            if (from.Length > 0 && !TestAlerts.EmailOk(from))
            { JsonOut(res, new { ok = false, message = "The From address does not look right." }, 400); return; }
            if (to.Length > 0 && (host.Length == 0 || from.Length == 0))
            { JsonOut(res, new { ok = false, message = "Email needs a From address and the mail server." }, 400); return; }
            if (update.AlertSmtpPort is < 1 or > 65535)
            { JsonOut(res, new { ok = false, message = "The mail server port must be from 1 to 65535." }, 400); return; }
        }

        if (update.Sbcs is not null)
        {
            var clean = new List<SbcEntry>();
            foreach (SbcEntry e in update.Sbcs)
            {
                string name = (e.Name ?? "").Trim(), ip = (e.Ip ?? "").Trim(), url = (e.Url ?? "").Trim();
                if (name.Length == 0 && ip.Length == 0 && url.Length == 0) continue;
                if (!IPAddress.TryParse(ip, out _))
                { JsonOut(res, new { ok = false, message = $"'{ip}' is not an IP address (SBC {name})." }, 400); return; }
                if (url.Length > 0 && !url.Contains("://")) url = "https://" + url;
                if (url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out Uri? u) || (u.Scheme != "http" && u.Scheme != "https")))
                { JsonOut(res, new { ok = false, message = $"The web link for {name} must be an http or https address." }, 400); return; }
                if (clean.Any(c => c.Ip == ip))
                { JsonOut(res, new { ok = false, message = $"{ip} is listed twice." }, 400); return; }
                var kept = _settings.Sbcs.FirstOrDefault(x => x.Ip == ip);
                clean.Add(new SbcEntry
                {
                    Name = name.Length > 0 ? name : ip, Ip = ip, Url = url,
                    SshHost = kept?.SshHost ?? "", SshPort = kept?.SshPort ?? 22, SshUser = kept?.SshUser ?? "",
                    SshPasswordProtected = kept?.SshPasswordProtected ?? "", EnablePasswordProtected = kept?.EnablePasswordProtected ?? "",
                });
            }
            _settings.Sbcs = clean; // replaced whole, so readers never see a half-edited list
        }
        if (update.OnlyListedSbcs is bool only) _settings.OnlyListedSbcs = only;
        if (update.AutoStopMinutes is int minutes)
            _settings.AutoStopMinutes = Math.Clamp(minutes, 0, 24 * 60);
        if (update.LogMaxMb is int logMb)
            _settings.LogMaxMb = Math.Clamp(logMb, 1, 100);
        if (update.LogRetentionDays is int logDays)
            _settings.LogRetentionDays = Math.Clamp(logDays, 1, 3650);
        string? peopleMessage = null;
        if (!string.IsNullOrWhiteSpace(update.RemoveWebUser))
        {
            string name = update.RemoveWebUser.Trim();
            PageAccount? account = _settings.Accounts.FirstOrDefault(a => a.User.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (account is null) { JsonOut(res, new { ok = false, message = "No sign-in with that username." }, 404); return; }
            if (_settings.Accounts.Count < 2) { JsonOut(res, new { ok = false, message = "Keep at least one sign-in. Add the next person before removing this one." }, 400); return; }
            _settings.Accounts.Remove(account);
            DropUser(account.User);
            peopleMessage = $"Removed {account.User}.";
            Log.Write($"Sign-in removed for {account.User}");
        }
        else if (update.WebUser is not null || update.WebPassword is not null)
        {
            string user = (update.WebUser ?? "").Trim();
            if (UserProblem(user) is string problem) { JsonOut(res, new { ok = false, message = problem }, 400); return; }
            string password = update.WebPassword ?? "";
            if (password.Length > 200) { JsonOut(res, new { ok = false, message = "That password is too long." }, 400); return; }
            PageAccount? existing = _settings.Accounts.FirstOrDefault(a => a.User.Equals(user, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                if (password.Length == 0) { JsonOut(res, new { ok = false, message = "Type a password for this person." }, 400); return; }
                if (_settings.Accounts.Count >= 40) { JsonOut(res, new { ok = false, message = "Forty people can sign in, which is the limit." }, 400); return; }
                _settings.Accounts.Add(new PageAccount { User = user, Password = password });
                peopleMessage = $"Sign-in added for {user}.";
                Log.Write($"Sign-in added for {user}");
            }
            else
            {
                if (password.Length == 0) { JsonOut(res, new { ok = false, message = "That person already has a sign-in. Type a new password to change it." }, 400); return; }
                existing.Password = password;
                DropUser(existing.User);
                peopleMessage = $"Password changed for {existing.User}. They need to sign in again.";
                Log.Write($"Sign-in password changed for {existing.User}");
            }
        }
        bool retention = false, portsChanged = false, storageChanged = false;
        // Folders: each must be a place this program can write to.
        foreach (var (value, label) in new[] { (update.TraceFolder, "Packet captures"), (update.LogFolder, "SBC logs"), (update.BackupFolder, "Config backups"), (update.ProgramLogFolder, "Program log") })
            if (value is { Length: > 0 } && Storage.FolderProblem(value) is string problem)
            { JsonOut(res, new { ok = false, message = $"{label}: {problem}" }, 400); return; }
        if (_recorder.Recording && update.TraceFolder is not null && Settings.FullFolder(update.TraceFolder, "traces") != _settings.TraceFolderFull)
        { JsonOut(res, new { ok = false, message = "Stop the packet capture before moving the capture folder." }, 400); return; }
        if (update.TraceFolder is not null) { _settings.TraceFolder = update.TraceFolder.Trim().Length > 0 ? update.TraceFolder.Trim() : "traces"; storageChanged = true; }
        if (update.LogFolder is not null) { _settings.LogFolder = update.LogFolder.Trim().Length > 0 ? update.LogFolder.Trim() : "splunk"; storageChanged = true; }
        if (update.BackupFolder is not null) { _settings.BackupFolder = update.BackupFolder.Trim().Length > 0 ? update.BackupFolder.Trim() : "sbc-backups"; storageChanged = true; }
        if (update.ProgramLogFolder is not null) { _settings.ProgramLogFolder = update.ProgramLogFolder.Trim(); storageChanged = true; }
        if (update.LogMaxTotalMb is int lt) { _settings.LogMaxTotalMb = Math.Clamp(lt, 0, 10_000_000); storageChanged = true; }
        if (update.BackupRetentionDays is int bd) { _settings.BackupRetentionDays = Math.Clamp(bd, 0, 3650); storageChanged = true; }
        if (update.ProgramLogMaxMb is int pm) { _settings.ProgramLogMaxMb = Math.Clamp(pm, 1, 1000); storageChanged = true; }
        if (update.HistoryDays is int hd) { _settings.HistoryDays = Math.Clamp(hd, 0, 3650); storageChanged = true; }
        if (update.RetentionDays is int days)
        { _settings.RetentionDays = Math.Clamp(days, 0, 3650); retention = true; }
        if (update.RetentionMaxMb is int mb)
        { _settings.RetentionMaxMb = Math.Clamp(mb, 0, 1_000_000); retention = true; }
        if (update.SyslogPort is not null || update.DebugRecordingPort is not null || update.WebPort is not null)
        {
            int syslogPort = update.SyslogPort ?? _settings.SyslogPort;
            int drPort = update.DebugRecordingPort ?? _settings.DebugRecordingPort;
            int webPort = update.WebPort ?? _settings.WebPort;
            if (syslogPort is < 1 or > 65535 || drPort is < 1 or > 65535 || webPort is < 1 or > 65535)
            { JsonOut(res, new { ok = false, message = "Ports must be from 1 to 65535." }, 400); return; }
            if (syslogPort == drPort || syslogPort == webPort || drPort == webPort)
            { JsonOut(res, new { ok = false, message = "Syslog, Debug Recording and the web page need three different ports." }, 400); return; }
            _settings.SyslogPort = syslogPort;
            _settings.DebugRecordingPort = drPort;
            _settings.WebPort = webPort;
            portsChanged = true;
        }
        // Syslog: TCP / TLS ports, reordering, buffer, receive filters, write log.
        bool streamsChanged = false, syslogChanged = false;
        if (update.SyslogTcpPort is not null || update.SyslogTlsPort is not null || update.SyslogTlsPfx is not null || update.SyslogTlsPfxPassword is not null)
        {
            int tcp = update.SyslogTcpPort ?? _settings.SyslogTcpPort, tls = update.SyslogTlsPort ?? _settings.SyslogTlsPort;
            if (tcp is < 0 or > 65535 || tls is < 0 or > 65535)
            { JsonOut(res, new { ok = false, message = "Syslog TCP and TLS ports must be from 1 to 65535, or 0 for off." }, 400); return; }
            if (tcp > 0 && tcp == tls || tcp > 0 && tcp == _settings.WebPort || tls > 0 && tls == _settings.WebPort)
            { JsonOut(res, new { ok = false, message = "Syslog over TCP, syslog over TLS and the web page need different ports." }, 400); return; }
            string pfx = (update.SyslogTlsPfx ?? _settings.SyslogTlsPfx).Trim();
            if (pfx.Length > 0 && !File.Exists(Settings.FullFolder(pfx, pfx)))
            { JsonOut(res, new { ok = false, message = $"There is no certificate file {pfx} on this server. Leave it empty to use one MediantTrace makes." }, 400); return; }
            _settings.SyslogTcpPort = tcp;
            _settings.SyslogTlsPort = tls;
            _settings.SyslogTlsPfx = pfx;
            if (update.SyslogTlsPfxPassword is not null) _settings.SyslogTlsPfxPassword = update.SyslogTlsPfxPassword;
            streamsChanged = true;
        }
        if (update.IgnoreSyslogPatterns is not null)
        {
            foreach (string p in update.IgnoreSyslogPatterns.Where(p => p.Trim().Length > 0))
            {
                try { _ = new System.Text.RegularExpressions.Regex(p); }
                catch (ArgumentException e) { JsonOut(res, new { ok = false, message = $"Content filter '{p}' is not a valid regular expression: {e.Message}" }, 400); return; }
            }
            _settings.IgnoreSyslogPatterns = update.IgnoreSyslogPatterns.Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().Take(100).ToList();
            syslogChanged = true;
        }
        if (update.IgnoreSyslogFrom is not null)
        {
            var ips = update.IgnoreSyslogFrom.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
            if (ips.FirstOrDefault(x => !IPAddress.TryParse(x, out _)) is string bad)
            { JsonOut(res, new { ok = false, message = $"'{bad}' is not an IP address (IP filters)." }, 400); return; }
            _settings.IgnoreSyslogFrom = ips.Take(500).ToList();
            syslogChanged = true;
        }
        if (update.WriteLogFolder is { Length: > 0 } wlf && Storage.FolderProblem(wlf) is string wlProblem)
        { JsonOut(res, new { ok = false, message = $"Write log: {wlProblem}" }, 400); return; }
        if (update.WriteLogFile is not null && (update.WriteLogFile.Trim().Length == 0 || update.WriteLogFile.Trim() != Path.GetFileName(update.WriteLogFile.Trim()) || update.WriteLogFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        { JsonOut(res, new { ok = false, message = "Write log: the file name must be a plain name such as syslog.txt." }, 400); return; }
        if (update.SyslogReorder is bool reorder) { _settings.SyslogReorder = reorder; syslogChanged = true; }
        if (update.SyslogBufferLines is int buffer) { _settings.SyslogBufferLines = Math.Clamp(buffer, 1000, 1_000_000); syslogChanged = true; }
        if (update.WriteLog is bool wl) { _settings.WriteLog = wl; syslogChanged = true; }
        if (update.WriteLogFolder is not null) { _settings.WriteLogFolder = update.WriteLogFolder.Trim().Length > 0 ? update.WriteLogFolder.Trim() : "syslog"; syslogChanged = true; }
        if (update.WriteLogFile is not null) { _settings.WriteLogFile = update.WriteLogFile.Trim(); syslogChanged = true; }
        if (update.WriteLogFileMb is int wmb) { _settings.WriteLogFileMb = Math.Clamp(wmb, 0, 10_000); syslogChanged = true; }
        if (update.WriteLogFileCount is int wcount) { _settings.WriteLogFileCount = Math.Clamp(wcount, 0, 100_000); syslogChanged = true; }
        if (update.WriteLogPerDevice is bool wpd) { _settings.WriteLogPerDevice = wpd; syslogChanged = true; }
        if (update.WriteLogFolderPerDevice is bool wfd) { _settings.WriteLogFolderPerDevice = wfd; syslogChanged = true; }
        if (update.WriteLogUseNames is bool wun) { _settings.WriteLogUseNames = wun; syslogChanged = true; }

        if (webhook is not null) _settings.AlertWebhook = webhook;
        if (update.AlertEmailTo is not null) _settings.AlertEmailTo = to;
        if (update.AlertEmailFrom is not null) _settings.AlertEmailFrom = from;
        if (update.AlertSmtpHost is not null) _settings.AlertSmtpHost = host;
        if (update.AlertSmtpPort is int smtpPort) _settings.AlertSmtpPort = smtpPort;
        if (update.AlertSmtpUser is not null) _settings.AlertSmtpUser = update.AlertSmtpUser.Trim();
        if (update.AlertSmtpPassword is not null) _settings.AlertSmtpPassword = update.AlertSmtpPassword;
        _settings.Save();
        int removed = retention ? _recorder.ApplyRetention() : 0;
        if (storageChanged) _storage.Apply();
        Log.Write("Settings changed from the page");
        string? portMessage = null;
        if (portsChanged)
        {
            var problems = _recorder.Relisten();
            portMessage = problems.Count > 0 ? "Saved, but " + string.Join(" ", problems.Select(x => char.ToLowerInvariant(x[0]) + x[1..]))
                : $"Saved. Listening for syslog on UDP {_recorder.ListeningPort("SYSLOG")} and Debug Recording on UDP {_recorder.ListeningPort("DR")}."
                  + (_settings.WebPort != _boundWeb ? $" Restart MediantTrace to open the page on port {_settings.WebPort}." : "")
                  + " Update the firewall rule if Windows asks, and point the SBC at the new ports.";
            _firewall = (DateTime.MinValue, new { });   // re-check the rule against the new ports
        }
        if (syslogChanged)
        {
            var problems = _recorder.ApplySyslogOptions();
            if (problems.Count > 0) portMessage = "Saved, but " + string.Join(" ", problems);
            else if (update.WriteLog is not null || update.WriteLogFolder is not null || update.WriteLogFile is not null)
                portMessage = _settings.WriteLog ? $"Saved. Writing syslog to {_recorder.WriteLogFile}." : "Saved. Not writing the syslog text log.";
        }
        if (streamsChanged)
        {
            var problems = _recorder.ApplyStreamListeners();
            portMessage = problems.Count > 0 ? "Saved, but " + string.Join(" ", problems)
                : "Saved." + (_recorder.StreamPort("TCP") > 0 ? $" Syslog over TCP on {_recorder.StreamPort("TCP")}." : "")
                  + (_recorder.StreamPort("TLS") > 0 ? $" Syslog over TLS on {_recorder.StreamPort("TLS")}." : "")
                  + (_settings.SyslogTcpPort == 0 && _settings.SyslogTlsPort == 0 ? " Syslog over TCP and TLS is off." : " Allow these TCP ports in the firewall.");
        }
        string message = peopleMessage ?? portMessage ?? (removed > 0 ? $"Saved. Removed {removed} old capture{(removed == 1 ? "" : "s")}." : "Saved.");
        JsonOut(res, new { ok = true, message, removed });
    }

    private static (DateTime at, object value) _firewall = (DateTime.MinValue, new { });

    /// <summary>Whether Windows Firewall has our inbound rule. Checked once a minute (netsh is slow).</summary>
    private object FirewallState()
    {
        if (DateTime.UtcNow - _firewall.at < TimeSpan.FromSeconds(60)) return _firewall.value;
        bool? rule = null, portsOk = null;
        string rulePorts = "";
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", "advfirewall firewall show rule name=\"Mediant Trace (UDP)\" verbose")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true })!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            rule = p.ExitCode == 0 && output.Contains("Mediant Trace", StringComparison.OrdinalIgnoreCase);
            // The rule allows set ports only: after a port change it must be updated too.
            if (rule == true)
            {
                var ports = output.Split('\n').Where(l => l.TrimStart().StartsWith("LocalPort", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(l => l[(l.IndexOf(':') + 1)..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToHashSet();
                portsOk = ports.Contains("Any", StringComparer.OrdinalIgnoreCase)
                    || (ports.Contains(_settings.SyslogPort.ToString()) && ports.Contains(_settings.DebugRecordingPort.ToString()));
                rulePorts = string.Join(",", ports);
            }
        }
        catch { /* unknown */ }
        string exe = Environment.ProcessPath ?? "MediantTrace.exe";
        string remote = _settings.Sbcs.Count > 0 ? $" remoteip={string.Join(",", _settings.Sbcs.Select(x => x.Ip))}" : "";
        object value = new
        {
            rule,
            portsOk,
            rulePorts,
            command = (rule == true ? "netsh advfirewall firewall delete rule name=\"Mediant Trace (UDP)\" & " : "")
                + $"netsh advfirewall firewall add rule name=\"Mediant Trace (UDP)\" dir=in action=allow program=\"{exe}\" protocol=UDP localport={_settings.SyslogPort},{_settings.DebugRecordingPort}{remote}",
        };
        _firewall = (DateTime.UtcNow, value);
        return value;
    }

    private static List<string> LocalAddresses()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .Distinct().ToList();
        }
        catch { return []; }
    }

    private static void JsonOut(HttpListenerResponse res, object value, int status = 200)
    {
        res.StatusCode = status;
        Text(res, JsonSerializer.Serialize(value, Json), "application/json; charset=utf-8");
    }

    /// <summary>A font or its licence, from the resources built into the .exe. False when there is no such file.</summary>
    private static bool ServeFont(HttpListenerResponse res, string name)
    {
        using var stream = typeof(WebServer).Assembly.GetManifestResourceStream(name);
        if (stream is null) return false;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        res.Headers["Cache-Control"] = "public, max-age=604800";
        res.ContentType = name.EndsWith(".woff2", StringComparison.Ordinal) ? "font/woff2" : "text/plain; charset=utf-8";
        res.ContentLength64 = copy.Length;
        res.OutputStream.Write(copy.GetBuffer(), 0, (int)copy.Length);
        return true;
    }

    private static void Text(HttpListenerResponse res, string text, string type)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        res.ContentType = type;
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes);
    }

    public void Dispose()
    {
        _stop.Cancel();
        try { _listener.Stop(); _listener.Close(); } catch { }
    }
}
