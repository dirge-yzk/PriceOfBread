namespace PriceOfBread.SimCore;

public static class TechEducation
{
    internal static double MotherLiteracyRatio(Coefficients c, double womenWageRetention) => c.FemaleLitFloor + (1.0 - c.FemaleLitFloor) * womenWageRetention;

    private const double LiteracyRateNone = 0.0001;
    private const double LiteracyRateReligious = 0.004;
    private const double LiteracyRatePublic = 0.005;
    private const double LiteracyRateCompulsory = 0.007;
    private const double CensorshipPressFactor = 0.75;

    internal static double AbsorptionOf(World w, Coefficients c)
    {
        double pressAbs = PressFactor(w.Censorship);
        double breadth = PoliticalQuantities.Clamp01(w.Elite / c.EliteForFullTech);
        return breadth * pressAbs;
    }

    public static void GrowLiteracy(World w, Coefficients c)
    {
        double baseSpeed = w.WarmupFrozen ? 0 : LiteracyRate(w.EducationLaw);
        double motherLit = MotherLiteracyRatio(c, w.WomenWageRetention);
        if (w.WomenEducationLaw) motherLit = Math.Min(1.0, motherLit * (1.0 + c.WomenEduLitBonus));
        if (baseSpeed > 0)
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Pops)
                {
                    if (p.IsCriminal) continue;
                    if (p.Literacy < p.EducationFill)
                    {
                        double litFactor = 1.0 + c.ChildLaborLiteracyPenalty * (1.0 - LaborSupply.ChildLaborRate(p, r, c, p.SupplyFill));
                        p.Literacy = PoliticalQuantities.Toward(p.Literacy, p.EducationFill, baseSpeed * litFactor);
                    }
                    else if (p.Literacy > p.EducationFill)
                        p.Literacy = p.GenerationalMix(p.Literacy, Math.Max(p.EducationFill, motherLit * p.Literacy), 1);
                }
            }

        w.Elite = EmergentElite(w) ?? PoliticalQuantities.AvgLiteracy(w) * EliteMultiplier(w.EducationLaw);
        w.EliteCore = EmergentElite(w, coreOnly: true) ?? PoliticalQuantities.AvgLiteracy(w, coreOnly: true) * EliteMultiplier(w.EducationLaw);
    }

    public static void GrowTech(World w, Coefficients c)
    {
        if (!(w.Elite > 0)) return;

        double baseTarget = c.AdoptionCeiling * PoliticalQuantities.Clamp01(w.Elite / c.EliteForFullTech);
        bool absorptive = FrontierCapable(w, c);
        double catchFrac = absorptive ? c.CatchupFractionAdvanced.Value : c.CatchupFraction.Value;
        double adoptRate = absorptive ? c.AdoptionRateAdvanced : c.AdoptionRate;
        if (c.CatchupAbsorptionBlend > 0)
        {
            double absorption = AbsorptionOf(w, c);
            double a = absorptive ? 1.0 + (absorption - 1.0) * c.CatchupAbsorptionBlend : 0.0;
            catchFrac = c.CatchupFraction.Value + (c.CatchupFractionAdvanced.Value - c.CatchupFraction.Value) * a;
            adoptRate = c.AdoptionRate + (c.AdoptionRateAdvanced - c.AdoptionRate) * a;
        }
        double adoptionCeil = Math.Max(c.AdoptionCeiling, w.WorldTechLevel * catchFrac);
        double catchupTarget = baseTarget + Math.Max(0, adoptionCeil - c.AdoptionCeiling) * PoliticalQuantities.Clamp01(w.Elite / c.EliteForCatchup);
        if (catchupTarget > baseTarget + 1e-9)
        {
            if (w.TechLevel < catchupTarget)
                w.TechLevel += adoptRate * (catchupTarget - w.TechLevel);
        }
        else if (w.TechLevel < baseTarget)
        {
            w.TechLevel += Math.Min(c.TechGrowthRate * w.Elite, baseTarget - w.TechLevel);
        }

        if (absorptive)
        {
            double press = PressFactor(w.Censorship);
            double patentMul = w.PatentLaw ? c.PatentInnovationBonus : 1.0;
            double innov = c.InnovationRate * w.Elite * press * patentMul
                         / (1.0 + w.TechLevel / c.InnovationScale);
            w.TechLevel += innov;
        }

        w.TechLevel = Math.Min(w.TechLevel, c.FrontierCeiling);
    }

    private static double LiteracyRate(EducationLaw law) => law switch
    {
        EducationLaw.None => LiteracyRateNone,
        EducationLaw.Religious => LiteracyRateReligious,
        EducationLaw.Public => LiteracyRatePublic,
        EducationLaw.Compulsory => LiteracyRateCompulsory,
        _ => 0.0,
    };

    private static double PressFactor(CensorshipLaw law) => law switch { CensorshipLaw.FreePress => 1.0, CensorshipLaw.Censorship => CensorshipPressFactor, _ => 0.5 };

    private static double? EmergentElite(World w, bool coreOnly = false)
    {
        double secular = 0, total = 0;
        foreach (var r in w.Regions)
        {
            if (coreOnly && r.Tier == RegionTier.Colonial) continue;
            foreach (var p in r.Producers)
                if (p.GoodId == Catalog.EducationId)
                {
                    total += p.Output;
                    if (p.LaborPopIndex >= 0 && p.LaborPopIndex == r.TeacherPopIndex)
                        secular += p.Output;
                }
        }
        if (total <= 1e-9) return null;
        double channel = (secular + (total - secular) * Math.Min(1.0, EliteMultiplier(w.EducationLaw))) / total;
        return PoliticalQuantities.AvgLiteracy(w, coreOnly) * channel;
    }

    private static double EliteMultiplier(EducationLaw law) => law switch
    {
        EducationLaw.None => 0.0,
        EducationLaw.Religious => 0.1,
        EducationLaw.Public => 1.0,
        EducationLaw.Compulsory => 1.2,
        _ => 0.0,
    };

    private static bool FrontierCapable(World w, Coefficients c) =>
        (w.Freedom >= c.FrontierFreedomThreshold
         || (c.InnovationEliteSubstitute > 0 && w.Elite >= c.EliteForFullTech)) &&
        w.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory;

}
