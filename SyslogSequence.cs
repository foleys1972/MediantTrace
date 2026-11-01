using System.Text.RegularExpressions;

namespace MediantTrace;

/// <summary>Syslog numbering seen from one SBC.</summary>
public sealed class SequenceStats
{
    public long Received, Missing, OutOfOrder, Restarts;
    public DateTime? LastGap;
}

/// <summary>
/// The Mediant numbers its syslog messages ("[S=12345]"). Over UDP a message can be lost or overtaken
/// by the next one. Per SBC this:
///   - puts overtaken messages back in order, holding a message that arrives early for a short time
///     (a small jitter buffer) while the one before it may still come;
///   - counts messages that never came, and reports each gap (Syslog Viewer's "syslog.warning
///     N messages are missing");
///   - counts messages that came late, and starts again when the numbers jump back (the SBC restarted).
/// With reordering off, each message is passed on at once and only gaps are counted.
/// </summary>
public sealed partial class SyslogSequence<T>
{
    [GeneratedRegex(@"^\s*(?:<\d{1,3}>)?\s*(?:\S+\s+)?\[S=(\d+)\]")]
    private static partial Regex SeqTag();

    private sealed class Track
    {
        public long Expected = -1;
        public readonly SortedDictionary<long, (DateTime arrived, T item)> Held = new();
        public readonly SequenceStats Stats = new();
    }

    private readonly Dictionary<string, Track> _tracks = new();
    private readonly Action<T> _release;
    private readonly Action<string, long, long, DateTime> _gap;

    public bool Reorder { get; set; } = true;
    public TimeSpan Hold { get; set; } = TimeSpan.FromMilliseconds(400);
    private const int MaxHeld = 400;
    // A number this far behind means the SBC started counting again, not a late message.
    private const long RestartDistance = 5000;

    /// <param name="release">Gets each message, in order.</param>
    /// <param name="gap">Gets (sbc, first missing number, how many, when) for each gap.</param>
    public SyslogSequence(Action<T> release, Action<string, long, long, DateTime> gap)
    {
        _release = release;
        _gap = gap;
    }

    /// <summary>The [S=n] number of a syslog line, or -1.</summary>
    public static long NumberOf(string line) =>
        SeqTag().Match(line) is { Success: true } m && long.TryParse(m.Groups[1].Value, out long n) ? n : -1;

    public void Offer(string sbc, long seq, DateTime now, T item)
    {
        if (seq < 0) { _release(item); return; }
        if (!_tracks.TryGetValue(sbc, out Track? t)) _tracks[sbc] = t = new Track();
        t.Stats.Received++;
        if (t.Expected < 0) { t.Expected = seq + 1; _release(item); return; }
        if (seq == t.Expected)
        {
            t.Expected++;
            _release(item);
            Drain(t);
            return;
        }
        if (seq > t.Expected)
        {
            if (!Reorder || seq - t.Expected > RestartDistance)
            {
                if (seq - t.Expected > RestartDistance) { Release(t); t.Stats.Restarts++; }
                else Missing(sbc, t, t.Expected, seq - t.Expected, now);
                t.Expected = seq + 1;
                _release(item);
                return;
            }
            t.Held[seq] = (now, item);
            if (t.Held.Count > MaxHeld) Skip(sbc, t, now);
            return;
        }
        // Behind what is expected: late (already counted missing), or the SBC restarted.
        if (t.Expected - seq > RestartDistance)
        {
            Release(t);
            t.Stats.Restarts++;
            t.Expected = seq + 1;
            _release(item);
            return;
        }
        t.Stats.OutOfOrder++;
        if (t.Stats.Missing > 0) t.Stats.Missing--;
        _release(item);
    }

    /// <summary>Releases messages held longer than <see cref="Hold"/>, counting the ones still missing before them.</summary>
    public void Tick(DateTime now)
    {
        foreach (var (sbc, t) in _tracks)
            while (t.Held.Count > 0 && now - t.Held.First().Value.arrived >= Hold)
                Skip(sbc, t, now);
    }

    /// <summary>Releases everything held (end of a file, or shutting down).</summary>
    public void Flush(DateTime now)
    {
        foreach (var (sbc, t) in _tracks)
            while (t.Held.Count > 0) Skip(sbc, t, now);
    }

    public Dictionary<string, SequenceStats> Snapshot() =>
        _tracks.ToDictionary(p => p.Key, p => new SequenceStats { Received = p.Value.Stats.Received, Missing = p.Value.Stats.Missing, OutOfOrder = p.Value.Stats.OutOfOrder, Restarts = p.Value.Stats.Restarts, LastGap = p.Value.Stats.LastGap });

    public void Clear() => _tracks.Clear();

    // Give up waiting for the next number: count the gap, carry on from the first one held.
    private void Skip(string sbc, Track t, DateTime now)
    {
        long first = t.Held.First().Key;
        if (first > t.Expected) Missing(sbc, t, t.Expected, first - t.Expected, now);
        t.Expected = first;
        Drain(t);
    }

    private void Drain(Track t)
    {
        while (t.Held.Remove(t.Expected, out var next))
        {
            t.Expected++;
            _release(next.item);
        }
    }

    // Everything held goes out in order, without counting gaps (the numbering has restarted).
    private void Release(Track t)
    {
        foreach (var held in t.Held.Values) _release(held.item);
        t.Held.Clear();
    }

    private void Missing(string sbc, Track t, long from, long count, DateTime now)
    {
        t.Stats.Missing += count;
        t.Stats.LastGap = now;
        _gap(sbc, from, count, now);
    }
}
