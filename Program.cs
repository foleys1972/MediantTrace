using System.Diagnostics;
using System.ServiceProcess;

namespace MediantTrace;

/// <summary>
/// MediantTrace: receives what an AudioCodes Mediant sends out for
/// troubleshooting (Debug Recording on UDP 925, syslog on UDP 514), records
/// it to .pcap and .txt on request, and shows calls, SIP ladders, SDP checks
/// and audio streams on a web page. It only listens on its own ports; it does
/// not sniff the network.
///
///   MediantTrace.exe              run in this window (opens the page)
///   MediantTrace.exe install      install and start as a Windows service (administrator)
///   MediantTrace.exe uninstall    stop and remove the service (administrator)
/// </summary>
public static class Program
{
    private const string ServiceName = "MediantTrace";

    public static int Main(string[] args)
    {
        string command = args.FirstOrDefault()?.ToLowerInvariant() ?? "";
        switch (command)
        {
            case "--service":
                ServiceBase.Run(new TraceService());
                return 0;
            case "install":
                return Install();
            case "selftest":
                return SelfTest();
            case "uninstall":
                return Uninstall();
            case "-h" or "--help" or "/?" or "help":
                Console.WriteLine("""
                    MediantTrace - record AudioCodes Mediant Debug Recording and syslog, and view calls in a browser

                      MediantTrace.exe              run in this window and open the page
                      MediantTrace.exe install      run as a Windows service, always on (as administrator)
                      MediantTrace.exe uninstall    remove the service (as administrator)

                    Settings are in settings.json next to the program (created on first run).
                    """);
                return 0;
            case "":
                return RunConsole();
            default:
                Console.Error.WriteLine($"Unknown command '{args[0]}'. Try MediantTrace.exe help");
                return 2;
        }
    }

    private static int RunConsole()
    {
        using var app = new App();
        if (!app.Start()) return 1;
        Console.WriteLine();
        Console.WriteLine($"Open {app.Url} in a browser. Press Ctrl+C to quit.");
        try { Process.Start(new ProcessStartInfo(app.Url) { UseShellExecute = true }); } catch { /* no browser: the URL is printed */ }

        using var quit = new ManualResetEventSlim();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };
        quit.Wait();
        return 0;
    }

    /// <summary>Checks the codec and MOS maths against known values.</summary>
    private static int SelfTest()
    {
        int failures = 0;
        void Check(bool ok, string what) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}"); if (!ok) failures++; }
        int worstA = 0, worstU = 0;
        for (int v = -32768; v <= 32767; v += 7)
        {
            short x = (short)v;
            int ea = Math.Abs(G711.AlawToLinear(G711.LinearToAlaw(x)) - x);
            int eu = Math.Abs(G711.UlawToLinear(G711.LinearToUlaw(x)) - x);
            // G.711 quantisation error grows with level: allow about 1/16 of the value, at least 32.
            worstA = Math.Max(worstA, ea - Math.Max(32, Math.Abs(v) / 16));
            worstU = Math.Max(worstU, eu - Math.Max(32, Math.Abs(v) / 16));
        }
        Check(worstA <= 0, $"A-law round trip within quantisation error (worst excess {worstA})");
        Check(worstU <= 0, $"mu-law round trip within quantisation error (worst excess {worstU})");
        Check(G711.LinearToAlaw(0) == 0xD5, "A-law silence is 0xD5");
        Check(G711.LinearToUlaw(0) == 0xFF, "mu-law silence is 0xFF");
        double m0 = Quality.Mos(0, 0), m5 = Quality.Mos(5, 10), m20 = Quality.Mos(20, 30);
        Check(m0 is > 4.3 and < 4.5, $"MOS with no loss {m0} (expect about 4.4)");
        Check(m5 is > 3.4 and < 4.0, $"MOS with 5% loss {m5}");
        Check(m20 < m5 && m20 < 3.0, $"MOS with 20% loss {m20} (lower)");
        short[] speech = TestLine.Speak("Mediant Trace speech check.");
        Check(speech.Length > 8000, $"Speech engine produced {speech.Length / 8000.0:0.0} s of audio");
        short[] floor = TestLine.FloorAnnouncement();
        Check(floor.Length > 8000 * 5, $"Voice-team message is {floor.Length / 8000.0:0.0} s (must be over 5)");
        var active = BulkTests.ParseActiveCalls(
            "Total Active Calls: 2\r\n| Call Start Time                 |End Point      |     Caller         |     Callee         |Direction|  Remote IP    |Duration|Call State|   Session ID   \r\n" +
            "=====\r\n|08:27:01.154  UTC Wed Oct 14 2026|PRI-1/1/21     |9911                |9912                |Incoming |192.168.1.58   |00:17:26|Connected |a89b7e:109:2945 \r\n" +
            "|08:27:01.214  UTC Wed Oct 14 2026|PRI-2/2/21     |9911                |9912                |Outgoing |192.168.1.58   |00:17:26|Connected |a89b7e:109:2857 \r\n");
        Check(active.Count == 2 && active[0].Callee == "9912" && active[0].State == "Connected" && active[1].Session == "a89b7e:109:2857",
            $"SBC active call list parsed ({active.Count} calls)");
        var names = ConfigNames.Parse("""
            configure network
             interface network-if 0
              ip-address 192.168.1.244
              name "LAN_IF"
              activate
             exit
             interface network-if 1
              ip-address 192.168.1.242
              name "WAN_IF"
              activate
             exit
            exit
            configure voip
             proxy-set 1
              proxy-name "IP-PBX"
              sbcipv4-sip-int-name "sipInterface1"
              activate
             exit
             proxy-set 2
              proxy-name "ITSP"
              activate
             exit
             proxy-ip 1/0
              proxy-address "192.168.1.58:5060"
              activate
             exit
             proxy-ip 2/0
              proxy-address "192.0.2.10:5060"
              activate
             exit
             gateway routing tel2ip-routing 0
              route-name "MediantTrace echo test"
              dst-ip-address "192.168.1.58"
              dst-port 5090
             exit
             gateway routing tel2ip-routing 2
              route-name "Somewhere else"
              dst-ip-address "10.9.9.9"
              dst-port 5060
              activate
             exit
            exit
            """);
        PeerName? named(string ip) => names.FirstOrDefault(n => n.Ip == ip);
        Check(named("192.168.1.58") is { Name: "IP-PBX", Kind: "proxy" }, "Proxy address keeps the proxy-set name");
        Check(named("192.0.2.10")?.Name == "ITSP", "Second proxy set");
        Check(named("192.168.1.244")?.Name == "LAN_IF" && named("192.168.1.242")?.Name == "WAN_IF", "Network interface names");
        Check(named("10.9.9.9") is { Name: "Somewhere else", Kind: "route" }, "A route name is used when no proxy set has that address");
        Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
        return failures == 0 ? 0 : 1;
    }

    private static int Install()
    {
        string exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MediantTrace.exe");
        var settings = Settings.Load();
        bool ok =
            Run("sc.exe", $"create {ServiceName} binPath= \"\\\"{exe}\\\" --service\" start= auto DisplayName= \"Mediant Trace\"") &&
            Run("sc.exe", $"description {ServiceName} \"Records AudioCodes Mediant syslog and Debug Recording; web page on port {settings.WebPort}\"") &&
            Run("sc.exe", $"failure {ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");
        if (!ok)
        {
            Console.Error.WriteLine("Could not install the service. Run this from a command prompt opened as administrator.");
            return 1;
        }
        // Let the Mediant's packets in (and the page, if it is open to the network).
        // Only from the listed SBCs, when there are any.
        string remote = settings.Sbcs.Count > 0 ? $" remoteip={string.Join(",", settings.Sbcs.Select(x => x.Ip))}" : "";
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (UDP)\"");
        Run("netsh", $"advfirewall firewall add rule name=\"Mediant Trace (UDP)\" dir=in action=allow program=\"{exe}\" protocol=UDP localport={settings.SyslogPort},{settings.DebugRecordingPort}{remote}");
        if (settings.WebOnNetwork)
            Run("netsh", $"advfirewall firewall add rule name=\"Mediant Trace (web)\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={settings.WebPort}");
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (test calls)\"");
        Run("netsh", $"advfirewall firewall add rule name=\"Mediant Trace (test calls)\" dir=in action=allow program=\"{exe}\" protocol=UDP localport={settings.TestSipPort},{settings.TestRtpPortFirst}-{settings.TestRtpPortFirst + settings.TestRtpPortCount - 1}");
        Run("sc.exe", $"start {ServiceName}");
        Console.WriteLine($"Installed. The page is at http://localhost:{settings.WebPort}/");
        return 0;
    }

    private static int Uninstall()
    {
        Run("sc.exe", $"stop {ServiceName}");
        bool ok = Run("sc.exe", $"delete {ServiceName}");
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (UDP)\"");
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (web)\"");
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (test calls)\"");
        Console.WriteLine(ok ? "Removed." : "Could not remove the service (run as administrator?).");
        return ok ? 0 : 1;
    }

    private static bool Run(string file, string arguments)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(file, arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) Console.Error.WriteLine($"{file} {arguments}\n  {output.Trim()}");
            return p.ExitCode == 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{file}: {e.Message}");
            return false;
        }
    }
}

/// <summary>The recorder and the web page together.</summary>
public sealed class App : IDisposable
{
    private readonly Settings _settings = Settings.Load();
    private Recorder? _recorder;
    private SipTester? _tester;
    private ChangeManager? _changes;
    private WebServer? _web;

    public string Url => _web?.Url ?? "";

    public bool Start()
    {
        Log.Write($"MediantTrace {typeof(App).Assembly.GetName().Version?.ToString(3)} starting; settings in {Settings.FilePath}");
        _recorder = new Recorder(_settings);
        _recorder.Open();
        _tester = new SipTester(_settings);
        _tester.Open();
        if (_settings.McpToken.Length == 0)
        {
            _settings.McpToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            _settings.Save();
        }
        _changes = new ChangeManager(_settings);
        var bulk = new BulkTests(_tester, _recorder, _changes, _settings);
        var mcp = new McpServer(_settings, _recorder, _tester, _changes, bulk);
        _web = new WebServer(_settings, _recorder, _tester, _changes, mcp, bulk);
        return _web.Start();
    }

    public void Dispose()
    {
        _web?.Dispose();
        _tester?.Dispose();
        _recorder?.Dispose();
        Log.Write("MediantTrace stopped");
    }
}

public sealed class TraceService : ServiceBase
{
    private App? _app;

    public TraceService() => ServiceName = "MediantTrace";

    protected override void OnStart(string[] args)
    {
        _app = new App();
        if (!_app.Start()) Log.Write("Web page did not start; see the messages above");
    }

    protected override void OnStop()
    {
        _app?.Dispose();
        _app = null;
    }
}
