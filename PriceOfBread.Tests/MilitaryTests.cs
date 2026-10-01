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
            Assert.Equal(1.0, Discovery.Gate(r, p));   // 1783年は満額（退場は下の番人）
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
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.DiscoveryRow >= 0);                 // 既定の鋼＝ベッセマー転炉（発見の表の転炉鋼）
        var crucible = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.RecipeName == "坩堝鋼");

        r.Markets[Catalog.SteelId].Price = Mc(crucible);   // ベッセマー以前＝鋼は坩堝鋼の値段
        Assert.True(Mc(musket) < Mc(rifle), $"坩堝鋼の値段でマスケットが安くない（マスケット {Mc(musket):F3} / 既定 {Mc(rifle):F3}）");
        r.Markets[Catalog.SteelId].Price = Mc(bessemer);   // ベッセマー以後＝鋼が安い
        Assert.True(Mc(rifle) < Mc(musket), $"ベッセマーの値段で既定の小銃が安くない（既定 {Mc(rifle):F3} / マスケット {Mc(musket):F3}）＝転換が費用で起きない");
    }

    /// <summary>★前期の軍需レシピの退場（2026-09-27・回2-1 の続き）：後継（転炉鋼＝鋼の既定のレシピ）を発見すると退く。
    /// ★2026-10-01 段2A（作者裁定 §1A-2＝発見は2値）：旧＝能力に「1 − 後継の解禁度」（技術水準の傾斜・連続）→ 新＝後継を発見した四半期に門が0（2値・退場の表）。
    /// ①1783年（後継は未発見）は満額 ②技術水準をどれだけ上げても後継を発見するまで満額（技術水準は門を動かさない）
    /// ③後継の発見の後は門が0・操業規模も産出も0（未発明と同じ扱い）で、火砲・小銃は発見済みのまま（既定のレシピが引き継ぐ）。</summary>
    [Fact]
    public void 前期の軍需レシピは後継の発見で退場する()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(29), new NullAgent());
        var w = sim.World; var r = w.Region;
        var musket = Assert.Single(r.Producers, p => p.GoodId == Catalog.RifleId && p.RecipeName == "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == Catalog.ArtilleryId && p.RecipeName == "鋳鉄砲");
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == Catalog.SteelId && p.DiscoveryRow >= 0);   // 後継を実用にした発見＝鋼の既定レシピ（ベッセマー転炉＝発見の表の転炉鋼）
        var old = new[] { musket, castGun };
        foreach (var p in old) Assert.Equal(bessemer.DiscoveryRow, p.RetiredByRow);   // 退場の表＝後継は転炉鋼
        r.Discovered = w.Discovered;   // 州は国の配列を参照する（月次の写しの前の盤面）

        // ①1783年＝後継は未発見＝満額
        Assert.False(Discovery.Known(w.Discovered, bessemer.DiscoveryRow), "前提が崩れている（転炉鋼が発見済み）");
        foreach (var p in old) Assert.Equal(1.0, Discovery.Gate(r, p));

        // ②技術水準では退かない（2値＝発見だけが門を動かす）
        foreach (double t in new[] { 0.5, 1.5, 5.0 })
        {
            r.TechLevel = t;
            foreach (var p in old) Assert.Equal(1.0, Discovery.Gate(r, p));
        }

        // ③後継の発見の後＝門が0・操業規模も産出も0・装備は発見済みのまま
        w.Discovered[bessemer.DiscoveryRow] = true;
        foreach (var p in old) Assert.Equal(0.0, Discovery.Gate(r, p));
        sim.Run(2);
        foreach (var p in old)
        {
            Assert.Equal(0.0, Discovery.Gate(r, p));
            Assert.True(p.Output == 0.0 && p.Scale == 0.0, $"{p.RecipeName}：後継の発見の後も産出 {p.Output}／操業規模 {p.Scale}");
        }
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.ArtilleryId));   // 火砲は既定のレシピ（鋼の砲）で作れる＝発見済みのまま
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.RifleId));
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
            w.Coef.SustainMilShare = 1e9;   // ★(145) 平時の目標＝扶養限界の目標＝人口の側（上限割合×母数）で読む（経済の側が律速にならない）
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

    /// <summary>★(140)（2026-09-30・col-16）：植民地の不満は兵への俸給を立てない＝駐留の兵は兵力の購買（兵数×民間の賃金）で払われている＝旧の不満の俸給
    /// （30×人口ウェイト×不満を兵士の兵力の生産者へ）は同じ兵への2度目の支払いだった。盤面＝英仏の英の双子（植民地格の州の不満 0／0.9）で
    /// 植民地の月（Colonial.Step）を1回＝請求・国庫の台帳・全ての POP の貯蓄が1ビットも違わない。⚠突然変異「不満の俸給を戻す」で赤。</summary>
    [Fact]
    public void 植民地の不満は兵への二重の俸給を立てない()
    {
        (double claim, double led, double[] sav) Arm(double militancy)
        {
            var sim = TwoNations();
            var w = sim.Nations[0].World;
            int n = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active && r.Tier == RegionTier.Colonial) { n++; foreach (var p in r.Pops) p.Militancy = militancy; }
            Assert.True(n > 0, "前提が崩れた：英に植民地格の州が無い");
            w.FiscalFundingRatio = 0.7;
            double led0 = w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep];
            Colonial.Step(w, w.Coef);
            return (w.LastColonialUpkeep, w.TreasuryLedger[(int)TreasuryCat.ColonialUpkeep] - led0, SavingsOf(w).sav);
        }
        var calm = Arm(0.0); var hot = Arm(0.9);
        Assert.Equal(calm.claim, hot.claim);
        Assert.Equal(calm.led, hot.led);
        Assert.Equal(calm.sav, hot.sav);
    }

    /// <summary>★(140)（2026-09-30）：兵の給与は請求どおりの賃金（徴募の母数の働き盛り1人あたりの月収×SoldierPayMult×国家の支払い率 r＝SoldierPayBudget）で払い、
    /// 兵力の市場の値で払わない（旧＝兵力の参照価格 0.5×物価水準の価格の門で頭を押さえられた市場の値＝兵の給与が請求の 7〜99%）。
    /// 盤面＝仏の双子（同じ種で2四半期）＝月の頭に兵力の市場の値だけを 1/100 と 100 倍にして1か月＝兵士の月の所得が1ビットも違わず、給与の予算（名簿の兵×請求の単価×r）を受け取る。
    /// ⚠突然変異「市場の値で払う（旧）」「配給率を掛ける」で赤。⚠「価格の門を掛ける」は走らせていない（給与は予算に固定＝門を掛けても緑・(144) 反証の指摘）。
    /// ⚠見るのは「予算以上を受け取る」だけ＝予算の出どころ（請求の単価＝<see cref="Military.RecruitWage"/>）は新2 が見る。
    /// ★(145)：兵力の市場は削除した（下の「兵力の市場は無く…」）＝市場の値の拡縮は何も読まない＝この番人は市場を戻したときに給与がその値を読まないことの見張り。</summary>
    [Fact]
    public void 兵の給与は請求どおりの賃金で払い兵力の市場の値で払わない()
    {
        (double earned, double budget) Arm(double priceMul)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(2);
            foreach (var r in sim.World.Regions) if (r.State == RegionState.Active && r.SoldierPopIndex >= 0) r.Markets[Catalog.MilitaryPowerId].Price *= priceMul;
            for (int d = 0; d < 30; d++) sim.StepDay();
            double e = 0, b = 0;
            foreach (var r in sim.World.Regions) if (r.State == RegionState.Active && r.SoldierPopIndex >= 0) { e += r.Pops[r.SoldierPopIndex].Earned; b += r.SoldierPayBudget; }
            return (e, b);
        }
        var lo = Arm(0.01); var hi = Arm(100.0);
        Assert.True(lo.earned > 0, "前提が崩れた：兵士が給与を受け取っていない");
        Assert.Equal(lo.earned, hi.earned);
        Assert.True(lo.earned >= lo.budget * (1 - 1e-9), $"兵の所得 {lo.earned} が給与の予算 {lo.budget} に届かない（市場の値か配給率で払っている）");
    }

    /// <summary>★(140)（2026-09-30・分子と分母を同じ規則で）：兵の賃金（請求の単価と扶養限界の兵1人の費用）は徴募の母数（農民・都市労働者）の
    /// **働き盛り（16〜59歳）1人あたり**＝世帯の子供の数に依らない（旧＝頭数あたり＝子供が増えると兵の賃金が下がった）。
    /// 盤面＝仏の首都の農民の頭数を倍にし、増えた分を全て0〜9歳に置く（働き盛りの人数と所得は同じ）＝請求の単価（RecruitWage）も扶養限界も変わらない。
    /// ⚠突然変異「頭数で割る（旧）」（請求の単価・扶養限界のどちらか）で赤。</summary>
    [Fact]
    public void 兵の賃金は徴募の母数の働き盛り1人あたりで子供の数に依らない()
    {
        (double wage, double sustain) Arm(bool children)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef; var cap = w.Region;
            c.MaxSoldierFraction = 1e9;   // 扶養限界を経済の側で読む（人口の側が律速にならない）
            if (children)
                foreach (var r in w.Regions)
                    foreach (var p in r.Pops)
                        if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB) && p.Size > 0)
                        {
                            var a = (double[])p.Ages.Clone();
                            for (int k = 0; k < a.Length; k++) a[k] *= 0.5;
                            a[0] += 0.25; a[1] += 0.25;   // 0〜9歳の男女
                            p.Ages = a; p.Size *= 2;
                        }
            return (Military.RecruitWage(cap, c, last: true), Military.SustainLimitOf(w, c, out _));
        }
        var a0 = Arm(false); var a1 = Arm(true);
        Assert.True(a0.wage > 0 && a0.sustain > 0, $"前提が崩れた：賃金 {a0.wage}・扶養限界 {a0.sustain}");
        Assert.True(Math.Abs(a1.wage - a0.wage) <= 1e-9 * a0.wage, $"子供が増えると兵の賃金が動いた（{a0.wage} → {a1.wage}）");
        Assert.True(Math.Abs(a1.sustain - a0.sustain) <= 1e-9 * a0.sustain, $"子供が増えると扶養限界が動いた（{a0.sustain} → {a1.sustain}）");
    }

    /// <summary>★(144) 合流（反証の指摘＝分子と分母の非対称）：兵役の母数（兵の目標 Step と扶養限界 SustainLimitOf の人口の側）は**徴兵で動かない**
    /// ＝徴募元の項（働き盛りの男＝<see cref="Military.RecruitMen"/>・(145) で男16〜59歳の実数へ）と現兵士の項が同じ単位（兵士も RecruitMen）。
    /// 旧（(142) 4e-2b の段）＝兵士を頭数のまま足した＝男を1人徴兵すると徴募元の項が約6.9 減り兵士の項は1しか増えない＝母数が徴兵のたびに細った。
    /// 盤面＝仏の双子（同じ種で1四半期・経済の側を外す＝平時の目標＝扶養限界の人口の側＝上限割合×母数×軍拡の倍率・(145)）。片方だけ首都の農民から男20〜39 を3割徴兵（MoveCohort）してから
    /// 扶養限界の母数と、兵の目標（Step の1回＝兵数の変化÷率＋兵数）を読む＝2つの腕で一致する。
    /// ⚠突然変異「兵士を頭数のまま」（Step・扶養限界のどちらか）で赤（(144) の単位＝徴募元は男20〜39÷0.145）。
    /// ★(149) 反証の指摘：(145) の後は兵士POP の全員が働き盛りの男＝頭数と RecruitMen が同じ値＝この変異はもう赤にならない。
    ///   今の単位で赤になる変異＝徴募元と兵士の数え方がずれるもの（例＝兵士を規模×標準の構成の働き盛りの男の割合〔0.2765〕で数える＝男を1人徴兵すると徴募元が1減り兵士の項は約0.28 しか増えない＝(149) の突然変異 H3）。</summary>
    [Fact]
    public void 兵役の母数は徴兵で動かない_兵士も徴募元と同じ単位で数える()
    {
        (double natMan, double target) Arm(double draftFrac)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef; var cap = w.Region;
            c.SustainMilShare = 1e9;   // ★(145) 平時の目標＝扶養限界の目標＝人口の側（上限割合×母数×軍拡の倍率）で読む（経済の側が律速にならない＝母数だけを読む）
            var sld = cap.Pops[cap.SoldierPopIndex];
            Pop? pool = null; foreach (var p in cap.Pops) if (!p.IsSoldier && p.ClassId == c.SoldierRecruitClassA) { pool = p; break; }
            Assert.True(pool is not null && pool.Size > 0 && sld.Size > 0, "前提が崩れた：首都に農民か兵士がいない");
            if (draftFrac > 0) Military.MoveCohort(pool!, sld, draftFrac * pool!.Size * pool.AgeShare(20, 40, 0), c);
            Military.SustainLimitOf(w, c, out double nm);
            double s0 = sld.Size;
            Military.Step(w, c, atWar: false);
            return (nm, s0 + (sld.Size - s0) / c.SoldierMigrationRate);   // Step の目標（兵数の変化＝率×(目標−兵数)）
        }
        var a = Arm(0); var b = Arm(0.3);
        Assert.True(a.natMan > 0 && a.target > 0, $"前提が崩れた：母数 {a.natMan}・目標 {a.target}");
        Assert.True(System.Math.Abs(b.natMan - a.natMan) <= 1e-9 * a.natMan, $"徴兵で扶養限界の母数が動いた（{a.natMan} → {b.natMan}）＝兵士と徴募元の単位が違う");
        Assert.True(System.Math.Abs(b.target - a.target) <= 1e-9 * a.target, $"徴兵で兵の目標が動いた（{a.target} → {b.target}）＝兵士と徴募元の単位が違う");
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

    // ===== (145) 兵の単位（2026-10-01）の番人 =====

    /// <summary>★(145)：平時の兵の目標は扶養限界の目標**そのもの**で、州への按分の分子（州の母数）と分母（国の母数）が同じ規則（<see cref="Military.RegionManpower"/>）
    /// ＝州の目標の和が国の目標（<see cref="Military.SustainTargetOf"/>）に一致する。
    /// 旧＝0.75×軍事費の段×上限割合×母数＋0.25×扶養限界の按分・按分の分母だけが国民意識で割り引かれた（N4）＝和が 1÷平均の国民意識 倍（清で約12倍）。
    /// 盤面＝仏（1四半期・平時）＝Step の1回の兵数の変化から州の目標を読み（変化＝率×(目標−兵数)）、和を国の目標と比べる。
    /// ⚠突然変異「扶養限界の国の母数を国民意識で割り引く（旧 N4）」「平時の目標に軍事費の段の項を戻す」で赤。</summary>
    [Fact]
    public void 平時の兵の目標は扶養限界の目標そのもので州の和が国の目標に一致する()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        var w = sim.World; var c = w.Coef;
        double target = Military.SustainTargetOf(w, c, out double natMan);
        Assert.True(target > 0 && natMan > 0, $"前提が崩れた：目標 {target}・母数 {natMan}");
        var s0 = new Dictionary<Region, double>();
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active)
                foreach (var p in r.Pops) if (p.IsSoldier) { s0[r] = p.Size; break; }
        Military.Step(w, c, atWar: false);
        double sum = 0; int n = 0;
        foreach (var (r, before) in s0)
        {
            double after = 0; foreach (var p in r.Pops) if (p.IsSoldier) { after = p.Size; break; }
            sum += before + (after - before) / c.SoldierMigrationRate; n++;
        }
        Assert.True(n > 1, "前提が崩れた：兵士POPのある州が1つ以下");
        Assert.True(Math.Abs(sum - target) <= 1e-9 * target, $"州の目標の和 {sum} ≠ 国の目標（扶養限界の目標） {target}");
    }

    /// <summary>★(145)（分子と分母を同じ規則で＝単位をそろえる）：扶養限界の経済の側の兵1人の費用＝賃金（名目）＋装備（参照価格×自己費用の物価水準＝名目）。
    /// 旧＝装備だけ参照価格の単位のまま＝物価の高い国ほど装備が安く見えた。盤面＝仏（人口の側を外す）で自己費用の物価水準を倍にする
    /// ＝兵1人の費用（使える歳入×SustainMilShare÷限界）の増えが、参照価格の装備×元の物価水準に一致する。⚠突然変異「物価水準を掛けない」で赤。</summary>
    [Fact]
    public void 扶養限界の装備は自己費用の物価水準で名目へ直す()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        var w = sim.World; var c = w.Coef;
        c.MaxSoldierFraction = 1e9;   // 扶養限界を経済の側で読む
        double equipRef = 0;
        for (int b = 0; b < c.MilitaryGoods.Length; b++) equipRef += c.MilitaryUpkeepScale * c.MilitaryRates[b] * Catalog.RefPrices[c.MilitaryGoods[b]];
        double budget = c.SustainMilShare * Math.Max(0, w.SpendableRevenue());
        double pl0 = w.CostPriceLevel;
        double lim0 = Military.SustainLimitOf(w, c, out _);
        w.CostPriceLevel = 2.0 * pl0;
        double lim1 = Military.SustainLimitOf(w, c, out _);
        Assert.True(lim0 > 0 && lim1 > 0 && equipRef > 0, $"前提が崩れた：限界 {lim0}・{lim1}・装備 {equipRef}");
        double dPer = budget / lim1 - budget / lim0;   // 兵1人の費用の増え
        Assert.True(Math.Abs(dPer - equipRef * pl0) <= 1e-9 * equipRef * pl0, $"兵1人の費用の増え {dPer} ≠ 装備×元の物価水準 {equipRef * pl0}");
    }

    /// <summary>★(145) 兵の単位の読み替え：兵役の母数を「全ての人（標準の構成）」から「働き盛りの男」へ読み替えた比 u（<see cref="Military.WorkingMenStdShare"/>）で、
    /// 兵を単位にした較正を同じ比で読み替えた（旧の兵1単位の働きを保つ＝戦力ゲージ・治安維持力・装備・軍政の力・軍の政治力・徴兵の国民形成・戦争熱）。
    /// 艦隊ゲージの分母（戦力ゲージ×装備）は読み替えの前と同じ（積は u が消える）。⚠突然変異＝どれか1本の読み替えを外すと赤。</summary>
    [Fact]
    public void 兵を単位にした較正は同じ比で読み替える()
    {
        var c = new Coefficients(); double u = Military.WorkingMenStdShare;
        Assert.Equal(0.2765, u, 12);
        Assert.Equal(250.0, c.SoldiersPerMilitary / u, 9);
        Assert.Equal(5.0, c.CoercionPerSoldier * u, 9);
        Assert.Equal(0.18, c.MilitaryUpkeepScale * u, 12);
        Assert.Equal(1.4, c.MilitaryCoupWeight * u, 9);
        Assert.Equal(5.0, c.ArmyPoliticalWeight * u, 9);
        Assert.Equal(0.5, c.NationBuildConscript * u, 12);
        Assert.Equal(4.0, c.WarFeverSoldier * u, 9);
        Assert.Equal(250.0 * 0.18, c.SoldiersPerMilitary * c.MilitaryUpkeepScale, 9);   // 艦隊ゲージの分母の積は不変
        Assert.Equal(c.MaxSoldierFraction * u, Military.PeaceCapShareOfPop(c), 15);    // 平時の上限の人口比
    }

    /// <summary>★(145)：兵力の市場（国家の需要・値・配給率・兵力の生産者）は無い＝兵士の給与は兵士POPの所得へ直接（(140) から市場の値と量の読み手が無かった）。
    /// 盤面＝仏（1四半期＋30日）＝どの州の生産者の一覧にも兵力が無く、兵士の月の所得が給与の予算（名簿の兵×請求の単価×r）に届く。⚠突然変異「兵力の生産者を戻す」で赤。</summary>
    [Fact]
    public void 兵力の市場は無く兵士の給与は所得へ直接届く()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        for (int d = 0; d < 30; d++) sim.StepDay();
        double e = 0, b = 0;
        foreach (var r in sim.World.Regions)
        {
            foreach (var pr in r.Producers) Assert.NotEqual(Catalog.MilitaryPowerId, pr.GoodId);
            if (r.State == RegionState.Active && r.SoldierPopIndex >= 0) { e += r.Pops[r.SoldierPopIndex].Earned; b += r.SoldierPayBudget; }
        }
        Assert.True(b > 0, "前提が崩れた：兵士の給与の予算が0");
        Assert.True(e >= b * (1 - 1e-9), $"兵士の所得 {e} が給与の予算 {b} に届かない");
    }

    // ===== (151) 陸と海の軍事費の分割・脅威の作り直し（作者裁定 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」・
    //       脅威＝「同盟国は計上しない」「属国は仮想敵国ほどではないが、脅威を与える」「国境を接している国は脅威だが、そうでない国はあまり脅威ではない（国境線が長いほど軍事費が必要になる）」）=====

    private static MultiNationSim Frozen152(params string[] names)
        => new(System.Array.ConvertAll(names, WorldData.ByName), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };

    /// <summary>同盟を張る（本体の締結は AI の確率の枝だけ＝番人は盤面へ直に置く・WarThemeTests と同じ作法）。</summary>
    private static void Ally152(Diplomacy d, Power a, Power b)
    {
        var set = (System.Collections.Generic.HashSet<long>)typeof(Diplomacy).GetField("_alliance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(d)!;
        set.Add((long)typeof(Diplomacy).GetMethod("Key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [a, b])!);
    }

    /// <summary>★(151) 国境＝陸路グラフで隣り合う州の組の数×地形の通りにくさ（2÷両端の州の地形の陸運の費用の和）。対称・陸続きの隣国は正・海を挟む国（日本）は0。
    /// 盤面＝仏・西・日（開幕）。仏の全ての州を山にすると仏西の国境の重みが下がる＝地形を読んでいる。⚠突然変異「地形を読まない（重み1）」で赤。</summary>
    [Fact]
    public void 国境は陸路グラフで隣り合う州の組を地形の通りにくさで重みづけて数える()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var ps = sim.Nations.ConvertAll(n => n.World.SelfPower!);
        var b = ArmyShape.Borders(ps, sim.Coef);
        int n = ps.Count;
        for (int i = 0; i < n; i++) for (int j = 0; j < n; j++) Assert.Equal(b[i * n + j], b[j * n + i]);
        Assert.True(b[0 * n + 1] > 0, $"仏西の国境が0（{b[1]}）＝陸路グラフの隣り合う州を数えていない");
        Assert.Equal(0.0, b[0 * n + 2]); Assert.Equal(0.0, b[1 * n + 2]);   // 日本＝海を挟む＝陸の国境なし
        double before = b[0 * n + 1];
        foreach (var r in sim.Nations[0].World.Regions) r.Terrain = Terrain.Mountain;   // 山（陸運の費用 2.5）
        double after = ArmyShape.Borders(ps, sim.Coef)[0 * n + 1];
        Assert.True(after > 0 && after < before * 0.9, $"仏を山にしても国境の重みが下がらない（{before:F3}→{after:F3}）＝地形の通りにくさを読んでいない");
    }

    /// <summary>★(151) 敵意＝緊張の有理飽和・同盟は0（旧＝3割）・宗主と属国の間は ×(1−AllyAttentionDiscount)。⚠突然変異「同盟も割引だけ」「属国を割り引かない」で赤。</summary>
    [Fact]
    public void 敵意は同盟で0で宗主と属国の間は割り引く()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var fr = sim.Nations[0].World.SelfPower!; var es = sim.Nations[1].World.SelfPower!; var jp = sim.Nations[2].World.SelfPower!;
        static double H0(Diplomacy d, Power a, Power b, Coefficients c) { double t = System.Math.Max(0, d.Tension(a, b)); return c.AttentionHostilityFloor + (1 - c.AttentionHostilityFloor) * t / (t + c.AttentionTensionHalf); }
        Assert.Equal(H0(dip, fr, jp, c), ArmyShape.Hostility(dip, fr, jp, c), 12);
        Ally152(dip, fr, es);
        Assert.Equal(0.0, ArmyShape.Hostility(dip, fr, es, c));
        Assert.Equal(0.0, ArmyShape.Hostility(dip, es, fr, c));
        dip.SetDependency(jp, es);   // 日本が宗主・西が属国（同盟とは別の組）
        Assert.Equal(H0(dip, jp, es, c) * (1 - c.AllyAttentionDiscount), ArmyShape.Hostility(dip, es, jp, c), 12);
        Assert.True(c.AllyAttentionDiscount > 0 && c.AllyAttentionDiscount < 1, "属国の割引が 0 か 1＝仮想敵ほどではないが脅威、が崩れた");
    }

    /// <summary>★(151) 陸と海の分け方＝海の必要量÷max(軍の総合ゲージ, 陸＋内の守り＋海の必要量)。軍が必要量の和より大きい国は海の任務の分だけ（残りの兵は陸）＝清の型、
    /// 軍が足りない国は必要量の比。必要量が全て0（未算出）と NaN は −1（呼び手は今の比のまま）。⚠突然変異「必要量の比だけ（max を外す）」「未算出を0にする」で赤。</summary>
    [Fact]
    public void 海の割合は海の任務の分だけで軍が足りなければ必要量の比()
    {
        var big = new Power { Military = 100, NeedSea = 2, NeedLand = 3, NeedInternal = 1 };
        Assert.Equal(0.02, ArmyShape.NavalSpendShareOf(big), 12);           // 海は任務の分だけ（2÷100）＝残り98%の兵は陸に立つ
        var small = new Power { Military = 1, NeedSea = 2, NeedLand = 3, NeedInternal = 1 };
        Assert.Equal(2.0 / 6.0, ArmyShape.NavalSpendShareOf(small), 12);    // 軍が足りない＝必要量の比
        Assert.Equal(-1.0, ArmyShape.NavalSpendShareOf(new Power { Military = 5 }));
        Assert.Equal(-1.0, ArmyShape.NavalSpendShareOf(new Power { Military = 5, NeedSea = double.NaN, NeedLand = 1 }));
    }

    /// <summary>★(151) 海を越えて来る陸軍は海で止める＝首都どうしが別の陸塊の投射は海の必要量へ（陸の必要量へ入れない）。盤面＝英日の2国（開幕・艦隊の注意配分0・守る海の交易0）
    /// ＝陸の国境なし・海越え＝両国とも陸の必要量は0で海の必要量が正（⚠英仏は開幕から印度の州で陸の国境がある＝この盤面には使えない）。⚠突然変異「海越えも陸へ」で赤。</summary>
    [Fact]
    public void 海を越えて来る陸軍は海の必要量に入る()
    {
        var sim = Frozen152("イギリス", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var uk = sim.Nations[0].World.SelfPower!; var jp = sim.Nations[1].World.SelfPower!;
        Assert.True(dip.IsSeaCrossing(jp, uk, c) && dip.IsSeaCrossing(uk, jp, c), "前提が崩れた：英日が海越えでない");
        foreach (var p in new[] { uk, jp }) { p.NavalShare = 0; Assert.True(p.Military > 0, "前提が崩れた：軍が無い"); }   // 艦隊の注意配分を0に（海の必要量＝海越えの陸軍だけ）
        foreach (var r in sim.Nations[0].World.Regions) r.LastPortThroughput = 0;
        foreach (var r in sim.Nations[1].World.Regions) r.LastPortThroughput = 0;
        Assert.True(ArmyShape.Borders([uk, jp], c)[1] == 0, "前提が崩れた：英日に陸の国境がある");
        ArmyShape.Compute(dip, c);
        Assert.Equal(0.0, uk.NeedLand); Assert.Equal(0.0, jp.NeedLand);
        Assert.True(uk.NeedSea > 0, $"英の海の必要量が0（{uk.NeedSea}）＝海を越えて来る日本の陸軍を海で数えていない");
        Assert.True(jp.NeedSea > 0, $"日本の海の必要量が0（{jp.NeedSea}）");
    }

    /// <summary>★(151) 陸海の維持費の目標の比＝陸と海の必要量の分け方（AI とプレイヤーが同じ関数・合計は保つ）。必要量が未算出なら今の比のまま。⚠突然変異「旧の地理の比へ」で赤。</summary>
    [Fact]
    public void 陸海の維持費の目標は陸と海の必要量で分ける()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var p = w.Policy; var sp = w.SelfPower!;
        p.ArmyUpkeepTarget = 0.45; p.NavyUpkeepTarget = 0.30;
        UtilityAgent.SetNavalUpkeepShare(w);   // 必要量が未算出＝今の比のまま
        Assert.Equal(0.45, p.ArmyUpkeepTarget, 12); Assert.Equal(0.30, p.NavyUpkeepTarget, 12);
        sp.Military = 4; sp.NeedSea = 3; sp.NeedLand = 2; sp.NeedInternal = 1;   // 軍が足りない＝必要量の比 3÷6
        UtilityAgent.SetNavalUpkeepShare(w);
        Assert.Equal(0.75, p.ArmyUpkeepTarget + p.NavyUpkeepTarget, 12);
        Assert.Equal(0.5, p.NavyUpkeepTarget / (p.ArmyUpkeepTarget + p.NavyUpkeepTarget), 12);
    }

    /// <summary>★(151) 開幕（00一覧 §0-A の 11 の申し送り「開幕の陸海の維持費 0.27/0.03 が最初の四半期に跳ぶ」）＝最初の同期で陸と海の必要量を算出し、
    /// 陸海の維持費の比・海軍シェア・艦隊の播種を同じ比で置く（旧＝海軍シェアは0から建艦の時間遅れで比の3割・艦隊もその3割で播いた）。
    /// 盤面＝英仏日（構築の直後に外交を1か月）＝全ての国で 海軍シェア＝分け方・艦隊ゲージ＝軍×分け方・維持費の比＝分け方。⚠突然変異「開幕も旧の播種」で赤。</summary>
    [Fact]
    public void 開幕の艦隊は陸と海の必要量の比で播く()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("日本")], seed: 1789, coef: new Coefficients());
        foreach (var n in sim.Nations) Assert.True(n.World.SelfPower!.FleetGauge == 0, "前提が崩れた：構築の時に艦隊がある");
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 1);
        int seeded = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var p = w.SelfPower!;
            double s = ArmyShape.NavalSpendShareOf(p);
            Assert.True(s >= 0, $"{p.Name}の分け方が未定義");
            Assert.Equal(s, p.NavalShare, 12);
            Assert.Equal(s, w.NavalSpendShare, 12);
            Assert.True(System.Math.Abs(p.FleetGauge - p.Military * s) <= 1e-9 * System.Math.Max(1, p.Military), $"{p.Name}の艦隊ゲージ {p.FleetGauge} ≠ 軍×分け方 {p.Military * s}");
            if (p.FleetGauge > 0) seeded++;
        }
        Assert.True(seeded >= 1, "どの国も艦隊を播いていない（海の必要量が全て0）");
    }

    /// <summary>★(151) 脅威の緊張の届き方＝(国境の長さ＋投射)÷(1＋同)・同盟は0・宗主と属国の間は ×(1−AllyAttentionDiscount)（作者裁定 2026-09-29 の5）。
    /// 国境が長いほど1へ近づく・接しない国は投射だけ。⚠突然変異「届き方を1（旧＝全ての国へ同じ強さ）」「同盟も数える」で赤。</summary>
    [Fact]
    public void 脅威の緊張は届く国だけで同盟は0で国境が長いほど強い()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var fr = sim.Nations[0].World.SelfPower!; var es = sim.Nations[1].World.SelfPower!; var jp = sim.Nations[2].World.SelfPower!;
        double pr = dip.Projection(fr, es, c);
        Assert.Equal(pr / (1 + pr), dip.ThreatReach(fr, es, 0, c), 12);
        Assert.Equal((5 + pr) / (6 + pr), dip.ThreatReach(fr, es, 5, c), 12);
        Assert.True(dip.ThreatReach(fr, es, 5, c) > dip.ThreatReach(fr, es, 1, c) && dip.ThreatReach(fr, es, 1, c) > dip.ThreatReach(fr, es, 0, c), "国境が長いほど強くならない");
        Assert.True(dip.ThreatReach(fr, jp, 0, c) < 1, "接しない国にも満額の脅威");
        dip.SetDependency(fr, jp);
        double pj = dip.Projection(fr, jp, c);
        Assert.Equal(pj / (1 + pj) * (1 - c.AllyAttentionDiscount), dip.ThreatReach(fr, jp, 0, c), 12);
        Ally152(dip, fr, es);
        Assert.Equal(0.0, dip.ThreatReach(fr, es, 5, c));
    }

    /// <summary>★(151) 通商保護の割合＝海に出ている経済の割合＝港の州の取扱高（四半期・金建て）÷（為替×3×月の名目GDP）。港でない州の取扱高は数えない。
    /// ⚠突然変異「港でない州も数える」で赤。</summary>
    [Fact]
    public void 海に出ている経済の割合は港の取扱高を名目GDPで割る()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[1].World;
        int ports = 0;
        foreach (var r in w.Regions) { r.LastPortThroughput = 1000.0; if (r.State == RegionState.Active && r.IsPort) ports++; }
        Assert.True(ports > 0 && ports < w.Regions.Count, "前提が崩れた：仏に港の州と港でない州の両方が無い");
        double expect = ports * 1000.0 / (w.FxRate * 3 * w.NominalGdp);
        Assert.Equal(expect, ArmyShape.SeaTradeShare(w), 12);
    }

    /// <summary>★(151) 陸海の比は目標の生の比（0.05 格子に丸めない＝「陸海比は連続のまま」）。合計 0.75 で海 0.0225（3%）＝旧は丸めて海0（艦隊の乗員ごと消えた）。
    /// ⚠突然変異「丸めた2本の比」で赤。</summary>
    [Fact]
    public void 陸海の比は目標の生の比で丸めない()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        var w = sim.World; var p = w.Policy;
        p.ArmyUpkeepTarget = 0.7275; p.NavyUpkeepTarget = 0.0225;
        w.SetMilitarySpending(0.75, 0.5);
        Government.Step(w, w.Coef);
        Assert.Equal(0.03, w.NavalSpendShare, 9);
    }

    /// <summary>★(151) 呼び手の側：軍需の陸海の再配分（Simulation の月次の Region.ShapeNavalShare）は陸海の維持費と同じ分け方（ArmyShape.NavalSpendShareOf）を読む。
    /// 盤面＝英仏（外交は止める＝必要量と軍の総合ゲージは手で置いたまま）＝軍が必要量の和より大きい（海は任務の分だけ）値を置いて30日回す。
    /// ⚠突然変異「旧の必要量の比（海÷(陸＋内＋海)）」で赤（軍が大きい盤面では2つの式が違う値を出す）。</summary>
    [Fact]
    public void 軍需の陸海の再配分は陸海の維持費と同じ分け方を読む()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var sp = w.SelfPower!;
        sp.Military = 50; sp.NeedSea = 2; sp.NeedLand = 3; sp.NeedInternal = 1;
        for (int d = 0; d < 30; d++) sim.StepDay();
        Assert.True(sp.Military == 50 && sp.NeedSea == 2, "前提が崩れた：外交が走って必要量か軍の総合ゲージが書き換わった");
        double s = ArmyShape.NavalSpendShareOf(sp);
        Assert.Equal(0.04, s, 12);
        int seen = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { Assert.Equal(s, r.ShapeNavalShare, 12); seen++; }
        Assert.True(seen > 0, "前提が崩れた：稼働中の州が無い");
    }

    /// <summary>★(151) 呼び手の側：脅威の緊張（Diplomacy.ThreatTension）は届き方（ThreatReach）で配る＝同盟の組には立たない（作者裁定 2026-09-29 の5「同盟国は計上しない」）。
    /// 盤面＝仏西日（開幕・脅威の緊張のほかの緊張の経路を黙らせ・四半期の境でない月）＝仏西を同盟にして外交を1か月＝仏西の緊張は動かず、脅威の緊張そのものはどこかの組に立つ。
    /// ⚠突然変異「届き方を読まない（全ての国へ同じ強さ＝旧）」で赤。</summary>
    [Fact]
    public void 脅威の緊張は同盟の組に立たない()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var fr = sim.Nations[0].World.SelfPower!; var es = sim.Nations[1].World.SelfPower!; var jp = sim.Nations[2].World.SelfPower!;
        c.IdeologyTensionRate = 0; c.IdeologyAffinityRelief = 0; c.EthnicTensionRate = 0; c.EthnicAffinityRelief = 0;
        c.LawTensionRate = 0; c.ReligionTensionRate = 0; c.ReligionAffinityRelief = 0; c.OpiumBanTensionRate = 0;
        c.TensionDecay = 0; c.ProtectorTensionRate = 0; c.ActionTensionStep = 0; c.RouteDependenceTensionRate = 0;
        Ally152(dip, fr, es);
        double fe0 = dip.Tension(fr, es), fj0 = dip.Tension(fr, jp), ej0 = dip.Tension(es, jp);
        dip.Step(c, sim.Rng, 1);   // 四半期の境でない月＝AI・調停は走らない
        Assert.Equal(fe0, dip.Tension(fr, es));
        Assert.True(dip.Tension(fr, jp) + dip.Tension(es, jp) > fj0 + ej0, "前提が崩れた：脅威の緊張がどの組にも立たない（脅威を持つ国が無い）");
    }
}
