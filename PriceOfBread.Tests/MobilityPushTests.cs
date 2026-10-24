using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 産地の取り分は土地の混み具合に依らない（社会移動リアリティ監査 2026-08-15 の rank5 囲い込みプッシュは、地代の土地稀少弾力 RentScarcityElasticity として
/// 2026-08-31 に点火し、(529) で既定 0＝休眠にし、(541) で消した＝取り分は min(慣習のデータの取り分, 州の法の上限) の 1 本）。
/// （囲い込みの自給侵食 EnclosureRate は 2026-10-11 (253) に削除＝2026-09-21 作者裁定で「棄却」側を正として閉じた。）
/// ⚠作者条件＝新しい移動経路・移動補助は作らない（押し出しは所得低下→既存機構経由のみ）。
/// </summary>
public class MobilityPushTests
{
    [Fact]
    public void 産地の取り分は土地の混み具合に依らず慣習の取り分と法の上限の小さい方()   // ★(529)(541) 混んだ州でも取り分は動かない
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        sim.Run(20);
        var r = sim.World.Region; var coef = sim.World.Coef;
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        r.ArableLand = rural / 2.0;   // 農村人口/耕地=2（混んだ州）
        foreach (var pr in r.Producers) if (pr.IsRgo) { pr.RentShare = new Share(0.5); }
        r.FarmRentCap = new Share(1.0);
        foreach (var pr in r.Producers) if (pr.IsRgo) Assert.Equal(0.5, EconomyStep.RentShareOf(pr, r, coef), 12);
        r.FarmRentCap = new Share(0.3);
        foreach (var pr in r.Producers) if (pr.IsRgo) Assert.Equal(0.3, EconomyStep.RentShareOf(pr, r, coef), 12);
        r.ArableLand = rural / 8.0;   // もっと混んでも同じ
        foreach (var pr in r.Producers) if (pr.IsRgo) Assert.Equal(0.3, EconomyStep.RentShareOf(pr, r, coef), 12);
    }
}
