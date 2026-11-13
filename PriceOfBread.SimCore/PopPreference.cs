namespace PriceOfBread.SimCore;

internal static class PopPreference
{
    internal const double BurdenScale = 3.0;
    internal const double LandTaxRentierThreshold = 0.34;

    internal static readonly PolicyAxis[] Axes =
    [PolicyAxis.IncomeTax, PolicyAxis.LandTax, PolicyAxis.PollTax, PolicyAxis.SocialSecurity, PolicyAxis.Tariff, PolicyAxis.CornLaws];

    internal readonly record struct Stance(double Wealth, double Rent, double LandBase, double Poll, double Pop, double Benefit, double RentIncome, double Old);

    internal static double Burden(double baseShare, double popShare) => popShare > 0 && baseShare > 0 ? baseShare / popShare : 0.0;

    internal static double Pi(PolicyAxis a, in Stance s, double rentProtection, double pensionOldWeight, double stdOld)
    {
        switch (a)
        {
            case PolicyAxis.IncomeTax: return s.Benefit - BurdenScale * Burden(s.Wealth, s.Pop);
            case PolicyAxis.LandTax: { double b = Burden(s.LandBase, s.Pop); return (b < LandTaxRentierThreshold ? 1.0 : 0.0) - (BurdenScale + 1.0) * b; }
            case PolicyAxis.PollTax: { double b = Burden(s.Poll, s.Pop); return (b < 1e-6 ? 1.0 : 0.0) - BurdenScale * b; }
            case PolicyAxis.SocialSecurity: return s.Benefit - 2.0 * Burden(s.Wealth, s.Pop) + pensionOldWeight * (s.Old - stdOld);
            case PolicyAxis.Tariff: return rentProtection * Burden(s.Rent, s.Pop) - s.Benefit;
            case PolicyAxis.CornLaws: return rentProtection * Burden(s.Rent, s.Pop) - (1.0 - s.RentIncome);
            default: return 0.0;
        }
    }

    internal static int NearestParty(ReadOnlySpan<double> pi, double[][] parties)
    {
        int best = -1; double bd = double.PositiveInfinity;
        for (int k = 0; k < parties.Length; k++)
        {
            double d = 0;
            for (int a = 0; a < pi.Length && a < parties[k].Length; a++) d += System.Math.Abs(pi[a]) * System.Math.Abs(pi[a] - parties[k][a]);
            if (d < bd) { bd = d; best = k; }
        }
        return best;
    }
}
