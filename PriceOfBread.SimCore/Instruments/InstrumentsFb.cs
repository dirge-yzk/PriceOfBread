namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsFb.cs#1〕
public static partial class Instruments
{
    static readonly string[] FbRecipes = ["", "煉瓦造", "鋳鉄の骨組み", "鋼の骨組み", "鉄筋コンクリート造"];   // 先頭の空＝既定（木造・RecipeName が空）

    internal const string FbHeader = "quarter\tyear\tnation\tpopK\tmainPopK\tregions\toutWood\toutBrick\toutCast\toutSteel\toutRC\toutTotal\tgateCast\tgateSteel\tgateRC\t"
        + "costWood\tcostBrick\tcostCast\tcostSteel\tcostRC\tbuildPxOverRefMain\tbuildStockMean\tbuildDemand\tbuildProd\tlumDemand\tlumProd\tlimDemand\tlimProd\tclayDemand\tclayProd\tcementDemand\tcementProd";

    static void FbFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(w.Coef.BuildingGoodBundle > 0)) return;
        if (!(picked || yearEnd)) return;
        if (w.WarmupFrozen) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;
        var out5 = new double[FbRecipes.Length];
        int gateCast = 0, gateSteel = 0, gateRC = 0, regions = 0, stockN = 0;
        double mainPop = 0, pxW = 0, px = 0, stockSum = 0, bD = 0, bP = 0, lumD = 0, lumP = 0, limD = 0, limP = 0, clD = 0, clP = 0, cmD = 0, cmP = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            if (!(Catalog.BuildingId < r.Markets.Length)) continue;
            double pop = r.SumPopulation();
            mainPop += pop;
            bool has = false;
            foreach (var pr in r.Producers)
            {
                if (pr.GoodId != Catalog.BuildingId) continue;
                has = true;
                int k = System.Array.IndexOf(FbRecipes, pr.RecipeName);
                if (k >= 0) out5[k] += pr.Output;
                if (Discovery.Gate(r, pr) > 0) { if (k == 2) gateCast++; else if (k == 3) gateSteel++; else if (k == 4) gateRC++; }
            }
            if (has) regions++;
            var bm = r.Markets[Catalog.BuildingId];
            px += pop * bm.Price / System.Math.Max(1e-12, Catalog.RefPrices[Catalog.BuildingId] * r.PriceLevel); pxW += pop;
            stockSum += bm.Stock; stockN++;
            bD += bm.LastDemand; bP += bm.LastProduced;
            if (Catalog.LumberId < r.Markets.Length) { lumD += r.Markets[Catalog.LumberId].LastDemand; lumP += r.Markets[Catalog.LumberId].LastProduced; }
            if (Catalog.LimestoneId < r.Markets.Length) { limD += r.Markets[Catalog.LimestoneId].LastDemand; limP += r.Markets[Catalog.LimestoneId].LastProduced; }
            if (Catalog.ClayId < r.Markets.Length) { clD += r.Markets[Catalog.ClayId].LastDemand; clP += r.Markets[Catalog.ClayId].LastProduced; }
            if (Catalog.CementId < r.Markets.Length) { cmD += r.Markets[Catalog.CementId].LastDemand; cmP += r.Markets[Catalog.CementId].LastProduced; }
        }
        double tot = 0; foreach (double v in out5) tot += v;
        var cap = w.Region; var cost5 = new double[FbRecipes.Length]; System.Array.Fill(cost5, double.NaN);
        if (Catalog.BuildingId < cap.Markets.Length)
            foreach (var pr in cap.Producers)
            {
                if (pr.GoodId != Catalog.BuildingId) continue;
                int k = System.Array.IndexOf(FbRecipes, pr.RecipeName);
                if (k < 0 || !double.IsNaN(cost5[k]) || !(Discovery.Gate(cap, pr) > 0)) continue;
                cost5[k] = EconomyStep.MineralSelfCost(pr, cap, w.Coef, w) + EconomyStep.InputCostPerUnit(pr, cap, cap, w.Coef);
            }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Fb ??= Open("fb", seed);
        if (!sk.FbHeader) { sk.Fb.WriteLine(FbHeader); sk.FbHeader = true; }
        sk.Fb.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(popK), F(mainPop), I(regions),
            F(out5[0]), F(out5[1]), F(out5[2]), F(out5[3]), F(out5[4]), F(tot), I(gateCast), I(gateSteel), I(gateRC),
            F(cost5[0]), F(cost5[1]), F(cost5[2]), F(cost5[3]), F(cost5[4]),
            F(pxW > 0 ? px / pxW : double.NaN), F(stockN > 0 ? stockSum / stockN : double.NaN), F(bD), F(bP), F(lumD), F(lumP), F(limD), F(limP), F(clD), F(clP), F(cmD), F(cmP)));
    }
}
