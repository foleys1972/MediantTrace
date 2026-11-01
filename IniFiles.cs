using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MediantTrace;

/// <summary>
/// One standard AudioCodes .ini per SBC, stored next to the program, and a download
/// of the INI the device is running. Download is read-only: it never uploads a file
/// (a full upload would reset the Mediant).
/// </summary>
public static class IniFiles
{
    public const int MaxBytes = 8 * 1024 * 1024;
    public static string Folder => Path.Combine(AppContext.BaseDirectory, "standards");

    public static string FileFor(string ip)
    {
        if (!IPAddress.TryParse(ip, out IPAddress? addr) || addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new InvalidOperationException("That is not an SBC address.");
        return Path.Combine(Folder, addr + ".ini");
    }

    public static List<object> List(IEnumerable<string> ips)
    {
        var list = new List<object>();
        foreach (string ip in ips)
        {
            string path;
            try { path = FileFor(ip); }
            catch (InvalidOperationException) { continue; }
            if (!File.Exists(path)) continue;
            var info = new FileInfo(path);
            list.Add(new { ip, bytes = info.Length, savedAt = info.LastWriteTimeUtc });
        }
        return list;
    }

    public static string? Read(string ip)
    {
        string path = FileFor(ip);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public static void Save(string ip, string text)
    {
        if (!LooksLikeIni(text)) throw new InvalidOperationException("That file is not an AudioCodes .ini file.");
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new InvalidOperationException("That file is larger than 8 MB.");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FileFor(ip), text);
    }

    public static bool LooksLikeIni(string text) =>
        text.Contains('=') && text.Contains('[');

    /// <summary>GET /api/v1/files/ini. Uses the saved login. Accepts the Mediant's own certificate for that host only.</summary>
    public static async Task<string> Download(string host, string user, string password, string? pageUrl)
    {
        Uri first = Target(host, pageUrl);
        try { return await Get(first, user, password); }
        catch (HttpRequestException) when (first.Scheme == "https" && pageUrl is not { Length: > 0 })
        {
            return await Get(new Uri($"http://{host}/api/v1/files/ini"), user, password);
        }
    }

    private static Uri Target(string host, string? pageUrl)
    {
        if (Uri.TryCreate(pageUrl, UriKind.Absolute, out Uri? page) && page.Scheme is "https" or "http")
            return new UriBuilder(page.Scheme, page.Host, page.IsDefaultPort ? -1 : page.Port, "/api/v1/files/ini").Uri;
        return new Uri($"https://{host}/api/v1/files/ini");
    }

    private static async Task<string> Get(Uri uri, string user, string password)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        handler.ServerCertificateCustomValidationCallback = (req, _, _, _) =>
            req.RequestUri?.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase) == true;
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(user + ":" + password)));
        using HttpResponseMessage res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("The SBC refused the saved login for its web page.");
        if (res.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently or HttpStatusCode.Found)
            throw new InvalidOperationException("The SBC redirected the download somewhere else, so it was not followed.");
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"The SBC returned {(int)res.StatusCode} instead of the INI file.");
        var buf = new byte[81920];
        await using Stream stream = await res.Content.ReadAsStreamAsync();
        using var mem = new MemoryStream();
        int n, total = 0;
        while ((n = await stream.ReadAsync(buf)) > 0)
        {
            total += n;
            if (total > MaxBytes) throw new InvalidOperationException("The INI file is larger than 8 MB.");
            mem.Write(buf, 0, n);
        }
        string text = Encoding.UTF8.GetString(mem.ToArray()).TrimStart('\uFEFF');
        if (!LooksLikeIni(text)) throw new InvalidOperationException("The SBC did not return an INI file.");
        return text;
    }
}
