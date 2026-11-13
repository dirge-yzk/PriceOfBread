# PriceOfBread.SimCore/LandSale.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L4-9 W 直後 `internal static class LandSale`
````
/// <summary>★(991) 土地の持ち分 B-1 段 1〜2＝土地税の払えない分の公売（旗 <see cref="Coefficients.LandShareTaxSale"/>・AI の案・確証なし・事前登録 measurements/m991_20261109_prereg_sl.txt）。
/// 作者 2026-11-04「将来的に『土地を手放す』『土地を買う』は実装したい」→〈作者「全て推奨で」で推奨を採った〉。
/// 呼び手＝<see cref="Fiscal.Step"/> の十分の一税の移しの後（走査の中は売り手と不足額 V を集めるだけ＝買い手の払える割合が POP の並びの順に依らない）。
/// 新しい状態は既にある <see cref="Producer.LandShares"/> の欄の値だけ（欄が null の RGO は売る月に [1,0,0,0] を置く＝割り方は null と同じ）。
/// ⚠既知のずれ：値段は POP 水準で畳む（売り手の LandRent は不在地主の送金と今月 rk≤0 の RGO の欠けを含む・輸出の地代を含まない）・純の地代が正でなければ未払いは消える・
/// 州の中は売り手の添字の順に解く（後の売り手の買い手の貯蓄は先の売りで減る）・買い手の居ない RGO の分は払ったのに移らない。</summary>
````

## L12-12 W 直後 `internal static void Resolve(World w, Coefficients c, List<Pop> pops, List<(int Pi, double`
````
/// <summary>集めた（添字＝<paramref name="pops"/> の並び, 不足額 V）を解く。法の門＝農奴解放が〈なし〉でない国だけ。</summary>
````

## L25 T `if (i < 0 || !ReferenceEquals(r.Pops[i], pops[owed[oi].Pi])) continue;`
````
// 並びが合わなければ解かない（構造で起きない）
````

## L34-34 W 直後 `static int SlotPop(Region r, Producer s, int k)`
````
/// <summary>枠 <paramref name="k"/> の生の受け手（0 持ち主・1 聖職・2 担い手・3 投資家）。州の POP の範囲の外（寄せ）は −1。</summary>
````

## L41-41 W 直後 `static double SellOne(World w, Coefficients c, Region r, int i, double v, double tau, doub`
````
/// <summary>売り手 <paramref name="i"/> の 1 回の公売。戻り値＝買い手から国庫へ実際に移った額の和。</summary>
````

## L63 T `double price = Calendar.PerMonthToPerYear(net) / rate;`
````
// 持ち分の全体の値段＝純の地代の年額÷国債金利
````

## L65 T `if (phi > 1) phi = 1;`
````
// 定義域：持っている分より多く売れない
````

## L66 T `if (phi > sumS / price) phi = sumS / price;`
````
// 定義域：買い手の貯蓄より多く払えない
````

## L76 T `double moved = World.Transfer((w, [pj]), (w, null, null), amt, TreasuryCat.OtherIn);`
````
// 買い手→国庫（公売の代金・走査の税収には足さない）
````

## L84 T `if (!(eSum > 0)) continue;`
````
// この RGO に枠を持つ買い手がいない＝動かさない
````

## L85 T `if (s.LandShares is null) s.LandShares = [1.0, 0.0, 0.0, 0.0];`
````
// null＝今の持ち主に 1.0 と同じ割り方の配列
````

## L107-107 W 直後 `static int TargetSlot(Region r, Producer s, int j)`
````
/// <summary>RGO <paramref name="s"/> で POP <paramref name="j"/> が生の受け手の最初の枠（k＜4）。無ければ −1。</summary>
````

