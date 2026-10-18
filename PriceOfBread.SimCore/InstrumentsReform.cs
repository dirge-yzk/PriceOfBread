namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (404) 専制の改革の経路の表 reform_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（欄は計器の WState）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/rf_20261018_prereg.txt の記録の線 R1〜R8（専制の国の法の動き・正統性の分布・壁を通った一歩の数と代価・識字・教区学校への国庫の購入）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。（旗 MandateOverStockFromLegitimacy は (419) で焼き込み〔常に有効〕）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元の量・Count＝件数・PerMonth＝月あたりの流れ・Code＝法の段の整数）：
//   legitimacyShare／mandateStockIndex（目減り S）／mandateMaxIndex（最大量 b）／mandateSpentNewIndex（この四半期の末の今期増分＝Politics.Step が減算する前の値ではなく後の 0 になりうる）／supportShare（b を作る支持 sup）／
//   wallPassCount・wallPassLawCount・wallPassSliderCount・wallPassCostIndex（前の行からこの行までの間〔挙げた国は 1 四半期・他の国は 4 四半期〕に壁の分岐で通った一歩の数〔法・スライダーの内訳〕と代価の和）／
//   educationLawCode・educationSpendingShare／parishFundingShare（CompulsoryEduFunding の州の人口加重平均）／educationBuyPerMonth（国庫が教育に払った額＝CumEducationCost の前の行からの差を月数で割る）／literacyShare（人口加重の識字・兵士を除く）／
//   suffrageLawCode・censorshipLawCode・landReformLawCode・bureaucracyLawCode・serfLawCode・polityCode／militaryPushIndex（MilitaryPush＝S を粛清の記憶として読む）・coopParliamentCrownIndex（S を紛争記憶として読む）／blockedLawCount・blockedSliderCount（資本の門で止まった一歩・旗に依らず数える）・blockedWhileFrozenCount（うち S＞b で止まった数＝壁の一歩の後の凍結）／wallPassAxes（壁を通った一歩の軸名・なければ -）。
//   ★(409) 末尾に 2 列：wallPassCostShareOfAvailMax（前の行からこの行までに壁の分岐で通った一歩の cost÷avail の最大〔avail＝正統性−請求済み〕・通った一歩が無ければ 0）／wallLegitBlockedCount（満タン〔S≦0〕で代価＞b なのに正統性の側の式で止まった**試みの数**〔MandateGate と Move の呼びのたびに数える＝止まった同じ法を毎四半期に試し直せば重ねて数える＝別々の一歩の数ではない〕。旗に依らず数えるが、**「正統性の側で止まった」と読めるのは旗 MandateOverStockFromLegitimacy＝1 の世界だけ**〔旗 0 では在庫だけで止まった試みも数える〕）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／wallPass* は壁の分岐だけ（普通の一歩は数えない）／mandateSpentNewIndex は Politics.Step の後なので通常 0（読み手は mandateStockIndex を使う）／法の段の整数は enum の序数。
public static partial class Instruments
{
    internal const string ReformHeaderText = "quarter\tyear\tnation\tlegitimacyShare\tmandateStockIndex\tmandateMaxIndex\tmandateSpentNewIndex\tsupportShare\twallPassCount\twallPassLawCount\twallPassSliderCount\twallPassCostIndex\t"
        + "educationLawCode\teducationSpendingShare\tparishFundingShare\teducationBuyPerMonth\tliteracyShare\tsuffrageLawCode\tcensorshipLawCode\tlandReformLawCode\tbureaucracyLawCode\tserfLawCode\tpolityCode\t"
        + "militaryPushIndex\tcoopParliamentCrownIndex\tblockedLawCount\tblockedSliderCount\tblockedWhileFrozenCount\twallPassAxes\twallPassCostShareOfAvailMax\twallLegitBlockedCount";

    /// <summary>壁の分岐で通った一歩（<see cref="Government.PoliticalCapitalAffords"/> の第 2 枝）を数える。呼び手は払う前（Spend の前）に呼ぶ＝S は払う前の値。</summary>
    internal static void WallPass(World w, double cost, bool law, PolicyAxis a)
    {
        if (!(cost > w.PoliticalCapitalMax) || w.MandateSpent + cost <= w.PoliticalCapitalMax) return;
        var st = S(w);
        if (law) st.RfWallLaw++; else st.RfWallSlider++;
        st.RfWallCost += cost;
        st.RfWallAxes += (st.RfWallAxes.Length > 0 ? "," : "") + a;
        double avail = w.Legitimacy - w.MandateSpentNew;   // (409) 払う前の avail に対する代価の割合（払える量の何割まで使ったか）
        if (avail > 0 && cost / avail > st.RfWallShareMax) st.RfWallShareMax = cost / avail;
    }

    /// <summary>資本の門（PoliticalCapitalAffords）で止まった一歩を数える（旗に依らず数える＝A の表が旧の世界の止まりの表になる）。S＞b（壁の一歩の後の凍結）で止まった数を別に持つ。呼び手は Instruments.On の中。</summary>
    internal static void RfBlocked(World w, bool law, double cost)
    {
        var st = S(w);
        if (law) st.RfBlkLaw++; else st.RfBlkSlider++;
        if (w.MandateSpent > w.PoliticalCapitalMax) st.RfBlkFrozen++;
        if (w.MandateSpent <= 0 && cost > w.PoliticalCapitalMax) st.RfBlkLegit++;   // (409) 満タンで代価＞b なのに正統性の側の式で止まった一歩（形が噛んだ回数）
    }

    static void ReformFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;   // 書かない四半期は積み上げたまま（次に書く行が前の行からの和を持つ）
        double cum = 0, fundNum = 0, popSum = 0, litNum = 0, litDen = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            cum += r.CumEducationCost;
            double pop = r.SumPopulation();
            if (pop > 0) { fundNum += pop * r.CompulsoryEduFunding; popSum += pop; }
            foreach (var p in r.Pops) { if (p.IsSoldier) continue; litNum += p.Size * p.Literacy; litDen += p.Size; }
        }
        int since = st.RfPrevQ < 0 ? 1 : Math.Max(1, q - st.RfPrevQ);   // 前の行からの四半期数（挙げた国は 1・他は 4）
        double buy = double.IsNaN(st.RfCumEduPrev) ? 0 : (cum - st.RfCumEduPrev) / (3.0 * since);
        int wallLaw = st.RfWallLaw, wallSlider = st.RfWallSlider, blkLaw = st.RfBlkLaw, blkSlider = st.RfBlkSlider, blkFrozen = st.RfBlkFrozen, blkLegit = st.RfBlkLegit; double wallShareMax = st.RfWallShareMax; double wallCost = st.RfWallCost; string wallAxes = st.RfWallAxes.Length > 0 ? st.RfWallAxes : "-";
        st.RfCumEduPrev = cum; st.RfPrevQ = q; st.RfWallLaw = 0; st.RfWallSlider = 0; st.RfWallCost = 0; st.RfBlkLaw = 0; st.RfBlkSlider = 0; st.RfBlkFrozen = 0; st.RfBlkLegit = 0; st.RfWallShareMax = 0; st.RfWallAxes = "";
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
            F(PoliticalQuantities.MilitaryPush(w)), F(w.CoopParliamentCrown), I(blkLaw), I(blkSlider), I(blkFrozen), wallAxes, F(wallShareMax), I(blkLegit)));
    }
}
