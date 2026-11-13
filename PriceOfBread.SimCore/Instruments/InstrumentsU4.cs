namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない・CostLedger を触らない。〔履歴 InstrumentsU4.cs#1〕
public static partial class Instruments
{
    internal const string ConsfHeaderText = "quarter\tyear\tnation\tscope\tregionsCount\tpopulationK\tinvestorSavingsStock\t"
        + "judgedRegionMonthsCount\tdesiringRegionMonthsCount\tbudgetBoundRegionMonthsCount\tdesiredMoneySum\tbudgetMoneySum\tplannedMoneySum\tbudgetBoundCutMoneySum\t"
        + "flowAvailableRegionMonthsCount\tflowDesiringRegionMonthsCount\tflowBoundRegionMonthsCount\tflowDesiredMoneySum\tflowBudgetMoneySum\tflowPlannedMoneySum\tflowEarnedLastMoneySum\tflowConsumptionPaidLastMoneySum\t"
        + "paidMoneySum\tbuiltUnitsSum\toscRegionMonthsCount\toscPaidMoneySum\toscAbsFirstDiffMoneySum\toscAbsSecondDiffMoneySum\toscFirstDiffLagProductSum\toscFirstDiffSquareSum\toscFirstDiffLagSquareSum\tflowCurrentPlannedMoneySum";
    static readonly string[] ConsfScopes = ["home", "Colonial"];

    /// <summary>州ごとの積み（四半期の和）と、月をまたいで持つ写し（前月の消費の支払い・振動の履歴）。</summary>
    internal sealed class U4Acc
    {
        public long Judged, Desiring, Bound, FlowAvail, FlowDesiring, FlowBound, Osc;
        public double Desired, Budget, Planned, BoundCut, FlowDesired, FlowBudget, FlowPlanned, FlowCurPlanned, FlowEarned, FlowCons, Paid, Built, OscPaid, OscAbsD1, OscAbsD2, OscLag, OscSq, OscSqLag;
        // 月をまたぐ写し（四半期の末で戻さない）
        public bool HasLastCons; public int LastConsPop = -1; public double LastCons;
        public double CurPaid; public int Hist; public double H0, H1, D0;
        public void ClearQuarter()
        {
            Judged = Desiring = Bound = FlowAvail = FlowDesiring = FlowBound = Osc = 0;
            Desired = Budget = Planned = BoundCut = FlowDesired = FlowBudget = FlowPlanned = FlowCurPlanned = FlowEarned = FlowCons = Paid = Built = OscPaid = OscAbsD1 = OscAbsD2 = OscLag = OscSq = OscSqLag = 0;
        }
        public void ClearAll() { ClearQuarter(); HasLastCons = false; LastConsPop = -1; LastCons = 0; CurPaid = 0; Hist = 0; H0 = H1 = D0 = 0; }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, U4Acc> U4Accs = new();
    /// <summary>番人（テスト）が州の積みを直に読む口。無ければ null。</summary>
    internal static U4Acc? U4Of(Region r) => U4Accs.TryGetValue(r, out var a) ? a : null;

    /// <summary>EntryCtx.Finish（消費の財のループの後）から：投資家 POP の今月の消費の支払いを写す（翌月の判定が〈前月の消費の支払い〉として読む）。挙げた国だけ（EntryBegin が決める）。</summary>
    internal static void U4ConsStash(Region r, int popIndex, double consumptionPaid)
    {
        var a = U4Accs.GetValue(r, _ => new U4Acc());
        a.HasLastCons = true; a.LastConsPop = popIndex; a.LastCons = consumptionPaid;
    }

    /// <summary>ConsRegion（私的な建設の判定・州×月）から：今の規則と流れの影の切りを数え、前の月の建設の支払いを振動の履歴へ畳む。
    /// <paramref name="desiredUnits"/>・<paramref name="budgetUnits"/>・<paramref name="unitMoney"/>・<paramref name="plannedUnits"/>＝本体の min の 2 つの引数・束の費用×(1+工賃)・min の結果（本体から引数で受ける・計算し直さない）。</summary>
    internal static void U4Decide(Region r, double desiredUnits, double budgetUnits, double unitMoney, double plannedUnits)
    {
        var a = U4Accs.GetValue(r, _ => new U4Acc());
        // 振動の履歴：判定は決済より前＝この時点の CurPaid は前に判定した月の支払いの和（建てない月は 0）
        double x = a.CurPaid; a.CurPaid = 0;
        if (a.Hist >= 1)
        {
            double d = x - a.H0;
            if (a.Hist >= 2)
            {
                a.Osc++; a.OscPaid += x;
                a.OscAbsD1 += Math.Abs(d); a.OscAbsD2 += Math.Abs(x - 2.0 * a.H0 + a.H1);
                a.OscLag += d * a.D0; a.OscSq += d * d; a.OscSqLag += a.D0 * a.D0;
            }
            a.D0 = d;
        }
        a.H1 = a.H0; a.H0 = x; if (a.Hist < 2) a.Hist++;

        a.Judged++;
        double desired = desiredUnits * unitMoney, budget = budgetUnits * unitMoney, planned = plannedUnits * unitMoney;
        bool desiring = desiredUnits > 0;
        if (desiring)
        {
            a.Desiring++; a.Desired += desired;
            if (planned < desired * (1 - 1e-9)) { a.Bound++; a.BoundCut += desired - planned; }
        }
        a.Budget += budget; a.Planned += planned;

        int ip = r.InvestorPopIndex;
        if (a.HasLastCons && a.LastConsPop == ip && ip >= 0 && ip < r.Pops.Count)
        {
            double earned = r.Pops[ip].LastEarnedPerMonth.Value;
            double flow = earned - a.LastCons; if (!(flow > 0)) flow = 0;
            double keep = 1.0 - r.SeizureChill; if (!(keep > 0)) keep = 0;
            if (r.SeizureChill > 0) flow *= keep;   // 本体の budget と同じ作法（萎縮が正の州だけ掛ける）
            a.FlowAvail++; a.FlowBudget += flow; a.FlowEarned += earned; a.FlowCons += a.LastCons;
            if (desiring)
            {
                a.FlowDesiring++; a.FlowDesired += desired;
                double fp = desired < flow ? desired : flow;
                a.FlowPlanned += fp; a.FlowCurPlanned += planned;
                if (flow < desired * (1 - 1e-9)) a.FlowBound++;
            }
        }
    }

    /// <summary>ConsRegionPaid（建設の決済・州×月）から：払った額と建った束。</summary>
    internal static void U4Paid(Region r, double builtUnits, double cost)
    {
        var a = U4Accs.GetValue(r, _ => new U4Acc());
        a.CurPaid += cost; a.Paid += cost; a.Built += builtUnits;
    }

    /// <summary>助走の明け（ClearCons）：全ての写しを戻す（助走の月の消費・振動の履歴を持ち越さない）。</summary>
    static void U4ClearWorld(World w)
    {
        U4Departed.Remove(w);   // Remove〔履歴 InstrumentsU4.cs#2〕
        foreach (var r in w.Regions) if (U4Accs.TryGetValue(r, out var a)) a.ClearAll();
    }

    /// <summary>
    /// cons_s と同じ穴（州が国を出入りした四半期に積みが戻った国の行として出る）の同じ直し。〔履歴 InstrumentsU4.cs#3〕
    /// </summary>
    static void U4Depart(World from, Region r)
    {
        if (!U4Accs.TryGetValue(r, out var a)) return;
        var snap = new U4Acc(); snap.Judged = a.Judged; snap.Desiring = a.Desiring; snap.Bound = a.Bound; snap.FlowAvail = a.FlowAvail; snap.FlowDesiring = a.FlowDesiring; snap.FlowBound = a.FlowBound; snap.Osc = a.Osc;
        snap.Desired = a.Desired; snap.Budget = a.Budget; snap.Planned = a.Planned; snap.BoundCut = a.BoundCut; snap.FlowDesired = a.FlowDesired; snap.FlowBudget = a.FlowBudget; snap.FlowPlanned = a.FlowPlanned; snap.FlowCurPlanned = a.FlowCurPlanned; snap.FlowEarned = a.FlowEarned; snap.FlowCons = a.FlowCons;
        snap.Paid = a.Paid; snap.Built = a.Built; snap.OscPaid = a.OscPaid; snap.OscAbsD1 = a.OscAbsD1; snap.OscAbsD2 = a.OscAbsD2; snap.OscLag = a.OscLag; snap.OscSq = a.OscSq; snap.OscSqLag = a.OscSqLag;
        a.ClearQuarter();
        U4Departed.GetValue(from, _ => new List<(RegionTier Tier, U4Acc Acc)>()).Add((r.Tier, snap));
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, List<(RegionTier Tier, U4Acc Acc)>> U4Departed = new();

    /// <summary>四半期の末（挙げた国だけ）：consf_s の行（区分 2 本）を書き、四半期の積みを戻す（月をまたぐ写しは残す）。</summary>
    static void ConsfRows(Sink sk, ulong seed, int q, World w)
    {
        sk.Consf ??= Open("consf", seed);
        if (!sk.ConsfHeader) { sk.Consf.WriteLine(ConsfHeaderText); sk.ConsfHeader = true; }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
        List<(RegionTier Tier, U4Acc Acc)>? departed = null;
        if (U4Departed.TryGetValue(w, out var dl)) { departed = new(dl); U4Departed.Remove(w); }   // TryGetValue〔履歴 InstrumentsU4.cs#4〕
        for (int s = 0; s < ConsfScopes.Length; s++)
        {
            bool colonial = s == 1;
            long n = 0, judged = 0, desiring = 0, bound = 0, fAvail = 0, fDes = 0, fBound = 0, osc = 0;
            double pop = 0, invSav = 0, desired = 0, budget = 0, planned = 0, cut = 0, fDesired = 0, fBudget = 0, fPlanned = 0, fCurPlanned = 0, fEarn = 0, fCons = 0, paid = 0, built = 0, oP = 0, oD1 = 0, oD2 = 0, oLag = 0, oSq = 0, oSqLag = 0;
            foreach (var r in w.Regions)
            {
                if ((r.Tier == RegionTier.Colonial) != colonial) continue;
                n++;
                double rp = 0; foreach (var p in r.Pops) rp += p.Size;
                pop += rp;
                if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) invSav += r.Pops[r.InvestorPopIndex].Savings.Value;
                if (!U4Accs.TryGetValue(r, out var a)) continue;
                judged += a.Judged; desiring += a.Desiring; bound += a.Bound; fAvail += a.FlowAvail; fDes += a.FlowDesiring; fBound += a.FlowBound; osc += a.Osc;
                desired += a.Desired; budget += a.Budget; planned += a.Planned; cut += a.BoundCut; fDesired += a.FlowDesired; fBudget += a.FlowBudget; fPlanned += a.FlowPlanned; fCurPlanned += a.FlowCurPlanned; fEarn += a.FlowEarned; fCons += a.FlowCons;
                paid += a.Paid; built += a.Built; oP += a.OscPaid; oD1 += a.OscAbsD1; oD2 += a.OscAbsD2; oLag += a.OscLag; oSq += a.OscSq; oSqLag += a.OscSqLag;
                a.ClearQuarter();
            }
            bool anyDeparted = false;
            if (departed is not null) foreach (var (tier, a) in departed)
            {
                if ((tier == RegionTier.Colonial) != colonial) continue;
                anyDeparted = true;   // anyDeparted〔履歴 InstrumentsU4.cs#5〕
                judged += a.Judged; desiring += a.Desiring; bound += a.Bound; fAvail += a.FlowAvail; fDes += a.FlowDesiring; fBound += a.FlowBound; osc += a.Osc;
                desired += a.Desired; budget += a.Budget; planned += a.Planned; cut += a.BoundCut; fDesired += a.FlowDesired; fBudget += a.FlowBudget; fPlanned += a.FlowPlanned; fCurPlanned += a.FlowCurPlanned; fEarn += a.FlowEarned; fCons += a.FlowCons;
                paid += a.Paid; built += a.Built; oP += a.OscPaid; oD1 += a.OscAbsD1; oD2 += a.OscAbsD2; oLag += a.OscLag; oSq += a.OscSq; oSqLag += a.OscSqLag;
            }
            if (n == 0 && !anyDeparted) continue;
            sk.Consf.WriteLine(string.Join('\t', I(q), I(w.Year), nation, ConsfScopes[s], I(n), F(pop), F(invSav),
                I(judged), I(desiring), I(bound), F(desired), F(budget), F(planned), F(cut),
                I(fAvail), I(fDes), I(fBound), F(fDesired), F(fBudget), F(fPlanned), F(fEarn), F(fCons),
                F(paid), F(built), I(osc), F(oP), F(oD1), F(oD2), F(oLag), F(oSq), F(oSqLag), F(fCurPlanned)));
        }
    }
}
