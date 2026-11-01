using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediantTrace;

/// <summary>settings.json next to the program. Missing file or fields fall back to the defaults.</summary>
public sealed class Settings
{
    /// <summary>The SBCs that send to this server: name, address they send from, web link.</summary>
    public List<SbcEntry> Sbcs { get; set; } = [new SbcEntry { Name = "Mediant", Ip = "192.0.2.10", Url = "https://192.0.2.10" }];
    public bool OnlyListedSbcs { get; set; }             // true: ignore packets from addresses not in Sbcs
    public int DebugRecordingPort { get; set; } = 925;
    public int SyslogPort { get; set; } = 514;
    public int WebPort { get; set; } = 8925;
    public bool WebOnNetwork { get; set; }               // false: this PC only (http://localhost)
    public string TraceFolder { get; set; } = "traces";  // relative to the program's folder
    public int AutoStopMinutes { get; set; } = 60;       // 0 = never
    public int RetentionDays { get; set; } = 14;         // delete captures older than this; 0 = keep
    public int RetentionMaxMb { get; set; } = 2048;      // then drop the oldest until captures fit; 0 = no cap
    public string LogFolder { get; set; } = "splunk";    // daily folders of JSON logs, next to the program
    public int LogMaxMb { get; set; } = 10;              // close a log and start another at this size
    public int LogRetentionDays { get; set; } = 14;      // delete daily log folders older than this
    public int LogMaxTotalMb { get; set; }               // then drop the oldest days until the logs fit; 0 = no cap
    public string BackupFolder { get; set; } = "sbc-backups"; // SBC config backups
    public int BackupRetentionDays { get; set; }         // 0 = keep; the newest 10 per SBC are always kept
    public string ProgramLogFolder { get; set; } = "";   // mediant-trace.log; "" = the program's folder
    public int ProgramLogMaxMb { get; set; } = 10;       // then start a new file, keeping 3 older ones
    public int HistoryDays { get; set; } = 90;           // test results, line tests, ready checks, wire changes; 0 = keep
    // Syslog as AudioCodes Syslog Viewer receives it: also over TCP and TLS, reordered, filtered on arrival.
    public int SyslogTcpPort { get; set; }               // 0 = off
    public int SyslogTlsPort { get; set; }               // 0 = off; RFC 5425 uses 6514
    public string SyslogTlsPfx { get; set; } = "";       // "" = a self-signed certificate MediantTrace makes
    [JsonIgnore] public string SyslogTlsPfxPassword { get; set; } = "";
    public string SyslogTlsPfxPasswordProtected { get; set; } = "";
    public bool SyslogReorder { get; set; } = true;      // put [S=n] numbers back in order; count the missing
    public int SyslogBufferLines { get; set; } = 50000;  // syslog lines kept for the page
    public List<string> IgnoreSyslogFrom { get; set; } = [];      // senders whose syslog is dropped on arrival
    public List<string> IgnoreSyslogPatterns { get; set; } = [];  // regular expressions; matching lines are dropped on arrival

    // "Write log": plain text files of every syslog line, rotated by size, as Syslog Viewer writes them.
    public bool WriteLog { get; set; }
    public string WriteLogFolder { get; set; } = "syslog";
    public string WriteLogFile { get; set; } = "syslog.txt";
    public int WriteLogFileMb { get; set; } = 10;        // 0 = one file that grows for ever
    public int WriteLogFileCount { get; set; } = 10;     // 0 = keep every rotated file
    public bool WriteLogPerDevice { get; set; }          // a file per SBC
    public bool WriteLogFolderPerDevice { get; set; }    // a folder per SBC
    public bool WriteLogUseNames { get; set; } = true;   // SBC names (not addresses) in those file and folder names

    public int TestSipPort { get; set; } = 5090;         // SIP port the test caller uses
    public int TestRtpPortFirst { get; set; } = 40000;   // RTP ports the test caller uses (even ports)
    public int TestRtpPortCount { get; set; } = 100;

    // Where to say that a scheduled test has started failing. The webhook address and the mail password are secrets.
    [JsonIgnore] public string AlertWebhook { get; set; } = "";
    [JsonIgnore] public string AlertSmtpPassword { get; set; } = "";
    public string AlertWebhookProtected { get; set; } = "";
    public string AlertSmtpPasswordProtected { get; set; } = "";
    public string AlertEmailTo { get; set; } = "";
    public string AlertEmailFrom { get; set; } = "";
    public string AlertSmtpHost { get; set; } = "";
    public int AlertSmtpPort { get; set; } = 587;
    public string AlertSmtpUser { get; set; } = "";

    /// <summary>People who can open the page. Each password stays encrypted. Empty means the first page asks someone to create the first sign-in.</summary>
    public List<PageAccount> Accounts { get; set; } = [];
    /// <summary>Older settings.json had one username. Read once, then moved into <see cref="Accounts"/>.</summary>
    public string WebUser { get; set; } = "";
    // Held in memory only. On disk these are WebPasswordProtected and McpTokenProtected (DPAPI).
    [JsonIgnore] public string WebPassword { get; set; } = "";
    [JsonIgnore] public string McpToken { get; set; } = "";
    public string WebPasswordProtected { get; set; } = "";
    public string McpTokenProtected { get; set; } = "";
    // Old settings.json wrote these in clear. Read once, then dropped on the next save.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("WebPassword")]
    public string? WebPasswordFile { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("McpToken")]
    public string? McpTokenFile { get; set; }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public SbcEntry? FindSbc(string ip) => Sbcs.FirstOrDefault(s => s.Ip == ip);

    /// <summary>A folder setting as a full path: absolute as given, or relative to the program's folder.</summary>
    public static string FullFolder(string folder, string fallback) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, string.IsNullOrWhiteSpace(folder) ? fallback : folder.Trim()));

    public string TraceFolderFull => FullFolder(TraceFolder, "traces");
    public string LogFolderFull => FullFolder(LogFolder, "splunk");
    public string BackupFolderFull => FullFolder(BackupFolder, "sbc-backups");
    public string ProgramLogFolderFull => FullFolder(ProgramLogFolder, ".");
    public string WriteLogFolderFull => FullFolder(WriteLogFolder, "syslog");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
                if (loaded.OpenSecrets())
                {
                    Log.Write("Page password and MCP token in settings.json are now stored encrypted");
                    loaded.Save();
                }
                return loaded;
            }
        }
        catch (Exception e)
        {
            Log.Write($"settings.json could not be read ({e.Message}); using defaults");
        }
        var s = new Settings();
        s.Save();
        return s;
    }

    /// <summary>Copies secrets into memory. Returns true when the file still holds one in clear and should be rewritten.</summary>
    internal bool OpenSecrets()
    {
        bool rewrite = false;
        WebPassword = Reveal(WebPasswordProtected, WebPasswordFile, ref rewrite);
        McpToken = Reveal(McpTokenProtected, McpTokenFile, ref rewrite);
        AlertWebhook = Reveal(AlertWebhookProtected, null, ref rewrite);
        AlertSmtpPassword = Reveal(AlertSmtpPasswordProtected, null, ref rewrite);
        SyslogTlsPfxPassword = Reveal(SyslogTlsPfxPasswordProtected, null, ref rewrite);
        foreach (PageAccount account in Accounts)
            account.Password = Reveal(account.PasswordProtected, null, ref rewrite);
        if ((WebUser.Length > 0 || WebPassword.Length > 0) && !Accounts.Exists(a => a.User.Equals(WebUser, StringComparison.OrdinalIgnoreCase)))
        {
            Accounts.Insert(0, new PageAccount { User = WebUser, Password = WebPassword });
            rewrite = true;
        }
        if (WebUser.Length > 0 || WebPassword.Length > 0 || WebPasswordProtected.Length > 0)
        {
            WebUser = "";
            WebPassword = "";
            WebPasswordProtected = "";
            rewrite = true;
        }
        WebPasswordFile = null;
        McpTokenFile = null;
        return rewrite;
    }

    private static string Reveal(string protectedValue, string? fileValue, ref bool rewrite)
    {
        if (fileValue is { Length: > 0 }) rewrite = true;
        if (protectedValue.Length > 0)
        {
            try { return ChangeManager.Unprotect(protectedValue); }
            catch (Exception e)
            {
                Log.Write($"A secret in settings.json could not be decrypted ({e.Message}); set it again");
            }
        }
        return fileValue ?? "";
    }

    /// <summary>JSON as it is written to disk: secrets encrypted, never the clear values.</summary>
    internal string Serialize()
    {
        foreach (PageAccount account in Accounts)
            account.PasswordProtected = ChangeManager.Protect(account.Password);
        WebPasswordProtected = "";
        WebUser = "";
        WebPassword = "";
        McpTokenProtected = ChangeManager.Protect(McpToken);
        AlertWebhookProtected = ChangeManager.Protect(AlertWebhook);
        AlertSmtpPasswordProtected = ChangeManager.Protect(AlertSmtpPassword);
        SyslogTlsPfxPasswordProtected = SyslogTlsPfxPassword.Length > 0 ? ChangeManager.Protect(SyslogTlsPfxPassword) : "";
        WebPasswordFile = null;
        McpTokenFile = null;
        return JsonSerializer.Serialize(this, Json);
    }

    public void Save()
    {
        try { File.WriteAllText(FilePath, Serialize()); }
        catch (Exception e) { Log.Write($"settings.json could not be saved: {e.Message}"); }
    }
}

public sealed class PageAccount
{
    public string User { get; set; } = "";
    [JsonIgnore] public string Password { get; set; } = "";
    public string PasswordProtected { get; set; } = "";
}

public sealed class SbcEntry
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Url { get; set; } = "";
    // SSH login for reading and (with approval) changing configuration. Passwords are DPAPI-encrypted.
    public string SshHost { get; set; } = "";     // management address if different from Ip
    public int SshPort { get; set; } = 22;
    public string SshUser { get; set; } = "";
    public string SshPasswordProtected { get; set; } = "";
    public string EnablePasswordProtected { get; set; } = "";

    public bool HasLogin => SshUser.Length > 0 && SshPasswordProtected.Length > 0;
    public string SshTarget => SshHost.Length > 0 ? SshHost : Ip;
}

public static class Log
{
    private static readonly object Gate = new();
    public static readonly List<string> Recent = [];
    private static string _folder = AppContext.BaseDirectory;
    private static Func<long> _maxBytes = () => 10L * 1024 * 1024;

    /// <summary>Where mediant-trace.log goes and how big it may get before it is rotated (3 older files kept).</summary>
    public static void Configure(string folder, Func<long> maxBytes)
    {
        try { Directory.CreateDirectory(folder); } catch { return; }
        lock (Gate) { _folder = folder; _maxBytes = maxBytes; }
    }

    public static string FilePath { get { lock (Gate) return Path.Combine(_folder, "mediant-trace.log"); } }

    public static void Write(string text)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {text}";
        lock (Gate)
        {
            Recent.Add(line);
            if (Recent.Count > 200) Recent.RemoveAt(0);
        }
        try { Console.WriteLine(line); } catch { /* no console when running as a service */ }
        lock (Gate)
        {
            try
            {
                string path = Path.Combine(_folder, "mediant-trace.log");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > _maxBytes())
                {
                    // mediant-trace.log -> .1.log -> .2.log -> .3.log (the oldest is dropped)
                    for (int n = 3; n >= 1; n--)
                    {
                        string from = n == 1 ? path : Path.Combine(_folder, $"mediant-trace.{n - 1}.log"), to = Path.Combine(_folder, $"mediant-trace.{n}.log");
                        if (File.Exists(from)) File.Move(from, to, true);
                    }
                }
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* the log never stops the program */ }
        }
    }

    public static List<string> Snapshot()
    {
        lock (Gate) return [.. Recent];
    }
}
