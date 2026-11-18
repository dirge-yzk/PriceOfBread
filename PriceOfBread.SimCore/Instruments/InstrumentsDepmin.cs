using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// (1202) c120t2 計器 depmin_s（T2-0・T2-1 の旗 DepositSitesOnly の器）：鉱物の RGO の生産者の能力と頭数と在庫の欄を写すだけ。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンの関数を呼ばない（品位は計器の中の式で欄から出す）。
// 行：年末の四半期。鉱物の RGO の生産者は NgYear と最初の年・産地の生産者（siteRow≧0）は毎年。
public static partial class Instruments
{
    internal const string DepminHeaderText = "year\tq\tregion\townerNation\tgood\trecipe\tsiteRow\tsiteFound\toutput\tcapacity\tproductivity\theads\tscale\tregionGrade\tdepInit\tdepLeft\tgrade";

    sealed class DmSink { public System.IO.StreamWriter? W; public int FirstYear = int.MinValue; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DmSink> DmSinks = new();

    /// <summary>計器の品位の読み（エンジンの関数を呼ばない）：旗 0＝州の品位・旗 1＝産地の生産者は床＋(1−床)×残り÷初期・それ以外は 1。</summary>
    internal static double InsMineralGrade(Producer p, Region r, Coefficients c)
    {
        if (!(c.DepositSitesOnly > 0)) return r.MineralGradeFactor;
        if (!(p.SiteDepositInitial > 1e-9)) return 1.0;
        double g = c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * (p.SiteDeposit / p.SiteDepositInitial);
        return g >= c.DepositGradeFloor && g <= 1.0 ? g : 1.0;
    }

    /// <summary>計器の残り÷初期：旗 0＝州の鉱脈・旗 1＝生産者の在庫（未捕捉は NaN）。</summary>
    internal static double InsDepositFrac(Producer p, Region r, Coefficients c)
        => !(c.DepositSitesOnly > 0)
            ? (r.MineralDepositInitial > 0 ? r.MineralDeposit / r.MineralDepositInitial : double.NaN)
            : (p.SiteDepositInitial > 0 ? p.SiteDeposit / p.SiteDepositInitial : double.NaN);

    static void DepminTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("depmin_s")) return;
        bool yearEnd = q % Calendar.QuartersPerYear == Calendar.QuartersPerYear - 1;
        if (!yearEnd || sim.Nations.Count == 0) return;
        int yearLabel = sim.Nations[0].World.Year;
        var sk = DmSinks.GetValue(sim, _ => new DmSink());
        if (sk.FirstYear == int.MinValue) sk.FirstYear = yearLabel;
        bool all = NgYear(yearLabel) || yearLabel == sk.FirstYear;
        if (sk.W is null) { sk.W = Open("depmin", seed); sk.W.WriteLine(DepminHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // 国の行は prodcap_s と同じ：清算に居た国→清算の後に増えた国
        var worlds = new List<World>(); var seen = new HashSet<World>(); int goodLimit = Catalog.Goods.Length;
        if (NgAccs.TryGetValue(sim.Market, out var a0) && a0.Any) { foreach (var w0 in a0.Worlds) if (seen.Add(w0)) worlds.Add(w0); goodLimit = System.Math.Min(goodLimit, a0.Gc); }
        foreach (var n in sim.Nations) if (seen.Add(n.World)) worlds.Add(n.World);
        foreach (var w in worlds)
        {
            string nm = NameOf(w); var c = w.Coef;
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                {
                    int g = p.GoodId;
                    if (!p.IsRgo || g < 0 || g >= r.Markets.Length || g >= goodLimit || !Catalog.IsMineral(g)) continue;
                    if (!all && p.SiteRow < 0) continue;
                    sk.W.WriteLine(string.Join('\t', I(yearLabel), I(q), r.Name, nm, Catalog.Goods[g].Name, p.RecipeName.Length == 0 ? "(既定)" : p.RecipeName,
                        I(p.SiteRow), p.SiteFound ? "1" : "0", FR(p.Output), FR(p.Capacity), FR(p.Productivity), FR(p.HeadsLastMonth), FR(p.Scale),
                        FR(r.MineralGradeFactor), FR(p.SiteDepositInitial), FR(p.SiteDeposit), FR(InsMineralGrade(p, r, c))));
                }
        }
    }

    static void DepminClose(MultiNationSim sim) { if (DmSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
