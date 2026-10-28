using System;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(629) 疫病の発生と伝播の確率が読む都市の割合（<see cref="World.EpidemicUrbanDrive"/>）の番人。旗 <see cref="Coefficients.EpidemicDriveByNationUrban"/> が 0 なら最も都市化率の高い 1 州の割合、
/// 正なら国の都市の人口÷国の人口。期待値は州の POP を直に足す手書きの走査から作る（World の読み口を期待値の出所にしない）。
/// 配線の番人は発生（Simulation の年次の block）と伝播（MultiNationSim.StepEpidemicTransmission）を別々に踏む＝片方だけ読み先を戻すと赤。</summary>
public class EpidemicDriveByNationUrbanTests
{
    /// <summary>手書きの走査：州ごとの (都市の人口, 人口) を足す。人口 1e-9 未満の州は最大値の候補から外す（TryUrbanShare と同じ規約を独立に書く）。</summary>
    private static (double max, double nation) Manual(World w)
    {
        double max = 0, urbanSum = 0, popSum = 0;
        foreach (var r in w.Regions)
        {
            double p = 0, u = 0;
            foreach (var pop in r.Pops) { p += pop.Size; if (pop.IsUrban) u += pop.Size; }
            urbanSum += u; popSum += p;
            if (p >= 1e-9 && u / p > max) max = u / p;
        }
        return (max, popSum > 1e-9 ? urbanSum / popSum : 0.0);
    }

    /// <summary>1 州だけが小さく高い割合・他は大きく低い割合の盤面を作る（州の割合の最大と国の割合が大きく離れる）。小さい州を返す。</summary>
    private static Region SkewBoard(World w, double smallScale, double otherUrbanScale)
    {
        var pick = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        foreach (var pop in pick.Pops) pop.Size *= smallScale;
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, pick)) continue;
            foreach (var pop in r.Pops) if (pop.IsUrban) pop.Size *= otherUrbanScale;
        }
        return pick;
    }

    /// <summary>総人口を保ったまま盤面を偏らせる（人口保存の不変条件を踏まない）：1 つの州（返す）に小さな都市の POP だけを残し（割合はほぼ 1）、
    /// 他の都市の POP と、その州の他の POP の人数は別の州の農村 POP へ移す。国の割合は残した人数÷総人口＝ほぼ 0。</summary>
    private static Region ConcentrateBoard(World w)
    {
        var pick = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
        var keep = pick.Pops.First(p => p.IsUrban && p.Size > 0);
        var sink = w.Regions.Where(r => !ReferenceEquals(r, pick)).SelectMany(r => r.Pops).First(p => !p.IsUrban && p.Size > 0);
        double moved = 0;
        foreach (var r in w.Regions)
            foreach (var pop in r.Pops)
            {
                if (ReferenceEquals(pop, keep) || ReferenceEquals(pop, sink)) continue;
                if (!pop.IsUrban && !ReferenceEquals(r, pick)) continue;   // 他の州の農村はそのまま
                double leave = ReferenceEquals(r, pick) && !pop.IsUrban ? 1e-9 : 1e-12;
                moved += pop.Size - leave; pop.Size = leave;
            }
        moved += keep.Size - 1e-6; keep.Size = 1e-6;
        sink.Size += moved;
        return pick;
    }

    [Fact]
    public void 旗0は最も過密な州の割合_旗1は国の人口加重の割合を手書きの式と同じ値で返し旗1は旗0を超えない()
    {
        var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
        SkewBoard(w, 1e-3, 0.05);
        var (max, nation) = Manual(w);
        Assert.True(max > 0 && nation > 0, "前提：盤面に都市の人口がある");
        Assert.True(nation < 0.5 * max, "前提：1 州の割合と国の割合が離れた盤面（旗の違いが見える）");
        w.Coef.EpidemicDriveByNationUrban = 0;
        Assert.Equal(max, w.EpidemicUrbanDrive(w.Coef), 12);
        w.Coef.EpidemicDriveByNationUrban = 1;
        Assert.Equal(nation, w.EpidemicUrbanDrive(w.Coef), 12);
        Assert.True(w.EpidemicUrbanDrive(w.Coef) < max, "旗 1 の割合は旗 0 より小さい盤面");
    }

    [Fact]
    public void 旗1は任意の盤面で旗0を超えず一つの州の国では二つの旗が同じ値()
    {
        foreach (ulong seed in new ulong[] { 155, 7, 1789 })
        {
            var w = new Simulation(NationConfig.France().ToScenario(seed), new NullAgent()).World;
            foreach (double scale in new[] { 1.0, 1e-2, 40.0 })
            {
                foreach (var r in w.Regions) foreach (var pop in r.Pops) if (pop.IsUrban) pop.Size *= scale;
                w.Coef.EpidemicDriveByNationUrban = 0; double d0 = w.EpidemicUrbanDrive(w.Coef);
                w.Coef.EpidemicDriveByNationUrban = 1; double d1 = w.EpidemicUrbanDrive(w.Coef);
                Assert.True(d1 <= d0 + 1e-15, $"seed {seed} 倍率 {scale}：旗 1 の割合 {d1} が旗 0 の割合 {d0} を超えた");
                foreach (var r in w.Regions) foreach (var pop in r.Pops) if (pop.IsUrban) pop.Size /= scale;
            }
        }
        // 一つの州だけに人口が残る国（他の州の人口は 0）：国の割合＝その州の割合
        {
            var w = new Simulation(NationConfig.France().ToScenario(155), new NullAgent()).World;
            var keep = w.Regions.First(r => r.Pops.Any(p => p.IsUrban && p.Size > 0));
            foreach (var r in w.Regions) if (!ReferenceEquals(r, keep)) foreach (var pop in r.Pops) pop.Size = 0;
            w.Coef.EpidemicDriveByNationUrban = 0; double d0 = w.EpidemicUrbanDrive(w.Coef);
            w.Coef.EpidemicDriveByNationUrban = 1; double d1 = w.EpidemicUrbanDrive(w.Coef);
            Assert.True(d0 > 0);
            Assert.Equal(d0, d1, 12);
        }
    }

    [Fact]
    public void 発生の確率は旗1で国の割合を読む_旗0では最も過密な州の割合を読む()
    {
        // 盤面：最も過密な州は小さく（割合は高い）・国の割合はほぼ 0。EpidemicChance 2 は旗 0 の確率が 1 を超える大きさ＝旗 0 は最初の年次の block で必ず起き、旗 1 は国の割合が 1e-6 未満＝起きない。
        bool Fires(double flag)
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
            var w = sim.World;
            ConcentrateBoard(w);
            var (max, nation) = Manual(w);
            Assert.True(max > 0.5, "前提：最も過密な州の割合が 0.5 を超える（EpidemicChance 2 で旗 0 の確率が 1 を超える）");
            Assert.True(nation < 1e-6 * max, $"前提：国の割合が 1 州の割合よりはるかに小さい（{nation} / {max}）");
            w.Coef.EpidemicChance = 2;
            w.Coef.EpidemicOnsetSanitationWeight = 0;   // 衛生の割引なし
            w.Coef.EpidemicDriveByNationUrban = flag;
            Assert.Equal(0, w.EpidemicActiveYears);
            for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();   // 30 日目の月決算＝最初の年次の block
            return w.EpidemicActiveYears > 0;
        }
        Assert.True(Fires(0), "旗 0 では最も過密な州の割合で発生する（番人の前提）");
        Assert.False(Fires(1), "旗 1 では国の割合で確率が桁違いに小さい＝発生しない（Simulation の読み先を戻すと赤）");
    }

    [Fact]
    public void 伝播の確率は旗1で国の割合を読む_旗0では最も過密な州の割合を読む()
    {
        var step = typeof(MultiNationSim).GetMethod("StepEpidemicTransmission", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(step);
        bool Receives(double flag)
        {
            var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789);
            var recv = sim.Nations[0].World;
            SkewBoard(recv, 1e-9, 1e-12);
            var (max, nation) = Manual(recv);
            Assert.True(max > 0.01);
            Assert.True(nation < 1e-6 * max, $"前提：国の割合が 1 州の割合よりはるかに小さい（{nation} / {max}）");
            sim.Coef.EpidemicTransmitChance = 1e6;   // 旗 1 の確率は 1e6×曝露(≤1)×国の割合(<1e-6×max)＜1、旗 0 は曝露が 1e-6 を超えれば 1 超
            sim.Coef.EpidemicDriveByNationUrban = flag;
            foreach (var n in sim.Nations) n.World.Coef.EpidemicDriveByNationUrban = flag;
            // 全ての他国を感染源にし、受け手を 0 番に固定（他の国の盤面は触らない）
            for (int i = 0; i < sim.Nations.Count; i++) sim.Nations[i].World.EpidemicActiveYears = i == 0 ? 0 : 3;
            step!.Invoke(sim, null);
            return recv.EpidemicActiveYears > 0;
        }
        Assert.True(Receives(0), "旗 0 では最も過密な州の割合で伝播する（番人の前提）");
        Assert.False(Receives(1), "旗 1 では国の割合で確率が桁違いに小さい＝伝播しない（MultiNationSim の読み先を戻すと赤）");
    }

    [Fact]
    public void 旗の既定は0で従来の式のまま()
    {
        // 既定の旗は 0＝従来の式（既定が 1 に変わると既定の軌道が変わる＝採るときに倒すのはこの 1 行と旧の枝の削除）
        Assert.Equal(0.0, new Coefficients().EpidemicDriveByNationUrban);
    }
}
