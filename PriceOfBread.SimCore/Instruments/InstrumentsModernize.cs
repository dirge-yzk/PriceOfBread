namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsModernize.cs#1〕
public static partial class Instruments
{
    internal const string ModSigHeaderText = "quarter	year	nation	meanIndustrialization	industrialFrontier	returnShare	eligibleCount	investWeightSum	grossMean	hurdleMean	stance	gapUsed	modernizationFracSet";

    /// <summary>UtilityAgent.Apply が近代化投資の gap を決めた直後に呼ぶ（Instruments.On のときだけ）。行は Quarter の ModFold が書く。</summary>
    internal static void ModernizeSignal(World w, UtilityAgent.Stance s, double gapUsed)
    {
        var st = S(w);
        st.ModStance = (int)s; st.ModGapUsed = gapUsed;
    }

    static void ModFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        int n = 0; double wsum = 0, gross = 0, hurdle = 0;
        foreach (var r in w.Regions)
        {
            if (!w.StateInvestEligible(r)) continue;
            var (_, _, g, h) = EconomyStep.InvestmentReturn(r, w.Coef, w.TechProductivity(r));
            n++; gross += g; hurdle += h; wsum += w.StateInvestWeight(r);
        }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.ModSig ??= Open("modsig", seed);
        if (!sk.ModSigHeader) { sk.ModSig.WriteLine(ModSigHeaderText); sk.ModSigHeader = true; }
        sk.ModSig.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(w.MeanIndustrialization), F(w.IndustrialFrontier), F(w.ModernizeReturnShare()), I(n), F(wsum),
            F(n > 0 ? gross / n : 0), F(n > 0 ? hurdle / n : 0), I(st.ModStance), F(st.ModGapUsed), F(w.ModernizationFrac)));
        st.ModStance = -1; st.ModGapUsed = double.NaN;
    }
}
