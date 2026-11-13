namespace PriceOfBread.SimCore;

public static class InterestGroups
{
    public static (double Traditional, double Modern, double Mass) Blocs(World w)
    {
        var k = Context(w);
        double trad = 0, modern = 0, mass = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            double pw = PopWeight(w, p, k);
            if (p.Closed) trad += pw;
            else if (p.IsModernElite) modern += pw;
            else mass += pw;
        }
        return (trad, modern, mass);
    }

    internal readonly record struct WeightContext(double TotalWealth, double TotalPop, double Beta, Suffrage Eff, double ClosedMul);

    internal static WeightContext Context(World w)
    {
        var (totalWealth, totalPop) = w.PopSums is { } win ? win.LandedTotals(w) : ComputeLandedTotals(w);
        return new WeightContext(totalWealth, totalPop, w.Coef.InterestGroupHeadcountWeight, w.Suffrage,
            1.0 + w.Coef.MilitarismClout * PoliticalQuantities.MilitaryPoliticalRatio(w));
    }

    internal static double PopWeight(World w, Pop p, in WeightContext k)
    {
        double lw = LandedWealthOf(w, p), sz = p.Size;
        double wealthShare = k.TotalWealth > 1e-9 ? lw / k.TotalWealth : 0;
        double share = k.Beta > 0 && k.TotalPop > 1e-9
            ? (1.0 - k.Beta) * wealthShare + k.Beta * (sz / k.TotalPop)
            : wealthShare;
        return share * PoliticalWeight(p, k.Eff, k.ClosedMul, w.Coef);
    }

    private static double LandedWealthOf(World w, Pop p)
        => Math.Max(0, p.Savings.Value)
            + (p.Closed && w.Coef.LandedPowerCapitalization > 0 ? w.Coef.LandedPowerCapitalization * Math.Max(0, p.LandRentPerMonth.Value) : 0);

    internal static (double Wealth, double Pop) ComputeLandedTotals(World w)
    {
        double totalWealth = 0, totalPop = 0;
        foreach (var (_, p) in w.AllPopsWithRegion())
        {
            totalWealth += LandedWealthOf(w, p); totalPop += p.Size;
        }
        return (totalWealth, totalPop);
    }

    public static double SuffrageInclusion(World w, Region r, Pop p)
    {
        double core = p.CoreEthnicShare.Value;
        if (!(core >= 0 && core <= 1)) core = 1.0;
        if (r.Tier == RegionTier.Colonial)
            return w.ColonialSuffrage switch
            {
                ColonialSuffrageLaw.Full => 1.0,
                ColonialSuffrageLaw.OwnNationals => core,
                _ => 0.0,
            };
        return w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : core;
    }

    private static double PoliticalWeight(Pop p, Suffrage s, double closedMul, Coefficients c)
        => p.Closed ? closedMul
                    : FranchiseVoice(p, s, c) / 0.5;

    public static double FranchiseVoice(Pop p, Suffrage s, Coefficients c)
    {
        double gate = p.Closed ? 1.0
                    : p.IsModernElite ? (s >= Suffrage.Census ? 1.0 : c.ModernEliteSubCensusVoice)
                    : (s >= Suffrage.AdultMale ? 1.0 : 0.0);
        double a21 = s >= Suffrage.Universal ? 0 : p.AgeShare(21, double.PositiveInfinity);
        double fem = s >= Suffrage.Universal ? 1.0 : a21 > 1e-300 ? 1.0 - p.AgeShare(21, double.PositiveInfinity, 1) / a21 : 0.5;
        return gate * fem;
    }

    public static double SeatVoice(World w, Region r, Pop p, Suffrage s)
    {
        double v = FranchiseVoice(p, s, w.Coef);
        return r.Tier == RegionTier.Colonial ? v * SuffrageInclusion(w, r, p) : v;
    }
}
