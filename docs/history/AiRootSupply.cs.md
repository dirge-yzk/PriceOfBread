# PriceOfBread.SimCore/AiRootSupply.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-18 W 直後 `internal sealed class RootSupply`
````
/// <summary>★(595) 線 aic：候補の生産者が、投入の連鎖の根まで作れるか（読むだけ・状態を書かない・乱数を引かない）。
/// <see cref="UtilityAgent.FacilityBuildScore"/> が
/// 〈根まで作れない〉候補を NaN で落とす（(260) の門の NaN と同じ作法）＝AI の着工（<see cref="UtilityAgent"/> の TryBuildFacility）と
/// 全国工場目標の展開（<see cref="World"/> の StepFactoryTargets）が同じ 1 本を読む。
///
/// 規則（1 本・3 つの通り道・新しい倍率なし）：候補の生産者のレシピの非労働・非役務の投入ごとに、その国（active 州の全て）で
///  (a) 今、作られているか輸入されている（州の <see cref="Market.LastProduced"/> ＋ 輸入〔<see cref="Market.LastImport"/> と <see cref="Market.PrevImport"/> の大きい方〕の国の和が
///      〈量として意味を持たない〉の既存の物差し <see cref="Coefficients.TargetStockFloor"/> を超える）か、
///  (b) 作れる（その財を作る生産者が国内にあり、発見の門 <see cref="Discovery.Gate(Region, Producer)"/> が開いていて、その生産者の投入も再帰的に (a) か (b)）
///  (c) 世界のどこかで作られている（<see cref="World.WorldGoodsProduced"/>＝MultiNationSim が毎四半期に注入する全国の産出の和が同じ物差しを超える。自国の輸入は使い手の需要に引かれるので、使い手の無い国では輸入が構造的に 0＝自国の流れだけで読むと立ち上がりを閉じる。未注入・NaN は測れない＝通す）
///  のいずれかであること。どれでもない投入が 1 つでもあれば根が無い＝候補外。
/// (b) を残す理由＝作れるが今は作っていない上流は、連鎖の目（TryBuildFacility の starved）が上流を候補に足して先に建てる＝下流を落とさない。
/// 落とすのは、上流が国内に生産者を持たない（気候・産地の無い財）か門が閉じていて、しかも輸入も無い下流だけ
/// （例＝ゴムの無い国の防水布。値段が安全網の天井に張り付き〈足りない〉と読めるが、建てても産出 0）。
/// 循環（互いが互いの投入）は〈根が無いと証明されていない〉＝通す。再帰の深さは <see cref="MaxDepth"/> まで（超えたら通す）。
/// 1 回の走査（月の判断 1 回）の間だけ生きる作業の表＝セーブに載らない・新しい状態を作らない。</summary>
````

## L21 T `const int MaxDepth = 6;`
````
// 連鎖の遡りの深さの上限（最長の鎖より十分に深い・超えたら通す＝落とす証拠が無い）
````

## L23 T `readonly sbyte[] _st;`
````
// 財ごとの答え：0 未判定・1 作れる/ある・2 根が無い・3 判定中（循環＝通す）
````

## L24 T `readonly sbyte[] _flow;`
````
// 財ごとの流れ：0 未判定・1 ある・2 無い
````

## L25 T `readonly int[] _leaf;`
````
// 計器用：根が無いと判った財ごとの、行き止まりの財（流れも開いた作り手も無い財）
````

## L38-38 W 直後 `internal int Blocker => _lastLeaf;`
````
/// <summary>直近の <see cref="Of"/> が false を返したとき、行き止まりになった財（計器が読む・判定には使わない）。</summary>
````

## L41-41 W 直後 `internal bool Of(Producer pr) { _lastLeaf = -1; return InputsOk(pr, 0); }`
````
/// <summary>候補の生産者が根まで作れるか。</summary>
````

## L49 T `if (g < 0 || g >= _st.Length) continue;`
````
// 範囲外は証拠が無い＝通す
````

## L50 T `if (!(inp.QtyPerOutput > 1e-9)) continue;`
````
// 量が実質 0 の投入は律速ではない（連鎖の目と同じ線）
````

## L51 T `if (Catalog.Goods[g].Tier == GoodTier.Service) continue;`
````
// 役務（労働力の 3 財を含む＝人が供給する）は工場の連鎖の根ではない
````

## L63 T `_st[g] = 3;`
````
// 判定中（循環は通す）
````

## L82 T `if (g >= r.Markets.Length) continue;`
````
// 本番の州は常に稼働中＝判定は恒真（Region.State の読みは許可表の外）
````

## L84 T `sum += m.LastProduced + Math.Max(m.LastImport, m.PrevImport);`
````
// 輸入は直近の清算と 1 つ前の清算の大きい方（月初に LastImport が 0 へ戻り、清算が間引かれる月もある＝Model.cs の PrevImport の注）
````

## L86-86 W 直後 `bool flows = double.IsNaN(sum) || sum > _w.Coef.TargetStockFloor;`
````
// 測れない値（NaN）は落とす証拠にならない＝あるものとして通す（候補を落とすのは、根が無いと言えるときだけ）
````

## L88-90 W 直後 `if (!flows)`
````
// ★(595) 追記 3：自国の輸入は需要に引かれ、需要は動いている工場からしか立たない（EconomyStep の inputNeed）＝自国で使い手がまだ無い投入は輸入が構造的に 0
//   （建てれば需要が立ち輸入が来る旧の立ち上がりを、自国の流れだけで読むと永久に閉じる＝反射的な錠）。世界のどこかで作られていれば根が無いとは言えない＝通す。
//   読み手は World 側の写し（MultiNationSim が四半期に注入・WorldGoodsPrices と同型）。未注入（単一国・注入前）は測れない＝通す。
````

## L107 T `if (p.RecipeName == Simulation.HouseholdRecipeName) continue;`
````
// 農家の手仕事の行は工場ではない（FacilityBuildScore と同じ述語）
````

