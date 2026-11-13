# InstrumentsInheritIntent.cs の注の履歴

由来：1cd66aae5 の InstrumentsInheritIntent.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsInheritIntent.cs#番号〕だけが残る。

## §1　形=full　元の行=3-22　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (774) 継いだ着工の意志の表 inheritintent_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHabitlg.cs の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/m774_20261102_prereg_sw.txt。州が別の国へ移る（Territory.TransferRegion＝割譲・分離独立・再併合）とき、移る州の国営工場を建てる意志 StateTarget が新しい国へそのまま移る形の誤りを、
//   同じ盤面で旧（下げる前）と新（Territory.DroppedStateTarget＝同じ純関数・旗 InheritedStateIntentDrop は (817) で焼き込み）を並べて読む。
// 表 inheritintent_s（scope 列で 2 種類の行・全ての国。POB_INSTRUMENTS_NATIONS の絞りは読まない）：
//   E＝移しの事象の行（移る州の生産者のうち StateTarget＞0 のもの・四半期の末に出す）：quarter／year／how（Ceded・Seceded・Returned）／fromNation／toNation（四半期の末のその州の持ち主・見つからなければ空）／toBornQuarter（受け手が計器に初めて現れた四半期＝nation_s の生まれと同じ定義）／
//     region／good／recipe／stateTargetOld（下げる前）／stateTargetNew（DroppedStateTarget＝旗に依らず計算）／builtCount（今の棟数）／stateTargetUsed（規則を当てた後に実際に残った値）／constructionLeftDays（工事中なら残りの日数）／
//     buildCostPerMonthOld（旧の意志が次に着工させる月の工費の見込み＝stateTargetOld＞builtCount かつ工事中でないとき UtilityAgent.NewFacilityOutlayPerMonth〔工賃＋素材〕・それ以外は 0）。
//   Q＝四半期の行（年末 q%4==3・州のある国）：quarter／year／nation／bornQuarter／pendingWillCount（StateTarget＞今の棟数の生産者の数）／pendingWillSum（Σ(StateTarget−今の棟数)）／builtSum（Σ今の棟数）。
//     ＝〈移しの外の州〉で意志が残っているかを数える列（事象の行を出さない形の誤りは Q の行で見える）。
// ⚠限界（計器が自己申告する）：分離独立では移しの時点で国が無い＝toNation は四半期の末の持ち主の名前（同じ四半期に州が再び移ると外れる）／toBornQuarter は計器に現れた最初の四半期／
//   buildCostPerMonthOld は工賃＋素材の月の見込みで、工事の途中の借入や切り下げの費用は含まない／stateTargetOld＝0 の生産者は行にしない／単国の試験（MultiNationSim なし）では出さない／
//   toNation の結合は BaseName（同じ BaseName の後継国は取り違える）。
// ★(781) 直し：送り手の World が同じ四半期のうちに sim.Nations から消えても（再併合・全滅した国の割譲）E の行が出る＝移しを蓄えた World を試合（Diplomacy）の登録簿にも覚え、四半期の末に〈sim.Nations の順の行〉（旧と同じ順）の後ろへ消えた送り手の行を追記する。
//   列を末尾に足した：inProgressAtTransfer（移しの時に ConstructionLeft＞0 なら 1）・stateTargetPrereg（事前登録 m774 の文の規則 min(旧の意志, 棟数＋工事中なら 1)＝エンジンの DroppedStateTarget を呼ばずに独立に書く）。Q の行は空。
// 表 inheritpar_s（移しの事象の行 E と同じ生産者ごと・読むだけ）：継いだ工事の窓（移しから継いだ工事の完成まで）の間に、移った州で別の生産者の国営の着工が起きた回数 parallelStarts と、
//   比べる側 parallelStartsOwnBaseline（同じ窓の間の受け手の別の州で〈受け手自身が始めた工事が工事中〉のときに起きた国営の着工の回数）。分母 windowQuarters・ownExposureRegionQuarters は四半期の末の標本。
//   inProgressAtTransfer＝0 の行は窓を持たない（completedQuarter ほか −1）。完成しないまま試合が終わった行は completedQuarter＝−1（窓は打ち切り）。
// ⚠限界（inheritpar_s）：toNation は移した四半期の末の持ち主・窓の間に州が別の国へ移ると窓は追わない／四半期の末の標本（日ごとではない）／自国の工事は計器が見た FacilityStarted だけ（開幕から継いだ工事・助走の間の着工は自国にならない）／
//   同じ州に継いだ工事が複数あると窓の重なる行どうしが同じ着工を数える。
`````

## §2　形=doc3　元の行=35-35　次のメンバー=InheritReg
`````text
    /// <summary>(781) 移しを蓄えた World の登録簿（試合＝Diplomacy ごと）：送り手が sim.Nations から消えても四半期の末にその行を出すため。</summary>
`````

## §3　形=doc3　元の行=38-38　次のメンバー=InheritPar
`````text
    /// <summary>(781) inheritpar_s の 1 行（移しの事象の生産者ごと）。窓が閉じる（完成）か試合が終わるまで開いている。</summary>
`````

## §4　形=trail　元の行=55-55　次のメンバー=ConsDepart
`````text
// (948) cons_s の出る州の積算を出た国の行へ
`````

## §5　形=trail　元の行=96-96　次のメンバー=prereg
`````text
// (781) 事前登録 m774 の文の規則（工事中の棟は継ぐ＝棟数＋1 まで）。エンジンの DroppedStateTarget を呼ばずに独立に書く
`````

## §6　形=trail　元の行=110-110　次のメンバー=preg
`````text
// (781) sim.Nations から消えた送り手（再併合・全滅した国）の行は、旧の行の後ろへ追記する
`````

## §7　形=doc3　元の行=143-143　次のメンバー=InheritParFlush
`````text
    /// <summary>(781) 四半期の末：移しの行の持ち主と生まれを確かめ、窓の標本（開いたままの窓の数・受け手の別の州の自国の工事中の州の数）を足し、完成した行・窓を持たない行を書いて閉じる。</summary>
`````

## §8　形=doc3　元の行=174-174　次のメンバー=InheritOwnInProgress
`````text
    /// <summary>(781) 州 <paramref name="g"/> に、<paramref name="w"/> 自身が始めた工事が工事中の生産者があるか（<paramref name="except"/> は数えない）。読むだけ。</summary>
`````

## §9　形=doc3　元の行=190-190　次のメンバー=InheritParStarted
`````text
    /// <summary>(781) 国営の棟の着工（<see cref="FacilityStarted"/> の直後・読むだけ）：着工した World を覚え（自国の工事の判定）、開いた窓の数えを進める。</summary>
`````

## §10　形=doc3　元の行=206-206　次のメンバー=InheritParCompleted
`````text
    /// <summary>(781) 国営の棟の完成（<see cref="FacilityCompleted"/> の直後・読むだけ）：継いだ工事の窓を閉じる印（書き出しは次の四半期の末）。</summary>
`````

## §11　形=doc3　元の行=213-213　次のメンバー=InheritIntentClose
`````text
    /// <summary>(781) 試合の終わり（<see cref="Close"/>）：開いたままの窓（完成しないまま試合が終わった行）を書く。</summary>
`````
