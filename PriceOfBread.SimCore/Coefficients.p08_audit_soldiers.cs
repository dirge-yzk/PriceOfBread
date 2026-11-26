using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === 兵士POP軍制（Part 2）：軍隊規模＝兵士POP数。軍事費が徴募し給与を払う。治安維持力（警察）とは別レバー。 ===
    public int SoldierRecruitClassA = 1;                     // 徴募/復員プールその1＝農民
    public int SoldierRecruitClassB = 2;                     // 徴募/復員プールその2＝都市労働者
    public double SoldiersPerMilitary = 250.0 * Military.WorkingMenStdShare;              // 戦力ゲージ1.0 ＝ 兵士約69単位（×1000人）
    public double SoldierPayBillPrevMonth = 1.0;   // (1384) c147vs 2026-11-26 点火（腕 B の木）：旗（0＝月の頭で請求書を 0 にし同じ月の予算が 0 を読む・1＝前の月の請求書を読み月の終わりに 1 回書く・番人 SoldierPayBillTests）
    public double FiscalBufferSoldierPayWeight = 0.5;  // 兵士給与を潤沢判定（OpulenceLineOf）にのみ算入（緊縮反射FiscalBufferOfは不変＝旧全面算入の緊縮暴発を回避）
    public double ThreatRailTargetWeight = 16.6;    // 脅威比＝SecurityThreat/SecurityThreatOwnMass（前期値読み＝閉路なし）
    public Share MilitaryEnvelopeShare = new(0.7);   // 四分位の幅は旧 7.6倍・新 5.9倍
    public RatePerYear OpulenceSpendRatePerYear = new(0.6);         // 梯子0.3→0.6→1.2/年
    public double PanicBuyWeight = 0.5;   // 点火2026-09-06＝0.0→0.5・〔6-2〕主食の買占め（B3 自己成就期待の残り半分）
    public Share SerfCashShareFloor = new(0.15);       // 0=恒等
    public Share MaxSoldierFraction = new(0.04);               // 平時の上限＝兵役の母数（働き盛りの男＝Military.RecruitMen）のこの割合
    public double MilitarismStrengthBonus = 1.3;            // 同じ兵数で軍が強い（訓練・規律・動員）＝労働を奪わないので経済を弱めず軍事に出る
    public double MilitarismClout = 0.5;                   // 旧支配層(将校貴族)が軍から得る政治力＝MilitarismClout×軍の政治的優位（軍の圧力×(1−文民制度の弁)∈[0,1]）
    public double SoldierPayMult = 1.0;                     // 兵士給与＝民間の働き盛り1人あたり月収（徴募の母数・Military.RecruitWage）×これ（同等待遇を目指す。払えなければ r で単価が下がる＝充足↓＝弱兵）
    public double SoldierVacancyFromZero = 1;               // (1166) 旗：大きさ 0 の兵士の POP を、空きがあれば社会移動の職の行き先にする（所得は Meritocracy.EmptySeatIncome）。0＝旧の形（大きさ 0 の兵の席は候補にならない）
    public RatePerMonth SoldierMigrationRatePerMonth = new(0.08);             // 平時の徴募/離職速度（月次）
    public double SoldierToVacantSeat = 1;                  // (1271) 旗：同じ国の中で兵が席を超える州の〈超過×SoldierMigrationRatePerMonth〉を空席の州へ空席の比で移す（Military.MoveToVacantSeats）。0＝旧の形（席を超える兵を他の州へ移さない）
    public Share ConscriptionFraction = new(0.12);            // 戦時の徴兵上限＝働き盛りの男のこの割合まで兵士化（人口の約3.3%＝ナポレオン戦争の頂点 4〜6% の桁・行き過ぎは経済に打撃）
    public RatePerMonth ConscriptionRatePerMonth = new(0.15);               // 戦時の動員速度（平時より速い）

    // --- 軍事／武力解錠（§6.1／§A-8・段4e）---

    public double ColonyFuelOilEnergyRatio = 1.5;    // 原油のエネルギー密度＝同熱量の必要量が石炭の1/これ
    public double ColonyShipOccupyPerDist = 0.12;    // 植民地の質量1個数単位・距離1万kmあたり海運に課す混雑（ShipCapacity/(1+Σ)＝補給航路が交易船を占有・スケールフリー・2026-09-28 R2＝行の数でなく質量）
    public int OpiumGoodId = Catalog.OpiumId;                     // アヘンの good.Id
    public double OpiumAddictionGrowth = 0.1;        // 0=機構全体オフ（恒等）
    public double OpiumAddictionCap = 0.2;           // 有限の正の値で使う（+∞ を置くと形 H の (1−在庫/d) が 1 になり、蓋のない線形の式とビットで一致する）
    public double CollapsePeaceOneShare = 1;         // 新しい定数・倍率・状態は無い
    public double OpiumAddictionLogistic = 1;        // 確証なし（AI の推奨・作者「全て推奨で」で推奨を採った）
    public double OpiumAddictionDecay = 0.02;        // 中毒度の自然減（成長>減衰なら発散）

    // === リアリティ監査 2026-07-02 Wave1（致命バグ・保存則・ガード）===
    public RatePerQuarter CapitalDomesticFlightRatePerQuarter = new(0.05);      // 不信1.0あたり四半期に逃げる投資家貯蓄の割合
    public double CapitalFlightDistrustThreshold = 0.02; // 逃避が始まる不信（累積切り下げ／名目GDP）＝切り下げ常習国だけが対象
    public double FisherDeflationFloor = -0.02;   // 財4：Fisher期待インフレ項の下限
    public Share FertileShareFloor = new(0.05);   // 人6：出産可能女性プールの相対基準の下限フロア

    // === リアリティ監査 2026-07-02 Wave2（経済・金融の機構）===
    public double DemandElasticityMaxDaily = 2.5;   // 1.4=PriceFactorMaxと同値で現行と厳密一致・>1.4で価格暴落による大量消費化(産業革命の綿布型)を許す
    public double DomesticServiceProductivity = 0.30;   // EconomyStep の能力 Size×fraction×Productivity×boost の boost（役務は 1）を家事奉仕だけこの値にする
    public double DemandElasticityMaxLuxury = 2.0;   // 1.4=PriceFactorMaxと同値で現行と厳密一致・>1.4で安価工業製品の大量消費化を許す
    public double LuxuryIncomeElasticity = 0.5;   // 0＝旧の挙動（分岐に入らず配りも走らない）
    public double DomesticServiceIncomeFactorExponent = 2;   // 値は変えていない（恒等）
    public double DomesticServiceMarketRation = 1;   // 根＝役務の優先配分は BaseNeed>0 の役務を全て巻き込む（EconomyStep の `isService[gi] && BaseNeed>0`）
    public double MaintLocoRate = 0.5;   // 経18（係数化・中立）：インフラ保守1単位あたりの機関車(車両更新)消費率
    public double MaintAdvLaborRate = 0.4;   // 経18（係数化・中立）：インフラ保守1単位あたりの高度労働力(機関士・保線技師)消費率
    public double MaintLaborRate = 0.3;   // 経18（係数化・中立）：インフラ保守1単位あたりの労働力(火夫・荷役)消費率
    public double GridReplaceRate = 0.002;   // 経18（係数化・中立）：送電網の置換需要率（＝月の劣化率。EconomyStep の劣化は 1−これ）
    public Share CurrencyHoldingRatio = new(0.1);   // 経6（流通現金）：家計・企業の手元現金退蔵比率
    public double HoardPanicWeight = 1.0;       // 信用崩壊時の退蔵増幅（Confidence=0で退蔵率が2倍＝準備がさらに1割減る）
    public double WageClampMin = 0.3;   // 経12：賃金/役務の床＝WageClampMin×(参照価格×物価水準)
    public double WageClampMax = 3.0;   // 経12：賃金/役務の天井＝WageClampMax×(参照価格×物価水準)
    public double WarFarmOutputDamage = 0.3;      // 戦争→飢饉→政変の連鎖
    public double WarLivestockRequisition = 0.03; // 回復は既存のRGO供給弾力（年単位）から自然に出る＝回復コードなし（鉱脈在庫と同じ思想）
    public double WarLivestockOutputDamage = 0.5; // 05-A：畜産(19)は重め＝役畜・軍馬の徴発が市場価格経由で農業を締める
    public double InvestExtrapolationWeight = 0.3; // 05-B2：投資の外挿期待の重み（採算の勢い項）
    public double FreightRatePer10Mm = 0.10;     // 運賃率＝荷価の10%/10,000km（ロンドン→ボンベイ18,000kmで約18%）
    public double RiskLambda = 2.0;              // 03手順7：リスク線積分の強度＝1−exp(−λΣ敵支配度×km/1e4)
    public double LandRouteEnabled = 1.0;   // 0=旧・国一律（首都→重心）
    public double[] TerrainHorseCost = [1.0, 0.8, 0.9, 1.5, 3.0, 2.5, 8.0];   // 添字＝Terrain enum の順
    public double[] TerrainRailCost  = [1.0, 1.0, 1.0, 1.5, 1.2, 5.0, 3.0];
    public double TerrainWaterMajorCost = 0.15;   // 読み手は TerrainWaterMajorCost>0 のときだけ（BilateralMarket・Discovery）（水運は陸運の1/5〜1/10＝docのレンジ）
    public double TerrainWaterMinorCost = 0.4;
    public double RailCostFraction = 0.25;   // 旧＝帆船の時代と鉄道の時代が同じ12倍だった
    public double LandSeaCostRatio = 12.0;       // 陸の実効距離倍率（§4-2の実測例＝陸単価12。鉄道後の低減は#17で。B3裁定＝新係数はこの1本に集約・地形係数は作らない）
    public Share TransitTollShare = new(0.05);       // Stage C：通行料率＝トランジット利用州の取扱高（LastPortThroughput）比
    public double TransportWeightMarginScale = 0.3;   // 財9（輸送費の従量化）：交易マージンの従量成分の強度

    // === リアリティ監査 2026-07-02 Wave3（人口・社会・労働の機構）===
    public double CapacityFromFoodOutput = 0.5;   // 扶養力の食料産出アンカーの重み（実食料産出アンカーと初期人口の基準の混ぜ）
    public double OverworkMortalityWeight = 0.015;   // 短時間側 OverworkHealthSlope=0.015 と対称の初期値＝無制限14hで扶養力−0.03（=+19%産出 whOut の対価）
    public double EpidemicIntraCountrySpread = 0.5;   // 人14（疫病の州間伝播）：TriggerEpidemicの感染窓中に最過密州から次過密州へ与える二次スパイクの減衰致死率倍率
    public double PromotionLiteracySelection = 0.5;   // 人1：昇格の識字選抜強度（Meritocracy）
    public double CrossBorderOppGapThreshold = 0.5;    // 州機会gapの発火閾値（機会スケール1〜3に合わせた初期値＝要batch測定。旧EmigrationGapThreshold=満足度スケール0.15は不適合）
    public double MigrationDistanceScale = 10000.0;   // >0で距離を読む＝州の座標の間の距離を読む
    public RatePerQuarter ImmigrationReceiveCapPerQuarter = new(0.012);   // 2026-07-23 B-高6＝台帳一致（コメントの0.012へ・旧リテラル0.004は台帳の食い違い） // 受入国側の物理上限（/四半期・中間案(b)2026-07-10）＝アイルランド脱出級の絶対フロー年4.8%
    public double UniversalConscriptionFactor = 2.5;   // 値の根拠＝米1924年移民法は前年比で流入を約8割削った（年約70万→約16万）＝Closed≈0.2
    public double MonetaryPrefScale = 20.0;
    public double ImmigrationRestrictionPrefScale = 40.0;
    public double SpecialistExitBanPrefScale = 0.0;   // 技術者流出禁止法の選好の物差し
    public Share ImmigrationSelectiveFrac = new(0.5);   // 選別：受入上限を半分に
    public Share ImmigrationClosedFrac = new(0.2);      // 閉鎖：受入上限を2割に（完全ゼロにしない＝密航・家族呼び寄せ・特例は史実にも残った）
    public double TradeWeightBase = 0.15;   // 外1：動的TradeWeightのベース（現行式の切片に対応。#17較正）
    public double TradeWeightGdpSlope = 0.10;   // 外1：動的TradeWeightの相対取扱高シェア(世界平均1)への傾き（同上。#17較正）
    public double CanalTollTrafficRate = 0.02;   // 外6：取扱高あたり通行料率
    public Share CanalInvestReturns = new(0.5);   // 外7：運河建設拠出国へ通行料を持分比で配当する割合(0..1)＋交戦中の拠出停止
    public Share ConscriptionFloorFraction = new(0.02);   // 限定戦争の徴兵下限（depth=0の徴兵率＝働き盛りの男の割合・(145)。旧の旗 ConscriptionScalesWithWar は 2026-10-18 に焼き込み＝常に使う。#17較正）
    public double ConscriptionExhaustionWeight = 1.0;   // 外12：厭戦(WarExhaustion)→動員深度の重み（同上）
    public double ConscriptionFrontStep = 0.34;   // 外12：追加戦線1本あたりの動員深度増分（単一戦線=0で恒等。同上）
    public double ConscriptionEnemyRatioWeight = 1.0;   // 外12：敵陸軍優勢(比>1)→動員深度の重み（同上）

    // === リアリティ監査 2026-07-02 Wave4（政治・制度・外交の機構）===
    public double LegitimacyRegenRate = 0.1;    // 政1：正統性ストックの四半期再生率
    public double ReformFromAboveLegitThreshold = 0.4;   // 政2：上からの改革が発火する正統性の上限（Legitimacy<これ＝侵食が進行中の体制のみ自己改革）
    public double SecessionHoldCoercion = 1.0;   // 政5（分離の中央保持力判定がデッドコード）：>0でholdに軍・警察(Coercion)由来の飽和項を加え第2条件を活性化（大軍の帝国ほど分離を抑える）
    public double SecessionCultureAdditive = 3.0;   // 参考例（5.0の場合）＝強民族州(文化距離1×少数派0.8×不満0.8)で加算≈0.64＝関税項と合わせ閾値超えが可能に＝バルカン型の民族分離を実数化
    public double CultureDistanceFromEthnic = 1.0;   // 0で厳密恒等
    public double SecessionHoldSharedArmy = 1.0;   // 0で分母1＝厳密恒等
    public double SecessionHoldProjection = 1.0;   // 0で厳密恒等
    public double WallLinkKm = 700.0;          // 壁の線分にする点の間隔の上限（データの最大間隔＝ヒマラヤ694・サハラ697 を含む）

    public double DominanceSpeed = 0.025;            // 四半期あたりの積分速度＝(hold−pressure)≈−1 が10年続くと 1→0（分離判定と同じ四半期刻み）
    public double RuleOrderByRegion = 1.0;           // c69po 州ごとの治安 P1-1 の旗
    public double DominanceMatureYears = 30.0;       // 初期値＝保有年数÷これ（世代長＝同化の物差し）
    public double OpeningColonyDominanceFull = 1.0;  // 接収（Diplomacy.AcquireColony）は常に 0
    public double ColonyLabelsFromData = 1.0;   // (1278) c131cr 2026-11-21 点火（腕 B の木）：1＝昇格した植民地の州の文化と宗教の札を places.json の土地のデータから読み（無い名は空）、国の主流の札は植民地格の州を数えない・0＝旧（首都の州の札の写し・全ての州で数える）
    public double MajorityLabelTierOrder = 1.0;   // (1290) c133mr 2026-11-22 点火（腕 B の木）：1＝ColonyLabelsFromData 1 の下で、本国格に札の付いた人口が無い札（文化と宗教は別々）だけ植民地格の州で数える・0＝今（植民地格だけの国の札は空）
    public double NextCapitalHomeFirst = 1.0;   // (1304) c135nc 2026-11-22 点火（腕 B の木）：1＝首都の付け替え（Territory.NextCapital）の候補を本国格（Core・Integrated）の州に限る（本国格が無ければ今と同じ全ての州）・0＝今（格を見ない）。NextCapitalCoreFirst 1（既定）の下では読まれない（この分岐は CoreFirst 0 のときだけ）
    public double NextCapitalCoreFirst = 1.0;   // (1332) c139nc 2026-11-24 点火（腕 B の木）：1＝首都の付け替え（Territory.NextCapital）の候補を leaving の外で最も高い格（Core＞Integrated＞Colonial）の州だけにする（NextCapitalHomeFirst の分岐を置き換える）・0＝今（NextCapitalHomeFirst の形）
    public double ForeignRuleFromTreaty = 0.5;   // DominanceSpeed で目標へ寄せる
    public double TreatyPortHomeOnly = 1;        // (1138) c111tp 2026-11-14 点火：1＝条約港の読み手（ForeignPortShare・TreatyPortInForce）が植民地格の港を数えない・0＝旧（全ての港）
    public double ProjectionRuleMin = 1.1;   // 1.1（到達不能）＝{首都}＝旧＝厳密恒等
    public double ProjectionUsesLand = 0.0;   // §14b：非港州も陸路距離（PathGraph）で起点にする（露の中央アジア・シベリア＝鉄道の延伸が投射になる）
    // (1199) 削除：PeacetimeConscriptGrievance（支配度doc §15・平時の植民地徴募の遺恨・既定 0）。K3 の 1.0 は採らず（(1194)・推奨 (a)）、分岐ごと消した

    public double NoblePrivilegeWeight = 2.0;   // 特権身分の加算（伝統に＋これ・反動に＋これ/2）＝旧分岐の伝統4/反動2と地代依存度2×1+0.5 の差を埋める尺度（旧と同等の合計から出発）

    public double CompostProductivity = 0.5;        // 化学法（グアノ法と同じ2.0）比
    public double CompostLivestockInput = 1.0;      // 肥料1単位あたりの畜産（厩肥）
    public double CoalitionClosenessFloor = 0.5;   // 床＝CapitalCreditFloor と同じ「ゲートでなくプレミアム」の書き方
    public double ElectoralDistortion = 0.3;   // 政9：得票→議席の制度歪み
    public double InterestGroupHeadcountWeight = 0.25;   // 政11：利益集団の力の頭数按分
    public double PensionOldWeight = 2.5;               // 年金の受益者項：SocialSecurity Pref にブロックの高齢比率×これを加算＝高齢化した国だけ年金が通る（ビスマルク1889型分岐）
    public double LaborMilitancyConcession = 4.0;       // 労働軸（労組容認・時短）への闘争譲歩：資本/旧支配層の反対が大衆闘争度で緩む（参政権SuffrageConcessionと同型）
    public double BourgeoisCoupWeight = 1.5;            // 法多様性・政体 2026-07-07
    public double ModernizationOldGuardResist = 0.5;  // 旧支配層の政治力シェア1.0あたり近代化投資が止まる割合（免許・検地・認可を握るのが旧支配層）
    public double ModernizationCostGdpShare = 1.0;   // frac/(1-frac) に掛かる係数
    public Share SpreadLiteracyFull = new(0.5);       // 0.5＝読み書きできる者が人口の過半（AI の置き・確証なし）
    public double SpreadNeedFascismDecline = 1;       // 0＝今の式（危機×人々の闘争度）
    public double TechProductivityBracketOff = 0;     // World.TechProductivity の技術の括弧を whOut×union だけにする
    public double IndustryInvestRegionCap = 5.0;        // 州の上限＝この工業化度に達した州は AI の近代化の信号（World.StateInvestEligible）の適格から外れ、巨大建造物の直接の足しもこれで切る
    public double FascismCrisisSlumpRef = 0.20;   // 恐慌の物差し＝GDPドローダウンがこれで満額（0.20＝−20%＝史実の大恐慌級）＝ファシズムの発見の必要性（危機）
    public Share FascismCrisisLeftRef = new(0.30);    // 左派の脅威の物差し＝社会主義＋共産主義のシェアがこれで満額＝同上
    public double WarWearinessKeepPerMonth = 0.97;   // 非交戦国厭戦の月次残存率（旧の旗 WarWearinessDecays は 2026-10-18 に焼き込み＝常に使う）
    public double FrozenTensionDecayRate = 0.2;   // 開戦不能ペアの≥100緊張に課す月次減衰（旧の旗 FrozenTensionDecays は 2026-10-18 に焼き込み＝常に使う）
    public double FiscalExhaustionWarWeariness = 0.05;   // 外13：財政破綻の厭戦寄与＝(1-payRatio)にこの係数を掛けてWarExhaustionに月次加算（国庫枯渇＝戦費調達限界が講和事由に）
    public double ConquestColonyMilitancy = 1.5;   // 1.5＝武力奪取(0.45)＞無血併合(0.3)の序列＝アルジェリア型の抵抗

}
