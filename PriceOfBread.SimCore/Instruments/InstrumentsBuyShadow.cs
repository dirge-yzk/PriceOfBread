using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 貿易の置き換え T0a の計器（読むだけ・力学の値を書かない・乱数を引かない・エンジンに欄を足さない）。事前登録 measurements/m1124_20261114_prereg_t0.txt。
//   s4cost_s  ＝国×年：単位費用の 3 項（自己費用×為替・品位の分・投入）・為替の偏りの釘づけ・労働の財の値段と需要の 10 年の伸び（歯止めの判断の入力）。
//   s7staple_s＝国×四半期：主食 7 財（Tier＝Staple と GrainChain）の輸入÷需要・不足÷需要・交戦の印。
// 式は事前登録の文から独立に書く（ProfitFactor・InventoryAdjust・SupplyTargetAt を呼ばない）＝BsPHat・BsCostParts。
public static partial class Instruments
{
    internal const string S4costHeaderText = "year\tq\tnation\tfxRate\tpriceLevel\tcostPriceLevel\tproducers\toutputValueGold\tselfFxCostSum\tgradeFxCostSum\tinputFxCostSum\tunitCostFxSumEngine\t"
        + "fxBiasObs\tfxBiasPinnedShare\tfxBiasMeanRaw\ttradeFlowSum\tlaborPriceGold\tlaborDemandQty\tlaborPriceGrowth10y\tlaborDemandGrowth10y";
    internal const string S7stapleHeaderText = "year\tq\tnation\tgoods\timportQty\tdemandQty\tshortageQty\timportOverDemand\tshortageOverDemand\tatWar";

    sealed class BsW
    {
        public double PrevFlow;                                   // エンジンの _prevTradeFlow と同じ起点（0）から、同じ位置で更新する
        public int Obs, Pinned; public double RawSum, FlowSum;    // 今の年の観測（年の行を書いたら 0 へ戻す）
        public readonly Dictionary<int, (double Price, double Demand)> Hist = new();
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, BsW> BsWs = new();
    sealed class BsSink { public System.IO.StreamWriter? W4, W7; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, BsSink> BsSinks = new();

    /// <summary>主食 7 財（Tier＝Staple と GrainChain＝corncf_s と同じ括り）の財の番号。</summary>
    static int[]? _bsStaples;
    internal static int[] BsStaples
    {
        get
        {
            if (_bsStaples is { } s) return s;
            var l = new List<int>();
            for (int g = 0; g < Catalog.Goods.Length; g++) if (Catalog.Goods[g].Tier == GoodTier.Staple || Catalog.Goods[g].GrainChain) l.Add(g);
            return _bsStaples = l.ToArray();
        }
    }

    /// <summary>為替の偏り（World.UpdatePriceLevel の bias の中身）を四半期ごとに再計算する（PriceStep の呼びから＝偏りの前の同じ盤面）。
    /// 偏りはエンジンの局所変数で読めない＝再構成（貿易収支＝累積の差・政策金利・物価水準・実質 GDP の公式を、ここで書き直す）。</summary>
    internal static void BsBiasRead(World w, double gdp)
    {
        if (!Wants("s4cost_s")) return;
        var a = BsWs.GetValue(w, _ => new BsW());
        double flow = w.CumulativeTradeFlow.Value, bal = flow - a.PrevFlow; a.PrevFlow = flow;
        if (w.WarmupFrozen) return;   // 助走の呼びは年の行に混ぜない（起点だけ進める）
        var c = w.Coef;
        double raw = bal / System.Math.Max(1.0, gdp * w.PriceLevel) * c.ExchangeTradeAdjust + c.PolicyRateFxWeight * w.InterestRate;
        a.Obs++; a.RawSum += raw; a.FlowSum += bal;
        if (System.Math.Abs(raw) >= 0.2) a.Pinned++;
    }

    /// <summary>単位費用の 3 項（州の名目・為替を掛ける前）：自己費用（品位の前）・品位の分・投入。和＝MineralSelfCost＋InputCostPerUnit。エンジンの関数を呼ばない。</summary>
    internal static (double Self, double Grade, double Inputs) BsCostParts(Producer s, Region r, Coefficients c, World? w)
    {
        double own = s.MarginalCost * (w?.CostPriceLevel ?? 1.0) * r.LocalPriceMultiplier;
        double grade = 0.0;
        if (c.MineralCostFollowsGrade > 0 && s.IsRgo && Catalog.IsMineral(s.GoodId))
        {
            double gm = r.MineralGradeFactor;
            if (gm > 1e-9) grade = own / gm - own;
        }
        double inputs = 0.0;
        foreach (var inp in s.Inputs)
        {
            double qty = inp.QtyPerOutput;
            if (inp.GoodId == Catalog.LaborId && s.BuysLabor) qty = inp.QtyPerOutput * (1.0 - c.ElecLaborSave * s.ElecFill) * (1.0 - c.LaborSaveRate * r.Mechanization);
            inputs += r.Markets[inp.GoodId].Price * qty;
        }
        return (own, grade, inputs);
    }

    /// <summary>P̂＝供給①の産出を、価格・費用・能力・充足・在庫の生の欄から書き直した値。<paramref name="outputBefore"/>＝その月の Toward の前の産出。
    /// 値段・在庫は供給①の時点の値（清算の後の値を渡すと合わない）。ProfitFactor・InventoryAdjust・SupplyTargetAt は呼ばない。</summary>
    internal static double BsPHat(Producer s, Region r, Coefficients c, World? w, bool baumol, double gate, double outputBefore)
    {
        var (self, grade, inputs) = BsCostParts(s, r, c, w);
        double mc = self + grade + inputs;
        double price = r.Markets[s.GoodId].Price, pf;
        bool humanLabor = (s.GoodId == Catalog.LaborId || s.GoodId == Catalog.FarmLaborId) && s.RecipeName.Length == 0 && s.Inputs.Count == 0;
        if (c.LaborFloorOffGate > 0 && humanLabor)
        {
            double full = mc * (1.0 + c.SupplySlope);
            pf = !(price > 0) ? 0.0 : full > 0 ? System.Math.Min(1.0, price / full) : (full == 0 ? 1.0 : 0.0);
        }
        else pf = price <= mc ? 0.0 : System.Math.Min(1.0, (price - mc) / (mc * c.SupplySlope));
        double target = s.Capacity * s.LastInputFill * pf;
        double adj = 1.0;
        if (c.InventoryAdjustGain > 0)
        {
            var mk = r.Markets[s.GoodId];
            double desired = System.Math.Max(c.TargetStockFloor, c.DesiredInventoryCover * mk.LastDemand);
            adj = System.Math.Max(0.0, 1.0 + c.InventoryAdjustGain * ((desired - mk.Stock) / (desired + mk.Stock + 1e-9)));
        }
        target *= adj;
        double o = outputBefore + c.SupplyAdjustSpeed * (target - outputBefore);
        if (o < 0) o = 0;
        if (!baumol && !(gate > 0)) o = 0;
        else if (!(target > 0) && o < c.TargetStockFloor) o = 0;
        return o;
    }

    /// <summary>prodcap_s の profitFactor の欄：DesiredOutput÷Capacity。Capacity が 0・眠った行・倒産した行は空（0 にしない）。</summary>
    internal static string BsProfitFactorCell(Producer p, Region r, Coefficients c)
    {
        if (p.Bankrupt || !(p.Capacity != 0.0) || EconomyStep.SleepState(r, p, c) == 1) return "";
        return FR(p.DesiredOutput / p.Capacity);
    }

    static void BuyShadowTable(MultiNationSim sim, ulong seed, int q)
    {
        bool w4 = Wants("s4cost_s"), w7 = Wants("s7staple_s");
        if ((!w4 && !w7) || sim.Nations.Count == 0) return;
        int yearLabel = sim.Nations[0].World.Year;
        var sk = BsSinks.GetValue(sim, _ => new BsSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (w4 && q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1)
        {
            if (sk.W4 is null) { sk.W4 = Open("s4cost", seed); sk.W4.WriteLine(S4costHeaderText); }
            foreach (var n in sim.Nations)
            {
                var w = n.World; var c = w.Coef; double fx = w.FxRate;
                double outV = 0, selfS = 0, gradeS = 0, inS = 0, engS = 0; int np = 0;
                double lp = 0, ld = 0; int lpn = 0;
                foreach (var r in w.Regions)
                {
                    foreach (var p in r.Producers)
                    {
                        int g = p.GoodId; double o = p.Output;
                        if (g < 0 || g >= r.Markets.Length || !(o > 0)) continue;
                        var (self, grade, inputs) = BsCostParts(p, r, c, w);
                        np++; outV += o * r.Markets[g].Price * fx; selfS += o * self * fx; gradeS += o * grade * fx; inS += o * inputs * fx;
                        engS += o * (EconomyStep.MineralSelfCost(p, r, c, w) + EconomyStep.InputCostPerUnit(p, r, r, c)) * fx;
                    }
                    if (Catalog.LaborId < r.Markets.Length) { lp += r.Markets[Catalog.LaborId].Price * fx; ld += r.Markets[Catalog.LaborId].LastDemand; lpn++; }
                }
                double lpMean = lpn > 0 ? lp / lpn : double.NaN;
                var a = BsWs.GetValue(w, _ => new BsW());
                a.Hist[yearLabel] = (lpMean, ld);
                double lpg = a.Hist.TryGetValue(yearLabel - 10, out var h10) && h10.Price > 0 ? lpMean / h10.Price - 1.0 : double.NaN;
                double ldg = a.Hist.TryGetValue(yearLabel - 10, out var h10b) && h10b.Demand > 0 ? ld / h10b.Demand - 1.0 : double.NaN;
                sk.W4.WriteLine(string.Join('\t', I(yearLabel), I(q), NameOf(w), FR(fx), FR(w.PriceLevel), FR(w.CostPriceLevel), I(np), FR(outV), FR(selfS), FR(gradeS), FR(inS), FR(engS),
                    I(a.Obs), a.Obs > 0 ? FR((double)a.Pinned / a.Obs) : "nan", a.Obs > 0 ? FR(a.RawSum / a.Obs) : "nan", FR(a.FlowSum), FR(lpMean), FR(ld), FR(lpg), FR(ldg)));
                a.Obs = 0; a.Pinned = 0; a.RawSum = 0; a.FlowSum = 0;
            }
        }
        if (w7 && NgAccs.TryGetValue(sim.Market, out var ng) && ng.Any)
        {
            if (sk.W7 is null) { sk.W7 = Open("s7staple", seed); sk.W7.WriteLine(S7stapleHeaderText); }
            var rowWorlds = new List<World>(ng.Worlds);
            foreach (var n in sim.Nations) if (!ng.Idx.ContainsKey(n.World)) rowWorlds.Add(n.World);
            var st = BsStaples;
            foreach (var w in rowWorlds)
            {
                int ci = ng.Idx.TryGetValue(w, out var ix0) ? ix0 : -1;
                double imp = 0, dem = 0, sh = 0; int gn = 0;
                foreach (int g in st)
                {
                    if (g >= ng.Gc) continue;
                    gn++;
                    if (ci >= 0) imp += ng.ImpQ[ci * ng.Gc + g];
                    foreach (var r in w.Regions) if (g < r.Markets.Length) { dem += r.Markets[g].LastDemand; sh += r.Markets[g].LastShortage; }
                }
                sk.W7.WriteLine(string.Join('\t', I(yearLabel), I(q), NameOf(w), I(gn), F(imp), F(dem), F(sh), dem > 0 ? FR(imp / dem) : "nan", dem > 0 ? FR(sh / dem) : "nan", w.AtWarNow ? "1" : "0"));
            }
        }
    }

    static void BuyShadowClose(MultiNationSim sim) { if (BsSinks.TryGetValue(sim, out var sk)) { sk.W4?.Dispose(); sk.W4 = null; sk.W7?.Dispose(); sk.W7 = null; } }
}
