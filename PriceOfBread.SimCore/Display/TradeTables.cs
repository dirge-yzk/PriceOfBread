namespace PriceOfBread.SimCore.Display;

/// <summary>貿易の表の 1 行（財ごとの輸出か輸入）。値は清算 1 回分を月へ割った額・割合は 0〜1。</summary>
/// <param name="Raw">清算 1 回分の額（世界価格建て・並べ替えの元）</param>
/// <param name="MonthlyValue">月割の額（<see cref="Raw"/> ÷ 清算の月数）</param>
/// <param name="PriceRatio">国内価格 ÷ 世界価格（世界価格が 0 に近い財は NaN＝添えない）</param>
/// <param name="Fraction">輸入＝国内の消費に占める輸入量（依存度）／輸出＝世界の輸出に占める自国の額（世界シェア）</param>
/// <param name="Partner">実際に最も多く取引した相手（無ければ空）</param>
public sealed record TradeLine(int Good, string Name, double Raw, double MonthlyValue, double PriceRatio, double Fraction, string Partner);

/// <summary>貿易の表（輸入・輸出）。旧 App の貿易タブが手で組んでいた行を本体の目録へ移した（設計 §3-3・§7-1 段1）。
/// 盤面は読むだけ・書かない。額の降順（同額は財の番号順）。額が 0 の財の行は出さない。</summary>
public static class TradeTables
{
    private static IReadOnlyList<TradeLine> Lines(MultiNationSim sim, World w, bool exports)
    {
        var result = new List<TradeLine>();
        if (w.ExportsByGood is not { } eg || w.ImportsByGood is not { } ig) return result;   // 決済前／単一国は空
        var wp = sim.Market.Price;
        int gc = Catalog.Goods.Length;
        double mpc = w.Coef.MonthsPerClearing;   // 清算単位→月割（四半期清算=3・月次清算=1）

        // 世界シェアの分母＝全モデル国の同財輸出額の合計
        var worldExp = new double[gc];
        if (exports)
            foreach (var n in sim.Nations)
                if (n.World.ExportsByGood is { } neg)
                    for (int g = 0; g < gc && g < neg.Length; g++) worldExp[g] += neg[g];
        // 輸入依存度の分母（財ごとの前期総需要）を先に 1 パスで積む
        var demandByGood = new double[gc];
        if (!exports)
            foreach (var r in w.Regions)
                if (r.State == RegionState.Active)
                    for (int g = 0; g < gc && g < r.Markets.Length; g++) demandByGood[g] += r.Markets[g].LastDemand;

        // 相手＝実際に取引した相手の州（二国間交易）。エンジンが財ごとに最大額の相手を記録する。台帳が無い（置換前の経路）時だけ、その財を最も多く出している国で近似する。
        string TopPartner(int g, bool wantExporter)
        {
            var mine = wantExporter ? w.ImportFromByGood : w.ExportToByGood;   // 輸入の相手＝売り手／輸出の相手＝買い手
            if (mine is not null && g < mine.Length && !string.IsNullOrEmpty(mine[g])) return mine[g];
            string best = ""; double bv = 0;
            foreach (var n in sim.Nations)
            {
                if (ReferenceEquals(n.World, w)) continue;
                var arr = wantExporter ? n.World.ExportsByGood : n.World.ImportsByGood;
                if (arr is null || g >= arr.Length || !(arr[g] > bv)) continue;
                bv = arr[g]; best = n.World.SelfPower?.Name ?? n.World.Region.Name;
            }
            return best;
        }

        var src = exports ? eg : ig;
        for (int g = 0; g < gc; g++)
        {
            if (!(g < src.Length && src[g] > 1e-9)) continue;
            double p = g < wp.Count ? Math.Max(wp[g], 1e-9) : 1.0;
            double dom = g < w.Region.Markets.Length ? w.Region.Markets[g].Price : p;   // 自国（首都州）の相場＝世界価格比の分子
            double ratio = p > 1e-9 ? dom / p : double.NaN;
            double fraction;
            if (exports)
                fraction = worldExp[g] > 1e-12 ? eg[g] / worldExp[g] : 0;
            else
            {
                double qtyMonthly = ig[g] / p / mpc;   // 消費（前期総需要・月次）に占める輸入量（月割）
                double demand = demandByGood[g];
                fraction = demand > 1e-9 ? Math.Min(1.0, qtyMonthly / demand) : 0;
            }
            result.Add(new TradeLine(g, Catalog.Goods[g].Name, src[g], src[g] / mpc, ratio, fraction, TopPartner(g, !exports)));
        }
        return result.OrderByDescending(l => l.Raw).ToList();   // 額の降順（安定＝同額は財の番号順）
    }

    private static Table<TradeLine> Make(string key, string name, bool exports, string fractionName, string partnerName)
        => new(key, name, Page.Market, (sim, w) => Lines(sim, w, exports), l => l.Good.ToString(), l => l.Name,
        [
            Col.Text<TradeLine>("name", "財名", l => l.Name),
            Col.NumberNote<TradeLine>("value", "金額/月", Unit.MoneyPerMonth, l => l.MonthlyValue, l => double.IsNaN(l.PriceRatio) ? "" : $"(×{l.PriceRatio:F2})"),
            Col.Number<TradeLine>("fraction", fractionName, Unit.Share, l => l.Fraction),
            Col.Text<TradeLine>("partner", partnerName, l => l.Partner),
        ]);

    /// <summary>輸入の表（額の降順・依存度・輸入元）。</summary>
    public static Table<TradeLine> Imports { get; } = Make("trade.imports", "輸入", false, "依存度", "輸入元");

    /// <summary>輸出の表（額の降順・世界シェア・輸出先）。</summary>
    public static Table<TradeLine> Exports { get; } = Make("trade.exports", "輸出", true, "世界シェア", "輸出先");

    /// <summary>清算 1 回分の輸出と輸入の総額（額が 0 の財は足さない＝行に出す財と同じ母集団）。月割は呼び手が <see cref="Coefficients.MonthsPerClearing"/> で割る。</summary>
    public static (double Export, double Import) Totals(World w)
    {
        double e = 0, i = 0;
        if (w.ExportsByGood is { } eg && w.ImportsByGood is { } ig)
        {
            int gc = Catalog.Goods.Length;
            for (int g = 0; g < gc; g++)
            {
                if (g < eg.Length && eg[g] > 1e-9) e += eg[g];
                if (g < ig.Length && ig[g] > 1e-9) i += ig[g];
            }
        }
        return (e, i);
    }
}
