namespace PriceOfBread.SimCore;

internal static class PowerCenter
{
    internal static (double Loyal, double Army, double Revolt) SoldierSplit(ReadOnlySpan<double> mass, ReadOnlySpan<double> affX, ReadOnlySpan<double> affA, ReadOnlySpan<double> affV)
    {
        double l = 0, a = 0, v = 0, total = 0;
        for (int k = 0; k < mass.Length && k < affX.Length && k < affA.Length && k < affV.Length; k++)
        {
            double m = mass[k];
            if (!(m > 0)) continue;
            total += m;
            if (affA[k] > affX[k] && affA[k] >= affV[k]) a += m;
            else if (affV[k] > affX[k] && affV[k] > affA[k]) v += m;
            else l += m;
        }
        if (!(total > 0)) return (1.0, 0.0, 0.0);
        return (l / total, a / total, v / total);
    }

    internal static double GOf(double l, double w) { double g = l - w; return g > 0 ? g : 0.0; }

    internal static double HOf(double l, double w, double popTimesQ) { double g = GOf(l, w); double c = popTimesQ > 0 ? popTimesQ : 0.0; return g < c ? g : c; }

    internal static double Wallet(BudgetLaw k, double interestShare, double taxYearShare, bool revenuePositive)
    {
        if (!revenuePositive) return 0.0;
        double i = interestShare > 0 ? (interestShare < 1 ? interestShare : 1.0) : 0.0;
        double t = taxYearShare > 0 ? (taxYearShare < 1 ? taxYearShare : 1.0) : 0.0;
        return k switch { BudgetLaw.Decree => i, BudgetLaw.Partial => 1.0 - (1.0 - t) * (1.0 - i), _ => 1.0 };
    }

    internal static BudgetLaw KFromPolity(Polity p) => p switch { Polity.ConstitutionalMonarchy => BudgetLaw.Partial, Polity.Republic => BudgetLaw.Parliamentary, _ => BudgetLaw.Decree };

    internal static Polity NameOf(BudgetLaw k, CrownForm crown, SeizureForm seizure)
    {
        switch (seizure)
        {
            case SeizureForm.Bourgeois: return Polity.BourgeoisDictatorship;
            case SeizureForm.Proletarian: return Polity.ProletarianDictatorship;
            case SeizureForm.Military: return Polity.MilitaryDictatorship;
            case SeizureForm.Anarchy: return Polity.Anarchy;
        }
        if (k == BudgetLaw.Decree)
            return crown switch { CrownForm.Absolutist => Polity.Absolutist, CrownForm.Despotic => Polity.Despotic, CrownForm.Shogunate => Polity.Shogunate, _ => Polity.BourgeoisDictatorship };
        return crown != CrownForm.None ? Polity.ConstitutionalMonarchy : Polity.Republic;
    }

    internal readonly record struct Inputs(
        double G, double Q, double MRad, double MRefOut, double Loyal, double Army, double Revolt, double Push,
        double InterestShare, double TaxYearShare, bool RevenuePositive);

    internal readonly record struct Result(
        double Wallet, double ValveIndex, double AutonomyIndex,
        double FExec, double FPart, double FArmy, double FRev, double Sum, double SumParts,
        double SExec, double SPart, double SArmy, double SRev, double SRegime, double Safety);

    private static double Clamp01(double x) => x > 0 ? (x < 1 ? x : 1.0) : 0.0;

    internal static Result Evaluate(in Inputs x, BudgetLaw k)
    {
        double g = x.G > 0 ? x.G : 0.0, q = x.Q > 0 ? x.Q : 0.0;
        double mr = x.MRad > 0 ? x.MRad : 0.0, mo = x.MRefOut > 0 ? x.MRefOut : 0.0;
        double mExt = mr + mo;
        double sum = g + q * mExt;
        double dl = x.Loyal, da = x.Army, dv = x.Revolt;
        double sRev0 = sum > 0 ? (g * dv + q * mExt) / sum : 0.0;
        double appointExec = k == BudgetLaw.Parliamentary ? 0.0 : 1.0;
        double valve = Clamp01(0.5 * ((1.0 - appointExec) + (1.0 - sRev0)));
        double mA = Clamp01(x.Push) * (1.0 - valve);
        double w = Wallet(k, x.InterestShare, x.TaxYearShare, x.RevenuePositive);
        double aA = da * mA, regime = dl + da * (1.0 - mA), aV = dv;
        double aE = k == BudgetLaw.Parliamentary ? 0.0 : regime, aP = k == BudgetLaw.Parliamentary ? regime : 0.0;
        const double c = 1.0;
        double fE = g * aE * (1.0 - w * c), fP = g * (aE * w * c + aP), fA = g * aA, fV = g * aV + q * mExt;
        double parts = fE + fP + fA + fV;
        double sE, sP, sA, sV;
        if (sum > 0) { sE = fE / sum; sP = fP / sum; sA = fA / sum; sV = fV / sum; }
        else { sE = 1.0; sP = 0.0; sA = 0.0; sV = 0.0; }
        double gov = sE > sP ? sE : sP, partner = sE > sP ? sP : sE;
        double others = System.Math.Max(sA, System.Math.Max(sV, partner));
        double safety = gov > 0 ? others / gov : double.NaN;
        return new Result(w, valve, mA, fE, fP, fA, fV, sum, parts, sE, sP, sA, sV, sE + sP, safety);
    }

    internal readonly record struct Verdict(bool Violent, bool Peaceful, bool Immediate, bool Coup, double Nu, double NuCoup);

    internal static Verdict Decide(in Inputs x, in Result r, double landslide)
    {
        double g = x.G > 0 ? x.G : 0.0, q = x.Q > 0 ? x.Q : 0.0;
        double mr = x.MRad > 0 ? x.MRad : 0.0, mo = x.MRefOut > 0 ? x.MRefOut : 0.0;
        double fReg = r.FExec + r.FPart;
        bool ignite = r.FRev > fReg;
        double radicalSide = g * x.Revolt + q * mr, reformSide = q * mo;
        bool violent = ignite && radicalSide > reformSide;
        bool peaceful = ignite && !violent;
        double nu = fReg > 0 ? r.FRev / fReg : double.NaN;
        bool immediate = ignite && landslide > 0 && fReg > 0 && nu >= landslide;
        double aArmy = r.AutonomyIndex * x.Army;
        bool coup = g * aArmy > g * x.Loyal;
        double nuCoup = x.Loyal > 0 ? aArmy / x.Loyal : double.NaN;
        return new Verdict(violent, peaceful, immediate, coup, nu, nuCoup);
    }
}
