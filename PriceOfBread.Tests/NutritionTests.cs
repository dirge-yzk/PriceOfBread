using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>塊1 段1a/1b（Nutrition配線と動物性タンパクの食料化・docs/20260830/20260830_塊1）のON経路検証。
/// 「ゲート緑≠機構が働いた」＝ギッフェンの創発（肉高騰→需要がパン/芋へ流れる）を双子シムで直接観測する。</summary>
public class NutritionTests
{
    [Fact]
    public void 既定は休眠で栄養データは配線済み()
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.StapleElasticityZero);      // ★2026-08-31点火（ラウンドB・符号の是正）
        Assert.Equal(0.0, c.FoodIncludesAnimalProtein); // 休眠温存（英常勝復活のため＝負帰還とセットで再挑戦）
        // goods.json の nutrition 列＝加工肉63/干物64のみ1.0（dumpgoods再生成2026-08-31）
        Assert.Equal(1.0, Catalog.Goods[63].Nutrition);
        Assert.Equal(1.0, Catalog.Goods[64].Nutrition);
        Assert.True(Catalog.Goods[62].Nutrition < 0, "主食（パン）はTier由来＝-1のはず（データで上書きしない）");
        // 再配分率＝データ導出（0.22/1.10=0.2）
        Assert.Equal(0.2, Catalog.AnimalNutritionNeedRatio, 2);
    }

    [Fact]
    public void ギッフェンの創発_肉が高騰すると需要がパンへ流れゲート0では流れない()
    {
        // ⚠絶対比較（ON vs OFF）は物差し違い＝ONは主食BaseNeedの×0.8再配分（総食料の中立化）で基準量ごと縮むため、
        //   交差の増分が隠れる（初版で実測 OFF1071.6/ON1041.9）。ギッフェンの証拠は**肉の価格だけを動かした双子**で見る：
        //   同じゲートの2世界で肉高騰側だけパンの需要が増えるか（ゲート0では肉→パンの経路が無い＝不変が対照）。
        const int bread = 62, meat = 63;
        static (double breadD, double meatD) Probe(double gate, bool meatSpike)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
            sim.Run(20);
            sim.World.Coef.FoodIncludesAnimalProtein = gate;
            var r = sim.World.Region;
            if (meatSpike) r.Markets[meat].Price *= 5.0;   // 肉だけ高騰（パンの価格は不変）
            EconomyStep.Run(r, sim.World.Coef);
            return (r.Markets[bread].LastDemand, r.Markets[meat].LastDemand);
        }
        var onCalm = Probe(1.0, false); var onSpike = Probe(1.0, true);
        var offCalm = Probe(0.0, false); var offSpike = Probe(0.0, true);
        Assert.True(onSpike.breadD > onCalm.breadD * 1.001,
            $"ゲートONで肉高騰なのにパンの需要が増えない（平常 {onCalm.breadD:F1} / 高騰 {onSpike.breadD:F1}）＝ギッフェンが創発していない");
        Assert.True(offSpike.breadD < offCalm.breadD * 1.0001,
            $"ゲートOFFなのに肉高騰がパンへ波及した（平常 {offCalm.breadD:F1} / 高騰 {offSpike.breadD:F1}）＝恒等の破れ");
        // ⚠肉自身の需要は検証しない：Markets[].LastDemand はコア需要の合計＝缶詰工場の投入需要が支配的で（実測＝価格×5でも329.7で不変）、
        //   POPの食卓分の減少が埋もれる。ギッフェンの証拠は上の2本（パンへの波及＋OFF恒等）で足りる。
        _ = onSpike.meatD; _ = onCalm.meatD;
    }

    [Fact]
    public void 段0_主食の必要量は価格に反応しなくなるが交差の乗り換えは残る()
    {
        var off = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.France().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);
        off.World.Coef.StapleElasticityZero = 0;    // ★2026-08-31点火（既定1.0）：OFF側を明示ピン＝明示しないと対照の番人が空洞化する
        on.World.Coef.StapleElasticityZero = 1.0;
        const int bread = 62;
        foreach (var sim in new[] { off, on })
        {
            var r = sim.World.Region;
            foreach (var g in r.Goods) if (g.Tier == GoodTier.Staple) r.Markets[g.Id].Price *= 3.0;   // 主食が一斉高騰（交差の相対価格は不変＝自己弾力だけを単離）
            EconomyStep.Run(r, sim.World.Coef);
        }
        var ro = off.World.Region; var rn = on.World.Region;
        // OFF＝(rp/3rp)^0.3 ≈ 0.72 で需要が縮む／ON＝^0 ＝縮まない（買えるかは予算が決める）
        Assert.True(rn.Markets[bread].LastDemand > ro.Markets[bread].LastDemand * 1.05,
            $"主食一斉高騰でON側の必要量が縮んでいる（OFF {ro.Markets[bread].LastDemand:F1} / ON {rn.Markets[bread].LastDemand:F1}）＝符号の是正が効いていない");
    }
}
