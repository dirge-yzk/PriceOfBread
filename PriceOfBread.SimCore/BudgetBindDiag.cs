namespace PriceOfBread.SimCore;

public static class BudgetBindDiag
{
    public static bool On;

    public static readonly Dictionary<World, double[]> ByWorld = new();

    public const int Rows = 0;
    public const int BoundRows = 1;
    public const int DesiredVal = 2;
    public const int DemandVal = 3;
    public const int DesiredValTrade = 4;
    public const int DemandValTrade = 5;
    public const int DesiredValStaple = 6;
    public const int DemandValStaple = 7;
    public const int DesiredValDaily = 8;
    public const int DemandValDaily = 9;
    public const int DesiredValLux = 10;
    public const int DemandValLux = 11;
    public const int BudgetStart = 12;
    public const int BudgetLeft = 13;
    public const int PaidVal = 14;
    public const int PopDemVal = 15;
    public const int InputDemVal = 16;
    public const int InvestDemVal = 17;
    public const int MilDemVal = 18;
    public const int Count = 19;

    public static double[] For(World w)
    {
        if (ByWorld.TryGetValue(w, out var a)) return a;
        a = new double[Count]; ByWorld[w] = a; return a;
    }

    public static void Begin() { foreach (var a in ByWorld.Values) System.Array.Clear(a); }

    public static void Row(World? w, int tier, bool tradeable, double desired, double demand, double buyerPrice)
    {
        if (w is null) return;
        var a = For(w);
        double dv = desired * buyerPrice, mv = demand * buyerPrice;
        a[Rows]++;
        if (demand < desired - 1e-12) a[BoundRows]++;
        a[DesiredVal] += dv; a[DemandVal] += mv;
        if (tradeable) { a[DesiredValTrade] += dv; a[DemandValTrade] += mv; }
        switch (tier)
        {
            case 0: a[DesiredValStaple] += dv; a[DemandValStaple] += mv; break;
            case 1: a[DesiredValDaily] += dv; a[DemandValDaily] += mv; break;
            default: a[DesiredValLux] += dv; a[DemandValLux] += mv; break;
        }
    }

    public static void Compose(World? w, double pop, double input, double invest, double mil)
    {
        if (w is null) return;
        var a = For(w);
        a[PopDemVal] += pop; a[InputDemVal] += input; a[InvestDemVal] += invest; a[MilDemVal] += mil;
    }

    public static void Budget(World? w, double start, double left, double paid)
    {
        if (w is null) return;
        var a = For(w);
        a[BudgetStart] += start; a[BudgetLeft] += left; a[PaidVal] += paid;
    }
}
