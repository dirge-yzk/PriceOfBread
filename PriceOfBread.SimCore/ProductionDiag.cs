using System;
using System.Collections.Generic;

namespace PriceOfBread.SimCore;

public static class ProductionDiag
{
    private const double Eps = 1e-9;


    public readonly record struct PriceParts(
        double RefPrice, double PriceLevel, double SupplyDemand, double MarketPrice,
        double StateMonopoly, double DistributionMargin, double MonopolyRent, double GuildRent, double Insurance, double BuyerPrice,
        double StockRatio, double TariffWedge, double CornLawWedge)
    {
        public double RebuiltMarketPrice => RefPrice * PriceLevel * SupplyDemand;
        public double RebuiltBuyerPrice => MarketPrice * (1 + StateMonopoly + DistributionMargin + MonopolyRent + GuildRent) * (1 - Insurance);
    }

    public static PriceParts ExplainPrice(World w, Region region, int goodId)
    {
        var coef = w.Coef;
        var g = region.GoodOf(goodId);
        double rp = g?.RefPrice ?? 0.0;
        double pl = region.PriceLevel;
        double price = goodId >= 0 && goodId < region.Markets.Length ? region.Markets[goodId].Price : 0.0;
        double anchor = rp * pl;
        double sd = 1.0;
        if (anchor > Eps) sd = price / anchor;
        else { rp = price; pl = 1.0; }
        bool service = g?.Tier == GoodTier.Service;

        double[]? guildShare = EconomyStep.GuildShares(region, coef, w, out double guildDeg);
        var (stateMonop, distM, monRent, guildRent, insure) = EconomyStep.BuyerWedge(region, coef, goodId, service, guildShare, guildDeg);
        double buyer = price * (1 + stateMonop + distM + monRent + guildRent) * (1 - insure);

        var m = goodId >= 0 && goodId < region.Markets.Length ? region.Markets[goodId] : null;
        double stockRatio = m is not null && m.TargetStock > Eps ? m.Stock / m.TargetStock : double.NaN;
        double tariffWedge = InternationalMarket.TariffRate(w, coef);
        bool grain = g is not null && (g.Tier == GoodTier.Staple || g.GrainChain);
        double cornWedge = grain ? InternationalMarket.CornFactor(w.CornLaws) * coef.CornLawImportPenalty : 0.0;

        return new PriceParts(rp, pl, sd, price, stateMonop, distM, monRent, guildRent, insure, buyer,
                              stockRatio, tariffWedge, cornWedge);
    }


    public readonly record struct CostItem(int GoodId, double Qty, double UnitPrice, double Amount, bool IsLabor, double Fill);

    public readonly record struct CostParts(
        CostItem[] Items, double MaterialCost, double LaborCost, double OwnCost, double Mc, bool HasProducer, string Recipe)
    {
        public double Rebuilt => MaterialCost + LaborCost + OwnCost;
    }

    private static (Region Home, Producer P)? FindProducer(World w, Region region, int goodId)
    {
        foreach (var p in region.Producers) if (p.GoodId == goodId) return (region, p);
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, region)) continue;
            foreach (var p in r.Producers) if (p.GoodId == goodId) return (r, p);
        }
        return null;
    }

    public static CostParts ExplainCost(World w, Region region, int goodId) => CostOf(w, region, FindProducer(w, region, goodId), goodId);

    internal static CostParts ExplainCost(World w, Region region, Producer first) => CostOf(w, region, (region, first), first.GoodId);

    public static CostParts[] ExplainCostByRecipe(World w, Region region, int goodId)
    {
        var found = new List<(Region Home, Producer P)>(2);
        void Collect(Region r)
        {
            foreach (var p in r.Producers)
            {
                if (p.GoodId != goodId) continue;
                bool dup = false;
                foreach (var have in found) if (SameRecipe(have.P, p)) { dup = true; break; }
                if (!dup) found.Add((r, p));
            }
        }
        Collect(region);
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, region)) continue;
            Collect(r);
        }
        if (found.Count == 0) return [CostOf(w, region, null, goodId)];
        var order = new int[found.Count];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            int c = DecadeOf(found[a].P).CompareTo(DecadeOf(found[b].P));
            return c != 0 ? c : a.CompareTo(b);
        });
        var outp = new CostParts[found.Count];
        for (int i = 0; i < order.Length; i++) outp[i] = CostOf(w, region, found[order[i]], goodId);
        return outp;
        static int DecadeOf(Producer p) => p.DiscoveryRow is int row and >= 0 ? Catalog.Discoveries[row].Decade : 0;
    }

    private static bool SameRecipe(Producer a, Producer b)
    {
        if (!string.Equals(a.RecipeName, b.RecipeName, StringComparison.Ordinal)) return false;
        if (a.Inputs.Count != b.Inputs.Count) return false;
        for (int i = 0; i < a.Inputs.Count; i++) if (a.Inputs[i].GoodId != b.Inputs[i].GoodId) return false;
        return true;
    }

    private static CostParts CostOf(World w, Region region, (Region Home, Producer P)? found, int goodId)
    {
        var coef = w.Coef;
        double Price(int gid) => gid >= 0 && gid < region.Markets.Length ? region.Markets[gid].Price : 0.0;
        double Fill(int gid) => gid >= 0 && gid < region.Markets.Length ? PoliticalQuantities.Clamp01(region.Markets[gid].LastRation.Value) : 1.0;
        if (found is not { } f)
        {
            var def = goodId >= 0 && goodId < Catalog.Goods.Length ? Catalog.Goods[goodId].Inputs : Array.Empty<Input>();
            var rows = new CostItem[def.Length];
            double mat = 0;
            for (int i = 0; i < def.Length; i++)
            {
                double q = def[i].QtyPerOutput, up = Price(def[i].GoodId), am = q * up;
                rows[i] = new CostItem(def[i].GoodId, q, up, am, Catalog.IsLaborGood(def[i].GoodId), Fill(def[i].GoodId));
                mat += am;
            }
            return new CostParts(rows, mat, 0.0, 0.0, mat, false, "");
        }
        var (home, s) = f;
        var items = new CostItem[s.Inputs.Count];
        double material = 0, labor = 0;
        bool inRange = true;
        for (int i = 0; i < s.Inputs.Count; i++)
        {
            var inp = s.Inputs[i];
            double qty = EconomyStep.EffectiveQty(s, home, inp, coef);
            double unit = Price(inp.GoodId);
            double amount = qty * unit;
            bool lab = Catalog.IsLaborGood(inp.GoodId);
            items[i] = new CostItem(inp.GoodId, qty, unit, amount, lab, Fill(inp.GoodId));
            if (lab) labor += amount; else material += amount;
            if (inp.GoodId < 0 || inp.GoodId >= region.Markets.Length) inRange = false;
        }
        double own = EconomyStep.MineralSelfCost(s, region, coef, w);
        double mc = own + (inRange ? EconomyStep.InputCostPerUnit(s, home, region, coef) : material + labor);
        return new CostParts(items, material, labor, own, mc, true, s.RecipeName);
    }


    public readonly record struct PopUseParts(double Total, double[] ByClass, bool Recorded);

    public static PopUseParts ExplainPopConsumption(World w, int goodId)
    {
        double[] by = [];
        double total = 0;
        bool recorded = false;
        foreach (var region in w.Regions)
        {
            if (goodId < 0 || goodId >= region.Markets.Length) continue;
            if (region.Markets[goodId].PopBought is not { } pb) continue;
            recorded = true;
            if (pb.Length > by.Length) Array.Resize(ref by, pb.Length);
            for (int c = 0; c < pb.Length; c++) { by[c] += pb[c]; total += pb[c]; }
        }
        return new PopUseParts(total, by, recorded);
    }


    public readonly record struct UtilParts(
        double Capacity, double Output, double Utilization,
        double ProfitFactor, double InputFill, double MaterialFill, double InventoryAdjust, double Inertia,
        int Producers, bool Valid)
    {
        public double Rebuilt => ProfitFactor * InputFill * InventoryAdjust * Inertia;
    }

    public static UtilParts ExplainUtilization(World w, int goodId)
    {
        var coef = w.Coef;
        double sCap = 0, sCapP = 0, sCapPF = 0, sTarget = 0, sOut = 0, sOutMat = 0;
        int nProd = 0;
        foreach (var region in w.Regions)
        {
            if (goodId < 0 || goodId >= region.Markets.Length) continue;
            foreach (var s in region.Producers)
            {
                if (s.GoodId != goodId || s.Bankrupt) continue;
                nProd++;
                double mc = EconomyStep.MineralSelfCost(s, region, coef, w) + EconomyStep.InputCostPerUnit(s, region, region, coef);
                double price = region.Markets[s.GoodId].Price;
                double profit = EconomyStep.ProfitFactor(s, price, mc, coef);
                double fill = s.LastInputFill;
                double invAdj = EconomyStep.InventoryAdjust(region, s.GoodId, coef);
                sCap += s.Capacity;
                sCapP += s.Capacity * profit;
                sCapPF += s.Capacity * profit * fill;
                sTarget += s.Capacity * profit * fill * invAdj;
                sOut += s.Output;
                sOutMat += s.Output * s.LastMaterialFill;
            }
        }
        double util = sCap > Eps ? sOut / sCap : 0.0;
        bool valid = sCap > Eps && sCapP > Eps && sCapPF > Eps && sTarget > Eps;
        return new UtilParts(
            Capacity: sCap,
            Output: sOut,
            Utilization: util,
            ProfitFactor: sCap > Eps ? sCapP / sCap : 0.0,
            InputFill: sCapP > Eps ? sCapPF / sCapP : 0.0,
            MaterialFill: sOut > Eps ? sOutMat / sOut : 1.0,
            InventoryAdjust: sCapPF > Eps ? sTarget / sCapPF : 0.0,
            Inertia: sTarget > Eps ? sOut / sTarget : 0.0,
            Producers: nProd,
            Valid: valid);
    }



    public const double TechGateClosed = 0.001;

    public static bool[] UnavailableGoods(MultiNationSim sim, Coefficients coef)
    {
        int n = Catalog.Goods.Length;
        var produced = new double[n];
        var techOpen = new bool[n];
        foreach (var nat in sim.Nations)
        {
            var w = nat.World;
            var has = new bool[n];
            foreach (var region in w.Regions)
            {
                int mn = Math.Min(region.Markets.Length, n);
                for (int gi = 0; gi < mn; gi++) produced[gi] += region.Markets[gi].LastProduced;
                foreach (var s in region.Producers)
                {
                    int gi = s.GoodId;
                    if (gi < 0 || gi >= n) continue;
                    has[gi] = true;
                    if (Discovery.Gate(region, s) > TechGateClosed) techOpen[gi] = true;
                }
            }
            for (int gi = 0; gi < n; gi++)
                if (!has[gi]) { int row = Catalog.DiscoveryRowOf(gi, ""); if (row < 0 || Discovery.Known(w.Discovered, row)) techOpen[gi] = true; }
        }
        var res = new bool[n];
        for (int gi = 0; gi < n; gi++) res[gi] = !techOpen[gi] && !(produced[gi] > coef.TargetStockFloor);
        return res;
    }
}
