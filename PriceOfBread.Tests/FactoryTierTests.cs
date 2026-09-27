using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>工場建設費の素材階層+α（FactoryTierMachinePerDepth・2026-08-09）の番人。
/// 階層＝既存の Catalog.Complexity（レシピDAG最長深さ0〜9・手置きテーブルなし）。
/// +α＝工場建設ブロック限定＝建設1単位あたり 機械を 係数×max(0, 深さ−1) 束だけ追加購入（基本4材束は不変＝「基本は固定・上位材+α」）。
/// ★2026-08-09に点火（既定0.05・ign_tier）。0へ戻せば全新コードが >1e-12 ガードの内側で1命令も実行しない＝ビット恒等）。
/// ここでは (1)既定値ピン（点火0.05） (2)Complexity階層ピン＝goods.json変更の番人 (3)ON経路＝機械の需要と決済が深さの式どおり増え他3材は不変
/// (4)浅い財/RGOはONでも1ビットも動かない (5)ON経路のフル走行で基礎貨幣保存 を固定する。</summary>
public class FactoryTierTests
{
    private static Scenario Fr(ulong s, double pollTax = 0.5, double treasury = 200_000) => new()
    {
        Seed = s,
        StartYear = 1789,
        Kind = ScenarioKind.France,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        FiscalEnabled = true,
        TaxRate = 0.10,
        InitialPollTax = pollTax,
        InitialTreasury = treasury,
        BaseCoercion = 400,
        EducationLaw = EducationLaw.Religious,
        ExternalPressure = 0.1,
    };

    [Fact]
    public void 係数の既定は点火済み()
    {
        Assert.Equal(0.05, new Coefficients().FactoryTierMachinePerDepth);   // 点火2026-08-09（ign_tier＝硬性緑Δ総合+2.1・列強+3.7＝狙いの軸）   // 既定0＝ビット恒等（既存フルスイートが番人）。点火設計値0.05目安
    }

    [Fact]
    public void 連鎖深さの階層が期待どおり()   // goods.json のレシピ変更で+αの階層が黙って動かないための実測ピン
    {
        Assert.Equal(0.0, Catalog.Complexity[15]);   // 麦＝RGO（+α対象外）
        Assert.Equal(0.0, Catalog.Complexity[0]);    // 鉄鉱石＝RGO（+α対象外）
        Assert.Equal(1.0, Catalog.Complexity[47]);   // 小麦粉＝深さ1（max(0, 深さ−1)=0＝式から+αなし）
        Assert.Equal(2.0, Catalog.Complexity[57]);   // 衣料＝軽工業（+α深さ1）
        Assert.Equal(3.0, Catalog.Complexity[32]);   // 鋼＝重工業（+α深さ2）
        Assert.Equal(5.0, Catalog.Complexity[53]);   // 機械（+α深さ4）
        Assert.Equal(9.0, Catalog.Complexity[80]);   // 電化製品＝先端（+α深さ8）
    }

    /// <summary>3世界の単月切り分け：C=建設なし・A=建設(OFF)・B=建設(ON 0.05)。40四半期まで3世界はビット恒等（決定論）
    /// →同じ建設中フラグをA/Bへ手置き→EconomyStep.Run を1回だけ直接呼ぶ（式の単離＝着工ゲートは対象外）。
    /// 今月の建設束 u はOFF側の鋼需要差から逆算＝buildAfford を再実装せずに式を厳密検証。
    /// 機械在庫は3世界同一に厚くする＝配給率1で決済式（got=需要×ration）を厳密に読む。</summary>
    [Fact]
    public void ON経路_深い財の建設は機械の需要と決済が深さの式どおり増える()
    {
        var sA = new Simulation(Fr(11), new NullAgent());
        var sB = new Simulation(Fr(11), new NullAgent());
        var sC = new Simulation(Fr(11), new NullAgent());
        sA.Run(40); sB.Run(40); sC.Run(40);
        var rA = sA.World.Regions[0]; var rB = sB.World.Regions[0]; var rC = sC.World.Regions[0];
        var c = sA.World.Coef;
        int sg = c.SteelGoodId, mg = c.MachineGoodId, cg = c.CementGoodId, lg = c.LumberGoodId;
        rA.Markets[mg].Stock += 1e6; rB.Markets[mg].Stock += 1e6; rC.Markets[mg].Stock += 1e6;   // 3世界同一の在庫増＝比較は保たれたまま ration[機械]=1
        double days = Catalog.FactorySpec(sg).Quarters * (double)Calendar.DaysPerQuarter;
        var pA = rA.Producers.Find(p => p.GoodId == sg); var pB = rB.Producers.Find(p => p.GoodId == sg);
        Assert.NotNull(pA); Assert.NotNull(pB);
        pA!.ConstructionLeft = days; pB!.ConstructionLeft = days;   // Cは建設なし
        sA.World.Coef.FactoryTierMachinePerDepth = 0.0;             // OFF側を明示（既定は点火済み0.05＝2026-08-09。既定非依存の対照実験）
        sC.World.Coef.FactoryTierMachinePerDepth = 0.0;
        sB.World.Coef.FactoryTierMachinePerDepth = 0.05;            // ON側
        double spentA0 = rA.StateSpent, spentB0 = rB.StateSpent;
        double machinePrice = rA.Markets[mg].Price;                 // 決済価格（EconomyStep 内で価格は不変＝clearPrice と同値。価格更新は MarketFlow）
        EconomyStep.Run(rA, sA.World.Coef);
        EconomyStep.Run(rB, sB.World.Coef);
        EconomyStep.Run(rC, sC.World.Coef);
        double u = (rA.Markets[sg].LastDemand - rC.Markets[sg].LastDemand) / c.ConsSteel;   // 今月の建設束（OFF側の差分から逆算）
        Assert.True(u > 1e-9, "建設束が立っていない＝テスト前提の破れ");
        double expected = 0.05 * (Catalog.Complexity[sg] - 1.0) * u;                        // 鋼＝深さ3→建設1単位あたり機械+0.10
        Assert.Equal(expected, rB.Markets[mg].LastDemand - rA.Markets[mg].LastDemand, 6);   // 需要計上が式どおり
        Assert.Equal(rA.Markets[sg].LastDemand, rB.Markets[sg].LastDemand);   // +αは機械のみ＝他3材の需要は1ビットも動かない（基本束は固定）
        Assert.Equal(rA.Markets[cg].LastDemand, rB.Markets[cg].LastDemand);
        Assert.Equal(rA.Markets[lg].LastDemand, rB.Markets[lg].LastDemand);
        // 決済＝got×clearPrice（ration[機械]=1）：国庫負担の増分が +α束×市場価格に一致（StateSpent へ記帳・revenue[機械] は保存テストが番人）
        Assert.Equal(expected * machinePrice, (rB.StateSpent - spentB0) - (rA.StateSpent - spentA0), 6);
    }

    [Fact]
    public void ON経路_浅い財とRGOの建設では機械の需要も決済も1ビットも動かない()   // 深さ0/1は max(0, 深さ−1)=0＝式から自然に+αなし（特例コードなし）
    {
        var sOff = new Simulation(Fr(11), new NullAgent());
        var sOn = new Simulation(Fr(11), new NullAgent());
        sOff.Run(40); sOn.Run(40);
        var rOff = sOff.World.Regions[0]; var rOn = sOn.World.Regions[0];
        int mg = sOff.World.Coef.MachineGoodId;
        foreach (int gid in new[] { 47, 15 })   // 小麦粉＝深さ1・麦＝RGO深さ0
        {
            var pOff = rOff.Producers.Find(p => p.GoodId == gid); var pOn = rOn.Producers.Find(p => p.GoodId == gid);
            Assert.NotNull(pOff); Assert.NotNull(pOn);
            double days = Catalog.FactorySpec(gid).Quarters * (double)Calendar.DaysPerQuarter;
            pOff!.ConstructionLeft = days; pOn!.ConstructionLeft = days;
        }
        sOff.World.Coef.FactoryTierMachinePerDepth = 0.0;    // OFF側を明示（既定は点火済み0.05＝2026-08-09。既定依存だと同一世界比較になり番人が空洞化する）
        sOn.World.Coef.FactoryTierMachinePerDepth = 0.05;
        EconomyStep.Run(rOff, sOff.World.Coef);
        EconomyStep.Run(rOn, sOn.World.Coef);
        Assert.Equal(rOff.Markets[mg].LastDemand, rOn.Markets[mg].LastDemand);   // ビット恒等（tierQty=0＝加算も決済も不実行）
        Assert.Equal(rOff.StateSpent, rOn.StateSpent);
    }

    [Fact]
    public void ON経路_工場建設のフル走行で基礎貨幣を保存する()   // +αの材料購入＝国庫→生産者の移転（StateSpent と revenue の対記帳）＝Dブラケットが全ティック保持
    {
        var sim = new Simulation(Fr(2025, 1.0, 1_000_000), new NullAgent());   // FranceTests の国営工場テストと同じ厚い国庫（重工業の建設費は素材価格依存）
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        sim.World.Coef.FactoryTierMachinePerDepth = 0.05;
        double base0 = sim.World.BaseMoney;
        sim.Run(40);
        Assert.True(sim.World.BuildFactory(0, 32), "着工できない＝テスト前提の破れ");
        var ex = Record.Exception(() => sim.Run(60));   // §11.6（保存・非負・財収支）が全ティックで保持
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney - base0 - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued) < 1e-6 * base0,
            "素材階層+αの決済で基礎貨幣保存が破れた");
    }
    /// <summary>着工の採算ゲート（<see cref="Coefficients.FactoryStartAffordFloor"/>・2026-08-04）の番人。
    /// ★生産タブの着工ボタンは2026-08-09からこの判定を**同じ関数・同じ定数**で写す（GameViewModel.Production の canBuild）。
    ///   写す前は「押せる・押した・何も起きない」＝建材が高い期間だけ再現する無言の空クリックだった。
    ///   ここで固定するのは「価格だけで決まる／拒否は副作用を残さない／相場が戻れば着工できる」の3点＝UI が第二の基準を持てない形。</summary>
    [Fact]
    public void 建材が高すぎる間は着工できない()
    {
        var sim = new Simulation(Fr(2025, 1.0, 1_000_000), new NullAgent());   // 重工業の建設費は素材相場依存＝厚い国庫（既存の国営工場テストと同じ前提）
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        sim.Run(40);
        var w = sim.World;
        var r = w.Region;
        var c = w.Coef;
        int[] mats = { c.SteelGoodId, c.MachineGoodId, c.CementGoodId, c.LumberGoodId };
        Assert.True(World.ConstructionAfford(w) >= new Coefficients().FactoryStartAffordFloor, "平常の相場で採算が半減点を割っている＝テスト前提の破れ");
        Assert.True(w.BuildFactory(0, 32), "平常の相場で着工できない＝テスト前提の破れ");
        var p = World.FindFactory(r, 32);   // 同上＝着工した生産者そのものを掴む（別の生産者の ConstructionLeft を0にしても枝は外れない）
        Assert.NotNull(p);
        p!.ConstructionLeft = 0;                                   // 「建設中」の枝を外し、採算ゲートだけを単離する
        foreach (int id in mats) r.Markets[id].Price *= 10.0;       // 建材束を参照の半減点（FactoryBuildPriceCeil=2.5）の遥か上へ
        Assert.True(World.ConstructionAfford(w) < new Coefficients().FactoryStartAffordFloor);
        Assert.False(w.BuildFactory(0, 32), "建材が高騰しても着工できる＝ゲートが死んでいる");
        Assert.Equal(0.0, p.ConstructionLeft);                      // 拒否は副作用を残さない（UI が false を受けても世界は不変）
        foreach (int id in mats) r.Markets[id].Price /= 10.0;       // 相場が戻れば着工できる＝判定は価格だけに依存する
        Assert.True(w.BuildFactory(0, 32));
    }

    [Fact]
    public void 資本区画のない州でも工場建設は材料を買い国庫が払う()
    {
        // capゲート穴の閉鎖（2026-08-09）の番人：従来は CapitalEnabled=false の州で着工すると材料費ゼロ・工賃のみで完成した。
        var sA = new Simulation(Fr(11), new NullAgent());   // 建設あり
        var sC = new Simulation(Fr(11), new NullAgent());   // 建設なし（対照・同seed）
        sA.Run(40); sC.Run(40);
        var rA = sA.World.Regions[0]; var rC = sC.World.Regions[0];
        rA.CapitalEnabled = false; rC.CapitalEnabled = false;
        var c = sA.World.Coef;
        int sg = c.SteelGoodId;
        var pA = rA.Producers.Find(p => p.GoodId == sg);
        Assert.NotNull(pA);
        pA!.ConstructionLeft = Catalog.FactorySpec(sg).Quarters * (double)Calendar.DaysPerQuarter;
        EconomyStep.Run(rA, sA.World.Coef);
        EconomyStep.Run(rC, sC.World.Coef);
        double dSteel = rA.Markets[sg].LastDemand - rC.Markets[sg].LastDemand;
        Assert.True(dSteel > 1e-9, "cap無効州で建設材料の需要が立っていない＝穴が再発");
        Assert.True(rA.StateSpent > rC.StateSpent + 1e-9, "材料費が国庫負担（StateSpent）になっていない");
    }

    /// <summary>★施設の建設仕様の明示登録（LIST §0-9-C 段1・2026-09-12）：学校94＝軽工業／病院95＝重工業／大学136＝先端。
    /// 値は既存タプルの再利用（新しい数字ゼロ）。役務は建てられない（Tier==Service を先に弾く）ので今はどの読み手もこの id で引かない＝恒等。</summary>
    [Fact]
    public void 施設の建設仕様は既存の組を明示登録し役務は建てられないままである()
    {
        Assert.Equal(("軽工業", 30_000.0, 4), Catalog.FactorySpec(Catalog.EducationId));
        Assert.Equal(("重工業", 70_000.0, 8), Catalog.FactorySpec(Catalog.HealthcareId));
        Assert.Equal(("先端", 180_000.0, 20), Catalog.FactorySpec(Catalog.HigherEducationId));
        Assert.Equal(Catalog.FactorySpec(32), Catalog.FactorySpec(Catalog.HealthcareId));          // 病院＝鋼32と同じ組（既存タプルの再利用）
        Assert.Equal(Catalog.FactorySpec(55), Catalog.FactorySpec(Catalog.HigherEducationId));     // 大学＝発電機55と同じ組
        var sim = new Simulation(NationConfig.Britain().ToScenario(23), new NullAgent());
        foreach (int gid in new[] { Catalog.EducationId, Catalog.HealthcareId, Catalog.HigherEducationId })
        {
            var (ok, reason) = sim.World.CanBuildFactory(0, gid);
            Assert.False(ok); Assert.Equal("役務は建てられない", reason);   // FactorySpec より前で弾かれる＝登録値は読まれない
            Assert.False(sim.World.BuildFactory(0, gid));
        }
    }

    /// <summary>★国庫の門（2026-09-27・線2）：材料は工事が進むときだけ買う＝工賃の日次の門（World.StepConstruction）と同じ述語。
    /// 3世界の単月切り分け：C＝建設なし・A＝建設中で国庫が負・B＝建設中で国庫が正。A は C と1ビットも違わない（材料の需要も国庫負担も立たない）。
    /// B は材料が立つ＝門が「いつも閉じている」空の番人でないことの対照。旧＝国庫を見ずに毎月買った＝止まった工事が材料だけを買い続けた。</summary>
    [Fact]
    public void 国庫が負のとき着工中の工場は材料を買わない()
    {
        var sA = new Simulation(Fr(11), new NullAgent());
        var sB = new Simulation(Fr(11), new NullAgent());
        var sC = new Simulation(Fr(11), new NullAgent());
        sA.Run(40); sB.Run(40); sC.Run(40);
        var rA = sA.World.Regions[0]; var rB = sB.World.Regions[0]; var rC = sC.World.Regions[0];
        int sg = sA.World.Coef.SteelGoodId;
        var spec = Catalog.FactorySpec(sg);
        double days = Math.Max(1, spec.Quarters) * (double)Calendar.DaysPerQuarter;
        double payPerDay = (1.0 - sA.World.Coef.FactoryMaterialFrac) * spec.Cost / days;   // BuildFactory と同じ1日分の工賃
        foreach (var r in new[] { rA, rB })
        {
            var p = World.FindFactory(r, sg);
            Assert.NotNull(p);
            p!.ConstructionLeft = days; p.ConstructionPayPerDay = payPerDay;
        }
        foreach (var s in new[] { sA, sC }) s.World.Pay(TreasuryCat.OtherOut, -(s.World.Treasury + 1000.0));   // 国庫＝−1000（A と C は同じ）
        Assert.True(sA.World.Treasury < 0 && sC.World.Treasury == sA.World.Treasury, "前提が崩れた：A と C の国庫が同じ負の値でない");
        Assert.True(sB.World.Treasury >= payPerDay, "前提が崩れた：B の国庫が1日分の工賃に届かない");
        EconomyStep.Run(rA, sA.World.Coef, 1.0, null, sA.World);
        EconomyStep.Run(rB, sB.World.Coef, 1.0, null, sB.World);
        EconomyStep.Run(rC, sC.World.Coef, 1.0, null, sC.World);
        Assert.True(rB.Markets[sg].LastDemand > rC.Markets[sg].LastDemand + 1e-9, "前提が崩れた：国庫が正でも建設材料の需要が立たない");
        foreach (int g in new[] { sg, sA.World.Coef.MachineGoodId, sA.World.Coef.CementGoodId, sA.World.Coef.LumberGoodId })
            Assert.Equal(rC.Markets[g].LastDemand, rA.Markets[g].LastDemand);   // 国庫が負＝材料の需要は建設なしとビット一致
        Assert.Equal(rC.StateSpent, rA.StateSpent);                             // 国庫負担（材料の決済）も立たない
    }

}
