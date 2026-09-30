using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class MilitaryTests
{
    private static Scenario Force(ulong s) => new()
    {
        Seed = s,
        Kind = ScenarioKind.FrontierDevelopment,
        ForceUnlock = true,
        InitialTreasury = 100_000,
        PoliticsEnabled = true,
        InitialPolity = Polity.Absolutist,
        EducationLaw = EducationLaw.Public,
        BaseCoercion = 3000,
    };

    // ★2026-09-30（支払いの一元化 段3-2・作者裁定 Q8・C7）：武力解錠は**軍需の購買**（首都の市場から陸の軍需を買って消費する）になった
    //   ＝軍需の財を持たない較正の盤面（FrontierDevelopment＝食料1財）では制圧は進まない（軍需が無ければ制圧できない）。
    //   ⇒ 速さ・出血・闘争度・保存の番人は実国（英）の盤面の州を1つ閉じて測る（実国の世界に閉じた州は無い＝番人の中でだけ作る）。
    //   ⚠旧の「解錠が終わると闘争度が鎮まる」（σ の減衰）は較正の盤面の軌道に乗っていた＝実国の盤面では他の力学が混ざる＝この番人からは外した。
    private static (Simulation sim, Region locked) LockedBritain(ulong seed, bool force)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(seed), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        var locked = w.Regions.FindLast(r => r.State == RegionState.Active && !ReferenceEquals(r, w.Region))!;
        locked.State = RegionState.Locked; locked.Infra = 0;
        foreach (var m in locked.Markets) { m.PrevStock = m.Stock; m.LastProduced = 0; m.LastSold = 0; m.LastImport = 0; m.LastExport = 0; }   // 一度も開いていない州の市場（解錠の月の財収支の恒等式が閉じる）
        w.ForceUnlock = force;
        return (sim, locked);
    }

    [Fact]
    public void 武力解錠は開発より速いが軍需の出血と闘争度を伴う()   // §A-8 強硬 path
    {
        var (dev, dl) = LockedBritain(1, force: false);
        var (force, fl) = LockedBritain(1, force: true);
        double devLed0 = dev.World.TreasuryLedger[(int)TreasuryCat.Army], forceLed0 = force.World.TreasuryLedger[(int)TreasuryCat.Army];
        dev.Run(1); force.Run(1);   // 制圧の最中（ForceUnlockRate＝0.10/月×買えた割合）
        Assert.True(fl.Infra > dl.Infra + 0.05, $"武力が速くない（武力 {fl.Infra:F3} vs 開発 {dl.Infra:F3}）");
        double devArmy = devLed0 - dev.World.TreasuryLedger[(int)TreasuryCat.Army], forceArmy = forceLed0 - force.World.TreasuryLedger[(int)TreasuryCat.Army];
        Assert.True(forceArmy > devArmy, $"軍需の出血が立っていない（陸軍の区分の払い 武力 {forceArmy:F1} vs 開発 {devArmy:F1}）");
        Assert.True(force.Telemetry[^1].AvgMilitancy > dev.Telemetry[^1].AvgMilitancy,
            $"制圧の最中でも開発より闘争度が高くない（武力 {force.Telemetry[^1].AvgMilitancy:F4} vs 開発 {dev.Telemetry[^1].AvgMilitancy:F4}）");
    }

    [Fact]
    public void 武力解錠でも人口と基礎貨幣は保存される()
    {
        var (sim, locked) = LockedBritain(2, force: true);
        var w = sim.World;
        static double D(World w) => w.BaseMoney - w.CumulativeTradeFlow - w.CumulativeGoldMinted - w.CumulativeFiatIssued;
        double p0 = w.TotalPopulation - w.PopulationDelta, d0 = D(w);
        var ex = Record.Exception(() => sim.Run(6));
        Assert.Null(ex);
        Assert.True(locked.State == RegionState.Active, $"前提が崩れた：6四半期で解錠していない（進み {locked.Infra:F3}）");
        Assert.True(Math.Abs(w.TotalPopulation - w.PopulationDelta - p0) < 1e-6 * p0, "人口保存の破れ");
        Assert.True(Math.Abs(D(w) - d0) <= 1e-9 * Math.Max(1.0, Math.Abs(d0)), $"保存量 D の破れ（{d0} → {D(w)}）");
    }

    // ★番人の主張の書き直し（作者裁定2026-09-19 M1）：**総動員は徴兵法を迂回しない**。
    //   旧主張（2026-08-31・AI審査3-10の固定）＝「Military.cs の wartimeConscript ＝ atWar && (法≥選抜 || Mobilized)＝総動員は志願制でも強制徴募扱い＝闘争度が積まれる」。
    //   新主張＝wartimeConscript ＝ atWar && 法≥選抜（Military.cs:187）＝志願兵制の国は総動員しても強制徴募されない＝兵数も遺恨も総動員なしと1ビットも違わない。
    //   総動員が効くのは既にある徴兵の深さ（Military.cs:128 → 読み手は :191 の強制徴募だけ）＝下の「選抜以上の徴兵制は総動員で徴兵が深まる」が対の番人。
    //   史実＝代議制国家の大量徴兵は必ず立法を通った（英1916兵役法・米1863/1917徴兵法・南部連合1862）＝行政の総動員が志願制の法を覆した例は無い。
    //   この意味論が変わったらここが赤くなる（「無い」は最も誤りやすい主張＝コードでピンする）。
    [Fact]
    public void 総動員は志願兵制を迂回せず強制徴募も遺恨も立たない()
    {
        (double Soldiers, double Before, double After) Run(bool mobilized)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            var w = sim.Nations[0].World;
            w.ConscriptionLaw = ConscriptionLaw.Volunteer;
            w.Mobilized = mobilized;
            Assert.True(w.Coef.ConscriptGrievanceRate > 0, "前提が崩れた：徴兵遺恨の係数が0＝この番人は無意味");
            double before = PoolMilitancy(w);
            Military.Step(w, w.Coef, atWar: true);
            return (Soldiers(w), before, PoolMilitancy(w));
        }

        var plain = Run(mobilized: false);   // 対照腕：志願制×戦時×総動員なし＝志願規模のみ＝遺恨は積まれない
        Assert.Equal(plain.Before, plain.After);
        var mob = Run(mobilized: true);      // 本腕：志願制×戦時×総動員＝徴兵法が志願のまま＝強制徴募は立たない
        Assert.Equal(plain.Soldiers, mob.Soldiers);
        Assert.Equal(plain.After, mob.After);
        Assert.Equal(mob.Before, mob.After);
    }

    // ★対の番人（作者裁定2026-09-19 M1）：総動員は**既にある徴兵を深める**＝徴兵法≥選抜の国では Military.cs:128 の深さ（MobilizationConscriptFrac）が
    //   :191 の強制徴募に届き、兵数も遺恨も総動員なしより大きい。M1 は総動員を消したのではなく「法の上に乗る」ことを示す（この腕が無いと
    //   「総動員が何もしない」退化でも上の番人が緑になる＝ゲート緑≠機構が働いた）。
    [Theory]
    [InlineData(ConscriptionLaw.Selective)]
    [InlineData(ConscriptionLaw.Universal)]
    public void 選抜以上の徴兵制は総動員で徴兵が深まる(ConscriptionLaw law)
    {
        (double Soldiers, double Militancy) Run(bool mobilized)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            var w = sim.Nations[0].World;
            w.ConscriptionLaw = law;
            w.Mobilized = mobilized;
            Assert.True(w.Coef.ConscriptGrievanceRate > 0, "前提が崩れた：徴兵遺恨の係数が0＝この番人は無意味");
            Assert.True(w.Coef.MobilizationConscriptFrac > w.Coef.ConscriptionFraction, "前提が崩れた：総動員の深さが通常戦時の深さを超えない＝この番人は無意味");
            Military.Step(w, w.Coef, atWar: true);
            return (Soldiers(w), PoolMilitancy(w));
        }

        var plain = Run(mobilized: false);
        var mob = Run(mobilized: true);
        Assert.True(mob.Soldiers > plain.Soldiers + 1e-9,
            $"{law}：総動員で兵数が増えない（総動員なし {plain.Soldiers:F2} / 総動員 {mob.Soldiers:F2}）＝深さが強制徴募に届いていない");
        Assert.True(mob.Militancy > plain.Militancy + 1e-9,
            $"{law}：総動員で徴兵遺恨が深まらない（総動員なし {plain.Militancy:F6} / 総動員 {mob.Militancy:F6}）");
    }

    private static double PoolMilitancy(World w)
    {
        double m = 0; int n = 0;
        foreach (var p in w.Region.Pops)
            if (p.ClassId == w.Coef.SoldierRecruitClassA || p.ClassId == w.Coef.SoldierRecruitClassB) { m += p.Militancy; n++; }
        return n > 0 ? m / n : 0;
    }

    private static double Soldiers(World w)
    {
        double s = 0;
        foreach (var p in w.AllPops()) if (p.IsSoldier) s += p.Size;
        return s;
    }

    [Fact]
    public void 同一シードで武力解錠は決定論的()
    {
        var a = new Simulation(Force(3), new NullAgent());
        var b = new Simulation(Force(3), new NullAgent());
        a.Run(100);
        b.Run(100);
        Assert.Equal(a.World.Regions[1].SumPopulation(), b.World.Regions[1].SumPopulation());
        Assert.Equal(a.World.Treasury, b.World.Treasury);
    }

    /// <summary>★前期の軍需レシピ（2026-09-07 器・2026-09-27 回2-1 で旗ごと点火）：小銃(82)・火砲(81)に鋼を要しない並存レシピ（マスケット・鋳鉄砲）が
    /// 技術の始まり0で1つずつ立ち、密閉英国の1783〜（鋼＝坩堝鋼のみの時代）でも小銃が作られる。
    /// ⚠計器（2026-09-08・統合検証）：**一瞬のフローではなく累積**で測る＝在庫の周期で価格が落ちた月だけフローが絞られても計器が揺れない
    ///   （PriceLawSupplyIncludesStock の在庫項で 12期目の LastProduced が逆転した前例）。</summary>
    [Fact]
    public void 前期の軍需レシピは鋼の無い時代に小銃を作る()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(29), new NullAgent());
        var r = sim.World.Region;
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == Catalog.ArtilleryId && p.RecipeName == "鋳鉄砲");
        foreach (var p in new[] { musket, castGun })
        {
            Assert.Equal(1.0, ProductionDiag.TechGate(p.GoodId, p.TechOnset, r.TechLevel));   // 1783年は満額（退場は下の番人）
            Assert.DoesNotContain(p.Inputs, inp => inp.GoodId == Catalog.SteelId);   // 鋼を要しない
        }
        double cumulative = 0;
        for (int q = 0; q < 12; q++) { sim.Run(1); cumulative += musket.Output; }
        Assert.True(cumulative > 0, "マスケットのレシピが12四半期で1単位も産出していない（並存レシピの注入漏れか投入不足）");
    }

    /// <summary>★前期の軍需レシピの転換は**費用で**起きる（暦年を書かない）：限界費用はエンジンと同じ式（自己費用＋投入費）。
    /// 鋼が坩堝鋼の値段（ベッセマー以前）ならマスケットが安く、ベッセマー転炉の限界費用まで下がれば既定（鋼）の小銃が安い。
    /// ⚠火砲はこの形にならない（鋳鉄砲＝鉄1.3＋製材0.3 が、ベッセマーの値段でも既定＝鋼1.0＋機械部品0.5 より安い）＝このテストは小銃だけを固定する。
    /// 火砲（と小銃）の転換は下の「後継の発見で退場する」が固定する（史実の転換は費用でなく性能で起きた）。</summary>
    [Fact]
    public void 小銃の製法の転換は鋼の値段で起きる()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(29), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        double Mc(Producer p) => EconomyStep.MineralSelfCost(p, r, c, sim.World) + EconomyStep.InputCostPerUnit(p, r, r, c);
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var rifle = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.Inputs.Any(inp => inp.GoodId == Catalog.SteelId));   // 既定（鋼）レシピ
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.TechOnset > 0);                     // 既定の鋼＝ベッセマー転炉（onset 0.55）
        var crucible = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.RecipeName == "坩堝鋼");

        r.Markets[Catalog.SteelId].Price = Mc(crucible);   // ベッセマー以前＝鋼は坩堝鋼の値段
        Assert.True(Mc(musket) < Mc(rifle), $"坩堝鋼の値段でマスケットが安くない（マスケット {Mc(musket):F3} / 既定 {Mc(rifle):F3}）");
        r.Markets[Catalog.SteelId].Price = Mc(bessemer);   // ベッセマー以後＝鋼が安い
        Assert.True(Mc(rifle) < Mc(musket), $"ベッセマーの値段で既定の小銃が安くない（既定 {Mc(rifle):F3} / マスケット {Mc(musket):F3}）＝転換が費用で起きない");
    }

    /// <summary>★前期の軍需レシピの退場（2026-09-27・回2-1 の続き）：能力に「1 − 後継の解禁度」が乗る
    /// （TechOnset＝−ベッセマー転炉の onset＝Catalog.RecipeTechScale の負の枝・解禁の仕組みは1本のまま）。
    /// ①1783年は満額 ②後継の解禁の帯の中は、ちょうど 1 − 後継の解禁度（連続＝崖を作らない）
    /// ③後継の解禁の後は操業規模も産出も0（未発明と同じ扱い）で、火砲・小銃は発見済みのまま（既定のレシピが引き継ぐ）。</summary>
    [Fact]
    public void 前期の軍需レシピは後継の発見で退場する()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(29), new NullAgent());
        var w = sim.World; var r = w.Region; var c = w.Coef;
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == Catalog.ArtilleryId && p.RecipeName == "鋳鉄砲");
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.TechOnset > 0);   // 後継を実用にした発見＝鋼の既定レシピ（ベッセマー転炉）
        var old = new[] { musket, castGun };

        // ①1783年＝後継の onset の手前＝満額
        Assert.True(r.TechLevel < bessemer.TechOnset, $"前提が崩れている（初期技術 {r.TechLevel:F3}／後継の onset {bessemer.TechOnset:F3}）");
        foreach (var p in old) Assert.Equal(1.0, ProductionDiag.TechGate(p.GoodId, p.TechOnset, r.TechLevel));

        // ②後継の解禁の帯の中＝1 − 後継の解禁度（連続）
        foreach (double t in new[] { bessemer.TechOnset + 0.25, bessemer.TechOnset + 0.5, bessemer.TechOnset + 0.75 })
            foreach (var p in old)
            {
                double g = ProductionDiag.TechGate(p.GoodId, p.TechOnset, t);
                Assert.Equal(1.0 - ProductionDiag.TechGate(32, bessemer.TechOnset, t), g, 12);
                Assert.True(g > 0 && g < 1, $"{p.RecipeName}：技術 {t:F2} で退場が0か1に潰れている（{g:F3}）＝崖ができている");
            }

        // ③後継の解禁の後＝操業規模も産出も0・装備は発見済みのまま
        w.TechLevel = bessemer.TechOnset + 2.0;
        sim.Run(2);
        foreach (var p in old)
        {
            Assert.Equal(0.0, ProductionDiag.TechGate(p.GoodId, p.TechOnset, r.TechLevel));
            Assert.True(p.Output == 0.0 && p.Scale == 0.0, $"{p.RecipeName}：後継の解禁の後も産出 {p.Output}／操業規模 {p.Scale}");
        }
        Assert.Equal(1.0, EconomyStep.EquipUnlock(r, c, 81));   // 火砲は既定のレシピ（鋼の砲）で作れる＝発見済みのまま
        Assert.Equal(1.0, EconomyStep.EquipUnlock(r, c, 82));
    }

    /// <summary>M4（2026-09-11）：恒常性の信号の分子は上限（MilitaryBudgetCeiling）1本＝旧 Math.Max(Ceiling, Budget) は Budget ≤ Ceiling の単調性で同値。
    /// 必要額で切る腕・切らない腕の両方で全 Active 州が Budget ≤ Ceiling を保つ（分子と分母の非対称を作らない番人）。</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.0)]
    public void 軍需予算は上限を超えない(double needCapped)
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
        foreach (var n in sim.Nations) n.World.Coef.MilitaryBudgetNeedCapped = needCapped;
        sim.StepQuarter();
        int checkedRegions = 0, capitals = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
            {
                if (r.State != RegionState.Active) continue;
                checkedRegions++;
                Assert.True(r.MilitaryBudget <= r.MilitaryBudgetCeiling,
                    $"{n.World.SelfPower?.Name}／{r.Name}：予算 {r.MilitaryBudget} > 上限 {r.MilitaryBudgetCeiling}（NeedCapped={needCapped}）");
                Assert.True(r.MilitaryBudgetCeiling >= 0 && double.IsFinite(r.MilitaryAffordRaw) && r.MilitaryAffordRaw >= 0,
                    $"{n.World.SelfPower?.Name}／{r.Name}：上限 {r.MilitaryBudgetCeiling}／支払能力 {r.MilitaryAffordRaw} が不正");
                if (ReferenceEquals(r, n.World.Region)) { capitals++; Assert.True(r.MilitaryBudgetCeiling > 0, "首都の軍需予算上限が0＝Simulation の書き手を経ていない"); }
            }
        Assert.True(checkedRegions > 0 && capitals == sim.Nations.Count);
    }

    /// <summary>#7（2026-09-11）：徴兵目標の母数は Politics.RuleDelivery 倍（RuleSharesTaxBase>0・在地区分の村請け）。届き方0.5なら目標兵数も半分＝
    /// 同じ盤面で兵士の到達量が対照腕（g=0＝×1＝DormantIdentity の恒等）より小さい。App のヘッダー/政治タブの目標兵数はこの1本を読む。</summary>
    [Fact]
    public void 徴兵の母数は支配度の届き方で縮む()
    {
        static (double before, double after, World w) Arm(double g)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
            var w = sim.World;
            w.Coef.RuleSharesTaxBase = g;
            foreach (var r in w.Regions) r.Rule = [0.5, 0.0, 0.5, 0.0];   // 国家区分5割・在地区分0（村請け無し）＝届き方 0.5
            double before = w.SoldierSize();
            Military.Step(w, w.Coef, atWar: false);
            return (before, w.SoldierSize(), w);
        }
        var off = Arm(0.0); var on = Arm(1.0);
        Assert.Equal(1.0, Politics.RuleDelivery(off.w.Region, off.w.Coef, 0.0));
        Assert.Equal(0.5, Politics.RuleDelivery(on.w.Region, on.w.Coef, 1.0));
        Assert.Equal(off.before, on.before);
        Assert.True(on.after < off.after, $"母数が半分なのに兵数が縮まない（対照 {off.before:F1}→{off.after:F1}／本腕 {on.before:F1}→{on.after:F1}）");
    }

    // ===== 処理の一元化 段3（軍の不具合・2026-09-29）の番人 =====

    private static MultiNationSim TwoNations()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        return sim;
    }

    /// <summary>侵攻の標的＝奪う植民地を持つ弱小の抽象標的（従属していない）。</summary>
    private static Power InvasionTarget(MultiNationSim sim)
        => sim.Diplomacy.Powers.Find(p => p.Nation is null && p.ColonyList.Count > 0 && sim.Diplomacy.RankOf(p) == PowerRank.Minor && sim.Diplomacy.Suzerain(p) is null)!;

    [Fact]
    public void 即応度はMilitaryに一度だけ掛かる_侵攻の門と植民地の抑え()
    {
        // 旧＝Power.Military は既に即応度を含む（Diplomacy の戦力合成）のに、侵攻の門（World.CanInvade）と植民地の抑え（Colonial.Step）がもう一度掛けた
        //   ＝即応度の二乗で比べた（2026-09-29 処理の一元化 段3 G）。
        var sim = TwoNations();
        var w = sim.Nations[0].World; var me = w.SelfPower!; var c = w.Coef;
        var target = InvasionTarget(sim);
        Assert.NotNull(target);
        w.ExpansionWill = 1e12;
        w.MilitaryReadiness = 0.5;
        target.Military = 1.0;
        me.Military = 1.01 * c.InvadeMilitaryFactor;
        w.CanInvade(target, out string reason);
        Assert.NotEqual("兵力が不足", reason);   // 即応度 0.5 でも Military が要求を1%超えていれば兵力は足りる
        me.Military = 0.99 * c.InvadeMilitaryFactor;
        Assert.False(w.CanInvade(target, out reason));
        Assert.Equal("兵力が不足", reason);      // 前提＝門まで届いている（手前の門で落ちていない）

        // 植民地の抑え：同じ軍・同じ圧力で、即応度だけ違う双子が同じ結果（抑え＝軍×係数・圧力は抑えの7割）
        static bool Freed(double readiness)
        {
            var s = TwoNations();
            var ww = s.Nations[0].World; var pp = ww.SelfPower!;
            pp.ColonyList.Clear();
            ww.Region.ColonialResources ??= [];
            pp.Military = 0.01; ww.MilitaryReadiness = readiness;
            var col = new Colony { Name = "試験の植民地", Population = 1.0, Militancy = 0.95, PopSize = 0.7 * pp.Military * ww.Coef.ColonialHoldScale / 0.95 };
            pp.ColonyList.Add(col);
            Colonial.Step(ww, ww.Coef);
            return !pp.ColonyList.Contains(col);
        }
        Assert.False(Freed(1.0), "前提が崩れた：抑えが圧力を上回るのに独立した");
        Assert.False(Freed(0.2), "即応度が植民地の抑えにもう一度掛かっている（Military は既に即応度を含む）");
    }

    [Fact]
    public void 侵攻の物資は国家の調達で買って消費する_財と代金の台帳に残る()
    {
        // 旧＝World.Invade が首都の市場の在庫を直に *=・-= で消した＝缶詰・キニーネ・船が代金も財の出入りの台帳も無く消えた
        //   （2026-09-29 処理の一元化 段3 K＝国家の調達の1本 Colonial.BuyUpkeepGood・区分＝戦費）。
        var sim = TwoNations();
        var w = sim.Nations[0].World; var me = w.SelfPower!;
        var target = InvasionTarget(sim);
        Assert.NotNull(target);
        w.ExpansionWill = 1e12; me.Military = 1e9; target.Military = 1.0;
        w.FiscalFundingRatio = 1.0;
        int[] goods = [Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId, Catalog.QuinineId];
        var m = w.Region.Markets;
        foreach (int g in goods) m[g].Stock = 1e6;
        var st0 = new double[goods.Length]; var ex0 = new double[goods.Length]; var px = new double[goods.Length];
        for (int k = 0; k < goods.Length; k++) { st0[k] = m[goods[k]].Stock; ex0[k] = m[goods[k]].LastExport; px[k] = System.Math.Max(m[goods[k]].Price, 1e-9); }
        double led0 = w.TreasuryLedger[(int)TreasuryCat.WarCost];
        Assert.True(w.CanInvade(target, out string why), $"前提が崩れた：侵攻できない（{why}）");

        w.Invade(target);

        double used = 0, paid = 0;
        for (int k = 0; k < goods.Length; k++)
        {
            double u = st0[k] - m[goods[k]].Stock;
            Assert.Equal(u, m[goods[k]].LastExport - ex0[k], 6);   // 市場から出た量＝財の出入りの台帳に載った量
            used += u; paid += u * px[k];
        }
        Assert.True(used > 0, "前提が崩れた：物資を1つも消費していない");
        Assert.Equal(-paid, w.TreasuryLedger[(int)TreasuryCat.WarCost] - led0, 6);   // 国庫が代金を払った（戦費）＝生産者の手に渡る
    }

    // ===== 支払いの一元化 段3-2（2026-09-30・作者裁定 Q8・C6・C7）の番人 =====

    private static (List<Pop> pops, double[] sav) SavingsOf(World w)
    {
        var l = new List<Pop>();
        foreach (var r in w.Regions) l.AddRange(r.Pops);
        return (l, l.ConvertAll(p => p.Savings).ToArray());
    }

    /// <summary>★植民地の駐留と鎮圧の俸給は兵士の兵力の生産者の④（兵士の賃金）へ・国家の支払い率 r の分だけ。旧＝本国の全POP へ頭数。
    /// 盤面＝英仏の英の双子（植民地格の州の不満 0.5）＝不満の維持費の係数だけが違う（30 と 0）＝双子の貯蓄の差が俸給の流れそのもの。
    /// ⚠突然変異「全POP へ頭数で配る」で赤。</summary>
    [Fact]
    public void 植民地の駐留の俸給は兵士の兵力の賃金へ届く()
    {
        (World w, List<Pop> pops, double[] sav, double led) Arm(double scale)
        {
            var sim = TwoNations();
            var w = sim.Nations[0].World;
            w.Coef.ColonyUpkeepMilitancyScale = scale;
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) foreach (var p in r.Pops) p.Militancy = 0.5;
            w.FiscalFundingRatio = 0.7;
            double led0 = w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep];
            Colonial.Step(w, w.Coef);
            var (pops, sav) = SavingsOf(w);
            return (w, pops, sav, w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep] - led0);
        }
        var zero = Arm(0); var arm = Arm(new Coefficients().ColonyUpkeepMilitancyScale);
        double paid = zero.led - arm.led;
        Assert.True(paid > 0, "前提が崩れた：駐留の俸給が立っていない（植民地格の州の不満）");
        var soldiers = new HashSet<Pop>();
        foreach (var r in arm.w.Regions) if (r.State == RegionState.Active) foreach (var pr in r.Producers) if (pr.GoodId == Catalog.MilitaryPowerId && pr.Output > 0) soldiers.Add(r.Pops[pr.LaborPopIndex]);
        Assert.NotEmpty(soldiers);
        double got = 0;
        for (int i = 0; i < arm.pops.Count; i++)
        {
            double d = arm.sav[i] - zero.sav[i];
            if (soldiers.Contains(arm.pops[i])) got += d; else Assert.True(d == 0, $"兵士でない POP（階級 {arm.pops[i].ClassId}）の貯蓄が {d} 動いた＝全POP へ配っている");
        }
        Assert.True(Math.Abs(got - paid) <= 1e-9 * paid, $"兵士の受け取り {got} ≠ 国庫の払い {paid}");
    }

    /// <summary>★段4d（2026-09-30・分子と分母を同じ規則で）：駐留の俸給の請求（LastColonialUpkeep＝翌月の国家の支払い率 r の請求）は受け手（兵士の兵力の生産者）のいる分だけ＝
    /// 連合維持費と同じ1本の述語（InternationalMarket.HasSellers）。兵士のいない国は俸給の請求が立たず、国庫も動かない（旧＝請求だけが立ち、払えない請求で r が下がった）。
    /// 盤面＝英仏の英の2×2（不満の維持費の係数 0／既定 × 兵士の兵力の産出あり／0）＝係数の差が俸給の請求（補給財の請求は打ち消す）。⚠突然変異「受け手を見ない（旧）」で赤。</summary>
    [Fact]
    public void 植民地の駐留の俸給の請求は兵士のいる分だけ()
    {
        (double claim, double led) Arm(double scale, bool soldiers)
        {
            var sim = TwoNations();
            var w = sim.Nations[0].World;
            w.Coef.ColonyUpkeepMilitancyScale = scale;
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) foreach (var p in r.Pops) p.Militancy = 0.5;
            if (!soldiers)   // 兵士の兵力の生産者がいない国（月次の決算が書く産出の和 LastProduced も0＝盤面を一貫させる）
                foreach (var r in w.Regions) { foreach (var pr in r.Producers) if (pr.GoodId == Catalog.MilitaryPowerId) pr.Output = 0; r.Markets[Catalog.MilitaryPowerId].LastProduced = 0; }
            w.FiscalFundingRatio = 0.7;
            double led0 = w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep];
            Colonial.Step(w, w.Coef);
            return (w.LastColonialUpkeep, w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep] - led0);
        }
        double scale = new Coefficients().ColonyUpkeepMilitancyScale;
        var w0 = Arm(0, true); var w1 = Arm(scale, true); var n0 = Arm(0, false); var n1 = Arm(scale, false);
        Assert.True(w1.claim - w0.claim > 1e-9, "前提が崩れた：兵士のいる国で俸給の請求が立っていない（植民地格の州の不満）");
        Assert.True(Math.Abs(n1.claim - n0.claim) <= 1e-9 * Math.Max(1.0, n0.claim), $"兵士のいない国に俸給の請求が立った（{n1.claim - n0.claim}）");
        Assert.True(Math.Abs(n1.led - n0.led) <= 1e-9 * Math.Max(1.0, Math.Abs(n0.led)), $"兵士のいない国の国庫が俸給で動いた（{n1.led - n0.led}）");
    }

    /// <summary>★国家の調達（補給財・侵攻の物資・海軍の燃料＝Colonial.BuyUpkeepGood）の代金は、その財の生産者の④の持分（地代・国営持分・賃金）へ。旧＝本国の全POP へ頭数。
    /// 盤面＝英仏の英の双子＝首都で最も多く作られている財を買う量だけが違う。⚠突然変異「全POP へ頭数で配る」で赤。</summary>
    [Fact]
    public void 国家の調達の代金はその財の生産者の持分へ届く()
    {
        (World w, List<Pop> pops, double[] sav, double treas, int good) Arm(double scale)
        {
            var sim = TwoNations();
            var w = sim.Nations[0].World; var cap = w.Region;
            int good = -1; double best = 0;
            foreach (var pr in cap.Producers)
                if (pr.GoodId >= 0 && pr.GoodId < cap.Markets.Length && Catalog.Goods[pr.GoodId].Tier != GoodTier.Service && pr.Output > best) { best = pr.Output; good = pr.GoodId; }
            Assert.True(good >= 0, "前提が崩れた：首都に財の生産者がいない");
            w.FiscalFundingRatio = 1.0;
            cap.Markets[good].Stock = 1e9;
            Colonial.BuyUpkeepGood(w, cap, good, scale * best, TreasuryCat.ColonialUpkeep);
            var (pops, sav) = SavingsOf(w);
            return (w, pops, sav, w.Treasury, good);
        }
        var zero = Arm(0); var arm = Arm(1.0);
        var sellers = new HashSet<Pop>();
        foreach (var pr in arm.w.Region.Producers) if (pr.GoodId == arm.good && pr.Output > 0) { sellers.Add(arm.w.Region.Pops[pr.OwnerPopIndex]); sellers.Add(arm.w.Region.Pops[pr.LaborPopIndex]); }
        double got = 0;
        for (int i = 0; i < arm.pops.Count; i++)
        {
            double d = arm.sav[i] - zero.sav[i];
            if (sellers.Contains(arm.pops[i])) got += d; else Assert.True(d == 0, $"生産者の持分でない POP（階級 {arm.pops[i].ClassId}）の貯蓄が {d} 動いた＝全POP へ配っている");
        }
        double paid = zero.treas - arm.treas;
        Assert.True(got > 0, "前提が崩れた：調達の代金が生産者に届いていない");
        Assert.True(Math.Abs(got - paid) <= 1e-9 * Math.Max(1.0, Math.Abs(arm.treas)), $"生産者の受け取り {got} ≠ 国庫の払い {paid}");
    }

    /// <summary>★武力解錠は軍需の購買（C7）＝首都の市場から陸の軍需（艦隊の財を除く基本段・発見の解禁度を通した比）を月の予算ぶん買って消費し、
    /// 代金は陸軍の区分で国庫から・進み方は買えた割合（代金÷予算）。旧＝国庫から全POP へ頭数で配り（財を買わない）、進み方は r だけ。
    /// ⚠突然変異「全POP へ頭数で配る（財を買わない）」で赤（軍需の在庫が減らない）。</summary>
    [Fact]
    public void 武力解錠は首都の市場から陸の軍需を買って消費し進み方は買えた割合()
    {
        var (sim, locked) = LockedBritain(3, force: true);
        var w = sim.World; var c = w.Coef; var cap = w.Region;
        w.FiscalFundingRatio = 1.0;
        var rates = EconomyStep.DiscoveredRates(cap, c, c.MilitaryGoods, c.MilitaryRates);
        var land = new List<int>(); var fleet = new List<int>();
        for (int b = 0; b < c.MilitaryGoods.Length; b++)
        {
            int g = c.MilitaryGoods[b];
            if (g >= cap.Markets.Length) continue;
            cap.Markets[g].Stock = 1e9;
            if (Array.IndexOf(c.FleetGoodIds, g) >= 0) fleet.Add(g); else if (rates[b] > 0) land.Add(g);
        }
        Assert.NotEmpty(land);
        var st0 = new Dictionary<int, double>(); var ex0 = new Dictionary<int, double>(); var px = new Dictionary<int, double>();
        foreach (int g in land.Concat(fleet)) { st0[g] = cap.Markets[g].Stock; ex0[g] = cap.Markets[g].LastExport; px[g] = Math.Max(cap.Markets[g].Price, 1e-9); }
        double led0 = w.TreasuryLedger[(int)TreasuryCat.Army], infra0 = locked.Infra;
        Territory.Step(w, c);
        double spent = 0;
        foreach (int g in land)
        {
            double used = st0[g] - cap.Markets[g].Stock;
            Assert.Equal(used, cap.Markets[g].LastExport - ex0[g], 6);   // 市場から出た量＝財の出入りの台帳
            spent += used * px[g];
        }
        foreach (int g in fleet) Assert.Equal(st0[g], cap.Markets[g].Stock);   // 艦隊の財は買わない
        Assert.True(spent > 0, "陸の軍需を買っていない＝財を買わない国家支出のまま");
        Assert.True(Math.Abs(led0 - w.TreasuryLedger[(int)TreasuryCat.Army] - spent) <= 1e-9 * Math.Max(1.0, spent), "陸軍の区分の国庫の払いが軍需の代金と一致しない");
        double fill = Math.Min(1.0, spent / c.ForceUnlockTreasuryCost);
        Assert.True(Math.Abs(locked.Infra - infra0 - c.ForceUnlockRate * fill) <= 1e-9 * c.ForceUnlockRate, $"進み方 {locked.Infra - infra0} ≠ 速さ×買えた割合 {c.ForceUnlockRate * fill}");
    }
}
