using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1200) 総取りの枝の削除の後：着地の温度が 0 以下・NaN でも softmax は NaN を出さず、0 は Eps と同じ値になる。既定の 0.15 は旧と同じ値。</summary>
public class JobPoolTemperatureGuardTests
{
    private static (double moved, double total) Run(double? temperature)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        sim.World.Coef.JobSearchRatePerQuarter = new RatePerQuarter(0.10);
        if (temperature.HasValue) sim.World.Coef.JobPoolTemperature = temperature.Value;
        var r = sim.World.Region;
        var farmer = r.Pops[1]; var worker = r.Pops[r.MeritFromIndex];
        farmer.UnemploymentRate = new Share(0.5); farmer.UnemploymentEma = new Share(0.5);
        farmer.EarnedPerMonth = new MoneyPerMonth(0.5 * farmer.Size); worker.EarnedPerMonth = new MoneyPerMonth(10.0 * worker.Size);
        double before = farmer.Size;
        Meritocracy.Step(sim.World, sim.World.Coef);
        return (before - r.Pops[1].Size, r.SumPopulation());
    }

    [Fact]
    public void 温度が0以下やNaNでも移る量は有限で_0はEpsと同じ値()
    {
        var eps = Run(1e-9);
        Assert.True(eps.moved > 0 && double.IsFinite(eps.total), $"前提が崩れた：農民が動かない（{eps.moved}）");
        foreach (double t in new[] { 0.0, -1.0, double.NaN })
        {
            var x = Run(t);
            Assert.True(double.IsFinite(x.moved) && double.IsFinite(x.total), $"温度 {t} で NaN／無限（移る量 {x.moved}・総人口 {x.total}）");
            Assert.Equal(eps.moved, x.moved);
        }
    }

    [Fact]
    public void 既定の温度の移る量は低温の極限を超えない()
    {
        Assert.True(Run(0.15).moved <= Run(1e-9).moved + 1e-9, "低温（近似の総取り）より多く流した");
    }

    private static readonly System.Reflection.MethodInfo RouteViaPoolM =
        typeof(Meritocracy).GetMethod("RouteViaPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    /// <summary>(1206) 既定の温度の番人（(1200) の反証役 v の中＝旧の Run(null) と Run(0.15) の比べは同じ既定を 2 度読む同語反復）：
    /// 行き先が農民と都市労働者の 2 つだけの盤面（仏・兵士が送り手・他の下流の職は所得 0・兵の給与 0）で、農民へ着く量が
    /// **試験の中で手計算した softmax** 1/(1+exp((労働者の所得−農民の所得)/T))×流れ に一致する。T は既定値と 0.5 の 2 通り
    /// ＝既定が別の値へ変わっても 0.5 の腕は T を読む。
    /// ⚠突然変異「手計算の温度を 2 倍」で赤。</summary>
    [Fact]
    public void 既定の温度は手計算のsoftmaxの取り分と一致する()
    {
        (double got, double flow, double farmPW, double workPW) Arm(double? temperature)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var world = sim.World; var c = world.Coef; var r = world.Region;
            if (temperature.HasValue) c.JobPoolTemperature = temperature.Value;
            c.SerfGateCoversPool = 0;
            var s = r.Pops[r.SoldierPopIndex];
            Pop? farm = null, work = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) continue;
                if (p.ClassId == c.SoldierRecruitClassA) farm ??= p;
                else if (p.ClassId == c.SoldierRecruitClassB) work ??= p;
            }
            Assert.True(farm is not null && work is not null && s.Size > 1, "前提が崩れた：農民・都市労働者・兵士");
            for (int i = 0; i < r.Pops.Count; i++)
            {
                var p = r.Pops[i];
                p.UnemploymentRate = new Share(0);
                if (!p.IsSoldier && Meritocracy.ClassOf(r, i) == 0 && !ReferenceEquals(p, farm) && !ReferenceEquals(p, work)) p.EarnedPerMonth = new MoneyPerMonth(0);
            }
            farm!.EarnedPerMonth = new MoneyPerMonth(1.2 * Military.WorkingAdults(farm));
            work!.EarnedPerMonth = new MoneyPerMonth(1.0 * Military.WorkingAdults(work));
            s.EarnedPerMonth = new MoneyPerMonth(0.0);
            double flow = 0.1 * s.Size, f0 = farm.Size;
            object[] args = [r, world, 0.5, 1.0, s, 0, flow, 1.0, 1.0, c, 0.0, true];
            RouteViaPoolM.Invoke(null, args);
            double perHead = Military.WorkingAdults(s) / s.Size;
            return (farm.Size - f0, flow, 1.2 * perHead, 1.0 * perHead);
        }
        double defaultT = new Coefficients().JobPoolTemperature;
        Assert.True(defaultT > 0, "前提が崩れた：既定の温度が 0 以下");
        foreach (double? t in new double?[] { null, 0.5 })
        {
            double T = t ?? defaultT;
            var a = Arm(t);
            double share = 1.0 / (1.0 + Math.Exp((a.workPW - a.farmPW) / T));
            Assert.True(a.got > 0 && a.got < a.flow, $"前提が崩れた：2 つへ分かれない（農民へ {a.got}・流れ {a.flow}）");
            Assert.True(Math.Abs(a.got - share * a.flow) <= 1e-9 * a.flow, $"温度 {T}：農民の取り分 {a.got / a.flow:F6} ≠ 手計算の softmax {share:F6}");
        }
    }
}
