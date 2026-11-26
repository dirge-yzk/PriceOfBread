namespace PriceOfBread.SimCore;

public static class Megaprojects
{
    private static readonly Dictionary<string, int> _requiresRow = BuildRequiresRows();
    private static Dictionary<string, int> BuildRequiresRows()
    {
        var d = new Dictionary<string, int>();
        foreach (var c in WorldData.Setup().Chokepoints)
        {
            if (string.IsNullOrEmpty(c.Requires)) continue;
            int row = Catalog.DiscoveryRowNamed(c.Requires);
            if (row < 0) throw new System.InvalidOperationException($"運河〈{c.Id}〉の requires〈{c.Requires}〉が発見の表に無い");
            d[c.Id] = row;
        }
        return d;
    }
    internal static int RequiresRowOf(string id) => _requiresRow.TryGetValue(id, out var r) ? r : -1;
    internal static bool RequirementMet(World w, string id) { int r = RequiresRowOf(id); return r < 0 || Discovery.Known(w.Discovered, r); }

    public static double EstimateCost(Region host, Megaproject mp, Coefficients coef) => 0;
    public static string? StartRefusal(World w, Megaproject mp) => "巨大建造物の機構は無い";
    public static bool TryStart(World w, Megaproject mp) => false;
}
