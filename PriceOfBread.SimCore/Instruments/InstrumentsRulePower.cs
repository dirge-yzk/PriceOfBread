using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);

namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンの状態を作らない（蓄えは計器の側の ConditionalWeakTable〈試合の Diplomacy ごと〉）。〔履歴 InstrumentsRulePower.cs#1〕
public static partial class Instruments
{
    internal const string RulePowerHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tqIndex\tnIndex\ttauIndex\tlNationIndex\twNationIndex\twEngageNationIndex\thNationIndex\toNationIndex\toWorldShare\tnavalNationIndex\tnavalAllocIndex\t"
        + "fStateIndex\tfInterIndex\tfResistIndex\tfPushbackIndex\tfUngovIndex\tfForeignRawIndex\tfForeignSumIndex\tfForeignTopIndex\tforeignTopKey\tpressureIndex\tinterRatioIndex\t"
        + "interShare\tkappaIndex\townerSideShare\tlocalSideShare\tforeignSideShare\tungovSideShare\tclassSumShare\tbestClass\tnowBestClass\tnowRule0Share\tnowRule1Share\tnowRule2Share\tnowRule3Share\tnewRule0Share\tnewRule1Share\t"
        + "rhoNewIndex\trhoSourceCode\treachGreatCircleIndex\trhoDirectRailGateIndex\trhoDirectNoRailIndex\trhoDirectFullRailIndex\tdirectKm\tdirectLandFlag\trailCapacityIndex\topenQualFlag\topenDiffIndex\tphiStarIndex"
        // trhoNoFreezeIndex〔履歴 InstrumentsRulePower.cs#2〕
        + "\trhoNoFreezeIndex\ttauNoFreezeIndex\truleInit0Share\truleInit1Share\truleInit2Share\truleInit3Share\townerSideShareR\tungovSideShareR\tiProbeShare\townerWeightShare"
        // (1117) S5：計器が自分で数えた段 C′ の形〈D〉と、エンジンの使った値〈Eng〉（旗 RuleFromPower＝0 は nan）。どちらも四半期の最終日の Close の前の盤面。数は "R"。
        + "\tqNowIndex\trhoDIndex\ttauDIndex\tiDShare\thDNationIndex\toDNationIndex\tfForeignVbIndex\tt0DShare\tt1DShare\tt3DShare\tbestClassD"
        + "\trhoEngIndex\ttauEngIndex\tiEngShare\tqEngIndex\thEngNationIndex\toEngNationIndex\tfForeignEngIndex\tt0EngShare\tt1EngShare\tt3EngShare\tbestClassEng";
    /// <summary>
    /// 数は "R"。〔履歴 InstrumentsRulePower.cs#3〕
    /// </summary>
    internal const string RpAllocHeaderText = "quarter\tyear\tnation\tregion\tcandKind\tyIndex\trhoF\tsShare\tbShare\twgtIndex\tallocIndex\tfForeignIndex"
        + "\trhoFNoFreezeIndex\twDIndex\tallocDIndex\trhoFDIndex\twEngIndex\tallocEngIndex\tfForeignEngIndex";   // (1117) S5
    internal const string RulePeaceHeaderText = "quarter\tyear\twinner\tloser\tscoreIndex\tmonths\tloserRegionCount\ttakenCount\ttakenPopShare\tflowIndex\tloserLandIndex\twinnerLandIndex\tphiStarMedianIndex\tgreatCircleOnlyTakenCount";

    /// <summary>力の式の結果（陸軍力の単位）。</summary>
    internal readonly record struct RpForces(double FS, double FI, double FR, double A, double FU);

    /// <summary>州の力（設計 §3-2 の表）：F_S＝国内の分(hShare＝H_n×人口の取り分×ρ)＋A の中核の分／F_I＝N×τ×RuleInit[1]×仲介者の比／F_R＝N×圧力÷閾値＋A の中核でない分／A＝N×国民意識×X／F_U＝N×τ×RuleInit[3]。
    /// 純関数（NaN は 0 として扱う＝否定形の守り）。計器とエンジン（段 C′）が同じ関数を呼ぶ。</summary>
    internal static RpForces RulePowerForces(double n, double tau, double hShare, double ruleInit1, double interRatio, double ruleInit3, double pressure, double secessionThreshold, double consciousness, double xForeign, double coreShare)
    {
        static double P(double x) => x > 0 ? x : 0;
        double a = P(n) * P(consciousness) * P(xForeign);
        double core = coreShare > 1 ? 1 : P(coreShare);
        double fS = P(hShare) + a * core;
        double fI = P(n) * P(tau) * P(ruleInit1) * P(interRatio);
        double fR = secessionThreshold > 0 ? P(n) * P(pressure) / secessionThreshold + a * (1 - core) : a * (1 - core);
        double fU = P(n) * P(tau) * P(ruleInit3);
        return new RpForces(fS, fI, fR, a, fU);
    }

    /// <summary>4 区分の割合（設計 §3-5・§3-6）：持ち主の側＝F_S＋κ×F_I／自立した在地＝(1−κ)×F_I＋F_R／外国＝Σ F_f／無統治＝F_U を 力の和 で割る。和が 0 以下なら全て NaN（寄せない）。
    /// 最大の区分の番号＝0 持ち主・1 在地・2 外国・3 無統治（同点は若い番号）。</summary>
    internal static (double Owner, double Local, double Foreign, double Ungov, double Total, int Best) RulePowerClasses(in RpForces f, double foreignSum, double kappa)
    {
        double k = kappa > 1 ? 1 : kappa > 0 ? kappa : 0;
        double fs = foreignSum > 0 ? foreignSum : 0;
        double tot = f.FS + f.FI + f.FR + fs + f.FU;
        if (!(tot > 0)) return (double.NaN, double.NaN, double.NaN, double.NaN, 0, -1);
        double o = (f.FS + k * f.FI) / tot, l = ((1 - k) * f.FI + f.FR) / tot, fo = fs / tot, u = f.FU / tot;
        int best = 0; double bv = o;
        if (l > bv) { best = 1; bv = l; }
        if (fo > bv) { best = 2; bv = fo; }
        if (u > bv) { best = 3; }
        return (o, l, fo, u, tot, best);
    }

    static string RpR(double v) => double.IsNaN(v) ? "nan" : double.IsInfinity(v) ? (v > 0 ? "inf" : "-inf") : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    static readonly string[] RpClassNames = ["owner", "local", "foreign", "ungov"];

    /// <summary>届き D(a,b)＝1÷(1＋km÷DistanceNormKm)（設計 §3-4）。km が非有限なら 0。</summary>
    internal static double RulePowerReach(double km) => double.IsFinite(km) && km >= 0 ? 1.0 / (1.0 + km / Diplomacy.DistanceNormKm) : 0.0;

    /// <summary>四半期の最初の清算の州の表の複写（BilateralMarket が On のときだけ書く）。</summary>
    internal sealed class RpSnap
    {
        public TrEntry[] Tr = []; public int Tc; public int[] SeaNodeOf = [], LandNode = []; public double[] SeaHalf = [], TerrCost = [], LandRatio = [], LandGraft = [];
        public bool[] SuezOkN = [], PanOkN = []; public double LandRoute; public Dictionary<Region, int> Idx = new(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        public double[]? Km;   // tc×tc の遅延の表（NaN＝未計算）
        public double Get(int a, int b, double[]? landRatioOverride = null)
        {
            if (landRatioOverride is not null) return PairKmCoreOf(a, b, landRatioOverride);
            Km ??= NewKm(Tc); int ix = a * Tc + b; double v = Km[ix];
            if (double.IsNaN(v)) { v = PairKmCoreOf(a, b, LandRatio); Km[ix] = v; }
            return v;
        }
        static double[] NewKm(int tc) { var k = new double[tc * tc]; System.Array.Fill(k, double.NaN); return k; }
        double PairKmCoreOf(int a, int b, double[] lr) => InternationalMarket.PairKmCore(Tr, SeaNodeOf, SeaHalf, SuezOkN, PanOkN, TerrCost, lr, LandNode, LandGraft, LandRoute, a, b, out _);
        /// <summary>(1117) 影の ρ の直し：地形の係数も差し替える（旗 RailByRegion の国の〈線路なし〉〈満線〉）。</summary>
        public double GetTerr(int a, int b, double[] lr, double[] terr) => InternationalMarket.PairKmCore(Tr, SeaNodeOf, SeaHalf, SuezOkN, PanOkN, terr, lr, LandNode, LandGraft, LandRoute, a, b, out _);
        // 凍結の印は清算が表を組む時に書く計器でない欄を読むだけ。〔履歴 InstrumentsRulePower.cs#4〕
        public TrEntry[] TrNf = [];
        public double[]? KmNf;
        public double GetNf(int a, int b)
        {
            KmNf ??= NewKm(Tc); int ix = a * Tc + b; double v = KmNf[ix];
            if (double.IsNaN(v)) { v = InternationalMarket.PairKmCore(TrNf, SeaNodeOf, SeaHalf, SuezOkN, PanOkN, TerrCost, LandRatio, LandNode, LandGraft, LandRoute, a, b, out _); KmNf[ix] = v; }
            return v;
        }
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, RpSnap> RpSnaps = new();
    /// <summary>計器の仕事の回数（番人が〈計器が切れている時は増えない〉を数える・純カウンタ）。</summary>
    internal static long RpSnapCount, RpTableCount;
    internal static RpSnap? RpSnapOf(Diplomacy d) => RpSnaps.TryGetValue(d, out var s) ? s : null;

    /// <summary>複写の口（BilateralMarket.StepBilateral の中で毎回呼ばれ、頭の On／Wants の門で何もせず戻る・実際に写すのは四半期の最初の清算）。値を書かない・清算の配列を握らない（長さ tc だけ写す）。</summary>
    internal static void RpSnapshot(bool firstClearingOfQuarter, IReadOnlyList<Simulation> nations, TrEntry[] tr, int tc, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, double[] terrCost, double[] landRatio, int[] landNode, double[] landGraft, double landRoute)
    {
        if (!On || !firstClearingOfQuarter) return;   // 計器の門はここで読む（清算の側の On の読みを増やさない・OnDoubleReadScanTests）
        if (!(Wants("rulepower_s") || Wants("rulepeace_s") || Wants("rulepoweralloc_s"))) return;
        if (nations.Count == 0 || nations[0].World.Diplomacy is not { } dip) return;
        RpSnapCount++;
        var s = new RpSnap { Tc = tc, LandRoute = landRoute, Tr = new TrEntry[tc], SeaNodeOf = new int[tc], SeaHalf = new double[tc], TerrCost = new double[tc], LandRatio = new double[tc], LandNode = new int[tc], LandGraft = new double[tc], SuezOkN = (bool[])suezOkN.Clone(), PanOkN = (bool[])panOkN.Clone() };
        System.Array.Copy(tr, s.Tr, tc); System.Array.Copy(seaNodeOf, s.SeaNodeOf, tc); System.Array.Copy(seaHalf, s.SeaHalf, tc); System.Array.Copy(terrCost, s.TerrCost, tc);
        System.Array.Copy(landRatio, s.LandRatio, tc); System.Array.Copy(landNode, s.LandNode, tc); System.Array.Copy(landGraft, s.LandGraft, tc);
        for (int i = 0; i < tc; i++) s.Idx[tr[i].r] = i;
        s.TrNf = new TrEntry[tc];
        for (int i = 0; i < tc; i++) { var e = tr[i]; if (!e.seaOk && e.r.LastPortFrozen) e.seaOk = true; s.TrNf[i] = e; }   // LastPortFrozen〔履歴 InstrumentsRulePower.cs#5〕
        RpSnaps.Remove(dip); RpSnaps.Add(dip, s);
    }

    sealed class RpState
    {
        public bool Init; public double Q;
        public readonly Dictionary<Region, double> Tau = new(); public readonly HashSet<Region> Seen = new(); public readonly Dictionary<Simulation, double> Inter0 = new();
        public readonly HashSet<EndedWarRecord> PeaceSeen = new(); public bool PeaceInit;
        public bool HeaderPower, HeaderPeace, HeaderAlloc;
        public readonly Dictionary<Region, double> TauNf = new();   // TauNf〔履歴 InstrumentsRulePower.cs#6〕
        // (1117) 段 C′ の形〈D〉：計器の側の τ の蓄え（エンジンの Region.RuleTau とは別）と、四半期の最終日の Close の前に数えた行の写し。
        public readonly Dictionary<Region, double> TauD = new(), TauDInter = new();
        public readonly Dictionary<Region, RpDRow> D = new(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        public readonly Dictionary<(string Key, Region R), (double W, double Alloc, double RhoF)> DAlloc = new();
    }
    /// <summary>(1117) 〈D〉の 1 州の値（持ち主の国の値も写す）。</summary>
    sealed class RpDRow
    {
        public double Q = double.NaN, Rho = double.NaN, Tau = double.NaN, I = double.NaN, H = double.NaN, O = double.NaN, FSum, T0 = double.NaN, T1 = double.NaN, T3 = double.NaN; public string Best = "none";
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Diplomacy, RpState> RpStates = new();

    /// <summary>国ごとの 1 四半期の作業（行を書く前の中間）。</summary>
    sealed class RpNation
    {
        public Simulation Sim = null!; public World W = null!; public Power P = null!; public string Key = "";
        public double L, WA, WB, H, O, Navy, SumN, SumPop;
        public double InterRatio;
        public Region[] Regs = [];
        public RpReg[] Rows = [];
    }
    sealed class RpReg
    {
        public Region R = null!; public double PopK, N, Tau, Pressure, Rho, RhoGc, Y, Consc, X, Core, Kappa, I, OwnerWeight;
        public int RhoSource; public RpForces F; public double HShare;
        public double RhoNf, TauNf, HShareNf;   // HShareNf〔履歴 InstrumentsRulePower.cs#7〕
        public double RhoRailGate, RhoNoRail, RhoFullRail, DirectKm; public bool DirectLand;
        public double ForeignRaw, ForeignSum, ForeignTop; public string TopKey = ""; public double NavalAlloc;
    }

    /// <summary>(1117) 地形の基礎（清算の BilateralMarket の旗 RailByRegion の枝と同じ定義を計器の側で書く）：馬の係数（水の上書き）と、満線の係数＝鉄道の係数×Clamp01(RailCostFraction)。</summary>
    static (double Horse, double Rail) RpTerrBase(Region r, Coefficients c)
    {
        int ti = (int)r.Terrain; double rcf = PoliticalQuantities.Clamp01(c.RailCostFraction);
        double bh = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
        double br = (ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0) * rcf;
        if (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) { bh = c.TerrainWaterMajorCost; br = bh * rcf; }
        else if (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) { bh = c.TerrainWaterMinorCost; br = bh * rcf; }
        return (bh, br);
    }

    static double RpOutputValue(Region r)
    {
        double g = 0;
        foreach (var s in r.Producers) { var good = r.GoodOf(s.GoodId); if (good is null || good.Tier == GoodTier.Service) continue; g += s.Output * good.RefPrice; }
        return g;
    }

    /// <summary>四半期の末：影の表を全ての国の全ての州へ書く。呼び口は Quarter の 1 行。</summary>
    static void RulePowerTable(MultiNationSim sim, ulong seed, int q)
    {
        bool wantP = Wants("rulepower_s"), wantW = Wants("rulepeace_s"), wantA = Wants("rulepoweralloc_s") && q % 40 == 0;
        if (!(wantP || wantW || wantA) || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        RpTableCount++;
        var dip = sim.Diplomacy;
        var st = RpStates.GetValue(dip, _ => new RpState());
        RpSnaps.TryGetValue(dip, out var snap);
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // ---- 国の段：L・W・H・O ----
        var nats = new List<RpNation>();
        var byKey = new Dictionary<string, RpNation>();
        double totL = 0, totPopK = 0;
        foreach (var sm in sim.Nations)
        {
            var w = sm.World; if (w.SelfPower is not { } p || w.Regions.Count == 0) continue;
            var nn = new RpNation { Sim = sm, W = w, P = p, Key = p.BaseName.Length > 0 ? p.BaseName : p.Name, L = Math.Max(0, p.LandPower), Navy = Math.Max(0, p.NavalPower), Regs = [.. w.Regions] };
            foreach (var r in nn.Regs) nn.SumPop += Math.Max(0, r.SumPopulation());
            nats.Add(nn); byKey[nn.Key] = nn; totL += nn.L; totPopK += nn.SumPop;
        }
        if (!st.Init)
        {
            if (!(totL > 0 && totPopK > 0)) return;
            st.Q = totL / totPopK; st.Init = true;   // q＝開幕の世界の陸軍力の和÷人口の和（最初の観測の四半期に 1 回・設計 §3-1）
        }
        foreach (var nn in nats)
        {
            nn.SumN = nn.SumPop * st.Q;
            var c = nn.W.Coef;
            var opp = new List<Power>();
            foreach (var wr in dip.Wars) { if (ReferenceEquals(wr.A, nn.P)) opp.Add(wr.B); else if (ReferenceEquals(wr.B, nn.P)) opp.Add(wr.A); }
            int fronts = Math.Max(1, opp.Count);
            foreach (var y in opp)
            {
                int yf = 0; foreach (var wr in dip.Wars) if (ReferenceEquals(wr.A, y) || ReferenceEquals(wr.B, y)) yf++;
                double ps = dip.FrontPush(nn.P, y, c, fronts), po = dip.FrontPush(y, nn.P, c, Math.Max(1, yf));
                double ratio = ps > 0 ? Math.Min(1.0, po / ps) : (po > 0 ? 1.0 : 0.0);
                nn.WA += nn.L / fronts * ratio; nn.WB += Math.Min(ps, po);
            }
            double wUse = Math.Min(nn.WA, nn.L);
            nn.H = Math.Min(Math.Max(0, nn.L - wUse), nn.SumN);
            nn.O = Math.Max(0, nn.L - wUse - nn.H);
            if (!st.Inter0.TryGetValue(nn.Sim, out var i0)) { i0 = Privileges.IntermediaryDegree(nn.W); st.Inter0[nn.Sim] = i0; }
            nn.InterRatio = i0 > 0 ? Privileges.IntermediaryDegree(nn.W) / i0 : 1.0;
        }
        double totO = 0; foreach (var nn in nats) totO += nn.O;

        // ---- 州の段（持ち主の国）：ρ・力・τ ----
        foreach (var nn in nats)
        {
            var c = nn.W.Coef; var cap = nn.W.Regions[0];
            int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            var rows = new RpReg[nn.Regs.Length];
            for (int k = 0; k < rows.Length; k++)
            {
                var r = nn.Regs[k];
                double popK = Math.Max(0, r.SumPopulation());
                double crowd = Politics.RegionCrowd(r);
                double press = Politics.AvgRegionMilitancy(r) * Math.Max(0, 1.0 - (c.SecessionConsciousnessWeight > 0 ? c.SecessionConsciousnessWeight * Politics.AvgRegionConsciousness(r) : 0.0))
                    * (1.0 - PoliticalQuantities.Clamp01(Politics.RegionAvgSuffrageInclusion(nn.W, r))) * (crowd > 0 ? crowd / (crowd + c.ColonyPromotionMinPop) : 0.0);
                double x = 0;
                for (int j = 0; j < r.ForeignShares.Length && j < r.ForeignRulers.Length; j++)
                {
                    if (!(r.ForeignShares[j] > 0)) continue;
                    var rp = dip.Powers.Find(pp => (pp.BaseName.Length > 0 ? pp.BaseName : pp.Name) == r.ForeignRulers[j]);
                    if (rp is null || rp.Culture != nn.P.Culture) x += r.ForeignShares[j];   // 持ち主と主流の文化が違う外国の取り分（設計 §3-2 の A）
                }
                var rr = new RpReg { R = r, PopK = popK, N = popK * st.Q, Pressure = press, Consc = Politics.AvgRegionConsciousness(r), X = x, Core = Politics.AvgRegionCoreShare(r), Y = RpOutputValue(r) };
                // 大圏の届き（今）
                var rc = Places.Coord(r.Name);
                rr.RhoGc = (rc.Lat == 0 && rc.Lon == 0) ? double.NaN : RulePowerReach(Geo.DistanceKm(nn.W.Latitude, nn.W.Longitude, rc.Lat, rc.Lon));
                rows[k] = rr;
            }
            nn.Rows = rows;
            // κ・i（ρ に依らない部分）
            foreach (var rr in rows)
            {
                var r = rr.R;
                double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0, ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0;
                rr.Kappa = ri0 > 0 ? Math.Min(1.0, (r.Rule.Length > 0 ? r.Rule[0] : 0) / ri0) : 1.0;
                double fiProbe = rr.N * 1.0 * ri1 * nn.InterRatio;   // τ 抜きの比（i は τ に依らない）
                double frProbe = rr.N * rr.Pressure / c.SecessionThreshold + rr.N * rr.Consc * rr.X * (1 - rr.Core);
                rr.I = fiProbe + frProbe > 0 ? fiProbe / (fiProbe + frProbe) : 1.0;
                rr.OwnerWeight = (r.Rule.Length > 0 ? r.Rule[0] : 0) + (r.Rule.Length > 1 ? r.Rule[1] : 0) * rr.I * rr.Kappa;
            }
            // 保有の起点（持ち主の側の取り分を重みにして）
            var stakes = new List<(int ix, double w)>();
            if (snap is not null) foreach (var rr in rows) if (snap.Idx.TryGetValue(rr.R, out var si)) stakes.Add((si, rr.OwnerWeight));
            foreach (var rr in rows)
            {
                var r = rr.R; rr.RhoSource = 0; rr.Rho = rr.RhoGc;
                if (ReferenceEquals(r, cap)) { rr.Rho = 1.0; rr.RhoSource = capIx >= 0 ? 1 : 0; }
                else if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(r, out var ri))
                {
                    double best = RulePowerReach(snap.Get(capIx, ri));
                    foreach (var (si, w) in stakes) { if (si == ri || si == capIx) continue; double via = RulePowerReach(snap.Get(capIx, si)) * w * RulePowerReach(snap.Get(si, ri)); if (via > best) best = via; }
                    rr.Rho = best; rr.RhoSource = 1;
                }
                if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(r, out var di) && !ReferenceEquals(r, cap))
                {
                    double km = snap.Get(capIx, di); rr.DirectKm = km;
                    var lr = snap.LandRatio; double[] noRail = new double[snap.Tc], full = new double[snap.Tc];
                    // 鉄道の 3 通り：発見の門（今）＝複写の比／線路なし＝LandSeaCostRatio／満線＝LandSeaCostRatio×RailCostFraction
                    System.Array.Fill(noRail, c.LandSeaCostRatio); System.Array.Fill(full, c.LandSeaCostRatio * PoliticalQuantities.Clamp01(c.RailCostFraction));
                    rr.RhoRailGate = RulePowerReach(km);
                    if (c.RailByRegion > 0)
                    {
                        // (1117) 段 R の v2：州の terrCost は φ で混ぜた値＝比だけ差し替えると〈満線〉は RailCostFraction を 2 回・〈線路なし〉は φ の分が残る。地形の基礎から組み直す（端の 2 州だけ）。
                        System.Array.Fill(noRail, c.LandSeaCostRatio); System.Array.Fill(full, c.LandSeaCostRatio);
                        double[] tNo = (double[])snap.TerrCost.Clone(), tFull = (double[])snap.TerrCost.Clone();
                        foreach (int e in (ReadOnlySpan<int>)[capIx, di]) { var (bh, br) = RpTerrBase(snap.Tr[e].r, c); tNo[e] = bh; tFull[e] = br; }
                        rr.RhoNoRail = RulePowerReach(snap.GetTerr(capIx, di, noRail, tNo)); rr.RhoFullRail = RulePowerReach(snap.GetTerr(capIx, di, full, tFull));
                    }
                    else { rr.RhoNoRail = RulePowerReach(snap.Get(capIx, di, noRail)); rr.RhoFullRail = RulePowerReach(snap.Get(capIx, di, full)); }
                    InternationalMarket.PairKmCore(snap.Tr, snap.SeaNodeOf, snap.SeaHalf, snap.SuezOkN, snap.PanOkN, snap.TerrCost, lr, snap.LandNode, snap.LandGraft, snap.LandRoute, capIx, di, out bool landFlag);
                    rr.DirectLand = landFlag;
                }
                else { rr.RhoRailGate = rr.RhoNoRail = rr.RhoFullRail = rr.DirectKm = double.NaN; }
                if (!(rr.Rho >= 0)) rr.Rho = 0;
                rr.HShare = nn.SumPop > 0 ? nn.H * rr.PopK / nn.SumPop * rr.Rho : 0;
                // RhoNf〔履歴 InstrumentsRulePower.cs#8〕
                rr.RhoNf = rr.Rho;
                if (!ReferenceEquals(r, cap) && snap is not null && capIx >= 0 && snap.Idx.TryGetValue(r, out var rn))
                {
                    double bestNf = RulePowerReach(snap.GetNf(capIx, rn));
                    foreach (var (si, w) in stakes) { if (si == rn || si == capIx) continue; double via = RulePowerReach(snap.GetNf(capIx, si)) * w * RulePowerReach(snap.GetNf(si, rn)); if (via > bestNf) bestNf = via; }
                    rr.RhoNf = bestNf >= 0 ? bestNf : 0;
                }
                rr.HShareNf = nn.SumPop > 0 ? nn.H * rr.PopK / nn.SumPop * rr.RhoNf : 0;
            }
            // τ：最初に見た州は F_S0 から解く。解けない州は国の τ の中央値
            var raw = new List<double>(); var pending = new List<RpReg>();
            foreach (var rr in rows)
            {
                if (st.Tau.TryGetValue(rr.R, out var tv)) { rr.Tau = tv; continue; }
                double ri0 = rr.R.RuleInit.Length > 0 ? rr.R.RuleInit[0] : 0;
                if (ri0 > 0 && rr.HShare > 0 && rr.N > 0) { rr.Tau = rr.HShare / (rr.N * ri0); st.Tau[rr.R] = rr.Tau; raw.Add(rr.Tau); }
                else pending.Add(rr);
            }
            if (pending.Count > 0)
            {
                raw.Sort(); double med = raw.Count > 0 ? raw[raw.Count / 2] : 0;
                if (raw.Count == 0) { var all = st.Tau.Values.OrderBy(v => v).ToList(); med = all.Count > 0 ? all[all.Count / 2] : 0; }
                foreach (var rr in pending) { rr.Tau = med; st.Tau[rr.R] = med; }
            }
            // rawNf〔履歴 InstrumentsRulePower.cs#9〕
            var rawNf = new List<double>(); var pendingNf = new List<RpReg>();
            foreach (var rr in rows)
            {
                if (st.TauNf.TryGetValue(rr.R, out var tvn)) { rr.TauNf = tvn; continue; }
                double ri0n = rr.R.RuleInit.Length > 0 ? rr.R.RuleInit[0] : 0;
                if (ri0n > 0 && rr.HShareNf > 0 && rr.N > 0) { rr.TauNf = rr.HShareNf / (rr.N * ri0n); st.TauNf[rr.R] = rr.TauNf; rawNf.Add(rr.TauNf); }
                else pendingNf.Add(rr);
            }
            if (pendingNf.Count > 0)
            {
                rawNf.Sort(); double medNf = rawNf.Count > 0 ? rawNf[rawNf.Count / 2] : 0;
                if (rawNf.Count == 0) { var allNf = st.TauNf.Values.OrderBy(v => v).ToList(); medNf = allNf.Count > 0 ? allNf[allNf.Count / 2] : 0; }
                foreach (var rr in pendingNf) { rr.TauNf = medNf; st.TauNf[rr.R] = medNf; }
            }
            foreach (var rr in rows)
            {
                var r = rr.R;
                rr.F = RulePowerForces(rr.N, rr.Tau, rr.HShare, r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, nn.InterRatio, r.RuleInit.Length > 3 ? r.RuleInit[3] : 0, rr.Pressure, c.SecessionThreshold, rr.Consc, rr.X, rr.Core);
            }
        }

        // ---- 外国の力：O を候補の州へ配る（設計 §3-3） ----
        var regOwner = new Dictionary<Region, (RpNation n, RpReg rr)>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var nn in nats) foreach (var rr in nn.Rows) regOwner[rr.R] = (nn, rr);
        var foreignAt = new Dictionary<Region, List<(string key, double f, double raw)>>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var nn in nats)
        {
            if (!(nn.O > 0) && !(nn.Navy > 0) && !wantA) continue;   // (1117) S5：配りの表を書く四半期は O＝0 かつ海軍＝0 の国の候補の行も書く（配り 0）
            var c = nn.W.Coef; var cap = nn.W.Regions[0];
            int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            var winNodes = Diplomacy.CessionWinnerNodes(nn.P);
            var stakes = new List<(int ix, double w)>();
            if (snap is not null)
            {
                foreach (var rr in nn.Rows) if (snap.Idx.TryGetValue(rr.R, out var si)) stakes.Add((si, rr.OwnerWeight));
                foreach (var kv in regOwner) { var rg = kv.Key; for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == nn.Key && rg.ForeignShares[j] > 0 && snap.Idx.TryGetValue(rg, out var si)) stakes.Add((si, rg.ForeignShares[j])); }
            }
            var opps = new HashSet<Power>(ReferenceEqualityComparer.Instance as IEqualityComparer<Power>);
            foreach (var wr in dip.Wars) { if (ReferenceEquals(wr.A, nn.P)) opps.Add(wr.B); else if (ReferenceEquals(wr.B, nn.P)) opps.Add(wr.A); }
            var cand = new List<(Region r, RpNation on, RpReg rr, double wgt, double rho, double s, int kind)>();
            foreach (var kv in regOwner)
            {
                var (on, rr) = kv.Value; if (ReferenceEquals(on, nn)) continue;
                double s = 0; var rg = kv.Key;
                for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == nn.Key && rg.ForeignShares[j] > 0) s += rg.ForeignShares[j];
                bool isCand = s > 0 || opps.Contains(on.P);
                int kind = s > 0 ? 1 : isCand ? 2 : 3;   // kind〔履歴 InstrumentsRulePower.cs#10〕
                if (!isCand) { var rc = Places.Coord(rg.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon); isCand = nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _); }
                if (!isCand) continue;
                double rho = 0;
                if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(rg, out var ri))
                {
                    rho = RulePowerReach(snap.Get(capIx, ri));
                    foreach (var (si, w) in stakes) { if (si == ri || si == capIx) continue; double via = RulePowerReach(snap.Get(capIx, si)) * w * RulePowerReach(snap.Get(si, ri)); if (via > rho) rho = via; }
                }
                else { var rc = Places.Coord(rg.Name); rho = (rc.Lat == 0 && rc.Lon == 0) ? 0 : RulePowerReach(Geo.DistanceKm(nn.W.Latitude, nn.W.Longitude, rc.Lat, rc.Lon)); }
                double b = Math.Min(1.0, Math.Max(0.0, rr.OwnerWeight));
                double wgt = rr.Y * rho * (1 - Math.Min(1.0, s)) * (1 - b);
                cand.Add((rg, on, rr, wgt > 0 ? wgt : 0, rho, s, kind));
            }
            double sumW = 0, sumWPort = 0; foreach (var cd in cand) { sumW += cd.wgt; if (cd.r.IsPort && (cd.s > 0 || opps.Contains(cd.on.P))) sumWPort += cd.wgt; }
            if (wantA && !st.HeaderAlloc) { using (var wa0 = Open("rulepoweralloc", seed)) wa0.WriteLine(RpAllocHeaderText); st.HeaderAlloc = true; }
            var allocSb = wantA ? new System.Text.StringBuilder() : null;
            foreach (var cd in cand)
            {
                double alloc = sumW > 0 ? nn.O * cd.wgt / sumW : 0;
                if (allocSb is not null)
                {
                    // (1117) S5：凍結を外した外国の ρ（同じ起点と重み）・〈D〉と〈Eng〉の配り（同じ国×州の行へ）
                    double rhoNf = cd.rho;
                    if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(cd.r, out var rf))
                    {
                        rhoNf = RulePowerReach(snap.GetNf(capIx, rf));
                        foreach (var (si, w) in stakes) { if (si == rf || si == capIx) continue; double via = RulePowerReach(snap.GetNf(capIx, si)) * w * RulePowerReach(snap.GetNf(si, rf)); if (via > rhoNf) rhoNf = via; }
                    }
                    var dA = st.D.Count > 0 && st.DAlloc.TryGetValue((nn.Key, cd.r), out var da) ? da : (double.NaN, double.NaN, double.NaN);
                    var eA = RpEngAllocOf(dip, nn.Key, cd.r);
                    allocSb.Append(string.Join('\t', I(q), I(nn.W.Year), nn.Key, cd.r.Name, I(cd.kind), RpR(cd.rr.Y), RpR(cd.rho), RpR(cd.s), RpR(Math.Min(1.0, Math.Max(0.0, cd.rr.OwnerWeight))), RpR(cd.wgt), RpR(alloc), RpR(alloc > 0 ? alloc * cd.rho : 0),
                        RpR(rhoNf), RpR(dA.Item1), RpR(dA.Item2), RpR(dA.Item3), RpR(eA.W), RpR(eA.Alloc), RpR(eA.F))).Append('\n');
                }
                if (alloc > 0)
                {
                    if (!foreignAt.TryGetValue(cd.r, out var lst)) foreignAt[cd.r] = lst = [];
                    lst.Add((nn.Key, alloc * cd.rho, alloc));
                }
                if (sumWPort > 0 && cd.r.IsPort && (cd.s > 0 || opps.Contains(cd.on.P))) cd.rr.NavalAlloc += nn.Navy * cd.wgt / sumWPort;
            }
            if (allocSb is { Length: > 0 }) System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"rulepoweralloc_s{seed}.tsv"), allocSb.ToString(), new System.Text.UTF8Encoding(false));
        }
        foreach (var kv in foreignAt)
        {
            if (!regOwner.TryGetValue(kv.Key, out var o)) continue;
            double sum = 0, raw = 0, top = 0; string tk = "";
            foreach (var (key, f, rw) in kv.Value) { sum += f; raw += rw; if (f > top) { top = f; tk = key; } }
            o.rr.ForeignSum = sum; o.rr.ForeignRaw = raw; o.rr.ForeignTop = top; o.rr.TopKey = tk;
        }

        // ---- 行 ----
        if (wantP)
        {
            var sb = new System.Text.StringBuilder();
            if (!st.HeaderPower)
            {
                using (var wr0 = Open("rulepower", seed)) wr0.WriteLine(RulePowerHeaderText);
                st.HeaderPower = true;
            }
            foreach (var nn in nats)
            {
                var c = nn.W.Coef;
                foreach (var rr in nn.Rows)
                {
                    var r = rr.R;
                    var f = rr.F;
                    var cls = RulePowerClasses(f, rr.ForeignSum, rr.Kappa);
                    double iShare = f.FI + f.FR > 0 ? f.FI / (f.FI + f.FR) : 1.0;
                    // 今の最大の区分（Region.Rule の 4 つ・持ち主の側＝Rule[0]＋Rule[1]）
                    double[] nr = [r.Rule.Length > 0 ? r.Rule[0] : 0, r.Rule.Length > 1 ? r.Rule[1] : 0, r.Rule.Length > 2 ? r.Rule[2] : 0, r.Rule.Length > 3 ? r.Rule[3] : 0];
                    double[] nowCls = [nr[0] + nr[1], 0, nr[2], nr[3]];
                    int nb = 0; for (int k = 1; k < 4; k++) if (nowCls[k] > nowCls[nb]) nb = k;
                    // 開幕の自己検査（最初に見た四半期の行だけ）
                    bool first = st.Seen.Add(r);
                    double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0, ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, ri2 = r.RuleInit.Length > 2 ? r.RuleInit[2] : 0, ri3 = r.RuleInit.Length > 3 ? r.RuleInit[3] : 0;
                    bool qual = first && ri0 > 0 && rr.HShare > 0 && f.FR == 0 && ri2 == 0 && rr.ForeignSum == 0 && r.ForeignShares.Length == 0 && f.A == 0;
                    double openDiff = double.NaN;
                    if (qual && cls.Total > 0)
                    {
                        // 開幕の目標（Rule へ写す形）：Rule[0]←F_S／Rule[1]←F_I＋F_R／Rule[3]←F_U の割合と RuleInit の差
                        openDiff = Math.Max(Math.Abs(f.FS / cls.Total - ri0), Math.Max(Math.Abs((f.FI + f.FR) / cls.Total - ri1), Math.Abs(f.FU / cls.Total - ri3)));
                    }
                    // φ*：持ち主の側が最大でなくなる国家の力の比（κ＝今の値で固定の近似）
                    double maxOther = Math.Max(Math.Max((1 - rr.Kappa) * f.FI + f.FR, rr.ForeignSum), f.FU);
                    double phi = f.FS > 0 ? Math.Max(0, (maxOther - rr.Kappa * f.FI) / f.FS) : double.NaN;
                    string cn = cls.Best >= 0 ? RpClassNames[cls.Best] : "none";
                    sb.Append(string.Join('\t', I(q), I(nn.W.Year), nn.Key, r.Name, r.Tier.ToString(), F(rr.PopK), F(st.Q), F(rr.N), F(rr.Tau), F(nn.L), F(nn.WA), F(nn.WB), F(nn.H), F(nn.O), F(totO > 0 ? nn.O / totO : double.NaN), F(nn.Navy), F(rr.NavalAlloc),
                        F(f.FS), F(f.FI), F(f.FR), F(f.A), F(f.FU), F(rr.ForeignRaw), F(rr.ForeignSum), F(rr.ForeignTop), rr.TopKey, F(rr.Pressure), F(nn.InterRatio),
                        F(iShare), F(rr.Kappa), F(cls.Owner), F(cls.Local), F(cls.Foreign), F(cls.Ungov), F(cls.Owner + cls.Local + cls.Foreign + cls.Ungov), cn, RpClassNames[nb], F(nr[0]), F(nr[1]), F(nr[2]), F(nr[3]),
                        F(cls.Total > 0 ? f.FS / cls.Total : double.NaN), F(cls.Total > 0 ? (f.FI + f.FR) / cls.Total : double.NaN),
                        F(rr.Rho), I(rr.RhoSource), F(rr.RhoGc), F(rr.RhoRailGate), F(rr.RhoNoRail), F(rr.RhoFullRail), F(rr.DirectKm), rr.DirectLand ? "1" : "0", F(nn.W.RailCapacity), qual ? "1" : "0", F(openDiff), F(phi),
                        F(rr.RhoNf), F(rr.TauNf), F(ri0), F(ri1), F(ri2), F(ri3), RpR(cls.Owner), RpR(cls.Ungov), F(rr.I), F(rr.OwnerWeight),
                        RpDCols(st.D.TryGetValue(r, out var dRow) ? dRow : null), RpEngCols(dip, r))).Append('\n');
                }
            }
            System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"rulepower_s{seed}.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
        }

        // ---- 講和ごとの影の寄せ切り（設計 §4-2・四半期の末に終戦の記録の増分を検出） ----
        if (wantW)
        {
            var fresh = new List<EndedWarRecord>();
            foreach (var e in dip.EndedWars) if (st.PeaceSeen.Add(e) && st.PeaceInit) fresh.Add(e);
            st.PeaceInit = true;   // 最初の観測の四半期より前の記録は捨てる（助走の外の戦争だけ数える）
            if (fresh.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                if (!st.HeaderPeace) { using (var wr1 = Open("rulepeace", seed)) wr1.WriteLine(RulePeaceHeaderText); st.HeaderPeace = true; }
                foreach (var e in fresh)
                {
                    if (e.Score == 0) continue;
                    var pa = dip.Powers.Find(p => p.Id == e.AId); var pb = dip.Powers.Find(p => p.Id == e.BId);
                    if (pa is null || pb is null) continue;
                    var win = e.Score > 0 ? pa : pb; var lose = e.Score > 0 ? pb : pa;
                    var wn = nats.Find(n => ReferenceEquals(n.P, win)); var ln = nats.Find(n => ReferenceEquals(n.P, lose));
                    if (wn is null || ln is null) continue;
                    // 勝者の流せる力＝L_w − min(L_w, ΣN_w)・敗者の前線の分は国内へ戻した後（講和の後は W が無い前提で H を求め直す）
                    double flow = Math.Max(0, wn.L - Math.Min(wn.L, wn.SumN));
                    double hLose = Math.Min(ln.L, ln.SumN);
                    var wcap = wn.W.Regions[0];
                    int capIx = snap is not null && snap.Idx.TryGetValue(wcap, out var cix) ? cix : -1;
                    double sumW = 0; var wg = new double[ln.Rows.Length]; var rh = new double[ln.Rows.Length];
                    for (int k = 0; k < ln.Rows.Length; k++)
                    {
                        var rr = ln.Rows[k]; var rg = rr.R;
                        double rho;
                        if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(rg, out var ri)) rho = RulePowerReach(snap.Get(capIx, ri));
                        else { var rc = Places.Coord(rg.Name); rho = (rc.Lat == 0 && rc.Lon == 0) ? 0 : RulePowerReach(Geo.DistanceKm(wn.W.Latitude, wn.W.Longitude, rc.Lat, rc.Lon)); }
                        double s = 0; for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == wn.Key && rg.ForeignShares[j] > 0) s += rg.ForeignShares[j];
                        double b = Math.Min(1.0, Math.Max(0.0, rr.OwnerWeight));
                        rh[k] = rho; wg[k] = Math.Max(0, rr.Y * rho * (1 - Math.Min(1.0, s)) * (1 - b)); sumW += wg[k];
                    }
                    int taken = 0, takenGc = 0; double takenPop = 0, popAll = 0; var phis = new List<double>();
                    for (int k = 0; k < ln.Rows.Length; k++)
                    {
                        var rr = ln.Rows[k]; var r = rr.R; var c = ln.W.Coef;
                        popAll += rr.PopK;
                        double hs = ln.SumPop > 0 ? hLose * rr.PopK / ln.SumPop * rr.Rho : 0;
                        var f = RulePowerForces(rr.N, rr.Tau, hs, r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, ln.InterRatio, r.RuleInit.Length > 3 ? r.RuleInit[3] : 0, rr.Pressure, c.SecessionThreshold, rr.Consc, rr.X, rr.Core);
                        double fw = sumW > 0 ? flow * wg[k] / sumW * rh[k] : 0;
                        var cls = RulePowerClasses(f, rr.ForeignSum + fw, rr.Kappa);
                        // 勝者の取り分が最大＝外国の区分が最大で、勝者の力が他の外国の最大を超える
                        if (cls.Best == 2 && fw >= rr.ForeignTop) { taken++; takenPop += rr.PopK; }
                        // 大圏の届きだけで同じ配りを数えた場合（今の届きとの差）
                        double rhoGcW = 0; { var rc = Places.Coord(r.Name); rhoGcW = (rc.Lat == 0 && rc.Lon == 0) ? 0 : RulePowerReach(Geo.DistanceKm(wn.W.Latitude, wn.W.Longitude, rc.Lat, rc.Lon)); }
                        double fwGc = sumW > 0 ? flow * wg[k] / sumW * rhoGcW : 0;
                        var clsGc = RulePowerClasses(f, rr.ForeignSum + fwGc, rr.Kappa);
                        if (clsGc.Best == 2 && fwGc >= rr.ForeignTop) takenGc++;
                        double mo = Math.Max(Math.Max((1 - rr.Kappa) * f.FI + f.FR, rr.ForeignSum + fw), f.FU);
                        if (f.FS > 0) phis.Add(Math.Max(0, (mo - rr.Kappa * f.FI) / f.FS));
                    }
                    phis.Sort(); double phiMed = phis.Count > 0 ? phis[phis.Count / 2] : double.NaN;
                    sb.Append(string.Join('\t', I(q), I(wn.W.Year), wn.Key, ln.Key, F(e.Score), I(e.Months), I(ln.Rows.Length), I(taken), F(popAll > 0 ? takenPop / popAll : double.NaN), F(flow), F(ln.L), F(wn.L), F(phiMed), I(takenGc))).Append('\n');
                }
                if (sb.Length > 0) System.IO.File.AppendAllText(System.IO.Path.Combine(Dir, $"rulepeace_s{seed}.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
            }
        }
        st.D.Clear(); st.DAlloc.Clear();   // (1117) 〈D〉はこの四半期の行だけに写す
    }

    // ===================== (1117) S5：段 C′ の形〈D〉を計器の側で数える（エンジンの関数を呼ばない・エンジンの τ の蓄えを読まない） =====================

    static string RpDCols(RpDRow? d) => d is null
        ? "nan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnone"
        : string.Join('\t', RpR(d.Q), RpR(d.Rho), RpR(d.Tau), RpR(d.I), RpR(d.H), RpR(d.O), RpR(d.FSum), RpR(d.T0), RpR(d.T1), RpR(d.T3), d.Best);

    /// <summary>〈Eng〉の列（エンジンの表の [NonSerialized] の写し・旗 0 は nan）。</summary>
    static string RpEngCols(Diplomacy dip, Region r) => r.RpEng is { } e && e.Stamp == dip.RpEngStamp && dip.RpEngStamp != 0
        ? string.Join('\t', RpR(e.Rho), RpR(e.Tau), RpR(e.I), RpR(e.Q), RpR(e.H), RpR(e.O), RpR(e.FSum), RpR(e.T0), RpR(e.T1), RpR(e.T3), e.Best >= 0 ? RpClassNames[e.Best] : "none")
        : "nan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnan\tnone";

    static (double W, double Alloc, double F) RpEngAllocOf(Diplomacy dip, string key, Region r)
        => dip.RpEngAlloc is { } a && a.TryGetValue((key, r), out var v) ? v : (double.NaN, double.NaN, double.NaN);

    /// <summary>今の分離の圧力（Politics.CheckSecession の式を計器の側で書き直した物・首都〔Id 0〕は 0）。</summary>
    static double RpSecessionPressure(World w, Coefficients c, Region r, Region cap)
    {
        if (r.Id == 0) return 0;
        double stance = w.Tariff * 2 - 1;
        double cult = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                    + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
        double core = Politics.AvgRegionCoreShare(r);
        if (c.CultureDistanceFromEthnic > 0) cult = PoliticalQuantities.Toward(cult, 1.0 - core, c.CultureDistanceFromEthnic);
        double mil = Politics.AvgRegionMilitancy(r);
        double p = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cult);
        if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled) p += c.SecessionCultureAdditive * c.MinorityMilitancy * cult * (1.0 - core) * mil;
        if (c.SecessionConsciousnessWeight > 0) p *= Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * Politics.AvgRegionConsciousness(r));
        p *= 1.0 - PoliticalQuantities.Clamp01(Politics.RegionAvgSuffrageInclusion(w, r));
        double crowd = Politics.RegionCrowd(r);
        p *= crowd > 0 ? crowd / (crowd + c.ColonyPromotionMinPop) : 0.0;
        return p;
    }

    sealed class RpDNat
    {
        public World W = null!; public Power P = null!; public string Key = ""; public Region[] Regs = [];
        public double L, Wf, H, O, SumPop; public double[] PopK = [], N = [], Press = [], Consc = [], X = [], Core = [], Kappa = [], I = [], Ow = [], Rho = [], Tau = [], Y = [];
        public bool[] Solved = [];
    }

    /// <summary>
    /// 呼び口：MultiNationSim の四半期の最終日・全ての国の Close の前（エンジンの表と同じ盤面）。計器の門は呼び手が読む。
    /// q_t・Vb（重みに ρ なし）・凍結を外した距離の表・I-a（τ を解く州だけ τ＝1 の試し）・今の分離の圧力で、各州の 4 区分の目標を数え、四半期の末の表へ写す。
    /// </summary>
    internal static void RpDesign(MultiNationSim sim)
    {
        if (!(Wants("rulepower_s") || Wants("rulepoweralloc_s"))) return;
        if (sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var dip = sim.Diplomacy;
        var st = RpStates.GetValue(dip, _ => new RpState());
        RpSnaps.TryGetValue(dip, out var snap);
        st.D.Clear(); st.DAlloc.Clear();
        var nats = new List<RpDNat>();
        double totL = 0, totPop = 0;
        foreach (var sm in sim.Nations)
        {
            var w = sm.World; if (w.SelfPower is not { } p || w.Regions.Count == 0) continue;
            var d = new RpDNat { W = w, P = p, Key = p.BaseName.Length > 0 ? p.BaseName : p.Name, Regs = [.. w.Regions], L = Math.Max(0, p.LandPower) };
            int n = d.Regs.Length;
            d.PopK = new double[n]; d.N = new double[n]; d.Press = new double[n]; d.Consc = new double[n]; d.X = new double[n]; d.Core = new double[n]; d.Kappa = new double[n];
            d.I = new double[n]; d.Ow = new double[n]; d.Rho = new double[n]; d.Tau = new double[n]; d.Y = new double[n]; d.Solved = new bool[n];
            for (int k = 0; k < n; k++) { d.PopK[k] = Math.Max(0, d.Regs[k].SumPopulation()); d.SumPop += d.PopK[k]; }
            nats.Add(d); totL += d.L; totPop += d.SumPop;
        }
        if (!(totL > 0 && totPop > 0)) return;
        double q = totL / totPop;
        foreach (var d in nats)
        {
            var c = d.W.Coef;
            int fronts = 0; foreach (var wr in dip.Wars) if (ReferenceEquals(wr.A, d.P) || ReferenceEquals(wr.B, d.P)) fronts++;
            fronts = Math.Max(1, fronts);
            foreach (var wr in dip.Wars)
            {
                Power y; if (ReferenceEquals(wr.A, d.P)) y = wr.B; else if (ReferenceEquals(wr.B, d.P)) y = wr.A; else continue;
                int yf = 0; foreach (var w2 in dip.Wars) if (ReferenceEquals(w2.A, y) || ReferenceEquals(w2.B, y)) yf++;
                double ps = dip.FrontPush(d.P, y, c, fronts), po = dip.FrontPush(y, d.P, c, Math.Max(1, yf));
                double ratio = ps > 0 ? Math.Min(1.0, po / ps) : (po > 0 ? 1.0 : 0.0);
                d.Wf += d.L / fronts * ratio;
            }
            double wUse = Math.Min(d.Wf, d.L), need = d.SumPop * q;   // (1123) 設計の形：足りなければ外が先に 0・前線と国内は要る量の比で分ける
            if (d.L >= wUse + need) { d.H = need; d.O = d.L - wUse - need; }
            else { d.O = 0; d.H = wUse > 0 ? d.L * need / (wUse + need) : d.L; }
        }
        // 州の段（持ち主）
        foreach (var d in nats)
        {
            var c = d.W.Coef; var cap = d.Regs[0]; double inter = Privileges.IntermediaryDegree(d.W);
            for (int k = 0; k < d.Regs.Length; k++)
            {
                var r = d.Regs[k];
                d.N[k] = d.PopK[k] * q;
                d.Press[k] = RpSecessionPressure(d.W, c, r, cap);
                d.Consc[k] = Politics.AvgRegionConsciousness(r); d.Core[k] = Politics.AvgRegionCoreShare(r); d.Y[k] = RpOutputValue(r);
                double x = 0;
                for (int j = 0; j < r.ForeignShares.Length && j < r.ForeignRulers.Length; j++)
                {
                    if (!(r.ForeignShares[j] > 0)) continue;
                    var rp = dip.Powers.Find(pp => (pp.BaseName.Length > 0 ? pp.BaseName : pp.Name) == r.ForeignRulers[j]);
                    if (rp is null || rp.Culture != d.P.Culture) x += r.ForeignShares[j];
                }
                d.X[k] = x;
                double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0, ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0;
                double rule0 = r.Rule.Length > 0 ? r.Rule[0] : 0, rule1 = r.Rule.Length > 1 ? r.Rule[1] : 0;
                d.Kappa[k] = ri0 > 0 ? Math.Min(1.0, rule0 / ri0) : 1.0;
                double core01 = d.Core[k] > 1 ? 1 : d.Core[k] > 0 ? d.Core[k] : 0;
                double a = d.N[k] * Math.Max(0, d.Consc[k]) * Math.Max(0, x);
                double fr = d.N[k] * Math.Max(0, d.Press[k]) / c.SecessionThreshold + a * (1 - core01);
                double fi;
                if (st.TauD.TryGetValue(r, out var tau)) { d.Tau[k] = tau; double i0 = st.TauDInter[r]; fi = d.N[k] * tau * Math.Max(0, ri1) * (i0 > 0 ? inter / i0 : 1.0); }
                else { d.Tau[k] = double.NaN; fi = d.N[k] * Math.Max(0, ri1); }   // τ＝1 の試し（解く州）
                d.I[k] = fi + fr > 0 ? fi / (fi + fr) : 1.0;
                d.Ow[k] = rule0 + rule1 * d.I[k] * d.Kappa[k];
            }
            int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            for (int k = 0; k < d.Regs.Length; k++)
            {
                var r = d.Regs[k]; double rho;
                if (ReferenceEquals(r, cap)) rho = 1.0;
                else if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(r, out var ri))
                {
                    rho = RulePowerReach(snap.GetNf(capIx, ri));
                    for (int s = 0; s < d.Regs.Length; s++)
                    {
                        if (!snap.Idx.TryGetValue(d.Regs[s], out var si) || si == ri || si == capIx) continue;
                        double via = RulePowerReach(snap.GetNf(capIx, si)) * d.Ow[s] * RulePowerReach(snap.GetNf(si, ri)); if (via > rho) rho = via;
                    }
                }
                else { var rc = Places.Coord(r.Name); rho = (rc.Lat == 0 && rc.Lon == 0) ? 0 : RulePowerReach(Geo.DistanceKm(d.W.Latitude, d.W.Longitude, rc.Lat, rc.Lon)); }
                d.Rho[k] = rho >= 0 ? rho : 0;
            }
            // τ を解く（最初に見た州）
            var raw = new List<double>(); var pending = new List<int>();
            for (int k = 0; k < d.Regs.Length; k++)
            {
                if (!double.IsNaN(d.Tau[k])) continue;
                var r = d.Regs[k]; double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0;
                double hs = d.SumPop > 0 ? d.H * d.PopK[k] / d.SumPop * d.Rho[k] : 0;
                if (ri0 > 0 && hs > 0 && d.N[k] > 0) { d.Tau[k] = hs / (d.N[k] * ri0); raw.Add(d.Tau[k]); d.Solved[k] = true; }
                else pending.Add(k);
            }
            if (pending.Count > 0)
            {
                double med;
                if (raw.Count > 0) { raw.Sort(); med = raw[raw.Count / 2]; }
                else { var all = new List<double>(st.TauD.Values); all.Sort(); med = all.Count > 0 ? all[all.Count / 2] : 0; }
                foreach (var k in pending) { d.Tau[k] = med; d.Solved[k] = true; }
            }
            for (int k = 0; k < d.Regs.Length; k++) if (d.Solved[k]) { st.TauD[d.Regs[k]] = d.Tau[k]; st.TauDInter[d.Regs[k]] = inter; }
        }
        // 外への配り（O＞0 の国）
        var regOf = new Dictionary<Region, (RpDNat d, int k)>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var d in nats) for (int k = 0; k < d.Regs.Length; k++) regOf[d.Regs[k]] = (d, k);
        var fsum = new Dictionary<Region, double>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var d in nats)
        {
            if (!(d.O > 0)) continue;
            var cap = d.Regs[0]; int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            var winNodes = Diplomacy.CessionWinnerNodes(d.P);
            var stakes = new List<(int ix, double w)>();
            if (snap is not null)
            {
                for (int k = 0; k < d.Regs.Length; k++) if (snap.Idx.TryGetValue(d.Regs[k], out var si)) stakes.Add((si, d.Ow[k]));
                foreach (var kv in regOf) { var rg = kv.Key; for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == d.Key && rg.ForeignShares[j] > 0 && snap.Idx.TryGetValue(rg, out var si)) stakes.Add((si, rg.ForeignShares[j])); }
            }
            var cand = new List<(Region r, double w, double rho)>(); double sumW = 0;
            foreach (var kv in regOf)
            {
                var (od, ok) = kv.Value; if (ReferenceEquals(od, d)) continue;
                var rg = kv.Key; double s = 0;
                for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == d.Key && rg.ForeignShares[j] > 0) s += rg.ForeignShares[j];
                bool warOrAdj = false;   // (1123) ②③ が先・① は持ち主より届く時だけ（下）
                foreach (var wr in dip.Wars) if ((ReferenceEquals(wr.A, d.P) && ReferenceEquals(wr.B, od.P)) || (ReferenceEquals(wr.B, d.P) && ReferenceEquals(wr.A, od.P))) { warOrAdj = true; break; }
                if (!warOrAdj) { var rc = Places.Coord(rg.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon); warOrAdj = nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _); }
                if (!warOrAdj && !(s > 0)) continue;
                double rho = 0;
                if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(rg, out var ri))
                {
                    rho = RulePowerReach(snap.GetNf(capIx, ri));
                    foreach (var (si, w) in stakes) { if (si == ri || si == capIx) continue; double via = RulePowerReach(snap.GetNf(capIx, si)) * w * RulePowerReach(snap.GetNf(si, ri)); if (via > rho) rho = via; }
                }
                else { var rc = Places.Coord(rg.Name); rho = (rc.Lat == 0 && rc.Lon == 0) ? 0 : RulePowerReach(Geo.DistanceKm(d.W.Latitude, d.W.Longitude, rc.Lat, rc.Lon)); }
                if (!warOrAdj && !(rho >= od.Rho[ok])) continue;
                double b = Math.Min(1.0, Math.Max(0.0, od.Ow[ok]));
                double wgt = od.Y[ok] * (1 - Math.Min(1.0, s)) * (1 - b);   // Vb：重みに ρ を入れない
                if (!(wgt > 0)) wgt = 0;
                cand.Add((rg, wgt, rho)); sumW += wgt;
            }
            foreach (var (rg, wgt, rho) in cand)
            {
                double alloc = sumW > 0 ? d.O * wgt / sumW : 0;
                st.DAlloc[(d.Key, rg)] = (wgt, alloc, rho);
                if (alloc > 0) { fsum.TryGetValue(rg, out var fs0); fsum[rg] = fs0 + alloc * rho; }
            }
        }
        // 目標と最大の区分
        foreach (var d in nats)
        {
            var c = d.W.Coef;
            for (int k = 0; k < d.Regs.Length; k++)
            {
                var r = d.Regs[k];
                double ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, ri3 = r.RuleInit.Length > 3 ? r.RuleInit[3] : 0;
                double i0 = st.TauDInter.TryGetValue(r, out var iv) ? iv : 0, inter = Privileges.IntermediaryDegree(d.W);
                double core01 = d.Core[k] > 1 ? 1 : d.Core[k] > 0 ? d.Core[k] : 0;
                double a = d.N[k] * Math.Max(0, d.Consc[k]) * Math.Max(0, d.X[k]);
                double fS = (d.SumPop > 0 ? d.H * d.PopK[k] / d.SumPop * d.Rho[k] : 0) + a * core01;
                double fI = d.N[k] * Math.Max(0, d.Tau[k]) * Math.Max(0, ri1) * (i0 > 0 ? inter / i0 : 1.0);
                double fR = d.N[k] * Math.Max(0, d.Press[k]) / c.SecessionThreshold + a * (1 - core01);
                double fU = d.N[k] * Math.Max(0, d.Tau[k]) * Math.Max(0, ri3);
                fsum.TryGetValue(r, out var fF);
                double tot = fS + fI + fR + fF + fU;
                var row = new RpDRow { Q = q, Rho = d.Rho[k], Tau = d.Tau[k], I = d.I[k], H = d.H, O = d.O, FSum = fF };
                if (tot > 0)
                {
                    row.T0 = fS / tot; row.T1 = (fI + fR) / tot; row.T3 = fU / tot;
                    double kap = d.Kappa[k];
                    double[] v = [(fS + kap * fI) / tot, ((1 - kap) * fI + fR) / tot, fF / tot, fU / tot];
                    int best = 0; for (int j = 1; j < 4; j++) if (v[j] > v[best] * (1 + 1e-9)) best = j;
                    row.Best = RpClassNames[best];
                }
                st.D[r] = row;
            }
        }
    }
}
