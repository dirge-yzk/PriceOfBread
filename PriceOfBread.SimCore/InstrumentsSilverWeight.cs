namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (807) 銀の平価の注入の票の表 silverwt_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSilverParity.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝旗 SilverParityDemandWeighted（注入の対数平均を州の金鉱の需要で重みづける）の旧と新を同じ盤面で読む。silverpar_s は助走の注入を書かない（捕捉が !_calendarFrozen の門の中）ので別の表にした。
// 取り方（注入の式を呼ばない＝事前登録の文から独立）：
//   ①<see cref="SilverWeightState"/>：MultiNationSim の注入の走査が州（銀と金鉱の両方の市場が生きて価格が正）を数えるたびに呼ぶ。引数＝エンジンが**実際に使った票** wUsed・比の対数・金鉱の需要・金鉱の産出。
//      計器は票の定義を持たず、等重み・需要・需要の二乗・産出の 4 通りの対数平均を**ここで自分で**組み、エンジンの票 wUsed の平均と並べる（形の一致は usedRel と relW1 の比べで読む）。
//   ②<see cref="SilverWeightEnd"/>：注入の直後（助走の注入も含む＝1 注入 1 行）。World に注入された rel（SilverGoldRatio）と WorldSilverPrice を読み、銀本位国のうち平価の挟み（ExchangeRateFloor・Ceil）に当たる国を数えて 1 行を溜める。
//   ③<see cref="SilverWeightClose"/>：試合の終わりに溜めた行を書く（種は Close が知っている）。
// 列：seq（注入の通し番号・1 から）・frozen（助走の注入 1）・year・ratioOkFlag・stateCount（票を持ちうる州の数＝走査に入った州）・
//   relEqualIndex（等重みの対数平均÷基準の比＝旧の rel）・relW1Index（金鉱の需要を票にした rel）・relSqIndex（需要の二乗）・relProdIndex（金鉱の産出）・
//   usedRelIndex（実際に注入された rel＝World.SilverGoldRatio）・usedParityIndex（実際に注入された WorldSilverPrice÷RefPrice[銀]）・usedRelFromWeightsIndex（エンジンの票で計器が組み直した rel）・
//   usedWeightSum・usedMaxShare（エンジンの票のうち 1 州の最大の割合）・usedOneSidedShare（エンジンの票のうち片側の州〔金鉱の需要＜産出の 1%〕の割合）・
//   equalOneSidedShare（片側の州の数の割合＝等重みの票の割合）・w1OneSidedShare・w1MaxShare・sqMaxShare・prodMaxShare・w1ZeroWeightCount（需要が 0 か非有限の州の数）・
//   silverNationCount（銀本位で WorldSilverPrice＞1e-9 の国）・clampHitCount（そのうち注入の rel が [ExchangeRateFloor, Ceil] の外の国）。
// ⚠限界（計器が自己申告する）：基準の比（refRatio）は RefPrices から計器が組み直す／片側の州の 1% は計器の定義（エンジンの規則ではない）／挟みに当たる国は注入の時の法で数える（月々の法の替わりは見ない）／
//   行は試合の終わりに書く＝途中で落ちた試合は行が無い。
public static partial class Instruments
{
    internal const string SilverWeightHeaderText = "seq\tfrozen\tyear\tratioOkFlag\tstateCount\trelEqualIndex\trelW1Index\trelSqIndex\trelProdIndex\tusedRelIndex\tusedParityIndex\tusedRelFromWeightsIndex\tusedWeightSum\tusedMaxShare\tusedOneSidedShare\tequalOneSidedShare\tw1OneSidedShare\tw1MaxShare\tsqMaxShare\tprodMaxShare\tw1ZeroWeightCount\tsilverNationCount\tclampHitCount" + SilverstWeightTailHeader;   // (823) 末尾に反実仮想と自走査の列を足した（既存の列は変えない）

    sealed class SilverWeightSink
    {
        public int Seq; public readonly List<string> Rows = [];
        // 1 注入ぶんの累積（End が行を作ったら 0 へ戻す）
        public int N, OneSided, ZeroW;
        public double SumLn, UsedW, UsedWLn, UsedMax, UsedOne;
        public double W1, W1Ln, W1Max, W1One;
        public double Sq, SqLn, SqMax;
        public double Pr, PrLn, PrMax;
        public void Reset() { N = 0; OneSided = 0; ZeroW = 0; SumLn = UsedW = UsedWLn = UsedMax = UsedOne = W1 = W1Ln = W1Max = W1One = Sq = SqLn = SqMax = Pr = PrLn = PrMax = 0; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SilverWeightSink> SilverWeightSinks = new();

    static string SilverWeightR(double v) => double.IsNaN(v) ? "nan" : double.IsPositiveInfinity(v) ? "inf" : double.IsNegativeInfinity(v) ? "-inf" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>注入の走査が 1 州を数えるたび（MultiNationSim）：エンジンの票 <paramref name="wUsed"/>・比の対数・金鉱の需要と産出を溜める。</summary>
    internal static void SilverWeightState(MultiNationSim sim, double wUsed, double lnRatio, double demandAu, double producedAu)
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
    }

    /// <summary>注入の直後（助走の注入も含む）：World に入った rel と WorldSilverPrice を読み、挟みに当たる銀本位国を数えて 1 行を溜める。</summary>
    internal static void SilverWeightEnd(MultiNationSim sim, bool frozen, bool ratioOk)
    {
        var sk = SilverWeightSinks.GetValue(sim, _ => new SilverWeightSink());
        sk.Seq++;
        if (sim.Nations.Count == 0) { sk.Reset(); return; }
        var w0 = sim.Nations[0].World; var c0 = w0.Coef;
        var ag = SilverstEnd(sim, frozen, sk.Seq);   // (823) 計器が自分で全州を走査（silverst_s の行も溜める）
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
            // (823) 反実仮想（A′＝参照価格建ての薄い側・A＝局所の値段建て・max 版）と入った州の票の割合・自走査とエンジンの渡した値の差
            R(ag.RelRef), R(ag.RelVal), R(ag.RelMax), R(ag.MaxShareRef), R(ag.MaxShareVal), R(ag.MaxShareMax), R(ag.OneRef), R(ag.OneVal), R(ag.OneMax),
            R(ag.EnterEqual), R(ag.EnterW1), R(ag.EnterRef), R(ag.EnterVal), R(ag.EnterMax), I(ag.EnterCount), I(ag.LeaveCount), I(ag.ZeroRef), I(ag.ZeroVal),
            I(ag.N), R(ag.W1Sum), R(ag.RelW1), I(Math.Abs(ag.N - sk.N)), R(Math.Abs(ag.W1Sum - sk.W1)), R(double.IsNaN(ag.RelW1) && double.IsNaN(Rel(sk.W1Ln, sk.W1)) ? 0.0 : Math.Abs(ag.RelW1 - Rel(sk.W1Ln, sk.W1)))));
        sk.Reset();
    }

    /// <summary>試合の終わり（<see cref="Close"/>）：溜めた行を書く。</summary>
    static void SilverWeightClose(MultiNationSim sim, ulong seed)
    {
        SilverstClose(sim, seed);   // (823) 州の表 silverst_s も書く
        if (!SilverWeightSinks.TryGetValue(sim, out var sk) || sk.Rows.Count == 0) return;
        using var wr = Open("silverwt", seed);
        wr.WriteLine(SilverWeightHeaderText);
        foreach (var row in sk.Rows) wr.WriteLine(row);
        sk.Rows.Clear();
    }
}
