using System.Collections.Generic;
using System.Linq;
namespace PriceOfBread.SimCore;

public sealed partial class Coefficients
{
    // === テーマI（国際貿易・植民地）===
    public double MonopolyRentPremium = 0.15;   // 伝統的独占(塩の王室のgabelle・茶の東インド会社)の買い手価格上乗せ率

    // --- 資本・建設（§2.4 拡張：地域集約の「工業化度」。投資＝機械・セメントを消費して積む）---
    public double IndDepreciation = 0.003;        // 工業資本の減価率
    public double DevelopmentMemory = 0.4;   // 点火2026-09-06＝0.0→0.4・〔4-5〕開発度の記憶
    public double DepressionScrapRate = 0.06;       // 閾値超ドローダウン1あたりの月次・追加工業化減価（0で無効）
    public Share DepressionScrapThreshold = new(0.20);  // これ未満のドローダウンでは資本廃棄なし（通常の景気循環では工場は壊さない・大恐慌のみ）
    public double OpulenceTaxRelief = 1.0;         // 旧＝スタンスの税目標が固定率で、黒字国が経済から貨幣を吸い上げ続けデフレ螺旋（プロイセン国庫1.3M/GDP17k・米38M）
    public double LandedPowerCapitalization = 8000;   // 土地は政治力ではあるが流動的な購買力ではない
    public double PrivateAbsorptionBlend = 1.0;   // 速度を絞る＝天井なし＝デッドロックなし（敵対審査の推奨形）
    public double ClosedWealthMultiple = 1.0;     // 1.0で旧と厳密恒等
    public double[] LiteracyOddsByClass = [0.85 / 0.15, 0.80 / 0.20, 0.08 / 0.92, 0.25 / 0.75, 0.60 / 0.40, 0.80 / 0.20, 0.85 / 0.15, 0.70 / 0.30, 0.45 / 0.55, 0.30 / 0.70];   // 値＝旧の全国共通のテンプレ（world.json の41〜45国が置いていた 0.85/0.80/0.08/0.25/0.60/0.80/0.85 と直書きの商人 0.70・職人 0.45・使用人 0.30）t をオッズ t/(1−t) …
    public RatePerMonth IndInvestmentFrac = new(0.05);       // 投資家（資本家）が貯蓄からこの割合まで投資に回せる（affordability 上限）
    public double IndInvestmentSpeed = 0.012;     // 投資の速さ（純収益→建設需要への換算）
    public double IndPerConstruction = 0.01;      // 建設1単位が工業化度を上げる量（要較正）
    public double IndCapitalOutputYears = 1.0;    // >0 で私的な建設 1 束が上げる工業化度が 1÷N（N＝これ×州の実質の年所得÷基準の束の費用）になる＝州の経済の大きさに比例した投資額
    public double FactoryBuildScale = 0.5;        // 離散工場1棟の建設で増える生産者のScale下限（工場容量＝Vic2型）
    public Share FactoryMaterialFrac = new(0.6);      // プレイヤーの工場建設費のうち材料（鋼・機械・セメント・製材）に充てる割合
    public double ConstructionLaborWageFrac = 0.67; // 投資家/国家の建設：材料費×これを工賃として工員へ（材料6:工賃4の比＝投資にも工賃が乗る）
    public double FactoryBuildPriceCeil = 2.5;   // 建設率の半減点：素材束価格/参照 がこの倍率で建設可能率=0.5（崖でなく中心）
    public double FactoryBuildAffordSteep = 4;   // 点火2026-08-31＝16→4（清レポ＝16は「逓減の衣を着た崖」。4で束比3倍でも進捗が細く残る＝壁でなく逓減。ラウンドA実測でハーネス末端財の死蔵も解消）
    public double FactoryStartAffordFloor = 0.15; // 点火2026-08-31＝0.5→0.15（清レポ4-1「建設の壁」＝後発国の束比3〜5倍で着工が離散的に不可能だった崖の是正。進捗側が同じ採算率で連続に絞るので床は低くて良い。旧World定数の係数化と同時）
    public double FactoryMothballRate = 0.05;    // 建てた工場（倒産免除）の慢性赤字の垂れ流しを止める第4の動詞（施設は残る/倒産/休止/国有化）
    public double FactoryMothballMarginIn = 0.15; // 休止に入る慢性赤字の深さ（SmoothedMargin<−これ）
    public Share FactoryMothballCap = new(0.9);      // 休止できる床の上限割合（×民営持分。国営持分は休止しない＝国庫が支える既存設計の維持）
    public double FoodIncludesAnimalProtein = 0.0;   // 段1b：動物性タンパク（加工肉63/干物64・Nutrition=1）を食料に数え、交差弾力の食卓に参加させ、主食の必要量を AnimalNutritionNeedRatio で譲る（総食料需要は中立）
    public double StapleLedgerSum = 1;               // 食料の台帳を主食の和へ①（事前登録 measurements/fd504_20261023_prereg.txt）
    public double StapleDemandRebalance = 0;         // 0＝今の挟み（主食の相対価格の倍率を PriceFactorMin／Max で挟む）＝厳密恒等
    public double HarvestShortfallToMarket = 1;      // 凶作で自給が減った分を市場の需要へ③

    public double FactoryBuildAfford(double cpuNow, double cpuRef)   // 工場建設の採算：素材束の現在費用÷参照費用の比に対し建設可能率（0 より大きく 1 以下）を滑らかに返す（崖でなく漸減）
    {
        if (!(cpuRef > 1e-9) || !(FactoryBuildPriceCeil > 1e-9)) return 1.0;
        double ratio = cpuNow / cpuRef;
        if (!(ratio >= 0)) return 1.0;
        return 1.0 / (1.0 + System.Math.Pow(ratio / FactoryBuildPriceCeil, FactoryBuildAffordSteep));
    }
    public double FactoryBuildPriceGateScale = 1.0;   // 工事の進捗と素材需要の採算ゲートを軍需の PriceAfford と同じ式で混合（1＝旧恒等・0＝価格を見ずに工事が進み素材を相場で買う＝需要が立つので生産と輸入が応える）
    public double FactoryBuildProgress(double cpuNow, double cpuRef, double priceLevel)   // 復活は git の履歴から
    {
        double a = FactoryBuildAfford(cpuNow, cpuRef);
        return FactoryBuildPriceGateScale < 1.0 ? 1.0 - System.Math.Max(0, FactoryBuildPriceGateScale) * (1.0 - a) : a;
    }
    public double SoeSaleYears = 5.0;             // 国営工場の民間売却価格＝年間利益×これ（資本家の貯蓄が足りないと売れない＝レッセフェール移行のゲート）
    public double CreditRepayableYears = 5.0;     // 与信の上限の〈借り手の返せる額〉＝投資家の年間所得×これ（Credit.Step・A16）
    public double IndProductivityGain = 1.2;      // 低工業化度での生産性の伸び（飽和カーブの初期傾き）
    public double IndProductivityScale = 2.5;     // 生産性の逓減スケール（工業化度がこれを超えると伸びが鈍る）
    public double ScaleEconomyGain = 0.3;         // 規模ボーナスの最大上乗せ（飽和値。0で無効＝従来）
    public double ScaleEconomyHalf = 1.5;         // 規模ボーナスが半分に達する u（産出/自国需要）
    public double RefrigerationEffect = 0.7;      // 完全な冷蔵で生鮮の腐敗が下がる最大割合（0で無効）
    public double IndCapitalCost = 0.7;           // 投資の資本コスト（D1腕=0.45の顛末は IndDepreciation の注記を参照）
    public double IndTechLeverage = 2.4;          // 技術が工業化の収益を高める強さ（高技術国ほど高工業化＝国ごとの差を生む）
    public double ConsSteel = 1.0;   // 工場建設が食う束の鋼の量（鋼＋機械＋セメント＋製材）
    public double ConsMachine = 0.15;
    public double ConsCement = 0.4;
    public double ConsLumber = 0.5;
    public int SteelGoodId = Catalog.SteelId;                  // Catalog の id
    public int MachineGoodId = Catalog.MachineryId;
    public int CementGoodId = Catalog.CementId;
    public int LumberGoodId = Catalog.LumberId;
    public double BuildingGoodBundle = 1;   // 線 fb2 の採用で既定を 0→1 に倒した（旗 0 で従来の束へ戻せる）
    public double ConsBuilding = 2.3969;
    public int RailGoodId = Catalog.RailId;                   // 線路
    public int LocoGoodId = Catalog.LocomotiveId;                   // 機関車
    public double ConsRail = 0.25;
    public double ConsLoco = 0.12;
    public double InfraMaintRate = 0.4;   // 敷設済みインフラの保守需要＝工業化度×これ

    // --- 輸送・物流（§5.1拡張：港・鉄道・船。世界市場は港州のみ、内陸は鉄道で港経由。容量不足→購入量限界縮小→価格上昇）---
    public double TransportDecay = 0.02;            // 鉄道/船容量の月次減耗（維持に投資が要る＝持続需要）
    public double TransportTargetPerPop = 0.08;     // 目標容量＝人口×これ（必要数を増やし鉄道供給網の構築を要求）
    public double TransportCapPerUnit = 30.0;       // 鉄道/船 1単位の購入が積む容量
    public double TransportBuildRate = 0.15;        // 容量ギャップを毎月詰める速さ（投資の上限割合）
    public double RailByRegion = 1.0;   // 2026-11-13 に 1 で点火（腕 B の木・旗 0 の経路の番人は RailByRegionTests の旗 0 の 2 本）
    public double RailLayTerrain = 1.0;   // RailByRegion＝0 では読まない
    public double RailNeedByOutput = 1.0;   // 鉄道の need の取り分を州の産出の比へ（0＝潜在量の比）。RailByRegion＝0 では読まない。(1137) 2026-11-14 点火
    public int[] ShipGoodIds = [Catalog.SailingShipId, Catalog.SteamshipId];            // 帆船(tier1)・汽船(tier2)
    public int ShipTier2GoodId = Catalog.SteamshipId;                // 汽船＝tier2輸送船（蒸気船）
    public double ShipTier2CapMult = 2.5;           // tier2輸送船は1単位あたり容量が大（蒸気船は帆船より高速・大容量＝海運の革命）

    // --- 移民の機会と着地（国家間移民の行き先の機会＝Territory.Opportunity。★回1-2・2026-09-27：ゲーム外からの国際移民＝世界プールは削除＝MigrationStayHome も削除）---
    public double OpportunityWageWeight = 0.2;       // 州の労働価格プレミアム（1人あたり月次所得の対国平均比＝Region.WagePremium）1あたり移民機会の上乗せ（Opportunity ×(1+これ×(premium−1))）

    // --- 外交・戦争（§6.1 国際層）---
    public double HighTensionBand = 100.0;   // AI だけの旧経路（即時 AddTension ±6・ならし無し）と、その量 InsultTension=6・AI だけの乱数 InsultChance=0.35/四半期（検討 F 群の時間定数の兄弟）を削除した
    public double TensionDecay = 0.42;            // 平時の緊張の自然減（月）
    public double FriendlyTensionMax = 20;        // これ未満＝友好（同盟の条件）
    public double RivalTensionThreshold = 40;     // これ超＝ライバル（共通の敵）
    public int WarTriggerMonths = 6;              // 緊張100以上がこれだけ続くと開戦（攻守区別なし）
    public double AllyJoinTension = 40;           // 同盟国が参戦する敵への緊張（参加義務なし＝利害一致時のみ）
    public int MaxAlliances = 2;                  // 1国の同盟数の上限（全員同盟を防ぐ）
    public double HegemonThreatMin = 0.3;         // 覇権と見なす最低脅威超過（GDP×0.6＋植民地×0.4 の平均超過分）
    public double AntiHegemonAllianceChance = 0.7;// 覇権が共通の敵のときの同盟成立確率（通常0.4より高い＝合従が組まれやすい）
    public int AntiHegemonExtraAlliances = 1;     // 覇権に対抗する時だけ同盟上限を+これ（三国協商＝三極以上の合従を許す）
    public double AntiHegemonJoinFactor = 0.6;    // 敵が覇権のとき同盟国の参戦閾値をこの倍率に下げる（皆で覇権に雪崩れ込む）
    public double HegemonContainmentTrade = 0.40; // 封じ込めで覇権国が失う交易アクセス＝これ×脅威超過（0で無効）
    public double HegemonContainmentMax = 0.55;   // その上限（交易アクセス floor 0.1 まで落とさず・最大でも交易の約半分まで）
    public double PromotionGateOwnSchooling = 0.15;   // 「押し合いの累積」と「運の累積」へ分解した行を年表へ出す
    public double ImmigrationAbsorbExponent = 2.0;   // 0=現行と厳密恒等（休眠）
    public double ScarceMineralOnlySulfur = 1.0;   // 1で硫黄5のみを対象にする
    public double SulfurFromPyriteProductivity = 0.7;   // 硫黄法比の生産性（黄鉄鉱法は歩留まりが低い＝高コスト）
    public double SilverRushEnabled = 0.0;   // 0＝表の銀の行を置かない（西部の銀は非産地の0.4のまま）＝旧と同じ
    public double MineralCostFollowsGrade = 0.0;         // 0＝鉱脈で割らない（自己費用＝MarginalCost×自己費用の物価水準×州の乗数のまま）＝厳密恒等
    public double EndowmentCostFollowsAbundance = 0.0;   // 賦存倍率>1 の州：自己費用÷倍率^これ（0＝割らない＝厳密恒等／1＝完全に「同一費用あたりの産出能力」）
    public double TradeBuyBySupplyCurve = 1.0;   // (1172) 3 回目の旗 1・(1144) T1 A-1：清算の買いの〈来月の国内の見込み〉を u′＝min(着値, 州の値段) での供給①の見込み min(P, P̂(u′)) で読み、需要の倍率 (rpT÷u′)^ε を掛ける（0＝旧の xNeed＝厳密恒等）
    public double EndowmentCostFollowsScarcity = 0.0;    // 賦存倍率<1（非産地 0.4）の州：自己費用÷倍率^これ＝費用が上がる（0＝上げない＝厳密恒等）
    public double RecipeCostFollowsProductivity = 1.0;   // 点火（枝 gp の腕 B〔b10_20261014_gp329B_ins〕の形・合流の担当が作者 2026-10-14 の止める線の規則で読み直して採った）
    public double SteamTurbineCoalInput = 0.6;      // 石炭火力0.9に対し熱効率で0.6
    public double SteamTurbineProductivity = 1.25;  // 石炭火力比
    public double OilMachineryInput = 0.05;         // 原油1単位あたりの機械投入（RefPrice 4.39×0.05≈+0.22＝原油0.54→0.76）
    public RatePerMonth ArmsWearRatePerMonth = new(0.02);            // 平時の月次摩耗率（定常で 目標×これ ＝ 現行の平時フローに一致するよう逆算）
    public double ArmsWarWearScale = 1.0;         // 戦闘消耗＝摩耗に (warMult−1)×これ を上乗せ（1.0＝9倍戦時は摩耗9倍）
    public double TrenchDefenseWeight = 0.5;   // 塹壕の防御の重み（装備が戦場に残る ArmsStock と対で意味を持つ）
    public double WarPushSpeed = 0.10;            // 戦争スコアの押し合い速度（月・押しの割合1あたり）
    public double WarLuck = 0.12;                 // 戦況の運（決定的会戦・補給・士気）＝毎月の戦勝点に±これの乱数を加算
    public double SeaInvasionControlMin = 0.65;   // ＋精密化「厳密には、海戦で閾値に勝たない限り上陸はできない＝陸戦は起きるが、その被害は民間には出ない」
    public double WarDeclareTension = 140.0;      // 屈服経路（AISubmitMilRatio=3.0）の開通で一方的な組合せは戦わずに折れる＝旧140へ戻し互角の対立だけが戦争へ（160は屈服なし時代の戦争過多対策だった）
    public Share MobilizationConscriptFrac = new(0.22);   // 既存のDrawFromPoolで非兵士POPを兵士へ変換＝代償は自然（労働喪失で経済悪化・兵増で戦死増・国民意識ゲートで低NC国は届かない）
    public double ConcedeTensionTarget = 110.0;   // 最後通牒で「要求を受け入れる」＝緊張をこの値へ下げて開戦を回避（WarDeclareTension=140未満）
    public double TensionPopupThreshold = 70.0;   // 緊張ポップアップを出す内部緊張の閾値（同ランク列強と）＝宣戦閾値の半分（WarDeclareTension=140）
    public RatePerMonth WarExhaustionRatePerMonth = new(0.03);       // 総動員の上限（MobilizationConscriptFrac＝働き盛りの男の22%）まで動員した国の月の基礎
    public double WarExhaustionEcon = 0.10;       // ＋経済悪化（1人あたりの落ち込み GdpPcDrawdown・2026-09-29＝領土を失った総額の落ち込みは数えない）に比例して厭戦が増す（月）
    public double WarExhaustionBlockade = 0.07;   // ＋通商破壊（海上封鎖）の度合い(1−CommerceRaidFactor)に比例して厭戦が増す（月）
    public double WarExhaustionCasualty = 0.6;    // ＋戦死（兵士に対する戦死率）に比例して厭戦が増す
    public double WarFeverMilitancy = 0.6;     // 闘争度→戦争熱：大衆の闘争度が戦争熱を押し上げる（外征で内憂をそらす）
    public double WarFeverMilitancyGate = 0.5; // その出口は好戦世論の公共圏に比例して開く加点（将校貴族 trad と合算しClampで上限1）
    public double WarWearyThreshold = 0.5;        // 疲弊がこれ超なら新たな戦争に入らない（講和後も数年は減衰中の厭戦が再戦を抑える＝休戦の仕組みの代わり・Q9）
    public double WarPeaceExhaustion = 0.9;       // 双方がこれ超なら早期講和（疲れ果てて手打ち）
    public double PostWarTension = 38;            // 講和後の緊張の置き直し値
    public Share ReparationGdpFraction = new(0.2);    // 賠償金＝敗者GDP×戦勝点×これ

    // --- 戦争熱／膨張意欲（案Y・植民地獲得を許容する国内世論。段B＝計算と累積のみ／消費は段Cで配線。値は暫定でsweep対象） ---
    public double WarFeverMilitarism = 0.4;   // ×好戦世論の公共圏（報道自由度×識字×都市化×対外屈辱∈[0,1]）
    public double WarFeverMilSpend   = 0.5;   // ×軍事費(0..1)
    public double WarFeverNoble      = 0.3;   // ×伝統支配層の力（将校貴族＝旧支配層ブロック）
    public double WarFeverSoldier    = 4.0 / Military.WorkingMenStdShare;   // ×兵士人口比（兵役の母数を働き盛りの男 Military.WorkingMenStdShare で読む）
    public double WarFeverThreat     = 0.3;   // ×外的脅威(MaxTension/100)＝脅威下では軍が発言力を得る
    public double WarFeverThreshold  = 0.5;   // これを超えた分だけ膨張意欲が貯まる（中立＝0.5前後）
    public double ExpansionWillGrowth   = 0.5;   // 四半期あたりの累積率 ×(戦争熱-閾値)
    public double ExpansionWillCap      = 10.0;  // 膨張意欲の貯蓄上限（連続侵略の原資上限）

    // --- 能動侵略の要件（段C・案Y。値は暫定でsweep対象） ---
    public double InvadeShipsBase         = 60.0;  // 遠征に要る船（帆船+汽船）の基礎在庫
    public double InvadeMilitaryFactor    = 1.2;   // 自軍の実効戦力が標的軍事力×これ以上で挑める
    public double InvadeCannedBase        = 20.0;  // 遠征軍の携行糧食（缶詰）の基礎在庫＝全侵略で必須（兵站。缶詰は元来軍隊向けの発明）
    public double InvadeCannedPerMilitary = 20.0;  // ＋標的の軍事力に比例（大遠征ほど多く食う＝糧食産業を太らせる動機）
    public double ProximityTensionRange   = 3000.0; // この距離(km)内の列強が反応（隣接で最大、距離に比例して減衰）
    public double ProximityTensionMax     = 35.0;   // 隣接(距離0)時の緊張上乗せ
    public double OpiumExportPressureTension = 40.0;    // 「緊張が高い」とみなす閾値（Tension は 100 で宣戦水準）
    public double OpiumExportPressureThreshold = 1.2;   // 外圧の合計がこれを超えたら輸出を禁じる（TradeWeight の和＝交易上の重み）
    public Share InterestCapitalization = new(0.0);   // 未払い利息を残高へ繰り入れる比率
    public double PaySalesToProducerBook = 1;   // (1228) c124bk 旗：1＝InternationalMarket.PaySales の代金を生産者の帳へ（LastRevenue に代金・LastCost に賃金の分＝損益の増えは地代）。0＝帳は国内の売上だけ（番人 PaySalesProducerBookTests）
    public double CapSkipSellerPotential = 0;   // (1244) c126pf 旗：1＝清算で容量が尽きて飛ばした売り手の申し出（その行で通るはずだった量＝min(残り, 買い手の X を lp で読んだ値−その行の未充足)）を売り手の州の潜在量へ足す（BilateralMarket.cs の :963 の分岐・買い手の側には足さない）＝船腹の取り分が容量で塞がれた申し出も見る。0＝旧（飛ばした売り手は潜在量を残さない）
    public RatePerQuarter CanalInvestFrac = new(0.03);      // 運河へ毎四半期投じる投資家貯蓄の割合（全経済国が拠出＝集合投資。スエズは欧州全体の利益）
}
