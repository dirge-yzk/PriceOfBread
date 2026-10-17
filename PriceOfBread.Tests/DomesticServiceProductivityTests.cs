using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(306) 線(2)：使用人の1人あたりの産出の倍率 <c>DomesticServiceProductivity</c> の番人。作者 2026-10-12 の設計 (2) の梃子。
/// 倍率は毎月の能力（EconomyStep の boost）に届き、構築時の生産性（Producer.Productivity）と他の役務は動かさない。期待値は倍率そのもの（コードの式を呼ばない）。
/// 事前登録＝measurements/sv_20261013_prereg.txt。突然変異（実装を一時的に壊して赤を確かめる）：boost の係数の読みを 1.0 の直書きに／教育など家事奉仕以外の役務にも掛ける。</summary>
public class DomesticServiceProductivityTests
{
    [Fact]
    public void 使用人の産出の係数は毎月の能力に届き倍率どおりに変わる_構築時の生産性は1のまま()
    {
        var cfgs = new[] { NationConfig.Britain(), NationConfig.France() };
        var def = new MultiNationSim(cfgs, seed: 1789);
        var half = new MultiNationSim(cfgs, seed: 1789);
        foreach (var nat in def.Nations) nat.World.Coef.DomesticServiceProductivity = 1.0;   // ★(388) 既定が 0.30 になった＝比べる基線の倍率 1.0 を明示する（旧の既定と同じ）
        foreach (var nat in half.Nations) nat.World.Coef.DomesticServiceProductivity = 0.5;   // シムを作った後の書き換え（POB_COEF と同じ届き方）
        int svc = Catalog.DomesticServiceId;
        foreach (var s in new[] { def, half }) foreach (var nat in s.Nations) nat.TelemetryEnabled = false;
        int n = 0;
        foreach (var s in new[] { def, half })
            foreach (var nat in s.Nations)
                foreach (var r in nat.World.Regions.Where(r => r.State == RegionState.Active))
                    foreach (var p in r.Producers.Where(p => p.GoodId == svc)) { Assert.Equal(1.0, p.Productivity); n++; }   // 生産性そのものは動かさない（レバーは能力の boost）
        Assert.True(n >= 4, $"前提が崩れた：家事奉仕の生産者が {n} 本しか見つからない（主張が空洞）");
        def.StepQuarter(); half.StepQuarter();
        double capD = 0, capH = 0, otherD = 0, otherH = 0;
        for (int i = 0; i < cfgs.Length; i++)
        {
            foreach (var r in def.Nations[i].World.Regions.Where(r => r.State == RegionState.Active))
            { capD += r.Producers.Where(p => p.GoodId == svc).Sum(p => p.Capacity); otherD += r.Producers.Where(p => p.GoodId == Catalog.EducationId).Sum(p => p.Capacity); }
            foreach (var r in half.Nations[i].World.Regions.Where(r => r.State == RegionState.Active))
            { capH += r.Producers.Where(p => p.GoodId == svc).Sum(p => p.Capacity); otherH += r.Producers.Where(p => p.GoodId == Catalog.EducationId).Sum(p => p.Capacity); }
        }
        Assert.True(capD > 0 && otherD > 0);
        Assert.InRange(capH / capD, 0.45, 0.55);   // 使用人の人数は 1 四半期で少し動く＝許しの幅（係数が無視されれば 1.0 になる）
        Assert.InRange(otherH / otherD, 0.9, 1.1);   // 他の役務（教育）の能力は倍率に巻き込まれない
    }

    [Fact]
    public void 既定の倍率は0_30_388で採用()
    {
        Assert.Equal(0.30, new Coefficients().DomesticServiceProductivity);
    }
}
