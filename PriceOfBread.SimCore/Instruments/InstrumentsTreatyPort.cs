using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// (1138) c111tp 条約港の読み手の恒等の計器 tport_s（国×四半期・全ての国）。力学の値を 1 つも書かない・乱数を引かない・世界を書かない。
// 同じ盤面で〈全ての港〉と〈本国格の港だけ〉の 2 つの規則を、州の ForeignShares・IsPort・Tier から計器が自分で組む（Politics の関数を呼ばない）。
// エンジンの列（treatyEngineFlag・tariffScaleEngineShare）だけは本物の経路（UnequalTreatyInForce・TreatyPortTariffScale）から読む。
public static partial class Instruments
{
    internal const string TportHeaderText = "quarter\tyear\tnation\tportForeignAllShare\tportForeignHomeShare\ttreatyAllFlag\ttreatyHomeFlag\ttreatyEngineFlag\ttreatyTriggerRegion\tcolonialOnlyFlag\t"
        + "tariffScaleEngineShare\thomePortsAboveLineCount\tcolonialPortsAboveLineCount";

    sealed class TportSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, TportSink> TportSinks = new();

    /// <summary>港の外国の取り分の和（計器の写し）：正の取り分の和を [0,1] へ。非有限・負は 0。</summary>
    static double TportForeignSum(Region r)
    {
        double v = 0; foreach (var x in r.ForeignShares) if (x > 0) v += x;
        return !(v >= 0) ? 0 : System.Math.Min(1.0, v);
    }

    /// <summary>1 国の行の値（純関数）：港の人口加重の外国の取り分（全て・本国格だけ）と、線を越えた港の数と最初の港。</summary>
    internal static (double AllShare, double HomeShare, int HomeAbove, int ColAbove, string Trigger) TportRow(World w, double line)
    {
        double numA = 0, denA = 0, numH = 0, denH = 0;
        int homeAbove = 0, colAbove = 0; string? homeFirst = null, colFirst = null;
        foreach (var r in w.Regions)
        {
            if (!r.IsPort) continue;
            bool colonial = r.Tier == RegionTier.Colonial;
            double f = TportForeignSum(r);
            if (line > 0 && f >= line)
            {
                if (colonial) { colAbove++; colFirst ??= r.Name; }
                else { homeAbove++; homeFirst ??= r.Name; }
            }
            double pop = r.SumPopulation();
            if (!(pop > 0)) continue;
            numA += pop * f; denA += pop;
            if (!colonial) { numH += pop * f; denH += pop; }
        }
        return (denA > 0 ? numA / denA : 0.0, denH > 0 ? numH / denH : 0.0, homeAbove, colAbove, homeFirst ?? colFirst ?? "-");
    }

    static void TportTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Tg.TreatyPort) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!Wants("tport_s") || sim.Nations.Count == 0 || sim.Nations[0].World.WarmupFrozen) return;
        var sk = TportSinks.GetValue(sim, _ => new TportSink());
        if (sk.W is null) { sk.W = Open("tport", seed); sk.W.WriteLine(TportHeaderText); }
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (w.Regions.Count == 0) continue;
            double line = 0.5 * w.Coef.ForeignRuleFromTreaty;
            var t = TportRow(w, line);
            bool all = t.HomeAbove + t.ColAbove > 0, home = t.HomeAbove > 0;
            sk.W.WriteLine(string.Join('\t', q.ToString(inv), w.Year.ToString(inv), NameOf(w), RpR(t.AllShare), RpR(t.HomeShare),
                all ? "1" : "0", home ? "1" : "0", w.UnequalTreatyInForce ? "1" : "0", t.Trigger, all && !home ? "1" : "0",
                RpR(InternationalMarket.TreatyPortTariffScale(w)), t.HomeAbove.ToString(inv), t.ColAbove.ToString(inv)));
        }
    }

    static void TportClose(MultiNationSim sim)
    {
        if (!Tg.TreatyPort) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (TportSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; }
    }
}
