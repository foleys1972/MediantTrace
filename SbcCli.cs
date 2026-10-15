using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace MediantTrace;

/// <summary>
/// One SSH CLI session to an AudioCodes SBC (Mediant): finds the prompt, pages
/// through "--More--", enters privileged mode, and runs commands. Ported from
/// the mediant-sbc skill's mediant.py. Commands are classified first; saving
/// and resets are refused unless the caller asks for them explicitly.
/// </summary>
public sealed partial class SbcCli : IDisposable
{
    [GeneratedRegex(@"(?:^|\n)([^\r\n]{1,80}?)(\([^)\r\n]*\))?\*?[>#] ?$")]
    private static partial Regex GenericPrompt();

    [GeneratedRegex(@"-+ ?more ?-+", RegexOptions.IgnoreCase)]
    private static partial Regex More();

    [GeneratedRegex(@"(?i)password: ?$")]
    private static partial Regex PasswordPrompt();

    [GeneratedRegex(@"(?im)^\s*(%|error|invalid|incomplete command|unknown command)")]
    public static partial Regex ErrorLine();

    [GeneratedRegex(@"^\s*(do\s+)?(show|ping|traceroute|nslookup|exit|end|enable|disable|terminal|clear\s+screen|history|help)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ReadOnly();

    [GeneratedRegex(@"^\s*(do\s+)?(write|write-and-backup)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SaveCmd();

    [GeneratedRegex(@"^\s*(do\s+)?(reload|copy|erase|write-factory|write\s+factory)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ResetCmd();

    /// <summary>"read", "change", "save" or "reset" for one CLI line.</summary>
    public static string Classify(string command)
    {
        if (ResetCmd().IsMatch(command)) return "reset";
        if (SaveCmd().IsMatch(command)) return "save";
        if (ReadOnly().IsMatch(command) || command.TrimEnd().EndsWith('?')) return "read";
        return "change";
    }

    private static readonly string KnownHostsPath = Path.Combine(AppContext.BaseDirectory, "sbc-known-hosts.txt");
    private static readonly object KnownHostsGate = new();

    private readonly SshClient _ssh;
    private readonly ShellStream _shell;
    private readonly string _password;
    private readonly string _enablePassword;
    private Regex _prompt;
    public string Hostname { get; }

    private SbcCli(SshClient ssh, ShellStream shell, string password, string enablePassword, string hostname)
    {
        _ssh = ssh; _shell = shell; _password = password; _enablePassword = enablePassword; Hostname = hostname;
        _prompt = new Regex(@"(?:^|\n)" + Regex.Escape(hostname) + @"(\([^)\r\n]*\))?\*?[>#] ?$");
    }

    public static SbcCli Connect(string host, int port, string user, string password, string enablePassword)
    {
        var info = new ConnectionInfo(host, port, user, new PasswordAuthenticationMethod(user, password)) { Timeout = TimeSpan.FromSeconds(20) };
        var ssh = new SshClient(info);
        // Trust on first use: remember each SBC's host key; refuse a different one later.
        ssh.HostKeyReceived += (_, e) =>
        {
            string fp = e.FingerPrintSHA256;
            lock (KnownHostsGate)
            {
                var known = File.Exists(KnownHostsPath) ? File.ReadAllLines(KnownHostsPath) : [];
                string? line = known.FirstOrDefault(l => l.StartsWith(host + ":" + port + " ", StringComparison.Ordinal));
                if (line is null) { File.AppendAllText(KnownHostsPath, $"{host}:{port} {fp}{Environment.NewLine}"); e.CanTrust = true; }
                else e.CanTrust = line.EndsWith(" " + fp, StringComparison.Ordinal);
            }
        };
        Exception? last = null;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            try { ssh.Connect(); last = null; break; }
            catch (Renci.SshNet.Common.SshConnectionException e) when (e.Message.Contains("Key exchange", StringComparison.OrdinalIgnoreCase) || e.Message.Contains("host key", StringComparison.OrdinalIgnoreCase))
            { ssh.Dispose(); throw new InvalidOperationException($"The SBC's SSH host key does not match the one recorded for {host}. If the SBC was replaced, remove its line from {KnownHostsPath}."); }
            catch (Exception e) { last = e; Thread.Sleep(3000); } // the Mediant briefly refuses after several sessions in a row
        }
        if (last is not null) { ssh.Dispose(); throw new InvalidOperationException($"Could not connect to {host}:{port}: {last.Message} (SBC down, wrong login, or both CLI sessions for this user in use?)"); }

        var shell = ssh.CreateShellStream("vt100", 250, 1000, 0, 0, 65536);
        string banner = ReadUntil(shell, GenericPrompt(), TimeSpan.FromSeconds(20)).Replace("\r", "");
        var m = GenericPrompt().Match(banner.TrimEnd('\n'));
        if (!m.Success) { shell.Dispose(); ssh.Dispose(); throw new InvalidOperationException("Connected, but no CLI prompt was seen."); }
        return new SbcCli(ssh, shell, password, enablePassword, m.Groups[1].Value.Trim().TrimEnd('*').Trim());
    }

    private static string ReadUntil(ShellStream shell, Regex prompt, TimeSpan timeout)
    {
        var buf = new StringBuilder();
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            string? data = shell.DataAvailable ? shell.Read() : null;
            if (!string.IsNullOrEmpty(data))
            {
                buf.Append(data);
                string tail = buf.Length > 200 ? buf.ToString(buf.Length - 200, 200) : buf.ToString();
                if (More().IsMatch(tail))
                {
                    string all = More().Replace(buf.ToString(), "");
                    buf.Clear().Append(all);
                    shell.Write(" "); shell.Flush();
                    deadline = DateTime.UtcNow + timeout;
                    continue;
                }
                string plain = tail.Replace("\r", "").TrimEnd('\n');
                if (prompt.IsMatch(plain) || PasswordPrompt().IsMatch(plain.TrimEnd())) return buf.ToString();
            }
            else Thread.Sleep(80);
        }
        return buf + "\n[timed out waiting for the CLI prompt]";
    }

    /// <summary>Runs one line and returns its output without the echo and the prompt.</summary>
    public string Run(string command, TimeSpan? timeout = null)
    {
        _shell.Write(command + "\r"); // the CLI takes Enter as CR, as PuTTY sends it
        _shell.Flush();
        string output = ReadUntil(_shell, _prompt, timeout ?? TimeSpan.FromSeconds(30));
        if (PasswordPrompt().IsMatch(output.Replace("\r", "").TrimEnd()))
        {
            _shell.Write((_enablePassword.Length > 0 ? _enablePassword : _password) + "\r");
            _shell.Flush();
            output += "[password sent]\n" + ReadUntil(_shell, _prompt, timeout ?? TimeSpan.FromSeconds(30));
        }
        return Clean(output, command);
    }

    public void EnsureEnabled()
    {
        string out1 = Run("enable");
        // After enable the prompt ends in '#'. Re-check with an empty line, which only prints the prompt.
        _shell.Write("\r"); _shell.Flush();
        string p = ReadUntil(_shell, _prompt, TimeSpan.FromSeconds(10)).Replace("\r", "").TrimEnd('\n', ' ');
        if (!p.EndsWith('#')) throw new InvalidOperationException("Could not enter privileged mode (enable): check the enable password." + (out1.Length > 0 ? " " + out1.Trim() : ""));
    }

    private string Clean(string output, string command)
    {
        var lines = output.Replace("\r", "").Split('\n').ToList();
        if (lines.Count > 0 && lines[0].Trim() == command.Trim()) lines.RemoveAt(0);
        while (lines.Count > 0 && (lines[^1].Trim().Length == 0 || _prompt.IsMatch(lines[^1].TrimEnd()))) lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines);
    }

    public void Dispose()
    {
        try { _shell.Dispose(); } catch { }
        try { _ssh.Disconnect(); _ssh.Dispose(); } catch { }
    }
}
