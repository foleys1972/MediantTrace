using System.Text;

namespace MediantTrace;

/// <summary>
/// "Write log": every syslog line to a plain text file, as AudioCodes Syslog Viewer writes it, so the
/// file opens in Notepad, in Syslog Viewer and back in MediantTrace. One line per message:
///   2026-10-14 08:27:01.154  192.0.2.10  local0.notice  [S=1234] [SID=...] ...
/// with the further lines of a message indented by two spaces.
///
/// The current file always has the name chosen (syslog.txt); when it reaches the size limit it is
/// renamed syslog_0001.txt, syslog_0002.txt and so on (the highest number is the newest), and the
/// oldest are removed beyond the file count. Size 0: never rotate. Count 0: keep every rotated file.
/// Optionally one file per SBC, in one folder or a folder per SBC, named by the SBC's name or address.
/// </summary>
public sealed class SyslogFileLog : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Writer> _writers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, string> _label;

    public string Folder { get; }
    public string BaseName { get; }
    public long MaxBytes { get; }
    public int MaxFiles { get; }
    public bool PerDevice { get; }
    public bool FolderPerDevice { get; }
    public bool UseNames { get; }
    public long Lines { get; private set; }
    public string? Error { get; private set; }

    private sealed class Writer
    {
        public string Path = "";
        public StreamWriter? Stream;
        public long Bytes;
    }

    /// <param name="label">The SBC's name for an address ("" when it has none).</param>
    public SyslogFileLog(string folder, string baseName, int maxMb, int maxFiles, bool perDevice, bool folderPerDevice, bool useNames, Func<string, string> label)
    {
        Folder = folder;
        BaseName = string.IsNullOrWhiteSpace(baseName) ? "syslog.txt" : System.IO.Path.GetFileName(baseName.Trim());
        if (System.IO.Path.GetExtension(BaseName).Length == 0) BaseName += ".txt";
        MaxBytes = Math.Max(0, maxMb) * 1024L * 1024;
        MaxFiles = Math.Max(0, maxFiles);
        PerDevice = perDevice;
        FolderPerDevice = folderPerDevice;
        UseNames = useNames;
        _label = label;
    }

    /// <summary>One line as written to the file (also used for the page's Save As).</summary>
    public static string Format(DateTime utc, string source, string text, bool localTime = true)
    {
        string pri = "";
        string body = text;
        int sev = CallStore.Severity(text);
        if (sev >= 0)
        {
            int end = text.IndexOf('>');
            if (int.TryParse(text.AsSpan(1, end - 1), out int p)) pri = SyslogImport.FacilityText(p);
            body = text[(end + 1)..].TrimStart();
        }
        else pri = "user.notice";
        DateTime when = localTime ? utc.ToLocalTime() : utc;
        string lines = body.Replace("\r\n", "\n").TrimEnd('\n').Replace("\n", "\r\n  ");
        return $"{when:yyyy-MM-dd HH:mm:ss.fff}  {(source.Length > 0 ? source : "-")}  {pri}  {lines}";
    }

    public void Write(DateTime utc, string source, string text)
    {
        lock (_gate)
        {
            try
            {
                Writer w = WriterFor(source);
                string line = Format(utc, source, text) + "\r\n";
                int size = Encoding.UTF8.GetByteCount(line);
                if (MaxBytes > 0 && w.Bytes > 0 && w.Bytes + size > MaxBytes) Rotate(w);
                w.Stream ??= Open(w);
                w.Stream.Write(line);
                w.Bytes += size;
                Lines++;
                Error = null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                if (Error is null) Log.Write($"Write log: {e.Message}");
                Error = e.Message;
            }
        }
    }

    /// <summary>The file a line from this source goes to now.</summary>
    public string CurrentFile(string source = "")
    {
        lock (_gate) return WriterFor(source).Path;
    }

    private Writer WriterFor(string source)
    {
        string key = PerDevice || FolderPerDevice ? source : "";
        if (_writers.TryGetValue(key, out Writer? w)) return w;
        string folder = Folder;
        string name = BaseName;
        if (key.Length > 0)
        {
            string device = Safe(UseNames && _label(source) is { Length: > 0 } n ? n : source);
            if (FolderPerDevice) folder = System.IO.Path.Combine(folder, device);
            if (PerDevice) name = $"{System.IO.Path.GetFileNameWithoutExtension(BaseName)}_{device}{System.IO.Path.GetExtension(BaseName)}";
        }
        Directory.CreateDirectory(folder);
        string path = System.IO.Path.Combine(folder, name);
        w = new Writer { Path = path, Bytes = File.Exists(path) ? new FileInfo(path).Length : 0 };
        _writers[key] = w;
        return w;
    }

    private static StreamWriter Open(Writer w) =>
        new(new FileStream(w.Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), new UTF8Encoding(false)) { AutoFlush = false };

    private void Rotate(Writer w)
    {
        w.Stream?.Dispose();
        w.Stream = null;
        string dir = System.IO.Path.GetDirectoryName(w.Path)!;
        string stem = System.IO.Path.GetFileNameWithoutExtension(w.Path), ext = System.IO.Path.GetExtension(w.Path);
        var numbered = Rotated(dir, stem, ext);
        int next = numbered.Count == 0 ? 1 : numbered[^1].n + 1;
        File.Move(w.Path, System.IO.Path.Combine(dir, $"{stem}_{next:0000}{ext}"));
        w.Bytes = 0;
        if (MaxFiles > 0)
        {
            numbered = Rotated(dir, stem, ext);
            // The current file counts as one of the files kept.
            foreach (var (_, path) in numbered.Take(Math.Max(0, numbered.Count - (MaxFiles - 1))))
                try { File.Delete(path); } catch (IOException) { }
        }
    }

    private static List<(int n, string path)> Rotated(string dir, string stem, string ext)
    {
        var list = new List<(int, string)>();
        foreach (string f in Directory.EnumerateFiles(dir, $"{stem}_*{ext}"))
        {
            string middle = System.IO.Path.GetFileNameWithoutExtension(f)[(stem.Length + 1)..];
            if (middle.Length >= 4 && int.TryParse(middle, out int n)) list.Add((n, f));
        }
        return list.OrderBy(x => x.Item1).ToList();
    }

    private static string Safe(string name)
    {
        var sb = new StringBuilder();
        foreach (char c in name) sb.Append(System.IO.Path.GetInvalidFileNameChars().Contains(c) || c == ' ' ? '_' : c);
        string s = sb.ToString().Trim('.', '_');
        return s.Length == 0 ? "unknown" : s.Length > 60 ? s[..60] : s;
    }

    public void Flush()
    {
        lock (_gate)
            foreach (Writer w in _writers.Values)
                try { w.Stream?.Flush(); } catch (IOException) { }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (Writer w in _writers.Values) try { w.Stream?.Dispose(); } catch (IOException) { }
            _writers.Clear();
        }
    }
}
