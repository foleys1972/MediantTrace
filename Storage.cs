namespace MediantTrace;

/// <summary>
/// Everything MediantTrace writes to disk, in one place: where each kind of data
/// goes, how long it is kept, how much room it takes and how much is left on the
/// drive. Runs the clean-up hourly. Folders may be absolute (another drive, a
/// share) or relative to the program's folder.
/// </summary>
public sealed class Storage : IDisposable
{
    private readonly Settings _settings;
    private readonly Recorder _recorder;
    private readonly SipTester _tester;
    private readonly BulkTests _bulk;
    private readonly ReadyCheck _ready;
    private readonly LineMonitor _monitor;
    private readonly Upgrades _upgrades;
    private readonly ChangeManager _changes;
    private readonly CancellationTokenSource _stop = new();
    private (DateTime at, List<Row> rows) _cache = (DateTime.MinValue, []);
    private readonly object _gate = new();

    public sealed record Row(string Key, string Name, string Folder, string Keep, long Bytes, int Files, double? FreeGb, string Note);

    public Storage(Settings settings, Recorder recorder, SipTester tester, BulkTests bulk, ReadyCheck ready, LineMonitor monitor, Upgrades upgrades, ChangeManager changes)
    {
        _settings = settings; _recorder = recorder; _tester = tester; _bulk = bulk; _ready = ready; _monitor = monitor; _upgrades = upgrades; _changes = changes;
        Apply();
        _ = Task.Run(() => Loop(_stop.Token));
    }

    /// <summary>Points the program log, the backups and the SBC logs at the folders in settings. Safe to call after every save.</summary>
    public void Apply()
    {
        Log.Configure(_settings.ProgramLogFolderFull, () => Math.Clamp(_settings.ProgramLogMaxMb, 1, 1000) * 1024L * 1024);
        ChangeManager.BackupDir = _settings.BackupFolderFull;
        _recorder.ApplyLogFolder();
        lock (_gate) _cache = (DateTime.MinValue, []);
    }

    /// <summary>Checks a folder can be created and written to. Returns a reason when it cannot.</summary>
    public static string? FolderProblem(string folder)
    {
        string full;
        try { full = Settings.FullFolder(folder, "x"); }
        catch (Exception e) { return $"'{folder}' is not a folder path ({e.Message})."; }
        try
        {
            Directory.CreateDirectory(full);
            string probe = Path.Combine(full, $".mediantrace-write-test-{Guid.NewGuid():n}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return null;
        }
        catch (Exception e) { return $"Cannot write to {full}: {e.Message}"; }
    }

    public sealed record Drive(string Path, string Label, string Type, double TotalGb, double FreeGb);
    public sealed record Folder(string Name, string Path);

    /// <summary>The server's drives that are ready (local disks, network drives, USB), for the folder picker.</summary>
    public static List<Drive> Drives() =>
        DriveInfo.GetDrives().Where(d => d.DriveType is DriveType.Fixed or DriveType.Network or DriveType.Removable)
            .Select(d =>
            {
                try
                {
                    if (!d.IsReady || d.TotalSize <= 0) return null;   // empty card readers report 0 bytes
                    return new Drive(d.RootDirectory.FullName, d.VolumeLabel, d.DriveType switch { DriveType.Network => "network", DriveType.Removable => "removable", _ => "local" },
                        Math.Round(d.TotalSize / 1073741824.0, 1), Math.Round(d.AvailableFreeSpace / 1073741824.0, 1));
                }
                catch { return null; }   // a disconnected network drive
            }).OfType<Drive>().ToList();

    /// <summary>The folders inside one folder (names only, never files). Hidden and system folders are left out.</summary>
    public static (string path, string? parent, List<Folder> folders, string? error) Browse(string path)
    {
        string full;
        try { full = System.IO.Path.GetFullPath(path); } catch (Exception e) { return (path, null, [], e.Message); }
        var dir = new DirectoryInfo(full);
        if (!dir.Exists) return (full, dir.Parent?.FullName, [], "That folder does not exist.");
        try
        {
            var folders = dir.EnumerateDirectories()
                .Where(d => (d.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).Take(500)
                .Select(d => new Folder(d.Name, d.FullName)).ToList();
            return (dir.FullName, dir.Parent?.FullName, folders, null);
        }
        catch (Exception e) { return (dir.FullName, dir.Parent?.FullName, [], $"Cannot open it: {e.Message}"); }
    }

    /// <summary>Makes a folder inside <paramref name="parent"/>. Returns its path, or why not.</summary>
    public static (string? path, string? error) MakeFolder(string parent, string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0 || name.Length > 120 || name is "." or ".." || name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            return (null, @"Give a folder name without \ / : * ? "" < > |.");
        try
        {
            string full = System.IO.Path.Combine(System.IO.Path.GetFullPath(parent), name);
            Directory.CreateDirectory(full);
            return (full, null);
        }
        catch (Exception e) { return (null, e.Message); }
    }

    public List<Row> Rows()
    {
        lock (_gate)
            if (DateTime.UtcNow - _cache.at < TimeSpan.FromSeconds(60)) return _cache.rows;
        var s = _settings;
        string Days(int d, string none = "for ever") => d <= 0 ? none : $"{d} day{(d == 1 ? "" : "s")}";
        var rows = new List<Row>
        {
            Measure("captures", "Packet captures (.pcap / .txt)", s.TraceFolderFull, "*.*",
                $"{Days(s.RetentionDays)}{(s.RetentionMaxMb > 0 ? $", at most {s.RetentionMaxMb:N0} MB" : "")}", "Only while a packet capture runs."),
            Measure("logs", "SBC logs (Splunk JSON)", _recorder.LogFolder, "*.log",
                $"{Days(s.LogRetentionDays)}{(s.LogMaxTotalMb > 0 ? $", at most {s.LogMaxTotalMb:N0} MB" : "")}, files of {s.LogMaxMb} MB", "Written all the time: SIP, call records, SBC log lines."),
            Measure("backups", "SBC config backups", s.BackupFolderFull, "*.txt",
                $"{Days(s.BackupRetentionDays)}, always the newest 10 per SBC", "Taken before every change and for upgrades. Backups an upgrade compares are kept."),
            Measure("programlog", "MediantTrace's own log", s.ProgramLogFolderFull, "mediant-trace*.log",
                $"{s.ProgramLogMaxMb} MB, then 3 older files", "Start-up, errors, alerts, checks."),
            Measure("history", "History (tests, line tests, ready checks, wire changes)", AppContext.BaseDirectory, "*.json",
                Days(s.HistoryDays), "Next to the program, with the settings."),
        };
        lock (_gate) _cache = (DateTime.UtcNow, rows);
        return rows;
    }

    private static Row Measure(string key, string name, string folder, string pattern, string keep, string note)
    {
        long bytes = 0; int files = 0; double? free = null;
        try
        {
            if (Directory.Exists(folder))
                foreach (var f in new DirectoryInfo(folder).EnumerateFiles(pattern, key is "programlog" or "history" ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
                { bytes += f.Length; files++; }
        }
        catch { /* a share may be offline */ }
        try { free = Math.Round(new DriveInfo(Path.GetPathRoot(Path.GetFullPath(folder))!).AvailableFreeSpace / 1024.0 / 1024 / 1024, 1); } catch { }
        return new Row(key, name, folder, keep, bytes, files, free, note);
    }

    private async Task Loop(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), ct); } catch (OperationCanceledException) { return; }
        while (!ct.IsCancellationRequested)
        {
            try { CleanUp(); } catch (Exception e) { Log.Write($"Storage clean-up: {e.Message}"); }
            try { await Task.Delay(TimeSpan.FromHours(1), ct); } catch (OperationCanceledException) { return; }
        }
    }

    /// <summary>One pass of every retention rule. Returns what was removed, for the log.</summary>
    public string CleanUp()
    {
        var s = _settings;
        var done = new List<string>();
        int caps = _recorder.ApplyRetention();
        if (caps > 0) done.Add($"{caps} capture{(caps == 1 ? "" : "s")}");
        int days = SplunkLog.Prune(_recorder.LogFolder, s.LogRetentionDays);
        int capped = SplunkLog.PruneToSize(_recorder.LogFolder, s.LogMaxTotalMb * 1024L * 1024);
        if (days + capped > 0) done.Add($"{days + capped} SBC log folder{(days + capped == 1 ? "" : "s")}");
        int backups = PruneBackups();
        if (backups > 0) done.Add($"{backups} config backup{(backups == 1 ? "" : "s")}");
        if (s.HistoryDays > 0)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-s.HistoryDays);
            int h = _tester.PruneOlderThan(cutoff) + _bulk.PruneOlderThan(cutoff) + _ready.PruneOlderThan(cutoff) + _monitor.PruneOlderThan(cutoff);
            if (h > 0) done.Add($"{h} history entr{(h == 1 ? "y" : "ies")}");
        }
        lock (_gate) _cache = (DateTime.MinValue, []);
        string summary = done.Count == 0 ? "nothing to remove" : "removed " + string.Join(", ", done);
        if (done.Count > 0) Log.Write($"Storage clean-up: {summary}");
        return summary;
    }

    /// <summary>Backups older than the limit go, except the newest 10 per SBC and any an upgrade or a change still points at.</summary>
    private int PruneBackups()
    {
        if (_settings.BackupRetentionDays <= 0 || !Directory.Exists(ChangeManager.BackupDir)) return 0;
        var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in _upgrades.List()) { if (u.Before is { } b) keep.Add(b.ConfigFile); if (u.After is { } a) keep.Add(a.ConfigFile); }
        foreach (var p in _changes.Plans()) if (p.Backup.Length > 0) keep.Add(Path.GetFileName(p.Backup));
        DateTime cutoff = DateTime.Now.AddDays(-_settings.BackupRetentionDays);
        int removed = 0;
        foreach (var bySbc in new DirectoryInfo(ChangeManager.BackupDir).EnumerateFiles("*-running-config.txt").GroupBy(f => f.Name.Split('-')[0]))
            foreach (var f in bySbc.OrderByDescending(f => f.LastWriteTime).Skip(10).Where(f => f.LastWriteTime < cutoff && !keep.Contains(f.Name)))
            {
                try { f.Delete(); removed++; } catch { }
            }
        return removed;
    }

    public void Dispose() => _stop.Cancel();
}
