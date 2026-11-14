namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsRule.cs#1〕
public static partial class Instruments
{
    sealed class RuleLagState { public double Prev = double.NaN; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, RuleLagState> RuleLags = new();

    /// <summary>
    /// 人口が正でなければ nan。〔履歴 InstrumentsRule.cs#2〕
    /// </summary>
    internal static double RuleRawOrder(World w, Coefficients c)
    {
        double pop = w.TotalPopulation;
        if (!(pop > 0)) return double.NaN;
        double effCoerce = w.Coercion * (pop / System.Math.Max(1.0, c.PoliticalReferencePop));
        return effCoerce / System.Math.Max(1e-9, pop * c.CrimeOrderPerPop);
    }

    /// <summary>
    /// 〔履歴 InstrumentsRule.cs#3〕
    /// </summary>
    internal static (double Mil, double Size) RuleCrowdMilitancy(Region r)
    {
        double mil = 0, size = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { mil += p.Militancy * p.Size; size += p.Size; }
        return (mil, size);
    }

    static double RuleClamp01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    internal const string RuleHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\truleStateShare\truleLocalShare\truleForeignShare\truleUngovShare\truleInitLocalShare\tforeignRulerCount\tdominanceShare\tcapitalDistanceKm\treachShare\t"
        + "serfLawCode\tbureaucracyLawCode\tlandReformLawCode\tintermediaryDegreeIndex\tpublicOrderIndex\tstateReachIndex\tadminEfficiencyIndex\ttreatyPortInForceFlag\tportForeignShare\trevenuePerMonth\tgdpPerMonth\truleCutPerMonth\tstateReachHomeIndex"
        + RuleP1HeaderText;

    /// <summary>
    /// 〔履歴 InstrumentsRule.cs#4〕
    /// </summary>
    internal const string RuleP1HeaderText = "\torderRawIndex\tmilitancyRegionIndex\tmilitancyNationIndex\torderRegionIndex\tungovTargetOldShare\tungovTargetNewShare\torderRawLagIndex\torderRegionLagIndex\tungovTargetNewLagShare"
        + RuleOldTargetHeaderText;

    /// <summary>(1147) 段 C′ の計器 (c)：旧の規則の目標（Politics.cs の寄せの目標の式を計器が独立に書いた物）を目標に当てた読み手の値。〔履歴 InstrumentsRule.cs#8〕</summary>
    internal const string RuleOldTargetHeaderText = "\ttgtStateShare\ttgtLocalShare\ttgtForeignShare\ttgtUngovShare\tnowRuleDelivery\ttgtRuleDelivery\tnowRule1GtRule0\ttgtRule1GtRule0\tstepResidual\ttgtStateReachIndex";

    /// <summary>
    /// 本体の enforce は読まない（StateReach は植民地格の州も含む）。〔履歴 InstrumentsRule.cs#5〕
    /// </summary>
    static double HomeStateReach(World w, Coefficients c)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            num += pop * Politics.RuleReach(r); den += pop;
        }
        return den > 0 ? num / den : 1.0;
    }

    static void RuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        var c = w.Coef;
        // 行を出す前に前の値を取り、今の値を次の四半期のために置く。〔履歴 InstrumentsRule.cs#6〕
        var lagSt = RuleLags.GetValue(w, _ => new RuleLagState());
        double rNow = RuleRawOrder(w, c), rLag = lagSt.Prev;
        lagSt.Prev = rNow;
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) { RulePrevStore(w); return; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Rule ??= Open("rule", seed);
        if (!sk.RuleHeader) { sk.Rule.WriteLine(RuleHeaderText); sk.RuleHeader = true; }
        string head = I(q) + "\t" + I(w.Year) + "\t" + NameOf(w) + "\t";
        string nat = string.Join('\t', I((int)w.SerfEmancipation), I((int)w.Bureaucracy), I((int)w.LandReform), F(Privileges.IntermediaryDegree(w)), F(w.PublicOrder),
            F(Politics.StateReach(w, c)), F(w.AdminEfficiency), w.UnequalTreatyInForce ? "1" : "0", F(Politics.ForeignPortShare(w)),
            F(w.LastRevenuePerMonth.Value), F(w.GdpPerMonth.Value), F(w.LastRuleCut), F(HomeStateReach(w, c)));
        // crowdMil〔履歴 InstrumentsRule.cs#7〕
        double crowdMil = 0, crowdSize = 0;
        foreach (var r in w.Regions) { var (cm, cs) = RuleCrowdMilitancy(r); crowdMil += cm; crowdSize += cs; }
        double mBar = crowdSize > 1e-9 ? crowdMil / crowdSize : double.NaN;
        double orderOld = RuleClamp01(w.PublicOrder);
        double mBar0 = double.IsNaN(mBar) ? 0 : mBar, interm = Privileges.IntermediaryDegree(w);
        bool byRegion = c.RuleOrderByRegion > 0;
        double tgtNum = 0, tgtDen = 0;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            if (!(rp > 0)) continue;
            tgtNum += rp * (RuleTargetShares(w, c, r, byRegion, orderOld, rNow, mBar0, interm)[0] * RuleClamp01(r.AdminFill)); tgtDen += rp;
        }
        double tgtStateReach = tgtDen > 0 ? tgtNum / tgtDen : 1.0;
        foreach (var r in w.Regions)
        {
            var rc = Places.Coord(r.Name);
            double km = rc.Lat == 0 && rc.Lon == 0 ? double.NaN : Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            bool ok = r.Rule.Length == 4;
            double reach = Politics.RegionReach(w, c, r);
            var (rmil, rsize) = RuleCrowdMilitancy(r);
            double mReg = rsize > 1e-9 ? rmil / rsize : double.NaN;
            double orderNew = !(mReg > 0) ? 1.0 : RuleClamp01(rNow * mBar / mReg);
            double orderLag = double.IsNaN(rLag) ? double.NaN : !(mReg > 0) ? 1.0 : RuleClamp01(rLag * mBar / mReg);
            string p1 = string.Join('\t', F(rNow), F(mReg), F(mBar), F(orderNew),
                F(ok ? (1.0 - orderOld) * (1.0 - reach) : double.NaN), F(ok ? (1.0 - orderNew) * (1.0 - reach) : double.NaN),
                F(rLag), F(orderLag), F(ok ? (1.0 - orderLag) * (1.0 - reach) : double.NaN),
                RuleOldTargetCells(w, c, r, ok, byRegion, orderOld, rNow, mBar0, interm, tgtStateReach));
            sk.Rule.WriteLine(head + string.Join('\t', r.Name, r.Tier.ToString(), F(r.SumPopulation()),
                F(ok ? r.Rule[0] : double.NaN), F(ok ? r.Rule[1] : double.NaN), F(ok ? r.Rule[2] : double.NaN), F(ok ? r.Rule[3] : double.NaN),
                F(r.RuleInit.Length == 4 ? r.RuleInit[1] : double.NaN), I(r.ForeignRulers.Length), F(r.Dominance), F(km), F(reach))
                + "\t" + nat + "\t" + p1);
        }
        RulePrevStore(w);
    }

    // ---- (1147) 段 C′ の計器 (c)：旧の規則の目標（Politics.cs の寄せの目標〔tUngov・tLocal・tForeign・tState〕と 1 歩の式を計器が独立に書く）----
    sealed class RuleRegPrev { public readonly double[] Prev = new double[4]; public bool Has; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, RuleRegPrev> RuleRegPrevs = new();

    /// <summary>四半期の末の Rule を次の四半期の 1 歩の自己検査のために置く（計器の側の写し・エンジンの欄を足さない）。</summary>
    static void RulePrevStore(World w)
    {
        foreach (var r in w.Regions)
        {
            if (r.Rule.Length != 4) continue;
            var p = RuleRegPrevs.GetValue(r, _ => new RuleRegPrev());
            for (int k = 0; k < 4; k++) p.Prev[k] = r.Rule[k];
            p.Has = true;
        }
    }

    /// <summary>旧の目標 t＝[state, local, foreign, ungov]（純関数・Politics.cs:946〜962 の式の逐語）。</summary>
    internal static double[] RuleOldTarget(double orderR, double reach, double ruleInit1, double intermediary, double foreignSum)
    {
        double tUngov = (1.0 - orderR) * (1.0 - reach);
        double tLocal = System.Math.Min(1.0 - tUngov, ruleInit1 * intermediary);
        double tForeign = foreignSum;
        double tState = System.Math.Max(0, 1.0 - tLocal - tForeign - tUngov);
        return [tState, tLocal, tForeign, tUngov];
    }

    /// <summary>旧の寄せの 1 歩（純関数）：v＝r＋(t−r)×速度・負は 0・和で割る（和が 0 なら Rule[0]＝1）。</summary>
    internal static double[] RuleOldStep(double[] rule, double[] t, double speed)
    {
        var o = new double[4]; double sum = 0;
        for (int k = 0; k < 4; k++) { double v = rule[k] + (t[k] - rule[k]) * speed; if (!(v > 0)) v = 0; o[k] = v; sum += v; }
        if (sum > 1e-9) for (int k = 0; k < 4; k++) o[k] /= sum; else o[0] = 1.0;
        return o;
    }

    /// <summary>目標の取り分（和が 1 になるよう割った値＝寄せの不動点）。</summary>
    internal static double[] RuleNormalize(double[] t)
    {
        double sum = 0; foreach (var v in t) sum += v;
        if (sum > 1e-9) return [t[0] / sum, t[1] / sum, t[2] / sum, t[3] / sum];
        return [1.0, 0, 0, 0];
    }

    /// <summary>持ち主の取り分の読み手（RuleDelivery の式 Politics.cs:1114 の逐語・純関数）。</summary>
    internal static double RuleDeliveryOf(double[] rule, double adminFill, double taxDiscount)
    {
        if (rule.Length != 4) return 1.0;
        double deliver = 1.0 - RuleClamp01(taxDiscount);
        double v = RuleClamp01(rule[0]) * (deliver + (1.0 - deliver) * RuleClamp01(adminFill)) + RuleClamp01(rule[1]) * deliver;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }

    static double RuleForeignSumOf(Region r)
    {
        double v = 0; foreach (var x in r.ForeignShares) if (x > 0) v += x;
        return !(v >= 0) ? 0 : System.Math.Min(1.0, v);
    }

    /// <summary>州の旧の目標（国の定数は呼び手が渡す）。</summary>
    internal static double[] RuleTargetRaw(World w, Coefficients c, Region r, bool byRegion, double orderOld, double rawOrder, double mBar, double intermediary)
    {
        double orderR = orderOld;
        if (byRegion)
        {
            var (rmil, rsize) = RuleCrowdMilitancy(r);
            double mr = rsize > 1e-9 ? rmil / rsize : 0;
            orderR = !(mr > 0) ? 1.0 : RuleClamp01(rawOrder * mBar / mr);
        }
        return RuleOldTarget(orderR, Politics.RegionReach(w, c, r), r.RuleInit.Length == 4 ? r.RuleInit[1] : 0, intermediary, RuleForeignSumOf(r));
    }

    static double[] RuleTargetShares(World w, Coefficients c, Region r, bool byRegion, double orderOld, double rawOrder, double mBar, double intermediary)
        => RuleNormalize(RuleTargetRaw(w, c, r, byRegion, orderOld, rawOrder, mBar, intermediary));

    static string RuleOldTargetCells(World w, Coefficients c, Region r, bool ok, bool byRegion, double orderOld, double rawOrder, double mBar, double intermediary, double tgtStateReach)
    {
        if (!ok) return string.Join('\t', F(double.NaN), F(double.NaN), F(double.NaN), F(double.NaN), F(double.NaN), F(double.NaN), "nan", "nan", F(double.NaN), F(tgtStateReach));
        var t = RuleTargetRaw(w, c, r, byRegion, orderOld, rawOrder, mBar, intermediary);
        var sh = RuleNormalize(t);
        double disc = c.PrivilegeTaxDiscount.Value;
        double resid = double.NaN;
        if (RuleRegPrevs.TryGetValue(r, out var pv) && pv.Has)
        {
            var pred = RuleOldStep(pv.Prev, t, c.DominanceSpeed);
            resid = 0; for (int k = 0; k < 4; k++) resid = System.Math.Max(resid, System.Math.Abs(pred[k] - r.Rule[k]));
        }
        return string.Join('\t', F(sh[0]), F(sh[1]), F(sh[2]), F(sh[3]), F(RuleDeliveryOf(r.Rule, r.AdminFill, disc)), F(RuleDeliveryOf(sh, r.AdminFill, disc)),
            r.Rule[1] > r.Rule[0] ? "1" : "0", sh[1] > sh[0] ? "1" : "0", F(resid), F(tgtStateReach));
    }
}
