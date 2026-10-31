namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (707) 線 u4z：資本の U4（私的な建設の予算を流れへ）の段0 の表 consf_s（全期間・読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRegionProd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない・CostLedger を触らない。
// 出所：設計書 docs/202610/20261030/20261030_資本の在庫と投資_設計.md §3-5・§R2 高②・§R3 ④・(701) smu1 の推奨（cons_s は 1783〜1823 年の窓だけ＝U4 の門が読む量を全期間で INS の中に先に足す）。
// ■consf_s<種>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ・助走の後の全ての四半期）：国×区分（home＝Core と Integrated の州／Colonial＝植民地格の州）×四半期。
//   州×月の判定（EconomyStep の私的な建設の min の 1 回＝ConsRegion の呼び）ごとに数える＝月の単位で〈予算が希望を切ったか〉を見る（(701) u4base.py の〈四半期の月の平均で判定〉の限界を外す）。
//   今の規則：予算＝IndInvestmentFrac×max(0, 投資家の貯蓄)×(1−接収の萎縮)（EconomyStep の budget）。切った＝計画＜希望×(1−1e-9)（希望が正の州×月だけ）。
//   流れの影（U4 の形・§R2 高②の定義）：影の予算＝max(0, 投資家 POP の前月の所得〔LastEarnedPerMonth〕−前月の消費の支払い〔EntryCtx.Finish の cons＝消費の財のループの前後の PaidPerMonth の差〕)×max(0, 1−接収の萎縮)。
//     前月の消費の支払いは、前の月の Finish で投資家 POP の添字とともに写しておく（計器の側の欄・シミュの状態ではない）。写しが無い月（助走の明けの最初の月・投資家 POP の添字が替わった月）は〈影が無い〉として数え、切ったとは数えない。
//   周期 2 の振動の材料：州ごとの月の建設の支払い x_m（決済 ConsRegionPaid の cost の和・建てない月は 0）の差 d_m＝x_m−x_{m−1}。州×月で d_m と d_{m−1} が揃う月だけ
//     |d|・|x_m−2x_{m−1}+x_{m−2}|・d_m×d_{m−1}・d_m²・d_{m−1}² を足す＝読み手が ρ＝Σd_m d_{m−1}÷√(Σd_m²×Σd_{m−1}²)（−1〜1・純な周期 2 で −1・白色雑音で −0.5・滑らかな動きで 0 の近く〜正）と振れ幅 Σ|d|÷Σx を作る。
//   flowCurrentPlannedMoneySum＝影の判定がある州×月（希望が正）の今の計画の和＝影の計画と同じ州×月で比べる分母。
// ⚠限界（計器が自己申告する）：①影は静的な反事実＝希望・所得・消費は今の規則の軌道のまま（U4 の腕では応答する）。
//   ②今の規則の消費の支払いは投資の取り置き（EconomyStep の投資家の RemainingBudget×(1−IndInvestmentFrac)）の後の値＝U4 で取り置きを外すと消費は増えうる＝腕の流れは影より小さい向き。
//   ③州×月の判定は cap（資本の区画）と投資家 POP がある州だけ＝それ以外の州は数えない。④区分は四半期の末の州の Tier（四半期の中で Tier が替わった州の月は替わった後の区分へ入る）。
//   ⑧工業化度の列は置かない（IndustrializationReaderTests の許可表＝新しい読み手を足さない）。⑤州の持ち主が四半期の中で替わると、その州の積みは四半期の末の持ち主の行へ入る。⑥振動の差は判定が呼ばれた月の並び（判定の無い月を飛ばして隣どうしとする）。⑦値は G10 で書く。
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
        // (714) 番人の口（表の列ではない）：影が立った州×月で、本体の予算〈旗 InvestBudgetFromFlow が 1 のとき流れそのもの〉と影の予算の差の絶対値の和と、その月の数
        public double FlowBudgetGapAbs; public long FlowBudgetGapN, FlowBudgetHeldN;
        public void ClearQuarter()
        {
            Judged = Desiring = Bound = FlowAvail = FlowDesiring = FlowBound = Osc = 0;
            FlowBudgetGapAbs = 0; FlowBudgetGapN = 0; FlowBudgetHeldN = 0;
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
            double rawFlow = flow;   // (714) 番人の口：萎縮を掛ける前の流れ（本体は貯蓄までに留めてから萎縮を掛ける）
            if (r.SeizureChill > 0) flow *= keep;   // 本体の budget と同じ作法（萎縮が正の州だけ掛ける）
            {
                double held = r.Pops[ip].Savings.Value; if (!(held > 0)) held = 0;
                double expect = rawFlow < held ? rawFlow : held; if (r.SeizureChill > 0) expect *= keep;
                a.FlowBudgetGapAbs += Math.Abs(budget - expect); a.FlowBudgetGapN++;   // (714) 影の流れは変えない（pin の fbind は基線の影のまま）・番人だけ貯蓄までの留めを読む
                if (budget < flow * (1 - 1e-9)) a.FlowBudgetHeldN++;
            }
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
        foreach (var r in w.Regions) if (U4Accs.TryGetValue(r, out var a)) a.ClearAll();
    }

    /// <summary>四半期の末（挙げた国だけ）：consf_s の行（区分 2 本）を書き、四半期の積みを戻す（月をまたぐ写しは残す）。</summary>
    static void ConsfRows(Sink sk, ulong seed, int q, World w)
    {
        sk.Consf ??= Open("consf", seed);
        if (!sk.ConsfHeader) { sk.Consf.WriteLine(ConsfHeaderText); sk.ConsfHeader = true; }
        string I(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string nation = NameOf(w);
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
            if (n == 0) continue;
            sk.Consf.WriteLine(string.Join('\t', I(q), I(w.Year), nation, ConsfScopes[s], I(n), F(pop), F(invSav),
                I(judged), I(desiring), I(bound), F(desired), F(budget), F(planned), F(cut),
                I(fAvail), I(fDes), I(fBound), F(fDesired), F(fBudget), F(fPlanned), F(fEarn), F(fCons),
                F(paid), F(built), I(osc), F(oP), F(oD1), F(oD2), F(oLag), F(oSq), F(oSqLag), F(fCurPlanned)));
        }
    }
}
