using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマH（産業組織・固定資本・都市住宅）の dormant 配線検証。
/// 既定（全係数0/恒等値）は現行恒等＝較正中立（131本体が担保）。有効化時の保存則・境界と配線を確認する。
/// 住宅(housing-urban-rent-absent)は materialize済み＝goods.json 136財で HousingId=126 実在・HousingNeedPerUrban 点火中（02断線走査2026-07-28：陳腐化した「未再生成＝inert」注記を是正）。ここでは HousingFill=1.0 の中立のみ確認。
/// </summary>
public class IndustryThemeTests
{
    [Fact]
    public void テーマH_既定係数が方針どおり()   // 休眠復旧2026-07-08 第(iii)段：（組合賃金 UnionWageRiseBoost は 2026-10-11 (271) に係数ごと削除＝LaborWellbeing が常に0だった）（他はB保留/第(iv)段/前提作業待ち＝トリアージ文書）
    {
        var c = new Coefficients();
        Assert.Equal(0.1, c.InventoryAdjustGain);       // 05-B1点火（2026-07-29）＝期待導入の第一歩。0.2は放任経済テーゼ（過少消費）を消したため0.1へ減量（bisect実測）
        Assert.Equal(0.25, c.LaborSaveRate);            // 休眠復旧2026-07-09 第(iv)段採用（bisect＝挙動4テスト・較正とも無害）
        Assert.Equal(0.005, c.VintageMigrateRate);      // ヴィンテージ資本有効化2026-07-10（キュー5＝承認済み別作業項目(1)。半減期約11.6年の世代交代）
        Assert.Equal(0.5, c.VintageObsolescence);       // 同上（座礁＝0.5×techGap・gap2で全損）
        Assert.Equal(0.03, c.HousingNeedPerUrban);      // リアリティ群採用2026-07-18＝家賃(住居財追加済み)。旧0.0番人を新採用値へ
        Assert.Equal(0.5, c.HousingMigrationFriction);  // B6点火2026-07-19＝鉄筋コンクリート（住居の晩期レシピ）とセット＝過密で流入飽和・鉄筋コンクリが収容力を解放
    }

    [Fact]
    public void テーマH_全係数有効化でも保存則と境界が保たれ配線が効く()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var c = sim.World.Coef;
        c.InventoryAdjustGain = 0.3;                       // メッツラー在庫循環
        c.LaborSaveRate = 0.3; c.LaborSaveIndHalf = 1.0;  // 技術的失業
        c.VintageMigrateRate = 0.02; c.VintageObsolescence = 0.5;  // ヴィンテージ資本
        c.HousingMigrationFriction = 0.5;                 // 住宅(財はinertだが摩擦経路は走る・HousingFill=1で無効果)
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6 保存・境界が全ティック保持
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "テーマH有効化で人口保存が破れた");
        // ★(495) 点火後の既定（IndustrializationOff＝1）：機械化度・旧世代資本は旗が止める（作者 2026-10-16〈倍率は全て削除〉）＝立たないことを確かめる
        Assert.True(sim.World.Regions.All(r => !(r.Mechanization > 0)), "旗 IndustrializationOff が正なのに機械化度が立った");
        Assert.True(sim.World.Regions.All(r => !(r.OldVintageCapital > 0)), "旗 IndustrializationOff が正なのに旧世代資本が積まれた");
        // 旧の枝（旗 0・焼き込みで削除するまで残る）の配線確認：機械化度・旧世代資本が立つ（工業化が進む実国で）。保存則と境界も同じく保たれる。
        var sim0 = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        var c0 = sim0.World.Coef;
        c0.IndustrializationOff = 0; c0.RecipeEquipment = 0;
        c0.InventoryAdjustGain = 0.3; c0.LaborSaveRate = 0.3; c0.LaborSaveIndHalf = 1.0; c0.VintageMigrateRate = 0.02; c0.VintageObsolescence = 0.5; c0.HousingMigrationFriction = 0.5;
        double q0 = sim0.World.TotalPopulation;
        var ex0 = Record.Exception(() => sim0.Run(200));
        Assert.Null(ex0);
        Assert.True(Math.Abs(sim0.World.TotalPopulation - (q0 + sim0.World.PopulationDelta)) < 1e-6 * q0, "テーマH有効化（旧の枝）で人口保存が破れた");
        bool mechWired = sim0.World.Regions.Any(r => r.Mechanization > 0);
        bool vintageWired = sim0.World.Regions.Any(r => r.OldVintageCapital > 0);
        Assert.True(mechWired, "LaborSaveRate>0でも機械化度が立たない＝EffectiveQtyへの配線不全（旧の枝）");
        Assert.True(vintageWired, "VintageMigrateRate>0でも旧世代資本が積まれない＝二層化の配線不全（旧の枝）");
    }

    [Fact]
    public void 都市住宅_既定で住居充足は1_0のまま_移民摩擦が中立()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());
        sim.World.Coef.HousingNeedPerUrban = 0;   // 休眠経路の中立性を検証（既定は0.03採用済みだが、0にすると住居財inert＝HousingFillは1.0のまま＝移民摩擦中立の恒等保証）
        sim.Run(120);
        Assert.All(sim.World.Regions, r => Assert.Equal(1.0, r.HousingFill));
    }

    /// <summary>M2（2026-09-11）：機械化度は所属州の量（旧 Producer.Mechanization の写しを削除）。LaborSaveRate>0 かつ home.Mechanization>0 の労働投入は原単位より小さく、
    /// 他州（機械化0）を home に渡せば原単位のまま＝首都価格×他州の生産者（ProductionDiag／SelfPlay の目）でも所属州の機械化度で評価される。</summary>
    [Fact]
    public void 労働投入の節約は生産者の所属州の機械化度で決まる()
    {
        var c = new Coefficients { LaborSaveRate = 0.25, ElecLaborSave = 0 };
        var home = new Region { Mechanization = 0.5 };
        var other = new Region { Mechanization = 0.0 };
        var s = new Producer { GoodId = 1, BuysLabor = true, Inputs = [new Input(Catalog.LaborId, 2.0), new Input(0, 1.0)] };
        Assert.Equal(2.0 * (1.0 - 0.25 * 0.5), EconomyStep.EffectiveQty(s, home, s.Inputs[0], c));
        Assert.Equal(2.0, EconomyStep.EffectiveQty(s, other, s.Inputs[0], c));
        Assert.Equal(1.0, EconomyStep.EffectiveQty(s, home, s.Inputs[1], c));   // 労働以外は不変
        c.LaborSaveRate = 0;
        Assert.Equal(2.0, EconomyStep.EffectiveQty(s, home, s.Inputs[0], c));   // 対照腕＝恒等
    }
}
