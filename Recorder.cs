using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Listens for Debug Recording and syslog all the time. While a capture is
/// running it writes every packet to a .pcap and every SIP message to a .txt,
/// and feeds the call view. Between captures packets are only counted, so the
/// page can show that the Mediant is sending.
/// </summary>
public sealed class Recorder : IDisposable
{
    private readonly Settings _settings;
    private readonly object _gate = new();
    private readonly List<Socket> _sockets = [];
    // The socket listening for each feed ("DR", "SYSLOG"), so a port change can move it without a restart.
    private readonly Dictionary<string, (Socket socket, int port)> _feeds = new();
    private readonly object _feedGate = new();

    /// <summary>The UDP port each feed is listening on now (0 = not listening).</summary>
    public int ListeningPort(string kind)
    {
        lock (_feedGate) return _feeds.TryGetValue(kind, out var f) ? f.port : 0;
    }
    private readonly CancellationTokenSource _stop = new();
    // One reassembler per SBC and session: pieces from different SBCs and calls arrive interleaved.
    private readonly Dictionary<string, SyslogSip> _syslogSip = new();
    private readonly Dictionary<string, SourceStats> _sources = new();
    private readonly CdrDecoder _cdr = new();

    private PcapWriter? _pcap;
    private StreamWriter? _text;
    private SplunkLog? _logs;
    private bool _forward = true;

    public CallStore Store { get; } = new();
    public bool Recording { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? StopsAt { get; private set; }
    public string? PcapPath { get; private set; }
    public string? TextPath { get; private set; }
    /// <summary>The saved capture currently shown in the call view, when it was opened from the Captures page.</summary>
    public string? Opened { get; private set; }
    public string LogFolder => _logs?.Root ?? _settings.LogFolderFull;
    public string LogFile => _logs?.FileName ?? "";
    public long DrPackets, SyslogPackets, SipMessages, RtpPackets, Ignored;
    public List<string> ListenErrors { get; } = [];

    public sealed class SourceStats
    {
        public string Address = "";
        public string Kind = "";
        public long Packets;
        public DateTime LastSeen;
    }

    // Syslog put back in [S=n] order before it is decoded; gaps become warning lines.
    private readonly record struct PendingLine(DateTime Time, string Sbc, string Line);
    private readonly SyslogSequence<PendingLine> _sequence;
    // Syslog over TCP and TLS, by kind ("TCP", "TLS").
    private readonly Dictionary<string, SyslogStreamListener> _streams = new();
    private readonly object _streamGate = new();
    private X509Certificate2? _tlsCertificate;
    // Receive filters: senders and lines dropped on arrival.
    private volatile Regex[] _dropPatterns = [];
    private volatile HashSet<string> _dropFrom = [];
    private SyslogFileLog? _writeLog;
    private CancellationTokenSource? _watchStop;

    /// <summary>A log file being followed as it grows (Syslog Viewer's monitor mode), or null.</summary>
    public string? Watching { get; private set; }
    public long WatchLines { get; private set; }
    public string? WatchError { get; private set; }
    public string? TlsThumbprint => _tlsCertificate?.Thumbprint;
    public string? WriteLogError => _writeLog?.Error;
    public long WriteLogLines => _writeLog?.Lines ?? 0;
    public string WriteLogFile => _writeLog?.CurrentFile() ?? "";

    public Recorder(Settings settings)
    {
        _settings = settings;
        _sequence = new SyslogSequence<PendingLine>(p => IngestSyslog(p.Time, p.Sbc, p.Line, true), NoteGap);
    }

    /// <summary>The TCP port syslog is received on by kind ("TCP", "TLS"), 0 when off.</summary>
    public int StreamPort(string kind)
    {
        lock (_streamGate) return _streams.TryGetValue(kind, out var l) ? l.Port : 0;
    }

    public int StreamConnections()
    {
        lock (_streamGate) return _streams.Values.Sum(s => s.Connections);
    }

    /// <summary>Missing and out-of-order syslog, per SBC address.</summary>
    public Dictionary<string, SequenceStats> SequenceStats()
    {
        lock (_gate) return _sequence.Snapshot();
    }

    /// <summary>Moves the SBC logs to the folder in settings (from now on; earlier days stay where they are).</summary>
    public void ApplyLogFolder()
    {
        if (_logs is null || string.Equals(_logs.Root, _settings.LogFolderFull, StringComparison.OrdinalIgnoreCase)) return;
        var old = _logs;
        _logs = new SplunkLog(_settings.LogFolderFull, () => Math.Clamp(_settings.LogMaxMb, 1, 100) * 1024 * 1024, () => Math.Clamp(_settings.LogRetentionDays, 1, 3650));
        old.Dispose();
        Log.Write($"SBC logs now go to {_settings.LogFolderFull} (earlier days stay in {old.Root})");
    }

    public void Open()
    {
        _logs = new SplunkLog(_settings.LogFolderFull, () => Math.Clamp(_settings.LogMaxMb, 1, 100) * 1024 * 1024, () => Math.Clamp(_settings.LogRetentionDays, 1, 3650));
        SplunkLog.Prune(_settings.LogFolderFull, _settings.LogRetentionDays);
        Log.Write($"Logging to {_settings.LogFolderFull} (one folder a day, a new file at {_settings.LogMaxMb} MB)");
        TryListen(_settings.DebugRecordingPort, "DR");
        TryListen(_settings.SyslogPort, "SYSLOG");
        ApplySyslogOptions();
        ApplyStreamListeners();
        _ = Task.Run(() => Housekeeping(_stop.Token));
        _ = Task.Run(() => SequenceClock(_stop.Token));
    }

    /// <summary>Buffer size, reordering, receive filters and the write log, from settings. Returns problems with the filters.</summary>
    public List<string> ApplySyslogOptions()
    {
        var problems = new List<string>();
        Store.SyslogCapacity = _settings.SyslogBufferLines;
        var patterns = new List<Regex>();
        foreach (string p in _settings.IgnoreSyslogPatterns.Where(p => p.Trim().Length > 0))
        {
            try { patterns.Add(new Regex(p, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50))); }
            catch (ArgumentException e) { problems.Add($"Content filter '{p}' is not a valid regular expression: {e.Message}"); }
        }
        _dropPatterns = [.. patterns];
        _dropFrom = new HashSet<string>(_settings.IgnoreSyslogFrom.Select(x => x.Trim()).Where(x => x.Length > 0), StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            _sequence.Reorder = _settings.SyslogReorder;
            var old = _writeLog;
            bool same = old is not null && _settings.WriteLog
                && string.Equals(old.Folder, _settings.WriteLogFolderFull, StringComparison.OrdinalIgnoreCase) && old.BaseName == Path.GetFileName(_settings.WriteLogFile.Trim())
                && old.MaxBytes == Math.Max(0, _settings.WriteLogFileMb) * 1024L * 1024 && old.MaxFiles == _settings.WriteLogFileCount
                && old.PerDevice == _settings.WriteLogPerDevice && old.FolderPerDevice == _settings.WriteLogFolderPerDevice && old.UseNames == _settings.WriteLogUseNames;
            if (!same)
            {
                old?.Dispose();
                _writeLog = _settings.WriteLog
                    ? new SyslogFileLog(_settings.WriteLogFolderFull, _settings.WriteLogFile, _settings.WriteLogFileMb, _settings.WriteLogFileCount,
                        _settings.WriteLogPerDevice, _settings.WriteLogFolderPerDevice, _settings.WriteLogUseNames, ip => _settings.FindSbc(ip)?.Name ?? "")
                    : null;
                if (_writeLog is not null) Log.Write($"Writing syslog to {Path.Combine(_writeLog.Folder, _writeLog.BaseName)}");
                else if (old is not null) Log.Write("Stopped writing the syslog text log");
            }
        }
        return problems;
    }

    /// <summary>Starts, moves or stops syslog over TCP and TLS to match settings. Returns one line per listener that could not start.</summary>
    public List<string> ApplyStreamListeners()
    {
        var problems = new List<string>();
        foreach (var (kind, port) in new[] { ("TCP", _settings.SyslogTcpPort), ("TLS", _settings.SyslogTlsPort) })
        {
            SyslogStreamListener? current;
            lock (_streamGate) _streams.TryGetValue(kind, out current);
            if (current is not null && current.Port == port && port > 0) continue;
            if (current is not null)
            {
                current.Dispose();
                lock (_streamGate) _streams.Remove(kind);
                Log.Write($"Stopped syslog over {kind} on TCP {current.Port}");
            }
            if (port <= 0) continue;
            X509Certificate2? cert = null;
            if (kind == "TLS")
            {
                try { cert = _tlsCertificate ??= SyslogStreamListener.Certificate(_settings); }
                catch (Exception e)
                {
                    problems.Add($"Syslog over TLS is off: the certificate could not be loaded ({e.Message}).");
                    continue;
                }
            }
            var (listener, error) = SyslogStreamListener.Start(port, cert, OnStreamMessage, a => !_settings.OnlyListedSbcs || _settings.FindSbc(Plain(a)) is not null);
            if (listener is null) { problems.Add(error!); Log.Write(error!); continue; }
            lock (_streamGate) _streams[kind] = listener;
        }
        lock (_feedGate)
        {
            ListenErrors.RemoveAll(e => e.Contains("syslog over", StringComparison.OrdinalIgnoreCase) || e.StartsWith("Could not listen on TCP", StringComparison.Ordinal) || e.StartsWith("Syslog over TLS", StringComparison.Ordinal));
            ListenErrors.AddRange(problems);
        }
        return problems;
    }

    /// <summary>The certificate syslog over TLS uses (DER), to give the SBC to trust. Made on first use.</summary>
    public byte[] TlsCertificateDer()
    {
        _tlsCertificate ??= SyslogStreamListener.Certificate(_settings);
        return _tlsCertificate.Export(X509ContentType.Cert);
    }

    private static string Plain(IPAddress a) => (a.IsIPv4MappedToIPv6 ? a.MapToIPv4() : a).ToString();

    private void OnStreamMessage(DateTime now, IPEndPoint source, IPEndPoint destination, byte[] message)
    {
        var from = new IPEndPoint(source.Address.IsIPv4MappedToIPv6 ? source.Address.MapToIPv4() : source.Address, source.Port);
        if (!Count("SYSLOG", from, now)) return;
        try
        {
            // Into the capture as if it came by UDP to the syslog port, so the .pcap replays the same way.
            Handle("SYSLOG", now, from, new IPEndPoint(destination.Address, _settings.SyslogPort), message);
        }
        catch (Exception e) { Log.Write($"Syslog from {from} could not be handled: {e.Message}"); }
    }

    /// <summary>Counts a packet against its sender. False when the sender is not wanted (not a listed SBC, or filtered out).</summary>
    private bool Count(string kind, IPEndPoint source, DateTime now)
    {
        string address = source.Address.ToString();
        if (_settings.OnlyListedSbcs && _settings.FindSbc(address) is null || kind == "SYSLOG" && _dropFrom.Contains(address))
        {
            Interlocked.Increment(ref Ignored);
            return false;
        }
        lock (_sources)
        {
            string key = $"{kind}|{address}";
            if (!_sources.TryGetValue(key, out SourceStats? st))
                _sources[key] = st = new SourceStats { Address = address, Kind = kind };
            st.Packets++;
            st.LastSeen = now;
        }
        return true;
    }

    private async Task SequenceClock(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(100, ct); } catch (OperationCanceledException) { return; }
            try { lock (_gate) _sequence.Tick(DateTime.UtcNow); }
            catch (Exception e) { Log.Write($"Syslog order: {e.Message}"); }
        }
    }

    // A gap in the [S=n] numbers: a warning line in the syslog, as Syslog Viewer shows it.
    private void NoteGap(string sbc, long from, long count, DateTime now)
    {
        string text = $"<132>MediantTrace: {count} syslog message{(count == 1 ? " is" : "s are")} missing from {sbc} ([S={from}]{(count > 1 ? $" to [S={from + count - 1}]" : "")})";
        if (Opened is null) Store.AddSyslog(now, sbc, text);
        _writeLog?.Write(now, sbc, text);
        _text?.WriteLine($"{now.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}  syslog {sbc}  {text}");
    }

    /// <summary>
    /// Moves the feeds to the ports in settings, without a restart. The new port is opened
    /// before the old one is closed, so a port that cannot be opened (in use, no rights)
    /// leaves the feed where it was. Returns one line per feed that could not move.
    /// </summary>
    public List<string> Relisten()
    {
        var problems = new List<string>();
        foreach (var (kind, port) in new[] { ("DR", _settings.DebugRecordingPort), ("SYSLOG", _settings.SyslogPort) })
        {
            if (ListeningPort(kind) == port) continue;
            if (TryListen(port, kind) is string error) problems.Add(error);
        }
        lock (_feedGate)
            ListenErrors.RemoveAll(e => !problems.Contains(e) && _feeds.Values.Any(f => e.Contains($"UDP {f.port} ")));
        return problems;
    }

    private string? TryListen(int port, string kind)
    {
        string what = kind == "DR" ? "Debug Recording" : "syslog";
        Socket? socket = null;
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Never share a port with another program: two listeners would split the SBC's packets.
            socket.ExclusiveAddressUse = true;
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            socket.ReceiveBufferSize = 8 * 1024 * 1024;
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }
        catch (SocketException e)
        {
            socket?.Dispose();
            string reason = e.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? "another program is using it (another copy of MediantTrace, or a syslog server)"
                : e.SocketErrorCode == SocketError.AccessDenied ? "Windows refused it (ports below 1024 may need administrator rights)" : e.Message;
            int now = ListeningPort(kind);
            string msg = $"Could not listen on UDP {port} for {what}: {reason}.{(now > 0 ? $" Still listening on UDP {now}." : "")}";
            lock (_feedGate) { ListenErrors.RemoveAll(x => x.Contains($"for {what}:")); ListenErrors.Add(msg); }
            Log.Write(msg);
            return msg;
        }
        Socket? old = null;
        int oldPort = 0;
        lock (_feedGate)
        {
            if (_feeds.TryGetValue(kind, out var f)) { old = f.socket; oldPort = f.port; _sockets.Remove(f.socket); }
            _feeds[kind] = (socket, port);
            _sockets.Add(socket);
            ListenErrors.RemoveAll(x => x.Contains($"for {what}:"));
        }
        _ = Task.Run(() => Receive(socket, kind, port, _stop.Token));
        old?.Dispose();   // its receive loop ends on ObjectDisposedException
        Log.Write(oldPort > 0 ? $"Moved {what} from UDP {oldPort} to UDP {port}" : $"Listening for {what} on UDP {port}");
        return null;
    }

    public (bool ok, string message) Start()
    {
        lock (_gate)
        {
            if (Recording) return (false, "A capture is already running.");
            try
            {
                Directory.CreateDirectory(_settings.TraceFolderFull);
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                PcapPath = Path.Combine(_settings.TraceFolderFull, $"mediant-{stamp}.pcap");
                TextPath = Path.Combine(_settings.TraceFolderFull, $"mediant-{stamp}.txt");
                _pcap = new PcapWriter(PcapPath);
                _text = new StreamWriter(TextPath, false, new UTF8Encoding(false)) { NewLine = "\r\n" };
                _text.WriteLine($"MediantTrace capture started {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception e)
            {
                _pcap?.Dispose(); _pcap = null;
                _text?.Dispose(); _text = null;
                return (false, $"Could not create the capture files: {e.Message}");
            }
            Store.Clear();
            _syslogSip.Clear();
            _cdr.Clear();
            Opened = null;
            DrPackets = SyslogPackets = SipMessages = RtpPackets = Ignored = 0;
            Recording = true;
            StartedAt = DateTime.UtcNow;
            StopsAt = _settings.AutoStopMinutes > 0 ? StartedAt.Value.AddMinutes(_settings.AutoStopMinutes) : null;
            Log.Write($"Capture started: {Path.GetFileName(PcapPath)}");
            return (true, "Capture started.");
        }
    }

    public (bool ok, string message) Stop(string reason = "stopped from the page")
    {
        lock (_gate)
        {
            if (!Recording) return (false, "No capture is running.");
            Recording = false;
            StopsAt = null;
            _text?.WriteLine($"MediantTrace capture stopped {DateTime.Now:yyyy-MM-dd HH:mm:ss} ({reason})");
            _pcap?.Dispose(); _pcap = null;
            _text?.Dispose(); _text = null;
            Log.Write($"Capture stopped ({reason}): {DrPackets} Debug Recording and {SyslogPackets} syslog packets, {SipMessages} SIP messages");
        }
        ApplyRetention();
        return (true, "Capture stopped.");
    }

    /// <summary>Loads a saved capture into the call view. A .pcap is replayed; a .txt is used when there is no .pcap. Refused while a capture is recording.</summary>
    public (bool ok, string message) OpenSaved(string stem)
    {
        if (!CaptureFiles.TryStarted(stem, out _)) return (false, "That is not a saved capture.");
        string pcap = Path.Combine(_settings.TraceFolderFull, stem + ".pcap");
        string txt = Path.Combine(_settings.TraceFolderFull, stem + ".txt");
        bool hasPcap = File.Exists(pcap), hasTxt = File.Exists(txt);
        if (!hasPcap && !hasTxt) return (false, "That capture is not in the traces folder.");
        lock (_gate)
        {
            if (Recording) return (false, "Stop the capture that is running before opening a saved one.");
            ResetView();
            _forward = false;
            try
            {
                if (hasPcap) ReplayPcap(pcap);
                else ReplayText(txt);
                FinishSyslog();
            }
            catch (Exception e)
            {
                ResetView();
                return (false, "Could not open that capture: " + e.Message);
            }
            finally { _forward = true; }
            Opened = stem;
            PcapPath = hasPcap ? pcap : null;
            TextPath = hasTxt ? txt : null;
        }
        Log.Write($"Opened capture {stem}: {SipMessages} SIP messages, {Store.Count} calls");
        int n = Store.Count;
        return (true, $"Opened {stem}: {n} call{(n == 1 ? "" : "s")}, {SipMessages} SIP messages.");
    }

    /// <summary>
    /// Loads a log or capture made elsewhere into the call view and syslog view: a Wireshark or tcpdump
    /// capture (syslog, Debug Recording, or plain SIP on any UDP port), a MediantTrace .txt, or a syslog file
    /// from Syslog Viewer, acSyslog, a syslog server, Azure, Elastic, Excel, CSV or JSON (see <see cref="SyslogImport"/>).
    /// </summary>
    public (bool ok, string message) Import(string path, string name)
    {
        byte[] head = new byte[64];
        int got;
        using (var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) got = probe.Read(head);
        string start = Encoding.Latin1.GetString(head, 0, got);
        bool packets = CaptureReplay.IsPacketFile(head.AsSpan(0, got));
        bool ours = start.StartsWith("MediantTrace capture started", StringComparison.Ordinal) || Regex.IsMatch(start, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}  (?:syslog |=)");
        SyslogImport.Result? parsed = null;
        if (!packets && !ours)
        {
            try { parsed = SyslogImport.Read(path, name, File.GetLastWriteTime(path)); }
            catch (Exception e) when (e is InvalidOperationException or IOException or InvalidDataException or System.Xml.XmlException or System.Text.Json.JsonException)
            { return (false, $"Could not read {name}: {e.Message}"); }
            if (parsed.Lines.Count == 0) return (false, $"No syslog lines found in {name}.");
        }
        string format;
        lock (_gate)
        {
            if (Recording) return (false, "Stop the packet capture before opening a file.");
            ResetView();
            _forward = false;
            try
            {
                if (packets) { format = "packet capture"; ReplayPackets(path); }
                else if (ours) { format = "MediantTrace text"; ReplayText(path); }
                else
                {
                    format = parsed!.Format;
                    FeedLines(parsed.Lines, Path.GetFileNameWithoutExtension(name));
                }
                FinishSyslog();
            }
            catch (Exception e)
            {
                ResetView();
                return (false, $"Could not open {name}: {e.Message}");
            }
            finally { _forward = true; }
            Opened = $"{name} ({format})";
        }
        int n = Store.Count;
        var (_, _, lines) = Store.Counts();
        Log.Write($"Opened {name} ({format}): {lines} syslog lines, {SipMessages} SIP messages, {n} calls");
        return (true, $"Opened {name} ({format}): {lines:N0} syslog line{(lines == 1 ? "" : "s")}, {n} call{(n == 1 ? "" : "s")}, {SipMessages} SIP messages{(parsed?.Skipped > 0 ? $"; {parsed.Skipped} rows without a message skipped" : "")}.");
    }

    /// <summary>Syslog pasted on the page (Syslog Viewer's Ctrl+V), shown like an opened file.</summary>
    public (bool ok, string message) Paste(string text)
    {
        SyslogImport.Result parsed;
        try { parsed = SyslogImport.ReadPasted(text, DateTime.Now); }
        catch (Exception e) when (e is InvalidOperationException or System.Text.Json.JsonException) { return (false, "Could not read the pasted text: " + e.Message); }
        if (parsed.Lines.Count == 0) return (false, "Nothing to show in the pasted text.");
        lock (_gate)
        {
            if (Recording) return (false, "Stop the packet capture before pasting a log.");
            ResetView();
            _forward = false;
            try { FeedLines(parsed.Lines, "pasted"); FinishSyslog(); }
            finally { _forward = true; }
            Opened = $"pasted text ({parsed.Format})";
        }
        int n = Store.Count;
        return (true, $"Pasted {parsed.Lines.Count:N0} line{(parsed.Lines.Count == 1 ? "" : "s")} ({parsed.Format}): {n} call{(n == 1 ? "" : "s")}.");
    }

    /// <summary>Leaves an opened file and goes back to what the SBCs are sending now.</summary>
    public (bool ok, string message) BackToLive()
    {
        lock (_gate)
        {
            if (Opened is null) return (false, "The live view is already showing.");
            ResetView();
        }
        return (true, "Showing live traffic again.");
    }

    /// <summary>Empties the syslog view (and its warnings) without touching calls, streams or files.</summary>
    public void CleanSyslog()
    {
        lock (_gate) Store.ClearSyslog();
    }

    // Lines from an imported log, oldest first, through the same decoding as live syslog. Gaps in [S=n] are noted.
    private void FeedLines(List<SyslogImport.Line> lines, string fallbackSource)
    {
        string fallback = fallbackSource.Length > 40 ? fallbackSource[..40] : fallbackSource;
        var order = new SyslogSequence<PendingLine>(p => IngestSyslog(p.Time, p.Sbc, p.Line, false), NoteGap) { Reorder = false };
        // Some exports (Azure) list the newest first; keep the order of lines with the same time.
        foreach (var l in lines.Select((l, i) => (l, i)).OrderBy(x => x.l.Utc).ThenBy(x => x.i).Select(x => x.l))
        {
            string sbc = l.Source.Length > 0 ? l.Source : fallback;
            order.Offer(sbc, SyslogSequence<PendingLine>.NumberOf(l.Text), l.Utc, new PendingLine(l.Utc, sbc, l.Text));
        }
    }

    private void ReplayPackets(string path)
    {
        foreach (var packet in CaptureReplay.Pcap(path))
        {
            int port = packet.Destination.Port;
            string kind = port == _settings.DebugRecordingPort || port == 925 ? "DR"
                : port == _settings.SyslogPort || port == 514 ? "SYSLOG"
                : Acdr.Parse(packet.Payload) is not null ? "DR" : "";
            if (kind.Length > 0) { Ingest(kind, packet.Utc, packet.Source, packet.Payload); continue; }
            // Plain SIP in a capture taken on the network: both addresses are known.
            if (SipText.Find(packet.Payload) is { } m)
                RecordSip(packet.Utc, m, "PCAP", packet.Source.ToString(), packet.Destination.ToString(), null, "", "");
        }
    }

    /// <summary>
    /// Follows a log file as another program writes it (Syslog Viewer's monitor mode): what is already
    /// in it is shown, then each new line as it is added. Lines join the live syslog view.
    /// </summary>
    public (bool ok, string message) Watch(string path)
    {
        path = path.Trim().Trim('"');
        if (path.Length == 0) return (false, "Type the path of the file to follow.");
        if (!File.Exists(path)) return (false, $"There is no file {path} on this server.");
        StopWatching();
        var stop = new CancellationTokenSource();
        _watchStop = stop;
        Watching = path;
        WatchLines = 0;
        WatchError = null;
        _ = Task.Run(() => Follow(path, stop.Token));
        Log.Write($"Following {path}");
        return (true, $"Following {Path.GetFileName(path)}.");
    }

    public void StopWatching()
    {
        _watchStop?.Cancel();
        _watchStop = null;
        if (Watching is not null) Log.Write($"Stopped following {Watching}");
        Watching = null;
    }

    private async Task Follow(string path, CancellationToken ct)
    {
        long position = 0;
        var partial = new StringBuilder();
        string source = Path.GetFileNameWithoutExtension(path);
        DateTime last = DateTime.UtcNow;
        string? pendingSource = null, pendingText = null;
        DateTime pendingTime = default;
        void Emit()
        {
            if (pendingText is null) return;
            string sbc = pendingSource is { Length: > 0 } s ? s : source;
            lock (_gate) _sequence.Offer(sbc, SyslogSequence<PendingLine>.NumberOf(pendingText), pendingTime, new PendingLine(pendingTime, sbc, pendingText));
            WatchLines++;
            pendingText = null;
        }
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (file.Length < position) position = 0; // the file was rotated or emptied: start again
                file.Position = position;
                using var reader = new StreamReader(file, Encoding.UTF8, true, 64 * 1024, leaveOpen: true);
                string chunk = await reader.ReadToEndAsync(ct);
                position = file.Position;
                partial.Append(chunk);
                string all = partial.ToString();
                int end = all.LastIndexOf('\n');
                if (end >= 0)
                {
                    foreach (string raw in all[..end].Split('\n'))
                    {
                        string line = raw.TrimEnd('\r');
                        if (line.Length == 0) continue;
                        if (SyslogImport.TryRecord(line, DateTime.Now, last, out DateTime when, out string src, out string body, out _))
                        {
                            Emit();
                            pendingTime = when; last = when; pendingSource = src; pendingText = body;
                        }
                        else if (pendingText is not null) pendingText += "\n" + (line.StartsWith("  ", StringComparison.Ordinal) ? line.TrimStart() : line);
                        else { pendingTime = DateTime.UtcNow; pendingSource = ""; pendingText = line; }
                    }
                    partial.Clear().Append(all[(end + 1)..]);
                }
                WatchError = null;
            }
            catch (OperationCanceledException) { return; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                WatchError = e.Message;
            }
            try { await Task.Delay(500, ct); } catch (OperationCanceledException) { return; }
            Emit(); // a record is complete once the file has been quiet for a moment
        }
    }

    /// <summary>One G.711 stream of a call, as a WAV. The stream is the one shown on the call (ssrc, from, to).</summary>
    public (bool ok, string message, byte[]? wav) StreamWav(int id, string ssrc, string src, string dst)
    {
        string? path;
        CallStore.PacketSelect? select;
        lock (_gate)
        {
            path = PcapPath;
            select = Store.PacketSelectFor(id, DateTime.UtcNow);
        }
        if (select is null) return (false, "No such call.", null);
        if (ssrc.Length == 0 || src.Length == 0 || dst.Length == 0) return (false, "Choose a stream to play.", null);
        if (Store.AudioEncrypted(id, src, dst)) return (false, "This audio is encrypted, so it cannot be played.", null);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return (false, "This capture has no .pcap, so the audio cannot be played.", null);
        try
        {
            var rtp = new List<byte[]>();
            foreach (CaptureReplay.Datagram packet in CaptureReplay.Pcap(path))
            {
                if (!CallExport.Belongs(packet, select)) continue;
                if (CallAudio.TryStream(packet, ssrc, src, dst, out byte[] one)) rtp.Add(one);
            }
            return CallAudio.Build(rtp);
        }
        catch (Exception e)
        {
            return (false, "Could not read the capture: " + e.Message, null);
        }
    }

    /// <summary>The packets in the current capture that belong to one call, as a pcap. Empty when the capture has no pcap or nothing matches.</summary>
    public (bool ok, string message, byte[]? bytes, string name) CallPcap(int id)
    {
        string? path;
        CallStore.PacketSelect? select;
        lock (_gate)
        {
            path = PcapPath;
            select = Store.PacketSelectFor(id, DateTime.UtcNow);
        }
        if (select is null) return (false, "No such call.", null, "");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return (false, "This capture has no .pcap, so there are no packets to download.", null, "");
        try
        {
            var matched = new List<CaptureReplay.Datagram>();
            foreach (CaptureReplay.Datagram packet in CaptureReplay.Pcap(path))
                if (CallExport.Belongs(packet, select)) matched.Add(packet);
            if (matched.Count == 0) return (false, "No packets in the capture belong to this call.", null, "");
            return (true, "", CallExport.Pcap(matched), select.FileName);
        }
        catch (Exception e)
        {
            return (false, "Could not read the capture: " + e.Message, null, "");
        }
    }

    private void ResetView()
    {
        Store.Clear();
        _syslogSip.Clear();
        _cdr.Clear();
        Opened = null;
        PcapPath = null;
        TextPath = null;
        DrPackets = SyslogPackets = SipMessages = RtpPackets = Ignored = 0;
    }

    private void ReplayPcap(string path)
    {
        foreach (var packet in CaptureReplay.Pcap(path))
        {
            string kind = packet.Destination.Port == _settings.DebugRecordingPort || packet.Destination.Port == 925 ? "DR"
                : packet.Destination.Port == _settings.SyslogPort || packet.Destination.Port == 514 ? "SYSLOG"
                : Acdr.Parse(packet.Payload) is not null ? "DR" : "SYSLOG";
            Ingest(kind, packet.Utc, packet.Source, packet.Payload);
        }
    }

    private void ReplayText(string path)
    {
        CaptureReplay.ReadText(path,
            sip =>
            {
                SipText.Message? m = SipText.Find(sip.Text);
                if (m is not null) RecordSip(sip.Utc, m, sip.Source, sip.Src, sip.Dst, sip.Inbound, sip.SessionId, sip.Sbc);
            },
            line =>
            {
                SyslogPackets++;
                Store.AddSyslog(line.Utc, line.Sbc, line.Text);
                if (_cdr.Feed(line.Sbc, line.Text, line.Utc) is { } record) Store.AddCdr(line.Sbc, record);
                foreach (var a in Reassembler(line.Sbc, line.Text).Feed(line.Text, line.Utc)) RecordSyslogSip(a, line.Sbc);
            });
    }

    private void FinishSyslog()
    {
        DateTime later = DateTime.UtcNow.AddMinutes(5);
        foreach (var (key, r) in _syslogSip.ToList())
            foreach (var a in r.Expire(later)) RecordSyslogSip(a, key[..key.IndexOf('|')]);
    }

    /// <summary>Deletes captures past the age limit, then the oldest until under the size cap. Returns how many captures went.</summary>
    public int ApplyRetention()
    {
        long maxBytes = _settings.RetentionMaxMb <= 0 ? 0 : (long)_settings.RetentionMaxMb * 1024 * 1024;
        var gone = CaptureFiles.Prune(_settings.TraceFolderFull, IsOpenCapture, _settings.RetentionDays, maxBytes, DateTime.Now);
        if (gone.Count > 0) Log.Write($"Retention removed {gone.Count} capture{(gone.Count == 1 ? "" : "s")}: {string.Join(", ", gone)}");
        return gone.Count;
    }

    private bool IsOpenCapture(string path) =>
        Recording && (string.Equals(path, PcapPath, StringComparison.OrdinalIgnoreCase) || string.Equals(path, TextPath, StringComparison.OrdinalIgnoreCase));

    public List<SourceStats> Sources()
    {
        lock (_sources) return _sources.Values.Select(s => new SourceStats { Address = s.Address, Kind = s.Kind, Packets = s.Packets, LastSeen = s.LastSeen }).ToList();
    }

    private async Task Receive(Socket socket, string kind, int port, CancellationToken ct)
    {
        byte[] buffer = new byte[65536];
        var any = new IPEndPoint(IPAddress.Any, 0);
        while (!ct.IsCancellationRequested)
        {
            SocketReceiveMessageFromResult r;
            try { r = await socket.ReceiveMessageFromAsync(buffer, SocketFlags.None, any, ct); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            var source = (IPEndPoint)r.RemoteEndPoint;
            DateTime now = DateTime.UtcNow;
            if (!Count(kind, source, now)) continue;
            try
            {
                var destination = new IPEndPoint(r.PacketInformation.Address ?? IPAddress.Any, port);
                Handle(kind, now, source, destination, buffer.AsSpan(0, r.ReceivedBytes));
            }
            catch (Exception e)
            {
                Log.Write($"Packet from {source} could not be handled: {e.Message}");
            }
        }
    }

    private void Handle(string kind, DateTime now, IPEndPoint source, IPEndPoint destination, ReadOnlySpan<byte> data)
    {
        lock (_gate)
        {
            if (Recording && _pcap is not null) _pcap.Write(now, source, destination, data);
            if (kind == "SYSLOG")
            {
                // Put the SBC's numbered messages back in order first (UDP can overtake), then decode.
                string sbc = source.Address.ToString();
                string line = SipText.Printable(data);
                _sequence.Offer(sbc, SyslogSequence<PendingLine>.NumberOf(line), now, new PendingLine(now, sbc, line));
            }
            else Ingest(kind, now, source, data);
        }
    }

    /// <summary>Decodes one Debug Recording or syslog datagram into the call view. The caller holds <c>_gate</c>.</summary>
    private void Ingest(string kind, DateTime now, IPEndPoint source, ReadOnlySpan<byte> data)
    {
            string sbc = source.Address.ToString();

            if (kind == "DR")
            {
                DrPackets++;
                Acdr.Packet? p = Acdr.Parse(data);
                if (p is null) return;
                var payload = data.Slice(p.PayloadOffset, p.PayloadLength);
                if (p.IsRtp)
                {
                    RtpPackets++;
                    if (Opened is not null) return;
                    if (payload.Length < 12 || payload[0] >> 6 != 2)
                    {
                        if (SipText.LooksLikeDtls(payload) || SipText.LooksLikeTls(payload)) Store.NoteSealed(now, sbc, "dtls");
                    }
                    else
                        Store.AddRtp(now, p.Timestamp, true,
                            p.SrcIp?.ToString() ?? "", p.SrcPort, p.DstIp?.ToString() ?? "", p.DstPort, p.Inbound, payload, sbc, p.SessionId);
                }
                else if (!p.IsRtcp)
                {
                    // SIP, or any other packet type that carries a SIP message.
                    SipText.Message? m = SipText.Find(payload);
                    if (m is null)
                    {
                        if (SipText.LooksLikeTls(payload)) Store.NoteSealed(now, sbc, "tls");
                        return;
                    }
                    string src = p.SrcIp is null ? "" : $"{p.SrcIp}:{p.SrcPort}";
                    string dst = p.DstIp is null ? "" : $"{p.DstIp}:{p.DstPort}";
                    RecordSip(now, m, "DR", src, dst, p.Inbound, p.SessionId, sbc);
                }
            }
            else IngestSyslog(now, sbc, SipText.Printable(data), false);
    }

    /// <summary>One syslog message into the call view, the logs and the capture's .txt. The caller holds <c>_gate</c>.</summary>
    private void IngestSyslog(DateTime now, string sbc, string line, bool live)
    {
        foreach (Regex drop in _dropPatterns)
        {
            try { if (drop.IsMatch(line)) { Ignored++; return; } }
            catch (RegexMatchTimeoutException) { }
        }
        SyslogPackets++;
        var assembler = Reassembler(sbc, line);
        bool sipDump = line.Contains("SIP Message", StringComparison.OrdinalIgnoreCase) || assembler.Assembling || SipText.Find(line) is not null;
        if (Opened is null) Store.AddSyslog(now, sbc, line);
        if (_cdr.Feed(sbc, line, now) is { } record)
        {
            if (Opened is null) Store.AddCdr(sbc, record);
            if (_forward) ForwardCdr(now, sbc, record);
        }
        if (_text is not null) _text.WriteLine($"{now.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}  syslog {sbc}  {line}");
        if (live) _writeLog?.Write(now, sbc, line);
        if (_forward && !sipDump && line.Length > 0) ForwardSyslog(now, sbc, line);
        foreach (var a in assembler.Feed(line, now)) RecordSyslogSip(a, sbc);
    }

    private SyslogSip Reassembler(string sbc, string line)
    {
        string key = $"{sbc}|{SyslogSip.SessionOf(line)}";
        if (!_syslogSip.TryGetValue(key, out SyslogSip? r))
        {
            if (_syslogSip.Count > 5000) _syslogSip.Clear(); // calls long gone; pieces in flight are rare
            _syslogSip[key] = r = new SyslogSip();
        }
        return r;
    }

    private void RecordSyslogSip(SyslogSip.Assembled a, string sbc)
    {
        if (a.Kind != "SIP")
        {
            // HTTP, DNS or VoiceAI for a session: shown in that call's ladder.
            if (Opened is null) Store.AddOther(a.Time, a.Kind, a.Message.FirstLine, a.Message.Text, a.Inbound, a.Peer, a.SessionId, sbc);
            return;
        }
        // Syslog names only the far end; the Mediant is the other side.
        string src = a.Inbound == true ? a.Peer : "";
        string dst = a.Inbound == false ? a.Peer : "";
        RecordSip(a.Time, a.Message, "SYSLOG", src, dst, a.Inbound, a.SessionId, sbc);
    }

    private void RecordSip(DateTime time, SipText.Message m, string source, string src, string dst, bool? inbound, string sessionId, string sbc)
    {
        SipMessages++;
        if (_text is not null)
        {
            string dir = inbound switch { true => "IN ", false => "OUT", null => "   " };
            _text.WriteLine();
            _text.WriteLine($"===== {time.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}  SBC {SbcName(sbc)}  {dir} {src} -> {dst}  ({source})  Call-ID {m.CallId}" + (sessionId.Length > 0 ? $"  SID {sessionId}" : ""));
            _text.WriteLine(m.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }
        if (Opened is null) Store.AddSip(time, m, source, src, dst, inbound, sessionId, sbc);
        if (_forward) ForwardSip(time, m, source, src, dst, inbound, sessionId, sbc);
    }

    private void ForwardSip(DateTime time, SipText.Message m, string source, string src, string dst, bool? inbound, string sessionId, string sbc)
    {
        _logs?.Write(new
        {
            time = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            sbc,
            sbcName = SbcName(sbc),
            kind = "sip",
            channel = source,
            dir = inbound switch { true => "in", false => "out", _ => "" },
            src,
            dst,
            callId = m.CallId,
            session = sessionId,
            method = m.IsRequest ? m.Method ?? "" : m.CSeqMethod,
            status = m.Status,
            reason = m.Reason,
            from = Party(m.From),
            to = Party(m.To),
            transport = m.Transport,
            summary = m.FirstLine.Length > 300 ? m.FirstLine[..300] : m.FirstLine,
        });
    }

    private void ForwardCdr(DateTime time, string sbc, CdrDecoder.Record record)
    {
        _logs?.Write(new
        {
            time = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            sbc,
            sbcName = SbcName(sbc),
            kind = "cdr",
            report = record.Kind,
            family = record.Family,
            callId = record.Get("SIPCallId"),
            session = record.Get("SessionId"),
            from = record.Get("SrcPhoneNum", "Caller"),
            to = record.Get("DstPhoneNum", "Callee"),
            duration = record.Get("Durat"),
            codec = record.Get("Coder"),
            inPackets = record.Get("InPackets"),
            outPackets = record.Get("OutPackets"),
            loss = record.Get("PackLoss", "LocalPackLoss"),
            jitter = record.Get("RTPjitter"),
            mosIn = record.Get("LocalMOSCQ", "LocalMOS"),
            mosOut = record.Get("RemoteMOSCQ", "RemoteMOS"),
            endReason = record.Get("TrmReason"),
            transport = record.Get("SigTransportType"),
        });
    }

    private void ForwardSyslog(DateTime time, string sbc, string line)
    {
        _logs?.Write(new
        {
            time = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            sbc,
            sbcName = SbcName(sbc),
            kind = "syslog",
            text = line.Length > 2000 ? line[..2000] : line,
        });
    }

    private static string Party(string header)
    {
        int sip = header.IndexOf("sip:", StringComparison.OrdinalIgnoreCase);
        int take = 4;
        if (sip < 0) { sip = header.IndexOf("sips:", StringComparison.OrdinalIgnoreCase); take = 5; }
        if (sip < 0) return header.Length > 80 ? header[..80] : header;
        string rest = header[(sip + take)..];
        int end = rest.IndexOfAny(['@', '>', ';']);
        string user = end > 0 ? rest[..end] : rest;
        return user.Length > 80 ? user[..80] : user;
    }

    private string SbcName(string ip) => _settings.FindSbc(ip) is { Name.Length: > 0 } s ? $"{s.Name} ({ip})" : ip;

    private async Task Housekeeping(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(1000, ct); } catch (OperationCanceledException) { return; }
            lock (_gate)
            {
                _pcap?.Flush();
                _text?.Flush();
                _writeLog?.Flush();
                // The call view is fed all the time, so a message whose last piece never came is finished
                // whether or not a packet capture is running.
                foreach (var (key, r) in _syslogSip)
                    foreach (var a in r.Expire(DateTime.UtcNow)) RecordSyslogSip(a, key[..key.IndexOf('|')]);
            }
            if (Recording && StopsAt is { } at && DateTime.UtcNow >= at)
                Stop($"automatic stop after {_settings.AutoStopMinutes} minutes");
        }
    }

    public void Dispose()
    {
        if (Recording) Stop("program closing");
        StopWatching();
        lock (_streamGate) { foreach (var l in _streams.Values) l.Dispose(); _streams.Clear(); }
        lock (_gate) { _sequence.Flush(DateTime.UtcNow); _writeLog?.Dispose(); _writeLog = null; }
        _logs?.Dispose();
        _stop.Cancel();
        lock (_feedGate) foreach (Socket s in _sockets) s.Dispose();
    }
}
