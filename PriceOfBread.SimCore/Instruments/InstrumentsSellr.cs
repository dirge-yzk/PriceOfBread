using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。〔履歴 InstrumentsSellr.cs#1〕
public static partial class Instruments
{
    internal const string SellrHeaderText = "year\tq\tregion\townerNation\tgood\tlastDemand\tprevExportAll\tprevImportAll\tlastProduced\tstock\ttargetStock\tlaborWeight\trentR\tsellROld\tsellRBoth\trentRMul";   // SellrHeaderText〔履歴 InstrumentsSellr.cs#2〕

    /// <summary>
    /// 計器自身の写し。〔履歴 InstrumentsSellr.cs#3〕
    /// </summary>
    internal static double RentRMulOf(double rentR) => rentR > 0 ? rentR : 0.0;

    sealed class SrSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, SrSink> SrSinks = new();

    /// <summary>1 行の値（試験が合成の盤面で突き合わせる）。</summary>
    internal static (double SellROld, double SellRBoth) SellrOf(Market m, double floor)
    {
        double old = m.LastDemand > floor ? m.LastDemand : floor;
        double both = m.LastDemand + m.PrevExport - m.PrevImport;
        return (old, both > floor ? both : floor);
    }

    static void SellrTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.BuyShadow) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("sellr_s")) return;   // sellr_s〔履歴 InstrumentsSellr.cs#4〕
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0) return;
        int yearLabel = sim.Nations[0].World.Year;
        if (!NgYear(yearLabel)) return;
        var sk = SrSinks.GetValue(sim, _ => new SrSink());
        if (sk.W is null) { sk.W = Open("sellr", seed); sk.W.WriteLine(SellrHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var worlds = new List<World>(); var seen = new HashSet<World>(); int goodLimit = Catalog.Goods.Length;
        if (NgAccs.TryGetValue(sim.Market, out var a0) && a0.Any) { foreach (var w0 in a0.Worlds) if (seen.Add(w0)) worlds.Add(w0); goodLimit = System.Math.Min(goodLimit, a0.Gc); }
        foreach (var n in sim.Nations) if (seen.Add(n.World)) worlds.Add(n.World);
        foreach (var w in worlds)
        {
            string nm = NameOf(w); double floor = w.Coef.TargetStockFloor;
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int g = p.GoodId;
                    if (g < 0 || g >= r.Markets.Length || g >= goodLimit) continue;
                    var m = r.Markets[g];
                    var (sOld, sBoth) = SellrOf(m, floor);
                    double rentR = m.Price - EconomyStep.InputCostPerUnit(p, r, r, w.Coef);
                    sk.W.WriteLine(string.Join('\t', I(yearLabel), I(q), r.Name, nm, Catalog.Goods[g].Name,
                        FR(m.LastDemand), FR(m.PrevExport), FR(m.PrevImport), FR(m.LastProduced), FR(m.Stock), FR(m.TargetStock), FR(p.LaborWeight), FR(rentR), FR(sOld), FR(sBoth), FR(RentRMulOf(rentR))));
                }
        }
    }

    static void SellrClose(MultiNationSim sim) { if (SrSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
