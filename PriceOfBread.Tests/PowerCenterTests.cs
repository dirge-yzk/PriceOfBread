using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1090) 線 c104cp：王権と議会の段 P1（中心の影の計器 center_s・純関数 PowerCenter）の番人。事前登録 measurements/m1090_20261113_prereg_cp.txt。
/// 期待値は紙の式から試験の中で作る（エンジンや計器の関数を呼ばない・★(1035)）。
/// T1 兵の分かれの和が 1・倒し先／T2 力と取り分（ΣF＝G＋q×M_外・取り分の和 1）／T3 財布の 3 つの型／T4 名の写像／T5 点火とクーデタの紙の盤面／
/// T6 G＝L−W と旧の G＝H が違う盤面／T7 表が出て列が整う／T8 切れている時は仕事の件数が増えない。
/// 突然変異（使い捨ての木・measurements/m1090_20261113/）：M1 総和で割らない→T1・M2 d_V に DefectionShare→T1・M3 G＝H→T6／T7・M5 ΣF から q を落とす→T2。
/// </summary>
[Collection("Instruments")]
public class PowerCenterTests
{
    private const ulong Seed = 1789UL;

    [Fact]
    public void T1_兵の分かれの和は1で_倒し先は体制()
    {
        double[] mass = [0.2, 0.3, 0.5];
        var (l, a, v) = PowerCenter.SoldierSplit(mass, [1, 0, 0], [0, 1, 0], [0, 0, 1]);
        Assert.Equal(0.2, l, 12); Assert.Equal(0.3, a, 12); Assert.Equal(0.5, v, 12);
        // 質量の総和が 1 でなくても割合の和は 1（総和で割る）
        var (l2, a2, v2) = PowerCenter.SoldierSplit([0.1, 0.1, 0.2], [1, 0, 0], [0, 1, 0], [0, 0, 1]);
        Assert.Equal(1.0, l2 + a2 + v2, 12); Assert.Equal(0.25, l2, 12); Assert.Equal(0.5, v2, 12);
        // 同じ親和なら体制へ
        var (l3, a3, v3) = PowerCenter.SoldierSplit([1, 1], [0.5, 0.5], [0.5, 0.5], [0.5, 0.5]);
        Assert.Equal(1.0, l3, 12); Assert.Equal(0.0, a3 + v3, 12);
        // 軍政と革命が同じ（体制より上）なら軍政へ
        var (_, a4, v4) = PowerCenter.SoldierSplit([1], [0.1], [0.9], [0.9]);
        Assert.Equal(1.0, a4, 12); Assert.Equal(0.0, v4, 12);
        // 質量が空・NaN・負 → ℓ＝1
        var z = PowerCenter.SoldierSplit([0, double.NaN, -1], [1, 0, 0], [0, 1, 0], [0, 0, 1]);
        Assert.Equal((1.0, 0.0, 0.0), z);
    }

    private static PowerCenter.Inputs Case() => new(G: 100, Q: 2, MRad: 10, MRefOut: 5, Loyal: 0.5, Army: 0.3, Revolt: 0.2, Push: 0.8, InterestShare: 0.4, TaxYearShare: 0.5, RevenuePositive: true);

    [Fact]
    public void T2_力と取り分は紙の式で_ΣFはGとqMの和()
    {
        var x = Case();
        var r = PowerCenter.Evaluate(x, BudgetLaw.Partial);
        // 紙の式（手の数）：M_外＝15・ΣF＝100＋2×15＝130・s_革命＝(100×0.2＋30)÷130・弁＝0.5×((1−1)＋(1−s_革命))・m_A＝0.8×(1−弁)・w＝1−(1−0.5)(1−0.4)＝0.7
        double sRev = (100 * 0.2 + 2 * 15) / 130.0;
        double valve = 0.5 * ((1 - 1.0) + (1 - sRev)); double mA = 0.8 * (1 - valve);
        double regime = 0.5 + 0.3 * (1 - mA);
        Assert.Equal(130.0, r.Sum, 12);
        Assert.Equal(0.7, r.Wallet, 12);
        Assert.Equal(100 * regime * (1 - 0.7), r.FExec, 9);
        Assert.Equal(100 * regime * 0.7, r.FPart, 9);
        Assert.Equal(100 * 0.3 * mA, r.FArmy, 9);
        Assert.Equal(100 * 0.2 + 2 * 15, r.FRev, 9);
        foreach (var k in new[] { BudgetLaw.Decree, BudgetLaw.Partial, BudgetLaw.Parliamentary })
        {
            var rk = PowerCenter.Evaluate(x, k);
            Assert.True(System.Math.Abs(rk.SumParts - (100 + 2 * 15)) < 1e-9, $"ΣF {k} {rk.SumParts}");
            Assert.True(System.Math.Abs(rk.SExec + rk.SPart + rk.SArmy + rk.SRev - 1.0) < 1e-12);
        }
        // 議会の段：体制の力は全て議会へ（任免が議会・財布は効かない）
        var rp = PowerCenter.Evaluate(x, BudgetLaw.Parliamentary);
        Assert.Equal(0.0, rp.FExec, 12); Assert.True(rp.FPart > 0);
        // 倒し先：G＋qM_外 が 0 なら執政＝1
        var zero = PowerCenter.Evaluate(new PowerCenter.Inputs(0, 2, 0, 0, 1, 0, 0, 0, 0, 0, true), BudgetLaw.Decree);
        Assert.Equal(1.0, zero.SExec, 12); Assert.Equal(0.0, zero.SRev, 12);
    }

    [Fact]
    public void T3_財布の3つの型と倒し先()
    {
        Assert.Equal(0.4, PowerCenter.Wallet(BudgetLaw.Decree, 0.4, 0.9, true), 12);
        Assert.Equal(1 - (1 - 0.5) * (1 - 0.4), PowerCenter.Wallet(BudgetLaw.Partial, 0.4, 0.5, true), 12);
        Assert.Equal(1.0, PowerCenter.Wallet(BudgetLaw.Parliamentary, 0.0, 0.0, true), 12);
        Assert.Equal(0.0, PowerCenter.Wallet(BudgetLaw.Parliamentary, 0.4, 0.5, false), 12);   // 歳入が正でなければ 0
        Assert.Equal(0.0, PowerCenter.Wallet(BudgetLaw.Decree, double.NaN, 0.5, true), 12);   // NaN は 0
    }

    [Fact]
    public void T4_名の写像()
    {
        Assert.Equal(Polity.Absolutist, PowerCenter.NameOf(BudgetLaw.Decree, CrownForm.Absolutist, SeizureForm.None));
        Assert.Equal(Polity.Shogunate, PowerCenter.NameOf(BudgetLaw.Decree, CrownForm.Shogunate, SeizureForm.None));
        Assert.Equal(Polity.BourgeoisDictatorship, PowerCenter.NameOf(BudgetLaw.Decree, CrownForm.None, SeizureForm.None));
        Assert.Equal(Polity.ConstitutionalMonarchy, PowerCenter.NameOf(BudgetLaw.Partial, CrownForm.Absolutist, SeizureForm.None));
        Assert.Equal(Polity.Republic, PowerCenter.NameOf(BudgetLaw.Parliamentary, CrownForm.None, SeizureForm.None));
        Assert.Equal(Polity.MilitaryDictatorship, PowerCenter.NameOf(BudgetLaw.Parliamentary, CrownForm.None, SeizureForm.Military));
        Assert.Equal(BudgetLaw.Partial, PowerCenter.KFromPolity(Polity.ConstitutionalMonarchy));
        Assert.Equal(BudgetLaw.Decree, PowerCenter.KFromPolity(Polity.Despotic));
    }

    [Fact]
    public void T5_点火とクーデタの紙の盤面()
    {
        // 暴力：群衆が急進で大きい（G＝10・q＝1・M_急＝50・M_改外＝0・兵は全て体制）。F_革命＝50＞F_体制＝10・急進の側＞改革の側
        var xv = new PowerCenter.Inputs(10, 1, 50, 0, 1, 0, 0, 0, 0, 0, true);
        var rv = PowerCenter.Evaluate(xv, BudgetLaw.Decree);
        var dv = PowerCenter.Decide(xv, rv, 1.8);
        Assert.True(dv.Violent); Assert.False(dv.Peaceful); Assert.True(dv.Immediate); Assert.Equal(5.0, dv.Nu, 9);
        // 平和な転換：改革の群衆（道の無い）が大きい
        var xp = xv with { MRad = 0, MRefOut = 50 };
        var dp = PowerCenter.Decide(xp, PowerCenter.Evaluate(xp, BudgetLaw.Decree), 1.8);
        Assert.True(dp.Peaceful); Assert.False(dp.Violent);
        // 点火しない：体制の方が大きい
        var xq = new PowerCenter.Inputs(100, 1, 1, 0, 1, 0, 0, 0, 0, 0, true);
        var dq = PowerCenter.Decide(xq, PowerCenter.Evaluate(xq, BudgetLaw.Decree), 1.8);
        Assert.False(dq.Violent || dq.Peaceful || dq.Immediate);
        // クーデタ：勅令・群衆なし・押し 1 → 弁＝0.5×(0＋1)＝0.5・m_A＝0.5・d_A×m_A＝0.7×0.5＝0.35＞ℓ＝0.3
        var xc = new PowerCenter.Inputs(100, 0, 0, 0, 0.3, 0.7, 0, 1.0, 0, 0, true);
        var dc = PowerCenter.Decide(xc, PowerCenter.Evaluate(xc, BudgetLaw.Decree), 1.8);
        Assert.True(dc.Coup); Assert.Equal(0.35 / 0.3, dc.NuCoup, 9);
        // 同じ兵で議会の段（任免が議会）：弁＝0.5×(1＋1)＝1・m_A＝0 → クーデタなし
        var dm = PowerCenter.Decide(xc, PowerCenter.Evaluate(xc, BudgetLaw.Parliamentary), 1.8);
        Assert.False(dm.Coup);
    }

    [Fact]
    public void T6_G_はL_引くWで_旧のHと違う盤面がある()
    {
        Assert.Equal(80.0, PowerCenter.GOf(100, 20), 12);
        Assert.Equal(50.0, PowerCenter.HOf(100, 20, 50), 12);   // 人口×q が小さい盤面では H＜G
        Assert.Equal(0.0, PowerCenter.GOf(10, 20), 12);
        Assert.Equal(0.0, PowerCenter.GOf(double.NaN, 0), 12);
    }

    private static (MultiNationSim Sim, string Dir) Run(string? sel, int quarters, bool on)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cp_" + Guid.NewGuid().ToString("N"));
        if (on) Instruments.Configure(dir, "*"); else Instruments.Configure(null, null);
        Instruments.SelectTables(sel);
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        try
        {
            if (on) Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            if (on) Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (on) Instruments.Quarter(sim, Seed, q); }
        }
        finally { if (on) Instruments.Close(sim); }
        return (sim, dir);
    }

    private static void Reset() { Instruments.SelectTables(null); Instruments.Configure(null, null); }

    [Fact]
    public void T7_表が出て_列が整い_自己検査が通り_Gは旧のHより大きい行がある()
    {
        try
        {
            var (_, dir) = Run("center_s", 3, true);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"center_s{Seed}.tsv"));
            var head = lines[0].Split('\t');
            Assert.Equal(Instruments.CenterHeaderText.Split('\t'), head);
            int Ix(string n) => System.Array.IndexOf(head, n);
            double D(string[] f, string n) => double.Parse(f[Ix(n)], System.Globalization.CultureInfo.InvariantCulture);
            int rows = 0, gtH = 0;
            foreach (var ln in lines.Skip(1))
            {
                var f = ln.Split('\t'); Assert.Equal(head.Length, f.Length);
                rows++;
                Assert.True(System.Math.Abs(D(f, "loyalShare") + D(f, "armyShare") + D(f, "revoltShare") - 1.0) < 1e-9, "ℓ＋d_A＋d_V＝1");
                Assert.True(System.Math.Abs(D(f, "sumCheckIndex")) < 1e-9, "ΣF＝G＋qM_外");
                Assert.True(System.Math.Abs(D(f, "shareExec") + D(f, "sharePart") + D(f, "shareArmy") + D(f, "shareRev") - 1.0) < 1e-9, "取り分の和 1");
                Assert.True(D(f, "gHIndex") <= D(f, "gMinusWIndex") + 1e-12);
                if (D(f, "gMinusWIndex") > D(f, "gHIndex") + 1e-9) gtH++;
            }
            Assert.True(rows >= 4, $"対照：行がある rows={rows}");
            Assert.True(gtH >= 1, $"対照：G＞H の盤面が表に出る gtH={gtH}");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void T8_計器が切れている時は仕事の件数が増えず_入れた時は増える()
    {
        try
        {
            long c0 = Instruments.CenterWorkCount;
            Run(null, 3, false);
            Assert.Equal(c0, Instruments.CenterWorkCount);
            var (_, dir) = Run("center_s", 3, true);
            Assert.True(Instruments.CenterWorkCount > c0, "対照：入れた時に仕事が増えない");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }
}
