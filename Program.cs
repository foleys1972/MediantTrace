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
        // --no-browser: run without opening the page (scripts, remote sessions).
        bool browser = !args.Contains("--no-browser", StringComparer.OrdinalIgnoreCase);
        args = args.Where(a => !a.Equals("--no-browser", StringComparison.OrdinalIgnoreCase)).ToArray();
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
            case "configdiff" when args.Length == 3:
                {
                    // Compare two saved running-config files, as the upgrade page does (secrets masked).
                    string[] Read(string f) => File.ReadAllLines(f).Select(Upgrades.CleanLine).Where(l => l.Length > 0).ToArray();
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var hunks = Upgrades.DiffHunks(Read(args[1]), Read(args[2]));
                    foreach (var h in hunks)
                    {
                        Console.WriteLine($"@@ {h.Section}");
                        foreach (var (op, text) in h.Lines) Console.WriteLine($"{op} {Upgrades.Mask(text)}");
                    }
                    Console.WriteLine($"{hunks.Count} place(s) differ; {watch.ElapsedMilliseconds} ms");
                    return 0;
                }
            case "uninstall":
                return Uninstall();
            case "-h" or "--help" or "/?" or "help":
                Console.WriteLine("""
                    MediantTrace - record AudioCodes Mediant Debug Recording and syslog, and view calls in a browser

                      MediantTrace.exe              run in this window and open the page
                      MediantTrace.exe install      run as a Windows service, always on (as administrator)
                      MediantTrace.exe uninstall    remove the service (as administrator)
                      MediantTrace.exe <file>       run and open a log or capture on the page (Syslog Viewer, acSyslog,
                                                    CSV, TSV, Excel, JSON, SL4NT, web format, .pcap, .pcapng, MediantTrace .txt)
                      MediantTrace.exe open <file>  the same
                      MediantTrace.exe monitor <file>  run and follow a log file as it grows
                      --no-browser                  with any of the above: do not open the page

                    Settings are in settings.json next to the program (created on first run).
                    """);
                return 0;
            case "":
                return RunConsole(browser: browser);
            case "open" when args.Length == 2:
                return RunConsole(open: args[1], browser: browser);
            case "monitor" or "--monitor" when args.Length == 2:
                return RunConsole(follow: args[1], browser: browser);
            case var _ when args.Length == 1 && File.Exists(args[0]):
                return RunConsole(open: args[0], browser: browser);
            default:
                Console.Error.WriteLine($"Unknown command '{args[0]}'. Try MediantTrace.exe help");
                return 2;
        }
    }

    private static int RunConsole(string? open = null, string? follow = null, bool browser = true)
    {
        using var app = new App();
        if (!app.Start()) return 1;
        if (open is not null)
        {
            var (_, message) = app.Recorder!.Import(Path.GetFullPath(open), Path.GetFileName(open));
            Console.WriteLine(message);
        }
        if (follow is not null)
        {
            var (_, message) = app.Recorder!.Watch(Path.GetFullPath(follow));
            Console.WriteLine(message);
        }
        Console.WriteLine();
        Console.WriteLine($"Open {app.Url} in a browser. Press Ctrl+C to quit.");
        if (browser)
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
            "=====\r\n|08:27:01.154  UTC Wed Oct 14 2026|PRI-1/1/21     |9911                |9912                |Incoming |192.0.2.58   |00:17:26|Connected |a89b7e:109:2945 \r\n" +
            "|08:27:01.214  UTC Wed Oct 14 2026|PRI-2/2/21     |9911                |9912                |Outgoing |192.0.2.58   |00:17:26|Connected |a89b7e:109:2857 \r\n");
        Check(active.Count == 2 && active[0].Callee == "9912" && active[0].State == "Connected" && active[1].Session == "a89b7e:109:2857",
            $"SBC active call list parsed ({active.Count} calls)");
        {
            // The call view runs all the time: a full stream table makes room by dropping quiet streams.
            var cs = new CallStore();
            var rtp = new byte[172]; rtp[0] = 0x80; rtp[1] = 8;
            DateTime old = DateTime.UtcNow.AddHours(-1);
            for (int i = 0; i < 8000; i++) cs.AddRtp(old, 0, false, "10.0.0.1", 10000 + i % 50000, "10.0.0.2", 20000, true, rtp, "sbc", "");
            cs.AddRtp(DateTime.UtcNow, 0, false, "10.0.0.9", 4000, "10.0.0.2", 20000, true, rtp, "sbc", "a:1:2");
            var legs = cs.SessionMedia("sbc", ["a:1:2"]);
            Check(legs.Count == 1 && legs[0].ToSbc == 1, "A new stream is kept when the stream table is full (oldest quiet streams dropped)");
        }
        {
            // Real Mediant 800B output (7.20A).
            var alarms = ReadyCheck.ParseAlarms(
                "Seq. Source                         Severity   Date                   Description\r\n" +
                "---------------------------------------------------------------------------------\r\n" +
                "  3. Board#1/IPGroup#1              major      18.10.2026 , 21:19:21  IP Group is temporarily blocked. IPGroup(IP-PBX) Blocked Reason: Keepalive\r\n" +
                "  6. Board#1/EthernetLink#2         minor      19.10.2026 , 20:46:33  Ethernet link alarm. LAN port number 2 is down.\r\n");
            Check(alarms.Count == 2 && alarms[0].Severity == "major" && alarms[0].Source == "Board#1/IPGroup#1" && alarms[0].Date == "18.10.2026 21:19:21"
                  && alarms[1].Description == "Ethernet link alarm. LAN port number 2 is down.", $"SBC active alarms parsed ({alarms.Count})");
            var trunks = ReadyCheck.ParseTrunks(
                " show voip interface e1-t1 1/1\r\n-------------------------------\r\n       module/port:       1/1\r\n       trunk number:      0\r\n" +
                "       state:             active\r\n       alarm status:      LOS 0,  LOF 0,  RAI 0,  AIS 0,  RAI_CRC 0\r\n       d-channel status:  established\r\n" +
                " show voip interface e1-t1 2/1\r\n-------------------------------\r\n       module/port:       2/1\r\n       state:             active\r\n" +
                "       alarm status:      LOS 1,  LOF 0,  RAI 0,  AIS 0,  RAI_CRC 0\r\n       d-channel status:  not established\r\n");
            var proxies = SbcHealth.ParseProxySets(
                "    Active Proxy Sets Status     \r\n  ID     MODE    KEEP ALIVE       ADDRESS                                     PRIORITY WEIGHT SUCCESS COUNT  FAILED COUNT           STATUS\r\n" +
                "   0    None   Disabled                                                                                                           NOT RESOLVED\r\n" +
                "   1    Homing   Enabled                                                                                                           OFFLINE\r\n" +
                "                            192.0.2.58                                          --     --     0              352                    \r\n" +
                "   2    Homing   Disabled                                                                                                           ONLINE\r\n" +
                "                            192.0.2.10(*)                                          --     --     0              0                     \r\n");
            Check(proxies.Count == 3 && proxies[1].KeepAlive && proxies[1].Status == "OFFLINE" && proxies[1].Addresses.Single().Failed == 352
                  && proxies[0].Status == "NOT RESOLVED" && proxies[2].Status == "ONLINE", $"SBC proxy set status parsed ({proxies.Count})");
            var health = SbcHealth.Parse(c => c switch
            {
                "show version" => "\r\nVersion info:\r\n;Board: Mediant 800B\r\n;Serial Number: 12345678\r\n;Software Version: 7.20A.104.001\r\n;Num of DSP Cores: 3  Num DSP Channels: 150\r\n" +
                                  ";SBC Sessions Capability:;Local License: 0 SBC Sessions (up to 60);Pool License: 0 SBC Sessions;Total (Actual): 0 SBC Sessions (up to 60);;Key features:",
                "show system uptime" => "Uptime: 0 days, 1 hours, 29 minutes, 12 seconds",
                "show system utilization" => "CPUs utilization: Data 0% Voice 25% \r\nCPUs Used Memory: Data 0% Voice 50% \r\n",
                "show voip proxy sets status" => "   1    Homing   Enabled                                       OFFLINE\r\n                            192.0.2.58          --     --     0              352\r\n",
                _ => "",
            });
            Check(health.Version == "7.20A.104.001" && health.Serial == "12345678" && health.SbcSessions.StartsWith("0 SBC Sessions") && health.Cpu == 25 && health.Memory == 50
                  && health.UptimeHours is > 1.4 and < 1.6 && health.State == "bad" && health.Findings[0].StartsWith("Proxy set 1"), "SBC health report built from show output");
            string[] before = ["configure voip", "proxy-set 1", "proxy-name \"PBX\"", "activate", "exit", "sip-interface 0", "network-interface \"LAN\"", "exit"];
            string[] after = ["configure voip", "proxy-set 1", "proxy-name \"PBX2\"", "activate", "exit", "sip-interface 0", "network-interface \"LAN\"", "tls-mutual-auth disable", "exit"];
            var hunks = Upgrades.DiffHunks(before, after);
            var rebuilt = new List<string>();
            int bi = 0;
            foreach (var h in hunks) foreach (var l in h.Lines) if (l.op != '-') rebuilt.Add(l.text);
            var ops = hunks.SelectMany(h => h.Lines).Where(l => l.op != ' ').Select(l => $"{l.op}{l.text}").ToList();
            Check(ops.SequenceEqual(["-proxy-name \"PBX\"", "+proxy-name \"PBX2\"", "+tls-mutual-auth disable"]) && hunks[0].Section == "configure voip › proxy-set 1",
                $"Config diff finds the changed and added lines ({string.Join(" ", ops)})");
            Check(Upgrades.DiffHunks(before, before).Count == 0 && Upgrades.DiffHunks([], ["a"]).Single().Lines.Single().op == '+', "Config diff: no change, and all added");
            Check(Upgrades.Mask("password 0 s3cret").Contains("******") && !Upgrades.Mask("snmp community-string private").Contains("private ") && Upgrades.Mask("proxy-name \"PBX\"") == "proxy-name \"PBX\"",
                "Config lines with passwords or keys are masked for display");
            Check(Settings.FullFolder(@"D:\MT\captures", "traces") == @"D:\MT\captures"
                  && Settings.FullFolder("", "traces") == Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "traces"))
                  && Settings.FullFolder(@"\\server\share\mt", "x") == @"\\server\share\mt", "Data folders: full paths, shares, and names next to the program");
            {
                string root = Path.Combine(Path.GetTempPath(), "mt-selftest-" + Guid.NewGuid().ToString("n")[..8]);
                foreach (int ago in new[] { 3, 2, 1, 0 })
                {
                    string d = Path.Combine(root, DateTime.Now.Date.AddDays(-ago).ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(d);
                    File.WriteAllBytes(Path.Combine(d, "mediant.log"), new byte[400_000]);
                }
                int gone = SplunkLog.PruneToSize(root, 1_000_000);   // 1.6 MB of logs, 1 MB allowed
                var left = Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(x => x).ToList();
                Check(gone == 2 && left.Count == 2 && left[^1] == DateTime.Now.ToString("yyyy-MM-dd"), $"SBC logs over the size cap lose their oldest days first ({gone} removed)");
                Directory.Delete(root, true);
            }
            Check(SbcHealth.ClockOffset("22:15:01", new DateTime(2026, 10, 19, 21, 15, 0)) == 60 && SbcHealth.ClockOffset("00:05:00", new DateTime(2026, 10, 19, 23, 50, 0)) == 15
                  && SbcHealth.ClockOffset("12:00:00", new DateTime(2026, 10, 19, 12, 3, 0)) == 0, "SBC clock offset found, across midnight too");
            var alarmAt = SbcHealth.AlarmUtc("19.10.2026 , 20:46:33".Replace(" , ", " "), 60);
            Check(alarmAt is DateTime aa && aa.ToLocalTime() == new DateTime(2026, 10, 19, 19, 46, 33), "SBC alarm time moved onto this PC's clock");
            Check(trunks.Count == 2 && trunks[0].AlarmsClear && trunks[0].DChannel == "established" && !trunks[1].AlarmsClear && trunks[1].DChannel == "not established",
                $"SBC E1 trunk status parsed ({trunks.Count})");
        }
        var names = ConfigNames.Parse("""
            configure network
             interface network-if 0
              ip-address 192.0.2.100
              name "LAN_IF"
              activate
             exit
             interface network-if 1
              ip-address 192.0.2.101
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
              proxy-address "192.0.2.58:5060"
              activate
             exit
             proxy-ip 2/0
              proxy-address "192.0.2.10:5060"
              activate
             exit
             gateway routing tel2ip-routing 0
              route-name "MediantTrace echo test"
              dst-ip-address "192.0.2.58"
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
        Check(named("192.0.2.58") is { Name: "IP-PBX", Kind: "proxy" }, "Proxy address keeps the proxy-set name");
        Check(named("192.0.2.10")?.Name == "ITSP", "Second proxy set");
        Check(named("192.0.2.100")?.Name == "LAN_IF" && named("192.0.2.101")?.Name == "WAN_IF", "Network interface names");
        Check(named("10.9.9.9") is { Name: "Somewhere else", Kind: "route" }, "A route name is used when no proxy set has that address");

        var secrets = new Settings { WebPasswordFile = "s3cret", McpTokenFile = "tok-1" };
        Check(secrets.OpenSecrets() && secrets.Accounts.Count == 1 && secrets.Accounts[0].Password == "s3cret" && secrets.McpToken == "tok-1", "A clear password and token in an old settings file are read");
        string stored = secrets.Serialize();
        Check(!stored.Contains("s3cret") && !stored.Contains("tok-1") && !stored.Contains("\"WebPassword\":") && !stored.Contains("\"McpToken\":"), "settings.json does not contain the password or token in clear");
        var again = System.Text.Json.JsonSerializer.Deserialize<Settings>(stored)!;
        Check(!again.OpenSecrets() && again.Accounts.Count == 1 && again.Accounts[0].Password == "s3cret" && again.McpToken == "tok-1", "Encrypted password and token load back");

        const string stdIp = "203.0.113.9";
        string sample = "[SYSTEM Params]\r\nSyslogServerPort = 514\r\n";
        IniFiles.Save(stdIp, sample);
        Check(IniFiles.Read(stdIp) == sample && IniFiles.List([stdIp]).Count == 1, "A standard INI is stored for an SBC and listed");
        bool refused = false;
        try { IniFiles.Save(stdIp, "not a configuration"); } catch (InvalidOperationException) { refused = true; }
        Check(refused, "A file that is not an INI is not stored");
        bool badIp = false;
        try { IniFiles.FileFor("../secret"); } catch (InvalidOperationException) { badIp = true; }
        Check(badIp, "A standard file name cannot leave the standards folder");
        File.Delete(IniFiles.FileFor(stdIp));
        Check(IniDiff.Count(sample, sample) == 0, "A configuration that matches the standard has no drift");
        Check(IniDiff.Count(sample, sample.Replace("514", "5514", StringComparison.Ordinal)) == 1, "A changed setting counts as drift from the standard");
        string table = "[ \\PROXYIP ]\r\nFORMAT Index = ProxyIp_IpAddress;\r\nPROXYIP 0 = 192.0.2.10;\r\n[ \\PROXYIP ]\r\n";
        Check(IniDiff.Count(table, table.Replace("192.0.2.10", "192.0.2.11", StringComparison.Ordinal)) == 1, "A changed table row counts as drift from the standard");

        string capDir = Path.Combine(Path.GetTempPath(), "mt-captures-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(capDir);
            void touch(string name, int size) => File.WriteAllBytes(Path.Combine(capDir, name), new byte[size]);
            touch("mediant-20260101-000000.pcap", 100);
            touch("mediant-20260101-000000.txt", 10);
            touch("mediant-20261016-120000.pcap", 50);
            touch("mediant-20261016-120000.txt", 5);
            touch("notes.txt", 3);
            var now = new DateTime(2026, 10, 17, 12, 0, 0);
            string oldPcap = Path.Combine(capDir, "mediant-20260101-000000.pcap");
            var kept = CaptureFiles.Prune(capDir, path => path.Equals(oldPcap, StringComparison.OrdinalIgnoreCase), 14, 0, now);
            Check(kept.Count == 0 && File.Exists(oldPcap), "A capture still being recorded is not deleted for age");
            var aged = CaptureFiles.Prune(capDir, _ => false, 14, 0, now);
            Check(aged.Count == 1 && aged[0] == "mediant-20260101-000000"
                && !File.Exists(oldPcap) && !File.Exists(Path.Combine(capDir, "mediant-20260101-000000.txt"))
                && File.Exists(Path.Combine(capDir, "mediant-20261016-120000.pcap"))
                && File.Exists(Path.Combine(capDir, "notes.txt")),
                "Captures older than the limit are deleted; newer captures and other files stay");
            touch("mediant-20261015-120000.pcap", 80);
            var capped = CaptureFiles.Prune(capDir, _ => false, 0, 60, now);
            Check(capped.Count == 1 && capped[0] == "mediant-20261015-120000" && File.Exists(Path.Combine(capDir, "mediant-20261016-120000.pcap")),
                "Past the disk cap, the oldest capture is removed and a newer one stays");
            bool badName = false;
            try { CaptureFiles.Delete(capDir, "../secret", _ => false); } catch (InvalidOperationException) { badName = true; }
            Check(badName, "Deleting a capture cannot leave the trace folder");
            var removed = CaptureFiles.Delete(capDir, "mediant-20261016-120000", _ => false);
            Check(removed.Count == 2 && !File.Exists(Path.Combine(capDir, "mediant-20261016-120000.txt")), "Deleting a capture removes its pcap and its text file");
        }
        finally { try { Directory.Delete(capDir, true); } catch { } }

        string viewDir = Path.Combine(Path.GetTempPath(), "mt-open-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(viewDir);
            var viewSettings = new Settings { TraceFolder = viewDir, SyslogPort = 5514, DebugRecordingPort = 925, RetentionDays = 0, RetentionMaxMb = 0 };
            using var rec = new Recorder(viewSettings);
            const string stem = "mediant-20261017-120000";
            string payload = "<134> ---- Incoming SIP Message from 10.0.0.8:5060 to SIPInterface #0 UDP ----\r\nINVITE sip:100@10.0.0.1 SIP/2.0\r\nVia: SIP/2.0/UDP 10.0.0.8:5060;branch=z9hG4bK1\r\nFrom: <sip:a@10.0.0.8>;tag=1\r\nTo: <sip:b@10.0.0.1>\r\nCall-ID: abc@10.0.0.8\r\nCSeq: 1 INVITE\r\nContent-Length: 0\r\n\r\n";
            using (var w = new PcapWriter(Path.Combine(viewDir, stem + ".pcap")))
                w.Write(DateTime.UtcNow, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.10"), 5000), new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.20"), 5514), System.Text.Encoding.ASCII.GetBytes(payload));
            var (started, _) = rec.Start();
            var denied = rec.OpenSaved(stem);
            Check(started && !denied.ok, "A saved capture cannot be opened while one is recording");
            rec.Stop();
            var opened = rec.OpenSaved(stem);
            Check(opened.ok && rec.SipMessages == 1 && rec.Store.Count == 1 && rec.Opened == stem, "A saved pcap opens back into the call view (" + opened.message + ")");
            const string txtStem = "mediant-20261017-120100";
            File.WriteAllText(Path.Combine(viewDir, txtStem + ".txt"),
                "===== 2026-10-13 19:17:19.774  DR from 192.0.2.10:926  Call-ID call@host\r\nINVITE sip:9901@192.0.2.10 SIP/2.0\r\nFrom: <sip:9902@192.0.2.10>;tag=1\r\nTo: <sip:9901@192.0.2.10>\r\nCall-ID: call@host\r\nCSeq: 1 INVITE\r\nContent-Length: 0\r\n\r\n");
            var fromTxt = rec.OpenSaved(txtStem);
            Check(fromTxt.ok && rec.Store.Count == 1 && rec.SipMessages == 1, "A capture text file opens back into the call view (" + fromTxt.message + ")");
        }
        finally { try { Directory.Delete(viewDir, true); } catch { } }

        string bookFile = Path.Combine(Path.GetTempPath(), "mt-labels-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var book = new LabelBook(bookFile);
            book.SaveManual("192.0.2.1", "192.0.2.50", "Desk", "typed");
            int imported = book.Replace("192.0.2.1", [
                new PeerName { Ip = "192.0.2.50", Name = "From the SBC", Kind = "proxy" },
                new PeerName { Ip = "192.0.2.51", Name = "Carrier", Kind = "proxy" },
            ], DateTime.UtcNow);
            var all = book.All();
            Check(imported == 1 && all.Count == 2 && all.First(n => n.Ip == "192.0.2.50").Name == "Desk" && all.Any(n => n.Name == "Carrier"),
                "A typed address-book name is kept when names are read from the SBC");
            Check(book.Remove("192.0.2.1", "192.0.2.50") && book.All().All(n => n.Ip != "192.0.2.50"), "A typed name can be removed");
            bool badAddr = false;
            try { book.SaveManual("192.0.2.1", "not-an-ip", "X", ""); } catch (InvalidOperationException) { badAddr = true; }
            Check(badAddr, "An address book entry must be an IPv4 address");
            var (saved, skipped) = book.ImportCsv("192.0.2.1", "\uFEFFName;Address;Note\r\nFront desk;192.0.2.70:5060;\"lobby, left\"\r\nFrom file;192.0.2.51;sheet\r\n;192.0.2.80;\r\nNope;not-an-ip;x\r\n");
            var after = book.All();
            var front = after.First(n => n.Ip == "192.0.2.70");
            var carrier = after.First(n => n.Ip == "192.0.2.51");
            Check(saved == 2 && skipped.Count == 2 && front.Name == "Front desk" && front.Kind == "manual" && front.Detail == "lobby, left"
                && carrier.Name == "From file" && carrier.Kind == "manual",
                "A CSV updates matching addresses, keeps a port off the address, and skips bad rows");
            book.Replace("192.0.2.1", [new PeerName { Ip = "192.0.2.70", Name = "From the SBC", Kind = "proxy" }], DateTime.UtcNow);
            Check(book.All().First(n => n.Ip == "192.0.2.70").Name == "Front desk", "A name loaded from a CSV is kept when names are read from the SBC");
            var (plain, _) = book.ImportCsv("192.0.2.1", "Gate,192.0.2.71\n");
            Check(plain == 1 && book.All().Any(n => n.Ip == "192.0.2.71" && n.Name == "Gate") && book.All().Any(n => n.Name == "Front desk"),
                "A CSV without a header is Name, Address, Note, and names not in the file stay");
        }
        finally { try { File.Delete(bookFile); } catch { } }

        var qualityStore = new CallStore();
        var qualityCdr = new CdrDecoder();
        string mediaTitle = "|MediaReportType|EPTyp|SIPCallId|SessionId|Durat|Coder|LocalRtpIp|LocalRtpPort|RemoteRtpIp|RemoteRtpPort|InPackets|OutPackets|LocalPackLoss|RemotePackLoss|RTPdelay|RTPjitter|RemoteRTPjitter|LocalMOSCQ|RemoteMOSCQ|LatchedRtpIp|LatchedRtpPort|Caller|Callee|";
        string mediaRow = "|MEDIA_END|SBC|q@host|7|12|PCMU|192.0.2.20|6000|192.0.2.30|49170|500|480|0|25|18|4|6|43|35|203.0.113.9|49170|100|200|";
        var titled = qualityCdr.Feed("192.0.2.1", mediaTitle, DateTime.UtcNow);
        var media = qualityCdr.Feed("192.0.2.1", mediaRow, DateTime.UtcNow);
        Check(titled is null && media is not null && media.Get("LocalMOSCQ") == "43" && media.Get("LatchedRtpIp") == "203.0.113.9",
            "A MEDIA_END line takes its columns from the SBC's title line");
        qualityStore.AddCdr("192.0.2.1", new CdrDecoder.Record { Kind = "CALL_END", Family = "SBC", Received = DateTime.UtcNow, Fields = [new("SIPCallId", "q@host"), new("Durat", "12")] });
        qualityStore.AddCdr("192.0.2.1", media!);
        var qualityJson = System.Text.Json.JsonSerializer.SerializeToNode(qualityStore.CallDetail(1, DateTime.UtcNow), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
        var qualityChecks = qualityJson["checks"]!.AsArray().Select(c => c!["text"]!.GetValue<string>()).ToList();
        Check(qualityJson["mos"]!.GetValue<double>() == 3.5 && qualityJson["mosSource"]!.GetValue<string>() == "sbc"
            && qualityJson["sbcMedia"]!.AsArray().Count == 1
            && qualityJson["sbcMedia"]![0]!["mosIn"]!.GetValue<double>() == 4.3,
            "Audio quality comes from the SBC's MEDIA_END record when there is no Debug Recording");
        Check(qualityChecks.Any(t => t.Contains("203.0.113.9") && t.Contains("NAT")) && qualityChecks.Any(t => t.Contains("MOS 3.5")),
            "A latched address and a low SBC MOS are reported");

        var gateway = new CdrDecoder.Record
        {
            Kind = "CALL_END", Family = "GW", Received = DateTime.UtcNow,
            Fields = [new("SIPCallId", "gw@host"), new("Durat", "20"), new("Caller", "1"), new("Callee", "2"),
                new("InPackets", "950"), new("OutPackets", "1000"), new("PackLoss", "50"), new("RemotePackLoss", "0"),
                new("RTPjitter", "10"), new("RTPdelay", "20"), new("RtpIp", "192.0.2.40"), new("Port", "4000")],
        };
        qualityStore.AddCdr("192.0.2.1", gateway);
        var gatewayJson = System.Text.Json.JsonSerializer.SerializeToNode(qualityStore.CallDetail(2, DateTime.UtcNow), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
        Check(gatewayJson["mosSource"]!.GetValue<string>() == "counters" && Math.Abs(gatewayJson["mos"]!.GetValue<double>() - Quality.Mos(5, 10)) < 0.001,
            "A gateway call record with no MOS is scored from its packet loss and jitter");

        qualityStore.AddCdr("192.0.2.1", new CdrDecoder.Record { Kind = "CALL_END", Family = "GW", Received = DateTime.UtcNow, Fields = [new("SIPCallId", "none@host"), new("InPackets", "0"), new("OutPackets", "0")] });
        var noneJson = System.Text.Json.JsonSerializer.SerializeToNode(qualityStore.CallDetail(3, DateTime.UtcNow), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
        bool noMos = noneJson?["mos"] is null || noneJson["mos"]!.GetValueKind() == System.Text.Json.JsonValueKind.Null;
        bool noMedia = noneJson?["sbcMedia"]?.AsArray().Count == 0;
        Check(noMos && noMedia, "A call record with no audio is not given a MOS");

        string exportDir = Path.Combine(Path.GetTempPath(), "mt-export-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(exportDir);
            string exportPcap = Path.Combine(exportDir, "mediant-20261017-130000.pcap");
            var at = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
            var sbc = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.8"), 5000);
            var here = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.9"), 5514);
            using (var w = new PcapWriter(exportPcap))
            {
                w.Write(at, sbc, here, System.Text.Encoding.ASCII.GetBytes("INVITE sip:100@host SIP/2.0\r\nCall-ID: keep@host\r\nCSeq: 1 INVITE\r\n\r\n"));
                w.Write(at.AddSeconds(1), sbc, here, System.Text.Encoding.ASCII.GetBytes("INVITE sip:200@host SIP/2.0\r\nCall-ID: drop@host\r\nCSeq: 1 INVITE\r\n\r\n"));
                w.Write(at.AddMinutes(10), sbc, here, System.Text.Encoding.ASCII.GetBytes("Call-ID: keep@host too late"));
            }
            var select = new CallStore.PacketSelect
            {
                Sbc = "192.0.2.8", CallIds = ["keep@host"], From = at.AddSeconds(-5), To = at.AddSeconds(30), FileName = "100-to-200.pcap",
            };
            var kept = CaptureReplay.Pcap(exportPcap).Where(p => CallExport.Belongs(p, select)).ToList();
            string one = Path.Combine(exportDir, "one.pcap");
            File.WriteAllBytes(one, CallExport.Pcap(kept));
            Check(kept.Count == 1 && CaptureReplay.Pcap(one).Count() == 1, "A call's pcap keeps that call's packets and leaves the others out");
        }
        finally { try { Directory.Delete(exportDir, true); } catch { } }

        Check(TestAlerts.Kind("manual", "passed", "failed") is null, "A test run by hand does not send an alert");
        Check(TestAlerts.Kind("scheduled", "passed", "failed") == "failed", "A scheduled test alerts when it starts failing");
        Check(TestAlerts.Kind("scheduled", null, "failed") == "failed", "The first scheduled result alerts when it fails");
        Check(TestAlerts.Kind("scheduled", "failed", "failed") is null, "A scheduled test that is still failing does not alert again");
        Check(TestAlerts.Kind("scheduled", "failed", "warning") is null, "A warning after a failure is not a recovery");
        Check(TestAlerts.Kind("scheduled", "failed", "passed") == "recovered", "A scheduled test alerts when it passes again");
        Check(TestAlerts.Kind("scheduled", "passed", "warning") is null, "A warning does not count as a failure alert");
        Check(!TestAlerts.WebhookOk("http://example.com/hook") && !TestAlerts.WebhookOk("https://user:secret@example.com/hook"), "A webhook must be https and must not carry a password");
        Check(TestAlerts.WebhookOk("https://example.com/hook"), "An https webhook address is accepted");
        var failedCall = new TestResult { Name = "Echo", Target = "9901 via 192.0.2.1:5060", Findings = ["No audio came back"], Started = DateTime.UtcNow, FinalStatus = 408, FinalReason = "Request Timeout" };
        string alertJson = TestAlerts.Payload(failedCall, "failed");
        Check(alertJson.Contains("Echo") && alertJson.Contains("MessageCard") && alertJson.Contains("\"text\""), "The alert names the test for Teams and for a plain webhook");

        string searchDir = Path.Combine(Path.GetTempPath(), "mt-find-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(searchDir);
            File.WriteAllText(Path.Combine(searchDir, "mediant-20261013-191435.txt"),
                "===== 2026-10-13 19:17:19.774  SBC Lab SBC (192.0.2.10)  IN  10.0.0.8:5060 ->   (SYSLOG)  Call-ID keep@host  SID a89b7e:109:42\r\n" +
                "INVITE sip:9911@192.0.2.10 SIP/2.0\r\nFrom: <sip:9912@192.0.2.58>;tag=1\r\nTo: <sip:9911@192.0.2.10>\r\nCall-ID: keep@host\r\nCSeq: 1 INVITE\r\nContent-Length: 0\r\n\r\n");
            File.WriteAllText(Path.Combine(searchDir, "mediant-20261014-100000.txt"),
                "===== 2026-10-14 10:00:00.000  SBC Lab SBC (192.0.2.10)  IN  10.0.0.8:5060 ->   (SYSLOG)  Call-ID other@host  SID other:1\r\n" +
                "INVITE sip:1000@192.0.2.10 SIP/2.0\r\nFrom: <sip:1001@192.0.2.58>;tag=1\r\nTo: <sip:1000@192.0.2.10>\r\nCall-ID: other@host\r\nCSeq: 1 INVITE\r\nContent-Length: 0\r\n\r\n");
            using (var w = new PcapWriter(Path.Combine(searchDir, "mediant-20261015-120000.pcap")))
                w.Write(DateTime.UtcNow, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.10"), 5000), new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.20"), 5514),
                    System.Text.Encoding.ASCII.GetBytes("INVITE sip:5555@host SIP/2.0\r\nFrom: <sip:4444@host>;tag=1\r\nTo: <sip:5555@host>\r\nCall-ID: only-pcap@host\r\nCSeq: 1 INVITE\r\n\r\n"));
            var byNumber = CaptureSearch.Find(searchDir, "9912", out _);
            Check(byNumber.Count == 1 && byNumber[0].Stem == "mediant-20261013-191435" && byNumber[0].To == "9911" && byNumber[0].Session == "a89b7e:109:42", "A number is found in the capture that has it");
            var bySid = CaptureSearch.Find(searchDir, "a89b7e:109:42", out _);
            Check(bySid.Count == 1 && bySid[0].CallId == "keep@host", "A session id is found in the capture that has it");
            var byCall = CaptureSearch.Find(searchDir, "only-pcap@host", out _);
            Check(byCall.Count == 1 && byCall[0].From == "4444" && byCall[0].Stem == "mediant-20261015-120000", "A Call-ID in a pcap with no text file is found");
            File.WriteAllText(Path.Combine(searchDir, "mediant-20261012-120000.txt"),
                "===== 2026-10-12 12:00:00.000  SBC Lab SBC (192.0.2.10)  IN  10.0.0.8:5060 ->   (SYSLOG)  Call-ID sid-fill@host\r\n" +
                "INVITE sip:7777@192.0.2.10 SIP/2.0\r\nFrom: <sip:7777@192.0.2.58>;tag=1\r\nTo: <sip:7778@192.0.2.10>\r\nCall-ID: sid-fill@host\r\nCSeq: 1 INVITE\r\nContent-Length: 0\r\n\r\n");
            using (var w = new PcapWriter(Path.Combine(searchDir, "mediant-20261012-120000.pcap")))
                w.Write(DateTime.UtcNow, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.10"), 925), new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.20"), 925),
                    System.Text.Encoding.ASCII.GetBytes("INVITE sip:7778@host SIP/2.0\r\nCall-ID: sid-fill@host\r\nCSeq: 1 INVITE\r\n\r\n[SID=board:1:99]"));
            var byFilled = CaptureSearch.Find(searchDir, "board:1:99", out _);
            Check(byFilled.Count == 1 && byFilled[0].From == "7777" && byFilled[0].Session == "board:1:99", "A session id carried only in the pcap is found");
            Check(CaptureSearch.Find(searchDir, "no-such-call", out _).Count == 0, "A search that matches nothing returns no captures");
        }
        finally { try { Directory.Delete(searchDir, true); } catch { } }

        string playDir = Path.Combine(Path.GetTempPath(), "mt-play-" + Guid.NewGuid().ToString("N"));
        try
        {
            CallAudio.WriteFixture(playDir);
            byte[] other = new byte[22];
            other[0] = 0x80; other[1] = 96;
            Check(!CallAudio.Build([other]).ok, "A codec other than G.711 is not played");
            var playSettings = new Settings { TraceFolder = playDir, SyslogPort = 5514, DebugRecordingPort = 925, RetentionDays = 0, RetentionMaxMb = 0 };
            using var player = new Recorder(playSettings);
            var played = player.OpenSaved("mediant-20261017-130000");
            var wav = player.StreamWav(1, "11111111", "192.0.2.20:4000", "(SBC):5000");
            int samples = wav.wav is null ? 0 : (wav.wav.Length - 44) / 2;
            Check(played.ok && wav.ok && samples == 480, "Playing a G.711 stream fills a missing packet with silence (" + played.message + "; " + samples + " samples, " + wav.message + ")");
            if (wav.wav is not null)
            {
                short heard = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(wav.wav.AsSpan(44));
                short gap = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(wav.wav.AsSpan(44 + 160 * 2));
                Check(heard == G711.AlawToLinear(0x2A) && gap == 0, "The played audio is the A-law samples, and the gap is silence");
            }
            Check(!player.StreamWav(1, "22222222", "192.0.2.20:4000", "(SBC):5000").ok, "A stream that is not in the capture is not played");
        }
        finally { try { Directory.Delete(playDir, true); } catch { } }

        var tones = new CallStore();
        string toneSdp = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/AVP 8 101\r\na=rtpmap:8 PCMA/8000\r\na=rtpmap:101 telephone-event/8000\r\n";
        var toneInvite = SipText.Parse("INVITE sip:100@192.0.2.10 SIP/2.0\r\nFrom: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>\r\nCall-ID: dtmf@host\r\nCSeq: 1 INVITE\r\nContent-Type: application/sdp\r\n\r\n" + toneSdp);
        Check(toneInvite.Sdp is not null && toneInvite.Sdp.TelephoneEvents.Contains(101), "SDP names the telephone-event payload type");
        DateTime toneAt = DateTime.UtcNow;
        tones.AddSip(toneAt, toneInvite, "DR", "192.0.2.20:5060", "192.0.2.10:5060", true, "", "192.0.2.10");
        byte[] tone(ushort seq, uint ts, byte ev, bool end, ushort dur)
        {
            var packet = new byte[16];
            packet[0] = 0x80; packet[1] = 101;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), seq);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(4), ts);
            packet[12] = ev; packet[13] = (byte)(end ? 0x8A : 0x0A);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(14), dur);
            return packet;
        }
        tones.AddRtp(toneAt.AddMilliseconds(20), 0, false, "192.0.2.20", 4000, "192.0.2.10", 5000, true, tone(1, 0, 5, false, 160), "192.0.2.10");
        tones.AddRtp(toneAt.AddMilliseconds(40), 0, false, "192.0.2.20", 4000, "192.0.2.10", 5000, true, tone(2, 0, 5, true, 1600), "192.0.2.10");
        tones.AddRtp(toneAt.AddMilliseconds(60), 0, false, "192.0.2.20", 4000, "192.0.2.10", 5000, true, tone(3, 1600, 10, true, 800), "192.0.2.10");
        var audioOnly = new byte[12 + 160]; audioOnly[0] = 0x80; audioOnly[1] = 8;
        tones.AddRtp(toneAt.AddMilliseconds(80), 0, false, "192.0.2.20", 4000, "192.0.2.10", 5000, true, audioOnly, "192.0.2.10");
        string toneJson = System.Text.Json.JsonSerializer.Serialize(tones.CallDetail(1, DateTime.UtcNow));
        int digitRows = 0; for (int at = 0; (at = toneJson.IndexOf("\"digit\":", at, StringComparison.Ordinal)) >= 0; at++) digitRows++;
        Check(digitRows == 2 && toneJson.Contains("\"digit\":\"5\"", StringComparison.Ordinal) && toneJson.Contains("\"durationMs\":200", StringComparison.Ordinal) && toneJson.Contains("\"digit\":\"*\"", StringComparison.Ordinal), "A pressed digit is kept once with its length, and star stays a star");

        string digitDir = Path.Combine(Path.GetTempPath(), "mt-digit-" + Guid.NewGuid().ToString("N"));
        try
        {
            CallAudio.WriteDigitFixture(digitDir);
            var digitSettings = new Settings { TraceFolder = digitDir, SyslogPort = 5514, DebugRecordingPort = 925, RetentionDays = 0, RetentionMaxMb = 0 };
            using var digitRec = new Recorder(digitSettings);
            var digitOpen = digitRec.OpenSaved("mediant-20261017-131600");
            string savedDigits = System.Text.Json.JsonSerializer.Serialize(digitRec.Store.CallDetail(1, DateTime.UtcNow));
            Check(digitOpen.ok && savedDigits.Contains("\"digit\":\"5\"", StringComparison.Ordinal) && savedDigits.Contains("\"digit\":\"*\"", StringComparison.Ordinal), "A saved capture shows the pressed digits (" + digitOpen.message + ")");
        }
        finally { try { Directory.Delete(digitDir, true); } catch { } }

        string Invite(string uri, string via, string body, string id) =>
            $"INVITE {uri} SIP/2.0\r\nVia: {via}\r\nFrom: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>\r\nCall-ID: {id}\r\nCSeq: 1 INVITE\r\nContent-Type: application/sdp\r\n\r\n{body}";
        string avpBody = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/AVP 8\r\na=rtpmap:8 PCMA/8000\r\n";
        string srtpBody = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 RTP/SAVP 8\r\na=rtpmap:8 PCMA/8000\r\na=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:AA\r\n";
        string dtlsBody = "v=0\r\no=- 0 0 IN IP4 192.0.2.20\r\ns=-\r\nc=IN IP4 192.0.2.20\r\nt=0 0\r\nm=audio 4000 UDP/TLS/RTP/SAVPF 111\r\na=rtpmap:111 opus/48000/2\r\na=fingerprint:sha-256 AA:BB\r\n";
        var clearInvite = SipText.Parse(Invite("sip:100@192.0.2.10", "SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1", avpBody, "clear@host"));
        Check(clearInvite.Transport == "UDP" && clearInvite.Sdp is { Protection: "" }, "Clear SIP and audio stay unmarked");
        var srtpInvite = SipText.Parse(Invite("sips:100@192.0.2.10", "SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1", srtpBody, "srtp@host"));
        Check(srtpInvite.Transport == "TLS" && srtpInvite.Sdp is { Protection: "SRTP", Profile: "RTP/SAVP" }, "A sips address and an SRTP offer are marked");
        var dtlsInvite = SipText.Parse(Invite("sip:100@192.0.2.10", "SIP/2.0/TLS 192.0.2.20:5061;branch=z9hG4bK1", dtlsBody, "dtls@host"));
        Check(dtlsInvite.Transport == "TLS" && dtlsInvite.Sdp is { Protection: "DTLS-SRTP" }, "The first Via and DTLS-SRTP are marked");
        var hop = new SyslogSip();
        var assembled = hop.Feed("---- Incoming SIP Message from 192.0.2.20:5061 to SIPInterface #0 TLS 5061 ----\nINVITE sip:100@192.0.2.10 SIP/2.0\nVia: SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1\nFrom: <sip:200@192.0.2.20>;tag=1\nTo: <sip:100@192.0.2.10>\nCall-ID: tls@host\nCSeq: 1 INVITE\nContent-Length: 0\n\n", DateTime.UtcNow);
        Check(assembled.Count == 1 && assembled[0].Message.Transport == "TLS", "The SBC banner marks TLS when Via says UDP");
        byte[] tlsRecord = [0x17, 0x03, 0x03, 0x00, 0x01, 0x00];
        byte[] rtpHead = new byte[12]; rtpHead[0] = 0x80;
        byte[] dtlsRecord = new byte[13]; dtlsRecord[0] = 0x16; dtlsRecord[1] = 0xFE; dtlsRecord[2] = 0xFD;
        Check(SipText.LooksLikeTls(tlsRecord) && !SipText.LooksLikeTls(rtpHead) && SipText.LooksLikeDtls(dtlsRecord), "TLS and DTLS records are recognised, and an RTP packet is not");

        var secureStore = new CallStore();
        DateTime secureAt = DateTime.UtcNow;
        secureStore.AddSip(secureAt, srtpInvite, "SYSLOG", "192.0.2.20:5061", "192.0.2.10:5061", true, "", "192.0.2.10");
        var secureRtp = new byte[12 + 4]; secureRtp[0] = 0x80; secureRtp[1] = 8;
        secureStore.AddRtp(secureAt.AddMilliseconds(20), 0, false, "192.0.2.20", 4000, "", 5000, true, secureRtp, "192.0.2.10");
        secureStore.NoteSealed(secureAt.AddMilliseconds(40), "192.0.2.10", "tls");
        string secureJson = System.Text.Json.JsonSerializer.Serialize(secureStore.CallDetail(1, DateTime.UtcNow));
        string secureList = System.Text.Json.JsonSerializer.Serialize(secureStore.CallList(DateTime.UtcNow));
        Check(secureJson.Contains("\"signalling\":\"TLS\"", StringComparison.Ordinal) && secureJson.Contains("\"mediaProtection\":\"SRTP\"", StringComparison.Ordinal)
            && secureJson.Contains("cannot be played", StringComparison.Ordinal) && secureJson.Contains("could not be read", StringComparison.Ordinal)
            && secureJson.Contains("\"encrypted\":true", StringComparison.Ordinal) && secureList.Contains("\"severity\":\"ok\"", StringComparison.Ordinal),
            "An encrypted call says what cannot be decoded, and it is not listed as a fault");

        var negotiated = new CallStore();
        negotiated.AddSip(secureAt, SipText.Parse(Invite("sip:100@192.0.2.10", "SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1", srtpBody, "neg@host")), "DR", "192.0.2.20:5060", "192.0.2.10:5060", true, "", "192.0.2.10");
        negotiated.AddSip(secureAt.AddMilliseconds(30), SipText.Parse("SIP/2.0 200 OK\r\nVia: SIP/2.0/UDP 192.0.2.20:5060;branch=z9hG4bK1\r\nFrom: <sip:200@192.0.2.20>;tag=1\r\nTo: <sip:100@192.0.2.10>;tag=2\r\nCall-ID: neg@host\r\nCSeq: 1 INVITE\r\nContent-Type: application/sdp\r\n\r\n" + avpBody), "DR", "192.0.2.10:5060", "192.0.2.20:5060", false, "", "192.0.2.10");
        string negotiatedJson = System.Text.Json.JsonSerializer.Serialize(negotiated.CallDetail(1, DateTime.UtcNow));
        Check(negotiatedJson.Contains("\"mediaProtection\":\"\"", StringComparison.Ordinal), "An answer in the clear wins over an SRTP offer");

        string secureDir = Path.Combine(Path.GetTempPath(), "mt-secure-" + Guid.NewGuid().ToString("N"));
        try
        {
            CallAudio.WriteSecureFixture(secureDir);
            var secureSettings = new Settings { TraceFolder = secureDir, SyslogPort = 5514, DebugRecordingPort = 925, RetentionDays = 0, RetentionMaxMb = 0 };
            using var secureRec = new Recorder(secureSettings);
            var secureOpen = secureRec.OpenSaved("mediant-20261018-132900");
            string savedSecure = System.Text.Json.JsonSerializer.Serialize(secureRec.Store.CallDetail(1, DateTime.UtcNow));
            var refusedPlay = secureRec.StreamWav(1, "22222222", "192.0.2.20:4000", "(SBC):5000");
            Check(secureOpen.ok && savedSecure.Contains("\"signalling\":\"TLS\"", StringComparison.Ordinal) && savedSecure.Contains("\"mediaProtection\":\"SRTP\"", StringComparison.Ordinal)
                && savedSecure.Contains("\"encrypted\":true", StringComparison.Ordinal) && !refusedPlay.ok && refusedPlay.message.Contains("encrypted", StringComparison.Ordinal),
                "A saved encrypted call is marked and its audio is not played (" + secureOpen.message + "; " + refusedPlay.message + ")");
        }
        finally { try { Directory.Delete(secureDir, true); } catch { } }

        Check(WebServer.AccessOk("", "", false, true, null), "No sign-in is required until a username is set");
        Check(WebServer.AccessOk("", "secret", true, true, null), "A password on its own still leaves this PC open");
        string wrong = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("anyone:nope"));
        string rightPass = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("anyone:secret"));
        Check(!WebServer.AccessOk("", "secret", true, false, wrong) && WebServer.AccessOk("", "secret", true, false, rightPass), "A password on its own still protects the network");
        string rightUser = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("alex:secret"));
        string otherUser = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("other:secret"));
        Check(!WebServer.SignedIn("", "", false, true, null, false), "The sign-in page stays up until a password exists");
        Check(WebServer.SignedIn("alex", "secret", false, true, null, true), "A signed-in session opens the app");
        Check(!WebServer.SignedIn("alex", "secret", false, true, null, false) && !WebServer.SignedIn("alex", "secret", false, true, otherUser, false) && WebServer.SignedIn("alex", "secret", false, true, rightUser, false), "The app opens after the username and password, or a matching sign-in header");
        (string User, string Password)[] people = [("ann", "a-secret"), ("bob", "b-secret")];
        Check(WebServer.AccountMatches(people, "Ann", "a-secret") && WebServer.AccountMatches(people, "bob", "b-secret") && !WebServer.AccountMatches(people, "ann", "b-secret") && !WebServer.AccountMatches(people, "cara", "a-secret"), "Each person signs in with their own password");

        string logDir = Path.Combine(Path.GetTempPath(), "mt-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sink = new SplunkLog(logDir, () => 80, () => 14);
            sink.Write(new { kind = "sip", callId = "one@host", from = "100", to = "200" });
            sink.Write(new { kind = "sip", callId = "two@host-with-extra-text-so-the-file-rolls" });
            sink.Dispose();
            string day = Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd"));
            string[] logs = Directory.GetFiles(day, "*.log");
            string joined = string.Join("\n", logs.Select(File.ReadAllText));
            bool split = logs.Length >= 2 && logs.Any(f => { string t = File.ReadAllText(f); return t.Contains("\"callId\":\"one@host\"", StringComparison.Ordinal) && !t.Contains("two@host", StringComparison.Ordinal); });
            Check(split && joined.Contains("\"kind\":\"sip\"", StringComparison.Ordinal), "A log rolls into the next file at the size limit, in today's folder (" + logs.Length + " files)");
            Directory.CreateDirectory(Path.Combine(logDir, "2020-01-01"));
            File.WriteAllText(Path.Combine(logDir, "2020-01-01", "old.log"), "x");
            Check(SplunkLog.Prune(logDir, 14) == 1 && !Directory.Exists(Path.Combine(logDir, "2020-01-01")) && Directory.Exists(day), "Log folders older than the limit are removed and today's folder stays");
        }
        finally { try { Directory.Delete(logDir, true); } catch { } }

        // ---- 2.0: Syslog Viewer features
        {
            DateTime day = new(2026, 10, 14);
            Check(SyslogImport.TryRecord("12:28:52.794  10.4.219.224  local0.warning  [S=12] text", day, day, out DateTime t1, out string s1, out string b1, out string f1)
                && s1 == "10.4.219.224" && b1 == "<132>[S=12] text" && f1 == "viewer" && t1.ToLocalTime().TimeOfDay == new TimeSpan(0, 12, 28, 52, 794),
                $"A Syslog Viewer log line gives time, sender and severity ({s1} / {b1})");
            Check(SyslogImport.TryRecord("<157>2023-03-17T13:25:58.962Z 192.0.2.30 [S=125335] [SID=a0ac11:18:14684]  (N   538043) New", day, day, out DateTime t2, out string s2, out string b2, out _)
                && s2 == "192.0.2.30" && b2.StartsWith("<157>[S=125335]") && t2 == new DateTime(2023, 3, 17, 13, 25, 58, 962, DateTimeKind.Utc),
                "An acSyslog line keeps its PRI and UTC time");
            Check(SyslogImport.TryRecord("Oct 14 08:27:01 mediant1 [S=5] hello", day, day, out _, out string s3, out string b3, out _) && s3 == "mediant1" && b3 == "[S=5] hello",
                "A BSD syslog line gives the host name");
            Check(!SyslogImport.TryRecord("Via: SIP/2.0/UDP 10.0.0.1:5060", day, day, out _, out _, out _, out _), "A SIP header line continues the message before it");

            string Temp(string name, string body) { string f = Path.Combine(Path.GetTempPath(), "mt-import-" + Guid.NewGuid().ToString("N") + name); File.WriteAllText(f, body); return f; }
            var files = new List<string>();
            try
            {
                string viewer = Temp(".txt", "12:28:52.794  10.4.219.224  local0.notice  [S=1] [SID=a:1:2] ---- Incoming SIP Message from 10.0.0.8:5060 to SIPInterface #0 UDP ----\r\n  INVITE sip:100@10.0.0.1 SIP/2.0\r\n  Call-ID: abc@x\r\n12:28:52.900  10.4.219.224  local0.err  [S=2] second\r\n");
                files.Add(viewer);
                var rv = SyslogImport.Read(viewer, "v.txt", day);
                Check(rv.Lines.Count == 2 && rv.Lines[0].Text.Contains("\nINVITE sip:100@10.0.0.1 SIP/2.0\nCall-ID: abc@x") && rv.Lines[1].Text.StartsWith("<131>") && rv.Format == "Syslog Viewer log",
                    $"A Syslog Viewer log keeps multi-line messages together ({rv.Lines.Count} lines, {rv.Format})");

                string csv = Temp(".csv", "TimeGenerated,HostIP,Facility,SeverityLevel,SyslogMessage\r\n\"2026-10-14T08:00:01.5Z\",10.1.1.1,local0,err,\"[S=7] bad, with comma\"\r\n2026-10-14T08:00:00Z,10.1.1.1,local0,notice,\"[S=6] first\nsecond line\"\r\n");
                files.Add(csv);
                var rc = SyslogImport.Read(csv, "a.csv", day);
                Check(rc.Lines.Count == 2 && rc.Lines[0].Text == "<131>[S=7] bad, with comma" && rc.Lines[1].Text == "<133>[S=6] first\nsecond line" && rc.Lines[0].Source == "10.1.1.1",
                    "A CSV export with a header row is read (quoted commas and line breaks)");

                string sl4nt = Temp(".csv", "\"2023-01-21T13:41:31.903Z\",local3,notice,\"10.10.80.14\",\"[S=104101] [BID=f93af3:34] text, unescaped\"\r\n");
                files.Add(sl4nt);
                var rs = SyslogImport.Read(sl4nt, "s.csv", day);
                Check(rs.Lines.Count == 1 && rs.Lines[0].Source == "10.10.80.14" && rs.Lines[0].Text.StartsWith("<157>[S=104101]") && rs.Lines[0].Text.EndsWith("text, unescaped"),
                    $"A CSV without a header (SL4NT) is read ({rs.Format})");

                string azure = Temp(".json", """
                    [{"FrameType":"DataSetHeader","IsProgressive":false,"Version":"v2.0"},
                     {"FrameType":"DataTable","TableId":1,"TableKind":"PrimaryResult","TableName":"PrimaryResult",
                      "Columns":[{"ColumnName":"timestamp","ColumnType":"datetime"},{"ColumnName":"ip","ColumnType":"string"},{"ColumnName":"priority","ColumnType":"string"},{"ColumnName":"msg","ColumnType":"string"}],
                      "Rows":[["2023-11-14T12:29:28.082Z","10.80.80.4","local3.notice","[S=65572372] [SID=97f1af:2:339879] hello"]]}]
                    """);
                files.Add(azure);
                var ra = SyslogImport.Read(azure, "a.json", day);
                Check(ra.Lines.Count == 1 && ra.Lines[0].Text == "<157>[S=65572372] [SID=97f1af:2:339879] hello" && ra.Lines[0].Source == "10.80.80.4",
                    $"An Azure Log Analytics JSON export is read ({ra.Format})");

                string ndjson = Temp(".json", "{\"@timestamp\":\"2026-10-14T08:00:00Z\",\"host\":{\"ip\":\"10.2.2.2\"},\"message\":\"[S=1] one\",\"severity\":\"warning\"}\n{\"@timestamp\":\"2026-10-14T08:00:01Z\",\"host\":{\"ip\":\"10.2.2.2\"},\"message\":\"[S=2] two\"}\n");
                files.Add(ndjson);
                var rn = SyslogImport.Read(ndjson, "n.json", day);
                Check(rn.Lines.Count == 2 && rn.Lines[0].Source == "10.2.2.2" && rn.Lines[0].Text == "<132>[S=1] one", "JSON with one object per line is read (nested host.ip)");

                string web = Temp(".txt", "[S=65572373] [SID=97f1af:2:339879] (N 71397950) (#1073)gwSession[Deallocated] [Time:14-11@12:28:52.794]\r\n");
                files.Add(web);
                var rw = SyslogImport.Read(web, "w.txt", day);
                Check(rw.Lines.Count == 1 && rw.Lines[0].Utc.ToLocalTime().Month == 11 && rw.Lines[0].Utc.ToLocalTime().Day == 14, $"The legacy web format is read with its time tag ({rw.Format})");

                // Excel: a minimal workbook with shared strings.
                string xlsx = Path.Combine(Path.GetTempPath(), "mt-import-" + Guid.NewGuid().ToString("N") + ".xlsx");
                files.Add(xlsx);
                using (var zip = System.IO.Compression.ZipFile.Open(xlsx, System.IO.Compression.ZipArchiveMode.Create))
                {
                    void Entry(string name, string body) { using var w = new StreamWriter(zip.CreateEntry(name).Open()); w.Write(body); }
                    Entry("xl/sharedStrings.xml", "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><si><t>Time</t></si><si><t>Host</t></si><si><t>Message</t></si><si><t>[S=9] from excel</t></si><si><t>10.3.3.3</t></si></sst>");
                    Entry("xl/worksheets/sheet1.xml", "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"B1\" t=\"s\"><v>1</v></c><c r=\"C1\" t=\"s\"><v>2</v></c></row><row r=\"2\"><c r=\"A2\"><v>46309.5</v></c><c r=\"B2\" t=\"s\"><v>4</v></c><c r=\"C2\" t=\"s\"><v>3</v></c></row></sheetData></worksheet>");
                }
                var rx = SyslogImport.Read(xlsx, "x.xlsx", day);
                Check(rx.Lines.Count == 1 && rx.Lines[0].Text == "[S=9] from excel" && rx.Lines[0].Source == "10.3.3.3" && rx.Lines[0].Utc.ToLocalTime().Hour == 12,
                    $"An Excel sheet is read, Excel dates included ({rx.Format})");

                // What the write log writes reads back the same.
                string formatted = SyslogFileLog.Format(new DateTime(2026, 10, 14, 7, 0, 0, DateTimeKind.Utc), "10.1.1.1", "<131>[S=3] line one\nline two");
                string back = Temp(".txt", formatted + "\r\n");
                files.Add(back);
                var rb = SyslogImport.Read(back, "b.txt", day);
                Check(formatted.Contains("local0.err  [S=3] line one\r\n  line two") && rb.Lines.Count == 1 && rb.Lines[0].Text == "<131>[S=3] line one\nline two" && rb.Lines[0].Source == "10.1.1.1",
                    "A write-log line reads back as the same message");
            }
            finally { foreach (string f in files) try { File.Delete(f); } catch (IOException) { } }

            // [S=n]: overtaken messages back in order, gaps counted, late ones forgiven.
            var got = new List<int>();
            var gaps = new List<(long from, long count)>();
            var order = new SyslogSequence<int>(got.Add, (_, from, count, _) => gaps.Add((from, count)));
            DateTime t0 = DateTime.UtcNow;
            order.Offer("sbc", 1, t0, 1);
            order.Offer("sbc", 3, t0, 3);
            order.Offer("sbc", 2, t0, 2);
            order.Offer("sbc", 6, t0, 6);
            order.Tick(t0.AddSeconds(1));
            order.Offer("sbc", 4, t0.AddSeconds(1), 4);
            var st = order.Snapshot()["sbc"];
            Check(got.SequenceEqual([1, 2, 3, 6, 4]) && gaps.Count == 1 && gaps[0] == (4, 2) && st.Missing == 1 && st.OutOfOrder == 1,
                $"Syslog is reordered by [S=n], gaps are reported and a late message is counted ({string.Join(",", got)}; missing {st.Missing})");
            order.Offer("sbc", 100000, t0, 7);
            order.Offer("sbc", 1, t0, 8);
            Check(order.Snapshot()["sbc"].Restarts == 2 && got[^1] == 8, "A jump in the numbering counts as the SBC restarting, not as lost messages");
            Check(SyslogSequence<int>.NumberOf("<134>[S=4711] [SID=x]") == 4711 && SyslogSequence<int>.NumberOf("<134> nothing") == -1, "The [S=n] number is read from a syslog line");

            // TCP framing: octet counting and line feeds, partial messages kept for later.
            byte[] stream = System.Text.Encoding.ASCII.GetBytes("11 <134>hello!<134>line two\n5 <13");
            int at = 0;
            var frames = SyslogStreamListener.Frames(stream, stream.Length, ref at);
            Check(frames.Count == 2 && System.Text.Encoding.ASCII.GetString(stream, frames[0].start, frames[0].count) == "<134>hello!"
                && System.Text.Encoding.ASCII.GetString(stream, frames[1].start, frames[1].count) == "<134>line two" && at == stream.Length - 5,
                "Syslog over TCP is split into messages (octet counting and line ends)");

            // HTTP / DNS messages for a session, and IP phone SIP.
            var dns = new SyslogSip();
            var none = dns.Feed("[S=1] [SID=a:1:9] (N 10) ---- Outgoing DNS Message to 10.0.0.53:53 ----\nQuery sip.example.com A", t0);
            var flushed = dns.Feed("[S=2] [SID=a:1:9] (N 11) SIPServersIPList::ResolveDNS done", t0);
            Check(none.Count == 0 && flushed.Count == 1 && flushed[0].Kind == "DNS" && flushed[0].Inbound == false && flushed[0].Peer == "10.0.0.53:53" && flushed[0].SessionId == "a:1:9",
                "A DNS message the SBC logs for a session is kept for the ladder");
            var phone = new SyslogSip();
            var ph = phone.Feed("12:00:00 {acl_TransportMsgBuilderPrintMsg():123} <-- INVITE sip:100@10.0.0.1 SIP/2.0\nCall-ID: phone@x\nCSeq: 1 INVITE\nContent-Length: 0\n\n", t0);
            Check(ph.Count == 1 && ph[0].Inbound == true && ph[0].Message.CallId == "phone@x", "An IP phone's SIP (acl_Transport... <--) is read with its direction");

            var calls = new CallStore();
            calls.AddOther(t0, "DNS", "DNS Query", "Query", false, "10.0.0.53:53", "s:1:1", "sbc");
            calls.AddSip(t0.AddMilliseconds(5), SipText.Parse("INVITE sip:1@x SIP/2.0\r\nCall-ID: c1\r\nCSeq: 1 INVITE\r\nFrom: <sip:a@x>\r\nTo: <sip:b@x>\r\n\r\n"), "SYSLOG", "", "", true, "s:1:1", "sbc");
            string detailJson = System.Text.Json.JsonSerializer.Serialize(calls.CallDetail(1, t0));
            Check(detailJson.Contains("\"kind\":\"DNS\"") && detailJson.Contains("\"kind\":\"SIP\""), "A DNS look-up before the INVITE joins the call's ladder");

            calls.AddSyslog(t0, "sbc", "<131>[S=1] Error one");
            calls.AddSyslog(t0, "sbc", "<134>[S=2] fine |CALL_END |SBC |x");
            calls.AddSyslog(t0, "other", "<134>[S=3] [SID=q:1:2] call line");
            string View(CallStore.SyslogQuery q) => System.Text.Json.JsonSerializer.Serialize(calls.SyslogView(q));
            Check(View(new CallStore.SyslogQuery { Text = "error", Exclude = true }).Contains("fine") && !View(new CallStore.SyslogQuery { Text = "error", Exclude = true }).Contains("Error one"), "The display filter can hide matching lines");
            Check(View(new CallStore.SyslogQuery { Text = @"S=\d\]\s+Err", Regex = true }).Contains("Error one") && View(new CallStore.SyslogQuery { Text = "(", Regex = true }).Contains("not valid"), "The display filter takes a regular expression, and says when one is not valid");
            Check(View(new CallStore.SyslogQuery { Kind = "cdr" }).Contains("CALL_END") && !View(new CallStore.SyslogQuery { Kind = "cdr" }).Contains("Error one")
                && View(new CallStore.SyslogQuery { Session = "q:1:2" }).Contains("call line") && View(new CallStore.SyslogQuery { Level = "errors", Sbc = "sbc" }).Contains("Error one"),
                "CDR, session, severity and device filters pick the right lines");

            // Rotation: syslog.txt, then syslog_0001.txt, the oldest dropped beyond the count.
            string wl = Path.Combine(Path.GetTempPath(), "mt-wl-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var log = new SyslogFileLog(wl, "syslog.txt", 0, 3, false, false, true, _ => ""))
                {
                    // 0 MB = never rotate.
                    for (int i = 0; i < 5; i++) log.Write(t0, "10.1.1.1", "<134>x");
                }
                var rot = new SyslogFileLog(wl, "syslog.txt", 1, 3, true, false, true, ip => ip == "10.1.1.1" ? "Mediant A" : "");
                string big = new('x', 300_000);
                for (int i = 0; i < 12; i++) rot.Write(t0, "10.1.1.1", "<134>" + big);
                rot.Dispose();
                var written = Directory.GetFiles(wl).Select(Path.GetFileName).OrderBy(x => x).ToList();
                Check(written.Contains("syslog.txt") && written.Contains("syslog_Mediant_A.txt") && written.Count(n => n!.StartsWith("syslog_Mediant_A_")) == 2,
                    $"The write log rotates by size, per SBC by name, and keeps the file count ({string.Join(", ", written)})");
            }
            finally { try { Directory.Delete(wl, true); } catch (IOException) { } }

            // pcapng with a Linux cooked header, as Wireshark saves on Linux.
            string ng = Path.Combine(Path.GetTempPath(), "mt-" + Guid.NewGuid().ToString("N") + ".pcapng");
            try
            {
                byte[] payload = System.Text.Encoding.ASCII.GetBytes("<134>[S=1] hello");
                var frame = new List<byte>();
                byte[] sll = new byte[16]; sll[14] = 0x08; sll[15] = 0x00;   // Linux cooked header: protocol IPv4 in the last two bytes
                frame.AddRange(sll);
                byte[] ip = new byte[20]; ip[0] = 0x45; ip[9] = 17; ip[12] = 10; ip[13] = 1; ip[14] = 1; ip[15] = 1; ip[16] = 10; ip[17] = 9; ip[18] = 9; ip[19] = 9;
                int total = 20 + 8 + payload.Length; ip[2] = (byte)(total >> 8); ip[3] = (byte)total;
                frame.AddRange(ip);
                frame.AddRange([0x13, 0x88, 0x02, 0x02, (byte)((8 + payload.Length) >> 8), (byte)(8 + payload.Length), 0, 0]);
                frame.AddRange(payload);
                using (var w = new BinaryWriter(File.Create(ng)))
                {
                    w.Write(0x0A0D0D0A); w.Write(28); w.Write(0x1A2B3C4D); w.Write((ushort)1); w.Write((ushort)0); w.Write(-1L); w.Write(28);
                    w.Write(1); w.Write(20); w.Write((ushort)113); w.Write((ushort)0); w.Write(65535); w.Write(20);
                    int pad = (4 - frame.Count % 4) % 4, len = 32 + frame.Count + pad;
                    long micros = 1_700_000_000_000_000;
                    w.Write(6); w.Write(len); w.Write(0); w.Write((int)(micros >> 32)); w.Write((int)(micros & 0xffffffff)); w.Write(frame.Count); w.Write(frame.Count);
                    w.Write(frame.ToArray()); w.Write(new byte[pad]); w.Write(len);
                }
                var packets = CaptureReplay.Pcap(ng).ToList();
                Check(packets.Count == 1 && packets[0].Destination.Port == 514 && packets[0].Source.Address.ToString() == "10.1.1.1"
                    && System.Text.Encoding.ASCII.GetString(packets[0].Payload) == "<134>[S=1] hello" && packets[0].Utc.Year == 2023,
                    "A Wireshark pcapng with Linux cooked headers is read");
            }
            finally { try { File.Delete(ng); } catch (IOException) { } }
        }

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
        // Syslog over TCP and TLS, when turned on.
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (syslog TCP)\"");
        var syslogTcp = new[] { settings.SyslogTcpPort, settings.SyslogTlsPort }.Where(p => p > 0).ToList();
        if (syslogTcp.Count > 0)
            Run("netsh", $"advfirewall firewall add rule name=\"Mediant Trace (syslog TCP)\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={string.Join(",", syslogTcp)}{remote}");
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
        Run("netsh", "advfirewall firewall delete rule name=\"Mediant Trace (syslog TCP)\"");
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
    public Recorder? Recorder => _recorder;
    private SipTester? _tester;
    private ChangeManager? _changes;
    private WebServer? _web;
    private ReadyCheck? _ready;
    private LineMonitor? _monitor;
    private Storage? _storage;

    public string Url => _web?.Url ?? "";

    public bool Start()
    {
        Log.Write($"MediantTrace {typeof(App).Assembly.GetName().Version?.ToString(3)} starting; settings in {Settings.FilePath}");
        _recorder = new Recorder(_settings);
        _recorder.Open();
        _recorder.ApplyRetention();
        _tester = new SipTester(_settings);
        _tester.Open();
        if (_settings.McpToken.Length == 0)
        {
            _settings.McpToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            _settings.Save();
        }
        _changes = new ChangeManager(_settings);
        var bulk = new BulkTests(_tester, _recorder, _changes, _settings);
        var board = new LineBoard(_settings, _recorder, bulk);
        var drift = new DriftWatch(_settings);
        _ready = new ReadyCheck(_settings, _recorder, _tester, _changes, board, drift);
        var health = new SbcHealth(_settings, _changes);
        _monitor = new LineMonitor(_settings, board);
        var upgrades = new Upgrades(_settings, _changes, board);
        var timeline = new Timeline(_changes, health, _monitor, _tester, bulk, _ready, upgrades);
        _storage = new Storage(_settings, _recorder, _tester, bulk, _ready, _monitor, upgrades, _changes);
        var mcp = new McpServer(_settings, _recorder, _tester, _changes, bulk, board, _ready, health, _monitor, upgrades, timeline);
        _web = new WebServer(_settings, _recorder, _tester, _changes, mcp, bulk, board, _ready, drift, health, _monitor, upgrades, timeline, _storage);
        return _web.Start();
    }

    public void Dispose()
    {
        _web?.Dispose();
        _ready?.Dispose();
        _monitor?.Dispose();
        _storage?.Dispose();
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
