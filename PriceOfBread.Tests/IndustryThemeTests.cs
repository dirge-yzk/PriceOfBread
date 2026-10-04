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
    public void テーマH_既定係数が方針どおり()   // 休眠復旧2026-07-08 第(iii)段：組合賃金を有効化（他はB保留/第(iv)段/前提作業待ち＝トリアージ文書）
    {
        var c = new Coefficients();
        Assert.Equal(0.1, c.InventoryAdjustGain);       // 05-B1点火（2026-07-29）＝期待導入の第一歩。0.2は放任経済テーゼ（過少消費）を消したため0.1へ減量（bisect実測）
        Assert.Equal(0.25, c.LaborSaveRate);            // 休眠復旧2026-07-09 第(iv)段採用（bisect＝挙動4テスト・較正とも無害）
        Assert.Equal(0.5, c.UnionWageRiseBoost);        // 第(iii)段有効化（組合の賃金交渉＝賃上げに速く賃下げに抗う）
        Assert.Equal(0.5, c.CapitalOutputRatio);        // ★(200) 段2（能力は建てた分だけ）：月に1単位の産出の能力の設備の値段＝これ×年の産出の価額（米国の製造業の国勢調査 1880 0.45・1890 0.60）。ヴィンテージ資本は撤去
        Assert.Equal(0.0, c.DomesticMarkupMax);         // B保留（国内clearing直撃）
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
        c.UnionWageRiseBoost = 0.5;                       // 組合賃金
        c.DomesticMarkupMax = 0.5;                        // 国内寡占
        c.HousingMigrationFriction = 0.5;                 // 住宅(財はinertだが摩擦経路は走る・HousingFill=1で無効果)
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));
        Assert.Null(ex);   // §11.6 保存・境界が全ティック保持
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0, "テーマH有効化で人口保存が破れた");
        // 配線確認：機械化度が立つ（工業化が進む実国で）。旧世代資本（ヴィンテージ）は (200) 段2 で撤去
        bool mechWired = sim.World.Regions.Any(r => r.State == RegionState.Active && r.Mechanization > 0);
        Assert.True(mechWired, "LaborSaveRate>0でも機械化度が立たない＝EffectiveQtyへの配線不全");
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
    // ===== ★(200) 段2（生産を市場から導く・能力は建てた分だけ・設計 §3・§3A・作者 2026-09-28「全て推奨で採択」・(185) の形の作り直し）の番人 =====
    private static Simulation Fr200() => new Simulation(NationConfig.France().ToScenario(200), new NullAgent());
    private static Producer Fac200(Region r, int gid)
        => r.Producers.First(p => p.GoodId == gid && EconomyStep.IsFactory(p) && Discovery.Gate(r, p) > 0);

    /// <summary>工場の能力＝建てた設備（Scale＝職場の数）×1職場の産出＝工場労働者の人数を読まない（旧＝人数×取り分×生産性）。
    /// 双子の盤面＝設備を同じに置き、片方だけ工場労働者を倍にする＝能力は1ビットも違わない。</summary>
    [Fact]
    public void 段2_工場の能力は建てた設備で決まり工場労働者の人数を読まない()
    {
        var a = Fr200(); var b = Fr200();
        var ra = a.World.Regions[0]; var rb = b.World.Regions[0];
        var pa = Fac200(ra, Catalog.BreadId); var pb = Fac200(rb, Catalog.BreadId);
        pa.Scale = 10; pb.Scale = 10;
        rb.Pops[pb.LaborPopIndex].Size *= 2;   // 工場労働者だけ倍
        EconomyStep.Run(ra, a.World.Coef, 1.0, a.World.Bank, a.World);
        EconomyStep.Run(rb, b.World.Coef, 1.0, b.World.Bank, b.World);
        Assert.True(pa.Capacity > 0, "設備10職場の工場に能力が無い");
        Assert.Equal(pa.Capacity, pb.Capacity);
    }

    /// <summary>設備の無い工場は能力0＝「設備なしの生産」は立たない（産出は前月から減るだけ）。</summary>
    [Fact]
    public void 段2_設備の無い工場は能力0で産出は減るだけ()
    {
        var a = Fr200(); var r = a.World.Regions[0];
        var p = Fac200(r, Catalog.BreadId);
        p.Scale = 0; p.Output = 50;
        EconomyStep.Run(r, a.World.Coef, 1.0, a.World.Bank, a.World);
        Assert.Equal(0.0, p.Capacity);
        Assert.True(p.Output < 50, $"設備0の工場の産出が減らない（{p.Output:F2}）");
    }

    /// <summary>私的投資の計画（月初の盤面から＝EconomyStep.PlanPrivateInvest）：望む能力の伸び＝速さ×(見込みの採算−ハードル)×基準、
    /// 基準＝(自分の産出＋州の誰も作っていない需要)×投入の充足＋種火の床・金額＝伸び×CapitalPerOutputRef×物価水準。
    /// 遊んでいる設備（Scale）は読まない・同じ財の他の作り手の産出は自分の基準に数えない・採算がハードルを割れば0・助走は寿命の分の置き換えだけ。
    /// 見込みに使った変動費は供給①が同じ月に使う（PlanMc）。</summary>
    [Fact]
    public void 段2_私的投資の計画は自分の産出と州の誰も作っていない需要に比例し採算と投入の充足で決まる()
    {
        var a = Fr200(); var w = a.World; var c = w.Coef; var r = w.Regions[0];
        var p = Fac200(r, Catalog.BreadId); var m = r.Markets[p.GoodId];
        double fl = c.TargetStockFloor, cpo = EconomyStep.CapitalPerOutputRef(p, c), pl = Math.Max(r.PriceLevel, 1e-9);
        Assert.Equal(c.CapitalOutputRatio * Calendar.MonthsPerYear * Catalog.RefPrices[p.GoodId], cpo, 12);
        p.Output = 100; p.Scale = 1000; p.SmoothedMargin = 2.0; p.LastInputFill = 1; p.Bankrupt = false;
        m.LastDemand = 40; m.LastProduced = 100;   // 作り手はこの工場だけ・需要は産出より小さい
        double Want() { EconomyStep.PlanPrivateInvest(r, c, w); return p.InvestWant; }
        double w0 = Want();
        Assert.True(w0 > 0, "採算の高い工場に計画が立たない");
        double mc = EconomyStep.MineralSelfCost(p, r, c, w) + EconomyStep.InputCostPerUnit(p, r, r, c);
        Assert.Equal(mc, p.PlanMc, 12);   // 供給①が同じ値を使う
        double k = w0 / ((100 + fl) * cpo * pl);   // ＝速さ×超過
        p.Scale = 10;   // 設備の大きさは読まない
        Assert.Equal(w0, Want(), 9);
        m.LastDemand = 300;   // 誰も作っていない需要＝300−100
        Assert.Equal(k * (300 + fl) * cpo * pl, Want(), 9);
        m.LastProduced = 250;   // 同じ財の他の作り手が150を作っている＝誰も作っていない需要は50（他の作り手の産出を自分の基準に数えない）
        Assert.Equal(k * (150 + fl) * cpo * pl, Want(), 9);
        p.LastInputFill = 0.25;   // 投入の充足を掛ける（種火の床には掛けない）
        Assert.Equal(k * (150 * 0.25 + fl) * cpo * pl, Want(), 9);
        p.LastInputFill = 0;   // 投入が全く入らない工場には種火だけ
        Assert.Equal(k * fl * cpo * pl, Want(), 9);
        p.LastInputFill = 1; p.SmoothedMargin = -5;   // 見込みの採算がハードルを割る
        Assert.Equal(0.0, Want());
        p.SmoothedMargin = 2.0; p.Bankrupt = true;   // 倒産中は計画しない
        Assert.Equal(0.0, Want());
        p.Bankrupt = false; w.WarmupFrozen = true; p.Capacity = 500;   // 助走＝寿命の分の置き換えだけ
        Assert.Equal(c.IndDepreciation * 500 * 1.0 * cpo * pl, Want(), 9);
        w.WarmupFrozen = false;
    }

    /// <summary>私的投資は見込みの採算の高い工場へ積まれ、採算の無い工場には積まれず、投入の入らない工場には種火（TargetStockFloor の分）だけ。設備は寿命（IndDepreciation）で減る。
    /// 呼び手の盤面（EconomyStep.Run）＝計画が決済で設備に積まれることまで確かめる。</summary>
    [Fact]
    public void 段2_私的投資は採算の高い工場へ積まれ採算の無い工場には積まれず投入の入らない工場には種火だけで寿命で減る()
    {
        var a = Fr200(); var w = a.World; var r = w.Regions[0];
        var hi = Fac200(r, Catalog.BreadId); var lo = Fac200(r, Catalog.FurnitureId); var starved = Fac200(r, Catalog.ClothingId);
        foreach (var p in new[] { hi, lo, starved }) { p.Scale = 10; p.LastInputFill = 1; p.Output = 100; }
        hi.SmoothedMargin = 1.0; starved.SmoothedMargin = 1.0; lo.SmoothedMargin = -0.5;
        starved.LastInputFill = 0;
        r.Markets[lo.GoodId].Price = 1e-6;   // 今の採算も赤字
        r.Pops[r.InvestorPopIndex].SetSavings(1e9);
        double d = 1.0 - w.Coef.IndDepreciation;
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        Assert.Equal(10 * d, lo.Scale, 9);
        Assert.True(hi.Scale > 10 * d + 1e-6, $"採算の高い工場に設備が積まれない（{hi.Scale:F6}）");
        Assert.True(starved.Scale >= 10 * d - 1e-12 && starved.Scale - 10 * d < 0.05 * (hi.Scale - 10 * d),
            $"投入の入らない工場に種火を超える設備が積まれた（{starved.Scale - 10 * d:E3} 対 {hi.Scale - 10 * d:E3}）");
        Assert.True(r.InvestSpentMonth > 0, "投資家が建設費を立て替えていない");
    }

    /// <summary>建った束＝資材は参照の価値で互いに置き換わるが、人手（建設の工賃＝労働）は置き換わらない（巨大建造物§3-2「資本があっても人がいないと工場は建たない」）
    /// ＝工場労働者が居ない州では、資材を買えても設備はほぼ建たない（(185) の価値の和は工賃だけでも資材だけでも建った＝反証役 vb1#8）。双子の盤面で比べる。</summary>
    [Fact]
    public void 段2_資材があっても人手が無ければ設備は建たない()
    {
        double Grow(bool noHands)
        {
            var a = Fr200(); var w = a.World; var r = w.Regions[0];
            var hi = Fac200(r, Catalog.BreadId);
            hi.Scale = 10; hi.Output = 100; hi.LastInputFill = 1; hi.SmoothedMargin = 2.0;
            r.Pops[r.InvestorPopIndex].SetSavings(1e9);
            if (noHands)   // 労働力（建設の工賃が買う財）の作り手を止める＝働き手を消し、前月の産出と市場の在庫も0
            {
                foreach (var pr in r.Producers.Where(x => x.GoodId == Catalog.LaborId)) { r.Pops[pr.LaborPopIndex].Size = 1e-9; pr.Output = 0; }
                r.Markets[Catalog.LaborId].Stock = 0;
            }
            double d = 1.0 - w.Coef.IndDepreciation;
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
            return hi.Scale - 10 * d;
        }
        double with = Grow(false), without = Grow(true);
        Assert.True(with > 1e-6, $"人手のある州で設備が建たない（{with:E3}）");
        Assert.True(without < 0.05 * with, $"人手の無い州で設備が建った（{without:E3} 対 {with:E3}）");
    }

    /// <summary>国の池（作者裁定 P5「国全体で1つの池」）：州の投資家が立て替えた私的投資を、池への出資（月初の投資枠）の比で負担し直す＝お金は移るだけ。
    /// 精算した額と計画の印は0へ戻る（同じ月を2度精算しない）。</summary>
    [Fact]
    public void 段2_国の池は立て替えた私的投資を出資の比で負担し直しお金は移るだけ()
    {
        var a = Fr200(); var w = a.World;
        Assert.True(w.Regions.Count >= 2);
        var r0 = w.Regions[0]; var r1 = w.Regions[1];
        foreach (var r in w.Regions) { r.InvestSpentMonth = 0; r.InvestPoolShare = 0; r.InvestPlanned = true; }
        r0.InvestSpentMonth = 1000; r0.InvestPoolShare = 1; r1.InvestPoolShare = 3;
        var i0 = r0.Pops[r0.InvestorPopIndex]; var i1 = r1.Pops[r1.InvestorPopIndex];
        i0.SetSavings(5000); i1.SetSavings(5000);
        Simulation.SettleInvestPool(w);
        Assert.Equal(5750, i0.Savings, 6);   // 出資 1/4 だけ負担＝750 を受け取る
        Assert.Equal(4250, i1.Savings, 6);   // 出資 3/4＝立て替えていない分 750 を払う
        Assert.Equal(10000, i0.Savings + i1.Savings, 6);
        Assert.Equal(0.0, r0.InvestSpentMonth);
        Assert.All(w.Regions, r => Assert.False(r.InvestPlanned));
    }

    /// <summary>呼び手の側（Simulation.MonthSettle）：池の前段が全ての州の計画と出資を先に足す＝貯蓄の無い州の工場にも国の池から設備が建つ（P5）。
    /// 後段が立て替えを精算して0へ戻す。前段が計画しないと、貯蓄の無い州は自分の投資枠（0）で縮める＝建たない。</summary>
    [Fact]
    public void 段2_国の池は月の決算の中で全ての州の計画を先に足し貯蓄の無い州の工場にも建つ()
    {
        var a = Fr200(); var w = a.World;
        Assert.True(w.Regions.Count >= 2);
        var rich = w.Regions[0]; var poor = w.Regions[1];
        foreach (var r in w.Regions) if (r.InvestorPopIndex >= 0) r.Pops[r.InvestorPopIndex].SetSavings(0);
        rich.Pops[rich.InvestorPopIndex].SetSavings(1e9);
        var p = Fac200(poor, Catalog.BreadId);
        p.Scale = 10; p.Output = 100; p.LastInputFill = 1; p.SmoothedMargin = 2.0;
        a.RebaselineInvariants();   // 盤面の貯蓄を置き直した＝保存則の基準線を取り直す
        double d1 = 1.0 - w.Coef.IndDepreciation;
        for (int day = 0; day < Calendar.DaysPerMonth; day++) a.StepDay();   // 1か月＝月の決算1回（貯蓄の無い州の投資家は月末まで貯蓄0）
        Assert.True(p.Scale > 10 * d1 + 1e-6, $"貯蓄の無い州の採算の高い工場に国の池から設備が建たない（{p.Scale:F6}）");
        Assert.True(w.Regions.Sum(r => r.InvestPoolShare) > 0, "池の前段が出資を撮っていない");
        Assert.All(w.Regions, r => Assert.Equal(0.0, r.InvestSpentMonth));   // 後段が精算した
        Assert.All(w.Regions, r => Assert.False(r.InvestPlanned));
    }

    /// <summary>インフラの保守と送電網も工場の設備と同じ計画の金額に入り、同じ充足で縮む＝投資枠を2度使わない（(185) は保守と送電網を投資枠の残りでなく枠の全額から払った）。
    /// 世界の無い州（池＝この州の投資枠）で、計画が投資枠の10倍を超える盤面＝立て替えは投資枠の近くに収まる（値段の清算の差の分だけ）。</summary>
    [Fact]
    public void 段2_保守と送電網も同じ池の同じ充足で縮み投資枠を2度使わない()
    {
        var a = Fr200(); var w = a.World; var c = w.Coef; var r = w.Regions[0];
        r.Industrialization = 100.0;   // 保守の量＝InfraMaintRate×立っている資本の総量（保守が計画の大きな割合になる盤面＝縮まない保守を見分ける）
        foreach (var f in r.Producers.Where(EconomyStep.IsFactory)) { f.SmoothedMargin = 2.0; f.LastInputFill = 1; f.Output = Math.Max(f.Output, 10); }
        double plan = EconomyStep.PlanPrivateInvest(r, c, null);
        double factoryPlan = r.Producers.Sum(f => f.InvestWant);
        Assert.True(r.InvestMaintQty > 0, "保守の量が計画に入らない");
        Assert.True(plan - factoryPlan > 0.1 * plan, $"計画の金額に保守と送電網が入らない（{plan - factoryPlan:F1} 対 計画 {plan:F1}）");
        double budget = plan / 20;   // 計画の 1/20 の投資枠
        r.Pops[r.InvestorPopIndex].SetSavings(budget / c.IndInvestmentFrac);
        r.Pops[r.InvestorPopIndex].Earned = 0;   // 前の月の所得の項（IndInvestIncomeFrac）を0に＝投資枠＝貯蓄の IndInvestmentFrac＝計画の 1/20（所得が残ると充足が1になり保守の縮みを見ない）
        Assert.True(Math.Abs(EconomyStep.InvestBudget(r, c, 0, true) - budget) <= 1e-9 * budget, "投資枠が貯蓄の IndInvestmentFrac でない");
        EconomyStep.Run(r, c, 1.0, null, null);
        Assert.True(r.InvestSpentMonth > 0, "私的投資が立たない");
        Assert.True(r.InvestSpentMonth < 1.5 * budget, $"投資枠を超えて払った（{r.InvestSpentMonth:F1} 対 投資枠 {budget:F1}）");
    }

    /// <summary>国営の棟の完成は設備（Scale）を旧の1棟（FactoryBuildScale）×旧の操業規模1の職場の数（SlotsPerScaleUnit＝工場労働者の人数÷工場の数＝盤面から導く）だけ足し、
    /// 国営持分（BuiltScale）は旧の単位で1棟だけ足す＝棟の数（StateUnits）と国営の割合（StateProfitShare＝持分×換算÷設備）が同じ換算を読む。</summary>
    [Fact]
    public void 段2_国営の棟の完成は設備を旧の1棟の職場の数だけ足し持分は棟の単位()
    {
        var a = Fr200(); var w = a.World; var r = w.Regions[0];
        var p = Fac200(r, Catalog.BreadId);
        double sus = EconomyStep.SlotsPerScaleUnit(p, r);
        int nFac = r.Producers.Count(f => EconomyStep.IsFactory(f) && f.LaborPopIndex == p.LaborPopIndex);
        Assert.Equal(r.Pops[p.LaborPopIndex].Size / nFac, sus, 9);
        double k0 = p.Scale, b0 = p.BuiltScale;
        p.ConstructionLeft = 1e-3; p.ConstructionPayPerDay = 1;   // 残り1日の端（採算率×支払い率の1日の進みで完成する）
        w.StepConstruction(1);
        Assert.Equal(0.0, p.ConstructionLeft);
        Assert.Equal(k0 + w.Coef.FactoryBuildScale * sus, p.Scale, 9);
        Assert.Equal(b0 + w.Coef.FactoryBuildScale, p.BuiltScale, 12);
        Assert.Equal(1, World.StateUnits(p, w.Coef));
        Assert.Equal(Math.Min(1.0, p.BuiltScale * sus / p.Scale), World.StateProfitShare(p, r, w.Coef), 12);
    }

    /// <summary>工場も倒産で退出する（2026-07-09 の決定「倒産退出」）＝民営の設備は清算されて0・再参入は設備0のまま（能力は建てた分だけ＝私的投資が種火から建てる）。
    /// 産地（RGO）は旧の倒産と再参入の規模のまま（対照）。</summary>
    [Fact]
    public void 段2_工場も倒産で退出し設備0のまま再参入する()
    {
        var a = Fr200(); var w = a.World; var r = w.Regions[0];
        var f = Fac200(r, Catalog.FurnitureId);
        var g = r.Producers.First(p => p.IsRgo && p.GoodId == Catalog.WheatId);
        f.Scale = 10;
        foreach (var p in new[] { f, g }) { p.SmoothedMargin = -0.95; p.BuiltScale = 0; r.Markets[p.GoodId].Price = 1e-6; }
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        Assert.True(f.Bankrupt, "慢性の赤字の工場が倒産しない");
        Assert.Equal(0.0, f.Scale);
        Assert.True(g.Bankrupt, "産地の倒産の経路が死んでいる（対照）");
        r.Markets[f.GoodId].Price = 1e3; r.Markets[g.GoodId].Price = 1e3;   // 採算が戻る＝再参入
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        Assert.False(f.Bankrupt);
        Assert.Equal(0.0, f.Scale);   // 設備は無から湧かない
        Assert.False(g.Bankrupt);
        Assert.Equal(w.Coef.FactoryReentryScale, g.Scale, 12);   // 産地は旧の再参入の規模
    }

    /// <summary>助走（WarmupFrozen）の間は工場の設備を労働の取り分（旧の能力＝人数×取り分）で置き直す＝1783年の設備。</summary>
    [Fact]
    public void 段2_助走の間は設備を労働の取り分で置く()
    {
        var a = Fr200(); var w = a.World; var r = w.Regions[0];
        var facs = r.Producers.Where(EconomyStep.IsFactory).ToList();
        int lp = facs[0].LaborPopIndex;
        foreach (var p in facs) p.Scale = 1.0;
        w.WarmupFrozen = true;
        EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        w.WarmupFrozen = false;
        double sum = facs.Where(p => p.LaborPopIndex == lp).Sum(p => p.Scale);
        Assert.Equal(r.Pops[lp].Size, sum, 6);
    }

    /// <summary>〔7-1〕恐慌で遊んでいる設備の解体（作者裁定 2026-09-06 §4-1）＝慢性の赤字で止まっている設備だけ：止まっている割合＝平滑化した採算の負の深さ÷SupplySlope（0〜1）
    /// ×(1−scrapKeep)。採算が正の工場は壊さない・国営の持分も同じ割合。</summary>
    [Fact]
    public void 段2_恐慌の解体は慢性の赤字の工場の設備だけを止まっている割合で解体する()
    {
        var c = new Coefficients();
        var r = new Region();
        var ok = new Producer { BuysLabor = true, Scale = 100, SmoothedMargin = 0.2 };
        var half = new Producer { BuysLabor = true, Scale = 100, BuiltScale = 2, SmoothedMargin = -0.5 * c.SupplySlope };
        var dead = new Producer { BuysLabor = true, Scale = 100, SmoothedMargin = -3.0 };
        var rgo = new Producer { IsRgo = true, Scale = 1.0, SmoothedMargin = -3.0 };
        r.Producers.AddRange([ok, half, dead, rgo]);
        EconomyStep.ScrapIdleCapital(r, 0.9, c);
        Assert.Equal(100.0, ok.Scale);
        Assert.Equal(100 * (1 - 0.1 * 0.5), half.Scale, 12);
        Assert.Equal(2 * (1 - 0.1 * 0.5), half.BuiltScale, 12);
        Assert.Equal(100 * 0.9, dead.Scale, 12);
        Assert.Equal(1.0, rgo.Scale);   // 産地は対象外
    }

    /// <summary>呼び手の側（Simulation の恐慌の資本破壊の段）：閾値を超えたドローダウンの月に、慢性の赤字の工場の設備が寿命より速く減る・採算が正の工場は寿命だけ。</summary>
    [Fact]
    public void 段2_恐慌の月に月の決算が慢性の赤字の工場の設備を解体する()
    {
        var a = Fr200(); var w = a.World; var r = w.Regions[0];
        var bad = Fac200(r, Catalog.FurnitureId);
        bad.Scale = 100; bad.SmoothedMargin = -0.6; bad.BuiltScale = 0;
        r.Markets[bad.GoodId].Price = 1e-6;   // 赤字が続く（平滑の採算は倒産の線〔−0.85〕へ3か月では届かない）
        typeof(World).GetProperty(nameof(World.GdpPeak))!.SetValue(w, Math.Max(1e3, w.GdpTrend * 10));   // ドローダウン 0.9
        Assert.True(w.GdpDrawdown > w.Coef.DepressionScrapThreshold + 0.5);
        double d3 = Math.Pow(1.0 - w.Coef.IndDepreciation, 3);
        a.Run(1);
        Assert.False(bad.Bankrupt);
        Assert.True(bad.Scale < 100 * d3 * 0.97, $"恐慌の月に慢性の赤字の工場の設備が寿命の分しか減らない（{bad.Scale:F3} 対 {100 * d3:F3}）");
    }

    /// <summary>助走の終わりに景気の物差しのピークをトレンドへ置き直す（暦の外で付いたピークを覚えない＝国庫の置き直しと同じ理由）＝開幕のドローダウンは0。</summary>
    [Fact]
    public void 段2_景気のピークは助走の終わりにトレンドへ置き直す()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new UtilityAgent());
        sim.StartWarmup(null, 4);
        Assert.All(sim.Nations, n =>
        {
            Assert.Equal(n.World.GdpTrend, n.World.GdpPeak);
            Assert.Equal(0.0, n.World.GdpDrawdown);
        });
    }
}
