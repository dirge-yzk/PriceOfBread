namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・世界を書かない。〔履歴 InstrumentsSilverWeight.cs#1〕
public static partial class Instruments
{
    internal const string SilverWeightHeaderText = "seq\tfrozen\tyear\tratioOkFlag\tstateCount\trelEqualIndex\trelW1Index\trelSqIndex\trelProdIndex\tusedRelIndex\tusedParityIndex\tusedRelFromWeightsIndex\tusedWeightSum\tusedMaxShare\tusedOneSidedShare\tequalOneSidedShare\tw1OneSidedShare\tw1MaxShare\tsqMaxShare\tprodMaxShare\tw1ZeroWeightCount\tsilverNationCount\tclampHitCount" + SilverstWeightTailHeader + SilverWeightTail827Header;   // SilverWeightHeaderText〔履歴 InstrumentsSilverWeight.cs#2〕
    /// <summary>
    /// 〔履歴 InstrumentsSilverWeight.cs#3〕
    /// </summary>
    internal const string SilverWeightTail827Header = "\tusedAgOneSidedShare\tusedEnterShare\tequalAgOneSidedShare\tw1AgOneSidedShare\tminRefAgOneSidedShare\tmaxRefAgOneSidedShare";

    sealed class SilverWeightSink
    {
        public int Seq; public readonly List<string> Rows = [];
        // 1 注入ぶんの累積（End が行を作ったら 0 へ戻す）
        public int N, OneSided, ZeroW;
        public double SumLn, UsedW, UsedWLn, UsedMax, UsedOne;
        public double W1, W1Ln, W1Max, W1One;
        public double Sq, SqLn, SqMax;
        public double Pr, PrLn, PrMax;
        public double UsedAgOne, UsedEnter; public bool EnterKnown;   // UsedEnter〔履歴 InstrumentsSilverWeight.cs#4〕
        public void Reset() { N = 0; OneSided = 0; ZeroW = 0; SumLn = UsedW = UsedWLn = UsedMax = UsedOne = W1 = W1Ln = W1Max = W1One = Sq = SqLn = SqMax = Pr = PrLn = PrMax = UsedAgOne = UsedEnter = 0; EnterKnown = false; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SilverWeightSink> SilverWeightSinks = new();

    static string SilverWeightR(double v) => double.IsNaN(v) ? "nan" : double.IsPositiveInfinity(v) ? "inf" : double.IsNegativeInfinity(v) ? "-inf" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// 注入の走査が 1 州を数えるたび（MultiNationSim）：エンジンの票 wUsed・比の対数・金鉱の需要と産出を溜める。〔履歴 InstrumentsSilverWeight.cs#5〕
    /// </summary>
    internal static void SilverWeightState(MultiNationSim sim, Region r, double wUsed, double lnRatio, double demandAu, double producedAu, double demandAg, double producedAg)
    {
        var sk = SilverWeightSinks.GetValue(sim, _ => new SilverWeightSink());
        bool oneSided = demandAu < 0.01 * producedAu;
        sk.N++; sk.SumLn += lnRatio; if (oneSided) sk.OneSided++;
        // 票の候補（計器の定義）。非有限・非正は票なし。
        double w1 = demandAu > 0 && demandAu < double.PositiveInfinity ? demandAu : 0.0;
        double sq = w1 * w1;
        double pr = producedAu > 0 && producedAu < double.PositiveInfinity ? producedAu : 0.0;
        if (!(w1 > 0)) sk.ZeroW++;
        sk.W1 += w1; sk.W1Ln += w1 * lnRatio; sk.W1Max = Math.Max(sk.W1Max, w1); if (oneSided) sk.W1One += w1;
        sk.Sq += sq; sk.SqLn += sq * lnRatio; sk.SqMax = Math.Max(sk.SqMax, sq);
        sk.Pr += pr; sk.PrLn += pr * lnRatio; sk.PrMax = Math.Max(sk.PrMax, pr);
        double u = wUsed > 0 && wUsed < double.PositiveInfinity ? wUsed : 0.0;
        sk.UsedW += u; sk.UsedWLn += u * lnRatio; sk.UsedMax = Math.Max(sk.UsedMax, u); if (oneSided) sk.UsedOne += u;
        if (demandAg < 0.01 * producedAg) sk.UsedAgOne += u;   // producedAg〔履歴 InstrumentsSilverWeight.cs#6〕
        if (SilverstSinks.TryGetValue(sim, out var ss) && ss.HavePrev) { sk.EnterKnown = true; if (!ss.Prev.Contains(r)) sk.UsedEnter += u; }   // SilverstSinks〔履歴 InstrumentsSilverWeight.cs#7〕
    }

    /// <summary>注入の直後（助走の注入も含む）：World に入った rel と WorldSilverPrice を読み、挟みに当たる銀本位国を数えて 1 行を溜める。</summary>
    internal static void SilverWeightEnd(MultiNationSim sim, bool frozen, bool ratioOk)
    {
        var sk = SilverWeightSinks.GetValue(sim, _ => new SilverWeightSink());
        sk.Seq++;
        if (sim.Nations.Count == 0) { sk.Reset(); return; }
        var w0 = sim.Nations[0].World; var c0 = w0.Coef;
        var ag = SilverstEnd(sim, frozen, sk.Seq);   // ag〔履歴 InstrumentsSilverWeight.cs#8〕
        double refAg = Catalog.RefPrices[Catalog.SilverId], refAu = Catalog.RefPrices[Catalog.GoldOreId];
        double refRatio = refAu > 1e-9 ? refAg / refAu : double.NaN;
        double Rel(double lnSum, double wSum) => wSum > 0 && refRatio > 1e-9 ? Math.Exp(lnSum / wSum) / refRatio : double.NaN;
        double usedRel = w0.SilverGoldRatio, usedParity = refAg > 1e-9 ? w0.WorldSilverPrice / refAg : double.NaN;
        int silverNations = 0, clampHits = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.MonetaryLaw != MonetaryStandardLaw.Silver || !(w.WorldSilverPrice > 1e-9)) continue;
            silverNations++;
            double sr = refAg > 1e-9 ? w.WorldSilverPrice / refAg : 1.0;
            if (!(sr >= c0.ExchangeRateFloor && sr <= c0.ExchangeRateCeil)) clampHits++;
        }
        string R(double v) => SilverWeightR(v);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Rows.Add(string.Join('\t', I(sk.Seq), frozen ? "1" : "0", I(w0.Year), ratioOk ? "1" : "0", I(sk.N),
            R(sk.N > 0 && refRatio > 1e-9 ? Math.Exp(sk.SumLn / sk.N) / refRatio : double.NaN), R(Rel(sk.W1Ln, sk.W1)), R(Rel(sk.SqLn, sk.Sq)), R(Rel(sk.PrLn, sk.Pr)),
            R(usedRel), R(usedParity), R(Rel(sk.UsedWLn, sk.UsedW)),
            R(sk.UsedW), R(sk.UsedW > 0 ? sk.UsedMax / sk.UsedW : double.NaN), R(sk.UsedW > 0 ? sk.UsedOne / sk.UsedW : double.NaN),
            R(sk.N > 0 ? (double)sk.OneSided / sk.N : double.NaN), R(sk.W1 > 0 ? sk.W1One / sk.W1 : double.NaN), R(sk.W1 > 0 ? sk.W1Max / sk.W1 : double.NaN),
            R(sk.Sq > 0 ? sk.SqMax / sk.Sq : double.NaN), R(sk.Pr > 0 ? sk.PrMax / sk.Pr : double.NaN), I(sk.ZeroW), I(silverNations), I(clampHits),
            // R〔履歴 InstrumentsSilverWeight.cs#9〕
            R(ag.RelRef), R(ag.RelVal), R(ag.RelMax), R(ag.MaxShareRef), R(ag.MaxShareVal), R(ag.MaxShareMax), R(ag.OneRef), R(ag.OneVal), R(ag.OneMax),
            R(ag.EnterEqual), R(ag.EnterW1), R(ag.EnterRef), R(ag.EnterVal), R(ag.EnterMax), I(ag.EnterCount), I(ag.LeaveCount), I(ag.ZeroRef), I(ag.ZeroVal),
            I(ag.N), R(ag.W1Sum), R(ag.RelW1), I(Math.Abs(ag.N - sk.N)), R(Math.Abs(ag.W1Sum - sk.W1)), R(double.IsNaN(ag.RelW1) && double.IsNaN(Rel(sk.W1Ln, sk.W1)) ? 0.0 : Math.Abs(ag.RelW1 - Rel(sk.W1Ln, sk.W1))),
            // R〔履歴 InstrumentsSilverWeight.cs#10〕
            R(sk.UsedW > 0 ? sk.UsedAgOne / sk.UsedW : double.NaN), R(sk.EnterKnown && sk.UsedW > 0 ? sk.UsedEnter / sk.UsedW : double.NaN),
            R(ag.OneAgEqual), R(ag.OneAgW1), R(ag.OneAgRef), R(ag.OneAgMax)));
        sk.Reset();
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：溜めた行を書く。</summary>
    static void SilverWeightClose(MultiNationSim sim, ulong seed)
    {
        SilverstClose(sim, seed);   // SilverstClose〔履歴 InstrumentsSilverWeight.cs#11〕
        if (!SilverWeightSinks.TryGetValue(sim, out var sk) || sk.Rows.Count == 0) return;
        using var wr = Open("silverwt", seed);
        wr.WriteLine(SilverWeightHeaderText);
        foreach (var row in sk.Rows) wr.WriteLine(row);
        sk.Rows.Clear();
    }
}
