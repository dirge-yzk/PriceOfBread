using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1408) 項 3（B13）：旧 DominanceSpeed を 3 本に分けた（DominanceIntegralSpeed・RuleRelaxSpeed・TreatyPortFadeSpeed）。
/// 3 本を別の値にしても、条約港の風化は TreatyPortFadeSpeed だけを読む（取り違えると既定の値が同じでも赤）。
/// 型の試験：3 本とも四半期あたりの割合の型で、既定は 3 本とも 0.025。
/// </summary>
public class DominanceSpeedSplitTests
{
    [Fact]
    public void 三本は四半期あたりの割合の型で既定は旧の値()
    {
        var c = new Coefficients();
        Assert.Equal(0.025, c.DominanceIntegralSpeed.Value);
        Assert.Equal(0.025, c.RuleRelaxSpeed.Value);
        Assert.Equal(0.025, c.TreatyPortFadeSpeed.Value);
        foreach (var n in new[] { "DominanceIntegralSpeed", "RuleRelaxSpeed", "TreatyPortFadeSpeed" })
            Assert.Equal(typeof(FractionPerQuarter), typeof(Coefficients).GetField(n)!.FieldType);
        Assert.Null(typeof(Coefficients).GetField("DominanceSpeed"));
    }

    [Fact]
    public void 条約港の風化は風化の速さだけを読む()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        sim.StartWarmup();
        var d = sim.Diplomacy;
        sim.Coef.DominanceIntegralSpeed = FractionPerQuarter.FromQuarter(0.4);
        sim.Coef.RuleRelaxSpeed = FractionPerQuarter.FromQuarter(0.6);
        sim.Coef.TreatyPortFadeSpeed = FractionPerQuarter.FromQuarter(0.033);
        var sc = NationalScore.ScoresByPower(d);
        var greatS = d.Powers.Where(p => p.Nation != null && d.RankOf(p) == PowerRank.Great).Select(p => sc[p.Id]).OrderBy(x => x).ToList();
        double median = greatS[greatS.Count / 2];
        var pick = d.Powers.Where(p => p.Nation != null && d.RankOf(p) != PowerRank.Great && sc[p.Id] / median < 0.45).OrderByDescending(p => sc[p.Id]).First();
        var region = pick.Nation!.Regions[0];
        region.ForeignRulers = ["測定用"]; region.ForeignShares = [0.8];
        double ratio = sc[pick.Id] / median;
        double expected = 0.8 * (1.0 - Math.Min(1.0, ratio / sim.Coef.TreatyPortLapseRatio.Value) * 0.033);
        d.TreatyPortLapse(sim.Coef, 3);
        Assert.Equal(expected, region.ForeignShares[0], 12);
    }

    // (1414) c151wr 項 5（B13）：Politics の 2 本（支配度の積分＝DominanceIntegralSpeed・4 区分の目標への寄り＝RuleRelaxSpeed）を別の値で読む。
    // 3 本が同じ 0.025 のままだと取り違えても緑＝2 本を別の値にして、同じ 1 四半期の踏みで支配度の増分と区分の増分を別々に確かめる。

    private static (double dRegion, double ungov) OneStep(double integral, double relax)
    {
        var sim = new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent());
        var w = sim.World; var c = w.Coef;
        c.RuleOrderByRegion = 0;
        c.DominanceIntegralSpeed = FractionPerQuarter.FromQuarter(integral);
        c.RuleRelaxSpeed = FractionPerQuarter.FromQuarter(relax);
        w.PublicOrder = 0.5;
        foreach (var r in w.Regions) { r.Rule = [1.0, 0.0, 0.0, 0.0]; r.RuleInit = [1.0, 0.0, 0.0, 0.0]; r.Dominance = 0.5; }
        Politics.CheckSecession(w, c, 0.0);
        var m = w.Regions.Single(r => r.Name == "マルセイユ");
        return (m.Dominance - 0.5, m.Rule[3]);
    }

    [Fact]
    public void 支配度の積分と区分の寄りは別の速さを読む()
    {
        var (d1, u1) = OneStep(0.1, 0.1);
        var (d4, u4) = OneStep(0.4, 0.1);   // 積分だけ 4 倍
        var (_, u9) = OneStep(0.1, 0.3);    // 区分の寄りだけ 3 倍
        Assert.True(System.Math.Abs(d1) > 1e-6, $"前提が崩れた：支配度が動かない（{d1}）");
        Assert.Equal(4.0, d4 / d1, 6);                                  // 支配度の増分は積分の速さに比例し、区分の速さに依らない
        Assert.Equal(u1, u4, 12);                                       // 区分は積分の速さに依らない
        Assert.Equal(3.0, u9 / u1, 6);                                  // 区分の増分は寄りの速さに比例
        Assert.InRange(u1, 0.0030, 0.0032);                             // 無統治の目標 0.5×(1−0.93813)×0.1（RuleSharesFormTests の D2 と同じ盤面・速さだけ別）
    }
}
