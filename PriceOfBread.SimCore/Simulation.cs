namespace PriceOfBread.SimCore;

/// <summary>四半期ごとの最小テレメトリ1点（§2.6／§8）。段0〜4 で見る指標を持つ。</summary>
public readonly record struct TurnSnapshot(
    int Year, int Quarter,
    double TotalMoney, double TotalPopulation,
    double UrbanShare,
    double FoodPrice, double FoodOutput, double FoodStock,
    double ManufPrice,
    double FarmerSatisfaction, double WorkerSatisfaction,
    double TechLevel, double AvgLiteracy, double Gdp,
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
    // ★復活2026-09-06（内戦の陣営選択 P8＝作者裁定「プレイヤー側UIはやる」）：プレイヤーが反乱側の新国家へ乗り換えるとき、
    //   旧国は効用AIへ・新国は NullAgent へ差し替える。呼び手＝App の MainWindowViewModel.SwitchPlayer だけ。
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

    // （一時デバッグ用の静的フック Simulation.Probe と Runner の drift は 2026-09-26 に削除＝同じ役を ProbeCheck（leakhunt）が持つ・fis-19）

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
            TaxRate = scenario.TaxRate,
            PollTax = scenario.InitialPollTax,   // 人頭税＝近世国家の主財源（行政不要）。序盤の国庫を支える（所得税は19世紀までロック）
            LandTax = scenario.InitialLandTax,   // 土地税＝検地台帳を保有済みの確立国家は開始から徴収（>0なら開始解禁）
            LandTaxUnlocked = scenario.InitialLandTax > 0,   // 台帳がある＝開始から解禁（admin ゲートは無から台帳を作る場合の関門）
            ForceUnlock = scenario.ForceUnlock,
            // 国庫は Pay 経由でしか動かせない（World の台帳・2026-08-13）＝生成時の初期化は下の SetTreasuryRaw で行う。
            GovDebt = scenario.InitialGovDebt,   // P5：初期国債（仏＝米独立戦争債務）。純負債＝貨幣非移動・保存中立。GovAvgCouponはrollover=1で初回Fiscalが市場金利へ上書き
            // ★★予算法・統帥法の初期値（2026-08-20）。⚠**開始時の制度の写し取り**であって実行時の分岐ではない
            //   （作者裁定「政体は表示専用」＝政体が**上限を決める**のをやめたのであり、**初期条件の略記**としては人口や識字と同じ）。
            //   ★以後は法が一次量として独立に動く＝政体を読み直すことは無い。
            //   ⚠将来 world.json に列を足すならそちらが優先（初期条件のデータは本来そこに置く）。
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
            Bureaucracy = scenario.InitialBureaucracy,   // ★官僚制度の初期値（2026-08-04）
            InstitutionalDecay = scenario.InitialInstitutionalDecay,
            // C-高7（史実検証2026-07-22）：天井の共通床＝decayを持たない国も硬直しうる（旧＝天井=初期値のため50カ国が構造的に硬直不能＝3カ国だけの特権）。
            // DecayCeilingUniversal=0 で Max は初期値そのまま＝厳密恒等。初期値が高い国（清0.8等）はそのまま。
            InstitutionalDecayCeiling = Math.Max(scenario.InitialInstitutionalDecay, scenario.Coef.DecayCeilingUniversal),   // P7：制度疲労（天井＝初期値∨共通床）
            InitialNationalConsciousness = scenario.InitialNationalConsciousness,   // P9：国民意識v2の初期値（蓄積初回に採用・−1=係数既定）
            Latitude = scenario.NationConfig?.Latitude ?? 0,    // 首都の座標（多国＝距離で輸送マージンが嵩む）
            Longitude = scenario.NationConfig?.Longitude ?? 0,
            EthnicEnabled = scenario.NationConfig is not null,   // 民族統一度（倍率排除⑧）は各popの CoreEthnicShare 加重平均から創発。NationConfig 由来の国だけ有効化＝合成較正/最小世界は無効で較正不変
            MonetaryEnabled = scenario.NationConfig is not null,       // 物価水準（インフレ/デフレ）も実在国だけ＝合成較正/最小世界は P≡1 で従来同値
            ParliamentEnabled = scenario.NationConfig is not null,     // 議会・選挙も実在国だけ＝合成較正は従来の政治モデルのまま（33/33/33不変）

        };
        // ★J-003（作者裁定2026-09-03「首都（史実に基づく・表示のみ）と、経済中心地は別でよい」）。
        //   ⚠現物＝`World.Latitude/Longitude` は world.json の**史実の首都**から入り、
        //   **エンジンの距離計算が全部これを読んでいる**（植民地維持費・脅威・戦力投射・移民・招聘・運河・分離の投射・行政費）。
        //   ⇒ 距離の基準を**経済中心地＝1番目の州**（エンジンが首都として扱い、分離もさせない州）へ移す。
        //   ★実装は**1箇所**＝ここで座標そのものを差し替える（20箇所以上の読み手を書き換えない＝改修面積が最小）。
        //   ⚠史実の首都の写し（表示専用の DisplayCapitalLat/Lon）は画面も年代記も読んでいなかった＝2026-09-26 に削除（x-03）。
        //   ⚠差が出るのは実測3カ国だけ（露634km・コーカンド167km・モロッコ166km）＝他は200km以内で一致。
        //   ⚠CenterIsFirstProvince=0 で1命令も走らない＝厳密恒等（休眠だった＝2026-09-05 B群で点火・0は従来の腕）。
        //   ★座標の規則は遷都と同じ1本（World.SetCapital・2026-09-28 処理の一元化 段2 B）＝未登録の州名は据え置き（幻の座標を作らない）。
        if (scenario.Coef.CenterIsFirstProvince > 0 && World.Regions.Count > 0) World.SetCapital(World.Regions[0]);
        World.SetTreasuryRaw(scenario.InitialTreasury);   // 初期国庫＝フローでない（台帳に載せない）
        if (scenario.NationConfig is not null) World.FactionRng = new DeterministicRng(scenario.Seed ^ 0xFAC7_1035_EED5UL);   // F3変異の独立系列（実在国のみ＝較正/最小世界は null＝不発）
        foreach (var r0 in regions) if (r0.State == RegionState.Active) World.FoundingActiveProvinces++;   // 建国時のActive州数（2026-08-04・残余国判定の分母）
        World.FoundingPopulation = World.TotalPopulation;   // ★H-02（2026-09-03）：建国時人口＝残余国判定のもう一つの分母（州数と対）
        // ★代替レシピ（坩堝鋼/錬鉄レール・石油ケロシン・冷蔵の加工肉・黄鉄鉱法）の注入は AddAlternateRecipes（州の構築経路）へ移した（2026-09-06）。
        //   旧＝ここで World.Regions を走査＝**構築時に存在した州だけ**に注入され、昇格植民地・建国州は代替を持たなかった
        //   （実測＝E-13 で黄鉄鉱法を持たない州が硫黄34倍のルブラン法を回し、化学薬品の平均価格が16倍・中央値は2倍）。
        //   ★汽船(85)・機関車(88)の onset（リ5・SteamTransportOnset）も 2026-09-27 に AddAlternateRecipes へ移した（同じ穴＝植民地格の州は1783年から無ゲートだった）。
        // 倍率排除⑧：各popの中核民族割合を国の初期統一度で初期化（以後 移民[core=0で希釈]/同化/征服で per-pop に変動＝EthnicUnity が加重平均から創発）。
        // ⑭-B（史実検証2026-07-22）：CoreInitByRegionCulture>0 では州の Culture ラベルで分化＝主流文化州は高・少数文化州は低（墺のボヘミア/ハンガリー・
        // 露のポーランドが「どの州か」を持つ＝分離圧力の地理的局在）。加重平均はデータ u0 と州構成のブレンド＝0で国一律u0＝厳密恒等。
        if (World.EthnicEnabled && scenario.PrebuiltRegions is null)
        {
            double u0 = scenario.NationConfig?.EthnicUnity ?? 1.0;
            double g = scenario.Coef.CoreInitByRegionCulture;
            string majority = "";
            if (g > 0)
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
                if (g > 0 && majority.Length > 0 && !string.IsNullOrEmpty(r.Culture))
                    core = u0 + g * ((r.Culture == majority ? 1.0 : 0.0) - u0);
                foreach (var p in r.Pops) p.CoreEthnicShare = core;
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
            // 金本位の志向は AI/プレイヤーが決める（UtilityAgent は信用があれば志向＝史実の規範。NullAgent のテストは志向せず＝従来挙動）。実加入は信用ゲートが決める
        };
        World.Suffrage = World.Policy.Suffrage;       // 法整備の現在値＝初期は政策の初期値（利益集団のゲートで漸進）
        World.Censorship = World.Policy.Censorship;   // 報道法の現在値＝初期値（米蘭＝自由／英＝検閲付／他＝国家報道のみ）
        // P2：登録簿（Crown/Seizure）を初期政体から構築（英CM→Crown=Absolutist・米Rep→None等）。flag=0ではdead write＝挙動恒等。
        CrownParliament.ApplyRegime(World, World.Coef, World.Polity);
        // 政体が含意する参政権・報道自由を初期化時に法へ一回畳み込む（倍率排除②：以後 Freedom/eff＝法律値。毎期の政体→スカラー写像を廃止）。
        if (scenario.PoliticsEnabled) Politics.RaiseFoundingInstitutions(World);
        World.Freedom = scenario.PoliticsEnabled ? Politics.FreedomFromLaws(World.Suffrage, World.Censorship) : scenario.Freedom;   // 倍率排除②：探究の自由＝参政権＋報道法から創発（floor-raise 後に算出）
        World.LaborUnion = World.Policy.LaborUnion;   // 労働組合の現在値＝初期値（1789＝非合法）
        World.WorkHours = World.Policy.WorkHours;     // 労働時間の法定上限＝初期値（1789＝無制限）。以後 Government が政治ロック越しに漸進（A-1#2）
        World.WorkHoursValue = World.WorkHours.Hours();   // 実労働時間の初期値＝法定上限のアンカー（無制限＝14時間）。以後 MonthSettle で連続追従（倍率排除②）
        World.WomenWageRetention = World.WomenPropertyLaw ? 1.0 : 0.0;       // 賃金保持の初期値＝財産権法のアンカー。以後 MonthSettle で連続追従（倍率排除④）
        World.ChildLaborAllowed = World.Policy.ChildLaborAllowed;            // 児童労働法の現在値＝初期値（02断線走査2026-07-28：以後 Government が政治ロック越しに漸進）
        World.ChildLaborLegal = World.ChildLaborAllowed ? 1.0 : 0.0;  // 児童労働の合法度の初期値＝児童労働法のアンカー。以後 MonthSettle で連続追従（倍率排除④）
        World.CornLaws = World.Policy.CornLaws;             // 穀物法の現在値＝初期値（農業国＝保護／自由貿易国＝なし）
        World.SerfEmancipation = World.Policy.SerfEmancipation;   // 農奴解放の現在値＝初期値（東欧＝未解放／西欧＝全面）
        World.ImmigrationRestriction = World.Policy.ImmigrationRestriction;   // 移民制限法の現在値＝初期値
        World.MonetaryLaw = scenario.InitialMonetaryLaw;          // 通貨制度の現在値＝初期値（英＝金／他＝銀＝NationConfig の既定・2026-09-30 (119)＝助走は法を動かさない）
        World.Policy.MonetaryTarget = (int)World.MonetaryLaw;    // 目標も現在値に揃える（官僚制度と同じ＝翌期に巻き戻さない）
        // ★(169) 開幕の既知の発見＝データ（NationConfig.KnownDiscoveries）＝1783年より前に史実でその国が使っていた発見。助走の前から持つ（助走の盤面も知った国の動力で均衡する）。
        //   名前が発見の表に無いのはデータの誤り＝構築で止める（黙って捨てない）。
        foreach (var name in scenario.InitialDiscoveries)
        {
            int row = Catalog.DiscoveryRowNamed(name);
            if (row < 0) throw new System.InvalidOperationException($"開幕の既知の発見「{name}」は発見の表に無い（world.json の knownDiscoveries）");
            World.Discovered[row] = true;
        }
        // 軍事費の現在値＝初期目標（兵士の徴募/給与を駆動）。★陸海へ按分（2026-08-04）
        World.Policy.Bureaucracy = World.Bureaucracy;   // ★官僚制度の目標も初期値に揃える（Government が翌期に巻き戻さない・2026-08-04）
        World.SetMilitarySpending(PoliticalQuantities.Clamp01(World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget),
                                  World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget > 1e-9
                                      ? World.Policy.NavyUpkeepTarget / (World.Policy.ArmyUpkeepTarget + World.Policy.NavyUpkeepTarget) : 0.5);
        World.Tariff = scenario.InitialTariff;                          // 関税の現在値＝初期値（日本＝鎖国。開国まで世界市場から孤立）
        World.InterestRate = scenario.InitialInterestRate;
        // P6 乾隆帝の遺産（2026-07-17）：非特権大衆の初期闘争度（清＝白蓮教乱前夜の慢性的混乱）。既定0=不走行＝恒等
        if (scenario.InitialMassMilitancy > 0)
            foreach (var rg in World.Regions) foreach (var p in rg.Pops)
                if (Politics.IsMassPop(p)) p.Militancy = scenario.InitialMassMilitancy;
        // ★全ての世界に銀行（2026-09-29 段2-2・作者裁定 Q11＝旧 Scenario.CreditEnabled を削除）。家計の与信は POP の債務の和から始まる
        //   （建国＝親から運んだ州の POP の輸入の為替手形・Territory.TransferRegion が親の銀行から外した分＝2026-09-29 S3b。開幕は0）。
        double popDebt = 0; foreach (var rg in World.Regions) foreach (var p in rg.Pops) popDebt += p.Debt;
        World.Bank = new Bank { Gold = World.TotalMoney, Loans = 0, Confidence = 1.0, HouseholdLoans = popDebt };

        Fiscal.SeedBondHoldings(World);   // ★国債の保有台帳（2026-09-21）：初期国債（仏）の持ち主を購入と同じ規則で起票（金は動かない・国内債0の国は何もしない）
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
        _invariants.Add(new BondLedger());   // ★国債の保有台帳（2026-09-21）：Σ保有＝国内元本（状態なし）
    }

    // ◆玩具の世界ここから（2026-09-30 段0-1）：下の単一州の最小シナリオは Catalog を使わず、財 id は各シナリオの中だけの番号（Catalog の財 id ではない）＝番人「数値の財 id を書かない」の対象外。
    private static List<Region> BuildSelfSufficientFarm()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var farmer = new Pop { ClassId = 0, ClassName = "自作農", IsUrban = false, Size = 1000, Savings = 1_000_000, Satisfaction = 1.0 };
        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };
        var market = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        return [new Region { Id = 0, Name = "本土", IsPort = true, Goods = [food], Pops = [farmer], Producers = [rgo], Markets = [market] }];   // 港ゲート後：交易デモは海上交易できる沿岸州（IsPort）。較正はAgrarianToUrban別シナリオなので無影響
    }

    private static List<Region> BuildAgrarianToUrban()
    {
        var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
        var manuf = new Good { Id = 1, Name = "工業品", Tier = GoodTier.Daily, BaseNeed = 0.5, RefPrice = 1.0, Elasticity = 0.6 };

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 900, Savings = 900_000, Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = 100, Savings = 100_000, Satisfaction = 1.0 };

        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 720, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };
        var factory = new Producer { GoodId = 1, Productivity = 1.5, Output = 120, MarginalCost = 0.6, OwnerPopIndex = 1, LaborPopIndex = 1, RentShare = 0.0 };

        var foodMkt = new Market { Price = 1.0, Stock = 1200, TargetStock = 1200 };
        var manufMkt = new Market { Price = 1.0, Stock = 600, TargetStock = 600 };

        return [new Region
        {
            Id = 0, Name = "本土",
            Goods = [food, manuf], Pops = [farmer, worker],
            Producers = [rgo, factory], Markets = [foodMkt, manufMkt],
        }];
    }

    /// <summary>段4c：本拠地（active・満杯）＋辺境（locked・豊かな土地）。開発で解錠し移民が流入する（§A-8/§A-9）。</summary>
    private static List<Region> BuildFrontierDevelopment()
    {
        Region MakeRegion(int id, string name, RegionState state, double bonus, double capacity, double farmerSize, double savings)
        {
            var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
            var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = farmerSize, Savings = savings, Satisfaction = 1.0 };
            var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = farmerSize * 0.85 * bonus, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };
            var market = new Market { Price = 1.0, Stock = 1000, TargetStock = 1000 };
            var region = new Region
            {
                Id = id, Name = name, State = state, ProductivityBonus = bonus, ArableLand = capacity,
                Goods = [food], Pops = [farmer], Producers = [rgo], Markets = [market],
            };
        return region;
        }

        var core = MakeRegion(0, "本土", RegionState.Active, bonus: 1.0, capacity: 1000, farmerSize: 1000, savings: 1_000_000);
        var frontier = MakeRegion(1, "辺境", RegionState.Locked, bonus: 2.5, capacity: 1500, farmerSize: 10, savings: 10_000);
        return [core, frontier];
    }

    /// <summary>局地飢饉の検証用：2つの active 州。州0＝食料余剰（農民多）、州1＝食料大幅不足（労働者多・自給僅少）。
    /// 世界市場なし（単一国・TradeEnabled=false）なので州1は輸入できず飢える＝POP の飢えの上乗せ（DemographicStep）で州1だけ人口が減る。</summary>
    private static List<Region> BuildRegionalFamine()
    {
        Region Make(int id, string name, double farmerSize, double workerSize, double foodOutput)
        {
            var food = new Good { Id = 0, Name = "食料", Tier = GoodTier.Staple, BaseNeed = 0.8, RefPrice = 1.0, Elasticity = 0.3 };
            var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = farmerSize, Savings = farmerSize * 1000, Satisfaction = 1.0 };
            var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = workerSize, Savings = workerSize * 1000, Satisfaction = 1.0 };
            var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = foodOutput, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };
            var mkt = new Market { Price = 1.0, Stock = foodOutput, TargetStock = foodOutput };
            return new Region { Id = id, Name = name, State = RegionState.Active, Goods = [food], Pops = [farmer, worker], Producers = [rgo], Markets = [mkt] };
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

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 700, Savings = 700_000, Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工場労働者", IsUrban = true, Size = 250, Savings = 50_000, Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 2, ClassName = "資本家", IsUrban = true, Size = 50, Savings = 50_000, Satisfaction = 1.0 };

        var rgo = new Producer { GoodId = 0, Productivity = 0.85, Output = 560, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };       // 自営農
        var factory = new Producer { GoodId = 1, Productivity = 1.5, Output = 200, MarginalCost = 0.6, OwnerPopIndex = 2, LaborPopIndex = 1, RentShare = factoryRentShare };    // 資本家が取る利潤割合＝搾取度

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

        var farmer = new Pop { ClassId = 0, ClassName = "農民", IsUrban = false, Size = 400, Savings = 400_000, Satisfaction = 1.0 };
        var woodcutter = new Pop { ClassId = 1, ClassName = "木こり", IsUrban = false, Size = 100, Savings = 100_000, Satisfaction = 1.0 };
        var miner = new Pop { ClassId = 2, ClassName = "鉱夫", IsUrban = false, Size = 100, Savings = 100_000, Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 3, ClassName = "工員", IsUrban = true, Size = 350, Savings = 200_000, Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 4, ClassName = "資本家", IsUrban = true, Size = 50, Savings = 100_000, Satisfaction = 1.0 };

        var pFood = new Producer { GoodId = 0, Productivity = 2.2, Output = 800, MarginalCost = 0.5, OwnerPopIndex = 0, LaborPopIndex = 0, RentShare = 0.0 };
        var pWood = new Producer { GoodId = 5, Productivity = 3.0, Output = 200, MarginalCost = 0.5, OwnerPopIndex = 1, LaborPopIndex = 1, RentShare = 0.0 };
        var pOre = new Producer { GoodId = 6, Productivity = 3.0, Output = 200, MarginalCost = 0.5, OwnerPopIndex = 2, LaborPopIndex = 2, RentShare = 0.0 };
        var pLumber = new Producer { GoodId = 2, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = 0.3, Inputs = [new Input(5, 1.0)] };
        var pIron = new Producer { GoodId = 3, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = 0.3, Inputs = [new Input(6, 1.0)] };
        var pSteel = new Producer { GoodId = 4, Productivity = 1.5, Output = 200, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = 0.3, Inputs = [new Input(3, 1.0)] };
        var pFurn = new Producer { GoodId = 1, Productivity = 1.5, Output = 400, MarginalCost = 0.3, OwnerPopIndex = 4, LaborPopIndex = 3, RentShare = 0.3, Inputs = [new Input(2, 0.5), new Input(4, 0.5)] };

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
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, Spoilage = Catalog.SpoilageOf(d.Id), NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            if (d.IsColonial) continue;   // 植民地素材は国内生産者なし（輸入のみ）
            if (d.Tier == GoodTier.Service) continue;   // 役務（労働力・高度労働力・農業労働力）の生産者は下で紐づけて追加
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
                RentShare = isFactory ? 1.0 : 0.3,    // 自作農の自己地代（土地税の課税ベース。所有者＝労働者なので同じ懐）
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }

        var goodsList = new List<Good>();
        foreach (var g in goodsById) if (g is not null) goodsList.Add(g);
        // ★★全順序（2026-08-17）：Tier が同値のときは id で決める。List.Sort は introsort＝**不安定**なので、
        //   同 Tier 内の相対順序が配列長に依存する＝**財を1つ足すだけで既存財の並びが総入れ替えになる**（実測：
        //   高等教育1財の追加で勝者が英→米・最大DD 35.4%→94.7%）。この並びは MarketFlow の価格更新順と
        //   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。
        //   id を第2キーに置くと全順序になり、以後は新財を足しても既存財の順序が不変になる。
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順

        var laborer = new Pop { ClassId = 0, ClassName = "一次労働者", IsUrban = false, Size = 500, Savings = 500_000, Satisfaction = 1.0 };
        var worker = new Pop { ClassId = 1, ClassName = "工員", IsUrban = true, Size = 450, Savings = 450_000, Satisfaction = 1.0 };
        var capitalist = new Pop { ClassId = 2, ClassName = "資本家", IsUrban = true, Size = 50, Savings = 100_000, Satisfaction = 1.0 };
        // 労働力：工員が算出し工場が買う（賃金＝労働市場の価格）。生産性は工場と同じスケール＝供給が工場群と釣り合う。
        producers.Add(new Producer { GoodId = Catalog.LaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = worker.Size * 2.5, MarginalCost = 0.35, LaborPopIndex = 1, OwnerPopIndex = 1, RentShare = 0.0 });
        // 高度労働力：技師の居ない簡易世界＝工員が兼ねる（小さな供給）。石油火力も工員で。
        producers.Add(new Producer { GoodId = Catalog.AdvLaborId, Productivity = 0.5, Output = worker.Size * 0.2, MarginalCost = 0.5, LaborPopIndex = 1, OwnerPopIndex = 1, RentShare = 0.0 });
        // 農業労働力：一次労働者が算出し自家農家（RGO）が買う。
        producers.Add(new Producer { GoodId = Catalog.FarmLaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = laborer.Size * 0.5, MarginalCost = 0.3, LaborPopIndex = 0, OwnerPopIndex = 0, RentShare = 0.0 });
        // A-中1（2026-07-23）：石油火力も燃料主導の比率へ（発電機0.5→0.15＝石炭火力と同一の資本費。原油0.6＝石炭0.9のエネルギー等価1/1.5）。
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "石油火力", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.498), new Input(Catalog.CrudeOilId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // A-中1（2026-07-23）：水力発電＝燃料ゼロ・資本費重（発電機0.40＝火力の2.7倍＝ダム/導水路/大型水車の資本）。石炭が高い州でのみ火力に勝つ
        //（燃料の地理が電源を分ける＝スイス/伊/ノルウェー型の水力国が価格から創発）。解禁＝発見の表の水力発電（1880年代・2026-10-01 段2A）。
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "水力発電", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.647), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 魚の缶詰（缶詰の第2レシピ）：漁獲を缶詰める＝漁獲に第2の消費先（従来は干物のみ）。市場が安い方を選ぶ。
        producers.Add(new Producer { GoodId = Catalog.CannedFoodId, RecipeName = "魚の缶詰", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 1, OwnerPopIndex = 2, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.FishId, 0.6), new Input(Catalog.IronId, 0.3), new Input(Catalog.SaltId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CannedFoodId))] });

        AddAlternateRecipes(producers, new Coefficients());   // ★代替レシピ（2026-09-06）：フルチェーンも同じ1回（旧＝コンストラクタの走査が拾っていた＝生産者126の内訳）
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
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, Spoilage = Catalog.SpoilageOf(d.Id), NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            markets[d.Id] = new Market { Price = refP, Stock = 200, TargetStock = 200 };
            if (d.IsColonial) continue;   // 植民地素材は国内生産者なし（輸入のみ・植民地と制海で供給）
            if (d.Tier == GoodTier.Service) continue;   // 役務は AddServiceProducers が担い手popに紐づけて追加
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
                RentShare = isFactory ? 1.0 : 0.5,     // 農地の残余の半分＝封建貢租・残り半分は耕す農民へ
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }

        var goodsList = new List<Good>(goodsById);
        // ★★全順序（2026-08-17）：Tier が同値のときは id で決める。List.Sort は introsort＝**不安定**なので、
        //   同 Tier 内の相対順序が配列長に依存する＝**財を1つ足すだけで既存財の並びが総入れ替えになる**（実測：
        //   高等教育1財の追加で勝者が英→米・最大DD 35.4%→94.7%）。この並びは MarketFlow の価格更新順と
        //   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。
        //   id を第2キーに置くと全順序になり、以後は新財を足しても既存財の順序が不変になる。
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順

        // 身分・階級（代表スケール 1単位＝1000人・計≈28,000＝人口2800万）。識字は階級で大差。
        var nobility     = new Pop { ClassId = 0, ClassName = "貴族",       IsUrban = false, Size = 300,   Savings = 900_000,   Satisfaction = 1.0, Literacy = 0.95, Closed = true };       // 世襲＝移民しない
        var clergy       = new Pop { ClassId = 6, ClassName = "聖職者",     IsUrban = false, Size = 200,   Savings = 600_000,   Satisfaction = 1.0, Literacy = 0.92, Closed = true };       // 宗教学校の教育を算出（世俗化で没落）
        var peasant      = new Pop { ClassId = 1, ClassName = "農民",       IsUrban = false, Size = 20000, Savings = 4_000_000, Satisfaction = 1.0, Literacy = 0.15, Subsistence = 0.7 };   // 食料の多くを自給
        var worker       = new Pop { ClassId = 2, ClassName = "工場労働者", IsUrban = true,  Size = 4200,  Savings = 1_260_000, Satisfaction = 1.0, Literacy = 0.40 };   // 労働力を算出し工場に売る
        var bourgeois    = new Pop { ClassId = 3, ClassName = "資本家",     IsUrban = true,  Size = 2500,  Savings = 8_000_000, Satisfaction = 1.0, Literacy = 0.80, IsModernElite = true };   // 深い連鎖の投資元手（§8較正までのバッファ）
        var engineer     = new Pop { ClassId = 4, ClassName = "技師",       IsUrban = true,  Size = 520,   Savings = 416_000,   Satisfaction = 1.0, Literacy = 0.85, IsModernElite = true };  // 工場の生産性（近代エリート）
        var teacher      = new Pop { ClassId = 7, ClassName = "教師",       IsUrban = true,  Size = 112,   Savings = 89_600,    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 公教育の教育を算出
        var bureaucrat   = new Pop { ClassId = 8, ClassName = "官僚",       IsUrban = true,  Size = 98,    Savings = 78_400,    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 行政を算出
        var doctor       = new Pop { ClassId = 9, ClassName = "医者",       IsUrban = true,  Size = 70,    Savings = 56_000,    Satisfaction = 1.0, Literacy = 0.92, IsModernElite = true };  // 医療を算出
        var merchant     = new Pop { ClassId = 10, ClassName = "商人",      IsUrban = true,  Size = 500,   Savings = 750_000,   Satisfaction = 1.0, Literacy = 0.70, IsModernElite = true };  // 流通マージン＋貿易の仲立ち
        var artisan      = new Pop { ClassId = 11, ClassName = "職人", IsUrban = true,  Size = 1500,  Savings = 1_050_000, Satisfaction = 1.0, Literacy = 0.45 };  // 手工業の自営（ギルド）。※この試験世界は銀行なし＋工場が成熟済＝初日から競争に負けるため、世紀を生き延びる蓄え（700/人）を持たせる

        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, clergy, teacher, bureaucrat, doctor, merchant, artisan };
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 6, bureaucratIdx: 7, doctorIdx: 8);
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, new Coefficients());   // 高度労働力（技師）＋石油火力
        AddArtisanProducers(producers, artisanIdx: pops.Count - 1);   // 職人の工房（工場との並存レシピ）
        AddAlternateRecipes(producers, new Coefficients());   // ★代替レシピ（2026-09-06）：最小ビルダーも同じ1回（旧＝コンストラクタの走査が拾っていた）
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
        // ★辺境の廃止（2026-08-26・作者裁定「Locked 州は外科的すぎるので廃止したい」）：
        //   旧＝world.json の frontiers[] を BuildFrontier が **State=Locked の第3スキーマ**として作っていた（POPのindex割り当ても州と別）。
        //   5件（蝦夷地・西部・ウラル・シベリア・ウラジオストク）は普通の州へ畳んだ＝州は1つのスキーマだけになった。
        WirePortDistances(regions);
        // ★初期軍需の工廠備蓄（初期値調整2026-08-22・<see cref="InitialArmsIndustryGoods"/> の注記の続き）：
        //   初期軍隊の平時消費（StepMonth の MilitaryUpkeepScale×兵士 と同じ式）に対し、市場在庫が一律200では大軍の国は数日分
        //   （清＝小銃200は6日分）＝助走中に価格が参照の3〜7倍へ発散し、国家調達の価格ゲート（StateProcurePriceGate・半減点2.5×）が
        //   閉じて需要ごと消える→播種した軍需産業も労働を取れず（prodW=LastDemand×Scale）「火器0%」に固着する（milstart実測）。
        //   軍需調達は首都市場のみ（StepMonth の capShare）＝首都に平時消費 InitialArsenalMonths ヶ月分の備蓄を置く＝
        //   助走（StartWarmupQuarters）の間、価格を参照帯に保ち、軍需産業が需要に立ち上がる時間を買う。
        //   対象は軍需の最終財のみ（火砲81/小銃82/弾薬83/軍艦86＝InitialArmsIndustryGoods との交差）。
        //   鋼など汎用中間財の在庫は触らない（「AI国庫×初期市場在庫」の開幕工業化バースト＝indprobe2026-07-27の既知リスク）。
        //   汽船85（技術ゲート＝1783年に作れない）・馬127（畜産＝食料源と競合する既知の感度）・自動車132は置かない。
        //   ⚠在庫だけを引き上げる操作が財収支恒等式（Invariants.GoodsBalance）を破らないのは、検査が StepMonth の**末尾**で走り、
        //   その前に EconomyStep が期首で PrevStock=Stock を取り直すため（旧来の一律200も同じ構造で成立している）。
        if (coef.InitialArsenalMonths > 0)
        {
            double natSoldiers = 0;
            foreach (var r in regions) foreach (var pp in r.Pops) if (pp.IsSoldier) natSoldiers += pp.Size;
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

    /// <summary>内陸州の最寄り港距離を設定（監査2026-07-05・trade-capacity-even-share-not-network のデータ側配線）。
    /// 従来 NearestPortDistanceKm はどこからも代入されず、InlandPortDistanceDecay（dormant）を有効化しても減衰式が不発だった（#17較正の阻害）。
    /// 州都市座標（Climate）から同国の最寄り港州への大圏距離を設定。座標未登録・港なし世界は 0＝減衰なし（従来挙動）。係数0の既定では挙動不変（較正中立）。
    ///
    /// ★rewire＝再配線モード（W2・是正1/是正2案B・2026-08-16・doc 20260812_港の配線を領土に追随させる）。
    /// 領土が動いた国（割譲・分離独立・植民地州昇格・辺境解錠/州凍結）の配線を張り直す。構築時（rewire=false・既定）との差は2点だけ：
    ///   ①候補港を State==Active に限る（割譲で凍結中の州・Locked辺境港を海の口にしない。「同一World」は引数 regions が
    ///     1国のリスト（World.Regions）であることで構造的に満たす＝毎期の所有辞書引き（却下案A）は要らない）。
    ///   ②先に旧配線を消す＝港州0になった国は NearestPortName="" が残り、BilateralMarket が通過港（TransitPort*）分岐へ落ちる
    ///    （doc §1-4「S1是正と通過港配布は同時でないと交易から完全脱落」の受け皿。張り直しは MultiNationSim.RewirePorts が毎四半期強制する）。
    ///   SeaBasin は消さない＝港州0の国の内陸州は旧海盆を保持（未決9a=保守側：封鎖・冬季閉鎖の帯の連続性を優先。港が戻れば次の再配線で追随）。
    ///   呼び出しは PortRewireEnabled>0.5 の内側のみ（**点火1.0・2026-08-16**。0では本モードに1命令も入らない＝構築時経路は従来とビット一致）。
    ///   決定論＝盤面（州集合と座標）だけから決まる・RNG不使用・同率は従来どおり先着（州リスト順＝決定論）。</summary>
    internal static void WirePortDistances(List<Region> regions, bool rewire = false)
    {
        if (rewire)
            foreach (var r in regions)
                if (!r.IsPort) { r.NearestPortName = ""; r.NearestPortDistanceKm = 0; r.PortLandKm = 0; }
        var ports = new List<(double Lat, double Lon, string Name)>();
        foreach (var r in regions)
            if (r.IsPort && (!rewire || r.State == RegionState.Active)) { var pc = Climate.CityCoord(r.Name); if (pc.Lat != 0 || pc.Lon != 0) { ports.Add((pc.Lat, pc.Lon, r.Name)); r.SeaBasin = SeaGraph.BasinAt(pc.Lat, pc.Lon); } }
        if (ports.Count == 0) return;
        foreach (var r in regions)
        {
            if (r.IsPort) continue;
            var cc = Climate.CityCoord(r.Name);
            if (cc.Lat == 0 && cc.Lon == 0) continue;
            double best = double.MaxValue; string bestName = "";
            foreach (var p in ports) { double d = Geo.DistanceKm(cc.Lat, cc.Lon, p.Lat, p.Lon); if (d < best) { best = d; bestName = p.Name; } }
            r.NearestPortDistanceKm = best;
            r.NearestPortName = bestName;   // 03 §12-3：どの港かを記憶（キエフ/ハリコフ→ロストフ＝穀物動線がデータを足さずに出る）
            // 03手順4A：陸脚＝陸グラフの最短路（山脈迂回込み）。孤立成分（別陸塊等）は大圏で退避＝0を残さない（0=港州の意味）
            var bp = Climate.CityCoord(bestName);
            double lg = PathGraph.LandDistanceKm(cc.Lat, cc.Lon, bp.Lat, bp.Lon);
            r.PortLandKm = double.IsPositiveInfinity(lg) ? best : lg;
            r.SeaBasin = SeaGraph.BasinAt(bp.Lat, bp.Lon);   // 03手順5：内陸州＝最寄り港の海盆を継承（キエフ→ロストフ＝黒海勢が自動で出る・§12-3）
        }
    }

    /// <summary>1つの州（代表都市）を生成。国全体の各階級を Share で按分し、独立したカタログ全財の経済を持たせる（州ごとに世界市場と交易）。</summary>
    // 港州（§5.1拡張）：世界市場と直接交易できる代表的な港町。内陸州は鉄道で港経由（容量ゲート）。
    // 02断線走査（2026-07-28）：旧12港のPortCities backstopを削除＝全12件がworld.jsonでcoastal:true と全数確認済み（恒真的に冗長・coastal編集を殺す罠だった）。


    // 州ごとの資源（NationConfig の Provinces で指定）：列挙財を増産（地理的な得意＝輸出）、非列挙RGOは減産（要輸入＝交易を生む）。
    // 倍率排除⑧：列挙財の賦存量を good の地理的性質から導く（uniform な手置き×3.5 を廃止＝賦存の大小が一次データから出る）。
    // 鉱物・石油（IsMineral）は点在鉱床＝産地では集中的に豊富＝高い賦存（4.0・ルール炭/ペンシルベニア炭/アナトリアのクロム/ガリツィアの石油…）。
    // 農産・繊維・加工の得意は面的＝中庸（3.5）。鉱物が偏在し産地が圧倒的に強い、という史実の地理がここから創発する。
    private const double ResourceScarcity = 0.4;   // 非列挙RGOの産出（賦存に乏しい＝最低限の自給はあるが輸入依存。ゼロにしない＝封鎖即飢饉を回避）
    internal static double ResourceEndowment(int good) => Catalog.IsMineral(good) ? 4.0 : 3.5;   // internal＝EconomyStep の希少鉱物天井（E-15 手順3）が読む

    // ★財の地理ゲート（2026-08-13・doc 20260812_財の3分類と地理ゲート §4-1／作者裁定⑥）。
    //
    // 旧＝非産地でも 0.4 で産出する＝**どの国も硫黄を自給できる**。史実の希少鉱物（硫黄・硝石・クロム・錫・金・銀）は
    // 産地を押さえた国が世界を人質に取れる財で、それが本作では起きない。
    // 新＝希少鉱物は**産地でしか出ない**（非産地は厳密に0）。ただし総量が減ると火薬と肥料が細って
    // パンの値段（本作の背骨）に直撃するので、**世界総産出が不変になるよう産地の賦存を引き上げる**。
    //
    //   E = ( k×4.0 + (N−k)×0.4 ) / k        k＝産地の数・N＝産出しうる地点の総数
    //
    // ★k は world.json から数える＝**産地を足せば E は自動で下がる**（手で書かない＝doc §4-1 の要求）。
    // ★N は**州のみ**（植民地は生産者を1つも持たないことを2026-08-13に確認済み）。
    // 硫黄は k=1（パレルモのみ。北九州は後期産地＝k に数えない・蝦夷地の抽象標的は 2026-09-28 に削除）→ E = 4.0 + 183×0.4 = **77.2**＝現行最大の約20倍。
    // ScarceMineralGate=0 で E を返さない＝厳密恒等。
    private static int[]? _scarceCounts;   // 財ID→産地の州数（world.json から1回だけ数える・決定論）
    private static int _scarceProvinceTotal;
    private static int[]? _scarceCountsWithColonies;   // ★ScarceCountsIncludeColonies（2026-09-06）：植民地の固有資源も数えた版
    private static int _scarceTotalWithColonies;

    private static void EnsureScarceCounts(bool withColonies)
    {
        if ((withColonies ? _scarceCountsWithColonies : _scarceCounts) is not null) return;
        var counts = new int[Catalog.Goods.Length];
        int total = 0;
        foreach (var n in WorldData.Nations())
        {
            foreach (var p in n.Provinces)
            {
                total++;
                foreach (var g in p.Resources) if (g >= 0 && g < counts.Length) counts[g]++;
            }
            // ★植民地（ColoniesAreRegions で実体の生産者を持ち、Simulation:1242 で同じ ScarceEndowment を受ける）を数えないと
            //   世界総産出が保存しない（分子と分母の非対称）。器 ScarceCountsIncludeColonies>0 でだけ数える（0＝従来）。
            if (withColonies)
                foreach (var col in n.InitialColonies)
                {
                    total++;
                    foreach (var g in col.Resources) if (g >= 0 && g < counts.Length) counts[g]++;
                }
        }
        if (withColonies) { _scarceTotalWithColonies = total; _scarceCountsWithColonies = counts; }
        else { _scarceProvinceTotal = total; _scarceCounts = counts; }   // ★最後に代入＝他スレッドが半端な配列を見ない（バッチは並列実行される）
    }

    /// <summary>希少鉱物の産地での賦存量（総量保存で逆算）。対象外・未点火なら通常の賦存を返す。</summary>
    internal static double ScarceEndowment(int good, Coefficients c, bool sharedWorld)
    {
        // ★密閉世界（単国シム＝較正・テスト・sharedWorld=false）では産地ゲートを効かせない（2026-09-06）：運ぶ相手がいない世界で産地を絞るのは
        //   「地理」でなく「欠乏の注入」＝実測で硫黄0→化学→紙→教育→行政力の鎖が絶えた（TaxTests/MobilityTests）。史実の英は硫黄をシチリアから運んだ。
        if (!sharedWorld || !(c.ScarceMineralGate > 0) || !Catalog.IsScarceMineral(good, c)) return ResourceEndowment(good);
        bool withCol = c.ScarceCountsIncludeColonies > 0 && c.ColoniesAreRegions > 0;
        EnsureScarceCounts(withCol);
        int k = (withCol ? _scarceCountsWithColonies : _scarceCounts)![good];
        int total = withCol ? _scarceTotalWithColonies : _scarceProvinceTotal;
        if (k <= 0) return ResourceEndowment(good);   // 産地が1つも無い財は触らない（fail-open）
        return (k * ResourceEndowment(good) + (total - k) * ResourceScarcity) / k;
    }

    /// <summary>賦存倍率を生産者へ当てる唯一の入口（播種の6箇所が全部ここを通る）。
    /// ★器 <see cref="Coefficients.EndowmentCostFollowsAbundance"/>／<see cref="Coefficients.EndowmentCostFollowsScarcity"/>
    /// （2026-09-08・作者裁定「同一生産コスト辺りの産出能力にするのが正しい」・既定0＝**休眠**）：
    /// 旧＝倍率は Productivity＝Capacity にしか乗らず、自己費用（Producer.MarginalCost）は全州で同じ
    /// ＝硫黄はパレルモも非産地も単位費用 0.540＝比較優位がデータ上ゼロ。
    /// 新＝賦存の高い州は同じ費用でより多く採れる＝<c>費用 ＝ 自己費用÷賦存倍率 ＋ 投入費用</c>。
    /// 割り算の形は <see cref="EconomyStep.MineralSelfCost"/>（鉱脈の品位）と同じ物理＝あちらは月ごとに動く鉱脈の質を毎月割り、
    /// こちらは動かない地理なので播種で1回だけ MarginalCost に畳む（MarginalCost は播種後どこも書き換えない＝毎月割るのと厳密同値）。
    /// 両方点火すれば mc の自己費用は 賦存倍率×鉱脈倍率 で割られる＝積として重なる。
    /// ⚠豊富側（倍率>1）と希少側（倍率<1＝非産地の 0.4）は別ゲート＝非対称を選べる：希少側は費用を上げる方向で、
    ///   効きが強すぎると石炭・鉄の非産地が死ぬ（2026-09-07 の MineralCostFollowsGrade 点火で人口21.8→7.1 の前例）。
    /// ゲート0では Math.Pow を1度も通らない＝×1.0 すら掛けない＝厳密恒等。倍率が NaN・非正なら費用を触らない（否定形の番人）。</summary>
    internal static void ApplyEndowment(Producer pr, double endowment, Coefficients c)
    {
        pr.Productivity *= endowment;
        double gate = endowment > 1.0 ? c.EndowmentCostFollowsAbundance : endowment < 1.0 ? c.EndowmentCostFollowsScarcity : 0.0;
        if (!(gate > 0) || !(endowment > 0)) return;
        pr.MarginalCost /= System.Math.Pow(endowment, gate);
    }

    /// <summary>★産地の発見の行（2026-10-01 (148)・<see cref="Catalog.Sites"/>）をこの場所・この財について引く（州の構築と植民地の昇格の2か所が呼ぶ＝同じ規則）。
    /// ⚠共有世界だけ（密閉世界＝単国の較正とテストでは産地を絞らない＝旧 LateResources の枝と同じ・ScarceEndowment と同じ理由）。
    /// 銀の行は <see cref="Coefficients.SilverRushEnabled"/>＝0 の間は置かない（銀の平価の自己参照を断つ回の後に点火＝検討 Q13 (d)）。</summary>
    internal static int SiteRowFor(string place, int good, Coefficients c, bool sharedWorld)
    {
        if (!sharedWorld) return -1;
        int row = Catalog.SiteRowOf(place, good);
        return row >= 0 && good == Catalog.SilverId && !(c.SilverRushEnabled > 0) ? -1 : row;
    }
    private const double LandHeadroom = 1.8;         // 本土州の耕地賦存量＝初期農民×これ。初期は余裕（landFactor=1＝既存収支を保持）、流入で農村人口がこれを超えると収穫逓減が効く
    // 主食の自給免除は気候帯で切替（Climate.StapleSelfSufficient）：麦・野菜＝普遍／米＝モンスーン・熱帯／ジャガイモ＝冷涼帯／トウモロコシ＝新大陸（2026-07-05・旧StapleRaw=[15,102,108]の全州一律を地理化）

    /// <summary>播種時に読む政策の既定値。⚠数字をここに書き写さない＝<see cref="NationPolicy"/> の初期化子が唯一の出所。
    /// 初期兵は「平時の均衡」で播くので軍事費の既定が要る（BuildProvince は World より前に走るので World.MilitarySpending は無い）。</summary>
    private static readonly NationPolicy PolicyDefaults = new();

    // 州の関税利害（分離独立の駆動）：工業州（鉄・石炭・鉄鋼・機械等を産む）は保護(+1)・農業/一次産品輸出州は自由貿易(−1)。＝史実の南北対立軸。
    // 初期地代シードの実勢補正（2026-08-04）：素の rentShare×Output×RefPrice は生産性乗数・実価格の分だけ実勢を下回る
    // （frdbg実測＝シードで旧支配層25%・実流の翌四半期63%）。初月のみの近似＝実流が翌月に上書きするので精度はこの桁で足りる。
    private const double InitialRentSeedScale = 3.0;

    // ★初期軍需産業（2026-08-22・作者指示「初期軍隊と実際の生産が噛み合っていない（火器0%・軍艦1083%）＝初期値を調整」）。
    // 初期軍隊は平時の均衡で播く（BuildProvince の soldier 徴募）のに、軍需財の工場は Scale=0.05（前工業＝「工場はほぼ無い」）で
    // 始まる＝結果（軍隊）だけ置いて支え（工廠・火薬廠・造船所）を置かない不整合（識字と教育供給の不整合と同じ型）。
    // 実測（Runner の milstart・seed1）＝小銃/火砲の価格が助走で参照の5〜25倍へ発散→国家調達の価格ゲート
    // （StateProcurePriceGate）が閉じて充足0%・軍艦は初期在庫200が配給1.0に見せて陸海再配分倍率
    // （ShapeNavalShare/flatShare≈10.8）がそのまま「充足1083%」に露出していた。
    // ★1783年の現実＝マスケットは職人の産物（バーミンガム銃器街・リエージュ）・工廠（ウーリッジ/テュラ/springfield）・
    // 王立造船所（ポーツマス/ブレスト/トゥーロン）は操業中＝職人工房（AddArtisanProducers の Scale=1.0）と同じ扱いで
    // 軍需連鎖を「既に操業している」初期値にする。規模は国ごとに書かない（国名分岐なし）＝労働配分が需要比例
    // （EconomyStep:300 prodW=LastDemand×Scale）なので、大軍の国ほど大きな軍需産業が需要から自然に立つ。
    // 対象＝軍需の最終財（火砲81/小銃82/弾薬83/帆船84/軍艦86）と軍需専用の中間財（火薬38/帆布42）。
    // 馬127は除外（畜産＝食料源と競合する既知の感度・MilitaryRates 0.1で症状の主因でない）。
    // 技術ゲート財（汽船85/装甲艦105等）は対象外＝発見の表に行を持つ並存レシピも触らない（TechGatedScaleZero と整合）。
    // ★備蓄の深さは <see cref="Coefficients.InitialArsenalMonths"/>（0で旧初期値と厳密恒等＝アブレーションの腕を POB_COEF で測れる）。
    //   操業規模の器 InitialArmsIndustryScale は 2026-08-22 の実測で棄却＝2026-09-26 に削除（棚卸し mil-21・dorm-36）。
    internal static readonly int[] InitialArmsIndustryGoods = [Catalog.GunpowderId, Catalog.CanvasId, Catalog.ArtilleryId, Catalog.RifleId, Catalog.AmmunitionId, Catalog.SailingShipId, Catalog.WarshipId];

    private static Region BuildProvince(int id, ProvinceSpec p, NationConfig cfg, Coefficients coef, bool sharedWorld = false)
    {
        var (city, share, resources, culture, religion, _, _, _, _, _, _) = p;   // Coastal/HeldSince/Malarial/ArableLand/IndShare/Rule は p.* で直接参照（★2026-10-01 (148)：LateResources を削除＝産地の発見の表 Catalog.Sites へ）
        var (goods, markets, producers) = CatalogEconomy(Math.Min(cfg.FarmLordShare, 1.0 - coef.SerfCashShareFloor), coef);   // 農奴の市場参加床（既定0=恒等）：オブローク＝農奴も市場で現金を得た＝取り分100%の現金遮断を緩める（辺境は対象外＝入植小農）
        // 気候ゲート財（茶葉/ゴム/香辛料…旧colonial）は賦存州（Resources に列挙）のみ産する＝非賦存州の生産者を除去（英は茶を産めない＝気候の現実）。
        // 列挙する州（中国の茶州等）は下の賦存ブースト×3.5で産み、加工して世界へ売る＝供給の偏在が地理から創発する（287州世界）。
        producers.RemoveAll(pr => Catalog.IsColonial(pr.GoodId) && System.Array.IndexOf(resources, pr.GoodId) < 0 && SiteRowFor(city, pr.GoodId, coef, sharedWorld) < 0);
        // 州の気候帯（主食の地理化・2026-07-05）：代表都市の座標で判定。未登録都市・単一州世界は首都座標へフォールバック（テスト世界の(0,0)＝従来挙動を保存）。
        var (plat, plon) = Climate.CityCoord(city);
        if (plat == 0 && plon == 0) { plat = cfg.Latitude; plon = cfg.Longitude; }
        // 米の気候ゲート：モンスーン・熱帯の適地外の州は、賦存列挙がない限り生産者ごと除去（欧州の全州が米を自給する非現実の排除。茶葉等と同型）。
        producers.RemoveAll(pr => pr.GoodId == Catalog.RiceId && !Climate.RiceSuitable(plat, plon) && System.Array.IndexOf(resources, pr.GoodId) < 0);
        // A-中5（史実検証2026-07-22）：綿花の気候ゲート（米ゲートと同型）。適地外（|緯度|>37）は賦存列挙がない限り生産者ごと除去＝
        // 英仏独露の本国綿花自給を排除（綿布は輸入原綿で織る＝ランカシャーの現実。賦存21州＝米南部・インド・エジプト等のみ産する）。
        producers.RemoveAll(pr => pr.GoodId == Catalog.CottonId && !Climate.CottonSuitable(plat) && System.Array.IndexOf(resources, pr.GoodId) < 0);
        // 州の資源プロファイル：列挙財は増産（×3.5）、非列挙のRGOは減産（×0.4・要輸入）。工場財は地理に縛られないので減産しない。
        // 主食は気候帯の自給セット（Climate.StapleSelfSufficient）に該当すれば非列挙でも減産しない（自給ぶんを作る＝封鎖で即飢饉にしない）。
        foreach (var pr in producers)
        {
            // ★産地の発見（2026-10-01 (148)・Catalog.Sites＝大鉱床と植林）：その場所の生産者は通常の賦存で作り、見つかるまで門0（Discovery.Gate＝Producer.SiteFound）。
            //   開始時の賦存（resources）に載る財（カリフォルニアの金・アジアのキナ）は下の賦存の枝をそのまま通す（地代の取り分も同じ）＝門が付くだけ。
            //   載らない財（西部の硫黄・北九州の硫黄・豪州と北ユーコンの金）は通常の賦存（総量保存の k に数えない＝新しい供給＝旧 LateResources の枝と同じ）。
            int site = pr.IsRgo ? SiteRowFor(city, pr.GoodId, coef, sharedWorld) : -1;
            if (site >= 0 && System.Array.IndexOf(resources, pr.GoodId) < 0)
            {
                ApplyEndowment(pr, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, coef);
                pr.SiteRow = site;
            }
            else if (System.Array.IndexOf(resources, pr.GoodId) >= 0)
            {
                pr.SiteRow = site;
                ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, sharedWorld) * coef.ResourceEndowmentScale, coef);   // 賦存量＝good の地理的性質（鉱物=集中4.0／他=中庸3.5）。希少鉱物は総量保存で逆算した高い賦存。★Scale=1.0（既定）でビット恒等＝診断用レバー
                // D6（プランテーションdoc・2026-07-30）：**地代取り分を州別・財別へ**。旧＝`RentShare = max(0.3, farmLordShare)` の国家スカラー1個で、
                // 同じ関数の中で Productivity だけが州の resources で財別に分化していた（器はあるのに使っていなかった）。
                // 賦存財の RGO＝その州の輸出向け一次産品＝**大農園/鉱山の経営**なので、残余は経営者（地主階級）へ厚く落ちる。
                // ★実測の矛盾がこれで解ける：チャールストン（綿花・葉煙草の賦存州）は米の farmLordShare=0 ゆえ
                //   「奴隷プランテーションが自作農の家族農場として計上される」状態だった。
                // ★思想②に抵触しない＝world.json の resources は一次データ（国ごとの倍率ではない）。PlantationRentShare=0 で旧値＝厳密恒等。
                if (pr.IsRgo && coef.PlantationRentShare > 0)
                {
                    double plantation = coef.PlantationRentShare;
                    if (plantation > pr.RentShare) { pr.RentShare = plantation; pr.OwnerPopIndex = 0; }   // 所有は地主階級（貴族）＝経営者のいる大農園
                }
            }
            // ★希少鉱物は**非産地では厳密に0**（2026-08-13・裁定⑥）。総量は産地の賦存を引き上げて保つ（ScarceEndowment）。
            else if (sharedWorld && coef.ScarceMineralGate > 0 && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;   // ★密閉世界（単国シム）では非産地を0にしない（2026-09-06・ScarceEndowment と同じ理由）
            else if (pr.IsRgo && !Climate.StapleSelfSufficient(pr.GoodId, plat, plon)) ApplyEndowment(pr, ResourceScarcity * coef.ResourceScarcityScale, coef);   // ★Scale=1.0（既定）でビット恒等＝診断用レバー
        }

        // ★貴族・聖職の初期資産＝土地（2026-08-04・作者指示「現在の問題を全て解決して」の統一根）。
        //   ── 実測（blocprobe・1783年）────────────────────────────────────────────
        //   旧＝貴族3000/人 vs 資本家3200/人（ほぼ同額）。貴族は頭数が少ない（英108K vs 資本家2,100K）ので
        //   旧支配層の政治力＝富シェア×参政権は **英3%・仏21%・普26%** になっていた。
        //   1783年の現実は逆＝土地が国富の過半で、それを握るのが貴族（英の改革法1832は地主支配への闘争そのもの・
        //   ユンカーは1918年まで支配・仏は革命前夜）。
        //   ── 帰結の連鎖（これが「制度の階段が立たない」の統一根）───────────────
        //   旧支配層に富が無い → DoctrineFrom が近代化派に倒れる → 法の門番（旧 ModernizationLawsGated＝伝統教義のみ発動・2026-09-26 に削除＝回5-1 項目19）が
        //   全員素通り → 教育/官僚/検閲が全国1783年に通る（lawtiming実測）→ 識字・技術・吸収能力が収束 → 工業化が全球収束。
        //   ⚠ClosedWealthMultiple=1.0 で旧と厳密恒等（×1.0はIEEE754で恒等）。
        var nobility     = MkPop(0, "貴族",       false, cfg.Nobility * (1 - cfg.ClergyFrac) * share, cfg.NobLit,  3000 * coef.ClosedWealthMultiple, closed: true);
        var clergy       = MkPop(6, "聖職者",     false, cfg.Nobility * cfg.ClergyFrac * share,       cfg.ClergyLit, 3000 * coef.ClosedWealthMultiple, closed: true);   // 宗教学校の教育を算出（世俗化＝公教育で収入を失い没落）。教会領も土地＝貴族と同じ倍率
        var peasant      = MkPop(1, "農民",       false, cfg.Peasant * share,      cfg.PeasLit, 200,  subs: cfg.PeasantSubsistence);
        var worker       = MkPop(2, "工場労働者", true,  cfg.Worker * share,       cfg.WorkLit, 300);
        var bourgeois    = MkPop(3, "資本家",     true,  cfg.Bourgeois * share,    cfg.BourgLit, 3200, modern: true);
        var engineer     = MkPop(4, "技師",       true,  cfg.Professional * (1 - cfg.IntelligentsiaFrac) * share, cfg.ProfLit, 800, modern: true);
        // 旧インテリPOPを専門職に分解（利益団体「インテリ」＝教師・官僚・医者・技師の政治的まとまり。実体は職業別POP）。
        double intel = cfg.Professional * cfg.IntelligentsiaFrac * share;
        // ★研究者（2026-08-17・作者裁定「上流階級と別カテゴリでインテリ（医者/技師/研究者）を置く」）＝インテリの4人目。
        //   ⚠**総量保存**：教師/官僚/医者の取り分から按分で切り出す（新しい人は湧かない＝思想⑥）。
        //   ResearcherFrac=0（既定）で keep=1.0＝旧の 0.40/0.35/0.25 と厳密一致・研究者Size=0＝**ビット恒等**。
        double resFrac = System.Math.Clamp(coef.ResearcherFrac, 0, 0.9);
        double keep = 1.0 - resFrac;
        var teacher    = MkPop(7, "教師", true, intel * 0.40 * keep, cfg.IntelLit, 800, modern: true);   // 公教育・義務教育の教育を算出
        var bureaucrat = MkPop(8, "官僚", true, intel * 0.35 * keep, cfg.IntelLit, 800, modern: true);   // 行政を算出（不足＝汚職）
        var doctor     = MkPop(9, "医者", true, intel * 0.25 * keep, cfg.IntelLit, 800, modern: true);   // 医療を算出（人口扶養力）
        var researcher = MkPop(13, "研究者", true, intel * resFrac, cfg.IntelLit, 800, modern: true);   // 大学の高等教育を算出（技師・上位POPの母体）
        var merchant   = MkPop(10, "商人", true, cfg.Merchant * share, 0.70, 1500, modern: true); // 流通マージン＋貿易の仲立ち。商船を買い海運を支える
        var artisan    = MkPop(11, "職人", true, cfg.Artisan * share, 0.45, 400);   // 手工業の自営（大衆ブロック＝ラッダイトの母体）
        // #12 家事使用人：富裕層(貴族＋資本家)の世帯規模に比例して雇われる（家事奉仕を算出）。女性がなりやすい（高FemaleShare＝紡績/家事奉公）。低識字・低富。
        var servant    = MkPop(12, "使用人", true, (nobility.Size + bourgeois.Size) * 1.0, 0.30, 150);   // 表示名は「使用人」（2026-07-04ユーザー指示）
        servant.SetAdultFemaleShare(0.75);   // 女性が多数（19世紀の家事使用人＝女性就労の主要な受け皿）
        // 職業POPの成人女性比（1789・ユーザー指示2026-07-04）：聖職/専門職は男性が支配的（貴族・農民・労働者は世帯階級＝0.5のまま）。
        //   ★年齢の層（2026-09-30 段4e 後半）＝20歳以上の層の男女を職業の女性比で分ける（0〜19 は半々）。
        clergy.SetAdultFemaleShare(0.10); bourgeois.SetAdultFemaleShare(0.20); engineer.SetAdultFemaleShare(0.02);
        teacher.SetAdultFemaleShare(0.25); bureaucrat.SetAdultFemaleShare(0.03); doctor.SetAdultFemaleShare(0.05); researcher.SetAdultFemaleShare(0.02);   // 研究者＝技師と同じ（19世紀の大学は男性のみ）
        merchant.SetAdultFemaleShare(0.25); artisan.SetAdultFemaleShare(0.30);

        // 兵士POP（Part 2）：平時の均衡の近くで播く＝既定の軍事費の段 × MaxSoldierFraction × 兵役の母数（働き盛りの男・(145)）。
        // ── ★2026-08-02：国別のハードコード初期兵（cfg.Military×250）を撤去 ──────────────────
        // ⚠旧は国ごとに手で置いた数で、**経済が養える量と桁で食い違っていた**（作者指摘）。実測（armydump）＝
        //   イギリス 初期兵1125／人頭均衡150＝**7.5倍**・プロイセン 900／61＝**14.7倍**・オーストリア 650／270＝2.4倍。
        //   その結果、開始10年で英の兵は 1125→364・充足率 1.00→0.21 へ落ち、軍ゲージは **450→20 と20倍以上潰れた**。
        //   逆に清は初期225に対し10年後 9,575人まで膨れ、充足率0.00＝**払えない大軍**になった。
        //   ＝初期値が台本で、1793年に一斉に経済の現実へ引き戻されていた（設計原則①台本禁止に反する）。
        // ★播種＝扶養限界の人口の側（上限割合×兵役の母数＝働き盛りの男・Military.RecruitMen）×既定の軍事費の段。
        //   ★(145)→(155)：平時の兵は定員（Military.EstablishmentOf＝払える予算÷1人あたりの費用と徴兵法の割当ての小さい方）＝払える予算は播種の時点に歳入が無いので読めない
        //   ＝最初の1〜2年で定員へ寄る（徴兵制＝強制の徴募が月 SoldierMigrationRate で・志願制＝転職の仕組みの志願・定員の超過は転職の仕組みが民間の職へ戻す）。
        //   旧＝「Military.cs の desired と完全一致」（軍事費の段×上限割合×頭数＝旧の平時の目標）。
        //   富の乗数（SoldierWealthExponent）は世界平均を要るので播種時には掛けない＝最初の数四半期で自然に効く。
        // ⚠ cfg.Military は抽象勢力（Nation を持たない標的）と Power の初期ゲージ表示に残る（次の Diplomacy.Step で再導出）。
        double pw = peasant.Size + worker.Size;
        var soldier = new Pop { ClassId = 5, ClassName = "兵士", IsUrban = false, Satisfaction = 1.0, IsSoldier = true, Ages = Pop.SoldierAges(),
            Literacy = pw > 1e-9 ? (peasant.Literacy * peasant.Size + worker.Literacy * worker.Size) / pw : 0.3 };
        double pwMen = Military.RecruitMen(peasant) + Military.RecruitMen(worker);   // ★(145) 兵役の母数の単位（働き盛りの男）
        double draft = PolicyDefaults.MilitarySpendingTarget * coef.MaxSoldierFraction * pwMen;
        if (pw > 1e-9)
        {
            Military.MoveCohort(peasant, soldier, draft * peasant.Size / pw, coef);   // ★2026-09-26：手元の coef を渡す（旧＝3引数版が既定の係数を新しく作っていた・sweep-23）
            Military.MoveCohort(worker,  soldier, draft * worker.Size  / pw, coef);
        }

        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, soldier, clergy, teacher, bureaucrat, doctor, merchant, artisan, servant };
        // ⚠⚠休眠時は**POPそのものを並べない**（実測2026-08-17・作者裁定A）：Size=0 でも pops に居ると
        //   `region.Pops.Count` が 13→14 になり、EconomyStep のスクラッチ配列長・労働配分・地代分配の
        //   ループ長が変わって軌道が動く（実測＝勝者 英→米・DD 83.1→87.6）。★大学の生産者を止めても
        //   差分ゼロだった＝**原因は生産者でなくPOPの存在そのもの**と切り分け済み。
        //   ResearcherFrac=0 で pops に1件も足さない＝**厳密恒等**。>0 のとき researcher は index 13（大学の LaborPopIndex と一致）。
        if (resFrac > 0) pops.Add(researcher);
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 7, bureaucratIdx: 8, doctorIdx: 9, coef: coef, researcherIdx: resFrac > 0 ? 13 : -1, clergyIdx: 6);   // clergyIdx＝段1 器 EduProducersCoexist の教区学校の担い手（0 では読まれない）
        // #12 家事奉仕の生産者：家事使用人(index12)が算出し富裕層が雇う（兵力と同型＝pop自身が役務を売り収入を得る）。
        producers.Add(new Producer { GoodId = Catalog.DomesticServiceId, Productivity = 1.0, Output = servant.Size * 0.5, MarginalCost = 0.3, LaborPopIndex = 12, OwnerPopIndex = 12, RentShare = 0.0 });
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, coef);   // 高度労働力（技師）＋石油火力
        AddArtisanProducers(producers, artisanIdx: 11);   // 職人の工房（工場との並存レシピ）。家事使用人を末尾(12)に足したので pops.Count-1 でなく職人の明示index
        // （兵力の生産者は (145) で削除＝兵士の給与は EconomyStep が兵士POPの所得へ直接払う＝兵力の市場は (140) から読み手が無かった）
        AddAlternateRecipes(producers, coef);   // ★代替レシピ（2026-09-06）：全ての州の構築経路で同じ1回
        var region = new Region
        {
            Id = id, Name = city, IsPort = p.Coastal, Resources = resources, Culture = culture, Religion = religion,   // 改修2026-06-28：港判定を一次データ(p.Coastal)へ＝改名耐性・網羅性。TariffInterest は CheckSecession で実利から毎期算出
            // ★州の格（植民地のラベル化・器2026-08-01）：一次データ HeldSince から確定する。
            // 既定 HeldSince=0 では 1783−0 ＝ 1783 ≥ CoreHeldYears(150) なので**この経路の全州が Core**。
            // ★B14（実バグ21件・2026-09-07）：world.json に heldSince（併合年＝ポーランド分割地/シレジア/ガリツィア/リガ/バグダード/台湾 等28州）を入れた。
            //   HeldSinceFromData=0（既定）では読まない＝全州 Core・支配度1.0＝旧＝厳密恒等（群計測の腕にするための読み手ゲート・採用後は係数を消して常時読む）。
            Dominance = !(coef.HeldSinceFromData > 0) || p.HeldSince <= 0 ? 1.0 : PoliticalQuantities.Clamp01((Calendar.StartYear - p.HeldSince) / coef.DominanceMatureYears),   // ★E-06 支配度の初期値＝保有年数÷世代長（中核＝1）
            Tier = !(coef.HeldSinceFromData > 0) || (Calendar.StartYear - p.HeldSince) >= coef.CoreHeldYears ? RegionTier.Core : RegionTier.Integrated,
            Malarial = p.Malarial,
            Rule = p.Rule is { Length: 4 } ? (double[])p.Rule.Clone() : cfg.Rule is { Length: 4 } ? (double[])cfg.Rule.Clone() : [1.0, 0.0, 0.0, 0.0],   // ★支配度の一般化 段1（2026-09-06）：4区分の初期値（州→国既定→国家1.0）
            RuleInit = p.Rule is { Length: 4 } ? (double[])p.Rule.Clone() : cfg.Rule is { Length: 4 } ? (double[])cfg.Rule.Clone() : [1.0, 0.0, 0.0, 0.0],

            // ★2026-08-26：一次データ（world.json の arableLand）があればそれを使う。0＝従来＝初期農民×余裕。
            //   ⚠これは辺境の廃止で失われるはずだった「人口と独立な土地の量」の受け皿＝(3)で全州へ一般化する入口。
            ArableLand = p.ArableLand > 0 ? p.ArableLand : peasant.Size * LandHeadroom,   // 耕地賦存量＝初期農民×余裕。流入で農村人口が超えると収穫逓減
            Terrain = Places.TerrainOf(city),   // ★(3) 地形タグ（places.json の1列・既定 Plain）。係数全1.0の間は恒等
            CapitalEnabled = true,
            Industrialization = cfg.Industrialization * (p.IndShare >= 0 ? p.IndShare : share),   // ★IndShare（一次データ）があれば産業地理で按分・無ければ従来の人口比
            CulturalGoodId = cfg.CulturalGoodId,   // 象徴的主食（表示アンカーのみ・固執機構は廃止）
            InvestorPopIndex = 3, MeritFromIndex = 2, MeritToIndex = 4,
            ClergyPopIndex = 6, TeacherPopIndex = 7, BureaucratPopIndex = 8, DoctorPopIndex = 9, SoldierPopIndex = 5, MerchantPopIndex = 10, ArtisanPopIndex = 11, ServantPopIndex = 12,
            ResearcherPopIndex = resFrac > 0 ? 13 : -1,   // ★段②：研究者は休眠時 -1＝Meritocracy の昇格先に数えない（POP自体も並んでいない）
            EducationProducerIndex = eduIdx,
            ColonialResources = [],   // 多国＝植民地ベース（首都州 Id0 のみ StepMonth で更新）
            Goods = goods,
            Pops = pops,
            Producers = producers,
            Markets = markets,
        };
        // ★初期地代のシード（2026-08-04・コールドスタートの穴）：LandRent は月次アキュムレータで、開幕1ヶ月目の
        //   政治（Blocs＝土地の政治力）が走る時点ではまだ0＝**開幕だけ旧支配層が無力**になり、AIがその隙に
        //   近代化法を通していた（lawtiming実測＝仏の公教育・資格任用が1783年に逆戻り）。
        //   月次の実勢（rentShare×産出×参照価格）を1回だけ推定して置く＝初回の EconomyStep が実流で上書きする。
        //   ⚠貨幣ではない（LandRent は診断/課税ベースの記録値）＝保存則に触れない。決定論（データのみから導出）。
        foreach (var pr in region.Producers)
            if (pr.IsRgo && pr.RentShare > 0 && pr.OwnerPopIndex >= 0 && pr.OwnerPopIndex < region.Pops.Count
                && pr.GoodId >= 0 && pr.GoodId < Catalog.RefPrices.Length)
                region.Pops[pr.OwnerPopIndex].LandRent += pr.RentShare * pr.Output * Catalog.RefPrices[pr.GoodId] * InitialRentSeedScale;
        return region;
    }

    // 役務（サービス財）の係数：労働力＝工場の投入。賃金は労働市場の価格で決まる（生産が先→売って収入の設計）。
    private const double LaborPerOutput = Catalog.LaborPerOutput;     // 工場の産出1単位あたりの労働力投入（Catalogが単一の真実）
    private const double LaborProductivity = 1.0;  // 工場労働者1人あたりの労働力算出の基準（×FullChainProductivityScale＝工場と同じ規模感。工業化・技術の倍率にも乗る）

    private const double FarmLaborPerOutput = Catalog.FarmLaborPerOutput;   // 自家農家の産出1単位あたりの農業労働力

    // 技術→工場生産性の逓減：技術水準がこの閾値（較正域）までは線形、超過分は緩やかな傾きで逓減（高技術でも生産性が暴走しない）。
    private const double TechProdDiminishKnee = 2.0;    // 逓減が始まる技術水準（Math.Min の上限＝Math.Max の基準）
    private const double TechProdDiminishSlope = 0.3;   // 閾値超の技術1あたりの追加生産性（逓減後の傾き）

    /// <summary>カタログ経済（市場・生産者）を1地域ぶん生成。地域ごとに独立した市場・状態を持つ（複数地域は別インスタンス）。
    /// 役務（労働力・教育・医療・行政）の生産者は担い手popの添字が地域構成で違うため、呼び出し側が <see cref="AddServiceProducers"/> で足す。
    /// farmLordShare＝農制：自家農家（RGO）の残余のうち地主（貴族）の取り分。1.0＝農奴制・0.5＝小作・0＝自作農（農民が所有）。</summary>
    private static (List<Good> goods, Market[] markets, List<Producer> producers) CatalogEconomy(double farmLordShare, Coefficients coef)   // ★coef（2026-09-05）：原油の機械入力・蒸気タービンの器を読む
    {
        int n = Catalog.Goods.Length;
        var goodsById = new Good[n];
        var markets = new Market[n];
        var producers = new List<Producer>(n);
        foreach (var d in Catalog.Goods)
        {
            double refP = Catalog.RefPrices[d.Id];   // 参照価格＝労働込みの実費用連鎖（深い連鎖ほど高い＝自然な価格勾配。役務は固定錨）
            goodsById[d.Id] = new Good { Id = d.Id, Name = d.Name, Tier = d.Tier, BaseNeed = d.BaseNeed, RefPrice = refP, Elasticity = d.Elasticity, IsColonial = d.IsColonial, Spoilage = Catalog.SpoilageOf(d.Id), NonTradeable = d.NonTradeable, GrainChain = d.GrainChain, Nutrition = d.Nutrition > 0 ? d.Nutrition : 0 };
            markets[d.Id] = new Market { Price = refP, Stock = 200, TargetStock = 200 };
            // 旧「colonial財は国内生産者なし（輸入のみ）」のスキップを撤廃＝colonial財も RGO 生産者を持つ。
            // 賦存州（州が Resources に列挙）のみ産み、非賦存州の生産者は BuildProvince が除去する＝茶＝中国/ゴム＝東南ア…の偏在が地理から創発（287州世界）。
            if (d.Tier == GoodTier.Service) continue;   // 役務の生産者は AddServiceProducers が担い手popに紐づけて追加
            bool isFactory = d.Inputs.Length > 0;
            // 雇用の一本化：工場は労働力＋高度労働力、自家農家（RGO）は農業労働力を市場で買う＝全生産者が同一ロジック。
            // 自家農家の残余（売上−労働費）＝地代：農奴制では全て地主（貴族）へ、小作は折半、自作農は農民自身へ。
            producers.Add(new Producer
            {
                GoodId = d.Id,
                // RGOの施設名（プランテーションdoc D2・2026-07-30／採取形態の是正 2026-08-10）＝鉱山／採掘場／牧場／漁場／猟場／伐採場／養蚕場／農場。工場は空（＝単一レシピの既定表示）。
                // 表示のみ＝エンジンの読み手ゼロ（RecipeName を読むのは Runner の診断と BreakDetectionTests の名前照合だけ）＝挙動不変。
                RecipeName = isFactory ? "" : Catalog.RgoFacilityName(d.Id),
                Productivity = d.Productivity * FullChainProductivityScale,   // 倍率排除⑤：国一律倍率を撤去。生産性差は工業化度(indMult)・技術・資源(辺境/鉄道)から創発
                Output = isFactory ? 0.5 : 50,         // 工場は表示閾値(Output>1)未満で開始＝初期はリストに出ず、育って初めて現れる（1789は前工業＝農村中心・工場はブルジョワ/プレイヤーが育てる）
                Scale = isFactory ? 0.05 : 1.0,        // 工場は操業規模を低く開始（全財がScale1.0で立たない＝初期は工場がほぼ無い）
                MarginalCost = isFactory ? Catalog.FactoryVa : Catalog.RgoVa,   // 自家農家＝0.34＋農業労働0.16≈旧0.5（供給曲線の損益分岐を維持＝過剰生産を防ぐ） // 賃金分は投入費（労働購入）へ移った＝残りは管理・資本/土地の付加価値
                LaborPopIndex = isFactory ? 2 : 1,     // 能力の按分基準：自家農家＝農民／工場＝都市労働者（実際の雇用は労働市場）
                OwnerPopIndex = isFactory ? 3 : (farmLordShare > 0 ? 0 : 1),   // 農地の所有：地主制＝貴族／自作農＝農民自身
                RentShare = isFactory ? 1.0 : Math.Max(0.3, farmLordShare),    // 残余の地代率（自作農でも0.3＝自分の土地の地代＝土地税の課税ベース）
                BuysLabor = isFactory,
                IsRgo = !isFactory,
                EffGoods = isFactory ? [] : Catalog.RgoBoosterGoods(d.Id),   // RGOの効率化材の候補（肥料/帆船/飼料/工具・火薬）。工場は空（電化を使う）
                Inputs = isFactory ? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(d.Id))]
                                   : coef.OilNeedsMachinery > 0 && d.Id == Catalog.CrudeOilId
                                       ? [new Input(Catalog.FarmLaborId, FarmLaborPerOutput), new Input(Catalog.MachineryId, coef.OilMachineryInput)]   // ★原油＝掘削機（機械）を要する（2026-09-05・器）
                                       : [new Input(Catalog.FarmLaborId, FarmLaborPerOutput)],
            });
        }
        var goodsList = new List<Good>(goodsById);
        // ★★全順序（2026-08-17）：Tier が同値のときは id で決める。List.Sort は introsort＝**不安定**なので、
        //   同 Tier 内の相対順序が配列長に依存する＝**財を1つ足すだけで既存財の並びが総入れ替えになる**（実測：
        //   高等教育1財の追加で勝者が英→米・最大DD 35.4%→94.7%）。この並びは MarketFlow の価格更新順と
        //   EconomyStep の消費順（同 Tier・同価格のタイブレーク）に直結するため、力学が動く。
        //   id を第2キーに置くと全順序になり、以後は新財を足しても既存財の順序が不変になる。
        goodsList.Sort((a, b) => { int t = ((int)a.Tier).CompareTo((int)b.Tier); return t != 0 ? t : a.Id.CompareTo(b.Id); });   // 消費優先順
        return (goodsList, markets, producers);
    }

    /// <summary>職人の工房（工場との並存レシピ）：衣料・家具・革製品・陶磁器を手仕事で作る自営（全付加価値が職人の収入）。
    /// 1789年は職人が主役（操業規模1.0 vs 工場0.05）。工場が機械・電化で安くなるほど職人レシピは採算で負け、
    /// 没落して労働者へ転落する（ラッダイトの母体＝転落の闘争度が政治に流れ込む）。</summary>
    /// <summary>★代替レシピ（並存レシピ）の注入＝州の構築経路で1回（2026-09-06）。坩堝鋼/錬鉄レール・石油ケロシン（★(149)：(148) の F4＝石油灯油への畳み込みは差し戻した）・冷蔵の加工肉・黄鉄鉱法。汽船・機関車の onset（2026-09-27）。
    /// 旧＝Simulation のコンストラクタで構築時の州だけに注入していた（昇格植民地・建国州が代替を持たない穴）。各器=0 で1件も足さない＝恒等。</summary>
    private static void AddAlternateRecipes(List<Producer> producers, Coefficients coef)
    {
        // ★汽船・機関車・鋼の既定のレシピ（転炉鋼）の門＝発見の表の行（2026-10-01 段2A＝汽船・機関車 1820年代・転炉鋼 1850年代）が財とレシピ名から置く
        //   ＝ここでは onset を焼かない（旧＝リ5 SteamTransportOnset・A-高1 CrucibleSteelEnabled の2本の旗が既定の Producer へ onset を焼いた・旗は焼き込みで削除）。
        // A-高1（史実検証2026-07-22）：鋼のベッセマー不連続。既定レシピ（鉄1.0+コークス0.4＝ベッセマー転炉）は発見（転炉鋼）まで作れず、
        // 「坩堝鋼」（鉄2.2+コークス1.2・Productivity0.9＝るつぼ1基数十kgのバッチ＝5倍以上高い）と「錬鉄レール」（鉄1.3＝
        // 鉄道が転炉鋼より手前にあるため、これが無いと最初の鉄道が坩堝鋼で敷設され逆向きに行き過ぎる）を地理差（常にある）として並存させる。
        // 転炉鋼の発見の後はベッセマーがコストで自動的に坩堝鋼・錬鉄レールを駆逐（1870年代英のレール転換が創発）。
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
                    LaborPopIndex = steelDefault.LaborPopIndex, OwnerPopIndex = steelDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                    Inputs = [new Input(Catalog.IronId, 2.2), new Input(Catalog.CoalId, 1.978), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SteelId))] });
                if (railDefault is not null)
                    producers.Add(new Producer { GoodId = Catalog.RailId, RecipeName = "錬鉄レール", Productivity = 1.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                        LaborPopIndex = railDefault.LaborPopIndex, OwnerPopIndex = railDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                        Inputs = [new Input(Catalog.IronId, 1.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RailId))] });
            }
        }
        // ★前期の軍需レシピ（2026-09-07・英の軍が立たない根＝armsdebug 実測：1783年の小銃在庫363/目標3696・発注は毎期立つのに60年間充足0.02〜0.09）：
        //   小銃(82)＝鋼0.6＋製材0.4／火砲(81)＝鋼1.0＋機械部品0.5 は鋼を要するが、鋼の既定レシピ（ベッセマー）は onset0.55（≈1858）まで無く〔★2026-10-01 段2A＝発見の表の転炉鋼（1850年代）を発見するまで無く〕坩堝鋼（5倍高い・規模0.05）だけ
        //   ＝軍需は鋼に縛られて作れない。史実のマスケット（錬鉄の銃身）と鋳鉄砲/青銅砲は鋼を要しない＝**時代錯誤の投入**。
        //   坩堝鋼/錬鉄レールと同じ型で「マスケット」（鉄1.0＋製材0.4）「鋳鉄砲」（鉄1.3＋製材0.3）を1783年から満額で並存させる（退場は下）。生産性＝既定×MusketProductivity（前装式・手工程）。
        //   新しい財は足さない。★旗 MusketRecipeEnabled は 2026-09-27 回2-1 で点火して削除＝常に足す（古いセーブは組み立て直さない＝前期レシピの無いまま続く・作者裁定 B13）。全国対称（国名を書かない）。
        // ★退場（2026-09-27 回2-1 の続き・(62) の申し送り③）：前期レシピは後継の発見で退く＝発見の表の退場の行（Catalog・2026-10-01 段2A＝2値＝転炉鋼を発見した四半期に能力0）。
        //   後継＝既定のレシピ（鋼の後装施条砲・鋼の小銃）を実用にした発見＝ベッセマー転炉（上の鋼の既定レシピ＝発見の表の転炉鋼）。
        //   根＝火砲は費用で転換しない（鋳鉄砲＝鉄1.3＋製材0.3 はベッセマーの限界費用の鋼でも既定＝鋼1.0＋機械部品0.5 より安い・1783年の参照価格で 2.50 対 4.10）
        //   ＝1950年まで鋳鉄の前装砲が残っていた。史実の転換（アームストロング1859・クルップ1860年代）は費用でなく性能（射程・発射速度）で起き、1880年以後に鋳鉄の滑腔砲を買う軍は無い。
        //   小銃も同じ規則の1本（史実の後装銃・施条銃は前装の滑腔銃より高かった＝費用の転換は後押しに過ぎない）。onset は鋼のデータから読む＝新しい係数を作らない。
        Producer? rifleDefault = null, cannonDefault = null;
        foreach (var pr in producers)
        {
            if (pr.GoodId == Catalog.RifleId && rifleDefault is null) rifleDefault = pr;
            else if (pr.GoodId == Catalog.ArtilleryId && cannonDefault is null) cannonDefault = pr;
        }
        if (rifleDefault is not null)   // 軍需の無い世界（最小シナリオ）は不発
            producers.Add(new Producer { GoodId = Catalog.RifleId, RecipeName = "マスケット",
                Productivity = rifleDefault.Productivity * coef.MusketProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = rifleDefault.LaborPopIndex, OwnerPopIndex = rifleDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.0), new Input(Catalog.LumberId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RifleId))] });
        if (cannonDefault is not null)
            producers.Add(new Producer { GoodId = Catalog.ArtilleryId, RecipeName = "鋳鉄砲",
                Productivity = cannonDefault.Productivity * coef.MusketProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = cannonDefault.LaborPopIndex, OwnerPopIndex = cannonDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.3), new Input(Catalog.LumberId, 0.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ArtilleryId))] });
        // ★(169) 1780年代の蒸気機関＝「鋳鉄の機関」（時代のレシピ・前期の軍需と同じ型・推奨で進めた）。既定のレシピ（機械0.726＋石炭0.4）の機械は鋼（転炉鋼の前は坩堝鋼＝5倍高い）
        //   ＝開幕の蒸気機関の財を作る費用が参照の3〜10倍（(165)(167) の腕A・B）＝時代錯誤の投入。史実＝ワットの機関（1776〜1800）は鋳鉄のシリンダー（ウィルキンソンの中ぐり盤1774）・
        //   鋳鉄の梁と弾み車・錬鉄と銅のボイラーで、鋼は使わなかった（Dickinson & Jenkins 1927・Hills 1989）。投入＝既定の機械の中身（鋼1.10＋銅0.41）の鋼を鉄に置き換え、
        //   重さはマスケット（鉄1.0 対 鋼0.6）と同じ比（鋳鉄・錬鉄は同じ強さに鋼の約1.7倍）＝鉄1.832＋銅0.414＋石炭0.4＝参照価格の世界で既定と同じ費用（3.81 対 3.80）。
        //   退場＝転炉鋼の発見（Catalog の退場の表・高圧と複式の鋼の機関＝性能で退く）。国名の分岐を作らない（全ての州に同じ1本）。
        Producer? engineDefault = null;
        foreach (var pr in producers) if (pr.GoodId == Catalog.SteamEngineId) { engineDefault = pr; break; }
        if (engineDefault is not null)   // 蒸気機関の無い世界（最小シナリオ）は不発
            producers.Add(new Producer { GoodId = Catalog.SteamEngineId, RecipeName = "鋳鉄の機関",
                Productivity = engineDefault.Productivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                LaborPopIndex = engineDefault.LaborPopIndex, OwnerPopIndex = engineDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.IronId, 1.832), new Input(Catalog.CopperId, 0.414), new Input(Catalog.CoalId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SteamEngineId))] });
        // ★石油ケロシン（灯油doc・2026-08-13）：灯油(68)の**第2レシピ**。既定は鯨油/魚油（漁獲0.8）。
        // ⚠★2026-09-03（キューAUD-09・監査「保存則・台帳」）：**追加根拠のコメントが事実と食い違っていた**。
        //   「原油には買い手が1本も無い40年の空白があった」と書かれていたが、現物には AddAdvancedProducers 側に
        //   同じ原油→灯油の近代レシピ（RecipeName="石油灯油"・同じ1850年代の発見の行〔旧 TechOnset=0.6〕）が**既にある**＝空白ではない。
        //   ⇒ 実体は「同じ変換が2レシピ並存している」。⚠どちらを残すかは帯が動く（KeroseneFromOilEnabled=1 は点火済みで
        //   列強+6.2/人口−5.7 の実測がある）ので、ここでは**事実の記録だけ**に留め、統合は測定とセットで行う（キューAUD-09）。
        // ⚠新しい Good は1つも足さない——足すのは Producer 1件だけ。極性は鋼と逆で、**新レシピの方が安い**＝発見の後に自動で置き換わる（★2026-10-01 段2A・旧＝onset後）。
        //   史実の較正＝鯨油1.45ドル/ガロン(1856)に対しケロシンは0.58(1865)→0.08(1885)＝おおむね5〜10倍安い。
        //   投入は原油0.5（1870年代の精製収率45〜60%）＋生産性1.6（連続精製 vs 3〜4年の捕鯨航海）で表す＝倍率を書かない。
        // ★解禁＝発見の表の石油ケロシン（1850年代・2026-10-01 段2A＝石油灯油と同じ年代・畳むのは設計 §5 F4）。⚠KeroseneFromOilEnabled=0 で不発＝厳密恒等。
        if (coef.KeroseneFromOilEnabled > 0)
            {
                Producer? keroDefault = null;
                foreach (var pr in producers) if (pr.GoodId == Catalog.LampOilId) { keroDefault = pr; break; }
                if (keroDefault is not null)   // 灯油を作らない世界（最小シナリオ）は不発
                {
                producers.Add(new Producer { GoodId = Catalog.LampOilId, RecipeName = "石油ケロシン",
                    Productivity = coef.KeroseneOilProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = keroDefault.LaborPopIndex, OwnerPopIndex = keroDefault.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                    Inputs = [new Input(Catalog.CrudeOilId, coef.KeroseneOilInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LampOilId))] });
                }
            }

        // ★冷蔵の加工肉（2026-09-04・作者提案「冷蔵庫という形にして、肉輸送+消費財でよいのでは」）＝**加工肉(63)の第2レシピ**。
        //   既定は 肉+小麦粉（塩漬け・ソーセージ）。⚠冷凍船（1877/1882）以後は**冷蔵**が主役になる。
        //   ★新しい財を作らない＝加工肉の需要は既に較正済みのまま使える（作者指摘で「冷蔵肉」の新設を撤回した）。
        //   ⚠解禁は暦年でなく**冷蔵庫(137)が作れるか**＝精密工作機械の連鎖に律速される（時計を書かない）。
        //   ★副産物＝塩漬けの設備が座礁する＝「乗り換えるか／捨てられる側が抵抗するか」の実例になる。
        //   ⚠ChilledMeatEnabled=0 で Producer を1件も足さない＝**厳密恒等**（休眠）。
        if (coef.ChilledMeatEnabled > 0)
            {
                Producer? saltCured = null;
                foreach (var pr in producers) if (pr.GoodId == Catalog.PreservedMeatId) { saltCured = pr; break; }
                if (saltCured is not null)   // 加工肉を作らない世界（最小シナリオ）は不発
                {
                producers.Add(new Producer { GoodId = Catalog.PreservedMeatId, RecipeName = "冷蔵の加工肉",
                    Productivity = saltCured.Productivity * coef.ChilledMeatProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = saltCured.LaborPopIndex, OwnerPopIndex = saltCured.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                    Inputs = [new Input(Catalog.MeatId, 0.6), new Input(Catalog.RefrigeratorId, coef.ChilledMeatColdInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.PreservedMeatId))] });
                }
            }

        // ★E-15 手順1（2026-09-05）：化学薬品(37)の第2レシピ「黄鉄鉱法」＝硫黄の代替（鉄鉱石＋石炭＋塩＝黄鉄鉱を焙焼して硫酸）。
        //   史実＝1838年の硫黄危機（シチリア独占）の後、世界は原料の代替（リオ・ティントの黄鉄鉱）で解いた。新しい財は足さない。
        //   解禁＝発見の表の黄鉄鉱法（1830年代・2026-10-01 段2A）・生産性は硫黄法×SulfurFromPyriteProductivity（高くついた）。
        //   火薬(38)は代替しない（黒色火薬は元素硫黄が要る）。⚠SulfurFromPyriteEnabled=0 で Producer を1件も足さない＝厳密恒等。
        if (coef.SulfurFromPyriteEnabled > 0)
            {
                Producer? leblanc = null;
                foreach (var pr in producers) if (pr.GoodId == Catalog.ChemicalsId) { leblanc = pr; break; }
                if (leblanc is not null)
                {
                producers.Add(new Producer { GoodId = Catalog.ChemicalsId, RecipeName = "黄鉄鉱法", Productivity = leblanc.Productivity * coef.SulfurFromPyriteProductivity, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa,
                    LaborPopIndex = leblanc.LaborPopIndex, OwnerPopIndex = leblanc.OwnerPopIndex, RentShare = 1.0, BuysLabor = true,
                    Inputs = [new Input(Catalog.IronOreId, 0.6), new Input(Catalog.CoalId, 0.8), new Input(Catalog.SaltId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ChemicalsId))] });
                }
            }
    }

    private static void AddArtisanProducers(List<Producer> producers, int artisanIdx)
    {
        foreach (int gid in (int[])[Catalog.ClothingId, Catalog.FurnitureId, Catalog.LeatherGoodsId, Catalog.PorcelainId])   // 衣料・家具・革製品・陶磁器
        {
            var d = Catalog.Goods[gid];
            producers.Add(new Producer
            {
                GoodId = gid, RecipeName = "職人",
                Productivity = d.Productivity * FullChainProductivityScale * 0.6,   // 手仕事＝工場より遅い
                Output = 30, Scale = 1.0, MarginalCost = 0.35,   // 1789＝職人が現役の主役（工場は0.5/0.05から育つ）
                LaborPopIndex = artisanIdx, OwnerPopIndex = artisanIdx, RentShare = 0.0,
                Inputs = [.. d.Inputs],   // 素材だけ（雇用も機械も使わない自営）
            });
        }
    }

    /// <summary>役務の生産者（労働力・教育・医療・行政）を追加し、教育の生産者の添字を返す。
    /// 「生産が先にあり、売って収入を得る」：各役務popは自分の役務を算出し、売上が全て自分の収入になる（自営＝RentShare 0）。
    /// 教育の担い手（聖職者⇄教師）と生産性は毎月 StepMonth が教育法で切り替える。
    /// ★段1 器 <see cref="Coefficients.EduProducersCoexist"/>（2026-09-12）：>0 かつ clergyIdx≥0 なら教育の生産者を**常設2件**（[返値]=教区学校・[返値+1]=公立学校）として並置し、
    /// StepMonth は差し替えず非活性側の Scale=0 だけを置く。0（既定）では従来の1件だけを add＝Producers の長さ・順序が不変＝厳密恒等。</summary>
    private static int AddServiceProducers(List<Producer> producers, List<Pop> pops, int workerIdx, int peasantIdx, int teacherIdx, int bureaucratIdx, int doctorIdx, Coefficients? coef = null, int researcherIdx = -1, int clergyIdx = -1)
    {
        // 労働力：工場労働者が算出。生産性は工場と同じスケール（×FullChainProductivityScale）＝労働供給が工場群の需要と釣り合う。
        // 工業化・技術の倍率にも乗る（機械が労働の産出を増やす＝実質賃金の上昇余地）。
        producers.Add(new Producer { GoodId = Catalog.LaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = pops[workerIdx].Size * 0.3, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = 0.0 });
        // 04材化(c)（2026-07-29）：動力＝労働力の並存レシピ（Catalog.cs:14の設計を実体化）。同じ労働者popを担い手に持つ＝労働配分の
        // 零和分割の内側＝採算が合う時だけ人力からシェアを奪う（機械化＝同じpopからより多くの労働力サービス）。馬・蒸気機関・石炭・電力に
        // 「動力源」という新しい出口ができる（新財ゼロ＝§1の致命3障害を全回避）。水車はE1立地上限が未解決＝裁定待ちで見送り。
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "畜力", Productivity = LaborProductivity * FullChainProductivityScale * 1.3, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = 0.0,
            Inputs = [new Input(Catalog.HorseId, 0.05)] });   // 馬力＝荷駄・巻上機（馬が本当の生産投入になる）
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "蒸気機関", Productivity = LaborProductivity * FullChainProductivityScale * 2.0, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = 0.0,
            Inputs = [new Input(Catalog.SteamEngineId, 0.02), new Input(Catalog.CoalId, 0.3)] });   // 蒸気機関54＋石炭＝工場に据える原動機（54の第3の出口＝Runner診断が点灯）
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "電動", Productivity = LaborProductivity * FullChainProductivityScale * 2.5, Output = 0.5, Scale = 0.05, MarginalCost = 0.35, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = 0.0,
            Inputs = [new Input(Catalog.ElectricityId, 0.25)] });   // 電動機＝電力が安い国だけ採算が合う（発電機1.3以後）
        // 水車（07_再設計 §2＋A2裁定・2026-07-31）：★裁定＝**水車は弱い動力（自家農場級）／山岳の強力な水力は巨大建造物**の2層に分ける。
        // したがって立地上限（案a/b/c）は全て破棄＝上限なしの微優位レシピ1本にする。投入ゼロ（落差は買えない）・維持費は人力より高い（堰・水路の普請）。
        // 1789年から使えて蒸気(0.5)に駆逐される＝前産業の動力がカタログに存在する状態を作るのが目的。
        producers.Add(new Producer { GoodId = Catalog.LaborId, RecipeName = "水車", Productivity = LaborProductivity * FullChainProductivityScale * 1.1, Output = 0.5, Scale = 0.05, MarginalCost = 0.40, LaborPopIndex = workerIdx, OwnerPopIndex = workerIdx, RentShare = 0.0 });
        // 農業労働力：農民が算出し自家農家（RGO）が買う＝農業も「労働を売る」同一ロジック。農村の留保賃金は都市より安い（都市化の誘因）。
        producers.Add(new Producer { GoodId = Catalog.FarmLaborId, Productivity = LaborProductivity * FullChainProductivityScale, Output = pops[peasantIdx].Size * 0.3, MarginalCost = 0.3, LaborPopIndex = peasantIdx, OwnerPopIndex = peasantIdx, RentShare = 0.0 });
        int eduIndex = producers.Count;
        if (coef is { EduProducersCoexist: > 0 } && clergyIdx >= 0)
        {
            // ★段1 器 EduProducersCoexist（教育の三層化 doc §2-1／LIST §0-9-C 段3・2026-09-12）：同じ財（教育94）に生産者を2本**常設**で並べる
            //   （E-13 の黄鉄鉱法・蒸気タービンと同じ「同じ財に複数レシピ」型）。[eduIndex]=教区学校＝聖職者・ClergyEduProductivity・紙61×0.05（写本・教義書）／
            //   [eduIndex+1]=公立学校＝教師・TeacherEduProductivity・書籍112×0.05（近代の教科書）。2件目の添字は「連続 add」から導く＝Region に新フィールドを持たない（盤面則）。
            //   担い手・生産性・投入は**構築時に確定し以後差し替えない**。教育法は StepMonth が非活性側の Scale=0 を置くゲートに降格（TechGatedScaleZero の役務版）。
            //   自営（LaborPopIndex=OwnerPopIndex・RentShare 0）は現行と同じ＝売上が丸ごと担い手 pop の Earned（doc §1-1）。
            producers.Add(new Producer { GoodId = Catalog.EducationId, RecipeName = "教区学校", Productivity = coef.ClergyEduProductivity, Output = 1, MarginalCost = 0.4, LaborPopIndex = clergyIdx, OwnerPopIndex = clergyIdx, RentShare = 0.0, Inputs = [new Input(Catalog.PaperId, 0.05)] });
            producers.Add(new Producer { GoodId = Catalog.EducationId, RecipeName = "公立学校", Productivity = coef.TeacherEduProductivity, Output = 1, MarginalCost = 0.4, LaborPopIndex = teacherIdx, OwnerPopIndex = teacherIdx, RentShare = 0.0, Inputs = [new Input(Catalog.BookId, 0.05)] });
        }
        else
        {
            // 教育（器0＝現行）：既定＝教師（公教育の生産性）。宗教学校では聖職者に切替（生産性が低い＝少人数しか教えられない＝供給希少）。紙を消費（教材）。
            producers.Add(new Producer { GoodId = Catalog.EducationId, Productivity = 5.0, Output = 1, MarginalCost = 0.4, LaborPopIndex = teacherIdx, OwnerPopIndex = teacherIdx, RentShare = 0.0, Inputs = [.. Catalog.Goods[Catalog.EducationId].Inputs] });
        }
        // 医療：医者が算出（医薬品を消費）。POPが買い、消費が人口扶養力に効く。供給は希少（1789＝富裕層しか医者にかかれない）。
        producers.Add(new Producer { GoodId = Catalog.HealthcareId, Productivity = 4.0, Output = 1, MarginalCost = 0.3, LaborPopIndex = doctorIdx, OwnerPopIndex = doctorIdx, RentShare = 0.0, Inputs = [.. Catalog.Goods[Catalog.HealthcareId].Inputs] });
        // 行政：官僚が算出（紙を消費＝文書行政）。国家が買う。不足＝汚職（税収が漏れる）。初期は不足気味＝近世国家。
        producers.Add(new Producer { GoodId = Catalog.AdminId, Productivity = 3.0, Output = 1, MarginalCost = 0.3, LaborPopIndex = bureaucratIdx, OwnerPopIndex = bureaucratIdx, RentShare = 0.0, Inputs = [.. Catalog.Goods[Catalog.AdminId].Inputs] });
        // ★大学（2026-08-17・作者裁定「大学は首都州の欄にあるべきではない。★病院や学校と同じで、全州に建設される可能性がある」）：
        //   研究者が高等教育を算出し、**教育を既に買ったpopがさらに自費で買う**ことでのみ技師・研究者へ昇格（無償化されない＝作者裁定2026-08-14）。
        //   投入＝書籍のみ（⚠ガラスは史実検討で却下2026-08-17）。生産性は教育5.0/医療4.0/行政3.0 より低い＝**希少で高価**。
        //   ⚠researcherIdx<0（研究者POPを持たない世界＝較正・辺境・最小）または ResearcherFrac=0 なら1件も足さない＝**厳密恒等**。
        if (researcherIdx >= 0 && coef is { ResearcherFrac: > 0 })
            producers.Add(new Producer { GoodId = Catalog.HigherEducationId, Productivity = coef.UniversityProductivity, Output = pops[researcherIdx].Size * 0.5, MarginalCost = 0.4, LaborPopIndex = researcherIdx, OwnerPopIndex = researcherIdx, RentShare = 0.0, Inputs = [.. Catalog.Goods[Catalog.HigherEducationId].Inputs] });
        return eduIndex;
    }

    /// <summary>高度労働力（技師の役務）＋石油火力（電力の並存レシピ）の生産者。
    /// 高度労働力＝技師が算出する監督・保守・運転の役務。全工場が少量・エネルギーを使う工程ほど多く要求し、
    /// 鉄道・船舶の運行も消費する＝工業化・鉄道・電化のすべてが技師の価値を押し上げる（教育→技師供給と噛み合う）。
    /// 石油火力＝発電機＋原油→電力（石炭火力との並存レシピ＝燃料の択一。市場が安い方を選ぶ）。</summary>
    private static void AddAdvancedProducers(List<Producer> producers, List<Pop> pops, int engineerIdx, int workerIdx, int ownerIdx, Coefficients coef)
    {
        // 高度労働力：技師の自営役務（人力と同じく頭数なり＝技師階級の成長が供給を増やす唯一の道）。
        producers.Add(new Producer { GoodId = Catalog.AdvLaborId, Productivity = 4.0, Output = pops[engineerIdx].Size * 0.5, MarginalCost = 0.5, LaborPopIndex = engineerIdx, OwnerPopIndex = engineerIdx, RentShare = 0.0 });
        // 石油火力（電力の第2レシピ）：他の工場と同じく労働力＋高度労働力を買う。
        // A-中1（2026-07-23）：燃料主導の比率へ（発電機0.5→0.15。原油0.6＝石炭0.9のエネルギー等価1/1.5）。
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "石油火力", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.498), new Input(Catalog.CrudeOilId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // A-中1（2026-07-23）：水力発電（電力の第3レシピ）＝燃料ゼロ・資本費重（発電機0.40＝火力の2.7倍＝ダム/導水路の資本）。
        // 石炭が高い州でのみ火力に勝つ＝スイス/伊/ノルウェー型の水力国が価格から創発（1914年に伊約2/3・スイス/ノルウェーほぼ100%水力）。
        producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "水力発電", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.MachineryId, 0.647), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // ★蒸気タービン（2026-09-05・器 SteamTurbineEnabled）：電力の第4レシピ＝発電機0.2＋石炭 SteamTurbineCoalInput（熱効率で0.9→0.6）・生産性×SteamTurbineProductivity。
        //   パーソンズ1884→1890s＝発見の表の蒸気タービン（1880年代・2026-10-01 段2A）。市場が安い方を選ぶ＝火力の中で石炭を節約する製法が創発的に置き換える。0＝足さない＝恒等。
        if (coef.SteamTurbineEnabled > 0)
            producers.Add(new Producer { GoodId = Catalog.ElectricityId, RecipeName = "蒸気タービン", Productivity = 1.4 * coef.SteamTurbineProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = 2, OwnerPopIndex = 3, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.MachineryId, 0.199), new Input(Catalog.CoalId, coef.SteamTurbineCoalInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ElectricityId))] });
        // 魚の缶詰（缶詰の第2レシピ）：肉(畜産)の代わりに漁獲を缶詰める＝缶詰の軍需増を漁業からも賄える。漁獲に第2の消費先を与える（従来は干物のみ）。
        producers.Add(new Producer { GoodId = Catalog.CannedFoodId, RecipeName = "魚の缶詰", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.FishId, 0.6), new Input(Catalog.IronId, 0.3), new Input(Catalog.SaltId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CannedFoodId))] });
        // 下位インフラ＝水道局（#4 施設化）：建設会社(資本家所有)が労働力+技師+資材(セメント/鋼)で衛生インフラ(上下水道)を敷設＝流通不可の役務。国家が行政費で調達。
        producers.Add(new Producer { GoodId = Catalog.InfrastructureId, RecipeName = "水道局", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CementId, 0.4), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // 上位インフラ＝電化インフラ（近代都市インフラ）。2つの並存レシピ＝早期はガス灯、電化が安くなると電灯が駆逐（創造的破壊）。国家が行政費で調達。便益＝満足度＋衛生＋送電容量。
        // 電灯式（既定）：送電網＋電化製品＋鋼。電化期の財が高い序盤は高コストで休眠＝自然に終盤の能力。
        producers.Add(new Producer { GoodId = Catalog.ElecInfraId, RecipeName = "電灯網", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.GridId, 0.4), new Input(Catalog.ApplianceId, 0.3), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // ガス灯式（並存・早期）：ガス＋鋼。電化前から都市照明を供給＝電灯が安くなるまでの中道。電灯に駆逐される。
        producers.Add(new Producer { GoodId = Catalog.ElecInfraId, RecipeName = "ガス灯", Productivity = 1.5 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.GasId, 0.6), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.2)] });
        // === 植民地素材の国内代替（並存レシピ）＝封鎖/戦争で植民地材が高騰すると自動で勝つ＝輸入代替が創発（特別分岐なし）===
        // 平時は植民地材が安く既定レシピ（サトウキビ/パーム油）が勝つよう、国内材は係数をやや厚く＝普段は休眠・危機に起動。
        // 甜菜糖：砂糖の第2レシピ。サトウキビ(25・植民地輸入)の代わりに国内の果実(16)を糖源に＋化学薬品(精製)。ナポレオン大陸封鎖のビート糖。
        // **明確に劣位なフォールバック**（果実を厚く＝平時は植民地糖が安く勝ち休眠、植民地糖が断たれた時だけ起動）。
        producers.Add(new Producer { GoodId = Catalog.SugarId, RecipeName = "甜菜糖", Productivity = 1.4 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.FruitId, 2.2), new Input(Catalog.ChemicalsId, 0.15), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SugarId))] });
        // 獣脂石鹸：石鹸の第2レシピ。パーム油(23・植民地輸入)の代わりに国内の畜産(19・獣脂)＋化学薬品。伝統的な獣脂石鹸（植民地油脂が断たれても作れる）。
        producers.Add(new Producer { GoodId = Catalog.SoapId, RecipeName = "獣脂石鹸", Productivity = 1.3 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.LivestockId, 1.1), new Input(Catalog.ChemicalsId, 0.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SoapId))] });
        // === 旧生産方式を駆逐する「電化/化学」の新レシピ（並存＝安くなった新法が旧法を置換＝創造的破壊）===
        // ハーバーボッシュ（肥料109の第2レシピ）：硫黄を使わず化学薬品＋**電力**で窒素固定＝超安価。電力(87)依存で**電化期に自動ゲート**（電化製品80と同型）。TechOnset=既定ゲート(0.6)の床付き（02断線走査2026-07-28：名目解禁の1789迂回を閉鎖―実勝利は電力価格が決める）。
        // 高tech＝電力が安い国だけ起動し従来肥料（化学薬品+硫黄）を駆逐＝終盤の農業革命（食料連結で人口増＝技術投資の報酬）。
        producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "ハーバーボッシュ", Productivity = 2.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.ChemicalsId, 0.15), new Input(Catalog.ElectricityId, 0.25), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });   // 02断線走査（2026-07-28）：0.3/0.4→0.15/0.25＝「少量で同機能」の勝ち筋（旧・3レシピ中最も高い型F＝終盤の農業革命が一度も起きない）を是正。電力の燃料主導化とセット
        // 合成染料（染料17の第2レシピ）：天然染料(RGO)の代わりに化学薬品から合成（アニリン1856）。化学工業が成熟＝化学薬品が安い国で天然染料を駆逐。
        producers.Add(new Producer { GoodId = Catalog.DyeId, RecipeName = "合成染料", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.ChemicalsId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.DyeId))] });
        // 毛織衣料（衣料57の第2レシピ）：綿布(綿花)の代わりに毛織物(畜産)で仕立てる＝綿花が断たれた時の代替（米南北戦争の綿花飢饉でランカシャーが羊毛へ）。
        producers.Add(new Producer { GoodId = Catalog.ClothingId, RecipeName = "毛織衣料", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.WoolenClothId, 0.7), new Input(Catalog.DyeId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ClothingId))] });
        // === #30 Stage2 産業作物の並存レシピ＝新主食を主食baseNeedでなく産業原料として接続（食料系を撹乱しない）===
        // ジャガイモ蒸留酒（蒸留酒103の第2レシピ）：麦(15)の代わりにジャガイモ(115)澱粉を蒸留＝東欧/露の安価な穀物代替（ウォッカ/シュナップス）。ジャガイモが安い産地で起動。
        producers.Add(new Producer { GoodId = Catalog.SpiritsId, RecipeName = "ジャガイモ蒸留酒", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.PotatoId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SpiritsId))] });
        // トウモロコシ飼料（飼料110の第2レシピ）：麦(15)の代わりにトウモロコシ(116)＝米州/南欧/バルカンの高単収飼料。トウモロコシが安い産地で起動し畜産(19)の濃厚飼料を賄う。
        producers.Add(new Producer { GoodId = Catalog.FodderId, RecipeName = "トウモロコシ飼料", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.MaizeId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FodderId))] });
        // 米酒（蒸留酒103の第3レシピ）：麦(15)/ジャガイモ(115)の代わりに米(102)を醸造蒸留＝日本/東アジア/南欧の米産地の清酒・焼酎。米が安い産地で起動（麦蒸留酒との燃料/原料択一）。
        producers.Add(new Producer { GoodId = Catalog.SpiritsId, RecipeName = "米酒", Productivity = 1.9 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.RiceId, 0.6), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SpiritsId))] });
        // グアノ肥料（肥料109の第3レシピ）：合成(化学薬品+硫黄)・ハーバーボッシュ(電力)に並ぶ天然窒素源＝グアノ(121・ペルー賦存の植民地材)。ペルー保有国(グアノ輸入可)でのみ起動し、合成肥料より安価＝1840s-70sのグアノ時代を再現。非保有国は合成肥料のまま。
        producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "グアノ肥料", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.GuanoId, 0.6), new Input(Catalog.ChemicalsId, 0.1), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });
        // ★堆肥（2026-09-06・器 CompostFertilizerEnabled）：肥料(109)の第4レシピ＝畜産(19)＋労働・解禁0。化学法より高い＝硫黄が届かない所の代替。0＝足さない＝恒等。
        if (coef.CompostFertilizerEnabled > 0)
            producers.Add(new Producer { GoodId = Catalog.FertilizerId, RecipeName = "堆肥", Productivity = 2.0 * coef.CompostProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.LivestockId, coef.CompostLivestockInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.FertilizerId))] });   // ★雇用の一本化＝全工場が労働+高度労働を買う（ServiceEconomyTests の番人・2026-09-06）
        // ★硝石丘（2026-09-06・器 NiterBedEnabled）：硝石(118)の代替レシピ＝畜産(19)＋労働・解禁0（仏1793の硝石丘＝厩肥と灰から）。産地の硝石より高い＝
        //   財の3分類（作者裁定「無くても低効率で生産できる」）の硝石版。0＝足さない＝恒等。
        if (coef.NiterBedEnabled > 0)
            producers.Add(new Producer { GoodId = Catalog.SaltpeterId, RecipeName = "硝石丘", Productivity = 2.0 * coef.NiterBedProductivity * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
                Inputs = [new Input(Catalog.LivestockId, coef.CompostLivestockInput), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SaltpeterId))] });   // ★雇用の一本化（同上）
        // === 製法の時代差・地理差を表す代替レシピ（市場が安い方を採る・近代法は発見の表の行で門＝2026-10-01 段2A）===
        // ぼろ布製紙（紙61の早期レシピ）：麻+綿布のボロ布を漉く＝19世紀前半の製紙原料（木材パルプは1840s砕木/1860s化学パルプ）。
        // デフォルトの木材パルプ製紙(goods.json)は発見の表の木材パルプ製紙（1840年代）で門＝早期はこのぼろ布製紙が供給し、パルプが安くなると駆逐される。
        producers.Add(new Producer { GoodId = Catalog.PaperId, RecipeName = "ぼろ布製紙", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.HempId, 0.6), new Input(Catalog.CottonClothId, 0.4), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.PaperId))] });
        // ★2026-09-30 (129) 樹皮の粉末（キニーネの早期レシピ＝ぼろ布製紙と同じ「早期の橋」）：単離（1820・既定のレシピ＝樹皮0.5＋化学薬品0.3・解禁の表の既定の行 0.5）の前は、
        //   欧州の薬局が樹皮を挽いてワインで飲ませた（「イエズス会の粉」＝17世紀から）。同じ病に効くが、苦く嵩が張り吐き戻す・品位の低い樹皮と混ぜ物が多い＝効き目が低い。
        //   ⚠樹皮1.5（単離の3倍）＝史実に1本の換算率は無い（推奨で決めた）。下限は史実の帰結＝単離の後20年ほどで硫酸キニーネが薬局から粉末を駆逐した＝参照価格で
        //   粉末の単位費用（0.326＋0.54×樹皮）が単離（0.944）を上回る樹皮 1.14 より上。1.5 は樹皮が参照価格の 0.35 まで安くなるまで単離が勝つ幅（樹皮の値崩れの後は粉末が戻る＝
        //   1880年代のジャワの植林で樹皮が暴落した後、インドの「キナの解熱剤」〔精製しない混合アルカロイド〕が貧者の薬になったのと同じ向き）。
        producers.Add(new Producer { GoodId = Catalog.QuinineId, RecipeName = "樹皮の粉末", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CinchonaBarkId, 1.5), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.QuinineId))] });
        // 木炭製鉄/銅/鉛（鉄31・銅33・鉛34の代替）：コークス(石炭)の代わりに木炭(木)で還元＝石炭が乏しく森林が豊かな地域の製錬（スウェーデン/露ウラル/米の木炭高炉が19世紀半ばまで存続）。
        // 産出は低め＝石炭/コークスが安い地域ではコークス法が勝ち、石炭が高い地域でのみ木炭法が勝つ（燃料の地理が製錬法を分ける・倍率排除と同思想）。
        producers.Add(new Producer { GoodId = Catalog.IronId, RecipeName = "木炭製鉄", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.IronOreId, 1.0), new Input(Catalog.TimberId, 0.8), new Input(Catalog.LimestoneId, 0.25), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.IronId))] });
        producers.Add(new Producer { GoodId = Catalog.CopperId, RecipeName = "木炭製銅", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CopperOreId, 1.0), new Input(Catalog.TimberId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.CopperId))] });
        producers.Add(new Producer { GoodId = Catalog.LeadId, RecipeName = "木炭製鉛", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.LeadOreId, 1.0), new Input(Catalog.TimberId, 0.7), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LeadId))] });
        // クロムなめし革（革46の近代レシピ・発見の表の1880年代＝商用1884）：原皮＋クロム鉱を少量。タンニンなめし(木0.15)より投入質量が小さく速い＝onset後にコストで勝ちデフォルト(タンニン)を駆逐＝クロムの工業需要は晩期に立つ（史実：クロムは19世紀後半まで用途僅少）。
        producers.Add(new Producer { GoodId = Catalog.LeatherId, RecipeName = "クロムなめし", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.LivestockId, 0.5), new Input(Catalog.ChromeId, 0.08), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LeatherId))] });
        // 無煙火薬（火薬38の近代レシピ・発見の表の1880年代）：化学薬品(ニトロセルロース)＋硝石。高エネルギー密度＝総投入質量が黒色火薬(1.0)より小さい(0.55)＝少量で同威力＝onset後にコストで勝つ。
        producers.Add(new Producer { GoodId = Catalog.GunpowderId, RecipeName = "無煙火薬", Productivity = 2.0 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.ChemicalsId, 0.3), new Input(Catalog.SaltpeterId, 0.2), new Input(Catalog.CoalId, 0.05), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.GunpowderId))] });
        // 亜麻衣料（衣料57の代替）：綿布の代わりに帆布(麻織物)で仕立てる＝亜麻が安い地域・綿花が断たれた時の代替（市場が安い方を採る）。
        producers.Add(new Producer { GoodId = Catalog.ClothingId, RecipeName = "亜麻衣料", Productivity = 1.7 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CanvasId, 0.6), new Input(Catalog.DyeId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.ClothingId))] });
        // 石油灯油＝ケロシン（灯油68の近代レシピ・発見の表の1850年代＝1859ドレーク油井〜）：原油を精製。原油(安価RGO)は鯨油/魚油(漁獲)より安い＝onset後にコストで勝ち鯨油を駆逐（史実：ケロシンが捕鯨を終わらせた）。原油RGOも DefaultRecipeOnset(10)=0.6 でゲート＝早期に湧かないので原油の価格スパイクも起きない。
        producers.Add(new Producer { GoodId = Catalog.LampOilId, RecipeName = "石油灯油", Productivity = 1.9 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CrudeOilId, 1.0), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.LampOilId))] });
        // === B6（2026-07-19ユーザー裁定）===
        // 人絹＝ビスコース（絹織物45の並存レシピ・発見の表の1890年代）：木材パルプ(製材)＋化学薬品から人造絹糸＝
        // 化学工業が生糸(14)経済を創造的破壊（"artificial silk"そのもの・ぼろ布製紙/クロムなめしと同型・RefPrice不変）。
        // C-高8（2026-07-23）：旧0.95は統一尺度で1800年代半ば＝実用レーヨン1905より約60年早い。圧縮帯（自動車1.50・WWI財1.55・ラジオ1.60）へ整合。
        producers.Add(new Producer { GoodId = Catalog.SilkClothId, RecipeName = "人絹", Productivity = 2.2 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.LumberId, 0.4), new Input(Catalog.ChemicalsId, 0.5), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.SilkClothId))] });
        // 鉄筋コンクリート（住居126の並存レシピ・発見の表の1860年代）：セメント＋鋼の多層建築＝戸あたり資材(0.5)が
        // 木造漆喰(0.7)より小さい＝発見の後（旧＝onset後）にコストで勝ち都市の収容力を解放（無煙火薬と同じ「少量で同機能」の勝ち筋）。
        // C-高8（2026-07-23）：旧0.85は統一尺度で1800年前後＝エヌビック式1892・普及1900sより約100年早い。圧縮帯へ整合。
        producers.Add(new Producer { GoodId = Catalog.HousingId, RecipeName = "鉄筋コンクリート", Productivity = 1.6 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.CementId, 0.3), new Input(Catalog.SteelId, 0.2), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.HousingId))] });
        // === 1934拡張E2（2026-07-19ユーザー裁定）===
        // 合成ゴム＝ブナ/ネオプレン（ゴム50の並存レシピ・発見の表の1910年代）：化学薬品＋石炭から合成＝
        // 天然ゴム（賦存州＋加硫0.9ゲート）の地理独占を晩期に技術が破る（人絹・甜菜糖と同型の創造的破壊・RefPrice不変）。
        // C-高8（2026-07-23）：旧1.10は統一尺度で1815年前後＝実用合成ゴム（ブナ1930s）より約120年早い。ラジオ(1.60)の後へ。
        producers.Add(new Producer { GoodId = Catalog.RubberId, RecipeName = "合成ゴム", Productivity = 1.8 * FullChainProductivityScale, Output = 0.5, Scale = 0.05, MarginalCost = Catalog.FactoryVa, LaborPopIndex = workerIdx, OwnerPopIndex = ownerIdx, RentShare = 1.0, BuysLabor = true,
            Inputs = [new Input(Catalog.ChemicalsId, 0.5), new Input(Catalog.CoalId, 0.3), new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, Catalog.AdvLaborPerOutput(Catalog.RubberId))] });
    }

    // 階級（代表スケール 1単位＝1000人）。貯蓄＝サイズ×階級別の一人当たり富。識字・自給は設定から。
    private static Pop MkPop(int id, string name, bool urban, double size, double lit, double wealthPer, bool closed = false, bool modern = false, double subs = 0)
        => new() { ClassId = id, ClassName = name, IsUrban = urban, Size = size, Savings = size * wealthPer, Satisfaction = 1.0, Literacy = lit, Closed = closed, IsModernElite = modern, Subsistence = subs };


    /// <summary>植民地の州昇格（2026-08-09・作者裁定2026-08-08「植民地州昇格＝実装」・設計原典＝docs/202607/20260731_植民地のラベル化_部分完了.md §3-3d）。
    /// 入植者が過半数に達した植民地（<see cref="Colonial"/>.Step §5 が判定）を、宗主の**完全な州**（pop/生産者/市場・RegionTier.Integrated）として実体化する。
    /// ── 保存則（思想⑥）＝湧かさない ────────────────────────────────────────
    ///   人口＝現地住民(col.PopSize)だけ。骨格階級は全て Size0（辺境の兵士Size0と同じ作法）＝社会移動・国内移住が埋める。台帳＝PopulationDelta へ全額。
    ///   貨幣＝持ち込まない（植民地の蓄え col.Savings・入植者の持参金 col.SettlerSavings は 2026-09-30 段4g に経路ごと削除＝作者裁定 Q12＝どちらも常に0だった）。
    ///   ★入植者（col.SettlerSize）も同じ回に削除＝Colony オブジェクトの入植は国の植民地の一覧〔ゲームの間ずっと空〕の上でしか走らなかった。
    /// ── 同名の州が既に在る（J-006＝同じ領土を既に州として保有）とき（回1-2・2026-09-27）──────────────
    ///   新しい州は作らず、住民をその州の同じ階級へ運んでその州を返す（作者裁定 2026-09-26「住民が消えるのは好ましくない」。旧＝呼び手が Colony を捨て、住民も消えた）。
    /// ── 設計判断（記録・docs/202608/植民地州昇格_2026-08-09_完了.md【完了】.md）───────────────
    ///   ・農制＝自作農（farmLordShare=0）＝北西部条例/ホームステッドの物理。プランテーション上書き（PlantationRentShare）も適用しない（昇格の定義＝入植者多数の州）
    ///   ・市場在庫＝0 で開始（CatalogEconomy の初期200を持たせない）＝「AI国庫×初期市場在庫200の開幕工業化バースト」（indprobe実測2026-07-27）を新州で再現しない
    ///   ・Tier既定=Integrated（議席あり・植民地維持費なし。tier 引数で Colonial も渡せる＝開始時の一括昇格）・Core にはしない（「ゲーム中のコア化はしない」の規則どおり＝HomeProvinces1914 計器の分子定義を守る）
    ///   ・港州（IsPort）＝⚠是正2026-08-28：landmass.json の coastal（沿岸か否かの一次データ）から引く（未登録は旧挙動どおり true へ倒す）。座標は Climate.CityCoord（史実の積出港・landmass 機械検証済み）。既存内陸州の最寄り港は再配線しない（既存州の挙動を動かさない。⚠PortRewireEnabled>0.5 では MultiNationSim.RewirePorts が四半期末に WirePortDistances を張り直す＝新港が最寄りなら切り替わる・doc 20260812_港の配線を領土に追随させる）
    ///   ・州の Culture/Religion＝首都州と同じ（入植者多数＝本国文化が主流になった州。現地住民の民族の実体は CoreEthnicShare=0 が担う＝分離圧力・同化・国民意識が既存機構で働く）</summary>
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
            // ★産地の発見（2026-10-01 (148)）＝BuildProvince と同じ規則（場所＝植民地の名前）。
            int site = pr.IsRgo ? SiteRowFor(col.Name, pr.GoodId, coef, !w.StepsDiplomacy) : -1;
            if (site >= 0 && System.Array.IndexOf(col.Resources, pr.GoodId) < 0)
            {
                ApplyEndowment(pr, ResourceEndowment(pr.GoodId) * coef.ResourceEndowmentScale, coef);
                pr.SiteRow = site;
            }
            else if (System.Array.IndexOf(col.Resources, pr.GoodId) >= 0)
            {
                pr.SiteRow = site;
                ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, !w.StepsDiplomacy) * coef.ResourceEndowmentScale, coef);   // 植民地の固有資源＝そのまま州の賦存へ（一次データ）。BuildProvince(769)と同じ ScarceEndowment＝ScarceMineralGate=0 なら ResourceEndowment と厳密同値（⚠既定は点火済みの1.0＝2026-09-05・x-02 で注記を是正）
                // ★B5（実バグ21件・2026-09-07）：本国の賦存州は PlantationRentShare（live 0.6）で大農園の地代を当てるのに、植民地格の昇格州は自作農のまま＝史実の逆
                //   （ベンガル/ジャワ/カリブは大農園）。Colonial 格のとき賦存RGOに同じ上書き・持ち主の口座＝資本家（入植の農園主＝開いた階級・0から流入で育つ）。
                // ★2026-10-02 (173)（R1＋c6＝植民地の損益の診断の c6「B5 点火」・推奨で進めた）：休眠の旗 ColonyPlantationRent を焼き込んだ＝係数ごと削除。
                //   上書きは**輸出作物**（主食でない賦存＝Colonial.ExportCrop・Colonial.AbsenteeRgo と同じ判別）だけ＝主食は現地の地主のまま（作者 2026-09-29「主食の生産は現地の地主のまま」・
                //   メモリの要約）＝自作農の床 0.3・持ち主は農民（「現地の地主＝今の持ち主〔農民〕のまま」は AI の読み・(177) 反証役 vc1#7＝地代の記録〔LandRent〕を首都へ移すのも AI の設計＝植民地の州の地税の課税ベースから外れる）。輸出作物の地代は植民地格の間は不在地主（本国の首都の投資家）へ送る（EconomyStep の④・InternationalMarket.PaySales）。
                if (tier == RegionTier.Colonial && pr.IsRgo && Colonial.ExportCrop(pr.GoodId) && coef.PlantationRentShare > pr.RentShare)
                {
                    pr.RentShare = coef.PlantationRentShare; pr.OwnerPopIndex = 3;
                }
            }
            else if (!w.StepsDiplomacy && coef.ScarceMineralGate > 0 && Catalog.IsScarceMineral(pr.GoodId, coef)) pr.Productivity = 0;   // ★共有世界だけ（2026-09-06）   // ★同上（植民地→州の昇格側）
            else if (pr.IsRgo && !Climate.StapleSelfSufficient(pr.GoodId, clat, clon)) ApplyEndowment(pr, ResourceScarcity * coef.ResourceScarcityScale, coef);
        }
        // 階級構成：実人口は現地住民だけ（保存則＝湧かさない）。（入植者の着地〔農民/労働者〕は 2026-09-30 段4g に入植の経路ごと削除＝作者裁定 Q12）
        double natives = Math.Max(0, col.PopSize);
        double peasantSize = natives;
        // 現地住民の自給率：植民地POPは元々市場の外で食っていた（台帳の外）＝自給農。首都の農民自給率を下回らない床0.5
        double subs = 0.5;
        foreach (var cp in w.Region.Pops) if (cp.ClassId == PopClass.Peasant) { subs = Math.Max(subs, cp.Subsistence); break; }
        var nobility  = MkPop(0, "貴族",       false, 0, 0.5, 0, closed: true);
        var peasant   = MkPop(1, "農民",       false, peasantSize, 0.3, 0, subs: subs);
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
            peasant.CoreEthnicShare = 0.0;   // 現地住民＝宗主の中核民族ではない
        }
        worker.CoreEthnicShare = 1.0;   // 労働者枠（器）は宗主の中核民族
        // ★J-006 の吸収（回1-2・2026-09-27・作者裁定 2026-09-26「住民が消えるのは好ましくない」）：同名の州を既に持つ＝同じ領土を既に州として保有している
        //   （一度州になった領土は Colony に戻さない＝J-006「止める」2026-09-05）。旧＝呼び手が Colony オブジェクトを捨て、住民も州へ入らずに消えた。
        //   新＝新しい州は作らず、上で組んだ住民（現地の農民＋入植の農民・労働者＝蓄え・識字・不満・中核民族の割合）をその州の同じ階級へ運ぶ
        //   （運搬は州間の移住・入植と同じ1本＝Territory.MoveBetweenRegions）。記帳は新しい州のときと同じ（台帳の外から入った分だけ）。
        if (w.Regions.Find(x => x.Name == col.Name) is { } same)
        {
            double pop0 = same.SumPopulation();
            // （支払いの一元化 段2-2 の A12「受け手の階級が無いと蓄えが運ばれず消える」【推】は確かめて外した＝2026-09-29：実国の州を作る経路〔BuildProvince・辺境・
            //   植民地の州昇格〕は全て農民と労働者の階級を持つ＝同名の州の吸収で受け手が欠けることは無い）
            foreach (var mp in new[] { peasant, worker })
                if (mp.Size > 1e-9 && same.PopOf(mp.ClassId) is { } dp) Territory.MoveBetweenRegions(mp, dp, mp.Size, coef);
            double inP = same.SumPopulation() - pop0;
            w.PopulationDelta += inP;   // 貨幣は持ち込まない（上の注記＝蓄えは 2026-09-30 段4g に削除＝旧の貨幣の台帳の行は常に +0）
            same.AnnexBaggage += inP;   // 案Y（観測専用）：併合の持ち込み人口＝本土の自然増ではない（新しい州のときと同じ記帳）
            return same;
        }
        var soldier = new Pop { ClassId = 5, ClassName = "兵士", IsUrban = false, Satisfaction = 1.0, IsSoldier = true, Ages = Pop.SoldierAges(), Literacy = peasant.Literacy };   // Size0の器（辺境と同じ作法）＝軍事費が平時均衡で徴募
        var pops = new List<Pop> { nobility, peasant, worker, bourgeois, engineer, clergy, teacher, bureaucrat, doctor, merchant, artisan, servant, soldier };
        // ★段1 器 EduProducersCoexist（2026-09-12）：昇格植民地州も本国州と同じ2件並置を受ける（coef を渡す・researcherIdx は既定 -1 のままなので大学の枝は不変＝0 で厳密恒等）。
        //   渡さないと >0 の世界で StepMonth が [eduIdx+1] を公立学校と誤認する（実体は医療）＝型の統合が起こす静かな破損の型。
        int eduIdx = AddServiceProducers(producers, pops, workerIdx: 2, peasantIdx: 1, teacherIdx: 6, bureaucratIdx: 7, doctorIdx: 8, coef: coef, clergyIdx: 5);
        AddAdvancedProducers(producers, pops, engineerIdx: 4, workerIdx: 2, ownerIdx: 3, coef);   // ★2026-09-26：渡された coef を使う（旧＝ここだけ既定の係数を新しく作っていた＝国ごとの係数・テストの w.Coef.X= が植民地州の高度な生産者に届かなかった・col-26）
        AddArtisanProducers(producers, artisanIdx: 10);
        producers.Add(new Producer { GoodId = Catalog.DomesticServiceId, Productivity = 1.0, Output = 0, MarginalCost = 0.3, LaborPopIndex = 11, OwnerPopIndex = 11, RentShare = 0.0 });
        AddAlternateRecipes(producers, coef);   // ★代替レシピ（2026-09-06）：全ての州の構築経路で同じ1回
        var region = new Region
        {
            // ⚠是正2026-08-28：旧＝無条件 IsPort=true＝**内陸の植民地（内陸ベンガル・ボゴタ等）も海港になっていた**。
            //   沿岸かどうかは landmass.json の coastal（一次データ）から引く。
            // ⚠是正2026-09-02（G03監査#2）：未登録名を旧挙動どおり true（港）へ倒すのは fail-open で同じ穴が再発する
            //   （内陸植民地が landmass.json 未登録なら再び海港になる）＝fail-closed（false）へ倒し、ログで沈黙を避ける。
            Name = col.Name, IsPort = Landmass.CoastalOf(col.Name, fallback: false),
            Tier = tier, Dominance = 0.0, Malarial = col.Malarial,   // 支配度0＝取った年から数える
            Rule = [0.0, 1.0, 0.0, 0.0],   // ★支配度の一般化 段1：昇格した植民地＝間接統治（在地1.0）から始める（段2で支配度と同じ物理で国家区分が育つ）
            RuleInit = [0.0, 1.0, 0.0, 0.0],   // ★E-06 昇格した植民地＝支配度0から積み上げる（引き留め>圧力で育つ）   // ★格は呼び手が決める（入植者昇格＝Integrated／開始時の一括昇格＝Colonial）
            Terrain = Places.TerrainOf(col.Name),   // ★(3) 地形タグ（昇格植民地も同じ1枚の表から）
            Resources = [.. col.Resources], Culture = w.Region.Culture, Religion = w.Region.Religion,
            ArableLand = Math.Max(1.0, peasantSize) * LandHeadroom,   // 本土州と同じ規則（耕地賦存＝初期農民×余裕）
            CapitalEnabled = true, Industrialization = 0.0,
            CulturalGoodId = w.Region.CulturalGoodId,
            InvestorPopIndex = 3, MeritFromIndex = 2, MeritToIndex = 4,
            ClergyPopIndex = 5, TeacherPopIndex = 6, BureaucratPopIndex = 7, DoctorPopIndex = 8, MerchantPopIndex = 9, ArtisanPopIndex = 10, ServantPopIndex = 11, SoldierPopIndex = 12,
            EducationProducerIndex = eduIdx,
            ColonialResources = [],   // 植民地財の産出は首都州（Id0）のみが担う（BuildProvince と同じ）
            Goods = goods, Pops = pops, Producers = producers, Markets = markets,
        };
        region.SeaBasin = SeaGraph.BasinAt(clat, clon);   // 港州＝海盆を積出港座標から（WirePortDistances の港側と同じ規則）
        if (!Landmass.IsRegistered(col.Name)) w.LogAdd($"{w.Year} {col.Name}州はlandmass.json未登録＝IsPort=falseへ倒した（沈黙回避）");   // fail-closed の可視化
        foreach (var m in region.Markets) { m.Stock = 0; m.PrevStock = 0; }   // 在庫0で開始（上記の設計判断）。Last*=0 は初期値どおり＝財収支恒等式（GoodsBalance）が最初から閉じる
        Territory.Adopt(w, region);   // Id＝国の中で最大＋1（移し替えと同じ1本・2026-09-28 処理の一元化 段2 B）
        // 保存の記帳（同一値を両辺に使う＝CedeTerritory の作法）
        double P = region.SumPopulation();
        w.PopulationDelta += P;       // 現地住民＝台帳の外から（ゲーム外流入の作法）。貨幣は持ち込まない（蓄えと持参金は 2026-09-30 段4g に削除＝旧の貨幣の台帳の行は常に +0）
        // 案Y（人口物差しの脱汚染・2026-08-21）：植民地の州昇格＝**本土の自然増ではない**外部人口の流入（帯の定義＝本土人口。
        // 葡帯の注記「ブラジル喪失は植民地＝本土人口に非算入」と同じ物差し）。持ち込み分Pを記帳し PopGrowth 分子から除く（昇格後の自然増は自国分）。
        // 以後この州はこの国の故地扱い（OriginPowerId）＝将来の喪失→再征服の往復で二重記帳しない。観測専用＝力学は読まない。
        region.OriginPowerId = w.SelfPower?.Id ?? -1;
        region.AnnexBaggage = P;
        return region;
    }

    public void Run(int quarters)
    {
        for (int q = 0; q < quarters; q++) StepQuarter();
    }

    /// <summary>四半期＝90回の日次刻み。三層時計の合成（戦略前段→3×月次決算→戦略後段）はそのまま StepDay 内の境界で発火する
    /// ＝テスト・self-play は従来どおりこのメソッドを呼べばよい（合成順・RNG順は不変、変わるのは価格形成が日次になった点のみ）。
    /// ★2026-09-26 A3：価格形成は旬次（<see cref="Coefficients.PriceFlowStepDays"/> 日ごとに h 日ぶんを1歩）。公開の <see cref="StepDay"/> は1日のまま。</summary>
    public void StepQuarter() { for (int d = 0; d < Calendar.DaysPerQuarter; d++) StepDay(); }

    /// <summary>1日進める（三層時計の最小単位）。日次＝価格形成の鼓動（価格が動くのは旬の最終日＝A3）。30日ごとに月次決算、90日目に戦略。四半期が締まった日に true。</summary>
    /// <summary>1日を進める（単独呼び用）。多国（MultiNationSim）は3相を別々に呼ぶ＝
    /// StepDayOpen（境界・逐次）→ StepDayDaily（純粋日次・並列可）→ StepDayClose（境界・逐次）。
    /// この一体呼びは3相を順に呼ぶだけ＝単国経路（テスト・leakhunt）は分割前とビット恒等（2026-08-29 高速化裁定）。</summary>
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

    /// <summary>★信用の「月の中の定数」（2026-09-26 A2-1・結果を変えない高速化）＝名目GDP・大衆の平均満足・平均工業化。
    /// 旧＝<see cref="Credit.Step"/> が毎日・各国で3つとも全件集計していた（作業スレッドの標本の約3割＝`World.get_Gdp`）。
    /// 3つの入力（産出・州の状態・州とPOPの出入り・POPの規模と満足と身分・工業化・参照価格・物価水準）を書く行は、どれも
    /// 月次決算・四半期の首と末・共有層（外交・国際市場・移民）の中にあり、日次相（価格形成・建設・信用）はどれも書かない
    /// ＝月の初日の日次で求めた値は、月の残りの日に毎日求める値とビット単位で同じ。
    /// ⚠ 境界相の外から入力を書く動詞はプレイヤーの侵略（<see cref="World.Invade"/>＝兵の疫病死と植民地の州化）だけで、そこで表から外す。
    /// ⚠ 置き場＝静的な弱参照の表（World が消えれば消える）＝セーブに載らない（SaveGame はインスタンスのフィールドだけを辿る）。
    ///   表に無いとき（ロード直後・新しい国）は、その日に求める。並列の日次から書くが、鍵は自国の World だけ（他国の値を読まない）・表はスレッド安全。</summary>
    internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, System.Runtime.CompilerServices.StrongBox<(double NominalGdp, double AvgSat, double MeanInd)>> CreditMonth = new();

    /// <summary>純粋な日次（価格形成・建設・信用）＝**国ローカル**＝並列可。実測74%の時間がここ（timeprobe 2026-08-29）。
    /// ⚠不変条件＝自国 World/Region 以外に触れない・乱数を引かない・共有状態（Diplomacy/他国World/静的可変）へ書かない。
    ///   これが並列化の根拠なので、共有へ触る処理を足すなら StepDayClose 側へ。番人＝armstrace の並列/逐次ビット一致。
    ///   （例外は <see cref="CreditMonth"/> だけ＝静的な表だが、書くのも読むのも自国の World の鍵だけ・表はスレッド安全。）</summary>
    public void StepDayDaily()
    {
        // 日次（鼓動）：価格形成・建設の進捗・信用。毎日わずかに動く層。
        // ★価格形成は旬次（2026-09-26 A3）：旬の最終日（(日＋1)%h==0＝h=PriceFlowStepDays）にだけ MarketFlow を呼び、h 日ぶんを1歩で進める。
        //   h は30の約数だけ＝旬は月をまたがない＝月次決算（日29/59/89 の締め）と四半期首の AI が読む価格は、今と同じ「その月の30日ぶん寄せた価格」。
        //   助走も同じ判定（助走は StepQuarter＝この日次を通る）。州への写しは毎日のまま。新しい状態は持たない（_dayInQuarter は既にセーブに載る）。
        //   ⚠ 旬の0〜(h−2)日目に価格を読むもの（建設の建材4財・画面）は、前の旬の価格を読む（遅れは最大 h−1 日）。
        // ★建設と信用も旬次（2026-09-30 高速化の段 S3c・近似）：旬の初日（日%h==0）に h 日ぶんをまとめて進める。
        //   根拠＝どちらの入力も境界相（月次決算の支払い率・月の定数）か旬の中で動かない価格（建材4財）で、旬は月をまたがない
        //   ＝旬の中で変わるのは自分の状態（工事の残り日数・信頼・与信の残高）だけ。建設＝働く日数 n＝min(h, 完成までの日数)ぶんの工賃と進捗を1回で
        //   （旬の最終日だけ価格が1旬古い＝建材の採算率の遅れは旧の h−1 日から h 日へ）。信用＝信頼と与信の緩和を h 日ぶん閉じた形で（1日の縮み 1/150〜1/1500＝h≪時定数）。
        //   月次決算が読む月末の値は旧と同じ日数ぶん（完成・工賃・与信）。h=1 なら旧来の日次と1ビットも変わらない。
        int hFlow = World.Coef.PriceFlowStepDays;
        if (hFlow < 1 || Calendar.DaysPerMonth % hFlow != 0)
            throw new System.InvalidOperationException($"PriceFlowStepDays={hFlow} は {Calendar.DaysPerMonth} の約数でない（旬は月をまたげない）");
        bool priceDay = (_dayInQuarter + 1) % hFlow == 0;
        bool spanStart = _dayInQuarter % hFlow == 0;   // 旬の初日（建設と信用＝S3c）
        long[]? cost = priceDay || spanStart ? World.Cost : null;   // 計算量の台帳（診断・この国の台帳＝並列の日次相でも国ごと）
        bool monthStartSpan = _dayInQuarter % Calendar.DaysPerMonth < hFlow;   // ★月の最初の旬（2026-09-30 計算量の予算 第2回）＝張り付いた財の眠りの印を作り直す旬（MarketFlow.StepDay の remarks）
        foreach (var r in World.Regions)
            if (r.State == RegionState.Active)
            {
                r.PriceLevel = World.PriceLevel * r.LocalPriceMultiplier;   // 国家物価×地域乗数＝地域別の物価水準（金融中心は高・辺境は低）。1州なら乗数1＝較正中立
                r.FarmRentCap = System.Math.Min(RentCap(World.LandReform, World.Coef.LandReformRentCap), RentCap(World.SerfEmancipation, World.Coef.SerfEmancipationRentCap));   // 地代上限＝土地改革/農奴解放の段階で（限定的＝なしと全面の中間）。元の地代がこれ未満なら効かない
                r.CrowdingPremium = System.Math.Max(0, World.GovBondRate - World.Coef.BondBaseRate);   // 政府借入の金利プレミアム→民間投資ハードルへ（クラウディングアウト。金本位の信用国は GovBondRate が低く弱い＝条件付き）
                r.FinancialHeat = World.FinancialHeat;   // 与信過熱を州へ複製（改修2026-06-28・投資ハードルのバブル項へ）
                //（借入スタックの出費上限 SpendingCapFactor の州への複製は 2026-09-28 に撤去＝作者裁定 Q9・補助は国家の支払い率 r を World から読む）
                r.CivilWarActive = World.InCivilWar;   // ★B3：述語1本（CivilWarPredicateUnified=1.0＝2026-09-08点火＝War で戦う内戦もここへ届く）      // 内戦中を州へ複製（#136 内戦の経済麻痺の読み口。CivilWarEconomyDisruption=0で不読＝恒等）
                if (priceDay) MarketFlow.StepDay(r, World.Coef, hFlow, cost, monthStartSpan);   // 物価水準を複製し需給を読み、旬の h 日ぶん価格を少しずつ動かす（月の2旬目以降は不動点に着いた張り付いた財を眠らせる）
            }
        ProbeCheck("日次価格");
        if (spanStart) World.StepConstruction(hFlow);                               // 工場建設の進捗（旬の初日に h 日ぶん・労務費を働いた日数ぶん払って進む）
        ProbeCheck("日次建設");
        if (spanStart)
        {
            (double NominalGdp, double AvgSat, double MeanInd)? creditMonth = null;
            if (World.Bank is not null)   // （2026-09-29 段2-2 から全ての世界に銀行がある＝常に真・盤面の直書きで外したときだけ偽）
            {
                bool fresh = !CreditMonth.TryGetValue(World, out var box);
                if (box is null) { box = new(); CreditMonth.Add(World, box); }
                if (fresh || _dayInQuarter % Calendar.DaysPerMonth == 0)   // 月の初日は旬の初日（h は30の約数）
                    box.Value = (World.NominalGdp, NationalScore.MeanSatisfaction(World), World.MeanIndustrialization);
                creditMonth = box.Value;
            }
            Credit.Step(World, World.Coef, creditMonth, hFlow);                     // 信用（与信・信頼）も旬次（旬の初日に h 日ぶん・速度は日数配分）
            cost![CostLedger.CreditSpans]++;                                         // 計算量の台帳（診断）：信用は国×旬
        }
        ProbeCheck("日次信用");
        // 助走中は暦を進めない（World.WarmupFrozen・2026-08-10）。Tick は「開始からの経過日数」＝年境界の唯一の読み手が
        // 豊凶ロール（MonthSettle の harvestYear）なので、止めれば助走が何年ぶんでも天候は1783年の1回きり＝時間が経たない。
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

    /// <summary>保存量プローブ（leakhunt・恒久計測器）：保存量 D＝基礎貨幣−累積台帳（貿易/鋳造/不換発行）は内部処理のどこでも不変のはず。
    /// 動いたサブステップ＝台帳記帳漏れの現場を stderr へ出す。既定OFF＝実行ゼロ（Runner の leakhunt コマンドが有効化）。
    /// 使い方＝`dotnet PriceOfBread.Runner.dll leakhunt <seed> [四半期]`。過去の実績：汚職Distribute蒸発（2026-07-07）を成分分解まで3手で特定。</summary>
    public static bool ConservationProbe;
    private double _probeDLast = double.NaN;
    internal void ProbeCheck(string label)
    {
        if (!ConservationProbe) return;
        double now = World.BaseMoney - World.CumulativeTradeFlow - World.CumulativeGoldMinted - World.CumulativeFiatIssued;
        if (!double.IsNaN(_probeDLast) && Math.Abs(now - _probeDLast) > 1e-4)
            Console.Error.WriteLine($"[PROBE国内] {label} {World.Region.Name} {World.Year}年T{World.Tick} ΔD={now - _probeDLast:F6}");
        _probeDLast = now;
    }

    // 四半期の戦略前段：金融ショック・AIの政策決定・政府の政策反映・建設の進行。
    private void QuarterStart()
    {
        World.Cost[CostLedger.QuarterStartPops] += ActivePopCount();   // 計算量の台帳（診断）：四半期首（AI・議会・政府の反映）は POP×国に比例
        // 仕込みの金融ショックは「暦のn四半期目に起きる事件」＝助走では起こさない（2026-08-10）。
        // ⚠助走は _quarterIndex を進めないので、ガードが無いと ShockQuarterIndex=0 のシナリオで**助走の全四半期**に発火する。
        // 実ゲーム/自己対戦は ShockQuarterIndex=-1（なし）＝この行は現状どちらの経路でも不発だが、罠を残さない。
        if (!World.WarmupFrozen && _quarterIndex == _shockQuarter && World.Bank is not null)
            World.Bank.Confidence = Math.Max(0, World.Bank.Confidence - _shockMagnitude);
        // ★2026-08-02：近代化投資を**自動化**する（作者指示「AIと同じ挙動に」）。
        //   （汚職根絶の自動化は C2-C5撤去2026-08-16 で機構ごと物理削除＝V2恒久ONで根絶費の動詞は死骸だった）
        // ⚠★置き場所は DecideQuarter の**前**。後ろに置くと**AIの判断を上書きしてしまう**（AIはスタンス別の率を使うのに、
        //   ここは一律の近代化率）＝AI挙動を変えない原則に反する。前に置けば、AIが動かす国はAIの値で上書きされる。
        // ⚠★多国世界（SelfPower あり＝実ゲーム/自己対戦）に限る。単一国の較正・テストは AI 経路そのものが無く、
        //   ここで政策を書くと「既定0では何も起きない」という恒等が壊れる（実測でテスト2件が赤になった）。
        if (World.SelfPower is { } self)
        {
            double fs = UtilityAgent.FiscalStressOf(World);
            // ★近代化投資はプレイヤー国を除外する（2026-08-10・財政タブのスライダー復活）。
            //   ⚠旧＝**全国**に毎四半期この一律値を代入していた。AI国は直後の DecideQuarter が自分の値で上書きするので無害だったが、
            //     NullAgent のプレイヤー国には上書きが来ない＝**UIで動かしても次の四半期に必ず 0.08 へ戻る**（スライダーが効かない）。
            //   ⚠判定は Power.IsPlayer＝UI（GameViewModel が Powers[playerIndex].IsPlayer=true を立てる）と Runner の warmtest、
            //     および一部のプレイヤー経路テスト（ElectionEventTests・ElectionAiTests・MultiNationTests）が立てるフラグ。
            //     自己対戦（SelfPlay）・較正・その他のテストは IsPlayer を立てない＝この分岐は偽＝AI挙動はビット単位で不変。
            //   プレイヤー国の初期値は MultiNationSim が UtilityAgent.OpeningModernizationFrac で播く（AIと同じ出発点）＝以後はUIの値を尊重。
            if (!self.IsPlayer) World.ModernizationFrac = UtilityAgent.AutoModernizationFrac(World, fs);
            // ★陸海の比もプレイヤーを地理に乗せる（作者裁定2026-09-23＝財政タブの「海軍の比率」レバーを撤去）。
            //   2026-08-04 作者仕様(5)「陸海の割り振りは**廃止**」・2026-08-10「陸軍と海軍の注力レバーは廃止」＝**誰も比を振らない**が本来の姿で、
            //   2026-09-20 に「比」1本だけが復活していた。AI は DecideQuarter の中で同じ関数を通る＝ここは**同じ関数を同じ引数で**呼ぶだけ
            //   （割り振りの式はどこにも書き写さない）。合計（軍事費の段）は保たれ、比だけが地理で決まる＝AI・プレイヤー等価。
            //   ⚠AI国はこの枝に入らない（!IsPlayer 側は従来どおり DecideQuarter が呼ぶ）＝自己対戦・較正はビット恒等（プレイヤーが居ない）。
            else UtilityAgent.SetNavalUpkeepShare(World);
        }
        _agent.DecideQuarter(World);
        UpdateRegionalPriceLevels();           // 地域別の貨幣的物価乗数（地域物価・名目賃金の格差）を四半期ごとに更新。日次の StepDay が r.PriceLevel に適用
        Parliament.Step(World, World.Coef);   // 議会・選挙（段5）：得票・選挙・議席。目標は書かない（法軸の綱領は 2026-09-26 に撤去＝回5-1 項目17・AI の意図へ移設／経済軸は 2026-09-19）。ParliamentEnabled の国のみ＝較正不変
        World.ApplyModernizationInvestment();      // 近代化投資（UI・AI 共通の動詞）
        Government.Step(World, World.Coef);   // 政策を反映（法整備＝即時／スライダー＝利益集団の力で律速して漸進）
    }

    /// <summary>地域別の貨幣的物価水準の乗数（②集計粒度）：各州の1人あたり貯蓄（手元現金の厚み）が国平均からどれだけ乖離するかで、
    /// 国家物価水準（World.PriceLevel）を上下させる。現金の厚い金融中心州は物価・名目賃金が高く、現金の乏しい辺境州は低い
    /// （19世紀の地域物価/賃金の格差・辺境のデフレ）。1州なら州=国平均で乗数1.0＝較正中立。日次の MarketFlow が r.PriceLevel として価格帯/参照価格アンカーに使う。</summary>
    internal void UpdateRegionalPriceLevels()   // internal＝番人テスト（InvariantTests 現金が尽きた州…）が直接呼ぶ。可視性だけの変更
    {
        var c = World.Coef;
        bool priceOn = c.RegionalPriceLevelWeight > 0;   // 無効＝全州一律（従来挙動・較正中立）。労働価格プレミアムの簿記は常に更新（使用は OpportunityWageWeight でゲート）
        double natSav = 0, natEarn = 0, natPop = 0;
        foreach (var r in World.Regions) if (r.State == RegionState.Active)
            foreach (var p in r.Pops) { natSav += p.Savings; natEarn += p.Earned; natPop += p.Size; }
        double natPerCap = natPop > 1e-9 ? natSav / natPop : 0;
        double natEarnPerCap = natPop > 1e-9 ? natEarn / natPop : 0;   // 国平均の1人あたり月次所得（QuarterStart時点＝直前月）
        foreach (var r in World.Regions)
        {
            if (r.State != RegionState.Active) { r.LocalPriceMultiplier = 1.0; r.WagePremium = 1.0; continue; }
            double rSav = 0, rEarn = 0, rPop = 0;
            foreach (var p in r.Pops) { rSav += p.Savings; rEarn += p.Earned; rPop += p.Size; }
            double mult = 1.0;
            if (priceOn && natPerCap > 1e-9 && rPop > 1e-9)
            {
                double rPerCap = rSav / rPop;   // 州の1人あたり貯蓄（手元現金の厚み）
                // ⚠否定形のガード（2026-09-21・NaN破綻の修正）：現金が尽きた州では州合計の貯蓄が丸め残差で**極小の負値**になり得る
                //   （実測＝本国を全て失い植民地3州に縮んだスペインのエルパソ州・1831年・rSav=−8.3e-16＝資本家popの−8.9e-16が残っただけ）。
                //   Math.Pow(負, 0.3) は NaN・Math.Clamp は NaN を除去しない（既知の罠）ので、NaN が
                //   州物価乗数→州物価→MarketFlow の価格→専売益→国庫→popの貯蓄 と伝播し「貯蓄[貴族] が NaN」で落ちていた。
                //   退避値は分母の意味から決まる＝現金ゼロの州は「国平均に対して最も薄い」＝辺境デフレの底（RegionalPriceLevelMin）。
                //   rPerCap==0 の既存経路も Pow(0,w)=0→Clamp で同じ床に落ちていた＝正値では1ビットも動かない。
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
        if (pr.ConstructionLeft > 0) claims += pr.ConstructionPayPerDay * Calendar.DaysPerMonth;   // 工事の工賃の1か月分（World.StepConstruction）
        double loss = pr.LastCost - pr.LastRevenue;
        if (loss > 0) claims += World.StateProfitShare(pr, r, w.Coef) * loss;   // 国営持分の赤字の見込み（先月の損益）
    }

    /// <summary>稼働中の州の POP の件数（計算量の台帳の四半期の計数だけが読む＝診断）。</summary>
    private int ActivePopCount()
    {
        int n = 0;
        foreach (var r in World.Regions) if (r.State == RegionState.Active) n += r.Pops.Count;
        return n;
    }

    // 四半期の戦略後段：技術・制度・政治・社会移動・景気循環トラッカー（90日目）。
    private void QuarterEnd()
    {
        World.Cost[CostLedger.QuarterEndPops] += ActivePopCount();   // 計算量の台帳（診断）：四半期末（技術・制度・政治・社会移動ほか）は POP×国に比例
        // 開始前の助走（World.WarmupFrozen・2026-08-10）＝**時間が経たない**四半期。
        // 止める＝技術の前進・膨張意欲の累積・四半期スナップショット・暦（Quarter/Year/_quarterIndex）。
        // 回す＝制度の施行・社会移動・景気循環・物価水準＝価格と生産が実勢へ落ち着くための経済側。
        bool warm = World.WarmupFrozen;
        if (!warm) TechEducation.GrowTech(World, World.Coef);          // §2.8 技術＝社会の連続創発（カード廃止）。助走中は WarmupTech が別途担う＝ここで二重に進めない
        // ★発見（段2A・2026-10-01）：単独の世界だけここで判定する（伝わり＝0）。多国は MultiNationSim の四半期末の共有層が接触を求めてから判定する（同じ関数）。助走は判定しない（開幕の既知は助走の終わりに1度）。
        if (!warm && World.StepsDiplomacy) { Discovery.AdvancePrice(World, World.Coef); Discovery.Step(World, null, World.Coef); }   // ★(159) 平滑化した値段は判定の段の頭で1歩（判定は読むだけ）
        else if (warm && World.StepsDiplomacy) World.DiscoveryLossQ = null;   // ★(153) 損耗の記帳は判定で捨てる（助走は判定しない＝捨てるだけ・多国は MultiNationSim の共有層が捨てる）
        // （研究C＝制度の施行 Institutions.Step は 2026-10-01 (152) に削除＝特許・株式会社・鉄道は発見の表の行の2値〔World.PatentLaw ほか〕を読むだけ）
        Politics.Step(World, World.Coef);                   // §9-E
        Meritocracy.Step(World, World.Coef);                // 社会移動の全体（昇格・転落・聖職者の流入と下限＝①〜⑤）
        if (!warm) World.TickExpansionWill();                // 膨張意欲の累積（案Y・段B）。助走で10年ぶん貯めて開幕に侵略が噴くのを防ぐ＝時間の経過そのもの
        World.UpdateGdpCycle();   // 景気循環トラッカー更新（次四半期のAIの反景気循環的金融政策の入力）
        World.UpdatePriceLevel(World.Coef);   // 物価水準（数量説）＝広義マネー/実質産出の伸び差でインフレ/デフレ
        if (!warm) Snapshot();   // 締めた四半期の盤面をその四半期のラベルで記録（監査2026-07-12 他21：旧＝インクリメント後に呼び翌四半期のラベルが付いていた。現読者は添字参照のみ＝挙動不変）
        if (warm) return;   // ★暦を進めない：助走を何年回しても開始は1783/Q1（1914断面・全長 GameQuarters・TechOnset の年ゲートが動かない）
        World.Quarter++;
        if (World.Quarter > Calendar.QuartersPerYear) { World.Quarter = 1; World.Year++; }
        _quarterIndex++;
    }

    // 月次決算（旧 StepMonth の本体）：金の動く決済はここ。日次ループは StepDay が外から駆動する。
    /// <summary>★通知（作者指示2026-09-04「通知：職人没落・座礁」）：四半期末に州ごとの製法の逆転を1回だけ報せる（観測のみ）。
    /// 「座礁」＝同じ財で発見の表に行を持つ新製法の産出（Output×Scale）が旧製法を初めて上回った四半期（塩漬け→冷蔵・石炭ガス→石油…）。
    /// 「職人の没落」＝職人（RecipeName="職人"）の産出シェアが50%以上あった財で、25%を割った四半期（工場化）。
    /// ⚠ログは World.PendingLog→共有ログ（日次 FlushLog）＝News が拾う。力学は1ビットも読まない。</summary>
    private void LogRecipeTransitions()
    {
        int nGoods = Catalog.Goods.Length;
        Span<double> newOut = stackalloc double[nGoods];
        Span<double> oldOut = stackalloc double[nGoods];
        Span<double> artOut = stackalloc double[nGoods];
        foreach (var r in World.Regions)
        {
            if (r.State != RegionState.Active) continue;
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
                else if (pr.DiscoveryRow >= 0) { newOut[pr.GoodId] += q; newName[pr.GoodId] ??= pr.RecipeName; }
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

    private void MonthSettle()
    {
        // 四半期の締め月（_dayInQuarter==89＝quarterDone と同時に走る月次決算）だけ true・他2回（29/59日目）は false。
        // MonthSettle は月境界（_dayInQuarter∈{29,59,89}）でのみ呼ばれる＝89 が四半期の締め月。読み手＝製法の通知・軍の熟練度（財政の器 FiscalQuarterlyGating は 2026-09-26 に削除）。
        World.IsQuarterEndMonth = _dayInQuarter == Calendar.QuarterEndDayIndex;
        if (World.IsQuarterEndMonth) LogRecipeTransitions();   // ★通知（2026-09-05）：新製法の逆転・職人の没落を報せる（観測のみ・力学非接触）
        // 豊作/不作：年1回ロール（決定論RNG）。主食RGO産出と自給に乗算＝不作は食糧高騰＋自給減で貧農を飢えさせる。
        // 他14（監査2026-07-11・2026-07-25修正）：旧 Tick/360 は各年12月の月次決算で「翌年分」を発火＝年表ログの年表記と適用年が
        // 1ヶ月ずれる系統誤差（忠実度の史実年突合を汚す）。1ヶ月ぶん引いて年境界を1月の月次決算に合わせる＝ロールは当年ラベルで当年に適用。
        // 初回ロール（Tick=30）は不変・各ロールのRNG消費数も不変（消費「時点」だけが月次1つ後ろへ＝決定論は保たれ軌道は再抽選）。
        int harvestYear = (int)((World.Tick - Calendar.DaysPerMonth) / Calendar.DaysPerYear);
        if (harvestYear != _lastHarvestYear)
        {
            _lastHarvestYear = harvestYear;
            // AR(1)（B harvest-iid-no-persistence）：ρ=0 で純IID（現行恒等）。ε のRNG消費は1回のまま＝決定論系列不変。
            double u = Rng.NextDouble();   // RNG消費はSkew/ρに関わらず1回（決定論系列不変）
            double harvestEps = 1.0 + SkewHarvest(u, World.Coef);   // Skew=0 で (u-0.5)*2*HarvestVariation＝対称一様（現行恒等）
            double rho = World.Coef.HarvestPersistence;
            // 分散保存AR(1)（2026-07-09式修正）：旧・凸結合(1−ρ)は定常分散を(1−ρ)/(1+ρ)倍へ圧縮し（ρ=0.35で標準偏差0.69倍）、
            // 「12%超の凶作」の年頻度が26%→11%へ半減＝恐慌原因の不作先行46%→15%逆転の主犯だった（batch30実測）。
            // 革新係数を√(1−ρ²)にすると定常分散＝εの分散を厳密保存＝連続凶作の累積（多年飢饉）を得つつ凶作頻度を維持。ρ=0で厳密に旧恒等・RNG消費不変。
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
                cropHealth[g] += hc.CropDiseaseRecovery * (1.0 - cropHealth[g]);   // 前年までの病害が数年かけて癒える（→1.0）
                if (cropHealth[g] > 0.9 && HarvestRng.NextDouble() < hc.CropDiseaseChance)
                {
                    cropHealth[g] = hc.CropDiseaseSeverity * (0.5 + HarvestRng.NextDouble());   // 新規の胴枯れ＝0.15〜0.45へ激減（重さに揺らぎ）
                    World.LogAdd($"{World.Year} {World.SelfPower?.Name}で{Catalog.Goods[g].Name}が病虫害に見舞われた（収穫{cropHealth[g]:P0}）");
                }
            }
            // 【削除 2026-10-01 (148)】鉱物の枯渇と発見の年次の抽選（国ごとに年3%の枯渇・年2.5%の鉱脈の発見〔全ての発見が frontierRush＝州の全ての鉱物を5倍〕・富鉱の年15%の戻り・
            //   シルバーラッシュ）と Region.MineralDepletion を削除＝①枯渇は採掘による品位（StepMineralDeposits・旗 DepletionFromExtraction を焼き込み）②大鉱床は産地の発見の表
            //   （Catalog.Sites＝場所ごとの年代と入植者×財の値〔(171)：必要性＝財の値段÷掘る費用・入植者は見つける力〕・Discovery.Step が四半期に判定・富鉱は開いた財の生産者だけ）＝検討 §10-3 項目10・Q13（計画の 2-2）。
            //   年に4つ引いていた主系列の乱数（枯渇2・発見2）も消えた＝以後の乱数の列がずれる（挙動の変更＝10種で測った）。
            // 疫病（B-4・コレラ等）：発生は過密（都市化）が駆動し、衛生整備（工業化）は死亡率を下げる。専用RNG系列＝主系列を乱さない。発生＝感染窓を立て他国へ伝播しうる（MultiNationSim）。
            if (World.EpidemicActiveYears > 0) World.EpidemicActiveYears--;   // 感染窓の年次減衰
            double epiRoll = EpidemicRng.NextDouble(); double epiPick = EpidemicRng.NextDouble();   // 常に2つ引く（専用系列）
            double worstUrban = World.MaxUrbanShare();   // 最も過密な地域の都市化率＝発生駆動
            // 接12（監査2026-07-11・2026-07-25休眠配線）：発生確率にも衛生を接続（従来は死亡率側のみ＝1900年代の近代都市でも
            // 1832年のスラムと同頻度でコレラが発生し続けた。史実では衛生インフラが流行の発生自体を止めた）。0（対照腕）で乗数1・既定0.4でlive。
            double epiOnsetCut = World.Coef.EpidemicOnsetSanitationWeight > 0
                ? World.Coef.EpidemicOnsetSanitationWeight * World.WorstUrbanRegionSanitation(World.Coef) : 0;
            if (epiRoll < World.Coef.EpidemicChance * worstUrban * (1.0 - epiOnsetCut))   // 発生率＝基礎×過密度×(1−衛生)（農村国はほぼ起きない）
                World.TriggerEpidemic(World.Coef, 0.5 + epiPick);   // 内部発生（過密スラム）→死亡スパイク＋感染窓
        }

        // 労働時間の連続追従（倍率排除②）：実労働時間 WorkHoursValue は法定上限（WorkHours enum のアンカー）へ毎月収束する。
        // 法は離散（8/10/12時間法を議会が可決）だが現場の労働時間は連続で追従＝移行期は中間値を取り、産出・厚生・健康が滑らかに動く。
        // 第6段6-8a（2026-07-28）執行の飢餓：先進方向（短縮・禁止・保持確立）の浸透は監督官が要る＝行政力でゲート
        //（法は残して監督官を切る反動＝1802徒弟法の空文化。緩和方向は即時＝規制撤廃に監督官は不要）。LawEnforcementAdminGate=0で恒等。
        double enforce = World.Coef.LawEnforcementAdminGate > 0
            ? Math.Min(1.0, World.AdminPowerSmoothed / World.Coef.LawEnforcementAdminGate) : 1.0;
        double hoursTarget = World.WorkHours.Hours();
        World.WorkHoursValue += World.Coef.WorkHoursFollowSpeed * (hoursTarget < World.WorkHoursValue ? enforce : 1.0) * (hoursTarget - World.WorkHoursValue);
        // 賃金保持・児童労働合法度の連続追従（倍率排除④）：法は離散だが社会への浸透は連続（女性の賃金保持の確立／児童労働禁止の取締り）。
        World.WomenWageRetention += World.Coef.WomenRightsFollowSpeed * enforce * ((World.WomenPropertyLaw ? 1.0 : 0.0) - World.WomenWageRetention);
        // 第6段6-7：児童労働の再合法化（禁止→合法）は政治軸を持たない連続追従のため、ここで不可逆を判定＝戦時・断裂の窓のみ可（英1915軍需品法型）。
        double childTarget = World.ChildLaborAllowed ? 1.0 : 0.0;   // 02断線走査2026-07-28：現在値（政治ゲート通過後）を追従＝目標の直読みをやめる
        bool childRegress = childTarget > World.ChildLaborLegal + 1e-9;
        // 交戦判定は国スコープ（州非依存）。毎州の再評価とラムダ・クロージャの確保を避け、ループ前に一度だけ算出して児童労働の不可逆判定・軍需調達・Military.Step で共用する。
        bool atWarNation = World.Diplomacy is { } dipN && World.SelfPower is { } spN && dipN.Wars.Any(war => war.A == spN || war.B == spN);
        if (!(World.Coef.IrreversibleLaws > 0 && childRegress && !atWarNation && World.RuptureWindowQuarters <= 0))
            World.ChildLaborLegal += World.Coef.ChildLaborFollowSpeed * (childRegress ? 1.0 : enforce) * (childTarget - World.ChildLaborLegal);
        double colonialAccess = ColonialAccess();                        // 植民地素材の入手率（植民地×制海・封鎖で↓）
        double nationPop = World.TotalPopulation;                        // 行政予算の地域按分に使う
        bool navalYard = false;   // ★2026-08-01：国内に稼働中の港州が1つでもあるか（州ループの外で1回だけ数える）
        foreach (var pr in World.Regions) if (pr.State == RegionState.Active && pr.IsPort) { navalYard = true; break; }
        // 州非依存の国スカラー（州ループ前に1回だけ算出＝毎州の再計算を避ける。いずれも州ループ内で書き換わらない入力のみ読む）。
        double natSoldiers = World.SoldierSize();
        // ★海の金の割合（2026-10-03 (189)）＝施行された陸海の維持費の比（World.NavalSpendShare＝海軍維持費÷陸海の維持費＝AI は ArmyShape.NavalSpendShareOf を目標に置く・プレイヤーは同じ法の値）。
        //   装備の器（軍事費の器から給与を払った残り）を陸の装備と艦に分ける（下の州ループ・旧＝理想の海の割合〔ShapeNavalShare〕で軍需の内訳を再配分＝艦を兵の頭数×割合で買った）。
        double navalSpendNat = World.NavalSpendShare;
        double warScareTensionNat = World.Diplomacy is { } wsDip && World.SelfPower is { } wsMe ? wsDip.MaxTension(wsMe) : 0;
        double monopolyRentFactorNat = World.Coef.AntitrustGate > 0
            ? World.Antitrust switch { AntitrustLaw.CartelBan => World.Coef.CartelBanRent, AntitrustLaw.Trustbust => World.Coef.TrustbustRent, _ => 1.0 }
            : 1.0;
        int nationalizedMaskNat = World.Coef.NationalizationGate > 0 ? World.NationalizedMask() : 0;
        World.LastSoldierPay = 0;   // 兵士給与の月次請求書（形が先・金が後 段0）＝州ループで積む
        // ★軍事費の吸い込み（2026-10-02 (155)・作者 2026-10-01「高い軍事費はより多くの兵士の他、より十分な武器（≒軍需産業を育てる需要）や、高品質な軍隊を作ります。
        //   ただし、扶養限界の考え方は必要だと思います（つまり、十分な軍隊がそろっている状態だと、兵士の給与は高止まりし、軍事費が高い設定でも出費は伸び続けない）」＝推奨で進めた）：
        //   軍事費の器（Military.EnvelopeOf＝軍事費の段×器の割合×使える歳入〔＋戦時段の割合×借入の1件の額＝(175) 戦時国債〕）を **給与・糧食 → 装備（目標の在庫まで＋更新＋新型＝軍需の必要額）→ 質（幹部の給与＝訓練・効きは逓減）
        //   → 使い切れない分は使わない（請求しない＝出費の頭打ち）** の順に吸わせる。旧＝給与は兵数×賃金を器の外で全額・装備は 軍事費×0.25×歳入（必要額で切る）の2本で、
        //   質（熟練）は払った金でなく維持費の段（ArmyUpkeep＞0.5）で育った＝支出から直接の効き（2026-08-17 の「支出から直接バフを排除する」の型）。
        //   ・給与（規定）＝州の兵数×徴募の母数の働き盛り1人あたりの賃金×SoldierPayMult（Military.RecruitWage＝市場で競り上がらない）。器が給与に届かない＝未払い（給与の充足 payFill＜1＝兵士の「失業」）。
        //   ・戦時の器＝段のまま（戦時経済＝軍事費の段の最上段が戦時の器・旧の装備の上限の戦時の追随 WartimeBudgetFollowsNeed は削除＝Coefficients の注記・(162) で装備の部分だけ戻し (163) の合流で差し戻した）。
        //     → ★(175)：戦時段（交戦中かつ総動員中・施行が段3 を超えた分）の器に借入の1件の額を足す＝戦時国債（Military.EnvelopeAt・借入の門と1件の額は Fiscal.Borrow と同じ1本＝推奨で進めた・作者の承認は未確認）。
        //   ・★(161) 徴兵は兵1人を安くする（AI が作者に返した設計・作者の承認は未確認＝推奨で進めた）＝徴兵制（選抜・皆兵）の兵卒の給与は徴兵の兵の手当（Military.RecruitWage の keep＝徴募の母数の
        //     頭数あたりの所得＝世帯の1人ぶんの暮らし＝糧食・被服と小遣い）・志願制は規定の給与（働き盛り1人あたり）。幹部の給与（質）の物差しはどちらも規定の給与（soldierRegularPay）。
        double milEnvelope = Military.EnvelopeOf(World, World.Coef);
        bool keepPay = World.ConscriptionLaw >= ConscriptionLaw.Selective;
        var soldierBasePay = new double[World.Regions.Count];   // 州の給与の満額（請求の単価×兵数×SoldierPayMult・r と充足を掛ける前）＝徴兵制は手当・志願制は規定の給与
        var soldierRegularPay = new double[World.Regions.Count];   // ★(161) 州の規定の給与の満額（働き盛り1人あたり）＝幹部の給与（質）の物差し（志願制では soldierBasePay と同じ）
        double payFull = 0, regularFull = 0;
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            if (r.State != RegionState.Active || r.SoldierPopIndex < 0) continue;
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
        foreach (var r in World.Regions)
            if (r.State == RegionState.Active)
            {
                r.ColonialAccess = colonialAccess;
                // 保有植民地から、固有資源の和（本国の植民地資源）と供給駆動の産出量を集計（征服で増減＝動的）。単一国テスト(null)は従来挙動。
                if (r.ColonialResources != null && World.SelfPower is { } selfP && r.Id == 0)   // 植民地供給は首都州のみ（多州で二重計上しない＝国の植民地は首都市場に着く）
                {
                    r.ColonialResources = selfP.ColonyList.SelectMany(col => col.Resources).Distinct().ToArray();
                    r.ColonialOutput = Colonial.OutputCurve(World.ColonialInfra, World.Coef.ColonialOutputHalf);
                    r.ColonialSupply ??= new double[Catalog.Goods.Length];
                    Array.Clear(r.ColonialSupply);
                    foreach (var col in selfP.ColonyList)
                    {
                        // 産出は住民の数と不満で変わる：人口が育てば増え、不満（怠業・反乱）で減る＝搾取しすぎは産出を蝕む。
                        double colScale = (col.PopSize > 0 ? col.PopSize / Math.Max(1e-9, col.Population * World.Coef.ColonialPopScale) : 1.0)
                                        * (1.0 - World.Coef.ColonialUnrestSupplyHit * col.Militancy);
                        foreach (int res in col.Resources)
                            if (res >= 0 && res < r.ColonialSupply.Length)
                                r.ColonialSupply[res] += r.ColonialOutput * World.Coef.ColonialProductionPerColony * colScale;   // 植民地ごとに毎期産出
                    }
                    // 独立した旧植民地＝自由な供給者：産物は貿易比重に応じて各国市場へ流れる（資源は世界から消えない。
                    // 旧宗主国は**専有**を失うだけ＝独立の経済的打撃は相対的・世界の連鎖は生きる）。
                    if (World.Diplomacy is { } dipC && dipC.IndependentColonies.Count > 0)
                    {
                        double twSum = 0;
                        foreach (var pw in dipC.Powers) if (pw.Nation is not null) twSum += pw.TradeWeight;
                        double myShare = twSum > 1e-9 ? (selfP.TradeWeight / twSum) : 0;
                        foreach (var col in dipC.IndependentColonies)
                        {
                            double colScale = (col.PopSize > 0 ? col.PopSize / Math.Max(1e-9, col.Population * World.Coef.ColonialPopScale) : 1.0) * 0.8;   // 独立後＝外資なしでやや低い産出
                            // 独立ショック（B-4）：独立直後は産出が崩壊し IndependenceShockYears 年かけて回復（ハイチ1804型＝独立はショック）。
                            double recovery = col.FreedYear < 0 ? 1.0 : PoliticalQuantities.Clamp01((World.Year - col.FreedYear) / (double)Math.Max(1, World.Coef.IndependenceShockYears));
                            foreach (int res in col.Resources)
                                if (res >= 0 && res < r.ColonialSupply.Length)
                                    r.ColonialSupply[res] += World.Coef.ColonialProductionPerColony * colScale * myShare * recovery;
                        }
                    }
                }
                else r.ColonialOutput = 1.0;
                // 軍需調達：軍隊（兵士POP）＋治安維持力に比例、交戦中は増（武器需要ブーム）。財源は国庫の一部。
                // ※Part2で軍＝兵士POPに刷新したが調達は旧・治安維持力(=警察)基準のままだった。軍隊を主軸に紐づけ直す＝弾薬→火薬→鉛→鉛鉱の連鎖が平時も需要を持ち死蔵を防ぐ。
                bool atWar = atWarNation;
                double warExh = atWar && World.SelfPower is { } wsp ? wsp.WarExhaustion : 0;   // 戦時動員の深まり＝厭戦%(長期戦・経済悪化・戦死)を総力戦進行度の代理に
                double mobilize = 1.0 + World.Coef.WarMobilizationDepth * warExh;   // WarMobilizationDepth=0 なら mobilize=1＝旧挙動（⚠既定は0.5＝live・x-02 で注記を是正）。>0で長期戦ほど軍需倍率↑（総動員）
                double warMult = World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 : (atWar ? World.Coef.MilitaryWarMult * mobilize : 1.0);   // 内戦＝対外戦争の2倍の軍需消費（火器需要↑）。対外戦は総力戦の深まりで漸増
                // 軍需＝**兵士の装備のみ**（治安維持力=軍の一本化2026-06-23：治安維持力は装備された軍の効果＝BaseCoercion を MilitaryScale に含めない＝二重計上を解消し装備コストを下げる）。軍隊規模(兵数)×戦時倍率に比例。
                // 課題6（駐屯州調達・2026-08-08）：装備需要の州配分＝首都一極と駐屯州比例のブレンド（総量保存）。
                // g=0＝旧（首都のみ）と厳密同値。兵士は自分の駐屯州の市場で装備を買う＝単一市場の物理律速を解く。
                {
                    double gGar = World.Coef.GarrisonProcurement;
                    double capShare = r.Id == 0 ? 1.0 : 0.0;
                    double share = capShare;
                    if (gGar > 0 && natSoldiers > 1e-9)
                    {
                        double localSoldiers = 0;
                        foreach (var gp in r.Pops) if (gp.IsSoldier) localSoldiers += gp.Size;
                        share = (1.0 - gGar) * capShare + gGar * (localSoldiers / natSoldiers);
                    }
                    r.MilitaryScale = share > 1e-12 ? World.Coef.MilitaryUpkeepScale * natSoldiers * warMult * share : 0;
                    // ★耐久装備のストック（段2a・2026-08-13）：戦時倍率を**摩耗の側**へ渡すために別置きする。
                    //   耐久品の目標在庫は平時基準（warMult を掛けない）＝機関銃は戦争が始まっても9倍要らない。
                    r.MilitaryScalePeace = share > 1e-12 ? World.Coef.MilitaryUpkeepScale * natSoldiers * share : 0;
                    r.MilitaryWarMultLast = warMult;
                    r.GarrisonShare = share;   // 予算配分（下）と共有
                }
                r.NationHasPort = navalYard;   // ★2026-08-01：軍艦の発注ゲート（EconomyStep）が読む。国に港が1つでもあれば艦を発注できる   // 軍需調達は首都州のみ（多州で全国軍を二重調達しない）
                // Region.AtWar（戦時の建設抑制）は倍率排除バッチ2で機構ごと撤去済み（軍需の価格競合による自然縮小に代替）＝書き込みも削除（監査2026-07-12 死37）
                // 軍需装備の財源：**軍事費の器の給与の後の残り**（★(155)＝吸い込みの2段目・旧＝軍事費×MilitaryRevenueShare（0.25）×粗歳入＝給与と別の2本目の器）。
                //   粗歳入から確実に（裁量余剰でなく）＝**軍国（余剰0でも）が装備できる**＝武器を買えた軍だけが治安維持力＋対外武力を持つ（買えねば即応度0→治安維持力0→革命）。
                // ⚠旧＝借入スタックの出費上限（SpendingCapFactor）を軍需にだけ掛けなかった（軍を最後まで維持する）。2026-09-28 に出費上限ごと撤去し、全ての行が同じ国家の支払い率 r（優先順位なし＝作者裁定 F3・Q9）。
                // 課題5（2026-08-08）：戦時は必要額（MilitaryScale×warMult）に予算も追随＝赤字戦費＝★(155) 戦時経済の段（器の最上段）が担う。国庫が尽きれば国家の支払い率 r が自然に絞る。
                // ★陸海の分割（2026-10-03 (189)・作者 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」・2026-08-01「海軍は戦艦ありきです」）：
                //   装備の器を海の金の割合（navalSpendNat）で陸の装備と艦に分ける＝艦は海軍の予算（Region.NavalBudget）で買い、陸の装備の段は艦を買わない（EconomyStep.LandMul）。
                //   旧＝装備の器の全体を陸の装備の段の予算にし、艦も同じ段の中で兵の頭数×海の割合で買った（陸海の再配分 ShapeNavalShare）。
                double seaShare = navalSpendNat;
                if (!(seaShare >= 0)) seaShare = 0; else if (seaShare > 1) seaShare = 1;   // 否定形＝NaN は陸へ
                double milCap = equipRoom * (1.0 - seaShare);   // 陸の装備の請求（r を掛ける前）。支出反射（OpulenceSpendRate>0で国庫超過分も使える歳入へ＝戦争準備金）は器の側で効く
                double navalCap = equipRoom * seaShare;         // 艦の請求（r を掛ける前）
                r.MilitaryBudgetCeiling = World.Coef.GarrisonProcurement > 0 ? milCap * r.GarrisonShare : (r.Id == 0 ? milCap : 0);   // 駐屯州調達＝予算も需要と同じ配分（g=0で旧と厳密同値）   // 上限（必要額で切る前）＝恒常性の信号の分母側。S3a=0 では下の予算と同値＝厳密恒等
                // ★S3a：必要額を超えて割り当てない（上限の意味どおり）。MilitaryBudgetNeedCapped=0 で従来＝厳密恒等。
                // ⚠必要額は**全4段**（基本・上位・tier3・空軍）。基本段だけで打ち切ると残余が厳密0になり上位装備が全滅する（EconomyStep.MilitaryProcurementNeed の注記）。
                // ★(189) 艦も同じ規則（海軍の調達の必要額＝目標の艦隊と在庫の差・EconomyStep.NavalProcurementNeed）＝十分な艦隊が揃えば出費は伸びない（(155) の扶養限界と同じ形）。
                if (World.Coef.MilitaryBudgetNeedCapped > 0 && r.Id == 0)
                {
                    milCap = Math.Min(milCap, EconomyStep.MilitaryProcurementNeed(r, World.Coef));
                    navalCap = Math.Min(navalCap, EconomyStep.NavalProcurementNeed(r, World.Coef, World));
                }
                r.MilitaryBudget = World.Coef.GarrisonProcurement > 0 ? milCap * r.GarrisonShare : (r.Id == 0 ? milCap : 0);   // 駐屯州調達＝予算も配分（r は下で全ての予算に同じく掛ける）
                r.NavalBudget = r.Id == 0 ? navalCap : 0;   // 艦は国の調達（首都の州の1本・港の門は EconomyStep.FleetOrder）
                equipClaimNat += r.MilitaryBudget + r.NavalBudget;   // ★(155) 吸い込みの2段目の請求（国の和）＝3段目（質）はこの残り
                r.SocialSecurity = World.SocialSecurity;                 // 公共医療＝医薬品消費に使う（EconomyStep）
                r.InsuranceCoverage = World.Coef.InsuranceCoverageMax * World.SocialSecurity;   // 健康保険(#3)：医薬品購入費の国家負担割合（r は下で掛ける＝旧 WelfareFunding は国家の支払い率へ畳んだ・2026-09-28）
                r.StateMonopolyMargins = World.StateMonopolyMargins;       // 材ごとの専売の上乗せ率への参照（★2026-09-20＝これが専売の唯一の実体）
                // 戦時備蓄（07_再設計 §5(a)・2026-07-31）：国の最大緊張を州へ複製＝日次の在庫目標（MarketFlow）が読む。
                // 外交層のない世界（単一国テスト・較正）は0＝在庫目標は従来どおり＝厳密恒等。
                r.WarScareTension = warScareTensionNat;
                // 独占禁止法（E6）：独占レント(塩/茶)の実効倍率を法段から複製。AntitrustGate=0で常に1.0＝恒等（EconomyStep が monRent に乗算）
                r.MonopolyRentFactor = monopolyRentFactorNat;
                // 阿片の全面禁止（禁制品doc C3・2026-07-30）：国内生産も違法＝取り締まれた分だけ産出が消え、残るのは闇栽培。
                // 漏れ率は州の行政充足率が決める＝★清が禁止後に国産阿片を激増させた史実（1860年代に国産が輸入を上回る）が
                // 「弱い行政 × 高い価格」から出る。輸入禁止（Limited）は国内生産に触れない＝1.0。
                r.ContrabandProductionFactor = World.Coef.OpiumAddictionGrowth > 0 && World.OpiumPossession == ReformLevel.Full
                    ? Contraband.LeakFraction(r.AdminFill, World.Coef)
                    : 1.0;
                // 国有化法（E6）：5部門＋『全て』（§11-6d統合）のビットマスクを複製。NationalizationGate=0で常に0＝恒等（EconomyStep が該当財の利潤を国庫へ）
                r.NationalizedMask = nationalizedMaskNat;
                // 倍率排除④：女性参加・児童労働を連続状態（賃金保持・合法度）から複製。実効値は LaborSupply が家計困窮・就学を加味して創発。
                r.WomenWageRetention = World.WomenWageRetention;          // 賃金保持（連続）。女性参加・女性識字の駆動
                r.ChildLaborLegal = World.ChildLaborLegal;                // 児童労働の合法度（連続）。子供の労働/就学の時間配分を律する
                r.FemaleParticipation = World.Coef.FemalePartBase + (World.Coef.FemalePartProperty - World.Coef.FemalePartBase) * World.WomenWageRetention;   // 構造的参加率（need=0 の代表値・診断/UI用。0.4〜0.7 を賃金保持で連続補間＝2値の崖を解消）
                r.HarvestFactor = World.HarvestFactor;                   // 今年の豊作/不作（主食RGO産出と自給に乗算）
                r.CropHealth = World.CropHealth;                         // 作物別の健全度（病虫害）。栽培州の産出に乗る＝作物固有の不作（天候と積算）
                r.WeatherBandFactor = World.WeatherBandFactor;           // 緯度帯（気候帯）の天候。HarvestFactorと積算＝同じ気候帯の国が一緒に不作（欧州一帯の凶作）
                r.TechLevel = World.TechLevel;                           // 技術水準（先進財の技術依存スケールに使う）
                r.Discovered = World.Discovered;                         // ★発見の表の解禁（段2A）＝国の配列への参照（写しではない＝判定の四半期から同じ値を読む）
                // 役務（サービス財）の月次設定：教育の担い手と生産性を教育法で切替＋義務教育の財源率＋行政・兵力の予算。
                // C-高9（史実検証2026-07-22）：公教育（Public）にも国庫財源を配線（旧＝Compulsory 以外は厳密0＝「国が無料で供給を得る」因果の逆向き）。
                // 史実の公教育は共同体が教師俸給を払い親も一部払う混合財政（プロイセン無償化1888・仏1881・英1891）＝定義的特徴は歳出項目化。PublicEduFundingShare=0 で旧挙動と厳密一致。
                r.CompulsoryEduFunding = (World.EducationLaw == EducationLaw.Compulsory ? World.EducationSpending
                    : World.EducationLaw == EducationLaw.Public ? World.Coef.PublicEduFundingShare * World.EducationSpending : 0)
                    * Politics.RuleReach(r, World.Coef);   // ★支配度 段4（2026-09-07）：公共財の到達＝国家区分に比例（RuleSharesReach=0 で ×1＝恒等）。0 で従来
                if (r.EducationProducerIndex >= 0)
                {
                    var ep = r.Producers[r.EducationProducerIndex];
                    int pubIdx = r.EducationProducerIndex + 1;
                    if (World.Coef.EduProducersCoexist > 0 && pubIdx < r.Producers.Count && r.Producers[pubIdx].GoodId == Catalog.EducationId)
                    {
                        // ★段1 器 EduProducersCoexist（doc §2-1・LIST §0-9-C 段3・2026-09-12）：[eduIdx]=教区学校（聖職者・紙）／[eduIdx+1]=公立学校（教師・書籍）は**常設**＝
                        //   担い手・生産性・投入を差し替えない（doc §1-2 の崖＝法の1段が「ある職の収入 0↔全額」を切り替える構造の撤去）。
                        //   教育法は「非活性側の Scale=0」のゲートだけ（TechGatedScaleZero の役務版＝EconomyStep が教育財に限り Scale=0 を fraction=0 と読む）。
                        //   再活性は操業規模の更新（床 ScaleFloor へ寄る）が翌月から戻す＝ここで Scale を1へ戻す外科処置は置かない（採算の伸縮は現行のまま）。
                        //   ⚠2件目の存在は盤面（連続 add・GoodId）から判定＝コエフだけでなく実体を見る（BuildFrance 等 clergyIdx を渡さない構築経路は1件のまま＝旧枝へ）。
                        //   ⚠0 ではこの枝に1度も入らない（第1項で短絡）＝旧枝がそのまま走る＝厳密恒等。
                        var pub = r.Producers[pubIdx];
                        bool parishOn = World.EducationLaw == EducationLaw.Religious && r.ClergyPopIndex >= 0;
                        bool publicOn = World.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory && r.TeacherPopIndex >= 0;
                        if (!parishOn) ep.Scale = 0;
                        if (!publicOn) pub.Scale = 0;
                        else if (World.Coef.TeacherQualityWeight > 0 && r.TeacherPopIndex < r.Pops.Count)
                        {
                            // 教師の質（師範学校の効果）＝公立学校側の恒久プロパティ。基底 TeacherEduProductivity から毎月計算し直す（累積乗算にしない）。0 で不読＝構築時の値のまま
                            var tp = r.Pops[r.TeacherPopIndex];
                            double q = 1.0 + World.Coef.TeacherQualityWeight * (PoliticalQuantities.Clamp01(tp.Literacy) * PoliticalQuantities.Clamp01(tp.HigherEduFill) - 1.0);
                            pub.Productivity = World.Coef.TeacherEduProductivity * (q > 0 ? q : 1.0);
                        }
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
                        // ★教師の質（作者採用2026-09-06 知識doc 段5）：法の2値の上に教師pop自身の識字×高等教育充足を掛ける＝師範学校の効果。0 で不読（×1.0）
                        if (World.Coef.TeacherQualityWeight > 0 && r.TeacherPopIndex < r.Pops.Count)
                        {
                            var tp = r.Pops[r.TeacherPopIndex];
                            double q = 1.0 + World.Coef.TeacherQualityWeight * (PoliticalQuantities.Clamp01(tp.Literacy) * PoliticalQuantities.Clamp01(tp.HigherEduFill) - 1.0);
                            if (q > 0) ep.Productivity *= q;
                        }
                        if (ep.Inputs.Count > 0) ep.Inputs[0] = new Input(Catalog.BookId, 0.05);   // 書籍（教科書）＝紙の上位投入
                    }
                    else ep.Productivity = 0;   // 学校なし＝教育の供給なし（識字は停滞）
                }
                // funding修正：上下水道・電化インフラは**税収フロー(FiscalCapacity)から支弁**（国庫ストックは基礎義務に食われ常時0で餓死していた）。スライダー×シェア×歳入×人口比。
                // 行政だけは別格：行政＝徴税の費用＝「税収を生むサービス」。余剰から支弁すると貧国は余剰0→行政0→税収0の永久デッドロックに陥る（funding修正の盲点）。
                // → 行政は**粗歳入(LastRevenue)を第一請求権として支弁**（税吏を雇わねば税は集まらない）＝余剰0でもブートストラップできる（2026-06-22）。
                // ★距離の割増（2026-09-30 (135)＝旧の維持費の人頭割りの距離の割増を行政の購買へ・作者裁定 2026-09-03 E-05段4）：遠い州の予算は割増ぶん厚い
                //   ＝EconomyStep の単価も同じ割増＝充足は割増の無い州と同じ・金額だけ増える（Fiscal.AdminDistanceMul の1本）。
                r.AdminBudget = r.BureaucratPopIndex >= 0 && nationPop > 1e-9
                    ? World.AdminSpending * World.Coef.AdminBudgetFrac * World.SpendableRevenue() * (r.SumPopulation() / nationPop) * Fiscal.AdminDistanceMul(World, r) : 0;   // 請求（r を掛ける前）
                r.InfraBudget = nationPop > 1e-9
                    ? World.AdminSpending * World.Coef.InfraBudgetScale * World.FiscalCapacity * (r.SumPopulation() / nationPop) * HoardSpendScale() : 0;
                r.ElecInfraBudget = nationPop > 1e-9
                    ? World.AdminSpending * World.Coef.ElecInfraBudgetScale * World.FiscalCapacity * (r.SumPopulation() / nationPop) * HoardSpendScale() : 0;
                // 兵力（兵士の給与）の予算＝★(155) 州ループの後（吸い込みの1段目と3段目＝装備の請求が立ってから）。
            }
        // ★(155) 軍事費の吸い込みの1段目（給与・糧食）と3段目（質＝幹部の給与＝訓練）を州へ：
        //   給与の規定の単価＝民間賃金（前月の徴募の母数＝農民＋都市労働者の**働き盛り1人あたり**の所得・(140)）×SoldierPayMult＝Military.RecruitWage の1本（州ループの前の soldierRegularPay）。
        //   ★(161) 兵卒の単価（soldierBasePay）＝志願制は規定の単価・徴兵制（選抜・皆兵）は徴兵の兵の手当（RecruitWage の keep＝同じ所得の頭数あたり）。幹部の給与（質）の物差しは規定の単価。
        //   給与は器から先に払う（給与の充足 soldierPayFill＝器÷満額・1で頭打ち）。3段目＝器から給与と装備（首都の請求）を引いた残りを、幹部の給与の必要額（満額×MilitaryCadrePayRatio）まで
        //   （＝訓練の充足 World.MilitaryTrainingFill＝熟練の育ち方〔Military.StepSkill〕）。それでも残る器は請求しない＝使い切れない分は使わない（出費の頭打ち＝兵士の給与は高止まり）。
        //   幹部の給与は兵士 POP の所得へ（将校と下士官は兵士 POP の中＝兵士の1人あたりの所得が上がる＝志願の魅力）。
        //   ★(161) 幹部の給与の必要額は規定の給与（働き盛り1人あたり）の満額×MilitaryCadrePayRatio＝徴兵制でも幹部（将校と下士官＝長期の職業軍人）は規定の給与で払う
        //   （徴兵が安くするのは兵卒だけ＝質の値段は法に依らない）。志願制では regularFull＝payFull＝(155) と同じ額。
        double trainNeed = regularFull * World.Coef.MilitaryCadrePayRatio;
        double trainClaim = Math.Min(Math.Max(0, equipRoom - equipClaimNat), trainNeed);
        double trainShare = regularFull > 1e-9 ? trainClaim / regularFull : 0;   // 規定の給与の満額1あたりの幹部の給与
        if (!(trainShare >= 0)) trainShare = 0;
        World.MilitaryTrainingFill = trainNeed > 1e-9 && trainClaim >= 0 ? trainClaim / trainNeed : 0;
        for (int ri = 0; ri < World.Regions.Count; ri++)
        {
            var r = World.Regions[ri];
            if (r.State != RegionState.Active || r.SoldierPopIndex < 0) continue;
            r.SoldierPayBudget = soldierBasePay[ri] * soldierPayFill + soldierRegularPay[ri] * trainShare;   // 請求（r を掛ける前）。旧＝州ごとの min(国庫)＝先着（B2）・★(161) 兵卒の給与（手当か規定）＋幹部の給与（規定の物差し）
            World.LastSoldierPay += r.SoldierPayBudget;   // 請求書（形が先・金が後 段0）
        }
        World.LastMilitaryClaim = equipClaimNat;   // ★(155) 器の請求のうち装備（2段目）＝Fiscal の余剰と財政バッファが読む（旧＝Fiscal が 軍事費×0.25×歳入 を必要額で切って書いた）。給与と質（幹部の給与）は兵士の給与の請求（LastSoldierPay）の側＝旧と同じく余剰からは引かない
        // ★国家の支払い率 r（作者裁定 F3・Q1〜Q3・2026-09-28 支払いの一元化 段2-1＝計画 §10-12-3-3）。
        //   その月の**全ての国家の支払いの請求**を月初（ここ＝予算が立った直後・買う前）に集め、払える額と比べて1本の率を決め、
        //   全ての行（予算・工賃・材料・国営持分の赤字・補助・保険・福祉・年金・維持費・利子・戦費・貢納・選挙・通行料…）に同じ r を掛ける＝優先順位を作らない。
        //   払える額（Q1）＝max(0, 国庫)＋当月の歳入の見込み（LastRevenue）。請求（Q2）＝今月の予算（上の州ループで立った額）＋予約＋
        //   月中に決まるものは既存の量で見込む（先月の請求 Last*・先月の区分つき StateSpent・生産者の先月の損益・工事中の棟の工賃）。
        //   元本の返済（減債）だけは請求の外＝ r＝1 の月の余剰（spare＞0）から（Fiscal）・借入は払えない額（spare＜0）の分だけ・r を決める前（下の Fiscal.Borrow）。
        //   置き場所＝既存の FiscalFundingRatio（旧＝維持費の充足率）。逼迫度＝1−r（UtilityAgent.FiscalStressOf）。
        //   撤去したもの＝国庫の現金クランプ StateBudgetCashScale（係数 StateSpendTreasuryClamp）・兵士の給与の州ごとの min(国庫)・日次の工賃の丸休みの門・
        //   材料の門・Fiscal の滝（利払い→維持費→福祉/年金→減債）・WelfarePensionClampToTreasury・SpendingCapFactor（Q9）・手書きの Min(Max(0,国庫)) の門。
        //   ⚠出来事（選挙資金・賠償・武力解錠の軍需・通行料・港・売買方針の代金・外資の拠出・燃料）は月初に額が分からない＝請求の見込みに入らず、起きた時点の r で払う（拿捕は 2026-09-30 から私人の間＝国庫を通らない）。
        //   ⚠四半期の支払い（貢納・招聘の支度金・外資の利回り）は四半期末の外交と共有層がまとめて払う＝四半期の締め月の請求に入れる。
        //   （戦費 Fund の請求の見込みは 2026-09-30 に Fund ごと削除＝支払いの段3-2・戦争の費用の実体は上の軍需の予算と兵士の給与）
        double claims = World.LastInterest + World.LastWelfareCost + World.LastPensionCost + World.LastColonialUpkeep;   // （連合維持費 LastUpkeep は 2026-09-30 (135) に撤去＝兵力と行政の購買の1本）
        bool payQuarter = World.IsQuarterEndMonth;
        if (payQuarter) claims += World.LastTributeOut + World.RecruitBudget;   // 四半期の請求（貢納・招聘の支度金）
        if (payQuarter && World.SelfPower is not null && World.Diplomacy is { } payDip)
        {
            if (World.Coef.MegaprojectForeignYield > 0)
                foreach (var mp in payDip.Megaprojects)
                    if (mp.IsComplete && mp.ForeignPartnerId >= 0 && mp.ForeignPrincipal > 1e-9
                        && World.Regions.Exists(hr => hr.State == RegionState.Active && hr.Name == mp.Host))
                        claims += World.Coef.MegaprojectForeignYield * mp.ForeignPrincipal / World.FxRate;   // 外資の利回り（四半期）＝元本は金建て（2026-09-30 段3-3）→自国の通貨へ＝Megaprojects.PayForeignYield と同じ式
        }
        // 売買方針の代金とダンピング補助（清算ごと・清算の末尾の World.Transfer＝旧 InternationalMarket.PaySubsidy）＝直近の清算の越境の輸入額・輸出額で見込む（同国の州どうしの買い付けの分は見込みの外）
        //   ★段3-1（A7）：清算は金建て＝自国通貨へ為替で換算して見込む（清算と同じ守り＝為替≦1e-9 は1）。
        double erClaim = World.FxRate;
        double tradeClaim = World.Tariff < 0 ? World.LastExportValue / erClaim * -World.Tariff : 0;
        if (World.ImportsByGood is { } payImp)
            for (int g = 0; g < payImp.Length; g++) if (World.TradeBiasOf(g) > 0) tradeClaim += World.Coef.TradeBiasStrength * payImp[g] / erClaim;
        claims += tradeClaim / World.Coef.MonthsPerClearing;
        // 先月の区分つき StateSpent で見込む行（義務教育・健康保険・工場の材料・民営の補助）は先月の r で払った額＝請求へ戻すには先月の r で割る
        //   （この時点の FiscalFundingRatio＝先月の r。先月 0 なら先月は払っていない＝見込みも 0）。
        double lastRate = World.FiscalFundingRatio;
        double regrow = lastRate > 0 ? 1.0 / lastRate : 0.0;
        foreach (var r in World.Regions)
            if (r.State == RegionState.Active)
            {
                claims += r.MilitaryBudget + r.NavalBudget + r.AdminBudget + r.InfraBudget + r.ElecInfraBudget + r.SoldierPayBudget   // ★(189) 艦の請求（海軍の予算）も同じ1本
                        + r.StateBuildQueue + r.MonumentBuildQueue + r.HigherEduFunding
                        + regrow * (Math.Max(0, r.StateSpent[(int)TreasuryCat.Education]) + Math.Max(0, r.StateSpent[(int)TreasuryCat.Welfare])       // 先月の義務教育・健康保険
                                  + Math.Max(0, r.StateSpent[(int)TreasuryCat.FactoryBuild]) + Math.Max(0, r.StateSpent[(int)TreasuryCat.Subsidy]));   // 先月の工場の材料・民営の補助
                // ★国営持分の請求の一覧（2026-09-30 作者裁定「計算量の予算」・恒等）：旧＝州の全ての生産者を毎月走査した（census＝請求に効いた生産者は10.2%）。
                //   一覧 Region.StateSites＝工事中か BuiltScale>1e-9 の生産者を州の生産者の順に。一覧の外の生産者は StateProfitShare＝0.0（BuiltScale≦1e-9・国有化の枝が死んでいる）
                //   ＝赤字（有限）に 0.0 を掛けた +0.0 を足すだけ＝請求は1ビットも変わらない（請求は正の予算の和から始まる＝−0.0 にならない）。和の順（州→生産者）も同じ。
                //   ⚠国有化の枝（NationalizationSeizureRate≦0 かつ州に国有化の印）が生きる州は BuiltScale の無い生産者にも持分がつく＝旧の全件の走査へ戻す。
                if (World.Coef.NationalizationSeizureRate <= 0 && r.NationalizedMask != 0)
                {
                    World.Cost[CostLedger.ClaimProducers] += r.Producers.Count;   // 計算量の台帳（診断）：請求で見る生産者
                    foreach (var pr in r.Producers) AddStateClaim(pr, r, World, ref claims);
                }
                else
                {
                    r.EnsureSites();
                    var sites = r.StateSites!;
                    World.Cost[CostLedger.ClaimProducers] += sites.Length;   // 計算量の台帳（診断）：請求で見る生産者＝国営持分の請求の一覧
                    foreach (var pr in sites) AddStateClaim(pr, r, World, ref claims);
                }
            }
        double payable = Math.Max(0, World.Treasury) + World.LastRevenue;
        // ＞0＝ r＝1 の月の余剰（減債の原資＝Fiscal）・＜0＝払えない請求（＋借越）。負の国庫（記録されない当座借越）は余剰から先に埋める
        //   ＝借越のまま国債を返さない・埋められない借越は赤字（r は Q1 のとおり max(0,国庫) で決める＝国庫0の国の率0→行政0→税収0の行き詰まりを作らない）。
        double spare = payable - claims + Math.Min(0, World.Treasury);
        // ★切り下げ（インフレ税）は**払える額の側**（2026-09-28・(87) の申し送り iii・作者裁定 F4「不足は量を減らすか未払いの記録＝他人から取らない」・思想⑨）。
        //   旧（段2-1）＝ここで r を決めて請求を r で切って払い、月末の Fiscal が「払えなかった請求の全額」（spare＜0）を全POPから国庫へ移した
        //   ＝不足を量で減らした上に同じ額を他人からも取る二重（切り下げの収入が税収の 0.1%→22〜41%・取った金は翌月の余剰＝減債へ回った）。
        //   ⇒ 切り下げの余地を月初に払える額へ入れて r を決め、取った金はその月の支払いに使う。余地は既存の不換の国の発行の規則のまま
        //   （払えない分×(1−兌換)・銀正貨国は改鋳の窓・月に総貯蓄の DevaluationCapFrac まで・うち FiatIssueFrac は新規発行）＝規則は1本。
        // ★払える額の中身の順＝税・歳入 → 借入 → それでも足りない分だけ切り下げ（作者裁定 2026-09-28 (b)「切り下げは借りられないとき（上限・冷却中）だけ」）。
        //   旧（(89)〜(93)）＝切り下げを先に払える額へ入れ、借入は月末の Fiscal が r＜1 の月に切り下げの残りだけ借りた（借りた金は翌月の払える額）
        //   ＝切り下げの97〜99% は借りられた四半期に起き（借入の11〜32倍・税収の30〜45%）、紙幣の増発が信用より先に立っていた。
        //   史実の紙幣の増発は信用が尽きた後（アッシニャ1790〜・墺1811・米のグリーンバック1862）。⇒ 払えない額（−spare）を、借入の上限の下・冷却中でなければ
        //   先に借りてその月の払える額へ入れ（Fiscal.Borrow＝1件の額と貸し手のプールまで）、残った払えない額だけを下の切り下げの余地が読む。
        //   ⚠優先順位の滝ではない＝支払いの行は全て同じ1本の率 r（下）。順があるのは払える額の中身だけ。
        double devalued = 0;   // この月の切り下げの額＝通貨改革の発動の門（切下げゼロの連続月数）と、月末の Fiscal.Step の国債の金利の信認の喪失（直近の切り下げの速さ・2026-09-29）が読む
        if (World.FiscalEnabled)
        {
            double borrowed = Fiscal.Borrow(World, World.Coef, spare);
            payable += borrowed; spare += borrowed;   // 借りた金はこの月の払える額＝r に入る（切り下げと同じ扱い）
            double deficit = spare < 0 ? -spare : 0;   // 否定形＝NaN は0（借りた後に残った払えない額＝借りられない月は全額）
            // 兌換可能性で切り下げ可能量を連続スケール（倍率排除②）：完全兌換(Conv=1)＝切り下げ封印（金の規律）／不換(Conv=0)＝赤字全額を切り下げ可。部分兌換は中間。
            double devalueRoom = deficit * (1.0 - World.Convertibility);
            // ★銀の段の軽い規律（SilverLightDiscipline・2026-08-21）：銀正貨国の切り下げ＝**改鋳（debasement）**は史実で日常だった
            //   （清の咸豊大銭・ムガル以来の含有量操作）。封印されるのは**銀行券の発行**の側＝正貨比率（BaseMoney/BroadMoney）ぶんは
            //   兌換中でも改鋳の窓が開く：room = deficit×(1 − Conv×(1−正貨比率))。純正貨経済（比率≈1）はほぼ全開・銀行券経済ほど封印が効く。
            //   ⚠新しい定数ゼロ（19-10差し戻しの根③＝切下げ封印の全世界閉鎖、の是正）。**live=1（2026-08-21点火）**・0に戻せば旧式と厳密恒等。金の段は従来どおり封印。
            if (World.Coef.SilverLightDiscipline > 0 && World.MonetaryLaw == MonetaryStandardLaw.Silver)
            {
                double specie = World.BroadMoney > 1e-9 ? World.BaseMoney / World.BroadMoney : 1.0;
                if (!(specie >= 0)) specie = 0; else if (specie > 1) specie = 1;   // NaNは0へ倒す（否定形＝ClampはNaNを消さない）
                devalueRoom = deficit * (1.0 - World.Convertibility * (1.0 - specie));
            }
            if (devalueRoom > 1e-9)
            {
                double totalSav = 0; foreach (var dr in World.Regions) foreach (var dp in dr.Pops) totalSav += Math.Max(0, dp.Savings);
                // DevaluationCapFrac は**月あたり**の上限（月次決算＝月に1回）。超過は r＜1＝量の減少で劣化（連合の薄給→治安維持力↓）
                double devalue = Math.Min(devalueRoom, totalSav * World.Coef.DevaluationCapFrac);
                if (devalue > 1e-9)
                {
                    // 不換シニョリッジ（fiat-expansion-as-haircut-not-issue）：切り下げのうち FiatIssueFrac は pop から取らず新規発行＝マネーストック膨張(M↑)。
                    // 発行ぶんは CumulativeFiatIssued に積み Invariants の expected に算入(鋳造と同型)。M↑→P↑は FiatInflationPressure が表現＝二重取りなし。
                    double issue = devalue * World.Coef.FiatIssueFrac;
                    double haircut = devalue - issue;
                    // ★2026-09-30 段4g：インフレ税＝全POP（貨幣の保有＝貯蓄の比・d5）→国庫と、不換の新規発行（世界の外の側＝不換の発行者 Outside.Fiat→国庫）の2本の移し方
                    //   （旧＝貯蓄の手書きの引き落とし・国庫の片側の記帳・不換の台帳の手書き）。台帳 CumulativeFiatIssued は発行の分だけ（BaseMoney の正当な増加源＝Invariants 算入）。
                    if (haircut > 1e-12)
                    {
                        var allPops = new List<Pop>(); foreach (var dr in World.Regions) allPops.AddRange(dr.Pops);
                        haircut = World.Transfer((World, allPops), (World, null, null), haircut, TreasuryCat.Devalue);
                    }
                    else haircut = 0;
                    issue = World.Transfer((null, null), (World, null, null), issue, TreasuryCat.Devalue, outside: World.Outside.Fiat);
                    devalue = haircut + issue;
                    World.CumulativeDevaluation += devalue; devalued = devalue;
                    World.FiatInflationPressure += devalue / Math.Max(1.0, World.BroadMoney);   // 紙幣増発＝インフレ圧（名目GDP↑→実質債務減）
                    payable += devalue; spare += devalue;   // 取った金はこの月の払える額＝r に入る（請求を切った上で別に取らない）
                }
            }
            World.NoDevaluationMonths = devalued > 0 ? 0 : World.NoDevaluationMonths + 1;
        }
        double payRate = claims > 0 ? payable / claims : 1.0;
        if (!(payRate >= 0)) payRate = 0; else if (payRate > 1) payRate = 1;   // 否定形＝NaN は払わない側
        World.FiscalFundingRatio = payRate;
        foreach (var r in World.Regions)
            if (r.State == RegionState.Active)
            {
                // 全ての予算と予約に同じ r（予算は量の上限＝買えた分だけ払う＝払えない分は量が減る）
                r.MilitaryBudget *= payRate; r.MilitaryBudgetCeiling *= payRate; r.NavalBudget *= payRate;   // ★(189) 艦の予算にも同じ r
                r.AdminBudget *= payRate; r.InfraBudget *= payRate; r.ElecInfraBudget *= payRate; r.SoldierPayBudget *= payRate;
                // ★(155) 兵士の「失業」＝給与の未払いの割合（規定の給与のうち払われない分）＝失業プール（Meritocracy の ①''）の駆動（多年EMA＝未払いが長引くほど脱走が増える）
                //   と志願の空き（払われない軍には志願しない）が読む。兵士は労働を市場で売らない＝EconomyStep の売れ残りの書き手は兵士に書かない（この行だけ）。
                if (r.SoldierPopIndex >= 0 && r.SoldierPopIndex < r.Pops.Count && r.Pops[r.SoldierPopIndex].IsSoldier)
                    r.Pops[r.SoldierPopIndex].UnemploymentRate = 1.0 - payRate * soldierPayFill;   // 未払い＝国家の支払い率×器の給与の充足の不足（幹部の給与は規定の給与の外＝数えない）
                r.CompulsoryEduFunding *= payRate; r.InsuranceCoverage *= payRate;
                r.StateBuildQueue *= payRate; r.MonumentBuildQueue *= payRate; r.HigherEduFunding *= payRate;
                // 技術→工場生産性。技術2まではそのまま（較正域）、それ超は逓減（高技術でも生産性7倍等にならず経済を壊さない）
                // 労働時間→工場出力（連続・倍率排除②）：労働供給は労働時間に比例しつつ疲労で逓減（h=14で1.0、短いほど産出↓）。死亡率↓は人口動態で別途。
                double whOut = Math.Pow(World.WorkHoursValue / WorkHours.Unlimited.Hours(), World.Coef.LaborFatigueExp)
                             * Math.Max(0.0, 1.0 - World.Coef.LaborFatiguePenalty * Math.Max(0.0, World.WorkHoursValue - WorkHours.Twelve.Hours()));   // 12h超は過労で産出頭打ち〜微減（LaborFatiguePenalty=0で旧＝単調増）
                // ※児童労働の出力係数(clOut)は廃止：子供の実体化により労働供給そのもの（EconomyStep の労働係数）で表現される（二重計上の防止）
                double union = World.LaborUnion switch { LaborUnionLaw.Tolerated => World.Coef.LaborUnionToleratedOutputMul, LaborUnionLaw.Controlled => World.Coef.LaborUnionControlledOutputMul, _ => 1.0 };   // 労働組合容認＝賃上げ/ストで工場出力↓（資本が反対する理由。不満緩和は別途）
                // no-graduated-unrest-stages（最小dormant）：本国の労働争議＝減産の土台。植民地の ColonialUnrestSupplyHit と同型を本国に開く。
                // 組合容認かつ地域平均闘争度が中間閾値超で操業停止（賃上げ要求）。0（対照腕）で乗数1.0＝不発・既定0.25でlive（台帳訂正2026-07-12）。
                if (World.Coef.StrikeProductionHit > 0 && World.LaborUnion != LaborUnionLaw.Illegal)
                {
                    double rmil = 0, rsz = 0; foreach (var mp in r.Pops) { rmil += mp.Size * mp.Militancy; rsz += mp.Size; }
                    double avgMil = rsz > 1e-9 ? rmil / rsz : 0;
                    double strikeIntensity = Math.Max(0, avgMil - World.Coef.StrikeMilitancyThreshold);
                    union *= PoliticalQuantities.Clamp01(1.0 - strikeIntensity * World.Coef.StrikeProductionHit);
                }
                // 満足度への直接バフ（時短whWell・福祉WelfareSatComponent・年金PensionWellbeing）は物理削除2026-08-21（思想⑨＝支出/法は財を経ずに満足度を動かさない。
                // 作者裁定8/19「満足度という値を捨てて生活水準＝買えたかのみ」）。時短は出力減 whOut・福祉/年金は現金給付（→消費→生活水準）としてのみ残る。
                double unionWell = World.LaborUnion switch { LaborUnionLaw.Tolerated => 1.0, LaborUnionLaw.Controlled => 0.5, _ => 0.0 };
                r.LaborWellbeing = World.Coef.UnionSatBonus * unionWell;   // ⚠休眠（既定0・作者裁定2026-08-19「団体交渉権などに使うので休眠」）＝組合の非物質厚生のみ
                double techProd = (1.0 + Math.Min(World.TechLevel, TechProdDiminishKnee) + Math.Max(0.0, World.TechLevel - TechProdDiminishKnee) * TechProdDiminishSlope) * whOut * union;
                RepayHouseholdDebt(r);   // ★出口：返済規律（消費の前・Savings 自体を削る。下の定義を見よ）
                EconomyStep.Run(r, World.Coef, techProd, World.Bank, World);   // §9-A/B/C（active 地域ごと）
                // 恐慌の資本破壊（最終調整2026-06-29・ユーザー指示「恐慌が資本を破壊」）：深い景気後退（GdpDrawdown 閾値超）では工場閉鎖・資本廃棄で工業化度が退行する＝
                // 恐慌が『ストック』を削る＝覇権の構造的先行（工業化）を大恐慌が巻き戻す経路。閾値未満の通常循環では工場を壊さない。0（対照腕）で恒等・既定0.06でlive。
                // ★助走中は資本廃棄も止める（2026-08-22）＝「凍結は両側」（2026-08-10裁定＝助走は資本を一切育てない・減らさない）。準レント配分で助走中に
                //   一過性のドローダウンが出て scrapKeep が走り、StartWarmupTests（資本の恒等）が割れた＝旧配分では偶然踏まなかった穴。
                // ★戦争被害のストック化（AI審査3-11・2026-08-31★live同日点火 0.03）：戦場州では家畜の群れそのものが徴発で減る（flow減産＝WarFarmOutputDamageとは別のstock喪失）。
                //   回復は書かない＝家畜RGOの操業規模は既存の供給弾力（RGOの年単位の速度）でしか戻らない＝「終戦で即回復」しない時間構造が既存機構から出る。
                if (World.Coef.WarLivestockRequisition > 0 && r.LastWarIntensity > 0 && !World.WarmupFrozen)
                    foreach (var lpr in r.Producers)
                        if (lpr.IsRgo && lpr.GoodId == Catalog.LivestockId)
                            lpr.Scale = Math.Max(0, lpr.Scale * (1.0 - World.Coef.WarLivestockRequisition * Math.Min(1.0, r.LastWarIntensity)));
                if (World.Coef.DepressionScrapRate > 0 && r.CapitalEnabled && !World.WarmupFrozen)
                {
                    double scrapKeep = 1.0 - World.Coef.DepressionScrapRate * Math.Max(0, World.GdpDrawdown - World.Coef.DepressionScrapThreshold);
                    r.Industrialization *= scrapKeep;
                    r.OldVintageCapital *= scrapKeep;   // ヴィンテージ2026-07-10：恐慌の資本廃棄は新旧両層に同率（合計＝旧単層と同一動学）。StrandedVintageも比例縮小（座礁≤旧世代の不変条件を保つ・次月に再計算）
                    r.StrandedVintage *= scrapKeep;
                    if (World.Coef.DepressionScrapsBuiltScale > 0 && scrapKeep < 1)
                        foreach (var fp in r.Producers)
                            if (!fp.IsRgo && fp.BuiltScale > 1e-9 && fp.MothballFrac > 1e-9)
                                fp.BuiltScale *= scrapKeep * fp.MothballFrac + (1.0 - fp.MothballFrac);   // 〔7-1〕第2版（2026-09-06）：**休止中の分だけ**解体（第1版＝採算割れ全部は batch10 硬性赤＝バイエルン最下位固定＝小国の工場が戻らない）。休止率0＝不変
                }
                // 国家の調達・国営の損益・専売の精算を**区分ごとに**（2026-09-28 段2-1 D1〜D3）。旧＝1本の StateSpent を OtherOut へ呑ませ、
                //   専売益だけを別の欄 LastStateMonopolyRevenue に二重に積んで割り戻していた＝財政タブで軍需・行政・教育・建設・補助・国営の損益の区分が出なかった。
                World.SettleStateSpent(r);   // 国庫→生産者（支出）／生産者・消費者→国庫（専売益・国営持分の利潤）で保存（World の中の1本・2026-09-30 段4g）
                // （植民地の採取費の対外流出と、その一部を植民地の蓄えへ渡す枝は 2026-09-28 に削除＝旗 TradeColonialGoods=1 で常に0だった）
            }
        ProbeCheck("月次経済");
        Transport.Step(World, World.Coef);                               // §5.1拡張 輸送容量（鉄道/船を積む→Tradeで交易量を律速）
        ProbeCheck("月次輸送");
        // ⚠撤去2026-09-03（J-011＝作者裁定「推奨どおり」）：旧・世界市場（`Trade.cs`）を削除。
        //   ★実測＝`Trade.Step` は `w.TradeEnabled` が真のときだけ走り、その真値は**テスト2本にしか現れなかった**
        //   ＝61カ国の実ゲームでは**1命令も走っていなかった**（二国間交易 `BilateralMarket` が置き換え済み）。
        //   ⚠先に検査内容を実経路へ移してから消した（`ConservationTests` が保存量Dと輸出利潤経路の生存を見る）。
        ProbeCheck("月次交易");
        Colonial.Step(World, World.Coef);                                // §6.1 植民地（インフラ整備→資源産出＋捕囚市場。交易流入で保存）
        ProbeCheck("月次植民地");
        StepMineralDeposits();   // ★鉱脈在庫（2026-08-14）：掘った分だけ減る・減れば品位が落ちる。ゲート0＝即return＝恒等
        MintGold();                                                      // 鋳造（金鉱→正貨）：金産出の一部が基礎貨幣に（ゴールドラッシュ＝マネー膨張）。GoldMintFraction=0で無効＝較正中立
        ProbeCheck("月次鋳造");
        Fiscal.Step(World, World.Coef, spare, devalued);                 // §9-F/§6.2（全 pop に課税）。spare＝月初の払える額（借入と切り下げの後）−請求（＞0＝減債の原資）・devalued＝この月の切り下げ（国債の金利の信認の喪失）
        ProbeCheck("月次財政");
        // 国営工場カウンター（作者裁定2026-08-10）：目標と現在の差を1歩ずつ埋める（着工の再挑戦・1棟ずつの払い下げ）＋
        // 国有化法の接収。★財政の**後**に置く＝払い下げ代金と国庫の残高が同じ月の中で前後しない
        // （売却は資本家→国庫の移転＝Fiscal の課税ベースを後から動かさない）。プレイヤーもAIも同じここを通る。
        World.StepStateTargets();
        ProbeCheck("月次国営工場");
        Crime.Step(World, World.Coef);                                   // 犯罪と抑止：秩序（治安維持力の人口カバレッジ）が崩れると窃盗＝闇の再分配（純移転）
        ProbeCheck("月次犯罪");
        // 兵士POP軍制（Part 2）：軍事費で徴募し給与を払う（国庫→兵士で保存）。戦時は徴兵。財政の後・外交ゲージ集計の前。
        Military.Step(World, World.Coef, atWarNation);   // 交戦判定はループ前に算出済み（atWarNation）を共用
        if (World.IsQuarterEndMonth) Military.StepSkill(World, World.Coef, atWarNation);   // ★熟練度は四半期に1回（係数の時定数は四半期で書いてある・2026-08-04）
        ProbeCheck("月次軍事");
        if (World.StepsDiplomacy)
            World.Diplomacy?.Step(World.Coef, Rng, (int)(World.Tick / Calendar.DaysPerMonth));  // §6.1 外交・戦争（月次・opt-in。多国では共有外交を MultiNationSim が進める）
        // rank3：農奴解放の梯子（Colonial.cs入植と同じ物差し）＝SerfMobilityGate>0のとき農村→都市流を縛る（ループ不変＝外で1回）
        double serfFree = 1.0 - Privileges.SeigneurialDegree(World);
        foreach (var r in World.Regions)
            if (r.State == RegionState.Active)
                PopDecision.Run(r, World.Coef, serfFree);           // §9-D（地域内の階級遷移）
        ProbeCheck("月次政治");
        if (!World.WarmupFrozen) Territory.Step(World, World.Coef);      // §5.2/§A-8（解錠＋地域間移民）。助走中は止める＝州の解錠も国内移住も「時間が経って起きる」こと
        ProbeCheck("月次領土移民");
        TechEducation.GrowLiteracy(World, World.Coef);                   // §A-1（助走中は識字の伸びだけ止まり Elite の導出は通る＝分岐は GrowLiteracy 内）

        // 助走中（World.WarmupFrozen・2026-08-10）は人口動態を止める＝出生死亡は「時間が経つと積み上がる」量そのもの。
        // 暦を止めたまま回すと「10年ぶんの人口成長が時間経過なしに起きる」＝1783年の開始盤面でなくなる。
        if (!World.WarmupFrozen)
            DemographicStep();           // 人口動態：出生（医療/都市化）−死亡（労働時間）＝動的人口・戦死からの回復も兼ねる（人口転換）。飢えの超過死亡（平常の死亡率の上乗せ）と州の食料充足の平滑も内包
        ProbeCheck("月次人口");
        BackstopNegativeSavings();   // 損失吸収の脆さの会計：貯蓄が負になった pop は不足を信用で埋める（有限責任・保存則厳守）
        ProbeCheck("月次バックストップ");
        CheckInvariants();
    }

    /// <summary>
    /// 人口動態（人口転換・§A-3 拡張）：扶養力(carrying capacity)へロジスティック収束＝出生−死亡の純。
    /// 扶養力＝規定値×(1＋医療−都市化)×労働時間。医療＝min(社会保障, 識字/gate)で教育律速（医者が要る）。
    /// 戦死で減った人口もこのステップで扶養力へ回復。規模按分で全 pop に適用、符号付き台帳で保存則追跡。頑健（暴走/崩壊しない）。
    /// </summary>
    // ①Phase3：出産可能女性プール（成人のうち非老年×女性比率）。BirthWeight と InitialFertileShare 捕捉で共有＝t=0 正規化と実行時重みが同一式（Σbirths=net の保存を厳密化）。
    // 人5（徴兵で出生が増える符号逆転の解消）：素の女性プール（∝女性）は、成人男性だけ抜く徴兵で残留popの女性比率が上がると出生配分が「増える」＝史実（夫不在で出生急減）と逆。
    // CoupleLimitedBirths=0 で女性の数に比例・1で「カップル律速」＝少数側の性 min(女,男)×2 に律速。
    // 徴兵で男が抜けると min(女,男) の男側が縮み出生が減る＝カップル（夫婦）が律速資源＝WWI 仏の出生半減を再現。Wave1 の FertileShareFloor（重み膨張の防止）と整合。
    // ★年齢の層（2026-09-30 段4e 後半・設計書 §4-1）：15〜49歳の女×カップル律速（同じ15〜49歳の男との少数側）。旧＝成人（16歳以上）のうち老年（60歳以上）でない者×成人の女性比。
    //   起点合わせは要らない＝InitialFertileShare（t=0 の各 POP の値）で正規化する。標準の分母（FertileAbsoluteBlend）は標準のピラミッドをこの式に通した値（旧 FertileShareStd）。
    internal static double FertilePool(Pop p, Coefficients c) => FertilePoolOf(p.Ages, c);
    private static double FertilePoolOf(double[] ages, Coefficients c)
    {
        double f = Pop.AgeShareOf(ages, 15, 50, 1), m = Pop.AgeShareOf(ages, 15, 50, 0);
        return f + c.CoupleLimitedBirths * (2.0 * Math.Min(f, m) - f);   // 少数側の性が2人でカップル1組＝律速（男女どちらが不足しても出生↓）
    }
    // ①Phase3：国の出生の**総数**と州の中の階級への配り方の鍵（★2026-10-01 (143) から州への配り方は今の出産可能のプール×規模＝DemographicStep）。false（対照腕）は Size＝従来の粗出生（規模比例）＝厳密に従来挙動。既定 true で女性プール律速。
    // true で「出産可能女性プール」に律速し、各popの t=0 値で正規化＝標準構成（子供0.3・老年0・女性0.5）では Size と恒等＝較正中立。
    private static double BirthWeight(Pop p, Coefficients c)
    {
        if (!c.FertileWomenBirths) return p.Size;
        double fertile = FertilePool(p, c);
        double init = p.InitialFertileShare > 0 ? p.InitialFertileShare : Math.Max(c.FertileShareFloor, fertile);   // フロアを現実的下限へ引き上げ可能に（1e-6 だと捕捉時 fertile≈0 の pop の重みが桁違いに膨張＝出生集中バグ）
        double w = p.Size * fertile / init;
        // 凍結分母の是正（休眠 FertileAbsoluteBlend=0・作者裁定2026-08-16＝年代スケール禁止・確立した形で）：
        // popごとの t=0 凍結分母 init は、初期構成が偏ったpop（聖職者F=0.10）でも捕捉時に重み=Size を保証するが、新生児（F≈0.5）の
        // 蓄積で構成が正常化すると**分母凍結のまま分子 fertile が育ち恒久出生倍率（実測2.1倍＝聖職者爆発182→8362）**になる
        //（「比のガードは絶対値で書かない」と同族＝docs/202608/20260815_失業プールと社会移動_部分完了.md「点火直後の回帰」節）。
        // 是正形＝分母を**全pop共通の定数1本**へ：FertilePool(標準構成)＝(1−0.3子供)×0.5女性=FertileShareStd(0.35)を、FertilePool と同じ
        // カップル律速項に通した 0.35×(1+CoupleLimitedBirths)（標準構成では couple=1.0）。標準popの重み=Size を保つ＝総出生の物差し
        //（totalBirths=CBR×fertility×Σ重み）を動かさない。時間・年号は一切読まない＝現行より単純。
        // fertile≈0 のpopは共通分母では分子側で重み≈0＝膨張の恐れがなく自然に安全（FertileShareFloor は旧経路の捕捉時分母にのみ残る）。
        // Σbirths=net の保存（回復期の按分・totalDeaths=totalBirths−net）は既存の仕組みをそのまま通る＝配分の重みが変わるだけ＝人口保存不変。
        // ゲート0＝下の分岐に入らず w をそのまま返す＝旧popごと凍結分母とビット恒等／1＝(1−b)w+b·wStd で wStd へ厳密に全置換。
        if (c.FertileAbsoluteBlend > 0)
        {
            double wStd = p.Size * fertile / FertilePoolOf(Pop.StdAgesRef, c);   // 標準のピラミッドの出産可能プール（旧＝FertileShareStd×(1＋CoupleLimitedBirths)＝同じ式の標準の構成）
            w = (1.0 - c.FertileAbsoluteBlend) * w + c.FertileAbsoluteBlend * wStd;
        }
        return w;
    }
    /// <summary>★平常の死の年齢の指数（2026-10-01 (143)）＝その POP の年齢の構成で平常の死の年齢の重み（<see cref="Pop.MortalityWeights"/>）を平均した値（1人あたりの相対の死亡率）。
    /// 平常の死はこれ×規模の比で POP へ配る＝年寄りの多い POP ほど多く死に、若い POP ほど少なく死ぬ（年齢別の死）。標準のピラミッドでは粗死亡率（出生−表の増加率）。</summary>
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
        double baseline = pop - World.PopulationDelta;   // 規定値
        if (baseline < 1e-9) return;
        double litSum = 0, healthSum = 0, foodMet = 0, foodNeed = 0;
        foreach (var p in World.AllPops()) { litSum += p.Literacy * p.Size; healthSum += p.HealthFill * p.Size; foodMet += p.FoodMet; foodNeed += p.FoodNeed; }
        double healthcare = Math.Min(World.SocialSecurity * World.FiscalFundingRatio, (litSum / pop) / c.HealthcareEducationGate);   // 社会保障(充足率で減衰)と識字(医者)の小さい方で律速＝財政不足で医療→人口増が鈍る
        double doctorCare = healthSum / pop;   // 医者の役務（医療財）の消費＝乳児死亡の低下。医者が居ない/買えない社会は人口が伸びにくい
        // 接13（監査2026-07-11・2026-07-25休眠配線）：主医療チャネル（重み1.2）が社会保障×識字の抽象値のみ＝医者が1人も居ない国でも
        // 満額が出て、実装済みの医療サービス経済（医者pop・医薬品・重み0.05）が人口にほぼ報いない。これ>0で実医療財の消費(HealthFill)を
        // 第3のmin項としてゲート化（1.0＝医者なし→抽象医療も0・中間＝部分ゲート＝実体側への段階移行）。0（対照腕）で第3項なし・既定0.5でlive。
        if (c.HealthcareRealGate > 0) healthcare = Math.Min(healthcare, doctorCare / c.HealthcareRealGate);
        // 市場医療の代替（人口忠実度ラウンド2026-08-21・DoctorHealthcareSubstitute＝**0.5でlive**）：福祉法のない国でも実医療財の消費（doctorCare＝医者の役務）が
        // 乳児死亡を下げる＝国家チャネルを下から支える。実測（popprobe s1）＝英は識字0.64・医者役務0.53でも社会保障0のため healthcare が131年間0だった
        // （最大の扶養力項 +1.2×healthcare が英でだけ死ぬ＝史実と逆。英の死亡率低下は福祉国家より1世紀早い）。
        // 単独A/B＝人口次元18.6→28.6%（英×1.73→1.95・日30→80%・破綻0/10・他次元ノイズ内）で点火。0でゲート不読＝厳密恒等。
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
        double indPopSum = 0, indPopWeight = 0;   // 人16：工業化度の人口加重（Σ Ind×rpop / Σ rpop）。IndustrializationPopWeighted=1で非加重avgIndを置換
        double sanitSum = 0, sanitPop = 0;   // 衛生（上下水道+石鹸+電化）の人口加重。施設の無い世界は各 Fill 既定で sanitation=1.0=中立（較正温存）
        double arableSum = 0, ruralSum = 0;   // 人2b 空き土地の扶養力（下のlandRoom）：耕地と農村人口の全国集計。ArableLand≥1e8（未設定＝較正/最小世界）は除外＝厳密恒等
        // B-高1（史実検証2026-07-22）：主食の純輸入。下の消費ベース補正コメントが「輸入の分子算入は将来課題＝Trade記録から」と自認していた欠落を埋める（FoodAnchorNetImport適用は下）。
        // 消費ベースは met=sub+min(qty, need−sub) により foodMet≤foodNeed が構造的に保証され、生産ベースは自国産出だけなので、
        // **輸入で人口を養う国（英は1900年頃に小麦消費の約8割を輸入）が構造的に人口爆発できない**。実測でも英×1.22（史実×3.4）・清×1.01（史実×1.45）。
        double stapleNetImport = 0;
        double stapleOutput = 0;   // 人2：全active州の主食(Staple)実産出量の合計（Σ Market[主食].LastProduced）。耕地×農業技術×豊凶を既に内包した「実食料産出量」＝扶養力の食料アンカーの分子。FoodOutputAnchorProduction=0では未使用（読むだけ・挙動非依存）
        foreach (var r in World.Regions) if (r.State == RegionState.Active)
        {
            avgInd += r.TotalIndustrialization; actN++;   // 扶養力の工業化項＝立っている資本の総量（新旧合計＝ヴィンテージ休眠時は恒等。2026-07-10）
            double rpop = r.SumPopulation();
            indPopSum += r.TotalIndustrialization * rpop; indPopWeight += rpop;
            double rsanit = PoliticalQuantities.Clamp01(r.InfraFill + c.SoapSanitationWeight * r.SoapFill + c.ElecInfraSanitationWeight * r.ElecInfraFill);
            sanitSum += rsanit * rpop; sanitPop += rpop;
            // 人2：この州の主食RGO産出を合計（LastProduced＝需要非クランプの実産出。regionLandFactor[耕地逓減]×農業生産性×HarvestFactor×WeatherBandFactor×CropHealth を既に内包＝「耕地×技術×豊凶の代理」）。
            foreach (var g in r.Goods) if (g.Tier == GoodTier.Staple)
            {
                var mk = r.Markets[g.Id];
                stapleOutput += mk.LastProduced;
                // ★S2台帳衛生（TradeLedgerFix・2026-08-30）：LastImport/Export は月初(:EconomyStep)でゼロ化→清算で積むため、
                //   この読み手（人口ステップ）の位置では**常に0か当月断片**＝FoodAnchorNetImport が発火不能だった（全経路マップ:282）。
                //   ON＝前清算の完全な値（PrevImport/Export）を読む。0＝旧式＝厳密恒等
                stapleNetImport += c.TradeLedgerFix > 0
                    ? mk.PrevImport - mk.PrevExport
                    : mk.LastImport - mk.LastExport;   // B-高1：主食の純輸入（輸出国は負＝食料を送り出す分だけ自国の扶養力は下がる）
            }
            if (r.ArableLand < 1e8)   // 人2b：空き土地の集計（センチネル除外）
            {
                arableSum += r.ArableLand;
                ruralSum += r.SumRuralPopulation();   // 州の農村人口の1本（兵士を含む＝土地の逓減・機会・受け入れの上限と同じ入力・段4e H＝旧はここだけ兵士を除いた）
            }
            stapleOutput += r.LastSubsistenceOutput;   // 人2補完：自給産出（市場外）を分子へ＝自給7割の農業国で分子過小→capacity崩落を防ぐ（2026-07-11）
        }
        if (actN > 0) avgInd /= actN;
        // 人16（工業化度の人口加重）：非加重avgInd（空の辺境州の追加で出生率/扶養力が変わる）を人口加重平均へブレンド。
        // IndustrializationPopWeighted=0で avgInd（現行）・1で人口加重（Σ Ind×rpop/Σ rpop）。1州世界では両者が恒等＝較正中立。
        double indPopWeighted = indPopWeight > 1e-9 ? indPopSum / indPopWeight : avgInd;
        avgInd += c.IndustrializationPopWeighted * (indPopWeighted - avgInd);
        double sanitation = sanitPop > 1e-9 ? sanitSum / sanitPop : 1.0;
        // 人2（扶養力の食料産出アンカー・大改修スカフォールド→本実装）：現行は capacity＝初期人口(baseline)×有界乗数束のため天井が約2.5倍で
        // 19世紀の人口爆発（英4倍/米23倍）が構造的に不能。さらに消費ベースの foodMet/foodNeed は[0,1]クランプ（消費は需要で頭打ち）で農業拡大が capacity を押し上げる経路がない。
        // capacityBaseline を「初期人口(baseline)」と「実食料産出量から換算した扶養人口（foodAnchor）」の間でブレンド。
        // foodAnchor の食料比 foodRatio は2経路を係数 FoodOutputAnchorProduction でブレンド：
        //   =0（既定）：消費ベース foodMet/foodNeed（需要クランプ∈[0,1]・現行と厳密一致）。
        //   =1：**生産ベース** stapleOutput/foodNeed＝(全active州の主食実産出Σ)÷(主食必要量Σ)。耕地×農業技術×豊凶を内包した LastProduced が分子なので
        //        農業増産・開墾・技術で1を超えられる＝天井の押し上げ経路。1人あたり必要量で割る＝実食料産出を扶養人口へ換算（「耕地×技術ベースの再構成」）。
        // CapacityFromFoodOutput=0 で capacityBaseline==baseline＝現行の初期人口アンカーを厳密再現（foodAnchor は乗数0で不使用＝恒等）。
        // 乗数束（医療・工業・労働時間）は下で capacityBaseline に掛かる＝「食料が足りる範囲での上乗せ」に再配置（アンカーが実産出になれば pop>capacity の補償死倒錯＝在来民の負のロジスティック項が緩む）。
        // 生産ベースの foodRatio は1を超えうる＝乗数束の実効上限が撤去される段階的経路（係数で連続に効かせられる）。完全な上限撤去・33/33/33再較正は #17 の較正再走に委ねる（notes 参照）。
        double foodRatioConsumption = foodNeed > 1e-9 ? foodMet / foodNeed : 1.0;   // 消費ベース（需要クランプ・現行経路）
        // B-高1：分子に主食の純輸入を算入（FoodAnchorNetImport=0 で加算項ゼロ＝厳密恒等）。**輸入のみ**（Max0＝輸出国の天井は削らない）＝
        // landAnchor と同じ「下方向なし」原則。⚠初版は負値（輸出）も通していたため、序盤穀物輸出国の英の人口が×0.94へ**縮む**逆効果を実測（batch2026-07-23）＝輸出は既に
        // 消費ベース（国内に残った分＝foodMet）が拾っており、生産ベース分子から重ねて引くと二重ペナルティになる。
        double foodRatioProduction = foodNeed > 1e-9 ? (stapleOutput + c.FoodAnchorNetImport * Math.Max(0, stapleNetImport)) / foodNeed : 1.0;   // 生産ベース（未クランプ＝実食料産出÷必要量。余剰で1超＝天井押し上げ）
        // 人2補完(1b)：生産比は月次の豊凶（HarvestFactor×帯天候×病害）を生で浴びて跳ねる→FoodFillと同じ時定数のEMAで平滑（初回=-1未捕捉は直接初期化＝跳躍なし）。
        if (World.FoodRatioProductionSmoothed < 0) World.FoodRatioProductionSmoothed = foodRatioProduction;
        else World.FoodRatioProductionSmoothed += c.FoodFillSmoothing * (foodRatioProduction - World.FoodRatioProductionSmoothed);
        // 人2補完(1c)：輸入依存国（英）の保護＝生産ブレンドは**上方向専用**（max）。輸入で食う国の capacity を産出不足が直に削らない
        //（輸入の分子算入は B-高1 で実装済み＝下の foodRatioProduction が FoodAnchorNetImport で加算。ここは max で「農業拡大は天井を押し上げ・輸入国は従来の消費アンカー」）。
        double foodRatioRaw = Math.Max(foodRatioConsumption,
            foodRatioConsumption + c.FoodOutputAnchorProduction * (World.FoodRatioProductionSmoothed - foodRatioConsumption));   // FoodOutputAnchorProduction=0で消費ベースに厳密一致
        double foodAnchor = baseline * Math.Max(0.3, foodRatioRaw);          // 実食料産出アンカー（下限0.3＝崩壊防止・baseline側と同水準）。生産ベースでは baseline×(実産出/必要)＝扶養人口
        double capacityBaseline = baseline + c.CapacityFromFoodOutput * (foodAnchor - baseline);
        // 人2c 土地アンカー（2026-07-11・#17級の天井撤廃）：capacityBaseline を「初期人口」から**土地が養える人口**へ。
        // landAnchor = Σ耕地/(1−都市化率)＝耕地は農村人口容量の単位（ArableLand=初期農民×余裕の定義）・都市民は農村の食料基盤の上に乗る。
        // baseline を上回る分だけ引き上げ（max0＝下方向なし＝輸入国・都市国家を削らない）。欧州（耕地≈初期農民×1.8≈baseline級）はほぼ中立・
        // 新大陸（西部開拓後は耕地≫初期人口）だけ天井が大陸規模へ＝19世紀の人口爆発（米25×）の構造解。センチネル除外＝較正厳密恒等。0で休眠可。
        if (c.LandAnchorCapacityWeight > 0 && arableSum > 1e-9)
        {
            // landAnchor＝Σ耕地そのもの（農村人口容量）。都市民の上乗せは既存の乗数束が担う＝都市化率で割らない
            //（割ると旧世界の耕地余裕1.8×が都市化とともに膨張＝清936M/仏82Mの過剰解放を実測→撤回）。
            double landAnchor = arableSum;
            capacityBaseline += c.LandAnchorCapacityWeight * Math.Max(0, landAnchor - capacityBaseline);
        }
        // U3（変数統合監査2026-07-13・裁定a）：空き土地（Σ耕地/Σ農村−1）を**扶養力天井から分離し出生率へ役割移管**（旧 landRoom×capacity乗数＝LandAnchorと同じΣ耕地で天井を二重リフトしていた＝解消）。
        // 空き土地＝辺境の高出生（米西部のTFR>7＝早婚多産）として下の fertility へ。ArableLand≥1e8（較正/最小世界）は集計除外＝厳密恒等。既定0で休眠可。
        double landRoom = c.LandRoomFertilityWeight > 0 && ruralSum > 1e-9
            ? c.LandRoomFertilityWeight * Math.Max(0, arableSum / ruralSum - 1.0) : 0;
        // B-高2（史実検証2026-07-22）：辺境出生ブーストの上限。無上限だと米は西部解錠後 landRoom≈3.7＝出生倍率4.7倍→年6.4%成長（史実3.09%・生物学的上限約4%）。
        // 既定 LandRoomFertilityCap=1e9 は landRoom(実測最大≈3.7)を素通し＝演算なしで厳密恒等。点火0.6＝倍率1.6。
        landRoom = Math.Min(landRoom, c.LandRoomFertilityCap);
        double capacity = capacityBaseline * (1 + c.HealthcareCapacityBonus * healthcare + c.HealthFillCapacityBonus * doctorCare + c.IndustryCapacityBonus * avgInd
                                        - c.UrbanCapacityPenalty * World.UrbanShare + c.FoodCapacityWeight * (World.FoodFill - 1.0)
                                        + c.SanitationCapacityWeight * World.UrbanShare * (sanitation - 1.0)
                                        - c.PollutionCapacityWeight * World.UrbanShare * avgInd * (1.0 - sanitation)) * WorkHoursCapacity(World.WorkHoursValue, c);   // 衛生は都市で按分＝疫病は過密の問題＋工業化の環境外部性（公害圧＝工業化×都市化×(1−衛生/規制)で超過死亡・上下水道で緩和）。PollutionCapacityWeight=0で従来完全一致
        capacity = Math.Max(baseline * 0.3, capacity);
        // ★診断（2026-08-01・純記帳）：扶養力の内訳を外へ出す。popprobe が読む。
        World.DiagCapacity = capacity; World.DiagCapacityBaseline = capacityBaseline;
        World.DiagHealthcare = healthcare; World.DiagDoctorCare = doctorCare;
        World.DiagFoodRatio = foodRatioRaw; World.DiagLandAnchor = arableSum;   // 下限（崩壊防止＝飢饉でも全滅しない＝翌年の収穫で再生可能）
        World.DiagStapleNetImport = stapleNetImport;   // ★人口診断拡張（2026-08-21・純記帳）：主食の純輸入＝輸入で人口を養う経路（B-高1）が実際に流れているか
        // 出生率＝女性の識字で下がる（人口転換の因果＝女子教育→少子化）。賃金保持の無い社会は女子に教育が届きにくい（係数 FemaleLitFloor＝0.6）。
        // 初等識字（開始点まで）は出生率に効かない（実証準拠）＝無学な社会は従来の較正値のまま。
        // 倍率排除④：旧 WomenPropertyLaw?1:0.6 の2値を、賃金保持（連続）で 0.6→1.0 を補間（retention=0で0.6＝旧無法・retention=1で1.0＝旧有法）。
        double femaleLit = (litSum / pop) * (c.FemaleLitFloor + (1.0 - c.FemaleLitFloor) * World.WomenWageRetention);
        // 1934拡張E6 女性教育権（★2026-08-10 以降は女性の権利の梯子の**教育権の段**）：女子教育が女性識字を底上げ（財産権の賃金保持とは別経路＝学校）。WomenEducationGate=0で不読＝恒等。
        if (c.WomenEducationGate > 0 && World.WomenEducationLaw)
            femaleLit = Math.Min(litSum / pop, femaleLit * (1.0 + c.WomenEduLitBonus));
        // 出生率＝基礎−女性識字−所得/発展(工業化度)。識字は教育経路、工業化は所得/都市化経路で、医療(死亡率低下＝capacity)の後に遅れて効く＝二段階の人口転換（B-4）。
        // U3：空き土地(landRoom)＝辺境出生ブースト。FertilityBase以上へ引き上げ（クランプ上限も同じだけ上げる）＝米西部の高出生を扶養力天井でなく出生側で表現。landRoom=0で厳密恒等。
        double landFertBoost = c.FertilityBase * landRoom;
        // 分割相続の出生抑制（人口忠実度ラウンド2026-08-21・FertilityLandDivisionSlope）：土地改革＝小土地所有の創出が進んだ国は、自作農が
        // 土地の細分化を避けて出生を絞る（ナポレオン民法典の均分相続＝仏の出生率早期低下の標準的説明。識字より前に効いた唯一の大国が仏）。
        // 既存の法状態 LandReform（None=0/Limited=0.5/Full=1）のみを読む＝国名分岐なし。英=None（囲い込み＝土地なし労働者に抑制動機なし）＝抑制ゼロ。既定0＝厳密恒等。
        double landDivision = c.FertilityLandDivisionSlope > 0 ? c.FertilityLandDivisionSlope * ((int)World.LandReform / 2.0) : 0.0;
        // 所得少子化の因果順序（人口忠実度ラウンド2026-08-21・FertilityIncomeSurvivalGate）：所得/発展の少子化は死亡率低下（医療＝healthcare）が
        // 実現した国でだけ進む（人口転換の定説＝子が生き残るようになってから産み控える。下のコメント「死亡率低下の後に遅れて効く」の実装）。
        // 旧実装は avgInd を直に読む＝開幕の工業化バースト(+2.0)で全国の出生率が1808年までに床0.5へ落ちていた（英=ヴィクトリア朝の高出生と正反対）。
        // 既定0＝乗算(1−0)+0×…=1.0はIEEE754で正確＝厳密恒等。healthcare∈[0,∞)をmin(1,·)で有界化。
        double incomeGate = (1.0 - c.FertilityIncomeSurvivalGate) + c.FertilityIncomeSurvivalGate * Math.Min(1.0, healthcare);
        double fertility = Math.Clamp(c.FertilityBase + landFertBoost - c.FertilityLitSlope * Math.Max(0, femaleLit - c.FertilityLitOnset) - c.FertilityIncomeSlope * avgInd * incomeGate - landDivision, c.FertilityFloor, c.FertilityBase + landFertBoost);
        // 人7（出生率倍率の死亡側混線の解消）：ロジスティック純増 net＝PopGrowthRate×fertility×… なので、totalDeaths＝totalBirths−net の両項が fertility 比例。
        // ＝少子化した国は同じ食料不足でも飢饉死が半分という因果の混線（fertility は本来「産む数」であって「死ぬ数」ではない）。
        // FertilityAffectsBirthsOnly=0 で netFertility==fertility＝現行と厳密一致・1で net の収束速度から fertility を外し 1.0（出生側のみ fertility を残す）＝死亡側を出生率倍率から独立。
        double netFertility = fertility + c.FertilityAffectsBirthsOnly * (1.0 - fertility);
        // 人口転換の均衡結合（人口忠実度ラウンド2026-08-21・FertilityEquilibriumWeight）：FertilityAffectsBirthsOnly=1.0（人7・2026-07-27）で
        // netFertility≡1 となって以降、出生率倍率は純増に一切効かない＝人口転換の全機構が「収束の速さ」からも「均衡」からも切断されていた
        // （実測 popprobe s1＝全国 Q100 で pop/capacity≈1.0 張り付き＝成長は100%天井が決める・英は出生率が下限0.5でも帰結ゼロ）。
        // 是正＝出生率の低下は**均衡人口**を天井より下へ引く（出生=死亡の均衡。少子化の国は食料が余っていても人口が伸びない＝仏1.45×の物理）。
        // eqFert=1−これ×max(0,1−fertility)。上側はクランプ（fertility>1の開拓地ブーストは均衡を天井超えに押し上げない＝過冲・飢饉振動の禁止）。
        // 死亡側は不変（totalDeaths=totalBirths−net の構造そのまま）＝人7「少子化の国は飢饉死も半分」の誤因果を再導入しない。0で (1−pop/cap) と厳密同値。
        double eqFert = 1.0 - c.FertilityEquilibriumWeight * Math.Max(0.0, 1.0 - fertility);
        double net = c.PopGrowthRate * netFertility * pop * (eqFert - pop / capacity);   // ロジスティック：均衡＝eqFert×扶養力へ収束（出生−死亡の純）。fertility の死亡側寄与は FertilityAffectsBirthsOnly で切り離せる
        World.DiagFertility = fertility; World.DiagLandRoom = landRoom;   // ★人口診断拡張（2026-08-21・純記帳）：出生率倍率と空き土地ブースト＝仏過大/英過小の因子分解用
        var mortW = Pop.MortalityWeights(c.CrudeBirthRate);   // ★年齢の層（段4e 後半）：平常の死の年齢の重み（標準のピラミッドが1783年の出生で動かない重み）
        double massPop = 0, massBirthWeight = 0, massBirthKey = 0, massDeathWeight = 0;
        var regions = World.Regions;
        var regKey = new double[regions.Count];   // ★(143) 州ごとの出産年齢の女性の実数（今の出産可能のプール×規模の和）＝国の出生を州へ配る鍵（プール＝(142) から15〜49歳の女×カップル律速・(143) の測りの時点は16〜59歳×成人の女性比＝(144) 注記）
        var regBW = new double[regions.Count];    //   州ごとの旧の鍵の和＝州の中の階級への配り方（凍結の分母のまま＝9-2）
        for (int ri = 0; ri < regions.Count; ri++)
        foreach (var p in regions[ri].Pops) if (!p.IsSoldier)   // 出生死亡は兵士以外の全階級（貴族・聖職も人口とともに増減。兵士は軍事力学が別途管理）
        {
            massPop += p.Size;
            massDeathWeight += p.Size * AgeMortalityIndex(p, mortW);   // ★(143) 平常の死の配分鍵＝年齢の構成で重みを付けた規模
            if (c.FertileWomenBirths && p.InitialFertileShare < 0)
                p.InitialFertileShare = Math.Max(c.FertileShareFloor, FertilePool(p, c));   // t=0 の出産可能女性プールを各popの相対基準として捕捉（BirthWeight と同一式＝人5 のカップル律速も t=0 正規化に反映）

            double bwp = BirthWeight(p, c);
            massBirthWeight += bwp;   // ①Phase3：国の出生の総数の物差し（既定は Size＝massBirthWeight==massPop）
            regBW[ri] += bwp;
            double key = c.FertileWomenBirths ? p.Size * FertilePool(p, c) : p.Size;
            regKey[ri] += key; massBirthKey += key;
        }
        if (massPop < 1e-9) return;
        bool ageDeath = massDeathWeight > 1e-12;   // 年齢の重みが全て0（係数で死亡率が負に振り切れた場合）なら規模で配る＝台帳の Σ死＝総死は保つ
        // ★出生と死は同じ単位（2026-09-30 (131)）＝POP ごとに「1人あたりの率×規模」。出生は配分鍵（粗出生）、平常の死は国の平常の死亡率×規模。
        //   ★2026-10-01 (143)：**平常の死は年齢の構成で重みを付けた規模**（年齢の指数×規模＝年寄りの多い POP ほど多く死ぬ・標準のピラミッドの POP は国の平均の率）。
        //   国の中の年齢別の死亡率は年齢の重み（MortalityWeights）に比例する＝POP の中の年齢の配り方（StepAges）と同じ重み。旧＝POP の間は1人あたり同じ率・POP の中だけ年齢の重み
        //   ＝若い POP の年寄りほど多く死に、老いた POP の年寄りほど少なく死んだ（POP の間と中で規則が違った）。
        //   総出生 − 平常の死 = net（ロジスティック）。回復期（純増＞粗出生）は平常の死0で超過を出生（旧 Math.Max と同じ）。
        //   ★飢えの超過死亡は**飢えの深さの絶対の率**（下の POP のループの終わり＝FamineExcessMortality×深さ^FamineExcessMortalityExponent×規模・(143)）＝国の死の予算から取らない。
        //   ⚠(131) の形＝平常の死亡率×濃さ×深さ＝平常の死が多い国ほど同じ飢えで多く死んだ＝年齢の層（(137)）で出生と死が史実へ近づいて増えた分だけ飢えの死も増え、
        //     r138 で飢えによる州の空洞化（ピークの1/10 未満）が2→16件・(140) で25件。
        //   ⚠旧＝国の死（総出生−net）を脆弱性の重みで飢えた POP に寄せていた（重み＝規模×(1＋これ×不足)÷Σ重み）＝飢えた POP が国全体の死の受け皿になり、
        //     豊かな POP の死がその分だけ減った＝飢えた州の減りが国の中の他の州の伸びに付け替わった（分子と分母の非対称）。
        //     加えて局地飢饉（旧 RegionalFamineStep＝国平均からの相対の不足×0.025／月）が同じ飢えを2本目の死として数えていた（思想⑨）＝植民地の州が開幕から
        //     飢えの死で年3%ずつ痩せ、ピークの1/10 未満へ落ちた（(125) の10種で532件・(131) の計器＝局地飢饉 32.5‰・寄せた分 5.5‰／年）。
        double totalBirths = c.CrudeBirthRate / Calendar.MonthsPerYear * fertility * massBirthWeight;   // 既定 massBirthWeight==massPop ＝現行と厳密一致
        bool noMothers = !(massBirthKey > 1e-12);   // 産む人が居ない（全 POP の出産可能のプール≈0）＝出生0（否定形＝NaN も0）
        if (noMothers) totalBirths = 0;
        bool recovering = net >= totalBirths;
        double totalDeaths = recovering ? 0 : totalBirths - net;
        double deathRate = recovering ? 0 : totalDeaths / (ageDeath ? massDeathWeight : massPop);   // 平常の死の率（年齢の指数×規模の1単位あたり・月）
        double diagBirths = 0, diagDeaths = 0;   // ★計器（2026-09-26）：粗出生・粗死亡の累積の元（World.DiagBirths/DiagDeaths へ＝力学は読まない）
        double ghostDeaths = 0;   // 人口保存バグ修正：負人口防止クランプ(下)で削られた「人が足りず起き得なかった死」を集計し、台帳のnetから差し戻す（無クランプ時は0＝ビット恒等・West巨大人口の崩壊時のみ発火）
        double famineDead = 0;    // 飢えの超過死亡（国の死の予算の外）の実際に減った人数＝台帳へ別に引く
        bool birthsSuppressed = recovering && noMothers;   // 人口保存：回復期に産む人が居ない（add0=0）と実出生0なのに台帳が+=netでズレる＝netを差し引く（正常時false・出産可能プール≈0の退化popでのみ発火）
        Span<double> disasterW = stackalloc double[2 * Pop.AgeBands];
        Pop.DisasterWeights(disasterW, c.DisasterAgeMortalityExcess);   // 飢えの超過死亡の年齢の重み（災害死と同じ）
        for (int ri = 0; ri < regions.Count; ri++)
        {
            var r = regions[ri];
            // ★(143) 出生の州の取り分＝州の出産年齢の女性の実数÷国の和（旧＝旧の鍵＝POP ごとの t=0 の凍結の分母で州へも配った＝ペルシャのタブリーズは国の平均より年 7‰ 少なく産んだ）。
            //   州の中の階級へは旧の鍵の比（凍結の分母のまま）＝階級の配り方を今のプールにすると、凍結の分母が膨らませていた識字の高い階級（聖職者 2.3→5.4%・資本家 4.1→6.1%
            //   〔日本・公教育の番人の盤面・30年〕）が膨らまなくなり、公教育の下で識字が 0.35→0.19 へ落ちて幕府の番人が赤＝教育の経路が凍結の分母に支えられていた＝9-2 で一緒に。
            double regShare = noMothers ? 0 : regKey[ri] / massBirthKey;
            double regBWr = regBW[ri];
            foreach (var p in r.Pops)
                if (p.IsSoldier) { if (p.Size > 1e-12) p.StepAges(p.Size, 0, 0, mortW); }   // 兵士も年を取る（作者裁定 Q10・自然の出生と死は外のまま＝9-2 で）
                else
                {
                    double bk = regBWr > 1e-300 ? regShare * BirthWeight(p, c) / regBWr : 0;   // 出生の取り分＝州の取り分×州の中の旧の鍵の比（加齢の「前」に捕捉＝事前パスと同一構成＝Σ取り分＝1＝Σbirths=net の保存を厳密化）
                    double size0 = p.Size;           // 月初の規模＝出生・平常の死・飢えの上乗せが掛かる同じ規模
                    double dw = ageDeath ? size0 * AgeMortalityIndex(p, mortW) : size0;   // 平常の死の配分鍵（事前パスと同じ月初の構成）
                    // 出生は**粗出生**として0〜9 の層に加わる（純増だけを子供にすると成熟社会で子供が払底＝構成崩壊）。平常の死は年齢の構成で重みを付けた規模（(143)）。
                    double births, deaths;
                    if (recovering)
                    {
                        double add0 = net * bk;   // 回復期：純増を出生の取り分で按分。産む人が居なければ0（旧 massBirthWeight≈0 の 0/0=NaN の防止＝#54運賃点火が露出した下流NaN と同じ）
                        births = add0; deaths = 0;
                    }
                    else
                    {
                        births = totalBirths * bk;   // 出生＝国の総出生×取り分（(143)・州へは出産年齢の女性の実数・州の中は旧の鍵）
                        double deathsRaw = deathRate * dw;                  // 平常の死＝国の平常の死の率×年齢の指数×規模（飢えで寄せない・(143) 年齢別の死）
                        deaths = Math.Min(deathsRaw, p.Size + births);   // 下限保護（負人口の防止）
                        ghostDeaths += deathsRaw - deaths;   // クランプで削られた「人が足りず起き得なかった死」を集計（下でnetから差し戻す）。無クランプ時は0＝ビット恒等（West巨大人口の崩壊時のみ発火＝人口保存バグ修正）
                    }
                    double add = births - deaths;
                    double newSize = p.Size + add;
                    if (newSize < 0) { ghostDeaths += -newSize; newSize = 0; }   // 死亡クランプ発火時の浮動小数丸めでSizeが極小負(≈-3.5e-18)になるのを0にクランプし、削った分をghostDeathsで台帳へ差し戻す（非負境界の保護・正常時newSize≥0で不発＝ビット恒等）
                    diagBirths += births; diagDeaths += births - (newSize - p.Size);   // ★計器：死亡＝実際に減った人数（クランプ込み）＝出生−実際の増分
                    // ★年齢の層（2026-09-30 段4e 後半）：加齢（毎月・層の上の端の密度で上の層へ・70以上は死でだけ出る）→ 出生（0〜9 の男女へ半々）→ 平常の死（年齢の重み＝
                    //   標準のピラミッドが1783年の出生で動かない重み）＝実際に減った人数。旧＝子供比（約16年で成人・AgingYears）と老年比（約45年で老年・WorkingYears・老衰の重み3・上限0.15）の2本。
                    p.StepAges(size0, births, births - (newSize - size0), mortW);
                    p.Size = newSize;
                    // 人11a（新生児の識字の無償相続の解消）：出生は Size を増やすだけで Literacy 不変＝新生児が親popの識字を暗黙相続（無償の世代間伝達＝識字ラチェット）。
                    // NewbornLiteracyDilution=0 で希釈なし＝現行と厳密一致・1で出生分（識字0の新生児 births 人）を加重平均で希釈：Lit_new = Lit_old×(Size−births)/Size。
                    // 死亡は平均識字で抜ける（平均不変）ため births のみで希釈。生存者(Size−births)が旧識字を担い、新生児は0＝親の識字を無償相続しない。（教育側の識字ラチェットは TechEducation.cs=別担当）
                    if (c.NewbornLiteracyDilution > 0 && births > 1e-12 && p.Size > 1e-9)
                    {
                        double diluted = p.Literacy * PoliticalQuantities.Clamp01((p.Size - births) / p.Size);   // 新生児=識字0 で加重平均（births 人が0を持ち込む）
                        p.Literacy += c.NewbornLiteracyDilution * (diluted - p.Literacy);   // 0で不変（恒等）・1で完全希釈
                    }
                    // ★飢えの超過死亡（2026-10-01 (143)）＝**飢えの深さの絶対の率**＝FamineExcessMortality（年率）÷12×深さ^FamineExcessMortalityExponent×月初の規模。
                    //   飢えの深さ＝(門−充足)÷門（門＝飢饉死の門 FamineDeathFillFloor・絶対水準・平常 FoodRef は読まない＝回B）＝門の上は0・門で連続（(131) 腕C）・充足0で1。
                    //   凸（指数4）＝慢性の浅い不足（充足0.6〜0.7）はほとんど殺さず、深い飢饉は速く殺す（史実の飢饉の死は栄養の不足に強く凸・係数の注記）。
                    //   ⚠旧（(131)）＝平常の死亡率×FamineMortalityConcentration×深さ＝平常の死が多い国ほど同じ飢えで多く死に、回復期（平常の死0）は飢えても死ななかった。
                    //   ⚠深い飢えが何十年も続く POP（植民地の工場労働者＝充足0.22）は凸の率で速く死ぬ＝農村から飢えた都市へ移る流しの管が州を痩せさせた（(143) 腕B）
                    //     ＝飢饉の線の下で農村より食べられない都市へは移らない（PopDecision）と対。
                    //   国の死の予算の外＝豊かな POP の死は減らない。子供と老年に厚く（災害死と同じ運搬＝リ10）。否定形＝NaN の上乗せは殺さない側。
                    double fill = p.FoodFill;   // 主食の充足の1本
                    double floorF = c.FamineDeathFillFloor;
                    double excess = fill < floorF ? c.FamineExcessMortality / Calendar.MonthsPerYear * Math.Pow((floorF - fill) / floorF, c.FamineExcessMortalityExponent) * size0 : 0.0;
                    if (excess > 0 && p.Size > 0)
                    {
                        double d = Math.Min(excess, p.Size);
                        p.KillAges(p.Size, d, disasterW);   // 年齢の層：災害死の年齢の重み（0〜9 と 60以上に厚く）
                        p.Size -= d; famineDead += d;
                    }
                }
        }
        World.DiagBirths += diagBirths; World.DiagDeaths += diagDeaths + famineDead;   // ★計器（2026-09-26・純記帳）
        World.PopulationDelta += net + ghostDeaths - (birthsSuppressed ? net : 0.0) - famineDead;   // 人口保存：意図netに①クランプで起き得なかった幽霊死ghostDeathsを差し戻し②ガードで実出生0の時はnetを引く③飢えの上乗せで実際に減った人数を引く。実Size減と一致
        SmoothRegionalFoodFill(c);   // 州の食料充足の平滑（移民の飢饉の押し出し・州反乱・派閥の危機が読む）
    }

    /// <summary>州の食料充足（<see cref="Region.FoodFill"/>）の平滑＝穀倉の備蓄（餓死は数ヶ月の持続的欠乏で起きる）。
    /// 読み手＝移民の飢饉の押し出し（MultiNationSim）・州反乱（Politics）・派閥の危機（Factions）。
    /// ★2026-09-30 (131)：旧 RegionalFamineStep の局地飢饉の死（国平均からの相対の不足×RegionalFamineMortality／月・閾値 FamineAbsoluteThreshold）を削除
    ///   ＝同じ飢えを POP の飢えの上乗せ（DemographicStep）と2本目の死で数えていた（思想⑨）。人は国平均より貧しいから死ぬのではなく食べられないから死ぬ
    ///   ＝POP の絶対の充足だけが死に効く（州の中の不作・孤立・途絶で飢えた州は、その州の POP の充足が落ちて上乗せが立つ）。</summary>
    private void SmoothRegionalFoodFill(Coefficients c)
    {
        foreach (var region in World.Regions)
        {
            if (region.State != RegionState.Active) continue;
            double met = 0, need = 0;
            foreach (var p in region.Pops) { met += p.FoodMet; need += p.FoodNeed; }
            double raw = need > 1e-9 ? PoliticalQuantities.Clamp01(met / need) : 1.0;
            region.FoodFill += c.FoodFillSmoothing * (raw - region.FoodFill);   // 州の食料充足を平滑（穀倉の備蓄＝餓死は数ヶ月の持続的欠乏で起きる）
        }
    }

    /// <summary>支出反射のインフラ拡張（2026-08-08）：行政・軍需の実支出は**需要でキャップ**され国庫が減らない
    /// （行政需要∝人口・軍需はS3aで必要額min＝fisprobe実測で普が0.6点火後も×0.25ロック継続）。
    /// 弾力的に吸収できる唯一の国家支出＝**インフラ建設**（プロイセン国有鉄道・明治の官営事業＝退蔵の史実の使い道）を
    /// 潤沢超過に比例して増額する。上限は市場の実吸収（鋼・セメントの在庫と価格）＝台本なし。
    /// ⚠OpulenceInfraChannel=0（既定＝休眠）で 1.0＝乗算恒等（旧注記は OpulenceSpendRate と書いていた＝そちらは点火済みの0.6・x-02 で是正）。</summary>
    private double HoardSpendScale()
    {
        // ⚠差し戻し2026-08-08＝OpulenceSpendRate=0.6との同時点火で硬性赤（公教育の自由国テスト失敗＝較正世界の
        //   資源配分を建設ブームが歪める）。独立係数 OpulenceInfraChannel（既定0）へ分離＝単独で再測定可能に。
        if (World.Coef.OpulenceInfraChannel <= 0) return 1.0;
        double baseRev = Math.Max(1e-9, World.LastRevenue);
        return World.SpendableRevenue() / baseRev;   // 潤沢超過ぶんだけ >1（普≈18倍＝鉄道国家の建設ブーム）
    }


    /// <summary>改革法の地代上限：全面＝設定値／限定的＝なし(1.0)と全面の中間／なし＝1.0(無制限)。＝段階的な土地改革/農奴解放。</summary>
    private static double RentCap(ReformLevel lv, double full) => lv switch
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

    /// <summary>★鉱脈在庫（2026-08-14・作者裁定「枯渇率は採掘量により増加すべき」）。
    /// 鉱脈＝有限の在庫：毎月、州の鉱物産出の合計だけ減る（**決定論＝サイコロなし**）。
    /// 残量が膝（DepositGradeKnee）を切ると品位が線形に低下→産出減衰→採掘も減る＝自己減速（ハードストップでなく品位曲線）。
    /// 初期在庫＝助走明けの最初の産出月の率×DepositYearsAtBirth（InitialWork と同じ初回捕捉の作法）。
    /// 発見が在庫を補充する＝産出の大きな伸びは**新しい鉱脈**の仕事（加州1848・ランド1886）＝史実の形（★(148)：産地の発見 Discovery.OpenSite が補充する）。</summary>
    private void StepMineralDeposits()
    {
        var c = World.Coef;
        // ⚠多国シム限定（2026-08-14の是正）：発見イベント（在庫の補充）は Diplomacy 非null 限定なのに、排水だけ全世界で効かせると
        //   単一国の較正世界は**純減のみ**＝石炭が60年で痩せて9テストが赤になった（自作農の飢え・PI収束・信用保存）。
        //   排水と発見は**対の機構**＝同じゲートに置く（較正世界は従来の確率イベントのまま＝24-24-24の前提を壊さない）。
        if (World.WarmupFrozen || World.Diplomacy is null) return;   // ★(148)：旗 DepletionFromExtraction（点火 2026-08-14）を焼き込み＝枯渇は採掘量の関数だけ
        foreach (var r in World.Regions)
        {
            if (r.State != RegionState.Active) continue;
            double outSum = 0;
            foreach (var p in r.Producers) if (Catalog.IsMineral(p.GoodId)) outSum += p.Output;
            if (!(outSum > 0)) continue;   // 産出なし（NaNも「なし」へ倒す＝否定形で書く）
            if (r.MineralDepositInitial <= 0)
            {
                // 初回捕捉：この州の初期鉱脈＝現在の年率×DepositYearsAtBirth
                r.MineralDepositInitial = outSum * Calendar.MonthsPerYear * c.DepositYearsAtBirth;
                r.MineralDeposit = r.MineralDepositInitial;
            }
            r.MineralDeposit = System.Math.Max(0, r.MineralDeposit - outSum);   // 掘った分だけ減る
            double frac = r.MineralDepositInitial > 1e-9 ? r.MineralDeposit / r.MineralDepositInitial : 1.0;
            double knee = System.Math.Clamp(c.DepositGradeKnee, 1e-6, 1.0);
            double grade = frac >= knee ? 1.0 : c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * System.Math.Max(0, frac) / knee;
            if (!(grade >= c.DepositGradeFloor && grade <= 1.0)) grade = 1.0;   // NaNは品位低下なし側へ倒す（否定形＝ClampはNaNを消さない）
            r.MineralGradeFactor = grade;
        }
    }

    /// <summary>鋳造（金鉱→正貨）：金鉱産出の一部を造幣局が新鋳貨で買い取る＝基礎貨幣の増加（金産出国の通貨的優位・ゴールドラッシュ→マネー膨張→物価/好況）。
    /// 鋳造分は金の商品市場から引き取られる＝宝飾と造幣が金を奪い合う（金価格↑）。GoldMintFraction=0 で無効＝較正中立。
    /// 保存：鉱山所有者へ新鋳貨を支払い（Savings↑＝TotalMoney↑＝BaseMoney↑）、同額を CumulativeGoldMinted に積み貨幣保存の基準に算入。
    /// GoodsBalance：引き取り量を Stock−／LastSold＋で対称に動かし在庫恒等式を保つ。</summary>
    private void MintGold()
    {
        double frac = World.Coef.GoldMintFraction;
        bool floor = World.Coef.MintPriceFloorGate > 0;   // ★自由鋳造＝床モード（2026-08-14設計・2026-08-29点火＝既定1）。0＝旧来
        if (frac <= 0 && !floor) return;
        // ★本位金属の一般化（銀本位doc G3・2026-08-13）：造幣局が買うのは「金鉱」ではなく**その国の本位金属**である。
        //   金本位→金鉱／銀本位→銀／不換→どちらも買わない。⚠これで需要が法に従って現れ・消える＝
        //   独が1871年に金へ移れば独の銀需要は【消える】＝需要曲線そのものが下へ落ちる（価格弾力性では戻せない＝大デフレ）。
        //   StandardMetalMintGate=0（対照腕）では常に金鉱＝旧経路と厳密一致（数値中立）。既定1.0で本位金属（★点火2026-08-29）。
        int metal = Catalog.GoldOreId;
        bool isFiat = false;
        if (World.Coef.StandardMetalMintGate > 0)
        {
            isFiat = World.MonetaryLaw == MonetaryStandardLaw.Fiat;
            if (!isFiat) metal = World.MonetaryLaw == MonetaryStandardLaw.Silver ? Catalog.SilverId : Catalog.GoldOreId;
        }
        // ★床モード：法が捨てた金属の準備を市場へ売る（demonetization・2026-08-14）。買うより先に売る＝
        //   金へ移った国は同じ月から銀を吐き出す（独1871年型＝買い手の消滅と投げ売りが同時）。床OFFでは準備が常に0＝恒等。
        if (floor) SellDemonetizedReserves(metal, isFiat);
        if (isFiat) return;   // 不換＝準備需要そのものが無い（造幣しない）
        // ★準備需要（G3）：買う量は「基礎貨幣×目標カバー率 − 現在の準備」＝準備が足りているほど買わない。
        //   MintReserveTargetWeight=0 なら shortfall 係数=1＝旧来の産出比例と厳密一致（⚠既定は点火済みの1.0＝2026-08-29・x-02 で注記を是正）。
        double mintScale = 1.0;
        double coverShort = 0;   // ★MintBuysAtMarket（2026-09-06）：カバー率の不足（0..1）＝平価の上でも市場で買う根拠
        double gapLeft = 0;      // ★第2版（2026-09-06）：今月の市場買いの予算＝準備ギャップ（目標カバー率×広義貨幣−基礎貨幣）×売却側と同じ月次率（MintReserveSellRate）
        if (World.Coef.MintReserveTargetWeight > 0)
        {
            double coverage = World.BroadMoney > 1e-9 ? World.BaseMoney / World.BroadMoney : 1.0;
            double coverTarget = World.Coef.GoldCoverMin * World.Coef.GoldRejoinCoverMargin;
            double shortfall = coverTarget > 1e-9 ? (coverTarget - coverage) / coverTarget : 0;
            shortfall = PoliticalQuantities.Clamp01(shortfall);   // NaN は Clamp01 が0＝造幣停止側へ倒す
            mintScale = 1.0 - World.Coef.MintReserveTargetWeight * (1.0 - shortfall);
            mintScale = PoliticalQuantities.Clamp01(mintScale);
            coverShort = shortfall;
            if (World.Coef.MintBuysAtMarket > 0 && shortfall > 1e-9)
                gapLeft = Math.Max(0, coverTarget * World.BroadMoney - World.BaseMoney) * PoliticalQuantities.Clamp01(World.Coef.MintReserveSellRate);   // 買い戻しの歩調＝投げ売りと同じ率（独1873-79の逆向き）
        }
        // ★跛行本位（2026-09-06・器 MintFloorRespectsReserve）：床モードの standing offer は準備が目標を十分超えたら**閉じる**（ラテン通貨同盟 1873-78＝銀の自由鋳造停止）。
        //   これが無いと、金へ移った国が投げ売る銀を残りの銀本位国の造幣局が平価で無限に吸い、銀/金比が1.0に釘付けになる（goldwave 腕で実測＝金圏49/61でも比1.0）。
        //   閾＝目標カバー率×GoldRejoinCoverMargin（再加入と同じ余白＝新しい数なし）。0＝従来（無限の standing offer）。
        bool floorClosed = false;
        if (floor && World.Coef.MintFloorRespectsReserve > 0)
        {
            double cov = World.BroadMoney > 1e-9 ? World.BaseMoney / World.BroadMoney : 1.0;
            double covTarget = World.Coef.GoldCoverMin * World.Coef.GoldRejoinCoverMargin;
            floorClosed = cov >= covTarget * World.Coef.GoldRejoinCoverMargin;   // 目標×余白を超えたら自由鋳造を停止
        }
        if (!floor && mintScale <= 1e-12) return;   // ⚠床モードは mintScale を読まない（床＝無制限の standing offer に「準備が足りたら買わない」を混ぜると床が消える＝罠の記録どおり）
        double parity = Catalog.RefPrices[metal];   // 造幣平価（固定・RefPrice建て）＝史実の固定平価。市場価格がこれを超えれば誰も持ち込まない
        // ★器 MintParitySells の売り予算（2026-09-07・休眠既定0・レビュー是正）：国の状態で1本に決める。準備不足（coverShort>0＝市場買い gapLeft の根）の月は**売らない**
        //   （州ごとの !atMarket では gapLeft が尽きた後続州が同じ月に平価で売る＝買い高・売り安の閉ループ）。今月の売り上限＝準備×MintFloorBuyRate（買い側と同じ月次率）を
        //   **州をまたいで1本**消費する（gapLeft と同じ作法。州ごとに準備×率を掛け直すと 1−(1−率)^N に複利化し先頭の州ほど多く受ける）。0＝sellLeft=0＝厳密恒等。
        double sellLeft = 0;
        if (floor && World.Coef.MintParitySells > 0 && !(coverShort > 1e-9))
        {
            sellLeft = (metal == Catalog.SilverId ? World.SilverReserveQty : World.GoldReserveQty) * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRate);
            if (!(sellLeft > 1e-12)) sellLeft = 0;   // 否定形＝NaN/非正の準備は売らない側へ
        }
        foreach (var r in World.Regions)
        {
            if (r.State != RegionState.Active || r.GoodOf(metal) is null) continue;
            var m = r.Markets[metal];
            bool atMarket = m.Price > parity && gapLeft > 1e-9;   // ★準備不足なら平価の上でも市場で買う（2026-09-06・第2版＝予算 gapLeft が尽きるまで・在庫のある州ならどこでも）
            // ★器 MintParitySells（2026-09-07・休眠既定0）：平価の**上**では造幣局が準備を平価で**売る**＝下の平価買いの厳密な逆操作（固定平価の両側）。
            //   数量＝今月の国1本の予算 sellLeft（準備×MintFloorBuyRate＝買い側と同じ月次率・ループ前で確定）と買い手の貯蓄÷平価の小さい方。買い手＝商人（不在なら投資家＝床の持ち込み手と同じ探索）。
            //   会計＝Savings−value／CumulativeGoldMinted−value（鋳造の逆＝保存量D＝BaseMoney−累積貿易−鋳造−不換 は不変）／準備−qty／Stock＋qty・LastImport＋qty（準備の売却の1本 SellReserve＝投げ売りと同じ在庫恒等式・2026-09-30）。
            //   準備不足の月（sellLeft=0）は売らない。売りは在庫ゲートの**前**（在庫0の州こそ売り先）。売ったあとは従来どおり下の continue（平価の上では買わない）へ落ちる＝買い側は非接触。
            if (sellLeft > 1e-12 && m.Price > parity)
            {
                double sold = SellReserve(r, metal, sellLeft, parity);   // 準備の売却の1本（平価で・下の投げ売りと同じ）
                if (metal == Catalog.SilverId) World.SilverReserveQty -= sold; else World.GoldReserveQty -= sold;
                sellLeft -= sold;                                          // 国1本の月次予算を消費
            }
            if (m.Stock <= 1e-12) continue;
            if (!floor && m.LastProduced <= 0) continue;   // 旧来＝産出のある月しか買わない／★床＝在庫があれば買う（自由鋳造は持ち込み制＝産出と無関係）
            if (m.Price > parity && !atMarket) continue;
            if (floorClosed && !atMarket) continue;   // ★跛行本位：閉じた造幣局は平価で買わない（準備不足の市場買いだけは残る）   // 市場価格＞平価なら鋳造停止（宝飾/工業へ流れる＝正帰還の遮断・史実の固定平価）。★MintBuysAtMarket=0 で従来
            double qty = atMarket ? Math.Min(m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRate), gapLeft / m.Price)   // 市場買い＝床と同じ在庫上限×今月の予算（第1版の産出比例は銀鉱州にしか買い手が立たなかった）
                               : floor ? m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRate)
                               : Math.Min(frac * mintScale * m.LastProduced, m.Stock);
            if (qty <= 1e-12) continue;
            double totOut = 0;
            foreach (var p in r.Producers) if (p.GoodId == metal) totOut += p.Output;
            if (!floor && totOut <= 1e-12) continue;   // 旧来＝国内鉱山限定／★床＝撤廃（パリの造幣局はどこで掘られた銀も受けた＝仏は大産地でないのに世界の緩衝材だった）
            double price = atMarket ? m.Price : parity;   // 鋳造価値＝平価建て（旧 qty×m.Price は正帰還）。★市場買い（MintBuysAtMarket）だけ市場価格＝準備の購入代金
            // ★支払いの一元化 段3-5（2026-09-30・C8＝処理の一元化 段4d）：鋳造の代金は**④の持分**（InternationalMarket.PaySales＝その財の生産者の産出の比・地代→持ち主
            //   〔国営持分は国庫〕・賃金→労働・Earned を通す＝国内の売上と輸出代金と同じ分け方）。旧＝産出の比で持ち主へ・労働へは係数 MintWageShare（0.5）で別に分けた
            //   （④と別の規則・国営分の区分も別）＝係数を削除。**在庫のうち輸入の分**（輸入の量＝前の月の輸入＋この月の輸入〔EconomyStep の商人の取り分の上限と同じ量〕と
            //   この月の産出の比で按分＝在庫の出どころの比）は
            //   輸入の捌き手（州の商人・不在は州の頭数＝PaySales の生産者のいない在庫の枝と同じ）へ＝旧は輸入の在庫も今月の産出の比で国内の鉱山へ払った。
            //   国内に鉱山の無い州（床モード）は全てが捌き手へ（旧＝商人・不在は投資家）。払い手＝造幣局（新しい貨幣＝世界の台帳は鋳造＝移し方の1本の minted）。
            //   受け手のいない分は買わない（量を減らす）＝旧＝商人も投資家も居なければ買わなかった。
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
            // ★★準備需要を**需要として計上する**（2026-08-13・実測で発見した会計の欠落）。
            // ⚠旧＝Stock と LastSold しか書かず `LastDemand` を書いていなかった。`MarketFlow.StepDay` は
            //   totalDemand＝`m.LastDemand`（重み FlowAdjustSpeed）と TargetStock＝TargetStockBuffer×LastDemand を読むので、
            //   **造幣の買いは需要曲線にも在庫目標にも存在しなかった**＝在庫を削る片肺の経路しか通らない。
            //   実測＝造幣は世界の銀産出の12.8〜17.7%を毎月吸っているのに、銀/金の相対価格は151年で2〜3%しか動かない。
            //   これが「本位金属の準備需要が消えれば需要曲線そのものが下へ落ちる」（銀本位doc §3-1の心臓）が効かない理由である。
            // ★市場外の買い手はこのコードベースの作法として全て LastDemand を計上している
            //   （Colonial.cs:234/325 の捕囚市場・EconomyStep.cs:1271 の国家購入・Transport.cs:72 の鉄道船）。造幣だけが外れていた。
            // ⚠0（対照腕）で書かない＝厳密恒等。既定1.0でlive（★点火2026-08-14。金の造幣は既定 live なので、点火は必ず単独バッチで測る）。
            if (World.Coef.MintDemandCounted > 0) m.LastDemand += qty * World.Coef.MintDemandCounted;
            //（鋳造の台帳＝基礎貨幣増の正当な源は、上の移し方の1本〔minted〕が動いた額で書いた＝MoneyConservation が許容）
            // ★床モード＝吸った現物を国の準備として保持（旧来は「コインになって消える」＝追跡しない＝恒等）。
            //   法が本位を捨てたとき、この準備が市場へ売られる＝暴落を深める投げ売りが台本なしで出る。
            if (floor) { if (metal == Catalog.SilverId) World.SilverReserveQty += qty; else World.GoldReserveQty += qty; }
        }
    }

    /// <summary>★demonetization（2026-08-14・床モードのみ）：法が捨てた本位金属の準備を毎月 MintReserveSellRate で首都州の市場へ売る（<see cref="SellReserve"/>＝市場価格で）。
    /// ⚠鋳造時に発行された貨幣は流通し続ける（demonetized coins＝不換のオーバーハング）。★2026-09-30（段3-5・C8）：売った代金は買い手（捌き手）から造幣局へ＝貨幣の回収
    ///   （旧＝金は動かさず、在庫へ入った金属の国内の売上は輸入の捌き手の商人へ＝払っていない積荷の代金が商人の所得になった）。独は売却額を回収し、銀安で損を出して1879年に売却を停止した。</summary>
    private void SellDemonetizedReserves(int lawMetal, bool isFiat)
    {
        double rate = PoliticalQuantities.Clamp01(World.Coef.MintReserveSellRate);
        if (rate <= 0) return;
        if (World.SilverReserveQty > 1e-9 && (isFiat || lawMetal != Catalog.SilverId))
            World.SilverReserveQty -= SellReserve(World.Region, Catalog.SilverId, World.SilverReserveQty * rate);
        if (World.GoldReserveQty > 1e-9 && (isFiat || lawMetal != Catalog.GoldOreId))
            World.GoldReserveQty -= SellReserve(World.Region, Catalog.GoldOreId, World.GoldReserveQty * rate);
    }

    /// <summary>★準備の売却の1本（支払いの一元化 段3-5・C8・2026-09-30＝旧 DumpReserve と平価の売り〔休眠 MintParitySells〕の2か所を畳んだ）：
    /// 造幣局が金属の準備を州の捌き手（商人・不在は投資家・どちらも居なければ売らない）へ <paramref name="price"/>（省略時＝その州の市場価格）で売る。
    /// 代金は買い手から造幣局へ（移し方の1本・minted＝鋳造の逆＝貨幣の回収＝保存量 D は不変）・金属は市場の在庫へ（輸入＝在庫の恒等式 Stock＝前在庫＋生産＋輸入−販売−輸出）
    /// ＝買い手はその在庫を国内で売る輸入の捌き手（EconomyStep の商人の取り分）。払えるのは貯蓄まで＝払えない分は売らない（量を減らす）。戻り値＝売った量（呼び手が準備から引く）。</summary>
    private double SellReserve(Region r, int gid, double qty, double price = double.NaN)
    {
        if (!(qty > 1e-12) || r.State != RegionState.Active || r.GoodOf(gid) is null) return 0;
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

    /// <summary>出口＝返済規律（POPの個人債務・設計 docs/20260802・2026-08-02）。黒字が出たらまず返す。
    /// ★★**RemainingBudget ではなく <c>Savings</c> 自体を削る**のが唯一整合的な形である。
    ///   理由＝投資家だけが二重の予算を持つ（消費予算＝<c>RemainingBudget×(1−IndInvestmentFrac)</c>／
    ///   投資予算＝<c>IndInvestmentFrac×Savings</c>）。RemainingBudget 側だけ削ると投資予算が削れず、
    ///   投資側だけ削ると消費予算が削れない。**Savings を先に削れば両者が同じ縮んだ値を読む**＝自動的に整合する。
    /// ★これは backstop（入口A）の厳密な逆操作で、<c>BaseMoney = TotalMoney + Treasury − OutstandingLoans</c> を1ビットも動かさない。
    /// ⚠置き場所＝<c>EconomyStep.Run</c> の**直前**（設計は EconomyStep 内を指すが、あの署名は World も Bank も見えない）。
    ///   月次順序としては設計どおり「消費の前」で同じ。
    /// ⚠刻みは月次（利息の日次に揃えない＝Credit 側が強いられている「月次利潤を/30」の歪みを再導入しないため）。
    /// ⚠税との順序＝月初に置いたので**国家は債務に劣後する**（借金を返してから税を払う）＝設計§11-1の裁定。
    /// ⚠走査は Active のみ（EconomyStep と同じ範囲）。backstop は無フィルタで**走査範囲が元から非対称**＝設計§11-2の裁定。</summary>
    private void RepayHouseholdDebt(Region r)
    {
        if (!(World.Coef.PopDebtDriven > 0) || !(World.Coef.DebtRepayFrac > 0) || World.Bank is null) return;
        foreach (var p in r.Pops)
        {
            if (!(p.Debt > 0)) continue;   // ⚠否定形＝NaN も飛ばす
            double pay = Math.Min(p.Debt, Math.Max(0, p.Savings) * World.Coef.DebtRepayFrac);
            if (!(pay > 0)) continue;
            Bank.Lend(World, p, -pay);   // 返済＝貸しの逆（Bank.Lend の1本）
            World.HouseholdDebtRepaid += pay;   // 診断。⚠LossFinanced とは**別カウンタ**（硬性ゲートの分子を汚さない・設計§6-1）
        }
    }

    /// <summary>
    /// 損失吸収の脆さの会計（§9-C 拡張）：月次の全フロー後、損失で貯蓄が負になった pop の不足を信用（借入）で埋める。
    /// 貯蓄→0・Bank.Loans＋不足 ＝ BaseMoney(=TotalMoney+Treasury−Loans) を不変に保つ（保存則厳守）。資本家は損失を負債として抱え、
    /// 貯蓄は0で下げ止まる＝有限責任。慢性赤字は負債(OutstandingLoans)の増加として可視化される（隠蔽しない）。
    /// ★支払いの一元化 段2-2（B7＋A8・作者裁定 Q11・2026-09-29）：**全ての世界に銀行がある**（Scenario の CreditEnabled を削除＝構築で必ず Bank を作る）＝受け皿は与信の1本。
    /// 旧＝銀行の無い世界（試験世界）では ①州の最富裕POP ②ほかの正の貯蓄の全員 から取って埋め（他人から取る＝規則(2)の唯一の違反）、
    /// ③それでも残る不足を非常発行として CumulativeFiatIssued に積んだ（消えた不足を不換の発行の名目で隠す＝保存の検査 ProbeCheck が漏れを正当と見なした）。
    /// <para>★返せない与信の償却（作者裁定 E6 の2歩目・2026-09-28）：埋めた後も基礎貨幣が負の国は、負の分だけ銀行の与信（<see cref="Bank.Loans"/>）を
    /// 償却し、同じ額を非常発行として台帳（<see cref="World.CumulativeFiatIssued"/>）に積む（貨幣を作る）。
    /// 基礎貨幣が負＝与信が国内の貨幣の全部（貯蓄＋国庫）を越えた＝越えた分は国の中のどのお金でも返せない。回収は基礎貨幣を動かさない
    /// （預金と与信が同じ額だけ減る）ので、負の国は回収では戻らない（(82) の §2）。償却は与信の残高まで（国庫の負の分は与信でない＝残る）。
    /// ⚠この月に上で積んだ損失の負債化も同じ残高に入る＝負の国では、上限（A16）の外で積む与信も同じ規則で閉じる。
    /// ⚠家計の与信（<see cref="Bank.HouseholdLoans"/> と <see cref="Pop.Debt"/>・休眠の器 PopDebtDriven）は償却しない：借りる枠が借り手の所得で決まり
    /// （A16 と同じ「返せる額」）、返済の出口を自分で持つ。点火するときは同じ規則をここへ（お金の書き手を増やさない＝WriterRatchetTests）。</para>
    /// </summary>
    internal void BackstopNegativeSavings()
    {
        var bank = World.Bank!;   // 構築で必ず作る（Simulation のコンストラクタ）
        List<Pop>? one = null; List<double>? unit = null;   // 受け手1人の並び（使い回す＝POP ごとに確保しない）
        foreach (var r in World.Regions)
            foreach (var p in r.Pops)
                if (p.Savings < 0)
                {
                    double need = -p.Savings;
                    // ★2026-09-30 段4g：不足を借入（負債↑）＝移し方の1本（世界の外の側＝銀行の与信 Outside.Credit）＝貯蓄は 0 ちょうどへ（x+(−x)＝0）・与信が同じ額だけ増える
                    //   （旧＝与信と貯蓄を手で書いた2行＝下げ止まり＝TotalMoney＋不足, Loans＋不足 で相殺＝保存）。受け手は重み1（頭数0の POP でも受ける＝旧と同じ）。
                    (one ??= new List<Pop>(1)).Clear(); one.Add(p);
                    World.Transfer((null, null), (World, one, unit ??= [1.0]), need, outside: World.Outside.Credit);
                    World.LossFinanced += need; // 可視化：損失の負債化を累積記録
                    // ★入口A（損失の記憶・2026-08-02）：**記録を併記するだけ**＝貸し手側（Bank.Loans）は1ビットも変えない。
                    //   PopDebtLossShare は「消えた赤字のうち個人が背負う割合」＝有限責任の度合い（1.0で無限責任）。
                    //   ⚠これが無いと、赤字は毎月0へ均されて**誰の記憶にも残らず**、デレバレッジが起きない。
                    if (World.Coef.PopDebtDriven > 0) p.Debt += need * World.Coef.PopDebtLossShare;
                }
        // ★E6：負の分だけ与信を償却（基礎貨幣は償却の額だけ上がり、非常発行が同じ額だけ台帳に積まれる＝保存量は動かない）。
        //   ⚠NaN は比較が偽＝何もしない（否定形の門）。償却の後の基礎貨幣＝0（与信が足りれば）か国庫の負の分（与信を全部消しても残る分）。
        double writeOff = Math.Min(-World.BaseMoney, bank.Loans);
        if (writeOff > 0) { bank.Loans -= writeOff; World.CumulativeFiatIssued += writeOff; }   // 非常発行の台帳（負の国の与信の償却だけ）
    }


    // ⚠撤去2026-09-04（J-005）：TriggerGoldRush（暦年1848×州名「西部」の台本）を関数ごと削除。到達不能になったため。

    // 植民地素材の入手率＝植民地保有 × 制海（禁輸/交戦で交易アクセス↓）。外交なしなら満額。
    private double ColonialAccess()
    {
        if (World.Diplomacy is null) return 1.0;
        var p = World.SelfPower ?? World.Diplomacy.Player;
        if (p is null) return 1.0;
        double byColonies = PoliticalQuantities.Clamp01(p.Colonies / World.Coef.ColoniesForFullColonial);
        return byColonies * World.Diplomacy.TradeAccess(p);
    }

    private void CheckInvariants()
    {
        foreach (var inv in _invariants) inv.Check(World);
    }

    /// <summary>近代化の経路を分類（§B-5・M5）。</summary>
    public ModernizationPath ClassifyPath()
    {
        var c = World.Coef;
        bool industrialized =
            World.TechLevel > c.IndustrializedTechThreshold || World.UrbanShare > c.IndustrializedUrbanThreshold;
        if (!industrialized) return ModernizationPath.Failure;
        return World.Polity switch
        {
            Polity.ConstitutionalMonarchy or Polity.Republic => ModernizationPath.Meiji,   // 自由な近代化
            Polity.ProletarianDictatorship => ModernizationPath.Radical,
            _ => ModernizationPath.Tokugawa,                                                // 権威主義下の近代化
        };
    }

    private void Snapshot()
    {
        if (!TelemetryEnabled) return;   // 測定短縮①（2026-07-09）：selfplay/バッチは読者なし＝四半期スナップショットの割当と保持を止める（テストは既定trueのまま）
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
        double gdp = World.Gdp;   // 役務を除いた産出×参照価格（旧実装は r.Goods をIDで添字していたが、ソート後は不一致＝集計バグ。World.Gdp に統一）
        double inv = sizeSum > 1e-9 ? 1.0 / sizeSum : 0.0;

        Telemetry.Add(new TurnSnapshot(
            World.Year, World.Quarter, World.TotalMoney, World.TotalPopulation,
            World.UrbanShare,
            foodMkt.Price, foodProd.Output, foodMkt.Stock,
            manufPrice, farmerSat, workerSat,
            World.TechLevel, litSum * inv, gdp,
            (int)World.Polity, conscSum * inv, milSum * inv,
            World.ReformistPower, World.Coercion,
            World.BroadMoney, World.OutstandingLoans, World.Bank?.Confidence ?? 1.0,
            World.Treasury, World.FiscalFundingRatio,
            World.Regions[0].SumPopulation(), World.Regions.Count > 1 ? World.Regions[1].SumPopulation() : 0.0,
            World.Bank?.Gold ?? 0.0, World.CumulativeTradeFlow));
    }
}
