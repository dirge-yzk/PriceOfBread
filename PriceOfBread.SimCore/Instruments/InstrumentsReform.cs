namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（欄は計器の WState）。〔履歴 InstrumentsReform.cs#1〕
public static partial class Instruments
{
    internal const string ReformHeaderText = "quarter\tyear\tnation\tlegitimacyShare\tmandateStockIndex\tmandateMaxIndex\tmandateSpentNewIndex\tsupportShare\twallPassCount\twallPassLawCount\twallPassSliderCount\twallPassCostIndex\t"
        + "educationLawCode\teducationSpendingShare\tparishFundingShare\teducationBuyPerMonth\tliteracyShare\tsuffrageLawCode\tcensorshipLawCode\tlandReformLawCode\tbureaucracyLawCode\tserfLawCode\tpolityCode\t"
        + "militaryPushIndex\tcoopParliamentCrownIndex\tblockedLawCount\tblockedSliderCount\tblockedWhileFrozenCount\twallPassAxes\twallPassCostShareOfAvailMax\twallLegitBlockedCount\t"
        + "spendStockAppliedIndex\tspendLegitOnlyIndex\tspendStockShareOfMaxMax\tconcedeSpendIndex";   // tspendStockShareOfMaxMax〔履歴 InstrumentsReform.cs#2〕

    /// <summary>壁の分岐で通った一歩（<see cref="Government.PoliticalCapitalAffords"/> の第 2 枝）を数える。呼び手は払う前（Spend の前）に呼ぶ＝S は払う前の値。</summary>
    internal static void WallPass(World w, double cost, bool law, PolicyAxis a)
    {
        if (!(cost > w.PoliticalCapitalMax) || w.MandateSpent + cost <= w.PoliticalCapitalMax) return;
        var st = S(w);
        if (law) st.RfWallLaw++; else st.RfWallSlider++;
        st.RfWallCost += cost;
        st.RfWallAxes += (st.RfWallAxes.Length > 0 ? "," : "") + a;
        double avail = w.Legitimacy - w.MandateSpentNew;   // avail〔履歴 InstrumentsReform.cs#3〕
        if (avail > 0 && cost / avail > st.RfWallShareMax) st.RfWallShareMax = cost / avail;
    }

    /// <summary>
    /// 旧の注〈A の表が旧の世界の止まりの表になる〉は点火前の腕 A の話）。〔履歴 InstrumentsReform.cs#4〕
    /// </summary>
    internal static void RfBlocked(World w, bool law, double cost)
    {
        var st = S(w);
        if (law) st.RfBlkLaw++; else st.RfBlkSlider++;
        if (w.MandateSpent > w.PoliticalCapitalMax) st.RfBlkFrozen++;
        if (w.MandateSpent <= 0 && cost > w.PoliticalCapitalMax) st.RfBlkLegit++;   // PoliticalCapitalMax〔履歴 InstrumentsReform.cs#5〕
    }

    /// <summary>
    /// 〔履歴 InstrumentsReform.cs#6〕
    /// </summary>
    internal static void SpendApplied(World w, double amount, double applied)
    {
        var st = S(w);
        st.RfSpendApplied += applied;
        st.RfSpendLegitOnly += amount - applied;
        double b = w.PoliticalCapitalMax;
        if (b > 0 && applied / b > st.RfSpendShareMax) st.RfSpendShareMax = applied / b;
    }

    /// <summary>
    /// 〔履歴 InstrumentsReform.cs#7〕
    /// </summary>
    internal static void ConcedeSpent(World w, double spend) => S(w).RfConcede += spend;

    static void ReformFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;   // 書かない四半期は積み上げたまま（次に書く行が前の行からの和を持つ）
        double cum = 0, fundNum = 0, popSum = 0, litNum = 0, litDen = 0;
        foreach (var r in w.Regions)
        {
            cum += r.CumEducationCost;
            double pop = r.SumPopulation();
            if (pop > 0) { fundNum += pop * r.CompulsoryEduFunding; popSum += pop; }
            foreach (var p in r.Pops) { if (p.IsSoldier) continue; litNum += p.Size * p.Literacy; litDen += p.Size; }
        }
        int since = st.RfPrevQ < 0 ? 1 : Math.Max(1, q - st.RfPrevQ);   // 前の行からの四半期数（挙げた国は 1・他は 4）
        double buy = double.IsNaN(st.RfCumEduPrev) ? 0 : (cum - st.RfCumEduPrev) / (3.0 * since);
        int wallLaw = st.RfWallLaw, wallSlider = st.RfWallSlider, blkLaw = st.RfBlkLaw, blkSlider = st.RfBlkSlider, blkFrozen = st.RfBlkFrozen, blkLegit = st.RfBlkLegit; double wallShareMax = st.RfWallShareMax; double wallCost = st.RfWallCost; string wallAxes = st.RfWallAxes.Length > 0 ? st.RfWallAxes : "-";
        double spApplied = st.RfSpendApplied, spLegitOnly = st.RfSpendLegitOnly, spShareMax = st.RfSpendShareMax, spConcede = st.RfConcede;
        st.RfCumEduPrev = cum; st.RfPrevQ = q; st.RfWallLaw = 0; st.RfWallSlider = 0; st.RfWallCost = 0; st.RfBlkLaw = 0; st.RfBlkSlider = 0; st.RfBlkFrozen = 0; st.RfBlkLegit = 0; st.RfWallShareMax = 0; st.RfWallAxes = "";
        st.RfSpendApplied = 0; st.RfSpendLegitOnly = 0; st.RfSpendShareMax = 0; st.RfConcede = 0;
        var c = w.Coef;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Reform ??= Open("reform", seed);
        if (!sk.ReformHeader) { sk.Reform.WriteLine(ReformHeaderText); sk.ReformHeader = true; }
        double aff = Ideologies.SupportAt(Ideologies.National(w), PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        double sup = PoliticalQuantities.Clamp01((aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin));
        sk.Reform.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(w.Legitimacy), F(w.MandateSpent), F(w.PoliticalCapitalMax), F(w.MandateSpentNew), F(sup),
            I(wallLaw + wallSlider), I(wallLaw), I(wallSlider), F(wallCost),
            I((int)w.EducationLaw), F(w.EducationSpending), F(popSum > 0 ? fundNum / popSum : 0), F(buy), F(litDen > 0 ? litNum / litDen : double.NaN),
            I((int)w.Suffrage), I((int)w.Censorship), I((int)w.LandReform), I((int)w.Bureaucracy), I((int)w.SerfEmancipation), I((int)w.Polity),
            F(PoliticalQuantities.MilitaryPush(w)), F(w.CoopParliamentCrown), I(blkLaw), I(blkSlider), I(blkFrozen), wallAxes, F(wallShareMax), I(blkLegit),
            F(spApplied), F(spLegitOnly), F(spShareMax), F(spConcede)));
    }
}
