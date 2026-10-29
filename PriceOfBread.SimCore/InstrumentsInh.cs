namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (650) 相続法の表 inh_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ。
// 目的＝事前登録 measurements/inh_650_20261030_prereg.txt・_prereg2.txt の狙いの数字 ③（『全て』の制定の数と前 20 四半期の崩れ）・③c・④・⑥（歳入の大きさ）を読む。
// 行＝**全ての国**×四半期（人口が 0 の国は出さない・旗 InheritanceLawGate が 0 のときは表そのものを出さない＝旗 0 の腕の出力は 1 バイトも増えない）。
// 列（Code＝法の段の整数・PerMonth＝月あたりの流れ・Share＝0〜1・Index＝無次元）：
//   inheritanceLawCode（現在の段 0〜4）／inheritanceTargetCode（政府の目標の段）／estatePerMonth（直近月に POP から国庫へ移った額＝World.LastEstateDutyPerMonth）／
//   revenuePerMonth・gdpPerMonth（歳入÷GDP を読み手が組む）／suffrageCode（Suffrage の段）／
//   powerTradIndex・powerModernIndex・powerMassIndex（立場の土台の政治力〔Profile.Power〕）／wealthTradShare・wealthModernShare・wealthMassShare（貯蓄のシェア〔Profile.Wealth〕）／
//   savingsPerYearGdpIndex（貯蓄の合計 ÷ 年 GDP）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（Instruments.Quarter の呼び時点）／月の移転は直近月だけ（四半期の 3 か月の合計ではない）／制定の事象は段の変化を読み手が前の四半期との差で拾う（四半期の中の往復は見えない）。
public static partial class Instruments
{
    internal const string InhHeaderText = "quarter\tyear\tnation\tinheritanceLawCode\tinheritanceTargetCode\testatePerMonth\trevenuePerMonth\tgdpPerMonth\tsuffrageCode\t"
        + "powerTradIndex\tpowerModernIndex\tpowerMassIndex\twealthTradShare\twealthModernShare\twealthMassShare\tsavingsPerYearGdpIndex";

    static void InhFold(Sink sk, ulong seed, World w, int q)
    {
        if (!(w.Coef.InheritanceLawGate > 0) || !(w.TotalPopulation > 0)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Inh ??= Open("inh", seed);
        if (!sk.InhHeader) { sk.Inh.WriteLine(InhHeaderText); sk.InhHeader = true; }
        var e = PolicyPreferences.Build(w);
        double sav = 0;
        foreach (var (_, p) in w.AllPopsWithRegion()) sav += System.Math.Max(0, p.Savings.Value);
        double gdpYear = w.GdpPerMonth.Value * 12.0;
        sk.Inh.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), I((int)w.Inheritance), I((int)w.Policy.Inheritance),
            F(w.LastEstateDutyPerMonth), F(w.LastRevenuePerMonth.Value), F(w.GdpPerMonth.Value), I((int)w.Suffrage),
            F(e.Power[0]), F(e.Power[1]), F(e.Power[2]), F(e.Wealth[0]), F(e.Wealth[1]), F(e.Wealth[2]),
            F(gdpYear > 0 ? sav / gdpYear : double.NaN)));
    }
}
