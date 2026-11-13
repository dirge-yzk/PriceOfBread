namespace PriceOfBread.SimCore;

public static class MechanismProbe
{
    public enum Site
    {
        AntiImperialOpinion,
        CoalitionComputed,
        CoalitionRevolutionAlly,
        FactionConcede,
        FactionCompensate,
        FactionRevolt,
        RebelArmsPurchase,
        CivilianArmsLaw,
        Count,
    }

    public static bool Enabled;

    private static readonly long[] Evals = new long[(int)Site.Count];
    private static readonly long[] Hits = new long[(int)Site.Count];

    public static void Eval(Site s) { if (Enabled) System.Threading.Interlocked.Increment(ref Evals[(int)s]); }

    public static void Hit(Site s) { if (Enabled) System.Threading.Interlocked.Increment(ref Hits[(int)s]); }

    public static (long Eval, long Hit) Read(Site s)
        => (System.Threading.Interlocked.Read(ref Evals[(int)s]), System.Threading.Interlocked.Read(ref Hits[(int)s]));

    public static void Reset()
    {
        for (int i = 0; i < (int)Site.Count; i++)
        { System.Threading.Interlocked.Exchange(ref Evals[i], 0); System.Threading.Interlocked.Exchange(ref Hits[i], 0); }
    }
}
