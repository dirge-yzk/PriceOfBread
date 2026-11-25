using TrEntry = (int nation, PriceOfBread.SimCore.Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk);

namespace PriceOfBread.SimCore;

public sealed partial class InternationalMarket
{
    internal static long TimeListsTicks, TimeClearTicks;
    internal static long CntGoods, CntBuyers, CntRollIters, CntScanSteps, CntSkips, CntFills, CntDeals;
    internal static long CntIdleSkips, CntScopeRows, CntScopeFallbacks, CntScopeWalk;
    internal static long CntRowSkipNeed, CntRowSkipRef, CntRowSkipDead, CntRowSkipBudget, CntCutLp, CntWedgeSkips;
    [System.NonSerialized] private long[]? _cost;
    internal long[] Cost => _cost ??= new long[CostLedger.Count];
    internal long[]? CostOrNull => _cost;
    internal static bool DealConstAlwaysStale;

    internal static double InlandRailAfterDecay(double railToPort, double nearestPortKm)
        => railToPort / (1.0 + nearestPortKm / Diplomacy.DistanceNormKm);

    private static double IsolationExportFactor(double tariff, Coefficients c)
        => System.Math.Max(0, 1.0 - c.IsolationExportWeight * System.Math.Max(0, tariff) * System.Math.Max(0, tariff));
    private const int RolloverGuardMax = 400;
    public double[][]? PairFlowQ;
    public double[][]? LastPairFlow;
    public double[]? DiscoveryGoodFlowQ;
    [System.NonSerialized] private int[][]? _discCarriers, _discRowsByCarrier;
    internal int[][] DiscoveryCarriers(IReadOnlyList<Simulation> nations, Coefficients c)
    {
        if (_discCarriers is null)
        {
            var ws = new List<World>(nations.Count);
            foreach (var n in nations) ws.Add(n.World);
            _discCarriers = Discovery.CarriersByRow(ws, c.DiscoveryCarrierDownstream > 0);
            _discRowsByCarrier = Discovery.RowsByCarrier(_discCarriers);
        }
        return _discCarriers;
    }
    internal void AccumulateDiscovery(IReadOnlyList<Simulation> nations, Coefficients c, int seller, int buyer, int g, double value)
    {
        var gf = DiscoveryGoodFlowQ ??= new double[Catalog.Goods.Length];
        if (g < 0 || g >= gf.Length || !(value > 0)) return;
        gf[g] += value;
        DiscoveryCarriers(nations, c);
        var rows = _discRowsByCarrier![g];
        if (rows.Length == 0) return;
        var ks = nations[seller].World.Discovered; var wb = nations[buyer].World; var kb = wb.Discovered;
        foreach (int r in rows)
            if (Discovery.Known(ks, r) && !Discovery.Known(kb, r))
            {
                (wb.DiscoveryImportQ ??= new double[Catalog.Discoveries.Length])[r] += value;
                if (Instruments.On) Instruments.DiscoveryContact(wb, nations[seller].World, r, g, value);
            }
    }
    private void AccumulatePairFlow(int seller, int buyer, double value, int n)
    {
        if (PairFlowQ is null || PairFlowQ.Length != n)
        {
            PairFlowQ = new double[n][];
            for (int i2 = 0; i2 < n; i2++) PairFlowQ[i2] = new double[n];
        }
        PairFlowQ[seller][buyer] += value;
    }

    [System.NonSerialized] private int _scopeStamp;
    internal static double PairKmCore(TrEntry[] tr, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, double[] terrCost, double[] landRatio, int[] landNode, double[] landGraft, double landRouteEnabled, int a, int b, out bool land)
    {
        double viaSea = double.PositiveInfinity;
        if (tr[a].seaOk && tr[b].seaOk)
        {
            double sea = SeaGraph.SeaDistanceKmFromNodes(seaNodeOf[a], seaNodeOf[b], seaHalf[a] + seaHalf[b], tr[a].lat, tr[a].lon, tr[b].lat, tr[b].lon, suezOkN[tr[a].nation] && suezOkN[tr[b].nation], panOkN[tr[a].nation] && panOkN[tr[b].nation]);
            viaSea = sea + tr[a].landKm * terrCost[a] * landRatio[a] + tr[b].landKm * terrCost[b] * landRatio[b];
        }
        land = false;
        if (!(landRouteEnabled > 0)) return viaSea;
        int na = landNode[a], nb = landNode[b];
        if (na < 0 || nb < 0) return viaSea;
        double raw = PathGraph.LandMatrix()[na * PathGraph.Nodes.Length + nb];
        if (double.IsPositiveInfinity(raw)) return viaSea;
        double viaLand = (raw + landGraft[a] + landGraft[b]) * 0.5 * (terrCost[a] * landRatio[a] + terrCost[b] * landRatio[b]);
        land = viaLand < viaSea;
        return Math.Min(viaSea, viaLand);
    }

    internal const byte KmStDone = 1, KmStSea = 2, KmStLand = 4;

    internal static byte PairKmParts(TrEntry[] tr, int[] seaNodeOf, double[] seaHalf, bool[] suezOkN, bool[] panOkN, int[] landNode, double[] landGraft, double landRouteEnabled, int a, int b, out double sea, out double landHalf)
    {
        byte st = KmStDone; sea = 0; landHalf = 0;
        if (tr[a].seaOk && tr[b].seaOk)
        {
            sea = SeaGraph.SeaDistanceKmFromNodes(seaNodeOf[a], seaNodeOf[b], seaHalf[a] + seaHalf[b], tr[a].lat, tr[a].lon, tr[b].lat, tr[b].lon, suezOkN[tr[a].nation] && suezOkN[tr[b].nation], panOkN[tr[a].nation] && panOkN[tr[b].nation]);
            st |= KmStSea;
        }
        if (!(landRouteEnabled > 0)) return st;
        int na = landNode[a], nb = landNode[b];
        if (na < 0 || nb < 0) return st;
        double raw = PathGraph.LandMatrix()[na * PathGraph.Nodes.Length + nb];
        if (double.IsPositiveInfinity(raw)) return st;
        landHalf = (raw + landGraft[a] + landGraft[b]) * 0.5;
        return (byte)(st | KmStLand);
    }

    internal static double ComposeKm(byte st, double sea, double landHalf, double ea, double eb, double ka, double kb, out bool land)
    {
        double viaSea = (st & KmStSea) != 0 ? sea + ea + eb : double.PositiveInfinity;
        land = false;
        if ((st & KmStLand) == 0) return viaSea;
        double viaLand = landHalf * (ka + kb);
        land = viaLand < viaSea;
        return Math.Min(viaSea, viaLand);
    }

    [System.NonSerialized] internal int ClearingSerial;

    private static double PayRailFreight(IReadOnlyList<Simulation> nations, TrEntry[] tr, int tc, double[] railRecv, double[] exRate, ref double fxIntended)
    {
        bool ins = Instruments.On;
        double back = 0;
        Dictionary<string, int>? keyOf = null;
        List<Pop>?[]? invOf = null;
        for (int t = 0; t < tc; t++)
        {
            double g = railRecv[t];
            if (!(g > 0)) continue;
            var r = tr[t].r; int n = tr[t].nation; var W = nations[n].World;
            double sumF = 0;
            for (int k = 0; k < r.ForeignShares.Length; k++) if (r.ForeignShares[k] > 0) sumF += r.ForeignShares[k];
            double scale = sumF > 1.0 ? 1.0 / sumF : 1.0;
            double dom = g;
            if (sumF > 0)
                for (int k = 0; k < r.ForeignShares.Length && k < r.ForeignRulers.Length; k++)
                {
                    double x = r.ForeignShares[k];
                    if (!(x > 0)) continue;
                    if (keyOf is null)
                    {
                        keyOf = new Dictionary<string, int>(StringComparer.Ordinal);
                        for (int i2 = 0; i2 < nations.Count; i2++)
                            if (nations[i2].World.SelfPower is { } sp) keyOf.TryAdd(sp.BaseName.Length > 0 ? sp.BaseName : sp.Name, i2);
                        invOf = new List<Pop>?[nations.Count];
                    }
                    if (!keyOf.TryGetValue(r.ForeignRulers[k], out int f) || f == n) continue;
                    var inv = invOf![f];
                    if (inv is null)
                    {
                        inv = new List<Pop>();
                        foreach (var r2 in nations[f].World.Regions) if (r2.InvestorPopIndex >= 0 && r2.InvestorPopIndex < r2.Pops.Count) inv.Add(r2.Pops[r2.InvestorPopIndex]);
                        invOf[f] = inv;
                    }
                    double sz = 0; foreach (var p2 in inv) sz += p2.Size;
                    if (!(sz > 1e-9)) continue;
                    double gf = g * x * scale;
                    dom -= gf;
                    double amtF = gf / exRate[f];
                    fxIntended += amtF;
                    World.Transfer((null, null), (nations[f].World, inv, null), amtF, TreasuryCat.OtherIn, income: true);
                    if (ins) Instruments.RailFreightPaid(r, nations[f].World, gf);
                }
            if (!(dom > 0)) continue;
            List<Pop>? rec = null;
            if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count && r.Pops[r.InvestorPopIndex].Size > 1e-9) rec = [r.Pops[r.InvestorPopIndex]];
            else { double szR = 0; foreach (var p2 in r.Pops) szR += p2.Size; if (szR > 1e-9) rec = r.Pops; }
            if (rec is null) { back += dom; continue; }
            double amt = dom / exRate[n];
            fxIntended += amt;
            World.Transfer((null, null), (W, rec, null), amt, TreasuryCat.OtherIn, income: true);
            if (ins) Instruments.RailFreightPaid(r, W, dom);
        }
        return back;
    }


    private readonly struct Seller(int node, double price, double stock)
    {
        public readonly int Node = node;
        public readonly double Price = price;
        public readonly double Stock = stock;
    }

    private static void RecordPartner(string[]? slot, int g, string name, double value, double[] top)
    {
        if (slot is null || !(value > top[g])) return;
        top[g] = value; slot[g] = name;
    }

    private sealed class KmScratch
    {
        public int Tc = -1, G;
        public double LandRoute = double.NaN, RiskLambda = double.NaN;
        public double[] Km = [], Risk = [];
        public double[] Sea = [], LandHalf = [];
        public byte[] St = [];
        public double[] E = [], K = [];
        public bool[] Land = [];
        public int[] Gen = [];
        public long[] Sig = [], SigNext = [], RSig = [], RSigNext = [];
        public double[] LbKm = [];
        public int[] NatOff = [], NatSt = [], NatCur = [];
    }
    [System.ThreadStatic] internal static bool LbOffForTest;
    internal delegate void LbAuditFn(double delLB, double del2, double lp, int buyer, int seller);
    [System.ThreadStatic] internal static LbAuditFn? LbAudit;
    internal static bool LbSkip(double delLB, double lp) => delLB >= lp;
    [System.ThreadStatic] private static KmScratch? _kmScratch;

    private sealed class ClearScratch
    {
        public int[] A1St = []; public Producer[] A1Pr = [];   // (1144) A-1：買い手の州×財の生産者の一覧（CSR・清算ごとに作り直す作業領域）
        public Dictionary<Region, double> PortStart = new();
        public Dictionary<string, int> TransitPortOwner = new();
        public List<(int user, int owner, Region r)> TransitUses = [];
        public Good?[] GoodById = [];
        public double[] ExRate = [], Tar = [], Corn = [], MilExportAcc = [], ShipShare = [], ImportVal = [], ExportVal = [], PartnerStd = [], PartnerBal = [], PartnerTot = [], PremRecv = [];
        public bool[] Oban = [], Oexban = [], NatAnyHostile = [], SuezOkN = [], PanOkN = [], Ok = [], CheapAfter = [];
        public double[][] NatHostility = [], ImpTop = [], ExpTop = [], ExpByGood = [], ImpByGood = [];
        public TrEntry[] Tr = new TrEntry[512];
        public int[] SeaNodeOf = [], LandNode = [], NextS = [], CandS = [];
        public double[] SeaHalf = [], TerrCost = [], LandRatio = [], LandGraft = [], Budget = [], PayHouse = [], PortRecv = [], PayTariff = [], PaySub = [],
            Rev = [], RevX = [], ExpReg = [], PotNew = [], CapLeft = [], LpOf = [], Left = [], CandD = [], CandK = [], CandF = [];
        public List<Pop>?[] PayerOf = [], Underwriters = [];
        public List<Pop>?[] ShipOwners = [];
        public bool[] Bill = [];
        public Pop?[] PortOwner = [];
        public Market[] MktOf = [];
        public double[] RailFrac = [], RailRecv = [], RailTgtN = [], RailEvenN = [], RailPotN = [];
        public bool[] RailByRegN = [];
        public Region[] DealtTmp = [];
        public int[] Member = [];
        public uint[] IdleKey = [];
        public List<Seller> Sellers = [];
        public List<(int Node, double Price)> Buyers = [];

        public static T[] Fit<T>(ref T[] a, int n) { if (a.Length < n) a = new T[n]; return a; }
        public static T[] FitZero<T>(ref T[] a, int n) { if (a.Length < n) a = new T[n]; else Array.Clear(a, 0, n); return a; }
        public static double[][] FitRows(ref double[][] a, int n, int gc)
        {
            if (a.Length < n) { var b = new double[n][]; Array.Copy(a, b, a.Length); a = b; }
            for (int i = 0; i < n; i++) { if (a[i] is null || a[i].Length != gc) a[i] = new double[gc]; else Array.Clear(a[i]); }
            return a;
        }
        public void Release(int tc, int nn, int gc)
        {
            Array.Clear(Tr, 0, tc); Array.Clear(PayerOf, 0, tc); Array.Clear(PortOwner, 0, tc); Array.Clear(MktOf, 0, tc);
            Array.Clear(Underwriters, 0, nn); Array.Clear(ShipOwners, 0, nn); Array.Clear(GoodById, 0, gc);
            Array.Clear(NatHostility, 0, nn); Array.Clear(ExpByGood, 0, nn); Array.Clear(ImpByGood, 0, nn);
            Sellers.Clear(); Buyers.Clear(); PortStart.Clear(); TransitPortOwner.Clear(); TransitUses.Clear();
            Array.Clear(DealtTmp);
        }
    }
    [System.ThreadStatic] private static ClearScratch? _clearScratch;

    /// <summary>(1144) A-1：繋がった買い手の州×財の生産者の一覧（CSR・鍵＝t×gc＋財・並びは r.Producers の順）。返り値＝並べた生産者の数（台帳の件数）。</summary>
    private static long A1Index(TrEntry[] tr, int tc, int gc, ClearScratch sc)
    {
        int n = tc * gc;
        if (sc.A1St.Length < n + 1) sc.A1St = new int[n + 1];
        var st = sc.A1St; Array.Clear(st, 0, n + 1);
        int tot = 0;
        for (int t = 0; t < tc; t++)
        {
            if (!tr[t].linked) continue;
            foreach (var p in tr[t].r.Producers) if ((uint)p.GoodId < (uint)gc) { st[t * gc + p.GoodId + 1]++; tot++; }
        }
        for (int i = 0; i < n; i++) st[i + 1] += st[i];
        if (sc.A1Pr.Length < tot) sc.A1Pr = new Producer[Math.Max(64, tot * 2)];
        var pr = sc.A1Pr; var cur = new int[gc];
        for (int t = 0; t < tc; t++)
        {
            if (!tr[t].linked) continue;
            Array.Copy(st, t * gc, cur, 0, gc);
            foreach (var p in tr[t].r.Producers) if ((uint)p.GoodId < (uint)gc) pr[cur[p.GoodId]++] = p;
        }
        return tot;
    }

    /// <summary>(1144) A-1 の P̂(p)＝州×財の生産者の〈供給①を値段 p で読み直した来月の産出〉の和（写し T1OutPrev・T1Base・T1Mc から・目標＝T1Base×採算・Toward・負の切り捨て・床）。
    /// 写しが無い生産者（3 つとも 0＝眠り・倒産・その月に供給①を通らない）は今の産出のまま（退かない）・未発明（T1Base＜0）は 0。<paramref name="cnt"/>＝見た生産者の数（台帳）。</summary>
    internal static double A1PHat(Producer[] pr, int[] st, int key, double pLoc, Coefficients cn, ref long cnt)
    {
        int lo = st[key], hi = st[key + 1];
        cnt += hi - lo;
        double sum = 0;
        for (int k = lo; k < hi; k++)
        {
            var p = pr[k];
            double b = p.T1Base, o0 = p.T1OutPrev, mc = p.T1Mc;
            if (b == 0 && o0 == 0 && mc == 0) { sum += p.Output; continue; }
            if (b < 0) continue;
            double target = b * EconomyStep.ProfitFactor(p, pLoc, mc, cn);
            double o = PoliticalQuantities.Toward(o0, target, cn.SupplyAdjustSpeed);
            if (o < 0) o = 0;
            else if (!(target > 0) && o < cn.TargetStockFloor) o = 0;
            sum += o;
        }
        return sum;
    }

    /// <summary>(1144→1159) A-1 の写しを 0 へ戻す＝この清算で並べた生産者（A1Index の一覧の先頭 n 本）だけ。並べていない生産者の写しは清算が読まない・
    /// 次の月の供給①が全ての生産者の写しを書き直す（眠り・倒産で飛ばす生産者は供給①が 0 へ戻す＝EconomyStep の ① の 2 つの continue）。返り値＝戻した数（台帳）。</summary>
    private static long A1Invalidate(Producer[] pr, long n)
    {
        for (long k = 0; k < n; k++) { var p = pr[k]; p.T1OutPrev = 0; p.T1Base = 0; p.T1Mc = 0; }
        return n;
    }

    private void StepBilateral(IReadOnlyList<Simulation> nations, Coefficients c, bool firstClearingOfQuarter)
    {
        int gc = _price.Length, nn = nations.Count;
        if (nn == 0) return;
        var sc = _clearScratch ??= new ClearScratch();
        var portStart = sc.PortStart; portStart.Clear();
        var goodById = ClearScratch.FitZero(ref sc.GoodById, gc);
        foreach (var g in nations[0].World.Region.Goods) goodById[g.Id] = g;

        var exRate = ClearScratch.Fit(ref sc.ExRate, nn); var tar = ClearScratch.Fit(ref sc.Tar, nn); var corn = ClearScratch.Fit(ref sc.Corn, nn);
        var oban = ClearScratch.Fit(ref sc.Oban, nn); var oexban = ClearScratch.Fit(ref sc.Oexban, nn); var milExportAcc = ClearScratch.Fit(ref sc.MilExportAcc, nn);
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            exRate[i] = w0.FxRate;
            tar[i] = TariffRate(w0, c);
            corn[i] = CornFactor(w0.CornLaws);
            oban[i] = c.OpiumAddictionGrowth > 0 && w0.OpiumPossession != ReformLevel.None;
            oexban[i] = c.OpiumAddictionGrowth > 0 && w0.OpiumExportBan;
            milExportAcc[i] = w0.MilitaryExportAccess();
        }

        var dip = nations[0].World.Diplomacy;
        Chokepoint? Cp(string id) { if (dip is not null) foreach (var c2 in dip.Chokepoints) if (c2.Id == id) return c2; return null; }
        var suez = Cp(Diplomacy.SuezId); var pan = Cp(Diplomacy.PanamaId);
        bool Blocked(World w, Chokepoint? cp) => cp is { Owner.Nation: not null } && cp.Owner != w.SelfPower && dip is { } d && w.SelfPower is { } sp && d.IsAtWar(sp, cp.Owner!);

        var tr = sc.Tr; int tc = 0;
        var transitPortOwner = sc.TransitPortOwner; transitPortOwner.Clear();
        for (int i2 = 0; i2 < nn; i2++)
            foreach (var r2 in nations[i2].World.Regions)
                if (r2.IsPort) transitPortOwner.TryAdd(r2.Name, i2);
        var transitUses = sc.TransitUses; transitUses.Clear();
        var natHostility = ClearScratch.Fit(ref sc.NatHostility, nn);
        var natAnyHostile = ClearScratch.Fit(ref sc.NatAnyHostile, nn);
        var suezOkN = ClearScratch.Fit(ref sc.SuezOkN, nn); var panOkN = ClearScratch.Fit(ref sc.PanOkN, nn);
        var railByRegN = ClearScratch.Fit(ref sc.RailByRegN, nn); var railTgtN = ClearScratch.Fit(ref sc.RailTgtN, nn);
        var railEvenN = ClearScratch.Fit(ref sc.RailEvenN, nn); var railPotN = ClearScratch.Fit(ref sc.RailPotN, nn);
        ClearingSerial++;
        for (int i = 0; i < nn; i++)
        {
            var W = nations[i].World;
            suezOkN[i] = suez is { IsOpen: true } && !Blocked(W, suez);
            panOkN[i] = pan is { IsOpen: true } && !Blocked(W, pan);
            natHostility[i] = dip is { } dH && W.SelfPower is { } spH ? dH.BasinHostility(spH) : new double[Basins.Count];
            bool any = false; foreach (var hv in natHostility[i]) if (hv > 1e-9) { any = true; break; }
            natAnyHostile[i] = any;
            double natAccess = W.ExternalTradeAccess();
            var shareBasis = Transport.ShareBasis(W, c);
            bool byReg = railByRegN[i] = W.Coef.RailByRegion > 0;
            railTgtN[i] = byReg ? Transport.Target(W, W.Coef) : 0.0; railEvenN[i] = shareBasis.Even; railPotN[i] = W.Coef.RailNeedByOutput > 0 ? double.NaN : shareBasis.TotalPot;   // (1137) 旗 1：産出の和は下の φ で線路のある州が初めて読むときに取る
            double railCapNat = byReg ? 0.0 : W.RailCapacity + Transport.PreRailCapacity(W, c);
            double preRail = byReg ? Transport.PreRailCapacity(W, c) : 0.0;
            double shipCapNat = W.ShipCapacity / (1.0 + W.ColonialShipBurden);
            foreach (var r in W.Regions)
            {
                double share = Transport.RegionShare(shareBasis, r);
                double railToPort = byReg ? r.RailTrack + preRail * share : railCapNat * share;
                double shipEff = shipCapNat * share;
                if (firstClearingOfQuarter) { r.LastPortThroughput = 0; r.LastLandPartnerName = ""; r.LastLandPartnerFlow = 0; }
                portStart[r] = r.LastPortThroughput;
                var cc = Climate.CityCoord(r.Name);
                double lat = cc.Lat, lon = cc.Lon, landKm = 0; bool linked = r.IsPort;
                if (!r.IsPort && r.NearestPortName.Length > 0)
                {
                    var pc = Climate.CityCoord(r.NearestPortName);
                    if (pc.Lat != 0 || pc.Lon != 0) { lat = pc.Lat; lon = pc.Lon; landKm = r.PortLandKm; linked = true; }
                }
                else if (!r.IsPort && r.TransitPortName.Length > 0)
                {
                    r.LastTransitPortName = "";
                    for (int cix = 0; cix < 2; cix++)
                    {
                        string tName = cix == 0 ? r.TransitPortName : r.TransitPort2Name;
                        double tKm = cix == 0 ? r.TransitLandKm : r.TransitLand2Km;
                        if (tName.Length == 0 || !transitPortOwner.TryGetValue(tName, out int oi)) continue;
                        if (oi != i && dip is { } dT && W.SelfPower is { } spT && nations[oi].World.SelfPower is { } opT && dT.IsAtWar(spT, opT)) continue;
                        var pcT = Climate.CityCoord(tName);
                        if (pcT.Lat == 0 && pcT.Lon == 0) continue;
                        lat = pcT.Lat; lon = pcT.Lon; landKm = tKm; linked = true;
                        r.LastTransitPortName = tName;
                        if (oi != i) transitUses.Add((i, oi, r));
                        break;
                    }
                }
                if (lat == 0 && lon == 0) linked = false;
                if (linked && Climate.PortFrozen(lat, lon, W.Quarter - 1)) { linked = false; r.LastPortFrozen = true; }
                else r.LastPortFrozen = false;
                double portBonus = c.PortCapToThroughput * r.PortCapacity;
                double inlandRail = railToPort;
                if (!r.IsPort && r.NearestPortDistanceKm > 1e-9)
                    inlandRail = InlandRailAfterDecay(railToPort, r.NearestPortDistanceKm);
                double cap = (r.IsPort ? shipEff : Math.Min(inlandRail, shipEff)) + portBonus;
                r.LastTransportCap = cap;
                bool seaOk = linked;
                if (c.LandRouteEnabled > 0 && (cc.Lat != 0 || cc.Lon != 0)) linked = true;
                if (tc == tr.Length) { Array.Resize(ref sc.Tr, tr.Length * 2); tr = sc.Tr; }
                tr[tc++] = (i, r, lat, lon, landKm, natAccess, cap, linked, cc.Lat, cc.Lon, seaOk);
            }
        }
        var seaNodeOf = ClearScratch.Fit(ref sc.SeaNodeOf, tc); var seaHalf = ClearScratch.Fit(ref sc.SeaHalf, tc);
        for (int t0 = 0; t0 < tc; t0++)
        {
            int sn = SeaGraph.NearestSeaNode(tr[t0].lat, tr[t0].lon);
            seaNodeOf[t0] = sn;
            seaHalf[t0] = Geo.DistanceKm(tr[t0].lat, tr[t0].lon, SeaGraph.Nodes[sn].Lat, SeaGraph.Nodes[sn].Lon);
        }
        var terrCost = ClearScratch.Fit(ref sc.TerrCost, tc);
        var landRatio = ClearScratch.Fit(ref sc.LandRatio, tc);
        var railFrac = ClearScratch.Fit(ref sc.RailFrac, tc);
        double rcf01 = PoliticalQuantities.Clamp01(c.RailCostFraction);
        for (int t0 = 0; t0 < tc; t0++)
        {
            int nt0 = tr[t0].nation;
            if (railByRegN[nt0])
            {
                var rr = tr[t0].r; int tiR = (int)rr.Terrain;
                double bh = tiR < c.TerrainHorseCost.Length ? c.TerrainHorseCost[tiR] : 1.0;
                double br = (tiR < c.TerrainRailCost.Length ? c.TerrainRailCost[tiR] : 1.0) * rcf01;
                if (rr.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) { bh = c.TerrainWaterMajorCost; br = bh * rcf01; }
                else if (rr.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) { bh = c.TerrainWaterMinorCost; br = bh * rcf01; }
                var wR = nations[nt0].World;
                if (double.IsNaN(railPotN[nt0]) && rr.RailTrack > 0) railPotN[nt0] = wR.GdpPerMonth.Value;   // (1137) T＝0 の州は φ＝0 で取り分を読まない＝線路の無い国は和を取らない
                double phi = Transport.RailPhi(rr.RailTrack, railTgtN[nt0] * Transport.RailNeedShare(wR, (railEvenN[nt0], railPotN[nt0]), rr));
                double m1 = (1.0 - phi) * bh + phi * br;
                terrCost[t0] = m1;
                landRatio[t0] = c.LandSeaCostRatio;
                railFrac[t0] = phi > 0 && m1 > 0 ? phi * br / m1 : 0.0;
                continue;
            }
            railFrac[t0] = 0.0;
            var W0 = nations[nt0].World;
            double rg0 = Discovery.RailTransport(W0.Discovered) ? 1.0 : 0.0;
            landRatio[t0] = c.LandSeaCostRatio * (1.0 - rg0 * (1.0 - PoliticalQuantities.Clamp01(c.RailCostFraction)));
            double rg = rg0;
            int ti = (int)tr[t0].r.Terrain;
            double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
            double m0 = horse + rg * (rail - horse);
            if (tr[t0].r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) m0 = c.TerrainWaterMajorCost;
            else if (tr[t0].r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) m0 = c.TerrainWaterMinorCost;
            terrCost[t0] = m0;
        }
        var landNode = ClearScratch.Fit(ref sc.LandNode, tc); var landGraft = ClearScratch.FitZero(ref sc.LandGraft, tc);
        if (c.LandRouteEnabled > 0)
            for (int t0 = 0; t0 < tc; t0++)
            {
                int nd = PathGraph.NearestNode(tr[t0].ownLat, tr[t0].ownLon);
                landNode[t0] = nd;
                landGraft[t0] = nd >= 0 ? Geo.DistanceKm(tr[t0].ownLat, tr[t0].ownLon, PathGraph.Nodes[nd].Lat, PathGraph.Nodes[nd].Lon) : 0;
            }
        else for (int t0 = 0; t0 < tc; t0++) landNode[t0] = -1;
        if (TradeReachDiag.On)
        {
            TradeReachDiag.Begin(nn);
            for (int t = 0; t < tc; t++)
            {
                var e = tr[t]; TradeReachDiag.RegTotal++; TradeReachDiag.ActiveByNation[e.nation]++;
                if (e.linked) { TradeReachDiag.RegLinked++; TradeReachDiag.LinkedByNation[e.nation]++; }
                else if (e.r.LastPortFrozen) TradeReachDiag.RegFrozen++;
                else if (!e.r.IsPort && e.r.NearestPortName.Length == 0 && e.r.TransitPortName.Length == 0) TradeReachDiag.RegNoPortLink++;
                else TradeReachDiag.RegNoCoord++;
            }
        }

        var ks = _kmScratch ??= new KmScratch();
        {
            const int KS = 10, KR = 4 + Basins.Count;
            if (ks.SigNext.Length < tc * KS) ks.SigNext = new long[tc * KS];
            if (ks.RSigNext.Length < tc * KR) ks.RSigNext = new long[tc * KR];
            var sg = ks.SigNext; var rg = ks.RSigNext;
            for (int t0 = 0; t0 < tc; t0++)
            {
                var e = tr[t0]; int o = t0 * KS;
                sg[o] = e.seaOk ? 1 : 0; sg[o + 1] = BitConverter.DoubleToInt64Bits(e.lat); sg[o + 2] = BitConverter.DoubleToInt64Bits(e.lon);
                sg[o + 3] = suezOkN[e.nation] ? 1 : 0; sg[o + 4] = panOkN[e.nation] ? 1 : 0; sg[o + 5] = BitConverter.DoubleToInt64Bits(e.landKm);
                sg[o + 6] = BitConverter.DoubleToInt64Bits(terrCost[t0]); sg[o + 7] = BitConverter.DoubleToInt64Bits(landRatio[t0]);
                sg[o + 8] = landNode[t0]; sg[o + 9] = BitConverter.DoubleToInt64Bits(landGraft[t0]);
                int r0 = t0 * KR; var hv0 = natHostility[e.nation];
                rg[r0] = BitConverter.DoubleToInt64Bits(e.lat); rg[r0 + 1] = BitConverter.DoubleToInt64Bits(e.lon);
                rg[r0 + 2] = suezOkN[e.nation] ? 1 : 0; rg[r0 + 3] = panOkN[e.nation] ? 1 : 0;
                for (int b = 0; b < Basins.Count; b++) rg[r0 + 4 + b] = BitConverter.DoubleToInt64Bits(hv0[b]);
            }
            static void Drop(double[] tab, long[] sOld, long[] sNew, int k, int n)
            {
                for (int a = 0; a < n; a++)
                {
                    int o = a * k; bool same = true;
                    for (int q = 0; q < k; q++) if (sOld[o + q] != sNew[o + q]) { same = false; break; }
                    if (same) continue;
                    System.Array.Fill(tab, -1.0, a * n, n);
                    for (int j = 0; j < n; j++) tab[j * n + a] = -1.0;
                }
            }
            static void DropKm(double[] tab, byte[] st, long[] sOld, long[] sNew, int k, int n)
            {
                for (int a = 0; a < n; a++)
                {
                    int o = a * k;
                    bool geo = sOld[o] == sNew[o] && sOld[o + 1] == sNew[o + 1] && sOld[o + 2] == sNew[o + 2] && sOld[o + 3] == sNew[o + 3] && sOld[o + 4] == sNew[o + 4] && sOld[o + 8] == sNew[o + 8] && sOld[o + 9] == sNew[o + 9];
                    bool cost = sOld[o + 5] == sNew[o + 5] && sOld[o + 6] == sNew[o + 6] && sOld[o + 7] == sNew[o + 7];
                    if (geo && cost) continue;
                    System.Array.Fill(tab, -1.0, a * n, n);
                    for (int j = 0; j < n; j++) tab[j * n + a] = -1.0;
                    if (geo) continue;
                    System.Array.Clear(st, a * n, n);
                    for (int j = 0; j < n; j++) st[j * n + a] = 0;
                }
            }
            if (ks.Tc != tc)
            {
                ks.Tc = tc; ks.LandRoute = c.LandRouteEnabled; ks.RiskLambda = c.RiskLambda;
                ks.Km = new double[tc * tc]; System.Array.Fill(ks.Km, -1.0); ks.Land = new bool[tc * tc]; ks.Gen = new int[tc * tc];
                ks.Sea = new double[tc * tc]; ks.LandHalf = new double[tc * tc]; ks.St = new byte[tc * tc];
                ks.Risk = new double[tc * tc]; System.Array.Fill(ks.Risk, -1.0);
            }
            else
            {
                if (BitConverter.DoubleToInt64Bits(ks.LandRoute) != BitConverter.DoubleToInt64Bits(c.LandRouteEnabled)) { ks.LandRoute = c.LandRouteEnabled; System.Array.Fill(ks.Km, -1.0); System.Array.Clear(ks.St); }
                else DropKm(ks.Km, ks.St, ks.Sig, sg, KS, tc);
                if (BitConverter.DoubleToInt64Bits(ks.RiskLambda) != BitConverter.DoubleToInt64Bits(c.RiskLambda)) { ks.RiskLambda = c.RiskLambda; System.Array.Fill(ks.Risk, -1.0); }
                else Drop(ks.Risk, ks.RSig, rg, KR, tc);
            }
            (ks.Sig, ks.SigNext) = (sg, ks.Sig);
            (ks.RSig, ks.RSigNext) = (rg, ks.RSig);
        }
        var kmTab = ks.Km;
        var kSt = ks.St; var kSea = ks.Sea; var kLh = ks.LandHalf;
        if (ks.E.Length < tc) { ks.E = new double[tc]; ks.K = new double[tc]; }
        var kE = ks.E; var kK = ks.K;
        for (int t0 = 0; t0 < tc; t0++) { kE[t0] = tr[t0].landKm * terrCost[t0] * landRatio[t0]; kK[t0] = terrCost[t0] * landRatio[t0]; }
        long nKmRaw = 0, nKmComp = 0;
        var kmLand = ks.Land;
        var kmGen = ks.Gen; int kmG = ++ks.G;
        double KmOf(int a, int b)
        {
            int ix = b * tc + a;
            double v = kmTab[ix];
            if (v >= 0) { kmGen[ix] = kmG; return v; }
            v = PairKm(a, b, ix, out bool pairLand);
            kmTab[ix] = v;
            kmLand[ix] = pairLand;
            kmGen[ix] = kmG;
            return v;
        }
        int nnLb = nn;
        var lbTab = ks.LbKm; var natOff = ks.NatOff; var natSt = ks.NatSt;
        {
            if (lbTab.Length < tc * nnLb) lbTab = ks.LbKm = new double[tc * nnLb];
            System.Array.Fill(lbTab, -1.0, 0, tc * nnLb);
            if (natOff.Length < nnLb + 1) natOff = ks.NatOff = new int[nnLb + 1];
            if (ks.NatCur.Length < nnLb) ks.NatCur = new int[nnLb];
            if (natSt.Length < tc) natSt = ks.NatSt = new int[tc];
            System.Array.Clear(natOff, 0, nnLb + 1);
            for (int s0 = 0; s0 < tc; s0++) natOff[tr[s0].nation + 1]++;
            for (int n0 = 0; n0 < nnLb; n0++) { natOff[n0 + 1] += natOff[n0]; ks.NatCur[n0] = natOff[n0]; }
            for (int s0 = 0; s0 < tc; s0++) natSt[ks.NatCur[tr[s0].nation]++] = s0;
        }
        double LbKmOf(int t, int nat)
        {
            int li = t * nnLb + nat; double v = lbTab[li];
            if (v >= 0) return v;
            double mn = double.PositiveInfinity;
            for (int q = natOff[nat]; q < natOff[nat + 1]; q++)
            {
                int s = natSt[q]; int ix = t * tc + s; double k = kmTab[ix];
                if (!(k >= 0)) { k = PairKm(s, t, ix, out bool pairLand); kmTab[ix] = k; kmLand[ix] = pairLand; }
                mn = Math.Min(mn, k);
            }
            lbTab[li] = mn;
            return mn;
        }
        bool LandNow(int ix) => kmGen[ix] == kmG && kmLand[ix];
        double PairKm(int a, int b, int ix, out bool landOut)
        {
            byte st = kSt[ix]; double sea, lh;
            if (st == 0) { st = PairKmParts(tr, seaNodeOf, seaHalf, suezOkN, panOkN, landNode, landGraft, c.LandRouteEnabled, a, b, out sea, out lh); kSt[ix] = st; kSea[ix] = sea; kLh[ix] = lh; nKmRaw++; }
            else { sea = kSea[ix]; lh = kLh[ix]; nKmComp++; }
            double km = ComposeKm(st, sea, lh, kE[a], kE[b], kK[a], kK[b], out bool land);
            landOut = land;
            return km;
        }
        Instruments.TrRoute RouteParts(int a, int b, bool sea, double eqBook)
        {
            var rt = new Instruments.TrRoute { Sea = sea, EqBook = eqBook, TerrSeller = terrCost[a], TerrBuyer = terrCost[b], RatioSeller = landRatio[a], RatioBuyer = landRatio[b] };
            if (sea)
            {
                rt.RealSeaKm = SeaGraph.SeaDistanceKmFromNodes(seaNodeOf[a], seaNodeOf[b], seaHalf[a] + seaHalf[b], tr[a].lat, tr[a].lon, tr[b].lat, tr[b].lon, suezOkN[tr[a].nation] && suezOkN[tr[b].nation], panOkN[tr[a].nation] && panOkN[tr[b].nation]);
                rt.EqSea = rt.RealSeaKm;
                rt.RealLandKm = tr[a].landKm + tr[b].landKm; rt.RealLandSellerKm = tr[a].landKm; rt.RealLandBuyerKm = tr[b].landKm;
                rt.EqLand = tr[a].landKm * terrCost[a] * landRatio[a] + tr[b].landKm * terrCost[b] * landRatio[b]; rt.EqLandSeller = tr[a].landKm * terrCost[a] * landRatio[a]; rt.EqLandBuyer = tr[b].landKm * terrCost[b] * landRatio[b];
            }
            else
            {
                double raw = PathGraph.LandMatrix()[landNode[a] * PathGraph.Nodes.Length + landNode[b]];
                rt.RealLandKm = raw + landGraft[a] + landGraft[b]; rt.RealLandSellerKm = rt.RealLandKm * 0.5; rt.RealLandBuyerKm = rt.RealLandKm * 0.5;
                rt.EqLand = rt.RealLandKm * 0.5 * (terrCost[a] * landRatio[a] + terrCost[b] * landRatio[b]); rt.EqLandSeller = rt.RealLandKm * 0.5 * (terrCost[a] * landRatio[a]); rt.EqLandBuyer = rt.RealLandKm * 0.5 * (terrCost[b] * landRatio[b]);
            }
            return rt;
        }
        var riskTab = ks.Risk;
        double PairRisk(int seller, int buyer)
        {
            int bi = tr[buyer].nation;
            if (!natAnyHostile[bi]) return 0;
            double cached = riskTab[buyer * tc + seller];
            if (cached >= 0) return cached;
            var kmB = SeaGraph.RouteBasinKm(seaNodeOf[seller], seaNodeOf[buyer], suezOkN[bi], panOkN[bi]);
            double integral = 0;
            for (int b = 0; b < Basins.Count; b++) integral += natHostility[bi][b] * kmB[b];
            double risk = 1.0 - Math.Exp(-c.RiskLambda * integral / Diplomacy.DistanceNormKm);
            riskTab[buyer * tc + seller] = risk;
            return risk;
        }

        var impTop = ClearScratch.FitRows(ref sc.ImpTop, nn, gc); var expTop = ClearScratch.FitRows(ref sc.ExpTop, nn, gc);
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            if (w0.ImportFromByGood is null || w0.ImportFromByGood.Length != gc) { w0.ImportFromByGood = new string[gc]; w0.ExportToByGood = new string[gc]; }
            else { Array.Fill(w0.ImportFromByGood, null!); Array.Fill(w0.ExportToByGood!, null!); }
        }

        var expByGood = ClearScratch.Fit(ref sc.ExpByGood, nn); var impByGood = ClearScratch.Fit(ref sc.ImpByGood, nn);
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            if (w0.ExportsByGood is not { } eg0 || eg0.Length != gc) { w0.ExportsByGood = new double[gc]; w0.ImportsByGood = new double[gc]; }
            else { Array.Clear(eg0); Array.Clear(w0.ImportsByGood!); }
            expByGood[i] = w0.ExportsByGood!; impByGood[i] = w0.ImportsByGood!;
        }
        Instruments.NgHead(this, nations, gc, tar, exRate);
        var ts = Instruments.TsBegin(this, nations, gc, firstClearingOfQuarter, Cost[CostLedger.ClearingRows], tar, exRate);
        Instruments.RpSnapshot(firstClearingOfQuarter, nations, tr, tc, seaNodeOf, seaHalf, suezOkN, panOkN, terrCost, landRatio, landNode, landGraft, c.LandRouteEnabled);

        var importVal = ClearScratch.FitZero(ref sc.ImportVal, nn); var exportVal = ClearScratch.FitZero(ref sc.ExportVal, nn);
        var partnerStd = ClearScratch.Fit(ref sc.PartnerStd, nn); var partnerBal = ClearScratch.FitZero(ref sc.PartnerBal, nn); var partnerTot = ClearScratch.FitZero(ref sc.PartnerTot, nn);
        for (int i = 0; i < nn; i++) partnerStd[i] = nations[i].World.MonetaryLaw switch { MonetaryStandardLaw.Gold => 1.0, MonetaryStandardLaw.Silver => -1.0, _ => 0.0 };
        double freightPool = 0;
        var railRecv = ClearScratch.FitZero(ref sc.RailRecv, tc);
        var shipShare = ClearScratch.FitZero(ref sc.ShipShare, nn);
        var shipOwners = ClearScratch.FitZero(ref sc.ShipOwners, nn);
        double freightRate = 0;
        if (c.FreightRatePer10Mm > 1e-12)
        {
            double shipSum = 0; int eligible = 0;
            var ok = ClearScratch.FitZero(ref sc.Ok, nn);
            for (int i2 = 0; i2 < nn; i2++)
            {
                var so = new List<Pop>();
                foreach (var r2 in nations[i2].World.Regions)
                    if (r2.MerchantPop is { } merchSo) so.Add(merchSo);
                if (so.Count == 0) foreach (var r2 in nations[i2].World.Regions) so.AddRange(r2.Pops);
                double sz = 0; foreach (var p2 in so) sz += p2.Size;
                shipOwners[i2] = so;
                if (!(sz > 1e-9)) continue;
                ok[i2] = true; eligible++;
                shipShare[i2] = Math.Max(0, nations[i2].World.ShipCapacity / (1.0 + Math.Max(0, nations[i2].World.ColonialShipBurden))); shipSum += shipShare[i2];
            }
            if (shipSum > 1e-9) for (int i2 = 0; i2 < nn; i2++) shipShare[i2] /= shipSum;
            else for (int i2 = 0; i2 < nn; i2++) shipShare[i2] = ok[i2] ? 1.0 / eligible : 0.0;
            if (eligible > 0) freightRate = c.FreightRatePer10Mm;
        }

        double WeightMarginAdd(int g)
        {
            if (c.TransportWeightMarginScale <= 0) return 0.0;
            double rp = goodById[g] is { } gd0 ? gd0.RefPrice : 0.0;
            return rp > 1e-9 ? c.TransportWeightMarginScale * Catalog.TransportWeight(g) / rp : 0.0;
        }

        double twoEps = 2.0 * c.PriceElasticity;
        int[] nextS = sc.NextS;
        int[] candS = sc.CandS; double[] candD = sc.CandD; double[] candK = sc.CandK; double[] candF = sc.CandF;
        var payerOf = ClearScratch.FitZero(ref sc.PayerOf, tc);
        var budget = ClearScratch.FitZero(ref sc.Budget, tc);
        var bill = ClearScratch.FitZero(ref sc.Bill, tc);
        for (int t2 = 0; t2 < tc; t2++)
        {
            var r2 = tr[t2].r; bool ins2 = Instruments.On;
            var w2 = nations[tr[t2].nation].World;
            foreach (var p2 in r2.Pops)
                if (p2.Debt.Value > 0) { double back = Math.Min(p2.Debt.Value, Math.Max(0, p2.Savings.Value)); if (back > 0) { double debt0 = p2.Debt.Value; Bank.Lend(w2, p2, -back); if (ins2) Instruments.CearnBillSettle(r2, p2, debt0 - p2.Debt.Value); } }
            if (r2.MerchantPop is { } merchPay && !(c.ImportPayerLive > 0 && !(merchPay.Size > 1e-12))) { payerOf[t2] = [merchPay]; bill[t2] = !(merchPay.Debt.Value > 0); }
            else if (r2.Pops.Count > 0) payerOf[t2] = !(c.ImportPayerLive > 0) || r2.Pops.TrueForAll(p => p.Size > 1e-12) ? r2.Pops : r2.Pops.Exists(p => p.Size > 1e-12) ? r2.Pops.FindAll(p => p.Size > 1e-12) : null;
            if (payerOf[t2] is { } pl) foreach (var p2 in pl) budget[t2] += Math.Max(0, p2.Savings.Value);
            if (bill[t2]) budget[t2] = double.PositiveInfinity; if (ins2) Instruments.CearnImportHead(r2);
        }
        var payHouse = ClearScratch.FitZero(ref sc.PayHouse, tc);
        var portOwner = ClearScratch.FitZero(ref sc.PortOwner, tc);
        var portRecv = ClearScratch.FitZero(ref sc.PortRecv, tc);
        for (int t2 = 0; t2 < tc; t2++)
        {
            var r2 = tr[t2].r;
            if (c.PortFeeRate > 0 && r2.IsPort && r2.InvestorPopIndex >= 0 && r2.InvestorPopIndex < r2.Pops.Count && r2.Pops[r2.InvestorPopIndex].Size > 1e-9) portOwner[t2] = r2.Pops[r2.InvestorPopIndex];
        }
        var underwriters = ClearScratch.FitZero(ref sc.Underwriters, nn);
        var premRecv = ClearScratch.FitZero(ref sc.PremRecv, nn);
        if (c.MarinePremiumRate > 0)
            for (int i2 = 0; i2 < nn; i2++)
            {
                var w2 = nations[i2].World;
                if (w2.SelfPower is null) continue;
                var uw = new List<Pop>(); double uwSize = 0;
                foreach (var r2 in w2.Regions) if (r2.MerchantPop is { } merchUw) { uw.Add(merchUw); uwSize += merchUw.Size; }
                if (uwSize > 1e-9) underwriters[i2] = uw;
            }
        var payTariff = ClearScratch.FitZero(ref sc.PayTariff, tc);
        var paySub = ClearScratch.FitZero(ref sc.PaySub, tc);
        var rev = ClearScratch.FitZero(ref sc.Rev, tc); var revX = ClearScratch.FitZero(ref sc.RevX, tc);
        var expReg = ClearScratch.FitZero(ref sc.ExpReg, tc);
        double fxIntended = 0;
        var potNew = ClearScratch.FitZero(ref sc.PotNew, tc);
        var capLeft = ClearScratch.Fit(ref sc.CapLeft, tc); for (int t2 = 0; t2 < tc; t2++) capLeft[t2] = tr[t2].cap;
        bool thin = !firstClearingOfQuarter;
        int scopeK = (int)Math.Min(Math.Max(c.ClearingPartnerScope, 1), Math.Max(tc, 1));
        int[] member = ClearScratch.FitZero(ref sc.Member, tc);
        int rowStamp = 0;
        Region[] dealtTmp = ClearScratch.Fit(ref sc.DealtTmp, scopeK);
        if (!thin)
            for (int i = 0; i < nn; i++)
                foreach (var rC in nations[i].World.Regions)
                    foreach (var mC in rC.Markets) { mC.ClearFailLp = 0; mC.ClearPartners = null; }

        var sellers = sc.Sellers;
        var buyers = sc.Buyers;
        var mktOf = ClearScratch.Fit(ref sc.MktOf, tc); var lpOf = ClearScratch.Fit(ref sc.LpOf, tc);

        int idleN = c.ClearingIdleSearchQuarters >= 2 && !nations[0].World.WarmupFrozen ? (int)Math.Min(c.ClearingIdleSearchQuarters, 1e6) : 0;
        uint idleQ = 0; uint[] idleKey = sc.IdleKey;
        if (idleN > 0)
        {
            idleQ = (uint)(nations[0].World.Year * Calendar.QuartersPerYear + nations[0].World.Quarter - 1);
            idleKey = ClearScratch.Fit(ref sc.IdleKey, tc);
            for (int t0 = 0; t0 < tc; t0++)
            {
                uint hk = 2166136261u;
                foreach (char ch in tr[t0].r.Name) hk = (hk ^ ch) * 16777619u;
                idleKey[t0] = hk;
            }
        }

        var gOrder = new int[gc];
        for (int gi0 = 0; gi0 < gc; gi0++) gOrder[gi0] = gi0;
        {
            var dens = new double[gc];
            for (int gi0 = 0; gi0 < gc; gi0++) dens[gi0] = goodById[gi0] is { } gdd ? gdd.RefPrice / Math.Max(1e-9, Catalog.TransportWeight(gi0)) : 0.0;
            Array.Sort(gOrder, (a, b) => dens[b] != dens[a] ? dens[b].CompareTo(dens[a]) : a.CompareTo(b));
        }
        bool cheapOn = c.TradeCheapSellSubsidy > 0 && c.TradeBiasStrength > 0 && c.TradeBiasStrength < 1;
        double[]? sellCheap = cheapOn ? new double[nn] : null;
        double[]? subPay = cheapOn ? new double[tc] : null;
        bool a1On = c.TradeBuyBySupplyCurve > 0;   // (1144) T1 A-1
        long nA1 = 0, a1Tot = 0;
        if (a1On) { a1Tot = A1Index(tr, tc, gc, sc); nA1 += a1Tot; }
        var a1St = sc.A1St; var a1Pr = sc.A1Pr;
        var swSec = MultiNationSim.TimeOn ? System.Diagnostics.Stopwatch.StartNew() : null;
        for (int gIdx = 0; gIdx < gc; gIdx++)
        {
            int g = gOrder[gIdx];
            var gd = goodById[g];
            bool stapleG = gd is not null && (gd.Tier == GoodTier.Staple || gd.GrainChain);
            if (swSec is not null) { TimeClearTicks += swSec.ElapsedTicks; swSec.Restart(); }
            _volume[g] = 0;
            if (gd is null || gd.NonTradeable) continue;

            double sum = 0; int cnt = 0;
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                var mk0 = tr[t].r.Markets[g];
                double lp0 = mk0.Price * exRate[tr[t].nation];
                mktOf[t] = mk0; lpOf[t] = lp0;
                if (lp0 > 1e-9) { sum += lp0; cnt++; }
            }
            if (cnt == 0) continue;
            double avg = sum / cnt;
            if (avg <= 1e-9) continue;
            _price[g] = Math.Clamp(avg, gd.RefPrice * PriceBandFloorMul, gd.RefPrice * PriceBandCeilMul);

            double weightAdd = WeightMarginAdd(g);
            double Wg = Catalog.TransportWeight(g);
            bool cheapG = false;
            if (cheapOn)
                for (int ci = 0; ci < nn; ci++)
                {
                    bool on = nations[ci].World.TradeBiasOf(g) < 0;
                    sellCheap![ci] = on ? c.TradeBiasStrength : 0.0;
                    if (on) cheapG = true;
                }
            bool milGood = Array.IndexOf(c.MilitaryExportGoods, g) >= 0;

            sellers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                var m = mktOf[t];
                double lp = lpOf[t];
                if (lp <= 1e-9 || m.Stock <= 1e-9) continue;
                int ni = tr[t].nation;
                double sellAcc = tr[t].access * (milGood ? milExportAcc[ni] : 1.0);
                if (c.IsolationExportWeight > 0) sellAcc *= IsolationExportFactor(tar[ni], c);
                if (g == c.OpiumGoodId && oexban[ni]) sellAcc *= Contraband.LeakFraction(tr[t].r.AdminFill, c);
                if (sellAcc <= 1e-12) continue;
                double availS = m.Stock - m.TargetStock;
                if (!(availS > 1e-12)) continue;
                sellers.Add(new Seller(t, lp, availS));
            }
            if (sellers.Count == 0) { ts?.NoSellers(g); continue; }
            sellers.Sort((a, b) => a.Price != b.Price ? a.Price.CompareTo(b.Price) : string.CompareOrdinal(tr[a.Node].r.Name, tr[b.Node].r.Name));
            if (ts is not null) { ts.GoodBegin(sellers.Count); for (int s0 = 0; s0 < sellers.Count; s0++) ts.AddSeller(sellers[s0].Node, sellers[s0].Price); ts.SellerSide(tr, tc, lpOf, mktOf, g, milGood, milExportAcc); }

            bool[]? cheapAfter = null;
            if (cheapG)
            {
                cheapAfter = ClearScratch.Fit(ref sc.CheapAfter, sellers.Count);
                bool has = false;
                for (int s = sellers.Count - 1; s >= 0; s--) { if (sellCheap![tr[sellers[s].Node].nation] > 0) has = true; cheapAfter[s] = has; }
            }
            var left = ClearScratch.Fit(ref sc.Left, sellers.Count);
            for (int s = 0; s < sellers.Count; s++) left[s] = sellers[s].Stock;
            int headS = 0;
            if (nextS.Length < sellers.Count) nextS = sc.NextS = new int[Math.Max(64, sellers.Count * 2)];
            for (int s = 0; s < sellers.Count; s++) nextS[s] = s + 1;
            int gStamp = 0;
            { gStamp = ++_scopeStamp; for (int s = 0; s < sellers.Count; s++) { var sr0 = tr[sellers[s].Node].r; sr0.ClearSellerPos = s; sr0.ClearSellerStamp = gStamp; } }

            buyers.Clear();
            for (int t = 0; t < tc; t++)
            {
                if (!tr[t].linked) continue;
                double lpB = lpOf[t];
                if (lpB > 1e-9) buyers.Add((t, lpB));
            }
            if (buyers.Count == 0) continue;
            buyers.Sort((x, y) => x.Price != y.Price ? y.Price.CompareTo(x.Price) : string.CompareOrdinal(tr[x.Node].r.Name, tr[y.Node].r.Name));
            if (swSec is not null) { TimeListsTicks += swSec.ElapsedTicks; swSec.Restart(); CntGoods++; CntBuyers += buyers.Count; }
            var costC = Cost; costC[CostLedger.ClearingRows] += buyers.Count;
            long nEvalLB = 0;
            bool lbAuditOn = LbAudit is not null;
            long nSkipLB = 0;
            bool lbOn = c.TransportMargin >= 0 && weightAdd >= 0 && c.BlockadeStapleBias >= 0 && c.RiskLambda >= 0 && !LbOffForTest;
            long nFill = 0, nInv = 0, nBest = 0;
            foreach (var (t, lp) in buyers)
            {
                if (ts is not null) ts.Row(tr[t].nation, g, tr[t].r, mktOf[t]);
                var m = mktOf[t];
                if (thin && !(m.PrevImport > 0) && !(m.LastShortage > 0)) continue;
                if (thin && !(m.PrevImport > 0) && m.ClearFailLp > 0 && !(lp > m.ClearFailLp)) continue;
                double potW = 1.0;
                if (idleN > 0 && !thin && !(m.PrevImport > 0) && (!(m.LastShortage > 0) || !stapleG))
                {
                    uint hh = idleKey[t] ^ ((uint)g * 0x9E3779B1u); hh ^= hh >> 15; hh *= 0x2C1B3C6Du; hh ^= hh >> 12;
                    bool inTurn = (hh + idleQ) % (uint)idleN == 0;
                    bool wakeThr = c.ClearingIdleThrottledScan > 0 && !(tr[t].r.LastTradeThrottle >= 1.0);
                    if (!inTurn && !wakeThr) { if (MultiNationSim.TimeOn) CntIdleSkips++; if (ts is not null) ts.Outcome(Instruments.TsOutIdle); if (Instruments.On) Instruments.ScopeSkip(this, nations[tr[t].nation].World, !(tr[t].r.LastTradeThrottle >= 1.0)); m.ClearFailLp = lp; continue; }
                    if (inTurn && !wakeThr && c.ClearingIdlePotentialReweight > 0) potW = idleN;
                }
                bool restrict = false; int dealtN = 0;
                if (thin && m.ClearPartners is { } pmem)
                {
                    restrict = true; rowStamp++; if (MultiNationSim.TimeOn) CntScopeRows++;
                    foreach (var pr in pmem) if (pr.ClearSellerStamp == gStamp) member[pr.ClearSellerPos] = rowStamp;
                }
                bool scopeThr = false; long scopeFill0 = nFill, scopeInv0 = nInv, scopeBest0 = nBest, scopeSkip0 = nSkipLB;
                int scopeCls = 0; bool scopeFell = false, tbOn = false;
                if (Instruments.On) { scopeCls = thin ? (restrict ? 3 : (m.PrevImport > 0 ? 2 : 5)) : (!(m.PrevImport > 0) && (!(m.LastShortage > 0) || !stapleG) ? 1 : 0); scopeThr = !(tr[t].r.LastTradeThrottle >= 1.0); tbOn = true; }
                m.ClearFailLp = 0;
                int ni = tr[t].nation;
                int bias = nations[ni].World.TradeBiasOf(g);
                double buyDear = bias > 0 ? c.TradeBiasStrength : 0;
                double protect = Math.Max(0, tar[ni]);
                double cornPen = gd.Tier == GoodTier.Staple || gd.GrainChain ? corn[ni] * c.CornLawImportPenalty : 0;
                double duty = protect + cornPen;
                double wedge = duty - buyDear;
                double wedgeHome = -buyDear;

                if (tbOn) { double tbBm = 1 + c.TransportMargin, tbLb = tbBm + Math.Min(wedge, wedgeHome); Instruments.TbRow(this, nations[ni].World, tr[t].r, g, m, c, lp, gd.RefPrice * tr[t].r.PriceLevel * exRate[ni], headS < sellers.Count ? sellers[headS].Price : double.NaN, Math.Min(1.0, Math.Min(tbLb, cheapG ? tbLb - c.TradeBiasStrength : tbLb)), exRate[ni]); }   // T0b：行の門の 6 区分（読むだけ）
                bool rowSkip; double a1Ph0 = double.NaN;
                if (a1On)
                {   // (1144) A-1 の行の門＝真の上界 TS＋D×(rpT÷u_min)^ε−min(P, P̂(u_min))−S が 0 以下なら飛ばす（u_min≦どの成約の u′＝飛ばした行は成約できない）。P̂ は B-b の上界が 0 以下の行だけ評価する（Q≦P＝B-b が正なら A-1 も正）。
                    double a1Bm = 1 + c.TransportMargin, a1Lb = a1Bm + Math.Min(wedge, wedgeHome);
                    double a1LbMin = Math.Min(1.0, Math.Min(a1Lb, cheapG ? a1Lb - c.TradeBiasStrength : a1Lb));
                    double rpT0 = gd.RefPrice * tr[t].r.PriceLevel * exRate[ni], uMin = lp;
                    if (headS < sellers.Count) { double hm = sellers[headS].Price * a1LbMin; if (hm < uMin) uMin = hm; }
                    double ub;
                    if (!(rpT0 > 1e-9) || !(uMin > 1e-9) || double.IsInfinity(uMin)) ub = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;   // 上界を作れない行は旧の門
                    else
                    {
                        double root0 = Math.Pow(rpT0 / uMin, c.PriceElasticity);
                        ub = m.TargetStock + m.LastDemand * root0 - m.LastProduced - m.Stock;
                        if (!(ub > 0))
                        {
                            a1Ph0 = A1PHat(a1Pr, a1St, t * gc + g, uMin / exRate[ni], nations[ni].World.Coef, ref nA1);
                            double q0 = Math.Min(m.LastProduced, a1Ph0); if (!(q0 >= 0)) q0 = m.LastProduced;
                            ub = m.TargetStock + m.LastDemand * root0 - q0 - m.Stock;
                        }
                    }
                    rowSkip = !(ub > 0);
                }
                else rowSkip = !(m.TargetStock + m.LastDemand - m.LastProduced - m.Stock > 0);
                if (tbOn) { double t1Bm = 1 + c.TransportMargin, t1Lb = t1Bm + Math.Min(wedge, wedgeHome); Instruments.T1Row(this, nations[ni].World, tr[t].r, g, m, lp, gd.RefPrice * tr[t].r.PriceLevel * exRate[ni], headS < sellers.Count ? sellers[headS].Price : double.NaN, Math.Min(1.0, Math.Min(t1Lb, cheapG ? t1Lb - c.TradeBiasStrength : t1Lb)), exRate[ni], a1On, rowSkip, a1Ph0); }   // (1144) t1buy_s：W2(a) の母集合と門の照合（読むだけ）
                if (rowSkip) { if (MultiNationSim.TimeOn) CntRowSkipNeed++; if (ts is not null) { ts.Outcome(Instruments.TsOutNoNeed); ts.SkipNeed(m, gd.RefPrice, tr[t].r.PriceLevel, exRate[ni], headS, c.PriceElasticity); } continue; }
                if (!(gd.RefPrice * tr[t].r.PriceLevel * exRate[ni] > 1e-9)) { if (MultiNationSim.TimeOn) CntRowSkipRef++; if (ts is not null) ts.Outcome(Instruments.TsOutNoNeed); continue; }
                if (!(m.LastDemand > 0) && !(m.LastProduced + m.LastImport < 0)) { if (MultiNationSim.TimeOn) CntRowSkipDead++; if (ts is not null) ts.Outcome(Instruments.TsOutNoNeed); continue; }
                if (!(budget[t] > 0)) { if (MultiNationSim.TimeOn) CntRowSkipBudget++; if (ts is not null) ts.Outcome(Instruments.TsOutBudget0); continue; }
                if (thin && !(capLeft[t] / Math.Max(1e-9, Wg) > 1e-12)) { m.ClearFailLp = lp; continue; }
                if (candS.Length < sellers.Count) { int cn0 = Math.Max(64, sellers.Count * 2); candS = sc.CandS = new int[cn0]; candD = sc.CandD = new double[cn0]; candK = sc.CandK = new double[cn0]; candF = sc.CandF = new double[cn0]; }
                int rowBase = t * tc; bool hostileRow = natAnyHostile[ni];
                double tMargin = c.TransportMargin, blockStaple = c.BlockadeStapleBias; bool stapleTier = gd.Tier == GoodTier.Staple;
                double unmetQ = 0, skipQ = 0;   // (1277) skipQ＝この行で容量で飛ばした売り手へ足した量の和（旗 CapSkipSellerPotential 0 では 0 のまま）
                bool insOnC = Instruments.On, capSkipOn = !thin && c.CapSkipSellerPotential > 0, capSkipIns = insOnC && !thin;   // (1277) c131pf：潜在量を書くのは四半期の最初の清算だけ（StepBilateral の中の thin＝!firstClearingOfQuarter〔清算の準備の後で定義〕・firstClearingOfQuarter でない回は書かない）
                double baseMul = 1 + c.TransportMargin;
                double lbMul = baseMul + Math.Min(wedge, wedgeHome);
                double lbMulCheap = cheapG ? lbMul - c.TradeBiasStrength : lbMul;
                int candN = 0, resS = headS, resPrev = -1;
                int bestA = -1, bestPos = -1; double bestDelA = double.MaxValue, secondDelA = double.MaxValue, bestKmA = 0;
                bool keepBest = false;
                bool dealConstStale = true;
                double unitA = 0, rpT = 0, powVal = 0, sellAccB = 0, rpS = 0, frA = 0, sA = 0, dutyA = 0, portT = 0, portS = 0, premA = 0, costA = 0, sellSubA = 0;
                int snA = 0, siA = 0; Market msA = null!; bool xPriceOn = false, crossA = false, seaA = false;
                bool dealtRow = false;
                double a1Q = m.LastProduced, a1Root = 1.0;   // (1144) A-1 の〈来月の国内の見込み〉と需要の倍率（決まり直しごとに u′ で読み直す）
                int guard = 0; var tsWhy = Instruments.TsExit.None; bool yRemoved = false;
                if (ts is not null) ts.Scan(tr, t, lp, left, capLeft, baseMul, wedge, duty, sellCheap, cornPen);
                while (true)
                {
                    if (MultiNationSim.TimeOn) CntRollIters++;
                    if (++guard > RolloverGuardMax) { tsWhy = Instruments.TsExit.Guard; break; }
                    if (!keepBest)
                    {
                        dealConstStale = true;
                        bestA = -1; bestPos = -1; bestDelA = double.MaxValue; secondDelA = double.MaxValue;
                        for (int k = 0; k < candN; k++)
                        {
                            double dk = candD![k]; int sk = candS[k];
                            if (dk < bestDelA || (dk == bestDelA && sk < bestA)) { secondDelA = bestDelA; bestDelA = dk; bestA = sk; bestPos = k; }
                            else if (dk < secondDelA) secondDelA = dk;
                        }
                        int s2 = resS, prevS = resPrev;
                        var sellSpan = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(sellers);
                        var trL = tr; var kmTabL = kmTab; var kmGenL = kmGen; int kmGL = kmG; var riskTabL = riskTab;
                        while (s2 < sellSpan.Length)
                        {
                            if (left[s2] <= 1e-12)
                            {
                                if (MultiNationSim.TimeOn) CntSkips++;
                                int nxS = nextS![s2];
                                if (prevS < 0) headS = nxS; else nextS[prevS] = nxS;
                                s2 = nxS; continue;
                            }
                            ref readonly var sel = ref sellSpan[s2];
                            int sn = sel.Node;
                            if (capLeft[sn] <= 1e-12)
                            {
                                if (MultiNationSim.TimeOn) CntSkips++;
                                if (capSkipOn || capSkipIns)
                                {   // (1277) c131pf 旗 CapSkipSellerPotential：容量が尽きて飛ばす売り手のうち値段の門（直後の priced＝売り手の値段×下界の乗数＜買い手の値段 lp）を通る物は、この行で通るはずだった量 qS を売り手の州の潜在量へ（買い手の側には足さない）。
                                    // qS＝min(残り, max(0, X_lp−unmetQ−skipQ))・X_lp＝買い手の X を買い手の自分の値段 lp で読んだ値（P̂ を引かない）。足した qS は skipQ に数え、次の売り手の側の未充足から引く（newUnmetS の行）。
                                    bool priced = sel.Price * (cheapAfter is not null && cheapAfter[s2] ? lbMulCheap : lbMul) < lp;
                                    double addW = 0;
                                    if (capSkipOn && priced)
                                    {
                                        double ratioS = gd.RefPrice * tr[t].r.PriceLevel * exRate[ni] / lp;
                                        double xpS = m.LastDemand * Math.Pow(ratioS, twoEps) - (m.LastProduced + m.LastImport);
                                        double xnS = a1On ? m.TargetStock + m.LastDemand * Math.Pow(ratioS, c.PriceElasticity) - m.LastProduced - m.Stock : m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
                                        double xS = tr[t].access * Math.Max(0, Math.Min(xpS, xnS));
                                        if (g == c.OpiumGoodId && oban[ni]) xS *= Contraband.LeakFraction(tr[t].r.AdminFill, c);
                                        double qS = Math.Min(left[s2], Math.Max(0, xS - unmetQ - skipQ));
                                        if (qS > 0) { addW = qS * Wg * potW; potNew[sn] += addW; skipQ += qS; }
                                    }
                                    if (capSkipIns) Instruments.CapSkip(nations[tr[sn].nation].World, g, mktOf[sn], m, tr[t].r, tr[t].access, Wg, priced, addW);
                                }
                                int nxS = nextS![s2];
                                if (prevS < 0) headS = nxS; else nextS[prevS] = nxS;
                                s2 = nxS; continue;
                            }
                            double selP = sel.Price;
                            double lbv = selP * (cheapAfter is not null && cheapAfter[s2] ? lbMulCheap : lbMul);
                            if (lbv >= bestDelA) break;
                            if (lbv >= lp) { if (MultiNationSim.TimeOn) CntCutLp++; break; }
                            if (restrict && member[s2] != rowStamp) { if (MultiNationSim.TimeOn) CntScopeWalk++; prevS = s2; s2 = nextS![s2]; continue; }
                            if (MultiNationSim.TimeOn) CntScanSteps++;
                            int sNat = trL[sn].nation;
                            double wS2 = sNat == ni ? wedgeHome : wedge;
                            if (cheapG && sNat != ni) wS2 -= sellCheap![sNat];
                            if (selP * (baseMul + wS2) >= lp) { if (MultiNationSim.TimeOn) CntWedgeSkips++; prevS = s2; s2 = nextS![s2]; continue; }
                            double lbAud = double.NaN;
                            if (lbOn && selP >= 0)
                            {   nEvalLB++;
                                double kmL = LbKmOf(t, sNat);
                                double marL = tMargin * (1.0 + kmL / Diplomacy.DistanceNormKm)
                                            + weightAdd * (kmL / Diplomacy.DistanceNormKm) + 0.0;
                                double facL = 1 + marL + wS2 + 0.0;
                                if (lbAuditOn) lbAud = selP * facL;
                                else if (LbSkip(selP * facL, lp)) { nSkipLB++; prevS = s2; s2 = nextS![s2]; continue; }
                            }
                            if (MultiNationSim.TimeOn) CntFills++;
                            nFill++;
                            int ix = rowBase + sn;
                            double km2 = kmTabL[ix];
                            if (km2 >= 0) kmGenL[ix] = kmGL; else km2 = KmOf(sn, t);
                            double risk2 = 0;
                            if (hostileRow) { double rc = riskTabL[ix]; risk2 = rc >= 0 ? rc : PairRisk(sn, t); }
                            double bb2 = stapleTier ? blockStaple * risk2 : 0.0;
                            double mar2 = tMargin * (1.0 + km2 / Diplomacy.DistanceNormKm)
                                        + weightAdd * (km2 / Diplomacy.DistanceNormKm) + risk2;
                            double fac2 = 1 + mar2 + wS2 + bb2;
                            double del2 = selP * fac2; if (del2 >= lp) nInv++;
                            if (lbAuditOn && !double.IsNaN(lbAud)) LbAudit(lbAud, del2, lp, t, sn);
                            if (del2 < double.MaxValue)
                            {
                                candS[candN] = s2; candD![candN] = del2; candK![candN] = km2; candF![candN] = fac2;
                                if (del2 < bestDelA) { nBest++; secondDelA = bestDelA; bestDelA = del2; bestA = s2; bestPos = candN; }
                                else if (del2 < secondDelA) secondDelA = del2;
                                candN++;
                            }
                            prevS = s2; s2 = nextS![s2];
                        }
                        resS = s2; resPrev = prevS;
                        bestKmA = bestPos >= 0 ? candK![bestPos] : 0;
                    }
                    keepBest = false;
                    if (bestA < 0 || !(lp > bestDelA))
                    {
                        if (restrict && capLeft[t] > 1e-12 && budget[t] > 1e-12) { restrict = false; scopeFell = true; candN = 0; resS = headS; resPrev = -1; if (MultiNationSim.TimeOn) CntScopeFallbacks++; continue; }
                        tsWhy = dealtRow ? Instruments.TsExit.Dealt : yRemoved ? Instruments.TsExit.YZero : Instruments.TsExit.PriceHigh;
                        break;
                    }
                    if (dealConstStale || DealConstAlwaysStale)
                    {
                        dealConstStale = false;
                        unitA = Math.Min(lp, double.IsPositiveInfinity(secondDelA) ? lp : secondDelA);
                        unitA = Math.Max(unitA, sellers[bestA].Price);
                        snA = sellers[bestA].Node; siA = tr[snA].nation;
                        msA = mktOf[snA];
                        rpT = gd.RefPrice * tr[t].r.PriceLevel * exRate[ni];
                        xPriceOn = rpT > 1e-9 && unitA > 1e-9;
                        if (xPriceOn) powVal = Math.Pow(rpT / unitA, twoEps);
                        sellAccB = tr[snA].access * (milGood ? milExportAcc[siA] : 1.0);
                        if (c.IsolationExportWeight > 0) sellAccB *= IsolationExportFactor(tar[siA], c);
                        if (g == c.OpiumGoodId && oexban[siA]) sellAccB *= Contraband.LeakFraction(tr[snA].r.AdminFill, c);
                        rpS = gd.RefPrice * tr[snA].r.PriceLevel * exRate[siA];
                        frA = freightRate > 1e-12 ? freightRate * bestKmA / Diplomacy.DistanceNormKm : 0.0;
                        crossA = siA != ni;
                        sellSubA = cheapG && crossA ? sellCheap![siA] : 0.0;
                        sA = unitA / candF![bestPos];
                        dutyA = crossA ? duty : 0.0;
                        seaA = !LandNow(t * tc + snA);
                        portT = seaA && portOwner[t] is not null ? c.PortFeeRate : 0.0; portS = seaA && portOwner[snA] is not null ? c.PortFeeRate : 0.0;
                        premA = seaA && underwriters[ni] is not null ? c.MarinePremiumRate : 0.0;
                        costA = sA * (1 + dutyA + frA + portT + portS + premA) / exRate[ni];
                        if (sellSubA > 0) costA = sA * (1 + dutyA + frA + portT + portS + premA - sellSubA) / exRate[ni];
                        if (ts is not null) ts.Repick(m, tr[t].access, gd.RefPrice, tr[t].r.PriceLevel, exRate[ni], unitA, c.PriceElasticity);
                        if (a1On)
                        {   // (1144) A-1：u′＝min(unitA, lp)・Q＝min(P, P̂(u′))・需要の倍率 (rpT÷u′)^ε
                            double uP = Math.Min(unitA, lp);
                            if (xPriceOn && uP > 1e-9)
                            {
                                a1Root = Math.Pow(rpT / uP, c.PriceElasticity);
                                double ph = A1PHat(a1Pr, a1St, t * gc + g, uP / exRate[ni], nations[ni].World.Coef, ref nA1);
                                a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;
                            }
                            else { a1Root = 1.0; a1Q = m.LastProduced; }
                        }
                    }
                    double xPrice, xNeed;
                    if (a1On)
                    {
                        xPrice = xPriceOn ? m.LastDemand * powVal - (a1Q + m.LastImport) : 0.0;
                        xNeed = m.TargetStock + m.LastDemand * a1Root - a1Q - m.Stock;
                    }
                    else
                    {
                        xPrice = xPriceOn ? m.LastDemand * powVal - (m.LastProduced + m.LastImport) : 0.0;
                        xNeed = m.TargetStock + m.LastDemand - m.LastProduced - m.Stock;
                    }
                    double X = tr[t].access * Math.Max(0, Math.Min(xPrice, xNeed));
                    if (g == c.OpiumGoodId && oban[ni]) X *= Contraband.LeakFraction(tr[t].r.AdminFill, c);
                    double yPrice = rpS > 1e-9 ? (msA.LastProduced + msA.LastImport) * unitA / rpS - msA.LastDemand - msA.LastExport : 0.0;
                    double Y = sellAccB * Math.Max(0, Math.Min(yPrice, left[bestA]));
                    double pot = Math.Min(Math.Min(X, Y), left[bestA]);
                    pot = Math.Min(pot, budget[t] / Math.Max(1e-12, costA));
                    double qA = pot;
                    qA = Math.Min(qA, Math.Min(capLeft[t], capLeft[snA]) / Math.Max(1e-9, Wg));
                    if (ts is not null) ts.Iter(X, Y, left[bestA], budget[t] / Math.Max(1e-12, costA), Math.Min(capLeft[t], capLeft[snA]) / Math.Max(1e-9, Wg));
                    if (pot > 0)
                    {
                        double doneQ = qA > 1e-12 ? qA : 0.0;
                        double newUnmet = Math.Min(pot - doneQ, Math.Max(0, X - doneQ - unmetQ));
                        double newUnmetS = skipQ > 0 ? Math.Min(newUnmet, Math.Max(0, X - doneQ - unmetQ - skipQ)) : newUnmet;   // (1277) 売り手の側の未充足は、この行で飛ばした売り手へ足した skipQ を引く（同じ需要を二度数えない）。買い手の側は newUnmet のまま
                        double potBefore = insOnC ? potNew[t] : 0.0;
                        if (potW != 1.0) { potNew[t] += (doneQ + newUnmet) * Wg * potW; potNew[snA] += (doneQ + newUnmetS) * Wg * potW; }
                        else { potNew[t] += (doneQ + newUnmet) * Wg; potNew[snA] += (doneQ + newUnmetS) * Wg; }
                        if (insOnC) { Instruments.ScopePot(this, nations[ni].World, scopeCls, scopeThr, (doneQ + newUnmet) * Wg, potNew[t] - potBefore); if (skipQ > 0) Instruments.CapSkipDedup(nations[siA].World, g, (newUnmet - newUnmetS) * Wg * potW, newUnmet * Wg * potW); }
                        unmetQ += newUnmet;
                    }
                    if (!(qA > 1e-12))
                    {
                        if (!(X > 1e-12)) { tsWhy = dealtRow ? Instruments.TsExit.Dealt : xPrice <= xNeed ? Instruments.TsExit.XPriceSmall : Instruments.TsExit.XNeedSmall; if (ts is not null) Instruments.TsExitProbe?.Invoke(xPrice, xNeed, tsWhy); break; }
                        if (capLeft[t] <= 1e-12 && !(X - unmetQ > 1e-12)) { tsWhy = Instruments.TsExit.CapMid; break; }
                        if (!(budget[t] > 1e-12)) { tsWhy = Instruments.TsExit.Budget; break; }
                        yRemoved = true; if (ts is not null) ts.IterRemove();
                        candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN]; candF![bestPos] = candF[candN];
                        continue;
                    }
                    if (MultiNationSim.TimeOn) CntDeals++;
                    dealtRow = true;
                    if (!thin && dealtN < scopeK)
                    {
                        var dr = tr[snA].r; bool dup = false;
                        for (int di = 0; di < dealtN; di++) if (ReferenceEquals(dealtTmp[di], dr)) { dup = true; break; }
                        if (!dup) dealtTmp[dealtN++] = dr;
                    }
                    double vA = qA * sA;
                    double frG = vA * frA;
                    if (TradeReachDiag.On) { TradeReachDiag.Deal(siA, ni, vA, bestKmA); TradeReachDiag.DealRef(siA, ni, qA * gd.RefPrice); }
                    left[bestA] -= qA;
                    msA.Stock -= qA; msA.LastExport += qA;
                    m.Stock += qA; m.LastImport += qA;
                    if (c.DiffusionWeight > 0 && crossA) AccumulatePairFlow(siA, ni, vA, nn);
                    if (crossA) AccumulateDiscovery(nations, c, siA, ni, g, vA);
                    double subV = sellSubA > 0 ? vA * sellSubA : 0.0;
                    double payA = sellSubA > 0 ? (vA - subV + frG) / exRate[ni] : (vA + frG) / exRate[ni];
                    double dutyPay = vA * dutyA / exRate[ni];
                    double feePay = vA * (portT + portS + premA) / exRate[ni];
                    payHouse[t] += payA + feePay; payTariff[t] += dutyPay; budget[t] -= payA + dutyPay + feePay;
                    double recvA = vA / exRate[siA];
                    double subSell = 0, recvEff = recvA;
                    if (sellSubA > 0)
                    {
                        subSell = subV / exRate[siA];
                        double subPaid = subSell * nations[siA].World.FiscalFundingRatio.Value;
                        recvEff = recvA - subSell + subPaid;
                        subPay![snA] += subPaid;
                    }
                    rev[snA] += recvEff;
                    if (crossA) { revX[snA] += recvEff; expReg[snA] += recvA; }
                    fxIntended += sellSubA > 0 ? recvA - subSell - payA : recvA - payA;
                    if (portT > 0) portRecv[t] += vA * portT / exRate[ni];
                    if (portS > 0) { double rS = vA * portS / exRate[siA]; portRecv[snA] += rS; fxIntended += rS - vA * portS / exRate[ni]; }
                    if (premA > 0) premRecv[ni] += vA * premA / exRate[ni];
                    if (railFrac[snA] > 0 || railFrac[t] > 0)
                    {
                        double rS = 0, rB = 0;
                        if (seaA) { if (bestKmA > 0 && double.IsFinite(bestKmA)) { rS = frG * kE[snA] * railFrac[snA] / bestKmA; rB = frG * kE[t] * railFrac[t] / bestKmA; } }
                        else { double kSum = kK[snA] + kK[t]; if (kSum > 0) { rS = frG * kK[snA] * railFrac[snA] / kSum; rB = frG * kK[t] * railFrac[t] / kSum; } }
                        if (!(rS >= 0)) rS = 0; if (!(rB >= 0)) rB = 0;
                        railRecv[snA] += rS; railRecv[t] += rB;
                        freightPool += frG - rS - rB;
                    }
                    else freightPool += frG;
                    if (Instruments.On) { Instruments.ScopeDeal(this, nations[ni].World, nations[siA].World, scopeCls, scopeFell, qA, unitA, bestDelA, lp, sA, scopeThr); Instruments.TbDeal(this, nations[ni].World, nations[siA].World, tr[t].r, tr[snA].r, g, m, c, lp, unitA, rpT, qA, crossA, exRate[ni]); Instruments.T1Deal(this, nations[ni].World, tr[t].r, g, m, lp, unitA, rpT, qA, crossA, vA, exRate[ni], a1On ? a1Q : double.NaN); }   // T0b：成約の上界の越え（成約の後の台帳・m.Stock はここまで動かない）・(1144) t1buy_s：U1 と W2(a) の成約
                    if (Instruments.On) Instruments.TradeDeal(this, nations[ni].World, nations[siA].World, tr[snA].r, tr[t].r, Wg, qA, vA, frG, RouteParts(snA, t, seaA, bestKmA), crossA, shipShare[siA], shipShare[ni], g, crossA ? vA * protect / exRate[ni] : 0.0, crossA ? vA * cornPen / exRate[ni] : 0.0);
                    if (ts is not null) ts.Deal(crossA);
                    if (buyDear > 0) paySub[t] += vA * buyDear / exRate[ni];
                    if (crossA) { importVal[ni] += vA + frG; impByGood[ni][g] += vA + frG; exportVal[siA] += vA; expByGood[siA][g] += vA; }
                    if (crossA) { partnerBal[ni] += partnerStd[siA] * vA; partnerTot[ni] += vA; partnerBal[siA] += partnerStd[ni] * vA; partnerTot[siA] += vA; }
                    if (seaA) { tr[t].r.LastPortThroughput += vA; tr[snA].r.LastPortThroughput += vA; }
                    _volume[g] += qA;
                    RecordPartner(nations[ni].World.ImportFromByGood, g, tr[snA].r.Name, vA, impTop[ni]);
                    RecordPartner(nations[siA].World.ExportToByGood, g, tr[t].r.Name, vA, expTop[siA]);
                    if (LandNow(t * tc + snA) || LandNow(snA * tc + t))
                    {
                        if (vA > tr[t].r.LastLandPartnerFlow) { tr[t].r.LastLandPartnerFlow = vA; tr[t].r.LastLandPartnerName = tr[snA].r.Name; }
                        if (vA > tr[snA].r.LastLandPartnerFlow) { tr[snA].r.LastLandPartnerFlow = vA; tr[snA].r.LastLandPartnerName = tr[t].r.Name; }
                    }
                    capLeft[t] -= qA * Wg; capLeft[snA] -= qA * Wg;
                    if (capLeft[t] <= 1e-12) { tsWhy = Instruments.TsExit.CapMid; break; }
                    if (!(left[bestA] <= 1e-12) && !(capLeft[snA] <= 1e-12)) keepBest = true;
                    else { candN--; candS[bestPos] = candS[candN]; candD![bestPos] = candD[candN]; candK![bestPos] = candK[candN]; candF![bestPos] = candF[candN]; }
                }
                if (ts is not null) ts.Exit(tsWhy);
                if (!thin && dealtN > 0) { var keep = new Region[dealtN]; Array.Copy(dealtTmp, keep, dealtN); m.ClearPartners = keep; }
                if (Instruments.On) { Instruments.ScopeRow(this, nations[ni].World, scopeCls, dealtRow, scopeFell, scopeThr, nFill - scopeFill0); Instruments.ScopeRowX(this, nations[ni].World, tr[t].r, nFill - scopeFill0, nInv - scopeInv0, nBest - scopeBest0, nSkipLB - scopeSkip0); }
                if (!dealtRow) m.ClearFailLp = lp;
            }
            costC[CostLedger.ClearingFills] += nFill; costC[CostLedger.ClearingSkipsLB] += nEvalLB;
            for (int t = 0; t < tc; t++)
            {
                if (!(rev[t] > 0)) continue;
                var rS = tr[t].r; var wS = nations[tr[t].nation].World;
                double comm = 0;
                if (revX[t] > 0 && rS.MerchantPop is { } merchComm)
                    comm = World.Transfer((null, null), (wS, [merchComm], null), revX[t] * c.MerchantTradeMargin.Value, TreasuryCat.OtherIn, income: true);
                if (subPay is not null && subPay[t] > 0)
                {
                    double mv = World.Transfer((wS, null), (null, null, null), subPay[t], TreasuryCat.Subsidy);
                    if (Instruments.On) wS.DiagSfCheapSubsidyPaidCum += mv;
                    subPay[t] = 0;
                }
                PaySales((null, null), rS, g, rev[t] - comm, wS);
                rev[t] = 0; revX[t] = 0;
            }
        }
        if (swSec is not null) TimeClearTicks += swSec.ElapsedTicks;

        for (int t = 0; t < tc; t++)
        {
            double cap = tr[t].cap;
            double potential = potNew[t];
            if (!thin)
            {
                tr[t].r.LastTradeThrottle = potential > 1e-9 && cap < double.MaxValue ? Math.Min(1.0, cap / potential) : 1.0;
                tr[t].r.LastTradePotential = potential;
            }
        }

        freightPool += PayRailFreight(nations, tr, tc, railRecv, exRate, ref fxIntended);
        for (int i = 0; i < nn; i++)
        {
            if (!(freightPool > 0) || !(shipShare[i] > 0)) continue;
            double amt = freightPool * shipShare[i] / exRate[i];
            if (Instruments.On) Instruments.TradeFreightReceived(this, nations[i].World, freightPool * shipShare[i]);
            fxIntended += amt;
            World.Transfer((null, null), (nations[i].World, shipOwners[i], null), amt, TreasuryCat.OtherIn, income: true);
        }
        for (int t = 0; t < tc; t++)
            if (portRecv[t] > 0 && portOwner[t] is { } po) World.Transfer((null, null), (nations[tr[t].nation].World, [po], null), portRecv[t], TreasuryCat.OtherIn, income: true);
        for (int i = 0; i < nn; i++)
            if (premRecv[i] > 0 && underwriters[i] is { } uw) World.Transfer((null, null), (nations[i].World, uw, null), premRecv[i], TreasuryCat.OtherIn, income: true);
        for (int i = 0; i < nn; i++) nations[i].World.LastTariffRevenuePerClearing = 0;
        for (int t = 0; t < tc; t++)
        {
            var w0 = nations[tr[t].nation].World; if (Instruments.On) Instruments.CearnImportPayRegion(tr[t].r);
            if (paySub[t] > 0 && payerOf[t] is { } ps) World.Transfer((w0, null), (w0, ps, null), paySub[t] * w0.FiscalFundingRatio.Value, TreasuryCat.Subsidy);
            if (bill[t] && payerOf[t] is [var mer])
            {
                double due = payTariff[t] + payHouse[t], have = Math.Max(0, mer.Savings.Value);
                if (due > have) Bank.Lend(w0, mer, due - have);
            }
            if (payTariff[t] > 0 && payerOf[t] is { } pt) w0.LastTariffRevenuePerClearing += World.Transfer((w0, pt), (w0, null, null), payTariff[t], TreasuryCat.Tariff);
            if (payHouse[t] > 0 && payerOf[t] is { } pp) World.Transfer((w0, pp), (null, null, null), payHouse[t]);
        }
        Cost[CostLedger.ClearingKmCompose] += nKmComp; Cost[CostLedger.ClearingKmRaw] += nKmRaw;
        Cost[CostLedger.ClearingA1Eval] += nA1;
        if (a1On) Cost[CostLedger.ClearingA1Invalidate] += A1Invalidate(a1Pr, a1Tot);   // (1144→1159) 写しはその月の清算だけが読む＝並べた生産者だけ 0 へ戻す（旧＝全ての国の全ての州の生産者を清算ごとに・計器が On の旗 0 でも回った＝台帳の外の漏れ）
        if (Instruments.On) Instruments.CearnImportPayRegion(null); ts?.End(Cost[CostLedger.ClearingRows]); if (dip is not null) dip.CumulativeFxResidual += fxIntended;
        for (int i = 0; i < nn; i++)
        {
            var w0 = nations[i].World;
            w0.LastImportValue = importVal[i];
            w0.LastExportValue = exportVal[i];
            w0.TradePartnerStandardBalance = partnerTot[i] > 1e-12 ? partnerBal[i] / partnerTot[i] : 0;
            if (tar[i] < 0 && exportVal[i] > 0)
            {
                var ms = new List<Pop>(); var mw = new List<double>();
                for (int t = 0; t < tc; t++)
                    if (tr[t].nation == i && expReg[t] > 0 && tr[t].r.MerchantPop is { } merchTar)
                    { ms.Add(merchTar); mw.Add(expReg[t]); }
                w0.LastTariffRevenuePerClearing -= World.Transfer((w0, null), (w0, ms, mw), exportVal[i] / exRate[i] * Math.Abs(tar[i]) * w0.FiscalFundingRatio.Value, TreasuryCat.Tariff);
            }
        }
        if (transitUses is { Count: > 0 } && c.TransitTollShare.Value > 1e-12)
        {
            var owed = new Dictionary<(int U, int O), double>();
            var order = new List<(int U, int O)>();
            var totalByUser = new double[nn];
            foreach (var (u, o, r) in transitUses)
            {
                double amt = c.TransitTollShare.Value * (r.LastPortThroughput - (portStart.TryGetValue(r, out double ps0) ? ps0 : 0.0));
                if (amt <= 1e-12) continue;
                var k = (u, o);
                if (owed.TryGetValue(k, out double cur)) owed[k] = cur + amt; else { owed[k] = amt; order.Add(k); }
                totalByUser[u] += amt;
            }
            foreach (var k in order)
            {
                double toll = owed[k] / exRate[k.U] * nations[k.U].World.FiscalFundingRatio.Value;
                if (toll <= 1e-12) continue;
                World.Transfer((nations[k.U].World, null), (nations[k.O].World, null, null), toll, TreasuryCat.OtherOut, toCat: TreasuryCat.OtherIn);
            }
        }
        sc.Release(tc, nn, gc);
    }
}
