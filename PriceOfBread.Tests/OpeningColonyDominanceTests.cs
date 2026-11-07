using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(952) c84kp 旗 OpeningColonyDominanceFull の番人（事前登録 measurements/m952_20261108_prereg_kp.txt §6）。
/// 開幕の一括昇格（MultiNationSim.PromoteAllColonies）の植民地格の州だけ、支配度を本国の州と同じ 1.0 から始める（初期条件＝Privileges.cs:131 と同じ作法）。
/// 途中の接収（Diplomacy.cs:2269 が呼ぶ Simulation.PromoteColonyToRegion）は 0 のまま。Tier・Rule・RuleInit・本国の州は動かさない。
/// 期待値は字で書く（エンジンの式から作らない）。</summary>
public class OpeningColonyDominanceTests
{
    private static MultiNationSim Build(double flag) =>
        new(WorldData.Nations(), 1789UL, _ => new NullAgent(), new Coefficients { OpeningColonyDominanceFull = flag });

    private static readonly string[] ColonialNations = ["イギリス", "スペイン", "ポルトガル", "ネーデルラント"];

    // T1：旗 1 で構築した直後、植民地格の全ての州の支配度が 1.0（国の名を読む形・一部の国だけの形を拾う）
    [Fact]
    public void T1_旗1で開幕の植民地格の州は全ての国で支配度1から()
    {
        var sim = Build(1.0);
        foreach (var name in ColonialNations)
        {
            var w = sim.Nations.First(n => n.World.SelfPower!.Name == name).World;
            Assert.True(w.Regions.Any(r => r.Tier == RegionTier.Colonial), $"前提が崩れた：{name} に植民地格の州が無い");
        }
        int n0 = 0;
        foreach (var sn in sim.Nations)
            foreach (var r in sn.World.Regions.Where(r => r.Tier == RegionTier.Colonial))
            {
                Assert.True(r.Dominance == 1.0, $"{sn.World.SelfPower!.Name}/{r.Name} の支配度が 1.0 でない（{r.Dominance:R}）");
                n0++;
            }
        Assert.True(n0 >= 40, $"前提が崩れた：植民地格の州が少ない（{n0}）");
    }

    // T2：旗 0 と旗 1 の同じ盤面で、Tier・Rule・RuleInit・州の並び・植民地格でない州の支配度がビットで同じ（形の誤り＝格や区分を動かす・本国の州を動かす）
    [Fact]
    public void T2_旗は格と支配の区分と本国の州を動かさない()
    {
        var a = Build(0.0); var b = Build(1.0);
        Assert.Equal(a.Nations.Count, b.Nations.Count);
        int colonial = 0;
        for (int i = 0; i < a.Nations.Count; i++)
        {
            var ra = a.Nations[i].World.Regions; var rb = b.Nations[i].World.Regions;
            Assert.Equal(ra.Select(r => r.Name), rb.Select(r => r.Name));
            for (int j = 0; j < ra.Count; j++)
            {
                Assert.Equal(ra[j].Tier, rb[j].Tier);
                Assert.Equal(ra[j].Rule, rb[j].Rule);
                Assert.Equal(ra[j].RuleInit, rb[j].RuleInit);
                if (ra[j].Tier != RegionTier.Colonial)
                    Assert.True(ra[j].Dominance.Equals(rb[j].Dominance), $"{ra[j].Name}：植民地格でない州の支配度が旗で変わった（{ra[j].Dominance:R}→{rb[j].Dominance:R}）");
                else colonial++;
            }
        }
        Assert.True(colonial >= 40, $"前提が崩れた：植民地格の州が少ない（{colonial}）");
    }

    // T3：旗 1 でも、接収の経路と同じ関数（Simulation.PromoteColonyToRegion）を直に呼んだ州は 0 から（呼び口の違いで書いた＝関数は変えない）
    [Fact]
    public void T3_旗1でも途中の昇格は支配度0から()
    {
        var sim = Build(1.0);
        var w = sim.Nations.First(n => n.World.SelfPower!.Name == "イギリス").World;
        var reg = Simulation.PromoteColonyToRegion(w, sim.Coef, new Colony { Name = "存在しない植民地", Resources = [], PopSize = 10, Population = 1.0 }, RegionTier.Colonial);
        Assert.Equal(RegionTier.Colonial, reg.Tier);
        Assert.Equal(0.0, reg.Dominance);
    }

    // T4：旗 0 で構築した直後の植民地格の州は支配度 0（器の恒等）
    [Fact]
    public void T4_旗0で開幕の植民地格の州は支配度0から()
    {
        var sim = Build(0.0);
        int n0 = 0;
        foreach (var sn in sim.Nations)
            foreach (var r in sn.World.Regions.Where(r => r.Tier == RegionTier.Colonial))
            {
                Assert.True(r.Dominance == 0.0, $"{sn.World.SelfPower!.Name}/{r.Name} の支配度が 0 でない（{r.Dominance:R}）");
                n0++;
            }
        Assert.True(n0 >= 40, $"前提が崩れた：植民地格の州が少ない（{n0}）");
    }
}
