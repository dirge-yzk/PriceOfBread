using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1209) ハーネスの〈満足度・最小国〉＝建国が途中でない国のうち、直近 8 四半期の満足度の平均（SatRecentMean）が最小の国。
/// 平均が無い国（NaN）は最後の一点（MeanSatisfaction）。WorstSat・WorstSatName・WorstSatDetail は同じ国を指す。
/// 盤面は試験の中で NationMetrics を置く（試合は回さない）。閾値 0.05 合格・0.02 注意は Validation.Evaluate の物。
/// </summary>
public class HarnessSatRecentTests
{
    static NationMetrics N(string name, double last, double recent, bool spawned = false, int regions = 1, int founding = 1)
        => new(name, 0.5, last, 0.0, 0.0, 0.6, 0.2, 0.3, 1000, 0, 0, spawned, regions, founding, 0.9, 1000,
               SatRecentMean: recent, SatRecentSwing: double.IsNaN(recent) ? double.NaN : 0.1);

    static SimMetrics M(params NationMetrics[] ns) => new(1950, ns, 0, 0.6, 3, 0, 1.0, 20);

    static Verdict SatVerdict(SimMetrics m) => Validation.Evaluate(m).Single(c => c.Name == "満足度・最小国").Verdict;

    [Fact]
    public void T1_最後の一点が0でも直近8四半期の平均が0点12なら合格()
    {
        var m = M(N("甲", last: 0.0, recent: 0.12), N("乙", last: 0.30, recent: 0.30));
        Assert.Equal(0.12, m.WorstSat);
        Assert.Equal(Verdict.Pass, SatVerdict(m));
    }

    [Fact]
    public void T2_直近8四半期ずっと0の国は不合格()
    {
        var m = M(N("乙", last: 0.30, recent: 0.30), N("甲", last: 0.0, recent: 0.0));
        Assert.Equal(0.0, m.WorstSat);
        Assert.Equal("甲", m.WorstSatName);
        Assert.Equal(Verdict.Fail, SatVerdict(m));
    }

    [Fact]
    public void T3_平均がNaNの国は最後の一点で判定する()
    {
        // 平均ありの国を先に置く＝NaN を比べに入れると NaN の国は選ばれない（WorstSat が 0.30 になる）
        var m = M(N("乙", last: 0.30, recent: 0.30), N("甲", last: 0.01, recent: double.NaN));
        Assert.Equal(0.01, m.WorstSat);
        Assert.Equal("甲", m.WorstSatName);
        Assert.Equal(Verdict.Fail, SatVerdict(m));
    }

    [Fact]
    public void T4_最小国の名と詳細は判定に使った国を指す()
    {
        var m = M(N("甲", last: 0.0, recent: 0.12, regions: 3, founding: 4),
                  N("乙", last: 0.08, recent: 0.06, regions: 7, founding: 9),
                  N("丙", last: 0.0, recent: 0.0, spawned: true, regions: 5, founding: 6));
        Assert.Equal(0.06, m.WorstSat);
        Assert.Equal("乙", m.WorstSatName);
        Assert.StartsWith("・州7/9・", m.WorstSatDetail);
        Assert.Equal(Verdict.Pass, SatVerdict(m));
    }
}
