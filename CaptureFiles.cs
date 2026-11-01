using System.Globalization;
using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>
/// Saved captures (mediant-yyyyMMdd-HHmmss.pcap and .txt) in the trace folder.
/// Retention deletes whole captures by age, then the oldest until under a size cap.
/// The capture currently being written is never deleted.
/// </summary>
public static class CaptureFiles
{
    private static readonly Regex StemPattern = new(@"^mediant-\d{8}-\d{6}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public sealed record Item(string Name, long Size, DateTime Modified, bool Active);

    public static bool TryStarted(string stem, out DateTime started)
    {
        started = default;
        if (!StemPattern.IsMatch(stem)) return false;
        return DateTime.TryParseExact(stem.AsSpan("mediant-".Length), "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out started);
    }

    public static List<Item> List(string folder, Func<string, bool> isActive)
    {
        var list = new List<Item>();
        if (!Directory.Exists(folder)) return list;
        foreach (FileInfo file in new DirectoryInfo(folder).EnumerateFiles("mediant-*.*"))
        {
            if (!file.Extension.Equals(".pcap", StringComparison.OrdinalIgnoreCase) && !file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;
            if (!TryStarted(Path.GetFileNameWithoutExtension(file.Name), out _)) continue;
            list.Add(new Item(file.Name, file.Length, file.LastWriteTimeUtc, isActive(file.FullName)));
        }
        return list.OrderByDescending(f => f.Modified).ToList();
    }

    /// <summary>Free space on the drive that holds the trace folder. Null when the drive cannot be read.</summary>
    public static (long? free, string? drive) FreeSpace(string folder)
    {
        try
        {
            string root = Path.GetPathRoot(Path.GetFullPath(folder)) ?? "";
            if (root.Length == 0) return (null, null);
            var drive = new DriveInfo(root);
            return (drive.AvailableFreeSpace, drive.Name.TrimEnd('\\'));
        }
        catch (Exception) { return (null, null); }
    }

    /// <summary>Deletes one capture's .pcap and .txt. The name must be a capture stem, inside the folder, and not being recorded.</summary>
    public static IReadOnlyList<string> Delete(string folder, string stem, Func<string, bool> isProtected)
    {
        if (!TryStarted(stem, out _)) throw new InvalidOperationException("That is not a saved capture.");
        var existing = new List<string>();
        foreach (string ext in new[] { ".pcap", ".txt" })
        {
            string path = InFolder(folder, stem, ext);
            if (File.Exists(path)) existing.Add(path);
        }
        if (existing.Count == 0) throw new InvalidOperationException("That capture is already gone.");
        if (existing.Any(isProtected)) throw new InvalidOperationException("That capture is still being recorded. Stop it first.");
        foreach (string path in existing) File.Delete(path);
        return existing.Select(Path.GetFileName).ToList()!;
    }

    /// <summary>Deletes captures older than <paramref name="keepDays"/> (0 skips the age check), then the oldest until the rest fit in <paramref name="maxBytes"/> (0 skips the cap). Returns the stems removed.</summary>
    public static IReadOnlyList<string> Prune(string folder, Func<string, bool> isProtected, int keepDays, long maxBytes, DateTime now)
    {
        var deleted = new List<string>();
        if (keepDays <= 0 && maxBytes <= 0) return deleted;
        var groups = Load(folder);
        if (keepDays > 0)
        {
            DateTime cutoff = now.AddDays(-keepDays);
            foreach (Group group in groups.Where(g => g.Started < cutoff).OrderBy(g => g.Started).ToList())
                Remove(group, groups, deleted, isProtected);
        }
        if (maxBytes > 0)
        {
            long used = groups.Sum(g => g.Size);
            foreach (Group group in groups.OrderBy(g => g.Started).ToList())
            {
                if (used <= maxBytes) break;
                if (Remove(group, groups, deleted, isProtected)) used -= group.Size;
            }
        }
        return deleted;
    }

    private static bool Remove(Group group, List<Group> groups, List<string> deleted, Func<string, bool> isProtected)
    {
        if (group.Paths.Any(isProtected)) return false;
        foreach (string path in group.Paths)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
        groups.Remove(group);
        deleted.Add(group.Stem);
        return true;
    }

    private sealed class Group
    {
        public required string Stem { get; init; }
        public DateTime Started { get; init; }
        public long Size { get; set; }
        public List<string> Paths { get; } = [];
    }

    private static List<Group> Load(string folder)
    {
        var groups = new List<Group>();
        if (!Directory.Exists(folder)) return groups;
        foreach (FileInfo file in new DirectoryInfo(folder).EnumerateFiles("mediant-*.*"))
        {
            if (!file.Extension.Equals(".pcap", StringComparison.OrdinalIgnoreCase) && !file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;
            string stem = Path.GetFileNameWithoutExtension(file.Name);
            if (!TryStarted(stem, out DateTime started)) continue;
            var group = groups.FirstOrDefault(g => g.Stem.Equals(stem, StringComparison.OrdinalIgnoreCase));
            if (group is null)
            {
                group = new Group { Stem = stem, Started = started };
                groups.Add(group);
            }
            group.Paths.Add(file.FullName);
            group.Size += file.Length;
        }
        return groups;
    }

    private static string InFolder(string folder, string stem, string ext)
    {
        string dir = Path.GetFullPath(folder);
        string path = Path.GetFullPath(Path.Combine(dir, stem + ext));
        string prefix = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("That capture is outside the trace folder.");
        return path;
    }
}
