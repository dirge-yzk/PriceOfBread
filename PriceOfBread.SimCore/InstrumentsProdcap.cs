using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1019) 貿易の段0″ の計器 prodcap_s（年×州×生産者の播種の能力と賦存の級・読むだけ・恒等・事前登録 measurements/m1019_20261110_prereg_pc.txt）
// 部分クラス（InstrumentsRecipeout.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。
// 級と賦存の倍率は recipeout_s と同じ RoRead を呼ぶ（規則の写しを作らない）。productivity・capacity・output・marginalCost・siteRow は Producer の欄の素の値。
// 年：recipeout_s と同じ呼び口（q%4==3）の NgYear の年に加え、開幕の最初の q%4==3 の年（nationgood_s には無い＝突き合わせは両方にある年だけ）。
// ⚠限界（計器が自己申告する）：marginalCost は参照価格の単位の素の値（州の物価水準を掛けない）。1 種で約 100 万行。
public static partial class Instruments
{
    internal const string ProdcapHeaderText = "year\tq\tregion\townerNation\tgood\trecipe\tisRgo\tclass\tproductivity\tendowFromProductivity\tcapacity\toutput\tmarginalCost\tsiteRow\tsiteFound";

    sealed class PcSink { public System.IO.StreamWriter? W; public int FirstYear = int.MinValue; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, PcSink> PcSinks = new();

    static void ProdcapTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("prodcap_s")) return;   // (1061) 選ばれない表は組み立ても飛ばす（読むだけ・FirstYear も PcSink の中だけ）
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
