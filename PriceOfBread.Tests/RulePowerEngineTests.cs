using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1117) 線 c108rc：支配の力の段 C′（旗 RuleFromPower・RulePowerEngine.cs）の機構の番人。事前登録 measurements/m1117_20261114_prereg_rc.txt §4。
/// E1＝同点の許容差（相対 1e-9・同点は持ち主の側から順）・E2＝寄せ（表の目標へ DominanceSpeed・4 区分の和が 1・Rule[2]＝Σ外国の取り分・新しい外国は末尾に足す・表の外の外国は 0 へ）・
/// E3＝旗 1 の多国の走り：τ は 1 回だけ解いて動かない・q_t は四半期ごとに変わる・全ての州の 4 区分の和が 1・Rule[2]＝Σx_f・セーブの往復で τ が残る・
/// E4＝旗 0 では表を作らず τ を解かない・E5＝旗 1 では TreatyPortLapse が外国の取り分を動かさない（対照＝旗 0 では減る）。
/// 期待値は試験の中で手で置いた数から作る（エンジンの関数で期待を作らない）。
/// </summary>
public class RulePowerEngineTests
{
    private const ulong Seed = 1789UL;

    [Fact]
    public void E1_同点の許容差は相対1e9で持ち主の側から順()
    {
        Assert.Equal(0, MultiNationSim.RulePowerBestClass(0.5, 0.5 * (1 + 5e-10), 0.0, 0.0));
        Assert.Equal(1, MultiNationSim.RulePowerBestClass(0.5, 0.5 * (1 + 2e-9), 0.0, 0.0));
        Assert.Equal(0, MultiNationSim.RulePowerBestClass(0.25, 0.25, 0.25, 0.25));
        Assert.Equal(3, MultiNationSim.RulePowerBestClass(0.2, 0.1, 0.3, 0.4));
        Assert.Equal(2, MultiNationSim.RulePowerBestClass(0.2, 0.1, 0.4, 0.4 * (1 + 5e-10)));
    }

    private static MultiNationSim NewSim(double flag)
    {
        var coef = new Coefficients { RuleFromPower = flag };
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), coef);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.RuleFromPower = flag; }
        return sim;
    }

    [Fact]
    public void E2_寄せは表の目標へ_和は1_Rule2は外国の和_新しい外国は末尾()
    {
        var sim = NewSim(1.0);
        var w = sim.Nations[0].World; var c = w.Coef; c.DominanceSpeed = 0.5;
        var r = w.Regions[^1];
        r.Rule = [0.6, 0.2, 0.1, 0.1];
        r.ForeignRulers = ["甲"]; r.ForeignShares = [0.1];
        long stamp = w.Diplomacy is { } d ? d.RpEngStamp : long.MinValue;
        var e = new RulePowerRow { Stamp = stamp, T0 = 0.5, T1 = 0.1, T3 = 0.2 };
        e.FKeys.Add("乙"); e.FT.Add(0.2);
        r.RpEng = e;
        Politics.RulePowerApply(w, c);
        // 手で：v0＝0.6＋(0.5−0.6)×0.5＝0.55・v1＝0.2＋(0.1−0.2)×0.5＝0.15・v3＝0.1＋(0.2−0.1)×0.5＝0.15・甲＝0.1＋(0−0.1)×0.5＝0.05・乙＝0＋(0.2−0)×0.5＝0.1・和＝1.0
        Assert.Equal(["甲", "乙"], r.ForeignRulers);
        Assert.Equal(0.55, r.Rule[0], 12); Assert.Equal(0.15, r.Rule[1], 12); Assert.Equal(0.15, r.Rule[3], 12);
        Assert.Equal(0.05, r.ForeignShares[0], 12); Assert.Equal(0.1, r.ForeignShares[1], 12);
        Assert.Equal(0.15, r.Rule[2], 12);
        Assert.Equal(1.0, r.Rule[0] + r.Rule[1] + r.Rule[2] + r.Rule[3], 12);
        // 表の印が違う州は動かさない（対照）
        var r2 = w.Regions[^2]; r2.Rule = [0.7, 0.1, 0.1, 0.1]; r2.RpEng = new RulePowerRow { Stamp = stamp - 1, T0 = 0.1, T1 = 0.1, T3 = 0.8 };
        Politics.RulePowerApply(w, c);
        Assert.Equal([0.7, 0.1, 0.1, 0.1], r2.Rule);
    }

    [Fact]
    public void E3_旗1の走り_tauは1回だけ_qは四半期ごと_和は1_セーブで残る()
    {
        var sim = NewSim(1.0);
        sim.StartWarmup();
        sim.StepQuarter();
        var regs = sim.Nations.SelectMany(n => n.World.Regions).ToList();
        var tau1 = regs.Select(r => r.RuleTau).ToArray();
        int solved = tau1.Count(t => double.IsFinite(t));
        Assert.True(solved >= regs.Count * 9 / 10, $"τ を解いた州 {solved}/{regs.Count}");
        double q1 = regs.First(r => r.RpEng is not null).RpEng!.Q;
        sim.StepQuarter();
        double q2 = regs.First(r => r.RpEng is not null).RpEng!.Q;
        Assert.NotEqual(q1, q2);
        for (int i = 0; i < regs.Count; i++) if (double.IsFinite(tau1[i])) Assert.Equal(tau1[i], regs[i].RuleTau);
        int checkedRows = 0, moved = 0;
        foreach (var r in regs)
        {
            if (r.Rule.Length != 4) continue;
            checkedRows++;
            if (r.RpEng is not { } e || double.IsNaN(e.T0)) continue;
            double fx = 0; foreach (var x in r.ForeignShares) fx += x;
            Assert.Equal(1.0, r.Rule[0] + r.Rule[1] + r.Rule[2] + r.Rule[3], 9);
            Assert.Equal(fx, r.Rule[2], 9);
            moved++;
        }
        Assert.True(checkedRows > 20 && moved > 20, $"調べた州 {checkedRows}・目標のある州 {moved}");
        using var ms = new System.IO.MemoryStream();
        SaveGame.Save(sim, ms); ms.Position = 0;
        var sim2 = SaveGame.Load(ms);
        var regs2 = sim2.Nations.SelectMany(n => n.World.Regions).ToList();
        Assert.Equal(regs.Count, regs2.Count);
        for (int i = 0; i < regs.Count; i++) { Assert.Equal(regs[i].RuleTau, regs2[i].RuleTau); Assert.Equal(regs[i].RuleTauInter, regs2[i].RuleTauInter); }
    }

    [Fact]
    public void E4_旗0では表を作らずtauを解かない()
    {
        var sim = NewSim(0.0);
        sim.StartWarmup();
        sim.StepQuarter(); sim.StepQuarter();
        var regs = sim.Nations.SelectMany(n => n.World.Regions).ToList();
        Assert.True(regs.Count > 20);
        Assert.All(regs, r => { Assert.Null(r.RpEng); Assert.True(double.IsNaN(r.RuleTau)); });
        Assert.Equal(0, sim.Diplomacy.RpEngStamp);
    }

    [Fact]
    public void E5_旗1ではTreatyPortLapseが外国の取り分を動かさない_旗0では減る()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var sim = NewSim(flag);
            sim.StartWarmup();
            var regs = sim.Nations.SelectMany(n => n.World.Regions).ToList();
            foreach (var r in regs) { r.ForeignRulers = ["甲"]; r.ForeignShares = [0.3]; }
            var c = new Coefficients { RuleFromPower = flag };
            sim.Diplomacy.TreatyPortLapse(c, 0);
            int changed = regs.Count(r => r.ForeignShares[0] != 0.3);
            if (flag > 0) Assert.Equal(0, changed);
            else Assert.True(changed > 0, "対照：旗 0 では風化で減る州がある");
        }
    }
}
