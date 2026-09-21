using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 運用コスト監査2026-08-16（燃料系4本＝海軍運用燃料・鉄道運行石炭・汽船運行燃料・空軍/輜重燃料）。
/// 全て休眠（既定0）＝既定では新状態（EMA・台帳Navy）が1ビットも動かないことのガードと、係数を直接非0にしたON経路の検証。
/// </summary>
public class FuelOperatingCostTests
{
    [Fact]
    public void 海軍と汽船の燃料は休眠_鉄道空輜重は点火済み()
    {
        // ★2026-08-29 夜間R18で RailCoalRate/AirFuelRate/MotorFuelRate を点火（帯中立・破綻0）。
        //   NavalFuelPerGauge と ShipFuelRate は実測逆算（fidelity/tradeprobe）が要るので休眠のまま＝この2本の番人は残す。
        var c = new Coefficients();
        Assert.Equal(0.0, c.NavalFuelPerGauge);
        Assert.Equal(0.22, c.RailCoalRate);
        Assert.Equal(0.0, c.ShipFuelRate);
        Assert.Equal(0.045, c.AirFuelRate);
        Assert.Equal(0.05, c.MotorFuelRate);

        var sim = new Simulation(new Scenario { Seed = 11, Kind = ScenarioKind.France }, new NullAgent());
        sim.Run(120);
        Assert.Equal(1.0, sim.World.NavalFuelFillEma);                              // 書き手（NavalFuelStep）が一度も走っていない
        Assert.Equal(0.0, sim.World.ShipSteamShareEma);                             // EMA更新が ShipFuelRate ゲート内に居る証拠（点火3本では動かない）
        Assert.Equal(0.0, sim.World.TreasuryLedger[(int)TreasuryCat.Navy]);         // Navy枠の書き手は海軍燃料のみ＝休眠でゼロ
    }

    [Fact]
    public void 海軍燃料_係数0では直接呼んでも何も変わらない()
    {
        var coef = new Coefficients();
        var sim = new Simulation(new Scenario { Seed = 13, Kind = ScenarioKind.France }, new NullAgent());
        sim.Run(8);
        var w = sim.World;
        w.SelfPower = new Power { FleetGauge = 100.0 };
        w.SetMilitaryUpkeep(0.2, 0.4);
        var m = w.Region.Markets[Catalog.CoalId];
        double treasury0 = w.Treasury, stock0 = m.Stock, demand0 = m.LastDemand;

        Military.NavalFuelStep(w, coef);

        Assert.Equal(treasury0, w.Treasury);
        Assert.Equal(stock0, m.Stock);
        Assert.Equal(demand0, m.LastDemand);
        Assert.Equal(1.0, w.NavalFuelFillEma);
    }

    [Fact]
    public void 海軍燃料ON_艦隊が石炭を焚き国庫Navyに記帳される()
    {
        var coef = new Coefficients { NavalFuelPerGauge = 5.0, SteamTechOnset = -1.0 };   // 蒸気ゲート全開（序盤の技術でも焚ける＝テスト専用）
        var sim = new Simulation(new Scenario { Seed = 12, Kind = ScenarioKind.France, Coef = coef }, new NullAgent());
        sim.Run(8);
        var w = sim.World;
        w.SelfPower = new Power { FleetGauge = 100.0 };   // 艦隊を注入（単一国世界はSelfPowerなし＝燃料需要ゼロのため）
        w.SetMilitaryUpkeep(0.2, 0.4);                    // NavyUpkeep=0.4＝哨戒稼働
        var m = w.Region.Markets[Catalog.CoalId];
        m.Stock = 1_000_000; m.Price = Catalog.RefPrices[Catalog.CoalId];   // 価格ゲートを中立化（相場＝参照価格）
        w.Pay(TreasuryCat.OtherIn, 1_000_000);
        double treasury0 = w.Treasury, stock0 = m.Stock, demand0 = m.LastDemand;
        double navy0 = w.TreasuryLedger[(int)TreasuryCat.Navy];

        Military.NavalFuelStep(w, coef);   // 需要 = 5.0×100×0.4×蒸気1.0×Era1.0 = 200石炭

        Assert.True(m.LastDemand > demand0, "石炭需要が立っていない");
        Assert.True(m.Stock < stock0, "石炭在庫が減っていない（焚いていない）");
        Assert.True(w.Treasury < treasury0, "国庫が燃料代を払っていない");
        Assert.True(w.TreasuryLedger[(int)TreasuryCat.Navy] < navy0, "TreasuryCat.Navy に記帳されていない");

        // 燃料が払底すると充足EMAが下がる（＝出撃能力の入力が動く）
        m.Stock = 1.0;
        Military.NavalFuelStep(w, coef);
        Assert.True(w.NavalFuelFillEma < 1.0, $"燃料不足なのに充足EMAが下がらない（{w.NavalFuelFillEma}）");
    }

    [Fact]
    public void 海軍燃料_出撃能力は艦隊項に緩く噛む()
    {
        var p = new Power { FleetGauge = 100.0, Military = 1000.0, NavalShare = 0.5, _fleetDriven = true };
        Assert.Equal(100.0, p.NavalPower);   // 既定 NavalFuelMul=1.0＝×1.0のIEEE恒等（従来と同値）

        p.NavalFuelMul = 0.5;
        Assert.Equal(50.0, p.NavalPower);    // 艦隊律速側＝燃料半減で艦隊項が半分

        p.FleetGauge = 2000.0; p.NavalFuelMul = 0.9;
        Assert.Equal(500.0, p.NavalPower);   // 乗員律速側＝min(1800, 500)＝燃料は効かない（港湾には艦が余っている）
    }

    [Fact]
    public void 鉄道石炭ON_保守が石炭需要を立てる()
    {
        Simulation Grow(ulong seed) { var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent()); s.Run(8); return s; }
        var off = Grow(21);
        var on = Grow(21);   // 同一シード＝直接呼び出しの直前まで両世界はビット一致
        foreach (var s in new[] { off, on })
        {
            var r = s.World.Region;
            r.CapitalEnabled = true;
            r.Industrialization = 5.0;                             // 保守対象のインフラ
            r.Pops[r.InvestorPopIndex].Savings = 10_000_000;       // 投資予算を潤沢に
        }
        var coefOff = new Coefficients { SteamTechOnset = -1.0 };
        var coefOn = new Coefficients { SteamTechOnset = -1.0, RailCoalRate = 10.0 };

        EconomyStep.Run(off.World.Region, coefOff, 1.0, off.World.Bank, off.World);
        EconomyStep.Run(on.World.Region, coefOn, 1.0, on.World.Bank, on.World);

        double dOff = off.World.Region.Markets[Catalog.CoalId].LastDemand;
        double dOn = on.World.Region.Markets[Catalog.CoalId].LastDemand;
        Assert.True(dOn > dOff + 1.0, $"運行石炭の需要が立っていない（ON {dOn:F2} vs OFF {dOff:F2}）");
    }

    [Fact]
    public void 汽船燃料ON_商人が石炭を買い地域の金は保存される()
    {
        var coef = new Coefficients { ShipFuelRate = 1.0 };
        var sim = new Simulation(new Scenario { Seed = 31, Kind = ScenarioKind.France, Coef = coef }, new NullAgent());
        sim.Run(8);
        var w = sim.World;
        w.ShipSteamShareEma = 0.5;    // 汽船が普及した状態を注入（EMAの育ちは購入比率から＝別経路）
        w.ShipCapacity = 1000.0;
        var r = w.Region;
        int buyerIdx = r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex;
        Assert.True(buyerIdx >= 0, "買い手（商人/投資家）が居ない世界ではテストにならない");
        var m = r.Markets[Catalog.CoalId];
        m.Stock = 1_000_000; m.Price = Catalog.RefPrices[Catalog.CoalId];
        r.Markets[Catalog.CrudeOilId].Price = 1_000.0;   // 二択を石炭側へ固定（重油の実効単価を吊り上げる。二択の式自体は既存Colonial B6と同一）
        r.Pops[buyerIdx].Savings = 1_000_000;
        double money0 = r.SumSavings();
        double stock0 = m.Stock, demand0 = m.LastDemand;

        Transport.Step(w, coef);

        Assert.True(m.LastDemand > demand0, "運行燃料の石炭需要が立っていない");
        Assert.True(m.Stock < stock0, "石炭在庫が減っていない（焚いていない）");
        Assert.Equal(money0, r.SumSavings(), 6);   // 商人→生産者所有者のpop間移転＝地域の総貯蓄は不変（保存）
    }

    [Fact]
    public void 空軍燃料ON_原油需要が立ち国家が支払う()
    {
        Simulation Mil(ulong seed) { var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent()); s.Run(8); return s; }
        var off = Mil(41);
        var on = Mil(41);
        foreach (var s in new[] { off, on })
        {
            var r = s.World.Region;
            r.MilitaryScale = 100.0; r.MilitaryScalePeace = 100.0;             // 空軍段まで届く軍needsを注入
            r.MilitaryBudget = 10_000_000; r.MilitaryBudgetCeiling = 10_000_000;
            var oil = r.Markets[Catalog.CrudeOilId];
            oil.Stock = 1_000_000; oil.Price = Catalog.RefPrices[Catalog.CrudeOilId];   // 供給実在ガードは今月のproduced[]（実産出）が満たす
            // ★2026-08-23（配分＝付加価値×売れる量・均等床撤去）：需要0の原油RGOには労働が行かない＝「原油が産出されている」前提を明示する（旧は床0.15/Nが暗黙に産出させていた）
            foreach (var pr in r.Producers) if (pr.GoodId == Catalog.CrudeOilId) { pr.LaborWeight = 1e6; pr.Scale = 1.0; }
        }
        var coefOff = new Coefficients();
        var coefOn = new Coefficients { AirFuelRate = 0.5 };

        EconomyStep.Run(off.World.Region, coefOff, 1.0, off.World.Bank, off.World);
        EconomyStep.Run(on.World.Region, coefOn, 1.0, on.World.Bank, on.World);

        double dOff = off.World.Region.Markets[Catalog.CrudeOilId].LastDemand;
        double dOn = on.World.Region.Markets[Catalog.CrudeOilId].LastDemand;
        Assert.True(dOn > dOff + 1.0, $"空軍燃料の原油需要が立っていない（ON {dOn:F2} vs OFF {dOff:F2}）");
        Assert.True(on.World.Region.StateSpent > off.World.Region.StateSpent, "燃料代が国家支出（StateSpent）に載っていない");
    }

    [Fact]
    public void 輜重燃料ON_原油需要が立つ()
    {
        Simulation Mil(ulong seed) { var s = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France }, new NullAgent()); s.Run(8); return s; }
        var off = Mil(43);
        var on = Mil(43);
        foreach (var s in new[] { off, on })
        {
            var r = s.World.Region;
            r.MilitaryScale = 100.0; r.MilitaryScalePeace = 100.0;
            r.MilitaryBudget = 10_000_000; r.MilitaryBudgetCeiling = 10_000_000;
            var oil = r.Markets[Catalog.CrudeOilId];
            oil.Stock = 1_000_000; oil.Price = Catalog.RefPrices[Catalog.CrudeOilId];
            foreach (var pr in r.Producers) if (pr.GoodId == Catalog.CrudeOilId) { pr.LaborWeight = 1e6; pr.Scale = 1.0; }   // 同上＝原油が産出されている前提
        }
        var coefOff = new Coefficients();
        var coefOn = new Coefficients { MotorFuelRate = 0.3 };

        EconomyStep.Run(off.World.Region, coefOff, 1.0, off.World.Bank, off.World);
        EconomyStep.Run(on.World.Region, coefOn, 1.0, on.World.Bank, on.World);

        double dOff = off.World.Region.Markets[Catalog.CrudeOilId].LastDemand;
        double dOn = on.World.Region.Markets[Catalog.CrudeOilId].LastDemand;
        Assert.True(dOn > dOff + 1.0, $"輜重燃料の原油需要が立っていない（ON {dOn:F2} vs OFF {dOff:F2}）");
    }
}
