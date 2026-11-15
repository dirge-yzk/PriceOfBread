using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);

namespace PriceOfBread.SimCore;

/// <summary>(1117) エンジンの距離の表：(1165) M1＝四半期の末の清算の州の表の写し（凍っていた港だけ海へ戻す＝凍結を外す）。同じ StepDay の中で作って使い切る＝セーブに載らない。</summary>
internal sealed class RulePowerSnap
{
    public TrEntry[] Tr = []; public int Tc; public int[] SeaNodeOf = [], LandNode = []; public double[] SeaHalf = [], TerrCost = [], LandRatio = [], LandGraft = [];
    public bool[] SuezOkN = [], PanOkN = []; public double LandRoute;
    public readonly Dictionary<Region, int> Idx = new(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
    double[]? _km; public long Evals;
    public double KmOf(int a, int b)
    {
        if (_km is null) { _km = new double[Tc * Tc]; System.Array.Fill(_km, double.NaN); }
        int ix = a * Tc + b; double v = _km[ix];
        if (double.IsNaN(v)) { v = InternationalMarket.PairKmCore(Tr, SeaNodeOf, SeaHalf, SuezOkN, PanOkN, TerrCost, LandRatio, LandNode, LandGraft, LandRoute, a, b, out _); _km[ix] = v; }
        Evals++;
        return v;
    }
}

/// <summary>(1117) 1 州のエンジンの値（その四半期の表・セーブに載らない）。Stamp が Diplomacy.RpEngStamp と同じときだけ有効。</summary>
internal sealed class RulePowerRow
{
    public long Stamp; public double Q, Rho, Tau, I, H, O, FSum, T0 = double.NaN, T1 = double.NaN, T3 = double.NaN; public int Best = -1;
    public readonly List<string> FKeys = []; public readonly List<double> FT = [];
}

public sealed partial class MultiNationSim
{
    /// <summary>(1165) M1：四半期の末の清算（全ての国の Close の後）の直後に 1 回（StepDay から）。表を作り、同じ呼び出しで全ての国を寄せる＝日をまたいで残る物が無い。</summary>
    void RulePowerQuarterEnd()
    {
        var snap = Diplomacy.RpEngSnap; Diplomacy.RpEngSnap = null;   // 清算の口が同じ StepDay の中で置いた写しを受け取って消す
        bool diag = Instruments.On;
        if (diag) Instruments.RpDesign(this);   // 計器の側の〈D〉（エンジンの関数を呼ばない・同じ盤面）
        Diplomacy.RpEngOn = _coef.RuleFromPower > 0 ? 2 : 1;   // (1151) F16
        if (!(_coef.RuleFromPower > 0)) return;
        RulePowerBuild(diag, snap);
        foreach (var sim in Nations) Politics.RulePowerApply(sim.World, sim.World.Coef);   // (1165) M1：Apply は共有層（F1 の早期 return の前の寄せは要らない）
    }

    /// <summary>清算の口（BilateralMarket の 1 行）：計器の複写を同じ引数で呼び、旗 RuleFromPower のときだけエンジンの表を写す（計器の門 On はここで読まない）。</summary>
    internal static void RulePowerCapture(bool firstClearingOfQuarter, IReadOnlyList<Simulation> nations, Coefficients c, TrEntry[] tr, int tc, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, double[] terrCost, double[] landRatio, int[] landNode, double[] landGraft, double landRoute)
    {
        Instruments.RpSnapshot(firstClearingOfQuarter, nations, tr, tc, seaNodeOf, seaHalf, suezOkN, panOkN, terrCost, landRatio, landNode, landGraft, landRoute);
        if (firstClearingOfQuarter || !(c.RuleFromPower > 0) || nations.Count == 0 || nations[0].World.Diplomacy is not { } dip) return;   // (1165) M1：第 2 月末と四半期の末の清算で写す（四半期の末の写しを同じ StepDay の RulePowerQuarterEnd が読む）
        var s = new RulePowerSnap { Tc = tc, LandRoute = landRoute, Tr = new TrEntry[tc], SeaNodeOf = seaNodeOf[..tc], SeaHalf = seaHalf[..tc], TerrCost = terrCost[..tc], LandRatio = landRatio[..tc], LandNode = landNode[..tc], LandGraft = landGraft[..tc], SuezOkN = (bool[])suezOkN.Clone(), PanOkN = (bool[])panOkN.Clone() };
        for (int i = 0; i < tc; i++)
        {
            var e = tr[i];
            if (!e.seaOk && e.r.LastPortFrozen) e.seaOk = true;   // 凍結を外す（凍っていた港だけ）
            s.Tr[i] = e; s.Idx[e.r] = i;
        }
        dip.RpEngSnap = s;
    }

    static double RpEngReach(double km) => double.IsFinite(km) && km >= 0 ? 1.0 / (1.0 + km / Diplomacy.DistanceNormKm) : 0.0;

    static double RpEngGreatCircle(World w, Region r)
    {
        var rc = Places.Coord(r.Name);
        return (rc.Lat == 0 && rc.Lon == 0) ? 0.0 : RpEngReach(Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon));
    }

    /// <summary>F_R の圧力＝Politics.CheckSecession の分離の圧力と同じ式（首都 Id 0 は 0）。</summary>
    internal static double RulePowerPressure(World w, Coefficients c, Region r, Region cap)
    {
        if (r.Id == 0) return 0.0;
        double stance = w.Tariff * 2 - 1;
        double cultDist = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                        + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
        if (c.CultureDistanceFromEthnic > 0) cultDist = PoliticalQuantities.Toward(cultDist, 1.0 - Politics.AvgRegionCoreShare(r), c.CultureDistanceFromEthnic);
        double mil = Politics.AvgRegionMilitancy(r);
        double pressure = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cultDist);
        if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled) pressure += c.SecessionCultureAdditive * c.MinorityMilitancy * cultDist * (1.0 - Politics.AvgRegionCoreShare(r)) * mil;
        if (c.SecessionConsciousnessWeight > 0) pressure *= Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * Politics.AvgRegionConsciousness(r));
        pressure *= 1.0 - PoliticalQuantities.Clamp01(Politics.RegionAvgSuffrageInclusion(w, r));
        double crowd = Politics.RegionCrowd(r);
        pressure *= crowd > 0 ? crowd / (crowd + c.ColonyPromotionMinPop) : 0.0;
        return pressure;
    }

    sealed class RpEngNation
    {
        public World W = null!; public Power P = null!; public string Key = ""; public Region[] R = [];
        public double L, Front, H, O, Pop; public double[] PopK = [], N = [], Press = [], Consc = [], Xf = [], Core = [], Kappa = [], I = [], Ow = [], Rho = [], Y = [];
        public List<double>? Solved; public List<int>? Unsolved;   // (1151) F9 τ の段 ① の結果
    }

    /// <summary>
    /// 段 C′ の表（設計 §3-1〜3-5・§3-9＋推奨 Vb・q_t・I-a）：q_t・W・H・O・ρ・τ・力・外への配り・目標と最大の区分を、各州の RpEng に置く。
    /// 各国の CheckSecession は自国の州の RpEng を読むだけ（国の並び順に依らない）。
    /// </summary>
    void RulePowerBuild(bool diag, RulePowerSnap? snap)
    {
        if (Nations.Count == 0 || Nations[0].World.WarmupFrozen) return;
        var dip = Diplomacy; long stamp = ++dip.RpEngStamp;
        long evals0 = snap?.Evals ?? 0;
        if (diag) (dip.RpEngAlloc ??= new()).Clear(); else dip.RpEngAlloc = null;
        var list = new List<RpEngNation>(); double sumL = 0, sumPop = 0;
        foreach (var sim in Nations)
        {
            var w = sim.World; if (w.SelfPower is not { } p || w.Regions.Count == 0) continue;
            var n = new RpEngNation { W = w, P = p, Key = p.BaseName.Length > 0 ? p.BaseName : p.Name, R = [.. w.Regions], L = Math.Max(0, p.LandPower) };
            int m = n.R.Length;
            n.PopK = new double[m]; n.N = new double[m]; n.Press = new double[m]; n.Consc = new double[m]; n.Xf = new double[m]; n.Core = new double[m];
            n.Kappa = new double[m]; n.I = new double[m]; n.Ow = new double[m]; n.Rho = new double[m]; n.Y = new double[m];
            for (int k = 0; k < m; k++) { n.PopK[k] = Math.Max(0, n.R[k].SumPopulation()); n.Pop += n.PopK[k]; }
            list.Add(n); sumL += n.L; sumPop += n.Pop;
        }
        if (!(sumL > 0 && sumPop > 0)) return;
        double q = sumL / sumPop;   // q_t（四半期ごとに導く・状態にしない）
        // (1151) F10：州を最初に見た表（RuleTau が NaN＝既にある印）で、名の無い開幕の外国の取り分（データ）に持ち主の名を写す
        foreach (var n in list) foreach (var r in n.R) if (double.IsNaN(r.RuleTau)) RulePowerSeedOpening(r, dip);

        // 国：前線 W・国内 H・外 O
        foreach (var n in list)
        {
            var c = n.W.Coef;
            int fronts = 0; foreach (var war in dip.Wars) if (ReferenceEquals(war.A, n.P) || ReferenceEquals(war.B, n.P)) fronts++;
            if (fronts < 1) fronts = 1;
            foreach (var war in dip.Wars)
            {
                Power y; if (ReferenceEquals(war.A, n.P)) y = war.B; else if (ReferenceEquals(war.B, n.P)) y = war.A; else continue;
                int yFronts = 0; foreach (var w2 in dip.Wars) if (ReferenceEquals(w2.A, y) || ReferenceEquals(w2.B, y)) yFronts++;
                double self = dip.FrontPush(n.P, y, c, fronts), other = dip.FrontPush(y, n.P, c, Math.Max(1, yFronts));
                double bind = self > 0 ? Math.Min(1.0, other / self) : (other > 0 ? 1.0 : 0.0);
                n.Front += n.L / fronts * bind;
            }
            (n.H, n.O) = RulePowerSplitArmy(n.L, Math.Min(n.Front, n.L), n.Pop * q);
        }

        // 州（持ち主）：i・持ち主の重み・ρ・τ
        foreach (var n in list)
        {
            var c = n.W.Coef; var cap = n.R[0]; double inter = Privileges.IntermediaryDegree(n.W);
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k];
                n.N[k] = n.PopK[k] * q;
                n.Press[k] = RulePowerPressure(n.W, c, r, cap);
                n.Consc[k] = Politics.AvgRegionConsciousness(r); n.Core[k] = Politics.AvgRegionCoreShare(r); n.Y[k] = r.OutputRefValue();
                double xf = 0;
                for (int j = 0; j < r.ForeignShares.Length && j < r.ForeignRulers.Length; j++)
                {
                    if (!(r.ForeignShares[j] > 0)) continue;
                    var fp = dip.Powers.Find(pp => (pp.BaseName.Length > 0 ? pp.BaseName : pp.Name) == r.ForeignRulers[j]);
                    if (fp is null || fp.Culture != n.P.Culture) xf += r.ForeignShares[j];
                }
                n.Xf[k] = xf;
                double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0, ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0;
                double s0 = r.Rule.Length > 0 ? r.Rule[0] : 0, s1 = r.Rule.Length > 1 ? r.Rule[1] : 0;
                n.Kappa[k] = ri0 > 0 ? Math.Min(1.0, s0 / ri0) : 1.0;
                double core = n.Core[k] > 1 ? 1 : n.Core[k] > 0 ? n.Core[k] : 0;
                double push = n.N[k] * Math.Max(0, n.Consc[k]) * Math.Max(0, xf);
                double fR = n.N[k] * Math.Max(0, n.Press[k]) / c.SecessionThreshold + push * (1 - core);
                double fI = double.IsNaN(r.RuleTau)
                    ? n.N[k] * Math.Max(0, ri1)   // I-a：τ を解く州は τ＝1 の試し
                    : n.N[k] * r.RuleTau * Math.Max(0, ri1) * (r.RuleTauInter > 0 ? inter / r.RuleTauInter : 1.0);
                n.I[k] = fI + fR > 0 ? fI / (fI + fR) : 1.0;
                n.Ow[k] = s0 + s1 * n.I[k] * n.Kappa[k];
            }
            int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            // (1151) F15：首都→起点の届きを国ごとに 1 回だけ引く・上界〈首都→起点の届き×起点の重み〉が今の ρ 以下の起点は起点→州を引かない（届き≦1 で値は同じ）
            var capReach = new double[n.R.Length]; var nodeOf = new int[n.R.Length];
            for (int s = 0; s < n.R.Length; s++)
            {
                nodeOf[s] = snap is not null && snap.Idx.TryGetValue(n.R[s], out var si0) ? si0 : -1;
                capReach[s] = nodeOf[s] >= 0 && capIx >= 0 && nodeOf[s] != capIx ? RpEngReach(snap!.KmOf(capIx, nodeOf[s])) : 0.0;
            }
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k]; double rho; int ri = nodeOf[k];
                if (ReferenceEquals(r, cap)) rho = 1.0;
                else if (snap is not null && capIx >= 0 && ri >= 0)
                {
                    rho = ri == capIx ? RpEngReach(snap.KmOf(capIx, ri)) : capReach[k];
                    for (int s = 0; s < n.R.Length; s++)
                    {
                        int si = nodeOf[s]; if (si < 0 || si == ri || si == capIx) continue;
                        double up = capReach[s] * n.Ow[s]; if (up <= rho) continue;
                        double via = up * RpEngReach(snap.KmOf(si, ri)); if (via > rho) rho = via;
                    }
                }
                else rho = RpEngGreatCircle(n.W, r);
                n.Rho[k] = rho >= 0 ? rho : 0;
            }
            // τ_r の段 ①：州を最初に見た表で 1 回だけ解く（州について行く）。解けなかった州は全ての国の段 ① の後（下）。
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k]; if (!double.IsNaN(r.RuleTau)) continue;
                double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0;
                double hs = n.Pop > 0 ? n.H * n.PopK[k] / n.Pop * n.Rho[k] : 0;
                if (ri0 > 0 && hs > 0 && n.N[k] > 0) { r.RuleTau = hs / (n.N[k] * ri0); r.RuleTauInter = inter; (n.Solved ??= []).Add(r.RuleTau); }
                else (n.Unsolved ??= []).Add(k);
            }
        }
        // (1151) F9 τ の段 ②：解けなかった州に〈自国の段 ① で解けた州の中央〉、無ければ〈全ての国の段 ① を終えた時点の全ての州の中央〉＝国の並び順に依らない
        double worldMed = double.NaN;
        foreach (var n in list)
        {
            if (n.Unsolved is null) continue;
            double med;
            if (n.Solved is { Count: > 0 } sv) { sv.Sort(); med = sv[sv.Count / 2]; }
            else
            {
                if (double.IsNaN(worldMed))
                {
                    var all = new List<double>();
                    foreach (var m in list) foreach (var rg in m.R) if (!double.IsNaN(rg.RuleTau) && !(m.Unsolved is { } mu && mu.Contains(System.Array.IndexOf(m.R, rg)))) all.Add(rg.RuleTau);
                    all.Sort(); worldMed = all.Count > 0 ? all[all.Count / 2] : 0;
                }
                med = worldMed;
            }
            double inter = Privileges.IntermediaryDegree(n.W);
            foreach (var k in n.Unsolved) { n.R[k].RuleTau = med; n.R[k].RuleTauInter = inter; }
        }

        // 外への配り（O＞0 の国）：重み w＝Y×(1−s)×(1−b)（Vb）・F_f＝配り×ρ_f
        var where = new Dictionary<Region, (RpEngNation n, int k)>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var n in list) for (int k = 0; k < n.R.Length; k++) where[n.R[k]] = (n, k);
        var foreign = new Dictionary<Region, List<(string Key, double F)>>(ReferenceEqualityComparer.Instance as IEqualityComparer<Region>);
        foreach (var n in list)
        {
            if (!(n.O > 0)) continue;
            var cap = n.R[0]; int capIx = snap is not null && snap.Idx.TryGetValue(cap, out var ci) ? ci : -1;
            var winNodes = Diplomacy.CessionWinnerNodes(n.P);
            var origins = new List<(int ix, double w)>();
            if (snap is not null)
            {
                for (int k = 0; k < n.R.Length; k++) if (snap.Idx.TryGetValue(n.R[k], out var si)) origins.Add((si, n.Ow[k]));
                foreach (var kv in where) { var rg = kv.Key; for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == n.Key && rg.ForeignShares[j] > 0 && snap.Idx.TryGetValue(rg, out var si)) origins.Add((si, rg.ForeignShares[j])); }
            }
            var originUp = new double[origins.Count];   // (1151) F15：首都→起点の届き×起点の重み（起点ごとに 1 回）
            for (int i = 0; i < origins.Count; i++) originUp[i] = origins[i].ix == capIx || capIx < 0 ? 0.0 : RpEngReach(snap!.KmOf(capIx, origins[i].ix)) * origins[i].w;
            var cands = new List<Region>(); var cv = new List<(double Y, double S, double B, double Rho)>();
            foreach (var kv in where)
            {
                var (on, ok) = kv.Value; if (ReferenceEquals(on, n)) continue;
                var rg = kv.Key; double held = 0;
                for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == n.Key && rg.ForeignShares[j] > 0) held += rg.ForeignShares[j];
                // (1151) 候補：③自国の州と陸の隣（F8）・①取り分の州（held＞0）。②交戦の相手は段 F（F6）。①だけの州の重みに s（F2）。
                var rc = Places.Coord(rg.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon);
                bool adj = nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _);
                if (!adj && !(held > 0)) continue;
                double rho;
                if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(rg, out var ri))
                {
                    rho = RpEngReach(snap.KmOf(capIx, ri));
                    for (int i = 0; i < origins.Count; i++)
                    {
                        int si = origins[i].ix; if (si == ri || si == capIx) continue;
                        double up = originUp[i]; if (up <= rho) continue;
                        double via = up * RpEngReach(snap.KmOf(si, ri)); if (via > rho) rho = via;
                    }
                }
                else rho = RpEngGreatCircle(n.W, rg);
                cands.Add(rg); cv.Add((RulePowerCandY(on.Y[ok], held, adj), held, on.Ow[ok], rho));   // Vb：重みに ρ を入れない（ρ は力で 1 回だけ＝RulePowerAllocate）
            }
            var allocs = new double[cands.Count]; var forces = new double[cands.Count];
            RulePowerAllocate(n.O, cv, allocs, forces);
            for (int i = 0; i < cands.Count; i++)
            {
                var rg = cands[i]; double alloc = allocs[i];
                if (dip.RpEngAlloc is { } da) da[(n.Key, rg)] = (RulePowerWeight(cv[i].Y, cv[i].S, cv[i].B), alloc, forces[i], cv[i].Y, cv[i].S, cv[i].B);   // (1165) Y は F2 の Y′（①だけは Y×s）
                if (!(alloc > 0)) continue;
                if (!foreign.TryGetValue(rg, out var lst)) foreign[rg] = lst = [];
                lst.Add((n.Key, forces[i]));
            }
        }

        // 力・目標・最大の区分
        foreach (var n in list)
        {
            var c = n.W.Coef; double inter = Privileges.IntermediaryDegree(n.W);
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k];
                double ri1 = r.RuleInit.Length > 1 ? r.RuleInit[1] : 0, ri3 = r.RuleInit.Length > 3 ? r.RuleInit[3] : 0;
                double core = n.Core[k] > 1 ? 1 : n.Core[k] > 0 ? n.Core[k] : 0;
                double push = n.N[k] * Math.Max(0, n.Consc[k]) * Math.Max(0, n.Xf[k]);
                double fS = (n.Pop > 0 ? n.H * n.PopK[k] / n.Pop * n.Rho[k] : 0) + push * core;
                double fI = n.N[k] * Math.Max(0, r.RuleTau) * Math.Max(0, ri1) * (r.RuleTauInter > 0 ? inter / r.RuleTauInter : 1.0);
                double fR = n.N[k] * Math.Max(0, n.Press[k]) / c.SecessionThreshold + push * (1 - core);
                double fU = n.N[k] * Math.Max(0, r.RuleTau) * Math.Max(0, ri3);
                double fF = 0; foreign.TryGetValue(r, out var fl);
                if (fl is not null) foreach (var (_, f) in fl) fF += f;
                var row = r.RpEng ??= new RulePowerRow();
                row.Stamp = stamp; row.Q = q; row.Rho = n.Rho[k]; row.Tau = r.RuleTau; row.I = n.I[k]; row.H = n.H; row.O = n.O; row.FSum = fF;
                row.FKeys.Clear(); row.FT.Clear(); row.T0 = row.T1 = row.T3 = double.NaN; row.Best = -1;
                double tot = fS + fI + fR + fF + fU;
                if (!(tot > 0)) continue;   // 和が 0 以下なら寄せない
                row.T0 = fS / tot; row.T1 = (fI + fR) / tot; row.T3 = fU / tot;
                if (fl is not null) foreach (var (key, f) in fl) { row.FKeys.Add(key); row.FT.Add(f / tot); }
                row.Best = RulePowerBestClass((fS + n.Kappa[k] * fI) / tot, ((1 - n.Kappa[k]) * fI + fR) / tot, fF / tot, fU / tot);
            }
        }
        if (snap is not null) Cost[CostLedger.RulePowerPairs] += snap.Evals - evals0;
    }

    /// <summary>
    /// (1123) 陸軍力 L を前線 W・国内 H・外 O に割る（同じ兵は 1 回だけ）。足りる時は前線と国内を満たし残りが外。
    /// 足りない時は外を 0 にし、前線と国内で要る量（front・need）に比例して分ける＝戦時でも国内の治安の分が残る（前線に全て食わせない）。
    /// </summary>
    internal static (double H, double O) RulePowerSplitArmy(double l, double front, double need)
    {
        if (l >= front + need) return (need, l - front - need);
        if (front > 0) return (l * need / (front + need), 0.0);
        return (l, 0.0);
    }

    /// <summary>(1123) 外への配り：O を重み w＝Y×(1−min(1,s))×(1−b)（Vb＝ρ を入れない）に比例して配り、力は配り×ρ（ρ は力で 1 回だけ）。</summary>
    internal static void RulePowerAllocate(double o, IReadOnlyList<(double Y, double S, double B, double Rho)> cand, double[] alloc, double[] force)
    {
        double sumW = 0;
        for (int i = 0; i < cand.Count; i++) sumW += RulePowerWeight(cand[i].Y, cand[i].S, cand[i].B);
        for (int i = 0; i < cand.Count; i++)
        {
            double a = sumW > 0 ? o * RulePowerWeight(cand[i].Y, cand[i].S, cand[i].B) / sumW : 0;
            alloc[i] = a; force[i] = a > 0 ? a * cand[i].Rho : 0;
        }
    }

    /// <summary>(1151) F2：①だけの候補（陸の隣③でない取り分の州）は重みの Y に取り分 s を掛ける（w＝Y×s×(1−s)×(1−b)）・③は Vb のまま。</summary>
    internal static double RulePowerCandY(double y, double s, bool adjacent) => adjacent ? y : y * s;

    /// <summary>(1151) F10：名の無い開幕の外国の取り分（world.json の州の rule[2]＞0・名の付いた取り分の和が 0）を、データの持ち主の名（foreignRuler）へ写す。
    /// 名が無いか、その名の国が盤面に無ければ在地へ移す（Rule と RuleInit の [2]→[1]）。呼ぶのは州を最初に見た表（RuleTau が NaN）だけ。</summary>
    internal static void RulePowerSeedOpening(Region r, Diplomacy dip)
    {
        if (r.Rule.Length != 4 || !(r.Rule[2] > 0)) return;
        for (int j = 0; j < r.ForeignShares.Length; j++) if (r.ForeignShares[j] > 0) return;
        string name = OpeningForeignRuler(r.Name);
        bool known = name.Length > 0 && dip.Powers.Exists(p => (p.BaseName.Length > 0 ? p.BaseName : p.Name) == name);
        if (known)
        {
            int k = System.Array.IndexOf(r.ForeignRulers, name);
            if (k >= 0 && k < r.ForeignShares.Length) { r.ForeignShares[k] = r.Rule[2]; return; }
            var names = new string[r.ForeignRulers.Length + 1]; var xs = new double[r.ForeignRulers.Length + 1];
            System.Array.Copy(r.ForeignRulers, names, r.ForeignRulers.Length); System.Array.Copy(r.ForeignShares, xs, Math.Min(r.ForeignShares.Length, r.ForeignRulers.Length));
            names[^1] = name; xs[^1] = r.Rule[2]; r.ForeignRulers = names; r.ForeignShares = xs;
            return;
        }
        r.Rule[1] += r.Rule[2]; r.Rule[2] = 0;
        if (r.RuleInit.Length == 4) { r.RuleInit[1] += r.RuleInit[2]; r.RuleInit[2] = 0; }
    }

    /// <summary>world.json の州の foreignRuler（開幕の外国の取り分の持ち主の名・データ）。</summary>
    internal static string OpeningForeignRuler(string city) => RpOpeningData.Map.TryGetValue(city, out var v) ? v : "";

    static class RpOpeningData
    {
        internal static readonly Dictionary<string, string> Map = Build();
        static Dictionary<string, string> Build()
        {
            var m = new Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var nc in WorldData.Nations()) foreach (var p in nc.Provinces) if (p.ForeignRuler.Length > 0) m[p.City] = p.ForeignRuler;
            return m;
        }
    }

    static double RulePowerWeight(double y, double s, double b)
    {
        double w = y * (1 - Math.Min(1.0, s)) * (1 - Math.Min(1.0, Math.Max(0.0, b)));
        return w > 0 ? w : 0;
    }

    /// <summary>最大の区分（0 持ち主の側・1 自立・2 外国・3 無統治）。同点の許容差は相対 1e-9（1+1e-9 倍を超えたときだけ大きいと読む・同点は若い番号）。</summary>
    internal static int RulePowerBestClass(double owner, double local, double foreign, double ungov)
    {
        int best = 0; double bv = owner;
        if (local > bv * (1 + RulePowerTieTolerance)) { best = 1; bv = local; }
        if (foreign > bv * (1 + RulePowerTieTolerance)) { best = 2; bv = foreign; }
        if (ungov > bv * (1 + RulePowerTieTolerance)) best = 3;
        return best;
    }
    internal const double RulePowerTieTolerance = 1e-9;
}
