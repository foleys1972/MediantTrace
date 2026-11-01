using System.Text;
using System.Text.Json;

namespace MediantTrace;

/// <summary>
/// One JSON event per line, in a folder per day. A file is closed when it
/// reaches the size limit and another is started, so a Splunk forwarder can
/// read a file without it growing forever.
/// </summary>
public sealed class SplunkLog : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly object _gate = new();
    private readonly string _root;
    private readonly Func<int> _maxBytes;
    private readonly Func<int> _keepDays;
    private StreamWriter? _file;
    private string _path = "";
    private DateTime _day;
    private long _bytes;
    private DateTime _pruned;
    private bool _closed;

    public SplunkLog(string root, Func<int> maxBytes, Func<int> keepDays)
    {
        _root = root;
        _maxBytes = maxBytes;
        _keepDays = keepDays;
        Directory.CreateDirectory(Path.Combine(_root, DateTime.Now.ToString("yyyy-MM-dd")));
    }

    public string Root => _root;
    public string FileName => Path.GetFileName(_path);

    public void Write(object fields)
    {
        string line = JsonSerializer.Serialize(fields, Json);
        int size = Encoding.UTF8.GetByteCount(line) + 1;
        lock (_gate)
        {
            if (_closed) return;   // replaced by a log in another folder
            DateTime today = DateTime.Now.Date;
            bool roll = _file is null || today != _day || (_bytes > 0 && _bytes + size > Math.Max(1, _maxBytes()));
            if (roll) Open(today);
            _file!.WriteLine(line);
            _bytes += size;
            if (today != _pruned.Date) { _pruned = today; Prune(_root, _keepDays()); }
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            _file?.Dispose();
            _file = null;
        }
    }

    public void Dispose()
    {
        lock (_gate) _closed = true;
        Close();
    }

    /// <summary>Removes the oldest day folders (never today's) until the logs fit in <paramref name="maxBytes"/>; 0 = no cap.</summary>
    public static int PruneToSize(string root, long maxBytes)
    {
        if (maxBytes <= 0 || !Directory.Exists(root)) return 0;
        var days = Directory.GetDirectories(root)
            .Select(d => (dir: d, ok: DateTime.TryParseExact(Path.GetFileName(d), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime day), day))
            .Where(x => x.ok).OrderBy(x => x.day).ToList();
        long Size(string d) { try { return new DirectoryInfo(d).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); } catch { return 0; } }
        long total = days.Sum(x => Size(x.dir));
        int removed = 0;
        foreach (var x in days.Where(x => x.day < DateTime.Now.Date))
        {
            if (total <= maxBytes) break;
            long size = Size(x.dir);
            try { Directory.Delete(x.dir, true); total -= size; removed++; } catch { /* in use */ }
        }
        return removed;
    }

    /// <summary>Removes day folders strictly older than <paramref name="keepDays"/>.</summary>
    public static int Prune(string root, int keepDays)
    {
        if (keepDays < 1 || !Directory.Exists(root)) return 0;
        DateTime limit = DateTime.Now.Date.AddDays(-keepDays);
        int removed = 0;
        foreach (string dir in Directory.GetDirectories(root))
        {
            if (!DateTime.TryParseExact(Path.GetFileName(dir), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime day)) continue;
            if (day >= limit) continue;
            try { Directory.Delete(dir, true); removed++; }
            catch { /* a forwarder may still have a file open */ }
        }
        return removed;
    }

    private void Open(DateTime day)
    {
        _file?.Dispose();
        string dir = Path.Combine(_root, day.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        string stamp = DateTime.Now.ToString("HHmmss");
        string path = Path.Combine(dir, $"mediant-{stamp}.log");
        for (int n = 2; File.Exists(path); n++) path = Path.Combine(dir, $"mediant-{stamp}-{n}.log");
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
        _file = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        _path = path;
        _day = day;
        _bytes = 0;
    }
}
