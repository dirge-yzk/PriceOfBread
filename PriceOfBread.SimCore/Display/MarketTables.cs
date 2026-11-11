namespace PriceOfBread.SimCore.Display;

/// <summary>財の値段の表の 1 行（自国の市場での財ごとの値段と需給）。値は盤面を読むだけ（書かない・貯めない）。
/// <paramref name="Domestic"/>＝首都州の値段を為替で世界価格と同じ建て値にした額／<paramref name="World"/>＝世界価格／<paramref name="Gap"/>＝国内価格 ÷ 世界価格 − 1（世界価格が立たない財は NaN）／
/// <paramref name="Demand"/>・<paramref name="Supply"/>・<paramref name="Shortage"/>＝自国の活きている州の前期の総需要・産出・満たせなかった需要の和。</summary>
public sealed record MarketLine(int Good, string Name, string Tier, double Domestic, double World, double Gap, double Demand, double Supply, double Shortage);

/// <summary>市場・清算・貿易ページの表のうち、輸入と輸出（<see cref="TradeTables"/>）以外。旧 App には無かった表（設計 §3-3「財の値段の表」）。</summary>
public static class MarketTables
{
    /// <summary>財の層の並び（凡例と同じ順＝素材→主食→日用品→奢侈品→生産財）。</summary>
    internal static int TierOrder(GoodTier t) => t switch
    {
        GoodTier.Intermediate => 0,
        GoodTier.Staple => 1,
        GoodTier.Daily => 2,
        GoodTier.Luxury => 3,
        GoodTier.Producer => 4,
        _ => 5,
    };

    /// <summary>行＝役務でない財のうち、自国で需要か産出が立っている財（実用ゼロの足切りは係数 <see cref="Coefficients.TargetStockFloor"/> の 1 本）。層の順・同じ層の中は財の番号順
    /// （目録は文化に依らない＝50 音順の比較子は使わない。名前の列の見出しを押すと文字の順に並べ替わる）。</summary>
    internal static IReadOnlyList<MarketLine> Lines(MultiNationSim sim, World w)
    {
        int gc = Catalog.Goods.Length;
        var demand = new double[gc]; var supply = new double[gc]; var shortage = new double[gc];
        foreach (var r in w.Regions)
        {
            int n = Math.Min(r.Markets.Length, gc);
            for (int g = 0; g < n; g++)
            {
                var m = r.Markets[g];
                demand[g] += m.LastDemand; supply[g] += m.LastProduced; shortage[g] += m.LastShortage;
            }
        }
        double eps = w.Coef.TargetStockFloor;
        var wp = sim.Market.Price;
        var cap = w.Region;
        double ex = w.ExchangeRate;
        var result = new List<MarketLine>();
        foreach (var d in Catalog.Goods)
        {
            if (d.Tier == GoodTier.Service) continue;
            int g = d.Id;
            if (!(demand[g] > eps || supply[g] > eps)) continue;   // 否定形＝NaN も出さない側へ
            double world = g < wp.Count ? wp[g] : double.NaN;
            double dom = g < cap.Markets.Length ? cap.Markets[g].Price * ex : double.NaN;
            double gap = world > 1e-6 && dom > 1e-9 ? dom / world - 1.0 : double.NaN;
            result.Add(new MarketLine(g, d.Name, ProductionTables.TierName(d.Tier), dom, world, gap, demand[g], supply[g], shortage[g]));
        }
        return result.OrderBy(l => TierOrder(Catalog.Goods[l.Good].Tier)).ThenBy(l => l.Good).ToList();
    }

    /// <summary>世界価格比の文字の色（高騰＝赤・下落＝青）。濃さは 100% からの離れに比例する（離れが 100% 以上で最も濃い）。世界価格と同じ（離れが 0.5% 以内）は既定の色（null）。
    /// 財の値段・財の表・貿易の表が同じ関数を通る。<paramref name="gap"/>＝国内価格 ÷ 世界価格 − 1。</summary>
    public static string? RatioInk(double gap)
    {
        if (!(Math.Abs(gap) > 0.005)) return null;   // 否定形＝NaN も既定の色
        double t = Math.Min(1.0, Math.Abs(gap));
        var (r, g, b) = gap > 0 ? (0xB0, 0x24, 0x18) : (0x2F, 0x6E, 0xA5);
        const int br = 0x6A, bg = 0x62, bb = 0x58;   // 離れが小さい時は本文に近い灰
        int Mix(int a, int c) => (int)Math.Round(a + (c - a) * t);
        return $"#{Mix(br, r):X2}{Mix(bg, g):X2}{Mix(bb, b):X2}";
    }

    /// <summary>財の値段の表（国内価格・世界価格比・需要・供給・不足）。</summary>
    public static Table<MarketLine> Prices { get; } = new("market.prices", "財の値段", Page.Market, Lines, l => l.Good.ToString(), l => l.Name,
    [
        Col.Text<MarketLine>("name", "財名", l => l.Name),
        Col.Number<MarketLine>("domestic", "国内価格", Unit.PricePerUnit, l => l.Domestic),
        // 世界価格を 100% とした国内価格（2026-11-11 作者指示）。高騰＝赤・下落＝青（世界価格そのものは出さない）
        Col.Number<MarketLine>("gap", "世界価格比", Unit.Share, l => l.Gap + 1.0, ink: l => RatioInk(l.Gap)),
        Col.Number<MarketLine>("demand", "需要", Unit.Quantity, l => l.Demand),
        Col.Number<MarketLine>("supply", "供給", Unit.Quantity, l => l.Supply),
        Col.Number<MarketLine>("shortage", "不足", Unit.Quantity, l => l.Shortage),
    ], l => l.Tier);
}
