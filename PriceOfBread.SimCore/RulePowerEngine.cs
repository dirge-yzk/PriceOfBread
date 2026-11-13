using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);

namespace PriceOfBread.SimCore;

/// <summary>(1117) エンジンの距離の表：四半期の最初の清算の州の表の写し（凍っていた港だけ海へ戻す＝凍結を外す）。セーブに載らない。</summary>
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
    /// <summary>四半期の最終日・全ての国の StepDayClose の前に 1 回（StepDay から）。</summary>
    void RulePowerQuarterEnd()
    {
        bool diag = Instruments.On;
        if (diag) Instruments.RpDesign(this);   // 計器の側の〈D〉（エンジンの関数を呼ばない）
        if (_coef.RuleFromPower > 0) RulePowerBuild(diag);
    }

    /// <summary>清算の口（BilateralMarket の 1 行）：計器の複写を同じ引数で呼び、旗 RuleFromPower のときだけエンジンの表を写す（計器の門 On はここで読まない）。</summary>
    internal static void RulePowerCapture(bool firstClearingOfQuarter, IReadOnlyList<Simulation> nations, Coefficients c, TrEntry[] tr, int tc, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, double[] terrCost, double[] landRatio, int[] landNode, double[] landGraft, double landRoute)
    {
        Instruments.RpSnapshot(firstClearingOfQuarter, nations, tr, tc, seaNodeOf, seaHalf, suezOkN, panOkN, terrCost, landRatio, landNode, landGraft, landRoute);
        if (!firstClearingOfQuarter || !(c.RuleFromPower > 0) || nations.Count == 0 || nations[0].World.Diplomacy is not { } dip) return;
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
    }

    /// <summary>
    /// 段 C′ の表（設計 §3-1〜3-5・§3-9＋推奨 Vb・q_t・I-a）：q_t・W・H・O・ρ・τ・力・外への配り・目標と最大の区分を、各州の RpEng に置く。
    /// 各国の CheckSecession は自国の州の RpEng を読むだけ（国の並び順に依らない）。
    /// </summary>
    void RulePowerBuild(bool diag)
    {
        if (Nations.Count == 0 || Nations[0].World.WarmupFrozen) return;
        var dip = Diplomacy; var snap = dip.RpEngSnap; long stamp = ++dip.RpEngStamp;
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
            double front = Math.Min(n.Front, n.L);
            n.H = Math.Min(Math.Max(0, n.L - front), n.Pop * q);
            n.O = Math.Max(0, n.L - front - n.H);
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
                n.Consc[k] = Politics.AvgRegionConsciousness(r); n.Core[k] = Politics.AvgRegionCoreShare(r); n.Y[k] = Diplomacy.RegionOutputValue(r);
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
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k]; double rho;
                if (ReferenceEquals(r, cap)) rho = 1.0;
                else if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(r, out var ri))
                {
                    rho = RpEngReach(snap.KmOf(capIx, ri));
                    for (int s = 0; s < n.R.Length; s++)
                    {
                        if (!snap.Idx.TryGetValue(n.R[s], out var si) || si == ri || si == capIx) continue;
                        double via = RpEngReach(snap.KmOf(capIx, si)) * n.Ow[s] * RpEngReach(snap.KmOf(si, ri)); if (via > rho) rho = via;
                    }
                }
                else rho = RpEngGreatCircle(n.W, r);
                n.Rho[k] = rho >= 0 ? rho : 0;
            }
            // τ_r：州を最初に見た表で 1 回だけ解く（州について行く）
            var solved = new List<double>(); var unsolved = new List<int>();
            for (int k = 0; k < n.R.Length; k++)
            {
                var r = n.R[k]; if (!double.IsNaN(r.RuleTau)) continue;
                double ri0 = r.RuleInit.Length > 0 ? r.RuleInit[0] : 0;
                double hs = n.Pop > 0 ? n.H * n.PopK[k] / n.Pop * n.Rho[k] : 0;
                if (ri0 > 0 && hs > 0 && n.N[k] > 0) { r.RuleTau = hs / (n.N[k] * ri0); r.RuleTauInter = inter; solved.Add(r.RuleTau); }
                else unsolved.Add(k);
            }
            if (unsolved.Count > 0)
            {
                double med;
                if (solved.Count > 0) { solved.Sort(); med = solved[solved.Count / 2]; }
                else
                {
                    var all = new List<double>();
                    foreach (var sim in Nations) foreach (var rg in sim.World.Regions) if (!double.IsNaN(rg.RuleTau)) all.Add(rg.RuleTau);
                    all.Sort(); med = all.Count > 0 ? all[all.Count / 2] : 0;
                }
                foreach (var k in unsolved) { n.R[k].RuleTau = med; n.R[k].RuleTauInter = inter; }
            }
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
            var cands = new List<(Region r, double w, double rho)>(); double sumW = 0;
            foreach (var kv in where)
            {
                var (on, ok) = kv.Value; if (ReferenceEquals(on, n)) continue;
                var rg = kv.Key; double held = 0;
                for (int j = 0; j < rg.ForeignRulers.Length && j < rg.ForeignShares.Length; j++) if (rg.ForeignRulers[j] == n.Key && rg.ForeignShares[j] > 0) held += rg.ForeignShares[j];
                bool cand = held > 0;
                if (!cand) foreach (var war in dip.Wars) if ((ReferenceEquals(war.A, n.P) && ReferenceEquals(war.B, on.P)) || (ReferenceEquals(war.B, n.P) && ReferenceEquals(war.A, on.P))) { cand = true; break; }
                if (!cand) { var rc = Places.Coord(rg.Name); int nd = Diplomacy.CessionNode(rc.Lat, rc.Lon); cand = nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _); }
                if (!cand) continue;
                double rho;
                if (snap is not null && capIx >= 0 && snap.Idx.TryGetValue(rg, out var ri))
                {
                    rho = RpEngReach(snap.KmOf(capIx, ri));
                    foreach (var (si, ow) in origins) { if (si == ri || si == capIx) continue; double via = RpEngReach(snap.KmOf(capIx, si)) * ow * RpEngReach(snap.KmOf(si, ri)); if (via > rho) rho = via; }
                }
                else rho = RpEngGreatCircle(n.W, rg);
                double b = Math.Min(1.0, Math.Max(0.0, on.Ow[ok]));
                double wgt = on.Y[ok] * (1 - Math.Min(1.0, held)) * (1 - b);   // Vb：重みに ρ を入れない（ρ は力で 1 回だけ）
                if (!(wgt > 0)) wgt = 0;
                cands.Add((rg, wgt, rho)); sumW += wgt;
            }
            foreach (var (rg, wgt, rho) in cands)
            {
                double alloc = sumW > 0 ? n.O * wgt / sumW : 0;
                if (dip.RpEngAlloc is { } da) da[(n.Key, rg)] = (wgt, alloc, alloc > 0 ? alloc * rho : 0);
                if (!(alloc > 0)) continue;
                if (!foreign.TryGetValue(rg, out var lst)) foreign[rg] = lst = [];
                lst.Add((n.Key, alloc * rho));
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
