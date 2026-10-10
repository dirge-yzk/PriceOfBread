using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>工場の建設（着工・材料・工賃・支払い率・保存）の番人。
/// ★2026-09-30 段1（財の削減＝作者裁定 Q2）：素材階層+α（FactoryTierMachinePerDepth＝連鎖の深さ×機械の上乗せ）を撤去＝
///   その番人（既定値ピン・深さの階層ピン・ON経路の式・浅い財で不変）と段0 の「焼いた発明の門＝深さの旧式」の番人を消した。ファイル名は歴史の名前のまま。</summary>
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
    public void 工場建設のフル走行で基礎貨幣を保存する()   // 材料購入＝国庫→生産者の移転（StateSpent と revenue の対記帳）＝Dブラケットが全ティック保持
    {
        var sim = new Simulation(Fr(2025, 1.0, 1_000_000), new NullAgent());   // FranceTests の国営工場テストと同じ厚い国庫（重工業の建設費は素材価格依存）
        sim.World.BaseCoercion = 80; sim.World.Policy.CoercionTarget = 80;
        double base0 = sim.World.BaseMoney.Value;
        sim.Run(40);
        Assert.True(sim.World.BuildFactory(0, Catalog.SteelId), "着工できない＝テスト前提の破れ");
        var ex = Record.Exception(() => sim.Run(60));   // §11.6（保存・非負・財収支）が全ティックで保持
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.BaseMoney.Value - base0 - sim.World.CumulativeTradeFlow - sim.World.CumulativeGoldMinted - sim.World.CumulativeFiatIssued) < 1e-6 * base0,
            "工場建設の材料の決済で基礎貨幣保存が破れた");
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
        Assert.True(w.BuildFactory(0, Catalog.SteelId), "平常の相場で着工できない＝テスト前提の破れ");
        var p = World.FindFactory(r, Catalog.SteelId);   // 同上＝着工した生産者そのものを掴む（別の生産者の ConstructionLeft を0にしても枝は外れない）
        Assert.NotNull(p);
        p!.ConstructionLeft = 0;                                   // 「建設中」の枝を外し、採算ゲートだけを単離する
        foreach (int id in mats) r.Markets[id].Price *= 10.0;       // 建材束を参照の半減点（FactoryBuildPriceCeil=2.5）の遥か上へ
        Assert.True(World.ConstructionAfford(w) < new Coefficients().FactoryStartAffordFloor);
        Assert.False(w.BuildFactory(0, Catalog.SteelId), "建材が高騰しても着工できる＝ゲートが死んでいる");
        Assert.Equal(0.0, p.ConstructionLeft);                      // 拒否は副作用を残さない（UI が false を受けても世界は不変）
        foreach (int id in mats) r.Markets[id].Price /= 10.0;       // 相場が戻れば着工できる＝判定は価格だけに依存する
        Assert.True(w.BuildFactory(0, Catalog.SteelId));
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
        Assert.True(rA.StateSpent[(int)TreasuryCat.FactoryBuild] > rC.StateSpent[(int)TreasuryCat.FactoryBuild] + 1e-9, "材料費が国庫負担（StateSpent の工場建設の区分）になっていない");
    }

    /// <summary>★施設の建設仕様の明示登録（LIST §0-9-C 段1・2026-09-12）：学校94＝軽工業／病院95＝重工業／大学136＝先端。
    /// 値は既存タプルの再利用（新しい数字ゼロ）。役務は建てられない（Tier==Service を先に弾く）ので今はどの読み手もこの id で引かない＝恒等。</summary>
    [Fact]
    public void 施設の建設仕様は既存の組を明示登録し役務は建てられないままである()
    {
        Assert.Equal(("軽工業", 30_000.0, 4), Catalog.FactorySpec(Catalog.EducationId));
        Assert.Equal(("重工業", 70_000.0, 8), Catalog.FactorySpec(Catalog.HealthcareId));
        Assert.Equal(("先端", 180_000.0, 20), Catalog.FactorySpec(Catalog.HigherEducationId));
        Assert.Equal(Catalog.FactorySpec(Catalog.SteelId), Catalog.FactorySpec(Catalog.HealthcareId));          // 病院＝鋼32と同じ組（既存タプルの再利用）
        Assert.Equal(Catalog.FactorySpec(Catalog.ElectricityId), Catalog.FactorySpec(Catalog.HigherEducationId));   // 大学＝電力（発電所）と同じ組（★段1：発電機は機械へ畳んだ）
        var sim = new Simulation(NationConfig.Britain().ToScenario(23), new NullAgent());
        foreach (int gid in new[] { Catalog.EducationId, Catalog.HealthcareId, Catalog.HigherEducationId })
        {
            var (ok, reason) = sim.World.CanBuildFactory(0, gid);
            Assert.False(ok); Assert.Equal("役務は建てられない", reason);   // FactorySpec より前で弾かれる＝登録値は読まれない
            Assert.False(sim.World.BuildFactory(0, gid));
        }
    }

    /// <summary>★材料は工賃と同じ国家の支払い率 r で買う（2026-09-28 段2-1＝旧は国庫が1日分の工賃に満たない月は買わない門・2026-09-27）。
    /// 3世界の単月切り分け：C＝建設なし・A＝建設中で r＝0・B＝建設中で r＝1。A は C と1ビットも違わない（材料の需要も国庫負担も立たない＝工事も進まない）。
    /// B は材料が立つ＝「いつも閉じている」空の番人でないことの対照。</summary>
    [Fact]
    public void 国家の支払い率が0の月は着工中の工場は材料を買わない()
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
        sA.World.FiscalFundingRatio = 0; sC.World.FiscalFundingRatio = 0; sB.World.FiscalFundingRatio = 1.0;   // A と C＝払えない月・B＝満額
        EconomyStep.Run(rA, sA.World.Coef, 1.0, null, sA.World);
        EconomyStep.Run(rB, sB.World.Coef, 1.0, null, sB.World);
        EconomyStep.Run(rC, sC.World.Coef, 1.0, null, sC.World);
        Assert.True(rB.Markets[sg].LastDemand > rC.Markets[sg].LastDemand + 1e-9, "前提が崩れた：国庫が正でも建設材料の需要が立たない");
        foreach (int g in new[] { sg, sA.World.Coef.MachineGoodId, sA.World.Coef.CementGoodId, sA.World.Coef.LumberGoodId })
            Assert.Equal(rC.Markets[g].LastDemand, rA.Markets[g].LastDemand);   // r＝0＝材料の需要は建設なしとビット一致
        Assert.Equal(rC.StateSpent, rA.StateSpent);                             // 国庫負担（材料の決済）も立たない
    }

    /// <summary>★建設の旬次（2026-09-30 高速化の段 S3c・近似）：<see cref="World.StepConstruction"/>(h) は棟ごとに働く日数 n＝min(h, 完成までの日数) ぶんの
    /// 工賃と進捗を1回で払う＝採算率と支払い率が旬の中で動かない盤面（価格を動かさない）では、日次を h 回まわした工事の残り・棟・国庫・工員の貯蓄と一致する
    /// （丸めの差だけ）。完成の旬（n＜h）は働かない日の工賃を払わない＝旧の日次も完成の翌日から払わない。</summary>
    [Fact]
    public void 建設の旬次は働いた日数ぶんの工賃と進捗を払い日次をh回まわした結果と一致する()
    {
        var sA = new Simulation(Fr(11), new NullAgent());   // 旬次（h 日ぶんを1回）
        var sB = new Simulation(Fr(11), new NullAgent());   // 日次を h 回（同じ種＝同じ盤面）
        sA.Run(40); sB.Run(40);
        int sg = sA.World.Coef.SteelGoodId;
        var pA = World.FindFactory(sA.World.Regions[0], sg); var pB = World.FindFactory(sB.World.Regions[0], sg);
        Assert.NotNull(pA); Assert.NotNull(pB);
        // 採算率＝1（建材の価格を見ない）・支払い率＝1 ⇒ 1日の進捗＝1日。残り 23.5日＝旬10日の3回目に4日働いて完成する（n＜h の枝）。
        foreach (var (s, p) in new[] { (sA, pA!), (sB, pB!) }) { p.ConstructionLeft = 23.5; p.ConstructionPayPerDay = 100.0; s.World.FiscalFundingRatio = 1.0; s.World.Coef.FactoryBuildPriceGateScale = 0; }
        var labA = sA.World.Regions[0].Pops[pA!.LaborPopIndex]; var labB = sB.World.Regions[0].Pops[pB!.LaborPopIndex];
        double built0 = pA.BuiltScale, tre0 = sA.World.Treasury.Value;
        const int h = 10;
        bool partialSpan = false;
        for (int span = 0; span < 12 && pA.ConstructionLeft > 0; span++)
        {
            double leftBefore = pB.ConstructionLeft;
            sA.World.StepConstruction(h);
            for (int d = 0; d < h; d++) sB.World.StepConstruction(1);
            if (pB.ConstructionLeft == 0 && leftBefore <= h - 1) partialSpan = true;   // 完成の旬で働いたのは h 日より少ない
            Assert.True(Math.Abs(pA.ConstructionLeft - pB.ConstructionLeft) <= 1e-9, $"工事の残りが日次と違う（旬 {span}：{pA.ConstructionLeft} 対 {pB.ConstructionLeft}）");
            Assert.Equal(pB.BuiltScale, pA.BuiltScale);
            Assert.True(Math.Abs(sA.World.Treasury.Value - sB.World.Treasury.Value) <= 1e-9 * Math.Abs(sB.World.Treasury.Value), $"国庫が日次と違う（旬 {span}：{sA.World.Treasury} 対 {sB.World.Treasury}）");
            Assert.True(Math.Abs(labA.Savings.Value - labB.Savings.Value) <= 1e-9 * Math.Abs(labB.Savings.Value), $"工員の貯蓄が日次と違う（旬 {span}：{labA.Savings} 対 {labB.Savings}）");
        }
        Assert.Equal(0.0, pA.ConstructionLeft);                     // 盤面の前提＝工事は完成した
        Assert.True(pA.BuiltScale > built0, "前提が崩れた：完成で棟が増えていない");
        Assert.True(partialSpan, "前提が崩れた：完成の旬が h 日ぶんちょうどで終わった（働く日数 n＜h の枝を通っていない）");
        Assert.True(sA.World.Treasury.Value < tre0, "前提が崩れた：工賃が払われていない");
    }

    /// <summary>★工事中の一覧の印（2026-09-30 作者裁定「計算量の予算」・恒等）：<see cref="World.StepConstruction"/> は州の工事中の一覧
    /// （Region.BuildSites）だけを回し、一覧は ConstructionLeft の書き込み（「工事中か」が変わるとき）の印で作り直す。
    /// 一覧を作った後に直書きで着工しても（テスト・盤面の復元の型）次の呼び出しで工事が進み、一覧を捨てて作り直した双子と工事の残り・国庫・工員の貯蓄が一致する。
    /// 完成（0 の書き込み）でも印が立ち工事中の一覧から外れ、建った棟（BuiltScale の印）は国営持分の請求の一覧（Region.StateSites）に入る。
    /// ⚠突然変異「ConstructionLeft の書き込みで印を立てない」で赤（一覧が古いまま＝着工した棟が進まない）。</summary>
    [Fact]
    public void 工事中の一覧は直書きの着工と完成を書き込みの印で拾う()
    {
        var sA = new Simulation(Fr(11), new NullAgent());   // 一覧を作った後の直書き＝書き込みの印だけが頼り
        var sB = new Simulation(Fr(11), new NullAgent());   // 双子＝毎回一覧を捨てて盤面から作り直す（真値）
        sA.Run(40); sB.Run(40);
        int sg = sA.World.Coef.SteelGoodId;
        var rA = sA.World.Regions[0]; var rB = sB.World.Regions[0];
        var pA = World.FindFactory(rA, sg)!; var pB = World.FindFactory(rB, sg)!;
        foreach (var (s, p) in new[] { (sA, pA), (sB, pB) }) { s.World.FiscalFundingRatio = 1.0; s.World.Coef.FactoryBuildPriceGateScale = 0; p.ConstructionPayPerDay = 100.0; }
        sA.World.StepConstruction(10); sB.World.StepConstruction(10);   // 一覧を作る（鋼の工場は工事中でない）
        Assert.True(pA.ConstructionLeft <= 0 && rA.BuildSites is not null && Array.IndexOf(rA.BuildSites, pA) < 0, "前提が崩れた：鋼の工場が既に工事中か一覧が無い");
        double built0 = pA.BuiltScale;
        pA.ConstructionLeft = 23.5; pB.ConstructionLeft = 23.5;   // 直書きの着工（一覧を作った後）
        var labA = rA.Pops[pA.LaborPopIndex]; var labB = rB.Pops[pB.LaborPopIndex];
        for (int span = 0; span < 4; span++)
        {
            rB.BuildSites = null;   // 双子は毎回作り直す
            sA.World.StepConstruction(10); sB.World.StepConstruction(10);
            Assert.Equal(pB.ConstructionLeft, pA.ConstructionLeft);
            Assert.Equal(sB.World.Treasury.Value, sA.World.Treasury.Value);
            Assert.Equal(labB.Savings.Value, labA.Savings.Value);
        }
        Assert.Equal(0.0, pA.ConstructionLeft);                     // 3旬目に完成
        Assert.True(pA.BuiltScale > built0, "前提が崩れた：完成で棟が増えていない");
        rA.EnsureSites();
        Assert.True(Array.IndexOf(rA.BuildSites!, pA) < 0, "完成した棟が工事中の一覧に残った（0 の書き込みで印が立っていない）");
        Assert.True(Array.IndexOf(rA.StateSites!, pA) >= 0, "建った棟が国営持分の請求の一覧に入っていない");
        // 工事と無関係に BuiltScale だけが立つ書き込み（接収・盤面の直書きの型）も印を立てる
        var q = rA.Producers.Find(x => x.ConstructionLeft <= 0 && !(x.BuiltScale > 1e-9));
        Assert.NotNull(q);
        Assert.True(Array.IndexOf(rA.StateSites!, q) < 0, "前提が崩れた：棟の無い生産者が国営持分の請求の一覧に居る");
        q!.BuiltScale = 1.0;
        rA.EnsureSites();
        Assert.True(Array.IndexOf(rA.StateSites!, q) >= 0, "BuiltScale の書き込みで印が立っていない（国営持分の請求の一覧が古い）");
    }

    /// <summary>★着工の門は建てる州の建材の価格を読む（2026-09-30 処理の一元化 段4e K）＝旧は首都州の価格を読んで別の州に建てた
    /// （工事の進捗・材料・巨大建造物の見積りは建てる州を読んでいた＝門だけ首都）。盤面＝仏（多州の世界）・首都と首都でない州 k の両方で着工できる財 g。
    /// k の建材だけを10倍＝k は「建材が高すぎる」で着工できず首都はできる／首都の建材だけを10倍＝k はできる。⚠突然変異「首都州の価格を読む」で赤。</summary>
    [Fact]
    public void 着工の門は建てる州の建材の価格を読む()
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        var w = sim.Nations[0].World; var c = w.Coef;
        int[] mats = { c.SteelGoodId, c.MachineGoodId, c.CementGoodId, c.LumberGoodId };
        int k = -1, g = -1;
        for (int i = 1; i < w.Regions.Count && k < 0; i++)
            for (int gi = 0; gi < Catalog.Goods.Length; gi++)
                if (w.CanBuildFactory(0, gi).Ok && w.CanBuildFactory(i, gi).Ok) { k = i; g = gi; break; }
        Assert.True(k > 0, "前提が崩れた：首都と別の州で同じ財を着工できる組が無い");
        var rk = w.Regions[k]; var r0 = w.Regions[0];
        foreach (int id in mats) rk.Markets[id].Price *= 10.0;
        var (okK, why) = w.CanBuildFactory(k, g);
        Assert.False(okK, "建てる州の建材が高騰しても着工できる＝門が首都を読んでいる");
        Assert.Contains("建材", why);
        Assert.True(w.CanBuildFactory(0, g).Ok, "首都の建材は平常なのに首都で着工できない");
        foreach (int id in mats) { rk.Markets[id].Price /= 10.0; r0.Markets[id].Price *= 10.0; }
        Assert.True(w.CanBuildFactory(k, g).Ok, "首都の建材の高騰で別の州の着工が止まる＝門が首都を読んでいる");
        Assert.False(w.CanBuildFactory(0, g).Ok);
    }
}
