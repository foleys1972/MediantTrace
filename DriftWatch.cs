namespace MediantTrace;

/// <summary>Compares each SBC's live INI with the saved standard. A check is remembered for ten minutes so the dashboard does not download on every refresh.</summary>
public sealed class DriftWatch
{
    private readonly Settings _settings;
    private readonly object _gate = new();
    private readonly Dictionary<string, Row> _rows = new(StringComparer.Ordinal);
    private readonly HashSet<string> _running = new(StringComparer.Ordinal);
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(10);

    public DriftWatch(Settings settings) => _settings = settings;

    public sealed record DriftView(string Ip, string Name, string State, int Differences, string? Message);

    public List<DriftView> Snapshot()
    {
        var list = new List<DriftView>();
        foreach (SbcEntry sbc in _settings.Sbcs.Where(s => s.Ip.Length > 0))
        {
            string? standard;
            try { standard = IniFiles.Read(sbc.Ip); }
            catch (InvalidOperationException) { continue; }
            if (standard is null)
            {
                list.Add(View(sbc, "none", 0, null));
                continue;
            }
            if (!sbc.HasLogin)
            {
                list.Add(View(sbc, "nologin", 0, null));
                continue;
            }
            Row? row;
            lock (_gate) _rows.TryGetValue(sbc.Ip, out row);
            bool due = row is null
                || (row.State == "error" && DateTime.UtcNow - row.Checked > TimeSpan.FromMinutes(2))
                || (row.State is "same" or "drift" && DateTime.UtcNow - row.Checked > Fresh);
            if (due) Kick(sbc, standard);
            list.Add(row is null ? View(sbc, "checking", 0, null) : View(sbc, row.State, row.Differences, row.Message));
        }
        return list;
    }

    private void Kick(SbcEntry sbc, string standard)
    {
        lock (_gate)
        {
            if (!_running.Add(sbc.Ip)) return;
        }
        _ = Task.Run(async () =>
        {
            var row = new Row { Checked = DateTime.UtcNow };
            try
            {
                string live = await IniFiles.Download(sbc.SshTarget, sbc.SshUser, ChangeManager.Unprotect(sbc.SshPasswordProtected), sbc.Url);
                int n = IniDiff.Count(standard, live);
                row.State = n == 0 ? "same" : "drift";
                row.Differences = n;
            }
            catch (Exception e)
            {
                row.State = "error";
                row.Message = e.Message;
            }
            lock (_gate)
            {
                _rows[sbc.Ip] = row;
                _running.Remove(sbc.Ip);
            }
        });
    }

    private static DriftView View(SbcEntry sbc, string state, int differences, string? message) =>
        new(sbc.Ip, sbc.Name.Length > 0 ? sbc.Name : sbc.Ip, state, differences, message);

    private sealed class Row
    {
        public string State { get; set; } = "checking";
        public int Differences { get; set; }
        public string? Message { get; set; }
        public DateTime Checked { get; set; }
    }
}
