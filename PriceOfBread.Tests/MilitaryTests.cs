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
    /// ★(161)：これは**志願制の規定の給与**の物差し（盤面を志願制にした）。徴兵制の兵卒の手当は頭数あたり（世帯の1人ぶんの暮らし）＝下の「徴兵制の兵卒の給与は…」の番人。</summary>
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

    // ===== (154) 呼び手の配線の番人（(151) の反証役の指摘＝Borders・SeaTradeShare・ThreatReach は単体で試しているが、Compute と ThreatTension が読む配線は見ていなかった） =====

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

    /// <summary>★(154) 理想の軍隊の形は守る海の交易（軍×海に出ている経済の割合）を海の必要量に足す（通商保護の配線）。
    /// 盤面＝英日（開幕・外交は止める）。英の港の取扱高を0から正へ変えた差＝英の軍×<see cref="ArmyShape.SeaTradeShare"/>（他の項は取扱高を読まない）。
    /// ⚠突然変異「通商保護の行を消す」で赤（海越えの陸軍だけでも海の必要量は正＝旧の番人は気づかない）。</summary>
    [Fact]
    public void 理想の軍隊の形は守る海の交易を海の必要量に足す()
    {
        var sim = Frozen152("イギリス", "日本");
        var dip = sim.Diplomacy; var c = sim.Coef;
        var ukw = sim.Nations[0].World; var uk = ukw.SelfPower!;
        foreach (var nn in sim.Nations) foreach (var r in nn.World.Regions) r.LastPortThroughput = 0;
        ArmyShape.Compute(dip, c);
        double sea0 = uk.NeedSea;
        foreach (var r in ukw.Regions) r.LastPortThroughput = 1000.0;
        double share = ArmyShape.SeaTradeShare(ukw);
        Assert.True(share > 0 && uk.Military > 0, $"前提が崩れた：割合 {share}・軍 {uk.Military}");
        ArmyShape.Compute(dip, c);
        double expect = uk.Military * share;
        Assert.True(System.Math.Abs(uk.NeedSea - sea0 - expect) <= 1e-9 * System.Math.Max(1.0, expect),
            $"取扱高を入れた差 {uk.NeedSea - sea0:G17} ≠ 軍×割合 {expect:G17}＝Compute が守る海の交易を海の必要量に足していない");
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
    /// 徴兵法の政治＝脅威の割合が半分以上（届く敵意の養える軍が自国の養える軍以上）か交戦中＝徴兵制。⚠突然変異「軍備スタンスが 0.7 を書く（旧）」「徴兵法を軍備スタンスで読む（旧）」で赤。</summary>
    [Fact]
    public void AIの軍事費の段は安全保障の不安で決まり徴兵法の政治も同じ不安を読む()
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
        var decide = typeof(UtilityAgent).GetMethod("DecideModernLaws", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.SecurityThreat = 150;   // 脅威の割合 0.6
        decide.Invoke(null, [w, w.Policy, UtilityAgent.Stance.Industrialize]);
        Assert.Equal(ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
        // ★(157) 線の両側（反証役の指摘＝線の下側が固定されていなかった）：割合 0.5（ちょうど線）は徴兵制・0.4（線の下・平時）は徴兵制にしない（志願制のまま）。
        //   ⚠突然変異「線を 0.3 へ下げる」「線を > 0.5 にする」で赤。
        Assert.False(w.AtWarNow, "前提が崩れた：仏が交戦中（交戦中は割合に依らず徴兵制）");
        w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.SecurityThreat = 100;   // 脅威の割合 0.5
        decide.Invoke(null, [w, w.Policy, UtilityAgent.Stance.Industrialize]);
        Assert.Equal(ConscriptionLaw.Selective, w.Policy.ConscriptionLaw);
        w.Policy.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.SecurityThreat = 100.0 * 0.4 / 0.6;   // 脅威の割合 0.4
        decide.Invoke(null, [w, w.Policy, UtilityAgent.Stance.Industrialize]);
        Assert.Equal(ConscriptionLaw.Volunteer, w.Policy.ConscriptionLaw);
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

    /// <summary>★(161) 徴兵は兵1人を安くする（作者に返した設計「徴兵は兵1人を安くする制度でもあった（仏・普が英より大きな軍を持てた理由）」・承認済みの流れ）：
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
}
