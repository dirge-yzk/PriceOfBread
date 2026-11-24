namespace PriceOfBread.SimCore;

/// <summary>四半期ごとの最小テレメトリ1点（§2.6／§8）。段0〜4 で見る指標を持つ。</summary>
public readonly record struct TurnSnapshot(
    int Year, int Quarter,
    double TotalMoney, double TotalPopulation,
    double UrbanShare,
    double FoodPrice, double FoodOutput, double FoodStock,
    double ManufPrice,
    double FarmerSatisfaction, double WorkerSatisfaction,
    double AvgLiteracy, double Gdp,
    int PolityCode, double AvgConsciousness, double AvgMilitancy,
    double ReformistPower, double Coercion,
    double BroadMoney, double Loans, double Confidence,
    double Treasury, double FiscalFundingRatio,
    double HomeRegionPop, double FrontierRegionPop,
    double Gold, double CumTradeFlow);

/// <summary>
/// シミュレーション本体。三層時計（§3・§11.3）を回す。段0〜4 は period=月：
/// 月次に 信用→（active地域ごとに）市場/所得→財政→pop意思決定→領土（地域間移民・解錠）→識字、
/// 四半期に カードドラフト→政治。各層は対応フラグの時だけ動く。国家は複数地域を保有しうる（§5.2）。
/// </summary>
public sealed class Simulation
{
    private INationAgent _agent;
    // 呼び手＝App の MainWindowViewModel.SwitchPlayer だけ。〔履歴 Simulation.cs#1〕
    public void SetAgent(INationAgent agent) => _agent = agent;
    // 実際の助走は MultiNationSim.WarmupEconomy が**政治・外交を凍結**して行う（エージェントを差し替えない）＝この動詞は不要になっていた。
    private readonly List<IInvariant> _invariants;
    private readonly int _shockQuarter;
    private readonly double _shockMagnitude;
    private int _quarterIndex;
    private int _dayInQuarter;           // 三層時計の日カウンタ（0..89）。StepDay が進め、月/四半期の境界で決算・戦略を発火
    private int _lastHarvestYear = -1;   // 豊作/不作を年1回ロールするための前回年
    private double _prevHarvestFactor = 1.0;   // 前年の収穫（AR(1)の遅延項・harvest-iid-no-persistence）。初期1.0で1年目はε主導＝平年基準

    // 収穫ショックの分布（symmetric-uniform-harvest）：HarvestSkew=0 で対称一様（現行恒等）、>0 で左裾の重い負歪み。
    // 入力 u∈[0,1) は呼び出し側で1回だけ引く（RNG消費1個＝決定論不変）。返り値は平均0付近の収穫偏差（1.0+これ がHarvestFactor）。
    internal static double SkewHarvest(double u, Coefficients c)
    {
        double a = c.HarvestVariation;
        if (c.HarvestSkew <= 0) return (u - 0.5) * 2.0 * a;   // 既定：対称一様[-a,+a]＝現行と厳密一致
        double k = System.Math.Clamp(c.HarvestSkew, 0, 0.95);
        // 負歪み（左裾）：指数 p=(1-k)<1 で u^p の質量を「1（平年〜豊作）」側へ寄せ、稀に u→0 で壊滅的凶作の薄い左裾を作る。
        // 平均 E[u^(1-k)]=1/(2-k) を引いて期待値を1.0に保つ（食料供給期待の保存）。k→0 で p→1・mean→1/2＝対称一様に連続。
        double up = System.Math.Pow(u, 1.0 - k);
        double mean = 1.0 / (2.0 - k);   // up の期待値 E[u^(1-k)]=∫₀¹u^(1-k)du=1/(2-k)
        return (up - mean) * 2.0 * a;   // 平均0・下限 (0-mean)*2a＝-2a/(2-k)（kが大きいほど深い谷）・上限 (1-mean)*2a（豊作側は浅い＝左歪み）
    }

    public World World { get; }
    public DeterministicRng Rng { get; }
    public DeterministicRng EpidemicRng { get; }   // 疫病専用の独立系列（B-4）。主RNGを乱さない＝豊凶/鉱山/pop意思決定の消費順を保ち較正/Stage3を保護
    public DeterministicRng HarvestRng { get; }    // 作物病虫害の独立系列。主RNGも疫病系列も乱さない＝較正中立（合成食料は非対象）
    public List<TurnSnapshot> Telemetry { get; } = [];
    public bool TelemetryEnabled = true;   // false＝Snapshot()を止める（selfplay/バッチの割当削減。インスタンス単位＝並列テストと不干渉）

    // Simulation〔履歴 Simulation.cs#2〕

    public Simulation(Scenario scenario, INationAgent agent)
    {
        _agent = agent;
        _shockQuarter = scenario.ShockQuarterIndex;
        _shockMagnitude = scenario.ShockMagnitude;
        Rng = new DeterministicRng(scenario.Seed);
        EpidemicRng = new DeterministicRng(scenario.Seed ^ 0xC0FFEE_D15EA5EUL);   // 疫病専用系列（主RNGと分離）
        HarvestRng = new DeterministicRng(scenario.Seed ^ 0xBADC0FFEE_0DDF00DUL);   // 作物病虫害の専用系列（主RNG・疫病系列と分離）

        var regions = scenario.PrebuiltRegions ?? scenario.Kind switch
        {
            ScenarioKind.AgrarianToUrban => BuildAgrarianToUrban(),
            ScenarioKind.FrontierDevelopment => BuildFrontierDevelopment(),
            ScenarioKind.RegionalFamine => BuildRegionalFamine(),
            ScenarioKind.LaissezFaire => BuildLaissezFaire(scenario.FactoryRentShare),
            ScenarioKind.ProductionChain => BuildProductionChain(),
            ScenarioKind.FullChain => BuildFullChain(),
            ScenarioKind.France => BuildFrance(),
            ScenarioKind.Nation => BuildNation(scenario.NationConfig
                ?? throw new ArgumentException("Kind=Nation には NationConfig が要る"), scenario.Coef, scenario.SharedWorld),
            _ => BuildSelfSufficientFarm(),
        };
        World = new World(regions, scenario.Coef)
        {
            Year = scenario.StartYear,
            Quarter = 1,
            EducationLaw = scenario.EducationLaw,
            EducationSpending = scenario.EducationSpending,
            PoliticsEnabled = scenario.PoliticsEnabled,
            Polity = scenario.InitialPolity,
            ExternalPressure = scenario.ExternalPressure,
            BaseCoercion = scenario.BaseCoercion,
            FiscalEnabled = scenario.FiscalEnabled,
            TaxRate = new Share(scenario.TaxRate),
            PollTax = scenario.InitialPollTax,   // 人頭税＝近世国家の主財源（行政不要）。序盤の国庫を支える（所得税は19世紀までロック）
            LandTax = scenario.InitialLandTax,   // 土地税の開始値（確立国家は開始から徴収）
            // GovDebt〔履歴 Simulation.cs#3〕
            GovDebt = new MoneyStock(scenario.InitialGovDebt),   // P5：初期国債（仏＝米独立戦争債務）。純負債＝貨幣非移動・保存中立。GovAvgCouponPerYearはrollover=1で初回Fiscalが市場金利へ上書き
            // ★以後は法が一次量として独立に動く＝政体を読み直すことは無い。〔履歴 Simulation.cs#4〕
            BudgetLaw = scenario.InitialPolity switch
            {
                Polity.Republic => BudgetLaw.Parliamentary,                       // 英米型＝歳入も歳出も議会の同意による
                Polity.ConstitutionalMonarchy => BudgetLaw.Partial,               // 普(第99/109条)・日(第67/71条)＝歳出の授権だけ
                _ => BudgetLaw.Decree,                                            // 露(1906年以前)・清＝勅令
            },
            CommandLaw = scenario.InitialPolity switch
            {
                Polity.Republic => CommandLaw.Parliament,                         // 英(反乱法)・仏第三共和政
                Polity.ConstitutionalMonarchy => CommandLaw.Split,                // 軍政は内閣・軍令は君主（明治憲法第11条・普の軍事内閣）
                _ => CommandLaw.Crown,
            },
            Bureaucracy = scenario.InitialBureaucracy,   // Bureaucracy〔履歴 Simulation.cs#5〕
            Latitude = scenario.NationConfig?.Latitude ?? 0,    // 首都の座標（多国＝距離で輸送マージンが嵩む）
            Longitude = scenario.NationConfig?.Longitude ?? 0, OpeningTreasuryMonths = scenario.NationConfig?.TreasuryMonths ?? 0, OpeningGovDebtToGdpYear = scenario.NationConfig?.GovDebtToGdpYear ?? 0, OpeningStateMonopolyTier = scenario.NationConfig?.OpeningStateMonopolyTier ?? 0, LandTaxBasisFixed = scenario.NationConfig?.LandTaxBasis == "Fixed",   // Longitude〔履歴 Simulation.cs#6〕
            EthnicEnabled = scenario.NationConfig is not null,   // 民族統一度（倍率排除⑧）は各popの CoreEthnicShare 加重平均から創発。NationConfig 由来の国だけ有効化＝合成較正/最小世界は無効で較正不変
            MonetaryEnabled = scenario.NationConfig is not null,       // 物価水準（インフレ/デフレ）も実在国だけ＝合成較正/最小世界は P≡1 で従来同値
            ParliamentEnabled = scenario.NationConfig is not null,     // 議会・選挙も実在国だけ＝合成較正は従来の政治モデルのまま（33/33/33不変）

        };
        // ⇒ 距離の基準を**経済中心地＝1番目の州**（エンジンが首都として扱い、分離もさせない州）へ移す。〔履歴 Simulation.cs#7〕
        if (World.Regions.Count > 0) World.SetCapital(World.Regions[0]);
        World.SetTreasuryRaw(scenario.InitialTreasury);   // 初期国庫＝フローでない（台帳に載せない）
        if (scenario.NationConfig is not null) World.FactionRng = new DeterministicRng(scenario.Seed ^ 0xFAC7_1035_EED5UL); if (scenario.NationConfig is not null) World.AiRng = new DeterministicRng(scenario.Seed ^ 0xA1C0_FFEE_5EED_0337UL);   // xA1C0_FFEE_5EED_0337UL〔履歴 Simulation.cs#8〕
        World.FoundingActiveProvinces += regions.Count;   // Count〔履歴 Simulation.cs#9〕
        if (scenario.PrebuiltRegions is null) foreach (var r1 in regions) { r1.AnchorPopulation = r1.SumPopulation(); if (Catalog.AdminId < r1.Markets.Length) r1.AdminFill = 0.0; } World.FoundingPopulation = World.TotalPopulation;   // 建国の子の州〔PrebuiltRegions〕は親の値のまま・植民地の州昇格は 0）。〔履歴 Simulation.cs#10〕
        // 倍率排除⑧：各popの中核民族割合を国の初期統一度で初期化（以後 移民[core=0で希釈]/同化/征服で per-pop に変動＝EthnicUnity が加重平均から創発）。〔履歴 Simulation.cs#11〕
        if (World.EthnicEnabled && scenario.PrebuiltRegions is null)
        {
            const double u0 = 1.0;   // u0〔履歴 Simulation.cs#12〕
            string majority = "";
            {
                var byCulture = new Dictionary<string, double>();
                foreach (var r in World.Regions)
                {
                    if (string.IsNullOrEmpty(r.Culture)) continue;
                    double rp = 0; foreach (var q in r.Pops) rp += q.Size;
                    byCulture[r.Culture] = byCulture.GetValueOrDefault(r.Culture) + rp;
                }
                double bestP = 0; foreach (var kv in byCulture) if (kv.Value > bestP || (kv.Value == bestP && kv.Value > 0 && string.CompareOrdinal(kv.Key, majority) < 0)) { bestP = kv.Value; majority = kv.Key; }   // 同点はオルディナル順で確定（Dictionary列挙順への依存を排除）
            }
            foreach (var r in World.Regions)
            {
                double core = u0;
                if (majority.Length > 0 && !string.IsNullOrEmpty(r.Culture))
                    core = r.Culture == majority ? 1.0 : 0.0;   // 一致州 1.0／不一致州 0.0（旧の混ぜ u0+g×(x−u0) は u0=g=1 で x そのもの）
                foreach (var p in r.Pops) p.CoreEthnicShare = new Share(core);
            }
        }   // 持ち込み州（建国）は民族構成を上書きしない（スポーン側で新中核へ再アンカー済み）
        // 政策の初期値＝シナリオの現状（目標を現在値に置く→NullAgent では何も動かない＝後方互換）
        World.Policy = new NationPolicy
        {
            EducationLaw = scenario.EducationLaw,
            IncomeTaxTarget = scenario.TaxRate,
            PollTaxTarget = scenario.InitialPollTax,
            LandTaxTarget = scenario.InitialLandTax,
            EducationSpendingTarget = scenario.EducationSpending,
            CoercionTarget = scenario.BaseCoercion,
            InterestRateTarget = scenario.InitialInterestRate,
            TariffTarget = scenario.InitialTariff,
            Suffrage = scenario.InitialSuffrage,            // 初期参政権（史実1789：英米蘭＝制限選挙／他＝なし）
            Censorship = scenario.InitialCensorship,        // 初期報道法（米蘭＝自由／英＝検閲付／他＝国家報道のみ）
            CornLaws = scenario.InitialCornLaws,            // 初期穀物法（農業国＝保護／自由貿易国＝なし）
            SerfEmancipation = scenario.InitialSerfEmancipation,   // 初期農奴解放（東欧＝未解放／西欧＝全面）
            ImmigrationRestriction = scenario.InitialImmigration,  // 初期移民制限法（日朝＝閉鎖／清＝選別／他＝開放）
            ConscriptionLaw = scenario.InitialConscription,        // ConscriptionLaw〔履歴 Simulation.cs#13〕
            // 金本位の志向は AI/プレイヤーが決める（UtilityAgent は信用があれば志向＝史実の規範。NullAgent のテストは志向せず＝従来挙動）。実加入は信用ゲートが決める
        };
        World.Suffrage = World.Policy.Suffrage;       // 法整備の現在値＝初期は政策の初期値（利益集団のゲートで漸進）
        World.Censorship = World.Policy.Censorship;   // 報道法の現在値＝初期値（米蘭＝自由／英＝検閲付／他＝国家報道のみ）
        World.ConscriptionLaw = World.Policy.ConscriptionLaw;   // ConscriptionLaw〔履歴 Simulation.cs#14〕
        // P2：登録簿（Crown/Seizure）を初期政体から構築（英CM→Crown=Absolutist・米Rep→None等）。flag=0ではdead write＝挙動恒等。
        CrownParliament.ApplyRegime(World, World.Coef, World.Polity); if (Instruments.On) Instruments.CrownSource(World, 0);   // ApplyRegime〔履歴 Simulation.cs#15〕
        // 政体が含意する参政権・報道自由を初期化時に法へ一回畳み込む（倍率排除②：以後 Freedom/eff＝法律値。〔履歴 Simulation.cs#16〕
        if (scenario.PoliticsEnabled) Politics.RaiseFoundingInstitutions(World);
        World.Freedom = scenario.PoliticsEnabled ? Politics.FreedomFromLaws(World.Suffrage, World.Censorship) : scenario.Freedom;   // 倍率排除②：探究の自由＝参政権＋報道法から創発（floor-raise 後に算出）
        World.LaborUnion = World.Policy.LaborUnion;   // 労働組合の現在値＝初期値（1789＝非合法）
        World.WorkHours = World.Policy.WorkHours;     // 労働時間の法定上限＝初期値（1789＝無制限）。以後 Government が政治ロック越しに漸進（A-1#2）
        World.WorkHoursValue = World.WorkHours.Hours();   // 実労働時間の初期値＝法定上限のアンカー（無制限＝14時間）。以後 MonthSettle で連続追従（倍率排除②）
        World.WomenWageRetention = World.WomenPropertyLaw ? 1.0 : 0.0;       // 賃金保持の初期値＝財産権法のアンカー。以後 MonthSettle で連続追従（倍率排除④）
        World.ChildLaborAllowed = World.Policy.ChildLaborAllowed;            // ChildLaborAllowed〔履歴 Simulation.cs#17〕
        World.ChildLaborLegal = World.ChildLaborAllowed ? 1.0 : 0.0;  // 児童労働の合法度の初期値＝児童労働法のアンカー。以後 MonthSettle で連続追従（倍率排除④）
        World.CornLaws = World.Policy.CornLaws;             // 穀物法の現在値＝初期値（農業国＝保護／自由貿易国＝なし）
        World.SerfEmancipation = World.Policy.SerfEmancipation;   // 農奴解放の現在値＝初期値（東欧＝未解放／西欧＝全面）
        World.ImmigrationRestriction = World.Policy.ImmigrationRestriction;   // 移民制限法の現在値＝初期値
        World.MonetaryLaw = scenario.InitialMonetaryLaw;          // MonetaryLaw〔履歴 Simulation.cs#18〕
        World.Policy.MonetaryTarget = (int)World.MonetaryLaw;    // 目標も現在値に揃える（官僚制度と同じ＝翌期に巻き戻さない）
        // 助走の前から持つ（助走の盤面も知った国の動力で均衡する）。〔履歴 Simulation.cs#19〕
        foreach (var name in scenario.InitialDiscoveries)
        {
            int row = Catalog.DiscoveryRowNamed(name);
            if (row < 0) throw new System.InvalidOperationException($"開幕の既知の発見「{name}」は発見の表に無い（world.json の knownDiscoveries）");
            World.Discovered[row] = true;
        }
        // 正なら発見で開く。〔履歴 Simulation.cs#20〕
        Discovery.SeedCompatRows(World);
        // 軍事費の現在値＝初期目標（兵士の徴募/給与を駆動）。〔履歴 Simulation.cs#21〕
        World.Policy.Bureaucracy = World.Bureaucracy;   // Bureaucracy〔履歴 Simulation.cs#22〕
        World.SetMilitarySpending(new Share(PoliticalQuantities.Clamp01(World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget)),
                                  new Share(World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget > 1e-9
                                      ? World.Policy.NavyUpkeepTarget / (World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget) : 0.5));
        World.Tariff = scenario.InitialTariff;                          // 関税の現在値＝初期値（日本＝鎖国。開国まで世界市場から孤立）
        World.InterestRate = scenario.InitialInterestRate;
        // 既定0=不走行＝恒等〔履歴 Simulation.cs#23〕
        if (scenario.InitialMassMilitancy > 0)
            foreach (var (_, p) in World.AllPopsWithRegion())
                if (Politics.IsMassPop(p)) p.Militancy = scenario.InitialMassMilitancy;
        // 開幕は0）。〔履歴 Simulation.cs#24〕
        MoneyStock popDebt = default; foreach (var (_, p) in World.AllPopsWithRegion()) popDebt += p.Debt;
        World.Bank = new Bank { Gold = World.TotalMoney, Loans = default, Confidence = 1.0, HouseholdLoans = popDebt };

        Fiscal.SeedBondHoldings(World);   // SeedBondHoldings〔履歴 Simulation.cs#25〕
        _invariants = [];
        RebaselineInvariants();
    }

    /// <summary>不変条件の基準線（基礎貨幣・人口）を現在値で取り直す。開始盤面で統治状態（国庫・与信）を初期へ戻すと
    /// 基礎貨幣が交易を伴わず不連続に変わるため、その直後に呼んで保存則の基準を現状へ更新する（相対基準なので整合する）。</summary>
    public void RebaselineInvariants()
    {
        _invariants.Clear();
        _invariants.Add(new MoneyConservation(World));
        _invariants.Add(new PopulationConservation(World));
        _invariants.Add(new NonNegativeBounds());
        _invariants.Add(new GoodsBalance());
        _invariants.Add(new BondLedger());   // Add〔履歴 Simulation.cs#26〕
    }

    // ◆玩具の世界ここから（2026-09-30 段0-1）：下の単一州の最小シナリオは Catalog を使わず、財 id は各シナリオの中だけの番号（Catalog の財 id ではない）＝番人「数値の財 id を書かない」の対象外。
    private static List<Region> BuildSelfSufficientFarm()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var farmer = new Pop { ClassId = 0, ClassName = "自作農", IsUrban = false, Size = 1000, Savings = new MoneyStock(1_000_000), Satisfaction = 1.0 };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return [new Region { Id = 0, Name = "本土", IsPort = true, Goods = [food], Pops = [farmer], Producers = [rgo], Markets = [market] }];   // 港ゲート後：交易デモは海上交易できる沿岸州（IsPort）。較正はAgrarianToUrban別シナリオなので無影響
    }

    private static List<Region> BuildAgrarianToUrban()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var manuf = new Good { Id = 1, Name = "工業品", Tier = GoodTier.Daily, BaseNeed = 0.5, RefPrice = 1.0, Elasticity = 0.6 };

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 900, Savings = new MoneyStock(900_000), Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = 100, Savings = new MoneyStock(100_000), Satisfaction = 1.0 };

        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 720, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };
        var factory = new Producer { GoodId = 1, Productivity = 1.5, Output = 120, MarginalCost = 0.6, OwnerPopIndex = 1, LaborPopIndex = 1, RentShare = default };

        var foodMkt = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        var manufMkt = new Market { Price = 1.0, Stock = 600, TargetStock = 600 };

        return [new Region
        {
            Id = 0, Name = "本土",
            Goods = [food, manuf], Pops = [farmer, worker],
            Producers = [rgo, factory], Markets = [foodMkt, manufMkt],
        }];
    }

    /// <summary>
    /// 段4c：本拠地（満杯）＋辺境（豊かな土地・人はまばら）の 2 州。〔履歴 Simulation.cs#27〕
    /// </summary>
    private static List<Region> BuildFrontierDevelopment()
    {
        Region MakeRegion(int id, string name, double bonus, double capacity, double farmerSize, double savings)
        {
            var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
            var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = farmerSize, Savings = new MoneyStock(savings), Satisfaction = 1.0 };
            var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = farmerSize * 0.85 * bonus, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };
            var market = new Market { Price = 1.0, Stock = 1000, TargetStock = 1000 };
            var region = new Region
            {
                Id = id, Name = name, ProductivityBonus = bonus, ArableLand = capacity,
                Goods = [food], Pops = [farmer], Producers = [rgo], Markets = [market],
            };
        return region;
        }

        var core = MakeRegion(0, "本土", bonus: 1.0, capacity: 1000, farmerSize: 1000, savings: 1_000_000);
        var frontier = MakeRegion(1, "辺境", bonus: 2.5, capacity: 1500, farmerSize: 10, savings: 10_000);
        return [core, frontier];
    }

    /// <summary>局地飢饉の検証用：2つの active 州。州0＝食料余剰（農民多）、州1＝食料大幅不足（労働者多・自給僅少）。
    /// 世界市場なし（単一国の世界）なので州1は輸入できず飢える＝POP の飢えの上乗せ（DemographicStep）で州1だけ人口が減る。</summary>
    private static List<Region> BuildRegionalFamine()
    {
        Region Make(int id, string name, double farmerSize, double workerSize, double foodOutput)
        {
            var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
            var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = farmerSize, Savings = new MoneyStock(farmerSize * 1000), Satisfaction = 1.0 };
            var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = workerSize, Savings = new MoneyStock(workerSize * 1000), Satisfaction = 1.0 };
            var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = foodOutput, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };
            var mkt = new Market { Price = 1.0, Stock = foodOutput, TargetStock = foodOutput };
            return new Region { Id = id, Name = name, Goods = [food], Pops = [farmer, worker], Producers = [rgo], Markets = [mkt] };
        }
        var rich = Make(0, "豊作州", farmerSize: 1500, workerSize: 200, foodOutput: 1500);   // 余剰（自給十分・国平均を引き上げる大州）
        var poor = Make(1, "不足州", farmerSize: 50, workerSize: 450, foodOutput: 60);        // 大幅不足の小州（輸入できず国平均より大きく飢える）
        return [rich, poor];
    }

    /// <summary>
    /// 放任経済（§2.3.1/§2.4.2/§0）：農民（食料・自営）＋工場労働者＋資本家（工場所有・利潤吸収）。
    /// 資本家が工業品利潤の大半を取り（搾取）溜め込む（需要に回らない）→ 労働者は工業品を買えず
    /// 工業の需要が縮む（過少消費）。介入（再分配・需要管理）なしで経済が立ち行かなくなるかを見る。
    /// </summary>
    private static List<Region> BuildLaissezFaire(double factoryRentShare)
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var manuf = new Good { Id = 1, Name = "工業品", Tier = GoodTier.Daily, BaseNeed = 0.6, RefPrice = 1.0, Elasticity = 0.6 };

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 700, Savings = new MoneyStock(700_000), Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = 250, Savings = new MoneyStock(50_000), Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 2, ClassName = "資本家", IsUrban = true, Size = 50, Savings = new MoneyStock(50_000), Satisfaction = 1.0 };

        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 560, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };       // 自営農
        var factory = new Producer { GoodId = 1, Productivity = 1.5, Output = 200, MarginalCost = 0.6, OwnerPopIndex = 2, LaborPopIndex = 1, RentShare = new Share(factoryRentShare) };    // 資本家が取る利潤割合＝搾取度

        var foodMkt = new Market { Price = 1.0, Stock = 1000, TargetStock = 1000 };
        var manufMkt = new Market { Price = 1.0, Stock = 600, TargetStock = 600 };

        return [new Region
        {
            Id = 0, Name = "本土",
            Goods = [food, manuf], Pops = [farmer, worker, capitalist],
            Producers = [rgo, factory], Markets = [foodMkt, manufMkt],
        }];
    }

    /// <summary>
    /// 多段生産チェーン（§2.4）。tier1 原料（木・鉄鉱石）→ tier2 素材（木材←木／鉄←鉄鉱石）→
    /// tier3（鉄鋼←鉄）→ tier4（家具←木材＋鉄鋼＝2素材）。pop は食料（必需）と家具（日用）を消費し、
    /// その需要が連鎖を遡って原料まで波及する。RGO は自営、工場は資本家所有・工員労働。
    /// ※工員は4工場で共用（部門間の労働配分は最小核では未実装の簡略化）。
    /// </summary>
    private static List<Region> BuildProductionChain()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var furniture = new Good { Id = 1, Name = "家具", Tier = GoodTier.Daily, BaseNeed = 0.4, RefPrice = 1.0, Elasticity = 0.6 };
        var lumber = new Good { Id = 2, Name = "木材", Tier = GoodTier.Intermediate, BaseNeed = 0, RefPrice = 1.0, Elasticity = 0.4 };
        var iron = new Good { Id = 3, Name = "鉄", Tier = GoodTier.Intermediate, BaseNeed = 0, RefPrice = 1.0, Elasticity = 0.4 };
        var steel = new Good { Id = 4, Name = "鉄鋼", Tier = GoodTier.Intermediate, BaseNeed = 0, RefPrice = 1.0, Elasticity = 0.4 };
        var wood = new Good { Id = 5, Name = "木", Tier = GoodTier.Intermediate, BaseNeed = 0, RefPrice = 1.0, Elasticity = 0.4 };
        var ore = new Good { Id = 6, Name = "鉄鉱石", Tier = GoodTier.Intermediate, BaseNeed = 0, RefPrice = 1.0, Elasticity = 0.4 };

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 400, Savings = new MoneyStock(400_000), Satisfaction = 1.0 };
        var woodcutter = new Pop { ClassId = 1, ClassName = "木こり", IsUrban = false, Size = 100, Savings = new MoneyStock(100_000), Satisfaction = 1.0 };
        var miner = new Pop { ClassId = 2, ClassName = "鉱夫", IsUrban = false, Size = 100, Savings = new MoneyStock(100_000), Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 3, ClassName = "工員", IsUrban = true, Size = 350, Savings = new MoneyStock(200_000), Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 4, ClassName = "資本家", IsUrban = true, Size = 50, Savings = new MoneyStock(100_000), Satisfaction = 1.0 };

        var pFood = new Producer { GoodId = 0, Productivity = 2.2, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = default };
        var pWood = new Producer { GoodId = 5, Productivity = 3.0, Output = 200, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 1, RentShare = default };
        var pOre = new Producer { GoodId = 6, Productivity = 3.0, Output = 200, MarginalCost = 0.5, OwnerPopIndex = 2, LaborPopIndex = 2, RentShare = default };
        var pLumber = new Producer { GoodId = 2, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = new Share(0.3), Inputs = [new Input(5, 1.0)] };
        var pIron = new Producer { GoodId = 3, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = new Share(0.3), Inputs = [new Input(6, 1.0)] };
        var pSteel = new Producer { GoodId = 4, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = new Share(0.3), Inputs = [new Input(3, 1.0)] };
        var pFurn = new Producer { GoodId = 1, Productivity = 1.5, Output = 400, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = new Share(0.3), Inputs = [new Input(2, 0.5), new Input(4, 0.5)] };

        Market Mk(double s) => new Market { Price = 1.0, Stock = s, TargetStock = s };

        return [new Region
        {
            Id = 0, Name = "本土",
            Goods = [food, furniture, lumber, iron, steel, wood, ore],
            Pops = [farmer, woodcutter, miner, worker, capitalist],
            Producers = [pFood, pWood, pOre, pLumber, pIron, pSteel, pFurn],
            Markets = [Mk(1000), Mk(500), Mk(300), Mk(300), Mk(300), Mk(300), Mk(300)],
        }];
    }
    // ◆玩具の世界ここまで

    /// <summary>
    /// カタログ全財のフルチェーン（§2.4・Catalog）。各財に生産者を1つ生成：RGO は一次労働者の自営、工場は工員労働・資本家所有。
    /// Goods は消費優先（tier 昇順）で並べ予算配分の順序とする。※工員/一次労働者は全工場/全RGOで共用（労働配分は簡略）。
    /// </summary>
    // 工業化スケール：労働配分後（総能力＝pop.Size）に1人が全財×多段の消費を賄うには高い生産性が要る。
    // 各労働者の産出 ≈ 生産性、消費 ≈ Σ需要×段数 ≈ 4〜5/人。生産性をこの倍率で底上げ＝工業化（§8 較正）。
    private const double FullChainProductivityScale = 7.0;

    private static List<Region> BuildFullChain()
    {
        int n = Catalog.Goods.Length;
        var goodsById = new Good[n];
        var markets = new Market[n];
        var producers = new List<Producer>(n);

        foreach (var d in Catalog.Goods)
        {
            double refP = Catalog.RefPrices[d.Id];   // 参照価格＝労働込みの実費用連鎖（深い連鎖ほど高い＝自然な価格勾配。役務は固定錨）
            markets[d.Id] = new Market { Price = refP, Stock = 200, TargetStock = 200 };
            // 教育・医療・行政・兵力は担い手pop（教師・医者・官僚・兵士）が居ない簡易世界では持たない（市場の器だけ＝休眠。識字は Fill=1.0 の従来挙動）。
            if (d.Tier == GoodTier.Service && d.Id != Catalog.LaborId && d.Id != Catalog.AdvLaborId && d.Id != Catalog.FarmLaborId) continue;
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, WearRatePerMonth = d.WearRatePerMonth, NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            if (d.Tier == GoodTier.Service) continue;   // 役務（労働力・高度労働力・農業労働力）の生産者は下で紐づけて追加
            if (d.Id == Catalog.BuildingId && !(new Coefficients().BuildingGoodBundle > 0)) continue;   // 本番の州の構築は CatalogEconomy が旗を読む）〔履歴 Simulation.cs#28〕
            bool isFactory = d.Inputs.Length > 0;
            // 雇用の一本化：工場＝労働力＋高度労働力、自家農家（RGO）＝農業労働力を市場で買う（自作農＝一次労働者が所有）。
            producers.Add(new Producer
            {
                GoodId = d.Id,
                Productivity = d.Productivity * FullChainProductivityScale,   // 工業化の表現（§8較正・労働配分後の規模合わせ）
                Output = 50,
                MarginalCost = isFactory ? Catalog.FactoryVa : Catalog.RgoVa,   // 自家農家＝0.34＋農業労働0.16≈旧0.5（供給曲線の損益分岐を維持＝過剰生産を防ぐ）
                LaborPopIndex = isFactory ? 1 : 0,
                OwnerPopIndex = isFactory ? 2 : 0,
                RentShare = new Share(isFactory ? 1.0 : 0.3),    // 自作農の自己地代（土地税の課税ベース。所有者＝労働者なので同じ懐）
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }

        var goodsList = new List<Good>();
        foreach (var g in goodsById) if (g is not null) goodsList.Add(g);
        // この並びは MarketFlow の価格更新順と   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。〔履歴 Simulation.cs#29〕
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順

        var laborer = new Pop { ClassId = 0, ClassName = "一次労働者", IsUrban = false, Size = 500, Savings = new MoneyStock(500_000), Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工員", IsUrban = true, Size = 450, Savings = new MoneyStock(450_000), Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 2, ClassName = "資本家", IsUrban = true, Size = 50, Savings = new MoneyStock(100_000), Satisfaction = 1.0 };
        // 労働力：工員が算出し工場が買う（賃金＝労働市場の価格）。生産性は工場と同じスケール＝供給が工場群と釣り合う。
        producers.Add(new Producer { GoodId = Catalog.LaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = worker.Size * 2.5, MarginalCost = 0.35, LaborPopIndex = 1, OwnerPopIndex = 1, RentShare = default });
        // 高度労働力：技師の居ない簡易世界＝工員が兼ねる（小さな供給）。石油火力も工員で。
        producers.Add(new Producer { GoodId = Catalog.AdvLaborId, Productivity = 0.5, Output = worker.Size * 0.2, MarginalCost = 0.5, LaborPopIndex = 1, OwnerPopIndex = 1, RentShare = default });
        // 農業労働力：一次労働者が算出し自家農家（RGO）が買う。
        producers.Add(new Producer { GoodId = Catalog.FarmLaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = laborer.Size * 0.5, MarginalCost = 0.3, LaborPopIndex = 0, OwnerPopIndex = 0, RentShare = default });
        // 原油0.6＝石炭0.9のエネルギー等価1/1.5）。〔履歴 Simulation.cs#30〕
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "石油火力", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.498), new Input(Catalog.CrudeOilId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 石炭が高い州でのみ火力に勝つ （燃料の地理が電源を分ける＝スイス/伊/ノルウェー型の水力国が価格から創発）。〔履歴 Simulation.cs#31〕
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "水力発電", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.647), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 魚の缶詰（缶詰の第2レシピ）：漁獲を缶詰める＝漁獲に第2の消費先（従来は干物のみ）。市場が安い方を選ぶ。
        producers.Add(new Producer { GoodId = Catalog.CannedFoodId, RecipeName = "魚の缶詰", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.FishId, 0.55), new Input(Catalog.IronId, 0.3), new Input(Catalog.TinOreId, 0.05), new Input(Catalog.SaltId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CannedFoodId))] });

        AddAlternateRecipes(producers, new Coefficients());   // AddAlternateRecipes〔履歴 Simulation.cs#32〕
        return [new Region
        {
            Id = 0, Name = "本土",
            Goods = goodsList,
            Pops = [laborer, worker, capitalist],
            Producers = producers,
            Markets = markets,
        }];
    }

    /// <summary>
    /// 史実フランス1789（旧体制）。三身分を経済の役に割り当てる：
    /// RGO（農業・原料）は農民が耕し、地代（封建貢租）を特権身分（貴族・聖職）が抜く＝搾取構造。
    /// 工場は都市労働者が働きブルジョワが所有。識字は身分で大差＝近代化の伏線。代表スケール 1単位＝1000人。
    /// </summary>
    private static List<Region> BuildFrance()
    {
        int n = Catalog.Goods.Length;
        var goodsById = new Good[n];
        var markets = new Market[n];
        var producers = new List<Producer>(n);

        // pop index：0=貴族, 1=農民, 2=都市労働者, 3=ブルジョワ, 4=技師, 5=聖職者, 6=教師, 7=官僚, 8=医者
        foreach (var d in Catalog.Goods)
        {
            double refP = Catalog.RefPrices[d.Id];   // 参照価格＝労働込みの実費用連鎖（深い連鎖ほど高い＝自然な価格勾配。役務は固定錨）
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, WearRatePerMonth = d.WearRatePerMonth, NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            markets[d.Id] = new Market { Price = refP, Stock = 200, TargetStock = 200 };
            if (d.Tier == GoodTier.Service) continue;   // 役務は AddServiceProducers が担い手popに紐づけて追加
            if (d.Id == Catalog.BuildingId && !(new Coefficients().BuildingGoodBundle > 0)) continue;   // BuildingGoodBundle〔履歴 Simulation.cs#33〕
            bool isFactory = d.Inputs.Length > 0;
            // 雇用の一本化：工場＝労働力＋高度労働力、自家農家（RGO）＝農業労働力を市場で買う。残余＝地代（旧体制＝小作0.5を貴族へ）。
            producers.Add(new Producer
            {
                GoodId = d.Id,
                Productivity = d.Productivity * FullChainProductivityScale,
                Output = 50,
                MarginalCost = isFactory ? Catalog.FactoryVa : Catalog.RgoVa,   // 自家農家＝0.34＋農業労働0.16≈旧0.5（供給曲線の損益分岐を維持＝過剰生産を防ぐ）
                LaborPopIndex = isFactory ? 2 : 1,     // 能力の按分基準：自家農家＝農民／工場＝都市労働者
                OwnerPopIndex = isFactory ? 3 : 0,     // 工場＝ブルジョワ／農地＝貴族（封建地代を抜く）
                RentShare = new Share(isFactory ? 1.0 : 0.5),     // 農地の残余の半分＝封建貢租・残り半分は耕す農民へ
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }

        var goodsList = new List<Good>(goodsById);
        // この並びは MarketFlow の価格更新順と   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。〔履歴 Simulation.cs#34〕
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順

        // 身分・階級（代表スケール 1単位＝1000人・計≈28,000＝人口2800万）。識字は階級で大差。
        var nobility     = new Pop { ClassId = 0, ClassName = "貴族",       IsUrban = false, Size = 300,   Savings = new MoneyStock(900_000),   Satisfaction = 1.0, Literacy = 0.95, Closed = true };       // 世襲＝移民しない
        var clergy       = new Pop { ClassId = 6, ClassName = "聖職者",     IsUrban = false, Size = 200,   Savings = new MoneyStock(600_000),   Satisfaction = 1.0, Literacy = 0.92, Closed = true };       // 宗教学校の教育を算出（世俗化で没落）
        var peasant      = new Pop { ClassId = 1, ClassName = "農民",       IsUrban = false, Size = 20000, Savings = new MoneyStock(4_000_000), Satisfaction = 1.0, Literacy = 0.15, SubsistenceBasis = 0.7 };   // 食料の多くを自給
        var worker       = new Pop { ClassId = 2, ClassName = "工場労働者", IsUrban = true,  Size = 4200,  Savings = new MoneyStock(1_260_000), Satisfaction = 1.0, Literacy = 0.40 };   // 労働力を算出し工場に売る
        var bourgeois    = new Pop { ClassId = 3, ClassName = "資本家",     IsUrban = true,  Size = 2500,  Savings = new MoneyStock(8_000_000), Satisfaction = 1.0, Literacy = 0.80, IsModernElite = true };   // 深い連鎖の投資元手（§8較正までのバッファ）
        var engineer     = new Pop { ClassId = 4, ClassName = "技師",       IsUrban = true,  Size = 520,   Savings = new MoneyStock(416_000),   Satisfaction = 1.0, Literacy = 0.85, IsModernElite = true };  // 工場の生産性（近代エリート）
        var teacher      = new Pop { ClassId = 7, ClassName = "教師",       IsUrban = true,  Size = 112,   Savings = new MoneyStock(89_600),    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 公教育の教育を算出
        var bureaucrat   = new Pop { ClassId = 8, ClassName = "官僚",       IsUrban = true,  Size = 98,    Savings = new MoneyStock(78_400),    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 行政を算出
        var doctor       = new Pop { ClassId = 9, ClassName = "医者",       IsUrban = true,  Size = 70,    Savings = new MoneyStock(56_000),    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 医療を算出
        var merchant     = new Pop { ClassId = 10, ClassName = "商人",      IsUrban = true,  Size = 500,   Savings = new MoneyStock(750_000),   Satisfaction = 1.0, Literacy = 0.70, IsModernElite = true };  // 流通マージン＋貿易の仲立ち
        var artisan      = new Pop { ClassId = 11, ClassName = "職人", IsUrban = true,  Size = 1500,  Savings = new MoneyStock(1_050_000), Satisfaction = 1.0, Literacy = 0.45 };  // 手工業の自営（ギルド）。※この試験世界は銀行なし＋工場が成熟済＝初日から競争に負けるため、世紀を生き延びる蓄え（700/人）を持たせる

        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, clergy, teacher, bureaucrat, doctor, merchant, artisan };
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 6, bureaucratIdx: 7, doctorIdx: 8);
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, new Coefficients());   // 高度労働力（技師）＋石油火力
        AddArtisanProducers(producers, artisanIdx: pops.Count - 1);   // 職人の工房（工場との並存レシピ）
        AddAlternateRecipes(producers, new Coefficients());   // AddAlternateRecipes〔履歴 Simulation.cs#35〕
        return [new Region
        {
            Id = 0, Name = "フランス本土",
            CapitalEnabled = true,        // 集約「工業化度」を有効化（投資で積み、都市化を工業化に縛る・生産性を上げる）
            Industrialization = 0.0,      // 1789＝前工業（工業化度0から投資で積む）
            InvestorPopIndex = 3,         // 投資の出し手＝ブルジョワ
            MeritFromIndex = 2,           // メリトクラシー昇格：都市労働者 →
            MeritToIndex = 4,             //   専門職・技師（識字が高いほど・M が高いほど昇る）
            ClergyPopIndex = 5, TeacherPopIndex = 6, BureaucratPopIndex = 7, DoctorPopIndex = 8, MerchantPopIndex = 9, ArtisanPopIndex = 10,
            EducationProducerIndex = eduIdx,
            Goods = goodsList,
            Pops = pops,
            Producers = producers,
            Markets = markets,
        }];
    }

    /// <summary>
    /// 汎用国家モデル（§6.1 多国）。BuildFrance を <see cref="NationConfig"/> 駆動に一般化したもの。
    /// 同じカタログ全財の経済を、階級サイズ・識字・初期工業化度・資源生産性で差別化する。
    /// 役割割り当て（RGO＝農民／生産財＝専門職／工場＝都市労働者・ブルジョワ所有）は共通。
    /// </summary>
    private static List<Region> BuildNation(NationConfig cfg, Coefficients coef, bool sharedWorld = false)
    {
        var regions = new List<Region>();
        int rid = 0;
        // 国土を州（代表都市）に分割し、各階級を Share で按分する。Provinces 未指定なら単一地域（後方互換）。首都＝Id 0。
        var provinces = cfg.Provinces.Length > 0 ? cfg.Provinces : [new ProvinceSpec(cfg.Name, 1.0, [])];
        foreach (var p in provinces)
            regions.Add(BuildProvince(rid++, p, cfg, coef, sharedWorld));
        // 5件（蝦夷地・西部・ウラル・シベリア・ウラジオストク）は普通の州へ畳んだ＝州は1つのスキーマだけになった。〔履歴 Simulation.cs#36〕
        WirePortDistances(regions);
        // 対象は軍需の最終財のみ（火砲81/小銃82/弾薬83/軍艦86＝InitialArmsIndustryGoods との交差）。〔履歴 Simulation.cs#37〕
        if (coef.InitialArsenalMonths > 0)
        {
            // 経済の側の定員は歳入を読み助走の前はほぼ0（構築時の EstablishmentOf は人口の側の約7分の1）なので使えない。〔履歴 Simulation.cs#38〕
            double manpower = 0;
            foreach (var r in regions) manpower += Military.RegionManpower(r, coef);
            double natSoldiers = coef.MaxSoldierFraction.Value * manpower;
            var capReg = regions[0];
            for (int b = 0; b < coef.MilitaryGoods.Length; b++)
            {
                int g = coef.MilitaryGoods[b];
                if (g < 0 || g >= capReg.Markets.Length) continue;
                if (System.Array.IndexOf(InitialArmsIndustryGoods, g) < 0) continue;
                double monthly = coef.MilitaryUpkeepScale * natSoldiers * coef.MilitaryRates[b];
                capReg.Markets[g].Stock = System.Math.Max(capReg.Markets[g].Stock, monthly * coef.InitialArsenalMonths);
                capReg.Markets[g].TargetStock = System.Math.Max(capReg.Markets[g].TargetStock, monthly);   // 月次需要1ヶ月分＝MarketFlow の在庫目標（TargetStockBuffer=1.0×需要）と同じ物差し
            }
        }
        return regions;
    }

    /// <summary>
    /// 従来 NearestPortDistanceKm はどこからも代入されず、内陸の鉄道の減衰（旧の係数 InlandPortDistanceDecay〔dormant〕）を有効化しても減衰式が不発だった（#17較正の阻害）。〔履歴 Simulation.cs#39〕
    /// </summary>
    internal static void WirePortDistances(List<Region> regions, bool rewire = false)
    {
        if (rewire)
            foreach (var r in regions)
                if (!r.IsPort) { r.NearestPortName = ""; r.NearestPortDistanceKm = 0; r.PortLandKm = 0; }
        var ports = new List<(double Lat, double Lon, string Name)>();
        foreach (var r in regions)
            if (r.IsPort) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) { ports.Add((pc.Lat, pc.Lon, r.Name)); r.SeaBasin = SeaGraph.BasinAt(pc.Lat, pc.Lon); } }
        if (ports.Count == 0) return;
        foreach (var r in regions)
        {
            if (r.IsPort) continue;
            var cc = Climate.CityCoord(r.Name);
            if (cc.Lat == 0 && cc.Lon == 0) continue;
            var pick = PortChoice(cc.Lat, cc.Lon, ports);   // pick〔履歴 Simulation.cs#40〕
            r.NearestPortDistanceKm = pick.LandKm;   // NearestPortDistanceKm〔履歴 Simulation.cs#41〕
            r.NearestPortName = pick.Name;   // 03 §12-3：どの港かを記憶（キエフ/ハリコフ→ロストフ＝穀物動線がデータを足さずに出る）
            // 03手順4A：陸脚＝陸グラフの最短路（山脈迂回込み）。孤立成分（別陸塊等）は大圏で退避＝0を残さない（0=港州の意味）
            r.PortLandKm = pick.LandKm;
            var bp = Climate.CityCoord(pick.Name);
            r.SeaBasin = SeaGraph.BasinAt(bp.Lat, bp.Lon);   // 03手順5：内陸州＝最寄り港の海盆を継承（キエフ→ロストフ＝黒海勢が自動で出る・§12-3）
        }
    }

    /// <summary>
    /// ports＝同じ国の港（座標つき）の並び。〔履歴 Simulation.cs#42〕
    /// </summary>
    internal static (string Name, double GcKm, double LandKm) PortChoice(double lat, double lon, List<(double Lat, double Lon, string Name)> ports)
    {
        // 1 パスで両方の最小を持つ＝全ての港の eff の最小（bestEff）と、陸路が有限の港だけの陸路の最小（finEff）。
        double bestEff = double.MaxValue, bestGc = 0; string pickName = "";
        string lmR = Landmass.Of(lat, lon);
        double finEff = double.MaxValue, finGc = 0; string finName = ""; bool sameFin = false;
        foreach (var p in ports)
        {
            double d = Geo.DistanceKm(lat, lon, p.Lat, p.Lon);
            double l = PathGraph.LandDistanceKm(lat, lon, p.Lat, p.Lon);
            bool fin = double.IsFinite(l);
            double e = fin ? l : d;
            if (e < bestEff) { bestEff = e; bestGc = d; pickName = p.Name; }
            if (fin)
            {
                if (e < finEff) { finEff = e; finGc = d; finName = p.Name; }
                if (!sameFin && Landmass.Of(p.Lat, p.Lon) == lmR) sameFin = true;
            }
        }
        return sameFin ? (finName, finGc, finEff) : (pickName, bestGc, bestEff);
    }

    /// <summary>1つの州（代表都市）を生成。国全体の各階級を Share で按分し、独立したカタログ全財の経済を持たせる（州ごとに世界市場と交易）。</summary>
    // 港州（§5.1拡張）：世界市場と直接交易できる代表的な港町。〔履歴 Simulation.cs#43〕


    // 州ごとの資源（NationConfig の Provinces で指定）：列挙財を増産（地理的な得意＝輸出）、非列挙RGOは減産（要輸入＝交易を生む）。〔履歴 Simulation.cs#44〕
    private const double ResourceScarcity = 0.4;   // 非列挙RGOの産出（賦存に乏しい＝最低限の自給はあるが輸入依存。ゼロにしない＝封鎖即飢饉を回避）
    internal static double ResourceEndowment(int good) => Catalog.IsMineral(good) ? 4.0 : 3.5;

    // 史実の希少鉱物（硫黄・硝石・クロム・錫・金・銀）は 産地を押さえた国が世界を人質に取れる財で、それが本作では起きない。〔履歴 Simulation.cs#45〕
    private static int[]? _scarceCountsWithColonies;   // 財ID→産地の数（world.json から1回だけ数える・決定論）。〔履歴 Simulation.cs#46〕
    private static int _scarceTotalWithColonies;

    private static void EnsureScarceCounts()
    {
        if (_scarceCountsWithColonies is not null) return;
        var counts = new int[Catalog.Goods.Length];
        int total = 0;
        foreach (var n in WorldData.Nations())
        {
            foreach (var p in n.Provinces)
            {
                total++;
                foreach (var g in p.Resources) if (g >= 0 && g < counts.Length) counts[g]++;
            }
            // ★植民地（ColoniesAreRegions で実体の生産者を持ち、PromoteColonyToRegion で同じ ScarceEndowment を受ける）を数えないと
            //   世界総産出が保存しない（分子と分母の非対称）。
            foreach (var col in n.InitialColonies)
            {
                total++;
                foreach (var g in col.Resources) if (g >= 0 && g < counts.Length) counts[g]++;
            }
        }
        _scarceTotalWithColonies = total; _scarceCountsWithColonies = counts;   // ★最後に代入＝他スレッドが半端な配列を見ない（バッチは並列実行される）
    }

    /// <summary>希少鉱物の産地での賦存量（総量保存で逆算）。対象外・未点火なら通常の賦存を返す。</summary>
    internal static double ScarceEndowment(int good, Coefficients c, bool sharedWorld)
    {
        // 史実の英は硫黄をシチリアから運んだ。〔履歴 Simulation.cs#47〕
        if (!sharedWorld || !Catalog.IsScarceMineral(good, c)) return ResourceEndowment(good);
        EnsureScarceCounts();
        int k = _scarceCountsWithColonies![good];
        int total = _scarceTotalWithColonies;
        if (k <= 0) return ResourceEndowment(good);   // 産地が1つも無い財は触らない（fail-open）
        return (k * ResourceEndowment(good) + (total - k) * ResourceScarcity) / k;
    }

    /// <summary>
    /// 賦存倍率を生産者へ当てる唯一の入口（播種の6箇所が全部ここを通る）。〔履歴 Simulation.cs#48〕
    /// </summary>
    internal static void ApplyEndowment(Producer pr, double capMul, double costDiv, Coefficients c)
    {
        pr.Productivity *= capMul;
        DivideCostByEndowment(pr, costDiv, c);
    }

    /// <summary>
    /// 1.0 を渡せば割らない（産地の発見の行の播種）。〔履歴 Simulation.cs#49〕
    /// </summary>
    internal static void DivideCostByEndowment(Producer pr, double costDiv, Coefficients c)
    {
        double gate = costDiv > 1.0 ? c.EndowmentCostFollowsAbundance : costDiv < 1.0 ? c.EndowmentCostFollowsScarcity : 0.0;
        if (!(gate > 0) || !(costDiv > 0)) return;
        pr.MarginalCost /= System.Math.Pow(costDiv, gate);
    }

    /// <summary>
    /// ⚠共有世界だけ（密閉世界＝単国の較正とテストでは産地を絞らない＝旧 LateResources の枝と同じ・ScarceEndowment と同じ理由）。〔履歴 Simulation.cs#50〕
    /// </summary>
    internal static int SiteRowFor(string place, int good, Coefficients c, bool sharedWorld)
    {
        if (!sharedWorld) return -1;
        int row = Catalog.SiteRowOf(place, good);
        return row >= 0 && good == Catalog.SilverId && !(c.SilverRushEnabled > 0) ? -1 : row;
    }
    private const double LandHeadroom = 1.8;         // 本土州の耕地賦存量＝初期農民×これ。初期は余裕（landFactor=1＝既存収支を保持）、流入で農村人口がこれを超えると収穫逓減が効く
    // PolicyDefaults〔履歴 Simulation.cs#51〕

    /// <summary>播種時に読む政策の既定値。⚠数字をここに書き写さない＝<see cref="NationPolicy"/> の初期化子が唯一の出所。
    /// 初期兵は「平時の均衡」で播くので軍事費の既定が要る（BuildProvince は World より前に走るので World.MilitarySpending は無い）。</summary>
    private static readonly NationPolicy PolicyDefaults = new();

    // 州の関税利害（分離独立の駆動）：工業州（鉄・石炭・鉄鋼・機械等を産む）は保護(+1)・農業/一次産品輸出州は自由貿易(−1)。〔履歴 Simulation.cs#52〕
    private const double InitialRentSeedScale = 3.0;

    // 規模は国ごとに書かない（国名分岐なし）＝労働配分が需要比例 （EconomyStep の労働配分 prodW＝LaborWeight×Scale・LaborWeight＝付加価値×売れる量〔前期需要〕）なので、大軍の国ほど大きな軍需産業が需要から自然に立つ。〔履歴 Simulation.cs#53〕
    internal static readonly int[] InitialArmsIndustryGoods = [Catalog.GunpowderId, Catalog.CanvasId, Catalog.ArtilleryId, Catalog.RifleId, Catalog.AmmunitionId, Catalog.SailingShipId, Catalog.WarshipId];

    /// <summary>
    /// 足した行は各州の配列の末尾＝旗 0 で外すと足す前の並びに戻る。〔履歴 Simulation.cs#54〕
    /// </summary>
    internal static readonly (string City, int Good)[] ResourceRowsAdded =
    [
        ("バーミンガム", Catalog.CopperOreId), ("マンチェスター", Catalog.SaltId), ("グラスゴー", Catalog.FishId), ("ブレスラウ", Catalog.IronOreId),
        ("セントポール", Catalog.LeadOreId), ("広州", Catalog.IronOreId), ("グラーツ", Catalog.SaltId), ("ビルバオ", Catalog.IronOreId),
        ("セビーリャ", Catalog.CopperOreId), ("セビーリャ", Catalog.LeadOreId),
    ];

    /// <summary>
    /// 州の Region.Resources はこの配列＝全ての読み手が同じ配列を見る。〔履歴 Simulation.cs#55〕
    /// </summary>
    internal static int[] ProvinceResources(string city, int[] resources, Coefficients coef)
    {
        if (coef.ResourceRowsFix > 0) return resources;
        bool Added(int g) { foreach (var (c, a) in ResourceRowsAdded) if (a == g && c == city) return true; return false; }
        bool any = false;
        foreach (var g in resources) if (Added(g)) { any = true; break; }
        return any ? System.Array.FindAll(resources, g => !Added(g)) : resources;
    }

    private static Region BuildProvince(int id, ProvinceSpec p, NationConfig cfg, Coefficients coef, bool sharedWorld = false)
    {
        var (city, share, resources, culture, religion, _, _, _, _) = p;   // resources〔履歴 Simulation.cs#56〕
        resources = ProvinceResources(city, resources, coef);   // resources〔履歴 Simulation.cs#57〕
        var (goods, markets, producers) = CatalogEconomy(Math.Min(cfg.FarmLordShare, 1.0 - coef.SerfCashShareFloor.Value), coef);   // 農奴の市場参加床（既定0.15・FarmLordShare の上限を 1−床 に抑える Math.Min。0 なら効かない）：オブローク＝農奴も市場で現金を得た＝取り分100%の現金遮断を緩める（辺境は対象外＝入植小農）
        // 気候ゲート財（茶葉/ゴム/香辛料…旧colonial）は賦存州（Resources に列挙）のみ産する＝非賦存州の生産者を除去（英は茶を産めない＝気候の現実）。
        // 列挙する州（中国の茶州等）は下の賦存ブースト×3.5で産み、加工して世界へ売る＝供給の偏在が地理から創発する（287州世界）。
        producers.RemoveAll(pr => Catalog.IsColonial(pr.GoodId) && System.Array.IndexOf(resources, pr.GoodId) < 0 && SiteRowFor(city, pr.GoodId, coef, sharedWorld) < 0);
        // 未登録都市・単一州世界は首都座標へフォールバック（テスト世界の(0,0)＝従来挙動を保存）。〔履歴 Simulation.cs#58〕
        var (plat, plon) = Climate.CityCoord(city);
        if (plat == 0 && plon == 0) { plat = cfg.Latitude; plon = cfg.Longitude; }
        // 米の気候ゲート：モンスーン・熱帯の適地外の州は、賦存列挙がない限り生産者ごと除去（欧州の全州が米を自給する非現実の排除。茶葉等と同型）。
        producers.RemoveAll(pr => pr.GoodId == Catalog.RiceId && !Climate.RiceSuitable(plat, plon) && System.Array.IndexOf(resources, pr.GoodId) < 0);
        // 適地外（|緯度|>37）は賦存列挙がない限り生産者ごと除去＝ 英仏独露の本国綿花自給を排除（綿布は輸入原綿で織る＝ランカシャーの現実。〔履歴 Simulation.cs#59〕
        producers.RemoveAll(pr => pr.GoodId == Catalog.CottonId && !Climate.CottonSuitable(plat) && System.Array.IndexOf(resources, pr.GoodId) < 0);
        // 州の資源プロファイル：列挙財は増産（×3.5）、非列挙のRGOは減産（×0.4・要輸入）。工場財は地理に縛られないので減産しない。
        // 主食は気候帯の自給セット（Climate.StapleSelfSufficient）に該当すれば非列挙でも減産しない（自給ぶんを作る＝封鎖で即飢饉にしない）。
        foreach (var pr in producers)
        {
            // 開始時の賦存（resources）に載る財（カリフォルニアの金・アジアのキナ）は下の賦存の枝をそのまま通す（地代の取り分も同じ）＝門が付くだけ。〔履歴 Simulation.cs#60〕
            int site = pr.IsRgo ? SiteRowFor(city, pr.GoodId, coef, sharedWorld) : -1;
            if (site >= 0 && System.Array.IndexOf(resources, pr.GoodId) < 0)
            {
                ApplyEndowment(pr, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, 1.0, coef);   // ApplyEndowment〔履歴 Simulation.cs#61〕
                pr.SiteRow = site;
            }
            else if (System.Array.IndexOf(resources, pr.GoodId) >= 0)
            {
                pr.SiteRow = site;
                ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, sharedWorld) * coef.ResourceEndowmentScale, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, coef);   // 賦存量＝good の地理的性質（鉱物=集中4.0／他=中庸3.5）。希少鉱物は総量保存で逆算した高い賦存。★Scale=1.0（既定）でビット恒等＝診断用レバー
                // 賦存財の RGO＝その州の輸出向け一次産品＝**大農園/鉱山の経営**なので、残余は経営者（地主階級）へ厚く落ちる。〔履歴 Simulation.cs#62〕
                if (pr.IsRgo && coef.PlantationRentShare.Value > 0)
                {
                    double plantation = coef.PlantationRentShare.Value;
                    if (plantation > pr.RentShare.Value) { pr.RentShare = new Share(plantation); pr.OwnerPopIndex = 0; }   // 所有は地主階級（貴族）＝経営者のいる大農園
                }
            }
            // 総量は産地の賦存を引き上げて保つ（ScarceEndowment）。〔履歴 Simulation.cs#63〕
            else if (sharedWorld && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;   // IsScarceMineral〔履歴 Simulation.cs#64〕
            else if (pr.IsRgo && !Climate.StapleSelfSufficient(pr.GoodId, plat, plon) && !Catalog.IsUbiquitousMineral(pr.GoodId, coef)) ApplyEndowment(pr, ResourceScarcity * coef.ResourceScarcityScale, ResourceScarcity * coef.ResourceScarcityScale, coef);   // ★Scale=1.0（既定）でビット恒等＝診断用レバー
        }

        // 貴族は頭数が少ない（英108K vs 資本家2,100K）ので   旧支配層の政治力＝富シェア×参政権は **英3%・仏21%・普26%** になっていた。〔履歴 Simulation.cs#65〕
        var lit = ClassLiteracy(cfg, coef);   // lit〔履歴 Simulation.cs#66〕
        var nobility     = MkPop(0, "貴族",       false, cfg.Nobility * (1 - cfg.ClergyFrac) * share, lit[0],  3000 * coef.ClosedWealthMultiple, closed: true);
        var clergy       = MkPop(6, "聖職者",     false, cfg.Nobility * cfg.ClergyFrac * share,       lit[1], 3000 * coef.ClosedWealthMultiple, closed: true);   // 宗教学校の教育を算出（世俗化＝公教育で収入を失い没落）。教会領も土地＝貴族と同じ倍率
        var peasant      = MkPop(1, "農民",       false, cfg.Peasant * share,      lit[2], 200,  subs: cfg.PeasantSubsistence);
        var worker       = MkPop(2, "工場労働者", true,  cfg.Worker * share,       lit[3], 300);
        var bourgeois    = MkPop(3, "資本家",     true,  cfg.Bourgeois * share,    lit[4], 3200, modern: true);
        var engineer     = MkPop(4, "技師",       true,  cfg.Professional * (1 - cfg.IntelligentsiaFrac) * share, lit[5], 800, modern: true);
        // 旧インテリPOPを専門職に分解（利益団体「インテリ」＝教師・官僚・医者・技師の政治的まとまり。実体は職業別POP）。
        double intel = cfg.Professional * cfg.IntelligentsiaFrac * share;
        // ⚠**総量保存**：教師/官僚/医者の取り分から按分で切り出す（新しい人は湧かない＝思想⑥）。〔履歴 Simulation.cs#67〕
        double resFrac = System.Math.Clamp(coef.ResearcherFrac.Value, 0, 0.9);
        double keep = 1.0 - resFrac;
        var teacher    = MkPop(7, "教師", true, intel * 0.40 * keep, lit[6], 800, modern: true);   // 公教育・義務教育の教育を算出
        var bureaucrat = MkPop(8, "官僚", true, intel * 0.35 * keep, lit[6], 800, modern: true);   // 行政を算出（不足＝汚職）
        var doctor     = MkPop(9, "医者", true, intel * 0.25 * keep, lit[6], 800, modern: true);   // 医療を算出（人口扶養力）
        var researcher = MkPop(13, "研究者", true, intel * resFrac, lit[6], 800, modern: true);   // 大学の高等教育を算出（技師・上位POPの母体）
        var merchant   = MkPop(10, "商人", true, cfg.Merchant * share, lit[7], 1500, modern: true); // 流通マージン＋貿易の仲立ち。商船を買い海運を支える
        var artisan    = MkPop(11, "職人", true, cfg.Artisan * share, lit[8], 400);   // 手工業の自営（大衆ブロック＝ラッダイトの母体）
        // #12 家事使用人：富裕層(貴族＋資本家)の世帯規模に比例して雇われる（家事奉仕を算出）。女性がなりやすい（高FemaleShare＝紡績/家事奉公）。低識字・低富。
        var servant    = MkPop(12, "使用人", true, (nobility.Size + bourgeois.Size) * 1.0, lit[9], 150);   // servant〔履歴 Simulation.cs#68〕
        servant.SetAdultFemaleShare(0.75);   // 女性が多数（19世紀の家事使用人＝女性就労の主要な受け皿）
        // SetAdultFemaleShare〔履歴 Simulation.cs#69〕
        clergy.SetAdultFemaleShare(0.10); bourgeois.SetAdultFemaleShare(0.20); engineer.SetAdultFemaleShare(0.02);
        teacher.SetAdultFemaleShare(0.25); bureaucrat.SetAdultFemaleShare(0.03); doctor.SetAdultFemaleShare(0.05); researcher.SetAdultFemaleShare(0.02);   // 研究者＝技師と同じ（19世紀の大学は男性のみ）
        merchant.SetAdultFemaleShare(0.25); artisan.SetAdultFemaleShare(0.30);

        // 実測（armydump）＝   イギリス 初期兵1125／人頭均衡150＝**7.5倍**・プロイセン 900／61＝**14.7倍**・オーストリア 650／270＝2.4倍。〔履歴 Simulation.cs#70〕
        double pw = peasant.Size + worker.Size;
        var soldier = new Pop { ClassId = 5, ClassName = "兵士", IsUrban = false, Satisfaction = 1.0, IsSoldier = true, Ages = Pop.SoldierAges(),
            Literacy = pw > 1e-9 ? (peasant.Literacy * peasant.Size + worker.Literacy * worker.Size) / pw : 0.3 };
        double pwMen = Military.RecruitMen(peasant) + Military.RecruitMen(worker);   // pwMen〔履歴 Simulation.cs#71〕
        double draft = PolicyDefaults.MilitarySpendingTarget * coef.MaxSoldierFraction.Value * pwMen;
        if (pw > 1e-9)
        {
            Military.MoveCohort(peasant, soldier, draft * peasant.Size / pw, coef);   // MoveCohort〔履歴 Simulation.cs#72〕
            Military.MoveCohort(worker,  soldier, draft * worker.Size  / pw, coef);
        }

        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, soldier, clergy, teacher, bureaucrat, doctor, merchant, artisan, servant };
        // ★大学の生産者を止めても   差分ゼロだった＝**原因は生産者でなくPOPの存在そのもの**と切り分け済み。〔履歴 Simulation.cs#73〕
        if (resFrac > 0) pops.Add(researcher);
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 7, bureaucratIdx: 8, doctorIdx: 9, coef: coef, researcherIdx: resFrac > 0 ? 13 : -1, clergyIdx: 6);   // clergyIdx＝段1 器 EduProducersCoexist の教区学校の担い手（0 では読まれない）
        // #12 家事奉仕の生産者：家事使用人(index12)が算出し富裕層が雇う（兵力と同型＝pop自身が役務を売り収入を得る）。
        producers.Add(new Producer { GoodId = Catalog.DomesticServiceId, Productivity = 1.0, Output = servant.Size * 0.5, MarginalCost = 0.3, LaborPopIndex = 12, OwnerPopIndex = 12, RentShare = default });
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, coef);   // 高度労働力（技師）＋石油火力
        AddArtisanProducers(producers, artisanIdx: 11);   // 職人の工房（工場との並存レシピ）。家事使用人を末尾(12)に足したので pops.Count-1 でなく職人の明示index
        // AddAlternateRecipes〔履歴 Simulation.cs#74〕
        AddAlternateRecipes(producers, coef);   // AddAlternateRecipes〔履歴 Simulation.cs#75〕
        var region = new Region
        {
            Id = id, Name = city, IsPort = p.Coastal, Resources = resources, Culture = culture, Religion = religion,   // TariffInterest は CheckSecession で実利から毎期算出〔履歴 Simulation.cs#76〕
            // Dominance〔履歴 Simulation.cs#77〕
            Dominance = 1.0,   // ★E-06 支配度の初期値（中核＝1）
            Tier = RegionTier.Core,
            Rule = p.Rule is { Length: 4 } ? (double[])p.Rule.Clone() : cfg.Rule is { Length: 4 } ? (double[])cfg.Rule.Clone() : [1.0, 0.0, 0.0, 0.0],   // Rule〔履歴 Simulation.cs#78〕
            RuleInit = p.Rule is { Length: 4 } ? (double[])p.Rule.Clone() : cfg.Rule is { Length: 4 } ? (double[])cfg.Rule.Clone() : [1.0, 0.0, 0.0, 0.0],

            // 0＝従来＝初期農民×余裕。〔履歴 Simulation.cs#79〕
            ArableLand = p.ArableLand > 0 ? p.ArableLand : peasant.Size * LandHeadroom,   // 耕地賦存量＝初期農民×余裕。流入で農村人口が超えると収穫逓減
            Terrain = Places.TerrainOf(city),   // 係数全1.0の間は恒等〔履歴 Simulation.cs#80〕
            CapitalEnabled = true,
            Industrialization = cfg.Industrialization * (p.IndShare >= 0 ? p.IndShare : share),   // ★IndShare（一次データ）があれば産業地理で按分・無ければ従来の人口比
            CulturalGoodId = cfg.CulturalGoodId,   // CulturalGoodId〔履歴 Simulation.cs#81〕
            InvestorPopIndex = 3, MeritFromIndex = 2, MeritToIndex = 4,
            ClergyPopIndex = 6, TeacherPopIndex = 7, BureaucratPopIndex = 8, DoctorPopIndex = 9, SoldierPopIndex = 5, MerchantPopIndex = 10, ArtisanPopIndex = 11, ServantPopIndex = 12,
            CriminalPopIndex = AddCriminalPop(pops, coef, worker.Literacy), ResearcherPopIndex = resFrac > 0 ? 13 : -1,   // ★段②：研究者は休眠時 -1＝Meritocracy の昇格先に数えない（POP自体も並んでいない）
            EducationProducerIndex = eduIdx,
            Goods = goods,
            Pops = pops,
            Producers = producers,
            Markets = markets,
        };
        // 月次の実勢（rentShare×産出×参照価格）を1回だけ推定して置く＝初回の EconomyStep が実流で上書きする。〔履歴 Simulation.cs#82〕
        if (LandOwnershipShares(cfg.LandOwnership) is { } lo)
            foreach (var pr in region.Producers)
                if (pr.IsRgo && System.Array.IndexOf(resources, pr.GoodId) < 0 && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < region.Pops.Count
                    && region.Pops[pr.OwnerPopIndex].ClassId == PopClass.Noble)
                    pr.LandShares = (double[])lo.Clone();
        foreach (var pr in region.Producers)
            if (pr.IsRgo && pr.RentShare.Value > 0 && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < region.Pops.Count
                && pr.GoodId >= 0 && pr.GoodId < Catalog.RefPrices.Length)
                for (int k = 0, nk = pr.LandShares is { } ls0 ? (coef.LandShareCrownSlot > 0 && ls0.Length == 5 ? 4 : ls0.Length) : 1; k < nk; k++)   // LandShareCrownSlot〔履歴 Simulation.cs#83〕
                    if (pr.LandShares is not { } ls || ls[k] > 0)
                        region.Pops[pr.LandShares is null ? pr.OwnerPopIndex : EconomyStep.LandShareRecipient(region, pr, k)].LandRentPerMonth += new MoneyPerMonth(pr.LandShares is { } lsk ? pr.RentShare.Value * pr.Output * Catalog.RefPrices[pr.GoodId] * InitialRentSeedScale * lsk[k] : pr.RentShare.Value * pr.Output * Catalog.RefPrices[pr.GoodId] * InitialRentSeedScale);
        return region;
    }

    /// <summary>
    /// 長さが 4 か 5 でない・負・非有限・和が正でない・5 個目が 1 以上（1−c で割れない）なら null（＝今の持ち主に 1.0）。〔履歴 Simulation.cs#84〕
    /// </summary>
    internal static double[]? LandOwnershipShares(double[]? lo)
    {
        if (lo is null || (lo.Length != 4 && lo.Length != 5)) return null;
        double sum = 0;
        foreach (var v in lo) { if (!(v >= 0) || double.IsInfinity(v)) return null; sum += v; }
        if (!(sum > 0) || double.IsInfinity(sum)) return null;
        var o = new double[lo.Length];
        for (int k = 0; k < lo.Length; k++) o[k] = lo[k] / sum;
        return o.Length == 5 && !(1.0 - o[4] > 0) ? null : o;
    }

    // 役務（サービス財）の係数：労働力＝工場の投入。賃金は労働市場の価格で決まる（生産が先→売って収入の設計）。
    private const double LaborPerOutput = Catalog.LaborPerOutput;     // 工場の産出1単位あたりの労働力投入（Catalogが単一の真実）
    private const double LaborProductivity = 1.0;  // 工場労働者1人あたりの労働力算出の基準（×FullChainProductivityScale＝工場と同じ規模感。工業化・技術の倍率にも乗る）

    private const double FarmLaborPerOutput = Catalog.FarmLaborPerOutput;   // 自家農家の産出1単位あたりの農業労働力


    /// <summary>カタログ経済（市場・生産者）を1地域ぶん生成。地域ごとに独立した市場・状態を持つ（複数地域は別インスタンス）。
    /// 役務（労働力・教育・医療・行政）の生産者は担い手popの添字が地域構成で違うため、呼び出し側が <see cref="AddServiceProducers"/> で足す。
    /// farmLordShare＝農制：自家農家（RGO）の残余のうち地主（貴族）の取り分。1.0＝農奴制・0.5＝小作・0＝自作農（農民が所有）。</summary>
    private static (List<Good> goods, Market[] markets, List<Producer> producers) CatalogEconomy(double farmLordShare, Coefficients coef)   // CatalogEconomy〔履歴 Simulation.cs#85〕
    {
        int n = Catalog.Goods.Length;
        var goodsById = new Good[n];
        var markets = new Market[n];
        var producers = new List<Producer>(n);
        foreach (var d in Catalog.Goods)
        {
            double refP = Catalog.RefPrices[d.Id];   // 参照価格＝労働込みの実費用連鎖（深い連鎖ほど高い＝自然な価格勾配。役務は固定錨）
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, WearRatePerMonth = d.WearRatePerMonth, NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            markets[d.Id] = new Market { Price = refP, Stock = 200, TargetStock = 200 };
            // 旧「colonial財は国内生産者なし（輸入のみ）」のスキップを撤廃＝colonial財も RGO 生産者を持つ。
            // 賦存州（州が Resources に列挙）のみ産み、非賦存州の生産者は BuildProvince が除去する＝茶＝中国/ゴム＝東南ア…の偏在が地理から創発（287州世界）。
            if (d.Tier == GoodTier.Service) continue;   // 役務の生産者は AddServiceProducers が担い手popに紐づけて追加
            if (d.Id == Catalog.BuildingId && !(coef.BuildingGoodBundle > 0)) continue;   // 旗が正（既定 1）のときだけ木造〔既定〕の生産者を置く〔履歴 Simulation.cs#86〕
            bool isFactory = d.Inputs.Length > 0;
            // 雇用の一本化：工場は労働力＋高度労働力、自家農家（RGO）は農業労働力を市場で買う＝全生産者が同一ロジック。
            // 自家農家の残余（売上−労働費）＝地代：農奴制では全て地主（貴族）へ、小作は折半、自作農は農民自身へ。
            producers.Add(new Producer
            {
                GoodId = d.Id,
                // 工場は空（＝単一レシピの既定表示）。〔履歴 Simulation.cs#87〕
                RecipeName = isFactory ? "" : Catalog.RgoFacilityName(d.Id),
                Productivity = d.Productivity * FullChainProductivityScale,   // 生産性差は工業化度(indMult)・技術・資源(辺境/鉄道)から創発〔履歴 Simulation.cs#88〕
                Output = isFactory ? 0.5 : 50,         // 工場は表示閾値(Output>1)未満で開始＝初期はリストに出ず、育って初めて現れる（1789は前工業＝農村中心・工場はブルジョワ/プレイヤーが育てる）
                Scale = isFactory ? 0.05 : 1.0,        // 工場は操業規模を低く開始（全財がScale1.0で立たない＝初期は工場がほぼ無い）
                MarginalCost = isFactory ? Catalog.FactoryVa : Catalog.RgoVa,   // 自家農家＝0.34＋農業労働0.16≈旧0.5（供給曲線の損益分岐を維持＝過剰生産を防ぐ） // 賃金分は投入費（労働購入）へ移った＝残りは管理・資本/土地の付加価値
                LaborPopIndex = isFactory ? 2 : 1,     // 能力の按分基準：自家農家＝農民／工場＝都市労働者（実際の雇用は労働市場）
                OwnerPopIndex = isFactory ? 3 : (farmLordShare > 0 ? 0 : 1),   // 農地の所有：地主制＝貴族／自作農＝農民自身
                RentShare = new Share(isFactory ? 1.0 : Math.Max(0.3, farmLordShare)),    // 残余の地代率（自作農でも0.3＝自分の土地の地代＝土地税の課税ベース）
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : d.Id == Catalog.CrudeOilId
                                       ? [new Input(Catalog.FarmLaborId, FarmLaborPerOutput), new Input(Catalog.MachineryId, coef.OilMachineryInput)]   // Input〔履歴 Simulation.cs#89〕
                                       : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }
        var goodsList = new List<Good>(goodsById);
        // この並びは MarketFlow の価格更新順と   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。〔履歴 Simulation.cs#90〕
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順
        return (goodsList, markets, producers);
    }

    /// <summary>
    /// 門＝転炉鋼の発見（Producer.OpenedByRow）まで 0＝時代錯誤の投入を作らない。〔履歴 Simulation.cs#91〕
    /// </summary>
    private static void ToWeaponStrength(Producer p, double k)
    {
        if (k > 0)
        {
            p.Productivity *= k;
            var scaled = new List<Input>(p.Inputs.Count);
            foreach (var i in p.Inputs) scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));
            p.Inputs = scaled;
        }
        p.OpenedByRow = Catalog.OpenedByRowOf(p.GoodId);
    }

    /// <summary>
    /// 職人の工房（工場との並存レシピ）：衣料・家具・革製品・陶磁器を手仕事で作る自営（全付加価値が職人の収入）。〔履歴 Simulation.cs#92〕
    /// </summary>
    private static void AddAlternateRecipes(List<Producer> producers, Coefficients coef)
    {
        // 転炉鋼の発見の後はベッセマーがコストで自動的に坩堝鋼・錬鉄レールを駆逐（1870年代英のレール転換が創発）。〔履歴 Simulation.cs#93〕
        {
            Producer? steelDefault = null, railDefault = null;
            foreach (var pr in producers)
            {
                if (pr.GoodId == Catalog.SteelId) steelDefault = pr;
                else if (pr.GoodId == Catalog.RailId) railDefault = pr;
            }
            if (steelDefault is not null)   // 鋼の無い世界（最小シナリオ）は不発
            {
                producers.Add(new Producer { GoodId = Catalog.SteelId, RecipeName = "坩堝鋼", Productivity = 0.9 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = steelDefault.LaborPopIndex, OwnerPopIndex = steelDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                    Inputs = [new Input(Catalog.IronId, 2.2), new Input(Catalog.CoalId, 1.978), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SteelId))] });
                if (railDefault is not null)
                    producers.Add(new Producer { GoodId = Catalog.RailId, RecipeName = "錬鉄レール", Productivity = 1.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                        LaborPopIndex = railDefault.LaborPopIndex, OwnerPopIndex = railDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                        Inputs = [new Input(Catalog.IronId, 1.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RailId))] });
            }
        }
        // 史実のマスケット（錬鉄の銃身）と鋳鉄砲/青銅砲は鋼を要しない＝**時代錯誤の投入**。〔履歴 Simulation.cs#94〕
        Producer? rifleDefault = null, cannonDefault = null;
        foreach (var pr in producers)
        {
            if (pr.GoodId == Catalog.RifleId && rifleDefault is null) rifleDefault = pr;
            else if (pr.GoodId == Catalog.ArtilleryId && cannonDefault is null) cannonDefault = pr;
        }
        // ⚠旧の生産性は新しいレシピに強さを掛ける前の既定の値から作る（下の2本は Add が先・ToWeaponStrength が後＝旧の生産性は強さを掛ける前の値から作る）。〔履歴 Simulation.cs#95〕
        if (rifleDefault is not null)   // 軍需の無い世界（最小シナリオ）は不発
        {
            producers.Add(new Producer { GoodId = Catalog.RifleId, RecipeName = "マスケット",
                Productivity = rifleDefault.Productivity * coef.MusketProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = rifleDefault.LaborPopIndex, OwnerPopIndex = rifleDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.0), new Input(Catalog.LumberId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RifleId))] });
            ToWeaponStrength(rifleDefault, coef.WeaponStrengthRifle);
        }
        if (cannonDefault is not null)
        {
            producers.Add(new Producer { GoodId = Catalog.ArtilleryId, RecipeName = "鋳鉄砲",
                Productivity = cannonDefault.Productivity * coef.MusketProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = cannonDefault.LaborPopIndex, OwnerPopIndex = cannonDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.3), new Input(Catalog.LumberId, 0.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ArtilleryId))] });
            ToWeaponStrength(cannonDefault, coef.WeaponStrengthArtillery);
        }
        // 史実＝ワットの機関（1776〜1800）は鋳鉄のシリンダー（ウィルキンソンの中ぐり盤1774）・   鋳鉄の梁と弾み車・錬鉄と銅のボイラーで、鋼は使わなかった（Dickinson & Jenkins 1927・Hills 1989）。〔履歴 Simulation.cs#96〕
        Producer? engineDefault = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.SteamEngineId) { engineDefault = pr; break; }
        if (engineDefault is not null)   // 蒸気機関の無い世界（最小シナリオ）は不発
            producers.Add(new Producer { GoodId = Catalog.SteamEngineId, RecipeName = "鋳鉄の機関",
                Productivity = engineDefault.Productivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = engineDefault.LaborPopIndex, OwnerPopIndex = engineDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.832), new Input(Catalog.CopperId, 0.414), new Input(Catalog.CoalId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SteamEngineId))] });
        // 鋳鉄の機関（上）と同じ型。〔履歴 Simulation.cs#97〕
        Producer? machineDefault = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.MachineryId) { machineDefault = pr; break; }
        if (machineDefault is not null)   // 機械を作らない世界（最小シナリオ）は不発
            producers.Add(new Producer { GoodId = Catalog.MachineryId, RecipeName = "鋳鉄の機械",
                Productivity = machineDefault.Productivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = machineDefault.LaborPopIndex, OwnerPopIndex = machineDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 2.52), new Input(Catalog.CopperId, 0.57), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.MachineryId))] });
        // 鋳鉄の機械（上）と同じ型・新しい倍率なし。〔履歴 Simulation.cs#98〕
        if (coef.BuildingGoodBundle > 0)
        {
            Producer? woodDefault = null;
            foreach (var pr in producers) if (pr.GoodId == Catalog.BuildingId) { woodDefault = pr; break; }
            if (woodDefault is not null)   // 建造物の無い世界（最小シナリオ）は不発
            {
                void AddBuilding(string recipe, Input[] mats, string? openedBy)
                {
                    var inputs = new List<Input>(mats) { new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.BuildingId)) };
                    producers.Add(new Producer { GoodId = Catalog.BuildingId, RecipeName = recipe,
                        Productivity = woodDefault.Productivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                        LaborPopIndex = woodDefault.LaborPopIndex, OwnerPopIndex = woodDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                        OpenedByRow = openedBy is null ? -1 : Catalog.DiscoveryRowNamed(openedBy),
                        Inputs = inputs });
                }
                AddBuilding("煉瓦造", [new Input(Catalog.ClayId, 0.8), new Input(Catalog.CoalId, 0.4), new Input(Catalog.LumberId, 0.35)], null);
                AddBuilding("鋳鉄の骨組み", [new Input(Catalog.IronId, 0.25), new Input(Catalog.ClayId, 0.6), new Input(Catalog.CoalId, 0.3)], null);
                AddBuilding("鋼の骨組み", [new Input(Catalog.SteelId, 0.2), new Input(Catalog.ClayId, 0.45), new Input(Catalog.CoalId, 0.2)], "転炉鋼");
                AddBuilding("鉄筋コンクリート造", [new Input(Catalog.CementId, 0.4), new Input(Catalog.SteelId, 0.07), new Input(Catalog.LimestoneId, 0.25)], "鉄筋コンクリート");
            }
        }
        // 既定は鯨油/魚油（漁獲0.8）。〔履歴 Simulation.cs#99〕
        Producer? keroDefault = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.LampOilId) { keroDefault = pr; break; }
        if (keroDefault is not null)   // 灯油を作らない世界（最小シナリオ）は不発
        {
            producers.Add(new Producer { GoodId = Catalog.LampOilId, RecipeName = "石油ケロシン",
                Productivity = coef.KeroseneOilProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = keroDefault.LaborPopIndex, OwnerPopIndex = keroDefault.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.CrudeOilId, coef.KeroseneOilInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LampOilId))] });
        }

        // 既定は 肉+小麦粉（塩漬け・ソーセージ）。〔履歴 Simulation.cs#100〕
        Producer? saltCured = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.PreservedMeatId) { saltCured = pr; break; }
        if (saltCured is not null)   // 加工肉を作らない世界（最小シナリオ）は不発
        {
            producers.Add(new Producer { GoodId = Catalog.PreservedMeatId, RecipeName = "冷蔵の加工肉",
                Productivity = saltCured.Productivity * coef.ChilledMeatProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = saltCured.LaborPopIndex, OwnerPopIndex = saltCured.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.MeatId, 0.6), new Input(Catalog.RefrigeratorId, coef.ChilledMeatColdInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.PreservedMeatId))] });
        }

        // 史実＝1838年の硫黄危機（シチリア独占）の後、世界は原料の代替（リオ・ティントの黄鉄鉱）で解いた。〔履歴 Simulation.cs#101〕
            {
                Producer? leadChamber = null;
                foreach (var pr in producers) if (pr.GoodId == Catalog.SulfuricAcidId) { leadChamber = pr; break; }
                if (leadChamber is not null)
                {
                producers.Add(new Producer { GoodId = Catalog.SulfuricAcidId, RecipeName = "黄鉄鉱法", Productivity = leadChamber.Productivity * coef.SulfurFromPyriteProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = leadChamber.LaborPopIndex, OwnerPopIndex = leadChamber.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true,
                    Inputs = [new Input(Catalog.CopperOreId, 1.4), new Input(Catalog.CoalId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SulfuricAcidId))] });   // 燃料は少量（黄鉄鉱は自分で燃える）。〔履歴 Simulation.cs#102〕
                }
            }
        // 既定は草木灰（goods.json の行）。〔履歴 Simulation.cs#103〕
        Producer? sodaAsh = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.SodaAshId) { sodaAsh = pr; break; }
        if (sodaAsh is not null)   // ソーダ灰を作らない世界（最小シナリオ）は不発
        {
            void AddSodaAsh(string recipe, params (int Good, double Qty)[] materials)
            {
                var inputs = new List<Input>(materials.Length + 2);
                foreach (var (good, qty) in materials) inputs.Add(new Input(good, qty));
                inputs.Add(new Input(Catalog.LaborId, LaborPerOutput));
                inputs.Add(new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SodaAshId)));
                producers.Add(new Producer { GoodId = Catalog.SodaAshId, RecipeName = recipe, Productivity = sodaAsh.Productivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = sodaAsh.LaborPopIndex, OwnerPopIndex = sodaAsh.OwnerPopIndex, RentShare = new Share(1.0), BuysLabor = true, Inputs = inputs });
            }
            AddSodaAsh("ルブラン法", (Catalog.SaltId, 0.5), (Catalog.SulfuricAcidId, 0.25), (Catalog.LimestoneId, 0.25), (Catalog.CoalId, 0.3));
            AddSodaAsh("ソルベー法", (Catalog.SaltId, 0.5), (Catalog.LimestoneId, 0.4), (Catalog.CoalId, 0.25));
            AddSodaAsh("電解", (Catalog.SaltId, 0.5), (Catalog.ElectricityId, 0.3));
        }
        AddHouseholdProducers(producers, coef);   // AddHouseholdProducers〔履歴 Simulation.cs#104〕
        ApplyRecipeCostFollowsProductivity(producers, coef);   // 4本の構築経路の最後の add の後の1回〔履歴 Simulation.cs#105〕
    }

    private static void AddArtisanProducers(List<Producer> producers, int artisanIdx)
    {
        foreach (int gid in (int[])[Catalog.ClothingId, Catalog.FurnitureId, Catalog.LeatherGoodsId, Catalog.PorcelainId])   // 衣料・家具・革製品・陶磁器
        {
            var d = Catalog.Goods[gid];
            producers.Add(new Producer
            {
                GoodId = gid, RecipeName = "職人",
                Productivity = d.Productivity * FullChainProductivityScale * HandcraftProductivityRatio,   // Productivity〔履歴 Simulation.cs#106〕
                Output = 30, Scale = 1.0, MarginalCost = HandcraftSelfCost,   // 1789＝職人が現役の主役（工場は0.5/0.05から育つ）
                LaborPopIndex = artisanIdx, OwnerPopIndex = artisanIdx, RentShare = default,
                Inputs = [.. d.Inputs],   // 素材だけ（雇用も機械も使わない自営）
            });
        }
    }

    /// <summary>
    /// 役務の生産者（労働力・教育・医療・行政）を追加し、教育の生産者の添字を返す。〔履歴 Simulation.cs#107〕
    /// </summary>
    private static int AddServiceProducers(List<Producer> producers, List<Pop> pops, int workerIdx, int peasantIdx, int teacherIdx, int bureaucratIdx, int doctorIdx, Coefficients? coef = null, int researcherIdx = -1, int clergyIdx = -1)
    {
        // 労働力：工場労働者が算出。生産性は工場と同じスケール（×FullChainProductivityScale）＝労働供給が工場群の需要と釣り合う。
        // 工業化・技術の倍率にも乗る（機械が労働の産出を増やす＝実質賃金の上昇余地）。
        producers.Add(new Producer { GoodId = Catalog.LaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = pops[workerIdx].Size * 0.3, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = default });
        // 同じ労働者popを担い手に持つ＝労働配分の 零和分割の内側＝採算が合う時だけ人力からシェアを奪う（機械化＝同じpopからより多くの労働力サービス）。〔履歴 Simulation.cs#108〕
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "畜力", Productivity = LaborProductivity * FullChainProductivityScale * 1.3, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = default,
            Inputs = [new Input(Catalog.HorseId, 0.05)] });   // 馬力＝荷駄・巻上機（馬が本当の生産投入になる）
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "蒸気機関", Productivity = LaborProductivity * FullChainProductivityScale * 2.0, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = default,
            Inputs = [new Input(Catalog.SteamEngineId, 0.02), new Input(Catalog.CoalId, 0.3)] });   // 蒸気機関54＋石炭＝工場に据える原動機（54の第3の出口＝Runner診断が点灯）
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "電動", Productivity = LaborProductivity * FullChainProductivityScale * 2.5, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = default,
            Inputs = [new Input(Catalog.ElectricityId, 0.25)] });   // 電動機＝電力が安い国だけ採算が合う（発電機1.3以後）
        // したがって立地上限（案a/b/c）は全て破棄＝上限なしの微優位レシピ1本にする。〔履歴 Simulation.cs#109〕
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "水車", Productivity = LaborProductivity * FullChainProductivityScale * 1.1, Output = 0.5, Scale = 0.05, MarginalCost = 0.40, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = default });
        // 農業労働力：農民が算出し自家農家（RGO）が買う＝農業も「労働を売る」同一ロジック。農村の留保賃金は都市より安い（都市化の誘因）。
        producers.Add(new Producer { GoodId = Catalog.FarmLaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = pops[peasantIdx].Size * 0.3, MarginalCost = 0.3, LaborPopIndex = peasantIdx, OwnerPopIndex = peasantIdx, RentShare = default });
        int eduIndex = producers.Count;
        if (coef is { EduProducersCoexist: > 0 } && clergyIdx >= 0)
        {
            // 2件目の添字は「連続 add」から導く＝Region に新フィールドを持たない（盤面則）。〔履歴 Simulation.cs#110〕
            producers.Add(new Producer { GoodId = Catalog.EducationId, RecipeName = "教区学校", Productivity = coef.ClergyEduProductivity, Output = 1, MarginalCost = 0.4, LaborPopIndex = clergyIdx, OwnerPopIndex = clergyIdx, RentShare = default, Inputs = [new Input(Catalog.PaperId, 0.05)] });
            producers.Add(new Producer { GoodId = Catalog.EducationId, RecipeName = "公立学校", Productivity = coef.TeacherEduProductivity, Output = 1, MarginalCost = 0.4, LaborPopIndex = teacherIdx, OwnerPopIndex = teacherIdx, RentShare = default, Inputs = [new Input(Catalog.BookId, 0.05)] });
        }
        else
        {
            // 教育（器0＝現行）：既定＝教師（公教育の生産性）。宗教学校では聖職者に切替（生産性が低い＝少人数しか教えられない＝供給希少）。紙を消費（教材）。
            producers.Add(new Producer { GoodId = Catalog.EducationId, Productivity = 5.0, Output = 1, MarginalCost = 0.4, LaborPopIndex = teacherIdx, OwnerPopIndex = teacherIdx, RentShare = default, Inputs = [.. Catalog.Goods[Catalog.EducationId].Inputs] });
        }
        // 医療：医者が算出（医薬品を消費）。POPが買い、消費が人口扶養力に効く。供給は希少（1789＝富裕層しか医者にかかれない）。
        producers.Add(new Producer { GoodId = Catalog.HealthcareId, Productivity = 4.0, Output = 1, MarginalCost = 0.3, LaborPopIndex = doctorIdx, OwnerPopIndex = doctorIdx, RentShare = default, Inputs = [.. Catalog.Goods[Catalog.HealthcareId].Inputs] });
        // 行政：官僚が算出（紙を消費＝文書行政）。国家が買う。不足＝汚職（税収が漏れる）。初期は不足気味＝近世国家。
        producers.Add(new Producer { GoodId = Catalog.AdminId, Productivity = 3.0, Output = 1, MarginalCost = 0.3, LaborPopIndex = bureaucratIdx, OwnerPopIndex = bureaucratIdx, RentShare = default, Inputs = [.. Catalog.Goods[Catalog.AdminId].Inputs] });
        // 生産性は教育5.0/医療4.0/行政3.0 より低い＝**希少で高価**。〔履歴 Simulation.cs#111〕
        if (researcherIdx >= 0 && coef is { ResearcherFrac.Value: > 0 })
            producers.Add(new Producer { GoodId = Catalog.HigherEducationId, Productivity = coef.UniversityProductivity, Output = pops[researcherIdx].Size * 0.5, MarginalCost = 0.4, LaborPopIndex = researcherIdx, OwnerPopIndex = researcherIdx, RentShare = default, Inputs = [.. Catalog.Goods[Catalog.HigherEducationId].Inputs] });
        return eduIndex;
    }

    /// <summary>高度労働力（技師の役務）＋石油火力（電力の並存レシピ）の生産者。
    /// 高度労働力＝技師が算出する監督・保守・運転の役務。全工場が少量・エネルギーを使う工程ほど多く要求し、
    /// 鉄道・船舶の運行も消費する＝工業化・鉄道・電化のすべてが技師の価値を押し上げる（教育→技師供給と噛み合う）。
    /// 石油火力＝発電機＋原油→電力（石炭火力との並存レシピ＝燃料の択一。市場が安い方を選ぶ）。</summary>
    private static void AddAdvancedProducers(List<Producer> producers, List<Pop> pops, int engineerIdx, int workerIdx, int ownerIdx, Coefficients coef)
    {
        // 高度労働力：技師の自営役務（人力と同じく頭数なり＝技師階級の成長が供給を増やす唯一の道）。
        producers.Add(new Producer { GoodId = Catalog.AdvLaborId, Productivity = 4.0, Output = pops[engineerIdx].Size * 0.5, MarginalCost = 0.5, LaborPopIndex = engineerIdx, OwnerPopIndex = engineerIdx, RentShare = default });
        // 石油火力（電力の第2レシピ）：他の工場と同じく労働力＋高度労働力を買う。〔履歴 Simulation.cs#112〕
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "石油火力", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.498), new Input(Catalog.CrudeOilId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 石炭が高い州でのみ火力に勝つ＝スイス/伊/ノルウェー型の水力国が価格から創発（1914年に伊約2/3・スイス/ノルウェーほぼ100%水力）。〔履歴 Simulation.cs#113〕
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "水力発電", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.647), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 市場が安い方を選ぶ＝火力の中で石炭を節約する製法が創発的に置き換える。〔履歴 Simulation.cs#114〕
            producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "蒸気タービン", Productivity = 1.4 * coef.SteamTurbineProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 2, OwnerPopIndex = 3, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.MachineryId, 0.199), new Input(Catalog.CoalId, coef.SteamTurbineCoalInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 魚の缶詰（缶詰の第2レシピ）：肉(畜産)の代わりに漁獲を缶詰める＝缶詰の軍需増を漁業からも賄える。漁獲に第2の消費先を与える（従来は干物のみ）。
        producers.Add(new Producer { GoodId = Catalog.CannedFoodId, RecipeName = "魚の缶詰", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.FishId, 0.55), new Input(Catalog.IronId, 0.3), new Input(Catalog.TinOreId, 0.05), new Input(Catalog.SaltId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CannedFoodId))] });
        // 下位インフラ＝水道局（#4 施設化）：建設会社(資本家所有)が労働力+技師+資材(セメント/鋼)で衛生インフラ(上下水道)を敷設＝流通不可の役務。国家が行政費で調達。
        producers.Add(new Producer { GoodId = Catalog.InfrastructureId, RecipeName = "水道局", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CementId, 0.4), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // 上位インフラ＝電化インフラ（近代都市インフラ）。2つの並存レシピ＝早期はガス灯、電化が安くなると電灯が駆逐（創造的破壊）。国家が行政費で調達。便益＝満足度＋衛生＋送電容量。
        // 電灯式（既定）：送電網＋電化製品＋鋼。電化期の財が高い序盤は高コストで休眠＝自然に終盤の能力。
        producers.Add(new Producer { GoodId = Catalog.ElecInfraId, RecipeName = "電灯網", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.GridId, 0.4), new Input(Catalog.ApplianceId, 0.3), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // ガス灯式（並存・早期）：ガス＋鋼。電化前から都市照明を供給＝電灯が安くなるまでの中道。電灯に駆逐される。
        producers.Add(new Producer { GoodId = Catalog.ElecInfraId, RecipeName = "ガス灯", Productivity = 1.5 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.GasId, 0.6), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // === 植民地素材の国内代替（並存レシピ）＝封鎖/戦争で植民地材が高騰すると自動で勝つ＝輸入代替が創発（特別分岐なし）=== 平時は植民地材が安く既定レシピ（サトウキビ/パーム油）が勝つよう、国内材は係数をやや厚く＝普段は休眠・危機に起動。〔履歴 Simulation.cs#115〕
        producers.Add(new Producer { GoodId = Catalog.SugarId, RecipeName = "甜菜糖", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.VegetableId, 2.2), new Input(Catalog.LimestoneId, 0.3222), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SugarId))] });
        // 獣脂石鹸：石鹸の第2レシピ。パーム油(23・植民地輸入)の代わりに国内の畜産(19・獣脂)＋ソーダ灰(鹸化のアルカリ)。伝統的な獣脂石鹸（植民地油脂が断たれても作れる）。
        producers.Add(new Producer { GoodId = Catalog.SoapId, RecipeName = "獣脂石鹸", Productivity = 1.3 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.LivestockId, 1.1), new Input(Catalog.SodaAshId, 0.2434), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SoapId))] });
        // === 旧生産方式を駆逐する「電化/化学」の新レシピ（並存＝安くなった新法が旧法を置換＝創造的破壊）=== ハーバーボッシュ（肥料109の第2レシピ）：硫黄を使わず硫酸＋**電力**で窒素固定＝超安価。〔履歴 Simulation.cs#116〕
        producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "ハーバーボッシュ", Productivity = 2.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.SulfuricAcidId, 0.15), new Input(Catalog.ElectricityId, 0.25), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });   // 電力の燃料主導化とセット〔履歴 Simulation.cs#117〕
        // 合成染料（染料17の第2レシピ）：天然染料(RGO)の代わりに硫酸・ソーダ灰・石炭〔コールタール〕から合成（アニリン1856）。化学工業が成熟＝硫酸とソーダ灰が安い国で天然染料を駆逐。
        producers.Add(new Producer { GoodId = Catalog.DyeId, RecipeName = "合成染料", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.SulfuricAcidId, 0.35), new Input(Catalog.SodaAshId, 0.2084), new Input(Catalog.CoalId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.DyeId))] });
        // 毛織衣料（衣料57の第2レシピ）：綿布(綿花)の代わりに毛織物(畜産)で仕立てる＝綿花が断たれた時の代替（米南北戦争の綿花飢饉でランカシャーが羊毛へ）。
        producers.Add(new Producer { GoodId = Catalog.ClothingId, RecipeName = "毛織衣料", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.WoolenClothId, 0.7), new Input(Catalog.DyeId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ClothingId))] });
        // ジャガイモが安い産地で起動。〔履歴 Simulation.cs#118〕
        producers.Add(new Producer { GoodId = Catalog.SpiritsId, RecipeName = "ジャガイモ蒸留酒", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.PotatoId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SpiritsId))] });
        // トウモロコシが安い産地で起動し畜産(19)の濃厚飼料を賄う。〔履歴 Simulation.cs#119〕
        producers.Add(new Producer { GoodId = Catalog.FodderId, RecipeName = "トウモロコシ飼料", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.MaizeId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FodderId))] });
        // 米が安い産地で起動（麦蒸留酒との燃料/原料択一）。〔履歴 Simulation.cs#120〕
        producers.Add(new Producer { GoodId = Catalog.SpiritsId, RecipeName = "米酒", Productivity = 1.9 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.RiceId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SpiritsId))] });
        // グアノ肥料（肥料109の第3レシピ）：合成(硫酸+畜産の骨)・ハーバーボッシュ(電力)に並ぶ天然窒素源＝グアノ(121・ペルー賦存の植民地材)。ペルー保有国(グアノ輸入可)でのみ起動し、合成肥料より安価＝1840s-70sのグアノ時代を再現。非保有国は合成肥料のまま。
        producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "グアノ肥料", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.GuanoId, 0.6), new Input(Catalog.SulfuricAcidId, 0.1), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });
        // 化学法より高い＝硫黄が届かない所の代替。〔履歴 Simulation.cs#121〕
            producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "堆肥", Productivity = 2.0 * coef.CompostProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
                Inputs = [new Input(Catalog.LivestockId, coef.CompostLivestockInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });   // Inputs〔履歴 Simulation.cs#122〕
        // (1175) 硝石丘（旗 NiterBedEnabled 既定 0＝足さない）の枝を削除。硝石を産地にする回で戻す。
        // デフォルトの木材パルプ製紙(goods.json)は発見の表の木材パルプ製紙（1840年代）で門＝早期はこのぼろ布製紙が供給し、パルプが安くなると駆逐される。〔履歴 Simulation.cs#124〕
        producers.Add(new Producer { GoodId = Catalog.PaperId, RecipeName = "ぼろ布製紙", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.HempId, 0.6), new Input(Catalog.CottonClothId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.PaperId))] });
        // 同じ病に効くが、苦く嵩が張り吐き戻す・品位の低い樹皮と混ぜ物が多い＝効き目が低い。〔履歴 Simulation.cs#125〕
        producers.Add(new Producer { GoodId = Catalog.QuinineId, RecipeName = "樹皮の粉末", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CinchonaBarkId, 1.5), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.QuinineId))] });
        // 木炭製鉄/銅/鉛（鉄31・銅33・鉛34の代替）：コークス(石炭)の代わりに木炭(木)で還元＝石炭が乏しく森林が豊かな地域の製錬（スウェーデン/露ウラル/米の木炭高炉が19世紀半ばまで存続）。
        // 産出は低め＝石炭/コークスが安い地域ではコークス法が勝ち、石炭が高い地域でのみ木炭法が勝つ（燃料の地理が製錬法を分ける・倍率排除と同思想）。
        producers.Add(new Producer { GoodId = Catalog.IronId, RecipeName = "木炭製鉄", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.IronOreId, 1.0), new Input(Catalog.TimberId, 0.8), new Input(Catalog.LimestoneId, 0.25), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.IronId))] });
        producers.Add(new Producer { GoodId = Catalog.CopperId, RecipeName = "木炭製銅", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CopperOreId, 1.0), new Input(Catalog.TimberId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CopperId))] });
        producers.Add(new Producer { GoodId = Catalog.LeadId, RecipeName = "木炭製鉛", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.LeadOreId, 1.0), new Input(Catalog.TimberId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LeadId))] });
        // クロムなめし革（革46の近代レシピ・発見の表の1880年代＝商用1884）：原皮＋クロム鉱を少量。タンニンなめし(木0.15)より投入質量が小さく速い＝onset後にコストで勝ちデフォルト(タンニン)を駆逐＝クロムの工業需要は晩期に立つ（史実：クロムは19世紀後半まで用途僅少）。
        producers.Add(new Producer { GoodId = Catalog.LeatherId, RecipeName = "クロムなめし", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.LivestockId, 0.5), new Input(Catalog.ChromeId, 0.08), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LeatherId))] });
        // 無煙火薬（火薬38の近代レシピ・発見の表の1880年代）：硫酸(ニトロセルロースの硝化)＋硝石＋綿花。高エネルギー密度＝総投入質量が黒色火薬(1.0)より小さい(0.55)＝少量で同威力＝onset後にコストで勝つ。
        producers.Add(new Producer { GoodId = Catalog.GunpowderId, RecipeName = "無煙火薬", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.SulfuricAcidId, 0.3), new Input(Catalog.SaltpeterId, 0.2), new Input(Catalog.CottonId, 0.05), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.GunpowderId))] });
        // 亜麻衣料（衣料57の代替）：綿布の代わりに帆布(麻織物)で仕立てる＝亜麻が安い地域・綿花が断たれた時の代替（市場が安い方を採る）。
        producers.Add(new Producer { GoodId = Catalog.ClothingId, RecipeName = "亜麻衣料", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CanvasId, 0.6), new Input(Catalog.DyeId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ClothingId))] });
        // 圧縮帯（自動車1.50・WWI財1.55・ラジオ1.60）へ整合。〔履歴 Simulation.cs#126〕
        producers.Add(new Producer { GoodId = Catalog.SilkClothId, RecipeName = "人絹", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.TimberId, 0.6415), new Input(Catalog.SulfuricAcidId, 0.25), new Input(Catalog.SodaAshId, 0.2028), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SilkClothId))] });
        // 圧縮帯へ整合。〔履歴 Simulation.cs#127〕
        producers.Add(new Producer { GoodId = Catalog.HousingId, RecipeName = "鉄筋コンクリート", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CementId, 0.3), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.HousingId))] });
        // ラジオ(1.60)の後へ。〔履歴 Simulation.cs#128〕
        producers.Add(new Producer { GoodId = Catalog.RubberId, RecipeName = "合成ゴム", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = new Share(1.0), BuysLabor = true,
            Inputs = [new Input(Catalog.CoalId, 0.3), new Input(Catalog.LimestoneId, 0.3), new Input(Catalog.ElectricityId, 0.2661), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RubberId))] });
    }

    /// <summary>
    /// 階級 c の識字は ρ_c z / (1 + ρ_c z)。〔履歴 Simulation.cs#129〕
    /// </summary>
    internal static double[] ClassLiteracy(NationConfig cfg, Coefficients coef)
    {
        double[] rho = coef.LiteracyOddsByClass;
        double noble = cfg.Nobility * (1 - cfg.ClergyFrac);
        double[] w = [noble, cfg.Nobility * cfg.ClergyFrac, cfg.Peasant, cfg.Worker, cfg.Bourgeois,
                      cfg.Professional * (1 - cfg.IntelligentsiaFrac), cfg.Professional * cfg.IntelligentsiaFrac,
                      cfg.Merchant, cfg.Artisan, noble + cfg.Bourgeois];
        double total = 0; for (int c = 0; c < w.Length; c++) total += w[c];
        var lit = new double[w.Length];
        double target = cfg.Literacy;
        if (!(total > 1e-9) || !(target > 1e-9)) return lit;
        if (!(target < 1 - 1e-9)) { for (int c = 0; c < lit.Length; c++) lit[c] = 1; return lit; }
        double lo = 0, hi = 1;   // t = z/(1+z)∈(0,1)：識字 = ρ t / ((1−t) + ρ t)
        for (int it = 0; it < 60; it++)
        {
            double t = 0.5 * (lo + hi), m = 0;
            for (int c = 0; c < w.Length; c++) m += w[c] * (rho[c] * t / ((1 - t) + rho[c] * t));
            if (m / total < target) lo = t; else hi = t;
        }
        double tt = 0.5 * (lo + hi);
        for (int c = 0; c < lit.Length; c++) lit[c] = rho[c] * tt / ((1 - tt) + rho[c] * tt);
        return lit;
    }

    // 階級（代表スケール 1単位＝1000人）。貯蓄＝サイズ×階級別の一人当たり富。識字・自給は設定から。
    private static Pop MkPop(int id, string name, bool urban, double size, double lit, double wealthPer, bool closed = false, bool modern = false, double subs = 0)
        => new() { ClassId = id, ClassName = name, IsUrban = urban, Size = size, Savings = new MoneyStock(size * wealthPer), Satisfaction = 1.0, Literacy = lit, Closed = closed, IsModernElite = modern, SubsistenceBasis = subs };


    /// <summary>
    /// 入植者が過半数に達した植民地（Colonial.Step §5 が判定）を、宗主の**完全な州**（pop/生産者/市場・RegionTier.Integrated）として実体化する。〔履歴 Simulation.cs#130〕
    /// </summary>
    internal static Region PromoteColonyToRegion(World w, Coefficients coef, Colony col, RegionTier tier = RegionTier.Integrated)
    {
        var (clat, clon) = Climate.CityCoord(col.Name);
        if (clat == 0 && clon == 0) { clat = col.Latitude; clon = col.Longitude; }   // 表に無い名前（将来の新植民地）は植民地オブジェクトの座標（積出港）で代用
        var (goods, markets, producers) = CatalogEconomy(0.0, coef);   // 自作農（RGOの残余は床0.3＝自分の土地の地代・所有は農民自身）
        // 気候ゲート（BuildProvince と同じ規則）：気候ゲート財（茶葉/ゴム…旧colonial）は賦存列挙のみ・米/綿花は適地のみ産する
        producers.RemoveAll(pr => Catalog.IsColonial(pr.GoodId) && System.Array.IndexOf(col.Resources, pr.GoodId) < 0 && SiteRowFor(col.Name, pr.GoodId, coef, !w.StepsDiplomacy) < 0);
        producers.RemoveAll(pr => pr.GoodId == Catalog.RiceId && !Climate.RiceSuitable(clat, clon) && System.Array.IndexOf(col.Resources, pr.GoodId) < 0);
        producers.RemoveAll(pr => pr.GoodId == Catalog.CottonId && !Climate.CottonSuitable(clat) && System.Array.IndexOf(col.Resources, pr.GoodId) < 0);
        foreach (var pr in producers)
        {
            // site〔履歴 Simulation.cs#131〕
            int site = pr.IsRgo ? SiteRowFor(col.Name, pr.GoodId, coef, !w.StepsDiplomacy) : -1;
            if (site >= 0 && System.Array.IndexOf(col.Resources, pr.GoodId) < 0)
            {
                ApplyEndowment(pr, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, 1.0, coef);   // ApplyEndowment〔履歴 Simulation.cs#132〕
                pr.SiteRow = site;
            }
            else if (System.Array.IndexOf(col.Resources, pr.GoodId) >= 0)
            {
                pr.SiteRow = site;
                ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, !w.StepsDiplomacy) * coef.ResourceEndowmentScale, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, coef);   // 植民地の固有資源＝そのまま州の賦存へ（一次データ）。〔履歴 Simulation.cs#133〕
                // Colonial 格のとき賦存RGOに同じ上書き・持ち主の口座＝資本家（入植の農園主＝開いた階級・0から流入で育つ）。〔履歴 Simulation.cs#134〕
                if (tier == RegionTier.Colonial && pr.IsRgo && Colonial.ExportCrop(pr.GoodId) && coef.PlantationRentShare.Value > pr.RentShare.Value)
                {
                    pr.RentShare = coef.PlantationRentShare; pr.OwnerPopIndex = 3;
                }
            }
            else if (!w.StepsDiplomacy && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;   // IsScarceMineral〔履歴 Simulation.cs#135〕
            else if (pr.IsRgo && !Climate.StapleSelfSufficient(pr.GoodId, clat, clon) && !Catalog.IsUbiquitousMineral(pr.GoodId, coef)) ApplyEndowment(pr, ResourceScarcity * coef.ResourceScarcityScale, ResourceScarcity * coef.ResourceScarcityScale, coef);
        }
        // 階級構成：実人口は現地住民だけ（保存則＝湧かさない）。〔履歴 Simulation.cs#136〕
        double natives = Math.Max(0, col.PopSize);
        double peasantSize = natives;
        // 現地住民の自給率：植民地POPは元々市場の外で食っていた（台帳の外）＝自給農。〔履歴 Simulation.cs#137〕
        double subs = 0.5; var subsKind = SubsistenceBasisKind.OpeningShare;
        foreach (var cp in w.Region.Pops) if (cp.ClassId == PopClass.Peasant) { if (cp.SubsistenceBasisKind == SubsistenceBasisKind.Productivity) { subs = cp.SubsistenceBasis; subsKind = cp.SubsistenceBasisKind; } else subs = Math.Max(subs, cp.SubsistenceBasis); break; }
        var nobility  = MkPop(0, "貴族",       false, 0, 0.5, 0, closed: true);
        var peasant   = MkPop(1, "農民",       false, peasantSize, 0.3, 0, subs: subs);
        peasant.SubsistenceBasisKind = subsKind;   // SubsistenceBasisKind〔履歴 Simulation.cs#138〕
        var worker    = MkPop(2, "工場労働者", true,  0, 0.3, 0);   // 器（Size0）＝識字は旧の入植者の既定値 0.3 のまま
        var bourgeois = MkPop(3, "資本家",     true,  0, 0.5, 0, modern: true);
        var engineer  = MkPop(4, "技師",       true,  0, 0.5, 0, modern: true);
        var clergy    = MkPop(6, "聖職者",     false, 0, 0.5, 0, closed: true);
        var teacher   = MkPop(7, "教師",       true,  0, 0.5, 0, modern: true);
        var bureaucrat = MkPop(8, "官僚",      true,  0, 0.5, 0, modern: true);
        var doctor    = MkPop(9, "医者",       true,  0, 0.5, 0, modern: true);
        var merchant  = MkPop(10, "商人",      true,  0, 0.5, 0, modern: true);
        var artisan   = MkPop(11, "職人",      true,  0, 0.4, 0);
        var servant   = MkPop(12, "使用人",    true,  0, 0.3, 0);
        servant.SetAdultFemaleShare(0.75);   // 職業POPの女性比（BuildProvince/BuildFrontier と同値・20歳以上の層＝段4e 後半）
        clergy.SetAdultFemaleShare(0.10); bourgeois.SetAdultFemaleShare(0.20); engineer.SetAdultFemaleShare(0.02);
        teacher.SetAdultFemaleShare(0.25); bureaucrat.SetAdultFemaleShare(0.03); doctor.SetAdultFemaleShare(0.05);
        merchant.SetAdultFemaleShare(0.25); artisan.SetAdultFemaleShare(0.30);
        // 農民pop＝現地住民（民族の実体＝CoreEthnicShare0・不満と識字を運搬）。蓄えは持ち込まない（上の注記）。
        if (peasantSize > 1e-9)
        {
            peasant.Literacy = col.Literacy * natives / peasantSize;
            peasant.Militancy = col.Militancy * (natives / peasantSize);
            peasant.CoreEthnicShare = new Share(0.0);   // 現地住民＝宗主の中核民族ではない
        }
        worker.CoreEthnicShare = new Share(1.0);   // 労働者枠（器）は宗主の中核民族
        // 新＝新しい州は作らず、上で組んだ住民（現地の農民＋入植の農民・労働者＝蓄え・識字・不満・中核民族の割合）をその州の同じ階級へ運ぶ   （運搬は州間の移住・入植と同じ1本＝Territory.MoveBetweenRegions）。〔履歴 Simulation.cs#139〕
        if (w.Regions.Find(x => x.Name == col.Name) is { } same)
        {
            double pop0 = same.SumPopulation();
            // peasant〔履歴 Simulation.cs#140〕
            foreach (var mp in new[] { peasant, worker })
                if (mp.Size > 1e-9 && same.PopOf(mp.ClassId) is { } dp) Territory.MoveBetweenRegions(mp, dp, mp.Size, coef);
            double inP = same.SumPopulation() - pop0;
            w.PopulationDelta += inP;   // inP〔履歴 Simulation.cs#141〕
            if (Instruments.On) Instruments.PopMove(Instruments.PopMoveKind.ColonyMerge, null, same, inP);   // (1261) popclose_s
            same.AnnexBaggage += inP;   // 案Y（観測専用）：併合の持ち込み人口＝本土の自然増ではない（新しい州のときと同じ記帳）
            return same;
        }
        var soldier = new Pop { ClassId = 5, ClassName = "兵士", IsUrban = false, Satisfaction = 1.0, IsSoldier = true, Ages = Pop.SoldierAges(), Literacy = peasant.Literacy };   // Size0の器（辺境と同じ作法）＝軍事費が平時均衡で徴募
        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, clergy, teacher, bureaucrat, doctor, merchant, artisan, servant, soldier };
        // 渡さないと >0 の世界で StepMonth が [eduIdx+1] を公立学校と誤認する（実体は医療）＝型の統合が起こす静かな破損の型。〔履歴 Simulation.cs#142〕
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 6, bureaucratIdx: 7, doctorIdx: 8, coef: coef, clergyIdx: 5);
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, coef);   // AddAdvancedProducers〔履歴 Simulation.cs#143〕
        AddArtisanProducers(producers, artisanIdx: 10);
        producers.Add(new Producer { GoodId = Catalog.DomesticServiceId, Productivity = 1.0, Output = 0, MarginalCost = 0.3, LaborPopIndex = 11, OwnerPopIndex = 11, RentShare = default });
        AddAlternateRecipes(producers, coef);   // AddAlternateRecipes〔履歴 Simulation.cs#144〕
        var region = new Region
        {
            // 沿岸かどうかは landmass.json の coastal（一次データ）から引く。〔履歴 Simulation.cs#145〕
            Name = col.Name, IsPort = Landmass.CoastalOf(col.Name, fallback: false),
            Tier = tier, Dominance = 0.0,   // 支配度0＝取った年から数える
            Rule = Places.RuleOf(col.Name) is { } colRule0 ? colRule0 : [0.0, 1.0, 0.0, 0.0],   // ★支配度の一般化 段1：昇格した植民地＝places.json の rule（開始時のデータ・土地の側）から始める（無い名前は間接統治〔在地1.0〕・段2で支配度と同じ物理で国家区分が育つ）
            RuleInit = Places.RuleOf(col.Name) is { } colRule1 ? colRule1 : [0.0, 1.0, 0.0, 0.0],   // ★E-06 昇格した植民地＝支配度0から積み上げる（引き留め>圧力で育つ）   // ★格は呼び手が決める（入植者昇格＝Integrated／開始時の一括昇格＝Colonial）
            Terrain = Places.TerrainOf(col.Name),   // Terrain〔履歴 Simulation.cs#146〕
            Resources = [.. col.Resources],
            // (1278) c131cr 旗 1：札は土地のデータ（places.json の culture・religion）。データに無い名は空＝首都の札へ逃がさない（番人 ColonyLabelsFromDataTests）
            Culture = coef.ColonyLabelsFromData > 0 ? Places.CultureOf(col.Name) ?? "" : w.Region.Culture,
            Religion = coef.ColonyLabelsFromData > 0 ? Places.ReligionOf(col.Name) ?? "" : w.Region.Religion,
            ArableLand = Math.Max(1.0, peasantSize) * LandHeadroom,   // 本土州と同じ規則（耕地賦存＝初期農民×余裕）
            CapitalEnabled = true, Industrialization = 0.0,
            CulturalGoodId = w.Region.CulturalGoodId,
            InvestorPopIndex = 3, MeritFromIndex = 2, MeritToIndex = 4,
            ClergyPopIndex = 5, TeacherPopIndex = 6, BureaucratPopIndex = 7, DoctorPopIndex = 8, MerchantPopIndex = 9, ArtisanPopIndex = 10, ServantPopIndex = 11, SoldierPopIndex = 12, CriminalPopIndex = AddCriminalPop(pops, coef, peasant.Literacy),
            EducationProducerIndex = eduIdx,
            Goods = goods, Pops = pops, Producers = producers, Markets = markets,
        };
        region.SeaBasin = SeaGraph.BasinAt(clat, clon);   // 港州＝海盆を積出港座標から（WirePortDistances の港側と同じ規則）
        if (!Landmass.IsRegistered(col.Name)) w.LogAdd($"{w.Year} {col.Name}州はlandmass.json未登録＝IsPort=falseへ倒した（沈黙回避）");   // fail-closed の可視化
        foreach (var m in region.Markets) { m.Stock = 0; m.PrevStock = 0; }   // 在庫0で開始（上記の設計判断）。Last*=0 は初期値どおり＝財収支恒等式（GoodsBalance）が最初から閉じる
        if (Catalog.AdminId < region.Markets.Length) region.AdminFill = 0.0; Territory.Adopt(w, region);   // AdminFill〔履歴 Simulation.cs#147〕
        // 保存の記帳（同一値を両辺に使う＝CedeTerritory の作法）
        double P = region.SumPopulation();
        w.PopulationDelta += P;       // 現地住民＝台帳の外から（ゲーム外流入の作法）。〔履歴 Simulation.cs#148〕
        // 葡帯の注記「ブラジル喪失は植民地＝本土人口に非算入」と同じ物差し）。〔履歴 Simulation.cs#149〕
        region.OriginPowerId = w.SelfPower?.Id ?? -1;
        region.AnnexBaggage = P;
        return region;
    }

    public void Run(int quarters)
    {
        for (int q = 0; q < quarters; q++) StepQuarter();
    }

    /// <summary>
    /// 四半期＝90回の日次刻み。〔履歴 Simulation.cs#150〕
    /// </summary>
    public void StepQuarter() { for (int d = 0; d < Calendar.DaysPerQuarter; d++) StepDay(); }

    /// <summary>
    /// 1日進める（三層時計の最小単位）。〔履歴 Simulation.cs#151〕
    /// </summary>
    public bool StepDay()
    {
        StepDayOpen();
        StepDayDaily();
        bool q = StepDayClose();
        World.FlushLog();   // 単独呼び＝境界ごとに自国分を即時反映（多国は MultiNationSim が国順にフラッシュ＝順序決定論）
        return q;
    }

    /// <summary>開幕境界（四半期首）。⚠多国では逐次で呼ぶ。</summary>
    public void StepDayOpen()
    {
        ProbeCheck("外部層");   // 前日の最終点から今日の開始まで＝共有層（外交/国際市場/移民/国際金融）で動いた分は各所が台帳計上済みのはず
        if (_dayInQuarter == 0) { QuarterStart(); ProbeCheck("四半期首"); }
    }

    /// <summary>
    /// ⚠ 境界相の外から入力を書く動詞はプレイヤーの侵略（World.Invade＝兵の疫病死と植民地の州化）だけで、そこで表から外す。〔履歴 Simulation.cs#152〕
    /// </summary>
    internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, System.Runtime.CompilerServices.StrongBox<(double NominalGdp, double AvgSat)>> CreditMonth = new();

    /// <summary>
    /// 純粋な日次（価格形成・建設・信用）＝**国ローカル**＝並列可。〔履歴 Simulation.cs#153〕
    /// </summary>
    public void StepDayDaily()
    {
        // 日次（鼓動）：価格形成・建設の進捗・信用。〔履歴 Simulation.cs#154〕
        int hFlow = World.Coef.PriceFlowStepDays;
        if (hFlow < 1 || Calendar.DaysPerMonth % hFlow != 0)
            throw new System.InvalidOperationException($"PriceFlowStepDays={hFlow} は {Calendar.DaysPerMonth} の約数でない（旬は月をまたげない）");
        int hPrice = World.Coef.PriceOnlyStepDays > 0 ? World.Coef.PriceOnlyStepDays : hFlow;   // ★(spg2) ① 国内価格だけの刻み（0＝同じ旬）
        if (Calendar.DaysPerMonth % hPrice != 0)
            throw new System.InvalidOperationException($"PriceOnlyStepDays={hPrice} は {Calendar.DaysPerMonth} の約数でない（旬は月をまたげない）");
        bool priceDay = (_dayInQuarter + 1) % hPrice == 0;
        bool spanStart = _dayInQuarter % hFlow == 0;   // 旬の初日（建設と信用＝S3c）
        long[]? cost = priceDay || spanStart ? World.Cost : null;   // 計算量の台帳（診断・この国の台帳＝並列の日次相でも国ごと）
        bool monthStartSpan = _dayInQuarter % Calendar.DaysPerMonth < hPrice;   // monthStartSpan〔履歴 Simulation.cs#155〕
        foreach (var r in World.Regions)
            {
                r.PriceLevel = World.PriceLevel * r.LocalPriceMultiplier;   // 国家物価×地域乗数＝地域別の物価水準（金融中心は高・辺境は低）。1州なら乗数1＝較正中立
                r.FarmRentCap = new Share(System.Math.Min(RentCap(World.LandReform, World.Coef.LandReformRentCap.Value), RentCap(World.SerfEmancipation, World.Coef.SerfEmancipationRentCap.Value)));   // 地代上限＝土地改革/農奴解放の段階で（限定的＝なしと全面の中間）。元の地代がこれ未満なら効かない
                r.CrowdingPremium = System.Math.Max(0, (World.GovBondRatePerYear - World.Coef.BondBaseRatePerYear).Value);   // 政府借入の金利プレミアム→民間投資ハードルへ（クラウディングアウト。金本位の信用国は GovBondRatePerYear が低く弱い＝条件付き）
                r.FinancialHeat = World.FinancialHeat;   // FinancialHeat〔履歴 Simulation.cs#156〕
                // CivilWarActive〔履歴 Simulation.cs#157〕
                r.CivilWarActive = World.InCivilWar;   // CivilWarEconomyDisruption=0で不読＝恒等）〔履歴 Simulation.cs#158〕
                if (priceDay) MarketFlow.StepDay(r, World.Coef, hPrice, cost, monthStartSpan, World.MinimumWage);   // 物価水準を複製し需給を読み、旬の h 日ぶん価格を少しずつ動かす（月の2旬目以降は不動点に着いた張り付いた財を眠らせる）
            }
        ProbeCheck("日次価格");
        if (spanStart) World.StepConstruction(hFlow);                               // 工場建設の進捗（旬の初日に h 日ぶん・労務費を働いた日数ぶん払って進む）
        ProbeCheck("日次建設");
        if (spanStart)
        {
            (double NominalGdp, double AvgSat)? creditMonth = null;
            if (World.Bank is not null)   // World〔履歴 Simulation.cs#159〕
            {
                bool fresh = !CreditMonth.TryGetValue(World, out var box);
                if (box is null) { box = new(); CreditMonth.Add(World, box); }
                if (fresh || _dayInQuarter % Calendar.DaysPerMonth == 0)   // 月の初日は旬の初日（h は30の約数）
                    box.Value = (World.NominalGdpPerMonth.Value, NationalScore.MeanSatisfaction(World));
                creditMonth = box.Value;
            }
            Credit.Step(World, World.Coef, creditMonth, hFlow);                     // 信用（与信・信頼）も旬次（旬の初日に h 日ぶん・速度は日数配分）
            cost![CostLedger.CreditSpans]++;                                         // 計算量の台帳（診断）：信用は国×旬
        }
        ProbeCheck("日次信用");
        // WarmupFrozen〔履歴 Simulation.cs#160〕
        if (!World.WarmupFrozen) World.Tick++;
    }

    /// <summary>締め境界（月次決算・四半期末）。⚠多国では逐次で呼ぶ＝MonthSettle は共有（Diplomacy.Log 等）へ触る。</summary>
    public bool StepDayClose()
    {
        if ((_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) MonthSettle();   // 月次：クリアリング・賃金/地代/税・労働再配分・移民・外交・人口

        bool quarterDone = _dayInQuarter == Calendar.QuarterEndDayIndex;
        if (quarterDone) { QuarterEnd(); ProbeCheck("四半期末"); }
        _dayInQuarter = (_dayInQuarter + 1) % Calendar.DaysPerQuarter;
        return quarterDone;
    }

    /// <summary>
    /// 保存量プローブ（leakhunt・恒久計測器）：保存量 D＝基礎貨幣−累積台帳（貿易/鋳造/不換発行）は内部処理のどこでも不変のはず。〔履歴 Simulation.cs#161〕
    /// </summary>
    public static bool ConservationProbe;
    private double _probeDLast = double.NaN;
    internal void ProbeCheck(string label)
    {
        if (Instruments.On) Instruments.UrxMark(World, label);   // Instruments〔履歴 Simulation.cs#162〕
        if (!ConservationProbe) return;
        double now = (World.BaseMoney - World.CumulativeTradeFlow - World.CumulativeGoldMinted - World.CumulativeFiatIssued).Value;
        if (!double.IsNaN(_probeDLast) && Math.Abs(now - _probeDLast) > 1e-4)
            Console.Error.WriteLine($"[PROBE国内] {label} {World.Region.Name} {World.Year}年T{World.Tick} ΔD={now - _probeDLast:F6}");
        _probeDLast = now;
    }

    // 四半期の戦略前段：金融ショック・AIの政策決定・政府の政策反映・建設の進行。〔履歴 Simulation.cs#163〕
    private void QuarterStart()
    {
        World.Cost[CostLedger.QuarterStartPops] += ActivePopCount();   // 計算量の台帳（診断）：四半期首（AI・議会・政府の反映）は POP×国に比例
        // ⚠助走は _quarterIndex を進めないので、ガードが無いと ShockQuarterIndex=0 のシナリオで**助走の全四半期**に発火する。〔履歴 Simulation.cs#164〕
        if (!World.WarmupFrozen && _quarterIndex == _shockQuarter && World.Bank is not null)
            World.Bank.Confidence = Math.Max(0, World.Bank.Confidence - _shockMagnitude);
        // 後ろに置くと**AIの判断を上書きしてしまう**（AIはスタンス別の率を使うのに、   ここは一律の近代化率）＝AI挙動を変えない原則に反する。〔履歴 Simulation.cs#165〕
        bool decideNow = !(World.WarmupFrozen && World.Coef.WarmupNoDecisions > 0);   // (1343) c141wd 旗 WarmupNoDecisions 1＝助走の間は全ての国で決定の段（下の 2 つ）を飛ばす
        if (decideNow && World.SelfPower is { } self)
        {
            double fs = UtilityAgent.FiscalStressOf(World);
            // AI国は直後の DecideQuarter が自分の値で上書きするので無害だったが、     NullAgent のプレイヤー国には上書きが来ない＝**UIで動かしても次の四半期に必ず 0.08 へ戻る**（スライダーが効かない）。〔履歴 Simulation.cs#166〕
            if (!self.IsPlayer) World.ModernizationFrac = UtilityAgent.AutoModernizationFrac(World, fs);
            // AI は DecideQuarter の中で同じ関数を通る＝ここは**同じ関数を同じ引数で**呼ぶだけ   （割り振りの式はどこにも書き写さない）。〔履歴 Simulation.cs#167〕
            else UtilityAgent.SetNavalUpkeepShare(World);
        }
        if (decideNow) { if (Instruments.On) Instruments.MonAiBefore(World); _agent.DecideQuarter(World); if (Instruments.On) Instruments.MonAiAfter(World); }   // DecideQuarter〔履歴 Simulation.cs#168〕
        UpdateRegionalPriceLevels();           // 地域別の貨幣的物価乗数（地域物価・名目賃金の格差）を四半期ごとに更新。日次の StepDay が r.PriceLevel に適用
        Parliament.Step(World, World.Coef);   // 議会・選挙（段5）：得票・選挙・議席。〔履歴 Simulation.cs#169〕
        // 窓の中の 2 段が書くのは国の欄・州の欄・法の欄・闘争度ほか。〔履歴 Simulation.cs#170〕
        World.PopSums = new PopWindow();
        try
        {
            World.ApplyModernizationInvestment();      // 近代化投資（UI・AI 共通の動詞）
            Government.Step(World, World.Coef);   // 政策を反映（法整備＝即時／スライダー＝利益集団の力で律速して漸進）
        }
        finally { World.PopSums = null; }
        if (Instruments.On) Instruments.AfterGovernment(World);   // 計器（読むだけ）：戦時段の解禁・軍事費の段の目標と施行（四半期の頭）
    }

    /// <summary>地域別の貨幣的物価水準の乗数（②集計粒度）：各州の1人あたり貯蓄（手元現金の厚み）が国平均からどれだけ乖離するかで、
    /// 国家物価水準（World.PriceLevel）を上下させる。現金の厚い金融中心州は物価・名目賃金が高く、現金の乏しい辺境州は低い
    /// （19世紀の地域物価/賃金の格差・辺境のデフレ）。1州なら州=国平均で乗数1.0＝較正中立。日次の MarketFlow が r.PriceLevel として価格帯/参照価格アンカーに使う。</summary>
    internal void UpdateRegionalPriceLevels()   // internal＝番人テスト（InvariantTests 現金が尽きた州…）が直接呼ぶ。可視性だけの変更
    {
        var c = World.Coef;
        bool priceOn = c.RegionalPriceLevelWeight > 0;   // 無効＝全州一律（従来挙動・較正中立）。労働価格プレミアムの簿記は常に更新（使用は OpportunityWageWeight でゲート）
        // 各和の足し込みの順は旧と同じ＝1 ビットも変わらない（国の和は州ごとの和の合計ではない＝足し方が違う）。〔履歴 Simulation.cs#171〕
        double natSav = 0, natEarn = 0, natPop = 0;
        // 本土の州が無い（人口 0）ときは全ての州。〔履歴 Simulation.cs#172〕
        double mainSav = 0, mainEarn = 0, mainPop = 0;
        var regSums = new double[World.Regions.Count * 3];
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            double rSav = 0, rEarn = 0, rPop = 0;
            foreach (var p in r.Pops)
            {
                double sv = p.Savings.Value, ea = p.EarnedPerMonth.Value, sz = p.Size;
                natSav += sv; natEarn += ea; natPop += sz;
                rSav += sv; rEarn += ea; rPop += sz;
            }
            regSums[ri * 3] = rSav; regSums[ri * 3 + 1] = rEarn; regSums[ri * 3 + 2] = rPop;
            if (r.Tier != RegionTier.Colonial) { mainSav += rSav; mainEarn += rEarn; mainPop += rPop; }
        }
        if (mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }
        double natPerCap = natPop > 1e-9 ? natSav / natPop : 0;
        double natEarnPerCap = natPop > 1e-9 ? natEarn / natPop : 0;   // 国平均の1人あたり月次所得（QuarterStart時点＝共有層〔戦死・移民・国際市場・資本市場の受け取り〕が直前の月の決算の後に書いた分を含む）
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            double rSav = regSums[ri * 3], rEarn = regSums[ri * 3 + 1], rPop = regSums[ri * 3 + 2];
            double mult = 1.0;
            if (priceOn && natPerCap > 1e-9 && rPop > 1e-9)
            {
                double rPerCap = rSav / rPop;   // 州の1人あたり貯蓄（手元現金の厚み）
                // 退避値は分母の意味から決まる＝現金ゼロの州は「国平均に対して最も薄い」＝辺境デフレの底（RegionalPriceLevelMin）。〔履歴 Simulation.cs#173〕
                mult = !(rPerCap > 0)
                    ? c.RegionalPriceLevelMin
                    : Math.Clamp(Math.Pow(rPerCap / natPerCap, c.RegionalPriceLevelWeight), c.RegionalPriceLevelMin, c.RegionalPriceLevelMax);
            }
            r.LocalPriceMultiplier = mult;   // 1州なら rPerCap==natPerCap で比1→乗数1＝較正中立
            // 労働価格プレミアム（キュー3・金発見移民）＝州の1人あたり月次所得の対国平均比。[0.25,4]クランプ＋四半期EMA（月次ノイズの平滑）。1州なら比1→1.0＝較正中立
            double prem = natEarnPerCap > 1e-9 && rPop > 1e-9 ? Math.Clamp(rEarn / rPop / natEarnPerCap, 0.25, 4.0) : 1.0;
            r.WagePremium = 0.75 * r.WagePremium + 0.25 * prem;
        }
    }

    /// <summary>月初の国家の支払いの請求のうち生産者1件の分（工事の工賃の1か月分・国営持分の赤字の見込み）＝旧の州×生産者のループの本体そのもの
    /// （2つの足し算を同じ順で＝和は1ビットも変わらない）。</summary>
    private static void AddStateClaim(Producer pr, Region r, World w, ref double claims)
    {
        if (pr.ConstructionLeft > 0) claims += Calendar.PerDayToPerMonth(pr.ConstructionPayPerDay);   // 工事の工賃の1か月分（World.StepConstruction）
        if (Instruments.On && pr.ConstructionLeft > 0) Instruments.Claim(w, Instruments.Item.StateConstruction, Calendar.PerDayToPerMonth(pr.ConstructionPayPerDay));
        double loss = pr.LastCost - pr.LastRevenue;
        if (loss > 0) claims += World.StateProfitShare(pr, r, w.Coef) * loss;   // 国営持分の赤字の見込み（先月の損益）
        if (Instruments.On && loss > 0) Instruments.Claim(w, Instruments.Item.StateLoss, World.StateProfitShare(pr, r, w.Coef) * loss);
    }

    /// <summary>稼働中の州の POP の件数（計算量の台帳の四半期の計数だけが読む＝診断）。</summary>
    private int ActivePopCount()
    {
        int n = 0;
        foreach (var r in World.Regions) n += r.Pops.Count;
        return n;
    }

    // 四半期の戦略後段：技術・制度・政治・社会移動・景気循環トラッカー（90日目）。〔履歴 Simulation.cs#174〕
    private void QuarterEnd()
    {
        World.Cost[CostLedger.QuarterEndPops] += ActivePopCount();   // 計算量の台帳（診断）：四半期末（技術・制度・政治・社会移動ほか）は POP×国に比例
        // 止める＝技術の前進・膨張意欲の累積・四半期スナップショット・暦（Quarter/Year/_quarterIndex）。〔履歴 Simulation.cs#175〕
        bool warm = World.WarmupFrozen;
        if (!warm) TechEducation.GrowTech(World, World.Coef);          // 助走中は WarmupTech が別途担う＝ここで二重に進めない〔履歴 Simulation.cs#176〕
        // 多国は MultiNationSim の四半期末の共有層が接触を求めてから判定する（同じ関数）。〔履歴 Simulation.cs#177〕
        if (!warm && World.StepsDiplomacy) { Discovery.AdvancePrice(World, World.Coef); Discovery.Step(World, null, World.Coef); }   // StepsDiplomacy〔履歴 Simulation.cs#178〕
        else if (warm && World.StepsDiplomacy) World.DiscoveryLossQ = null;   // StepsDiplomacy〔履歴 Simulation.cs#179〕
        // Step〔履歴 Simulation.cs#180〕
        Politics.Step(World, World.Coef);                   // §9-E
        if (!(warm && World.BakeRun?.StopSocialMobility == true))
        {
            if (Instruments.On && !warm) { Instruments.UrxEnter(World); Instruments.MerchEnter(World); }   // Instruments〔履歴 Simulation.cs#181〕
            Meritocracy.Step(World, World.Coef);                // Step〔履歴 Simulation.cs#182〕
            if (Instruments.On && !warm) Instruments.UrxExit(World, "社会移動");   // Instruments〔履歴 Simulation.cs#183〕
        }
        if (!warm) World.TickExpansionWill();                // 膨張意欲の累積（案Y・段B）。助走で10年ぶん貯めて開幕に侵略が噴くのを防ぐ＝時間の経過そのもの
        World.UpdateGdpCycle();   // 景気循環トラッカー更新（次四半期のAIの反景気循環的金融政策の入力）
        World.UpdatePriceLevel(World.Coef);   // 物価水準（数量説）＝広義マネー/実質産出の伸び差でインフレ/デフレ
        if (!warm) Snapshot();   // 現読者は添字参照のみ＝挙動不変）〔履歴 Simulation.cs#184〕
        if (warm) return;   // ★暦を進めない：助走を何年回しても開始は1783/Q1（1914断面・全長 GameQuarters・TechOnset の年ゲートが動かない）
        World.Quarter++;
        if (World.Quarter > Calendar.QuartersPerYear) { World.Quarter = 1; World.Year++; }
        _quarterIndex++;
    }

    // 月次決算（旧 StepMonth の本体）：金の動く決済はここ。日次ループは StepDay が外から駆動する。
    /// <summary>
    /// 「座礁」＝同じ財で発見の表に行を持つ新製法の産出（Output×Scale）が旧製法を初めて上回った四半期（塩漬け→冷蔵・石炭ガス→石油…）。〔履歴 Simulation.cs#185〕
    /// </summary>
    private void LogRecipeTransitions()
    {
        int nGoods = Catalog.Goods.Length;
        Span<double> newOut = stackalloc double[nGoods];
        Span<double> oldOut = stackalloc double[nGoods];
        Span<double> artOut = stackalloc double[nGoods];
        foreach (var r in World.Regions)
        {
            r.RecipeTransitionFlags ??= new byte[nGoods];
            var flags = r.RecipeTransitionFlags;
            if (flags.Length < nGoods) continue;   // 財が増えた旧セーブ＝安全側（次のロードで揃う）
            newOut.Clear(); oldOut.Clear(); artOut.Clear();
            string?[] newName = new string?[nGoods], oldName = new string?[nGoods];
            foreach (var pr in r.Producers)
            {
                if (pr.GoodId < 0 || pr.GoodId >= nGoods || pr.Bankrupt) continue;
                double q = pr.Output * pr.Scale;
                if (!(q > 0)) continue;
                if (pr.RecipeName == "職人") { artOut[pr.GoodId] += q; oldOut[pr.GoodId] += q; oldName[pr.GoodId] ??= "職人"; }
                else if (pr.DiscoveryRow >= 0 && !(pr.DiscoveryRow == Discovery.SyntheticDyeRow && !(World.Coef.DiscoveryEngineerHalfPerMonth > 0))) { newOut[pr.GoodId] += q; newName[pr.GoodId] ??= pr.RecipeName; }   // DiscoveryEngineerHalfPerMonth〔履歴 Simulation.cs#186〕
                else { oldOut[pr.GoodId] += q; oldName[pr.GoodId] ??= (pr.RecipeName.Length > 0 ? pr.RecipeName : "従来の製法"); }
            }
            for (int g = 0; g < nGoods; g++)
            {
                double total = newOut[g] + oldOut[g];
                if (!(total > 1e-9)) continue;
                if ((flags[g] & 1) == 0 && newOut[g] > oldOut[g] && oldOut[g] > 1e-9 && newName[g] is { } nn)
                {
                    flags[g] |= 1;
                    World.LogAdd($"{World.Year} {r.Name}：{Catalog.Goods[g].Name}の{nn}が{oldName[g] ?? "旧製法"}を上回った（旧設備の座礁）");
                }
                double artShare = artOut[g] / total;
                if (artShare >= 0.5) flags[g] |= 2;
                if ((flags[g] & 2) != 0 && (flags[g] & 4) == 0 && artShare < 0.25)
                {
                    flags[g] |= 4;
                    World.LogAdd($"{World.Year} {r.Name}：{Catalog.Goods[g].Name}の職人が没落（工場化）");
                }
            }
        }
    }

    // POP の貯蓄を書く口は移し方の 1 本（World.Transfer）と Pop の Take／Distribute／Carry／SettleMonth／Blend／KillPops だけ＝下の「書く」は呼び手の段に数える。〔履歴 Simulation.cs#187〕
    private void MonthSettle()
    {
        // 四半期の締め月（_dayInQuarter==89＝quarterDone と同時に走る月次決算）だけ true・他2回（29/59日目）は false。〔履歴 Simulation.cs#188〕
        World.IsQuarterEndMonth = _dayInQuarter == Calendar.QuarterEndDayIndex;
        if (World.IsQuarterEndMonth) LogRecipeTransitions();   // LogRecipeTransitions〔履歴 Simulation.cs#189〕
        // 豊作/不作：年1回ロール（決定論RNG）。〔履歴 Simulation.cs#190〕
        int harvestYear = (int)((World.Tick - Calendar.DaysPerMonth) / Calendar.DaysPerYear);
        // 初回のロールは最初の実月（Tick=30）＝上の「初回ロール（Tick=30）は不変」の設計どおり。〔履歴 Simulation.cs#191〕
        if (!World.WarmupFrozen && harvestYear != _lastHarvestYear)
        {
            _lastHarvestYear = harvestYear;
            // AR(1)（B harvest-iid-no-persistence）：ρ=0 で純IID（現行恒等）。ε のRNG消費は1回のまま＝決定論系列不変。
            double u = Rng.NextDouble();   // RNG消費はSkew/ρに関わらず1回（決定論系列不変）
            double harvestEps = 1.0 + SkewHarvest(u, World.Coef);   // Skew=0 で (u-0.5)*2*HarvestVariation＝対称一様（現行恒等）
            double rho = World.Coef.HarvestPersistence;
            // 革新係数を√(1−ρ²)にすると定常分散＝εの分散を厳密保存＝連続凶作の累積（多年飢饉）を得つつ凶作頻度を維持。〔履歴 Simulation.cs#192〕
            World.HarvestFactor = rho > 0
                ? 1.0 + rho * (_prevHarvestFactor - 1.0) + Math.Sqrt(1.0 - rho * rho) * (harvestEps - 1.0)
                : harvestEps;   // ρ=0（対照腕）：前年項なし＝純IID。既定 HarvestPersistence=0.35 でlive
            _prevHarvestFactor = World.HarvestFactor;
            if (World.HarvestFactor < 0.80)   // 顕著な凶作のみ年表に出す（収穫が平年の8割未満）
                World.LogAdd($"{World.Year} {World.SelfPower?.Name}が凶作に見舞われた（収穫{World.HarvestFactor:P0}）");
            // 作物病虫害（年次・専用RNG＝主系列を乱さず較正中立）：病害に弱い作物に稀に胴枯れ。数年で回復。栽培州で局地飢饉（ジャガイモ胴枯病/葡萄フィロキセラ）。
            // 天候(HarvestFactor)とは独立に作物固有でロール＝両者の悪い目が重なれば栽培州の産出はほぼ0（積算）。合成食料(id 0)は IsDiseaseProneCrop=false で非対象＝較正中立。
            var cropHealth = World.CropHealth;
            var hc = World.Coef;
            for (int g = 0; g < cropHealth.Length; g++)
            {
                if (!Catalog.IsDiseaseProneCrop(g)) continue;
                cropHealth[g] = PoliticalQuantities.Toward(cropHealth[g], 1.0, hc.CropDiseaseRecovery);   // 前年までの病害が数年かけて癒える（→1.0）
                if (cropHealth[g] > 0.9 && HarvestRng.NextDouble() < hc.CropDiseaseChance)
                {
                    cropHealth[g] = hc.CropDiseaseSeverity * (0.5 + HarvestRng.NextDouble());   // 新規の胴枯れ＝0.15〜0.45へ激減（重さに揺らぎ）
                    World.LogAdd($"{World.Year} {World.SelfPower?.Name}で{Catalog.Goods[g].Name}が病虫害に見舞われた（収穫{cropHealth[g]:P0}）");
                }
            }
            // 年に4つ引いていた主系列の乱数（枯渇2・発見2）も消えた＝以後の乱数の列がずれる（挙動の変更＝10種で測った）。〔履歴 Simulation.cs#193〕
            if (World.EpidemicActiveYears > 0) World.EpidemicActiveYears--;   // 感染窓の年次減衰
            double epiRoll = EpidemicRng.NextDouble(); double epiPick = EpidemicRng.NextDouble();   // epiRoll〔履歴 Simulation.cs#194〕
            if (epiRoll < World.EpidemicOnsetProbability(World.Coef))   // 発生率の式は World.EpidemicOnsetProbability の 1 本
                World.TriggerEpidemic(World.Coef, 0.5 + epiPick, pickRng: EpidemicRng);   // 内部発生（過密スラム）→死亡スパイク＋感染窓
        }

        // 労働時間の連続追従（倍率排除②）：実労働時間 WorkHoursValue は法定上限（WorkHours enum のアンカー）へ毎月収束する。〔履歴 Simulation.cs#195〕
        double enforce = Politics.StateReach(World, World.Coef);
        double hoursTarget = World.WorkHours.Hours();
        World.WorkHoursValue = PoliticalQuantities.Toward(World.WorkHoursValue, hoursTarget, World.Coef.WomenRightsFollowSpeed * (hoursTarget < World.WorkHoursValue ? enforce : 1.0));
        // 法の浸透の速さ WomenRightsFollowSpeed（0.15）は労働時間・賃金保持・児童労働の 3 本の法で共有する（3 本とも同じ形・同じ月次）。
        // 賃金保持・児童労働合法度の連続追従（倍率排除④）：法は離散だが社会への浸透は連続（女性の賃金保持の確立／児童労働禁止の取締り）。
        World.WomenWageRetention = PoliticalQuantities.Toward(World.WomenWageRetention, World.WomenPropertyLaw ? 1.0 : 0.0, World.Coef.WomenRightsFollowSpeed * enforce);
        // 第6段6-7：児童労働の再合法化（禁止→合法）は政治軸を持たない連続追従のため、ここで不可逆を判定＝戦時・断裂の窓のみ可（英1915軍需品法型）。
        double childTarget = World.ChildLaborAllowed ? 1.0 : 0.0;   // childTarget〔履歴 Simulation.cs#196〕
        bool childRegress = childTarget > World.ChildLaborLegal + 1e-9;
        // 交戦判定は国スコープ（州非依存）。毎州の再評価とラムダ・クロージャの確保を避け、ループ前に一度だけ算出して児童労働の不可逆判定・軍需調達・Military.Step で共用する。
        bool atWarNation = World.Diplomacy is { } dipN && World.SelfPower is { } spN && dipN.Wars.Any(war => war.A == spN || war.B == spN);
        if (!(World.Coef.IrreversibleLaws > 0 && childRegress && !atWarNation && World.RuptureWindowQuarters <= 0))
            World.ChildLaborLegal = PoliticalQuantities.Toward(World.ChildLaborLegal, childTarget, World.Coef.WomenRightsFollowSpeed * (childRegress ? 1.0 : enforce));
        double nationPop = World.TotalPopulation;                        // 行政予算の地域按分に使う
        bool navalYard = false;   // navalYard〔履歴 Simulation.cs#197〕
        foreach (var pr in World.Regions) if (pr.IsPort) { navalYard = true; break; }
        // 州非依存の国スカラー（州ループ前に1回だけ算出＝毎州の再計算を避ける。いずれも州ループ内で書き換わらない入力のみ読む）。
        double natSoldiers = World.SoldierSizeK(); double warEngageNat = atWarNation && World.Diplomacy is { } dgE && World.SelfPower is { } spE ? dgE.Engagement(spE, World.Coef) : 0;   // natSoldiers〔履歴 Simulation.cs#198〕
        // navalSpendNat〔履歴 Simulation.cs#199〕
        double navalSpendNat = World.NavalSpendShare.Value;
        double monopolyRentFactorNat = World.Antitrust switch { AntitrustLaw.CartelBan => World.Coef.CartelBanRent, AntitrustLaw.Trustbust => World.Coef.TrustbustRent, _ => 1.0 };
        int nationalizedMaskNat = World.NationalizedMask();
        World.LastSoldierPayPerMonth = new MoneyPerMonth(0);   // 兵士給与の月次請求書（形が先・金が後 段0）＝州ループで積む
        // ・給与（規定）＝州の兵数×徴募の母数の働き盛り1人あたりの賃金×SoldierPayMult（Military.RecruitWage＝市場で競り上がらない）。〔履歴 Simulation.cs#200〕
        double milEnvelope = Military.EnvelopeOf(World, World.Coef);
        bool keepPay = World.ConscriptionLaw >= ConscriptionLaw.Selective;
        var soldierBasePay = new double[World.Regions.Count];   // 州の給与の満額（請求の単価×兵数×SoldierPayMult・r と充足を掛ける前）＝徴兵制は手当・志願制は規定の給与
        var soldierRegularPay = new double[World.Regions.Count];   // soldierRegularPay〔履歴 Simulation.cs#201〕
        double payFull = 0, regularFull = 0;
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            if (r.SoldierPopIndex < 0) continue;
            double size = r.Pops[r.SoldierPopIndex].Size;
            soldierRegularPay[ri] = size * Military.RecruitWage(r, World.Coef, last: false) * World.Coef.SoldierPayMult;
            soldierBasePay[ri] = keepPay ? size * Military.RecruitWage(r, World.Coef, last: false, keep: true) * World.Coef.SoldierPayMult : soldierRegularPay[ri];
            payFull += soldierBasePay[ri];
            regularFull += soldierRegularPay[ri];
        }
        double soldierPayFill = payFull > 1e-9 ? milEnvelope / payFull : 1.0;
        if (!(soldierPayFill >= 0)) soldierPayFill = 0; else if (soldierPayFill > 1) soldierPayFill = 1;   // 否定形＝NaN は払わない側
        double equipRoom = Math.Max(0, milEnvelope - payFull * soldierPayFill);   // 給与の後に残る器＝装備の上限（必要額で切る前＝恒常性の信号の分子）
        if (!(equipRoom >= 0)) equipRoom = 0;
        double equipClaimNat = 0;   // 州ループで立った装備の請求（首都・r を掛ける前）＝下の質の残り
        // 州の並びに依らない（EconomyStep の Run は州ごとに順に走り、前の州の決済で国の艦の在庫が増える＝Run の中で FleetOrder を引くと後の州の発注が減る）。〔履歴 Simulation.cs#202〕
        foreach (var pr in World.Regions) pr.NationHasPort = navalYard;
        var navShare = new double[World.Regions.Count]; var navBudShare = new double[World.Regions.Count];
        // navQ〔履歴 Simulation.cs#203〕
        double[]? navQ = EconomyStep.NavalPortSplit(World, World.Coef, navShare, navBudShare, out double navNeedNat);
        World.Cost[CostLedger.NavalOrderScans] += 3L * World.Regions.Count;   // 計算量の台帳（診断）：NavalPortSplit の2周（取扱高の和・割合）＋下の予算の配りの州ループ1周
        int navIdx = -1;
        foreach (var r in World.Regions)
            {
                navIdx++;
                // 軍需調達：兵数（natSoldiers）×戦時倍率（warMult）に比例（治安維持力は掛からない）。財源は国庫の一部。
                // ※Part2で軍＝兵士POPに刷新したが調達は旧・治安維持力(=警察)基準のままだった。軍隊を主軸に紐づけ直す＝弾薬→火薬→鉛→鉛鉱の連鎖が平時も需要を持ち死蔵を防ぐ。
                bool atWar = atWarNation;
                double warMult = atWar ? 1.0 + World.Coef.WarArmsDemandPerContact * warEngageNat : 1.0;   // warMult〔履歴 Simulation.cs#205〕
                // 軍隊規模(兵数)×戦時倍率に比例。〔履歴 Simulation.cs#206〕
                {
                    double share = r.Id == 0 ? 1.0 : 0.0;   // 軍需調達は首都州のみ（駐屯州調達の器 GarrisonProcurement は (1132) で消した）
                    r.MilitaryScale = share > 1e-12 ? World.Coef.MilitaryUpkeepScale * natSoldiers * warMult * share : 0;
                    // 耐久品の目標在庫は平時基準（warMult を掛けない）＝機関銃は戦争が始まっても9倍要らない。〔履歴 Simulation.cs#207〕
                    r.MilitaryScalePeace = share > 1e-12 ? World.Coef.MilitaryUpkeepScale * natSoldiers * share : 0;
                    r.MilitaryWarMultLast = warMult;
                }
                r.NationHasPort = navalYard;   // 国に港が1つでもあれば艦を発注できる   // 軍需調達は首都州のみ（多州で全国軍を二重調達しない）〔履歴 Simulation.cs#208〕
                // 粗歳入から確実に（裁量余剰でなく）＝**軍国（余剰0でも）が装備できる**＝武器を買えた軍だけが治安維持力＋対外武力を持つ（買えねば即応度0→治安維持力0→革命）。〔履歴 Simulation.cs#209〕
                double seaShare = navalSpendNat;
                if (!(seaShare >= 0)) seaShare = 0; else if (seaShare > 1) seaShare = 1;   // 否定形＝NaN は陸へ
                double milCap = equipRoom * (1.0 - seaShare);   // 陸の装備の請求（r を掛ける前）。支出反射（OpulenceSpendRatePerYear>0で国庫超過分も使える歳入へ＝戦争準備金）は器の側で効く
                r.MilitaryBudgetCeiling = (r.Id == 0 ? milCap : 0);   // MilitaryBudgetCeiling〔履歴 Simulation.cs#210〕
                // ★S3a：必要額を超えて割り当てない（上限の意味どおり）。〔履歴 Simulation.cs#211〕
                if (r.Id == 0)
                {
                    milCap = Math.Min(milCap, EconomyStep.MilitaryProcurementNeed(r, World.Coef));
                }
                r.MilitaryBudget = (r.Id == 0 ? milCap : 0);   // 予算も首都州だけに配分（r は下で全ての予算に同じく掛ける）
                {
                    // 割合 0 の州は予算も発注も無し（null）＝EconomyStep は艦を買わない。〔履歴 Simulation.cs#212〕
                    double navalNat = equipRoom * seaShare;
                    navalNat = Math.Min(navalNat, navNeedNat);
                    double sh = navShare[navIdx];
                    r.NavalBudget = EconomyStep.NavalPortBudget(navalNat, sh, navBudShare[navIdx]);   // 追記 1：予算の割合は発注額の比（各州が自分の値段で買う額）・発注は取扱高の比
                    double[]? nq = null;
                    if (sh > 0 && navQ is not null)
                    {
                        nq = new double[navQ.Length];
                        for (int k = 0; k < nq.Length; k++) nq[k] = navQ[k] * sh;
                    }
                    r.NavalOrder = nq;
                }
                equipClaimNat += r.MilitaryBudget + r.NavalBudget;   // NavalBudget〔履歴 Simulation.cs#213〕
                r.SocialSecurity = World.SocialSecurity;                 // 公共医療＝医薬品消費に使う（EconomyStep）
                r.InsuranceCoverage = World.Coef.InsuranceCoverageMax.Value * World.SocialSecurity;   // InsuranceCoverage〔履歴 Simulation.cs#214〕
                r.StateMonopolyMargins = World.StateMonopolyMargins;       // StateMonopolyMargins〔履歴 Simulation.cs#215〕
                // EconomyStep が monRent に乗算）〔履歴 Simulation.cs#217〕
                r.MonopolyRentFactor = monopolyRentFactorNat;
                // 漏れ率は州の行政充足率が決める＝★清が禁止後に国産阿片を激増させた史実（1860年代に国産が輸入を上回る）が 「弱い行政 × 高い価格」から出る。〔履歴 Simulation.cs#218〕
                r.ContrabandProductionFactor = World.Coef.OpiumAddictionGrowth > 0 && World.OpiumPossession == ReformLevel.Full
                    ? Contraband.LeakFraction(r.AdminFill, World.Coef)
                    : 1.0;
                // EconomyStep が該当財の利潤を国庫へ）〔履歴 Simulation.cs#219〕
                r.NationalizedMask = nationalizedMaskNat;
                // 倍率排除④：女性参加・児童労働を連続状態（賃金保持・合法度）から複製。実効値は LaborSupply が家計困窮・就学を加味して創発。
                r.WomenWageRetention = World.WomenWageRetention;          // 賃金保持（連続）。女性参加・女性識字の駆動
                r.ChildLaborLegal = World.ChildLaborLegal;                // 児童労働の合法度（連続）。子供の労働/就学の時間配分を律する
                r.FemaleParticipation = World.Coef.FemalePartBase + (World.Coef.FemalePartProperty - World.Coef.FemalePartBase) * World.WomenWageRetention;   // 構造的参加率（need=0 の代表値・診断/UI用。0.4〜0.7 を賃金保持で連続補間＝2値の崖を解消）
                r.HarvestFactor = World.HarvestFactor;                   // 今年の豊作/不作（主食RGO産出と自給に乗算）
                r.CropHealth = World.CropHealth;                         // 作物別の健全度（病虫害）。栽培州の産出に乗る＝作物固有の不作（天候と積算）
                r.WeatherBandFactor = World.WeatherBandFactor;           // 緯度帯（気候帯）の天候。HarvestFactorと積算＝同じ気候帯の国が一緒に不作（欧州一帯の凶作）
                r.Discovered = World.Discovered;                         // ★発見の表の解禁（段2A）＝国の配列への参照（写しではない＝判定の四半期から同じ値を読む）
                // 役務（サービス財）の月次設定：教育の担い手と生産性を教育法で切替＋義務教育の財源率＋行政・兵力の予算。〔履歴 Simulation.cs#220〕
                r.CompulsoryEduFunding = (World.EducationLaw == EducationLaw.Compulsory ? World.EducationSpending
                    : World.EducationLaw == EducationLaw.Public || World.EducationLaw == EducationLaw.Religious ? World.Coef.PublicEduFundingShare.Value * World.EducationSpending : 0)   // PublicEduFundingShare〔履歴 Simulation.cs#221〕
                    * Politics.RuleReach(r);   // RuleReach〔履歴 Simulation.cs#222〕
                r.EducationSpending = World.EducationSpending;   // EducationSpending〔履歴 Simulation.cs#223〕
                if (r.EducationProducerIndex >= 0)
                {
                    var ep = r.Producers[r.EducationProducerIndex];
                    int pubIdx = r.EducationProducerIndex + 1;
                    if (World.Coef.EduProducersCoexist > 0 && pubIdx < r.Producers.Count && r.Producers[pubIdx].GoodId == Catalog.EducationId)
                    {
                        // 教育法は「非活性側の Scale=0」のゲートだけ（技術ゲート0の操業規模0の役務版＝EconomyStep が教育財に限り Scale=0 を fraction=0 と読む）。〔履歴 Simulation.cs#224〕
                        var pub = r.Producers[pubIdx];
                        bool parishOn = World.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0;
                        bool publicOn = World.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory && r.TeacherPopIndex >= 0;
                        if (!parishOn) ep.Scale = 0;
                        if (!publicOn) pub.Scale = 0;
                    }
                    else if (World.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0)
                    {
                        // 宗教学校：聖職者が教える。少人数しか教えられない（供給希少）＝下層に行き渡らず識字が低位で頭打ち。紙を消費（写本・教義書）。
                        ep.LaborPopIndex = r.ClergyPopIndex; ep.OwnerPopIndex = r.ClergyPopIndex;
                        ep.Productivity = World.Coef.ClergyEduProductivity;
                        if (ep.Inputs.Count > 0) ep.Inputs[0] = new Input(Catalog.PaperId, 0.05);   // 紙（教材）
                    }
                    else if (World.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory && r.TeacherPopIndex >= 0)
                    {
                        // 公教育・義務教育：教師が教える。大量に教えられる＝大衆識字が可能（聖職者は収入源を失い没落）。**書籍**を消費（近代の教科書＝紙の一段上）。
                        ep.LaborPopIndex = r.TeacherPopIndex; ep.OwnerPopIndex = r.TeacherPopIndex;
                        ep.Productivity = World.Coef.TeacherEduProductivity;
                        if (ep.Inputs.Count > 0) ep.Inputs[0] = new Input(Catalog.BookId, 0.05);   // 書籍（教科書）＝紙の上位投入
                    }
                    else ep.Productivity = 0;   // 学校なし＝教育の供給なし（識字は停滞）
                }
                // funding修正：上下水道・電化インフラは**税収フロー(FiscalCapacityPerMonth)から支弁**（国庫ストックは基礎義務に食われ常時0で餓死していた）。〔履歴 Simulation.cs#226〕
                r.AdminBudget = r.BureaucratPopIndex >= 0 && nationPop > 1e-9
                    ? World.AdminSpending * World.Coef.AdminBudgetFrac.Value * World.SpendableRevenue() * (r.SumPopulation() / nationPop) * Fiscal.AdminDistanceMul(World, r) : 0;   // 請求（r を掛ける前）
                r.InfraBudget = nationPop > 1e-9
                    ? (World.AdminSpending * World.Coef.InfraBudgetScale * World.FiscalCapacityPerMonth * (r.SumPopulation() / nationPop)).Value : 0;
                r.ElecInfraBudget = nationPop > 1e-9
                    ? (World.AdminSpending * World.Coef.ElecInfraBudgetScale * World.FiscalCapacityPerMonth * (r.SumPopulation() / nationPop)).Value : 0;
                if (Instruments.On) Instruments.AdminBase(World, r, nationPop);   // 計器（読むだけ）：行政の予算の基数（SpendableRevenue・官僚の居る州の人口の比と距離の割増）
                // 注〔履歴 Simulation.cs#227〕
            }
        // 幹部の給与（質）の物差しは規定の単価。〔履歴 Simulation.cs#228〕
        double trainNeed = regularFull * World.Coef.MilitaryCadrePayRatio;
        double trainClaim = Math.Min(Math.Max(0, equipRoom - equipClaimNat), trainNeed);
        double trainShare = regularFull > 1e-9 ? trainClaim / regularFull : 0;   // 規定の給与の満額1あたりの幹部の給与
        if (!(trainShare >= 0)) trainShare = 0;
        World.MilitaryTrainingFill = trainNeed > 1e-9 && trainClaim >= 0 ? trainClaim / trainNeed : 0;
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            if (r.SoldierPopIndex < 0) continue;
            r.SoldierPayBudget = soldierBasePay[ri] * soldierPayFill + soldierRegularPay[ri] * trainShare;   // 請求（r を掛ける前）。〔履歴 Simulation.cs#229〕
            World.LastSoldierPayPerMonth += new MoneyPerMonth(r.SoldierPayBudget);   // 請求書（形が先・金が後 段0）
        }
        World.LastMilitaryClaimPerMonth = new MoneyPerMonth(equipClaimNat);   // 給与と質（幹部の給与）は兵士の給与の請求（LastSoldierPayPerMonth）の側＝旧と同じく余剰からは引かない〔履歴 Simulation.cs#230〕
        // 払える額（Q1）＝max(0, 国庫)＋当月の歳入の見込み（LastRevenue）。〔履歴 Simulation.cs#231〕
        if (Instruments.On) Instruments.Begin(World);   // 計器（読むだけ）：今月の請求の写しを始める
        double claims = (World.LastInterestPerMonth + World.LastWelfareCostPerMonth + World.LastPensionCostPerMonth + World.LastColonialUpkeepPerMonth).Value;   // claims〔履歴 Simulation.cs#232〕
        if (Instruments.On) Instruments.ClaimsBase(World);
        bool payQuarter = World.IsQuarterEndMonth;
        if (payQuarter) claims += World.LastTributeOutPerQuarter + World.RecruitBudget;   // 四半期の請求（貢納・招聘の支度金）
        if (Instruments.On && payQuarter) Instruments.ClaimsQuarter(World);
        double portThr = payQuarter ? World.PortThroughputTotal() : 0;   // portThr〔履歴 Simulation.cs#233〕
        // 売買方針の代金とダンピング補助（清算ごと・清算の末尾の World.Transfer＝旧 InternationalMarket.PaySubsidy）＝直近の清算の越境の輸入額・輸出額で見込む（同国の州どうしの買い付けの分は見込みの外）
        //   ★段3-1（A7）：清算は金建て＝自国通貨へ為替で換算して見込む（清算と同じ守り＝為替≦1e-9 は1）。
        double erClaim = World.FxRate;
        double tradeClaim = World.Tariff < 0 ? World.LastExportValue / erClaim * -World.Tariff : 0;
        if (World.ImportsByGood is { } payImp)
            for (int g = 0; g < payImp.Length; g++) if (World.TradeBiasOf(g) > 0) tradeClaim += World.Coef.TradeBiasStrength * payImp[g] / erClaim;
        if (World.Coef.TradeCheapSellSubsidy > 0 && World.ExportsByGood is { } payExp)   // TradeCheapSellSubsidy〔履歴 Simulation.cs#234〕
            for (int g = 0; g < payExp.Length; g++) if (World.TradeBiasOf(g) < 0) tradeClaim += World.Coef.TradeBiasStrength * payExp[g] / erClaim;
        claims += tradeClaim / World.Coef.MonthsPerClearing;
        if (Instruments.On) Instruments.Claim(World, Instruments.Item.TradePolicy, tradeClaim / World.Coef.MonthsPerClearing);
        // 先月の区分つき StateSpent で見込む行（義務教育・健康保険・工場の材料・民営の補助）は先月の r で払った額＝請求へ戻すには先月の r で割る
        //   （この時点の FiscalFundingRatio＝先月の r。先月 0 なら先月は払っていない＝見込みも 0）。
        double lastRate = World.FiscalFundingRatio.Value;
        double regrow = lastRate > 0 ? 1.0 / lastRate : 0.0;
        foreach (var r in World.Regions)
            {
                if (payQuarter) claims += World.PortStateRequest(r, portThr);
                claims += r.MilitaryBudget + r.NavalBudget + r.AdminBudget + r.InfraBudget + r.ElecInfraBudget + r.SoldierPayBudget   // SoldierPayBudget〔履歴 Simulation.cs#235〕
                        + r.HigherEduFunding
                        + regrow * (Math.Max(0, r.StateSpent[(int)TreasuryCat.Education]) + Math.Max(0, r.StateSpent[(int)TreasuryCat.Welfare])       // 先月の義務教育・健康保険
                                  + Math.Max(0, r.StateSpent[(int)TreasuryCat.FactoryBuild]) + Math.Max(0, r.StateSpent[(int)TreasuryCat.Subsidy]));   // 先月の工場の材料・民営の補助
                if (Instruments.On) Instruments.ClaimRegion(World, r, regrow, payQuarter, portThr);
                // 一覧 Region.StateSites＝工事中か BuiltScale>1e-9 の生産者を州の生産者の順に。〔履歴 Simulation.cs#236〕
                {
                    r.EnsureSites();
                    var sites = r.StateSites!;
                    World.Cost[CostLedger.ClaimProducers] += sites.Length;   // 計算量の台帳（診断）：請求で見る生産者＝国営持分の請求の一覧
                    foreach (var pr in sites) AddStateClaim(pr, r, World, ref claims);
                }
            }
        double payable = Math.Max(0, World.Treasury.Value) + World.LastRevenuePerMonth.Value;
        // ＞0＝ r＝1 の月の余剰（減債の原資＝Fiscal）・＜0＝払えない請求（＋借越）。負の国庫（記録されない当座借越）は余剰から先に埋める
        //   ＝借越のまま国債を返さない・埋められない借越は赤字（r は Q1 のとおり max(0,国庫) で決める＝国庫0の国の率0→行政0→税収0の行き詰まりを作らない）。
        double spare = payable - claims + Math.Min(0, World.Treasury.Value);
        // ⇒ 切り下げの余地を月初に払える額へ入れて r を決め、取った金はその月の支払いに使う。〔履歴 Simulation.cs#237〕
        double devalued = 0;   // devalued〔履歴 Simulation.cs#238〕
        double unopenedRoom = 0;   // (1250) 兌換の遅れでまだ開いていない切り下げの余地（deficit−余地）＝Fiscal.Step の不履行の累計が読む（旗 ArrearsBeyondOpenedRoom）
        if (World.FiscalEnabled)
        {
            if (Instruments.On) Instruments.PreBorrow(World, payable, spare);
            double borrowed = Fiscal.Borrow(World, World.Coef, spare);
            payable += borrowed; spare += borrowed;   // 借りた金はこの月の払える額＝r に入る（切り下げと同じ扱い）
            if (Instruments.On) Instruments.PostBorrow(World, borrowed);
            double deficit = spare < 0 ? -spare : 0;   // 否定形＝NaN は0（借りた後に残った払えない額＝借りられない月は全額）
            // 兌換可能性で切り下げ可能量を連続スケール（倍率排除②）：完全兌換(Conv=1)＝切り下げ封印（金の規律）／不換(Conv=0)＝赤字全額を切り下げ可。部分兌換は中間。
            double devalueRoom = deficit * (1.0 - World.Convertibility);
            // 封印されるのは**銀行券の発行**の側＝正貨比率（BaseMoney/BroadMoney）ぶんは   兌換中でも改鋳の窓が開く：room = deficit×(1 − Conv×(1−正貨比率))。〔履歴 Simulation.cs#239〕
            if (World.MonetaryLaw == MonetaryStandardLaw.Silver)
            {
                double specie = World.BroadMoney.Value > 1e-9 ? World.BaseMoney / World.BroadMoney : 1.0;
                if (!(specie >= 0)) specie = 0; else if (specie > 1) specie = 1;   // NaNは0へ倒す（否定形＝ClampはNaNを消さない）
                devalueRoom = deficit * (1.0 - World.Convertibility * (1.0 - specie));
            }
            unopenedRoom = deficit - devalueRoom;
            if (devalueRoom > 1e-9)
            {
                double totalSav = 0; foreach (var (_, dp) in World.AllPopsWithRegion()) totalSav += Math.Max(0, dp.Savings.Value);
                // DevaluationCapFracPerMonth は**月あたり**の上限（月次決算＝月に1回）。〔履歴 Simulation.cs#240〕
                double devalueCap = totalSav * World.Coef.DevaluationCapFracPerMonth.Value;   // 貯蓄の月の上限（旧の式の第2項と同じ式）
                double devalue = Fiscal.DevaluationAllowed(devalueRoom, devalueCap);   // 切りを超える余地は余地そのもの・それ以外は Math.Min(余地, 貯蓄×上限)〔履歴 Simulation.cs#241〕
                if (Instruments.On) Instruments.DevCap(World, devalueRoom, devalueCap, devalue);   // 計器（読むだけ）：旧の切りに当たった月
                if (devalue > 1e-9)
                {
                    // 不換シニョリッジ（fiat-expansion-as-haircut-not-issue）：切り下げのうち FiatIssueFrac は pop から取らず新規発行＝マネーストック膨張(M↑)。
                    // 発行ぶんは CumulativeFiatIssued に積み Invariants の expected に算入(鋳造と同型)。M↑→P↑は FiatInflationPressure が表現＝二重取りなし。
                    double issue = devalue * World.Coef.FiatIssueFrac.Value;
                    double haircut = devalue - issue;
                    // 台帳 CumulativeFiatIssued は発行の分だけ（BaseMoney の正当な増加源＝Invariants 算入）。〔履歴 Simulation.cs#242〕
                    if (haircut > 1e-12)
                    {
                        var allPops = new List<Pop>(); foreach (var (_, dp) in World.AllPopsWithRegion()) allPops.Add(dp);
                        haircut = World.Transfer((World, allPops), (World, null, null), haircut, TreasuryCat.Devalue, fromHeld: totalSav);   // ★(351) 払い手の保有の合計は上の totalSav と同じ順の同じ和（間に何も書かない）
                    }
                    else haircut = 0;
                    issue = World.Transfer((null, null), (World, null, null), issue, TreasuryCat.Devalue, outside: World.Outside.Fiat);
                    devalue = haircut + issue;
                    World.CumulativeDevaluation += devalue; devalued = devalue;
                    double broadAfter = World.BroadMoney.Value;   // 切り下げの後の広義の貨幣（haircut で貯蓄が減った後）
                    if (Instruments.On) Instruments.PfixMonth(World, devalue, broadAfter);   // Instruments〔履歴 Simulation.cs#243〕
                    double fiatPressure = Fiscal.FiatPressureOfLedger(devalue, totalSav, issue);   // fiatPressure〔履歴 Simulation.cs#244〕
                    if (Instruments.On) Instruments.FsxPressure(World, devalue, totalSav, issue, broadAfter, fiatPressure);   // fiatPressure〔履歴 Simulation.cs#245〕
                    World.FiatInflationPressure += fiatPressure;   // 紙幣増発＝インフレ圧（名目GDP↑→実質債務減）。〔履歴 Simulation.cs#246〕
                    payable += devalue; spare += devalue;   // 取った金はこの月の払える額＝r に入る（請求を切った上で別に取らない）
                }
            }
            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1;
        }
        double payRate = claims > 0 ? payable / claims : 1.0;
        if (!(payRate >= 0)) payRate = 0; else if (payRate > 1) payRate = 1;   // 否定形＝NaN は払わない側
        World.FiscalFundingRatio = new Share(payRate);
        if (Instruments.On) Instruments.Rate(World, claims, payable, payRate, devalued);
        foreach (var r in World.Regions)
            {
                // 全ての予算と予約に同じ r（予算は量の上限＝買えた分だけ払う＝払えない分は量が減る）
                r.MilitaryBudget *= payRate; r.MilitaryBudgetCeiling *= payRate; r.NavalBudget *= payRate;   // payRate〔履歴 Simulation.cs#247〕
                r.AdminBudget *= payRate; r.InfraBudget *= payRate; r.ElecInfraBudget *= payRate; r.SoldierPayBudget *= payRate;
                // 兵士は労働を市場で売らない＝EconomyStep の売れ残りの書き手は兵士に書かない（この行だけ）。〔履歴 Simulation.cs#248〕
                if (r.SoldierPopIndex >= 0 && r.SoldierPopIndex < r.Pops.Count && r.Pops[r.SoldierPopIndex].IsSoldier)
                    r.Pops[r.SoldierPopIndex].UnemploymentRate = new Share(1.0 - payRate * soldierPayFill);   // 未払い＝国家の支払い率×器の給与の充足の不足（幹部の給与は規定の給与の外＝数えない）
                r.CompulsoryEduFunding *= payRate; r.InsuranceCoverage *= payRate;
                r.HigherEduFunding *= payRate;
                double techProd = World.TechProductivity(r);   // techProd〔履歴 Simulation.cs#249〕
                RepayHouseholdDebt(r);   // ★出口：返済規律（消費の前・Savings 自体を削る。下の定義を見よ）
                if (Instruments.On) Instruments.PreEconomy(World, r);   // Instruments〔履歴 Simulation.cs#250〕
                EconomyStep.Run(r, World.Coef, techProd, World.Bank, World);   // §9-A/B/C（active 地域ごと）
                // 閾値未満の通常循環では工場を壊さない。〔履歴 Simulation.cs#251〕
                if (World.Coef.WarLivestockRequisition > 0 && r.LastWarIntensity > 0 && !World.WarmupFrozen)
                    foreach (var lpr in r.Producers)
                        if (lpr.IsRgo && lpr.GoodId == Catalog.LivestockId)
                            lpr.Scale = Math.Max(0, lpr.Scale * (1.0 - World.Coef.WarLivestockRequisition * Math.Min(1.0, r.LastWarIntensity)));
                if (World.Coef.DepressionScrapRate > 0 && r.CapitalEnabled && !World.WarmupFrozen)
                {
                    double scrapKeep = 1.0 - World.Coef.DepressionScrapRate * Math.Max(0, World.GdpDrawdown.Value - World.Coef.DepressionScrapThreshold.Value);
                    r.Industrialization *= scrapKeep;
                    r.OldVintageCapital *= scrapKeep;   // StrandedVintageも比例縮小（座礁≤旧世代の不変条件を保つ・次月に再計算）〔履歴 Simulation.cs#252〕
                    r.StrandedVintage *= scrapKeep;
                    if (scrapKeep < 1)
                        foreach (var fp in r.Producers)
                            if (!fp.IsRgo && fp.BuiltScale > 1e-9 && fp.MothballFrac.Value > 1e-9)
                            {
                                double builtBefore = fp.BuiltScale;
                                fp.BuiltScale *= scrapKeep * fp.MothballFrac.Value + (1.0 - fp.MothballFrac.Value);   // 休止率0＝不変〔履歴 Simulation.cs#253〕
                                if (World.Coef.RecipeEquipment > 0 && FactoryEquipment.Eligible(fp)) FactoryEquipment.Dismantle(fp, builtBefore, World);   // 休止率0＝不変〔履歴 Simulation.cs#254〕
                            }
                }
                // SettleStateSpent〔履歴 Simulation.cs#255〕
                World.SettleStateSpent(r);   // SettleStateSpent〔履歴 Simulation.cs#256〕
                // 注〔履歴 Simulation.cs#257〕
            }
        ProbeCheck("月次経済");
        Transport.Step(World, World.Coef);                               // §5.1拡張 輸送容量（鉄道/船を積む→Tradeで交易量を律速）
        ProbeCheck("月次輸送");
        // ⚠先に検査内容を実経路へ移してから消した（`ConservationTests` が保存量Dと輸出利潤経路の生存を見る）。〔履歴 Simulation.cs#258〕
        ProbeCheck("月次交易");
        Colonial.Step(World, World.Coef);                                // §6.1 植民地（インフラ整備→資源産出＋捕囚市場。交易流入で保存）
        ProbeCheck("月次植民地");
        StepMineralDeposits();   // ゲート0＝即return＝恒等〔履歴 Simulation.cs#259〕
        MintGold();                                                      // 鋳造（金鉱→正貨）：金産出の一部が基礎貨幣に（ゴールドラッシュ＝マネー膨張）。〔履歴 Simulation.cs#260〕
        ProbeCheck("月次鋳造");
        Fiscal.Step(World, World.Coef, spare, devalued, unopenedRoom);                 // §9-F/§6.2（全 pop に課税）。spare＝月初の払える額（借入と切り下げの後）−請求（＞0＝減債の原資）・devalued＝この月の切り下げ（国債の金利の信認の喪失）
        ProbeCheck("月次財政");
        // ★財政の**後**に置く＝払い下げ代金と国庫の残高が同じ月の中で前後しない （売却は資本家→国庫の移転＝Fiscal の課税ベースを後から動かさない）。〔履歴 Simulation.cs#261〕
        World.StepStateTargets();
        ProbeCheck("月次国営工場");
        Crime.Step(World, World.Coef);                                   // 犯罪と抑止：秩序（治安維持力の人口カバレッジ）が崩れると窃盗＝闇の再分配（純移転）
        ProbeCheck("月次犯罪");
        // 兵士POP軍制（Part 2）：軍事費で徴募し給与を払う（国庫→兵士で保存）。戦時は徴兵。財政の後・外交ゲージ集計の前。
        if (Instruments.On) Instruments.WarMonth(World, atWarNation);   // 計器（読むだけ）：交戦と内戦の月を数える（軍の更新の直前）
        Military.Step(World, World.Coef, atWarNation);   // 交戦判定はループ前に算出済み（atWarNation）を共用
        if (World.IsQuarterEndMonth) Military.StepSkill(World, World.Coef, atWarNation);   // IsQuarterEndMonth〔履歴 Simulation.cs#262〕
        ProbeCheck("月次軍事");
        if (World.StepsDiplomacy)
            World.Diplomacy?.Step(World.Coef, Rng, (int)(World.Tick / Calendar.DaysPerMonth));  // §6.1 外交・戦争（月次・opt-in。多国では共有外交を MultiNationSim が進める）
        // rank3：農奴解放の梯子（Colonial.cs入植と同じ物差し）＝SerfMobilityGate>0のとき農村→都市流を縛る（ループ不変＝外で1回）
        double serfFree = 1.0 - Privileges.SeigneurialDegree(World);
        foreach (var r in World.Regions)
            if (!(World.WarmupFrozen && World.BakeRun?.StopPopDecision == true))   // StopPopDecision〔履歴 Simulation.cs#263〕
            {
                if (Instruments.On && !World.WarmupFrozen) Instruments.UrxPd(World, r, serfFree);   // WarmupFrozen〔履歴 Simulation.cs#264〕
                if (Instruments.On) Instruments.RdhPd(World, r, serfFree);   // Instruments〔履歴 Simulation.cs#265〕
                PopDecision.Run(r, World.Coef, serfFree);           // §9-D（地域内の階級遷移）
            }
        ProbeCheck("月次政治");
        if (!World.WarmupFrozen) Territory.Step(World, World.Coef);      // §5.2/§A-8（解錠＋地域間移民）。助走中は止める＝州の解錠も国内移住も「時間が経って起きる」こと
        ProbeCheck("月次領土移民");
        TechEducation.GrowLiteracy(World, World.Coef);                   // §A-1（助走中は識字の伸びだけ止まり Elite の導出は通る＝分岐は GrowLiteracy 内）

        // 暦を止めたまま回すと「10年ぶんの人口成長が時間経過なしに起きる」＝1783年の開始盤面でなくなる。〔履歴 Simulation.cs#266〕
        if (!World.WarmupFrozen)
            DemographicStep();           // 人口動態：出生（医療/都市化）−死亡（労働時間）＝動的人口・戦死からの回復も兼ねる（人口転換）。飢えの超過死亡（平常の死亡率の上乗せ）と州の食料充足の平滑も内包
        ProbeCheck("月次人口");
        BackstopNegativeSavings();   // 損失吸収の脆さの会計：貯蓄が負になった pop は不足を信用で埋める（有限責任・保存則厳守）
        ProbeCheck("月次バックストップ");
        CheckInvariants();
        if (Instruments.On) Instruments.MonthClose(World);   // 計器（読むだけ）：月の収支と階級の所得・支出
    }

    /// <summary>
    /// 人口動態（人口転換・§A-3 拡張）：扶養力(carrying capacity)へロジスティック収束＝出生−死亡の純。
    /// 扶養力＝規定値×(1＋医療−都市化)×労働時間。医療＝min(社会保障, 識字/gate)で教育律速（医者が要る）。
    /// 戦死で減った人口もこのステップで扶養力へ回復。規模按分で全 pop に適用、符号付き台帳で保存則追跡。頑健（暴走/崩壊しない）。
    /// </summary>
    // ①Phase3：出産可能女性プール（成人のうち非老年×女性比率）。〔履歴 Simulation.cs#267〕
    internal static double FertilePool(Pop p, Coefficients c) => FertilePoolOf(p.Ages, c);
    private static double FertilePoolOf(double[] ages, Coefficients c)
    {
        double f = Pop.AgeShareOf(ages, 15, 50, 1), m = Pop.AgeShareOf(ages, 15, 50, 0);
        return f + (2.0 * Math.Min(f, m) - f);   // 少数側の性が2人でカップル1組＝律速（男女どちらが不足しても出生↓）。⚠式の形は旧のまま（f+(2min−f) は 2min と最終ビットが違いうる）
    }
    // false（対照腕）は Size＝従来の粗出生（規模比例）＝厳密に従来挙動。〔履歴 Simulation.cs#268〕
    private static double BirthWeight(Pop p, Coefficients c)
    {
        double fertile = FertilePool(p, c);
        double init = p.InitialFertileShare.Value > 0 ? p.InitialFertileShare.Value : Math.Max(c.FertileShareFloor.Value, fertile);   // フロアを現実的下限へ引き上げ可能に（1e-6 だと捕捉時 fertile≈0 の pop の重みが桁違いに膨張＝出生集中バグ）
        // popごとの t=0 凍結分母 init は、初期構成が偏った pop でも捕捉時に重み=Size を保証するが、構成が育つと分子だけが伸びる穴が残る＝古い設計の一覧 H25（9-2 と一緒に直す）。〔履歴 Simulation.cs#269〕
        return p.Size * fertile / init;
    }
    /// <summary>
    /// 平常の死はこれ×規模の比で POP へ配る＝年寄りの多い POP ほど多く死に、若い POP ほど少なく死ぬ（年齢別の死）。〔履歴 Simulation.cs#270〕
    /// </summary>
    private static double AgeMortalityIndex(Pop p, double[] mortW)
    {
        double s = 0;
        for (int k = 0; k < mortW.Length; k++) s += mortW[k] * p.Ages[k];
        return s;
    }

    internal void DemographicStep()   // internal＝番人テスト（FamineTests 飢えた植民地の州は…）が月ごとに直接呼ぶ。可視性だけの変更
    {
        double pop = World.TotalPopulation;
        if (pop < 1e-9) return;
        var c = World.Coef;
        double baseline = 0.0; foreach (var ar in World.Regions) baseline += ar.AnchorPopulation;   // 錨 0（植民地格の州だけの国）でも飛ばさず門は下の capacity〔履歴 Simulation.cs#271〕
        double litSum = 0, healthSum = 0, foodMet = 0, foodNeed = 0;
        foreach (var p in World.AllPops()) { litSum += p.Literacy * p.Size; healthSum += p.HealthFill * p.Size; foodMet += p.FoodMet; foodNeed += p.FoodNeed; }
        double healthcare = World.SocialSecurity * World.FiscalFundingRatio.Value;   // 社会保障(充足率で減衰)＝財政不足で医療→人口増が鈍る。医者が要るは下の医者の役務の門だけが持つ（思想⑨・(1222) K03 の識字の項の削除を (1227) で焼いた）
        double doctorCare = healthSum / pop;   // 医者の役務（医療財）の消費＝乳児死亡の低下。医者が居ない/買えない社会は人口が伸びにくい
        // これ>0で実医療財の消費(HealthFill)を 第3のmin項としてゲート化（1.0＝医者なし→抽象医療も0・中間＝部分ゲート＝実体側への段階移行）。〔履歴 Simulation.cs#272〕
        if (c.HealthcareRealGate > 0) healthcare = Math.Min(healthcare, doctorCare / c.HealthcareRealGate);
        // 実測（popprobe s1）＝英は識字0.64・医者役務0.53でも社会保障0のため healthcare が131年間0だった （最大の扶養力項 +1.2×healthcare が英でだけ死ぬ＝史実と逆。〔履歴 Simulation.cs#273〕
        if (c.DoctorHealthcareSubstitute > 0) healthcare = Math.Max(healthcare, c.DoctorHealthcareSubstitute * doctorCare);
        // 実食料の連結（マルサスの積極的抑制）：主食充足 FoodFill（消費ベース∈[0,1]）が1未満＝凶作/過密/高騰で主食が買えない＝飢饉。
        // 食料は年単位の備蓄でならす（月次の振れを直に死へ変換しない＝餓死は数ヶ月の欠乏で起きる）＝緩やかな移動平均で扶養力に効かせる。
        double prevFood = World.FoodFill;
        double rawFill = foodNeed > 1e-9 ? PoliticalQuantities.Clamp01(foodMet / foodNeed) : 1.0;
        // 説明API（#63）：食料充足＝前期の備蓄（1−s）×前期＋今期の需給 s×rawFill（平滑）。Explain=nullでゼロコスト。
        if (World.Explain != null)
        {
            World.Explain.Reset("FoodFill");
            World.Explain.Add("FoodFill", "前期の食料充足（穀倉の備蓄）", (1 - c.FoodFillSmoothing) * prevFood);
            World.Explain.Add("FoodFill", "今期の主食需給", c.FoodFillSmoothing * rawFill);
        }
        World.FoodFill += c.FoodFillSmoothing * (rawFill - prevFood);
        // 扶養力：医療(乳児生存)で増、工業化(雇用・食料生産性)で増、都市化(少子化)で減、過重労働で微減、主食不足で減（飢饉）＝人口転換＋マルサス。
        double avgInd = 0; int actN = 0;
        double indPopSum = 0, indPopWeight = 0;   // 人16：工業化度の人口加重（Σ Ind×rpop / Σ rpop）＝非加重 avgInd を置換
        double sanitSum = 0, sanitPop = 0;   // 衛生（上下水道+石鹸+電化）の人口加重。施設の無い世界は各 Fill 既定で sanitation=1.0=中立（較正温存）
        double arableSum = 0, ruralSum = 0;   // 人2b 空き土地の扶養力（下のlandRoom）：耕地と農村人口の全国集計。ArableLand≥1e8（未設定＝較正/最小世界）は除外＝厳密恒等
        // 下の消費ベース補正コメントが「輸入の分子算入は将来課題＝Trade記録から」と自認していた欠落を埋める（FoodAnchorNetImport適用は下）。〔履歴 Simulation.cs#274〕
        double stapleNetImport = 0;
        double stapleOutput = 0;   // 人2：全active州の主食(Staple)実産出量の合計（Σ Market[主食].LastProduced）。耕地×農業技術×豊凶を既に内包した「実食料産出量」＝扶養力の食料アンカーの分子。FoodOutputAnchorProduction=0では未使用（読むだけ・挙動非依存）
        foreach (var r in World.Regions)
        {
            avgInd += r.TotalIndustrialization; actN++;   // 扶養力の工業化項＝立っている資本の総量（新旧合計＝ヴィンテージ休眠時は恒等。〔履歴 Simulation.cs#275〕
            double rpop = r.SumPopulation();
            indPopSum += r.TotalIndustrialization * rpop; indPopWeight += rpop;
            double rsanit = r.Sanitation(c);
            sanitSum += rsanit * rpop; sanitPop += rpop;
            // 人2：この州の主食RGO産出を合計（LastProduced＝需要非クランプの実産出。regionLandFactor[耕地逓減]×農業生産性×HarvestFactor×WeatherBandFactor×CropHealth を既に内包＝「耕地×技術×豊凶の代理」）。
            foreach (var g in r.Goods) if (g.Tier == GoodTier.Staple)
            {
                var mk = r.Markets[g.Id];
                stapleOutput += mk.LastProduced;
                // PrevExport〔履歴 Simulation.cs#276〕
                stapleNetImport += mk.PrevImport - mk.PrevExport;   // B-高1：主食の純輸入（輸出国は負＝食料を送り出す分だけ自国の扶養力は下がる）
            }
            if (r.ArableLand < 1e8)   // 人2b：空き土地の集計（センチネル除外）
            {
                arableSum += r.ArableLand;
                ruralSum += r.SumRuralPopulation();   // 州の農村人口の1本（兵士を含む＝土地の逓減・機会・受け入れの上限と同じ入力・段4e H＝旧はここだけ兵士を除いた）
            }
            stapleOutput += r.LastSubsistenceOutput;   // LastSubsistenceOutput〔履歴 Simulation.cs#277〕
        }
        if (actN > 0) avgInd /= actN;
        // 人16（工業化度の人口加重）：非加重avgInd（空の辺境州の追加で出生率/扶養力が変わる）を人口加重平均へブレンド。〔履歴 Simulation.cs#278〕
        double indPopWeighted = indPopWeight > 1e-9 ? indPopSum / indPopWeight : avgInd;
        avgInd += (indPopWeighted - avgInd);
        double sanitation = sanitPop > 1e-9 ? sanitSum / sanitPop : 1.0;
        // 人2（扶養力の食料産出アンカー・大改修スカフォールド→本実装）：現行は capacity＝初期人口(baseline)×有界乗数束のため天井が約2.5倍で 19世紀の人口爆発（英4倍/米23倍）が構造的に不能。〔履歴 Simulation.cs#279〕
        double foodRatioConsumption = foodNeed > 1e-9 ? foodMet / foodNeed : 1.0;   // 消費ベース（需要クランプ・現行経路）
        // B-高1：分子に主食の純輸入を算入（FoodAnchorNetImport=0 で加算項ゼロ＝厳密恒等）。〔履歴 Simulation.cs#280〕
        double foodRatioProduction = foodNeed > 1e-9 ? (stapleOutput + c.FoodAnchorNetImport * Math.Max(0, stapleNetImport)) / foodNeed : 1.0;   // 生産ベース（未クランプ＝実食料産出÷必要量。余剰で1超＝天井押し上げ）
        // 人2補完(1b)：生産比は月次の豊凶（HarvestFactor×帯天候×病害）を生で浴びて跳ねる→FoodFillと同じ時定数のEMAで平滑（初回=-1未捕捉は直接初期化＝跳躍なし）。
        if (World.FoodRatioProductionSmoothed < 0) World.FoodRatioProductionSmoothed = foodRatioProduction;
        else World.FoodRatioProductionSmoothed = PoliticalQuantities.Toward(World.FoodRatioProductionSmoothed, foodRatioProduction, c.FoodFillSmoothing);
        // 人2補完(1c)：輸入依存国（英）の保護＝生産ブレンドは**上方向専用**（max）。輸入で食う国の capacity を産出不足が直に削らない
        //（輸入の分子算入は B-高1 で実装済み＝下の foodRatioProduction が FoodAnchorNetImport で加算。ここは max で「農業拡大は天井を押し上げ・輸入国は従来の消費アンカー」）。
        double foodRatioRaw = Math.Max(foodRatioConsumption,
            foodRatioConsumption + c.FoodOutputAnchorProduction * (World.FoodRatioProductionSmoothed - foodRatioConsumption));   // FoodOutputAnchorProduction=0で消費ベースに厳密一致
        double foodAnchor = baseline * Math.Max(0.3, foodRatioRaw);          // 実食料産出アンカー（下限0.3＝崩壊防止・baseline側と同水準）。生産ベースでは baseline×(実産出/必要)＝扶養人口
        double capacityBaseline = baseline + c.CapacityFromFoodOutput * (foodAnchor - baseline);
        // landAnchor = Σ耕地/(1−都市化率)＝耕地は農村人口容量の単位（ArableLand=初期農民×余裕の定義）・都市民は農村の食料基盤の上に乗る。〔履歴 Simulation.cs#281〕
        if (c.LandAnchorCapacityWeight > 0 && arableSum > 1e-9)
        {
            // landAnchor＝Σ耕地そのもの（農村人口容量）。都市民の上乗せは既存の乗数束が担う＝都市化率で割らない
            //（割ると旧世界の耕地余裕1.8×が都市化とともに膨張＝清936M/仏82Mの過剰解放を実測→撤回）。
            double landAnchor = arableSum;
            capacityBaseline += c.LandAnchorCapacityWeight * Math.Max(0, landAnchor - capacityBaseline);
        }
        // 空き土地＝辺境の高出生（米西部のTFR>7＝早婚多産）として下の fertility へ。〔履歴 Simulation.cs#282〕
        double landRoom = c.LandRoomFertilityWeight > 0 && ruralSum > 1e-9
            ? c.LandRoomFertilityWeight * Math.Max(0, arableSum / ruralSum - 1.0) : 0;
        // 無上限だと米は西部解錠後 landRoom≈3.7＝出生倍率4.7倍→年6.4%成長（史実3.09%・生物学的上限約4%）。〔履歴 Simulation.cs#283〕
        landRoom = Math.Min(landRoom, c.LandRoomFertilityCap);
        // 出生の項は avgInd のまま（出生の設計 9-2 の持ち物）。〔履歴 Simulation.cs#284〕
        double capacity = capacityBaseline * (1 + c.HealthcareCapacityBonus * healthcare + c.HealthFillCapacityBonus * doctorCare
                                        - c.UrbanCapacityPenalty * World.UrbanShare + c.FoodCapacityWeight * (World.FoodFill - 1.0)
                                        + c.SanitationCapacityWeight * World.UrbanShare * (sanitation - 1.0)) * WorkHoursCapacity(World.WorkHoursValue, c);   // WorkHoursCapacity〔履歴 Simulation.cs#285〕
        if (!(capacity > 1e-9)) return;   // 〔履歴 Simulation.cs#347〕門＝錨 0 かつ耕地の無い世界（較正の小世界・扶養力 0）は人口の段を飛ばす（否定形＝NaN も飛ばす側）
        // popprobe が読む。〔履歴 Simulation.cs#286〕
        World.DiagCapacity = capacity; World.DiagCapacityBaseline = capacityBaseline; World.DiagAnchorBaseline = baseline;   // DiagCapacity〔履歴 Simulation.cs#287〕
        World.DiagHealthcare = healthcare; World.DiagDoctorCare = doctorCare;
        World.DiagFoodRatio = foodRatioRaw; World.DiagLandAnchor = arableSum;   // 下限（崩壊防止＝飢饉でも全滅しない＝翌年の収穫で再生可能）
        World.DiagStapleNetImport = stapleNetImport;   // DiagStapleNetImport〔履歴 Simulation.cs#288〕
        // 出生率＝女性の識字で下がる（人口転換の因果＝女子教育→少子化）。賃金保持の無い社会は女子に教育が届きにくい（係数 FemaleLitFloor＝0.6）。
        // 初等識字（開始点まで）は出生率に効かない（実証準拠）＝無学な社会は従来の較正値のまま。
        // 倍率排除④：旧 WomenPropertyLaw?1:0.6 の2値を、賃金保持（連続）で 0.6→1.0 を補間（retention=0で0.6＝旧無法・retention=1で1.0＝旧有法）。
        double femaleLit = (litSum / pop) * TechEducation.MotherLiteracyRatio(c, World.WomenWageRetention);
        // WomenEducationLaw〔履歴 Simulation.cs#289〕
        if (World.WomenEducationLaw)
            femaleLit = Math.Min(litSum / pop, femaleLit * (1.0 + c.WomenEduLitBonus));
        // 出生率＝基礎−女性識字−所得/発展(工業化度)。識字は教育経路、工業化は所得/都市化経路で、医療(死亡率低下＝capacity)の後に遅れて効く＝二段階の人口転換（B-4）。
        // U3：空き土地(landRoom)＝辺境出生ブースト。FertilityBase以上へ引き上げ（クランプ上限も同じだけ上げる）＝米西部の高出生を扶養力天井でなく出生側で表現。landRoom=0で厳密恒等。
        double landFertBoost = c.FertilityBase * landRoom;
        double fertility = PoliticalQuantities.ClampOr(c.FertilityBase + landFertBoost - c.FertilityLitSlope * Math.Max(0, femaleLit - c.FertilityLitOnset) - c.FertilityIncomeSlope * avgInd, c.FertilityFloor, c.FertilityBase + landFertBoost, c.FertilityFloor);   // fertility〔履歴 Simulation.cs#290〕
        // 人7（出生率倍率の死亡側混線の解消）：ロジスティック純増 net＝PopGrowthRatePerMonth×fertility×… なので、totalDeaths＝totalBirths−net の両項が fertility 比例。〔履歴 Simulation.cs#291〕
        double net = c.PopGrowthRatePerMonth.Value * pop * (1.0 - pop / capacity);   // ロジスティック：均衡＝扶養力へ収束（出生−死亡の純）。fertility の死亡側寄与は無い（出生側だけが fertility を持つ）
        World.DiagFertility = fertility; World.DiagLandRoom = landRoom;   // DiagFertility〔履歴 Simulation.cs#292〕
        var mortW = Pop.MortalityWeights(c.CrudeBirthRatePerYear.Value);   // ★年齢の層（段4e 後半）：平常の死の年齢の重み（標準のピラミッドが1783年の出生で動かない重み）
        double massPop = 0, massBirthWeight = 0, massBirthKey = 0, massDeathWeight = 0;
        var regions = World.Regions;
        var regKey = new double[regions.Count];   // regKey〔履歴 Simulation.cs#293〕
        var regBW = new double[regions.Count];    //   州ごとの旧の鍵の和＝州の中の階級への配り方（凍結の分母のまま＝9-2）
        for (int ri = 0; ri < regions.Count; ri++)
        foreach (var p in regions[ri].Pops) if (!p.IsSoldier)   // 出生死亡は兵士以外の全階級（貴族・聖職も人口とともに増減。兵士は軍事力学が別途管理）
        {
            massPop += p.Size;
            massDeathWeight += p.Size * AgeMortalityIndex(p, mortW);   // AgeMortalityIndex〔履歴 Simulation.cs#294〕
            if (p.InitialFertileShare.Value < 0)
                p.InitialFertileShare = new Share(Math.Max(c.FertileShareFloor.Value, FertilePool(p, c)));   // t=0 の出産可能女性プールを各popの相対基準として捕捉（BirthWeight と同一式＝人5 のカップル律速も t=0 正規化に反映）

            double bwp = BirthWeight(p, c);
            massBirthWeight += bwp;   // ①Phase3：国の出生の総数の物差し（既定は Size＝massBirthWeight==massPop）
            regBW[ri] += bwp;
            double key = p.Size * FertilePool(p, c);
            regKey[ri] += key; massBirthKey += key;
        }
        if (massPop < 1e-9) return;
        bool ageDeath = massDeathWeight > 1e-12;   // 年齢の重みが全て0（係数で死亡率が負に振り切れた場合）なら規模で配る＝台帳の Σ死＝総死は保つ
        // 出生は配分鍵（粗出生）、平常の死は国の平常の死亡率×規模。〔履歴 Simulation.cs#295〕
        double totalBirths = (Calendar.PerYearToPerMonth(c.CrudeBirthRatePerYear) * fertility * massBirthWeight).Value;   // 既定 massBirthWeight==massPop ＝現行と厳密一致
        bool noMothers = !(massBirthKey > 1e-12);   // 産む人が居ない（全 POP の出産可能のプール≈0）＝出生0（否定形＝NaN も0）
        if (noMothers) totalBirths = 0;
        bool recovering = net >= totalBirths; if (Instruments.On) Instruments.DemoMonth(World, recovering, noMothers);   // recovering〔履歴 Simulation.cs#296〕
        double totalDeaths = recovering ? 0 : totalBirths - net;
        double deathRate = recovering ? 0 : totalDeaths / (ageDeath ? massDeathWeight : massPop);   // 平常の死の率（年齢の指数×規模の1単位あたり・月）
        double diagBirths = 0, diagDeaths = 0;   // diagBirths〔履歴 Simulation.cs#297〕
        double ghostDeaths = 0;   // 人口保存バグ修正：負人口防止クランプ(下)で削られた「人が足りず起き得なかった死」を集計し、台帳のnetから差し戻す（無クランプ時は0＝ビット恒等・West巨大人口の崩壊時のみ発火）
        double famineDead = 0;    // 飢えの超過死亡（国の死の予算の外）の実際に減った人数＝台帳へ別に引く
        // 旗 0・段 None は estateRate＝0（以下の分岐は全て飛ぶ＝式を計算しない・乱数を引かない・リストを確保しない）。〔履歴 Simulation.cs#298〕
        EmptyPopEstate.Step(World); double estateRate = Government.InheritanceRate(World.Inheritance);   // Step〔履歴 Simulation.cs#299〕
        World.LastEstateDutyPerMonth = 0;
        List<Pop>? estPayers = estateRate > 0 ? new List<Pop>() : null; List<double>? estClaims = estateRate > 0 ? new List<double>() : null; double estSum = 0;
        bool birthsSuppressed = recovering && noMothers;   // 人口保存：回復期に産む人が居ない（add0=0）と実出生0なのに台帳が+=netでズレる＝netを差し引く（正常時false・出産可能プール≈0の退化popでのみ発火）
        Span<double> disasterW = stackalloc double[2 * Pop.Layout.Bands];
        Pop.DisasterWeights(disasterW, c.DisasterAgeMortalityExcess);   // 飢えの超過死亡の年齢の重み（災害死と同じ）
        for (int ri = 0; ri < regions.Count; ri++)
        {
            var r = regions[ri];
            if (Instruments.On) Instruments.RdhRegion(r);   // Instruments〔履歴 Simulation.cs#300〕
            // regShare〔履歴 Simulation.cs#301〕
            double regShare = noMothers ? 0 : regKey[ri] / massBirthKey;
            double regBWr = regBW[ri];
            double estDeliv = estateRate > 0 ? Politics.RuleDelivery(r, c) : 0.0;   // estDeliv〔履歴 Simulation.cs#302〕
            foreach (var p in r.Pops)
                if (p.IsSoldier) { if (p.Size > 1e-12) p.StepAges(p.Size, 0, 0, mortW); }   // IsSoldier〔履歴 Simulation.cs#303〕
                else
                {
                    double bk = regBWr > 1e-300 ? regShare * BirthWeight(p, c) / regBWr : 0;   // 出生の取り分＝州の取り分×州の中の旧の鍵の比（加齢の「前」に捕捉＝事前パスと同一構成＝Σ取り分＝1＝Σbirths=net の保存を厳密化）
                    double size0 = p.Size;           // 月初の規模＝出生・平常の死・飢えの上乗せが掛かる同じ規模
                    double dw = ageDeath ? size0 * AgeMortalityIndex(p, mortW) : size0;   // 平常の死の配分鍵（事前パスと同じ月初の構成）
                    // 出生は**粗出生**として0〜9 の層に加わる（純増だけを子供にすると成熟社会で子供が払底＝構成崩壊）。〔履歴 Simulation.cs#304〕
                    double births, deaths;
                    if (recovering)
                    {
                        double add0 = net * bk;   // 回復期：純増を出生の取り分で按分。産む人が居なければ0（旧 massBirthWeight≈0 の 0/0=NaN の防止＝#54運賃点火が露出した下流NaN と同じ）
                        births = add0; deaths = 0;
                    }
                    else
                    {
                        births = totalBirths * bk;   // births〔履歴 Simulation.cs#305〕
                        double deathsRaw = deathRate * dw;                  // deathsRaw〔履歴 Simulation.cs#306〕
                        deaths = Math.Min(deathsRaw, p.Size + births);   // 下限保護（負人口の防止）
                        ghostDeaths += deathsRaw - deaths;   // クランプで削られた「人が足りず起き得なかった死」を集計（下でnetから差し戻す）。無クランプ時は0＝ビット恒等（West巨大人口の崩壊時のみ発火＝人口保存バグ修正）
                    }
                    double add = births - deaths;
                    double newSize = p.Size + add;
                    if (newSize < 0) { ghostDeaths += -newSize; newSize = 0; }   // 死亡クランプ発火時の浮動小数丸めでSizeが極小負(≈-3.5e-18)になるのを0にクランプし、削った分をghostDeathsで台帳へ差し戻す（非負境界の保護・正常時newSize≥0で不発＝ビット恒等）
                    diagBirths += births; diagDeaths += births - (newSize - p.Size);   // ★計器：死亡＝実際に減った人数（クランプ込み）＝出生−実際の増分
                    // Instruments〔履歴 Simulation.cs#307〕
                    if (Instruments.On) Instruments.DemoPop(World, p, size0, births, births - (newSize - size0), mortW); p.StepAges(size0, births, births - (newSize - size0), mortW);   // Instruments〔履歴 Simulation.cs#308〕
                    p.Size = newSize;
                    // 凸（指数4）＝慢性の浅い不足（充足0.6〜0.7）はほとんど殺さず、深い飢饉は速く殺す（史実の飢饉の死は栄養の不足に強く凸・係数の注記）。〔履歴 Simulation.cs#309〕
                    double famineD = 0;   // famineD〔履歴 Simulation.cs#310〕
                    double fill = p.FoodFill;   // 主食の充足の1本
                    double floorF = c.FamineDeathFillFloor;
                    double excess = fill < floorF ? Calendar.PerYearToPerMonth(c.FamineExcessMortality) * Math.Pow((floorF - fill) / floorF, c.FamineExcessMortalityExponent) * size0 : 0.0;
                    if (excess > 0 && p.Size > 0)
                    {
                        double d = Math.Min(excess, p.Size);
                        if (Instruments.On) Instruments.DemoFamine(World, p, d, disasterW); p.KillAges(p.Size, d, disasterW);   // Instruments〔履歴 Simulation.cs#311〕
                        p.Size -= d; famineDead += d; famineD = d;
                    }
                    if (estateRate > 0 && size0 > 1e-12)   // estateRate〔履歴 Simulation.cs#312〕
                    {
                        double sv = p.Savings.Value, deadP = deaths + famineD;   // 平常の死（回復期は 0）＋飢えの上乗せ
                        if (sv > 0 && deadP > 0)
                        {
                            double cl = deadP * (sv / size0) * estateRate * estDeliv;
                            if (cl > 0) { estPayers!.Add(p); estClaims!.Add(cl); estSum += cl; }
                        }
                    }
                }
        }
        if (estateRate > 0 && estSum > 0) World.LastEstateDutyPerMonth = World.Transfer((World, estPayers!), (World, null, null), estSum, TreasuryCat.OtherIn, fromWeights: estClaims);   // ★(650) 相続の移転（走査の後に 1 回・払い手ごとに自分の請求・自分の貯蓄まで）
        if (Instruments.On) Instruments.RdhRegion(null);   // Instruments〔履歴 Simulation.cs#313〕
        World.DiagBirths += diagBirths; World.DiagDeaths += diagDeaths + famineDead;   // diagBirths〔履歴 Simulation.cs#314〕
        World.PopulationDelta += net + ghostDeaths - (birthsSuppressed ? net : 0.0) - famineDead;   // 人口保存：意図netに①クランプで起き得なかった幽霊死ghostDeathsを差し戻し②ガードで実出生0の時はnetを引く③飢えの上乗せで実際に減った人数を引く。実Size減と一致
        SmoothRegionalFoodFill(c);   // 州の食料充足の平滑（移民の飢饉の押し出し・州反乱・派閥の危機が読む）
    }

    /// <summary>
    /// 州の食料充足（Region.FoodFill）の平滑＝穀倉の備蓄（餓死は数ヶ月の持続的欠乏で起きる）。〔履歴 Simulation.cs#315〕
    /// </summary>
    private void SmoothRegionalFoodFill(Coefficients c)
    {
        foreach (var region in World.Regions)
        {
            double met = 0, need = 0;
            foreach (var p in region.Pops) { met += p.FoodMet; need += p.FoodNeed; }
            double raw = need > 1e-9 ? PoliticalQuantities.Clamp01(met / need) : 1.0;
            region.FoodFill = PoliticalQuantities.Toward(region.FoodFill, raw, c.FoodFillSmoothing);   // 州の食料充足を平滑（穀倉の備蓄＝餓死は数ヶ月の持続的欠乏で起きる）
        }
    }


    /// <summary>改革法の地代上限：全面＝設定値／限定的＝なし(1.0)と全面の中間／なし＝1.0(無制限)。＝段階的な土地改革/農奴解放。</summary>
    internal static double RentCap(ReformLevel lv, double full) => lv switch
    {
        ReformLevel.Full => full,
        ReformLevel.Limited => full + (1.0 - full) * 0.5,
        _ => 1.0,
    };

    /// <summary>労働時間→扶養力係数（連続・倍率排除②）：12時間（過重労働の基準）より短い1時間ごとに健康改善で扶養力↑。
    /// 人12（14hの健康被害が12hと同値の解消）：長時間側（12h超）は現行 Max(0,12−hours) で一律ゼロ＝工場法以前の超長時間労働に人口動態上のコストがない。
    /// OverworkMortalityWeight=0 で長時間側の追加項ゼロ＝現行と厳密一致・>0で12h超の1時間ごとに扶養力↓（超過死亡＝両側に傾く）。出力低下は別途 techProd 側。</summary>
    private static double WorkHoursCapacity(double hours, Coefficients c) => 1.0
        + c.OverworkHealthSlope * Math.Max(0.0, WorkHours.Twelve.Hours() - hours)
        - c.OverworkMortalityWeight * Math.Max(0.0, hours - WorkHours.Twelve.Hours());

    /// <summary>
    /// 鉱脈＝有限の在庫：毎月、州の鉱物産出の合計だけ減る（**決定論＝サイコロなし**）。〔履歴 Simulation.cs#316〕
    /// </summary>
    private void StepMineralDeposits()
    {
        var c = World.Coef;
        // 排水と発見は**対の機構**＝同じゲートに置く（較正世界は従来の確率イベントのまま＝24-24-24の前提を壊さない）。〔履歴 Simulation.cs#317〕
        if (World.WarmupFrozen || World.Diplomacy is null) return;   // WarmupFrozen〔履歴 Simulation.cs#318〕
        foreach (var r in World.Regions)
        {
            double outSum = 0;
            foreach (var p in r.Producers) if (Catalog.IsMineral(p.GoodId)) outSum += p.Output;
            if (!(outSum > 0)) continue;   // 産出なし（NaNも「なし」へ倒す＝否定形で書く）
            if (r.MineralDepositInitial <= 0)
            {
                // 初回捕捉：この州の初期鉱脈＝現在の年率×DepositYearsAtBirth
                r.MineralDepositInitial = Calendar.PerMonthToPerYear(outSum) * c.DepositYearsAtBirth;
                r.MineralDeposit = r.MineralDepositInitial;
            }
            r.MineralDeposit = System.Math.Max(0, r.MineralDeposit - outSum);   // 掘った分だけ減る
            double frac = r.MineralDepositInitial > 1e-9 ? r.MineralDeposit / r.MineralDepositInitial : 1.0;
            double knee = System.Math.Clamp(c.DepositGradeKnee.Value, 1e-6, 1.0);
            double grade = frac >= knee ? 1.0 : c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * System.Math.Max(0, frac) / knee;
            if (!(grade >= c.DepositGradeFloor && grade <= 1.0)) grade = 1.0;   // NaNは品位低下なし側へ倒す（否定形＝ClampはNaNを消さない）
            r.MineralGradeFactor = grade;
        }
    }

    /// <summary>
    /// 鋳造（金鉱→正貨）：金鉱産出の一部を造幣局が新鋳貨で買い取る＝基礎貨幣の増加（金産出国の通貨的優位・ゴールドラッシュ→マネー膨張→物価/好況）。〔履歴 Simulation.cs#319〕
    /// </summary>
    private void MintGold()
    {
        bool insOn = Instruments.On;   // 読みは 1 回（OnDoubleReadScanTests）
        if (insOn) { Instruments.MintregMonth(World); Instruments.MintcapMonth(World); }   // MintregMonth（(1168) 造幣の床の効き・mintreg_s）・MintcapMonth（(1180) 造幣の買いの置き場・mintcap_s）
        // 旧来（産出比例の上限・国内鉱山限定・金鉱だけ）の枝と GoldMintFraction は消した。〔履歴 Simulation.cs#320〕
        int metal = Catalog.GoldOreId;
        bool isFiat = World.MonetaryLaw == MonetaryStandardLaw.Fiat;
        if (!isFiat) metal = World.MonetaryLaw == MonetaryStandardLaw.Silver ? Catalog.SilverId : Catalog.GoldOreId;
        // 買うより先に売る＝   金へ移った国は同じ月から銀を吐き出す（独1871年型＝買い手の消滅と投げ売りが同時）。〔履歴 Simulation.cs#321〕
        SellDemonetizedReserves(metal, isFiat);
        if (isFiat) return;   // 不換＝準備需要そのものが無い（造幣しない）
        // ★準備需要（G3）：買う量は「基礎貨幣×目標カバー率 − 現在の準備」＝準備が足りているほど買わない。〔履歴 Simulation.cs#322〕
        double coverShort = 0;   // coverShort〔履歴 Simulation.cs#323〕
        double gapLeft = 0;      // gapLeft〔履歴 Simulation.cs#324〕
        if (World.Coef.MintReserveTargetWeight > 0)
        {
            double coverage = World.BroadMoney.Value > 1e-9 ? World.BaseMoney / World.BroadMoney : 1.0;
            double coverTarget = World.Coef.GoldCoverMin.Value * World.Coef.GoldRejoinCoverMargin;
            double shortfall = coverTarget > 1e-9 ? (coverTarget - coverage) / coverTarget : 0;
            shortfall = PoliticalQuantities.Clamp01(shortfall);   // NaN は Clamp01 が0＝造幣停止側へ倒す
            coverShort = shortfall;
            if (shortfall > 1e-9)
                gapLeft = Math.Max(0, (coverTarget * World.BroadMoney - World.BaseMoney).Value) * PoliticalQuantities.Clamp01(World.Coef.MintReserveSellRatePerMonth.Value);   // 買い戻しの歩調＝投げ売りと同じ率（独1873-79の逆向き）
        }
        // parity〔履歴 Simulation.cs#325〕
        double parity = Catalog.RefPrices[metal];   // 造幣平価（固定・RefPrice建て）＝史実の固定平価。市場価格がこれを超えれば誰も持ち込まない
        // 準備不足（coverShort>0＝市場買い gapLeft の根）の月は**売らない**   （州ごとの !atMarket では gapLeft が尽きた後続州が同じ月に平価で売る＝買い高・売り安の閉ループ）。〔履歴 Simulation.cs#326〕
        double sellLeft = 0;
        if (World.Coef.MintParitySells > 0 && !(coverShort > 1e-9))
        {
            sellLeft = (metal == Catalog.SilverId ? World.SilverReserveQty : World.GoldReserveQty) * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRatePerMonth.Value);
            if (!(sellLeft > 1e-12)) sellLeft = 0;   // 否定形＝NaN/非正の準備は売らない側へ
        }
        bool capOnly = World.Coef.MintBuysAtCapital > 0;   // (1180) 旗 0（NaN・非正を含む）＝全ての州の旧の道
        foreach (var r in World.Regions)
        {
            if (capOnly && !ReferenceEquals(r, World.Region)) continue;   // (1180) 旗 1＝首都の州（Regions[0]）の市場だけ
            if (r.GoodOf(metal) is null) continue;
            var m = r.Markets[metal];
            bool atMarket = m.Price > parity && gapLeft > 1e-9;   // atMarket〔履歴 Simulation.cs#327〕
            // 数量＝今月の国1本の予算 sellLeft（準備×MintFloorBuyRatePerMonth＝買い側と同じ月次率・ループ前で確定）と買い手の貯蓄÷平価の小さい方。〔履歴 Simulation.cs#328〕
            if (sellLeft > 1e-12 && m.Price > parity)
            {
                double sold = SellReserve(r, metal, sellLeft, parity);   // 準備の売却の1本（平価で・下の投げ売りと同じ）
                if (metal == Catalog.SilverId) World.SilverReserveQty -= sold; else World.GoldReserveQty -= sold;
                sellLeft -= sold;                                          // 国1本の月次予算を消費
            }
            if (m.Stock <= 1e-12) continue;
            if (m.Price > parity && !atMarket) continue;   // 市場価格＞平価なら鋳造停止（宝飾/工業へ流れる＝正帰還の遮断・史実の固定平価）。（市場買いの枝は (1168) で旗ごと焼き込んだ）
            double qty = atMarket ? Math.Min(m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRatePerMonth.Value), gapLeft / m.Price)   // 市場買い＝床と同じ在庫上限×今月の予算（第1版の産出比例は銀鉱州にしか買い手が立たなかった）
                               : m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRatePerMonth.Value);   // 床＝在庫があれば買う（自由鋳造は持ち込み制＝産出と無関係）
            if (qty <= 1e-12) continue;
            double totOut = 0;
            foreach (var p in r.Producers) if (p.GoodId == metal) totOut += p.Output;
            double price = atMarket ? m.Price : parity;   // 鋳造価値＝平価建て（旧 qty×m.Price は正帰還）。★市場買い（atMarket）だけ市場価格＝準備の購入代金
            // 払い手＝造幣局（新しい貨幣＝世界の台帳は鋳造＝移し方の1本の minted）。〔履歴 Simulation.cs#329〕
            double imp = Math.Max(0, m.PrevImport + m.LastImport), prodQ = Math.Max(0, m.LastProduced);
            double impShare = totOut > 1e-12 ? (imp + prodQ > 0 ? imp / (imp + prodQ) : 0.0) : 1.0;
            double want = qty * price;
            double impPart = want * impShare;
            double paid = InternationalMarket.PaySales((null, null), r, metal, want - impPart, World, minted: true);
            if (impPart > 0)
                paid += World.Transfer((null, null), (World, r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? [r.Pops[r.MerchantPopIndex]] : r.Pops, null), impPart, TreasuryCat.OtherIn, income: true, outside: World.Outside.Mint);
            if (!(paid > 1e-12)) continue;
            if (paid < want) qty *= paid / want;   // 受け手のいない分は引き取らない（量を減らす）
            double value = paid;
            if (atMarket) gapLeft -= value;   // 予算を消費（国の準備ギャップは州をまたいで1本）
            m.Stock -= qty; m.LastSold += qty;        // 造幣局への引き取り＝GoodsBalance不変（Stock−／LastSold＋で対称）
            if (insOn) { Instruments.SilverMintCapture(r, metal, qty); Instruments.MintregBought(r, metal, qty); Instruments.MintcapBought(World, r, metal, qty); }   // SilverMintCapture〔履歴 Simulation.cs#330〕
            // 実測＝造幣は世界の銀産出の12.8〜17.7%を毎月吸っているのに、銀/金の相対価格は151年で2〜3%しか動かない。〔履歴 Simulation.cs#331〕
            m.LastDemand += qty;   // qty〔履歴 Simulation.cs#332〕
            //（鋳造の台帳＝基礎貨幣増の正当な源は、上の移し方の1本〔minted〕が動いた額で書いた＝MoneyConservation が許容）
            // ★床モード＝吸った現物を国の準備として保持（旧来は「コインになって消える」＝追跡しない＝恒等）。
            //   法が本位を捨てたとき、この準備が市場へ売られる＝暴落を深める投げ売りが台本なしで出る。
            if (metal == Catalog.SilverId) World.SilverReserveQty += qty; else World.GoldReserveQty += qty;
        }
    }

    /// <summary>
    /// ⚠鋳造時に発行された貨幣は流通し続ける（demonetized coins＝不換のオーバーハング）。〔履歴 Simulation.cs#333〕
    /// </summary>
    private void SellDemonetizedReserves(int lawMetal, bool isFiat)
    {
        double rate = PoliticalQuantities.Clamp01(World.Coef.MintReserveSellRatePerMonth.Value);
        if (rate <= 0) return;
        if (World.SilverReserveQty > 1e-9 && (isFiat || lawMetal != Catalog.SilverId))
            World.SilverReserveQty -= SellReserve(World.Region, Catalog.SilverId, World.SilverReserveQty * rate);
        if (World.GoldReserveQty > 1e-9 && (isFiat || lawMetal != Catalog.GoldOreId))
            World.GoldReserveQty -= SellReserve(World.Region, Catalog.GoldOreId, World.GoldReserveQty * rate);
    }

    /// <summary>
    /// 払えるのは貯蓄まで＝払えない分は売らない（量を減らす）。〔履歴 Simulation.cs#334〕
    /// </summary>
    private double SellReserve(Region r, int gid, double qty, double price = double.NaN)
    {
        if (!(qty > 1e-12) || r.GoodOf(gid) is null) return 0;
        var m = r.Markets[gid];
        if (double.IsNaN(price)) price = m.Price;
        if (!(price > 1e-12)) return 0;   // 否定形＝NaN・0 の値段では売らない
        int b = r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count ? r.MerchantPopIndex
              : r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count ? r.InvestorPopIndex : -1;
        if (b < 0) return 0;
        double sold = World.Transfer((World, [r.Pops[b]]), (null, null, null), qty * price, outside: World.Outside.Mint) / price;   // 払えた額の分だけ（Transfer は貯蓄まで）
        if (!(sold > 1e-12)) return 0;
        m.Stock += sold; m.LastImport += sold;
        return sold;
    }

    /// <summary>
    /// 黒字が出たらまず返す。〔履歴 Simulation.cs#335〕
    /// </summary>
    private void RepayHouseholdDebt(Region r)
    {
        if (!(World.Coef.PopDebtDriven > 0) || !(World.Coef.DebtRepayFrac > 0) || World.Bank is null) return;
        foreach (var p in r.Pops)
        {
            if (!(p.Debt.Value > 0)) continue;   // ⚠否定形＝NaN も飛ばす
            double pay = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value) * World.Coef.DebtRepayFrac);
            if (!(pay > 0)) continue;
            Bank.Lend(World, p, -pay);   // 返済＝貸しの逆（Bank.Lend の1本）
        }
    }

    /// <summary>
    ///  損失吸収の脆さの会計（§9-C 拡張）：月次の全フロー後、損失で貯蓄が負になった pop の不足を信用（借入）で埋める。〔履歴 Simulation.cs#336〕
    /// </summary>
    internal void BackstopNegativeSavings()
    {
        var bank = World.Bank!;   // 構築で必ず作る（Simulation のコンストラクタ）
        List<Pop>? one = null; List<double>? unit = null;   // 受け手1人の並び（使い回す＝POP ごとに確保しない）
        foreach (var r in World.Regions)
            foreach (var p in r.Pops)
                if (p.Savings.Value < 0)
                {
                    double need = -p.Savings.Value;
                    // 受け手は重み1（頭数0の POP でも受ける＝旧と同じ）。〔履歴 Simulation.cs#337〕
                    (one ??= new List<Pop>(1)).Clear(); one.Add(p);
                    World.Transfer((null, null), (World, one, unit ??= [1.0]), need, outside: World.Outside.Credit);
                    World.LossFinanced += need; // 可視化：損失の負債化を累積記録
                    // PopDebtLossShare は「消えた赤字のうち個人が背負う割合」＝有限責任の度合い（1.0で無限責任）。〔履歴 Simulation.cs#338〕
                    if (World.Coef.PopDebtDriven > 0) p.Debt += new MoneyStock(need * World.Coef.PopDebtLossShare);
                }
        // ★E6：負の分だけ与信を償却（基礎貨幣は償却の額だけ上がり、非常発行が同じ額だけ台帳に積まれる＝保存量は動かない）。
        //   ⚠NaN は比較が偽＝何もしない（否定形の門）。償却の後の基礎貨幣＝0（与信が足りれば）か国庫の負の分（与信を全部消しても残る分）。
        double writeOff = Math.Min((-World.BaseMoney).Value, bank.Loans.Value);
        if (writeOff > 0) { var wo = new MoneyStock(writeOff); bank.Loans -= wo; World.CumulativeFiatIssued += wo; }   // 非常発行の台帳（負の国の与信の償却だけ）
    }


    // 到達不能になったため。〔履歴 Simulation.cs#339〕

    private void CheckInvariants()
    {
        foreach (var inv in _invariants) inv.Check(World);
    }

    private void Snapshot()
    {
        if (!TelemetryEnabled) return;   // TelemetryEnabled〔履歴 Simulation.cs#340〕
        var home = World.Region;
        var foodMkt = home.Markets[0];
        var foodProd = home.Producers[0];
        int farmerIdx = 0, workerIdx = 1;
        // ★是正（監査G03#4）：Markets[0]/Producers[0]/Pops[0]/[1]の添字決め打ちは、Nation/FullChain系（Catalog全財の世界＝
        //   producer.IsRgo が実際に設定される世界）では鉄鉱石(id0)・貴族・農民を指す＝FoodPrice/FoodOutput/
        //   FarmerSatisfaction/WorkerSatisfaction のラベルが偽になっていた。主食（Staple・home.Goods基準＝カスタム世界の
        //   GoodId流用と衝突しない）のRGOを実際の生産関係（LaborPopIndex）から見つけて記録する。
        //   ⚠工場（worker）探しはこの中に**入れ子**にする：旧・簡易シナリオ（IsRgoを一度も設定しない世界）はRGOも工場も
        //   IsRgo=falseの既定値で見分けが付かない＝外に出すと農民popをworker扱いする退行が起きる（実測で発見）。
        //   IsRgoが意味を持つ世界だとここで確認できて初めて工場探しを行う＝見つからない小世界は従来の添字（0/0/0/1）へ
        //   自然にフォールバック＝挙動不変。
        foreach (var g in home.Goods)
        {
            if (g.Tier != GoodTier.Staple) continue;
            Producer? sp = null;
            foreach (var pr in home.Producers) if (pr.GoodId == g.Id && pr.IsRgo) { sp = pr; break; }
            if (sp is null) continue;
            if (g.Id < home.Markets.Length) foodMkt = home.Markets[g.Id];
            foodProd = sp; farmerIdx = sp.LaborPopIndex;
            foreach (var pr in home.Producers)
            {
                if (pr.IsRgo) continue;
                Good? gd = null;
                foreach (var g2 in home.Goods) if (g2.Id == pr.GoodId) { gd = g2; break; }
                if (gd is not null && gd.Tier != GoodTier.Service) { workerIdx = pr.LaborPopIndex; break; }
            }
            break;
        }
        double manufPrice = home.Markets.Length > 1 ? home.Markets[1].Price : 0.0;
        double farmerSat = farmerIdx >= 0 && farmerIdx < home.Pops.Count ? home.Pops[farmerIdx].Satisfaction : 1.0;
        double workerSat = workerIdx >= 0 && workerIdx < home.Pops.Count ? home.Pops[workerIdx].Satisfaction : 1.0;

        double litSum = 0, conscSum = 0, milSum = 0, sizeSum = 0;
        foreach (var p in World.AllPops())
        {
            litSum += p.Literacy * p.Size;
            conscSum += p.Consciousness * p.Size;
            milSum += p.Militancy * p.Size;
            sizeSum += p.Size;
        }
        double gdp = World.GdpPerMonth.Value;   // 役務を除いた産出×参照価格（旧実装は r.Goods をIDで添字していたが、ソート後は不一致＝集計バグ。World.Gdp に統一）
        double inv = sizeSum > 1e-9 ? 1.0 / sizeSum : 0.0;

        Telemetry.Add(new TurnSnapshot(
            World.Year, World.Quarter, World.TotalMoney.Value, World.TotalPopulation,
            World.UrbanShare,
            foodMkt.Price, foodProd.Output, foodMkt.Stock,
            manufPrice, farmerSat, workerSat,
            litSum * inv, gdp,
            (int)World.Polity, conscSum * inv, milSum * inv,
            World.ReformistPower, World.Coercion,
            World.BroadMoney.Value, World.OutstandingLoans.Value, World.Bank?.Confidence ?? 1.0,
            World.Treasury.Value, World.FiscalFundingRatio.Value,
            World.Regions[0].SumPopulation(), World.Regions.Count > 1 ? World.Regions[1].SumPopulation() : 0.0,
            World.Bank?.Gold.Value ?? 0.0, World.CumulativeTradeFlow.Value));
    }

    /// <summary>
    /// MarginalCost ÷= (そのレシピの Productivity ÷ 人力の Productivity)^gate。〔履歴 Simulation.cs#341〕
    /// </summary>
    internal static void ApplyRecipeCostFollowsProductivity(List<Producer> producers, Coefficients c)
    {
        double gate = c.RecipeCostFollowsProductivity;
        if (!(gate > 0)) return;
        double baseProd = 0;
        foreach (var p in producers)
            if (p.GoodId == Catalog.LaborId && p.RecipeName.Length == 0 && !p.IsRgo) { baseProd = p.Productivity; break; }
        if (!(baseProd > 0)) return;
        foreach (var p in producers)
        {
            if (p.GoodId != Catalog.LaborId || p.RecipeName.Length == 0 || p.IsRgo) continue;
            double ratio = p.Productivity / baseProd;
            if (!(ratio > 0) || double.IsInfinity(ratio)) continue;
            p.MarginalCost /= System.Math.Pow(ratio, gate);
        }
    }

    /// <summary>
    /// CriminalClassEnabled=0（既定）では pops に 1 件も足さず -1 を返す＝Pops.Count も既存の添字も不変＝**厳密恒等**（研究者と同じ作法）。〔履歴 Simulation.cs#342〕
    /// </summary>
    private static int AddCriminalPop(List<Pop> pops, Coefficients coef, double literacy)
    {
        if (!(coef.CriminalClassEnabled > 0)) return -1;
        pops.Add(new Pop { ClassId = PopClass.Criminal, ClassName = "犯罪者", IsUrban = false, Size = 0, Satisfaction = 1.0, Literacy = literacy });
        return pops.Count - 1;
    }

    /// <summary>
    /// 〔履歴 Simulation.cs#343〕
    /// </summary>
    internal const double HandcraftProductivityRatio = 0.6;
    /// <summary>
    /// 〔履歴 Simulation.cs#344〕
    /// </summary>
    internal const double HandcraftSelfCost = 0.35;
    /// <summary>農家の手仕事の行の RecipeName（職人の `"職人"` と衝突しない＝職人の没落の計器は数えない・FacilityBuildScore が候補から外す述語）。</summary>
    internal const string HouseholdRecipeName = "家内工業";

    /// <summary>
    /// 外した財＝硫酸・ソーダ灰など工業の投入を要する物（紙・陶磁器・石鹸・缶詰・防水布・医薬品・キニーネ・書籍・新聞・ガス・自転車・ラジオ）と、輸入・商品作物の地理に偏る物（茶・コーヒー・灯油・煙草）。〔履歴 Simulation.cs#345〕
    /// </summary>
    internal static readonly int[] HouseholdGoods =
    [
        Catalog.ClothingId, Catalog.FurnitureId, Catalog.PreservedMeatId, Catalog.DriedFishId,
        Catalog.BeerId, Catalog.SpiritsId, Catalog.CheeseId, Catalog.LeatherGoodsId,
    ];

    /// <summary>
    /// ★形は 2 度直した（事前登録 追記2・追記3）：①最初は職人と同じ自営（労働を投入に置かない）＝賃金が上がると手仕事の行の取り分が増える逆向きで、農民の時間が二重に使われた（点検の指摘・ソースで確定）。〔履歴 Simulation.cs#346〕
    /// </summary>
    private static void AddHouseholdProducers(List<Producer> producers, Coefficients coef)
    {
        int peasantIdx = -1;
        foreach (var pr in producers) if (pr.IsRgo) { peasantIdx = pr.LaborPopIndex; break; }
        if (peasantIdx < 0) return;   // RGO の無い世界（最小シナリオ）は不発
        foreach (int gid in HouseholdGoods)
        {
            if (!producers.Exists(p => p.GoodId == gid)) continue;   // その財を作らない世界は不発
            var d = Catalog.Goods[gid];
            producers.Add(new Producer
            {
                GoodId = gid, RecipeName = HouseholdRecipeName,
                Productivity = d.Productivity * FullChainProductivityScale * HandcraftProductivityRatio,
                Output = 30, Scale = 1.0, MarginalCost = HandcraftSelfCost,   // 職人と同じ自己費用 0.35（工場の最後の一段を手でやる行＝工場より遅く高い）
                LaborPopIndex = peasantIdx, OwnerPopIndex = peasantIdx, RentShare = default,
                Inputs = [.. d.Inputs, new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],   // 財の表の投入（職人と同じ）＋農家の時間を市場で買う（RGO の行と同じ 1 本＝賃金が両方の付加価値に同じ向きで効く）
            });
        }
    }
}
