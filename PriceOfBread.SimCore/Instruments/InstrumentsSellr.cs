using System.Collections.Generic;
namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (1036) 貿易の計器 C4c sellr_s（労働の重みの売れる量 sellR の旧と両側・読むだけ・恒等・事前登録 measurements/m1036_20261111_prereg_lc.txt）
// 部分クラス（InstrumentsProdcap.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。EconomyStep.cs:962 の sellR は変えない。
// 行＝調べる年（NgEveryYears の倍数と 1913）の第 4 四半期の末 × 州 × 生産者（労働の重み LaborWeight が生産者の欄＝州×財に畳むと 1 財に複数の生産者が居る州で重みが混ざる）。
// 列：lastDemand・prevExportAll・prevImport・lastProduced・stock・targetStock・laborWeight・rentR・sellROld・sellRBoth。
//   sellROld＝max(LastDemand, TargetStockFloor)（EconomyStep.cs:962 の式の計器自身の写し・エンジンの関数は呼ばない）。
//   sellRBoth＝max(LastDemand＋PrevExport−PrevImport, TargetStockFloor)（案 A の両側）。TargetStockFloor は州の World の係数から読む。
// ⚠限界（計器が自己申告する）：
//   ・PrevExport・PrevImport は国内の州どうしの売り買いも含む（Model.cs:835〜836）＝列の名は prevExportAll・prevImportAll。越境だけには分けられない（分けるには清算の中の台帳が要る）。
//   ・rentR は EconomyStep.InputCostPerUnit を呼ぶ（InstrumentsFb.cs の先例・式の写しを作らない）＝眠りの月の最後の値ではなく四半期の末の値段と投入費。
//   ・laborWeight は平滑 Toward の途中の値（目覚めた月を除き sellROld×rentR とは一致しない）＝一致の割合は記録で、門は合成の盤面。
public static partial class Instruments
{
    internal const string SellrHeaderText = "year\tq\tregion\townerNation\tgood\tlastDemand\tprevExportAll\tprevImportAll\tlastProduced\tstock\ttargetStock\tlaborWeight\trentR\tsellROld\tsellRBoth\trentRMul";   // (1045) 末尾：エンジン（EconomyStep.cs:963 の周り）が重みに掛ける形＝rentR が正でなければ 0（NaN も 0）

    /// <summary>(1045) エンジンが sellR に掛ける rentR の形（否定形で丸める＝NaN・負は 0）。計器自身の写し。</summary>
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
        if (!Wants("sellr_s")) return;   // (1061) 選ばれない表は組み立ても飛ばす（読むだけ）
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
