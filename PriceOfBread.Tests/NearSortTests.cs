using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spt2) 前の並びから挿入で並べ直す部品（<see cref="NearSort"/>）の番人。
/// 呼び手の形（成功なら並べ直した順・失敗なら元の順のまま全部の並べ替え）が、どの場合も全部の並べ替えと同じ並びを返すこと：
/// 顔ぶれが同じで少し動いた月（成功）・NaN がある月（失敗＝全部）・同値の組がある月（失敗＝全部）・顔ぶれが変わった月（失敗＝全部）・大きく崩れた月（失敗＝全部）。
/// 種 10 個の成績表の一致は恒等の証明にならない（作者の手順）＝部品の性質をここで直に見る。
/// ⚠突然変異：〈並べた後の確かめ〉を外すと「全順序でない比べ」が赤。〈顔ぶれの確かめ〉を外しても緑のまま＝顔ぶれが違うと前の並びの写しに同じ要素が 2 度入り、
///   並べた後の確かめ（比べが 0 の隣どうし）が捕まえる＝2 つの確かめは重なっている（記録・measurements/spt2_20261026_identity.txt）。</summary>
public class NearSortTests
{
    private readonly record struct Item(int Key, double Price);

    // 清算の買い手と同じ形の比べ＝値段の降順、同じ値段は名前（ここでは鍵の名前）の順
    private static readonly string[] Names = System.Linq.Enumerable.Range(0, 400).Select(i => $"州{i:D3}").ToArray();
    private static int Cmp(Item x, Item y) => x.Price != y.Price ? y.Price.CompareTo(x.Price) : string.CompareOrdinal(Names[x.Key], Names[y.Key]);

    /// <summary>呼び手と同じ手順：TryReorder が成功すればその順、失敗なら元の順のまま List.Sort（呼び手の元の全部の並べ替え）。</summary>
    private static (List<Item> got, bool usedPrev) CallerSort(List<Item> items, int[]? prev, int prevN)
    {
        var slot = new int[400]; var buf = new Item[items.Count];
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(items);
        bool ok = NearSort.TryReorder(span, prev, prevN, x => x.Key, Cmp, x => double.IsNaN(x.Price), slot, buf);
        if (!ok) items.Sort(Cmp);
        Assert.All(slot, v => Assert.Equal(0, v));   // 作業配列は全て 0 で返る
        return (items, ok);
    }

    private static List<Item> FullSort(List<Item> items) { var c = new List<Item>(items); c.Sort(Cmp); return c; }

    private static (List<Item> month0, int[] prev) Month0(System.Random rng, int n)
    {
        var items = new List<Item>();
        for (int i = 0; i < n; i++) items.Add(new Item(i, 1.0 + rng.NextDouble()));
        var sorted = FullSort(items);
        return (items, sorted.Select(x => x.Key).ToArray());
    }

    [Fact]
    public void 少し動いた月は前の並びから並べ直して全部の並べ替えと同じ()
    {
        var rng = new System.Random(7);
        int used = 0;
        for (int trial = 0; trial < 200; trial++)
        {
            var (items, prev) = Month0(rng, 300);
            var moved = items.Select(x => x with { Price = x.Price * (1 + 0.002 * (rng.NextDouble() - 0.5)) }).ToList();   // 月ごとの小さな動き
            var want = FullSort(moved);
            var (got, usedPrev) = CallerSort(new List<Item>(moved), prev, prev.Length);
            Assert.Equal(want, got);
            if (usedPrev) used++;
        }
        Assert.True(used > 150, $"前の並びを使えた月が少ない（{used}/200）＝部品が効いていない");
    }

    [Fact]
    public void NaN_同値_顔ぶれの変化_大きな崩れは全部の並べ替えに戻って同じ並び()
    {
        var rng = new System.Random(11);
        var (items, prev) = Month0(rng, 200);
        // NaN
        var nan = new List<Item>(items); nan[17] = nan[17] with { Price = double.NaN }; nan[40] = nan[40] with { Price = double.NaN };
        var (g1, u1) = CallerSort(new List<Item>(nan), prev, prev.Length);
        Assert.False(u1); Assert.Equal(FullSort(nan), g1);
        // 同値（値段も名前の順も同じ組は作れない＝値段の同値は名前で決まる＝全順序のまま＝前の並びを使ってよい）
        var tie = new List<Item>(items); tie[5] = tie[5] with { Price = tie[9].Price };
        var (g2, _) = CallerSort(new List<Item>(tie), prev, prev.Length);
        Assert.Equal(FullSort(tie), g2);
        // ±0（比べは != と CompareTo＝-0 と +0 は同じ値段＝名前で決まる）
        var zero = new List<Item>(items); zero[3] = zero[3] with { Price = 0.0 }; zero[4] = zero[4] with { Price = -0.0 };
        var (g3, _) = CallerSort(new List<Item>(zero), prev, prev.Length);
        Assert.Equal(FullSort(zero), g3);
        // 顔ぶれの変化（1 州が抜けて別の州が入る＝数は同じ）
        var swap = new List<Item>(items); swap[50] = new Item(399, 1.5);
        var (g4, u4) = CallerSort(new List<Item>(swap), prev, prev.Length);
        Assert.False(u4); Assert.Equal(FullSort(swap), g4);
        // 数の変化
        var fewer = items.Take(150).ToList();
        var (g5, u5) = CallerSort(new List<Item>(fewer), prev, prev.Length);
        Assert.False(u5); Assert.Equal(FullSort(fewer), g5);
        // 大きな崩れ（値段を逆順に＝挿入の動かす数が上限を越える）
        var rev = items.Select(x => x with { Price = 3.0 - x.Price }).ToList();
        var (g6, u6) = CallerSort(new List<Item>(rev), prev, prev.Length);
        Assert.False(u6); Assert.Equal(FullSort(rev), g6);
        // 前の並びが無い
        var (g7, u7) = CallerSort(new List<Item>(items), null, 0);
        Assert.False(u7); Assert.Equal(FullSort(items), g7);
    }

    [Fact]
    public void 全順序でない比べは並べた後の確かめで全部の並べ替えに戻る()
    {
        // 鍵の名前が同じ 2 州が同じ値段＝比べが 0＝全順序でない＝挿入の結果を使わない
        var items = new List<Item> { new(0, 2.0), new(1, 1.0), new(2, 1.0), new(3, 0.5) };
        int[] prev = [0, 1, 2, 3];
        var slot = new int[8]; var buf = new Item[4];
        System.Comparison<Item> weak = (x, y) => y.Price.CompareTo(x.Price);   // 名前で決めない比べ
        bool ok = NearSort.TryReorder(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(items), prev, 4, x => x.Key, weak, x => double.IsNaN(x.Price), slot, buf);
        Assert.False(ok);
        Assert.Equal(new List<Item> { new(0, 2.0), new(1, 1.0), new(2, 1.0), new(3, 0.5) }, items);   // 失敗のときは元の順のまま
    }

    [Fact]
    public void 生きた財の一覧は使い回しても一から作った一覧と同じ()
    {
        // ★(spt2) 候補3：作り直しを 1 周にし、中身が前と同じなら前の配列を使い回す＝返す一覧は Goods の順のうち死んでいない財（一から作ったものと同じ）
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region;
        var rng = new System.Random(5);
        for (int step = 0; step < 300; step++)
        {
            var g = r.Goods[rng.Next(r.Goods.Count)];
            var m = r.Markets[g.Id];
            if (rng.Next(2) == 0) m.LastDemand = rng.Next(3) == 0 ? 0 : rng.NextDouble(); else m.LastProduced = rng.Next(3) == 0 ? 0 : rng.NextDouble();
            var got = r.EnsureLiveGoods();
            var want = r.Goods.Where(x => !(r.Markets[x.Id].LastDemand < Market.DeadEps && r.Markets[x.Id].LastProduced < Market.DeadEps)).ToArray();
            Assert.Equal(want, got);
            Assert.Same(got, r.EnsureLiveGoods());   // 印が立っていなければ同じ配列
        }
    }
}
