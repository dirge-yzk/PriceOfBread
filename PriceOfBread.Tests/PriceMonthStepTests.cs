using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1400) c149pm 国内価格の刻みを月へ（係数 <see cref="Coefficients.PriceOnlyStepDays"/> の既定）の機構の試験。
/// 盤面＝英の開幕を試験の中で 1 か月進めた後の 2 か月目（30 日）を日ごとに数える。既定の刻みでは、値段は月の終わりの日（日 29＝月次の締めの直前）の 1 回だけ動き、
/// 日 14 では動かない。最低賃金の床（MinimumWageFloor.AbsFloor＝籠の走査）も月 1 回だけ求める。
/// 対照として同じ盤面で刻み 15（旧の既定）を回し、日 14 に値段が動くこと・床を月 2 回求めることを確かめる（盤面が両方を見分ける）。
/// ⚠突然変異 μ1「既定を 15 に戻す」でこの 2 本が赤。</summary>
public class PriceMonthStepTests
{
    // 日ごとに、値段が動いた市場の数（全ての州×財）と、最低賃金の籠の走査の台帳が増えたか
    private static (int[] moved, bool[] floorScanned) RunMonth(int? priceOnlyStepDays, MinimumWageLaw law)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        if (priceOnlyStepDays is int h) w.Coef.PriceOnlyStepDays = h;
        w.MinimumWage = law;
        for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();   // 開幕の月は需給の入力が 0（最初の締めの前）＝1 か月進めてから 2 か月目を数える
        var moved = new int[Calendar.DaysPerMonth];
        var scanned = new bool[Calendar.DaysPerMonth];
        for (int d = 0; d < Calendar.DaysPerMonth; d++)
        {
            var before = w.Regions.Select(r => r.Markets.Select(m => m.Price).ToArray()).ToArray();
            long led0 = w.Cost[CostLedger.MinimumWageBasketScans];
            sim.StepDay();
            int n = 0;
            for (int i = 0; i < before.Length; i++)
                for (int g = 0; g < before[i].Length; g++)
                    if (w.Regions[i].Markets[g].Price != before[i][g]) n++;
            moved[d] = n;
            scanned[d] = w.Cost[CostLedger.MinimumWageBasketScans] > led0;
        }
        return (moved, scanned);
    }

    // 建設と信用の旬の初日（日 0・10・20）と月の締め（日 29）以外の日＝値段を書くのは国内価格の刻みだけ
    private static readonly int[] QuietDays = Enumerable.Range(1, 28).Where(d => d != 10 && d != 20).ToArray();

    [Fact]
    public void 既定の刻みでは値段は月の終わりだけで動き日14では動かない()
    {
        var (mv, _) = RunMonth(null, MinimumWageLaw.None);
        Assert.True(mv[29] > 0, $"前提が崩れた：日 29 に値段が動いた市場が 0");
        foreach (int d in QuietDays)
            Assert.True(mv[d] == 0, $"既定の刻みで日 {d} に値段が {mv[d]} 市場動いた（月 1 回の刻みなら日 29 だけ）");
        var (mv15, _) = RunMonth(15, MinimumWageLaw.None);   // 対照：旧の既定 15 は日 14 にも動く＝盤面が刻みを見分ける
        Assert.True(mv15[14] > 0, "対照が崩れた：刻み 15 で日 14 に値段が動かない");
    }

    [Fact]
    public void 既定の刻みでは最低賃金の床を月に1回だけ求める()
    {
        var (_, sc) = RunMonth(null, MinimumWageLaw.Living);
        int n = Enumerable.Range(1, Calendar.DaysPerMonth - 1).Count(d => sc[d]);
        Assert.True(sc[29], "前提が崩れた：日 29 に最低賃金の床を求めていない");
        Assert.True(n == 1, $"既定の刻みで最低賃金の床を月に {n} 回求めた（月 1 回の刻みなら 1）");
        var (_, sc15) = RunMonth(15, MinimumWageLaw.Living);   // 対照：刻み 15 は日 14 と日 29 の 2 回
        Assert.Equal(2, Enumerable.Range(1, Calendar.DaysPerMonth - 1).Count(d => sc15[d]));
    }
}
