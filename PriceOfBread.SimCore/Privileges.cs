namespace PriceOfBread.SimCore;

public static class Privileges
{
    public const int Tithe = 0;
    public const int TaxExemption = 1;
    public const int VestedOffice = 2;
    public const int Seigneurial = 3;
    public const int Monopoly = 4;
    public const int Guild = 5;
    public const int Count = 6;
    public static readonly string[] Names = ["十分の一税", "身分制免税", "既得の官職", "領主権", "特許会社", "同業組合"];
    public static readonly int[] Beneficiary = [PopClass.Clergy, PopClass.Noble, PopClass.Bureaucrat, PopClass.Noble, PopClass.Capitalist, PopClass.Artisan];

    public static double EstateDegree(World w) => EstateDegreeFor(w.Bureaucracy);
    public static double EstateDegreeFor(BureaucracyLaw b) => b switch { BureaucracyLaw.Spoils => 1.0, BureaucracyLaw.MeritExam => 0.5, _ => 0.0 };
    public static double LandReformDegreeFor(ReformLevel lr) => lr switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
    public static double SeigneurialDegree(World w) => w.SerfEmancipation switch { ReformLevel.None => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
    public static double IntermediaryDegree(World w) => IntermediaryDegreeFor(w.Bureaucracy, w.LandReform);
    public static double IntermediaryDegreeFor(BureaucracyLaw b, ReformLevel lr) => System.Math.Max(EstateDegreeFor(b), LandReformDegreeFor(lr));
    public static double GuildDegree(World w) => w.Guild switch { GuildLaw.Full => 1.0, GuildLaw.Limited => 0.5, _ => 0.0 };

    public static double TitheDegree(World w, Coefficients c) => TitheDegreeFor(w.StateReligion, c);
    public static double TitheDegreeFor(StateReligionLaw s, Coefficients c)
        => !(c.TitheRate.Value > 0) ? 0.0
         : s switch { StateReligionLaw.InnerFreedom => c.InnerFreedomTithe, StateReligionLaw.ReligiousFreedom => c.ReligiousFreedomTithe, StateReligionLaw.StateAtheism => 0.0, _ => 1.0 };

    public static double TaxExemptionDegree(World w, Coefficients c, double crownShare)
        => !(c.PrivilegeTaxDiscount.Value > 0) ? 0.0
         : c.PrivilegeFromCrownShare.Value > 0
            ? c.PrivilegeTaxDiscount.Value * c.PrivilegeFromCrownShare.Value * PoliticalQuantities.Clamp01(crownShare)
            : (w.Polity is Polity.Absolutist or Polity.Despotic or Polity.Shogunate ? c.PrivilegeTaxDiscount.Value : 0.0);

    public static double MonopolyDegree(World w, Coefficients c)
    {
        double best = 0;
        foreach (var r in w.Regions)
        {
            double d = EconomyStep.MonopolyDegree(r, c, 1.0);
            if (d > best) best = d;
        }
        return best;
    }

    public static double Degree(World w, Coefficients c, int kind) => kind switch
    {
        Tithe => TitheDegree(w, c),
        TaxExemption => TaxExemptionDegree(w, c, Fiscal.CrownShareForPrivilege(w, c)),
        VestedOffice => EstateDegree(w),
        Seigneurial => SeigneurialDegree(w),
        Monopoly => MonopolyDegree(w, c),
        Guild => GuildDegree(w),
        _ => 0.0,
    };

    public static bool Enabled(Coefficients c, int kind) => kind switch
    {
        Tithe => c.TitheRate.Value > 0,
        TaxExemption => c.PrivilegeTaxDiscount.Value > 0,
        VestedOffice => true,
        Seigneurial => true,
        Monopoly => c.MonopolyRentPremium > 0,
        Guild => c.GuildLawGate > 0 && c.GuildRentPremium > 0,
        _ => false,
    };

    public static bool HeldBy(int kind, int classId)
        => kind == TaxExemption ? classId is PopClass.Noble or PopClass.Clergy : Beneficiary[kind] == classId;

    public static double TaxAssessment(World w, Coefficients c, Pop p, double crownShare)
        => p.Closed && !p.IsSoldier ? 1.0 - TaxExemptionDegree(w, c, crownShare) : 1.0;


    public static void Record(World w, int kind, double take)
    {
        if (kind >= w.PrivilegeTakeMonth.Length) return;
        if (!(take > 0)) return;
        w.PrivilegeTakeMonth[kind] += take;
    }

    public static void StepMonth(World w, Coefficients c)
    {
        double dt = 1.0 / Calendar.MonthsPerYear;
        for (int k = 0; k < Count && k < w.PrivilegeTakeMonth.Length; k++)
        {
            double take = w.PrivilegeTakeMonth[k];
            w.PrivilegeTakeYear[k] = w.PrivilegeTakeYear[k] * (1.0 - dt) + take;
            bool active = take > 0;
            if (c.PrivilegeStripMilitancy > 0 && !active && w.PrivilegeHeldYears[k] > 0 && w.PrivilegeLastActive[k])
            {
                double anger = c.PrivilegeStripMilitancy * Strength(w, c, k);
                foreach (var p in w.AllPops()) if (HeldBy(k, p.ClassId) && !p.IsSoldier) p.Militancy = System.Math.Min(1.0, p.Militancy + anger);
                w.LogAdd($"{w.Year} {Names[k]}の剥離＝受益層の怒り+{anger:F2}");
            }
            w.PrivilegeLastActive[k] = active;
            if (w.PrivilegeHeldYears[k] < 0) w.PrivilegeHeldYears[k] = active ? c.DominanceMatureYears : 0;
            w.PrivilegeHeldYears[k] = active ? w.PrivilegeHeldYears[k] + dt : System.Math.Max(0, w.PrivilegeHeldYears[k] - dt);
            w.PrivilegeTakeMonth[k] = 0;
        }
    }

    public static double Strength(World w, Coefficients c, int kind)
    {
        double y = System.Math.Max(0, w.PrivilegeHeldYears[kind]) / System.Math.Max(1e-9, c.DominanceMatureYears);
        return PoliticalQuantities.Clamp01(y);
    }
}
