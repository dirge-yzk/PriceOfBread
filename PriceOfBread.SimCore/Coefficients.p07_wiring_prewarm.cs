using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // ===== 2026-07-24設計群（7doc）＝点火状況は各行参照 =====
    public double SubsidySoftBudgetDecay = 0.4;        // 2026-07-24点火試験（レバー1＝恒久依存で最大4割の非効率）
    public RatePerMonth SubsidyDependenceRatePerMonth = new(0.007);       // 依存ストックの月次増分（+0.02/四半期相当）
    public double AiDevelopmentalSubsidy = 0.0;   // 債務の重さは利子の請求が大きくなることで国家の支払い率 r に出る＝補助は補助率×r の1本（EconomyStep の④）
    public Share BondRateRepresentationDiscount = new(0.1);   // 議会統制力の連続量へ改修して再点火（旧・政体ラベル二値0.2は2026-07-25差し戻し＝DebtGdp年換算フリップで再挑戦解禁の記録どおり）
    public double GoldRejoinCoverMargin = 1.3;      // 1.0=現行恒等（シュミットトリガー＝境界チャタリング防止）
    public double GoldCoverCreditRestraint = 0.5;   // 金本位国のカバー率防衛＝coverageが床(GoldCoverMin×(1+これ))未満で与信上限を比例縮小（1890年代バンクレート防衛の与信面＝兌換を守るため信用を絞る・与信↓→BroadMoney↓→coverage回復の負帰還）
    public double CivilWarEconomyDisruption = 0.2;  // 内戦中の実効生産性×(1−これ)（流通麻痺・徴発・戦場化・労働の逃散）
    public double WarCivilianToll = 0.002;          // 戦争が本当に恐ろしくなる（厭戦→革命の実体＝餓死）
    public double WarZoneCapitalDamage = 0.01;      // 対外戦争＝劣勢側の領土が戦場になる：敗色×接触強度に応じ工業化資本・港湾が月次摩耗
    public double ImposeCrownFromLoser = 1.0;   // 門（政体の差とイデオロギーの差）は勝者の政体のまま
    public double ImposeRegimeIdeologyGap = 0.4;    // 発動に要する政体イデオロギー距離（Ideology()スカラー差＝絶対0.0⇔共和0.60や立憲0.45⇔絶対が入り、立憲⇔共和0.15は入らない）
    public double ImposedRegimeLegitimacy = 0.3;    // 押し付け政体の正統性上限（低く始まる＝復古の芽。実績ストックが失望を蓄積→宗主が弱れば揺り戻し＝ウィーン体制を書かずに復古が出る）
    // === 全域監査2026-07-11の配線系13本（2026-07-25一括配線・2026-07-27に全点火・第1〜3群）===
    public double TransportCongestionWeight = 1.0;   // 輸送容量目標×(1+これ×活動州の(1−律速率)平均)＝混雑シグナルを目標へ接続（交易集約国が増強・非交易国は誤配分しない）
    public double TariffInterestPrefWeight = 0.3;   // 州関税利害（競争力）の国平均を近代エリートの関税選好へ加算＝後発工業国の保護主義（米国システム/独1879）
    public double TariffStateRevenueWeight = 0.08;   // 国家の歳入要求を関税軸へ（旧・専売の腕と同型＝19世紀最大の歳入源）
    public double EpidemicOnsetSanitationWeight = 0.4;   // 疫病の発生率×(1−これ×最過密州の衛生整備度)＝衛生インフラが流行の発生自体を止める（従来は死亡率側のみ）
    public double HealthcareRealGate = 0.5;   // 0=第3項なし（現行）
    public double CrimePushWeight = 6.0;   // 犯罪率への押しの重み（犯罪率の上限は CrimeBase 0.01×(1＋CrimeUrbanWeight 0.6)＝0.016 しかない）
    public double DisasterAgeMortalityExcess = 0.4;   // 災害死（疫病・飢えの超過死亡）の年齢の重み＝0〜9 と 60以上の1人あたりの死に (1＋これ)（2026-09-30 段4e 後半＝年齢の層・Pop.DisasterWeights＝POP の間と POP の中の1本）
    public double ExternalPressureInlandFactor = 0.5; // 接7：内陸国（活動州に港なし）の外圧係数＝黒船は海から来る（陸伝いの外圧は弱い）
    public double ChilledMeatProductivity = 1.4;  // 冷蔵レシピの生産性倍率（塩漬け比）
    public double ChilledMeatColdInput = 0.15;    // 加工肉1単位あたりの冷蔵庫の投入量（設備の摩耗＝機械と同じ扱い）
    public double KeroseneOilInput = 0.5;         // 灯油1単位あたりの原油
    public double KeroseneOilProductivity = 1.6;  // 連続精製の生産性（既定＝鯨油レシピの相対。史実の価格差5〜10倍はこれと投入係数の合成で出す）
    public double FoodProcessingNoEngineer = 0;   // (1237) c125al 旗：1＝食品の加工の財（Catalog.IsFoodProcessingGood）の既定のレシピ（CatalogEconomy）から高度労働力（技師）の投入の行を外す・0＝今のまま（全ての工場に AdvLaborPerOutput の行）。参照価格（Catalog.RefPrices・static）は旗を読まない＝旧の値のまま（焼き込みの時に Catalog の枝へ移す）
    public double MusketProductivity = 0.8;     // 前期レシピの生産性＝既定（鋼）レシピ比（前装式・手工程）
    public double WeaponStrengthRifle = 3.0;       // 構築時（Simulation.AddAlternateRecipes）に一度だけ読む
    public double WeaponStrengthArtillery = 4.0;   // 構築時（Simulation.AddAlternateRecipes）に一度だけ読む
    public double LandRoomFertilityCap = 0.6;      // 旧1e9＝実質無上限で、米は西部解錠後 landRoom≈3.7＝出生倍率4.7倍→年6.4%成長（史実3.09%・人類最大の観測値フッター派約4.0%）＝生物学的に不可能だった
    public double LandAnchorCapacityWeight = 1.0;   // capacityBaselineへ＋これ×max(0, Σ耕地/(1−都市化)−capacityBaseline)＝土地が養える人口を天井に（米25×の構造解・下方向なし・センチネル除外で較正厳密恒等）
    public double SanitationCapacityWeight = 0.4; // 衛生の扶養力連結（近代公衆衛生→死亡率低下）：sanitation=上下水道(InfraFill)+石鹸(SoapFill)+電化(ElecInfraFill)の人口加重∈[0,1]
    public double FoodFillSmoothing = 0.2;        // FoodFill の移動平均速度（備蓄＝穀倉が月次の振れをならす。餓死は単月でなく数ヶ月の持続的欠乏で起きる＝時定数≒5ヶ月）
    public int MedicalGoodId = Catalog.MedicineId;                // 医薬品の good.Id（社会保障＝公共医療が消費する資材）
    public Share InsuranceCoverageMax = new(0.6);     // 健康保険(#3)：国家が医薬品の購入費をこの割合まで肩代わり（実効負担＝社会保障×充足×これ）
    public int SoapGoodId = Catalog.SoapId;                   // 石鹸の good.Id（消費充足が衛生＝公衆衛生を上げる）
    public double MedicalWelfareBoost = 2.0;      // 医薬品需要 ＝ 基礎 ×(1＋社会保障×これ)
    public double HealthCrowdingBoost = 2.0;      // 疫病圧(都市化×(1−衛生))が医療需要を最大この倍率上乗せ（満額で×3）
    public double HousingNeedPerUrban = 0.03;   // HousingUrbanThreshold超で立つ
    public double HousingUrbanThreshold = 0.25;  // 点火2026-08-31＝0.45→0.25
    public double HousingMigrationFriction = 0.5; // 移民引力に住居充足(HousingFill)を負の摩擦として乗じる強さ
    public double SoapSanitationWeight = 0.3;     // 石鹸消費が衛生に与える上乗せ（衛生＝Clamp(InfraFill + これ×SoapFill, 0,1)）
    public double InfraNeedPerUrban = 0.04;       // 都市人口1あたりの上下水道の必要量（衛生インフラ）
    public double InfraBudgetScale = 0.15;        // 上下水道＝歳入シェア（行政費スライダー×これ×歳入×人口比）
    public double ElecInfraNeedPerUrban = 0.03;   // 都市人口1あたりの電化インフラの必要量（上下水道より少なめ＝上位財）
    public double ElecInfraBudgetScale = 0.15;    // 電化インフラ＝歳入シェア（上下水道より控えめ＝先進国の余力で電化）
    public double ElecInfraSanitationWeight = 0.2; // 電化インフラ充足が衛生に与える上乗せ（電動ポンプ/上水処理＝上下水道に次ぐ）
    public double ElecInfraGridBoost = 0.5;       // 電化インフラ充足が送電容量(GridCapacity)を底上げする割合（夜間操業の工場電化を後押し＝既存の電化ブーストを増幅）
    public double ChildLaborLiteracyPenalty = 0.6; // 児童労働を禁止すると識字成長 ×(1＋これ)（子供が工場でなく学校へ）＝出力と教育のトレードオフ

    // --- POP動詞3種（2026-07-19ユーザー裁定・設計doc「POP動詞設計」）。全て中立恒等＝動詞を使わない限り不走行 ---
    public double ConscriptGrievanceRate = 0.05;      // 戦時の強制徴兵（徴兵法≥選抜の国・総動員はその深さを上げる）で徴兵された階級の闘争度加算×徴兵率＝E5徴兵法の一般効果

    // --- 軍需調達（国家が軍需財を消費＝死蔵解消。財源は国庫→生産者で保存。戦時増＝武器需要ブーム）---
    public int[] MilitaryGoods = [Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId, Catalog.WarshipId, Catalog.SteamshipId, Catalog.HorseId, Catalog.AutomobileId];   // 裸の127をHorseIdへ＋自動車132を輜重へ（馬と併存＝WWIも馬匹依存だが自動車が食い込む。DemandOnset1.50到達後の晩期のみ実需） // 小銃・弾薬・火砲・軍艦・汽船・馬・自動車
    public double[] MilitaryRates = [1.0, 0.8, 0.4, 0.15, 0.2, 0.1, 0.02]; // 各軍需財の消費比率
    public double[] MilitaryWearRatesPerMonth = [0.02, 1.0, 0.02, 0.02, 0.02, 1.0, 0.02];   // 必要量＝汽船1003／小銃5013／火砲2005／軍艦501 に対し買えたのは 汽船0.0／小銃32.7／火砲13.9／軍艦6.4＝装備充足0〜1%
    public double MilitaryUpkeepScale = 0.18 / Military.WorkingMenStdShare;                 // 兵士1人あたりの平時軍需消費スケール（0.12→0.18＝戦争が減っても常備軍の調達で火薬/鉛/帆船の連鎖を維持＝死蔵防止）
    public double InitialArsenalMonths = 60.0;   // 播種の対象財は Simulation.InitialArmsIndustryGoods
    public double MilitaryWarMult = 9.0;                      // 元3.0の3倍＝9.0（5倍は軍が溶けすぎたため3倍に決定）
    public double WarMobilizationDepth = 0.5;                 // 長期戦ほど総動員(WWI型)
    public double MilitaryWarMultFromContact = 1.0;           // 0＝今の式（事前登録 measurements/m959_20261108_prereg_mw.txt）
    public double WarArmsDemandPerContact = 42.0;             // 接触の割合 1 あたりの軍需の上乗せの倍率（旗 MilitaryWarMultFromContact が読む・戦時の倍率＝1＋これ×接触の割合）
    public int[] MilitaryQualityGoods = [Catalog.MachineGunId, Catalog.IroncladId];          // 機関銃・装甲艦
    public double[] MilitaryQualityRates = [0.5, 0.1];       // 各上位軍需の消費比率（基本より少量＝高価な少数精鋭）
    public double MilitaryQualityBonus = 0.6;                // 上位軍需を満たした時の Power.Military 乗数の最大上乗せ（+60%）
    // --- 軍需輸出の法（禁制品doc C5・2026-07-30）：「許可／禁止」の1本の法＋緊張の閾値で、売らない相手を**法が選ぶ**（50カ国の手動管理を作らない）---
    public int[] MilitaryExportGoods = [Catalog.RifleId, Catalog.AmmunitionId, Catalog.ArtilleryId, Catalog.WarshipId];      // 輸出禁止の客体＝小銃・弾薬・火砲・軍艦
    public double MilitaryExportTensionThreshold = 60.0;      // この緊張以上の相手へは軍需財を輸出しない（尺度0..200・ライバル40とポップアップ100の間）
    public double MilitaryExportPrefScale = 10.0;             // 軍需財の生産額シェア→資本（近代エリート）の反対強度（シェア0.1で−1.0＝他軸と同水準）
    public double MilitaryExportAccessFloor = 0.1;            // 禁止しても残る輸出口（Diplomacy.TradeAccess と同じ床＝全遮断はしない＝密輸・第三国再輸出の抽象）
    // --- 密輸の一般機構（禁制品doc C2・2026-07-30）：障壁（禁令）× 取り締まる力（行政充足率）。価格差は呼び出し側の式に既にある（Contraband.cs 参照）---
    public Share SmugglingMax = new(0.5);                         // 行政が完全に無力(AdminFill=0)なとき禁令をすり抜ける割合
    // --- 阿片の法（禁制品doc C3・2026-07-30）：旧 bool の行政禁令 OpiumBan を議会を通る法2本へ。賛否は保有から創発させる ---
    public double OpiumBanPrefScale = 4.0;                    // 全国中毒度→大衆の禁令要求の上乗せ（中毒0.2で+0.8＝他軸と同水準の強い要求）
    public double OpiumTradePrefScale = 10.0;                 // 阿片の生産額シェア→売る側の資本の反対強度（軍需輸出と同スケール）
    // --- プランテーション（プランテーションdoc D6・2026-07-30）：地代取り分を州別・財別へ ---
    public Share PlantationRentShare = new(0.6);                  // 賦存財のRGO（＝その州の輸出向け一次産品＝大農園/鉱山）の地代取り分の下限
    public int[] MilitaryTier3Goods = [Catalog.TankId, Catalog.DreadnoughtId];            // 戦車（陸tier3）・弩級戦艦（海tier3）
    public double[] MilitaryTier3Rates = [0.04, 0.015];      // 各tier3の消費比率（上位より更に少量＝極少数の決戦兵器）
    public double MilitaryTier3Bonus = 1.2;                  // tier3を満たした時の Power.Military 乗数の最大上乗せ（+120%＝tier2の+60%を圧倒）
    // --- 1934拡張E2（2026-07-19）：空軍装備＝tier3の更なる残余予算で調達する第4tier。財の解禁(onset1.0=1910s)前は生産ゼロ→Fill0＝空の概念ごと不走行＝厳密恒等 ---
    public int[] MilitaryAirGoods = [Catalog.MilitaryAircraftId, Catalog.ChemicalWeaponId];         // 飛行機（偵察/輸送）・戦闘機・化学兵器
    public double[] MilitaryAirRates = [0.05, 0.02];   // 各空軍財の消費比率（tier3級の少量＝最晩期の高価な装備）
    public double AirControlPushWeight = 0.3;                // 制空の押し合い補正＝aPush×(1−これ/2＋これ×AirControl)
    public double AirCasualtyWeight = 0.3;                   // 制空劣勢側の戦死増幅＝cas×(1＋これ×(2×(1−AirControl)−1)クランプ正)
    public double WarExhaustionAirRaid = 0.03;               // 空襲の厭戦（封鎖と同型）＝これ×(1−AirControl)×敵AirPower実在ゲート
    public double ChemExhaustionWeight = 0.04;               // 化学兵器の泥沼＝相互保有 min(自Fill,敵Fill)×これ を双方の厭戦へ（イーペルの相互報復）

    // --- B-1 海軍力・制海権・距離（§B 軍事リアリティ）。海軍は新スライダーを足さず Power.Military（陸海合算）から地理で創発分離 ---
    public double SkillUpkeepThreshold = 0.5;      // 平時にこの維持費を超えると熟練度が育ち、下回ると錆びる（作者仕様「50%以上で増加、以下で減衰」）
    public double SkillGainRate = 0.02;            // 熟練度の四半期あたり獲得率（閾値超過分×これ）
    public double SkillWarGain = 0.05;             // 戦争中（内戦含む）の四半期あたり獲得率＝実戦は訓練に勝る
    public double SkillDecayPerQuarter = 0.0115;   // 平時に閾値を下回るときの減衰率／四半期
    public double SkillCombatMax = 0.5;            // 熟練度1.0での戦力倍率の上乗せ＝最大1.5倍（2026-08-04・作者の問い「最大2倍は史実的に適切か」への判断）
    public Share WartimeUpkeepFloor = new(0.8);        // 戦時の維持費の下限＝現在値のこの割合を下回れない（戦争中に軍を解散できない）
    public double SkillTensionWeight = 0.5;        // 熟練度が生む緊張＝軍隊規模に乗算する形（作者指示）
    public RatePerMonth NavalAttritionRatePerMonth = new(0.05);      // 月次の摩耗率＝これ×(自分の展開海盆での敵支配度)＝その月に沈む艦の割合（(189) 艦の在庫を財ごとに同じ割合で沈める・旧＝海軍シェアを削り床 NavalShareMin）
    public double AttentionHostilityFloor = 0.15;  // 平時（緊張0）でも残る敵意＝「無政府状態では武力は武力」
    public double AttentionTensionHalf = 40.0;     // 敵意が半分に達する緊張
    public double AllyAttentionDiscount = 0.7;     // 宗主と属国の間の敵意の割引（3割だけ向ける）＝作者裁定 2026-09-29 の5「属国は仮想敵国ほどではないが、脅威を与える」（ArmyShape.Hostility・脅威の緊張が同じ1本）
    public int FleetContactPerGdp = 1;   // 開放度は規模に依らず〈海の交易にどれだけ依るか〉を読む（MultiNationSim の TradeGoldPull と同じ物差し）
    public double InternalGarrisonPerPop = 0.002;  // 不穏度1.0のとき国内秩序に要する兵÷人口
    public double NavalCrewPerGauge = 0.5;   // 同じ戦力ゲージあたりの乗員（海軍は資本集約・陸軍は労働集約）＝英の常備陸軍が人口比で小さいことの本質
    public int[] FleetGoodIds = [Catalog.WarshipId, Catalog.IroncladId, Catalog.DreadnoughtId];   // 軍艦・装甲艦・弩級戦艦（＝戦闘艦。汽船85/帆船84は輸送・補助なので艦隊に数えない）
    public int WarshipGoodId = Catalog.WarshipId;                // 軍艦（(189) 1隻の乗員の物差し＝帆船の戦列艦〔EconomyStep.FleetCrewPerHull〕。旧＝修理需要の宛先〔撤去〕）
    public Share AbstractNavalShare = new(0.10);      // 抽象標的（被植民地勢力）の海軍シェア＝弱小（陸の守りが主）
    public double DistanceWarDecay = 1.0;         // 戦力投射の距離減衰（1あたり1万km）
    public double HomeDefenseBonus = 1.25;        // 本土防衛の地の利（抽象標的＝自領で戦う側は投射せず守りに専念）
    public double InvadeSeaControlMin = 0.45;     // 海越え侵略に要る最低制海権（弱海軍の内陸国は海外植民地を取れない＝英の海上覇権が意味を持つ）
    public double PushUnrestWeight = 0.5;            // 移民のプッシュ要因：地域の不満（迫害・治安悪化）が土地機会を下げ流出を促す（ポグロム型）

    // === 開始前の助走（作者裁定2026-08-10「助走をプレイヤーの世界と自己対戦バッチで統一し、延長する」）===
    public int StartWarmupQuarters = 20;        // 助走の長さ（四半期）
    public int StartWarmupTechIterations = 8;  // 技術の助走の反復回数（旧 GameViewModel のハードコード8）
    public RatePerMonth ConsumeBudgetSavingsRatePerMonth = new(0);   // d＞0 の月率（貯蓄のうち1か月に消費へ回してよい割合）

    public int MonopolyBurdenPushMode = 1;   // MonopolyBurdenPushMode＝AI が専売の段を広げる判定に、払えていて r＝1 のままでも利払いが重い間（借入の門が閉じ国庫を食う間）の動きを足す


    public double ClearingIdleSearchQuarters = 4;   // ClearingIdleSearchQuarters（②・N）：前の月に輸入が無く、不足も無いか主食でない財の不足だけの買い手行（州×財）は N 四半期に 1 回だけ、四半期の最初の清算で売り手を探す（行ごとに位相が散る）
    public double ClearingPartnerScope = 4;   // ClearingPartnerScope（①・K）：四半期の 2・3 月目の清算で、その四半期の最初の清算に成約した買い手行は、その清算で相手にした売り手（先の K 人・1 以上）から先に買い、
    public double ClearingIdleThrottledScan = 1;   // 番でない眠る行は潜在量の加算（BilateralMarket の potNew）の手前で飛ばされる＝LastTradePotential は番の行が見た分だけに過小に記帳され
    public double ClearingIdlePotentialReweight = 1;

    public const int GameQuarters = 668;                     // ゲーム全期間＝四半期数（1783/01/01→1950/01/01＝167年・1950延長2026-09-21作者裁定＝旧604（1934拡張2026-07-19）から+64四半期。selfplay/プローブの正準の長さ）
    public const int FidelityQuarters = 524;                 // 史実忠実度の主指標の断面＝Q524（1914年1月1日＝1913年の締め）
    public const int FidelityQuarters1820 = 152;             // Maddison の基準年＝1820年12月（(1820−1783)×4+4）
    public const int FidelityQuarters1870 = 352;             // 同 1870年12月
    public const int FidelityQuarters1920 = 552;             // 1920年12月の政体だけを見る（経済は戦時攪乱で物差しにならない）
    public const int FidelityQuarters1950 = (1950 - Calendar.StartYear) * Calendar.QuartersPerYear;   // 政体と従属だけを見る（exact/near）
    public double HarvestVariation = 0.25;                    // 豊作/不作の振れ幅（毎年 1±これ で主食RGO産出と自給を変動。不作→食糧高騰→貧農の飢え→不満。決定論RNG）
    // === テーマB（飢饉・天候）＝点火済み（Skew0.3/ρ0.35/帯ρ0.35・2026-07-07/07-09）。0で旧IID恒等 ===
    public double HarvestSkew = 0.3;                          // 収穫分布の負歪み（symmetric-uniform-harvest）
    public double HarvestPersistence = 0.35;                   // 収穫の年次自己相関ρ（harvest-iid-no-persistence）
    public double WeatherBandPersistence = 0.35;              // 緯度帯天候の年次自己相関（多国シム限定）
    public double BlockadeStapleBias = 0.5;                  // 海上封鎖の主食狙撃（blockade-no-famine-strangulation）∈[0,1]
    public double FaminePushMaxFracBoost = 1.0;             // 飢饉時の移民上限緩和：(1-FoodFill)に比例してEmigrationMaxPerQuarterを引き上げ
    public double GoldRushEndowment = 5.0;                   // 開いた大鉱床の富鉱倍率（浅い高品位の鉱脈＝通常の賦存4.0に掛ける＝開いた財の生産者だけ・Discovery.OpenSite）
}
