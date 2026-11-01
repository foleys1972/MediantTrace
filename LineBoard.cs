using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// The line board: every dealerboard line we know about, with its live state.
/// Always-on lines (MRD, Hoot) are up when the SBC lists a call for their number;
/// their audio rate each way comes from Debug Recording. ARD and DDI lines show
/// a call in progress, otherwise their last line test result.
/// The SBC's call list is read in the background, at most every 30 s and only
/// while someone has the board open, so viewing the board never waits on SSH.
/// </summary>
public sealed partial class LineBoard
{
    private readonly Settings _settings;
    private readonly Recorder _recorder;
    private readonly BulkTests _bulk;
    private readonly object _gate = new();
    private List<LineEntry> _lines = [];
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "lines.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    // Per SBC: last call list read, when, and why it failed.
    private readonly Dictionary<string, SbcRead> _reads = new();
    // Per Mediant session: packet totals at the last look, for a rate each way.
    private readonly Dictionary<string, (DateTime at, long toSbc, long fromSbc, double toRate, double fromRate)> _rates = new();

    private sealed class SbcRead
    {
        public DateTime At;
        public List<BulkTests.SbcCall>? Calls;
        public string Error = "";
        public bool Busy;
    }

    public sealed record LastTestView(string Status, DateTime At, string Summary);

    public sealed record LineView(string Id, string Name, string Uri, string Number, string Type, string Group, string Via, string Sbc,
        string State, string Detail, string? Since, double? ToSbcPerSec, double? FromSbcPerSec, double? Mos, LastTestView? LastTest, List<string> Sessions);

    public sealed class LineEntry
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Uri { get; set; } = "";
        public string Number { get; set; } = "";
        public string Type { get; set; } = "";   // ARD, MRD, HOOT, DDI
        public string Group { get; set; } = "";  // desk, counterparty or site; empty = by type
        public string Via { get; set; } = "";    // SBC ip[:port] calls go through; empty = the URI's host
    }

    public sealed class ImportInput
    {
        public string? Via { get; set; }
        public bool Replace { get; set; }
        public List<ImportLine>? Lines { get; set; }
    }

    public sealed class ImportLine
    {
        public string? Name { get; set; }
        public string? Uri { get; set; }
        public string? Type { get; set; }
        public string? Group { get; set; }
    }

    public LineBoard(Settings settings, Recorder recorder, BulkTests bulk)
    {
        _settings = settings;
        _recorder = recorder;
        _bulk = bulk;
        try { if (File.Exists(FilePath)) _lines = JsonSerializer.Deserialize<List<LineEntry>>(File.ReadAllText(FilePath)) ?? []; }
        catch (Exception e) { Log.Write($"lines.json could not be read: {e.Message}"); }
    }

    [GeneratedRegex(@"^(?:sips?:)?(?<user>[A-Za-z0-9+*#._-]{1,64})(?:@(?<host>[^:;>\s]+)(?::(?<port>\d{1,5}))?)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UriPattern();

    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ARD"] = "ARD", ["AUTO RINGDOWN"] = "ARD", ["AUTOMATIC RINGDOWN"] = "ARD",
        ["MRD"] = "MRD", ["MANUAL RINGDOWN"] = "MRD",
        ["HOOT"] = "HOOT", ["HOOT N HOLLER"] = "HOOT", ["HOOTNHOLLER"] = "HOOT", ["HOOT & HOLLER"] = "HOOT",
        ["DDI"] = "DDI", ["DID"] = "DDI", ["DIAL"] = "DDI", ["PHONE"] = "DDI",
    };

    public static bool AlwaysOn(string type) => type is "MRD" or "HOOT";

    public (int added, List<string> errors) Import(ImportInput input)
    {
        var errors = new List<string>();
        var rows = input.Lines ?? [];
        string via = (input.Via ?? "").Trim();
        var added = new List<LineEntry>();
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            string uri = (r.Uri ?? "").Trim();
            var m = UriPattern().Match(uri);
            if (!m.Success) { errors.Add($"Row {i + 1}: '{uri}' is not a SIP URI or a number."); continue; }
            if (!Types.TryGetValue((r.Type ?? "").Trim(), out string? type)) { errors.Add($"Row {i + 1}: line type '{r.Type}' must be ARD, MRD, Hoot or DDI."); continue; }
            if (via.Length == 0 && !m.Groups["host"].Success) { errors.Add($"Row {i + 1}: '{uri}' has no host; give the full SIP URI or the SBC to send through."); continue; }
            string number = m.Groups["user"].Value;
            added.Add(new LineEntry
            {
                Name = (r.Name ?? "").Trim() is { Length: > 0 } n ? n : number,
                Uri = uri, Number = number, Type = type,
                Group = (r.Group ?? "").Trim(), Via = via,
            });
        }
        if (errors.Count > 0) return (0, errors);
        lock (_gate)
        {
            if (input.Replace) _lines.Clear();
            foreach (var l in added)
            {
                // Same number through the same SBC is the same line: update it in place.
                var same = _lines.FirstOrDefault(x => x.Number == l.Number && SbcOf(x) == SbcOf(l));
                if (same is not null) { l.Id = same.Id; _lines[_lines.IndexOf(same)] = l; }
                else { l.Id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(); _lines.Add(l); }
            }
            Save();
        }
        return (added.Count, errors);
    }

    public bool Delete(string id)
    {
        lock (_gate)
        {
            int n = _lines.RemoveAll(l => l.Id == id);
            if (n > 0) Save();
            return n > 0;
        }
    }

    public LineEntry? Find(string id)
    {
        lock (_gate) return _lines.FirstOrDefault(l => l.Id == id);
    }

    /// <summary>The SBC address a line's calls go to: the Via host, else the URI's host.</summary>
    public static string SbcOf(LineEntry l)
    {
        string target = l.Via.Length > 0 ? l.Via : UriPattern().Match(l.Uri) is { Success: true } m && m.Groups["host"].Success ? m.Groups["host"].Value : "";
        int colon = target.IndexOf(':');
        return colon > 0 ? target[..colon] : target;
    }

    /// <summary>Every line with its state now. Starts a background read of the SBC call lists when they are stale.</summary>
    private (List<LineView> lines, object sbcs, bool live, DateTime now) Build()
    {
        List<LineEntry> lines;
        lock (_gate) lines = _lines.ToList();
        var now = DateTime.UtcNow;
        bool live = _recorder.Opened is null;

        var sbcs = lines.Where(l => AlwaysOn(l.Type)).Select(SbcOf).Where(s => s.Length > 0).Distinct().ToList();
        foreach (string sbc in sbcs) RefreshIfStale(sbc);

        // Latest line-test result per number and SBC.
        var lastTest = new Dictionary<string, (string status, string summary, DateTime at, double? mos)>();
        foreach (var run in _bulk.Runs().AsEnumerable().Reverse())   // oldest first, newest wins
            foreach (var bl in run.Lines.Where(x => x.Status is "passed" or "warning" or "failed"))
            {
                var m = UriPattern().Match(bl.Uri);
                if (!m.Success) continue;
                string host = run.Via.Length > 0 ? run.Via.Split(':')[0] : m.Groups["host"].Value;
                lastTest[$"{m.Groups["user"].Value}|{host}"] = (bl.Status, bl.Summary, run.Ended ?? run.Started, bl.Mos);
            }

        // One pass per SBC over the call view and the SBC's call list, not one per line (boards run to 1,000+ lines).
        var allSbcs = lines.Select(SbcOf).Distinct().ToList();
        var liveBySbc = allSbcs.ToDictionary(s => s, s => live ? _recorder.Store.LiveCallsByNumber(s) : []);
        var mediaBySbc = allSbcs.ToDictionary(s => s, s => live ? _recorder.Store.MediaBySession(s) : []);
        var legsBySbc = new Dictionary<string, Dictionary<string, List<BulkTests.SbcCall>>?>();
        foreach (string s in allSbcs)
        {
            List<BulkTests.SbcCall>? calls;
            lock (_reads) calls = _reads.TryGetValue(s, out var rd) ? rd.Calls : null;
            legsBySbc[s] = calls?.SelectMany(c => new[] { (n: c.Caller, c), (n: c.Callee, c) }).Where(x => x.n.Length > 0)
                .GroupBy(x => x.n).ToDictionary(g => g.Key, g => g.Select(x => x.c).Distinct().ToList());
        }

        var views = lines.Select(l =>
        {
            string sbc = SbcOf(l);
            SbcRead? read;
            lock (_reads) _reads.TryGetValue(sbc, out read);
            lastTest.TryGetValue($"{l.Number}|{sbc}", out var test);
            var captured = liveBySbc[sbc].TryGetValue(l.Number, out var cl) ? cl : [];

            string state, detail;
            string? since = null;
            List<string> sessions = [];
            if (AlwaysOn(l.Type))
            {
                var byNumber = legsBySbc[sbc];
                var legs = byNumber is null ? null : byNumber.TryGetValue(l.Number, out var found) ? found : [];
                if (legs is not null)
                {
                    sessions = legs.Select(c => c.Session).ToList();
                    if (legs.Count == 0) { state = "down"; detail = "No call for this number on the SBC: the wire is down."; }
                    else if (legs.Any(c => !c.State.Equals("Connected", StringComparison.OrdinalIgnoreCase)))
                    { state = "warn"; detail = $"Wire leg {legs.First(c => !c.State.Equals("Connected", StringComparison.OrdinalIgnoreCase)).State}."; since = legs[0].Duration; }
                    else { state = "up"; detail = $"Wire {legs[0].Caller} ↔ {legs[0].Callee}, {legs.Count} leg{(legs.Count == 1 ? "" : "s")}."; since = legs[0].Duration; }
                }
                else if (captured.Count > 0)
                {
                    sessions = captured.Select(c => c.session).ToList();
                    state = "up"; detail = "Wire call seen in the capture (no SBC call list).";
                }
                else
                {
                    state = "unknown";
                    detail = _settings.FindSbc(sbc) is not { HasLogin: true }
                        ? "Add an SSH login for this SBC on the SBCs page to see the wire's state."
                        : read?.Error is { Length: > 0 } err ? $"SBC call list: {err}" : "Reading the SBC's call list…";
                }
            }
            else if (captured.Count > 0)
            {
                sessions = captured.Select(c => c.session).ToList();
                state = "call"; detail = $"In a call: {captured[0].from} → {captured[0].to}.";
            }
            else if (test.status is not null)
            {
                state = test.status == "passed" ? "up" : test.status == "warning" ? "warn" : "down";
                detail = test.summary;
            }
            else { state = "idle"; detail = "Not tested yet."; }

            // Audio rate each way on the line's sessions.
            double? toRate = null, fromRate = null, mos = null;
            if (live && sessions.Count > 0)
            {
                var media = sessions.Where(mediaBySbc[sbc].ContainsKey).Select(x => mediaBySbc[sbc][x]).ToList();
                if (media.Any(x => x.ToSbc + x.FromSbc > 0))
                {
                    toRate = 0; fromRate = 0;
                    foreach (var x in media)
                    {
                        var (tr, fr) = Rate($"{sbc}|{x.Session}", now, x.ToSbc, x.FromSbc);
                        toRate += tr; fromRate += fr;
                    }
                    mos = media.Select(x => x.Mos).Where(x => x is not null).DefaultIfEmpty(null).Min();
                    if (state == "up" && AlwaysOn(l.Type) && (toRate == 0 || fromRate == 0))
                    {
                        state = "warn";
                        detail += fromRate == 0 ? " No audio from the SBC to the dealerboard right now." : " No audio from the dealerboard right now (normal while nobody talks on the line).";
                    }
                }
            }

            return new LineView(l.Id, l.Name, l.Uri, l.Number, l.Type, l.Group, l.Via, sbc, state, detail, since,
                toRate is double a ? Math.Round(a, 0) : null,
                fromRate is double b ? Math.Round(b, 0) : null,
                mos ?? (state is "up" or "warn" or "down" && !AlwaysOn(l.Type) ? test.mos : null),
                test.status is null ? null : new LastTestView(test.status, test.at, test.summary),
                sessions);
        }).ToList();

        object sbcView;
        lock (_reads)
            sbcView = sbcs.Select(s => _reads.TryGetValue(s, out var r)
                ? new { sbc = s, checkedAt = r.At == default ? (DateTime?)null : r.At, error = r.Error, calls = r.Calls?.Count }
                : new { sbc = s, checkedAt = (DateTime?)null, error = "", calls = (int?)null }).ToList();
        return (views, sbcView, live, now);
    }

    public object Snapshot()
    {
        var (lines, sbcs, live, now) = Build();
        return new { lines, sbcs, live, now };
    }

    /// <summary>The board's lines with their state, for checks that read it (the ready check).</summary>
    public List<LineView> Views() => Build().lines;

    /// <summary>Reads every SBC's call list now (not from the 30 s cache), so a check sees the wires as they are.</summary>
    public async Task<List<string>> ReadSbcsNow(CancellationToken ct)
    {
        List<LineEntry> lines;
        lock (_gate) lines = _lines.ToList();
        var problems = new List<string>();
        foreach (string sbc in lines.Where(l => AlwaysOn(l.Type)).Select(SbcOf).Where(s => s.Length > 0).Distinct())
        {
            if (_settings.FindSbc(sbc) is not { HasLogin: true }) continue;
            SbcRead r;
            lock (_reads) { if (!_reads.TryGetValue(sbc, out r!)) _reads[sbc] = r = new SbcRead(); }
            try
            {
                var calls = await _bulk.ActiveCalls(sbc, ct, 0);
                lock (_reads) { r.Calls = calls; r.Error = ""; r.At = DateTime.UtcNow; }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { lock (_reads) { r.Error = e.Message; r.At = DateTime.UtcNow; } problems.Add($"{sbc}: {e.Message}"); }
        }
        // Two looks a few seconds apart, so audio rates are measured over a real interval.
        Build();
        await Task.Delay(4000, ct);
        return problems;
    }

    private (double to, double from) Rate(string key, DateTime now, long toSbc, long fromSbc)
    {
        lock (_rates)
        {
            if (_rates.TryGetValue(key, out var prev))
            {
                double secs = (now - prev.at).TotalSeconds;
                if (secs < 2) return (prev.toRate, prev.fromRate);   // too soon: keep the last rate
                double tr = Math.Max(0, (toSbc - prev.toSbc) / secs), fr = Math.Max(0, (fromSbc - prev.fromSbc) / secs);
                _rates[key] = (now, toSbc, fromSbc, tr, fr);
                return (tr, fr);
            }
            _rates[key] = (now, toSbc, fromSbc, 0, 0);
            return (0, 0);
        }
    }

    private void RefreshIfStale(string sbc)
    {
        if (_settings.FindSbc(sbc) is not { HasLogin: true }) return;
        SbcRead r;
        lock (_reads)
        {
            if (!_reads.TryGetValue(sbc, out r!)) _reads[sbc] = r = new SbcRead();
            if (r.Busy || DateTime.UtcNow - r.At < TimeSpan.FromSeconds(30)) return;
            r.Busy = true;
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var calls = await _bulk.ActiveCalls(sbc, CancellationToken.None, 25);
                lock (_reads) { r.Calls = calls; r.Error = ""; }
            }
            catch (Exception e) { lock (_reads) r.Error = e.Message; }
            finally { lock (_reads) { r.At = DateTime.UtcNow; r.Busy = false; } }
        });
    }

    /// <summary>Line-test input for one line: a wire check for MRD / Hoot, a call for ARD / DDI.</summary>
    public BulkTests.StartInput TestInput(IEnumerable<LineEntry> lines, string name) => new()
    {
        Name = name,
        Via = lines.Select(l => l.Via).Distinct().Count() == 1 ? lines.First().Via : "",
        Lines = lines.Select(l => new BulkTests.LineInput
        {
            Name = l.Name,
            // With no common Via, each line carries its own SBC in the URI.
            Uri = l.Via.Length > 0 && lines.Select(x => x.Via).Distinct().Count() > 1 ? $"sip:{l.Number}@{l.Via}" : l.Uri,
            Type = l.Type,
        }).ToList(),
    };

    public List<LineEntry> All()
    {
        lock (_gate) return _lines.ToList();
    }

    private void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(_lines, Json)); }
        catch (Exception e) { Log.Write($"lines.json could not be saved: {e.Message}"); }
    }
}
