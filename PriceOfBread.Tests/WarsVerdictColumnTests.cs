using System.IO;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1330) 線 c139sl：wars_s の末尾 2 列（verdictCode・rebelTreasuryEnd）の番人。盤面は試験の中で置く。
/// 期待は計器でなく盤面から：判定は EndedWars の記録（エンジンの終戦の 1 本が書いた物）と、手で置いた戦勝点から読む。国庫は終戦の直前に反乱国の World から読む。
/// 突然変異（使い捨ての木）：M1 判定を A 勝ち（1）に固定 → T1 赤。M2 反乱国を常に B で読む → T1 赤（反乱国が A の戦争を置く）。M3 国庫を nan 固定 → T1 赤。
/// 旗・係数・状態は無い＝Instruments.On のときだけ終戦の 1 本が積む。</summary>
[Collection("Instruments")]
public class WarsVerdictColumnTests
{
    private const ulong Seed = 1789UL;

    private static (MultiNationSim sim, Power a, Power b, Power c3) Board()
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン", "ロシア" }, OpeningFixture.Named), seed: (int)Seed, coef: new Coefficients()) { DiplomacyFrozen = true };
        var d = sim.Diplomacy;
        foreach (var p in d.Powers.ToList()) foreach (var q in d.Powers.ToList()) if (p != q) d.RemoveDependency(p, q);
        return (sim, sim.Nations[0].World.SelfPower!, sim.Nations[1].World.SelfPower!, sim.Nations[2].World.SelfPower!);
    }

    private static void End(Diplomacy d, War war, Coefficients c)
        => typeof(Diplomacy).GetMethod("EndWar", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(d, [war, d.Wars.IndexOf(war), c]);

    private static string G10(double v) => v.ToString("G10", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>T1：内戦 2 本（反乱国が B の戦争と A の戦争）と外国戦 1 本と終わらない戦争 1 本を置き、終戦の 1 本を通す。
    /// 列＝判定（B 勝ち 2・A 勝ち 1・白紙 0）・反乱国の国庫（反乱国が A でも B でも）・外国戦は nan・終わらない戦争は -1 と nan。</summary>
    [Fact]
    public void T1_終戦の判定と反乱国の国庫が列に出る()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_wv_" + System.Guid.NewGuid().ToString("N"));
        bool saved = Instruments.On;
        Instruments.Configure(dir, null);
        var (sim, a, b, c3) = Board();
        var d = sim.Diplomacy; var c = sim.Coef;
        string tr1 = "", tr2 = "";
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 0);
            // 内戦 1：親 a・反乱 b（反乱国は B）
            d.DeclareCivilWar(a, b, a.Nation!.Polity == Polity.Republic ? Polity.Absolutist : Polity.Republic, c);
            var w1 = d.Wars.Single(w => w.CivilWarParent == a);
            Assert.Same(b, w1.B);
            // 内戦 2：親 c3・反乱 b を A に置く（反乱国が A）・外国戦・終わらせない戦争
            var w2 = new War { A = b, B = c3, CivilWarParent = c3 };
            var w3 = new War { A = a, B = c3 };
            var w4 = new War { A = b, B = c3 };
            foreach (var w in new[] { w2, w3, w4 }) { d.Wars.Add(w); Instruments.WarStarted(d, w, 1783); }
            w1.Score = -0.9;   // B（反乱国）の勝ち
            w2.Score = 0.4;    // A（反乱国）の勝ち
            w3.Score = 0.0;    // 白紙
            double t1 = b.Nation!.Treasury.Value;
            Assert.True(double.IsFinite(t1), "前提が崩れた：反乱国の国庫が有限でない");
            tr1 = G10(t1);
            End(d, w1, c);
            Assert.Equal(WarVerdict.BWon, d.EndedWars[^1].Verdict);   // 手で置いた戦勝点から
            End(d, w3, c);
            Assert.Equal(WarVerdict.Blank, d.EndedWars[^1].Verdict);
            tr2 = G10(b.Nation!.Treasury.Value);
            End(d, w2, c);
            Assert.Equal(WarVerdict.AWon, d.EndedWars[^1].Verdict);
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 1);
        }
        finally { Instruments.Close(sim); Instruments.On = saved; }
        var tsv = File.ReadAllLines(Path.Combine(dir, "wars_s1789.tsv"));
        var cols = tsv[0].Split('	');
        Assert.Equal("entryCode", cols[^3]); Assert.Equal("verdictCode", cols[^2]); Assert.Equal("rebelTreasuryEnd", cols[^1]);
        var rows = tsv.Skip(1).Select(l => l.Split('	')).ToList();
        Assert.Equal(4, rows.Count);   // 開始順：w1（内戦・反乱 B）・w2（内戦・反乱 A）・w3（外国戦）・w4（終わらない）
        Assert.Equal("2", rows[0][^2]); Assert.Equal(tr1, rows[0][^1]);
        Assert.Equal("1", rows[1][^2]); Assert.Equal(tr2, rows[1][^1]);
        Assert.Equal("0", rows[2][^2]); Assert.Equal("nan", rows[2][^1]);
        Assert.Equal("-1", rows[3][^2]); Assert.Equal("nan", rows[3][^1]);
    }
}
