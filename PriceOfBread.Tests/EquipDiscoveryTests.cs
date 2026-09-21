using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>装備の発見（作者裁定2026-09-08「機関銃陣地を発見するまで、理想の軍隊に機関銃を入れない」）の番人。
/// 休眠（EquipNeedsDiscovery=0）では原単位表が**同じ配列そのもの**であること、点火すると
/// 未解禁の装備が必要量・見積から抜け、解禁後は戻ることを固定する。</summary>
public class EquipDiscoveryTests
{
    private static Simulation Build(Coefficients coef, ulong seed)
    {
        var sim = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France, Coef = coef }, new NullAgent());
        sim.Run(24);
        return sim;
    }

    [Fact]
    public void 既定は休眠_原単位表は同じ配列のまま()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.EquipNeedsDiscovery);
        var sim = Build(c, 31);
        // 同一参照＝1命令も実行していない（掛け算すら起きない）
        Assert.Same(c.MilitaryRates, EconomyStep.DiscoveredRates(sim.World.Region, c, c.MilitaryGoods, c.MilitaryRates));
        Assert.Same(c.MilitaryQualityRates, EconomyStep.DiscoveredRates(sim.World.Region, c, c.MilitaryQualityGoods, c.MilitaryQualityRates));
    }

    [Fact]
    public void 未解禁の装備は必要量に入らない_解禁後は入る()
    {
        var c = new Coefficients { EquipNeedsDiscovery = 1.0 };
        var sim = Build(c, 32);
        var r = sim.World.Region;
        int iShip = System.Array.IndexOf(c.MilitaryGoods, Catalog.SteamshipId);   // 汽船＝既定Producerに TechOnset=0.5 が焼かれている
        int iRifle = System.Array.IndexOf(c.MilitaryGoods, 82);                   // 小銃＝1783年から作れる（onset 0）
        Assert.True(iShip >= 0 && iRifle >= 0);

        // 1783年の技術水準（助走直後）＝汽船は未発見
        Assert.True(r.TechLevel < 0.5, $"前提が崩れている（初期技術 {r.TechLevel:F3}）");
        var early = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates);
        Assert.Equal(0.0, early[iShip]);
        Assert.Equal(c.MilitaryRates[iRifle], early[iRifle]);   // 解禁済みの装備は1ビットも変わらない
        Assert.Equal(0.0, EconomyStep.EquipUnlock(r, c, Catalog.SteamshipId));

        // 発見後（技術水準を解禁の帯の上へ）＝理想の軍隊に戻る
        r.TechLevel = 3.0;
        var late = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates);
        Assert.Equal(c.MilitaryRates[iShip], late[iShip]);
        Assert.Equal(1.0, EconomyStep.EquipUnlock(r, c, Catalog.SteamshipId));
    }

    [Fact]
    public void 解禁は連続_崖を作らない()
    {
        var c = new Coefficients { EquipNeedsDiscovery = 1.0 };
        var sim = Build(c, 33);
        var r = sim.World.Region;
        r.TechLevel = 0.5 + 0.5;   // 汽船の onset 0.5 ＋ 解禁幅の半ば
        double half = EconomyStep.EquipUnlock(r, c, Catalog.SteamshipId);
        Assert.True(half > 0 && half < 1, $"部分解禁が0か1に潰れている（{half:F3}）＝崖ができている");
    }

    /// <summary>兵士POPが立つのは多国世界（MilitaryScale の書き手＝Simulation の軍需調達）なので、見積・充足の番人だけは実世界の首都で見る。
    /// ★機構の層（2026-09-18 書き直し）：**同じ盤面の双子**を作り、片方にだけ休眠の規則を当てて**月次決算1回**だけ進める＝違うのは規則だけ。
    /// 盤面は点火の世界で作る＝未発見の装備（1783年の汽船・自動車）は武器庫に0で載る（それが物理＝まだ誰も作れない）。
    /// ⚠旧版は OFF と ON を**別々に**助走20＋4四半期回し、2本の軌道の末端を比べていた（創発の層）。probe 実測（清・首都）で
    ///   符号を決めていたのは発見の係数ではなく、軌道ごとに違う軍艦の相場（OFF腕 440→56）→海のコストシェア（FleetCostShare）
    ///   →陸海の再配分 landMul →耐久品の目標在庫（BaseArmsTarget）だった＝武器庫の実量はほぼ同じ（小銃 9966→9927）なのに
    ///   小銃の充足が 0.177→0.248 と動き、所得税の天井を廃止した 2026-09-12 に OFF 腕だけ 0.1191→0.1466（ON は 0.1392→0.1398）で赤になった。
    ///   配給率は天井の有無に関わらず既に0近傍（小銃 0.002／0.003）＝「天井廃止で発注が供給を上回った」という診断は誤り。
    ///   同日 23:25 の「軍需財の市場在庫を毎四半期 1e9 へ」の栓は、財を無から湧かせて価格を潰し予算充足を1超へ張り付かせる
    ///   別の世界（未発明の汽船まで142隻買えていた）で符号を戻していただけ＝機構を測っていないので撤去した。</summary>
    private static MultiNationSim IgnitedBoard()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 4242, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.EquipNeedsDiscovery = 1.0;   // ⚠Coef は国ごと別インスタンス
        sim.StepQuarter();   // 兵士POPが立ち、軍需調達が一巡する
        sim.StepDay();       // 次の四半期首（戦略の前段）まで踏む＝分岐後の1か月に AI の四半期決定が挟まらない
        return sim;
    }

    [Fact]
    public void 点火すると軍需の見積が減り装備充足が上がる()
    {
        var on = IgnitedBoard();
        var off = IgnitedBoard();
        int i = 0;
        for (int k = 1; k < on.Nations.Count; k++)
            if (on.Nations[k].World.Region.MilitaryScale > on.Nations[i].World.Region.MilitaryScale) i = k;
        Region capOn = on.Nations[i].World.Region, capOff = off.Nations[i].World.Region;
        Coefficients cOn = on.Nations[i].World.Coef, cOff = off.Nations[i].World.Coef;
        Assert.True(capOn.MilitaryScale > 0, "前提＝軍需調達が走っていない");
        // 同じ入力＝分岐前の双子はビット一致
        Assert.Equal(capOn.MilitaryScale, capOff.MilitaryScale);
        Assert.Equal(capOn.MilitaryEquipFill, capOff.MilitaryEquipFill);
        // 前提＝未発見の装備が実在し、武器庫に無い
        Assert.Equal(0.0, EconomyStep.EquipUnlock(capOn, cOn, Catalog.SteamshipId));
        Assert.True(capOn.ArmsStock is { } st && st.TryGetValue(Catalog.SteamshipId, out double heldShip) && heldShip == 0,
            "前提が崩れた：点火の世界の武器庫に未発見の汽船が0で載っていない");

        foreach (var n in off.Nations) n.World.Coef.EquipNeedsDiscovery = 0.0;   // 片方にだけ休眠の規則（盤面は同一）

        // (1) 見積＝純関数・同じ盤面
        double needOn = EconomyStep.MilitaryProcurementNeed(capOn, cOn);
        double needOff = EconomyStep.MilitaryProcurementNeed(capOff, cOff);
        Assert.True(needOn > 0, $"発見済みの装備まで見積から消えた（ON {needOn:F1}）");
        Assert.True(needOn < needOff, $"未発見の装備が見積から抜けていない（OFF {needOff:F1} / ON {needOn:F1}）");

        // (2) 同じ盤面から月次決算1回＝未発見の装備は必要量（分母）に入らない＝持っていないことが欠乏に数えられない
        for (int d = 0; d < Calendar.DaysPerMonth; d++) on.StepDay();
        for (int d = 0; d < Calendar.DaysPerMonth; d++) off.StepDay();
        Assert.True(capOn.MilitaryEquipFill > capOff.MilitaryEquipFill,
            $"装備充足が上がっていない（{on.Nations[i].World.SelfPower?.Name} OFF {capOff.MilitaryEquipFill:F4} / ON {capOn.MilitaryEquipFill:F4}）");
    }
}
