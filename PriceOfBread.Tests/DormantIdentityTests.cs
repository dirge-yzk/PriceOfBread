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
        // ★PaySalesToProducerBook（(1228) c124bk 記帳の線）は 2026-11-19 に 1 で腕 B の木へ＝この表から外した（旗 0 の経路の番人は PaySalesProducerBookTests の T1・T4 の旗 0 の段）。
        // ★LordReligionStake・LordIntermediaryStake は 2026-11-11 (1048) c99db に 1 で**点火**＝この表から外した（旗 0 の経路の番人は LordstatD1aTests の D1a_2）。
        // ★GoldRushEmergent は 2026-09-04 に 1.0 で**点火**（J-005）＝この表から外した。
        // ★ChilledMeatEnabled は 2026-09-04 に 1.0 で**点火**＝この表から外した。
        // ★AiBureaucracyFromPrereq は 2026-10-17 (389) に 2 で**点火**（欲しい法が官僚制度の前提で止まったとき、法律タブの門も通る法だけ官僚制度の目標を上げる）＝この表から外した。(599) で旗ごと削除（RetiredFields）。
        // ★StateEduDemandInFront（(452) 国庫の教育の買いたい分を値段が読む需要へ）は 2026-10-20 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★ServiceClosedFixedPoint（(spa) A′ 役務の不動点の閉じた式）は 2026-10-23 に 1 で**点火**（S1c・ServiceClosedMaxCurv=0.01）＝この表から外した。
        // ★LaborClosedForm（(spb) 案 B 労働の閉じた式）は 2026-10-24 に 1 で**点火**＝この表から外した。
        // ★UrbanCapOff（(687) 線 urb2）は 2026-10-30 に 1 で**点火**＝この表から外した。(812) で旗ごと焼き込んだ（RetiredFields）。
        // ★DependencyCycleBreak（(1064) c101vs 宗主と属国の輪を切る）は 2026-11-12 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は DependencyCycleTests（旗を明示）。
        "CessionUnified",              // ★(723) 線 ces2：講和の割譲の 4 経路を 1 本の判定（陸の隣接か港の州で上陸の門）と講和 1 回に 1 本の予算へ（設計 20261031_割譲の候補と費用_設計.md 段2）＝0 で今の 4 経路・1 ビットも変わらない。旗 1 の腕は波51 以後（番人 CessionUnifiedTests）
        // ★RailByRegion（(1097) c105rr 支配の力の段 R・州の線路）は 2026-11-13 に 1 で**点火**＝この表から外した（旗 0 の経路の番人は RailByRegionTests の旗 0 の 2 本）。
        // ★RailNeedByOutput（(1137) c111rb 段 R の need の基底・鉄道の need の取り分を州の産出の比へ）は 2026-11-14 に 1 で**点火**＝この表から外した（旗 0 の経路の番人は RailByRegionTests の G3 と RailNeedByOutputTests の旗 0 の側）。
        // ★CedeRuleToLocal（(902) c77ce 割譲の支配度 C1＋H-a）は (915) c79ce が 2026-11-07 に 1 で**点火**＝この表から外した。
        // ★SubsistenceStage2（(849) c69ss 自給の段 2）は (870) c72ss が 2026-11-06 に 1 で**点火**＝この表から外した。
        "SubsistenceNoBoughtTech",     // ★(877) c73ts 自給の段 2′ Σ2′：技術の比 T を自給の 3 か所（焼く ρ・σ・(b′) の lneed）から外す旗（0＝S1-a のまま・番人 SubsistenceNoBoughtTechTests）。点火時にこの表から外す
        "UrbanFlowReal",               // ★(706) 線 urb3：州の中の移動を〈移る値打ち〉の対称の相対差に比例させる案B（設計 20261031_都市化の目標の式_設計.md 段2 の器）＝0 で今のシグモイドの式・1 ビットも変わらない。旗 1 の腕は波49（番人 UrbanFlowRealTests）
        // ★RuleOrderByRegion（(850) c69po・(862) c71po 州ごとの治安 P1-1）は線の枝で 1 に**点火**＝この表から外した。旗 0 の経路の番人は RuleOrderP1Tests の P1_0・P1_2・P1_5 と RuleSharesFormTests の D2 の 2 本（旗 0 を明示）。
        // ★CollapsePeaceOneShare（(791) c10cp 崩壊の講和の取り分を 1 回分に）は 1 で**点火**＝この表から外した。旗 0 の経路の番人は CollapsePeaceTests（旗を明示）。
        "ForcedPeaceBlankOverCollapse",   // (821) c65md 列強の調停の戦争は崩壊の上書きを通らず白紙で終える旗（0＝今の形・1 ビットも変わらない・番人 ForcedPeaceBlankTests）。点火時にこの表から外す
        "SilverParityDemandWeighted",   // (807) c12sv 銀の平価の注入の対数平均を州の金鉱の需要で重みづける旗（0＝州ごとに等しい 1 票＝今の形・番人 SilverParityDemandWeightedTests）
        // ★CapitalClaimCancelFromLedger（(828) c66dw 消えた請求権の額を外資の負債の台帳の差で書く）は線の枝で 1 に**点火**＝この表から外した。旗 0 の経路の番人は CapitalCouponWriteDownTests の Theory（旗を明示）。
        "CapitalDividendFromHostRent",   // (801) c11fl 外資の利札を受け皿の投資家の工業レントの持ち分に結ぶ旗（0＝今の形・1 ビットも変わらない・番人 CapitalDividendHostRentTests）。点火時にこの表から外す
        // ★CapitalCouponUnpaidWriteDown（(815) c14fl 払えない利札で請求権を落とす）は 1 で**点火**（腕 B の木）＝この表から外した。旗 0 の経路の番人は CapitalCouponWriteDownTests（旗を明示）。
        // ★OpiumAddictionCapOff（(771) c6op アヘンの蓋の段1・既定 0）は (830) で旗ごと削除（RetiredFields）。d は常に OpiumAddictionCap。
        // ★OpiumAddictionLogistic（(820) c65op 形 H）は線の枝で 1 に**点火**＝この表から外した。旗 0 の経路の番人は HabitTests の Theory（旗を明示）・HabitLogisticTests。
        // ★PortSameLandmassFirst（(779) c8pd 港の形 (d)）は線の枝で 1 に**点火**＝この表から外した。(785) c9bk で旗ごと焼き込み（欄は削除）＝番人は PortByLandTests の形d_ 3 本。
        "CapitalHeadroomFromReturn",   // (605) 線 ind4：工業化度の読み手を盤面の量へ置き換える旗の束（既定 0＝今のまま・番人 IndustrializationReaderTests）
        // ★TechMultipliersOff（(613) 線 tl 段3）は 2026-10-28 に 1 で**点火**＝この表から外した。TechGateByDiscovery・TechSignalOff・TechThresholdOff・EraOff（段1・2a・2b・4）も同日に 1 で点火。TechProductivityBracketOff（段5・腕 B5）は既定 0 のまま〔cp3 と同じ線か後〕
        // ★InheritanceLawGate（(650) 相続法）は 2026-10-30 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は InheritanceLawTests。
        // ★HcdColonyCessionDistance（(696) 線 hcd 講和の植民地格の州の複数割譲に距離）は 2026-10-31 に 1 で**点火**＝この表から外した。(817) で旗ごと削除（RetiredFields）。番人は ColonyCessionDistanceTests。
        "TechProductivityBracketOff",
        // ★NavalCedeFleetReturn（(688) 線 navf5 割譲で艦が移る経路）は 2026-10-30 に 1 で**点火**＝この表から外した。(817) で旗ごと削除（RetiredFields）。番人は NavalCedeFleetReturnTests。
        // ★StanceEduHoldsLaw（(680) 線 edq）は 2026-10-30 に 1 で**点火**＝この表から外した。(772) c6sp で焼き込み（欄は墓標・旗 0 の経路は無い）＝番人 StanceEduHoldsLawTests は既定の形だけを確かめる。
        // ★SpreadLiteracyFull・SpreadPrereqEdges（(621) 線 inst）は 2026-10-28 に点火＝この表から外した（SpreadLiteracyFull は既定 0.5・辺は Catalog.After の共産主義 ← 社会主義へ移して旗を消した）。旗 0 の経路の番人は InstitutionAnchorTests。
        // ★MandateWallStockCap（(615) 政治資本の壁の一歩の勘定）は 2026-10-28 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。(803) で旗ごと削除（RetiredFields）。番人は MandateWallStockCapTests。
        "CriminalClassEnabled",         // ★(366) 治安と所有の安全 段0 の器＝犯罪者 POP を並べる主フラグ（0＝Pops.Count も CriminalPopIndex=-1 も不変）。点火は段1（腕 A）
        // ★NavalPortProcurement（(606)(669)(679) 線 navf4 海軍の需要の道）は 2026-10-30 に 1 で**点火**＝この表から外した。(824) で旗ごと削除（RetiredFields）。番人は NavalPortProcurementTests。
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
        // ★CivilWarArmsDemandUnified（(903) c77cw 内戦の軍需の倍率の枝を消す）は 2026-11-07 (922) c80cw で 1 に**点火**＝この表から外した・2026-11-08 (973) c87mw ⑰ で旧の 2.0 の枝ごと旗を消した（番人は MilitaryWarMultContactTests・CivilWarArmsDemandTests の計器の試験）。
        // ★MilitaryWarMultFromContact（(959) c85mw 戦時の軍需の倍率を接触の割合から導く）は 2026-11-08 に枝 c85mw で 1 に**点火**＝この表から外した（番人 MilitaryWarMultContactTests）。
        // ★FamineDriveGain は 2026-09-22 に 10 で**点火**、NeedTierSplit は同日に旗ごと削除（階層3分割を既定化＝作者裁定2026-09-20）＝この表から外した。
        // ★ParticipationDiscontent は 2026-10-19 (431) に 1 で**点火**、DiscontentSplit は焼き込んで旗ごと削除（二量分離の完成＝闘争度の目標から生活の項を外し、参加の述語と思想の更新の入力は max(不満度, 闘争度)）。この表から外した。
        "AssimilationRevival",          // ★段4（2026-09-09・§7-7バグa後半）＝民族復興＝抵抗の床を外して符号を反転させる（勾配の差し替え）。
                                        //   0＝床あり＝厳密恒等。点火値 1.0526＝1/0.95（反転点＝ボヘミア1900の識字0.95）
        "SerfGateCoversPool",           // C-03：農奴の移動制限を失業プール経路へも広げる
        // ★DomesticFlightHasHost は 2026-09-22 に**点火**（旗ごと削除＝逃避の受け皿が常に外資の負債を記帳）＝この表から外した。
        // ★ScarceCountsIncludeColonies（希少鉱物の総量保存の k/N に植民地を数える）は 2026-10-04 (197) で**点火**（反証役 vq2#1）＝この表から外した。
        "ConsumeBudgetSavingsRatePerMonth",   // ★(273) 線P 段5＝消費の予算を min(予算, 前月の所得＋貯蓄×d) で切る（EconomyStep.Run ③の入口）。0＝分岐に入らず評価しない＝厳密恒等（d＝0 を式に入れると min(予算, 前月の所得) になり恒等でないのでコードは d＞0 のときだけ評価する）。腕は POB_COEF で置く。
        // ★LuxuryIncomeElasticity・DomesticServiceIncomeFactorExponent（(302)(329) 奢侈品と家事奉仕の所得弾力）は 2026-10-14 (331) の合流で**点火**（0.5・1）＝この表から外した。
        "PrivilegeLedgerEnabled",       // 特権の台帳（段1・観測のみ）＝0で1命令も走らない
        "PrivilegeStripMilitancy",      // 特権システム 段3＝剥離の怒り（PrivilegeLedgerEnabled>0 が前提）＝0で加算なし
        "PrivilegeMonopolyInLedger",    // 特権システム 段5前半＝特許会社の取り分を台帳へ（記帳のみ）
        "GuildLawGate",                 // 特権システム 段4＝同業組合法の主ゲート（法が動かない）
        // ★LaborFloorOffGate・LaborBandFloorOffGate・MinimumWageLawGate（(469) 労働の下限の撤去と最低賃金法）は 2026-10-20 (475) の合流で 2・1・1 へ**点火**＝この表から外した。LaborBandFloorOffGate は (1175) で旗ごと焼いた（RetiredFields）。
        "GuildRentPremium",             // 特権システム 段4＝職人の上乗せ（0＝なし）
        "MintParitySells",             // ★器（2026-09-07）＝床の standing offer を両側に（平価の上で準備を平価売り＝平価買いの逆操作）＝0で従来（片側）
        "MintBuysAtCapital",            // ★器（(1180) c117b0 B-1′ 段 B′-0）＝造幣の買いを首都の州の市場だけに（Simulation.MintGold）。0＝全ての州＝厳密恒等
        "ProjectionUsesLand",           //支配度doc §14b＝陸路の起点（ProjectionRuleMin=1.1 が主ゲート＝既定で集合={首都}）
        // ★BuildingGoodBundle（(550)(555) 工場の建屋の財・建設の束の 2 枠）は 2026-10-25 (555) 線 fb2 で既定 1 へ**点火**＝この表から外した。
        // ★MusketRecipeEnabled（前期の軍需レシピ）は 2026-09-27 回2-1 で**点火**（旗ごと削除）＝この表から外した。
        "SilverRushEnabled",            // 銀の後期産地＝シルバーラッシュ（0＝解禁も富鉱化もしない）
        "MineralCostFollowsGrade",      // ★器（2026-09-07）＝鉱脈の倍率を鉱物RGOの限界費用にも通す（EconomyStep.MineralSelfCost）。0＝鉱脈で割らない＝厳密恒等
        "EndowmentCostFollowsAbundance",   // ★器（2026-09-08）＝賦存倍率>1 の州の自己費用を賦存で割る（Simulation.ApplyEndowment）。0＝MarginalCost そのまま＝厳密恒等
        // ★SoldierVacancyFromZero（(1166) c115sv 大きさ 0 の兵の席）は 2026-11-16 に 1 で**点火**＝この表から外した（旗 0 の経路の番人は SoldierVacancyFromZeroTests の F0）。
        // ★TradeBuyBySupplyCurve（(1144) T1 A-1）は 2026-11-16 (1172) c116t1 で 1 に**点火**＝この表から外した（旗 0 の経路の番人は TradeT1Tests の M3〔旗 0〕・M5）。
        // ★HealthcareLiteracyGateOff（(1222) c123k3 K03 医療の min から識字の項を外す）は 2026-11-19 に 1 で腕 B の木へ＝この表から外した（(1227) で旗ごと焼いた＝RetiredFields・番人は HealthcareLiteracyGateTests）。
        "EndowmentCostFollowsScarcity",    // ★器（2026-09-08）＝賦存倍率<1（非産地0.4）の州の自己費用を上げる（同上）。0＝上げない＝厳密恒等
        // ★RecipeCostFollowsProductivity（(328) 流れG ①＝動力の別レシピの自己費用を生産性の比で割る）は 2026-10-14 (331) の合流で**点火**＝この表から外した。
        // ★FacilityBuildOutlayIncludesMaterial（裁定②の安全弁）は 2026-09-27 線2 で**点火**（旗ごと削除）＝この表から外した。
        // ★PriceLawSupplyIncludesStock と TradeBuyerSolvesAtUnitPrice は 2026-09-08 に 1.0 で**対で点火**（腕 price2b）＝この表から外した。
        // ★TreatyPortHomeOnly（(1138) c111tp 条約港の読み手から植民地格の港を外す）は 2026-11-14 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は RuleSharesFormTests の D3 の 2 本（旗 0 を明示）・旗 1 は TreatyPortHomeOnlyTests。
        // ★B9（州一揆の保持力の州按分）・項目2(b)（F1鎮圧枝の緩和なし）・項目3（F1の州選択＝熱×群衆）は 2026-09-11 に1腕で**点火**（係数ごと削除）＝この表から外した。
        // ★ColonyPlantationRent（実バグ B5＝植民地格の昇格州に大農園の地代）は 2026-10-02 (173) で**点火**（R1＋c6・旗ごと削除）＝この表から外した。
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
        // ★SilverParityFromRatio（世界銀価格を同一市場内の 銀/金鉱 比から作る・E→平均→E の自己参照を断つ）は 2026-11-02 (775) に 1 で**点火**（腕 B）＝この表から外した。(1168) で旗ごと焼き込んだ（RetiredFields）。
        "DiscontentLawGap",             // ★二量分離 段1（2026-09-09）＝不満度（導出量）の「法の隔たり」の物差しを PolicyPreferences.LawGap へ（0＝旧＝4軸座標の距離）。
                                        //   ⚠段1は Politics.Discontent の**読み手がゼロ**なので、この係数はどちらの値でも世界を動かさない（点火は段2＝読み手を付けるとき）
        // ★NonElectiveSeatsEmpty（(1099) c105gp 非選挙の政体で議席を空にする）は 2026-11-13 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は NonElectiveSeatsTests（旗を明示）。
        // ★ParliamentSeatsWithoutGap（(1118) c108ps 開院の条件を思想の有無へ）は 2026-11-14 に 1 で**点火**＝この表から外した。旗 0 の経路の番人は ParliamentSeatsWithoutGapTests・NonElectiveSeatsTests（旗を明示）。
        // ★B-03（WarFeverUsesBudgetShare）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
        // ★B-06（TransportBaselineIsLand）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
        // ★AiModernizeFromReturn（(416) AI の近代化投資の信号を設備の収益へ）は 2026-10-19 に 3 で**点火**（止める線に当たらず）＝この表から外した。(591) で旗ごと削除（RetiredFields）。
        // ★AiLawGateFromBoard（(423) 工業化度を読む AI の法の門を盤面の実物へ）は 2026-10-19 に 3 で**点火**（止める線に当たらず）＝この表から外した。(599) で旗ごと削除（RetiredFields）。
        // ★ScoreFromWorldShare（国力の物差しを世界の総量に対する割合へ）は 2026-10-20 (449) に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        "DiscoveryClockMotorParent",    // ★(461) 同・電動機の親の産業を蒸気機関の財にする別の旗（DiscoveryClockWorldPerMonth が正のときだけ読む・既定 0＝今の規則）
        "OpeningBakeEnabled",           // ★(490) 開幕の焼き込みの読み込み（<see cref="OpeningBake"/>）＝0 で表（opening_bake.json）を開きもしない。倒さない（焼くのは工業化度の削除の後・(464) の順）。鍵の一致と読み込みの往復は OpeningBakeTests
        // ★DevaluationCapReliefExp（(587) 切り下げの月の上限の緩め）は 2026-10-27 に 1 で**点火**（線 hyp の腕 B・止める線に当たらず）＝この表から外した。0 に戻せば旧の切り（貯蓄×DevaluationCapFracPerMonth）。
        // ★StapleLedgerSum・HarvestShortfallToMarket（(504) 食料の台帳を主食の和へ①③）は 2026-10-23 に 1 で**点火**（腕 C・止める線に当たらず）＝この表から外した。②は表に残る。
        "StapleDemandRebalance",        // ★(504) 主食どうしの乗り換えの配り直し②（挟みを外し欲しい量の和を必要量に合わせる）＝0 で今の挟み。点火は同上
        // ★MoTrendInitNeedsGdp（(972) c87mt (D) 実質 GDP が正になってから M/Y の趨勢の基準を置く）は 2026-11-08 に 1 で**点火**＝この表から外した。
        // ★UnionBargaining（(520) 交渉の取り分）は 2026-10-24 に 1 で**点火**・LandRentResidual（産地の地代の残りの道）は (529) で撤回して旗ごと削除（腕 B1″〔種 21〜30〕・B2・合成の腕 B3・止める線に当たらず）＝この表から外した。
        // ★RentScarcityElasticity（産地の地代の土地稀少弾力）は (529) で既定 0＝休眠にし、(541) で墓標へ消した＝この表から外した（復活を禁じる番人は Hz8IdentityTests）。
        // ★LordOppositionByRentLoss（(547) 農奴解放・土地改革への旧支配層の反対の強さを次の一段で失う所得の割合へ）は 2026-10-25 に 1 で**点火**（腕 B・計器つき 10 種・止める線に当たらず）＝この表から外した。(803) で旗ごと削除（RetiredFields）。番人は Rs2LordOppositionTests。
        // ★LordOppositionReverseGain（(558) 農奴解放・土地改革への旧支配層の減らす向きの強さを 1 段戻したときに得る所得の割合へ）は 2026-10-25 に 1 で**点火**（腕 B・計器つき 10 種・止める線に当たらず）＝この表から外した。(803) で旗ごと削除（RetiredFields）。番人は Rs3LordReverseGainTests。
        // ★LandTaxFixedBasis（(937) c82lt 土地税の算定の基準 A-2・定額の国の凍結した基数）は 2026-11-08 の (958) c85lt・(964) c86lt で 1 に**点火**（1 回目 (945) は採らず）＝この表から外した。
        "ImportPayerLive",              // (863) c71ip 輸入の払い手を人の居る POP に限る旗（0＝今の形・1 ビットも変わらない・番人 ImportPayerLiveTests）。点火時にこの表から外す
        "TradeCheapSellSubsidy",        // ★(471) 〈安く売る〉の輸出奨励金（交-16・0＝旧＝清算は +1 しか読まない）。1 は −1 を +1 の鏡像として読む。腕 B で採否
        // ★EmptyPopEstate（(848) c69eh 人の居ない POP の在庫の相続 R1）は 2026-11-05 に 1 で**点火**＝この表から外した。
        // ★OpeningColonyDominanceFull（(952) c84kp 開幕の一括昇格の植民地の支配度を 1.0 から）は 2026-11-08 に 1 で**点火**＝この表から外した（番人 OpeningColonyDominanceTests）。
        "LandTaxPhiMove",               // (990) c90px 土地税の基数の定額の度合い φ* を動かす器（0＝world.json のまま・番人 LandTaxPhiTests）。点火は A-2b-1d（答え待ち）
        // ★ResourceRowsFix（(1025) c96rd 段0″a 資源のデータ＝world.json に足した史実の産地の 10 行を州の構築で使う旗）は 2026-11-10 に 1 で**点火**＝この表から外した（番人 ResourceRowsTests）
        // ★RailLocoNeedsDiscovery（(1130) c110lo 機関車の需要と払いに鉄道の発見の門）は 2026-11-14 に 1 で**点火**＝この表から外した（旗 0 と 1 の番人は RailLocoDiscoveryTests＝旗を明示）。
        // ★RailLocoGateWorld（(1152) c113lc 機関車の門を世界の発見の和で読む）は 2026-11-15 に 1 で**点火**（腕 B の木）＝この表から外した（旗 0 と 1 の番人は RailLocoGateWorldTests＝旗を明示）。
        // ★ArrearsBeyondOpenedRoom（(1250) c127bd 金本位でない月の不履行の累計を開いた余地で払えなかった分だけにする旗）は 2026-11-20 に 1 で**点火**（腕 B の木）＝この表から外した（旗 0 と 1 の番人は ArrearsOpenedRoomTests）
        "EmptyPopDebtFirst",            // (910) c78ep 案 B5 相続の前に人の居ない POP の債務を返す旗（0＝今の形・番人 EmptyPopDebtFirstTests）。点火時にこの表から外す
        // ★LaborWeightsByPower（(1037) c98lw 労働力の重み）は 2026-11-11 に 1 で**点火**＝この表から外した（番人 LaborWeightTests）。
        // ★UbiquitousMineralExempt（(1020) c95ub 遍在の財〔石灰石・珪砂・粘土〕を非産地の 0.4 の外へ出す旗）は 2026-11-10 に 1 で**点火**＝この表から外した（番人 UbiquitousMineralTests）。
        // ★ColonyPettyEntryEmptyTarget（(1014) c94pe 小営業の入口の空の商人の行き先を飛ばさない旗）は 2026-11-10 に 1 で**点火**＝この表から外した（番人 ColonyPettyEntryTests）。
        // ★TakeFloorPositive（(984) c89dd ⑬ Pop.Take の額の床を〈正なら動かす〉にする旗）は 2026-11-09 に 1 で**点火**＝この表から外した（番人 TakeDustFloorTests）。
        // ★LandShareCrownSlot（(978) c88cs 土地の持ち分 B-2b 国家の枠）は 2026-11-09 に 1 で**点火**＝この表から外した（番人 LandShareCrownTests）。
        // ★RecipeEquipment・IndustrializationOff（(495) レシピごとの設備と工業化度の倍率の削除）は 2026-10-23 に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★MinimumWageAiFromFloor（(480) AI が最低賃金法に置く目標を盤面の実物から選ぶ旗）は 2026-10-21 (486) に 1 で**点火**（腕 B・止める線に当たらず）＝この表から外した。
        // ★LandShareTaxSale（(991) c90sl 土地の持ち分 B-1 段 1〜2 土地税の払えない分の公売の旗）は 2026-11-09 (997) c91sl で 1 に**点火**＝この表から外した（番人 LandSaleTests）。
    ];

    /// <summary>★1.0 が恒等の scale 型の器の休眠表（レビュー2026-09-07）：反射走査（既定0だけを数える）の外にあるので手で書く。点火時にこの表からも外す。</summary>
    private static readonly string[] DormantOneFields =
    [
        "FactoryBuildPriceGateScale",      // 裁定②＝工事の進捗/素材需要の価格ゲート混合（1＝旧恒等・0＝価格を見ずに進む）
        "RailLayTerrain",                 // (1097) c105rr 段 R のアブレーション：敷く量に地形を掛ける（1＝掛ける・RailByRegion＝0 では読まない）
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
        "FoodIncludesAnimalProtein",
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
        "GuildMobilityDrag",   // (1227) c124kb 項 3 K10′：同業組合の移動の遅れ（既定 0・主ゲート GuildLawGate も 0＝休眠）を削除。法の軸 GuildLawGate・GuildRentPremium は残す
        "HealthcareLiteracyGateOff", "HealthcareEducationGate",   // (1227) c124kb 項 1：(1222) K03 の旗（既定 1）と識字の錠の係数を焼き込んだ（旗 0 の枝と係数を消した＝番人は HealthcareLiteracyGateTests）
        "SecessionConsciousnessWeight", "RedefinedMandateFloor", "RedefinedMandateNcMin", "RedefinedMandateMandateMin", "NationBuildConscript", "NationBuildAccumRate", "NationBuildDecayRate", "NationBuildCrisisDefense", "NationBuildCrisisCivilWar", "NationBuildCrisisIndependence", "NationBuildInitialDefault", "RadioNationWeight", "NationConsciousnessReachDecay",   // (1193) c119nd 段 B：信任帝政の床の分岐と国民意識の物理（漸化式・POP の欄・国民形成費・計器の列）を消した。
        "NationBuildMobilizationWeight", "RevolutionConsciousness", "SkillMobilizationRelief", "NationConsciousnessDerived", "NationConsciousnessPerPop",   // (1193) c119nd 段 A：国民意識の重み 0 の読み手（徴兵の門・革命の目覚め）と既定の焼き込み（不動点の枝・スカラーだけの枝）。
        "NiterBedEnabled", "NiterBedProductivity",   // (1175) c116cl：硝石丘の枝と係数 2 本を削除（旗は既定 0 のまま届かなかった＝休眠 0・硝石を産地にする回で戻す）
        "PriceLawSeesExports", "LaborBandFloorOffGate",   // (1175) c116cl：既定 1 の旗 2 本を焼き込んだ（輸出を需要に足す式・労働 3 財の帯の床の撤去＝常に 1 の経路・旗 0 の番人は LaborMarketFormTests で今の盤面へ移した）
        "MalariaQuininePerColony",   // (1167) c115qc：植民地の毎月のキニーネ（(1158) で既定 0・(1164) で係数 0 を採った）。枝と係数を削除
        "MintBuysAtMarket", "SilverParityFromRatio",   // (1168) c115sb D-s4：既定 1.0 の旗 2 本を焼き込んだ（市場買いの枝と銀の平価の器・旗 0 の経路の番人は隔離の盤面へ移し旧の行は削除）
        "EraTensionRange",   // (1160) c114sr D-r2：近接の緊張の距離の時代係数（EraFactor は常に 1.0＝×1.0 を消した・読み手は World.cs の 1 行だけだった）
        "DefeatShockTensionRelief", "DefeatShockProtectMin",   // (1154) c113sw D-b：ハイエナ抑制の枝と係数 2 本の削除（(1145) c112hy が既定 0 で閉じた後・旧の道は HyenaBranchTests の H2 が戻ると赤）
        "MilitaryProcurePriceGateScale",   // (1132) c110sm K1：軍需財の価格ゲート倍率（既定 1 の経路だけ残した・FactoryBuildPriceGateScale は FactoryTierTests が 0 を置く本物の読み手があるので残す）
        "GarrisonProcurement",             // (1132) c110sm K2：駐屯州調達の器（既定 0・旗 0 の道だけ残した・★(1129) ③ 作者「しばらくの間、全て推奨で」で推奨を採った）
        "MineralScaleCapLift", "MineralScaleCap",   // (1132) c110sm K4：鉱山の天井の旗の焼き込み（天井は ScaleMax へ・MineralScaleCap は読み手 0）
        "MintDemandCounted", "SteamTurbineEnabled", "CompostFertilizerEnabled", "SulfurFromPyriteEnabled", "OilNeedsMachinery", "CenterIsFirstProvince", "CoreInitByRegionCulture", "ScarceCountsIncludeColonies", "ScarceMineralGate", "FactoryBuildRefIsReal", "FarmScaleCap",   // (1089) 線 c104fb：旗の焼き込み（既定 1 の旧い枝を消した。各欄の意味は Coefficients.cs の墓標）
        "HeldSinceFromData",          // (956) 線 c85hs：B14＝world.json の heldSince を読む旗〔既定0〕。heldSince のデータと CoreHeldYears〔150〕とあわせて削除（全州 Core・支配度1.0 の経路だけ残した）
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
        // ★旗の焼き込み 2026-11-03 (812) c13ib（I5・港と移民の旗。既定 1 の経路だけ残した）
        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",
        "NavalCedeFleetReturn",   // (817) c14id：I5 の旗の焼き込み（割譲で艦を送り手の首都の州へ戻す・既定 1 の経路だけ残した）
        "InheritedStateIntentDrop",   // (817) c14id：I5 の旗の焼き込み（移る州の生産者の意志を今の棟数まで下げる・既定 1 の経路だけ残した）
        "HcdColonyCessionDistance",   // (817) c14id：I5 の旗の焼き込み（講和の植民地格の州の複数割譲に距離の項・既定 1 の経路だけ残した）
        "NavalPortProcurement",   // (824) c65id：I5 の旗の焼き込み（国の海軍の発注と予算を本国格の港の州へ配る・既定 1 の経路だけ残した）
        "TechSignalOff",   // (824) c65id：技術水準の削除の段2a の旗の焼き込み（姿勢の効用の技術の遅れと外圧の格差を 0 にする・既定 1 の経路だけ残した）
        "TechThresholdOff",   // (824) c65id：技術水準の削除の段2b の旗の焼き込み（Modernize の法の目標と制限選挙の技術の閾値・制度と思想の行の技術の比・既定 1 の経路だけ残した）
        "ExternalPressureGapScale",   // (824) c65id：外圧が満額になる技術格差（読み手は MultiNationSim の gap の 1 行だけだった＝TechSignalOff の焼き込みで読み手が無くなり、読み手のない係数の番人に従って削除）
        "UrbanCapOff", "BaseUrbanCap", "UrbanPerInd", "UrbanCapFollowSpeed", "UrbanIndSmoothed",   // 都市化の天井の旗と目盛り・Region の平滑化の状態（A3）
        // ★(551) 恒等の線 gc1（ゴミ変数の 1 回目）：本番の盤面で動かない locked 州の解錠の道（Territory.Step の §1）の係数 5 本と、既定 0 で差し戻し・棄却・採否未決だった休眠 5 本（作者 2026-10-25 の答え〈推奨で消す〉）。
        // 期待値の出所は実装ログ (551) の削除の一覧を手で写した名簿。TradeCheapSellSubsidy は古い設計の一覧 H21（波27 貿易の線 R-a）の予定が生きているので残した。
        "PeacetimeConscriptGrievance",   // (1199) c120nk：支配度doc §15 の平時の植民地徴募の遺恨（既定 0 の器・(1194) で 1.0 を採らず分岐ごと消した）
        "NationalConsciousnessCorruptionWeight",   // (798) c11dm：国民意識の蓄積の門の左の項（既定 0.2 で常に真・他の読み手なし〔Coefficients の注にあった World.civicCarriesEthnic は存在しない〕）。試験〈係数は全て本体のどこかで読まれる〉が門を消した後に赤で知らせた
        "UpkeepDistanceGeneral",   // (804) c12ds：既定 1.0 の距離の物差し（×1.0 の因子の除去・読み手は Fiscal.AdminDistanceMulOfKm・Fiscal の行政の距離の割増）
        "InlandPortDistanceDecay",   // (804) c12ds：既定 1.0 の距離の物差し（×1.0 の因子の除去・読み手は InternationalMarket.InlandRailAfterDecay と計器 portch_s の railDecay の 2 列）
        "ScoreTechWeight",              // (798) c11dm：旧の国力の式 OldScore の技術の項の重み（読み手は計器 score_s の旧の 3 列だけ＝列ごと消した）
        "OpeningMoneyFromRatioMode",    // (798) c11dm：★(574) 線 svf 開幕の貨幣の量（00一覧 296 行で採らない・World.RescaleOpeningSavings・NationConfig.BroadMoneyToGdpYear・world.json の表ごと削除）
        "SpreadNeedSocialismBoard", "SpreadNeedCommunismBoard",   // (798) c11dm：★(640) 線 idn の盤面の旗（00一覧 255 行で差し戻し）。式は工場労働者の人数×闘争度のまま
        "FrontierDevelopmentRate", "FrontierUnlockThreshold", "ForceUnlockRate", "ForceUnlockTreasuryCostPerMonth", "ForceUnlockMilitancy",
        "PreemptiveSeizeWeight", "FertileAbsoluteBlend", "MigrationFarePerkKm", "NationalismFromConsciousness", "WorldPriceStatUnclamped",
        "LordOppositionByRentLoss", "LordOppositionReverseGain", "MandateWallStockCap", "FactionOpportunism",   // (803) c12lw：法の旗の焼き込み（どれも既定 1 で点火済み＝旧の枝は消した。番人は Rs2LordOppositionTests・Rs3LordReverseGainTests・MandateWallStockCapTests・RevoltIgnitionTests）
        "OpiumAddictionCapOff",   // (830) c66id：アヘンの蓋の旗（(771)・既定 0＝蓋 OpiumAddictionCap）の焼き込み。正の枝〔d=+∞〕は形 H の下で形 H が黙って消える罠だった。計器 habitcap_s・habitbal_s も削除
        "TechMultipliersOff", "TechGateByDiscovery", "EraOff",   // (843) c68ui：技術水準の削除の段6（段1・3・4 の旗の焼き込み・既定 1 の経路だけ残した）
        "ExpeditionTechLevel", "ExpeditionTechFloor", "TechMilLowFloor", "TechMilFullLevel", "ModernizationTechLagCost",   // (843) c68ui：TechMultipliersOff の焼き込みで読み手が無くなった技術の倍率の係数 5 本
        "MinimumWageLawGate", "InheritanceLawGate", "AntitrustGate", "NationalizationGate",   // (843) c68ui：I3 の残り 4 本（法の旗・既定 1 の経路だけ残した）
        "ServiceClosedFixedPoint", "LaborClosedForm", "HouseholdCreditStapleOnly", "StapleElasticityZero", "StateEduDemandInFront", "ArmsStockEnabled", "BaseArmsStockEnabled",   // (843) c68ui：I1 経済の旗（既定の経路だけ残した）
        "DistanceMarginScale",   // (843) c68ui：B12 既定 1.0 の距離の物差し（×1.0 の因子の除去）
        "MilitaryBudgetNeedCapped", "RegionalPriceBaseExcludesColonies", "MachineCastIronRecipe", "ParishEduFunded", "ChilledMeatEnabled", "KeroseneFromOilEnabled", "HouseholdProductionEnabled",   // (811) c13ia：Simulation.cs の旗の焼き込み（どれも既定 1 で点火済み＝旧の枝は消した。番人は MilitaryTests・IvfInitialValuesTests・ReformPathTests・HouseholdProductionTests ほか）
        "MegaprojectEnabled", "MegaprojectStateFrac", "MegaprojectStateCap", "MegaprojectPrestige", "MegaprojectCounterCyclical", "MegaprojectCrisisDrawdown", "MegaprojectForeignShare", "MegaprojectForeignMinRank", "MegaprojectForeignDrawFrac", "MegaprojectForeignYield", "FormationLegitimacyBoost", "MonumentBuildQueue", "MonumentBuiltUnits", "MonumentApplied",   // (1081) c103dm：威信と巨大建造物を消した（係数 11 本・州の欄 3 本）
        "StockpilePanicWeight", "StockpileTensionSpan", "StockpileOtherFrac", "WarScareTension",   // (1174) c116hd：戦時の買いだめ（在庫の目標の倍率）を 0.5→0.0 で閉じた（S02-H004a）。(1185) c118hc で枝 Model.StockpileFactor と欄 WarScareTension・係数 StockpileTensionSpan・StockpileOtherFrac も消した＝(1192) c119kf で名簿へ
        "UnificationAiPursueChance", "UnificationAiCooldownYears", "UnificationAiWarGapYears",   // (1195) c119us：統一戦争の AI の乱数と待ちの係数 3 本を削除（(1188) で 1.0・0 にした道を焼いた＝候補と標的が立った四半期に必ず使う）
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
            if (GoodIdGuardTests.IsTestRel(rel)) continue;
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
