using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>装備の発見（作者裁定2026-09-08「機関銃陣地を発見するまで、理想の軍隊に機関銃を入れない」）の番人。
/// 旗 EquipNeedsDiscovery は 2026-09-27 回2-1 で点火して削除＝原単位表は常に「原単位×解禁度」（EconomyStep.DiscoveredRates）。
/// 未解禁の装備が必要量・見積・武器庫の目標から抜け、解禁後は戻ること、「要らない」と「持っている」を区別することを固定する。</summary>
public class EquipDiscoveryTests
{
    private const int Car = Catalog.AutomobileId;   // 自動車（輜重）＝DefaultRecipeOnset 1.50

    private static Simulation Build(ulong seed)
    {
        var sim = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent());
        sim.Run(24);
        return sim;
    }

    [Fact]
    public void 未解禁の装備は必要量に入らない_解禁後は入る()
    {
        var sim = Build(32);
        var r = sim.World.Region; var c = sim.World.Coef;
        int iShip = System.Array.IndexOf(c.MilitaryGoods, Catalog.SteamshipId);   // 汽船＝既定Producerに TechOnset=0.5 が焼かれている
        int iCar = System.Array.IndexOf(c.MilitaryGoods, Car);
        int iRifle = System.Array.IndexOf(c.MilitaryGoods, Catalog.RifleId);                   // 小銃＝1783年から作れる（onset 0）
        Assert.True(iShip >= 0 && iCar >= 0 && iRifle >= 0);

        // 1783年の技術水準（助走直後）＝汽船も自動車も未発見
        Assert.True(r.TechLevel < 0.5, $"前提が崩れている（初期技術 {r.TechLevel:F3}）");
        var early = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates);
        Assert.Equal(0.0, early[iShip]);
        Assert.Equal(0.0, early[iCar]);
        Assert.Equal(c.MilitaryRates[iRifle], early[iRifle]);   // 解禁済みの装備は1ビットも変わらない
        Assert.Equal(0.0, EconomyStep.EquipUnlock(r, c, Catalog.SteamshipId));

        // 発見後（技術水準を解禁の帯の上へ）＝理想の軍隊に戻る＝素の表と一致
        r.TechLevel = 3.0;
        var late = EconomyStep.DiscoveredRates(r, c, c.MilitaryGoods, c.MilitaryRates);
        Assert.Equal(c.MilitaryRates, late);
        Assert.Equal(1.0, EconomyStep.EquipUnlock(r, c, Catalog.SteamshipId));
    }

    [Fact]
    public void 汽船と機関車の解禁は植民地格の州にも焼かれる()
    {
        // ★2026-09-27（回4-3 の再挑戦）：旧＝汽船(85) onset0.5・機関車(88) onset0.6 をコンストラクタが「構築時の州」だけに焼いていた
        //   ＝植民地格の州（PromoteColonyToRegion＝開幕の植民地・講和で奪った植民地）は onset0 のまま1783年から作れた（seed 1 の開幕で77州）。
        //   州の構築経路（AddAlternateRecipes）へ移した＝どの州も同じ解禁。
        var sim = new MultiNationSim(WorldData.Nations(), 1, _ => new UtilityAgent());
        int colonial = 0, bad = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.Tier == RegionTier.Colonial) colonial++;
                foreach (var pr in r.Producers)
                    if ((pr.GoodId == Catalog.SteamshipId && !(pr.TechOnset >= 0.5)) || (pr.GoodId == Catalog.LocomotiveId && !(pr.TechOnset >= 0.6))) bad++;
            }
        Assert.True(colonial > 0, "植民地格の州が無い世界ではテストにならない");
        Assert.Equal(0, bad);
    }

    [Fact]
    public void 解禁は連続_崖を作らない()
    {
        var sim = Build(33);
        var r = sim.World.Region;
        r.TechLevel = 0.5 + 0.5;   // 汽船の onset 0.5 ＋ 解禁幅の半ば
        double half = EconomyStep.EquipUnlock(r, sim.World.Coef, Catalog.SteamshipId);
        Assert.True(half > 0 && half < 1, $"部分解禁が0か1に潰れている（{half:F3}）＝崖ができている");
    }

    /// <summary>兵士POPが立つのは多国世界（MilitaryScale の書き手＝Simulation の軍需調達）なので、見積・充足の番人は実世界の首都で見る。
    /// 1四半期で兵士POPが立ち軍需調達が一巡し、次の四半期首（戦略の前段）まで1日踏む。同じ種の2枚は**ビット一致**（双子）。</summary>
    private static MultiNationSim Board()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 4242, _ => new UtilityAgent());
        sim.StepQuarter();
        sim.StepDay();
        return sim;
    }

    private static int LargestArmy(MultiNationSim sim)
    {
        int i = 0;
        for (int k = 1; k < sim.Nations.Count; k++)
            if (sim.Nations[k].World.Region.MilitaryScale > sim.Nations[i].World.Region.MilitaryScale) i = k;
        return i;
    }

    /// <summary>その州の技術で解禁度が0の財の原単位を0にした素の表（＝「要らない」を**表で**言った世界）。</summary>
    private static double[] ZeroUndiscovered(Region r, Coefficients c, int[] goods, double[] rates)
    {
        var z = (double[])rates.Clone();
        for (int b = 0; b < goods.Length; b++) if (EconomyStep.EquipUnlock(r, c, goods[b]) == 0.0) z[b] = 0.0;
        return z;
    }

    /// <summary>★番人の穴を埋める（00一覧 2026-09-18 ⑩①）＝「要らない」と「持っている」を区別する。
    /// 双子の盤面の片方（B）だけ、1783年に未発見の基本段の装備（汽船・自動車）の原単位を**素の表で0**にする＝「要らない」と表が言う世界。
    /// 同じ盤面から同じ月次決算を1回だけ進め、装備の充足が**ビット一致**すること＝未発見の装備は分母（必要量）にも分子（持っている量）にも入らない。
    /// ⚠壊し方＝「必要量だけ素の表・在庫の目標は発見後の表」（決済の want を素の表に戻す）にすると、目標0の財は ArmsFillContribution が
    ///   want×1.0 を返す＝未発見の汽船を**満額持っている**と数え、分母と分子に同じ量が乗って充足が上がる＝この等式が割れる（旧番人は符号しか見ず緑のままだった）。
    /// ⚠逆の壊し方＝「目標在庫だけ素の表」は、武器庫に未発見の装備が満額で積まれる＝武器庫0の等式が割れる。</summary>
    [Fact]
    public void 未発見の装備は武器庫0で_装備充足の分母にも分子にも入らない()
    {
        var a = Board();
        var b = Board();
        int i = LargestArmy(a);
        var wa = a.Nations[i].World; var wb = b.Nations[i].World;
        Region ra = wa.Region, rb = wb.Region;
        Coefficients ca = wa.Coef, cb = wb.Coef;
        Assert.True(ra.MilitaryScale > 0, "前提＝軍需調達が走っていない");
        Assert.Equal(ra.MilitaryScale, rb.MilitaryScale);   // 双子はビット一致
        Assert.Equal(ra.MilitaryEquipFill, rb.MilitaryEquipFill);

        // 前提＝1783年に未発見の基本段の装備が実在する（汽船・自動車）
        Assert.Equal(0.0, EconomyStep.EquipUnlock(ra, ca, Catalog.SteamshipId));
        Assert.Equal(0.0, EconomyStep.EquipUnlock(ra, ca, Car));
        // 武器庫は0（未発見の装備は目標0＝満額で持って始まらない）
        Assert.NotNull(ra.ArmsStock);
        Assert.Equal(0.0, ra.ArmsStock!.GetValueOrDefault(Catalog.SteamshipId));
        Assert.Equal(0.0, ra.ArmsStock.GetValueOrDefault(Car));

        cb.MilitaryRates = ZeroUndiscovered(rb, cb, cb.MilitaryGoods, cb.MilitaryRates);   // B だけ「要らない」を表で言う（新しい配列＝A の表は触らない）
        Assert.NotEqual(ca.MilitaryRates, cb.MilitaryRates);

        EconomyStep.Run(ra, ca, 1.0, wa.Bank, wa);
        EconomyStep.Run(rb, cb, 1.0, wb.Bank, wb);

        // 前提＝発見済みの装備が満ちていない（満ちていれば「持っている」と「要らない」が同じ値になり区別できない）
        Assert.True(ra.MilitaryEquipFill < 1.0, $"前提が崩れた：装備が満ちている（{ra.MilitaryEquipFill:F4}）＝この盤面では区別を測れない");
        Assert.Equal(rb.MilitaryEquipFill, ra.MilitaryEquipFill);
        Assert.Equal(0.0, ra.ArmsStock.GetValueOrDefault(Catalog.SteamshipId));
        Assert.Equal(0.0, ra.ArmsStock.GetValueOrDefault(Car));
    }

    /// <summary>同じ州で技術を下げると、必要額（MilitaryProcurementNeed＝財政の軍事費の請求の上限と軍需予算の上限が読む見積）が
    /// **未発見の装備の分だけ**減る（上の段の弩級戦艦・戦闘機も含めて＝4段すべてが同じ表を通る）。純関数＝同じ盤面で技術と表だけを替える。</summary>
    [Fact]
    public void 技術を下げると必要額が未発見の装備の分だけ減る()
    {
        var sim = Board();
        int i = LargestArmy(sim);
        var r = sim.Nations[i].World.Region; var c = sim.Nations[i].World.Coef;
        Assert.True(r.MilitaryScale > 0, "前提＝軍需調達が走っていない");
        double techLow = r.TechLevel;
        var tiers = new[] { c.MilitaryGoods, c.MilitaryQualityGoods, c.MilitaryTier3Goods, c.MilitaryAirGoods };
        // 前提＝1783年の解禁度は0か1だけ（部分解禁が無い）＝「未発見の装備の分」がはっきり決まる
        foreach (var goods in tiers)
            foreach (int g in goods)
            {
                double u = EconomyStep.EquipUnlock(r, c, g);
                Assert.True(u == 0.0 || u == 1.0, $"前提が崩れた：1783年に部分解禁の装備 {g}（{u:F3}）");
            }
        foreach (int g in new[] { Catalog.SteamshipId, Car, Catalog.DreadnoughtId, Catalog.MilitaryAircraftId })   // 汽船・自動車・弩級戦艦・軍用機（★段1：飛行機と戦闘機を畳んだ）
            Assert.Equal(0.0, EconomyStep.EquipUnlock(r, c, g));

        double needLow = EconomyStep.MilitaryProcurementNeed(r, c);
        double[] z0 = ZeroUndiscovered(r, c, c.MilitaryGoods, c.MilitaryRates), z1 = ZeroUndiscovered(r, c, c.MilitaryQualityGoods, c.MilitaryQualityRates),
                 z2 = ZeroUndiscovered(r, c, c.MilitaryTier3Goods, c.MilitaryTier3Rates), z3 = ZeroUndiscovered(r, c, c.MilitaryAirGoods, c.MilitaryAirRates);

        r.TechLevel = 10.0;   // 全ての装備が解禁
        foreach (var goods in tiers)
            foreach (int g in goods) Assert.Equal(1.0, EconomyStep.EquipUnlock(r, c, g));
        double needHigh = EconomyStep.MilitaryProcurementNeed(r, c);
        var (m0, m1, m2, m3) = (c.MilitaryRates, c.MilitaryQualityRates, c.MilitaryTier3Rates, c.MilitaryAirRates);
        (c.MilitaryRates, c.MilitaryQualityRates, c.MilitaryTier3Rates, c.MilitaryAirRates) = (z0, z1, z2, z3);
        double needHighWithoutUndiscovered = EconomyStep.MilitaryProcurementNeed(r, c);
        (c.MilitaryRates, c.MilitaryQualityRates, c.MilitaryTier3Rates, c.MilitaryAirRates) = (m0, m1, m2, m3);
        r.TechLevel = techLow;

        Assert.True(needLow > 0, $"発見済みの装備まで見積から消えた（{needLow:F1}）");
        Assert.True(needLow < needHigh, $"未発見の装備が見積から抜けていない（1783年 {needLow:F1} / 全解禁 {needHigh:F1}）");
        Assert.Equal(needHighWithoutUndiscovered, needLow);   // 減ったのは未発見の装備の分だけ（上の段を含む4段とも）
    }

    /// <summary>★処理の一元化 段3 F（2026-09-29）＝上位段・tier3 の充足は基本段と同じ1本（ArmsFillContribution）＝陸海の再配分を分母（目標）にも通す。
    /// 旧＝発注にだけ再配分を掛け、充足の分母には掛けなかった＝港の無い国の装甲艦・弩級戦艦は発注0なのに分母に残り、陸の装備が満ちても充足が1に届かなかった
    /// （E1 で基本段を是正したのと同型の非対称）。港の無い国の充足は陸の装備の充足と**ビット一致**する（海の装備は要らない＝分母にも分子にも入らない）。</summary>
    [Fact]
    public void 上位段の充足は陸海の再配分を分母にも通す_港の無い国は海の装備を分母に入れない()
    {
        var sim = Board();
        int i = -1;
        for (int k = 0; k < sim.Nations.Count && i < 0; k++)
        {
            var rk = sim.Nations[k].World.Region;
            if (!rk.NationHasPort && rk.MilitaryScale > 0 && rk.ArmsStock is not null) i = k;
        }
        Assert.True(i >= 0, "前提が崩れた：港の無い国で軍需調達が走っていない");
        var w = sim.Nations[i].World; var r = w.Region; var c = w.Coef;
        Assert.True(c.NavalProcurementNeedsPort > 0, "前提が崩れた：港の無い国も軍艦を買う");
        r.TechLevel = 10.0;   // 上位段・tier3 を解禁
        foreach (var tier in new[] { c.MilitaryQualityGoods, c.MilitaryTier3Goods })
            foreach (int g in tier)
                if (System.Array.IndexOf(c.FleetGoodIds, g) < 0) r.ArmsStock![g] = 1e9;   // 陸の装備（機関銃・戦車）は満ちている

        EconomyStep.Run(r, c, 1.0, w.Bank, w);

        Assert.Equal(1.0, r.MilitaryQualityLandFill);   // 前提＝陸は満ちている
        Assert.Equal(1.0, r.MilitaryTier3LandFill);
        Assert.Equal(r.MilitaryQualityLandFill, r.MilitaryQualityFill);   // 港の無い国の海の装備（装甲艦）は分母に入らない
        Assert.Equal(r.MilitaryTier3LandFill, r.MilitaryTier3Fill);       // 同じ（弩級戦艦）
    }
}
