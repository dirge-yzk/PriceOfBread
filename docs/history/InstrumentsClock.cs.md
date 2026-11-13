# InstrumentsClock.cs の注の履歴

由来：1cd66aae5 の InstrumentsClock.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsClock.cs#番号〕だけが残る。

## §1　形=full　元の行=3-9　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (461) 発見の時計の表 clock_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsHouse.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（`MultiNationSim` が求める導出量を読むだけ）。
// 目的＝事前登録 measurements/dk_20261020_prereg.txt [5] の予備計測（census）＝時計 S の W〔世界の高度労働力の雇用の合計〕と自国の項 C の Am の規則と、狙い T3・T7 を読む。旗 DiscoveryClockWorldPerMonth に関わらず出す（旗 0 の世界で予備計測を取る）。
// 行（kind 列）：W＝8四半期ごとの世界の合計 W（全ての財の和・行は `*`）／T＝同じ標本の全ての非制度・非思想の行の国別の A（ownNation は `*`）／E＝国が行を解禁した四半期のその国の A_n と W／C＝世界の合計の巡回の累計（worldTotalPerMonth 列＝秒・holders 列＝訪れた生産者）。
//   列：kind／quarter／year／row／decade／worldTotalPerMonth（W・月あたり。C 行は秒）／holders（A＞0 の国の数。C 行は訪れた生産者）／ownNation／ownPerMonth（E のみ）／top1Nation／top1PerMonth／britainPerMonth／francePerMonth／prussiaPerMonth／chinaPerMonth（T のみ）。
// ⚠限界（計器が自己申告する）：A は `Score` の代表の生産者（その行の最初の生産者）の投入から親の財を定める＝国によって代表のレシピが違えば A も違う（T の行は全ての国の最初の生産者で 1 本に定める）／
//   四半期の末の断面／国の名前は表示名（建国・改称で別の名前になる）／電動機の A は旗 DiscoveryClockMotorParent が正のときだけ蒸気機関の財で数える（既定 0＝今の規則＝電動機の行の財）。★(466) 合流で dd5eeabb の切り離しに合わせて直した。
`````
