# InstrumentsAnchor.cs の注の履歴

由来：1cd66aae5 の InstrumentsAnchor.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsAnchor.cs#番号〕だけが残る。

## §1　形=full　元の行=3-8　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (369) 扶養力の錨の表 anchor_s（読むだけ・事前登録 measurements/an_20261016_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（衝突面を消す）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない
// （Pop／World／Region に欄なし）。国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。
//   anchorOld ＝ 旧の錨（pop − PopulationDelta）／anchorRegions ＝ 今その国が持つ州の AnchorPopulation の和／anchorUsed ＝ 人口の段が実際に使った錨（World.DiagAnchorBaseline＝人口の段が最後に書いた純記帳・人口の段が飛んだ国は前の値のまま・旗に従う式を計器の中で再計算しない＝何もしない機構が緑にならない）。
//   capacity・capBase・arableSum ＝ 人口の段が最後に書いた診断（World.DiagCapacity／DiagCapacityBaseline／DiagLandAnchor＝Σ耕地）＝人口の段が飛んだ国は前の値のまま。
//   born ＝ 分離独立で生まれた国（BornOfSecession）。regionsN ＝ 州の数（全て）／activeRegionsN ＝ 活きた州／anchorRegionsN ＝ AnchorPopulation が正の州（本国起源の州）。
`````
