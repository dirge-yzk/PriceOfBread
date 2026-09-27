namespace PriceOfBread.SimCore;

/// <summary>
/// 複数国家の並列シミュレーション（§6.1 国際層）。各国は独立した94財経済（<see cref="Simulation"/>）を回し、
/// **共有外交**（<see cref="Diplomacy"/>）で相互作用する：緊張→戦争、戦費（自国 pop へ還流）、賠償（国家間の対外移転）、
/// 植民地の争奪（勝者が植民地素材の入手率を得る）、覇権防止（大国ほど周囲の緊張を集め孤立）。
/// 各国の保存則（§11.6）は自国 CumulativeTradeFlow で閉じる（賠償は両国の貿易収支に相殺計上）。
/// </summary>
public sealed class MultiNationSim
{
    public List<Simulation> Nations { get; } = [];
    public Diplomacy Diplomacy { get; } = new();
    public InternationalMarket Market { get; }
    public DeterministicRng Rng { get; }
    private readonly DeterministicRng _weatherRng;   // 緯度帯天候の専用系列（外交Rng・各国Rng・疫病系列と分離＝較正/盤面非依存）
    private readonly Dictionary<int, double> _prevBandFactor = new();   // 帯ごとの前年 WeatherBandFactor（AR(1)の帯別遅延項・WeatherBandPersistence）
    private readonly Coefficients _coef;
    private readonly ulong _seed;   // 建国スポーンの国別乱数種の派生元（MixSeed(_seed, 新index)＝純関数・決定論）。リフレクション直列化で自動保存
    internal Coefficients Coef => _coef;   // 共有外交/国際市場の係数（テストのON経路検証用にinternal公開・InternalsVisibleTo）
    /// <summary>地図が**実際に使われた航路**を太さ付きで描くための半減距離km。
    /// ⚠旧・重力カーネル（世界の交易重心・単一世界価格）は2026-08-12撤去＝現在の清算（二国間交易）はこの値を読まない。
    /// RegionalDistanceHalf の読み手はこの地図描画スケールのみ（死蔵係数を1本だけ描画用に流用）。</summary>
    public double TradeGravityHalfKm => _coef.RegionalDistanceHalf;   // （EraRegionalHalf は読み手ゼロで2026-08-29撤去＝×1恒等だった）
    private int _quarter;
    private bool _calendarFrozen;   // 開始前の助走中＝共有層の四半期カウンタも止める（World.WarmupFrozen と対。WarmupEconomy だけが立てて必ず戻す）
    private int _nextPowerId;   // ★建国スポーンの採番（wave2b是正）：Diplomacy.Powers.Count は RemovePower 後に既存Idと衝突する（再併合で欠番が生まれても番号を再利用しない・単調増加）

    // マジックナンバー命名（挙動不変・値は据え置き）。中央 Coefficients には入れない当ファイル固有のハードコード値。
    private const double DefaultTradeWeightBase = 0.15;              // 交易比重の基準値（初期国の基数・分離独立国の既定）
    private const double TradeWeightIndustrializationSlope = 0.10;   // 工業化度あたりの交易比重の増分（初期国のみ）
    private const double SocialSecurityLawMaxLevel = 3.0;            // 社会保障法の最上位段（年金=3）＝法段を目標[0,1]へ正規化する除数（2026-07-21「なし」廃止・4段）
    private const double MigrationFareDistanceUnitKm = 1000.0;       // 渡航運賃の距離単位（MigrationFarePerkKm＝この距離あたりの運賃）
    private const double RumpGdpFractionOfParent = 0.01;             // 残骸国家判定＝親GDP比のこの割合未満が続いたら再併合対象
    private const double EpidemicTransmitSeverityFloor = 0.5;        // 国家間伝播で発症した疫病の重症度の下限（上限は+NextDouble()で1.5まで）

    // splitmix64（Diplomacy.Hash01と同系の混合）：国別乱数種の生成（他16）。
    private static ulong MixSeed(ulong seed, ulong i)
    {
        ulong x = seed + 0x9E3779B97F4A7C15UL * (i + 1);
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    /// <param name="coef">★2026-08-26：係数を外から渡せるようにした（既定 null＝new Coefficients()＝従来と厳密同値）。
    /// ⚠**構築時に走る機構**（植民地の州昇格 ColoniesAreRegions）が出来たので、構築後に w.Coef を書き換えても間に合わない。
    ///   テストが旧経路（Colony オブジェクトの入植者昇格）を検証するには、構築の前に落とす口が要る。</param>
    public MultiNationSim(IReadOnlyList<NationConfig> configs, ulong seed, Func<NationConfig, INationAgent>? agentFactory = null, Coefficients? coef = null)
    {
        _coef = coef ?? new Coefficients();
        _seed = seed;
        Rng = new DeterministicRng(seed ^ 0x9E37_79B9);   // 外交用の独立系列（各国の Rng とは別）
        _weatherRng = new DeterministicRng(seed ^ 0xC11A7E_5EEDUL);   // 緯度帯天候の独立系列
        Diplomacy.UnificationRng = new DeterministicRng(seed ^ 0xF0E1_D2C3_B4A5UL);   // P4 統一戦争の専用系列（既存系列のdraw数を乱さない・主フラグOFFでは未消費）
        Diplomacy.SubmitRng = new DeterministicRng(seed ^ 0xA5B4_C3D2_E1F0UL);        // ★屈服の確率化（AI審査3-1）の専用系列（SubmitProbabilistic=0では未消費＝恒等）
        Market = new InternationalMarket(Catalog.Goods.Length, _coef.WorldPriceInitFromRefPrice);   // A-低7（2026-07-24配線・点火2026-07-27）：世界価格の初期値をRefPriceから初期化（既定1.0）。0で全財1.0の旧初期化
        for (int i = 0; i < configs.Count; i++)
        {
            var cfg = configs[i];
            // agentFactory 省略時は NullAgent（放置＝検証ハーネス用の後方互換）。自己対戦では効用AIを差す。
            // 国別乱数種＝splitmix64混合（監査2026-07-12 他16・裁定同梱）：旧 seed+i+1 の直接種付けは「試合sの国i」と「試合s+1の国i−1」が
            // 同一系列＝batch30の試合間の統計的独立性を毀損していた。混合で全ペアが独立系列に。決定論は不変（純関数・同seed同結果）。
            var sim = new Simulation(cfg.ToScenario(MixSeed(seed, (ulong)i), sharedWorld: true), agentFactory?.Invoke(cfg) ?? new NullAgent());
            var power = new Power
            {
                Id = i, Name = cfg.Name, BaseName = cfg.Name, Nation = sim.World,
                // ★2026-08-02：初期ゲージも**兵士POPから導出**する（cfg.Military の手置きを撤去）。
                // ⚠旧は手置きの数を入れていたが、最初の Diplomacy.Step が兵士POPから作り直すので、
                //   **1783年の画面だけが別のスケール**を映していた（実測＝英450→翌年20＝20倍の段差）。
                //   同じ式で播けば段差が消える（充足度と質の乗数は初回Stepで乗る）。
                Military = sim.World.SoldierSize() / _coef.SoldiersPerMilitary,
                Gdp = sim.World.Gdp,
                TradeWeight = DefaultTradeWeightBase + TradeWeightIndustrializationSlope * cfg.Industrialization,   // 倍率排除⑤：工業先進国ほど交易比重が大きい（旧 ProductivityBonus 駆動→工業化度へ。発展度の状態変数から創発）
            };
            foreach (var (cname, cres, cpop) in cfg.InitialColonies) { var cc = Geo.ColonyCoord(cname); power.ColonyList.Add(new Colony { Name = cname, Resources = [.. cres], Population = cpop, PopSize = cpop * _coef.ColonialPopScale, Latitude = cc.Lat, Longitude = cc.Lon }); }   // 名前＋固有資源＋人口規模＋座標（史実1780）。住民数は人口ウェイト×換算＝作成時の1か所（回1-1・col-01）
            sim.World.Diplomacy = Diplomacy;     // 読み取り用（植民地アクセス・交易）
            sim.World.SelfPower = power;
            // ★近代化投資レバーの出発点を全国で揃える（2026-08-02・UtilityAgent.OpeningModernizationFrac を見よ）。
            // ⚠既定0のままだと、AIだけが毎ステップ自分で設定し、NullAgent のプレイヤー国は**永久に0**＝
            //   国家による工業化が一度も起きない（実測でQ24に14倍の差）。AIは次のステップで上書きする＝較正に無影響。
            sim.World.ModernizationFrac = UtilityAgent.OpeningModernizationFrac;
            sim.World.StepsDiplomacy = false;    // 共有外交は本クラスが進める
            sim.World.CoercionFollowsForce = true;   // 武力統合：実ゲーム＝多国シム＝治安維持力は軍事費スライダーから導出（治安維持力スライダー廃止）。較正/単体は単一 Simulation＝既定 false で旧挙動
            Diplomacy.Powers.Add(power);
            Nations.Add(sim);
            // 案Y（人口物差しの脱汚染・2026-08-21）：1783年の州所有を恒久ラベルとして焼き込む＝「故地」判定の基準。
            // 征服割譲で他国州を得ると持ち込み人口（AnnexBaggage）が記帳され PopGrowth の分子から除かれるが、
            // 故地への復帰（OriginPowerId 一致＝再征服・回復）は記帳0＝清の太平天国回復型が帯へ戻れる。書くだけ＝力学は読まない。
            foreach (var r0 in sim.World.Regions) r0.OriginPowerId = power.Id;
        }
        WireTransitPorts();   // Stage C（裁定A3）：内陸国の外国港トランジット候補＝全国が出揃った後の国境跨ぎ配線（WirePortDistances は国内のみ）。TransitEnabled=0 では読まれない＝恒等
        // 清・インド・東南アジア・近東・中央アジア・北アフリカ温帯は実国化（Phase 3・world.json 駆動）＝抽象 Power は廃止（上の configs ループが生成）。
        // 能動侵略の抽象標的（段C・案Y）：Nation=null・Gdp=0＝ThreatTension を生まず自動では緊張も溜まらない＝Invade 動詞でのみ取れる。熱帯内陸（アシャンティ/ソコト/コンゴ）はマラリア付き（硬直でなく実コストで律速）。
        // 抽象標的・史実ライバル・従属・海峡運河は WorldData.Setup()（world.json、無ければ BootstrapSetup）へデータ化。順序厳守＝Power.Id は Powers.Count で連番付与＝配列順が挙動を決める。
        var setup = WorldData.Setup();
        foreach (var t in setup.Targets)
        {
            var p = new Power { Id = Diplomacy.Powers.Count, Name = t.Name, BaseName = t.Name, Military = t.Military, Gdp = 0, Treasury = t.Treasury, TradeWeight = 0.0, RequiresQuinine = t.Quinine };
            foreach (var c in t.Colonies) { var cc = Geo.ColonyCoord(c.Name); p.ColonyList.Add(new Colony { Name = c.Name, Resources = [.. c.Resources], Population = c.Population, PopSize = c.Population * _coef.ColonialPopScale, Latitude = cc.Lat, Longitude = cc.Lon, Malarial = t.Quinine }); }   // ★回1-1（col-01）：住民数も作成時に置く（旧＝未設定のまま侵略で州化され、住民0・耕地1.8の州になった＝約650万人分が消えた）。熱帯(Quinine)標的の州はマラリア地＝征服で本国へ風土病が付いて回る。Resourcesは試合ごとに複製（キャッシュ共有回避）
            Diplomacy.Powers.Add(p);
        }
        _nextPowerId = Diplomacy.Powers.Count;   // ★建国スポーンはここから単調採番（起動時のId=Powers.Countの連番と同じ値で始まり、以後は欠番があっても戻らない）

        Diplomacy.TradeAccessNormalizedGate = _coef.TradeAccessNormalized;   // ★貿易復活④：交易アクセスの正規化ゲート（0=旧式ビット恒等）
        SeedRivalries(setup);   // 初期の史実ライバル（必要悪：これが無いと ThreatTension/Ideology だけでは戦争が点火せず＝load-bearing な初期条件）
        SeedDependencies(setup);   // 初期の史実従属（属国一本化）＝シャムの朝貢属国・ムガル傀儡

        // 海峡・運河（§B）：自然海峡は開通済み、運河は host 保有国の資本家が建設して開通。扼者は host から毎四半期解決（征服で交代）。**Id/BuildCost/TechGate はデータ側（world.json）が真実源**（2026-07-28・CanalKey 分岐を廃止）。
        foreach (var cp in setup.Chokepoints)
        {
            Diplomacy.Chokepoints.Add(new Chokepoint
            {
                Id = cp.Id, Name = cp.Name, Host = cp.Host, IsOpen = cp.IsOpen, IsCanal = cp.IsCanal,
                BuildCost = cp.BuildCost, TechGate = cp.TechGate, ServesBasins = [.. cp.ServesBasins],
                ExitOfBasin = cp.ExitOfBasin is { } eb && System.Enum.TryParse<Basin>(eb, out var ebv) ? (int)ebv : -1,   // 13l地政学：閉鎖海盆の出口（データ側が真実源・不正名は-1＝該当なしへ倒す）
            });
        }

        // 巨大建造物（2026-07-28）：**MegaprojectEnabled=0 では1件も生成しない**＝以後の全処理が空回りせず厳密恒等。
        // host は州名のみ（植民地は host にしない）＝将来の植民地→州の格上げで自然に建てられるようになる。
        if (_coef.MegaprojectEnabled > 0)
            foreach (var mp in WorldData.Megaprojects())
                Diplomacy.Megaprojects.Add(new Megaproject
                {
                    Id = mp.Id, Name = mp.Name, Host = mp.Host, BuildUnits = mp.BuildUnits, TechGate = mp.TechGate,
                    EffectTarget = mp.EffectTarget, EffectMode = mp.EffectMode, EffectValue = mp.EffectValue,
                });

        // 国家ランク（列強/中堅/弱小）を開始盤面のスコアで初期化。Diplomacy.Step は四半期末にしか走らないため、
        // これが無いと最初の四半期が締まるまで RankOf が既定値=弱小を返し、UI で全国が「弱小」に見える（純関数＝ダイナミクス不変・四半期末に再計算で上書き）。
        Diplomacy.ComputeRanks(_coef);

        // ★植民地の州昇格（2026-08-26・ColoniesAreRegions）＝開始時点で全植民地を州にする。
        //   ⚠植民地総数の基準線を取る**前**に走らせる＝昇格した分は最初から植民地でない（保存検査の基準線と整合）。
        if (_coef.ColoniesAreRegions > 0) PromoteAllColonies();

        // 植民地総数の初期値（監査2026-07-12 死12）：Validationの保存検査の基準線＝植民地は移転（戦争・植民地化）と独立（IndependentColoniesへ）だけで増減しない。
        foreach (var p in Diplomacy.Powers) Diplomacy.InitialColonyCount += p.Colonies;
        foreach (var p in Diplomacy.Powers) Diplomacy.InitialColonyMass += p.ColonyMassUnits(_coef.ColonialPopScale);   // ★質量→個数単位の換算の錨（2026-08-28）
    }

    /// <summary>★開始時点の一括昇格（2026-08-26・ColoniesAreRegions）。入植者昇格（Colonial.Step）と**同じ動詞**を通す
    /// ＝人口・蓄え・資源賦存・気候ゲート・保存の記帳を二重定義しない。格は Colonial のまま（議会代表なし・維持費あり）。
    /// ⚠同名の州が既に居る国では昇格しない＝州名の一意性（座標表・地図・扼者解決のキー）を守る＝既存ガードと同じ規律。</summary>
    private void PromoteAllColonies()
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            if (w.SelfPower is not { } sp) continue;
            var cols = new System.Collections.Generic.List<Colony>(sp.ColonyList);
            foreach (var col in cols)
            {
                if (w.Regions.Exists(r => r.Name == col.Name)) continue;
                sp.ColonyList.Remove(col);
                // ⚠PromotedColonies には数えない＝これは**初期条件**であってプレイ中の昇格ではない。
                //   数えると保存検査（Validation「植民地の保存」＝残り＋独立＋昇格＝初期）が二重計上で落ちる
                //   （実測 37+0+42=79 vs 初期43）。初期の総数はこの直後に Colonies から取り直される。
                Simulation.PromoteColonyToRegion(w, _coef, col, RegionTier.Colonial);
            }
        }
    }

    /// <summary>経路グラフ第1段 Stage C（03 §11-6・裁定A3・2026-08-09）：港州ゼロ国の各州へ、外国港州トランジットの静的候補（上位2件）を配線する。
    /// ・純地理（PathGraph は国境を知らない）＝構築時に1回だけ計算。Region の string/double フィールドに記憶
    ///   （NearestPortName/PortLandKm と同型＝リフレクション直列化に安全な型・ロード後も値がそのまま復元＝再計算不要でビット一致）。
    /// ・候補1＝陸グラフ最短の外国港州。候補2＝候補1と**構築時所有国が違う**次点（戦時封鎖の迂回先＝同一所有国の第2港は封鎖で一緒に閉まるので迂回にならない）。
    /// ・現所有国の解決（征服で動く）と交戦判定は四半期側（InternationalMarket.Step）＝ここは距離だけ。
    /// ・到達不能（別陸塊・孤立成分）は候補にしない＝内陸国は定義上どこかの大陸にいるので通常は必ず届く。
    /// ・植民地昇格などで後から生まれる州・分離独立の新国家は初期配線の対象外だが、下の RewirePorts が四半期ごとに盤面から張り直す。
    /// ・TransitEnabled=0 ではどこからも読まれない＝厳密恒等（既定1.0＝点火済み）。決定論＝距離同率は港州名の序数順。</summary>
    // ★港の配線を領土に追随させる（W3・2026-08-13／案1へ簡素化・2026-09-21・doc 20260812_港の配線を領土に追随させる・20260816_港配線の簡素化案）。
    //
    // 旧＝トランジットの配線は**構築時に1回**しか張られず、内陸国が港を割譲されても外国港への通行料を払い続け、
    // 逆に港を失っても自国港のまま扱われた。
    //
    // 触発の仕方＝**呼び出し忘れが起きない形**にする。割譲・分離独立・植民地の州昇格は別ファイルに散らばっており、
    // 「territory が動いたら Mark を呼ぶ」規約は必ずどこかで破られる（本プロジェクトの前科＝再配分側の lawCap 掛け忘れ）。
    // ⚠旧はそこへ**国ごとの領土署名（FNV-1a）を毎四半期比べる層**を置いていたが、キャッシュ無効化は状態管理で最も事故りやすい
    //   種類の複雑さ（署名・呼び忘れ・同時性・セーブ追随が全部付いてくる）。配線は盤面の純関数（乱数不使用・冪等）なので、
    //   ★2026-09-21＝**署名の層を削除**し、毎四半期・全国を無条件で張り直す（案1）。状態ゼロ＝セーブ追随の考慮も消える。
    //   恒等の実証＝seed 1/6/25 の `selfplay 1 200` が署名層ありと sha256 で一致。⚠旧の「ビット恒等でない」根拠（構築時に候補へ入った
    //   Locked 沿岸辺境＝ウラジオストク）は **2026-08-26 の辺境の廃止（Simulation.cs:571-573）で盤面から消えている**＝構築時 Locked の州は無い。

    /// <summary>港の配線を盤面から張り直す（四半期末に1回・無状態＝doc 20260816 案1）。冪等＝盤面だけから決まる・RNG不使用。
    /// ★PortRewireEnabled>0.5（既定1.0＝点火2026-08-16）：領土（active州の集合）へ追随して WirePortDistances を張り直す（是正1）。
    ///   0 では最寄り港を張り直さない＝構築時1回の凍結配線（旧挙動）。
    /// 通過港は係数によらず毎四半期張り直す（是正3＝港州0になった国へ WireTransitPorts の hasPort 現在判定で配布・
    /// 内陸州だけの移動でも移動州の TransitPort* が現所有構成で引き直る）。TransitEnabled=0 では誰も配線を読まない＝張り直しても挙動不変。</summary>
    private void RewirePorts()
    {
        if (_coef.PortRewireEnabled > 0.5)
            for (int i = 0; i < Nations.Count; i++) Simulation.WirePortDistances(Nations[i].World.Regions, rewire: true);
        foreach (var n in Nations)
            foreach (var r in n.World.Regions) { r.TransitPortName = ""; r.TransitPort2Name = ""; r.TransitLandKm = 0; r.TransitLand2Km = 0; }
        WireTransitPorts();
    }

    private void WireTransitPorts()
    {
        // 全国の港州（座標つき）＝候補プール。資格は WirePortDistances と同じ（IsPort＋Climate座標あり）
        var ports = new List<(int Nation, string Name, int Node, double Graft)>();
        var hasPort = new bool[Nations.Count];
        for (int i = 0; i < Nations.Count; i++)
            foreach (var r in Nations[i].World.Regions)
                if (r.IsPort)
                {
                    var pc = Climate.CityCoord(r.Name);
                    if (pc.Lat == 0 && pc.Lon == 0) continue;
                    hasPort[i] = true;
                    int node = PathGraph.NearestNode(pc.Lat, pc.Lon);
                    if (node < 0) continue;
                    ports.Add((i, r.Name, node, Geo.DistanceKm(pc.Lat, pc.Lon, PathGraph.Nodes[node].Lat, PathGraph.Nodes[node].Lon)));
                }
        if (ports.Count == 0) return;   // 港のない世界（較正・最小テスト世界）＝対象なし
        // ★2026-09-26（econ-04 の残り）：州ごとに四半期ごと Dijkstra（LandDistancesFrom）を回し直していた＝起動時に1回だけ作る全点対の表
        //   PathGraph.LandMatrix の行を読む（表の各行は LandDistancesFrom(s) そのもの＝同じ double が返る＝厳密恒等）。
        var landMatrix = PathGraph.LandMatrix();
        int landN = PathGraph.Nodes.Length;
        for (int i = 0; i < Nations.Count; i++)
        {
            if (hasPort[i]) continue;   // 港のある国は既存の自国港3脚（WirePortDistances）が担う
            foreach (var r in Nations[i].World.Regions)
            {
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;   // 座標なし州＝旧フォールバック
                int src = PathGraph.NearestNode(cc.Lat, cc.Lon);
                if (src < 0) continue;
                int row = src * landN;   // 全点対の表の src 行＝src から全ノードへの陸路距離（接ぎ木なし）
                double srcGraft = Geo.DistanceKm(cc.Lat, cc.Lon, PathGraph.Nodes[src].Lat, PathGraph.Nodes[src].Lon);
                var cands = new List<(double Km, string Name, int Owner)>();
                foreach (var p in ports)
                {
                    if (p.Nation == i) continue;   // 外国港のみ（港州ゼロ国では自国港は空集合＝実質全港）
                    double d = landMatrix[row + p.Node];
                    if (double.IsPositiveInfinity(d)) continue;   // 陸路で届かない＝トランジット不能
                    cands.Add((d + srcGraft + p.Graft, p.Name, p.Nation));
                }
                if (cands.Count == 0) continue;
                cands.Sort((a, b) => { int c0 = a.Km.CompareTo(b.Km); return c0 != 0 ? c0 : string.CompareOrdinal(a.Name, b.Name); });   // 同率は名前順＝決定論
                r.TransitPortName = cands[0].Name; r.TransitLandKm = cands[0].Km;
                foreach (var cd in cands)
                    if (cd.Owner != cands[0].Owner) { r.TransitPort2Name = cd.Name; r.TransitLand2Km = cd.Km; break; }
            }
        }
    }

    // 史実的なライバル構図の初期緊張（名前で引く。無ければ何もしない）。覇権は ThreatTension が自動で生む。
    // 初期の史実ライバル（必要悪）。創発の ThreatTension/Ideology だけでは開戦が点火しないため、初期条件として最小限の緊張を置く。以後は減衰し創発が引き継ぐ。
    private void SeedRivalries(WorldData.WorldSetup setup)
    {
        Power? P(string name) => Diplomacy.Powers.Find(p => p.BaseName == name);
        foreach (var r in setup.Rivalries) { if (P(r.A) is { } pa && P(r.B) is { } pb) Diplomacy.SeedTension(pa, pb, r.Tension); }
    }

    // 初期の史実従属関係（属国・一本化）。以前は属国モデルが無く繰延だった項目を、従属機構で実体化。
    private void SeedDependencies(WorldData.WorldSetup setup)
    {
        Power? P(string name) => Diplomacy.Powers.Find(p => p.BaseName == name);
        foreach (var d in setup.Dependencies) { if (P(d.Suzerain) is { } s && P(d.Dependent) is { } dep) Diplomacy.SetDependency(s, dep); }
    }

    // 分離独立の建国（SecessionSpawnsNation>0）：Politics.ProcessCivilWar が親Worldへ記録した建国要求を四半期末に回収し、
    // 州を物理移転して新国家（新World＋新Power）を立てる。保存則＝割譲（Diplomacy.CedeTerritory）と同型の両建て記帳。
    // 決定論＝親のindex昇順・乱数種は MixSeed(_seed, 新index) の純関数・乱数不使用。
    private void SpawnSecededNations()
    {
        int count0 = Nations.Count;   // 走査前の国数を固定（追記しても再走査しない）
        for (int i = 0; i < count0; i++)
        {
            var pw = Nations[i].World;
            // ★内戦一元化（CivilWarAsWar・2026-08-30）：蜂起＝開戦時の建国。SpawnNation（同じ建国路）＋内戦タグ付き開戦。
            if (pw.PendingRebelRegions is { Count: > 0 } rebelRegs)
            {
                var rebelPolity = pw.PendingRebelPolity;
                pw.PendingRebelRegions = null;
                if (_coef.CivilWarAsWar > 0 && pw.SelfPower is { } parentPw)
                {
                    var aliveR = new System.Collections.Generic.List<Region>();
                    foreach (var r in rebelRegs)
                        if (r.State == RegionState.Active && r.Id != 0 && pw.Regions.Contains(r)) aliveR.Add(r);
                    if (aliveR.Count > 0 && aliveR.Count < pw.Regions.Count)
                    {
                        int beforeN = Nations.Count;
                        SpawnNation(Nations[i], aliveR, rebelPolity, civilWar: true);
                        if (Nations.Count > beforeN && Nations[^1].World.SelfPower is { } rebelPw)
                            Diplomacy.DeclareCivilWar(parentPw, rebelPw, rebelPolity, _coef);
                    }
                }
                continue;   // 同じ親から同四半期に2国は作らない（既存の分裂・分離と同じ規律）
            }
            // ★内戦による国家分裂（点火2026-08-01）：離脱側の州集合をまとめて1つの新国家にする。
            // ⚠分離独立（単一州）と**同じ建国路**を通る＝保存則も命名も継承も1本で済む。
            if (pw.PendingSplitRegions is { Count: > 0 } split)
            {
                var polity = pw.PendingSplitPolity;
                pw.PendingSplitRegions = null; pw.PendingSplitPolity = null;
                var alive = new System.Collections.Generic.List<Region>();
                foreach (var r in split)   // 二重防御：同四半期の割譲等で不在／首都へ昇格していたら除く
                    if (r.State == RegionState.Active && r.Id != 0 && pw.Regions.Contains(r)) alive.Add(r);
                if (alive.Count > 0 && alive.Count < pw.Regions.Count) SpawnNation(Nations[i], alive, polity);
                // ⚠コメント是正2026-09-08：旧コメントは「分離独立の要求は次の四半期へ持ち越さず捨てる」と書いていたが**嘘**だった。
                //   この continue が捨てるのは今期の**処理順**だけで、他レーンの予約（PendingSecessionWars／PendingSecessionRegion）は
                //   null 化されないまま残り、翌期の同じ走査で処理される＝持ち越される。null にしているのは自レーン（PendingSplitRegions）のみ。
                continue;   // 同じ親から同四半期に2国は作らない（内戦の決着が優先・他レーンの予約は翌期へ持ち越し）
            }
            // ★同時独立（SecessionAsWar・2026-09-06）：分離独立の戦争へ入る州を**州ごとに**建国し分離戦を開く（同じ親から同四半期に複数可＝「バラバラに分裂してもよい」）。
            //   首都（Id0）は出ない・親に州が1つしか残らないなら出ない（首都不変条件）。決着は Diplomacy.ResolvePeace の内戦分岐（CivilWarSecession）。
            if (pw.PendingSecessionWars is { Count: > 0 } secWars)
            {
                pw.PendingSecessionWars = null;
                if (pw.SelfPower is { } parentSec)
                    foreach (var r in secWars)
                    {
                        if (r.State != RegionState.Active || r.Id == 0 || !pw.Regions.Contains(r) || pw.Regions.Count <= 1) continue;
                        int beforeN = Nations.Count;
                        SpawnNation(Nations[i], [r], null, civilWar: true);
                        if (Nations.Count > beforeN && Nations[^1].World.SelfPower is { } secPw)
                            Diplomacy.DeclareCivilWar(parentSec, secPw, pw.Polity, _coef, secession: true);
                    }
                continue;
            }
            if (pw.PendingSecessionRegion is not { } reg) continue;
            pw.PendingSecessionRegion = null;
            // 二重防御：同四半期の割譲等で不在／内戦中の遷都で首都（Id0）へ昇格していたら建国不成立（首都不変条件＝Regions[0]の全読者を守る）
            if (reg.State != RegionState.Active || reg.Id == 0 || !pw.Regions.Contains(reg)) continue;
            SpawnNation(Nations[i], reg);
        }
    }

    private void SpawnNation(Simulation parent, Region reg) => SpawnNation(parent, [reg], null);

    /// <summary>★多州の建国（2026-08-01・内戦による国家分裂）。単一州の呼び口は上のラッパ＝**要素1なら旧経路と完全に同じ**。
    /// regs[0] が新首都（World.Region=Regions[0] 規約）。polity=null で親の政体を継ぐ（分離独立）／指定で離脱側の政体を持って出る（内戦分裂）。</summary>
    private void SpawnNation(Simulation parent, System.Collections.Generic.IReadOnlyList<Region> regs, Polity? polity, bool civilWar = false)
    {
        var reg = regs[0];
        var pw = parent.World;
        var parentPower = pw.SelfPower!;   // ProcessCivilWar 側で非nullゲート済み

        // --- 0) 段5＝守備兵の寝返りは**割合**（作者裁定3・2026-09-08）---
        // ⚠旧＝離脱する州の兵士POPは州ごと新国家へ移る＝**常に全員が寝返る**（「政府が軍を丸ごと敵に渡す」が全建国で起きる）。
        //   C案の「守備兵は寝返らない」も常に寝返らない側の絶対規則＝どちらも非歴史的最適解の型。史実の寝返りは最大の機序
        //   （セポイ1857・ペトログラード守備隊1917・キール軍港1918）だが常時ではない＝**兵士POPの闘争度が寝返り率を決める**（二値にしない）。
        // ⚠寝返らなかった分は親の残る州の兵士POPへ移す＝親に留まる（「離脱州の守備隊のうち忠誠を守った部隊は本国軍へ復帰した」）。
        // ⚠**人口台帳の順序に手を入れない**＝下の 1) S/P 確定より前・親の内側だけで動かす。親の総人口も総貨幣も不変で、
        //   確定される移転量 P が「寝返った分だけ」になる（S/P 確定と PopulationDelta -= P の間には1命令も入れない＝Invariants の帯）。
        // ⚠移す動詞は既存の Military.MoveCohort（規模・貯蓄・債務・結晶資産・年齢/男女コホートを既存の作法で運ぶ）＝新しい経路を作らない。
        // ⚠SoldierDefectionByMilitancy=0（既定）では1命令も走らない＝厳密恒等。
        if (_coef.SoldierDefectionByMilitancy > 0)
        {
            Pop? loyalPop = null;
            foreach (var rr in pw.Regions)   // 親が保持する最初の活動州の兵士POP（首都 Id0 は建国路の全経路で必ず親に残る）＝決定論
            {
                if (rr.State != RegionState.Active) continue;
                bool leaving = false;
                foreach (var q in regs) if (ReferenceEquals(q, rr)) { leaving = true; break; }
                if (leaving) continue;
                loyalPop = rr.Pops.Find(x => x.IsSoldier);
                if (loyalPop is not null) break;
            }
            if (loyalPop is not null)
                foreach (var rr in regs)
                {
                    var garrison = rr.Pops.Find(x => x.IsSoldier);
                    if (garrison is null || !(garrison.Size > 1e-9)) continue;
                    double defect = Math.Clamp(_coef.SoldierDefectionByMilitancy * garrison.Militancy, 0, 1);
                    if (!(defect < 1.0)) continue;   // 否定形＝NaN も旧（全員寝返る）側へ倒れる（Math.Clamp は NaN を除去しない）
                    Military.MoveCohort(garrison, loyalPop, garrison.Size * (1.0 - defect), _coef);
                }
        }

        // --- 1) 保存量の確定（割譲と同じ「同一値を両辺に使う」作法）---
        double S = 0, P = 0;
        foreach (var rr in regs) { S += rr.SumSavings(); P += rr.SumPopulation(); }
        double parentPop = pw.TotalPopulation;   // 移転前（reg込み）
        double T = Math.Max(0, pw.Treasury) * (parentPop > 1e-9 ? P / parentPop : 0);   // 国庫の人口比分与（新国家の運転資金）

        // --- 2) 親からの物理除去＋台帳（割譲のloser側と同型。国庫分与も同じ対外移転台帳）＋金準備の対称移転 ---
        foreach (var rr in regs) pw.Regions.Remove(rr);
        pw.Pay(TreasuryCat.OtherOut, -T);
        pw.CumulativeTradeFlow -= S + T;
        pw.PopulationDelta -= P;
        // （Bank.Gold の親⇄新国家の移し替え2行は 2026-09-28 に削除＝Bank.Gold は導出量で、翌日の Credit.Step が BaseMoney から上書きするまで読み手が無い）

        // --- 3) 州を新国家の首都に整形＋付け直し（回1-1＝割譲・建国・再併合の1本 Territory.Rehome の建国）---
        //   民族＝旧少数派がこの国の中核になる（CoreEthnicShare の意味は「所属国の中核民族に属する割合」＝反転が要る・敵対検証2026-07-12）。
        //   併合の荷物0（案Y・2026-08-21：分離建国の創設州は新国家の「生まれの人口」）・市場の台帳のリセット・植民地財の空化。
        //   ⚠格と支配度は独立が確定するまで動かさない（RegisterIndependence／内戦の白紙講和で本土へ）＝親が勝てば元の格のまま戻る。
        for (int k = 0; k < regs.Count; k++)
        {
            var rr = regs[k];
            rr.Id = k; rr.Seceded = false;   // 新世界の州Id＝0が首都（World.Region=Regions[0] 規約）
            Territory.Rehome(rr, Territory.Rehoming.Seceded);
        }

        // --- 4) 名前・設定（親の現行法・税を継承した NationConfig 経由＝ToScenario と同形の手組み）---
        string name = SecessionNames.Resolve(reg.Name, polity ?? pw.Polity, Diplomacy.Powers);
        var cc = Climate.CityCoord(reg.Name);
        if (cc.Lat == 0 && cc.Lon == 0) { cc = (pw.Latitude, pw.Longitude); }   // 座標未登録州は親首都へフォールバック
        var cfg = new NationConfig
        {
            Name = name, Polity = polity ?? pw.Polity, Education = pw.EducationLaw,
            Coercion = pw.BaseCoercion, Treasury = T, Latitude = cc.Lat, Longitude = cc.Lon,
            InitialTariff = pw.Tariff,
            InitialSuffrage = pw.Suffrage, InitialCensorship = pw.Censorship,
            InitialCornLaws = pw.CornLaws, InitialSerfEmancipation = pw.SerfEmancipation, InitialMonetaryLaw = pw.MonetaryLaw,
        };
        var scenario = new Scenario
        {
            SharedWorld = true,   // ★建国された国も共有世界＝産地ゲートが効く（2026-09-06）
            Seed = MixSeed(_seed, (ulong)Nations.Count), StartYear = pw.Year, Kind = ScenarioKind.Nation,
            NationConfig = cfg, PrebuiltRegions = [.. regs], Coef = pw.Coef,   // 係数は親インスタンスを共有（テスト/実験の書き込みが分裂しない）
            PoliticsEnabled = true, InitialPolity = polity ?? pw.Polity, FiscalEnabled = true, CreditEnabled = true,
            TaxRate = pw.TaxRate, InitialPollTax = pw.PollTax, InitialLandTax = Math.Max(pw.LandTax, pw.Coef.InitialLandTaxScale),   // 1783年の地租台帳（全53カ国一律・既定0=恒等）
            InitialTreasury = T, BaseCoercion = pw.BaseCoercion, EducationLaw = pw.EducationLaw,
            EducationSpending = pw.EducationSpending, ExternalPressure = pw.ExternalPressure,
            InitialInterestRate = pw.InterestRate, InitialTariff = pw.Tariff,
            InitialSuffrage = pw.Suffrage, InitialCensorship = pw.Censorship,
            InitialCornLaws = pw.CornLaws, InitialSerfEmancipation = pw.SerfEmancipation,
            InitialImmigration = pw.ImmigrationRestriction,   // 移民制限法の継承（E7・独立は制度を巻き戻さない＝他の法と同じ規則）
            InitialMonetaryLaw = pw.MonetaryLaw,              // 通貨制度の継承（F6・金の親から独立した国は金で始まる＝同じ規則）
        };
        // 新国家は常に効用AI（プレイヤー国から独立してもUI操作者はいない＝NullAgentのゾンビ国家を作らない）
        var sim = new Simulation(scenario, new UtilityAgent()) { TelemetryEnabled = parent.TelemetryEnabled };
        // ★国債の保有台帳（2026-09-21）：出ていく州のPOPが持つ親の国債は**親の対外債へ振替**＝新国家の対外債権になる
        //   （フランクフルト条約1871と同じ扱い＝債務者は親のまま）。新国家の構築（上）は台帳の立っているPOPに起票しない＝ここで0へ落ちる。
        //   ⚠構築の後に置く理由＝持ち手の国（sim.World）が要る。この間に不変条件の検査は走らない（検査は月次の Step の末尾だけ）。
        foreach (var rr in regs) foreach (var p in rr.Pops) Fiscal.ExportBondHoldings(p, p.BondHoldings, pw, sim.World);

        // --- 5) Scenario に載らない現在値の継承（技術・法・行政の到達＝独立は文明を巻き戻さない。正統性は既定＝新政権の蜜月）---
        var nw = sim.World;
        // 保存則の注（#107・2026-07-19）：子の CumulativeTradeFlow は**0のまま**が正しい。ランタイムの per-nation 不変量プローブは
        // 新国家を birth BaseMoney(=S+T) で再基線化するため、子は baseline=S+T・TradeFlow=0 で保存が閉じる（親は −(S+T) を BaseMoney/TradeFlow 両方に記帳）。
        // ここに +S+T を足すと二重計上でプローブが即例外（検証済み）。MultiNationTestの世界合計式がspawnの子baselineを勘定しない側の非対応でありコードは正しい。
        nw.TechLevel = pw.TechLevel; nw.Elite = pw.Elite;
        nw.PatentLaw = pw.PatentLaw; nw.JointStock = pw.JointStock; nw.Railways = pw.Railways;
        nw.LaborUnion = pw.LaborUnion; nw.Pension = pw.Pension; nw.LandReform = pw.LandReform;
        nw.ConscriptionLaw = pw.ConscriptionLaw; nw.Policy.ConscriptionLaw = pw.ConscriptionLaw;   // 徴兵法の継承（E5・独立は制度を巻き戻さない）
        nw.SecretBallot = pw.SecretBallot; nw.Policy.SecretBallot = pw.SecretBallot;   // 秘密投票法の継承（E5）
        nw.Bureaucracy = pw.Bureaucracy; nw.Policy.Bureaucracy = pw.Bureaucracy;   // 官僚制度の継承（E5）
        nw.SocialSecurityLaw = pw.SocialSecurityLaw; nw.Policy.SocialSecurityLaw = pw.SocialSecurityLaw;   // 社会保障法の継承（E5）
        nw.StateReligion = pw.StateReligion; nw.Policy.StateReligion = pw.StateReligion;   // 国教法の継承（E6）
        nw.Antitrust = pw.Antitrust; nw.Policy.Antitrust = pw.Antitrust;   // 独占禁止法の継承（E6）
        nw.Guild = pw.Guild; nw.Policy.Guild = pw.Guild;   // ★同業組合法の継承（特権 段4）
        nw.WartimeRationing = pw.WartimeRationing; nw.Policy.WartimeRationing = pw.WartimeRationing;   // 戦時配給法の継承（E6）
        nw.CapitalControlLaw = pw.CapitalControlLaw; nw.Policy.CapitalControlTarget = pw.CapitalControlLaw;   // 資本移動の法の継承（トリレンマdoc・2026-08-13）
        nw.ChildLaborAllowed = pw.ChildLaborAllowed; nw.Policy.ChildLaborAllowed = pw.ChildLaborAllowed;   // 児童労働法の継承（02断線走査2026-07-28・独立は制度を巻き戻さない）
        nw.MilitaryExportBan = pw.MilitaryExportBan; nw.Policy.MilitaryExportBan = pw.MilitaryExportBan;   // 軍需輸出の法の継承（禁制品doc C5・2026-07-30）
        nw.CivilianArmsBan = pw.CivilianArmsBan; nw.Policy.CivilianArmsBan = pw.CivilianArmsBan;           // ★民間の武器購入の法の継承（2026-09-11・独立は制度を巻き戻さない）
        nw.OpiumPossession = pw.OpiumPossession; nw.Policy.OpiumPossession = pw.OpiumPossession;           // 阿片の保有法の継承（禁制品doc C3・2026-07-30）
        nw.OpiumExportBan = pw.OpiumExportBan; nw.Policy.OpiumExportBan = pw.OpiumExportBan;               // 阿片の輸出法の継承（同）
        nw.NationalizeRailways = pw.NationalizeRailways; nw.Policy.NationalizeRailways = pw.NationalizeRailways;   // 国有化（5部門＋『全て』）の継承（E6）
        nw.NationalizeMines = pw.NationalizeMines; nw.Policy.NationalizeMines = pw.NationalizeMines;
        nw.NationalizeMilitary = pw.NationalizeMilitary; nw.Policy.NationalizeMilitary = pw.NationalizeMilitary;
        nw.NationalizeOil = pw.NationalizeOil; nw.Policy.NationalizeOil = pw.NationalizeOil;
        nw.NationalizeElectricity = pw.NationalizeElectricity; nw.Policy.NationalizeElectricity = pw.NationalizeElectricity;
        nw.NationalizeAll = pw.NationalizeAll; nw.Policy.NationalizeAll = pw.NationalizeAll;   // ★『全て』（§11-6d統合）。新国がソ連型でなければ Government が制定目標を現状へ丸める（撤回＝民営化は可）
        nw.WorkHours = pw.WorkHours; nw.WorkHoursValue = pw.WorkHoursValue;
        // ★女性の権利の継承（統合法・2026-08-10）：旧は財産権/参政権を World にだけ写し **Policy を写していなかった**（教育権だけが両方写す非対称）
        //   ＝分離独立の翌期に Government が目標なし側へ引き戻そうとして幽霊戦線になり得た。統合ついでに他法と同じ流儀（World＋Policy）へ揃える。
        nw.WomenRights = pw.WomenRights; nw.Policy.WomenRights = pw.WomenRights;
        nw.LandTaxUnlocked = pw.LandTaxUnlocked;
        nw.AdminPowerSmoothed = pw.AdminPowerSmoothed;
        // #126 制度疲労の継承（2026-07-25・ユーザー裁定「清の近代化が簡単すぎる」）：独立は文明を巻き戻さない（技術・法の継承）が、**制度の病も引き継ぐ**。
        // 旧＝新国家は decay=0 で生まれ、親が清（decay≈0.8）でも技術採用×0.6・改革抵抗×1.4・軍質ペナルティが全て消滅＝
        // 「分裂で身軽に近代化」（上海王国4勝/昆明系4勝の機構的な根）。軍閥期の中国は清の制度的な病（禄位・割拠）を引き継いだ＝継承が史実。
        nw.InstitutionalDecay = pw.InstitutionalDecay;
        nw.InstitutionalDecayCeiling = pw.InstitutionalDecayCeiling;
        nw.BornOfSecession = true;   // P9：独立戦争の危機乗数（レヴェ・アン・マス）の判定用（分離で生まれた国＋交戦中）
        nw.SetMilitarySpending(pw.MilitarySpending, pw.NavalSpendShare);   // 陸海の維持費ごと継承（2026-08-04）
        nw.Policy.LaborUnion = pw.LaborUnion; nw.Policy.Pension = pw.Pension; nw.Policy.LandReform = pw.LandReform; nw.Policy.WorkHours = pw.WorkHours;   // 目標=現在値（Government が翌期に引き戻さない）
        nw.Policy.MilitarySpendingTarget = pw.Policy.MilitarySpendingTarget;
        // 社会保障の目標=継承した法段（レビュー2026-07-20）：SocialSecurityLawGate>0 では Government が Policy.SocialSecurityTarget から法段を毎期導出するため、
        // 目標を継承しないと既定0（救貧法）へ毎期引き戻され「独立は制度を巻き戻さない」に反して福祉が浸食される。継承法段（救貧0/労災0.33/失業0.67/年金1.0）を目標に写す。
        nw.Policy.SocialSecurityTarget = (int)pw.SocialSecurityLaw / SocialSecurityLawMaxLevel;
        nw.Quarter = pw.Quarter;

        // --- 6) Power 生成（コンストラクタと同型）＋共有外交への接続 ---
        var power = new Power
        {
            Id = _nextPowerId++, Name = name, BaseName = name, Nation = nw,   // ★Powers.Count でなく単調カウンタ（wave2b是正）＝ReabsorbNations の RemovePower で欠番が出ても既存Idと衝突しない
            Military = parentPower.Military * (parentPop > 1e-9 ? P / parentPop : 0),   // 初期表示値のみ（次の Diplomacy.Step が兵士POPから再導出＝実体は移転した兵士）
            Gdp = nw.Gdp, TradeWeight = DefaultTradeWeightBase,
            Culture = reg.Culture, Religion = reg.Religion,   // 次の毎月同期まで州ラベルで代用
        };
        power.SpawnedDuringPlay = true;   // ★プレイ中に生まれた国＝ハーネスの「満足度・最小国」の対象外（Validation.WorstSat）
        nw.Diplomacy = Diplomacy; nw.SelfPower = power;
        // ⚠★診断台帳は親から受け継ぐ（2026-08-02）。無いと、途中で生まれた国だけ Explain が null になり、
        //   台帳を読む側が NullReferenceException で落ちる（実測＝ExplainTests が分離独立の起きるシードで死んだ）。
        //   ★診断が入っているかどうかは「世界の設定」であって国の属性ではない＝親に合わせるのが正しい。
        if (pw.Explain is not null)
        {
            nw.Explain = new ExplainLedger();
            // ⚠★建国した瞬間の値を1項として記帳する。台帳は「前期値＋各フロー＝最終値の厳密分解」を約束しているので、
            //   起点を入れないと**生まれた四半期だけ内訳が空**になり、合計が最終値と一致しない（実測でテストが赤）。
            nw.Explain.Add("Legitimacy", "建国時の正統性", nw.Legitimacy);
        }
        nw.StepsDiplomacy = false; nw.CoercionFollowsForce = true;
        Diplomacy.Powers.Add(power);
        Nations.Add(sim);
        if (civilWar)
        {
            // ★内戦一元化：反乱国家＝講和相当の状態を張らない（呼び出し側が直ちに DeclareCivilWar する）。ログも開戦側が出す。
            return;
        }
        Diplomacy.RegisterIndependence(parentPower, power, _coef);   // 残留緊張（従属なし＝独立国）
        Diplomacy.Log.Add(regs.Count > 1
            ? $"{pw.Year} {parentPower.Name}が内戦で分裂し{name}が成立（{regs.Count}州）"
            : $"{pw.Year} {name}が{parentPower.Name}から分離し建国（{reg.Name}）");   // 文言規約（Diplomacy:664）：「独立」は植民地独立の集計語＝分離建国では使わない（6-8で4国世界にも分離が出て集計衝突が顕在化・2026-07-28）
    }

    public void Run(int quarters) { for (int q = 0; q < quarters; q++) StepQuarter(); }

    /// <summary>★開始前の助走の**唯一の入口**（作者裁定2026-08-10）。App（新規ゲーム）も自己対戦バッチ（<see cref="SelfPlayMatch"/>）も
    /// このメソッドだけを通る＝「遊ぶ世界」と「測る世界」の初期条件が同じになる。
    /// <para>⚠2026-08-10以前は App だけが <c>WarmupEconomy(4)＋WarmupTech(8)</c> を回し、selfplay/fidelity は素の盤面から走っていた
    /// ＝**プレイヤーが見る開始価格と、帯を測っている開始価格が別物**だった。長さは <see cref="Coefficients.StartWarmupQuarters"/> の1本で決める。</para>
    /// <para>中身＝①価格の助走（暦・政治・外交・人口・識字・技術・移民・資本形成を止めて価格と在庫だけ実勢へ寄せる）②技術の助走
    /// （識字差を技術差へ実体化）③保存則の基準線の取り直し。<b>暦は1四半期も進まず、生産能力も1783年のまま</b>
    /// （<see cref="World.WarmupFrozen"/>）＝助走を何年に延ばしても「開始盤面が先へ進む」ことがない。</para>
    /// <para><paramref name="onQuarter"/>＝助走の各四半期末に呼ばれる**観測だけ**の手（UIの価格グラフの標本）。力学には触れない。
    /// <paramref name="quartersOverride"/>＝**診断専用**（Runner の warmupprobe が 4/20/40 を並べて測る）。本番は null＝係数一本。</para>
    /// <para>★<see cref="Coefficients.StartWarmupQuarters"/>=0 で**助走を丸ごとしない**（技術の助走も基準線の取り直しもしない）＝
    /// 2026-08-10以前のバッチと厳密恒等の逃げ道。帯が動いたときの二分探索の基線に使う。</para></summary>
    public void StartWarmup(Action? onQuarter = null, int? quartersOverride = null)
    {
        if (_startWarmupDone) return;   // 二重呼び出しの防止（ロード経路・診断からの再呼び出しで盤面が二度助走しない）
        _startWarmupDone = true;
        int quarters = quartersOverride ?? _coef.StartWarmupQuarters;
        if (quarters <= 0) return;   // 助走なし＝構築直後の盤面そのままで開始（厳密恒等の底）
        WarmupEconomy(quarters, onQuarter);
        WarmupTech(_coef.StartWarmupTechIterations);
        RebaselineInvariants();
    }
    private bool _startWarmupDone;

    /// <summary>**価格の助走**：暦・政治・外交・人口・識字・技術・移民・**資本形成**を凍結したまま指定四半期を回す＝
    /// 価格・在庫・クリアリング・賃金/地代/税という初期過渡だけを実勢へ落ち着かせる。開始スナップショットを
    /// 「1783年の生産能力のまま、価格だけが均衡に居る盤面」にするため。
    /// <para>★資本形成の凍結は作者裁定2026-08-10 追加分（「価格のみ試走することはできない？　問題は開始時の振動なので」）。
    /// 詳細と理由は <see cref="World.WarmupFrozen"/> の説明を見よ。</para>
    /// <para>★2026-08-10（作者裁定）：暦を止めた（<see cref="World.WarmupFrozen"/>）。旧は <see cref="StepQuarter"/> をそのまま回していたので
    /// 4四半期の助走で**エンジンの年が1783→1784へ進み**、表示暦（1783起点）と1年ずれていた（監査2026-07-11 他9で既知の未修正バグ）。
    /// 5〜10年へ延ばす裁定と両立しないため、暦・人口・識字・技術・膨張意欲を止める方式へ改めた。</para>
    /// <para>外交（戦争・緊張・植民地）もこの間だけ凍結する＝呼び出し側が <see cref="DiplomacyFrozen"/> を立て忘れても
    /// 「経済だけの助走」という名前どおりに動く（旧は App だけが外から立てており、Runner の各プローブでは外交が走っていた）。</para>
    /// <para><paramref name="onQuarter"/>＝各四半期の終わりに呼ばれる**観測だけ**の手（既定 null＝一切呼ばれない）。
    /// UI が助走中の価格を標本に採るために使う（生産タブの価格グラフ・2026-08-10）。力学には触れない。</para></summary>
    public void WarmupEconomy(int quarters, Action? onQuarter = null)
    {
        if (quarters <= 0) return;   // 助走なし＝素の構築直後の盤面と厳密恒等（フラグの上げ下げすらしない）
        var prevPolitics = Nations.Select(n => n.World.PoliticsEnabled).ToArray();
        bool prevDiplomacy = DiplomacyFrozen;
        foreach (var n in Nations) { n.World.PoliticsEnabled = false; n.World.WarmupFrozen = true; }
        DiplomacyFrozen = true;
        _calendarFrozen = true;
        for (int q = 0; q < quarters; q++) { StepQuarter(); onQuarter?.Invoke(); }
        _calendarFrozen = false;
        DiplomacyFrozen = prevDiplomacy;
        // 助走中に生まれた国は無い（外交凍結＝戦争も分離独立も起きない）が、Nations が伸びても添字が壊れないよう長さで守る
        for (int i = 0; i < Nations.Count; i++)
        {
            Nations[i].World.WarmupFrozen = false;
            if (i < prevPolitics.Length) Nations[i].World.PoliticsEnabled = prevPolitics[i];
        }
    }

    /// <summary>技術の助走（2026-07-13）：暦を一切進めず（TechOnset等の年ゲートを乱さない）、各国の技術水準だけを
    /// 教育制度の均衡へ寄せる＝開始盤面で欧州とアジア（あるいは欧州内）の技術が「団子」にならないよう、
    /// 識字×世俗水路（Elite）の差を技術差へ実体化する。GrowTech は StepQuarter と同一の後追い/革新/フロンティア追随機構
    /// （＝較正済みの技術ダイナミクスそのもの）を回すだけで、係数の外科補正は一切足さない＝差は制度から創発する。
    /// ★2026-08-10：selfplay/fidelity も <see cref="StartWarmup"/> 経由でこの助走を通るようになった（旧コメントの
    /// 「selfplay/fidelity はこの助走を通らない」「1789→1914を厳密500四半期」は二重に陳腐化していた＝起点は1783・全長 GameQuarters）。
    /// 暦を動かさないので1914断面（Q524）も全長も不変。</summary>
    public void WarmupTech(int iterations)
    {
        for (int it = 0; it < iterations; it++)
        {
            foreach (var n in Nations) TechEducation.GrowTech(n.World, n.World.Coef);
            double frontier = 0;
            foreach (var n in Nations) frontier = Math.Max(frontier, n.World.TechLevel);
            foreach (var n in Nations) n.World.WorldTechLevel = frontier;   // StepDay と同一：後追いが立ち上がる先端を毎反復同期
            { double fe = 0; foreach (var n in Nations) if (n.World.Expertise > fe) fe = n.World.Expertise; foreach (var n in Nations) n.World.WorldFrontierExpertise = fe; }   // ★専門人材率の先端も配る（知識doc 段3）
            { double fd = 0; foreach (var n in Nations) if (n.World.PrivilegeDecayRaw > fd) fd = n.World.PrivilegeDecayRaw; foreach (var n in Nations) n.World.WorldMaxPrivilegeDecayRaw = fd; }   // ★特権段6：制度疲労の導出量の世界最大（正規化の分母）
        }
    }

    /// <summary>全国の保存則の基準線を現在値で取り直す（開始盤面で統治状態＝国庫・与信を初期へ戻した直後に呼ぶ）。</summary>
    public void RebaselineInvariants() { foreach (var n in Nations) n.RebaselineInvariants(); }

    /// <summary>true の間は共有外交（戦争・緊張・植民地）を進めない。開始盤面の経済助走中に立て、1789の外交状態を保つ。</summary>
    public bool DiplomacyFrozen { get; set; }

    private int _dayInQuarter;

    /// <summary>四半期＝90回の日次刻み。共有層（外交・国際市場・移民・世界の技術水準）は四半期末に従来と同一順で発火する
    /// ＝テスト・self-play は StepQuarter を呼べば従来の合成順を踏む（各国の RNG ストリームは独立なので日次インターリーブでも結果は不変）。</summary>
    public void StepQuarter() { for (int d = 0; d < Calendar.DaysPerQuarter; d++) StepDay(); }

    // 保存量プローブ（leakhunt・恒久計測器）：共有層のどのサブステップで D＝基礎貨幣−累積台帳 が動いたかを特定する。
    // 既定OFF（Simulation.ConservationProbe）＝実行ゼロ。詳細は Simulation.ProbeCheck の説明を参照。
    // ★世界の合計（fis-21・2026-09-26）：国ごとの D は**片側記帳**（払った側だけが記帳し、受け取る国が無い／受け取った側だけが記帳する）を拾えない。
    //   配列の最後の1枠に世界の W＝Σ現存国（基礎貨幣−鋳造−不換発行）を持ち、共有層の段ごとに ΔW を見る（国ごとの D が閉じていれば ΔW＝ΣΔ累積貿易＝世界の外との出入り）。
    //   新しい状態は作らない（既存の配列を1枠伸ばすだけ）。★回1-2（2026-09-27）で世界プール（ゲーム外から湧く持参金＝旧の既知の湧き口）は消えた。
    private double[]? _probeD;
    private static double ProbeD(World w) => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
    private double ProbeWorldW() { double s = 0; foreach (var n in Nations) { var w = n.World; s += w.BaseMoney - w.CumulativeGoldMinted - w.CumulativeFiatIssued; } return s; }
    private void ProbeSnap()
    {
        if (!Simulation.ConservationProbe) return;
        if (_probeD is null || _probeD.Length != Nations.Count + 1) _probeD = new double[Nations.Count + 1];
        for (int i = 0; i < Nations.Count; i++) _probeD[i] = ProbeD(Nations[i].World);
        _probeD[Nations.Count] = ProbeWorldW();
    }
    private void ProbeDiff(string label)
    {
        if (!Simulation.ConservationProbe || _probeD is null || _probeD.Length != Nations.Count + 1) return;
        for (int i = 0; i < Nations.Count; i++)
        {
            var w = Nations[i].World;
            double now = ProbeD(w);
            if (Math.Abs(now - _probeD[i]) > 1e-4)
                Console.Error.WriteLine($"[PROBE共有] {label} {w.Region.Name} {w.Year}年 ΔD={now - _probeD[i]:F6}");
            _probeD[i] = now;
        }
        double wNow = ProbeWorldW(), wPrev = _probeD[Nations.Count];
        if (Math.Abs(wNow - wPrev) > 1e-7 * Math.Max(1.0, Math.Abs(wNow)))
            Console.Error.WriteLine($"[PROBE世界] {label} {Year}年 ΔW={wNow - wPrev:F3}（月次の段の片側と対なら四半期で閉じる＝判定は leakhunt の[世界の台帳]）");
        _probeD[Nations.Count] = wNow;
    }

    /// <summary>1日進める（UIの日次描画用）。各国を1日進め、四半期末に共有層をまとめて発火。四半期が締まった日に true を返す。</summary>
    // ★所要時間の計器（2026-08-26・作者報告「シミュレートに時間がかかりすぎ」）。環境変数 POB_TIME=1 のときだけ計る。
    //   ⚠既定は Stopwatch を1度も触らない＝実行ゼロ・ビット恒等（分岐は静的 readonly bool の判定1回のみ）。
    internal static readonly bool TimeOn = System.Environment.GetEnvironmentVariable("POB_TIME") == "1";
    /// <summary>各国の日次（純粋日次相のみ）を並列に回すか。★**既定 true**（2026-08-29 高速化裁定＝StepDay 3相分割で決定論を確保して点火）。
    /// POB_PAR=0 で逐次へ倒す（レース疑いの切り分け・非常口）。
    /// ★履歴：2026-08-26 の全相並列は armstrace のビット一致に失敗（1788年分岐）＝月次決算が共有（Diplomacy.Log 等）へ触るため。
    ///   2026-08-29 に純粋日次（価格形成・建設・信用＝実測74%）だけを並列化し、境界相（四半期首・月次決算・四半期末）を逐次に残す形で
    ///   **ビット一致を実証**＝armstrace seed1 で（旧一体型≡新逐次）と（新逐次≡新並列）・selfplay seed2 全出力一致・savetest 0.0E+000。
    /// ⚠この保証は Simulation.StepDayDaily の国ローカル不変条件（同所コメント）に立つ＝共有へ触る処理は StepDayClose 側へ。</summary>
    internal static readonly bool ParallelDays = System.Environment.GetEnvironmentVariable("POB_PAR") != "0";
    public static long TicksNations, TicksMarket;   // POB_TIME=1 の計器。★2026-09-26（sweep-25）：selfplay-batch は1プロセスで試合を並列に回す＝素の += は数が欠ける＝Interlocked で足す

    public bool StepDay()
    {
        // ★各国の日次を3相の段階実行で回す（2026-08-29 高速化裁定＝StepDay分割）。
        //   相1 Open（四半期首）＝逐次　→　相2 Daily（価格形成・建設・信用＝実測74%）＝**並列**　→　相3 Close（月次決算・四半期末）＝逐次。
        //   2026-08-26 の全相並列はビット一致に失敗（1788年分岐）＝月次決算が共有（Diplomacy.Log 等）へ触るため。
        //   純粋日次は国ローカル（自国 World/Region のみ・乱数なし・共有 static は読み取り専用か ConcurrentDictionary）＝
        //   実行順に依存しない ⇒ 並列/逐次でビット一致する（番人＝armstrace diff。Simulation.StepDayDaily の不変条件コメント参照）。
        //   ⚠診断の静的カウンタ（TradeReachDiag/ConservationProbe）ONのときは逐次へ倒す（診断は再現性が命）。
        //   ⚠逐次モードも同じ3相順で回す＝並列との差分が「レースの有無」だけになる（単国 Simulation.StepDay() も同じ3相を順に呼ぶ＝恒等）。
        //   POB_PAR=0 で並列を切る（レース疑いの切り分け用）。
        long t0 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        bool par = ParallelDays && Nations.Count > 1 && !Simulation.ConservationProbe && !TradeReachDiag.On && !BudgetBindDiag.On;   // ★2026-09-26（sweep-25）：BudgetBindDiag も逐次へ（ByWorld は通常の Dictionary＝並列の日次から書くと壊れる）
        foreach (var sim in Nations) sim.StepDayOpen();
        if (par)
            System.Threading.Tasks.Parallel.For(0, Nations.Count, i => Nations[i].StepDayDaily());
        else
            foreach (var sim in Nations) sim.StepDayDaily();    // 各国の鼓動（価格形成＝旬の最終日だけ・建設・信用）
        // 締め境界＝**常に逐次**。★月次決算の並列化は2026-08-29に実測で差し戻し：ログを国別バッファへ退避しても
        //   armstrace が1788年（助走明け最初の断面）から発散＝決算は**他国の当日更新を恒常的に読む**（旧調査の疑いどおり。
        //   候補＝金融の相互参照：通貨ペア引力・金利・金流。ログ退避（World.PendingLog）は恒等が証明できたので恒久採用＝
        //   共有 List へのレース類を根絶）。再挑戦するなら国別状態ハッシュのフェーズ計器で読み手を特定してから。
        foreach (var sim in Nations) sim.StepDayClose();
        foreach (var sim in Nations) sim.World.FlushLog();      // 国順フラッシュ＝共有ログの順序を旧・直接書きと一致させる（恒等は検査Dで実証）
        if (TimeOn) System.Threading.Interlocked.Add(ref TicksNations, System.Diagnostics.Stopwatch.GetTimestamp() - t0);

        bool quarterDone = _dayInQuarter == Calendar.QuarterEndDayIndex;
        // ★月次清算（2026-08-22・TradeMonthlyClearing）：量の式が月次規模（LastDemand/LastProduced/TargetStock）を読むのに清算が四半期1回＝
        //   単位混在（年間輸出≤年産出の1.67%の算術天井の半分はこれ）。四半期の第1・第2月末にも清算する＝全国 Simulation.MonthSettle の後＝
        //   scale/Stock 更新済み・Invariants 検査後＝四半期末の既存呼び出し（下）と同じ相対順序。0＝追加呼び出しゼロ＝ビット恒等。
        //   第1清算（月0）だけ四半期累計（LastPortThroughput）をリセット＝四半期末の読み手（港湾投資・保険・通行料・ArmyShape）の物差しを保つ。
        if (_coef.TradeMonthlyClearing > 0 && !quarterDone && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0)
        {
            int monthIdx = (_dayInQuarter + 1) / Calendar.DaysPerMonth - 1;   // 0 or 1（2＝四半期末は下の既存呼び出し）
            ProbeSnap();
            { long t1 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Market.Step(Nations, _coef, firstClearingOfQuarter: monthIdx == 0); if (TimeOn) System.Threading.Interlocked.Add(ref TicksMarket, System.Diagnostics.Stopwatch.GetTimestamp() - t1); }
            ProbeDiff("国際市場(月)");
        }
        if (quarterDone)
        {
            ProbeSnap();                                              // 保存量プローブ（leakhunt・既定OFF＝実行ゼロ）
            foreach (var n in Nations) NationalScore.StepStanding(n.World);   // 国力の在庫を1期ぶん寄せる（2026-08-02）＝序列は在庫で決まる。外交(ComputeRanks)より前
            if (_coef.ThreatRailTargetWeight > 0) StepSecurityThreat();   // ★安保手順3（2026-08-30点火＝既定1）：脅威の観測（=0なら1命令も走らない。threatprobe と同式）
            UpdateTradeWeights(); ProbeDiff("交易比重");                // 外1：交易比重を貿易取扱高のシェア（LastExportValue+LastImportValue の EMA）から四半期同期（TradeWeightDynamic=0 で1789固定値を維持＝恒等）
            if (!DiplomacyFrozen)
                for (int m = 0; m < Calendar.MonthsPerQuarter; m++) { Diplomacy.Step(_coef, Rng, _quarter * Calendar.MonthsPerQuarter + m); ProbeDiff($"外交m{m}"); }   // 共有外交を月次3回（戦争の月次カデンス）。凍結中＝経済助走で戦争・緊張を進めない
            StepCanals(); ProbeDiff("運河");   // 海峡・運河：扼者を解決し、運河は host 保有国の資本家が建設（§B）
            // ★助走中は巨大建造物を進めない（2026-08-10 追加裁定＝能力の凍結）。着工も進捗も完成効果（工業化度・耕地・送電容量）も
            //   「年月をかけて建つ」もの＝暦の進まない助走で建ててはいけない。1783年は技術ゲートでほぼ全件が未解禁＝需要への影響も無い。
            if (!_calendarFrozen) { Megaprojects.Step(Diplomacy, Nations, _coef); ProbeDiff("巨大建造物"); }   // 巨大建造物：保有国を解決し国庫から資材＋労働を買って建て、完成したら州へ恒久効果を貼る（2026-07-28）
            { long t2 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Market.Step(Nations, _coef, firstClearingOfQuarter: !(_coef.TradeMonthlyClearing > 0)); if (TimeOn) System.Threading.Interlocked.Add(ref TicksMarket, System.Diagnostics.Stopwatch.GetTimestamp() - t2); } ProbeDiff("国際市場");          // 国際市場：州と州の二国間清算（実経路・比較優位）。月次清算ONでは四半期の第3清算＝累計を続ける（リセットしない）
            RewirePorts();   // ★港の配線を盤面から張り直す（毎四半期・無状態＝2026-09-21 案1。旧＝署名で条件付き・2026-08-13）
            StepPorts(); ProbeDiff("港湾");                             // 港湾容量：混雑する港州を国家＋資本家が拡張（Market.Step の取扱高/律速を見て投資。改修2026-06-28）
            StepMarineInsurance(); ProbeDiff("海上保険");                // 海上保険：商人pop基金で通商破壊損失を分散（平時=保険料・封鎖時=保険金。改修2026-06-28・金融改革③）
            // 移民は助走では動かさない（2026-08-10）。移民は総量保存だが時間の流れ（暦を止めた助走で人を動かさない）。
            if (!_calendarFrozen)
            {
                StepAssimilation(); ProbeDiff("同化");                       // 民族の同化（移民の希釈への歯止め）
                // ★2026-09-03（J-004）：⚠移民は**受入国/送出国のPOP構成**を変えるので、国の国民意識（POPのsize加重平均）が動く。
                //   ⇒ 説明台帳へ1行足す。⚠Explain は既定 null＝バッチ/較正では getter を1回も余分に呼ばない（恒等・ゼロコスト）。
                double[]? ncBeforeMig = null;
                for (int ei = 0; ei < Nations.Count; ei++)
                    if (Nations[ei].World.Explain is not null)
                    { ncBeforeMig ??= new double[Nations.Count]; ncBeforeMig[ei] = Nations[ei].World.NationalConsciousness; }
                MigrateBetweenNations(); ProbeDiff("国家間移民");
                if (ncBeforeMig is not null)
                    for (int ei = 0; ei < Nations.Count && ei < ncBeforeMig.Length; ei++)
                        if (Nations[ei].World.Explain is { } exm)
                        {
                            double d = Nations[ei].World.NationalConsciousness - ncBeforeMig[ei];
                            if (d != 0) exm.Add("NationalConsciousness", "国家間移民による構成の変化", d);
                        }            // 国家間移民：貧しい国の大衆が豊かな国へ抜ける（欧州→新大陸）
                RecruitSpecialists(); ProbeDiff("技師の招聘");                // ★段④：後進国が国庫で先進国から技師・研究者を買う（大衆とは逆向き・ゼロサム）
            }
            CapitalMarket.Step(Nations, _coef); ProbeDiff("国際金融");   // 国際金融（第3b段）：民間外資が高収益国へ流れ工業化を支え、配当を送り、危機で逃避・伝染
            if (!DiplomacyFrozen) StepEpidemicTransmission();          // 疫病の国家間伝播（年次）：感染国から近接＋交易接触で未感染国へ飛び火（コレラの世界的流行）
            StepWeatherBands();                                         // 緯度帯天候（年次）：同じ気候帯の国を一緒に豊凶させる（欧州一帯の干ばつ/冷夏）。自然現象＝外交凍結中も走る
            double frontier = 0;                                       // 世界の技術水準＝各国の最高水準（後発の優位・§5.1）
            foreach (var n in Nations) frontier = Math.Max(frontier, n.World.TechLevel);
            foreach (var n in Nations) n.World.WorldTechLevel = frontier;
            { double fe = 0; foreach (var n in Nations) if (n.World.Expertise > fe) fe = n.World.Expertise; foreach (var n in Nations) n.World.WorldFrontierExpertise = fe; }   // ★専門人材率の先端も配る（知識doc 段3）
            { double fd = 0; foreach (var n in Nations) if (n.World.PrivilegeDecayRaw > fd) fd = n.World.PrivilegeDecayRaw; foreach (var n in Nations) n.World.WorldMaxPrivilegeDecayRaw = fd; }   // ★特権段6：制度疲労の導出量の世界最大（正規化の分母）
            // 接7（監査2026-07-11・2026-07-25休眠配線）：外圧（改革派の追い風＝明治の黒船）が創建時定数0.3で全国・全期間凍結だった＝
            // 「先進勢力との接触」の実体から四半期導出する。外圧＝世界の技術水準との格差（GapScaleで正規化）×海からの接触係数
            //（港あり=1/内陸=InlandFactor＝黒船は海から来る）。世界の技術水準国自身は格差0＝外圧0（追い風は後発の現象）。
            // 海軍格差の精密化（艦隊到達力の重み）は今後の検討課題。既定1.0＝点火済み（2026-07-27）。0で定数0.3据え置き。
            if (_coef.ExternalPressureDerived > 0)
                foreach (var n in Nations)
                {
                    var w = n.World;
                    bool coastal = false;
                    foreach (var r in w.Regions) if (r.State == RegionState.Active && r.IsPort) { coastal = true; break; }
                    double gap = Math.Clamp((frontier - w.TechLevel) / Math.Max(1e-9, _coef.ExternalPressureGapScale), 0, 1);
                    w.ExternalPressure = gap * (coastal ? 1.0 : _coef.ExternalPressureInlandFactor);
                }
            // 政16（世界工業化の先端状態量・スカフォールド完成）：各国 MeanIndustrialization の最大値＝「世界最先端の工業化度」を
            // 全 World.IndustrialFrontier へ注入（WorldTechLevel と同型）。Ideology の発展度ゲート（思想の解禁度）が
            // 代理(WorldTechLevel との max)でなくこれを参照する（暦年ゲートとの混合は 2026-09-26 に削除＝解禁度は発展度だけ）。
            double indFrontier = 0;
            foreach (var n in Nations) indFrontier = Math.Max(indFrontier, n.World.MeanIndustrialization);
            foreach (var n in Nations) n.World.IndustrialFrontier = indFrontier;
            // ★銀の世界価格（銀本位doc G6・2026-08-13）：銀本位国の平価は「銀の価値」に縛られる。World は国際市場を見られないので、
            //   ここで各国へ配る（IndustrialFrontier と同型の共有スカラー）。⚠新しい価格を作らない——既存の世界価格をそのまま渡すだけ。
            //   SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では World 側が読まない＝注入しても挙動中立。
            if (_coef.SilverStandardGate > 0 && Catalog.SilverId < Market.Price.Count)
            {
                double silverWorld = Market.Price[Catalog.SilverId];
                // ★器 SilverParityFromRatio（2026-09-07・休眠既定0）：Market.Price[銀] は「局所価格×ExchangeRate」の平均＝銀本位国の E がこの平均から
                //   作られ、平均は E を含む（E→平均→E の正帰還・silverprobe で天井近傍に乗数2.8）。1＝同一市場内の 銀/金鉱 比の全 Active 州平均 R を
                //   RefPrice 比で正規化して RefPrices[銀] の単位へ戻す（比は PriceLevel と ExchangeRate が約分される＝単位無し・クランプ不要）。
                //   ⚠平均は**対数平均**・両市場が生きている州だけ（レビュー是正2026-09-07）：局所価格は rp×[PriceFloorFactor,PriceCeilFactor] の帯を持つので
                //   片側が死んだ州（需要も産出も無い＝MarketFlow が価格を動かさない）の凍った比が最大10⁴＝算術平均を1州で独占する（seed1 実測＝フォートユーコンの
                //   金鉱 0.0088 が R を 1.18→1.34 へ押した・中央値は 0.99〜1.02）。対数平均なら1州の票は ln(天井/床)/N で有界＝クランプも新定数も要らない。
                //   走査順＝Nations 順×Regions 順（決定論）。有効な州が無い／比が不正なら旧統計へ倒す。0（既定）ではこの分岐に入らない＝厳密恒等。
                if (_coef.SilverParityFromRatio > 0)
                {
                    double ratioSum = 0; int ratioCnt = 0;
                    foreach (var n in Nations)
                        foreach (var r in n.World.Regions)
                        {
                            if (r.State != RegionState.Active) continue;
                            if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                            var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                            if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;   // 死んだ市場（MarketFlow と同じ条件）は除外
                            double pAg = mAg.Price, pAu = mAu.Price;
                            if (!(pAg > 1e-9) || !(pAu > 1e-9)) continue;   // 否定形＝NaN も除外
                            ratioSum += System.Math.Log(pAg / pAu); ratioCnt++;   // 対数平均
                        }
                    double refAu = Catalog.RefPrices[Catalog.GoldOreId];
                    double refRatio = refAu > 1e-9 ? Catalog.RefPrices[Catalog.SilverId] / refAu : 0;
                    double meanRatio = ratioCnt > 0 ? System.Math.Exp(ratioSum / ratioCnt) : 0;
                    if (ratioCnt == 0 || !(meanRatio > 1e-9) || !(refRatio > 1e-9)) { /* 旧統計のまま */ }
                    else silverWorld = meanRatio / refRatio * Catalog.RefPrices[Catalog.SilverId];
                }
                foreach (var n in Nations) n.World.WorldSilverPrice = silverWorld;
            }
            // ★交易網の引力（タスク#75再開・2026-08-21）：TradeGoldPull_i ＝（世界貿易の金建て比率）×（自国の貿易開放度）。
            //   貿易額＝ExportsByGood/ImportsByGood（BilateralMarket が決済ごとに書く四半期値）・開放度＝(輸出+輸入)×年換算/年GDP
            //   （MajorsTradeGdp と同一の物差し）。**新しい量を1つも作らない**——既存の帳簿の比率だけ。
            //   「交易加重の相手の本位」の実装形＝ペア別行列は持たない設計（54×136×54 の理由で BilateralMarket が全対を捨てる）ため、
            //   世界共通の金建て比率×自国の開放度で近似する：貿易の大きい国が金へ移るほど比率が跳ね（英→独の正帰還＝雪崩）、
            //   開放度の低い国（アジア内陸・自給経済）は引力を感じない＝銀圏に残る。⚠国名も年号も書かない。
            //   MonetaryPeerPullScale=0（旧既定・点火5.0=2026-08-21銀本位ラウンド）では注入自体をスキップ（TradeGoldPull は0のまま）＝旧挙動と厳密恒等。
            if (_coef.MonetaryPeerPullScale > 0)
            {
                double goldTrade = 0, worldTrade = 0;
                Span<double> natTrade = Nations.Count <= 128 ? stackalloc double[Nations.Count] : new double[Nations.Count];
                for (int i = 0; i < Nations.Count; i++)
                {
                    var wn = Nations[i].World;
                    double t = 0;
                    if (wn.ExportsByGood is { } eg) foreach (var v in eg) if (v > 0) t += v;
                    if (wn.ImportsByGood is { } ig) foreach (var v in ig) if (v > 0) t += v;
                    if (!(t >= 0)) t = 0;   // NaNは0へ倒す（否定形）
                    natTrade[i] = t; worldTrade += t;
                    if (wn.MonetaryLaw == MonetaryStandardLaw.Gold) goldTrade += t;
                }
                double goldShare = worldTrade > 1e-9 ? goldTrade / worldTrade : 0;
                if (!(goldShare >= 0 && goldShare <= 1)) goldShare = 0;   // NaN/発散は0へ（否定形）
                // 開放度は**世界平均との比**で正規化する（min(1, 自国開放度/世界平均)）：貿易/GDPの絶対水準は時代と較正で桁が動く
                //（世界貿易の消滅の記録＝1914英0.2%）ので、絶対値に目盛りを彫ると尺度が壊れる。平均以上＝引力を満額感じる／
                //   平均未満＝比例で弱く＝「対外に開いた経済ほど相手に本位を合わせたい」だけを尺度不変に取り出す。⚠新しい定数ゼロ（capの1は正規化の定義）。
                double openSum = 0; int openN = 0;
                Span<double> natOpen = Nations.Count <= 128 ? stackalloc double[Nations.Count] : new double[Nations.Count];
                for (int i = 0; i < Nations.Count; i++)
                {
                    var wn = Nations[i].World;
                    double annualGdp = wn.NominalGdp * Calendar.MonthsPerYear;
                    double openness = annualGdp > 1e-9 ? natTrade[i] * _coef.ClearingsPerYear / annualGdp : 0;   // 清算単位→年換算（単位ヘルパー）
                    if (!(openness >= 0)) openness = 0;   // 否定形（分母が痩せた比の暴走もここで抑える）
                    natOpen[i] = openness; openSum += openness; openN++;
                }
                double openMean = openN > 0 ? openSum / openN : 0;
                for (int i = 0; i < Nations.Count; i++)
                {
                    var wn = Nations[i].World;
                    double rel = openMean > 1e-12 ? natOpen[i] / openMean : 0;
                    if (!(rel >= 0)) rel = 0;   // 否定形
                    wn.TradeGoldPull = goldShare * Math.Min(1.0, rel);
                }
            }
            // ★鋼市場診断#2（2026-08-16・AiWorldPriceEntry>0のみ）：国際市場の平均価格（Market.Price＝BilateralMarket が成約から書く265州平均）を
            //   各国 World へ**読み取り専用**注入＝WorldTechLevel/IndustrialFrontier と同型の四半期同期・WorldSilverPrice(G6) と同じ
            //   「World は国際市場を見られない（層の分離）」ための配布。⚠新しい価格を作らない——既存の平均価格をそのまま渡すだけ。
            //   読み手＝SelfPlay.TryBuildFacility（AIの参入判断＝逆インセンティブの反転）のみ。
            //   係数0＝注入自体をスキップ（World 側は null のまま・配列も確保しない）＝厳密恒等。★既定は点火1.0＝毎四半期注入する。
            if (_coef.AiWorldPriceEntry > 0)
                foreach (var n in Nations)
                {
                    var wn = n.World;
                    if (wn.WorldGoodsPrices is not { } wgp || wgp.Length != Market.Price.Count)
                        wn.WorldGoodsPrices = wgp = new double[Market.Price.Count];
                    for (int gi = 0; gi < wgp.Length; gi++) wgp[gi] = Market.Price[gi];
                }
            // 財5（対外国債の他国資本引き当て・プール注入）：各国の対外国債プールに『他モデル国の資本家貯蓄の合計』＝対外国債の引き受け余力を注入。
            // Fiscal.Step はこれを上限に対外債発行をクランプする（実引き当て/保有国配分は #17 の CapitalMarket 経路で・notes 参照）。
            // 毎四半期このプールへ注入し Fiscal 側が上限として読む（★2026-09-26：旗 ForeignBondFromInvestors を焼き込み・x-01）。
            {
                double worldCapSavings = 0;
                foreach (var n in Nations)
                    foreach (var r in n.World.Regions) if (r.State == RegionState.Active)
                        foreach (var p in r.Pops) if (p.IsModernElite && !p.IsSoldier) worldCapSavings += Math.Max(0, p.Savings);
                foreach (var n in Nations)   // 各国の余力＝世界の資本家貯蓄から自国ぶんを除いた合計（他国だけが自国債を引き受けられる）
                {
                    double own = 0;
                    foreach (var r in n.World.Regions) if (r.State == RegionState.Active)
                        foreach (var p in r.Pops) if (p.IsModernElite && !p.IsSoldier) own += Math.Max(0, p.Savings);
                    n.World.ForeignBondInvestorPool = Math.Max(0, worldCapSavings - own);
                }
            }
            // ★§1-4（ForeignLendingDebits・2026-08-30）：対外借入元本の貸し手側引き落とし。借り手は Fiscal で +CumulativeTradeFlow 済み＝
            //   ここで他国資本家の貯蓄から比例控除し、貸し手国の CumulativeTradeFlow を同額 − する＝両建てで世界の貨幣創造を閉じる。
            //   比例の物差し＝国内債と同じ max(0,Savings) 比（Fiscal の domestic 枝と同型）。★2026-09-26：旗 ForeignLendingDebits を焼き込み（x-01）。
            {
                for (int bi = 0; bi < Nations.Count; bi++)
                {
                    var bw = Nations[bi].World;
                    double owe = bw.PendingForeignBorrow;
                    if (!(owe > 1e-9)) { bw.PendingForeignBorrow = 0; continue; }
                    bw.PendingForeignBorrow = 0;
                    double lendTotal = 0;
                    for (int li = 0; li < Nations.Count; li++)
                    {
                        if (li == bi) continue;
                        foreach (var r2 in Nations[li].World.Regions) if (r2.State == RegionState.Active)
                            foreach (var p2 in r2.Pops) if (p2.IsModernElite && !p2.IsSoldier) lendTotal += Math.Max(0, p2.Savings);
                    }
                    if (!(lendTotal > 1e-9)) continue;   // 貸し手不在（合成テスト世界）＝控除なし（借入は既に成立済み＝旧挙動へ倒す）
                    for (int li = 0; li < Nations.Count; li++)
                    {
                        if (li == bi) continue;
                        var lw = Nations[li].World;
                        double debited = 0;
                        foreach (var r2 in lw.Regions) if (r2.State == RegionState.Active)
                            foreach (var p2 in r2.Pops) if (p2.IsModernElite && !p2.IsSoldier)
                            {
                                double d2 = owe * (Math.Max(0, p2.Savings) / lendTotal);
                                p2.Savings -= d2; debited += d2;
                            }
                        if (debited > 1e-12) lw.CumulativeTradeFlow -= debited;   // 貸し手側の台帳＝流出（借り手の +流入 と対）
                    }
                }
            }
            ReabsorbNations();       // ★内戦一元化：内戦決着の再統合（州の返還・国家除去）。SpawnSecededNations の前＝同四半期の建国と衝突しない
            SpawnSecededNations();   // 建国キューの回収（分離戦勝利の州→新国家）。Nations走査の外＝リスト追記が安全・新国家は次の四半期から鼓動
            StepDiffusion();         // ★発見・段7：思想の伝播（貿易路×メディア・前四半期読み＝閉路なし）。DiffusionWeight=0で即return＝恒等
            // 助走中は共有層の四半期カウンタも止める（2026-08-10）＝本編は必ず _quarter=0（外交の月次索引 _quarter*3+m の起点）から始まる。
            // 助走では DiplomacyFrozen＝Diplomacy.Step を1度も呼ばないので、止めても飛ばした索引は存在しない。
            if (!_calendarFrozen) _quarter++;
        }
        _dayInQuarter = (_dayInQuarter + 1) % Calendar.DaysPerQuarter;
        return quarterDone;
    }

    /// <summary>★内戦一元化（CivilWarAsWar・2026-08-30）：内戦決着の再統合＝SpawnNation の厳密な逆。
    /// 政府勝利＝反乱国の全州を親へ返還し国家を除去／反乱勝利＝先に親へ政体を敷き（ImposeRegime と同じ機構）、同じ返還を行う（革命の成就）。
    /// 保存則＝反乱国の基礎貨幣（S=貯蓄和＋T=国庫−L=与信）と人口 P を親の台帳へ（貸借の仕分けは ReabsorbNations の★2026-09-22）。民族反転（建国時の 1−x）も戻す。
    /// 白紙講和はここへ来ない＝分断がそのまま残る（台湾・南北型）。</summary>
    /// <summary>★発見・段7（2026-08-31）：思想の伝播。到達(A→B)＝貿易額(A,B)/買い手GDP×メディアA×メディアB×**前四半期の**到達A
    /// （疫病の伝播と同型＝各到達国が新たな発信源・再放射。前四半期スナップショット＝同期読みの閉路禁止・原典§5-5）。
    /// メディア＝NewsFill×識字＋RadioFill（pop加重・全て既存量）。ペア行列は四半期で消費しゼロ化。
    /// ⚠DiffusionWeight=0＝即return（ペア記帳側も同ゲート）＝厳密恒等。読者（思想eraゲート）は段8。</summary>
    private void StepDiffusion()
    {
        var c = _coef;
        if (!(c.DiffusionWeight > 0)) return;
        var pf = Market.PairFlowQ;
        if (pf is null) return;
        int n = System.Math.Min(Nations.Count, pf.Length);
        var media = new double[n];
        for (int i2 = 0; i2 < n; i2++)
        {
            double m = 0, sz = 0;
            foreach (var r in Nations[i2].World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p2 in r.Pops) { m += (p2.NewsFill * p2.Literacy + p2.RadioFill) * p2.Size; sz += p2.Size; }
            }
            media[i2] = sz > 1e-9 ? System.Math.Clamp(m / sz, 0, 1) : 0;
        }
        var prev = new double[n][];
        for (int i2 = 0; i2 < n; i2++)
        {
            var wr = Nations[i2].World;
            // ★段8の種（2026-09-04）：思想を**持つ**国が放射源＝到達を自国の National シェアで下支え（名前で起点を書かない・対称）。
            //   IdeologyReachGate=0 でも DiffusionWeight>0 なら種は積まれるが読み手が無い＝世界は動かない（deadprobe の通り）。
            // ★2026-09-04 E-12実測（差し戻し）：種を IdeologyReach に**書き戻す**と「自国シェア→自国到達→自国ゲート→自国シェア」の
            //   正帰還が国内で閉じ、社会主義が61/61カ国へ1792〜95年に自家発火した。種は**放射側（prev）だけ**に使い、ゲートが読む
            //   受信側 IdeologyReach には他国から届いた分しか積まない（＝伝播の定義）。
            prev[i2] = (double[])wr.IdeologyReach.Clone();
            if (_coef.IdeologyReachGate > 0)
            {
                var nat = Ideologies.National(wr);
                for (int k2 = 0; k2 < prev[i2].Length && k2 < nat.Length; k2++)
                    if (nat[k2] > prev[i2][k2]) prev[i2][k2] = nat[k2];
            }
        }
        // 正規化＝**世界総貿易に占めるペアのシェア**（実測2026-08-31：買い手GDP割は小国を過大評価し
        //   「英→ハワイ・阿爾及が仏独より先」の逆転を生んだ。絶対量の大きい経路＝英仏・英米が太い＝史実の順序。
        //   世界貿易の成長とともに伝播も速くなる＝グローバル化が思想を速めた、も自然に出る）。
        double worldFlow = 0;
        for (int a0 = 0; a0 < n; a0++) for (int b0 = 0; b0 < n; b0++) worldFlow += pf[a0][b0];
        // ★接触度（作者採用2026-09-06・知識doc §4／2026-09-07 是正）：自国の貿易の**強度**（(入＋出)÷自国名目GDP）÷世界の強度（世界貿易×2÷世界名目GDP）
        //   ＝平均1.0・鎖国0。世界シェアだと経済の大きさに比例して清が最大になる（実測 6.8〜7.5）＝接触は量でなく強度。思想の到達と同じ入力＝新しい量を作らない。
        {
            double worldGdp = 0; for (int a1 = 0; a1 < n; a1++) worldGdp += System.Math.Max(0, Nations[a1].World.NominalGdp);
            double worldIntensity = worldGdp > 1e-9 ? 2.0 * worldFlow / worldGdp : 0;
            for (int a1 = 0; a1 < n; a1++)
            {
                double own = 0; for (int b1 = 0; b1 < n; b1++) if (b1 != a1) own += pf[a1][b1] + pf[b1][a1];
                double gdp = Nations[a1].World.NominalGdp;
                double v = worldIntensity > 1e-12 && gdp > 1e-9 ? (own / gdp) / worldIntensity : 0;
                Nations[a1].World.TradeContactShare = !(v >= 0) ? 0 : v;
            }
        }
        if (!(worldFlow > 1e-9)) { Market.LastPairFlow = pf; Market.PairFlowQ = null; return; }   // 消費（テレメトリ退避＝次四半期は再確保）
        for (int b = 0; b < n; b++)
        {
            var wB = Nations[b].World;
            for (int a = 0; a < n; a++)
            {
                if (a == b) continue;
                double link = c.DiffusionWeight * (pf[a][b] / worldFlow) * media[a] * media[b];
                if (!(link > 0)) continue;
                var ra = prev[a];
                var rb = wB.IdeologyReach;
                for (int k = 0; k < ra.Length && k < rb.Length; k++)
                    if (ra[k] > 0) rb[k] = System.Math.Min(1.0, rb[k] + link * ra[k]);
            }
        }
        Market.LastPairFlow = pf; Market.PairFlowQ = null;   // 四半期消費（行列はテレメトリへスワップ退避＝pairprobeが読む・次四半期は再確保）
    }

    internal void ReabsorbNations()   // internal＝番人（FiscalTests の再併合の貸借）が四半期の他の流れを挟まずにこの1段だけを踏む
    {
        // ★残骸国家の自然再併合（2026-08-31・清プレイレポート同日ラウンド）：内戦の白紙分断で生まれた国が
        //   親のGDP1%未満（スケールフリーの親相対・⚠構造定数＝連続性docの対象として記録）を RumpReabsorbYears 年続けたら
        //   平和的に親へ戻す（産出1の死骸が61カ国の外交・清算・ハーネスを漂う問題の是正）。親が交戦中/不在なら持ち越し。
        if (_coef.RumpReabsorbYears > 0)
            for (int ri2 = 0; ri2 < Nations.Count; ri2++)
            {
                var rw2 = Nations[ri2].World;
                if (rw2.RumpParentName.Length == 0 || rw2.SelfPower is not { } rp2) continue;
                var par2 = Diplomacy.Powers.Find(q => (q.BaseName.Length > 0 ? q.BaseName : q.Name) == rw2.RumpParentName);
                if (par2?.Nation is not { } pw2) { rw2.RumpParentName = ""; continue; }   // 親が消滅＝独立が確定
                bool low = rw2.NominalGdp < RumpGdpFractionOfParent * System.Math.Max(1.0, pw2.NominalGdp);
                rw2.RumpLowQuarters = low ? rw2.RumpLowQuarters + 1 : 0;
                if (rw2.RumpLowQuarters >= (int)(_coef.RumpReabsorbYears * Calendar.QuartersPerYear)
                    && !Diplomacy.IsAtWar(par2, rp2))
                {
                    Diplomacy.PendingReintegrations.Add((par2, rp2, false, pw2.Polity));
                    rw2.RumpParentName = "";   // 二重予約の防止
                    Diplomacy.Log.Add($"{pw2.Year} {rp2.Name}は立ち行かず{par2.Name}へ平和的に復帰した");
                }
            }
        if (Diplomacy.PendingReintegrations.Count == 0) return;
        foreach (var (parentP, rebelP, rebelWon, rebelPolity) in Diplomacy.PendingReintegrations)
        {
            int pi = Nations.FindIndex(n => n.World.SelfPower == parentP);
            int ri = Nations.FindIndex(n => n.World.SelfPower == rebelP);
            if (pi < 0 || ri < 0 || pi == ri) continue;   // どちらかが既に消滅（防御・二重予約）
            var pw = Nations[pi].World; var rw = Nations[ri].World;
            if (rebelWon && pw.PoliticsEnabled && pw.Polity != rebelPolity)
            {
                CrownParliament.ApplyRegime(pw, _coef, rebelPolity);
                Politics.RaiseFoundingInstitutions(pw, allowRevoke: true, _coef);   // 新政体の含意参政権・報道（自由化も反動剥奪も対称）
            }
            // ★再併合の貸借（2026-09-22・作者裁定「全て推奨」）：反乱国の貸借の各行を「州について親へ行く」か「請求権として消える（金は動かない）」かに仕分ける。
            //   規則＝貨幣であるものは湧きも消えもしない＝世界の基礎貨幣 Σ(貯蓄＋国庫−与信) は再併合の前後で同じ。
            //   旧＝親へ渡すのは貯蓄と**正の**国庫だけで、反乱国の与信と負の国庫が国ごと消えた＝世界の貨幣がその額だけ湧いた（㉚の前の木・seed1789・604四半期の一時計器で +10,241）。
            //   親へ：貯蓄（POPごと）・国庫（負＝当座借越も）・与信（Loans＝預金の対／HouseholdLoans＝POPと一緒に動く Pop.Debt の対）・
            //         外資の負債と対外資産（ForeignCapitalIn／CapitalAbroad＝工場も投資家も州にいる）・対外債権（ForeignBondClaims）・
            //         途中の管（元利の管／借入の予約＝同じ四半期の CapitalMarket.Step と貸し手の引き落としで空だが、順序に頼らない）・造幣局の金属準備（現物）。
            //   消える：反乱国の国債＝国内の保有（下の BondHoldings）と対外債（ForeignDebt）の両方。対外債は債権国の ForeignBondClaims を債権の比で落とす
            //         （損は持ち手。落とさないと相手のいない債権が元利の配当を吸う）。
            //   Bank.Gold は導出量（Credit.Step が毎日 BaseMoney から上書き）＝貨幣の行ではない（返還の1行も 2026-09-28 に削除）。
            double S = 0, P = 0;
            foreach (var rr in rw.Regions) { S += rr.SumSavings(); P += rr.SumPopulation(); }
            double T = rw.Treasury;
            var moving = new System.Collections.Generic.List<Region>(rw.Regions);
            // ★国債の保有台帳（2026-09-21）：反乱国は国家ごと消え、その国債（GovDebt）も既存どおり誰にも継承されない＝国が払うのを止めた割合（全額）だけ
            //   保有を消す＝損は持ち手（金は動かない）。★史実：合衆国憲法修正14条4節（1868）は南部連合の債務の継承・支払いを明文で禁じた。
            //   rw.GovDebt も同額下げる＝消える直前まで反乱国側の不変条件も厳密。
            foreach (var rr in moving) foreach (var pp in rr.Pops) { rw.GovDebt -= pp.BondHoldings; pp.BondHoldings = 0; }
            foreach (var rr in moving)
            {
                rw.Regions.Remove(rr);
                rr.Id = pw.Regions.Count;   // 親は必ず首都（Id0）を持つ＝返還州は末尾へ連番
                rr.Seceded = false;
                Territory.Rehome(rr, Territory.Rehoming.Returned);   // 回1-1：建国時の反転を戻す（親の中核定義へ復帰）・市場の台帳のリセット・植民地財の空化（null にしない＝col-02）
                pw.Regions.Add(rr);
            }
            pw.Pay(T >= 0 ? TreasuryCat.OtherIn : TreasuryCat.OtherOut, T);
            double L = 0;
            if (pw.Bank is { } pb && rw.Bank is { } rb)
            {
                pb.Loans += rb.Loans; pb.HouseholdLoans += rb.HouseholdLoans; L = rb.Loans + rb.HouseholdLoans;
            }
            pw.CumulativeTradeFlow += S + T - L;   // ＝反乱国の基礎貨幣＝親の per-nation 保存が閉じ、世界の和も動かない（建国時の −(S+T) の鏡映＋反乱国として積んだ分）
            pw.PopulationDelta += P;
            pw.ForeignCapitalIn += rw.ForeignCapitalIn; pw.CapitalAbroad += rw.CapitalAbroad;
            // 反乱国の対外債の否認：債権へ繰り上げ済みの額（PrevForeignDebt）を、反乱国以外の債権の比で落とす（反乱国自身の債権は次の行で親へ）
            double repudiated = rw.PrevForeignDebt, held = 0;
            for (int j = 0; j < Nations.Count; j++) if (j != ri) held += Nations[j].World.ForeignBondClaims;
            if (repudiated > 1e-9 && held > 1e-9)
            {
                double keep = System.Math.Max(0, 1.0 - repudiated / held);
                for (int j = 0; j < Nations.Count; j++) if (j != ri) Nations[j].World.ForeignBondClaims *= keep;
            }
            pw.ForeignBondClaims += rw.ForeignBondClaims;
            pw.ForeignIntPaidThisQuarter += rw.ForeignIntPaidThisQuarter; pw.PendingForeignBorrow += rw.PendingForeignBorrow;
            pw.SilverReserveQty += rw.SilverReserveQty; pw.GoldReserveQty += rw.GoldReserveQty;
            // 反乱国の植民地（戦中に得た場合の防御）＝親へ移す（無主化して消さない）
            if (rebelP.ColonyList.Count > 0) { parentP.ColonyList.AddRange(rebelP.ColonyList); rebelP.ColonyList.Clear(); }
            // ★計器の是正2026-09-08（F3b蜂起doc §5 段1・純記帳）：F3bの「蜂起を武力で鎮圧」ログ（Politics.CheckFactionRevolt の無血鎮圧枝）は
            //   到達不能になり（枝そのものは2026-09-08の段6で物理削除）、鎮圧が年代記から消えていた。鎮圧の実体は「親が内戦に勝って再併合する」
            //   この経路なので、決着した運動の名をここで出す＝計器を実経路へ移す（力学は1ビットも読まない）。
            string spentName = pw.RevoltingFaction is { } spentF ? $"（{Factions.DisplayName(spentF)}の蜂起を鎮圧）" : "";
            // ★段2＝鎮圧の後始末（F3b蜂起doc §3-4・作者裁定②＝A・2026-09-08）。旧コメントは「燃え尽き＝旧経路と同じ帰結」と書いていたが
            //   **嘘**だった：旧経路（Politics.cs:985 の ProcessCivilWar 決着）は主体の解放の前に `spent.Cohesion = 0` を書いて組織を壊滅させるが、
            //   ここは参照を捨てるだけで結束は満額のまま残る＝同じ運動が結束0.8超のまま翌期また立てる（ヴュルテンベルクの毎年7月1日の正体）。
            //   ⚠この4行はすべて**既存の量の再利用**（新しい数はゼロ）。RevoltAftermathOnWarPath=0 で1命令も走らない＝現行のまま。
            //   ⚠主体（pw.RevoltingFaction）がある＝F3bの運動が武力で決着した場合だけ。残骸の平和的復帰（RumpReabsorb）には後始末を書かない。
            if (_coef.RevoltAftermathOnWarPath > 0 && pw.RevoltingFaction is { } spent)
            {
                spent.Cohesion = 0;   // 組織の壊滅＝旧経路 Politics.cs:985 と同じ1行（結束の再成長は FactionCohesionGrowth のロジスティック＝物理的に4〜16年）
                // 恐怖（人は静まる）＝**返還州のPOPだけ**。全国一律にすると旧経路 Politics.cs:979 と二重になる。
                // フィルタなしは中央が鎮圧して州を再統合する Politics.cs:869 と同形（同じ状況＝同じ規則）。
                foreach (var rr in moving) foreach (var pp in rr.Pops) pp.Militancy *= _coef.ReformMilitancyRelief;
                if (!rebelWon)   // 親が鎮定した側だけ＝反乱勝利は上の ApplyRegime/RaiseFoundingInstitutions が新体制を敷く（正統性へ二重に触らない）
                {
                    double legBeforeCw = pw.Legitimacy;
                    pw.Legitimacy = System.Math.Max(0, pw.Legitimacy - _coef.RevoltSuppressLegitCost);   // 流血の記憶＝F1の鎮圧 Politics.cs:1498 と同じストックへの傷
                    pw.Explain?.Add("Legitimacy", "内戦の鎮定の流血(ストック正統性への傷)", pw.Legitimacy - legBeforeCw);   // 説明API（#63）
                    pw.RuptureWindowQuarters = System.Math.Max(pw.RuptureWindowQuarters, (int)_coef.RuptureWindowQuartersOnCrush);   // 反動の窓＝旧経路 Politics.cs:981 と同じ
                }
            }
            pw.RevoltingFaction = null;   // 運動の主体はここで軍事的に決着した（参照の解放のみ）
            Diplomacy.RemovePower(rebelP);
            Nations.RemoveAt(ri);
            Diplomacy.Log.Add(rebelWon
                ? $"{pw.Year} {rebelP.Name}が{parentP.Name}を制した＝{rebelPolity}の下で再統一"
                : $"{pw.Year} {parentP.Name}が{rebelP.Name}を再併合＝内戦の傷跡だけが残った{spentName}");
        }
        Diplomacy.PendingReintegrations.Clear();
    }

    // 疫病の国家間伝播（B-4）：感染国（EpidemicActiveYears>0）から、近接（首都距離＝陸路）と交易接触（両国の交易開放度の積＝海路の港）で、未感染国へ年次で飛び火。
    // 受け手が過密ほど定着しやすい。決定論は受け手の専用 EpidemicRng（各国独立＝盤面非依存）。死亡・感染窓は World.TriggerEpidemic に集約。単一国較正は MultiNationSim を使わず＝非走行で無影響。
    private int _lastEpiYear = int.MinValue;
    private void StepEpidemicTransmission()
    {
        int year = Nations.Count > 0 ? Nations[0].World.Year : 0;
        if (year == _lastEpiYear) return;   // 年が変わった四半期に1回
        _lastEpiYear = year;
        int n = Nations.Count;
        for (int i = 0; i < n; i++)
        {
            var w = Nations[i].World;
            if (w.EpidemicActiveYears > 0) continue;   // 既に流行中＝対象外
            double exposure = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                var m = Nations[j].World;
                if (m.EpidemicActiveYears <= 0) continue;   // 感染源でない
                double km = Geo.DistanceKm(w.Latitude, w.Longitude, m.Latitude, m.Longitude);
                double proximity = Math.Clamp(1.0 - km / (_coef.EpidemicTransmitRange * _coef.EraFactor(w.WorldTechLevel, _coef.EraEpidemicRange)), 0, 1);   // ★時代関数化（掃討第2波2026-08-13）：鉄道と汽船で伝播距離が伸びる（2026-08-29点火＝既定1。=0で×1恒等）              // 陸路＝近いほど強い
                double trade = _coef.EpidemicTradeTransmit * w.ExternalTradeAccess() * m.ExternalTradeAccess();   // 海路＝両国が交易に開かれているほど（港経由のコレラ）
                // 戦争＝距離に依らない強制的な濃厚接触（軍営・前線・補給線・復員兵）＝最強の伝播経路（スペイン風邪1918型）。
                double war = w.SelfPower is { } sp && m.SelfPower is { } mp && Diplomacy.IsAtWar(sp, mp) ? _coef.EpidemicWarTransmit : 0;
                exposure = Math.Max(exposure, Math.Max(Math.Max(proximity, trade), war));   // 最も近い/繋がった/交戦中の感染国からの曝露
            }
            if (exposure <= 1e-9) continue;
            double chance = _coef.EpidemicTransmitChance * exposure * w.MaxUrbanShare();   // 受け手も過密ほど定着
            var rng = Nations[i].EpidemicRng;   // 受け手の専用RNG（各国独立＝盤面非依存の決定論）
            if (rng.NextDouble() < chance) w.TriggerEpidemic(_coef, EpidemicTransmitSeverityFloor + rng.NextDouble());   // 伝染＝死亡スパイク＋感染窓（連鎖しうる）
        }
    }

    private int _lastWeatherYear = int.MinValue;
    // 緯度帯（気候帯）の天候（②集計粒度・国跨ぎ）：同じ緯度帯の国を一緒に豊凶させる＝欧州一帯の干ばつ/冷夏。
    // 各国の HarvestFactor（自前の局地天候）と積算＝帯と国の悪い目が重なれば大不作（さらに作物病虫害 CropHealth と三重積算しうる）。専用RNG＝主系列/外交/疫病を乱さない。
    private void StepWeatherBands()
    {
        int year = Nations.Count > 0 ? Nations[0].World.Year : 0;
        if (year == _lastWeatherYear) return;   // 年が変わった四半期に1回
        _lastWeatherYear = year;
        var bandFactor = new System.Collections.Generic.Dictionary<int, double>();
        foreach (var n in Nations)
        {
            int band = (int)Math.Floor(n.World.Latitude / _coef.WeatherBandSize);   // 首都緯度を帯に丸める（15度ごと）
            if (!bandFactor.TryGetValue(band, out double f))
            {
                double bandEps = 1.0 + (_weatherRng.NextDouble() - 0.5) * 2.0 * _coef.WeatherBandVariation;   // 帯全体の豊凶（同じ帯の国は同じ値＝気候相関）
                double brho = _coef.WeatherBandPersistence;
                double bprev = _prevBandFactor.TryGetValue(band, out double bp) ? bp : 1.0;
                // 他15（監査2026-07-11・2026-07-25修正）：革新係数を(1−ρ)から√(1−ρ²)へ＝分散保存AR(1)。旧凸結合はρ>0で実効振幅が縮む
                //（ρ=0.5で0.69倍＝持続性を上げるほど天候がおとなしくなる逆効果）。brho=0 では √1=1＝純IID（既定0.35＝多年相関あり・有効化2026-07-07）。
                f = brho > 0 ? 1.0 + brho * (bprev - 1.0) + Math.Sqrt(1.0 - brho * brho) * (bandEps - 1.0) : bandEps;
                bandFactor[band] = f;
                _prevBandFactor[band] = f;
            }
            n.World.WeatherBandFactor = f;
        }
    }

    // 海峡・運河（§B）：扼者を host 領土から解決し、運河は host 保有の経済国の資本家が自腹で建設（投資家→自国pop＝労務で保存）。富裕な工業国ほど速く開通。
    private void StepCanals()
    {
        foreach (var cp in Diplomacy.Chokepoints)
        {
            cp.Owner = ResolveOwner(cp.Host);   // 扼者＝host保有国（封鎖を握る。host未征服＝中立で封鎖者なし）。★扼者の解決は助走中も通す＝封鎖と海路の物理は開始盤面の一部
            if (!cp.IsCanal || cp.IsOpen) continue;
            if (_calendarFrozen) continue;   // ★助走：運河の建設は進めない（能力の凍結）。1783年は技術ゲート未達で拠出者ゼロだが、罠を残さない
            double cost = cp.BuildCost;
            int year = 0;
            // 所有に関わらず、利益を見る各国の資本家が投資（スエズ＝欧州全体の利益。仏資本で建設の史実）。投資家→自国pop で保存。
            foreach (var sim in Nations)
            {
                var w = sim.World; year = w.Year;
                if (w.TechLevel < cp.TechGate) continue;   // 近代土木の技術ゲート＝運河の時代を出す（スエズ1869/パナマ1914）。技術を持つ国だけが拠出できる
                // 外7：CanalInvestReturns>0 のとき host（扼者）と交戦中の国は拠出停止（敵の運河には投資しない＝資本の論理）。既定0.5＝停止が効く。0で無条件拠出。
                if (_coef.CanalInvestReturns > 0 && cp.Owner is { Nation: not null } cpo && w.SelfPower is { } wsp && Diplomacy.IsAtWar(wsp, cpo)) continue;
                double pool = 0;
                foreach (var r in w.Regions) if (r.State == RegionState.Active && r.InvestorPopIndex >= 0) pool += Math.Max(0, r.Pops[r.InvestorPopIndex].Savings);
                if (pool <= 1e-6) continue;
                double inv = _coef.CanalInvestFrac * pool;
                foreach (var r in w.Regions) if (r.State == RegionState.Active && r.InvestorPopIndex >= 0) { var ip = r.Pops[r.InvestorPopIndex]; ip.Savings -= inv * (Math.Max(0, ip.Savings) / pool); }   // 投資家から引く（pool と同一フィルタ＝総額=inv）
                double tot = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) tot += p.Size;
                if (tot > 1e-9) foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) p.Savings += inv * (p.Size / tot);   // 自国pop（建設労務）へ＝保存
                cp.BuildProgress += inv;
                // 外7：拠出国の持分（累計拠出額）を記録＝後で通行料を持分比で分配（配当）。既定0.5＝この持分が配当に効く。0では通行料が扼者総取りのままなので持分は参照されず無影響。
                if (_coef.CanalInvestReturns > 0 && w.SelfPower is { } shp)
                    cp.InvestorShares[shp.Id] = cp.InvestorShares.TryGetValue(shp.Id, out double prev) ? prev + inv : inv;
            }
            // ★即開通ガード（2026-07-28）：下限が無いと BuildCost 未設定（データ記述漏れ＝0）のとき 0>=0 が真になり、
            // **1783年の最初の四半期にその運河が開通**して海路距離が全世界で即変わる（604四半期が全面的に別物になる）。
            // 「データが無い＝不発」を保証する安全弁。ログは従来「スエズ運河運河が開通」と重複していたので是正（Runner の集計語も同時に追随）。
            if (cost > 1e-9 && cp.BuildProgress >= cost) { cp.IsOpen = true; Diplomacy.Log.Add($"{year} {cp.Name}が開通（{(cp.Owner is { } o ? o.Name + "が扼す" : "中立")}）"); }
        }

        // 通行料：開通済みの咽喉の利用国から扼者の国庫へ（迂回より安い＝使う方が得）。実在の扼者のみ徴収。保存＝両国の CumulativeTradeFlow。
        // 監査2026-07-11（リ2・CanalTollByRoute=1既定）：旧＝「本国の海盆所属（ServesBasins）」で課金対象を決めていた＝スエズの支払国が
        // 日本・清など東方勢のみで、史実の最大利用者（対印交易の英仏）が1銭も払わない逆転＋自然海峡（ジブラルタル等＝19世紀に通航税は実在しない）
        // まで課金していた。新＝(a)課金は運河（IsCanal）のみ＝自然海峡は封鎖の物理（ChokeBlockadePenalty）だけ残す
        // (b)課金対象＝「交易重心への海路がこの運河の開通で実際に縮む国」（利用の物理＝Geo.SeaDistanceKm の開通あり/なし差分）
        // (c)基数＝海運取扱高（通航トン数課金）。CanalTollByRoute=0 で旧挙動（海盆所属判定）。
        // 02断線走査2026-07-28：CanalTollByRoute=1恒久確定＝旧・海盆所属判定の分岐（CanalTollRate GDP比例/CanalTollByTraffic）を係数ごと削除＝ルート判定（海路が実際に縮む国だけ課金）一本化
        double tollCx = 0, tollCy = 0, tollCz = 0, tollTw = 0;   // 手順3.5（2026-07-28）：球面重心（InternationalMarket と同型＝円柱平均のずれを是正）
        bool suezOpenNow = false, panOpenNow = false;
        foreach (var cp2 in Diplomacy.Chokepoints) { if (cp2.Id == Diplomacy.SuezId) suezOpenNow = cp2.IsOpen; else if (cp2.Id == Diplomacy.PanamaId) panOpenNow = cp2.IsOpen; }
        foreach (var sim in Nations)
        {
            var w2 = sim.World; double t2 = w2.SelfPower?.TradeWeight ?? 1.0;
            double rl2 = w2.Latitude * Math.PI / 180.0, ro2 = w2.Longitude * Math.PI / 180.0;
            tollCx += Math.Cos(rl2) * Math.Cos(ro2) * t2; tollCy += Math.Cos(rl2) * Math.Sin(ro2) * t2; tollCz += Math.Sin(rl2) * t2;
            tollTw += t2;
        }
        double tollClat = tollTw > 1e-9 ? Math.Atan2(tollCz, Math.Sqrt(tollCx * tollCx + tollCy * tollCy)) * 180.0 / Math.PI : 0;
        double tollClon = tollTw > 1e-9 ? Math.Atan2(tollCy, tollCx) * 180.0 / Math.PI : 0;
        foreach (var cp in Diplomacy.Chokepoints)
        {
            if (!cp.IsOpen || cp.Owner is not { Nation: { } ow } op) continue;
            if (!cp.IsCanal) continue;   // 自然海峡に通航税は実在しない（1936モントルーまで）＝封鎖効果のみ
            foreach (var sim in Nations)
            {
                var uw = sim.World;
                if (uw == ow || uw.SelfPower is not { } up || up == op) continue;
                if (Diplomacy.IsAtWar(up, op)) continue;   // 交戦＝封鎖で通れない（通行料は発生しない）
                bool isSuez = cp.Id == Diplomacy.SuezId, isPan = cp.Id == Diplomacy.PanamaId;
                double dWith = Geo.SeaDistanceKm(uw.Latitude, uw.Longitude, tollClat, tollClon, suezOpenNow, panOpenNow);
                double dWithout = Geo.SeaDistanceKm(uw.Latitude, uw.Longitude, tollClat, tollClon, suezOpenNow && !isSuez, panOpenNow && !isPan);
                if (dWithout - dWith < 1.0) continue;   // この運河で海路が縮まない国＝通らない＝課金しない（英仏の対印交易はスエズで縮む＝払う）
                double traffic = 0; foreach (var r in uw.Regions) if (r.State == RegionState.Active) traffic += r.LastPortThroughput;
                double tollBase = _coef.CanalTollTrafficRate * traffic;   // 通航トン数課金（取扱高の代理・前四半期値）
                double toll = Math.Min(tollBase, Math.Max(0, uw.Treasury));
                if (toll <= 1e-9) continue;
                uw.Pay(TreasuryCat.OtherOut, -toll); uw.CumulativeTradeFlow -= toll;   // 使用国＝対外流出
                // 外7：CanalInvestReturns>0 なら通行料の一部を建設拠出国へ持分（累計拠出額）比で配当し、残りを扼者へ。既定0.5＝配当あり。0＝全額扼者。
                double dividend = _coef.CanalInvestReturns > 0 ? DistributeCanalDividend(cp, toll * _coef.CanalInvestReturns, op) : 0;
                double toOwner = toll - dividend;   // 配当されなかった残余（＝扼者取り分）。分配失敗時も toll 全額が扼者へ回り保存
                ow.Pay(TreasuryCat.OtherIn, toOwner); ow.CumulativeTradeFlow += toOwner;   // 扼者＝対外流入（保存）
            }
        }
    }

    // 外7：通行料の配当ぶんを、この運河の建設拠出国へ持分（InvestorShares＝累計拠出額）比で配分。扼者自身への配当は扼者本人の徴収と二重計上になるため除外し、その分は残余として扼者へ戻す（呼び出し側で toll−dividend）。
    // 返り値＝実際に外部拠出国へ配当した総額（＜=amount）。全pop⇄国庫でなく国庫⇄国庫の移転＝両建てで CumulativeTradeFlow が閉じる。
    private double DistributeCanalDividend(Chokepoint cp, double amount, Power owner)
    {
        if (amount <= 1e-12 || cp.InvestorShares.Count == 0) return 0;
        double totalShare = 0;
        foreach (var kv in cp.InvestorShares) if (kv.Key != owner.Id) totalShare += kv.Value;   // 扼者自身の持分は配当対象外（自国の運河料は上で徴収済）
        if (totalShare <= 1e-9) return 0;
        double paid = 0;
        foreach (var kv in cp.InvestorShares)
        {
            if (kv.Key == owner.Id) continue;
            var inv = Diplomacy.Powers.Find(p => p.Id == kv.Key);
            if (inv?.Nation is not { } iw) continue;   // 抽象/消滅国は配当を受け取れない＝その分は残余として扼者へ
            double d = amount * (kv.Value / totalShare);
            iw.Pay(TreasuryCat.OtherIn, d); iw.CumulativeTradeFlow += d;   // 拠出国＝配当流入（保存）
            paid += d;
        }
        return paid;
    }

    // 港湾の建設（改修2026-06-28）：StepCanals の雛形を Region に移植＝各国の港州を、交易が容量で律速（混雑）している時に**資本家が主導・国家が上乗せ**して拡張する。
    // 保存：投資（資本家貯蓄＋国庫）→ 自国pop へ Size 按分で還流（建設労務＝StepCanals同型・BaseMoney不変）。港料：取扱高×率を地元投資家へ（関税同型の国内移転）。
    private void StepPorts()
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            double natPop = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) natPop += p.Size;
            if (natPop <= 1e-9) continue;
            bool techOk = w.TechLevel >= _coef.PortTechOnset;
            List<Pop>? pops = null;   // 港料徴収の母集団（国ごとに高々1回だけ構築＝複数港での重複確保を避ける）
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || !r.IsPort) continue;
                if (!_calendarFrozen) r.PortCapacity = Math.Max(0, r.PortCapacity * (1 - _coef.PortDecay));   // 減耗（維持に再投資）。★助走中は減耗も拡張も止める＝港湾容量は1783年のまま
                // 港料：取扱高×率を地元投資家へ（荷主popから薄く徴収＝関税同型の国内移転で保存）。港を持つ意味＝施設使用料収入。
                // ★港料は助走中も徴収する＝所得と価格の均衡の一部（止めるのは「能力が育つこと」だけ）。
                if (r.LastPortThroughput > 1e-9 && r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count)
                {
                    if (pops is null) { pops = new List<Pop>(); foreach (var r2 in w.Regions) if (r2.State == RegionState.Active) pops.AddRange(r2.Pops); }
                    CollectPortFee(pops, r.LastPortThroughput * _coef.PortFeeRate, r.Pops[r.InvestorPopIndex]);
                }
                // 拡張は混雑時のみ（律速率<目標＝容量が交易を絞っている）＝採算ゲートで過剰投資しない。技術ゲート（近代港湾）。
                if (_calendarFrozen || !techOk || r.LastTradeThrottle >= _coef.PortCongestionTarget) continue;   // ★助走：港湾も「年月をかけて広がる」能力＝凍結
                if (r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) continue;
                var ip = r.Pops[r.InvestorPopIndex];
                double cCap = _coef.PortInvestFrac * Math.Max(0, ip.Savings);          // 資本家ルート
                double cState = Math.Min(_coef.PortStateFrac * cCap, Math.Max(0, w.Treasury));   // 国家ルート＝資本家投資に上乗せ（公共インフラ＝国家プロジェクト）
                double invest = cCap + cState;
                if (invest <= 1e-9) continue;   // ★判定→引落の順（wave2b是正）：引落してから捨てると微小額が pop へ還流されず消える片側記帳になる
                ip.Savings -= cCap;
                w.Pay(TreasuryCat.OtherOut, -cState);
                foreach (var rr in w.Regions) if (rr.State == RegionState.Active) foreach (var p in rr.Pops) p.Savings += invest * (p.Size / natPop);   // 建設労務で自国popへ＝保存
                r.PortCapacity += invest / _coef.PortBuildCostPerUnit;
            }
        }
    }

    // 海上保険（改修2026-06-28・金融改革③）：商人pop＝引受手。平時は海上取扱高×料率を荷主(全pop)→商人へ（underwriting収入）、封鎖損失時は損失×給付率を商人基金→荷主へ（基金=商人貯蓄の範囲で部分払い＝ロイズの破綻リスク）。
    // すべてpop⇄pop移転＝基礎貨幣不変。多国シム(StepDay四半期)からのみ呼ばれる＝単一国較正は不走行で中立。商人不在の国は保険市場が立たない。
    private void StepMarineInsurance()
    {
        if (_coef.MarinePremiumRate <= 0 && _coef.InsurancePayoutFrac <= 0) return;
        foreach (var sim in Nations)
        {
            var w = sim.World;
            if (w.SelfPower is null) continue;
            double throughput = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) throughput += r.LastPortThroughput;
            if (throughput < 1e-9) continue;
            var allPops = new List<Pop>(); var merchants = new List<Pop>();
            foreach (var r in w.Regions) if (r.State == RegionState.Active)
            {
                foreach (var p in r.Pops) allPops.Add(p);
                if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count) merchants.Add(r.Pops[r.MerchantPopIndex]);
            }
            if (merchants.Count == 0) continue;   // 引受手(商人)が居ない国は保険市場が立たない
            TransferToPops(allPops, merchants, _coef.MarinePremiumRate * throughput);   // 保険料：荷主→商人（平時の収入＝基金の積み増し）
            double risk = w.LastBlockadeRisk;   // 03手順7：旧CommerceRaidFactorの置換＝リスク線積分（取扱高加重平均）
            if (risk > 1e-9)
                TransferToPops(merchants, allPops, _coef.InsurancePayoutFrac * throughput * risk);   // 保険金：商人基金→荷主（基金の範囲で部分払い＝TransferToPopsが from の貯蓄でクランプ）
        }
    }

    // amount を from の各pop（貯蓄比例）から集め、to の各pop（規模比例）へ配る。from の貯蓄で上限クランプ（部分実行）。pop⇄pop＝保存。from/to が重複しても両建てで純保存。
    private static void TransferToPops(List<Pop> from, List<Pop> to, double amount)
    {
        if (amount <= 1e-12 || from.Count == 0 || to.Count == 0) return;
        double fromTotal = 0; foreach (var p in from) fromTotal += Math.Max(0, p.Savings);
        double take = Math.Min(amount, fromTotal);
        if (take <= 1e-12 || fromTotal <= 1e-9) return;
        foreach (var p in from) p.Savings -= take * (Math.Max(0, p.Savings) / fromTotal);
        double toSize = 0; foreach (var p in to) toSize += p.Size;
        if (toSize <= 1e-9) { to[0].Savings += take; return; }   // 規模0の縮退＝先頭へ寄せ保存
        double done = 0;
        for (int i = 0; i < to.Count - 1; i++) { double s = take * (to[i].Size / toSize); to[i].Savings += s; done += s; }
        to[^1].Savings += take - done;   // 端数は最後のpopへ＝厳密保存
    }

    // 港湾料を全pop（荷主）から薄く徴収し港所有者（地元投資家）へ。関税(CollectTariff)と同型＝pop間の国内移転で保存（owner も徴収対象に含むが両建てで純0）。
    // pops＝呼び手（StepPorts）が国ごとに1回だけ構築した active州の全pop（複数港を持つ国での重複確保を避ける）。
    private static void CollectPortFee(List<Pop> pops, double amount, Pop owner)
    {
        double total = 0; foreach (var p in pops) total += Math.Max(0, p.Savings);
        double take = Math.Min(amount, total);
        if (take <= 1e-12 || total <= 1e-9) return;
        foreach (var p in pops) p.Savings -= take * (Math.Max(0, p.Savings) / total);
        owner.Savings += take;
    }

    private Power? ResolveOwner(string host)
    {
        if (string.IsNullOrEmpty(host)) return null;
        foreach (var p in Diplomacy.Powers)
            foreach (var col in p.ColonyList)
                if (col.Name == host) return p;
        // 植民地に無ければ実州(Region)の保有国を解決＝実領土化したhost(例：エジプト実国化後のカイロ＝スエズ運河host)に対応。
        // 征服で州の保有国が変われば host も新本国へ追従する（チョークポイントは地理に固定・所有は政治で動く）。
        foreach (var sim in Nations)
            foreach (var r in sim.World.Regions)
                if (r.State == RegionState.Active && r.Name == host)
                    return sim.World.SelfPower;
        return null;
    }

    /// <summary>★安保doc手順3（2026-08-30）：全国の受脅威を四半期1回計算する。脅威(B←A)＝max over Aの州 rA
    /// [質量(rA)×1/(1+最近接距離(rA,Bの州)/ProximityTensionRange)] を A≠B で合計。質量＝州人口×(1+工業化度)。
    /// 読み手は翌期の Transport.Step（前期値読み＝同一ティック閉路なし・doc 4-1）。係数0では呼ばれない（呼び出し側ゲート）。</summary>
    private void StepSecurityThreat()
    {
        int n = Nations.Count;
        var pts = new List<(double lat, double lon, double mass)>[n];
        for (int i = 0; i < n; i++)
        {
            pts[i] = new List<(double, double, double)>();
            double own = 0;
            foreach (var r in Nations[i].World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                var cc = Climate.CityCoord(r.Name);
                if (cc.Lat == 0 && cc.Lon == 0) continue;
                double mass = r.SumPopulation() * (1.0 + r.TotalIndustrialization);
                pts[i].Add((cc.Lat, cc.Lon, mass));
                own += mass;
            }
            Nations[i].World.SecurityThreatOwnMass = own;
        }
        double scale = System.Math.Max(1.0, _coef.ProximityTensionRange);
        for (int b = 0; b < n; b++)
        {
            double total = 0;
            if (pts[b].Count > 0)
                for (int a = 0; a < n; a++)
                {
                    if (a == b || pts[a].Count == 0) continue;
                    double best = 0;
                    foreach (var ra in pts[a])
                    {
                        double dmin = double.MaxValue;
                        foreach (var rb in pts[b])
                        {
                            double d = Geo.DistanceKm(ra.lat, ra.lon, rb.lat, rb.lon);
                            if (d < dmin) dmin = d;
                        }
                        double v = ra.mass / (1.0 + dmin / scale);
                        if (v > best) best = v;
                    }
                    total += best;
                }
            Nations[b].World.SecurityThreat = total;
        }
    }

    // 外1：交易比重（TradeWeight）を貿易取扱高のシェアから四半期同期（A-中6 で GDP シェアから置き換え＝下の注記）。1789固定値だと1900年に経済覇権となった国を禁輸しても痛みが初期比重のまま＝関税報復/禁輸/国際価格形成の比重が化石化する問題を解く。
    // 既定1.0（点火2026-07-23）＝取扱高シェアで毎四半期更新。0で即 return＝コンストラクタで設定した 0.15+0.10×Industrialization を1789固定維持。抽象標的（Nation=null）は据え置き。
    // A-中6（史実検証2026-07-22）：駆動変数を GDP シェアから**貿易取扱高**（LastExportValue+LastImportValue の EMA）のシェアへ。
    // GDP駆動だと開放度を無視して1913年の米・清・露を過大にする（清は巨大GDPだが広東体制の閉鎖経済）。旧＝1783年の値で604四半期凍結＝
    // 英の山形（1870年前後ピーク24-25%→1913年約14%）も独米の台頭も、禁輸/交戦の痛み・関税報復の観測重みに一切反映されなかった。
    private void UpdateTradeWeights()
    {
        if (_coef.TradeWeightDynamic <= 0) return;
        double total = 0; int count = 0;
        foreach (var p in Diplomacy.Powers)
            if (p.Nation is { } nw)
            {
                double v = Math.Max(0, nw.LastExportValue) + Math.Max(0, nw.LastImportValue);
                nw.TradeValueSmoothed += TradeValueSmoothing * (v - nw.TradeValueSmoothed);   // 数期平滑＝単四半期の禁輸/封鎖スパイクで比重が跳ねない
                total += nw.TradeValueSmoothed; count++;
            }
        if (total <= 1e-9 || count == 0) return;
        foreach (var p in Diplomacy.Powers)
        {
            if (p.Nation is not { } nw) continue;   // 抽象標的＝TradeWeight=0 のまま
            double relative = (nw.TradeValueSmoothed / total) * count;   // 世界平均を1に正規化した相対取扱高シェア
            p.TradeWeight = _coef.TradeWeightBase + _coef.TradeWeightGdpSlope * relative;   // 貿易の大きい国ほど交易比重が増す（禁輸の痛み・国際価格形成の重み）
        }
    }
    private const double TradeValueSmoothing = 0.1;   // 取扱高EMAの時定数（約2.5年）

    // 民族の同化の段（回1-2・2026-09-27＝旧 MigrateInternational の同化の部分だけを残した。世界プール＝ゲーム外から人と持参金が湧く経路は削除）。
    internal void StepAssimilation()   // ★段4（2026-09-09）：internal＝同化の式をテストから単離して検証するため（AccumulatePopConsciousness と同じ扱い）
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            // 民族統一度の同化（毎四半期・歯止め）：時間で1へ回復し、公教育/義務教育で加速＝移民希釈への対抗力（均衡を作る）。
            if (w.EthnicEnabled)
            {
                double assim = _coef.EthnicAssimilation + (w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory ? _coef.AssimilationEducationBonus : 0);
                // B-高3（史実検証2026-07-22）：識字の高い少数派は同化を拒む（ボヘミアのチェコ人＝識字95%超で同化拒否・バデーニ暴動1897。
                // 識字は「自民族語の学校網・出版市場を持てたか」の代理変数）。Resist=0 で×1.0＝厳密恒等。
                // ★逆流（民族復興・段4 2026-09-09・設計メモ§7-7バグa）：AssimilationRevival>0 で床 max(0,·) を外し、
                //   識字が 1/AssimilationRevival を超えた少数派では resist が**負**＝中核度が下がる（自民族語の学校網と
                //   出版市場を持てた少数派は同化を押し返す）。0＝床あり＝厳密恒等。
                bool revive = _coef.AssimilationRevival > 0;
                double rr = revive ? _coef.AssimilationRevival : _coef.AssimilationLiteracyResist;
                foreach (var r in w.Regions) foreach (var p in r.Pops)
                {
                    double resist = rr > 0 ? 1.0 - rr * p.Literacy : 1.0;
                    if (!revive) resist = Math.Max(0.0, resist);   // 旧＝床あり（同化は遅くなるが止まらない）
                    // 逆流の核＝core×(1−core)：中核度1のPOPは1ビットも動かない（**持っていない民族は復興できない**＝
                    // 単一文化国の自国民が自分から離反する暴走を構造で閉じる）。同化側の核は従来どおり (1−core)＝core=0 から立ち上がる。
                    double kernel = resist > 0 ? 1.0 - p.CoreEthnicShare : p.CoreEthnicShare * (1.0 - p.CoreEthnicShare);
                    p.CoreEthnicShare = Math.Clamp(p.CoreEthnicShare + assim * resist * kernel, 0, 1);   // 倍率排除⑧：同化は per-pop で中核割合を1へ寄せる（EthnicUnity は加重平均から創発）
                }
            }
        }
    }

    /// <summary>★受入能力（2026-08-12・作者裁定③「移民が殺到→移民求心力が落ちる→適切な水準に落ち着く」）。
    ///
    /// 旧＝受入上限 cap = 0.012 × 自国人口 が binding で、しかも**率**なので人口が増えれば上限も同率で増える＝正帰還。
    /// 引力(pull)側には混雑項（土地逓減・失業・住居・不穏）が4本あるのに、cap に張り付いた国では inflow が pull に
    /// 依存しないので**混雑がいくら進んでも流入が1人も減らない**＝信号線が上限で切断されていた。
    /// 結果、ネーデルラントは10年で人口2.14倍（年8.3%）・アメリカ2.39倍（年9.6%）と指数で発散し、有限の定常点が存在しない。
    ///
    /// 新＝**binding な制約そのもの**に既存の混雑量を通す。cap ∝ pop × (食料充足×住居充足×土地逓減)^e となり、
    /// 耕地は移民で増えない既存ストックなので、指数 e が十分大きいと1人あたり流入が人口の減少関数になり**水準で止まる**。
    /// 止まる位置を決めるのは新しい数ではなく、既に較正されている耕地・住宅の供給力・食料生産である。
    /// ★回1-2（2026-09-27）：絞られた分は送出州に残る（旧＝世界プールの余剰再配分で他国へ回した。プールは削除）。
    ///
    /// 既定 <c>ImmigrationAbsorbExponent = 2.0</c>＝受入能力が有効。0 なら 1.0 を返す＝**厳密恒等**（掛け算が1回増えるだけで値は不変）。</summary>
    private static double AbsorbCap(Region? r, Coefficients c)
    {
        if (r is null || !(c.ImmigrationAbsorbExponent > 0)) return 1.0;
        double rural = 0;
        foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        double slack = Math.Clamp(r.FoodFill, 0, 1) * Math.Clamp(r.HousingFill, 0, 1) * Territory.LandFactor(r, rural, c);
        if (!(slack > 0)) return 0;   // 否定形＝NaN もここで落ちる（有限値では厳密同値）
        return Math.Pow(slack, c.ImmigrationAbsorbExponent);
    }

    // 国家間移民（回1-2・2026-09-27＝国際移民を閉じる・外部レビューの検討 §10-3 Q3／項目3）：機会（Territory.Opportunity＝土地逓減・住宅摩擦・
    // 賃金プレミアムを含む飽和しない実体量）の差で、各州の大衆（農民・労働者）が豊かな国の最良州へ移る。「アメリカに流すのではなく、アメリカが豊かだから流入する」。
    // ★人はゲームの外から来ない＝送出州から引いた分だけ受入州に着く（旧の世界プール＝世界人口×0.004/四半期を送出国から1人も引かずに配り、
    //   持参金を世界平均の貯蓄から湧かせた経路は削除）。保存：出る側 PopulationDelta−/持参金は CumulativeTradeFlow−、入る側 ＋（両国で閉じる）。
    // 流量はすべて四半期の初めの値で決め、決め終えてから移す。
    //   1 受入国 d＝最良州とその機会 O_d・受入の重み a_d（民族：低統一度は引く・低識字は差別で阻む）・受入の上限 C_d（人口×移民制限法×受入能力）
    //   2 送出州 r（全ての Active 州）＝押し出し P_r＝機会×州の食料充足。差 g_rd＝(O_d−P_r−閾値)×距離の減衰（正のものだけ）・重み w_rd＝g_rd×a_d
    //   3 階級ごとの流出 F＝min(EmigrationRate×最良の行き先の差, 上限×(1＋飢え))×人数×送出の床。**流出の上限はここで1回だけ**＝行き先がいくつあっても同じ
    //     （旧＝「最悪の1州→最良の1国」の1組だけ動かしていた。全組で上限まで抜くと1国が多重に排水される＝プロイセン技術0.38への空洞化を実測したため。
    //     上限を1回だけ掛ければ、1つの州が多くの国へ同時に上限まで流れ出ることが構造的に起きない）。
    //   4 F を w_rd の比で行き先へ割り当てる（休眠の運賃は払える割合を掛ける）。受入国ごとの合計が C_d を超えたら d への分をすべて C_d÷合計で縮める
    //     ＝**縮んだ分は送出州に残る**（消えない・湧かない。移民制限法は「別の国へ回す」でなく「流れを減らす」法＝作者裁定 B3）。
    //   5 移す（MoveMigrants＝決まった人数を1つの階級だけ・属性の運搬は旧のまま）。
    internal void MigrateBetweenNations()   // internal＝番人テストが段を単離して1回だけ呼ぶため（StepAssimilation・MoveMigrants と同じ扱い）
    {
        foreach (var sim0 in Nations) { var w0 = sim0.World; w0.LastImmigration = 0; w0.LastEmigration = 0; w0.LastSpecialistExit = 0; }   // 診断（UI）：四半期の移民収支カウンタ（★段⑤：専門職の流出カウンタも同じ場所で0に＝無条件＝滞留しない）
        if (!(_coef.CrossBorderMigrationByOpportunity > 0)) return;   // 0＝越境移民なし（旧・満足度ベース経路は2026-07-28に係数ごと削除済み）
        int n = Nations.Count;
        // 1. 受入国ごとの最良州・機会・重み・上限
        var dstR = new Region?[n]; var dstO = new double[n]; var dstA = new double[n]; var dstCap = new double[n];
        for (int d = 0; d < n; d++)
        {
            var w = Nations[d].World;
            Region? bR = null; double bO = double.NegativeInfinity;
            // ★非有限の遮断（2026-07-31・Territory 側と同じ流儀）：評価できない州は選ばない
            foreach (var r in w.Regions) if (r.State == RegionState.Active) { double o = Territory.Opportunity(r, _coef); if (double.IsFinite(o) && (bR is null || o > bO)) { bR = r; bO = o; } }
            if (bR is null) continue;
            dstR[d] = bR; dstO[d] = bO;
            // 民族：低統一度は移民を引く（坩堝＝アメリカ）／低教育は差別で移民を阻む（平均識字が閾値未満で減衰＝排斥）。
            dstA[d] = w.EthnicEnabled
                ? (1.0 + _coef.EthnicDraw * (1.0 - w.EthnicUnity)) * Math.Clamp(PoliticalQuantities.AvgLiteracy(w) / _coef.DiscriminationLiteracy, 0, 1)
                : 1.0;
            // 受入の物理上限＝アイルランド脱出級の絶対フロー（ImmigrationReceiveCapFrac/四半期）×移民制限法×受入能力（混雑で縮む＝AbsorbCap）。
            // ★移民制限法（法の棚卸しdoc L2・2026-08-01）：Open＝1.0／Selective＝選別で細る／Closed＝ほぼ閉じる
            //   （史実＝米の排華移民法1882・紳士協約1907・1924移民法／豪の白豪主義）。
            double lawCap = w.ImmigrationRestriction switch
            {
                ImmigrationLaw.Selective => _coef.ImmigrationSelectiveFrac,
                ImmigrationLaw.Closed => _coef.ImmigrationClosedFrac,
                _ => 1.0,
            };
            dstCap[d] = _coef.ImmigrationReceiveCapFrac * w.TotalPopulation * lawCap * AbsorbCap(bR, _coef);
        }
        // 2〜4. 送出州ごと・階級ごとの流量を決める（まだ移さない）
        var plan = new List<(World Src, Region SrcR, int Cls, int D, double Flow)>();
        var toD = new double[n];
        var decay = new double[n]; var gap = new double[n];
        for (int s = 0; s < n; s++)
        {
            var src = Nations[s].World;
            // R3：距離減衰＝遠い行き先ほど差（＝行き先選好＋流量）が細る（国の座標の間＝旧と同じ式）。scale=0 で乗算しない。
            for (int d = 0; d < n; d++)
                decay[d] = d != s && dstR[d] is not null && _coef.MigrationDistanceScale > 0
                    ? 1.0 / (1.0 + Geo.DistanceKm(src.Latitude, src.Longitude, Nations[d].World.Latitude, Nations[d].World.Longitude) / (_coef.MigrationDistanceScale * _coef.EraFactor(src.WorldTechLevel, _coef.EraMigrationScale)))   // ★時代関数化：大西洋横断が6週間→6日
                    : 1.0;
            foreach (var r in src.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double push = Territory.Opportunity(r, _coef) * r.FoodFill;
                if (!double.IsFinite(push)) continue;   // ★非有限の遮断＝評価できない州は送出元にしない
                double gMax = 0, wSum = 0;
                for (int d = 0; d < n; d++)
                {
                    gap[d] = 0;
                    if (d == s || dstR[d] is null) continue;
                    double g = (dstO[d] - push - _coef.CrossBorderOppGapThreshold) * decay[d];
                    if (!(g > 0)) continue;   // 差が0以下の国へは1人も行かない
                    gap[d] = g;
                    if (g > gMax) gMax = g;
                    wSum += g * dstA[d];
                }
                if (!(wSum > 0)) continue;
                foreach (int cls in MigrantClasses)   // 農民・労働者（大衆）が移る
                {
                    var sp = r.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
                    if (sp is null || !(sp.Size >= 1e-9)) continue;   // ★否定形＝NaN の Size も弾く
                    // 危機時の上限緩和：故郷の飢饉(1-FoodFill)に比例して MaxFrac を引き上げ＝アイルランド型の大量脱出。
                    // B-高5（史実検証2026-07-22）：判定を送出**州**の FoodFill へブレンド（RegionBlend=0で国平均のみ）。
                    double effFill = src.FoodFill + _coef.FaminePushRegionBlend * (r.FoodFill - src.FoodFill);
                    double srcFamine = Math.Max(0.0, 1.0 - effFill);
                    double maxFrac = _coef.EmigrationMaxFrac * (1.0 + _coef.FaminePushMaxFracBoost * srcFamine);
                    double flow = Math.Min(_coef.EmigrationRate * gMax, maxFrac) * sp.Size;
                    // 移民の自己制限（07_再設計 §12-1・A4裁定・2026-07-31）：人が減るほど賃金が上がり、縁故と渡航費のネットワークが細る＝流出は自己制限的。
                    //   送出側の残存規模で表す：閾値未満へ近づくほど流出が絞られ、下限では止まる（「最後の住民は故郷を捨てない」）。EmigrationFloorSize=0 で絞らない。
                    if (_coef.EmigrationFloorSize > 0)
                    {
                        double room = (sp.Size - _coef.EmigrationFloorSize) / Math.Max(1e-9, _coef.EmigrationFloorSize * _coef.EmigrationFloorSpan);
                        flow *= Math.Clamp(room, 0, 1);
                    }
                    if (!(flow > 1e-9)) continue;   // ★否定形＝NaN の流量を弾く
                    for (int d = 0; d < n; d++)
                    {
                        if (!(gap[d] > 0)) continue;
                        double f = flow * (gap[d] * dstA[d] / wSum);
                        // R3 貧困の罠（休眠 MigrationFarePerkKm）：運賃を賄える貯蓄がある層だけ動ける。1人当たり貯蓄が運賃の2倍で全員可・運賃以下で0。
                        //   0 の間はこの分岐に入らない（距離計算もしない）。
                        if (_coef.MigrationFarePerkKm > 0)
                        {
                            var dw = Nations[d].World;
                            double fare = _coef.MigrationFarePerkKm * Geo.DistanceKm(src.Latitude, src.Longitude, dw.Latitude, dw.Longitude) / MigrationFareDistanceUnitKm;
                            if (fare > 1e-9)
                            {
                                double perCap = sp.Size > 1e-9 ? sp.Savings / sp.Size : 0;
                                double af = (perCap - fare) / fare;
                                // 安全弁：fare/perCap が非有限（座標NaN／貯蓄NaN）ならゲートを飛ばす（割り当て据え置き）＝NaN を Size に伝播させない。
                                f *= double.IsFinite(af) ? Math.Clamp(af, 0, 1) : 1.0;
                                if (Simulation.ConservationProbe && (!double.IsFinite(fare) || !double.IsFinite(perCap)))
                                    Console.Error.WriteLine($"[R3診断] 非有限：{src.Region.Name}({src.Latitude},{src.Longitude})→{dw.Region.Name}({dw.Latitude},{dw.Longitude}) fare={fare} perCap={perCap} sav={sp.Savings} size={sp.Size}");
                            }
                        }
                        if (!(f > 0)) continue;
                        plan.Add((src, r, cls, d, f)); toD[d] += f;
                    }
                }
            }
        }
        // 4〜5. 受入の上限で縮めてから移す（縮んだ分は送出州に残る）
        foreach (var (src, srcR, cls, d, f) in plan)
        {
            double cap = dstCap[d];
            double scale = toD[d] <= cap ? 1.0 : cap > 0 ? cap / toD[d] : 0.0;   // ★上限が NaN なら0の側へ倒す（fail-closed）
            double flow = f * scale;
            if (!(flow > 1e-9)) continue;
            MoveMigrants(src, Nations[d].World, srcR, dstR[d]!, cls, flow);
        }
    }

    // 越境移民の実転送：大衆（農民・労働者）の1つの階級 cls を srcR→dstR へ、決まった人数 flow だけ移す（流量は MigrateBetweenNations が決める）。
    // 保存＝人口/貨幣台帳が両国で閉じる。internal＝P12残渣②のON経路/保存則テスト用（InternalsVisibleTo・Coef と同じ流儀）。
    internal void MoveMigrants(World src, World dst, Region srcR, Region dstR, int cls, double flow)
    {
        double moved = 0, money = 0;
        // R3 渡航コスト：距離比例の運賃（1人あたり）。MigrationFarePerkKm=0 で fare=0＝以下の運賃ロジックを一切通らず厳密恒等（距離計算もしない）。
        // 払える割合（貧困の罠）は流量を決める側（MigrateBetweenNations の段4）が掛ける。ここは持参金から運賃を引くだけ。
        double fare = 0;
        if (_coef.MigrationFarePerkKm > 0)
            fare = _coef.MigrationFarePerkKm * Geo.DistanceKm(src.Latitude, src.Longitude, dst.Latitude, dst.Longitude) / MigrationFareDistanceUnitKm;
        {   // 運搬（旧の階級ループの本体＝属性を運ぶ行は回1-2 で1行も変えていない）
            var sp = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            var dp = dstR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            if (sp is null || dp is null || !(sp.Size >= 1e-9)) return;   // ★否定形＝NaN の Size も弾く
            if (!(flow > 1e-9)) return;   // ★否定形＝NaN の流量を弾く（旧は NaN 素通しで Size/Savings を直接汚染した）
            double sav = sp.Savings * (flow / sp.Size);   // 持参金＝1人あたり貯蓄ぶん
            // R3 運賃控除：越境するのは運賃を引いた持参金だけ（運賃分は送出経済に残る＝保存則を閉じる）。fare=0 では savArriving=sav で厳密恒等（Math.Max に触れない）。非有限fareは運賃控除せず（安全弁）。
            double savArriving = fare > 1e-9 && double.IsFinite(fare) ? Math.Max(0, sav - fare * flow) : sav;
            double newSize = dp.Size + flow;
            // 移民は家族で動く＝識字・男女比・子供比を携えて受け入れ国に混ざる
            dp.Literacy = (dp.Literacy * dp.Size + sp.Literacy * flow) / newSize;
            dp.FemaleShare = (dp.FemaleShare * dp.Size + sp.FemaleShare * flow) / newSize;
            dp.ChildShare = (dp.ChildShare * dp.Size + sp.ChildShare * flow) / newSize;
            dp.OldShare = (dp.OldShare * dp.Size + sp.OldShare * flow) / newSize;   // 倍率排除⑧：越境移民も老年比率を運搬（OldShare=0の世界では恒等＝較正中立）
            // 人1：闘争度も運搬（識字は上で運搬済・欠落していた Militancy を補完＝人的資本の運搬整合）。既定1.0（#17有効化）で運搬。0で dp 据え置き＝厳密恒等。
            if (_coef.MigrationCarriesHumanCapital > 0) dp.Militancy = Military.BlendCarry(dp.Militancy, dp.Size, sp.Militancy, flow, newSize, clamp: false);   // ★運搬1本化（段1・恒等）
            // ★P12残渣②（原典2026-07-17「移住は加重平均で混合」・0801ログ:79既知の限界の是正）：移民は**出身popの国民意識を持参**し、
            //   到着popとSize加重平均で混合＝NC×Size総量が移動の前後で保存（新規生成も消滅もない・出典popの1人当たり値は不変）。
            //   未初期化(−1)側は各国の実効現在値で評価（CrownParliament の種播きと同じ規約＝跳躍なし）。sp へは書き戻さない（移動に必要な読みだけ）。
            //   MigrantCarriesConsciousness＝★点火済み（2026-08-09 ign_p12＝1.0）。0に戻せばブロック不実行＝旧挙動（移動分が到着国popの値を纏う）とビット恒等。
            if (_coef.MigrantCarriesConsciousness > 1e-12)
            {
                double spNc = sp.NationalConsciousness >= 0 ? sp.NationalConsciousness : EffectiveConsciousness(src, _coef);
                double dpNc = dp.NationalConsciousness >= 0 ? dp.NationalConsciousness : EffectiveConsciousness(dst, _coef);
                dp.NationalConsciousness = (dpNc * dp.Size + spNc * flow) / newSize;
            }
            if (dst.EthnicEnabled) dp.CoreEthnicShare = dp.CoreEthnicShare * dp.Size / newSize;   // 倍率排除⑧：越境移民は受入国で中核外(core=0)＝希釈（EthnicUnity の加重平均が自動で下がる）
            // R3：越境するのは savArriving のみ（運賃 sav−savArriving は sp に残る＝送出経済に留まり保存則が閉じる）。fare=0 で savArriving=sav＝現行と厳密恒等。
            // 個人債務も按分して運ぶ（2026-08-02・既定0で 0×比=0.0 の恒等）。⚠債務は運賃で目減りしない＝債務は移民に付いて回る
            double debtMove = sp.Size > 1e-9 ? sp.Debt * (flow / sp.Size) : 0;
            // ★国債の保有台帳（2026-09-21）：移民の按分ぶんの保有は**発行国（src）の対外債へ振替**＝請求権は src に対するまま、受入国の対外債権になる
            //   （国境を越えると1本のスカラーでは「どの国の国債か」を持てない＝Fiscal.ExportBondHoldings の注）。運賃は金の話＝保有は目減りしない。
            if (sp.Size > 1e-9) Fiscal.ExportBondHoldings(sp, sp.BondHoldings * (flow / sp.Size), src, dst);
            sp.Size -= flow; sp.Savings -= savArriving; sp.Debt -= debtMove;
            dp.Size += flow; dp.Savings += savArriving; dp.Debt += debtMove;
            moved += flow; money += savArriving;
        }
        if (!(moved > 0)) return;
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）：越境の流出/流入
        src.PopulationDelta -= moved; dst.PopulationDelta += moved;          // 人口台帳（両国で相殺）
        src.CumulativeTradeFlow -= money; dst.CumulativeTradeFlow += money;  // 持参金＝対外移転（基礎貨幣の台帳が閉じる）
    }

    private static readonly int[] MigrantClasses = [PopClass.Peasant, PopClass.Worker];   // 農民・労働者
    private static readonly int[] SpecialistClasses = [PopClass.Engineer, PopClass.Researcher];   // ★段④：技師・研究者（インテリ＝希少資源）

    // ★★★ 段④ 招聘（2026-08-17・作者指示「工業化投資は…研究者や技師を増やす補助金として機能させたい」）
    //
    // ⚠⚠ 大衆移民（MigrateBetweenNations）と**主語が逆**である。ここを混ぜてはならない：
    //   ★大衆は【自発的に】動く＝機会の高い方へ流れる（欧州→新大陸）。
    //   ⚠技師は【呼ばれて】動く＝**機会の勾配に逆らって**動く（英→明治日本）。
    //   ★史実：お雇い外国人の俸給は**日本の大臣より高かった**。つまり逆風を買っているのであり、
    //     予算がその逆風の値段を払う。だから「相手が豊かなほど1人が高くつく」が正しい形になる。
    //
    // ⚠ `MigrantClasses` に技師を足す実装は**禁忌**：技師は数が桁違いに少ないので、大衆と同じ流量式では
    //   先進国が一瞬で空になる（既に前例＝MigrateBetweenNations の旧・全組判定でプロイセン技術0.38への空洞化を実測）。
    //
    // ★ 保存（作者裁定2026-08-16「保存です。POPは無からわかない」）：送出国が減り受入国が増える＝ゼロサム。
    //   ★大衆移民も同じくゼロサム（回1-2・2026-09-27＝ゲーム外から湧く世界プールを削除）。
    //   世界全体の技師の総数は**高等教育で育つ分だけ**増える。奪い合いでは増えない。
    //
    // ★★ 自己制限的である（新しい上限は要らない）：A が B から呼ぶ→B の技師が減る→B の賃金が上がる→
    //   割増が上がる→呼びにくくなる。既存の `Territory.Opportunity` 差がそのままブレーキになる。
    //
    // ⚠ 7h（受入国に技師POPが無いと空振りする）は**構造で解決済み**：`Simulation.BuildProvince` は全州に
    //   技師(ClassId 4)を作るので、「技師が一人もいない国」でも Size≈0 の受け皿が既に居る。研究者(13)は
    //   ResearcherFrac>0 の世界にしか居ないが、それは招聘が意味を持つ条件と一致する（居なければ skip）。
    private void RecruitSpecialists()
    {
        if (!(_coef.RecruitCostPerHead > 0)) return;   // ⚠0なら経路に入らない（0除算も踏まない。★2026-08-29点火＝既定100）
        int n = Nations.Count;
        // ★A2-3（2026-09-26・結果を変えない高速化）：送出国ごとの送出州・専門職の数・その州の機会は1回だけ求めて使い回す
        //   （旧＝受入国ごとに全送出国の全州を引き直す）。MoveSpecialists が書くのは送出州と受入州の POP と2国の台帳だけ
        //   ＝招聘が起きた2国だけ引き直せば、旧と同じ値を同じ順で比べる。
        var srcBest = new Region?[n]; var srcPool = new double[n]; var srcOpp = new double[n]; var srcKnown = new bool[n];
        for (int d = 0; d < n; d++)
        {
            var dst = Nations[d].World;
            if (!(dst.RecruitBudget > 0)) continue;   // ★否定形＝NaN の予算も弾く
            // 受入先＝機会が最も高い州（★大衆移民の行き先選定と同じ物差し＝新しいスカラーを置かない）
            Region? dstR = null; double dstO = double.NegativeInfinity;
            foreach (var r in dst.Regions)
                if (r.State == RegionState.Active)
                { double o = Territory.Opportunity(r, _coef); if (double.IsFinite(o) && (dstR is null || o > dstO)) { dstR = r; dstO = o; } }
            if (dstR is null) continue;
            // 送出元＝**その予算で最も多く呼べる国**を1つだけ選ぶ（⚠全組に配ると多重排水になる＝上のプロイセン空洞化の前例と同じ轍）
            int bestS = -1; Region? bestSrcR = null; double bestHeads = 0, bestCost = 0;
            for (int sIdx = 0; sIdx < n; sIdx++)
            {
                if (sIdx == d) continue;
                var src = Nations[sIdx].World;
                double ban = System.Math.Clamp(src.SpecialistExitBan, 0, 1);
                if (ban >= 1.0) continue;   // ★段⑤：完全な流出禁止法＝呼べない（自力で育てるしかない＝近代化の壁）
                // 送出州＝専門職が最も多く居る州（★呼ぶ側は「居る所」から呼ぶ）
                if (!srcKnown[sIdx])
                {
                    Region? bR = null; double pl = 0;
                    foreach (var r in src.Regions)
                    {
                        if (r.State != RegionState.Active) continue;
                        double sz = 0;
                        foreach (var cls in SpecialistClasses)
                        { var q = r.Pops.Find(x => x.ClassId == cls && !x.IsSoldier); if (q is not null && q.Size > 0) sz += q.Size; }
                        if (sz > pl) { pl = sz; bR = r; }
                    }
                    srcBest[sIdx] = bR; srcPool[sIdx] = pl; srcKnown[sIdx] = true;
                    if (bR is not null && pl > 1e-9) srcOpp[sIdx] = Territory.Opportunity(bR, _coef);   // 旧と同じく、送出できる州があるときだけ求める
                }
                Region? srcR = srcBest[sIdx]; double pool = srcPool[sIdx];
                if (srcR is null || !(pool > 1e-9)) continue;
                double srcO = srcOpp[sIdx];
                if (!double.IsFinite(srcO)) continue;
                // ⚠1人あたり実費＝基礎費 ×（相手が豊かなほどの割増）×（遠いほどの割増）
                double premium = 1.0 + System.Math.Max(0, srcO - dstO) / System.Math.Max(1e-9, _coef.RecruitPremiumScale);
                if (_coef.RecruitDistanceScale > 0)
                    premium *= 1.0 + Geo.DistanceKm(src.Latitude, src.Longitude, dst.Latitude, dst.Longitude) / _coef.RecruitDistanceScale;
                double perHead = _coef.RecruitCostPerHead * premium;
                if (!(perHead > 0) || !double.IsFinite(perHead)) continue;
                double heads = System.Math.Min(dst.RecruitBudget / perHead, pool * _coef.RecruitMaxFracOfSource) * (1.0 - ban);
                if (!(heads > 1e-9)) continue;
                if (heads > bestHeads) { bestHeads = heads; bestS = sIdx; bestSrcR = srcR; bestCost = perHead; }
            }
            if (bestS >= 0 && bestSrcR is not null)
            {
                MoveSpecialists(Nations[bestS].World, dst, bestSrcR, dstR, bestHeads, bestCost);
                srcKnown[bestS] = srcKnown[d] = false;   // 州の POP が動いた2国だけ引き直す
            }
        }
        // ⚠予約のゼロ化は**無条件**（EconomyStep:1674 と同じ規律＝条件の内側でゼロ化すると永久滞留する既知のバグ型）。
        //   ★カネは Pay した分しか国庫を出ていないので、捨てても保存は不変＝これが「空振りの返金」である。
        for (int i = 0; i < n; i++) Nations[i].World.RecruitBudget = 0;
    }

    // ★段④の実転送：pool の中で階級ごとに比例配分して heads 人を動かす。保存＝人口/貨幣の台帳が両国で閉じる。
    private void MoveSpecialists(World src, World dst, Region srcR, Region dstR, double heads, double perHead)
    {
        double pool = 0;
        foreach (var cls in SpecialistClasses)
        { var q = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier); if (q is not null && q.Size > 0) pool += q.Size; }
        if (!(pool > 1e-9)) return;
        double moved = 0, money = 0;
        foreach (var cls in SpecialistClasses)
        {
            var sp = srcR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            var dp = dstR.Pops.Find(x => x.ClassId == cls && !x.IsSoldier);
            if (sp is null || dp is null || !(sp.Size > 1e-9)) continue;   // ⚠受け皿が無ければ呼べない（研究者を持たない世界）
            double flow = System.Math.Min(heads * (sp.Size / pool), sp.Size);
            if (!(flow > 1e-9)) continue;
            double sav = sp.Savings * (flow / sp.Size);          // ★持参金（大衆移民と同じ＝自分の貯蓄を持って来る）
            double debtMove = sp.Debt * (flow / sp.Size);
            Fiscal.ExportBondHoldings(sp, sp.BondHoldings * (flow / sp.Size), src, dst);   // ★国債の保有台帳（2026-09-21）：大衆移民と同じ＝発行国の対外債へ振替
            double newSize = dp.Size + flow;
            // ★人的資本を運搬（⚠招聘の要点はまさにこれ＝識字と技能を持って来る）。MoveMigrants（大衆移民）と同じ加重平均。
            dp.Literacy = (dp.Literacy * dp.Size + sp.Literacy * flow) / newSize;
            dp.FemaleShare = (dp.FemaleShare * dp.Size + sp.FemaleShare * flow) / newSize;
            dp.ChildShare = (dp.ChildShare * dp.Size + sp.ChildShare * flow) / newSize;
            dp.OldShare = (dp.OldShare * dp.Size + sp.OldShare * flow) / newSize;
            if (_coef.MigrationCarriesHumanCapital > 0) dp.Militancy = Military.BlendCarry(dp.Militancy, dp.Size, sp.Militancy, flow, newSize, clamp: false);   // ★運搬1本化（段1・恒等）
            if (_coef.MigrantCarriesConsciousness > 1e-12)
            {
                double spNc = sp.NationalConsciousness >= 0 ? sp.NationalConsciousness : EffectiveConsciousness(src, _coef);
                double dpNc = dp.NationalConsciousness >= 0 ? dp.NationalConsciousness : EffectiveConsciousness(dst, _coef);
                dp.NationalConsciousness = (dpNc * dp.Size + spNc * flow) / newSize;
            }
            // ⚠お雇い外国人は**中核民族に数えない**（MoveMigrants の大衆移民と同じ＝希釈）。契約が切れれば帰る人々である。
            if (dst.EthnicEnabled) dp.CoreEthnicShare = dp.CoreEthnicShare * dp.Size / newSize;
            sp.Size -= flow; sp.Savings -= sav; sp.Debt -= debtMove;
            dp.Size += flow; dp.Savings += sav; dp.Debt += debtMove;
            moved += flow; money += sav;
        }
        if (!(moved > 0)) return;
        // ★支度金＝国庫→呼ばれた本人（⚠受入国の内側で保存する。国庫が減り、その分だけ技師の貯蓄が増える）
        double cost = System.Math.Min(perHead * moved, dst.RecruitBudget);
        if (cost > 0)
        {
            dst.Pay(TreasuryCat.Modernization, -cost);
            dst.RecruitBudget -= cost;
            var paid = dstR.Pops.Find(x => x.ClassId == SpecialistClasses[0] && !x.IsSoldier);
            if (paid is not null) paid.Savings += cost;
            else dst.Pay(TreasuryCat.Modernization, cost);   // ⚠受け手が居なければ払わない（保存＝取り消す）
        }
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）
        src.LastSpecialistExit += moved;                                     // ★段⑤：抜かれた側の実測＝流出禁止法の選好の入力（抜かれていない国では法が争点にならない）
        src.PopulationDelta -= moved; dst.PopulationDelta += moved;          // 人口台帳（両国で相殺）
        src.CumulativeTradeFlow -= money; dst.CumulativeTradeFlow += money;  // 持参金＝対外移転（基礎貨幣の台帳が閉じる）
    }

    // P12残渣②：国の実効国民意識（未捕捉は world.json 初期値→係数既定へフォールバック＝CrownParliament:194-195 の初期化と同じ規約）。
    // 移民NC運搬で未初期化(−1)popを評価するためだけの読み（書かない）。
    private static double EffectiveConsciousness(World w, Coefficients c)
    {
        double v = w.NationalConsciousness;
        if (!(v >= 0)) v = w.InitialNationalConsciousness >= 0 ? w.InitialNationalConsciousness : c.NationBuildInitialDefault;
        return v;
    }

    public int Year => Nations.Count > 0 ? Nations[0].World.Year : 0;
}
