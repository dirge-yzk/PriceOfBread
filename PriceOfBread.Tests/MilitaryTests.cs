using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class MilitaryTests
{
    private static Scenario Dev(ulong s) => new() { Seed = s, Kind = ScenarioKind.FrontierDevelopment };

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

    [Fact]
    public void 武力解錠は開発より速いが国庫出血と闘争度を伴う()   // §A-8 強硬 path
    {
        var dev = new Simulation(Dev(1), new NullAgent());
        var force = new Simulation(Force(1), new NullAgent());
        dev.Run(20);
        force.Run(20);   // 早期：武力は解錠済、開発はまだ locked

        Assert.True(force.World.Regions[1].SumPopulation() > dev.World.Regions[1].SumPopulation() + 200,
            $"武力が速くない（武力 {force.World.Regions[1].SumPopulation():F0} vs 開発 {dev.World.Regions[1].SumPopulation():F0}）");
        Assert.True(force.World.Treasury < 100_000, "国庫が出血していない");
        // ★段2（2026-09-09）で物差しを**制圧の最中**へ移した。旧は Telemetry[^1]（20四半期後＝制圧が終わって4年後）を見ていたが、
        //   それが通っていたのは σ=0.03（半減期5.8年）で衝撃が事実上永続していたからで、σ=0.30（半減期5.8か月・作者裁定）では
        //   **解錠が終われば数四半期で鎮まる**＝それが正しい挙動である。機構（制圧は闘争度を伴う）は衝撃が加わっている間に測る。
        //   Territory.Step が月ごとに +ForceUnlockMilitancy を全popへ書き、ForceUnlockRate=0.10 なので解錠は約10か月＝Telemetry[1] は制圧中。
        Assert.True(force.Telemetry[1].AvgMilitancy > dev.Telemetry[1].AvgMilitancy,
            $"制圧の最中でも開発より闘争度が高くない（武力 {force.Telemetry[1].AvgMilitancy:F3} vs 開発 {dev.Telemetry[1].AvgMilitancy:F3}）");
        Assert.True(force.Telemetry[1].AvgMilitancy > 0.05, $"制圧の闘争度が立っていない（{force.Telemetry[1].AvgMilitancy:F3}）");
        Assert.True(force.Telemetry[^1].AvgMilitancy < force.Telemetry[1].AvgMilitancy,
            $"解錠が終わっても闘争度が鎮まらない（制圧中 {force.Telemetry[1].AvgMilitancy:F3} → 20四半期後 {force.Telemetry[^1].AvgMilitancy:F3}）＝σが積分の側で効いていない");
    }

    [Fact]
    public void 武力解錠でも人口と基礎貨幣は保存される()
    {
        var sim = new Simulation(Force(2), new NullAgent());
        double p0 = sim.World.TotalPopulation, base0 = sim.World.BaseMoney;

        var ex = Record.Exception(() => sim.Run(400));
        Assert.Null(ex);
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "人口保存の破れ");
        Assert.True(Math.Abs(sim.World.BaseMoney - base0) < 1e-3 * base0, "基礎貨幣保存の破れ（交易なしでは一定）");
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
        var musket = Assert.Single(r.Producers, p => p.GoodId == 82 && p.RecipeName == "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == 81 && p.RecipeName == "鋳鉄砲");
        foreach (var p in new[] { musket, castGun })
        {
            Assert.Equal(1.0, ProductionDiag.TechGate(p.GoodId, p.TechOnset, r.TechLevel));   // 1783年は満額（退場は下の番人）
            Assert.DoesNotContain(p.Inputs, inp => inp.GoodId == 32);   // 鋼を要しない
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
        var musket = Assert.Single(r.Producers, p => p.GoodId == 82 && p.RecipeName == "マスケット");
        var rifle = Assert.Single(r.Producers, p => p.GoodId == 82 && p.Inputs.Any(inp => inp.GoodId == 32));   // 既定（鋼）レシピ
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == 32 && p.TechOnset > 0);                     // 既定の鋼＝ベッセマー転炉（onset 0.55）
        var crucible = Assert.Single(r.Producers, p => p.GoodId == 32 && p.RecipeName == "坩堝鋼");

        r.Markets[32].Price = Mc(crucible);   // ベッセマー以前＝鋼は坩堝鋼の値段
        Assert.True(Mc(musket) < Mc(rifle), $"坩堝鋼の値段でマスケットが安くない（マスケット {Mc(musket):F3} / 既定 {Mc(rifle):F3}）");
        r.Markets[32].Price = Mc(bessemer);   // ベッセマー以後＝鋼が安い
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
        var musket = Assert.Single(r.Producers, p => p.GoodId == 82 && p.RecipeName == "マスケット");
        var castGun = Assert.Single(r.Producers, p => p.GoodId == 81 && p.RecipeName == "鋳鉄砲");
        var bessemer = Assert.Single(r.Producers, p => p.GoodId == 32 && p.TechOnset > 0);   // 後継を実用にした発見＝鋼の既定レシピ（ベッセマー転炉）
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
}
