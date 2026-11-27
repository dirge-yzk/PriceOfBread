using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1415) 線 c151lk：戦争の運 <see cref="Coefficients.WarLuck"/> の既定を 0 にした形の機構の試験（作者 2026-11-26 の決まり〈乱数は気候と疫病だけ〉・
/// wl の案 a の段 1）。運が無い月の戦勝点の動きは押しの割合だけ＝WarPushSpeed×(押しA−押しB)÷(押しA＋押しB)。
/// 盤面は試験の中で置く（仏英の 2 国・助走なし・外交の AI は止める・塹壕と上陸の門は外す）。運の係数は**試験で書かない**（既定を読む）。
/// 期待値の 0.10 は Coefficients を呼ばないリテラル（既定の固定は CoefficientRelationTests が別に持つ）。押しはエンジンが War.PushA・PushB に積んだ月の増分を読む。
/// 突然変異：既定 0.12 に戻す（M1）・0.06（M2）→ 3 本とも赤。押しの項を消す（M3）→ 割合の式と片側の押しの 2 本が赤。</summary>
[Collection("Instruments")]
public class WarLuckZeroTests
{
    private const double PushSpeed = 0.10;   // WarPushSpeed の既定（Coefficients を呼ばない出所）

    private static (MultiNationSim sim, War war, Power fr, Power uk) Board()
    {
        var sim = new MultiNationSim(Array.ConvertAll(new[] { "フランス", "イギリス" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        c.TrenchDefenseWeight = 0; c.SeaInvasionControlMin = 0;   // 押しを月の生の値のまま読む・どちらも上陸できる
        var fr = sim.Nations[0].World.SelfPower!; var uk = sim.Nations[1].World.SelfPower!;
        fr.WarExhaustion = 0; uk.WarExhaustion = 0;
        var war = new War { A = fr, B = uk, Score = 0 };
        sim.Diplomacy.Wars.Add(war);
        return (sim, war, fr, uk);
    }

    /// <summary>1 か月進め、その月の押しの増分（A・B）と戦勝点の差を返す。戦争が講和で消えたら前提の崩れ。</summary>
    private static (double a, double b, double d) Month(MultiNationSim sim, War war, int month)
    {
        double a0 = war.PushA, b0 = war.PushB, s0 = war.Score;
        sim.Diplomacy.Step(sim.Coef, sim.Rng, month: month);
        Assert.True(sim.Diplomacy.Wars.Count == 1 && sim.Diplomacy.Wars[0] == war, $"前提が崩れた：月 {month} で戦争が終わった（戦勝点 {war.Score:G6}）");
        return (war.PushA - a0, war.PushB - b0, war.Score - s0);
    }

    [Fact]
    public void T1_押しが等しい月は戦勝点が動かない()
    {
        var (cs, cw, _, _) = Board();
        var (ca, cb, _) = Month(cs, cw, 61);
        Assert.True(ca > 0 && cb > 0, $"前提が崩れた：押しが立っていない（{ca:G6}/{cb:G6}）");
        // 同じ盤面をもう一度置き、強い側の即応度を押しの比で下げる（戦力は即応度に比例・押しは戦力に比例）＝その月の押しが等しい。
        var (sim, war, fr, uk) = Board();
        if (ca > cb) fr.Nation!.MilitaryReadiness *= cb / ca; else uk.Nation!.MilitaryReadiness *= ca / cb;
        var (a, b, d) = Month(sim, war, 61);
        Assert.True(a > 0 && Math.Abs(a - b) <= 1e-9 * (a + b), $"前提が崩れた：押しが等しくない（{a:G17}/{b:G17}）");
        Assert.True(Math.Abs(d) <= 1e-9, $"押しが等しい月に戦勝点が {d:G17} 動いた＝押しの他の項（運）が残っている");
    }

    [Fact]
    public void T1b_毎月の戦勝点の動きは押しの割合の式だけ()
    {
        var (sim, war, _, _) = Board();
        for (int k = 0; k < 6; k++)
        {
            var (a, b, d) = Month(sim, war, 61 + k);
            Assert.True(a + b > 0, $"前提が崩れた：月 {k} の押しの和が 0");
            double want = PushSpeed * (a - b) / (a + b);
            Assert.True(Math.Abs(d - want) <= 1e-12, $"月 {k}：戦勝点の動き {d:G17} が 0.10×(押しA−押しB)÷(押しA＋押しB)＝{want:G17} でない（差 {d - want:G6}）");
        }
    }

    [Fact]
    public void T2_片側だけが押す戦争は毎月0_10ずつ進む()
    {
        var (sim, war, _, uk) = Board();
        uk.Nation!.MilitaryReadiness = 0;   // 英の戦力 0＝英の押し 0（仏だけが押す＝割合 1）
        const int N = 9;                     // 9 か月で 0.9＝決定的の線 1.0 の手前（戦争は続く）
        for (int k = 0; k < N; k++)
        {
            var (a, b, _) = Month(sim, war, 61 + k);
            Assert.True(a > 0 && b == 0, $"前提が崩れた：月 {k} の押しが片側だけでない（{a:G6}/{b:G6}）");
        }
        Assert.True(Math.Abs(war.Score - PushSpeed * N) <= 1e-12, $"{N} か月の後の戦勝点 {war.Score:G17} が 0.10×{N}＝{PushSpeed * N:G17} でない");
    }
}
