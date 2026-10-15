namespace MediantTrace;

/// <summary>
/// The Mediant's own call records, sent in its syslog: pipe-separated
/// CALL_START / CALL_CONNECT / CALL_END / MEDIA_END lines, announced by a
/// title line ("|GWReportType |Cid |SessionId ..." for the gateway,
/// "|SBCReportType |EPTyp |SIPCallId ..." for the SBC). Columns are matched by
/// that title, so the layout can differ between firmware and configuration.
/// </summary>
public sealed class CdrDecoder
{
    // The 7.20A gateway layout seen on a Mediant 800B, used until the device sends its own title line.
    private static readonly string[] GatewayDefault =
        "GWReportType,Cid,SessionId,LegId,Trunk,BChan,ConId,TG,EPTyp,Orig,SourceIp,DestIp,TON,NPI,SrcPhoneNum,SrcNumBeforeMap,TON,NPI,DstPhoneNum,DstNumBeforeMap,Durat,Coder,Intrv,RtpIp,Port,TrmSd,TrmReason,Fax,InPackets,OutPackets,PackLoss,RemotePackLoss,SIPCallId,SetupTime,ConnectTime,ReleaseTime,RTPdelay,RTPjitter,RTPssrc,RemoteRTPssrc,RedirectReason,TON,NPI,RedirectPhonNum,MeteringPulses,SrcHost,SrcHostBeforeMap,DstHost,DstHostBeforeMap,IPG (name),LocalRtpIp,LocalRtpPort,Amount,Mult,TrmReasonCategory,RedirectNumBeforeMap,SrdId (name),SIPInterfaceId (name),ProxySetId (name),IpProfileId (name),MediaRealmId (name),SigTransportType,TxRTPIPDiffServ,TxSigIPDiffServ".Split(',');

    private readonly Dictionary<string, string[]> _titles = new(); // "sbc|kind" -> column names

    public sealed class Record
    {
        public string Kind = "";            // CALL_END, MEDIA_END, ...
        public string Family = "";          // GW or SBC
        public DateTime Received;
        public List<KeyValuePair<string, string>> Fields = [];

        public string Get(params string[] names)
        {
            foreach (string n in names)
            {
                var hit = Fields.FirstOrDefault(f => f.Key.Equals(n, StringComparison.OrdinalIgnoreCase));
                if (hit.Value is { Length: > 0 }) return hit.Value;
            }
            return "";
        }

        public long Num(params string[] names) => long.TryParse(Get(names), out long v) ? v : -1;
    }

    /// <summary>Feeds one syslog line; returns a record when the line is a CDR.</summary>
    public Record? Feed(string sbc, string line, DateTime now)
    {
        int bar = line.IndexOf('|');
        if (bar < 0) return null;
        string body = line[bar..];
        int time = body.IndexOf(" [Time:", StringComparison.Ordinal);
        if (time > 0) body = body[..time];
        string[] cells = body.Split('|').Skip(1).Select(c => c.Trim()).ToArray();
        if (cells.Length < 5) return null;
        string first = cells[0];

        if (first is "GWReportType" or "SBCReportType" or "MediaReportType" or "ReportType")
        {
            // A title line. Remember it for this SBC and family; a long one may be cut short (fine:
            // the columns that matter come first).
            string family = first.StartsWith("SBC", StringComparison.Ordinal) ? "SBC" : first.StartsWith("GW", StringComparison.Ordinal) ? "GW" : "MEDIA";
            if (cells.Contains("Coder") && family == "SBC" && cells.Contains("MediaType")) family = "SBCMEDIA";
            lock (_titles) _titles[$"{sbc}|{family}"] = cells;
            return null;
        }
        if (!first.StartsWith("CALL_", StringComparison.Ordinal) && !first.StartsWith("MEDIA_", StringComparison.Ordinal)) return null;

        // SBC records have "SBC" in the second column; gateway records a number (the channel).
        string fam = cells.Length > 1 && cells[1].Equals("SBC", StringComparison.OrdinalIgnoreCase) ? "SBC" : "GW";
        if (first.StartsWith("MEDIA_", StringComparison.Ordinal) && fam == "SBC") fam = "SBCMEDIA";
        string[]? names;
        lock (_titles) _titles.TryGetValue($"{sbc}|{fam}", out names);
        names ??= fam == "GW" ? GatewayDefault : null;

        var rec = new Record { Kind = first, Family = fam.StartsWith("SBC", StringComparison.Ordinal) ? "SBC" : "GW", Received = now };
        var seen = new Dictionary<string, int>();
        for (int i = 0; i < cells.Length; i++)
        {
            string name = names is not null && i < names.Length ? names[i] : $"Field {i + 1}";
            name = name.Replace(" (name)", "");
            if (seen.TryGetValue(name, out int n)) { seen[name] = n + 1; name = $"{name} {n + 1}"; } else seen[name] = 1;
            if (i == 0) name = "ReportType";
            rec.Fields.Add(new(name, cells[i]));
        }
        return rec;
    }
}
