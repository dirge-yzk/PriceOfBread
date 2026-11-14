namespace PriceOfBread.SimCore;

public enum GoodTier { Staple, Daily, Luxury, Intermediate, Producer, Service }

public enum EducationLaw { None, Religious, Public, Compulsory }

public enum CrownForm { None, Absolutist, Despotic, Shogunate }

public enum SeizureForm { None, Bourgeois, Proletarian, Military, Anarchy }

public enum Polity
{
    Absolutist,
    Despotic,
    Shogunate,
    ConstitutionalMonarchy,
    Republic,
    BourgeoisDictatorship,
    ProletarianDictatorship,
    MilitaryDictatorship,
    Anarchy,
}

public enum PowerRank
{
    Minor,
    Middle,
    Secondary,
    Great,
}

public enum Ideology
{
    Traditionalism,
    Reactionism,
    Liberalism,
    Republicanism,
    Nationalism,
    Socialism,
    Communism,
    Anarchism,
    Imperialism,
    Fascism,
    Royalism,
}

public enum RegionState { Active, Locked }

public enum SpecialistExitLaw { Free, Restricted, Banned }

public enum TreasuryCat
{
    Tax, Tariff, StateMonopoly, Borrow, Devalue, StateFactory, TributeIn, OtherIn,
    Army, Navy, Admin, Education, Welfare, Pension, Modernization, AntiCorruption,
    Interest, DebtRepay, FactoryBuild, Subsidy, Megaproject, ColonialUpkeep, TributeOut, WarCost, OtherOut,
    Count,
}

public enum RegionTier { Core, Integrated, Colonial }

public enum SubsistenceBasisKind : byte { OpeningShare, WarmupShare, Productivity }

public enum Terrain { Plain, WaterMajor, WaterMinor, Hill, Forest, Mountain, Barren }

public enum Basin { Atlantic, Mediterranean, BlackSea, IndianOcean, Pacific, Baltic, RedSea, Caspian }
public static class Basins { public const int Count = 8; }

public sealed class Good
{
    public int Id;
    public string Name = "";
    public GoodTier Tier;

    public double BaseNeed;
    public double RefPrice;
    public double Elasticity;
    public bool IsColonial;
    public double WearRatePerMonth;
    public double Durability => 1.0 - WearRatePerMonth;
    public bool NonTradeable;
    public bool GrainChain;
    public double Nutrition;
}

public sealed class Pop
{
    public int ClassId;
    public string ClassName = "";
    public bool IsUrban;
    public double Size;
    public MoneyStock Savings { get => _savings; init => _savings = value; }
    private MoneyStock _savings;
    internal void ApplyBakedSavings(double value) => _savings = new MoneyStock(value);
    public MoneyStock Debt;
    public MoneyStock BondHoldings;
    public double Satisfaction = 1.0;
    public double Literacy;
    public double Consciousness;
    public double Militancy;
    public MoneyPerMonth LastEarnedPerMonth;
    public double[] IdeologyShares = new double[Ideologies.Count];
    public bool Closed;
    public double SubsistenceBasis;
    public SubsistenceBasisKind SubsistenceBasisKind;
    [System.NonSerialized] public double SubsistenceShare;
    public bool IsModernElite;
    public bool IsSoldier;
    public bool IsCommoner => !Closed && !IsSoldier;
    public bool IsCriminal => ClassId == PopClass.Criminal;
    public Share MilitiaShare;
    public double EducationFill = 1.0;
    public double LastEducationFill = 1.0;
    public double LivingRef = -1.0;
    public double FoodRef = -1.0;
    public double HigherEduFill;

    public double SchoolingConvStock = -1.0;
    public double HealthFill;
    public double NewsFill;
    public double RadioFill;
    public Share UnemploymentRate;
    public Share UnemploymentEma = new Share(-1.0);
    public double InitialWork = -1.0;
    public Share InitialFertileShare = new Share(-1.0);
    public double[] Ages = StdAges();
    public Share ChildLaborRate;
    public Share CoreEthnicShare = new Share(1.0);
    public double NationalConsciousness = -1;

    public double MilVoice;
    public double MilWar;
    public double MilNation;
    public double MilFamine;

    public double RemainingBudget;
    public double GoodDemand;
    public double GoodNeed;
    public double NeedMet;
    public double NeedTotal;
    public double FoodMet;
    public double FoodNeed;
    public double SupplyFill
    {
        get
        {
            double need = FoodNeed + NeedTotal;
            if (!(need > 0)) return 1.0;
            double v = (FoodMet + NeedMet) / need;
            return PoliticalQuantities.Clamp01(v);
        }
    }
    public double FoodFill => FoodNeed > 1e-9 ? PoliticalQuantities.Clamp01(FoodMet / FoodNeed) : 1.0;
    public MoneyPerMonth PaidPerMonth;
    public MoneyPerMonth EarnedPerMonth;
    public MoneyPerMonth LandRentPerMonth;
    public MoneyPerMonth LandTaxBasisAdjPerMonth;
    public MoneyPerMonth IndustrialRentPerMonth;
    public MoneyPerMonth SubsistenceValuePerMonth;

    internal static void Carry(Pop from, Pop to, MoneyStock savings, MoneyStock debt, MoneyStock bond) => Carry(from, to, savings, debt, bond, savings, debt);

    internal static void Carry(Pop from, Pop to, MoneyStock savings, MoneyStock debt, MoneyStock bond, MoneyStock savingsIn, MoneyStock debtIn)
    {
        if (PopWindow.Verify) PopWindow.GuardWrite("運搬");
        from._savings -= savings; from.Debt -= debt; from.BondHoldings -= bond;
        to._savings += savingsIn; to.Debt += debtIn; to.BondHoldings += bond;
        if (Instruments.On) { Instruments.Sav(from, Instruments.SavKind.CarryOut, -savings.Value); Instruments.Sav(to, Instruments.SavKind.CarryIn, savingsIn.Value); Instruments.CearnCarryDebt(to, debtIn.Value); }
    }

    internal static double Distribute(List<Pop> pops, double amount, List<double>? weights = null, bool income = false)
    {
        if (!(Math.Abs(amount) > 0)) return 0;
        if (pops.Count == 0) return amount;
        if (PopWindow.Verify) PopWindow.GuardWrite("按分");
        double total = 0;
        if (weights is null) foreach (var p in pops) total += p.Size;
        else foreach (var x in weights) total += x;
        if (!(total > 1e-9)) return amount;
        double done = 0;
        for (int i = 0; i < pops.Count; i++)
        {
            double share = i < pops.Count - 1 ? amount * ((weights is null ? pops[i].Size : weights[i]) / total) : amount - done;
            pops[i]._savings += new MoneyStock(share); if (income) pops[i].EarnedPerMonth += new MoneyPerMonth(share);
            if (Instruments.On) Instruments.Sav(pops[i], income ? Instruments.SavKind.GiveIncome : Instruments.SavKind.Give, share);
            done += share;
        }
        return 0;
    }

    internal static double Take(List<Pop> payers, double amount, List<double>? weights = null, List<double>? taken = null, double heldKnown = double.NaN, Coefficients? coef = null)
    {
        double moved = 0;
        if (weights is null)
        {
            double held = 0;
            if (!double.IsNaN(heldKnown) && !PopWindow.Verify) held = heldKnown;
            else
            {
                foreach (var p in payers) held += Math.Max(0, p._savings.Value);
                if (!double.IsNaN(heldKnown)) PopWindow.Check("払い手の保有の合計", heldKnown, held);
            }
            double take = Math.Min(amount, held);
            if (!(take > (coef is not null && coef.TakeFloorPositive > 0 ? 0 : 1e-12)) || !(held > 1e-9)) return 0;
            if (PopWindow.Verify) PopWindow.GuardWrite("取り立て");
            foreach (var p in payers) { double s = take * (Math.Max(0, p._savings.Value) / held); p._savings -= new MoneyStock(s); moved += s; taken?.Add(s); if (Instruments.On) Instruments.Sav(p, Instruments.SavKind.Take, -s); }
            return moved;
        }
        double tw = 0;
        foreach (var x in weights) tw += x;
        if (!(tw > 0)) return 0;
        if (PopWindow.Verify) PopWindow.GuardWrite("取り立て");
        double scale = amount / tw;
        for (int i = 0; i < payers.Count; i++)
        {
            double claim = weights[i] * scale, h = Math.Max(0, payers[i]._savings.Value);
            double s = !(claim > 0) ? 0 : claim <= h ? claim : h;
            if (s > 0) { payers[i]._savings -= new MoneyStock(s); moved += s; if (Instruments.On) Instruments.Sav(payers[i], Instruments.SavKind.Take, -s); }
            taken?.Add(s);
        }
        return moved;
    }

    internal void SettleMonth()
    {
        if (PopWindow.Verify) PopWindow.GuardWrite("月末の精算");
        _savings += Calendar.OneMonthOf(EarnedPerMonth - PaidPerMonth);
        if (Instruments.On) Instruments.Sav(this, Instruments.SavKind.Settle, Calendar.OneMonthOf(EarnedPerMonth - PaidPerMonth).Value);
    }

    internal static double Blend(Pop from, Pop to, double n, bool draft = false, double femaleAccess = double.NaN, double literacy = double.NaN,
                                 double coreAtDest = double.NaN, bool money = true, double draftLo = 20, double draftHi = 40,
                                 [System.Runtime.CompilerServices.CallerFilePath] string routeFile = "", [System.Runtime.CompilerServices.CallerLineNumber] int routeLine = 0)
    {
        if (Instruments.On) Instruments.Route(routeFile, routeLine, 'P', 'P');
        if (!(n > 1e-12) || !(from.Size > 1e-12) || ReferenceEquals(from, to)) return 0;
        if (PopWindow.Verify) PopWindow.GuardWrite("人の移動");
        if (n > from.Size) n = from.Size;
        double s0 = from.Size, t0 = to.Size, t1 = t0 + n;
        BlendAges(from, to, n, s0, t0, draft, draftLo, draftHi, femaleAccess);
        double cLit = double.IsNaN(literacy) ? from.Literacy : literacy;
        double cCore = double.IsNaN(coreAtDest) ? from.CoreEthnicShare.Value : coreAtDest;
        double rest = s0 - n;
        if (rest > 1e-12 && cLit != from.Literacy) from.Literacy = PoliticalQuantities.Clamp01((from.Literacy * s0 - cLit * n) / rest);
        to.Literacy = PoliticalQuantities.Clamp01((to.Literacy * t0 + cLit * n) / t1);
        to.Militancy = PoliticalQuantities.Clamp01((to.Militancy * t0 + from.Militancy * n) / t1);
        to.Consciousness = PoliticalQuantities.Clamp01((to.Consciousness * t0 + from.Consciousness * n) / t1);
        to.CoreEthnicShare = new Share(PoliticalQuantities.Clamp01((to.CoreEthnicShare.Value * t0 + cCore * n) / t1));
        double wTo = to.NationalConsciousness >= 0 ? t0 : 0, wFrom = from.NationalConsciousness >= 0 ? n : 0;
        if (wTo + wFrom > 1e-12)
            to.NationalConsciousness = PoliticalQuantities.Clamp01(((wTo > 0 ? to.NationalConsciousness * wTo : 0) + (wFrom > 0 ? from.NationalConsciousness * wFrom : 0)) / (wTo + wFrom));
        var ti = to.IdeologyShares; var fi = from.IdeologyShares;
        for (int i = 0; i < ti.Length && i < fi.Length; i++) ti[i] = (ti[i] * t0 + fi[i] * n) / t1;
        double frac = n / s0;
        MoneyStock sav = from.Savings * frac, debt = from.Debt * frac, bond = from.BondHoldings * frac;
        from.Size -= n; to.Size += n;
        if (Instruments.On) Instruments.UrxRtFlow(from, to, n, routeFile);
        if (money) Carry(from, to, sav, debt, bond);
        return n;
    }

    private static void BlendAges(Pop from, Pop to, double n, double s0, double t0, bool draft, double draftLo, double draftHi, double femaleAccess)
    {
        int nb = from.Ages.Length;
        if (to.Ages.Length != nb) throw new InvalidOperationException($"年齢の層の器が送り手と受け手で違う（{nb} と {to.Ages.Length}）");
        var L = AgeLayout.OfLength(nb);
        Span<double> took = stackalloc double[AgeLayout.MaxSlots]; took = took[..nb];
        bool female = !draft && !double.IsNaN(femaleAccess);
        if (!draft && !female)
            for (int k = 0; k < took.Length; k++) took[k] = from.Ages[k] * n;
        else
        {
            Span<double> cnt = stackalloc double[AgeLayout.MaxSlots]; cnt = cnt[..nb];
            Span<double> w = stackalloc double[AgeLayout.MaxSlots]; w = w[..nb];
            for (int k = 0; k < cnt.Length; k++) cnt[k] = from.Ages[k] * s0;
            for (int b = 0; b < L.Bands; b++)
            {
                if (draft) { w[2 * b] = L.Overlap(b, draftLo, draftHi); w[2 * b + 1] = 0; continue; }
                w[2 * b] = w[2 * b + 1] = 1.0;
                if (b < L.AdultFirstBand) continue;
                double t = from.Ages[2 * b] + from.Ages[2 * b + 1];
                double phi = t > 1e-300 ? from.Ages[2 * b + 1] / t : 0;
                w[2 * b + 1] = femaleAccess;
                w[2 * b] = phi < 1 ? (1.0 - phi * femaleAccess) / (1.0 - phi) : 1.0;
            }
            TakeWeighted(cnt, took, w, n);
            if (s0 - n > 1e-12) from.SetAgesFromCounts(cnt);
        }
        Span<double> dst = stackalloc double[AgeLayout.MaxSlots]; dst = dst[..nb];
        for (int k = 0; k < dst.Length; k++) dst[k] = to.Ages[k] * t0 + took[k];
        to.SetAgesFromCounts(dst);
    }

    internal void Rebase(World from, World to)
    {
        double sav0 = _savings.Value;
        _savings = World.CrossFx(from, to, _savings);
        if (Instruments.On) Instruments.Sav(this, Instruments.SavKind.Rebase, _savings.Value - sav0);
        Debt = -World.CrossFx(from, to, -Debt);
    }

    public static readonly AgeLayout Layout = AgeLayout.FromEnvironment();
    internal const double StdPyramidGrowth = 0.005;

    internal static readonly double[] StdAgesRef = StdAges();
    internal static double StdShare(double lo, double hi, int sex = -1) => AgeShareOf(StdAgesRef, lo, hi, sex);

    internal static double[] StdAges() => StdAges(Layout);
    internal static double[] StdAges(AgeLayout l)
    {
        var a = new double[2 * l.Bands];
        for (int b = 0; b < l.Bands; b++) a[2 * b] = a[2 * b + 1] = 0.5 * l.Pyramid[b];
        return a;
    }

    internal static double[] SoldierAges() => SoldierAges(Layout);
    internal static double[] SoldierAges(AgeLayout l)
    {
        var a = new double[2 * l.Bands];
        double s = l.Pyramid[l.SoldierBandA] + l.Pyramid[l.SoldierBandB];
        a[2 * l.SoldierBandA] = l.Pyramid[l.SoldierBandA] / s; a[2 * l.SoldierBandB] = l.Pyramid[l.SoldierBandB] / s;
        return a;
    }

    internal static double[] MaleAgeWeights(AgeLayout l, double lo, double hi)
    {
        var w = new double[2 * l.Bands];
        for (int b = 0; b < l.Bands; b++) w[2 * b] = l.Overlap(b, lo, hi);
        return w;
    }

    internal void SetAdultFemaleShare(double female)
    {
        var l = AgeLayout.OfLength(Ages.Length);
        for (int b = l.AdultFirstBand; b < l.Bands; b++)
        {
            double t = Ages[2 * b] + Ages[2 * b + 1];
            Ages[2 * b] = t * (1.0 - female); Ages[2 * b + 1] = t * female;
        }
    }

    public double AgeShare(double lo, double hi, int sex = -1) => AgeShareOf(Ages, lo, hi, sex);

    internal static double AgeShareOf(double[] a, double lo, double hi, int sex)
    {
        var l = AgeLayout.OfLength(a.Length);
        double s = 0;
        for (int b = 0; b < l.Bands; b++)
        {
            double ov = l.Overlap(b, lo, hi);
            if (ov > 0) s += ov * (sex < 0 ? a[2 * b] + a[2 * b + 1] : a[2 * b + sex]);
        }
        return s;
    }

    internal static double[] MortalityWeights(double crudeBirthRate) => MortalityWeights(crudeBirthRate, Layout);
    internal static double[] MortalityWeights(double crudeBirthRate, AgeLayout l)
    {
        var w = new double[2 * l.Bands];
        double inflow = crudeBirthRate;
        for (int b = 0; b < l.Bands; b++)
        {
            double outflow = b < l.Bands - 1 ? Calendar.PerMonthToPerYear(l.Outflow(b, l.Pyramid[b])) : 0.0;
            double mu = (inflow - outflow) / l.Pyramid[b] - StdPyramidGrowth;
            if (!(mu > 0)) mu = 0;
            w[2 * b] = w[2 * b + 1] = mu;
            inflow = outflow;
        }
        return w;
    }

    internal void StepAges(double size0, double births, double deaths, double[] mortW)
    {
        int nb = Ages.Length;
        Span<double> n0 = stackalloc double[AgeLayout.MaxSlots]; n0 = n0[..nb];
        Span<double> n = stackalloc double[AgeLayout.MaxSlots]; n = n[..nb];
        for (int k = 0; k < n.Length; k++) n[k] = n0[k] = Ages[k] * size0;
        if (deaths > 0)
        {
            Span<double> tmp = stackalloc double[AgeLayout.MaxSlots]; tmp = tmp[..nb];
            Span<double> took = stackalloc double[AgeLayout.MaxSlots]; took = took[..nb];
            n0.CopyTo(tmp);
            TakeWeighted(tmp, took, mortW, deaths);
            for (int k = 0; k < n.Length; k++) n[k] -= took[k];
        }
        AgeOneMonth(n0, n);
        if (births > 0) { n[0] += 0.5 * births; n[1] += 0.5 * births; }
        for (int k = 0; k < n.Length; k++) if (!(n[k] > 0)) n[k] = 0;
        SetAgesFromCounts(n);
    }

    internal void KillAges(double sizeBefore, double d, ReadOnlySpan<double> w)
    {
        int nb = Ages.Length;
        Span<double> n = stackalloc double[AgeLayout.MaxSlots]; n = n[..nb];
        Span<double> took = stackalloc double[AgeLayout.MaxSlots]; took = took[..nb];
        for (int k = 0; k < n.Length; k++) n[k] = Ages[k] * sizeBefore;
        TakeWeighted(n, took, w, d);
        SetAgesFromCounts(n);
    }

    internal static void DisasterWeights(Span<double> w, double excess)
    {
        var l = AgeLayout.OfLength(w.Length);
        for (int b = 0; b < l.Bands; b++)
        {
            double v = l.Lower[b] < 10.0 || l.Lower[b] >= 60.0 ? 1.0 + excess : 1.0;
            w[2 * b] = w[2 * b + 1] = v;
        }
    }

    internal static void AgeOneMonth(ReadOnlySpan<double> n0, Span<double> n)
    {
        var l = AgeLayout.OfLength(n.Length);
        for (int b = 0; b < l.Bands - 1; b++)
            for (int s = 0; s < 2; s++)
            {
                double o = l.Outflow(b, n0[2 * b + s]);
                n[2 * b + s] -= o; n[2 * b + 2 + s] += o;
            }
    }

    internal static void TakeWeighted(Span<double> n, Span<double> took, ReadOnlySpan<double> w, double d)
    {
        for (int iter = 0; iter <= n.Length && d > 1e-12; iter++)
        {
            double sw = 0, sn = 0;
            for (int k = 0; k < n.Length; k++) { sn += n[k]; if (!w.IsEmpty) sw += w[k] * n[k]; }
            bool uniform = w.IsEmpty || !(sw > 1e-300);
            if (uniform) sw = sn;
            if (!(sw > 1e-300)) break;
            double f = d / sw, left = 0; bool clipped = false;
            for (int k = 0; k < n.Length; k++)
            {
                double r = f * (uniform ? 1.0 : w[k]) * n[k];
                if (!(r > 0)) continue;
                if (r >= n[k]) { r = n[k]; clipped = true; }
                n[k] -= r; took[k] += r; left += r;
            }
            d -= left;
            if (!clipped) break;
        }
    }

    internal void SetAgesFromCounts(ReadOnlySpan<double> n)
    {
        double s = 0;
        for (int k = 0; k < n.Length; k++) { if (!(n[k] > 0)) continue; s += n[k]; }
        if (!(s > 1e-300)) return;
        for (int k = 0; k < n.Length; k++) Ages[k] = n[k] > 0 ? n[k] / s : 0.0;
    }

    internal double EnteringAdultsMonthly() => EnteringAdultsMonthlyOf(Ages);
    internal static double EnteringAdultsMonthlyOf(double[] a)
    {
        var l = AgeLayout.OfLength(a.Length);
        return l.Outflow(l.EntryBand, a[2 * l.EntryBand]) + l.Outflow(l.EntryBand, a[2 * l.EntryBand + 1]);
    }

    internal double GenerationalMix(double x, double xIn, int months)
    {
        double e = months * EnteringAdultsMonthly(), a = AgeShare(AgeLayout.OfLength(Ages.Length).AdultAge, double.PositiveInfinity);
        return a + e > 1e-300 ? (a * x + e * xIn) / (a + e) : x;
    }

    internal static double StdReplacementRate(int months)
    {
        double e = months * EnteringAdultsMonthlyOf(StdAgesRef), a = AgeShareOf(StdAgesRef, Layout.AdultAge, double.PositiveInfinity, -1);
        return e / (a + e);
    }
}

public readonly record struct Input(int GoodId, double QtyPerOutput);

public sealed class Producer
{
    public int GoodId;
    public double Productivity;
    public double Capacity;
    [System.NonSerialized] internal double HeadsLastMonth;
    public double LaborWeight;
    public double Output
    {
        get => _output;
        set { if (_siteRegion is { } sr) sr.GdpVersion++; _output = value; }
    }
    private double _output;
    public double DesiredOutput;
    public double MarginalCost;
    public int OwnerPopIndex;
    public int LaborPopIndex;
    public Share RentShare;
    public MoneyPerMonth? FrozenLandRentPerMonth;
    public double[]? LandShares;
    public List<Input> Inputs = [];
    public double LastInputFill = 1.0;
    public double LastMaterialFill = 1.0;
    public double Scale = 1.0;
    public double SmoothedMargin;
    public bool Bankrupt;
    public double BuiltScale
    {
        get => _builtScale;
        set
        {
            if (_siteRegion is { } sr && (value > 1e-9) != (_builtScale > 1e-9)) sr.SitesDirty = true;
            _builtScale = value;
        }
    }
    private double _builtScale;
    [System.NonSerialized] internal Region? _siteRegion;
    public Share MothballFrac;
    public int StateTarget;
    public double ConstructionLeft
    {
        get => _constructionLeft;
        set
        {
            if (_siteRegion is { } sr && ((value > 0) != (_constructionLeft > 0) || (value <= 0) != (_constructionLeft <= 0))) sr.SitesDirty = true;
            _constructionLeft = value;
        }
    }
    private double _constructionLeft;
    public double ConstructionPayPerDay;
    public Share SubsidyRate;
    public bool Subsidized => SubsidyRate.Value > 1e-9;
    public double SubsidyDependence;
    public double LastRevenue, LastCost;
    public bool BuysLabor;
    public bool IsRgo;
    public string RecipeName = "";
    public int SiteRow = -1;
    public bool SiteFound;
    public int DiscoveryRow
    {
        get
        {
            if (_discGood != GoodId || !ReferenceEquals(_discRecipe, RecipeName))
            {
                _discRow = Catalog.DiscoveryRowOf(GoodId, RecipeName);
                _retRow = Catalog.RetiredByRowOf(GoodId, RecipeName);
                _discGood = GoodId; _discRecipe = RecipeName;
            }
            return _discRow;
        }
    }
    public int RetiredByRow { get { _ = DiscoveryRow; return _retRow; } }
    public int OpenedByRow = -1;
    [System.NonSerialized] private int _discRow = -1, _retRow = -1, _discGood = int.MinValue;
    [System.NonSerialized] private string? _discRecipe;
    public double ElecFill;
    public double SteamFill;
    public int[] EffGoods = [];
    public double EffFill;
}

public sealed class Market
{
    public double Price;
    public double Stock;
    public double TargetStock;

    public double PrevStock;
    public double LastProduced
    {
        get => _lastProduced;
        set
        {
            if (_liveRegion is { } lr && (_lastDemand < DeadEps && value < DeadEps) != (_lastDemand < DeadEps && _lastProduced < DeadEps)) lr.LiveDirty = true;
            _lastProduced = value;
        }
    }
    private double _lastProduced;
    public double LastSold;
    public double LastShortage;
    public double LastImport;
    public double LastExport;
    public double PrevImport;
    public double PrevExport;
    public double LastDemand
    {
        get => _lastDemand;
        set
        {
            if (_liveRegion is { } lr && (value < DeadEps && _lastProduced < DeadEps) != (_lastDemand < DeadEps && _lastProduced < DeadEps)) lr.LiveDirty = true;
            _lastDemand = value;
        }
    }
    private double _lastDemand;
    internal const double DeadEps = 1e-9;
    [System.NonSerialized] internal Region? _liveRegion;
    internal bool FlowAsleep;
    internal double ClearFailLp;
    internal Region[]? ClearPartners;
    public double ReservationWage;
    public Share LastRation = new Share(1.0);
    public double[]? PopBought;
    public double[] SpoilLog = new double[12];
    public int SpoilIdx;
    public double SpoiledLastYear { get { double s = 0; for (int i = 0; i < 12; i++) s += SpoilLog[i]; return s; } }
}

public enum ThrottleBinding { None, Shipping, Rail, Port }

public sealed class Region
{
    public byte[]? RecipeTransitionFlags;
    public int Id;
    public string Name = "";
    public RegionState State = RegionState.Active;
    public bool IsPort;
    public double PortCapacity;
    public double LastPortThroughput;
    public string LastLandPartnerName = "";
    public double LastLandPartnerFlow;
    public double NearestPortDistanceKm;
    public string NearestPortName = "";
    public double PortLandKm;
    public string TransitPortName = "";
    public double TransitLandKm;
    public string TransitPort2Name = "";
    public double TransitLand2Km;
    public string LastTransitPortName = "";
    public Basin SeaBasin;
    public double LastWarIntensity;
    public double TariffInterest;
    public int OriginPowerId = -1;
    public double AnnexBaggage;
    public double AnchorPopulation;
    public bool NationHasPort;
    public double CrimeRate;
    public double PriceLevel = 1.0;
    public double LocalPriceMultiplier = 1.0;
    public double WagePremium = 1.0;
    public Share FarmRentCap = new Share(1.0);
    public double CrowdingPremium = 0;
    public double FinancialHeat = 0;
    public double ProductivityBonus = 1.0;
    public double ArableLand = 1e9;
    public bool CapitalEnabled;
    public double Industrialization;
    public int InvestorPopIndex = -1;
    public int MeritFromIndex = -1;
    public int MeritToIndex = -1;
    public int ClergyPopIndex = -1;
    public int TeacherPopIndex = -1;
    public int BureaucratPopIndex = -1;
    public int DoctorPopIndex = -1;
    public int CriminalPopIndex = -1;
    public int ResearcherPopIndex = -1;
    public int SoldierPopIndex = -1;
    public int MerchantPopIndex = -1;
    internal Pop? MerchantPop => MerchantPopIndex >= 0 && MerchantPopIndex < Pops.Count ? Pops[MerchantPopIndex] : null;
    public int ArtisanPopIndex = -1;
    public int ServantPopIndex = -1;
    public int EducationProducerIndex = -1;
    public double AdminFill = 1.0;
    public bool LastPortFrozen;
    public double LastTradeThrottle = 1.0;
    public double LastTradePotential;
    public double LastTransportCap;
    public bool CivilWarActive;
    public double CumPrivateIndustrialization;
    public double AdminBudget;
    public double InfraFill = 1.0;
    public double InfraBudget;
    public double SoapFill;
    public double InsuranceCoverage;
    public double MonopolyRentFactor = 1.0;
    public double WarScareTension;
    public double StockpileFactor(int goodId, Coefficients c)
    {
        if (!(c.StockpilePanicWeight > 0) || !(WarScareTension > c.RivalTensionThreshold)) return 1.0;
        double scare = PoliticalQuantities.Clamp01((WarScareTension - c.RivalTensionThreshold) / System.Math.Max(1e-9, c.StockpileTensionSpan));
        bool priority = goodId >= 0 && goodId < Catalog.Goods.Length
                        && (Catalog.Goods[goodId].Tier == GoodTier.Staple || System.Array.IndexOf(c.MilitaryGoods, goodId) >= 0);
        return 1.0 + c.StockpilePanicWeight * scare * (priority ? 1.0 : c.StockpileOtherFrac);
    }
    public double ContrabandProductionFactor = 1.0;
    public int NationalizedMask;
    public double SeizureChill;
    public double Mechanization;
    public double OldVintageCapital;
    public double DevelopmentPeak;
    public double StrandedVintage;
    public double TotalIndustrialization => Industrialization + OldVintageCapital;
    public double ProductiveIndustrialization => Industrialization + OldVintageCapital - StrandedVintage;
    public double HousingFill = 1.0;
    public double ElecInfraFill;
    public double ElecInfraBudget;
    internal double Sanitation(Coefficients c) => PoliticalQuantities.Clamp01(InfraFill + c.SoapSanitationWeight * SoapFill + c.ElecInfraSanitationWeight * ElecInfraFill);
    public double MilitaryAffordRaw = 1.0;
    public double SoldierPayBudget;
    public double CompulsoryEduFunding;
    public double EducationSpending;
    public double GridCapacity;
    public double RailTrack;
    public double LastElecWant;
    public double[]? StateMonopolyMargins;
    public double FemaleParticipation = 0.4;
    public double WomenWageRetention;
    public double ChildLaborLegal = 1.0;
    public double ColonialAccess = 1.0;
    public int[] Resources = [];
    public string Culture = "", Religion = "";
    public int[]? ColonialResources;
    public double OpiumAddiction;
    public double[]? ConstructionCarryover;
    public double SocialSecurity;
    public double HarvestFactor = 1.0;
    public double LastSubsistenceOutput;
    public double[]? CropHealth;
    public double WeatherBandFactor = 1.0;
    public double FoodFill = 1.0;
    public double MineralDepositInitial = -1.0;
    public double MineralDeposit = -1.0;
    public double MineralGradeFactor = 1.0;
    public int CulturalGoodId = -1;
    public bool[]? Discovered;
    public double MilitaryScalePeace;
    public double MilitaryWarMultLast;
    public double MilitaryScale;
    public double MilitaryBudget;
    public double NavalBudget;
    public double MilitaryBudgetCeiling;
    public double MilitaryEquipFill = 1.0;
    public Dictionary<int, double>? ArmsStock;

    public double MilitaryQualityFill = 0.0;
    public RegionTier Tier;
    public RegionTier? TierInParent;
    public Terrain Terrain;
    public bool Malarial;
    public double Dominance = 1.0;
    public double[] Rule = [1.0, 0.0, 0.0, 0.0];
    public double[] RuleInit = [1.0, 0.0, 0.0, 0.0];
    public double[] RuleStateYears = [-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];
    public int RuleStateHead;
    public int RuleStateLastYear = -1;
    public double RuleStateTenYearsAgo()
    {
        if (RuleStateYears.Length == 0) return -1;
        double v = RuleStateYears[RuleStateHead % RuleStateYears.Length];
        return v >= 0 ? v : -1;
    }
    public string[] ForeignRulers = [];
    public double[] ForeignShares = [];
    public double[]? FleetUnits;
    public double[]? NavalOrder;
    public double MilitaryTier3Fill = 0.0;
    public double MilitaryAirFill = 0.0;
    public double[] StateSpent = new double[(int)TreasuryCat.Count];
    public double CumEducationCost;
    public double CumStateMonopolyRevenue;
    public double HigherEduFunding;
    public List<Good> Goods = [];
    public List<Pop> Pops = [];
    public List<Producer> Producers = [];
    public Market[] Markets = [];

    [System.NonSerialized] internal Producer[]? BuildSites;
    [System.NonSerialized] internal Producer[]? StateSites;
    [System.NonSerialized] internal bool SitesDirty;
    [System.NonSerialized] internal long GdpVersion;
    internal void EnsureSites()
    {
        if (BuildSites is not null && !SitesDirty) return;
        int nb = 0, ns = 0;
        foreach (var p in Producers)
        {
            p._siteRegion = this;
            bool b = !(p.ConstructionLeft <= 0);
            if (b) nb++;
            if (b || p.BuiltScale > 1e-9) ns++;
        }
        var bs = nb == 0 ? System.Array.Empty<Producer>() : new Producer[nb];
        var ss = ns == 0 ? System.Array.Empty<Producer>() : new Producer[ns];
        int ib = 0, iss = 0;
        foreach (var p in Producers)
        {
            bool b = !(p.ConstructionLeft <= 0);
            if (b) bs[ib++] = p;
            if (b || p.BuiltScale > 1e-9) ss[iss++] = p;
        }
        BuildSites = bs; StateSites = ss; SitesDirty = false;
    }

    [System.NonSerialized] internal Good[]? LiveGoods;
    [System.NonSerialized] internal bool LiveDirty;
    [System.NonSerialized] internal int ClearSellerPos;
    [System.NonSerialized] internal int ClearSellerStamp;
    internal Good[] EnsureLiveGoods()
    {
        if (LiveGoods is { } lg && !LiveDirty) return lg;
        var buf = _liveBuf;
        if (buf is null || buf.Length < Goods.Count) buf = _liveBuf = new Good[Goods.Count];
        int n = 0;
        foreach (var g in Goods)
        {
            var m = Markets[g.Id];
            if (!ReferenceEquals(m._liveRegion, this)) m._liveRegion = this;
            if (!(m.LastDemand < Market.DeadEps && m.LastProduced < Market.DeadEps)) buf[n++] = g;
        }
        var old = LiveGoods;
        bool same = old is not null && old.Length == n;
        for (int i = 0; same && i < n; i++) if (!ReferenceEquals(old![i], buf[i])) same = false;
        if (!same)
        {
            var arr = n == 0 ? System.Array.Empty<Good>() : new Good[n];
            System.Array.Copy(buf, arr, n);
            LiveGoods = arr;
        }
        LiveDirty = false;
        return LiveGoods!;
    }
    [System.ThreadStatic] private static Good[]? _liveBuf;

    private Good?[]? _goodById;
    public Good? GoodOf(int id)
    {
        if (_goodById is null)
        {
            int max = -1;
            foreach (var g in Goods) if (g.Id > max) max = g.Id;
            var arr = new Good?[max + 1];
            foreach (var g in Goods) arr[g.Id] = g;
            _goodById = arr;
        }
        return (uint)id < (uint)_goodById.Length ? _goodById[id] : null;
    }

    public MoneyStock SumSavings()
    {
        MoneyStock s = default;
        foreach (var p in Pops) s += p.Savings;
        return s;
    }

    public double SumPopulation()
    {
        double s = 0;
        foreach (var p in Pops) s += p.Size;
        return s;
    }

    public double SumUrbanPopulation()
    {
        double s = 0;
        foreach (var p in Pops) if (p.IsUrban) s += p.Size;
        return s;
    }

    public double SumRuralPopulation()
    {
        double s = 0;
        foreach (var p in Pops) if (!p.IsUrban) s += p.Size;
        return s;
    }

    public Pop? PopOf(int classId)
    {
        foreach (var p in Pops) if (p.ClassId == classId && !p.IsSoldier) return p;
        return null;
    }
}
