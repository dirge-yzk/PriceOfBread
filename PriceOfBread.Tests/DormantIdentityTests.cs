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
        "ScarceCapFollowsEndowment",    // E-15 手順3：希少鉱物の産地の天井を賦存比例に
        // ★ChilledMeatEnabled は 2026-09-04 に 1.0 で**点火**＝この表から外した。
        "MeritModernityFromClosed",     // AUD-07：メリトクラシーの政体バフ除去
        "PrivilegeFromCrownShare",      // AUD-07：身分制免税の政体バフ除去
        // ★SymmetricTensionVerb は 2026-09-28 に点火して旗ごと削除（回8-1・dip-04）＝この表から外した。
        "RebelArmsMarketEnabled",       // ★武器の市場購入（設計メモ§7 (a)・2026-09-11）＝反乱勢力が参加POPの貯蓄で国軍と同じ財を買う＋法「民間の武器購入」＋FactionStrength 第5因子。
                                        //   0＝EconomyStep の枝に入らず（rebelDemand 全0）・法は7箇所全部が素通り・第5因子は掛けない＝厳密恒等。点火は 1.0（0/1の主フラグ）
        // ★FamineDriveGain は 2026-09-22 に 10 で**点火**、NeedTierSplit は同日に旗ごと削除（階層3分割を既定化＝作者裁定2026-09-20）＝この表から外した。
        "ParticipationDiscontent",      // ★二量分離 段3（2026-09-09）＝参加の述語を不満度へ（読み手は FactionSnapshot.ComputeClusters の1箇所）。
                                        //   0＝旧＝「闘争度 > MilitancyThreshold(0.5)」の二値＝不満度を1度も読まない（ビット恒等）
        "DesperationMilitancyOverride", // ★二量分離 段3（2026-09-09）＝闘争度による層の上書き（窮乏チャネルの意識ゲートを開ける）。0＝×(1−0)＝ビット恒等
        "AssimilationRevival",          // ★段4（2026-09-09・§7-7バグa後半）＝民族復興＝抵抗の床を外して符号を反転させる（勾配の差し替え）。
                                        //   0＝床あり＝厳密恒等。点火値 1.0526＝1/0.95（反転点＝ボヘミア1900の識字0.95）
        "NationConsciousnessDerived",   // ★段4（2026-09-09・作者裁定5／§7-7バグb）＝国民意識をストックから**同じ漸化式の不動点**へ。
                                        //   0＝旧＝ストック＝厳密恒等。点火値 1.0（記憶が消え、国内経路の運搬漏れが定義ごと消える）
        "SerfGateCoversPool",           // C-03：農奴の移動制限を失業プール経路へも広げる
        // ★DomesticFlightHasHost は 2026-09-22 に**点火**（旗ごと削除＝逃避の受け皿が常に外資の負債を記帳）＝この表から外した。
        "NiterBedEnabled",              // 硝石丘＝硝石の低効率レシピ（財の3分類の再開）＝0で足さない
        "ScarceCountsIncludeColonies",  // 希少鉱物の総量保存の k/N に植民地を数える（分子と分母の非対称の是正）＝0で従来
        "PrivilegeLedgerEnabled",       // 特権の台帳（段1・観測のみ）＝0で1命令も走らない
        "PrivilegeStripMilitancy",      // 特権システム 段3＝剥離の怒り（PrivilegeLedgerEnabled>0 が前提）＝0で加算なし
        "DecayFromPrivilege",           // 特権システム 段6＝制度疲労の導出（読み手3箇所は DecayForReaders・ゲート0で旧値）
        "PrivilegeMonopolyInLedger",    // 特権システム 段5前半＝特許会社の取り分を台帳へ（記帳のみ）
        "GuildLawGate",                 // 特権システム 段4＝同業組合法の主ゲート（法が動かない）
        "GuildRentPremium",             // 特権システム 段4＝職人の上乗せ（0＝なし）
        "GuildMobilityDrag",            // 特権システム 段4＝工場への移動の遅れ（0＝なし）
        "GoldStandardFrontierRatio",    // 金本位志向を前線比へ（08-40）＝0で絶対閾（従来）
        "MintFloorRespectsReserve",     // 跛行本位＝床の standing offer を準備過剰で閉じる（銀暴落の線）＝0で従来
        "MintParitySells",             // ★器（2026-09-07）＝床の standing offer を両側に（平価の上で準備を平価売り＝平価買いの逆操作）＝0で従来（片側）
        "RuleSharesEnabled",            // 支配度の一般化 段1＝州の4区分の積分（観測のみ）＝0で従来
        "ProjectionUsesLand",           // 支配度doc §14b＝陸路の起点（ProjectionRuleMin=1.1 が主ゲート＝既定で集合={首都}）
        "RuleSharesReach",              // 支配度 段4＝公共財の到達を国家区分に比例（教育の財源率・年金）
        "RuleSharesTaxBase",            // 支配度 段3＝課税・徴兵ベースを区分へ（仲介者の取り分は貴族へ）
        "ForeignRuleFromTreaty",        // 支配度 段5＝条約港→外国持分（観測のみ）
        // ★MusketRecipeEnabled（前期の軍需レシピ）は 2026-09-27 回2-1 で**点火**（旗ごと削除）＝この表から外した。
        "SilverRushEnabled",            // 銀の後期産地＝シルバーラッシュ（0＝解禁も富鉱化もしない）
        "MineralCostFollowsGrade",      // ★器（2026-09-07）＝鉱脈の倍率を鉱物RGOの限界費用にも通す（EconomyStep.MineralSelfCost）。0＝鉱脈で割らない＝厳密恒等
        "EndowmentCostFollowsAbundance",   // ★器（2026-09-08）＝賦存倍率>1 の州の自己費用を賦存で割る（Simulation.ApplyEndowment）。0＝MarginalCost そのまま＝厳密恒等
        "EndowmentCostFollowsScarcity",    // ★器（2026-09-08）＝賦存倍率<1（非産地0.4）の州の自己費用を上げる（同上）。0＝上げない＝厳密恒等
        // ★FacilityBuildOutlayIncludesMaterial（裁定②の安全弁）は 2026-09-27 線2 で**点火**（旗ごと削除）＝この表から外した。
        "FactoryBuildRefIsReal",        // 進捗/素材ゲートの参照束を実質（×PriceLevel）へ＝兄弟ゲートと同じ物差し（0＝名目＝旧恒等）
        // ★PriceLawSupplyIncludesStock と TradeBuyerSolvesAtUnitPrice は 2026-09-08 に 1.0 で**対で点火**（腕 price2b）＝この表から外した。
        "WorldPriceStatUnclamped",      // 表示用の世界価格統計のクランプを外す（0＝旧式）
        "PeacetimeConscriptGrievance",  // 支配度doc §15＝平時の植民地徴兵の遺恨
        // ★B9（州一揆の保持力の州按分）・項目2(b)（F1鎮圧枝の緩和なし）・項目3（F1の州選択＝熱×群衆）は 2026-09-11 に1腕で**点火**（係数ごと削除）＝この表から外した。
        "DesperationLayerBlend",        // ★⑤3層の振る舞い分け（2026-09-11）＝窮乏チャネルの「誰が動くか」を層の重みで読む（エンジン内で wMid を読む唯一の箇所）。0＝旧ゲート＝ビット恒等
        "ColonyPlantationRent",         // 実バグ B5＝植民地格の昇格州に大農園の地代
        "HeldSinceFromData",            // 実バグ B14＝world.json の heldSince を読む（0＝全州 Core・支配度1）
        // ★CivilWarPredicateUnified は 2026-09-08 に 1.0 で**点火**（F3b蜂起doc §5 段3・作者裁定②）＝この表から外した。
        "RevoltIgnitionIntensity",      // F3b蜂起doc 段4＝点火の付け替え（門1＝FactionStrength÷f.Size の示強量）。0で旧2門（力の門＋好機/圧倒）＝厳密恒等
        "RevoltGrievanceWeight",        // F3b蜂起doc 段4＝門2の怨嗟の重み（min(1, FactionRejectionMutation×Rejections)）。0で加算項が消える
        "RevoltVoiceFloor",             // F3b蜂起doc 裁定1＝(1−発言権)の床（旧支配層と普選後の大衆の恒久ゼロを外す）。0で FactionStrength は1命令も変わらない
        "RevoltMilitiaRaise",           // F3b蜂起doc 段5＝蜂起した運動のブロックのPOPが不満度に応じて武器を取る率（Pop.MilitiaShare）。0で誰も札を持たない＝全読み手が恒等
        "MilitiaCombatValue",           // F3b蜂起doc 段5＝民兵1人あたりの戦力 a（<1＝弱兵）。戦場の民間人死の配分の重み 1+a×民兵率 も同じ a。0で戦力にも死の配分にも寄与しない
        "SoldierDefectionByMilitancy",  // F3b蜂起doc 段5＝守備兵の寝返り率＝min(1, これ×兵士POPの闘争度)。0で旧＝反乱州の守備兵は州ごと全員が寝返る
        "RevoltCooldownRetire",         // F3b蜂起doc 段6＝4四半期の固定タイマーの退役（代替＝結束0からの再成長と「もう野に出ている」状態の排他）。0で旧タイマーがそのまま効く
        "RevoltExpansionScore",         // F3b蜂起doc 段7＝反乱の膨張（膠着の講和で戦勝点を予算に既存の割譲経路で親の州を取る・州1つの値段）。0で内戦の講和は1命令も変わらない
        // ★要求は法の経路（作者裁定2026-09-11）は同日に**点火**（主フラグごと削除・旧3枝も物理削除）＝この表から外した。
        // ★FiscalLawStages（税・支出・軍事費の段＝作者裁定2026-09-12 R1/R2）は 2026-09-20 に**点火**（旗と段の無い世界の枝ごと削除・AiMobilization も同時に削除）＝この表から外した。
        // ★CoupForceShare／CoupMassArmyDiscount（作者裁定2026-09-19 R3/R4＝クーデタ項の取り分化と職業軍の度合い π）は 2026-09-20 に既定化（腕 A4）＝旗と旧式の枝ごと削除＝この表から外した。
        "ColonialRegionSettlement",     // 実バグ B1＝植民地格の州への入植と Integrated 昇格
        "ColonyDevCostFromRegion",      // 実バグ B2＝植民地格の州の奪取コストが州の開発度を読む
        // ★EquipNeedsDiscovery（装備の発見）は 2026-09-27 回2-1 で**点火**（旗ごと削除）＝この表から外した。
        "AdoptExpertiseWeight", "InnovateExpertiseWeight", "TeacherQualityWeight", "CatchupContactWeight",   // 知識と人的資本の経路（作者採用2026-09-06）＝0で従来（★EduFundingCashClamp は 2026-09-27 線2 で点火・旗ごと削除）
        "EduReachFromAdmin",            // 特権 段1（作者裁定2026-09-08）＝義務教育の国庫給付の天井に行政の充足率（0で×1.0＝給付は行政を見ない＝従来）
        "EduProducersCoexist",          // ★教育の三層化 段1 器（2026-09-12）＝教区学校（聖職者・紙）と公立学校（教師・書籍）を常設2件で並置し教育法を非活性側の Scale=0 へ降格。
                                        //   0＝AddServiceProducers が従来の1件だけを add・StepMonth の月次差し替えがそのまま走る・EconomyStep は Scale を読まない＝ビット恒等。点火は段3（batch30 単独＋savetest）
        "NobleAdminFromSeigneurial",    // 特権 段2（作者裁定2026-09-08）＝領主権の見返り＝貴族の行政の素地を「地代シェア∨農奴の未解放度」で変調（0で×1.0＝頭数だけ＝従来）
        // ★下2本は D-35 の反射走査が拾った（**初期化子を書かない宣言**＝`public double X;` なので静的なテキスト検索では見つからない）。
        //   ⇒ 反射で数え上げる形にした甲斐がそのまま出た1件目。
        "PopDebtDriven",                // POP個人債務の主フラグ（0＝誰も読み書きしない厳密恒等）
        "UnionSatBonus",                // 作者裁定2026-08-19「労働組合は団体交渉権などに使うので休眠」（思想⑨＝法→満足度の直接加算を避ける）
        "SilverParityFromRatio",        // ★器（2026-09-07）＝世界銀価格を同一市場内の 銀/金鉱 比から作る（E→平均→E の自己参照を断つ）。0＝旧統計 Market.Price[銀] を注入＝厳密恒等
        "DiscontentLawGap",             // ★二量分離 段1（2026-09-09）＝不満度（導出量）の「法の隔たり」の物差しを PolicyPreferences.LawGap へ（0＝旧＝4軸座標の距離）。
                                        //   ⚠段1は Politics.Discontent の**読み手がゼロ**なので、この係数はどちらの値でも世界を動かさない（点火は段2＝読み手を付けるとき）
        // ★B-03（WarFeverUsesBudgetShare）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
        // ★B-06（TransportBaselineIsLand）は 2026-09-03 に 1.0 で**点火**＝この表から外した。
    ];

    /// <summary>★1.0 が恒等の scale 型の器の休眠表（レビュー2026-09-07）：反射走査（既定0だけを数える）の外にあるので手で書く。点火時にこの表からも外す。</summary>
    private static readonly string[] DormantOneFields =
    [
        "MilitaryProcurePriceGateScale",   // 軍需財の国家調達の価格ゲート倍率（1＝満額＝旧恒等・0＝価格を見ない）
        "MilitaryBuildCostCapScale",       // 軍需工場の建設費上限の倍率（1＝旧恒等）
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
            Assert.True(f!.GetValue(c) is double d && d == 1.0, $"scale型の休眠係数「{name}」の既定が1.0でない（点火したなら表から外す）");
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
            double v = System.Convert.ToDouble(f!.GetValue(c));
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
    /// ★2026-09-27（回4-3 の再挑戦）：ShipFuelRate を点火（0.0016）＝外した。</summary>
    private static readonly string[] ZeroDefaultLedger =
    [
        // ★D-35 の反射走査が拾った「初期化子なし」の宣言（`public double X;`）＝静的なテキスト検索では見えない4本。
        //   いずれも休眠中の POP個人債務（主フラグ `PopDebtDriven`）の**下位パラメータ**＝主フラグを点火するときに値が入る。
        //   ⚠主フラグではないので休眠表には置かない（休眠表は「これ1本で機構が入り切りする器」の一覧である）。
        "PopDebtLossShare", "DebtRepayFrac", "DebtRuinRatio",
        "HouseholdCreditMonths",
        "AbsorptionIncomeRef", "AbsorptionIncomeWeight",
        "AiDevelopmentalSubsidy",   // AiMobilization は 2026-09-20 に係数ごと削除（段の点火で総動員のAI分岐が常時 live）・AiCrisisBorrow は 2026-09-28 に係数ごと削除（借入の自動化で予約の列が消えた）
        "ArmyShapeGauge",   // BondRateDebtQuad は 2026-09-29 に係数ごと削除（国債の金利を史実から出し直した＝債務の二次項は英の債務比2.5 の史実と合わない）
        "CoercionUpkeepToSoldiers", "CohortGateK",
        "CorruptionPoorOfficialWeight",
        "DomesticMarkupMax", "EducationGateUsesEma",
        "ElectionBribePovertyWeight", "EnclosureRate", "EraNavalFuel",
        "FamineAbsoluteThreshold", "FertileAbsoluteBlend",
        "FertilityEquilibriumWeight", "FertilityIncomeSurvivalGate", "FertilityLandDivisionSlope",
        "FoodIncludesAnimalProtein", "GarrisonProcurement",
        "HigherEduNeedPerEducated",
        "IncumbencyBias",
        "InterestCapitalization", "LoanSpread",
        "MigrationFarePerkKm",
        "ModernizationToHigherEdu", "NationalismFromConsciousness",
        "NavalFuelPerGauge", "NewbornLiteracyDilution", "NobleInvests",
        "OpulenceInfraChannel",
        "PettyInterestGate", "PolicyRateLoanWeight",
        "PreemptiveSeizeWeight", "PromotionGateHigherEdu", "ReactionFromLoss",
        "ResearchBaseRefPop", "ResearchScaleWeight",
        "ResearcherFrac", "RuralPromotionRate",
        "SeaEscortPerTradeValue", "SilverStandardIndThreshold",
        "SoeSaleMilitancyCeil", "SpecialistExitBanPrefScale",
        "SubmitProbabilistic",
        "TransportDemandAffordCap", "TransportPotentialWeight",
        "WarStartPoliticsScale",
    ];

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
            if (f.GetValue(c) is not double d || d != 0.0) continue;
            if (!known.Contains(f.Name)) missing.Add(f.Name);
        }
        Assert.True(missing.Count == 0,
            $"既定0の係数 {string.Join(", ", missing)} が休眠表にも台帳にも無い。"
            + "器として入れたなら DormantZeroFields へ（恒等が自動で検査される）。"
            + "0が正しい較正値なら ZeroDefaultLedger へ理由とともに足すこと。");
    }
}
