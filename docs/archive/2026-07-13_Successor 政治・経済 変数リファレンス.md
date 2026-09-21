# Successor 政治・経済 変数リファレンス

> 本書は Successor（Victoria2 型・経済/政治シミュレーション）の**政治・経済に関する変数**を、日本語訳と「どの要素から算出されるか」を軸に整理したものです。
> - 対象は `Successor.SimCore` の状態変数・派生指標。途中計算に使う一時的な作業用フィールド（`RemainingBudget`, `GoodDemand`, `NeedTotal`, `Paid`, `Earned` 等）は原則除外しています。
> - 「算出方法」列は**何から算出するか（入力要素・主要係数名）**を示し、完全な数式の再現は省いています。厳密な式は各 `.cs` を参照してください。
> - 係数（`Coefficients.cs`）は既定でオフ（=0）の「休眠機構」が多数あり、その場合は従来挙動に恒等（＝中立）です。表中で「既定0＝中立」「dormant」等と注記したものがこれに当たります。
> - `Q5(...)` は四半期に一度だけ動く目標値、`StepEnum`/`StepBool`/`Move` は Government の漸進ヘルパを指します。

**目次**
0. [分類（列挙型）](#0-分類列挙型)
1. [経済コア（生産・市場・労働・人口）](#1-経済コア生産市場労働人口)
2. [財政・金融・貨幣](#2-財政金融貨幣)
3. [国際貿易・世界市場・植民地](#3-国際貿易世界市場植民地)
4. [政治動学（不満・意識・イデオロギー・政体）](#4-政治動学不満意識イデオロギー政体)
5. [統治機構（議会・王権・政府・制度）](#5-統治機構議会王権政府制度)
6. [外交・軍事・国力・人口・技術](#6-外交軍事国力人口技術)

---

## 0. 分類（列挙型）

### 政体 `World.Polity`（誰が支配するか × 自由/権威 × 君主の有無）
利益集団の極端な力で遷移する。自由側のみ制度革新（フロンティア）可能、権威側は採用のみ。

| 値 | 日本語 | 区分 | 説明 |
|---|---|---|---|
| `Absolutist` | 絶対君主制 | 権威・君主 | 貴族と統治する伝統的君主 |
| `Despotic` | 専制君主制 | 権威・君主 | 貴族を抑え官僚軍で個人統治＝上から近代化可 |
| `Shogunate` | 幕府 | 権威・君主 | 軍事貴族の封建分権・名目君主（日本） |
| `ConstitutionalMonarchy` | 立憲君主制 | 自由・君主 | 君主を残し議会化＝明治型 |
| `Republic` | 共和制 | 自由 | 君主を廃した代議制 |
| `BourgeoisDictatorship` | ブルジョア独裁 | 権威 | 大衆を抑える権威主義的資本主義 |
| `ProletarianDictatorship` | プロレタリア独裁 | 権威 | 労働者の革命独裁 |
| `MilitaryDictatorship` | 軍事独裁 | 権威 | 軍人POPが不安定に乗じ掌握。正統性が侵食されやすい |
| `Anarchy` | 無政府 | ― | 秩序崩壊（一過性） |

補助登録簿：`World.Crown`（`CrownForm`＝君主制の形：Absolutist/Despotic/Shogunate/None）、`World.Seizure`（`SeizureForm`＝奪取の形：Bourgeois/Proletarian/Military/Anarchy/None）。軸（統制力・参政権）だけでは区別不能な「形」を歴史の産物として登録簿に持つ。

### イデオロギー `Pop.IdeologyShares[9]`（各POPが分布として保持・合計1）
階級×条件×時代から創発し、集計が政体への引力点を決める。

| # | 値 | 日本語 | 志向 |
|---|---|---|---|
| 0 | `Traditionalism` | 伝統主義 | 現秩序（君主・教会・身分）の維持 |
| 1 | `Reactionism` | 反動主義 | 旧秩序への復古（反革命） |
| 2 | `Liberalism` | 自由主義 | 立憲・自由市場・制限選挙 |
| 3 | `Republicanism` | 共和主義 | 君主廃止・代議制・参政権拡大 |
| 4 | `Nationalism` | ナショナリズム | 国威・統一・戦争熱（横断） |
| 5 | `Socialism` | 社会主義 | 労働者の権利・再分配（議会内左派） |
| 6 | `Communism` | 共産主義 | 革命的労働者独裁（1848以降） |
| 7 | `Anarchism` | 無政府主義 | 国家の廃止 |
| 8 | `Imperialism` | 帝国主義 | 海外膨張・植民地（横断・エリート） |

### 国家ランク `PowerRank`（NationalScore の相対閾値で四半期ごと動的分類）
| 値 | 日本語 | 説明 |
|---|---|---|
| `Minor` | 弱小国 | 植民地化の標的。抽象勢力は常にこれ |
| `Middle` | 中堅国 | 中堅同士で緊張上下・同盟 |
| `Great` | 列強 | 列強同士で緊張上下・同盟、弱小を植民地化 |

### 財の階層 `Good.Tier`（予算は優先度＝数値の小さい順に配分）
| 値 | 日本語 | 説明 |
|---|---|---|
| `Staple` | 必需品（主食） | 最優先。飢饉判定の分母 |
| `Daily` | 日用品 | |
| `Luxury` | 奢侈品 | 消費税の主対象 |
| `Intermediate` | 中間財 | |
| `Producer` | 生産財 | |
| `Service` | 役務 | 労働力・教育・医療・行政。全段の後に消費（最上の贅沢）。GDPからは除外 |

- **教育法** `World.EducationLaw`：None → Religious（宗教）→ Public（公教育）→ Compulsory（義務教育）。段階で担い手（聖職者/教師）と識字の天井が変わる。
- **近代化経路** `ModernizationPath`：Failure（失敗）/ Tokugawa（徳川）/ Meiji（明治）/ Radical（急進）。
- **海盆** `Basin`：Atlantic / Mediterranean / BlackSea / IndianOcean / Pacific。座標から導出。交易が経由する海域＝チョークポイントの利用を地理から決める。

---

## 1. 経済コア（生産・市場・労働・人口）

### 1.1 生産・市場
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Producer.Output` | 実産出 | 目標へゆっくり収束（`SupplyAdjustSpeed`）。目標＝`Capacity`×投入充足`effFill`（≒`LastInputFill`）×採算`profitFactor`。`InventoryAdjustGain`>0で在庫ギャップ補正 |
| `Producer.Capacity` | 生産能力 | ＝労働`Pops[lp].Size`×配分`fraction`×`Productivity`×`boost`×不作`harvestMul`×鉱物`mineralMul`。`boost`＝`ProductivityBonus`×`indMult`×(1+`SteamBoost`×`SteamFill`)(1+`ElecBoost`×`ElecFill`)(1+`EffBoost`×`EffFill`)×`AdvancedTechScale`×`RecipeTechScale`。土地作物は`regionLandFactor`、工場財は規模の経済`ScaleEconomyGain` |
| `Producer.Productivity` | 労働1あたり産出 | 永続パラメータ（Catalog/設定値）。`Capacity`の基礎係数 |
| `Producer.DesiredOutput` | 作りたい量（採算充足前） | ＝`Capacity`×`profitFactor`。死んだ工場の再点火トリクル（価格で蘇生）に使う |
| `Producer.MarginalCost` | 限界費用（付加価値分＝賃金・生存線） | 永続パラメータ。実効mc＝`MarginalCost`＋投入費`InputCostPerUnit`（＝Σ投入価格×`EffectiveQty`）。採算`profitFactor`＝min(1,(price−mc)/(mc×`SupplySlope`)) |
| `Producer.Scale` | 操業規模（参入退出） | 目標＝clamp(1+margin, floor, `ScaleMax`)へ`ScaleAdjustSpeed`で収束。margin＝(price−mc)/mc。floor＝max(`ScaleFloor`,`BuiltScale`)。労働配分の重みに掛かる |
| `Producer.SmoothedMargin` | 採算のEMA（倒産判定用） | margin を`FactoryMarginSmoothing`で平滑 |
| `Producer.Bankrupt` | 倒産（産出0・労働解放） | `SmoothedMargin`<`FactoryBankruptcyMargin`かつ`BuiltScale`≒0で倒産。margin>`FactoryReentryMargin`または品薄（需要>供給+在庫）で再開 |
| `Producer.LastInputFill` | 前期投入充足率（リービッヒ律速） | ＝投入財の配給率`ration`の最小値。当期産出を制約 |
| `Producer.RentShare` | 地代取り分ρ | 永続パラメータ。所得分配で rent＝ρ×付加価値（RGOは`FarmRentCap`で上限） |
| `Producer.LastRevenue`／`LastCost` | 工場別の収入／支出 | `LastRevenue`＝producerRevenue、`LastCost`＝投入費＋賃金。損益UI用 |
| `Market.Price` | 市場価格 | 日次`MarketFlow`で更新。非役務＝(1+(`PriceAdjustSpeed`×在庫gap＋`FlowAdjustSpeed`×需給flow)/div×amp)。gap＝(`TargetStock`−`Stock`)/(和)。flowは価格弾力`PriceElasticity`込み。`SoftBand`で床`PriceFloorFactor`天井`PriceCeilFactor`。`PriceLevel`でアンカーをスケール |
| `Market.Stock` | 在庫 | ＝available−sold（＝前在庫＋生産＋輸入−販売）。月初に`Spoilage`（工業化で冷蔵`RefrigerationEffect`）で腐敗 |
| `Market.TargetStock` | 望ましい在庫 | ＝max(`TargetStockFloor`,`TargetStockBuffer`×`LastDemand`) |
| `Market.LastDemand` | 前期総需要 | ＝消費popDemand＋工場投入`inputNeed`＋投資`investDemand`＋軍需`militaryDemand`(＋効率化)。次期の労働配分の重み・価格flow・在庫目標の基礎 |
| `Market.LastProduced`／`LastSold`／`LastShortage` | 当期の生産量／販売量／不足量 | クリアリングで確定（sold＝min(available,需要)）。失業率・倒産再開・価格flowの判定に使う |
| `Market.LastImport`／`LastExport` | 当期の輸入／輸出量 | 植民地・国際市場からの流入等。財収支に算入（詳細は §3） |
| `Region.LastSubsistenceOutput` | 当月の主食自給産出Σ | 市場外・無償の自給分の累積。扶養力の生産アンカーの分子補完 |

### 1.2 労働・賃金・雇用
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Market.ReservationWage` | 留保賃金（賃金の下方硬直源） | 労働財のみ。目標賃金へ賃上げは速く`ReservationWageRise`（組合`UnionWageRiseBoost`で増幅）・賃下げは遅く`ReservationWageFall`で追従。賃金＝max(目標,留保賃金) |
| `wages`／`Pop.LandRent`（要素所得） | 賃金／地代 | 付加価値valueAdded＝producerRevenue−投入費。valueAdded≥0で rent＝rentShare×valueAdded、wages＝valueAdded−rent。RGOのrentは`Pop.LandRent`へ（土地税ベース）。赤字は所有者/国庫（補助金`Subsidized`）が負担 |
| `Pop.UnemploymentRate` | 失業率 | 自分の労働財の売れ残り率＝1−`LastSold`/`LastProduced`。農業労働は自給`Subsistence`分を控除 |
| `work`（労働供給・EconomyStep内） | 働ける労働供給 | ＝adults×((1−`FemaleShare`)＋`FemaleShare`×女性参加)＋`ChildShare`×`ChildWorkFactor`×`ChildLaborRate`。adults＝1−`ChildShare`−(1−`OldLaborFactor`)×`OldShare`。労働財の`boost`に work/`InitialWork`で乗る |
| `Pop.InitialWork` | 労働供給の初期値(t=0のwork) | 初回`EconomyStep`で確定（標準構成0.64）。相対変化（女性法・児童労働禁止・戦死）の基準＝倍率排除の正規化定数 |
| `Pop.ChildLaborRate` | 児童労働率 | ＝`ChildLaborLegal`×(1−就学)。就学＝学校あれば`EducationFill`×(1−困窮Need)、Need＝1−`Satisfaction`。`LaborSupply`が算出 |
| `Pop.FemaleShare` | 成人の女性比率 | 徴兵・戦死（兵士=0）で上下。移住で加重混合。女性参加・参政権に効く |
| `Region.FemaleParticipation` | 女性の構造的労働参加率 | 賃金保持`WomenWageRetention`から導く基礎値(0.4〜0.7)。実効＝`FemaleParticipation`＋`FemaleNeedWeight`×Need×`WomenWageRetention`（`LaborSupply`） |
| `Region.WomenWageRetention` | 女性の賃金保持度(0..1) | Worldから複製。女性参加・女性識字の駆動（困窮就労のゲート） |
| `Region.ChildLaborLegal` | 児童労働の合法度(0..1) | Worldから複製。子供の労働/就学の時間配分を律する |

### 1.3 都市化・工業化・技術資本
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Pop.IsUrban` | 都市人口フラグ | `PopDecision`が農村↔都市間で所得差の低温softmaxで移動。都市人口比＝`SumUrbanPopulation`/`SumPopulation` |
| 都市化上限 maxUrbanShare（PopDecision内） | 都市化の天井 | ＝clamp(`BaseUrbanCap`＋indForCap×`UrbanPerInd`,0,0.85)。indForCap＝`TotalIndustrialization`（`UrbanCapFollowSpeed`で`UrbanIndSmoothed`に平滑）。都市化は工業化に従う |
| `Region.Industrialization` | 工業化度（集約資本） | 毎期`IndDepreciation`で減価。投資で built×`IndPerConstruction`だけ積む。投資量は限界収益 netReturn＝`IndProductivityGain`系×(1+`IndTechLeverage`×tech)−hurdle(資本コスト＋`CrowdingOutWeight`×金利−`EquityHeatEuphoria`×過熱)から創発 |
| `Region.TotalIndustrialization` | 立っている資本総量(新旧合計) | ＝`Industrialization`＋`OldVintageCapital`。都市化天井・衛生proxy・保守・移民引力が読む |
| `Region.ProductiveIndustrialization` | 生産性寄与ぶん(座礁控除後) | ＝`Industrialization`＋`OldVintageCapital`−`StrandedVintage`。`indMult`・機械化・投資収益が読む |
| `indMult`（EconomyStep内） | 工業化由来の生産性倍率 | ＝1＋`IndProductivityGain`×effInd/(1＋effInd/`IndProductivityScale`)。effInd＝`ProductiveIndustrialization`（飽和カーブ） |
| `Region.OldVintageCapital` | 旧世代の工業化資本 | 現世代の`VintageMigrateRate`分が毎期経年移動。`IndDepreciation`で同率減価。既定0 |
| `Region.VintageTechLevel` | 旧世代資本の据付時平均技術 | 移動時に加重平均更新。現`TechLevel`との差techGapが座礁度を決める |
| `Region.StrandedVintage` | 座礁量(生産性に寄与しない旧資本) | ＝min(`OldVintageCapital`,`VintageObsolescence`×`OldVintageCapital`×techGap)。techGap＝max(0,`TechLevel`−`VintageTechLevel`) |
| `Region.Mechanization`／`Producer.Mechanization` | 機械化度(技術的失業) | ＝`LaborSaveRate`>0? effInd/(effInd＋`LaborSaveIndHalf`):0。Producerへ複製し労働投入`EffectiveQty`を削る |
| `Producer.ElecFill` | 電化率(0..1・EMA) | `FillInertia`で電力配給率`ration`へ収束。下落は`ElecFillIrreversibility`で減速。稼働率↑・労働投入↓ |
| `Producer.SteamFill` | 蒸気動力化率(0..1・EMA) | `FillInertia`で石炭配給率へ収束。蒸気発明`steamGate`(＝`SteamTechOnset`/`Width`)後、採算が合えば石炭を焚く。電化分は電力が代替 |
| `Producer.EffFill` | RGO効率化率(0..1・EMA) | `FillInertia`で効率化材（肥料/工具等・profit-gate最安）の残余配給率へ収束。RGO産出を増やす |
| `Region.TechLevel` | 技術水準 | Worldから複製。先進財の生産性(`AdvancedTechScale`)・電化/蒸気ゲート・工業化収益に効く |
| `Region.GridCapacity` | 送電容量 | 投資で`GridId`購入分積み増し、毎期`GridDecayRate`で劣化。電力がこの容量までしか工場に届かない |
| `Region.Infra` | インフラ/開発度 | 永続。locked地域の解錠を駆動 |
| `Region.PortCapacity` | 港湾取扱容量 | 0スタート、国家＋資本家投資で積む。港州の実効海運に加算。混雑`LastPortThroughput`が投資を呼ぶ（詳細は §3） |

### 1.4 消費・満足度・人口扶養
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Pop.Satisfaction` | 満足度(生活水準) | ＝clamp(NeedMet/NeedTotal,0,1)。tier別重み(`DailyNeedWeight`/`LuxuryNeedWeight`)で加重。＋電化`ElecInfraSatBonus`×`ElecInfraFill`(都市)・労働保護`LaborWellbeing`・福祉`WelfareSatComponent`(困窮`WelfareTargeting`)・年金`PensionWellbeing`の非物質厚生 |
| `Pop.Savings` | 貯蓄 | ＝前貯蓄＋`Earned`（賃金・地代・交易益）−`Paid`（消費・投資支払い）。移住で1人あたり按分して運搬 |
| `Pop.Subsistence` | 自給率(0..1) | 永続。主食の一部を市場外で自足（農民）。市場需要から控除、飢えと失業の分母から除外 |
| `Region.FoodFill` / `World.FoodFill` | 主食充足(マルサス項) | 消費ベース∈[0,1]の平滑値（当期の主食met/needから）。局地飢饉（超過死亡）と扶養力（人口増減 §6）に使う |
| `Pop.EducationFill` | 教育財の充足率 | 役務の階層優先配分でqty/`GoodNeed`。義務教育`CompulsoryEduFunding`で国庫が不足分を補填。識字の到達天井 |
| `Pop.HealthFill` | 医療財の充足率 | 役務配分でqty/`GoodNeed`。過密×衛生不足で医療需要`HealthCrowdingBoost`が跳ねる。人口扶養力に効く |
| `Region.AdminFill` | 行政の充足率 | ＝got/adminNeed（`AdminNeedPerPop`×人口・予算`AdminBudget`律速）。1未満＝汚職（税収漏れ） |
| `Region.InfraFill` | 上下水道の充足率(衛生) | ＝got/infraNeed（`InfraNeedPerUrban`×都市人口・`InfraBudget`律速）。衛生＝`InfraFill`＋`SoapSanitationWeight`×`SoapFill`＋`ElecInfraSanitationWeight`×`ElecInfraFill`。低いと過密疫病 |
| `Region.SoapFill` | 石鹸の消費充足率 | ＝石鹸財の配給率`ration`。公衆衛生として衛生に加算 |
| `Region.ElecInfraFill` | 電化インフラ充足率 | ＝got/elecInfraNeed（`ElecInfraNeedPerUrban`×都市人口）。満足度＋衛生＋送電容量`ElecInfraGridBoost`の便益 |
| `Pop.Literacy` | 識字(0..1) | 経済入力＝`EducationFill`(到達天井)と`ChildLaborRate`(働く子は就学不可)。伸長式はTechEducationが担う（§6.3） |

**GDP** `World.Gdp`：全active州の生産者について Σ(`Output`×`RefPrice`)。役務(Service)とlocked州は除外。賠償金・借入枠・金本位資格・脅威評価の基準。

---

## 2. 財政・金融・貨幣

### 2.1 税制・歳入
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.TaxRate` | 所得税率 | Government で設定する現在値（行政力で上限 ceiling）。Fiscal で各popの Earned に定率課税。 |
| `World.PollTax` | 人頭税率 | 政策の現在値。Fiscal で `PollTaxRate×PollTax×Size` を非特権・非兵士popに課す（逆進的）。 |
| `World.LandTax` | 土地税率 | 政策の現在値（LandTaxUnlocked が前提）。Fiscal で pop の `LandRent`（地代）に課税。 |
| `World.ConsumptionTax` | 消費税率 | 政策の現在値。EconomyStep の購入時に `qty×Price×cTax` を上乗せ→`Region.LastConsumptionTax` に累積。 |
| `World.ProgressiveTax` | 累進課税率 | 政策の現在値。Fiscal で富裕層(IsModernElite/Closed・非兵士)の Earned に所得税へ**追加**課税。 |
| `World.Tariff` | 関税率 | 政策現在値＋`TariffRetaliationBias`。InternationalMarket で輸出入に課し実収を LastTariffRevenue へ。 |
| `World.LastRevenue` | 予算基数（診断） | Fiscal の `budgetBase＝純税収 revenue ＋ IndirectTaxInBudgetBase×間接税実収`。行政/軍需予算の基数。 |
| `World.LastTariffRevenue` | 関税実収（診断） | InternationalMarket の CollectTariff が設定（輸出補助金時は負値）。Fiscal で月次換算し予算基数へ。 |
| `World.LastTaxLeak` | 汚職漏れ実額（診断） | `revenue×CorruptionMax×(1−AdminEfficiency)×(1−CleanGovernance)`（官僚popへ移転）。 |
| `World.CleanGovernance` | 清廉度ストック | 根絶費 acTarget（`AntiCorruptionSpending`と債務/汚職ロック上限の小さい方、統治スパンで逓減）へ`AntiCorruptionSpeed`で漸近。 |
| `World.AdminPowerSmoothed` | 行政力 | `AdminEfficiency` の移動平均（`AdminPowerSmoothing`で平滑）。税制アンロックの物差し。 |
| `World.FiscalCapacity` | 裁量余剰（EMA） | `surplus＝budgetBase−維持費−利払い−行政/軍需/根絶請求権` を `FiscalCapacitySmoothing` で平滑。福祉/年金の財源。 |

### 2.2 国庫・国債・債務
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.Treasury` | 国庫残高 | 税収 revenue を加算し、利払い・維持費・福祉/年金・根絶費を支出、借入/切下げで補填する累積ストック。 |
| `World.GovDebt` | 国債残高 | 借入 borrowed を加算、減債 repay とデフォルトで減算。 |
| `World.GovBondRate` | 国債金利（年率） | `(BondBaseRate＋BondRateDebtScale×債務GDP比＋二次項＋fisher＋距離信認distrust＋PolicyRateBondWeight×InterestRate)×兌換割引`、`BondRateMax`上限。 |
| `World.BondPrice` | 国債二次流通価格 | `BondBaseRate/GovBondRate`（永久債＝利回り上昇で価格暴落）。 |
| `World.ForeignDebt` | 対外債務（うち海外保有） | 借入の海外分 foreignPortion を加算、減債/デフォルトで減算。利払いの対外流出比を決める。 |
| `World.AusterityIntensity` | 緊縮強度 | Government で `Clamp((GovDebt/Gdp − AusterityStartDebt)/AusterityRange,0,1)`。逆進課税下限・救済上限を動かす。 |
| `World.BorrowStacks` | 借入スタック数 | 借入1件で+1、減債で `repay/loanUnit` 減。完済で0。SpendingCapFactor の源。 |
| `World.SpendingCapFactor` | 支出上限係数 | `max(0, 1 − BorrowStacks/MaxBorrowStacks)`。福祉/インフラ/軍需予算に乗る。 |
| `World.BorrowQueue` | 起債予約数 | RequestBorrow() で加算、Fiscal が毎月1件処理して減算（連続起債）。 |
| `World.InsolvencyQuarters` | 支払不能連続四半期 | 借入上限張り付き（BorrowStacks≒MaxBorrowStacks）で加算、離脱で0。`DefaultTriggerQuarters`超でデフォルト。 |
| `World.BorrowCooldown` | デフォルト後借入禁止四半期 | デフォルト時に `DefaultCooldownQuarters` を設定、四半期ごとに減算。 |
| `World.CumulativeDevaluation` | 累積通貨切下げ額 | 赤字時の切り下げ devalue を累積（GovBondRate の信認 distrust 項に効く）。 |

### 2.3 貨幣・物価・為替・信用
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.PriceLevel` | 物価水準 | UpdatePriceLevel で `money/Gdp` のトレンド比 cyclical へ `MonetaryPriceSensitivity` で寄せ、FiatInflationPressure を乗算、Floor/Ceil でクランプ。 |
| `World.NominalGdp` | 名目GDP | `Gdp×(MonetaryEnabled?PriceLevel:1)`。実質×物価。債務/GDP比の分母。 |
| `World.ExpectedInflation` | 期待インフレ（年率） | 物価騰落 `(PriceLevel/before−1)×4` を `InflationExpectationSmoothing` で EMA。Fisher項で国債金利へ。 |
| `World.ExchangeRate` | 為替レート | 目標＝`floatTarget（PPP=1/P×貿易収支+政策金利バイアス）` と平価1.0を Convertibility で補間し、`ExchangeRateSpeed` で追従。 |
| `World.Convertibility` | 兌換可能性 | OnGoldStandard を目標(1/0)に非対称速度(`ConvertibilityRise/DropSpeed`)で追従。切下げ/金利/対外信用の連続lerp軸。 |
| `World.OnGoldStandard` | 金本位加入 | Fiscal で `GoldStandardTarget＋債務GDP比≤閾値＋FiscalFundingRatio≥閾値＋正貨カバー率≥GoldCoverMin` で加入、割れで強制離脱。 |
| `World.FiatInflationPressure` | 紙幣増発の物価圧 | 切下げ時 `devalue/BroadMoney` を加算、UpdatePriceLevel が P に反映後リセット。 |
| `World.TotalMoney` / `World.BroadMoney` | 総/広義マネー | 全Regionのpop貯蓄合計 `SumSavings`（BroadMoney は TotalMoney の別名＝金＋信用）。 |
| `World.BaseMoney` | 基礎貨幣（金） | `TotalMoney＋Treasury−OutstandingLoans`（保存量）。Credit で銀行準備 `Bank.Gold` を導出。 |
| `World.InterestRate` | 政策金利レバー | 政策の無次元現在値(0..1)。Credit の貸出意欲・国債金利・為替バイアス・外資carryに配線。 |
| `World.FinancialHeat` | 金融過熱度 | `Bank.Heat`＝`Loans/effMaxLoans`（与信使用率）。恐慌予兆ゲージ。 |
| `Bank.Confidence` | 貸す意欲 | 目標 targetConf＝`1−パニック/ドローダウン/財政破綻/負債デフレ/債券評価損/過熱脆弱性` へ Panic/ConfidenceSpeed で追従。 |
| `Bank.Loans` | 与信残高 | 目標 `effMaxLoans×Confidence×appetite` へ CreditAdjustSpeed で調整。maxLoans＝`Gold×(CreditMultiplier−1)`。 |
| `World.ForeignCapitalIn` | 流入外資（負債） | CapitalMarket で収益率>worldRate の需要に応じ流入、信用低下で逃避・配当流出。 |
| `World.CapitalAbroad` | 対外資本（資産） | CapitalMarket で収益率<worldRate 時に送り出し加算、逃避還流で減算。 |

---

## 3. 国際貿易・世界市場・植民地

### 3.1 貿易フロー・世界市場
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `InternationalMarket.Price[g]` / `Good.WorldPrice` | 財ごとの世界価格（emergent） | 初期は`RefPrices`か1.0。各期 `WorldPriceSpeed`で `(totalImport-totalExport)/denom + markup + rowAnchor` 分だけ更新しRefPrice×[0.1,10]にクランプ。markupはHHI集中度(`MonopolyMarkupMax`,`MonopolyHhiFloor`)、rowAnchorは`RestOfWorldElasticity`×(RefPrice/価格-1)。 |
| `InternationalMarket.Volume[g]` | 直近の財別取引量（テレメトリ） | `traded = min(totalImport, totalExport)`（短い側で律速）。 |
| `Region.Markets[g].LastImport` | 州の財別輸入量 | 金建て局所価格`m.Price×ExchangeRate`が輸入閾値超で `access×TradeSpeed×((lp-importThresh)/w)×scale`、両側の充足率`impFill`で按分。国内市場`StepDomestic`分も加算。 |
| `Region.Markets[g].LastExport` | 州の財別輸出量 | 局所価格が輸出閾値未満で同式（在庫上限`m.Stock`）、`expFill`で按分。 |
| `World.LastImportValue` | 輸入額（世界価格建て） | `importVal = Σ(輸入量×世界価格)`。関税の課税ベース。 |
| `World.LastExportValue` | 輸出額（世界価格建て） | `max(0, grossVal - importVal)`（grossVal=粗取扱高）。 |
| `World.CumulativeTradeFlow` | 累積貿易収支＝対外基礎貨幣流入 | 毎期 `netFlow + directLedger`（netFlow＝輸出受取−輸入支払い、directは生産者直払い`ExportRevenueToProducers`分）を加算。植民地捕囚市場のrevenueも加算。 |
| `World.LastTariffRevenue` | 関税の実収（正）/輸出補助金（負） | 正の関税＝`importVal×Tariff×TariffRevenueRate`を商人pop貯蓄から国庫へ。負の関税＝輸出額×\|Tariff\|×同率をダンピング補助として国庫→商人（`PaySubsidy`で負値）。 |
| `World.ExternalTradeAccess()` | 交易アクセス（禁輸・交戦で縮む） | 本体は Diplomacy 定義（§6.1）。全州アクセスの基礎倍率で、各州で輸送スロットル`LastTradeThrottle`を乗じて実効化。 |

### 3.2 輸送容量（海運・鉄道・港湾）
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.ShipCapacity` | 海運容量（海上交易の口） | 入力（船財投資で積む）。実効=`(ShipCapacity×CommerceRaidFactor/(1+ColonialShipBurden)+TransportBaseline)×(1/州数)`＝通商破壊・植民地補給占有で減る。 |
| `World.RailCapacity` | 鉄道容量（内陸→港の到達） | 入力（鉄道財投資で積む）。`railToPort = RailCapacity×(1/州数)`。0なら港なし内陸州は世界市場に非接続。内陸は`InlandPortDistanceDecay`と`NearestPortDistanceKm`で減衰。 |
| `Region.PortCapacity` | 港湾容量（港の太さ） | 入力（国家＋資本家投資）。港州の容量に加法的に`portBonus = PortCapToThroughput×PortCapacity`を上乗せ。 |
| `Region.LastPortThroughput` | 港の取扱高（利益・投資判断の基礎） | 決済ループで当該州を通る `輸入量×世界価格 ＋ 輸出量×世界価格` を積算。 |
| `Region.LastTradeThrottle` | 輸送律速（1未満で容量が交易を絞る） | `min(1, cap/vol)`。cap＝港州は`shipEff+portBonus`／内陸は`min(inlandRail,shipEff)+portBonus`、vol＝Σ(潜在輸送量×`TransportWeight`)。 |
| `World.Basin` | 海盆（地中海/黒海/太平洋等） | 入力（enum・座標導出）。敵が扼するチョークポイント封鎖時に距離係数へ `ChokeBlockadePenalty` を加算（地中海=ジブラルタル、黒海=ボスポラス＋ジブラルタル、太平洋=マラッカ）。 |
| `Chokepoint`(`.IsOpen`/`.Owner`) | チョークポイント（海峡・運河） | `Diplomacy.Chokepoints`から動的解決。スエズ/パナマの開通で海路距離短絡、扼者との交戦で封鎖判定`Blocked`→距離ペナルティ。 |

### 3.3 植民地
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.ColonialInfra` | 植民地開発度 | 毎期`×(1-ColonialInfraDepreciation)`で減衰後、投資でdev加算。dev＝`min(ColonialDevStep, 予算/devCost, 在庫律速)`、devCost＝`ColonialShipPerInfra×汽船価格＋ColonialRailPerInfra×線路価格`。私的投資家は限界資源価値>コストの間だけ投資。 |
| `Colonial.OutputCurve(dev,half)` | 産出率（開発度→0..1の飽和曲線） | `dev/(dev+ColonialOutputHalf)`（初期低・逓減）。ColonialPop・識字成長に効く。 |
| `World.ColonialPop` | 植民地人口（国力・スコア集計） | `Σ Colony.PopSize × outputRate`（開発が進むほど数えられる人口）。 |
| `World.LastColonialUpkeep` | 植民地維持費（今月の実支払い・俸給分） | `min(Treasury, totalUpkeep)`。totalUpkeep＝`Σ ColonyUpkeepBase×(1+ColonyUpkeepMilitancyScale×Population×Militancy)`。別途、距離比例で石炭・缶詰糧食を本国市場から物理消費。 |
| `World.ColonialShipBurden` | 植民地補給の海運占有負担 | `Σ ColonyShipOccupyPerDist×(本国→植民地の海路距離km/10000)`。ShipCapacityを`/(1+これ)`で絞る。 |
| `Colony.PopSize` | 植民地個別人口（実体） | 初期`Population×ColonialPopScale`。毎期`×max(0.5, 1+ColonialPopGrowth - ColonialExploitMortality×exploitation×(0.5+Militancy) - malariaDeath)`。exploitation＝`1-ColonialPayShare`。 |
| `Colony.Militancy` | 植民地の不満（独立圧力） | `Clamp(+ColonialMilitancyGrowth×exploitation×(0.3+Literacy) - ColonialMilitancyDecay, 0,1)`。人口×不満が本国の抑え(`Military×MilitaryReadiness×ColonialHoldScale`)超で独立。 |
| `Colony.Literacy` | 植民地の識字 | `min(0.9, +ColonialLiteracyGrowth×outputRate)`（開発との接触で上昇）。 |
| `Colony.Savings` | 植民地の蓄え（捕囚市場の購買力） | 捕囚市場で`spendBudget=Σ Savings×ColonialSpendRate`を本国完成財の購入に充て、revenue分を按分減算し本国ブルジョワへ還流。 |
| `Region.ColonialAccess` / `ColonialOutput` / `ColonialSupply` / `OpiumAddiction` | 州の植民地素材入手率／産出率／供給／アヘン中毒 | Model.cs 定義の州側状態量。供給駆動の植民地流入・封鎖時の入手率を担い、EconomyStep が州市場へ注入（保有植民地×固有資源×産出率）。 |

---

## 4. 政治動学（不満・意識・イデオロギー・政体）

### 4.1 闘争度・政治意識・満足度
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Pop.Militancy` | 闘争度 | 生活不足+政治疎外+失業+群衆熱+格差+民族地位+厭戦を係数加重した target を `MilitancySpeed` でEMA追従。上昇分は `UnityMilitancyRelief×EthnicUnity` で鈍化 |
| `Pop.MilLiving` | 生活不足の不満 | `WantWeight × (1−Satisfaction)`。満足度（経済連鎖の帰結）が唯一の入力 |
| `Pop.MilVoice` | 政治疎外の不満 | `PoliticalDistanceWeight × politicalGap`。politicalGap＝`Consciousness×(1−FranchiseVoice)×dist(IdealPoint, 現政体座標)×0.5`。意識・発言権・理想点/政体座標距離から |
| `Pop.MilJobs` | 失業の不満 | `UnemploymentMilitancy × Pop.UnemploymentRate`。失業率のみ |
| `Pop.MilWar` | 厭戦 | `WarWearinessMilitancy × SelfPower.WarExhaustion`（対外戦争消耗）。単一国較正は0 |
| `Pop.MilCrowd` | 群衆熱 | `BandwagonMilitancy × momentum × (1−Satisfaction)`。momentum＝`(ReformistPower+RadicalPower)/(Coercion×popScale)×BandwagonSensitivity`＝前期運動力÷実効保持力 |
| `Pop.MilGap` | 格差の不満 | `InequalityMilitancy × max(0, GiniIndex − giniNeutral)`。ジニと中立アンカーの差分のみ（既定係数0で不発） |
| `Pop.MilNation` | 民族地位の不満 | `UnitySecessionMilitancy×(1−EthnicUnity)×CoreEthnicShare` ＋ `MinorityMilitancy×(1−CoreEthnicShare)`。EthnicEnabled時のみ |
| `Pop.Consciousness` | 政治意識 | target＝`clamp((Literacy×convert×urban + bandwagon)×press)` を `ConsciousnessSpeed` でEMA。convert＝識字→意識変換、urban＝`UrbanConsciousnessBoost`、bandwagon＝`BandwagonWeight×momentum×(1−Literacy)`、press＝`0.75+0.5×PressFreedom` |
| `ConsciousnessConversion(EducationLaw)` | 識字→意識変換係数 | 教育法から：Public/Compulsory=1.0・Religious=0.15・None=0.1 |
| `SchoolingConversion(Pop,…)` | 就学履歴ストック変換 | 既定（`ConsciousnessFromSchoolingStock≤0`）は即時値convertNowを返す。>0で`Pop.SchoolingConvStock`（`SchoolingStockSpeed`で緩和）と混合＝世代遅れ |
| `World.MeanSatisfactionPol` | 平均満足度（政治用） | Σ`Satisfaction×Size`÷ΣSize＝人口加重平均。経済危機の指標（一党支配→独裁化のゲート） |

### 4.2 イデオロギー・政策選好
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `IdeologyShares[0]` 伝統 | 伝統主義 | 基盤＝地代依存`2×rentier+0.5`（Closedは`4+2×rich`）。意識/闘争が他を加点し正規化で相対低下 |
| `IdeologyShares[1]` 反動 | 反動 | 基盤2（Closed/兵士）＋`2.0×Militancy×conserv`。conserv＝相対的富から（富裕ほど失う物を守る＝1848反動） |
| `IdeologyShares[2]` 自由 | 自由主義 | 基盤＝`(ModernElite?3:1)+3×rich×(1−rentier)`（資本・教育）＋`2.5×Consciousness×prog`（啓蒙）。prog＝1−conserv |
| `IdeologyShares[3]` 共和 | 共和主義 | 基盤＝市民/教育＋`(1.5×Consciousness+1.0×Militancy)×prog`（啓蒙＋過激） |
| `IdeologyShares[4]` ナショ | ナショナリズム | 基盤のみ（兵士4・他0.5）。時代ゲートなし＝初期から |
| `IdeologyShares[5]` 社会 | 社会主義 | 基盤＝`2×poor×(1−rentier)+都市1`＋`(1.0Consc+1.5Mil)×prog`、`×eraSoc`（1830〜／発展度ゲート） |
| `IdeologyShares[6]` 共産 | 共産主義 | 基盤＝`1.5×poor×都市`＋`2.0×Militancy×prog`、`×eraComm`（1848〜） |
| `IdeologyShares[7]` 無政府 | 無政府主義 | 基盤＝poor（窮乏）＋`1.0×Militancy×prog` |
| `IdeologyShares[8]` 帝国 | 帝国主義 | 基盤＝`(ModernElite?1:0)+rich×(1−rentier)`（資本）、`×eraImp`（1860〜） |
| `Ideologies.National(w)` | 国民イデオロギー集計 | Σ`IdeologyShares×(Size×Consciousness×voice)`を正規化。voice＝`IdeologyVoiceFloor+(1−floor)×FranchiseVoice`＝票が声を増幅 |
| `Ideologies.IdealPoint(shares)` | POPの4軸理想点 | `IdeologyShares`を思想座標`IdeologyCoord`で加重平均（参加/強権/経済/国家軍の4軸） |
| `Ideologies.SupportAt(agg,coord)` | 政体支持 | National集計 · 親和（政体座標と各思想座標の距離を`AffMax−slope×距離`で親和化） |
| `PolicyPreferences.Pref(axis,bloc)` | ブロックの政策賛否 | 3ブロックの経済プロファイル（富/地代/所得/人頭シェア・国家歳入要求）から各軸±2で創発。参政権/組合等は闘争度`AvgMassMilitancy`で譲歩変調 |
| `NetSupport`/`OpposingPower` | 純支持/反対力 | Σ`Blocs力×Pref×sign`（＋議会議席`ParliamentLawWeight`・国家歳入）。正で法/スライダーが一歩進む |

### 4.3 政体遷移・正統性・圧政
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.ReformistPower` | 改革派の力 | Σ`Size×Consciousness×(1−Militancy)` ＋組合動員`UnionMobilization`、`×(1+ReformPressureBoost×ExternalPressure)`（明治の追い風） |
| `World.RadicalPower` | 急進派の力 | Σ`Size×Consciousness×Militancy` ＋窮乏挑戦`max(0,Militancy−RiotMilitancyThreshold)×DesperationChallengeWeight×(1−DesperationConsciousnessGate×Consciousness)`（無学の飢えた群衆） |
| 遷移引力点 `attractor` | 政体遷移の引力点 | 各政体で`SupportAt`最大を選ぶ。軍`MilitaryCoupSupport×SoldierForce`・一党支配`DominanceCoupWeight`・ブルジョワ`bourgeoisCoup`を加点。`best−cur > PolityInertia×Legitimacy` かつ 動員>実効圧政 で発火 |
| `World.Legitimacy` | 正統性 | `1−erosion`（またはストック版）。erosion＝`LegitimacyErosionRate×institutionalGap+腐敗侵食+DefeatShockLegitWeight×DefeatShock`（+議会項）。institutionalGap＝`avgConsc×max(0,RepresentationThreshold−Freedom)`。`MandateSpent`減算しclamp |
| `World.Coercion` | 実効圧政/保持力 | `BaseCoercion × Legitimacy × FiscalFundingRatio × warDrain`。warDrain＝`1−WarCoercionDrain×WarExhaustion`（戦時は鎮圧力低下）。遷移判定はさらに×popScale |
| `World.BaseCoercion` | 圧政の基礎値 | `CoercionTarget`（政策・警察予算）由来の入力（`CoercionFollowsForce`時は軍事費から導出）。Coercionの乗数基礎 |
| `World.Freedom` / `FreedomFromLaws` | 自由度 | `political(参政権:0.20〜0.65) + 0.25×PressFreedom(検閲法)`。政体ラベルでなく参政権＋報道法から創発 |
| `PressFreedom(Censorship)` | 報道自由度 | 検閲法から：FreePress=1.0・Censorship=0.6・StatePressOnly=0.0（単一ソース） |
| `World.DefeatShock` | 敗戦ショック | 外部注入され`DefeatShockDecay`で四半期減衰。正統性侵食（`DefeatShockLegitWeight`）とクーデタ力へ |
| `World.ExternalPressure` | 外圧（改革圧力） | 外交由来の入力。`ReformPressureBoost`経由でReformistPowerを押す一本化経路 |
| `InterestGroups.FranchiseVoice` | 実効発言権 | 参政権sとブロックから：Closed=1・近代エリート(Census以上=1/未満`ModernEliteSubCensusVoice`)・大衆(AdultMale以上=1)、`×(女性比 fem)`。MilVoice/National重み/Blocs力の単一ソース |
| `Politics.SoldierForce` | 兵士の政治力 | Σ`Size×(0.2+Militancy)`（不満な大軍ほど大）×`MilitaryCoupWeight×(1+WarExhaustionCoupBoost×WarExhaustion)` |

### 4.4 格差・分離独立・内戦・勢力
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.GiniIndex` | ジニ係数 | 各POPの1人あたり富`Savings/Size`を値・`Size`を重みにした人口加重ロレンツ曲線から`1−2×曲線下面積` |
| `World.GiniNeutralAuto` | ジニ中立アンカー | `GiniNeutral<0`のとき初回実測ジニを自動記録。格差の悪化分（差分）だけがMilGapを駆動 |
| `World.SecessionWorstPressure` | 最大分離圧力 | 州ごと`\|関税姿勢−TariffInterest\|×AvgRegionMilitancy×SecessionPressureScale×(1+CultureSecessionWeight×文化距離)`の最大。関税姿勢＝`Tariff×2−1` |
| `World.SecessionHold` | 中央の保持力 | `Legitimacy/(1+TotalPopulation×SecessionHoldPerPop)` ＋`SecessionHoldCoercion`×(coerce飽和項)。大国ほど辺境を抑えにくい |
| `World.CivilWarYears` | 内戦の残年数 | 暴力転換/分離戦/勢力内乱の発火で`CivilWarDurationYears`に設定、四半期ごと−0.25。0で決着（動員 vs 保持力） |
| `World.CivilWarRebelStrength` | 反乱側の累積動員 | 内戦中の徴集量×`CivilWarRebelSide`を累積し決着時のchallengeへ加算（既定0で不発） |
| `Politics.FactionStrength` | 組織勢力の強度 | `Size×AvgMilitancy×Cohesion×org×(1−voice)×FactionRevoltScale`。org＝未変異は成員意識平均、voice＝ブロック別の平和的回路（票） |
| `FactionState.Cohesion` | 勢力の結束 | 同クラスタが継続観測されるほど`FactionCohesionGrowth×(0.5+ブロック意識)×(変異?2)`で成長、不在時`FactionCohesionDecay`で減衰。F3b内乱の発火ゲート |
| `Politics.AvgMassMilitancy` | 大衆平均闘争度 | 大衆pop（非Closed・非エリート・非兵士）の人口加重平均Militancy。ブルジョワ独裁・参政権譲歩の引き金 |
| `World.MandateSpent` | 消費した政治資本 | 譲歩/押し切り統治で加算し正統性から減算、`LegitimacyRegenRate`で減衰（負のマンデート） |

---

## 5. 統治機構（議会・王権・政府・制度）

### 5.1 議会・選挙・政党
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.Parties` | 政党リスト | 常設3党（保守/自由/社会＝族index0-2固定）＋クラスタ由来の動的政党。`DynamicParties>0`で`SyncDynamicParties`が結束度(`PartyFormCohesion`)・活性本拠州・参政権ブロック票を満たす`Faction`を政党化、適格クラスタ40四半期不在で解党。 |
| `World.VoteShares[3]` | 得票率（族単位・表示専用） | 全pop×`FranchiseVoice(参政権 Suffrage ゲート)`×popのイデオロギー比を保守/自由/社会の3族へ集計し正規化。`ElectoralDistortion>0`で得票を凸変換して第一党プレミアムを付与。 |
| `World.PartySeats[3]` | 議席シェア（政治力学が読む確定議席） | 既定(`SeatsFrozenBetweenElections≤0`)は`VoteShares`を毎期コピー＝両者恒等。凍結時は`SnapshotSeats`が選挙時（選挙政体）／毎期（非選挙政体）にのみ書き写す。 |
| `World.GoverningParty` | 与党（族index） | 選挙政体は`QuartersSinceElection≥ElectionIntervalQuarters`で`Winner`（`VoteShares`＋在任側`IncumbencyBias`最大の族）。非選挙政体は`RulingParty(Polity)`（ブル独裁=自由/プロ独裁=社会/他=保守）。 |
| `World.GoverningPartyName` | 与党の実体名 | `GoverningPartyOf(w).Name`＝与党族の中で`VoteShare`最大の政党名。 |
| `World.QuartersSinceElection` | 前回選挙からの四半期数 | 毎期++、選挙成立時に0リセット。`Winner`の在任効果と`ElectionIntervalQuarters`判定に使用。 |
| `PoliticalParty.VoteShare` / `.AbsentQuarters` | 政党別得票率／連続不在期 | 動的党の得票＝その思想への全国票を族計で比例配分、常設党＝族計−Σ動的。`AbsentQuarters`は適格クラスタ不在の連続四半期（≥40で解党）。 |

### 5.2 法整備の漸進（利益集団ゲート）・政治資本
> 法整備の「現在値」は Government が四半期に、対応する`PolicyAxis`の**純支持`NetSupport`>−θ**（θ＝マンデート余白）を満たす向きにだけ一歩ずつ動かす。抵抗する利益集団（`OpposingPower`）が律速。

| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.Suffrage` | 参政権 | `StepEnum`で目標へ1段（Universalは`WomenPropertyLaw`成立が前提）。近代エリート/ブルジョワが制限選挙で梯子を止める。 |
| `World.EducationLaw` | 教育法 | `StepEnum`。`PolicyAxis.EducationLaw`の純支持>−θで1段。 |
| `World.Censorship` | 検閲・報道自由化 | `StepEnum`。旧支配層が抵抗する向きに純支持ゲートで1段。 |
| `World.LaborUnion` | 労働組合法 | `StepEnum`。資本・旧支配層が抵抗する純支持ゲートで1段。 |
| `World.WorkHours` | 労働時間（短縮=enum減） | `StepEnum`。資本（近代エリート）の抵抗を純支持ゲートで越えた分だけ1段短縮。 |
| `World.CornLaws` | 穀物法 | `StepEnum`。地主が保護（Full）を望み、撤廃は自由/大衆の純支持で1段。 |
| `World.LandReform` | 土地改革 | `LandTaxUnlocked`（検地台帳）が前提。地主の強い抵抗を純支持で越えて1段。`IrreversibleEmancipation>0`で不可逆。 |
| `World.SerfEmancipation` | 農奴解放 | `StepEnum`（布告＝検地台帳不要）。地主抵抗の純支持ゲートで1段、`IrreversibleEmancipation>0`で不可逆。 |
| `World.Pension` | 年金法 | `StepEnum`（`PolicyAxis.SocialSecurity`）。資本/旧支配層が財政負担を嫌う抵抗を純支持で越えて1段。 |
| `World.WomenPropertyLaw` | 女性財産権法 | `StepBool`。旧支配層（家父長制−2）の抵抗を純支持ゲートで越えたとき切替。 |
| `World.Militarism` | 親軍法 | `StepBool`。`PolicyAxis.Militarism`の純支持ゲートで与党綱領（保守=真）へ。 |
| `World.TaxRate` / `World.ProgressiveTax` | 所得税率／累進課税 | `Move`で min(Q5(目標), 上限=`AdminPowerSmoothed/IncomeTaxAdminGate`) へ。富裕の抵抗で律速、純支持>−θでのみ動く。 |
| `World.PollTax` / `World.ConsumptionTax` | 人頭税／消費税（逆進） | `Move`でQ5(目標)へ。`AusterityIntensity>0`のとき下限＝min(1, a×`AusterityTaxHike`)（貸し手有利の緊縮）。 |
| `World.LandTax` | 土地税 | `LandTaxUnlocked`のときのみ`Move`でQ5(`LandTaxTarget`)へ、未解禁は0。 |
| `World.SocialSecurity` | 弱者救済（福祉） | `Move`でQ5(`SocialSecurityTarget`)へ。`AusterityIntensity>0`で上限＝1−a（満額緊縮で0）。 |
| `World.Tariff` | 関税 | `Move`で clamp(Q5(`TariffTarget`)+`TariffRetaliationBias`, `TariffFloor`, 1) へ。 |
| `World.BaseCoercion` | 圧政（保持力） | `Move`で`CoercionTarget`へ。`CoercionFollowsForce`時は目標＝min(兵数×`MilitaryReadiness`×`CoercionPerSoldier`, `CoercionMax`)。 |
| `World.MilitarySpending` | 武力（軍規模＋圧政の駆動） | `Move`でQ5(`MilitarySpendingTarget`)へ。軍系ブロックの政治力で律速。 |
| `World.EducationSpending` | 教育費 | `Move`でQ5(`EducationSpendingTarget`)へ、抵抗で律速。 |
| `World.InterestRate` / `AdminSpending` / `AntiCorruptionSpending` | 金利／行政費／汚職根絶費 | `PolicyBaseSpeed×(Q5(目標)−現在)`で漸進＝行政府/中銀の内部事項で利益集団ゲートを受けない。 |
| `World.PoliticalLockEnabled` | 政治ロック有効 | フラグ。trueで`Move`/`Step`が`NetSupport`をゲートに使う。false（財政単体テスト）は net=1.0＝ゲート/消費とも不発。 |
| `World.MandateSpent` / `MandateSpentNew` | マンデート消費（減衰ストック／今期増分） | `Spend()`が「余白でのみ通った一歩」の代償＝\|純支持\|×`MandateSpendRate`を両台帳へ加算。余白 θ=`MandateWeight`×(`Legitimacy`−0.5)（<0.5で×`MandateNegativeScale`）。Politics が毎期減衰/ゼロ化。 |

### 5.3 王権軸・協力度・軍帰属
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.Polity` | 政体ラベル | `ApplyRegime(target)`が`Crown`/`Seizure`と共に設定。`PolityLabelFromAxes>0`では`LabelFromAxes`＝奪取(`Seizure`)最優先→`ParliamentControl`の立憲床（`ConstitutionalControlFloor`/離脱は`ConstitutionalControlExit`のヒステリシス）→`Crown`形、から毎期導出。 |
| `World.Crown` | 王権形態 | enum None/Absolutist/Despotic/Shogunate。`ApplyRegime`で設定。`RepublicanDeposition>0`＋議会優位＋共和支持差で廃位(→None)、`CrownFormMutation>0`で支持(`SupportAt`)最大の形へ変異。 |
| `World.Seizure` | 奪取形態 | enum None/Bourgeois/Military/Proletarian/Anarchy。`ApplyRegime`で独裁/無政府のtargetに応じ設定。 |
| `World.ParliamentControl` | 議会統制力（大臣責任・予算権の強度） | `ParliamentControlSpeed`で`ParliamentControlTarget(Polity)`（共和1.0/立憲0.65/ブル独裁0.30/プロ独裁0.20/軍政0.10/絶対0）へEMA。`SummonParliamentRate`（緊縮>0.5で↑）、`CrownRollbackRate`（敵対議会×高正統性×`ArmyAllegiance`で↓）等で動く。 |
| `World.CoopParliamentCrown` | 協力度：議会↔王権 | 議会ある政体のみ＝clamp(`CoopClassAffinityWeight`×(1−`GoverningParty`) − `CoopPolicyGapWeight`×政策距離 − `CoopConflictMemoryWeight`×min(1,`MandateSpent`), −1, 1)。 |
| `World.CoopArmyCrown` | 協力度：軍↔王権 | clamp(`ArmyPayWeight`×(2×pay−1) + `ArmyIdeologyWeight`×lean + milAff, −1, 1)。pay=`FiscalFundingRatio`×`MilitaryReadiness`、lean=兵士popの王権寄り傾き、milAff=`Militarism`?`ArmyMilitarismAffinity`:0。 |
| `World.CoopPublicRegime` | 協力度：世論↔体制 | clamp((2×`MeanSatisfactionPol`−1) − `AvgMassMilitancy`, −1, 1)。満足した社会が＋、高闘争が−。 |
| `World.ArmyAllegiance` | 軍帰属（王権+1/議会−1/自律0） | clamp(pay×(lean+milAff), −1, 1)。給与(pay)が0だと方向を失い自律0へ、方向は兵士のイデオロギー傾き＋親軍法。 |

### 5.4 行政・メリトクラシー・制度解禁
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.AdminEfficiency` | 行政効率 | 官僚が買えた行政`AdminFill`の人口加重平均（market）＋貴族素地`aristocratic`（Closed貴族比×`NobleAdminFactor`）を上積み、×民族統一度`unity`×説明責任`accountability`（報道自由×メリトクラシー）。行政財なき世界は1.0。 |
| `World.AdminPowerSmoothed` | 行政力（行政効率の移動平均） | Fiscal で`AdminEfficiency`を`AdminPowerSmoothing`でEMA。Government で所得税/累進の実効上限 ceiling＝`AdminPowerSmoothed/IncomeTaxAdminGate`、土地税解禁`LandTaxAdminGate`を駆動。 |
| `World.Meritocracy` | メリトクラシー（社会移動度） | 全活性州の人口加重平均。州ごと m=clamp(`PolityModernity(Polity)` + 近代エリート人口比×`MeritGroupWeight`, 0, 1)。高いほど識字×高等教育の下層が専門職へ昇格。 |
| `World.Elite` | エリート水準 | TechEducation で `EmergentElite` ?? 平均識字×`EliteMultiplier(EducationLaw)`。特許解禁ゲート`Elite≥PatentEliteThreshold`として参照。 |
| `World.PatentLaw` | 特許制度（解禁フラグ・不可逆） | `Freedom≥FrontierFreedomThreshold` ∧ `EducationLaw`∈{Public,Compulsory} ∧ `Elite≥PatentEliteThreshold` で一度だけ解禁。効果＝技術革新加速。 |
| `World.JointStock` | 株式会社（解禁フラグ） | 全活性州の最大工業化度 maxInd≥`JointStockIndThreshold` で解禁。効果＝各活性州の`IndustrializationBoost`×`JointStockInvestBonus`。 |
| `World.Railways` | 鉄道（解禁フラグ） | maxInd≥`RailwayIndThreshold` で解禁。効果＝各活性州の`ProductivityBonus`+=`RailwayProductivityBonus`。 |

---

## 6. 外交・軍事・国力・人口・技術

### 6.1 国力・ランク・外交
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `NationalScore.Of(World)` | 総合国力スコア | `Military+Wealth+Finance+Tech` の和。軍(2×Military逓減cap≈12)＋豊かさ(GDP逓減＋工業化×9＋満足度×12＋植民地×1.5)＋金融(信用残高/人口)＋技術(`ScoreTechWeight×TechLevel`)。規模より1人あたり発展を重視 |
| `Diplomacy.RankOf` → `PowerRank` | 国家ランク（弱小/中堅/列強） | 実国を`NationalScore`降順ソート。大世界(>12国)は上位`GreatPowerCount`＝列強・次`MiddlePowerCount`＝中堅・残り弱小。小世界は首位比率(`GreatPowerScoreFraction`/`MinorPowerScoreFraction`)。抽象勢力は常に弱小 |
| `Power.Gdp` | GDP（毎月同期） | 背後の`World.Gdp`を毎月コピー |
| `Power.Colonies` / `Colony.Value` | 植民地数／植民地価値 | `Colonies`=`ColonyList.Count`。`Value`=`Resources.Length+0.5+Population×0.5`（資源数＋人口＝争奪優先順） |
| `Diplomacy.Tension(a,b)` | 二国間緊張(0..200) | `AddTension`で蓄積：脅威(`ThreatTensionRate×脅威超過`)・イデオロギー・民族(`EthnicTensionRate`)・法・宗教の隔たり＋AI侮辱。`DriftTension`で減衰、講和で`PostWarTension`にリセット。100以上継続で開戦 |
| `Diplomacy.TradeAccess(fr)` | 交易アクセス(0.1..1) | `1−Σ(禁輸・交戦相手の TradeWeight)`。覇権国はさらに`_hegemonContainLost`を減算（反覇権封じ込め） |
| `ThreatScore`/`ThreatExcess` | 覇権脅威超過 | `0.45×(EffGdp/平均)+0.35×(EffColonies/平均)+0.20×(EffMilitary/平均)−1`。従属国分は`DependentThreatWeight`で加重。平均国=0 |
| `Diplomacy.Hegemon` | 覇権国 | 脅威超過(`ThreatScore`)が最大かつ`HegemonThreatMin`超の国 |
| `Suzerain`/`_dependency` | 従属（属国）関係 | 有向`宗主→従属`。四半期に貢納`VassalTributeRate×従属GDP`。離反＝従属軍が`宗主LandPower×Projection×VassalHoldScale`超＋属国民の平均闘争度が`VassalDefectionMilitancy`超 |
| `Power.FormationTier` / `FormationRule` | 変名段位／統一・formable規則 | `Formations.Check`が、必要州(直接∪従属国領有)＋識別条件(BaseName/文化/宗教)＋文明ゲートに合致する最高Tierへ改名。段位は単調増加のみ |

### 6.2 軍事力・即応度・戦争
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Power.Military` | 陸海軍総合ゲージ | `SoldierSize()/SoldiersPerMilitary × MilitaryReadiness × (1+MilitaryQualityBonus×QualityFill+MilitaryTier3Bonus×Tier3Fill) × cohesion × militarism × techQuality`。cohesion=民族統一度、militarism=軍国法、techQuality=`TechMilLowFloor+(1−floor)×clamp(TechLevel/TechMilFullLevel)` |
| `Power.NavalShare` / `NavalPower` / `LandPower` | 海軍シェア／海軍力・陸軍力 | `NavalShare`=clamp(`NavalShareBase`＋外洋海盆ボーナス＋`NavalShareColonyWeight×植民地規模`＋`NavalEmphasisWeight×(NavalEmphasis−0.5)`)。`NavalPower=Military×NavalShare`、`LandPower=Military×(1−NavalShare)` |
| `World.MilitaryReadiness` | 即応度(0..1) | `clamp(兵士充足(moraleFill) × MilitaryEquipFill)`。moraleFill＝兵士POPの満足度をSize加重平均 |
| `World.SoldierSize()` | 兵士POP総数 | `IsSoldier`なPOPの`Size`合計。徴募＝軍事費が`MaxSoldierFraction×母数`を目標に農民/都市労働者から引抜、戦時は`ConscriptionFraction`で徴兵 |
| `Region.MilitaryEquipFill` | 装備充足率 | 軍需財を必要量のうち買えた割合`clamp(equipGot/equipWant)` |
| `Region.MilitaryQualityFill` / `MilitaryTier3Fill` | 上位(tier2)／tier3軍需充足 | 機関銃/装甲艦・戦車/弩級戦艦を残余予算で買えた割合。戦力の質・最上位乗数 |
| `Diplomacy.SeaControl(x,y)` | 制海権(0..1) | `x.NavalPower/(x.NavalPower+y.NavalPower)` |
| `Diplomacy.Projection(x,y)` | 戦力投射係数 | 距離減衰`1/(1+DistanceWarDecay×海路km/10000)`。海越えなら`×SeaControl`。抽象標的は`HomeDefenseBonus` |
| `Diplomacy.CommerceRaidFactor(p)` | 通商破壊係数 | `max(CommerceRaidFloor, 1−(1−制海権)×CommerceRaidDepth)`。海上交易のみ縮小 |
| `Power.WarExhaustion` | 戦争疲弊(0..1) | 交戦中に`+WarExhaustionRate+WarExhaustionEcon×GdpDrawdown+封鎖痛+財政破綻`で蓄積。1.0で無条件敗北。非交戦は0 |
| `War.Score` | 戦勝点(−1.5..+1.5) | `+WarPushSpeed×(aPush−bPush)+luck`。aPush=`LandPower/戦線数×戦費充足×Projection`。厭戦100%で決定的敗北に上書き |
| 賠償（`ResolvePeace`の`rep`） | 講和賠償金 | `ReparationGdpFraction×敗者GDP×\|戦勝点\|`を国庫上限でクランプ。植民地奪取は戦勝点＝予算で価値順に`ColonyCaptureMax`まで |
| `World.WarDeadCumulative` | 累積戦死者 | `WarCasualtyRate×Military`(月次上限`WarCasualtyMonthlyCap`)を兵士POPから、加えて士官貴族`OfficerCasualtyShare×cas`を累積 |
| `World.ExpansionWill` | 膨張意欲（累積） | 戦争熱が閾値超で四半期`ExpansionWillGrowth×超過分`貯まり、`ExpansionWillCap`上限。植民地化/侵略で`InvadeWillBase+InvadeWillPerMilitary×標的軍`を消費 |

### 6.3 技術・教育・識字
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `World.TechLevel` | 技術水準 | ①採用＝世界フロンティア`×catchFrac`へギャップ比例で後追い(`AdoptionRate`、後発ほど速い)。②革新＝自由＋公教育のみ`InnovationRate×Elite×報道自由×特許/(1+TechLevel/InnovationScale)`。上限`FrontierCeiling` |
| `World.TechPower` / `World.Elite` | 革新能力／エリート水準 | `TechPower=Elite`。`Elite`=平均識字×世俗水路（教育財産出のうち世俗教師の割合、聖職者は`EliteMultiplier(law)`で減衰）。教育財なき小世界は`平均識字×EliteMultiplier` |
| `World.TechFrontier` | 世界技術先端 | 多国シミュの最高`TechLevel`（後発の後追い目標）。単独国は0 |
| `Pop.Literacy` | 識字率 | `LiteracyRate(EducationLaw)×litFactor×(EducationFill−Literacy)`で買えた教育を天井に漸近。litFactor=児童労働率で減速。`LiteracyGenerationalDecay`で供給裏付けなき分は世代減衰 |
| `Pop.EducationFill` | 教育財充足（識字の天井） | 教育財（聖職者/教師が生産）をPOPが市場で買えた割合。買えぬ貧困POPは識字が伸びない |
| `Pop.EducationEma` | 修学年数EMA | `+EducationEmaRate×(EducationFill−EducationEma)`。`EducationGateUsesEma>0`時のみMeritocracyの資格判定に使用（既定dormant） |

### 6.4 人口動態・気候・疫病・犯罪
| フィールド | 日本語 | 算出方法（何から算出するか） |
|---|---|---|
| `Pop.ChildShare` / `OldShare` | 子供比率／老年比率（コホート） | 出生(`CrudeBirthRate×fertility`)で子供補充、`AgingYears`で子供→生産年齢へ送り、成人死亡は`OldMortalityWeight`で老年に厚配分・`OldShareMax`上限。徴兵/復員は成人男性のみ移動し残余比率を更新 |
| `Pop.FemaleShare` | 女性比率 | 徴兵で成人男性が抜け残留集団の女性比率上昇、復員で低下。階級間移動は移動者で加重混合 |
| `Pop.CoreEthnicShare` → `World.EthnicUnity` | 中核民族割合／民族統一度 | 徴兵・復員・征服で加重運搬。異民族植民地征服は`homePop/(homePop+異民族)`で希釈。軍の結束(cohesion)に反映。`EthnicUnity`＝人口加重平均 |
| 扶養力（`capacity`・出生/死亡の純） | 人口扶養力＝人口転換 | ロジスティックで人口が扶養力へ収束(`PopGrowthRate`)。医療(`HealthcareCapacityBonus`)・工業化(`IndustryCapacityBonus`)で↑、都市化(`UrbanCapacityPenalty`)・食料不足(`FoodCapacityWeight×(FoodFill−1)`)で↓。出生率は女性識字(`FertilityLitSlope`)・所得(`FertilityIncomeSlope`)で低下（少子化） |
| `World.HarvestFactor` | 豊凶（局地天候・年次） | 年次AR(1)ロール`1+rho×(前年−1)+√(1−rho²)×ε`。主食RGO産出と自給に乗算。0.80未満で凶作 |
| `World.WeatherBandFactor` | 天候帯係数（緯度帯） | 緯度帯ごとに年次ロール（帯別AR(1)・`WeatherBandPersistence`）。`HarvestFactor`と積算＝同帯の国が一緒に不作。単一国は1.0中立 |
| `World.CropHealth[good]` | 作物別健全度（病虫害） | 作物固有に年次ロールで<1（ジャガイモ胴枯病型）、`CropDiseaseRecovery`で回復（約4年）。栽培州産出に乗算＝天候と三重積算 |
| `Climate.StapleSelfSufficient` | 主食自給免除（気候帯） | goodIdで分岐：麦/野菜=普遍、米=`RiceSuitable(\|lat\|≤32 or アジア夏雨帯)`、ジャガイモ=`PotatoBelt(lat≥45)`、トウモロコシ=`MaizeBelt(lon≤−30)` |
| `World.EpidemicActiveYears` | 疫病の感染窓 | 発生時`EpidemicContagiousYears`をセット、年次減衰。>0＝流行中＝他国への感染源（近接＋交易接触で伝播）。致死率＝`EpidemicSeverity×(1−0.7×衛生)×jitter` |
| `World.CrimeRate` | 犯罪率（最悪州） | `CrimeBase×(1−秩序)×clamp(CrimeMilitancyWeight×criminal/人口)×(1+CrimeUrbanWeight×都市化率)`。criminal＝Σ`Size×Militancy×(1−Satisfaction)` |
| `World.PublicOrder` | 治安（秩序） | `clamp(実効圧政/(人口×CrimeOrderPerPop))`。実効圧政＝`Coercion×人口/PoliticalReferencePop`。政治無効時は1.0 |

---

## 付記：参照ソースファイル

| 章 | 主な参照ファイル |
|---|---|
| 0 分類 | `Model.cs`, `World.cs` |
| 1 経済コア | `EconomyStep.cs`, `MarketFlow.cs`, `LaborSupply.cs`, `PopDecision.cs`, `Transport.cs`, `Model.cs` |
| 2 財政・金融 | `Fiscal.cs`, `Credit.cs`, `Bank.cs`, `CapitalMarket.cs`, `World.cs` |
| 3 国際貿易・植民地 | `InternationalMarket.cs`, `Trade.cs`, `Colonial.cs` |
| 4 政治動学 | `Politics.cs`, `Ideology.cs`, `PolicyPreferences.cs`, `Policy.cs`, `InterestGroups.cs`, `Factions.cs` |
| 5 統治機構 | `Parliament.cs`, `CrownParliament.cs`, `Government.cs`, `Institutions.cs`, `Meritocracy.cs` |
| 6 外交・軍事・人口・技術 | `Diplomacy.cs`, `Military.cs`, `Formations.cs`, `TechEducation.cs`, `Climate.cs`, `Crime.cs` |

- 係数の既定値・意味は `Coefficients.cs`、休眠係数の棚卸しは `docs/2026-07-08_休眠係数一覧.md` を参照。
- 本書は 2026-07-13 時点のソースに基づく。恒等（中立）注記のある機構は、対応する係数を有効化しない限り従来挙動と一致する。
