# InstrumentsRdh.cs の注の履歴

由来：1cd66aae5 の InstrumentsRdh.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsRdh.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (721) 線 rdh：名簿の外の空洞化の原因の表 rdh_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsPrdf.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは Region を鍵の弱い表）。
// 書くのは環境変数 POB_INSTRUMENTS_RDH が空でないときだけ（INS=1 の中だけ＝Instruments.On）。INS=1 でも RDH が空なら 1 行も書かない（他の表は 1 バイトも変わらない）。
// 行＝全ての国の全ての州（挙げた国・本国の格で絞らない＝バーミンガムがネーデルラント領 int・オムスクがハイデラバード領のときも出る）。
//   kind O＝構築の直後（Phase open）の州の断面（人口・農村・その瞬間の〈移る値打ち〉1 回）／W＝助走（WarmupFrozen の月）の和を助走の直後（Phase warm）に書く／Q＝四半期の和（四半期の末）。
//   駆動の列（months 以下）＝PopDecision.Run の直前に、Run と同じ式（Instruments.UrxPdDrive＝流れと門・PopDecision.MigrationValues＝V と g）で取った月ごとの値の和。
//     gPosMonths／gNegMonths＝g の符号の月（g＝0 は gZeroMonths）・gSum＝Σg（月の平均は ÷months）・vRuralSum／vUrbanSum＝1 人・月の V の和・earnRuralSum／earnUrbanSum＝1 人・月の現金の所得の和（負のまま）・
//     subSum＝1 人・月の自給の値の和・housingSum＝1 人・月の家賃の和・vUrbanClipMonths＝V都市が 0 で切れた月（現金−家賃≦0）・flowToUrbanK／flowToRuralK＝流れ（門の後・千人）の和・
//     serfGateMonths／famineGateMonths＝農奴の門・飢えの門が流れを縛った月・noPairMonths＝農村と都市の対が無い月。
//   人口の列（birthsK・deathsNormalK・deathsFamineK）＝DemographicStep の POP のループの hook（DemoPop・DemoFamine と同じ値）を州へ足した和。兵士の POP は出生と死の外（本体と同じ）。
// ⚠限界（計器が自己申告する）：①O の行の V は構築の直後の欄（前の月の所得・自給の値は 0 のまま）で、助走の最初の月の V とは違う ②流れは Run の直前の盤面の式＝同じ月の後の書き手（社会移動・徴兵）の移しは入らない
//   ③疫病・戦死・鎮圧は epi_s ほか別の表（ここの人口の列に入らない）④州の格・持ち主は行を書いた瞬間の値（四半期の途中の割譲は四半期の末の持ち主へ寄せる）。
`````
