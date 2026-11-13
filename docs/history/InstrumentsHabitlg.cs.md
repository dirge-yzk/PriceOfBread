# InstrumentsHabitlg.cs の注の履歴

由来：1cd66aae5 の InstrumentsHabitlg.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsHabitlg.cs#番号〕だけが残る。

## §1　形=full　元の行=3-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (820) c65op アヘンの依存のロジスティック（形 H）の旧新の表 habitlg_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（旧 InstrumentsHabitcap.cs〔(830) で削除〕の型）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（蓄えは計器の側の ConditionalWeakTable）。
// 目的＝旗 OpiumAddictionLogistic の旧（線形＋蓋）と新（s' = s + G·pc·(1 − s/d) − D·s）を同じ盤面の同じ前の在庫から並べ、実際の次の在庫がどちらに等しいかと、ロジスティックの釣り合いの不変式を数える。
// エンジンの関数（Habit.Of・Habit.NextStock）を呼ばない＝係数 G＝OpiumAddictionGrowth・D＝OpiumAddictionDecay・d＝OpiumAddictionCap から自分で組む（旧の旗 OpiumAddictionCapOff は (830) で焼き込み＝d は常に有限の係数）。
// 実際の次の在庫は計器が自分で読む＝次の月の口の時点の Region.OpiumAddiction と、四半期の末の表の時点の同じ欄（エンジンから受け取るのは 1 人あたり消費 pc だけ）。
// 表 habitlg_s（四半期の末・州の行・前の在庫か pc が正の月があった州だけ）：
//   monthsCount＝口が呼ばれた月数／logisticFlag＝旗の値／pcMeanPerMonth・pcMinPerMonth・pcMaxPerMonth＝その四半期の pc の平均・最小・最大／
//   stockLastIndex＝四半期の末の実際の在庫／stockMaxIndex＝その四半期の遷移の実際の次の在庫の最大／oldNextLastIndex・logNextLastIndex＝最後の遷移の旧・新の次の在庫（"R" 書式）／
//   transCount＝数えた遷移（月）／oldMismatchCount・logMismatchCount＝実際が旧・新とビットで違った遷移／
//   lgViolMonthsCount＝実際が max(s, s_eq(pc))（率 G·pc/d + D が 1 を超える月は max(s, d)）を相対 1e-9＋絶対 1e-15 を超えて破った遷移（s_eq = G·pc·d/(D·d + G·pc)・否定形で NaN も破り）／lgViolMaxIndex＝破った量の最大／
//   pinMonthsCount＝実際 ≧ 0.995·d／floorMonthsCount＝害×実際 ≧ 0.5（労働の床）／overDMonthsCount＝新の式の挟む前の値が d を超えた遷移（挟みが効いた月）。
// ⚠限界（自己申告）：助走の月は数えない（助走の明けの最初の月は前が無い）。pc は州の売れた量÷人口（POP 以外の買い手も含む＝(793) で 0）。
//   月が飛んだ州（その月の EconomyStep が呼ばれない）は飛んだ前後を 1 つの遷移として数える（稀）。旗 1 の腕では旧の列は〈同じ前の在庫に旧の式を当てた値〉であって旧の軌道ではない。
`````

## §2　形=full　元の行=27-27　次のメンバー=HabitcapR
`````text
    // (830) InstrumentsHabitcap.cs（habitcap_s・habitbal_s）を削除したので、この表が借りていた 2 つの定義をそのまま（名前も変えず）ここへ移した。
`````
