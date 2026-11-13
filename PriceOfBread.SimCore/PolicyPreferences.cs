namespace PriceOfBread.SimCore;

public enum PolicyAxis
{
    IncomeTax, SocialSecurity, Coercion, Tariff, EducationSpending,
    PollTax, LandTax, ProgressiveTax,
    EducationLaw, Suffrage, Censorship, LaborUnion, ShorterHours,
    WomenRights,
    CornLaws, LandReform, SerfEmancipation,
    ConscriptionLaw,
    SecretBallot,
    Bureaucracy,
    StateReligion,
    Antitrust,
    WartimeRationing,
    Nationalization,
    NavyUpkeep,
    ChildLabor,
    MilitaryExport,
    OpiumPossession,
    OpiumExport,
    ImmigrationRestriction,
    MonetaryStandard,
    CapitalMobility,
    NationalizationAll,
    ColonialSuffrage,
    MigrantNativeSuffrage,
    SpecialistExitBan,
    Guild,
    CivilianArms,
    MinimumWage,
    Inheritance,
}

public static class PolicyPreferences
{
    private const double BurdenScale = 3.0;
    internal const double StatusScale = 2.0;
    private const double ProgressiveCapitalSurcharge = 1.5;
    private const double NationalizationCapitalOpposition = 1.5;
    private const double LandTaxRentierThreshold = 0.34;
    internal const int BlocCount = 3;

    internal readonly struct Profile
    {
        public readonly double[] Power;
        public readonly double[] Wealth;
        public readonly double[] Rent;
        public readonly double[] Income;
        public readonly double[] Poll;
        public readonly double[] Old;
        public readonly double StateRevenue;
        public readonly double NominalGdp;
        public readonly double LordRentToIncome;
        public readonly double[] LandBase;
        public readonly double ClergyToIncome0;
        public readonly double CutRhoToIncome0;
        public Profile(double[] pw, double[] we, double[] re, double[] inc, double[] po, double[] old, double sr, double nominalGdp, double lordRentToIncome, double[] landBase, double clergyToIncome0 = 0.0, double cutRhoToIncome0 = 0.0)
        { Power = pw; Wealth = we; Rent = re; Income = inc; Poll = po; Old = old; StateRevenue = sr; NominalGdp = nominalGdp; LordRentToIncome = lordRentToIncome; LandBase = landBase; ClergyToIncome0 = clergyToIncome0; CutRhoToIncome0 = cutRhoToIncome0; }
    }

    internal static double AvgUnemployment(World w)
    {
        double u = 0, size = 0;
        foreach (var p in w.AllPops()) if (p.IsCommoner) { u += p.UnemploymentRate.Value * p.Size; size += p.Size; }
        return size > 1e-9 ? u / size : 0;
    }

    internal static int BlocOf(Pop p) => p.Closed ? 0 : p.IsModernElite ? 1 : 2;

    internal static Profile Build(World w)
    {
        var (t, m, s) = InterestGroups.Blocs(w);
        double[] power = [t, m, s];
        var sums = w.PopSums is { } win ? win.BuildSums(w) : ComputeBuildSums(w);
        double[] wealth = sums.Wealth, rent = sums.Rent, income = sums.Income, poll = sums.Poll, old = sums.Old, blocPop = sums.BlocPop;
        double tw = sums.Tw, tr = sums.Tr, ti = sums.Ti, tp = sums.Tp, clergyInc0 = sums.ClergyInc0;
        double lordRentToIncome = RentToIncomeOf(rent[0], income[0]);
        double clergyToIncome0 = RentToIncomeOf(clergyInc0, income[0]);
        double cutRhoToIncome0 = w.Coef.LordIntermediaryStake > 0 ? RentToIncomeOf(w.LastRuleCut, income[0]) * IntermediaryKeepShare(w) : 0.0;
        Normalize(wealth, tw); Normalize(rent, tr); Normalize(income, ti); Normalize(poll, tp);
        double[] landBase = sums.LandBase; Normalize(landBase, sums.Tl);
        for (int i = 0; i < BlocCount; i++) old[i] = blocPop[i] > 1e-9 ? old[i] / blocPop[i] : 0;
        double ngdp = w.NominalOf(w.GdpPerMonth.Value);
        double stress = w.FiscalEnabled
            ? PoliticalQuantities.Clamp01((1.0 - w.FiscalFundingRatio.Value) + (ngdp > 1e-9 ? System.Math.Min(1.0, Calendar.ToGdpYear(w.GovDebt, Calendar.PerMonthToPerYear(new MoneyPerMonth(ngdp))).Value) : 0))
            : 0;
        return new Profile(power, wealth, rent, income, poll, old, 0.5 + stress, ngdp, lordRentToIncome, landBase, clergyToIncome0, cutRhoToIncome0);
    }

    internal static double IntermediaryKeepShare(World w)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Rule.Length != 4 || r.RuleInit.Length != 4) continue;
            double pop = r.SumPopulation(); if (!(pop > 0)) continue;
            double f = r.AdminFill > 0 ? (r.AdminFill < 1 ? r.AdminFill : 1.0) : 0.0;
            num += pop * r.Rule[1] * f; den += pop * (r.Rule[1] + r.Rule[0] * (1.0 - f));
        }
        return den > 0 ? num / den : 0.0;
    }

    internal sealed class BuildSums
    {
        internal double[] Wealth = new double[BlocCount], Rent = new double[BlocCount], Income = new double[BlocCount], Poll = new double[BlocCount];
        internal double[] Old = new double[BlocCount], BlocPop = new double[BlocCount];
        internal double[] LandBase = new double[BlocCount];
        internal double Tw, Tr, Ti, Tp, Tl;
        internal double ClergyInc0;
        internal BuildSums Copy() => new()
        {
            Wealth = (double[])Wealth.Clone(), Rent = (double[])Rent.Clone(), Income = (double[])Income.Clone(), Poll = (double[])Poll.Clone(),
            Old = (double[])Old.Clone(), BlocPop = (double[])BlocPop.Clone(), Tw = Tw, Tr = Tr, Ti = Ti, Tp = Tp,
            LandBase = (double[])LandBase.Clone(), Tl = Tl, ClergyInc0 = ClergyInc0,
        };
        internal void CheckSame(BuildSums f)
        {
            PopWindow.Check("立場の土台の合計(富)", Tw, f.Tw); PopWindow.Check("立場の土台の合計(地代)", Tr, f.Tr);
            PopWindow.Check("立場の土台の合計(所得)", Ti, f.Ti); PopWindow.Check("立場の土台の合計(人頭税)", Tp, f.Tp);
            PopWindow.Check("立場の土台の合計(土地税の基数)", Tl, f.Tl);
            PopWindow.Check("立場の土台(聖職者の所得)", ClergyInc0, f.ClergyInc0);
            for (int i = 0; i < BlocCount; i++)
            {
                PopWindow.Check("立場の土台(富)", Wealth[i], f.Wealth[i]); PopWindow.Check("立場の土台(地代)", Rent[i], f.Rent[i]);
                PopWindow.Check("立場の土台(所得)", Income[i], f.Income[i]); PopWindow.Check("立場の土台(人頭税)", Poll[i], f.Poll[i]);
                PopWindow.Check("立場の土台(高齢)", Old[i], f.Old[i]); PopWindow.Check("立場の土台(ブロックの人口)", BlocPop[i], f.BlocPop[i]);
                PopWindow.Check("立場の土台(土地税の基数)", LandBase[i], f.LandBase[i]);
            }
        }
    }

    internal static BuildSums ComputeBuildSums(World w)
    {
        var o = new BuildSums();
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            int b = BlocOf(p);
            double sv = System.Math.Max(0, p.Savings.Value);
            o.Wealth[b] += sv; o.Tw += sv;
            double rn = System.Math.Max(0, p.LandRentPerMonth.Value); o.Rent[b] += rn; o.Tr += rn;
            double lb = w.Coef.LandTaxFixedBasis > 0 ? System.Math.Max(0, Fiscal.LandTaxBase(w.Coef, p).Value) : rn; o.LandBase[b] += lb; o.Tl += lb;
            double inc = System.Math.Max(0, p.EarnedPerMonth.Value); o.Income[b] += inc; o.Ti += inc;
            if (b == 0 && p.ClassId == PopClass.Clergy) o.ClergyInc0 += inc;
            if (p.IsCommoner) { o.Poll[b] += p.Size; o.Tp += p.Size; }
            { o.Old[b] += p.Size * p.AgeShare(60, double.PositiveInfinity); o.BlocPop[b] += p.Size; }
        }
        return o;
    }

    internal static double RentToIncomeOf(double rent, double income) => income > 1e-9 ? rent / income : 0.0;

    internal static double CustomaryRentShare(World w)
    {
        if (w.Regions.Count == 0) return 0.0;
        double c = double.PositiveInfinity;
        foreach (var pr in w.Regions[0].Producers) if (pr.IsRgo && pr.RentShare.Value < c) c = pr.RentShare.Value;
        return double.IsPositiveInfinity(c) ? 0.0 : c;
    }

    internal static double LordIncomeLoss(World w, PolicyAxis axis, double lordRentToIncome)
    {
        EffectiveRentShares(w, axis, out double now, out double next);
        return now > 1e-9 ? (now - next) / now * lordRentToIncome : 0.0;
    }

    internal static void EffectiveRentShares(World w, PolicyAxis axis, out double now, out double other, bool back = false)
    {
        var c = w.Coef;
        var lr = w.LandReform; var se = w.SerfEmancipation;
        var lrOther = axis == PolicyAxis.LandReform ? Step(lr, back) : lr;
        var seOther = axis == PolicyAxis.SerfEmancipation ? Step(se, back) : se;
        double custom = CustomaryRentShare(w);
        now = System.Math.Min(custom, System.Math.Min(Simulation.RentCap(lr, c.LandReformRentCap.Value), Simulation.RentCap(se, c.SerfEmancipationRentCap.Value)));
        other = System.Math.Min(custom, System.Math.Min(Simulation.RentCap(lrOther, c.LandReformRentCap.Value), Simulation.RentCap(seOther, c.SerfEmancipationRentCap.Value)));
    }

    private static ReformLevel Step(ReformLevel lv, bool back) => back ? (lv == ReformLevel.None ? ReformLevel.None : lv - 1) : (lv == ReformLevel.Full ? ReformLevel.Full : lv + 1);

    internal static double InheritanceStepSize(World w)
    {
        int cur = (int)w.Inheritance, top = (int)InheritanceLaw.All;
        return cur < top ? Government.InheritanceRate((InheritanceLaw)(cur + 1)) - Government.InheritanceRate((InheritanceLaw)cur)
                         : Government.InheritanceRate(InheritanceLaw.All) - Government.InheritanceRate(InheritanceLaw.High);
    }

    private static void Normalize(double[] a, double total)
    { if (total > 1e-9) for (int i = 0; i < BlocCount; i++) a[i] /= total; }

    internal static double Pref(PolicyAxis a, int b, in Profile e, World w, bool increasing = true)
    {
        bool trad = b == 0, modern = b == 1, mass = b == 2;
        switch (a)
        {
            case PolicyAxis.IncomeTax:
                return (mass ? 1.0 : 0) - BurdenScale * e.Wealth[b];
            case PolicyAxis.ProgressiveTax:
                return (mass ? 1.0 : 0) - (BurdenScale + (modern ? ProgressiveCapitalSurcharge : 0)) * e.Wealth[b];
            case PolicyAxis.LandTax:
                return (e.LandBase[b] < LandTaxRentierThreshold ? 1.0 : 0) - (BurdenScale + 1.0) * e.LandBase[b];
            case PolicyAxis.PollTax:
                return (e.Poll[b] < 1e-6 ? 1.0 : 0) - BurdenScale * e.Poll[b];
            case PolicyAxis.SocialSecurity:
                return (mass ? 1.0 : 0) - 2.0 * e.Wealth[b] + w.Coef.PensionOldWeight * (e.Old[b] - Pop.StdShare(60, double.PositiveInfinity));
            case PolicyAxis.EducationSpending:
                return trad ? -1.0 : 1.0;
            case PolicyAxis.Tariff:
            {
                double tariffExtra = 0;
                if (modern && w.Coef.TariffInterestPrefWeight > 0)
                {
                    double tiSum = 0; int tiN = 0;
                    foreach (var rg in w.Regions) if (rg.Id != 0) { tiSum += rg.TariffInterest; tiN++; }
                    if (tiN > 0) tariffExtra += w.Coef.TariffInterestPrefWeight * (tiSum / tiN);
                }
                if (w.Coef.TariffStateRevenueWeight > 0) tariffExtra += w.Coef.TariffStateRevenueWeight * e.StateRevenue;
                if (modern && w.Coef.TariffExportOppositionWeight > 0 && e.NominalGdp > 1e-9)
                {
                    double tradeBase = w.LastExportValue;
                    if (w.ImportsByGood is { } impC)
                        for (int gC = 0; gC < impC.Length && gC < Catalog.Goods.Length; gC++)
                            if (Catalog.Goods[gC].Tier is GoodTier.Producer or GoodTier.Intermediate) tradeBase += impC[gC];
                    tariffExtra -= w.Coef.TariffExportOppositionWeight * PoliticalQuantities.Clamp01(tradeBase / e.NominalGdp);
                }
                return w.Coef.RentProtectionWeightCoef * e.Rent[b] - (mass ? 1.0 : 0) + tariffExtra;
            }
            case PolicyAxis.CornLaws:
                return w.Coef.RentProtectionWeightCoef * e.Rent[b] - (trad ? 0 : 1.0);
            case PolicyAxis.LaborUnion:
                double unionConc = w.Coef.LaborMilitancyConcession * System.Math.Max(0, Politics.AvgMassMilitancy(w) - w.Coef.RiotMilitancyThreshold);
                return mass ? 1.0 : -0.5 + unionConc;
            case PolicyAxis.ShorterHours:
            case PolicyAxis.ChildLabor:
                double hoursConc = w.Coef.LaborMilitancyConcession * System.Math.Max(0, Politics.AvgMassMilitancy(w) - w.Coef.RiotMilitancyThreshold);
                return mass ? -1.0 : modern ? 1.0 - hoursConc : 0;
            case PolicyAxis.Coercion:
            case PolicyAxis.ConscriptionLaw:
                return trad ? 1.0 : mass ? -1.0 : 0;
            case PolicyAxis.NavyUpkeep:
                return trad ? 0.5 : 0;

            case PolicyAxis.EducationLaw:
            case PolicyAxis.WomenRights:
                return trad ? -StatusScale * (0.5 + e.Wealth[0]) : 1.0;
            case PolicyAxis.SerfEmancipation:
                return trad ? -StatusScale * (0.5 + LordStake(w, a, e, increasing)) : 1.0;
            case PolicyAxis.LandReform:
                if (trad && w.Coef.LordIntermediaryStake > 0) return -StatusScale * (0.5 + LordStake(w, a, e, increasing) + IntermediaryStake(w, a, e, increasing));
                return trad ? -StatusScale * (0.5 + LordStake(w, a, e, increasing)) : mass ? 2.0 : 1.0;
            case PolicyAxis.Censorship:
            case PolicyAxis.SecretBallot:
                return trad ? -1.0 : 1.0;
            case PolicyAxis.Bureaucracy:
                if (trad && w.Coef.LordIntermediaryStake > 0) return -StatusScale * (0.5 + IntermediaryStake(w, a, e, increasing));
                return trad ? -1.0 : 1.0;
            case PolicyAxis.StateReligion:
                if (trad && w.Coef.LordReligionStake > 0) return -StatusScale * (0.5 + ReligionStake(w, e, increasing));
                return trad ? -1.0 : 1.0;
            case PolicyAxis.Antitrust:
                return mass ? 1.0 : -1.0;
            case PolicyAxis.Guild:
                return w.Coef.GuildLawGate > 0 ? (trad ? 0.0 : 1.0) : 0.0;
            case PolicyAxis.CivilianArms:
                return w.Coef.RebelArmsMarketEnabled > 0 ? (trad ? 1.0 : -1.0) : 0.0;
            case PolicyAxis.MinimumWage:
                return mass ? 1.0 : modern ? -1.0 : -0.5;
            case PolicyAxis.Inheritance:
                return (mass ? 1.0 : 0.0) - BurdenScale * InheritanceStepSize(w) * e.Wealth[b];
            case PolicyAxis.WartimeRationing:
                return trad ? 0.5 : modern ? -0.5 : 0;
            case PolicyAxis.MilitaryExport:
                return modern ? -w.Coef.MilitaryExportPrefScale * MilitaryOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.OpiumPossession:
                return mass ? 1.0 + w.Coef.OpiumBanPrefScale * NationalOpiumAddiction(w)
                     : modern ? -w.Coef.OpiumTradePrefScale * OpiumOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.OpiumExport:
                return modern ? -w.Coef.OpiumTradePrefScale * OpiumOutputShare(w, e.NominalGdp) : 0;
            case PolicyAxis.ImmigrationRestriction:
                if (w.TotalPopulation <= 1e-9) return 0;
                double inflowRate = Math.Max(0, w.LastImmigration) / w.TotalPopulation;
                double pressure = w.Coef.ImmigrationRestrictionPrefScale * inflowRate;
                return mass ? pressure * (1.0 + AvgUnemployment(w)) : modern ? -pressure : 0;
            case PolicyAxis.SpecialistExitBan:
                if (w.TotalPopulation <= 1e-9 || !modern) return 0;
                double exitRate = Math.Max(0, w.LastSpecialistExit) / w.TotalPopulation;
                double banPressure = w.Coef.SpecialistExitBanPrefScale * exitRate;
                if (!(banPressure > 0)) return 0;
                double capW = 0, proW = 0;
                foreach (var rr in w.Regions)
                    foreach (var q in rr.Pops)
                    {
                        double wealth = Math.Max(0, q.Savings.Value);
                        if (q.ClassId == PopClass.Capitalist) capW += wealth;
                        else if (q.ClassId == PopClass.Engineer || q.ClassId == PopClass.Researcher) proW += wealth;
                    }
                double bloc = capW + proW;
                return bloc > 1e-9 ? banPressure * (capW - proW) / bloc : 0;
            case PolicyAxis.MonetaryStandard:
                double silverDrop = 0;
                if (w.MonetaryLaw == MonetaryStandardLaw.Silver)
                {
                    silverDrop = PoliticalQuantities.Clamp01(1.0 - w.SilverGoldRatio);
                    if (!(silverDrop >= 0 && silverDrop <= 1)) silverDrop = 0;
                }
                double peerPull = 0;
                if (w.Coef.MonetaryPeerPullScale > 0 && w.MonetaryLaw == MonetaryStandardLaw.Silver)
                {
                    peerPull = w.Coef.MonetaryPeerPullScale * Math.Abs(1.0 - w.SilverGoldRatio) * w.TradeGoldPull;
                    if (!(peerPull >= -w.Coef.MonetaryPeerPullScale && peerPull <= w.Coef.MonetaryPeerPullScale)) peerPull = 0;
                }
                return w.Coef.MonetaryPrefScale
                     * (e.Wealth[b] * (Math.Max(0, w.ExpectedInflationPerYear.Value) + silverDrop + peerPull)
                      - (1.0 - e.Wealth[b]) * (Math.Max(0, (-w.ExpectedInflationPerYear).Value) + silverDrop));
            case PolicyAxis.CapitalMobility:
                double abroadShare = e.NominalGdp > 1e-9 ? PoliticalQuantities.Clamp01(w.CapitalAbroad.Value / e.NominalGdp) : 0;
                if (!(abroadShare >= 0 && abroadShare <= 1)) abroadShare = 0;
                return modern ? -(1.0 + w.Coef.CapitalControlPrefScale * abroadShare) : 1.0;
            case PolicyAxis.Nationalization:
            case PolicyAxis.NationalizationAll:
                return modern ? -NationalizationCapitalOpposition : mass ? 1.0 : 0;
            case PolicyAxis.ColonialSuffrage:
                goto case PolicyAxis.Suffrage;
            case PolicyAxis.MigrantNativeSuffrage:
                return trad ? -1.0 : modern ? 1.0 : 0;
            case PolicyAxis.Suffrage:
                double threat = Politics.AvgMassMilitancy(w) * w.Coef.SuffrageConcession;
                return trad ? -1.0 + threat
                     : modern ? (w.Suffrage < Suffrage.Census ? 1.0 : -1.0 + threat)
                     : 1.0;
            default: return 0;
        }
    }

    internal static bool HasLegislature(Polity p) => p is Polity.ConstitutionalMonarchy or Polity.Republic;

    internal static double ParliamentNet(World w, PolicyAxis a, double sign, in Profile e)
    {
        if (!w.ParliamentEnabled || !HasLegislature(w.Polity)) return 0;
        double sum = 0;
        for (int party = 0; party < BlocCount; party++) sum += w.PartySeats[party] * Pref(a, party, e, w, sign > 0) * sign;
        return w.Coef.ParliamentLawWeight * sum;
    }

    internal static double OpposingPower(World w, PolicyAxis a, bool increasing, in Profile e)
    {
        double sign = increasing ? 1 : -1;
        double sum = 0;
        for (int b = 0; b < BlocCount; b++)
        {
            double pr = Pref(a, b, e, w, increasing) * sign;
            if (pr < 0) sum += e.Power[b] * -pr;
        }
        if (w.ParliamentEnabled && HasLegislature(w.Polity))
            for (int party = 0; party < BlocCount; party++)
            {
                double pr = Pref(a, party, e, w, increasing) * sign;
                if (pr < 0) sum += w.Coef.ParliamentLawWeight * w.PartySeats[party] * -pr;
            }
        double armyPr = ArmyStake(a) * sign;
        if (armyPr < 0) sum += ArmyPower(w) * -armyPr;
        return sum;
    }

    private static double MilitaryOutputShare(World w, double gdp)
    {
        if (gdp <= 1e-9) return 0;
        double v = 0;
        foreach (var r in w.Regions)
        {
            foreach (int g in w.Coef.MilitaryExportGoods)
                if (g >= 0 && g < r.Markets.Length) v += r.Markets[g].LastProduced * Catalog.RefPrices[g];
        }
        return PoliticalQuantities.Clamp01(v / gdp);
    }

    private static double OpiumOutputShare(World w, double gdp)
    {
        int g = w.Coef.OpiumGoodId;
        if (gdp <= 1e-9 || g < 0) return 0;
        double v = 0;
        foreach (var r in w.Regions)
        {
            if (g >= r.Markets.Length) continue;
            v += r.Markets[g].LastProduced * Catalog.RefPrices[g];
        }
        return PoliticalQuantities.Clamp01(v / gdp);
    }

    private static double NationalOpiumAddiction(World w)
    {
        double add = 0, size = 0;
        foreach (var r in w.Regions)
        {
            double rp = r.SumPopulation();
            add += r.OpiumAddiction * rp; size += rp;
        }
        return size > 1e-9 ? add / size : 0;
    }


    internal static double ArmyStake(PolicyAxis a)
        => a is PolicyAxis.Coercion or PolicyAxis.ConscriptionLaw ? 1.0 : 0.0;

    internal static double ArmyPower(World w)
        => w.Coef.ArmyPoliticalWeight <= 0 ? 0
         : w.Coef.ArmyPoliticalWeight * (w.SoldierSizeK() / System.Math.Max(1.0, w.TotalPopulation))
           * (1.0 - PoliticalQuantities.Clamp01(w.ParliamentControl));

    public static (double Trad, double Modern, double Mass, double Net) Stance(World w, PolicyAxis a, bool increasing)
    {
        var e = Build(w);
        double sign = increasing ? 1 : -1;
        return (Pref(a, 0, e, w, increasing) * sign, Pref(a, 1, e, w, increasing) * sign, Pref(a, 2, e, w, increasing) * sign, NetSupport(w, a, increasing, e));
    }


    public static (string Who, double Weight)[] SupportBreakdown(World w, PolicyAxis a, bool increasing)
    {
        var e = Build(w);
        double sign = increasing ? 1 : -1;
        string[] blocNames = ["旧支配層", "近代エリート", "大衆"];
        var list = new List<(string, double)>(BlocCount + 3);
        double streetB = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;
        for (int b = 0; b < BlocCount; b++) list.Add((blocNames[b], streetB * e.Power[b] * Pref(a, b, e, w, increasing) * sign));
        if (w.ParliamentEnabled && HasLegislature(w.Polity))
        {
            string[] partyNames = ["議会・保守党", "議会・自由党", "議会・社会党"];
            for (int party = 0; party < BlocCount; party++)
            {
                double v = w.Coef.ParliamentLawWeight * w.PartySeats[party] * Pref(a, party, e, w, increasing) * sign;
                if (System.Math.Abs(v) > 1e-9) list.Add((partyNames[party], v));
            }
        }
        double army = ArmyPower(w) * ArmyStake(a) * sign;
        if (System.Math.Abs(army) > 1e-9) list.Add(("軍", army));
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax)
            list.Add(("国庫", e.StateRevenue * sign));
        list.Sort((x, y) => y.Item2.CompareTo(x.Item2));
        return [.. list];
    }

    public static double NetSupport(World w, PolicyAxis a, bool increasing) => NetSupport(w, a, increasing, Build(w));

    internal static double NetSupport(World w, PolicyAxis a, bool increasing, in Profile e)
    {
        double sign = increasing ? 1 : -1;
        double sum = 0;
        double street = w.Coef.ParliamentStreetSupersede > 0 && w.ParliamentEnabled && HasLegislature(w.Polity)
            ? 1.0 - PoliticalQuantities.Clamp01(w.Coef.ParliamentStreetSupersede) : 1.0;
        for (int b = 0; b < BlocCount; b++) sum += street * e.Power[b] * Pref(a, b, e, w, increasing) * sign;
        sum += ParliamentNet(w, a, sign, e);
        if (a is PolicyAxis.IncomeTax or PolicyAxis.PollTax or PolicyAxis.LandTax)
            sum += e.StateRevenue * sign;
        sum += ArmyPower(w) * ArmyStake(a) * sign;
        return sum;
    }

    internal static double[] LawGap(World w)
    {
        var e = Build(w);
        double[] gap = new double[BlocCount], wsum = new double[BlocCount];
        foreach (var a in FactionSnapshot.Axes)
        {
            if (a == PolicyAxis.NationalizationAll) continue;
            for (int b = 0; b < BlocCount; b++)
            {
                double pref = Pref(a, b, e, w);
                double s = System.Math.Abs(pref);
                if (!(s > 0)) continue;
                wsum[b] += s;
                gap[b] += s * FactionSnapshot.Headroom(w, a, pref >= 0);
            }
        }
        for (int b = 0; b < BlocCount; b++)
        {
            double v = wsum[b] > 1e-9 ? gap[b] / wsum[b] : 0;
            gap[b] = PoliticalQuantities.Clamp01(v);
        }
        return gap;
    }


    private static double LordStake(World w, PolicyAxis a, in Profile e, bool increasing)
        => !increasing ? LordIncomeGain(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome);

    internal static bool DirectionAware(World w, PolicyAxis a, int b)
        => b == 0 && (a == PolicyAxis.SerfEmancipation || a == PolicyAxis.LandReform
                      || (a == PolicyAxis.Bureaucracy && w.Coef.LordIntermediaryStake > 0) || (a == PolicyAxis.StateReligion && w.Coef.LordReligionStake > 0));

    internal static double LordIncomeGain(World w, PolicyAxis axis, double lordRentToIncome)
    {
        EffectiveRentShares(w, axis, out double now, out double prev, back: true);
        return now > 1e-9 ? (prev - now) / now * lordRentToIncome : 0.0;
    }


    private static double IntermediaryStake(World w, PolicyAxis a, in Profile e, bool increasing)
    {
        var b = w.Bureaucracy; var lr = w.LandReform;
        double now = Privileges.IntermediaryDegreeFor(b, lr);
        double other = a == PolicyAxis.Bureaucracy ? Privileges.IntermediaryDegreeFor(BureaucracyStep(b, !increasing), lr) : Privileges.IntermediaryDegreeFor(b, Step(lr, !increasing));
        return now > 1e-9 ? e.CutRhoToIncome0 * System.Math.Abs(now - other) / now : 0.0;
    }

    private static double ReligionStake(World w, in Profile e, bool increasing)
    {
        var s = w.StateReligion; var c = w.Coef;
        double now = Privileges.TitheDegreeFor(s, c), other = Privileges.TitheDegreeFor(ReligionStep(s, !increasing), c);
        return now > 1e-9 ? e.ClergyToIncome0 * System.Math.Abs(now - other) / now : 0.0;
    }

    private static BureaucracyLaw BureaucracyStep(BureaucracyLaw b, bool back)
        => back ? (b == BureaucracyLaw.Spoils ? b : b - 1) : (b == BureaucracyLaw.Professional ? b : b + 1);

    private static StateReligionLaw ReligionStep(StateReligionLaw s, bool back)
        => s == StateReligionLaw.StateAtheism ? s
         : back ? (s == StateReligionLaw.StateChurch ? s : s - 1) : (s == StateReligionLaw.ReligiousFreedom ? s : s + 1);

}
