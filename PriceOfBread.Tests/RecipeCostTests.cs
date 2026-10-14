using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(328) 流れG ①：同じ財に並ぶ別レシピの自己費用を生産性の比で割る器 <see cref="Coefficients.RecipeCostFollowsProductivity"/>
/// （<see cref="Simulation.ApplyRecipeCostFollowsProductivity"/>・既定0＝休眠）の番人。
/// 期待は式の写しでなく**手で書いた表**（レシピ→自己費用の播種の値と生産性の比。値は goods.json の productivity と Simulation.cs のレシピの字面から人が読んだ物＝
/// 実装の計算とは別の出所）と、作者の原則（同一生産コスト辺りの産出能力＝自己費用×生産性が人力と同じ）の関係で書く。</summary>
public class RecipeCostTests
{
    static Simulation Seed(Coefficients c) =>
        new(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = 1, Coef = c }, new NullAgent());

    static Producer? Find(Region r, int good, string recipe) => r.Producers.Find(p => p.GoodId == good && p.RecipeName == recipe);

    [Fact]
    public void ゲート0は全州全生産者で費用も生産性もビット一致()
    {
        var off = Seed(new Coefficients());
        var zero = Seed(new Coefficients { RecipeCostFollowsProductivity = 0.0 });
        Assert.Equal(off.World.Regions.Count, zero.World.Regions.Count);
        for (int i = 0; i < off.World.Regions.Count; i++)
        {
            var a = off.World.Regions[i].Producers; var b = zero.World.Regions[i].Producers;
            Assert.Equal(a.Count, b.Count);
            for (int j = 0; j < a.Count; j++)
            {
                Assert.Equal(a[j].MarginalCost, b[j].MarginalCost);
                Assert.Equal(a[j].Productivity, b[j].Productivity);
            }
        }
    }

    [Fact]
    public void ゲート1は動力のレシピの自己費用を生産性の比で割り_手で書いた表と一致する()
    {
        var off = Seed(new Coefficients()); var on = Seed(new Coefficients { RecipeCostFollowsProductivity = 1.0 });
        var rOff = off.World.Regions[0]; var rOn = on.World.Regions[0];
        // (レシピ, 財, 播種の自己費用, 生産性の比＝人が goods.json とレシピの字面から読んだ値)
        var table = new (string Recipe, int Good, double Mc0, double Ratio)[]
        {
            ("畜力", Catalog.LaborId, 0.35, 1.3), ("蒸気機関", Catalog.LaborId, 0.35, 2.0), ("電動", Catalog.LaborId, 0.35, 2.5), ("水車", Catalog.LaborId, 0.40, 1.1),
            ("職人", Catalog.ClothingId, 0.35, 0.6), ("職人", Catalog.FurnitureId, 0.35, 0.6),
            ("坩堝鋼", Catalog.SteelId, Catalog.FactoryVa, 0.9 / 1.9), ("甜菜糖", Catalog.SugarId, Catalog.FactoryVa, 1.4 / 2.2),
        };
        foreach (var (recipe, good, mc0, ratio) in table)
        {
            var a = Find(rOff, good, recipe); var b = Find(rOn, good, recipe);
            Assert.True(a is not null && b is not null, $"{Catalog.Goods[good].Name}の{recipe}が無い");
            Assert.Equal(mc0, a!.MarginalCost, 12);               // 前提＝播種の自己費用（手で書いた表と現物が合う）
            Assert.Equal(mc0 / ratio, b!.MarginalCost, 12);        // 新＝自己費用÷生産性の比
            Assert.Equal(a.Productivity, b.Productivity);          // 生産性は触らない
        }
        // 作者の原則（同一生産コスト辺りの産出能力）：播種の自己費用が同じ 0.35 の動力は、自己費用×生産性が人力と同じ（= 同じ費用で出る産出が生産性に比例）
        var human = Find(rOn, Catalog.LaborId, "")!;
        Assert.Equal(0.35, human.MarginalCost, 12);   // 基準レシピ（人力）は動かない
        foreach (var name in new[] { "畜力", "蒸気機関", "電動" })
        {
            var p = Find(rOn, Catalog.LaborId, name)!;
            Assert.Equal(human.MarginalCost * human.Productivity, p.MarginalCost * p.Productivity, 9);
        }
        // 蒸気の動力は、ゲート1で自己費用が人力の半分（0.175）= 構造的に高かった側が解ける
        Assert.True(Find(rOn, Catalog.LaborId, "蒸気機関")!.MarginalCost < human.MarginalCost);
        Assert.True(Find(rOff, Catalog.LaborId, "蒸気機関")!.MarginalCost >= Find(rOff, Catalog.LaborId, "")!.MarginalCost);
    }

    [Fact]
    public void ゲートは指数_半分なら平方根_投入と生産性は触らない()
    {
        var off = Seed(new Coefficients()); var half = Seed(new Coefficients { RecipeCostFollowsProductivity = 0.5 });
        var a = Find(off.World.Regions[0], Catalog.LaborId, "蒸気機関")!; var b = Find(half.World.Regions[0], Catalog.LaborId, "蒸気機関")!;
        Assert.Equal(0.35 / System.Math.Sqrt(2.0), b.MarginalCost, 12);
        Assert.Equal(a.Inputs.Count, b.Inputs.Count);
        for (int i = 0; i < a.Inputs.Count; i++) { Assert.Equal(a.Inputs[i].GoodId, b.Inputs[i].GoodId); Assert.Equal(a.Inputs[i].QtyPerOutput, b.Inputs[i].QtyPerOutput); }
    }

    [Fact]
    public void 動くのは別レシピだけ_基準レシピと一次産品と基準の無い財は動かない()
    {
        var off = Seed(new Coefficients()); var on = Seed(new Coefficients { RecipeCostFollowsProductivity = 1.0 });
        int moved = 0, rgoSeen = 0, noBaseSeen = 0;
        for (int i = 0; i < off.World.Regions.Count; i++)
        {
            var a = off.World.Regions[i].Producers; var b = on.World.Regions[i].Producers;
            Assert.Equal(a.Count, b.Count);
            for (int j = 0; j < a.Count; j++)
            {
                Assert.Equal(a[j].Productivity, b[j].Productivity);
                bool changed = a[j].MarginalCost != b[j].MarginalCost;
                if (changed) moved++;
                // 期待は構築とは別の出所＝財の層（役務の財は基準レシピを持たない・労働力だけが人力を基準に持つ）と名前
                var g = Catalog.Goods[a[j].GoodId];
                bool hasBase = g.Tier != GoodTier.Service || a[j].GoodId == Catalog.LaborId;
                bool alt = a[j].RecipeName.Length > 0 && !a[j].IsRgo && hasBase;
                if (a[j].IsRgo) rgoSeen++;
                if (a[j].RecipeName.Length > 0 && !hasBase) noBaseSeen++;
                // 比が 1 のレシピ（石油火力ほか）は割っても変わらない＝動いたなら別レシピ・動かないなら別レシピでないか比が 1
                if (changed) Assert.True(alt, $"別レシピでないのに自己費用が動いた：{g.Name}/{a[j].RecipeName}");
                if (!alt) Assert.Equal(a[j].MarginalCost, b[j].MarginalCost);
            }
        }
        Assert.True(moved > 0 && rgoSeen > 0 && noBaseSeen > 0, $"前提の確認：動いた{moved}・一次産品{rgoSeen}・基準の無い財{noBaseSeen}");
        // 教育の並置（教区学校・公立学校）は基準が無い＝動かない（運転時に Productivity が書き換わるので播種の比に意味が無い）
        foreach (var p in on.World.Regions[0].Producers.FindAll(p => p.GoodId == Catalog.EducationId)) Assert.Equal(0.4, p.MarginalCost, 12);
    }

    [Fact]
    public void 共有世界の植民地格の州も蒸気の動力の自己費用が畳まれる_PromoteColonyToRegion_の経路()
    {
        // 構築の経路が複数（BuildProvince／PromoteColonyToRegion）＝どれかが呼びを落とすと一部の州だけ畳まれない。BuildProvince は上のテスト（単国）が通す。
        // ⚠MultiNationSim の coef 引数は、開幕の国の州の播種（cfg.ToScenario が新しい係数を作る）には届かず、植民地格の州の昇格（w.Coef）にだけ届く
        //   ＝この経路の番人は植民地格の州を見る。開幕の国の州を共有世界で畳むには POB_COEF（Coefficients の構築時に読む）を使う。
        var sim = new MultiNationSim(WorldData.Nations(), 1, _ => new UtilityAgent(), new Coefficients { RecipeCostFollowsProductivity = 1.0 });
        int colonial = 0; var notFolded = new List<string>();
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.Tier != RegionTier.Colonial) continue;
                var steam = Find(r, Catalog.LaborId, "蒸気機関");
                if (steam is null) continue;
                colonial++;
                if (System.Math.Abs(steam.MarginalCost - 0.175) > 1e-12) notFolded.Add(r.Name);
            }
        Assert.True(colonial > 20, $"植民地格の州 {colonial}＝テストにならない");
        Assert.True(notFolded.Count == 0, $"畳まれていない植民地格の州 {notFolded.Count}/{colonial}：" + string.Join("・", notFolded.Take(12)));
    }
}
