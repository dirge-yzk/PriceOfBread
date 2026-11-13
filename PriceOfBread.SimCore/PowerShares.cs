namespace PriceOfBread.SimCore;

public static class PowerShares
{
    public const double PartnerViableShare = 0.5;

    public static double PrivilegeIntact(World w)
    {
        double serf = Privileges.SeigneurialDegree(w);
        double taxExempt = 1.0 - PoliticalQuantities.Clamp01(w.LandTax + w.ProgressiveTax);
        double estate = Privileges.EstateDegree(w);
        double v = (serf + taxExempt + estate) / 3.0;
        return !(v >= 0 && v <= 1) ? 0.5 : v;
    }

    public static double StatePatronage(World w)
    {
        double tariff = PoliticalQuantities.Clamp01(w.Tariff);
        double orders = PoliticalQuantities.Clamp01(w.MilitarySpending.Value);
        double order = PoliticalQuantities.Clamp01(w.PublicOrder);
        return (tariff + orders + order) / 3.0;
    }

    public static double ParliamentControl(World w)
    {
        double pc = w.ParliamentControl;
        if (!(pc >= 0 && pc <= 1)) return !(pc > 0) ? 0 : 1;
        return pc;
    }

    public static double ParliamentGrip(World w)
    {
        double annual = System.Math.Max(0, w.TaxRate.Value) + System.Math.Max(0, w.ProgressiveTax);
        double standing = System.Math.Max(0, w.PollTax) + System.Math.Max(0, w.LandTax)
                        + System.Math.Max(0, w.StateMonopolyBreadth) * System.Math.Max(0, w.Coef.StateMonopolyMargin) + System.Math.Max(0, w.Tariff);
        double tot = annual + standing;
        double taxGrip = !(tot > 1e-12) ? 0 : annual / tot;
        if (!(taxGrip >= 0 && taxGrip <= 1)) taxGrip = 0;
        double v = PoliticalQuantities.ProbOr(taxGrip, DebtGrip(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    internal static double DebtGrip(World w)
    {
        double gdpYear = Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value;
        return !(gdpYear > 0) ? 0 : PoliticalQuantities.Clamp01(System.Math.Max(0, w.GovDebt.Value) / gdpYear);
    }

    public static double LockedSpending(World w)
    {
        var c = w.Coef;
        double floor = 0;
        if (w.AtWarNow && c.WartimeUpkeepFloor.Value > 0)
            floor = System.Math.Max(floor, (w.ArmyUpkeep.Value + w.NavyUpkeep.Value) * c.WartimeUpkeepFloor.Value);
        return PoliticalQuantities.Clamp01(floor);
    }

    public static double ParliamentEffective(World w)
    {
        double v = ParliamentControl(w) * ParliamentGrip(w) * (1.0 - LockedSpending(w));
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    public static double CivilianInstitutions(World w)
    {
        double parliament = ParliamentEffective(w);
        double bureau = PoliticalQuantities.Clamp01(PoliticalQuantities.BureaucracyDepth(w.Bureaucracy));
        double parties = PoliticalQuantities.Clamp01(Ideologies.Openness(w.Suffrage));
        double legitimacy = PoliticalQuantities.Clamp01(w.Legitimacy);
        double v = (parliament + bureau + parties + legitimacy) / 4.0;
        return !(v >= 0 && v <= 1) ? 0 : v;
    }

    public static double SoldierWelfare(World w)
    {
        double size = 0, sat = 0;
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            double sz = System.Math.Max(0, p.Size);
            double s = p.SupplyFill;
            size += sz; sat += sz * s;
        }
        if (!(size > 1e-12)) return 1.0;
        double v = sat / size;
        return !(v >= 0 && v <= 1) ? 1.0 : v;
    }
}
