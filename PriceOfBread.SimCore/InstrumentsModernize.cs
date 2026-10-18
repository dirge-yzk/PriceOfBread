namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (416) AI の近代化投資の信号の表 modsig_s（読むだけ・事前登録 measurements/sc_20261019_prereg.txt [2]） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。旗 AiModernizeFromReturn＝0 でも出す（A の表が〈旧の世界の信号〉の表になる）。
// 列（単位は接尾辞：Share＝0〜1・Count＝件数）：
//   meanIndustrialization（旧の信号の入力・人口加重）／indCeilingOld（EffectiveIndCeiling＝max(2.5, α×前線)）／industrialFrontier（全国の MeanIndustrialization の最大・World に注入された値）／
//   indGapOld（旧の gap＝Clamp01((ceil−Mean)÷ceil)）／returnShare（World.ModernizeReturnShare＝新しい信号）／eligibleCount（国庫の近代化投資の適格の州の数）／
//   investWeightSum（適格の州の按分の重みの和）／grossMean・hurdleMean（適格の州の設備の総収益とハードルの単純平均）／
//   stance（Apply が選んだ姿勢の番号・Apply が走らない国は −1）／gapUsed（Apply が実際に使った gap）／flagLevel（AiModernizeFromReturn）／modernizationFracSet（実際に置かれたレバー）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Apply は四半期の頭に走る＝gapUsed は頭の値・他の列は末の値）／単純平均は州の大きさを見ない／Apply が四半期に複数回走れば最後の 1 回だけ。
public static partial class Instruments
{
    internal const string ModSigHeaderText = "quarter	year	nation	meanIndustrialization	indCeilingOld	industrialFrontier	indGapOld	returnShare	eligibleCount	investWeightSum	grossMean	hurdleMean	stance	gapUsed	flagLevel	modernizationFracSet";

    /// <summary>UtilityAgent.Apply が近代化投資の gap を決めた直後に呼ぶ（Instruments.On のときだけ）。行は Quarter の ModFold が書く。</summary>
    internal static void ModernizeSignal(World w, UtilityAgent.Stance s, double gapUsed, bool fromReturn)
    {
        var st = S(w);
        st.ModStance = (int)s; st.ModGapUsed = gapUsed; st.ModFromReturn = fromReturn;
    }

    static void ModFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        double ceil = UtilityAgent.EffectiveIndCeiling(w);
        double gapOld = PoliticalQuantities.Clamp01((ceil - w.MeanIndustrialization) / ceil);
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
        sk.ModSig.WriteLine(string.Join('	', I(q), I(w.Year), NameOf(w), F(w.MeanIndustrialization), F(ceil), F(w.IndustrialFrontier), F(gapOld), F(w.ModernizeReturnShare()), I(n), F(wsum),
            F(n > 0 ? gross / n : 0), F(n > 0 ? hurdle / n : 0), I(st.ModStance), F(st.ModGapUsed), F(w.Coef.AiModernizeFromReturn), F(w.ModernizationFrac)));
        st.ModStance = -1; st.ModGapUsed = double.NaN; st.ModFromReturn = false;
    }
}
