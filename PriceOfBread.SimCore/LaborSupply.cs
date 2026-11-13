namespace PriceOfBread.SimCore;

public static class LaborSupply
{
    private static bool HasSchool(Region r) => Catalog.EducationId < r.Markets.Length;

    private static double Need(double supplyFill) => PoliticalQuantities.Clamp01(1.0 - supplyFill);

    public static double FemaleParticipation(Region r, Coefficients c, double supplyFill)
        => PoliticalQuantities.Clamp01(r.FemaleParticipation + c.FemaleNeedWeight * Need(supplyFill) * r.WomenWageRetention);

    public static double ChildLaborRate(Pop p, Region r, Coefficients c, double supplyFill)
    {
        double schoolAttend = HasSchool(r) ? p.EducationFill * (1.0 - Need(supplyFill)) : 0.0;
        return PoliticalQuantities.Clamp01(r.ChildLaborLegal * (1.0 - schoolAttend));
    }
}
