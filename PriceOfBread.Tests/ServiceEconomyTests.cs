using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 役務（サービス財）経済の仕様テスト：教育の優先配分（下層→中流→上流）、義務教育の国費購入、
/// 労働市場の清算、社会移動（昇格・転落・聖職者への転身）、兵力（兵士の給与＝市場経由）。
/// 保存則（§11.6）は各シミュレーションの不変条件が全ティックで検査する＝例外が出ないこと自体が検証。
/// </summary>
public class ServiceEconomyTests
{
    // === 直接 EconomyStep を回す最小の細工地域（教育市場のみ）。supplyPerTeacher で希少（0.9）⇄潤沢（5+）を切替 ===
    private static (Region r, Pop worker, Pop engineer, Pop noble) EduRegion(double workerSavings, double compulsoryFunding, double supplyPerTeacher = 0.9)
    {
        int n = Catalog.Goods.Length;
        var markets = new Market[n];
        for (int i = 0; i < n; i++) markets[i] = new Market { Price = 1.0, Stock = 0, TargetStock = 1 };
        markets[Catalog.EducationId].Price = 0.6;

        var edu = new Good { Id = Catalog.EducationId, Name = "教育", Tier = GoodTier.Service, BaseNeed = 0.06, RefPrice = 0.6, Elasticity = 0.8, Spoilage = 1.0, NonTradeable = true };
        var teacher  = new Pop { ClassId = 7, ClassName = "教師", IsUrban = true, Size = 10, Savings = new MoneyStock(0), Satisfaction = 1.0, IsModernElite = true };
        var worker   = new Pop { ClassId = 2, ClassName = "労働者", IsUrban = true, Size = 100, Savings = new MoneyStock(workerSavings), Satisfaction = 1.0 };
        var engineer = new Pop { ClassId = 4, ClassName = "技師", IsUrban = true, Size = 100, Savings = new MoneyStock(10_000), Satisfaction = 1.0, IsModernElite = true };
        var noble    = new Pop { ClassId = 0, ClassName = "貴族", IsUrban = false, Size = 100, Savings = new MoneyStock(10_000), Satisfaction = 1.0, Closed = true };
        // 教育の生産者（教師・投入なし＝紙の市場価格に依存しない純粋な供給制約）。
        var producer = new Producer { GoodId = Catalog.EducationId, Productivity = supplyPerTeacher, Output = supplyPerTeacher * 10, MarginalCost = 0.4, LaborPopIndex = 0, OwnerPopIndex = 0, RentShare = 0.0 };

        var r = new Region
        {
            Id = 0, Name = "テスト",
            Goods = [edu],
            Pops = [teacher, worker, engineer, noble],
            Producers = [producer],
            Markets = markets,
            CompulsoryEduFunding = compulsoryFunding,
        };
        return (r, worker, engineer, noble);
    }

    [Fact]
    public void 教育は下層が優先して買い余剰だけが中流上流へ届く()
    {
        var (r, worker, engineer, noble) = EduRegion(workerSavings: 10_000, compulsoryFunding: 0);
        for (int m = 0; m < 3; m++) EconomyStep.Run(r, new Coefficients());
        Assert.True(worker.EducationFill >= engineer.EducationFill - 1e-9, $"下層({worker.EducationFill:F2})より中流({engineer.EducationFill:F2})が先に教育を得ている");
        Assert.True(engineer.EducationFill >= noble.EducationFill - 1e-9, $"中流({engineer.EducationFill:F2})より上流({noble.EducationFill:F2})が先に教育を得ている");
        Assert.True(worker.EducationFill > 0.8, $"供給があるのに下層の就学が低い（{worker.EducationFill:F2}）");
        Assert.True(noble.EducationFill < 0.5, $"供給が希少なのに上流まで行き渡っている（{noble.EducationFill:F2}）＝優先配分が効いていない");
    }

    [Fact]
    public void 義務教育は買えない者の教育を国庫が買い与える()
    {
        var poor = EduRegion(workerSavings: 0, compulsoryFunding: 0);
        for (int m = 0; m < 3; m++) EconomyStep.Run(poor.r, new Coefficients());
        Assert.True(poor.worker.EducationFill < 0.05, $"無一文の労働者が教育を買えてしまっている（{poor.worker.EducationFill:F2}）");

        // 義務教育＝供給が足りていれば（教師が潤沢）、買えない者の分を国庫が買い与える
        var comp = EduRegion(workerSavings: 0, compulsoryFunding: 1.0, supplyPerTeacher: 5.0);
        for (int m = 0; m < 3; m++) EconomyStep.Run(comp.r, new Coefficients());
        Assert.True(comp.worker.EducationFill > 0.3, $"義務教育なのに貧しい労働者に教育が届かない（{comp.worker.EducationFill:F2}）");
        Assert.True(comp.r.StateSpent[(int)TreasuryCat.Education] > 0, "義務教育の購入が国庫の支出（StateSpent の教育の区分）に計上されていない");
    }

    // ★特権 段1（作者裁定2026-09-08）＝国家の教育給付は行政（＝官僚）を通る。以下2本は**機構**の番人。
    //   水準も創発（識字・技術水準・発見の遅れ）も主張しない＝それは帯が見る。

    [Fact]
    public void 義務教育の給付は配分の総和が国庫の購入量と一致する()
    {
        // Σ_p need×(1−fill)×gr ＝ take。行政の脚を配分率 gr の側に入れると Σ配分＝take/reach になり教育が無から湧く。
        // 全popの貯蓄をゼロにする＝私費購入が立たない＝この期の販売量 m.LastSold は**国庫購入だけ**。
        var (r, _, _, _) = EduRegion(workerSavings: 0, compulsoryFunding: 1.0, supplyPerTeacher: 0.9);
        foreach (var p in r.Pops) p.SetSavings(0);
        r.AdminFill = 0.4;
        EconomyStep.Run(r, new Coefficients { EduReachFromAdmin = 0.5 });
        var m = r.Markets[Catalog.EducationId];
        double dist = r.Pops.Where(p => !p.IsSoldier).Sum(p => p.GoodNeed * p.EducationFill);
        Assert.True(m.LastSold > 1e-9, "国庫が教育を買っていない（テストの前提が崩れている）");
        Assert.True(r.CumEducationCost > 0, "義務教育費が計上されていない");
        Assert.Equal(m.LastSold, dist, 9);   // 配れた量＝買った量
    }

    [Fact]
    public void 教育給付の行政の脚は天井にだけ効き行政ゼロでも給付は消えない()
    {
        static Region Arm(double adminFill)
        {
            var (r, _, _, _) = EduRegion(workerSavings: 0, compulsoryFunding: 1.0, supplyPerTeacher: 5.0);
            foreach (var p in r.Pops) p.SetSavings(0);
            r.AdminFill = adminFill;
            return r;
        }
        // (a) 係数0＝AdminFill を1ビットも見ない（休眠の恒等）
        var offHigh = Arm(1.0); var offLow = Arm(0.0);
        EconomyStep.Run(offHigh, new Coefficients());
        EconomyStep.Run(offLow, new Coefficients());
        Assert.Equal(offHigh.CumEducationCost, offLow.CumEducationCost, 12);

        // (b) 係数0.5＝天井が 1−0.5×(1−AdminFill) 倍。供給が潤沢な腕なので比は形どおり 1:0.5
        var c = new Coefficients { EduReachFromAdmin = 0.5 };
        var onHigh = Arm(1.0); var onLow = Arm(0.0);
        EconomyStep.Run(onHigh, c);
        EconomyStep.Run(onLow, c);
        Assert.Equal(offHigh.CumEducationCost, onHigh.CumEducationCost, 12);   // 行政満額なら係数の有無で変わらない
        Assert.Equal(0.5, onLow.CumEducationCost / onHigh.CumEducationCost, 9);
        // (c) 床＝行政ゼロでも給付は消えない（素の ×AdminFill に書き換えた瞬間に赤）
        Assert.True(onLow.CumEducationCost > 0, "行政ゼロで給付が消えた＝床(1−係数)が失われている");
    }

    [Fact]
    public void 労働市場は清算し賃金は値幅に収まり労働者は収入を得る()
    {
        var sim = new Simulation(new Scenario { Seed = 5, Kind = ScenarioKind.FullChain }, new NullAgent());
        sim.Run(200);
        var m = sim.World.Regions[0].Markets[Catalog.LaborId];
        Assert.True(m.LastProduced > 0, "労働力が生産されていない");
        Assert.True(m.LastSold <= m.LastProduced + m.PrevStock + 1e-6, "労働力の販売が供給を超えている");
        Assert.InRange(m.Price, 0.5 * 0.3 - 1e-9, 0.5 * 3.0 + 1e-9);   // 参照価格0.5の±3倍の値幅
        var worker = sim.World.Regions[0].Pops[1];
        Assert.True(worker.Savings.Value > 0, "労働者が労働力の売上で収入を得ていない");
    }

    [Fact]
    public void 豊かな中流は資本家へ昇格し貧した上流は中流へ転落する()
    {
        var sim = new Simulation(FranceScenario(11), new NullAgent());
        var r = sim.World.Regions[0];
        var capitalist = r.Pops[r.InvestorPopIndex];
        var teacher = r.Pops[r.TeacherPopIndex];
        // 資本家の富を教師へ移す（恐慌で資本が溶け中流に金が残った状況の再現。総貨幣は不変＝保存則を破らない）
        double transfer = capitalist.Savings.Value - capitalist.Size * 0.5;
        capitalist.SetSavings(capitalist.Savings.Value - (transfer));
        teacher.SetSavings(teacher.Savings.Value + (transfer));
        double cap0 = capitalist.Size;
        double mil0 = teacher.Militancy;
        sim.StepQuarter();
        Assert.True(capitalist.Size < cap0, $"無一文の資本家が転落しない（{cap0:F1}→{capitalist.Size:F1}）");
        Assert.True(teacher.Militancy >= mil0, "転落が不満（闘争度）を運んでいない");
    }

    [Fact]
    public void 宗教学校の社会では儲かる聖職者へ教育済みPOPが流れる()
    {
        // フランス＝宗教学校。聖職者は初期から平均よりはるかに豊か（1人あたり3000 vs 平均≈280）＝魅力的な職。
        var sim = new Simulation(FranceScenario(13), new NullAgent());
        var r = sim.World.Regions[0];
        var clergy = r.Pops[r.ClergyPopIndex];
        double c0 = clergy.Size;
        sim.Run(8);
        Assert.True(clergy.Size > c0, $"豊かな聖職者に転身者が流れない（{c0:F1}→{clergy.Size:F1}）＝世俗化を阻む罠が働かない");
    }

    /// <summary>★(145)：兵士は国家の給与（名簿の兵×請求の単価×r）を所得として受け取る（旧＝兵力を市場で売った売上＝兵力の市場は (145) で削除）。</summary>
    [Fact]
    public void 兵士は国家の給与で収入を得る()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.World.GovDebt = new MoneyStock(0);   // P5：仏は重債務開始＝利払いが軍事費を圧迫するため中立財政で隔離
        foreach (var rg in sim.World.Regions) foreach (var pp in rg.Pops) pp.BondHoldings = new MoneyStock(0);   // ★2026-09-21：国債を手で消すなら持ち手の記録（保有台帳）も消す＝Σ保有＝国内元本の不変条件
        sim.Run(20);
        var r = sim.World.Regions[0];
        var soldier = r.Pops[r.SoldierPopIndex];
        Assert.True(soldier.LastEarnedPerMonth.Value > 0, "国家が兵士に給与を払っていない（兵士の所得が0）");
        Assert.True(soldier.Savings.Value > 0, "兵士が給与で収入を得ていない");
    }

    [Fact]
    public void 行政の不足は汚職として税収を漏らすが保存則は破れない()
    {
        var sim = new Simulation(NationConfig.Ottoman().ToScenario(19), new NullAgent());   // 後進国＝官僚不足
        var ex = Record.Exception(() => sim.Run(120));   // 不変条件（貨幣・人口・財収支）が全ティック検査
        Assert.Null(ex);
        Assert.InRange(sim.World.AdminEfficiency, 0.0, 1.0);
    }

    [Fact]
    public void 複数レシピの財は並存し市場が選ぶ()   // 電力＝石炭火力 or 石油火力（A+B+(C or D)＝燃料違いの並存レシピ）
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(23), new NullAgent());
        var r = sim.World.Regions[0];
        Assert.True(r.Producers.Count(p => p.GoodId == Catalog.ElectricityId) >= 2, "電力のレシピ（石炭火力/石油火力）が並存していない");
        Assert.True(r.Producers.Any(p => p.GoodId == Catalog.AdvLaborId), "高度労働力（技師の役務）の生産者が居ない");
        Assert.True(r.Producers.Where(p => p.BuysLabor).All(p => p.Inputs.Any(i => i.GoodId == Catalog.LaborId) && p.Inputs.Any(i => i.GoodId == Catalog.AdvLaborId)),
            "労働力＋高度労働力を買わない工場がある（雇用の一本化の漏れ）");
        var ex = Record.Exception(() => sim.Run(120));   // 複数生産者でも保存則（売上の産出シェア分配）が破れない
        Assert.Null(ex);
    }

    [Fact]
    public void 電化は黎明期には広がらない()   // 時代クランプなし＝発電・送電の値段が高いうちは採算が立たず買われない（創発の遅延）
    {
        var sim = new Simulation(NationConfig.Russia().ToScenario(29), new NullAgent());   // 後進国＝電力が高いまま
        sim.Run(40);   // 1789-1799
        var r = sim.World.Regions[0];
        var factories = r.Producers.Where(p => p.BuysLabor).ToList();
        double avgFill = factories.Count > 0 ? factories.Average(p => p.ElecFill) : 0;
        Assert.True(avgFill < 0.15, $"1790年代のロシアで工場が電化している（平均電化率 {avgFill:P0}）");
    }

    // ⚠★撤去2026-09-03（J-007）：「距離の遠い国は輸送マージンが嵩み交易が細い」を**この層から外した**。
    //
    // ⚠経緯＝この検査は**2度**壊れた。
    //   ①旧版（〜2026-08-02）＝`|CumulativeTradeFlow|` で測っていた＝貨幣保存の台帳であって交易の厚さではなかった。
    //   ②改訂版（〜2026-09-03）＝「日本の交易額 < イギリスの交易額」＝**別々の2国の順位**。
    //     飢餓の政治項（J-007）が入った瞬間に順位が入れ替わって赤になった（日本1,294,558 > 英1,235,041＝5%差）。
    //   ③その場で作り直した「運賃ゼロ腕との比」も赤（日本×1.088 < 英×1.107）。
    //
    // ★結論＝**測っていた対象がこの層のものではなかった**。
    //   ・**機構**の主張＝「運賃は距離×荷価に比例する」は `BilateralMarket` の1行で保証されており、
    //     テストで書き直すと**式の二重定義**になる（当プロジェクトの禁じ手）。
    //   ・**創発**の主張＝「だから遠い国の交易は細い」は**モデルの定理ではない**。
    //     交易量は関税・容量・需要・政治を通る合成の結果で、単一シードの大小関係として固定できる量ではない。
    //   ⇒ 創発の主張は**帯（忠実度ハーネスの貿易開放度）と `tradewhy`**（成約に必要な比が距離込みで出る）が見る。
    //     xUnit は機構と不変条件を見る、という2段構えの分担へ戻した。
    //
    // ⚠これは「テストが赤いから消した」ではない。**機構と無関係な理由で2度書き直しを強いられた物差しは、
    //   その層に置く物差しではない**——J-007 が無くても同じ結論になる。
    //   （★係数を下げてテストを通す案は却下済み＝物差しと対象の逆転。）
    //
    // ★副産物（罠として記録）：作り直しの初版が両腕とも**きっかり×1.000**で赤になった＝
    //   二国間交易は**シム共有の係数**（`MultiNationSim.Coef`）を読み、各国の `World.Coef` を書いても効かない。

    private static Scenario FranceScenario(ulong seed) => new()
    {
        Seed = seed, StartYear = 1789, Kind = ScenarioKind.France,
        PoliticsEnabled = true, InitialPolity = Polity.Absolutist, FiscalEnabled = true,
        TaxRate = 0.10, InitialPollTax = 0.5, InitialTreasury = 200_000, BaseCoercion = 400,   // 人口正規化後＝人口あたり強度＋近世財源（人頭税）
        EducationLaw = EducationLaw.Religious, ExternalPressure = 0.1,
    };

    // === ★教育の三層化 段1 器 EduProducersCoexist（2026-09-12・doc §2-1／LIST §0-9-C 段1・段3）の機構の番人 ===
    //   国家世界（BuildProvince＝聖職者6・教師7）を密閉で組む。器は構築時に読まれる（AddServiceProducers）ので Scenario.Coef で渡す。
    private static Simulation NationWorld(Coefficients c, EducationLaw law, ulong seed = 7) =>
        new(new Scenario { Kind = ScenarioKind.Nation, NationConfig = WorldData.ByName("イギリス"), Seed = seed, Coef = c, EducationLaw = law }, new NullAgent());

    [Fact]
    public void 教育生産者の並置は器ゼロで生産者の数と添字が旧と一致し軌道も動かない()
    {
        var off = NationWorld(new Coefficients(), EducationLaw.Religious);
        var zero = NationWorld(new Coefficients { EduProducersCoexist = 0.0 }, EducationLaw.Religious);
        Assert.Equal(off.World.Regions.Count, zero.World.Regions.Count);
        for (int i = 0; i < off.World.Regions.Count; i++)
        {
            var a = off.World.Regions[i]; var b = zero.World.Regions[i];
            Assert.Equal(a.Producers.Count, b.Producers.Count);
            Assert.Equal(a.EducationProducerIndex, b.EducationProducerIndex);
            Assert.True(a.EducationProducerIndex >= 0, $"{a.Name}：教育の生産者の添字が無い");
            Assert.Equal(1, a.Producers.Count(p => p.GoodId == Catalog.EducationId));   // 旧＝教育の生産者は州に**1件**（月次差し替え）＝器0では2件目を add しない
            Assert.Equal(Catalog.EducationId, a.Producers[a.EducationProducerIndex].GoodId);
            Assert.Equal(Catalog.HealthcareId, a.Producers[a.EducationProducerIndex + 1].GoodId);   // 添字+1 は医療のまま（並びが不変）
        }
        off.Run(8); zero.Run(8);
        Assert.Equal(off.World.BaseMoney, zero.World.BaseMoney);
        Assert.Equal(off.World.TotalPopulation, zero.World.TotalPopulation);
        var r0 = off.World.Regions[0];
        var ep = r0.Producers[r0.EducationProducerIndex];
        Assert.Equal(r0.ClergyPopIndex, ep.LaborPopIndex);   // 宗教学校＝現行の月次差し替えが聖職者を担い手に置いている（器0の世界はこの崖のまま）
        Assert.Equal(off.World.Coef.ClergyEduProductivity, ep.Productivity);
    }

    [Fact]
    public void 教育生産者の並置は器で教区学校と公立学校が常設され法は非活性側の操業をゼロにするだけで担い手を差し替えない()
    {
        var c = new Coefficients { EduProducersCoexist = 1.0 };
        var sim = NationWorld(c, EducationLaw.Religious);
        var w = sim.World;
        foreach (var r in w.Regions)
        {
            Assert.True(r.EducationProducerIndex >= 0 && r.EducationProducerIndex + 1 < r.Producers.Count, $"{r.Name}：教育の生産者の添字が無い");
            var parish = r.Producers[r.EducationProducerIndex]; var pub = r.Producers[r.EducationProducerIndex + 1];
            Assert.Equal(2, r.Producers.Count(p => p.GoodId == Catalog.EducationId));   // 同じ州に**同時に**2件
            Assert.Equal(Catalog.EducationId, parish.GoodId); Assert.Equal(Catalog.EducationId, pub.GoodId);
            Assert.Equal(r.ClergyPopIndex, parish.LaborPopIndex); Assert.Equal(r.ClergyPopIndex, parish.OwnerPopIndex);   // 教区学校＝聖職者の自営
            Assert.Equal(r.TeacherPopIndex, pub.LaborPopIndex); Assert.Equal(r.TeacherPopIndex, pub.OwnerPopIndex);         // 公立学校＝教師の自営
            Assert.Equal(c.ClergyEduProductivity, parish.Productivity); Assert.Equal(c.TeacherEduProductivity, pub.Productivity);
            Assert.Equal(Catalog.PaperId, Assert.Single(parish.Inputs).GoodId); Assert.Equal(Catalog.BookId, Assert.Single(pub.Inputs).GoodId);   // 紙／書籍
            Assert.Equal(0.0, parish.RentShare); Assert.Equal(0.0, pub.RentShare);
        }
        var r0 = w.Regions[0];
        var pa = r0.Producers[r0.EducationProducerIndex]; var pu = r0.Producers[r0.EducationProducerIndex + 1];
        sim.Run(4);   // 宗教学校の1年
        Assert.Equal(0.0, pu.Capacity);   // 法の非活性側（公立学校）＝StepMonth が Scale=0 を置き、EconomyStep が教育財に限り fraction=0 と読む＝能力0
        Assert.Equal(0.0, pu.Output);     // 初期産出1は目標0で減衰し幻の生産のスナップ（<TargetStockFloor）で0
        Assert.True(pa.Capacity > 0 && pa.Output > 0, $"教区学校が動いていない（能力 {pa.Capacity:F2}・産出 {pa.Output:F2}）");
        double parishOut = pa.Output;
        // ⚠担い手・生産性・投入の**差し替えが起きていない**（旧＝月次で LaborPopIndex/Productivity/Inputs[0] を上書き）
        Assert.Equal(r0.ClergyPopIndex, pa.LaborPopIndex); Assert.Equal(r0.TeacherPopIndex, pu.LaborPopIndex);
        Assert.Equal(c.ClergyEduProductivity, pa.Productivity); Assert.Equal(c.TeacherEduProductivity, pu.Productivity);

        // 法を公教育へ（PolityTests と同じ作法＝World と Policy の両方）
        w.EducationLaw = EducationLaw.Public; w.Policy.EducationLaw = EducationLaw.Public;
        sim.Run(4);
        Assert.Equal(0.0, pa.Capacity);   // 今度は教区学校が非活性
        Assert.True(pa.Output < parishOut, $"法を公教育へ変えても教区学校の産出が減らない（{parishOut:F2}→{pa.Output:F2}）");
        Assert.True(pu.Capacity > 0 && pu.Output > 0, $"公教育で公立学校が立ち上がらない（能力 {pu.Capacity:F2}・産出 {pu.Output:F2}）");
        Assert.Equal(r0.ClergyPopIndex, pa.LaborPopIndex); Assert.Equal(r0.TeacherPopIndex, pu.LaborPopIndex);
        Assert.Equal(c.ClergyEduProductivity, pa.Productivity); Assert.Equal(c.TeacherEduProductivity, pu.Productivity);
        Assert.Equal(Catalog.PaperId, Assert.Single(pa.Inputs).GoodId); Assert.Equal(Catalog.BookId, Assert.Single(pu.Inputs).GoodId);
        Assert.Equal(2, r0.Producers.Count(p => p.GoodId == Catalog.EducationId));   // 法を変えても生産者の数は不変（差し替えでなく並置）
    }
}
