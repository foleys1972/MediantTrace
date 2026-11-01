namespace MediantTrace;

/// <summary>
/// "Which change broke it": everything MediantTrace knows that can explain a
/// problem, on one time axis. SBC alarms and restarts, config changes and
/// upgrades, wires going down and back, call tests failing and recovering,
/// line test runs and ready checks. The page draws failed calls above it.
/// </summary>
public sealed class Timeline
{
    private readonly ChangeManager _changes;
    private readonly SbcHealth _health;
    private readonly LineMonitor _monitor;
    private readonly SipTester _tester;
    private readonly BulkTests _bulk;
    private readonly ReadyCheck _ready;
    private readonly Upgrades _upgrades;

    public sealed record Event(DateTime At, string Kind, string Level, string Sbc, string Title, string Detail, string Link);

    public Timeline(ChangeManager changes, SbcHealth health, LineMonitor monitor, SipTester tester, BulkTests bulk, ReadyCheck ready, Upgrades upgrades)
    {
        _changes = changes; _health = health; _monitor = monitor; _tester = tester; _bulk = bulk; _ready = ready; _upgrades = upgrades;
    }

    public List<Event> Since(DateTime since)
    {
        var list = new List<Event>();

        // SBC alarms (from its own history) and restarts.
        foreach (var r in _health.Reports())
        {
            foreach (var a in r.Report.AlarmHistory)
            {
                if (SbcHealth.AlarmUtc(a.Date, r.Report.ClockOffsetMinutes) is not DateTime at || at < since) continue;
                bool cleared = a.Severity == "cleared";
                list.Add(new Event(at, "alarm", cleared ? "ok" : a.Severity is "critical" or "major" ? "bad" : "warn", r.Name,
                    cleared ? a.Description.Replace("Alarm cleared: ", "Cleared: ") : $"{Cap(a.Severity)} alarm: {a.Description}", a.Source, "#view=health"));
            }
            if (r.Report.UptimeHours is double up)
            {
                // Rounded to the minute: uptime is read in whole minutes.
                var restarted = r.At.AddHours(-up);
                restarted = new DateTime(restarted.Ticks - restarted.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);
                if (restarted >= since)
                    list.Add(new Event(restarted, "restart", "warn", r.Name, "SBC restarted", "Calls on it dropped; changes not saved before then were lost.", "#view=health"));
            }
        }

        // Config changes made through MediantTrace.
        foreach (var p in _changes.Plans().Where(p => p.Status is "applied" or "failed" && (p.Applied ?? p.Created) >= since))
            list.Add(new Event(p.Applied ?? p.Created, "change", p.Status == "failed" || p.Errors > 0 ? "bad" : "info", p.SbcName,
                p.Kind == "save" ? "Configuration saved to flash" : $"Configuration change{(p.Status == "failed" ? " failed" : p.Errors > 0 ? $" applied with {p.Errors} error{(p.Errors == 1 ? "" : "s")}" : " applied")}",
                $"{p.Reason} ({p.Commands.Count} command{(p.Commands.Count == 1 ? "" : "s")}, {p.RequestedBy}{(p.ApprovedVia.Length > 0 ? ", approved " + p.ApprovedVia : "")})", "#view=changes"));

        // Upgrades
        foreach (var u in _upgrades.List())
        {
            if (u.Before is { } b && b.At >= since)
                list.Add(new Event(b.At, "upgrade", "info", u.SbcName, "Upgrade baseline taken", $"{b.Version}{(u.Note.Length > 0 ? " · " + u.Note : "")}", "#view=upgrades"));
            if (u.After is { } a && a.At >= since)
                list.Add(new Event(a.At, "upgrade", "info", u.SbcName, "After-upgrade snapshot", $"{u.Before?.Version} → {a.Version}", "#view=upgrades"));
        }

        // Wires (line monitor)
        foreach (var e in _monitor.Events(since))
            list.Add(new Event(e.At, "wire", e.To == "down" ? "bad" : e.To == "warn" ? "warn" : e.To is "up" or "call" ? "ok" : "info", "",
                $"{e.Name} ({e.Number}): {Word(e.From)} → {Word(e.To)}", e.Detail, "#view=lines"));

        // Call tests: failures, and the first pass after one.
        foreach (var g in _tester.ResultsSince(since.AddDays(-1)).GroupBy(r => r.TestId))
        {
            string previous = "";
            foreach (var r in g.OrderBy(r => r.Started))
            {
                if (r.Started >= since)
                {
                    if (r.Outcome is "failed" or "warning")
                        list.Add(new Event(r.Started, "test", r.Outcome == "failed" ? "bad" : "warn", "", $"Call test {r.Name} {(r.Outcome == "failed" ? "failed" : "warning")}",
                            r.Findings.FirstOrDefault() ?? "", $"#result={r.Id}"));
                    else if (r.Outcome == "passed" && previous is "failed")
                        list.Add(new Event(r.Started, "test", "ok", "", $"Call test {r.Name} passed again", r.MosRx is double mos ? $"MOS {mos:0.0}" : "", $"#result={r.Id}"));
                }
                previous = r.Outcome;
            }
        }

        // Line test runs
        foreach (var run in _bulk.Runs().Where(r => r.Started >= since && r.State != "running"))
        {
            int failed = run.Lines.Count(l => l.Status == "failed"), warn = run.Lines.Count(l => l.Status == "warning");
            list.Add(new Event(run.Ended ?? run.Started, "linetest", failed > 0 ? "bad" : warn > 0 ? "warn" : "ok", "",
                $"Line test: {run.Name}", $"{run.Lines.Count(l => l.Status == "passed")} passed, {warn} warning{(warn == 1 ? "" : "s")}, {failed} failed of {run.Lines.Count}", "#view=bulk"));
        }

        // Ready checks
        foreach (var r in _ready.Runs(200).Where(r => r.Started >= since && r.Verdict != "running"))
            list.Add(new Event(r.Ended ?? r.Started, "ready", r.Verdict == "ready" ? "ok" : r.Verdict == "warnings" ? "warn" : r.Verdict == "notready" ? "bad" : "info", "",
                $"Ready check: {ReadyCheck.VerdictText(r.Verdict)}", string.Join("; ", r.Items.Where(i => i.State == "bad").Take(3).Select(i => $"{i.Name}: {i.Detail}")), "#view=ready"));

        return list.OrderByDescending(e => e.At).ToList();
    }

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string Word(string st) => st switch
    {
        "up" => "up", "warn" => "one-way audio", "down" => "down", "call" => "in call", _ => st,
    };
}
