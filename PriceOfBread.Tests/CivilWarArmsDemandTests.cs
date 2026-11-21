using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(903) 線 c77cw の計器 <c>cwmult_s</c>（内戦の軍需の倍率）の不変条件。★(973) c87mw ⑰ で旗 CivilWarArmsDemandUnified を旧の 2.0 の枝ごと消した＝
/// 旧の式の字（11.7・18・厭戦の差 2.7）を固定した 4 件と旗の Theory を消し（事前登録 measurements/m973_20261108_prereg_mw.txt §0 (a) 1〜6）、
/// 計器の試験を接触の割合の規則（旗 MilitaryWarMultFromContact は (1281) で 1 に焼いて消した）の κ の双子へ直した。〈内戦＝対外戦〉の機構の番人は MilitaryWarMultContactTests の T4。
/// 期待値は式の外から：κ は試験が置く字・接触の割合は計器が自分で数えた値（エンジンの Diplomacy.Engagement を呼ばない別の出所）。</summary>
[Collection("Instruments")]
public class CivilWarArmsDemandTests
{
    /// <summary>T5′（計器）：多国 仏西（外交を凍結・陸続き＝接触が正・(903) の英仏は海越えで接触 0）に仏を親とする内戦を置き 2 四半期。cwmult_s に内戦の行が両国に出て、
    /// 実際の値は接触の割合の規則と全ての月で一致し（ずれ 0）、実際の和＝内戦の月＋κ×接触の和（κ＝0 なら月の数＝平時の倍率 1）、
    /// 旧の規則（×2.0）とは全ての月でずれる（内戦だけの増しが無い）。旧と新の規則の列は計器の定義のまま（18×月・9×(1+0.5×厭戦)×月）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(7.0)]
    public void 計器は内戦の月に接触の割合の規則と実際の値を数える(double kappa)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cwmult_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("スペイン")], seed: 1791UL) { DiplomacyFrozen = true };
        try
        {
            var fr = sim.Nations[0].World; var es = sim.Nations[1].World;
            foreach (var n in sim.Nations)
            {
                Assert.Equal(9.0, n.World.Coef.MilitaryWarMult);          // 前提（旧と新の規則の列の期待の出所）
                Assert.Equal(0.5, n.World.Coef.WarMobilizationDepth);
                n.World.Coef.WarArmsDemandPerContact = kappa;
            }
            fr.SelfPower!.WarExhaustion = 0.4; es.SelfPower!.WarExhaustion = 0.0;
            sim.Diplomacy.Wars.Add(new War { A = fr.SelfPower!, B = es.SelfPower!, CivilWarParent = fr.SelfPower });
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
            double ct = D(r, "contactShareSumIndex");
            Assert.True(ct > 0, $"{r["nation"]} 接触の割合の和が 0（前提：陸続きの戦線）");
            Assert.Equal("0", r["zeroContactWarMonthsCount"]);
            Assert.Equal("0", r["contactRuleMismatchMonthsCount"]);
            double want = months + kappa * ct;
            Assert.True(Math.Abs(D(r, "warMultActualCivilSumIndex") - want) <= 1e-9 * want, $"{r["nation"]} 実際の和 {r["warMultActualCivilSumIndex"]}（期待 {want:G17}）");
            Assert.Equal("3", r["oldRuleMismatchMonthsCount"]);   // 内戦の 2.0 の枝が無い
            double per = r["nation"] == "フランス" ? 9.0 * 1.2 : 9.0;    // 厭戦 0.4（仏）・0（西）
            Assert.True(Math.Abs(D(r, "warMultNewRuleCivilSumIndex") - per * months) <= 1e-9, $"{r["nation"]} 新の規則の和 {r["warMultNewRuleCivilSumIndex"]}");
            Assert.True(Math.Abs(D(r, "warMultOldRuleCivilSumIndex") - 18.0 * months) <= 1e-9, $"{r["nation"]} 旧の規則の和 {r["warMultOldRuleCivilSumIndex"]}");
            Assert.True(D(r, "militaryScaleCivilSumIndex") > 0, "前提：装備の需要の規模が 0");
            if (r["nation"] == "フランス") Assert.True(D(r, "ammunitionDemandCivilQty") > 0, $"{r["nation"]} 内戦の月の弾薬の需要が 0");   // 西は 1791 の盤面で弾薬の市場の需要が 0（(973) の 1 回目で確かめた）＝仏の行だけ
        }
        try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
    }
}
