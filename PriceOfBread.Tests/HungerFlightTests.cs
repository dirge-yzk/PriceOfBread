using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1335) c140hf 旗 DomesticStayFoodFill：国内の移民の残る値打ちを越境と同じ 1 本の式（<see cref="Territory.StayValue(Region, Coefficients)"/>＝機会×残る人の土地の持ち分×食の充足）にする。
/// 事前登録 measurements/m1335_20261124_prereg_hf.txt。盤面は試験の中で置く（機会は生産性の倍で、持ち分は耕地で、食の充足は州の欄で直に置く）。
/// ⚠突然変異 μ1（StayValue から食の充足を外す）で T1・T3 赤・μ2（食の充足の 2 乗）で T3 赤・μ3（越境の側だけ食の項なしの式へ）で T3 赤・μ0（旗を読まない＝旧の規則）で T1 赤。</summary>
public class HungerFlightTests
{
    private static Scenario Frontier(ulong s) => new() { Seed = s, Kind = ScenarioKind.FrontierDevelopment };

    /// <summary>2 州の盤面（本土→辺境）。本土の機会と辺境の機会を同じ値 O にそろえ、本土の持ち分を 1 より大きく置く（旧の残る値打ち O×持ち分＞O−摩擦＝旧の規則では 1 人も出ない）。</summary>
    private static (Simulation Sim, Region S, Region D) HungryBoard(double flag, double foodFill)
    {
        var sim = new Simulation(Frontier(1), new NullAgent());
        var w = sim.World; var c = w.Coef;
        c.DomesticStayFoodFill = flag;
        c.ImmigrationReceiveCapPerQuarter = new RatePerQuarter(1e9);   // 受け入れの上限を外す（上限で縮められて 0 になるのを除く）
        Region s = w.Regions[0], d = w.Regions[1];
        s.ArableLand = s.SumRuralPopulation() * 1.3;   // 持ち分＝1.3^0.7≈1.20
        d.ArableLand = 1e7;                            // 入る側は混まない
        s.FoodFill = foodFill; d.FoodFill = 1.0;
        const double O = 1.0;
        foreach (var r in new[] { s, d }) r.ProductivityBonus *= O / Territory.Opportunity(r, c);
        return (sim, s, d);
    }

    [Fact]
    public void T1_飢えた州から隣へ人が出る_旗0では出ない()
    {
        var (sim1, s1, d1) = HungryBoard(1.0, 0.3);
        var c = sim1.World.Coef;
        double h = Territory.Holding(s1, c);
        Assert.True(h > 1.1, $"前提が崩れた：持ち分 {h}");
        Assert.True(Math.Abs(Territory.Opportunity(s1, c) - 1.0) < 1e-9 && Math.Abs(Territory.Opportunity(d1, c) - 1.0) < 1e-9, "前提が崩れた：機会がそろわない");
        Assert.True(1.0 - 1.0 * h - c.TransportFriction < 0, "前提が崩れた：旧の残る値打ちでも差が正");
        var cs = Places.Coord(s1.Name); var cd = Places.Coord(d1.Name);
        double km = Geo.DistanceKm(cs.Lat, cs.Lon, cd.Lat, cd.Lon);
        double fric = double.IsFinite(km) ? Territory.EntryFrictionTerm(km, sim1.World.WorldTechLevel, c) : 0.0;
        Assert.True(1.0 - 1.0 * h * 0.3 - c.TransportFriction - fric > 0.1, "前提が崩れた：食の充足 0.3 でも差が正にならない");
        Assert.True(Territory.AbsorbCap(d1, c) > 0, "前提が崩れた：入る側の受け入れが 0");
        double s0 = s1.SumPopulation(), dd0 = d1.SumPopulation();
        Territory.Step(sim1.World, c);
        double outflow = s0 - s1.SumPopulation();
        Assert.True(outflow > 1e-6, $"飢えた州（食の充足 0.3・持ち分 {h:F3}）から人が出ない（{outflow}）");
        Assert.True(outflow <= 0.2 * s0 * (1 + 1e-12), $"1 か月の流出 {outflow} が上限 0.2×人口を超えた");
        Assert.True(d1.SumPopulation() - dd0 > 1e-6, "辺境に人が着かない");

        var (sim0, s0b, _) = HungryBoard(0.0, 0.3);
        double p0 = s0b.SumPopulation();
        Territory.Step(sim0.World, sim0.World.Coef);
        Assert.Equal(p0, s0b.SumPopulation());   // 旗 0＝旧の規則（食の項なし）＝持ち分で止まる＝1 人も出ない
    }

    [Fact]
    public void T2_食の充足が1なら旗0と同じ流れ()
    {
        Simulation Run(double flag)
        {
            var sim = new Simulation(Frontier(1), new NullAgent());
            var w = sim.World; var c = w.Coef;
            c.DomesticStayFoodFill = flag;
            Region core = w.Regions[0], fr = w.Regions[1];
            fr.ProductivityBonus = core.ProductivityBonus;
            core.WagePremium = 0.25; fr.WagePremium = 4.0;
            core.ArableLand = core.SumPopulation() * 1.8;
            fr.ArableLand = 1e7;
            foreach (var r in w.Regions) r.FoodFill = 1.0;
            for (int m = 0; m < 24; m++) Territory.Step(w, c);
            return sim;
        }
        var a = Run(0.0); var b = Run(1.0);
        double core0 = new Simulation(Frontier(1), new NullAgent()).World.Regions[0].SumPopulation();
        Assert.True(a.World.Regions[0].SumPopulation() < core0 - 1.0, "前提が崩れた：旗 0 でも流れていない");
        for (int i = 0; i < a.World.Regions.Count; i++)
        {
            var pa = a.World.Regions[i].Pops; var pb = b.World.Regions[i].Pops;
            Assert.Equal(pa.Count, pb.Count);
            for (int j = 0; j < pa.Count; j++)
                Assert.True(BitConverter.DoubleToInt64Bits(pa[j].Size) == BitConverter.DoubleToInt64Bits(pb[j].Size),
                    $"食の充足 1 で旗 0 と旗 1 の流れが違う（州 {i} の POP {j}：{pa[j].Size:R} と {pb[j].Size:R}）");
        }
    }

    private static MultiNationSim CrossBoard(double flag)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス"), WorldData.ByName("アメリカ"),
                                      WorldData.ByName("プロイセン"), WorldData.ByName("ロシア"), WorldData.ByName("スペイン")], seed: 1789, coef: new Coefficients { DomesticStayFoodFill = flag }) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.Diplomacy.Log.Enabled = false;
        sim.StepQuarter();
        return sim;
    }

    /// <summary>越境の送出州 src（英の最初の農民の居る州）の残る値打ちを、食の充足 1 では最良の行き先の機会−閾値の 1/0.6 倍（出ない）・0.3 では 1/2 倍（出る）に置く。
    /// 他の州は食の充足 1＝その州の残る値打ち≧その州の機会＞src の機会＝src へは誰も入らない（src の農民の減り＝流出だけ）。</summary>
    private static (double Before, double After, double Stay, double Formula) CrossRun(double flag, double foodFill)
    {
        var sim = CrossBoard(flag);
        var c = sim.Coef;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) r.FoodFill = 1.0;
        var sw = sim.Nations[0].World;
        var src = sw.Regions.First(r => r.PopOf(PopClass.Peasant) is not null);
        src.ArableLand = Math.Max(1.0, src.SumRuralPopulation());   // 耕地÷農村＝1（土地の因子 1・持ち分 1）
        double oMin = double.PositiveInfinity, best = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            if (ReferenceEquals(r, src)) continue;
            double o = Territory.Opportunity(r, c);
            if (!double.IsFinite(o)) continue;
            oMin = Math.Min(oMin, o);
            if (!ReferenceEquals(n.World, sw)) best = Math.Max(best, o);
        }
        src.ProductivityBonus *= 0.5 * oMin / Territory.Opportunity(src, c);
        double o1 = Territory.Opportunity(src, c);
        Assert.True(Math.Abs(o1 - 0.5 * oMin) < 1e-9 * oMin, "前提が崩れた：src の機会");
        double g = best - c.CrossBorderOppGapThreshold;
        Assert.True(g > 0, "前提が崩れた：最良の行き先の機会が閾値以下");
        double hWant = g / (0.6 * o1);
        Assert.True(hWant > 1.0, "前提が崩れた：要る持ち分が 1 以下");
        src.ArableLand = Math.Max(1.0, src.SumRuralPopulation()) * Math.Pow(hWant, 1.0 / c.LandDiminishExponent);
        Assert.True(src.ArableLand < 1e8, "前提が崩れた：耕地が未設定の印に届いた");
        Assert.True(Math.Abs(Territory.Opportunity(src, c) - o1) < 1e-12 * o1, "前提が崩れた：耕地を広げて機会が動いた");
        Assert.True(Math.Abs(Territory.Holding(src, c) - hWant) < 1e-9 * hWant, "前提が崩れた：持ち分");
        src.FoodFill = foodFill;
        double formula = Territory.Opportunity(src, c) * Territory.Holding(src, c) * src.FoodFill;
        double stay = Territory.StayValue(src, c);
        var sp = src.PopOf(PopClass.Peasant)!;
        double before = sp.Size;
        sim.MigrateBetweenNations();
        return (before, sp.Size, stay, formula);
    }

    [Fact]
    public void T3_越境の送出はStayValueと同じ値で旗に依らない()
    {
        var full = CrossRun(0.0, 1.0);
        Assert.Equal(full.Before, full.After);   // 食の充足 1：残る値打ち＝(最良−閾値)/0.6＞最良−閾値＝1 人も出ない
        var hungry0 = CrossRun(0.0, 0.3);
        Assert.True(BitConverter.DoubleToInt64Bits(hungry0.Stay) == BitConverter.DoubleToInt64Bits(hungry0.Formula),
            $"StayValue {hungry0.Stay:R} が機会×持ち分×食の充足 {hungry0.Formula:R} と違う");
        Assert.True(hungry0.After < hungry0.Before - 1e-9, $"食の充足 0.3 の州から越境の流出が無い（{hungry0.Before}→{hungry0.After}）＝越境の送出が食の項を読んでいない");
        var hungry1 = CrossRun(1.0, 0.3);
        Assert.True(BitConverter.DoubleToInt64Bits(hungry0.After) == BitConverter.DoubleToInt64Bits(hungry1.After),
            $"越境の流出が国内の旗で変わった（{hungry0.After:R} と {hungry1.After:R}）");
    }

    [Fact]
    public void T4_移す前後で総人口が同じ()
    {
        var (sim, s, d) = HungryBoard(1.0, 0.3);
        var w = sim.World;
        double total0 = s.SumPopulation() + d.SumPopulation();
        for (int m = 0; m < 12; m++) Territory.Step(w, w.Coef);
        double total1 = s.SumPopulation() + d.SumPopulation();
        Assert.True(Math.Abs(total1 - total0) <= 1e-9 * total0, $"総人口が保存されない（{total0:R}→{total1:R}）");
        Assert.True(d.SumPopulation() > 10.0 + 1e-6, "前提が崩れた：12 か月で辺境へ人が移っていない");
    }
}
