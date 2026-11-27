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
}
