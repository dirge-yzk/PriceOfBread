using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === 借入（Fiscal.Borrow の1行＝月初に払えない額があればエンジンが自動で借りる＝切り下げより先・2026-09-28 (b)）：1件の額＝名目GDP×これ・借入額＝min(1件の額, 月初の払えない額)・上限＝利払い÷歳入 ===
    public Share BorrowFractionOfGdp = new(0.10);    // 1件の額＝月次名目GDPのこの割合＝月に借りられる額の上限（資本家貯蓄＋外資から調達＝保存）
    public Share MaxInterestShareOfRevenue = new(0.67);   // 借入の上限＝直近の月次決算の利払いの請求÷歳入がこれに届いたら借りない（借入の門・不履行の判定・巨大建造物の「まだ借りられる」が同じ1本を読む）
    public double DefaultArrearsMonths = 1.0;   // その累計が月の請求の何か月分に届いたら不履行（1.0＝国の全ての支払いの1か月分を払えなかった）
    public double DefaultLegitimacyHit = 0.5;    // デフォルト時の正統性崩壊（→闘争度↑・革命リスク）
    public int DefaultCooldownMonths = 24;     // この間は赤字を切り下げ/薄給で凌ぐしかない
    public double CrowdingOutWeight = 1.5;       // クラウディングアウト（金利チャネル）：政府の国債金利プレミアム(GovBondRatePerYear−基礎)×これ を民間投資のハードルレートに上乗せ
    public double CrimeOrderPerPop = 0.8;        // 治安の必要治安維持力（人口あたり強度）
    public double CrimeBase = 0.01;              // 秩序が完全に崩れた時の月次窃盗率の上限（被害者の基数に掛ける率・基数は下の CrimeVictimBasis）
    public double CrimeMilitancyWeight = 1.0;    // 犯罪率に効く「貧しく不満な大衆」の重み（闘争度×不充足）
    public double CrimeUrbanWeight = 0.6;        // B-4：都市化（過密スラム・匿名性・相対的剥奪）が犯罪を増幅＝犯罪率×(1+これ×都市人口比)
    // === テーマG（社会・不満・格差・犯罪）＝**大半が有効化済み**（CorruptionLegitimacyErosion は (383) で物理削除・CrimeSatPenalty/CrimeWealthFear は (384) で削除〔犯罪→満足度の直の減算は窃盗の移しで貯蓄を通って充足に既に入る〕・StrikeMilitancyThresholdの土台のみ残／InequalityMilitancyは段2で削除）。台帳訂正2026-07-12（監査 死22）===
    public double CriminalClassEnabled = 0.0;    // 犯罪者 POP の器の主フラグ（治安と所有の安全 段0）＝0 で犯罪者 POP を並べない（Pops.Count も Region.CriminalPopIndex=-1 も不変・厳密恒等）／>0 で州の Pops の末尾に規模 0 の犯…
    public double StrikeProductionHit = 0.25;     // 本国の労働争議減産（no-graduated-unrest-stages）：組合容認かつ高闘争地域でstrikeIntensity×これ だけ操業停止
    public double StrikeMilitancyThreshold = 0.5; // ストライキ開始の地域平均闘争度の中間閾値（革命未満の争議段）
    public double LaborUnionToleratedOutputMul = 0.95;  // 労働組合(容認)の工場出力乗数＝賃上げ/ストで▲5%（資本が反対する理由）
    public double LaborUnionControlledOutputMul = 0.98;  // 労働組合(統制)の工場出力乗数＝御用組合で▲2%
    // === テーマF（政治・近代化回路）===
    // --- 政府・政策（介入レバー。スライダーは利益集団の力で律速して漸進）---
    public double PolicyBaseSpeed = 0.12;        // スライダーが目標へ寄る基本速さ（四半期）
    public double PolicyResistanceWeight = 4.0;  // 力ある集団の抵抗の強さ（速さ ÷ (1+これ×力)）
    // --- 役務（サービス財）の経済：教育者の生産性は法で切替（聖職者の宗教学校＝少人数しか教えられない＝供給希少）---
    public double ClergyEduProductivity = 2.0;   // 聖職者1人あたりの教育算出（宗教学校）
    public double TeacherEduProductivity = 5.0;  // 教師1人あたりの教育算出（公教育・義務教育）
    public double EduProducersCoexist = 0.0;   // 「公立学校（教師・TeacherEduProductivity・書籍112×0.05）」の常設2件として並置する（E-13 の黄鉄鉱法・蒸気タービンと同じ「同じ財に複数レシピ」型）
    // --- 電化（電力＝稼働率ブースト。時代クランプなし＝発電・送電の価格が下がると採算が立ち電化の波が創発）---
    public double ElecPerOutput = 0.25;          // 産出1単位あたりの電力消費（電化を選んだ工場）
    public double ElecBoost = 0.3;               // 完全電化時の能力上乗せ（夜間操業・機械駆動）
    public double ElecLaborSave = 0.4;           // 完全電化時に労働力の投入が何割減るか（機械化＝人手の節約）
    public double LaborSaveRate = 0.25;           // 技術的失業（no-labor-saving-tech-unemployment）：機械化工場の労働投入の最大逓減率
    public double LaborSaveIndHalf = 1.0;        // 機械化（労働節約）が半分に達する工業化度
    public double EffPerOutput = 0.08;            // RGO産出1単位あたりの効率化材消費（profit-gateで割に合う時だけ買う）
    public double EffBoost     = 0.3;             // 完全充足時のRGO産出の上乗せ（肥料/飼料/漁船/工具が満たされた時）
    public double SubsistenceStage2 = 1;          // 0＝旧の形（食べる割合は欄のデータのまま・1 ビットも変わらない）
    public double SubsistenceNoBoughtTech = 0;    // 0＝今の形（S1-a＝自給の食べる割合と焼く ρ と農民の失業の必要な労働に、同じ州の主食の RGO が買った効率化材の倍率 T を掛ける）
    public double GridBuildRate = 0.1;           // 送電容量の不足分を月に何割ずつ敷設するか（投資家の残予算の範囲で）
    public double FillInertia = 0.25;            // 電化率(ElecFill)・効率化率(EffFill)が目標へ寄る月次速度（設備の慣性＝ゆっくり進み剥がれる）
    public double ElecFillIrreversibility = 1.0; // 0=無効（採算で毎期進退）・1で電化率は下がらない（投資済み発電設備は石炭安でも剥がさない＝史実の電化一方通行）
    public double EnergyPerOutput = 0.05;        // 蒸気動力化した工場の産出1単位あたりの石炭（燃料）消費
    public double SteamBoost = 0.3;              // 完全蒸気動力時の能力上乗せ（蒸気機関＝水車/畜力を超える出力。電化のElecBoostと対称）
    // --- 女性・子供（構成属性。POP分割はしない）---
    public double FemalePartBase = 0.4;          // 女性の労働参加率の基礎（紡績女工・農婦・家事奉公＝19世紀の現実）
    public double FemalePartProperty = 0.7;      // 女性財産権法の下での参加率（自分の賃金を持てる＝働く誘因。労働供給+2割）
    public double ChildWorkFactor = 0.5;         // 児童労働の半人前係数（働く子供は0.5人分の労働力を算出）
    public double LaborWeightsByPower = 1;       // 旗（0＝旧の式・正＝重み）
    public double FemaleHandLaborWeight = 0.6;   // 手作業の女の 1 時間の産出÷成人男性（比）
    public double ChildHandLaborWeight = 0.35;   // 手作業の子（10〜14 歳）の 1 時間の産出÷成人男性（比）
    public double WomenRightsFollowSpeed = 0.15;  // 法の浸透の月次収束速度＝女性財産権法→賃金保持(WomenWageRetention)・児童労働法→合法度(ChildLaborLegal)・実労働時間→法定上限の 3 つで共有（法は離散だが社会的確立は連続）
    public double FemaleNeedWeight = 0.5;         // 家計困窮(1−満足)が女性の追加就労を押し上げる強さ
    public double FemaleLitFloor = 0.6;           // 賃金保持なし時の女性識字の実効係数（女子が就学を後回しにされる＝人口転換の出生率項。retention=1で1.0＝旧 WomenPropertyLaw?1:0.6 の連続版）
    public double OldLaborFactor = 0.75;   // ①Phase3：老年の労働参加率（0=完全引退〜1=フル稼働）
    public double PensionCostRatePerMonth = 0.8;         // 年金「十分」時・老年1単位あたりの月次給付（国庫→老年）
    public double EduPerChild = 0.06 / Pop.StdShare(5, 15);   // 学齢（5〜14歳）の子供1人あたりの教育の必要量＝人口あたり0.06（旧＝子供比0.3×0.2）÷標準のピラミッドの5〜14歳（0.225）＝約0.267
    public double FertilityBase = 1.0;           // 出生率の基礎倍率（無学の社会＝従来の較正値そのまま＝中立）
    public double FertilityLitSlope = 0.55;      // 女性の識字で出生率が下がる幅＝人口転換の因果（女子教育→少子化。多産側の加速は医療＝死亡率低下が担当）
    public double FertilityFloor = 0.5;          // 出生率の下限（先進社会でも人口は緩やかに増える）
    public double FertilityLitOnset = 0.1;       // 出生率が下がり始める女性識字の開始点（初等識字は出生率に効かない＝中等教育から。実証準拠）
    public double FertilityIncomeSlope = 0.15;   // 所得/発展（工業化度）で出生率が下がる幅（B-4）＝都市化・所得上昇の少子化
    public RatePerYear CrudeBirthRatePerYear = new(0.035);        // 粗出生率（年率・19世紀の実証値≈3-4%）
    // === 人口忠実度ラウンド（2026-08-21）＝「英が増えず（×1.68・帯2.6-4.3）仏が増えすぎる（×2.39・帯1.25-1.7）」の構造修正 ===
    public double DoctorHealthcareSubstitute = 0.5;   // 2026-08-21点火（単独A/B＝人口18.6→28.6%・英×1.73→1.95[0→10%]・日30→80%・西10→30%・破綻0/10・他次元ノイズ内）
    public double FamineExcessMortality = 0.9;          // 2026-10-01 (143) 飢えの超過死亡の絶対の率（年率・飢えの深さ1＝充足0 のとき）＝飢えた POP の月の超過死亡＝これ÷12×深さ^FamineExcessMortalityExponent×規模
    public double FamineExcessMortalityExponent = 4.0; // 2026-10-01 (143) 飢えの超過死亡の凸さ（飢えの深さの指数）＝史実の飢饉の死は栄養の不足に強く凸（浅い慢性の不足は病気との相乗で少し・深い不足は数か月で死ぬ）
    public double FamineDeathFillFloor = 0.8;          // 旧・連続(1−ff)重みは平均をわずかに下回るだけの州に恒常的な偽陽性の超過死亡を配っていた
    public double FamineMigrationFillGate = 0.8;       // 飢えの超過死亡の門 FamineDeathFillFloor と値は同じ 0.8 だが別の係数＝片方を較正し直してももう片方は動かない（旧は1本が2つの意味を持っていた）
    public double FamineGateReturnsRural = 1;          // (1283) c132fg 旗（2026-11-21 に 1・腕 B の木）：飢えの門が農村→都市を止めた月に、移る値打ち MigrationValues の g が負なら Mobility×g×都市の規模で都市→農村へ戻す（0＝止めるだけ）
    public double CropDiseaseChance = 0.02;        // 病害に弱い作物1つあたりの年次の胴枯れ発生確率（健全時のみ）
    public double CropDiseaseSeverity = 0.3;       // 胴枯れ時の作物産出倍率の基準（×0.5〜1.5の揺らぎ＝0.15〜0.45へ激減）
    public double CropDiseaseRecovery = 0.4;       // 病害からの年次回復速度（CropHealth→1.0へ。0.4で胴枯れは約4年で癒える＝多年飢饉）
    public double WeatherBandVariation = 0.12;     // 緯度帯天候の振幅（±12%＝帯全体の豊凶）
    public double WeatherBandSize = 15.0;          // 15度ごとに同じ気候帯＝近い緯度の国が一緒に不作
    public double EpidemicChance = 0.18;         // 疫病の年次発生確率の基礎（×脆弱度）
    public Share EpidemicSeverity = new(0.06);       // 流行時に地域人口が失う割合の基礎（揺らぎで0.5〜1.5倍）
    // === テーマC（疫病の完成）===
    public double EpidemicVulnConcentration = 0.5; // 疫病死を飢えた層へ集中する強さ（POP の重み＝Size×(1＋これ×(1−主食の充足))×年齢の重み）
    public int EpidemicContagiousYears = 3;      // 発生後に感染源であり続ける年数（この間は他国へ伝染しうる）
    public double EpidemicTransmitChance = 0.35; // 完全接触（隣接の感染国）から1年で伝染する基礎確率
    public double EpidemicTransmitRange = 6000;  // 近接伝播が効く首都間距離（kmこれ以内で陸路接触・遠いほど弱い）
    public double EpidemicTradeTransmit = 0.6;   // 交易/港の接触経路の重み（両国の交易開放度の積＝海路のコレラ。陸の近接と最大を取る）
    public double EpidemicWarTransmit = 0.85;    // 戦争＝距離に依らない強制的な濃厚接触（軍営・前線・補給線・復員兵）＝最強の伝播経路（スペイン風邪1918型）

    // --- 需要の階層（エンゲルの法則）と固執資材（§C 恐慌の社会的増幅）---
    public double DailyNeedWeight = 0.5;         // 日用品の満足度の台帳での重み
    public double LuxuryNeedWeight = 0.2;   // ＝買えない嗜好品は台帳に1ビットも載らず（罰にならない）、買えた嗜好品だけが比を1へ引き上げる（上振れ）
    public double ReservationWageFallPerDay = 0.25;    // 留保賃金が下方の目標へ追従する 1 日あたりの速度（小さいほど賃下げに抗う＝下方硬直が強い）
    public double ReservationWageRisePerDay = 1.0;     // 留保賃金が上方の目標へ追従する 1 日あたりの速度（賃上げ＝新しい生活水準へすぐ慣れる）
    public int PriceFlowStepDays = 10;   // 式の中の速度は 1 日あたりのまま
    public int PriceOnlyStepDays = 15;   // 0＝国内価格も PriceFlowStepDays（旧と1ビットも変わらない）
    public double ServiceClosedMaxCurv = 0.01;   // 既定 0.01（S1c・同上）
    public double WarWearinessMilitancy = 0.35;   // 厭戦（対外戦争の消耗）→闘争度入力

    // --- 転職（昇格・転落）。閾値＝国民平均の1人あたり貯蓄に対する倍率＝経済規模で自動スケール ---
    public Share ClergyFloorShare = new(0.002);      // 聖職者の下限（地域人口比0.2%）

    // --- 植民地の州昇格（2026-08-09・作者裁定2026-08-08「植民地州昇格＝実装」。設計原典＝docs/202607/20260731_植民地のラベル化_部分完了.md §3-3d）---
    public double ColonyPromotionMinPop = 60;        // 昇格に要る総人口（1単位=1000人）＝北西部条例1787「自由人口6万人で州昇格を申請できる」の一次データ
    // --- 商人（流通・貿易の仲立ち）---
    public double MerchantMargin = 0.04;         // 小売・問屋の取り分（消費者の購入に上乗せ→商人の収入）
    public Share MerchantTradeMargin = new(0.05);    // 国際交易の仲介料率＝取扱高（輸入額＋輸出額）に掛かる手数料
    // === テーマJ（関税戦争・公害）===
    public double TariffRetaliation = 0.5;       // 報復関税（tariff-no-strategic-retaliation）：相手の対自国関税の超過分に比例して自国関税目標を上げ/友好低関税国に下げる偏倚
    public double TariffReactionMax = 0.25;      // 報復/相互低減で偏倚させ得る上限(±)

    // --- 税制の拡張（土地税の検地台帳の錠は (383) で物理削除。所得税の天井/ラッチは2026-09-12に廃止＝行政の弱さは汚職漏れが額で表す）---
    public double PollTaxRate = 0.15;            // 人頭税スライダー1.0時の1人あたり月額（労働者の月収2〜3に対し約5〜7%＝重い）
    public double LandTaxFixedBasis = 1.0;       // 土地税の算定の基準 A-2（AI の案・確証なし・事前登録 measurements/m937_20261107_prereg_lt.txt）：> 0 のとき
    public double LordReligionStake = 1;         // c98da 器・
    public double LordIntermediaryStake = 1;     // c98da 器・(1048) c99db 本体・既定 1＝地主の政治 D-2 の旗（0＝官僚制度の専門化への旧支配層の反対は札 −1・土地改革は LordStake のまま。>0＝仲介の取り分の損〔得〕を足す）
    public double LandTaxPhiMove = 0.0;   // 0＝φ* は world.json の landTaxBasis のまま（恒等）
    public double AdminNeedPerPop = 0.04;        // 人口1単位あたりの行政の必要量
    // --- 社会移動（昇格・転落）：閾値は地域の1人あたり平均貯蓄に対する倍率＝経済規模で自動スケール ---
    public double CapitalRiseThreshold = 8.0;    // 中流→資本家の昇格閾値（平均の8倍の資金）
    public RatePerQuarter CapitalRiseRatePerQuarter = new(0.01);        // 中流→資本家の四半期移動率
    public double UpperFallThreshold = 0.8;      // 上流（貴族・資本家・聖職者）→中流の転落閾値（平均的市民より貧しい上流＝没落。高くしすぎると身分ごと溶ける）
    public RatePerQuarter UpperFallRatePerQuarter = new(0.02);          // 上流→中流の四半期転落率（恐慌で貯蓄が溶けると大量転落）
    public double MiddleFallThreshold = 0.4;     // 中流→下層の転落閾値（平均の4割を切る＝職が無い専門職）
    public RatePerQuarter MiddleFallRatePerQuarter = new(0.03);         // 中流→下層の四半期転落率
    public double InsolventThreshold = 0.10;     // 貯蓄/人 が 平均×これ 未満＝事実上の破産（信用延命中）とみなす
    public RatePerQuarter BankruptcyFallRatePerQuarter = new(0.15);     // 破産した所有者（資本家・職人）の四半期転落率（緩やか転落より速く抜ける＝ゾンビ延命を断つ。0.25は江戸を常勝化させた疑い→0.15へ緩和）
}
