namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (371) 流れM の計器 2 本（読むだけ・設計の材料） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPop.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・メモ化された量を新しい位置で読まない。
// 出力＝ fm_s<seed>.tsv（国×四半期・挙げた国は毎四半期、他の全ての国は年末）。
//   ①戦時の軍事費の純支持（militaryMoveNetSupportIndex）の項の分解＝街頭（旧支配層・近代エリート・大衆）・議会・軍の項。戦争の規模・存亡・戦場の強さと並べて読む。
//      純支持（PolicyPreferences.NetSupport）の式は戦争の量を 1 つも読まない。戦時に動く経路は、総動員で増えた兵士の人口比が軍の項（ArmyPower）を押し上げる 1 本と、
//      議席・ブロックの力が戦時に動く分だけ。分解はその寄与の大きさを測る（読んでいるかの確認ではなくソースの読みの裏づけ）。
//   ②海の金の割合の分母＝max(陸の必要量＋内の守り, 自国の軍の大きさ)（ArmyShape.NavalSpendShareOf）の組成＝陸の必要量を（本国どうしの国境・植民地を含む国境・国境でない投射）に、
//      内の守りを（人口・不穏度・質）に分け、どちらが分母を決めているかを写す。
// ⚠限界（計器が自分で申告する）：
//   ・純支持の項は NetSupport の式の写し（街頭の譲渡率 street・Pref・ParliamentNet・ArmyPower×ArmyStake）＝式が変われば食い違う。列 netTermsGapIndex が写しと実際の純支持の差（有限の行では 1e-9 以内）。
//   ・陸の必要量の 3 部分は「h×国境」「h×本国どうしの国境」「hrLand−h×国境」の差で割る＝浮動小数の結合順が Compute と違い、ビット一致はしない（3 部分の和と NeedLand の相対差は列 needLandGapShare）。
//   ・純支持の項が出るのは軍事費の合計の施行の一歩が来た四半期だけ（目標＝現在で動かない四半期は空）。陸の必要量の組成は ArmyShape.Compute が走った後の最新の値（四半期に 1 回）。
//   ・四半期の末の戦場の強さ（region の LastWarIntensity の最大）は最後の月の値。

public static partial class Instruments
{
    /// <summary>国ごとの蓄え（hook が書き、四半期の末の <see cref="FmFold"/> が行にして空へ戻す）。</summary>
    internal sealed class FmAcc
    {
        // ① 純支持の分解（MilitaryMove の hook）
        public bool HaveNet; public int Increasing;
        public double NetTrad = double.NaN, NetModern = double.NaN, NetMass = double.NaN, NetParl = double.NaN, NetArmy = double.NaN, Street = double.NaN, ArmyPowerV = double.NaN, ParlControl = double.NaN, Polity = double.NaN;
        // ② 陸の必要量の組成（ArmyShape.Compute の hook）
        public bool HaveArmy;
        public double NeedLand = double.NaN, NeedLandBorderHome = double.NaN, NeedLandBorderColonial = double.NaN, NeedLandProjection = double.NaN, NeedLandPartsSum = double.NaN;
        public double NeedSea = double.NaN, NeedSeaFleet = double.NaN, NeedSeaCross = double.NaN;
        public double NeedInternal = double.NaN, GarrisonMen = double.NaN, UnrestShare = double.NaN, QualityQ = double.NaN, PopK = double.NaN;
        public double ForcePot = double.NaN, LandPot = double.NaN, NavalShareOfIdeal = double.NaN, LandSideDenominator = double.NaN;
        public double BorderHomeLen = double.NaN, BorderColonialLen = double.NaN, NavalPowerIndex = double.NaN;
        public int LandSideBindsFlag = -1;
        public string Top3 = "", TopColonial3 = "";
        public void ClearNet()
        {
            HaveNet = false; Increasing = -1;
            NetTrad = NetModern = NetMass = NetParl = NetArmy = Street = ArmyPowerV = ParlControl = Polity = double.NaN;
        }
        public void ClearArmy()
        {
            HaveArmy = false;
            NeedLand = NeedLandBorderHome = NeedLandBorderColonial = NeedLandProjection = NeedLandPartsSum = double.NaN;
            NeedSea = NeedSeaFleet = NeedSeaCross = NeedInternal = GarrisonMen = UnrestShare = QualityQ = PopK = ForcePot = LandPot = NavalShareOfIdeal = LandSideDenominator = BorderHomeLen = BorderColonialLen = NavalPowerIndex = double.NaN;
            LandSideBindsFlag = -1; Top3 = ""; TopColonial3 = "";
        }
    }
    static FmAcc FA(World w) { var st = S(w); return st.FmA ??= new FmAcc(); }
    /// <summary>番人（テスト）が蓄えを読むための口。</summary>
    internal static FmAcc FmAccOf(World w) => FA(w);

    // ---------------------------------------------------------------- hook ----------------------------------------------------------------

    /// <summary>軍事費の合計の施行の一歩（<see cref="MilitaryMove"/> の中・同じ瞬間の立場の土台 <paramref name="e"/>）：純支持を項に分ける。式は <see cref="PolicyPreferences.NetSupport"/> の写し。</summary>
    internal static void FmNet(World w, in PolicyPreferences.Profile e, bool increasing)
    {
        var a = FA(w);
        a.HaveNet = true; a.Increasing = increasing ? 1 : 0;
        const PolicyAxis ax = PolicyAxis.Coercion;
        double sign = increasing ? 1 : -1;
        var c = w.Coef;
        double street = c.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && PolicyPreferences.HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(c.ParliamentStreetSupersede) : 1.0;
        a.Street = street;
        a.NetTrad = street * e.Power[0] * PolicyPreferences.Pref(ax, 0, e, w) * sign;
        a.NetModern = street * e.Power[1] * PolicyPreferences.Pref(ax, 1, e, w) * sign;
        a.NetMass = street * e.Power[2] * PolicyPreferences.Pref(ax, 2, e, w) * sign;
        a.NetParl = PolicyPreferences.ParliamentNet(w, ax, sign, e);
        a.ArmyPowerV = PolicyPreferences.ArmyPower(w);
        a.NetArmy = a.ArmyPowerV * PolicyPreferences.ArmyStake(ax) * sign;
        a.ParlControl = w.ParliamentControl;
        a.Polity = (double)(int)w.Polity;
    }

    /// <summary>陸の必要量の組成（<see cref="ArmyShape.Compute"/> の終わり・必要量の書き込みが全て済んだ後）。<paramref name="home"/>＝本国どうしの国境（<see cref="ArmyShape.Borders"/> の第 3 引数）・
    /// <paramref name="hAll"/>＝敵意 h(i,j)（添字 j*n+i＝j の武力が i に届く強さ）。</summary>
    internal static void ArmyNeed(List<Power> ps, Coefficients c, double[] border, double[] home, double[] hAll, double[] hrLand, double[] hrCross, double[] hrSea, double[] landPot, double[] unrest)
    {
        int n = ps.Count;
        var sumL = new double[n]; var sumS = new double[n];
        for (int j = 0; j < n; j++)
        {
            double sl = unrest[j], ss = unrest[j];
            for (int k = 0; k < n; k++) { sl += hrLand[j * n + k] + hrCross[j * n + k]; ss += hrSea[j * n + k]; }
            sumL[j] = sl; sumS[j] = ss;
        }
        for (int i = 0; i < n; i++)
        {
            var p = ps[i]; var w = p.Nation!;
            var a = FA(w); a.ClearArmy();
            double bh = 0, bc = 0, pr = 0, cr = 0, fl = 0, lenH = 0, lenC = 0;
            var contrib = new (double v, int j)[n]; var contribCol = new (double v, int j)[n];
            int m = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                double hj = hAll[j * n + i];
                lenH += home[j * n + i]; lenC += border[j * n + i] - home[j * n + i];
                if (sumL[j] > 1e-12)
                {
                    double cf = landPot[j] / sumL[j];
                    double vH = hj * home[j * n + i] * cf, vC = hj * (border[j * n + i] - home[j * n + i]) * cf, vP = (hrLand[j * n + i] - hj * border[j * n + i]) * cf;
                    bh += vH; bc += vC; pr += vP; cr += hrCross[j * n + i] * cf;
                    contrib[m] = (hrLand[j * n + i] * cf, j); contribCol[m] = (vC, j); m++;
                }
                if (sumS[j] > 1e-12) fl += hrSea[j * n + i] / sumS[j] * ps[j].NavalPower;
            }
            Array.Sort(contrib, 0, m, Comparer<(double v, int j)>.Create((x, y) => y.v.CompareTo(x.v)));
            Array.Sort(contribCol, 0, m, Comparer<(double v, int j)>.Create((x, y) => y.v.CompareTo(x.v)));
            a.HaveArmy = true;
            a.NeedLand = p.NeedLand; a.NeedLandBorderHome = bh; a.NeedLandBorderColonial = bc; a.NeedLandProjection = pr; a.NeedLandPartsSum = bh + bc + pr;
            a.NeedSea = p.NeedSea; a.NeedSeaFleet = fl; a.NeedSeaCross = cr;
            a.NeedInternal = p.NeedInternal; a.UnrestShare = unrest[i]; a.PopK = w.TotalPopulation;
            a.GarrisonMen = c.InternalGarrisonPerPop * a.PopK * unrest[i];
            var (qt, qc, qm) = ArmyShape.Quality(w, c); a.QualityQ = qt * qc * qm;
            a.ForcePot = ArmyShape.ForcePotential(p); a.LandPot = ArmyShape.LandPotential(p);
            a.NavalShareOfIdeal = ArmyShape.NavalSpendShareOf(p);
            double needSide = p.NeedLand + p.NeedInternal;
            a.LandSideDenominator = Math.Max(needSide, a.ForcePot);
            a.LandSideBindsFlag = needSide >= a.ForcePot ? 1 : 0;
            a.BorderHomeLen = lenH; a.BorderColonialLen = lenC; a.NavalPowerIndex = p.NavalPower;
            a.Top3 = TopNames(ps, contrib, m); a.TopColonial3 = TopNames(ps, contribCol, m);
        }
    }
    static string TopNames(List<Power> ps, (double v, int j)[] sorted, int m)
    {
        var sb = new System.Text.StringBuilder();
        for (int k = 0; k < Math.Min(3, m); k++)
        {
            if (!(sorted[k].v > 0)) break;
            if (sb.Length > 0) sb.Append(';');
            sb.Append(NameOf(ps[sorted[k].j])).Append(':').Append(F(sorted[k].v));
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------- 四半期の末（Quarter から・ResetQuarter の前） ----------------------------------------------------------------

    const string FmHeader = "quarter\tyear\tnationUid\tnation\t"
        + "netIncreasingFlag\tnetStreetTradIndex\tnetStreetModernIndex\tnetStreetMassIndex\tnetParliamentIndex\tnetArmyIndex\tnetTermsSumIndex\tnetTermsGapIndex\tnetStreetFactorShare\tarmyPowerIndex\tparliamentControlShare\tpolityCode\t"
        + "moveOutcomeCode\tmoveNetSupportIndex\tmoveResistIndex\tmoveStepFraction\tmoveCurrentShare\tmoveTargetShare\t"
        + "atWarAtDecisionFlag\tmobilizedAtDecisionFlag\twarScaleRatioAtDecision\thomeLosingScoreAtDecision\tatWarMonthsCount\twarIntensityMaxIndex\tsoldierShare\t"
        + "needLandIndex\tneedLandBorderHomeIndex\tneedLandBorderColonialIndex\tneedLandProjectionIndex\tneedLandGapShare\tneedInternalIndex\tgarrisonMenIndex\tunrestShare\tqualityIndex\ttotalPopulationK\t"
        + "forcePotentialIndex\tlandPotentialIndex\tlandSideDenominatorIndex\tlandSideBindsFlag\tneedSeaIndex\tneedSeaFleetIndex\tneedSeaCrossIndex\tnavalSpendShareIdealShare\tnavalPowerIndex\t"
        + "borderHomeLengthIndex\tborderColonialLengthIndex\tneedLandTop3\tcolonialBorderTop3";

    static void FmFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        var a = st.FmA;
        if (a is null) return;
        if (!(picked || yearEnd)) { a.ClearNet(); a.ClearArmy(); return; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        double nan = double.NaN;
        double wi = 0;
        foreach (var r in w.Regions) if (r.LastWarIntensity > wi) wi = r.LastWarIntensity;
        double pop = w.TotalPopulation, soldierK = 0;   // NationRow と同じ位置・同じ読み方（兵士は州の POP を直に数える＝窓のメモを使わない）
        foreach (var r in w.Regions) foreach (var pp in r.Pops) if (pp.IsSoldier) soldierK += pp.Size;
        double soldier = pop > 0 ? soldierK / pop : nan;
        double sum = a.HaveNet ? a.NetTrad + a.NetModern + a.NetMass + a.NetParl + a.NetArmy : nan;
        double gap = a.HaveNet ? sum - st.MmNet : nan;
        double landGap = a.HaveArmy && a.NeedLand > 0 ? (a.NeedLandPartsSum - a.NeedLand) / a.NeedLand : nan;
        sk.Fm ??= Open("fm", seed);
        if (!sk.FmHeader) { sk.Fm.WriteLine(FmHeader); sk.FmHeader = true; }
        sk.Fm.WriteLine(string.Join('\t',
            I(q), I(w.Year), I(st.Uid), NameOf(w),
            a.HaveNet ? I(a.Increasing) : "", F(a.NetTrad), F(a.NetModern), F(a.NetMass), F(a.NetParl), F(a.NetArmy), F(sum), F(gap), F(a.Street), F(a.ArmyPowerV), F(a.ParlControl), F(a.Polity),
            I(st.MmOutcome), F(st.MmNet), F(st.MmResist), F(st.MmSpeed), F(st.MmCur), F(st.MmTarget),
            I(st.DecAtWar), I(st.DecMobilized), F(st.DecScale), F(st.DecLosing), I(st.WarMonths), F(wi), F(soldier),
            F(a.NeedLand), F(a.NeedLandBorderHome), F(a.NeedLandBorderColonial), F(a.NeedLandProjection), F(landGap), F(a.NeedInternal), F(a.GarrisonMen), F(a.UnrestShare), F(a.QualityQ), F(a.PopK),
            F(a.ForcePot), F(a.LandPot), F(a.LandSideDenominator), a.HaveArmy ? I(a.LandSideBindsFlag) : "", F(a.NeedSea), F(a.NeedSeaFleet), F(a.NeedSeaCross), F(a.NavalShareOfIdeal), F(a.NavalPowerIndex),
            F(a.BorderHomeLen), F(a.BorderColonialLen), a.Top3, a.TopColonial3));
        a.ClearNet(); a.ClearArmy();
    }
}
