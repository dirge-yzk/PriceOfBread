using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 犯罪と抑止（治安）：秩序＝実効治安維持力の人口カバレッジ。秩序が崩れ・大衆が困窮すると窃盗（純移転）が湧く。
/// 治安維持力に革命鎮圧以外の日常の役割を与える＝警察を捨てた国家の平時コスト。
/// </summary>
public class CrimeTests
{
    [Fact]
    public void 治安維持力を捨てた不満国家には犯罪が湧き保存則は保たれる()
    {
        var guarded = new Simulation(NationConfig.Russia().ToScenario(101), new NullAgent());   // 高い治安維持力＝抑止が効く（人口正規化後＝強度700）
        var lawless = new Simulation(NationConfig.Russia().ToScenario(101), new NullAgent());
        lawless.World.PoliticalLockEnabled = false;   // 治安維持力の放棄→犯罪の機構を検証＝政治ロック（軍/旧支配層が解体に反対）とは直交なので隔離
        lawless.World.Policy.CoercionTarget = 0;   // 警察を解体
        guarded.Run(200);
        var ex = Record.Exception(() => lawless.Run(200));   // 窃盗＝純移転＝保存則（§11.6）が破れない
        Assert.Null(ex);
        Assert.True(lawless.World.PublicOrder < guarded.World.PublicOrder,
            $"警察を解体しても秩序が落ちない（{guarded.World.PublicOrder:P0}→{lawless.World.PublicOrder:P0}）");
        Assert.True(lawless.World.CrimeRate > guarded.World.CrimeRate,
            $"無秩序で犯罪が湧かない（抑止あり{guarded.World.CrimeRate:P3} vs なし{lawless.World.CrimeRate:P3}）");
    }

    [Fact]
    public void 犯罪の率は州の秩序を読まず国の秩序だけで決まる_旗RuleOrderByRegionに依らない()
    {
        // (850) c69po：州の秩序 order_r（Rule の無統治の目標の旗）は犯罪の率に入れない＝闘争度を 2 回数えない（思想⑨）。
        // 同じ盤面（警察が足りない・州ごとに闘争度が違う）を旗 0 と旗 1 で 1 か月だけ踏み、州の犯罪率・国の秩序・POP の貯蓄がビットで同じことを見る。
        Simulation Make(double flag)
        {
            var s = new Simulation(NationConfig.Russia().ToScenario(101), new NullAgent());
            var w = s.World;
            w.Coef.RuleOrderByRegion = flag;
            w.Coercion = 0.4 * System.Math.Max(1.0, w.Coef.PoliticalReferencePop) * w.Coef.CrimeOrderPerPop;   // 切る前の被覆 0.4
            int i = 0;
            foreach (var r in w.Regions) { double m = 0.05 + 0.3 * (i++ % 3); foreach (var p in r.Pops) { p.Militancy = m; p.Satisfaction = 0.2; } }
            return s;
        }
        var a = Make(0.0); var b = Make(1.0);
        Assert.True(a.World.PoliticsEnabled, "前提：政治のある世界（犯罪の段が走る）");
        Crime.Step(a.World, a.World.Coef); Crime.Step(b.World, b.World.Coef);
        Assert.Equal(System.BitConverter.DoubleToInt64Bits(a.World.PublicOrder), System.BitConverter.DoubleToInt64Bits(b.World.PublicOrder));
        Assert.True(a.World.PublicOrder < 1.0, $"前提：警察が足りない {a.World.PublicOrder}");
        Assert.Contains(a.World.Regions, r => r.CrimeRate > 0);
        for (int i = 0; i < a.World.Regions.Count; i++)
        {
            var ra = a.World.Regions[i]; var rb = b.World.Regions[i];
            Assert.Equal(System.BitConverter.DoubleToInt64Bits(ra.CrimeRate), System.BitConverter.DoubleToInt64Bits(rb.CrimeRate));
            for (int k = 0; k < ra.Pops.Count; k++)
                Assert.Equal(ra.Pops[k].Savings.Value, rb.Pops[k].Savings.Value);
        }
    }

    [Fact]
    public void 満たされた社会は警察が薄くても平和()   // 犯罪の母体＝貧しく不満な大衆（豊かさそのものが抑止）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(103), new UtilityAgent());   // 統治AI＝豊かな社会
        sim.Run(200);
        Assert.True(sim.World.CrimeRate < 0.005,
            $"豊かに統治された社会で犯罪が高止まり（{sim.World.CrimeRate:P2}/月）");
    }
}
