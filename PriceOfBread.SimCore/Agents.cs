namespace PriceOfBread.SimCore;

public interface INationAgent
{
    void DecideQuarter(World world);
}

public sealed class NullAgent : INationAgent
{
    public void DecideQuarter(World world) {  }
}

public sealed class ModernizerAgent : INationAgent
{
    private const double IncomeTaxTarget = 0.25;
    private const double CoercionTarget = 200;

    public void DecideQuarter(World w)
    {
        var p = w.Policy;
        p.EducationLaw = EducationLaw.Public;
        p.EducationSpendingTarget = Government.EducationSpendingTop;
        p.IncomeTaxTarget = IncomeTaxTarget;
        p.CoercionTarget = CoercionTarget;
        if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;
        if (w.Polity != Polity.Absolutist && p.Suffrage < Suffrage.AdultMale) p.Suffrage = Suffrage.AdultMale;
    }
}
