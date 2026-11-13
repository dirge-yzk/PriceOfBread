# PriceOfBread.SimCore/Improvement.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `internal static class Improvement`
````
/// <summary>★(705) 線 dsc1：発見の連続化（改良度 L・1 の上）の列の決め方（設計書 docs/202610/20261030/20261030_発見の連続化_設計.md §3-1・事前登録 measurements/dsc1_705_20261031_prereg.txt [1]）。
/// 列 k＝生産者の発見の行（≧0）・表に無いレシピは「発見の表の行数＋財の番号」（表に無い名前つきのレシピは財ごとに 1 列を既定のレシピと共有する＝限界として名乗る）。
/// 領分＝技術水準の括弧（<see cref="World.TechProductivity"/>）が掛かっていた生産者と同じ＝工場と産地。労働の財（労働力・農業労働力）と役務（高度労働力を含む）は列を持たない。
/// 農家の手仕事（<see cref="Simulation.HouseholdRecipeName"/>）と職人のレシピも列を持たない（古い技法の自営＝工場の改良を分けない）。純関数（状態を書かない）。</summary>
````

## L9-9 W 直後 `internal const string ArtisanRecipeName = "職人";`
````
/// <summary>職人の手仕事のレシピ名（<c>Simulation</c> の構築と同じ文字列）。</summary>
````

## L12-12 W 直後 `internal static int ColumnCount => Catalog.Discoveries.Length + Catalog.Goods.Length;`
````
/// <summary>列の数＝発見の表の行数＋財の数。</summary>
````

## L15-15 W 直後 `internal static int ColumnOf(Producer p)`
````
/// <summary>生産者の列（−1＝領分の外）。</summary>
````

## L26-26 W 直後 `internal static string ColumnName(int k)`
````
/// <summary>列の表示名（計器だけ）＝発見の行の名前か「財の名前/既定」。</summary>
````

## L35-35 W 直後 `internal static double BracketOf(double techLevel) => 1.0 + System.Math.Min(techLevel, 2.0`
````
/// <summary>技術水準の括弧の値（<see cref="World.TechProductivity"/> の括弧と同じ式・計器と番人の手の値の出所ではない＝読み手の写し）。</summary>
````

