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
            w.Coef.MilitaryEnvelopeShare = 1e9;   // ★(155) 定員＝人口の側（上限割合×母数）で読む（払える予算が律速にならない）
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
    /// ⚠突然変異「頭数で割る（旧）」（請求の単価・扶養限界のどちらか）で赤。
    /// ★(161)：これは**志願制の規定の給与**の物差し（盤面を志願制にした）。徴兵制の兵卒の手当は頭数あたり（世帯の1人ぶんの暮らし）＝下の「徴兵制の兵卒の給与は…」の番人。
    /// ★(163)：徴兵制の兵卒の手当は子供の数で動く（(140) で外した依存を徴兵制の兵卒にだけ戻した）＝番人「徴兵制の兵卒の手当は…子供が増えると下がり…」。</summary>
    [Fact]
    public void 兵の賃金は徴募の母数の働き盛り1人あたりで子供の数に依らない()
    {
        (double wage, double sustain) Arm(bool children)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef; var cap = w.Region;
            w.ConscriptionLaw = ConscriptionLaw.Volunteer;   // ★(161) 規定の給与（志願制）の物差しを見る
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
            return (Military.RecruitWage(cap, c, last: true), Military.EstablishmentOf(w, c, out _));
        }
        var a0 = Arm(false); var a1 = Arm(true);
        Assert.True(a0.wage > 0 && a0.sustain > 0, $"前提が崩れた：賃金 {a0.wage}・扶養限界 {a0.sustain}");
        Assert.True(Math.Abs(a1.wage - a0.wage) <= 1e-9 * a0.wage, $"子供が増えると兵の賃金が動いた（{a0.wage} → {a1.wage}）");
        Assert.True(Math.Abs(a1.sustain - a0.sustain) <= 1e-9 * a0.sustain, $"子供が増えると定員（払える予算÷1人あたりの費用）が動いた（{a0.sustain} → {a1.sustain}）");
    }

    /// <summary>★(144) 合流（反証の指摘＝分子と分母の非対称）：兵役の母数（定員 EstablishmentOf の人口の側と州への按分＝<see cref="Military.RegionManpower"/>）は**徴兵で動かない**
    /// ＝徴募元の項（働き盛りの男＝<see cref="Military.RecruitMen"/>・(145) で男16〜59歳の実数へ）と現兵士の項が同じ単位（兵士も RecruitMen）。
    /// 旧（(142) 4e-2b の段）＝兵士を頭数のまま足した＝男を1人徴兵すると徴募元の項が約6.9 減り兵士の項は1しか増えない＝母数が徴兵のたびに細った。
    /// 盤面＝仏の双子（同じ種で1四半期）。片方だけ首都の農民から男20〜39 を3割徴兵（MoveCohort）してから、国の母数（定員の人口の側の分母）と首都の州の母数（州への按分の分子）を読む＝2つの腕で一致する。
    /// ★(155)：旧は Step の1回の兵数の変化から目標を逆算した（平時の目標へ寄せる専用の経路）＝その経路は削除（兵士は転職の仕組みの職）＝母数そのものを読む。
    /// 今の単位で赤になる変異＝徴募元と兵士の数え方がずれるもの（例＝兵士を規模×標準の構成の働き盛りの男の割合〔0.2765〕で数える＝(149) の突然変異 H3）。</summary>
    [Fact]
    public void 兵役の母数は徴兵で動かない_兵士も徴募元と同じ単位で数える()
    {
        (double natMan, double capMan) Arm(double draftFrac)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef; var cap = w.Region;
            var sld = cap.Pops[cap.SoldierPopIndex];
            Pop? pool = null; foreach (var p in cap.Pops) if (!p.IsSoldier && p.ClassId == c.SoldierRecruitClassA) { pool = p; break; }
            Assert.True(pool is not null && pool.Size > 0 && sld.Size > 0, "前提が崩れた：首都に農民か兵士がいない");
            if (draftFrac > 0) Military.MoveCohort(pool!, sld, draftFrac * pool!.Size * pool.AgeShare(20, 40, 0), c);
            Military.EstablishmentOf(w, c, out double nm);
            return (nm, Military.RegionManpower(cap, c));
        }
        var a = Arm(0); var b = Arm(0.3);
        Assert.True(a.natMan > 0 && a.capMan > 0, $"前提が崩れた：国の母数 {a.natMan}・首都の母数 {a.capMan}");
        Assert.True(System.Math.Abs(b.natMan - a.natMan) <= 1e-9 * a.natMan, $"徴兵で国の母数が動いた（{a.natMan} → {b.natMan}）＝兵士と徴募元の単位が違う");
        Assert.True(System.Math.Abs(b.capMan - a.capMan) <= 1e-9 * a.capMan, $"徴兵で首都の州の母数が動いた（{a.capMan} → {b.capMan}）＝兵士と徴募元の単位が違う");
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
            Assert.True(Math.Abs(used - (cap.Markets[g].LastExport - ex0[g])) <= 5e-7, $"市場から出た量 {used:G17} ≠ 財の出入りの台帳 {cap.Markets[g].LastExport - ex0[g]:G17}");   // 市場から出た量＝財の出入りの台帳（★(161) 6桁の丸めの比較〔丸めの境目で落ちる〕を同じ桁の差へ＝在庫 1e9 の引き算の丸めは約1e-7）
            spent += used * px[g];
        }
        foreach (int g in fleet) Assert.Equal(st0[g], cap.Markets[g].Stock);   // 艦隊の財は買わない
        Assert.True(spent > 0, "陸の軍需を買っていない＝財を買わない国家支出のまま");
        Assert.True(Math.Abs(led0 - w.TreasuryLedger[(int)TreasuryCat.Army] - spent) <= 1e-9 * Math.Max(1.0, spent), "陸軍の区分の国庫の払いが軍需の代金と一致しない");
        double fill = Math.Min(1.0, spent / c.ForceUnlockTreasuryCost);
        Assert.True(Math.Abs(locked.Infra - infra0 - c.ForceUnlockRate * fill) <= 1e-9 * c.ForceUnlockRate, $"進み方 {locked.Infra - infra0} ≠ 速さ×買えた割合 {c.ForceUnlockRate * fill}");
    }

    // ===== (145) 兵の単位（2026-10-01）の番人 =====

    /// <summary>★(155)（旧 (145) の「平時の兵の目標は扶養限界の目標そのもの」の後継）：徴兵制の国は平時に**定員の州按分へ強制で寄せる**（兵士だけの差分＝徴兵は強制で席を埋める）。
    /// 州への按分の分子（州の母数）と分母（国の母数）が同じ規則（<see cref="Military.RegionManpower"/>）＝州の席の和が国の定員（<see cref="Military.EstablishmentOf"/>）に一致する。
    /// 盤面＝仏（徴兵制〔選抜〕・1四半期・平時・器と上限割合を大きくして全ての州で兵が席より少ない＝寄せる向きだけ）＝Step の1回の兵数の変化から州の席を読み（変化＝率×(席−兵数)）、和を国の定員と比べる。
    /// ⚠突然変異「国の母数を国民意識で割り引く（旧 N4）」「席を軍拡の倍率で掛ける（旧）」で赤。</summary>
    [Fact]
    public void 徴兵制は平時に定員の州按分へ強制で寄せ州の席の和が国の定員に一致する()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        var w = sim.World; var c = w.Coef;
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        c.MaxSoldierFraction *= 3; c.MilitaryEnvelopeShare = 1e9;   // 席を今の兵より多く（全ての州で寄せる向き）
        double seats = Military.EstablishmentOf(w, c, out double natMan);
        Assert.True(seats > 0 && natMan > 0, $"前提が崩れた：定員 {seats}・母数 {natMan}");
        var s0 = new Dictionary<Region, double>();
        foreach (var r in w.Regions)
            if (r.State == RegionState.Active)
                foreach (var p in r.Pops) if (p.IsSoldier) { s0[r] = p.Size; break; }
        Military.Step(w, c, atWar: false);
        double sum = 0; int n = 0;
        foreach (var (r, before) in s0)
        {
            double after = 0; foreach (var p in r.Pops) if (p.IsSoldier) { after = p.Size; break; }
            Assert.True(after > before, $"前提が崩れた：{r.Name} で兵が増えない（席が今の兵より少ない）");
            sum += before + (after - before) / c.SoldierMigrationRate; n++;
        }
        Assert.True(n > 1, "前提が崩れた：兵士POPのある州が1つ以下");
        Assert.True(Math.Abs(sum - seats) <= 1e-9 * seats, $"州の席の和 {sum} ≠ 国の定員 {seats}");
    }

    /// <summary>★(155)：志願制の国は Military.Step が兵を強制で集めない（席は転職の仕組みが埋める＝Meritocracy の ⑦・志願は失業プールの職の選択）。
    /// 盤面＝仏の双子（器と上限割合を大きくして席＞兵）＝志願制は平時に1か月の Step で兵数が1ビットも動かない・徴兵制は増える。
    /// ⚠突然変異「志願制も強制で集める」で赤。</summary>
    [Fact]
    public void 志願制はMilitaryで兵を強制で集めず徴兵制だけが席を強制で埋める()
    {
        double Arm(ConscriptionLaw law)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef;
            w.ConscriptionLaw = law;
            c.MaxSoldierFraction *= 3; c.MilitaryEnvelopeShare = 1e9;
            double before = w.SoldierSize();
            Military.Step(w, c, atWar: false);
            return w.SoldierSize() - before;
        }
        double vol = Arm(ConscriptionLaw.Volunteer), sel = Arm(ConscriptionLaw.Selective);
        Assert.Equal(0.0, vol);
        Assert.True(sel > 0, $"徴兵制で席を埋めていない（増え {sel}）");
    }

    /// <summary>★(145)（分子と分母を同じ規則で＝単位をそろえる）：扶養限界の経済の側の兵1人の費用＝賃金（名目）＋装備（参照価格×自己費用の物価水準＝名目）。
    /// 旧＝装備だけ参照価格の単位のまま＝物価の高い国ほど装備が安く見えた。盤面＝仏（人口の側を外す）で自己費用の物価水準を倍にする
    /// ＝兵1人の費用（払える予算〔軍事費の器・(155)〕÷定員）の増えが、参照価格の装備×元の物価水準に一致する。⚠突然変異「物価水準を掛けない」で赤。</summary>
    [Fact]
    public void 扶養限界の装備は自己費用の物価水準で名目へ直す()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        var w = sim.World; var c = w.Coef;
        c.MaxSoldierFraction = 1e9;   // 扶養限界を経済の側で読む
        double equipRef = 0;
        for (int b = 0; b < c.MilitaryGoods.Length; b++) equipRef += c.MilitaryUpkeepScale * c.MilitaryRates[b] * Catalog.RefPrices[c.MilitaryGoods[b]];
        double budget = Military.EnvelopeOf(w, c);   // ★(155) 払える予算＝軍事費の器
        double pl0 = w.CostPriceLevel;
        double lim0 = Military.EstablishmentOf(w, c, out _);
        w.CostPriceLevel = 2.0 * pl0;
        double lim1 = Military.EstablishmentOf(w, c, out _);
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

    /// <summary>★(151) 陸と海の分け方＝軍が必要量の和より大きい国は海の任務の分だけ（残りの兵は陸）＝清の型、軍が足りない国は必要量の比。必要量が全て0（未算出）と NaN は −1（呼び手は今の比のまま）。
    /// ★(189) 式＝海の必要量÷(海の必要量＋max(陸＋内の守りの必要量, 自国の軍の大きさ))（旧＝海÷max(軍, 必要量の和)＝陸の必要量0の島の国は海の割合がほぼ1の錠）＝軍が大きい国は 2÷(2＋100)。
    /// 経済を持たない抽象標的の軍の大きさは軍の総合ゲージ。⚠突然変異「必要量の比だけ（max を外す）」「未算出を0にする」で赤。</summary>
    [Fact]
    public void 海の割合は海の任務の分だけで軍が足りなければ必要量の比()
    {
        var big = new Power { Military = 100, NeedSea = 2, NeedLand = 3, NeedInternal = 1 };
        Assert.Equal(2.0 / 102.0, ArmyShape.NavalSpendShareOf(big), 12);    // 海は任務の分だけ（2÷(2＋100)）＝残りの兵は陸に立つ
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

    /// <summary>★(151) 陸海の維持費の目標の比＝陸と海の必要量の分け方（AI とプレイヤーが同じ関数・合計は保つ）。必要量が未算出なら今の比のまま。⚠突然変異「旧の地理の比へ」で赤。
    /// ★(189) 分母の自国の軍の大きさ＝兵の頭数のゲージ×質（ArmyShape.ForcePotential）＝即応度と装備の充足（症状）を読まない（設計書 §2-7）。
    /// ①軍が陸と内の必要量より小さい＝必要量の比 ②軍が陸と内の必要量より大きい＝海の必要量÷(海の必要量＋自国の軍の大きさ)＝払っている兵の装備の分は陸に残る（陸の必要量が0の島の国の錠を作らない）
    /// ③即応度・上位の充足・軍の総合ゲージを変えても分け方は1ビットも動かない。
    /// ⚠突然変異「分母を軍の総合ゲージ（Military＝即応度つき）へ戻す」で赤（③）。</summary>
    [Fact]
    public void 陸海の維持費の目標は陸と海の必要量で分ける()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var p = w.Policy; var sp = w.SelfPower!;
        p.ArmyUpkeepTarget = 0.45; p.NavyUpkeepTarget = 0.30;
        UtilityAgent.SetNavalUpkeepShare(w);   // 必要量が未算出＝今の比のまま
        Assert.Equal(0.45, p.ArmyUpkeepTarget, 12); Assert.Equal(0.30, p.NavyUpkeepTarget, 12);
        double pot = ArmyShape.ForcePotential(sp);
        Assert.True(pot > 0, "前提が崩れた：自国の軍の大きさが0");
        // ① 軍が足りない＝必要量の比 3÷6
        sp.NeedSea = 3 * pot; sp.NeedLand = 2 * pot; sp.NeedInternal = 1 * pot;
        UtilityAgent.SetNavalUpkeepShare(w);
        Assert.Equal(0.75, p.ArmyUpkeepTarget + p.NavyUpkeepTarget, 12);
        Assert.Equal(0.5, p.NavyUpkeepTarget / (p.ArmyUpkeepTarget + p.NavyUpkeepTarget), 12);
        // ② 軍が陸と内の必要量より大きい＝海の必要量÷(海の必要量＋自国の軍の大きさ)（海は任務の分だけ・残りの兵は陸）
        sp.NeedSea = 0.1 * pot; sp.NeedLand = 0.2 * pot; sp.NeedInternal = 0.1 * pot;
        Assert.Equal(0.1 / 1.1, ArmyShape.NavalSpendShareOf(sp), 12);
        // ②' 陸の必要量が0の島の国（海の必要量が軍より大きい）＝旧は海の割合 1（陸の装備の予算0の錠）・今は兵の装備の分が陸に残る
        sp.NeedSea = 3 * pot; sp.NeedLand = 0; sp.NeedInternal = 0;
        Assert.Equal(0.75, ArmyShape.NavalSpendShareOf(sp), 12);
        // ③ 症状を読まない
        double s0 = ArmyShape.NavalSpendShareOf(sp);
        sp.Military = pot * 0.01; w.MilitaryReadiness = w.MilitaryReadiness * 0.1; w.Region.MilitaryQualityFill = 1.0;
        Assert.Equal(s0, ArmyShape.NavalSpendShareOf(sp));
        Assert.Equal(pot, ArmyShape.ForcePotential(sp));
    }

    /// <summary>★(151) 開幕（00一覧 §0-A の 11 の申し送り「開幕の陸海の維持費 0.27/0.03 が最初の四半期に跳ぶ」）＝最初の同期で陸と海の必要量を算出し、
    /// 陸海の維持費の比と艦隊の播種を同じ比で置く（旧＝海軍シェアは0から建艦の時間遅れで比の3割・艦隊もその3割で播いた）。
    /// 盤面＝英仏日（構築の直後に外交を1か月）＝全ての国で 艦の強さのゲージ＝軍×分け方・維持費の比＝分け方。⚠突然変異「開幕も旧の播種」で赤。
    /// ★(189)：開幕の艦隊＝目標の艦隊（海の必要量÷海の熟練を 1783 年に発見済みの艦＝帆船の軍艦で割り付け＝陸の装備の開幕の武器庫と同じ「未初期化は目標」）・港の州が無い国は播かない
    ///   ・海軍シェアは置かない（乗員÷兵士＝播いた艦から導く・乗員の充足は1＝兵士は乗員より多い）。⚠突然変異「旧の軍×分け方で播く」で赤。</summary>
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
            Assert.Equal(s, w.NavalSpendShare, 12);
            Assert.Equal(1.0, p.CrewFill);
            Assert.True(p.NavalShare >= 0 && p.NavalShare < s + 1e-12, $"{p.Name}の海軍シェア（乗員÷兵士）{p.NavalShare} が分け方 {s} を超える");
            double target = p.NeedSea / p.NavySkillMul;
            Assert.True(System.Math.Abs(p.FleetGauge - target) <= 1e-9 * System.Math.Max(1, target), $"{p.Name}の艦の強さのゲージ {p.FleetGauge} ≠ 目標（海の必要量÷海の熟練）{target}");
            if (w.Regions[0].FleetUnits is { } fu)
                for (int k = 0; k < fu.Length; k++) if (w.Coef.FleetGoodIds[k] != Catalog.WarshipId) Assert.Equal(0.0, fu[k]);   // 1783 年＝装甲艦・弩級戦艦は未発見
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

    // （(151) の「海に出ている経済の割合は港の取扱高を名目GDPで割る」は 2026-10-03 (183) に関数 ArmyShape.SeaTradeShare ごと削除＝読み手〔通商保護の項〕を外した）

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

    /// <summary>★(189) 呼び手の側：海軍の予算（Simulation の月次の Region.NavalBudget）＝装備の器（軍事費の器から給与を払った残り）×海の金の割合（World.NavalSpendShare＝施行された陸海の維持費の比）・
    /// 陸の装備の上限（MilitaryBudgetCeiling）＝装備の器×(1−海の金の割合)＝作者 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」の金の分け方。
    /// 盤面＝英仏（外交は止める＝艦は播かれない・海の必要量は手で大きく置く＝目標の艦隊は在庫より大きい＝必要額で切られない）＝30日回すと首都の 海軍の予算÷陸の装備の上限＝割合÷(1−割合)
    /// （どちらも同じ装備の器と同じ国家の支払い率 r を掛ける＝比は r に依らない）。首都の外の州は0。
    /// ⚠突然変異「陸の装備の上限を装備の器の全体のまま（旧）」「海軍の予算を分けない」で赤。</summary>
    [Fact]
    public void 海軍の予算は装備の器を海の金の割合で分ける()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var sp = w.SelfPower!;
        sp.NeedSea = 50; sp.NeedLand = 3; sp.NeedInternal = 1;
        for (int d = 0; d < 30; d++) sim.StepDay();
        Assert.True(sp.NeedSea == 50, "前提が崩れた：外交が走って海の必要量が書き換わった");
        double s = w.NavalSpendShare;
        Assert.True(s > 0.01 && s < 0.99, $"前提が崩れた：海の金の割合 {s}");
        var cap = w.Region;
        Assert.True(cap.NavalBudget > 0 && cap.MilitaryBudgetCeiling > 0, $"前提が崩れた：海軍の予算 {cap.NavalBudget}・陸の装備の上限 {cap.MilitaryBudgetCeiling}");
        Assert.Equal(s / (1 - s), cap.NavalBudget / cap.MilitaryBudgetCeiling, 9);
        foreach (var r in w.Regions) if (!ReferenceEquals(r, cap)) Assert.Equal(0.0, r.NavalBudget);
    }

    /// <summary>★(189) 海軍力は艦の在庫×段の効き×乗員の充足×海の熟練から決まり、陸軍の即応度と陸の質（Military の乗数）を読まない
    /// （作者の裁定 2026-08-01「陸軍は兵士ありきですが、海軍は戦艦ありきです。海軍の強さが海兵の数から決まるのはナンセンス」・(187) 反証役 va1#1＝00一覧 §0-A の 0h）。
    /// 盤面＝英仏（構築の直後に外交を1か月＝開幕の播種）。①陸の質の充足と即応度を変えて外交をもう1か月＝軍の総合ゲージは動くが英の海軍力は1ビットも動かない
    /// ②英の首都に装甲艦を1隻足して同期＝艦の強さのゲージが建造費×(1＋上位の乗数)÷換算分母だけ増える（段の効き）・弩級戦艦は×(1＋上位＋最上位)
    /// ③要る乗員＝隻数×1隻の乗員（艦の種類に依らない）＝装甲艦1隻は帆船の軍艦1隻と同じ乗員。
    /// ⚠突然変異「旧の min(艦隊, 軍×海軍シェア)」「段の乗数を1」「乗員を艦の強さから数える」で赤。</summary>
    [Fact]
    public void 海軍力は艦の在庫と段の効きから決まり陸軍の即応度と質を読まない()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients());
        var c = sim.Coef;
        c.NavalAttritionRate = 0;   // 交戦の艦隊摩耗を外す（艦の在庫を変えるもう1つの入力）
        sim.Diplomacy.Step(c, sim.Rng, 1);   // 開幕の同期＝艦の播種
        var w = sim.Nations[0].World; var uk = w.SelfPower!;
        Assert.True(uk.FleetGauge > 0 && uk.NavalPower > 0, "前提が崩れた：英に艦が無い");
        double naval0 = uk.NavalPower, mil0 = uk.Military;
        // ① 陸の質と即応度
        w.Region.MilitaryQualityFill = 1.0; w.Region.MilitaryTier3Fill = 1.0;
        w.MilitaryReadiness = w.MilitaryReadiness * 0.25;
        sim.Diplomacy.Step(c, sim.Rng, 2);
        Assert.NotEqual(mil0, uk.Military);   // 前提＝軍の総合ゲージは動いた
        Assert.Equal(naval0, uk.NavalPower);  // 海軍力は動かない
        // ② 段の効き
        double denom = EconomyStep.FleetGaugeDenominator(w.Regions[0], w.Coef);
        int ki = System.Array.IndexOf(c.FleetGoodIds, Catalog.IroncladId), kd = System.Array.IndexOf(c.FleetGoodIds, Catalog.DreadnoughtId), kw = System.Array.IndexOf(c.FleetGoodIds, Catalog.WarshipId);
        double g0 = uk.FleetGauge, share0 = uk.NavalShare;
        w.Regions[0].FleetUnits![ki] += 1.0;
        sim.Diplomacy.Step(c, sim.Rng, 4);
        Assert.Equal((Catalog.RefPrices[Catalog.IroncladId] * (1 + c.MilitaryQualityBonus)) / denom, uk.FleetGauge - g0, 9);
        double g1 = uk.FleetGauge;
        w.Regions[0].FleetUnits![kd] += 1.0;
        sim.Diplomacy.Step(c, sim.Rng, 5);
        Assert.Equal((Catalog.RefPrices[Catalog.DreadnoughtId] * (1 + c.MilitaryQualityBonus + c.MilitaryTier3Bonus)) / denom, uk.FleetGauge - g1, 9);
        // ③ 乗員は隻数×1隻の乗員（2隻足した分＝帆船の軍艦2隻と同じ）
        double perHull = EconomyStep.FleetCrewPerHull(w.Regions[0], w.Coef, denom);
        Assert.Equal(c.NavalCrewPerGauge * c.SoldiersPerMilitary * Catalog.RefPrices[Catalog.WarshipId] / denom, perHull, 12);
        Assert.Equal(share0 + 2.0 * perHull / w.SoldierSize(), uk.NavalShare, 9);
        Assert.True(kw >= 0 && w.Regions[0].FleetUnits![kw] > 0, "前提が崩れた：開幕の艦が帆船の軍艦で無い");
    }

    /// <summary>★(189) 軍艦の購入（EconomyStep.FleetOrder＝海軍の調達）は兵の頭数（MilitaryScale）に比例せず、目標の艦隊（海の必要量÷海の熟練）と在庫の差で決まる
    /// （(187) 反証役 va1#1「軍艦の購入も兵の頭数×海の割合」の直し）。盤面＝英仏（外交は止める＝艦は無い）。
    /// ①兵の頭数を10倍にしても発注は1ビットも変わらない ②海の必要量を2倍にすると発注が2倍 ③在庫が目標に届けば発注しない ④港の州が無い国は買わない
    /// ⑤まだ発見していない艦（装甲艦・弩級戦艦）は発注しない。⚠突然変異「発注に MilitaryScale を掛ける」「目標に在庫を引かない」で赤。</summary>
    [Fact]
    public void 軍艦の購入は兵の頭数に比例せず海の必要量と在庫の差で決まる()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var sp = w.SelfPower!; var r = w.Region; var c = w.Coef;
        r.NationHasPort = true;
        sp.NeedSea = 0.4; sp.NavySkillMul = 1.0;
        var q0 = EconomyStep.FleetOrder(r, c, w, out double cost0);
        Assert.NotNull(q0);
        Assert.True(cost0 > 0, "前提が崩れた：必要額0");
        int kw = System.Array.IndexOf(c.FleetGoodIds, Catalog.WarshipId), ki = System.Array.IndexOf(c.FleetGoodIds, Catalog.IroncladId), kd = System.Array.IndexOf(c.FleetGoodIds, Catalog.DreadnoughtId);
        Assert.True(q0![kw] > 0, "前提が崩れた：帆船の軍艦を発注しない");
        Assert.Equal(0.0, q0[ki]); Assert.Equal(0.0, q0[kd]);   // ⑤ 1783 年＝装甲艦・弩級戦艦は未発見
        // ① 兵の頭数
        double ms = r.MilitaryScale, msp = r.MilitaryScalePeace;
        r.MilitaryScale = ms * 10 + 1; r.MilitaryScalePeace = msp * 10 + 1;
        var q1 = EconomyStep.FleetOrder(r, c, w, out double cost1);
        Assert.Equal(q0[kw], q1![kw]); Assert.Equal(cost0, cost1);
        // ② 海の必要量
        sp.NeedSea = 0.8;
        var q2 = EconomyStep.FleetOrder(r, c, w, out _);
        Assert.Equal(2 * q0[kw], q2![kw], 9);
        // ③ 在庫が目標に届く
        (r.FleetUnits ??= new double[c.FleetGoodIds.Length])[kw] = q2[kw];
        Assert.Null(EconomyStep.FleetOrder(r, c, w, out double cost3));
        Assert.Equal(0.0, cost3);
        r.FleetUnits[kw] = 0.5 * q2[kw];
        Assert.Equal(0.5 * q2[kw], EconomyStep.FleetOrder(r, c, w, out _)![kw], 9);
        // ④ 港の州が無い国
        r.NationHasPort = false;
        Assert.Null(EconomyStep.FleetOrder(r, c, w, out _));
        Assert.Equal(0.0, EconomyStep.NavalProcurementNeed(r, c, w));
    }

    /// <summary>★(189) 陸の装備の段は艦を買わない（EconomyStep.LandMul＝艦は0）＝陸の装備の必要額は軍艦の値段に依らない・上位と最上位の充足は陸分と同じ
    /// （艦の段の効きは海軍力の1か所＝思想⑨・旧＝陸海の再配分で艦も陸の段で買い、装甲艦・弩級戦艦が Power.Military の質の乗数に入った）。
    /// 盤面＝英仏（外交は止める）を1か月＝首都の陸の装備の必要額を軍艦・装甲艦・弩級戦艦の値段を100倍にして測り直しても同じ・充足の合算＝陸分。
    /// ⚠突然変異「LandMul を常に1（艦も陸の段で買う）」で赤。</summary>
    [Fact]
    public void 陸の装備の段は艦を買わない()
    {
        var sim = Frozen152("イギリス", "フランス");
        var w = sim.Nations[0].World; var r = w.Region; var c = w.Coef;
        for (int d = 0; d < 30; d++) sim.StepDay();
        Assert.True(r.MilitaryScale > 0, "前提が崩れた：首都の軍需の規模が0");
        double need0 = EconomyStep.MilitaryProcurementNeed(r, c);
        Assert.True(need0 > 0, "前提が崩れた：陸の装備の必要額が0");
        foreach (int g in c.FleetGoodIds) r.Markets[g].Price *= 100;
        Assert.Equal(need0, EconomyStep.MilitaryProcurementNeed(r, c));
        foreach (int g in c.FleetGoodIds) Assert.Equal(0.0, EconomyStep.LandMul(c, g));
        Assert.Equal(1.0, EconomyStep.LandMul(c, Catalog.RifleId));
        Assert.Equal(r.MilitaryQualityLandFill, r.MilitaryQualityFill);
        Assert.Equal(r.MilitaryTier3LandFill, r.MilitaryTier3Fill);
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

    // ===== (154) 呼び手の配線の番人（(151) の反証役の指摘＝Borders・SeaTradeShare・ThreatReach は単体で試しているが、Compute と ThreatTension が読む配線は見ていなかった・(183) で SeaTradeShare は削除） =====

    /// <summary>★(154) 理想の軍隊の形（<see cref="ArmyShape.Compute"/>）は国境の長さを陸の必要量に読む（作者裁定 2026-09-29「国境線が長いほど軍事費が必要になる」の配線）。
    /// 盤面＝仏・西・日（開幕・外交は止める）。仏の州を全て山にすると仏西の国境の重みだけが下がる（投射・敵意・不穏度は地形を読まない）＝西の陸軍のうち仏へ向く分が減り、仏の陸の必要量が下がる。
    /// ⚠突然変異「陸の到達から国境を外す」で赤（地形を変えても陸の必要量が1ビットも動かない）。</summary>
    [Fact]
    public void 理想の軍隊の形は国境の長さを陸の必要量に読む()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var fr = sim.Nations[0].World.SelfPower!;
        ArmyShape.Compute(dip, c);
        double before = fr.NeedLand;
        Assert.True(before > 0, $"前提が崩れた：仏の陸の必要量が0（{before}）");
        foreach (var r in sim.Nations[0].World.Regions) r.Terrain = Terrain.Mountain;
        ArmyShape.Compute(dip, c);
        Assert.True(fr.NeedLand < before * (1 - 1e-9), $"仏を山にしても陸の必要量が下がらない（{before:G17}→{fr.NeedLand:G17}）＝Compute が国境の長さを読んでいない");
    }

    /// <summary>★(183) 理想の軍隊の形の海の必要量は「自国の軍×海に出ている経済の割合」を足さない（海軍は陸軍とは別のベース＝作者 2026-09-29「陸軍は安全保障ベース、海軍は別のベースで、軍事費を分割するようにしよう」・推奨で進めた）。
    /// 旧（(151)〜(182)・(154) の番人「守る海の交易を海の必要量に足す」）＝自国の軍の総合ゲージ×港の取扱高÷名目GDP を足した＝海の大きさが自国の陸軍の大きさに比例した（1913 の清の海の必要量の約68%・r182）。
    /// 盤面＝英日（開幕・外交は止める・海を越えて来る日本の陸軍で英の海の必要量は正）。英の港の取扱高を0から正へ変えても英の海の必要量は1ビットも動かない
    /// （海の必要量＝相手の艦隊の注意配分＋海を越えて来る相手の陸軍＝取扱高を読まない）。
    /// ⚠突然変異「通商保護の行を戻す（軍×港の州の取扱高÷(為替×3×名目GDP)）」で赤。</summary>
    [Fact]
    public void 理想の軍隊の形の海の必要量は自国の軍と海の交易の割合を足さない()
    {
        var sim = Frozen152("イギリス", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var ukw = sim.Nations[0].World; var uk = ukw.SelfPower!;
        foreach (var nn in sim.Nations) foreach (var r in nn.World.Regions) r.LastPortThroughput = 0;
        ArmyShape.Compute(dip, c);
        double sea0 = uk.NeedSea;
        Assert.True(sea0 > 0, $"前提が崩れた：英の海の必要量が0（{sea0}）＝海を越えて来る日本の陸軍が数えられていない");
        int ports = 0;
        foreach (var r in ukw.Regions) { r.LastPortThroughput = 1000.0; if (r.State == RegionState.Active && r.IsPort) ports++; }
        Assert.True(ports > 0 && uk.Military > 0 && ukw.NominalGdp > 0, $"前提が崩れた：港の州 {ports}・軍 {uk.Military}・名目GDP {ukw.NominalGdp}（旧の項なら正の差が出る盤面）");
        ArmyShape.Compute(dip, c);
        Assert.Equal(sea0, uk.NeedSea);
    }

    /// <summary>★(154) 脅威の緊張（Diplomacy.ThreatTension）は国境の長さを届き方（<see cref="Diplomacy.ThreatReach"/>）へ渡す（「国境を接している国は脅威だが、そうでない国はあまり脅威ではない」の配線）。
    /// 盤面＝仏・西・日を2つ（開幕・他の緊張の源を0）。片方は仏の州を全て山にする（仏西の国境の重みだけが下がる）＝仏西の組に立つ脅威の緊張が小さくなる。
    /// ⚠突然変異「国境に0を渡す」で赤（2つの盤面で同じ緊張が立つ）。</summary>
    [Fact]
    public void 脅威の緊張は国境の長い組ほど強く立つ()
    {
        static double Rise(bool mountain)
        {
            var sim = Frozen152("フランス", "スペイン", "日本");
            var dip = sim.Diplomacy; var c = sim.Coef;
            var fr = sim.Nations[0].World.SelfPower!; var es = sim.Nations[1].World.SelfPower!;
            c.IdeologyTensionRate = 0; c.IdeologyAffinityRelief = 0; c.EthnicTensionRate = 0; c.EthnicAffinityRelief = 0;
            c.LawTensionRate = 0; c.ReligionTensionRate = 0; c.ReligionAffinityRelief = 0; c.OpiumBanTensionRate = 0;
            c.TensionDecay = 0; c.ProtectorTensionRate = 0; c.ActionTensionStep = 0; c.RouteDependenceTensionRate = 0;
            if (mountain) foreach (var r in sim.Nations[0].World.Regions) r.Terrain = Terrain.Mountain;
            double t0 = dip.Tension(fr, es);
            dip.Step(c, sim.Rng, 1);   // 四半期の境でない月＝AI・調停は走らない
            return dip.Tension(fr, es) - t0;
        }
        double flat = Rise(false), mount = Rise(true);
        Assert.True(flat > 0, $"前提が崩れた：仏西の組に脅威の緊張が立たない（{flat}）");
        Assert.True(mount < flat * (1 - 1e-9), $"国境の重みを下げても仏西の脅威の緊張が弱まらない（{flat:G17}→{mount:G17}）＝ThreatTension が国境の長さを届き方へ渡していない");
    }

    // ===== (155) 軍の労働市場と安全保障の不安（2026-10-02・作者 2026-10-01「軍事費を上げる→兵士の給与が上がる→兵士を志すPOPが増える→兵士が増える という経路は残したい」・
    //       「低い軍事費は軍隊POPの流出を招き、「理想の軍隊」に届かない兵数になる」・兵士を転職の仕組みへ「それでよい。ただし兵士は他の職とは性質が違うので、多少の外科的な処置は許可する」）=====

    private static Simulation France155()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);
        return sim;
    }

    private static Pop? SoldierOf(Region r) { foreach (var p in r.Pops) if (p.IsSoldier) return p; return null; }

    /// <summary>★(155) 定員の経済の側＝払える予算（軍事費の器＝軍事費の段×MilitaryEnvelopeShare×使える歳入）÷1人あたりの費用＝**軍事費の段に比例**
    /// （(145) の「軍事費の段は平時の兵数を動かさない」の解消・英の陸軍予算の型）。盤面＝仏（人口の側を外す）で段を 0.25→0.5＝定員が2倍。
    /// ⚠突然変異「器が軍事費の段を読まない（旧 SustainMilShare の形）」で赤。</summary>
    [Fact]
    public void 定員の経済の側は軍事費の段に比例する()
    {
        var w = France155().World; var c = w.Coef;
        c.MaxSoldierFraction = 1e9;   // 人口の側を外す
        w.SetMilitarySpending(0.25, w.NavalSpendShare);
        double s1 = Military.EstablishmentOf(w, c, out _);
        w.SetMilitarySpending(0.5, w.NavalSpendShare);
        double s2 = Military.EstablishmentOf(w, c, out _);
        Assert.True(s1 > 0, $"前提が崩れた：定員 {s1}");
        Assert.True(Math.Abs(s2 - 2 * s1) <= 1e-9 * s2, $"段を倍にしても定員が倍にならない（{s1} → {s2}）");
    }

    /// <summary>★(155) 平時に定員を超えた兵は転職の仕組み（Meritocracy の ⑦）が民間の最良の職へ戻す＝超過×旧の徴募/離職の速さの四半期の値（1−(1−SoldierMigrationRate)^3）・人口は保存。
    /// 戦時は戻さない（兵士だけの差分＝戦時は復員しない）。盤面＝仏英（器をほぼ0＝全ての州で超過・未払いの脱走を外す）で平時と戦時の Meritocracy の1回。
    /// ⚠突然変異「戦時も戻す」「戻さない」で赤。</summary>
    [Fact]
    public void 平時に定員を超えた兵は民間の職へ戻り戦時は戻らない()
    {
        (double before, double after, double expect, double pop0, double pop1) Arm(bool war)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            var w = sim.Nations[0].World; var c = w.Coef;
            if (war) sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
            c.MilitaryEnvelopeShare = 1e-12;   // 席をほぼ0＝全ての州で超過
            double seatsNat = Military.EstablishmentOf(w, c, out double natMan);
            double dq = 1.0 - Math.Pow(1.0 - c.SoldierMigrationRate, Calendar.MonthsPerQuarter), expect = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || SoldierOf(r) is not { } s) continue;
                s.UnemploymentRate = 0; s.UnemploymentEma = 0;   // 未払いの脱走（①''）を外す
                if (natMan > 0) expect += dq * Math.Max(0, s.Size - seatsNat * Military.RegionManpower(r, c) / natMan);
            }
            double b = w.SoldierSize(), p0 = w.TotalPopulation;
            Meritocracy.Step(w, c);
            return (b, w.SoldierSize(), expect, p0, w.TotalPopulation);
        }
        var peace = Arm(false); var war = Arm(true);
        Assert.True(peace.expect > 0, "前提が崩れた：定員の超過が無い");
        Assert.True(Math.Abs((peace.before - peace.after) - peace.expect) <= 1e-9 * peace.expect, $"平時の復員 {peace.before - peace.after} ≠ 超過×速さ {peace.expect}");
        Assert.True(Math.Abs(peace.pop1 - peace.pop0) <= 1e-9 * peace.pop0, "復員で人口が保存されない");
        Assert.True(war.after >= war.before - 1e-9, $"戦時に復員した（{war.before} → {war.after}）");
    }

    /// <summary>★(155) 志願＝失業プール（①''）の職の選択に兵士の席が入る＝**席の空きまで**・入るのは徴兵の年齢の男だけ（Military.MoveCohort の人の移し方＝世帯ごとでない）・新兵は熟練を薄める。
    /// 盤面＝仏（志願制・州ごとに小さな空き〔上限割合を最も詰まった州の兵÷母数の 1.01 倍〕・徴募元の失業 0.3・兵士の給与を10倍＝魅力のある職）。
    /// ⚠突然変異「空きで切らない」「兵士を候補にしない」「世帯ごとに移す」「志願で薄めない」で赤。</summary>
    [Fact]
    public void 志願は失業プールから席の空きまでで徴兵の年齢の男だけが入り熟練を薄める()
    {
        var w = France155().World; var c = w.Coef;
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
        c.MilitaryEnvelopeShare = 1e9;
        double fill = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || SoldierOf(r) is not { } s) continue;
            double man = Military.RegionManpower(r, c);
            if (man > 0) fill = Math.Max(fill, s.Size / man);
        }
        c.MaxSoldierFraction = fill * 1.01;   // 全ての州で席＞兵（空きは小さい）
        var vac = new Dictionary<Region, (Pop s, double before, double vac)>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || SoldierOf(r) is not { } s) continue;
            s.UnemploymentRate = 0; s.UnemploymentEma = 0; s.Earned *= 10;
            vac[r] = (s, s.Size, c.MaxSoldierFraction * Military.RegionManpower(r, c) - s.Size);
            foreach (var p in r.Pops)
                if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB)) { p.UnemploymentRate = 0.3; p.UnemploymentEma = 0.3; }
        }
        w.ArmySkill = 0.5; w.NavySkill = 0.5;
        double s0 = w.SoldierSize();
        Meritocracy.Step(w, c);
        double s1 = w.SoldierSize(), gained = 0;
        foreach (var (r, v) in vac)
        {
            double inc = v.s.Size - v.before;
            Assert.True(inc <= v.vac * (1 + 1e-9) + 1e-12, $"{r.Name}：志願 {inc} が席の空き {v.vac} を超えた");
            Assert.True(v.s.AgeShare(0, 1000, 1) <= 1e-9, $"{r.Name}：兵士 POP に女性が入った（世帯ごとに移した）");
            gained += Math.Max(0, inc);
        }
        Assert.True(gained > 0, "志願が席に入らない（兵士が職の候補になっていない）");
        Assert.True(Math.Abs(w.ArmySkill - 0.5 * (s1 - gained) / s1) <= 1e-9, $"志願の新兵で熟練が薄まらない（{w.ArmySkill} 期待 {0.5 * (s1 - gained) / s1}）");
        Assert.True(s1 > s0);
    }

    /// <summary>★(155) 未払いの兵は脱走して民間の職へ（兵士の「失業」＝給与の未払いの割合・多年EMA を失業プール ①'' が読む＝強制＝席に留まれない）。
    /// 盤面＝仏の双子（器を大きく・上限割合をどの州でも席≦兵に〔空きが無い＝志願の空きの係数（1−未払い）で差が出ない・超過の復員は双子で同じ〕）で兵士の未払いの割合だけ 0 と 0.5 ＝未払いの腕だけ兵が減り、人口は同じ。
    /// ⚠突然変異「兵士を ①'' から外す（旧）」で赤。</summary>
    [Fact]
    public void 未払いの兵は脱走して民間の職へ移る()
    {
        (double sold, double pop) Arm(double unpaid)
        {
            var w = France155().World; var c = w.Coef;
            c.MilitaryEnvelopeShare = 1e9;
            foreach (var r in w.Regions) if (r.State == RegionState.Active && SoldierOf(r) is { } s) { s.UnemploymentRate = unpaid; s.UnemploymentEma = unpaid; }
            // 席＝どの州でも今の兵以下（空きを作らない＝志願の空きの係数〔1−未払い〕で差が出ないように・超過の復員は双子で同じ）
            double fill = double.PositiveInfinity;
            foreach (var r in w.Regions)
                if (r.State == RegionState.Active && SoldierOf(r) is { } s2 && Military.RegionManpower(r, c) is var man && man > 0) fill = Math.Min(fill, s2.Size / man);
            c.MaxSoldierFraction = fill;
            Meritocracy.Step(w, c);
            return (w.SoldierSize(), w.TotalPopulation);
        }
        var paid = Arm(0); var unpaid = Arm(0.5);
        Assert.True(unpaid.sold < paid.sold * (1 - 1e-6), $"未払いの兵が脱走しない（払う {paid.sold} / 未払い {unpaid.sold}）");
        Assert.True(Math.Abs(unpaid.pop - paid.pop) <= 1e-9 * paid.pop, "脱走で人口が保存されない");
    }

    /// <summary>★(155) 兵士の「失業」＝1−国家の支払い率×器の給与の充足（月初の予算・Simulation）。盤面＝仏の双子で器を大きく／ほぼ0 にして1か月＝
    /// 大きい器は 1−r・ほぼ0 の器は ほぼ1（給与が払えない）。⚠突然変異「器の給与の充足を読まない（1−r だけ）」で赤。</summary>
    [Fact]
    public void 兵士の失業は器の給与の充足と国家の支払い率で決まる()
    {
        (double u, double r) Arm(double env)
        {
            var sim = France155(); var w = sim.World;
            w.Coef.MilitaryEnvelopeShare = env;
            for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
            var s = w.Region.Pops[w.Region.SoldierPopIndex];
            return (s.UnemploymentRate, w.FiscalFundingRatio);
        }
        var big = Arm(1e9); var tiny = Arm(1e-12);
        Assert.True(Math.Abs(big.u - (1 - big.r)) <= 1e-12, $"大きい器で未払い {big.u} ≠ 1−r {1 - big.r}");
        Assert.True(tiny.u > 0.99, $"器がほぼ0 なのに未払いが立たない（{tiny.u}）");
    }

    /// <summary>★(155) 軍事費の吸い込みの順＝給与・糧食 → 装備（全4段の必要額まで）→ 質（幹部の給与＝規定の給与の満額×MilitaryCadrePayRatio まで）→ 使い切れない分は請求しない。
    /// 盤面＝仏の双子（1四半期の後に係数だけ替えて1か月）：(i) 器が大きい・幹部 1＝訓練の充足 1・兵士の給与の請求＝(ii) の2倍／(ii) 幹部 0＝訓練 0／(iii) 器がほぼ0＝装備も訓練も0／
    /// (iv) 器が大きい・幹部 1e15（質の必要額が器を超える）＝装備は (ii) と同じ（質は装備の後）。⚠突然変異「質を装備より先に」「幹部の給与を兵士の給与へ足さない」で赤。
    /// ★(161)：盤面は志願制（兵卒の給与＝規定の給与＝幹部の給与の物差し＝(i) が (ii) の2倍）。</summary>
    [Fact]
    public void 軍事費の器は給与から装備から質の順に吸われ使い切れない分は請求しない()
    {
        (double pay, double equip, double train, double env) Arm(double envShare, double cadre)
        {
            var sim = France155(); var w = sim.World; var c = w.Coef;
            w.ConscriptionLaw = ConscriptionLaw.Volunteer; w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer;   // ★(161) 志願制＝兵卒と幹部の単価が同じ規定の給与（徴兵制の手当は「徴兵制の兵卒の給与は…」の番人）・施行の目標も同じ法（月の施行が戻さない）
            c.MilitaryEnvelopeShare = envShare; c.MilitaryCadrePayRatio = cadre;
            for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
            return (w.LastSoldierPay, w.LastMilitaryClaim, w.MilitaryTrainingFill, Military.EnvelopeOf(w, c));
        }
        var i = Arm(1e9, 1.0); var ii = Arm(1e9, 0.0); var iii = Arm(1e-12, 1.0); var iv = Arm(1e9, 1e15);   // (iv) 質の必要額が器より大きい
        Assert.True(ii.pay > 0 && ii.equip > 0, $"前提が崩れた：給与 {ii.pay}・装備 {ii.equip}");
        Assert.Equal(1.0, i.train);
        Assert.Equal(0.0, ii.train);
        Assert.True(Math.Abs(i.pay - 2 * ii.pay) <= 1e-9 * i.pay, $"幹部の給与が兵士の給与の請求に入らない（{i.pay} vs 2×{ii.pay}）");
        Assert.True(Math.Abs(i.equip - ii.equip) <= 1e-9 * ii.equip, "幹部の給与で装備が変わった（装備は質より先）");
        Assert.True(Math.Abs(iv.equip - ii.equip) <= 1e-9 * ii.equip, $"質の必要額が大きいと装備が削られた（{ii.equip} → {iv.equip}）＝質が装備より先");
        Assert.True(iii.equip <= 1e-9 * ii.equip && iii.train <= 1e-9, $"器がほぼ0 なのに装備 {iii.equip}・訓練 {iii.train} を請求した（給与が先）");
        Assert.True(i.pay + i.equip < 1e-3 * i.env, "使い切れない器を請求した（出費の頭打ちが無い）");
    }

    /// <summary>★(155) 熟練は払った訓練（器の3段目の充足＝World.MilitaryTrainingFill）で育ち、維持費の段（旧＝ArmyUpkeep が閾値0.5 を超えるか）を読まない。陸と海は同じ1本。
    /// 新兵（徴兵）は熟練を薄める（入る前の兵÷入った後の兵）。⚠突然変異「維持費の段を読む（旧）」「徴兵で薄めない」で赤。</summary>
    [Fact]
    public void 熟練は払った訓練で育ち徴兵の新兵で薄まる()
    {
        var w = France155().World; var c = w.Coef;
        w.SetMilitaryUpkeep(1.0, 0.0);   // 段は最上段（旧の規則なら育つ）
        w.MilitaryTrainingFill = 0.0; w.ArmySkill = 0.4; w.NavySkill = 0.4;
        Military.StepSkill(w, c, atWar: false);
        Assert.True(w.ArmySkill < 0.4 && w.NavySkill < 0.4, $"訓練が払われないのに熟練が育った（陸 {w.ArmySkill}・海 {w.NavySkill}）");
        w.SetMilitaryUpkeep(0.0, 0.0);
        w.MilitaryTrainingFill = 1.0; w.ArmySkill = 0.4; w.NavySkill = 0.4;
        Military.StepSkill(w, c, atWar: false);
        double expect = 0.4 + c.SkillGainRate * (1.0 - c.SkillUpkeepThreshold) * 0.6;
        Assert.Equal(expect, w.ArmySkill, 12);
        Assert.Equal(expect, w.NavySkill, 12);
        // 徴兵の新兵で薄める（呼び手＝Military.Step）
        w.SetMilitarySpending(0.5, 0.1);
        w.ConscriptionLaw = ConscriptionLaw.Selective; c.MaxSoldierFraction *= 3; c.MilitaryEnvelopeShare = 1e9;
        w.ArmySkill = 0.6; w.NavySkill = 0.6;
        double before = w.SoldierSize();
        Military.Step(w, c, atWar: false);
        double after = w.SoldierSize();
        Assert.True(after > before, "前提が崩れた：徴兵で兵が増えない");
        Assert.Equal(0.6 * before / after, w.ArmySkill, 12);
        Assert.Equal(0.6 * before / after, w.NavySkill, 12);
    }

    /// <summary>★(155) 安全保障の不安（エンジンの SecurityThreat）＝相手の養える軍（定員＝選んだ軍事費の段）のうち自国に向く注意配分（(151) の理想の軍隊の形と同じ物差し＝敵意×(国境＋投射)÷(内向き＋相手の全ての向き)）の和。
    /// 盤面＝仏・西・日（開幕・外交を止める）：(a) 式どおり（公開の部品で数え直して一致）(b) 世界の脅威の和は世界の養える軍の和より小さい（注意配分）(c) 西の軍事費の段を上げると仏の不安が増える
    /// （相手の選んだ段を読む）(d) 仏西を同盟にすると仏の不安が減る。⚠突然変異「注意配分で割らない」「相手の今の兵数を読む」「同盟も数える」で赤。</summary>
    [Fact]
    public void 安全保障の不安は相手の養える軍を注意配分で数え同盟は数えない()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var c = sim.Coef;   // 共有層の係数（敵意・国境・投射）＝国の係数は国ごとの別の写し（定員は国の係数）
        foreach (var nn in sim.Nations) nn.World.Coef.MilitaryEnvelopeShare = 0.01;   // 全ての国で定員が払える予算で決まる（段を上げれば席が増える）
        sim.StepQuarter();
        var step = typeof(MultiNationSim).GetMethod("StepSecurityThreat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var ps = sim.Nations.ConvertAll(n => n.World.SelfPower!);
        int n = ps.Count;
        double[] Expected()
        {
            var seats = new double[n];
            for (int i = 0; i < n; i++) seats[i] = Military.EstablishmentOf(sim.Nations[i].World, sim.Nations[i].World.Coef, out _);
            var border = ArmyShape.Borders(ps, c);
            var hr = new double[n * n]; var col = new double[n];
            for (int a = 0; a < n; a++)
            {
                double sum = ArmyShape.Unrest(ps[a]);
                for (int b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    double h = ArmyShape.Hostility(sim.Diplomacy, ps[b], ps[a], c);
                    if (!(h > 0)) continue;
                    double r = border[a * n + b] + sim.Diplomacy.Projection(ps[a], ps[b], c);
                    if (!(r > 0)) continue;
                    hr[a * n + b] = h * r; sum += h * r;
                }
                col[a] = sum;
            }
            var t = new double[n];
            for (int b = 0; b < n; b++) for (int a = 0; a < n; a++) if (a != b && col[a] > 1e-12 && seats[a] > 0) t[b] += hr[a * n + b] / col[a] * seats[a];
            return t;
        }
        step.Invoke(sim, null);
        var e0 = Expected();
        double sumT = 0, sumSeats = 0;
        for (int i = 0; i < n; i++)
        {
            Assert.Equal(e0[i], sim.Nations[i].World.SecurityThreat, 9);
            sumT += sim.Nations[i].World.SecurityThreat; sumSeats += Military.EstablishmentOf(sim.Nations[i].World, sim.Nations[i].World.Coef, out double man);
            // ★(157) 自国の分母（脅威の割合の分母・戦略鉄道の分母）＝養える軍の**人口の側**（兵役の母数×MaxSoldierFraction）＝定員（経済の側を含む）ではない（反証役の指摘＝定義の番人が無かった）。
            //   ⚠突然変異「自国の分母を定員にする」で赤（この盤面は器の割合 0.01＝定員が経済の側で決まる＝人口の側より小さい）。
            var wi = sim.Nations[i].World;
            Assert.Equal(wi.Coef.MaxSoldierFraction * man, wi.SecurityThreatOwnMass, 9);
        }
        Assert.True(sumT < sumSeats, $"世界の脅威の和 {sumT} が世界の養える軍の和 {sumSeats} 以上（注意配分で割っていない）");
        double fr0 = sim.Nations[0].World.SecurityThreat;
        Assert.True(fr0 > 0, "前提が崩れた：仏に脅威が無い");
        var es = sim.Nations[1].World;
        double esOwn0 = es.SecurityThreatOwnMass;
        es.SetMilitarySpending(Math.Min(1.0, es.MilitarySpending * 2), es.NavalSpendShare);
        step.Invoke(sim, null);
        double fr1 = sim.Nations[0].World.SecurityThreat;
        Assert.Equal(esOwn0, es.SecurityThreatOwnMass, 12);   // ★(157) 自国の段を上げても自国の分母は動かない（自分の段で不安が動かない）
        Assert.True(fr1 > fr0 * (1 + 1e-9), $"西の軍事費の段を上げても仏の不安が増えない（{fr0} → {fr1}）＝相手の選んだ段を読んでいない");
        Ally152(sim.Diplomacy, ps[0], ps[1]);
        step.Invoke(sim, null);
        double fr2 = sim.Nations[0].World.SecurityThreat;
        Assert.True(fr2 < fr1 * (1 - 1e-9), $"仏西を同盟にしても仏の不安が減らない（{fr1} → {fr2}）");
        Assert.Equal(Expected()[0], fr2, 9);
    }

    /// <summary>★(155) AI の軍事費の段＝安全保障の不安の唯一の入口＝平時の段の幅（段1〜段3）を脅威の割合（脅威÷(脅威＋自国の養える軍)）で内分。軍備スタンスは段を書かない。
    /// ★(178) 徴兵法の目標（作者 2026-10-03「全て推奨でよい。(1)は良いと思う。」で採った推奨①②④＝00一覧の軍の裁定の表・AI の整理）＝**陸軍の不安の割合**（SecurityThreatArmy÷(それ＋自国の養える軍)）が
    /// 半分以上か、交戦中かつ総動員中＝上限（皆兵の門〔身分の特権が無い＝農奴解放が全面〕が開いていれば国民皆兵・閉じていれば選抜・既に皆兵なら皆兵）／他は現状維持
    /// （陸軍の不安に入らない海の不安と植民地の不安は数えない・志願制の理由にもしない・旧 (155) の「平時の男子普選以上なら志願制」の枝は外した・交戦だけでは上げない）。
    /// 軍事費の段は不安の全体を読む（陸軍の不安を読まない）。呼び手（UtilityAgent.Apply＝目標を書いて法の門へ通す）でも同じ。
    /// ⚠突然変異「徴兵法を不安の全体で読む（旧）」「男子普選の枝を戻す（旧）」「交戦だけで上げる（旧）」「上限を門に依らず選抜（旧）／皆兵に」「線を 0.3／&gt; 0.5 に」で赤。</summary>
    [Fact]
    public void AIの軍事費の段は安全保障の不安の全体を読み徴兵法の目標は陸軍の不安と皆兵の門を読む()
    {
        var w = France155().World;
        w.SecurityThreat = 0; w.SecurityThreatOwnMass = 100;
        Assert.Equal(0.25, UtilityAgent.SecurityStageTarget(w), 12);
        w.SecurityThreat = 100;
        Assert.Equal(0.5, UtilityAgent.SecurityStageTarget(w), 12);
        w.SecurityThreat = 300;
        Assert.Equal(0.25 + 0.5 * 0.75, UtilityAgent.SecurityStageTarget(w), 12);
        w.SecurityThreat = double.NaN;
        Assert.Equal(0.25, UtilityAgent.SecurityStageTarget(w), 12);
        // Apply は目標を法の門（段への丸め・1段ずつ）へ通す＝今の段1 で不安0 なら段1 のまま（旧の軍備スタンスの 0.7＝段3 なら門で段2 へ上がる）
        w.SetMilitarySpending(0.25, w.NavalSpendShare);
        w.SecurityThreat = 0;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Militarize);
        Assert.Equal(0.25, w.Policy.MilitarySpendingTarget, 12);
        // ★(178) 段は不安の全体を読む＝陸軍の不安を0 にしても段の目標は変わらない（徴兵法の目標だけが陸軍の不安を読む）
        w.SecurityThreat = 300; w.SecurityThreatArmy = 0;
        Assert.Equal(0.25 + 0.5 * 0.75, UtilityAgent.SecurityStageTarget(w), 12);
        var decide = typeof(UtilityAgent).GetMethod("DecideModernLaws", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        ConscriptionLaw Decide(World x, ConscriptionLaw from, double total, double army)
        {
            x.SecurityThreat = total; x.SecurityThreatArmy = army; x.Policy.ConscriptionLaw = from;
            decide.Invoke(null, [x, x.Policy, UtilityAgent.Stance.Industrialize]);
            return x.Policy.ConscriptionLaw;
        }
        Assert.False(w.AtWarNow, "前提が崩れた：仏が交戦中");
        Assert.True(w.Suffrage < Suffrage.AdultMale && !w.Mobilized, "前提が崩れた：仏1783 が男子普選以上か総動員中");
        Assert.True(w.SerfEmancipation == ReformLevel.Limited && w.ConscriptionLaw == ConscriptionLaw.Selective, "前提が崩れた：仏1783 の農奴解放が限定でないか徴兵法が選抜でない");
        Assert.False(Government.UniversalConscriptionOpen(w), "前提が崩れた：限定の農奴解放で皆兵の門が開いている");
        // 門が閉じている（限定）＝上限は選抜：陸軍の不安の割合 0.6・0.5（ちょうど線）＝選抜（(157) の線の両側）
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Volunteer, 150, 150));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Volunteer, 100, 100));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, 150, 150));
        // 陸軍の不安の割合 0.4（線の下・平時）＝現状維持（どちらの法からも動かない）
        double l4 = 100.0 * 0.4 / 0.6;
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(w, ConscriptionLaw.Volunteer, l4, l4));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, l4, l4));
        // 不安の全体は大きい（割合 0.75）が陸軍の不安は0（海軍で防げる海の不安と植民地の不安）＝現状維持（旧＝不安の全体を読み徴兵制／(162)①＝志願制）
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(w, ConscriptionLaw.Volunteer, 300, 0));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, 300, 0));
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(w, ConscriptionLaw.Volunteer, 300, l4));
        // NaN＝現状維持（比較が偽）
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(w, ConscriptionLaw.Volunteer, double.NaN, double.NaN));
        // 門が開いている（全面）＝上限は国民皆兵（線の上だけ）・線の下は現状維持（門が開いても上げない＝英米型）
        w.SerfEmancipation = ReformLevel.Full;
        Assert.True(Government.UniversalConscriptionOpen(w));
        Assert.Equal(ConscriptionLaw.Universal, Decide(w, ConscriptionLaw.Volunteer, 150, 150));
        Assert.Equal(ConscriptionLaw.Universal, Decide(w, ConscriptionLaw.Selective, 100, 100));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, l4, l4));
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(w, ConscriptionLaw.Volunteer, 300, 0));
        // 既に皆兵の国は門が閉じても上限は皆兵（目標を下げる理由にしない）
        w.SerfEmancipation = ReformLevel.None; w.ConscriptionLaw = ConscriptionLaw.Universal;
        Assert.Equal(ConscriptionLaw.Universal, Decide(w, ConscriptionLaw.Universal, 150, 150));
        w.SerfEmancipation = ReformLevel.Limited; w.ConscriptionLaw = ConscriptionLaw.Selective;
        // 男子普選以上でも陸軍の不安が線の下なら現状維持（旧 (155)＝志願制へ）・線の上なら上限へ
        w.Suffrage = Suffrage.AdultMale;
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, l4, l4));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Selective, 0, 0));
        Assert.Equal(ConscriptionLaw.Selective, Decide(w, ConscriptionLaw.Volunteer, 150, 150));
        w.Suffrage = Suffrage.None;
        // 呼び手（Apply＝目標を書き、法の門で今の段＋1 までに丸める）：志願制の国で陸軍の割合 0.6＝選抜・全体 0.75 で陸軍0＝志願のまま
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer; w.SecurityThreat = 150; w.SecurityThreatArmy = 150;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Industrialize);
        Assert.Equal(ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
        w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer; w.SecurityThreat = 300; w.SecurityThreatArmy = 0;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Industrialize);
        Assert.Equal(ConscriptionLaw.Volunteer, w.Policy.ConscriptionLaw);
        // 呼び手で門が閉じている（限定）＝選抜の国の陸軍の割合 0.6＝選抜のまま（皆兵を目標に残さない＝規則で届かない段は現行法へ畳む）
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        w.Policy.ConscriptionLaw = ConscriptionLaw.Selective; w.SecurityThreat = 150; w.SecurityThreatArmy = 150;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Industrialize);
        Assert.Equal(ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
        // ④交戦の枝は総動員の時だけ（旧＝交戦中なら徴兵制）：交戦中で総動員していない＝現状維持・総動員中＝上限（仏＝限定＝選抜）
        var sim = Frozen152("フランス", "スペイン");
        sim.StepQuarter();
        var fr = sim.Nations[0].World;
        sim.Diplomacy.Wars.Add(new War { A = fr.SelfPower!, B = sim.Nations[1].World.SelfPower! });
        fr.SecurityThreatOwnMass = 100;
        Assert.True(fr.AtWarNow, "前提が崩れた：仏が交戦中でない");
        fr.Mobilized = false;
        Assert.Equal(ConscriptionLaw.Volunteer, Decide(fr, ConscriptionLaw.Volunteer, 300, 0));
        fr.Mobilized = true;
        Assert.Equal(ConscriptionLaw.Selective, Decide(fr, ConscriptionLaw.Volunteer, 300, 0));
        fr.SerfEmancipation = ReformLevel.Full;
        Assert.Equal(ConscriptionLaw.Universal, Decide(fr, ConscriptionLaw.Selective, 300, 0));
    }

    /// <summary>★(178) 陸軍の不安（World.SecurityThreatArmy・作者 2026-10-03 で採った推奨②「海と陸を完全に分けない」＝陸の不安＋海の不安のうち自国の海軍で防げない分）
    /// ＝同じ注意配分の分母のうち、A が B に届く強さを「**両側とも本国の州（Core・Integrated）の国境**＋投射×**上陸の門**（Diplomacy.LandingGate＝陸続き1・海越えは A の制海権が門に届けば1）」に限った分。
    /// 盤面＝仏・西・日（開幕・外交を止める）：(a) 式どおり（公開の部品で数え直して一致）(b) 陸軍の不安≦全体
    /// (c) 日本（島国＝陸の国境なし）＝日本の海軍が優勢（相手の制海権が門の下）なら陸軍の不安0（全体は正）・日本が艦を持たないと相手の投射が上陸の門を通る＝陸軍の不安が正
    /// (d) 西の州を全て植民地格にすると仏西の本国の国境は**両方の向きで**0（植民地から来る不安も植民地へ向かう不安も数えない）・全ての国境は変わらない・統合格は本国に数える
    /// (e) エンジンを回し直すと仏と西の陸軍の不安は下がり、数え直しと一致する。
    /// (f) ★(188) 植民地の国境を数えないのは徴兵法の目標（陸軍の不安）だけ＝同じ盤面で不安の全体（AI の軍事費の段が読む＝SecurityShare の全体）と
    ///     理想の軍隊の形の陸の必要量（ArmyShape.Compute の NeedLand＝軍の大きさ）は植民地の国境を数えたまま動かない（作者 2026-10-03 README §1-7 で③を推奨の形へ＝
    ///     「植民地の国境は軍事費と軍の大きさには数える（2026-09-29 (2) のまま）・徴兵法の目標にだけは数えない」・00一覧の軍の裁定の表）。
    /// ⚠突然変異「陸軍の不安に植民地の国境を数える」「片側だけ本国（(168) の形）」「海越えの投射を上陸の門に依らず数える」「海越えの投射を全て除く（(168) の形）」
    ///   「不安の全体も本国の国境だけを読む」「理想の軍隊の形の陸の必要量も本国の国境だけを読む」で赤。</summary>
    [Fact]
    public void 陸軍の不安は本国どうしの国境と上陸の門を通る投射だけを数える()
    {
        var sim = Frozen152("フランス", "スペイン", "日本");
        var c = sim.Coef;
        sim.StepQuarter();
        var step = typeof(MultiNationSim).GetMethod("StepSecurityThreat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var ps = sim.Nations.ConvertAll(n => n.World.SelfPower!);
        int n = ps.Count;
        double[] ExpectArmy()
        {
            var seats = new double[n];
            for (int i = 0; i < n; i++) seats[i] = Military.EstablishmentOf(sim.Nations[i].World, sim.Nations[i].World.Coef, out _);
            var home = new double[n * n];
            var border = ArmyShape.Borders(ps, c, home);
            var ha = new double[n * n]; var col = new double[n];
            for (int a = 0; a < n; a++)
            {
                double sum = ArmyShape.Unrest(ps[a]);
                for (int b = 0; b < n; b++)
                {
                    if (a == b) continue;
                    double h = ArmyShape.Hostility(sim.Diplomacy, ps[b], ps[a], c);
                    if (!(h > 0)) continue;
                    double proj = sim.Diplomacy.Projection(ps[a], ps[b], c);
                    double r = border[a * n + b] + proj;
                    if (!(r > 0)) continue;
                    sum += h * r;
                    ha[a * n + b] = h * (home[a * n + b] + proj * sim.Diplomacy.LandingGate(ps[a], ps[b], c));
                }
                col[a] = sum;
            }
            var army = new double[n];
            for (int b = 0; b < n; b++)
                for (int a = 0; a < n; a++) if (a != b && col[a] > 1e-12 && seats[a] > 0) army[b] += ha[a * n + b] / col[a] * seats[a];
            return army;
        }
        void Check(string label)
        {
            step.Invoke(sim, null);
            var e = ExpectArmy();
            for (int b = 0; b < n; b++)
            {
                var wb = sim.Nations[b].World;
                Assert.Equal(e[b], wb.SecurityThreatArmy, 9);
                Assert.True(wb.SecurityThreatArmy <= wb.SecurityThreat * (1 + 1e-12), $"{label}：{wb.SelfPower!.Name} の陸軍の不安 {wb.SecurityThreatArmy} が全体 {wb.SecurityThreat} を超える");
            }
        }
        var fr = sim.Nations[0].World; var es = sim.Nations[1].World; var jp = sim.Nations[2].World;
        Check("開幕");
        double fr0 = fr.SecurityThreatArmy, es0 = es.SecurityThreatArmy;
        Assert.True(fr0 > 0 && es0 > 0, "前提が崩れた：仏西に陸軍の不安が無い（仏西の国境）");
        // (c) 日本＝陸の国境なし。日本の海軍が圧倒（仏西の艦なし）＝上陸の門が閉じる＝陸軍の不安0・全体は正
        //   艦隊は盤面へ直に置く（外交を止めた盤面は艦隊を播かない）＝海軍力＝min(艦隊, 総合ゲージ×海軍シェア)。
        var keep = ps.ConvertAll(q => (q.Military, q.FleetGauge, q.NavalShare));
        void Navy(int i, double share) { ps[i].Military = 100; ps[i].FleetGauge = 100; ps[i].NavalShare = share; }
        //   日本の海軍が優勢（仏の制海権が上陸の門に届かない＝仏の投射は制海権の分だけ届くので不安の全体には入る）・西は艦なし
        Navy(0, 0.4); Navy(1, 0); Navy(2, 0.6);
        Check("日本の海軍が優勢");
        double scFJ = sim.Diplomacy.SeaControl(ps[0], ps[2]);
        Assert.True(scFJ > 0 && scFJ < c.SeaInvasionControlMin && sim.Diplomacy.LandingGate(ps[0], ps[2], c) == 0, $"前提が崩れた：仏→日の制海権 {scFJ} が門の下でない");
        Assert.True(jp.SecurityThreat > 0, "前提が崩れた：日本に不安が無い（海越えの投射が届かない）");
        Assert.Equal(0.0, jp.SecurityThreatArmy);
        // 日本が艦を持たない＝仏西の制海権1＝上陸の門が開く＝仏西の海越えの投射が陸軍の不安に入る
        Navy(0, 0.5); Navy(1, 0.5); Navy(2, 0);
        Check("日本に艦が無い");
        Assert.True(sim.Diplomacy.LandingGate(ps[0], ps[2], c) > 0, "前提が崩れた：仏→日の上陸の門が閉じている");
        Assert.True(jp.SecurityThreatArmy > 0, $"日本に艦が無いのに陸軍の不安が0（{jp.SecurityThreatArmy}）＝海で防げない投射を数えていない");
        for (int i = 0; i < n; i++) (ps[i].Military, ps[i].FleetGauge, ps[i].NavalShare) = keep[i];
        Check("戻した");
        double frTot0 = fr.SecurityThreat, esTot0 = es.SecurityThreat;   // (f) の前の値（不安の全体）
        ArmyShape.Compute(sim.Diplomacy, c);
        double frLand0 = ps[0].NeedLand, esLand0 = ps[1].NeedLand;        // (f) の前の値（理想の軍隊の形の陸の必要量）
        Assert.True(frLand0 > 0 && esLand0 > 0, "前提が崩れた：仏西に陸の必要量が無い");
        // (d) 西の州を全て植民地格に＝仏西の本国の国境は両方の向きで0・全ての国境は変わらない
        var home1 = new double[n * n];
        var border1 = ArmyShape.Borders(ps, c, home1);
        double before = border1[0 * n + 1];
        Assert.True(before > 0 && Math.Abs(home1[0 * n + 1] - before) <= 1e-12 * before && Math.Abs(home1[1 * n + 0] - before) <= 1e-12 * before, "前提が崩れた：仏西の国境が本国どうしでない");
        var tiers = es.Regions.ConvertAll(r => r.Tier);
        foreach (var r in es.Regions) r.Tier = RegionTier.Integrated;   // 統合格（割譲で得た州）は本国に数える
        var homeI = new double[n * n];
        ArmyShape.Borders(ps, c, homeI);
        Assert.Equal(before, homeI[0 * n + 1], 12);
        foreach (var r in es.Regions) r.Tier = RegionTier.Colonial;
        var home2 = new double[n * n];
        var border2 = ArmyShape.Borders(ps, c, home2);
        Assert.Equal(before, border2[0 * n + 1], 12);
        Assert.Equal(0.0, home2[0 * n + 1]);
        Assert.Equal(0.0, home2[1 * n + 0]);
        // (e) エンジン（呼び手）を回し直す＝仏も西も国境の分が抜けて下がり、数え直しと一致
        Check("西が植民地格");
        Assert.True(es.SecurityThreatArmy < es0 * (1 - 1e-6), $"西の本国が植民地格になっても西の陸軍の不安が下がらない（{es0} → {es.SecurityThreatArmy}）＝植民地の国境を数えた");
        Assert.True(fr.SecurityThreatArmy < fr0 * (1 - 1e-6), $"西が植民地格になっても仏の陸軍の不安が下がらない（{fr0} → {fr.SecurityThreatArmy}）＝植民地から来る不安を数えた（(168) の片側の形）");
        // (f) ★(188) 軍事費の段と軍の大きさは植民地の国境を数えたまま＝同じ盤面で不安の全体と陸の必要量は格を変えても動かない
        Assert.Equal(frTot0, fr.SecurityThreat, 9);
        Assert.Equal(esTot0, es.SecurityThreat, 9);
        Assert.True(UtilityAgent.SecurityShare(fr) > UtilityAgent.SecurityShare(fr, army: true), "仏の不安の全体の割合が陸軍の不安の割合を超えない＝軍事費の段が植民地の国境を数えていない");
        ArmyShape.Compute(sim.Diplomacy, c);
        Assert.Equal(frLand0, ps[0].NeedLand, 9);
        Assert.Equal(esLand0, ps[1].NeedLand, 9);
        for (int i = 0; i < es.Regions.Count; i++) es.Regions[i].Tier = tiers[i];
    }

    /// <summary>★(178) 国民皆兵へ登る門（作者 2026-10-03「全て推奨でよい。(1)は良いと思う。」で採った推奨①「封建の兵から国民の兵へ」＝AI の整理）＝身分の特権（領主権の残高
    /// Privileges.SeigneurialDegree＝農奴解放 なし1／限定0.5／全面0）がある間は皆兵へ登れない（Government.LawPrereq の1本）。下げはいつでも・既に皆兵の国は留まれる。
    /// 同じ門を読む全ての経路で確かめる＝プレイヤー（法律タブ＝GateInfo）・勢力の要求の目標（Parliament.TargetDemand）・勝った運動の強制施行（Politics.ApplyDemand）・
    /// AI の目標の整形（UtilityAgent.GateAllLawTargets＝規則で届かない段は現行法へ畳む）・施行（Government.Step の段の法のループ）。純支持の門は外す（主題は前提の門）。
    /// ⚠突然変異「門を外す」「門を限定で開く（SeigneurialDegree&lt;1）」「下げる向きにも掛ける／現在段の免除を外す」で赤。</summary>
    [Fact]
    public void 国民皆兵へ登る門は身分の特権が無い時だけ開きプレイヤーとAIと勢力の要求が同じ門を読む()
    {
        var w = France155().World; var c = w.Coef;
        w.PoliticalLockEnabled = false;
        foreach (var (serf, open) in new[] { (ReformLevel.None, false), (ReformLevel.Limited, false), (ReformLevel.Full, true) })
        {
            w.SerfEmancipation = serf;
            w.ConscriptionLaw = ConscriptionLaw.Selective; w.Policy.ConscriptionLaw = ConscriptionLaw.Selective;
            Assert.Equal(open, Government.UniversalConscriptionOpen(w));
            var (ok, why) = Government.LawPrereq(w, PolicyAxis.ConscriptionLaw, (int)ConscriptionLaw.Universal, c);
            Assert.Equal(open, ok);
            if (!open) Assert.Equal("農奴解放（全面）が前提", why);
            Assert.True(Government.LawPrereq(w, PolicyAxis.ConscriptionLaw, (int)ConscriptionLaw.Volunteer, c).Ok, $"{serf}：志願へ下げる段が塞がれた");
            // プレイヤー（法律タブ）
            Assert.Equal(open, Government.GateInfo(w, PolicyAxis.ConscriptionLaw, true, (int)ConscriptionLaw.Universal).Ok);
            Assert.True(Government.GateInfo(w, PolicyAxis.ConscriptionLaw, false, (int)ConscriptionLaw.Volunteer).Ok, $"{serf}：志願へ下げるボタンが塞がれた");
            // 勢力の要求の目標
            Parliament.TargetDemand(w, PolicyAxis.ConscriptionLaw, inc: true);
            Assert.Equal(open ? ConscriptionLaw.Universal : ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
            // 勝った運動の強制施行
            w.Policy.ConscriptionLaw = ConscriptionLaw.Selective;
            Assert.Equal(open ? 1 : 0, Politics.ApplyDemand(w, c, PolicyAxis.ConscriptionLaw, inc: true, 1));
            Assert.Equal(open ? ConscriptionLaw.Universal : ConscriptionLaw.Selective, w.ConscriptionLaw);
            // AI の目標の整形
            w.ConscriptionLaw = ConscriptionLaw.Selective; w.Policy.ConscriptionLaw = ConscriptionLaw.Universal;
            UtilityAgent.GateAllLawTargets(w, w.Policy);
            Assert.Equal(open ? ConscriptionLaw.Universal : ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
            // 施行（目標を皆兵に置いても門が閉じていれば上がらない）
            w.ConscriptionLaw = ConscriptionLaw.Selective; w.Policy.ConscriptionLaw = ConscriptionLaw.Universal;
            Government.Step(w, c);
            Assert.Equal(open ? ConscriptionLaw.Universal : ConscriptionLaw.Selective, w.ConscriptionLaw);
        }
        // 既に皆兵の国は門が閉じても留まれる（天井型の前提は登る側だけ）・皆兵から選抜へ下げられる
        w.SerfEmancipation = ReformLevel.None; w.ConscriptionLaw = ConscriptionLaw.Universal;
        Assert.True(Government.LawPrereq(w, PolicyAxis.ConscriptionLaw, (int)ConscriptionLaw.Universal, c).Ok);
        Assert.True(Government.GateInfo(w, PolicyAxis.ConscriptionLaw, false, (int)ConscriptionLaw.Selective).Ok);
        w.Policy.ConscriptionLaw = ConscriptionLaw.Universal;
        Government.Step(w, c);
        Assert.Equal(ConscriptionLaw.Universal, w.ConscriptionLaw);
    }

    /// <summary>★(178) 戦時の非常の法（作者 2026-10-03「全て推奨でよい」で採った推奨「戦時に上げた徴兵法は戦後に戻る」・形は (168) のパッチ）＝戦時に上げた徴兵法は、戦争が終わった四半期に戦前の段へ戻る（R2 の戦時段の失効と同じ形・Government.Step＝AI とプレイヤーの共通経路）。
    /// 盤面＝英（開幕の志願制）・仏（開幕の選抜）を外交を止めて並べ、戦争を盤面へ直に置く（NullAgent＝AI は目標を書かない）。
    /// (a) 交戦に入った四半期に戦前の段を記録 (b) 戦時に上げた段は戦争の間は残る (c) 戦争が終わった四半期に戦前の段へ戻り、保存目標も同じ段・記録は消える
    /// (d) 平時から有った徴兵法は戻らない（仏＝選抜のまま）・皆兵へ上げた分だけ選抜へ戻る (e) 戦時に下げた段は上げ戻さない (f) 建国は親の記録を継ぐ（CopyLawsFrom）
    /// (g) 呼び手＝四半期の頭（MultiNationSim.StepQuarter → Simulation の QuarterStart → Government.Step）で同じことが起き、戻した段から翌期に上がらない。
    /// ⚠突然変異「戻さない」「上げたか確かめずに戻す」「交戦中に毎期記録し直す」「保存目標を戻さない」「建国が記録を継がない」で赤。</summary>
    [Fact]
    public void 戦時に上げた徴兵法は戦争が終わった四半期に戦前の段へ戻る()
    {
        var sim = Frozen152("イギリス", "フランス");
        sim.StepQuarter();
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Selective, fr.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
        Assert.Equal(-1, fr.ConscriptionPreWar);
        var war = new War { A = uk.SelfPower!, B = fr.SelfPower! };
        sim.Diplomacy.Wars.Add(war);
        // (a) 交戦に入った四半期に記録
        Government.Step(uk, uk.Coef); Government.Step(fr, fr.Coef);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        Assert.Equal((int)ConscriptionLaw.Selective, fr.ConscriptionPreWar);
        // (b) 戦時に上げる（どの経路でも同じ＝ここでは法と目標を直に置く）＝戦争の間は残り、記録は戦前のまま
        uk.ConscriptionLaw = ConscriptionLaw.Selective; uk.Policy.ConscriptionLaw = ConscriptionLaw.Selective;
        fr.ConscriptionLaw = ConscriptionLaw.Universal; fr.Policy.ConscriptionLaw = ConscriptionLaw.Universal;
        Government.Step(uk, uk.Coef); Government.Step(fr, fr.Coef);
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Universal, fr.ConscriptionLaw);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        Assert.Equal((int)ConscriptionLaw.Selective, fr.ConscriptionPreWar);
        // (f) 建国は親の記録を継ぐ（親の戦時の法と記録が一緒に移る）
        var child = new Simulation(WorldData.ByName("デンマーク").ToScenario(5), new NullAgent()).World;
        child.CopyLawsFrom(uk);
        Assert.Equal(ConscriptionLaw.Selective, child.ConscriptionLaw);
        Assert.Equal((int)ConscriptionLaw.Volunteer, child.ConscriptionPreWar);
        // (c)(d) 戦争が終わった四半期＝英は志願へ・仏は皆兵から選抜へ（平時から有った選抜は残る）・保存目標も同じ段・記録は消える
        sim.Diplomacy.Wars.Remove(war);
        Government.Step(uk, uk.Coef); Government.Step(fr, fr.Coef);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.Policy.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Selective, fr.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Selective, fr.Policy.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
        Assert.Equal(-1, fr.ConscriptionPreWar);
        Government.Step(child, child.Coef);   // 建国した国は交戦していない（外交なし）＝親の戦前の段へ
        Assert.Equal(ConscriptionLaw.Volunteer, child.ConscriptionLaw);
        // (e) 戦時に下げた段は上げ戻さない（仏＝選抜から志願へ下げた）
        sim.Diplomacy.Wars.Add(war);
        Government.Step(fr, fr.Coef);
        Assert.Equal((int)ConscriptionLaw.Selective, fr.ConscriptionPreWar);
        fr.ConscriptionLaw = ConscriptionLaw.Volunteer; fr.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer;
        sim.Diplomacy.Wars.Remove(war);
        Government.Step(fr, fr.Coef);
        Assert.Equal(ConscriptionLaw.Volunteer, fr.ConscriptionLaw);
        Assert.Equal(-1, fr.ConscriptionPreWar);
        // (g) 呼び手＝四半期の頭：交戦中に英を選抜へ上げ、交戦のまま1四半期回す（残る）→ 戦争を除いて1四半期回す（志願へ戻る）→ もう1四半期（上がらない）
        sim.Diplomacy.Wars.Add(war);
        sim.StepQuarter();
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        uk.ConscriptionLaw = ConscriptionLaw.Selective; uk.Policy.ConscriptionLaw = ConscriptionLaw.Selective;
        sim.StepQuarter();
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        sim.Diplomacy.Wars.Remove(war);
        sim.StepQuarter();
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.Policy.ConscriptionLaw);
        sim.StepQuarter();
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
    }

    /// <summary>★(190) 法の錠を対称に（00一覧 §0-A の 0d・作者 2026-10-03「他は推奨でよい」・推奨で進めた＝作者の承認は未確認）＝**非常時に上げた徴兵法は非常時が去れば戻る**
    /// （(178) の「戦時に上げた徴兵法は戦後に戻る」を非常時へ広げた＝非常時＝交戦中 または 陸軍の不安の割合が AI の上げる線〔UtilityAgent.ConscriptionThreatLine〕以上）。
    /// 上げる線と戻す線は同じ1本（AI の目標がちょうど線の上で上限へ上げる＝上の AI の番人・戻すのはその線の下）。盤面＝英（開幕の志願制）・外交を止めて不安を盤面へ直に置く。
    /// (a) 平時に不安が線の上になった四半期に今の段を記録 (b) 線の上の間は上げた段が残り記録はそのまま (c) ちょうど線の上（割合＝線）は非常時＝戻さない
    /// (d) 線の下へ戻り交戦もしていない四半期に記録の段へ戻り・保存目標も同じ段・記録は消える (e) 非常時でない間に上げた段（記録なし）は戻さない
    /// (f) 戦争が終わっても不安が線の上なら戻さず（普1814・仏1872 型）、線の下へ戻った四半期に戻す（英1920・米1865 型）。
    /// ⚠突然変異「非常時を交戦だけで読む（(178) の旧）」「戻す線を上げる線と別にする（線ちょうどを非常時に数えない）」で赤。</summary>
    [Fact]
    public void 平時に不安で上げた徴兵法は不安が線の下へ戻れば非常時の前の段へ戻る()
    {
        var sim = Frozen152("イギリス", "フランス");
        sim.StepQuarter();
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        double line = UtilityAgent.ConscriptionThreatLine;
        void Share(double s) { uk.SecurityThreatOwnMass = 100; uk.SecurityThreatArmy = 100 * s / (1 - s); }
        void Set(ConscriptionLaw l) { uk.ConscriptionLaw = l; uk.Policy.ConscriptionLaw = l; }
        Assert.False(uk.AtWarNow, "前提が崩れた：英が交戦中");
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
        Share(line);
        Assert.Equal(line, UtilityAgent.SecurityShare(uk, army: true), 12);   // 盤面の作り方の確かめ（ちょうど線）
        // (a) 平時に不安が線の上＝非常時に入った四半期に記録
        Share(0.6);
        Government.Step(uk, uk.Coef);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        // (b) 線の上の間に上げた段は残る（どの経路でも同じ＝ここでは法と目標を直に置く）
        Set(ConscriptionLaw.Selective);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        // (c) ちょうど線の上＝非常時（AI が上げる線と同じ）＝戻さない
        Share(line);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        // (d) 線の下へ戻り交戦もしていない＝非常時の前の段へ・保存目標も・記録は消える
        Share(0.4);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.Policy.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
        // (e) 非常時でない間に上げた段（記録なし）は戻さない＝平時の法（法律タブで置いた法・平時から有った法）
        Set(ConscriptionLaw.Selective);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
        // (f) 戦争が終わっても不安が線の上なら戻さず、線の下へ戻った四半期に戻す
        Set(ConscriptionLaw.Volunteer);
        var war = new War { A = uk.SelfPower!, B = fr.SelfPower! };
        sim.Diplomacy.Wars.Add(war);
        Government.Step(uk, uk.Coef);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        Set(ConscriptionLaw.Selective);
        sim.Diplomacy.Wars.Remove(war);
        Share(0.6);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Selective, uk.ConscriptionLaw);
        Assert.Equal((int)ConscriptionLaw.Volunteer, uk.ConscriptionPreWar);
        Share(0.4);
        Government.Step(uk, uk.Coef);
        Assert.Equal(ConscriptionLaw.Volunteer, uk.ConscriptionLaw);
        Assert.Equal(-1, uk.ConscriptionPreWar);
    }

    /// <summary>★(178) 開幕の徴兵法（作者 2026-10-03「全て推奨でよい」で採った推奨「開幕の徴兵法を史実のデータで」）＝1783 年の史実のデータ（world.json の initialConscription・国名の分岐でなくデータ・制度と年と出典は実装ログ (178)）。英・米・日・清・蘭・オスマン＝志願制／普・露・墺・西・仏＝選抜（既定）。
    /// 現在値（World）と目標（Policy）が同じ値で始まる＝開幕の四半期に法の戦線が開かない。建国は親の法を継ぐ（MultiNationSim の Scenario と CopyLawsFrom）。
    /// ⚠突然変異「NationConfig が Scenario へ渡さない」「現在値を目標から置かない（既定の選抜のまま）」で赤。</summary>
    [Fact]
    public void 開幕の徴兵法は1783年の史実のデータで始まる()
    {
        foreach (var (name, law) in new[] { ("イギリス", ConscriptionLaw.Volunteer), ("アメリカ", ConscriptionLaw.Volunteer), ("日本", ConscriptionLaw.Volunteer),
                                            ("清", ConscriptionLaw.Volunteer), ("ネーデルラント", ConscriptionLaw.Volunteer), ("オスマン", ConscriptionLaw.Volunteer),
                                            ("プロイセン", ConscriptionLaw.Selective), ("ロシア", ConscriptionLaw.Selective), ("オーストリア", ConscriptionLaw.Selective),
                                            ("スペイン", ConscriptionLaw.Selective), ("フランス", ConscriptionLaw.Selective) })
        {
            Assert.Equal(law, WorldData.ByName(name).InitialConscription);
            var w = new Simulation(WorldData.ByName(name).ToScenario(3), new NullAgent()).World;
            Assert.True(w.ConscriptionLaw == law && w.Policy.ConscriptionLaw == law, $"{name} の開幕の徴兵法 {w.ConscriptionLaw}／目標 {w.Policy.ConscriptionLaw}（データ {law}）");
        }
    }

    /// <summary>★(157) 呼び手の番人（反証役の指摘）：Fiscal の余剰（guns-vs-butter）は月初の予算（Simulation の軍事費の吸い込み）が書いた装備の請求 World.LastMilitaryClaim を読む
    /// ＝同じ盤面の双子で月初の装備の請求だけを Δ 変えると、裁量余剰の移動平均（FiscalCapacity）が 平滑化×Δ だけ下がる（余剰が正の範囲）。⚠突然変異「Fiscal が装備の請求を読まない」で赤。</summary>
    [Fact]
    public void 財政の余剰は月初の予算が書いた装備の請求を引く()
    {
        (double fc, double room, double smooth) Twin(double claim)
        {
            var w = France155().World; var c = w.Coef;
            w.GovDebt = 0; w.ForeignDebt = 0;   // 利払いを消す（仏1789 は利払いと行政で余剰0＝余剰が正の範囲の盤面にする・QuietBritain と同じ作法）
            foreach (var r in w.Regions) foreach (var p in r.Pops) p.BondHoldings = 0;
            w.FiscalCapacity = 1.0;              // 平滑化の枝（前の値が正）に置く
            w.LastMilitaryClaim = claim;
            Fiscal.Step(w, c);
            return (w.FiscalCapacity, w.LastRevenue - w.LastInterest - w.LastAdminClaim, c.FiscalCapacitySmoothing);   // room＝装備の請求を引く前の余剰
        }
        var a = Twin(0);
        double d = 0.01 * a.room;
        Assert.True(d > 0, $"前提が崩れた：装備の請求を引く前の余剰が0以下（{a.room}）");
        var b = Twin(d);
        double s = a.smooth;
        Assert.True(Math.Abs(a.room - b.room) <= 1e-9 * a.room, "前提が崩れた：双子の歳入・利払い・行政が違う");
        Assert.True(Math.Abs((a.fc - b.fc) - s * d) <= 1e-9 * s * d, $"月初の装備の請求を Δ 増やしても余剰の移動平均が 平滑化×Δ だけ下がらない（差 {a.fc - b.fc:G17}・期待 {s * d:G17}）");
    }

    /// <summary>★(155) 失業プールの職の比較の所得＝1人あたり。兵士の席は兵1人の給与を送り手の世帯の1人あたりへ直す（×送り手の働き盛り÷頭数）＝分子と分母を同じ規則で
    /// （兵士 POP は働き盛りの男だけ＝頭数＝働き手）。⚠突然変異「直さない」で赤。</summary>
    [Fact]
    public void 兵士の席の所得は送り手の世帯の1人あたりへ直して比べる()
    {
        var w = France155().World; var r = w.Region;
        var s = r.Pops[r.SoldierPopIndex];
        Pop? src = null; foreach (var p in r.Pops) if (!p.IsSoldier && p.ClassId == w.Coef.SoldierRecruitClassA) { src = p; break; }
        Assert.NotNull(src);
        var f = typeof(Meritocracy).GetMethod("JobIncome", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        double got = (double)f.Invoke(null, [s, src])!;
        double expect = Math.Max(0, s.Earned) / s.Size * (Military.WorkingAdults(src!) / src!.Size);
        Assert.True(expect > 0, "前提が崩れた：兵士の所得が0");
        Assert.Equal(expect, got, 12);
        // ★(161)：送り手が兵士のとき民間の職は働き手1人あたり（世帯の所得÷働き盛り）を送り手の世帯の1人あたりへ直す（兵士 POP の働き盛り÷頭数）＝同じ規則（旧＝頭数あたりのまま＝片向き）
        Assert.Equal(Math.Max(0, src.Earned) / Military.WorkingAdults(src) * (Military.WorkingAdults(s) / s.Size), (double)f.Invoke(null, [src, s])!, 12);
        Pop? other = null; foreach (var p in r.Pops) if (!p.IsSoldier && p.ClassId == w.Coef.SoldierRecruitClassB) { other = p; break; }
        Assert.NotNull(other);
        Assert.Equal(Math.Max(0, other!.Earned) / other.Size, (double)f.Invoke(null, [other, src])!, 12);   // 民間どうしは頭数あたりのまま
    }

    private static readonly System.Reflection.MethodInfo RouteViaPoolM =
        typeof(Meritocracy).GetMethod("RouteViaPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    /// <summary>★(161)（(157) の反証役の指摘 va#5・行き先の分布の番人）：兵士が送り手（平時の復員・未払いの脱走＝強制の経路 RouteViaPool）のとき、行き先の民間の職は
    /// **働き手1人あたりの所得**で比べる（JobIncome の1本）。盤面＝仏の首都で農民と都市労働者の働き手1人あたりの所得を 1.2 と 1.0 に置き、農民の世帯に子供を足して
    /// **頭数あたりの順位を逆に**する（頭数あたりなら都市労働者が上）。他の下流の職は所得0（候補にならない）・空き（1−失業）は1・都市化の天井は外す。
    /// (a) 按分＝兵の給与0（どの職も兵の給与×転職のプレミアムを超える）＝低温の softmax で2つへ分かれ、取り分は働き手1人あたりの所得の softmax に一致。
    /// (b) 総取り＝兵の給与が両方の職を大きく上回る＝第1段が通らず強制の残りが**働き手1人あたりで最良の1つ**（農民）へ全量。人口は保存。
    /// ⚠突然変異「送り手が兵士でも民間の職は頭数あたり（旧＝片向き）」で (a)(b) とも赤・「強制の残りの最良を頭数あたりで選ぶ」で (b) が赤。</summary>
    [Fact]
    public void 兵士が送り手のとき民間の職は働き手1人あたりで比べ行き先は按分か最良の1つへの総取り()
    {
        (double toFarm, double toWork, double flow, double popDiff, double farmPW, double workPW) Arm(double soldierEarnedPerHead)
        {
            var w = France155().World; var c = w.Coef; var r = w.Region;
            c.BaseUrbanCap = 0.85; c.UrbanPerInd = 0;   // 都市化の天井を外す（兵士 POP は農村＝都市の職へは天井の残枠まで）
            c.SerfGateCoversPool = 0;                    // 農奴の移動の門を外す（送った人数＝流れ）
            var s = r.Pops[r.SoldierPopIndex];
            Pop? farm = null, work = null;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) continue;
                if (p.ClassId == c.SoldierRecruitClassA) farm ??= p;
                else if (p.ClassId == c.SoldierRecruitClassB) work ??= p;
            }
            Assert.True(farm is not null && work is not null && s.Size > 1, "前提が崩れた：農民・都市労働者・兵士");
            // 農民の世帯に子供を足す（頭数を2倍・増えた分は0〜9歳）＝働き盛り÷頭数を半分へ
            var a = (double[])farm!.Ages.Clone();
            for (int k = 0; k < a.Length; k++) a[k] *= 0.5;
            a[0] += 0.25; a[1] += 0.25;
            farm.Ages = a; farm.Size *= 2;
            for (int i = 0; i < r.Pops.Count; i++)
            {
                var p = r.Pops[i];
                p.UnemploymentRate = 0;   // 空き（1−失業）を1に
                if (!p.IsSoldier && Meritocracy.ClassOf(r, i) == 0 && !ReferenceEquals(p, farm) && !ReferenceEquals(p, work)) p.Earned = 0;   // 他の下流の職は候補にならない
            }
            farm.Earned = 1.2 * Military.WorkingAdults(farm);   // 働き手1人あたり 1.2
            work!.Earned = 1.0 * Military.WorkingAdults(work);  // 働き手1人あたり 1.0
            s.Earned = soldierEarnedPerHead * s.Size;
            Assert.True(work.Earned / work.Size > farm.Earned / farm.Size, "前提が崩れた：頭数あたりでは都市労働者が上（順位が逆になる盤面）");
            double flow = 0.1 * s.Size, f0 = farm.Size, w0 = work.Size, pop0 = r.SumPopulation(), vac = 0;
            object[] args = [r, w, 0.5, 1.0, s, 0, flow, 1.0, 1.0, false, c, vac, true];
            RouteViaPoolM.Invoke(null, args);
            return (farm.Size - f0, work.Size - w0, flow, r.SumPopulation() - pop0,
                    1.2 * Military.WorkingAdults(s) / s.Size, 1.0 * Military.WorkingAdults(s) / s.Size);
        }
        var split = Arm(0.0);    // (a) 按分
        var all = Arm(100.0);    // (b) 総取り
        double T = new Coefficients().JobPoolTemperature;
        Assert.True(T > 0, "前提が崩れた：着地の温度が0（総取りのウォーターフォール）");
        double share = 1.0 / (1.0 + Math.Exp((split.workPW - split.farmPW) / T));   // 農民の取り分（働き手1人あたりの softmax）
        Assert.True(split.toFarm > 0 && split.toWork > 0, $"(a) 按分にならない（農民 {split.toFarm}・都市労働者 {split.toWork}）");
        Assert.True(Math.Abs(split.toFarm - share * split.flow) <= 1e-9 * split.flow, $"(a) 農民の取り分 {split.toFarm / split.flow:F6} ≠ 働き手1人あたりの softmax {share:F6}");
        Assert.True(Math.Abs(split.toFarm + split.toWork - split.flow) <= 1e-9 * split.flow, "(a) 送った人数が行き先の和に一致しない");
        Assert.True(Math.Abs(all.toFarm - all.flow) <= 1e-9 * all.flow && Math.Abs(all.toWork) <= 1e-9 * all.flow,
            $"(b) 強制の残りが働き手1人あたりで最良の職（農民）へ総取りにならない（農民 {all.toFarm}・都市労働者 {all.toWork}・送った {all.flow}）");
        Assert.True(Math.Abs(split.popDiff) <= 1e-9 * split.flow && Math.Abs(all.popDiff) <= 1e-9 * all.flow, "州の人口が保存されない");
    }

    /// <summary>★(161)（(157) の反証役の指摘 va#8）：移民の引力（<see cref="Territory.Opportunity"/>＝州の雇用の項 1−失業者÷人口）は**兵士の未払いを失業に数えない**
    /// （兵士の「失業」＝給与の未払いの割合＝脱走と志願の空きだけが読む）。民間の失業は今までどおり効く。
    /// ⚠突然変異「兵士の失業も足す（旧）」で赤。</summary>
    [Fact]
    public void 移民の引力は兵士の未払いを失業に数えない()
    {
        var w = France155().World; var c = w.Coef; var r = w.Region;
        var s = r.Pops[r.SoldierPopIndex];
        Assert.True(s.Size > 1, "前提が崩れた：兵士が居ない");
        s.UnemploymentRate = 0;
        double o0 = Territory.Opportunity(r, c);
        s.UnemploymentRate = 1;
        double o1 = Territory.Opportunity(r, c);
        Assert.True(o0 > 0, $"前提が崩れた：機会 {o0}");
        Assert.Equal(o0, o1);
        Pop? farm = null; foreach (var p in r.Pops) if (!p.IsSoldier && p.ClassId == c.SoldierRecruitClassA) { farm = p; break; }
        farm!.UnemploymentRate = Math.Min(1, farm.UnemploymentRate + 0.5);
        Assert.True(Territory.Opportunity(r, c) < o0, "民間の失業が移民の引力を下げない");
    }

    /// <summary>★(161) 徴兵は兵1人を安くする（AI が作者に返した設計「徴兵は兵1人を安くする制度でもあった（仏・普が英より大きな軍を持てた理由）」・作者の承認は未確認）：
    /// 徴兵制（選抜・皆兵）の兵卒の給与＝徴兵の兵の手当＝徴募の母数の**頭数あたり**の所得（世帯の1人ぶんの暮らし＝糧食・被服と小遣い）・志願制＝**働き盛り1人あたり**（規定の給与）。
    /// 幹部の給与（質）の必要額は**どちらの法でも規定の給与**（将校と下士官は長期の職業軍人）。盤面＝仏（器を大きく＝給与の充足1・人口の側を0＝徴兵で兵が動かない）で1か月＝
    /// 兵士の給与の請求（LastSoldierPay）が Σ兵数×(兵卒の単価＋幹部の割合×規定の単価)×SoldierPayMult に一致（兵数と単価＝月の決算の直前に読む＝予算が読む値）。
    /// ⚠突然変異「徴兵制でも規定の給与」「幹部の給与を兵卒の手当で数える」で赤。</summary>
    [Fact]
    public void 徴兵制の兵卒の給与は徴募の母数の頭数あたりで幹部の給与は規定の給与で数える()
    {
        (double claim, double expect, double keep, double reg, double train) Arm(ConscriptionLaw law, double cadre)
        {
            var sim = France155(); var w = sim.World; var c = w.Coef;
            w.ConscriptionLaw = law; w.Policy.ConscriptionLaw = law;   // 施行の目標も同じ法（月の施行が戻さない）
            c.MilitaryEnvelopeShare = 1e9; c.MilitaryCadrePayRatio = cadre;
            c.MaxSoldierFraction = 1e-12;   // 席をほぼ0＝徴兵で兵が増えない（減らす枝は Military に無い・Meritocracy は四半期＝この1か月に走らない）
            for (int d = 0; d < Calendar.DaysPerMonth - 1; d++) sim.StepDay();   // 月の決算（30日目の締め）の直前まで
            double e = 0, k = 0, g = 0;
            bool conscript = law >= ConscriptionLaw.Selective;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active || r.SoldierPopIndex < 0) continue;
                double n = r.Pops[r.SoldierPopIndex].Size;
                double kw = Military.RecruitWage(r, c, last: false, keep: true), rw = Military.RecruitWage(r, c, last: false);   // 予算が読む値＝月の決算の前の Earned
                k += n * kw; g += n * rw;
                e += n * ((conscript ? kw : rw) + cadre * rw) * c.SoldierPayMult;
            }
            sim.StepDay();   // 30日目＝月の決算（予算が立つ）
            return (w.LastSoldierPay, e, k, g, w.MilitaryTrainingFill);
        }
        var sel = Arm(ConscriptionLaw.Selective, 0.0);
        var vol = Arm(ConscriptionLaw.Volunteer, 0.0);
        var selQ = Arm(ConscriptionLaw.Selective, 1.0);
        var uni = Arm(ConscriptionLaw.Universal, 0.0);
        Assert.True(sel.keep > 0 && sel.keep < sel.reg, $"前提が崩れた：手当 {sel.keep} が規定の給与 {sel.reg} より安くない");
        Assert.True(Math.Abs(sel.claim - sel.expect) <= 1e-9 * sel.expect, $"選抜：兵卒の給与の請求 {sel.claim} ≠ 手当×兵数 {sel.expect}");
        Assert.True(Math.Abs(uni.claim - uni.expect) <= 1e-9 * uni.expect, $"皆兵：兵卒の給与の請求 {uni.claim} ≠ 手当×兵数 {uni.expect}");
        Assert.True(Math.Abs(vol.claim - vol.expect) <= 1e-9 * vol.expect, $"志願：兵卒の給与の請求 {vol.claim} ≠ 規定の給与×兵数 {vol.expect}");
        Assert.Equal(1.0, selQ.train);
        Assert.True(Math.Abs(selQ.claim - selQ.expect) <= 1e-9 * selQ.expect, $"選抜＋幹部：請求 {selQ.claim} ≠ 手当＋規定の給与 {selQ.expect}（幹部の給与を手当で数えた）");
    }

    /// <summary>★(161) 定員の経済の側（払える予算÷兵1人あたりの費用）も同じ物差し＝徴兵制は兵1人の給与が手当（頭数あたり）の分だけ安い＝同じ器でより多くの席
    /// （仏・普が英より大きな軍を安く持てた理由）。盤面＝仏（人口の側を外す）で法だけ替える＝志願の定員から装備の単価を逆算し、徴兵の定員を予測して一致。
    /// ⚠突然変異「定員は徴兵制でも働き盛り1人あたり」で赤。</summary>
    [Fact]
    public void 定員の経済の側は徴兵制で兵1人の手当の分だけ安い()
    {
        var w = France155().World; var c = w.Coef;
        c.MaxSoldierFraction = 1e9;   // 人口の側を外す
        double earn = 0, heads = 0, wa = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || !(Military.RegionManpower(r, c) > 0)) continue;
            foreach (var p in r.Pops)
                if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB))
                { earn += Math.Max(0, p.LastEarned); heads += p.Size; wa += Military.WorkingAdults(p); }
        }
        double env = Military.EnvelopeOf(w, c);
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
        double estV = Military.EstablishmentOf(w, c, out _);
        w.ConscriptionLaw = ConscriptionLaw.Selective;
        double estS = Military.EstablishmentOf(w, c, out _);
        Assert.True(estV > 0 && env > 0 && heads > wa, $"前提が崩れた：定員 {estV}・器 {env}・頭数 {heads}・働き盛り {wa}");
        double equip = env / estV - earn / wa * c.SoldierPayMult;   // 志願の定員から逆算した装備の単価（自己費用の物価水準込み）
        double expectS = env / (earn / heads * c.SoldierPayMult + equip);
        Assert.True(estS > estV, $"徴兵制の定員 {estS} が志願制 {estV} より大きくない");
        Assert.True(Math.Abs(estS - expectS) <= 1e-9 * expectS, $"徴兵制の定員 {estS} ≠ 器÷(手当＋装備) {expectS}");
    }

    /// <summary>★(163) 合流（(161) の反証役の指摘 va#3）：徴兵制の兵卒の手当（<see cref="Military.RecruitWage"/> の keep）は徴募の母数の**頭数あたり**＝世帯の子供の数で動く
    /// （(140) が外した依存を徴兵制の兵卒にだけ戻した＝AI の推奨・作者への確認）。規定の給与（働き盛り1人あたり）は動かない。盤面＝(140) の番人と同じ（仏の首都の農民と都市労働者の頭数を倍にし、
    /// 増えた分を全て0〜9歳に置く＝働き盛りの人数と所得は同じ）＝手当はちょうど半分・規定の給与は同じ。⚠突然変異「手当も働き盛りで割る」で赤。</summary>
    [Fact]
    public void 徴兵制の兵卒の手当は徴募の母数の頭数あたりで子供が増えると下がり規定の給与は動かない()
    {
        (double keep, double regular) Arm(bool children)
        {
            var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
            sim.Run(1);
            var w = sim.World; var c = w.Coef; var cap = w.Region;
            w.ConscriptionLaw = ConscriptionLaw.Selective;
            if (children)
                foreach (var p in cap.Pops)
                    if (!p.IsSoldier && (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB) && p.Size > 0)
                    {
                        var a = (double[])p.Ages.Clone();
                        for (int k = 0; k < a.Length; k++) a[k] *= 0.5;
                        a[0] += 0.25; a[1] += 0.25;   // 0〜9歳の男女
                        p.Ages = a; p.Size *= 2;
                    }
            return (Military.RecruitWage(cap, c, last: true, keep: true), Military.RecruitWage(cap, c, last: true));
        }
        var a0 = Arm(false); var a1 = Arm(true);
        Assert.True(a0.keep > 0 && a0.regular > 0, $"前提が崩れた：手当 {a0.keep}・規定の給与 {a0.regular}");
        Assert.True(a0.keep < a0.regular, $"前提が崩れた：頭数あたりの手当 {a0.keep} が働き盛り1人あたりの規定の給与 {a0.regular} 以上");
        Assert.True(Math.Abs(a1.regular - a0.regular) <= 1e-9 * a0.regular, $"子供が増えると規定の給与が動いた（{a0.regular} → {a1.regular}）");
        Assert.True(Math.Abs(a1.keep - 0.5 * a0.keep) <= 1e-9 * a0.keep, $"頭数が倍になっても手当が半分にならない（{a0.keep} → {a1.keep}）");
    }

    /// <summary>★(163) 合流（(162) の反証役の指摘 vb#1＝戦時の装備の追随を差し戻した）：交戦中の月の予算で、装備の上限（給与の後に残る器＝MilitaryBudgetCeiling）は
    /// 戦時の倍率（MilitaryWarMult）で広がらない＝軍事費の器は選んだ段だけが決める（作者裁定 R2 2026-09-12「戦時経済は総動員とセット」・2026-09-19 の裁定2「選んだ軍事費を黙って上げない」）。
    /// 盤面＝仏英（開幕の1四半期の後に仏英を交戦へ）で1か月を進め、月の決算（翌月の予算）の直前に戦時の倍率だけを替えた双子（器を大きく＝装備の上限は正）。兵士の給与の請求も同じ（前提）。
    /// 上限は国家の支払い率 r を掛ける前（請求）で比べる（装備の必要額は戦時の倍率で違う＝r が双子で違う）。
    /// ⚠突然変異「装備の上限を 1＋0.35×(戦時の倍率−1) 倍（(162) の形）」で赤＝戻す回はこの番人を作者の裁定と一緒に書き直す。</summary>
    [Fact]
    public void 交戦中も装備の上限は戦時の倍率で広がらず選んだ段の器のまま()
    {
        (double ceil, double pay, double warMult) Twin(double warMultCoef)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            var w = sim.Nations[0].World; var c = w.Coef;
            sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
            c.MilitaryEnvelopeShare = 1000;     // 器を大きく＝給与の後に装備の上限が残る
            for (int d = 0; d < Calendar.DaysPerMonth - 1; d++) sim.StepDay();
            c.MilitaryWarMult = warMultCoef;    // 月の決算（月の最後の日＝翌月の予算）の直前に替える＝双子は予算までの盤面が同じ（戦時の倍率は日々の戦争の消耗も読む）
            sim.StepDay();
            double ceil = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) ceil += r.MilitaryBudgetCeiling;
            Assert.True(w.FiscalFundingRatio > 0, $"前提が崩れた：国家の支払い率 {w.FiscalFundingRatio}");
            return (ceil / w.FiscalFundingRatio, w.LastSoldierPay, w.Region.MilitaryWarMultLast);   // 請求（r を掛ける前）＝装備の必要額は戦時の倍率で違う＝r が双子で違う
        }
        var hi = Twin(new Coefficients().MilitaryWarMult); var lo = Twin(1.0);
        Assert.True(hi.warMult > lo.warMult && hi.warMult > 1, $"前提が崩れた：戦時の倍率 {hi.warMult}／{lo.warMult}");
        Assert.True(lo.pay > 0 && Math.Abs(hi.pay - lo.pay) <= 1e-12 * lo.pay, $"前提が崩れた：兵士の給与の請求が双子で違う（{lo.pay:G17} → {hi.pay:G17}）");
        Assert.True(lo.ceil > 0, $"前提が崩れた：装備の上限が0（{lo.ceil}）");
        Assert.True(Math.Abs(hi.ceil - lo.ceil) <= 1e-12 * lo.ceil, $"戦時の倍率で装備の上限が広がった（{lo.ceil:G17} → {hi.ceil:G17}）＝選んだ段の外で軍事費が上がる");
    }

    // ===== (175) 戦時の財政＝戦時段の戦費を借入で賄う（戦時国債・推奨で進めた・作者の承認は未確認）=====

    /// <summary>★(175) 戦時段の器（<see cref="Military.EnvelopeAt"/>）＝段×器の割合×使える歳入 ＋ 戦時段の割合×借入の1件の額（<see cref="Fiscal.BorrowTicket"/>）。
    /// 足すのは交戦中かつ総動員中（戦時段を解禁する述語＝<see cref="Government.MilitaryTotalTarget"/> と同じ1本）で、施行が段3 を超えた分だけ（段3→段4 で 0→1・線形）。
    /// 平時・総動員の旗だけ・交戦だけでは段4 の施行でも足さない（作者裁定 2026-09-19 の推奨の2「総動員は戦時段を解禁するだけで選んだ軍事費を黙って上げない」・R2「戦後は下の値に戻る」）。
    /// 借入の門（<see cref="Fiscal.BorrowGateOpen"/>＝冷却・利払いの限界）が閉じれば足さない（実際に借りる門と同じ1本＝思想⑨）。施行中の器（EnvelopeOf）は施行の段の EnvelopeAt。
    /// 盤面＝日英（開幕の1四半期の後・日本は国債が無い＝借入の門が開いている〔仏は開幕の利払いが歳入を超える〕）・値は同じ瞬間の同じ式で比べる（厳密一致）。
    /// ⚠突然変異「戦時の器を足さない」「総動員を読まない」「段3 以下にも足す」「借入の門を読まない」で赤。</summary>
    [Fact]
    public void 戦時段の器は交戦かつ総動員の戦時段の割合だけ借入の1件の額を足し借入の門が閉じれば足さない()
    {
        var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
        sim.StepQuarter();
        sim.Diplomacy.Wars.Clear();
        var w = sim.Nations[0].World; var c = w.Coef; var foe = sim.Nations[1].World.SelfPower!;
        w.Mobilized = false;
        Assert.True(Fiscal.BorrowGateOpen(w, c), $"前提が崩れた：借入の門が閉じている（冷却 {w.BorrowCooldown}・利払い {w.LastInterest:G6}・歳入 {w.LastRevenue:G6}）");
        double ticket = Fiscal.BorrowTicket(w.NominalGdp, c);
        double Base(double lv) => lv * c.MilitaryEnvelopeShare * System.Math.Max(0, w.SpendableRevenue());
        Assert.True(ticket > 0 && Base(1.0) > 0, $"前提が崩れた：1件の額 {ticket}・器 {Base(1.0)}");

        Assert.Equal(Base(1.0), Military.EnvelopeAt(w, c, 1.0));          // 平時＝段4 の施行でも足さない
        w.Mobilized = true;
        Assert.Equal(Base(1.0), Military.EnvelopeAt(w, c, 1.0));          // 総動員の旗だけ（交戦していない）
        w.Mobilized = false;
        sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = foe });
        Assert.Equal(Base(1.0), Military.EnvelopeAt(w, c, 1.0));          // 交戦だけ（総動員なし）
        w.Mobilized = true;                                                 // 交戦かつ総動員＝戦時段の解禁
        w.Policy.MilitarySpendingTarget = 1.0;                              // ★(177) 目標も戦時段（段4）＝選んだ段（目標の段で頭打ち＝下の新しい盤面）
        Assert.Equal(Base(0.5), Military.EnvelopeAt(w, c, 0.5));          // 段2 の施行＝足さない
        Assert.Equal(Base(0.75), Military.EnvelopeAt(w, c, 0.75));        // 段3 の施行＝足さない（総動員だけで1銭も増えない）
        Assert.Equal(Base(0.875) + 0.5 * ticket, Military.EnvelopeAt(w, c, 0.875));   // 段3→段4 の中ほど＝1件の額の半分
        Assert.Equal(Base(1.0) + 1.0 * ticket, Military.EnvelopeAt(w, c, 1.0));       // 段4＝1件の額
        w.SetMilitarySpending(1.0, w.NavalSpendShare);
        Assert.Equal(Military.EnvelopeAt(w, c, w.MilitarySpending), Military.EnvelopeOf(w, c));   // 施行中の器＝施行の段の EnvelopeAt

        w.BorrowCooldown = 3;                                               // 冷却中（デフォルト後の信用喪失）＝借りられない
        Assert.Equal(Base(1.0), Military.EnvelopeAt(w, c, 1.0));
        w.BorrowCooldown = 0;
        w.LastInterest = c.MaxInterestShareOfRevenue * w.LastRevenue;      // 利払いが歳入の限界に届いた＝借りられない
        Assert.False(Fiscal.BorrowGateOpen(w, c));
        Assert.Equal(Base(1.0), Military.EnvelopeAt(w, c, 1.0));
    }

    /// <summary>★(175) 呼び手の側＝月の予算（Simulation の月次決算）は戦時段の器で請求する＝装備の上限（給与の後に残る器・r を掛ける前）が借入の1件の額に比例して広がる。
    /// 盤面＝日英（開幕の1四半期の後に交戦・日本は国債が無い＝借入の門が開いている）・器を大きく（装備の上限が正）・月の決算の直前に総動員と段4 の施行と1件の割合（BorrowFractionOfGdp）だけを替えた双子
    /// ＝決算までの盤面は同じ（1件の割合は借入と戦時段の器だけが読む）。1件の割合 0.10→0.20 と 0.10→0.30 の差が 1:2（線形）・総動員なしの双子は1件の割合で動かない。
    /// ⚠突然変異「月の予算が戦時の器を読まない（旧の式）」で赤。</summary>
    [Fact]
    public void 月の予算は戦時段の器で請求し装備の上限は借入の1件の額に比例して広がる()
    {
        double Twin(double frac, bool mobilized, double target = 1.0)
        {
            var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            var w = sim.Nations[0].World; var c = w.Coef;
            sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
            c.MilitaryEnvelopeShare = 1000;     // 器を大きく＝給与の後に装備の上限が残る
            for (int d = 0; d < Calendar.DaysPerMonth - 1; d++) sim.StepDay();
            w.Mobilized = mobilized; w.SetMilitarySpending(1.0, w.NavalSpendShare);   // 月の決算の直前に戦時段（段4 の施行）へ＝四半期の頭の施行は通らない
            w.Policy.MilitarySpendingTarget = target;                                 // ★(177) 選んだ目標の段（戦時段の割合は目標の段で頭打ち）
            c.BorrowFractionOfGdp = frac;
            Assert.True(Fiscal.BorrowGateOpen(w, c), $"前提が崩れた：借入の門が閉じている（冷却 {w.BorrowCooldown}・利払い {w.LastInterest:G6}・歳入 {w.LastRevenue:G6}）");
            sim.StepDay();
            double ceil = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) ceil += r.MilitaryBudgetCeiling;
            Assert.True(w.FiscalFundingRatio > 0, $"前提が崩れた：国家の支払い率 {w.FiscalFundingRatio}");
            return ceil / w.FiscalFundingRatio;   // 請求（r を掛ける前）
        }
        double a = Twin(0.10, true), b = Twin(0.20, true), c3 = Twin(0.30, true);
        double d20 = b - a, d30 = c3 - a;
        Assert.True(a > 0 && d20 > 1e-6 * a, $"戦時段の装備の上限が1件の額で広がらない（{a:G17} → {b:G17}）＝月の予算が戦時の器を読んでいない");
        Assert.True(System.Math.Abs(d30 - 2 * d20) <= 1e-9 * d30, $"装備の上限の広がりが1件の額に比例しない（0.20 で {d20:G17}・0.30 で {d30:G17}）");
        double p1 = Twin(0.10, false), p2 = Twin(0.30, false);
        Assert.True(p1 > 0 && System.Math.Abs(p2 - p1) <= 1e-12 * p1, $"総動員なしの装備の上限が1件の額で動いた（{p1:G17} → {p2:G17}）＝総動員の外で戦時の器が立つ");
        // ★(177)（反証役 vc2#1）呼び手の側：交戦かつ総動員で施行は段4 でも、選んだ目標が段3 なら月の予算は1件の額で動かない（目標の段で頭打ち）。
        double s1 = Twin(0.10, true, 0.75), s2 = Twin(0.30, true, 0.75);
        Assert.True(s1 > 0 && System.Math.Abs(s2 - s1) <= 1e-12 * s1, $"目標が段3 の総動員で装備の上限が1件の額で動いた（{s1:G17} → {s2:G17}）＝選んでいない戦時段の借入が器に入る");
    }

    /// <summary>★(177)（反証役 vc2#1）戦時段の割合は選んだ目標の段（<see cref="Government.MilitaryTotalTarget"/>）で頭打ち＝戦時段の失効で目標が段3 へ戻った後、
    /// 施行が段3 の上に残ったまま再び総動員しても、選び直さなければ戦時の器（借入の1件の額）は立たない（作者裁定 2026-09-19・「全て推奨・歴史的に正しい方向で」で採った推奨の2
    /// 「総動員は戦時段を解禁するだけで選んだ軍事費を黙って上げない」）。画面（目標の段で予測）とエンジン（施行の段）が同じ値。
    /// 盤面＝日英（開幕の1四半期の後に交戦・総動員）・施行 0.9・目標 段3 と段4 の双子（同じ瞬間の同じ式で厳密一致）。⚠突然変異「施行だけを読む（旧の式）」で赤。</summary>
    [Fact]
    public void 戦時段の割合は選んだ目標の段で頭打ちになり施行が残っても選び直さなければ足さない()
    {
        var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
        sim.StepQuarter();
        sim.Diplomacy.Wars.Clear();
        var w = sim.Nations[0].World; var c = w.Coef;
        sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
        w.Mobilized = true;
        Assert.True(Fiscal.BorrowGateOpen(w, c), $"前提が崩れた：借入の門が閉じている（冷却 {w.BorrowCooldown}・利払い {w.LastInterest:G6}・歳入 {w.LastRevenue:G6}）");
        double ticket = Fiscal.BorrowTicket(w.NominalGdp, c);
        double Base(double lv) => lv * c.MilitaryEnvelopeShare * System.Math.Max(0, w.SpendableRevenue());
        Assert.True(ticket > 0 && Base(1.0) > 0, $"前提が崩れた：1件の額 {ticket}・器 {Base(1.0)}");
        w.SetMilitarySpending(0.9, w.NavalSpendShare);                     // 施行が段3 の上に残っている（戦後の下り途中）
        w.Policy.MilitarySpendingTarget = 0.75;                             // 目標は段3（戦時段の失効の後＝選び直していない）
        Assert.Equal(0.75, Government.MilitaryTotalTarget(w, w.Policy));
        double lv = w.MilitarySpending;
        Assert.True(lv > 0.75 + 1e-6, $"前提が崩れた：施行 {lv}");
        Assert.Equal(Base(lv), Military.EnvelopeOf(w, c));                  // 施行の器に戦時の分を足さない
        Assert.Equal(Base(0.75), Military.EnvelopeAt(w, c, Government.MilitaryTotalTarget(w, w.Policy)));   // 画面の予測（目標の段）も足さない
        w.Policy.MilitarySpendingTarget = 1.0;                              // 選び直した＝戦時段
        Assert.Equal(1.0, Government.MilitaryTotalTarget(w, w.Policy));
        Assert.Equal(Base(lv) + (lv - 0.75) * Government.MilitaryLawStages * ticket, Military.EnvelopeOf(w, c), 9);   // 施行 0.9 の分＝1件の額の約 0.6
    }

    // ===== (181) 戦後の施行＝戦時段の戦費の失効（推奨で進めた・作者の承認は未確認）=====

    /// <summary>★(181) 戦時段の戦費の失効（<see cref="Government.Step"/>）＝作者裁定 R2（2026-09-12）「戦時経済は総動員とセットで、戦後は下の値に戻る」の施行の側。
    /// 解禁（交戦中かつ総動員中）の外では、四半期の頭に施行も段3（高）へ戻る（Move の前＝議決が切れるだけ）。段3 より下の施行は今のまま（Move＝軍の抵抗）。
    /// 解禁の中は施行を削らない。効いた量そのもの＝施行中の器（<see cref="Military.EnvelopeOf"/>＝月の予算の請求の上限）が段3 の器になる。
    /// 盤面＝日英（開幕の1四半期の後）・双子は同じ瞬間からの別の世界（各盤面で新しく作る）。
    /// ⚠突然変異「失効を外す」「解禁の中でも削る」「交戦だけを読む」「総動員だけを読む」「目標へ飛ばす」で赤。</summary>
    [Fact]
    public void 戦時段の戦費は解禁の外で四半期の頭に施行も段3へ失効し段3より下は軍の抵抗のまま()
    {
        (double x, World w, Coefficients c) Run(bool war, bool mob, double x0, double target)
        {
            var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            sim.Diplomacy.Wars.Clear();
            var w = sim.Nations[0].World; var c = w.Coef;
            if (war) sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
            w.Mobilized = mob;
            w.SetMilitarySpending(x0, w.NavalSpendShare);
            w.Policy.MilitarySpendingTarget = target;
            Government.Step(w, c);
            return (w.MilitarySpending, w, c);
        }
        const double High = 0.75;
        // 平時・交戦だけ・総動員だけ＝解禁の外＝施行 0.95 は段3 へ失効（目標が段3＝失効の後の Move は動かない＝厳密に段3）
        foreach (var (war, mob) in new[] { (false, false), (true, false), (false, true) })
        {
            var (x, w, c) = Run(war, mob, 0.95, 1.0);
            Assert.Equal(High, Government.MilitaryTotalTarget(w, w.Policy));   // 前提＝解禁の外の目標は段3
            Assert.True(x == High, $"解禁の外（交戦 {war}・総動員 {mob}）で施行 0.95 が段3 へ失効しない（{x:G17}）");
            Assert.Equal(Military.EnvelopeAt(w, c, High), Military.EnvelopeOf(w, c));   // 効いた量＝施行中の器は段3 の器
        }
        // 解禁の中（交戦かつ総動員・目標は戦時段）＝削らない（施行は目標へ寄るか留まる）
        {
            var (x, w, _) = Run(true, true, 0.95, 1.0);
            Assert.Equal(1.0, Government.MilitaryTotalTarget(w, w.Policy));
            Assert.True(x >= 0.95 - 1e-12, $"解禁の中で施行が削られた（0.95 → {x:G17}）");
        }
        // 目標が段3 より下（段1）＝失効は段3 まで・その先は Move（軍の抵抗）のまま＝目標へは飛ばない
        {
            var (x, _, _) = Run(false, false, 0.95, 0.25);
            Assert.True(x <= High && x > 0.25 + 1e-6, $"段3 より下の施行が Move でなく動いた（0.95 → {x:G17}・目標 0.25）");
            var (y, _, _) = Run(false, false, 0.70, 0.25);
            Assert.True(y <= 0.70 && y > 0.25 + 1e-6, $"段3 より下の施行が Move でなく動いた（0.70 → {y:G17}・目標 0.25）");
        }
    }

    /// <summary>★(181) 呼び手の側＝四半期の頭（Simulation の QuarterStart＝AI の判断→<see cref="Government.Step"/>）を通ると、平時の国の施行は段3 を超えて残らない。
    /// 盤面＝日英（開幕の1四半期の後・平時・総動員なし）・施行を 0.95 に置いて次の四半期の頭の1日だけ進める。双子＝交戦かつ総動員（AI は戦時段を選ぶ）＝段3 の上に残る。
    /// ⚠突然変異「失効を外す」で赤。</summary>
    [Fact]
    public void 四半期の頭を通ると平時の施行は段3を超えて残らず交戦かつ総動員なら残る()
    {
        (double x, bool unlocked) Run(bool warMob)
        {
            var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            sim.Diplomacy.Wars.Clear();
            var w = sim.Nations[0].World;
            if (warMob) sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = sim.Nations[1].World.SelfPower! });
            w.Mobilized = warMob;
            w.SetMilitarySpending(0.95, w.NavalSpendShare);
            sim.StepDay();   // 次の四半期の頭（QuarterStart）
            return (w.MilitarySpending, w.AtWarNow && w.Mobilized);
        }
        var (peace, pu) = Run(false);
        Assert.False(pu);
        Assert.True(peace <= 0.75, $"平時の施行が四半期の頭で段3 へ失効しない（0.95 → {peace:G17}）");
        var (war, wu) = Run(true);
        Assert.True(wu, "前提が崩れた：交戦かつ総動員のまま四半期の頭を通らない（AI が復員した）");
        Assert.True(war > 0.75 + 1e-6, $"交戦かつ総動員の施行が段3 へ削られた（0.95 → {war:G17}）");
    }

    /// <summary>★(182)（反証役 vm2#7）戦時段の戦費の失効は Move の前（<see cref="Government.Step"/>）＝段3 の上の施行は政治資本を使わずに段3 へ戻り、その先の一歩だけが軍の抵抗（Move）を通る。
    /// 盤面＝日英（開幕の1四半期の後・平時・総動員なし）・目標 段1（0.25）。双子＝施行 0.95 と施行 0.75（段3 ちょうど）＝失効の後は同じ点から（ほぼ）同じ一歩＝施行も政治資本の使った量も ±5% で一致し、施行は段3 の下へ進む。
    /// 効いた量そのもの＝施行と政治資本の目減り（<see cref="World.MandateSpent"/>）。⚠突然変異「Move の後で段3 へ削る」で赤（0.95 からの一歩は段3 の上で止まり段3 へ削られる＝施行が段3 の下へ進まない）。</summary>
    [Fact]
    public void 戦時段の戦費の失効はMoveの前で段3の上の施行は政治資本を使わずに戻る()
    {
        (double x, double spent) Run(double x0)
        {
            var sim = new MultiNationSim([NationConfig.Japan(), NationConfig.Britain()], seed: 1789);
            sim.StepQuarter();
            sim.Diplomacy.Wars.Clear();
            var w = sim.Nations[0].World;
            w.Mobilized = false;
            w.SetMilitarySpending(x0, w.NavalSpendShare);
            w.Policy.MilitarySpendingTarget = 0.25;
            double before = w.MandateSpent;
            Government.Step(w, w.Coef);
            return (w.MilitarySpending, w.MandateSpent - before);
        }
        var (xb, sb) = Run(0.75);
        Assert.True(xb < 0.75 - 1e-9, $"前提が崩れた：段3 からの Move が動かない盤面（0.75 → {xb:G17}）");
        var (xa, sa) = Run(0.95);
        Assert.True(xa < 0.75 - 1e-9, $"段3 の上の施行が失効の後に段3 の下へ進まない（0.95 → {xa:G17}）＝失効が Move の後");
        // 一歩の大きさは双子で厳密には一致しない＝Move の中の立場の土台（PolicyPreferences.Build）が保存された施行（0.95 と 0.75）を読む＝速さが約0.1%違う。
        //   突然変異「Move の後で削る」は 0.95 からの一歩（0.7×速さ）の分だけ政治資本を使う＝段3 からの一歩（0.5×速さ）の約1.4倍＝±5% の外。
        Assert.InRange(xa, xb - 0.05 * (0.75 - xb), xb + 0.05 * (0.75 - xb));   // 失効の後は段3 から（ほぼ）同じ一歩
        Assert.InRange(sa, sb * 0.95, sb * 1.05);                                 // 政治資本は段3 からの一歩の分だけ（失効の分は使わない）
    }
}
