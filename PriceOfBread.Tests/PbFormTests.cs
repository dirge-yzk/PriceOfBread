using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(384) 政治の束の削除の形の番人。事前登録 measurements/pb_20261017_prereg.txt。
/// 腕 B は既存の係数の中立値（POB_COEF）と旗 1 本 DeleteRegimeChangeRelief で作る＝ここは〈B の名前が実在し既定が中立でない〉〈旗が :1901 の枝だけを切る〉〈中立値が本当に中立〉を確かめる。
/// 期待値は本体の式から作らない（旗 0 と旗 1 の世界の差、係数 0 と係数あり の世界の差だけを見る）。
/// 突然変異で赤になる箇所：Politics.ResolveRevolt の旗の条件を外す／旗の枝が別の層まで切る／Ideology の恐慌の 3 項を係数から切り離す／Crime の減算の門を外す／B の名簿の綴りを変える。</summary>
[Collection("Instruments")]
public class PbFormTests
{
    // 腕 B の POB_COEF（事前登録 [4]）。中立値＝0（旗は 1）。
    private static readonly string[] BCoefficients =
    [
        "DominanceCoupWeight", "AutocraticPolicyAgility", "CrimeSatPenalty", "CrimeWealthFearWeight", "DefaultMilitancyHit", "DeposedMilitancySpike",
        "WarDefeatMilitancySpike", "SeizureMilitancyCost", "DepressionSocialism", "DepressionCommunism", "DepressionReaction",
    ];

    [Fact]
    public void Bの名前は実在し既定は中立でなく旗の既定は0()
    {
        var c = new Coefficients();
        foreach (var n in BCoefficients)
        {
            var f = typeof(Coefficients).GetField(n);
            Assert.True(f is not null && f.FieldType == typeof(double), $"B の係数 {n} が Coefficients に無い・double でない（POB_COEF が例外で止まる）");
            Assert.True((double)f!.GetValue(c)! > 0, $"B の係数 {n} の既定が正でない＝0 にしても腕 A と同じ（何もしない腕になる）");
        }
        var flag = typeof(Coefficients).GetField("DeleteRegimeChangeRelief");
        Assert.True(flag is not null && flag.FieldType == typeof(double));
        Assert.Equal(0.0, (double)flag!.GetValue(c)!);
        Assert.True(c.ReformMilitancyRelief is > 0 and < 1, "ReformMilitancyRelief の既定が 0〜1 の間でない＝緩和にならない");
        // 計器の列：scalars_s の見出しに追加の 15 列がある（読み手が名前で引く）
        string hdr = (string)typeof(Instruments).GetField("ScalarsHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetRawConstantValue()!;
        foreach (var col in new[] { "meanSatPol", "maxSeat", "govParty", "gdpDrawdown", "factionRevolts", "pbDomActive", "pbDomFlip", "pbDomDecisive", "pbDomExec", "pbDomMismatch", "pbRegime", "pbRegimeViolent", "pbRegimeDict", "pbAgil", "pbSeize" })
            Assert.Contains("\t" + col, hdr);
    }

    // 政変の実行部を 1 回踏んで、各 POP の闘争度を返す（失脚した層は +DeposedMilitancySpike でクランプ、それ以外は ×ReformMilitancyRelief）。
    private static List<Pop> Revolt(Action<Coefficients> setup)
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var c = w.Coef;
        setup(c);
        foreach (var p in w.AllPops()) p.Militancy = 0.4;   // 全 POP を同じ闘争度から始める（0.4＋0.6＝1.0 でクランプに届く・×0.5＝0.2）
        Politics.ExecuteRegimeChange(w, c, Polity.Republic, violent: false);
        return w.AllPops().ToList();
    }

    [Fact]
    public void 旗は政変の緩和の枝だけを切り_失脚の加算は係数で切れる()
    {
        var a = Revolt(c => { });                                              // 腕 A
        var flag = Revolt(c => c.DeleteRegimeChangeRelief = 1.0);              // 旗だけ
        var spike0 = Revolt(c => c.DeposedMilitancySpike = 0.0);               // 失脚の加算だけ 0
        Assert.Equal(a.Count, flag.Count); Assert.Equal(a.Count, spike0.Count);
        double relief = new Coefficients().ReformMilitancyRelief, spike = new Coefficients().DeposedMilitancySpike;
        int relieved = 0, deposed = 0;
        for (int i = 0; i < a.Count; i++)
        {
            double ma = a[i].Militancy, mf = flag[i].Militancy, ms = spike0[i].Militancy;
            if (Math.Abs(ma - 0.4 * relief) < 1e-12)          // 失脚しなかった層：A は ×relief
            {
                relieved++;
                Assert.Equal(0.4, mf, 12);                    // 旗 1＝緩和しない（据え置き）
                Assert.Equal(ma, ms, 12);                     // 失脚の加算を 0 にしても この層は変わらない
            }
            else                                              // 失脚した層（または対象外）
            {
                if (Math.Abs(ma - Math.Min(1.0, 0.4 + spike)) < 1e-12) { deposed++; Assert.Equal(ma, mf, 12); Assert.Equal(0.4, ms, 12); }   // 旗では変わらず、加算を 0 にすると据え置き
                else Assert.Equal(ma, mf, 12);                // どちらでもない層（兵士など）は旗でも変わらない
            }
        }
        Assert.True(relieved > 0, "前提：緩和を受ける層が無い＝旗の枝を確かめられない");
        Assert.True(deposed > 0, "前提：失脚する層が無い＝加算の枝を確かめられない");
    }

    [Fact]
    public void 恐慌の3項は係数0で消え_恐慌が無い世界と同じ思想の分布になる()
    {
        // 恐慌（GdpDrawdown＝トレンドGDPのピーク比収縮）を置いて思想の分布を 1 回更新する。対照＝恐慌 0。
        Func<double, Action<Coefficients>?, List<double[]>> run = (draw, setup) =>
        {
            var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
            var w = sim.World; setup?.Invoke(w.Coef);
            sim.Run(2);   // 分布を動かす助走（POP に思想の分布が立つ）
            typeof(World).GetProperty("GdpPeak")!.SetValue(w, 100.0);
            typeof(World).GetProperty("GdpTrend")!.SetValue(w, 100.0 * (1.0 - draw));
            Ideologies.UpdateShares(w);
            return w.AllPops().Select(p => (double[])p.IdeologyShares.Clone()).ToList();
        };
        var calm = run(0.0, null);
        var crisis = run(0.3, null);
        var crisisNeutral = run(0.3, c => { c.DepressionSocialism = 0; c.DepressionCommunism = 0; c.DepressionReaction = 0; });
        Assert.Equal(calm.Count, crisis.Count);
        bool anyDiff = false;
        for (int i = 0; i < calm.Count; i++)
        {
            for (int k = 0; k < calm[i].Length; k++)
            {
                Assert.Equal(calm[i][k], crisisNeutral[i][k], 15);   // 3 項を 0 にした恐慌の世界＝恐慌の無い世界（ビット一致の近似）
                if (calm[i][k] != crisis[i][k]) anyDiff = true;
            }
        }
        Assert.True(anyDiff, "恐慌 0.3 で係数が既定でも思想の分布が動かない＝恐慌→思想の経路が消えている（B の効きを確かめられない）");
    }

    [Fact]
    public void 犯罪の満足度の減算は2係数0で消える()
    {
        // 秩序を崩して犯罪率を正にし、満足度の減算を比べる（Crime.Step は税の後の貯蓄と闘争度を読む）。対照＝2 係数 0。
        Func<Action<Coefficients>?, double> run = setup =>
        {
            var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
            var w = sim.World; var c = w.Coef; setup?.Invoke(c);
            sim.Run(2);
            c.CrimeOrderPerPop = 1e9;   // 必要な治安維持力が桁違い＝秩序≈0（犯罪の母体が太る）
            foreach (var p in w.AllPops()) { p.Militancy = 0.6; p.Satisfaction = 0.5; p.SetSavings(p.Size * 5.0); }
            Crime.Step(w, c);
            return w.AllPops().Sum(p => p.Satisfaction * p.Size) / w.AllPops().Sum(p => p.Size);
        };
        double with = run(null);
        double without = run(c => { c.CrimeSatPenalty = 0; c.CrimeWealthFearWeight = 0; });
        Assert.True(with < without - 1e-6, $"犯罪の満足度の減算が働かない（係数あり {with:F6}・0 {without:F6}）＝前提が崩れた（犯罪率が 0）か減算が消えている");
        Assert.True(without <= 0.5 + 1e-12, "犯罪を 0 にした世界で満足度が上がっている（減算以外が満足度を動かした）");
    }
}
