using System.Text.Json;

namespace MediantTrace;

/// <summary>
/// Watches the line board in the background: every few minutes it reads the SBCs'
/// call lists, records when a line changes state (a change only counts once it is
/// seen on two checks in a row, so one missed read is not an outage), keeps per-line
/// uptime by day, and tells someone (webhook / email) when a wire goes down and when
/// it is back. Answers "was 9912 down overnight?".
/// </summary>
public sealed class LineMonitor : IDisposable
{
    private readonly Settings _settings;
    private readonly LineBoard _board;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private MonitorConfig _config = new();
    private List<LineEvent> _events = [];
    // line id -> day (yyyy-MM-dd, local) -> (checks, checks up)
    private Dictionary<string, Dictionary<string, int[]>> _days = new();
    private readonly Dictionary<string, (string confirmed, string pending, int count)> _state = new();
    private DateTime _lastCheck;
    private string _lastError = "";
    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "line-monitor.json");
    private static string EventsPath => Path.Combine(AppContext.BaseDirectory, "line-events.json");
    private static string DaysPath => Path.Combine(AppContext.BaseDirectory, "line-uptime.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public sealed class MonitorConfig
    {
        public bool Enabled { get; set; }
        public int IntervalMinutes { get; set; } = 2;
        public bool AlertDown { get; set; } = true;       // a wire went down / came back
        public bool AlertOneWay { get; set; }             // audio one way for two checks running (noisy on quiet wires)
        public int Confirm { get; set; } = 2;             // checks in a row before a change counts
    }

    public sealed class LineEvent
    {
        public DateTime At { get; set; }
        public string LineId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Number { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    public LineMonitor(Settings settings, LineBoard board)
    {
        _settings = settings;
        _board = board;
        try { if (File.Exists(ConfigPath)) _config = JsonSerializer.Deserialize<MonitorConfig>(File.ReadAllText(ConfigPath)) ?? new(); } catch (Exception e) { Log.Write($"line-monitor.json: {e.Message}"); }
        try { if (File.Exists(EventsPath)) _events = JsonSerializer.Deserialize<List<LineEvent>>(File.ReadAllText(EventsPath)) ?? []; } catch (Exception e) { Log.Write($"line-events.json: {e.Message}"); }
        try { if (File.Exists(DaysPath)) _days = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int[]>>>(File.ReadAllText(DaysPath)) ?? new(); } catch (Exception e) { Log.Write($"line-uptime.json: {e.Message}"); }
        // Pick up where the last run left off, so a restart is not reported as every line changing.
        foreach (var g in _events.GroupBy(e => e.LineId)) _state[g.Key] = (g.Last().To, g.Last().To, 0);
        _ = Task.Run(() => Loop(_stop.Token));
    }

    public List<LineEvent> Events(DateTime since)
    {
        lock (_gate) return _events.Where(e => e.At >= since && e.From.Length > 0).ToList();
    }

    public MonitorConfig Config()
    {
        lock (_gate) return new MonitorConfig { Enabled = _config.Enabled, IntervalMinutes = _config.IntervalMinutes, AlertDown = _config.AlertDown, AlertOneWay = _config.AlertOneWay, Confirm = _config.Confirm };
    }

    public string SaveConfig(MonitorConfig c)
    {
        lock (_gate)
        {
            _config = new MonitorConfig
            {
                Enabled = c.Enabled, IntervalMinutes = Math.Clamp(c.IntervalMinutes, 1, 60),
                AlertDown = c.AlertDown, AlertOneWay = c.AlertOneWay, Confirm = Math.Clamp(c.Confirm, 1, 5),
            };
            try { File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true })); }
            catch (Exception e) { return $"Could not save: {e.Message}"; }
            return _config.Enabled ? $"Monitoring every {_config.IntervalMinutes} min." : "Monitoring off.";
        }
    }

    /// <summary>Status, recent changes (all lines or one), and uptime per line over 24 h and 7 days.</summary>
    public object Snapshot(string? lineId = null, int limit = 200)
    {
        lock (_gate)
        {
            var today = DateTime.Now.Date;
            var uptime = _days.ToDictionary(k => k.Key, k =>
            {
                double? Pct(int days)
                {
                    int checks = 0, up = 0;
                    for (int d = 0; d < days; d++)
                        if (k.Value.TryGetValue(today.AddDays(-d).ToString("yyyy-MM-dd"), out var v)) { checks += v[0]; up += v[1]; }
                    return checks == 0 ? null : Math.Round(100.0 * up / checks, 1);
                }
                return new { day = Pct(1), week = Pct(7) };
            });
            return new
            {
                config = Config(),
                lastCheck = _lastCheck == default ? (DateTime?)null : _lastCheck,
                lastError = _lastError,
                events = _events.Where(e => lineId is null || e.LineId == lineId).Reverse().Take(limit).ToList(),
                uptime,
            };
        }
    }

    private async Task Loop(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), ct); } catch (OperationCanceledException) { return; }
        while (!ct.IsCancellationRequested)
        {
            MonitorConfig c = Config();
            if (c.Enabled)
            {
                try { await CheckOnce(c, ct); }
                catch (OperationCanceledException) { return; }
                catch (Exception e) { lock (_gate) _lastError = e.Message; Log.Write($"Line monitor: {e.Message}"); }
            }
            try { await Task.Delay(TimeSpan.FromMinutes(c.Enabled ? c.IntervalMinutes : 1), ct); } catch (OperationCanceledException) { return; }
        }
    }

    /// <summary>States that count as the line working, for uptime.</summary>
    private static bool IsUp(string state) => state is "up" or "call";

    private async Task CheckOnce(MonitorConfig c, CancellationToken ct)
    {
        var problems = await _board.ReadSbcsNow(ct);
        var lines = _board.Views();
        var changes = new List<LineEvent>();
        DateTime now = DateTime.UtcNow;
        string day = DateTime.Now.ToString("yyyy-MM-dd");
        lock (_gate)
        {
            _lastCheck = now;
            _lastError = problems.Count > 0 ? string.Join("; ", problems) : "";
            foreach (var l in lines)
            {
                // Only lines whose state the SBC reports right now: wires. ARD / DDI states come from tests.
                if (!LineBoard.AlwaysOn(l.Type) || l.State == "unknown") continue;
                if (!_days.TryGetValue(l.Id, out var byDay)) _days[l.Id] = byDay = new();
                if (!byDay.TryGetValue(day, out var counts)) byDay[day] = counts = new int[2];
                counts[0]++;
                if (IsUp(l.State) || l.State == "warn") counts[1]++;   // one-way right now still has a call up

                var (confirmed, pending, count) = _state.TryGetValue(l.Id, out var s) ? s : ("", "", 0);
                if (l.State == confirmed) { _state[l.Id] = (confirmed, confirmed, 0); continue; }
                count = l.State == pending ? count + 1 : 1;
                if (count >= c.Confirm || confirmed.Length == 0)
                {
                    if (confirmed.Length > 0)
                        changes.Add(new LineEvent { At = now, LineId = l.Id, Name = l.Name, Number = l.Number, From = confirmed, To = l.State, Detail = l.Detail });
                    else
                        _events.Add(new LineEvent { At = now, LineId = l.Id, Name = l.Name, Number = l.Number, From = "", To = l.State, Detail = "Monitoring started." });
                    _state[l.Id] = (l.State, l.State, 0);
                }
                else _state[l.Id] = (confirmed, l.State, count);
            }
            _events.AddRange(changes);
            // Age limits are set in Settings (Storage); this only stops a runaway file.
            DateTime cutoff = now.AddDays(-400);
            _events.RemoveAll(e => e.At < cutoff);
            if (_events.Count > 20000) _events.RemoveRange(0, _events.Count - 20000);
            foreach (var k in _days.Keys.ToList())
                foreach (var d in _days[k].Keys.Where(d => string.CompareOrdinal(d, DateTime.Now.AddDays(-400).ToString("yyyy-MM-dd")) < 0).ToList()) _days[k].Remove(d);
            Save();
        }
        foreach (var e in changes) Log.Write($"Line monitor: {e.Name} ({e.Number}) {e.From} -> {e.To}");

        // One message per check, listing every line that changed.
        var down = changes.Where(e => e.To == "down" && c.AlertDown).ToList();
        var back = changes.Where(e => e.From == "down" && IsUp(e.To) && c.AlertDown).ToList();
        var oneWay = changes.Where(e => e.To == "warn" && c.AlertOneWay).ToList();
        if (down.Count + oneWay.Count > 0)
        {
            string title = down.Count > 0 ? $"MediantTrace: {Names(down)} {(down.Count == 1 ? "is" : "are")} down" : $"MediantTrace: audio one way on {Names(oneWay)}";
            await TestAlerts.SendText(_settings, title, string.Join("\n", down.Concat(oneWay).Select(e => $"{e.Name} ({e.Number}): {e.Detail}")
                .Append(DateTime.Now.ToString("ddd dd MMM HH:mm"))), false);
        }
        if (back.Count > 0)
            await TestAlerts.SendText(_settings, $"MediantTrace: {Names(back)} {(back.Count == 1 ? "is" : "are")} back up",
                string.Join("\n", back.Select(e => $"{e.Name} ({e.Number}): {e.Detail}").Append(DateTime.Now.ToString("ddd dd MMM HH:mm"))), true);
    }

    private static string Names(List<LineEvent> list) =>
        list.Count <= 3 ? string.Join(", ", list.Select(e => e.Name)) : $"{list.Count} wires";

    public int PruneOlderThan(DateTime cutoff)
    {
        lock (_gate)
        {
            int n = _events.RemoveAll(e => e.At < cutoff);
            string day = cutoff.ToLocalTime().ToString("yyyy-MM-dd");
            foreach (var k in _days.Keys.ToList())
                foreach (var d in _days[k].Keys.Where(d => string.CompareOrdinal(d, day) < 0).ToList()) { _days[k].Remove(d); n++; }
            if (n > 0) Save();
            return n;
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(EventsPath, JsonSerializer.Serialize(_events, Json));
            File.WriteAllText(DaysPath, JsonSerializer.Serialize(_days, Json));
        }
        catch (Exception e) { Log.Write($"Line monitor could not save: {e.Message}"); }
    }

    public void Dispose() => _stop.Cancel();
}
