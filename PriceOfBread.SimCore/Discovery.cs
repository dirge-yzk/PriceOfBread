namespace PriceOfBread.SimCore;

public enum DiscoveryField { Institution, Idea, Process, Weapon, Product }

public enum WarDomain { None, Land, Sea, Air }

public readonly record struct DiscoveryRow(string Name, int Good, string Recipe, DiscoveryField Field, int Decade, WarDomain Domain = WarDomain.None, int Replaces = Catalog.ReplacesOwn, string After = "", int EngineerParent = -1);

public static class Discovery
{
    public const double TermMax = 70.0, GateScore = 100.0;

    public static double Difficulty(int row, Coefficients c) => DifficultyOf(Catalog.Discoveries[row].Decade, c);
    public static double DifficultyOf(int decade, Coefficients c)
        => c.DiscoveryTechPerDecade * (decade - Catalog.DiscoveryBaseDecade) / 10.0;

    internal static double EngineerUse(Producer p)
    {
        double o = p.Output;
        if (!(o > 0)) return 0;
        foreach (var inp in p.Inputs)
            if (inp.GoodId == Catalog.AdvLaborId) return inp.QtyPerOutput > 0 ? o * inp.QtyPerOutput : 0;
        return 0;
    }

    internal static double ParentEngineers(int row, Producer rp, double[] eng, bool clockParent = false)
    {
        var d = Catalog.Discoveries[row];
        int g0 = clockParent && d.EngineerParent >= 0 ? d.EngineerParent : d.Replaces >= 0 ? d.Replaces : d.Good;
        double a = g0 >= 0 && g0 < eng.Length ? eng[g0] : 0;
        foreach (var inp in rp.Inputs)
        {
            int g = inp.GoodId;
            if (g < 0 || g >= eng.Length || g == g0 || Catalog.IsLaborGood(g)) continue;
            a += eng[g];
        }
        return a;
    }

    internal static void SeedCompatRows(World w)
    {
        if (SyntheticDyeRow >= 0 && !(w.Coef.DiscoveryEngineerHalfPerMonth > 0)) w.Discovered[SyntheticDyeRow] = true;
    }

    internal static double[] ParentEngineersByRow(World w, WorldView? view, Coefficients c)
        => ParentEngineersByRow(w, view, c, out _, out _);

    internal static double[] ParentEngineersByRow(World w, WorldView? view, Coefficients c, out Terms[] terms, out (double Replaced, double Cost)[] ratio)
    {
        var a = new double[Catalog.Discoveries.Length];
        ratio = new (double Replaced, double Cost)[Catalog.Discoveries.Length];
        terms = Score(w, view, c, includeKnown: true, null, ratio, a, out _, out _);
        return a;
    }

    internal static double EngineerFactor(int row, double a, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        if (!(a > 0)) return 0.0;
        double f = a / (a + c.DiscoveryEngineerHalfPerMonth * d);
        return PoliticalQuantities.Clamp01(f);
    }

    internal static double WorldAdvLabor(IEnumerable<World> worlds, out long visited)
    {
        double total = 0; long n = 0;
        foreach (var w in worlds)
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    n++;
                    double u = EngineerUse(p);
                    if (!(u > 0) || p.GoodId < 0 || p.GoodId >= Catalog.Goods.Length || !(Gate(r, p) > 0)) continue;
                    total += u;
                }
            }
        visited = n;
        return total;
    }

    internal static double[] EngineersByGood(World w)
    {
        var eng = new double[Catalog.Goods.Length];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= eng.Length || !(Gate(r, p) > 0)) continue;
                eng[p.GoodId] += EngineerUse(p);
            }
        }
        return eng;
    }

    internal static double ClockStage(int row, double w, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        double w0 = c.DiscoveryClockWorldPerMonth, width = c.DiscoveryClockRampWidth;
        if (!(w > 0) || !(w0 > 0) || !(width > 0)) return 0.0;
        double s = (w / (w0 * d) - 1.0) / width + 0.5;
        return PoliticalQuantities.Clamp01(s);
    }

    internal static double OwnCapacity(int row, double a, Coefficients c)
    {
        double d = (Catalog.Discoveries[row].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        if (!(d > 0)) return 1.0;
        if (!(a > 0)) return 0.0;
        double am = c.DiscoveryOwnHalfPerMonth > 0 ? c.DiscoveryOwnHalfPerMonth : 0;
        double f = a / (a + am);
        return PoliticalQuantities.Clamp01(f);
    }

    public static bool Known(bool[]? known, int row) => row >= 0 && known is not null && row < known.Length && known[row];

    public static readonly int SteamRow = Catalog.DiscoveryRowOf(Catalog.LaborId, "蒸気機関");
    public static readonly int LocomotiveRow = Catalog.DiscoveryRowOf(Catalog.LocomotiveId, "");
    public static readonly int ReeferRow = Catalog.DiscoveryRowOf(Catalog.RefrigeratorId, "");
    public static readonly int SyntheticDyeRow = Catalog.DiscoveryRowOf(Catalog.DyeId, "合成染料");
    public static bool SteamPower(bool[]? known) => Known(known, SteamRow);
    public static bool RailTransport(bool[]? known) => Known(known, LocomotiveRow);
    public static bool Refrigeration(bool[]? known) => Known(known, ReeferRow);

    public static readonly int PatentRow = Catalog.DiscoveryRowNamed("特許");
    public static readonly int JointStockRow = Catalog.DiscoveryRowNamed("株式会社");
    public static readonly int SocialismRow = Catalog.DiscoveryRowNamed("社会主義");
    public static readonly int CommunismRow = Catalog.DiscoveryRowNamed("共産主義");
    public static readonly int ImperialismRow = Catalog.DiscoveryRowNamed("帝国主義");
    public static readonly int FascismRow = Catalog.DiscoveryRowNamed("ファシズム");
    internal static double SpreadLiteracyRatio(double lit, double full)
        => !(lit > 0) ? 0.0 : !(full > 0) ? 1.0 : lit >= full ? 1.0 : lit / full;
    public static int IdeaRow(Ideology k) => k switch
    {
        Ideology.Socialism => SocialismRow, Ideology.Communism => CommunismRow,
        Ideology.Imperialism => ImperialismRow, Ideology.Fascism => FascismRow, _ => -1,
    };
    public static readonly int[] SpreadRows = Enumerable.Range(0, Catalog.Discoveries.Length).Where(r => IsSpread(Catalog.Discoveries[r].Field)).ToArray();
    private static readonly int[] SpreadSlots = Enumerable.Range(0, Catalog.Discoveries.Length).Select(r => System.Array.IndexOf(SpreadRows, r)).ToArray();
    public static int SpreadSlot(int row) => row >= 0 && row < SpreadSlots.Length ? SpreadSlots[row] : -1;
    public static bool IsSpread(DiscoveryField f) => f is DiscoveryField.Institution or DiscoveryField.Idea;

    internal static double SpreadSource(World a, int slot, double[] nat)
    {
        int row = SpreadRows[slot];
        if (Catalog.Discoveries[row].Field == DiscoveryField.Institution) return Known(a.Discovered, row) ? 1.0 : 0.0;
        int k = row == SocialismRow ? (int)Ideology.Socialism : row == CommunismRow ? (int)Ideology.Communism
              : row == ImperialismRow ? (int)Ideology.Imperialism : row == FascismRow ? (int)Ideology.Fascism : -1;
        return k >= 0 && k < nat.Length && nat[k] > 0 ? nat[k] : 0.0;
    }

    private static double SpreadNeed(int row, World w, double maxInd, double workerMil, double colonialShare, double popMil, ref double crisis, in IdeaBoard b, Coefficients c)
    {
        double s;
        if (row == PatentRow) s = w.EliteCore;
        else if (row == JointStockRow) s = maxInd;
        else if (row == SocialismRow) s = workerMil;
        else if (row == CommunismRow) s = workerMil;
        else if (row == ImperialismRow) s = colonialShare;
        else if (row == FascismRow) { if (double.IsNaN(crisis)) crisis = Ideologies.FascismCrisis(w); s = c.SpreadNeedFascismDecline > 0 ? FasStrength(crisis, popMil, b) : crisis * popMil; }
        else s = 0;
        return PoliticalQuantities.Pos(s);
    }

    internal readonly record struct IdeaBoard(double WageShare, double WageUnmet, double OwnerTake, double Decline, double WorkerMil, double ColonialShare, double PopMil, double WorkerSupplyFill);

    internal static double ComStrength(in IdeaBoard b) => b.OwnerTake * b.WageShare;
    internal static double FasStrength(double crisis, double popMil, in IdeaBoard b) => crisis * popMil * b.Decline;

    internal static double ScoreDecline(double standing, double? now)
    {
        if (now is not { } n || !(standing > 0) || !(n >= 0)) return 0.0;
        double d = (standing - n) / standing;
        return PoliticalQuantities.Clamp01(d);
    }

    internal static IdeaBoard IdeaBoardOf(World w, double? scoreNow)
    {
        double popSum = 0, wage = 0, unmet = 0, fillW = 0, rent = 0, workerEarned = 0, workerMilSum = 0, colonialPop = 0, milSum = 0, workerPop = 0;
        foreach (var r in w.Regions)
        {
            bool colonial = r.Tier == RegionTier.Colonial;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                popSum += p.Size;
                if (p.Militancy > 0) { milSum += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) workerMilSum += p.Size * p.Militancy; }
                if (colonial) colonialPop += p.Size;
                double ir = p.IndustrialRentPerMonth.Value;
                if (ir > 0) rent += ir;
                if (p.ClassId != PopClass.Worker) continue;
                workerPop += p.Size;
                double sub = p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis; sub = PoliticalQuantities.Clamp01(sub);
                double dep = p.Size * (1.0 - sub);
                double fill = p.SupplyFill;
                wage += dep; unmet += dep * (1.0 - fill); fillW += p.Size * fill;
                double e = p.EarnedPerMonth.Value;
                if (e > 0) workerEarned += e;
            }
        }
        double wageShare = popSum > 1e-12 ? wage / popSum : 0, wageUnmet = popSum > 1e-12 ? unmet / popSum : 0;
        double pie = rent + workerEarned, take = pie > 1e-12 ? rent / pie : 0;
        return new IdeaBoard(wageShare, wageUnmet, take, ScoreDecline(w.ScoreStanding, scoreNow),
            popSum > 1e-12 ? workerMilSum / popSum : 0, popSum > 1e-12 ? colonialPop / popSum : 0, popSum > 1e-12 ? milSum / popSum : 0,
            workerPop > 1e-12 ? fillW / workerPop : double.NaN);
    }

    public static double Gate(Region r, Producer p) => Gate(r, p, r.Discovered);
    public static double Gate(Region r, Producer p, bool[]? known)
    {
        if (p.SiteRow >= 0 && !p.SiteFound) return 0.0;
        int row = p.DiscoveryRow;
        double g = row >= 0 ? (Known(known, row) ? 1.0 : 0.0) : 1.0;
        if (p.OpenedByRow >= 0 && !Known(known, p.OpenedByRow)) return 0.0;
        int ret = p.RetiredByRow;
        return ret >= 0 && Known(known, ret) ? 0.0 : g;
    }

    public static double GoodGate(Region r, int gid)
    {
        double best = -1;
        foreach (var pr in r.Producers)
            if (pr.GoodId == gid && !Unfound(pr)) { double g = Gate(r, pr); if (g > best) best = g; }
        if (best < 0) { int row = Catalog.DefaultDiscoveryRowOf(gid); best = row < 0 ? 1.0 : Known(r.Discovered, row) ? 1.0 : 0.0; }
        return PoliticalQuantities.Clamp01(best);
    }

    public static void GoodGates(Region r, double[] into, int n, bool[]? known)
    {
        for (int g = 0; g < n; g++) into[g] = -1;
        foreach (var pr in r.Producers)
        {
            int g = pr.GoodId;
            if (g < 0 || g >= n || Unfound(pr)) continue;
            double x = Gate(r, pr, known);
            if (x > into[g]) into[g] = x;
        }
        for (int g = 0; g < n; g++)
        {
            double b = into[g];
            if (b < 0) { int row = Catalog.DefaultDiscoveryRowOf(g); b = row < 0 ? 1.0 : Known(known, row) ? 1.0 : 0.0; }
            into[g] = PoliticalQuantities.Clamp01(b);
        }
    }

    private static bool Unfound(Producer p) => p.SiteRow >= 0 && !p.SiteFound;

    public static void UnionInto(IEnumerable<World> worlds, bool[] into)
    {
        System.Array.Clear(into);
        foreach (var w in worlds)
        {
            var k = w.Discovered;
            for (int r = 0; r < into.Length && r < k.Length; r++) if (k[r]) into[r] = true;
        }
    }

    public sealed record WorldView(double[]? Contact, double[]? Loss = null, WorldMarket? Market = null, double? WorldAdvLabor = null, double? ScoreNow = null);

    public sealed class WorldMarket
    {
        private readonly World?[] _w;
        private readonly double[]?[] _s;
        private readonly double[][] _x;
        private readonly double[][] _wt;
        public double Freight { get; }
        public double[] Average { get; }

        internal WorldMarket(World?[] worlds, double[]?[] smoothed, double[][] inst, double[][] weight, double freight)
        {
            _w = worlds; _s = smoothed; _x = inst; _wt = weight; Freight = freight;
            int ng = Catalog.Goods.Length;
            var num = new double[ng]; var den = new double[ng];
            for (int i = 0; i < _s.Length; i++)
            {
                var s = _s[i]; var x = _x[i]; var wt = _wt[i];
                if (s is null) continue;
                for (int g = 0; g < ng && g < s.Length && g < x.Length && g < wt.Length; g++)
                {
                    if (!(x[g] >= 0) || !(s[g] >= 0) || !(wt[g] > 0)) continue;
                    num[g] += wt[g] * s[g]; den[g] += wt[g];
                }
            }
            var avg = new double[ng];
            for (int g = 0; g < ng; g++) avg[g] = den[g] > 1e-12 ? num[g] / den[g] : NoPrice;
            Average = avg;
        }

        internal static WorldMarket Flat(double[] px)
        {
            var x = new double[px.Length]; var wt = new double[px.Length];
            for (int g = 0; g < px.Length; g++) { x[g] = px[g] >= 0 ? px[g] : NoPrice; wt[g] = 1.0; }
            return new WorldMarket([null], [px], [x], [wt], 1.0);
        }

        internal double Landed(int g, World buyer, double duty, double[] km)
        {
            var c = buyer.Coef;
            double num = 0, den = 0;
            for (int i = 0; i < _s.Length; i++)
            {
                var s = _s[i];
                if (s is null || g >= s.Length || g >= _x[i].Length || g >= _wt[i].Length) continue;
                double wt = _wt[i][g];
                if (!(_x[i][g] >= 0) || !(s[g] >= 0) || !(wt > 0)) continue;
                double fac;
                if (_w[i] is { } seller)
                {
                    double d = i < km.Length ? km[i] : double.NaN;
                    if (double.IsNaN(d))
                    {
                        d = ReferenceEquals(seller, buyer) ? 0 : Geo.DistanceKm(buyer.Latitude, buyer.Longitude, seller.Latitude, seller.Longitude);
                        if (i < km.Length) km[i] = d;
                    }
                    fac = 1.0 + c.TransportMargin * (1.0 + d / Diplomacy.DistanceNormKm) * Freight + duty;
                }
                else fac = 1.0;
                num += wt * s[g] * fac; den += wt;
            }
            return den > 1e-12 ? num / den : NoPrice;
        }

        internal int Sellers => _s.Length;
    }

    public const int LossDomains = (int)WarDomain.Air + 1;

    public readonly record struct Terms(double Tech, double Need, double Transmit)
    {
        public double Score => Tech + Need + Transmit;
    }

    public static int[][] CarriersByRow(IEnumerable<World> worlds, bool downstream = false)
    {
        int nr = Catalog.Discoveries.Length;
        var rep = new Producer?[nr];
        HashSet<int>?[]? downOf = downstream ? new HashSet<int>?[Catalog.Goods.Length] : null;
        bool[]? freeUse = downstream ? new bool[Catalog.Goods.Length] : null;
        foreach (var w in worlds)
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int row = p.DiscoveryRow;
                    if (row >= 0 && rep[row] is null) rep[row] = p;
                    if (downOf is not null)
                        foreach (var inp in p.Inputs)
                        {
                            if (inp.GoodId < 0 || inp.GoodId >= downOf.Length || inp.GoodId == p.GoodId) continue;
                            if (row < 0) freeUse![inp.GoodId] = true;
                            else if (Carries(p.GoodId) && Catalog.Goods[p.GoodId].Tier == GoodTier.Producer) (downOf[inp.GoodId] ??= new HashSet<int>()).Add(p.GoodId);
                        }
                }
        var res = new int[nr][];
        var tmp = new List<int>();
        for (int row = 0; row < nr; row++)
        {
            tmp.Clear();
            int g0 = Catalog.Discoveries[row].Good;
            if (Carries(g0)) tmp.Add(g0);
            if (rep[row] is { } p)
                foreach (var inp in p.Inputs)
                    if (Carries(inp.GoodId) && Catalog.Goods[inp.GoodId].Tier == GoodTier.Producer && !tmp.Contains(inp.GoodId)) tmp.Add(inp.GoodId);
            if (downOf is not null)
            {
                int n0 = tmp.Count;
                var add = new SortedSet<int>();
                for (int i = 0; i < n0; i++)
                    if (!freeUse![tmp[i]] && downOf[tmp[i]] is { } ds) foreach (int g in ds) if (!tmp.Contains(g)) add.Add(g);
                foreach (int g in add) tmp.Add(g);
            }
            res[row] = [.. tmp];
        }
        return res;
        static bool Carries(int g) => g >= 0 && g < Catalog.Goods.Length && Catalog.Goods[g].Tier != GoodTier.Service && !Catalog.Goods[g].NonTradeable;
    }

    public static int[][] RowsByCarrier(int[][] carriers)
    {
        int ng = Catalog.Goods.Length;
        var lists = new List<int>?[ng];
        for (int row = 0; row < carriers.Length; row++)
            foreach (int g in carriers[row]) (lists[g] ??= new List<int>()).Add(row);
        var res = new int[ng][];
        for (int g = 0; g < ng; g++) res[g] = lists[g] is { } l ? [.. l] : [];
        return res;
    }

    public static Terms[] Evaluate(World w, WorldView? view, Coefficients c, bool includeKnown = false, double[]? realPrice = null, (double Replaced, double Cost)[]? ratioOut = null)
        => Score(w, view, c, includeKnown, realPrice, ratioOut);

    public const double NoPrice = -1.0;

    public static double[] InstantRealPrices(World w)
    {
        int ng = Catalog.Goods.Length;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        Aggregate(w, ng, prod, imp, dem, pxd);
        return InstantReal(w, ng, prod, imp, dem, pxd);
    }

    public static double PriceSmoothingQ(Coefficients c)
    {
        double a = PoliticalQuantities.Clamp01(c.FactoryMarginSmoothing);
        return 1.0 - (1.0 - a) * (1.0 - a) * (1.0 - a);
    }

    private static Terms[] Score(World w, WorldView? view, Coefficients c, bool includeKnown, double[]? realPrice = null, (double Replaced, double Cost)[]? ratioOut = null)
        => Score(w, view, c, includeKnown, realPrice, ratioOut, null, out _, out _);

    private static Terms[] Score(World w, WorldView? view, Coefficients c, bool includeKnown, double[]? realPrice, (double Replaced, double Cost)[]? ratioOut,
                                 double[]? engineerOut, out PriceBook? bookOut, out double[]? demOut)
    {
        bookOut = null; demOut = null;
        double[]? contact = view?.Contact;
        double[]? loss = view?.Loss;
        var market = view?.Market;
        var rows = Catalog.Discoveries;
        int nr = rows.Length, ng = Catalog.Goods.Length;
        var res = new Terms[nr];
        var refP = Catalog.RefPrices;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        var canMake = new bool[ng];
        var rep = new Producer?[nr];
        bool engOn = c.DiscoveryEngineerHalfPerMonth > 0;
        bool poolOn = engOn && c.DiscoveryClockWorldPerMonth > 0 && c.DiscoveryClockRampWidth > 0;
        double[]? eng = engOn || engineerOut is not null ? new double[ng] : null;
        if (engineerOut is not null) System.Array.Fill(engineerOut, double.NaN);
        var recipes = new List<Producer>?[ng];
        int nAct = Aggregate(w, ng, prod, imp, dem, pxd);
        double maxInd = 0;
        foreach (var r in w.Regions)
        {
            if (r.TotalIndustrialization > maxInd) maxInd = r.TotalIndustrialization;
            foreach (var p in r.Producers)
            {
                int row = p.DiscoveryRow;
                if (row >= 0 && rep[row] is null) rep[row] = p;
                if (p.GoodId < 0 || p.GoodId >= ng || !(Gate(r, p) > 0)) continue;
                canMake[p.GoodId] = true;
                if (eng is not null) eng[p.GoodId] += EngineerUse(p);
                var lr = recipes[p.GoodId] ??= new List<Producer>(2);
                bool seen = false; foreach (var q in lr) if (q.RecipeName == p.RecipeName) { seen = true; break; }
                if (!seen) lr.Add(p);
            }
        }
        if (nAct == 0) return res;
        double wClock = double.NaN;
        if (poolOn)
        {
            if (view?.WorldAdvLabor is { } wv) wClock = wv;
            else { wClock = 0; foreach (double e in eng!) wClock += e; }
        }
        double[] real;
        if (realPrice is { } rpx && rpx.Length >= ng) real = rpx;
        else real = w.DiscoveryPrice is { } dpx && dpx.Length >= ng ? dpx : InstantReal(w, ng, prod, imp, dem, pxd);
        var alive = new bool[ng];
        for (int g = 0; g < ng; g++) alive[g] = Alive(dem[g], prod[g], imp[g]);
        double plv = w.NominalOf(1.0);
        double selfScale = plv > 0 ? w.CostPriceLevel / plv : 1.0;
        if (!(selfScale > 0)) selfScale = 1.0;
        double railCost = double.NaN;
        double duty = market is null ? 0 : System.Math.Max(0, InternationalMarket.TariffRate(w, w.Coef));
        var book = new PriceBook(real, alive, market, recipes, selfScale, w, duty);
        bookOut = book; demOut = dem;
        double landPress = 0, seaPress = 0;
        if (w.SelfPower is { } sp)
        {
            landPress = Share(sp.NeedLand, ArmyShape.LandPotential(sp));
            seaPress = Share(sp.NeedSea, sp.NavalPower);
        }
        double popSum = 0, workerMilSum = 0, colonialPop = 0, milSum = 0;
        foreach (var r in w.Regions)
        {
            bool colonial = r.Tier == RegionTier.Colonial;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier || !(p.Size > 0)) continue;
                popSum += p.Size;
                if (p.Militancy > 0) { milSum += p.Size * p.Militancy; if (p.ClassId == PopClass.Worker) workerMilSum += p.Size * p.Militancy; }
                if (colonial) colonialPop += p.Size;
            }
        }
        double workerMil = popSum > 1e-12 ? workerMilSum / popSum : 0, colonialShare = popSum > 1e-12 ? colonialPop / popSum : 0, popMil = popSum > 1e-12 ? milSum / popSum : 0;
        double crisis = double.NaN;
        IdeaBoard board = c.SpreadNeedFascismDecline > 0 ? IdeaBoardOf(w, view?.ScoreNow) : default;
        double spreadLit = double.NaN;
        var reach = w.SpreadReach;
        for (int row = 0; row < nr; row++)
        {
            if (!includeKnown && Known(w.Discovered, row)) continue;
            {
                int after = Catalog.DiscoveryAfter[row];
                if (after >= 0 && !Known(w.Discovered, after)) continue;
            }
            var d = rows[row];
            bool spread = IsSpread(d.Field);
            var rp = rep[row];
            if (!spread && rp is null) continue;
            double ratio = TechRatioOf(w, row, spread, engOn, c);
            if (spread && c.SpreadLiteracyFull.Value > 0 && d.Decade > Catalog.DiscoveryBaseDecade)
            {
                if (double.IsNaN(spreadLit)) spreadLit = PoliticalQuantities.AvgLiteracy(w, coreOnly: true);
                ratio = SpreadLiteracyRatio(spreadLit, c.SpreadLiteracyFull.Value);
            }
            if (spread)
            {
                double sNeed = SpreadNeed(row, w, maxInd, workerMil, colonialShare, popMil, ref crisis, in board, c);
                double hNeed = NeedHalf(d.Field, c);
                int slot = SpreadSlot(row);
                double xr = reach is not null && slot >= 0 && slot < reach.Length ? reach[slot] : 0;
                res[row] = new Terms(TermMax * ratio, Saturate(sNeed, hNeed), Saturate(xr, c.DiscoveryContactHalf));
                continue;
            }
            double wSum = 0, wDom = 0;
            foreach (var inp in rp!.Inputs)
            {
                int g = inp.GoodId;
                if (g < 0 || g >= ng) continue;
                double wt = inp.QtyPerOutput * refP[g];
                if (!(wt > 0)) continue;
                double sup = prod[g] + (imp[g] > 0 ? imp[g] : 0);
                double dom = sup > 1e-12 ? prod[g] / sup : canMake[g] ? 1.0 : 0.0;
                wSum += wt; wDom += wt * dom;
            }
            double push = wSum > 0 ? wDom / wSum : 1.0;
            double aRow = eng is not null ? ParentEngineers(row, rp!, eng, poolOn && c.DiscoveryClockMotorParent > 0) : double.NaN;
            if (engineerOut is not null && row < engineerOut.Length) engineerOut[row] = aRow;
            double tech = poolOn && d.Decade > Catalog.DiscoveryBaseDecade ? TermMax * ClockStage(row, wClock, c) * OwnCapacity(row, aRow, c)
                                                                            : TermMax * (engOn ? EngineerFactor(row, aRow, c) : ratio) * push;
            double s = 0, half = 1;
            switch (d.Field)
            {
                case DiscoveryField.Process:
                case DiscoveryField.Product:
                    if (d.Replaces == Catalog.LandFreight && double.IsNaN(railCost)) railCost = RailCostIndex(w, book, rp!, market?.Freight ?? 1.0);
                    s = ReplacedStrength(row, rp!, book, dem, railCost, out double vRep, out double vCost);
                    if (ratioOut is not null && row < ratioOut.Length) ratioOut[row] = (vRep, vCost);
                    half = NeedHalf(d.Field, c);
                    break;
                case DiscoveryField.Weapon:
                {
                    int dom = (int)d.Domain;
                    double lossI = loss is not null && dom < loss.Length && loss[dom] > 0 ? loss[dom] : 0;
                    s = (d.Domain == WarDomain.Sea ? seaPress : landPress) + lossI;
                    half = c.DiscoveryNeedHalfWeapon;
                    break;
                }
            }
            double need = Saturate(s, half);
            double x = contact is not null && row < contact.Length ? contact[row] : 0;
            double trans = Saturate(x, c.DiscoveryContactHalf);
            res[row] = new Terms(tech, need, trans);
        }
        return res;

        static double Share(double need, double own) => need > 1e-12 ? need / (need + (own > 0 ? own : 0)) : 0;
    }

    public static double NeedHalf(DiscoveryField f, Coefficients c) => f switch
    {
        DiscoveryField.Process => c.DiscoveryNeedHalfProcess, DiscoveryField.Weapon => c.DiscoveryNeedHalfWeapon,
        DiscoveryField.Institution => c.DiscoveryNeedHalfInstitution, DiscoveryField.Idea => c.DiscoveryNeedHalfIdea,
        _ => c.DiscoveryNeedHalfProduct,
    };
    private static int Aggregate(World w, int ng, double[] prod, double[] imp, double[] dem, double[] pxd)
    {
        int nAct = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            nAct++;
            int mn = System.Math.Min(r.Markets.Length, ng);
            for (int g = 0; g < mn; g++)
            {
                var m = r.Markets[g];
                prod[g] += m.LastProduced; imp[g] += m.LastImport - m.LastExport; dem[g] += m.LastDemand;
                pxd[g] += m.Price * m.LastDemand;
            }
        }
        return nAct;
    }

    private static double[] InstantReal(World w, int ng, double[] prod, double[] imp, double[] dem, double[] pxd)
    {
        double pl = w.NominalOf(1.0);
        var x = new double[ng + 1];
        for (int g = 0; g < ng; g++)
        {
            double v = pl > 0 && Alive(dem[g], prod[g], imp[g]) ? pxd[g] / dem[g] / pl : NoPrice;
            x[g] = v >= 0 ? v : NoPrice;
        }
        x[ng] = LandFreightPrice(w);
        return x;
    }

    private static bool Alive(double dem, double prod, double imp) => dem > 1e-9 && prod + (imp > 0 ? imp : 0) > 1e-9;

    internal static (double[] Inst, double[] Sold) AdvancePrice(World w, Coefficients c)
    {
        w.Cost[CostLedger.DiscoveryPriceNations]++;
        int ng = Catalog.Goods.Length;
        var prod = new double[ng]; var imp = new double[ng]; var dem = new double[ng]; var pxd = new double[ng];
        Aggregate(w, ng, prod, imp, dem, pxd);
        var x = InstantReal(w, ng, prod, imp, dem, pxd);
        var s = w.DiscoveryPrice;
        if (s is null || s.Length != x.Length)
        {
            var s0 = (double[])x.Clone();
            if (c.DiscoveryPriceInitFromCost > 0)
            {
                var nat = NaturalPrior(w, ng);
                double a0 = PriceSmoothingQ(c);
                for (int g = 0; g < x.Length; g++)
                    if (x[g] >= 0 && nat[g] >= 0) s0[g] = nat[g] + a0 * (x[g] - nat[g]);
            }
            w.DiscoveryPrice = s0;
            return (x, prod);
        }
        double a = PriceSmoothingQ(c);
        for (int g = 0; g < x.Length; g++)
        {
            if (!(x[g] >= 0)) continue;
            s[g] = s[g] >= 0 ? PoliticalQuantities.Toward(s[g], x[g], a) : x[g];
        }
        return (x, prod);
    }

    internal static double[] NaturalPrior(World w, int ng)
    {
        var recipes = new List<Producer>?[ng];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (p.GoodId < 0 || p.GoodId >= ng || !(Gate(r, p) > 0)) continue;
                var lr = recipes[p.GoodId] ??= new List<Producer>(2);
                bool seen = false; foreach (var q in lr) if (q.RecipeName == p.RecipeName) { seen = true; break; }
                if (!seen) lr.Add(p);
            }
        }
        double plv = w.NominalOf(1.0);
        double selfScale = plv > 0 ? w.CostPriceLevel / plv : 1.0;
        if (!(selfScale > 0)) selfScale = 1.0;
        var none = new double[ng + 1]; System.Array.Fill(none, NoPrice);
        var book = new PriceBook(none, new bool[ng], null, recipes, selfScale, w, 0);
        var res = new double[ng + 1];
        for (int g = 0; g < ng; g++) { double v = book.Of(g); res[g] = v >= 0 ? v : NoPrice; }
        res[ng] = NoPrice;
        return res;
    }

    internal static WorldMarket WorldPrices(IReadOnlyList<World> worlds, IReadOnlyList<(double[] Inst, double[] Sold)> obs, double freight = 1.0)
    {
        int n = System.Math.Min(worlds.Count, obs.Count);
        var ws = new World?[n]; var ss = new double[]?[n]; var xs = new double[n][]; var wts = new double[n][];
        for (int i = 0; i < n; i++) { ws[i] = worlds[i]; ss[i] = worlds[i].DiscoveryPrice; xs[i] = obs[i].Inst; wts[i] = obs[i].Sold; }
        return new WorldMarket(ws, ss, xs, wts, freight);
    }

    private static double Saturate(double s, double half) => s > 0 && half > 0 ? TermMax * s / (s + half) : 0;

    private static double ReplacedStrength(int row, Producer rep, PriceBook book, double[] dem, double railCost, out double v, out double cost)
    {
        var d = Catalog.Discoveries[row];
        int ng = Catalog.Goods.Length;
        v = NoPrice; cost = NoPrice;
        if (d.Replaces == Catalog.LandFreight)
        {
            v = book.LandFreight;
            cost = railCost;
        }
        else
        {
            int g = d.Replaces == Catalog.ReplacesOwn ? d.Good : d.Replaces;
            if (g < 0 || g >= ng || g >= dem.Length || !(dem[g] > 1e-9)) return 0;
            double pg = book.Of(g);
            v = pg < 0 ? NoPrice : pg * ReplacedUnits[row];
            cost = book.Cost(rep);
        }
        if (!(v > 0) || !(cost > 0)) return 0;
        double ratio = v / cost;
        return ratio > 1.0 ? System.Math.Log(ratio) : 0;
    }

    private sealed class PriceBook(double[] real, bool[] alive, WorldMarket? market, List<Producer>?[] recipes, double selfScale, World buyer, double duty)
    {
        private readonly double[] _memo = new double[alive.Length];
        private double[]? _km;
        private readonly byte[] _state = new byte[alive.Length];
        public double LandFreight => alive.Length < real.Length ? real[alive.Length] : NoPrice;

        public double Of(int g)
        {
            if (g < 0 || g >= alive.Length) return NoPrice;
            if (alive[g] && g < real.Length && real[g] >= 0) return real[g];
            if (_state[g] == 2) return _memo[g];
            if (_state[g] == 1) return NoPrice;
            _state[g] = 1;
            double best = NoPrice;
            bool trade = Catalog.Goods[g].Tier != GoodTier.Service && !Catalog.Goods[g].NonTradeable;
            if (trade && market is not null)
            {
                if (_km is null) { _km = new double[market.Sellers]; System.Array.Fill(_km, double.NaN); }
                double lp = market.Landed(g, buyer, duty, _km);
                if (lp >= 0) best = lp;
            }
            if (recipes[g] is { } lr)
                foreach (var p in lr)
                {
                    double c = Cost(p);
                    if (c >= 0 && (best < 0 || c < best)) best = c;
                }
            _memo[g] = best; _state[g] = 2;
            return best;
        }

        public double Cost(Producer p)
        {
            double cost = p.MarginalCost * selfScale;
            foreach (var inp in p.Inputs)
            {
                if (!(inp.QtyPerOutput > 0)) continue;
                double px = Of(inp.GoodId);
                if (!(px >= 0)) return NoPrice;
                cost += inp.QtyPerOutput * px;
            }
            return cost >= 0 ? cost : NoPrice;
        }
    }

    public static readonly double[] ReplacedUnits = Catalog.Discoveries.Select(d =>
        d.Field == DiscoveryField.Product && d.Replaces >= 0 && d.Good >= 0 && Catalog.RefPrices[d.Replaces] > 0
            ? Catalog.RefPrices[d.Good] / Catalog.RefPrices[d.Replaces] : 1.0).ToArray();

    private static double RailCostIndex(World w, PriceBook book, Producer loco, double freight)
    {
        var c = w.Coef;
        double f = PoliticalQuantities.Clamp01(c.RailCostFraction);
        double pRail = book.Of(c.RailGoodId), pLoco = book.Cost(loco);
        if (!(pRail >= 0) || !(pLoco >= 0) || !(c.TransportCapPerUnit > 0)) return NoPrice;
        double capital = c.TransportDecay * (pRail + pLoco) / (2.0 * c.TransportCapPerUnit);
        double horseUnit = c.TransportMargin * c.LandSeaCostRatio * (freight > 0 ? freight : 1.0) / Diplomacy.DistanceNormKm;
        double pot = 0, sum = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.IsPort) continue;
            double p = r.LastTradePotential;
            if (!(p > 0)) continue;
            int ti = (int)r.Terrain;
            bool water = (r.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) || (r.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0);
            double horse = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
            double rail = ti < c.TerrainRailCost.Length ? c.TerrainRailCost[ti] : 1.0;
            double m0 = water ? (r.Terrain == Terrain.WaterMajor ? c.TerrainWaterMajorCost : c.TerrainWaterMinorCost) : horse;
            double horseMoney = horseUnit * m0 * r.PortLandKm;
            if (!(horseMoney > 0)) continue;
            double k = (water || !(horse > 0) ? f : f * rail / horse) + capital / horseMoney;
            pot += p; sum += p * k;
        }
        return pot > 1e-12 ? sum / pot : NoPrice;
    }

    internal static double LandFreightPrice(World w)
    {
        var c = w.Coef;
        double cap = c.PriceCeilFactor > 1.0 ? c.PriceCeilFactor : 1.0;
        double e = c.PriceElasticity > 0 ? 1.0 / (2.0 * c.PriceElasticity) : 1.0;
        double served = LandFreightServed(w);
        if (!(served > 0)) return cap;
        double p = System.Math.Pow(1.0 / served, e);
        double lnCap = System.Math.Log(cap);
        return MarketFlow.SoftBandLn(p, 1.0, lnCap, lnCap, c.SoftBandSharpness);
    }

    internal static double LandFreightServed(World w)
    {
        double pot = 0, served = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.IsPort) continue;
            double p = r.LastTradePotential;
            if (!(p > 0)) continue;
            double t = r.LastTradeThrottle;
            t = PoliticalQuantities.Clamp01(t);
            pot += p; served += p * t;
        }
        return pot > 1e-12 ? served / pot : 1.0;
    }

    internal static void Step(World w, WorldView? view, Coefficients c)
    {
        var known = w.Discovered;
        w.Cost[CostLedger.DiscoveryNations]++;
        if (w.DiscoveryLossQ is { } lq)
        {
            var avg = new double[lq.Length];
            for (int i = 0; i < lq.Length; i++) avg[i] = Calendar.PerQuarterToPerMonth(lq[i]);
            view = (view ?? new WorldView(null)) with { Loss = avg };
            w.DiscoveryLossQ = null;
        }
        bool insOn = Instruments.On;
        var ro = insOn ? new (double Replaced, double Cost)[Catalog.Discoveries.Length] : null;
        var eo = insOn ? new double[Catalog.Discoveries.Length] : null;
        var terms = Score(w, view, c, includeKnown: false, null, ro, eo, out var book, out var dem);
        if (insOn) Instruments.IdnObserve(w, view, c, terms);
        if (eo is not null && Calendar.QuarterIndexOfTick(w.Tick) % 8 == 0)
        {
            var all = ParentEngineersByRow(w, view, c, out var allTerms, out var allRatio);
            for (int row = 0; row < all.Length && row < known.Length; row++)
                if (!double.IsNaN(all[row])) Instruments.EngineerRow("T", w, row, all[row], known[row] ? double.NaN : terms[row].Tech, known[row], allTerms[row].Need, allRatio[row]);
        }
        if (ro is not null)
        {
            if (!known[SteamRow]) Instruments.DiscoveryTrace(w, SteamRow, terms[SteamRow], ro[SteamRow]);
            if (!known[LocomotiveRow]) Instruments.DiscoveryTrace(w, LocomotiveRow, terms[LocomotiveRow], ro[LocomotiveRow]);
        }
        for (int row = 0; row < terms.Length && row < known.Length; row++)
        {
            if (known[row] || !(terms[row].Score >= GateScore)) continue;
            known[row] = true;
            if (ro is not null) Instruments.DiscoveryEvent(w, row, terms[row], ro[row]);
            if (eo is not null && !double.IsNaN(eo[row])) Instruments.EngineerRow("E", w, row, eo[row], terms[row].Tech, false, terms[row].Need, ro![row]);
            if (eo is not null && !double.IsNaN(eo[row])) Instruments.ClockOpened(w, row, eo[row], view?.WorldAdvLabor ?? double.NaN);
            w.LogAdd($"{w.Year} {w.SelfPower?.Name ?? w.Region.Name}が{Catalog.Discoveries[row].Name}を発見");
        }
        if (book is not null && dem is not null)
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                foreach (var p in r.Producers)
                {
                    if (!Unfound(p)) continue;
                    double[]? po = insOn ? new double[3] : null;
                    var tm = SiteTerms(w, r, p, c, book, dem, po);
                    if (!(tm.Score >= GateScore)) continue;
                    if (po is not null) Instruments.SiteOpened(w, r, p, tm, po);
                    OpenSite(w, r, p, c);
                }
            }
        w.LastDiscoveryView = view;
    }

    public static Terms SiteTerms(World w, Region r, Producer p, Coefficients c, double[]? realPrice = null, WorldView? view = null, double[]? partsOut = null)
    {
        Score(w, view ?? w.LastDiscoveryView, c, includeKnown: false, realPrice, null, null, out var book, out var dem);
        if (book is null || dem is null) return default;
        return SiteTerms(w, r, p, c, book, dem, partsOut);
    }

    internal static void VisitSiteTerms(World w, Coefficients c, System.Action<Region, Producer, Terms, double[]> visit)
    {
        bool any = false;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers) if (Unfound(p)) { any = true; break; }
            if (any) break;
        }
        if (!any) return;
        Score(w, w.LastDiscoveryView, c, includeKnown: false, null, null, null, out var book, out var dem);
        if (book is null || dem is null) return;
        var parts = new double[3];
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Producers)
            {
                if (!Unfound(p)) continue;
                var t = SiteTerms(w, r, p, c, book, dem, parts);
                visit(r, p, t, parts);
            }
        }
    }

    internal static double TechRatioOf(World w, int row, bool spread, bool engOn, Coefficients c)
    {
        double ratio = 1.0;
        if (!spread && !engOn)
        {
            double diff = Difficulty(row, c);
            ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = PoliticalQuantities.Clamp01(ratio);
        }
        return ratio;
    }

    private static Terms SiteTerms(World w, Region r, Producer p, Coefficients c, PriceBook book, double[] dem, double[]? partsOut)
    {
        var site = Catalog.Sites[p.SiteRow];
        double ratio = 1.0;
        if (!(c.DiscoveryEngineerHalfPerMonth > 0))
        {
            double diff = DifficultyOf(site.Decade, c);
            ratio = !(diff > 0) ? 1.0 : w.TechLevel / diff;
            ratio = PoliticalQuantities.Clamp01(ratio);
        }
        double people = 0;
        foreach (var q in r.Pops)
        {
            if (q.IsSoldier || !(q.Size > 0)) continue;
            people += site.Kind == Catalog.SiteKind.Deposit ? q.Size * PoliticalQuantities.Clamp01(q.CoreEthnicShare.Value) : q.Size;
        }
        double n0 = c.DiscoverySiteFindHalfPop > 0 ? c.DiscoverySiteFindHalfPop : 0;
        double thick = people > 0 ? people / (people + n0) : 0;
        int g = site.Good;
        double v = NoPrice, cost = NoPrice, s = 0;
        bool used = g >= 0 && g < dem.Length && dem[g] > 1e-9;
        if (used || partsOut is not null)
        {
            v = book.Of(g);
            cost = book.Cost(p);
        }
        if (used)
        {
            double x = v > 0 && cost > 0 ? v / cost : 0;
            s = x > 1.0 ? System.Math.Log(x) : 0;
        }
        if (partsOut is { Length: >= 3 }) { partsOut[0] = people; partsOut[1] = v; partsOut[2] = cost; }
        return new Terms(TermMax * ratio * thick, Saturate(s, c.DiscoveryNeedHalfSite), 0);
    }


    internal static void OpenSite(World w, Region r, Producer p, Coefficients c)
    {
        var site = Catalog.Sites[p.SiteRow];
        p.SiteFound = true;
        string gname = Catalog.Goods[site.Good].Name, who = w.SelfPower?.Name ?? w.Region.Name;
        double siteCostDiv = System.Array.IndexOf(r.Resources, p.GoodId) < 0 ? Simulation.ResourceEndowment(p.GoodId) * c.ResourceEndowmentScale : 1.0;
        if (site.Kind == Catalog.SiteKind.Deposit)
        {
            Simulation.ApplyEndowment(p, c.GoldRushEndowment, siteCostDiv, c);
            if (p.Scale < 1.0) p.Scale = 1.0;
            if (!(c.DepositSitesOnly > 0) && r.MineralDepositInitial > 0)   // (1202) 旗 1＝補充は産地の生産者の捕捉へ置き換える（Simulation.StepDeposits）
                r.MineralDeposit += c.BonanzaDepositYears * r.MineralDepositInitial / System.Math.Max(1e-9, c.DepositYearsAtBirth);
            w.LogAdd($"{w.Year} {r.Name}で{gname}の大鉱脈が見つかった（{who}）");
        }
        else { Simulation.DivideCostByEndowment(p, siteCostDiv, c); w.LogAdd($"{w.Year} {r.Name}で{gname}の植林が始まった（{who}）"); }
    }
}
