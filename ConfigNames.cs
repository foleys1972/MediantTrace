using System.Text.Json;

namespace MediantTrace;

/// <summary>A name the SBC itself uses for an IP address: a proxy set, a network interface, or a gateway route.</summary>
public sealed class PeerName
{
    public string Sbc { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";   // proxy, interface, route
    public string Detail { get; set; } = "";
    public DateTime ReadAt { get; set; }
}

/// <summary>
/// Reads names out of an AudioCodes <c>show running-config</c>. Only names and addresses:
/// password lines are never kept.
/// </summary>
public static class ConfigNames
{
    public static List<PeerName> Parse(string config)
    {
        var rows = new List<Dictionary<string, string>>();
        Dictionary<string, string>? row = null;
        foreach (string raw in config.Replace("\r", "").Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line is "activate" or "exit") continue;
            if (line.StartsWith("configure ", StringComparison.Ordinal)) { row = null; continue; }
            if (IsRow(line))
            {
                row = new Dictionary<string, string> { ["_h"] = line };
                rows.Add(row);
                continue;
            }
            if (row is null) continue;
            int sp = line.IndexOf(' ');
            if (sp <= 0) continue;
            row[line[..sp]] = Unquote(line[(sp + 1)..].Trim());
        }

        var names = new List<PeerName>();
        void Add(string ip, string name, string kind, string detail)
        {
            if (ip.Length == 0 || name.Length == 0) return;
            if (names.Any(n => n.Ip == ip)) return; // a proxy name is the peer; an interface or route must not overwrite it
            names.Add(new PeerName { Ip = ip, Name = name, Kind = kind, Detail = detail });
        }

        foreach (var r in rows.Where(r => r["_h"].StartsWith("proxy-ip ", StringComparison.Ordinal)))
        {
            string index = r["_h"]["proxy-ip ".Length..].Split('/')[0];
            var set = rows.FirstOrDefault(x => x["_h"] == "proxy-set " + index);
            string name = set?.GetValueOrDefault("proxy-name") ?? "";
            string via = set?.GetValueOrDefault("sbcipv4-sip-int-name") ?? "";
            Add(HostOf(r.GetValueOrDefault("proxy-address") ?? ""), name, "proxy", via.Length > 0 ? "via " + via : "");
        }
        foreach (var r in rows.Where(r => r["_h"].StartsWith("interface network-if ", StringComparison.Ordinal)))
            Add(r.GetValueOrDefault("ip-address") ?? "", r.GetValueOrDefault("name") ?? "", "interface", "");
        foreach (var r in rows.Where(r => r["_h"].StartsWith("gateway routing tel2ip-routing ", StringComparison.Ordinal)))
        {
            string port = r.GetValueOrDefault("dst-port") ?? "";
            string detail = port.Length > 0 ? "port " + port : "";
            Add(HostOf(r.GetValueOrDefault("dst-ip-address") ?? ""), r.GetValueOrDefault("route-name") ?? "", "route", detail);
        }
        return names;
    }

    private static bool IsRow(string line) =>
        line.StartsWith("proxy-ip ", StringComparison.Ordinal)
        || line.StartsWith("proxy-set ", StringComparison.Ordinal)
        || line.StartsWith("interface network-if ", StringComparison.Ordinal)
        || line.StartsWith("gateway routing tel2ip-routing ", StringComparison.Ordinal);

    private static string Unquote(string value)
    {
        value = value.Trim().TrimEnd(';');
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
    }

    private static string HostOf(string address)
    {
        address = Unquote(address);
        int c = address.LastIndexOf(':');
        return c > 0 ? address[..c] : address;
    }
}

/// <summary>Names for each SBC, kept next to the program. A name typed in the address book is kept when names are read again from the SBC.</summary>
public sealed class LabelBook
{
    private readonly object _gate = new();
    private readonly string _file;
    private List<PeerName> _names = [];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public LabelBook(string? file = null)
    {
        _file = file ?? Path.Combine(AppContext.BaseDirectory, "labels.json");
        try
        {
            if (File.Exists(_file))
                _names = JsonSerializer.Deserialize<List<PeerName>>(File.ReadAllText(_file)) ?? [];
        }
        catch (Exception e) { Log.Write($"labels.json could not be read ({e.Message})"); }
    }

    public List<PeerName> All()
    {
        lock (_gate) return _names.ToList();
    }

    /// <summary>Replaces names read from the SBC. Names typed in the address book for the same address stay, and win.</summary>
    public int Replace(string sbc, IEnumerable<PeerName> names, DateTime readAt)
    {
        var next = names.Where(n => n.Ip.Length > 0 && n.Name.Length > 0)
            .Select(n => new PeerName { Sbc = sbc, Ip = n.Ip, Name = n.Name, Kind = n.Kind, Detail = n.Detail, ReadAt = readAt }).ToList();
        lock (_gate)
        {
            var manual = _names.Where(n => n.Sbc == sbc && n.Kind == "manual").Select(n => n.Ip).ToHashSet();
            var imported = next.Where(n => !manual.Contains(n.Ip)).ToList();
            _names = _names.Where(n => n.Sbc != sbc || n.Kind == "manual").Concat(imported).ToList();
            Save();
            return imported.Count;
        }
    }

    public void SaveManual(string sbc, string ip, string name, string detail)
    {
        var row = Checked(ip, name, detail);
        lock (_gate)
        {
            _names = _names.Where(n => !(n.Sbc == sbc && n.Ip == row.Ip)).Append(WithSbc(sbc, row)).ToList();
            Save();
        }
    }

    /// <summary>
    /// Adds or updates addresses from a CSV. Columns are Name, Address and an optional Note.
    /// A matching address is replaced. Other names for this SBC stay. Imported names are kept when the SBC is read again.
    /// </summary>
    public (int Saved, IReadOnlyList<string> Skipped) ImportCsv(string sbc, string text)
    {
        if (text.Length > 512 * 1024) throw new InvalidOperationException("The file is too large.");
        var table = ReadCsv(text);
        if (table.Count == 0) throw new InvalidOperationException("The file has no addresses.");
        var head = table[0].Fields.Select(ColumnKey).ToList();
        int iName = Column(head, "name", "label");
        int iIp = Column(head, "address", "ip", "ipaddress", "host");
        int iNote = Column(head, "note", "notes", "detail", "comment");
        int start = 1;
        if (iName < 0 || iIp < 0) { iName = 0; iIp = 1; iNote = 2; start = 0; }

        var incoming = new List<PeerName>();
        var skipped = new List<string>();
        for (int i = start; i < table.Count; i++)
        {
            var fields = table[i].Fields;
            string Cell(int index) => index >= 0 && index < fields.Count ? fields[index] : "";
            string name = Cell(iName), ip = Cell(iIp), note = Cell(iNote);
            if (name.Length == 0 && ip.Length == 0 && note.Length == 0) continue;
            try
            {
                var row = Checked(ip, name, note);
                incoming.RemoveAll(n => n.Ip == row.Ip);
                incoming.Add(row);
            }
            catch (InvalidOperationException e) { skipped.Add($"Row {table[i].Line}: {e.Message}"); }
        }
        if (incoming.Count == 0)
            throw new InvalidOperationException(skipped.Count == 0 ? "The file has no addresses." : string.Join(" ", skipped.Take(3)));

        lock (_gate)
        {
            foreach (var row in incoming)
                _names = _names.Where(n => !(n.Sbc == sbc && n.Ip == row.Ip)).Append(WithSbc(sbc, row)).ToList();
            Save();
        }
        return (incoming.Count, skipped);
    }

    private static PeerName WithSbc(string sbc, PeerName row) => new()
    {
        Sbc = sbc, Ip = row.Ip, Name = row.Name, Kind = "manual", Detail = row.Detail, ReadAt = DateTime.UtcNow,
    };

    private static PeerName Checked(string ip, string name, string detail)
    {
        ip = AddressOf(ip);
        if (!System.Net.IPAddress.TryParse(ip, out System.Net.IPAddress? addr) || addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new InvalidOperationException($"'{ip}' is not an IPv4 address.");
        ip = addr.ToString();
        name = name.Trim();
        if (name.Length == 0 || name.Length > 80) throw new InvalidOperationException("Give the address a name, up to 80 characters.");
        detail = detail.Trim();
        if (detail.Length > 80) detail = detail[..80];
        return new PeerName { Ip = ip, Name = name, Detail = detail };
    }

    /// <summary>An address may be written with a port, as 192.0.2.10:5060. Only the host is kept.</summary>
    private static string AddressOf(string raw)
    {
        raw = raw.Trim();
        int colon = raw.LastIndexOf(':');
        if (colon > 0 && raw.IndexOf(':') == colon && int.TryParse(raw[(colon + 1)..], out _))
            raw = raw[..colon];
        return raw;
    }

    private static int Column(List<string> head, params string[] names)
    {
        for (int i = 0; i < head.Count; i++)
            if (names.Contains(head[i])) return i;
        return -1;
    }

    private static string ColumnKey(string cell)
    {
        var key = new System.Text.StringBuilder();
        foreach (char ch in cell)
            if (char.IsLetter(ch)) key.Append(char.ToLowerInvariant(ch));
        return key.ToString();
    }

    private readonly record struct CsvRow(int Line, List<string> Fields);

    private static List<CsvRow> ReadCsv(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];
        string first = "";
        foreach (string line in text.Split('\n'))
            if (line.Trim().Length > 0) { first = line; break; }
        char sep = first.Contains('\t') ? '\t'
            : first.Count(c => c == ';') > first.Count(c => c == ',') ? ';' : ',';

        var rows = new List<CsvRow>();
        var row = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool quoted = false;
        int lineNo = 1, rowLine = 1;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cur.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else
                {
                    if (ch == '\n') lineNo++;
                    cur.Append(ch);
                }
                continue;
            }
            if (ch == '"') quoted = true;
            else if (ch == sep) { row.Add(cur.ToString().Trim()); cur.Clear(); }
            else if (ch == '\n')
            {
                row.Add(cur.ToString().Trim());
                cur.Clear();
                if (row.Exists(c => c.Length > 0)) rows.Add(new CsvRow(rowLine, row));
                row = [];
                lineNo++;
                rowLine = lineNo;
            }
            else cur.Append(ch);
        }
        if (cur.Length > 0 || row.Count > 0)
        {
            row.Add(cur.ToString().Trim());
            if (row.Exists(c => c.Length > 0)) rows.Add(new CsvRow(rowLine, row));
        }
        return rows;
    }

    public bool Remove(string sbc, string ip)
    {
        lock (_gate)
        {
            int before = _names.Count;
            _names = _names.Where(n => !(n.Sbc == sbc && n.Ip == ip)).ToList();
            if (_names.Count == before) return false;
            Save();
            return true;
        }
    }

    private void Save()
    {
        try { File.WriteAllText(_file, JsonSerializer.Serialize(_names, Json)); }
        catch (Exception e) { Log.Write($"labels.json could not be saved ({e.Message})"); }
    }
}
