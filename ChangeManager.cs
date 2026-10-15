using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediantTrace;

/// <summary>
/// SBC configuration through SSH, with a person in the loop for every change:
///   read      - show commands, any time;
///   plan      - commands are checked and stored as a plan with a one-time code;
///   approve   - a person approves on the Changes page, or gives the code to the client;
///   apply     - backup (show running-config), run the commands live, re-read, log.
/// Saving to flash is its own plan type. Resets are never run.
/// </summary>
public sealed class ChangeManager
{
    private readonly Settings _settings;
    private readonly object _gate = new();
    private readonly Dictionary<string, SemaphoreSlim> _perSbc = new();
    private List<ChangePlan> _plans = [];
    private static string PlansPath => Path.Combine(AppContext.BaseDirectory, "changes.json");
    public static string BackupDir => Path.Combine(AppContext.BaseDirectory, "sbc-backups");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public sealed class ChangePlan
    {
        public string Id { get; set; } = "";
        public string Sbc { get; set; } = "";          // SBC IP (list key)
        public string SbcName { get; set; } = "";
        public string Kind { get; set; } = "change";   // change | save
        public List<string> Commands { get; set; } = [];
        public string Reason { get; set; } = "";
        public string RequestedBy { get; set; } = "";  // "MCP client", "page"
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public string Code { get; set; } = "";         // shown to people only, never returned to the MCP client
        public string Status { get; set; } = "pending"; // pending, applied, failed, rejected, expired
        public string ApprovedVia { get; set; } = "";
        public DateTime? Applied { get; set; }
        public string Backup { get; set; } = "";
        public string Output { get; set; } = "";
        public int Errors { get; set; }
    }

    public ChangeManager(Settings settings)
    {
        _settings = settings;
        try { if (File.Exists(PlansPath)) _plans = JsonSerializer.Deserialize<List<ChangePlan>>(File.ReadAllText(PlansPath)) ?? []; }
        catch (Exception e) { Log.Write($"changes.json could not be read: {e.Message}"); }
    }

    // ------------------------------------------------------------ credentials

    public static string Protect(string s) => s.Length == 0 ? "" : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(s), null, DataProtectionScope.LocalMachine));
    private static string Unprotect(string s) => s.Length == 0 ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(s), null, DataProtectionScope.LocalMachine));

    public SbcEntry Find(string sbc)
    {
        string key = sbc.Trim();
        var e = _settings.Sbcs.FirstOrDefault(x => x.Ip == key || x.Name.Equals(key, StringComparison.OrdinalIgnoreCase) || x.SshHost == key);
        return e ?? throw new InvalidOperationException($"No SBC '{sbc}' in the SBC list. Known: {string.Join(", ", _settings.Sbcs.Select(x => $"{x.Name} ({x.Ip})"))}.");
    }

    private T WithSession<T>(SbcEntry sbc, bool enable, Func<SbcCli, T> work)
    {
        if (!sbc.HasLogin) throw new InvalidOperationException($"No SSH login is set for {sbc.Name}. Add one on the SBCs page in MediantTrace.");
        SemaphoreSlim gate;
        lock (_gate) { if (!_perSbc.TryGetValue(sbc.Ip, out gate!)) _perSbc[sbc.Ip] = gate = new SemaphoreSlim(1, 1); }
        // One session per SBC at a time: the Mediant allows only two CLI sessions per user.
        if (!gate.Wait(TimeSpan.FromSeconds(60))) throw new InvalidOperationException($"{sbc.Name} is busy with another session.");
        try
        {
            using var cli = SbcCli.Connect(sbc.SshTarget, sbc.SshPort, sbc.SshUser, Unprotect(sbc.SshPasswordProtected), Unprotect(sbc.EnablePasswordProtected));
            if (enable) cli.EnsureEnabled();
            return work(cli);
        }
        finally { gate.Release(); }
    }

    // ------------------------------------------------------------ read

    /// <summary>Runs read-only commands (show ...). Anything else is refused.</summary>
    public List<(string command, string output)> Show(string sbc, IEnumerable<string> commands)
    {
        var list = commands.Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
        if (list.Count == 0) throw new InvalidOperationException("No commands given.");
        foreach (string c in list)
            if (SbcCli.Classify(c) != "read" || c.Contains('?'))
                throw new InvalidOperationException($"'{c}' is not a read-only command. Changes go through sbc_plan_change.");
        var entry = Find(sbc);
        return WithSession(entry, true, cli => list.Select(c => (c, cli.Run(c, TimeSpan.FromSeconds(60)))).ToList());
    }

    public string Backup(string sbc)
    {
        var entry = Find(sbc);
        return WithSession(entry, true, cli => TakeBackup(entry, cli));
    }

    private static string TakeBackup(SbcEntry sbc, SbcCli cli)
    {
        string config = cli.Run("show running-config", TimeSpan.FromSeconds(120));
        if (!config.Contains("configure", StringComparison.Ordinal)) throw new InvalidOperationException("Backup failed: show running-config returned no configuration.");
        Directory.CreateDirectory(BackupDir);
        string path = Path.Combine(BackupDir, $"{sbc.Ip}-{DateTime.Now:yyyyMMdd-HHmmss}-running-config.txt");
        File.WriteAllText(path, config);
        return path;
    }

    // ------------------------------------------------------------ plan / approve / apply

    public ChangePlan Plan(string sbc, IEnumerable<string> commands, string reason, string requestedBy, string kind = "change")
    {
        var entry = Find(sbc);
        if (!entry.HasLogin) throw new InvalidOperationException($"No SSH login is set for {entry.Name}. Add one on the SBCs page first.");
        var list = kind == "save" ? ["write"] : commands.Select(c => c.Trim()).Where(c => c.Length > 0 && !c.StartsWith('#')).ToList();
        if (list.Count == 0) throw new InvalidOperationException("No commands given.");
        if (list.Count > 200) throw new InvalidOperationException("Too many commands in one plan (200 at most).");
        if (kind != "save")
            foreach (string c in list)
            {
                string k = SbcCli.Classify(c);
                if (k == "reset") throw new InvalidOperationException($"'{c}' resets or replaces configuration; MediantTrace never runs that.");
                if (k == "save") throw new InvalidOperationException($"'{c}' saves to flash; plan the save separately with sbc_plan_save.");
                if (c.Contains('?')) throw new InvalidOperationException($"'{c}' asks for help; use sbc_show to explore instead.");
            }
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Give a reason for the change.");
        var plan = new ChangePlan
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant(),
            Sbc = entry.Ip, SbcName = entry.Name, Kind = kind, Commands = list, Reason = reason.Trim(), RequestedBy = requestedBy,
            Created = DateTime.UtcNow, Expires = DateTime.UtcNow.AddMinutes(30),
            Code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(),
        };
        lock (_gate) { _plans.Add(plan); if (_plans.Count > 500) _plans.RemoveAt(0); Save(); }
        Log.Write($"Change plan {plan.Id} for {entry.Name} ({kind}, {list.Count} commands) waiting for approval: {plan.Reason}");
        return plan;
    }

    /// <summary>Applies an approved plan. Approval is either the page (a person) or the one-time code a person read from it.</summary>
    public ChangePlan Apply(string planId, string? code, string via)
    {
        ChangePlan plan;
        lock (_gate)
        {
            plan = _plans.FirstOrDefault(p => p.Id == planId) ?? throw new InvalidOperationException($"No change plan '{planId}'.");
            if (plan.Status != "pending") throw new InvalidOperationException($"Plan {plan.Id} is {plan.Status}, not waiting for approval.");
            if (DateTime.UtcNow > plan.Expires) { plan.Status = "expired"; Save(); throw new InvalidOperationException($"Plan {plan.Id} expired; make a new plan."); }
            if (via != "page" && !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code ?? ""), Encoding.UTF8.GetBytes(plan.Code)))
                throw new InvalidOperationException("The approval code is wrong. A person must read it from the Changes page in MediantTrace.");
            plan.Status = "applying";
            plan.ApprovedVia = via;
            Save();
        }
        var entry = Find(plan.Sbc);
        try
        {
            WithSession(entry, true, cli =>
            {
                plan.Backup = TakeBackup(entry, cli);
                var sb = new StringBuilder();
                foreach (string c in plan.Commands)
                {
                    string output = cli.Run(c, TimeSpan.FromSeconds(60));
                    sb.Append("### ").AppendLine(c);
                    if (output.Trim().Length > 0) sb.AppendLine(output.Trim());
                    if (SbcCli.ErrorLine().IsMatch(output)) plan.Errors++;
                }
                plan.Output = sb.ToString();
                return 0;
            });
            plan.Status = plan.Errors == 0 ? "applied" : "failed";
        }
        catch (Exception e)
        {
            plan.Status = "failed";
            plan.Output += $"\n[{e.Message}]";
        }
        plan.Applied = DateTime.UtcNow;
        lock (_gate) Save();
        Log.Write($"Change plan {plan.Id} for {plan.SbcName}: {plan.Status} (approved via {via}; backup {Path.GetFileName(plan.Backup)})");
        return plan;
    }

    public ChangePlan Reject(string planId)
    {
        lock (_gate)
        {
            var plan = _plans.FirstOrDefault(p => p.Id == planId) ?? throw new InvalidOperationException($"No change plan '{planId}'.");
            if (plan.Status != "pending") throw new InvalidOperationException($"Plan {plan.Id} is {plan.Status}.");
            plan.Status = "rejected";
            Save();
            Log.Write($"Change plan {plan.Id} for {plan.SbcName} rejected");
            return plan;
        }
    }

    public List<ChangePlan> Plans()
    {
        lock (_gate)
        {
            foreach (var p in _plans.Where(p => p.Status == "pending" && DateTime.UtcNow > p.Expires)) p.Status = "expired";
            return _plans.AsEnumerable().Reverse().ToList();
        }
    }

    /// <summary>A plan as an MCP client may see it: never the approval code.</summary>
    public static object PublicView(ChangePlan p) => new
    {
        p.Id, sbc = p.SbcName, sbcIp = p.Sbc, p.Kind, p.Commands, p.Reason, p.Status, p.Created, p.Expires, p.ApprovedVia, p.Applied,
        backup = p.Backup.Length > 0 ? Path.GetFileName(p.Backup) : "", p.Errors, output = p.Output,
    };

    private void Save()
    {
        try { File.WriteAllText(PlansPath, JsonSerializer.Serialize(_plans, Json)); }
        catch (Exception e) { Log.Write($"changes.json could not be saved: {e.Message}"); }
    }
}
