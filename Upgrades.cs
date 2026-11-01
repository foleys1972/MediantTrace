using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Upgrade assistant: a baseline of an SBC before a firmware upgrade (version,
/// licence, full config backup, alarms, proxy sets, E1 trunks, which wires are up)
/// and the same after, compared so only what changed is shown. The upgrade itself
/// is done by a person: a failed upgrade drops every call on the box.
/// Everything here is read-only on the SBC.
/// </summary>
public sealed partial class Upgrades
{
    private readonly Settings _settings;
    private readonly ChangeManager _changes;
    private readonly LineBoard _board;
    private readonly object _gate = new();
    private List<UpgradeRecord> _records = [];
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "upgrades.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private static readonly string[] Commands =
    [
        "show version", "show system uptime", "show system utilization", "show ntp", "show active-alarms",
        "show voip proxy sets status", "show voip interface e1-t1", "show voip calls statistics gw", "show voip calls statistics sbc",
    ];

    public sealed record Wire(string Name, string Number, string State);

    public sealed class Snapshot
    {
        public DateTime At { get; set; }
        public string Version { get; set; } = "";
        public string Board { get; set; } = "";
        public string Serial { get; set; } = "";
        public string Licence { get; set; } = "";
        public string Features { get; set; } = "";
        public double? UptimeHours { get; set; }
        public int ActiveCalls { get; set; }
        public List<ReadyCheck.Alarm> Alarms { get; set; } = [];
        public List<SbcHealth.ProxySet> ProxySets { get; set; } = [];
        public List<ReadyCheck.Trunk> Trunks { get; set; } = [];
        public List<Wire> Wires { get; set; } = [];
        public string ConfigFile { get; set; } = "";
        public int ConfigLines { get; set; }
    }

    public sealed class UpgradeRecord
    {
        public string Id { get; set; } = "";
        public string SbcIp { get; set; } = "";
        public string SbcName { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime Created { get; set; }
        public string Busy { get; set; } = "";     // "before" or "after" while a snapshot is being taken
        public string Error { get; set; } = "";
        public Snapshot? Before { get; set; }
        public Snapshot? After { get; set; }
    }

    public Upgrades(Settings settings, ChangeManager changes, LineBoard board)
    {
        _settings = settings; _changes = changes; _board = board;
        try { if (File.Exists(FilePath)) _records = JsonSerializer.Deserialize<List<UpgradeRecord>>(File.ReadAllText(FilePath)) ?? []; }
        catch (Exception e) { Log.Write($"upgrades.json could not be read: {e.Message}"); }
        foreach (var r in _records.Where(r => r.Busy.Length > 0)) { r.Error = "MediantTrace restarted while the snapshot was being taken."; r.Busy = ""; }
    }

    public List<UpgradeRecord> List()
    {
        lock (_gate) return _records.AsEnumerable().Reverse().Select(Copy).ToList();
    }

    private static UpgradeRecord Copy(UpgradeRecord r) => JsonSerializer.Deserialize<UpgradeRecord>(JsonSerializer.Serialize(r))!;

    /// <summary>Starts a baseline (new record) or the after-snapshot of an existing one.</summary>
    public (bool ok, string message, string id) Take(string? sbcIp, string? recordId, string? note)
    {
        UpgradeRecord rec;
        string phase;
        lock (_gate)
        {
            if (!string.IsNullOrEmpty(recordId))
            {
                rec = _records.FirstOrDefault(r => r.Id == recordId) ?? throw new InvalidOperationException("No such upgrade.");
                if (rec.Before is null) return (false, "Take the baseline first.", rec.Id);
                phase = "after";
            }
            else
            {
                var sbc = _settings.FindSbc(sbcIp ?? "") ?? throw new InvalidOperationException("Choose an SBC from the list.");
                if (!sbc.HasLogin) return (false, $"No SSH login for {sbc.Name}: add one on the SBCs page.", "");
                rec = new UpgradeRecord
                {
                    Id = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(),
                    SbcIp = sbc.Ip, SbcName = sbc.Name, Note = (note ?? "").Trim(), Created = DateTime.UtcNow,
                };
                _records.Add(rec);
                if (_records.Count > 100) _records.RemoveAt(0);
                phase = "before";
            }
            if (rec.Busy.Length > 0) return (false, "A snapshot of this SBC is already being taken.", rec.Id);
            rec.Busy = phase;
            rec.Error = "";
            Save();
        }
        _ = Task.Run(() =>
        {
            try
            {
                var snap = TakeSnapshot(rec.SbcIp);
                lock (_gate) { if (phase == "before") rec.Before = snap; else rec.After = snap; }
                Log.Write($"Upgrade {rec.Id}: {phase} snapshot of {rec.SbcName} taken ({snap.Version}, {snap.ConfigLines} config lines)");
            }
            catch (Exception e) { lock (_gate) rec.Error = e.Message; Log.Write($"Upgrade {rec.Id}: {phase} snapshot failed: {e.Message}"); }
            finally { lock (_gate) { rec.Busy = ""; Save(); } }
        });
        return (true, phase == "before" ? "Taking the baseline: about half a minute." : "Taking the after-upgrade snapshot: about half a minute.", rec.Id);
    }

    public bool Delete(string id)
    {
        lock (_gate)
        {
            int n = _records.RemoveAll(r => r.Id == id && r.Busy.Length == 0);
            if (n > 0) Save();
            return n > 0;
        }
    }

    private Snapshot TakeSnapshot(string sbcIp)
    {
        var outputs = _changes.Show(sbcIp, Commands);
        string Out(string c) => outputs.FirstOrDefault(o => o.command == c).output ?? "";
        var h = SbcHealth.Parse(Out);
        string version = Out("show version");
        string backup = _changes.Backup(sbcIp);   // full running-config, in sbc-backups
        var gw = h.Gateway; var sb = h.Sbc;
        int active = (int.TryParse(gw.GetValueOrDefault("Active TDM channels"), out int t) ? t : 0) + (int.TryParse(sb.GetValueOrDefault("Active INVITE dialogs"), out int d) ? d : 0);
        return new Snapshot
        {
            At = DateTime.UtcNow, Version = h.Version, Board = h.Board, Serial = h.Serial, Licence = h.SbcSessions,
            Features = Regex.Match(version, @"Key features:(.*?)(?:;Default features|$)", RegexOptions.Singleline) is { Success: true } f ? f.Groups[1].Value.Trim(';', ' ') : "",
            UptimeHours = h.UptimeHours, ActiveCalls = active,
            Alarms = h.Alarms, ProxySets = h.ProxySets, Trunks = h.Trunks,
            Wires = _board.Views().Where(l => LineBoard.AlwaysOn(l.Type) && l.Sbc == sbcIp).Select(l => new Wire(l.Name, l.Number, l.State)).ToList(),
            ConfigFile = Path.GetFileName(backup),
            ConfigLines = File.ReadAllLines(backup).Length,
        };
    }

    // ------------------------------------------------------------ compare

    public sealed record Change(string Area, string Level, string Text);   // Level: bad, warn, info
    public sealed record Hunk(string Section, List<(char op, string text)> Lines);

    public object Compare(string id)
    {
        UpgradeRecord rec;
        lock (_gate) rec = Copy(_records.FirstOrDefault(r => r.Id == id) ?? throw new InvalidOperationException("No such upgrade."));
        var b = rec.Before ?? throw new InvalidOperationException("No baseline yet.");
        var a = rec.After ?? throw new InvalidOperationException("No after-upgrade snapshot yet.");
        var changes = new List<Change>();

        changes.Add(b.Version == a.Version
            ? new Change("Version", "warn", $"Still {a.Version}: the upgrade does not seem to have happened.")
            : new Change("Version", "info", $"{b.Version} → {a.Version}."));
        if (a.UptimeHours is double up && b.Version != a.Version && up > (a.At - b.At).TotalHours + 0.1)
            changes.Add(new Change("Version", "warn", "The SBC has not restarted since the baseline, but the version changed: check the reading."));
        if (b.Licence != a.Licence) changes.Add(new Change("Licence", "bad", $"SBC sessions changed: {b.Licence} → {a.Licence}."));
        if (b.Features != a.Features) changes.Add(new Change("Licence", "warn", "The licensed features changed: compare the feature key with AudioCodes."));

        foreach (var x in a.Alarms.Where(x => !b.Alarms.Any(y => y.Source == x.Source && y.Description == x.Description)))
            changes.Add(new Change("Alarms", x.Severity is "critical" or "major" ? "bad" : "warn", $"New {x.Severity} alarm: {x.Description}"));
        foreach (var x in b.Alarms.Where(x => !a.Alarms.Any(y => y.Source == x.Source && y.Description == x.Description)))
            changes.Add(new Change("Alarms", "info", $"Cleared: {x.Description}"));

        foreach (var p in b.ProxySets)
        {
            var q = a.ProxySets.FirstOrDefault(x => x.Id == p.Id);
            if (q is null) changes.Add(new Change("Proxy sets", "bad", $"Proxy set {p.Id} is missing after the upgrade."));
            else if (p.Status != q.Status)
                changes.Add(new Change("Proxy sets", q.Status == "ONLINE" ? "info" : q.KeepAlive ? "bad" : "warn", $"Proxy set {p.Id}: {p.Status.ToLowerInvariant()} → {q.Status.ToLowerInvariant()}."));
        }
        foreach (var t in b.Trunks)
        {
            var u = a.Trunks.FirstOrDefault(x => x.Port == t.Port);
            bool good(ReadyCheck.Trunk x) => x.State == "active" && x.DChannel == "established" && x.AlarmsClear;
            if (u is null) changes.Add(new Change("E1 trunks", "bad", $"Trunk {t.Port} is missing after the upgrade."));
            else if (good(t) && !good(u)) changes.Add(new Change("E1 trunks", "bad", $"Trunk {t.Port} was up and is now: state {u.State}, D-channel {u.DChannel}{(u.AlarmsClear ? "" : ", alarms " + u.Alarms)}."));
            else if (!good(t) && good(u)) changes.Add(new Change("E1 trunks", "info", $"Trunk {t.Port} is up now (it was not before)."));
        }
        foreach (var w in b.Wires.Where(w => w.State is "up" or "warn"))
        {
            var v = a.Wires.FirstOrDefault(x => x.Number == w.Number);
            if (v is null || v.State is "down" or "unknown")
                changes.Add(new Change("Wires", "bad", $"{w.Name} ({w.Number}) was up and is {(v is null ? "not on the board" : v.State)} now."));
        }

        // Config
        string dir = ChangeManager.BackupDir;
        string[] before = ReadConfig(Path.Combine(dir, b.ConfigFile)), after = ReadConfig(Path.Combine(dir, a.ConfigFile));
        var hunks = DiffHunks(before, after);
        int added = hunks.Sum(h => h.Lines.Count(l => l.op == '+')), removed = hunks.Sum(h => h.Lines.Count(l => l.op == '-'));
        changes.Add(hunks.Count == 0
            ? new Change("Configuration", "info", "Configuration unchanged.")
            : new Change("Configuration", "warn", $"{added} line{(added == 1 ? "" : "s")} added and {removed} removed in {hunks.Count} place{(hunks.Count == 1 ? "" : "s")}: new firmware often adds default settings; check anything you did not expect."));

        string verdict = changes.Any(c => c.Level == "bad") ? "problems" : changes.Any(c => c.Level == "warn" && c.Area != "Configuration") ? "check" : "good";
        return new
        {
            verdict,
            changes = changes.OrderBy(c => c.Level == "bad" ? 0 : c.Level == "warn" ? 1 : 2).ToList(),
            config = new { added, removed, hunks = hunks.Take(200).Select(h => new { section = h.Section, lines = h.Lines.Select(l => new { op = l.op.ToString(), text = Mask(l.text) }) }) },
            before = b, after = a,
        };
    }

    private static string[] ReadConfig(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"The config backup {Path.GetFileName(path)} is missing from {ChangeManager.BackupDir}.");
        return File.ReadAllLines(path).Select(CleanLine).Where(l => l.Length > 0).ToArray();
    }

    [GeneratedRegex(@"\x1B\[[0-9;?]*[A-Za-z]|[\x00-\x08\x0B-\x1F\x7F]")]
    private static partial Regex ControlChars();

    /// <summary>A config line without the terminal codes the CLI's paging leaves behind, trimmed.</summary>
    public static string CleanLine(string line) => ControlChars().Replace(line, "").Trim();

    [GeneratedRegex(@"\b(password|passwd|secret|community|key|hash|pass-phrase|private)\b(\S*)\s+(\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex SecretLine();

    /// <summary>Hides values on lines that hold passwords, keys or hashes, so the page never shows them.</summary>
    public static string Mask(string line) => SecretLine().Replace(line, m => $"{m.Groups[1].Value}{m.Groups[2].Value} ******");

    /// <summary>Line diff (Myers, O((N+M)D)) grouped into hunks with two lines of context and the nearest "configure" section.</summary>
    public static List<Hunk> DiffHunks(string[] a, string[] b)
    {
        var ops = Diff(a, b);
        var hunks = new List<Hunk>();
        const int ctx = 2;
        int i = 0;
        while (i < ops.Count)
        {
            if (ops[i].op == ' ') { i++; continue; }
            int start = Math.Max(0, i - ctx), end = i;
            while (end < ops.Count)
            {
                int next = end;
                while (next < ops.Count && ops[next].op != ' ') next++;
                int same = next;
                while (same < ops.Count && ops[same].op == ' ') same++;
                if (same < ops.Count && same - next <= ctx * 2) { end = same; continue; }
                end = Math.Min(ops.Count, next + ctx);
                break;
            }
            // Section: the last "configure ..." and the last table-row line before the change.
            // A table row starts on the line after "exit" (or after "configure ...") and runs to its own "exit".
            string section = "", row = "";
            bool rowClosed = false;
            for (int k = i - 1; k >= 0; k--)
            {
                string t = ops[k].text;
                if (t.StartsWith("configure ", StringComparison.Ordinal)) { section = t; break; }
                if (row.Length > 0 || rowClosed) continue;
                if (t == "exit") { rowClosed = true; continue; }
                if (k > 0 && (ops[k - 1].text == "exit" || ops[k - 1].text.StartsWith("configure ", StringComparison.Ordinal))) row = t;
            }
            hunks.Add(new Hunk(row.Length > 0 && section.Length > 0 ? $"{section} › {row}" : section, ops.GetRange(start, end - start)));
            i = end;
        }
        return hunks;
    }

    // trace[d] holds V for k in [-d, d] as it was before step d (index k + d): O(D^2) memory, not O(D * (N + M)).
    private const int MaxEdits = 4000;

    private static List<(char op, string text)> Diff(string[] a, string[] b)
    {
        int n = a.Length, m = b.Length, max = n + m;
        var v = new int[2 * max + 3];
        int off = max + 1;
        var trace = new List<int[]>();
        for (int d = 0; d <= Math.Min(max, MaxEdits); d++)
        {
            var saved = new int[2 * d + 1];
            Array.Copy(v, off - d, saved, 0, 2 * d + 1);
            trace.Add(saved);
            for (int k = -d; k <= d; k += 2)
            {
                int x = k == -d || (k != d && v[off + k - 1] < v[off + k + 1]) ? v[off + k + 1] : v[off + k - 1] + 1;
                int y = x - k;
                while (x < n && y < m && a[x] == b[y]) { x++; y++; }
                v[off + k] = x;
                if (x >= n && y >= m) return Backtrack(trace, a, b);
            }
        }
        // Too different to line up: show it as all removed and all added.
        return a.Select(l => ('-', l)).Concat(b.Select(l => ('+', l))).ToList();
    }

    private static List<(char op, string text)> Backtrack(List<int[]> trace, string[] a, string[] b)
    {
        var result = new List<(char, string)>();
        int x = a.Length, y = b.Length;
        for (int d = trace.Count - 1; d >= 0; d--)
        {
            var v = trace[d];
            int V(int k) => k < -d || k > d ? 0 : v[k + d];
            int k = x - y;
            int prevK = k == -d || (k != d && V(k - 1) < V(k + 1)) ? k + 1 : k - 1;
            int prevX = d == 0 ? 0 : V(prevK), prevY = prevX - prevK;
            if (d == 0) { prevX = 0; prevY = 0; }
            while (x > prevX && y > prevY) { result.Add((' ', a[x - 1])); x--; y--; }
            if (d > 0)
            {
                if (x == prevX) result.Add(('+', b[y - 1])); else result.Add(('-', a[x - 1]));
            }
            x = prevX; y = prevY;
        }
        result.Reverse();
        return result;
    }

    private void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(_records, Json)); }
        catch (Exception e) { Log.Write($"upgrades.json could not be saved: {e.Message}"); }
    }
}
