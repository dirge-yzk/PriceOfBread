using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(469) 線 mw：労働の下限の撤去（崖 <see cref="Coefficients.LaborFloorOffGate"/>・帯の床 <see cref="Coefficients.LaborBandFloorOffGate"/>）と最低賃金法（<see cref="PolicyAxis.MinimumWage"/>）の番人。
/// ★(459) 線 lw の法の軸の番人を土台にし、留保賃金の広がり σ の番人を撤去の番人へ置き換えた。
/// 既定（3 本の旗が 0）は今の挙動と厳密恒等（成績表の全行一致は腕 A が見る）＝ここは機構の形と、軸の配線の漏れを見る。
/// 期待値は実装と同じ出所から作らない：旧の式はテストの中に書き直した参照実装、軸の件数は表ごとに別の出所（列挙・表・文言・方針の欄の反射）から数える。</summary>
public class LaborMarketFormTests
{
    // ---------------------------------------------------------------- 供給の式（崖の撤去）

    /// <summary>旧の採算の率（参照実装・EconomyStep の旧の1行をここに書き直した物）。</summary>
    private static double OldProfitFactor(double price, double mc, double slope) => price <= mc ? 0.0 : System.Math.Min(1.0, (price - mc) / (mc * slope));

    private static Producer Human(int good) => new() { GoodId = good, RecipeName = "" };

    [Fact]
    public void 崖の撤去のゲートが0なら採算の率は旧の式とビットで一致する()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.LaborFloorOffGate);
        Assert.Equal(0.0, c.LaborBandFloorOffGate);
        foreach (int g in new[] { Catalog.LaborId, Catalog.FarmLaborId, Catalog.AdvLaborId, Catalog.SteelId })
            foreach (double mc in new[] { 0.1, 0.35, 1.7 })
                foreach (double k in new[] { 0.0, 0.5, 0.97, 1.0, 1.03, 1.2, 1.5, 1.51, 3.0, 40.0 })
                {
                    double price = mc * k;
                    Assert.Equal(OldProfitFactor(price, mc, c.SupplySlope), EconomyStep.ProfitFactor(Human(g), price, mc, c));
                }
    }

    [Fact]
    public void 崖の撤去が正なら人力の労働力と農業労働力だけが値段が正なら能力いっぱいになる()
    {
        var c = new Coefficients { LaborFloorOffGate = 1 };
        double mc = 0.35;
        // 人力＝崖が消える：mc 以下でも・mc の半分でも供給は能力いっぱい（旧は 0）
        foreach (int g in new[] { Catalog.LaborId, Catalog.FarmLaborId })
        {
            foreach (double k in new[] { 1e-6, 0.5, 1.0, 1.2, 40.0 })
                Assert.Equal(1.0, EconomyStep.ProfitFactor(Human(g), mc * k, mc, c));
            Assert.Equal(0.0, OldProfitFactor(mc * 0.5, mc, c.SupplySlope));   // 旧では 0（対照）
            // 否定形＝値段が NaN・0・負なら働かない側
            foreach (double bad in new[] { double.NaN, 0.0, -1.0 }) Assert.Equal(0.0, EconomyStep.ProfitFactor(Human(g), bad, mc, c));
        }
        // 対象外＝旧のまま：畜力・蒸気（レシピ名あり）・投入を持つ人力・技師・工場
        var animal = new Producer { GoodId = Catalog.LaborId, RecipeName = "畜力" };
        var withInput = new Producer { GoodId = Catalog.LaborId, RecipeName = "", Inputs = [new Input(Catalog.SteelId, 0.1)] };
        foreach (var s in new[] { animal, withInput, Human(Catalog.AdvLaborId), Human(Catalog.SteelId) })
            foreach (double k in new[] { 0.5, 1.0, 1.2, 2.0 })
                Assert.Equal(OldProfitFactor(mc * k, mc, c.SupplySlope), EconomyStep.ProfitFactor(s, mc * k, mc, c));
    }

    [Fact]
    public void 崖の撤去が2なら人力の供給は原点を通る線形で満額の値段は旧と同じ_弾力を残す()
    {
        var c = new Coefficients { LaborFloorOffGate = 2 };
        double mc = 0.35, slope = c.SupplySlope;
        foreach (int g in new[] { Catalog.LaborId, Catalog.FarmLaborId })
        {
            double prev = -1;
            foreach (double k in new[] { 1e-6, 0.01, 0.1, 0.5, 1.0, 1.2, 1.4999, 1.5, 3.0, 40.0 })
            {
                double expect = System.Math.Min(1.0, k / (1.0 + slope));   // テストの側に書いた参照（満額＝旧と同じ mc×(1＋SupplySlope)・原点を通る）
                double pf = EconomyStep.ProfitFactor(Human(g), mc * k, mc, c);
                Assert.Equal(expect, pf, 12);
                Assert.True(pf >= prev, "単調でない");
                prev = pf;
            }
            Assert.Equal(1.0, EconomyStep.ProfitFactor(Human(g), mc * (1 + slope), mc, c), 12);   // 旧の満額の位置
            Assert.True(EconomyStep.ProfitFactor(Human(g), mc, mc, c) > 0.5);                       // 旧は mc で 0（対照）
            foreach (double bad in new[] { double.NaN, 0.0, -1.0 }) Assert.Equal(0.0, EconomyStep.ProfitFactor(Human(g), bad, mc, c));
        }
        // 対象外は旧のまま（1 のときと同じ）
        var animal = new Producer { GoodId = Catalog.LaborId, RecipeName = "畜力" };
        foreach (var s in new[] { animal, Human(Catalog.AdvLaborId), Human(Catalog.SteelId) })
            foreach (double k in new[] { 0.5, 1.0, 1.2, 2.0 })
                Assert.Equal(OldProfitFactor(mc * k, mc, slope), EconomyStep.ProfitFactor(s, mc * k, mc, c));
    }

    // ---------------------------------------------------------------- 世界の中

    private static double LaborPrice(Simulation sim) => sim.World.Region.Markets[Catalog.LaborId].Price;

    /// <summary>人力の労働力の自己費用（名目・州の乗数込み・エンジンが崖に使う値と同じ関数）。</summary>
    private static double HumanLaborMc(Simulation sim)
    {
        var r = sim.World.Region;
        var s = r.Producers.First(p => p.GoodId == Catalog.LaborId && EconomyStep.IsHumanLaborSupply(p));
        return EconomyStep.MineralSelfCost(s, r, sim.World.Coef, sim.World) + EconomyStep.InputCostPerUnit(s, r, r, sim.World.Coef);
    }

    [Fact]
    public void 世界の崖の撤去が正なら労働の値段は自己費用の下を通り産出は能力に張り付き_保存則と有限性は保たれる()
    {
        var a = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var b = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        b.World.Coef.LaborFloorOffGate = 1;
        var moneyB = new MoneyConservation(b.World);
        a.Run(8); b.Run(8);
        moneyB.Check(b.World);
        Assert.True(double.IsFinite(LaborPrice(b)) && LaborPrice(b) > 0);
        // 旧の盤面の創発（旧の世界の値段が自己費用の直上に居る）は番人に固定しない＝腕の N0（labor_s）が読む（機構のテストを旧の盤面に固定しない・(469) 追記 3）
        // 撤去した世界では値段が自己費用を下回る（下限が消えた証拠・事前登録 N0）
        Assert.True(LaborPrice(b) < HumanLaborMc(b) * 0.9, $"崖が外れていない（値段 {LaborPrice(b):F4}・自己費用 {HumanLaborMc(b):F4}）");
        // 供給は能力いっぱい：売りに出た量（産出）が旧の世界より大きい（この小さな世界は能力が需要の 4% 増し程度＝桁の違いは多国の世界で腕が見る）
        var rb = b.World.Region; var ra = a.World.Region;
        Assert.True(rb.Markets[Catalog.LaborId].LastProduced > 1.1 * ra.Markets[Catalog.LaborId].LastProduced, $"産出が増えていない（旧 {ra.Markets[Catalog.LaborId].LastProduced:F3}・新 {rb.Markets[Catalog.LaborId].LastProduced:F3}・需要 {rb.Markets[Catalog.LaborId].LastDemand:F3}・能力 {rb.Producers.Where(p => EconomyStep.IsHumanLaborSupply(p) && p.GoodId == Catalog.LaborId).Sum(p => p.Capacity):F3}）");
        // 失業（1−売れた量÷産出）の跳びは能力が需要の約 20 倍の多国の世界の話（事前登録 §0 ③）＝この小さな世界では確かめず、腕の labor_s が読む
    }

    [Fact]
    public void 世界の崖の撤去が2でも労働の値段は自己費用の下を通り_保存則と有限性は保たれる()
    {
        var b = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        b.World.Coef.LaborFloorOffGate = 2;
        var moneyB = new MoneyConservation(b.World);
        b.Run(8);
        moneyB.Check(b.World);
        Assert.True(double.IsFinite(LaborPrice(b)) && LaborPrice(b) > 0);
        Assert.True(LaborPrice(b) < HumanLaborMc(b) * 0.9, $"崖が外れていない（値段 {LaborPrice(b):F4}・自己費用 {HumanLaborMc(b):F4}）");
    }

    [Fact]
    public void 既定の3つの旗が0なら明示の0と同じ軌道()
    {
        var a = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var b = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        b.World.Coef.LaborFloorOffGate = 0.0; b.World.Coef.LaborBandFloorOffGate = 0.0; b.World.Coef.MinimumWageLawGate = 0.0;
        a.Run(4); b.Run(4);
        Assert.Equal(LaborPrice(a), LaborPrice(b));
        Assert.Equal(a.World.Region.Markets[Catalog.FarmLaborId].Price, b.World.Region.Markets[Catalog.FarmLaborId].Price);
    }

    // ---------------------------------------------------------------- 帯の床の撤去（MarketFlow・労働 3 財の枝の中だけ）

    private static readonly int[] Labor3 = [Catalog.LaborId, Catalog.FarmLaborId, Catalog.AdvLaborId];

    private static double Wrp(Simulation s, int gi) => s.World.Region.GoodOf(gi)!.RefPrice * s.World.Region.PriceLevel;

    /// <summary>労働 3 財の市場を〈供給が需要より桁違いに多い〉状態に置き、価格を参照価格（物価水準込み）の何倍かに置いた世界（MarketFlow の単体の確かめ用）。</summary>
    private static Simulation OversuppliedLabor(double bandOff, double priceOverRef)
    {
        var s = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        s.Run(2);                                       // 旗は助走の後に立てる（助走の軌道が両方で同じ＝違いは旗の分岐だけ）
        s.World.Coef.LaborBandFloorOffGate = bandOff;
        var r = s.World.Region;
        foreach (int gi in Labor3)
        {
            var m = r.Markets[gi];
            m.Price = priceOverRef * Wrp(s, gi); m.ReservationWage = m.Price;
            m.LastDemand = 1.0; m.LastProduced = 100.0; m.LastSold = 1.0; m.Stock = 0;
        }
        return s;
    }

    [Fact]
    public void 帯の床の撤去は労働3財の下側だけを外す_ゲート0は旧の漸近形で床へ引き上げ_参照より上は同じ()
    {
        var off = OversuppliedLabor(1, 0.12); var on = OversuppliedLabor(0, 0.12);   // 参照の 0.12 倍＝帯の床（WageClampMin 0.3）の下から出発
        Assert.Equal(0.3, on.World.Coef.WageClampMin);
        MarketFlow.StepDay(off.World.Region, off.World.Coef, 10, null, true);
        MarketFlow.StepDay(on.World.Region, on.World.Coef, 10, null, true);
        foreach (int gi in Labor3)
        {
            double pOff = off.World.Region.Markets[gi].Price, pOn = on.World.Region.Markets[gi].Price;
            Assert.True(pOn > 0.25 * Wrp(on, gi), $"旧の世界で帯の床が効いていない gi={gi}（{pOn:F5}）");   // 床の下から出発しても旧は床の近くへ引き上げられる
            Assert.True(pOff < 0.12 * Wrp(off, gi), $"撤去した世界で値段が床の下に居ない gi={gi}（{pOff:F5}）");   // 過剰供給で下がり続ける（床で止まらない）
            Assert.True(pOff > 0, "値段は正のまま");
        }
        // 参照より上は同じ式（参照で連続）：参照の 1.2 倍から 1 日進める
        var up0 = OversuppliedLabor(0, 1.2); var up1 = OversuppliedLabor(1, 1.2);
        MarketFlow.StepDay(up0.World.Region, up0.World.Coef, 1, null, true);
        MarketFlow.StepDay(up1.World.Region, up1.World.Coef, 1, null, true);
        foreach (int gi in Labor3)
            Assert.Equal(up0.World.Region.Markets[gi].Price, up1.World.Region.Markets[gi].Price);
    }

    [Fact]
    public void 帯の床の撤去は需給が釣り合う帯の内側の値段を歪めない_ゲート0は漸近形が値段を動かす()
    {
        // 需給差 0（産出＝需要）なら目標＝今の値段。撤去した世界は値段が 1 ビットも動かない・旧の世界は漸近形が参照より下の値段を床の側へ寄せて動かす
        var off = OversuppliedLabor(1, 0.5); var on = OversuppliedLabor(0, 0.5);
        foreach (var s in new[] { off, on })
            foreach (int gi in Labor3) { var m = s.World.Region.Markets[gi]; m.LastProduced = 1.0; m.LastDemand = 1.0; m.LastSold = 1.0; m.LastImport = 0; }
        var before = Labor3.ToDictionary(gi => gi, gi => off.World.Region.Markets[gi].Price);
        MarketFlow.StepDay(off.World.Region, off.World.Coef, 10, null, true);
        MarketFlow.StepDay(on.World.Region, on.World.Coef, 10, null, true);
        foreach (int gi in Labor3)
        {
            Assert.Equal(before[gi], off.World.Region.Markets[gi].Price);
            double moved = System.Math.Abs(on.World.Region.Markets[gi].Price / before[gi] - 1.0);
            Assert.True(moved > 1e-4, $"旧の世界の漸近形が値段を動かしていない（前提が崩れた）gi={gi} moved={moved:E2}");
        }
    }

    [Fact]
    public void 帯の床の撤去は教育と医療の役務の枝に触らない()
    {
        var off = OversuppliedLabor(1, 0.12); var on = OversuppliedLabor(0, 0.12);
        foreach (var s in new[] { off, on })
        {
            var r = s.World.Region;
            foreach (int gi in new[] { Catalog.EducationId, Catalog.HealthcareId })
            {
                var m = r.Markets[gi];
                m.Price = 0.12 * Wrp(s, gi); m.LastDemand = 1.0; m.LastProduced = 100.0;
            }
        }
        MarketFlow.StepDay(off.World.Region, off.World.Coef, 10, null, true);
        MarketFlow.StepDay(on.World.Region, on.World.Coef, 10, null, true);
        foreach (int gi in new[] { Catalog.EducationId, Catalog.HealthcareId })
            Assert.Equal(on.World.Region.Markets[gi].Price, off.World.Region.Markets[gi].Price);
    }

    // ---------------------------------------------------------------- 最低賃金法：床

    [Fact]
    public void 法の段は4つ_なしは何にも効かず_生存以上は工場労働と農業労働に効き技師は効かない()
    {
        Assert.Equal(["None", "Subsistence", "Living", "Sufficiency"], System.Enum.GetNames<MinimumWageLaw>());
        Assert.False(MinimumWageFloor.Applies(MinimumWageLaw.None, Catalog.LaborId));
        Assert.False(MinimumWageFloor.Applies(MinimumWageLaw.None, Catalog.FarmLaborId));
        foreach (var law in new[] { MinimumWageLaw.Subsistence, MinimumWageLaw.Living, MinimumWageLaw.Sufficiency })
        {
            Assert.True(MinimumWageFloor.Applies(law, Catalog.LaborId));
            Assert.True(MinimumWageFloor.Applies(law, Catalog.FarmLaborId));
            Assert.False(MinimumWageFloor.Applies(law, Catalog.AdvLaborId));
        }
    }

    [Fact]
    public void 籠の中身は段で増える_生存は食料_生活は日用品も_充足は嗜好品も_役務と住居は入れない()
    {
        Assert.False(MinimumWageFloor.InBasket(MinimumWageLaw.None, GoodTier.Staple));
        Assert.True(MinimumWageFloor.InBasket(MinimumWageLaw.Subsistence, GoodTier.Staple));
        Assert.False(MinimumWageFloor.InBasket(MinimumWageLaw.Subsistence, GoodTier.Daily));
        Assert.True(MinimumWageFloor.InBasket(MinimumWageLaw.Living, GoodTier.Daily));
        Assert.False(MinimumWageFloor.InBasket(MinimumWageLaw.Living, GoodTier.Luxury));
        Assert.True(MinimumWageFloor.InBasket(MinimumWageLaw.Sufficiency, GoodTier.Luxury));
        foreach (var law in new[] { MinimumWageLaw.Subsistence, MinimumWageLaw.Living, MinimumWageLaw.Sufficiency })
            foreach (var tier in new[] { GoodTier.Service, GoodTier.Intermediate, GoodTier.Producer })
                Assert.False(MinimumWageFloor.InBasket(law, tier));
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(2);
        var r = sim.World.Region;
        double b0 = MinimumWageFloor.BasketPerPerson(r, MinimumWageLaw.None);
        double b1 = MinimumWageFloor.BasketPerPerson(r, MinimumWageLaw.Subsistence);
        double b2 = MinimumWageFloor.BasketPerPerson(r, MinimumWageLaw.Living);
        double b3 = MinimumWageFloor.BasketPerPerson(r, MinimumWageLaw.Sufficiency);
        Assert.Equal(0.0, b0);
        Assert.True(b1 > 0 && b2 > b1 && b3 > b2, $"籠が段で増えない：{b1:F4} {b2:F4} {b3:F4}");
    }

    [Fact]
    public void 床は籠と担い手の人数と能力から出て_売れた量と参照価格を読まない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(2);
        var r = sim.World.Region;
        // 籠（生活の段）＝主食と日用品の必要量×今の値段の和（テストの側で別に数え直す・需要の立たない財は入れない）
        const MinimumWageLaw law = MinimumWageLaw.Living;
        double basket = 0;
        foreach (var g in r.Goods) if ((g.Tier == GoodTier.Staple || g.Tier == GoodTier.Daily) && g.BaseNeed > 0 && r.Markets[g.Id].LastDemand > 0) basket += g.BaseNeed * r.Markets[g.Id].Price;
        Assert.True(basket > 0);
        Assert.Equal(basket, MinimumWageFloor.BasketPerPerson(r, law), 9);
        var human = r.Producers.Where(s => s.GoodId == Catalog.LaborId && s.RecipeName.Length == 0 && !s.Bankrupt).ToList();
        int lp = human[0].LaborPopIndex;
        double cap = human.Where(s => s.LaborPopIndex == lp).Sum(s => s.Capacity);
        double sold = r.Markets[Catalog.LaborId].LastSold;
        Assert.True(cap > 0 && sold > 0);
        // 能力ベース（1 つに決めた読み・(469) 事前登録 §1 [E]）：床＝籠×人数÷能力
        double before = MinimumWageFloor.PerUnit(r, Catalog.LaborId, law);
        Assert.Equal(basket * r.Pops[lp].Size / cap, before, 9);
        // 売れた量を動かしても床は動かない（正帰還が無い）
        r.Markets[Catalog.LaborId].LastSold = sold / 4.0;
        Assert.Equal(before, MinimumWageFloor.PerUnit(r, Catalog.LaborId, law));
        r.Markets[Catalog.LaborId].LastSold = 0;
        Assert.Equal(before, MinimumWageFloor.PerUnit(r, Catalog.LaborId, law));
        r.Markets[Catalog.LaborId].LastSold = sold;
        // 参照価格を動かしても床は動かない（値段が同じなら）
        foreach (var g in r.Goods) g.RefPrice *= 2.0;
        Assert.Equal(before, MinimumWageFloor.PerUnit(r, Catalog.LaborId, law));
        // 天井を超えない（帯の順序を保つ）
        Assert.True(MinimumWageFloor.AbsFloor(r, Catalog.LaborId, before * 0.5, law) <= before * 0.5);
        // 籠が段で増えれば床も増える・法 None は床なし（0）
        Assert.True(MinimumWageFloor.PerUnit(r, Catalog.LaborId, MinimumWageLaw.Sufficiency) > MinimumWageFloor.PerUnit(r, Catalog.LaborId, MinimumWageLaw.Subsistence));
        Assert.Equal(0.0, MinimumWageFloor.PerUnit(r, Catalog.LaborId, MinimumWageLaw.None));
    }

    [Fact]
    public void 日次の価格の歩は法の床を下から効かせ_Noneと対象外の財は旧と同じ()
    {
        // 能力ベースの床は今の値段の約 0.06 倍で縛らない（事前登録 §1 [E]）。縛る場面は〈帯の床を外して値段が床より下まで落ちた世界〉を直接作って確かめる
        Simulation Build()
        {
            var s = OversuppliedLabor(1, 1.0);
            s.World.Coef.MinimumWageLawGate = 1;
            return s;
        }
        var none = Build(); var trades = Build(); var national = Build();   // trades＝生存・national＝充足
        double floorS = MinimumWageFloor.AbsFloor(trades.World.Region, Catalog.LaborId, double.MaxValue, MinimumWageLaw.Subsistence);
        double floorF = MinimumWageFloor.AbsFloor(trades.World.Region, Catalog.LaborId, double.MaxValue, MinimumWageLaw.Sufficiency);
        Assert.True(floorS > 0 && floorF > floorS, $"前提が崩れた：床 生存 {floorS:F6}・充足 {floorF:F6}");
        // 値段を〈充足の床より十分に下〉から出発させる（テストの中で作る前提）
        foreach (var s in new[] { none, trades, national })
            foreach (int gi in new[] { Catalog.LaborId, Catalog.FarmLaborId })
            {
                var m = s.World.Region.Markets[gi];
                m.Price = floorS * 0.01; m.ReservationWage = m.Price;
            }
        MarketFlow.StepDay(none.World.Region, none.World.Coef, 10, null, true, MinimumWageLaw.None);
        MarketFlow.StepDay(trades.World.Region, trades.World.Coef, 10, null, true, MinimumWageLaw.Subsistence);
        MarketFlow.StepDay(national.World.Region, national.World.Coef, 10, null, true, MinimumWageLaw.Sufficiency);
        double pNone = none.World.Region.Markets[Catalog.LaborId].Price;
        double pTrades = trades.World.Region.Markets[Catalog.LaborId].Price;
        Assert.True(pNone < floorS, "前提が崩れた：床の無い世界で値段が床に届いた");
        var rr = trades.World.Region; var cc = trades.World.Coef;
        double ceiling = rr.GoodOf(Catalog.LaborId)!.RefPrice * rr.PriceLevel * (cc.WageClampMax * (1 + cc.WageClampIndScale * rr.Industrialization));   // 帯の天井（床はこれを超えて上げない）
        double want = System.Math.Min(floorS, ceiling);
        Assert.True(pTrades >= want * (1 - 1e-9) && pTrades > pNone, $"生存の床が工場労働の値段に効いていない（値段 {pTrades:F6}・床なし {pNone:F6}・床 {floorS:F6}・天井 {ceiling:F4}）");
        // 農業労働にも効く（段は籠の中身であって業種の範囲ではない）・技師には効かない・段が上がれば値段は下がらない
        double farmNone = none.World.Region.Markets[Catalog.FarmLaborId].Price;
        Assert.True(trades.World.Region.Markets[Catalog.FarmLaborId].Price > farmNone);
        Assert.True(national.World.Region.Markets[Catalog.FarmLaborId].Price >= trades.World.Region.Markets[Catalog.FarmLaborId].Price);
        Assert.True(national.World.Region.Markets[Catalog.LaborId].Price >= pTrades);
        Assert.Equal(none.World.Region.Markets[Catalog.AdvLaborId].Price, national.World.Region.Markets[Catalog.AdvLaborId].Price);
    }

    [Fact]
    public void 法がNoneなら旧と同じ_ゲートが開いても床は足さない()
    {
        var none = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var gateOnly = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        gateOnly.World.Coef.MinimumWageLawGate = 1;
        none.Run(4); gateOnly.Run(4);
        Assert.Equal(LaborPrice(none), LaborPrice(gateOnly));
        Assert.Equal(none.World.Region.Markets[Catalog.FarmLaborId].Price, gateOnly.World.Region.Markets[Catalog.FarmLaborId].Price);
    }

    // ---------------------------------------------------------------- 最低賃金法：軸の配線（漏れを赤に）

    [Fact]
    public void ゲート0では法の軸は全ての入口で非活性である()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        Assert.Equal(0.0, w.Coef.MinimumWageLawGate);
        var e = PolicyPreferences.Build(w);
        for (int b = 0; b < PolicyPreferences.BlocCount; b++) Assert.Equal(0.0, PolicyPreferences.Pref(PolicyAxis.MinimumWage, b, e, w));
        // 段の法の表の門（施行と戦線の両方）が閉じている（前提が無い軸では GateInfo は純支持 0 で通るため、門は表の Open が持つ）
        var row = Government.LevelLaws.Single(l => l.Axis == PolicyAxis.MinimumWage);
        Assert.False(row.Open!(w));
        w.Coef.MinimumWageLawGate = 1;
        Assert.True(row.Open!(w));
        w.Coef.MinimumWageLawGate = 0;
        Assert.Null(UtilityAgent.MinimumWageTargetFromBoard(w));
        // 方針の欄は既定（None）のまま・議会の綱領は載せない
        Assert.Equal(MinimumWageLaw.None, w.Policy.MinimumWage);
        for (int bloc = 0; bloc < PolicyPreferences.BlocCount; bloc++)
            Assert.DoesNotContain(FactionSnapshot.ComputeDemands(w, bloc), d => d.Axis == PolicyAxis.MinimumWage);
    }

    [Fact]
    public void ゲートが開けば大衆が賛成し資本が反対し_他の法の前提を持たず段は1つずつ()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.MinimumWageLawGate = 1;
        var e = PolicyPreferences.Build(w);
        Assert.True(PolicyPreferences.Pref(PolicyAxis.MinimumWage, 2, e, w) > 0);   // 大衆
        Assert.True(PolicyPreferences.Pref(PolicyAxis.MinimumWage, 1, e, w) < 0);   // 近代エリート（雇う側）
        Assert.True(PolicyPreferences.Pref(PolicyAxis.MinimumWage, 0, e, w) < 0);   // 旧支配層
        // 組合の法とは独立の軸（作者 2026-10-20）：労働組合が違法でも・官僚制度が猟官でも、最初の段へ登れる（他の法の前提を持たない）
        w.LaborUnion = LaborUnionLaw.Illegal;
        w.Bureaucracy = BureaucracyLaw.Spoils;
        Assert.True(Government.LawPrereq(w, PolicyAxis.MinimumWage, 1, w.Coef).Ok);
        // 段の飛び越し禁止（労働時間と同じ）：今は None＝2 以上は飛ばし
        Assert.False(Government.LawPrereq(w, PolicyAxis.MinimumWage, 2, w.Coef).Ok);
        Assert.False(Government.LawPrereq(w, PolicyAxis.MinimumWage, 3, w.Coef).Ok);
        w.MinimumWage = MinimumWageLaw.Subsistence;
        Assert.True(Government.LawPrereq(w, PolicyAxis.MinimumWage, 2, w.Coef).Ok);
        Assert.False(Government.LawPrereq(w, PolicyAxis.MinimumWage, 3, w.Coef).Ok);
        w.MinimumWage = MinimumWageLaw.Living;
        Assert.True(Government.LawPrereq(w, PolicyAxis.MinimumWage, 3, w.Coef).Ok);
    }

    [Fact]
    public void AIの目標は都市の割合の線に従い下げる目標は書かない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World;
        w.Coef.MinimumWageLawGate = 1;
        double u = UtilityAgent.HomeUrbanShare(w);
        var want = u > 2.0 / 3.0 ? MinimumWageLaw.Sufficiency : u > 0.5 ? MinimumWageLaw.Living : u > 1.0 / 3.0 ? MinimumWageLaw.Subsistence : (MinimumWageLaw?)null;   // 別の出所（リテラル）で
        Assert.Equal(want, UtilityAgent.MinimumWageTargetFromBoard(w));
        w.MinimumWage = MinimumWageLaw.Sufficiency;   // 施行済みの床を引き下げる目標は立てない
        Assert.Null(UtilityAgent.MinimumWageTargetFromBoard(w));
        w.MinimumWage = MinimumWageLaw.Subsistence;   // 現行が目標以上なら書かない・未満なら目標は現行より上
        var t = UtilityAgent.MinimumWageTargetFromBoard(w);
        Assert.True(t is null || t > MinimumWageLaw.Subsistence);
    }

    [Fact]
    public void AIの段の置き方は都市の割合の線で4段に分かれる()
    {
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageWant(0.0));
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageWant(1.0 / 3.0));
        Assert.Equal(MinimumWageLaw.Subsistence, UtilityAgent.MinimumWageWant(0.40));
        Assert.Equal(MinimumWageLaw.Living, UtilityAgent.MinimumWageWant(0.55));
        Assert.Equal(MinimumWageLaw.Sufficiency, UtilityAgent.MinimumWageWant(0.70));
        Assert.Equal(MinimumWageLaw.None, UtilityAgent.MinimumWageWant(double.NaN));
    }

    [Fact]
    public void 軸は方針の複写_検証_文言_段の表_議会_要求_PlayHarness_すべてに載る()
    {
        // 方針の複写（Clone/CopyFrom の運搬漏れ）
        var p = new NationPolicy { MinimumWage = MinimumWageLaw.Sufficiency };
        var q = new NationPolicy();
        q.CopyFrom(p);
        Assert.Equal(MinimumWageLaw.Sufficiency, q.MinimumWage);
        // 検証：値域外を拾う
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        Assert.Null(Validation.LawOutOfRange(sim.World));
        sim.World.Policy.MinimumWage = (MinimumWageLaw)99;
        Assert.Equal("最低賃金法", Validation.LawOutOfRange(sim.World));
        sim.World.Policy.MinimumWage = MinimumWageLaw.None;
        // 文言（英字の列挙名が UI に出ない）
        Assert.NotEqual(PolicyAxis.MinimumWage.ToString(), FactionSnapshot.DemandJa(PolicyAxis.MinimumWage, true));
        Assert.NotEqual(PolicyAxis.MinimumWage.ToString(), FactionSnapshot.DemandJa(PolicyAxis.MinimumWage, false));
        // 段の法の表に1行（件数は列挙の側から数える）
        Assert.Single(Government.LevelLaws, l => l.Axis == PolicyAxis.MinimumWage);
        Assert.Contains(PolicyAxis.MinimumWage, FactionSnapshot.Axes);
        // 列挙の末尾（既存の序数を動かさない）
        var axes = System.Enum.GetValues<PolicyAxis>();
        Assert.Equal(PolicyAxis.MinimumWage, axes[^1]);
        // 方針の欄を見る PlayHarness の表（足し漏れの番人が別にあるが、ここでも名前で引く）
        Assert.Contains(PriceOfBread.Runner.PlayHarness.LawStatusRows(sim.World), r => r.Field == nameof(NationPolicy.MinimumWage));
    }

    [Fact]
    public void 軸を足した世界はセーブの往復でバイト一致する()
    {
        var sim = new MultiNationSim([OpeningFixture.Named("イギリス"), OpeningFixture.Named("フランス")], 1789, _ => new NullAgent());
        var w = sim.Nations[0].World;
        w.Coef.MinimumWageLawGate = 1;
        w.MinimumWage = MinimumWageLaw.Living; w.Policy.MinimumWage = MinimumWageLaw.Sufficiency;
        sim.StartWarmup();
        sim.StepQuarter();
        using var ms1 = new MemoryStream();
        SaveGame.Save(sim, ms1);
        ms1.Position = 0;
        var sim2 = SaveGame.Load(ms1);
        Assert.Equal(sim.Nations[0].World.MinimumWage, sim2.Nations[0].World.MinimumWage);
        Assert.Equal(sim.Nations[0].World.Policy.MinimumWage, sim2.Nations[0].World.Policy.MinimumWage);
        using var ms2 = new MemoryStream();
        SaveGame.Save(sim2, ms2);
        Assert.Equal(ms1.ToArray(), ms2.ToArray());
    }
}

/// <summary>★(459) 労働の値段の表 <c>labor_s</c>（読むだけ）の番人。行が出る・列の数が合う・賃金の支払いが世界の市場から別に数え直した値と合う・
/// 法が None の既定で lawCode が 0・床÷値段の 2 つの読みが分母の比で並ぶ（売れた量が能力以下なら実雇用ベースが能力ベースを下回らない）。
/// 軌道が計器で変わらないことは既存の全ビット一致の番人（InstrumentsTests）が同じ hook を通って担う。非空虚の対照＝能力と賃金の支払いが正の行がある。</summary>
[Collection("Instruments")]
public class LaborInstrumentTests
{
    private static double Col(string[] h, string[] r, string name)
    {
        int i = System.Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    [Fact]
    public void 労働の値段の表は賃金の支払いを世界の市場から数え直した値と合わせ_既定の法は0()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lw_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            const int Q = 12;
            double expectBill = 0, expectFarmBill = 0; string lastNation = "";
            try
            {
                for (int q = 0; q < Q; q++)
                {
                    sim.StepQuarter();
                    if (q == Q - 1)
                    {   // 最後の四半期の行と同じ瞬間（Quarter を呼ぶ直前）の市場から、フランスの賃金の支払いを別に数え直す
                        var w = sim.Nations[0].World; lastNation = Instruments.NameOf(w);
                        foreach (var r in w.Regions)
                        {
                            if (r.State != RegionState.Active) continue;
                            bool hasP = r.Producers.Any(s => s.GoodId == Catalog.LaborId && s.RecipeName.Length == 0 && s.Inputs.Count == 0 && !s.Bankrupt);
                            bool hasF = r.Producers.Any(s => s.GoodId == Catalog.FarmLaborId && s.RecipeName.Length == 0 && s.Inputs.Count == 0 && !s.Bankrupt);
                            if (hasP) expectBill += r.Markets[Catalog.LaborId].LastSold * r.Markets[Catalog.LaborId].Price;
                            if (hasF) expectFarmBill += r.Markets[Catalog.FarmLaborId].LastSold * r.Markets[Catalog.FarmLaborId].Price;
                        }
                    }
                    Instruments.Quarter(sim, 1789UL, q);
                }
            }
            finally { Instruments.Close(sim); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "labor_s1789.tsv"));
            var h = lines[0].Split('\t');
            var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            Assert.Equal(Instruments.LwHeaderText.Split('\t'), h);
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            Assert.True(rows.Count >= 4 * Q - 4, $"行が少ない {rows.Count}");
            int qi = System.Array.IndexOf(h, "quarter"), ni = System.Array.IndexOf(h, "nation");
            var last = rows.Single(r => r[qi] == (Q - 1).ToString() && r[ni] == lastNation);
            Assert.True(expectBill > 0 && expectFarmBill > 0, "前提が崩れた：賃金の支払いが 0");
            // 表の値は G10（有効数字 10 桁）で書かれる＝絶対の桁数でなく相対 1e-8 で合わせる
            Assert.InRange(Col(h, last, "plainWageBillPerMonth"), expectBill * (1 - 1e-8), expectBill * (1 + 1e-8));
            Assert.InRange(Col(h, last, "farmWageBillPerMonth"), expectFarmBill * (1 - 1e-8), expectFarmBill * (1 + 1e-8));
            int pos = 0;
            foreach (var r in rows)
            {
                Assert.Equal(0.0, Col(h, r, "lawCode"));                                  // 既定（法のゲート 0）では法は None のまま
                Assert.Equal(0.0, Col(h, r, "enactedBindShare"));                         // 法が None なら縛る州は 0
                double cap = Col(h, r, "plainCapPerMonth"), sold = Col(h, r, "plainSoldPerMonth");
                if (!(cap > 0)) continue;
                pos++;
                Assert.True(Col(h, r, "plainPriceOverMcIndex") > 0 && Col(h, r, "plainProfitMeanShare") is >= 0 and <= 1);
                // 売れた量が能力以下なら、実雇用ベースの床（分母＝売れた量）は能力ベースの床（分母＝能力）を下回らない（段ごと）
                if (sold > 0 && sold <= cap)
                    foreach (var tier in new[] { "Subs", "Liv", "Suff" })
                        Assert.True(Col(h, r, $"plainFloor1Over{tier}Index") >= Col(h, r, $"plainFloor0Over{tier}Index") * (1 - 1e-6) - 1e-12);
                // 段が上がれば籠は増える（床÷値段は単調）
                Assert.True(Col(h, r, "plainFloor0OverSubsIndex") <= Col(h, r, "plainFloor0OverLivIndex") + 1e-12 && Col(h, r, "plainFloor0OverLivIndex") <= Col(h, r, "plainFloor0OverSuffIndex") + 1e-12);
            }
            Assert.True(pos > 4 * Q / 2, $"能力が正の行が少ない {pos}（hook が空振り）");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>法が施行された世界で、縛りの列（N1・N2）は〈値段÷機構と同じ天井で削った床〉の閾値（1.05・0.90）で決まり、州の粒度の列は〈値段÷物価水準〉の閾値（1e-3）で決まる。
    /// 期待値は表の外で作る：床は MinimumWageFloor.AbsFloor を世界から直に呼び（天井は参照価格×物価水準×天井の倍率を別に組む）、値段を床の倍率に置いてから表を書かせ、分母（能力のある州）を数え直す。</summary>
    [Fact]
    public void 縛りの列は値段と床の比の閾値で決まり_州の粒度の列は実質の値段の閾値で決まる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_lwb_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent());
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            var w = sim.Nations[0].World;
            string nat = Instruments.NameOf(w);
            try
            {
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 0);
                sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, 1);
                w.MinimumWage = MinimumWageLaw.Living;
                static double HumanCap(Region r) => r.Producers.Where(s => s.GoodId == Catalog.LaborId && EconomyStep.IsHumanLaborSupply(s) && !s.Bankrupt).Sum(s => s.Capacity);
                // 能力のある州の分母と、床が立つ州の分子（表の外で数える）
                var list = new List<(Region r, double floor)>();
                double den = 0, num = 0;
                foreach (var r in w.Regions)
                {
                    if (r.State != RegionState.Active) continue;
                    var human = r.Producers.FirstOrDefault(s => s.GoodId == Catalog.LaborId && EconomyStep.IsHumanLaborSupply(s) && !s.Bankrupt);
                    double cap = HumanCap(r);
                    if (human is null || !(cap > 0) || human.LaborPopIndex < 0 || human.LaborPopIndex >= r.Pops.Count || !(r.Markets[Catalog.LaborId].Price > 0)) continue;
                    double ceiling = r.GoodOf(Catalog.LaborId)!.RefPrice * r.PriceLevel * (w.Coef.WageClampMax * (1 + w.Coef.WageClampIndScale * r.Industrialization));
                    double f = MinimumWageFloor.AbsFloor(r, Catalog.LaborId, ceiling, w.MinimumWage);
                    den += cap; if (f > 0) num += cap;
                    list.Add((r, f));
                }
                Assert.True(den > 0 && num > 0, $"前提が崩れた：床の立つ州が無い（分母 {den}・分子 {num}）");
                // 表は閉じるまで書き手が掴んでいる＝四半期ごとに値段を置いて表を書かせ、期待値を控え、閉じてから読む
                var checks = new List<(int q, string col, double exp, double tol)>();
                // (四半期, 値段÷床, 縛り〔1.05 以下〕の割合の倍率, 床の下〔0.90 未満〕の倍率)
                foreach (var (q, scale, near, below) in new[] { (2, 0.95, 1.0, 0.0), (3, 0.5, 1.0, 1.0), (4, 1.5, 0.0, 0.0), (5, 1.04, 1.0, 0.0), (6, 1.06, 0.0, 0.0), (7, 0.89, 1.0, 1.0) })
                {
                    foreach (var (r, f) in list) if (f > 0) r.Markets[Catalog.LaborId].Price = f * scale;
                    Instruments.Quarter(sim, 1789UL, q);
                    checks.Add((q, "enactedNearFloorShare", num / den * near, 1e-6));
                    checks.Add((q, "enactedBelowFloorShare", num / den * below, 1e-6));
                }
                // 州の粒度の列：値段を物価水準の 0.5e-3 倍／2e-3 倍に置く
                foreach (var (q, real, tiny) in new[] { (8, 0.5e-3, 1.0), (9, 2e-3, 0.0) })
                {
                    foreach (var r in w.Regions) if (r.State == RegionState.Active) r.Markets[Catalog.LaborId].Price = real * r.PriceLevel;
                    Instruments.Quarter(sim, 1789UL, q);
                    checks.Add((q, "plainTinyPriceRegionShare", tiny, 1e-9));
                    checks.Add((q, "plainRealPriceMinIndex", real, real * 1e-8));
                }
                Instruments.Close(sim);
                var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "labor_s1789.tsv"));
                var h = lines[0].Split('\t');
                var all = lines.Skip(1).Select(l => l.Split('\t')).ToList();
                foreach (var (q, col, exp, tol) in checks)
                {
                    var row = all.Last(r => r[System.Array.IndexOf(h, "quarter")] == q.ToString() && r[System.Array.IndexOf(h, "nation")] == nat);
                    Assert.InRange(Col(h, row, col), exp - tol, exp + tol);
                }
            }
            finally { Instruments.Close(sim); }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>人力の述語の一本化：投入を持つ人力の行（IsHumanLaborSupply でない）は床の分母（能力の和）に数えない。</summary>
    [Fact]
    public void 床の分母は供給の式と同じ述語で数え_投入を持つ人力の行を数えない()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(2);
        var r = sim.World.Region;
        const MinimumWageLaw law = MinimumWageLaw.Living;
        double before = MinimumWageFloor.PerUnit(r, Catalog.LaborId, law);
        Assert.True(before > 0);
        var human = r.Producers.First(s => s.GoodId == Catalog.LaborId && EconomyStep.IsHumanLaborSupply(s) && !s.Bankrupt);
        // 同じ POP に載る、投入を持つ人力の行（レシピ名は空）を足す＝旧の述語（レシピ名が空）なら分母に入って床が下がる
        var extra = new Producer { GoodId = Catalog.LaborId, RecipeName = "", LaborPopIndex = human.LaborPopIndex, Capacity = human.Capacity * 3.0 };
        extra.Inputs.Add(new Input(Catalog.LaborId, 0.1));
        Assert.False(EconomyStep.IsHumanLaborSupply(extra));
        r.Producers.Add(extra);
        Assert.Equal(before, MinimumWageFloor.PerUnit(r, Catalog.LaborId, law));
    }

    /// <summary>天井の倍率の1本：StepDay と計器が読む EffWageMax は、係数と工業化度から別に組んだ式と等しい。</summary>
    [Fact]
    public void 賃金の天井の倍率は係数と工業化度から組んだ式と等しい()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        foreach (double ind in new[] { 0.0, 0.3, 1.7 })
        {
            r.Industrialization = ind;
            Assert.Equal(c.WageClampMax * (1 + c.WageClampIndScale * ind), MarketFlow.EffWageMax(r, c));
        }
    }
}
