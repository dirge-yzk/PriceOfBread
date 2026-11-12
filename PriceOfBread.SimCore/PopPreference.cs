namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1090) 段 B1：POP の好み π_p(a) の純関数（影・エンジンは呼ばない） ----------------------------------------------------------------
// 設計＝docs/202611/20261112/ の王権の設計 §12-3 の 1〜2（下書き・式は AI の推奨・確証なし）。事前登録 measurements/m1090_20261113_prereg_cp.txt。
// 今の PolicyPreferences.Pref（ブロックの枝）の式の形を残し、ブロックの 0／1 の札を POP の立場の連続量に替える：
//   負担＝その POP の課税の基数の割合÷人口の割合（平均 1 の指数）／受益＝所得のうち賃金の割合（地代でない所得）。関税の露出の項は T3 の前なので 0（事前登録 [0] ⑥）。
// 扱う軸は 6 本（IncomeTax・LandTax・PollTax・SocialSecurity・Tariff・CornLaws）。入力は数の束＝試験の期待値を紙の式から作る。
internal static class PopPreference
{
    /// <summary>PolicyPreferences.cs の BurdenScale・LandTaxRentierThreshold の写し（値の一致は試験が Pref を呼んで逆算して確かめる）。</summary>
    internal const double BurdenScale = 3.0;
    internal const double LandTaxRentierThreshold = 0.34;

    internal static readonly PolicyAxis[] Axes =
    [PolicyAxis.IncomeTax, PolicyAxis.LandTax, PolicyAxis.PollTax, PolicyAxis.SocialSecurity, PolicyAxis.Tariff, PolicyAxis.CornLaws];

    /// <summary>POP の立場。*Share＝国の和に対するその POP の割合・Pop＝人口の割合・Benefit＝所得のうち賃金の割合（0〜1）・RentIncome＝所得のうち地代の割合（0〜1）・Old＝60 歳以上の割合。</summary>
    internal readonly record struct Stance(double Wealth, double Rent, double LandBase, double Poll, double Pop, double Benefit, double RentIncome, double Old);

    /// <summary>負担の指数＝基数の割合÷人口の割合（人口の割合が正でなければ 0・否定形）。</summary>
    internal static double Burden(double baseShare, double popShare) => popShare > 0 && baseShare > 0 ? baseShare / popShare : 0.0;

    /// <summary>POP の好み（進める向きへの賛否）。rentProtection＝RentProtectionWeightCoef・pensionOldWeight＝PensionOldWeight・stdOld＝標準のピラミッドの 60 歳以上の割合。未対応の軸は 0。</summary>
    internal static double Pi(PolicyAxis a, in Stance s, double rentProtection, double pensionOldWeight, double stdOld)
    {
        switch (a)
        {
            case PolicyAxis.IncomeTax: return s.Benefit - BurdenScale * Burden(s.Wealth, s.Pop);
            case PolicyAxis.LandTax: { double b = Burden(s.LandBase, s.Pop); return (b < LandTaxRentierThreshold ? 1.0 : 0.0) - (BurdenScale + 1.0) * b; }
            case PolicyAxis.PollTax: { double b = Burden(s.Poll, s.Pop); return (b < 1e-6 ? 1.0 : 0.0) - BurdenScale * b; }
            case PolicyAxis.SocialSecurity: return s.Benefit - 2.0 * Burden(s.Wealth, s.Pop) + pensionOldWeight * (s.Old - stdOld);
            case PolicyAxis.Tariff: return rentProtection * Burden(s.Rent, s.Pop) - s.Benefit;   // 関税の露出の項は 0（T3 の前）
            case PolicyAxis.CornLaws: return rentProtection * Burden(s.Rent, s.Pop) - (1.0 - s.RentIncome);
            default: return 0.0;
        }
    }

    /// <summary>空間の投票：好み π（軸ごと）に一番近い党（距離＝Σ_軸 |π|×|π−党の位置|・同じなら小さい番号）。党の位置が空なら −1。</summary>
    internal static int NearestParty(ReadOnlySpan<double> pi, double[][] parties)
    {
        int best = -1; double bd = double.PositiveInfinity;
        for (int k = 0; k < parties.Length; k++)
        {
            double d = 0;
            for (int a = 0; a < pi.Length && a < parties[k].Length; a++) d += System.Math.Abs(pi[a]) * System.Math.Abs(pi[a] - parties[k][a]);
            if (d < bd) { bd = d; best = k; }
        }
        return best;
    }
}
