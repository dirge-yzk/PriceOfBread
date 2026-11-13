# InstrumentsMerch.cs の注の履歴

由来：1cd66aae5 の InstrumentsMerch.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsMerch.cs#番号〕だけが残る。

## §1　形=full　元の行=4-9　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1007) 商人・職人の空と小営業の入口の錠 A・B の計器 merch_s（読むだけ・恒等・事前登録 measurements/m1007_20261110_prereg_ms.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・エンジンの欄を足さない（置き場は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝全ての州（Tier を問わない）で商人・職人の POP が空かを数え、小営業の入口（Meritocracy.cs の ①*）の錠 B（労働者の元手）と錠 A（空の行き先を飛ばす）を、エンジンの関数を呼ばずに計器が r.Pops から再計算する。cpop_s は植民地格の州だけ（格が変わると打ち切り）。
// 二段：MerchEnter（Simulation.cs の社会移動の区間の前・国の World ごと・助走の外）が州の行を直近の 1 四半期ぶんだけ置く／MerchTable（Quarter）が q を頭に付けて書き出す。
// ⚠限界（計器が自己申告する）：①year は呼んだ時点の World.Year＝その四半期自身の年（Quarter から書く表の q%4==3 の行は翌年のラベルなので年では結ばず (quarter,nation,region) で結ぶ）
//   ②merch_s は社会移動の前・cpop_s は後＝空の商人の POP へ入る道は Meritocracy に無いので空の判定は同じだが、popK の値は同じとは限らない③錠 B の元手は労働者（MeritFromIndex）の生の貯蓄・avgPerCap は max(0,貯蓄) の和÷頭数（エンジンと同じ定義を計器が自分で書く）。
`````

## §2　形=trail　元の行=15-15　次のメンバー=tmerchSavPerCapAtGate
`````text
// (1024) 末尾 7 列＝Meritocracy.Step の中で書く（前の 21 列は社会移動の前・この 7 列は Step の中＝時点が違う）
`````

## §3　形=doc3　元の行=22-24　次のメンバー=MerchStepAcc
`````text
    /// <summary>(1024) K3：州ごとの Meritocracy.Step の中の記録（置き場は ConditionalWeakTable・セーブに載らない）。道の量は実際に移った人数（移す前後の POP の Size の差）。
    /// 道の番号（列の順）：0＝①* 商人へ入った・1＝①* 職人へ入った・2＝③′ 商人→資本家・3＝:345 商人の転落・4＝⑥ 職人の転落。
    /// 門（:345 の転落の資産の門）は Step の中のその位置で計器が自分で計算する（前の 21 列の時点＝社会移動の前とは違う＝⑤）。FallAnchor・FallDepth は private＝係数から自分で書く。</summary>
`````

## §4　形=doc3　元の行=33-33　次のメンバー=MerchRoute
`````text
    /// <summary>(1024) 道の量 1 件（Meritocracy.Step の中・<c>Instruments.On</c> を読んだ呼び手だけが呼ぶ）。<paramref name="moved"/>＝実際に移った人数（正）。</summary>
`````

## §5　形=doc3　元の行=39-40　次のメンバー=MerchGate
`````text
    /// <summary>(1024) :345 の転落の門の位置で、商人の貯蓄÷人数と資産の門（人数＞Eps かつ 1 人あたり資産＜MiddleFallThreshold×アンカー）を計器が自分で計算する（エンジンの関数は呼ばない）。
    /// アンカー＝f≦0 なら州の平均資産（Σmax(0,貯蓄)÷Σ人数・門の時点で再計算）・f＞0 なら (1−f)×平均＋f×自分の年所得÷人数。限界：平均はエンジンが州の頭で測った値、こちらは門の時点の再計算＝移動で貯蓄の和が保たれない分だけずれうる。</summary>
`````

## §6　形=doc3　元の行=54-54　次のメンバー=MerchStepGet
`````text
    /// <summary>(1024) 試験の口：州の記録を読む（無ければ全て 0・NaN の空）。</summary>
`````

## §7　形=trail　元の行=97-97　次のメンバー=stepAcc
`````text
// (1024) K3：この四半期の Step の記録をここで空にする（助走の Step の記録を持ち込まない）
`````
