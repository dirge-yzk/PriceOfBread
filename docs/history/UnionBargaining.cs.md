# PriceOfBread.SimCore/UnionBargaining.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-19 W 直後 `internal static class UnionBargaining`
````
/// <summary>
/// ★(520) 交渉の取り分（設計 cp3 §4-8-4・事前登録 measurements/sb520_20261024_prereg.txt [2] と 追記 3 ⑴）。旗 <see cref="Coefficients.UnionBargaining"/> が正のとき、EconomyStep の ④（所得の分け方）で、
/// 工場の付加価値のうち〈同じ州の工場の 1 人あたりの付加価値の平均を超える分〉の β 倍を、持ち主の取り分から労働者の POP へ移す。合計は付加価値のまま＝保存則は変わらない。
/// <code>
///   β_i ＝ [法≠禁止] × κ_r × (1 − U_i) × S_i
///   S_i ＝ 1 − (1 − S票_i) × (1 − m_i)         〈票で押す道〉と〈罷業で押す道〉は別の道＝どちらかが通れば力が出る（積の合成）
///   S票_i ＝ V_w,i ÷ (V_w,i + V_o,i)         V_w,i ＝ FranchiseVoice(担い手 POP)・V_o,i ＝ FranchiseVoice(持ち主 POP)（同じ物差し＝両側が同じ割合で倍になっても S票 は変わらない）
///   m_i ＝ 容認のとき 担い手 POP の闘争度（0〜1）・統制は 0
///   κ_r ＝ Σ{運動体 f: f.Region＝r, f.Bloc＝2}(f.Size×f.Cohesion) ÷ Σ{POP p∈r: BlocOf(p)＝2} p.Size   （大衆の束のうち結束した運動体に組織されている割合）
///   U_i ＝ 担い手 POP の失業率（前の月の値）
///   u_i ＝ min(rent_i , β_i × max(0, a_i − ā_r) × 頭数_i)     a_i ＝ 付加価値÷頭数・ā_r ＝ Σ付加価値÷Σ頭数（適格な工場の頭数加重）
/// </code>
/// 段ごとの固定の値は置かない（作者「Q3:固定倍率はよくない。労働者の権利など政治的な要素から発言力を計算するべき。適切な値を考えてほしい」）。
/// 新しい状態は無い（β は月ごとに既にある量から導く）。声は唯一の定義 <see cref="InterestGroups.FranchiseVoice"/> を呼ぶ。
/// ⚠限界（名乗る）：κ は運動体の束ね（州×束×思想）の値で、農民・宗教の運動も入り分母に農村の大衆と兵士が入る＝工場の担い手の組織の割合より薄まる向きにずれる（追記 3 ⑷）。
/// 旗が 0 の経路は一切通らない（EconomyStep の呼び手が <c>ubOn</c> で門をかける）。AI の設計＝確証なし。
/// </summary>
````

## L24-24 W 直後 `internal struct Ctx`
````
/// <summary>州ごとに月 1 回の文脈（κ・ā・罷業の力の有無）と、直前の（担い手, 持ち主）の組の β の控え（同じ州の工場はほぼ同じ組＝声の関数を工場ごとに呼ばない）。</summary>
````

## L31 T `public double MassN, UrbanMass, FacScan;`
````
// 計器の記録（κ の分母の POP 数・そのうち都市・運動体の走査の件数）。INS=1 のときだけ埋める
````

## L32 T `public bool Any;`
````
// 適格な工場が 1 つでもあるか
````

## L35-35 W 直後 `internal readonly record struct Outcome(bool Eligible, double Transfer, double Beta, doubl`
````
/// <summary>1 つの工場の月の結果（計器と番人が読む）。</summary>
````

## L38-38 W 直後 `internal static double Kappa(World w, Region r, bool ins = false) => Kappa(w, r, ins, out `
````
/// <summary>組織された割合 κ。運動体（<see cref="World.Factions"/>）が無ければ 0。<paramref name="ins"/>＝計器（読むだけ）が分母の内訳と走査の件数も欲しいとき（κ の値は変わらない）。</summary>
````

## L55-55 W 直後 `internal static bool IsEligible(Producer s, byte sleep, double va)`
````
/// <summary>適格な工場＝眠っておらず・労働を買う非 RGO で・付加価値が正で・頭数がある（否定形＝NaN は適格でない）。</summary>
````

## L59-59 W 直後 `internal static Ctx Prepare(World w, Region r, int np, byte[] sleep, double[] produced, in`
````
/// <summary>州の月の前走査：κ と ā を求める。付加価値の式は EconomyStep の ④ と同じ（売上の取り分−投入費）。</summary>
````

## L80-80 W 直後 `internal static Outcome Transfer(ref Ctx c, World w, Region r, Producer s, byte sleep, dou`
````
/// <summary>工場 1 つの上乗せ。<paramref name="rent"/>＝上乗せを引く前の持ち主の取り分（守り＝上乗せは取り分を超えない）。不適格なら Transfer＝0。</summary>
````

## L92 T `double m = c.Strike && mil > 0 ? (mil < 1.0 ? mil : 1.0) : 0.0;`
````
// 否定形（NaN は 0）・容認だけ
````

## L98 T `c.LastBetaNoStrike = BetaOf(c.Kappa, u, sVote);`
````
// 罷業の力を除いた場合（計器の U5＝統制と容認の差）
````

## L107 T `if (!(t <= rent)) { t = rent; guarded = true; }`
````
// 保存の守り（賃金が付加価値を超えない）＝β の式の下では通常は効かない
````

## L112-112 W 直後 `internal static double BetaOf(double kappa, double unemployment, double s)`
````
/// <summary>β＝κ×(1−U)×S。否定形で書く（NaN は 0）。</summary>
````

