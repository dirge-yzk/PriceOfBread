using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === POPの個人債務（設計＝docs/202608/20260802_POPの個人債務_部分完了.md・2026-08-02）===
    public double PopDebtDriven;                 // 主フラグ（0＝休眠＝誰も読み書きしない厳密恒等）
    public double PopDebtLossShare;              // 入口A：backstop で消えた赤字のうち個人が背負う割合（有限責任の度合い）
    public double DebtRepayFrac;                 // 出口：黒字のうち毎月返済に回す割合
    public double DebtRuinRatio;                 // 返済不能：債務が年所得のこの倍数を超えると階級転落の判定へ算入（目安 2.0〜3.0）
    public double HouseholdCreditMonths;         // 入口B：借入枠＝月次所得のこの倍数（目安 1.0〜2.0）。0＝転落に算入しない

    public double ClergyAttractThreshold = 2.5;  // 聖職者の魅力閾値（平均比の豊かさ）
    public RatePerQuarter ClergyJoinRatePerQuarter = new(0.004);        // 下層→聖職者の四半期移動率（宗教学校の社会のみ）
    public RatePerQuarter ClergyExitRatePerQuarter = new(0.004);        // 0で退出なし
    // --- 国家間移民（豊かさの引力で大衆が移る・ゼロサム＝人はゲームの外から来ない。回1-2・2026-09-27 に世界プール WorldMigrationPoolFrac を削除）---
    public double EmigrationRate = 0.004;        // 機会差1.0あたりの四半期流出率
    public RatePerQuarter EmigrationMaxPerQuarter = new(0.003);     // 四半期の流出上限（人口割合）＝大量流出の暴走防止
    public double RegionalDistanceHalf = 4000.0; // 表示専用（地図の交易重力円）・力学には効かない
    public double FiscalCapacitySmoothing = 0.15; // 裁量余剰(FiscalCapacityPerMonth)EMAの平滑速度（豊作/恐慌の振れをならす）
    public double GdpTrendSmoothing = 0.18;       // トレンドGDP(GdpTrend)EMAの平滑速度（旧 World直書き0.18。GdpDrawdown/恐慌予兆の基準）
    public double InflationExpectationSmoothing = 0.34; // 期待インフレ(ExpectedInflationPerYear)EMAの平滑速度（旧 World直書き0.34）
    public Share AdminBudgetFrac = new(0.5);          // 行政＝歳入シェア（行政費スライダー×これ×歳入）
    public double MilitaryCadrePayRatio = 1.0;   // 旧の式〔0＝固定額を割り当てる〕は物理削除
    public Share WelfareRevenueShare = new(0.8);      // 福祉＝歳入シェア（社会保障スライダー×これ×歳入が給付の上限）
    public double StateMonopolyMargin = 0.30;   // 旧＝App の GameViewModel.Production.cs の TaxOnRate=0.30（チェックした材に乗る唯一の率）
    public double StateMonopolyLeakMax = 0.6;   // 仏の総徴税請負（塩税・aides・煙草）約2億リーヴル÷課された財の消費 約7億＝約28%＝据え置き
    public Share TitheRate = new(0.05);              // 十分の一税（教会税）：大衆（非特権・非兵士）の所得のこの割合を聖職者が受け取る（pop→聖職者の移転＝保存）
    public double HealthFillCapacityBonus = 0.05; // 医療（医者の役務）の消費が人口扶養力に与える上乗せ（小さく・創発の味付け）
    public double WelfareCostRatePerMonth = 0.6;         // 社会保障1.0・大衆1単位あたりの月次給付（国庫→大衆）
    public double LaborFatigueExp = 0.37;         // 労働時間→工場出力の疲労逓減指数：whOut=(hours/14)^exp
    public double LaborFatiguePenalty = 0.03;     // 0.03で14h=×0.94＝実効産出0.940<12hの0.944＝12hが産出最適・無制限は好況でも損（賞金消失）
    public double OverworkHealthSlope = 0.015;    // 労働時間→扶養力：12時間（過重労働の基準）より短い1時間ごとに健康改善で扶養力↑（8h=+0.06・10h=+0.03・12h以上=±0＝旧テーブルと一致）
    public double UnionMobilization = 0.15;        // B-5：容認組合の政治動員＝組織労働が改革派(reformist)を押し上げる（一方的な安定化でなく体制への改革圧力。1889ドック・スト型）

    // --- メリトクラシー・昇格（教育2段階の社会的上昇）---
    public double MeritGroupWeight = 0.8;        // 近代的利益団体の勢力割合がメリトクラシー値に効く重み
    public RatePerQuarter PromotionSpeedPerQuarter = new(0.04);         // 教育済みPOPが上の階級へ昇る速さ（M×識字×これ）
    public Share ReligiousProfessionShare = new(0.05); // 宗教教育下の技師・教師・医者への昇格係数（神学校は技師をほぼ再生産できない＝世俗水路。官僚は例外）
    public double PromotionPremium = 1.5;        // 転職に要る給与倍率（機会費用＋資格取得の壁＝現状維持バイアス。雇用枠は廃止＝職の需給は給与が表現する）
    public RatePerQuarter ArtisanDeclineRatePerQuarter = new(0.01);     // 駆動は昇格①と同じ所得差の言語
    public RatePerQuarter JobSearchRatePerQuarter = new(0.025);         // 同階級のもっともマシな職へ資格（識字×高等教育）の按分で転職し、駄目なら1段下の最良職へ降格
    public double JobPoolTemperature = 0.15;     // 値はMigrationTemperature同値
    // --- 社会移動リアリティ監査 2026-08-15（docs/202608/20260815_社会移動リアリティ監査_部分完了.md・点火状況は各行参照・点火は1本1バッチのA/B）---
    public double MerchantArtisanCapitalRise = 1.0;  // 閾値/率は既存CapitalRiseThreshold/CapitalRiseRatePerQuarterを共用（これは率の乗数・1で専門職と同率）
    public RatePerQuarter PettyEntryRatePerQuarter = new(0.02);             // >0でプールJobGate(職人)にも同じ元手ゲート＝「無一文の失業者が工房主」の非一貫を閉鎖
    public double SerfGateCoversPool = 0.0;   // ＝農奴が「職探し」なら都市へ入れる
    public double SerfMobilityGate = 1.0;            // 都市→農村は自由＝非対称
    public double ServiceUnemployment = 1.0;         // 使用人の袋小路（U恒0＝プール対象外・出口ゼロ）の解消
    public double JobSearchLiteracyWeight = 0.5;     // 1783年の教区縁故社会と1911年の職業紹介所社会の探索効率差＝識字（新聞求人・文書応募）が代理
    public double JobSearchUsesEma = 0.02;           // プール流入の駆動を瞬間UでなくU多年EMAへ＝循環失業は滞留して闘争度になり（Politics既存）、慢性失業だけが職業転換を駆動（手織工の遅さと恐慌の怒りが同じ係数から分岐）
    public double MinorityPenaltyModernBlend = 0.5;  // 開かれた社会ほど法的障壁が畳まれ慣行が残る（ユダヤ人解放の年代差が政体・社会構成から出る＝国名条件なし）
    // --- 作者裁定2026-08-16（同監査 rank7/8/12・点火状況は各行参照・実装は Meritocracy.cs・点火は1本1バッチのA/B）---
    public double FallAnchorReform = 0.3;            // カナリア二分探索で崖は0.4と0.5の間＝崖の直下を避けて0.3（テスト緑側の余裕）

    // --- 信用・銀行（§2.5.1／§9-G・段4）---
    public double CreditMultiplier = 3.0;     // 信用乗数（1/準備率）
    public double CreditAdjustSpeed = 0.05;   // 与信が目標へ寄る速さ（緩やか＝循環を滑らかに＝過剰変動の抑制）
    public double InterestRateEffect = 1.5;   // 金利が貸出意欲を抑える強さ＝貸出 ×clamp(1−これ×金利)
    public double PolicyRateBondWeight = 0.02;   // 政策金利→国債金利の上乗せ（高金利＝政府の借入も高くつく）
    public double PolicyRateCapitalDraw = 1.0;   // 政策金利→外資の引き付け（高金利が資本を呼ぶ＝carry/正貨還流。uniform分は ret−worldRate で相殺＝相対金利差が効く）
    public double PolicyRateFxWeight = 0.2;      // 政策金利→自国通貨高（高金利でペッグ防衛・減価を抑える）
    public double ConfidenceSpeed = 0.02;     // 信頼が育つ速さ（月次・遅い＝過信は平穏の年月が育てる。ミンスキー）
    public double PanicSpeed = 0.20;          // 信頼が崩れる速さ（速い＝取り付けは一夜。非対称が恐慌の形を作る）
    public double PanicSensitivity = 2.5;   // 平穏線を割った充足の不足幅→信頼目標の下げ感応度
    public double DrawdownPanic = 3.0;        // 景気後退（トレンドGDPのピーク比収縮）→信頼目標の下げ感応度（収縮10%＝目標−0.3）
    public double OverheatFragility = 1.1;        // 過熱(Heat)が閾値を超えた分×これ を信頼目標から減じる（0で無効＝較正中立）
    public double OverheatFragilityThreshold = 1.0; // この与信使用率を超えると過熱が信頼を蝕む（1.0＝枠いっぱい超＝バブル領域。Heat は 0..1.5）
    public double ConfidencePivot = 0.49;   // 平穏線：これ以上の充足が続くと信頼が満枠へ漸増（割るとパニック側）
    // --- B-3 金融危機：銀行取り付け・最後の貸し手・民間負債デフレ伝播。すべて Confidence/maxLoans 経由＝実Loans変化は既存の保存安全コードを通る ---
    public double BankRunSensitivity = 1.16;   // 預金者パニック：充足が平穏線を割った不足幅→実効準備(maxLoans)の縮み（正貨逃避）
    public Share BankRunMax = new(0.5);           // 取り付けによる実効準備の最大縮小率（0.5＝半分まで）
    public double LolrHeatTrigger = 0.9;      // 最後の貸し手の発動：与信過熱度(Heat)がこれ超で危機と判定
    public Share LolrDrawdownTrigger = new(0.10); // 同：景気後退(GdpDrawdown)がこれ超で危機と判定
    public double GdpPeakDecay = 0.02;        // GDPピークの忘却係数（四半期）
    public Share LolrMinFunding = new(0.6);       // 最後の貸し手を担える財政健全度（FiscalFundingRatio）の下限＝破綻寸前の国は救えない（Bagehot：solventにのみ貸す）
    public Share LolrStrength = new(0.7);         // 危機時に取り付け(run)を打ち消す割合＝流動性供給で恐慌を和らげる（高利＝根治はせず底打ちのみ）
    public Share PreTightenHeatFrac = new(0.75);      // 予防的引締めの発動＝過熱閾値のこの割合から（1.0＝予防なし＝素の過熱閾値のみ）
    public double CrisisAusterityRelief = 0.5;    // 恐慌の谷での緊縮反射の緩和（1.0＝緩和なし＝1930年代の緊縮の罠を素通し）
    // === テーマD 貨幣金融（価格・バランスシート層）===
    public double LolrReserveBoost = 0.5;   // 最後の貸し手の準備拡張（no-lolr-balance-sheet）：危機×財政健全時にmaxLoansを(1+これ)倍＝バジョット「平時の準備を危機に貸す」
    public RatePerYear LoanSpreadPerYear = new(0.0);           // 民間貸出金利の年率スプレッド（no-loan-interest-cashflow）：借り手(投資家)→預金者の利息移転 bank.Loans×(これ+政策金利項)
    public double PolicyRateLoanWeight = 0.0; // 政策金利（無次元0..1）→年率への換算重み（引締めが借り手の利払いを増やす）
    public double BondLossConfWeight = 0.5;   // 国債二次流通(no-bond-secondary-market)：BondPrice下落(利回り上昇)の評価損が銀行信頼targetConfを下げる×(国内債/GDP)
    public double DebtDeflationWeight = 4.0;  // フィッシャー民間負債デフレ：デフレ率×レバレッジ(Heat)→信頼目標の下げ（実質債務増→債務者破産→さらにデフレの渦）

    // --- 国庫・財政（§9-F／§6.2・段4b）---
    public double WarCoercionDrain = 0.15;    // 治安維持力と軍事のリンク(A)：軍は内外を兼ねる一つの武力＝戦時は前線に取られ国内の治安維持力が下がる
    public double CoercionPerSoldier = 5.0 / Military.WorkingMenStdShare;   // 装備された軍だけが治安維持力を持つ＝武器を買えぬ軍は即応度0→治安維持力0→革命
    public double CoercionMax = 2000;   // 装備された大軍でも2000で漸近（CoercionSoftCap）

    // --- 領土・地域間移民（§5.2／§A-8・段4c）---
    public double RegionMobility = 0.03;            // 地域間移民の緩和速度
    public double TransportFriction = 0.15;         // 州間移民の差から差し引く摩擦（国内の移民は距離の項も同じ摩擦に掛けて差から引く＝Territory.EntryFrictionTerm／越境は別に乗算の減衰 Territory.MigrationDecay）
    public double EthnicDraw = 0.7;                 // 低統一度は移民を引く：流入の引力 ×(1+これ×(1−統一度))＝坩堝
    public double EthnicAssimilation = 0.00017327;   // 同化の基礎（公教育なし＝半減期1000年＝墺の横這い）
    public double AssimilationEducationBonus = 0.0015581;// 公教育/義務教育の同化加速（合計0.0017314＝半減期100年＝「学校が農民をフランス人にする」）
    public double DiscriminationLiteracy = 0.45;    // 平均識字がこれ未満だと差別で移民を阻む：流入 ×clamp(平均識字/これ)＝低教育＝排斥
    public double UnityMilitaryQuality = 0.2;       // Power.Military ×(1+これ×(統一度−0.5))＝同質な軍は強い・多民族軍は脆い
    public double MinorityPromotionPenalty = 0.5;   // 昇格率 ×(1−これ×(1−core))＝完全少数派(core=0)で昇格半減＝ガラスの天井（移民/被征服層が下層に滞留）
    public double MinorityMilitancy = 0.2;          // 0で無効

    public double MonetaryPriceSensitivity = 0.25;  // 物価水準の感応度：P を「広義マネー/実質産出 のトレンド比」へ寄せる速度
    public double MonetaryTrendSpeed = 0.02;        // M/Y トレンドの追従速度（遅い＝secular な産出成長を吸収。小さいほど長期デフレが残り、大きいほど純粋に循環のみ）
    public double PriceLevelFloor = 0.4;            // 物価水準の下限（深いデフレの底＝暴走防止）
    public double MoTrendInitNeedsGdp = 1.0;        // > 0 のとき World.UpdatePriceLevel は実質 GDP が正でない四半期に M/Y の趨勢の基準を置かない（0＝今の形・番人 MoTrendInitTests）
    public double RegionalPriceLevelWeight = 0.3;   // 地域の現金の厚みの差→物価格差への感応度（0=地域差なし=従来一律）
    public double RegionalPriceLevelMin = 0.7;      // 地域物価乗数の下限（辺境デフレの底）
    public double RegionalPriceLevelMax = 1.45;     // 地域物価乗数の上限（金融中心の物価高の天井）
    public Share GoldCoverMin = new(0.25);            // 兌換維持に要る正貨準備カバー率（convertibility-not-reserve-backed＋fx-peg防衛）＝BaseMoney/BroadMoney
    public double GoldSuspendMinFundingRatio = 0.7;   // 金本位維持の最低funding比（下回ると強制離脱）※加入閾値 GoldStandardMinFunding とは別（ヒステリシス）
    public Share GoldStandardMinFunding = new(0.85);    // 加入の信用要件：連合維持を払えている（funding ratio がこれ以上）
    public double GoldStandardRateDiscount = 0.6;   // 完全兌換時の国債金利の倍率（信用＝低金利。0.6＝4割引）
    public double ConvertibilityRiseSpeed = 0.05;   // 加入後に兌換が確立する速さ（準備・信認の積み上げ＝遅い）
    public double ArrearsBeyondOpenedRoom = 1.0;    // (1215) >0：金本位でない月の不履行の累計は〈開いた切り下げの余地で払えなかった分〉だけ＝兌換の遅れのせいの未払いを数えない（0＝累計は 1−r）
    public double ConvertibilityDropSpeed = 0.34;   // 危機で兌換が崩れる速さ（兌換停止は一夜＝速い・非対称）
    public double ConvertibilityCoverageBand = 1.0; // 加入していても準備が薄れれば兌換が目減りする＝金本位期でも物価/為替/国債金利のチャネルが死なない
    public double FisherInflationWeight = 0.5;       // 名目国債金利＝実質＋これ×期待インフレ（高インフレ→高名目金利／デフレ→低名目金利だが実質債務は重い）
    public double FiatInflationWeight = 0.6;         // 通貨切下げ（紙幣増発）が物価を押し上げる強さ：P ×= 1+これ×(切下げ額/広義マネー)＝印刷→インフレ→実質債務減
    public double ExchangeRateSpeed = 0.2;           // レートが目標へ寄る速度（四半期）
    public double ExchangeTradeAdjust = 3.0;         // 貿易収支によるオーバーシュート強度（赤字→余分に減価＝輸出競争力／黒字→増価）
    // ==== 通貨改革（2026-08-13・作者採用）====
    public Share CurrencyReformForgiveShare = new(1.0);   // 帳消しにする累積切下げの割合（1=全消し）
    public Share CurrencyReformBurnShare = new(0.5);      // 記憶の代価＝累積切下げのこの割合を貯蓄から焼却
    public Share CurrencyReformBurnCap = new(0.5);        // 焼却の上限＝総貯蓄のこの割合（1度で全財産は焼かない）
    public double CurrencyReformLegitimacyHit = 0.35; // 正統性の代価＝帳消し比に比例（大きな罪を許すほど高い）
    public int CurrencyReformCalmMonths = 24;         // 発動に要る「切下げゼロ」の連続月数＝財政の同時再建（1923年11月だけ成功した差）
    public int CurrencyReformCooldownMonths = 120;    // 濫用防止
    public double ExchangeRateFloor = 0.3;           // レート下限（極端な減価の底＝暴走防止）
    public double ExchangeRateCeil = 3.0;            // レート上限
    public double CapitalIndTarget = 4.0;            // 投資収益率の素地＝(これ−工業化度)
    public double OffGoldCapitalPenalty = 0.5;       // 不換国の対外信用の倍率（金本位＝1.0。不換は外資が付きにくく逃げやすい）
    public double CapitalDemandScale = 0.02;         // 流入需要＝(収益率−世界平均)×名目GDP×これ（吸収力＝経済規模）
    public Share CapitalExportFrac = new(0.10);          // 資本余剰国が対外投資に回す投資家貯蓄の割合
    public RatePerQuarter CapitalDividendRatePerQuarter = new(0.02);        // 外資への四半期配当率（ForeignCapitalIn に対し・本国へ送金＝外資のコスト）
    public double CapitalDividendFromHostRent = 0;   // 確証なし（形 B は AI の推奨・金利生活者のポンプの同族の読み手 (794)）
    public double CapitalCouponUnpaidWriteDown = 1;   // 確証なし（AI の推奨）・事前登録 measurements/m815_20261104_prereg_fl.txt
    public double CapitalClaimCancelFromLedger = 1;   // 正で両側が同じ数（double の引き算の丸め・1e-9 級の吸収で持ち主の側だけが落ちる片側の書き込みを閉じる）・新しい係数なし・新しい状態なし
    public RatePerQuarter CapitalFlightRatePerQuarter = new(0.15);          // 資本逃避：毎四半期 ForeignCapitalIn×これ×(1−信用) が本国送還（信用崩壊で急増）
    public double CapitalContagion = 0.6;            // 伝染：危機国の割合×これだけ世界の資本意欲が縮む（サドンストップ）
    public double CapitalCreditFloor = 0.5;          // 信用の床：収益率＝伸びしろ×(これ+(1−これ)×信用)
    public double TreatyPortWinnerDistanceKm = 6000;  // 勝者と敗者がこれ以上離れていたら属国化しない（遠隔地の属国は統治できない）
    public Share TreatyPortLapseRatio = new(0.5);         // 0＝1命令も走らない厳密恒等（片道ラチェット）
    public double TreatyPortReparationMult = 2.5;     // 条約港の講和での賠償倍率（「多額の賠償金」＝属国化の代わりに金と通商特権を取る）
    public double TradeBiasStrength = 0.25;   // 敷居を動かす幅（世界価格比）
    public int TradeCheapSellSubsidy = 0;   // 売り手は満額を受け取り、差 s×積出し価格×r は売り手の国庫が出す（輸出奨励金・r＜1 の不足は売り手が食う）
    public double TariffFloor = -0.25;               // 関税の下限＝負の関税（ダンピング／輸出補助金）
    public double LandDiminishExponent = 0.7;       // landFactor=min(1,(耕地/農村人口)^これ)

    // --- 国際・世界市場（§5.1・段4d）---
    public double ResourceScarcityScale = 1.0;   // 検証する主張＝「実在する定義の穴は植民地財が二国間交易にも貿易統計にも一切入らないことだけ（規模は数%）」
    public double ResourceEndowmentScale = 1.0;
    public double TransportMargin = 0.18;   // 前鉄道の道路/河川は高コスト
}
