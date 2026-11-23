namespace PriceOfBread.SimCore;

// (1277) c131pf 計器 capskip_s：清算（四半期の最初の回）で容量が尽きて飛ばした売り手（BilateralMarket.cs の :963 の分岐）の観測。(1244) c126pf の計器の写しに 2 列を足した。
// 力学の値を 1 つも書かない・乱数を引かない・エンジンに欄を足さない（置き場は計器の ConditionalWeakTable）。
// 同じ盤面で旧の規則（足さない）と新の規則（足す）を両方数える＝影の量は計器が売り手と買い手の市場から自分で組む（エンジンの式を呼ばない・行の未充足は引かない＝上界）。
// 列：skips＝飛ばした回数・skipsPricedOut＝そのうちエンジンの値段の門で止まった回数・offerWeight＝飛ばした申し出（在庫−目標）×重さ・
//     shadowAllWeight＝値段の門を問わない影 min(申し出, X_lp)×重さ・shadowWeight＝値段の門を通った物だけの影・addedWeight＝エンジンが実際に足した重さ（旗 0 は 0）・
//     dedupWeight＝同じ行の次の売り手の側の未充足から引いた重さ（旗 0 は 0・エンジンの実際の側）・dedupBaseWeight＝その引く前の未充足の重さ（(1322)・重なりの割合＝dedup÷dedupBase）。国＝売り手の国（dedup は成約の売り手の国）。
public static partial class Instruments
{
    internal const string CapskipHeaderText = "quarter\tyear\tnation\tgood\tskips\tskipsPricedOut\tofferWeight\tshadowAllWeight\tshadowWeight\taddedWeight\tdedupWeight\tdedupBaseWeight";

    const int CkSkips = 0, CkPricedOut = 1, CkOffer = 2, CkShadowAll = 3, CkShadow = 4, CkAdded = 5, CkDedup = 6, CkDedupBase = 7, CkLen = 8;

    sealed class CkWorld { public double[,]? Q; }
    sealed class CkSink { public System.IO.StreamWriter? W; }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<World, CkWorld> CkWorlds = new();
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MultiNationSim, CkSink> CkSinks = new();

    /// <summary>試験の読み口：国×財の和（無ければ null）。</summary>
    internal static double[,]? CapskipPeek(World w) => CkWorlds.TryGetValue(w, out var s) ? s.Q : null;

    static double[,] CkOf(World w) { var s = CkWorlds.GetValue(w, _ => new CkWorld()); return s.Q ??= new double[Catalog.Goods.Length, CkLen]; }

    /// <summary>受け口（:963 の分岐・Instruments.On かつ四半期の最初の清算）：<paramref name="sellerWorld"/> は売り手の国・<paramref name="ms"/> は売り手の市場・<paramref name="mb"/> は買い手の市場・
    /// <paramref name="buyer"/> は買い手の州・<paramref name="access"/> は買い手の行の入手率・<paramref name="priced"/> はエンジンの値段の門・<paramref name="addedW"/> はエンジンが実際に足した重さ（旗 0 は 0）。</summary>
    internal static void CapSkip(World sellerWorld, int g, Market ms, Market mb, Region buyer, double access, double wg, bool priced, double addedW)
    {
        if (sellerWorld.WarmupFrozen || g < 0 || g >= Catalog.Goods.Length) return;
        var q = CkOf(sellerWorld);
        double offer = ms.Stock - ms.TargetStock; if (!(offer > 0)) offer = 0;
        q[g, CkSkips] += 1; q[g, CkOffer] += offer * wg; q[g, CkAdded] += addedW;
        if (!priced) q[g, CkPricedOut] += 1;
        // 影＝買い手の X を買い手の自分の値段で読んだ値（rpT÷lp＝参照価格×州の物価水準÷州の値段＝為替は約される）
        double rp = Catalog.RefPrices[g] * buyer.PriceLevel, p = mb.Price;
        if (!(rp > 0) || !(p > 0)) return;
        var cf = sellerWorld.Coef; double eps = cf.PriceElasticity;
        double d = mb.LastDemand, prod = mb.LastProduced;
        double xp = d * System.Math.Pow(rp / p, 2.0 * eps) - (prod + mb.LastImport);
        double xn = cf.TradeBuyBySupplyCurve > 0 ? mb.TargetStock + d * System.Math.Pow(rp / p, eps) - prod - mb.Stock : mb.TargetStock + d - prod - mb.Stock;
        double x = access * System.Math.Max(0, System.Math.Min(xp, xn));
        double sh = System.Math.Min(offer, x);
        if (!(sh > 0)) return;
        q[g, CkShadowAll] += sh * wg;
        if (priced) q[g, CkShadow] += sh * wg;
    }

    /// <summary>受け口（:1083・Instruments.On かつその行の skipQ が正）：同じ行の次の売り手の側の未充足から引いた重さ（エンジンの実際の側）と、引く前の未充足の重さ（(1322)）。</summary>
    internal static void CapSkipDedup(World sellerWorld, int g, double dedupW, double baseW)
    {
        if (sellerWorld.WarmupFrozen || g < 0 || g >= Catalog.Goods.Length) return;
        var a = CkOf(sellerWorld);
        a[g, CkDedup] += dedupW; a[g, CkDedupBase] += baseW;
    }

    /// <summary>四半期の末：全ての国×その四半期に飛ばしか差し引きが有った財の 1 行。書いたら国の和を 0 へ戻す。</summary>
    static void CapskipTable(MultiNationSim sim, ulong seed, int q)
    {
        var ck = CkSinks.GetValue(sim, _ => new CkSink());
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            if (!CkWorlds.TryGetValue(w, out var s) || s.Q is not { } a) continue;
            if (Wants("capskip_s"))
            {
                if (ck.W is null) { ck.W = Open("capskip", seed); ck.W.WriteLine(CapskipHeaderText); }
                for (int g = 0; g < a.GetLength(0); g++)
                {
                    if (!(a[g, CkSkips] > 0) && !(a[g, CkDedup] > 0)) continue;
                    var cols = new List<string>(4 + CkLen) { I(q), I(w.Year), NameOf(w), Catalog.Goods[g].Name };
                    for (int k = 0; k < CkLen; k++) cols.Add(F(a[g, k]));
                    ck.W.WriteLine(string.Join('\t', cols));
                }
            }
            System.Array.Clear(a);
        }
    }

    static void CapskipClose(MultiNationSim sim) { if (CkSinks.TryGetValue(sim, out var s)) { s.W?.Dispose(); s.W = null; } }
}
