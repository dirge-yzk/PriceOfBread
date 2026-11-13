# InstrumentsAdmin.cs の注の履歴

由来：1cd66aae5 の InstrumentsAdmin.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsAdmin.cs#番号〕だけが残る。

## §1　形=full　元の行=3-14　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (408) 行政費の返りの表 admrule_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsRule.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/ar_20261018_prereg.txt の記録の線 R1〜R3（国庫に届く割合の旧と新・役務が届かない国家区分・法の浸透の旧と新）を読む。取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。（旗 AdminServesStateRule は (419) で焼き込み〔常に 1〕。旧の式の列〔deliveryOld・stateReachOld〕は旧との差を読む比較の物差しとして残す。）
// 列（単位は接尾辞：Share＝0〜1・Count＝件数。重みは活きた州の人口）：
//   adminFillShare（人口加重の Region.AdminFill）／ruleStateShare・ruleLocalShare（人口加重の Rule[0]・Rule[1]。区分が無い州は 1・0）／
//   deliveryOldShare（旧の式 R0＋R1×d の人口加重・比較の物差し）／deliveryShare（実際の Politics.RuleDelivery の人口加重）／unservedStateShare（人口加重の R0×(1−AdminFill)）／
//   stateReachOldShare（人口加重の Politics.RuleReach・比較の物差し）／stateReachShare（実際の Politics.StateReach）／provincesCount（活きた州の数）。
//   ★追記2（2026-10-18）で足した列（既存の列の後ろ・K＝千人）：ruleCutRateAllShare・ruleCutRateLordedShare（人口加重の仲介者の取り分の率・後者は領主が居る州だけ＝差が納税者の手元に残る減税）／lordlessPopShare（領主の居ない州の人口割合）／
//   manLimitK・manLimitOldK（人口の側の定員 MaxSoldierFraction×兵役の母数の実際と旧の届き方の値）／seatsK（EstablishmentOf の定員）／popSideDecidesFlag（定員が人口の側で決まるか）／
//   soldierK・excessSoldierK（兵士の数と、平時の復員の対象〔Meritocracy ⑦ と同じ州按分で席を超えた分〕）／atWarFlag／nobleIncomeSumPerMonth・nobleIncomeMaxPerCapPerMonth（領主〔貴族 POP〕の前月の所得の和と一人あたりの最大）。
// ⚠限界（計器が自己申告する）：四半期の末の断面（税の月ごとの値は読まない）／人口加重（徴税の重みは所得だが州の中の所得の分布は読まない）／国の平均では州ごとの偏り（植民地の充足 0 など）が隠れる。
`````

## §2　形=full　元の行=46-46　次のメンバー=mNow
`````text
            // 徴兵の母数（RegionManpower＝RuleDelivery を共有）：実際の値と、旧の届き方（R0＋R1×d）に置き換えた値（旧＝実際÷実際の届け率×旧の届け率・届け率 0 の州は母数も 0）
`````
