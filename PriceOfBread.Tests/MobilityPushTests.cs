using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// rank5 囲い込みプッシュ（社会移動リアリティ監査 2026-08-15・作者裁定 2026-08-16）のON経路検証。
/// ①地代の土地稀少弾力（RentScarcityElasticity）：土地が混んだ州ほど地主の取り分↑→農民のEarned↓＝プッシュ型離農の駆動。
/// （②囲い込みの自給侵食 EnclosureRate は 2026-10-11 (253) に削除＝2026-09-21 作者裁定で「棄却」側を正として閉じた。）
/// ⚠作者条件＝新しい移動経路・移動補助は作らない（押し出しは所得低下→既存機構経由のみ）＝この係数は所得・自給しか触らない。
/// </summary>
public class MobilityPushTests
{
    [Fact]
    public void 既定係数が方針どおり()   // RentScarcityElasticity=0（★(529) 2026-10-24 に 1.0→0＝産地の取り分は慣習のデータの取り分と法の上限の小さい方・弾力の項は読まない）
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.RentScarcityElasticity);
    }

    [Fact]
    public void ON経路_土地が混んだ州では農民所得が下がり地主の地代が増える()   // rank5主係数：実効RentShare=RentShare×(1+弾力×稀少度)。★(529) 既定の弾力は 0＝この番人は弾力の項の ON 経路そのものを見る
    {
        // 双子シム（同シード＝決定論で同一状態）：ON側だけ弾力を点火＝差分が機構の効果そのもの。
        // ⚠ロシア＝農奴解放前（FarmRentCap=1.0）を使う。英は農奴解放済みで cap=0.5＝稀少でも貢租を上げられない
        //（法上限がclampの物差しという設計そのもの＝英で試すとOFF/ONがビット恒等になることを実測済み）。
        var off = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        off.Run(20); on.Run(20);   // 経済を落ち着かせる
        off.World.Coef.RentScarcityElasticity = 0;    // OFF側を明示ピン（既定 0＝(529)）
        on.World.Coef.RentScarcityElasticity = 0.6;   // 稀少度1.0で実効RentShare 0.5→0.8（FarmRentCap=1.0の内側）
        foreach (var sim in new[] { off, on })
        {
            var r = sim.World.Region;
            double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
            r.ArableLand = rural / 2.0;   // 農村人口/耕地=2 → 稀少度=max(0,2−1)=1.0（双子とも同条件＝混んだ州を直接セット）
            foreach (var pr in r.Producers) if (pr.IsRgo) { pr.RentShare = new Share(0.5); pr.OwnerPopIndex = 0; }   // 地主制を明示（所有=貴族Pops[0]）＝決定論
            EconomyStep.Run(r, sim.World.Coef);   // 1ヶ月だけ直接呼ぶ＝所得の分配式を単離（価格・産出は§④より上流で双子恒等）
        }
        var ro = off.World.Region; var rn = on.World.Region;
        Assert.True(rn.Pops[1].EarnedPerMonth < ro.Pops[1].EarnedPerMonth,
            $"土地が混んだ州で農民所得が下がらない（OFF {ro.Pops[1].EarnedPerMonth:F1} / ON {rn.Pops[1].EarnedPerMonth:F1}）");
        Assert.True(rn.Pops[0].LandRentPerMonth > ro.Pops[0].LandRentPerMonth,
            $"土地が混んだ州で地主のLandRentが増えない（OFF {ro.Pops[0].LandRentPerMonth:F1} / ON {rn.Pops[0].LandRentPerMonth:F1}）");
    }
    [Fact]
    public void 既定では産地の取り分は土地の混み具合に依らず慣習の取り分と法の上限の小さい方()   // ★(529) 既定の弾力は 0＝混んだ州でも取り分は動かない（旧〔LandRentResidual=1〕の期間も同じ値）
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(41), new NullAgent());
        sim.Run(20);
        var r = sim.World.Region; var coef = sim.World.Coef;
        Assert.Equal(0.0, new Coefficients().RentScarcityElasticity);
        double rural = 0; foreach (var p in r.Pops) if (!p.IsUrban) rural += p.Size;
        r.ArableLand = rural / 2.0;   // 農村人口/耕地=2（稀少度 1.0）
        foreach (var pr in r.Producers) if (pr.IsRgo) { pr.RentShare = new Share(0.5); }
        r.FarmRentCap = new Share(1.0);
        foreach (var pr in r.Producers) if (pr.IsRgo) Assert.Equal(0.5, EconomyStep.RentShareOf(pr, r, coef, rural), 12);
        r.FarmRentCap = new Share(0.3);
        foreach (var pr in r.Producers) if (pr.IsRgo) Assert.Equal(0.3, EconomyStep.RentShareOf(pr, r, coef, rural), 12);
    }
}
