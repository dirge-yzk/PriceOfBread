using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    public double StateProcureAsSold = 1.0;   // (1336) c140sp・(1357) c143sp 2026-11-25 点火（腕 B の木）：1＝国家の購入（InternationalMarket.Procure の buyer null）を LastExport でなく LastSold に積む（値段の式が国家の需要を 2 回数えない）・0＝今の形。番人 StateProcureLedgerTests
    // === 港湾容量（国家プロジェクト＋資本家参入・改修2026-06-28）。運河(StepCanals)を雛形に、港州の取扱容量を投資で積む。港0なら従来挙動＝較正中立。多国シム専用（StepPorts）===
    public RatePerQuarter PortInvestFrac = new(0.04);       // 資本家が毎四半期 港へ投じる州投資家貯蓄の割合（混雑時のみ＝採算ゲート）
    public Share PortStateFrac = new(0.15);        // 国家が港建設に投じる裁量余剰（FiscalCapacityPerMonth＝純税収から利払い・行政・軍需の請求を引いた余り）のシェア＝行政費予算の一部
    public double PortBuildCostPerUnit = 40.0; // 港容量1単位の建設費（資本/国庫→自州popへ還流＝保存）
    public FractionPerQuarter PortDecay = FractionPerQuarter.FromQuarter(0.008);           // 港湾の四半期減耗（維持に再投資が要る＝持続需要）
    public double PortCapToThroughput = 1.0;   // 港容量1あたり、その港州の実効海運への加法的上乗せ（港の大きさ＝交易の口の太さ）
    public double PortFeeRate = 0.01;          // 港湾料率＝海路の成約の額×これを港の1端ごとに港所有者(地元投資家)へ（商人仲介料とは別＝施設使用料）
    public double PortCongestionTarget = 0.9;  // この律速率(LastTradeThrottle)未満＝港/船腹が交易を絞っている＝拡張の採算が立つ（混雑ゲート）
    // === 海上保険（ロイズ型・改修2026-06-28・金融改革③）。商人pop＝引受手(基金)。平時に保険料(荷主→商人)、封鎖損失時に保険金(商人→荷主・基金の範囲で部分払い)。すべてpop移転で保存・多国限定で較正中立 ===
    public double MarinePremiumRate = 0.003;   // 保険料率＝海路の成約の額×これ（1件の航海あたり・荷主→輸入国の引受の商人）
    public Share InsurancePayoutFrac = new(0.5);   // 保険金＝封鎖損失(取扱高×(1−raid))×これを 商人基金→損をした荷主（各州の捌き手の商人・海路の取扱高の比）へ（基金が尽きれば部分払い＝ロイズの破綻）

    public double ColoniesForFullColonial = 3.0;  // 植民地の数がこれで満額になる目盛り（入手率ではない・読み手は Colonial.cs の本国の不安の重みと SelfPlay.cs の植民地防衛の重み）
    public double WhitePeaceScore = 0.25;         // 戦勝点がこれ未満の講和は白紙（得るものなし）
    public double ForcedPeaceBlankOverCollapse = 0;   // AI とプレイヤーで同じ経路
    public double ColonyCaptureBudget = 100;       // 完全勝利(戦勝点1)の奪取予算＝100点（budget ＝ 戦勝点 × これ）
    public double ColonyCaptureCostBase = 30;      // 植民地1つの基本奪取コスト（未開発・無資源でも30点）
    public double ColonyCaptureResourceCost = 10;  // 固有資源1つあたりの追加奪取コスト（資源を生む植民地ほど奪いにくい）
    public double ColonyCapturePopCost = 2;        // 人口規模1あたりの追加奪取コスト（人口の多い植民地＝印度ほど奪いにくい＝大勝が必要）
    public int ColonyCaptureMax = 3;               // 1戦争で奪える植民地の上限（完全勝利でも3つほど）
    public double ColonyCaptureDistanceWeight = 1.0;   // 戦勝奪取の距離の重み＝奪取コスト×(1＋これ×海路距離万km×(1−制海権))
    public double ExpansionWillBourgeois = 0.4;        // 従来は戦争熱>0.5超過分のみ＝自由主義的な英仏が構造的に植民地化不能だった
    public double ColonizeWillSuperiorityDiscount = 1.0;   // 1.0＝ちょうど屈服させられる相手で上限の50%（弱小へは半分で行ける）
    public double ColonizeValueGdpPer100k = 1.0;   // AI標的の魅力に経済規模項＝GDP10万あたり+1（植民地化の目的＝市場・税基盤。旧＝植民地数のみで実国のインド諸邦より近場の抽象小勢力が常に勝った）
    public double TerritoryCessionScore = 1.2;     // |未クランプ戦勝点| がこれを超える圧勝（崩壊1.5寄り）で敗者の非首都州を1つ割譲（アルザス・ロレーヌ規模＝稀に・レジームチェンジ閾値1.0より厳しい）
    public double CessionSeaCrossingWeight = 1.0;   // 点火2026-08-01（上陸ゲートと同時）：渡海の摩擦
    public double CessionMaxDistanceKm = 4000;     // 圏外は割譲不可＝賠償のみ（実走で清がボストン/リガ獲得＝地理無制約の是正。全拠点アンカーは連鎖征服で無効化されたため首都のみ）
    public double CessionUnified = 0;               // 0＝今の 4 経路（ColonyList の奪取・植民地格の州の複数割譲・境界改定・戦利品）の候補と費用と予算のまま・1 ビットも変わらない
    public FractionPerQuarter DefeatShockDecayPerQuarter = FractionPerQuarter.FromQuarter(0.15);         // 敗戦ショックと戦勝ショックの四半期の減る割合（残る分は 1−これ＝0.85・1.0から数年で消える＝連勝暴走の防壁。厭戦・反覇権合従が他の防壁）
    public double DefeatShockLegitWeight = 0.075;  // 敗戦ショック→正統性の低下幅（leg −= これ×DefeatShock）
    public double CedeRuleToLocal = 1;   // 0＝割譲は支配の区分と外国持分を触らない（今のまま）
    public double CededColonyMilitancy = 0.3;      // 移転州（被征服地）の pop に上乗せする不満（征服への反発＝高すぎると即分離）
    public double RevanchismTension = 70.0;        // 復讐心：領土を奪われた国が勝者へ抱く恒久的遺恨（緊張上乗せ・ライバル閾値40超で失地回復戦を駆動）
    public double ConcertReparationRestraint = 0.04;  // 会議外交＝ウィーン体制（傍観する非交戦列強が勝者の賠償を削る＝サンステファノ→ベルリン1878）
    public double ConcertRestraintMax = 0.5;         // 抑制の上限（会議度が高くても賠償の半分まで）
    public double ThreatTensionPerMonth = 0.10;       // 経済/植民地の大国であること自体が周囲の緊張を高める（覇権防止）
    public double RouteDependenceTensionPerMonth = 0.10;   // 点火2026-09-03（B-02・単独A/B batch10・値は設計目安どおり ThreatTensionPerMonth と同桁＝新しい数字を作らない）
    public double IdeologyTensionPerMonth = 0.22;     // 異なる政体間の緊張（イデオロギー距離1あたり・月）
    public double IdeologyAffinityRelief = 0.30;  // 同じ政体間の緊張低下（月）
    public double EthnicTensionPerMonth = 0.06;       // 異民族グループ間の緊張（汎スラヴ/汎ゲルマン＝同グループは親近・異グループは反目）
    public double EthnicAffinityRelief = 0.12;    // 同民族グループ間の緊張低下（月・同一民族はさらに強い）
    public double LawTensionPerMonth = 0.05;          // 法のリベラル度(World.Freedom 0..1)の差による緊張（自由主義 vs 反動＝1848型）
    public double ReligionTensionPerMonth = 0.03;     // 宗教の隔たりによる緊張（1789-1880は宗教戦争後＝小重み）
    public double ReligionHeresyBonus = 0.22;     // 同ファミリー異宗派（スンナ⇔シーア・カトリック⇔プロテスタント）の摩擦加算＝「異端は異教より揉める（小さな差異のナルシシズム）」
    public double ReligionAffinityRelief = 0.06;  // 同一宗教間の緊張低下（月）
    public double ProtectorTensionPerMonth = 0.02;  // 休眠側の設計値0.02目安＝EthnicTensionPerMonth 0.06/ReligionTensionPerMonth 0.03 の弟分
    public double ProtectorInterventionWeight = 0.5;  // 休眠側の設計値0.5目安
    public double VassalTributeMonthGdpPerQuarter = 0.015;      // 属国/傀儡の貢納＝従属の月の GDP×これを毎四半期 宗主国庫へ（四半期ごとに月 GDP の 1.5%＝年 GDP の 0.5%・運河通行料の両建てを流用・保存）
    public double DependencyAlignmentRelief = 1.2; // 従属-宗主間の緊張を毎月これだけ下げる＝整列（従属は宗主に開戦しない）
    public double VassalHoldScale = 0.5;          // 属国の抑え＝宗主の陸軍力×投射(距離減衰)×これ
    public double VassalDefectionMilitancy = 0.6;  // 離反の意志ゲート＝属国民の平均闘争度がこれを超え、かつ軍事条件（VassalHoldScale）を満たすと従属を脱する
    public double VassalizeWinScore = 1.4;        // この戦勝点(未クランプ)以上の壊滅的勝利なら、領土割譲(1.2)の代わりに敗者を属国化＝国ごと従属(金銭貢納)
    public double DependentThreatWeight = 0.3;    // 宗主の覇権脅威評価に従属国のGDP/植民地をこの率で合算（属国の帝国は大きく見える＝反覇権連合を呼ぶ）
    public double DependencyCycleBreak = 1;      // 0＝旧（自分自身だけを拒む）
    public int MaxDependents = 8;                // 1宗主が抱えられる従属の上限（帝国の過剰拡張＝過伸を防ぐ）
    public double GovCostVassalThreshold = 0.4;   // 講和の圧勝後：敗者の統治コスト（民族距離0.5＋宗教距離0.5）がこれ以上＝異民族/異教で直接統治が高コスト→属国化（間接統治）／未満＝同族で安く併呑→追加割譲
    public double AllianceBreakTension = 50;      // 同盟国間の緊張がこれを超えると同盟が崩れる（強国は孤立へ）
    public RatePerMonth WarCasualtyRatePerMonth = new(0.002 / Military.WorkingMenStdShare);   // 戦死（上限前）＝兵士POP×（接触÷自軍の Military）×これ/月＝自軍のうち実際にぶつかった兵の、月の死亡率（無次元）
    public RatePerMonth WarCasualtyCapPerMonth = new(0.04);   // Rate 0.002 で上限に張り付く月は 0.0%（seed1/2/3）＝錨には効いていない（上限が効き始める Rate の p10 は 0.08〜0.09）
    public Share OfficerCasualtyShare = new(0.06);    // B-1：戦死の一部が士官＝伝統支配層(貴族)にも及ぶ割合＝WarFeverを煽る貴族も血を流す（士官は少数なので小・上限は貴族の5%/月）

}
