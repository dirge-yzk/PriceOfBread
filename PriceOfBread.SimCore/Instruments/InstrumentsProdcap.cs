using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。〔履歴 InstrumentsProdcap.cs#1〕
public static partial class Instruments
{
    internal const string ProdcapHeaderText = "year\tq\tregion\townerNation\tgood\trecipe\tisRgo\tclass\tproductivity\tendowFromProductivity\tcapacity\toutput\tmarginalCost\tsiteRow\tsiteFound";

    sealed class PcSink { public System.IO.StreamWriter? W; public int FirstYear = int.MinValue; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PcSink> PcSinks = new();

    static void ProdcapTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("prodcap_s")) return;   // prodcap_s〔履歴 InstrumentsProdcap.cs#2〕
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0) return;
        int yearLabel = sim.Nations[0].World.Year;
        var sk = PcSinks.GetValue(sim, _ => new PcSink());
        if (sk.FirstYear == int.MinValue) sk.FirstYear = yearLabel;
        if (!NgYear(yearLabel) && yearLabel != sk.FirstYear) return;
        if (sk.W is null) { sk.W = Open("prodcap", seed); sk.W.WriteLine(ProdcapHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // 国の行は recipeout_s と同じ：清算に居た国→清算の後に増えた国
        var worlds = new List<World>(); var seen = new HashSet<World>(); int goodLimit = Catalog.Goods.Length;
        if (NgAccs.TryGetValue(sim.Market, out var a0) && a0.Any) { foreach (var w0 in a0.Worlds) if (seen.Add(w0)) worlds.Add(w0); goodLimit = System.Math.Min(goodLimit, a0.Gc); }
        foreach (var n in sim.Nations) if (seen.Add(n.World)) worlds.Add(n.World);
        foreach (var w in worlds)
        {
            string nm = NameOf(w);
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int g = p.GoodId;
                    if (g < 0 || g >= r.Markets.Length || g >= goodLimit) continue;
                    var x = RoRead(w, r, p);
                    sk.W.WriteLine(string.Join('\t', I(yearLabel), I(q), r.Name, nm, Catalog.Goods[g].Name, p.RecipeName.Length == 0 ? "(既定)" : p.RecipeName,
                        p.IsRgo ? "1" : "0", x.Class, FR(p.Productivity), FR(x.Endow), FR(p.Capacity), FR(p.Output), FR(p.MarginalCost), I(p.SiteRow), p.SiteFound ? "1" : "0"));
                }
        }
    }

    static void ProdcapClose(MultiNationSim sim) { if (PcSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
