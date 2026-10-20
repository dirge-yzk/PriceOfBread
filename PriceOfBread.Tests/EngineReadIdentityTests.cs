using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(474) 恒等の線 2 件（画-05・画-06）の番人（政-14 は恒等でなく外した＝実装ログ (474)）。成績表が旧と全行一致であることは別（バッチ）。ここは〈予算表の税の実効を読む診断が国庫の台帳と合う〉
/// 〈軍事タブと挑発のボタン脇が読むエンジンの関数が手計算と合う〉を、期待値を別の出所（手で書いた数・台帳・実際の機構）から作って見る。</summary>
public class EngineReadIdentityTests
{
    // ===== 画-05：予算表の税の実効は徴税の走査が書く種類ごとの実際の税収 =====

    private static World FiscalBoard()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(17), new NullAgent());
        sim.Run(1);   // 前の月の所得（LastEarnedPerMonth）を立てる
        var w = sim.World;
        w.TaxRate = new Share(0); w.LandTax = 0; w.ProgressiveTax = 0; w.PollTax = 0;
        w.Coef.TitheRate = new Share(0);
        foreach (var rg in w.Regions) { rg.Rule = [1.0, 0.0, 0.0, 0.0]; rg.AdminFill = 1.0; }   // 国家が全域を治め、官僚の役務も満額届く
        w.SocialSecurity = 0; w.Pension = ReformLevel.None;   // 福祉・年金を止める（税の診断と直交）
        w.GovDebt = new MoneyStock(0); w.ForeignDebt = new MoneyStock(0);
        foreach (var rg in w.Regions) foreach (var pp in rg.Pops) { pp.BondHoldings = new MoneyStock(0); pp.SetSavings(System.Math.Max(pp.Savings.Value, 0) + 1e3 * pp.Size); }   // 全員が払える＝払える割合は 1
        w.FiscalFundingRatio = new Share(1.0);
        return w;
    }

    private static double Run(World w)
    {
        double t0 = w.TreasuryLedger[(int)TreasuryCat.Tax];
        Fiscal.Step(w, w.Coef);
        return w.TreasuryLedger[(int)TreasuryCat.Tax] - t0;
    }

    private static double Near(double expected) => System.Math.Max(1e-9, 1e-9 * System.Math.Abs(expected));

    /// <summary>人頭税だけを課した月＝人頭税の診断は〈一律額×払う頭数〉の手計算に合い、他の 3 つは 0・台帳の税収も同じ額。
    /// ⚠突然変異「人頭税の診断を所得税の欄へ書く」「診断に免税の閉鎖階級を数える」で赤。</summary>
    [Fact]
    public void 予算表の人頭税の実効は一律額と払う頭数の手計算と台帳に合う()
    {
        var w = FiscalBoard();
        w.PollTax = Government.PollTaxTop;
        double heads = w.AllPops().Where(p => p.IsCommoner && !p.IsCriminal).Sum(p => p.Size);
        double expected = w.Coef.PollTaxRate * Government.PollTaxTop * heads;
        Assert.True(expected > 0, "前提が崩れた：払う頭数が 0");
        double ledger = Run(w);
        Assert.InRange(w.LastTaxPollPerMonth.Value, expected - Near(expected), expected + Near(expected));
        Assert.Equal(0.0, w.LastTaxIncomePerMonth.Value);
        Assert.Equal(0.0, w.LastTaxProgressivePerMonth.Value);
        Assert.Equal(0.0, w.LastTaxLandPerMonth.Value);
        Assert.InRange(ledger, expected - Near(expected), expected + Near(expected));
    }

    /// <summary>4 つの税を全部課した月＝種類ごとの診断は全て正で、和は台帳の税収と合う（診断は台帳とは別の足し方＝納税者ごとの査定×届く割合×払える割合）。
    /// 課税への同意が効く世界（TaxConsentWeight 既定 0.35・開幕の仏は大衆に参政権が無い）では、所得税の実効は〈税率×前の月の所得の和〉より小さい（旧の画面の式が落としていた分）。
    /// （★(475) 突然変異「種類の診断が払える割合を掛けない」はこの盤面〔全員に貯蓄を足す＝払える割合 1〕では検出できない＝次の試験〔貯蓄 0〕で赤・反証役 hp〔v〕低#3）。</summary>
    [Fact]
    public void 予算表の税の実効の和は台帳の税収と合い同意で削られた分だけ画面の旧の式より小さい()
    {
        var w = FiscalBoard();
        w.TaxRate = new Share(0.1); w.ProgressiveTax = 0.2; w.PollTax = Government.PollTaxTop; w.LandTax = 0.2;
        double oldIncomeFormula = 0.1 * w.AllPops().Where(p => !p.IsCriminal).Sum(p => System.Math.Max(0, p.LastEarnedPerMonth.Value));
        Assert.True(w.Coef.TaxConsentWeight > 0 && oldIncomeFormula > 0, "前提が崩れた");
        double ledger = Run(w);
        double a = w.LastTaxIncomePerMonth.Value, b = w.LastTaxProgressivePerMonth.Value, c = w.LastTaxPollPerMonth.Value, d = w.LastTaxLandPerMonth.Value;
        Assert.True(a > 0 && b > 0 && c > 0 && d > 0, $"種類の診断が正でない（所得 {a}・累進 {b}・人頭 {c}・土地 {d}）");
        Assert.InRange(a + b + c + d, ledger - Near(ledger), ledger + Near(ledger));
        Assert.True(a < oldIncomeFormula, $"同意で削られているはずの所得税の実効 {a} が旧の画面の式 {oldIncomeFormula} 以上");
    }

    /// <summary>誰も払えない月（貯蓄 0）＝診断も台帳も 0（税率×課税ベースの旧の式は正のまま＝国庫の実際の歳入と合わなかった）。
    /// ⚠突然変異「種類の診断が払える割合を掛けない」で診断の和が正になって赤（★(475) 前の試験の注から移した）。</summary>
    [Fact]
    public void 払える貯蓄が無ければ予算表の税の実効は0で旧の画面の式だけが正のまま()
    {
        var w = FiscalBoard();
        w.TaxRate = new Share(0.1); w.PollTax = Government.PollTaxTop;
        foreach (var rg in w.Regions) foreach (var pp in rg.Pops) pp.SetSavings(0);
        double oldIncomeFormula = 0.1 * w.AllPops().Where(p => !p.IsCriminal).Sum(p => System.Math.Max(0, p.LastEarnedPerMonth.Value));
        Assert.True(oldIncomeFormula > 0, "前提が崩れた");
        double ledger = Run(w);
        Assert.Equal(0.0, ledger);
        Assert.Equal(0.0, w.LastTaxIncomePerMonth.Value + w.LastTaxProgressivePerMonth.Value + w.LastTaxPollPerMonth.Value + w.LastTaxLandPerMonth.Value);
    }

    // ===== 画-06：軍事タブが読むエンジンの関数 =====

    private static Region CapitalRegion(out Coefficients c)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(29), new NullAgent()).World;
        c = w.Coef;
        return w.Region;
    }

    /// <summary>武器庫の在庫の読み＝武器庫が一度も開いていない（null）なら目標・開いた武器庫は財の在庫（無い財は 0）。目標在庫と発注量は手計算。
    /// ⚠突然変異「武器庫が開いていないとき 0」「開いた武器庫の無い財を目標」「発注に陸海の乗数を掛けない」で赤。</summary>
    [Fact]
    public void 武器庫の在庫の読みと目標在庫と発注量は手計算に合う()
    {
        var r = CapitalRegion(out var c);
        const int g = 7;
        r.ArmsStock = null;
        Assert.Equal(123.0, EconomyStep.ArmsOnHand(r, g, 123.0));      // 開いていない＝目標
        r.ArmsStock = new System.Collections.Generic.Dictionary<int, double> { [g] = 40.0 };
        Assert.Equal(40.0, EconomyStep.ArmsOnHand(r, g, 123.0));       // 開いた武器庫の在庫
        Assert.Equal(0.0, EconomyStep.ArmsOnHand(r, g + 1, 123.0));    // 開いた武器庫に無い財＝0
        // 目標在庫＝平時規模×原単位×陸海の乗数÷摩耗（手計算）
        Assert.Equal(200.0 * 0.5 * 0.4 / 0.05, EconomyStep.BaseArmsTarget(200.0, 0.5, 0.05, 0.4), 12);
        // 発注量＝(目標−在庫)⁺（耐久品）／フローは規模×原単位×乗数
        r.MilitaryScalePeace = 200.0; r.MilitaryScale = 150.0;
        double wear = c.ArmsWearRatePerMonth.Value;
        double target = 200.0 * 0.5 * 1.0 / wear;
        r.ArmsStock = new System.Collections.Generic.Dictionary<int, double> { [g] = target - 10.0 };
        Assert.Equal(10.0, EconomyStep.TierQty(r, c, g, 0.5, true, 1.0), 9);
        r.ArmsStock[g] = target + 5.0;
        Assert.Equal(0.0, EconomyStep.TierQty(r, c, g, 0.5, true, 1.0));
        Assert.Equal(150.0 * 0.5 * 0.0, EconomyStep.TierQty(r, c, g, 0.5, false, 0.0));   // 艦（乗数 0）は陸の装備の段で 0
        Assert.Equal(150.0 * 0.5 * 1.0, EconomyStep.TierQty(r, c, g, 0.5, false, 1.0));
    }

    /// <summary>陸の装備の段の見積は艦（<see cref="Coefficients.FleetGoodIds"/>）を数えない＝軍艦だけに原単位を置いた見積は 0・小銃だけなら〈規模×原単位×値段〉。
    /// （旧の画面の見積は艦も数えた＝予算の充足がエンジンより低く出た。）⚠突然変異「見積が陸海の乗数を掛けない」で赤。</summary>
    [Fact]
    public void 陸の装備の見積は艦を数えず手計算に合う()
    {
        var r = CapitalRegion(out var c);
        r.MilitaryScale = 80.0;
        int n = c.MilitaryGoods.Length;
        int ship = System.Array.IndexOf(c.MilitaryGoods, Catalog.WarshipId), rifle = System.Array.IndexOf(c.MilitaryGoods, Catalog.RifleId);
        Assert.True(ship >= 0 && rifle >= 0 && System.Array.IndexOf(c.FleetGoodIds, Catalog.WarshipId) >= 0, "前提が崩れた（軍艦か小銃が基本段にない）");
        var onlyShip = new double[n]; onlyShip[ship] = 1.0;
        Assert.Equal(0.0, EconomyStep.TierCost(r, c, c.MilitaryGoods, onlyShip, false));
        var onlyRifle = new double[n]; onlyRifle[rifle] = 0.25;
        double price = r.Markets[Catalog.RifleId].Price;
        Assert.Equal(80.0 * 0.25 * price, EconomyStep.TierCost(r, c, c.MilitaryGoods, onlyRifle, false), 9);
    }

    // ===== 画-06：挑発のボタン脇の近隣波及 =====

    /// <summary>標的から 111.19 km（赤道上で経度 1°）と 1111.95 km（10°）の近隣は波及を受け、4447.8 km（40°）の国は範囲（3000 km）の外で受けない。
    /// 数の期待値は手で書いた距離（R×π/180×度）から＝量は 係数×(1−距離÷範囲)。さらに実際の機構（挑発の反映の月数だけ <c>StepTensionRamp</c> を回す）で近隣へ足された緊張の和と見積もりの和が合う。
    /// ⚠突然変異「範囲の外にも波及」「距離で減衰しない」で赤。</summary>
    [Fact]
    public void 挑発のボタン脇の近隣波及は手計算の距離と実際の機構の足した緊張に合う()
    {
        var cfg = new[] { NationConfig.France(), NationConfig.Britain(), NationConfig.Netherlands(), NationConfig.Prussia(), NationConfig.Austria() };
        var sim = new MultiNationSim(cfg, 1789, _ => new NullAgent()) { DiplomacyFrozen = true };
        var c = sim.Coef;
        Power P(int i) => sim.Diplomacy.Powers.First(p => p.Nation == sim.Nations[i].World);
        // 標的＝英（赤道・経度 0）。近隣 A＝蘭（経度 1°）・B＝普（経度 10°）・遠い C＝墺（経度 40°）。挑発者＝仏
        foreach (var (i, lon) in new[] { (1, 0.0), (2, 1.0), (3, 10.0), (4, 40.0), (0, 90.0) }) { sim.Nations[i].World.Latitude = 0; sim.Nations[i].World.Longitude = lon; }
        var actor = P(0); var target = P(1);
        const double kmPerDeg = 6371.0 * System.Math.PI / 180.0;   // 111.19492664…（赤道上の経度 1° の大圏距離）
        double Amount(double deg) => c.ActionTensionStep * c.ProvokeProximitySpread * (1 - kmPerDeg * deg / c.ProximityTensionRange);
        Assert.True(kmPerDeg * 40.0 > c.ProximityTensionRange && kmPerDeg * 10.0 < c.ProximityTensionRange, "前提が崩れた（範囲の係数が変わった）");
        double expected = Amount(1.0) + Amount(10.0);
        var (n, sum) = sim.Diplomacy.PreviewProvocationSpread(actor, target, c);
        Assert.Equal(2, n);
        Assert.Equal(expected, sum, 9);
        // 実際の機構：挑発して反映の月数だけ回す＝近隣（蘭・普・墺）への挑発者の緊張の増分の和が見積もりの和
        double Tension3() => sim.Diplomacy.Tension(actor, P(2)) + sim.Diplomacy.Tension(actor, P(3)) + sim.Diplomacy.Tension(actor, P(4));
        double before = Tension3();
        Assert.True(sim.Diplomacy.RaiseTension(actor, target, c), "前提が崩れた（挑発できない）");
        for (int m = 0; m < c.TensionActionRampMonths + 2; m++) sim.Diplomacy.StepTensionRamp(c);
        Assert.Equal(expected, Tension3() - before, 6);
    }
}
