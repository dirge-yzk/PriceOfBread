using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(903) 線 c77cw：内戦の軍需の倍率の枝を消す旗 <see cref="Coefficients.CivilWarArmsDemandUnified"/> の機構の番人と、計器 <c>cwmult_s</c> の不変条件。
/// 事前登録 measurements/m903_20261106_prereg_cw.txt §5。期待値は字の値（MilitaryWarMult 9・WarMobilizationDepth 0.5 を前提として Assert してから 11.7・18・2.7）＝エンジンの式から作らない。
/// 盤面＝単国 France 1789（外交を進めない＝厭戦は置いた値のまま）。1 か月進めて首都州の <see cref="Region.MilitaryWarMultLast"/>（その月に使った戦時の倍率）を読む。
/// 突然変異（使い捨ての木・measurements/m903_20261106/mutation.txt）：M1 枝を残す・M2 内戦で 1.0・M3 総動員を外す・M4 対外戦も 2 倍。</summary>
[Collection("Instruments")]
public class CivilWarArmsDemandTests
{
    private static double MonthWarMult(double flag, bool civil, double exhaustion)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(1);
        var w = sim.World; var c = w.Coef;
        Assert.Equal(9.0, c.MilitaryWarMult);          // 前提（字の値の期待の出所）
        Assert.Equal(0.5, c.WarMobilizationDepth);
        c.CivilWarArmsDemandUnified = flag;
        w.StepsDiplomacy = false;                       // 厭戦を置いた値のまま（外交の月の歩みを止める）
        var dip = new Diplomacy();
        var me = new Power { Name = "政府", Nation = w, WarExhaustion = exhaustion };
        w.Diplomacy = dip; w.SelfPower = me;
        dip.Wars.Add(new War { A = me, B = new Power { Name = "相手" }, CivilWarParent = civil ? me : null });
        Assert.Equal(civil, w.InCivilWar);
        for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
        Assert.Equal(exhaustion, me.WarExhaustion);     // 前提：厭戦が動いていない
        return w.Region.MilitaryWarMultLast;
    }

    /// <summary>T1：旗 1 の内戦＝9×(1+0.5×0.6)＝11.7（旧の 18 でも 1.0 でも総動員なしの 9 でもない）。</summary>
    [Fact]
    public void 旗1の内戦の戦時の倍率は対外戦と同じ総動員の式()
    {
        double x = MonthWarMult(1.0, civil: true, exhaustion: 0.6);
        Assert.True(Math.Abs(x - 11.7) <= 1e-9, $"内戦の戦時の倍率 {x:G17}（期待 11.7）");
    }

    /// <summary>T2：旗 1 の対外戦も 11.7＝内戦と同じ値（対外戦の側を 2 倍にしない）。</summary>
    [Fact]
    public void 旗1の対外戦の戦時の倍率は内戦と同じ値()
    {
        double f = MonthWarMult(1.0, civil: false, exhaustion: 0.6);
        double cw = MonthWarMult(1.0, civil: true, exhaustion: 0.6);
        Assert.True(Math.Abs(f - 11.7) <= 1e-9, $"対外戦の戦時の倍率 {f:G17}（期待 11.7）");
        Assert.Equal(f, cw);
    }

    /// <summary>T3：旗 1 の内戦で厭戦 0 と 0.6 の差＝0.5×9×0.6＝2.7（総動員が内戦にも効く）。</summary>
    [Fact]
    public void 旗1の内戦は厭戦で総動員が深まる()
    {
        double lo = MonthWarMult(1.0, civil: true, exhaustion: 0.0);
        double hi = MonthWarMult(1.0, civil: true, exhaustion: 0.6);
        Assert.True(Math.Abs(lo - 9.0) <= 1e-9, $"厭戦 0 の内戦の戦時の倍率 {lo:G17}（期待 9）");
        Assert.True(Math.Abs(hi - lo - 2.7) <= 1e-9, $"厭戦 0.6 と 0 の差 {hi - lo:G17}（期待 2.7）");
    }

    /// <summary>T4：旗 0 の内戦＝18（休眠の枝が今の形のまま・厭戦を読まない）。</summary>
    [Fact]
    public void 旗0の内戦は今の2倍の枝のまま()
    {
        double x = MonthWarMult(0.0, civil: true, exhaustion: 0.6);
        Assert.True(Math.Abs(x - 18.0) <= 1e-9, $"旗 0 の内戦の戦時の倍率 {x:G17}（期待 18）");
    }

    /// <summary>T5（計器）：多国 英仏（外交を凍結）に仏を親とする内戦を置き 2 四半期。cwmult_s に内戦の行が両国に出て、
    /// 旗 0 では旧の規則と実際が全ての月で一致し実際の和＝18×内戦の月、旗 1 では新の規則と実際が一致し実際の和＝9×(1+0.5×0.4)×内戦の月（仏）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 計器は同じ盤面で旧と新の規則と実際の値を数える(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cwmult_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1791UL) { DiplomacyFrozen = true };
        try
        {
            var gb = sim.Nations[0].World; var fr = sim.Nations[1].World;
            foreach (var n in sim.Nations) n.World.Coef.CivilWarArmsDemandUnified = flag;
            fr.SelfPower!.WarExhaustion = 0.4; gb.SelfPower!.WarExhaustion = 0.0;
            sim.Diplomacy.Wars.Add(new War { A = fr.SelfPower!, B = gb.SelfPower!, CivilWarParent = fr.SelfPower });
            for (int q = 0; q < 2; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1791UL, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var rows = SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, "cwmult_s1791.tsv"), Instruments.CwHeaderText);
        Assert.Equal(4, rows.Count);   // 2 国×2 四半期（どちらも全ての月が内戦）
        double D(Dictionary<string, string> r, string k) => double.Parse(r[k], System.Globalization.CultureInfo.InvariantCulture);
        foreach (var r in rows)
        {
            int months = int.Parse(r["civilWarMonthsCount"]);
            Assert.Equal(3, months);
            Assert.Equal("0", r["foreignWarMonthsCount"]);
            double per = r["nation"] == "フランス" ? 9.0 * 1.2 : 9.0;    // 厭戦 0.4（仏）・0（英）
            if (flag > 0)
            {
                Assert.Equal("0", r["newRuleMismatchMonthsCount"]);
                Assert.Equal("3", r["oldRuleMismatchMonthsCount"]);
                Assert.True(Math.Abs(D(r, "warMultActualCivilSumIndex") - per * months) <= 1e-9, $"{r["nation"]} 実際の和 {r["warMultActualCivilSumIndex"]}（期待 {per * months}）");
            }
            else
            {
                Assert.Equal("0", r["oldRuleMismatchMonthsCount"]);
                Assert.Equal("3", r["newRuleMismatchMonthsCount"]);
                Assert.True(Math.Abs(D(r, "warMultActualCivilSumIndex") - 18.0 * months) <= 1e-9, $"{r["nation"]} 実際の和 {r["warMultActualCivilSumIndex"]}（期待 54）");
            }
            Assert.True(Math.Abs(D(r, "warMultNewRuleCivilSumIndex") - per * months) <= 1e-9, $"{r["nation"]} 新の規則の和 {r["warMultNewRuleCivilSumIndex"]}");
            Assert.True(Math.Abs(D(r, "warMultOldRuleCivilSumIndex") - 18.0 * months) <= 1e-9, $"{r["nation"]} 旧の規則の和 {r["warMultOldRuleCivilSumIndex"]}");
            Assert.True(D(r, "militaryScaleCivilSumIndex") > 0, "前提：装備の需要の規模が 0");
            Assert.True(D(r, "ammunitionDemandCivilQty") > 0, $"{r["nation"]} 内戦の月の弾薬の需要が 0");
        }
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
    }
}
