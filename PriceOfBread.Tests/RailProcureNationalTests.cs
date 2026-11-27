using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1407) 線 c150rp 旗 RailProcureNational（線路と機関車の調達を同じ国の州の在庫へ広げる）の機構の番人。事前登録 measurements/m1407_20261127_prereg_rp.txt。
/// 盤面は試験の中で置く：内陸の州 A（線路 0・在庫 0・必要＞0・貯蓄あり）と、同じ国の州 B（在庫あり）。旗 0 では A の買った量が 0、旗 1 では正。
/// 期待の運賃は試験の中で書いた式（φ＝0 の両端の馬の地形の費用×LandSeaCostRatio・道の距離×½・FreightRatePer10Mm÷10,000km）で作る（エンジンの LandMul を呼ばない）。
/// </summary>
public class RailProcureNationalTests
{
    private static World Board(MultiNationSim sim)
    {
        sim.DiplomacyFrozen = true;
        sim.StepQuarter();   // 産出と値段が載った盤面
        foreach (var n in sim.Nations)
        {
            var c = n.World.Coef;
            c.RailByRegion = 1; c.RailNeedByOutput = 1;
            n.World.Discovered[Discovery.LocomotiveRow] = true;
        }
        return sim.Nations[0].World;
    }

    private static World France() => Board(new MultiNationSim([WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()));

    private static bool HasCoord(Region r) { var cc = Climate.CityCoord(r.Name); return cc.Lat != 0 || cc.Lon != 0; }

    private static double Km(Region a, Region b)
    {
        var ca = Climate.CityCoord(a.Name); var cb = Climate.CityCoord(b.Name);
        return PathGraph.LandDistanceKm(ca.Lat, ca.Lon, cb.Lat, cb.Lon);
    }

    /// <summary>A＝港でない・投資家あり・産出＞0・座標あり。</summary>
    private static Region PickA(World w)
    {
        foreach (var a in w.Regions)
            if (!a.IsPort && a.InvestorPopIndex >= 0 && HasCoord(a) && a.OutputRefValue() > 0) return a;
        throw new Xunit.Sdk.XunitException("前提：内陸の州が無い");
    }

    /// <summary>A と陸の道でつながる、A でない州（座標あり・POP あり・50km より遠い）を近い順に。</summary>
    private static List<Region> Linked(World w, Region a)
    {
        var l = new List<Region>();
        foreach (var b in w.Regions)
            if (!ReferenceEquals(a, b) && HasCoord(b) && b.Pops.Count > 0 && double.IsFinite(Km(a, b)) && Km(a, b) > 50) l.Add(b);
        l.Sort((x, y) => Km(a, x).CompareTo(Km(a, y)));
        Assert.True(l.Count >= 2, "前提：陸でつながる州が 2 つ未満（" + l.Count + "）");
        return l;
    }

    private static int[] RailGoods(Coefficients c) => [c.RailGoodId, c.LocoGoodId];

    /// <summary>国の全ての州の線路・機関車の在庫を 0 に・全ての州の線路を 0 に（φ＝0）・投資家の貯蓄を 0 に（自分の州で買えない）。</summary>
    private static void Clear(World w, Coefficients c)
    {
        foreach (var r in w.Regions)
        {
            foreach (int gi in RailGoods(c)) if (gi < r.Markets.Length) r.Markets[gi].Stock = 0;
            r.RailTrack = 0;
            if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) r.Pops[r.InvestorPopIndex].SetSavings(0);
        }
    }

    private static void Stock(Region b, Coefficients c, double qty, double price)
    {
        foreach (int gi in RailGoods(c)) { b.Markets[gi].Stock = qty; b.Markets[gi].Price = price; }
    }

    private static double HorseK(Coefficients c, Region x)
    {
        int ti = (int)x.Terrain;
        double h = ti < c.TerrainHorseCost.Length ? c.TerrainHorseCost[ti] : 1.0;
        if (x.Terrain == Terrain.WaterMajor && c.TerrainWaterMajorCost > 0) h = c.TerrainWaterMajorCost;
        else if (x.Terrain == Terrain.WaterMinor && c.TerrainWaterMinorCost > 0) h = c.TerrainWaterMinorCost;
        return h * c.LandSeaCostRatio;
    }

    private static double TotalMoney(World w)
    {
        double m = w.Treasury.Value;
        foreach (var p in w.AllPops()) m += p.Savings.Value;
        return m;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void T1_旗1で在庫の無い内陸の州は同じ国の他の州から線路を買い_旗0では買えない(int flag)
    {
        var w = France(); var c = w.Coef;
        c.RailProcureNational = flag;
        var a = PickA(w); var b = Linked(w, a)[0];
        Clear(w, c);
        Stock(b, c, 1e7, 1.0);
        a.Pops[a.InvestorPopIndex].SetSavings(1e9);
        Assert.True(Transport.Target(w, c) > 0, "前提：鉄道の目標が 0");
        double bStock0 = b.Markets[c.RailGoodId].Stock;
        Transport.Step(w, c);
        if (flag == 1)
        {
            Assert.True(a.RailTrack > 0, $"旗 1：{a.Name} の線路 {a.RailTrack}（{b.Name} に在庫 1e7）");
            Assert.True(b.Markets[c.RailGoodId].Stock < bStock0, "旗 1：売り手の州の在庫が減っていない");
        }
        else
        {
            Assert.Equal(0.0, a.RailTrack);
            Assert.Equal(bStock0, b.Markets[c.RailGoodId].Stock);   // 旗 0：B の投資家は貯蓄 0＝誰も B の在庫を買わない
        }
    }

    [Fact]
    public void T2_売り手の州の在庫は買った量だけ減り_払いは値段と運賃の和で_国の中のお金は保存する()
    {
        var w = France(); var c = w.Coef;
        c.RailProcureNational = 1;
        var a = PickA(w); var b = Linked(w, a)[0];
        Clear(w, c);
        Stock(b, c, 1e6, 2.0);
        var inv = a.Pops[a.InvestorPopIndex];
        inv.SetSavings(1e9);
        var basis = Transport.RailNeedBasis(w, c);
        double target = Transport.Target(w, c);
        double m0 = TotalMoney(w), s0 = inv.Savings.Value, st0 = b.Markets[c.RailGoodId].Stock, px = b.Markets[c.RailGoodId].Price;
        double got = Transport.ProcureNational(w, c, a, c.RailGoodId, 100.0, inv, target, basis);
        Assert.Equal(100.0, got, 9);
        Assert.Equal(st0 - got, b.Markets[c.RailGoodId].Stock, 6);   // 在庫の保存
        double fr = c.FreightRatePer10Mm * (Km(a, b) * 0.5 * (HorseK(c, a) + HorseK(c, b))) / 10_000.0;
        Assert.True(fr > 1e-4, "前提：運賃の率が小さすぎる " + fr);
        double paid = s0 - inv.Savings.Value;
        double want = got * px * (1.0 + fr);
        Assert.True(Math.Abs(paid - want) <= 1e-9 * want, $"払い {paid}・値段と運賃の和 {want}（運賃の率 {fr}）");
        double m1 = TotalMoney(w);
        Assert.True(Math.Abs(m1 - m0) <= 1e-9 * Math.Abs(m0), $"国の中のお金 {m0}→{m1}");
    }

    [Fact]
    public void T3_運賃つきの値段が一番安い州から買う()
    {
        var w = France(); var c = w.Coef;
        c.RailProcureNational = 1;
        var a = PickA(w); var l = Linked(w, a);
        var near = l[0]; var far = l[^1];
        Clear(w, c);
        Stock(near, c, 1e6, 3.0);
        Stock(far, c, 1e6, 1.0);   // 遠いが値段が安い
        double frNear = c.FreightRatePer10Mm * (Km(a, near) * 0.5 * (HorseK(c, a) + HorseK(c, near))) / 10_000.0;
        double frFar = c.FreightRatePer10Mm * (Km(a, far) * 0.5 * (HorseK(c, a) + HorseK(c, far))) / 10_000.0;
        bool farCheaper = 1.0 * (1 + frFar) < 3.0 * (1 + frNear);
        var inv = a.Pops[a.InvestorPopIndex]; inv.SetSavings(1e9);
        double got = Transport.ProcureNational(w, c, a, c.RailGoodId, 10.0, inv, Transport.Target(w, c), Transport.RailNeedBasis(w, c));
        Assert.True(got > 0);
        var cheap = farCheaper ? far : near; var dear = farCheaper ? near : far;
        Assert.Equal(1e6 - got, cheap.Markets[c.RailGoodId].Stock, 6);
        Assert.Equal(1e6, dear.Markets[c.RailGoodId].Stock);
    }

    [Fact]
    public void T4_国が違う州の在庫からは買わない()
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("プロイセン")], seed: 1789, coef: new Coefficients());
        var w = Board(sim); var c = w.Coef;
        c.RailProcureNational = 1;
        var other = sim.Nations[1].World;
        var a = PickA(w);
        Clear(w, c);
        Region? foreign = null;
        foreach (var r in other.Regions) if (HasCoord(r) && double.IsFinite(Km(a, r)) && r.Pops.Count > 0) { foreign = r; break; }
        Assert.NotNull(foreign);
        Stock(foreign!, c, 1e7, 1.0);
        int checkedN = 0;
        foreach (var r in other.Regions) if (r.InvestorPopIndex >= 0 && r.InvestorPopIndex < r.Pops.Count) { r.Pops[r.InvestorPopIndex].SetSavings(0); checkedN++; }
        Assert.True(checkedN > 0);
        a.Pops[a.InvestorPopIndex].SetSavings(1e9);
        Transport.Step(w, c);
        Assert.Equal(0.0, a.RailTrack);
        Assert.Equal(1e7, foreign!.Markets[c.RailGoodId].Stock);
        double got = Transport.ProcureNational(w, c, a, c.RailGoodId, 10.0, a.Pops[a.InvestorPopIndex], Transport.Target(w, c), Transport.RailNeedBasis(w, c));
        Assert.Equal(0.0, got);
    }
}
