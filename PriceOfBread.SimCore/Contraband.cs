namespace PriceOfBread.SimCore;

public static class Contraband
{
    public static double LeakFraction(double adminFill, Coefficients c)
    {
        if (c.SmugglingMax.Value <= 0) return 0.0;
        double f = c.SmugglingMax.Value * (1.0 - adminFill);
        return !(f >= 0) ? 0.0 : f > 1.0 ? 1.0 : f;
    }
}
