using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients   // 全体のチューニング係数（§9-A/§9-D）
{
    // --- 市場（§9-A）---
    public double PriceAdjustSpeed = 0.13;   // 価格の積分項：在庫ギャップ駆動（§9-A 仕組み1）
    public double FlowAdjustSpeed = 0.48;    // 価格の比例項：需給差駆動＝振動の減衰（設計判断A）
    public double PriceElasticity = 0.5;     // 段B：日次価格探索の需給の価格弾力性
    public double PriceAmpLnScale = 2.9957;  // amp曲線の端の対数距離（=ln20）
    public double PriceAmpCenter = 0.7;      // 100%付近の調整倍率（<1＝張り付かない範囲は今までより静か）
    public double PriceAmpEdge = 8.0;        // 床5%/天井2000%での調整倍率（≫1＝極端な乖離は直ちに是正）
    public double PriceAmpPower = 3.0;       // 増幅の凸度（高いほど中間は平坦・端だけ急峻＝加速度的）
    public double SupplyAdjustSpeed = 0.15;  // 供給能力調整の速さ（§9-A 仕組み4）
    public double SupplySlope = 0.5;         // 供給曲線の傾き（オプションB）：価格が mc×(1+これ) で能力上限
    public Share ReignitionDemandFrac = new(0.03);  // 入力需要の再点火トリクル＝採算が合えば「作りたい量」のこの割合を最低需要に（産出の床に代わるデッドロック防止。死んだ工場も需要を残し価格で蘇生＝無から有なし）
    public double PriceFactorMin = 0.4;      // 価格弾力の下クランプ
    public double PriceFactorMax = 1.4;      // 価格弾力の上クランプ
    public double CrossElasticity = 0.6;     // 主食間の代替の強さ（相対価格の指数）
    public double PriceFloorFactor = 0.01;    // 暴落の底（安全網）
    public double PriceCeilFactor = 100.0;    // 暴騰の天井（安全網）
    public double SoftBandSharpness = 8.0;    // 価格安全網(SoftBand)の対数空間飽和の鋭さ（→∞でハードクランプ。旧 MarketFlow関数内 const 8.0 を係数化＝sweep対象）
    public double TargetStockBuffer = 1.0;   // 在庫目標 ＝ 総需要 × これ（≒保有したい在庫＝数期分の需要）
    // === テーマH（産業組織・固定資本・住宅）===
    public double InventoryAdjustGain = 0.1;     // メッツラー在庫循環＝在庫についての期待
    public double DesiredInventoryCover = 1.0;   // 望ましい在庫＝これ×LastDemand（TargetStockBufferと一致＝定常でギャップ0）
    public double TargetStockFloor = 1.0;    // 在庫目標の下限（需要≈0でのゼロ除算回避）＝「量として意味を持たない」の既存の物差し（下の幻の生産のスナップもこの実用スケールを流用する）
    public int ClearingsPerYear => Calendar.MonthsPerYear;   // 国際清算の年あたり回数（年換算の単位ヘルパー）
    public int MonthsPerClearing => 1;                    // 1清算が覆う月数（月割の単位ヘルパー）
    // --- 企業の操業規模（参入退出。採算で伸縮し供給を需要へ寄せる＝価格二極化の是正・§9-A 仕組み4）---
    public double ScaleAdjustSpeed = 0.05;   // 操業規模が採算の目標へ寄る速さ（緩やか＝供給の振動を防ぐ）
    public double ScaleFloor = 0.1;          // 採算が悪い工場の最小規模（≒閉鎖。0でなく＝需要回復で再開できる）
    public double ScaleMax = 3.0;            // 採算が良い工場の最大規模（拡張＝希少財へ労働を集める）
    public bool FactoryBankruptcyEnabled = true;   // 倒産機構（恐慌カスケードの核）
    public double FactoryMarginSmoothing = 0.025;  // 採算EMAの平滑係数（小＝長い移動平均＝一過性の赤字では潰れない。0.025≒3年＝持続的危機のみ倒産）
    public double FactoryBankruptcyMargin = -0.85; // 平滑採算がこれ未満で倒産（原価の15%以下で売り続け＝極度の慢性赤字のみ。死のスパイラル防止）
    public double FactoryReentryMargin = -0.05;    // 倒産中、現採算がこれ超で新規参入（やや赤字でも再開＝恐慌からの早い回復）
    public double FactoryReentryScale = 0.3;       // 新規参入時の操業規模（小さく入って採算が良ければ拡張）

    // --- pop 意思決定（§9-D・段1）---
    public double Mobility = 0.02;            // 階級間の再配分速度（緩和）
    public double MigrationTemperature = 0.15; // softmax 温度（低温＝ほぼ最善へ）

    // --- 技術・教育（§2.8／§9-H・段2）---
    public double AdoptionCeiling = 0.5;          // 暗黙の世界水準＝孤立国でも基礎技術はここまで採用できる（単一国の採用上限）
    public double FrontierCeiling = 6.0;          // 技術の安全上限（革新は逓減するので実際は届かない＝張り付き防止）
    public double EliteForFullTech = 0.5;         // 基礎採用/革新に要るエリート水準（厳しい＝乏しい社会は失敗型）
    public double TechGrowthRate = 0.015;         // 基礎採用（暗黙の世界水準まで）の線形速さ＝単一国の挙動を温存
    public double EliteForCatchup = 0.25;         // 世界の技術水準からの後追いに要るエリート（緩い＝コピーは発明より易しい＝後発のブースト）
    public Share CatchupFraction = new(0.5);          // 後追い採用で届く先端比率＝永遠の二番手（先端に並ぶには自前の革新＝自由＋公教育が要る）
    public Share CatchupFractionAdvanced = new(0.97); // 近代化(FrontierCapable)後発国が後追いで届く先端比率（≒並んでから自前革新で追い越す）
    public double ScoreInertiaRate = 0.02;   // 序列に使う国力を在庫化する1四半期あたりの追従率
    public double CatchupAbsorptionBlend = 1.0;   // 二値ゲートは全球収束の主因（後追い0.13/四半期は前線前進≈0.006の約20倍＝資格国は必ず前線に張り付く）
    public double AdoptionRateAdvanced = 0.13;    // 近代化後発国の後追い速さ（基礎 AdoptionRate 0.06 より速い＝制度が整えば急速に追いつく）
    public double AdoptionRate = 0.06;            // 世界の技術水準後追いの速さ（ギャップ比例＝遅れているほど速い＝後発のブースト）
    public double InnovationRate = 0.012;         // 革新の速さ（自由＋公教育が世界の技術水準を押し上げる）
    public double InnovationScale = 3.0;          // 革新の逓減スケール（技術がこれを超えると伸びにくい＝先端の張り付き防止）
    public double FrontierFreedomThreshold = 0.6; // 世界の技術水準解禁（革新）の自由度閾値
    public double InnovationEliteSubstitute = 1.0; // 0=自由必須・>0で検閲下でも技術力の高い国（ドイツ帝国/明治日本）が革新可＝史実の「権威主義的近代化」

    // --- 制度（発見の表の制度の行の2値＝2026-10-01 (152)・設計 §1A-6）---
    public double PatentInnovationBonus = 1.5;    // 特許制度：技術成長 ×これ（革新を加速）
    public double JointStockInvestBonus = 1.6;    // 株式会社：工業化投資 ×これ（中産の資本が株で工業へ＝資本調達の拡大の proxy）
    public double EquityHeatEuphoria = 0.05;      // 株式バブル（金融改革②）：与信過熱(FinancialHeat 0..1.5)×これ を投資ハードルから減じる＝過熱→株高→投資ブーム→Confidence崩壊で恐慌
    public Share ResearcherFrac = new(0.0);           // インテリのうち研究者になる割合（0=休眠＝厳密恒等）
    public double UniversityProductivity = 2.0;   // 大学1単位の高等教育の産出（研究者1人あたり）
    public double HigherEduNeedPerEducated = 0.0; // 高等教育の必要量＝これ×前月EducationFill×識字×人数（0=需要ゼロ＝厳密恒等）
    public double PromotionGateHigherEdu = 0.0;   // 技師・研究者への昇格を高等教育の充足で絞る度合い（0=絞らない＝厳密恒等／1=買えていない分だけ資格が消える）
    public double ModernizationToHigherEdu = 0.0;   // 1=全額が人材（作者の意図する最終形）
    public int ScholarshipTopStage = 0;   // 教育費の軸（EducationSpending・4 段）の最上段の国だけが、高等教育を買う POP の自己負担を国庫の負担分だけ下げる（買い手の値段の楔＝健康保険 #3 の insure と同型・社会保障法の最上段が年金であるのと同じ型）
    public Share ModernizationToRecruit = new(0.05);   // 近代化予算のうち招聘へ回す割合
    public double RecruitCostPerHead = 100.0;      // 技師1人を呼ぶ基礎費用
    public double RecruitDestByVacancy = 1.0;      // (1270) 招聘の受入先：0＝機会が最大の 1 州／1（既定・2026-11-21 c130rc）＝州の高度労働力の足りない量（前の清算の LastShortage＝技師の空席）の比で配る（空席が無ければ呼ばない）
    public double RecruitPremiumScale = 0.5;       // 割増の物差し＝相手国の機会がこれだけ上回ると費用が2倍（お雇い外国人は大臣より高給）
    public RatePerQuarter RecruitMaxFracOfSource = new(0.02);   // 1四半期に1国から抜ける上限（送出側の規模比）

    // --- 政治（§9-E／§A-11・段3）---
    public double PoliticalReferencePop = 1000.0;     // 政体力学の基準人口（保持力・維持費の人口あたり強度の分母）
    public double ConsciousnessSpeed = 0.015;         // 意識の緩和速度（四半期）
    public double UrbanConsciousnessBoost = 1.5;      // 都市pop の意識加速
    public double NewsConsciousnessFloor = 0.3;       // 1934拡張E3：意識のpress項の自由分を「内容の自由×紙の実在」の積へ＝floor+(1−floor)×NewsFill
    public double LivingRefSpeed = 0.03;              // 参照点が今の生活水準へ寄る四半期速度（半減期≒6年＝「暮らしの慣れ」）
    public double MilitancySpeed = 0.30;              // 闘争度（ストック）の追従速度σ＝半減期5.8か月
    public int ElectionIntervalQuarters = 16;         // 議会選挙の間隔（四半期）
    public double NonElectiveSeatsEmpty = 1;          // 0＝非選挙の政体は毎期 議席＝得票の写し（今のまま）
    public double ParliamentSeatsWithoutGap = 1;      // 1＝開院の条件を思想の有無へ（空席の四半期を閉じる）・0＝旧（選挙の後 1 四半期から開院）。(1118) 2026-11-14 点火
    public Share PartyDominanceThreshold = new(0.6);      // 一党支配と見なす議席シェアの閾値（これ超で多元性が崩れ始める）
    public double DominanceLegitimacyErosion = 0.05;  // 一党支配が正統性を削る強さ（支配＝多元的正統性の喪失→権威主義へ漂流）
    public double DominanceInstabilitySatisfaction = 0.43;   // これ超の満足した国では
    public double ConstMonarchyErosion = 0.02;       // 立憲君主制：議会の反王党派（自由+社会の議席）が王冠の正統性を削る＝議会優勢→共和へ（君主の形骸化）
    public double ParliamentLawWeight = 1.2;          // 法案可決/スライダー律速に効く議席の重み（議席×党選好を NetSupport/OpposingPower に加算）
    public Share LandReformRentCap = new(0.3);            // 土地改革：RGO地代（地主取り分）の上限（地代>0.3の国を再分配。小作0.5も自作0.3へ）
    public Share SerfEmancipationRentCap = new(0.5);      // 農奴解放：地代の上限（農奴制1.0→小作0.5へ。自作/小作の国＝既に0.5以下には効かない＝農奴国だけ解放）
    public double CornLawImportPenalty = 1.0;         // 穀物法：主食の輸入閾値への上乗せ（高関税相当＝輸入を阻む→国内パン高騰）
    public double FamineDriveGain = 30.0;    // 飢餓の駆動の利得＝落ち込み D が起点を超えた分の傾き（導出は上＝1/(D の p95 中央値 − p90 中央値)）
    public double FamineDropOnset = 0.07;    // 飢餓の駆動の起点＝平常 FoodRef からの落ち込み D がこれを超えると駆動>0（導出は上＝観測5カ国の D の p90 の中央値 0.0687）
    public double WantWeight = 0.8;                   // 不満(1-充足)の闘争度寄与（満たされた階級は穏健＝改革派）
    public double PoliticalDistanceWeight = 0.25;     // 政治の空間モデル：意識×dist(POPの理想点, 現政体座標) の闘争度寄与（旧 VoiceWeight＝参政権ギャップの一般化＝4軸全体の乖離）
    public double DiscontentLawGap = 0.0;   // 読み手は Politics.Discontent 1箇所
    public double FiscalStressBufferMonths = 5.0;     // 国庫がこれを下回るほど逼迫度→1（旧＝絶対額50,000固定を53カ国へ一律適用＝小国が恒久破産モード・大国は無風の規模非中立）
    public double SuffrageConcession = 1.0;           // 革命脅威下でエリートが参政権拡大に譲歩する強さ（trad/modern の反対 −1 に threat×これを加算）
    public double ModernEliteSubCensusVoice = 0.3;    // 倍率排除②：制限選挙(Census)未満での近代エリート（ブルジョワ）の実効発言権
    public double IdeologyVoiceFloor = 0.7;           // 倍率排除②：国民イデオロギー集計で参政権ゲート外の層が持つ最低寄与（公共圏の機運）
    public double ReactionFromLoss = 0;   // （日本固有ではなく全世界＝識字を読む全ての量に効く）
    public double IdeologyAffinitySlope = 2.3;        // 倍率排除⑧：政体×思想の制度座標の距離→親和の傾き（親和=clamp(2−slope×距離,−1,2)）
    public double PolityCoordDriftWeight = 0.4;       // 倍率排除⑧段階2：現政体の参加軸を実際の参政権へドリフトする割合（0=固定/段階1・1=完全に実参政権）
    public double ReformPressureBoost = 0.5;          // 外圧が改革派の力を増す（明治の追い風 §A-11）※§8 導出：均衡分布（エリート天井ゲートで元値に復帰）
    public double LegitimacyErosionRate = 0.0175;     // 制度ギャップ1単位あたりの正統性侵食（四半期フロー）
    public double RepresentationThreshold = 0.6;      // この Freedom 以上の体制は代表的＝意識による正統性侵食を受けない（旧 authMonarchy 以外 erosion=0 の連続版）
    public double ReformMilitancyRelief = 0.5;        // 譲歩が鎮める係数（Politics の 3 か所＝譲歩で庶民の闘争度・内戦の決着で反乱者・州の譲歩）
    // --- 革命ダイナミクス（便乗・失脚・軍事クーデター。仏革命の再現＋専制の不安定化＋政体の多様化）---
    public double BandwagonSensitivity = 0.45;        // 便乗の勢い＝(改革派+急進派)/保持力×これ（0..1）
    public double BandwagonWeight = 0.32;             // 勢いある運動が無学な民の政治意識を引き上げる強さ（教育に依らず＝専制の安定を崩す）
    public double RevolutionTerror = 0.25;             // 旧R5(全失脚革命に適用)は◎−4で保留したがタスクBは的を絞り再挑戦：革命で失脚した旧支配層（Closed＝貴族聖職／ProletarianDictatorshipではModernEliteも）のPOPを
    public double CivilWarLandslideRatio = 1.8;       // 動員が治安維持力×これ未満＝拮抗→首都の蜂起（首都の分離独立＝戦争の形の内戦）／超＝圧勝で即フリップ
    public double SecessionPressureScale = 1.0;       // 分離圧力＝関税乖離(0..2)×州平均闘争度×これ
    public double CultureSecessionWeight = 1.0;       // 同一/未設定で0＝較正中立＝多民族帝国(墺洪/オスマン)の分裂を文化ラベルから創発
    public double SecessionThreshold = 1.43;          // 分離圧力(最大2)がこれ超＋中央の保持力を上回ると離脱（高め＝極端な乖離×極端な不満のみ＝稀）
    public double SecessionHoldPerPop = 0.000005;      // 中央の保持力＝治安維持力×正統性をこれ×人口で割る（大国ほど辺境を抑えにくい）
    public double SecessionTreasuryByMoney = 1;       // (1318) c137ak 2026-11-23 点火（腕 B の木）：1＝分離の建国の国庫の分与を移る州の貯蓄の和÷親の貯蓄の和で（どちらかの和が 0 以下なら 0）・0＝今（移る州の人口÷親の人口）

    // --- 画一的な政体遷移（イデオロギー駆動）。支持(政体)=国民イデオロギー集計·親和。引力点-現政体>保持力で遷移 ---
    public double PolityInertia = 0.4;           // 保持力＝PolityInertia×正統性
    public double RiotMilitancyThreshold = 0.5;  // 群衆が暴動＝革命的脅威になる闘争度の閾値（これ以下は蜂起しない＝平穏な抑圧は留まる）
    public double DesperationChallengeWeight = 1.0; // 閾値超の怒り（窮乏）が radical 挑戦力に寄与する強さ（意識非依存＝飢えた群衆そのもの）
    public double DesperationConsciousnessGate = 1.0;   // DesperationChallengeWeightの補償とセットで有効化
    public double FiscalDefaultPanic = 0.8;      // 財政破綻（連合を養えない＝債務不履行）が信用を崩す強さ＝経済悪化の段階（信用収縮→恐慌→不満→政治）
    // --- 国債（赤字の借金。国内＝資本家の余剰貯蓄でクラウディングアウト／海外＝開放度依存。焦げ付き→経済悪化、対外不履行→介入）---
    public Share BondAppetite = new(0.15);           // 資本家の余剰貯蓄のうち国債購入に回る割合（工場投資と食い合う＝クラウディングアウト）
    public Share ForeignBondCapacity = new(0.05);    // 海外債は対外資本流入（保存＝BaseMoney+CumulativeTradeFlow同時）
    public RatePerMonth BondRolloverFracPerMonth = new(0.01);       // 旧1.0=永久/浮動（金利変動が全ストックへ即時波及＝英3%コンソルの固定クーポンが再現不能）
    public RatePerYear BondBaseRatePerYear = new(0.04);           // 国債の基礎金利
    public double BondRateDebtScale = 0.018;     // 金利上昇＝×(債務/年名目GDP)
    public RatePerMonth BondRepayFracPerMonth = Calendar.PerYearToPerMonth(new RatePerYear(0.01));   // 月に国庫の余剰から返す国債の割合（減債基金）
    public RatioToGdpYear AusterityStartDebt = new(0.3);      // 債務÷年名目GDP がこれ超で緊縮開始（借金がかさむほど弱者救済を削り逆進課税を上げる＝赤字を貸し手に響かない形で閉じる負のフィードバック）
    public RatioToGdpYear AusterityRange = new(0.7);          // ここ幅で緊縮が0→満額（債務÷年名目GDP = Start+Range＝1.0 で弱者救済ゼロ・逆進課税が最大上乗せ）
    public double AusterityTaxHike = 0.5;        // 満額緊縮時に人頭税へ上乗せする逆進課税の幅（貴族免除の人頭税＝負担は弱者に集中）
    public RatePerMonth DevaluationCapFracPerMonth = new(0.05);     // 月に切り下げ（インフレ税）で吸える総貯蓄の上限割合
    public Share FiatIssueFrac = new(0.2);   // 不換シニョリッジ（fiat-expansion-as-haircut-not-issue）：赤字補填の切り下げのうち、popから取らず新規発行する割合＝マネーストック膨張(M↑→P↑)
    public RatioToGdpYear DebtDistressRatio = new(3.0);        // 対外信用（CapitalMarket の信用の債務の項＝1−債務÷年名目GDP÷これ）が尽きる債務の水準＝外資が逃げ、呼べなくなる（読み手は CapitalMarket だけ・国債の起債は読まない）
    public RatePerYear BondRateMaxPerYear = new(0.30);            // 国債金利の上限（年率）
    public double BondRateDistrustScale = 0.5;   // 信認の喪失＝直近の切り下げの速さ（この月の切り下げ×12÷年名目GDP）×これ を国債金利に上乗せ（2026-09-29・作者裁定＝(94) の申し送り1 の (i)）
    public double BondYieldFlightWeight = 1.5;   // 国債の利回りスプレッド(GovBondRatePerYear−BondBaseRatePerYear)が対外信用(CapitalMarket)を下げる強さ＝国債暴落→資本逃避→為替減価→さらに金利↑のバーリング危機ループ
}
