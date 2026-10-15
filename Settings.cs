using System.Text.Json;

namespace MediantTrace;

/// <summary>settings.json next to the program. Missing file or fields fall back to the defaults.</summary>
public sealed class Settings
{
    /// <summary>The SBCs that send to this server: name, address they send from, web link.</summary>
    public List<SbcEntry> Sbcs { get; set; } = [new SbcEntry { Name = "Mediant", Ip = "192.168.1.244", Url = "https://192.168.1.244" }];
    public bool OnlyListedSbcs { get; set; }             // true: ignore packets from addresses not in Sbcs
    public int DebugRecordingPort { get; set; } = 925;
    public int SyslogPort { get; set; } = 514;
    public int WebPort { get; set; } = 8925;
    public bool WebOnNetwork { get; set; }               // false: this PC only (http://localhost)
    public string WebPassword { get; set; } = "";        // asked for when WebOnNetwork is on
    public string TraceFolder { get; set; } = "traces";  // relative to the program's folder
    public int AutoStopMinutes { get; set; } = 60;       // 0 = never
    public int TestSipPort { get; set; } = 5090;         // SIP port the test caller uses
    public int TestRtpPortFirst { get; set; } = 40000;   // RTP ports the test caller uses (even ports)
    public int TestRtpPortCount { get; set; } = 100;
    public string McpToken { get; set; } = "";           // Bearer token for the MCP endpoint (/mcp); made on first run

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public SbcEntry? FindSbc(string ip) => Sbcs.FirstOrDefault(s => s.Ip == ip);

    public string TraceFolderFull => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, TraceFolder));

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception e)
        {
            Log.Write($"settings.json could not be read ({e.Message}); using defaults");
        }
        var s = new Settings();
        s.Save();
        return s;
    }

    public void Save()
    {
        try { File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json)); }
        catch (Exception e) { Log.Write($"settings.json could not be saved: {e.Message}"); }
    }
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

    public static void Write(string text)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {text}";
        lock (Gate)
        {
            Recent.Add(line);
            if (Recent.Count > 200) Recent.RemoveAt(0);
        }
        try { Console.WriteLine(line); } catch { /* no console when running as a service */ }
        try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "mediant-trace.log"), line + Environment.NewLine); } catch { }
    }

    public static List<string> Snapshot()
    {
        lock (Gate) return [.. Recent];
    }
}
