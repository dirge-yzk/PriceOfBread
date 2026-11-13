namespace PriceOfBread.SimCore;

internal static class Improvement
{
    internal const string ArtisanRecipeName = "職人";

    internal static int ColumnCount => Catalog.Discoveries.Length + Catalog.Goods.Length;

    internal static int ColumnOf(Producer p)
    {
        int g = p.GoodId;
        if (g < 0 || g >= Catalog.Goods.Length) return -1;
        if (g == Catalog.LaborId || g == Catalog.FarmLaborId || Catalog.Goods[g].Tier == GoodTier.Service) return -1;
        if (p.RecipeName == Simulation.HouseholdRecipeName || p.RecipeName == ArtisanRecipeName) return -1;
        int row = p.DiscoveryRow;
        return row >= 0 ? row : Catalog.Discoveries.Length + g;
    }

    internal static string ColumnName(int k)
    {
        int nr = Catalog.Discoveries.Length;
        if (k < nr) return Catalog.Discoveries[k].Name;
        int g = k - nr;
        return (uint)g < (uint)Catalog.Goods.Length ? Catalog.Goods[g].Name + "/既定" : "?";
    }

    internal static double BracketOf(double techLevel) => 1.0 + System.Math.Min(techLevel, 2.0) + System.Math.Max(0.0, techLevel - 2.0) * 0.3;
}
