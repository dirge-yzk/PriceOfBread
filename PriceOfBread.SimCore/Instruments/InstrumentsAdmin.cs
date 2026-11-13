namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsAdmin.cs#1〕
public static partial class Instruments
{
    internal const string AdmRuleHeaderText = "quarter\tyear\tnation\tadminFillShare\truleStateShare\truleLocalShare\tdeliveryOldShare\tdeliveryShare\tunservedStateShare\tstateReachOldShare\tstateReachShare\tprovincesCount\truleCutRateAllShare\truleCutRateLordedShare\tlordlessPopShare\tmanLimitK\tmanLimitOldK\tseatsK\tpopSideDecidesFlag\tsoldierK\texcessSoldierK\tatWarFlag\tnobleIncomeSumPerMonth\tnobleIncomeMaxPerCapPerMonth";

    static void AdmRuleFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        double d = 1.0 - PoliticalQuantities.Clamp01(c.PrivilegeTaxDiscount.Value);
        double den = 0, fill = 0, r0 = 0, r1 = 0, dOld = 0, dNew = 0, unserved = 0, reachOld = 0; int n = 0;
        double cutAll = 0, cutLorded = 0, lordless = 0, manOld = 0, nobleSum = 0, nobleMaxPerCap = 0;
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            bool ok = r.Rule.Length == 4;
            double s0 = ok ? PoliticalQuantities.Clamp01(r.Rule[0]) : 1.0, s1 = ok ? PoliticalQuantities.Clamp01(r.Rule[1]) : 0.0, f = PoliticalQuantities.Clamp01(r.AdminFill);
            den += pop; n++;
            fill += pop * f; r0 += pop * s0; r1 += pop * s1;
            dOld += pop * (s0 + s1 * d); dNew += pop * Politics.RuleDelivery(r, c);
            unserved += pop * (s0 * (1.0 - f)); reachOld += pop * Politics.RuleReach(r);
            // 仲介者の取り分の率（Fiscal.TaxScan.Of の cut と同じ式の読み）：領主が居る州だけが領主へ移し、居ない州は納税者の手元に残る（＝減税として働く）
            bool hasLord = false;
            foreach (var lp in r.Pops)
                if (lp.ClassId == PopClass.Noble && !lp.IsSoldier && lp.Size > 1e-9)
                {
                    hasLord = true;
                    double inc = Math.Max(0, lp.LastEarnedPerMonth.Value); nobleSum += inc; nobleMaxPerCap = Math.Max(nobleMaxPerCap, inc / lp.Size);
                }
            double cutRate = ok ? (s1 + s0 * (1.0 - f)) * (1.0 - d) : 0.0;
            cutAll += pop * cutRate; if (hasLord) cutLorded += pop * cutRate; else lordless += pop;
            // mNow〔履歴 InstrumentsAdmin.cs#2〕
            double mNow = Military.RegionManpower(r, c), dNow = Politics.RuleDelivery(r, c);
            manOld += dNow > 0 ? mNow / dNow * (s0 + s1 * d) : 0.0;
        }
        if (!(den > 0)) return;
        // 定員（EstablishmentOf＝min(経済の側, 人口の側)）と平時の復員の対象（Meritocracy ⑦ と同じ州按分で、兵士 POP が席を超えた分）
        double seats = Military.EstablishmentOf(w, c, out double natMan), soldier = 0, excess = 0;
        double manLimit = c.MaxSoldierFraction.Value * natMan;
        foreach (var r in w.Regions)
        {
            Pop? sp = null;
            foreach (var p in r.Pops) if (p.IsSoldier) { sp = p; break; }
            if (sp is null) continue;
            soldier += sp.Size;
            if (natMan > 1e-9) excess += Math.Max(0, sp.Size - seats * (Military.RegionManpower(r, c) / natMan));
        }
        int popSide = seats > 0 && Math.Abs(seats - manLimit) <= 1e-9 * Math.Max(1.0, manLimit) ? 1 : 0;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.AdmRule ??= Open("admrule", seed);
        if (!sk.AdmRuleHeader) { sk.AdmRule.WriteLine(AdmRuleHeaderText); sk.AdmRuleHeader = true; }
        sk.AdmRule.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(fill / den), F(r0 / den), F(r1 / den), F(dOld / den), F(dNew / den), F(unserved / den), F(reachOld / den), F(Politics.StateReach(w, c)), I(n),
            F(cutAll / den), F(cutLorded / den), F(lordless / den), F(manLimit), F(c.MaxSoldierFraction.Value * manOld), F(seats), I(popSide), F(soldier), F(excess), I(w.AtWarNow ? 1 : 0), F(nobleSum), F(nobleMaxPerCap)));
    }
}
