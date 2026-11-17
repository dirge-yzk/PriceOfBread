using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === スカフォールド完成 2026-07-03（点火済み）===
    public double FoodOutputAnchorProduction = 1.0;   // 扶養力の食料産出アンカーを消費ベースから生産ベースへ寄せる割合（0＝現行と厳密一致）

    // === 政治資本 2026-07-09（会社PC）→ 2026-09-21 裁定S1で「在庫の門1本」へ（㉖で焼き込み）===
    // ==== 税・支出・軍事費の段（作者裁定2026-09-12 R1/R2・2026-09-20 点火＝主フラグ FiscalLawStages を削除）====
    public double MandateWeight = 0.3;          // 在庫 b の目盛り（上の注記）＝b=これ×MandateSpendRate×(正統性+支持)
    public double MandateSpendRate = 0.5;       // 押し切った一歩（純支持<0）の政治資本消費＝|純支持|×これを MandateSpent へ累積
    public double MandateRefillQuarters = 40;    // 実行度 ≥ 1/(1+2.667)≈0.273 なので最悪でも ≈147四半期＝0の吸収状態を作らない
    public double MandateOverStockLegitPower = 2;   // Government.Spend は S へ足す量を残りの在庫 max(0, b−S) で止める＝在庫を使い切る（S は b で止まる）

    // === 王権議会P0＝内部状態化 2026-07-10（出典＝設計doc「王権議会_史実精査」§3新規1-6/§4結論/§4-b協力度2次元/§5判断③・「設計メモ」§2段階(i)）===
    public double CoopPublicSatPivot = 0.14;   // 協力度③世論↔体制（CoopPublicRegime）の満足項の中立点
    public double CoopClassAffinityWeight = 0.5;      // 協力度（議会↔王権）の階級親和項の重み：王権の基盤=旧支配層（史実精査§5判断②）と議席の構成の距離＝Σ議席×(保守+1/自由0/社会−1)（2026-09-26 回5-1 Q16＝旧は第一党の札1つ）・王冠の無い国は0
    public double CoopPolicyGapWeight = 1.0;          // Policy の目標
    public double CoopConflictMemoryWeight = 1.0;     // 協力度（議会↔王権）の紛争記憶項の重み：既存 MandateSpent ストック（反対を押し切った累積＝敵対の蓄積器・史実精査§4-b「新規状態を作らず参照」）をそのまま減算
    public double ArmyMilitarismAffinity = 0.25;      // 将校の任免権の所在∈[−1,1]×これ＝軍帰属の向き（＋君主が任免／−内閣が任免／0軍が自分の主）
    // --- 王権議会P1＝動学6フック（2026-07-10・P0の観測軸を動学化・点火済み。値は各行。P1-F は 2026-09-20 に削除＝残り5本）---
    public double SummonParliamentRate = 0.02;      // P1-H 破産召集（1789三部会）：緊縮(Austerity>0.5)の深さ×これ/四半期でParliamentControl↑
    public double SummonByInterestShare = 1;        // 0＝今の式（緊縮＞0.5 の深さで Clamp01 の 1.0 まで）
    public double GrantedControlOnReform = 0.3;    // P1-A 下賜（明治1889欽定）：上からの改革の遷移時にParliamentControlの床をこの値へ（弱い議会を与える）
    public double ParliamentBudgetVeto = 2.0;      // P1-C 予算闘争（憲法紛争1862）：所得税スライダーの抵抗に 統制力×max(0,−協力度)×これ を加算＝非協力議会は予算を通さない
    public double VictoryLegitBoost = 0.05;         // P1-D 勝利の免責（ビスマルク型）：決定的勝利で正統性ストック+これ×戦勝点＝敗戦ショックの対称
    // --- 王権議会F1＝州一揆（2026-07-10・点火済み）---
    public double RegionalRevoltThreshold = 0.50;   // 発火閾値：州の熱（＝州平均闘争度）がこれ超で一揆（0=無効）
    public double RegionalRevoltStrength = 0.5;    // 蜂起の勢い係数（熱×州大衆規模×これ vs coerce＝Transitionのchallengeと同じ物差し）
    public Share RevoltSuppressDeadFrac = new(0.002);  // 鎮圧の戦死＝州大衆の0.2%（1848年級。貯蓄は遺族に残る＝貨幣保存非接触・人口台帳に計上）
    public double RevoltSuppressLegitCost = 0.05;  // 鎮圧の正統性コスト（ストックへ直接＝流血の記憶。regen0.1で数年かけ回復）
    public double RevoltConcessionSpend = 0.125;   // 内戦の講和で要求をのませる（Politics.ConcedeToFaction）ときの政治資本消費（旧＝余白押し切り1歩と同額＝負のマンデート）
    // --- 創発的国内勢力F3＝牙と変異（2026-07-12・F3a=永続化/変異は即時・F3b=内乱化は常に有効〔旧の旗 FactionRevoltScale は (739) で焼き込み〕）---
    public double FactionCohesionGrowth = 0.05;    // 結束の四半期成長率×(0.5+ブロック意識)（変異済みは2倍）
    public double FactionCohesionDecay = 0.10;     // 不在クラスタの結束減衰（四半期あたり・結束が 0.05 未満になった四半期に忘却＝(749) c-11 で連続不在の時計を外した）
    public double FactionMutationRate = 0.002;      // 変異の受理率×危機（州の飢え/敗戦ショック）
    public double FactionMutationCrisisFloor = 0.5;   // 変異ロールの危機下限（平時は絶対に変異しない）
    public double MutantCohesionFloor = 0.5;       // 変異時の結束の床（旗の下の凝集）
    public double RebelArmsMarketEnabled = 0.0;   // 国軍と同じ市場で同じ財を買う（首都州の運動は国庫と同じ配給を奪い合う）
    public double FactionCohesionFightThreshold = 0.8;   // 内乱化に要る結束（≈持続3年超の組織運動のみ＝一揆や瞬間革命と役割分担）
    public double RevoltIgnitionIntensity = 0.0;   // 戦争の形の再併合も旧経路の鎮圧の行（Politics.SettleRevolt の1本＝返還州の闘争度の緩和・断裂の窓・運動の燃え尽き）を常に通る
    public double RevoltGrievanceWeight = 0.0;   // 門2の怨嗟の重み：min(1, FactionRejectionMutation×Rejections)（変異ロールと同じ飽和の書き方）を州の絶望に加える強さ
    public double RevoltVoiceFloor = 0.0;   // 蜂起の強度の発言権の床：(1−発言権) の下限をこの値にする。0＝恒等
    public double RevoltMilitiaRaise = 0.0;   // ＝民兵率 min(1, これ×そのPOPの闘争度)
    public double MilitiaCombatValue = 0.0;   // 民兵1人あたりの戦力 a（<1＝弱兵）
    public int FactionConcessionSteps = 2;       // 内戦の講和の経済決着（Politics.ConcedeToFaction）：要求最上位の法を2段・以下1段のむ
    public double FactionConcessionSlider = 0.15;  // 経済決着：スライダー軸の強制移動量
    public double LoyalistMilitiaWeight = 0.5;     // 湘軍項：反乱と思想距離が遠いブロックが体制側で郷勇を組む重み（F3bで0.5から掃引）
    public double LoyalistDistanceFloor = 0.5;     // 湘軍が立つ最小の思想距離（近い反乱＝フロンド型には立たない）
    public double LoyalistHeresyBoost = 1.0;       // 異端（変異）反乱への郷勇の増し（曽国藩の檄）
    // --- F4 政党統一（2026-07-12・点火済み＝DynamicParties=1）---
    public double PartyFormCohesion = 0.3;         // 政党化に要るクラスタの結束（蜂起の0.8より低い＝議会経路は組織の敷居が低い）
    // --- P3b 連続動学（2026-07-12・点火済み＝遷移グラフの置換素子。出口はP2の床交差再ラベル）---
    public double ParliamentPressureRate = 0.04;    // 議会圧力：max(0, 代議制支持−現政体支持)×これ/四半期で統制力が育つ（漸進の立憲化＝グラフのジャンプの置換）
    public double ShogunateEndTension = 40.0;     // 幕府が倒れる外圧＝対外緊張（黒船）
    public double ShogunateEndMilitancy = 0.5;    // 幕府が倒れる内圧＝大衆の闘争度（同ゲートの内圧(c)と同水準）
    // --- 政体形態の実績ストック（信認⇔失望・経路依存・2026-07-13・設計doc「形態別失望ストック」）---
    public double RegimeRecordAccumRate = 0.02;    // CoopPublicRegime（満足−闘争）×これ/四半期で現政体系統の実績が符号のまま積む（+信認/−失望）
    public double RegimeRecordSuccessTenureGate = 20.0;  // 正の信認が満額になる在任年数（長期成功のみ信認へ寄る＝始まったばかりの形態は実績を持てない）
    public double RegimeRecordDecay = 0.00346;     // 実績の四半期減衰率（半減期50年＝2世代・ユーザー指定）
    public double RegimeTrackRecordWeight = 0.3;   // 主フラグ：Transition の各政体支持へ符号付き乗数 (1+これ×実績) を掛ける＝信認された形態は選ばれ/粘り、失望された形態は避けられる
    // --- 国民意識（市民的結束＝民主主義の前提・2026-07-13・設計doc「国民意識」）。蓄積は書くだけ（実値既定） ---
    public double NationBuildConscript = 0.5 / Military.WorkingMenStdShare;      // 徴兵（兵士POP比）の重み＝国民軍の国民形成（ウェーバー「軍は国民の学校」）
    public double RedefinedMandateFloor = 0.6;      // ①正統性の下限＝立憲君主×王冠保持×高NC×高天命の時のみ Legitimacy≥これ×Mandate（加算でなく床＝二重計上なし）
    public double RedefinedMandateNcMin = 0.5;      // 高NCの閾値（信任帝政の資格）
    public double RedefinedMandateMandateMin = 0.6; // 高天命の閾値（⚠緩いと立憲固着＋仏が共和に届かない＝難しく較正）
    public double UnificationV1Only = 1.0;   // 低満足popが嗜好品アヘンを消費→州の依存ストック→労働/満足ペナルティ＋対外流出→輸入禁止の法→輸出国と緊張→戦争は既存機構
    public Share UnificationNatioShare = new(0.25);     // ナショナリズムの国民シェア閾値（Ideologies.National[4]＝エリートの熱が統一の燃料）
    public double UnificationAiPursueChance = 1.0;  // AI追求確率＝(1188) 1.0＝候補と標的が立てば必ず使う（旧 0.7＝AI だけの 30% の見送りは根拠なし・作者「しばらくの間、全て推奨で」で推奨を採った）。系列と係数の物理の削除は波119〜
    public int UnificationAiCooldownYears = 20;     // 見送り後の再判定クールダウン（年）
    public int UnificationAiWarGapYears = 0;        // 宣戦後の次判定間隔（年）＝(1188) 0＝待たない（旧 3＝根拠の無い待ち・クールダウンを作らない 2026-09-06）
    public double OpiumMiseryScale = 2.0;        // 素のアヘン需要の困窮ゲート＝baseNeed×clamp(これ×(1−満足),0,1)
    public double OpiumLaborPenalty = 1.0;       // 依存が労働供給を蝕む＝成人労働×(1−これ×中毒度)（床0.5）
    public double OpiumBanTensionPerMonth = 2.5;     // 禁令中の輸出国との緊張蓄積/月（取締り・没収が敵意を積む＝林則徐→アヘン戦争。開戦閾値160へ数年で到達する規模）
    public double OpiumBanThreshold = 0.05;      // AIが禁令を出す全国平均中毒度の閾値（解除は×0.3のヒステリシス）
    public double DynMandateSatWeight = 0.6;    // perf: 満足重み×(未教育層加重の平均満足−中立点)÷(1−中立点)（第5段5-3＝天命の母数を未教育層へ再定義）
    public double DynMandateSatPivot = 0.16;   // 天命の満足項の中立点（これ未満で天命を削る）
    public double DynMandateFoodWeight = 1.0;   // perf: 食料重み×min(0, FoodFill−アンカー)
    public double DynMandateFoodAnchor = 0.8;   // 食料アンカー＝飢饉閾値基準（プローブ検証済み：1.0基準は慢性輸入国=英を常時罰し終盤BD化→0.8で解消。罰すべきは飢饉であって輸入依存ではない）
    public double DynMandateWarWeight = 0.4;    // perf: −戦争重み×WarExhaustion
    public double DynMandateEmaRate = 0.08;     // EMA率/四半期（時定数≈3年）
    public double MandateReligiousCircuit = 1.0;   // 開度=国教法（無神論=0）・容量=聖職者/(文盲×ClergyMediationPerCapita)飽和
    public double ClergyMediationPerCapita = 0.005;   // 媒介飽和の基準＝文盲人口の0.5%の聖職者で容量1.0（仏1789実測≈1%＝開始時は各国飽和・世俗化/無神論で痩せて初めて効く）
    public double DynMandateAmplify = 0.8;      // 効果幅：侵食×(1−0.8e)・自然回復×(1+0.8e)（e∈[−1,1]）
    public double NationBuildAccumRate = 0.03;         // v2蓄積率
    public double NationBuildDecayRate = 0.0025;       // v2減衰/四半期（1%/年起点・較正帯1〜2%/年。原案0.5%/年は半減期139年で弱すぎ）
    public double NationBuildCrisisDefense = 0.5;      // 危機乗数：交戦中（防衛戦争の代理・速度のみ＝天井は動かさない・無償の加算項は作らない）
    public double NationBuildCrisisCivilWar = 1.0;     // 危機乗数：革命/内戦
    public double NationBuildCrisisIndependence = 2.0; // 危機乗数：独立戦争（分離で生まれた国の初期戦争＝レヴェ・アン・マス）
    public double NationBuildInitialDefault = 0.075;   // v2初期値の既定（前国民的世界0.05〜0.1帯。「買ってないのに持ってる」一律0.3を廃止）
    public double RadioNationWeight = 0.5;             // E3：ラジオ項の重み＝size加重RadioFill×これ
    public double NationConsciousnessDerived = 0.0;   // >0 で、上の蓄積漸化式 NC ← NC(1−decay)+drive×(天井−NC) をその不動点 NC* = 天井×drive/(decay+drive) に置き換える
    public double NationConsciousnessPerPop = 1.0;      // 国の国民意識＝POPのsize加重平均（0＝旧スカラー・厳密恒等）
    public double NationConsciousnessReachDecay = 800.0; // 到達が半分になる首都からの距離km
    public double OpenBallotReality = 0.9;             // 議会政体×公開投票＝選挙は実在するが買収/脅迫で歪む（英1872以前）
    public double PaperFranchiseReality = 0.7;         // 非議会政体（専制等）＝紙の参政権＝選挙の実在なし
    public double WomenEduLitBonus = 0.25;             // 女性の権利が教育権の段以上で femaleLit ×(1+これ)（上限=全体識字）＝女子就学が出生転換を早める
    public double InnerFreedomTithe = 0.6;             // 内心の自由での十分の一税倍率（教会の税権が制限される）
    public double ReligiousFreedomTithe = 0.3;         // 信仰の自由での十分の一税倍率（さらに制限＝政教分離）
    public double CartelBanRent = 0.6;                 // 談合禁止での独占レント倍率（私的談合を禁じるが国家の独占は残る＝中間）
    public double TrustbustRent = 0.15;                // 独占の解体での独占レント倍率（トラスト解体＝ほぼ競争的）
    public double RationingBlockadeRelief = 0.5;       // 配給ありでの封鎖厭戦倍率（公平配給で飢餓の痛みが半減＝英国型）
    public Share NationalizationStateShare = new(1.0);     // 国有化部門の国営持分
    public double NationalizationSeizureRate = 0.05;   // 月の接収の速さ（Seize の take＝これ×操業規模）。旧経路〈0＝stateFrac の下限〉は (1132) で消した
    public double SeizureResistanceWeight = 1.0;       // 抵抗＝これ×資本家ブロックの政治力（InterestGroups.Blocs.Modern）×奪われる持分
    public double SeizureInvestmentChill = 1.0;        // 資本逃避＝Region.SeizureChill＋＝これ×奪った持分（私的投資予算に ×(1−chill)）
    public double SeizureChillDecay = 0.1;             // 資本逃避の月次減衰（chill ×= 1−これ）
    public double SoeSaleMilitancyCeil = 0.0;   // 量は既存の Pop.Militancy（闘争度0..1）＝他の「政府への不満」の読み手と同じ物差し（新しい不満の定義を作らない）
    public int StateTargetMax = 99;                    // 国営工場カウンターの上限（作者仕様＝0〜99）
    // ==== 参政権の範囲2法（作者裁定2026-08-16・docs/202608/20260815_失業プールと社会移動_部分完了.md 末尾「参政権の範囲2法」節）====


    // ==== AI第1波 2026-08-16（設計doc「AI動詞被覆監査」第1波「AIに使わせる・機構不接触」・実装記録「AI第1波実装」）====

}
