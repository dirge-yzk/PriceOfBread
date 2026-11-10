using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1020) 線 c95ub：旗 <see cref="Coefficients.UbiquitousMineralExempt"/>（遍在の財＝石灰石・珪砂・粘土を非産地の 0.4 の外へ）の器の番人。事前登録 measurements/m1020_20261110_prereg_ub.txt [7]。
/// 賦存は播種（州の構築・植民地の昇格）で 1 回だけ掛かる＝係数は構築の前に Scenario へ置く（構築後に w.Coef を書いても Productivity は動かない）。
/// 期待は手で置く：財の生産性（Catalog）×7.0（FullChainProductivityScale）×{0.4・1.0・4.0}。対照の件数の下限を全ての試験に置く（何もしない実装で緑にしない）。
/// </summary>
public class UbiquitousMineralTests
{
    private const ulong Seed = 1789UL;
    private static readonly int[] U3 = [Catalog.LimestoneId, Catalog.SilicaSandId, Catalog.ClayId];

    /// <summary>産地の州を持つ 3 国（ロンドン＝石灰石・プラハ＝珪砂・ロッテルダム＝粘土）を共有世界の形で 1 国ずつ作る。</summary>
    private static List<Simulation> Build(double flag)
    {
        var list = new List<Simulation>();
        foreach (var city in new[] { "ロンドン", "プラハ", "ロッテルダム" })
        {
            var cfg = WorldData.Nations().First(n => n.Provinces.Any(p => p.City == city));
            var sc = cfg.ToScenario(Seed, sharedWorld: true);
            var coef = new Coefficients { UbiquitousMineralExempt = flag };
            typeof(Scenario).GetProperty("Coef")!.SetValue(sc, coef);
            var s = new Simulation(sc, new NullAgent());
            s.World.StepsDiplomacy = false;
            list.Add(s);
        }
        return list;
    }

    private static double Base(int g) => Catalog.Goods[g].Productivity * 7.0;
    private static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-12 * Math.Max(1.0, Math.Abs(b));

    /// <summary>非産地の 3 財の RGO（resources に無い・産地の発見の行が無い）を財ごとに集める。</summary>
    private static Dictionary<int, List<Producer>> NonSource(List<Simulation> sims)
    {
        var d = U3.ToDictionary(g => g, _ => new List<Producer>());
        foreach (var s in sims) foreach (var r in s.World.Regions) foreach (var p in r.Producers)
            if (p.IsRgo && d.ContainsKey(p.GoodId) && p.SiteRow < 0 && Array.IndexOf(r.Resources, p.GoodId) < 0) d[p.GoodId].Add(p);
        return d;
    }

    [Fact]
    public void T1_旗0では非産地の3財は0点4のまま()
    {
        foreach (var (g, ps) in NonSource(Build(0.0)))
        {
            Assert.True(ps.Count >= 10, $"財{Catalog.Goods[g].Name}：非産地の対照が少ない（{ps.Count}）");
            foreach (var p in ps) Assert.True(Near(p.Productivity, Base(g) * 0.4), $"財{Catalog.Goods[g].Name}：{p.Productivity:R}（期待 {Base(g) * 0.4:R}）");
        }
    }

    [Fact]
    public void T2_旗1では非産地の3財は基準の1倍()
    {
        foreach (var (g, ps) in NonSource(Build(1.0)))
        {
            Assert.True(ps.Count >= 10, $"財{Catalog.Goods[g].Name}：非産地の対照が少ない（{ps.Count}）");
            foreach (var p in ps) Assert.True(Near(p.Productivity, Base(g)), $"財{Catalog.Goods[g].Name}：{p.Productivity:R}（期待 {Base(g):R}）");
        }
    }

    [Fact]
    public void T3_旗1でも他の非産地の鉱物は0点4のまま()
    {
        int n = 0;
        foreach (var s in Build(1.0)) foreach (var r in s.World.Regions) foreach (var p in r.Producers)
        {
            if (!p.IsRgo || p.SiteRow >= 0 || Array.IndexOf(r.Resources, p.GoodId) >= 0) continue;
            if (p.GoodId != Catalog.IronOreId && p.GoodId != Catalog.CoalId && p.GoodId != Catalog.CopperOreId) continue;
            Assert.True(Near(p.Productivity, Base(p.GoodId) * 0.4), $"{r.Name} 財{Catalog.Goods[p.GoodId].Name}：{p.Productivity:R}（期待 {Base(p.GoodId) * 0.4:R}）");
            n++;
        }
        Assert.True(n >= 20, $"他の鉱物の非産地の対照が少ない（{n}）");
    }

    [Fact]
    public void T4_旗1でも産地の州は4倍のまま()
    {
        var seen = new HashSet<int>();
        foreach (var s in Build(1.0)) foreach (var r in s.World.Regions) foreach (var p in r.Producers)
        {
            if (!p.IsRgo || Array.IndexOf(U3, p.GoodId) < 0 || Array.IndexOf(r.Resources, p.GoodId) < 0) continue;
            Assert.True(Near(p.Productivity, Base(p.GoodId) * 4.0), $"{r.Name} 財{Catalog.Goods[p.GoodId].Name}：{p.Productivity:R}（期待 {Base(p.GoodId) * 4.0:R}）");
            seen.Add(p.GoodId);
        }
        Assert.Equal(3, seen.Count);   // 3 財とも産地の州の対照がある（ロンドン・プラハ・ロッテルダム）
    }

    [Theory]
    [InlineData(0.0, 0.4)]
    [InlineData(1.0, 1.0)]
    public void T5_植民地の昇格の州でも同じ(double flag, double expect)
    {
        var s = Build(flag)[0];
        var reg = Simulation.PromoteColonyToRegion(s.World, s.World.Coef, new Colony { Name = "遍在の財の試験の植民地", Resources = [], PopSize = 10, Population = 1.0, Latitude = 10, Longitude = 100 }, RegionTier.Colonial);
        var seen = new HashSet<int>(); int other = 0;
        foreach (var p in reg.Producers)
        {
            if (!p.IsRgo || p.SiteRow >= 0) continue;
            if (Array.IndexOf(U3, p.GoodId) >= 0) { Assert.True(Near(p.Productivity, Base(p.GoodId) * expect), $"財{Catalog.Goods[p.GoodId].Name}：{p.Productivity:R}（期待 {Base(p.GoodId) * expect:R}）"); seen.Add(p.GoodId); }
            else if (p.GoodId == Catalog.IronOreId || p.GoodId == Catalog.CoalId) { Assert.True(Near(p.Productivity, Base(p.GoodId) * 0.4)); other++; }
        }
        Assert.Equal(3, seen.Count);
        Assert.True(other >= 2, $"他の鉱物の対照が少ない（{other}）");
    }

    [Fact]
    public void T6_計器の級は同じ述語に従う()
    {
        foreach (var (flag, cls, rule) in new[] { (0.0, "N", 0.4), (1.0, "S", 1.0) })
        {
            int n = 0;
            var sims = Build(flag);
            foreach (var s in sims) foreach (var r in s.World.Regions) foreach (var p in r.Producers)
            {
                if (!p.IsRgo || p.SiteRow >= 0 || Array.IndexOf(U3, p.GoodId) < 0 || Array.IndexOf(r.Resources, p.GoodId) >= 0) continue;
                var x = Instruments.RoRead(s.World, r, p);
                Assert.Equal(cls, x.Class);
                Assert.True(Near(x.Rule, rule) && Near(x.Endow, rule), $"{r.Name} 財{Catalog.Goods[p.GoodId].Name}：級 {x.Class} 規則 {x.Rule:R} 倍率 {x.Endow:R}（期待 {rule}）");
                n++;
            }
            Assert.True(n >= 30, $"旗 {flag}：対照が少ない（{n}）");
        }
    }
}
