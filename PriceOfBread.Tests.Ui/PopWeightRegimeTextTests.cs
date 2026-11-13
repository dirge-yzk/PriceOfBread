using PriceOfBread.SimCore;

namespace PriceOfBread.Tests;

/// <summary>(1104) の突然変異で赤にならなかった 2 本の穴（measurements/m1104_20261113/mut.txt の M1・M2）を埋める画面の試験。
/// 機構と不変条件だけを見る（期待値は定義から手で書く）。</summary>
public class PopWeightRegimeTextTests
{
    private static World Board() => new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789).Nations[0].World;

    /// <summary>非閉鎖の人口の重み＝（1−β）×財産の取り分＋β×頭数の取り分（財産・頭数の総計は閉鎖も含む）に、投票の声÷0.5 を掛けた物。β を 0・既定・1 で定義どおり。</summary>
    [Fact]
    public void 人口の重みは財産の取り分と頭数の取り分の混ぜ合わせで_階級ごとの合計は三つのブロックの力の和と一致する()
    {
        var w = Board();
        double keep = w.Coef.InterestGroupHeadcountWeight;
        try
        {
            foreach (double beta in new[] { 0.0, keep, 1.0 })
            {
                w.Coef.InterestGroupHeadcountWeight = beta;
                var pops = new List<Pop>(); foreach (var (_, q) in w.AllPopsWithRegion()) pops.Add(q);
                double wealth = 0, heads = 0;
                foreach (var p in pops)
                {
                    wealth += Math.Max(0, p.Savings.Value) + (p.Closed && w.Coef.LandedPowerCapitalization > 0 ? w.Coef.LandedPowerCapitalization * Math.Max(0, p.LandRentPerMonth.Value) : 0);
                    heads += p.Size;
                }
                Assert.True(wealth > 0 && heads > 0);
                var k = InterestGroups.Context(w);
                int checkedPops = 0; double sum = 0;
                foreach (var p in pops)
                {
                    double got = InterestGroups.PopWeight(w, p, k);
                    sum += got;
                    if (p.Closed) continue;
                    double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef) / 0.5;
                    double want = ((1 - beta) * (Math.Max(0, p.Savings.Value) / wealth) + beta * (p.Size / heads)) * voice;
                    Assert.True(Math.Abs(got - want) <= 1e-9 * Math.Max(1e-12, Math.Abs(want)) + 1e-15, $"β={beta} 重み={got} 定義={want}");
                    checkedPops++;
                }
                Assert.True(checkedPops >= 10, $"調べた非閉鎖の人口が少ない {checkedPops}");
                var (t1, t2, t3) = InterestGroups.Blocs(w);
                Assert.True(Math.Abs(sum - (t1 + t2 + t3)) <= 1e-9 * Math.Max(1e-12, sum));
            }
        }
        finally { w.Coef.InterestGroupHeadcountWeight = keep; }
    }

    [Fact]
    public void 年表の統治が変わった行の政体名は日本語で書かれる()
    {
        var cases = new[]
        {
            (SeizureForm.Anarchy, "無政府"), (SeizureForm.Military, "軍事独裁"),
            (SeizureForm.Proletarian, "プロレタリア独裁"), (SeizureForm.Bourgeois, "ブルジョア独裁"),
        };
        foreach (var (seizure, ja) in cases)
        {
            var w = Board();
            w.Polity = Polity.Republic; w.Seizure = seizure;
            w.PendingLog.Clear();
            CrownParliament.Step(w, w.Coef);
            var line = w.PendingLog.LastOrDefault(s => s.Contains("の統治が変わった"));
            Assert.NotNull(line);
            Assert.EndsWith($"（{ja}）", line);
            Assert.DoesNotMatch("[A-Za-z]{3,}", line!);
        }
    }
}
