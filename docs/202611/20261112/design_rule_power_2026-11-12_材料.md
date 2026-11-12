# 〈下書き・作者採択〉支配の力の足し算の設計の材料（2026-11-12・読んだ所の file:line と抜き書きと数え）

- 木：`C:/Users/dirge/Documents/PriceOfBread`・main 9b18137a。読むだけ（ビルド・試験・シミュは回していない）。
- 行番号は書いた時点の物。実装の前に grep し直す。
- 「」は作者の原文だけ。〈 〉は AI の要約。

---

## M1 州の支配度（4 区分）の器と書き手

- `Model.cs:1098` `Region.Rule`＝長さ 4（0 国家・1 在地・2 外国・3 無統治）・既定 [1,0,0,0]。`:1099` `RuleInit`（開幕のデータ）。`:1114〜1115` `ForeignRulers`（string[]・鍵は BaseName）と `ForeignShares`（double[]）の並列配列。`:1101` の注〈段8で Dependency を Rule 読み出しへ畳む〉は未着手。
- 目標と寄せ方（四半期 1 回・`Politics.Step` の `Politics.cs:505` から `CheckSecession`）：`Politics.cs:1560〜1597`
  - `:1581` 無統治の目標 `tUngov = (1 − orderR) × (1 − RegionReach)`
  - `:1582` 在地 `tLocal = min(1 − tUngov, RuleInit[1] × Privileges.IntermediaryDegree(w))`
  - `:1583` 外国 `tForeign = ForeignShareSum(r)`（持分の合計・上限 1）
  - `:1584` 国家 `tState = max(0, 1 − tLocal − tForeign − tUngov)`＝**残り**（国家の力は式に入っていない）
  - `:1587〜1588` `Rule[k] += (t[k] − Rule[k]) × DominanceSpeed`（0.025）→ 正規化
  - `:1575〜1580` 州の秩序 P1（旗 `RuleOrderByRegion=1.0`・`Coefficients.cs:2768`）＝`Clamp01(rawOrder × m̄ ÷ m_r)`・m_r＝州の平均闘争度（**闘争度は既に無統治の目標に入っている**）
- `Politics.cs:1720〜1728` `RegionReach`＝首都の座標からの**大圏距離**で `1/(1 + DistanceWarDecay × km / 10000)`。輸送の費用は読まない。座標なしは 1.0。読み手＝`:1581` と計器 `InstrumentsRule.cs:95`・`InstrumentsCede.cs:141`。
- 外国の持分の書き手：`Politics.cs:1784〜1795` `AddForeignShare`（呼び手は講和の条約港 `Diplomacy.cs:3184` の 1 か所）・風化 `Diplomacy.cs:530〜552` `TreatyPortLapse`（列強の中央値に対する国力比 ÷ `TreatyPortLapseRatio` 0.5 × DominanceSpeed で全持分を一律に減らす）・割譲の C1 `Territory.cs:378〜387`（`Rule[1] += Rule[0]; Rule[0] = 0`・受け手の自分の持分を 0）。

## M2 分離独立（E-06 の支配度 Dominance）

- `Politics.cs:1421〜1613` `CheckSecession`。
  - 圧力 `:1453`＝|関税の姿勢 − 州の関税利害| × 州の平均闘争度 × `SecessionPressureScale` × (1 + 文化の距離)、`:1458` 民族の加算、`:1466` 国民意識の割引、`:1475` 参政権の算入率の割引、`:1488` 人の数の飽和 N/(N + 6 万)。
  - 引き留め `:1425` 正統性 ÷ (1 + 人口 × `SecessionHoldPerPop`)＋`:1429` 治安維持力の飽和項、`:1497〜1506` 大圏距離の投射で割る、`:1524〜1537` 同時多発の共有分母。
  - 積分 `:1544〜1549` `Dominance += (引き留め − 圧力) × DominanceSpeed`（積分の書き手はここ 1 か所。初期値と確定の書き手は `Simulation.cs:956`〔本土の州 1.0〕・`:1622`〔昇格の植民地 0.0〕・`MultiNationSim.cs:177`〔`OpeningColonyDominanceFull`＝1.0 で開幕の植民地を 1.0 へ・`Coefficients.cs:2770`〕・`Territory.cs:288`〔独立の確定 1.0〕。反証 低-1 で訂正＝旧の記載 938・1604 は誤り）。
  - 点火 `:1605`＝`Dominance ≦ 0` の州を `PendingSecessionWars` へ＝分離独立の戦争（`MultiNationSim.cs:285` `SpawnSecededNations`・四半期末 `:1066`）。
- 植民地の独立の旧い経路（Colony オブジェクト）は 2026-10-18 に削除（`Colonial.cs:5`・`:106` の墓標）＝植民地格の州も同じ `CheckSecession` を通る。
- 1104 の下書き D1（Dominance と Rule[0] は別物）・(925) の案 A・(946) の H0／K は、Dominance を残す前提で書かれている。

## M3 属国（_dependency）

- 器：`Diplomacy.cs:375` `HashSet<long> _dependency`（鍵＝宗主 Id と属国 Id・種別なし）。`:451〜457` `SetDependency`（属国は宗主 1 つ）・`:458` 外す・`:459` 向き付き・`:461〜465` `Suzerain`（全国走査）・`:466〜475` `DependentsOf`・`:477` `InDependency`（両向き）。開幕の一覧 `WorldData.cs:145〜148`→`MultiNationSim.cs:273〜276`。
- world.json の開幕の従属：**3 件**（シャム→カンボジア・シャム→ラオス・マラータ同盟→デリー）。
- 書き手（入口 4 本＋開幕）：
  - `Diplomacy.cs:2242〜2269` `Subjugate`：`ColonizeForce ≧ SubmitLine` なら国のある相手は `SetDependency`（:2260）＋全 POP の闘争度 +0.3（:2261）・国の無い抽象標的は植民地の接収（:2249〜2256）。拒めば開戦（:2266）。呼び手は `World.Invade` の 1 本。
  - `Diplomacy.cs:3189〜3197` 圧勝（|戦勝点|≧1.4）× 統治コスト≧0.4 → 境界州を 1 つ割譲（:3193）＋属国化（:3194）。
  - `Diplomacy.cs:3204〜3208` 同族の単州国は保護国（:3206）。
  - `Diplomacy.cs:3247〜3261` 宗主権の引き継ぎ（敗者の属国のうち `Projection` 最大の 1 件・`MaxDependents` 8 未満）。
  - 離反 `Diplomacy.cs:1396〜1409`：属国の軍 ＞ 宗主の陸軍力 × `Projection` × 0.5、かつ平均闘争度 ＞ 0.6、かつ再屈服しない（陣営戦力比 3 未満）の 3 条件の積。
  - 宗主の消滅 `:1368`・国の除去 `:442`。⚠講和（EndWar）では従属は解けない（`:2166`・`:2236` の注は〈戦争終結で解除〉と書くが実装と食い違う）。
- 毎月 `StepDependency`（`:1353〜1411`）：整列 −1.2/月（:1370）・従属間の禁輸を外す（:1372）・貢納（:1375〜1388）＝四半期ごとに `0.015 × 属国の月の GDP × 支払い率`（年に GDP の約 0.5%）・払えない分は対外債。
- 読み手（効き目ごと・file:line）：
  - 参戦と宣戦の禁止：`Diplomacy.cs:1730〜1737` `DefendDependent`（:1733 宗主は属国の敵側に入らない）・`:1766` `DeclareWar`・`:1514` 宣戦の閾値・`:3785` `CannotDeclareWar`・`:1501` 阿片の禁輸の緊張・`:1783〜1799` 連鎖参戦。
  - 侵略の門：`World.cs:2190`（既に従属）・`:2191`（自国の宗主）・`:2194`（`DependentCount ≧ MaxDependents`）・`InstrumentsWar.cs:215〜219`（影）・`Diplomacy.cs:2005〜2007`（AI の植民地化の候補は宗主の無い相手だけ）。
  - 覇権の脅威：`Diplomacy.cs:1129`・`:1135`・`:1141`（宗主の GDP・植民地・軍に属国を 0.3 倍で足す＝`DependentThreatWeight`）。
  - 敵意の割引：`Diplomacy.cs:1110`・`ArmyShape.cs:169`＝`IsDependentOf`×(1 − `AllyAttentionDiscount`)（`Coefficients.cs:2267`＝0.7。読み手はこの 2 か所だけ・反証 低-1 で訂正）。
  - 講和の批准の拒否 `TreatyRatificationControlThreshold`（`Diplomacy.cs:3164`）＝属国化を割譲へ振り替える門。
  - c101vs の枝が足した `InstrumentsDep.cs`・`DependencyCycleBreak`・`DependencyCycleTests.cs`（反証 低-1・枝の差分。main には無い）。
  - 投射の起点：`Diplomacy.cs:3705〜3709` `OriginNations`＝自国＋属国。
  - 形成：`Formations.cs:503〜508` `Owns`＝属国の州も宗主の領有に数える。
  - 画面：`App/ViewModels/GameViewModel.Diplomacy.cs:97`・`:102`・`:123`（属国 n/MaxDependents）・`:162`・`GameViewModel.Map.cs:112`・`Runner/Program.cs:2712`・`:2719〜2722`・`Runner/PendingProbe.cs:36`。
  - 自走の AI：`SelfPlay.cs:2122`。
  - 史実の物差し：`HistoricalReference.cs` が宗主（`SuzerainsOk`・`SuzerainsOk1950`）を 87 行で読む＝段 G は基線の取り直しが要る。
- 試験：`SetDependency` 4 ファイル 9 行・`Subjugate` 4 ファイル 6 行・`StepDependency` 1 行。`MaxDependents`・`InDependency` を直に読む試験は 0。

## M4 講和の割譲・賠償・矯正講和

- `Diplomacy.cs:2983〜3145` 講和の始め：`saPay`・会議外交の賠償の抑制・植民地の奪取の予算 `budget = saPay × ColonyCaptureBudget`（100）・`CessionUnified`（既定 0・`Coefficients.cs:1569`）・抽象標的の植民地の奪取（`:3085〜3116`・`ColonyCaptureMax` 3）・賠償 `:3118〜3143`＝`ReparationGdpFraction` 0.2 × 敗者の月の GDP × |戦勝点|（遠い圧勝は × `TreatyPortReparationMult` 2.5）× 支払い率・払えない分は対外債。
- `:3149` `OutcomeShocks`（正統性と敗戦・戦勝の衝撃）。
- `:3150〜3268` 圧勝の帰結：割譲（|戦勝点|≧`TerritoryCessionScore` 1.2）・条約港（遠い圧勝・`TreatyPortWinnerDistanceKm` 6000・`:3171〜3188`）・属国化（`:3189〜3197`）・単州の保護国（`:3204〜3208`）・同族の併合（`:3209`）・矯正講和（`:3214〜3242`・`ImposeRegimeIdeologyGap` 0.4・`ImposedRegimeLegitimacy` 0.3・`ImposeCrownFromLoser` 1.0）・宗主権の引き継ぎ（`:3247〜3261`）・復讐心の予約（`:3265`）。
- `:3272〜3278` `CedeTerritoryOnce`・`:3318〜3381` `CedeTerritory`・`:3384〜3389` `RegionOutputValue`（州の産出の名目の値＝役務を除く Output × RefPrice の和）・`:3393〜3419` `CedeTerritoryUnified`・`:3420〜3430` `CedeRegion`（`Territory.TransferRegion` へ）。
- 計器の行数：`InstrumentsCess.cs` 158・`InstrumentsCpeace.cs` 136・`InstrumentsCpmed.cs` 91・`InstrumentsImpose.cs` 64・`InstrumentsCede.cs` 177・`InstrumentsSecession.cs` 165・`InstrumentsRule.cs` 109。
- 試験：`ReparationGdpFraction` 4 ファイル 6 行・`ImposeCrownFromLoser` 1 ファイル 3 行・`ImposeRegime` 1 行・`CedeTerritory`／`CedeRegion` を直に呼ぶ試験 0・`Dominance` を読む試験 15 ファイル 47 行・`ForeignShares` 6 ファイル 34 行。

## M5 州の移し替え

- `Territory.cs:262〜291` `Rehome`（Ceded・Seceded・Returned・Sovereign の 4 種）・`:304〜394` `TransferRegion`（貯蓄と人口の保存・為替の換算・国債の保有の振替・輸送の容量・艦の戻し・遷都・C1）。呼び手は割譲 `Diplomacy.cs:3420` 付近・建国 `MultiNationSim` の `SpawnNation`・再併合 `ReabsorbNations`・独立の確定 `Diplomacy.cs:504`。
- ⚠州を移す処理は重い（市場の台帳・艦・国債・輸送）＝境目で行き来すると毎回この全部が走る。

## M6 投射

- `Diplomacy.cs:3673〜3702` `Projection`：首都から相手の点への**海路の km**（スエズとパナマの開閉は `ChokeOpenFor` `:3652〜3657`＝交戦中の運河の持ち主が閉じる）→ `1/(1 + km/10000)`・海越えなら `SeaControl` を掛ける。
- 休眠の器 2 本：`ProjectionRuleMin`（`Coefficients.cs:2781`・1.1＝到達不能＝起点は首都だけ）・`ProjectionUsesLand`（`:2783`・0）＝起点を州の集合へ・陸路の `PathGraph.LandDistanceKm`。
- 呼び手：`ArmyShape.cs:83`・`Diplomacy.cs:1108`・`:1396`・`:1467`・`:1662`・`:1735`・`:1800`・`:2023`・`:2231`（`FrontPush`）・`:2297`・`:2408`・`:3253`・`InstrumentsWar.cs:272`・`MultiNationSim.cs:1595`。
- `FrontPush`（`:2230〜2231`）＝陸軍力 ÷ 戦線数 × 投射 × 上陸 × 制空。戦線は国と国の抽象で、州ごとの占領は無い（`StepWars` `:2512〜2529`）。

## M7 輸送の費用の表（貿易）

- `BilateralMarket.cs:459〜479` `PairKm(a, b)`＝**換算 km**（お金ではない）：
  - 海＝`SeaGraph.SeaDistanceKmFromNodes`（両端の国のスエズ・パナマの可否）＋両端の陸脚 × 地形係数 × 陸/海の比。
  - 陸の直行＝`PathGraph.LandMatrix`（全点対・開幕に 1 回・`PathGraph.cs:576`）× 両端の（地形 × 比）の平均。`LandRouteEnabled=1.0`（`Coefficients.cs:2641`）。
  - 陸/海の比 `:322`＝`LandSeaCostRatio`（12）×（1 − 鉄道の門 ×（1 − `RailCostFraction` 0.25））＝鉄道で 12→3。鉄道の門は**その州の持ち主の国**の発見〈機関車〉（`Discovery.RailTransport`）。
  - **汽船は表に入らない**：汽船の割合 `ShipSteamShare` は容量と燃料だけ（`Transport.cs:44〜52`・`:106`）。〈船の登場で届きが広がる〉は今の表からは出ない。
  - 封鎖は運河の開閉（交戦）だけ。道路・関税・国境は入らない。
- 表：`:366〜425` `KmScratch`＝スレッドの作業領域（セーブに載らない）。`ks.Km` は州 × 州（tc×tc・-1 で空）・**清算で引いた対だけ・片向き**・署名（`:368〜383`・10 項目）が変わった州の行と列を捨てて持ち越す。`:442〜455` `LbKmOf(t, nat)`＝買い手の州 t × 売り手の国 nat の最短（その国の全ての州の最小）・清算ごとに空へ戻す。
- 表に入る州：`:250〜297`・陸路が点火しているので座標のある州はほぼ全て（`:294`）。国の無い抽象標的は入らない。
- 速度の注（`:355〜365`）：メモ化の前は清算 1 回で数百万回・後は高々 tc(tc+1)/2 ≈ 4.6 万回。

## M8 国家の力の出どころ（既にある量）

- 治安維持力 `PoliticalQuantities.cs:13`＝`Coercion × 人口 ÷ 基準人口`（国の 1 本）。兵は徴募した州の兵士 POP（`Military.cs:146`・`:182〜183`・`:292`）＝州から動かない。
- 行政の州ごとの予算 `Simulation.cs:2284〜2285`＝行政費 × `AdminBudgetFrac` × 使える歳入 × 州の人口の比 × `Fiscal.AdminDistanceMul`（距離の割増・`Fiscal.cs:556〜561`）＝**国家の力を州へ配る規則は既にある**（官僚 POP の居ない州は 0）。充足 `Region.AdminFill`（`Model.cs:967`・`EconomyStep.cs:2685〜2686`）。
- 陸軍力 `Diplomacy.cs:44`＝Military ×（1 − 海軍の割合）× 熟練。

## M9 税の行き先（外国の取り分）

- `Politics.cs:1800〜1807` `RuleDelivery`＝R0 ×（d +（1 − d）× AdminFill）＋ R1 × d・d＝1 − `PrivilegeTaxDiscount`。
- `Fiscal.cs:735〜749` `TaxScan`：国庫へ届く割合が上の値・在地の仲介者の取り分は州の最初の貴族へ（`:740〜741`）。
- ⇒ **外国の取り分（R2）と無統治（R3）の分は課されず、納税者に残る**。外国の持ち主の国庫へは何も流れない。外国の持分の効き目は関税の目減り（`InternationalMarket.cs:167〜174`）と屈辱・移民法の外圧（`World.cs:43`）だけ。
- 徴兵の母数も同じ `RuleDelivery`（`Military.cs:149`）。相続税の届きも同じ（`Simulation.cs:2788`）。
- 桁（AI の算術）：貢納＝年に属国の GDP の約 0.5%。賠償＝敗者の月の GDP × 0.2 × |戦勝点|＝1 回で年の GDP の約 1.7〜2.5%。外国の取り分 x の州の税を持ち主の国へ運ぶ形では、年に x × 税収 ÷ GDP（英 10% 超・清 2〜3%・設計書 0906 §10）＝x＝0.2 で年に GDP の 0.4〜2%。

## M10 州の経済の大きさ

- 州ごとの欄は無い。`World.cs:1461〜1498` `GdpPerMonth` は州 × 生産者を走査して Output × RefPrice を足す（役務を除く）＝州ごとの途中の和が `Diplomacy.cs:3384` `RegionOutputValue` と同じ式。自給の分は入らない（生産者の産出だけ）。

## M11 開幕のデータ（world.json）

- 国 61・抽象標的 21・州のベクトル（国の rule と州の上書き）を数えた（台本は読むだけ）：**データのある州 227 のうち、最大の区分が国家 102・在地 110・無統治 14・外国 1**（マサワ 0/0.25/0.65/0.1）。
- ~~開幕の植民地 81（`Simulation.cs` の昇格で在地 1.0）~~（2026-11-12 反証 高-3 で訂正）：昇格は `Rule` と `RuleInit` を `Places.RuleOf(col.Name)` から先に読む（`Simulation.cs:1623〜1624`・在地 1.0 は places.json に行が無い時だけ）。places.json を数え直すと、開幕の植民地 81 州**全て**に 4 区分のデータがあり、国家の区分は全て正。4 区分の最大は国家 56・無統治 25（例：北部サン＝ドマング・ポンディシェリ 0.6/0.3/0/0.1）。在地 1.0 になるのは、データの無い抽象標的の植民地（29 のうち 6）と途中で生まれる州だけ。植民地にも官僚の POP の枠がある（`Simulation.cs:1631` `BureaucratPopIndex = 7`）。
- 本文 §3-6 の比べ合い（持ち主の側＝Rule[0]＋Rule[1]・開幕は協力の割合 1）で数え直すと、開幕に持ち主の側が最大でない州は 227＋81＝308 州のうち **無統治 6 州・外国 1 州（マサワ）**（植民地 81 州は 0）。
  - 在地が最大の例：日本（0.4/0.58・0.15/0.83 ほか）・米（0.35/0.55）・露（0.35/0.6 ほか）・英の 1 州（0.4/0.6）・スイス・ライン諸邦・ポーランド・インドと中東と東南アジアとアフリカの国の大半。
  - 無統治が最大の例：米の 2 州（0/0.05/0/0.95）・露の 8 州・日本の 1 州・清の 1 州。
- 外国が正の州と国：マサワ 0.65・ウィダー 0.15・アノマブ 0.2・オラン 0.2・カンボジア 0.2・デリー 0.15・アワド 0.15。**誰の持分かのデータは無い**（`ForeignRulers` は開幕に空）＝今の目標は外国 0 なので、これらは開幕から 0 へ寄っていく（00一覧 P1-8a の〈外国が常に0〉）。
- ⇒ 〈在地が最大＝分離独立〉をそのまま置くと、開幕に約 110 州が分離へ入る（植民地は国家か無統治が最大＝在地の最大ではない）。

## M12 戦争の長さと寄る速さ（AI の算術）

- DominanceSpeed 0.025／四半期＝1 年で差の 1 − 0.975⁴ ≈ 9.6%・半減 27.4 四半期（6.8 年）。
- 列強どうしの戦争の長さ：中央 8〜12 か月・最長 30 か月（`Diplomacy.cs:2562` 付近の注・r167 の 10 種）。
- 交差の時間（外国 x_f が目標 T_f へ・国家 x_s が x_s0 から目標 T_s へ、同じ速さ k で寄るとき）：q = e^(−kt) =（T_f − T_s）÷（T_f − T_s + x_s0）。
  - T_f 0.6・T_s 0.2・x_s0 0.7 → q 0.364 → 40 四半期（10 年）。
  - T_f 0.9・T_s 0.05・x_s0 0.7 → q 0.548 → 24 四半期（6 年）。
  - ⇒ 1〜3 年の戦争では、寄せ方を変えない限り州の持ち主は戦争の中で動かない。

## M12b 本文が後から使った数と行

- 秩序の分布：国の秩序（`publicOrderIndex`＝**1 で切った後**の被覆）が 1 に張り付く国×四半期 27.3%・中央 0.498・下 1/10 0.162（出どころ＝`docs/202611/20261104/20261104_2本の支配度と自給の世帯の家計_下書き.md` §1-3・基線 keep_818/base_ins の rule_s・10 種・国×四半期 149,320）。
  - ~~切る前の被覆は計器に列が無い＝未測~~（反証 低-2 で訂正）：計器 `InstrumentsRule.cs:17` の `orderRawIndex`＝切る前の R（四半期の末の `w.Coercion` で数える・人口は約分される＝計器の自己申告）。反証役が keep_1053/a11 の rule_s（国×年の年末の行 103,022 行）で数えた：R＞1 は 17.6%・中央 0.448。挙げた国の四半期の行で R＞1 の割合は 清 96%（中央 1.96）・日 87%・仏 79%・露 72%・墺 73%・英 46%（中央 0.93）・普 3%（中央 0.68）。
- 地形の陸路の係数 `Coefficients.cs:2653` `TerrainHorseCost`＝[平地 1.0・大河 0.8・小河 0.9・丘 1.5・森 3.0・山 2.5・荒地 8.0]・`:2654` `TerrainRailCost`＝[1.0・1.0・1.0・1.5・1.2・5.0・3.0]（`Model.cs:97` の並び）。
- 分離の圧力の割引：国民意識 `Politics.cs:1463〜1467`（×(1 − 重み×国民意識)）・参政権の算入率 `:1475`（×(1 − 算入率)・算入率＝州の平均中核度＝本国の州で約 1・`:1715〜1716`）＝本国の州では圧力がほぼ 0。州の国民意識 `:1693〜1698` `AvgRegionConsciousness`。
- 割譲の件数の基線：m839 の 1,192 件（`docs/202611/20261105/…割譲の支配度と外国持分の穴…_下書き.md` §2-1）・m919 の 1,417・1,410・1,285 件（`docs/202611/20261107/…Dominanceの引き継ぎ_盤面m919_下書き.md` の反証役の訂正 2）。分離の建国の基線：keep_933 の開幕 10 年 45・35・41・その後 133・140・121（`…開幕の州の支配度の初期値_盤面m933_下書き.md` 段 K の門）。

## M13 消す候補の大きさ（行数の見積もり・コメントを含む）

| 物 | 場所 | 行 |
|---|---|---|
| 属国の器と問い合わせ | `Diplomacy.cs:375`・`:442〜477` | 約 40 |
| `StepDependency`（整列・禁輸の除外・貢納・離反） | `Diplomacy.cs:1352〜1416` | 約 65 |
| `DefendDependent` と宣戦の禁止の枝 | `:1730〜1742`・`:1501`・`:1514`・`:1766`・`:3785` | 約 25 |
| 条約港の付与と風化 | `:3166〜3188`・`:526〜552` | 約 50 |
| 圧勝の帰結（属国化・保護国・引き継ぎ・矯正講和） | `:3150〜3268` | 約 120 |
| 割譲の予算と選び | `:3272〜3430` | 約 160 |
| 賠償 | `:3118〜3143` | 約 25 |
| 侵略の門の従属の枝 | `World.cs:2190〜2194` | 3 |
| E-06 の積分と点火 | `Politics.cs:1539〜1549`・`:1599〜1611` | 約 25 |
| 計器（割譲・講和・矯正・分離） | Instruments の 6 本 | 約 790 |
| 係数（属国・条約港・割譲・賠償・矯正・離反） | `Coefficients.cs` | 約 25 本 |

## M14 反証（2026-11-12）の後に現物で確かめた物

- 力の単位の根（反証 高-1）：
  - `Government.cs:59〜60` `CoercionTargetFromForce`＝force ÷ (1 + force ÷ `CoercionMax` 2000)（飽和）。`:298` 多国の世界では目標＝`SoldierSizeK()`（兵の数）×`MilitaryReadiness`×`CoercionPerSoldier`＝**兵の絶対数**。
  - `Politics.cs:484〜492` `w.Coercion = BaseCoercion × 正統性 × FiscalFundingRatio × warDrain`（warDrain＝1＋戦時の結束−`WarCoercionDrain`×厭戦）。
  - `PoliticalQuantities.cs:13〜14` `EffectiveCoerce`＝`Coercion`×人口÷`PoliticalReferencePop`（1000・`Coefficients.cs:209`）。`Crime.cs:20` 秩序＝`EffectiveCoerce`÷(人口×`CrimeOrderPerPop` 0.8)＝**人口が約分され、`Coercion`÷800 だけ**で決まる（`Coefficients.cs:484` の注〈秩序＝Coercion÷(基準人口×これ)＝Coercion/800〉と一致）。`NationConfig.cs:28` の注〈人口正規化後＝基準人口1000あたりの強度〉と、兵の絶対数から作る目標が食い違う＝今の木の不具合の疑い（この設計の外の線）。
  - 陸軍力 `Diplomacy.cs:44` `LandPower`＝`Military`×(1 − 海軍の割合)×熟練。海軍力 `:36〜38` `NavalPower`＝艦×燃料×乗員×熟練（同じゲージの単位）。即応度は `Military` の中（`World.cs:2196` の注）。
- 戦争の接触 `Diplomacy.cs:2212〜2222` `Engagement`＝Σ min(押しA, 押しB) ÷ `Military`（0〜1）。
- 並び順：各国の `Politics.Step` は `Simulation.cs:1926`（国ごとの Step の中）＝逐次。予約して四半期末に精算する先例＝`Fiscal.cs:704` `PendingForeignBorrow`→`MultiNationSim.cs:1028〜1030`。
- 助走：`MultiNationSim.cs:669` で全ての国の `PoliticsEnabled=false`（政治は止まる）。
- 失地のストック `World.LostRegionsCumulative`（`World.cs:908`）：書き手＝`Diplomacy.cs:3426`（割譲）・`:497`（独立の確定）。読み手＝`Ideology.cs:31`・`PoliticalQuantities.cs:186`・`:230`・風化 `Politics.cs:202〜203`。
- 陸の隣の判定：`Diplomacy.cs:3513〜3530` `CessionBuildAdj`・`CessionAdjacent`（`PathGraph.Edges` から作る）。
- 国の除去：`Diplomacy.cs:433〜` `RemovePower`。遷都：`Territory.cs:369`・`:400` `NextCapital`（残る州のうち最大の GDP）。
- 鉄道：容量の在庫は**国の 1 本** `World.RailCapacity`（`World.cs:1145`・鉄道財の投資で積む）。州への配り方 `Transport.RegionShare`（`Transport.cs:83`・潜在量の比）。**どの州に敷いたかの量は無い**。距離の表の鉄道の門は国の発見 0/1（`BilateralMarket.cs:321〜322`）。
- 冬の港の凍結は距離の表の署名に入る（`BilateralMarket.cs:282`・`:375`）。
- 鉄道の買い：`Transport.cs:34〜35`＝`railGate`（発見〈機関車〉の 0/1）×輸送の目標の容量 `target`（船と共有・混雑の割増つき `:26〜31`）へ向けて鉄道財と機関車を買う＝国の鉄道の充足 `RailCapacity`÷`target` は今の量から導ける。
- places.json の数え直し（2026-11-12・読むだけの台本）：開幕の植民地 81 州＝区分のデータ 81・国家が正 81・4 区分の最大は国家 56・無統治 25。§3-6 の比べ合い（無統治＞Rule[0]＋Rule[1]・外国＞Rule[0]＋Rule[1]）で、world.json の 227 州は無統治 6・外国 1、植民地 81 州は 0。
