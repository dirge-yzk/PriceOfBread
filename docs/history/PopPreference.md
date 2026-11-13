# PriceOfBread.SimCore/PopPreference.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-7 W 直後 `internal static class PopPreference`
````
// ---------------------------------------------------------------- (1090) 段 B1：POP の好み π_p(a) の純関数（影・エンジンは呼ばない） ----------------------------------------------------------------
// 設計＝docs/202611/20261112/ の王権の設計 §12-3 の 1〜2（下書き・式は AI の推奨・確証なし）。事前登録 measurements/m1090_20261113_prereg_cp.txt。
// 今の PolicyPreferences.Pref（ブロックの枝）の式の形を残し、ブロックの 0／1 の札を POP の立場の連続量に替える：
//   負担＝その POP の課税の基数の割合÷人口の割合（平均 1 の指数）／受益＝所得のうち賃金の割合（地代でない所得）。関税の露出の項は T3 の前なので 0（事前登録 [0] ⑥）。
// 扱う軸は 6 本（IncomeTax・LandTax・PollTax・SocialSecurity・Tariff・CornLaws）。入力は数の束＝試験の期待値を紙の式から作る。
````

## L10-10 W 直後 `internal const double BurdenScale = 3.0;`
````
/// <summary>PolicyPreferences.cs の BurdenScale・LandTaxRentierThreshold の写し（値の一致は試験が Pref を呼んで逆算して確かめる）。</summary>
````

## L17-17 W 直後 `internal readonly record struct Stance(double Wealth, double Rent, double LandBase, double`
````
/// <summary>POP の立場。*Share＝国の和に対するその POP の割合・Pop＝人口の割合・Benefit＝所得のうち賃金の割合（0〜1）・RentIncome＝所得のうち地代の割合（0〜1）・Old＝60 歳以上の割合。</summary>
````

## L20-20 W 直後 `internal static double Burden(double baseShare, double popShare) => popShare > 0 && baseSh`
````
/// <summary>負担の指数＝基数の割合÷人口の割合（人口の割合が正でなければ 0・否定形）。</summary>
````

## L23-23 W 直後 `internal static double Pi(PolicyAxis a, in Stance s, double rentProtection, double pension`
````
/// <summary>POP の好み（進める向きへの賛否）。rentProtection＝RentProtectionWeightCoef・pensionOldWeight＝PensionOldWeight・stdOld＝標準のピラミッドの 60 歳以上の割合。未対応の軸は 0。</summary>
````

## L32 T `case PolicyAxis.Tariff: return rentProtection * Burden(s.Rent, s.Pop) - s.Benefit;`
````
// 関税の露出の項は 0（T3 の前）
````

## L38-38 W 直後 `internal static int NearestParty(ReadOnlySpan<double> pi, double[][] parties)`
````
/// <summary>空間の投票：好み π（軸ごと）に一番近い党（距離＝Σ_軸 |π|×|π−党の位置|・同じなら小さい番号）。党の位置が空なら −1。</summary>
````

