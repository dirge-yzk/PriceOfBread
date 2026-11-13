namespace PriceOfBread.SimCore;

public static class MinimumWageFloor
{
    private const double Eps = 1e-9;

    internal static bool Applies(MinimumWageLaw law, int laborGoodId)
        => law != MinimumWageLaw.None && (laborGoodId == Catalog.LaborId || laborGoodId == Catalog.FarmLaborId);

    internal static bool InBasket(MinimumWageLaw law, GoodTier tier) => tier switch
    {
        GoodTier.Staple => law >= MinimumWageLaw.Subsistence,
        GoodTier.Daily => law >= MinimumWageLaw.Living,
        GoodTier.Luxury => law >= MinimumWageLaw.Sufficiency,
        _ => false,
    };

    internal static double BasketPerPerson(Region region, MinimumWageLaw law)
    {
        double sum = 0;
        foreach (var g in region.Goods)
        {
            if (!InBasket(law, g.Tier)) continue;
            if (!(g.BaseNeed > 0) || g.Id < 0 || g.Id >= region.Markets.Length) continue;
            var m = region.Markets[g.Id];
            if (!(m.LastDemand > 0)) continue;
            double px = m.Price;
            if (px > 0) sum += g.BaseNeed * px;
        }
        return sum;
    }

    internal static double PerUnit(Region region, int laborGoodId, MinimumWageLaw law)
    {
        if (law == MinimumWageLaw.None || laborGoodId < 0 || laborGoodId >= region.Markets.Length) return 0;
        double cap = LaborSupply(region, laborGoodId, out int lp);
        if (lp < 0 || lp >= region.Pops.Count) return 0;
        if (!(cap > Eps)) return 0;
        double f = BasketPerPerson(region, law) * region.Pops[lp].Size / cap;
        return f > 0 ? f : 0;
    }

    internal static double LaborSupply(Region region, int laborGoodId, out int lp)
    {
        lp = -1;
        double cap = 0;
        foreach (var s in region.Producers)
            if (s.GoodId == laborGoodId && EconomyStep.IsHumanLaborSupply(s) && !s.Bankrupt)
            {
                if (lp < 0) lp = s.LaborPopIndex;
                if (s.LaborPopIndex == lp) cap += s.Capacity;
            }
        return cap;
    }

    internal static double AbsFloor(Region region, int laborGoodId, double ceiling, MinimumWageLaw law)
    {
        double f = PerUnit(region, laborGoodId, law);
        return f > 0 ? System.Math.Min(f, ceiling) : 0;
    }


    internal const double NearFloorMargin = 1.05;

    internal sealed class HomeLabor
    {
        internal Region[] Regions = [];
        internal int[] Lp = [];
        internal double[] Cap = [];
        internal double[] Price = [];
        internal int N;
        internal double Unemployment = double.NaN;
    }

    internal static HomeLabor ReadHomeLabor(World w, long[]? cost = null)
    {
        var h = new HomeLabor();
        int n = w.Regions.Count;
        h.Regions = new Region[n]; h.Lp = new int[n]; h.Cap = new double[n]; h.Price = new double[n];
        double unW = 0, den = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            if (Catalog.LaborId >= r.Markets.Length) continue;
            if (cost is not null) cost[CostLedger.MinimumWageBasketScans] += r.Producers.Count;
            double cap = LaborSupply(r, Catalog.LaborId, out int lp);
            if (lp < 0 || lp >= r.Pops.Count) continue;
            if (!(cap > Eps)) continue;
            double price = r.Markets[Catalog.LaborId].Price;
            if (!(price > 0)) continue;
            var pop = r.Pops[lp];
            h.Regions[h.N] = r; h.Lp[h.N] = lp; h.Cap[h.N] = cap; h.Price[h.N] = price; h.N++;
            unW += pop.Size * pop.UnemploymentRate.Value; den += pop.Size;
        }
        if (den > Eps) h.Unemployment = unW / den;
        return h;
    }

    internal static double BindShare(HomeLabor h, MinimumWageLaw level, long[]? cost = null)
    {
        if (level == MinimumWageLaw.None || h.N == 0) return double.NaN;
        double bound = 0, total = 0;
        for (int i = 0; i < h.N; i++)
        {
            var r = h.Regions[i];
            if (cost is not null) cost[CostLedger.MinimumWageBasketScans] += r.Goods.Count;
            double f = BasketPerPerson(r, level) * r.Pops[h.Lp[i]].Size / h.Cap[i];
            total += h.Cap[i];
            if (f > 0 && h.Price[i] <= NearFloorMargin * f) bound += h.Cap[i];
        }
        return total > Eps ? bound / total : double.NaN;
    }
}
