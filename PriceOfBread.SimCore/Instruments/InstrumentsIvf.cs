namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsIvf.cs#1〕
public static partial class Instruments
{
    internal const int IvfMaxQuarter = 160;
    internal const string IvfHeader = "quarter\tyear\tnation\tpopK\tmainPopK\tcolPopK\tlpmMain\tlpmCol\twagePremMain\twagePremCol\t"
        + "coalOwnCostMain\tcoalPriceOverRefMain\tsteamMcOverWageMain\tlaborProdOverDemandMain\tlaborPriceOverRefMain\t"
        + "bundleSteelOverRefMain\tbundleBuildingOverRefMain\tbundleMonths\tbundleBuildingMonths\tbundleFlips\tbottleneckMonths\tbottleneckRationSum\t"
        + "ironDemand\tironProd\tsteelDemand\tsteelProd\tmachDemand\tmachProd\tcapGoodsCapacity\tcastMachRegions\tcastMachOutput\t"
        + "capRegionsMain\tmachOutAllMain\tmachOutCastMain\tmachPriceOverRefMain\tcolWagePremFloorShare\tcolLpmFloorShare";

    sealed class IvfAcc { public int Months, EraMonths, Flips, BottleMonths, Last = -1; public double BottleSum; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, IvfAcc> IvfAccs = new();

    /// <summary>建設の組を求めた月（EconomyStep の私的な建設の頭＝資本区画があって投資家の POP がある州だけ。追記 2 で工場建設の頭の呼びは外した）。助走の月は数えない。前の月から組が替わったら Flips を 1 足す（最初の月は替わりに数えない）。</summary>
    internal static void IvfChoice(World w, Region r, bool building)
    {
        if (w.WarmupFrozen) return;
        var a = IvfAccs.GetValue(r, _ => new IvfAcc());
        a.Months++; if (building) a.EraMonths++;   // EraMonths〔履歴 InstrumentsIvf.cs#2〕
        int now = building ? 1 : 0;
        if (a.Last >= 0 && a.Last != now) a.Flips++;
        a.Last = now;
    }

    /// <summary>私的な建設の決済（construction＞0 の月）：組の 4 成分の配給率の最小を足す。</summary>
    internal static void IvfConstruction(World w, Region r, double r0, double r1, double r2, double r3)
    {
        if (w.WarmupFrozen) return;
        var a = IvfAccs.GetValue(r, _ => new IvfAcc());
        a.BottleMonths++;
        a.BottleSum += Math.Min(Math.Min(r0, r1), Math.Min(r2, r3));
    }

    static void IvfFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!((picked && q <= IvfMaxQuarter) || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        var c = w.Coef;
        double mainPop = 0, colPop = 0, lpmMain = 0, lpmCol = 0, wpMain = 0, wpCol = 0;
        double coalW = 0, coalOwn = 0, coalPx = 0, steamW = 0, steamRatio = 0, labCapSum = 0, labDemSum = 0, labPxW = 0, labPx = 0;
        double steelW = 0, steelRef = 0, bldRef = 0;
        int months = 0, eraMonths = 0, flips = 0, bMonths = 0; double bSum = 0;
        double ironD = 0, ironP = 0, steelD = 0, steelP = 0, machD = 0, machP = 0, capGoods = 0, castOut = 0; int castRegions = 0;
        int capRegions = 0; double machOutAll = 0, machOutCast = 0, machPxW = 0, machPx = 0, colWageFloorPop = 0, colLpmFloorPop = 0;
        int ng = Catalog.Goods.Length;
        double bundleRef = EconomyStep.BundleRef(c);
        foreach (var r in w.Regions)
        {
            double pop = r.SumPopulation();
            bool mainland = r.Tier != RegionTier.Colonial;
            if (IvfAccs.TryGetValue(r, out var a))
            {
                if (mainland) { months += a.Months; eraMonths += a.EraMonths; flips += a.Flips; bMonths += a.BottleMonths; bSum += a.BottleSum; }   // 追記 2：G5〜G7 の母集団＝本土（植民地を除く）の資本区画のある州（組を求めるのはその州だけ）
                a.Months = a.EraMonths = a.Flips = a.BottleMonths = 0; a.BottleSum = 0;   // 四半期の積算を戻す（Last は持ち越す）
            }
            if (!mainland)
            {
                colPop += pop; lpmCol += pop * r.LocalPriceMultiplier; wpCol += pop * r.WagePremium;
                if (r.WagePremium <= 0.25 + 1e-3) colWageFloorPop += pop;
                if (r.LocalPriceMultiplier <= c.RegionalPriceLevelMin + 1e-6) colLpmFloorPop += pop;
                continue;
            }
            mainPop += pop; lpmMain += pop * r.LocalPriceMultiplier; wpMain += pop * r.WagePremium;
            // 石炭の自己費用と値段（石炭の RGO がある州だけ）
            if (Catalog.CoalId < r.Markets.Length)
            {
                Producer? coal = null;
                foreach (var pr in r.Producers) if (pr.GoodId == Catalog.CoalId && pr.IsRgo) { coal = pr; break; }
                if (coal is not null)
                {
                    coalW += pop; coalOwn += pop * EconomyStep.MineralSelfCost(coal, r, c, w);
                    coalPx += pop * r.Markets[Catalog.CoalId].Price / Math.Max(1e-12, Catalog.RefPrices[Catalog.CoalId] * r.PriceLevel);
                }
            }
            // 労働力
            if (Catalog.LaborId < r.Markets.Length)
            {
                var lm = r.Markets[Catalog.LaborId];
                labCapSum += lm.LastProduced; labDemSum += lm.LastDemand;
                labPxW += pop; labPx += pop * lm.Price / Math.Max(1e-12, Catalog.RefPrices[Catalog.LaborId] * r.PriceLevel);
                foreach (var pr in r.Producers)
                    if (pr.GoodId == Catalog.LaborId && pr.RecipeName == "蒸気機関")
                    {
                        double mc = EconomyStep.MineralSelfCost(pr, r, c, w) + EconomyStep.InputCostPerUnit(pr, r, r, c);
                        steamW += pop; steamRatio += pop * mc / Math.Max(1e-12, lm.Price);
                        break;
                    }
            }
            // 束の費用（資本区画のある州）
            if (r.CapitalEnabled && c.MachineGoodId < ng && c.CementGoodId < ng && c.SteelGoodId < r.Markets.Length && c.MachineGoodId < r.Markets.Length && c.CementGoodId < r.Markets.Length && c.LumberGoodId < r.Markets.Length
                && Catalog.BuildingId < r.Markets.Length)
            {
                double den = Math.Max(1e-12, bundleRef * r.PriceLevel);
                steelW += pop; capRegions++;
                steelRef += pop * EconomyStep.ConsBundle.Steel(c).Cost(r) / den;
                bldRef += pop * EconomyStep.ConsBundle.Building(c).Cost(r) / den;
            }
            // 資本財の需給と能力
            if (Catalog.MachineryId < r.Markets.Length) { machPxW += pop; machPx += pop * r.Markets[Catalog.MachineryId].Price / Math.Max(1e-12, Catalog.RefPrices[Catalog.MachineryId] * r.PriceLevel); }
            if (Catalog.IronId < r.Markets.Length) { ironD += r.Markets[Catalog.IronId].LastDemand; ironP += r.Markets[Catalog.IronId].LastProduced; }
            if (Catalog.SteelId < r.Markets.Length) { steelD += r.Markets[Catalog.SteelId].LastDemand; steelP += r.Markets[Catalog.SteelId].LastProduced; }
            if (Catalog.MachineryId < r.Markets.Length) { machD += r.Markets[Catalog.MachineryId].LastDemand; machP += r.Markets[Catalog.MachineryId].LastProduced; }
            bool hasCast = false;
            foreach (var pr in r.Producers)
            {
                int g = pr.GoodId;
                if (g == Catalog.IronId || g == Catalog.SteelId || g == Catalog.MachineryId || g == Catalog.CementId || g == Catalog.LumberId) capGoods += pr.Capacity;
                if (g == Catalog.MachineryId) { machOutAll += pr.Output; if (pr.RecipeName == "鋳鉄の機械") machOutCast += pr.Output; }
                if (g == Catalog.MachineryId && pr.RecipeName == "鋳鉄の機械") { hasCast = true; if (pr.Output > 1.0) castOut += 1; }
            }
            if (hasCast) castRegions++;
        }
        double Div(double x, double y) => y > 0 ? x / y : double.NaN;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Ivf ??= Open("ivf", seed);
        if (!sk.IvfHeader) { sk.Ivf.WriteLine(IvfHeader); sk.IvfHeader = true; }
        sk.Ivf.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(popK), F(mainPop), F(colPop),
            F(Div(lpmMain, mainPop)), F(Div(lpmCol, colPop)), F(Div(wpMain, mainPop)), F(Div(wpCol, colPop)),
            F(Div(coalOwn, coalW)), F(Div(coalPx, coalW)), F(Div(steamRatio, steamW)), F(Div(labCapSum, labDemSum)), F(Div(labPx, labPxW)),
            F(Div(steelRef, steelW)), F(Div(bldRef, steelW)), I(months), I(eraMonths), I(flips), I(bMonths), F(bSum),
            F(ironD), F(ironP), F(steelD), F(steelP), F(machD), F(machP), F(capGoods), I(castRegions), F(castOut),
            I(capRegions), F(machOutAll), F(machOutCast), F(Div(machPx, machPxW)), F(Div(colWageFloorPop, colPop)), F(Div(colLpmFloorPop, colPop))));
    }
}
