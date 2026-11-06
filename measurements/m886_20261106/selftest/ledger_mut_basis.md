# 機能の台帳（チェックシート）

〈改修の度の点検〉と〈一括の棚卸し〉を 1 つの表でつなぐ生きた台帳。作者 2026-11-06（原文「最終的には、全ての機能が正しく機能しているかをチェックする必要がある」「二度同じ機能をチェックするのはバカらしい」・つなぐ形は作者「判断して」で推奨を採った）。運用は docs/README.md ★(885)。骨は (886) c75lg（木 11db605f）。

## 0. 読み方と運用

- 行の粒＝〈1 つの門か式＋狙いの動き〉。**門ごとに割ってあるのは確かめた系統だけ**（今は S16）。他の系統の行は文書の見出し 1 件＝1 行の仮の行（期待する動きの列に〈粒＝見出し・未分割〉）＝その系統を確かめる線が門ごとに割る。1 本の読む線で 7151 行を直さないための範囲の切り方。
- 文書（docs/ゲーム概要と実装機能.md）は同じ機構を複数の分野に載せる。同じ題の見出しは 1 行に畳み、出典の列に他の行番号を書いた。
- 列：行・系統・機能・期待する動き（文書の出典の行）・錨（file:line:名＝木で引ける宣言か式の行）・持ち主のファイル（錨の係数を読むファイルと文書が名指すファイル・Coefficients.cs と計器は除く）・判定の型・最後に確かめたコミット・判定・根拠・残りタスク。
- 判定の型＝届かない門／読まれない欄／天井の錠／片側だけの門／両側の門（両側が生で出るか）／効き。判定＝未確かめ／働く／一部だけ働く／働かない／文書が古い／この波の線が埋める。
- **9-26 の棚卸し（docs/202609/20260926/20260926_機能の棚卸し.md）の判定は写さない**（規則違反の物差し）。文書の見出しの記号（●○◐◇＝9-26 の判定）は根拠の列に〈写さない〉と名乗って置くだけ＝判定ではない。
- 錨の取り方：文書の係数名（変数の括弧）を木の SimCore で引き、宣言の行を取る（Coefficients.cs が先）。係数名の無い見出しは文書のバッククォートの識別子を引く。どちらも引けない見出しは錨が〈—〉（その系統を確かめる線が取る）。文書の古い file:line は写さない（9-26 の座標で、24 本のファイルは木に無い）。
- 錨の確かめ：`python -X utf8 scripts/instruments/r886/ledger_check.py <repo> <木> docs/機能の台帳.md`＝各錨の行に名が含まれるか。
- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r886/changed_rows.py <repo か worktree> docs/機能の台帳.md`＝行の〈最後に確かめたコミット〉から、持ち主のファイルが変わった行（`git diff --name-only <コミット>`）と、錨の係数の宣言の行が変わった行（Coefficients.cs の差分にその名がある）だけを出す。最後に確かめたコミットが〈—〉の行は未確かめの数として別に数える。
- 周辺の点検（README ★(885)）は該当の行を門ごとに割って埋め、最後に確かめたコミットを書く。残りタスクの対象外の系統は波ごとに読む線 1 本で 1 系統ずつ。
- **行の参照（L の番号）は overview.patch を当てた後の docs/ゲーム概要と実装機能.md の行**。行の id（Sxx-Hnnn＝系統の中の見出しの連番・S16 は門の連番 S16-nn）は文書の行番号に結ばない。
- 生の名：R1＝D:/PoB_work/keep_885/base_ins（合わせた木 aef07f2a の腕 Z・計器つき 10 種・盤面 m873 と全行一致）。

## 1. 系統の表

〈持ち主のファイル〉は、文書が名指すファイルを最も多く名指す系統へ振った物（8 系統以上に出る拠点のファイル 16 本＝Simulation.cs・World.cs・EconomyStep.cs・MultiNationSim.cs・SelfPlay.cs・Model.cs・Politics.cs・Diplomacy.cs・Fiscal.cs・BilateralMarket.cs・Coefficients.cs・Government.cs・world.json・Catalog.cs・Territory.cs・PolicyPreferences.cs は除いた・同数は全ての系統へ）。コミットの数＝`git log --since=2026-09-26 --format=%h 11db605f -- <持ち主のファイル> | wc -l`。感度の列は拠点のファイルも最も多く名指す系統へ入れた数。

| 系統 | 名 | 文書の行 | 見出し | 持ち主のファイル（主の規則） | コミット（主） | （感度） | 残りタスクの対象（00一覧） | 状態 |
|---|---|---|---|---|---|---|---|---|
| S01 | 財と製法と発見 | 615-723 | 20 | Discovery.cs goods.json | 68 | 180 | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） | 未確かめ |
| S02 | 値段と賃金の形成 | 724-778 | 11 | Calendar.cs MarketFlow.cs | 44 | 44 | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） | 未確かめ |
| S03 | 生産と労働の配分 | 779-899 | 24 | — | 0 | 0 | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） | 未確かめ（土地の行は (891) c75ds） |
| S04 | 需要と消費と自給 | 900-985 | 17 | LaborSupply.cs | 1 | 1 | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） | この波の線が埋める (890) c75ts |
| S05 | 投資と建設 | 986-1129 | 28 | Megaprojects.cs megaprojects.json | 22 | 181 | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） | 未確かめ（地代の行は (891) c75ds） |
| S06 | お金の記帳・物価・GDP・損害 | 1130-1198 | 14 | Crime.cs | 10 | 10 | r1（612 行）・価格水準 aic（616 行 ⓪） | 未確かめ |
| S07 | 州と州の清算 | 1264-1346 | 19 | InternationalMarket.cs | 30 | 30 | 輸入の払い手（612 行 ①） | この波の線が埋める (887) c75ip |
| S08 | 輸送と地理 | 1347-1532 | 38 | Geo.cs Landmask.cs Landmass.cs PathGraph.cs Places.cs SeaGraph.cs Transport.cs barriers.json landmass.json passages.json places.json sea_nodes.json | 52 | 98 | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） | 未確かめ |
| S09 | 関税と通商の法と封鎖 | 1533-1715 | 38 | TradeReachDiag.cs | 1 | 63 | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） | 未確かめ |
| S10 | 人口動態と暮らし | 1775-1939,2257-2328 | 47 | LaborSupply.cs PopClass.cs | 3 | 377 | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） | 未確かめ（自給の行は (890) c75ts） |
| S11 | 教育と識字と技術の伝わり | 1940-2032 | 16 | TechEducation.cs | 16 | 16 | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） | 未確かめ |
| S12 | 社会移動と失業 | 2049-2157 | 22 | Meritocracy.cs | 29 | 29 | r1（612 行）・資本家（616 行 ⑨） | 未確かめ |
| S13 | 移住と都市化と民族 | 2033-2048,2158-2256 | 21 | Crime.cs NationConfig.cs PopDecision.cs | 69 | 237 | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） | 未確かめ |
| S14 | 不満と思想 | 2401-2592 | 37 | Ideology.cs PoliticalQuantities.cs | 46 | 508 | prdd・rd47 R4（612 行） | 未確かめ |
| S15 | 正統性と政体の遷移 | 2593-2796 | 32 | PoliticalQuantities.cs | 19 | 94 | なし | 未確かめ（対象外の候補・次の 1 系統の候補） |
| S16 | 蜂起と内戦と運動 | 2797-2861,2911-3055 | 39 | FactionSnapshot.cs Factions.cs SecessionNames.cs | 23 | 87 | なし | (886) で確かめた |
| S17 | 王権と議会 | 3056-3174,3627-3665,3695-3749 | 39 | CrownParliament.cs PoliticalQuantities.cs PowerAffinity.cs | 46 | 46 | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） | この波の線が埋める (889) c75cp |
| S18 | 選挙と政党と利益集団 | 3230-3399 | 29 | InterestGroups.cs Parliament.cs | 31 | 55 | 地主の政治の導き直し C-1（612 行 ⑧） | 未確かめ（土地の行は (891) c75ds） |
| S19 | 法の成立 | 3400-3626,3666-3694,3750-3801 | 45 | Contraband.cs Policy.cs | 15 | 73 | なし | 未確かめ（対象外の候補） |
| S20 | 税と支出と国庫 | 3865-4002,4371-4395,4501-4557 | 37 | Privileges.cs | 21 | 102 | 土地税の算定の基準 A1（612 行 ⑤⑧） | 未確かめ（土地税の行は (891) c75ds） |
| S21 | 国債と破産 | 4003-4158 | 20 | — | 0 | 0 | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） | 未確かめ |
| S22 | 貨幣と本位と信用 | 4159-4370,4396-4500 | 53 | Bank.cs CapitalMarket.cs Credit.cs Explain.cs NationConfig.cs | 94 | 94 | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） | 未確かめ |
| S23 | 軍備と動員 | 4614-4863 | 45 | ArmyShape.cs Military.cs NationConfig.cs PowerShares.cs Scenario.cs | 106 | 106 | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） | 未確かめ |
| S24 | 開戦と戦争と講和 | 4864-5347 | 86 | Landmass.cs | 1 | 1 | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） | 未確かめ |
| S25 | 外交 | 5398-5872 | 85 | Landmass.cs | 1 | 127 | なし | 未確かめ（対象外の候補） |
| S26 | 植民地の経営 | 5943-6050,6365-6472 | 39 | Colonial.cs Landmass.cs | 39 | 39 | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） | 未確かめ |
| S27 | 支配度と分離独立と領土の移転 | 2862-2910,6051-6364 | 68 | CultureRealms.cs Formations.cs SecessionNames.cs | 8 | 54 | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） | この波の線が埋める (888) c75ce |
| S28 | 環境と災害 | 6513-6708 | 41 | Climate.cs DeterministicRng.cs Geo.cs NationConfig.cs Places.cs megaprojects.json places.json | 63 | 63 | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） | 未確かめ（自給の天候の行は (890) c75ts） |
| S29 | 較正と計測 | 6743-7177 | 80 | Program.cs BudgetBindDiag.cs DeterministicRng.cs HistoricalReference.cs Invariants.cs MechanismProbe.cs SaveGame.cs Scenario.cs TradeReachDiag.cs Validation.cs | 163 | 614 | 9-3（612・616 行） | 未確かめ |

### 1-1. (886) が確かめる 1 系統の選び方

- 規則（(886) の指示）：残りタスクの対象外の系統のうち、2026-09-26 以後に持ち主のファイルが変わったコミットの数が最も多い系統。
- 対象外の判定は 00一覧 §00 の 612 行（波75〜・元の残りタスク r1・cesd 段3・dscd 段2・u12 B′・prdd・rd47 R4・rd48 R-c2・巨大建造物ほか・その後の束・cp3 と発見の連続化・9-3・原爆）と、〈その後の束〉の中身として 616 行（波37〜 の並べ方・⓪〜⑨）を名で引いた（上の表の〈残りタスクの対象〉の列）。9-3 は全ての系統の再較正なので、較正と計測の S29 だけを対象に数えた。(b) のこの波の線の 4 系統（S04・S07・S17・S27）も外した。
- 対象外に残った系統（主の規則のコミット数）：**S16 蜂起と内戦と運動 23**・S15 正統性と政体の遷移 19・S19 法の成立 15・S25 外交 1。→ **S16 を選んだ**（持ち主のファイル＝FactionSnapshot.cs・Factions.cs・SecessionNames.cs）。
- ⚠ 感度（拠点のファイルも入れた数）では順位が変わる：S25 外交 127（Diplomacy.cs が入る）・S15 94・S16 87・S19 73。主の規則は〈その系統だけの持ち主のファイル〉の数で、拠点のファイル（Politics.cs・Diplomacy.cs ほか 16 本）のコミットは全ての系統の変更を数えてしまう＝主の規則で選んだ。次の 1 系統は主の規則で S15、感度で S25（次の波の読む線で決める・〈作者に聞く物〉ではない）。
- 00一覧の古い未着手の行（2026-09-29 (104) の〈英の往復＝首都発の内戦の勝者が国の軍を握る〉・〈反乱の膨張は削除の候補〉）は今の §00 の次の波の行に無い＝対象には数えない（反乱の膨張は 2026-10-18 に削除済み）。

## 2. S16 蜂起と内戦と運動（(886) で確かめた・木 11db605f）

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S16-01 | S16 | 蜂起を地図に立てる入口は1本 | 首都の蜂起と組織運動の蜂起が同じ RaiseRevolt を通り、反乱国の建国を予約する（L2797） | Politics.cs:821:private static bool RaiseRevolt(;Politics.cs:824:w.PendingRebelRegions = [seat];;Politics.cs:664:RaiseRevolt(w, w.Regions[0], attractor);Politics.cs:996:RaiseRevolt(w, best.Region, target) | Politics.cs | 効き | 11db605f | 働く | 生 R1 coupgate_s の route 3（首都の蜂起）84 行・10 種／prdf_s の revoltN 計 110（組織運動の蜂起）／wars_s の civilWarFlag=1 が 177 件。呼び手 2 本（:664・:996）・書き手 PendingRebelRegions は :824 と回収の MultiNationSim.cs:294 の 2 本 | — |
| S16-02 | S16 | 建国の予約の回収と開戦 | 四半期の末に予約を回収し、反乱国を建てて内戦の戦争を宣する（L2797） | MultiNationSim.cs:282:internal void SpawnSecededNations();MultiNationSim.cs:294:pw.PendingRebelRegions = null; | MultiNationSim.cs | 効き | 11db605f | 働く | 生 R1 wars_s の内戦 177 件（予約が戦争になった）。ソース :294 で予約を消し、建てられないときは運動の参照を外す（MultiNationSim.cs:316） | — |
| S16-03 | S16 | 建てられない蜂起は即時に決着 | 単国の世界・州が 1 つの国は RaiseRevolt が偽を返し、その場で勝敗を決める（L2797・L2973） | Politics.cs:823:w.LeavableRegions([seat], capital: true);Politics.cs:1005:bool won = FactionStrength(w, c, best) > coerce + LoyalistMilitia(w, c, best); | Politics.cs | 片側だけの門 | 11db605f | 働く | ソース。点火の門は州按分の保持力（Politics.cs:927 の LocalizedHold）、即時の決着は全国の coerce を読む＝州が 1 つの国では州の人口の割合が 1 なので同じ値。単国の世界だけ差が出る（多国の盤面では届かない） | — |
| S16-04 | S16 | 内戦中かの述語は1本 | 当事者で CivilWarParent の付いた戦争があれば内戦中（親も反乱国も）（L2797） | World.cs:892:public bool InCivilWar | World.cs | 効き | 11db605f | 働く | 生 R1 nation_s の civilWarMonths>0 が 1453 国×四半期・93 国。読み手は 11 本（Politics.cs:234・:663・:889・Simulation.cs:1685・:2102・Parliament.cs:38・Military.cs:25・CrownParliament.cs:239・Diplomacy.cs:1810・PoliticalQuantities.cs:333・SelfPlay.cs:1621）。⚠ 消えた旗 CivilWarPredicateUnified を名指す注が 3 か所残る（Politics.cs:231・:888・Simulation.cs:1685） | — |
| S16-05 | S16 | 首都の蜂起の拮抗の門 | 暴力の転換で動員が保持力×1.8 未満なら首都の蜂起（内戦）・以上なら即時の政変（L2803） | Politics.cs:661:challenge < coerce * c.CivilWarLandslideRatio;Politics.cs:666:Instruments.CoupRoute(w, 3) | Politics.cs | 両側の門 | 11db605f | 働く | 生 R1 coupgate_s の route 3（蜂起）84 行と route 5（暴力の即時の政変）48 行＝門の両側が出る | — |
| S16-06 | S16 | 首都の蜂起の排他 | 内乱が地図の上で進行中か予約中は新しい首都の蜂起を立てない（L2803・L2932） | Politics.cs:663:Instruments.CoupRoute(w, 7) | Politics.cs | 効き | 11db605f | 働く | 生 R1 coupgate_s の route 7 が 5 行・3 種（排他が効いた四半期） | — |
| S16-07 | S16 | 首都が反乱国の手にある間の判定の停止 | 首都が出た内戦の間と首都の蜂起を予約した四半期は遷移・組織運動・州の一揆を判定しない（L2803） | Politics.cs:502:bool capitalHeld;Politics.cs:504:bool capitalRose;Politics.cs:506:CheckFactionRevolt(w, c, coerce);Politics.cs:507:CheckRegionalRevolt(w, c, coerce) | Politics.cs | 効き | 11db605f | 働く | ソース（:503 の Transition・:506・:507 が同じ 2 つの真偽で止まる）。分離独立の判定（CheckSecession）は止めない＝文書どおり | — |
| S16-08 | S16 | 遷都は1本 | 首都の入れ替えと首都の座標を書くのは SetCapital の1本（L2808） | World.cs:1391:internal void SetCapital(Region capital);World.cs:1407:Latitude = cc.Lat | World.cs | 効き | 11db605f | 働く | ソース。首都の座標の書き手は SetCapital（World.cs:1407）と建国時の初期化子（Simulation.cs:118・MultiNationSim.cs:424）だけ。呼び手 4 本（Formations.cs:402・MultiNationSim.cs:1222・Simulation.cs:134・Territory.cs:369） | — |
| S16-09 | S16 | 遷都先の選び方 | 首都を失ったときの遷都先は残る州の最大 GDP（忠誠の兵と武器庫の行き先も同じ）（L2808） | Territory.cs:387:internal static Region? NextCapital;Territory.cs:369:NextCapital(from) is { } cap;MultiNationSim.cs:366:Territory.NextCapital(pw, regs) | Territory.cs MultiNationSim.cs | 効き | 11db605f | 働く | ソース（割譲の遷都 :369 と首都の分離独立の忠誠の兵 :366 が同じ関数） | — |
| S16-10 | S16 | 内戦の国名 | 両陣営を政体の頭書き＋国名で呼ぶ（L2812） | SecessionNames.cs:55:PolityPrefix(Polity p);SecessionNames.cs:71:CivilWarName(;MultiNationSim.cs:417:CivilWarName | SecessionNames.cs | 効き | 11db605f | 働く | ソース（建国の名付け MultiNationSim.cs:417 の 1 本が読む）。表示だけ＝力学に効かない | — |
| S16-11 | S16 | 決着：反乱の勝ち | 目標の政体が今と違えば政体変更の実行部（暴力）・同じなら要求をのむ（L2816） | Politics.cs:845:if (target != w.Polity) ExecuteRegimeChange;Politics.cs:846:ConcedeToFaction(w, c, movement) | Politics.cs | 両側の門 | 11db605f | 働く | 生 R1 cpmed_s の内戦の講和 177（白紙 35・白紙でない 142）。勝ち負けと政体戦／要求戦の内訳の列は計器に無い（要る計器＝下の表） | — |
| S16-12 | S16 | 決着：政府の勝ち | 反乱した州の住民の闘争度を ReformMilitancyRelief 倍にし、断裂の窓を開く（L2816） | Politics.cs:850:p.Militancy *= c.ReformMilitancyRelief;Politics.cs:852:RuptureWindowQuartersOnCrush | Politics.cs | 効き | 11db605f | 働く | ソース（SettleRevolt の else の枝・呼び手は戦争の形の MultiNationSim.cs:1261 と即時の決着の Politics.cs:1009） | — |
| S16-13 | S16 | 運動の燃え尽き | 決着で運動の結束を 0 にし、内乱の主体の参照を外す（L2816） | Politics.cs:864:movement.Cohesion = 0;;Politics.cs:865:w.RevoltingFaction = null | Politics.cs | 効き | 11db605f | 働く | ソース。結束の書き手 6 本のうちの 1（Factions.cs:65・:84・:99・:125・:127・Politics.cs:864） | — |
| S16-14 | S16 | 再併合で分離の時点の格へ戻す | 分離した州が親へ戻るとき、分離の時点の格（植民地格なら植民地格）へ（L2816） | Territory.cs:278:r.TierInParent = r.Tier;;Territory.cs:283:if (r.TierInParent is { } tp) r.Tier = tp; | Territory.cs | 効き | 11db605f | 働く | ソース（建国で写し :278・戻して消す :283・第三国への割譲で消す :274） | — |
| S16-15 | S16 | 内戦の国債は勝者が引き継ぐ | 反乱の勝ちは反乱国の国債を統一後の国へ・政府の勝ちは否認（L2816 の作者裁定 2026-09-29） | MultiNationSim.cs:1262:反乱国の国債を統一後の国が引き継ぐ;MultiNationSim.cs:1157:internal void ReabsorbNations() | MultiNationSim.cs | 効き | 11db605f | 働く | ソース（ReabsorbNations の中の 1 本）。件数の列は計器に無い | — |
| S16-16 | S16 | 内戦の経済麻痺 | 内戦中は全州の実効生産性を ×(1−0.2)（L2836） | EconomyStep.cs:1048:coef.CivilWarEconomyDisruption > 0 && region.CivilWarActive;Simulation.cs:1685:r.CivilWarActive = World.InCivilWar;;Coefficients.cs:2011:CivilWarEconomyDisruption | EconomyStep.cs Simulation.cs | 効き | 11db605f | 働く | ソース＋生 R1 nation_s（内戦の月が出る国×四半期で州の旗が立つ＝:1685 は毎旬の複製） | — |
| S16-17 | S16 | 内戦の軍需の倍率 | 内戦中は軍需の倍率を対外戦争の 2 倍（L2836） | Simulation.cs:2102:World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 | Simulation.cs | 効き | 11db605f | 働く | ソース。⚠ 2.0 は係数表の外の定数（隠れた const）・内戦では総動員の深まり（WarMobilizationDepth×厭戦）を読まず常に 9×2＝18＝対外戦の最大 13.5 より大きい。文書どおりの挙動＝壊れてはいない（作者 2026-10-16 の倍率の削除の方針の対象の候補として記録） | — |
| S16-18 | S16 | 内戦中は選挙を止める・練度は実戦 | 内戦中は平時の選挙を止め、軍の練度は実戦として上がる（L2836） | Parliament.cs:38:w.InCivilWar) return;;Military.cs:25:bool fighting = atWar | Parliament.cs Military.cs | 効き | 11db605f | 働く | ソース（述語 1 本の読み手） | — |
| S16-19 | S16 | 参政権の範囲の算入率（2 本） | 分離と少数派の側は〈移民原住民参政権なら 1・なければ州の平均の中核度〉・POP の側は植民地格なら植民地参政権を読む（L2911） | Politics.cs:1714:RegionAvgSuffrageInclusion(World w, Region r);InterestGroups.cs:79:public static double SuffrageInclusion(;InterestGroups.cs:84:w.ColonialSuffrage switch | Politics.cs InterestGroups.cs | 片側だけの門 | 11db605f | 働く | ソース。文書どおり規則が 2 本（州の平均は格を見ない・POP は植民地参政権を読む）。読み手は分離の側 Politics.cs:293・:1474・計器 InstrumentsSecession.cs:93、POP の側 Fiscal.cs:742・InterestGroups.cs:120・計器 InstrumentsSf.cs:33。コードの注は〈J-009 の裁定待ち〉と名乗る | — |
| S16-20 | S16 | 運動の結束の成長 | 観測が続く運動は 0.05×(0.5＋意識)×(変異なら 2) で 1 へ寄る（L2917） | Factions.cs:64:double growth = c.FactionCohesionGrowth | Factions.cs | 効き | 11db605f | 働く | 文書の記号 ●（9-26・写さない）；消えた係数 FactionForgetQuarters | — |
| S16-21 | S16 | 結束の減衰と忘却 | 観測されない運動の結束は ×(1−0.10)・床 0.05 を割ると忘れる（L2917） | Factions.cs:99:f.Cohesion *= 1.0 - c.FactionCohesionDecay;;Factions.cs:100:if (f.Cohesion < ForgetCohesionFloor) w.Factions.RemoveAt(i);;Coefficients.cs:2993:FactionForgetQuarters | Factions.cs | 効き | 11db605f | 文書が古い | ソース。文書の〈忘却の四半期 FactionForgetQuarters=8〉は 2026-11-02 (749) で消えた（墓標 Coefficients.cs:2993）＝今は結束が床 0.05 を割ったら忘れる。機構はソースどおり働く | — |
| S16-22 | S16 | 変異（千年王国の旗） | 結束 0.3 以上・危機 0.5 超で 0.002×危機の確率で変異し、結束に床 0.5（L2917） | Factions.cs:74:if (crisis > c.FactionMutationCrisisFloor);Factions.cs:78:if (roll < c.FactionMutationRate * crisis);Factions.cs:84:c.MutantCohesionFloor | Factions.cs | 効き | 11db605f | 働く | 生 R1 dcont_s の factionsMutatedCount>0 が 2475 国×四半期・29 国 | — |
| S16-23 | S16 | 拒否の累積が変異の引き金 | 拒否の累計×0.5 を危機と max で合わせる（L2927） | Factions.cs:73:c.FactionRejectionMutation * st.Rejections;Politics.cs:967:best.Rejections += 1; | Factions.cs Politics.cs | 効き | 11db605f | 働く | ソース。拒否の書き手 4 本（Politics.cs:967・:1052〔受け入れで 0〕・:1060・Factions.cs:126）。危機と拒否のどちらで変異したかの列は計器に無い | — |
| S16-24 | S16 | 運動の合流 | 同じ束・同じ思想・同じ変異の状態で 800km 以内なら大きい方が吸収（L2922） | Factions.cs:123:c.FactionMergeRangeKm) continue;;Factions.cs:125:FactionMergeCohesionGain;Factions.cs:126:big.Rejections = System.Math.Max(big.Rejections, small.Rejections); | Factions.cs | 効き | 11db605f | 文書が古い | ソース。文書の〈1 四半期 1 件〉は違う＝1 四半期に運動ごとに 1 回まで（重ならない組は同じ四半期に何組でも合流する・Factions.cs の merged の集合）。合流の件数の列は計器に無い | — |
| S16-25 | S16 | 組織運動の結束の門 | 結束 0.8 未満の運動は危機を立てない（L2932） | Politics.cs:893:if (f.Cohesion < c.FactionCohesionFightThreshold) continue; | Politics.cs | 両側の門 | 11db605f | 働く | 生 R1 dcont_s の facCohGateCount>0 が 70964 国×四半期（門の上）・0 の国×四半期もある（行 149407） | — |
| S16-26 | S16 | 幽霊の運動の封じ | 親の州でなくなった本拠州の運動は選ばない（L2932） | Politics.cs:899:if (!w.Regions.Contains(f.Region)) continue; | Politics.cs | 効き | 11db605f | 働く | ソース（合流 Factions.cs と計器 InstrumentsDiscontent.cs:105 が同じ述語） | — |
| S16-27 | S16 | 力の門 | 運動の強さが州按分の保持力＋郷勇を超えないと危機は立たない（L2932） | Politics.cs:937:if (bestStrength <= hold + militia) | Politics.cs | 両側の門 | 11db605f | 働く | 生 R1 dcont_s の facStrongCount>0（強さが保持力＋郷勇を超えた運動がある）が 296 国×四半期・38 国＝門の両側が出る | — |
| S16-28 | S16 | 好機の門（結束の予算化） | 弱み（正統性 0.5 未満・GDP の落ち込み 0.1 超・内戦を除く外戦）か圧倒（1.5 倍）が無ければ待つ（L2957） | Politics.cs:949:bool weakness = w.Legitimacy < c.FactionStrikeLegitimacy;Politics.cs:952:c.FactionOverwhelmFactor | Politics.cs | 両側の門 | 11db605f | 未確かめ | 通る側は生 R1 で出る（危機の後の蜂起 110）。止めた側の件数（力の門を通ったのに待った国×四半期）の列が計器に無い＝facStrongCount 296 と危機の数を比べられない（要る計器＝下の表） | — |
| S16-29 | S16 | 要求は法の経路（蜂起の枝） | 危機の要求の法が 1 段も動かなければ蜂起（L2938） | Politics.cs:1049:int moved = CrisisDemandsMoved(w, best);;Politics.cs:1068:TriggerFactionRevolt(w, c, best, coerce); | Politics.cs | 効き | 11db605f | 働く | 生 R1 prdf_s の revoltN 計 110・dcont_s の factionRevoltsCum の終値の和 110（30 国）＝一致 | — |
| S16-30 | S16 | 要求は法の経路（受け入れ・別の軸の枝） | 本丸が動けば受け入れ（怨嗟 0）・本丸以外だけ動けば別軸の譲歩（怨嗟＋1）（L2938） | Politics.cs:1052:best.Rejections = 0;;Politics.cs:1060:best.Rejections += 1;;Parliament.cs:396:TargetCrisisDemands(World w);Government.cs:784:EnactingStage(World w, PolicyAxis a, bool increasing) | Politics.cs Parliament.cs Government.cs | 両側の門 | 11db605f | 未確かめ | ソースで届く（目標は AI とプレイヤーが同じ Parliament.TargetCrisisDemands・施行中は Government.EnactingStage）。枝ごとの件数は MechanismProbe の Hit で、keep の生の表に無い（要る計器＝下の表） | — |
| S16-31 | S16 | 勢力強度の式 | 強さ＝頭数×闘争度×結束×(1−発言権)・近代エリートは制限選挙未満でも声 0.3（L2942） | Politics.cs:1134:double strength = f.Size * f.AvgMilitancy * f.Cohesion * silenced;;Politics.cs:1123:c.ModernEliteSubCensusVoice | Politics.cs | 効き | 11db605f | 働く | 生 R1 dcont_s の strengthRatioMaxIndex（計器は同じ関数を呼ぶ）。読み手 4 本（選定 Politics.cs:900・即時の決着 :1005・計器 InstrumentsDiscontent.cs:107・画面 Display/ProblemScan.cs:163） | — |
| S16-32 | S16 | 発言権の床 | 票を持つブロックにも小さな武装の能力を残す（L2947） | Politics.cs:1133:if (!(silenced >= c.RevoltVoiceFloor)) silenced = c.RevoltVoiceFloor; | Politics.cs | 届かない門 | 11db605f | 働かない | ソース。既定 0（Coefficients.cs:3043）＝床が 0 なので代入は結果を変えない＝休眠の器（文書どおり） | — |
| S16-33 | S16 | 点火と決着の分離（段4） | 点火を〈組織された意志〉と〈州の絶望〉の 2 門へ（L2963） | Politics.cs:912:if (!(c.RevoltIgnitionIntensity > 0));Politics.cs:929:if (!(c.RevoltIgnitionIntensity > 0)) | Politics.cs | 届かない門 | 11db605f | 働かない | ソース。既定 0（Coefficients.cs:3030）＝旧の 2 門の側だけ走る＝休眠の器（文書どおり） | — |
| S16-34 | S16 | 保持力の州按分 | 保持力＝全国×((1−0.5)＋0.5×州の人口の割合)（L2968） | Politics.cs:1204:return coerce * ((1.0 - blend) + blend * share); | Politics.cs | 効き | 11db605f | 働く | ソース。読み手 3 本（組織運動の点火・州の一揆 Politics.cs:1373・計器 InstrumentsDiscontent.cs:106）。郷勇は全国のまま＝文書どおり | — |
| S16-35 | S16 | 内乱の分岐 | 蜂起の目標＝主軸思想の最寄りの政体・今と違えば政体戦・同じなら要求戦（L2973） | Politics.cs:994:Polity target = NearestPolity(best.Ideo); | Politics.cs | 効き | 11db605f | 文書が古い | 生 R1 prdf_s の revoltTarget＝共和制 94 行・立憲君主制 15 行・軍事独裁 1 行。文書の〈民族の分離／政体の転換／要求の貫徹の 3 つ〉の〈民族の分離〉は 2026-09-28（作者裁定 g）で消えた（民族の分離は支配度の分離独立 CheckSecession の 1 本）＝今は 2 つ | — |
| S16-36 | S16 | 体制側の義勇軍（郷勇） | 有産のブロックが思想の距離 0.5 を超える反乱に義勇軍を組み、異端（変異）なら倍（L2977） | Politics.cs:1186:militia += size[b] * Math.Max(0, dist - c.LoyalistDistanceFloor) | Politics.cs | 効き | 11db605f | 働く | ソース。読み手は力の門・即時の決着・計器 InstrumentsDiscontent.cs:106（facStrongCount の分母に入る） | — |
| S16-37 | S16 | 反乱勢力が武器を市場で買う | 運動の強さに装備の充足を掛ける（L2982） | Politics.cs:1139:if (c.RebelArmsMarketEnabled > 0) strength *= f.EquipFill; | Politics.cs | 届かない門 | 11db605f | 働かない | ソース。既定 0（Coefficients.cs:3008）＝休眠の器（文書どおり） | — |
| S16-38 | S16 | 民兵 | 蜂起した運動のブロックの POP が闘争度に応じて札を立て、戦場で弱兵に数える（L2987） | Politics.cs:976:if (c.RevoltMilitiaRaise > 0);Diplomacy.cs:858:c.MilitiaCombatValue > 0;Politics.cs:237:if (p.MilitiaShare.Value > 0 && !inRevolt) p.MilitiaShare = default; | Politics.cs Diplomacy.cs | 届かない門 | 11db605f | 働かない | ソース。既定 0（Coefficients.cs:3050・:3055）＝札の書き手 :985 に届かない・剥がす :237 も札が 0 なので何もしない＝休眠の器（文書どおり） | — |
| S16-39 | S16 | 守備兵の寝返り | 出ていく州の守備兵は闘争度の割合だけ反乱側へ・残りと武器庫は政府の座へ（L2992） | MultiNationSim.cs:373:double stay = 1.0 - PoliticalQuantities.Clamp01(garrison.Militancy);;MultiNationSim.cs:366:Territory.NextCapital(pw, regs) | MultiNationSim.cs | 効き | 11db605f | 働く | ソース（建国路の 1 本）。寝返った兵の量の列は計器に無い | — |
| S16-40 | S16 | 鎮圧の後始末（戦争の経路） | （文書では休眠の器 RevoltAftermathOnWarPath）（L2997） | Coefficients.cs:3013:RevoltAftermathOnWarPath | Politics.cs MultiNationSim.cs | 読まれない欄 | 11db605f | 文書が古い | ソース。係数は 2026-09-28 に消えた（墓標 Coefficients.cs:3013）＝政府の勝ちの後始末は SettleRevolt の 1 本（S16-12）に入った。文書の見出しは ○ 休眠のまま | — |
| S16-41 | S16 | 要求の導出 | 運動はブロックの選好と現行法の乖離から要求の法を導く（法だけ・即座に動く軸は除く）（L3028） | FactionSnapshot.cs:172:ComputeDemands(World w, int bloc;FactionSnapshot.cs:166:IsInstantAxis | FactionSnapshot.cs | 効き | 11db605f | 働く | ソース（読み手＝危機の記録 Politics.cs の RecordCrisisDemands と強制施行 ConcedeToFaction） | — |
| S16-42 | S16 | 強制施行（要求戦の勝ち） | 要求戦で反乱が勝てば要求をのむ＝本丸を 2 段・他を 1 段・政治資本を燃やす（L3028） | Politics.cs:1216:i == 0 ? c.FactionConcessionSteps : 1);Politics.cs:846:ConcedeToFaction(w, c, movement) | Politics.cs | 効き | 11db605f | 文書が古い | ソース。文書の〈強制施行が残るのは旧経路の内戦だけ・戦争の経路には無い〉は違う＝旧経路は 2026-09-28 に消え、今は戦争の経路の決着 SettleRevolt（:846）だけが呼ぶ | — |
| S16-43 | S16 | 弱い扉を押す | 要求は選好×残りの可動域÷(1＋反対の力) の上位 3 軸（L3034） | FactionSnapshot.cs:197:FactionTargetsWeakAxis > 0 | FactionSnapshot.cs | 効き | 11db605f | 働く | ソース（既定 1.0 で枝に入る） | — |
| S16-44 | S16 | 州の一揆の門 | 熱×州の大衆が最大の州の熱が 0.5 を超えると一揆（L3044） | Politics.cs:1369:worstHeat <= c.RegionalRevoltThreshold) return; | Politics.cs | 両側の門 | 11db605f | 働く | 生 R1 secession_s からの近似（首都を除く・挙げていない国は年末の行だけ）＝選ばれる州の熱が 0.5 を超える国×四半期 4717／109633・50 国＝門の両側が出る（r886_work/s16/regional_keep885_base.txt） | — |
| S16-45 | S16 | 州の一揆の鎮圧と譲歩の分岐 | 勢い＝熱×大衆×0.5 が州按分の保持力以下なら鎮圧（戦死 0.002・正統性 −0.05）・超えれば譲歩（L3044） | Politics.cs:1370:double strength = worstHeat * crowd * c.RegionalRevoltStrength;;Politics.cs:1377:c.RevoltSuppressDeadFrac.Value;Politics.cs:1381:c.RevoltSuppressLegitCost | Politics.cs | 両側の門 | 11db605f | 未確かめ | ソースで両方の枝に届く。発火・鎮圧・譲歩の件数の列は計器に無い（要る計器＝下の表） | — |
| S16-46 | S16 | 一揆の譲歩の梯子 | 穀物法撤廃→労働時間→年金→農奴解放→参政権を法の目標に置くだけ・法が動いたら州の闘争度を緩める（L3044） | Politics.cs:1406:if (w.CornLaws > ReformLevel.None);Politics.cs:217:foreach (var r in w.Regions) if (r.Id == w.PendingRevoltRegion) | Politics.cs | 効き | 11db605f | 未確かめ | ソースで届く（目標は Parliament.cs:398・SelfPlay.cs:1896）。件数の列は計器に無い | — |
| S16-47 | S16 | 一揆の熱＝州の平均闘争度 | 熱は 1 量（閉鎖階級と兵士を除く州の平均闘争度）（L3050） | Politics.cs:1686:internal static double RegionRevoltHeat(Region r, Coefficients c) => AvgRegionMilitancy(r); | Politics.cs | 効き | 11db605f | 働く | ソース（式どおり・読み手は州の一揆と組織運動の絶望の門〔休眠〕） | — |
| S16-48 | S16 | 蜂起の待ちは無い（クールダウンの撤去） | 州の一揆と組織運動の共有の4四半期のタイマーを外した＝頻度は内乱の進行の状態の排他と結束の再成長だけ（L3008） | Coefficients.cs:3058:RevoltCooldownRetire;Politics.cs:889:w.PendingRebelRegions is not null) return; | Politics.cs | 届かない門 | 11db605f | 働く | ソース。タイマーの名（RevoltCooldownRetire・RevoltCooldownQuarters・RegionalRevoltCooldown）は SimCore の注（墓標 Coefficients.cs:2977・:3058・:3059・World.cs:824 と Politics.cs:877）にだけ残り、読み手 0。排他は状態（:889・:663・SelectRevoltRegion の予約の州の除外 Politics.cs:1670）だけ | — |

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

## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）


### S01 財と製法と発見

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S01-H001 | S01 | 財カタログ128種 | 文書 L615（粒＝見出し・未分割） | — | Catalog.cs goods.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H002 | S01 | 財の6階層と買う順番 | 文書 L620（粒＝見出し・未分割） | Simulation.cs:62:Simulation | EconomyStep.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H003 | S01 | 参照価格の自動計算 | 文書 L624（粒＝見出し・未分割） | Catalog.cs:550:ChainRelaxIterations;Catalog.cs:535:FactoryVa;Catalog.cs:534:FarmLaborPerOutput;Catalog.cs:533:LaborPerOutput | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H004 | S01 | 発見（レシピ単位の2値＝点数が100に届いたら解禁・2026-10-01 (147) 段2A） | 文書 L629（粒＝見出し・未分割） | Coefficients.cs:1902:DiscoveryContactHalf;Coefficients.cs:1889:DiscoveryNeedHalfProcess;Coefficients.cs:1897:DiscoveryNeedHalfProduct;Coefficients.cs:1917:DiscoveryNeedHalfSite | BilateralMarket.cs Catalog.cs Diplomacy.cs Discovery.cs MultiNationSim.cs World.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H005 | S01 | 発見の表の行（年代＝難易度） | 文書 L647（粒＝見出し・未分割） | Catalog.cs:625:Discoveries;Catalog.cs:748:DiscoveryRowOf;Catalog.cs:774:RetiredByRowOf | Catalog.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H006 | S01 | 制度と思想の発見（財を持たない行・2026-10-01 (152)） | 文書 L651（粒＝見出し・未分割） | Coefficients.cs:1902:DiscoveryContactHalf;Coefficients.cs:1951:DiscoveryNeedHalfIdea;Coefficients.cs:1949:DiscoveryNeedHalfInstitution;Coefficients.cs:2874:FascismCrisisLeftRef | Catalog.cs Discovery.cs EconomyStep.cs Ideology.cs TechEducation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DiscoveryInstitutions・IdeologyReachGate | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H007 | S01 | 産地の発見（大鉱床と植林） | 文書 L656（粒＝見出し・未分割） | Coefficients.cs:3403:BonanzaDepositYears;Coefficients.cs:1917:DiscoveryNeedHalfSite;Coefficients.cs:1923:DiscoverySiteFindHalfPop;Coefficients.cs:2372:GoldRushEndowment | Catalog.cs Discovery.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H008 | S01 | 需要の門＝世界の解禁の和 | 文書 L661（粒＝見出し・未分割） | World.cs:182:WorldDiscovered;Discovery.cs:334:GoodGates | Discovery.cs EconomyStep.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H009 | S01 | 同じ財を作る複数の製法 | 文書 L665（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H010 | S01 | 動力の革命（労働力そのものに製法がある） | 文書 L669（粒＝見出し・未分割） | Simulation.cs:62:Simulation | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H011 | S01 | 職人の工房（工場と競合する） | 文書 L674（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H012 | S01 | 坩堝鋼と錬鉄レール（鋼のベッセマー不連続） | 文書 L678（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CrucibleSteelEnabled | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H013 | S01 | 黄鉄鉱法（硫黄の代替） | 文書 L683（粒＝見出し・未分割） | Coefficients.cs:1355:SulfurFromPyriteEnabled;Coefficients.cs:1356:SulfurFromPyriteProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H014 | S01 | 堆肥（肥料の劣った代替） | 文書 L689（粒＝見出し・未分割） | Coefficients.cs:2818:CompostFertilizerEnabled;Coefficients.cs:2820:CompostLivestockInput;Coefficients.cs:2819:CompostProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H015 | S01 | 硝石丘（硝石の劣った代替） | 文書 L694（粒＝見出し・未分割） | Coefficients.cs:2822:NiterBedEnabled;Coefficients.cs:2823:NiterBedProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H016 | S01 | 冷蔵庫と冷蔵の加工肉 | 文書 L699（粒＝見出し・未分割） | Coefficients.cs:2044:ChilledMeatColdInput;Coefficients.cs:2043:ChilledMeatProductivity | Simulation.cs goods.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ChilledMeatEnabled | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H017 | S01 | 蒸気タービン（電力の第4レシピ） | 文書 L704（粒＝見出し・未分割） | Coefficients.cs:1386:SteamTurbineCoalInput;Coefficients.cs:1384:SteamTurbineEnabled;Coefficients.cs:1387:SteamTurbineProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H018 | S01 | 原油は掘削機で掘る | 文書 L709（粒＝見出し・未分割） | Coefficients.cs:1389:OilMachineryInput;Coefficients.cs:1388:OilNeedsMachinery | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H019 | S01 | 石油ケロシン（灯油の第2レシピ） | 文書 L714（粒＝見出し・未分割） | Coefficients.cs:2046:KeroseneOilInput;Coefficients.cs:2047:KeroseneOilProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 KeroseneFromOilEnabled | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |
| S01-H020 | S01 | マスケットと鋳鉄砲（前期の軍需レシピ） | 文書 L719（粒＝見出し・未分割） | Coefficients.cs:2052:MusketProductivity | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MusketRecipeEnabled | dscd・発見の連続化（00一覧 §00 612 行の元の残り）・発見の式 dsc7・開幕から発見（616 行） |

### S02 値段と賃金の形成

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S02-H001 | S02 | 旬（10日）ごとの値動き・建設・信用 | 文書 L724（粒＝見出し・未分割） | Calendar.cs:10:DaysPerQuarter;Coefficients.cs:646:PriceFlowStepDays | Calendar.cs Credit.cs MarketFlow.cs Model.cs MultiNationSim.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PriceFlowDailyDiv | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H002 | S02 | 値段を動かす3つの力 | 文書 L729（粒＝見出し・未分割） | Coefficients.cs:20:FlowAdjustSpeed;Coefficients.cs:19:PriceAdjustSpeed;Coefficients.cs:21:PriceElasticity;Coefficients.cs:48:TargetStockBuffer | AiRootSupply.cs BilateralMarket.cs Discovery.cs EconomyStep.cs MarketFlow.cs MarketTables.cs ProductionDiag.cs ProductionTables.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H003 | S02 | 備蓄は生産とみなす（在庫も供給に数える） | 文書 L734（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PriceLawSupplyIncludesStock | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H004 | S02 | 戦時の買いだめ | 文書 L739（粒＝見出し・未分割） | Coefficients.cs:2187:StockpileOtherFrac;Coefficients.cs:2184:StockpilePanicWeight;Coefficients.cs:2186:StockpileTensionSpan | MarketFlow.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StockpileTensionThreshold | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H005 | S02 | 極端な値段ほど強く戻す | 文書 L744（粒＝見出し・未分割） | Coefficients.cs:25:PriceAmpCenter;Coefficients.cs:26:PriceAmpEdge;Coefficients.cs:24:PriceAmpLnScale;Coefficients.cs:27:PriceAmpPower | MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H006 | S02 | なめらかな上限・下限 | 文書 L749（粒＝見出し・未分割） | Coefficients.cs:44:PriceCeilFactor;Coefficients.cs:43:PriceFloorFactor;Coefficients.cs:45:SoftBandSharpness | Discovery.cs MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H007 | S02 | 輸出も値段に映る | 文書 L754（粒＝見出し・未分割） | Coefficients.cs:87:PriceLawSeesExports | MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H008 | S02 | 賃金は下がりにくい | 文書 L759（粒＝見出し・未分割） | — | MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ReservationWageFall・ReservationWageRise | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H009 | S02 | 組合の賃金交渉 | 文書 L764（粒＝見出し・未分割） | Simulation.cs:62:Simulation | EconomyStep.cs MarketFlow.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 UnionWageRiseBoost | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H010 | S02 | 組合法の出力倍率 | 文書 L769（粒＝見出し・未分割） | Coefficients.cs:502:LaborUnionControlledOutputMul;Coefficients.cs:501:LaborUnionToleratedOutputMul | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |
| S02-H011 | S02 | 工業化すると賃金の天井が上がる | 文書 L774（粒＝見出し・未分割） | Coefficients.cs:2590:WageClampMax;Coefficients.cs:2589:WageClampMin | MarketFlow.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WageClampIndScale・WageClampSoftBand | 参照価格のリワーク（616 行 ④）・価格水準の正帰還 aic（616 行 ⓪） |

### S03 生産と労働の配分

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S03-H001 | S03 | 作れる量＝人手×生産性×倍率 | 文書 L779（粒＝見出し・未分割） | Simulation.cs:400:FullChainProductivityScale;Coefficients.cs:1159:IndProductivityGain;Coefficients.cs:1160:IndProductivityScale | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H002 | S03 | 労働は価値を生むところへ行く | 文書 L784（粒＝見出し・未分割） | Coefficients.cs:28:SupplyAdjustSpeed;Coefficients.cs:52:TargetStockFloor | AiRootSupply.cs EconomyStep.cs MarketFlow.cs MarketTables.cs ProductionDiag.cs ProductionTables.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H003 | S03 | 儲かるだけ作る（ただし反応は遅い） | 文書 L789（粒＝見出し・未分割） | Coefficients.cs:28:SupplyAdjustSpeed;Coefficients.cs:30:SupplySlope | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H004 | S03 | 在庫を見て増減する | 文書 L794（粒＝見出し・未分割） | Coefficients.cs:51:DesiredInventoryCover;Coefficients.cs:50:InventoryAdjustGain | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H005 | S03 | 足りない材料が1つでもあれば止まる | 文書 L799（粒＝見出し・未分割） | — | EconomyStep.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H006 | S03 | 止まった工場も少しだけ材料を欲しがる | 文書 L803（粒＝見出し・未分割） | Coefficients.cs:36:ReignitionDemandFrac | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない）；消えた係数 ReignitionOrderSettles・ReignitionTrickleConserves | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H007 | S03 | 幻の生産と幻の工場を消す | 文書 L808（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PhantomOutputSnapFloor・TechGatedScaleZero | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H008 | S03 | 眠った生産者（計算量の予算） | 文書 L813（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PhantomOutputSnapFloor・TechGatedScaleZero | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H009 | S03 | 電化 | 文書 L818（粒＝見出し・未分割） | Coefficients.cs:523:ElecBoost;Coefficients.cs:534:ElecFillIrreversibility;Coefficients.cs:524:ElecLaborSave;Coefficients.cs:522:ElecPerOutput | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H010 | S03 | 送電容量 | 文書 L823（粒＝見出し・未分割） | Coefficients.cs:2083:ElecInfraGridBoost;Coefficients.cs:532:GridBuildRate;Coefficients.cs:2578:GridReplaceRate | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 GridDecayRate | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H011 | S03 | 蒸気動力 | 文書 L828（粒＝見出し・未分割） | Coefficients.cs:538:EnergyPerOutput;Coefficients.cs:539:SteamBoost | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H012 | S03 | 採取業の効率化材 | 文書 L833（粒＝見出し・未分割） | Coefficients.cs:529:EffBoost;Coefficients.cs:528:EffPerOutput | Catalog.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H013 | S03 | 工業化度と生産性の飽和 | 文書 L838（粒＝見出し・未分割） | Coefficients.cs:1069:IndDepreciation;Coefficients.cs:1159:IndProductivityGain;Coefficients.cs:1160:IndProductivityScale | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H014 | S03 | 古い設備が足枷になる | 文書 L843（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VintageMigrateRate・VintageObsolescence | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H015 | S03 | 機械が人手を奪う | 文書 L848（粒＝見出し・未分割） | Coefficients.cs:526:LaborSaveIndHalf;Coefficients.cs:525:LaborSaveRate | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H016 | S03 | 規模の経済 | 文書 L853（粒＝見出し・未分割） | Coefficients.cs:1163:ScaleEconomyGain;Coefficients.cs:1164:ScaleEconomyHalf | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H017 | S03 | 土地は増えない（収穫逓減） | 文書 L858（粒＝見出し・未分割） | Coefficients.cs:1033:LandDiminishExponent;Simulation.cs:776:LandHeadroom | EconomyStep.cs Simulation.cs Territory.cs | — | — | この波の線が埋める (891) c75ds | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H018 | S03 | 地代は土地の混み具合で上がる | 文書 L863（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (891) c75ds | 文書の記号 ●（9-26・写さない）；消えた係数 RentScarcityElasticity | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H019 | S03 | 天候・作物の病気・鉱物の枯渇 | 文書 L868（粒＝見出し・未分割） | — | Catalog.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H020 | S03 | 鉱脈は掘るほど痩せる | 文書 L875（粒＝見出し・未分割） | Coefficients.cs:3403:BonanzaDepositYears;Coefficients.cs:3402:DepositGradeFloor;Coefficients.cs:3401:DepositGradeKnee;Coefficients.cs:3400:DepositYearsAtBirth | Discovery.cs EconomyStep.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H021 | S03 | 希少鉱物は産地でしか採れない | 文書 L880（粒＝見出し・未分割） | Coefficients.cs:2829:ScarceCountsIncludeColonies;Coefficients.cs:1342:ScarceMineralGate;Coefficients.cs:1343:ScarceMineralOnlySulfur | Catalog.cs EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ScarceCapFollowsEndowment | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H022 | S03 | 賦存を「同じ費用でどれだけ採れるか」にする | 文書 L885（粒＝見出し・未分割） | Coefficients.cs:1373:EndowmentCostFollowsAbundance;Coefficients.cs:1374:EndowmentCostFollowsScarcity;Coefficients.cs:1365:MineralCostFollowsGrade | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H023 | S03 | 戦災による農業の被害 | 文書 L890（粒＝見出し・未分割） | Coefficients.cs:2601:WarFarmOutputDamage;Coefficients.cs:2603:WarLivestockOutputDamage;Coefficients.cs:2602:WarLivestockRequisition | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |
| S03-H024 | S03 | 在庫は腐る／冷蔵で腐りにくくなる | 文書 L895（粒＝見出し・未分割） | Coefficients.cs:1166:RefrigerationEffect | Catalog.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RefrigerationIndOnset・RefrigerationIndSpan | 資本の在庫 cp3・枠と機械（612・616 行 ①）・TechProductivity の読み手（616 行 ②） |

### S04 需要と消費と自給

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S04-H001 | S04 | 働ける人の数 | 文書 L900（粒＝見出し・未分割） | Coefficients.cs:546:ChildWorkFactor;Coefficients.cs:559:OldLaborFactor | EconomyStep.cs LaborSupply.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H002 | S04 | 欲しい量は値段で変わる（主食は例外） | 文書 L905（粒＝見出し・未分割） | Coefficients.cs:40:CrossElasticity;Coefficients.cs:2552:DemandElasticityMaxDaily;Coefficients.cs:2555:DemandElasticityMaxLuxury;Coefficients.cs:1123:FoodIncludesAnimalProtein | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない）；消えた係数 StapleElasticityZero | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H003 | S04 | 予算の範囲でしか買えない | 文書 L910（粒＝見出し・未分割） | Coefficients.cs:615:DailyNeedWeight;Coefficients.cs:619:LuxuryNeedWeight | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H004 | S04 | 予算の蓋を外す反実仮想（測定専用）（廃止の見出し） | 文書 L915（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ~（9-26・写さない）；消えた係数 PopBudgetRelax | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H005 | S04 | 不足の3分割（食料は怒り・日用品は不満・嗜好品は上振れ） | 文書 L921（粒＝見出し・未分割） | — | EconomyStep.cs Model.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H006 | S04 | 所得で贅沢の量が変わる（廃止の見出し） | 文書 L927（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ~（9-26・写さない）；消えた係数 DemandIncomeElasticity・DemandIncomeFactorMax | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H007 | S04 | 買い占め | 文書 L933（粒＝見出し・未分割） | Coefficients.cs:2468:PanicBuyWeight | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H008 | S04 | 質屋の信用 | 文書 L938（粒＝見出し・未分割） | Coefficients.cs:739:HouseholdCreditMonths;Coefficients.cs:732:PopDebtDriven | EconomyStep.cs FiscalTables.cs Meritocracy.cs Simulation.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ○（9-26・写さない）；消えた係数 HouseholdCreditStapleOnly | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H009 | S04 | 自給と飢饉 | 文書 L943（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない）；消えた係数 SubsistenceHarvestFullExposure | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H010 | S04 | 配給 | 文書 L948（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H011 | S04 | 教育と医療は階層順に配る | 文書 L952（粒＝見出し・未分割）；同じ題 L1950（S11） | Coefficients.cs:565:EduPerChild;EconomyStep.cs:14:ServiceRankCount | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H012 | S04 | 高等教育 | 文書 L957（粒＝見出し・未分割） | Coefficients.cs:175:HigherEduNeedPerEducated | Catalog.cs EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ○（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H013 | S04 | 買い手が見る値段 | 文書 L962（粒＝見出し・未分割） | Coefficients.cs:3185:CartelBanRent;Coefficients.cs:3341:GuildRentPremium;EconomyStep.cs:13:InsuranceCoverageCap;Coefficients.cs:704:MerchantMargin | Contraband.cs EconomyStep.cs Privileges.cs Simulation.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない）；消えた係数 InsuranceSubsidyConsistent・MonopolyDissolveInd | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H014 | S04 | 医療の需要と衛生 | 文書 L967（粒＝見出し・未分割） | Coefficients.cs:2080:ElecInfraSanitationWeight;Coefficients.cs:2068:HealthCrowdingBoost;Coefficients.cs:2065:MedicalWelfareBoost;Coefficients.cs:2073:SoapSanitationWeight | EconomyStep.cs Model.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H015 | S04 | 都市の住居と家賃 | 文書 L972（粒＝見出し・未分割） | Coefficients.cs:2072:HousingMigrationFriction;Coefficients.cs:2070:HousingNeedPerUrban;Coefficients.cs:2071:HousingUrbanThreshold | EconomyStep.cs PopDecision.cs PopTables.cs Simulation.cs Territory.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H016 | S04 | 新聞とラジオ | 文書 L977（粒＝見出し・未分割） | — | Catalog.cs EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない） | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |
| S04-H017 | S04 | アヘン中毒の悪循環 | 文書 L981（粒＝見出し・未分割） | Coefficients.cs:2513:OpiumAddictionCap;Coefficients.cs:2518:OpiumAddictionDecay;Coefficients.cs:2512:OpiumAddictionGrowth;Coefficients.cs:3114:OpiumLaborPenalty | BilateralMarket.cs Diplomacy.cs EconomyStep.cs Habit.cs LawCatalog.cs SelfPlay.cs Simulation.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ◐（9-26・写さない）；消えた係数 OpiumSatisfactionPenalty | 自給の世帯の家計（612 行 ④⑧）・使用人（616 行 ⑨） |

### S05 投資と建設

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S05-H001 | S05 | 資本家は儲かるときだけ投資する | 文書 L986（粒＝見出し・未分割） | Coefficients.cs:481:CrowdingOutWeight;Coefficients.cs:163:EquityHeatEuphoria;Coefficients.cs:1168:IndCapitalCost;Coefficients.cs:1100:IndInvestmentFrac | EconomyStep.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H002 | S05 | 私的投資にも吸収能力の関門 | 文書 L991（粒＝見出し・未分割） | Coefficients.cs:1014:CapitalCreditFloor;Coefficients.cs:1091:PrivateAbsorptionBlend | CapitalMarket.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H003 | S05 | 所得を投資の原資にする（廃止の見出し） | 文書 L996（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 IndInvestFromIncome | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H004 | S05 | 資本家の建設が畑と鉱山を広げる（届かない旧経路） | 文書 L1002（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 RgoCapitalShare・StateShareCounterOnly | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H005 | S05 | 建てるには5つ全部が要る | 文書 L1007（粒＝見出し・未分割） | Coefficients.cs:1181:ConsCement;Coefficients.cs:1182:ConsLumber;Coefficients.cs:1180:ConsMachine;Coefficients.cs:1179:ConsSteel | EconomyStep.cs Megaprojects.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H006 | S05 | 建設の工賃も市場で買う | 文書 L1012（粒＝見出し・未分割） | Coefficients.cs:1107:ConstructionLaborWageFrac | EconomyStep.cs Megaprojects.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConstructionLaborViaMarket | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H007 | S05 | 使い残した材料は手持ちになり、腐る | 文書 L1017（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConstructionMaterialCarryover | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H008 | S05 | 投資予算に全費目を入れる | 文書 L1022（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 InvestBudgetIncludesAllInputs | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H009 | S05 | 鉄道の敷設と保守 | 文書 L1027（粒＝見出し・未分割） | Coefficients.cs:1200:ConsLoco;Coefficients.cs:1199:ConsRail;Coefficients.cs:1202:InfraMaintRate;Coefficients.cs:2576:MaintAdvLaborRate | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H010 | S05 | 国庫による建設 | 文書 L1032（粒＝見出し・未分割） | Simulation.cs:53:World | EconomyStep.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateBuildCost・StateProcurePriceGate | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H011 | S05 | 国庫の建設に吸収能力の蓋（廃止の見出し） | 文書 L1037（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 StateAbsorptionBlend | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H012 | S05 | 国家建設の工期（廃止の見出し） | 文書 L1043（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 StateBuildUnitsPerMonth | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H013 | S05 | 国庫の建設が制度疲労を解く | 文書 L1049（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ModernizationDecayRelief・ModernizationTechGain | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H014 | S05 | 巨大建造物（119件） | 文書 L1055（粒＝見出し・未分割） | Coefficients.cs:1487:MegaprojectCounterCyclical;Coefficients.cs:1488:MegaprojectCrisisDrawdown;Coefficients.cs:1476:MegaprojectEnabled;Coefficients.cs:1483:MegaprojectPrestige | EconomyStep.cs Megaprojects.cs MultiNationSim.cs megaprojects.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H015 | S05 | 巨大建造物への外資 | 文書 L1060（粒＝見出し・未分割） | Coefficients.cs:1493:MegaprojectForeignDrawFrac;Coefficients.cs:1492:MegaprojectForeignMinRank;Coefficients.cs:1491:MegaprojectForeignShare;Coefficients.cs:1494:MegaprojectForeignYield | Megaprojects.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H016 | S05 | プレイヤーの工場建設 | 文書 L1066（粒＝見出し・未分割） | Coefficients.cs:1115:FactoryBuildAffordSteep;Coefficients.cs:1111:FactoryBuildPriceCeil;Coefficients.cs:1105:FactoryBuildScale;Coefficients.cs:1106:FactoryMaterialFrac | Catalog.cs EconomyStep.cs FactoryEquipment.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H017 | S05 | 建設の価格の関門を外す（作者裁定②の器） | 文書 L1071（粒＝見出し・未分割） | Coefficients.cs:1142:FactoryBuildPriceGateScale;Coefficients.cs:1146:FactoryBuildRefIsReal;Coefficients.cs:1114:MilitaryProcurePriceGateScale | EconomyStep.cs SelfPlay.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FacilityBuildOutlayIncludesMaterial・MilitaryBuildCostCapScale | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H018 | S05 | プランテーションの地代 | 文書 L1076（粒＝見出し・未分割） | Coefficients.cs:2193:PlantationRentShare | EconomyStep.cs Simulation.cs | — | — | この波の線が埋める (891) c75ds | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H019 | S05 | 軍需の4段階 | 文書 L1081（粒＝見出し・未分割） | Coefficients.cs:2474:GarrisonProcurement;Coefficients.cs:3553:MotorFuelRate | EconomyStep.cs Military.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ArmsStockEnabled・ArmsWearRate・BaseArmsStockEnabled・EquipNeedsDiscovery・MilitaryProcureProRata・NavalProcurementFromShape・NavalProcurementNeedsPort | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H020 | S05 | 行政・上下水道・電化インフラの国家調達（兵士の給与は下の注記） | 文書 L1086（粒＝見出し・未分割） | Coefficients.cs:713:AdminNeedPerPop;Coefficients.cs:2078:ElecInfraNeedPerUrban;Coefficients.cs:2075:InfraNeedPerUrban | EconomyStep.cs NationBrief.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AdminProcurePriceGate | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H021 | S05 | 国家の買い付けも相場を見る | 文書 L1091（粒＝見出し・未分割） | Coefficients.cs:1114:MilitaryProcurePriceGateScale | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateProcurePriceGate | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H022 | S05 | 儲けの分配 | 文書 L1096（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H023 | S05 | 国有化・補助金・軟予算制約 | 文書 L1100（粒＝見出し・未分割） | Coefficients.cs:3198:NationalizationSeizureRate;Coefficients.cs:3191:NationalizationStateShare;Coefficients.cs:3209:SoeSaleMilitancyCeil;Coefficients.cs:1985:SubsidySoftBudgetDecay | EconomyStep.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SeizureMilitancyCost・SubsidyDependenceRate・SubsidyRespectsSpendingCap | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H024 | S05 | 会社の参入と退出 | 文書 L1105（粒＝見出し・未分割） | Coefficients.cs:120:FactoryBankruptcyMargin;Coefficients.cs:119:FactoryMarginSmoothing;Coefficients.cs:121:FactoryReentryMargin;Coefficients.cs:122:FactoryReentryScale | Discovery.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FactoryBankruptcyAllTiers | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H025 | S05 | 工場の休止（もう1つの動詞） | 文書 L1110（粒＝見出し・未分割） | Coefficients.cs:1120:FactoryMothballCap;Coefficients.cs:1119:FactoryMothballMarginIn;Coefficients.cs:1118:FactoryMothballRate | EconomyStep.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H026 | S05 | 供給弾力のカテゴリ制約（鉱山と畑は工場のようには伸びない） | 文書 L1115（粒＝見出し・未分割） | Coefficients.cs:3419:FarmScaleCap;Coefficients.cs:3418:MineralScaleCap;Coefficients.cs:3420:RgoScaleSpeedFactor | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RgoElasticityGate | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H027 | S05 | 勢いで投資する（バブルの種） | 文書 L1120（粒＝見出し・未分割） | Coefficients.cs:2604:InvestExtrapolationWeight | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |
| S05-H028 | S05 | 商人への売上振替／植民地素材の輸入 | 文書 L1125（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Colonial.cs EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonialExtractionFraction・ColonialImportLagMonths・ColonialProfitInvestorShare・ColonialProfitMerchantShare・TradeColonialGoods | 巨大建造物・u12 B′（612 行）・建屋の段の耐久・StateBuildQueue gc7（616 行） |

### S06 お金の記帳・物価・GDP・損害

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S06-H001 | S06 | 失業率と生活水準 | 文書 L1130（粒＝見出し・未分割） | Coefficients.cs:830:JobSearchUsesEma;Coefficients.cs:828:ServiceUnemployment | Crime.cs EconomyStep.cs Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CrimeSatPenalty・CrimeWealthFearWeight | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H002 | S06 | お金の厳密な記帳 | 文書 L1135（粒＝見出し・未分割） | Bank.cs:40:Carry;UnionBargaining.cs:81:Transfer;Simulation.cs:53:World | Bank.cs EconomyStep.cs Invariants.cs Model.cs Simulation.cs UnionBargaining.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H003 | S06 | 物価水準と地域の物価差 | 文書 L1142（粒＝見出し・未分割） | Coefficients.cs:951:MonetaryTrendSpeed;Coefficients.cs:955:PriceLevelFloor;Coefficients.cs:962:RegionalPriceLevelMax;Coefficients.cs:961:RegionalPriceLevelMin | EconomyStep.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PriceLevelCeil | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H004 | S06 | GDPの測り方 | 文書 L1147（粒＝見出し・未分割） | Simulation.cs:53:World | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H005 | S06 | 内戦・ストライキ・恐慌による損害 | 文書 L1151（粒＝見出し・未分割） | Coefficients.cs:2011:CivilWarEconomyDisruption;Coefficients.cs:1082:DepressionScrapRate;Coefficients.cs:1083:DepressionScrapThreshold;Coefficients.cs:500:StrikeMilitancyThreshold | EconomyStep.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H006 | S06 | 恐慌が設備を解体する | 文書 L1156（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DepressionScrapsBuiltScale | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H007 | S06 | 開始前の助走 | 文書 L1161（粒＝見出し・未分割） | Simulation.cs:62:Simulation;Simulation.cs:53:World;MultiNationSim.cs:58:MultiNationSim | EconomyStep.cs MultiNationSim.cs Simulation.cs World.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H008 | S06 | 国の値を州へ写す | 文書 L1165（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H009 | S06 | 国内寡占の値上げ | 文書 L1169（粒＝見出し・未分割） | — | EconomyStep.cs MarketFlow.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 CompetitiveFirmsBase・DomesticMarkupMax・MonopolyResentmentMilitancy | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H010 | S06 | 退蔵の取り崩し | 文書 L1174（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 HoardConsumptionMultiple・HoardDishoardRate | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H011 | S06 | 退蔵の結晶化 | 文書 L1179（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 HoardCrystallizeRate・WealthAnchorYears | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H012 | S06 | 開発度の記憶 | 文書 L1184（粒＝見出し・未分割） | Coefficients.cs:1079:DevelopmentMemory | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H013 | S06 | 当期の在庫による律速（廃止の見出し） | 文書 L1189（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 CurrentInputFillGates | r1（612 行）・価格水準 aic（616 行 ⓪） |
| S06-H014 | S06 | 貿易タブ用の表示専用データ | 文書 L1195（粒＝見出し・未分割） | Simulation.cs:53:World | BilateralMarket.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | r1（612 行）・価格水準 aic（616 行 ⓪） |

### S07 州と州の清算

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S07-H001 | S07 | 州と州の突き合わせ | 文書 L1264（粒＝見出し・未分割） | InternationalMarket.cs:33:InternationalMarket | BilateralMarket.cs InternationalMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H002 | S07 | 平均価格（市場ではなく統計） | 文書 L1268（粒＝見出し・未分割） | InternationalMarket.cs:33:InternationalMarket;MultiNationSim.cs:58:MultiNationSim | BilateralMarket.cs InternationalMarket.cs MultiNationSim.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない）；消えた係数 WorldPriceStatUnclamped | 輸入の払い手（612 行 ①） |
| S07-H003 | S07 | 帯の拡張（裾どうしの遮断を外す） → 2026-09-29 段3-1（R6）で帯ごと撤去（廃止の見出し） | 文書 L1273（粒＝見出し・未分割） | — | — | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H004 | S07 | 引き渡し価格 | 文書 L1277（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H005 | S07 | 探索の打ち切り | 文書 L1281（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H006 | S07 | 高く買う相手から順に | 文書 L1285（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H007 | S07 | 単価は次善の相手が決める | 文書 L1289（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H008 | S07 | 買い手の量（水準式） | 文書 L1293（粒＝見出し・未分割） | Coefficients.cs:21:PriceElasticity | BilateralMarket.cs Discovery.cs MarketFlow.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない）；消えた係数 TradeArbitrageClearing | 輸入の払い手（612 行 ①） |
| S07-H009 | S07 | 買い手も約定の単価で量を解く | 文書 L1298（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | BilateralMarket.cs Coefficients.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない）；消えた係数 TradeBuyerSolvesAtUnitPrice | 輸入の払い手（612 行 ①） |
| S07-H010 | S07 | 売り手の量（水準式） | 文書 L1303（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H011 | S07 | 次善へ回る（ロールオーバー） | 文書 L1307（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H012 | S07 | 支払える金の上限 | 文書 L1311（粒＝見出し・未分割） | — | BilateralMarket.cs EconomyStep.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H013 | S07 | 価値の高い財から捌く | 文書 L1315（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H014 | S07 | 月ごとの清算 | 文書 L1319（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;InternationalMarket.cs:33:InternationalMarket | InternationalMarket.cs MultiNationSim.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない）；消えた係数 TradeMonthlyClearing | 輸入の払い手（612 行 ①） |
| S07-H015 | S07 | 州と州の融通（国内も同じ仕組み） | 文書 L1324（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H016 | S07 | 国際市場の間引き（眠っている組） | 文書 L1328（粒＝見出し・未分割） | Model.cs:832:PrevImport;Model.cs:857:ClearFailLp | BilateralMarket.cs Model.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H017 | S07 | 為替で換算する | 文書 L1332（粒＝見出し・未分割） | Diplomacy.cs:311:CumulativeFxResidual;InternationalMarket.cs:33:InternationalMarket;UnionBargaining.cs:81:Transfer | BilateralMarket.cs Diplomacy.cs InternationalMarket.cs UnionBargaining.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H018 | S07 | 輸出代金は④の持分へ | 文書 L1338（粒＝見出し・未分割） | InternationalMarket.cs:33:InternationalMarket;InternationalMarket.cs:70:PaySales;EconomyStep.cs:706:RentShareOf | EconomyStep.cs Fiscal.cs InternationalMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない） | 輸入の払い手（612 行 ①） |
| S07-H019 | S07 | 関税は国境の税 | 文書 L1342（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | この波の線が埋める (887) c75ip | 文書の記号 ●（9-26・写さない）；消えた係数 TariffBorderOnly | 輸入の払い手（612 行 ①） |

### S08 輸送と地理

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S08-H001 | S08 | 実距離（陸→港→海→港→陸） | 文書 L1347（粒＝見出し・未分割） | Coefficients.cs:2657:LandSeaCostRatio | BilateralMarket.cs Discovery.cs Geo.cs SeaGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H002 | S08 | 陸路の直行 | 文書 L1352（粒＝見出し・未分割） | Coefficients.cs:2626:LandRouteEnabled | BilateralMarket.cs Geo.cs PathGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H003 | S08 | 地形の輸送コスト | 文書 L1357（粒＝見出し・未分割） | Coefficients.cs:2638:TerrainHorseCost;Coefficients.cs:2639:TerrainRailCost;Coefficients.cs:2650:TerrainWaterMajorCost;Coefficients.cs:2651:TerrainWaterMinorCost | ArmyShape.cs BilateralMarket.cs Discovery.cs Places.cs places.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TerrainCostEnabled | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H004 | S08 | 鉄道時代の陸運低下 | 文書 L1363（粒＝見出し・未分割） | Coefficients.cs:2656:RailCostFraction | BilateralMarket.cs Discovery.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H005 | S08 | 海路（海のグラフの最短路） | 文書 L1368（粒＝見出し・未分割） | — | SeaGraph.cs passages.json sea_nodes.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H006 | S08 | 海に出る口（接ぎ木） | 文書 L1372（粒＝見出し・未分割） | Coefficients.cs:3598:SeaStitchLeadBudgetMult | SeaGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H007 | S08 | 陸のグラフ（州・植民地・バリア・峠） | 文書 L1377（粒＝見出し・未分割） | — | PathGraph.cs barriers.json landmass.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H008 | S08 | 陸塊の判定 | 文書 L1381（粒＝見出し・未分割） | Landmass.cs:40:Landmass;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs Landmass.cs MultiNationSim.cs PathGraph.cs landmass.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H009 | S08 | 陸のマスクは2枚 | 文書 L1385（粒＝見出し・未分割） | — | Landmask.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H010 | S08 | 水域の回避と沿岸の迂回 | 文書 L1389（粒＝見出し・未分割） | — | PathGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LandEdgesAvoidWater | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H011 | S08 | 山脈の壁（点でなく線分） | 文書 L1394（粒＝見出し・未分割） | PathGraph.cs:41:BarrierClearanceKm;Coefficients.cs:2746:WallLinkKm | PathGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BarrierWallSegments | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H012 | S08 | 峠 | 文書 L1399（粒＝見出し・未分割） | PathGraph.cs:19:PassCostMult | ArmyShape.cs PathGraph.cs barriers.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H013 | S08 | 海盆 | 文書 L1404（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Model.cs SeaGraph.cs Simulation.cs sea_nodes.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H014 | S08 | 輸送の楔（従価） | 文書 L1408（粒＝見出し・未分割） | Coefficients.cs:1059:TransportMargin | BilateralMarket.cs Discovery.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DistanceMarginScale | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H015 | S08 | 重量による楔（従量） | 文書 L1413（粒＝見出し・未分割） | Coefficients.cs:2661:TransportWeightMarginScale | BilateralMarket.cs Catalog.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H016 | S08 | 運賃革命 | 文書 L1418（粒＝見出し・未分割） | Coefficients.cs:1838:TransportRevolutionMax;Coefficients.cs:1839:TransportRevolutionOnset;Coefficients.cs:1840:TransportRevolutionSpan | BilateralMarket.cs DiscoveryTables.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FreightRevolutionAdValorem | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H017 | S08 | 運賃は海運国が受け取る | 文書 L1423（粒＝見出し・未分割） | Coefficients.cs:2605:FreightRatePer10Mm | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H018 | S08 | 商人の仲介料 | 文書 L1428（粒＝見出し・未分割） | Coefficients.cs:705:MerchantTradeMargin | BilateralMarket.cs InternationalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H019 | S08 | 海上保険 | 文書 L1433（粒＝見出し・未分割） | Coefficients.cs:1519:InsurancePayoutFrac;Coefficients.cs:1518:MarinePremiumRate | BilateralMarket.cs MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H020 | S08 | 通路（海峡と運河） | 文書 L1438（粒＝見出し・未分割） | — | SeaGraph.cs passages.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H021 | S08 | 咽喉と扼者 | 文書 L1442（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;MultiNationSim.cs:58:MultiNationSim | Diplomacy.cs MultiNationSim.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H022 | S08 | 運河の建設（スエズ・パナマ） | 文書 L1446（粒＝見出し・未分割） | Coefficients.cs:1504:CanalInvestFrac | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H023 | S08 | 運河の通行料と出資配当 | 文書 L1451（粒＝見出し・未分割） | Coefficients.cs:2707:CanalInvestReturns;Coefficients.cs:2706:CanalTollTrafficRate | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H024 | S08 | 運河の封鎖 | 文書 L1458（粒＝見出し・未分割） | — | BilateralMarket.cs SeaGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H025 | S08 | 海の出口を握られる | 文書 L1462（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RouteDependenceTensionRate | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H026 | S08 | 港湾の建設・減耗・港湾料 | 文書 L1467（粒＝見出し・未分割） | Coefficients.cs:1509:PortBuildCostPerUnit;Coefficients.cs:1512:PortCapToThroughput;Coefficients.cs:1514:PortCongestionTarget;Coefficients.cs:1510:PortDecay | BilateralMarket.cs MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H027 | S08 | 港の取扱高（四半期の累計） | 文書 L1472（粒＝見出し・未分割） | — | BilateralMarket.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H028 | S08 | 凍結港 | 文書 L1476（粒＝見出し・未分割） | — | BilateralMarket.cs Climate.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WinterPortFreeze | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H029 | S08 | 港の配線は領土に追随する | 文書 L1481（粒＝見出し・未分割） | Simulation.cs:62:Simulation;MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PortRewireEnabled | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H030 | S08 | 内陸州は港が遠い | 文書 L1486（粒＝見出し・未分割） | Simulation.cs:62:Simulation | BilateralMarket.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 InlandPortDistanceDecay | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H031 | S08 | 内陸国の通過港と通行料 | 文書 L1491（粒＝見出し・未分割） | Coefficients.cs:2659:TransitTollShare | BilateralMarket.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TransitEnabled | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H032 | S08 | 輸送容量（鉄道と船） | 文書 L1496（粒＝見出し・未分割） | Coefficients.cs:1227:ShipTier2CapMult;Coefficients.cs:1213:TransportBuildRate;Coefficients.cs:1212:TransportCapPerUnit;Coefficients.cs:1207:TransportDecay | Colonial.cs Discovery.cs Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H033 | S08 | 基礎輸送は陸（道路と川） | 文書 L1503（粒＝見出し・未分割） | Coefficients.cs:1208:TransportTargetPerPop | BilateralMarket.cs Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TransportBaseline・TransportBaselineIsLand | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H034 | S08 | 目標容量（人口・混雑・脅威） | 文書 L1508（粒＝見出し・未分割） | Coefficients.cs:2445:ThreatRailTargetWeight;Coefficients.cs:2020:TransportCongestionWeight;Coefficients.cs:1208:TransportTargetPerPop | Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H035 | S08 | 鉄道の技術解禁 | 文書 L1513（粒＝見出し・未分割） | — | Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H036 | S08 | 容量を州へ配る | 文書 L1518（粒＝見出し・未分割） | Transport.cs:83:RegionShare | BilateralMarket.cs Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TransportShareByPotential | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H037 | S08 | 輸送容量は同時制約 | 文書 L1523（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeCapacityHard | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |
| S08-H038 | S08 | 植民地補給が海運を食う | 文書 L1528（粒＝見出し・未分割） | Coefficients.cs:2508:ColonyShipOccupyPerDist | BilateralMarket.cs Colonial.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 輸送の段1と運河（616 行 ⑦）・港の設備の材化（616 行 ⑧） |

### S09 関税と通商の法と封鎖

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S09-H001 | S09 | 関税 | 文書 L1533（粒＝見出し・未分割） | Coefficients.cs:1031:TariffFloor | BilateralMarket.cs Fiscal.cs FiscalTables.cs Government.cs InternationalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TariffProtect | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H002 | S09 | 関税収入 | 文書 L1538（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H003 | S09 | ダンピング（負の関税） | 文書 L1542（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H004 | S09 | 高関税は輸出も閉じる | 文書 L1546（粒＝見出し・未分割） | Coefficients.cs:1956:IsolationExportWeight | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H005 | S09 | 関税報復と相互低減 | 文書 L1551（粒＝見出し・未分割） | Coefficients.cs:708:TariffReactionMax;Coefficients.cs:707:TariffRetaliation | Diplomacy.cs Government.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H006 | S09 | 通商条約（廃止の見出し） | 文書 L1556（粒＝見出し・未分割）；同じ題 L5780（S25） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeTreatyEnabled | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H007 | S09 | 条約港（関税自主権の喪失） | 文書 L1562（粒＝見出し・未分割） | Coefficients.cs:1023:TreatyPortReparationMult;Coefficients.cs:1018:TreatyPortWinnerDistanceKm | Diplomacy.cs InternationalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TreatyPortTariffLoss | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H008 | S09 | 条約港の自然失効 | 文書 L1567（粒＝見出し・未分割）；同じ題 L5685（S25） | Coefficients.cs:2762:ForeignRuleFromTreaty;Coefficients.cs:1022:TreatyPortLapseRatio | Diplomacy.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TreatyPortLapseYears | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H009 | S09 | 穀物法 | 文書 L1572（粒＝見出し・未分割） | Coefficients.cs:256:CornLawImportPenalty | BilateralMarket.cs InternationalMarket.cs ProductionDiag.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CornLawGrainChainGate | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H010 | S09 | 州の関税利害 | 文書 L1577（粒＝見出し・未分割）；同じ題 L2896（S27） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H011 | S09 | 阿片の法 | 文書 L1581（粒＝見出し・未分割） | Coefficients.cs:2512:OpiumAddictionGrowth | BilateralMarket.cs Diplomacy.cs Habit.cs LawCatalog.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H012 | S09 | 密輸（禁令の漏れ） | 文書 L1586（粒＝見出し・未分割） | Coefficients.cs:2188:SmugglingMax | Contraband.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H013 | S09 | 軍需輸出の法 | 文書 L1591（粒＝見出し・未分割）；同じ題 L3560（S19）・L5066（S24） | Coefficients.cs:2179:MilitaryExportAccessFloor;Coefficients.cs:2176:MilitaryExportGoods;Coefficients.cs:2177:MilitaryExportTensionThreshold | BilateralMarket.cs Diplomacy.cs PolicyPreferences.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H014 | S09 | 交易できない財 | 文書 L1596（粒＝見出し・未分割） | — | BilateralMarket.cs goods.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H015 | S09 | 植民地財も交易に載る | 文書 L1600（粒＝見出し・未分割） | — | BilateralMarket.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeColonialGoods | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H016 | S09 | 海上封鎖の主食狙撃 | 文書 L1605（粒＝見出し・未分割）；同じ題 L6678（S28） | Coefficients.cs:2364:BlockadeStapleBias | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H017 | S09 | 通商破壊（経路上のリスク） | 文書 L1610（粒＝見出し・未分割） | Coefficients.cs:2612:RiskLambda | BilateralMarket.cs Diplomacy.cs SeaGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H018 | S09 | 禁輸・交戦による遮断 | 文書 L1615（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Simulation.cs:53:World | BilateralMarket.cs Diplomacy.cs MultiNationSim.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeAccessNormalized | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H019 | S09 | 反覇権の経済封じ込め | 文書 L1620（粒＝見出し・未分割） | Coefficients.cs:1277:HegemonContainmentMax;Coefficients.cs:1276:HegemonContainmentTrade | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H020 | S09 | 交易比重の動的更新 | 文書 L1625（粒＝見出し・未分割） | Coefficients.cs:2704:TradeWeightBase;Coefficients.cs:2705:TradeWeightGdpSlope | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeWeightDynamic | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H021 | S09 | 国どうしの取引額の記帳（思想の伝播の道） | 文書 L1630（粒＝見出し・未分割） | Coefficients.cs:1954:DiffusionWeight | BilateralMarket.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H022 | S09 | 輸出代金は生産者へ直接入る | 文書 L1635（粒＝見出し・未分割） | InternationalMarket.cs:33:InternationalMarket | BilateralMarket.cs InternationalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ExportRevenueToProducers | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H023 | S09 | 輸出が労働を引っぱる | 文書 L1640（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H024 | S09 | 財ごとの売買方針 | 文書 L1644（粒＝見出し・未分割） | Coefficients.cs:1024:TradeBiasStrength | BilateralMarket.cs InternationalMarket.cs SelfPlay.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H025 | S09 | 貿易テレメトリと貿易タブ | 文書 L1649（粒＝見出し・未分割） | Simulation.cs:53:World | BilateralMarket.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H026 | S09 | 地図の航路と陸路 | 文書 L1653（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H027 | S09 | 輸送の断面 | 文書 L1657（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H028 | S09 | 地理グラフの診断 | 文書 L1661（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H029 | S09 | 到達性と相手の断面 | 文書 L1665（粒＝見出し・未分割） | — | TradeReachDiag.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H030 | S09 | 貿易の時系列と分解 | 文書 L1669（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H031 | S09 | 清算の時間の計器 | 文書 L1673（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H032 | S09 | 拿捕賞金 | 文書 L1677（粒＝見出し・未分割）；同じ題 L5060（S24）・L5597（S25） | Coefficients.cs:1792:PrizeCargoShare | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 PrizeLossProrated・PrizeMoneyRate | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H033 | S09 | 保護の代償（輸出閾値のドラッグ）（廃止の見出し） | 文書 L1682（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H034 | S09 | 旧経路の数量則（定数速度）（廃止の見出し） | 文書 L1686（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | BilateralMarket.cs Coefficients.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 TradeArbitrageMax・TradeScaleUsesDesired・TradeSpeed | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H035 | S09 | 汽船の運行燃料 | 文書 L1692（粒＝見出し・未分割） | Coefficients.cs:3539:ShipFuelRate | Colonial.cs Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ShipSteamShareSmoothing | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H036 | S09 | 輸送目標の別の形 | 文書 L1699（粒＝見出し・未分割） | — | Transport.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 TransportDemandAffordCap・TransportPotentialHeadroom・TransportPotentialWeight | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H037 | S09 | 報復を政治へ通す（廃止の見出し） | 文書 L1704（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs Government.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 TariffRetaliationViaPolitics | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |
| S09-H038 | S09 | 冗長な陸路の刈り込み（廃止の見出し） | 文書 L1710（粒＝見出し・未分割） | — | PathGraph.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 RedundantEdgePruning | 貿易 R-a 封鎖の危険の書き手（616 行 ⑦） |

### S10 人口動態と暮らし

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S10-H001 | S10 | 集団（POP）＝州×階級 | 文書 L1775（粒＝見出し・未分割） | — | Model.cs PopClass.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H002 | S10 | 養える人数へ向かう人口 | 文書 L1780（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PopGrowthRate | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H003 | S10 | 養える人数を決める8つの力 | 文書 L1785（粒＝見出し・未分割） | Coefficients.cs:1825:FoodCapacityWeight;Coefficients.cs:790:HealthFillCapacityBonus;Coefficients.cs:1822:HealthcareCapacityBonus;Coefficients.cs:801:OverworkHealthSlope | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IndustryCapacityBonus・PollutionCapacityWeight | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H004 | S10 | 医療は国家と市場の二本立て | 文書 L1790（粒＝見出し・未分割） | Coefficients.cs:575:DoctorHealthcareSubstitute;Coefficients.cs:1823:HealthcareEducationGate;Coefficients.cs:2027:HealthcareRealGate | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H005 | S10 | 工業化度は人口で重み付けする | 文書 L1795（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IndustrializationPopWeighted | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H006 | S10 | 食料の採れ高が人口の天井を押し上げる | 文書 L1800（粒＝見出し・未分割） | Coefficients.cs:2669:CapacityFromFoodOutput;Coefficients.cs:2886:FoodOutputAnchorProduction | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H007 | S10 | 輸入で人口を養う | 文書 L1805（粒＝見出し・未分割） | Coefficients.cs:1832:FoodAnchorNetImport | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeLedgerFix | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H008 | S10 | 耕地が養える人数 | 文書 L1810（粒＝見出し・未分割） | Coefficients.cs:2059:LandAnchorCapacityWeight;Simulation.cs:776:LandHeadroom | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H009 | S10 | 土地が余っていると子が増える | 文書 L1815（粒＝見出し・未分割） | Coefficients.cs:2058:LandRoomFertilityCap;Coefficients.cs:1826:LandRoomFertilityWeight | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H010 | S10 | 出生率の低下（女子教育と所得） | 文書 L1820（粒＝見出し・未分割） | Coefficients.cs:552:FemaleLitFloor;Coefficients.cs:567:FertilityBase;Coefficients.cs:569:FertilityFloor;Coefficients.cs:571:FertilityIncomeSlope | Simulation.cs TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WomenEducationGate | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H011 | S10 | 出生率を均衡人口へ結ぶ | 文書 L1825（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FertilityEquilibriumWeight | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H012 | S10 | 分割相続の出生抑制 | 文書 L1830（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FertilityLandDivisionSlope | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H013 | S10 | 所得少子化の因果順序 | 文書 L1835（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FertilityIncomeSurvivalGate | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H014 | S10 | 産める女性の数で決まる（カップル律速） | 文書 L1840（粒＝見出し・未分割） | Coefficients.cs:2546:FertileShareFloor | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CoupleLimitedBirths・CrudeBirthRate・FertileWomenBirths | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H015 | S10 | 出生配分の共通の分母（凍結分母の是正） | 文書 L1846（粒＝見出し・未分割） | Simulation.cs:62:Simulation;Simulation.cs:2525:FertilePool | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FertileAbsoluteBlend | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H016 | S10 | 飢えの超過死亡（飢えの深さの絶対の率） | 文書 L1851（粒＝見出し・未分割）；同じ題 L6563（S28） | Coefficients.cs:579:FamineExcessMortality;Coefficients.cs:586:FamineExcessMortalityExponent | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FamineMortalityConcentration | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H017 | S10 | 平常の死は年齢の構成で配る | 文書 L1858（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H018 | S10 | 飢餓の絶対ライン | 文書 L1863（粒＝見出し・未分割）；同じ題 L6568（S28） | Coefficients.cs:588:FamineDeathFillFloor | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H019 | S10 | 主食の充足はゆっくり動く（穀倉の備蓄） | 文書 L1868（粒＝見出し・未分割） | Coefficients.cs:2061:FoodFillSmoothing | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H020 | S10 | 局地の飢饉（廃止の見出し） | 文書 L1873（粒＝見出し・未分割）；同じ題 L6552（S28） | Simulation.cs:2819:SmoothRegionalFoodFill | Simulation.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 RegionalFamineMortality | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H021 | S10 | 局地飢饉の絶対水準ゲート（廃止の見出し） | 文書 L1879（粒＝見出し・未分割）；同じ題 L6558（S28） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 FamineAbsoluteThreshold | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H022 | S10 | 年齢の層（人口ピラミッド） | 文書 L1885（粒＝見出し・未分割） | Model.cs:486:StdPyramidGrowth | Model.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AgingYears・OldMortalityWeight・OldShareMax・WorkingYears | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H023 | S10 | 老年は現役ほど働かない | 文書 L1890（粒＝見出し・未分割） | Coefficients.cs:559:OldLaborFactor | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H024 | S10 | 人口の帳簿 | 文書 L1895（粒＝見出し・未分割） | Simulation.cs:62:Simulation;Simulation.cs:53:World | Politics.cs Simulation.cs Territory.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H025 | S10 | 疫病（コレラ型） | 文書 L1899（粒＝見出し・未分割） | Coefficients.cs:603:EpidemicChance;Coefficients.cs:608:EpidemicContagiousYears;Coefficients.cs:604:EpidemicSeverity | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EpidemicSanitationHalf・EpidemicSanitationWeight | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H026 | S10 | 衛生は流行の発生そのものを止める | 文書 L1904（粒＝見出し・未分割） | Coefficients.cs:2026:EpidemicOnsetSanitationWeight | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H027 | S10 | 疫病死は脆弱層に集中する | 文書 L1909（粒＝見出し・未分割） | Coefficients.cs:606:EpidemicVulnConcentration | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H028 | S10 | 疫病の伝播（国内と国際） | 文書 L1915（粒＝見出し・未分割） | Coefficients.cs:2674:EpidemicIntraCountrySpread;Coefficients.cs:611:EpidemicTradeTransmit;Coefficients.cs:609:EpidemicTransmitChance;Coefficients.cs:610:EpidemicTransmitRange | MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H029 | S10 | 災害はコホートに穴を残す | 文書 L1920（粒＝見出し・未分割）；同じ題 L6573（S28） | Coefficients.cs:2032:DisasterAgeMortalityExcess | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H030 | S10 | 働ける人の数（家計の時間の配分） | 文書 L1925（粒＝見出し・未分割） | Coefficients.cs:546:ChildWorkFactor;Coefficients.cs:551:FemaleNeedWeight;Coefficients.cs:544:FemalePartBase;Coefficients.cs:545:FemalePartProperty | EconomyStep.cs LaborSupply.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ChildLaborFollowSpeed | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H031 | S10 | 徴兵が人口構成を変える | 文書 L1930（粒＝見出し・未分割） | Coefficients.cs:2097:ConscriptGrievanceRate | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConscriptExcludeOldMales | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H032 | S10 | 貯蓄と持参金 | 文書 L1935（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | EconomyStep.cs MultiNationSim.cs PopDecision.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない）；消えた係数 HoardDishoardRate・MigrantDowryWorldBase | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |

### S11 教育と識字と技術の伝わり

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S11-H001 | S11 | 教育は財として売買され、識字を生む | 文書 L1940（粒＝見出し・未分割） | Coefficients.cs:565:EduPerChild | EconomyStep.cs TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H002 | S11 | 教育の担い手は法で切り替わる | 文書 L1945（粒＝見出し・未分割） | Coefficients.cs:509:ClergyEduProductivity;Coefficients.cs:510:TeacherEduProductivity | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H003 | S11 | 義務教育の国庫肩代わり | 文書 L1954（粒＝見出し・未分割） | Coefficients.cs:183:EduReachFromAdmin;Coefficients.cs:1858:PublicEduFundingShare | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RuleSharesReach | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H004 | S11 | 教育費の国庫の現金クランプ（⚠ 2026-09-28 支払いの一元化 段2-1で撤去＝教育費も国家の支払い率 r） | 文書 L1959（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EduFundingCashClamp | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H005 | S11 | 教区学校と公立学校の常設・師範学校の効果 | 文書 L1964（粒＝見出し・未分割） | Coefficients.cs:520:EduProducersCoexist;Coefficients.cs:178:TeacherQualityWeight | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H006 | S11 | 児童労働と識字のトレードオフ | 文書 L1969（粒＝見出し・未分割） | Coefficients.cs:2084:ChildLaborLiteracyPenalty | TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H007 | S11 | 法の執行には行政力が要る | 文書 L1974（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LawEnforcementAdminGate・WorkHoursFollowSpeed | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H008 | S11 | 支持層を作った法は戻せない | 文書 L1979（粒＝見出し・未分割） | Coefficients.cs:1727:IrreversibleLaws | Government.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IrreversibleEmancipation | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H009 | S11 | 識字は世代で薄まる | 文書 L1984（粒＝見出し・未分割） | Coefficients.cs:552:FemaleLitFloor | Model.cs TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LiteracyDecayIsGenerational・LiteracyGenerationalDecay | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H010 | S11 | エリートの創発 | 文書 L1989（粒＝見出し・未分割） | — | TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H011 | S11 | 吸収能力（識字 × 世俗の水路 × 報道の自由 × 行政） | 文書 L1993（粒＝見出し・未分割） | Coefficients.cs:152:CatchupAbsorptionBlend;Coefficients.cs:132:EliteForFullTech | TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AbsorptionAdminWeight・CapitalAbsorptionBlend | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H012 | S11 | 吸収能力の経済側（所得）と革新の規模 | 文書 L1998（粒＝見出し・未分割） | — | TechEducation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 AbsorptionIncomeRef・AbsorptionIncomeWeight・ResearchBaseRefPop・ResearchScaleWeight | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H013 | S11 | 専門人材の厚みと接触 | 文書 L2003（粒＝見出し・未分割） | — | TechEducation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 AdoptExpertiseWeight・CatchupContactWeight・InnovateExpertiseWeight | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H014 | S11 | 技術水準の伸び方（発明と模倣の2本立て） | 文書 L2008（粒＝見出し・未分割） | Coefficients.cs:130:AdoptionCeiling;Coefficients.cs:154:AdoptionRate;Coefficients.cs:153:AdoptionRateAdvanced;Coefficients.cs:135:CatchupFraction | TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DecayTechAdoptWeight・InnovationAdminThreshold | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |
| S11-H015 | S11 | 特許制度が革新を速める | 文書 L2028（粒＝見出し・未分割） | Coefficients.cs:161:PatentInnovationBonus | TechEducation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 奨学金の入口 sch3・TechProductivity と技術水準の読み手（616 行 ②） |

### S13 移住と都市化と民族

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S13-H001 | S13 | 移民制限法（開放／選別／閉鎖） | 文書 L2033（粒＝見出し・未分割） | Coefficients.cs:2702:ImmigrationClosedFrac;Coefficients.cs:2697:ImmigrationRestrictionPrefScale;Coefficients.cs:2701:ImmigrationSelectiveFrac | MultiNationSim.cs NationConfig.cs Policy.cs PolicyPreferences.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MigrationRedistributeClipped | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H002 | S13 | 受入能力（混雑そのものが流入を止める） | 文書 L2044（粒＝見出し・未分割） | Coefficients.cs:1308:ImmigrationAbsorbExponent | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |

### S12 社会移動と失業

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S12-H001 | S12 | 実力主義の度合い | 文書 L2049（粒＝見出し・未分割） | Coefficients.cs:811:MeritGroupWeight | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H002 | S12 | 実力主義を身分の閉鎖度から導く | 文書 L2054（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 MeritModernityFromClosed | r1（612 行）・資本家（616 行 ⑨） |
| S12-H003 | S12 | 階級の上昇①：下層→専門職 | 文書 L2059（粒＝見出し・未分割） | Coefficients.cs:946:MinorityPromotionPenalty;Coefficients.cs:1300:PromotionGateOwnSchooling;Coefficients.cs:814:PromotionPremium;Coefficients.cs:813:ReligiousProfessionShare | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PromotionSpeed | r1（612 行）・資本家（616 行 ⑨） |
| S12-H004 | S12 | 階級の上昇①*：労働者→小営業（職人・商人） | 文書 L2064（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PettyEntryRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H005 | S12 | 手工業の没落①'：職人→都市労働者 | 文書 L2069（粒＝見出し・未分割） | Coefficients.cs:3343:GuildMobilityDrag | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ArtisanDeclineRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H006 | S12 | 失業プール①''：職探し | 文書 L2074（粒＝見出し・未分割） | Coefficients.cs:817:JobPoolTemperature | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 JobSearchRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H007 | S12 | 職探しの効率は識字で決まる | 文書 L2079（粒＝見出し・未分割） | Coefficients.cs:829:JobSearchLiteracyWeight | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H008 | S12 | 慢性失業だけが職を変える | 文書 L2084（粒＝見出し・未分割） | Coefficients.cs:830:JobSearchUsesEma | EconomyStep.cs Meritocracy.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H009 | S12 | 役務専任の階級にも失業が立つ | 文書 L2089（粒＝見出し・未分割） | Coefficients.cs:828:ServiceUnemployment | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H010 | S12 | 農奴は都市へ出られない | 文書 L2094（粒＝見出し・未分割） | Coefficients.cs:827:SerfMobilityGate | Colonial.cs Meritocracy.cs PopDecision.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H011 | S12 | 農奴の栓を失業プールにも | 文書 L2099（粒＝見出し・未分割） | Coefficients.cs:826:SerfGateCoversPool | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H012 | S12 | 階級の上昇②：下層→聖職者 | 文書 L2104（粒＝見出し・未分割） | Coefficients.cs:745:ClergyAttractThreshold | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ClergyJoinRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H013 | S12 | 世俗化の退出②''：聖職者→教師・都市労働者 | 文書 L2109（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ClergyExitRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H014 | S12 | 階級の上昇と転落のはしご | 文書 L2114（粒＝見出し・未分割） | Coefficients.cs:715:CapitalRiseThreshold;Coefficients.cs:677:ClergyFloorShare;Coefficients.cs:723:InsolventThreshold;Coefficients.cs:719:MiddleFallThreshold | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BankruptcyFallRate・CapitalRiseRate・MiddleFallRate・UpperFallRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H015 | S12 | 転落の物差し（重い資産・年所得・深さ） | 文書 L2119（粒＝見出し・未分割） | Coefficients.cs:833:FallAnchorReform | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H016 | S12 | 商業・手工業の資本が産業資本になる | 文書 L2124（粒＝見出し・未分割） | Coefficients.cs:819:MerchantArtisanCapitalRise | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H017 | S12 | ガラスの天井は時代とともに畳まれる | 文書 L2129（粒＝見出し・未分割） | Coefficients.cs:831:MinorityPenaltyModernBlend | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |
| S12-H018 | S12 | 職業の性別 | 文書 L2134（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ProfessionGenderGate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H019 | S12 | 昇格するのは識字の高い者（頭脳流出） | 文書 L2139（粒＝見出し・未分割） | Coefficients.cs:2676:PromotionLiteracySelection | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MigrationCarriesHumanCapital | r1（612 行）・資本家（616 行 ⑨） |
| S12-H020 | S12 | 世代ゲート（移動は入職時に起きる） | 文書 L2144（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 CohortGateK | r1（612 行）・資本家（616 行 ⑨） |
| S12-H021 | S12 | 農村識字の直行便 | 文書 L2149（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 RuralPromotionRate | r1（612 行）・資本家（616 行 ⑨） |
| S12-H022 | S12 | 職業訓練と奨学金は存在しない | 文書 L2154（粒＝見出し・未分割） | — | Meritocracy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | r1（612 行）・資本家（616 行 ⑨） |

### S13 移住と都市化と民族

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S13-H003 | S13 | 都市化（農村と都市の行き来） | 文書 L2158（粒＝見出し・未分割） | Coefficients.cs:126:MigrationTemperature;Coefficients.cs:125:Mobility | PopDecision.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BaseUrbanCap・UrbanPerInd | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H004 | S13 | 都市化の天井は建設時間だけ遅れる | 文書 L2163（粒＝見出し・未分割） | — | Meritocracy.cs PopDecision.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 UrbanCapFollowSpeed | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H005 | S13 | 機会指数（すべての移民の共通の物差し） | 文書 L2168（粒＝見出し・未分割） | Coefficients.cs:2028:CrimePushWeight;Coefficients.cs:2072:HousingMigrationFriction;Coefficients.cs:1234:OpportunityWageWeight;Coefficients.cs:2314:PushUnrestWeight | Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H006 | S13 | 犯罪と治安 | 文書 L2173（粒＝見出し・未分割） | Coefficients.cs:485:CrimeBase;Coefficients.cs:490:CrimeMilitancyWeight;Coefficients.cs:484:CrimeOrderPerPop;Coefficients.cs:491:CrimeUrbanWeight | Crime.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CrimeSatPenalty・CrimeWealthFearWeight | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H007 | S13 | 犯罪が人を押し出す | 文書 L2178（粒＝見出し・未分割） | Coefficients.cs:2028:CrimePushWeight | Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H008 | S13 | 州と州の移民、辺境の解錠 | 文書 L2183（粒＝見出し・未分割） | Coefficients.cs:3490:EraMigrationScale;Coefficients.cs:1308:ImmigrationAbsorbExponent;Coefficients.cs:1033:LandDiminishExponent;Coefficients.cs:2683:MigrationDistanceScale | Territory.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない）；消えた係数 ForceUnlockMilitancy・ForceUnlockRate・ForceUnlockTreasuryCost・FrontierDevelopmentRate・FrontierUnlockThreshold・ImmigrationReceiveCapFrac・RegionMigrationExcludesClosed | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H009 | S13 | 移民は複数の行き先へ散る | 文書 L2189（粒＝見出し・未分割） | — | Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MigrationSecondDestShare | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H010 | S13 | 非有限値の遮断 | 文書 L2194（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H011 | S13 | 国際移民（世界プール方式）（廃止の見出し） | 文書 L2198（粒＝見出し・未分割） | Coefficients.cs:942:DiscriminationLiteracy;Coefficients.cs:925:EthnicDraw | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 ImmigrantLiteracyCarry・ImmigrationReceiveCapFrac・MigrationStayHome・WorldMigrationPoolFrac | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H012 | S13 | 越境移民（貧しい国から豊かな国へ） | 文書 L2204（粒＝見出し・未分割） | Coefficients.cs:2680:CrossBorderOppGapThreshold;Coefficients.cs:942:DiscriminationLiteracy;Coefficients.cs:749:EmigrationRate;Coefficients.cs:925:EthnicDraw | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CrossBorderMigrationByOpportunity・EmigrationMaxFrac・ImmigrationReceiveCapFrac | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H013 | S13 | 距離は時代とともに縮む | 文書 L2209（粒＝見出し・未分割） | Coefficients.cs:3490:EraMigrationScale | Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H014 | S13 | 移民は自分で止まる | 文書 L2214（粒＝見出し・未分割） | Coefficients.cs:1033:LandDiminishExponent | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EmigrationFloorSize・EmigrationFloorSpan | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H015 | S13 | 移民は人的資本と年齢構成を持って行く | 文書 L2219（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;Model.cs:399:Blend;MultiNationSim.cs:1807:MoveAbroad | Model.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MigrantCarriesConsciousness・MigrationCarriesHumanCapital | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H016 | S13 | 渡航運賃と貧困の罠 | 文書 L2225（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 MigrationFarePerkKm | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H017 | S13 | ゲーム外から来る移民（廃止の見出し） | 文書 L2231（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 ExternalImmigrantProfile・ImmigrantProfileChildShare・ImmigrantProfileFemaleShare・ImmigrantProfileLiteracy・ImmigrantWorkerBase・ImmigrantWorkerPerInd・MigrantDowryWorldBase | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H018 | S13 | 技師の招聘（国庫で頭脳を買う） | 文書 L2237（粒＝見出し・未分割） | Coefficients.cs:198:ModernizationToRecruit;Coefficients.cs:199:RecruitCostPerHead;Coefficients.cs:201:RecruitMaxFracOfSource;Coefficients.cs:200:RecruitPremiumScale | MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RecruitDistanceScale | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H019 | S13 | 技術者の出国禁止法 | 文書 L2242（粒＝見出し・未分割） | Coefficients.cs:2700:SpecialistExitBanPrefScale | Government.cs MultiNationSim.cs PolicyPreferences.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H020 | S13 | 民族の希釈と同化 | 文書 L2247（粒＝見出し・未分割） | Coefficients.cs:941:AssimilationEducationBonus;Coefficients.cs:1969:AssimilationLiteracyResist;Coefficients.cs:940:EthnicAssimilation | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |
| S13-H021 | S13 | 民族の復興（同化の逆流） | 文書 L2252（粒＝見出し・未分割） | Coefficients.cs:1977:AssimilationRevival | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | rd48 R-c2（612 行）・移民の旗（616 行 ⑦） |

### S10 人口動態と暮らし

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S10-H033 | S10 | 住宅（都市の家賃と過密） | 文書 L2257（粒＝見出し・未分割） | Coefficients.cs:2072:HousingMigrationFriction;Coefficients.cs:2070:HousingNeedPerUrban;Coefficients.cs:2071:HousingUrbanThreshold | EconomyStep.cs PopDecision.cs PopTables.cs Simulation.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H034 | S10 | 衛生インフラ | 文書 L2262（粒＝見出し・未分割） | Coefficients.cs:2080:ElecInfraSanitationWeight;Coefficients.cs:2060:SanitationCapacityWeight;Coefficients.cs:2073:SoapSanitationWeight | EconomyStep.cs Model.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H035 | S10 | 医療の需要と健康保険 | 文書 L2267（粒＝見出し・未分割） | Coefficients.cs:2068:HealthCrowdingBoost;Coefficients.cs:2063:InsuranceCoverageMax;Coefficients.cs:2065:MedicalWelfareBoost | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H036 | S10 | 年金と高齢化 | 文書 L2272（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PensionCostRate | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H037 | S10 | 自給（市場を通さない食料） | 文書 L2277（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない）；消えた係数 SubsistenceHarvestFullExposure | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H038 | S10 | 囲い込みが自給を削る | 文書 L2282（粒＝見出し・未分割） | Simulation.cs:53:World | EconomyStep.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 EnclosureRate | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H039 | S10 | 新聞とラジオの到達 | 文書 L2287（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H040 | S10 | 出生率と死亡率の分離 | 文書 L2291（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FertilityAffectsBirthsOnly | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H041 | S10 | 医療の効きには実体が要る | 文書 L2296（粒＝見出し・未分割） | Coefficients.cs:2027:HealthcareRealGate | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H042 | S10 | 失業は所得の外側でも怒りを生む（廃止の見出し） | 文書 L2301（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H043 | S10 | 不満の要因内訳 | 文書 L2305（粒＝見出し・未分割） | — | Model.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H044 | S10 | 飢えは怒りになる | 文書 L2309（粒＝見出し・未分割） | Coefficients.cs:302:FamineDriveGain;Coefficients.cs:303:FamineDropOnset;Coefficients.cs:221:LivingRefSpeed | Model.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H045 | S10 | 高等教育と研究者（大学） | 文書 L2314（粒＝見出し・未分割） | Coefficients.cs:175:HigherEduNeedPerEducated;Coefficients.cs:186:ModernizationToHigherEdu;Coefficients.cs:176:PromotionGateHigherEdu;Coefficients.cs:172:ResearcherFrac | Catalog.cs EconomyStep.cs Meritocracy.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H046 | S10 | 高等教育ゲートの多年平均 | 文書 L2319（粒＝見出し・未分割） | — | Meritocracy.cs TechEducation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 EducationEmaRate・EducationGateUsesEma | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |
| S10-H047 | S10 | 新生児による識字の希釈 | 文書 L2324（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 NewbornLiteracyDilution | 死の側の段3（616 行 ⑥）・出生・医療（616 行 ⑨）・上下水道の材化（616 行 ⑧） |

### S14 不満と思想

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S14-H001 | S14 | 政治意識 | 文書 L2401（粒＝見出し・未分割） | Coefficients.cs:210:ConsciousnessSpeed;Coefficients.cs:212:NewsConsciousnessFloor;Coefficients.cs:211:UrbanConsciousnessBoost | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H002 | S14 | 便乗（バンドワゴン） | 文書 L2407（粒＝見出し・未分割） | Coefficients.cs:380:BandwagonSensitivity;Coefficients.cs:381:BandwagonWeight | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H003 | S14 | 闘争度の5つの成分 | 文書 L2413（粒＝見出し・未分割） | Coefficients.cs:230:MilitancySpeed;Coefficients.cs:308:PoliticalDistanceWeight;Coefficients.cs:306:WantWeight;Coefficients.cs:673:WarWearinessMilitancy | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H004 | S14 | 不満度（導出量・画面の値） | 文書 L2419（粒＝見出し・未分割） | Politics.cs:100:LivingWant;Politics.cs:114:FamineDrive;Politics.cs:126:EffectiveVoice | Politics.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H005 | S14 | 運動への参加を不満度で決める（参加の述語） | 文書 L2424（粒＝見出し・未分割） | Coefficients.cs:323:ParticipationDiscontent | FactionSnapshot.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H006 | S14 | 法の隔たりを「現に動かせる法」で測る | 文書 L2429（粒＝見出し・未分割） | Coefficients.cs:313:DiscontentLawGap | FactionSnapshot.cs PolicyPreferences.cs Politics.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H007 | S14 | 生活の不満は「失望」から生まれる | 文書 L2434（粒＝見出し・未分割） | Coefficients.cs:221:LivingRefSpeed | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LivingRefWeight | prdd・rd47 R4（612 行） |
| S14-H008 | S14 | 政治的な疎外（4つの軸で測る） | 文書 L2439（粒＝見出し・未分割） | Politics.cs:126:EffectiveVoice | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H009 | S14 | 3層分割（文盲・意識のある識字層・中間） | 文書 L2443（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs PoliticalQuantities.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MiddleLayerMilitancy | prdd・rd47 R4（612 行） |
| S14-H010 | S14 | 闘争度による手段の上書き | 文書 L2449（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 DesperationMilitancyOverride | prdd・rd47 R4（612 行） |
| S14-H011 | S14 | 3層の振る舞い分け | 文書 L2454（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 DesperationLayerBlend | prdd・rd47 R4（612 行） |
| S14-H012 | S14 | 秘密投票と「紙の参政権」 | 文書 L2459（粒＝見出し・未分割）；同じ題 L3773（S19） | Coefficients.cs:3167:OpenBallotReality;Coefficients.cs:3168:PaperFranchiseReality | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SecretBallotGate | prdd・rd47 R4（612 行） |
| S14-H013 | S14 | 失業の怒りと失業保険（廃止の見出し） | 文書 L2465（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H014 | S14 | 格差の不満（ジニ係数）（廃止の見出し） | 文書 L2470（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs Politics.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H015 | S14 | 民族の不満（一本鎖） | 文書 L2475（粒＝見出し・未分割） | Coefficients.cs:947:MinorityMilitancy | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H016 | S14 | 飢えの絶対項（欠けているパン）（廃止の見出し） | 文書 L2481（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs Politics.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H017 | S14 | 11の思想が経済から生まれる | 文書 L2485（粒＝見出し・未分割） | Diplomacy.cs:1155:Ideology;Ideology.cs:16:UpdateShares;Ideology.cs:160:BaseWeights | Diplomacy.cs Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H018 | S14 | 貴族の思想は所得の構成から出る | 文書 L2489（粒＝見出し・未分割） | Coefficients.cs:2813:NoblePrivilegeWeight | Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NobleWeightsFromIncome | prdd・rd47 R4（612 行） |
| S14-H019 | S14 | 王党派（政治意識のある王権支持者） | 文書 L2495（粒＝見出し・未分割） | Diplomacy.cs:1155:Ideology | Diplomacy.cs Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RoyalistGate | prdd・rd47 R4（612 行） |
| S14-H020 | S14 | 反動の担い手を「地位の喪失」から | 文書 L2500（粒＝見出し・未分割） | Coefficients.cs:367:ReactionFromLoss | Ideology.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H021 | S14 | 敗戦・失地・戦勝・外圧がナショナリズムを噴出させる | 文書 L2505（粒＝見出し・未分割） | Coefficients.cs:1742:DefeatNationalism;Coefficients.cs:1765:ExternalPressureNationalism;Coefficients.cs:1743:LostLandNationalism;Coefficients.cs:1760:VictoryImperialism | Ideology.cs PoliticalQuantities.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LostLandHalflifeQuarters | prdd・rd47 R4（612 行） |
| S14-H022 | S14 | 国民意識からのナショナリズムの底上げ | 文書 L2510（粒＝見出し・未分割） | Diplomacy.cs:1155:Ideology | Diplomacy.cs Ideology.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 NationalismFromConsciousness | prdd・rd47 R4（612 行） |
| S14-H023 | S14 | 恐慌が両極化させる | 文書 L2515（粒＝見出し・未分割） | Diplomacy.cs:1155:Ideology | Diplomacy.cs Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DepressionCommunism・DepressionReaction・DepressionSocialism | prdd・rd47 R4（612 行） |
| S14-H024 | S14 | 飢えの記憶 | 文書 L2521（粒＝見出し・未分割）；同じ題 L6583（S28） | Coefficients.cs:3130:DynMandateFoodAnchor;Coefficients.cs:1762:FamineRepublicanism;Coefficients.cs:1763:FamineSocialism | Ideology.cs Politics.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FamineMemoryDecay | prdd・rd47 R4（612 行） |
| S14-H025 | S14 | 後の時代の思想の解禁（発展度ゲート→2026-10-01 (152) 発見の2値） | 文書 L2527（粒＝見出し・未分割） | Diplomacy.cs:1155:Ideology;Ideology.cs:127:EraGates | Diplomacy.cs Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IdeologyGateFromDevelopment・IdeologyGateIndDynamic・IdeologyGateSpan・IdeologyGateUrbanWeight | prdd・rd47 R4（612 行） |
| S14-H026 | S14 | 思想の到達（商人と本と留学生） | 文書 L2533（粒＝見出し・未分割） | Coefficients.cs:1902:DiscoveryContactHalf | Discovery.cs Ideology.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IdeologyReachGate | prdd・rd47 R4（612 行） |
| S14-H027 | S14 | ファシズムの結晶（発展度 × 危機） | 文書 L2539（粒＝見出し・未分割） | Coefficients.cs:2874:FascismCrisisLeftRef;Coefficients.cs:2873:FascismCrisisSlumpRef;Coefficients.cs:3272:FascismGate | Ideology.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FascismCrisisWeight・FascismGateFromDevelopment・IdeologyGateFascOnset | prdd・rd47 R4（612 行） |
| S14-H028 | S14 | 公約が果たされると思想が伸びる | 文書 L2545（粒＝見出し・未分割） | Coefficients.cs:1781:ManifestoCreditDecay;Coefficients.cs:1780:ManifestoFulfillCredit | Ideology.cs Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H029 | S14 | 政体と思想の親和（4軸の制度座標） | 文書 L2550（粒＝見出し・未分割） | Coefficients.cs:371:IdeologyAffinitySlope | CrownParliament.cs Ideology.cs PlayText.cs PoliticalQuantities.cs Politics.cs PowerAffinity.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H030 | S14 | 国民の思想の集計と発言権 | 文書 L2555（粒＝見出し・未分割） | Coefficients.cs:342:IdeologyVoiceFloor | Ideology.cs PowerAffinity.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H031 | S14 | 現政体の座標は実際の参政権へずれる | 文書 L2560（粒＝見出し・未分割） | PoliticalQuantities.cs:275:DriftedPolityCoord | PoliticalQuantities.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H032 | S14 | 利益集団3ブロックの力 | 文書 L2564（粒＝見出し・未分割）；同じ題 L3326（S18） | — | InterestGroups.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H033 | S14 | 第4の束「小営業」（プチブル） | 文書 L2568（粒＝見出し・未分割） | — | InterestGroups.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 PettyInterestGate | prdd・rd47 R4（612 行） |
| S14-H034 | S14 | 改革派と急進派の動員力 | 文書 L2573（粒＝見出し・未分割） | Coefficients.cs:373:ReformPressureBoost | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H035 | S14 | 組合の政治動員 | 文書 L2578（粒＝見出し・未分割） | Coefficients.cs:808:UnionMobilization | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H036 | S14 | 窮乏のチャネル（パンの蜂起） | 文書 L2583（粒＝見出し・未分割） | Coefficients.cs:411:DesperationChallengeWeight;Coefficients.cs:412:DesperationConsciousnessGate;Coefficients.cs:410:RiotMilitancyThreshold | PolicyPreferences.cs Politics.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |
| S14-H037 | S14 | 経済危機の物差し（大衆の平均満足度） | 文書 L2588（粒＝見出し・未分割） | Simulation.cs:53:World;World.cs:887:MeanSatisfactionPol;Politics.cs:1890:IsMassPop | Politics.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | prdd・rd47 R4（612 行） |

### S15 正統性と政体の遷移

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S15-H001 | S15 | 正統性（政府の体力） | 文書 L2593（粒＝見出し・未分割） | Coefficients.cs:1560:DefeatShockLegitWeight;Coefficients.cs:376:LegitimacyErosionRate;Coefficients.cs:2717:LegitimacyRegenRate;Coefficients.cs:377:RepresentationThreshold | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CorruptionLegitimacyErosion | — |
| S15-H002 | S15 | 議会による正統性の侵食 | 文書 L2599（粒＝見出し・未分割） | Coefficients.cs:246:ConstMonarchyErosion | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H003 | S15 | 一党支配の私物化による侵食 | 文書 L2605（粒＝見出し・未分割） | Coefficients.cs:245:DominanceInstabilitySatisfaction;Coefficients.cs:235:DominanceLegitimacyErosion;Coefficients.cs:234:PartyDominanceThreshold | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H004 | S15 | 政治資本（残り／最大）＝反対を押し切る唯一の門（作者裁定S1/S2・2026-09-21 ㉖で既定化） | 文書 L2611（粒＝見出し・未分割） | Coefficients.cs:2930:MandateRefillQuarters;Coefficients.cs:2912:MandateSpendRate;World.cs:815:MandateSpent;Coefficients.cs:2910:MandateWeight | CrownParliament.cs Government.cs PlayText.cs PoliticalQuantities.cs Politics.cs SelfPlay.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H005 | S15 | 憲法級の法（政治資本では押し切れない） | 文書 L2620（粒＝見出し・未分割） | Government.cs:478:MandateMargin;Government.cs:492:IsConstitutional | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConstitutionalMandateScale | — |
| S15-H006 | S15 | 押し切り遺恨と阻止された要求（廃止の見出し） | 文書 L2626（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 Grievance・ReversalAnger | — |
| S15-H007 | S15 | 政治予算（同時に押せる戦線の数） | 文書 L2631（粒＝見出し・未分割） | Coefficients.cs:1737:PoliticalBudgetGate | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H008 | S15 | 天命 | 文書 L2637（粒＝見出し・未分割） | Coefficients.cs:3136:DynMandateAmplify;Coefficients.cs:3132:DynMandateEmaRate;Coefficients.cs:3129:DynMandateFoodWeight;Coefficients.cs:3120:DynMandateSatWeight | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DynMandateGraftWeight・DynasticMandateEnabled | — |
| S15-H009 | S15 | 天命の宗教媒介 | 文書 L2643（粒＝見出し・未分割） | Coefficients.cs:3134:ClergyMediationPerCapita;Coefficients.cs:3133:MandateReligiousCircuit | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H010 | S15 | 信任帝政の正統性の床 | 文書 L2649（粒＝見出し・未分割） | Coefficients.cs:3099:RedefinedMandateFloor;Coefficients.cs:3101:RedefinedMandateMandateMin;Coefficients.cs:3100:RedefinedMandateNcMin | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RedefinedMandateEnabled | — |
| S15-H011 | S15 | 制度疲労 | 文書 L2655（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DecayLockinIndustrialization・DecayReactionRegrowth・DecayShockCooldownQuarters・DecayShockRelief | — |
| S15-H012 | S15 | 制度疲労のショックは何度でも開く | 文書 L2662（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DecayShockRepeatable | — |
| S15-H013 | S15 | 治安維持力と戦時の消耗 | 文書 L2668（粒＝見出し・未分割） | Coefficients.cs:911:CoercionMax;Coefficients.cs:910:CoercionPerSoldier;Coefficients.cs:209:PoliticalReferencePop;Coefficients.cs:909:WarCoercionDrain | FactionSnapshot.cs Government.cs PoliticalQuantities.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H014 | S15 | 戦時の結束（旗の下に集まる） | 文書 L2674（粒＝見出し・未分割） | Coefficients.cs:1745:WarRallyCoercion;Coefficients.cs:1746:WarRallyDecayPoint | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H015 | S15 | 政体の遷移 | 文書 L2680（粒＝見出し・未分割） | Coefficients.cs:405:PolityInertia | CrownParliament.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PolityGraphRetired | — |
| S15-H016 | S15 | 軍事クーデタ | 文書 L2686（粒＝見出し・未分割） | Coefficients.cs:2489:MaxSoldierFraction | Military.cs MultiNationSim.cs Politics.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MilitaryCoupSupport・MilitaryCoupWeight | — |
| S15-H017 | S15 | 一党支配から独裁へ | 文書 L2692（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DominanceCoupWeight | — |
| S15-H018 | S15 | ブルジョア独裁の引き金 | 文書 L2698（粒＝見出し・未分割） | Coefficients.cs:2848:BourgeoisCoupWeight | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BourgeoisCoupNeedsCrisis | — |
| S15-H019 | S15 | 体制の実績ストック（信認と失望） | 文書 L2704（粒＝見出し・未分割） | Coefficients.cs:3086:RegimeRecordAccumRate;Coefficients.cs:3088:RegimeRecordDecay;Coefficients.cs:3087:RegimeRecordSuccessTenureGate;Coefficients.cs:3089:RegimeTrackRecordWeight | CrownParliament.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H020 | S15 | 政権への失望（直近の政変の数） | 文書 L2710（粒＝見出し・未分割） | World.cs:848:RecentRegimeChanges | CrownParliament.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H021 | S15 | 上からの改革（明治型・欽定憲法） | 文書 L2715（粒＝見出し・未分割） | Coefficients.cs:2962:GrantedControlOnReform;Coefficients.cs:1867:ReformAnticipationWeight;Coefficients.cs:2719:ReformFromAboveLegitThreshold;Coefficients.cs:1871:ReformGrantPcWidth | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ReformFromAboveEnabled | — |
| S15-H022 | S15 | 建国時の制度と自由化のラチェット | 文書 L2720（粒＝見出し・未分割）；同じ題 L3690（S19） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SuffrageRatchetOnLiberalize | — |
| S15-H023 | S15 | 政変は法の梯子を1段ずつしか動かさない | 文書 L2726（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StagePrereq | — |
| S15-H024 | S15 | 革命は土地権力を壊す | 文書 L2731（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RevolutionLandReform | — |
| S15-H025 | S15 | 探究の自由（法から創発） | 文書 L2737（粒＝見出し・未分割） | — | Politics.cs TechEducation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H026 | S15 | 革命による債務の否認 | 文書 L2742（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DefaultCooldownQuarters・RepudiationCreditPenalty | — |
| S15-H027 | S15 | 失脚と恐怖政治 | 文書 L2748（粒＝見出し・未分割） | Coefficients.cs:378:ReformMilitancyRelief;Coefficients.cs:384:RevolutionTerror | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DeposedMilitancySpike | — |
| S15-H028 | S15 | 国民意識（育つものではなく、買うもの） | 文書 L2754（粒＝見出し・未分割） | Coefficients.cs:3137:NationBuildAccumRate;Coefficients.cs:3092:NationBuildConscript;Coefficients.cs:3140:NationBuildCrisisCivilWar;Coefficients.cs:3139:NationBuildCrisisDefense | CrownParliament.cs Fiscal.cs Military.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NationBuildCivicFloor | — |
| S15-H029 | S15 | 国民意識の導出化 | 文書 L2777（粒＝見出し・未分割） | Coefficients.cs:3153:NationConsciousnessDerived | CrownParliament.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S15-H030 | S15 | 市民的開放度（「国民」の定義） | 文書 L2782（粒＝見出し・未分割） | — | CrownParliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CivicOpennessEnabled・MigrantCarriesConsciousness | — |
| S15-H031 | S15 | 革命が国民を目覚めさせる | 文書 L2787（粒＝見出し・未分割） | Coefficients.cs:1726:RevolutionConsciousness | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S15-H032 | S15 | 断裂の窓（反動が法を巻き戻せる期間） | 文書 L2792（粒＝見出し・未分割） | Coefficients.cs:1729:RuptureWindowQuartersOnCrush | Government.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |

### S27 支配度と分離独立と領土の移転

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S27-H001 | S27 | 支配度（州ごとのストック） | 文書 L2862（粒＝見出し・未分割） | Coefficients.cs:2752:DominanceSpeed | Diplomacy.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 DominanceIsStock・RuleSharesEnabled | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H002 | S27 | 分離独立の圧力 | 文書 L2868（粒＝見出し・未分割） | Coefficients.cs:699:ColonyPromotionMinPop;Coefficients.cs:393:CultureSecessionWeight;Coefficients.cs:2725:SecessionCultureAdditive;Coefficients.cs:2724:SecessionHoldCoercion | NationBrief.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H003 | S27 | 保持力は「軍が届く分だけ」効く（投射） | 文書 L2874（粒＝見出し・未分割） | Coefficients.cs:2737:SecessionHoldProjection | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H004 | S27 | 同時に相手をしている運動の数で軍が割れる | 文書 L2880（粒＝見出し・未分割） | Coefficients.cs:2736:SecessionHoldSharedArmy | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H005 | S27 | 文化距離を民族の実体から出す | 文書 L2885（粒＝見出し・未分割） | Coefficients.cs:2733:CultureDistanceFromEthnic | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H006 | S27 | 国民意識が育った州は離れない | 文書 L2891（粒＝見出し・未分割） | Coefficients.cs:3508:SecessionConsciousnessWeight | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H007 | S27 | 同時独立（分離独立の戦争） | 文書 L2901（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 SecessionAsWar | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H008 | S27 | 分離独立で新しい国が生まれる | 文書 L2906（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:282:SpawnSecededNations;MultiNationSim.cs:340:SpawnNation | MultiNationSim.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 SecessionSpawnsNation | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |

### S17 王権と議会

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S17-H001 | S17 | 政体ラベルの導出 | 文書 L3056（粒＝見出し・未分割） | Coefficients.cs:3361:ConstitutionalControlExit;Coefficients.cs:3360:ConstitutionalControlFloor | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownlessCollapseKeepsRepublic・PolityLabelFromAxes | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H002 | S17 | 王権の登録簿 | 文書 L3062（粒＝見出し・未分割） | CrownParliament.cs:441:ApplyRegime | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H003 | S17 | 議会圧力・王権圧力・共和廃位 | 文書 L3066（粒＝見出し・未分割） | Coefficients.cs:3075:ParliamentPressureRate | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownPressureRate・RepublicanDeposition | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H004 | S17 | 王権の形の変異（幕府の終わり・官僚絶対主義） | 文書 L3071（粒＝見出し・未分割） | Coefficients.cs:3082:ShogunateEndMilitancy;Coefficients.cs:3081:ShogunateEndTension | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownFormMutation・DespoticToAbsolutistAdmin・DespoticToAbsolutistQuarters | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H005 | S17 | 軍が議会統制を押し戻す | 文書 L3077（粒＝見出し・未分割） | Coefficients.cs:1739:ArmyPcPressure | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H006 | S17 | 軍の統帥権 | 文書 L3083（粒＝見出し・未分割） | Coefficients.cs:2958:ArmyMilitarismAffinity | CrownParliament.cs PoliticalQuantities.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H007 | S17 | 破産による議会召集 | 文書 L3088（粒＝見出し・未分割）；同じ題 L3701（S17） | Coefficients.cs:2960:SummonParliamentRate | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H008 | S17 | ストルイピン後退（王権の巻き戻し） | 文書 L3094（粒＝見出し・未分割） | — | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownRollbackRate・CrownlessCollapseKeepsRepublic | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H009 | S17 | 軍帰属と協力度 | 文書 L3100（粒＝見出し・未分割） | Coefficients.cs:2954:CoopClassAffinityWeight;Coefficients.cs:2956:CoopConflictMemoryWeight;Coefficients.cs:2955:CoopPolicyGapWeight | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H010 | S17 | 世論と体制の協力度 | 文書 L3106（粒＝見出し・未分割） | Coefficients.cs:2953:CoopPublicSatPivot | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H011 | S17 | 革命の波（国際伝播）（廃止の見出し） | 文書 L3111（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ~（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H012 | S17 | 矯正講和（政体の押し付け） | 文書 L3116（粒＝見出し・未分割） | Coefficients.cs:2018:ImposedRegimeLegitimacy | Diplomacy.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 ImposeRegimeEnabled | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H013 | S17 | 議会の講和批准 | 文書 L3122（粒＝見出し・未分割） | Coefficients.cs:1787:TreatyRatificationWeight | Diplomacy.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H014 | S17 | 就学履歴のストック | 文書 L3127（粒＝見出し・未分割） | Coefficients.cs:2726:ConsciousnessFromSchoolingStock | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H015 | S17 | 伝統教義の近代化ゲート（黒船動学）（廃止の見出し） | 文書 L3132（粒＝見出し・未分割） | — | — | — | — | この波の線が埋める (889) c75cp | 文書の記号 ~（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H016 | S17 | 非選挙政体の綱領免除（廃止の見出し） | 文書 L3137（粒＝見出し・未分割） | — | — | — | — | この波の線が埋める (889) c75cp | 文書の記号 ~（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H017 | S17 | 政治量の単一実装 | 文書 L3142（粒＝見出し・未分割） | — | PoliticalQuantities.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H018 | S17 | 不満の内訳の診断 | 文書 L3147（粒＝見出し・未分割） | — | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H019 | S17 | 説明API | 文書 L3151（粒＝見出し・未分割） | World.cs:802:Explain | Explain.cs World.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H020 | S17 | 国内勢力のスナップショット | 文書 L3155（粒＝見出し・未分割） | — | FactionSnapshot.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H021 | S17 | 分離リスクの可視化（廃止の見出し） | 文書 L3159（粒＝見出し・未分割） | NationBrief.cs:81:SecessionHold | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ~（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H022 | S17 | 権力と政体の表示 | 文書 L3163（粒＝見出し・未分割） | — | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H023 | S17 | 恐怖政治と政変の記録 | 文書 L3167（粒＝見出し・未分割） | — | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H024 | S17 | 勢力強度の実測 | 文書 L3171（粒＝見出し・未分割） | — | — | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |

### S18 選挙と政党と利益集団

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S18-H001 | S18 | 得票の算出 | 文書 L3230（粒＝見出し・未分割） | Parliament.cs:176:ComputeVotes | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H002 | S18 | 参政権のゲート | 文書 L3234（粒＝見出し・未分割） | Coefficients.cs:341:ModernEliteSubCensusVoice | InterestGroups.cs Parliament.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H003 | S18 | 選挙（4年周期・第一党が組閣） | 文書 L3239（粒＝見出し・未分割） | Coefficients.cs:232:ElectionIntervalQuarters | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H004 | S18 | 不信任と繰り上げ選挙 | 文書 L3245（粒＝見出し・未分割） | Coefficients.cs:1785:EarlyElectionMinQuarters;Coefficients.cs:1784:NoConfidenceThreshold | Parliament.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EarlyElectionEnabled | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H005 | S18 | 議会の解散 | 文書 L3251（粒＝見出し・未分割） | — | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H006 | S18 | 総選挙の公示と加担 | 文書 L3255（粒＝見出し・未分割） | Coefficients.cs:1775:ElectionPushHalfPerVoter;Coefficients.cs:1782:ElectionPushScale | Parliament.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ElectionEventEnabled | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H007 | S18 | 買収しやすさの富加重 | 文書 L3263（粒＝見出し・未分割） | Coefficients.cs:1777:ElectionBribePovertyWeight | World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H008 | S18 | マニフェスト（選挙の公約） | 文書 L3268（粒＝見出し・未分割） | Coefficients.cs:1781:ManifestoCreditDecay;Coefficients.cs:1780:ManifestoFulfillCredit;Coefficients.cs:1779:ManifestoFulfillMove | Ideology.cs Parliament.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H009 | S18 | 議席の凍結（レームダック） | 文書 L3276（粒＝見出し・未分割） | — | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SeatsFrozenBetweenElections | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H010 | S18 | 開院時の議席 | 文書 L3281（粒＝見出し・未分割） | — | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ParliamentSeatOnOpen | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H011 | S18 | 選挙制度の歪み | 文書 L3286（粒＝見出し・未分割） | Coefficients.cs:2833:ElectoralDistortion | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H012 | S18 | 3党の綱領（法軸だけ）（廃止の見出し） | 文書 L3291（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H013 | S18 | 少数与党は綱領を通せない（廃止の見出し） | 文書 L3297（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H014 | S18 | 連立相手への歩み寄り（廃止の見出し） | 文書 L3302（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H015 | S18 | 非選挙政体は綱領を持たない（廃止の見出し） | 文書 L3307（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H016 | S18 | 動的な政党の結党と解党 | 文書 L3311（粒＝見出し・未分割） | Coefficients.cs:3073:PartyFormCohesion | Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DynamicParties | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H017 | S18 | 政党は「族」と「出自」の2つを持つ | 文書 L3318（粒＝見出し・未分割） | Factions.cs:19:Ideo;FactionSnapshot.cs:31:Bloc | FactionSnapshot.cs Factions.cs Parliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H018 | S18 | 土地は旧支配層の政治力 | 文書 L3334（粒＝見出し・未分割） | Coefficients.cs:1090:LandedPowerCapitalization | InterestGroups.cs | — | — | この波の線が埋める (891) c75ds | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H019 | S18 | 政策の好みは経済から出る | 文書 L3341（粒＝見出し・未分割） | PolicyPreferences.cs:230:Pref | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H020 | S18 | 議席が法を動かす重み | 文書 L3347（粒＝見出し・未分割） | Coefficients.cs:247:ParliamentLawWeight | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H021 | S18 | 議会の二重票の是正 | 文書 L3352（粒＝見出し・未分割） | Coefficients.cs:3520:ParliamentStreetSupersede | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H022 | S18 | 参政権の梯子と革命の脅威 | 文書 L3358（粒＝見出し・未分割） | Coefficients.cs:340:SuffrageConcession | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H023 | S18 | 労働闘争による資本の譲歩 | 文書 L3363（粒＝見出し・未分割） | Coefficients.cs:2846:LaborMilitancyConcession | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LaborConcessionThreshold | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H024 | S18 | 国家の歳入要求（第4の主体） | 文書 L3368（粒＝見出し・未分割） | Coefficients.cs:2024:TariffStateRevenueWeight | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateRevenueAllTaxes | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H025 | S18 | 年金の高齢受益者 | 文書 L3374（粒＝見出し・未分割） | Coefficients.cs:2845:PensionOldWeight | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H026 | S18 | 州の関税利害と輸出利害 | 文書 L3379（粒＝見出し・未分割） | Coefficients.cs:1963:RentProtectionWeightCoef;Coefficients.cs:1957:TariffExportOppositionWeight;Coefficients.cs:2023:TariffInterestPrefWeight | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TariffRetaliationViaPolitics | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H027 | S18 | 統帥権（軍＝第4の勢力） | 文書 L3387（粒＝見出し・未分割） | Coefficients.cs:1738:ArmyPoliticalWeight | PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 地主の政治の導き直し C-1（612 行 ⑧） |
| S18-H028 | S18 | 小営業（第4の束） | 文書 L3394（粒＝見出し・未分割） | — | InterestGroups.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 PettyInterestGate | 地主の政治の導き直し C-1（612 行 ⑧） |

### S19 法の成立

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S19-H001 | S19 | 法は1期に1段ずつ | 文書 L3400（粒＝見出し・未分割） | Government.cs:979:StepLevel;Government.cs:75:LevelLaws | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H002 | S19 | 段の飛び越し禁止 | 文書 L3404（粒＝見出し・未分割） | Government.cs:760:StagedAxis | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StagePrereq | — |
| S19-H003 | S19 | 法の順序（前提となる法） | 文書 L3409（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H004 | S19 | 税・支出・軍事費は段の法（2026-09-20 点火） | 文書 L3415（粒＝見出し・未分割） | Government.cs:391:TaxStage;Government.cs:399:TaxValue;Government.cs:409:MilitaryStage | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H005 | S19 | 政治資本で押し切る | 文書 L3420（粒＝見出し・未分割） | Coefficients.cs:2912:MandateSpendRate;Coefficients.cs:2910:MandateWeight | Government.cs PoliticalQuantities.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H006 | S19 | 憲法級の法は政治資本で押せない | 文書 L3426（粒＝見出し・未分割） | Government.cs:492:IsConstitutional | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConstitutionalMandateScale | — |
| S19-H007 | S19 | 押し切りの代償 | 文書 L3433（粒＝見出し・未分割） | Coefficients.cs:2930:MandateRefillQuarters | Government.cs PlayText.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H008 | S19 | 押し切りの遺恨（廃止の見出し） | 文書 L3440（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | — |
| S19-H009 | S19 | 阻止された要求の不満（廃止の見出し） | 文書 L3445（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | — |
| S19-H010 | S19 | 政治予算（同時には押せない） | 文書 L3450（粒＝見出し・未分割） | Coefficients.cs:1737:PoliticalBudgetGate | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H011 | S19 | スライダーの政治ロックと速度 | 文書 L3457（粒＝見出し・未分割） | Coefficients.cs:506:PolicyBaseSpeed;Coefficients.cs:507:PolicyResistanceWeight | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H012 | S19 | 勅令は速い | 文書 L3462（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AutocraticPolicyAgility | — |
| S19-H013 | S19 | 予算闘争（議会の拒否権） | 文書 L3469（粒＝見出し・未分割） | Coefficients.cs:2963:ParliamentBudgetVeto | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ParliamentBudgetScope | — |
| S19-H014 | S19 | 法の不可逆性 | 文書 L3475（粒＝見出し・未分割） | Coefficients.cs:1727:IrreversibleLaws | Government.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IrreversibleEmancipation | — |
| S19-H015 | S19 | 特権を剥がしたときの怒り | 文書 L3483（粒＝見出し・未分割） | Coefficients.cs:3313:PrivilegeStripMilitancy | Government.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S19-H016 | S19 | 断裂の窓 | 文書 L3488（粒＝見出し・未分割） | Coefficients.cs:1729:RuptureWindowQuartersOnCrush | Government.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H017 | S19 | 勢力の要求と強制施行 | 文書 L3493（粒＝見出し・未分割） | Coefficients.cs:3066:FactionConcessionSteps;Coefficients.cs:2989:RevoltConcessionSpend | Parliament.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H018 | S19 | 法律タブのゲート照会 | 文書 L3501（粒＝見出し・未分割） | Government.cs:607:CanStep | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H019 | S19 | 政策軸の全一覧（38軸） | 文書 L3507（粒＝見出し・未分割） | Policy.cs:194:Antitrust;NationConfig.cs:65:Bureaucracy;Policy.cs:173:Censorship;NationConfig.cs:28:Coercion | Agents.cs BilateralMarket.cs CrownParliament.cs Diplomacy.cs EconomyStep.cs FactionSnapshot.cs Fiscal.cs FiscalCatalog.cs FiscalTables.cs GovernanceSnapshot.cs Government.cs HistoricalReference.cs Ideology.cs InterestGroups.cs InternationalMarket.cs LawCatalog.cs Meritocracy.cs Military.cs MultiNationSim.cs NationConfig.cs Parliament.cs PlayText.cs Policy.cs PolicyPreferences.cs PoliticalQuantities.cs Politics.cs PopTables.cs PowerAffinity.cs PowerShares.cs Privileges.cs ProblemScan.cs ProductionDiag.cs SelfPlay.cs Simulation.cs TechEducation.cs UnionBargaining.cs Validation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalMobility・ChildLabor・CivilianArms・IncomeTax・MilitaryExport・MonetaryStandard・Nationalization・NationalizationAll・OpiumExport・ShorterHours | — |
| S19-H020 | S19 | 穀物法・土地改革・農奴解放 | 文書 L3554（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H021 | S19 | 民間の武器購入の法 | 文書 L3566（粒＝見出し・未分割） | Coefficients.cs:3008:RebelArmsMarketEnabled | EconomyStep.cs Government.cs LawCatalog.cs Parliament.cs PolicyPreferences.cs Politics.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S19-H022 | S19 | 阿片の法2本 | 文書 L3571（粒＝見出し・未分割） | Coefficients.cs:2190:OpiumBanPrefScale;Coefficients.cs:2191:OpiumTradePrefScale | Contraband.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H023 | S19 | 児童労働法 | 文書 L3578（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H024 | S19 | 国家無神論 | 文書 L3582（粒＝見出し・未分割） | World.cs:236:Policy;Policy.cs:94:StateReligionLaw | Government.cs Policy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H025 | S19 | 国有化の5部門と『全て』 | 文書 L3588（粒＝見出し・未分割） | World.cs:236:Policy | Government.cs Policy.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NationalizationGate | — |
| S19-H026 | S19 | 通貨制度と資本移動の法 | 文書 L3595（粒＝見出し・未分割） | Coefficients.cs:3480:MonetaryPeerPullScale;Coefficients.cs:2696:MonetaryPrefScale | Government.cs MultiNationSim.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalMobilityLawGate・SilverStandardGate | — |
| S19-H027 | S19 | 海軍維持費の軸 | 文書 L3603（粒＝見出し・未分割） | SelfPlay.cs:1240:SetNavalUpkeepShare;Simulation.cs:62:Simulation | PolicyPreferences.cs Politics.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H028 | S19 | 参政権の範囲2法 | 文書 L3608（粒＝見出し・未分割） | InterestGroups.cs:79:SuffrageInclusion;World.cs:236:Policy | Fiscal.cs InterestGroups.cs Policy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SuffrageScopeGate・SuffrageScopeVoice | — |
| S19-H029 | S19 | 技術者流出禁止法 | 文書 L3615（粒＝見出し・未分割） | Coefficients.cs:2700:SpecialistExitBanPrefScale | Government.cs MultiNationSim.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S19-H030 | S19 | 同業組合法 | 文書 L3622（粒＝見出し・未分割） | Coefficients.cs:3339:GuildLawGate | EconomyStep.cs Government.cs LawCatalog.cs Meritocracy.cs Parliament.cs PolicyPreferences.cs Politics.cs Privileges.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |

### S17 王権と議会

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S17-H025 | S17 | 議会統制力 | 文書 L3627（粒＝見出し・未分割） | Coefficients.cs:3360:ConstitutionalControlFloor | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H026 | S17 | 予算法（誰が歳出を決めるか） | 文書 L3632（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Politics.cs PowerAffinity.cs Simulation.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H027 | S17 | 統帥法（誰が軍を指揮するか） | 文書 L3638（粒＝見出し・未分割） | Simulation.cs:62:Simulation | PowerAffinity.cs Simulation.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H028 | S17 | 権力の取り分（王権・議会・軍部・革命勢力） | 文書 L3644（粒＝見出し・未分割） | Coefficients.cs:2832:CoalitionClosenessFloor | Politics.cs PowerAffinity.cs SelfPlay.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H029 | S17 | 軍が統帥権を独占する条件 | 文書 L3653（粒＝見出し・未分割） | PowerAffinity.cs:316:ArmyHoldsCommand | Politics.cs PowerAffinity.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ◇（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H030 | S17 | 権力の表モデル（診断専用）（廃止の見出し） | 文書 L3660（粒＝見出し・未分割） | — | PowerShares.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ~（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |

### S19 法の成立

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S19-H031 | S19 | 条約の批准権 | 文書 L3666（粒＝見出し・未分割）；同じ題 L5696（S25） | Coefficients.cs:1787:TreatyRatificationWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H032 | S19 | 所得税の行政力上限（廃止の見出し） | 文書 L3671（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 IncomeTaxUnlocked | — |
| S19-H033 | S19 | 課税への同意 | 文書 L3676（粒＝見出し・未分割）；同じ題 L3897（S20） | Coefficients.cs:1848:TaxConsentWeight | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H034 | S19 | 債務緊縮のハード制約 | 文書 L3682（粒＝見出し・未分割） | Coefficients.cs:436:AusterityRange;Coefficients.cs:435:AusterityStartDebt;Coefficients.cs:437:AusterityTaxHike | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |

### S17 王権と議会

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S17-H031 | S17 | 床交差による政体の再ラベル | 文書 L3695（粒＝見出し・未分割） | Coefficients.cs:3361:ConstitutionalControlExit | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 PolityLabelFromAxes | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H032 | S17 | 王権の巻き戻し（ストルイピン後退） | 文書 L3706（粒＝見出し・未分割） | — | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownRollbackRate | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H033 | S17 | 議会圧力と王権圧力 | 文書 L3713（粒＝見出し・未分割） | Coefficients.cs:3075:ParliamentPressureRate;Coefficients.cs:405:PolityInertia | CrownParliament.cs Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownPressureRate | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H034 | S17 | 軍による押し戻し | 文書 L3718（粒＝見出し・未分割） | Coefficients.cs:1739:ArmyPcPressure | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H035 | S17 | 廃位 | 文書 L3724（粒＝見出し・未分割） | — | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 RepublicanDeposition | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H036 | S17 | 王権の形の変異 | 文書 L3729（粒＝見出し・未分割） | — | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 CrownFormMutation | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H037 | S17 | 下賜（欽定憲法） | 文書 L3737（粒＝見出し・未分割） | Coefficients.cs:2962:GrantedControlOnReform;Coefficients.cs:1871:ReformGrantPcWidth | Politics.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない）；消えた係数 ReformFromAboveEnabled | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |
| S17-H038 | S17 | 協力度と軍帰属 | 文書 L3743（粒＝見出し・未分割） | Coefficients.cs:2958:ArmyMilitarismAffinity;Coefficients.cs:2954:CoopClassAffinityWeight;Coefficients.cs:2956:CoopConflictMemoryWeight;Coefficients.cs:2955:CoopPolicyGapWeight | CrownParliament.cs | — | — | この波の線が埋める (889) c75cp | 文書の記号 ●（9-26・写さない） | 王権と議会の点検（612 行 ③・作者の答えはまだ無い） |

### S19 法の成立

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S19-H035 | S19 | 政策距離（立法の停滞度） | 文書 L3750（粒＝見出し・未分割） | CrownParliament.cs:532:PolicyDistance | CrownParliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H036 | S19 | 体制の実績ストック | 文書 L3754（粒＝見出し・未分割） | Coefficients.cs:3086:RegimeRecordAccumRate;Coefficients.cs:3088:RegimeRecordDecay | CrownParliament.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H037 | S19 | 立憲君主の形骸化 | 文書 L3761（粒＝見出し・未分割） | Coefficients.cs:246:ConstMonarchyErosion | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H038 | S19 | 一党支配の私物化 | 文書 L3767（粒＝見出し・未分割） | Coefficients.cs:245:DominanceInstabilitySatisfaction;Coefficients.cs:235:DominanceLegitimacyErosion;Coefficients.cs:234:PartyDominanceThreshold | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S19-H039 | S19 | 議会と権力の画面表示 | 文書 L3779（粒＝見出し・未分割） | — | Parliament.cs Politics.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | — |
| S19-H040 | S19 | 現職効果 | 文書 L3785（粒＝見出し・未分割） | InstrumentsCess.cs:42:Winner | InstrumentsCess.cs Parliament.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 IncumbencyBias | — |
| S19-H041 | S19 | 保守の福祉（アメとムチ）（廃止の見出し） | 文書 L3791（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | — |
| S19-H042 | S19 | 政治ロックの無効化 | 文書 L3796（粒＝見出し・未分割） | Simulation.cs:53:World;World.cs:881:PoliticalLockEnabled | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | — |

### S20 税と支出と国庫

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S20-H001 | S20 | POP の義務は1本の率（2026-09-30 (124)・支払いの段3-4＝B6・作者裁定 Q4） | 文書 L3865（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H002 | S20 | 所得税 | 文書 L3870（粒＝見出し・未分割） | — | Fiscal.cs Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H003 | S20 | 所得税・累進の行政力の天井（廃止の見出し） | 文書 L3876（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 IncomeTaxAdminGate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H004 | S20 | 累進課税 | 文書 L3880（粒＝見出し・未分割） | — | Fiscal.cs Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H005 | S20 | 人頭税 | 文書 L3885（粒＝見出し・未分割） | Coefficients.cs:712:PollTaxRate | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H006 | S20 | 土地税 | 文書 L3891（粒＝見出し・未分割） | — | Fiscal.cs Government.cs | — | — | この波の線が埋める (891) c75ds | 文書の記号 ●（9-26・写さない）；消えた係数 LandTaxAdminGate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H007 | S20 | 身分制の免税 | 文書 L3904（粒＝見出し・未分割） | Coefficients.cs:1854:PrivilegeTaxDiscount | Fiscal.cs Politics.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H008 | S20 | 専売 | 文書 L3912（粒＝見出し・未分割） | Coefficients.cs:787:StateMonopolyLeakMax;Coefficients.cs:780:StateMonopolyMargin | EconomyStep.cs FiscalTables.cs PowerShares.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H009 | S20 | 十分の一税（教会税） | 文書 L3920（粒＝見出し・未分割） | Coefficients.cs:789:TitheRate | Fiscal.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateReligionGate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H010 | S20 | 行政力の移動平均 | 文書 L3926（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AdminPowerSmoothing | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H011 | S20 | 間接税の予算基数への算入 | 文書 L3931（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IndirectTaxInBudgetBase | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H012 | S20 | 汚職による徴収漏れ | 文書 L3937（粒＝見出し・未分割） | Simulation.cs:53:World | Fiscal.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CorruptionMax | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H013 | S20 | 監視は法で買う | 文書 L3945（粒＝見出し・未分割） | Coefficients.cs:3416:OversightFactorScale;Coefficients.cs:3415:OversightMeritExam;Coefficients.cs:3414:OversightPressCensored | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H014 | S20 | 汚職の分け前は官職の地代になる | 文書 L3951（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H015 | S20 | 予算の大きさ（歳入のシェア） | 文書 L3956（粒＝見出し・未分割） | Coefficients.cs:764:AdminBudgetFrac;Coefficients.cs:761:FiscalCapacitySmoothing | Fiscal.cs FiscalTables.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MilitaryRevenueShare | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H016 | S20 | 軍需の請求額を必要額で切る | 文書 L3963（粒＝見出し・未分割）；同じ題 L4789（S23） | Simulation.cs:62:Simulation | Fiscal.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MilitaryBudgetNeedCapped | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H017 | S20 | 行政の購買と首都からの距離（2026-09-30 (135) に行政費の人頭割りから移した） | 文書 L3968（粒＝見出し・未分割） | Coefficients.cs:2307:DistanceWarDecay | Diplomacy.cs EconomyStep.cs Fiscal.cs Politics.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AdminCostPerPop・UpkeepDistanceGeneral | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H018 | S20 | 治安維持力の維持費（2026-09-30 (135) に係数ごと削除）（廃止の見出し） | 文書 L3974（粒＝見出し・未分割） | — | Fiscal.cs Politics.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 CoercionUpkeepRate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H019 | S20 | 社会保障・年金の給付 | 文書 L3982（粒＝見出し・未分割） | Coefficients.cs:772:WelfareRevenueShare | Fiscal.cs FiscalTables.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PensionCostRate・WelfareCostRate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H020 | S20 | 国家の支払い率（全ての行に同じ率） | 文書 L3989（粒＝見出し・未分割） | Simulation.cs:62:Simulation;Simulation.cs:53:World;World.cs:1838:StepConstruction | EconomyStep.cs Fiscal.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateSpendTreasuryClamp・SubsidyRespectsSpendingCap・WelfarePensionClampToTreasury | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H021 | S20 | 国庫の潤沢を支出へ回す | 文書 L3998（粒＝見出し・未分割） | Simulation.cs:53:World;Simulation.cs:62:Simulation | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 OpulenceSpendRate | 土地税の算定の基準 A1（612 行 ⑤⑧） |

### S21 国債と破産

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S21-H001 | S21 | 自動起債（借金ボタンは無い） | 文書 L4003（粒＝見出し・未分割） | Coefficients.cs:456:BorrowFractionOfGdp | Fiscal.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H002 | S21 | 国債の調達プール | 文書 L4013（粒＝見出し・未分割） | Coefficients.cs:416:BondAppetite;Coefficients.cs:417:ForeignBondCapacity | CapitalMarket.cs Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H003 | S21 | 対外国債は他国の投資家が実際に引き受ける | 文書 L4020（粒＝見出し・未分割） | CapitalMarket.cs:305:Investors;MultiNationSim.cs:58:MultiNationSim | CapitalMarket.cs Fiscal.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ForeignBondFromInvestors・ForeignLendingDebits | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H004 | S21 | 借入の上限（利払い÷歳入） | 文書 L4026（粒＝見出し・未分割） | Coefficients.cs:462:MaxInterestShareOfRevenue | Fiscal.cs Megaprojects.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H005 | S21 | 債務緊縮（債権者が強いるハード制約） | 文書 L4036（粒＝見出し・未分割） | Coefficients.cs:436:AusterityRange;Coefficients.cs:435:AusterityStartDebt;Coefficients.cs:437:AusterityTaxHike | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H006 | S21 | 国債の満期構造 | 文書 L4044（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BondRolloverFrac | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H007 | S21 | 国債の保有台帳（誰がどれだけ持つか） | 文書 L4050（粒＝見出し・未分割） | — | Fiscal.cs Invariants.cs Model.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H008 | S21 | 金建ての対外債務 | 文書 L4061（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 GoldDebtBurdenCap・GoldDebtBurdenGate | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H009 | S21 | レンティア帝国の成立 | 文書 L4068（粒＝見出し・未分割） | — | CapitalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ForeignBondCounterparty | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H010 | S21 | 減債 | 文書 L4076（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BondRepayFrac・DebtRepayUsesPrincipalShare | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H011 | S21 | 債務の対GDP比の物差し | 文書 L4084（粒＝見出し・未分割） | — | CapitalMarket.cs Credit.cs Fiscal.cs Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DebtGdpAnnualized | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H012 | S21 | 国債金利の内生決定 | 文書 L4091（粒＝見出し・未分割） | Coefficients.cs:420:BondRateDebtScale;Coefficients.cs:451:BondRateDistrustScale;Coefficients.cs:2545:FisherDeflationFloor;Coefficients.cs:976:FisherInflationWeight | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BondBaseRate・BondRateMax | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H013 | S21 | 議会統制力による信認の割引 | 文書 L4100（粒＝見出し・未分割） | Coefficients.cs:1996:BondRateRepresentationDiscount | CrownParliament.cs Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H014 | S21 | 国債の二次流通価格 | 文書 L4106（粒＝見出し・未分割） | Coefficients.cs:903:BondLossConfWeight | Credit.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H015 | S21 | 通貨切下げ（インフレ税） | 文書 L4113（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DevaluationCapFrac | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H016 | S21 | 不換シニョリッジ | 文書 L4123（粒＝見出し・未分割） | Coefficients.cs:977:FiatInflationWeight;Coefficients.cs:445:FiatIssueFrac | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H017 | S21 | 通貨改革 | 文書 L4129（粒＝見出し・未分割） | Coefficients.cs:996:CurrencyReformBurnCap;Coefficients.cs:995:CurrencyReformBurnShare;Coefficients.cs:998:CurrencyReformCalmMonths;Coefficients.cs:999:CurrencyReformCooldownMonths | Fiscal.cs FiscalTables.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CurrencyReformEnabled | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H018 | S21 | 破産（ソブリン・デフォルト） | 文書 L4137（粒＝見出し・未分割） | Coefficients.cs:477:DefaultArrearsMonths;Coefficients.cs:479:DefaultLegitimacyHit | Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DefaultCooldownQuarters・DefaultMilitancyHit | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H019 | S21 | 破産が議会を呼ぶ | 文書 L4147（粒＝見出し・未分割） | Coefficients.cs:2960:SummonParliamentRate | CrownParliament.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |
| S21-H020 | S21 | 革命による債務否認 | 文書 L4153（粒＝見出し・未分割） | — | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RepudiationCreditPenalty | ⑩ 利札の残り・⑪ InsolvencyQuarters→Months（612 行） |

### S22 貨幣と本位と信用

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S22-H001 | S22 | 貨幣の3つの段（不換→銀→金） | 文書 L4159（粒＝見出し・未分割） | Coefficients.cs:3480:MonetaryPeerPullScale | Government.cs MultiNationSim.cs NationConfig.cs PolicyPreferences.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 GoldStandardIndThreshold・SilverStandardGate・SilverStandardIndThreshold | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H002 | S22 | 金本位の信用ゲートと強制離脱 | 文書 L4168（粒＝見出し・未分割） | Coefficients.cs:965:GoldCoverMin;Coefficients.cs:969:GoldStandardMinFunding | Credit.cs Fiscal.cs FiscalTables.cs Government.cs SelfPlay.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 GoldStandardMaxDebt・GoldStandardSuspendDebt | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H003 | S22 | 再加入のシュミットトリガー | 文書 L4175（粒＝見出し・未分割） | Coefficients.cs:2006:GoldRejoinCoverMargin | Fiscal.cs Government.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H004 | S22 | 兌換可能性の連続化 | 文書 L4180（粒＝見出し・未分割） | Coefficients.cs:974:ConvertibilityCoverageBand;Coefficients.cs:973:ConvertibilityDropSpeed;Coefficients.cs:972:ConvertibilityRiseSpeed | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H005 | S22 | 銀の段の軽い規律 | 文書 L4187（粒＝見出し・未分割） | — | Credit.cs Fiscal.cs Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SilverLightDiscipline | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H006 | S22 | 造幣（本位金属の買い上げ） | 文書 L4194（粒＝見出し・未分割） | Coefficients.cs:3510:MintReserveTargetWeight | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 GoldMintFraction・MintFloorBuyRate・MintPriceFloorGate・StandardMetalMintGate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H007 | S22 | 造幣局の市場買い | 文書 L4203（粒＝見出し・未分割） | Coefficients.cs:3452:MintBuysAtMarket | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H008 | S22 | 造幣の買いを需要として数える | 文書 L4209（粒＝見出し・未分割） | Coefficients.cs:3381:MintDemandCounted | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H009 | S22 | 法が捨てた金属の投げ売り | 文書 L4215（粒＝見出し・未分割） | Simulation.cs:3001:SellReserve;Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MintReserveSellRate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H010 | S22 | 鋳造の代金は④の持分（2026-09-30 (124)・支払いの段3-5＝C8・旧「鋳造代金の賃金分」） | 文書 L4221（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MintWageShare | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H011 | S22 | トリレンマ | 文書 L4227（粒＝見出し・未分割） | Coefficients.cs:3511:TrilemmaRateResponse | Fiscal.cs Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TrilemmaRateGate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H012 | S22 | 資本移動の法 | 文書 L4234（粒＝見出し・未分割） | Coefficients.cs:3372:CapitalControlOpenness | Fiscal.cs Government.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalMobilityLawGate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H013 | S22 | 物価水準 | 文書 L4241（粒＝見出し・未分割） | Coefficients.cs:950:MonetaryPriceSensitivity;Coefficients.cs:951:MonetaryTrendSpeed;Coefficients.cs:955:PriceLevelFloor | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PriceLevelCeil | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H014 | S22 | 為替相場 | 文書 L4247（粒＝見出し・未分割） | Coefficients.cs:1001:ExchangeRateCeil;Coefficients.cs:1000:ExchangeRateFloor;Coefficients.cs:979:ExchangeRateSpeed;Coefficients.cs:980:ExchangeTradeAdjust | CapitalMarket.cs MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H015 | S22 | 外資信用（対外信用の4つの内訳） | 文書 L4255（粒＝見出し・未分割） | Coefficients.cs:454:BondYieldFlightWeight;Coefficients.cs:446:DebtDistressRatio;Coefficients.cs:1005:OffGoldCapitalPenalty | CapitalMarket.cs Fiscal.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H016 | S22 | 銀行準備の内生化 | 文書 L4263（粒＝見出し・未分割） | — | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H017 | S22 | タンス預金 | 文書 L4268（粒＝見出し・未分割） | Coefficients.cs:2584:CurrencyHoldingRatio;Coefficients.cs:2585:HoardPanicWeight | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H018 | S22 | 信用乗数の発展度スケール | 文書 L4273（粒＝見出し・未分割） | Coefficients.cs:836:CreditMultiplier | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CreditDevScale・CreditMultiplierDevFloor・CreditMultiplierDevWeight | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H019 | S22 | ミンスキー型の信頼動学 | 文書 L4279（粒＝見出し・未分割） | Coefficients.cs:867:ConfidencePivot;Coefficients.cs:844:ConfidenceSpeed;Coefficients.cs:852:PanicSensitivity;Coefficients.cs:845:PanicSpeed | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H020 | S22 | 信頼を崩す経路 | 文書 L4286（粒＝見出し・未分割） | Coefficients.cs:904:DebtDeflationWeight;Coefficients.cs:853:DrawdownPanic;Coefficients.cs:414:FiscalDefaultPanic | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H021 | S22 | シティ型の過熱脆弱性 | 文書 L4292（粒＝見出し・未分割） | Coefficients.cs:857:OverheatFragility;Coefficients.cs:858:OverheatFragilityThreshold | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H022 | S22 | 銀行の取り付けと最後の貸し手 | 文書 L4298（粒＝見出し・未分割） | Coefficients.cs:876:BankRunMax;Coefficients.cs:875:BankRunSensitivity;Coefficients.cs:878:LolrDrawdownTrigger;Coefficients.cs:877:LolrHeatTrigger | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H023 | S22 | 金本位を守るための与信の絞り | 文書 L4304（粒＝見出し・未分割） | Coefficients.cs:2007:GoldCoverCreditRestraint | Credit.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H024 | S22 | 与信の注入と回収 | 文書 L4310（粒＝見出し・未分割） | Coefficients.cs:837:CreditAdjustSpeed;Coefficients.cs:838:InterestRateEffect | Credit.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H025 | S22 | 与信過熱度 | 文書 L4319（粒＝見出し・未分割） | — | Credit.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EuphoriaUsesStableReserve | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H026 | S22 | 国際資本市場 | 文書 L4325（粒＝見出し・未分割） | Coefficients.cs:1014:CapitalCreditFloor;Coefficients.cs:1007:CapitalExportFrac;Coefficients.cs:1004:CapitalIndTarget;Coefficients.cs:1005:OffGoldCapitalPenalty | CapitalMarket.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H027 | S22 | 資本逃避・配当・新規流入 | 文書 L4333（粒＝見出し・未分割） | — | CapitalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalDividendRate・CapitalFlightRate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H028 | S22 | 自国資本の逃避 | 文書 L4341（粒＝見出し・未分割） | Coefficients.cs:2543:CapitalFlightDistrustThreshold | CapitalMarket.cs Fiscal.cs FiscalTables.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalDomesticFlightRate | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H029 | S22 | 外資の吸収能力 | 文書 L4348（粒＝見出し・未分割） | — | CapitalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapitalAbsorptionBlend | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H030 | S22 | 金融危機の国際伝染 | 文書 L4355（粒＝見出し・未分割） | Coefficients.cs:1013:CapitalContagion | CapitalMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H031 | S22 | 貨幣保存の恒等式 | 文書 L4360（粒＝見出し・未分割） | — | Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H032 | S22 | 反景気循環の金融政策 | 文書 L4365（粒＝見出し・未分割） | Coefficients.cs:885:PreTightenHeatFrac | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |

### S20 税と支出と国庫

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S20-H022 | S20 | AIの財政規律 | 文書 L4371（粒＝見出し・未分割） | Coefficients.cs:886:CrisisAusterityRelief | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H023 | S20 | AIの通貨制度と資本移動の判断 | 文書 L4377（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H024 | S20 | 国庫が潤沢なら減税 | 文書 L4383（粒＝見出し・未分割） | Coefficients.cs:1089:OpulenceTaxRelief | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H025 | S20 | レバーのハード制約の可視化 | 文書 L4390（粒＝見出し・未分割） | — | Government.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |

### S22 貨幣と本位と信用

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S22-H033 | S22 | 実効貸出金利 | 文書 L4396（粒＝見出し・未分割） | Coefficients.cs:901:PolicyRateLoanWeight | Credit.cs FiscalTables.cs World.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない）；消えた係数 LoanSpread | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H034 | S22 | 金利の内訳説明 | 文書 L4402（粒＝見出し・未分割） | World.cs:802:Explain | Explain.cs Fiscal.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H035 | S22 | 与信目標の内訳説明 | 文書 L4406（粒＝見出し・未分割） | World.cs:802:Explain | Credit.cs Explain.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H036 | S22 | 投資収益率と為替の内訳説明 | 文書 L4410（粒＝見出し・未分割） | Simulation.cs:53:World | CapitalMarket.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H037 | S22 | 予算表（予想と実効の2列） | 文書 L4414（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H038 | S22 | 国庫の台帳 | 文書 L4420（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H039 | S22 | 政策金利と貸出金利の表示 | 文書 L4427（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H040 | S22 | 兌換カバー率の表示 | 文書 L4431（粒＝見出し・未分割） | InstrumentsCornint.cs:20:Header | Fiscal.cs InstrumentsCornint.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H041 | S22 | 家計債務と損失の負債化の表示 | 文書 L4435（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H042 | S22 | 国債のヘッダー表示 | 文書 L4439（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H043 | S22 | 連合維持費は治安と行政の役務の給与（2026-09-30 (117)・旧「○ 抑圧装置の維持費の帰属」＝休眠の傾斜を置き換え）→ **2026-09-30 (135) に連合維持費ごと撤去**＝同じ役務（兵士の兵力・官僚の行政）を購買と給与の上乗せで二重・三重に払っていた＝兵士と官僚の給与は兵力と行政の購買の1本（遠い州の割増は行政の購買へ＝上の「行政の購買と首都からの距離」）（廃止の見出し） | 文書 L4444（粒＝見出し・未分割） | InternationalMarket.cs:33:InternationalMarket | Fiscal.cs InternationalMarket.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 AdminCostPerPop・CoercionUpkeepRate・CoercionUpkeepToSoldiers | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H044 | S22 | 国債金利の債務二次項（2026-09-29 (96) に係数ごと削除）（廃止の見出し） | 文書 L4449（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 BondRateDebtQuad | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H045 | S22 | 民間貸出金利のキャッシュフロー | 文書 L4454（粒＝見出し・未分割） | Coefficients.cs:901:PolicyRateLoanWeight | Credit.cs FiscalTables.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 LoanSpread | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H046 | S22 | 未払い利息の複利化 | 文書 L4459（粒＝見出し・未分割） | Coefficients.cs:1503:InterestCapitalization | Credit.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H047 | S22 | 需要駆動の与信（廃止の見出し） | 文書 L4463（粒＝見出し・未分割） | — | Credit.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 CreditDemandDriven | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H048 | S22 | 家計の個人債務 | 文書 L4469（粒＝見出し・未分割） | Coefficients.cs:737:DebtRuinRatio;Coefficients.cs:739:HouseholdCreditMonths;Coefficients.cs:732:PopDebtDriven | Bank.cs EconomyStep.cs FiscalTables.cs Meritocracy.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 HouseholdCreditStapleOnly | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H049 | S22 | 財政カデンスの四半期ゲート | 文書 L4475（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 FiscalQuarterlyGating | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H050 | S22 | 跛行本位 | 文書 L4480（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 MintFloorRespectsReserve | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H051 | S22 | 造幣の両側の売買 | 文書 L4486（粒＝見出し・未分割） | Coefficients.cs:3460:MintParitySells | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H052 | S22 | 銀の平価を金銀比から導く | 文書 L4491（粒＝見出し・未分割） | Coefficients.cs:3440:SilverParityFromRatio | BilateralMarket.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 WorldPriceStatUnclamped | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |
| S22-H053 | S22 | シルバーラッシュ | 文書 L4496（粒＝見出し・未分割） | Coefficients.cs:1360:SilverRushEnabled | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | ⑩ 利札の残り・⑪ I2 造幣の組（612 行） |

### S20 税と支出と国庫

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S20-H026 | S20 | 特権の台帳 | 文書 L4501（粒＝見出し・未分割）；同じ題 L7148（S29） | Coefficients.cs:3307:PrivilegeLedgerEnabled | EconomyStep.cs Fiscal.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H027 | S20 | 王権の取り分に比例する免税 | 文書 L4508（粒＝見出し・未分割） | Coefficients.cs:1853:PrivilegeFromCrownShare | Fiscal.cs PopTables.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H028 | S20 | 参政権の範囲と課税への同意 | 文書 L4512（粒＝見出し・未分割） | InterestGroups.cs:79:SuffrageInclusion | Fiscal.cs InterestGroups.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SuffrageScopeGate | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H029 | S20 | 支配度で課税ベースと給付の届き方を割る | 文書 L4518（粒＝見出し・未分割） | — | Fiscal.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 RuleSharesReach・RuleSharesTaxBase | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H030 | S20 | 暮らせない官僚の汚職 | 文書 L4523（粒＝見出し・未分割） | Simulation.cs:53:World | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 CorruptionPoorOfficialWeight | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H031 | S20 | 貴族も投資家に数える | 文書 L4529（粒＝見出し・未分割） | — | CapitalMarket.cs Fiscal.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 NobleInvests | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H032 | S20 | 義務教育費の国庫の現金クランプ（⚠ 2026-09-27 に焼き込み・2026-09-28 支払いの一元化 段2-1で撤去＝国家の支払い率 r） | 文書 L4534（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 EduFundingCashClamp | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H033 | S20 | 恐慌の谷での前倒し起債（2026-09-28 (89) に係数ごと削除）（廃止の見出し） | 文書 L4539（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 AiCrisisBorrow | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H034 | S20 | 開発国家の補助金 | 文書 L4544（粒＝見出し・未分割） | Coefficients.cs:1991:AiDevelopmentalSubsidy | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H035 | S20 | 金本位志向の前線比 | 文書 L4549（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 GoldStandardFrontierRatio | 土地税の算定の基準 A1（612 行 ⑤⑧） |
| S20-H036 | S20 | 金本位の再加入クールダウン（廃止の見出し） | 文書 L4554（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 GoldRejoinCooldownYears | 土地税の算定の基準 A1（612 行 ⑤⑧） |

### S23 軍備と動員

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S23-H001 | S23 | 兵士は人口の一部 | 文書 L4614（粒＝見出し・未分割） | Coefficients.cs:2489:MaxSoldierFraction;Coefficients.cs:2375:SoldierRecruitClassA;Coefficients.cs:2376:SoldierRecruitClassB | Meritocracy.cs Military.cs MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SoldierMigrationRate | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H002 | S23 | 定員（制度が開く兵の席）＝旧の扶養限界 | 文書 L4620（粒＝見出し・未分割） | Coefficients.cs:2458:MilitaryEnvelopeShare;Coefficients.cs:2143:MilitaryUpkeepScale;Coefficients.cs:2492:SoldierPayMult | EconomyStep.cs Military.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ArmySustainWeight | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H003 | S23 | 軍拡競争 → 2026-10-02 (155) **削除**（廃止の見出し） | 文書 L4627（粒＝見出し・未分割） | Diplomacy.cs:22:Military | Diplomacy.cs Military.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 ArmsRaceHigh・ArmsRaceLow・ArmsRaceTensionFloor | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H004 | S23 | 軍事費の段（少額／中額／多額／戦時） | 文書 L4634（粒＝見出し・未分割） | Government.cs:48:MilitaryLawStages | FiscalCatalog.cs Government.cs Military.cs Parliament.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H005 | S23 | 軍事費の吸い込み（給与→装備→質→使わない） | 文書 L4640（粒＝見出し・未分割） | Coefficients.cs:771:MilitaryCadrePayRatio;Coefficients.cs:2458:MilitaryEnvelopeShare | Fiscal.cs Military.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H006 | S23 | 安全保障の不安（AI の軍事費の段の唯一の入口） | 文書 L4645（粒＝見出し・未分割） | World.cs:1362:SecurityThreat;MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:1563:StepSecurityThreat | MultiNationSim.cs SelfPlay.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H007 | S23 | 陸軍の不安（徴兵法の目標の入口） | 文書 L4650（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:1563:StepSecurityThreat;ArmyShape.cs:197:Borders | ArmyShape.cs MultiNationSim.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H008 | S23 | 戦争中は軍を解散できない | 文書 L4655（粒＝見出し・未分割） | Coefficients.cs:2224:WartimeUpkeepFloor | Government.cs PowerShares.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H009 | S23 | 戦時の徴兵 | 文書 L4660（粒＝見出し・未分割） | Coefficients.cs:2494:ConscriptionFraction | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConscriptionRate | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H010 | S23 | 徴兵率は戦争の規模で変わる | 文書 L4665（粒＝見出し・未分割） | Coefficients.cs:2712:ConscriptionEnemyRatioWeight;Coefficients.cs:2710:ConscriptionExhaustionWeight;Coefficients.cs:2709:ConscriptionFloorFraction;Coefficients.cs:2711:ConscriptionFrontStep | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConscriptionScalesWithWar | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H011 | S23 | 総動員 | 文書 L4670（粒＝見出し・未分割） | Coefficients.cs:1417:MobilizationConscriptFrac | Diplomacy.cs Fiscal.cs Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H012 | S23 | AIの総動員 | 文書 L4675（粒＝見出し・未分割） | SelfPlay.cs:478:MobilizeLosingScore;SelfPlay.cs:475:MobilizeOutmatchRatio | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H013 | S23 | 国民意識が動員の深さを決める | 文書 L4680（粒＝見出し・未分割） | Coefficients.cs:3274:NationBuildMobilizationWeight;Coefficients.cs:2223:SkillMobilizationRelief | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H014 | S23 | 国民意識のない州からは兵が出ない → 2026-10-01 (145) **削除**（廃止の見出し） | 文書 L4685（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 ConscriptConsciousnessWeight | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H015 | S23 | 徴兵法の3段 | 文書 L4691（粒＝見出し・未分割） | SelfPlay.cs:609:ConscriptionLowerMargin;SelfPlay.cs:604:ConscriptionRaiseShare;Coefficients.cs:2695:UniversalConscriptionFactor | Military.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H016 | S23 | 国民皆兵の門（封建の兵から国民の兵へ） | 文書 L4696（粒＝見出し・未分割） | Government.cs:621:UniversalConscriptionOpen;Privileges.cs:24:SeigneurialDegree | Government.cs Privileges.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H017 | S23 | 開幕の徴兵法（1783 の史実） | 文書 L4701（粒＝見出し・未分割） | Scenario.cs:69:NationConfig;Simulation.cs:62:Simulation | NationConfig.cs Scenario.cs Simulation.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H018 | S23 | 戦時に上げた徴兵法は戦後に戻る | 文書 L4706（粒＝見出し・未分割） | CapitalMarket.cs:24:Step;World.cs:125:ConscriptionPreWar | CapitalMarket.cs Government.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H019 | S23 | 徴兵の遺恨 | 文書 L4712（粒＝見出し・未分割） | Coefficients.cs:2097:ConscriptGrievanceRate | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H020 | S23 | 植民地の平時の徴募にも遺恨 | 文書 L4718（粒＝見出し・未分割） | Coefficients.cs:2769:PeacetimeConscriptGrievance | Military.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 RuleSharesTaxBase | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H021 | S23 | 徴兵が村を変える | 文書 L4723（粒＝見出し・未分割） | Diplomacy.cs:22:Military | Diplomacy.cs Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConscriptExcludeOldMales・MigrationCarriesHumanCapital | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H022 | S23 | 陸海の熟練度 | 文書 L4729（粒＝見出し・未分割） | Coefficients.cs:2213:SkillCombatMax;Coefficients.cs:2212:SkillDecayPerQuarter;Coefficients.cs:2210:SkillGainRate;Coefficients.cs:2209:SkillUpkeepThreshold | Military.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H023 | S23 | 即応度 | 文書 L4734（粒＝見出し・未分割） | Diplomacy.cs:22:Military;PowerShares.cs:149:SoldierWelfare | Diplomacy.cs Military.cs PowerShares.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H024 | S23 | 軍需の調達と装備の充足率 | 文書 L4739（粒＝見出し・未分割） | Coefficients.cs:2100:MilitaryGoods;Coefficients.cs:2101:MilitaryRates;Coefficients.cs:2168:MilitaryWarMult;Coefficients.cs:2169:WarMobilizationDepth | EconomyStep.cs Military.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MilitaryRevenueShare・StateProcurePriceGate・WartimeBudgetFollowsNeed | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H025 | S23 | 装備の4段階 | 文書 L4745（粒＝見出し・未分割） | Coefficients.cs:2199:MilitaryAirGoods;Coefficients.cs:2200:MilitaryAirRates;Coefficients.cs:2174:MilitaryQualityBonus;Coefficients.cs:2172:MilitaryQualityGoods | Diplomacy.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H026 | S23 | 耐久装備は在庫になる（上位の段） | 文書 L4751（粒＝見出し・未分割） | Coefficients.cs:1392:ArmsWarWearScale | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ArmsStockEnabled・ArmsWearRate | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H027 | S23 | 基本装備も在庫になる | 文書 L4757（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 BaseArmsStockEnabled・MilitaryWearRates | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H028 | S23 | 開幕の工廠備蓄 | 文書 L4763（粒＝見出し・未分割） | Coefficients.cs:2167:InitialArsenalMonths | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H029 | S23 | 未発見の装備を必要量に入れない | 文書 L4768（粒＝見出し・未分割） | Discovery.cs:323:GoodGate | Discovery.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EquipNeedsDiscovery | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H030 | S23 | 前期の軍需レシピ（マスケット・鋳鉄砲） | 文書 L4773（粒＝見出し・未分割） | Coefficients.cs:2052:MusketProductivity | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MusketRecipeEnabled | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H031 | S23 | 開幕の軍需産業の播種（廃止の見出し） | 文書 L4778（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 InitialArmsIndustryScale | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H032 | S23 | 軍需財だけ価格関門を緩める器 | 文書 L4784（粒＝見出し・未分割） | Coefficients.cs:1114:MilitaryProcurePriceGateScale | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H033 | S23 | 請求書駆動の軍事費（廃止の見出し） | 文書 L4794（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 SpendFromNeedMax・SpendFromNeedMin・SpendFromNeedWeight | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H034 | S23 | 輜重と航空機の燃料 | 文書 L4800（粒＝見出し・未分割） | Coefficients.cs:3551:AirFuelRate;Coefficients.cs:3553:MotorFuelRate | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H035 | S23 | 戦力ゲージの合成 | 文書 L4805（粒＝見出し・未分割） | Coefficients.cs:2377:SoldiersPerMilitary;Coefficients.cs:943:UnityMilitaryQuality | ArmyShape.cs Diplomacy.cs EconomyStep.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H036 | S23 | 技術による質の差（アヘン戦争の非対称） | 文書 L4811（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | ArmyShape.cs Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TechMilFullLevel・TechMilLowFloor | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H037 | S23 | 制度疲労で軍改革が失敗する | 文書 L4817（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | ArmyShape.cs Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DecayMilitaryWeight | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H038 | S23 | 軍の練度（識字 × 組織 × 勤務年数） | 文書 L4824（粒＝見出し・未分割） | Coefficients.cs:2490:MilitarismStrengthBonus | ArmyShape.cs Diplomacy.cs PoliticalQuantities.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H039 | S23 | 陸軍と海軍の分割は払った金の比 | 文書 L4829（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Simulation.cs:53:World;Simulation.cs:62:Simulation | Diplomacy.cs MultiNationSim.cs SelfPlay.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H040 | S23 | 建艦の時間遅れ | 文書 L4836（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NavalBuildRate | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H041 | S23 | 海軍は艦ありき（艦隊ゲージ） | 文書 L4841（粒＝見出し・未分割） | Coefficients.cs:2306:AbstractNavalShare;Coefficients.cs:2269:FleetGoodIds;Coefficients.cs:2174:MilitaryQualityBonus;Coefficients.cs:2197:MilitaryTier3Bonus | ArmyShape.cs Diplomacy.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NavalPowerFromFleet | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H042 | S23 | 港のない国は軍艦を買わない | 文書 L4848（粒＝見出し・未分割） | Simulation.cs:62:Simulation | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NavalProcurementNeedsPort | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H043 | S23 | 艦隊の摩耗と修理需要 | 文書 L4853（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs EconomyStep.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 NavalAttritionRate・NavalRepairPerLoss | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |
| S23-H044 | S23 | 空軍と制空権 | 文書 L4859（粒＝見出し・未分割） | Coefficients.cs:2202:AirCasualtyWeight;Coefficients.cs:2201:AirControlPushWeight;Coefficients.cs:2204:ChemExhaustionWeight;Coefficients.cs:2203:WarExhaustionAirRaid | Diplomacy.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 原爆（612 行）・武器の続き・海軍の需要の道（616 行 ①）・国民皆兵の門（616 行 ⑥） |

### S24 開戦と戦争と講和

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S24-H001 | S24 | 緊張から戦争が始まる（宣戦布告のボタンは無い） | 文書 L4864（粒＝見出し・未分割） | Coefficients.cs:1415:WarDeclareTension;Coefficients.cs:1265:WarTriggerMonths;Coefficients.cs:1433:WarWearyThreshold | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H002 | S24 | 届かない相手には宣戦しない | 文書 L4870（粒＝見出し・未分割） | Coefficients.cs:1794:DefendProjectionMin | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarNeedsReach | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H003 | S24 | 開戦寸前の屈服 | 文書 L4875（粒＝見出し・未分割） | Coefficients.cs:1964:AISubmitLegitCost;Coefficients.cs:1876:AISubmitMilRatio;Coefficients.cs:1418:ConcedeTensionTarget | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SubmitCountsAllies | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H004 | S24 | 最後通牒（プレイヤー版） | 文書 L4881（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H005 | S24 | 戦争見通し | 文書 L4885（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H006 | S24 | 同盟の参戦（義務は無い） | 文書 L4890（粒＝見出し・未分割） | Coefficients.cs:1266:AllyJoinTension;Coefficients.cs:1272:AntiHegemonJoinFactor | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H007 | S24 | 同盟国も勝ち目と国難を見る | 文書 L4895（粒＝見出し・未分割） | Coefficients.cs:1753:AllyJoinFoodMin;Coefficients.cs:1754:AllyJoinFundingMin;Coefficients.cs:1755:AllyJoinLegitimacyMin;Coefficients.cs:1788:AppeaseMilitaryRatio | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H008 | S24 | 参戦の連鎖 | 文書 L4900（粒＝見出し・未分割）；同じ題 L5518（S25） | Coefficients.cs:1758:WarChainDepthPenalty;Coefficients.cs:1757:WarChainMaxDepth | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H009 | S24 | 参戦要請と見送りの信義 | 文書 L4906（粒＝見出し・未分割） | Coefficients.cs:1756:AllyRefusalTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H010 | S24 | 宗主の防衛義務 | 文書 L4911（粒＝見出し・未分割）；同じ題 L5634（S25） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H011 | S24 | 戦争熱（世論） | 文書 L4915（粒＝見出し・未分割） | Coefficients.cs:1444:WarFeverMilSpend;Coefficients.cs:1431:WarFeverMilitancy;Coefficients.cs:1432:WarFeverMilitancyGate;Coefficients.cs:1442:WarFeverMilitarism | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H012 | S24 | 余剰戦力（膨張意欲）の蓄積 | 文書 L4920（粒＝見出し・未分割） | Coefficients.cs:1534:ExpansionWillBourgeois;Coefficients.cs:1450:ExpansionWillCap;Coefficients.cs:1449:ExpansionWillGrowth;Coefficients.cs:1448:WarFeverThreshold | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H013 | S24 | 主戦論は軍事費の「水準」でなく「シェア」で測る | 文書 L4925（粒＝見出し・未分割） | Simulation.cs:53:World | PoliticalQuantities.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarFeverUsesBudgetShare | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H014 | S24 | 侵略（唯一の攻撃の動詞） | 文書 L4930（粒＝見出し・未分割） | Coefficients.cs:1793:ColonizeAnnexMilitancy;Coefficients.cs:1698:ColonizeRivalTension;Coefficients.cs:1697:ColonizeSubmitFloorMilitary;Coefficients.cs:1696:ColonizeSubmitRatio | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H015 | S24 | 遠征の技術ゲート | 文書 L4937（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ExpeditionTechFloor・ExpeditionTechLevel | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H016 | S24 | 遠征の物資（船・糧食・キニーネ） | 文書 L4942（粒＝見出し・未分割） | Coefficients.cs:604:EpidemicSeverity;Coefficients.cs:1465:InvadeCannedBase;Coefficients.cs:1466:InvadeCannedPerMilitary;Coefficients.cs:2310:InvadeSeaControlMin | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 InvadeShipDistanceScale・InvadeShipSeaDistance | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H017 | S24 | 統一戦争のAI | 文書 L4948（粒＝見出し・未分割）；同じ題 L5752（S25）・L6359（S27） | Coefficients.cs:3111:UnificationAiCooldownYears;Coefficients.cs:3110:UnificationAiPursueChance;Coefficients.cs:3112:UnificationAiWarGapYears;Coefficients.cs:3107:UnificationV1Only | Diplomacy.cs Formations.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 UnificationWarEnabled | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H018 | S24 | 戦線の押し合い（戦勝点） | 文書 L4954（粒＝見出し・未分割） | Coefficients.cs:1401:WarPushSpeed | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H019 | S24 | 塹壕と戦車 | 文書 L4960（粒＝見出し・未分割） | Coefficients.cs:1397:TrenchDefenseWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H020 | S24 | 決定的会戦の揺らぎ | 文書 L4965（粒＝見出し・未分割） | Coefficients.cs:1402:WarLuck | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H021 | S24 | 届かない戦線には運も立たない | 文書 L4971（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarLuckNeedsContact | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H022 | S24 | 戦費（払えない軍は弱い）（2026-09-30 (117) に機構ごと削除）（廃止の見出し） | 文書 L4976（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs world.json | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 WarCostPerMilitary・WarFundFloor・WarFundPayWeight | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H023 | S24 | 戦力の投射（距離と制海権） | 文書 L4983（粒＝見出し・未分割） | Coefficients.cs:2307:DistanceWarDecay;Coefficients.cs:2309:HomeDefenseBonus | Diplomacy.cs Fiscal.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H024 | S24 | 陸続きかどうか | 文書 L4989（粒＝見出し・未分割） | Landmass.cs:40:Landmass;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs Landmass.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ContinentalLandReach・LandReachKm | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H025 | S24 | 制海権 | 文書 L4995（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H026 | S24 | 上陸ゲート | 文書 L4999（粒＝見出し・未分割） | Coefficients.cs:1414:SeaInvasionControlMin | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H027 | S24 | 運河と海峡の戦時封鎖 | 文書 L5004（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H028 | S24 | 厭戦（7つの源泉） | 文書 L5008（粒＝見出し・未分割） | Coefficients.cs:2882:FiscalExhaustionWarWeariness;Coefficients.cs:1429:WarExhaustionBlockade;Coefficients.cs:1430:WarExhaustionCasualty;Coefficients.cs:1428:WarExhaustionEcon | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarExhaustionRate | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H029 | S24 | 戦時配給法 | 文書 L5015（粒＝見出し・未分割） | Coefficients.cs:3189:RationingBlockadeRelief | Diplomacy.cs Policy.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 RationingGate | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H030 | S24 | 接触強度（前線が実際にぶつかった量） | 文書 L5021（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CasualtiesFromContact | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H031 | S24 | 戦死 | 文書 L5027（粒＝見出し・未分割） | Coefficients.cs:1645:OfficerCasualtyShare | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarCasualtyMonthlyCap・WarCasualtyRate | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H032 | S24 | 対外戦争の民間人死 | 文書 L5037（粒＝見出し・未分割） | Coefficients.cs:2012:WarCivilianToll | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H033 | S24 | 戦場の資本破壊 | 文書 L5043（粒＝見出し・未分割） | Coefficients.cs:2013:WarZoneCapitalDamage | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H034 | S24 | 戦災による農業と畜産の減産 | 文書 L5048（粒＝見出し・未分割）；同じ題 L6683（S28） | Coefficients.cs:2601:WarFarmOutputDamage;Coefficients.cs:2603:WarLivestockOutputDamage;Coefficients.cs:2602:WarLivestockRequisition | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H035 | S24 | 海上封鎖 | 文書 L5054（粒＝見出し・未分割） | Coefficients.cs:2364:BlockadeStapleBias;Coefficients.cs:2612:RiskLambda | BilateralMarket.cs Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H036 | S24 | 厭戦が国内の不満になる | 文書 L5072（粒＝見出し・未分割） | Coefficients.cs:673:WarWearinessMilitancy | Diplomacy.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarExhaustionMilitancy・WarMilitancyRate・WarMilitancyViaTarget | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H037 | S24 | 戦争が治安維持力を奪う | 文書 L5078（粒＝見出し・未分割） | Coefficients.cs:909:WarCoercionDrain;Coefficients.cs:1745:WarRallyCoercion;Coefficients.cs:1746:WarRallyDecayPoint | Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H038 | S24 | 戦争が疫病を運ぶ | 文書 L5084（粒＝見出し・未分割） | Coefficients.cs:612:EpidemicWarTransmit | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H039 | S24 | 厭戦の戦後減衰 | 文書 L5089（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CivilWarByScore・WarWearinessDecayRate・WarWearinessDecays | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H040 | S24 | 革命の圧力が講和を前倒す | 文書 L5094（粒＝見出し・未分割） | Coefficients.cs:1747:RevolutionSurrenderWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H041 | S24 | 終戦の4条件 | 文書 L5099（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarMaxMonths | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H042 | S24 | 崩壊講和の取り分は戦線が決める | 文書 L5105（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CollapseImposeFromFront | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H043 | S24 | 列強の調停 | 文書 L5110（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PeaceInterventionEnabled | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H044 | S24 | 白紙講和 | 文書 L5115（粒＝見出し・未分割） | Coefficients.cs:1522:WhitePeaceScore | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H045 | S24 | 賠償金 | 文書 L5120（粒＝見出し・未分割） | Coefficients.cs:1438:ReparationGdpFraction | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AbstractTargetReparations | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H046 | S24 | 会議外交による賠償の抑制 | 文書 L5125（粒＝見出し・未分割） | Coefficients.cs:1566:ConcertReparationRestraint;Coefficients.cs:1567:ConcertRestraintMax | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H047 | S24 | 植民地の奪取 | 文書 L5131（粒＝見出し・未分割） | Coefficients.cs:1526:ColonyCaptureBudget;Coefficients.cs:1527:ColonyCaptureCostBase;Coefficients.cs:1533:ColonyCaptureDistanceWeight;Coefficients.cs:1531:ColonyCaptureMax | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonyDevCostWeight | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H048 | S24 | 遠すぎる植民地は受け取れない | 文書 L5137（粒＝見出し・未分割） | Coefficients.cs:1557:CessionMaxDistanceKm | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonyCaptureReachGate | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H049 | S24 | 奪った植民地はその場で州になる | 文書 L5143（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Simulation.cs:1483:PromoteColonyToRegion | Diplomacy.cs MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CapturedColoniesAreRegions | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H050 | S24 | 植民地格の州の割譲 | 文書 L5149（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonialRegionCession・ColonyDevCostFromRegion | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H051 | S24 | 征服した土地の不満 | 文書 L5155（粒＝見出し・未分割） | Coefficients.cs:1561:CededColonyMilitancy;Coefficients.cs:2883:ConquestColonyMilitancy | Diplomacy.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ConquestForeignWeight | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H052 | S24 | 州の割譲 | 文書 L5161（粒＝見出し・未分割）；同じ題 L6236（S27） | Coefficients.cs:1543:TerritoryCessionScore | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H053 | S24 | 割譲には届く距離が要る | 文書 L5166（粒＝見出し・未分割） | Coefficients.cs:1557:CessionMaxDistanceKm | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H054 | S24 | 属国化か直接併合か | 文書 L5172（粒＝見出し・未分割）；同じ題 L6302（S27） | Coefficients.cs:1601:GovCostVassalThreshold;Coefficients.cs:1598:VassalizeWinScore | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H055 | S24 | 条約港（遠すぎて属国にできない相手） | 文書 L5177（粒＝見出し・未分割） | Coefficients.cs:1023:TreatyPortReparationMult;Coefficients.cs:1018:TreatyPortWinnerDistanceKm | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H056 | S24 | 条約の批准 | 文書 L5182（粒＝見出し・未分割） | Coefficients.cs:1787:TreatyRatificationWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H057 | S24 | 宗主権の引き継ぎ | 文書 L5187（粒＝見出し・未分割）；同じ題 L5691（S25）・L6318（S27） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VassalTransferOnCrush | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H058 | S24 | 政体の押し付け | 文書 L5192（粒＝見出し・未分割）；同じ題 L5813（S25） | Coefficients.cs:2017:ImposeRegimeIdeologyGap;Coefficients.cs:2018:ImposedRegimeLegitimacy | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ImposeRegimeEnabled | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H059 | S24 | 敗戦ショックと戦勝の免責 | 文書 L5197（粒＝見出し・未分割） | Coefficients.cs:1742:DefeatNationalism;Coefficients.cs:1560:DefeatShockLegitWeight;Coefficients.cs:2964:VictoryLegitBoost | Diplomacy.cs Ideology.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 DefeatShockDecay・WarDefeatMilitancySpike | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H060 | S24 | 戦勝ショック（主戦論の噴出） | 文書 L5203（粒＝見出し・未分割） | Coefficients.cs:1760:VictoryImperialism;Coefficients.cs:1759:VictoryNationalism | Diplomacy.cs Ideology.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VictoryShockDecay | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H061 | S24 | 失地の長期ストック | 文書 L5208（粒＝見出し・未分割） | Coefficients.cs:1743:LostLandNationalism | Diplomacy.cs Ideology.cs PoliticalQuantities.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LostLandHalflifeQuarters | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H062 | S24 | 復讐心 | 文書 L5213（粒＝見出し・未分割） | Coefficients.cs:1562:RevanchismTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 HighTensionDecayRate | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H063 | S24 | 講和後の休戦（廃止の見出し） | 文書 L5218（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 MinTruceYears・TruceYearsPerFullVictory | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H064 | S24 | 多正面戦争 | 文書 L5223（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H065 | S24 | 内戦は戦争として戦われる | 文書 L5227（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;MultiNationSim.cs:58:MultiNationSim | Diplomacy.cs MultiNationSim.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CivilWarAsWar | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H066 | S24 | 内戦の経済ダメージ | 文書 L5233（粒＝見出し・未分割） | Coefficients.cs:2011:CivilWarEconomyDisruption | EconomyStep.cs Politics.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CivilWarCivilianToll | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H067 | S24 | 分離した州の再征服 | 文書 L5239（粒＝見出し・未分割） | — | SelfPlay.cs Territory.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ForceUnlockMilitancy・ForceUnlockRate・ForceUnlockTreasuryCost | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H068 | S24 | 理想の軍隊の形 | 文書 L5245（粒＝見出し・未分割） | Coefficients.cs:2252:AllyAttentionDiscount;Coefficients.cs:2250:AttentionHostilityFloor;Coefficients.cs:2251:AttentionTensionHalf;Coefficients.cs:2257:InternalGarrisonPerPop | ArmyShape.cs BilateralMarket.cs Diplomacy.cs Discovery.cs EconomyStep.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ArmyShapeEnabled・NavalProcurementFromShape | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H069 | S24 | 毎月の戦闘が年表に出る | 文書 L5258（粒＝見出し・未分割） | Coefficients.cs:1686:BattleClearMargin;Coefficients.cs:1685:BattleDecisiveMargin;Coefficients.cs:1684:BattleLogEnabled | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H070 | S24 | 通商保護の必要量（守る海の交易）（廃止の見出し） | 文書 L5263（粒＝見出し・未分割） | — | ArmyShape.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H071 | S24 | 艦隊を軍事ゲージへ合算 | 文書 L5270（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ArmyShapeGauge | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H072 | S24 | 駐屯州での軍需調達 | 文書 L5277（粒＝見出し・未分割） | Coefficients.cs:2474:GarrisonProcurement | Military.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H073 | S24 | 海軍の運用燃料 | 文書 L5283（粒＝見出し・未分割） | Coefficients.cs:3535:EraNavalFuel;Coefficients.cs:3534:NavalFuelFillSmoothing;Coefficients.cs:3529:NavalFuelPerGauge;Coefficients.cs:3533:NavalFuelSortieFloor | Diplomacy.cs Military.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H074 | S24 | 屈服の確率化 | 文書 L5289（粒＝見出し・未分割）；同じ題 L5828（S25） | Coefficients.cs:1877:SubmitProbabilistic | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H075 | S24 | 軍需調達の一律按分（廃止の見出し） | 文書 L5295（粒＝見出し・未分割） | — | EconomyStep.cs Politics.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 MilitaryProcureProRata | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H076 | S24 | 割譲の距離が技術で伸びる（廃止の見出し） | 文書 L5302（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 CessionRangeTechKm | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H077 | S24 | 開戦そのものの政治的帰結 | 文書 L5309（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 WarStartPoliticsScale | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H078 | S24 | 戦勝点の分解（診断）（廃止の見出し） | 文書 L5315（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 WarScoreDecomposition | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H079 | S24 | 民兵の戦力 | 文書 L5322（粒＝見出し・未分割） | Coefficients.cs:3055:MilitiaCombatValue | Diplomacy.cs Politics.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H080 | S24 | 戦争台帳と終戦記録 | 文書 L5327（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H081 | S24 | 戦争タブの戦費と犠牲者 | 文書 L5331（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H082 | S24 | 同一ペアの再戦カウンタ | 文書 L5335（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H083 | S24 | 艦隊と乗員のどちらが効いているか | 文書 L5339（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |
| S24-H084 | S24 | 戦死の計器 | 文書 L5343（粒＝見出し・未分割）；同じ題 L7115（S29） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | cesd 段3（612 行）・統一戦争の置き換え（作者 2026-10-20 原文「統一戦争は名指ししないという原則に反しているので、廃止寄りです。」） |

### S25 外交

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S25-H001 | S25 | 緊張の台帳と開戦時計 | 文書 L5398（粒＝見出し・未分割） | Coefficients.cs:1415:WarDeclareTension;Coefficients.cs:1265:WarTriggerMonths | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H002 | S25 | 緊張の内訳台帳 | 文書 L5404（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Diplomacy.cs:679:TensionReasonJa | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H003 | S25 | 脅威の緊張（大国であること自体が敵を作る） | 文書 L5408（粒＝見出し・未分割） | Coefficients.cs:1599:DependentThreatWeight | ArmyShape.cs Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ThreatTensionRate・ThreatUsesColonyValue | — |
| S25-H004 | S25 | 熟練度が生む緊張 | 文書 L5415（粒＝見出し・未分割） | Coefficients.cs:2225:SkillTensionWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H005 | S25 | 思想の緊張 | 文書 L5421（粒＝見出し・未分割） | Coefficients.cs:1574:IdeologyAffinityRelief | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 IdeologyTensionRate | — |
| S25-H006 | S25 | 文明の距離（民族・法体系・宗教） | 文書 L5427（粒＝見出し・未分割） | Coefficients.cs:1577:EthnicAffinityRelief;Coefficients.cs:1581:ReligionAffinityRelief;Coefficients.cs:1580:ReligionHeresyBonus | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EnableCivilizationTension・EthnicTensionRate・LawTensionRate・ReligionTensionRate | — |
| S25-H007 | S25 | 同じ宗教の住民の保護 | 文書 L5433（粒＝見出し・未分割） | Coefficients.cs:1587:ProtectorInterventionWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ProtectorTensionRate | — |
| S25-H008 | S25 | 交易路の扼圧 | 文書 L5439（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RouteDependenceTensionRate | — |
| S25-H009 | S25 | アヘン禁令の緊張 | 文書 L5444（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Contraband.cs Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 OpiumBanTensionRate | — |
| S25-H010 | S25 | アヘン輸出禁止のAI | 文書 L5450（粒＝見出し・未分割） | Coefficients.cs:1501:OpiumExportPressureTension;Coefficients.cs:1502:OpiumExportPressureThreshold | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H011 | S25 | 緊張の自然減衰 | 文書 L5455（粒＝見出し・未分割） | Coefficients.cs:2881:FrozenTensionDecayRate;Coefficients.cs:1261:HighTensionBand;Coefficients.cs:1262:TensionDecay | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 HighTensionDecayRate | — |
| S25-H012 | S25 | 遺恨ストックによる解凍の遅れ | 文書 L5461（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 LostLandHalflifeQuarters | — |
| S25-H013 | S25 | ハイエナ抑制 | 文書 L5466（粒＝見出し・未分割） | Coefficients.cs:1767:DefeatShockProtectMin;Coefficients.cs:1766:DefeatShockTensionRelief | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H014 | S25 | 凍結ペアの解凍 | 文書 L5472（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 FrozenTensionDecays | — |
| S25-H015 | S25 | 復讐心（失地の記憶） | 文書 L5478（粒＝見出し・未分割） | Coefficients.cs:1562:RevanchismTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H016 | S25 | 国家ランク（列強・準列強・中堅・弱小） | 文書 L5483（粒＝見出し・未分割） | Coefficients.cs:1655:GreatPowerCount;Coefficients.cs:1650:GreatPowerScoreFraction;Coefficients.cs:1674:MiddlePowerCount;Coefficients.cs:1651:MinorPowerScoreFraction | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RankBracketsMerged | — |
| S25-H017 | S25 | 覇権国の特定と反覇権の封じ込め | 文書 L5493（粒＝見出し・未分割） | Coefficients.cs:1270:AntiHegemonAllianceChance;Coefficients.cs:1271:AntiHegemonExtraAlliances;Coefficients.cs:1277:HegemonContainmentMax;Coefficients.cs:1276:HegemonContainmentTrade | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H018 | S25 | 防衛同盟 | 文書 L5499（粒＝見出し・未分割） | Coefficients.cs:1263:FriendlyTensionMax;Coefficients.cs:1267:MaxAlliances;Coefficients.cs:1264:RivalTensionThreshold | Diplomacy.cs Model.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H019 | S25 | 同盟に「届く」ことの要求 | 文書 L5505（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H020 | S25 | 同盟の破棄 | 文書 L5509（粒＝見出し・未分割） | Coefficients.cs:1602:AllianceBreakTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H021 | S25 | 同盟による参戦（義務は無い） | 文書 L5514（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H022 | S25 | 参戦要請と信義の傷 | 文書 L5522（粒＝見出し・未分割） | Coefficients.cs:1756:AllyRefusalTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H023 | S25 | 開戦と強制禁輸 | 文書 L5527（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H024 | S25 | 到達できない相手には宣戦しない | 文書 L5532（粒＝見出し・未分割） | Coefficients.cs:1794:DefendProjectionMin | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 WarNeedsReach | — |
| S25-H025 | S25 | AI同士の屈服 | 文書 L5537（粒＝見出し・未分割） | Coefficients.cs:1964:AISubmitLegitCost;Coefficients.cs:1876:AISubmitMilRatio;Coefficients.cs:1418:ConcedeTensionTarget | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SubmitCountsAllies | — |
| S25-H026 | S25 | プレイヤーへの最後通牒 | 文書 L5543（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H027 | S25 | 緊張ポップアップ | 文書 L5547（粒＝見出し・未分割） | Coefficients.cs:1419:TensionPopupThreshold | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H028 | S25 | 戦争見通しの提示 | 文書 L5552（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H029 | S25 | 挑発と緩和 | 文書 L5557（粒＝見出し・未分割） | Coefficients.cs:1687:ActionTensionStep;Coefficients.cs:1688:TensionActionRampMonths | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TensionActionCooldownMonths | — |
| S25-H030 | S25 | AIの挑発回避と自衛的宥和 | 文書 L5563（粒＝見出し・未分割） | Coefficients.cs:1788:AppeaseMilitaryRatio | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 InsultChance・InsultTension | — |
| S25-H031 | S25 | 挑発の近隣波及 | 文書 L5569（粒＝見出し・未分割） | Coefficients.cs:1695:ProvokeProximitySpread;Coefficients.cs:1468:ProximityTensionRange | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H032 | S25 | 外交行動の国内帰結 | 文書 L5574（粒＝見出し・未分割） | Coefficients.cs:1692:TensionPoliticsScale | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H033 | S25 | 禁輸 | 文書 L5580（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H034 | S25 | 軍需輸出の禁止 | 文書 L5585（粒＝見出し・未分割） | Coefficients.cs:2179:MilitaryExportAccessFloor;Coefficients.cs:2177:MilitaryExportTensionThreshold | BilateralMarket.cs Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H035 | S25 | 通商破壊 | 文書 L5591（粒＝見出し・未分割） | Coefficients.cs:2612:RiskLambda | BilateralMarket.cs Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H036 | S25 | 拿捕の連合按分 | 文書 L5603（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 PrizeLossProrated | — |
| S25-H037 | S25 | 海軍シェアの地理的創発（廃止の見出し） | 文書 L5609（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 NavalShareBase・NavalShareColonyWeight・NavalShareFromUpkeep・NavalShareMax・NavalShareOceanBonus | — |
| S25-H038 | S25 | 艦隊の摩耗と修理 | 文書 L5613（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs EconomyStep.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H039 | S25 | 属国（従属関係） | 文書 L5618（粒＝見出し・未分割）；同じ題 L6287（S27） | Coefficients.cs:1590:DependencyAlignmentRelief | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EnableDependency | — |
| S25-H040 | S25 | 貢納 | 文書 L5623（粒＝見出し・未分割）；同じ題 L6292（S27） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VassalTributeRate | — |
| S25-H041 | S25 | 属国の離反 | 文書 L5628（粒＝見出し・未分割）；同じ題 L6297（S27） | Diplomacy.cs:396:DefectionTensionResidue;Coefficients.cs:1592:VassalDefectionMilitancy;Coefficients.cs:1591:VassalHoldScale | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VassalDefectionUsesSubmitMeasure | — |
| S25-H042 | S25 | 宗主が抱えられる数の上限 | 文書 L5639（粒＝見出し・未分割） | Coefficients.cs:1600:MaxDependents | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H043 | S25 | 列強の介入と和平提案 | 文書 L5645（粒＝見出し・未分割） | Coefficients.cs:1809:InterventionMotiveRatio;Coefficients.cs:1814:InterventionRivalTension;Coefficients.cs:1813:InterventionSubmitBar;Coefficients.cs:1815:InterventionWarMinMonths | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PeaceInterventionEnabled | — |
| S25-H044 | S25 | プレイヤーの調停 | 文書 L5653（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H045 | S25 | 調停の拒否と参戦 | 文書 L5658（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 InterventionRefusalSymmetric・InterventionRefusalWar | — |
| S25-H046 | S25 | 講和の中身 | 文書 L5663（粒＝見出し・未分割） | Coefficients.cs:1526:ColonyCaptureBudget;Coefficients.cs:1531:ColonyCaptureMax;Coefficients.cs:1601:GovCostVassalThreshold;Coefficients.cs:1438:ReparationGdpFraction | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AbstractTargetReparations | — |
| S25-H047 | S25 | 割譲の投射圏 | 文書 L5668（粒＝見出し・未分割）；同じ題 L6242（S27） | Coefficients.cs:1557:CessionMaxDistanceKm;Coefficients.cs:1414:SeaInvasionControlMin | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonyCaptureReachGate | — |
| S25-H048 | S25 | 植民地格の州の複数割譲 | 文書 L5674（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonialRegionCession | — |
| S25-H049 | S25 | 条約港（不平等条約） | 文書 L5680（粒＝見出し・未分割） | Coefficients.cs:2762:ForeignRuleFromTreaty;Coefficients.cs:1023:TreatyPortReparationMult;Coefficients.cs:1018:TreatyPortWinnerDistanceKm | Diplomacy.cs InternationalMarket.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TreatyPortTariffLoss | — |
| S25-H050 | S25 | 革命圧力による早期降伏 | 文書 L5701（粒＝見出し・未分割） | Coefficients.cs:1747:RevolutionSurrenderWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H051 | S25 | 休戦（規模で決める器・いまは0年）（廃止の見出し） | 文書 L5706（粒＝見出し・未分割） | Coefficients.cs:1436:PostWarTension | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 TruceYearsPerExhaustion | — |
| S25-H052 | S25 | 敗戦・戦勝の国内帰結 | 文書 L5713（粒＝見出し・未分割） | Coefficients.cs:1645:OfficerCasualtyShare | Diplomacy.cs Politics.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H053 | S25 | 砲艦外交（屈服か戦争か） | 文書 L5719（粒＝見出し・未分割） | Coefficients.cs:1793:ColonizeAnnexMilitancy;Coefficients.cs:1697:ColonizeSubmitFloorMilitary;Coefficients.cs:1696:ColonizeSubmitRatio | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H054 | S25 | 侵略の一本化 | 文書 L5725（粒＝見出し・未分割） | Coefficients.cs:1535:ColonizeWillSuperiorityDiscount;Coefficients.cs:1450:ExpansionWillCap;Coefficients.cs:1460:InvadeMilitaryFactor;Coefficients.cs:2310:InvadeSeaControlMin | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H055 | S25 | 遠征技術のゲート | 文書 L5734（粒＝見出し・未分割）；同じ題 L6192（S27） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ExpeditionTechFloor・ExpeditionTechLevel | — |
| S25-H056 | S25 | AIの侵略と自制 | 文書 L5739（粒＝見出し・未分割） | Coefficients.cs:1541:AiColonizeChance;Coefficients.cs:1789:HegemonRestraintFloor | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H057 | S25 | 勢力圏の衝突 | 文書 L5746（粒＝見出し・未分割） | Coefficients.cs:1698:ColonizeRivalTension;Coefficients.cs:1468:ProximityTensionRange | Diplomacy.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H058 | S25 | 戦力の投射 | 文書 L5758（粒＝見出し・未分割） | Coefficients.cs:2307:DistanceWarDecay;Coefficients.cs:2309:HomeDefenseBonus | Diplomacy.cs Fiscal.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H059 | S25 | 投射の起点を港州の鎖へ | 文書 L5764（粒＝見出し・未分割） | Coefficients.cs:2765:ProjectionRuleMin;Coefficients.cs:2767:ProjectionUsesLand | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S25-H060 | S25 | 陸続きの判定 | 文書 L5769（粒＝見出し・未分割） | Landmass.cs:40:Landmass;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs Landmass.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H061 | S25 | 報復関税 | 文書 L5774（粒＝見出し・未分割） | Coefficients.cs:708:TariffReactionMax;Coefficients.cs:707:TariffRetaliation | Diplomacy.cs Government.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H062 | S25 | 戦う価値の判定 | 文書 L5788（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SpoilsWorthThreshold | — |
| S25-H063 | S25 | 厭戦による開戦の抑止 | 文書 L5793（粒＝見出し・未分割） | Coefficients.cs:1433:WarWearyThreshold | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H064 | S25 | 初期条件（史実のライバルと従属） | 文書 L5798（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H065 | S25 | 独立国との講和相当状態 | 文書 L5802（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;MultiNationSim.cs:58:MultiNationSim | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H066 | S25 | 会議外交 | 文書 L5807（粒＝見出し・未分割） | Coefficients.cs:1566:ConcertReparationRestraint;Coefficients.cs:1567:ConcertRestraintMax | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H067 | S25 | 事件ログと終戦記録 | 文書 L5818（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | — |
| S25-H068 | S25 | 割譲圏の技術延伸と渡海の摩擦 | 文書 L5822（粒＝見出し・未分割） | Coefficients.cs:1551:CessionSeaCrossingWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない）；消えた係数 CessionRangeTechKm | — |
| S25-H069 | S25 | 離反の物理判定 | 文書 L5834（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 VassalDefectionUsesSubmitMeasure | — |
| S25-H070 | S25 | 緊張動詞の対称化 → ● 2026-09-28 (83) に点火して旗ごと削除（上の「挑発と緩和」「AIの挑発回避と自衛的宥和」）（廃止の見出し） | 文書 L5839（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 SymmetricTensionVerb | — |
| S25-H071 | S25 | 開戦の政治的帰結 | 文書 L5845（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 WarStartPoliticsScale | — |
| S25-H072 | S25 | 報復関税の政治化（廃止の見出し） | 文書 L5851（粒＝見出し・未分割） | — | Government.cs PolicyPreferences.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 TariffRetaliationViaPolitics | — |
| S25-H073 | S25 | 外交コア（過不足ベクトルと関係値） | 文書 L5858（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | — |
| S25-H074 | S25 | 介入の計器 | 文書 L5864（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない）；消えた係数 PeaceInterventionProbe | — |
| S25-H075 | S25 | 同一ペア再戦の計器 | 文書 L5869（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | — |

### S26 植民地の経営

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S26-H001 | S26 | 開始時の一括昇格（植民地は州になる） | 文書 L5943（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColoniesAreRegions | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H002 | S26 | 昇格した州の実体化 | 文書 L5948（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ImmigrantWorkerBase | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H003 | S26 | 植民地の大農園と不在地主（R1＋c6・2026-10-02 (173)） | 文書 L5955（粒＝見出し・未分割） | Coefficients.cs:2193:PlantationRentShare | Colonial.cs EconomyStep.cs InternationalMarket.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonyPlantationRent | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H004 | S26 | 内陸の植民地は港にならない | 文書 L5960（粒＝見出し・未分割） | Simulation.cs:62:Simulation;Landmass.cs:40:Landmass | Landmass.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H005 | S26 | 州の格（中核・編入・植民地格） | 文書 L5964（粒＝見出し・未分割） | Coefficients.cs:3289:CoreHeldYears | Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H006 | S26 | 代替レシピは州の構築経路で入る | 文書 L5970（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H007 | S26 | 行政は全州が買い、遠いほど高い | 文書 L5974（粒＝見出し・未分割） | Coefficients.cs:2307:DistanceWarDecay | Diplomacy.cs EconomyStep.cs Fiscal.cs Politics.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AdminCostPerPop・UpkeepDistanceGeneral | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H008 | S26 | 維持費①：駐留の俸給 → **削除（2026-10-01 (140)・col-16）**（廃止の見出し） | 文書 L5980（粒＝見出し・未分割） | — | Colonial.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 ColonyUpkeepBase・ColonyUpkeepMilitancyScale | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H009 | S26 | 維持費②：距離に比例する物理消費 | 文書 L5988（粒＝見出し・未分割） | Diplomacy.cs:62:ColonyMassUnits | Colonial.cs Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ColonyUpkeepCoalPerDist・ColonyUpkeepFoodPerDist | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H010 | S26 | 補給線は汽船で焚く（民間の汽船と同じ状態と物理） | 文書 L5994（粒＝見出し・未分割） | Coefficients.cs:3491:EraColonyCoal | Colonial.cs Transport.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H011 | S26 | 補給線の燃料は石炭か重油か | 文書 L6000（粒＝見出し・未分割） | Coefficients.cs:2505:ColonyFuelOilEnergyRatio | Colonial.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H012 | S26 | 維持費③：海運の占有 | 文書 L6005（粒＝見出し・未分割） | Coefficients.cs:2508:ColonyShipOccupyPerDist | BilateralMarket.cs Colonial.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H013 | S26 | 海路距離と運河の短絡 | 文書 L6013（粒＝見出し・未分割） | — | Colonial.cs SeaGraph.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H014 | S26 | マラリア地とキニーネ | 文書 L6017（粒＝見出し・未分割）；同じ題 L6673（S28） | Coefficients.cs:1464:MalariaQuininePerColony | Colonial.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H015 | S26 | 国家調達の価格ゲート | 文書 L6024（粒＝見出し・未分割） | — | Colonial.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 StateProcurePriceGate | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H016 | S26 | 反帝世論 | 文書 L6029（粒＝見出し・未分割） | Coefficients.cs:1818:ColonialUnrestHomeWeight;Coefficients.cs:1521:ColoniesForFullColonial | Colonial.cs SelfPlay.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 AntiImperialCoversRegions | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H017 | S26 | 帝国の規模が脅威評価に入る | 文書 L6035（粒＝見出し・未分割） | Coefficients.cs:1599:DependentThreatWeight | Diplomacy.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ThreatUsesColonyValue | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H018 | S26 | 海軍への配分が陸と海の必要量で決まる | 文書 L6041（粒＝見出し・未分割） | Coefficients.cs:2306:AbstractNavalShare | ArmyShape.cs Diplomacy.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H019 | S26 | 植民地財は普通の交易に載る | 文書 L6046（粒＝見出し・未分割） | — | BilateralMarket.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 TradeColonialGoods | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |

### S27 支配度と分離独立と領土の移転

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S27-H009 | S27 | 支配度＝ストック | 文書 L6051（粒＝見出し・未分割） | Coefficients.cs:2754:DominanceMatureYears;Coefficients.cs:2752:DominanceSpeed | Diplomacy.cs Model.cs Politics.cs Privileges.cs Simulation.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 DominanceIsStock | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H010 | S27 | 昇格した植民地は支配度0から始まる | 文書 L6057（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H011 | S27 | 併合年のデータで格と支配度を決める（B14） | 文書 L6061（粒＝見出し・未分割） | Coefficients.cs:2775:HeldSinceFromData | Simulation.cs world.json | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H012 | S27 | 支配の4区分（国家・在地・外国・無統治） | 文書 L6066（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Model.cs Politics.cs Simulation.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない）；消えた係数 RuleSharesEnabled | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H013 | S27 | 4区分で公共財の到達と課税・徴兵の基盤を割る | 文書 L6073（粒＝見出し・未分割） | Diplomacy.cs:22:Military;Simulation.cs:62:Simulation | Diplomacy.cs Fiscal.cs Military.cs Politics.cs Simulation.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない）；消えた係数 RuleSharesReach・RuleSharesTaxBase | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H014 | S27 | 条約港に外国の持分を積む | 文書 L6078（粒＝見出し・未分割） | Coefficients.cs:2762:ForeignRuleFromTreaty | Diplomacy.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H015 | S27 | 戦力投射の起点を実効支配の港へ広げる | 文書 L6083（粒＝見出し・未分割） | Coefficients.cs:2765:ProjectionRuleMin;Coefficients.cs:2767:ProjectionUsesLand | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H016 | S27 | 平時の徴募の遺恨 | 文書 L6088（粒＝見出し・未分割） | Coefficients.cs:2097:ConscriptGrievanceRate;Coefficients.cs:2769:PeacetimeConscriptGrievance | Military.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H017 | S27 | 州パネルの支配の半円 | 文書 L6093（粒＝見出し・未分割） | — | — | — | — | この波の線が埋める (888) c75ce | 文書の記号 ◇（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H018 | S27 | 分離圧力 | 文書 L6098（粒＝見出し・未分割） | Coefficients.cs:393:CultureSecessionWeight;Coefficients.cs:2724:SecessionHoldCoercion;Coefficients.cs:402:SecessionHoldPerPop;Coefficients.cs:392:SecessionPressureScale | NationBrief.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H019 | S27 | 文化距離は民族の実体から出る | 文書 L6104（粒＝見出し・未分割） | Coefficients.cs:2733:CultureDistanceFromEthnic | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H020 | S27 | 民族主因の分離 | 文書 L6110（粒＝見出し・未分割） | Coefficients.cs:947:MinorityMilitancy;Coefficients.cs:2725:SecessionCultureAdditive | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H021 | S27 | 国民意識の分離割引 | 文書 L6115（粒＝見出し・未分割） | Coefficients.cs:3508:SecessionConsciousnessWeight | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H022 | S27 | 保持力に投射が掛かる（遠い領土は保ちにくい） | 文書 L6120（粒＝見出し・未分割） | Coefficients.cs:2307:DistanceWarDecay;Coefficients.cs:2737:SecessionHoldProjection | Diplomacy.cs Fiscal.cs Politics.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H023 | S27 | 同時多発は保持力を割る | 文書 L6125（粒＝見出し・未分割） | Coefficients.cs:2736:SecessionHoldSharedArmy | Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H024 | S27 | 同時独立（分離は戦争になる） | 文書 L6130（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Politics.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 SecessionAsWar | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H025 | S27 | 分離戦の決着 | 文書 L6135（粒＝見出し・未分割） | Coefficients.cs:1522:WhitePeaceScore | Diplomacy.cs MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 CivilWarAsWar | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H026 | S27 | 分離する州は開戦の時点で新しい国になる | 文書 L6140（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Territory.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 SecessionSpawnsNation・SoldierDefectionByMilitancy | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H027 | S27 | 独立＝旧少数派がこの国の中核になる | 文書 L6146（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs Territory.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H028 | S27 | 建国した国は文明を引き継ぐ | 文書 L6151（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H029 | S27 | 建国名の解決 | 文書 L6155（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs SecessionNames.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H030 | S27 | 独立直後の講和相当 | 文書 L6159（粒＝見出し・未分割） | Coefficients.cs:1436:PostWarTension | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 MinTruceYears・TruceYearsPerExhaustion | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H031 | S27 | 残骸国家の自然再併合 | 文書 L6165（粒＝見出し・未分割） | Coefficients.cs:3521:RumpReabsorbYears | MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H032 | S27 | 分離で失った州は武力でしか戻らない | 文書 L6170（粒＝見出し・未分割） | — | Politics.cs SelfPlay.cs Territory.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない）；消えた係数 ForceUnlockMilitancy・ForceUnlockRate・ForceUnlockTreasuryCost | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H033 | S27 | 侵略＝弱小への唯一の動詞 | 文書 L6176（粒＝見出し・未分割） | Coefficients.cs:1460:InvadeMilitaryFactor;Coefficients.cs:1600:MaxDependents | Diplomacy.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H034 | S27 | 侵略の代価＝国力比 | 文書 L6182（粒＝見出し・未分割） | Coefficients.cs:1697:ColonizeSubmitFloorMilitary;Coefficients.cs:1696:ColonizeSubmitRatio;Coefficients.cs:1535:ColonizeWillSuperiorityDiscount;Coefficients.cs:1450:ExpansionWillCap | Diplomacy.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H035 | S27 | 渡海のゲート（船・制海権・糧食） | 文書 L6197（粒＝見出し・未分割） | Coefficients.cs:1465:InvadeCannedBase;Coefficients.cs:1466:InvadeCannedPerMilitary;Coefficients.cs:2310:InvadeSeaControlMin;Coefficients.cs:1458:InvadeShipsBase | World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 InvadeShipDistanceScale・InvadeShipSeaDistance | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H036 | S27 | キニーネ不足は遠征軍が溶ける | 文書 L6202（粒＝見出し・未分割） | Coefficients.cs:604:EpidemicSeverity | World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H037 | S27 | 屈服か戦争か | 文書 L6207（粒＝見出し・未分割） | Coefficients.cs:1793:ColonizeAnnexMilitancy | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H038 | S27 | 接収した植民地はその場で州になる | 文書 L6212（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Simulation.cs:62:Simulation | Diplomacy.cs MultiNationSim.cs Simulation.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 CapturedColoniesAreRegions | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H039 | S27 | 侵略は他の列強との緊張を上げる | 文書 L6218（粒＝見出し・未分割） | Coefficients.cs:1698:ColonizeRivalTension | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H040 | S27 | 近接緊張 | 文書 L6223（粒＝見出し・未分割） | Coefficients.cs:1469:ProximityTensionMax;Coefficients.cs:1468:ProximityTensionRange | Diplomacy.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H041 | S27 | AIの侵略 | 文書 L6229（粒＝見出し・未分割） | Coefficients.cs:1541:AiColonizeChance;Coefficients.cs:1539:ColonizeValueGdpPer100k;Coefficients.cs:1789:HegemonRestraintFloor | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H042 | S27 | 割譲の渡海の割り増し | 文書 L6247（粒＝見出し・未分割） | Coefficients.cs:1551:CessionSeaCrossingWeight;Coefficients.cs:1414:SeaInvasionControlMin | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H043 | S27 | 植民地格の州は講和で複数まとめて割譲される | 文書 L6252（粒＝見出し・未分割） | Coefficients.cs:1526:ColonyCaptureBudget;Coefficients.cs:1527:ColonyCaptureCostBase;Coefficients.cs:1531:ColonyCaptureMax;Coefficients.cs:1529:ColonyCapturePopCost | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 ColonialRegionCession・ColonyDevCostWeight | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H044 | S27 | 奪取費の開発度を州から読む（B2） | 文書 L6258（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ○（9-26・写さない）；消えた係数 ColonyDevCostFromRegion | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H045 | S27 | 奪取の射程ゲート | 文書 L6263（粒＝見出し・未分割） | Coefficients.cs:1533:ColonyCaptureDistanceWeight | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ◐（9-26・写さない）；消えた係数 ColonyCaptureReachGate | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H046 | S27 | 武力で奪った土地ほど従わない | 文書 L6269（粒＝見出し・未分割） | Coefficients.cs:2883:ConquestColonyMilitancy | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ◐（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H047 | S27 | 征服は中核民族を薄める（廃止の見出し） | 文書 L6275（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ~（9-26・写さない）；消えた係数 ConquestForeignWeight | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H048 | S27 | 講和で狙う土地への関心 | 文書 L6282（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;Simulation.cs:53:World | Diplomacy.cs MultiNationSim.cs Simulation.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ◐（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H049 | S27 | 遠すぎる国は属国にできない | 文書 L6308（粒＝見出し・未分割） | Coefficients.cs:1023:TreatyPortReparationMult;Coefficients.cs:1018:TreatyPortWinnerDistanceKm | Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H050 | S27 | 議会が従属条項を拒む | 文書 L6313（粒＝見出し・未分割） | Coefficients.cs:1787:TreatyRatificationWeight | CrownParliament.cs Diplomacy.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H051 | S27 | 初期の史実従属関係 | 文書 L6323（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs world.json | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H052 | S27 | 変名（条件を満たす州を集めると国名が変わる） | 文書 L6327（粒＝見出し・未分割） | — | CultureRealms.cs Formations.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 CultureFormationGate・CultureFormationNeedsGain・EnableFormations | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H053 | S27 | 変体の成立判定 | 文書 L6332（粒＝見出し・未分割） | — | Formations.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H054 | S27 | 段位の一方向性 | 文書 L6336（粒＝見出し・未分割） | — | Formations.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H055 | S27 | 世界に同名は一つ | 文書 L6340（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H056 | S27 | 成立時の遷都 | 文書 L6344（粒＝見出し・未分割） | — | Formations.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 FormationCapitalMove | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H057 | S27 | 変名の正統性ボーナス | 文書 L6349（粒＝見出し・未分割） | Coefficients.cs:3277:FormationLegitimacyBoost | Formations.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない） | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |
| S27-H058 | S27 | 統一戦争（プレイヤーの動詞） | 文書 L6354（粒＝見出し・未分割） | Coefficients.cs:3109:UnificationNatioShare | Formations.cs World.cs | — | — | この波の線が埋める (888) c75ce | 文書の記号 ●（9-26・写さない）；消えた係数 UnificationWarEnabled | 割譲の支配度の段 C-0・C-1（612 行 ②⑧）・cesd（612 行） |

### S26 植民地の経営

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S26-H020 | S26 | 州間の移民 | 文書 L6365（粒＝見出し・未分割） | Coefficients.cs:2683:MigrationDistanceScale;Coefficients.cs:915:RegionMobility;Coefficients.cs:917:TransportFriction | Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 MigrationSecondDestShare | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H021 | S26 | 国家間の移民 | 文書 L6371（粒＝見出し・未分割） | Coefficients.cs:2680:CrossBorderOppGapThreshold;Coefficients.cs:3490:EraMigrationScale;Coefficients.cs:2683:MigrationDistanceScale | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CrossBorderMigrationByOpportunity | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H022 | S26 | 移民経路の安全弁 | 文書 L6376（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H023 | S26 | 州の文化・宗教と国の主流 | 文書 L6380（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs Model.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H024 | S26 | 文化グループと宗教ファミリーの距離 | 文書 L6384（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H025 | S26 | 海峡と運河の扼者 | 文書 L6388（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim;MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H026 | S26 | 植民地の保存則検査 | 文書 L6392（粒＝見出し・未分割） | — | Validation.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H027 | S26 | 植民地の住民（旧経路） | 文書 L6396（粒＝見出し・未分割） | Model.cs:135:Savings;Simulation.cs:62:Simulation | Colonial.cs Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialExploitMortality・ColonialLiteracyGrowth・ColonialPayShare・ColonialPopGrowth | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H028 | S26 | 入植と州の格上げ（旧経路） | 文書 L6402（粒＝見出し・未分割） | Coefficients.cs:699:ColonyPromotionMinPop | Colonial.cs Politics.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonyPromotionCoreShare・ColonySettlerRate | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H029 | S26 | 植民地格の州への入植と編入（B1） | 文書 L6410（粒＝見出し・未分割） | — | Colonial.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialRegionSettlement | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H030 | S26 | 私的投資家による植民地開発 | 文書 L6416（粒＝見出し・未分割） | — | Colonial.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialDevStep・ColonialInfraConverge・ColonialInfraDepreciation・ColonialInfraPerColony・ColonialInvestFrac・ColonialOutputHalf・ColonialRailPerInfra・ColonialReturnScale・ColonialShipPerInfra | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H031 | S26 | 固有資源の本国市場への流入 | 文書 L6422（粒＝見出し・未分割） | Simulation.cs:62:Simulation | EconomyStep.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialImportLagMonths・ColonialProductionPerColony・ColonialUnrestSupplyHit | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H032 | S26 | 単一国用の植民地財の輸入 | 文書 L6428（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy;MultiNationSim.cs:58:MultiNationSim;Simulation.cs:62:Simulation | Colonial.cs Diplomacy.cs EconomyStep.cs MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H033 | S26 | 捕囚市場 | 文書 L6433（粒＝見出し・未分割） | — | Colonial.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H034 | S26 | 交易益の分配 | 文書 L6439（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialExtractionFraction・ColonialProfitInvestorShare・ColonialProfitMerchantShare | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H035 | S26 | 植民地の独立（旧経路） | 文書 L6445（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Colonial.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 ColonialHoldDistanceDecay・ColonialHoldScale・ColonialHoldSharedArmy・ColonialIndependenceMilitancy・ColonialMilitancyDecay・ColonialMilitancyGrowth・IndependenceShockYears | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H036 | S26 | 参政権の範囲法 | 文書 L6451（粒＝見出し・未分割） | World.cs:236:Policy | Fiscal.cs InterestGroups.cs Policy.cs Politics.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 SuffrageScopeGate・SuffrageScopeUnified・SuffrageScopeVoice | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H037 | S26 | 割譲圏の技術延伸 | 文書 L6457（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 CessionRangeTechKm | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H038 | S26 | 希少鉱物の総量に植民地を数える | 文書 L6462（粒＝見出し・未分割） | Coefficients.cs:2829:ScarceCountsIncludeColonies | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |
| S26-H039 | S26 | 先取りの奪取 | 文書 L6467（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない）；消えた係数 PreemptiveSeizeWeight | 植民地の稼ぎ（616・618 行 ⑥ 死の側の段3 の前提） |

### S28 環境と災害

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S28-H001 | S28 | 豊凶のロール | 文書 L6513（粒＝見出し・未分割） | Coefficients.cs:2359:HarvestVariation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H002 | S28 | 凶作は深く外れる | 文書 L6518（粒＝見出し・未分割） | Coefficients.cs:2361:HarvestSkew | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H003 | S28 | 不作は年を跨いで尾を引く | 文書 L6523（粒＝見出し・未分割） | Coefficients.cs:2362:HarvestPersistence | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H004 | S28 | 緯度帯の共通の天候 | 文書 L6528（粒＝見出し・未分割） | Coefficients.cs:2730:CenterIsFirstProvince;Coefficients.cs:2363:WeatherBandPersistence;Coefficients.cs:601:WeatherBandSize;Coefficients.cs:600:WeatherBandVariation | MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H005 | S28 | 財ごとの天候の効き方 | 文書 L6533（粒＝見出し・未分割） | — | Catalog.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H006 | S28 | 作物の病気 | 文書 L6537（粒＝見出し・未分割） | Coefficients.cs:595:CropDiseaseChance;Coefficients.cs:597:CropDiseaseRecovery;Coefficients.cs:596:CropDiseaseSeverity | Catalog.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H007 | S28 | 自給にも天候が掛かる | 文書 L6542（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | この波の線が埋める (890) c75ts | 文書の記号 ●（9-26・写さない）；消えた係数 SubsistenceHarvestFullExposure | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H008 | S28 | 食料充足と穀倉の備蓄 | 文書 L6547（粒＝見出し・未分割） | Coefficients.cs:2061:FoodFillSmoothing | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H009 | S28 | 飢饉が人を押し出す | 文書 L6578（粒＝見出し・未分割） | Coefficients.cs:2365:FaminePushMaxFracBoost | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EmigrationMaxFrac・FaminePushRegionBlend | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H010 | S28 | 疫病の発生 | 文書 L6588（粒＝見出し・未分割） | Coefficients.cs:603:EpidemicChance;Coefficients.cs:2026:EpidemicOnsetSanitationWeight | Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H011 | S28 | 疫病の致死率と衛生 | 文書 L6593（粒＝見出し・未分割） | Coefficients.cs:2080:ElecInfraSanitationWeight;Coefficients.cs:604:EpidemicSeverity;World.cs:9:SanitationMortalityReduction;Coefficients.cs:2073:SoapSanitationWeight | Model.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 EpidemicSanitationHalf・EpidemicSanitationWeight | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H012 | S28 | 疫病死も弱い層に集中する | 文書 L6598（粒＝見出し・未分割） | Coefficients.cs:606:EpidemicVulnConcentration | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H013 | S28 | 疫病の国内伝播 | 文書 L6603（粒＝見出し・未分割） | Coefficients.cs:2674:EpidemicIntraCountrySpread | World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H014 | S28 | 疫病の国際伝播 | 文書 L6608（粒＝見出し・未分割） | Coefficients.cs:608:EpidemicContagiousYears;Coefficients.cs:611:EpidemicTradeTransmit;Coefficients.cs:609:EpidemicTransmitChance;Coefficients.cs:610:EpidemicTransmitRange | MultiNationSim.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H015 | S28 | 主食は気候帯で変わる | 文書 L6613（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Climate.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H016 | S28 | 米の栽培適地 | 文書 L6617（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Climate.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H017 | S28 | 綿花の栽培適地 | 文書 L6621（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Climate.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H018 | S28 | 熱帯・特定産地の財 | 文書 L6625（粒＝見出し・未分割） | — | Catalog.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H019 | S28 | 州の資源賦存 | 文書 L6629（粒＝見出し・未分割） | Scenario.cs:69:NationConfig | NationConfig.cs Scenario.cs world.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H020 | S28 | 耕地の収穫逓減 | 文書 L6633（粒＝見出し・未分割） | Coefficients.cs:1033:LandDiminishExponent | EconomyStep.cs Territory.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H021 | S28 | 土地が稀少になるほど地代が上がる | 文書 L6638（粒＝見出し・未分割） | — | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RentScarcityElasticity | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H022 | S28 | 灌漑・干拓（耕地そのものを増やす） | 文書 L6643（粒＝見出し・未分割） | Diplomacy.cs:305:Megaprojects | Diplomacy.cs Megaprojects.cs megaprojects.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H023 | S28 | 鉱脈は有限の在庫 | 文書 L6647（粒＝見出し・未分割） | Coefficients.cs:3402:DepositGradeFloor;Coefficients.cs:3401:DepositGradeKnee;Coefficients.cs:3400:DepositYearsAtBirth | Discovery.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H024 | S28 | 産地の発見（大鉱床・旧 鉱脈の発見とゴールドラッシュ） | 文書 L6652（粒＝見出し・未分割） | Coefficients.cs:3403:BonanzaDepositYears;Coefficients.cs:1917:DiscoveryNeedHalfSite;Coefficients.cs:2372:GoldRushEndowment | Catalog.cs Discovery.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H025 | S28 | シルバーラッシュ（銀の後期産地） | 文書 L6658（粒＝見出し・未分割） | Coefficients.cs:1360:SilverRushEnabled | Simulation.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H026 | S28 | 鉱脈の品位を採掘の費用へ | 文書 L6663（粒＝見出し・未分割） | Coefficients.cs:1365:MineralCostFollowsGrade | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ○（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H027 | S28 | 鉱山と畑は工場のようには増やせない | 文書 L6668（粒＝見出し・未分割） | Coefficients.cs:3419:FarmScaleCap;Coefficients.cs:3418:MineralScaleCap;Coefficients.cs:3420:RgoScaleSpeedFactor | EconomyStep.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 RgoElasticityGate | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H028 | S28 | 代表都市の座標 | 文書 L6688（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Geo.cs Places.cs Simulation.cs places.json | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H029 | S28 | 災害の乱数の系列 | 文書 L6692（粒＝見出し・未分割） | Simulation.cs:62:Simulation;MultiNationSim.cs:58:MultiNationSim;DeterministicRng.cs:14:DeterministicRng | DeterministicRng.cs MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H030 | S28 | 工業都市の公害 | 文書 L6696（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 PollutionCapacityWeight | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H031 | S28 | 不作起因の恐慌の原因分解 | 文書 L6701（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |
| S28-H032 | S28 | 環境・災害の画面表示 | 文書 L6705（粒＝見出し・未分割） | Simulation.cs:53:World | Discovery.cs Simulation.cs World.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 鉱床（616 行 ⑨）・死の側の段3 の疫病（616 行 ⑥） |

### S29 較正と計測

| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク |
|---|---|---|---|---|---|---|---|---|---|---|
| S29-H001 | S29 | 決定論的な乱数 | 文書 L6743（粒＝見出し・未分割） | DeterministicRng.cs:14:DeterministicRng | DeterministicRng.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H002 | S29 | 日次の並列と逐次のビット一致 | 文書 L6747（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 POB_PAR | 9-3（612・616 行） |
| S29-H003 | S29 | 保存則の月次検査 | 文書 L6753（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Invariants.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H004 | S29 | お金の保存 | 文書 L6758（粒＝見出し・未分割） | — | Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H005 | S29 | 人口の保存 | 文書 L6764（粒＝見出し・未分割） | — | Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H006 | S29 | 非負と非有限の検査 | 文書 L6768（粒＝見出し・未分割） | — | Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H007 | S29 | 財の収支 | 文書 L6772（粒＝見出し・未分割） | — | Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H008 | S29 | 国債の保有台帳の検査 | 文書 L6776（粒＝見出し・未分割） | — | Fiscal.cs Invariants.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H009 | S29 | 基準線の取り直し | 文書 L6782（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H010 | S29 | 合格基準10項 | 文書 L6786（粒＝見出し・未分割） | — | Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H011 | S29 | 死蔵財の顔ぶれ | 文書 L6792（粒＝見出し・未分割） | — | Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H012 | S29 | 満足度・最小国の除外規則 | 文書 L6797（粒＝見出し・未分割） | — | Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H013 | S29 | 最小国の内訳 | 文書 L6808（粒＝見出し・未分割） | — | SelfPlay.cs Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H014 | S29 | 法の定義域の番人 | 文書 L6813（粒＝見出し・未分割） | — | SelfPlay.cs Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H015 | S29 | 植民地の保存 | 文書 L6817（粒＝見出し・未分割） | — | Validation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H016 | S29 | 国力スコア | 文書 L6821（粒＝見出し・未分割） | MultiNationSim.cs:12:Diplomacy | Diplomacy.cs MultiNationSim.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 ScoreTechWeight | 9-3（612・616 行） |
| S29-H017 | S29 | 国力の在庫化 | 文書 L6828（粒＝見出し・未分割） | Coefficients.cs:151:ScoreInertiaRate | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H018 | S29 | 自己対戦（1試合） | 文書 L6833（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H019 | S29 | 1914年の断面を捕まえる | 文書 L6837（粒＝見出し・未分割） | Coefficients.cs:2353:FidelityQuarters | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H020 | S29 | 1820年・1870年・1920年・1950年の断面 | 文書 L6842（粒＝見出し・未分割） | Coefficients.cs:2354:FidelityQuarters1820;Coefficients.cs:2355:FidelityQuarters1870;Coefficients.cs:2356:FidelityQuarters1920;Coefficients.cs:2357:FidelityQuarters1950 | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H021 | S29 | 断面で記録するもの | 文書 L6847（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H022 | S29 | 恐慌の検出と原因の分解 | 文書 L6853（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H023 | S29 | 大国8の史実基準の恐慌 | 文書 L6860（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H024 | S29 | 慢性赤字の判定 | 文書 L6866（粒＝見出し・未分割） | SelfPlay.cs:2025:LossFinancedRateThreshold;SelfPlay.cs:2024:LossRateMinOutputShare | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H025 | S29 | デッドロックの検出 | 文書 L6872（粒＝見出し・未分割） | SelfPlay.cs:2026:DeadlockFrozenQuarters | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H026 | S29 | 首位交代の計数 | 文書 L6877（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H027 | S29 | 跳躍ダイジェスト | 文書 L6881（粒＝見出し・未分割） | SelfPlay.cs:2415:JumpPopGrowthMax;SelfPlay.cs:2414:JumpRankSteps | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H028 | S29 | 走行中の異常検出 | 文書 L6887（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H029 | S29 | メモリ不足は握り潰さない | 文書 L6892（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H030 | S29 | バッチ自己対戦 | 文書 L6896（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H031 | S29 | 破綻した試合はハーネス不合格に数える | 文書 L6902（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H032 | S29 | 常勝・即死のフラグ | 文書 L6906（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H033 | S29 | 政体の分布と出現プレイ数 | 文書 L6910（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H034 | S29 | 法の多様性 | 文書 L6914（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H035 | S29 | 金本位離脱と国の出入りの集計 | 文書 L6919（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H036 | S29 | 戦争・介入・選挙・貿易の計器 | 文書 L6923（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H037 | S29 | 海軍力と艦隊ストックの順位 | 文書 L6929（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H038 | S29 | 史実の参照表（1914年・61カ国） | 文書 L6934（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H039 | S29 | 忠実度の照合 | 文書 L6938（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H040 | S29 | 旧ラベル基準の併走 | 文書 L6942（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H041 | S29 | 法一致の採点 | 文書 L6947（粒＝見出し・未分割） | HistoricalReference.cs:97:MilitarismDegreeOk | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H042 | S29 | 主権軸と宗主同一性 | 文書 L6953（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H043 | S29 | 判定の帯 | 文書 L6958（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H044 | S29 | 6つの次元 | 文書 L6963（粒＝見出し・未分割） | HistoricalReference.cs:88:ColonyCountTolerance;HistoricalReference.cs:144:DimensionTargetStrict;HistoricalReference.cs:86:GdpRankTolerance;HistoricalReference.cs:87:HomeProvinceMatchFrac | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H045 | S29 | 列強軸を再現率と特異度に割る | 文書 L6969（粒＝見出し・未分割） | Coefficients.cs:1675:GreatPowerCountForFidelity | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H046 | S29 | 貿易開放度の参照 | 文書 L6976（粒＝見出し・未分割） | — | HistoricalReference.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H047 | S29 | 参照エントリが少ない次元はゲートに載せない | 文書 L6981（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H048 | S29 | 照合できなかった国を明示する | 文書 L6985（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H049 | S29 | ゲート0（機械判定） | 文書 L6989（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 HarnessFailRateMax | 9-3（612・616 行） |
| S29-H050 | S29 | 忠実度は丸めない生値で出す | 文書 L6995（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H051 | S29 | 前回値との差分 | 文書 L6999（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 POB_FIDELITY_BASELINE | 9-3（612・616 行） |
| S29-H052 | S29 | 6次元50%ゲート | 文書 L7006（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H053 | S29 | 較正ハーネス（格子探索） | 文書 L7011（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 CalibQuarters | 9-3（612・616 行） |
| S29-H054 | S29 | 較正は密閉世界で走る | 文書 L7018（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Scenario.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H055 | S29 | 近代化の経路分類 | 文書 L7022（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Model.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H056 | S29 | 係数の環境変数上書き | 文書 L7026（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | Coefficients.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない）；消えた係数 POB_COEF | 9-3（612・616 行） |
| S29-H057 | S29 | 隔離測定ハーネス | 文書 L7033（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H058 | S29 | 計算量の台帳と番人 | 文書 L7044（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs SelfPlay.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H059 | S29 | 休眠フラグの番人 | 文書 L7050（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H060 | S29 | 既定0の係数の台帳 | 文書 L7056（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H061 | S29 | セーブとロード | 文書 L7062（粒＝見出し・未分割） | — | SaveGame.cs | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H062 | S29 | セーブ往復の検証 | 文書 L7069（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H063 | S29 | クイックセーブ／ロードとスロット | 文書 L7074（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H064 | S29 | 診断コマンド群（91の動詞・2026-11-01 (734) で使われない 56 本を削除） | 文書 L7079（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H065 | S29 | 保存量の漏れ狩り | 文書 L7085（粒＝見出し・未分割） | Simulation.cs:62:Simulation;MultiNationSim.cs:58:MultiNationSim | MultiNationSim.cs Simulation.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H066 | S29 | 保存量の旧計器（drift） | 文書 L7091（粒＝見出し・未分割） | Simulation.cs:62:Simulation | Simulation.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H067 | S29 | 機構発火の計器 | 文書 L7096（粒＝見出し・未分割） | — | MechanismProbe.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H068 | S29 | 計器そのものの罠 | 文書 L7102（粒＝見出し・未分割） | — | MechanismProbe.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H069 | S29 | 所要時間と到達性の計器（静的カウンタ） | 文書 L7110（粒＝見出し・未分割） | MultiNationSim.cs:58:MultiNationSim | BilateralMarket.cs BudgetBindDiag.cs MultiNationSim.cs TradeReachDiag.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H070 | S29 | 年代記 | 文書 L7121（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H071 | S29 | 政治のアブレーション | 文書 L7126（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H072 | S29 | 旧7カ国の検証ハーネス（引数なしの既定経路） | 文書 L7131（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H073 | S29 | ハーネス不合格の検査名の内訳 | 文書 L7136（粒＝見出し・未分割） | — | SelfPlay.cs | — | — | 未確かめ | 文書の記号 ◇（9-26・写さない） | 9-3（612・616 行） |
| S29-H074 | S29 | 説明台帳 | 文書 L7141（粒＝見出し・未分割） | World.cs:802:Explain | Explain.cs World.cs | — | — | 未確かめ | 文書の記号 ◐（9-26・写さない） | 9-3（612・616 行） |
| S29-H075 | S29 | エンジンの中の測定専用の腕（廃止の見出し） | 文書 L7154（粒＝見出し・未分割） | Coefficients.cs:3640:Coefficients | BilateralMarket.cs Coefficients.cs EconomyStep.cs | — | — | 未確かめ | 文書の記号 ~（9-26・写さない）；消えた係数 PopBudgetRelax・StateBuildUnitsPerMonth・SuffrageScopeVoice・TradeScaleUsesDesired | 9-3（612・616 行） |
| S29-H076 | S29 | テストは不変の文化で走る | 文書 L7160（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
| S29-H077 | S29 | クラウド計測（廃止の見出し） | 文書 L7166（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ~（9-26・写さない） | 9-3（612・616 行） |
| S29-H078 | S29 | 回帰テスト群 | 文書 L7170（粒＝見出し・未分割） | — | — | — | — | 未確かめ | 文書の記号 ●（9-26・写さない） | 9-3（612・616 行） |
