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

    // (1123) c109rc の形の直し 4 つの番人（事前登録 measurements/m1123_20261114_prereg_rc.txt §4）。期待値は手で置いた数から。

    [Fact]
    public void E6_配りの重みにρが無い_力は配り掛けるρ()
    {
        // 手で：重み 2×1×0.5＝1・2×1×0.5＝1・4×0.5×1＝2（和 4）→ 配り 2.5・2.5・5 → 力 2.5×0.2＝0.5・2.5×0.9＝2.25・5×0.5＝2.5
        var cand = new List<(double Y, double S, double B, double Rho)> { (2, 0, 0.5, 0.2), (2, 0, 0.5, 0.9), (4, 0.5, 0, 0.5) };
        var alloc = new double[3]; var force = new double[3];
        MultiNationSim.RulePowerAllocate(10, cand, alloc, force);
        Assert.Equal(2.5, alloc[0], 12); Assert.Equal(2.5, alloc[1], 12); Assert.Equal(5.0, alloc[2], 12);
        Assert.Equal(0.5, force[0], 12); Assert.Equal(2.25, force[1], 12); Assert.Equal(2.5, force[2], 12);
    }

    [Fact]
    public void E7_戦時でも国内の治安の分が残る_平時は前線0で国内を満たし残りが外()
    {
        Assert.Equal((4.0, 6.0), MultiNationSim.RulePowerSplitArmy(10, 0, 4));   // 平時：国内 4・外 6
        Assert.Equal((3.0, 0.0), MultiNationSim.RulePowerSplitArmy(3, 0, 4));    // 平時で足りない：全てを国内
        Assert.Equal((4.0, 2.0), MultiNationSim.RulePowerSplitArmy(10, 4, 4));   // 戦時で足りる：前線 4・国内 4・外 2
        Assert.Equal((5.0, 0.0), MultiNationSim.RulePowerSplitArmy(10, 10, 10)); // 戦時で全てが前線に縛られる：10×10÷20＝5 が国内に残る
        Assert.Equal((3.0, 0.0), MultiNationSim.RulePowerSplitArmy(6, 4, 4));    // 6×4÷8＝3
    }

    [Fact]
    public void E8_州が1つの国も旗1で寄せる()
    {
        // (1165) M1：寄せは共有層（四半期の末の清算の後）から全ての国へ＝CheckSecession は旗 1 で 4 区分を動かさない（F1 の早期 return の前の寄せは消した）
        var sim = NewSim(1.0);
        var w = sim.Nations[0].World; var c = w.Coef; c.DominanceSpeed = 0.5;
        var r = w.Regions[0];
        w.Regions.RemoveRange(1, w.Regions.Count - 1);
        Assert.Single(w.Regions);
        r.Rule = [0.6, 0.2, 0.1, 0.1]; r.ForeignRulers = ["甲"]; r.ForeignShares = [0.1];
        long stamp = w.Diplomacy is { } d ? d.RpEngStamp : long.MinValue;
        r.RpEng = new RulePowerRow { Stamp = stamp, T0 = 0.5, T1 = 0.1, T3 = 0.2 };
        r.RpEng.FKeys.Add("甲"); r.RpEng.FT.Add(0.2);
        Politics.CheckSecession(w, c, 0.0);
        Assert.Equal([0.6, 0.2, 0.1, 0.1], r.Rule);   // 旗 1 の CheckSecession は寄せない
        Politics.RulePowerApply(w, c);
        // 手で：0.55・0.15・0.15・甲 0.1＋(0.2−0.1)×0.5＝0.15（和 1.0）
        Assert.Equal(0.55, r.Rule[0], 12); Assert.Equal(0.15, r.Rule[1], 12); Assert.Equal(0.15, r.Rule[3], 12); Assert.Equal(0.15, r.Rule[2], 12);
        // 対照：旗 0 では州が 1 つの国の 4 区分は動かない
        c.RuleFromPower = 0; r.Rule = [0.6, 0.2, 0.1, 0.1];
        Politics.CheckSecession(w, c, 0.0);
        Assert.Equal([0.6, 0.2, 0.1, 0.1], r.Rule);
    }

    // (1151) c113cr の形の直しの番人（事前登録 measurements/m1151_20261115_prereg_cr.txt §3-2）。期待値は手で置いた数から。E9（ρ の比べ）は形を外したので消した。

    private static string KeyOf(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;

    [Fact]
    public void E10_交戦の相手でも陸の隣でも取り分も無い州は候補でない()
    {
        // 4 国の順の組から、宣戦の後も外への力 O＞0 で、相手の首都が陸の隣でない組を探す（組が見つからなければ赤）
        string[] names = ["フランス", "プロイセン", "イギリス", "オーストリア"];
        int found = 0;
        foreach (var a in names) foreach (var bName in names)
        {
            if (a == bName) continue;
            var sim = NewSim(1.0);
            sim.StartWarmup();
            sim.StepQuarter();
            var nW = sim.Nations.Select(s => s.World).First(x => KeyOf(x.SelfPower!) == a);
            var m = sim.Nations.Select(s => s.World).First(x => KeyOf(x.SelfPower!) == bName);
            // 標的＝相手の州のうち陸の隣でなく産出のある州（首都でない最初の州）。持ち主の側の重み b を小さく置く（候補なら重み Y×1×0.6＞0 で配りを受ける形にする）
            var winNodes = Diplomacy.CessionWinnerNodes(nW.SelfPower!);
            var target = m.Regions.Skip(1).FirstOrDefault(r => { var c = Places.Coord(r.Name); return r.OutputRefValue() > 0 && !Diplomacy.CessionAdjacent(Diplomacy.CessionNode(c.Lat, c.Lon), winNodes, out _); });
            if (target is null) continue;
            foreach (var r in m.Regions) { r.ForeignRulers = []; r.ForeignShares = []; }
            target.Rule = [0.3, 0.1, 0.0, 0.6];
            sim.Diplomacy.Wars.Add(new War { A = nW.SelfPower!, B = m.SelfPower!, Score = 0.0 });
            sim.StepQuarter();
            string nKey = KeyOf(nW.SelfPower!);
            double oN = nW.Regions[0].RpEng?.O ?? 0;
            int given = sim.Nations.SelectMany(s => s.World.Regions).Count(x => x.RpEng is { } e && e.FKeys.Contains(nKey));
            if (!(oN > 0 && given > 0)) continue;   // 対照：外へ配っている組だけを読む（候補が空で通る形ではない）
            Assert.NotNull(target.RpEng);
            Assert.DoesNotContain(nKey, target.RpEng!.FKeys);
            found++;
            if (found >= 2) return;
        }
        Assert.True(found > 0, $"宣戦の後も外へ配る組 {found}");
    }

    [Fact]
    public void E11_取り分だけの州の重みはsに比例_陸の隣の州はsを掛けない()
    {
        Assert.Equal(2.0 * 0.01, MultiNationSim.RulePowerCandY(2.0, 0.01, adjacent: false), 15);
        Assert.Equal(2.0, MultiNationSim.RulePowerCandY(2.0, 0.01, adjacent: true), 15);
        // 手で：①だけ s＝0.01・0.02（Y 1・b 0）と③ s＝0（Y 1）→ 重み 0.01×0.99＝0.0099・0.02×0.98＝0.0196・1 → 和 1.0295
        var cand = new List<(double Y, double S, double B, double Rho)>
        {
            (MultiNationSim.RulePowerCandY(1, 0.01, false), 0.01, 0, 1), (MultiNationSim.RulePowerCandY(1, 0.02, false), 0.02, 0, 1), (MultiNationSim.RulePowerCandY(1, 0, true), 0, 0, 1),
        };
        var alloc = new double[3]; var force = new double[3];
        MultiNationSim.RulePowerAllocate(1.0295, cand, alloc, force);
        Assert.Equal(0.0099, alloc[0], 12); Assert.Equal(0.0196, alloc[1], 12); Assert.Equal(1.0, alloc[2], 12);
    }

    /// <summary>手で置いた 1 州の寄せ：①だけの外国（重み Y×s×(1−s)）と③の対手（重み 1）に O を配り、力＝配り×ρ・持ち主の力 1。a＝O×ρ。</summary>
    private static List<double> E12Path(double a, double s0, int quarters)
    {
        var sim = NewSim(1.0);
        var w = sim.Nations[0].World; var c = w.Coef;
        var r = w.Regions[^1];
        r.Rule = [1 - s0, 0, s0, 0]; r.ForeignRulers = ["甲"]; r.ForeignShares = [s0];
        long stamp = w.Diplomacy is { } d ? d.RpEngStamp : long.MinValue;
        var path = new List<double> { s0 };
        for (int t = 0; t < quarters; t++)
        {
            double s = r.ForeignShares[0];
            var cand = new List<(double Y, double S, double B, double Rho)> { (MultiNationSim.RulePowerCandY(1, s, false), s, 0, 1), (1, 0, 0, 1) };
            var alloc = new double[2]; var force = new double[2];
            MultiNationSim.RulePowerAllocate(a, cand, alloc, force);
            double tot = force[0] + 1.0;
            var e = new RulePowerRow { Stamp = stamp, T0 = 1.0 / tot, T1 = 0, T3 = 0 };
            e.FKeys.Add("甲"); e.FT.Add(force[0] / tot);
            r.RpEng = e;
            Politics.RulePowerApply(w, c);
            path.Add(r.ForeignShares[0]);
        }
        return path;
    }

    [Fact]
    public void E12_取り分だけの州はkが1未満なら減り_1を超えれば不動点に留まる()
    {
        // k＝a（小さい s で t≈a×s）。a＝0.5：1 四半期の比は 1−0.025×(1−0.5×(1−s)÷(s(1−s)+1)…)＝1 未満で 0.975 以上
        var p = E12Path(0.5, 0.01, 40);
        for (int t = 1; t < p.Count; t++) { double ratio = p[t] / p[t - 1]; Assert.True(ratio < 1 && ratio >= 0.975, $"四半期 {t} の比 {ratio}"); }
        Assert.True(p[^1] < p[0] * 0.995, $"40 四半期で減る：{p[0]}→{p[^1]}");
        // a＝4：不動点は a(1−s)²＝s(1−s)＋1 の小さい根＝(9−√21)÷10（手で解いた）。小さい s から育って近くに留まる
        double sStar = (9 - Math.Sqrt(21)) / 10;
        var q = E12Path(4.0, 0.01, 600);
        Assert.True(Math.Abs(q[^1] - sStar) < 0.01, $"600 四半期の後 {q[^1]}・不動点 {sStar}");
        Assert.True(q[200] > 0.1, $"200 四半期で育つ {q[200]}");
    }

    [Fact]
    public void E13_τの代わりの値は国の並びに依らない_先に処理した国でも他の国の解けた州の中央()
    {
        var coef = new Coefficients { RuleFromPower = 1.0 };
        var sim = new MultiNationSim([NationConfig.Austria(), NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain()], Seed, _ => new UtilityAgent(), coef);
        foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.RuleFromPower = 1.0; }
        sim.StartWarmup();
        var au = sim.Nations[0].World;
        Assert.Equal("オーストリア", KeyOf(au.SelfPower!));
        foreach (var r in au.Regions) r.RuleInit[0] = 0;   // オーストリアの州は段 ① で解けない＝段 ② の代わりの値
        sim.StepQuarter();
        var others = sim.Nations.Skip(1).SelectMany(s => s.World.Regions).Select(r => r.RuleTau).Where(double.IsFinite).ToList();
        Assert.True(others.Count > 20, $"他の国の解けた州 {others.Count}");
        var tauAu = au.Regions.Select(r => r.RuleTau).ToList();
        Assert.All(tauAu, t => Assert.Equal(tauAu[0], t));
        Assert.True(tauAu[0] > 0 && tauAu[0] >= others.Min() && tauAu[0] <= others.Max(), $"オーストリアの τ {tauAu[0]}・他の国 {others.Min()}〜{others.Max()}");
    }

    [Fact]
    public void E14_旗1の最初の表で開幕のデータの州に持ち主の名が付く_名の無い州は在地へ()
    {
        foreach (double flag in new[] { 1.0, 0.0 })
        {
            var coef = new Coefficients { RuleFromPower = flag };
            var sim = new MultiNationSim([WorldData.ByName("ダホメ王国"), WorldData.ByName("ポルトガル"), WorldData.ByName("オスマン"), NationConfig.Britain()], Seed, _ => new UtilityAgent(), coef);
            foreach (var n in sim.Nations) { n.World.Coef.CivilWarLandslideRatio = 0; n.World.Coef.RuleFromPower = flag; }
            sim.StartWarmup();
            sim.StepQuarter();
            var regs = sim.Nations.SelectMany(s => s.World.Regions).ToList();
            var ouidah = regs.Single(r => r.Name == "ウィダー"); var massawa = regs.Single(r => r.Name == "マサワ");
            int k = System.Array.IndexOf(ouidah.ForeignRulers, "ポルトガル");
            if (flag > 0)
            {
                Assert.True(k >= 0 && ouidah.ForeignShares[k] > 0, $"ウィダーの外国の名 {string.Join(",", ouidah.ForeignRulers)}");
                Assert.Equal(0.0, massawa.Rule[2]); Assert.Equal(0.0, massawa.RuleInit[2]);
                Assert.Equal(0.25 + 0.65, massawa.RuleInit[1], 12);
            }
            else
            {
                Assert.True(k < 0, "対照：旗 0 は名を付けない");
                Assert.True(massawa.RuleInit[2] > 0, "対照：旗 0 はデータを動かさない");
            }
        }
    }
}
