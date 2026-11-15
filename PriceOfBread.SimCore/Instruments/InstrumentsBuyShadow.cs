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
    internal static double BsPHat(Producer s, Region r, Coefficients c, World? w, bool baumol, double gate, double outputBefore, double priceOv = double.NaN)
    {
        var (self, grade, inputs) = BsCostParts(s, r, c, w);
        double mc = self + grade + inputs;
        double price = double.IsNaN(priceOv) ? r.Markets[s.GoodId].Price : priceOv, pf;   // priceOv＝T0b が値段を差し替えて読む（既定 NaN＝今までの呼びと同じ値）
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

// ---- T0b（(1131)）：U1〈成約ごとの上界の越え〉と S2′〈行の門の 6 区分〉。事前登録 measurements/m1131_20261114_prereg_tb.txt。
// 読むだけ・力学の値を書かない・乱数を引かない・エンジンに欄を足さない。P̂ は BsPHat（事前登録の式の書き直し）で、エンジンの純関数を呼ばない。
public static partial class Instruments
{
    internal const string TbHeaderText = "year\tq\tnation\trows\tunclassified\tsk0\tsk1\tsk2\tps0\tps1\tps2\tdsk0\tdsk1\tdsk2\tdps0\tdps1\tdps2\ta1Close\ta1CloseD\tsmallD\tsmallDSkip\tevalSkip\tevalAll\tevalDone\t"
        + "u1Deals\tu1Rel\tu1Abs\tu1SmallD\tu1SmallDRel\tu1MaxRel\tu1Gt1pct\tu1Gt10pct\tu1ExcessQty\tu1Qty\timpQ\trexQ\tr4q\tr4qImpGoods";
    internal const double TbTolRel = 1e-9, TbTolAbs = 1e-6;

    sealed class TbN
    {
        public long Rows, Uncl, A1Close, SmallD, SmallDSkip, EvalSkip, EvalAll, EvalDone, U1Deals, U1Rel, U1Abs, U1SmallD, U1SmallDRel, U1G1, U1G10;
        public readonly long[] Cls = new long[6]; public readonly double[] ClsD = new double[6];
        public double A1CloseD, U1MaxRel = double.NegativeInfinity, U1Excess, U1Qty;
    }
    sealed class TbReg { public World W = null!; public double Imp, Exp; }
    sealed class TbState
    {
        public readonly Dictionary<World, TbN> N = new(); public readonly Dictionary<(Region, int), TbReg> Reg = new();
        public System.IO.StreamWriter? W; public World? LastW; public TbN? LastN;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, TbState> TbStates = new();
    sealed class TbPc { public int Epoch = -1, Count = -1; public Producer[][] By = System.Array.Empty<Producer[]>(); }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Region, TbPc> TbPcs = new();
    static int _tbEpoch;

    /// <summary>州の生産者を財ごとに並べた一覧（四半期ごとに作り直す・件数が変わっても作り直す）。</summary>
    static Producer[] TbProducers(Region r, int g)
    {
        var pc = TbPcs.GetValue(r, _ => new TbPc());
        if (pc.Epoch != _tbEpoch || pc.Count != r.Producers.Count)
        {
            int ng = Catalog.Goods.Length; var cnt = new int[ng];
            foreach (var p in r.Producers) if ((uint)p.GoodId < (uint)ng) cnt[p.GoodId]++;
            var by = new Producer[ng][];
            for (int i = 0; i < ng; i++) by[i] = cnt[i] == 0 ? System.Array.Empty<Producer>() : new Producer[cnt[i]];
            System.Array.Clear(cnt);
            foreach (var p in r.Producers) if ((uint)p.GoodId < (uint)ng) by[p.GoodId][cnt[p.GoodId]++] = p;
            pc.By = by; pc.Epoch = _tbEpoch; pc.Count = r.Producers.Count;
        }
        return (uint)g < (uint)pc.By.Length ? pc.By[g] : System.Array.Empty<Producer>();
    }

    /// <summary>P̂(u)＝州×財の生産者の P̂ の和（財の値段だけ u に差し替える）。眠った生産者・倒産した生産者はエンジンが飛ばす＝今の産出のまま足す。</summary>
    internal static double TbPHatSum(Region r, int g, Coefficients c, World w, double u)
    {
        double sum = 0;
        foreach (var p in TbProducers(r, g))
        {
            if (p.Bankrupt || EconomyStep.SleepState(r, p, c) == 1) { sum += p.Output; continue; }
            bool isLabor = p.GoodId is Catalog.LaborId or Catalog.FarmLaborId;
            bool baumol = Catalog.Goods[p.GoodId].Tier == GoodTier.Service && !isLabor;
            sum += BsPHat(p, r, c, w, baumol, Discovery.Gate(r, p), p.Output, u);
        }
        return sum;
    }

    /// <summary>行の門の 6 区分の判定（互いに素な 3 つの述語）。u_min＝min(lp, 最安の売り手の値段×lbMulMin)。</summary>
    internal static (bool Ok, bool B0, bool B1, bool B2, bool Eval, double XOld, double UbBb, double UbA1) TbClassify(Region r, int g, Market m, Coefficients c, World w, double lp, double rpT, double headPrice, double lbMulMin)
    {
        double ts = m.TargetStock, d = m.LastDemand, p = m.LastProduced, s = m.Stock;
        double xOld = ts + d - p - s;
        double um = lp, hm = headPrice * lbMulMin;
        if (!double.IsNaN(hm) && hm < um) um = hm;
        if (!(rpT > 1e-9) || !(um > 1e-9) || double.IsInfinity(um)) return (false, false, false, false, false, xOld, double.NaN, double.NaN);
        double root = System.Math.Pow(rpT / um, c.PriceElasticity);
        double ubBb = ts + d * root - p - s, ubA1 = ubBb; bool ev = false;
        bool b0 = ubBb > 0;
        if (!b0) { double q = System.Math.Min(p, TbPHatSum(r, g, c, w, um)); ubA1 = ts + d * root - q - s; ev = true; }
        return (true, b0, !b0 && ubA1 > 0, !b0 && !(ubA1 > 0), ev, xOld, ubBb, ubA1);   // (1159) 否定形＝NaN の上界は閉じた側（B2）へ＝Ok の行は B0・B1・B2 のちょうど 1 つ（旧は在庫の欄が NaN で 3 つとも偽の隙間）
    }

    /// <summary>成約の上界の越え。S は成約の後の台帳（m.Stock）。E_abs＝S−UB・E_rel＝E_abs÷max(D, TS)。</summary>
    internal static (bool Ok, double Ub, double EAbs, double ERel) TbU1(Region r, int g, Market m, Coefficients c, World w, double lp, double unitA, double rpT, double qA)
    {
        double u = System.Math.Min(unitA, lp);
        if (!(rpT > 1e-9) || !(u > 1e-9) || double.IsInfinity(u)) return (false, double.NaN, double.NaN, double.NaN);
        double root = System.Math.Pow(rpT / u, c.PriceElasticity);
        double q = System.Math.Min(m.LastProduced, TbPHatSum(r, g, c, w, u));
        double ub = m.TargetStock + m.LastDemand * root - q;
        double eAbs = m.Stock - ub, den = System.Math.Max(m.LastDemand, m.TargetStock);
        return (true, ub, eAbs, den > 0 ? eAbs / den : double.NaN);
    }

    static TbN TbNOf(TbState st, World w)
    {
        if (ReferenceEquals(st.LastW, w) && st.LastN is { } ln) return ln;
        if (!st.N.TryGetValue(w, out var a)) st.N[w] = a = new TbN();
        st.LastW = w; st.LastN = a; return a;
    }

    /// <summary>行の門（旧の門）の直前の呼び。</summary>
    internal static void TbRow(InternationalMarket mk, World w, Region r, int g, Market m, Coefficients c, double lp, double rpT, double headPrice, double lbMulMin)
    {
        if (w.WarmupFrozen || !Wants("tb_s")) return;
        var a = TbNOf(TbStates.GetValue(mk, _ => new TbState()), w);
        var x = TbClassify(r, g, m, c, w, lp, rpT, headPrice, lbMulMin);
        bool skipped = !(x.XOld > 0); int b = skipped ? 0 : 3; double d = m.LastDemand;
        a.Rows++; a.EvalAll++; if (skipped) a.EvalSkip++;
        if (d < c.TargetStockFloor) { a.SmallD++; if (skipped) a.SmallDSkip++; }
        if (!x.Ok) { a.Uncl++; return; }
        if (x.Eval) a.EvalDone++;
        if (x.B0) { a.Cls[b]++; a.ClsD[b] += d; }
        if (x.B1) { a.Cls[b + 1]++; a.ClsD[b + 1] += d; }
        if (x.B2) { a.Cls[b + 2]++; a.ClsD[b + 2] += d; if (x.XOld > 0) { a.A1Close++; a.A1CloseD += d; } }
    }

    /// <summary>成約の直後の呼び（m.Stock は成約の後）。</summary>
    internal static void TbDeal(InternationalMarket mk, World w, World sellerW, Region r, Region sellerR, int g, Market m, Coefficients c, double lp, double unitA, double rpT, double qA, bool cross)
    {
        if (w.WarmupFrozen || !Wants("tb_s")) return;
        var st = TbStates.GetValue(mk, _ => new TbState());
        var a = TbNOf(st, w);
        var u = TbU1(r, g, m, c, w, lp, unitA, rpT, qA);
        a.U1Qty += qA;
        if (u.Ok)
        {
            a.U1Deals++;
            bool rel = u.ERel > TbTolRel, small = m.LastDemand < c.TargetStockFloor;
            if (rel) a.U1Rel++;
            if (u.EAbs > TbTolAbs) a.U1Abs++;
            if (small) { a.U1SmallD++; if (rel) a.U1SmallDRel++; }
            if (u.ERel > a.U1MaxRel) a.U1MaxRel = u.ERel;
            if (u.ERel > 0.01) a.U1G1++;
            if (u.ERel > 0.1) a.U1G10++;
            if (u.EAbs > 0) a.U1Excess += u.EAbs;
        }
        if (cross)
        {
            if (!st.Reg.TryGetValue((r, g), out var bi)) st.Reg[(r, g)] = bi = new TbReg { W = w };
            bi.Imp += qA;
            if (!st.Reg.TryGetValue((sellerR, g), out var se)) st.Reg[(sellerR, g)] = se = new TbReg { W = sellerW };
            se.Exp += qA;
        }
    }

    static void TbTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("tb_s") || sim.Nations.Count == 0) return;
        _tbEpoch++;
        if (!TbStates.TryGetValue(sim.Market, out var st)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string L(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (st.W is null) { st.W = Open("tb", seed); st.W.WriteLine(TbHeaderText); }
        var agg = new Dictionary<(World, int), (double Imp, double Rex)>();
        foreach (var kv in st.Reg)
        {
            var key = (kv.Value.W, kv.Key.Item2);
            agg.TryGetValue(key, out var v);
            agg[key] = (v.Imp + kv.Value.Imp, v.Rex + System.Math.Min(kv.Value.Imp, kv.Value.Exp));
        }
        int yearLabel = sim.Nations[0].World.Year;
        foreach (var n in sim.Nations)
        {
            var w = n.World; st.N.TryGetValue(w, out var a); a ??= new TbN();
            double impQ = 0, rexQ = 0; int r4 = 0, r4g = 0;
            foreach (var kv in agg)
            {
                if (!ReferenceEquals(kv.Key.Item1, w)) continue;
                int g = kv.Key.Item2; impQ += kv.Value.Imp; rexQ += kv.Value.Rex;
                if (!(kv.Value.Imp > 0)) continue;
                r4g++;
                double d3 = 0; foreach (var rg in w.Regions) if (g < rg.Markets.Length) d3 += rg.Markets[g].LastDemand;
                d3 *= Calendar.MonthsPerQuarter;
                if (kv.Value.Imp > 2.0 * (d3 + kv.Value.Rex)) r4++;
            }
            var f = new List<string> { I(yearLabel), I(q), NameOf(w), L(a.Rows), L(a.Uncl) };
            for (int i = 0; i < 6; i++) f.Add(L(a.Cls[i]));
            for (int i = 0; i < 6; i++) f.Add(FR(a.ClsD[i]));
            f.Add(L(a.A1Close)); f.Add(FR(a.A1CloseD)); f.Add(L(a.SmallD)); f.Add(L(a.SmallDSkip)); f.Add(L(a.EvalSkip)); f.Add(L(a.EvalAll)); f.Add(L(a.EvalDone));
            f.Add(L(a.U1Deals)); f.Add(L(a.U1Rel)); f.Add(L(a.U1Abs)); f.Add(L(a.U1SmallD)); f.Add(L(a.U1SmallDRel));
            f.Add(a.U1Deals > 0 ? FR(a.U1MaxRel) : "nan"); f.Add(L(a.U1G1)); f.Add(L(a.U1G10)); f.Add(FR(a.U1Excess)); f.Add(FR(a.U1Qty));
            f.Add(FR(impQ)); f.Add(FR(rexQ)); f.Add(I(r4)); f.Add(I(r4g));
            st.W.WriteLine(string.Join('\t', f));
        }
        st.N.Clear(); st.Reg.Clear(); st.LastW = null; st.LastN = null;
    }

    static void TbClose(MultiNationSim sim) { if (TbStates.TryGetValue(sim.Market, out var st)) { st.W?.Dispose(); st.W = null; } }
}

// ---- T1（(1144) c112t1）：t1buy_s＝A-1 の腕の上側の門 U1〈成約ごとの上界の越え〉と W2(a)〈旧の門で飛ばし、かつ UB_Bb≦0＜UB_A1 の行の越境の成約〉。事前登録 measurements/m1144_20261115_prereg_t1.txt。
// 読むだけ・力学の値を書かない・乱数を引かない。P̂ は計器が事前登録の式から自分で書き直す（エンジンの A1PHat・ProfitFactor を呼ばない）。入力は供給①の写し（Producer の T1OutPrev・T1Base・T1Mc）。
// 生産者の一覧は計器の TbProducers（州×財・計器が自分で選ぶ）。値段は買い手の国の為替で州の名目へ直す（u′÷為替）。エンジンの値（門の P̂・成約の Q）は照合（自己検査）にだけ使う。
public static partial class Instruments
{
    internal const string T1buyHeaderText = "year\tq\tnation\trows\toldSkip\tinstSkip\tunclassified\tpopA\tpopAPx\tpopASmallD\tpopADealX\tpopAQtyX\tpopAValX\tsmallD\tgateEval\tgateMismatch\t"
        + "u1Deals\tu1Unok\tu1Viol\tu1ViolAbs\tu1MaxRel\tu1SmallD\tu1SmallDViol\tu1Excess\tu1Qty\tselfN\tselfDiff\tselfMaxRel\tretreatDeals\tretreatQty\tdealsX\tqtyX"
        + "\tpopC\tpopCUb\tpopCPart0\tpopCDealX\tpopCPart0DealX\tretreatQtySum\tretreatDealsTol\tretreatDealsRaw\tuBelowMin";   // (1159) 段 T1-0 の列（事前登録 measurements/m1159_20261115_prereg_t1.txt [1] ③2）
    internal const double T1TolRel = 1e-9, T1TolAbs = 1e-6;

    sealed class T1N
    {
        public long Rows, OldSkip, InstSkip, Uncl, PopA, PopAPx, PopASmallD, PopADealX, SmallD, GateEval, GateMis, U1Deals, U1Unok, U1Viol, U1ViolAbs, U1SmallD, U1SmallDViol, SelfN, SelfDiff, RetreatDeals, DealsX;
        public double PopAQtyX, PopAValX, U1MaxRel = double.NegativeInfinity, U1Excess, U1Qty, SelfMaxRel, RetreatQty, QtyX;
        public long PopC, PopCUb, PopCPart0, PopCDealX, PopCPart0DealX, RetreatDealsTol, RetreatDealsRaw, UBelowMin;   // (1159)
        public double RetreatQtySum;   // (1159)
    }
    sealed class T1State
    {
        public readonly Dictionary<World, T1N> N = new();
        public System.IO.StreamWriter? W; public World? LastW; public T1N? LastN;
        public bool CurPop, CurDealt;   // 今の行が W2(a) の母集合か・越境で成約したか（次の行の門の呼びで置き直す）
        public bool CurC, CurCPart0, CurDealtC; public double CurUMin = double.NaN;   // (1159) 今の行が W2(a)′ の母集合（popCUb）か・その sk0 の側か・越境で成約したか・行の門の時点の u_min（NaN＝区分できない行）
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<InternationalMarket, T1State> T1States = new();

    /// <summary>計器の P̂(pLoc)＝州×財の生産者の〈供給①を値段 pLoc で読み直した来月の産出〉の和（事前登録の式の書き直し）。
    /// 写しが 3 つとも 0 の生産者は今の産出のまま・T1Base＜0（未発明）は 0・目標＝T1Base×採算・x＋k×(目標−x)・負は 0・目標が正でなく床未満は 0。</summary>
    internal static double T1PHat(Region r, int g, Coefficients c, double pLoc)
    {
        double sum = 0;
        foreach (var p in TbProducers(r, g))
        {
            double b = p.T1Base, o0 = p.T1OutPrev, mc = p.T1Mc;
            if (b == 0 && o0 == 0 && mc == 0) { sum += p.Output; continue; }
            if (b < 0) continue;
            double pf;
            bool humanLabor = (p.GoodId == Catalog.LaborId || p.GoodId == Catalog.FarmLaborId) && p.RecipeName.Length == 0 && p.Inputs.Count == 0;
            if (c.LaborFloorOffGate > 0 && humanLabor)
            {
                double full = mc * (1.0 + c.SupplySlope);
                pf = !(pLoc > 0) ? 0.0 : full > 0 ? System.Math.Min(1.0, pLoc / full) : (full == 0 ? 1.0 : 0.0);
            }
            else pf = pLoc <= mc ? 0.0 : System.Math.Min(1.0, (pLoc - mc) / (mc * c.SupplySlope));
            double target = b * pf;
            double o = o0 + c.SupplyAdjustSpeed * (target - o0);
            if (o < 0) o = 0;
            else if (!(target > 0) && o < c.TargetStockFloor) o = 0;
            sum += o;
        }
        return sum;
    }

    static T1N T1NOf(T1State st, World w)
    {
        if (ReferenceEquals(st.LastW, w) && st.LastN is { } ln) return ln;
        if (!st.N.TryGetValue(w, out var a)) st.N[w] = a = new T1N();
        st.LastW = w; st.LastN = a; return a;
    }

    /// <summary>行の門の直前の呼び：旧の門・計器の A-1 の門・W2(a) の母集合。engSkip＝エンジンの門の結果・engPh0＝エンジンが門で評価した P̂（評価しなければ NaN）。</summary>
    internal static void T1Row(InternationalMarket mk, World w, Region r, int g, Market m, double lp, double rpT, double headPrice, double lbMulMin, double fx, bool a1On, bool engSkip, double engPh0)
    {
        if (w.WarmupFrozen || !Wants("t1buy_s")) return;
        var st = T1States.GetValue(mk, _ => new T1State());
        var a = T1NOf(st, w); var c = w.Coef;
        double ts = m.TargetStock, d = m.LastDemand, p = m.LastProduced, s = m.Stock, q = p;
        bool oldSkip = !(ts + d - p - s > 0);
        a.Rows++; if (oldSkip) a.OldSkip++;
        bool small = d < c.TargetStockFloor; if (small) a.SmallD++;
        st.CurPop = false; st.CurDealt = false;
        st.CurC = false; st.CurCPart0 = false; st.CurDealtC = false; st.CurUMin = double.NaN;
        double um = lp, hm = headPrice * lbMulMin;
        if (!double.IsNaN(hm) && hm < um) um = hm;
        bool instSkip;
        if (!(rpT > 1e-9) || !(um > 1e-9) || double.IsInfinity(um)) { a.Uncl++; instSkip = oldSkip; }
        else
        {
            double root = System.Math.Pow(rpT / um, c.PriceElasticity);
            double ubBb = ts + d * root - p - s, ubA1 = ubBb;
            if (!(ubBb > 0))
            {
                a.GateEval++;
                double ph = T1PHat(r, g, c, um / fx);
                q = System.Math.Min(p, ph); if (!(q >= 0)) q = p;
                ubA1 = ts + d * root - q - s;
                if (a1On && !double.IsNaN(engPh0)) { a.SelfN++; if (ph != engPh0) { a.SelfDiff++; double rel = System.Math.Abs(ph - engPh0) / System.Math.Max(1e-300, System.Math.Abs(engPh0)); if (rel > a.SelfMaxRel) a.SelfMaxRel = rel; } }
            }
            instSkip = !(ubA1 > 0);
            if (oldSkip && !(ubBb > 0) && ubA1 > 0)
            {
                st.CurPop = true; a.PopA++; if (small) a.PopASmallD++;
                if (d * System.Math.Pow(rpT / um, 2.0 * c.PriceElasticity) - (q + m.LastImport) > 0) a.PopAPx++;   // 値段の側の上界も正（成約の必要条件＝xPrice≦D×(rpT÷u_min)^{2ε}−(Q(u_min)＋I)）
            }
            st.CurUMin = um;   // (1159) 下界の番人 uBelowMin が成約ごとに読む
            if (oldSkip && T1HBelow(headPrice, lbMulMin, lp))
            {   // (1159) W2(a)′ の母集合 N_C＝旧の門が閉じ h_min＜lp。その中で UB_A1(u_min)＞0＝popCUb・UB_Bb(u_min)＞0＝popCPart0（sk0 の側）
                a.PopC++;
                if (ubA1 > 0) { a.PopCUb++; st.CurC = true; if (ubBb > 0) { a.PopCPart0++; st.CurCPart0 = true; } }
            }
        }
        if (instSkip) a.InstSkip++;
        if (a1On && instSkip != engSkip) a.GateMis++;
    }

    /// <summary>成約の直後の呼び（m.Stock は成約の後）。engQ＝エンジンがこの成約に使った Q（旗 0 は NaN）。</summary>
    internal static void T1Deal(InternationalMarket mk, World w, Region r, int g, Market m, double lp, double unitA, double rpT, double qA, bool cross, double vA, double fx, double engQ)
    {
        if (w.WarmupFrozen || !Wants("t1buy_s")) return;
        var st = T1States.GetValue(mk, _ => new T1State());
        var a = T1NOf(st, w); var c = w.Coef;
        a.U1Qty += qA;
        if (cross) { a.DealsX++; a.QtyX += qA; }
        if (cross && st.CurPop) { if (!st.CurDealt) { a.PopADealX++; st.CurDealt = true; } a.PopAQtyX += qA; a.PopAValX += vA; }
        if (!double.IsNaN(engQ) && engQ < m.LastProduced) { a.RetreatDeals++; a.RetreatQty += qA; }
        double u = System.Math.Min(unitA, lp);
        if (cross)
        {   // (1159) W2(a)′ の成約（行ごとに 1 回）と退きの量（越境の成約・エンジンの Q・旗 0 は NaN＝数えない）
            if (st.CurC && !st.CurDealtC) { a.PopCDealX++; if (st.CurCPart0) a.PopCPart0DealX++; st.CurDealtC = true; }
            if (!double.IsNaN(engQ))
            {
                double pm = m.LastProduced - engQ;
                if (pm > 0) a.RetreatDealsRaw++;
                if (pm > T1TolRel * System.Math.Max(m.LastProduced, m.LastDemand)) { a.RetreatDealsTol++; a.RetreatQtySum += pm; }
            }
        }
        if (!double.IsNaN(st.CurUMin) && u < st.CurUMin * (1.0 - T1TolRel)) a.UBelowMin++;   // (1159) 行の門の主張〈u_min≦どの成約の u′〉の番人
        if (!(rpT > 1e-9) || !(u > 1e-9) || double.IsInfinity(u)) { a.U1Unok++; return; }
        double root = System.Math.Pow(rpT / u, c.PriceElasticity);
        double ph = T1PHat(r, g, c, u / fx);
        double q = System.Math.Min(m.LastProduced, ph); if (!(q >= 0)) q = m.LastProduced;
        if (!double.IsNaN(engQ)) { a.SelfN++; if (q != engQ) { a.SelfDiff++; double rel = System.Math.Abs(q - engQ) / System.Math.Max(1e-300, System.Math.Abs(engQ)); if (rel > a.SelfMaxRel) a.SelfMaxRel = rel; } }
        double ub = m.TargetStock + m.LastDemand * root - q;
        double eAbs = m.Stock - ub, den = System.Math.Max(m.LastDemand, m.TargetStock);
        double eRel = den > 0 ? eAbs / den : double.NaN;
        a.U1Deals++;
        bool small = m.LastDemand < c.TargetStockFloor, viol = eRel > T1TolRel;
        if (viol) a.U1Viol++;
        if (eAbs > T1TolAbs) a.U1ViolAbs++;
        if (small) { a.U1SmallD++; if (viol) a.U1SmallDViol++; }
        if (eRel > a.U1MaxRel) a.U1MaxRel = eRel;
        if (eAbs > 0) a.U1Excess += eAbs;
    }

    static void T1Table(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("t1buy_s") || sim.Nations.Count == 0) return;
        if (!Wants("tb_s")) _tbEpoch++;   // 生産者の一覧を四半期ごとに作り直す（tb_s が回るときは TbTable が進める＝同じ刻み）
        if (!T1States.TryGetValue(sim.Market, out var st)) return;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string L(long v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (st.W is null) { st.W = Open("t1buy", seed); st.W.WriteLine(T1buyHeaderText); }
        int yearLabel = sim.Nations[0].World.Year;
        foreach (var n in sim.Nations)
        {
            var w = n.World; st.N.TryGetValue(w, out var a); a ??= new T1N();
            st.W.WriteLine(string.Join('\t', I(yearLabel), I(q), NameOf(w), L(a.Rows), L(a.OldSkip), L(a.InstSkip), L(a.Uncl), L(a.PopA), L(a.PopAPx), L(a.PopASmallD), L(a.PopADealX), FR(a.PopAQtyX), FR(a.PopAValX), L(a.SmallD), L(a.GateEval), L(a.GateMis),
                L(a.U1Deals), L(a.U1Unok), L(a.U1Viol), L(a.U1ViolAbs), a.U1Deals > 0 ? FR(a.U1MaxRel) : "nan", L(a.U1SmallD), L(a.U1SmallDViol), FR(a.U1Excess), FR(a.U1Qty),
                L(a.SelfN), L(a.SelfDiff), FR(a.SelfMaxRel), L(a.RetreatDeals), FR(a.RetreatQty), L(a.DealsX), FR(a.QtyX),
                L(a.PopC), L(a.PopCUb), L(a.PopCPart0), L(a.PopCDealX), L(a.PopCPart0DealX), FR(a.RetreatQtySum), L(a.RetreatDealsTol), L(a.RetreatDealsRaw), L(a.UBelowMin)));
        }
        st.N.Clear(); st.LastW = null; st.LastN = null;
    }

    static void T1Close(MultiNationSim sim) { if (T1States.TryGetValue(sim.Market, out var st)) { st.W?.Dispose(); st.W = null; } }

    /// <summary>(1159) W2(a)′ の母集合の述語〈着値の下界 h_min＝先頭の売り手の値段×lbMulMin が州の値段 lp を下回る〉（NaN は偽＝売り手が無い行は入らない）。</summary>
    internal static bool T1HBelow(double headPrice, double lbMulMin, double lp) => headPrice * lbMulMin < lp;
}
