# PriceOfBread.SimCore/Habit.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-12 W 直後 `public readonly record struct HabitProfile(bool Forms, double Growth, double Decay, double`
````
/// <summary>★(342) 段2：財の性質〈習慣性（依存）〉の1行＝習慣の強さ・減衰・上限・害。<see cref="Habit.Of"/> が財ごとに返す。
/// 習慣性の無い財は全て 0 の既定値（<see cref="Forms"/>＝偽・どの機構も回らない）。値の出所は今は係数のアヘンの行
/// （<see cref="Coefficients.OpiumAddictionGrowth"/> ほか）だけ＝2財目を足すときはここに行を足す（段4）。</summary>
/// <param name="Forms">この財が習慣性の行を持つか（財の番号で決まる・<see cref="Growth"/> が 0 でも真）。需要の上乗せと所得弾力の除外と中毒度の更新（減衰）が読む。</param>
/// <param name="Growth">習慣の強さ＝1人あたり消費が依存を育てる率。<b>0 でこの財の機構の全体が止まる</b>（主フラグ＝労働への害・困窮ゲート・禁令の機構が読む）。</param>
/// <param name="Decay">依存の自然減。</param>
/// <param name="Cap">依存の上限（<see cref="Coefficients.OpiumAddictionCap"/>＝形 H では依存者 1 人の上乗せの量 d・旧の線形の式では蓋）。</param>
/// <param name="LaborPenalty">依存が労働供給を蝕む強さ（成人労働×(1−これ×依存)）。</param>
/// <param name="MiseryScale">素の必要量の困窮ゲート＝基礎の必要量×clamp(これ×(1−補給の充足),0,1)。</param>
/// <param name="Logistic">(820) 形 H：更新をロジスティックにするか（成長の項に (1 − 在庫/上限)＝上限は依存者 1 人の上乗せの量・在庫/上限が依存者の割合・旗 <see cref="Coefficients.OpiumAddictionLogistic"/>）。偽なら今の線形の式。</param>
````

## L15-15 W 直後 `public bool Active => Forms && Growth > 0;`
````
/// <summary>機構が回る＝行を持ち、かつ習慣の強さが正。旧の <c>gi == OpiumGoodId &amp;&amp; OpiumAddictionGrowth &gt; 0</c>。</summary>
````

## L19-21 W 直後 `public static class Habit`
````
/// <summary>★(342) 段2：アヘンの番号で分岐していた依存の機構を、財の性質を読む1本へ移した（恒等＝アヘンの値は今の係数のまま・他の財は 0）。
/// 状態は今の <see cref="Region.OpiumAddiction"/> の1本のまま（州の1つの数）＝<see cref="StockOf"/>・<see cref="LaborFactor"/> は習慣性の財が1つの間はその1本を読む。
/// ⚠ 禁令の側（輸入・輸出の禁止の法・密輸・闇栽培・外圧の緊張）は財の番号のまま＝依存ではなく法（段3 の〈禁制品の法〉で扱う）。</summary>
````

## L24-25 W 直後 `public static HabitProfile Of(Coefficients c, int goodId) =>`
````
/// <summary>財 <paramref name="goodId"/> の習慣性の行。習慣性の無い財は既定値（全て 0）。
/// ⚠★(344) 反証役 op〔v〕：2 財目の行を足す前に、在庫を州×財へ分ける（今の在庫は <see cref="Region.OpiumAddiction"/> の 1 本＝<see cref="StockOf"/> と更新はどの財でもこの 1 本を読み書きする＝足すと黙って共有・上書きする）。番人＝HabitTests の〈行を持つ財は 1 つだけ〉。</summary>
````

## L31-31 W 直後 `public static double StockOf(Region region, in HabitProfile h) => h.Forms ? region.OpiumAd`
````
/// <summary>その州の依存の在庫（その財の行を持たなければ 0）。</summary>
````

## L34-35 W 直後 `public static double NextStock(in HabitProfile h, double stock, double perCapita) =>`
````
/// <summary>1か月の更新後の在庫＝在庫＋習慣の強さ×1人あたり消費−減衰×在庫（0〜上限）。
/// (820) 形 H（<see cref="HabitProfile.Logistic"/>）：成長の項に (1 − 在庫/上限) を掛ける＝新しく依存になるのは依存でない人の側だけ（在庫/上限＝依存者の割合 h・上限＝依存者 1 人の上乗せの量）。上限 d が +∞ なら (1 − 在庫/d) が 1 になり線形とビットで一致（既定の d は有限の <see cref="Coefficients.OpiumAddictionCap"/>）。</summary>
````

## L38 T `: stock + h.Growth * perCapita - h.Decay * stock, 0, h.Cap, 0);`
````
// (739) 群3：NaN は 0
````

## L40-41 W 直後 `public static double LaborFactor(Coefficients c, Region region)`
````
/// <summary>依存が成人の労働供給を蝕む倍率（1＝蝕まない・床 0.5）。旧 <c>OpiumAddictionGrowth &gt; 0 &amp;&amp; 在庫 &gt; 0</c> のときだけ <c>max(0.5, 1−害×在庫)</c>。
/// 州の1つの在庫に掛かる＝習慣性の財が1つの間はその行（<see cref="Coefficients.OpiumGoodId"/>）を読む。</summary>
````

