using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 植民地POP（現地住民の実体）と独立：搾取の分け前が住民の蓄えになり（捕囚市場の財布）、
/// 搾取×識字（開発との接触）で不満が育つ。圧力（人口×不満）が本国の抑え（軍×即応度）を超えた大植民地は独立する。
/// 住民は本国の利益団体に属さない（台帳の外）。
/// </summary>
// ⚠2026-08-26：同上。植民地POP（住民・搾取・不満・独立）は Colony オブジェクトの機構なので、構築時の一括昇格を落として検証する。
public class ColonialPopTests
{
    [Fact]
    public void 植民地に住民が立ち搾取の分け前と不満が育つ()
    {
        // 外交を凍結＝戦争/侵略で植民地が移らない（装備された軍で植民地マップが動的になったため、本テストは植民地POP機構を戦争から隔離して検証する）。
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83, coef: new Coefficients { ColoniesAreRegions = 0 }) { DiplomacyFrozen = true };
        sim.Run(60);   // 15年＝採取が流れ不満が育ち始めるが、まだ独立線（0.7）には届かない頃
        var bengal = sim.Nations[0].World.SelfPower!.ColonyList.FirstOrDefault(c => c.Name == "中核ベンガル");
        Assert.NotNull(bengal);
        Assert.True(bengal!.PopSize > 0, "植民地に住民が居ない");
        Assert.True(bengal.Militancy > 0, "搾取されているのに不満が育たない");
    }

    [Fact]
    public void 不満が極まり抑えを超えた大植民地は独立する()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 89, coef: new Coefficients { ColoniesAreRegions = 0 });
        var power = sim.Nations[0].World.SelfPower!;
        var bengal = power.ColonyList.First(c => c.Name == "中核ベンガル");
        bengal.PopSize = 50_000; bengal.Militancy = 1.0;   // 巨大植民地が沸騰している状態を注入
        int before = power.Colonies;
        sim.Run(4);                                         // 独立判定は月次（Colonial.Step）
        Assert.True(power.Colonies < before, "圧力が抑えを超えているのに独立が起きない");
        Assert.DoesNotContain(power.ColonyList, c => c.Name == "中核ベンガル");
        Assert.Contains(sim.Diplomacy.Log, l => l.Contains("独立"));
    }

    [Fact]
    public void 序盤は不満が独立線に届かず離反は起きない()   // 不満の蓄積は数十年単位（戦争での植民地の移転は起きてよい）
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 97, coef: new Coefficients { ColoniesAreRegions = 0 });
        sim.Run(40);   // 10年＝不満はまだ独立線（0.7）に届かない
        Assert.DoesNotContain(sim.Diplomacy.Log, l => l.Contains("独立"));
    }

    /// <summary>★2026-09-28（植民地の費用の側・R2）：植民地の維持費（距離の物理消費）と海運占有は、行（州）の数でなく質量（人口ウェイト）で数える。
    /// 資源の無い遠い前哨（英の前哨の中で最も遠い州）は、それより近い最大の植民地より維持費も海運占有も小さい
    /// （旧＝行ごとに同じ重み＝遠い前哨の方が大きかった・英1783 の距離の71%が資源の無い前哨16州）。
    /// ⚠この番人は構築時の一括昇格（既定 ColoniesAreRegions=1）の盤面で測る＝植民地格の州。</summary>
    [Fact]
    public void 維持費と海運占有は行の数でなく質量で数える_資源の無い遠い前哨は近い大植民地より小さい()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        w.ShipSteamShareEma = new Share(1.0);   // 補給線が石炭を焚く状態（帆走の時代は距離の物理消費が0＝比べられない）
        w.ShipCapacity = 10_000;     // ★(173) R4：補給線の燃料は占有された船腹（容量×占有÷(1＋占有)）の汽船の分＝船腹が要る（構築の直後は容量0）
        double Km(Region r) { var cc = Places.Coord(r.Name); return Geo.SeaDistanceKm(w.Latitude, w.Longitude, cc.Lat, cc.Lon, false, false); }
        var cols = w.Regions.Where(r => r.State == RegionState.Active && r.Tier == RegionTier.Colonial).ToList();
        var outpost = cols.Where(r => r.Resources.Length == 0).OrderByDescending(Km).First();
        var big = cols.Where(r => Km(r) < Km(outpost)).OrderByDescending(r => r.SumPopulation()).First();
        Assert.True(Km(outpost) > Km(big) && outpost.SumPopulation() < big.SumPopulation(),
            $"前提が崩れている（前哨 {outpost.Name} {Km(outpost):F0}km・{outpost.SumPopulation():F0}／大植民地 {big.Name} {Km(big):F0}km・{big.SumPopulation():F0}）");
        (double Upkeep, double Burden) Only(Region keep)
        {
            foreach (var r in cols) r.Tier = r == keep ? RegionTier.Colonial : RegionTier.Integrated;
            Colonial.Step(w, w.Coef);
            foreach (var r in cols) r.Tier = RegionTier.Colonial;
            return (w.LastColonialUpkeepPerMonth.Value, w.ColonialShipBurden);
        }
        var o = Only(outpost);
        var b = Only(big);
        Assert.True(o.Burden > 0 && o.Upkeep > 0, $"前哨の維持費か海運占有が0（{o}）＝比べられない");
        Assert.True(o.Burden < b.Burden, $"遠い前哨 {outpost.Name} の海運占有 {o.Burden:G4} が近い大植民地 {big.Name} の {b.Burden:G4} 以上＝行の数で数えている");
        Assert.True(o.Upkeep < b.Upkeep, $"遠い前哨 {outpost.Name} の維持費の請求 {o.Upkeep:G4} が近い大植民地 {big.Name} の {b.Upkeep:G4} 以上＝行の数で数えている");
    }

    // ===== 2026-10-02 (173) 植民地の残り（R1・c6・R4）の番人 =====
    // ⚠要約の出どころ：R1 の受け手（不在地主＝本国首都の投資家 POP）と作物で分けること・R4 の線（汽船を急ぐ程度に経済的・汽船は石炭を足すが占有を減らす）は作者の裁定
    //   （2026-09-28・09-29）。輸出作物の判別（賦存かつ主食でない）・缶詰の撤去・占有の軽くし方（容量倍で割る）・燃料の分け方は AI の推奨（作者の承認は未確認）。

    private static Region ColonyWith(World w, int good) =>
        w.Regions.First(r => r.State == RegionState.Active && r.Tier == RegionTier.Colonial && r.Producers.Any(p => p.GoodId == good && Colonial.AbsenteeRgo(r, p)));

    /// <summary>★(173) R1＋c6：植民地格の州の賦存の輸出作物（主食でない）は大農園＝地代の取り分は本国の賦存州と同じ PlantationRentShare・持ち主の口座は資本家・
    /// 不在地主の判別が立つ。賦存の主食（トウモロコシ）は現地の地主のまま（自作農の床・持ち主は農民・不在地主でない＝「現地の地主＝今の持ち主」は AI の読み・(177) 反証役 vc1#7）。盤面＝英西の構築の直後（西の中央メキシコ＝サトウキビ・トウモロコシの賦存）。
    /// ⚠突然変異「主食も上書き」「上書きを外す」で赤。</summary>
    [Fact]
    public void 植民地の賦存の輸出作物は不在地主の大農園で主食は現地の地主のまま()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), WorldData.ByName("スペイン")], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[1].World;
        var mex = w.Regions.First(r => r.Name == "中央メキシコ");
        Assert.Equal(RegionTier.Colonial, mex.Tier);
        var sugar = mex.Producers.First(p => p.IsRgo && p.GoodId == Catalog.SugarCaneId);
        var maize = mex.Producers.First(p => p.IsRgo && p.GoodId == Catalog.MaizeId);
        Assert.True(Colonial.AbsenteeRgo(mex, sugar), "サトウキビ（賦存・主食でない）が不在地主の大農園と判別されない");
        Assert.Equal(w.Coef.PlantationRentShare, sugar.RentShare.Value);
        Assert.Equal(PopClass.Capitalist, mex.Pops[sugar.OwnerPopIndex].ClassId);
        Assert.False(Colonial.AbsenteeRgo(mex, maize), "トウモロコシ（主食）が不在地主の大農園と判別された");
        Assert.True(maize.RentShare.Value < w.Coef.PlantationRentShare, $"主食の地代の取り分が大農園の値（{maize.RentShare.Value}）＝現地の地主のままでない");
        Assert.Equal(PopClass.Peasant, mex.Pops[maize.OwnerPopIndex].ClassId);
    }

    /// <summary>★(173) R1：輸出代金（清算の受け取り＝InternationalMarket.PaySales）のうち植民地の輸出作物の大農園の地代は本国の首都の投資家へ直接（所得にも書く）・
    /// 在地の持ち主の口座は受け取らない・賃金は在地の労働へ。盤面＝英仏の英（ジャマイカのサトウキビ）。⚠突然変異「受け手を在地の持ち主へ戻す」で赤。</summary>
    [Fact]
    public void 植民地の輸出作物の輸出代金の地代は本国の首都の投資家が受け取る()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        var col = ColonyWith(w, Catalog.SugarCaneId);
        var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        var pr = col.Producers.First(p => p.GoodId == Catalog.SugarCaneId && Colonial.AbsenteeRgo(col, p));
        var local = col.Pops[pr.OwnerPopIndex];
        double i0 = inv.Savings.Value, ie0 = inv.EarnedPerMonth.Value, l0 = local.Savings.Value;
        double paid = InternationalMarket.PaySales((null, null), col, Catalog.SugarCaneId, 1000.0, w);
        Assert.Equal(1000.0, paid, 6);
        Assert.True(inv.Savings.Value - i0 > 1.0, $"本国の首都の投資家が地代を受け取らない（{inv.Savings.Value - i0}）");
        Assert.Equal(inv.Savings.Value - i0, inv.EarnedPerMonth.Value - ie0, 9);
        Assert.Equal(l0, local.Savings.Value, 9);
    }

    /// <summary>★(173) R1：国内の売上の地代は④で在地の口座に入り州の月末の精算で貯蓄になる＝精算の後に不在地主へ送る（Colonial.RemitAbsenteeRent）。
    /// 黒字＝在地の口座→首都の投資家（貯蓄・所得・地代の記録が同じ額だけ移る）・赤字＝首都の投資家が在地の口座を埋める。⚠突然変異「送らない」「所得の記録を動かさない」で赤。</summary>
    [Fact]
    public void 植民地の輸出作物の国内の地代は精算の後に本国の首都の投資家へ送られる()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        var col = ColonyWith(w, Catalog.SugarCaneId);
        var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        var local = col.Pops[PopClass.Capitalist];
        local.SetSavings(500); local.LandRentPerMonth = new MoneyPerMonth(200); local.EarnedPerMonth = new MoneyPerMonth(200);
        double i0 = inv.Savings.Value, ie0 = inv.EarnedPerMonth.Value, il0 = inv.LandRentPerMonth.Value;
        var net = new double[col.Pops.Count];
        net[PopClass.Capitalist] = 200;
        Colonial.RemitAbsenteeRent(w, col, net);
        Assert.Equal(300, local.Savings.Value, 9); Assert.Equal(0, local.EarnedPerMonth.Value, 9); Assert.Equal(0, local.LandRentPerMonth.Value, 9);
        Assert.Equal(i0 + 200, inv.Savings.Value, 9); Assert.Equal(ie0 + 200, inv.EarnedPerMonth.Value, 9); Assert.Equal(il0 + 200, inv.LandRentPerMonth.Value, 9);
        Assert.Equal(0, net[PopClass.Capitalist]);
        net[PopClass.Capitalist] = -50;   // 赤字＝不在地主が埋める
        Colonial.RemitAbsenteeRent(w, col, net);
        Assert.Equal(350, local.Savings.Value, 9); Assert.Equal(50, local.EarnedPerMonth.Value, 9);
        Assert.Equal(i0 + 150, inv.Savings.Value, 9); Assert.Equal(ie0 + 150, inv.EarnedPerMonth.Value, 9);
    }

    /// <summary>★(173) R1（呼び手の側＝EconomyStep の④）：植民地格の州の月の決算で、輸出作物の大農園が国内で売った地代は首都の投資家の貯蓄へ着く
    /// （＝不在地主の大農園の地代の和）・在地の資本家の口座には残らない。盤面＝英仏の英のジャマイカ（コーヒー＝州の住民が飲む）に住民の貯蓄を入れて1か月。
    /// ⚠突然変異「④で積まない」「精算の後に送らない」で赤。</summary>
    [Fact]
    public void 植民地の月の決算で輸出作物の地代は首都の投資家へ着く()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        var col = ColonyWith(w, Catalog.CoffeeId);
        // ★(227)：ジャマイカの住民を史実の約25万人へ直したら（旧 100万人）、コーヒーの国内の売り先が4分の1になり大農園の地代が赤字（-3.3）になった＝この番人が見るのは地代の送り先で住民数ではない。
        //   盤面を旧の規模（100万人）へ戻して測る（住民の人数を一括で掛ける・貯蓄は旧と同じ1POPあたり 1e5）。
        double upscale = 1000.0 / col.SumPopulation();
        foreach (var p in col.Pops) if (p.Size > 1e-9) { p.Size *= upscale; p.SetSavings(p.Savings.Value + 1e5); }
        var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        double i0 = inv.Savings.Value;
        EconomyStep.Run(col, w.Coef, 1.0, w.Bank, w);
        double rent = 0;
        foreach (var p in col.Producers) if (Colonial.AbsenteeRgo(col, p)) rent += p.LastRevenue - p.LastCost;
        Assert.True(rent > 1e-6, $"前提が崩れた：不在地主の大農園に国内の地代が立たない（{rent}）");
        Assert.Equal(rent, inv.Savings.Value - i0, 6);
    }

    /// <summary>★(173) R4①：植民地の補給線は駐留の缶詰を買わない（駐留の兵は兵士 POP で給与から州の食料を買う＝2度食わせない・思想⑨）。
    /// 盤面＝英仏の英・缶詰を発見済みにして植民地の月（Colonial.Step）を1回＝缶詰の需要が立たない。⚠突然変異「缶詰の請求を戻す」で赤。</summary>
    [Fact]
    public void 植民地の補給線は駐留の缶詰を買わない()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        var r = w.Region;
        r.Discovered = w.Discovered;
        foreach (var row in Enumerable.Range(0, Catalog.Discoveries.Length).Where(i => Catalog.Discoveries[i].Good == Catalog.CannedFoodId)) w.Discovered[row] = true;
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.CannedFoodId));
        w.ShipCapacity = 10_000; w.ShipSteamShareEma = new Share(1.0);
        double d0 = r.Markets[Catalog.CannedFoodId].LastDemand;
        Colonial.Step(w, w.Coef);
        Assert.Equal(d0, r.Markets[Catalog.CannedFoodId].LastDemand);
    }

    /// <summary>★(173) R4②③：汽船は補給線の占有を軽くし（占有×((1−割合)＋割合÷容量倍)・容量倍＝ShipTier2CapMult×汽船の解禁度÷機関の時代係数＝民間の船の選び方と同じ）、
    /// 占有された船腹の汽船の分の燃料を国庫が買う（ShipFuelRate×時代係数×割合×占有された容量）・商人は交易に使える容量の分だけ焚く（2つの和＝船隊の全ての燃料＝2度焚かない）。
    /// 盤面＝英仏の英・汽船を発見済み・船腹 10,000・汽船の割合 0 と 1。⚠突然変異「占有を軽くしない」「旧の 400×距離×質量の石炭」「商人が全ての船腹を焚く」で赤。</summary>
    [Fact]
    public void 汽船は補給線の占有を軽くし占有された船腹の燃料だけを国庫が買う()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var c = w.Coef; var r = w.Region;
        r.Discovered = w.Discovered;
        foreach (var row in Enumerable.Range(0, Catalog.Discoveries.Length).Where(i => Catalog.Discoveries[i].Good == Catalog.SteamshipId)) w.Discovered[row] = true;
        Assert.Equal(1.0, Discovery.GoodGate(r, Catalog.SteamshipId));
        w.ShipCapacity = 10_000; w.FiscalFundingRatio = new Share(1.0);
        double era = c.EraFactor(w.WorldTechLevel, c.EraColonyCoal);
        double mult = c.ShipTier2CapMult / era;
        w.ShipSteamShareEma = new Share(0.0); Colonial.Step(w, c); double b0 = w.ColonialShipBurden;
        w.ShipSteamShareEma = new Share(1.0);
        double coal0 = r.Markets[Catalog.CoalId].LastDemand;
        Colonial.Step(w, c); double b1 = w.ColonialShipBurden;
        Assert.True(b0 > 0 && mult > 1, $"前提が崩れた（占有 {b0}・容量倍 {mult}）");
        Assert.Equal(b0 / mult, b1, 9);
        double occ = Colonial.OccupiedShipCapacity(w);
        Assert.Equal(w.ShipCapacity * b1 / (1 + b1), occ, 9);
        double want = c.ShipFuelRate * era * 1.0 * occ * EconomyStep.StatePriceAfford(r, c, Catalog.CoalId);
        Assert.Equal(want, r.Markets[Catalog.CoalId].LastDemand - coal0, 9);
        // 商人の燃料（Transport の BuyShipFuel）＝交易に使える容量の分だけ
        double before = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) before += rg.Markets[Catalog.CoalId].LastDemand;
        typeof(Transport).GetMethod("BuyShipFuel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, [w, c, era]);
        double after = 0; foreach (var rg in w.Regions) if (rg.State == RegionState.Active) after += rg.Markets[Catalog.CoalId].LastDemand;
        Assert.Equal(c.ShipFuelRate * era * (w.ShipCapacity - occ) * 1.0, after - before, 6);
    }
}
