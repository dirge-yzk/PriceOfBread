using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段4：問題の一覧（SimCore/Display/ProblemScan.cs）の番人。機構と不変条件だけを見る（画面の見た目は --uishot が見る）。
/// 読むだけ（盤面を書かない）・閾値で選ばない（0 でない物を大きさの順）・飛ぶ先の行が実在する・動かせる法の点灯と同じ 1 本。</summary>
public class ProblemScanTests
{
    private static (MultiNationSim Sim, World W) Board(int quarters = 8)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return (sim, sim.Nations[0].World);
    }

    private static byte[] Bytes(MultiNationSim sim)
    {
        using var ms = new MemoryStream();
        SaveGame.Save(sim, ms);
        return ms.ToArray();
    }

    [Fact]
    public void 種類の名前は規則に合い_全ての種類が名前を持つ()
    {
        foreach (var k in Enum.GetValues<ProblemKind>())
        {
            string n = ProblemScan.KindName(k);
            Assert.False(string.IsNullOrEmpty(n), $"{k} に名前が無い");
            Assert.True(DisplayNames.IsValid(n), $"種類の名前が規則に合わない: {n}");
        }
        Assert.Equal(Enum.GetValues<ProblemKind>().Length, Enum.GetValues<ProblemKind>().Select(ProblemScan.KindName).Distinct().Count());
    }

    [Fact]
    public void 読むだけ_走査の前後で盤面は1バイトも変わらない()
    {
        var (sim, w) = Board();
        var before = Bytes(sim);
        var items = ProblemScan.Scan(sim, w, new FiscalFlows(0, 0, 0, 0, new double[(int)TreasuryCat.Count]));
        Assert.NotEmpty(items);   // 8 四半期回した盤面には何かしらの問題がある（空なら検出が働いていない）
        Assert.Equal(before, Bytes(sim));
    }

    [Fact]
    public void 並びは種類の順_同じ種類は大きさの降順_鍵は重ならず_大きさは有限で床より大きい()
    {
        var (sim, w) = Board();
        var items = ProblemScan.Scan(sim, w);
        Assert.Equal(items.Count, items.Select(p => p.Key).Distinct().Count());
        Assert.All(items, p =>
        {
            Assert.True(double.IsFinite(p.Size));
            Assert.True(p.Size > ProblemScan.Floor(p.Unit), $"{p.Key} の大きさが床以下");
        });
        for (int i = 1; i < items.Count; i++)
        {
            Assert.True((int)items[i].Kind >= (int)items[i - 1].Kind, "種類の宣言順に並ぶ");
            if (items[i].Kind == items[i - 1].Kind) Assert.True(items[i].Size <= items[i - 1].Size, "同じ種類の中は大きさの降順");
        }
        Assert.Equal(items.Select(p => p.Key).ToArray(), ProblemScan.Scan(sim, w).Select(p => p.Key).ToArray());   // 決定的
    }

    [Fact]
    public void 飛ぶ先の行は実在する_表を持つ対象は表の鍵と行の鍵が引ける()
    {
        var (sim, w) = Board();
        ITable? Of(string key) => TableCatalog.ByKey(key);
        foreach (var p in ProblemScan.Scan(sim, w))
        {
            if (p.TableKey.Length == 0) { Assert.Equal("", p.RowKey); continue; }
            var t = Of(p.TableKey);
            Assert.NotNull(t);
            Assert.Equal(p.Page, t!.Page);
            Assert.True(t.Read(sim, w).Rows.Any(r => r.Key == p.RowKey), $"飛び先の行が無い: {p.Key}（{p.Target}）→ {p.TableKey}/{p.RowKey}");
        }
    }

    [Fact]
    public void 倒産した工場は財ごとに数えて並ぶ_床で丸まる休止の端数は並べない()
    {
        var (sim, w) = Board();
        var rg = w.Regions.First();
        foreach (var r in w.Regions) foreach (var p in r.Producers) p.Bankrupt = false;   // いったん全て動いている形にしてから 1 つだけ止める
        var prod = rg.Producers.First(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
        prod.Bankrupt = true;
        var items = ProblemScan.Scan(sim, w).Where(p => p.Kind == ProblemKind.Bankruptcy).ToList();
        var mine = Assert.Single(items, p => p.RowKey == prod.GoodId.ToString());
        Assert.True(mine.Size >= 1.0);
        Assert.Equal(Catalog.Goods[prod.GoodId].Name, mine.Target);
    }

    [Fact]
    public void 止まった棟数は財カードの棟数を超えない_技術ゲートの閉じた生産者は数えない()
    {
        var (sim, w) = Board();
        foreach (var r in w.Regions) foreach (var p in r.Producers) p.Bankrupt = false;
        w.Regions.First().Producers.First(p => Catalog.Goods[p.GoodId].Tier != GoodTier.Service).Bankrupt = true;
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            double cap = 0;   // 財カードの工場カウンターと同じ集合（技術ゲートが開いた生産者）の棟数（国営＋民営）
            foreach (var r in w.Regions)
                foreach (var p in r.Producers)
                    if (p.GoodId == g && Discovery.Gate(r, p) > ProductionDiag.TechGateClosed) cap += w.StateUnitsOf(p) + w.PrivateUnitsOf(p);
            double stopped = ProductionTables.StoppedUnits(w, g);
            Assert.True(stopped <= cap + 1e-9, $"{Catalog.Goods[g].Name}：止まった棟数 {stopped} が棟数 {cap} を超えた");
        }
        foreach (var it in ProblemScan.Scan(sim, w).Where(p => p.Kind == ProblemKind.Bankruptcy))
            Assert.Equal(ProductionTables.StoppedUnits(w, int.Parse(it.RowKey)), it.Size, 9);   // 一覧と数え方の 1 本が同じ
    }

    [Fact]
    public void 州の飢えは主食の不足の割合_充足が満ちた州は並ばない()
    {
        var (sim, w) = Board();
        foreach (var r in w.Regions) r.FoodFill = 1.0;
        Assert.DoesNotContain(ProblemScan.Scan(sim, w), p => p.Kind == ProblemKind.RegionHunger);
        var rg = w.Regions.First();
        rg.FoodFill = 0.5;
        var h = Assert.Single(ProblemScan.Scan(sim, w), p => p.Kind == ProblemKind.RegionHunger);
        Assert.Equal(rg.Name, h.RowKey);
        Assert.Equal(0.5, h.Size, 6);
        Assert.Equal(Unit.Share, h.Unit);
    }

    [Fact]
    public void 動かせる法の点灯の印は_法の改正の方向ごとの照会と同じ()
    {
        var (_, w) = Board();
        ulong bits = ProblemScan.MovableLawBits(w);
        // 決め打ちの盤面（英・プロイセン・種 1789・8 四半期）に、点灯した方向も消灯した方向もある＝全部点灯・全部消灯で空回りしない
        int lit = 0;
        for (int i = 0; i < ProblemScan.ReformSteps.Count; i++)
        {
            var (axis, inc) = ProblemScan.ReformSteps[i];
            bool on = (bits & (1UL << i)) != 0;
            Assert.Equal(Government.CanStep(w, axis, inc), on);
            if (on) lit++;
        }
        Assert.InRange(lit, 1, ProblemScan.ReformSteps.Count - 1);
    }

    [Fact]
    public void 動かせる法の行は点灯した方向と同じ数_出す条件が偽の法は並べない()
    {
        var (sim, w) = Board();
        var items = ProblemScan.Scan(sim, w).Where(p => p.Kind == ProblemKind.MovableLaw).ToList();
        ulong bits = ProblemScan.MovableLawBits(w);
        int lit = Enumerable.Range(0, ProblemScan.ReformSteps.Count).Count(i => (bits & (1UL << i)) != 0);
        Assert.True(lit > 0, "決め打ちの盤面に点灯した方向がある");
        // 点灯した方向はどれも、画面の行を持つ（出す条件が真の）法に結ばれる＝行の数は点灯した方向の数と同じ
        for (int i = 0; i < ProblemScan.ReformSteps.Count; i++)
            if ((bits & (1UL << i)) != 0)
                Assert.Contains(LawCatalog.All, d => d.Axis == ProblemScan.ReformSteps[i].Axis && d.Slot == LawSlot.Law && d.IsVisible(w));
        Assert.Equal(lit, items.Count);
        Assert.All(items, p => Assert.True(LawCatalog.ByField(p.RowKey)!.IsVisible(w)));
    }

    [Fact]
    public void 財の不足はまだ世界のどこにも作れない財を並べない()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        var w = sim.Nations[0].World;
        var unavailable = ProductionDiag.UnavailableGoods(sim, w.Coef);
        foreach (var p in ProblemScan.Scan(sim, w).Where(p => p.Kind == ProblemKind.GoodShortage))
        {
            int g = int.Parse(p.Key["short|".Length..]);
            Assert.False(unavailable[g], $"作れない財が不足に並んだ: {p.Target}");
        }
    }

    [Fact]
    public void 輸送力不足は州の律速率の不足分で_運ぶ見込みが無い州と余裕のある州は並ばない_貿易の頁へ結ぶ()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        for (int i = 0; i < 8; i++) sim.StepQuarter();
        var w = sim.Nations[0].World;
        // 機構：律速率と潜在量を直接置いて、並びと大きさを読む（創発の量は試験に書かない）
        var tight = w.Regions[0]; var loose = w.Regions[^1];
        tight.LastTradePotential = 100; tight.LastTradeThrottle = 0.25;
        loose.LastTradePotential = 100; loose.LastTradeThrottle = 1.0;
        var items = ProblemScan.Scan(sim, w).Where(p => p.Kind == ProblemKind.TransportShortage).ToList();
        var t = Assert.Single(items, p => p.Key == "transport|" + tight.Name);
        Assert.Equal(0.75, t.Size, 9);
        Assert.Equal(Unit.Share, t.Unit);
        Assert.Equal(Page.Trade, t.Page);
        Assert.DoesNotContain(items, p => p.Key == "transport|" + loose.Name);
        Assert.All(items, p => Assert.InRange(p.Size, 0.0, 1.0));
        tight.LastTradePotential = 0;   // 運びたかった量が無ければ不足ではない
        Assert.DoesNotContain(ProblemScan.Scan(sim, w), p => p.Kind == ProblemKind.TransportShortage && p.Key == "transport|" + tight.Name);
    }
}
