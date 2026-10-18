using System;
using System.Collections.Generic;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★作者要望2026-08-17「**金のないPOPほど買収しやすい**」のON経路。（汚職に手を染めやすい側＝B は汚職の廃止で 2026-10-18 に削除した。）
///
/// <para>A＝買収しやすさ（<see cref="Coefficients.ElectionBribePovertyWeight"/>・既定0＝休眠）：
/// 半減点の単価を「全国一律の一人あたり名目所得」から「その有権者pop自身の前月所得」へブレンドする。</para>
/// <code>
/// half ＝ 係数 × Σ_pop [ 声_p × ( (1−w)×規模_p + w × max(0,前月所得_p) ÷ 全国の1人あたり所得 ) ] × (名目GDP÷人口)
/// </code>
///
/// ここで固定するもの：(a)係数の既定0の番人 (b)貧しい選挙民ほど半減点が小さい (c)0でビット同値・1で完全pop別 (d)ONでの決定論。
/// </summary>
public class PoorPopBriberyTests
{
    // ============ 盤面と物差し ============

    /// <summary>単一国（英＝制限選挙）。NullAgent＝popも法も勝手に動かない＝式だけを見る軽い盤面（ElectionAiTests と同じ作法）。</summary>
    private static Simulation ElectiveWorld(ulong seed)
        => new(WorldData.ByName("イギリス").ToScenario(seed), new NullAgent());

    /// <summary>世論・所得が立った盤面（公示の四半期＝15 は跨がない）。</summary>
    private static World Board(ulong seed, int quarters = 6)
    {
        var sim = ElectiveWorld(seed);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return sim.World;
    }

    /// <summary>有権者数＝Σ 規模×FranchiseVoice（<see cref="Parliament"/> の ComputeVotes が議席を数えるのと**同じ重み**）。</summary>
    private static double VotersOf(World w)
    {
        double v = 0;
        foreach (var p in w.AllPops()) v += p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
        return v;
    }

    /// <summary>全国一律の半減点（＝富加重0のときの式）をテスト側で独立に組み直す。</summary>
    private static double UniformHalf(World w)
        => (w.Coef.ElectionPushHalfPerVoter * VotersOf(w) * (w.NominalGdpPerMonth / w.TotalPopulation)).Value;

    /// <summary>富加重 w の半減点をテスト側で独立に組み直す（式が変わればこの番人が落ちる）。</summary>
    private static double BlendedHalf(World w, double bribeW)
    {
        double incSum = 0, sizeSum = 0;
        foreach (var p in w.AllPops()) { incSum += Math.Max(0, p.LastEarnedPerMonth.Value); sizeSum += p.Size; }
        double mean = incSum / sizeSum;
        double weighted = 0;
        foreach (var p in w.AllPops())
        {
            double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (!(voice > 0)) continue;
            weighted += voice * ((1.0 - bribeW) * p.Size + bribeW * (Math.Max(0, p.LastEarnedPerMonth.Value) / mean));
        }
        return (w.Coef.ElectionPushHalfPerVoter * weighted * (w.NominalGdpPerMonth / w.TotalPopulation)).Value;
    }

    /// <summary>相対許容での一致（金額は桁が大きい＝絶対桁指定の Assert.Equal は使えない）。</summary>
    private static void AssertClose(double expect, double actual, string what, double rel = 1e-9)
        => Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"{what}：期待 {expect} ／ 実際 {actual}");

    // ============ (a) 既定0の番人 ============

    [Fact]
    public void 買収の係数の既定は休眠()
    {
        // ⚠POB_COEF で明示上書きした測定走行では既定値を見ない（点火A/Bを同一バイナリで取るため・ElectionAiTests と同じ作法）
        string ov = Coefficients.OverrideSummary();
        var c = new Coefficients();
        if (!ov.Contains("ElectionBribePovertyWeight", StringComparison.Ordinal))
            Assert.Equal(0.0, c.ElectionBribePovertyWeight);
    }

    // ============ (b) A：貧しい選挙民ほど安く買える ============

    [Fact]
    public void A_有権者が貧しい国ほど半減点が小さく同じ投入で効きが大きい()
    {
        var w = Board(5);
        w.Coef.ElectionBribePovertyWeight = 1.0;
        // ★(205) 2026-10-04 盤面の前提（裁量余剰）を明示で置く＝この番人は買収の効きの比だけを見る（余剰の大きさは比に効かない）。
        //   旧の盤面は 1784 年の英（NullAgent）が r 0.92〜0.96 の8か月で不履行になり国債が消えて余剰が立っていた＝不履行の計数を払えなかった大きさの累計にした (205) では
        //   累計 0.70（6四半期目）で不履行にならず国債が残る（利払い 2,657 ＞ 歳入 1,854）＝余剰0。
        if (!(w.FiscalCapacityPerMonth.Value > 0)) w.FiscalCapacityPerMonth = w.LastRevenuePerMonth;

        // 有権者（声>0）と非有権者（声=0）を取り、**総所得を変えずに**所得を有権者→非有権者へ移す。
        // ＝全国の1人あたり所得（ブレンドの分母）も名目GDPも人口も1ビットも動かない＝動くのは「有権者が豊かか」だけ。
        Pop? voter = null, outsider = null;
        foreach (var p in w.AllPops())
        {
            double voice = InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            if (voice > 0 && p.LastEarnedPerMonth.Value > 0 && (voter is null || p.LastEarnedPerMonth > voter.LastEarnedPerMonth)) voter = p;
            if (!(voice > 0) && outsider is null) outsider = p;
        }
        Assert.True(voter is not null, "前提の破れ：所得のある有権者popが居ない");
        Assert.True(outsider is not null, "前提の破れ：非有権者popが居ない（普通選挙の盤面では所得移転の相手が作れない）");

        double half0 = w.ElectionPushHalfPoint();
        double eff0 = w.ElectionPushEffectOf(0.5);
        Assert.True(half0 > 0 && eff0 > 0, "前提の破れ：半減点も効きも立たない盤面");

        double move = (0.5 * voter!.LastEarnedPerMonth).Value;
        voter.LastEarnedPerMonth -= new MoneyPerMonth(move); outsider!.LastEarnedPerMonth += new MoneyPerMonth(move);

        double half1 = w.ElectionPushHalfPoint();
        double eff1 = w.ElectionPushEffectOf(0.5);
        Assert.True(half1 < half0, $"有権者が貧しくなったのに半減点が下がらない（{half0} → {half1}）＝票の値段が稼ぎで決まっていない");
        Assert.True(eff1 > eff0, $"同じ投入額（裁量余剰の50%）で効きが増えない（{eff0} → {eff1}）＝安く買えていない");

        // 対照：全国一律（既定0）では**同じ所得移転で半減点が1ビットも動かない**＝動かしたのは富加重だけ
        w.Coef.ElectionBribePovertyWeight = 0.0;
        double flatPoor = w.ElectionPushHalfPoint();
        voter.LastEarnedPerMonth += new MoneyPerMonth(move); outsider.LastEarnedPerMonth -= new MoneyPerMonth(move);
        Assert.Equal(flatPoor, w.ElectionPushHalfPoint());
    }

    // ============ (c) A：0でビット同値・1で完全pop別 ============

    [Fact]
    public void A_ブレンド0は現行とビット同値で1は完全pop別()
    {
        var w = Board(5);

        // ① 0＝否定形ガードで即 return＝全国一律の現行式そのもの（丸め桁を指定しない＝**ビット同値**）
        w.Coef.ElectionBribePovertyWeight = 0.0;
        double uniform = UniformHalf(w);
        Assert.True(uniform > 0, "前提の破れ：有権者も所得も無い盤面");
        Assert.Equal(uniform, w.ElectionPushHalfPoint());

        // ② 1＝完全にpop別（テスト側で独立に組み直した式とビット同値）
        w.Coef.ElectionBribePovertyWeight = 1.0;
        double popWise = BlendedHalf(w, 1.0);
        Assert.Equal(popWise, w.ElectionPushHalfPoint());
        Assert.True(Math.Abs(popWise - uniform) > 1e-6 * Math.Max(1.0, uniform),
            $"0と1で半減点が同じ（{uniform}）＝ブレンドが空虚（有権者の1人あたり所得が全国平均と偶然一致した盤面）");
        // 制限選挙の英＝有権者は富裕層＝pop別にすると票は**高くなる**（狙った向き＝金のない者ほど安い）
        Assert.True(popWise > uniform, $"制限選挙で有権者（富裕層）の票が安くなっている（{uniform} → {popWise}）");

        // ③ 中間は両端の内分（形が線形ブレンドであることの固定）
        w.Coef.ElectionBribePovertyWeight = 0.5;
        Assert.Equal(BlendedHalf(w, 0.5), w.ElectionPushHalfPoint());
        AssertClose(0.5 * (uniform + popWise), w.ElectionPushHalfPoint(), "ブレンド0.5＝両端の内分");
    }

    // ============ (d) 決定論 ============

    [Fact]
    public void 買収の係数ONでも同シードは同一の最終状態を生む()
    {
        Assert.Equal(Digest(4242), Digest(4242));

        static string Digest(ulong seed)
        {
            var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("アメリカ")], seed);
            foreach (var n in sim.Nations)
            {
                n.World.Coef.ElectionBribePovertyWeight = 1.0;
            }
            sim.StartWarmup();
            for (int q = 0; q < 16; q++) sim.StepQuarter();
            var sb = new System.Text.StringBuilder();
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                sb.Append(w.Treasury.Value.ToString("R")).Append('|')
                  .Append(w.TotalMoney.Value.ToString("R")).Append('|')
                  .Append(w.TotalPopulation.ToString("R")).Append('|')
                  .Append(w.EffectiveCorruption.ToString("R")).Append('|')
                  .Append(w.ElectionPushHalfPoint().ToString("R")).Append('|')
                  .Append(w.VoteShares[0].ToString("R")).Append(';');
            }
            return sb.ToString();
        }
    }
}
