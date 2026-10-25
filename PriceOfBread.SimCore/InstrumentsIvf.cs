namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (536) 初期値の直し（資本の土台）の表 ivf_s（読むだけ・事前登録 measurements/ivf536_20261024_prereg.txt）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsScalars.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。
// 国×四半期の行（挙げた国は q が IvfMaxQuarter まで毎四半期・他の全ての国は年末）。INS=1 のときだけ Quarter から呼ばれる。既存の表に列を足さない（m533_ins の生と比べられる）。
// 列（本土＝州の格が植民地でない稼働中の州・植民地＝格が植民地の稼働中の州。重みは州の人口）：
//   popK・mainPopK・colPopK／lpmMain・lpmCol＝州の物価の乗数 LocalPriceMultiplier の人口加重平均／wagePremMain・wagePremCol＝賃金の割増 WagePremium の人口加重平均（G1・G2）
//   coalOwnCostMain＝石炭の生産者がある本土の州の自己費用（MineralSelfCost・石炭の RGO の先頭の 1 件）の人口加重平均（G3・G4）／coalPriceOverRefMain＝同じ州の石炭の値段÷（参照価格×州の物価水準）
//   steamMcOverWageMain＝動力の〈蒸気機関〉レシピ（労働力の財）の限界費用（自己費用＋投入費）÷労働力の値段（本土・蒸気のレシピがある州・人口加重）（R1）
//   laborProdOverDemandMain＝労働力の財の LastProduced の和÷LastDemand の和（本土）（R4）／laborPriceOverRefMain＝労働力の値段÷（参照価格×州の物価水準）の人口加重平均
//   bundleSteelOverRefMain・bundleBuildingOverRefMain＝資本区画のある本土の州の、鋼の組・建造物の組の今の価格の費用÷（今の旗の組の参照価格 BundleRef×州の物価水準）の人口加重平均（R2・(550) で時代の組から建造物の組へ）
//   bundleMonths・bundleBuildingMonths・bundleFlips＝この四半期に建設の組を求めた州×月の数・そのうち建造物の組が選ばれた数（旗 BuildingGoodBundle が正なら全て）・前の月から組が替わった数（G5・G6）
//   bottleneckMonths・bottleneckRationSum＝建設が走った州×月の数と、その月の組の 4 成分の配給率の最小の和（G7＝和÷数）
//   ironDemand・ironProd・steelDemand・steelProd・machDemand・machProd＝本土の LastDemand／LastProduced の和（R3）
//   capGoodsCapacity＝鉄・鋼・機械・セメント・製材の生産者の Capacity の和（本土・G8）
//   castMachRegions・castMachOutput＝鋳鉄の機械のレシピの生産者がある本土の州の数と、そのうち Output が 1 を超える州の数（G9・追記 2 で分母を capRegionsMain に直した）
//   ★追記 2（反証役の点検の処置・列は全て末尾に足しただけ＝既存の列は変わらない）：capRegionsMain＝資本区画のある本土の州の数（稼働中）／machOutAllMain・machOutCastMain＝本土の機械の生産者の Output の和と、そのうち鋳鉄の機械のレシピの和（G9′＝鋳鉄の機械の割合）
//     machPriceOverRefMain＝機械の値段÷（参照価格×州の物価水準）の人口加重平均（本土）／colWagePremFloorShare・colLpmFloorShare＝植民地の人口のうち賃金の割増が下限 0.25 以下・州の物価の乗数が下限以下の割合（F1 の副作用の記録 R10）
// ⚠限界（計器が自分で申告する）：
//   ・bundle* の月の数は稼働している間の積算＝助走（暦が進まない間）は数えない。組を求めるのは資本区画があって投資家の POP がある州（EconomyStep の資本家の建設の頭）か、工場建設の材料を買う州だけ。
//   ・bottleneckRation は私的な建設（construction＞0 の月）の配給率。国家の建設・巨大建造物・工場建設の材料は含めない。組が時代の組の月は 4 成分が鉄・製材・石灰石・粘土、鋼の組の月は鋼・機械・セメント・製材。
//   ・MineralSelfCost は石炭の RGO の先頭の 1 件を読む（州に 2 件以上あっても 1 件）。蒸気のレシピの限界費用は EconomyStep の mc の式（MineralSelfCost＋InputCostPerUnit）を行を書く時点の価格で再評価した値。
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
        a.Months++; if (building) a.EraMonths++;   // EraMonths＝建造物の組が選ばれた月（(550) で時代の組から意味を替えた・欄の名は据え置き）
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
            if (r.State != RegionState.Active) continue;
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
