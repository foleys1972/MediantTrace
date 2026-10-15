using System.Text.Json;
using System.Text.Json.Nodes;

namespace MediantTrace;

/// <summary>
/// Model Context Protocol server (Streamable HTTP, JSON responses) at /mcp.
/// Gives an AI client the same powers as the page: capture, triage calls,
/// read SBC logs, run test calls, read SBC configuration, and propose changes
/// that a person approves before they are applied.
/// </summary>
public sealed class McpServer
{
    private const string ProtocolVersion = "2025-06-18";
    private readonly Settings _settings;
    private readonly Recorder _recorder;
    private readonly SipTester _tester;
    private readonly ChangeManager _changes;
    private readonly BulkTests _bulk;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };

    public McpServer(Settings settings, Recorder recorder, SipTester tester, ChangeManager changes, BulkTests bulk)
    {
        _settings = settings; _recorder = recorder; _tester = tester; _changes = changes; _bulk = bulk;
    }

    private const string Instructions = """
        MediantTrace watches AudioCodes SBCs (Mediant) and troubleshoots their calls.
        Typical flow for a call problem: capture_status (start_capture if nothing is recording), reproduce or wait for the call,
        list_calls (filter "issues" or "oneway"), then triage_call on the call: it returns the verdict, every check, the SIP flow,
        SDP, audio per direction, the SBC's own call record and the SBC log lines for the call.
        To prove audio end to end use run_test (a saved test) or create_test. Read configuration with sbc_show (show commands only).
        Configuration changes: sbc_plan_change returns a plan; a PERSON must approve it on MediantTrace's Changes page or tell you
        the one-time code shown there, then call sbc_apply_change with that code. Never guess or invent codes. Changes are live only;
        saving to flash is a separate plan (sbc_plan_save). Explain each change and its effect to the user before asking for approval.
        """;

    // ------------------------------------------------------------ JSON-RPC

    /// <summary>Handles one POST body. Returns the response JSON, or null for notifications (HTTP 202).</summary>
    public string? Handle(string body)
    {
        JsonNode? msg;
        try { msg = JsonNode.Parse(body); }
        catch (JsonException) { return Error(null, -32700, "Parse error"); }
        if (msg is JsonArray batch)
        {
            var results = new JsonArray();
            foreach (var m in batch) { string? r = HandleOne(m); if (r is not null) results.Add(JsonNode.Parse(r)); }
            return results.Count == 0 ? null : results.ToJsonString();
        }
        return HandleOne(msg);
    }

    private string? HandleOne(JsonNode? msg)
    {
        if (msg is not JsonObject o) return Error(null, -32600, "Invalid request");
        JsonNode? id = o["id"]?.DeepClone();
        string method = o["method"]?.GetValue<string>() ?? "";
        if (id is null) return null; // notification (e.g. notifications/initialized)
        try
        {
            JsonNode result = method switch
            {
                "initialize" => new JsonObject
                {
                    ["protocolVersion"] = o["params"]?["protocolVersion"]?.GetValue<string>() is string v && v.Length > 0 ? v : ProtocolVersion,
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject { ["listChanged"] = false } },
                    ["serverInfo"] = new JsonObject { ["name"] = "mediantrace", ["title"] = "MediantTrace", ["version"] = typeof(McpServer).Assembly.GetName().Version?.ToString(3) },
                    ["instructions"] = Instructions,
                },
                "ping" => new JsonObject(),
                "tools/list" => new JsonObject { ["tools"] = Tools() },
                "tools/call" => Call(o["params"]?["name"]?.GetValue<string>() ?? "", o["params"]?["arguments"] as JsonObject ?? new JsonObject()),
                _ => throw new McpError(-32601, $"Method not found: {method}"),
            };
            return new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result }.ToJsonString();
        }
        catch (McpError e) { return Error(id, e.Code, e.Message); }
    }

    private static string Error(JsonNode? id, int code, string message) =>
        new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = message } }.ToJsonString();

    private sealed class McpError(int code, string message) : Exception(message) { public int Code { get; } = code; }

    // ------------------------------------------------------------ tools

    private static JsonObject Tool(string name, string description, string schema, bool readOnly = true, bool destructive = false) => new()
    {
        ["name"] = name,
        ["description"] = description,
        ["inputSchema"] = JsonNode.Parse(schema),
        ["annotations"] = new JsonObject { ["readOnlyHint"] = readOnly, ["destructiveHint"] = destructive, ["openWorldHint"] = !readOnly },
    };

    private const string NoArgs = """{"type":"object","properties":{}}""";

    private static JsonArray Tools() =>
    [
        Tool("list_sbcs", "SBCs MediantTrace knows: name, address, whether syslog and Debug Recording are arriving, and whether an SSH login is set.", NoArgs),
        Tool("capture_status", "Whether a capture is running, since when, packet and call counts, and where it is being saved.", NoArgs),
        Tool("start_capture", "Start recording what the SBCs send (calls, SIP, audio statistics, syslog) to a new .pcap and .txt. Stops itself after the configured time.", NoArgs, readOnly: false),
        Tool("stop_capture", "Stop the running capture; its files stay under Captures.", NoArgs, readOnly: false),
        Tool("list_calls", "Calls in the current capture, newest first, with state, SIP result, MOS and the main issue found.",
            """{"type":"object","properties":{"filter":{"type":"string","enum":["all","issues","oneway","failed","connected"],"description":"Which calls (default all)"},"sbc":{"type":"string","description":"SBC name or IP (default: all)"},"search":{"type":"string","description":"Number, Call-ID or SID to look for"},"limit":{"type":"integer","minimum":1,"maximum":200,"description":"Default 30"}}}"""),
        Tool("triage_call", "Everything about one call to find what is wrong: verdict, every check (one-way audio, NAT, codecs, loss, jitter, SBC end reason), SIP flow, SDP offer/answer, audio per direction, the SBC's own call record and its log lines for the call's SID.",
            """{"type":"object","properties":{"call_id":{"type":"integer","description":"The id from list_calls"}},"required":["call_id"]}"""),
        Tool("get_sip_messages", "The full text of a call's SIP messages (optionally one message by index from triage_call).",
            """{"type":"object","properties":{"call_id":{"type":"integer"},"index":{"type":"integer","description":"Only this message"}},"required":["call_id"]}"""),
        Tool("search_sbc_log", "Search the SBCs' syslog in the current capture (newest first). Use a SID, Call-ID, number or word.",
            """{"type":"object","properties":{"query":{"type":"string"},"sbc":{"type":"string"},"warnings_only":{"type":"boolean","description":"Only warnings and errors"},"limit":{"type":"integer","minimum":1,"maximum":1000,"description":"Default 100"}}}"""),
        Tool("list_tests", "Saved call tests (number, SBC, schedule) with their latest result, plus recent calls to the test line.", NoArgs),
        Tool("create_test", "Save a call test: MediantTrace calls a number through an SBC, plays a 1004 Hz tone and scores the audio coming back. An echo number tests both directions.",
            """{"type":"object","properties":{"name":{"type":"string"},"sbc_ip":{"type":"string","description":"The SBC's SIP address"},"sbc_port":{"type":"integer","description":"Default 5060"},"to":{"type":"string","description":"Number to call"},"from":{"type":"string","description":"Calling number (default mediantrace)"},"codec":{"type":"string","enum":["PCMA","PCMU"]},"duration_seconds":{"type":"integer","minimum":3,"maximum":300},"repeat_minutes":{"type":"integer","minimum":0,"description":"0 = only when run"}},"required":["name","sbc_ip","to"]}""", readOnly: false),
        Tool("run_test", "Place a saved test call now and (by default) wait for the result: SIP outcome, answer time, audio both ways, loss, jitter, MOS, findings.",
            """{"type":"object","properties":{"test_id":{"type":"string"},"wait":{"type":"boolean","description":"Wait for the result (default true)"}},"required":["test_id"]}""", readOnly: false),
        Tool("get_test_result", "One test or test-line result in full, including its SIP messages.",
            """{"type":"object","properties":{"result_id":{"type":"string"}},"required":["result_id"]}"""),
        Tool("bulk_line_test", "Test a list of dealerboard lines one after another. ARD and DDI lines are called and pass when a person picks up (the call holds a few seconds with a test tone, then hangs up). MRD and Hoot lines are always-on wires and are NOT called by default (a call would replace the live wire call): the SBC active call list shows whether the wire is up, and the capture measures its audio. Set test=call on a line to dial it anyway. Returns the run id; follow with bulk_status.",
            """{"type":"object","properties":{"name":{"type":"string"},"via":{"type":"string","description":"SBC address (ip or ip:port) to send every call through; empty = each URI's host"},"from":{"type":"string"},"hold_seconds":{"type":"integer","minimum":1,"maximum":120},"lines":{"type":"array","minItems":1,"maxItems":500,"items":{"type":"object","properties":{"name":{"type":"string"},"uri":{"type":"string","description":"sip:number@host[:port], or a number when 'via' is given"},"type":{"type":"string","enum":["ARD","MRD","HOOT","DDI"]},"test":{"type":"string","enum":["wire","call"],"description":"MRD / Hoot only: wire (default) checks the live wire call, call dials the line"},"hold_seconds":{"type":"integer"},"ring_seconds":{"type":"integer"}},"required":["uri","type"]}}},"required":["lines"]}""", readOnly: false),
        Tool("bulk_status", "Bulk line test runs (newest first): each line's status, SIP result, answer time, audio and the main finding.",
            """{"type":"object","properties":{"limit":{"type":"integer","minimum":1,"maximum":10}}}"""),
        Tool("sbc_show", "Run read-only CLI commands on an SBC over SSH (show ..., ping) and return their output. Example: [\"show running-config voip\", \"show voip calls statistics gw\"].",
            """{"type":"object","properties":{"sbc":{"type":"string","description":"SBC name or IP"},"commands":{"type":"array","items":{"type":"string"},"minItems":1,"maxItems":20}},"required":["sbc","commands"]}"""),
        Tool("sbc_backup", "Save the SBC's running configuration on the MediantTrace server and return the file name.",
            """{"type":"object","properties":{"sbc":{"type":"string"}},"required":["sbc"]}""", readOnly: false),
        Tool("sbc_plan_change", "Propose a configuration change (AudioCodes CLI lines, written like the running-config: configure voip / table row / settings / activate / exit). Nothing is changed yet: a person must approve the plan in MediantTrace (Changes page), which gives them a one-time code. Returns the plan id.",
            """{"type":"object","properties":{"sbc":{"type":"string"},"commands":{"type":"array","items":{"type":"string"},"minItems":1,"maxItems":200},"reason":{"type":"string","description":"What the change fixes and what it affects, in plain words"}},"required":["sbc","commands","reason"]}""", readOnly: false),
        Tool("sbc_plan_save", "Propose saving the SBC's running configuration to flash ('write'), so live changes survive a reboot. Needs a person's approval like any change.",
            """{"type":"object","properties":{"sbc":{"type":"string"},"reason":{"type":"string"}},"required":["sbc","reason"]}""", readOnly: false),
        Tool("sbc_apply_change", "Apply an approved plan: backup first, run the commands live, report the output. Needs the one-time code a person read from the Changes page; never guess it.",
            """{"type":"object","properties":{"plan_id":{"type":"string"},"approval_code":{"type":"string"}},"required":["plan_id","approval_code"]}""", readOnly: false, destructive: true),
        Tool("list_changes", "Change plans and their status (pending, applied, failed, rejected, expired), with output and backup file names.",
            """{"type":"object","properties":{"limit":{"type":"integer","minimum":1,"maximum":100}}}"""),
    ];

    private JsonNode Call(string name, JsonObject a)
    {
        try
        {
            object result = name switch
            {
                "list_sbcs" => _settings.Sbcs.Select(s => new
                {
                    s.Name, s.Ip, webPage = s.Url, sshLogin = s.HasLogin, sshUser = s.SshUser,
                    feeds = _recorder.Sources().Where(x => x.Address == s.Ip).Select(x => new { kind = x.Kind == "DR" ? "debug recording" : "syslog", x.Packets, lastSeen = x.LastSeen }),
                }).ToList(),
                "capture_status" => new
                {
                    recording = _recorder.Recording, startedAt = _recorder.StartedAt, stopsAt = _recorder.StopsAt, pcap = _recorder.PcapPath, text = _recorder.TextPath,
                    sipMessages = _recorder.SipMessages, rtpPackets = _recorder.RtpPackets, syslogPackets = _recorder.SyslogPackets, calls = _recorder.Store.Counts().calls,
                },
                "start_capture" => Wrap(_recorder.Start()),
                "stop_capture" => Wrap(_recorder.Stop("stopped by an MCP client")),
                "list_calls" => ListCalls(a),
                "triage_call" => Triage(Int(a, "call_id")),
                "get_sip_messages" => SipMessages(Int(a, "call_id"), a["index"]?.GetValue<int>()),
                "search_sbc_log" => _recorder.Store.SyslogLines(Str(a, "query", ""), Math.Clamp(a["limit"]?.GetValue<int>() ?? 100, 1, 1000), SbcIp(Str(a, "sbc", "")), a["warnings_only"]?.GetValue<bool>() ?? false),
                "list_tests" => new { tests = _tester.Tests(), testLine = _tester.Results("inbound", 10) },
                "create_test" => CreateTest(a),
                "run_test" => RunTest(Str(a, "test_id"), a["wait"]?.GetValue<bool>() ?? true),
                "get_test_result" => (object?)_tester.Result(Str(a, "result_id")) ?? throw new InvalidOperationException("No such result."),
                "bulk_line_test" => BulkStart(a),
                "bulk_status" => _bulk.Runs().Take(Math.Clamp(a["limit"]?.GetValue<int>() ?? 1, 1, 10)).ToList(),
                "sbc_show" => _changes.Show(Str(a, "sbc"), Strings(a, "commands")).Select(x => new { x.command, x.output }).ToList(),
                "sbc_backup" => new { saved = Path.GetFileName(_changes.Backup(Str(a, "sbc"))), folder = ChangeManager.BackupDir },
                "sbc_plan_change" => PlanResult(_changes.Plan(Str(a, "sbc"), Strings(a, "commands"), Str(a, "reason"), "MCP client")),
                "sbc_plan_save" => PlanResult(_changes.Plan(Str(a, "sbc"), ["write"], Str(a, "reason"), "MCP client", "save")),
                "sbc_apply_change" => ChangeManager.PublicView(_changes.Apply(Str(a, "plan_id"), Str(a, "approval_code"), "MCP client with code")),
                "list_changes" => _changes.Plans().Take(Math.Clamp(a["limit"]?.GetValue<int>() ?? 20, 1, 100)).Select(ChangeManager.PublicView).ToList(),
                _ => throw new McpError(-32602, $"Unknown tool: {name}"),
            };
            return new JsonObject { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = JsonSerializer.Serialize(result, Json) }) };
        }
        catch (McpError) { throw; }
        catch (Exception e)
        {
            // Tool errors go back as results the model can read and act on (MCP convention).
            return new JsonObject { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = e.Message }), ["isError"] = true };
        }
    }

    private static object Wrap((bool ok, string message) r) => new { r.ok, r.message };

    private object PlanResult(ChangeManager.ChangePlan p) => new
    {
        planId = p.Id, sbc = p.SbcName, kind = p.Kind, commands = p.Commands, p.Reason, expires = p.Expires,
        nextStep = $"Nothing has changed yet. Ask the user to review plan {p.Id} in MediantTrace (Changes page) and either approve it there, or read you the 6-digit approval code shown there; then call sbc_apply_change with it. The plan expires in 30 minutes.",
    };

    private object ListCalls(JsonObject a)
    {
        string filter = Str(a, "filter", "all"), search = Str(a, "search", "").ToLowerInvariant();
        int limit = Math.Clamp(a["limit"]?.GetValue<int>() ?? 30, 1, 200);
        var list = JsonSerializer.SerializeToNode(_recorder.Store.CallList(DateTime.UtcNow, SbcIp(Str(a, "sbc", ""))), Json)!.AsArray();
        var picked = list.Where(c =>
        {
            int warnings = c!["warnings"]!.GetValue<int>();
            string state = c["state"]!.GetValue<string>();
            bool ok = filter switch
            {
                "issues" => warnings > 0,
                "oneway" => c["oneWay"]!.GetValue<bool>(),
                "failed" => state == "failed",
                "connected" => state is "connected" or "ended",
                _ => true,
            };
            if (ok && search.Length > 0)
                ok = new[] { "from", "to", "callId", "session" }.Any(k => (c[k]?.GetValue<string>() ?? "").ToLowerInvariant().Contains(search));
            return ok;
        }).Take(limit).Select(c => new
        {
            id = c!["id"]!.GetValue<int>(), sbc = SbcName(c["sbc"]!.GetValue<string>()), from = c["from"]!.GetValue<string>(), to = c["to"]!.GetValue<string>(),
            started = c["started"]!.GetValue<DateTime>(), state = c["state"]!.GetValue<string>(), sipStatus = c["status"]!.GetValue<int>(),
            talkSeconds = c["seconds"]!.GetValue<double>(), mos = c["mos"]?.GetValue<double?>(), severity = c["severity"]!.GetValue<string>(),
            issue = c["topIssue"]!.GetValue<string>(), session = c["session"]!.GetValue<string>(),
        }).ToList();
        return new { total = list.Count, shown = picked.Count, calls = picked };
    }

    private object Triage(int id)
    {
        object detail = _recorder.Store.CallDetail(id, DateTime.UtcNow) ?? throw new InvalidOperationException($"No call {id} in the current capture.");
        var d = JsonSerializer.SerializeToNode(detail, Json)!.AsObject();
        string session = d["session"]?.GetValue<string>() ?? "", sbc = d["sbc"]?.GetValue<string>() ?? "";
        var messages = d["messages"]!.AsArray().Select(m => new
        {
            index = m!["index"]!.GetValue<int>(), time = m["time"]!.GetValue<DateTime>(),
            direction = m["inbound"]?.GetValue<bool?>() switch { true => "into SBC", false => "out of SBC", _ => "?" },
            src = m["src"]!.GetValue<string>(), dst = m["dst"]!.GetValue<string>(), first = m["first"]!.GetValue<string>(), hasSdp = m["hasSdp"]!.GetValue<bool>(),
        }).ToList();
        var streams = d["streams"]!.AsArray().Select(s => new
        {
            from = s!["src"]!.GetValue<string>(), to = s["dst"]!.GetValue<string>(), packets = s["packets"]!.GetValue<long>(), lost = s["lost"]!.GetValue<long>(),
            lossPercent = s["lossPercent"]!.GetValue<double>(), jitterMs = s["jitterMs"]?.GetValue<double?>(), longestGapMs = s["maxGapMs"]!.GetValue<double>(), mos = s["mos"]?.GetValue<double?>(),
        }).ToList();
        string[] cdrKeys = ["TrmReason", "TrmReasonCategory", "SIPTrmReason", "Durat", "Coder", "InPackets", "OutPackets", "PackLoss", "RemotePackLoss", "RTPjitter", "RTPdelay",
                            "RtpIp", "Port", "LocalRtpIp", "LocalRtpPort", "IPG", "IPGroup", "SIPInterfaceId", "ProxySetId", "IpProfileId", "MediaRealmId", "SrcPhoneNum", "DstPhoneNum", "Caller", "Callee"];
        var cdrs = d["cdrs"]!.AsArray().Select(r => new
        {
            kind = r!["kind"]!.GetValue<string>(),
            fields = r["fields"]!.AsArray().Where(f => cdrKeys.Contains(f!["k"]!.GetValue<string>()) && f["v"]!.GetValue<string>() is { Length: > 0 } v && v != "-1")
                .ToDictionary(f => f!["k"]!.GetValue<string>(), f => f!["v"]!.GetValue<string>()),
        }).ToList();
        object? sbcLog = null;
        if (session.Length > 0)
        {
            var all = JsonSerializer.SerializeToNode(_recorder.Store.SyslogLines("SID=" + session, 3000, sbc), Json)!.AsArray();
            var warn = JsonSerializer.SerializeToNode(_recorder.Store.SyslogLines("SID=" + session, 50, sbc, errorsOnly: true), Json)!.AsArray();
            sbcLog = new { lines = all.Count, warningsAndErrors = warn.Select(l => l!["text"]!.GetValue<string>()).Distinct().Take(25).ToList(), hint = "search_sbc_log with query SID=" + session + " for every line" };
        }
        return new
        {
            id, sbc = SbcName(sbc), callId = d["callId"]?.GetValue<string>(), session, from = d["from"]?.GetValue<string>(), to = d["to"]?.GetValue<string>(),
            state = d["state"]?.GetValue<string>(), sipStatus = d["status"]?.GetValue<int>(), started = d["started"]?.GetValue<DateTime>(),
            answered = d["answered"]?.GetValue<DateTime?>(), ended = d["ended"]?.GetValue<DateTime?>(), talkSeconds = d["seconds"]?.GetValue<double>(), mos = d["mos"]?.GetValue<double?>(),
            checks = d["checks"], otherLegs = d["otherLegs"], sip = messages, sdp = d["sdps"], audioByFarEnd = d["media"], audioStreams = streams, sbcCallRecords = cdrs, sbcLog,
        };
    }

    private object SipMessages(int id, int? index)
    {
        object detail = _recorder.Store.CallDetail(id, DateTime.UtcNow) ?? throw new InvalidOperationException($"No call {id}.");
        var msgs = JsonSerializer.SerializeToNode(detail, Json)!["messages"]!.AsArray();
        return msgs.Where(m => index is null || m!["index"]!.GetValue<int>() == index)
            .Select(m => new { index = m!["index"]!.GetValue<int>(), time = m["time"]!.GetValue<DateTime>(), src = m["src"]!.GetValue<string>(), dst = m["dst"]!.GetValue<string>(), text = m["text"]!.GetValue<string>() }).ToList();
    }

    private object BulkStart(JsonObject a)
    {
        var lines = (a["lines"] as JsonArray ?? throw new McpError(-32602, "Missing argument: lines")).Select(x => new BulkTests.LineInput
        {
            Name = x?["name"]?.GetValue<string>(), Uri = x?["uri"]?.GetValue<string>(), Type = x?["type"]?.GetValue<string>(),
            HoldSeconds = x?["hold_seconds"]?.GetValue<int>(), RingSeconds = x?["ring_seconds"]?.GetValue<int>(), Test = x?["test"]?.GetValue<string>(),
        }).ToList();
        var (run, errors) = _bulk.Start(new BulkTests.StartInput
        {
            Name = a["name"]?.GetValue<string>(), Via = a["via"]?.GetValue<string>(), From = a["from"]?.GetValue<string>(),
            HoldSeconds = a["hold_seconds"]?.GetValue<int>(), Lines = lines,
        });
        if (run is null) throw new InvalidOperationException(string.Join(" ", errors));
        return new { runId = run.Id, lines = run.Lines.Count, message = "Testing the lines one after another; use bulk_status to follow." };
    }

    private object CreateTest(JsonObject a)
    {
        var (ok, message, id) = _tester.Save(new SipTester.TestInput
        {
            Name = Str(a, "name"), SbcIp = Str(a, "sbc_ip"), SbcPort = a["sbc_port"]?.GetValue<int>() ?? 5060, To = Str(a, "to"), From = Str(a, "from", ""),
            Codec = Str(a, "codec", "PCMA"), DurationSeconds = a["duration_seconds"]?.GetValue<int>() ?? 15, RepeatMinutes = a["repeat_minutes"]?.GetValue<int>() ?? 0,
        });
        if (!ok) throw new InvalidOperationException(message);
        return new { testId = id, message };
    }

    private object RunTest(string testId, bool wait)
    {
        var (ok, message, resultId) = _tester.Run(testId);
        if (!ok || resultId is null) throw new InvalidOperationException(message);
        if (!wait) return new { resultId, message = "Started; use get_test_result." };
        DateTime until = DateTime.UtcNow.AddMinutes(6);
        while (DateTime.UtcNow < until)
        {
            Thread.Sleep(1000);
            var r = _tester.Result(resultId);
            if (r is not null && r.Ended is not null && r.Outcome != "running") return Summary(r);
        }
        return new { resultId, message = "Still running; use get_test_result." };
    }

    private static object Summary(TestResult r) => new
    {
        r.Id, r.Name, r.Target, r.Outcome, sip = $"{r.FinalStatus} {r.FinalReason}".Trim(), answerMs = r.AnswerMs, talkSeconds = r.TalkSeconds,
        sentPackets = r.TxPackets, receivedPackets = r.RxPackets, lossPercent = r.RxLossPercent, jitterMs = r.RxJitterMs, longestGapMs = r.RxMaxGapMs,
        mos = r.MosRx, toneCameBack = r.ToneReturned, levelDbov = r.RxLevelDb, r.Findings, r.CallId, farEndMedia = r.RemoteMedia, audioCameFrom = r.AudioFrom,
    };

    // ------------------------------------------------------------ argument helpers

    private string? SbcIp(string nameOrIp)
    {
        if (nameOrIp.Length == 0) return null;
        var e = _settings.Sbcs.FirstOrDefault(s => s.Ip == nameOrIp || s.Name.Equals(nameOrIp, StringComparison.OrdinalIgnoreCase));
        return e?.Ip ?? nameOrIp;
    }

    private string SbcName(string ip) => _settings.Sbcs.FirstOrDefault(s => s.Ip == ip)?.Name is { Length: > 0 } n ? $"{n} ({ip})" : ip;

    private static string Str(JsonObject a, string key, string? fallback = null)
    {
        if (a[key] is JsonValue v && v.TryGetValue(out string? s)) return s;
        return fallback ?? throw new McpError(-32602, $"Missing argument: {key}");
    }

    private static int Int(JsonObject a, string key)
    {
        if (a[key] is JsonValue v && (v.TryGetValue(out int i) || (v.TryGetValue(out string? s) && int.TryParse(s, out i)))) return i;
        throw new McpError(-32602, $"Missing or invalid argument: {key}");
    }

    private static List<string> Strings(JsonObject a, string key) =>
        (a[key] as JsonArray)?.Select(x => x?.GetValue<string>() ?? "").ToList() ?? throw new McpError(-32602, $"Missing argument: {key}");
}
