namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (547) 線 rs2 の計器（読むだけ） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsIvf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しい状態を持たない。
// 出力＝ rs2_s<seed>.tsv（国×四半期・挙げた国は毎四半期、他の全ての国は年末）。
//   農奴解放と土地改革への旧支配層（ブロック 0）の反対の強さの〈今の式 StatusScale×(0.5＋W0)〉と〈地代の損の式 StatusScale×(0.5＋次の一段で失う所得の割合)〉を同じ断面で並べる
//   （旗 LordOppositionByRentLoss の 0／1 のどちらの腕でも両方の列が出る＝腕を分けずに W0 の傾きと損の割合の傾きを比べられる）。
// ⚠限界（計器が自分で申告する）：
//   ・四半期の末の断面。法の門が実際に Pref を読む瞬間（四半期の途中）とは違う。W0・地代÷所得は ComputeBuildSums を直に 1 周して求める（窓のメモを使わない＝挙動に触らない）。
//   ・慣習の取り分は首都州の産地の生産者の最小の RentShare（PolicyPreferences.CustomaryRentShare）。賦存の大農園の州は別の値を持つが、ここでは写さない。
//   ・反対の強さは Pref の旧支配層の項だけ。門の純支持（政治力 Power[0]・議会・軍・大衆の項）は含まない。

public static partial class Instruments
{
    internal const string Rs2Header = "quarter\tyear\tnation\tserfLawCode\tlandReformLawCode\tcustomaryRentShare\teffRentShareNow\teffRentShareNextSerf\teffRentShareNextLand\t"
        + "lordRentToIncomeShare\tlordWealthShare\tlordRentShare\tlossSerfShare\tlossLandShare\t"
        + "oppOldIndex\toppSerfNewIndex\toppLandNewIndex\tflagLevel\t"
        + "power0\tpullSerfUsed\tpullSerfOld\tdem0Top";

    static void Rs2Fold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd)) return;
        if (!(w.TotalPopulation > 0)) return;   // 死んだ国は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sums = PolicyPreferences.ComputeBuildSums(w);
        double rentToIncome = PolicyPreferences.RentToIncomeOf(sums.Rent[0], sums.Income[0]);
        double w0 = sums.Tw > 1e-9 ? sums.Wealth[0] / sums.Tw : 0.0;
        double rentShare0 = sums.Tr > 1e-9 ? sums.Rent[0] / sums.Tr : 0.0;
        PolicyPreferences.EffectiveRentShares(w, PolicyAxis.SerfEmancipation, out double nowS, out double nextS);
        PolicyPreferences.EffectiveRentShares(w, PolicyAxis.LandReform, out double nowL, out double nextL);
        double lossS = PolicyPreferences.LordIncomeLoss(w, PolicyAxis.SerfEmancipation, rentToIncome);
        double lossL = PolicyPreferences.LordIncomeLoss(w, PolicyAxis.LandReform, rentToIncome);
        double ss = PolicyPreferences.StatusScale;
        // 追記 3（事前登録 [11]）：挙げた国の行だけ、旧支配層の政治力と〈政治力×反対の強さ〉と旧支配層の要求の上位を出す（年末だけの国は空）。読むだけ。
        string power0 = "", pullUsed = "", pullOld = "", dem0 = "";
        if (picked)
        {
            var prof = PolicyPreferences.Build(w);
            double p0 = prof.Power[0];
            power0 = F(p0);
            pullUsed = F(p0 * -PolicyPreferences.Pref(PolicyAxis.SerfEmancipation, 0, prof, w));
            pullOld = F(p0 * ss * (0.5 + w0));
            dem0 = string.Join(';', FactionSnapshot.ComputeDemands(w, 0, true).ConvertAll(d => d.Axis.ToString() + (d.Increase ? "+" : "-")));
        }
        sk.Rs2 ??= Open("rs2", seed);
        if (!sk.Rs2Header) { sk.Rs2.WriteLine(Rs2Header); sk.Rs2Header = true; }
        sk.Rs2.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I((int)w.SerfEmancipation), I((int)w.LandReform),
            F(PolicyPreferences.CustomaryRentShare(w)), F(nowS), F(nextS), F(nextL),
            F(rentToIncome), F(w0), F(rentShare0), F(lossS), F(lossL),
            F(ss * (0.5 + w0)), F(ss * (0.5 + lossS)), F(ss * (0.5 + lossL)), F(w.Coef.LordOppositionByRentLoss),
            power0, pullUsed, pullOld, dem0));
    }
}
