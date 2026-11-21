using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(959) 線 c85mw：戦時の軍需の倍率は接触の割合から導く（旗 MilitaryWarMultFromContact は 1 に焼いて消した＝(1281)）機構の番人と計器 cwmult_s の末尾 5 列の番人。
/// 事前登録 measurements/m959_20261108_prereg_mw.txt §5。期待値は式の外から書く：相手の押しが弱い盤面では接触＝相手の陸軍力×本土防衛の地の利（抽象標的は投射しない）
/// ＝e＝相手の Military×HomeDefenseBonus÷自軍の Military（エンジンの Engagement を呼ばない）。盤面は試験が置いた Power の Military だけで決まる（軌道に依らない）。</summary>
[Collection("Instruments")]
public class MilitaryWarMultContactTests
{
    private sealed class Board
    {
        public required Simulation Sim; public required World W; public required Power Me; public required Power Opp; public required Diplomacy Dip;
        public double Month() { for (int d = 0; d < Calendar.DaysPerMonth; d++) Sim.StepDay(); return W.Region.MilitaryWarMultLast; }
    }

    private static Board Make(bool civil, double myMil, double oppMil, double exhaustion = 0)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        w.StepsDiplomacy = false;                        // Military と厭戦を置いた値のまま（外交の月の歩みを止める）
        var dip = new Diplomacy();
        var me = new Power { Name = "政府", Nation = w, Military = myMil, WarExhaustion = exhaustion };
        var opp = new Power { Name = "相手", Military = oppMil };
        w.Diplomacy = dip; w.SelfPower = me;
        dip.Wars.Add(new War { A = me, B = opp, CivilWarParent = civil ? me : null });
        Assert.Equal(civil, w.InCivilWar);
        return new Board { Sim = sim, W = w, Me = me, Opp = opp, Dip = dip };
    }

    /// <summary>相手が弱い盤面の e（式の外から）＝相手の陸軍力×本土防衛の地の利÷自軍の Military。前提＝自軍の押しの方が強い（min が相手の側）。</summary>
    private static double WeakOppShare(Board b)
    {
        var c = b.W.Coef;
        Assert.True(b.Dip.FrontPush(b.Me, b.Opp, c) > b.Dip.FrontPush(b.Opp, b.Me, c), "前提：相手の押しの方が弱い盤面");
        return b.Opp.Military * c.HomeDefenseBonus / b.Me.Military;
    }

    /// <summary>T1：対外戦＝1＋κ×e（κ＝WarArmsDemandPerContact）・9 倍の式でも 1 でもない。</summary>
    [Fact]
    public void 対外戦の倍率は1足すκ掛ける接触の割合()
    {
        var b = Make(civil: false, myMil: 1.0, oppMil: 0.1, exhaustion: 0.6);
        double e = WeakOppShare(b);
        double x = b.Month();
        double want = 1.0 + b.W.Coef.WarArmsDemandPerContact * e;
        Assert.True(e > 0.05, $"前提：接触の割合 {e:G17}");
        Assert.True(Math.Abs(x - want) <= 1e-9 * want, $"戦時の倍率 {x:G17}（期待 {want:G17}）");
        Assert.True(x > 1.0 + 1e-6, $"κ が効いていない {x:G17}");
    }

    /// <summary>T2：接触 0（相手の陸軍 0）＝1（平時と同じ）。</summary>
    [Fact]
    public void 接触が0の戦争は平時の倍率になる()
    {
        double on = Make(civil: false, myMil: 1.0, oppMil: 0.0).Month();
        Assert.Equal(1.0, on);
    }

    /// <summary>T3：遅れ 0（新しい状態 0）＝盤面を替えた次の月に新しい接触を読む・戦争が終われば次の月に 1。</summary>
    [Fact]
    public void 接触は毎月その場の盤面から求め直す()
    {
        var b = Make(civil: false, myMil: 1.0, oppMil: 0.1);
        double x1 = b.Month();
        b.Opp.Military = 0.04;
        double e2 = WeakOppShare(b);
        double x2 = b.Month();
        double want2 = 1.0 + b.W.Coef.WarArmsDemandPerContact * e2;
        Assert.True(Math.Abs(x2 - want2) <= 1e-9 * want2, $"盤面を替えた次の月の倍率 {x2:G17}（期待 {want2:G17}・前の月 {x1:G17}）");
        Assert.True(Math.Abs(x2 - x1) > 1e-6, "前の月の値のまま＝遅れている");
        b.Dip.Wars.Clear();
        Assert.Equal(1.0, b.Month());
    }

    /// <summary>T4：内戦も対外戦と同じ式（内戦だけの増しも 2.0 の枝も無い）。</summary>
    [Fact]
    public void 内戦は対外戦と同じ式()
    {
        var bc = Make(civil: true, myMil: 1.0, oppMil: 0.1, exhaustion: 0.6);
        double e = WeakOppShare(bc);
        double xc = bc.Month();
        double xf = Make(civil: false, myMil: 1.0, oppMil: 0.1, exhaustion: 0.6).Month();
        double want = 1.0 + bc.W.Coef.WarArmsDemandPerContact * e;
        Assert.True(Math.Abs(xc - want) <= 1e-9 * want, $"内戦の倍率 {xc:G17}（期待 {want:G17}）");
        Assert.Equal(xf, xc);
    }

    /// <summary>T5：比は削らない（思想⑧）＝熟練で押しが自軍の Military を超える盤面では e＞1 のまま 1＋κ×e。</summary>
    [Fact]
    public void 接触の割合は1で削らない()
    {
        var b = Make(civil: false, myMil: 1.0, oppMil: 0.1);
        b.Me.ArmySkillMul = 4.0;             // 自軍の押し＝1×4×投射（首都から首都＝1 に近い）
        b.Opp.ArmySkillMul = 4.0;
        b.Opp.Military = 0.5;                // 相手の押し 0.5×4×1.25＝2.5＜自軍の押し＝相手が弱い側のまま e＝2.5
        var c = b.W.Coef;
        Assert.True(b.Dip.FrontPush(b.Me, b.Opp, c) > b.Dip.FrontPush(b.Opp, b.Me, c), "前提：相手の押しの方が弱い盤面");
        double e = b.Opp.Military * b.Opp.ArmySkillMul * c.HomeDefenseBonus / b.Me.Military;
        Assert.True(e > 1.5, $"前提：接触の割合 {e:G17} が 1 を超える");
        double x = b.Month();
        double want = 1.0 + c.WarArmsDemandPerContact * e;
        Assert.True(Math.Abs(x - want) <= 1e-9 * want, $"戦時の倍率 {x:G17}（期待 {want:G17}・削れば {1.0 + c.WarArmsDemandPerContact:G17}）");
    }

    private static double D(Dictionary<string, string> r, string k) => double.Parse(r[k], System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>T6 で試験が置く κ（(973)：既定の字 42 を期待に使わない＝(959) の反証役・World の κ から期待を作らない）。</summary>
    private const double Kappa6 = 7.0;

    /// <summary>T6：計器 cwmult_s の末尾 5 列＝同じ盤面で接触の割合の規則を数える（旗を読まない）。旗 1＝ずれ 0・旗 0＝交戦の月の全てがずれ・接触の和は旗に依らない。</summary>
    [Fact]
    public void 計器は同じ盤面で接触の割合の規則を数える()
    {
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cwmc_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("スペイン")], seed: 1791UL) { DiplomacyFrozen = true };
            try
            {
                var fr = sim.Nations[0].World; var es = sim.Nations[1].World;
                foreach (var n in sim.Nations) { n.World.Coef.WarArmsDemandPerContact = Kappa6; }
                sim.Diplomacy.Wars.Add(new War { A = fr.SelfPower!, B = es.SelfPower! });
                sim.StepQuarter(); Instruments.Quarter(sim, 1791UL, 0);
            }
            finally { Instruments.Close(sim); Instruments.Configure(null, null); }
            var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "cwmult_s1791.tsv"), Instruments.CwHeaderText);
            Assert.Equal(2, rows.Count);   // 2 国×1 四半期（どちらも全ての月が対外戦）
            foreach (var r in rows)
            {
                int months = int.Parse(r["foreignWarMonthsCount"]);
                Assert.Equal(3, months);
                double ct = D(r, "contactShareSumIndex");
                Assert.True(ct > 0, $"{r["nation"]} 接触の割合の和が 0（前提：陸続きの戦線）");
                Assert.Equal("0", r["zeroContactWarMonthsCount"]);
                Assert.Equal("0", r["contactRuleMismatchMonthsCount"]);
                double rule = D(r, "warMultContactRuleSumIndex");
                Assert.True(Math.Abs(rule - (months + Kappa6 * ct)) <= 1e-9 * rule, $"{r["nation"]} 規則の和 {rule:G17}（期待 {months + Kappa6 * ct:G17}）");
                Assert.True(Math.Abs(D(r, "warMultActualForeignSumIndex") - rule) <= 1e-9 * rule, $"{r["nation"]} 実際の和 {r["warMultActualForeignSumIndex"]}（期待 {rule:G17}）");
            }
        }
    }
}
