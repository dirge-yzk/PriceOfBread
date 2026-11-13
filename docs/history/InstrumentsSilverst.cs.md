# InstrumentsSilverst.cs の注の履歴

由来：1cd66aae5 の InstrumentsSilverst.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsSilverst.cs#番号〕だけが残る。

## §1　形=full　元の行=5-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (823) 銀の平価の注入の州の表 silverst_s と silverwt_s へ足す反実仮想の列（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝InstrumentsSilverWeight.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。INS=1（POB_INSTRUMENTS）のときだけ動く。
// 目的＝(816) の候補 A′（薄い側の額＝参照価格建て）・A（局所の値段建て）で、旗 0 の盤面の注入を数え直し、入った州・出た州・票の上位の州を見る（挙動は変えない）。
// 取り方（事前登録 [4] の文から独立＝エンジンの関数も局所変数も受け取らない）：
//   ①<see cref="SilverstEnd"/>：注入の直後（助走の注入も含む）に呼ばれ、計器が sim.Nations × World.Regions を**自分で**走査して r.Markets の銀と金鉱を読む。
//      走査に入る州の条件（両方の市場が生きて価格が正）は計器が別に書く。エンジンの票は受け取らない（エンジンの値との突き合わせは silverwt_s の stateCountDiff・w1SumDiff・relW1ReadDiff）。
//   ②<see cref="SilverMintCapture"/>：MintGold が州の市場から引き取るたびに（州・金属・量）を渡す。量は計器の ConditionalWeakTable に溜める（Region に欄を足さない）。注入の直後に 0 へ戻す。
//   ③<see cref="SilverstClose"/>：試合の終わりに溜めた行を書く（SilverWeightClose の頭から呼ぶ）。
// ⚠限界（計器が自己申告する）：A・A′・max 版は計器の定義（反実仮想）でエンジンの規則ではない／dAu（LastDemand）は月の流れ・mintQty は前の注入からの和（単位が違う＝比べない）／
//   L は今の注入でも走査に現れる州だけ（国から消えた州は書かない）／行は試合の終わりに書く＝途中で落ちた試合は行が無い／票の和が 0 のとき rel は nan。
`````

## §2　形=trail　元の行=45-45　次のメンバー=OneAgMax
`````text
// (827) 銀の側の片側（銀の需要＜銀の産出の 1%）の票の割合（等重み・w1・A′・max 版）
`````

## §3　形=doc3　元の行=53-53　次のメンバー=SilverMinWeight
`````text
    /// <summary>★(823) 薄い側の額の票＝2 辺（それぞれ額）の小さい方。非有限・非正の辺は 0（＝片側が無い州は票なし）。純関数（試験が手の値で読む）。</summary>
`````

## §4　形=trail　元の行=79-79　次のメンバー=oneAgW
`````text
// (827) 銀の側の片側
`````

## §5　形=trail　元の行=111-111　次のメンバー=oneSidedAg
`````text
// (827)
`````

## §6　形=trail　元の行=133-133　次のメンバー=OneAgEqual
`````text
// (827)
`````
