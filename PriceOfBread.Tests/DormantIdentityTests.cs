using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>休眠フラグの恒等の番人（2026-09-03・キューD-40／出典 トリアージ 2-D 10-7a）。
///
/// ⚠**きっかけ**：規律③「新機構は既定0で入れ、1ビットも変わらないことを証明してから点火」は、
/// 証明を**人が手で**（measure.sh の before/after diff）やる約束だった。手でやる約束は、忙しい日に飛ぶ。
/// ★ここでは「休眠と名乗る係数を0のまま走らせた世界」と「素の世界」が**同じ軌道になる**ことを自動で確かめる。
/// ⚠これは点火済みの係数を0へ倒すテストではない（それは挙動を変える＝別の話）。
/// 既定値がそのまま0の係数を明示的に0で上書きしても何も変わらない、という**トートロジーの検査**である。
/// それでも価値がある：**誰かが既定を0以外に変えた瞬間に赤になる**＝「休眠のはずが点火されていた」の検出器。
///
/// ★走行は短距離（助走＋8四半期・数秒）。長距離の恒等は measure.sh の diff が担当する（2段構え）。</summary>
public class DormantIdentityTests
{
    /// <summary>★ここに並ぶのは「休眠0で入れた器」＝点火時にこの表からも外す（＝表そのものが休眠の一覧になる）。</summary>
    private static readonly string[] DormantZeroFields =
    [
        // ★GoldRushEmergent は 2026-09-04 に 1.0 で**点火**（J-005）＝この表から外した。
        // ★ChilledMeatEnabled は 2026-09-04 に 1.0 で**点火**＝この表から外した。
        // ★AiBureaucracyFromPrereq は 2026-10-17 (389) に 2 で**点火**（欲しい法が官僚制度の前提で止まったとき、法律タブの門も通る法だけ官僚制度の目標を上げる）＝この表から外した。(599) で旗ごと削除（RetiredFields）。
        // ★StateEduDemandInFront（(452) 国庫の教育の買いたい分を値段が読む需要へ）は 2026-10-20 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★ServiceClosedFixedPoint（(spa) A′ 役務の不動点の閉じた式）は 2026-10-23 に 1 で**点火**（S1c・ServiceClosedMaxCurv=0.01）＝この表から外した。
        // ★LaborClosedForm（(spb) 案 B 労働の閉じた式）は 2026-10-24 に 1 で**点火**＝この表から外した。
        "UrbanCapOff", "CapitalHeadroomFromReturn",   // (605) 線 ind4：工業化度の読み手を盤面の量へ置き換える旗の束（既定 0＝今のまま・番人 IndustrializationReaderTests）
        // ★TechMultipliersOff（(613) 線 tl 段3）は 2026-10-28 に 1 で**点火**＝この表から外した。TechGateByDiscovery・TechSignalOff・TechThresholdOff・EraOff（段1・2a・2b・4）も同日に 1 で点火。TechProductivityBracketOff（段5・腕 B5）は既定 0 のまま〔cp3 と同じ線か後〕
        // ★InheritanceLawGate（(650) 相続法）は 2026-10-30 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は InheritanceLawTests。
        "TechProductivityBracketOff",
        // ★StanceEduHoldsLaw（(680) 線 edq）は 2026-10-30 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は StanceEduHoldsLawTests。
        "SpreadNeedSocialismBoard", "SpreadNeedCommunismBoard",   // ★(640) 線 idn：思想の必要性を盤面の量から読む旗（既定 0＝今の式・番人 IdeaNeedBoardTests・G2b で落ちて差し戻し）。SpreadNeedFascismDecline は 1 で点火・IdeologyDevTermOff は読み手ごと消した（IdeologyDevFromUrban ほか 3 本も削除）
        // ★SpreadLiteracyFull・SpreadPrereqEdges（(621) 線 inst）は 2026-10-28 に点火＝この表から外した（SpreadLiteracyFull は既定 0.5・辺は Catalog.After の共産主義 ← 社会主義へ移して旗を消した）。旗 0 の経路の番人は InstitutionAnchorTests。
        // ★MandateWallStockCap（(615) 政治資本の壁の一歩の勘定）は 2026-10-28 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。旗 0 の経路の番人は MandateWallStockCapTests。
        "CriminalClassEnabled",         // ★(366) 治安と所有の安全 段0 の器＝犯罪者 POP を並べる主フラグ（0＝Pops.Count も CriminalPopIndex=-1 も不変）。点火は段1（腕 A）
        // ★NavalPortProcurement（(606)(669)(679) 線 navf4 海軍の需要の道）は 2026-10-30 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は NavalPortProcurementTests（旗を 0 に置いた走行）。
        // ★ModernizationAbolished（(573) 近代化投資の廃止・作者 2026-10-26）は 2026-10-26 に 1 で**点火**（腕 B_mod・止める線に当たらず）＝この表から外した。
        // ★ClearingIdleThrottledScan・ClearingIdlePotentialReweight（(434) 清算の片方向の錠）は 2026-10-19 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★DomesticServiceMarketRation（(433) 家事奉仕を優先配分から外す）は 2026-10-19 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★HouseholdProductionEnabled は 2026-10-19 (418) に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★ClearingIdleSearchQuarters・ClearingPartnerScope（(403) 清算の見る範囲の絞り）は 2026-10-18 に既定 4 へ**点火**（旗 ClearingIdleShortageWakesStaplesOnly は主食の線として無条件へ焼き込み・削除）＝この表から外した。
        // ★OpeningFiscalAlign（(400) 英・蘭の開幕の財政の整合）は 2026-10-18 に 1 で**点火**（既定を 1 へ・止める線に当たらず）＝この表から外した。(405) で焼き込んで旗ごと物理削除した。
        // ★MandateOverStockFromLegitimacy・MandateOverStockLegitFloor（床 0.4）・ParishEduFunded（(404) 専制の改革の経路）は 2026-10-18 (405) に 1／0.4／1 で**点火**（腕 BF・止める線に当たらず）＝この表から外した。
        // ★AdminServesStateRule（(408) 行政費の返り）は 2026-10-18 に 1 で**点火**（止める線に当たらず）＝この表から外した。
        // ★MandateOverStockLegitPower（(409) 壁の一歩の払える量＝avail^p）は 2026-10-18 に 2 で**点火**（腕 B・止める線に当たらず・同時に床の係数を削除）＝この表から外した。
        "PrivilegeFromCrownShare",      // AUD-07：身分制免税の政体バフ除去
        // ★WeaponParallelEnabled は 2026-10-24 (537) に採って旗ごと削除（既定で並行・退場の表からマスケットと鋳鉄砲を外した）＝この表から外した。
        // ★SymmetricTensionVerb は 2026-09-28 に点火して旗ごと削除（回8-1・dip-04）＝この表から外した。
        "RebelArmsMarketEnabled",       // ★武器の市場購入（設計メモ§7 (a)・2026-09-11）＝反乱勢力が参加POPの貯蓄で国軍と同じ財を買う＋法「民間の武器購入」＋FactionStrength 第5因子。
                                        //   0＝EconomyStep の枝に入らず（rebelDemand 全0）・法は7箇所全部が素通り・第5因子は掛けない＝厳密恒等。点火は 1.0（0/1の主フラグ）
        // ★FamineDriveGain は 2026-09-22 に 10 で**点火**、NeedTierSplit は同日に旗ごと削除（階層3分割を既定化＝作者裁定2026-09-20）＝この表から外した。
        // ★ParticipationDiscontent は 2026-10-19 (431) に 1 で**点火**、DiscontentSplit は焼き込んで旗ごと削除（二量分離の完成＝闘争度の目標から生活の項を外し、参加の述語と思想の更新の入力は max(不満度, 闘争度)）。この表から外した。
        "AssimilationRevival",          // ★段4（2026-09-09・§7-7バグa後半）＝民族復興＝抵抗の床を外して符号を反転させる（勾配の差し替え）。
                                        //   0＝床あり＝厳密恒等。点火値 1.0526＝1/0.95（反転点＝ボヘミア1900の識字0.95）
        "NationConsciousnessDerived",   // ★段4（2026-09-09・作者裁定5／§7-7バグb）＝国民意識をストックから**同じ漸化式の不動点**へ。
                                        //   0＝旧＝ストック＝厳密恒等。点火値 1.0（記憶が消え、国内経路の運搬漏れが定義ごと消える）
        "SerfGateCoversPool",           // C-03：農奴の移動制限を失業プール経路へも広げる
        // ★DomesticFlightHasHost は 2026-09-22 に**点火**（旗ごと削除＝逃避の受け皿が常に外資の負債を記帳）＝この表から外した。
        "NiterBedEnabled",              // 硝石丘＝硝石の低効率レシピ（財の3分類の再開）＝0で足さない
        // ★ScarceCountsIncludeColonies（希少鉱物の総量保存の k/N に植民地を数える）は 2026-10-04 (197) で**点火**（反証役 vq2#1）＝この表から外した。
        "ConsumeBudgetSavingsRatePerMonth",   // ★(273) 線P 段5＝消費の予算を min(予算, 前月の所得＋貯蓄×d) で切る（EconomyStep.Run ③の入口）。0＝分岐に入らず評価しない＝厳密恒等（d＝0 を式に入れると min(予算, 前月の所得) になり恒等でないのでコードは d＞0 のときだけ評価する）。腕は POB_COEF で置く。
        // ★LuxuryIncomeElasticity・DomesticServiceIncomeFactorExponent（(302)(329) 奢侈品と家事奉仕の所得弾力）は 2026-10-14 (331) の合流で**点火**（0.5・1）＝この表から外した。
        "PrivilegeLedgerEnabled",       // 特権の台帳（段1・観測のみ）＝0で1命令も走らない
        "PrivilegeStripMilitancy",      // 特権システム 段3＝剥離の怒り（PrivilegeLedgerEnabled>0 が前提）＝0で加算なし
        "PrivilegeMonopolyInLedger",    // 特権システム 段5前半＝特許会社の取り分を台帳へ（記帳のみ）
        "GuildLawGate",                 // 特権システム 段4＝同業組合法の主ゲート（法が動かない）
        // ★LaborFloorOffGate・LaborBandFloorOffGate・MinimumWageLawGate（(469) 労働の下限の撤去と最低賃金法）は 2026-10-20 (475) の合流で 2・1・1 へ**点火**＝この表から外した。
        "GuildRentPremium",             // 特権システム 段4＝職人の上乗せ（0＝なし）
        "GuildMobilityDrag",            // 特権システム 段4＝工場への移動の遅れ（0＝なし）
        "MintParitySells",             // ★器（2026-09-07）＝床の standing offer を両側に（平価の上で準備を平価売り＝平価買いの逆操作）＝0で従来（片側）
        "ProjectionUsesLand",           // 支配度doc §14b＝陸路の起点（ProjectionRuleMin=1.1 が主ゲート＝既定で集合={首都}）
        // ★BuildingGoodBundle（(550)(555) 工場の建屋の財・建設の束の 2 枠）は 2026-10-25 (555) 線 fb2 で既定 1 へ**点火**＝この表から外した。
        // ★MusketRecipeEnabled（前期の軍需レシピ）は 2026-09-27 回2-1 で**点火**（旗ごと削除）＝この表から外した。
        "SilverRushEnabled",            // 銀の後期産地＝シルバーラッシュ（0＝解禁も富鉱化もしない）
        "MineralCostFollowsGrade",      // ★器（2026-09-07）＝鉱脈の倍率を鉱物RGOの限界費用にも通す（EconomyStep.MineralSelfCost）。0＝鉱脈で割らない＝厳密恒等
        "EndowmentCostFollowsAbundance",   // ★器（2026-09-08）＝賦存倍率>1 の州の自己費用を賦存で割る（Simulation.ApplyEndowment）。0＝MarginalCost そのまま＝厳密恒等
        "EndowmentCostFollowsScarcity",    // ★器（2026-09-08）＝賦存倍率<1（非産地0.4）の州の自己費用を上げる（同上）。0＝上げない＝厳密恒等
        // ★RecipeCostFollowsProductivity（(328) 流れG ①＝動力の別レシピの自己費用を生産性の比で割る）は 2026-10-14 (331) の合流で**点火**＝この表から外した。
        // ★FacilityBuildOutlayIncludesMaterial（裁定②の安全弁）は 2026-09-27 線2 で**点火**（旗ごと削除）＝この表から外した。
        "FactoryBuildRefIsReal",        // 進捗/素材ゲートの参照束を実質（×PriceLevel）へ＝兄弟ゲートと同じ物差し（0＝名目＝旧恒等）
        // ★PriceLawSupplyIncludesStock と TradeBuyerSolvesAtUnitPrice は 2026-09-08 に 1.0 で**対で点火**（腕 price2b）＝この表から外した。
        "PeacetimeConscriptGrievance",  // 支配度doc §15＝平時の植民地徴兵の遺恨
        // ★B9（州一揆の保持力の州按分）・項目2(b)（F1鎮圧枝の緩和なし）・項目3（F1の州選択＝熱×群衆）は 2026-09-11 に1腕で**点火**（係数ごと削除）＝この表から外した。
        // ★ColonyPlantationRent（実バグ B5＝植民地格の昇格州に大農園の地代）は 2026-10-02 (173) で**点火**（R1＋c6・旗ごと削除）＝この表から外した。
        "HeldSinceFromData",            // 実バグ B14＝world.json の heldSince を読む（0＝全州 Core・支配度1）
        // ★CivilWarPredicateUnified は 2026-09-08 に 1.0 で**点火**（F3b蜂起doc §5 段3・作者裁定②）＝この表から外した。
        "RevoltIgnitionIntensity",      // F3b蜂起doc 段4＝点火の付け替え（門1＝FactionStrength÷f.Size の示強量）。0で旧2門（力の門＋好機/圧倒）＝厳密恒等
        "RevoltGrievanceWeight",        // F3b蜂起doc 段4＝門2の怨嗟の重み（min(1, FactionRejectionMutation×Rejections)）。0で加算項が消える
        "RevoltVoiceFloor",             // F3b蜂起doc 裁定1＝(1−発言権)の床（旧支配層と普選後の大衆の恒久ゼロを外す）。0で FactionStrength は1命令も変わらない
        "RevoltMilitiaRaise",           // F3b蜂起doc 段5＝蜂起した運動のブロックのPOPが不満度に応じて武器を取る率（Pop.MilitiaShare）。0で誰も札を持たない＝全読み手が恒等
        "MilitiaCombatValue",           // F3b蜂起doc 段5＝民兵1人あたりの戦力 a（<1＝弱兵）。戦場の民間人死の配分の重み 1+a×民兵率 も同じ a。0で戦力にも死の配分にも寄与しない
        // ★SoldierDefectionByMilitancy（F3b蜂起doc 段5＝守備兵の寝返りは割合）は 2026-09-29 処理の一元化 段3 で**点火**（係数ごと削除）＝この表から外した。
        // ★RevoltCooldownRetire（F3b蜂起doc 段6＝共有クールダウンの退役）は 2026-09-29 に**点火**（作者裁定「根拠のない待ち」・旗・タイマー・係数ごと削除）＝この表から外した。
        // ★要求は法の経路（作者裁定2026-09-11）は同日に**点火**（主フラグごと削除・旧3枝も物理削除）＝この表から外した。
        // ★FiscalLawStages（税・支出・軍事費の段＝作者裁定2026-09-12 R1/R2）は 2026-09-20 に**点火**（旗と段の無い世界の枝ごと削除・AiMobilization も同時に削除）＝この表から外した。
        // ★CoupForceShare／CoupMassArmyDiscount（作者裁定2026-09-19 R3/R4＝クーデタ項の取り分化と職業軍の度合い π）は 2026-09-20 に既定化（腕 A4）＝旗と旧式の枝ごと削除＝この表から外した。
        // ★EquipNeedsDiscovery（装備の発見）は 2026-09-27 回2-1 で**点火**（旗ごと削除）＝この表から外した。
        "TeacherQualityWeight",   // 知識と人的資本の経路（作者採用2026-09-06）＝0で従来（★EduFundingCashClamp は 2026-09-27 線2 で点火・旗ごと削除）
        "EduReachFromAdmin",            // 特権 段1（作者裁定2026-09-08）＝義務教育の国庫給付の天井に行政の充足率（0で×1.0＝給付は行政を見ない＝従来）
        "EduProducersCoexist",          // ★教育の三層化 段1 器（2026-09-12）＝教区学校（聖職者・紙）と公立学校（教師・書籍）を常設2件で並置し教育法を非活性側の Scale=0 へ降格。
                                        //   0＝AddServiceProducers が従来の1件だけを add・StepMonth の月次差し替えがそのまま走る・EconomyStep は Scale を読まない＝ビット恒等。点火は段3（batch30 単独＋savetest）
        // ★下2本は D-35 の反射走査が拾った（**初期化子を書かない宣言**＝`public double X;` なので静的なテキスト検索では見つからない）。
        //   ⇒ 反射で数え上げる形にした甲斐がそのまま出た1件目。
        "PopDebtDriven",                // POP個人債務の主フラグ（0＝誰も読み書きしない厳密恒等）
        // ★DiscoveryPriceInitFromCost・DiscoveryCarrierDownstream（流れG）・DomesticServiceProductivity（既定 0.30）は 2026-10-17 (388) で**点火**＝この表から外した。
        "SilverParityFromRatio",        // ★器（2026-09-07）＝世界銀価格を同一市場内の 銀/金鉱 比から作る（E→平均→E の自己参照を断つ）。0＝旧統計 Market.Price[銀] を注入＝厳密恒等
        "DiscontentLawGap",             // ★二量分離 段1（2026-09-09）＝不満度（導出量）の「法の隔たり」の物差しを PolicyPreferences.LawGap へ（0＝旧＝4軸座標の距離）。
                                        //   ⚠段1は Politics.Discontent の**読み手がゼロ**なので、この係数はどちらの値でも世界を動かさない（点火は段2＝読み手を付けるとき）
        // ★B-03（WarFeverUsesBudgetShare）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
        // ★B-06（TransportBaselineIsLand）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
        // ★AiModernizeFromReturn（(416) AI の近代化投資の信号を設備の収益へ）は 2026-10-19 に 3 で**点火**（止める線に当たらず）＝この表から外した。(591) で旗ごと削除（RetiredFields）。
        // ★AiLawGateFromBoard（(423) 工業化度を読む AI の法の門を盤面の実物へ）は 2026-10-19 に 3 で**点火**（止める線に当たらず）＝この表から外した。(599) で旗ごと削除（RetiredFields）。
        // ★ScoreFromWorldShare（国力の物差しを世界の総量に対する割合へ）は 2026-10-20 (449) に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        "DiscoveryClockMotorParent",    // ★(461) 同・電動機の親の産業を蒸気機関の財にする別の旗（DiscoveryClockWorldPerMonth が正のときだけ読む・既定 0＝今の規則）
        "OpeningBakeEnabled",           // ★(490) 開幕の焼き込みの読み込み（<see cref="OpeningBake"/>）＝0 で表（opening_bake.json）を開きもしない。倒さない（焼くのは工業化度の削除の後・(464) の順）。鍵の一致と読み込みの往復は OpeningBakeTests
        "OpeningMoneyFromRatioMode",    // ★(574) 開幕の貨幣の量（読む線 sv〔(564)〕）＝0 で助走の各四半期の末は貯蓄にも与信にも触れない。1＝全 Pop の貯蓄と与信（Loans・HouseholdLoans・Pop.Debt）を国ごとの倍率で戻す。採否は線 svf の腕 B1（事前登録 measurements/svf574_20261026_prereg.txt）
        // ★DevaluationCapReliefExp（(587) 切り下げの月の上限の緩め）は 2026-10-27 に 1 で**点火**（線 hyp の腕 B・止める線に当たらず）＝この表から外した。0 に戻せば旧の切り（貯蓄×DevaluationCapFracPerMonth）。
        // ★StapleLedgerSum・HarvestShortfallToMarket（(504) 食料の台帳を主食の和へ①③）は 2026-10-23 に 1 で**点火**（腕 C・止める線に当たらず）＝この表から外した。②は表に残る。
        "StapleDemandRebalance",        // ★(504) 主食どうしの乗り換えの配り直し②（挟みを外し欲しい量の和を必要量に合わせる）＝0 で今の挟み。点火は同上
        // ★UnionBargaining（(520) 交渉の取り分）は 2026-10-24 に 1 で**点火**・LandRentResidual（産地の地代の残りの道）は (529) で撤回して旗ごと削除（腕 B1″〔種 21〜30〕・B2・合成の腕 B3・止める線に当たらず）＝この表から外した。
        // ★RentScarcityElasticity（産地の地代の土地稀少弾力）は (529) で既定 0＝休眠にし、(541) で墓標へ消した＝この表から外した（復活を禁じる番人は Hz8IdentityTests）。
        // ★LordOppositionByRentLoss（(547) 農奴解放・土地改革への旧支配層の反対の強さを次の一段で失う所得の割合へ）は 2026-10-25 に 1 で**点火**（腕 B・計器つき 10 種・止める線に当たらず）＝この表から外した（0 の経路は Rs2LordOppositionTests が踏む）。
        // ★LordOppositionReverseGain（(558) 農奴解放・土地改革への旧支配層の減らす向きの強さを 1 段戻したときに得る所得の割合へ）は 2026-10-25 に 1 で**点火**（腕 B・計器つき 10 種・止める線に当たらず）＝この表から外した（0 の経路は Rs3LordReverseGainTests が踏む）。
        "TradeCheapSellSubsidy",        // ★(471) 〈安く売る〉の輸出奨励金（交-16・0＝旧＝清算は +1 しか読まない）。1 は −1 を +1 の鏡像として読む。腕 B で採否
        // ★RecipeEquipment・IndustrializationOff（(495) レシピごとの設備と工業化度の倍率の削除）は 2026-10-23 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★MinimumWageAiFromFloor（(480) AI が最低賃金法に置く目標を盤面の実物から選ぶ旗）は 2026-10-21 (486) に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
    ];

    /// <summary>★1.0 が恒等の scale 型の器の休眠表（レビュー2026-09-07）：反射走査（既定0だけを数える）の外にあるので手で書く。点火時にこの表からも外す。</summary>
    private static readonly string[] DormantOneFields =
    [
        "MilitaryProcurePriceGateScale",   // 軍需財の国家調達の価格ゲート倍率（1＝満額＝旧恒等・0＝価格を見ない）
        "FactoryBuildPriceGateScale",      // 裁定②＝工事の進捗/素材需要の価格ゲート混合（1＝旧恒等・0＝価格を見ずに進む）
        "UnificationV1Only",               // ★2026-09-26（fis-27・dorm-52）：統一戦争の名指し（北ドイツ連邦・ドイツ・イタリアの3件）＝1が旧挙動。点火（0＝名指しの撤去）は回8-3（col-19）
    ];

    [Fact]
    public void scale型の休眠器は既定で1である()
    {
        var c = new Coefficients();
        foreach (var name in DormantOneFields)
        {
            var f = typeof(Coefficients).GetField(name);
            Assert.True(f is not null, $"休眠係数「{name}」が Coefficients に無い（改名したら本テストの表も直す）");
            Assert.True(f!.FieldType != typeof(int) && UnitValues.TryToDouble(f.GetValue(c), out double d) && d == 1.0, $"scale型の休眠係数「{name}」の既定が1.0でない（点火したなら表から外す）");   // ★(250) 取り出しは橋の1本（double・専用の値型。int は従来どおり対象外）
        }
        // ★2026-09-26（fis-27・dorm-52）：投射の起点を州の支配へ広げる旗は「到達しない値」（>1.0）で休眠＝支配の国家区分 Rule[0] は1を超えない。
        //   点火（≦1.0）は dorm-09（支配の4区分）の後＝回6-4。値が1のちょうど上なので上の表（==1.0）にも既定ゼロの表にも載らない＝ここで見る。
        Assert.True(c.ProjectionRuleMin > 1.0, $"休眠の ProjectionRuleMin が {c.ProjectionRuleMin}＝到達する値（点火したなら本テストからも外す）");
        // 機構の恒等＝既定の FactoryBuildProgress は FactoryBuildAfford そのもの（ビット同値・参照束の物差しも名目のまま）
        foreach (var (now, rf, pl) in new[] { (1.0, 1.0, 1.0), (3.7, 1.2, 1.4), (0.2, 5.0, 0.7), (12.0, 2.0, 3.0) })
            Assert.Equal(c.FactoryBuildAfford(now, rf), c.FactoryBuildProgress(now, rf, pl));
    }

    [Fact]
    public void 休眠フラグは既定でゼロである()
    {
        var c = new Coefficients();
        foreach (var name in DormantZeroFields)
        {
            var f = typeof(Coefficients).GetField(name);
            Assert.True(f is not null, $"休眠係数「{name}」が Coefficients に無い（改名したら本テストの表も直す）");
            Assert.True(UnitValues.TryToDouble(f!.GetValue(c), out double v), $"休眠係数「{name}」が double・int・専用の値型でない");   // ★(246)(248) 専用の値型の係数も読む（取り出しは橋の1本）
            Assert.True(v == 0.0,
                $"休眠のはずの「{name}」が既定 {v}＝**点火されている**。点火したなら本テストの表からも外すこと（表が休眠の一覧である）");
        }
    }

    // 【削除2026-09-26（棚卸し fis-22）】「休眠フラグをゼロで明示しても軌道が変わらない」＝既定値そのもの（0）を明示で書き直して8四半期回す検査＝必ず一致する
    //   （情報を増やさない約56四半期ぶんの計算）。既定が0であることは上の「休眠フラグは既定でゼロである」が見る。

    /// <summary>★D-35「死蔵ゼロ」の台帳（2026-09-03・出典 トリアージ 2-D 10-1b）。
    ///
    /// ⚠**きっかけ**：AUD-09 の実測で `DomesticFlightHasHost`（2026-08-14に入った器）が上の休眠表から**漏れていた**。
    /// 手で書く表は漏れる。⇒ 既定が 0 の係数を**全件数え上げ**、上の休眠表かこの台帳のどちらかに載っていることを求める。
    ///
    /// ★この台帳は**是認ではなくスナップショット**である。ここに並ぶのは「2026-09-03 時点で既に既定0だった係数」で、
    /// 器（休眠フラグ）なのか、0が正しい較正値なのか、単なる死蔵なのかは**まだ仕分けていない**。
    /// ⚠仕分けは D-35 の後半（摂動走行＝0を1に振って何も動かない係数を死蔵と判定する）で行う。
    ///
    /// ★それでも今この検査に価値があるのは、捕まえたい失敗が「**新しく器を入れて休眠表に載せ忘れる**」だからで、
    /// それはこの台帳にも載っていない新しい名前として必ず現れる（＝赤になる）。
    /// ⚠点火して既定が0でなくなった係数はここから外すこと（残っていても赤にはならないが、台帳が嘘になる）。
    /// ★2026-09-26（fis-27・dorm-52）：点火済みの DevelopmentMemory（0.4）・PanicBuyWeight（0.5）・ScarceMineralGate（1.0）が残っていた＝外した。
    /// ★2026-09-27（回4-3 の再挑戦）：ShipFuelRate を点火（0.0016）＝外した。
    /// ★2026-10-01 (151)：SeaEscortPerTradeValue を係数ごと削除（通商保護は割合の形＝ArmyShape.SeaTradeShare・2026-10-03 (183) に割合の形の項も関数ごと削除）＝外した。</summary>
    private static readonly string[] ZeroDefaultLedger =
    [
        // ★D-35 の反射走査が拾った「初期化子なし」の宣言（`public double X;`）＝静的なテキスト検索では見えない4本。
        //   いずれも休眠中の POP個人債務（主フラグ `PopDebtDriven`）の**下位パラメータ**＝主フラグを点火するときに値が入る。
        //   ⚠主フラグではないので休眠表には置かない（休眠表は「これ1本で機構が入り切りする器」の一覧である）。
        "PopDebtLossShare", "DebtRepayFrac", "DebtRuinRatio",
        "HouseholdCreditMonths",
        "AiDevelopmentalSubsidy",   // AiMobilization は 2026-09-20 に係数ごと削除（段の点火で総動員のAI分岐が常時 live）・AiCrisisBorrow は 2026-09-28 に係数ごと削除（借入の自動化で予約の列が消えた）
        // BondRateDebtQuad は 2026-09-29 に係数ごと削除（国債の金利を史実から出し直した＝債務の二次項は英の債務比2.5 の史実と合わない）
        // CoercionUpkeepToSoldiers は 2026-09-30 に係数ごと削除（連合維持費は兵士と官僚の役務の④へ全額＝支払いの段3-2）
        "ElectionBribePovertyWeight", "EraNavalFuel",
        // FertileAbsoluteBlend は 2026-10-25 (551) に係数ごと削除（RetiredFields）。FamineAbsoluteThreshold は 2026-09-30 (131) に局地飢饉の死と一緒に係数ごと削除
        "FoodIncludesAnimalProtein", "GarrisonProcurement",
        "HigherEduNeedPerEducated",
        "InterestCapitalization", "LoanSpreadPerYear",
        "ModernizationToHigherEdu",
        "ScholarshipTopStage",        // ★(597) 奨学金の法の旗（教育費の軸の最上段だけ・既定 0・高等教育は既定で二重に休眠。(590) の ScholarshipLaw は削除）
        "NavalFuelPerGauge",
        "PolicyRateLoanWeight",
        "PromotionGateHigherEdu", "ReactionFromLoss",
        "ResearcherFrac",
        // SeaEscortPerTradeValue は 2026-10-01 (151) に係数ごと削除（通商保護は割合の形）＝外した・SilverStandardIndThreshold は 2026-10-04 (210) に係数ごと削除＝外した
        "SoeSaleMilitancyCeil", "SpecialistExitBanPrefScale",
        "SubmitProbabilistic",
    ];

    /// <summary>★休眠の棚卸し（2026-10-11 (253)・measurements/dormant_20261011.tsv の行き先「消す」）で削除した係数の名簿＝**再び入れたら赤**。
    /// 期待値の出所は measurements/dormant_20261011.tsv の「消す（済み）」の行を手で写した名簿であって Coefficients ではない（同語反復にしない・tsv を読みはしない）。消した理由は各名前の注記。
    /// ★(254) 反証役 vz#1：CohortGateK・EnclosureRate・OpulenceInfraChannel・RuralPromotionRate の削除は担当の推奨（作者の承認は未確認）＝作者の 2026-09-21 の言葉は「生きた一覧の矛盾は棄却側で閉じる」（一覧の付け方）で削除の指示ではない・再測は git の履歴から。
    /// ★(255)：NobleInvests（貴族も投資家に含める述語の開閉・2026-08-14 差し戻し）を足した＝消す前の恒等は帯3本の全行バイト一致で示した（(255)）。
    /// ★(375)：OpeningSavingsRebaseMode・SavingsYearsByClass は開幕の貯蓄の置き直しの機構の削除（実装ログ (375)・00一覧 00-REB）で足した＝tsv には無い。
    /// 消す前の恒等は帯3本の全行バイト一致で示した（(253)）。復活させたいときは、この名簿から外す前に tsv と docs の裁定を読むこと。</summary>
    private static readonly string[] RetiredFields =
    [
        "WageClampIndScale",          // (605) 線 ind4：賃金の天井の工業化度の項〔R2 点火 1.0・(495) で IndustrializationOff が 0 を掛けていた〕。項ごと削除（天井そのものの WageClampMax は残る）
        "MonopolyDissolveInd",        // (605) 同：独占が解体する工業化度〔0.3〕。解体は独占禁止法の係数 MonopolyRentFactor に任せる
        "RefrigerationIndOnset",      // (605) 同：冷蔵の立ち上がりの工業化度〔1.0〕。冷蔵の門は発見の行
        "RefrigerationIndSpan",       // (605) 同：冷蔵が満額になる工業化度の幅〔1.5〕
        "AiLawGateFromBoard",         // (599) 恒等の線 gc6：工業化度を読む AI の法の門の置き換えの旗〔(423)・既定 3〕の旧の腕〔段 0〜2〕。旗ごと削除（願いは GoldWishFromBoard／AntitrustTargetFromBoard／GuildTargetFromBoard の 1 本）
        "AiBureaucracyFromPrereq",    // (599) 同：AI の官僚制度の走査の旗〔(389) Q-a8・既定 2〕。旗ごと削除（走査は常に走り政治の門を通る）
        "GoldStandardIndThreshold",   // (599) 同：金本位を自力で志向する工業化度の下限〔3.5〕。読み手は旧の金の願い GoldWishOld だけだった
        "ScholarshipLaw",             // (597) 線 sch2：(590) の世界の旗〔全ての段に比例〕。旗ごと削除（奨学金の仕組みを 2 つ残さない）＝代わりに教育費の軸の最上段だけの ScholarshipTopStage
        "AiModernizeFromReturn",      // (591) 恒等の線 gc5：AI の近代化投資の信号の旗〔(416)・既定 3〕の旧の腕〔段 0〜2〕。旗ごと削除（信号は World.ModernizeReturnShare の 1 本）
        "AiIndustrialCeilingFrontier", // (591) 同：旧の包絡 max(2.5, α×世界前線) の α〔鋼市場診断#1・既定 0.6〕。読み手は旧の腕だけだった
        "ModernizationAbolished",     // (583) 恒等の線 gc4：近代化投資の廃止（(573)・作者 2026-10-26）の旗。既定 1 で点火済みのまま旗ごと削除（州の StateBuildQueue へ撒く道を World.ApplyModernizationInvestment から物理削除）
        "StateBuildCost",             // 読み手0（手動工場建設は削除済み）
        "StateBuildQueue",            // (608) 恒等の線 gc7：Region の欄〔国庫の建設の予約額・書き手は (583) から 0 本〕。欄ごと削除（請求の和・支払い率・月末の 0・計器の Item.StateBuild・Runner の表示の読み手も）＝下の名簿の検査は Region の欄も見る
        "CumStateIndustrialization",  // (608) 同：Region の欄〔国家建設分の工業化度の累積・書き手は (599) で 0 本〕。欄ごと削除
        "CohortGateK",                // 世代ゲート＝2026-09-21 作者が一覧の矛盾を「棄却」側で閉じた（00一覧）・削除は担当の推奨（作者の承認は未確認）
        "EnclosureRate",              // 囲い込みの自給侵食＝同上（削除は担当の推奨・作者の承認は未確認）
        "RuralPromotionRate",         // 農村の直行便＝2026-08-16 差し戻し（exact−7・棚卸し dorm-16）・削除は担当の推奨（作者の承認は未確認）
        "OpulenceInfraChannel",       // 支出反射のインフラ拡張＝2026-08-08 同時点火で硬性赤・08-08 は「単独再測定可能な形で温存」＝削除は棚卸し dorm-51（AI の推奨）に従った担当の推奨（作者の承認は未確認）
        "MilitaryBuildCostCapScale",  // 軍需の身の丈ゲート倍率＝armscap 単独で基線とビット同一（実装ログ 2026-09-07）
        "NobleInvests",               // 貴族も投資家に含める述語の開閉＝2026-08-14 差し戻し（rentier 診断が二重に不成立・実効果は資金量＝清抑制の較正地雷）・棚卸し dorm-51・(255) で CapitalMarket.IsInvestor の引数 c ごと削除
        // ★(271) 休眠8本の削除（作者 2026-10-11「全て推奨で」（00-A）で採った推奨・measurements/dormant_20261011.tsv の「消す（済み）」の行を手で写した名簿）。消す前の恒等は帯3本の全行バイト一致で示した（(271)）。
        "DesperationMilitancyOverride", // 闘争度による層の上書き（窮乏チャネルの意識ゲートを開ける）＝意識を識字で頭打ちにしない（設計思想⑧）
        "DesperationLayerBlend",      // 3層の振る舞い分け＝同上
        "WarStartPoliticsScale",      // 開戦そのものの政治的帰結＝戦争の実体の被害で表す（2026-07-25 の教訓）
        "UnionSatBonus",              // 組合の満足度への加算（思想⑨＝満足度への上乗せは賃金の経路と二重）
        "UnionWageRiseBoost",         // 組合厚生 LaborWellbeing を通る賃金交渉の増幅＝UnionSatBonus と一緒に（LaborWellbeing が常に 0 だった）
        "ArmyShapeGauge",             // 艦隊ゲージの兵数ゲージへの合算＝理想の軍隊の撤去と一緒に
        "PettyInterestGate",          // プチブル利益束（第4束「小営業」）の主フラグ＝段階化は作る予定が無い
        "IncumbencyBias",             // 現職効果＝振り返りの投票は作る予定が無い（履歴から戻せる）
        "ScarceCapFollowsEndowment",  // 希少鉱物の産地の天井を賦存比例に＝鉱脈の在庫が物理の限界をすでに担う（二重）
        "CorruptionCoreV2",           // 汚職V2の点火の記録＝読み手0（2026-08-16 C2-C5 で旧式の分岐を撤去した後は係数だけが残っていた）
        // ★(275) 線P (273) が置き換えて削除した旧 (220) の開幕の貨幣の器（λ 規則 OpeningSavingsRebaseMode・NationConfig.BroadMoneyToIncome へ）。NationConfig.BroadMoneyToGdp は係数でないのでここには載らない。
        "InitMoneyMode",              // 投資家の貯蓄を比×年名目GDP へ置き直すモード＝OpeningSavingsRebaseMode に置き換えた
        "InitInvestorMoneyShare",     // 同じ器の投資家の取り分
        "InitMoneyScale",             // 同じ器の倍率
        // ★(375) (273) の開幕の貯蓄の置き直し（モード 0 が既定の休眠）を削除した（作者 2026-10-16 の答え「全て推奨」）。NationConfig.BroadMoneyToIncome と world.json の broadMoneyToIncome・World.RebaseOpeningMoney・Pop.RebaseSavingsTo・計器 money_s の rebase の行も一緒に消えた。
        "OpeningSavingsRebaseMode",   // 開幕の貯蓄の置き直しのモード（0 置かない・1 助走の終わり・2 最初の四半期の後）
        "SavingsYearsByClass",        // 置き直しの配分の形（階級ごとの貯蓄の年数）＝読み手は置き直しだけだった
        // ★(恒等の削除 2026-10-18・精査 doc §4 ①-a／①-b／Q2)：既定0で1命令も走らなかった器（読み手が無い・回らない枝の中だけ）を物理削除した。消す前の恒等は selfplay 1 200 の全体の一致で示した（成績表3枚の全行一致はメインの合流の確かめ）。
        "TransportDemandAffordCap", "TransportPotentialWeight", "TransportPotentialHeadroom",   // 輸送の目標の付け足し 3 本（目標を追う形の中の付け足し）
        "FertilityEquilibriumWeight", "FertilityLandDivisionSlope", "FertilityIncomeSurvivalGate",   // 出生率の休眠の 3 項
        "NewbornLiteracyDilution", "EducationGateUsesEma", "EducationEmaRate", "MeritModernityFromClosed",   // 新生児の識字の希釈・高等教育ゲートの多年 EMA・メリトクラシーの政体バフの代わり
        "RevoltExpansionScore", "DomesticMarkupMax", "CompetitiveFirmsBase", "CorruptionPoorOfficialWeight",   // 反乱の膨張・国内寡占マークアップ・暮らせない官僚の汚職
        "AbsorptionIncomeWeight", "AbsorptionIncomeRef", "ResearchScaleWeight", "ResearchBaseRefPop",   // Q2＝技術水準の伸びに掛ける倍率（作者 2026-10-18「全て推奨」）
        "AdoptExpertiseWeight", "InnovateExpertiseWeight", "CatchupContactWeight",
        "ColonialRegionSettlement", "ColonySettlerRatePerMonth", "ColonyPromotionCoreShare", "ColonyDevCostFromRegion", "ColonyDevCostWeight",   // 植民地の入植と昇格・開発度の項（ColonyPromotionMinPop は分離の圧力が読む＝残す）
        "ColonialPopGrowth", "ColonialPayShare", "ColonialExploitMortality", "ColonialLiteracyGrowth", "ColonialMilitancyGrowth", "ColonialMilitancyDecay",   // 植民地の旧い型（Colony オブジェクト）の経路一式
        "ColonialUnrestSupplyHit", "ColonialHoldScale", "ColonialIndependenceMilitancy", "ColonialHoldDistanceDecay", "ColonialHoldSharedArmy", "IndependenceShockYears",
        "ColonialOutputHalf", "ColonialShipPerInfra", "ColonialRailPerInfra", "ColonialInfraDepreciation", "ColonialInfraPerColony", "ColonialInfraConverge",
        "ColonialReturnScale", "ColonialDevStep", "ColonialInvestFrac", "ColonialProductionPerColony",
        "RgoCapitalShare", "StateShareCounterOnly",   // 投資家が畑と鉱山の面積を広げる枝（StateShareCounterOnly=1 の点火 2026-08-30 以来一度も走らない）と、それを止めていた旗
        "TechGatedScaleZero",   // Q1＝幻の工場の旗の 0 の枝（作者 2026-10-18「全て推奨」）
        "IndustrializedTechThreshold", "IndustrializedUrbanThreshold", "CalibQuarters",   // 単国の経路の較正（Calibration・Sweep・ClassifyPath）とその物差し
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 経16（既定1.0の旗の 0 の枝を削除・式は1の側だけ）
        "PhantomOutputSnapFloor", "ConstructionMaterialCarryover", "SubsistenceHarvestFullExposure", "RgoElasticityGate",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 財3（既定1.0の旗の 0 の枝を削除）。SilverStandardGate は最後の束で消す
        "IndirectTaxInBudgetBase", "CurrencyReformEnabled", "CapitalMobilityLawGate", "EuphoriaUsesStableReserve", "CoercionSoftCap", "TrilemmaRateGate", "SilverLightDiscipline", "CapitalOpennessTariffWeight",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 政8（既定1.0の旗15本の 0 の枝を削除）と、0 の枝だけで読んでいた NationBuildCivicFloor・NationConsciousnessCoreWeight
        "CivicOpennessEnabled", "DynamicParties", "EarlyElectionEnabled", "ElectionEventEnabled", "PolityLabelFromAxes", "RepublicanDeposition", "CrownFormMutation", "SeatsFrozenBetweenElections", "ParliamentSeatOnOpen", "RoyalistGate", "CrownlessCollapseKeepsRepublic", "SuffrageScopeGate", "SecretBallotGate", "StateReligionGate", "SocialSecurityLawGate", "NationBuildCivicFloor", "NationConsciousnessCoreWeight",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 軍14（既定1.0の旗の 0 の枝を削除。ColoniesAreRegions だけは下の最後の名簿）と、0 の枝だけで読んでいた WarExhaustionMilitancy・WarMilitancyRatePerMonth・LandReachKm
        "ArmyShapeEnabled", "ColonialRegionCession", "ConscriptionScalesWithWar", "EnableFormations", "CultureFormationGate", "CultureFormationNeedsGain", "FormationCapitalMove", "ImposeRegimeEnabled", "PeaceInterventionEnabled", "PeaceInterventionProbe", "InterventionRefusalSymmetric", "InterventionRefusalWar", "RankBracketsMerged", "SubmitCountsAllies", "ThreatUsesColonyValue", "TradeTreatyEnabled", "TradeTreatyAbolished", "VassalDefectionUsesSubmitMeasure", "VassalTransferOnCrush", "WarMilitancyViaTarget", "WarExhaustionMilitancy", "WarMilitancyRatePerMonth", "WarNeedsReach", "WarWearinessDecays", "FrozenTensionDecays", "AntiImperialCoversRegions", "ContinentalLandReach", "LandReachKm", "ColonyCaptureReachGate", "RationingGate", "UnificationWarEnabled", "WarLuckNeedsContact",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 人3（出生・台帳・窃盗）と交4（DiscoveryTierGate）
        "FertilityAffectsBirthsOnly", "FertileWomenBirths", "CoupleLimitedBirths", "TradeLedgerFix", "CrimeVictimBasis", "DiscoveryTierGate",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 交5（LandRouteEnabled を除く）。LandRouteEnabled は TransitTests・TradeClearingTests が陸路を切る環境固定に使うので残した
        "TradeMonthlyClearing", "WinterPortFreeze", "TradeWeightDynamic", "TradeAccessNormalized", "LandEdgesAvoidWater", "BarrierWallSegments", "TransportShareByPotential", "TransportEnabled", "TransitEnabled", "PortRewireEnabled",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c A1（AI・世界の既定1.0の旗10本）
        "AiLawGateUnified", "AiLawWriteCoverage", "AiSupplyChainEyes", "AiWorldPriceEntry", "AiProtectiveTariff", "AiBuildGuardRelax", "InvadeShipSeaDistance", "ExternalPressureDerived", "WorldPriceInitFromRefPrice", "CivilWarPredicateUnified",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 財3の最後（SilverStandardGate＝銀本位の主旗）
        "SilverStandardGate",
        // ★旗の焼き込み 2026-10-18・精査 doc §4 ①-c 軍14の最後（ColoniesAreRegions＝植民地の州昇格。読み手が多いので最後・環境固定のテストは固定を外して緑)
        "ColoniesAreRegions",
        // ★(551) 恒等の線 gc1（ゴミ変数の 1 回目）：本番の盤面で動かない locked 州の解錠の道（Territory.Step の §1）の係数 5 本と、既定 0 で差し戻し・棄却・採否未決だった休眠 5 本（作者 2026-10-25 の答え〈推奨で消す〉）。
        // 期待値の出所は実装ログ (551) の削除の一覧を手で写した名簿。TradeCheapSellSubsidy は古い設計の一覧 H21（波27 貿易の線 R-a）の予定が生きているので残した。
        "FrontierDevelopmentRate", "FrontierUnlockThreshold", "ForceUnlockRate", "ForceUnlockTreasuryCostPerMonth", "ForceUnlockMilitancy",
        "PreemptiveSeizeWeight", "FertileAbsoluteBlend", "MigrationFarePerkKm", "NationalismFromConsciousness", "WorldPriceStatUnclamped",
    ];

    /// <summary>★読み手のない係数の番人（2026-10-11 (253)・休眠の棚卸しで <c>StateBuildCost</c>〔手動工場建設の削除の後に残った死蔵〕と <c>CorruptionCoreV2</c>〔点火の記録だけ〕が見つかった型）：
    /// Coefficients の全ての公開の欄が、コメントと文字列を除いた SimCore・App・Runner のソースのどこかで宣言以外にも1回は現れる。
    /// 期待値の出所は欄の列挙（反射）とソースの走査であって手書きの表ではない。⚠限界：補間文字列の穴の中の読みは見ない（<see cref="GoodIdGuardTests.StripCode"/> と同じ）・
    /// 名前が同じ別の欄の読みと区別しない・書き込みだけの欄は通る・Runner の計器（診断）だけが読む欄も通る・既定の世界で決して通らない枝の中だけで読まれる欄も通る（字句の出現しか見ない＝到達は見ない）。
    /// Tests だけが読む欄は通さない（テストだけが読む係数は本体では死んでいる）。</summary>
    [Fact]
    public void 係数は全て本体のどこかで読まれる()
    {
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        var word = new System.Text.RegularExpressions.Regex(@"[A-Za-z_][A-Za-z0-9_]*");
        foreach (var (f, rel) in GoodIdGuardTests.CsFiles())   // SimCore・Runner・App・Tests の .cs（obj・bin を除く）＝Tests は外す
        {
            if (rel.StartsWith("PriceOfBread.Tests/")) continue;
            foreach (System.Text.RegularExpressions.Match m in word.Matches(GoodIdGuardTests.StripCode(File.ReadAllText(f))))
                counts[m.Value] = counts.GetValueOrDefault(m.Value) + 1;
        }
        var unread = new System.Collections.Generic.List<string>();
        foreach (var fld in typeof(Coefficients).GetFields())
        {
            if (!fld.IsPublic || fld.IsStatic) continue;
            if (counts.GetValueOrDefault(fld.Name) <= 1) unread.Add(fld.Name);   // 1回＝宣言だけ
        }
        Assert.True(unread.Count == 0,
            $"読み手のない係数 {string.Join(", ", unread)}。使わないなら削除する（記録は墓標のコメントと docs へ）。休眠の器なら読む枝を置くこと");
    }

    [Fact]
    public void 削除した休眠の係数は戻っていない()
    {
        foreach (var name in RetiredFields)
            Assert.True(typeof(Coefficients).GetField(name) is null && typeof(Region).GetField(name) is null,
                $"棚卸しと削除の回（(253) 以後・名簿の注記）で削除した係数・州の欄「{name}」が Coefficients か Region に戻っている。復活させるなら measurements/dormant_20261011.tsv の行き先と docs の裁定を読んでから、この名簿から外すこと");
    }

    [Fact]
    public void 既定ゼロの係数は休眠表か台帳のどちらかに載っている()
    {
        var c = new Coefficients();
        var known = new System.Collections.Generic.HashSet<string>(DormantZeroFields);
        foreach (var n in ZeroDefaultLedger) known.Add(n);
        var missing = new System.Collections.Generic.List<string>();
        foreach (var f in typeof(Coefficients).GetFields())
        {
            if (!f.IsPublic || f.IsStatic || f.IsInitOnly) continue;
            if (!UnitValues.TryToDouble(f.GetValue(c), out double d) || f.FieldType == typeof(int) || d != 0.0) continue;   // ★(246) 専用の値型の係数も見る（int は従来どおり対象外）
            if (!known.Contains(f.Name)) missing.Add(f.Name);
        }
        Assert.True(missing.Count == 0,
            $"既定0の係数 {string.Join(", ", missing)} が休眠表にも台帳にも無い。"
            + "器として入れたなら DormantZeroFields へ（恒等が自動で検査される）。"
            + "0が正しい較正値なら ZeroDefaultLedger へ理由とともに足すこと。");
    }
}
