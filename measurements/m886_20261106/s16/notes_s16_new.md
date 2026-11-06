### 2-1. S16 の書き手の数と行の数

| 量 | 書き手（file:line・木 11db605f の grep・計器を除く） | 書き手の数 | その書き手を持つ行 |
|---|---|---|---|
| 運動の結束 FactionState.Cohesion | Factions.cs:65・:84・:99・:125・:127・Politics.cs:864 | 6 | S16-20・S16-22・S16-21・S16-24（:125・:127）・S16-13 |
| 蜂起の予約 World.PendingRebelRegions | Politics.cs:824・MultiNationSim.cs:294 | 2 | S16-01・S16-02 |
| 蜂起の政体 World.PendingRebelPolity | Politics.cs:825 | 1 | S16-01（同じ関数） |
| 内乱の主体 World.RevoltingFaction | Politics.cs:865・:998・MultiNationSim.cs:316 | 3 | S16-13・S16-29（:998 は TriggerFactionRevolt）・S16-02 |
| 拒否の累計 FactionState.Rejections | Politics.cs:967・:1052・:1060・Factions.cs:126 | 4 | S16-23・S16-30・S16-24 |
| 危機の通知 World.PendingFactionCrisis | Politics.cs:1020・:1041 | 2 | S16-29・S16-30（CrisisThroughLawPath の記録と解除） |
| 一揆の譲歩 World.PendingRevoltDemand | Politics.cs:216・:1412 | 2 | S16-46 |
| 民兵の札 Pop.MilitiaShare | Politics.cs:237・:985 | 2 | S16-38 |
| 変異 FactionState.Mutated | Factions.cs:80 | 1 | S16-22 |
| 運動の生と死 World.Factions の Add／RemoveAt | Factions.cs:60・:100 | 2 | S16-20・S16-21 |
| 蜂起の累計 World.FactionRevolts | Politics.cs:966 | 1 | S16-29 |
| 首都の記し World.SecedingRegion | Diplomacy.cs:2923・MultiNationSim.cs:307・:1223 | 3 | S16-07（読み手）・S16-02（:307）・決着の側は S27 と共有（未分割） |
| 州の内戦の旗 Region.CivilWarActive | Simulation.cs:1685 | 1 | S16-16 |
| 内戦の経験 World.HadCivilWar | Politics.cs:826 | 1 | 読み手は Runner の表示だけ（Program.cs:3938・:5346・:5853）＝力学に読み手 0（表示用のテレメトリ・行にしない） |

- S16 の文書の見出し 39 件（L2797〜2861・L2911〜3055・overview.patch を当てた後の行）→ 門と式の行 48。見出しが打消しの廃止 5 件（L2952 運動の年季・L3013 武力鎮圧・L3018 鎮圧の値段・L3023 体制の転覆・L3039 別の軸で埋め合わせる）は門が無い＝行にしない。見出しは ● のまま本文が〈削除（2026-09-28）〉の 5 件（内戦の進行・反乱側も動員・戦勝点で決める・国家分裂・分裂の断層）も門が無い＝行にせず、文書が古いの表に書いた。

### 2-2. 文書が古い（S16・(886) の差分 overview.patch で docs/ゲーム概要と実装機能.md を直した）

| 文書の行 | 文書の書き方 | 今のソース |
|---|---|---|
| L2823・L2830・L2842・L2849・L2855 | 見出しが ●（稼働中）のまま | 本文のとおり 2026-09-28 に削除＝見出しの記号を打消しにする |
| L2917 | 変数 忘却の四半期(FactionForgetQuarters)=8 | 2026-11-02 (749) で消えた（墓標 Coefficients.cs:2993）＝床 0.05 を割ったら忘れる |
| L2922 | 1 四半期 1 件（連鎖合体の暴走なし） | 1 四半期に運動ごとに 1 回まで＝重ならない組は同じ四半期に何組でも合流する |
| L2973 | 民族の分離／政体の転換／要求の貫徹の 3 つ | 民族の分岐は 2026-09-28（作者裁定 g）で削除＝政体戦と要求戦の 2 つ（民族の分離は CheckSecession の 1 本） |
| L2997 | ○ 休眠・変数 後始末(RevoltAftermathOnWarPath)=0 | 係数は 2026-09-28 に削除（墓標 Coefficients.cs:3013）＝政府の勝ちの後始末は SettleRevolt の 1 本 |
| L3003 | ○ 休眠の見出し（本文は 2026-10-18 削除と書いている） | 見出しの記号を打消しにする |
| L3028 | 強制施行が残るのは旧経路の内戦だけ・戦争の経路には無い | 旧経路は 2026-09-28 に消えた＝今は戦争の経路の決着 SettleRevolt（Politics.cs:846）だけが ConcedeToFaction を呼ぶ |
| 第3部の各項の file:line | 2026-09-26 の座標（例 Politics.cs:858 RaiseRevolt） | 例 RaiseRevolt は Politics.cs:821＝座標は台帳の錨の列で取り直した |

### 2-3. 機構が壊れている候補と、恒等で直せる物（00一覧に足す行の下書き）

| 種類 | 物 | 根拠 | 直す線 |
|---|---|---|---|
| 恒等（区分 I・届かない枝） | Politics.Transition の経路 6〈非暴力の即時の政変〉（Politics.cs:686 の `violent ? 5 : 6` の 6）と、Transition から ExecuteRegimeChange へ violent=false で入る道 | 紙：Politics.cs:644 の早い return の後に残るのは〈奪取〉か〈急進が改革派より強い〉か〈上からの改革かつ下賜の幅〉だけで、前の 2 つは上からの改革でなければ violent＝真、上からの改革なら :685 で return＝violent＝偽のまま :687 に届く道が無い。生 R1 coupgate_s の route 6 は 10 種で 0 行（1〜5・7 は出る）。系統は S15（政体の遷移）＝S16 の隣の関数で見つけた | 恒等の線（区分 I の届かない枝の削除・ExecuteRegimeChange の violent=false の呼び手は他にある＝引数は残す） |
| 恒等（注の直し） | 消えた旗 CivilWarPredicateUnified を〈点火済み・0 へ戻すと…〉と名指す注 3 か所 | Politics.cs:231・:888・Simulation.cs:1685（旗は 2026-10-18 に焼き込みで削除＝Coefficients.cs:2800 の墓標） | 恒等の線（注だけ） |
| 恒等（隠れた定数） | 内戦の軍需の倍率の 2.0（Simulation.cs:2102） | 係数表の外の定数 | 恒等の線（区分 I の〈係数表の外の隠れた const を Coefficients へ〉・値と型を変えない） |
| 挙動の候補（確証なし・記録） | 内戦の軍需の倍率が総動員の深まりを読まず常に 18（対外戦は 9〜13.5） | Simulation.cs:2102・作者 2026-10-16 原文「全てのバフ・デバフ(特に倍率を変更するもの）は全て削除したいです。」の対象の候補 | 挙動の線の候補（軍需の需要の道＝S23 の残りタスク〈武器の続き〉と同じ所＝その線の周辺の点検で読む） |
| 記録 | 州が 1 つの国の蜂起の即時の決着は全国の coerce・点火は州按分（S16-03） | 州が 1 つなら同じ値＝多国の盤面では差が出ない | 直さない |

- 壊れている（狙いの動きをしない）と言える機構は S16 には無かった。〈未確かめ〉の 4 行（S16-28・S16-30・S16-45・S16-46）は計器の列が無い所。

### 2-4. 足すべき計器の仕様（S16 の未確かめを埋める・恒等の線の候補・作者に聞く物ではない）

- 表 revolt_s（仮）：行＝国×四半期（挙げた国）。列＝crisisRaisedCount（CheckFactionRevolt が CrisisThroughLawPath に入った回）・crisisWaitCount（力の門 Politics.cs:937 を通り好機の門 :952 で戻った回）・lawMovedMain（CrisisDemandsMoved＝1）・lawMovedOther（＝2）・lawMovedNone（＝0＝蜂起）・regionalFired（州の一揆の門 :1369 を通った回）・regionalSuppressed（:1374 の鎮圧の枝）・regionalConceded（譲歩の枝）・regionalDemandMet（:216 で緩めた回）・mergeCount（Factions.cs の合流の組の数）・civilOutcome（決着の四半期に 1＝反乱の勝ちで政体戦・2＝反乱の勝ちで要求戦・3＝政府の勝ち・4＝分断・5＝独立の確定）・defectedSoldierK（寝返った守備兵の千人）。
- 書き方：計器の部分クラスを 1 本足し、各枝の直後に Instruments.On のときだけ数を足す（力学の値を書かない・乱数を引かない・[System.NonSerialized] の数だけ）。旗 0 で ident〈恒等〉。持ち主は Politics.cs・Factions.cs・MultiNationSim.cs の各 1 行と新しいファイル。
- これがあれば S16-28・S16-30・S16-45・S16-46 と S16-11 の内訳・S16-24 の件数・S16-39 の量が生で読める。
