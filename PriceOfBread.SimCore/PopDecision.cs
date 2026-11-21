namespace PriceOfBread.SimCore;

public static class PopDecision
{
    private const double Eps = 1e-9;

    internal static (Pop? Rural, Pop? Urban) MigrationPair(Region region)
    {
        Pop? rural = null, urban = null;
        foreach (var p in region.Pops)
        {
            if (p.Closed || p.IsCriminal) continue;
            if (p.IsUrban) urban ??= p;
            else rural ??= p;
        }
        return (rural, urban);
    }

    internal static bool HousingGateOpen(Coefficients coef, double urbanPop, double totalPop)
        => (totalPop > Eps ? urbanPop / totalPop : 0.0) > coef.HousingUrbanThreshold;

    internal static MigrationValueRow MigrationValues(Region region, Coefficients coef, Pop rural, Pop urban)
    {
        double earnedR = rural.Size > Eps ? rural.EarnedPerMonth.Value / rural.Size : 0.0;
        double earnedU = urban.Size > Eps ? urban.EarnedPerMonth.Value / urban.Size : 0.0;
        double sub = rural.Size > Eps ? rural.SubsistenceValuePerMonth.Value / rural.Size : 0.0;
        double housing = 0.0;
        if (coef.HousingNeedPerUrban > 0 && Catalog.HousingId < region.Markets.Length)
        {
            double totalPop = region.SumPopulation();
            if (HousingGateOpen(coef, region.SumUrbanPopulation(), totalPop)) housing = coef.HousingNeedPerUrban * region.Markets[Catalog.HousingId].Price;
        }
        double vr = (earnedR > 0 ? earnedR : 0.0) + (sub > 0 ? sub : 0.0);
        double vuRaw = (earnedU > 0 ? earnedU : 0.0) - housing;
        double vu = vuRaw > 0 ? vuRaw : 0.0;
        double sum = vr + vu;
        double g = sum > Eps ? 2.0 * (vu - vr) / sum : 0.0;
        if (!(g == g)) g = 0.0;
        return new MigrationValueRow(vr, vu, sub > 0 ? sub : 0.0, housing, g);
    }

    internal readonly record struct MigrationValueRow(double VRural, double VUrban, double SubsistenceValue, double HousingCost, double Gsym);

    public static void Run(Region region, Coefficients coef, double serfFree = 1.0)
    {
        if (region.Pops.Count < 2) return;

        var (rural, urban) = MigrationPair(region);
        if (rural is null || urban is null) return;

        double total = rural.Size + urban.Size;
        if (total <= Eps) return;

        double flow;
        if (coef.UrbanFlowReal > 0)
        {
            double g = MigrationValues(region, coef, rural, urban).Gsym;
            flow = g > 0 ? coef.Mobility * g * rural.Size : coef.Mobility * g * urban.Size;
        }
        else
        {
            double incRural = rural.Size > Eps ? (rural.EarnedPerMonth / rural.Size).Value : 0.0;
            double incUrban = urban.Size > Eps ? (urban.EarnedPerMonth / urban.Size).Value : 0.0;

            double z = Math.Clamp((incUrban - incRural) / coef.MigrationTemperature, -40, 40);
            double targetUrban = 1.0 / (1.0 + Math.Exp(-z));
            double curUrban = urban.Size / total;


            flow = coef.Mobility * (targetUrban - curUrban) * total;
        }

        if (flow > 0 && coef.SerfMobilityGate > 0)
            flow *= (1.0 - coef.SerfMobilityGate) + coef.SerfMobilityGate * serfFree;

        if (flow > 0 && urban.FoodFill < coef.FamineMigrationFillGate && urban.FoodFill < rural.FoodFill)
        {
            flow = 0;
            // (1283) 旗 FamineGateReturnsRural：飢えの門が掛かった月に、移る値打ちの g が負なら都市→農村へ（UrbanFlowReal の負の枝と同じ式）
            if (coef.FamineGateReturnsRural > 0)
            {
                double g = MigrationValues(region, coef, rural, urban).Gsym;
                if (g < 0) flow = coef.Mobility * g * urban.Size;
            }
        }
        if (flow > 0) Move(rural, urban, flow);
        else if (flow < 0) Move(urban, rural, -flow);
    }

    private static void Move(Pop src, Pop dst, double amount)
    {
        if (amount <= 0 || src.Size <= Eps) return;
        Pop.Blend(src, dst, amount);
    }
}
