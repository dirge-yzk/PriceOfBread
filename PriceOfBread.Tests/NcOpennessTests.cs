using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>同化の番人（段4・2026-09-09）。国民意識の開放度と移民の運び（P12 残渣）の試験は (1193) c119nd で国民意識ごと消した。</summary>
public class NcOpennessTests
{
    // ── 段4（2026-09-09）：§7-7の実バグ2件 ─────────────────────────────
    // a. 同化が速すぎる（到達年で判定）／b. 国民意識が国内経路で保存しない（裁定5の導出化で定義ごと消す）

    /// <summary>同化の速度は**到達年から逆算**した値である（勘で置いていないことの番人）。
    /// 式は「毎四半期 民族差 ×(1−a)」なので、民族差が半分になる年数 = ln2 / (−ln(1−a)) / 4。</summary>
    [Fact]
    public void 段4_同化の速度は半減期100年と1000年から逆算されている()
    {
        var c = new Coefficients();
        double HalfLifeYears(double a) => Math.Log(0.5) / Math.Log(1.0 - a) / Calendar.QuartersPerYear;
        // 公教育なし＝半減期1000年（墺の独語話者シェアが4回の国勢調査で横這い）
        Assert.Equal(1000.0, HalfLifeYears(c.EthnicAssimilation), 1);
        // 公教育あり＝半減期100年（仏＝革命から農民をフランス人にするのにほぼ1世紀）
        double edu = c.EthnicAssimilation + c.AssimilationEducationBonus;
        Assert.Equal(100.0, HalfLifeYears(edu), 1);
        // 検算＝81年後（1789→1870）に閉じるのは半分未満＝ブルターニュ/オクシタニアはまだフランス語の多数派でない
        Assert.True(1.0 - Math.Pow(1.0 - edu, 81 * Calendar.QuartersPerYear) < 0.5);
        // 検算＝1914断面（主指標・Q524）までに公教育なしでは1割に届かない＝学校を持たない帝国は大戦まで多民族（墺洪の国勢調査の横這い）。
        //   ★2026-09-22：旧「ゲーム全長で1割未満」は長さの主張だった（604Qで9.9%・1950延長の668Qで10.9%）＝史実の主張の断面へ書き直した
        Assert.True(1.0 - Math.Pow(1.0 - c.EthnicAssimilation, Coefficients.FidelityQuarters) < 0.10);
    }

    /// <summary>同化の式を単離する2国世界（同化の段だけを呼ぶ＝CoreEthnicShare を動かす経路は同化1本）。</summary>
    private static (MultiNationSim sim, Pop pop) AssimWorld(double revival, double literacy, double core)
    {
        var sim = new MultiNationSim([WorldData.ByName("スイス"), WorldData.ByName("ジェノヴァ")], seed: 17);
        sim.Coef.AssimilationRevival = revival;
        var w = sim.Nations[0].World;
        w.EthnicEnabled = true;
        foreach (var p in w.AllPops()) { p.Literacy = literacy; p.CoreEthnicShare = new Share(core); }
        var pop = w.Regions[0].Pops.Find(x => x.Size > 1e-9)!;
        return (sim, pop);
    }

    [Fact]
    public void 段4_逆流は既定で起きない_床が同化を止めるだけ()
    {
        var (sim, pop) = AssimWorld(revival: 0, literacy: 1.0, core: 0.4);   // 識字1.0＝抵抗が最大の帯
        double before = pop.CoreEthnicShare.Value;
        sim.StepAssimilation();
        var c = sim.Coef;
        double assim = c.EthnicAssimilation + (sim.Nations[0].World.EducationLaw is EducationLaw.Public or EducationLaw.Compulsory ? c.AssimilationEducationBonus : 0);
        double resist = Math.Max(0.0, 1.0 - c.AssimilationLiteracyResist * 1.0);
        Assert.Equal(before + assim * resist * (1.0 - before), pop.CoreEthnicShare.Value, 12);
        Assert.True(pop.CoreEthnicShare.Value > before, "既定（床あり）では識字がいくら高くても同化は止まるだけで逆流しない");
    }

    [Fact]
    public void 段4_点火すると識字の高い少数派で同化が逆流する()
    {
        const double ign = 1.0 / 0.95;   // 点火値＝反転点をボヘミア1900の識字0.95に置く
        // 識字0.99＞0.95＝逆流（自民族語の学校網と出版市場を持てた少数派）
        var (simHi, popHi) = AssimWorld(revival: ign, literacy: 0.99, core: 0.4);
        double beforeHi = popHi.CoreEthnicShare.Value;
        simHi.StepAssimilation();
        Assert.True(popHi.CoreEthnicShare.Value < beforeHi, $"識字0.99では中核度が下がるはず（{beforeHi:F6}→{popHi.CoreEthnicShare.Value:F6}）");
        // 識字0.58＜0.95＝なお同化（ハンガリーのマジャール化の反例が壊れないこと）
        var (simLo, popLo) = AssimWorld(revival: ign, literacy: 0.58, core: 0.4);
        double beforeLo = popLo.CoreEthnicShare.Value;
        simLo.StepAssimilation();
        Assert.True(popLo.CoreEthnicShare.Value > beforeLo, "識字0.58では同化が進むはず");
        // 反転点ちょうど（識字0.95）では1ビットも動かない＝符号が連続に反転する
        var (simMid, popMid) = AssimWorld(revival: ign, literacy: 0.95, core: 0.4);
        double beforeMid = popMid.CoreEthnicShare.Value;
        simMid.StepAssimilation();
        Assert.Equal(beforeMid, popMid.CoreEthnicShare.Value, 15);
    }

    /// <summary>逆流の安全弁＝中核度1.0のPOPは1ビットも動かない（単一文化国の自国民が自分から離反する暴走を構造で閉じる）。</summary>
    [Fact]
    public void 段4_逆流の核は中核度1のPOPを動かさない()
    {
        var (sim, pop) = AssimWorld(revival: 1.0 / 0.95, literacy: 1.0, core: 1.0);
        sim.StepAssimilation();
        Assert.Equal(1.0, pop.CoreEthnicShare.Value, 15);
    }
}
