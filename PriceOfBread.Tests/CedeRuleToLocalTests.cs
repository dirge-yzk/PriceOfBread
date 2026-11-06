using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(902) c77ce 割譲の支配度 C1＋H-a（旗 <see cref="Coefficients.CedeRuleToLocal"/>・事前登録 measurements/m902_20261106_prereg_ce.txt）の機構の番人。
/// C1＝割譲（<see cref="Territory.Rehoming.Ceded"/>）の 1 回だけ Rule[1] += Rule[0]; Rule[0] = 0。H-a＝受け手の鍵（BaseName・空なら Name）の外国持分を 0。
/// RuleInit・Rule[2]・Rule[3]・Dominance・第三国の持分・配列の長さは触らない。建国（Seceded）・再併合（Returned）・独立の確定（Sovereign）は動かさない。
/// 期待値は試験が移す前に写した値とリテラルから作る（エンジンの関数を呼ばない）。旗はエンジンが読む送り手の Coef（World.Coef は国ごと別のインスタンス）に置く＝両国に置く。
/// 突然変異（使い捨ての木・結果は measurements/m902_20261106/mut902.txt）は事前登録 [5] の表。</summary>
public class CedeRuleToLocalTests
{
    static (MultiNationSim Sim, World Uk, World Fr) Setup(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.CedeRuleToLocal = flag;
        return (sim, sim.Nations[0].World, sim.Nations[1].World);
    }

    static Region Pick(World w, int nth) => w.Regions.Where(x => x.Id != 0 && x.Tier == RegionTier.Core).Skip(nth).First();

    static long B(double v) => BitConverter.DoubleToInt64Bits(v);

    static void SameBits(double[] want, double[] got)
    {
        Assert.Equal(want.Length, got.Length);
        for (int k = 0; k < want.Length; k++) Assert.Equal(B(want[k]), B(got[k]));
    }

    [Fact]
    public void T1_旗0の割譲は支配の区分と外国持分を触らない()
    {
        var (_, uk, fr) = Setup(0);
        var r = Pick(uk, 0);
        r.Rule = [0.55, 0.25, 0.05, 0.15]; r.RuleInit = [0.6, 0.4, 0, 0]; r.Dominance = 0.7;
        r.ForeignRulers = ["フランス", "スペイン"]; r.ForeignShares = [0.2, 0.05];
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Contains(r, fr.Regions);
        SameBits([0.55, 0.25, 0.05, 0.15], r.Rule);
        SameBits([0.6, 0.4, 0, 0], r.RuleInit);
        Assert.Equal(B(0.7), B(r.Dominance));
        SameBits([0.2, 0.05], r.ForeignShares);
        Assert.Equal(new[] { "フランス", "スペイン" }, r.ForeignRulers);
    }

    [Fact]
    public void T2_旗1の割譲は国家の分を在地へ移し受け手の持分だけ0()
    {
        var (_, uk, fr) = Setup(1);
        var r = Pick(uk, 0);
        r.Rule = [0.55, 0.25, 0.05, 0.15]; r.RuleInit = [0.6, 0.4, 0, 0]; r.Dominance = 0.7;
        r.ForeignRulers = ["フランス", "スペイン"]; r.ForeignShares = [0.2, 0.05];
        Territory.TransferRegion(uk, fr, r, Territory.Rehoming.Ceded);
        Assert.Contains(r, fr.Regions);
        Assert.Equal(B(0.0), B(r.Rule[0]));
        Assert.Equal(B(0.25 + 0.55), B(r.Rule[1]));
        Assert.Equal(B(0.05), B(r.Rule[2]));
        Assert.Equal(B(0.15), B(r.Rule[3]));
        Assert.True(Math.Abs(r.Rule.Sum() - 1.0) <= 1e-12, $"4 区分の和 {r.Rule.Sum():R}");
        SameBits([0.6, 0.4, 0, 0], r.RuleInit);
        Assert.Equal(B(0.7), B(r.Dominance));
        SameBits([0.0, 0.05], r.ForeignShares);
        Assert.Equal(new[] { "フランス", "スペイン" }, r.ForeignRulers);
    }

    [Fact]
    public void T3_受け手の鍵は名を替えた国でもBaseNameで_BaseNameが空ならName()
    {
        var (_, uk, fr) = Setup(1);
        var fp = fr.SelfPower!;
        fp.Name = "フランス帝国";   // BaseName は「フランス」のまま＝鍵は BaseName
        var r1 = Pick(uk, 0);
        r1.ForeignRulers = ["フランス帝国", "フランス", "スペイン"]; r1.ForeignShares = [0.1, 0.2, 0.05];
        Territory.TransferRegion(uk, fr, r1, Territory.Rehoming.Ceded);
        SameBits([0.1, 0.0, 0.05], r1.ForeignShares);
        fp.BaseName = "";   // BaseName が空＝鍵は Name
        var r2 = Pick(uk, 0);
        r2.ForeignRulers = ["フランス帝国", "フランス", "スペイン"]; r2.ForeignShares = [0.3, 0.2, 0.07];
        Territory.TransferRegion(uk, fr, r2, Territory.Rehoming.Ceded);
        SameBits([0.0, 0.2, 0.07], r2.ForeignShares);
        Assert.Equal(3, r2.ForeignRulers.Length);
    }

    [Fact]
    public void T4_旗1でも建国と再併合と独立の確定は支配の区分と外国持分を触らない()
    {
        var (_, uk, fr) = Setup(1);
        double[] rule = [0.55, 0.25, 0.05, 0.15];
        var rs = Pick(uk, 0);
        rs.Rule = (double[])rule.Clone(); rs.ForeignRulers = ["フランス"]; rs.ForeignShares = [0.2];
        Territory.TransferRegion(uk, fr, rs, Territory.Rehoming.Seceded);
        SameBits(rule, rs.Rule); SameBits([0.2], rs.ForeignShares);
        var rr = Pick(uk, 0);
        rr.Rule = (double[])rule.Clone(); rr.ForeignRulers = ["フランス"]; rr.ForeignShares = [0.2];
        Territory.TransferRegion(uk, fr, rr, Territory.Rehoming.Returned);
        SameBits(rule, rr.Rule); SameBits([0.2], rr.ForeignShares);
        var rv = Pick(uk, 0);
        rv.Rule = (double[])rule.Clone(); rv.ForeignRulers = ["イギリス"]; rv.ForeignShares = [0.2];
        Territory.Rehome(rv, Territory.Rehoming.Sovereign);   // 独立の確定（Diplomacy の直の呼び）と同じ関数
        SameBits(rule, rv.Rule); SameBits([0.2], rv.ForeignShares);
    }

    static MultiNationSim Board(double flag)
    {
        var sim = new MultiNationSim(Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.CedeRuleToLocal = flag;
        foreach (var (p, nav) in new[] { (sim.Nations[0].World.SelfPower!, 100.0), (sim.Nations[1].World.SelfPower!, 10.0), (sim.Nations[2].World.SelfPower!, 50.0) })
        { p._fleetDriven = false; p.Military = 1000; p.NavalShare = new Share(nav / 1000.0); p.NavySkillMul = 1.0; }
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is null) p.Military = 0;
        return sim;
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void T5_講和の境界改定の割譲で国家の分が在地へ_旗0は動かない(double flag)
    {
        var sim = Board(flag);
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World;
        var before = uk.Regions.ToDictionary(r => r, r => (double[])r.Rule.Clone());
        sim.Diplomacy.ResolvePeace(sim.Coef, new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = 1.3 });   // 1.2 以上 1.4 未満＝決定的勝利＝境界改定（CessionInstrumentTests と同じ盤面）
        var moved = fr.Regions.Where(before.ContainsKey).ToList();
        Assert.True(moved.Count > 0, "前提が崩れた：講和で州が動いていない");
        Assert.Contains(moved, r => before[r][0] > 0.01);   // 前の国家の分のある州が動いた（空振りでない）
        foreach (var r in moved)
        {
            var b = before[r];
            if (flag > 0)
            {
                Assert.Equal(B(0.0), B(r.Rule[0]));
                Assert.Equal(B(b[1] + b[0]), B(r.Rule[1]));
                Assert.Equal(B(b[2]), B(r.Rule[2]));
                Assert.Equal(B(b[3]), B(r.Rule[3]));
            }
            else SameBits(b, r.Rule);
        }
    }
}
