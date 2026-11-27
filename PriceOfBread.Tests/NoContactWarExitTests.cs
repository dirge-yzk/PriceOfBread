using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1429) 線 c153nx：旗 <see cref="Coefficients.NoContactCollapseBlank"/> の機構の試験。崩壊の側（厭戦が切り目以上）がいて、
/// 戦争の累計の押しが両側 0（一度も接触していない）なら、その月に白紙で講和する。接触のある崩壊は今の書き換え（±1.5×取り分）のまま。
/// 盤面は試験の中で置く（仏英の 2 国・助走なし・外交の AI は止める・塹壕と上陸の門は外す）。押しを 0 にするのは両国の即応度 0（戦力 0）。
/// 期待値は試験の中の式から作る（エンジンの RevCut・WarEnds は呼ばない）。厭戦 1.0 は切り目（1−重み×革命圧力≤1）以上。
/// 突然変異：M1 新しい条件を外す→T1 が赤／M2 崩壊の条件を外す→T2 が赤／M3 押しの条件を外す→T3 が赤（measurements/m1429_20261128/mut_M*.txt）。</summary>
[Collection("Instruments")]
public class NoContactWarExitTests
{
    private static (MultiNationSim sim, War war, Power fr, Power uk) Board(double flag)
    {
        var sim = new MultiNationSim(Array.ConvertAll(new[] { "フランス", "イギリス" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.NoContactCollapseBlank = flag;
        c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;   // 押しを月の生の値のまま読む・どちらも上陸できる
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!;
        fr.WarExhaustion = 0; uk.WarExhaustion = 0;
        var war = new War { A = fr, B = uk, Score = 0 };
        sim.Diplomacy.Wars.Add(war);
        return (sim, war, fr, uk);
    }

    /// <summary>両国の戦力を 0 にする（押し＝陸軍力×投射＝0）。</summary>
    private static void NoContact(Power fr, Power uk) { fr.Nation!.MilitaryReadiness = 0; uk.Nation!.MilitaryReadiness = 0; }

    [Fact]
    public void T1_接触なしで崩壊の側がいれば1か月で白紙の講和()
    {
        var (sim, war, fr, uk) = Board(1);
        NoContact(fr, uk);
        fr.WarExhaustion = 1.0;   // 崩壊の側
        int e0 = sim.Diplomacy.EndedWars.Count;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.False(war.ForcedPeace, "前提が崩れた：調停で終わった");
        Assert.True(war.PushA + war.PushB == 0, $"前提が崩れた：押しが立った（{war.PushA:G6}/{war.PushB:G6}）");
        Assert.True(uk.WarExhaustion < 0.9 * 0.5, $"前提が崩れた：B の厭戦が疲れ果ての線の付近（{uk.WarExhaustion:G6}）");
        Assert.DoesNotContain(war, sim.Diplomacy.Wars);
        Assert.Equal(e0 + 1, sim.Diplomacy.EndedWars.Count);
        var rec = sim.Diplomacy.EndedWars[^1];
        Assert.Equal(fr.Id, rec.AId); Assert.Equal(uk.Id, rec.BId);
        Assert.Equal(WarVerdict.Blank, rec.Verdict);
        Assert.Equal(0.0, rec.Score);
    }

    [Fact]
    public void T1z_旗0では同じ盤面の戦争が続き戦勝点は0()
    {
        var (sim, war, fr, uk) = Board(0);
        NoContact(fr, uk);
        fr.WarExhaustion = 1.0;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.True(war.PushA + war.PushB == 0, "前提が崩れた：押しが立った");
        Assert.Contains(war, sim.Diplomacy.Wars);
        Assert.Equal(0.0, war.Score);
    }

    [Fact]
    public void T2_接触なしでも崩壊の側がいなければ続く()
    {
        var (sim, war, fr, uk) = Board(1);
        NoContact(fr, uk);
        for (int k = 0; k < 3; k++) sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61 + k);
        Assert.True(fr.WarExhaustion < 0.5 && uk.WarExhaustion < 0.5, $"前提が崩れた：厭戦が崩壊の線の付近（{fr.WarExhaustion:G6}/{uk.WarExhaustion:G6}）");
        Assert.True(war.PushA + war.PushB == 0, "前提が崩れた：押しが立った");
        Assert.Contains(war, sim.Diplomacy.Wars);
        Assert.Equal(3, war.Months);
    }

    [Fact]
    public void T3_接触ありの崩壊は今の取り分の式のまま続く()
    {
        // 押しの比を盤面で置く：探りの盤面で 1 か月の押しを読み、英の即応度を押しの比で下げる（押しは戦力に比例・戦力は即応度に比例）＝英の押し÷仏の押し≈0.1。
        var (ps, pw, pfr, puk) = Board(1);
        ps.Diplomacy.Step(ps.Coef, ps.Rng, month: 61);
        double ca = pw.PushA, cb = pw.PushB;
        Assert.True(ca > 0 && cb > 0, $"前提が崩れた：探りの盤面で押しが立っていない（{ca:G6}/{cb:G6}）");
        var (sim, war, fr, uk) = Board(1);
        uk.Nation!.MilitaryReadiness *= 0.1 * ca / cb;
        fr.WarExhaustion = 1.0;   // 仏が崩壊の側＝英が勝ち側（勝ち側の押し＝PushB）
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61);
        Assert.True(war.PushA > 0 && war.PushB > 0, $"前提が崩れた：押しが片側 0（{war.PushA:G6}/{war.PushB:G6}）");
        double reach = Math.Max(0.0, Math.Min(1.0, 2.0 * war.PushB / (war.PushA + war.PushB + 1e-12)));
        double want = -1.5 * reach;
        Assert.True(Math.Abs(want) < 1.0, $"前提が崩れた：書き換えの後の戦勝点が決定的の線に届く（{want:G6}）");
        Assert.True(uk.WarExhaustion < 0.5, $"前提が崩れた：英の厭戦が疲れ果ての線の付近（{uk.WarExhaustion:G6}）");
        Assert.Contains(war, sim.Diplomacy.Wars);
        Assert.True(Math.Abs(war.Score - want) <= 1e-12 * Math.Max(1.0, Math.Abs(want)), $"戦勝点 {war.Score:G17} が −1.5×取り分＝{want:G17} でない");
    }

    [Fact]
    public void T4_計器wexitの末尾の7列は厭戦と累計の押しと書き換えの後の戦勝点()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_nx_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("wexit_s");
        Instruments.Configure(dir, "*");
        const ulong Seed = 1789UL;
        var (sim, war, fr, uk) = Board(0);
        NoContact(fr, uk);
        fr.WarExhaustion = 1.0;
        try { Instruments.Phase(sim, Seed, "open"); sim.Diplomacy.Step(sim.Coef, sim.Rng, month: 61); }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"wexit_s{Seed}.tsv"));
        try { System.IO.Directory.Delete(dir, true); } catch { }
        var h = lines[0].Split('\t');
        Assert.Equal(new[] { "exhA", "exhB", "cutA", "cutB", "cumPushA", "cumPushB", "imposeScore" }, h.Skip(h.Length - 7).ToArray());
        Assert.Equal(2, lines.Length);   // 1 戦争 × 1 か月
        var row = lines[1].Split('\t');
        Assert.Equal(h.Length, row.Length);
        double Col(string name) => double.Parse(row[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(Col("exhA") - fr.WarExhaustion) <= 1e-9, $"exhA {Col("exhA")} が仏の厭戦 {fr.WarExhaustion} でない");
        Assert.True(Math.Abs(Col("exhB") - uk.WarExhaustion) <= 1e-9, $"exhB {Col("exhB")} が英の厭戦 {uk.WarExhaustion} でない");
        Assert.True(Col("exhA") >= Col("cutA"), "崩壊の側の厭戦が切り目より下");
        Assert.Equal(0.0, Col("cumPushA")); Assert.Equal(0.0, Col("cumPushB"));
        Assert.Equal(0.0, Col("imposeScore"));
        Assert.Equal(war.Score, Col("imposeScore"));
        Assert.Equal("1", row[Array.IndexOf(h, "oldCollapseAFlag")]);
        Assert.Equal("0", row[Array.IndexOf(h, "pushSumPositiveFlag")]);
    }
}
