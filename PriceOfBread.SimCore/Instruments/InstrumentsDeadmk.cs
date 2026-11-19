namespace PriceOfBread.SimCore;

// (1229) c124pf 値段の形成の線の計器 deadmk_s（国×財×四半期・四半期の終わりの 1 時点）。事前登録 measurements/m1229_20261119_prereg_pf.txt。
//   計器が自分で国の全ての州と、その州に作り手のある財の市場を走査する（エンジンの関数を呼ばない・旗 SellOfferExcessStock を見ない）。
//   床に張り付いた市場＝値段 ＜ 2×PriceFloorFactor×RefPrice×PriceLevel（(1224) のプローブの 0.02 と同じ物差し）。死んだ市場＝そのうち LastDemand と LastProduced が 1e-9 未満。
//   同じ盤面で旧と新の売りの申し出の影を同じ式で数える（着値＝その州の参照の値段 rpS とした時の値・成約の着値は数えない＝近似）：
//     旧＝max(0, (LastProduced＋LastImport)−LastDemand−LastExport)・新＝max(0, 旧の中身＋max(0, Stock−TargetStock))。
//   力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない。行は床に張り付いた市場が 1 つ以上ある国×財だけ。
public static partial class Instruments
{
    internal const string DeadmkHeaderText = "year\tq\tnation\tgood\tnWithProducer\tnFloor\tnFloorDead\tnFloorDeadAbove\tnFloorDeadExported"
        + "\tstockFloorDead\ttargetFloorDead\texportLastMonthFloorDead\tprevExportFloorDead\tofferOldAtRpFloorDead\tofferNewAtRpFloorDead\tnationTopExportPartner";

    sealed class DeadmkSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, DeadmkSink> DeadmkSinks = new();

    /// <summary>1 市場の旧と新の申し出の影（着値＝rpS）。試験が合成の盤面で突き合わせる。</summary>
    internal static (double Old, double New) DeadmkOfferOf(Market m)
    {
        double flow = m.LastProduced + m.LastImport - m.LastDemand - m.LastExport;
        double ex = m.Stock - m.TargetStock;
        double nw = ex > 0 ? flow + ex : flow;
        return (flow > 0 ? flow : 0.0, nw > 0 ? nw : 0.0);
    }

    /// <summary>床に張り付いているか（値段 ＜ 2×PriceFloorFactor×RefPrice×PriceLevel）。</summary>
    internal static bool DeadmkFloor(Market m, int g, Region r, Coefficients c) => m.Price < 2.0 * c.PriceFloorFactor * Catalog.RefPrices[g] * r.PriceLevel;

    static void DeadmkTable(MultiNationSim sim, ulong seed, int q)
    {
        if (!Wants("deadmk_s") || sim.Nations.Count == 0) return;
        int yearLabel = sim.Nations[0].World.Year;
        var sk = DeadmkSinks.GetValue(sim, _ => new DeadmkSink());
        if (sk.W is null) { sk.W = Open("deadmk", seed); sk.W.WriteLine(DeadmkHeaderText); }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int ng = Catalog.Goods.Length;
        var nWith = new int[ng]; var nFloor = new int[ng]; var nDead = new int[ng]; var nAbove = new int[ng]; var nExp = new int[ng];
        var stock = new double[ng]; var target = new double[ng]; var exp = new double[ng]; var pexp = new double[ng]; var oOld = new double[ng]; var oNew = new double[ng];
        var has = new bool[ng];
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            System.Array.Clear(nWith); System.Array.Clear(nFloor); System.Array.Clear(nDead); System.Array.Clear(nAbove); System.Array.Clear(nExp);
            System.Array.Clear(stock); System.Array.Clear(target); System.Array.Clear(exp); System.Array.Clear(pexp); System.Array.Clear(oOld); System.Array.Clear(oNew);
            foreach (var r in w.Regions)
            {
                System.Array.Clear(has);
                foreach (var p in r.Producers) if (p.GoodId >= 0 && p.GoodId < ng && p.GoodId < r.Markets.Length) has[p.GoodId] = true;
                for (int g = 0; g < ng; g++)
                {
                    if (!has[g] || Catalog.Goods[g].Tier == GoodTier.Service) continue;
                    var m = r.Markets[g];
                    nWith[g]++;
                    if (!DeadmkFloor(m, g, r, w.Coef)) continue;
                    nFloor[g]++;
                    if (!(m.LastDemand < 1e-9 && m.LastProduced < 1e-9)) continue;
                    nDead[g]++;
                    if (m.Stock - m.TargetStock > 0) nAbove[g]++;
                    if (m.LastExport > 1e-12) nExp[g]++;
                    stock[g] += m.Stock; target[g] += m.TargetStock; exp[g] += m.LastExport; pexp[g] += m.PrevExport;
                    var (o0, o1) = DeadmkOfferOf(m); oOld[g] += o0; oNew[g] += o1;
                }
            }
            string nm = NameOf(w);
            for (int g = 0; g < ng; g++)
            {
                if (nFloor[g] == 0) continue;
                string top = w.ExportToByGood is { } ex && g < ex.Length && ex[g] is { Length: > 0 } s ? s : "-";
                sk.W.WriteLine(string.Join('\t', I(yearLabel), I(q), nm, Catalog.Goods[g].Name, I(nWith[g]), I(nFloor[g]), I(nDead[g]), I(nAbove[g]), I(nExp[g]),
                    FR(stock[g]), FR(target[g]), FR(exp[g]), FR(pexp[g]), FR(oOld[g]), FR(oNew[g]), top));
            }
        }
    }

    static void DeadmkClose(MultiNationSim sim) { if (DeadmkSinks.TryGetValue(sim, out var sk)) { sk.W?.Dispose(); sk.W = null; } }
}
