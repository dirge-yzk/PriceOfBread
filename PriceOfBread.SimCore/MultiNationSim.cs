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
            var p = new Power { Id = Diplomacy.Powers.Count, Name = t.Name, BaseName = t.Name, Military = t.Military, Gdp = 0, TradeWeight = 0.0, RequiresQuinine = t.Quinine };
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

    // 蜂起と分離独立の建国：Politics が親Worldへ記録した建国要求（蜂起＝PendingRebelRegions・分離独立＝PendingSecessionWars）を四半期末に回収し、
    // 州を物理移転して新国家（新World＋新Power）を立て、内戦タグの戦争を開く（決着は Diplomacy.ResolvePeace の戦後処理の1本）。
    // 保存則＝割譲（Diplomacy.CedeTerritory）と同型の両建て記帳。決定論＝親のindex昇順・乱数種は MixSeed(_seed, 新index) の純関数・乱数不使用。
    // ★2026-09-28（処理の一元化 段2 束3）：旧経路の内戦（Politics.ProcessCivilWar）が決着で積んだ2本の予約（国家分裂 PendingSplitRegions／
    //   単一州の分離戦の勝利 PendingSecessionRegion）は旧経路ごと削除（作者裁定 a・g）＝建国は全て開戦時（戦争の形）。
    // internal＝番人（RevoltIgnitionTests の守備兵の寝返り）が四半期の他の流れを挟まずにこの1段だけを踏む（ReabsorbNations と同じ作法）。
    internal void SpawnSecededNations()
    {
        int count0 = Nations.Count;   // 走査前の国数を固定（追記しても再走査しない）
        for (int i = 0; i < count0; i++)
        {
            var pw = Nations[i].World;
            // ★内戦一元化（2026-08-30）：蜂起＝開戦時の建国。SpawnNation（同じ建国路）＋内戦タグ付き開戦。
            // ★首都の分離独立（2026-09-28 段2 束3・作者裁定 a）：蜂起が首都州に拠ったら、蜂起側が首都州を握り、政府は残る州のうち最大GDPの州へ遷都して戦う
            //   （Territory.TransferRegion の首都を失ったときの遷都＝World.SetCapital の1本）。決着で首都を返すために首都州を記す（World.SecedingRegion）。
            if (pw.PendingRebelRegions is { Count: > 0 } rebelRegs)
            {
                var rebelPolity = pw.PendingRebelPolity;
                pw.PendingRebelRegions = null;
                if (pw.SelfPower is { } parentPw)
                {
                    var aliveR = pw.LeavableRegions(rebelRegs, capital: true);
                    if (aliveR.Count > 0)
                    {
                        var capital = pw.Regions[0];
                        int beforeN = Nations.Count;
                        SpawnNation(Nations[i], aliveR, rebelPolity);
                        if (Nations.Count > beforeN && Nations[^1].World.SelfPower is { } rebelPw)
                        {
                            if (aliveR.Contains(capital))
                            {
                                pw.SecedingRegion = capital;
                                Diplomacy.Log.Add($"{pw.Year} {parentPw.Name}の政府は{pw.Regions[0].Name}へ移った（首都{capital.Name}は{rebelPw.Name}の手に）");
                            }
                            Diplomacy.DeclareCivilWar(parentPw, rebelPw, rebelPolity, _coef);
                        }
                    }
                    // 本拠州が同じ四半期に離れられなくなった（割譲・首都への昇格）＝蜂起は地図の上に立たなかった＝運動の参照を外す（燃え尽きではない）。
                    //   ⚠旧＝参照が残り、次にどこかの再併合が無条件に外すまで「内戦中の運動」として数えられた（分離の共有分母 civilFronts）。
                    //   2026-09-28（処理の一元化 段2 束2）で再併合はその戦争の運動だけを外すようになった＝ここで外す。
                    else if (pw.RevoltingFaction is { } stillborn && rebelRegs.Contains(stillborn.Region)) pw.RevoltingFaction = null;
                }
                continue;   // 同じ親から同四半期に2国は作らない（他レーンの予約は翌期へ持ち越し）
            }
            // ★同時独立（SecessionAsWar・2026-09-06）：分離独立の戦争へ入る州を**州ごとに**建国し分離戦を開く（同じ親から同四半期に複数可＝「バラバラに分裂してもよい」）。
            //   首都（Id0）は出ない・親に州が1つしか残らないなら出ない（首都不変条件）。決着は Diplomacy.ResolvePeace の内戦分岐（CivilWarSecession）。
            if (pw.PendingSecessionWars is { Count: > 0 } secWars)
            {
                pw.PendingSecessionWars = null;
                if (pw.SelfPower is { } parentSec)
                    foreach (var r in pw.LeavableRegions(secWars))
                    {
                        int beforeN = Nations.Count;
                        SpawnNation(Nations[i], [r], null);
                        if (Nations.Count > beforeN && Nations[^1].World.SelfPower is { } secPw)
                            Diplomacy.DeclareCivilWar(parentSec, secPw, pw.Polity, _coef, secession: true);
                    }
            }
        }
    }

    /// <summary>★建国路の1本＝反乱国家の建国（呼び手が直ちに内戦タグの戦争を開く＝講和相当の状態は張らない・ログも開戦側）。
    /// regs[0] が新首都（World.Region=Regions[0] 規約）。polity=null で親の政体を継ぐ（分離独立）／指定で蜂起側の目指す政体を持って出る（政体戦・要求戦）。
    /// ★2026-09-28（処理の一元化 段2 束3）：旧経路の内戦の決着で建てた2本（国家分裂＝多州・単一州の分離戦の勝利）は旧経路ごと削除＝独立の確定は決着（ConfirmIndependence）。</summary>
    private void SpawnNation(Simulation parent, System.Collections.Generic.IReadOnlyList<Region> regs, Polity? polity)
    {
        var reg = regs[0];
        var pw = parent.World;
        var parentPower = pw.SelfPower!;   // SpawnSecededNations 側で非nullゲート済み

        // --- 0) 段5＝守備兵の寝返りは**割合**（作者裁定3・2026-09-08／点火＝2026-09-29 処理の一元化 段3・作者裁定＝推奨）---
        // ⚠旧（休眠の間）＝離脱する州の兵士POPは州ごと新国家へ移る＝**常に全員が寝返る**（「政府が軍を丸ごと敵に渡す」が全建国で起きる＝
        //   首都の分離独立の反乱の勝ち 94.6%〔r95〕）。C案の「守備兵は寝返らない」も常に寝返らない側の絶対規則＝どちらも非歴史的最適解の型。
        //   史実の寝返りは最大の機序（セポイ1857・ペトログラード守備隊1917・キール軍港1918）だが常時ではない
        //   （鎮圧＝パリ1848年6月・ウィーン1848年10月・コミューン1871・ダブリン1916）＝**寝返る割合は守備兵（兵士POP）の闘争度そのもの**
        //   （二値にしない・つまみを持たない＝旧係数 SoldierDefectionByMilitancy は1で焼き込んで削除）。
        // ⚠寝返らなかった分は親の政府の座の兵士POPへ移す＝親に留まる（「離脱州の守備隊のうち忠誠を守った部隊は本国軍へ復帰した」）。
        // ★武器も兵と一緒に（2026-09-29 段3・分子と分母の対称）：出ていく州の武器庫（Region.ArmsStock＝耐久装備の在庫）のうち、
        //   寝返らなかった兵と**同じ割合**を忠誠の兵と同じ州の武器庫へ移す（旧＝兵は割合でも武器庫は州ごと全部が反乱国へ渡った＝
        //   首都の武器庫は全国軍の分〔GarrisonProcurement=0＝調達は首都の1州〕）。移すのは財の量そのもの（出る側から引いた量を受ける側へ足す＝財は湧かず消えない）。
        //   ⚠読むのは在庫にある財だけ（未初期化の財を目標と読むのは一度も開いていない1783年の開幕の武器庫だけ＝EconomyStep.ArmsHeld）。
        //   守備兵がいない州は割合が定まらない＝武器庫は州ごと反乱国へ（兵のいない武器庫は蜂起した側の手に落ちる）。
        // ★忠誠の兵と武器庫は政府について行く（2026-09-29）＝行き先は**政府の座**：首都が出ていくなら遷都先（下の段2 の Territory.TransferRegion が
        //   遷都する州＝選び方の1本 Territory.NextCapital を先に読む）・首都が残るなら首都。
        //   ⚠旧＝親の Regions の順で最初の残る活動州＝首都の分離独立では遷都先と違う州になりえた（英＝マンチェスターへ遷都・兵と武器庫はダブリンへ）＝
        //   軍需の調達は首都の1州だけ＝移した武器庫は読まれず、遷都先は「未初期化は目標」で満額の在庫が湧いた。
        // ⚠**人口台帳の順序に手を入れない**＝下の 1) S/P 確定より前・親の内側だけで動かす。親の総人口も総貨幣も不変で、
        //   確定される移転量 P が「寝返った分だけ」になる（S/P 確定と PopulationDelta -= P の間には1命令も入れない＝Invariants の帯）。
        // ⚠移す動詞は既存の Military.MoveCohort（規模・貯蓄・債務・結晶資産・年齢/男女コホートを既存の作法で運ぶ）＝新しい経路を作らない。
        bool capitalRising = regs.Contains(pw.Regions[0]);   // 首都発の建国（首都の蜂起＝同じ社会の首都の住民）。下の厭戦の継承も読む（段2 の遷都で Regions[0] が変わる前に取る）
        Region? loyalRegion = capitalRising ? Territory.NextCapital(pw, regs) : pw.Regions[0];
        Pop? loyalPop = loyalRegion?.Pops.Find(x => x.IsSoldier);
        if (loyalPop is not null && loyalRegion is not null)
            foreach (var rr in regs)
            {
                var garrison = rr.Pops.Find(x => x.IsSoldier);
                if (garrison is null || !(garrison.Size > 1e-9)) continue;
                double stay = 1.0 - PoliticalQuantities.Clamp01(garrison.Militancy);   // 寝返らない割合＝1−闘争度
                if (!(stay > 0)) continue;   // 否定形＝NaN も全員寝返る側へ倒れる（Math.Clamp は NaN を除去しない）
                Military.MoveCohort(garrison, loyalPop, garrison.Size * stay, _coef);
                if (rr.ArmsStock is { } arms)
                {
                    var kept = loyalRegion.ArmsStock ??= new System.Collections.Generic.Dictionary<int, double>();
                    int[] goods = [.. arms.Keys];   // 鍵の写しを走査（値の書き換えで列挙を壊さない）＝鍵の順は挿入順＝決定論
                    foreach (int g in goods)
                    {
                        double moved = arms[g] * stay;
                        arms[g] -= moved;
                        kept[g] = kept.GetValueOrDefault(g) + moved;
                    }
                }
            }

        // --- 1) 国庫の分与（新国家の運転資金＝移転前の人口比）---
        double P = 0;
        foreach (var rr in regs) P += rr.SumPopulation();
        double parentPop = pw.TotalPopulation;   // 移転前（reg込み）
        double T = Math.Max(0, pw.Treasury) * (parentPop > 1e-9 ? P / parentPop : 0);   // 国庫の人口比分与（新国家の運転資金）

        // --- 2) 親から外す＋付け直し（州の移し替えの1本 Territory.TransferRegion の建国＝新しい国の器は下で州から組む・2026-09-28 処理の一元化 段2 B）---
        //   親の台帳（割譲の送り手と同じ「同一値を両辺に使う」作法）・首都を失えば遷都（首都の分離独立＝政府は残る州の最大GDPの州へ・2026-09-28 段2 束3）・
        //   民族＝旧少数派がこの国の中核になる（CoreEthnicShare の意味は「所属国の中核民族に属する割合」＝反転が要る・敵対検証2026-07-12）・
        //   併合の荷物0（案Y・2026-08-21：分離建国の創設州は新国家の「生まれの人口」）・市場の台帳のリセット・植民地財の空化。
        //   ⚠格と支配度は独立が確定するまで動かさない（RegisterIndependence／内戦の白紙講和で本土へ）＝親が勝てば元の格のまま戻る。
        double railIn = 0, shipIn = 0;   // ★輸送の容量は州と一緒に移る（2026-09-30 (118)）＝新しい国の器を組んだ後に受け取る（下の段5）
        foreach (var rr in regs) { var (rlMoved, shMoved) = Territory.TransferRegion(pw, null, rr, Territory.Rehoming.Seceded); railIn += rlMoved; shipIn += shMoved; }
        // 国庫の分与は移し方の1本で世界の外へ（段4f）＝親の国庫から引き、同じ額を国境の出入りの台帳へ（子は生まれの基礎貨幣 S＋T で基線を取り直す＝下の注）。
        World.Transfer((pw, null), (null, null, null), T, TreasuryCat.OtherOut);
        // （Bank.Gold の親⇄新国家の移し替え2行は 2026-09-28 に削除＝Bank.Gold は導出量で、翌日の Credit.Step が BaseMoney から上書きするまで読み手が無い）

        // --- 3) 新世界の州Id＝0が首都（World.Region=Regions[0] 規約）---
        for (int k = 0; k < regs.Count; k++) regs[k].Id = k;
        // ★建国の新しい首都の武器庫は開いた空で来る＝無い財は0（2026-09-29・遷都の World.SetCapital と同じ規則）＝未初期化を目標と読むのは
        //   一度も開いていない1783年の開幕の武器庫だけ（EconomyStep.ArmsHeld）。首都の蜂起は寝返った分の武器庫（段0）だけを持って立ち、分離独立の州は丸腰から買う。
        reg.ArmsStock ??= new System.Collections.Generic.Dictionary<int, double>();

        // --- 4) 名前・設定（親の現行法・税を継承した NationConfig 経由＝ToScenario と同形の手組み）---
        //   名前（作者承認 2026-09-28）＝分離独立（polity=null）は史実の国名（SecessionNames の対応表）／政体をめぐる内戦は目指す政体の頭書き＋親の国名
        //   （共和制フランス）／同じ政体の要求戦は運動の表示名を頭に（本拠州がこの建国の州の運動＝TriggerFactionRevolt が立てた主体）。表は SecessionNames の1か所。
        string? movement = pw.RevoltingFaction is { } mv && regs.Contains(mv.Region) ? Factions.DisplayName(mv) : null;
        string name = polity is { } pol
            ? SecessionNames.CivilWarName(parentPower.Name, pol, pw.Polity, movement, Diplomacy.Powers)
            : SecessionNames.Resolve(reg.Name, pw.Polity, Diplomacy.Powers);
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
            PoliticsEnabled = true, InitialPolity = polity ?? pw.Polity, FiscalEnabled = true,
            TaxRate = pw.TaxRate, InitialPollTax = pw.PollTax, InitialLandTax = pw.LandTax,   // ⚠建国は1783年の税の段（NationConfig.PollTaxStage／LandTaxStage）を読まない＝親の率のまま（法の継承 World.CopyLawsFrom と同じ値）
            InitialTreasury = T, BaseCoercion = pw.BaseCoercion, EducationLaw = pw.EducationLaw,
            EducationSpending = pw.EducationSpending, ExternalPressure = pw.ExternalPressure,
            InitialInterestRate = pw.InterestRate, InitialTariff = pw.Tariff,
            InitialSuffrage = pw.Suffrage, InitialCensorship = pw.Censorship,
            InitialCornLaws = pw.CornLaws, InitialSerfEmancipation = pw.SerfEmancipation,
            InitialImmigration = pw.ImmigrationRestriction,   // 移民制限法の継承（E7・独立は制度を巻き戻さない＝他の法と同じ規則）
            InitialConscription = pw.ConscriptionLaw,         // 徴兵法の継承（(178)・同じ規則＝この後の World.CopyLawsFrom も同じ値を写す）
            InitialMonetaryLaw = pw.MonetaryLaw,              // 通貨制度の継承（F6・金の親から独立した国は金で始まる＝同じ規則）
        };
        // 新国家は常に効用AI（プレイヤー国から独立してもUI操作者はいない＝NullAgentのゾンビ国家を作らない）
        var sim = new Simulation(scenario, new UtilityAgent()) { TelemetryEnabled = parent.TelemetryEnabled };
        // ★国債の保有台帳（2026-09-21）：出ていく州のPOPが持つ親の国債は**親の対外債へ振替**＝新国家の対外債権になる
        //   （フランクフルト条約1871と同じ扱い＝債務者は親のまま）。新国家の構築（上）は台帳の立っているPOPに起票しない＝ここで0へ落ちる。
        //   ⚠構築の後に置く理由＝持ち手の国（sim.World）が要る。この間に不変条件の検査は走らない（検査は月次の Step の末尾だけ）。
        // ★為替は親の値から始まる（2026-09-30 段3-3）＝生まれた瞬間の通貨は親の通貨（州の貯蓄も物価も親の通貨のまま移る）。国境をまたぐ払いは為替で換算する
        //   （振り替えた保有の債権もここで換算される）＝旧の既定値1（平価）から始めると、銀の親から離れた州の請求権と資本移動が生まれた月に約2倍に跳ねた。以後は自国の月次の為替の式で動く。
        sim.World.ExchangeRate = pw.ExchangeRate;
        foreach (var rr in regs) foreach (var p in rr.Pops) Fiscal.BookForeignDebt(p, p.BondHoldings, pw, sim.World);

        // --- 5) Scenario に載らない現在値の継承（技術・法・行政の到達＝独立は文明を巻き戻さない。正統性は既定＝新政権の蜜月）---
        var nw = sim.World;
        // 保存則の注（#107・2026-07-19）：子の CumulativeTradeFlow は**0のまま**が正しい。ランタイムの per-nation 不変量プローブは
        // 新国家を birth BaseMoney(=S+T) で再基線化するため、子は baseline=S+T・TradeFlow=0 で保存が閉じる（親は −(S+T) を BaseMoney/TradeFlow 両方に記帳）。
        // ここに +S+T を足すと二重計上でプローブが即例外（検証済み）。MultiNationTestの世界合計式がspawnの子baselineを勘定しない側の非対応でありコードは正しい。
        nw.TechLevel = pw.TechLevel; nw.Elite = pw.Elite;
        // ★発見は親から（段2A・独立は文明を巻き戻さない＝技術水準と同じ）。移った州は新しい国の配列を読む（月次の写しを待たない）。
        System.Array.Copy(pw.Discovered, nw.Discovered, System.Math.Min(pw.Discovered.Length, nw.Discovered.Length));
        nw.WorldDiscovered = pw.WorldDiscovered;   // 世界の解禁の和は全ての国で同じ配列
        foreach (var rr in regs) rr.Discovered = nw.Discovered;
        Transport.Receive(nw, railIn, shipIn, pw.ShipSteamShareEma);   // 移った州の鉄道と船（旧＝船腹0・鉄道0で生まれた）
        // ★法の継承は1本（World.CopyLawsFrom＝段の法と税・支出の段は法の軸の表のまま・政策の目標は丸ごと・2026-09-28 処理の一元化 段2 C）。
        //   旧＝ここに World と Policy の対を軸ごとに約40行＝女性の権利は Policy だけ写し漏れた前歴・累進課税／植民地参政権／移民原住民参政権／
        //   技術者流出禁止法／行政費は漏れたままだった。政体の含意する参政権・報道の床は構築時と同じ規則で畳み直す（写した親の値が床を下回らないように）。
        nw.CopyLawsFrom(pw);
        Politics.RaiseFoundingInstitutions(nw);
        nw.AdminPowerSmoothed = pw.AdminPowerSmoothed;
        // #126 制度疲労の継承（2026-07-25・ユーザー裁定「清の近代化が簡単すぎる」）：独立は文明を巻き戻さない（技術・法の継承）が、**制度の病も引き継ぐ**。
        // 旧＝新国家は decay=0 で生まれ、親が清（decay≈0.8）でも技術採用×0.6・改革抵抗×1.4・軍質ペナルティが全て消滅＝
        // 「分裂で身軽に近代化」（上海王国4勝/昆明系4勝の機構的な根）。軍閥期の中国は清の制度的な病（禄位・割拠）を引き継いだ＝継承が史実。
        nw.InstitutionalDecay = pw.InstitutionalDecay;
        nw.InstitutionalDecayCeiling = pw.InstitutionalDecayCeiling;
        nw.BornOfSecession = true;   // P9：独立戦争の危機乗数（レヴェ・アン・マス）の判定用（分離で生まれた国＋交戦中）
        // ★保持力の測定は親の値から始まる（2026-09-29 線2）＝治安の強度（人口あたり）は構築の BaseCoercion と同じく親から＝次の四半期末の Politics.Step が自国の軍と正統性で測り直す。
        //   ⚠旧＝新しい国の測定は0（最初の四半期は保持力0）＝開戦で動員を自国の POP で測る（Diplomacy.DeclareCivilWar）と革命圧力が「保持力0」で満杯になる。
        nw.Coercion = pw.Coercion;
        nw.Quarter = pw.Quarter;

        // --- 6) Power 生成（コンストラクタと同型）＋共有外交への接続 ---
        var power = new Power
        {
            Id = _nextPowerId++, Name = name, BaseName = name, Nation = nw,   // ★Powers.Count でなく単調カウンタ（wave2b是正）＝ReabsorbNations の RemovePower で欠番が出ても既存Idと衝突しない
            Military = parentPower.Military * (parentPop > 1e-9 ? P / parentPop : 0),   // 初期表示値のみ（次の Diplomacy.Step が兵士POPから再導出＝実体は移転した兵士）
            Gdp = nw.Gdp, TradeWeight = DefaultTradeWeightBase,
            Culture = reg.Culture, Religion = reg.Religion,   // 次の毎月同期まで州ラベルで代用
            // ★厭戦の継承（2026-09-29）：首都発の建国は親の厭戦を継ぐ（厭戦は前の戦争の疲れが住民に残った量＝首都の蜂起は同じ社会の、いちばん疲れた首都の住民が立てる）。
            //   ⚠旧＝新しい国は厭戦0から＝首都発の内戦で政府（持ち越しの厭戦 中央0.52）だけが先に崩壊の線に届いた（反乱の勝ち 93.8%・(97) §8）。
            // ★地方の建国（分離独立・地方の組織運動の蜂起）は0から（2026-09-29 線2＝(100) の申し送り4 (b)・推奨で進めた）＝親の戦争の疲れは親の政府と首都の住民のもの
            //   ＝(100) で全ての建国に継がせた結果、疲れ切った親から離れた州が生まれた月に講和の線を越えた（分離独立の戦争の月数の中央 18→1）。
            //   史実の独立戦争は年単位（ギリシア1821-29・スペイン領アメリカ1810-25・ハンガリー1848-49・合衆国1861-65）。
            WarExhaustion = capitalRising ? parentPower.WarExhaustion : 0,
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
        foreach (var n in Nations) n.World.RebaseOpeningMoney();   // ★(220) 開幕の貨幣を史実の比へ（係数 InitMoneyMode>0 かつデータ>0 の国だけ）＝減らした貨幣の基準線は下の RebaselineInvariants で取り直す
        WarmupTech(_coef.StartWarmupTechIterations);
        // ★開幕の既知の集合（(169)）＝データ（NationConfig.KnownDiscoveries＝world.json・1783年より前に史実でその国が使っていた発見）＝構築で置いた（Simulation のコンストラクタ）。
        //   ここでは世界の解禁の和（需要の門）を配るだけ。判定は最初の四半期の終わりから（他の四半期と同じ1本）。
        //   旧（段2A・2026-10-01〜(167)）＝助走の終わりの盤面で1度だけ点数を判定した＝1780年代の行は技術力が最大＝必要性が30に届く国が開幕から持った
        //   （(165)＝主要国の全てが1784年に蒸気機関・データの史実〔英だけ〕と食い違った）。
        ShareWorldDiscovered();
        RebaselineInvariants();
    }
    private bool _startWarmupDone;

    /// <summary>★発見の段（段2A・2026-10-01・設計 §1A-2・§1A-7）＝四半期末の共有層。
    /// 接触（行ごと）＝①取引：その行を解禁済みの国から、まだの自国が買った運び手の財の額 ÷ 世界のその運び手の財の取引額の和（思想の伝播 StepDiffusion と同じ「世界の取引に占める割合」＝小国を過大に見ない）
    /// ＋②技師：この四半期に解禁済みの国から呼んだ技師・研究者 ÷ 自国の技師・研究者（招聘 RecruitSpecialists が記帳）。
    /// 全ての国の接触を先に求めてから判定する（判定の順で他国の接触が変わらない）。記帳は毎四半期0へ戻す（助走も）。</summary>
    private void StepDiscoveries(bool evaluate)
    {
        if (evaluate)
        {
            int nr = Catalog.Discoveries.Length, n = Nations.Count;
            var gf = Market.DiscoveryGoodFlowQ;
            var carriers = Market.DiscoveryCarriers(Nations);
            var contacts = new double[n][];
            for (int i = 0; i < n; i++)
            {
                var w = Nations[i].World;
                var ct = new double[nr];
                if (w.DiscoveryImportQ is { } dq && gf is not null)
                    for (int r = 0; r < nr && r < dq.Length; r++)
                    {
                        if (!(dq[r] > 0)) continue;
                        double den = 0; foreach (int g in carriers[r]) if (g < gf.Length) den += gf[g];
                        if (den > 1e-12) ct[r] += dq[r] / den;
                    }
                if (_recruitLog is { Count: > 0 } log)
                {
                    double pool = 0;
                    foreach (var reg in w.Regions)
                    {
                        if (reg.State != RegionState.Active) continue;
                        foreach (var cls in SpecialistClasses) { var q = reg.PopOf(cls); if (q is not null && q.Size > 0) pool += q.Size; }
                    }
                    if (pool > 1e-9)
                        foreach (var (src, dst, heads) in log)
                        {
                            if (!ReferenceEquals(dst, w)) continue;
                            for (int r = 0; r < nr; r++)
                                if (Discovery.Known(src.Discovered, r) && !Discovery.Known(w.Discovered, r)) ct[r] += heads / pool;
                        }
                }
                contacts[i] = ct;
            }
            // ★(159) 平滑化した値段（作者裁定 Q12）を全ての国で先に1歩進めてから判定する（判定は読むだけ＝書き手は判定の段の頭の1か所）。
            //   ★(165) 世界の平滑化した値段（導出量）も同じ段の頭で1回だけ求めて全ての国の判定へ渡す。
            var market = AdvanceDiscoveryPrices();
            for (int i = 0; i < n; i++) Discovery.Step(Nations[i].World, new Discovery.WorldView(contacts[i], null, market), _coef);
            ShareWorldDiscovered();
        }
        foreach (var sim in Nations) { sim.World.DiscoveryImportQ = null; sim.World.DiscoveryLossQ = null; }   // 損耗の記帳（(153)）も判定の後（助走は判定しない＝捨てるだけ）
        Market.DiscoveryGoodFlowQ = null;
        _recruitLog?.Clear();
    }
    /// <summary>判定の段の頭＝全ての国の平滑化した値段を1歩進め（<see cref="Discovery.AdvancePrice"/>・作者裁定 Q12）、世界の市場を返す（★(165)(169)・<see cref="Discovery.WorldPrices"/>＝導出量）。</summary>
    private Discovery.WorldMarket AdvanceDiscoveryPrices()
    {
        var worlds = new World[Nations.Count];
        var obs = new (double[] Inst, double[] Sold)[Nations.Count];
        double frontier = 0;   // ★(169) 運賃革命の倍率（清算と同じ1本＝前線の技術水準＝全ての国の技術水準の最大）
        for (int i = 0; i < Nations.Count; i++) { worlds[i] = Nations[i].World; obs[i] = Discovery.AdvancePrice(worlds[i], _coef); if (worlds[i].TechLevel > frontier) frontier = worlds[i].TechLevel; }
        return Discovery.WorldPrices(worlds, obs, InternationalMarket.FreightRevolution(frontier, _coef));
    }
    /// <summary>世界の解禁の和（需要の門が読む）を書き直して全ての国へ配る（同じ配列＝建国した国も親と同じものを参照）。</summary>
    private void ShareWorldDiscovered()
    {
        _worldDiscovered ??= new bool[Catalog.Discoveries.Length];
        Discovery.UnionInto(Nations.Select(s => s.World), _worldDiscovered);
        foreach (var sim in Nations) sim.World.WorldDiscovered = _worldDiscovered;
    }
    private bool[]? _worldDiscovered;   // ⚠セーブに載る（全ての国の World と同じ配列＝参照は1つ）

    // （人件費の世界の平均 WorldLaborWage は 2026-10-02 (159) に削除＝発見の必要性は生み出す物の値段だけを読む〔作者 2026-10-02〕）
    /// <summary>この四半期の招聘（送り手・受け手・人数）＝発見の段が読んで空にする。作業領域（同じ四半期末の中で書いて読む＝セーブを挟まない）。</summary>
    [System.NonSerialized] private List<(World Src, World Dst, double Heads)>? _recruitLog;

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
    /// UI が助走中の価格を標本に採るために使う（生産タブの価格グラフ・2026-08-10）。力学には触れない。</para>
    /// <para>★**助走は国庫を動かさない**（2026-09-29）＝各四半期の終わりに国庫を助走の前の値へ戻し、台帳も捨てる（<see cref="World.RebaseTreasury"/>＝暦の外で動いた金は歴史に属さない）
    /// ＝国債（Fiscal の3つの門）・人口・資本と同じ「年月が経って積み上がる量」。税と支出の流れは止めない（価格の均衡の一部）。
    /// 旧＝開幕の歳入が請求の約2〜4倍（歳入 年GDP の15〜50%・請求 6〜16%＝初期の人頭税の最上段と所得税10%）の余剰を5年積み、開幕の国庫が年GDP の約1〜2
    /// （英 0.79→0.97・仏 0.26→0.83・清 0.01→1.19・日 0.23→0.99）＝史実の国庫（年GDP の数%・普の戦時財宝 約0.3 は例外）の数十倍だった。
    /// 四半期ごとに戻す＝助走の国庫が潤沢線（<see cref="UtilityAgent.OpulenceLineOf"/>）へ届かない＝開幕と同じ予算の形のまま価格が落ち着く（終わりに1度だけ戻すと、
    /// 助走の後半に潤沢の支出が乗った均衡から開幕に支出が抜ける）。戻した分は基礎貨幣から消えるので保存則の基準線を取り直す（<see cref="RebaselineInvariants"/>）。</para></summary>
    public void WarmupEconomy(int quarters, Action? onQuarter = null)
    {
        if (quarters <= 0) return;   // 助走なし＝素の構築直後の盤面と厳密恒等（フラグの上げ下げすらしない）
        var prevPolitics = Nations.Select(n => n.World.PoliticsEnabled).ToArray();
        var treasury0 = Nations.Select(n => n.World.Treasury).ToArray();   // 助走の前の国庫（world.json の初期値）
        bool prevDiplomacy = DiplomacyFrozen;
        foreach (var n in Nations) { n.World.PoliticsEnabled = false; n.World.WarmupFrozen = true; }
        DiplomacyFrozen = true;
        _calendarFrozen = true;
        for (int q = 0; q < quarters; q++)
        {
            StepQuarter();
            for (int i = 0; i < treasury0.Length && i < Nations.Count; i++) Nations[i].World.RebaseTreasury(treasury0[i]);
            RebaselineInvariants();
            onQuarter?.Invoke();
        }
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
    private double ProbeWorldW() { double s = -Diplomacy.CumulativeFxResidual; foreach (var n in Nations) { var w = n.World; s += w.BaseMoney - w.CumulativeGoldMinted - w.CumulativeFiatIssued; } return s; }   // ★段3-1（A7）：為替の換算の台帳を差し引く
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
            Cost[CostLedger.SharedNationQuarters] += Nations.Count;   // 計算量の台帳（診断）：四半期末の共有層は国×四半期に比例（国力・脅威・移民・国際金融ほか）
            foreach (var n in Nations) NationalScore.StepStanding(n.World);   // 国力の在庫を1期ぶん寄せる（2026-08-02）＝序列は在庫で決まる。外交(ComputeRanks)より前
            StepSecurityThreat();   // ★安全保障の不安（(155) 作り直し）：読み手＝AI の軍事費の段・徴兵法の目標（陸軍の不安）・戦略鉄道（旧＝鉄道の係数 ThreatRailTargetWeight が0なら走らなかった＝鉄道だけの読み手）
            UpdateTradeWeights(); ProbeDiff("交易比重");                // 外1：交易比重を貿易取扱高のシェア（LastExportValue+LastImportValue の EMA）から四半期同期（TradeWeightDynamic=0 で1789固定値を維持＝恒等）
            if (!DiplomacyFrozen)
            {
                Diplomacy.BeginQuarterSyncWindow();   // ★同じ瞬間の3回＝GDP の同期を使い回す窓（2026-09-30 計算量の予算・恒等＝Diplomacy.SyncGdp）
                for (int m = 0; m < Calendar.MonthsPerQuarter; m++) { Diplomacy.Step(_coef, Rng, _quarter * Calendar.MonthsPerQuarter + m); ProbeDiff($"外交m{m}"); }   // 共有外交を月次3回（戦争の月次カデンス）。凍結中＝経済助走で戦争・緊張を進めない
                Diplomacy.EndQuarterSyncWindow();
            }
            StepCanals(); ProbeDiff("運河");   // 海峡・運河：扼者を解決し、運河は host 保有国の資本家が建設（§B）
            // ★助走中は巨大建造物を進めない（2026-08-10 追加裁定＝能力の凍結）。着工も進捗も完成効果（工業化度・耕地・送電容量）も
            //   「年月をかけて建つ」もの＝暦の進まない助走で建ててはいけない。1783年は技術ゲートでほぼ全件が未解禁＝需要への影響も無い。
            if (!_calendarFrozen) { Megaprojects.Step(Diplomacy, Nations, _coef); ProbeDiff("巨大建造物"); }   // 巨大建造物：保有国を解決し国庫から資材＋労働を買って建て、完成したら州へ恒久効果を貼る（2026-07-28）
            { long t2 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Market.Step(Nations, _coef, firstClearingOfQuarter: !(_coef.TradeMonthlyClearing > 0)); if (TimeOn) System.Threading.Interlocked.Add(ref TicksMarket, System.Diagnostics.Stopwatch.GetTimestamp() - t2); } ProbeDiff("国際市場");          // 国際市場：州と州の二国間清算（実経路・比較優位）。月次清算ONでは四半期の第3清算＝累計を続ける（リセットしない）
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
            StepDiscoveries(evaluate: !_calendarFrozen);   // ★段2A：発見（伝わりの接触＝この四半期の取引と技師の移動→国ごとに点数を求めて100に届いた行を解禁）。助走は記帳を捨てるだけ
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
                    double gap = PoliticalQuantities.Clamp01((frontier - w.TechLevel) / Math.Max(1e-9, _coef.ExternalPressureGapScale));
                    w.ExternalPressure = gap * (coastal ? 1.0 : _coef.ExternalPressureInlandFactor);
                }
            // 政16（世界工業化の先端状態量・スカフォールド完成）：各国 MeanIndustrialization の最大値＝「世界最先端の工業化度」を
            // 全 World.IndustrialFrontier へ注入（WorldTechLevel と同型）。Ideology の発展度（ファシズムの支持の式・発見パネルのホバー）が
            // 代理(WorldTechLevel との max)でなくこれを参照する（思想の門は 2026-10-01 (152) から発見の表の思想の行＝発展度の傾斜は削除）。
            double indFrontier = 0;
            foreach (var n in Nations) indFrontier = Math.Max(indFrontier, n.World.MeanIndustrialization);
            foreach (var n in Nations) n.World.IndustrialFrontier = indFrontier;
            // ★銀の世界価格（銀本位doc G6・2026-08-13）：銀本位国の平価は「銀の価値」に縛られる。World は国際市場を見られないので、
            //   ここで各国へ配る（IndustrialFrontier と同型の共有スカラー）。⚠新しい価格を作らない——既存の世界価格をそのまま渡すだけ。
            //   SilverStandardGate=0（旧既定・点火1.0=2026-08-21銀本位ラウンド）では World 側が読まない＝注入しても挙動中立。
            // ★銀と金の比（(210)・00一覧 §0-A の 10＝金への入力を同じ州の比の世界で＝表示用の統計に乗せない）：同一市場内の 銀/金鉱 比の全 Active 州の対数平均 R
            //   を RefPrice 比で正規化（比は PriceLevel と ExchangeRate が約分される＝単位無し・クランプ不要）＝World.SilverGoldRatio（通貨制度の選好の入力）。
            //   器 SilverParityFromRatio（R1・休眠）は同じ R を銀本位国の平価の元へ使う＝走査はこの1本（旧＝器の枝の中だけで数えた）。
            //   ⚠平均は**対数平均**・両市場が生きている州だけ（レビュー是正2026-09-07）：局所価格は rp×[PriceFloorFactor,PriceCeilFactor] の帯を持つので
            //   片側が死んだ州（需要も産出も無い＝MarketFlow が価格を動かさない）の凍った比が最大10⁴＝算術平均を1州で独占する（seed1 実測＝フォートユーコンの
            //   金鉱 0.0088 が R を 1.18→1.34 へ押した・中央値は 0.99〜1.02）。対数平均なら1州の票は ln(天井/床)/N で有界＝クランプも新定数も要らない。
            //   走査順＝Nations 順×Regions 順（決定論）。有効な州が無い／比が不正なら 1（平価どおり）・R1 は旧統計へ倒す。
            if (_coef.SilverStandardGate > 0 && Catalog.SilverId < Market.Price.Count)
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
                bool ratioOk = ratioCnt > 0 && meanRatio > 1e-9 && refRatio > 1e-9;
                double rel = ratioOk ? meanRatio / refRatio : 1.0;
                // ★銀の世界価格（銀本位doc G6・2026-08-13）：銀本位国の平価は「銀の価値」に縛られる。World は国際市場を見られないので、
                //   ここで各国へ配る（IndustrialFrontier と同型の共有スカラー）。⚠新しい価格を作らない——既存の世界価格をそのまま渡すだけ。
                // ★器 SilverParityFromRatio（2026-09-07・休眠既定0）：Market.Price[銀] は「局所価格×ExchangeRate」の平均＝銀本位国の E がこの平均から
                //   作られ、平均は E を含む（E→平均→E の正帰還・silverprobe で天井近傍に乗数2.8）。1＝上の R を RefPrices[銀] の単位へ戻して平価の元にする。0（既定）＝旧統計。
                double silverWorld = Market.Price[Catalog.SilverId];
                if (_coef.SilverParityFromRatio > 0 && ratioOk) silverWorld = rel * Catalog.RefPrices[Catalog.SilverId];
                foreach (var n in Nations) { n.World.WorldSilverPrice = silverWorld; n.World.SilverGoldRatio = rel; }
            }
            // ★交易網の引力（タスク#75再開・2026-08-21・(210) で組ごとの相手へ）：TradeGoldPull_i ＝（自国の取引の相手の本位の差）×（自国の貿易開放度）。
            //   相手の本位の差＝World.TradePartnerStandardBalance（BilateralMarket が越境の成約ごとに相手の本位の符号で積む＝金＋1・銀−1・不換0・−1..1）。
            //   開放度＝(輸出+輸入)×年換算/年GDP（MajorsTradeGdp と同一の物差し）。
            //   ★(210) 外部レビューの検討 §10-11-6 F6 の表の行（金本位の波は組ごとの相手で作り直す＝AI が要約した行・作者の原文ではない）と §10-11-7 の行（比が平価から外れている間だけ取引の過半の相手へ揃える＝AI の推奨・作者「質問を確認しました。推奨でよいと思います。」で承認）：
            //   旧＝世界共通の金建て比率×自国の開放度（ペア別の行列を持たないための近似）＝自分の相手が銀の国ばかりでも世界の金の割合で引かれた（全ての国に同じ向き）。
            //   新＝相手が金の過半なら＋・銀の過半なら−＝貿易の大きい国が金へ移るほどその相手の差が金へ傾く（英→独の正帰還＝雪崩）・相手が銀の国（アジア内陸）は銀圏に残る。
            //   比が平価から外れている大きさは読み手（PolicyPreferences）が掛ける。⚠ 今の世界では同じ州の銀と金の比が 0.96〜1.13＝|1−比| が小さく引力はほぼ0（(210) の10種で移った四半期の中央 −0.055）
            //   ＝上の正帰還（雪崩）は比が動く世界でだけ起きる＝今は起きない（(212) 反証役 vs1#6）。⚠国名も年号も書かない。
            //   MonetaryPeerPullScale=0（旧既定・点火5.0=2026-08-21銀本位ラウンド）では注入自体をスキップ（TradeGoldPull は0のまま）。
            if (_coef.MonetaryPeerPullScale > 0)
            {
                Span<double> natTrade = Nations.Count <= 128 ? stackalloc double[Nations.Count] : new double[Nations.Count];
                for (int i = 0; i < Nations.Count; i++)
                {
                    var wn = Nations[i].World;
                    double t = 0;
                    if (wn.ExportsByGood is { } eg) foreach (var v in eg) if (v > 0) t += v;
                    if (wn.ImportsByGood is { } ig) foreach (var v in ig) if (v > 0) t += v;
                    if (!(t >= 0)) t = 0;   // NaNは0へ倒す（否定形）
                    natTrade[i] = t;
                }
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
                    double bal = wn.TradePartnerStandardBalance;
                    if (!(bal >= -1 && bal <= 1)) bal = 0;   // 否定形＝NaN は相手の過半なしへ倒す
                    wn.TradeGoldPull = bal * Math.Min(1.0, rel);
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
            // ★段3-3（C13・2026-09-30）：貸し手は投資家の述語の1本（CapitalMarket.Investors＝国際金融の払い手・受け手と同じ集合）・余力は**金建て**で集めて借り手の通貨へ
            //   （旧＝近代エリート〔活きた州〕の貯蓄を国をまたいで名目のまま足した）。
            {
                Span<double> inv = Nations.Count <= 128 ? stackalloc double[Nations.Count] : new double[Nations.Count];
                double worldCapSavings = 0;
                for (int k = 0; k < Nations.Count; k++) { inv[k] = CapitalMarket.InvestorSavings(Nations[k].World) * Nations[k].World.FxRate; worldCapSavings += inv[k]; }
                for (int k = 0; k < Nations.Count; k++)   // 各国の余力＝世界の資本家貯蓄から自国ぶんを除いた合計（他国だけが自国債を引き受けられる）
                    Nations[k].World.ForeignBondInvestorPool = Math.Max(0, worldCapSavings - inv[k]) / Nations[k].World.FxRate;
            }
            // ★§1-4（ForeignLendingDebits・2026-08-30）：対外借入元本の貸し手側引き落とし。借り手は Fiscal で +CumulativeTradeFlow 済み＝
            //   ここで他国資本家の貯蓄から比例控除し、貸し手国の CumulativeTradeFlow を同額 − する＝両建てで世界の貨幣創造を閉じる。
            //   比例の物差し＝国内債と同じ max(0,Savings) 比（Fiscal の domestic 枝と同型）。★2026-09-26：旗 ForeignLendingDebits を焼き込み（x-01）。
            //   ★支払いの一元化 段2-2（A5・2026-09-29）：貸し手の払いは**貸し手の貯蓄まで**（移し方の1本 World.Transfer＝払い手は他国の資本家・受け手は世界の外）。
            //   払えなかった分（貸し手不在・月初のプールが四半期末までに痩せた）は**その借入が立たなかった**＝借り手の国債・対外債を同じ額だけ減らし、
            //   受け取った額を国庫から世界の外へ戻す（Fiscal の借入の入金の逆＝量を減らす）。旧＝貸し手不在なら控除なし（借り手の受領だけが残る湧き口）・
            //   貸し手の貯蓄を負へ押した（他人から取る）。
            // ★段3-3（2026-09-30）：①貸し手は投資家の述語の1本（C13）②**払った額そのもの**を貸し手の国の対外債権へ（払った比＝債権の比・旧＝翌四半期の外債レグが
            //   その時の投資家の貯蓄の比で配った）③借りた額は金の値打ちで貸し手の通貨へ換算（貿易と同じ為替・差は為替の換算の台帳）。払えなかった分は国債・対外債・
            //   繰上げ済みの印（PrevForeignDebt＝借入の月に Fiscal.Borrow が上げた）から同じ額を戻す。
            {
                List<Pop>[]? lenders = null;   // 各国の貸し手（投資家）＝四半期に1回だけ組む（POP の顔ぶれはこのループで変わらない）
                double fxLend = 0;
                for (int bi = 0; bi < Nations.Count; bi++)
                {
                    var bw = Nations[bi].World;
                    double owe = bw.PendingForeignBorrow;
                    if (!(owe > 1e-9)) { bw.PendingForeignBorrow = 0; continue; }
                    bw.PendingForeignBorrow = 0;
                    if (lenders is null)
                    {
                        lenders = new List<Pop>[Nations.Count];
                        for (int li = 0; li < Nations.Count; li++) lenders[li] = CapitalMarket.Investors(Nations[li].World);
                    }
                    var heldBy = new double[Nations.Count]; double lendTotal = 0;   // 貸し手の国の余力（金建て）
                    for (int li = 0; li < Nations.Count; li++)
                    {
                        if (li == bi) continue;
                        foreach (var p2 in lenders[li]) heldBy[li] += Math.Max(0, p2.Savings);
                        heldBy[li] *= Nations[li].World.FxRate;
                        lendTotal += heldBy[li];
                    }
                    double oweGold = owe * bw.FxRate, lent = 0, paid = 0;   // lent＝借り手の通貨で届いた額・paid＝貸し手が払った額（各自の通貨）
                    if (lendTotal > 1e-9)
                        for (int li = 0; li < Nations.Count; li++)
                            if (li != bi && heldBy[li] > 0)
                            {
                                var lw = Nations[li].World;
                                double k = World.Transfer((lw, lenders[li]), (null, null, null), oweGold * (heldBy[li] / lendTotal) / lw.FxRate);   // 貸し手国の台帳＝流出（借り手の +流入 と対）
                                lw.ForeignBondClaims += k;
                                paid += k; lent += k * lw.FxRate / bw.FxRate;
                            }
                    double unmet = owe - lent;
                    double back = unmet > 0 ? unmet : 0;
                    if (back > 0)
                    {
                        bw.GovDebt -= back; bw.ForeignDebt -= back; bw.PrevForeignDebt -= back;
                        World.Transfer((bw, null), (null, null, null), back, TreasuryCat.Borrow);
                    }
                    fxLend += (owe - back) - paid;   // 借り手が受け取ったまま残る額（借り手の通貨）−貸し手が払った額
                }
                Diplomacy.CumulativeFxResidual += fxLend;
            }
            ReabsorbNations();       // ★内戦一元化：内戦決着の再統合（州の返還・国家除去）。SpawnSecededNations の前＝同四半期の建国と衝突しない
            SpawnSecededNations();   // 建国キューの回収（分離戦勝利の州→新国家）。Nations走査の外＝リスト追記が安全・新国家は次の四半期から鼓動
            // ★港の配線を盤面から張り直す（毎四半期・無状態＝2026-09-21 案1。旧＝署名で条件付き・2026-08-13）。
            //   ★置き場所＝その四半期の領土の動き（講和の割譲・再併合・建国）が全て済んだ後（2026-09-28 処理の一元化 段2 B）。
            //   ⚠旧＝国際市場の直後（再併合と建国の前）＝返った州と建った国の配線が1四半期遅れた。配線を読むのは国際市場の清算（BilateralMarket）だけ
            //   ＝旧の位置からここまでの間（港湾・海上保険・移民・国際金融・疫病・天候・伝播）に読み手は無い＝動いたのは再併合と建国の四半期だけ。
            RewirePorts();
            StepDiffusion();         // ★発見・段7：思想の伝播（貿易路×メディア・前四半期読み＝閉路なし）。DiffusionWeight=0で即return＝恒等
            // 助走中は共有層の四半期カウンタも止める（2026-08-10）＝本編は必ず _quarter=0（外交の月次索引 _quarter*3+m の起点）から始まる。
            // 助走では DiplomacyFrozen＝Diplomacy.Step を1度も呼ばないので、止めても飛ばした索引は存在しない。
            if (!_calendarFrozen) _quarter++;
        }
        _dayInQuarter = (_dayInQuarter + 1) % Calendar.DaysPerQuarter;
        return quarterDone;
    }

    /// <summary>★内戦一元化（CivilWarAsWar・2026-08-30）：内戦決着の再統合＝SpawnNation の厳密な逆。
    /// 政府勝利＝反乱国の全州を親へ返還し国家を除去／反乱勝利＝同じ返還の後に政体変更の実行部（Politics.SettleRevolt＝旧経路の内戦の決着と同じ1本・革命の成就）。
    /// 保存則＝反乱国の基礎貨幣（S=貯蓄和＋T=国庫−L=与信）と人口 P を親の台帳へ（貸借の仕分けは ReabsorbNations の★2026-09-22）。民族反転（建国時の 1−x）も戻す。
    /// 白紙講和はここへ来ない＝分断がそのまま残る（台湾・南北型）。</summary>
    /// <summary>★発見・段7（2026-08-31）：思想と制度の伝播。到達(A→B)＝貿易額(A,B)の世界の貿易に占める割合×メディアA×メディアB×**前四半期の**放射A
    /// （疫病の伝播と同型＝各到達国が新たな発信源・再放射。前四半期スナップショット＝同期読みの閉路禁止・原典§5-5）。
    /// メディア＝NewsFill×識字＋RadioFill（pop加重・全て既存量）。ペア行列は四半期で消費しゼロ化。
    /// ★2026-10-01 (152)：到達の欄は発見の表の思想と制度の行（<see cref="Discovery.SpreadRows"/>・<see cref="World.SpreadReach"/>）＝発見の「伝わり」の項が読む（設計 §1A-7＝距離ではなく取引量）。
    ///   放射＝max(自国に届いた到達, 自国の種)・種＝思想はその思想を持つ人の割合（国の思想の集計＝旧と同じ）・制度は解禁済みなら1（<see cref="Discovery.SpreadSource"/>）。
    /// ⚠DiffusionWeight=0＝即return（ペア記帳側も同ゲート）。</summary>
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
            media[i2] = sz > 1e-9 ? PoliticalQuantities.Clamp01(m / sz) : 0;
        }
        var prev = new double[n][];
        for (int i2 = 0; i2 < n; i2++)
        {
            var wr = Nations[i2].World;
            // ★段8の種（2026-09-04）：思想を**持つ**国が放射源＝到達を自国の種で下支え（名前で起点を書かない・対称）。
            // ★2026-09-04 E-12実測（差し戻し）：種を到達に**書き戻す**と「自国シェア→自国到達→自国の門→自国シェア」の
            //   正帰還が国内で閉じ、社会主義が61/61カ国へ1792〜95年に自家発火した。種は**放射側（prev）だけ**に使い、発見が読む
            //   受信側 SpreadReach には他国から届いた分しか積まない（＝伝播の定義）。
            prev[i2] = (double[])wr.SpreadReach.Clone();
            var nat = Ideologies.National(wr);
            for (int k2 = 0; k2 < prev[i2].Length; k2++)
            {
                double src = Discovery.SpreadSource(wr, k2, nat);
                if (src > prev[i2][k2]) prev[i2][k2] = src;
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
                var rb = wB.SpreadReach;
                for (int k = 0; k < ra.Length && k < rb.Length; k++)
                    if (ra[k] > 0) rb[k] = System.Math.Min(1.0, rb[k] + link * ra[k]);
            }
        }
        Market.LastPairFlow = pf; Market.PairFlowQ = null;   // 四半期消費（行列はテレメトリへスワップ退避＝pairprobeが読む・次四半期は再確保）
    }

    internal void ReabsorbNations()   // internal＝番人（FiscalTests の再併合の貸借）が四半期の他の流れを挟まずにこの1段だけを踏む
    {
        System.Collections.Generic.HashSet<Power>? peaceful = null;   // 残骸の平和的復帰＝戦争の決着ではない＝決着の当てはめ（政体・鎮圧の後始末）を通さない
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
                    (peaceful ??= []).Add(rp2);
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
            // ★政体の変更は州が戻った後（下の決着の当てはめ）＝革命の後始末（失脚・恐怖政治・土地改革）が再統一した国の全POPに掛かる（旧経路の内戦の勝ちと同じ範囲）。
            // ★再併合の貸借（2026-09-22・作者裁定「全て推奨」）：反乱国の貸借の各行を「州について親へ行く」か「請求権として消える（金は動かない）」かに仕分ける。
            //   規則＝貨幣であるものは湧きも消えもしない＝世界の基礎貨幣 Σ(貯蓄＋国庫−与信) は再併合の前後で同じ。
            //   旧＝親へ渡すのは貯蓄と**正の**国庫だけで、反乱国の与信と負の国庫が国ごと消えた＝世界の貨幣がその額だけ湧いた（㉚の前の木・seed1789・604四半期の一時計器で +10,241）。
            //   親へ：貯蓄（POPごと）・国庫（負＝当座借越も）・与信（Loans＝預金の対／HouseholdLoans＝POPと一緒に動く Pop.Debt の対）・
            //         外資の負債と対外資産（ForeignCapitalIn／CapitalAbroad＝工場も投資家も州にいる）・対外債権（ForeignBondClaims）・
            //         途中の管（元利の管／借入の予約＝同じ四半期の CapitalMarket.Step と貸し手の引き落としで空だが、順序に頼らない）・造幣局の金属準備（現物）。
            //   反乱国の国債＝★作者裁定 2026-09-29（段3-3 で実施・2026-09-30）＝**勝った側の国債は統一後の国が引き継ぐ**：
            //     反乱の勝ち＝反乱国の国債（国内の保有と対外債）を親へ（下の決着の当てはめの後＝急進革命の否認は旧政府の国債だけに掛かる＝既存の規則）。
            //     政府の勝ち（と残骸の平和的復帰）＝反乱国の国債は消える＝国内の保有（下の BondHoldings）と対外債（ForeignDebt）の両方。対外債は債権国の
            //     ForeignBondClaims を債権の比で落とす（損は持ち手。落とさないと相手のいない債権が元利の配当を吸う）。★史実＝米の南部連合の債務は修正14条4節（1868）で否認。
            //   Bank.Gold は導出量（Credit.Step が毎日 BaseMoney から上書き）＝貨幣の行ではない（返還の1行も 2026-09-28 に削除）。
            double T = rw.Treasury;
            var moving = new System.Collections.Generic.List<Region>(rw.Regions);
            // ★国債の保有台帳（2026-09-21）：政府の勝ち＝反乱国の国債は誰にも継承されない＝国が払うのを止めた割合（全額）だけ保有を消す＝損は持ち手（金は動かない）。
            //   rw.GovDebt も同額下げる＝消える直前まで反乱国側の不変条件も厳密。★史実：合衆国憲法修正14条4節（1868）は南部連合の債務の継承・支払いを明文で禁じた。
            //   反乱の勝ち＝保有を預かり（0へ）、決着の当てはめ（急進革命の否認・恐怖政治の没収＝旧政府と旧支配層に掛かる）の後に持ち手へ戻し、親の国債へ足す。
            List<(Pop Pop, double Held)>? inherit = rebelWon ? [] : null;
            foreach (var rr in moving)
                foreach (var pp in rr.Pops)
                {
                    if (inherit is not null) { if (pp.BondHoldings > 0) inherit.Add((pp, pp.BondHoldings)); }
                    else rw.GovDebt -= pp.BondHoldings;
                    pp.BondHoldings = 0;
                }
            // 州は移し替えの1本（Territory.TransferRegion の再併合・2026-09-28 処理の一元化 段2 B）＝貯蓄と人口は州ごとに両国の台帳へ（同一値を両辺）・
            //   建国時の反転を戻す（親の中核定義へ復帰）・市場の台帳のリセット・植民地財の空化（null にしない＝col-02）。
            //   ★返還州の Id は親の中で最大＋1（Territory.Adopt）。⚠旧＝`親の州の数` で振った＝建国で親の州が抜けて Id が歯抜けのとき
            //   既存の州と同じ Id が付いた（0..9 から 4 が抜けて数が9 → 返還州に 9）。Id で州を引く所（Politics の州の索引）と割譲先の同点決着が崩れていた。
            foreach (var rr in moving) Territory.TransferRegion(rw, pw, rr, Territory.Rehoming.Returned);
            // ★首都を返す（2026-09-28 処理の一元化 段2 束3・作者裁定 a）：首都の分離独立の反乱国が握っていた首都州が戻った＝政府は戦時の首都から戻る
            //   （反乱の勝ち＝革命政府が首都で新しい政体を敷く／政府の勝ち＝首都を再征服）。遷都の1本（World.SetCapital＝Id と Regions[0]・距離の基準の座標・植民地財）。
            //   返還州は Territory.Adopt が最大＋1の Id を振った後＝SetCapital が戦時の首都と Id を入れ替える。
            //   首都州がこの国へ戻らなかった（戦中に第三国へ割譲された）ままこの親の内戦が全て終わったら、記しだけを外す（戦時の首都が首都のまま）。
            if (pw.SecedingRegion is { } seat && moving.Contains(seat)) pw.SetCapital(seat);
            if (pw.SecedingRegion is { } seat2 && (moving.Contains(seat2) || !Diplomacy.Wars.Exists(x => x.CivilWarParent == parentP))) pw.SecedingRegion = null;
            // ★人と動くお金の換算（2026-09-30 処理の一元化 段4e＝(120) の残り）：反乱国の国庫・与信・対外の残高は反乱国の通貨＝親の通貨へ換算して足す
            //   （換算の1本 World.CrossFx＝国庫と与信は基礎貨幣＝差は換算の台帳へ・与信は負の額として・対外の資本と債権と途中の管は名目の残高＝台帳に積まない）。
            //   旧＝名目のまま足した（反乱国の為替は建国の時は親の値だが、戦争の間に自国の月次の式で離れる）。金属の準備は現物＝換算しない。
            // ★2026-09-30 段4g：国庫は移し方の1本（反乱国の国庫→親の国庫・国をまたぐ＝換算の1本・台帳は国境の出入り＝親の CumulativeTradeFlow へ動いた額）。
            //   負の国庫（当座借越）は逆向き（親の国庫→反乱国の国庫＝反乱国の借越を埋める額を親が負う）＝親の国庫が同じ換算の額だけ減る（旧の Pay(負の額) と同じ量）。
            //   旧＝親の国庫の片側の記帳（Pay）と台帳の手書き。反乱国の側の台帳と国庫は反乱国ごと消える（読み手なし）。
            double Tin = World.CrossFx(rw, pw, T, book: false);   // 換算の目安（台帳には積まない＝移し方の1本が積む）
            if (T > 0) World.Transfer((rw, null), (pw, null, null), T, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            else if (T < 0) World.Transfer((pw, null), (rw, null, null), -Tin, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            double L = 0;
            if (pw.Bank is { } pb && rw.Bank is { } rb)
            {
                double loansIn = -World.CrossFx(rw, pw, -rb.Loans), hhIn = -World.CrossFx(rw, pw, -rb.HouseholdLoans);
                pb.Loans += loansIn; pb.HouseholdLoans += hhIn; L = loansIn + hhIn;
            }
            pw.CumulativeTradeFlow -= L;   // 与信（負債）の合算＝基礎貨幣の −L（親の通貨）。貯蓄 S は州ごとに上で記帳済み・国庫は上の移し方の1本＝合わせて反乱国の基礎貨幣（S＋T−L）＝親の per-nation 保存が閉じ、世界の和の差は換算の台帳（建国時の −(S+T) の鏡映＋反乱国として積んだ分）
            pw.ForeignCapitalIn += World.CrossFx(rw, pw, rw.ForeignCapitalIn, book: false); pw.CapitalAbroad += World.CrossFx(rw, pw, rw.CapitalAbroad, book: false);
            // 反乱国の対外債の否認（政府の勝ち）：債権へ繰り上げ済みの額（PrevForeignDebt）を、反乱国以外の債権の比で落とす（債権の増減の1本＝CapitalMarket.AdjustClaims・
            //   四半期の償還と同じ規則）。反乱国自身の債権は次の行で親へ。
            if (!rebelWon && rw.PrevForeignDebt > 1e-9) CapitalMarket.AdjustClaims(Nations, ri, -rw.PrevForeignDebt);
            pw.ForeignBondClaims += World.CrossFx(rw, pw, rw.ForeignBondClaims, book: false);
            pw.ForeignIntPaidThisQuarter += World.CrossFx(rw, pw, rw.ForeignIntPaidThisQuarter, book: false); pw.PendingForeignBorrow += World.CrossFx(rw, pw, rw.PendingForeignBorrow, book: false);
            pw.SilverReserveQty += rw.SilverReserveQty; pw.GoldReserveQty += rw.GoldReserveQty;
            // 反乱国の植民地（戦中に得た場合の防御）＝親へ移す（無主化して消さない）
            foreach (var col in rebelP.ColonyList) Diplomacy.AcquireColony(parentP, col, _coef);   // 植民地の受け取りの1本（講和の奪取・無血併合と同じ＝国を持つ受け手は植民地格の州へ）
            rebelP.ColonyList.Clear();
            // ★決着の当てはめ＝③政体・⑤人と政治（2026-09-28 処理の一元化 段2 束2）＝旧経路の内戦の決着と同じ1本（Politics.SettleRevolt）。
            //   反乱の勝ち＝政体変更の実行部（急進革命だけ国債の否認・失脚・土地改革・覚醒・断裂の窓＝即時の政変と同じ規則）／同じ政体の要求戦は要求をのむ（ConcedeToFaction）。
            //   政府の勝ち＝鎮圧の後始末は**反乱していた州（返還州）の住民だけ**（作者裁定 d）＋断裂の窓。運動はどちらでも燃え尽きる。
            //   ⚠旧＝革命の勝ちは登録簿と建国制度の2行だけ・要求戦の勝ちは何も起きない・鎮圧の後始末は休眠の係数（RevoltAftermathOnWarPath＝0）の裏
            //     ＝旧経路の鎮圧の行（Politics.ProcessCivilWar）を正本に1本へ畳み、係数は削除した（旧の休眠の枝が足していた正統性の減算は旧経路の鎮圧の行に無い＝持ち込まない）。
            //   運動＝本拠州がこの反乱国の州だった運動だけ（同じ親で別の内戦が進んでいても他の運動を解かない）。残骸の平和的復帰は戦争の決着ではない＝当てはめない。
            var movement = pw.RevoltingFaction is { } mv && moving.Contains(mv.Region) ? mv : null;
            // ★計器（F3b蜂起doc §5 段1・純記帳）：鎮圧の実体は「親が内戦に勝って再併合する」この経路＝決着した運動の名をここで出す（力学は読まない）。
            string spentName = movement is not null && !rebelWon ? $"（{Factions.DisplayName(movement)}の蜂起を鎮圧）" : "";
            var polityBefore = pw.Polity;
            if (pw.PoliticsEnabled && !(peaceful?.Contains(rebelP) ?? false))
                Politics.SettleRevolt(pw, _coef, rebelWon, rebelPolity, movement, moving.SelectMany(r => r.Pops));
            // 反乱の勝ち＝反乱国の国債を統一後の国が引き継ぐ（作者裁定 2026-09-29）＝預かった保有を持ち手へ戻し、国債・対外債・繰上げ済みの印を親へ足す
            //   （対外債の債権国の債権はそのまま＝請求の相手が統一後の国になるだけ）。国内元本＝Σ保有＝rw.GovDebt−rw.ForeignDebt＝親の不変条件も閉じる。
            if (inherit is not null)
            {
                // ★人と動くお金の換算（段4e）：反乱国の国債の元本と保有は反乱国の通貨＝親の通貨へ（名目の残高＝換算の台帳に積まない・Σ保有＝国内元本の不変条件は同じ比で保つ）。
                foreach (var (hp, h) in inherit) hp.BondHoldings += World.CrossFx(rw, pw, h, book: false);
                double debt0 = pw.GovDebt, rwDebt = World.CrossFx(rw, pw, rw.GovDebt, book: false);
                pw.GovDebt += rwDebt; pw.ForeignDebt += World.CrossFx(rw, pw, rw.ForeignDebt, book: false); pw.PrevForeignDebt += World.CrossFx(rw, pw, rw.PrevForeignDebt, book: false);
                if (pw.GovDebt > 1e-9) pw.GovAvgCoupon = (debt0 * pw.GovAvgCoupon + rwDebt * rw.GovAvgCoupon) / pw.GovDebt;   // 既往債の利率は元本の加重（起債の R9 の混ぜ方と同じ）
            }
            // ★作者裁定 f（2026-09-28）：反乱側に乗り換えたプレイヤーの国が吸収された＝親の国で続ける（操作者と外交の IsPlayer を親へ・画面は GameViewModel が追従して通知）。
            //   ⚠AI の国は何もしない（操作者が居ない）＝エンジンの規則は同じ。
            if (rebelP.IsPlayer)
            {
                rebelP.IsPlayer = false; parentP.IsPlayer = true;
                Nations[pi].SetAgent(new NullAgent());
                Diplomacy.Log.Add($"{pw.Year} {rebelP.Name}は{parentP.Name}へ再併合された＝操作国は{parentP.Name}");
            }
            Diplomacy.RemovePower(rebelP, _coef);   // 反乱国が巻き込まれていた第三国との戦争も EndWar を通して終わる＝終戦の記録が残る
            CostLedger.AddInto(_costDeparted ??= new long[CostLedger.Count], Nations[ri].World.CostOrNull);   // 計算量の台帳：消える国の件数を受け皿へ（診断・軌道に触れない）
            Nations.RemoveAt(ri);
            // ★国の番号のずれ（2026-09-28 処理の一元化 段2 B）：四半期のペア貿易額 PairFlowQ は国の番号（Nations の添字）で引く＝除いた国の行と列を落とす。
            //   ⚠旧＝そのまま残った＝この後の StepDiffusion（思想の伝播・交易の接触度）が ri より後ろの国に1つ前の国の行を読ませた
            //   （除いた国の番号より後ろの全ての国が、隣の国の貿易で接触度と思想の到達を受け取った）。
            if (Market.PairFlowQ is { } pfq && ri < pfq.Length)
                Market.PairFlowQ = pfq.Where((_, a) => a != ri).Select(row => row.Where((_, b) => b != ri).ToArray()).ToArray();
            Diplomacy.Log.Add(!rebelWon
                ? $"{pw.Year} {parentP.Name}が{rebelP.Name}を再併合＝内戦の傷跡だけが残った{spentName}"
                : rebelPolity != polityBefore
                ? $"{pw.Year} {rebelP.Name}が{parentP.Name}を制した＝{rebelPolity}の下で再統一"
                : $"{pw.Year} {rebelP.Name}の要求が通り{parentP.Name}へ再統一");
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
                double proximity = PoliticalQuantities.Clamp01(1.0 - km / (_coef.EpidemicTransmitRange * _coef.EraFactor(w.WorldTechLevel, _coef.EraEpidemicRange)));   // ★時代関数化（掃討第2波2026-08-13）：鉄道と汽船で伝播距離が伸びる（2026-08-29点火＝既定1。=0で×1恒等）              // 陸路＝近いほど強い
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

    // 海峡・運河（§B）：扼者を host 領土から解決し、運河は host 保有の経済国の資本家が自腹で建設（投資家→自国の労働の④＝工賃で保存）。富裕な工業国ほど速く開通。
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
                // ★段3-3（C13・2026-09-30）：出資者＝投資家の述語の1本（CapitalMarket.Investors＝配当の受け手と同じ集合）。旧＝各州の資本家1人（InvestorPopIndex・活きた州）。
                var investors = CapitalMarket.Investors(w);
                double pool = 0; foreach (var ip in investors) pool += Math.Max(0, ip.Savings);
                if (pool <= 1e-6) continue;
                // ★支払いの一元化 段2-3（E1・2026-09-29）：投資家（貯蓄の比）→建設の工賃を移し方の1本で＝動いた額が進捗と持分になる。
                // ★段3-2（2026-09-30・C9）：工賃は**自国の労働の④の持分**（全ての活きた州の労働力の生産者の産出の比・賃金は労働者・Earned を通す＝InternationalMarket.PaySales の国の枝）。
                //   旧＝自国の全POP へ頭数（財を買わない＝建設の工賃なのに農民・貴族・兵士まで受け取った）。労働の担い手がいなければ動かない（進捗も持分も立たない）。
                double inv = InternationalMarket.PaySales((w, investors), null, Catalog.LaborId, _coef.CanalInvestFrac * pool, w);
                if (!(inv > 0)) continue;
                cp.BuildProgress += inv;
                // 外7：拠出国の持分（累計拠出額）を記録＝後で通行料を持分比で分配（配当）。既定0.5＝この持分が配当に効く。0では通行料が扼者総取りのままなので持分は参照されず無影響。
                //   ★段3-3（2026-09-30）：持分は**金建て**（拠出×為替）＝国をまたぐ配当の比を1つの物差しで（進捗 BuildProgress は建設の工賃＝国内の払いの額のまま）。
                if (_coef.CanalInvestReturns > 0 && w.SelfPower is { } shp)
                {
                    double invG = inv * w.FxRate;
                    cp.InvestorShares[shp.Id] = cp.InvestorShares.TryGetValue(shp.Id, out double prev) ? prev + invG : invG;
                }
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
                double tollBase = _coef.CanalTollTrafficRate * traffic;   // 通航トン数課金（取扱高の代理・前四半期値）＝取扱高は金建て（積出し価格）
                double toll = tollBase / uw.FxRate * uw.FiscalFundingRatio;   // 使用国の通貨（★段3-3・2026-09-30）×国家の支払い率 r の分だけ（2026-09-28 段2-1＝旧は国庫の残りで切る門）
                if (toll <= 1e-9) continue;
                // 外7：CanalInvestReturns>0 なら通行料の一部を建設拠出国の**投資家**へ持分（累計拠出額）比で配当し、残りを扼者の国庫へ。既定0.5＝配当あり。0＝全額扼者。
                //   ★段3-3（C9・2026-09-30）：配当の受け手は出資した国の投資家（出資者と同じ集合・旧＝拠出国の国庫）。払いは使用国の国庫から直接・国境をまたげば為替で換算（移し方の1本）。
                double dividend = _coef.CanalInvestReturns > 0 ? DistributeCanalDividend(cp, toll * _coef.CanalInvestReturns, uw) : 0;
                World.Transfer((uw, null), (ow, null, null), toll - dividend, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);   // 配当されなかった残余（＝扼者取り分）。分配失敗時も toll 全額が扼者へ回り保存
            }
        }
    }

    // 外7：通行料の配当ぶんを、この運河の建設拠出国へ持分（InvestorShares＝累計拠出額・金建て）比で配分。
    // ★段3-3（C9・2026-09-30）：受け手は拠出国の**投資家**（CapitalMarket.GiveGold＝投資家だけ・投資家のいない国は国庫）・払い手は使用国の国庫（区分 その他の支出）。
    //   扼者の国も出資していればその投資家が持分どおり受け取る（旧＝国庫⇄国庫で、扼者自身の持分は扼者本人の徴収と二重になるため除外していた＝受け手が投資家になり二重ではなくなった）。
    // 返り値＝実際に配当した総額（使用国の通貨・≦amount）。抽象/消滅国の持分は配らない＝呼び手が残余として扼者へ。
    internal double DistributeCanalDividend(Chokepoint cp, double amount, World payer)   // internal＝番人（FiscalTests の運河の配当）がこの1段だけを踏む
    {
        if (amount <= 1e-12 || cp.InvestorShares.Count == 0) return 0;
        double totalShare = 0;
        foreach (var kv in cp.InvestorShares) totalShare += kv.Value;
        if (totalShare <= 1e-9) return 0;
        double paid = 0;
        foreach (var kv in cp.InvestorShares)
        {
            var inv = Diplomacy.Powers.Find(p => p.Id == kv.Key);
            if (inv?.Nation is not { } iw) continue;   // 抽象/消滅国は配当を受け取れない＝その分は残余として扼者へ
            paid += CapitalMarket.GiveGold(iw, amount * (kv.Value / totalShare), (payer, null), TreasuryCat.OtherOut);
        }
        return paid;
    }

    // 港湾の建設（改修2026-06-28）：StepCanals の雛形を Region に移植＝各国の港州を、交易が容量で律速（混雑）している時に**資本家が主導・国家が上乗せ**して拡張する。
    // 保存：投資（資本家貯蓄＋国庫）→ 港の州の労働の④（建設の工賃＝StepCanals と同じ・BaseMoney不変）。
    // ★2026-09-30（段3-4・C10）：港湾料は成約の時点の上乗せへ移した（BilateralMarket.StepBilateral＝荷主〔輸入の捌き手〕が払い、港の州の投資家が受け取る）＝ここは容量だけ。
    private void StepPorts()
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            double natPop = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) foreach (var p in r.Pops) natPop += p.Size;
            if (natPop <= 1e-9) continue;
            double natThr = w.PortThroughputTotal();   // ★(223) 国家ルートの請求の分母（World.PortStateRequest）
            bool techOk = Discovery.SteamPower(w.Discovered);   // ★近代港湾（蒸気の浚渫・埠頭）の門＝発見の表の「蒸気機関」の行（2026-10-01 (150)・旧＝技術水準の閾値 PortTechOnset 0.2）
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || !r.IsPort) continue;
                if (!_calendarFrozen) r.PortCapacity = Math.Max(0, r.PortCapacity * (1 - _coef.PortDecay));   // 減耗（維持に再投資）。★助走中は減耗も拡張も止める＝港湾容量は1783年のまま
                //（港湾料は 2026-09-30 に成約の時点へ＝旧＝四半期末に取扱高×率を国の全POP から貯蓄の比で・段3-4 C10）
                // 拡張は混雑時のみ（律速率<目標＝容量が交易を絞っている）＝採算ゲートで過剰投資しない。技術ゲート（近代港湾）。
                if (_calendarFrozen || !techOk || r.LastTradeThrottle >= _coef.PortCongestionTarget) continue;   // ★助走：港湾も「年月をかけて広がる」能力＝凍結
                if (r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) continue;
                var ip = r.Pops[r.InvestorPopIndex];
                double cCap = _coef.PortInvestFrac * Math.Max(0, ip.Savings);          // 資本家ルート
                double cState = w.PortStateRequest(r, natThr) * w.FiscalFundingRatio;   // 国家ルート＝国家の予算の行（四半期の締め月の請求に入っている額）×国家の支払い率 r（2026-09-28 段2-1・(223) 旧＝PortStateFrac×資本家の投資×r を請求の外で）
                double invest = cCap + cState;
                if (invest <= 1e-9) continue;   // ★判定→引落の順（wave2b是正）：引落してから捨てると微小額が pop へ還流されず消える片側記帳になる
                // ★支払いの一元化 段3-2（2026-09-30・C9）：建設の工賃は**港の州の労働の④の持分**（労働力の生産者の産出の比・賃金は労働者・Earned を通す＝InternationalMarket.PaySales）。
                //   旧＝自国の全POP へ頭数（財を買わない）。進捗は動いた額（資本家の分＋国庫の分）。
                invest = InternationalMarket.PaySales((w, [ip]), r, Catalog.LaborId, cCap, w)
                       + InternationalMarket.PaySales((w, null), r, Catalog.LaborId, cState, w, TreasuryCat.OtherOut);
                r.PortCapacity += invest / _coef.PortBuildCostPerUnit;
            }
        }
    }

    // 海上保険（改修2026-06-28・金融改革③）：商人pop＝引受手。封鎖損失時は損失×給付率を商人基金→荷主へ（基金=商人貯蓄の範囲で部分払い＝ロイズの破綻リスク）。
    // すべてpop⇄pop移転＝基礎貨幣不変。多国シム(StepDay四半期)からのみ呼ばれる＝単一国較正は不走行で中立。商人不在の国は保険市場が立たない。
    // ★2026-09-30（段3-4・C10＝処理の一元化 段4d）：保険料は成約の時点の上乗せへ移した（BilateralMarket.StepBilateral＝輸入の捌き手が払い、輸入国の商人が受け取る）。
    //   保険金の受け手は**損をした荷主**＝各州の捌き手の商人（その州の海路の取扱高の比）。旧＝保険料は全POP の貯蓄の比・保険金は全POP の頭数（荷主でも損をした者でもない）。
    //   ⚠封鎖の危険 LastBlockadeRisk の書き手が無い＝常に0（棚卸し econ-14）＝保険金は実ゲームでは立たない（形だけを直した）。
    private void StepMarineInsurance()
    {
        if (_coef.InsurancePayoutFrac <= 0) return;
        foreach (var sim in Nations)
        {
            var w = sim.World;
            if (w.SelfPower is null) continue;
            double throughput = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) throughput += r.LastPortThroughput;
            if (throughput < 1e-9) continue;
            double risk = w.LastBlockadeRisk;   // 03手順7：旧CommerceRaidFactorの置換＝リスク線積分（取扱高加重平均）
            if (!(risk > 1e-9)) continue;
            var merchants = new List<Pop>(); var exposure = new List<double>();
            foreach (var r in w.Regions)
                if (r.State == RegionState.Active && r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count)
                { merchants.Add(r.Pops[r.MerchantPopIndex]); exposure.Add(Math.Max(0, r.LastPortThroughput)); }
            if (merchants.Count == 0) continue;   // 引受手(商人)が居ない国は保険市場が立たない
            // 保険金：商人基金（引受手・貯蓄の比）→荷主（捌き手の商人・海路の取扱高の比）＝基金の範囲で部分払い（払い手の貯蓄でクランプ・移し方の1本）
            World.Transfer((w, merchants), (w, merchants, exposure), _coef.InsurancePayoutFrac * throughput * risk);
        }
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

    /// <summary>★安全保障の不安（2026-10-02 (155) 作り直し・作者 2026-10-01 の名前「安全保障の不安」・推奨で進めた）＝全国の受ける脅威を四半期1回計算する。
    /// 脅威(B←A)＝A の養える軍（定員＝<see cref="Military.EstablishmentOf"/>＝A が選んだ軍事費の段×歳入÷1人あたりの費用と人口の側の小さい方＝相手の今の兵数〔戦時の徴兵の跳ね〕は読まない・⚠(177) 注記の一巡〔反証役 vc2#5〕＝(175) から A が交戦×総動員の間は器に戦時段の借入の1件の額が入る＝戦時の跳ねの一部が入る）のうち
    /// B に向く注意配分 a(A→B)＝hr(A→B)÷(u(A)＋Σ_K hr(A→K))。hr＝敵意 h（<see cref="ArmyShape.Hostility"/>＝同盟0・宗主と属国の間×(1−AllyAttentionDiscount)・緊張の有理飽和）
    /// ×届く強さ（国境の長さと地形〔陸路グラフで隣り合う州の組×地形の通りにくさ・植民地格の州も＝<see cref="ArmyShape.Borders"/>〕＋投射〔距離減衰×海越えの制海権＝<see cref="Diplomacy.Projection"/>〕）
    /// ・u＝内向きに使われる軍（<see cref="ArmyShape.Unrest"/>）＝**(151) の理想の軍隊の形の陸の必要量と同じ物差しと同じ注意配分**（相手の軍は同時に全方位へ向けられない＝世界の脅威の和は世界の養える軍の和より小さい
    /// ＝軍拡が縮小写像。⚠正規化しない総和は全ての国が全ての国から脅威を受け、61カ国の中央の国の脅威の割合が0.9を超えた＝(155) の試走）。
    /// 自国の分母＝養える軍の人口の側（兵役の母数×MaxSoldierFraction＝自分の軍事費の段に依らない＝自国の段を上げても不安は下がらない＝段の往復を作らない）。
    /// 読み手＝AI の軍事費の段（唯一の入口・SelfPlay の脅威の割合）・徴兵法の目標（(178) から陸軍の不安 SecurityThreatArmy の割合＝本国の国境＋投射×上陸の門）・戦略鉄道（Transport の目標・脅威÷自国の養える軍・前期の値）。旧＝州の経済質量×距離減衰の最大の和（同盟も国境も海も読まない）。
    /// 世界の中央との比（作者のメモ M4「100%を基本に」＝表示だけ）は UI の回で（導出＝持たない）。</summary>
    private void StepSecurityThreat()
    {
        int n = Nations.Count;
        var ps = new List<Power>(n);
        var seats = new double[n];
        var own = new double[n];
        for (int i = 0; i < n; i++)
        {
            var w = Nations[i].World;
            ps.Add(w.SelfPower!);   // 多国では全ての国が外交上の代表を持つ（ArmyShape.Borders は Nation を持つ Power の並び）
            seats[i] = Military.EstablishmentOf(w, w.Coef, out double man);
            own[i] = w.Coef.MaxSoldierFraction * man;
        }
        var borderHome = new double[n * n];   // ★(178) 両側とも本国の州（Core・Integrated）の国境だけ（対称）
        var border = ArmyShape.Borders(ps, _coef, borderHome);
        var hr = new double[n * n];   // hr[a*n+b]＝A の軍が B に届く強さ×敵意
        var hrArmy = new double[n * n];   // ★(178) そのうち陸軍の不安＝h×(本国の国境＋投射×上陸の門)＝B の海軍で防げない分（海越えで A の制海権が門に届く投射）だけを数える＝徴兵法の目標が読む
        var colSum = new double[n];   // A の注意配分の分母＝内向き u(A)＋Σ_B hr(A→B)（陸と海越えの両方へ配る同じ1つの分母＝ArmyShape と同じ）
        long pairs = 0;
        for (int a = 0; a < n; a++)
        {
            if (ps[a] is null) continue;
            double sum = ArmyShape.Unrest(ps[a]);
            for (int b = 0; b < n; b++)
            {
                if (a == b || ps[b] is null) continue;
                double h = ArmyShape.Hostility(Diplomacy, ps[b], ps[a], _coef);
                if (!(h > 0)) continue;   // 同盟＝0（否定形＝NaN も数えない）
                pairs++;
                double proj = Diplomacy.Projection(ps[a], ps[b], _coef);
                double r = border[a * n + b] + proj;
                if (!(r > 0)) continue;
                hr[a * n + b] = h * r;
                hrArmy[a * n + b] = h * (borderHome[a * n + b] + proj * Diplomacy.LandingGate(ps[a], ps[b], _coef));   // 上陸の門＝陸続き1・海越えは A の制海権が門に届けば1（B の海軍が防げる投射は陸軍の不安に数えない）
                sum += h * r;
            }
            colSum[a] = sum;
        }
        for (int b = 0; b < n; b++)
        {
            double total = 0, army = 0;
            for (int a = 0; a < n; a++)
                if (a != b && colSum[a] > 1e-12 && seats[a] > 0)
                {
                    total += hr[a * n + b] / colSum[a] * seats[a];
                    army += hrArmy[a * n + b] / colSum[a] * seats[a];   // 同じ注意配分の分母・同じ養える軍（分子と分母を同じ規則で）
                }
            var wb = Nations[b].World;
            wb.SecurityThreat = total;
            wb.SecurityThreatArmy = army;
            wb.SecurityThreatOwnMass = own[b];
        }
        Cost[CostLedger.ThreatReachPairs] += pairs;   // 計算量の台帳（診断）：脅威の届き方の組（安全保障の不安＝四半期1回・同盟の組を除く）
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
                    p.CoreEthnicShare = PoliticalQuantities.Clamp01(p.CoreEthnicShare + assim * resist * kernel);   // 倍率排除⑧：同化は per-pop で中核割合を1へ寄せる（EthnicUnity は加重平均から創発）
                }
            }
        }
    }

    // 国家間移民（回1-2・2026-09-27＝国際移民を閉じる・外部レビューの検討 §10-3 Q3／項目3）：機会（Territory.Opportunity＝土地逓減・住宅摩擦・
    // 賃金プレミアムを含む飽和しない実体量）の差で、各州の大衆（農民・労働者）が豊かな国の州へ移る。「アメリカに流すのではなく、アメリカが豊かだから流入する」。
    // ★人はゲームの外から来ない＝送出州から引いた分だけ受入州に着く（旧の世界プール＝世界人口×0.004/四半期を送出国から1人も引かずに配り、
    //   持参金を世界平均の貯蓄から湧かせた経路は削除）。保存：出る側 PopulationDelta−/持参金は CumulativeTradeFlow−、入る側 ＋（両国で閉じる）。
    // ★2026-10-01 (141)：**国内の移民（Territory.Step）と同じ規則**＝行き先は全ての国の全ての州（旧＝各国の最良の1州に着いた＝米東部の混んだ州に積もり、
    //   国内の移民で西部へ抜けて1/4 未満〔(125) の申し送り5〕）・距離は州の座標の間で同じ1本（Territory.MigrationDecay・旧＝国の座標の間）・
    //   受け入れの上限は州ごと（国内の移民と同じ＝受け入れる州の人口×受け入れの能力・越境だけ移民制限法を掛ける）・
    //   出る側は国内と同じ「残る値打ち」＝機会×残る人の土地の持ち分（Territory.Holding）×州の食料充足（旧＝機会×食料充足に、送出 POP の規模の下限のクランプ
    //   EmigrationFloorSize〔50千人・幅 EmigrationFloorSpan 2〕＝A4 裁定「人が減るほど賃金が上がる」を床で書いたもの＝思想⑧＝残る人の持ち分が同じことを収穫逓減で言う）。
    // 流量はすべて四半期の初めの値で決め、決め終えてから移す。
    //   1 行き先 x＝全ての国の稼働中の州（機会が評価できる州）＝機会 O_x・座標・受入国 d の重み a_d（民族：低統一度は引く・低識字は差別で阻む）・
    //     州の受入の上限 C_x（州の人口×受入の上限率×移民制限法×州の受入能力）
    //   2 送出州 r（全ての Active 州）＝残る値打ち P_r＝機会×持ち分×州の食料充足。差 g_rx＝(O_x−P_r−閾値)×距離の減衰（正のものだけ）・重み w_rx＝g_rx×a_d
    //   3 階級ごとの流出 F＝min(EmigrationRate×最良の行き先の差, 上限×(1＋飢え))×人数。**流出の上限はここで1回だけ**＝行き先がいくつあっても同じ
    //     （旧＝「最悪の1州→最良の1国」の1組だけ動かしていた。全組で上限まで抜くと1国が多重に排水される＝プロイセン技術0.38への空洞化を実測したため。
    //     上限を1回だけ掛ければ、1つの州が多くの州へ同時に上限まで流れ出ることが構造的に起きない）。
    //   4 F を、その階級の POP が居る行き先へ w_rx の比で割り当てる（休眠の運賃は払える割合を掛ける＝居ない行き先の分は送出州に残る＝国内の移民の Movable と同じ）。
    //     受入州ごとの合計が C_x を超えたら x への分をすべて C_x÷合計で縮める
    //     ＝**縮んだ分は送出州に残る**（消えない・湧かない。移民制限法は「別の国へ回す」でなく「流れを減らす」法＝作者裁定 B3）。
    //   5 移す（MoveMigrants＝決まった人数を1つの階級だけ・属性の運搬は旧のまま）。
    internal void MigrateBetweenNations()   // internal＝番人テストが段を単離して1回だけ呼ぶため（StepAssimilation・MoveMigrants と同じ扱い）
    {
        foreach (var sim0 in Nations) { var w0 = sim0.World; w0.LastImmigration = 0; w0.LastEmigration = 0; w0.LastSpecialistExit = 0; }   // 診断（UI）：四半期の移民収支カウンタ（★段⑤：専門職の流出カウンタも同じ場所で0に＝無条件＝滞留しない）
        if (!(_coef.CrossBorderMigrationByOpportunity > 0)) return;   // 0＝越境移民なし（旧・満足度ベース経路は2026-07-28に係数ごと削除済み）
        int n = Nations.Count;
        // 1. 行き先の州・機会・座標・受入国の重み・州の受入の上限・居る階級
        var dstA = new double[n];
        var dst = new List<(int D, Region R, double O, double Lat, double Lon, double Cap, int Classes)>();
        for (int d = 0; d < n; d++)
        {
            var w = Nations[d].World;
            // 民族：低統一度は移民を引く（坩堝＝アメリカ）／低教育は差別で移民を阻む（平均識字が閾値未満で減衰＝排斥）。
            dstA[d] = w.EthnicEnabled
                ? (1.0 + _coef.EthnicDraw * (1.0 - w.EthnicUnity)) * PoliticalQuantities.Clamp01(PoliticalQuantities.AvgLiteracy(w) / _coef.DiscriminationLiteracy)
                : 1.0;
            // 受入の物理上限＝アイルランド脱出級の絶対フロー（ImmigrationReceiveCapFrac/四半期）×移民制限法×受入能力（混雑で縮む＝AbsorbCap）。
            // ★移民制限法（法の棚卸しdoc L2・2026-08-01）：Open＝1.0／Selective＝選別で細る／Closed＝ほぼ閉じる
            //   （史実＝米の排華移民法1882・紳士協約1907・1924移民法／豪の白豪主義）。★(141)：州ごと（国内の移民の受け入れの上限と同じ＝その州の人口×その州の受入能力）
            double lawCap = w.ImmigrationRestriction switch
            {
                ImmigrationLaw.Selective => _coef.ImmigrationSelectiveFrac,
                ImmigrationLaw.Closed => _coef.ImmigrationClosedFrac,
                _ => 1.0,
            };
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double o = Territory.Opportunity(r, _coef);
                if (!double.IsFinite(o)) continue;   // ★非有限の遮断（2026-07-31・Territory 側と同じ流儀）：評価できない州は選ばない
                var (la, lo) = Places.Coord(r.Name);
                int classes = 0;
                foreach (var p in r.Pops) if (!p.IsSoldier) classes |= 1 << p.ClassId;   // MoveMigrants が移せる相手（PopOf と同じ述語）
                dst.Add((d, r, o, la, lo, _coef.ImmigrationReceiveCapFrac * r.SumPopulation() * lawCap * Territory.AbsorbCap(r, _coef), classes));
            }
        }
        int m = dst.Count;
        // 2〜4. 送出州ごと・階級ごとの流量を決める（まだ移さない）
        var plan = new List<(World Src, Region SrcR, int Cls, int X, double Flow)>();
        var toX = new double[m]; var gap = new double[m];
        long seenPairs = 0;   // 計算量の台帳（診断）：送出州×他国の州の組（(146)＝O(州²)・旧は数えず単価に入っていた）
        for (int s = 0; s < n; s++)
        {
            var src = Nations[s].World;
            foreach (var r in src.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double push = Territory.Opportunity(r, _coef) * Territory.Holding(r, _coef) * r.FoodFill;
                if (!double.IsFinite(push)) continue;   // ★非有限の遮断＝評価できない州は送出元にしない
                seenPairs += m;
                var (sla, slo) = Places.Coord(r.Name);
                double gMax = 0, wSum = 0;
                for (int x = 0; x < m; x++)
                {
                    gap[x] = 0;
                    if (dst[x].D == s) continue;
                    double g = dst[x].O - push - _coef.CrossBorderOppGapThreshold;
                    if (!(g > 0)) continue;   // 差が0以下の州へは1人も行かない
                    g *= Territory.MigrationDecay(Geo.DistanceKm(sla, slo, dst[x].Lat, dst[x].Lon), src.WorldTechLevel, _coef);   // ★時代関数化：大西洋横断が6週間→6日
                    if (!(g > 0)) continue;
                    gap[x] = g;
                    if (g > gMax) gMax = g;
                    wSum += g * dstA[dst[x].D];
                }
                if (!(wSum > 0)) continue;
                foreach (int cls in MigrantClasses)   // 農民・労働者（大衆）が移る
                {
                    var sp = r.PopOf(cls);
                    if (sp is null || !(sp.Size >= 1e-9)) continue;   // ★否定形＝NaN の Size も弾く
                    // 危機時の上限緩和：故郷の飢饉(1-FoodFill)に比例して MaxFrac を引き上げ＝アイルランド型の大量脱出。
                    // B-高5（史実検証2026-07-22）：判定を送出**州**の FoodFill へブレンド（RegionBlend=0で国平均のみ）。
                    double effFill = src.FoodFill + _coef.FaminePushRegionBlend * (r.FoodFill - src.FoodFill);
                    double srcFamine = Math.Max(0.0, 1.0 - effFill);
                    double maxFrac = _coef.EmigrationMaxFrac * (1.0 + _coef.FaminePushMaxFracBoost * srcFamine);
                    double flow = Math.Min(_coef.EmigrationRate * gMax, maxFrac) * sp.Size;
                    if (!(flow > 1e-9)) continue;   // ★否定形＝NaN の流量を弾く
                    for (int x = 0; x < m; x++)
                    {
                        if (!(gap[x] > 0) || (dst[x].Classes >> cls & 1) == 0) continue;   // その階級の POP が居ない州の分は送出州に残る
                        double f = flow * (gap[x] * dstA[dst[x].D] / wSum);
                        // R3 貧困の罠（休眠 MigrationFarePerkKm）：運賃を賄える貯蓄がある層だけ動ける。1人当たり貯蓄が運賃の2倍で全員可・運賃以下で0。
                        //   0 の間はこの分岐に入らない（距離計算もしない）。
                        if (_coef.MigrationFarePerkKm > 0)
                        {
                            var dw = Nations[dst[x].D].World;
                            double fare = _coef.MigrationFarePerkKm * Geo.DistanceKm(src.Latitude, src.Longitude, dw.Latitude, dw.Longitude) / MigrationFareDistanceUnitKm;
                            if (fare > 1e-9)
                            {
                                double perCap = sp.Size > 1e-9 ? sp.Savings / sp.Size : 0;
                                double af = (perCap - fare) / fare;
                                // 安全弁：fare/perCap が非有限（座標NaN／貯蓄NaN）ならゲートを飛ばす（割り当て据え置き）＝NaN を Size に伝播させない。
                                f *= double.IsFinite(af) ? PoliticalQuantities.Clamp01(af) : 1.0;
                                if (Simulation.ConservationProbe && (!double.IsFinite(fare) || !double.IsFinite(perCap)))
                                    Console.Error.WriteLine($"[R3診断] 非有限：{src.Region.Name}({src.Latitude},{src.Longitude})→{dw.Region.Name}({dw.Latitude},{dw.Longitude}) fare={fare} perCap={perCap} sav={sp.Savings} size={sp.Size}");
                            }
                        }
                        if (!(f > 0)) continue;
                        plan.Add((src, r, cls, x, f)); toX[x] += f;
                    }
                }
            }
        }
        Cost[CostLedger.MigrationPairs] += seenPairs;
        // 4〜5. 受入の上限で縮めてから移す（縮んだ分は送出州に残る）
        foreach (var (src, srcR, cls, x, f) in plan)
        {
            double cap = dst[x].Cap;
            double scale = toX[x] <= cap ? 1.0 : cap > 0 ? cap / toX[x] : 0.0;   // ★上限が NaN なら0の側へ倒す（fail-closed）
            double flow = f * scale;
            if (!(flow > 1e-9)) continue;
            MoveMigrants(src, Nations[dst[x].D].World, srcR, dst[x].R, cls, flow);
        }
    }

    // 越境移民の実転送：大衆（農民・労働者）の1つの階級 cls を srcR→dstR へ、決まった人数 flow だけ移す（流量は MigrateBetweenNations が決める）。
    // 保存＝人口/貨幣台帳が両国で閉じる。internal＝P12残渣②のON経路/保存則テスト用（InternalsVisibleTo・Coef と同じ流儀）。
    internal void MoveMigrants(World src, World dst, Region srcR, Region dstR, int cls, double flow)
    {
        // R3 渡航コスト：距離比例の運賃（1人あたり）。MigrationFarePerkKm=0 で fare=0＝以下の運賃ロジックを一切通らず厳密恒等（距離計算もしない）。
        // 払える割合（貧困の罠）は流量を決める側（MigrateBetweenNations の段4）が掛ける。ここは持参金から運賃を引くだけ（MoveAbroad）。
        double fare = 0;
        if (_coef.MigrationFarePerkKm > 0)
            fare = _coef.MigrationFarePerkKm * Geo.DistanceKm(src.Latitude, src.Longitude, dst.Latitude, dst.Longitude) / MigrationFareDistanceUnitKm;
        var sp = srcR.PopOf(cls);
        var dp = dstR.PopOf(cls);
        if (sp is null || dp is null || !(sp.Size >= 1e-9)) return;   // ★否定形＝NaN の Size も弾く
        if (!(flow > 1e-9)) return;   // ★否定形＝NaN の流量を弾く（旧は NaN 素通しで Size/Savings を直接汚染した）
        double moved = MoveAbroad(src, dst, sp, dp, flow, fare);
        if (!(moved > 0)) return;
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）：越境の流出/流入
    }

    /// <summary>★国境をまたぐ人の運搬の1本（2026-09-30 処理の一元化 段4e I＋人と動くお金の換算）＝越境移民（<see cref="MoveMigrants"/>）と招聘（<see cref="MoveSpecialists"/>）。
    /// 旧＝2つが同じ運搬を逐語で写していた。人数と属性は人の移し方の1本（<see cref="Pop.Blend"/>）＝移民は家族で動く（年齢と男女の構成・識字・闘争度・政治意識・国民意識・思想を携えて混ざる）・
    /// 受入国では中核民族ではない（core＝0 で希釈＝EthnicUnity の加重平均が下がる・お雇い外国人も同じ＝契約が切れれば帰る人々）。
    /// お金＝持参金（1人あたりの貯蓄）から運賃を引いた額と債務（債務は債務者について行く＝運賃で目減りしない）を**受入国の通貨へ換算**して運ぶ（移し方の1本と同じ規則
    /// ＝<see cref="World.CrossFx"/>・差は換算の台帳へ＝旧は名目のまま＝(120) の残り）。運賃は持っている正の持参金から（C17）＝送出経済に残る。
    /// 国債の保有は発行国の対外債へ振り替える（<see cref="Fiscal.BookForeignDebt"/>）＝運ばない。人口の台帳・国境の出入りの台帳（持参金−債務・各自の通貨）・家計の与信（S3b）を両国で閉じる。
    /// 戻り値＝動いた人数。</summary>
    private static double MoveAbroad(World src, World dst, Pop sp, Pop dp, double flow, double fare)
    {
        double frac = flow / sp.Size;
        double sav = sp.Savings * frac;   // 持参金＝1人あたり貯蓄ぶん
        // ★支払いの一元化 段2-3（C17・2026-09-29）：運賃は**持っている正の持参金から**だけ払う（min(max(0,持参金), 運賃×人数)）＝負の貯蓄（借越）は運賃の有無に関わらず移民に付いて行く。
        double savOut = fare > 1e-9 && double.IsFinite(fare) ? sav - Math.Min(Math.Max(0, sav), fare * flow) : sav;
        double debtOut = sp.Debt * frac;
        Fiscal.BookForeignDebt(sp, sp.BondHoldings * frac, src, dst);   // ★国債の保有台帳（2026-09-21）：発行国（src）の対外債へ振替＝受入国の対外債権
        double moved = Pop.Blend(sp, dp, flow, coreAtDest: dst.EthnicEnabled ? 0.0 : dp.CoreEthnicShare, money: false);
        if (!(moved > 0)) return 0;
        double savIn = World.CrossFx(src, dst, savOut);     // 換算（差は換算の台帳へ）
        double debtIn = -World.CrossFx(src, dst, -debtOut); // 負債は負の額として換算＝台帳の差の符号が基礎貨幣（貯蓄−与信）と揃う
        Pop.Carry(sp, dp, savOut, debtOut, 0, savIn, debtIn);   // お金（運賃を引いた持参金・債務）＝国債の保有は上で振り替えた＝運ばない
        src.PopulationDelta -= moved; dst.PopulationDelta += moved;                      // 人口台帳（両国で相殺）
        src.CumulativeTradeFlow -= savOut - debtOut; dst.CumulativeTradeFlow += savIn - debtIn;   // 持参金−債務＝対外移転（基礎貨幣の台帳が閉じる・各自の通貨）
        src.Bank!.Carry(dst.Bank, debtOut, debtIn);                                      // 家計の与信も債務について行く（S3b・受入国の通貨）
        return moved;
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
                double ban = PoliticalQuantities.Clamp01(src.SpecialistExitBan);
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
                        { var q = r.PopOf(cls); if (q is not null && q.Size > 0) sz += q.Size; }
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

    // ★段④の実転送：pool の中で階級ごとに比例配分して heads 人を動かす。保存＝人口/貨幣の台帳が両国で閉じる（国境をまたぐ人の運搬の1本 MoveAbroad）。
    private void MoveSpecialists(World src, World dst, Region srcR, Region dstR, double heads, double perHead)
    {
        double pool = 0;
        foreach (var cls in SpecialistClasses)
        { var q = srcR.PopOf(cls); if (q is not null && q.Size > 0) pool += q.Size; }
        if (!(pool > 1e-9)) return;
        double moved = 0;
        var arrived = new List<Pop>(); var arrivedFlow = new List<double>();   // 支度金の受け手＝呼ばれた本人が入った受入州の POP と、その人数（C16）
        foreach (var cls in SpecialistClasses)
        {
            var sp = srcR.PopOf(cls);
            var dp = dstR.PopOf(cls);
            if (sp is null || dp is null || !(sp.Size > 1e-9)) continue;   // ⚠受け皿が無ければ呼べない（研究者を持たない世界）
            double flow = System.Math.Min(heads * (sp.Size / pool), sp.Size);
            if (!(flow > 1e-9)) continue;
            arrived.Add(dp); arrivedFlow.Add(flow);
            // ★人的資本を運搬（⚠招聘の要点はまさにこれ＝識字と技能を持って来る）。大衆移民と同じ1本（持参金＝自分の貯蓄を持って来る・運賃なし）。
            moved += MoveAbroad(src, dst, sp, dp, flow, 0);
        }
        if (!(moved > 0)) return;
        // ★支度金＝国庫→呼ばれた本人（⚠受入国の内側で保存する。国庫が減り、その分だけ呼ばれた人の貯蓄が増える）。
        //   ★支払いの一元化 段2-3（C16・2026-09-29）：受け手＝呼ばれた本人が入った POP へ人数の比（旧＝研究者の分も含めて受入州の技師 POP へ全額）。
        //   移し方の1本（World.Transfer）＝受け手不在なら払わない（国庫に残る）。
        double cost = System.Math.Min(perHead * moved, dst.RecruitBudget) * dst.FiscalFundingRatio;   // 予約の範囲×国家の支払い率 r（2026-09-28 段2-1）
        dst.RecruitBudget -= World.Transfer((dst, null), (dst, arrived, arrivedFlow), cost, TreasuryCat.Modernization);
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）
        src.LastSpecialistExit += moved;                                     // ★段⑤：抜かれた側の実測＝流出禁止法の選好の入力（抜かれていない国では法が争点にならない）
        (_recruitLog ??= new()).Add((src, dst, moved));                      // ★段2A：発見の伝わりの技師の移動（同じ四半期末の発見の段が読んで空にする）
    }

    // 【削除2026-09-30 段4e I】EffectiveConsciousness（越境移民の国民意識の未捕捉〔−1〕を国の実効値で読む）＝人の移し方の1本 Pop.Blend は未捕捉を重み0で混ぜる
    //   （書き手が触らない POP は読み手も見ない＝World.NationalConsciousness と同じ規則・国内の移動も同じ1本）。

    public int Year => Nations.Count > 0 ? Nations[0].World.Year : 0;

    // ★計算量の台帳（2026-09-30 作者裁定「計算量の予算」）＝共有層の台帳（四半期末の共有層）と、消えた国の台帳の受け皿。
    //   ⚠作業領域＝セーブに載らない（[NonSerialized]＝SaveGame が辿らない）・シミュの値はどれも読まない。
    [System.NonSerialized] private long[]? _cost;
    [System.NonSerialized] private long[]? _costDeparted;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];

    /// <summary>★この試合の計算量の台帳の合計（<see cref="CostLedger"/> の添字）＝共有層（この層・外交・国際市場）＋今の全ての国＋再併合で消えた国。
    /// 読むだけ＝軌道に触れない。ロードした盤面はロードした時点から数え直す（台帳はセーブに載らない）。</summary>
    public long[] CostTotals()
    {
        var t = new long[CostLedger.Count];
        CostLedger.AddInto(t, _cost); CostLedger.AddInto(t, _costDeparted);
        CostLedger.AddInto(t, Diplomacy.CostOrNull); CostLedger.AddInto(t, Market.CostOrNull);
        foreach (var n in Nations) CostLedger.AddInto(t, n.World.CostOrNull);
        return t;
    }
}

/// <summary>★計算量の台帳（2026-09-30 作者裁定「計算量の予算」＝推奨で採択）。処理ごとの「見た件数」を数える**診断**であって、シミュの状態ではない。
/// ── 規律 ──────────────────────────────────────────────────────────────
/// ・セーブに載らない＝置き場は <c>[NonSerialized]</c> の作業領域（World・Diplomacy・InternationalMarket・MultiNationSim の <c>_cost</c>）＝SaveGame はこの印のフィールドを辿らない。
/// ・軌道を1ビットも変えない＝件数を読む処理はシミュの中に1つも無い（読むのは <see cref="MultiNationSim.CostTotals"/> を呼ぶ Runner とテストだけ）。
/// ・並列の日次相（国内価格・建設・信用）は国ごとの World の台帳へ＝共有の変数を取り合わない。共有層（清算・外交・四半期末）はその層の台帳へ。
/// ・熱いループの中で1件ずつ台帳へ足さない＝ループの前（件数が分かるとき）か、局所の数をループの後にまとめて足す。
/// ── 推定の計算量 ──────────────────────────────────────────────────────
/// 推定（秒）＝Σ 件数×単価。単価は census（2026-09-30・S3b の木 61b751e・1種全長・計器つき）の処理ごとの秒÷件数を、計器の重さ（計器なし 472.4 秒÷計器つき 515.55 秒）で割り戻した値。
/// 覆う割合＝推定÷実測の CPU。★下がったら数えていない重い処理が増えた合図（新しい処理はこの台帳に行を足し、docs の「処理の刻みの表」に刻みの根拠を書く）。
/// ⚠ 外交の覇権の判定の単価だけは census に内訳が無い＝この回の計器（S3c の木・全長1試合で覇権の判定 11.17 秒）から。
/// ⚠ 判定は推定（決定的）を主にし、実測の CPU は記録（時刻と負荷で揺れる＝約7%）。</summary>
public static class CostLedger
{
    public const int PriceGoods = 0, PriceDailyRuns = 1, ConstructionSeen = 2, CreditSpans = 3, ConsumePopGoods = 4, ProducerMonths = 5,
        ClaimProducers = 6, MonthlyPops = 7, ClearingRows = 8, ClearingFills = 9, DiploPowerMonths = 10, DiploHegemonScans = 11,
        QuarterStartPops = 12, QuarterEndPops = 13, SharedNationQuarters = 14, GdpScans = 15, MigrationPairs = 16, DiscoveryNations = 17, ThreatReachPairs = 18, DiscoveryPriceNations = 19, Count = 20;

    /// <summary>計数の名前（番人の表・測りのファイル・docs の「処理の刻みの表」の列が同じ名前を使う）。</summary>
    public static readonly string[] Names =
    [
        "価格：財の歩（州×財×旬）", "価格：日次の写像の回", "建設：見た生産者（旬）", "信用：国×旬", "消費：POP×財（月）", "生産：生産者×月",
        "請求：見た生産者（月）", "月次：POP×月（決算の残り）", "清算：見た買い手行", "清算：着値の充填", "外交：国×月", "外交：覇権の判定の国×国",
        "四半期首：POP×国（AI・議会・政府）", "四半期末：POP×国", "共有層：国×四半期（国力・移民・国際金融ほか）",
        "GDP：州×生産者の和（取り直した分）", "移民：州×州の組（国内・越境）", "発見：国×四半期（点数の判定）", "外交：脅威の届き方（脅威を持つ国×国）",
        "発見：平滑化した値段の歩（国×四半期）",
    ];

    /// <summary>単価（マイクロ秒/件）。census の処理の秒÷件数×計器の重さの割り戻し（0.9163）。算出の式は docs の「処理の刻みの表」。
    /// ★2026-09-30 (136)：GDP の和（World.Gdp の州×生産者の走査）を5本の計数（外交・四半期首・四半期末・共有層・月次）の中から1本へ分けた＝
    /// 5本の単価はその区間の中の GDP の時間の割合（腕A＝(135) の木の census・種1全長）だけ下げ、分けた計数の単価は同じ census の GDP の時間を走査の件数で割った値
    /// （5本から外した推定の和 16.09 秒÷その区間の走査 10.38億件）。⚠これまで数えていなかった GDP の走査（試合の計器・日次の信用の月の定数）も数える＝腕A の推定は約 +7 秒。
    /// ★2026-10-01 (146)：恒等の手（立場の土台の分け合い・連鎖の目の読む個体・変名の判定の順・従属国の列挙の近道）で3本（四半期首・外交・覇権の判定）の単価を
    /// 同じ負荷の組の census（腕A と (146) の木を続けて単独で・種1全長・計器つき）の区間の時間の比だけ下げた＋移民の組の走査（国内＝州×州・越境＝州×他国の州）を
    /// 2本（月次・共有層）の単価の中から1本の計数へ分けた（(136) の GDP と同じ分け方＝分けた分の推定は種1 で差し引き0）。
    /// ★2026-10-03 (186)：眠った生産者（発見の門が0・EconomyStep.SleepState）を「生産：生産者×月」に数えない＝件数は −19%・Run の残りの実時間は −4.1%（同じ負荷の組の census 5対・標準誤差 2.2%）
    ///   ＝眠った生産者は平均より軽かった＝単価を時間の比×件数の比で取り直した（件数だけ減らすと推定が実時間の約5倍下がる＝覆う割合が下がる型）。
    ///   数えていなかった Discovery.AdvancePrice に計数「発見：平滑化した値段の歩」を足した（推定 +0.8 秒）。</summary>
    public static readonly double[] UnitMicros =
    [
        0.24025,   // 価格：財の歩＝（価格の秒 170.688−日次の経路 50.936−死んだ財 13.511）÷死んでいない財の歩 4.052億（死んだ財の歩は安い＝この単価は死んだ財を回す木では推定を過大にする）
        0.14406,   // 価格：日次の写像の回＝日次の経路 50.936 秒÷3.240億回
        0.01300,   // 建設：見た生産者＝48.618 秒÷（走査 25.45億＋工事のある州の生産者 530.9万州×166）
        2.58278,   // 信用：国×呼び出し＝11.040 秒÷391.7万
        0.03012,   // 消費：POP×財＝消費の段 38.445 秒÷11.70億
        0.54106,   // 生産：生産者×月＝EconomyStep.Run の残り（92.455−38.445 秒）÷1.084億＝0.45650×（(186) 眠った生産者を数えない＝回した生産者だけ・同じ負荷の組の census の Run の残りの比 0.9590×件数の比 1.23590）
        0.03490,   // 請求：見た生産者＝4.129 秒÷1.084億
        1.73157,   // 月次：POP×月＝月次決算の EconomyStep と請求の外 18.438 秒÷847.5万（州×月の POP）＝1.99345×(1−GDP の割合 0.09941)（(136)）＝1.79528×(1−国内の移民の割合 0.03549)（(146)＝移民の組を下の計数へ分けた）
        0.19264,   // 清算：見た買い手行＝準備・リスト・財の終わり 16.086 秒÷7,651万行
        0.11034,   // 清算：着値の充填＝買い手行の段 62.746 秒÷5.211億
        84.89478,  // 外交：国×月＝（外交 32.057−覇権の判定 11.17 秒）÷16.9万＝113.26019×(1−GDP の割合 0.12321)（(136)）＝99.30487×（外交−覇権の区間の時間の比 10.410÷12.177＝0.85489）（(146)＝変名の判定の順・従属国の列挙の近道）
        0.004705,  // 外交：覇権の判定の国×国＝11.17 秒÷（13.56万回×85.4²）＝この回の計器（inj_dp・S3c の木）＝0.01035×（覇権の判定の時間の比 1.277÷2.809＝0.45461）（(146)＝従属国の列挙の近道）
        3.36578,   // 四半期首：POP×国＝20.073 秒÷282.5万＝6.51065×(1−GDP の割合 0.31550)（(136)）＝4.45657×(1−減った割合 0.24476)（(146)＝立場の土台〔Build〕の分け合い・連鎖の目の読む個体＝議会・政府・AI の判断の3つの部品の差 4.321 秒÷区間 17.654 秒）
        1.05863,   // 四半期末：POP×国＝4.036 秒÷282.5万＝1.30907×(1−GDP の割合 0.19131)（(136)）
        110.34900, // 共有層：国×四半期＝12.194 秒÷4.35万＝256.74759×(1−GDP の割合 0.50369)（(136)）＝127.42606×(1−越境移民の割合 0.13402)（(146)＝移民の組を下の計数へ分けた）
        0.015503,  // GDP：州×生産者の和（取り直した分）＝上の5本から外した GDP の推定 16.09 秒÷その区間の走査 10.38億件（(136) の census）
        0.016708,  // 移民：州×州の組（国内・越境）＝上の2本（月次・共有層）から外した移民の推定 1.3173 秒÷種1全長の組 7,884万（(146) の census）
        DiscoveryUnitMicros,   // 発見：国×四半期＝Discovery.Evaluate の1回（州×財の集計・生産者の走査・行の3項）＝(147) の discprobe の実測（下の定数の注記）
        ThreatReachUnitMicros,   // 外交：脅威の届き方＝Diplomacy.ThreatReach の1組（同盟と従属の照会・投射・有理飽和）＋国境の表（月3回と四半期1回＝1回 0.046ms・1試合 約0.1秒＝この単価の中に含めない小さい分）＝(151) の実測（下の定数の注記）
        DiscoveryPriceUnitMicros,   // 発見：平滑化した値段の歩＝Discovery.AdvancePrice の1回（州×財の集計・瞬間の値段・平滑）＝(186) の census（下の定数の注記）
    ];
    /// <summary>★(186) 平滑化した値段の歩の単価（マイクロ秒/国×四半期）＝Discovery.AdvancePrice（判定の段の頭・国ごとに1回）を Stopwatch で計った和÷回数
    /// （census の計器 inj_c186・種1・668四半期・r182 の木〔586b80e〕5回＝19.87／19.61／19.01／19.49／20.02 µs・眠りの木5回＝19.89／19.59／19.42／19.62／19.81 µs の平均 19.63）。
    /// ⚠ (159)(165) から「共有層：国×四半期」の中の小さい分と書いていたが、その単価（(136)(146) の census）は AdvancePrice の前に取った＝数えていなかった（(182) の申し送り）。</summary>
    public const double DiscoveryPriceUnitMicros = 19.63;
    /// <summary>発見の判定の単価（マイクロ秒/国×四半期）＝(147) の Runner discprobe（種1・668四半期・全ての国の Discovery.Evaluate を Stopwatch で計った和÷回数＝76.0・41,373回〔割高さの参照を投入の費用へ直した後の木〕・直す前の木は 67.7）。
    /// ★(165) 91.0＝同じ計り方（種1・668四半期・41,468回・単独）＝比べる相手を新しいレシピで作る費用へ（門の開いたレシピの一覧・値段の帳＝市場の死んだ財を作る費用で読む繰り返し）。
    /// 世界の平滑化した値段（国×財の和・四半期に1回）は「共有層：国×四半期」の中（1回 約8千の積和＝小さい分）。
    /// ★(169) 98.1＝(165) の 91.0×1.078（線M のバッチと重なって単独で計れない＝同じ時刻に r167 の木と (169) の木を discprobe 1 200 で交互に2回ずつ＝r167 51.3・50.1µs／(169) 55.6・53.7µs の比）
    ///   ＝動力の行の自己費用・機関車の資本の費用（線路と機関車の費用）・市場の死んだ財の着値（売り手ごとの距離と運賃＝呼ばれた財だけ）の分。</summary>
    public const double DiscoveryUnitMicros = 98.1;
    /// <summary>★脅威の届き方の単価（マイクロ秒/組）＝(151) の実測（61カ国の開幕の盤面〔助走の後に1四半期〕で Diplomacy.ThreatReach を全ての組×20回 Stopwatch で計った平均 0.535µs・Release・テストの過程の中）。
    /// 脅威の緊張（Diplomacy.ThreatTension＝月次〔四半期末の外交3回〕）が脅威を持つ国×経済を持つ国の組ごとに1回呼ぶ＝件数は「脅威を持つ国×国×月」。</summary>
    public const double ThreatReachUnitMicros = 0.535;

    /// <summary>★予算の目標（1試合の CPU 秒・バッチの条件）＝10種の全長を5分で＝300 秒×働くコア 約12.9÷10（2026-09-30 作者裁定・推奨で決めた）。</summary>
    public const double BudgetTargetCpuSeconds = 387.0;
    /// <summary>★予算の上限（推定の計算量・1試合の秒）＝今の値（速くなるたびに下げるだけ＝目標に届くまで）。⚠超えるコミットは、どこで払ったかを実装ログとコミットに書く。
    /// 0＝未設定。判定は <see cref="BudgetSlack"/> の許し（軌道の小さな揺れ）つき。
    /// 2026-09-30 (116)＝計算量の予算 第1回（恒等の手の後）の全長10種の1試合の平均の推定＝297.2 秒（手の前 356.9 秒）。
    /// 2026-09-30 第2回（近似の手＝清算の不足で起きたが成約しない行を眠らせる・張り付いた財を月の残り眠らせる）の後＝288.3 秒（−3.0%）。
    /// 2026-09-30 (123) 財の削減（段1＝138→127財・合わせた main (122) の 293.6 秒から）の後＝276.3 秒（−5.9%＝価格の財の歩 −7.2%・清算の着値の充填 −13.2%・生産 −6.7%）。
    /// 2026-09-30 (130) 清算の眠りの切り分け（月の清算は潜在量を書かない＋買い手の容量が尽きた行は回さない・合わせた main (128) の 281.1 秒から）の後＝269.3 秒（−4.2%＝清算の着値の充填 −21.1%・日次の写像の回 −2.7%）。
    /// 2026-09-30 (132) 張り付いた財の閉じた式（帯の端の不動点の近くにいる旬を日次の写像を回さず1回の式で進める）の後＝255.1 秒（−5.3%＝日次の写像の回 −36.7%）。
    /// 2026-09-30 (136) GDP の和の使い回し（恒等・台帳は GDP の走査を1本に分けた）の後＝推定 236.7 秒（(135) の木を同じ台帳で数えれば約 258.5 秒）＝**目標 248.5 秒に届いた**
    /// ＝上限は目標へ（目標より下の余りは機能が使ってよい＝README §1-2）。⚠ここから先は超えるコミットが払う（どこで払ったかを書く）。
    /// 2026-10-03 (186)：r182 の 248.7 秒（上限を 0.2 秒超え）を眠った生産者（EconomyStep.SleepState・近似）で払った＝「生産：生産者×月」の件数 −19%・単価は census の時間の比で取り直し
    /// ＋数えていなかった Discovery.AdvancePrice を数えた（+0.8 秒）＝全長の10種の推定は実装ログ (186)。上限は変えない。</summary>
    public const double BudgetLimitEstSeconds = 248.5;
    /// <summary>上限を置いたときの覆う割合（推定÷実測の CPU・バッチの条件）＝目標を推定の単位へ写す（目標の推定＝目標の CPU×これ）。0＝未設定。
    /// 2026-09-30 (116)＝単独の10種（(115)・計器なし・他の実行なし）の1試合 555.6 CPU 秒に対する同じ軌道の推定 356.9 秒＝0.642
    /// （単価は単独の1試合の秒＝バッチでは SMT の取り合いで CPU 秒が膨らむ）⇒ 目標の推定＝387×0.642＝248.5 秒。
    /// ⚠第2回は取り直していない（別のセッションのバッチ2本が並び続け、単独の10種を回せなかった＝負荷の下の覆う割合は 0.54〜0.61 で動く）。</summary>
    public const double BudgetCoverageAtLimit = 0.642;
    /// <summary>予算の判定の許し（軌道の小さな揺れ＝番人と同じ +2%）。</summary>
    public const double BudgetSlack = 0.02;

    public static void AddInto(long[] total, long[]? part)
    {
        if (part is null) return;
        for (int i = 0; i < total.Length && i < part.Length; i++) total[i] += part[i];
    }

    /// <summary>推定の計算量（秒）＝Σ 件数×単価。</summary>
    public static double EstimatedSeconds(long[] n)
    {
        double s = 0;
        for (int i = 0; i < Count && i < n.Length; i++) s += n[i] * UnitMicros[i] * 1e-6;
        return s;
    }
}
