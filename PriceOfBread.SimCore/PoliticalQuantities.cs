namespace PriceOfBread.SimCore;

using System;

public static class PoliticalQuantities
{
    public static double EffectiveCoerce(World w, Coefficients c)
        => w.Coercion * (w.TotalPopulation / Math.Max(1.0, c.PoliticalReferencePop));

    public static double ChallengeRatio(double challenge, double coerce)
        => Clamp01(challenge / Math.Max(1.0, coerce));

    public static int FamilyOf(int ideo) => ideo is 2 or 3 ? 1 : ideo is 5 or 6 or 7 ? 2 : 0;

    public static (double share, double sat) MandateBase(World w)
    {
        double size = 0, illit = 0, sat = 0;
        foreach (var p in w.AllPops())
        {
            size += p.Size;
            double u = p.Size * (1.0 - p.Literacy);
            illit += u;
            sat += u * p.Satisfaction;
        }
        return (size > 1e-9 ? illit / size : 1.0, illit > 1e-9 ? sat / illit : -1);
    }


    public static double ConscriptionDepth(ConscriptionLaw law) => law switch
    {
        ConscriptionLaw.Universal => 1.0,
        ConscriptionLaw.Selective => 0.5,
        _ => 0.0,
    };

    public static double BureaucracyDepth(BureaucracyLaw law) => law switch
    {
        BureaucracyLaw.Professional => 1.0,
        BureaucracyLaw.MeritExam => 0.5,
        _ => 0.0,
    };

    public static double ServiceLength(ConscriptionLaw law) => law switch
    {
        ConscriptionLaw.Universal => 0.4,
        ConscriptionLaw.Selective => 0.6,
        _ => 1.0,
    };

    public static double AvgLiteracy(World w, bool coreOnly = false)
    {
        double lit = 0, size = 0;
        foreach (var r in w.Regions)
        {
            if (coreOnly && r.Tier == RegionTier.Colonial) continue;
            foreach (var p in r.Pops) { lit += p.Literacy * p.Size; size += p.Size; }
        }
        return size > 1e-9 ? lit / size : 0;
    }

    public static double ArmyProfessionalism(World w)
    {
        double lit = Clamp01(AvgLiteracy(w));
        double org = 0.5 * BureaucracyDepth(w.Bureaucracy) + 0.5 * (w.Railways ? 1.0 : 0.0);
        return lit * org * ServiceLength(w.ConscriptionLaw);
    }

    public static double MilitaryPoliticalRatio(World w) => MilitaryPush(w) * (1.0 - CivilianValve(w));

    public static double MilitaryPush(World w)
    {
        var c = w.Coef;
        double soldierShare = w.SoldierSizeK() / Math.Max(1.0, w.TotalPopulation);
        double weight = Clamp01(soldierShare / Math.Max(1e-9, Military.PeaceCapShareOfPop(c)));
        double purge = Clamp01(w.MandateSpent);
        double cur = w.MilitarySpending.Value;
        double target = Government.MilitaryTotalTarget(w, w.Policy);
        double dissolution = cur > 1e-9 ? Clamp01((cur - target) / cur) : 0;
        double humiliation = Clamp01(w.DefeatShock + c.LostLandNationalism * w.LostRegionsCumulative);
        return Clamp01(weight + dissolution + 0.5 * purge + humiliation);
    }

    public static double CivilianValve(World w)
    {
        return Clamp01(0.5 * ((1.0 - AppointmentAtCourt(w)) + Clamp01(w.Legitimacy)));
    }

    public static double AppointmentAtCourt(World w)
        => w.Crown != CrownForm.None ? 1.0 - Clamp01(w.ParliamentControl) : 0.0;

    public static double WarFeverPublicSphere(World w)
    {
        double press = Politics.PressFreedom(w.Censorship);
        double lit = Clamp01(AvgLiteracy(w));
        double urban = Clamp01(w.UrbanShare);
        double humiliation = Clamp01(w.DefeatShock + w.Coef.LostLandNationalism * w.LostRegionsCumulative
                                   + (w.UnequalTreatyInForce ? 1.0 : 0.0));
        return press * lit * urban * humiliation;
    }

    public static double OfficerAppointmentCrownward(World w)
    {
        return AppointmentAtCourt(w) - Clamp01(w.ParliamentControl);
    }


    public static double MilitaryBudgetShare(World w)
    {
        double mil = Math.Max(0, w.LastMilitaryClaimPerMonth.Value) + Math.Max(0, w.LastSoldierPayPerMonth.Value);
        double other = Math.Max(0, w.LastAdminClaimPerMonth.Value) + Math.Max(0, w.LastInterestPerMonth.Value);
        double tot = mil + other;
        return tot > 1e-9 ? Clamp01(mil / tot) : 0;
    }

    internal static double Clamp01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;
    internal static double Pos(double x) => !(x > 0) ? 0 : x;
    internal static double Toward(double x, double target, double k) => x + k * (target - x);
    internal static double ProbOr(double a, double b) => 1.0 - (1.0 - a) * (1.0 - b);
    internal static double ClampOr(double x, double lo, double hi, double neutral) => x < lo ? lo : x > hi ? hi : (x >= lo ? x : neutral);

    public static double[] DriftedPolityCoord(Polity polity, Suffrage suffrage, Coefficients c)
    {
        var coord = (double[])Ideologies.BasePolityCoord(polity).Clone();
        coord[0] = PoliticalQuantities.Toward(coord[0], Ideologies.Openness(suffrage), c.PolityCoordDriftWeight);
        return coord;
    }

    public static double PoliticalCapitalMax(World w)
    {
        var c = w.Coef;
        double aff = Ideologies.SupportAt(Ideologies.National(w), DriftedPolityCoord(w.Polity, w.Suffrage, c), c.IdeologyAffinitySlope);
        double sup = Clamp01((aff - Ideologies.AffMin) / (Ideologies.AffMax - Ideologies.AffMin));
        double b = c.MandateWeight * c.MandateSpendRate * (w.Legitimacy + sup);
        return Pos(b);
    }

    public static double PoliticalCapitalRate(World w)
    {
        double pe = PowerShares.ParliamentEffective(w);
        double seats = w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];
        double maxSeat = Math.Max(w.PartySeats[0], Math.Max(w.PartySeats[1], w.PartySeats[2]));
        double split = !(seats > 1e-9) ? 0 : Clamp01(1.0 - maxSeat / seats);
        double friction = pe * split + pe + (w.InCivilWar ? 1.0 : 0);
        return !(friction > 0) ? 1.0 : 1.0 / (1.0 + friction);
    }
}
