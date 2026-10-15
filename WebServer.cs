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
    private readonly LabelBook _labels = new();
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string Url { get; private set; } = "";

    public WebServer(Settings settings, Recorder recorder, SipTester tester, ChangeManager changes, McpServer mcp, BulkTests bulk)
    {
        _bulk = bulk;
        _settings = settings;
        _recorder = recorder;
        _tester = tester;
        _changes = changes;
        _mcp = mcp;
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
            if (!Authorised(req))
            {
                res.StatusCode = 401;
                res.Headers["WWW-Authenticate"] = "Basic realm=\"MediantTrace\"";
                Text(res, "Password required", "text/plain");
                return;
            }

            string path = req.Url?.AbsolutePath ?? "/";
            string method = req.HttpMethod;
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
                case ("GET", "/"):
                    Text(res, Page.Html, "text/html; charset=utf-8");
                    break;
                case ("GET", "/api/status"):
                    JsonOut(res, Status());
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
                case ("GET", "/api/files"):
                    JsonOut(res, Files());
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
                            var names = ConfigNames.Parse(File.ReadAllText(saved));
                            string ip = _settings.Sbcs.FirstOrDefault(s => s.Ip == b?.Id || s.Name == b?.Id)?.Ip ?? b?.Id ?? "";
                            _labels.Replace(ip, names, DateTime.UtcNow);
                            JsonOut(res, new { ok = true, message = $"Read {names.Count} names. Configuration saved as {Path.GetFileName(saved)}.", names });
                        }
                        catch (Exception e) { JsonOut(res, new { ok = false, message = e.Message }, 400); }
                    }
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
                    CallRoute(path, res);
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

    private bool Authorised(HttpListenerRequest req)
    {
        if (!_settings.WebOnNetwork || _settings.WebPassword.Length == 0) return true;
        if (req.IsLocal) return true;
        string? header = req.Headers["Authorization"];
        if (header is null || !header.StartsWith("Basic ", StringComparison.Ordinal)) return false;
        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header[6..]));
            string password = decoded[(decoded.IndexOf(':') + 1)..];
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(_settings.WebPassword));
        }
        catch { return false; }
    }

    private object Status()
    {
        var (calls, streams, syslog) = _recorder.Store.Counts();
        return new
        {
            recording = _recorder.Recording,
            startedAt = _recorder.StartedAt,
            stopsAt = _recorder.StopsAt,
            now = DateTime.UtcNow,
            pcap = _recorder.PcapPath is null ? null : Path.GetFileName(_recorder.PcapPath),
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
                autoStopMinutes = _settings.AutoStopMinutes,
                traceFolder = _settings.TraceFolderFull,
                webOnNetwork = _settings.WebOnNetwork,
                passwordSet = _settings.WebPassword.Length > 0,
            },
            serverAddresses = LocalAddresses(),
            firewall = FirewallState(),
            mcp = new { token = _settings.McpToken, url = $"http://{(_settings.WebOnNetwork ? LocalAddresses().FirstOrDefault() ?? "localhost" : "localhost")}:{_settings.WebPort}/mcp" },
            changesPending = _changes.Plans().Count(p => p.Status == "pending"),
            bulkRunning = _bulk.Runs().Any(r => r.State == "running"),
            tester = new { port = _settings.TestSipPort, rtpFirst = _settings.TestRtpPortFirst, rtpCount = _settings.TestRtpPortCount, error = _tester.ListenError, running = _tester.Live?.Name, echoActive = _tester.EchoActive, echoAnswered = _tester.EchoAnswered },
            version = typeof(WebServer).Assembly.GetName().Version?.ToString(3),
            labels = _labels.All(),
        };
    }

    /// <summary>Each configured SBC with what is arriving from it, then any unlisted sender.</summary>
    private List<object> SbcStatus()
    {
        var sources = _recorder.Sources();
        var list = new List<object>();
        foreach (SbcEntry sbc in _settings.Sbcs.ToList())
        {
            var mine = sources.Where(x => x.Address == sbc.Ip).ToList();
            list.Add(new
            {
                name = sbc.Name, ip = sbc.Ip, url = sbc.Url, listed = true,
                sshHost = sbc.SshHost, sshPort = sbc.SshPort, sshUser = sbc.SshUser, hasPassword = sbc.SshPasswordProtected.Length > 0, hasEnable = sbc.EnablePasswordProtected.Length > 0,
                syslog = mine.FirstOrDefault(x => x.Kind == "SYSLOG") is { } sy ? new { packets = sy.Packets, lastSeen = sy.LastSeen } : null,
                dr = mine.FirstOrDefault(x => x.Kind == "DR") is { } d ? new { packets = d.Packets, lastSeen = d.LastSeen } : null,
            });
        }
        foreach (var group in sources.Where(x => _settings.FindSbc(x.Address) is null).GroupBy(x => x.Address))
        {
            list.Add(new
            {
                name = "", ip = group.Key, url = "", listed = false,
                syslog = group.FirstOrDefault(x => x.Kind == "SYSLOG") is { } sy ? new { packets = sy.Packets, lastSeen = sy.LastSeen } : null,
                dr = group.FirstOrDefault(x => x.Kind == "DR") is { } d ? new { packets = d.Packets, lastSeen = d.LastSeen } : null,
            });
        }
        return list;
    }

    private void CallRoute(string path, HttpListenerResponse res)
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
        object? detail = _recorder.Store.CallDetail(id, DateTime.UtcNow);
        if (detail is null) { res.StatusCode = 404; return; }
        JsonOut(res, detail);
    }

    private object Files()
    {
        string folder = _settings.TraceFolderFull;
        if (!Directory.Exists(folder)) return Array.Empty<object>();
        return new DirectoryInfo(folder).GetFiles("mediant-*.*")
            .Where(f => f.Extension is ".pcap" or ".txt")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new { name = f.Name, size = f.Length, modified = f.LastWriteTimeUtc, active = f.FullName == _recorder.PcapPath || f.FullName == _recorder.TextPath })
            .ToList();
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

    private sealed class IdBody { public string? Id { get; set; } }
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
    }

    private void SaveSettings(HttpListenerRequest req, HttpListenerResponse res)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var update = JsonSerializer.Deserialize<SettingsUpdate>(reader.ReadToEnd(), Json);
        if (update is null) { JsonOut(res, new { ok = false, message = "Nothing to save" }, 400); return; }

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
        _settings.Save();
        Log.Write("Settings changed from the page");
        JsonOut(res, new { ok = true, message = "Saved." });
    }

    private static (DateTime at, object value) _firewall = (DateTime.MinValue, new { });

    /// <summary>Whether Windows Firewall has our inbound rule. Checked once a minute (netsh is slow).</summary>
    private object FirewallState()
    {
        if (DateTime.UtcNow - _firewall.at < TimeSpan.FromSeconds(60)) return _firewall.value;
        bool? rule = null;
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", "advfirewall firewall show rule name=\"Mediant Trace (UDP)\"")
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true })!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            rule = p.ExitCode == 0 && output.Contains("Mediant Trace", StringComparison.OrdinalIgnoreCase);
        }
        catch { /* unknown */ }
        string exe = Environment.ProcessPath ?? "MediantTrace.exe";
        object value = new
        {
            rule,
            command = $"netsh advfirewall firewall add rule name=\"Mediant Trace (UDP)\" dir=in action=allow program=\"{exe}\" protocol=UDP localport={_settings.SyslogPort},{_settings.DebugRecordingPort}",
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
