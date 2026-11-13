# InstrumentsEquipment.cs の注の履歴

由来：1cd66aae5 の InstrumentsEquipment.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsEquipment.cs#番号〕だけが残る。

## §1　形=full　元の行=3-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (495) レシピごとの設備の表 eq_s・eq_r（読むだけ・恒等） ---------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（蓄えは ConditionalWeakTable＝セーブに載らない）。INS=1 のときだけ出す。
// 目的＝事前登録 measurements/eq495_20261023_prereg.txt（追記 5）の狙い C1（配りが設備になった）・C3a（配りと退場で民間の持分の合計が保たれる）・C3b（台帳の恒等）・D1（向き）・D2（配りが利幅に従う）と後退の確かめ R1〜R3 の読み手 scripts/instruments/eq495/an_eq.py の入力。
// eq_s＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。列（単位は接尾辞）：
//   flagLevel（RecipeEquipment）／pointsBought（四半期の購入が州の工業化度の在庫に足した点の和＝私的・国家・巨大建造物。配りの入力 g）／equipInstalled（そのうち設備に足した Scale の和）／
//   scaleSum（適格な個体の Scale の和＝全ての活きている州）／builtSum（同・BuiltScale の和）／scaleMax・scaleMaxGood（最大の個体と財の番号）／eligibleCount／gateOpenCount（門が開いた適格な個体）／bankruptCount（倒産中の適格な個体）／
//   indMean（MeanIndustrialization）／indMax（州の TotalIndustrialization の最大）／
//   台帳（その四半期に書き手の側で足した量・状態の和とは出所が別）：completedScale（国営の完成が足した Scale）／dismantledScale（恐慌の廃棄が引いた Scale）／distNetScale（配りが変えた Scale の純増＝配った量＋薄めの変化）／retiredNetScale（退場が変えた Scale の純増＝戻した量−消えた量）／
//   distDriftMax（配りと退場の 1 回ごとの〈民間の持分の合計の達成値と目標の相対差〉の四半期の最大＝C3a）／renormShort（薄めきれなかった回数）／activeRegionCount（活きている州の数）／svcLaborShare（工員の取り合いの重み LaborWeight×Scale に占める役務の割合＝記録）。
// eq_r＝挙げた国だけ×レシピ×四半期（発見の行を持つレシピのある財の、門が開いた州があるか Scale が正のレシピ）。列：good・recipe・scaleSum（活きている州の和）・shareOfGood（同じ財の適格なレシピの Scale の和に対する割合）・builtSum・gateOpenRegions・marginSum（門が開いて倒産でない個体の max(0, SmoothedMargin) の和＝配りの重みの元）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／pointsBought と equipInstalled は INS=1 の月の合計（配りが走らなかった月は 0）／レシピの名前が空の個体（既定の 1 本目）は recipe＝"(既定)"。
`````
