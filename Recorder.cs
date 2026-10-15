using System.Net;
using System.Net.Sockets;
using System.Text;

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
    private readonly CancellationTokenSource _stop = new();
    // One reassembler per SBC and session: pieces from different SBCs and calls arrive interleaved.
    private readonly Dictionary<string, SyslogSip> _syslogSip = new();
    private readonly Dictionary<string, SourceStats> _sources = new();
    private readonly CdrDecoder _cdr = new();

    private PcapWriter? _pcap;
    private StreamWriter? _text;

    public CallStore Store { get; } = new();
    public bool Recording { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? StopsAt { get; private set; }
    public string? PcapPath { get; private set; }
    public string? TextPath { get; private set; }
    public long DrPackets, SyslogPackets, SipMessages, RtpPackets, Ignored;
    public List<string> ListenErrors { get; } = [];

    public sealed class SourceStats
    {
        public string Address = "";
        public string Kind = "";
        public long Packets;
        public DateTime LastSeen;
    }

    public Recorder(Settings settings) => _settings = settings;

    public void Open()
    {
        TryListen(_settings.DebugRecordingPort, "DR");
        TryListen(_settings.SyslogPort, "SYSLOG");
        _ = Task.Run(() => Housekeeping(_stop.Token));
    }

    private void TryListen(int port, string kind)
    {
        try
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            socket.ReceiveBufferSize = 8 * 1024 * 1024;
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _sockets.Add(socket);
            _ = Task.Run(() => Receive(socket, kind, port, _stop.Token));
            Log.Write($"Listening for {(kind == "DR" ? "Debug Recording" : "syslog")} on UDP {port}");
        }
        catch (SocketException e)
        {
            string msg = $"Could not listen on UDP {port} for {(kind == "DR" ? "Debug Recording" : "syslog")}: {e.Message}";
            ListenErrors.Add(msg);
            Log.Write(msg);
        }
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
            return (true, "Capture stopped.");
        }
    }

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
            if (_settings.OnlyListedSbcs && _settings.FindSbc(source.Address.ToString()) is null)
            {
                Interlocked.Increment(ref Ignored);
                continue;
            }
            lock (_sources)
            {
                string key = $"{kind}|{source.Address}";
                if (!_sources.TryGetValue(key, out SourceStats? st))
                    _sources[key] = st = new SourceStats { Address = source.Address.ToString(), Kind = kind };
                st.Packets++;
                st.LastSeen = now;
            }
            if (!Recording) continue;

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
            if (!Recording || _pcap is null || _text is null) return;
            _pcap.Write(now, source, destination, data);
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
                    Store.AddRtp(now, p.Timestamp, true,
                        p.SrcIp?.ToString() ?? "", p.SrcPort, p.DstIp?.ToString() ?? "", p.DstPort, p.Inbound, payload, sbc, p.SessionId);
                }
                else if (!p.IsRtcp)
                {
                    // SIP, or any other packet type that carries a SIP message.
                    SipText.Message? m = SipText.Find(payload);
                    if (m is null) return;
                    string src = p.SrcIp is null ? "" : $"{p.SrcIp}:{p.SrcPort}";
                    string dst = p.DstIp is null ? "" : $"{p.DstIp}:{p.DstPort}";
                    RecordSip(now, m, "DR", src, dst, p.Inbound, p.SessionId, sbc);
                }
            }
            else
            {
                SyslogPackets++;
                string line = SipText.Printable(data);
                Store.AddSyslog(now, source.Address.ToString(), line);
                if (_cdr.Feed(sbc, line, now) is { } record) Store.AddCdr(sbc, record);
                _text.WriteLine($"{now.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}  syslog {source.Address}  {line}");
                foreach (var a in Reassembler(sbc, line).Feed(line, now)) RecordSyslogSip(a, sbc);
            }
        }
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
        // Syslog names only the far end; the Mediant is the other side.
        string src = a.Inbound == true ? a.Peer : "";
        string dst = a.Inbound == false ? a.Peer : "";
        RecordSip(a.Time, a.Message, "SYSLOG", src, dst, a.Inbound, a.SessionId, sbc);
    }

    private void RecordSip(DateTime time, SipText.Message m, string source, string src, string dst, bool? inbound, string sessionId, string sbc)
    {
        SipMessages++;
        string dir = inbound switch { true => "IN ", false => "OUT", null => "   " };
        _text!.WriteLine();
        _text.WriteLine($"===== {time.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}  SBC {SbcName(sbc)}  {dir} {src} -> {dst}  ({source})  Call-ID {m.CallId}" + (sessionId.Length > 0 ? $"  SID {sessionId}" : ""));
        _text.WriteLine(m.Text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        Store.AddSip(time, m, source, src, dst, inbound, sessionId, sbc);
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
                if (Recording)
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
        _stop.Cancel();
        foreach (Socket s in _sockets) s.Dispose();
    }
}
