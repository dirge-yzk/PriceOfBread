namespace PriceOfBread.SimCore;

// (1255) 資本と投資の作り直し 段 0：送電網と工場の利潤の計器 capinv_s。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 生産者と州は計器が自分で選ぶ（エンジンの局所変数は受け取らない）。四半期の末に Region／Producer／Pop に既に残っている値だけを読む。
public static partial class Instruments
{
    internal static readonly string[] CapinvColNames =
    [
        "homeRegionCount", "gridCapacitySum", "elecWantSum", "gridOverWantRegionShare",
        "gridReplaceDemandPerMonth", "gridKeepMinusRetiredLiteral",
        "ownFactoryProfitPerMonth", "elecFactoryProfitPerMonth",
        "capitalistSavingsStock", "capitalistIncomePerYear",
    ];
    internal static readonly string CapinvHeaderText = "quarter\tyear\tnationUid\tnation\t" + string.Join('\t', CapinvColNames);

    sealed class CapinvSink { public System.IO.StreamWriter? W; public bool Header; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CapinvSink> CapinvSinks = new();

    /// <summary>本国格（植民地格でない）の州の送電網と工場の利潤の行（列の順＝<see cref="CapinvColNames"/>）。</summary>
    internal static double[] CapinvRow(World w)
    {
        int n = 0, over = 0;
        double grid = 0, want = 0, replace = 0, profit = 0, elecProfit = 0, sav = 0, inc = 0;
        foreach (var r in w.Regions)
        {
            if (r.Tier == RegionTier.Colonial) continue;
            n++; grid += r.GridCapacity; want += r.LastElecWant;
            if (r.GridCapacity >= r.LastElecWant) over++;
            replace += w.Coef.GridReplaceRate * r.GridCapacity;
            foreach (var s in r.Producers)
            {
                if (s.Bankrupt || !s.BuysLabor) continue;
                double pr = s.LastRevenue - s.LastCost;
                if (!(pr > 0)) continue;
                profit += pr;
                if (s.GoodId == Catalog.ElectricityId) elecProfit += pr;
            }
            int ip = r.InvestorPopIndex;
            if (ip >= 0 && ip < r.Pops.Count) { sav += r.Pops[ip].Savings.Value; inc += r.Pops[ip].EarnedPerMonth.Value * 12.0; }
        }
        // 退役した係数 GridKeepPerMonth の値 0.998 をリテラルで持ち、いまの劣化 1−GridReplaceRate との差を出す（既定で 0）。
        double keepMinusRetired = (1.0 - w.Coef.GridReplaceRate) - 0.998;
        return [n, grid, want, n > 0 ? (double)over / n : double.NaN, replace, keepMinusRetired, profit, elecProfit, sav, inc];
    }

    static void CapinvTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("capinv_s")) return;
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        var sk = CapinvSinks.GetValue(sim, _ => new CapinvSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var nat in sim.Nations)
        {
            var w = nat.World; var st = S(w);
            if (!(yearEnd || IsPicked(w, st)) || !(w.TotalPopulation > 0) || w.Regions.Count == 0) continue;
            sk.W ??= Open("capinv", seed);
            var row = CapinvRow(w);
            if (!sk.Header) { sk.W.WriteLine(CapinvHeaderText); sk.Header = true; }
            sk.W.WriteLine(I(q) + "\t" + I(w.Year) + "\t" + I(st.Uid) + "\t" + NameOf(w) + "\t" + string.Join('\t', row.Select(F)));
        }
    }

    static void CapinvClose(MultiNationSim sim) { if (CapinvSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
