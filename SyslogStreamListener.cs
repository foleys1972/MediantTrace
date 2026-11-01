using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MediantTrace;

/// <summary>
/// Syslog over TCP (RFC 6587) and over TLS (RFC 5425). Each message is either octet-counted
/// ("123 &lt;134&gt;...") or ends with a line feed (or NUL); both are accepted on one connection.
/// Every message is handed on as if it had arrived by UDP, so the rest of MediantTrace (capture,
/// call view, logs) treats it the same.
/// </summary>
public sealed class SyslogStreamListener : IDisposable
{
    public delegate void MessageHandler(DateTime now, IPEndPoint source, IPEndPoint destination, byte[] message);

    private readonly TcpListener _listener;
    private readonly X509Certificate2? _certificate;
    private readonly MessageHandler _handler;
    private readonly Func<IPAddress, bool> _allowed;
    private readonly CancellationTokenSource _stop = new();
    private int _connections;

    public int Port { get; }
    public bool Secure => _certificate is not null;
    public int Connections => _connections;
    private const int MaxMessage = 256 * 1024;
    private const int MaxConnections = 200;

    private SyslogStreamListener(TcpListener listener, int port, X509Certificate2? certificate, MessageHandler handler, Func<IPAddress, bool> allowed)
    {
        _listener = listener;
        Port = port;
        _certificate = certificate;
        _handler = handler;
        _allowed = allowed;
    }

    /// <summary>Starts listening, or returns the reason it could not.</summary>
    public static (SyslogStreamListener? listener, string? error) Start(int port, X509Certificate2? certificate, MessageHandler handler, Func<IPAddress, bool> allowed)
    {
        var tcp = new TcpListener(IPAddress.Any, port);
        tcp.Server.ExclusiveAddressUse = true;
        try { tcp.Start(); }
        catch (SocketException e)
        {
            string reason = e.SocketErrorCode == SocketError.AddressAlreadyInUse ? "another program is using it" : e.Message;
            return (null, $"Could not listen on TCP {port} for syslog{(certificate is null ? "" : " over TLS")}: {reason}.");
        }
        var l = new SyslogStreamListener(tcp, port, certificate, handler, allowed);
        _ = Task.Run(l.Accept);
        Log.Write($"Listening for syslog {(certificate is null ? "over TCP" : "over TLS")} on TCP {port}");
        return (l, null);
    }

    private async Task Accept()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }
            var remote = (IPEndPoint)client.Client.RemoteEndPoint!;
            if (!_allowed(remote.Address) || _connections >= MaxConnections) { client.Dispose(); continue; }
            _ = Task.Run(() => Serve(client, remote));
        }
    }

    private async Task Serve(TcpClient client, IPEndPoint remote)
    {
        Interlocked.Increment(ref _connections);
        var local = (IPEndPoint)client.Client.LocalEndPoint!;
        try
        {
            using (client)
            {
                client.ReceiveTimeout = 0;
                Stream stream = client.GetStream();
                if (_certificate is not null)
                {
                    var tls = new SslStream(stream, false);
                    using var handshake = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    handshake.CancelAfter(TimeSpan.FromSeconds(15));
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _certificate,
                        ClientCertificateRequired = false,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    }, handshake.Token);
                    stream = tls;
                }
                await using (stream)
                    await Read(stream, remote, new IPEndPoint(local.Address.IsIPv4MappedToIPv6 ? local.Address.MapToIPv4() : local.Address, Port));
            }
        }
        catch (Exception e) when (e is IOException or AuthenticationException or OperationCanceledException or ObjectDisposedException or SocketException)
        {
            if (e is AuthenticationException) Log.Write($"Syslog TLS from {remote.Address} failed: {e.Message}");
        }
        finally { Interlocked.Decrement(ref _connections); }
    }

    private async Task Read(Stream stream, IPEndPoint remote, IPEndPoint local)
    {
        var buffer = new byte[64 * 1024];
        var pending = new MemoryStream();
        while (!_stop.IsCancellationRequested)
        {
            int n = await stream.ReadAsync(buffer, _stop.Token);
            if (n == 0) break;
            pending.Write(buffer, 0, n);
            byte[] data = pending.GetBuffer();
            int length = (int)pending.Length, at = 0;
            foreach (var (start, count) in Frames(data, length, ref at))
                _handler(DateTime.UtcNow, remote, local, data.AsSpan(start, count).ToArray());
            if (length - at > MaxMessage) { at = length; }   // a message with no end: drop it rather than grow for ever
            byte[] rest = data.AsSpan(at, length - at).ToArray();
            pending.SetLength(0);
            pending.Write(rest);
        }
        if (pending.Length > 0) _handler(DateTime.UtcNow, remote, local, pending.ToArray());
    }

    /// <summary>The complete messages in a buffer, as (start, length); <paramref name="at"/> moves past them.</summary>
    internal static List<(int start, int count)> Frames(byte[] data, int length, ref int at)
    {
        var frames = new List<(int, int)>();
        while (at < length)
        {
            // Skip line ends between messages.
            while (at < length && data[at] is (byte)'\n' or (byte)'\r' or 0) at++;
            if (at >= length) break;
            if (data[at] is >= (byte)'1' and <= (byte)'9')
            {
                // Octet counting: digits, a space, then that many bytes.
                int i = at, size = 0;
                while (i < length && data[i] is >= (byte)'0' and <= (byte)'9' && i - at < 7) size = size * 10 + (data[i++] - '0');
                if (i < length && data[i] == (byte)' ' && size > 0)
                {
                    if (i + 1 + size > length) break; // wait for the rest
                    frames.Add((i + 1, size));
                    at = i + 1 + size;
                    continue;
                }
                if (i >= length) break; // digits so far, wait
            }
            int end = Array.FindIndex(data, at, length - at, b => b is (byte)'\n' or 0);
            if (end < 0) break;
            int stop = end > at && data[end - 1] == (byte)'\r' ? end - 1 : end;
            frames.Add((at, stop - at));
            at = end + 1;
        }
        return frames;
    }

    /// <summary>
    /// The certificate MediantTrace shows the SBC for syslog over TLS: the .pfx named in settings, or one it
    /// makes itself the first time (self-signed, 10 years, saved next to the program and protected with Windows DPAPI).
    /// Windows TLS (SChannel) cannot use a key held only in memory, so the key goes to a temporary key container
    /// that Windows removes when the certificate is released.
    /// </summary>
    public static X509Certificate2 Certificate(Settings settings)
    {
        if (settings.SyslogTlsPfx.Length > 0)
        {
            string path = Settings.FullFolder(settings.SyslogTlsPfx, settings.SyslogTlsPfx);
            return new X509Certificate2(File.ReadAllBytes(path), settings.SyslogTlsPfxPassword, X509KeyStorageFlags.DefaultKeySet);
        }
        string own = Path.Combine(AppContext.BaseDirectory, "syslog-tls.pfx.dpapi");
        if (File.Exists(own))
        {
            try
            {
                byte[] pfx = ProtectedData.Unprotect(File.ReadAllBytes(own), null, DataProtectionScope.LocalMachine);
                return new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.DefaultKeySet);
            }
            catch (CryptographicException) { Log.Write("The saved syslog TLS certificate could not be opened; making a new one"); }
        }
        using RSA key = RSA.Create(2048);
        string host = Dns.GetHostName();
        var request = new CertificateRequest($"CN={host}, O=MediantTrace syslog", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(host);
        foreach (IPAddress a in Dns.GetHostAddresses(host).Where(a => a.AddressFamily == AddressFamily.InterNetwork)) names.AddIpAddress(a);
        request.CertificateExtensions.Add(names.Build());
        using X509Certificate2 made = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        byte[] export = made.Export(X509ContentType.Pfx);
        File.WriteAllBytes(own, ProtectedData.Protect(export, null, DataProtectionScope.LocalMachine));
        Log.Write($"Made a certificate for syslog over TLS ({made.Thumbprint}); give the SBC syslog-tls.cer from the Settings page to trust it");
        return new X509Certificate2(export, (string?)null, X509KeyStorageFlags.DefaultKeySet);
    }

    public void Dispose()
    {
        _stop.Cancel();
        try { _listener.Stop(); } catch (SocketException) { }
    }
}
