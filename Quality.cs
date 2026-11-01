namespace MediantTrace;

/// <summary>
/// Estimated MOS from packet loss, jitter and delay: the ITU-T G.107 E-model,
/// simplified for G.711 with packet-loss concealment. It is an estimate for
/// comparing calls and directions, not a listening test.
/// </summary>
public static class Quality
{
    private const double R0 = 93.2;      // default basic signal-to-noise rating
    private const double Bpl = 25.1;     // G.711 with packet-loss concealment (G.113 Appendix I)

    /// <param name="lossPercent">packets lost, 0-100</param>
    /// <param name="jitterMs">interarrival jitter (RFC 3550)</param>
    /// <param name="networkDelayMs">one-way network delay; 20 ms assumed when unknown</param>
    public static double Mos(double lossPercent, double jitterMs, double networkDelayMs = 20)
    {
        double ppl = Math.Clamp(lossPercent, 0, 100);
        // Mouth-to-ear delay: network + 20 ms packetisation + a jitter buffer of about twice the jitter.
        double d = Math.Max(0, networkDelayMs) + 20 + Math.Max(10, 2 * Math.Max(0, jitterMs));
        double id = 0.024 * d + (d > 177.3 ? 0.11 * (d - 177.3) : 0);
        double ieEff = 95 * ppl / (ppl + Bpl);
        double r = R0 - id - ieEff;
        if (r <= 0) return 1.0;
        if (r >= 100) return 4.5;
        double mos = 1 + 0.035 * r + 7e-6 * r * (r - 60) * (100 - r);
        return Math.Round(Math.Clamp(mos, 1, 4.5), 2);
    }

    /// <summary>good (≥ 4.0), fair (≥ 3.6), poor (≥ 3.1), bad.</summary>
    public static string Grade(double mos) => mos >= 4.0 ? "good" : mos >= 3.6 ? "fair" : mos >= 3.1 ? "poor" : "bad";
}
