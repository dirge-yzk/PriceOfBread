using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 出生配分の凍結分母の是正（休眠 FertileAbsoluteBlend=0・作者裁定 2026-08-16）のON経路検証。
/// 旧＝重み Size×fertile÷InitialFertileShare（popごと t=0 凍結）は、初期構成が偏ったpop（聖職者F=0.10）が構成正常化すると
/// 恒久出生倍率2.1倍（実測＝聖職者爆発）になる罠。新＝全pop共通の定数分母 FertilePool(標準構成)＝FertileShareStd×(1+CoupleLimitedBirths)。
/// 時間・年号は一切読まない（年代スケール禁止の裁定）・Σbirths=net の保存は既存の正規化がそのまま担う。
/// </summary>
public class DemographyBirthWeightTests
{
    [Fact]
    public void 既定係数が方針どおり休眠()   // 0=旧popごと凍結分母とビット恒等。定数は標準構成 (1−0.3子供)×0.5女性=0.35
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.FertileAbsoluteBlend);   // ★2026-08-30 点火→同日差し戻し（幕府転換テスト赤の単独犯・英人口縮小化）＝再点火は出生率再較正とセット
        Assert.Equal(0.35, c.FertileShareStd);
    }

    [Fact]
    public void ON経路_共通分母では女性比0_1のpopの出生配分が規模比例より明確に小さい()
    {
        // A/B（同シード）：OFF＝旧凍結分母（聖職者は t=0 捕捉で重み≈Size＝ほぼ規模比例）／ON＝共通定数分母
        //（聖職者 F=0.10 は fertile=0.7×min側律速≈0.14 → 重み≈0.2×Size＝規模比例より明確に小さい）。
        var off = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var on = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        on.World.Coef.FertileAbsoluteBlend = 1.0;   // 開始前に点火＝凍結分母を一度も読まない側
        double p0 = on.World.TotalPopulation;
        var ro = off.World.Region; var rn = on.World.Region;
        // 標準構成（農民 F=0.5・子供0.3）は重み=Size のまま＝配分を奪われる側ではない（偏構成popの重みが落ちる分だけ
        // 配分シェアはむしろ増える）。「奪われない」の片側だけを固定する。
        // ⚠計器（2026-09-27 回2-1）：**1四半期**（同じ盤面から規則だけ違う1歩＝機構の層）で見る。旧＝10年後の大きさで比べていたが、
        //   OFF と ON は別の軌道＝10年のうちに片方だけ危機が起きると総人口が割れ（前期の軍需レシピの点火後は q30 で 15,012 対 14,378）、
        //   大きさの比が配分の規則でなく軌道の分岐を測っていた（q20 までの比は点火の前後で同じ 1.022〜1.026）。
        off.Run(1); on.Run(1);
        Assert.True(rn.Pops[1].Size > ro.Pops[1].Size,
            $"標準構成popが共通分母で出生配分を失った（1四半期 OFF {ro.Pops[1].Size:F2} / ON {rn.Pops[1].Size:F2}）＝物差しがずれている");
        off.Run(39); on.Run(39);   // 10年＝配分差が規模に積もる
        Assert.True(rn.Pops[6].Size < ro.Pops[6].Size * 0.99,
            $"女性比0.1の聖職者の出生配分が規模比例（旧凍結分母）より小さくならない（OFF {ro.Pops[6].Size:F1} / ON {rn.Pops[6].Size:F1}）");
        // 人口保存：配分の重みが変わるだけ＝Σbirths−Σdeaths=net（ロジスティック軌道）は既存の正規化で不変
        Assert.True(System.Math.Abs(on.World.TotalPopulation - (p0 + on.World.PopulationDelta)) < 1e-6 * p0,
            "FertileAbsoluteBlend=1 で人口保存（Σbirths=net）が破れた");
    }

    [Fact]
    public void ON経路_fertileほぼ0のpopは共通分母でも重みが膨張しない()   // FertileShareFloor整合：新形はフロア不要＝分子側で自然に安全
    {
        // 旧経路の膨張バグ＝捕捉時 fertile≈0 のpopで分母が極小→重みが桁違いに膨張（FertileShareFloorで防御）。
        // 新形＝分母は全pop共通の定数なので、fertile≈0 は分子側で重み≈0 ＝出生がほぼ配分されない（安全側）。
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.World.Coef.FertileAbsoluteBlend = 1.0;
        var r = sim.World.Region;
        var clergy = r.Pops[6];
        clergy.FemaleShare = 0.0;   // 出産可能プール≈0 の退化構成を直接セット（couple=0・female=0 → fertile=0）
        double size0 = clergy.Size;
        double p0 = sim.World.TotalPopulation;
        sim.Run(8);   // 2年（出生配分が毎月走る）
        double crude = sim.World.Coef.CrudeBirthRate / 12.0 * 24.0;   // 同期間の規模比例なら積もる粗出生の目安（≈7%）
        Assert.True(clergy.Size < size0 * (1.0 + 0.5 * crude),
            $"fertile≈0 のpopに出生が集中した（{size0:F1}→{clergy.Size:F1}）＝共通分母の重みが膨張している");
        Assert.True(System.Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0,
            "退化構成popがいても人口保存が破れた");
    }
}
