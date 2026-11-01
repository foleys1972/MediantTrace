namespace MediantTrace;

/// <summary>How many settings and table rows differ between a saved standard and a live AudioCodes .ini.</summary>
public static class IniDiff
{
    public static int Count(string standard, string current)
    {
        Parsed a = Parse(standard), b = Parse(current);
        int n = 0;
        foreach (string key in a.Params.Keys.Concat(b.Params.Keys).Distinct(StringComparer.Ordinal))
        {
            a.Params.TryGetValue(key, out string? left);
            b.Params.TryGetValue(key, out string? right);
            if (!string.Equals(left, right, StringComparison.Ordinal)) n++;
        }
        foreach (string name in a.Tables.Keys.Concat(b.Tables.Keys).Distinct(StringComparer.Ordinal))
        {
            a.Tables.TryGetValue(name, out Table? left);
            b.Tables.TryGetValue(name, out Table? right);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (left is not null) foreach (string k in left.Rows.Keys) keys.Add(k);
            if (right is not null) foreach (string k in right.Rows.Keys) keys.Add(k);
            foreach (string key in keys)
            {
                string[]? x = left is not null && left.Rows.TryGetValue(key, out string[]? xv) ? xv : null;
                string[]? y = right is not null && right.Rows.TryGetValue(key, out string[]? yv) ? yv : null;
                if (x is null || y is null || !SameRow(x, y)) n++;
            }
        }
        return n;
    }

    private static bool SameRow(string[] a, string[] b)
    {
        int n = Math.Max(a.Length, b.Length);
        for (int i = 0; i < n; i++)
            if (!string.Equals(i < a.Length ? a[i] : "", i < b.Length ? b[i] : "", StringComparison.Ordinal)) return false;
        return true;
    }

    private sealed class Parsed
    {
        public Dictionary<string, string> Params { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Table> Tables { get; } = new(StringComparer.Ordinal);
    }

    private sealed class Table
    {
        public Dictionary<string, string[]> Rows { get; } = new(StringComparer.Ordinal);
    }

    private static Parsed Parse(string text)
    {
        var parsed = new Parsed();
        Table? table = null;
        string? tableName = null;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim('\r', ' ', '\t').TrimStart('\uFEFF');
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.Length > 2 && line[0] == '[' && line[^1] == ']')
            {
                string inside = line[1..^1].Trim();
                if (inside.StartsWith('\\'))
                {
                    string name = inside[1..].Trim();
                    if (tableName == name) { table = null; tableName = null; continue; }
                    if (!parsed.Tables.TryGetValue(name, out table))
                    {
                        table = new Table();
                        parsed.Tables[name] = table;
                    }
                    tableName = name;
                    continue;
                }
                table = null;
                tableName = null;
                continue;
            }
            if (table is not null)
            {
                if (line.StartsWith("FORMAT", StringComparison.OrdinalIgnoreCase)) continue;
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string left = line[..eq].Trim();
                int space = left.LastIndexOf(' ');
                if (space > 0 && int.TryParse(left[(space + 1)..], out _))
                    table.Rows[left[(space + 1)..]] = SplitRow(line[(eq + 1)..].Trim().TrimEnd(';'));
                continue;
            }
            int cut = line.IndexOf('=');
            if (cut < 0) continue;
            string key = line[..cut].Trim();
            string value = Unquote(line[(cut + 1)..].Trim());
            if (key.Length > 0) parsed.Params[key] = value;
        }
        return parsed;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '\'' && value[^1] == '\'') || (value[0] == '"' && value[^1] == '"')))
            return value[1..^1];
        return value;
    }

    private static string[] SplitRow(string s)
    {
        var outp = new List<string>();
        var cur = new System.Text.StringBuilder();
        char? quote = null;
        foreach (char ch in s)
        {
            if ((ch == '\'' || ch == '"') && (quote is null || quote == ch)) { quote = quote is null ? ch : null; continue; }
            if (ch == ',' && quote is null) { outp.Add(cur.ToString().Trim()); cur.Clear(); continue; }
            cur.Append(ch);
        }
        outp.Add(cur.ToString().Trim());
        return outp.ToArray();
    }
}
