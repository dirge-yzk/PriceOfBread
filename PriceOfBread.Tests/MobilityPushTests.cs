using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// rank5 囲い込みプッシュ（社会移動リアリティ監査 2026-08-15・作者裁定 2026-08-16・全て休眠）のON経路検証。
/// ①地代の土地稀少弾力（RentScarcityElasticity）：土地が混んだ州ほど地主の取り分↑→農民のEarned↓＝プッシュ型離農の駆動。
/// ②囲い込みの自給侵食（EnclosureRate）：RGOの資本拡張1単位あたり当該州農民の Subsistence 微減。
/// ⚠作者条件＝新しい移動経路・移動補助は作らない（押し出しは所得低下→既存機構経由のみ）＝この2係数は所得・自給しか触らない。
/// </summary>
public class MobilityPushTests
{
    [Fact]
    public void 既定係数が方針どおり()   // RentScarcityElasticity=1.0（★2026-08-31点火＝ラウンドA）・EnclosureRateは休眠のまま
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.RentScarcityElasticity);
        Assert.Equal(0.0, c.EnclosureRate);
    }

    [Fact]
    public void ON経路_土地が混んだ州では農民所得が下がり地主の地代が増える()   // rank5主係数：実効RentShare=RentShare×(1+弾力×稀少度)
    {
        // 双子シム（同シード＝決定論で同一状態）：ON側だけ弾力を点火＝差分が機構の効果そのもの。
        // ⚠ロシア＝農奴解放前（FarmRentCap=1.0）を使う。英は農奴解放済みで cap=0.5＝稀少でも貢租を上げられない
        //（法上限がclampの物差しという設計そのもの＝英で試すとOFF/ONがビット恒等になることを実測済み）。
        var off = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);   // 経済を落ち着かせる
        off.World.Coef.RentScarcityElasticity = 0;    // ★点火2026-08-31：OFF側を明示ピン（既定1.0＝明示しないと対照の番人が空洞化する）
        on.World.Coef.RentScarcityElasticity = 0.6;   // 稀少度1.0で実効RentShare 0.5→0.8（FarmRentCap=1.0の内側）
        foreach (var sim in new[] { off, on })
        {
            var r = sim.World.Region;
            double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
            r.ArableLand = rural / 2.0;   // 農村人口/耕地=2 → 稀少度=max(0,2−1)=1.0（双子とも同条件＝混んだ州を直接セット）
            foreach (var pr in r.Producers) if (pr.IsRgo) { pr.RentShare = 0.5; pr.OwnerPopIndex = 0; }   // 地主制を明示（所有=貴族Pops[0]）＝決定論
            EconomyStep.Run(r, sim.World.Coef);   // 1ヶ月だけ直接呼ぶ＝所得の分配式を単離（価格・産出は§④より上流で双子恒等）
        }
        var ro = off.World.Region; var rn = on.World.Region;
        Assert.True(rn.Pops[1].Earned < ro.Pops[1].Earned,
            $"土地が混んだ州で農民所得が下がらない（OFF {ro.Pops[1].Earned:F1} / ON {rn.Pops[1].Earned:F1}）");
        Assert.True(rn.Pops[0].LandRent > ro.Pops[0].LandRent,
            $"土地が混んだ州で地主のLandRentが増えない（OFF {ro.Pops[0].LandRent:F1} / ON {rn.Pops[0].LandRent:F1}）");
    }

    [Fact]
    public void ON経路_RGOの資本拡張は当該州農民の自給を侵食し既定0では恒等()   // rank5第2段：EnclosureRate
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(20);
        var r = sim.World.Region;
        var rgo = r.Producers.Find(p => p.IsRgo);
        Assert.NotNull(rgo);
        var farm = r.Pops[rgo!.LaborPopIndex];   // RGOの労働pop＝農民（Subsistence=0.7で開始）
        double sub0 = farm.Subsistence;
        Assert.True(sub0 > 0, "前提が崩れた：農民に自給がない");
        EconomyStep.ApplyEnclosure(r, rgo, 1.0, sim.World.Coef);   // 既定 EnclosureRate=0 ＝1ビットも動かない
        Assert.Equal(sub0, farm.Subsistence);
        sim.World.Coef.EnclosureRate = 0.1;
        EconomyStep.ApplyEnclosure(r, rgo, 1.0, sim.World.Coef);   // 資本拡張1単位＝自給0.1侵食（囲い込み＝自給地の市場向け転換）
        Assert.Equal(sub0 - 0.1, farm.Subsistence, 12);
        farm.Subsistence = 0.05;
        EconomyStep.ApplyEnclosure(r, rgo, 1.0, sim.World.Coef);   // 下限0クランプ＝自給は負にならない
        Assert.Equal(0.0, farm.Subsistence);
    }
}
