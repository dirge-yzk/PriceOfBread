using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(848) 線 c69eh：人の居ない POP の口座のお金の相続 R1（<see cref="EmptyPopEstate"/>・旗 <see cref="Coefficients.EmptyPopEstate"/>）の機構の番人。
/// 事前登録 measurements/m848_20261105_prereg_eh.txt §3〜§5。期待は試験が自分で数える：払い手＝試験が置いた人数 0 の POP と、盤面の〈人数 ≦1e-12 で在庫が正〉の POP（試験が自分で選ぶ）・
/// 受け手の比＝試験が読む人数・州の和＝試験が移しの前に数えた和。突然変異の表は measurements/m848_20261105/mutation.txt。</summary>
[Collection("Instruments")]
public class EmptyPopEstateTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    /// <summary>国庫から 1 人の POP へ置く（重み 1＝人数 0 の POP でも受け取る）。</summary>
    private static void Put(World w, Pop p, double x) => Assert.Equal(x, World.Transfer((w, null), (w, [p], [1.0]), x));

    private static bool Empty(Pop p) => !(p.Size > 1e-12);

    private static long[] Bits(World w)
    {
        var l = new List<long>();
        foreach (var r in w.Regions) foreach (var p in r.Pops) l.Add(BitConverter.DoubleToInt64Bits(p.Savings.Value));
        return l.ToArray();
    }

    /// <summary>州 <paramref name="r"/> の人の居る POP を 2 つ空にして在庫を置く（州に人の居る POP が 2 つ以上残るように選ぶ）。</summary>
    private static (Pop A, Pop B) MakeEmpty(World w, Region r, double xa, double xb)
    {
        var live = r.Pops.Where(p => p.Size > 1e-12).ToList();
        Assert.True(live.Count >= 4, $"州 {r.Name} の人の居る POP が少ない（{live.Count}）");
        var a = live[^1]; var b = live[^2];
        a.Size = 0; b.Size = 0;
        Put(w, a, xa); Put(w, b, xb);
        return (a, b);
    }

    private static Region Pick(World w, int skip = 0) => w.Regions.Where(r => r.Pops.Count(p => p.Size > 1e-12) >= 6).Skip(skip).First();

    [Fact]
    public void 旗0では人数0のPOPが在庫を持っていても1ビットも変わらない()
    {
        var sim = Two(1848UL); var w = sim.Nations[0].World;
        MakeEmpty(w, Pick(w), 500, 200);
        w.Coef.EmptyPopEstate = 0;
        var before = Bits(w);
        EmptyPopEstate.Step(w);
        Assert.Equal(before, Bits(w));
    }

    [Fact]
    public void 人の居ないPOPの在庫は同じ州の人の居るPOPへ人数の比で移り和は保たれる()
    {
        var sim = Two(1849UL); var w = sim.Nations[0].World;
        var r = Pick(w);
        var (a, b) = MakeEmpty(w, r, 500, 200);
        w.Coef.EmptyPopEstate = 1;
        // 試験が自分で数える：払い手の和（人数 ≦1e-12 で在庫が正）と受け手（人数 ＞1e-12）
        double due = r.Pops.Where(p => Empty(p) && p.Savings.Value > 0).Sum(p => p.Savings.Value);
        Assert.True(due >= 700 - 1e-9);
        var heirs = r.Pops.Where(p => p.Size > 1e-12).ToList();
        var s0 = heirs.Select(p => p.Savings.Value).ToArray();
        double total0 = r.Pops.Sum(p => p.Savings.Value);
        EmptyPopEstate.Step(w);
        foreach (var p in r.Pops.Where(Empty)) Assert.True(Math.Abs(p.Savings.Value) <= 1e-12 || p.Savings.Value < 0, $"人の居ない POP に在庫が残る {p.ClassName} {p.Savings.Value}");
        double got = 0, sizeSum = heirs.Sum(p => p.Size);
        for (int i = 0; i < heirs.Count; i++)
        {
            double d = heirs[i].Savings.Value - s0[i];
            got += d;
            double want = due * heirs[i].Size / sizeSum;   // 人数の比（試験が自分で組む）
            Assert.True(Math.Abs(d - want) <= 1e-9 * Math.Max(1, want), $"人数の比でない {heirs[i].ClassName}：受けた {d}・人数の比 {want}");
        }
        Assert.True(Math.Abs(got - due) <= 1e-9 * due, $"受けた和 {got} ≠ 払い手の和 {due}");
        Assert.True(Math.Abs(r.Pops.Sum(p => p.Savings.Value) - total0) <= 1e-9 * Math.Max(1, Math.Abs(total0)), "州の在庫の和が変わった");
    }

    /// <summary>(848) 事前登録 §11：受け手の 1 人あたりの受け取りは州の中で 1 本（相対 1e-9）＝末尾の受け手の人数がごく小さくても（Pop.Distribute は端数を末尾の受け手へ寄せる）。</summary>
    [Fact]
    public void 末尾の受け手の人数がごく小さくても1人あたりの受け取りは1本()
    {
        var sim = Two(1855UL); var w = sim.Nations[0].World;
        var r = Pick(w);
        var live = r.Pops.Where(p => p.Size > 1e-12).ToList();
        var e = live[0]; e.Size = 0; Put(w, e, 1234.5678);
        live[^1].Size = 1e-10;   // 並びの末尾の受け手の人数をごく小さく
        if (live[^1].Savings.Value > 0) World.Transfer((w, [live[^1]]), (w, null, null), live[^1].Savings.Value);   // 在庫を 0 の近くへ（受けた額を引き算で読むときの桁落ちを避ける）
        Assert.True(Math.Abs(live[^1].Savings.Value) < 1e-9);
        w.Coef.EmptyPopEstate = 1;
        var heirs = r.Pops.Where(p => p.Size > 1e-12).ToList();
        var s0 = heirs.Select(p => p.Savings.Value).ToArray();
        EmptyPopEstate.Step(w);
        double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
        for (int i = 0; i < heirs.Count; i++) { double x = (heirs[i].Savings.Value - s0[i]) / heirs[i].Size; lo = Math.Min(lo, x); hi = Math.Max(hi, x); }
        Assert.True(hi > 0);
        Assert.True((hi - lo) / hi <= 1e-9, $"1 人あたりの受け取りが州の中で 1 本でない：(最大−最小)÷最大＝{(hi - lo) / hi}");
    }

    [Fact]
    public void 人の居るPOPからは出ない()
    {
        var sim = Two(1850UL); var w = sim.Nations[0].World;
        MakeEmpty(w, Pick(w), 800, 100);
        MakeEmpty(w, Pick(w, 1), 300, 50);
        w.Coef.EmptyPopEstate = 1;
        var live = new List<(Pop P, double S)>();
        foreach (var r in w.Regions) foreach (var p in r.Pops) if (p.Size > 1e-12) live.Add((p, p.Savings.Value));
        EmptyPopEstate.Step(w);
        foreach (var (p, s) in live) Assert.True(p.Savings.Value >= s, $"人の居る POP から出た {p.ClassName}：{s}→{p.Savings.Value}");
    }

    [Fact]
    public void 州に人の居るPOPが無ければ動かない()
    {
        var sim = Two(1851UL); var w = sim.Nations[0].World;
        var r = Pick(w);
        foreach (var p in r.Pops) p.Size = 0;
        Put(w, r.Pops[0], 400); Put(w, r.Pops[3], 90);
        var before = r.Pops.Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray();
        w.Coef.EmptyPopEstate = 1;
        EmptyPopEstate.Step(w);
        Assert.Equal(before, r.Pops.Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray());
    }

    [Fact]
    public void 人数0のPOPが在庫を持たない州は旗1でも1ビットも変わらない()
    {
        var sim = Two(1852UL); var w = sim.Nations[0].World;
        MakeEmpty(w, Pick(w), 500, 200);
        var quiet = w.Regions.Where(r => !r.Pops.Any(p => Empty(p) && p.Savings.Value > 0)).ToList();
        Assert.NotEmpty(quiet);
        var before = quiet.Select(r => r.Pops.Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray()).ToList();
        w.Coef.EmptyPopEstate = 1;
        EmptyPopEstate.Step(w);
        for (int i = 0; i < quiet.Count; i++)
            Assert.Equal(before[i], quiet[i].Pops.Select(p => BitConverter.DoubleToInt64Bits(p.Savings.Value)).ToArray());
    }

    [Fact]
    public void 別の州へは移らない_州ごとの在庫の和は保たれる()
    {
        var sim = Two(1853UL); var w = sim.Nations[0].World;
        MakeEmpty(w, Pick(w), 600, 0.5);
        MakeEmpty(w, Pick(w, 1), 250, 75);
        MakeEmpty(w, Pick(w, 2), 40, 10);
        w.Coef.EmptyPopEstate = 1;
        var sums = w.Regions.Select(r => r.Pops.Sum(p => p.Savings.Value)).ToArray();
        EmptyPopEstate.Step(w);
        for (int i = 0; i < w.Regions.Count; i++)
        {
            double s = w.Regions[i].Pops.Sum(p => p.Savings.Value);
            Assert.True(Math.Abs(s - sums[i]) <= 1e-9 * Math.Max(1, Math.Abs(sums[i])), $"州 {w.Regions[i].Name} の在庫の和が変わった {sums[i]}→{s}");
        }
    }

    [Fact]
    public void 計器の列は移った額を読み_出と入りが州の中で閉じる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_estate_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(1854UL);
        var w = sim.Nations[0].World;
        var r0 = Pick(w);
        try
        {
            sim.StepQuarter(); Instruments.Quarter(sim, 1854UL, 0);   // 四半期 0 は旗 0 のまま（台帳を始める）
            MakeEmpty(w, r0, 900, 300);
            w.Coef.EmptyPopEstate = 1;   // 国 0 だけ旗 1（国 1 は旗 0＝移さないが due は数える）
            sim.Nations[1].World.Coef.EmptyPopEstate = 0;   // 前提を既定に頼らず明示（点火の後の既定は 1）
            foreach (var n in sim.Nations) Instruments.Begin(n.World);   // 月初の登録
            foreach (var n in sim.Nations) EmptyPopEstate.Step(n.World);
            Instruments.Quarter(sim, 1854UL, 1);
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "cearn_s1854.tsv"));
            Assert.Equal(Instruments.CearnHeaderText, lines[0]);
            var h = lines[0].Split('\t');
            int Ix(string c) => Array.IndexOf(h, c);
            double V(string[] c, string col) => double.Parse(c[Ix(col)], System.Globalization.CultureInfo.InvariantCulture);
            string nation0 = Instruments.NameOf(sim.Nations[0].World);
            double movedAll = 0; bool sawR0 = false;
            foreach (var l in lines.Skip(1))
            {
                var c = l.Split('\t');
                Assert.Equal(h.Length, c.Length);
                if (c[Ix("quarter")] != "1") continue;
                double moved = V(c, "estateMovedPerQuarter"), inn = V(c, "estateInPerQuarter"), due = V(c, "estateDuePerQuarter");
                Assert.True(Math.Abs(inn - moved) <= 1e-9 * Math.Max(1, moved), $"出と入りが州の中で閉じない {c[Ix("region")]}：{moved}・{inn}");
                Assert.True(V(c, "estateOutFromLivePerQuarter") <= 1e-12, "人の居る POP から出た");
                Assert.True(V(c, "estateInToEmptyPerQuarter") <= 1e-12, "人の居ない POP へ入った");
                Assert.True(V(c, "estateRecvPerCapitaSpread") <= 1e-9, $"受けた額÷人数が州の中で 1 本でない {c[Ix("region")]}");
                Assert.True(moved <= due * (1 + 1e-9) + 1e-12, $"移した額がその月の人の居ない在庫を超える {c[Ix("region")]}：{moved}＞{due}");
                if (c[Ix("nation")] != nation0) Assert.Equal(0.0, moved);   // 旗 0 の国は移さない
                movedAll += moved;
                if (c[Ix("region")] == r0.Name && c[Ix("nation")] == nation0) { sawR0 = true; Assert.True(moved >= 1200 - 1e-6, $"置いた在庫が移っていない {moved}"); }
            }
            Assert.True(sawR0);
            Assert.True(movedAll > 0);
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>(854) 反証役 低 6（(848) の突然変異 E6）：旗 0 は早い return で何もしない＝在庫に触れない・乱数を引かない・リストを確保しない。人数 0 で在庫が正の POP と受け手がいる盤面（早い return を外せば移しが走る盤面）で確かめる。
    /// 乱数は同じ種で作った 2 つの世界の片方だけ Step を呼び、世界が持つ系列（FactionRng・(1413) で AiRng は消した）の次の値が一致するかで見る（DeterministicRng は状態の読み手を持たない）。確保は GC.GetAllocatedBytesForCurrentThread の前後差（JIT のために 1 回呼んでから測る）。
    /// 計器は切ってある（Instruments.On が真だと頭の EstateDue が確保する）＝この収集は Instruments と直列。突然変異の表は measurements/m854_20261105/mutation_854.txt。</summary>
    [Fact]
    public void 旗0は早くreturnする_在庫に触れず乱数を引かずリストを確保しない()
    {
        Assert.False(Instruments.On, "前提が崩れた：計器が入っている（EstateDue が確保する）");
        var simA = Two(1860UL); var wA = simA.Nations[0].World;
        var simB = Two(1860UL); var wB = simB.Nations[0].World;
        MakeEmpty(wA, Pick(wA), 500, 200);
        MakeEmpty(wB, Pick(wB), 500, 200);
        wA.Coef.EmptyPopEstate = 0; wB.Coef.EmptyPopEstate = 0;
        Assert.NotNull(wA.FactionRng);
        EmptyPopEstate.Step(wA);                                   // JIT（測る前に 1 回）
        var before = Bits(wA);
        long a0 = GC.GetAllocatedBytesForCurrentThread();
        EmptyPopEstate.Step(wA);
        long a1 = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0L, a1 - a0);                                 // リストを確保しない
        Assert.Equal(before, Bits(wA));                            // 在庫に触れない（人の居ない POP に在庫が残ったまま）
        Assert.Equal(wB.FactionRng!.NextULong(), wA.FactionRng!.NextULong());   // 乱数を引かない（B は一度も Step を呼んでいない）
        Assert.True(Pick(wA).Pops.Any(p => Empty(p) && p.Savings.Value > 0), "前提が崩れた：人の居ない POP の在庫が無い（早い return を外しても動かない盤面）");
    }
}
