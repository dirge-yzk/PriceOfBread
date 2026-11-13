# InstrumentsCpmed.cs の注の履歴

由来：1cd66aae5 の InstrumentsCpmed.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCpmed.cs#番号〕だけが残る。

## §1　形=full　元の行=3-16　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (821) 列強の調停の戦争の講和の表 cpmed_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpeace.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（行の一覧は計器の中だけ）。
// 目的＝旗 ForcedPeaceBlankOverCollapse（調停の戦争は崩壊の上書きを通らず白紙で終わる）の旧新を同じ盤面で読む。旗 0 でも旗 1 でも同じ列を書く。
// 取り方＝講和 1 本ごと（内戦を含む）に Diplomacy.ConcludeWar が ResolvePeace の前（CmBefore）と直後（CmAfter）で呼ぶ。
//   ⚠期待の側（blankFlag・overwriteMatchFlag・repWouldOldIndex・regionsMovedCount）はエンジンの関数を呼ばず、講和の前の入力から計器が式で書く。
//   実際の側＝repMovedIndex（ResolvePeace が移した rep）・ledgerAbsIndex（両国の国庫の台帳 WarCost の前後の差の絶対値の和＝台帳の側）・capturedCount・cededFlag。
// 列：seq／month／year／sideA／sideB／civilFlag／forcedFlag／collapsedIterFlag（その戦争の月の歩で崩壊と判定されたか＝CpIter の控え）／exA／exB（講和の前の厭戦）／scoreIndex（講和の前の未クランプの戦勝点）／
//   blankFlag（|Clamp(戦勝点,−1,1)| ＜ WhitePeaceScore）／overwriteMatchFlag（|戦勝点| が崩壊の上書きの式 1.5×min(1,max(0,2×勝者の押し÷(勝者の押し＋敗者の押し＋1e-12))) と A・B どちらかを敗者と見て一致）／
//   hitFlag（ResolvePeace が賠償の枝まで進んだ）／repMovedIndex／ledgerAbsIndex／capturedCount／cededFlag／regionsMovedCount（A・B の国の州の数の前後の差の絶対値の和）／
//   repWouldOldIndex（調停の戦争で collapsedIterFlag=1 の講和だけ＝旧の規則の反実仮想 ReparationGdpFraction×敗者の GdpPerMonth×sa×敗者の FiscalFundingRatio・sa＝min(1,1.5×reach)・sa が WhitePeaceScore 未満なら 0〔旧でも白紙〕・それ以外の行は 0）。
//   敗者＝崩壊の線（厭戦≧1−RevolutionSurrenderWeight×革命圧力）にいる側・両者なら厭戦が大きい側（同値は押しの累計が小さい側）。
// ⚠限界（計器が自分で申告する）：①collapsedIterFlag はエンジンの判定の控え②repWouldOld の同値の敗者決めは押しの累計（歩の本体は塹壕の後のその月の押し）
//   ③repWouldOld は会議の削り（repCut）と条約港の倍率の前・(791) の 1 回分の取り分の前（旧の規則で同じ敗者の崩壊の戦争と集合になれば実際は取り分だけ）
//   ④ledgerAbsIndex は ResolvePeace の中で動いた WarCost の全て（賠償以外の WarCost の動きがあれば混ざる）⑤国を持たない側（抽象標的）の台帳と州は 0 と数える。
`````
