using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);

namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンの状態を作らない（蓄えは計器の側の ConditionalWeakTable〈試合の Diplomacy ごと〉）。〔履歴 InstrumentsRulePower.cs#1〕
public static partial class Instruments
{
    internal const string RulePowerHeaderText = "quarter\tyear\tnation\tregion\ttier\tpopK\tqIndex\tnIndex\ttauIndex\tlNationIndex\twNationIndex\twEngageNationIndex\thNationIndex\toNationIndex\toWorldShare\tnavalNationIndex\tnavalAllocIndex\t"
        + "fStateIndex\tfInterIndex\tfResistIndex\tfUngovIndex\tfForeignRawIndex\tfForeignSumIndex\tfForeignTopIndex\tforeignTopKey\tpressureIndex\tinterRatioIndex\t"
        + "interShare\tkappaIndex\townerSideShare\tlocalSideShare\tforeignSideShare\tungovSideShare\tclassSumShare\tbestClass\tnowBestClass\tnowRule0Share\tnowRule1Share\tnowRule2Share\tnowRule3Share\tnewRule0Share\tnewRule1Share\t"
        + "rhoNewIndex\trhoSourceCode\treachGreatCircleIndex\trhoDirectRailGateIndex\trhoDirectNoRailIndex\trhoDirectFullRailIndex\tdirectKm\tdirectLandFlag\trailCapacityIndex\topenQualFlag\topenDiffIndex\tphiStarIndex"
        // trhoNoFreezeIndex〔履歴 InstrumentsRulePower.cs#2〕
        + "\trhoNoFreezeIndex\ttauNoFreezeIndex\truleInit0Share\truleInit1Share\truleInit2Share\truleInit3Share\townerSideShareR\tungovSideShareR\tiProbeShare\townerWeightShare";
    /// <summary>
    /// 数は "R"。〔履歴 InstrumentsRulePower.cs#3〕
    /// </summary>
    internal const string RpAllocHeaderText = "quarter\tyear\tnation\tregion\tcandKind\tyIndex\trhoF\tsShare\tbShare\twgtIndex\tallocIndex\tfForeignIndex";
    internal const string RulePeaceHeaderText = "quarter\tyear\twinner\tloser\tscoreIndex\tmonths\tloserRegionCount\ttakenCount\ttakenPopShare\tflowIndex\tloserLandIndex\twinnerLandIndex\tphiStarMedianIndex\tgreatCircleOnlyTakenCount";

    /// <summary>力の式の結果（陸軍力の単位）。</summary>
    internal readonly record struct RpForces(double FS, double FI, double FR, double FU);

    /// <summary>州の力（設計 §3-2 の表）：F_S＝国内の分(hShare＝H_n×人口の取り分×ρ)／F_I＝N×τ×RuleInit[1]×仲介者の比／F_R＝N×圧力÷閾値／F_U＝N×τ×RuleInit[3]。
    /// 純関数（NaN は 0 として扱う＝否定形の守り）。計器とエンジン（段 C′）が同じ関数を呼ぶ。</summary>
    internal static RpForces RulePowerForces(double n, double tau, double hShare, double ruleInit1, double interRatio, double ruleInit3, double pressure, double secessionThreshold)
    {
        static double P(double x) => x > 0 ? x : 0;
        // (1214) 押し返し A は消した（(1193) で入力〔国民意識〕が消えて 0 の定数だった・11-12 の読みは案丙＝輪の閉じ方で満たす）。F_S は国内の分だけ・F_R は圧力の分だけ。
        double fS = P(hShare);
        double fI = P(n) * P(tau) * P(ruleInit1) * P(interRatio);
        double fR = secessionThreshold > 0 ? P(n) * P(pressure) / secessionThreshold : 0.0;
        double fU = P(n) * P(tau) * P(ruleInit3);
        return new RpForces(fS, fI, fR, fU);
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

    /// <summary>(1214) 州 r の分離の圧（エンジンの CheckSecession と同じ関数）。関税の姿勢は w.Tariff×2−1・州の関税の利害 r.TariffInterest は書かずに読むだけ（前の四半期のエンジンの値）。</summary>
    internal static double RpPressure(World w, Region r) => r.Id == 0 ? 0.0 : Politics.SecessionPressure(w, w.Coef, r, w.Regions[0], w.Tariff * 2 - 1, r.TariffInterest);

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
        public RpHead? Head;   // (1147) 四半期の頭の取り分の写し（rponset_s が選ばれた時だけ）
        public double Get(int a, int b, double[]? landRatioOverride = null)
        {
            if (landRatioOverride is not null) return PairKmCoreOf(a, b, landRatioOverride);
            Km ??= NewKm(Tc); int ix = a * Tc + b; double v = Km[ix];
            if (double.IsNaN(v)) { v = PairKmCoreOf(a, b, LandRatio); Km[ix] = v; }
            return v;
        }
        static double[] NewKm(int tc) { var k = new double[tc * tc]; System.Array.Fill(k, double.NaN); return k; }
        double PairKmCoreOf(int a, int b, double[] lr) => InternationalMarket.PairKmCore(Tr, SeaNodeOf, SeaHalf, SuezOkN, PanOkN, TerrCost, lr, LandNode, LandGraft, LandRoute, a, b, out _);
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
        if (!(Wants("rulepower_s") || Wants("rulepeace_s") || Wants("rulepoweralloc_s") || Wants("rponset_s"))) return;
        if (nations.Count == 0 || nations[0].World.Diplomacy is not { } dip) return;
        RpSnapCount++;
        var s = new RpSnap { Tc = tc, LandRoute = landRoute, Tr = new TrEntry[tc], SeaNodeOf = new int[tc], SeaHalf = new double[tc], TerrCost = new double[tc], LandRatio = new double[tc], LandNode = new int[tc], LandGraft = new double[tc], SuezOkN = (bool[])suezOkN.Clone(), PanOkN = (bool[])panOkN.Clone() };
        System.Array.Copy(tr, s.Tr, tc); System.Array.Copy(seaNodeOf, s.SeaNodeOf, tc); System.Array.Copy(seaHalf, s.SeaHalf, tc); System.Array.Copy(terrCost, s.TerrCost, tc);
        System.Array.Copy(landRatio, s.LandRatio, tc); System.Array.Copy(landNode, s.LandNode, tc); System.Array.Copy(landGraft, s.LandGraft, tc);
        for (int i = 0; i < tc; i++) s.Idx[tr[i].r] = i;
        s.TrNf = new TrEntry[tc];
        for (int i = 0; i < tc; i++) { var e = tr[i]; if (!e.seaOk && e.r.LastPortFrozen) e.seaOk = true; s.TrNf[i] = e; }   // LastPortFrozen〔履歴 InstrumentsRulePower.cs#5〕
        if (Wants("rponset_s")) s.Head = RpHeadCopy(nations);   // (1147) 頭の s は寄せ・付与の前（この口は四半期の最初の清算）
        RpSnaps.Remove(dip); RpSnaps.Add(dip, s);
    }

    sealed class RpState
    {
        public bool Init; public double Q;
        public readonly Dictionary<Region, double> Tau = new(); public readonly HashSet<Region> Seen = new(); public readonly Dictionary<Simulation, double> Inter0 = new();
        public readonly HashSet<EndedWarRecord> PeaceSeen = new(); public bool PeaceInit;
        public bool HeaderPower, HeaderPeace, HeaderAlloc, HeaderOnset;
        public readonly HashSet<Region> DataRegions = new(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);   // (1147) 最初の表で名の無い取り分を持っていた州
        public readonly Dictionary<Region, double> TauNf = new();   // TauNf〔履歴 InstrumentsRulePower.cs#6〕
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
        public Region R = null!; public double PopK, N, Tau, Pressure, Rho, RhoGc, Y, Kappa, I, OwnerWeight;
        public int RhoSource; public RpForces F; public double HShare;
        public double RhoNf, TauNf, HShareNf;   // HShareNf〔履歴 InstrumentsRulePower.cs#7〕
        public double RhoRailGate, RhoNoRail, RhoFullRail, DirectKm; public bool DirectLand;
        public double ForeignRaw, ForeignSum, ForeignTop; public string TopKey = ""; public double NavalAlloc;
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
        bool wantP = Wants("rulepower_s"), wantW = Wants("rulepeace_s"), wantA = Wants("rulepoweralloc_s") && q % 40 == 0, wantO = Wants("rponset_s");
        if (!(wantP || wantW || wantA || wantO) || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        RpTableCount++;
        var dip = sim.Diplomacy;
        var st = RpStates.GetValue(dip, _ => new RpState());
        RpSnaps.TryGetValue(dip, out var snap);
        bool firstTable = !st.Init;   // (1147) 最初の表の印は既にある RpState.Init（新しい印を足さない）
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
        if (wantO) RponsetRows(sim, seed, q, firstTable);
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
                double press = RpPressure(nn.W, r);   // (1214) エンジンの分離の圧と同じ関数（Politics.SecessionPressure）。州の関税の利害は前の四半期のエンジンの値を読むだけ
                var rr = new RpReg { R = r, PopK = popK, N = popK * st.Q, Pressure = press, Y = RpOutputValue(r) };
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
                double frProbe = rr.N * rr.Pressure / c.SecessionThreshold;   // 圧はエンジンと同じ関数（(1214)）
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
                    rr.RhoRailGate = RulePowerReach(km); rr.RhoNoRail = RulePowerReach(snap.Get(capIx, di, noRail)); rr.RhoFullRail = RulePowerReach(snap.Get(capIx, di, full));
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
                rr.F = RulePowerForces(rr.N, rr.Tau, rr.HShare, r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, nn.InterRatio, r.RuleInit.Length > 3 ? r.RuleInit[3] : 0, rr.Pressure, c.SecessionThreshold);
            }
        }

        // ---- 外国の力：O を候補の州へ配る（設計 §3-3） ----
        var regOwner = new Dictionary<Region, (RpNation n, RpReg rr)>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var nn in nats) foreach (var rr in nn.Rows) regOwner[rr.R] = (nn, rr);
        var foreignAt = new Dictionary<Region, List<(string key, double f, double raw)>>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var nn in nats)
        {
            if (!(nn.O > 0) && !(nn.Navy > 0)) continue;
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
                allocSb?.Append(string.Join('\t', I(q), I(nn.W.Year), nn.Key, cd.r.Name, I(cd.kind), RpR(cd.rr.Y), RpR(cd.rho), RpR(cd.s), RpR(Math.Min(1.0, Math.Max(0.0, cd.rr.OwnerWeight))), RpR(cd.wgt), RpR(alloc), RpR(alloc > 0 ? alloc * cd.rho : 0))).Append('\n');
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
                    bool qual = first && ri0 > 0 && rr.HShare > 0 && f.FR == 0 && ri2 == 0 && rr.ForeignSum == 0 && r.ForeignShares.Length == 0;
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
                        F(f.FS), F(f.FI), F(f.FR), F(f.FU), F(rr.ForeignRaw), F(rr.ForeignSum), F(rr.ForeignTop), rr.TopKey, F(rr.Pressure), F(nn.InterRatio),
                        F(iShare), F(rr.Kappa), F(cls.Owner), F(cls.Local), F(cls.Foreign), F(cls.Ungov), F(cls.Owner + cls.Local + cls.Foreign + cls.Ungov), cn, RpClassNames[nb], F(nr[0]), F(nr[1]), F(nr[2]), F(nr[3]),
                        F(cls.Total > 0 ? f.FS / cls.Total : double.NaN), F(cls.Total > 0 ? (f.FI + f.FR) / cls.Total : double.NaN),
                        F(rr.Rho), I(rr.RhoSource), F(rr.RhoGc), F(rr.RhoRailGate), F(rr.RhoNoRail), F(rr.RhoFullRail), F(rr.DirectKm), rr.DirectLand ? "1" : "0", F(nn.W.RailCapacity), qual ? "1" : "0", F(openDiff), F(phi),
                        F(rr.RhoNf), F(rr.TauNf), F(ri0), F(ri1), F(ri2), F(ri3), RpR(cls.Owner), RpR(cls.Ungov), F(rr.I), F(rr.OwnerWeight))).Append('\n');
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
                        var f = RulePowerForces(rr.N, rr.Tau, hs, r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, ln.InterRatio, r.RuleInit.Length > 3 ? r.RuleInit[3] : 0, rr.Pressure, c.SecessionThreshold);
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
    }
}
