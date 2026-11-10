using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1025) 線 c96rd：旗 <see cref="Coefficients.ResourceRowsFix"/>（world.json の州の resources に足した史実の産地の 10 行）の器の番人。事前登録 measurements/m1025_20261110_prereg_rd.txt [7]
/// 賦存は州の構築で 1 回だけ掛かる＝係数は構築の前に Scenario へ置く（UbiquitousMineralTests と同じ形）。
/// 期待は手で置く：州の配列は足す前と足した後を手で書いた表・Productivity は財の生産性（Catalog）×7.0（FullChainProductivityScale）×{0.4・3.5・4.0}。
/// 対照の件数の下限を置く（何もしない実装で緑にしない）。
/// </summary>
public class ResourceRowsTests
{
    private const ulong Seed = 1789UL;
    private static readonly string[] NationsWithRows = ["イギリス", "プロイセン", "アメリカ", "清", "オーストリア", "スペイン"];
    private static readonly int[] Added5 = [Catalog.IronOreId, Catalog.CopperOreId, Catalog.LeadOreId, Catalog.SaltId, Catalog.FishId];

    /// <summary>足す前（旗 0）の 9 行の州の配列＝00f50935 の world.json から手で写した。</summary>
    private static readonly (string City, int[] Res)[] Old =
    [
        ("バーミンガム", [Catalog.IronOreId, Catalog.IronId, Catalog.CoalId, Catalog.TinOreId]),
        ("マンチェスター", [Catalog.CottonClothId, Catalog.CoalId]),
        ("グラスゴー", [Catalog.CoalId, Catalog.SailingShipId]),
        ("ブレスラウ", [Catalog.CoalId, Catalog.LeadOreId, Catalog.PotatoId]),
        ("セントポール", []),
        ("広州", [Catalog.TeaId, Catalog.SilkId, Catalog.SugarCaneId]),
        ("グラーツ", [Catalog.IronOreId, Catalog.LivestockId]),
        ("ビルバオ", []),
        ("セビーリャ", [Catalog.FruitId]),
    ];

    /// <summary>足した後（旗 1）の州の配列＝事前登録 §1 の表（末尾に足す）。</summary>
    private static readonly (string City, int[] Res)[] New =
    [
        ("バーミンガム", [Catalog.IronOreId, Catalog.IronId, Catalog.CoalId, Catalog.TinOreId, Catalog.CopperOreId]),
        ("マンチェスター", [Catalog.CottonClothId, Catalog.CoalId, Catalog.SaltId]),
        ("グラスゴー", [Catalog.CoalId, Catalog.SailingShipId, Catalog.FishId]),
        ("ブレスラウ", [Catalog.CoalId, Catalog.LeadOreId, Catalog.PotatoId, Catalog.IronOreId]),
        ("セントポール", [Catalog.LeadOreId]),
        ("広州", [Catalog.TeaId, Catalog.SilkId, Catalog.SugarCaneId, Catalog.IronOreId]),
        ("グラーツ", [Catalog.IronOreId, Catalog.LivestockId, Catalog.SaltId]),
        ("ビルバオ", [Catalog.IronOreId]),
        ("セビーリャ", [Catalog.FruitId, Catalog.CopperOreId, Catalog.LeadOreId]),
    ];

    private static List<Simulation> Build(double flag)
    {
        var list = new List<Simulation>();
        foreach (var name in NationsWithRows)
        {
            var cfg = WorldData.Nations().First(n => n.Name == name);
            var sc = cfg.ToScenario(Seed, sharedWorld: true);
            typeof(Scenario).GetProperty("Coef")!.SetValue(sc, new Coefficients { ResourceRowsFix = flag });
            var s = new Simulation(sc, new NullAgent());
            s.World.StepsDiplomacy = false;
            list.Add(s);
        }
        return list;
    }

    private static readonly Lazy<List<Simulation>> Off = new(() => Build(0.0));
    private static readonly Lazy<List<Simulation>> On = new(() => Build(1.0));

    private static Region RegionOf(List<Simulation> sims, string city)
    {
        var rs = sims.SelectMany(s => s.World.Regions).Where(r => r.Name == city).ToArray();
        Assert.Single(rs);
        return rs[0];
    }

    private static double Base(int g) => Catalog.Goods[g].Productivity * 7.0;
    private static double Endow(int g) => Catalog.IsMineral(g) ? 4.0 : 3.5;   // 鉱物は 4.0・塩と漁獲は 3.5（事前登録 §4）
    private static bool Near(double a, double b) => Math.Abs(a - b) <= 1e-12 * Math.Max(1.0, Math.Abs(b));
    private static bool IsAdded(string city, int g) => New.Any(n => n.City == city && n.Res.Contains(g)) && !Old.Any(o => o.City == city && o.Res.Contains(g));

    [Fact]
    public void T1_旗0では州の配列は足す前と同じで足した財は0点4()
    {
        var sims = Off.Value;
        int added = 0;
        foreach (var (city, res) in Old)
        {
            var r = RegionOf(sims, city);
            Assert.Equal(res, r.Resources);
            foreach (var g in New.First(n => n.City == city).Res.Where(g => IsAdded(city, g)))
            {
                var p = r.Producers.Single(x => x.IsRgo && x.GoodId == g);
                Assert.True(Near(p.Productivity, Base(g) * 0.4), $"{city}・{Catalog.Goods[g].Name}：{p.Productivity:R}（期待 {Base(g) * 0.4:R}）");
                added++;
            }
        }
        Assert.Equal(10, added);
    }

    [Fact]
    public void T2_旗1では足した行が入り賦存が4倍か3点5倍()
    {
        var sims = On.Value;
        var c = new Coefficients();
        int added = 0;
        foreach (var (city, res) in New)
        {
            var r = RegionOf(sims, city);
            Assert.Equal(res, r.Resources);
            foreach (var g in res.Where(g => IsAdded(city, g)))
            {
                var p = r.Producers.Single(x => x.IsRgo && x.GoodId == g);
                Assert.True(Near(p.Productivity, Base(g) * Endow(g)), $"{city}・{Catalog.Goods[g].Name}：{p.Productivity:R}（期待 {Base(g) * Endow(g):R}）");
                if (c.PlantationRentShare.Value > 0) Assert.Equal(0, p.OwnerPopIndex);   // 賦存の RGO の持ち主は地主階級（Simulation.cs の地代の取り分の枝）
                added++;
            }
        }
        Assert.Equal(10, added);
    }

    /// <summary>賦存の読み手（Productivity）と割譲・アヘン・植民地・計器の読み手（Region.Resources）が同じ配列を見る：足した 5 財の全ての RGO で
    /// 〈Region.Resources に財がある〉⇔〈Productivity＝基準×4.0／3.5〉（産地の発見の行の付いた生産者は除く）。旗 0 と旗 1 の両方。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T3_賦存の読み手と州の配列が同じ(double flag)
    {
        var sims = flag > 0 ? On.Value : Off.Value;
        int n = 0, endowed = 0, addedSeen = 0;
        foreach (var s in sims) foreach (var r in s.World.Regions) foreach (var p in r.Producers)
        {
            if (!p.IsRgo || !Added5.Contains(p.GoodId) || p.SiteRow >= 0) continue;
            bool inRes = Array.IndexOf(r.Resources, p.GoodId) >= 0;
            bool rich = Near(p.Productivity, Base(p.GoodId) * Endow(p.GoodId));
            Assert.True(inRes == rich, $"旗 {flag}・{r.Name}・{Catalog.Goods[p.GoodId].Name}：配列にある={inRes}・賦存の値={p.Productivity:R}（基準×{Endow(p.GoodId)}={Base(p.GoodId) * Endow(p.GoodId):R}）");
            n++; if (inRes) endowed++;
            if (IsAdded(r.Name, p.GoodId)) addedSeen++;
        }
        Assert.True(n >= 100, $"調べた RGO が少ない（{n}）");
        Assert.Equal(flag > 0 ? 19 : 9, endowed);   // 6 国の 5 財の賦存の RGO（足す前 9・足した 10 を加えて 19）
        Assert.Equal(10, addedSeen);
    }

    [Fact]
    public void T4_足していない生産者は旗で1ビットも動かない()
    {
        var a = Off.Value; var b = On.Value;
        int n = 0, diff = 0;
        for (int i = 0; i < a.Count; i++)
        {
            var ra = a[i].World.Regions; var rb = b[i].World.Regions;
            Assert.Equal(ra.Count, rb.Count);
            for (int j = 0; j < ra.Count; j++)
            {
                Assert.Equal(ra[j].Name, rb[j].Name);
                Assert.Equal(ra[j].Producers.Count, rb[j].Producers.Count);
                for (int k = 0; k < ra[j].Producers.Count; k++)
                {
                    var pa = ra[j].Producers[k]; var pb = rb[j].Producers[k];
                    Assert.Equal(pa.GoodId, pb.GoodId);
                    if (pa.IsRgo && IsAdded(ra[j].Name, pa.GoodId)) { diff++; continue; }
                    Assert.Equal(pa.Productivity, pb.Productivity);
                    Assert.Equal(pa.MarginalCost, pb.MarginalCost);
                    Assert.Equal(pa.RentShare.Value, pb.RentShare.Value);
                    Assert.Equal(pa.OwnerPopIndex, pb.OwnerPopIndex);
                    n++;
                }
            }
        }
        Assert.Equal(10, diff);
        Assert.True(n >= 1000, $"調べた生産者が少ない（{n}）");
    }

    /// <summary>EnsureScarceCounts（world.json を直接数える・旗を見ない）は希少鉱物の数えにしか読まれない＝足した 5 財がどの段でも希少鉱物でなければ結果に効かない。</summary>
    [Fact]
    public void T5_足した5財はどの段でも希少鉱物でない()
    {
        foreach (var stage in new[] { 0.0, 1.0, 2.0, 3.0, 4.0 })
        {
            var c = new Coefficients { ScarceMineralOnlySulfur = stage };
            foreach (var (_, g) in Simulation.ResourceRowsAdded) Assert.False(Catalog.IsScarceMineral(g, c), $"段 {stage}・{Catalog.Goods[g].Name} が希少鉱物");
        }
        foreach (var (_, g) in Simulation.ResourceRowsAdded) Assert.False(Catalog.IsScarceMineral(g));
    }

    [Fact]
    public void T6_足した行の表は10行で重ならずworldjsonにある()
    {
        var rows = Simulation.ResourceRowsAdded;
        Assert.Equal(10, rows.Length);
        Assert.Equal(10, rows.Distinct().Count());
        foreach (var (city, g) in rows)
        {
            var p = WorldData.Nations().SelectMany(n => n.Provinces).Single(x => x.City == city);
            Assert.Contains(g, p.Resources);
            Assert.True(IsAdded(city, g), $"{city}・{Catalog.Goods[g].Name} が手の表の〈足した行〉に無い");
        }
    }
}
