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

/// <summary>Names last read from each SBC, kept next to the program.</summary>
public sealed class LabelBook
{
    private readonly object _gate = new();
    private List<PeerName> _names = [];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "labels.json");

    public LabelBook()
    {
        try
        {
            if (File.Exists(FilePath))
                _names = JsonSerializer.Deserialize<List<PeerName>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch (Exception e) { Log.Write($"labels.json could not be read ({e.Message})"); }
    }

    public List<PeerName> All()
    {
        lock (_gate) return _names.ToList();
    }

    public void Replace(string sbc, IEnumerable<PeerName> names, DateTime readAt)
    {
        var next = names.Select(n => new PeerName { Sbc = sbc, Ip = n.Ip, Name = n.Name, Kind = n.Kind, Detail = n.Detail, ReadAt = readAt }).ToList();
        lock (_gate)
        {
            _names = _names.Where(n => n.Sbc != sbc).Concat(next).ToList();
            try { File.WriteAllText(FilePath, JsonSerializer.Serialize(_names, Json)); }
            catch (Exception e) { Log.Write($"labels.json could not be saved ({e.Message})"); }
        }
    }
}
