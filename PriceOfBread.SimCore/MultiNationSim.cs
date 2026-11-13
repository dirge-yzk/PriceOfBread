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
    /// <summary>
    /// 地図が**実際に使われた航路**を太さ付きで描くための半減距離km。〔履歴 MultiNationSim.cs#1〕
    /// </summary>
    public double TradeGravityHalfKm => _coef.RegionalDistanceHalf;   // TradeGravityHalfKm〔履歴 MultiNationSim.cs#2〕
    private int _quarter;
    private bool _calendarFrozen;   // 開始前の助走中＝共有層の四半期カウンタも止める（World.WarmupFrozen と対。WarmupEconomy だけが立てて必ず戻す）
    private int _nextPowerId;   // ★建国スポーンの採番（wave2b是正）：Diplomacy.Powers.Count は RemovePower 後に既存Idと衝突する（再併合で欠番が生まれても番号を再利用しない・単調増加）

    // マジックナンバー命名（挙動不変・値は据え置き）。中央 Coefficients には入れない当ファイル固有のハードコード値。
    private const double DefaultTradeWeightBase = 0.15;              // 交易比重の基準値（初期国の基数・分離独立国の既定）
    private const double RumpGdpFractionOfParent = 0.01;             // 残骸国家判定＝親GDP比のこの割合未満が続いたら再併合対象
    private const double EpidemicTransmitSeverityFloor = 0.5;        // 国家間伝播で発症した疫病の重症度の下限（上限は+NextDouble()で1.5まで）

    // splitmix64（Diplomacy.Hash01と同系の混合）：国別乱数種の生成（他16）。
    /// <summary>
    /// 旗 0＝州の数で割る（今の形）／旗が正＝票の和で割る（和が正でなければ 0＝比が不正の扱い）。〔履歴 MultiNationSim.cs#3〕
    /// </summary>
    internal static double SilverMeanRatio(bool weighted, double lnSum, int count, double weightedLnSum, double weightSum)
        => weighted ? (weightSum > 1e-9 ? System.Math.Exp(weightedLnSum / weightSum) : 0) : (count > 0 ? System.Math.Exp(lnSum / count) : 0);

    /// <summary>
    /// 2 つの積をそれぞれ非有限・非正なら 0（否定形）にしてから小さい方 （片側が無い州は票なし）。〔履歴 MultiNationSim.cs#4〕
    /// </summary>
    internal static double SilverMinRefWeight(double demandAg, double demandAu, double refAg, double refAu)
    {
        double sideAg = demandAg * refAg, sideAu = demandAu * refAu;
        if (!(sideAg > 0 && sideAg < double.PositiveInfinity)) sideAg = 0.0;
        if (!(sideAu > 0 && sideAu < double.PositiveInfinity)) sideAu = 0.0;
        return sideAg < sideAu ? sideAg : sideAu;
    }

    private static ulong MixSeed(ulong seed, ulong i)
    {
        ulong x = seed + 0x9E3779B97F4A7C15UL * (i + 1);
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    /// ⚠**構築時に走る機構**（植民地の州昇格 PromoteAllColonies）があるので、構築後に w.Coef を書き換えても間に合わない場合は、構築の前に係数を渡す口がここ。〔履歴 MultiNationSim.cs#5〕
    public MultiNationSim(IReadOnlyList<NationConfig> configs, ulong seed, Func<NationConfig, INationAgent>? agentFactory = null, Coefficients? coef = null)
    {
        _coef = coef ?? new Coefficients();
        _seed = seed;
        Rng = new DeterministicRng(seed ^ 0x9E37_79B9);   // 外交用の独立系列（各国の Rng とは別）
        _weatherRng = new DeterministicRng(seed ^ 0xC11A7E_5EEDUL);   // 緯度帯天候の独立系列
        Diplomacy.UnificationRng = new DeterministicRng(seed ^ 0xF0E1_D2C3_B4A5UL);   // P4 統一戦争の専用系列（既存系列のdraw数を乱さない・主フラグOFFでは未消費）
        Diplomacy.SubmitRng = new DeterministicRng(seed ^ 0xA5B4_C3D2_E1F0UL);        // ★屈服の確率化（AI審査3-1）の専用系列（SubmitProbabilistic=0では未消費＝恒等）
        // Colony.Population（質量）＝住民数÷これ。〔履歴 MultiNationSim.cs#6〕
        {
            double sumK = 0; int nCol = 0;
            foreach (var cfg0 in configs) foreach (var c0 in cfg0.InitialColonies) { sumK += c0.PopK; nCol++; }
            foreach (var t0 in WorldData.Setup().Targets) foreach (var c0 in t0.Colonies) { sumK += c0.PopK; nCol++; }
            Diplomacy.ColonyUnitPop = nCol > 0 && sumK > 1e-9 ? sumK / nCol : 1.0;
        }
        Market = new InternationalMarket(Catalog.Goods.Length);   // 0で全財1.0の旧初期化〔履歴 MultiNationSim.cs#7〕
        for (int i = 0; i < configs.Count; i++)
        {
            var cfg = configs[i];
            // agentFactory 省略時は NullAgent（放置＝検証ハーネス用の後方互換）。〔履歴 MultiNationSim.cs#8〕
            var sim = new Simulation(cfg.ToScenario(MixSeed(seed, (ulong)i), sharedWorld: true), agentFactory?.Invoke(cfg) ?? new NullAgent());
            var power = new Power
            {
                Id = i, Name = cfg.Name, BaseName = cfg.Name, Nation = sim.World,
                // ⚠旧は手置きの数を入れていたが、最初の Diplomacy.Step が兵士POPから作り直すので、   **1783年の画面だけが別のスケール**を映していた（実測＝英450→翌年20＝20倍の段差）。〔履歴 MultiNationSim.cs#9〕
                Military = sim.World.SoldierSizeK() / _coef.SoldiersPerMilitary,
                GdpPerMonth = sim.World.GdpPerMonth,
                TradeWeight = DefaultTradeWeightBase,   // TradeWeight〔履歴 MultiNationSim.cs#10〕
            };
            foreach (var (cname, cres, cpopK) in cfg.InitialColonies) { var cc = Geo.ColonyCoord(cname); power.ColonyList.Add(new Colony { Name = cname, Resources = [.. cres], Population = cpopK / Diplomacy.ColonyUnitPop, PopSize = cpopK, Latitude = cc.Lat, Longitude = cc.Lon }); }   // 名前＋固有資源＋人口規模＋座標（史実1780）。住民数は人口ウェイト×換算＝作成時の1か所（回1-1・col-01）
            sim.World.Diplomacy = Diplomacy;     // 読み取り用（植民地アクセス・交易）
            sim.World.SelfPower = power;
            // ⚠既定0のままだと、AIだけが毎ステップ自分で設定し、NullAgent のプレイヤー国は**永久に0**＝   国家による工業化が一度も起きない（実測でQ24に14倍の差）。〔履歴 MultiNationSim.cs#11〕
            sim.World.ModernizationFrac = UtilityAgent.OpeningModernizationFrac;
            sim.World.StepsDiplomacy = false;    // 共有外交は本クラスが進める
            sim.World.CoercionFollowsForce = true;   // 較正/単体は単一 Simulation＝既定 false で旧挙動〔履歴 MultiNationSim.cs#12〕
            Diplomacy.Powers.Add(power);
            Nations.Add(sim);
            // 征服割譲で他国州を得ると持ち込み人口（AnnexBaggage）が記帳され PopGrowth の分子から除かれるが、 故地への復帰（OriginPowerId 一致＝再征服・回復）は記帳0＝清の太平天国回復型が帯へ戻れる。〔履歴 MultiNationSim.cs#13〕
            foreach (var r0 in sim.World.Regions) r0.OriginPowerId = power.Id;
        }
        WireTransitPorts();   // WireTransitPorts〔履歴 MultiNationSim.cs#14〕
        // 能動侵略の抽象標的（段C・案Y）：Nation=null・Gdp=0＝ThreatTension を生まず自動では緊張も溜まらない＝Invade 動詞でのみ取れる。〔履歴 MultiNationSim.cs#15〕
        var setup = WorldData.Setup();
        foreach (var t in setup.Targets)
        {
            var p = new Power { Id = Diplomacy.Powers.Count, Name = t.Name, BaseName = t.Name, Military = t.Military, GdpPerMonth = new RealGdpPerMonth(0), TradeWeight = 0.0, RequiresQuinine = t.Quinine };
            foreach (var c in t.Colonies) { var cc = Geo.ColonyCoord(c.Name); p.ColonyList.Add(new Colony { Name = c.Name, Resources = [.. c.Resources], Population = c.PopK / Diplomacy.ColonyUnitPop, PopSize = c.PopK, Latitude = cc.Lat, Longitude = cc.Lon, Malarial = t.Quinine }); }   // 熱帯(Quinine)標的の州はマラリア地＝征服で本国へ風土病が付いて回る。〔履歴 MultiNationSim.cs#16〕
            Diplomacy.Powers.Add(p);
        }
        _nextPowerId = Diplomacy.Powers.Count;   // ★建国スポーンはここから単調採番（起動時のId=Powers.Countの連番と同じ値で始まり、以後は欠番があっても戻らない）

        SeedRivalries(setup);   // 初期の史実ライバル（必要悪：これが無いと ThreatTension/Ideology だけでは戦争が点火せず＝load-bearing な初期条件）
        SeedDependencies(setup);   // 初期の史実従属（属国一本化）＝シャムの朝貢属国・ムガル傀儡

        // 海峡・運河（§B）：自然海峡は開通済み、運河は host 保有国の資本家が建設して開通。〔履歴 MultiNationSim.cs#17〕
        foreach (var cp in setup.Chokepoints)
        {
            Diplomacy.Chokepoints.Add(new Chokepoint
            {
                Id = cp.Id, Name = cp.Name, Host = cp.Host, IsOpen = cp.IsOpen, IsCanal = cp.IsCanal,
                BuildCost = cp.BuildCost, TechGate = cp.TechGate, ServesBasins = [.. cp.ServesBasins],
                ExitOfBasin = cp.ExitOfBasin is { } eb && System.Enum.TryParse<Basin>(eb, out var ebv) ? (int)ebv : -1,   // 13l地政学：閉鎖海盆の出口（データ側が真実源・不正名は-1＝該当なしへ倒す）
            });
        }

        // 国家ランク（列強/中堅/弱小）を開始盤面のスコアで初期化。Diplomacy.Step は四半期末にしか走らないため、
        // これが無いと最初の四半期が締まるまで RankOf が既定値=弱小を返し、UI で全国が「弱小」に見える（純関数＝ダイナミクス不変・四半期末に再計算で上書き）。
        Diplomacy.ComputeRanks(_coef);

        // ⚠植民地総数の基準線を取る**前**に走らせる＝昇格した分は最初から植民地でない（保存検査の基準線と整合）。〔履歴 MultiNationSim.cs#18〕
        PromoteAllColonies();

        // InitialColonyCount〔履歴 MultiNationSim.cs#19〕
        foreach (var p in Diplomacy.Powers) Diplomacy.InitialColonyCount += p.Colonies;
    }

    /// <summary>
    /// 入植者昇格（Colonial.Step）と**同じ動詞**を通す ＝人口・蓄え・資源賦存・気候ゲート・保存の記帳を二重定義しない。〔履歴 MultiNationSim.cs#20〕
    /// </summary>
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
                var reg = Simulation.PromoteColonyToRegion(w, _coef, col, RegionTier.Colonial);
                // 途中の接収（Diplomacy.cs:2269）は 0 のまま＝実際に取った年から。〔履歴 MultiNationSim.cs#21〕
                if (_coef.OpeningColonyDominanceFull > 0) reg.Dominance = 1.0;
            }
        }
    }

    /// <summary>
    /// ・純地理（PathGraph は国境を知らない）＝構築時に1回だけ計算。〔履歴 MultiNationSim.cs#22〕
    /// </summary>
    // 触発の仕方＝**呼び出し忘れが起きない形**にする。〔履歴 MultiNationSim.cs#23〕

    /// <summary>
    /// 港の配線を盤面から張り直す（四半期末に1回・無状態＝doc 20260816 案1）。〔履歴 MultiNationSim.cs#24〕
    /// </summary>
    private void RewirePorts()
    {
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
        // landMatrix〔履歴 MultiNationSim.cs#25〕
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

    // 保存則＝割譲（Diplomacy.CedeTerritory）と同型の両建て記帳。〔履歴 MultiNationSim.cs#26〕
    internal void SpawnSecededNations()
    {
        int count0 = Nations.Count;   // 走査前の国数を固定（追記しても再走査しない）
        for (int i = 0; i < count0; i++)
        {
            var pw = Nations[i].World;
            // SpawnNation（同じ建国路）＋内戦タグ付き開戦。〔履歴 MultiNationSim.cs#27〕
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
                    // 本拠州が同じ四半期に離れられなくなった（割譲・首都への昇格）＝蜂起は地図の上に立たなかった＝運動の参照を外す（燃え尽きではない）。〔履歴 MultiNationSim.cs#28〕
                    else if (pw.RevoltingFaction is { } stillborn && rebelRegs.Contains(stillborn.Region)) pw.RevoltingFaction = null;
                }
                continue;   // 同じ親から同四半期に2国は作らない（他レーンの予約は翌期へ持ち越し）
            }
            // 首都（Id0）は出ない・親に州が1つしか残らないなら出ない（首都不変条件）。〔履歴 MultiNationSim.cs#29〕
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

    /// <summary>
    /// ★建国路の1本＝反乱国家の建国（呼び手が直ちに内戦タグの戦争を開く＝講和相当の状態は張らない・ログも開戦側）。〔履歴 MultiNationSim.cs#30〕
    /// </summary>
    private void SpawnNation(Simulation parent, System.Collections.Generic.IReadOnlyList<Region> regs, Polity? polity)
    {
        var reg = regs[0];
        var pw = parent.World;
        var parentPower = pw.SelfPower!;   // SpawnSecededNations 側で非nullゲート済み

        // C案の「守備兵は寝返らない」も常に寝返らない側の絶対規則＝どちらも非歴史的最適解の型。〔履歴 MultiNationSim.cs#31〕
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
        double T = Math.Max(0, pw.Treasury.Value) * (parentPop > 1e-9 ? P / parentPop : 0);   // 国庫の人口比分与（新国家の運転資金）

        // ⚠格と支配度は独立が確定するまで動かさない（RegisterIndependence／内戦の白紙講和で本土へ）＝親が勝てば元の格のまま戻る。〔履歴 MultiNationSim.cs#32〕
        double railIn = 0, shipIn = 0;   // railIn〔履歴 MultiNationSim.cs#33〕
        foreach (var rr in regs) { var (rlMoved, shMoved) = Territory.TransferRegion(pw, null, rr, Territory.Rehoming.Seceded); railIn += rlMoved; shipIn += shMoved; }
        // 国庫の分与は移し方の1本で世界の外へ（段4f）＝親の国庫から引き、同じ額を国境の出入りの台帳へ（子は生まれの基礎貨幣 S＋T で基線を取り直す＝下の注）。
        World.Transfer((pw, null), (null, null, null), T, TreasuryCat.OtherOut);
        // Count〔履歴 MultiNationSim.cs#34〕

        // --- 3) 新世界の州Id＝0が首都（World.Region=Regions[0] 規約）---
        for (int k = 0; k < regs.Count; k++) regs[k].Id = k;
        // 首都の蜂起は寝返った分の武器庫（段0）だけを持って立ち、分離独立の州は丸腰から買う。〔履歴 MultiNationSim.cs#35〕
        reg.ArmsStock ??= new System.Collections.Generic.Dictionary<int, double>();

        // 表は SecessionNames の1か所。〔履歴 MultiNationSim.cs#36〕
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
            SharedWorld = true,   // SharedWorld〔履歴 MultiNationSim.cs#37〕
            Seed = MixSeed(_seed, (ulong)Nations.Count), StartYear = pw.Year, Kind = ScenarioKind.Nation,
            NationConfig = cfg, PrebuiltRegions = [.. regs], Coef = pw.Coef,   // 係数は親インスタンスを共有（テスト/実験の書き込みが分裂しない）
            PoliticsEnabled = true, InitialPolity = polity ?? pw.Polity, FiscalEnabled = true,
            TaxRate = pw.TaxRate.Value, InitialPollTax = pw.PollTax, InitialLandTax = pw.LandTax,   // ⚠建国は1783年の税の段（NationConfig.PollTaxStage／LandTaxStage）を読まない＝親の率のまま（法の継承 World.CopyLawsFrom と同じ値）
            InitialTreasury = T, BaseCoercion = pw.BaseCoercion, EducationLaw = pw.EducationLaw,
            EducationSpending = pw.EducationSpending, ExternalPressure = pw.ExternalPressure,
            InitialInterestRate = pw.InterestRate, InitialTariff = pw.Tariff,
            InitialSuffrage = pw.Suffrage, InitialCensorship = pw.Censorship,
            InitialCornLaws = pw.CornLaws, InitialSerfEmancipation = pw.SerfEmancipation,
            InitialImmigration = pw.ImmigrationRestriction,   // 移民制限法の継承（E7・独立は制度を巻き戻さない＝他の法と同じ規則）
            InitialConscription = pw.ConscriptionLaw,         // InitialConscription〔履歴 MultiNationSim.cs#38〕
            InitialMonetaryLaw = pw.MonetaryLaw,              // 通貨制度の継承（F6・金の親から独立した国は金で始まる＝同じ規則）
        };
        // sim〔履歴 MultiNationSim.cs#39〕
        var sim = new Simulation(scenario, new UtilityAgent()) { TelemetryEnabled = parent.TelemetryEnabled };
        // 新国家の構築（上）は台帳の立っているPOPに起票しない＝ここで0へ落ちる。〔履歴 MultiNationSim.cs#40〕
        sim.World.ExchangeRate = pw.ExchangeRate;
        foreach (var rr in regs) foreach (var p in rr.Pops) Fiscal.BookForeignDebt(p, p.BondHoldings, pw, sim.World);

        // --- 5) Scenario に載らない現在値の継承（技術・法・行政の到達＝独立は文明を巻き戻さない。正統性は既定＝新政権の蜜月）---
        var nw = sim.World;
        // ここに +S+T を足すと二重計上でプローブが即例外（検証済み）。〔履歴 MultiNationSim.cs#41〕
        nw.TechLevel = pw.TechLevel; nw.Elite = pw.Elite;
        // ★発見は親から（段2A・独立は文明を巻き戻さない＝技術水準と同じ）。移った州は新しい国の配列を読む（月次の写しを待たない）。
        System.Array.Copy(pw.Discovered, nw.Discovered, System.Math.Min(pw.Discovered.Length, nw.Discovered.Length));
        nw.WorldDiscovered = pw.WorldDiscovered;   // 世界の解禁の和は全ての国で同じ配列
        foreach (var rr in regs) rr.Discovered = nw.Discovered;
        Transport.Receive(nw, railIn, shipIn, pw.ShipSteamShare.Value);   // Receive〔履歴 MultiNationSim.cs#42〕
        // 政体の含意する参政権・報道の床は構築時と同じ規則で畳み直す（写した親の値が床を下回らないように）。〔履歴 MultiNationSim.cs#43〕
        nw.CopyLawsFrom(pw);
        Politics.RaiseFoundingInstitutions(nw);
        nw.BornOfSecession = true;   // P9：独立戦争の危機乗数（レヴェ・アン・マス）の判定用（分離で生まれた国＋交戦中）
        // Coercion〔履歴 MultiNationSim.cs#44〕
        nw.Coercion = pw.Coercion;
        nw.Quarter = pw.Quarter;

        // --- 6) Power 生成（コンストラクタと同型）＋共有外交への接続 ---
        var power = new Power
        {
            Id = _nextPowerId++, Name = name, BaseName = name, Nation = nw,   // ★Powers.Count でなく単調カウンタ（wave2b是正）＝ReabsorbNations の RemovePower で欠番が出ても既存Idと衝突しない
            Military = parentPower.Military * (parentPop > 1e-9 ? P / parentPop : 0),   // 初期表示値のみ（次の Diplomacy.Step が兵士POPから再導出＝実体は移転した兵士）
            GdpPerMonth = nw.GdpPerMonth, TradeWeight = DefaultTradeWeightBase,
            Culture = reg.Culture, Religion = reg.Religion,   // 次の毎月同期まで州ラベルで代用
            // 史実の独立戦争は年単位（ギリシア1821-29・スペイン領アメリカ1810-25・ハンガリー1848-49・合衆国1861-65）。〔履歴 MultiNationSim.cs#45〕
            WarExhaustion = capitalRising ? parentPower.WarExhaustion : 0,
        };
        power.SpawnedDuringPlay = true;   // ★プレイ中に生まれた国＝ハーネスの「満足度・最小国」の対象外（Validation.WorstSat）
        nw.Diplomacy = Diplomacy; nw.SelfPower = power;
        // 無いと、途中で生まれた国だけ Explain が null になり、   台帳を読む側が NullReferenceException で落ちる（実測＝ExplainTests が分離独立の起きるシードで死んだ）。〔履歴 MultiNationSim.cs#46〕
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

    /// <summary>
    /// App（新規ゲーム）も自己対戦バッチ（SelfPlayMatch）も このメソッドだけを通る＝「遊ぶ世界」と「測る世界」の初期条件が同じになる。〔履歴 MultiNationSim.cs#47〕
    /// </summary>
    public void StartWarmup(Action? onQuarter = null, int? quartersOverride = null)
    {
        if (_startWarmupDone) return;   // 二重呼び出しの防止（ロード経路・診断からの再呼び出しで盤面が二度助走しない）
        _startWarmupDone = true;
        int quarters = quartersOverride ?? _coef.StartWarmupQuarters;
        if (quarters <= 0) return;   // 助走なし＝構築直後の盤面そのままで開始（厳密恒等の底）
        if (_coef.OpeningBakeEnabled > 0) OpeningBake.Apply(this);   // OpeningBakeEnabled〔履歴 MultiNationSim.cs#48〕
        // 助走のある通常の開始だけが取り直す。〔履歴 MultiNationSim.cs#49〕
        WarmupEconomy(quarters, onQuarter);
        WarmupTech(_coef.StartWarmupTechIterations);
        // ここでは世界の解禁の和（需要の門）を配るだけ。〔履歴 MultiNationSim.cs#50〕
        ShareWorldDiscovered();
        OpenForces();
        // 助走の直後（流れの値が出そろった盤面）で 1 度だけ取り直す（純関数・乱数なし）。〔履歴 MultiNationSim.cs#51〕
        Diplomacy.ComputeRanks(_coef);
        RebaselineInvariants();
    }
    private bool _startWarmupDone;

    /// <summary>
    /// 新しい表は無い。〔履歴 MultiNationSim.cs#52〕
    /// </summary>
    private void OpenForces() => Diplomacy.OpenGame(_coef);

    /// <summary>
    /// 全ての国の接触を先に求めてから判定する（判定の順で他国の接触が変わらない）。〔履歴 MultiNationSim.cs#53〕
    /// </summary>
    private void StepDiscoveries(bool evaluate)
    {
        if (evaluate)
        {
            int nr = Catalog.Discoveries.Length, n = Nations.Count;
            var gf = Market.DiscoveryGoodFlowQ;
            var carriers = Market.DiscoveryCarriers(Nations, _coef);
            if (Instruments.On && gf is not null) Instruments.DiscoveryFlow(Nations[0].World, gf, carriers);   // DiscoveryFlow〔履歴 MultiNationSim.cs#54〕
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
            // market〔履歴 MultiNationSim.cs#55〕
            var market = AdvanceDiscoveryPrices();
            // 台帳 DiscoveryWorldScans は旗が正のときだけ数える（計器だけの巡回は軌道の外）。〔履歴 MultiNationSim.cs#56〕
            double? worldAdv = null;
            bool clockOn = _coef.DiscoveryEngineerHalfPerMonth > 0 && _coef.DiscoveryClockWorldPerMonth > 0 && _coef.DiscoveryClockRampWidth > 0;
            if (clockOn || Instruments.On)
            {
                long ct0 = Instruments.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
                worldAdv = Discovery.WorldAdvLabor(Nations.Select(s => s.World), out long scanned);
                if (clockOn) Cost[CostLedger.DiscoveryWorldScans] += scanned;   // 計算量の台帳（診断）：世界の合計の巡回で訪れた生産者
                if (Instruments.On)
                {
                    _clockTicks += System.Diagnostics.Stopwatch.GetTimestamp() - ct0; _clockVisits += scanned;
                    if (Calendar.QuarterIndexOfTick(Nations[0].World.Tick) % 8 == 0) Instruments.ClockSample(this, worldAdv.Value, _clockTicks, _clockVisits);   // QuarterIndexOfTick〔履歴 MultiNationSim.cs#57〕
                }
            }
            double[]? scoreNow = _coef.SpreadNeedFascismDecline > 0 || Instruments.On ? NationalScore.ShareScores(Nations.ConvertAll(s => s.World)) : null; for (int i = 0; i < n; i++) Discovery.Step(Nations[i].World, new Discovery.WorldView(contacts[i], null, market, worldAdv, scoreNow?[i]), _coef);   // scoreNow〔履歴 MultiNationSim.cs#58〕
            ShareWorldDiscovered();
        }
        foreach (var sim in Nations) { sim.World.DiscoveryImportQ = null; sim.World.DiscoveryLossQ = null; }   // DiscoveryImportQ〔履歴 MultiNationSim.cs#59〕
        Market.DiscoveryGoodFlowQ = null;
        _recruitLog?.Clear();
    }
    /// <summary>
    /// 〔履歴 MultiNationSim.cs#60〕
    /// </summary>
    private Discovery.WorldMarket AdvanceDiscoveryPrices()
    {
        var worlds = new World[Nations.Count];
        var obs = new (double[] Inst, double[] Sold)[Nations.Count];
        double frontier = 0;   // frontier〔履歴 MultiNationSim.cs#61〕
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

    // _recruitLog〔履歴 MultiNationSim.cs#62〕
    /// <summary>この四半期の招聘（送り手・受け手・人数）＝発見の段が読んで空にする。作業領域（同じ四半期末の中で書いて読む＝セーブを挟まない）。</summary>
    [System.NonSerialized] private List<(World Src, World Dst, double Heads)>? _recruitLog;

    /// <summary>
    /// **価格の助走**：暦・政治・外交・人口・識字・技術・移民・**資本形成**を凍結したまま指定四半期を回す＝ 価格・在庫・クリアリング・賃金/地代/税という初期過渡だけを実勢へ落ち着かせる。〔履歴 MultiNationSim.cs#63〕
    /// </summary>
    public void WarmupEconomy(int quarters, Action? onQuarter = null)
    {
        if (quarters <= 0) return;   // 助走なし＝素の構築直後の盤面と厳密恒等（フラグの上げ下げすらしない）
        var prevPolitics = Nations.Select(n => n.World.PoliticsEnabled).ToArray();
        var treasury0 = Nations.Select(n => n.World.Treasury.Value).ToArray();   // 助走の前の国庫（world.json の初期値）
        foreach (var n in Nations) if (n.World.OpeningStateMonopolyTier > 0) n.World.SetStateMonopolyTier(n.World.OpeningStateMonopolyTier);   // 助走の始めに置く＝AI もプレイヤー（NullAgent）も同じ段で助走に入る。〔履歴 MultiNationSim.cs#64〕
        bool prevDiplomacy = DiplomacyFrozen;
        foreach (var n in Nations) { n.World.PoliticsEnabled = false; n.World.WarmupFrozen = true; }
        DiplomacyFrozen = true;
        _calendarFrozen = true;
        for (int q = 0; q < quarters; q++)
        {
            StepQuarter();
            for (int i = 0; i < treasury0.Length && i < Nations.Count; i++) Nations[i].World.RebaseOpeningFiscal(treasury0[i]);   // RebaseOpeningFiscal〔履歴 MultiNationSim.cs#65〕
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

    /// <summary>
    /// GrowTech は StepQuarter と同一の後追い/革新/フロンティア追随機構 （＝較正済みの技術ダイナミクスそのもの）を回すだけで、係数の外科補正は一切足さない＝差は制度から創発する。〔履歴 MultiNationSim.cs#66〕
    /// </summary>
    public void WarmupTech(int iterations)
    {
        for (int it = 0; it < iterations; it++)
        {
            foreach (var n in Nations) TechEducation.GrowTech(n.World, n.World.Coef);
            double frontier = 0;
            foreach (var n in Nations) frontier = Math.Max(frontier, n.World.TechLevel);
            foreach (var n in Nations) n.World.WorldTechLevel = frontier;   // StepDay と同一：後追いが立ち上がる先端を毎反復同期
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

    // 保存量プローブ（leakhunt・恒久計測器）：共有層のどのサブステップで D＝基礎貨幣−累積台帳 が動いたかを特定する。〔履歴 MultiNationSim.cs#67〕
    private double[]? _probeD;
    private static double ProbeD(World w) => (w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued).Value;
    private double ProbeWorldW() { double s = -Diplomacy.CumulativeFxResidual; foreach (var n in Nations) { var w = n.World; s += (w.BaseMoney - w.CumulativeGoldMinted - w.CumulativeFiatIssued).Value; } return s; }   // ★段3-1（A7）：為替の換算の台帳を差し引く
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
    // 環境変数 POB_TIME=1 のときだけ計る。〔履歴 MultiNationSim.cs#68〕
    internal static readonly bool TimeOn = System.Environment.GetEnvironmentVariable("POB_TIME") == "1";
    /// <summary>
    /// 各国の日次（純粋日次相のみ）を並列に回すか。〔履歴 MultiNationSim.cs#69〕
    /// </summary>
    internal static readonly bool ParallelDays = System.Environment.GetEnvironmentVariable("POB_PAR") != "0";
    public static long TicksNations, TicksMarket;   // POB_TIME=1 の計器。〔履歴 MultiNationSim.cs#70〕

    public bool StepDay()
    {
        // 相1 Open（四半期首）＝逐次　→　相2 Daily（価格形成・建設・信用＝実測74%）＝**並列**　→　相3 Close（月次決算・四半期末）＝逐次。〔履歴 MultiNationSim.cs#71〕
        long t0 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        bool par = ParallelDays && Nations.Count > 1 && !Simulation.ConservationProbe && !TradeReachDiag.On && !BudgetBindDiag.On;   // par〔履歴 MultiNationSim.cs#72〕
        foreach (var sim in Nations) sim.StepDayOpen();
        if (par)
            System.Threading.Tasks.Parallel.For(0, Nations.Count, i => Nations[i].StepDayDaily());
        else
            foreach (var sim in Nations) sim.StepDayDaily();    // 各国の鼓動（価格形成＝旬の最終日だけ・建設・信用）
        // 締め境界＝**常に逐次**。〔履歴 MultiNationSim.cs#73〕
        if (_coef.LuxuryIncomeElasticity > 0 && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0) DistributeWorldRealIncome();   // ε＝0 は走らない〔履歴 MultiNationSim.cs#74〕
        foreach (var sim in Nations) sim.StepDayClose();
        foreach (var sim in Nations) sim.World.FlushLog();      // 国順フラッシュ＝共有ログの順序を旧・直接書きと一致させる（恒等は検査Dで実証）
        if (TimeOn) System.Threading.Interlocked.Add(ref TicksNations, System.Diagnostics.Stopwatch.GetTimestamp() - t0);

        bool quarterDone = _dayInQuarter == Calendar.QuarterEndDayIndex;
        // 四半期の第1・第2月末にも清算する＝全国 Simulation.MonthSettle の後＝   scale/Stock 更新済み・Invariants 検査後＝四半期末の既存呼び出し（下）と同じ相対順序。〔履歴 MultiNationSim.cs#75〕
        if (!quarterDone && (_dayInQuarter + 1) % Calendar.DaysPerMonth == 0)
        {
            int monthIdx = (_dayInQuarter + 1) / Calendar.DaysPerMonth - 1;   // 0 or 1（2＝四半期末は下の既存呼び出し）
            ProbeSnap();
            { long t1 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Market.Step(Nations, _coef, firstClearingOfQuarter: monthIdx == 0); if (TimeOn) System.Threading.Interlocked.Add(ref TicksMarket, System.Diagnostics.Stopwatch.GetTimestamp() - t1); }
            ProbeDiff("国際市場(月)");
        }
        // 順序は変えない。〔履歴 MultiNationSim.cs#76〕
        if (quarterDone)
        {
            ProbeSnap();                                              // 保存量プローブ（leakhunt・既定OFF＝実行ゼロ）
            Cost[CostLedger.SharedNationQuarters] += Nations.Count;   // 計算量の台帳（診断）：四半期末の共有層は国×四半期に比例（国力・脅威・移民・国際金融ほか）
            NationalScore.StepStandingAll(Nations.ConvertAll(n => n.World));   // 外交(ComputeRanks)より前〔履歴 MultiNationSim.cs#77〕
            // StepSecurityThreat〔履歴 MultiNationSim.cs#78〕
            StepSecurityThreat();   // StepSecurityThreat〔履歴 MultiNationSim.cs#79〕
            UpdateTradeWeights(); ProbeDiff("交易比重");                // 外1：交易比重を貿易取扱高のシェア（LastExportValue+LastImportValue の EMA）から四半期同期
            if (!DiplomacyFrozen)
            {
                Diplomacy.BeginQuarterSyncWindow();   // BeginQuarterSyncWindow〔履歴 MultiNationSim.cs#80〕
                for (int m = 0; m < Calendar.MonthsPerQuarter; m++) { Diplomacy.Step(_coef, Rng, _quarter * Calendar.MonthsPerQuarter + m); ProbeDiff($"外交m{m}"); }   // 共有外交を月次3回（戦争の月次カデンス）。凍結中＝経済助走で戦争・緊張を進めない
                Diplomacy.EndQuarterSyncWindow();
            }
            StepCanals(); ProbeDiff("運河");   // 海峡・運河：扼者を解決し、運河は host 保有国の資本家が建設（§B）
            { long t2 = TimeOn ? System.Diagnostics.Stopwatch.GetTimestamp() : 0; Market.Step(Nations, _coef, firstClearingOfQuarter: false); if (TimeOn) System.Threading.Interlocked.Add(ref TicksMarket, System.Diagnostics.Stopwatch.GetTimestamp() - t2); } ProbeDiff("国際市場");          // 国際市場：州と州の二国間清算（実経路・比較優位）。月次清算ONでは四半期の第3清算＝累計を続ける（リセットしない）
            StepPorts(); ProbeDiff("港湾");                             // 港湾容量：混雑する港州を国家＋資本家が拡張（Market.Step の取扱高/律速を見て投資。〔履歴 MultiNationSim.cs#81〕
            StepMarineInsurance(); ProbeDiff("海上保険");                // 海上保険：商人pop基金で通商破壊損失を分散（平時=保険料・封鎖時=保険金。〔履歴 MultiNationSim.cs#82〕
            // 移民は総量保存だが時間の流れ（暦を止めた助走で人を動かさない）。〔履歴 MultiNationSim.cs#83〕
            if (!_calendarFrozen)
            {
                StepAssimilation(); ProbeDiff("同化");                       // 民族の同化（移民の希釈への歯止め）
                // ⇒ 説明台帳へ1行足す。〔履歴 MultiNationSim.cs#84〕
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
            // 書き込みは残す（NationConfig の死んだ定数 0.3 を残さない）。〔履歴 MultiNationSim.cs#85〕
            foreach (var n in Nations)
            {
                var w = n.World;
                bool coastal = false;
                foreach (var r in w.Regions) if (r.IsPort) { coastal = true; break; }
                double gap = 0.0;   // 旧の ExternalPressureGapScale は読み手が無くなったので削除〔履歴 MultiNationSim.cs#86〕
                w.ExternalPressure = gap * (coastal ? 1.0 : _coef.ExternalPressureInlandFactor);
            }
            // indFrontier〔履歴 MultiNationSim.cs#87〕
            double indFrontier = 0;
            foreach (var n in Nations) indFrontier = Math.Max(indFrontier, n.World.MeanIndustrialization);
            foreach (var n in Nations) n.World.IndustrialFrontier = indFrontier;
            // World は国際市場を見られないので、   ここで各国へ配る（IndustrialFrontier と同型の共有スカラー）。〔履歴 MultiNationSim.cs#88〕
            if (Catalog.SilverId < Market.Price.Count)
            {
                double ratioSum = 0; int ratioCnt = 0;
                bool demandWeighted = _coef.SilverParityDemandWeighted > 0;   // demandWeighted〔履歴 MultiNationSim.cs#89〕
                double refAgW = Catalog.RefPrices[Catalog.SilverId], refAuW = Catalog.RefPrices[Catalog.GoldOreId];   // refAgW〔履歴 MultiNationSim.cs#90〕
                double wLnSum = 0, wSum = 0;
                foreach (var n in Nations)
                    foreach (var r in n.World.Regions)
                    {
                        if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) continue;
                        var mAg = r.Markets[Catalog.SilverId]; var mAu = r.Markets[Catalog.GoldOreId];
                        if (!(mAg.LastDemand > 1e-9 || mAg.LastProduced > 1e-9) || !(mAu.LastDemand > 1e-9 || mAu.LastProduced > 1e-9)) continue;   // 死んだ市場（MarketFlow と同じ条件）は除外
                        double pAg = mAg.Price, pAu = mAu.Price;
                        if (!(pAg > 1e-9) || !(pAu > 1e-9)) continue;   // 否定形＝NaN も除外
                        double lnRatio = System.Math.Log(pAg / pAu);
                        ratioSum += lnRatio; ratioCnt++;   // 対数平均
                        double dAu = mAu.LastDemand;
                        double wState = demandWeighted ? SilverMinRefWeight(mAg.LastDemand, dAu, refAgW, refAuW) : 1.0;   // wState〔履歴 MultiNationSim.cs#91〕
                        if (demandWeighted) { wLnSum += wState * lnRatio; wSum += wState; }
                        if (Instruments.On) Instruments.SilverWeightState(this, r, wState, lnRatio, dAu, mAu.LastProduced, mAg.LastDemand, mAg.LastProduced);   // SilverWeightState〔履歴 MultiNationSim.cs#92〕
                    }
                double refAu = Catalog.RefPrices[Catalog.GoldOreId];
                double refRatio = refAu > 1e-9 ? Catalog.RefPrices[Catalog.SilverId] / refAu : 0;
                double meanRatio = SilverMeanRatio(demandWeighted, ratioSum, ratioCnt, wLnSum, wSum);   // meanRatio〔履歴 MultiNationSim.cs#93〕
                bool ratioOk = ratioCnt > 0 && meanRatio > 1e-9 && refRatio > 1e-9;
                double rel = ratioOk ? meanRatio / refRatio : 1.0;
                // World は国際市場を見られないので、   ここで各国へ配る（IndustrialFrontier と同型の共有スカラー）。〔履歴 MultiNationSim.cs#94〕
                double silverWorld = Market.Price[Catalog.SilverId];
                if (_coef.SilverParityFromRatio > 0 && ratioOk) silverWorld = rel * Catalog.RefPrices[Catalog.SilverId];
                foreach (var n in Nations) { n.World.WorldSilverPrice = silverWorld; n.World.SilverGoldRatio = rel; }
                if (Instruments.On && !_calendarFrozen) Instruments.SilverParityCapture(this, silverWorld, rel, ratioOk);   // SilverParityCapture〔履歴 MultiNationSim.cs#95〕
                if (Instruments.On) Instruments.SilverWeightEnd(this, _calendarFrozen, ratioOk);   // SilverWeightEnd〔履歴 MultiNationSim.cs#96〕
            }
            // 相手の本位の差＝World.TradePartnerStandardBalance（BilateralMarket が越境の成約ごとに相手の本位の符号で積む＝金＋1・銀−1・不換0・−1..1）。〔履歴 MultiNationSim.cs#97〕
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
                    double annualGdp = Calendar.PerMonthToPerYear(wn.NominalGdpPerMonth).Value;
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
            // ⚠新しい価格を作らない——既存の平均価格をそのまま渡すだけ。〔履歴 MultiNationSim.cs#98〕
            foreach (var n in Nations)
            {
                var wn = n.World;
                if (wn.WorldGoodsPrices is not { } wgp || wgp.Length != Market.Price.Count)
                    wn.WorldGoodsPrices = wgp = new double[Market.Price.Count];
                for (int gi = 0; gi < wgp.Length; gi++) wgp[gi] = Market.Price[gi];
            }
            // 読み手＝RootSupply のみ（AI の工場の選び方・根まで作れるかの判定：世界のどこかで作られている投入は根が無いとは言えない）。〔履歴 MultiNationSim.cs#99〕
            {
                int ng = Catalog.Goods.Length;
                var prod = new double[ng];
                foreach (var n in Nations)
                    foreach (var r in n.World.Regions)
                    {
                        int lim = Math.Min(ng, r.Markets.Length);
                        for (int gi = 0; gi < lim; gi++) prod[gi] += r.Markets[gi].LastProduced;
                    }
                foreach (var n in Nations)
                {
                    var wn = n.World;
                    if (wn.WorldGoodsProduced is not { } wgq || wgq.Length != ng) wn.WorldGoodsProduced = wgq = new double[ng];
                    Array.Copy(prod, wgq, ng);
                }
            }
            // 財5（対外国債の他国資本引き当て・プール注入）：各国の対外国債プールに『他モデル国の資本家貯蓄の合計』＝対外国債の引き受け余力を注入。〔履歴 MultiNationSim.cs#100〕
            {
                Span<double> inv = Nations.Count <= 128 ? stackalloc double[Nations.Count] : new double[Nations.Count];
                double worldCapSavings = 0;
                for (int k = 0; k < Nations.Count; k++) { inv[k] = CapitalMarket.InvestorSavings(Nations[k].World) * Nations[k].World.FxRate; worldCapSavings += inv[k]; }
                for (int k = 0; k < Nations.Count; k++)   // 各国の余力＝世界の資本家貯蓄から自国ぶんを除いた合計（他国だけが自国債を引き受けられる）
                    Nations[k].World.ForeignBondInvestorPool = Math.Max(0, worldCapSavings - inv[k]) / Nations[k].World.FxRate;
            }
            // 借り手は Fiscal で +CumulativeTradeFlow 済み＝   ここで他国資本家の貯蓄から比例控除し、貸し手国の CumulativeTradeFlow を同額 − する＝両建てで世界の貨幣創造を閉じる。〔履歴 MultiNationSim.cs#101〕
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
                        foreach (var p2 in lenders[li]) heldBy[li] += Math.Max(0, p2.Savings.Value);
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
                                lw.ForeignBondClaims += new MoneyStock(k);
                                paid += k; lent += k * lw.FxRate / bw.FxRate;
                            }
                    double unmet = owe - lent;
                    double back = unmet > 0 ? unmet : 0;
                    if (back > 0)
                    {
                        var backStock = new MoneyStock(back); bw.GovDebt -= backStock; bw.ForeignDebt -= backStock; bw.PrevForeignDebt -= backStock;
                        World.Transfer((bw, null), (null, null, null), back, TreasuryCat.Borrow);
                    }
                    fxLend += (owe - back) - paid;   // 借り手が受け取ったまま残る額（借り手の通貨）−貸し手が払った額
                }
                Diplomacy.CumulativeFxResidual += fxLend;
            }
            ReabsorbNations();       // ★内戦一元化：内戦決着の再統合（州の返還・国家除去）。SpawnSecededNations の前＝同四半期の建国と衝突しない
            SpawnSecededNations();   // 建国キューの回収（分離戦勝利の州→新国家）。Nations走査の外＝リスト追記が安全・新国家は次の四半期から鼓動
            // 配線を読むのは国際市場の清算（BilateralMarket）だけ   ＝旧の位置からここまでの間（港湾・海上保険・移民・国際金融・疫病・天候・伝播）に読み手は無い＝動いたのは再併合と建国の四半期だけ。〔履歴 MultiNationSim.cs#102〕
            RewirePorts();
            StepDiffusion();         // ★発見・段7：思想の伝播（貿易路×メディア・前四半期読み＝閉路なし）。DiffusionWeight=0で即return＝恒等
            // 助走では DiplomacyFrozen＝Diplomacy.Step を1度も呼ばないので、止めても飛ばした索引は存在しない。〔履歴 MultiNationSim.cs#103〕
            if (!_calendarFrozen) _quarter++;
        }
        _dayInQuarter = (_dayInQuarter + 1) % Calendar.DaysPerQuarter;
        return quarterDone;
    }

    /// <summary>
    /// 政府勝利＝反乱国の全州を親へ返還し国家を除去／反乱勝利＝同じ返還の後に政体変更の実行部（Politics.SettleRevolt＝旧経路の内戦の決着と同じ1本・革命の成就）。〔履歴 MultiNationSim.cs#104〕
    /// </summary>
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
                foreach (var p2 in r.Pops) { m += (p2.NewsFill * p2.Literacy + p2.RadioFill) * p2.Size; sz += p2.Size; }
            }
            media[i2] = sz > 1e-9 ? PoliticalQuantities.Clamp01(m / sz) : 0;
        }
        var prev = new double[n][];
        for (int i2 = 0; i2 < n; i2++)
        {
            var wr = Nations[i2].World;
            // 種は**放射側（prev）だけ**に使い、発見が読む   受信側 SpreadReach には他国から届いた分しか積まない（＝伝播の定義）。〔履歴 MultiNationSim.cs#105〕
            prev[i2] = (double[])wr.SpreadReach.Clone();
            var nat = Ideologies.National(wr);
            for (int k2 = 0; k2 < prev[i2].Length; k2++)
            {
                double src = Discovery.SpreadSource(wr, k2, nat);
                if (src > prev[i2][k2]) prev[i2][k2] = src;
            }
        }
        // 絶対量の大きい経路＝英仏・英米が太い＝史実の順序。〔履歴 MultiNationSim.cs#106〕
        double worldFlow = 0;
        for (int a0 = 0; a0 < n; a0++) for (int b0 = 0; b0 < n; b0++) worldFlow += pf[a0][b0];
        // 世界シェアだと経済の大きさに比例して清が最大になる（実測 6.8〜7.5）＝接触は量でなく強度。〔履歴 MultiNationSim.cs#107〕
        {
            double worldGdp = 0; for (int a1 = 0; a1 < n; a1++) worldGdp += System.Math.Max(0, Nations[a1].World.NominalGdpPerMonth.Value);
            double worldIntensity = worldGdp > 1e-9 ? 2.0 * worldFlow / worldGdp : 0;
            for (int a1 = 0; a1 < n; a1++)
            {
                double own = 0; for (int b1 = 0; b1 < n; b1++) if (b1 != a1) own += pf[a1][b1] + pf[b1][a1];
                double gdp = Nations[a1].World.NominalGdpPerMonth.Value;
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
        // 親が交戦中/不在なら持ち越し。〔履歴 MultiNationSim.cs#108〕
        if (_coef.RumpReabsorbYears > 0)
            for (int ri2 = 0; ri2 < Nations.Count; ri2++)
            {
                var rw2 = Nations[ri2].World;
                if (rw2.RumpParentName.Length == 0 || rw2.SelfPower is not { } rp2) continue;
                var par2 = Diplomacy.Powers.Find(q => (q.BaseName.Length > 0 ? q.BaseName : q.Name) == rw2.RumpParentName);
                if (par2?.Nation is not { } pw2) { rw2.RumpParentName = ""; continue; }   // 親が消滅＝独立が確定
                bool low = rw2.NominalGdpPerMonth.Value < RumpGdpFractionOfParent * System.Math.Max(1.0, pw2.NominalGdpPerMonth.Value);
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
            // ★政体の変更は州が戻った後（下の決着の当てはめ）＝革命の後始末（失脚・恐怖政治・土地改革）が再統一した国の全POPに掛かる（旧経路の内戦の勝ちと同じ範囲）。〔履歴 MultiNationSim.cs#109〕
            double T = rw.Treasury.Value;
            var moving = new System.Collections.Generic.List<Region>(rw.Regions);
            // rw.GovDebt も同額下げる＝消える直前まで反乱国側の不変条件も厳密。〔履歴 MultiNationSim.cs#110〕
            List<(Pop Pop, MoneyStock Held)>? inherit = rebelWon ? [] : null;
            foreach (var rr in moving)
                foreach (var pp in rr.Pops)
                {
                    if (inherit is not null) { if (pp.BondHoldings.Value > 0) inherit.Add((pp, pp.BondHoldings)); }
                    else rw.GovDebt -= pp.BondHoldings;
                    pp.BondHoldings = default;
                }
            // ★返還州の Id は親の中で最大＋1（Territory.Adopt）。〔履歴 MultiNationSim.cs#111〕
            foreach (var rr in moving) Territory.TransferRegion(rw, pw, rr, Territory.Rehoming.Returned);
            // 遷都の1本（World.SetCapital＝Id と Regions[0]・距離の基準の座標・植民地財）。〔履歴 MultiNationSim.cs#112〕
            if (pw.SecedingRegion is { } seat && moving.Contains(seat)) pw.SetCapital(seat);
            if (pw.SecedingRegion is { } seat2 && (moving.Contains(seat2) || !Diplomacy.Wars.Exists(x => x.CivilWarParent == parentP))) pw.SecedingRegion = null;
            // 金属の準備は現物＝換算しない。〔履歴 MultiNationSim.cs#113〕
            double Tin = World.CrossFx(rw, pw, T, book: false);   // 換算の目安（台帳には積まない＝移し方の1本が積む）
            if (T > 0) World.Transfer((rw, null), (pw, null, null), T, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            else if (T < 0) World.Transfer((pw, null), (rw, null, null), -Tin, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            MoneyStock L = default;
            if (pw.Bank is { } pb && rw.Bank is { } rb)
            {
                MoneyStock loansIn = -World.CrossFx(rw, pw, -rb.Loans), hhIn = -World.CrossFx(rw, pw, -rb.HouseholdLoans);
                pb.Loans += loansIn; pb.HouseholdLoans += hhIn; L = loansIn + hhIn;
            }
            pw.CumulativeTradeFlow -= L;   // 与信（負債）の合算＝基礎貨幣の −L（親の通貨）。貯蓄 S は州ごとに上で記帳済み・国庫は上の移し方の1本＝合わせて反乱国の基礎貨幣（S＋T−L）＝親の per-nation 保存が閉じ、世界の和の差は換算の台帳（建国時の −(S+T) の鏡映＋反乱国として積んだ分）
            pw.ForeignCapitalIn += World.CrossFx(rw, pw, rw.ForeignCapitalIn, book: false); pw.CapitalAbroad += World.CrossFx(rw, pw, rw.CapitalAbroad, book: false);
            // 反乱国の対外債の否認（政府の勝ち）：債権へ繰り上げ済みの額（PrevForeignDebt）を、反乱国以外の債権の比で落とす（債権の増減の1本＝CapitalMarket.AdjustClaims・
            //   四半期の償還と同じ規則）。反乱国自身の債権は次の行で親へ。
            if (!rebelWon && rw.PrevForeignDebt.Value > 1e-9) CapitalMarket.AdjustClaims(Nations, ri, -rw.PrevForeignDebt.Value);
            pw.ForeignBondClaims += World.CrossFx(rw, pw, rw.ForeignBondClaims, book: false);
            pw.ForeignIntPaidThisQuarter += World.CrossFx(rw, pw, rw.ForeignIntPaidThisQuarter, book: false); pw.PendingForeignBorrow += World.CrossFx(rw, pw, rw.PendingForeignBorrow, book: false);
            pw.SilverReserveQty += rw.SilverReserveQty; pw.GoldReserveQty += rw.GoldReserveQty;
            // 反乱国の植民地（戦中に得た場合の防御）＝親へ移す（無主化して消さない）
            foreach (var col in rebelP.ColonyList) Diplomacy.AcquireColony(parentP, col, _coef);   // 植民地の受け取りの1本（講和の奪取・無血併合と同じ＝国を持つ受け手は植民地格の州へ）
            rebelP.ColonyList.Clear();
            // 反乱の勝ち＝政体変更の実行部（急進革命だけ国債の否認・失脚・土地改革・覚醒・断裂の窓＝即時の政変と同じ規則）／同じ政体の要求戦は要求をのむ（ConcedeToFaction）。〔履歴 MultiNationSim.cs#114〕
            var movement = pw.RevoltingFaction is { } mv && moving.Contains(mv.Region) ? mv : null;
            // ★計器（F3b蜂起doc §5 段1・純記帳）：鎮圧の実体は「親が内戦に勝って再併合する」この経路＝決着した運動の名をここで出す（力学は読まない）。
            string spentName = movement is not null && !rebelWon ? $"（{Factions.DisplayName(movement)}の蜂起を鎮圧）" : "";
            var polityBefore = pw.Polity;
            if (pw.PoliticsEnabled && !(peaceful?.Contains(rebelP) ?? false))
                Politics.SettleRevolt(pw, _coef, rebelWon, rebelPolity, movement, moving.SelectMany(r => r.Pops));
            // 国内元本＝Σ保有＝rw.GovDebt−rw.ForeignDebt＝親の不変条件も閉じる。〔履歴 MultiNationSim.cs#115〕
            if (inherit is not null)
            {
                // ★人と動くお金の換算（段4e）：反乱国の国債の元本と保有は反乱国の通貨＝親の通貨へ（名目の残高＝換算の台帳に積まない・Σ保有＝国内元本の不変条件は同じ比で保つ）。
                foreach (var (hp, h) in inherit) hp.BondHoldings += World.CrossFx(rw, pw, h, book: false);
                double debt0 = pw.GovDebt.Value; MoneyStock rwDebt = World.CrossFx(rw, pw, rw.GovDebt, book: false);
                pw.GovDebt += rwDebt; pw.ForeignDebt += World.CrossFx(rw, pw, rw.ForeignDebt, book: false); pw.PrevForeignDebt += World.CrossFx(rw, pw, rw.PrevForeignDebt, book: false);
                if (pw.GovDebt.Value > 1e-9) pw.GovAvgCouponPerYear = (debt0 * pw.GovAvgCouponPerYear + rwDebt.Value * rw.GovAvgCouponPerYear) / pw.GovDebt.Value;   // 既往債の利率は元本の加重（起債の R9 の混ぜ方と同じ）
            }
            // IsPlayer〔履歴 MultiNationSim.cs#116〕
            if (rebelP.IsPlayer)
            {
                rebelP.IsPlayer = false; parentP.IsPlayer = true;
                Nations[pi].SetAgent(new NullAgent());
                Diplomacy.Log.Add($"{pw.Year} {rebelP.Name}は{parentP.Name}へ再併合された＝操作国は{parentP.Name}");
            }
            Diplomacy.RemovePower(rebelP, _coef);   // 反乱国が巻き込まれていた第三国との戦争も EndWar を通して終わる＝終戦の記録が残る
            CostLedger.AddInto(_costDeparted ??= new long[CostLedger.Count], Nations[ri].World.CostOrNull);   // 計算量の台帳：消える国の件数を受け皿へ（診断・軌道に触れない）
            Nations.RemoveAt(ri);
            // PairFlowQ〔履歴 MultiNationSim.cs#117〕
            if (Market.PairFlowQ is { } pfq && ri < pfq.Length)
                Market.PairFlowQ = pfq.Where((_, a) => a != ri).Select(row => row.Where((_, b) => b != ri).ToArray()).ToArray();
            Diplomacy.Log.Add(!rebelWon
                ? $"{pw.Year} {parentP.Name}が{rebelP.Name}を再併合＝内戦の傷跡だけが残った{spentName}"
                : rebelPolity != polityBefore
                ? $"{pw.Year} {rebelP.Name}が{parentP.Name}を制した＝{PlayText.PolityRegimeJa(rebelPolity)}の下で再統一"
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
                double proximity = PoliticalQuantities.Clamp01(1.0 - km / (_coef.EpidemicTransmitRange * _coef.EraFactor(w.WorldTechLevel, _coef.EraEpidemicRange)));   // =0で×1恒等）              // 陸路＝近いほど強い〔履歴 MultiNationSim.cs#118〕
                double trade = _coef.EpidemicTradeTransmit * w.ExternalTradeAccess() * m.ExternalTradeAccess();   // 海路＝両国が交易に開かれているほど（港経由のコレラ）
                // 戦争＝距離に依らない強制的な濃厚接触（軍営・前線・補給線・復員兵）＝最強の伝播経路（スペイン風邪1918型）。
                double war = w.SelfPower is { } sp && m.SelfPower is { } mp && Diplomacy.IsAtWar(sp, mp) ? _coef.EpidemicWarTransmit : 0;
                exposure = Math.Max(exposure, Math.Max(Math.Max(proximity, trade), war));   // 最も近い/繋がった/交戦中の感染国からの曝露
            }
            if (exposure <= 1e-9) continue;
            double chance = _coef.EpidemicTransmitChance * exposure * w.UrbanShare;   // chance〔履歴 MultiNationSim.cs#119〕
            var rng = Nations[i].EpidemicRng;   // 受け手の専用RNG（各国独立＝盤面非依存の決定論）
            if (rng.NextDouble() < chance) w.TriggerEpidemic(_coef, EpidemicTransmitSeverityFloor + rng.NextDouble(), transmitted: true, pickRng: rng);   // 伝染＝死亡スパイク＋感染窓（連鎖しうる）
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
                // 旧凸結合はρ>0で実効振幅が縮む （ρ=0.5で0.69倍＝持続性を上げるほど天候がおとなしくなる逆効果）。〔履歴 MultiNationSim.cs#120〕
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
                if (!Megaprojects.RequirementMet(w, cp.Id)) continue;   // 近代土木の技術ゲート＝運河の時代を出す（スエズ1869/パナマ1914）。〔履歴 MultiNationSim.cs#121〕
                // 外7：CanalInvestReturns>0 のとき host（扼者）と交戦中の国は拠出停止（敵の運河には投資しない＝資本の論理）。既定0.5＝停止が効く。0で無条件拠出。
                if (_coef.CanalInvestReturns.Value > 0 && cp.Owner is { Nation: not null } cpo && w.SelfPower is { } wsp && Diplomacy.IsAtWar(wsp, cpo)) continue;
                // investors〔履歴 MultiNationSim.cs#122〕
                var investors = CapitalMarket.Investors(w);
                double pool = 0; foreach (var ip in investors) pool += Math.Max(0, ip.Savings.Value);
                if (pool <= 1e-6) continue;
                // 労働の担い手がいなければ動かない（進捗も持分も立たない）。〔履歴 MultiNationSim.cs#123〕
                double inv = InternationalMarket.PaySales((w, investors), null, Catalog.LaborId, _coef.CanalInvestFrac.Value * pool, w);
                if (!(inv > 0)) continue;
                cp.BuildProgress += inv;
                // 外7：拠出国の持分（累計拠出額）を記録＝後で通行料を持分比で分配（配当）。〔履歴 MultiNationSim.cs#124〕
                if (_coef.CanalInvestReturns.Value > 0 && w.SelfPower is { } shp)
                {
                    double invG = inv * w.FxRate;
                    cp.InvestorShares[shp.Id] = cp.InvestorShares.TryGetValue(shp.Id, out double prev) ? prev + invG : invG;
                }
            }
            // 「データが無い＝不発」を保証する安全弁。〔履歴 MultiNationSim.cs#125〕
            if (cost > 1e-9 && cp.BuildProgress >= cost) { cp.IsOpen = true; Diplomacy.Log.Add($"{year} {cp.Name}が開通（{(cp.Owner is { } o ? o.Name + "が扼す" : "中立")}）"); }
        }

        // 通行料：開通済みの咽喉の利用国から扼者の国庫へ（迂回より安い＝使う方が得）。〔履歴 MultiNationSim.cs#126〕
        double tollCx = 0, tollCy = 0, tollCz = 0, tollTw = 0;   // tollCx〔履歴 MultiNationSim.cs#127〕
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
                double traffic = 0; foreach (var r in uw.Regions) traffic += r.LastPortThroughput;
                double tollBase = _coef.CanalTollTrafficRate * traffic;   // 通航トン数課金（取扱高の代理・前四半期値）＝取扱高は金建て（積出し価格）
                double toll = tollBase / uw.FxRate * uw.FiscalFundingRatio.Value;   // toll〔履歴 MultiNationSim.cs#128〕
                if (toll <= 1e-9) continue;
                // 外7：CanalInvestReturns>0 なら通行料の一部を建設拠出国の**投資家**へ持分（累計拠出額）比で配当し、残りを扼者の国庫へ。〔履歴 MultiNationSim.cs#129〕
                double dividend = _coef.CanalInvestReturns.Value > 0 ? DistributeCanalDividend(cp, toll * _coef.CanalInvestReturns.Value, uw) : 0;
                World.Transfer((uw, null), (ow, null, null), toll - dividend, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);   // 配当されなかった残余（＝扼者取り分）。分配失敗時も toll 全額が扼者へ回り保存
            }
        }
    }

    // 外7：通行料の配当ぶんを、この運河の建設拠出国へ持分（InvestorShares＝累計拠出額・金建て）比で配分。〔履歴 MultiNationSim.cs#130〕
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

    /// <summary>
    /// 導出：律速率＝min(1, cap÷potential)・cap＝船腹の実効＋PortCapToThroughput×PortCapacity（加法的＝BilateralMarket）。〔履歴 MultiNationSim.cs#131〕
    /// </summary>
    internal static double PortCapitalNeed(Region r, Coefficients c, double lostCapacity)
    {
        if (!(c.PortCapToThroughput > 0)) return 0;
        double units = (c.PortCongestionTarget - r.LastTradeThrottle) * r.LastTradePotential / c.PortCapToThroughput + lostCapacity;
        return units > 0 ? units * c.PortBuildCostPerUnit : 0;   // 否定形＝NaN は払わない
    }

    // 保存：投資（資本家貯蓄＋国庫）→ 港の州の労働の④（建設の工賃＝StepCanals と同じ・BaseMoney不変）。〔履歴 MultiNationSim.cs#132〕
    private void StepPorts()
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            double natPop = 0; foreach (var (_, p) in w.AllPopsWithRegion()) natPop += p.Size;
            if (natPop <= 1e-9) continue;
            double natThr = w.PortThroughputTotal();   // natThr〔履歴 MultiNationSim.cs#133〕
            bool techOk = Discovery.SteamPower(w.Discovered);   // techOk〔履歴 MultiNationSim.cs#134〕
            foreach (var r in w.Regions)
            {
                if (!r.IsPort) continue;
                double capBeforeDecay = r.PortCapacity;   // capBeforeDecay〔履歴 MultiNationSim.cs#135〕
                if (!_calendarFrozen) r.PortCapacity = Math.Max(0, r.PortCapacity * (1 - _coef.PortDecay));   // 減耗（維持に再投資）。★助走中は減耗も拡張も止める＝港湾容量は1783年のまま
                // 技術ゲート（近代港湾）。〔履歴 MultiNationSim.cs#136〕
                if (_calendarFrozen || !techOk || r.LastTradeThrottle >= _coef.PortCongestionTarget) continue;   // ★助走：港湾も「年月をかけて広がる」能力＝凍結
                if (r.InvestorPopIndex < 0 || r.InvestorPopIndex >= r.Pops.Count) continue;
                var ip = r.Pops[r.InvestorPopIndex];
                double need = Instruments.On ? PortCapitalNeed(r, _coef, capBeforeDecay - r.PortCapacity) : double.NaN;   // need〔履歴 MultiNationSim.cs#137〕
                double cCap = _coef.PortInvestFrac.Value * Math.Max(0, ip.Savings.Value);          // 資本家ルート
                double cState = w.PortStateRequest(r, natThr) * w.FiscalFundingRatio.Value;   // cState〔履歴 MultiNationSim.cs#138〕
                double invest = cCap + cState;
                if (Instruments.On) Instruments.PortGate(w, r, ip.Savings.Value, need, cCap, cState);   // Instruments〔履歴 MultiNationSim.cs#139〕
                if (invest <= 1e-9) continue;   // ★判定→引落の順（wave2b是正）：引落してから捨てると微小額が pop へ還流されず消える片側記帳になる
                // 進捗は動いた額（資本家の分＋国庫の分）。〔履歴 MultiNationSim.cs#140〕
                double paidCap = InternationalMarket.PaySales((w, [ip]), r, Catalog.LaborId, cCap, w);
                double paidState = InternationalMarket.PaySales((w, null), r, Catalog.LaborId, cState, w, TreasuryCat.OtherOut);
                invest = paidCap + paidState;
                r.PortCapacity += invest / _coef.PortBuildCostPerUnit;
                if (Instruments.On) Instruments.PortPaid(w, r, paidCap, paidState, invest / _coef.PortBuildCostPerUnit);   // PortBuildCostPerUnit〔履歴 MultiNationSim.cs#141〕
            }
        }
    }

    // 封鎖損失時は損失×給付率を商人基金→荷主へ（基金=商人貯蓄の範囲で部分払い＝ロイズの破綻リスク）。〔履歴 MultiNationSim.cs#142〕
    private void StepMarineInsurance()
    {
        if (_coef.InsurancePayoutFrac.Value <= 0) return;
        foreach (var sim in Nations)
        {
            var w = sim.World;
            if (w.SelfPower is null) continue;
            double throughput = 0;
            foreach (var r in w.Regions) throughput += r.LastPortThroughput;
            if (throughput < 1e-9) continue;
            double risk = w.LastBlockadeRisk;   // 03手順7：旧CommerceRaidFactorの置換＝リスク線積分（取扱高加重平均）
            if (!(risk > 1e-9)) continue;
            var merchants = new List<Pop>(); var exposure = new List<double>();
            foreach (var r in w.Regions)
                if (r.MerchantPopIndex >= 0 && r.MerchantPopIndex < r.Pops.Count)
                { merchants.Add(r.Pops[r.MerchantPopIndex]); exposure.Add(Math.Max(0, r.LastPortThroughput)); }
            if (merchants.Count == 0) continue;   // 引受手(商人)が居ない国は保険市場が立たない
            // 保険金：商人基金（引受手・貯蓄の比）→荷主（捌き手の商人・海路の取扱高の比）＝基金の範囲で部分払い（払い手の貯蓄でクランプ・移し方の1本）
            World.Transfer((w, merchants), (w, merchants, exposure), _coef.InsurancePayoutFrac.Value * throughput * risk);
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
                if (r.Name == host)
                    return sim.World.SelfPower;
        return null;
    }

    /// <summary>
    /// 自国の分母＝養える軍の人口の側（兵役の母数×MaxSoldierFraction＝自分の軍事費の段に依らない＝自国の段を上げても不安は下がらない＝段の往復を作らない）。〔履歴 MultiNationSim.cs#143〕
    /// </summary>
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
            own[i] = w.Coef.MaxSoldierFraction.Value * man;
        }
        var borderHome = new double[n * n];   // borderHome〔履歴 MultiNationSim.cs#144〕
        var border = ArmyShape.Borders(ps, _coef, borderHome);
        var hr = new double[n * n];   // hr[a*n+b]＝A の軍が B に届く強さ×敵意
        var hrArmy = new double[n * n];   // hrArmy〔履歴 MultiNationSim.cs#145〕
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

    // 外1：交易比重（TradeWeight）を貿易取扱高のシェアから四半期同期（A-中6 で GDP シェアから置き換え＝下の注記）。〔履歴 MultiNationSim.cs#146〕
    private void UpdateTradeWeights()
    {
        double total = 0; int count = 0;
        foreach (var p in Diplomacy.Powers)
            if (p.Nation is { } nw)
            {
                double v = Math.Max(0, nw.LastExportValue) + Math.Max(0, nw.LastImportValue);
                nw.TradeValueSmoothed = PoliticalQuantities.Toward(nw.TradeValueSmoothed, v, _coef.TradeValueSmoothing);   // 数期平滑＝単四半期の禁輸/封鎖スパイクで比重が跳ねない
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

    // 世界プール＝ゲーム外から人と持参金が湧く経路は削除）。〔履歴 MultiNationSim.cs#147〕
    internal void StepAssimilation()   // StepAssimilation〔履歴 MultiNationSim.cs#148〕
    {
        foreach (var sim in Nations)
        {
            var w = sim.World;
            // 民族統一度の同化（毎四半期・歯止め）：時間で1へ回復し、公教育/義務教育で加速＝移民希釈への対抗力（均衡を作る）。
            if (w.EthnicEnabled)
            {
                double assim = _coef.EthnicAssimilation + (w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory ? _coef.AssimilationEducationBonus : 0);
                // 識字は「自民族語の学校網・出版市場を持てたか」の代理変数）。〔履歴 MultiNationSim.cs#149〕
                bool revive = _coef.AssimilationRevival > 0;
                double rr = revive ? _coef.AssimilationRevival : _coef.AssimilationLiteracyResist;
                foreach (var (_, p) in w.AllPopsWithRegion())
                {
                    double resist = rr > 0 ? 1.0 - rr * p.Literacy : 1.0;
                    if (!revive) resist = Math.Max(0.0, resist);   // revive〔履歴 MultiNationSim.cs#150〕
                    // 逆流の核＝core×(1−core)：中核度1のPOPは1ビットも動かない（**持っていない民族は復興できない**＝
                    // 単一文化国の自国民が自分から離反する暴走を構造で閉じる）。同化側の核は従来どおり (1−core)＝core=0 から立ち上がる。
                    double kernel = resist > 0 ? 1.0 - p.CoreEthnicShare.Value : p.CoreEthnicShare.Value * (1.0 - p.CoreEthnicShare.Value);
                    p.CoreEthnicShare = new Share(PoliticalQuantities.Clamp01(p.CoreEthnicShare.Value + assim * resist * kernel));   // 倍率排除⑧：同化は per-pop で中核割合を1へ寄せる（EthnicUnity は加重平均から創発）
                }
            }
        }
    }

    // 「アメリカに流すのではなく、アメリカが豊かだから流入する」。〔履歴 MultiNationSim.cs#151〕
    internal void MigrateBetweenNations()   // internal＝番人テストが段を単離して1回だけ呼ぶため（StepAssimilation・MoveMigrants と同じ扱い）
    {
        foreach (var sim0 in Nations) { var w0 = sim0.World; w0.LastImmigration = 0; w0.LastEmigration = 0; w0.LastSpecialistExit = 0; }   // 診断（UI）：四半期の移民収支カウンタ（★段⑤：専門職の流出カウンタも同じ場所で0に＝無条件＝滞留しない）
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
            // 受入の物理上限＝アイルランド脱出級の絶対フロー（ImmigrationReceiveCapPerQuarter/四半期）×移民制限法×受入能力（混雑で縮む＝AbsorbCap）。〔履歴 MultiNationSim.cs#152〕
            double lawCap = w.ImmigrationRestriction switch
            {
                ImmigrationLaw.Selective => _coef.ImmigrationSelectiveFrac.Value,
                ImmigrationLaw.Closed => _coef.ImmigrationClosedFrac.Value,
                _ => 1.0,
            };
            foreach (var r in w.Regions)
            {
                double o = Territory.Opportunity(r, _coef);
                if (!double.IsFinite(o)) continue;   // IsFinite〔履歴 MultiNationSim.cs#153〕
                var (la, lo) = Places.Coord(r.Name);
                int classes = 0;
                foreach (var p in r.Pops) if (!p.IsSoldier) classes |= 1 << p.ClassId;   // MoveMigrants が移せる相手（PopOf と同じ述語）
                dst.Add((d, r, o, la, lo, _coef.ImmigrationReceiveCapPerQuarter.Value * r.SumPopulation() * lawCap * Territory.AbsorbCap(r, _coef), classes));
            }
        }
        int m = dst.Count;
        // 2〜4. 送出州ごと・階級ごとの流量を決める（まだ移さない）
        var plan = new List<(World Src, Region SrcR, int Cls, int X, double Flow)>();
        var toX = new double[m]; var gap = new double[m];
        long seenPairs = 0;   // seenPairs〔履歴 MultiNationSim.cs#154〕
        for (int s = 0; s < n; s++)
        {
            var src = Nations[s].World;
            foreach (var r in src.Regions)
            {
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
                    // 危機時の上限緩和：故郷の飢饉(1-FoodFill)に比例して MaxFrac を引き上げ＝アイルランド型の大量脱出。〔履歴 MultiNationSim.cs#155〕
                    double effFill = src.FoodFill + (r.FoodFill - src.FoodFill);
                    double srcFamine = Math.Max(0.0, 1.0 - effFill);
                    double maxFrac = _coef.EmigrationMaxPerQuarter.Value * (1.0 + _coef.FaminePushMaxFracBoost * srcFamine);
                    double flow = Math.Min(_coef.EmigrationRate * gMax, maxFrac) * sp.Size;
                    if (!(flow > 1e-9)) continue;   // ★否定形＝NaN の流量を弾く
                    for (int x = 0; x < m; x++)
                    {
                        if (!(gap[x] > 0) || (dst[x].Classes >> cls & 1) == 0) continue;   // その階級の POP が居ない州の分は送出州に残る
                        double f = flow * (gap[x] * dstA[dst[x].D] / wSum);
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
        var sp = srcR.PopOf(cls);
        var dp = dstR.PopOf(cls);
        if (sp is null || dp is null || !(sp.Size >= 1e-9)) return;   // ★否定形＝NaN の Size も弾く
        if (!(flow > 1e-9)) return;   // ★否定形＝NaN の流量を弾く（旧は NaN 素通しで Size/Savings を直接汚染した）
        if (Instruments.On) Instruments.CrossBorder(srcR, dstR, flow);   // 計器（読むだけ）
        double moved = MoveAbroad(src, dst, sp, dp, flow);
        if (!(moved > 0)) return;
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）：越境の流出/流入
    }

    /// <summary>
    /// 国債の保有は発行国の対外債へ振り替える（Fiscal.BookForeignDebt）＝運ばない。〔履歴 MultiNationSim.cs#156〕
    /// </summary>
    private static double MoveAbroad(World src, World dst, Pop sp, Pop dp, double flow)
    {
        double frac = flow / sp.Size;
        double sav = sp.Savings.Value * frac;   // 持参金＝1人あたり貯蓄ぶん
        // 持参金＝持っている貯蓄のまま（負の貯蓄〔借越〕も移民に付いて行く）。
        MoneyStock savOut = new MoneyStock(sav);
        MoneyStock debtOut = sp.Debt * frac;
        Fiscal.BookForeignDebt(sp, sp.BondHoldings * frac, src, dst);   // BookForeignDebt〔履歴 MultiNationSim.cs#157〕
        double moved = Pop.Blend(sp, dp, flow, coreAtDest: dst.EthnicEnabled ? 0.0 : dp.CoreEthnicShare.Value, money: false);
        if (!(moved > 0)) return 0;
        MoneyStock savIn = World.CrossFx(src, dst, savOut);     // 換算（差は換算の台帳へ）
        MoneyStock debtIn = -World.CrossFx(src, dst, -debtOut); // 負債は負の額として換算＝台帳の差の符号が基礎貨幣（貯蓄−与信）と揃う
        Pop.Carry(sp, dp, savOut, debtOut, default, savIn, debtIn);   // お金（持参金・債務）＝国債の保有は上で振り替えた＝運ばない
        src.PopulationDelta -= moved; dst.PopulationDelta += moved;                      // 人口台帳（両国で相殺）
        src.CumulativeTradeFlow -= savOut - debtOut; dst.CumulativeTradeFlow += savIn - debtIn;   // 持参金−債務＝対外移転（基礎貨幣の台帳が閉じる・各自の通貨）
        src.Bank!.Carry(dst.Bank, debtOut, debtIn);                                      // 家計の与信も債務について行く（S3b・受入国の通貨）
        return moved;
    }

    private static readonly int[] MigrantClasses = [PopClass.Peasant, PopClass.Worker];   // 農民・労働者
    private static readonly int[] SpecialistClasses = [PopClass.Engineer, PopClass.Researcher];   // ★段④：技師・研究者（インテリ＝希少資源）

    // ここを混ぜてはならない：   ★大衆は【自発的に】動く＝機会の高い方へ流れる（欧州→新大陸）。〔履歴 MultiNationSim.cs#158〕
    private void RecruitSpecialists()
    {
        if (!(_coef.RecruitCostPerHead > 0)) return;   // ⚠0なら経路に入らない（0除算も踏まない。〔履歴 MultiNationSim.cs#159〕
        int n = Nations.Count;
        // MoveSpecialists が書くのは送出州と受入州の POP と2国の台帳だけ   ＝招聘が起きた2国だけ引き直せば、旧と同じ値を同じ順で比べる。〔履歴 MultiNationSim.cs#160〕
        var srcBest = new Region?[n]; var srcPool = new double[n]; var srcOpp = new double[n]; var srcKnown = new bool[n];
        for (int d = 0; d < n; d++)
        {
            var dst = Nations[d].World;
            if (!(dst.RecruitBudget > 0)) continue;   // ★否定形＝NaN の予算も弾く
            // 受入先＝機会が最も高い州（★大衆移民の行き先選定と同じ物差し＝新しいスカラーを置かない）
            Region? dstR = null; double dstO = double.NegativeInfinity;
            foreach (var r in dst.Regions)
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
                premium *= 1.0 + Geo.DistanceKm(src.Latitude, src.Longitude, dst.Latitude, dst.Longitude) / Diplomacy.DistanceNormKm;
                double perHead = _coef.RecruitCostPerHead * premium;
                if (!(perHead > 0) || !double.IsFinite(perHead)) continue;
                double heads = System.Math.Min(dst.RecruitBudget / perHead, pool * _coef.RecruitMaxFracOfSource.Value) * (1.0 - ban);
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
            moved += MoveAbroad(src, dst, sp, dp, flow);
        }
        if (!(moved > 0)) return;
        // ★支度金＝国庫→呼ばれた本人（⚠受入国の内側で保存する。〔履歴 MultiNationSim.cs#161〕
        double cost = System.Math.Min(perHead * moved, dst.RecruitBudget) * dst.FiscalFundingRatio.Value;   // cost〔履歴 MultiNationSim.cs#162〕
        dst.RecruitBudget -= World.Transfer((dst, null), (dst, arrived, arrivedFlow), cost, TreasuryCat.Modernization);
        src.LastEmigration += moved; dst.LastImmigration += moved;           // 診断（UI）
        src.LastSpecialistExit += moved;                                     // ★段⑤：抜かれた側の実測＝流出禁止法の選好の入力（抜かれていない国では法が争点にならない）
        (_recruitLog ??= new()).Add((src, dst, moved));                      // ★段2A：発見の伝わりの技師の移動（同じ四半期末の発見の段が読んで空にする）
    }

    // Year〔履歴 MultiNationSim.cs#163〕

    public int Year => Nations.Count > 0 ? Nations[0].World.Year : 0;

    // ⚠作業領域＝セーブに載らない（[NonSerialized]＝SaveGame が辿らない）・シミュの値はどれも読まない。〔履歴 MultiNationSim.cs#164〕
    [System.NonSerialized] private long[]? _cost;
    [System.NonSerialized] private long[]? _costDeparted;
    [System.NonSerialized] private long _clockTicks, _clockVisits;   // 軌道は読まない〔履歴 MultiNationSim.cs#165〕
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

    /// <summary>
    /// 値＝Σ(全国の活きている州の全 POP の LastEarnedPerMonth÷その国の PriceLevel)÷Σ(同じ POP の Size)（人口加重・閾値なし）。〔履歴 MultiNationSim.cs#166〕
    /// </summary>
    internal void DistributeWorldRealIncome()
    {
        double incomeSum = 0, populationSum = 0;
        long pops = 0;
        foreach (var nation in Nations)
        {
            var w = nation.World;
            double earned = 0, size = 0;
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Pops) { earned += p.LastEarnedPerMonth.Value; size += p.Size; }
                pops += r.Pops.Count;
            }
            double real = earned / w.PriceLevel;
            if (size > 0 && double.IsFinite(real)) { incomeSum += real; populationSum += size; }
        }
        double worldPerCapita = populationSum > 0 ? incomeSum / populationSum : 0;
        foreach (var nation in Nations) nation.World.WorldRealIncomePerCapitaPerMonth = worldPerCapita;
        Cost[CostLedger.LuxuryRatioPops] += pops;
    }
}

/// <summary>
/// 処理ごとの「見た件数」を数える**診断**であって、シミュの状態ではない。〔履歴 MultiNationSim.cs#167〕
/// </summary>
public static class CostLedger
{
    public const int PriceGoods = 0, PriceDailyRuns = 1, ConstructionSeen = 2, CreditSpans = 3, ConsumePopGoods = 4, ProducerMonths = 5,
        ClaimProducers = 6, MonthlyPops = 7, ClearingRows = 8, ClearingFills = 9, DiploPowerMonths = 10, DiploHegemonScans = 11,
        QuarterStartPops = 12, QuarterEndPops = 13, SharedNationQuarters = 14, GdpScans = 15, MigrationPairs = 16, DiscoveryNations = 17, ThreatReachPairs = 18, DiscoveryPriceNations = 19, LuxuryRatioPops = 20, AiBureaucracyScans = 21, ParticipationPops = 22, DiscoveryWorldScans = 23, MinimumWageBasketScans = 24, AgeBandExtraPops = 25, RecipeEquipmentScans = 26, StapleLedgerPops = 27, NavalOrderScans = 28, ClearingSkipsLB = 29, LaborPowerScans = 30,
        ConsumeSkipZeroPop = 31, ConsumeZeroPopIndep = 32, DeadProdZeroPop = 33, DeadProdBankrupt = 34, DeadProdService = 35, DeadProdRgo = 36, DeadProdOther = 37, ZeroPopMonth = 38, ZeroPopEmptyMonth = 39, DeadMarketMonth = 40, ClearingKmCompose = 41, ClearingKmRaw = 42, Count = 43;

    /// <summary>計数の名前（番人の表・測りのファイル・docs の「処理の刻みの表」の列が同じ名前を使う）。</summary>
    public static readonly string[] Names =
    [
        "価格：財の歩（州×財×旬）", "価格：日次の写像の回", "建設：見た生産者（旬）", "信用：国×旬", "消費：POP×財（月）", "生産：生産者×月",
        "請求：見た生産者（月）", "月次：POP×月（決算の残り）", "清算：見た買い手行", "清算：着値の充填", "外交：国×月", "外交：覇権の判定の国×国",
        "四半期首：POP×国（AI・議会・政府）", "四半期末：POP×国", "共有層：国×四半期（国力・移民・国際金融ほか）",
        "GDP：州×生産者の和（取り直した分）", "移民：州×州の組（国内・越境）", "発見：国×四半期（点数の判定）", "外交：脅威の届き方（脅威を持つ国×国）",
        "発見：平滑化した値段の歩（国×四半期）",
        "奢侈品と家事奉仕の所得弾力：POP×月（世界の値の配りの走査と買い手の POP ごとの係数）",
        "AI の官僚制度の走査：国×四半期（欲しい法の表の走査と前提・門の照会）",
        "不満度を読む：POP×国×四半期（参加の述語〔旗 ParticipationDiscontent が正のとき〕と思想の更新の入力〔常に〕）",
        "発見：世界の高度労働力の雇用の合計の巡回（訪れた生産者×四半期・時計の形が正のときだけ）",
        "最低賃金の籠の走査：床を求めた回の（州の財＋生産者）の走査（法が施行されているときだけ）＋AI の判断の読み（旗 MinimumWageAiFromFloor が正のときの本国の州の生産者と財の走査・(486)）",
        "年齢の層の歩：POP×月×（層の数−8）（8層の算術は月次・消費の単価の中。作者の 11 層の増分だけ・8層では 0 件）",
        "レシピごとの設備の配り：購入が起きた州×月の生産者の数（旗 RecipeEquipment が正のとき・配りは 3 周・退場は 2 周の走査 1 周につき州の生産者の数）",
        "主食の台帳と配り直し：POP×月×段（旗 StapleLedgerSum が正のとき決済後の締め 1 段・旗 StapleDemandRebalance が正のとき月の頭の前計算 1 段・(504)）",
        "海軍の発注の配り：州×月×3 周（NavalPortSplit の本国格の港の取扱高の和と割合の 2 周＋予算の配りの州ループ 1 周・(669)）",
        "清算：着値の下界を評価した売り手（飛ばした＋通した・距離と危険料を引かず下界の比較だけ・(1029)(1036)）",
        "労働の重み：州の動力の割合 m の生産者の走査（旗 LaborWeightsByPower が正の州×月・州の生産者の数・(1045)）",
        "消費：飛ばした人口 0 の POP×財（人数がちょうど 0.0 の POP を必要量の輪と支払いの輪で飛ばした件数・計器つきでは 0・(1072)）",
        "消費：人口 0 の POP×財（独立）（州×月に POP を自分で 1 周して人数 0.0 を数え、見た財の数を掛けた・(1072)）",
        "中身の無い物：能力0・産出0の生産者×月（担い手の人口 0・計器 (1072)）",
        "中身の無い物：能力0・産出0の生産者×月（倒産）",
        "中身の無い物：能力0・産出0の生産者×月（役務）",
        "中身の無い物：能力0・産出0の生産者×月（産地）",
        "中身の無い物：能力0・産出0の生産者×月（その他）",
        "中身の無い物：人数 0.0 の POP×月（月の頭・計器 (1072)）",
        "中身の無い物：そのうちお金も債務も所得も無い POP×月",
        "中身の無い物：州×財で需要・産出・在庫が全て 0（州×月・計器 (1072)）",
        "清算：距離の表の成分からの組み直し（対・費用だけが動いた州の対・(1097)）",
        "清算：距離の表の素の計算（PairKmCore 相当・成分を引いた対・(1097)）",
    ];

    /// <summary>
    /// 単価（マイクロ秒/件）。〔履歴 MultiNationSim.cs#168〕
    /// </summary>
    public static readonly double[] UnitMicros =
    [
        0.22570,   // 注〔履歴 MultiNationSim.cs#169〕
        0.14748,   // 注〔履歴 MultiNationSim.cs#170〕
        0.01300,   // 建設：見た生産者＝48.618 秒÷（走査 25.45億＋工事のある州の生産者 530.9万州×166）
        2.58278,   // 信用：国×呼び出し＝11.040 秒÷391.7万
        0.02801,   // 必要の無い財は楔・価格弾力の冪を求めない〔恒等〕＝消費の段の時間の比。〔履歴 MultiNationSim.cs#171〕
        0.48553,   // 作業配列3本を消さない (spf) の分は取り直していない＝推定は下げない。〔履歴 MultiNationSim.cs#172〕
        0.03490,   // 請求：見た生産者＝4.129 秒÷1.084億
        1.73157,   // 注〔履歴 MultiNationSim.cs#173〕
        0.19264,   // 注〔履歴 MultiNationSim.cs#174〕
        0.08490,   // 注〔履歴 MultiNationSim.cs#175〕
        75.28385,  // 注〔履歴 MultiNationSim.cs#176〕
        0.004705,  // 注〔履歴 MultiNationSim.cs#177〕
        3.36578,   // 注〔履歴 MultiNationSim.cs#178〕
        1.05863,   // 注〔履歴 MultiNationSim.cs#179〕
        110.34900, // 注〔履歴 MultiNationSim.cs#180〕
        0.015503,  // 注〔履歴 MultiNationSim.cs#181〕
        0.016708,  // 注〔履歴 MultiNationSim.cs#182〕
        DiscoveryUnitMicros,   // DiscoveryUnitMicros〔履歴 MultiNationSim.cs#183〕
        ThreatReachUnitMicros,   // ThreatReachUnitMicros〔履歴 MultiNationSim.cs#184〕
        DiscoveryPriceUnitMicros,   // DiscoveryPriceUnitMicros〔履歴 MultiNationSim.cs#185〕
        LuxuryRatioUnitMicros,   // 採ったら B の .cost の実測で取り直す〔履歴 MultiNationSim.cs#186〕
        AiBureaucracyScanUnitMicros,   // AiBureaucracyScanUnitMicros〔履歴 MultiNationSim.cs#187〕
        ParticipationUnitMicros,   // ParticipationUnitMicros〔履歴 MultiNationSim.cs#188〕
        DiscoveryWorldScanUnitMicros,   // DiscoveryWorldScanUnitMicros〔履歴 MultiNationSim.cs#189〕
        MinimumWageBasketScanUnitMicros,   // MinimumWageBasketScanUnitMicros〔履歴 MultiNationSim.cs#190〕
        AgeBandExtraUnitMicros,   // 8層では件数 0〔履歴 MultiNationSim.cs#191〕
        RecipeEquipmentScanUnitMicros,   // RecipeEquipmentScanUnitMicros〔履歴 MultiNationSim.cs#192〕
        StapleLedgerUnitMicros,   // StapleLedgerUnitMicros〔履歴 MultiNationSim.cs#193〕
        NavalOrderScanUnitMicros,   // NavalOrderScanUnitMicros〔履歴 MultiNationSim.cs#194〕
        ClearingSkipUnitMicros,   // ClearingSkipUnitMicros〔履歴 MultiNationSim.cs#195〕
        LaborPowerScanUnitMicros,   // LaborPowerScanUnitMicros〔履歴 MultiNationSim.cs#196〕
        0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0,   // 注〔履歴 MultiNationSim.cs#197〕
        ClearingKmComposeUnitMicros,   // ClearingKmComposeUnitMicros〔履歴 MultiNationSim.cs#198〕
        ClearingKmComposeUnitMicros,   // ClearingKmComposeUnitMicros〔履歴 MultiNationSim.cs#199〕
    ];
    /// <summary>
    /// 1 件＝生産者 1 つの動力の割合の判定（電化率・蒸気率・産出の読み）＋労働力の投入の有無の確かめ。〔履歴 MultiNationSim.cs#200〕
    /// </summary>
    public const double LaborPowerScanUnitMicros = 0.055;
    /// <summary>
    /// 単価の根拠は measurements/m1029_*（小さな計り台）。〔履歴 MultiNationSim.cs#201〕
    /// </summary>
    public const double ClearingKmComposeUnitMicros = 0.0100;
    public const double ClearingSkipUnitMicros = 0.02;
    /// <summary>
    /// ⚠壁時計で、3本のバッチと突然変異のビルドが重なった負荷の下の値＝上限寄り（census で取り直すのは申し送り）。〔履歴 MultiNationSim.cs#202〕
    /// </summary>
    public const double AiBureaucracyScanUnitMicros = 12.76;
    /// <summary>
    /// 1.2＝四半期末 POP×国の単価 1.05863（Politics.Step の POP ごとの全費用＝不満度 1 回〔理想点の配列・発言権・生活の欲求〕はその一部）＋運動体の辞書の出し入れ 約 0.14。〔履歴 MultiNationSim.cs#203〕
    /// </summary>
    public const double ParticipationUnitMicros = 1.2;
    /// <summary>
    /// 巡回は技師を雇わない生産者（一次産品の大半）で門の照会を飛ばす。〔履歴 MultiNationSim.cs#204〕
    /// </summary>
    public const double DiscoveryWorldScanUnitMicros = 0.06;
    /// <summary>
    /// その 6 秒を 1 試合の POP×月（約 842 万＝台帳「月次：POP×月」の件数）×3 層（11−8）= 約 2,527 万で割った 0.24。〔履歴 MultiNationSim.cs#205〕
    /// </summary>
    public const double AgeBandExtraUnitMicros = 0.24;
    /// <summary>
    /// 配りは州の生産者を 3 周（合計〔適格かどうかの判定＋発見の門〕・配り・薄め）、退場は 2 周するだけで、熱いループには足さない。〔履歴 MultiNationSim.cs#206〕
    /// </summary>
    public const double RecipeEquipmentScanUnitMicros = 0.00604;
    /// <summary>
    /// 前計算は POP ごとに主食 5 財の乗算と加算（約 20 演算）、締めは POP ごとに 2 回の比較。〔履歴 MultiNationSim.cs#207〕
    /// </summary>
    public const double StapleLedgerUnitMicros = 0.04;
    /// <summary>
    /// 件数の上限＝国×月×州×3 周（61 国で全長 約 8 千州×月 3 周でも 1 試合 0.1 秒未満）。〔履歴 MultiNationSim.cs#208〕
    /// </summary>
    public const double NavalOrderScanUnitMicros = 0.02;
    /// <summary>
    /// 形が近い GDP の和（0.0155）と奢侈品の所得比の走査（0.0155）より少し重い 0.02（財ごとに層の分岐＋市場の需要・値段を読む）。〔履歴 MultiNationSim.cs#209〕
    /// </summary>
    public const double MinimumWageBasketScanUnitMicros = 0.02;
    /// <summary>
    /// 〔履歴 MultiNationSim.cs#210〕
    /// </summary>
    public const double DiscoveryPriceUnitMicros = 19.63;
    /// <summary>
    /// census は無い＝GDP の和の1要素あたり（0.0155）から置いた推定（走査の形が近い＝州→POP をなぞって2つの和を足す）。〔履歴 MultiNationSim.cs#211〕
    /// </summary>
    public const double LuxuryRatioUnitMicros = 0.0155;
    /// <summary>
    /// 世界の平滑化した値段（国×財の和・四半期に1回）は「共有層：国×四半期」の中（1回 約8千の積和＝小さい分）。〔履歴 MultiNationSim.cs#212〕
    /// </summary>
    public const double DiscoveryUnitMicros = 98.1;
    /// <summary>
    /// 脅威の緊張（Diplomacy.ThreatTension＝月次〔四半期末の外交3回〕）が脅威を持つ国×経済を持つ国の組ごとに1回呼ぶ＝件数は「脅威を持つ国×国×月」。〔履歴 MultiNationSim.cs#213〕
    /// </summary>
    public const double ThreatReachUnitMicros = 0.535;

    /// <summary>
    /// 〔履歴 MultiNationSim.cs#214〕
    /// </summary>
    public const double BudgetTargetCpuSeconds = 387.0;
    /// <summary>
    /// ★予算の上限（推定の計算量・1試合の秒）＝今の値（速くなるたびに下げるだけ＝目標に届くまで）。〔履歴 MultiNationSim.cs#215〕
    /// </summary>
    public const double BudgetLimitEstSeconds = 248.5;
    /// <summary>
    /// 上限を置いたときの覆う割合（推定÷実測の CPU・バッチの条件）＝目標を推定の単位へ写す（目標の推定＝目標の CPU×これ）。〔履歴 MultiNationSim.cs#216〕
    /// </summary>
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
