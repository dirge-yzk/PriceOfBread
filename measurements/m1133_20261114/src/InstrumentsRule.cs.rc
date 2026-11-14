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
    internal const string RuleP1HeaderText = "\torderRawIndex\tmilitancyRegionIndex\tmilitancyNationIndex\torderRegionIndex\tungovTargetOldShare\tungovTargetNewShare\torderRawLagIndex\torderRegionLagIndex\tungovTargetNewLagShare";

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
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
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
                F(rLag), F(orderLag), F(ok ? (1.0 - orderLag) * (1.0 - reach) : double.NaN));
            sk.Rule.WriteLine(head + string.Join('\t', r.Name, r.Tier.ToString(), F(r.SumPopulation()),
                F(ok ? r.Rule[0] : double.NaN), F(ok ? r.Rule[1] : double.NaN), F(ok ? r.Rule[2] : double.NaN), F(ok ? r.Rule[3] : double.NaN),
                F(r.RuleInit.Length == 4 ? r.RuleInit[1] : double.NaN), I(r.ForeignRulers.Length), F(r.Dominance), F(km), F(reach))
                + "\t" + nat + "\t" + p1);
        }
    }
}
