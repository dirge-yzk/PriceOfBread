using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 運用コスト監査2026-08-16（燃料系4本＝海軍運用燃料・鉄道運行石炭・汽船運行燃料・空軍/輜重燃料）。
/// 海軍燃料だけ休眠（既定0）＝既定では台帳Navy が1ビットも動かないことのガードと、係数を直接非0にしたON経路の検証。
/// ★2026-09-27（回4-3 の再挑戦）：汽船の船の選択（容量あたりの総費用）と、機関の時代係数で上がる汽船の性能の番人。
/// </summary>
public class FuelOperatingCostTests
{
    [Fact]
    public void 海軍の燃料は休眠_鉄道空輜重と汽船は点火済み()
    {
        // ★2026-08-29 夜間R18で RailCoalRate/AirFuelRate/MotorFuelRate を点火（帯中立・破綻0）。
        //   NavalFuelPerGauge は実測逆算（fidelity/tradeprobe）が要るので休眠のまま＝この番人は残す。
        // ★2026-09-27（回4-3 の再挑戦）：ShipFuelRate を点火（導出は Coefficients の注記＝汽船1隻の費用の構成）。
        //   汽船の割合（ShipSteamShareEma）は燃料の門の外で常に更新する形（2026-09-26）＝「割合0」の主張は外した（割合の正しさは下の番人）。
        var c = new Coefficients();
        Assert.Equal(0.0, c.NavalFuelPerGauge);
        Assert.Equal(0.22, c.RailCoalRate);
        Assert.Equal(0.0016, c.ShipFuelRate);
        Assert.Equal(0.045, c.AirFuelRate);
        Assert.Equal(0.05, c.MotorFuelRate);

        var sim = new Simulation(new Scenario { Seed = 11, Kind = ScenarioKind.France }, new NullAgent());
        sim.Run(120);
        Assert.Equal(1.0, sim.World.NavalFuelFillEma);                              // 書き手（NavalFuelStep）が一度も走っていない
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

    // ★2026-09-26（回4-3・項目8）：船は州ごとに容量あたりの総費用が安い1財だけを欲しがる。
    //   全ての活動州で帆船(84)と汽船(85)の値段・在庫・生産と、燃料（石炭＝参照価格・重油は高値で二択から外す）をそろえ、
    //   商人と投資家の財布を潤沢にして Transport.Step を直接呼ぶ。frontierTech＝前線技術（汽船の機関の時代係数 era の時計）。
    //   regionTech＝州の技術（汽船の解禁度＝製法の技術ゲート＝(技術−0.5)÷1 を 0..1 で読む。既定 1.5＝解禁度1）。
    static Simulation ShipWorld(ulong seed, Coefficients coef, double sailPrice, double steamPrice, bool steamSupplied, double frontierTech = 0.0, double coalPrice = -1, double regionTech = 1.5)
    {
        var sim = new Simulation(new Scenario { Seed = seed, Kind = ScenarioKind.France, Coef = coef }, new NullAgent());
        sim.Run(8);
        var w = sim.World;
        w.ShipCapacity = 0;   // 目標との差を最大に＝必ず買いに行く
        w.WorldTechLevel = frontierTech;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            r.TechLevel = regionTech;
            var sail = r.Markets[84]; var steam = r.Markets[85];
            sail.Price = sailPrice; sail.Stock = 1_000_000;
            steam.Price = steamPrice; steam.Stock = steamSupplied ? 1_000_000 : 0;
            steam.LastProduced = 0; steam.LastImport = 0;   // 供給の実在は在庫だけで決める
            foreach (var pr in r.Producers) if (pr.GoodId == 84 || pr.GoodId == 85) pr.Output = steamSupplied || pr.GoodId == 84 ? 1.0 : 0.0;
            var coal = r.Markets[Catalog.CoalId];
            coal.Price = coalPrice >= 0 ? coalPrice : Catalog.RefPrices[Catalog.CoalId]; coal.Stock = 1_000_000;
            r.Markets[Catalog.CrudeOilId].Price = 1_000.0;   // 燃料の二択を石炭へ固定（二択の式自体は Colonial.ChooseFuelGood のまま）
            if (r.MerchantPopIndex >= 0) r.Pops[r.MerchantPopIndex].Savings = 1e9;
            if (r.InvestorPopIndex >= 0) r.Pops[r.InvestorPopIndex].Savings = 1e9;
        }
        return sim;
    }

    static (double Sail, double Steam) ShipDemand(World w)
    {
        double s = 0, t = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { s += r.Markets[84].LastDemand; t += r.Markets[85].LastDemand; }
        return (s, t);
    }

    [Theory]
    [InlineData(10.0, 1.0, false)]    // 汽船の容量単価 1/75＋燃料 < 帆船 10/30 ＝汽船だけ
    [InlineData(1.0, 100.0, true)]    // 帆船 1/30 < 汽船 100/75 ＝帆船だけ
    public void 船の選択_容量あたりの総費用が安い方にだけ需要が立つ(double sailPrice, double steamPrice, bool sailWins)
    {
        var coef = new Coefficients();
        var sim = ShipWorld(51, coef, sailPrice, steamPrice, steamSupplied: true);
        var (s0, t0) = ShipDemand(sim.World);
        Transport.Step(sim.World, coef);
        var (s1, t1) = ShipDemand(sim.World);
        if (sailWins) { Assert.True(s1 > s0, "帆船の需要が立っていない"); Assert.Equal(t0, t1); }
        else { Assert.True(t1 > t0, "汽船の需要が立っていない"); Assert.Equal(s0, s1); }
    }

    // ★2026-09-27（回4-3 の再挑戦・作者裁定 2026-09-26 (3)）：転換は費用で起きる。参照価格の帆船(1.625)・汽船(3.1866)・石炭(0.54)と点火した ShipFuelRate のもとで、
    //   前線技術 1.84（この木の seed 1・2 で1840年ごろ＝1838年 1.78〜1.81・1843年 1.88〜1.91）では帆船が安く（汽船＝era 0.94×0.0857≈0.081 > 帆船 0.054）、
    //   2.78（1883年 2.77〜2.78）では汽船が安い（era 0.55×0.0857≈0.047 < 0.054）。同じ1880年代でも石炭が2倍の盤面では帆船に戻る（0.55×0.129≈0.071）。
    [Theory]
    [InlineData(1.84, 1.0, true)]
    [InlineData(2.78, 1.0, false)]
    [InlineData(2.78, 2.0, true)]
    public void 汽船への転換は費用で起きる_1840年は帆船_1880年代は汽船_燃料が高いと帆船へ戻る(double frontierTech, double coalMult, bool sailWins)
    {
        var coef = new Coefficients();
        double coal = Catalog.RefPrices[Catalog.CoalId] * coalMult;
        var sim = ShipWorld(57, coef, Catalog.RefPrices[84], Catalog.RefPrices[85], steamSupplied: true, frontierTech, coal);
        var (s0, t0) = ShipDemand(sim.World);
        Transport.Step(sim.World, coef);
        var (s1, t1) = ShipDemand(sim.World);
        if (sailWins) { Assert.True(s1 > s0, "帆船の需要が立っていない"); Assert.Equal(t0, t1); }
        else { Assert.True(t1 > t0, "汽船の需要が立っていない"); Assert.Equal(s0, s1); }
    }

    [Fact]
    public void 汽船の性能は機関の時代係数で上がる_1単位の容量は2倍で容量あたりの燃料は半分()
    {
        // era＝EraFactor(前線技術, EraColonyCoal)：前線 1.0（運賃革命のランプの前）で 1.0・3.5（満了の後）で 0.5。
        //   ①汽船1単位の容量＝30×2.5×解禁度÷era＝75→150（同時代の換算「汽船1トンは帆船3〜5トン分」）②容量あたりの燃料＝ShipFuelRate×era＝半分。
        //   ③州の解禁度が半分（州の技術1.0＝onset0.5＋幅1の半ば）なら1単位の容量も半分（外洋で使いこなせる度合い）。
        var coef = new Coefficients();
        double CapPerUnit(double tech, double regionTech)
        {
            var sim = ShipWorld(58, coef, sailPrice: 10.0, steamPrice: 1.0, steamSupplied: true, frontierTech: tech, regionTech: regionTech);   // 汽船が安い
            var w = sim.World;
            double stock0 = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) stock0 += r.Markets[85].Stock;
            Transport.Step(w, coef);
            double stock1 = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) stock1 += r.Markets[85].Stock;
            Assert.Equal(1.0, w.ShipSteamShareEma);
            return w.ShipCapacity / (stock0 - stock1);
        }
        double early = CapPerUnit(1.0, 1.5), late = CapPerUnit(3.5, 1.5), half = CapPerUnit(1.0, 1.0);
        Assert.Equal(coef.TransportCapPerUnit * coef.ShipTier2CapMult, early, 6);   // 買った単位は在庫(1e6)の差から数える＝差の丸め（約1e-9）ぶんの許容
        Assert.Equal(2.0 * early, late, 6);
        Assert.Equal(0.5 * early, half, 6);

        double FuelDemand(double tech)
        {
            var sim = ShipWorld(59, coef, sailPrice: 10.0, steamPrice: 1.0, steamSupplied: true, frontierTech: tech);
            var w = sim.World;
            w.ShipCapacity = 1e9; w.ShipSteamShareEma = 1.0;   // 目標を超える容量＝船は買わない＝燃料だけを比べる
            double d0 = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) d0 += r.Markets[Catalog.CoalId].LastDemand;
            Transport.Step(w, coef);
            double d1 = 0; foreach (var r in w.Regions) if (r.State == RegionState.Active) d1 += r.Markets[Catalog.CoalId].LastDemand;
            return d1 - d0;
        }
        double fEarly = FuelDemand(1.0), fLate = FuelDemand(3.5);
        Assert.True(fEarly > 0, "汽船が石炭を焚いていない");
        Assert.Equal(coef.ShipFuelRate * 1e9 * (1 - coef.TransportDecay), fEarly, 3);   // era＝1
        Assert.Equal(0.5 * fEarly, fLate, 6);
    }

    [Fact]
    public void 船の選択_汽船の供給が無い州では汽船の需要が立たない()
    {
        var coef = new Coefficients();
        var sim = ShipWorld(52, coef, sailPrice: 10.0, steamPrice: 0.01, steamSupplied: false);   // 値段だけなら汽船が圧倒的に安い
        var (s0, t0) = ShipDemand(sim.World);
        Transport.Step(sim.World, coef);
        var (s1, t1) = ShipDemand(sim.World);
        Assert.Equal(t0, t1);                          // 作れない船の需要は0（旧＝在庫0でも汽船の需要を立てていた）
        Assert.True(s1 > s0, "帆船の需要が立っていない");
        Assert.Equal(0.0, sim.World.ShipSteamShareEma);
    }

    [Fact]
    public void 船の選択_汽船が未発見の州では在庫があっても汽船の需要が立たない()
    {
        // ★2026-09-27（回4-3 の再挑戦）：全州は開幕に汽船200単位の在庫を持つ（Simulation.CatalogEconomy の一律の初期在庫）＝在庫だけを供給と数えると
        //   1783年に商人がその在庫を買い、汽船の割合が開幕に7割（seed 1 の1788年・61カ国中44カ国で過半）へ跳ねた。発見（解禁度＞0）を候補の条件にする。
        var coef = new Coefficients();
        var sim = ShipWorld(60, coef, sailPrice: 10.0, steamPrice: 0.01, steamSupplied: true, regionTech: 0.3);   // 在庫も生産もあり値段だけなら汽船が圧倒的に安いが、州の技術は onset0.5 の手前
        var (s0, t0) = ShipDemand(sim.World);
        Transport.Step(sim.World, coef);
        var (s1, t1) = ShipDemand(sim.World);
        Assert.Equal(t0, t1);
        Assert.True(s1 > s0, "帆船の需要が立っていない");
        Assert.Equal(0.0, sim.World.ShipSteamShareEma);
    }

    [Fact]
    public void 船の選択_積んだ容量は単位の容量どおりで代金は生産者の持ち主へ保存される()
    {
        var coef = new Coefficients();
        var sim = ShipWorld(53, coef, sailPrice: 10.0, steamPrice: 1.0, steamSupplied: true);   // 汽船が安い
        var w = sim.World;
        double era = coef.EraFactor(w.WorldTechLevel, coef.EraColonyCoal);
        double stock0 = 0, money0 = 0, buyers0 = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            stock0 += r.Markets[85].Stock; money0 += r.SumSavings();
            if (r.InvestorPopIndex >= 0) buyers0 += r.Pops[r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex].Savings;
        }
        Transport.Step(w, coef);
        double stock1 = 0, money1 = 0, buyers1 = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            stock1 += r.Markets[85].Stock; money1 += r.SumSavings();
            if (r.InvestorPopIndex >= 0) buyers1 += r.Pops[r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex].Savings;
        }
        double bought = stock0 - stock1;
        Assert.True(bought > 0, "汽船を買っていない");
        Assert.Equal(bought * coef.TransportCapPerUnit * coef.ShipTier2CapMult / era, w.ShipCapacity, 6);   // 汽船は1単位で容量×2.5÷era（購入前の容量は0）
        Assert.Equal(1.0, w.ShipSteamShareEma);
        Assert.Equal(money0, money1, 3);                                    // 買い手→生産者の持ち主＝州の中の移転（船も燃料も保存）
        Assert.True(buyers1 < buyers0 - 0.5 * bought * 1.0, "買い手が代金を払っていない（払い戻しに落ちている）");
    }

    [Fact]
    public void 船の選択_買い手が生産者の持ち主でも金は保存される()
    {
        // ⚠実装で踏んだ罠の番人：`買い手.Savings -= 払う関数(...)` と書くと、左辺を先に読むので、買い手が持ち主のとき受け取った分が上書きで消える
        //   （ConservationTests の貨幣保存が −0.03 で赤になった）。汽船の生産者の持ち主を買い手（商人）にして、州の貯蓄の和が動かないことを見る。
        var coef = new Coefficients();
        var sim = ShipWorld(56, coef, sailPrice: 10.0, steamPrice: 1.0, steamSupplied: true);
        var w = sim.World;
        double money0 = 0; int owned = 0;
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active || r.InvestorPopIndex < 0) continue;
            int buyerIdx = r.MerchantPopIndex >= 0 ? r.MerchantPopIndex : r.InvestorPopIndex;
            foreach (var pr in r.Producers) if (pr.GoodId == 85 || pr.GoodId == Catalog.CoalId) { pr.OwnerPopIndex = buyerIdx; owned++; }
            money0 += r.SumSavings();
        }
        Assert.True(owned > 0, "汽船の生産者が居ない世界ではテストにならない");
        Transport.Step(w, coef);
        double money1 = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active && r.InvestorPopIndex >= 0) money1 += r.SumSavings();
        Assert.True(System.Math.Abs(money1 - money0) <= 1e-12 * money0 + 1e-6, $"金が保存されていない（{money0:R} → {money1:R}）");
    }

    [Fact]
    public void 汽船の割合は減耗と購入をどう重ねても容量の在庫の割合と一致する()
    {
        var coef = new Coefficients();
        var sim = ShipWorld(54, coef, sailPrice: 10.0, steamPrice: 1.0, steamSupplied: true, frontierTech: 2.3);   // 運賃革命のランプの途中（era が1でも0.5でもない）
        var w = sim.World;
        double era = coef.EraFactor(w.WorldTechLevel, coef.EraColonyCoal);
        w.ShipCapacity = 5000; w.ShipSteamShareEma = 0;   // 始まりは帆船だけ
        double steamCap = 0;
        for (int k = 0; k < 12; k++)
        {
            bool steamCheap = k % 3 != 1;
            double stock85 = 0;
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                r.Markets[84].Price = steamCheap ? 10.0 : 0.2;   // 帆船 0.2/30 < 汽船 1/(75÷era)＋燃料
                stock85 += r.Markets[85].Stock;
            }
            if (k % 4 == 3) { const double wear = 0.3; w.ShipCapacity *= 1.0 - wear; steamCap *= 1.0 - wear; }   // 戦災（Diplomacy の輸送容量の戦災と同じ一律の掛け算）
            Transport.Step(w, coef);
            double stock85b = 0;
            foreach (var r in w.Regions) if (r.State == RegionState.Active) stock85b += r.Markets[85].Stock;
            steamCap = steamCap * (1 - coef.TransportDecay) + (stock85 - stock85b) * coef.TransportCapPerUnit * coef.ShipTier2CapMult / era;
            double expect = steamCap / w.ShipCapacity;   // 汽船の容量は在庫(1e6)の差から数える＝差の丸め（約1e-10）ぶんだけ相対の許容を置く
            Assert.True(System.Math.Abs(w.ShipSteamShareEma - expect) <= 1e-9 * System.Math.Max(expect, 1e-12), $"{k}: 割合 {w.ShipSteamShareEma:R} ≠ 汽船の容量÷容量 {expect:R}");
        }
        Assert.True(w.ShipSteamShareEma > 0 && w.ShipSteamShareEma < 1, $"帆船と汽船が混ざっていない（{w.ShipSteamShareEma}）");
    }

    [Fact]
    public void 汽船燃料ON_汽船の割合が0の国は燃料を買わない()
    {
        var coef = new Coefficients { ShipFuelRate = 1.0 };
        var sim = ShipWorld(55, coef, sailPrice: 10.0, steamPrice: 0.01, steamSupplied: false);   // 汽船は買えない＝割合0のまま
        var w = sim.World;
        w.ShipSteamShareEma = 0; w.ShipCapacity = 1000.0;
        double d0 = 0, st0 = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { d0 += r.Markets[Catalog.CoalId].LastDemand + r.Markets[Catalog.CrudeOilId].LastDemand; st0 += r.Markets[Catalog.CoalId].Stock; }
        Transport.Step(w, coef);
        double d1 = 0, st1 = 0;
        foreach (var r in w.Regions) if (r.State == RegionState.Active) { d1 += r.Markets[Catalog.CoalId].LastDemand + r.Markets[Catalog.CrudeOilId].LastDemand; st1 += r.Markets[Catalog.CoalId].Stock; }
        Assert.Equal(0.0, w.ShipSteamShareEma);
        Assert.Equal(d0, d1);
        Assert.Equal(st0, st1);
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
