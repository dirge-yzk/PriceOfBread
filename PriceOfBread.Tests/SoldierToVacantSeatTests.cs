using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1271) c130gs 旗 SoldierToVacantSeat（Military.MoveToVacantSeats）の機構の試験。事前登録 measurements/m1271_20261121_prereg_gs.txt。
/// 盤面＝フランスの開幕（1789）・志願兵制（徴募のループが走らない＝兵の総数の変化は移しだけ）・平時・定員は人口の側（MilitaryEnvelopeShare＝1e9）。
/// 期待の値は試験の中で書いた式（席＝国の定員×州の兵役の母数の比・移す量＝min(率×Σ超過, Σ空席)・受け手は空席の比）から作る＝MoveToVacantSeats を呼ばない。</summary>
public class SoldierToVacantSeatTests
{
    static (World W, List<Region> Rs) Board(double flag)
    {
        var sim = new Simulation(OpeningFixture.Named("フランス").ToScenario(1789), new NullAgent());
        var w = sim.World;
        w.Coef.MilitaryEnvelopeShare = new Share(1e9);
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.Coef.SoldierToVacantSeat = flag;
        var rs = new List<Region>();
        foreach (var r in w.Regions)
            if (Soldier(r) is not null && Military.RegionManpower(r, w.Coef) > 0) rs.Add(r);
        Assert.True(rs.Count >= 3, $"前提が崩れた：兵の POP と兵役の母数を持つ州が {rs.Count}");
        return (w, rs);
    }

    static Pop? Soldier(Region r) { foreach (var p in r.Pops) if (p.IsSoldier) return p; return null; }

    static double Seat(World w, Region r)
    {
        double nat = Military.EstablishmentOf(w, w.Coef, out double natMan);
        return nat * (Military.RegionManpower(r, w.Coef) / natMan);
    }

    static double Total(World w) { double s = 0; foreach (var r in w.Regions) s += Soldier(r)?.Size ?? 0; return s; }

    /// <summary>2 州の型：A の兵が席の 3 倍・B の席が空 → A から兵が出て B へ入り、兵の総数は保存。B は席を超えない。</summary>
    [Fact]
    public void T1_席を超える州から空席の州へ兵が動き総数は保存()
    {
        var (w, rs) = Board(1);
        Region a = rs[0], b = rs[1];
        Soldier(a)!.Size = 3 * Seat(w, a);
        Soldier(b)!.Size = 0;
        double seatB = Seat(w, b), a0 = Soldier(a)!.Size, t0 = Total(w);
        Assert.True(seatB > 0 && a0 > 0, "前提が崩れた：席が 0");
        Military.Step(w, w.Coef, atWar: false);
        double a1 = Soldier(a)!.Size, b1 = Soldier(b)!.Size, t1 = Total(w);
        Assert.True(a1 < a0, $"席の 3 倍の州から兵が出ない（{a0} → {a1}）");
        Assert.True(b1 > 0, $"空席の州に兵が入らない（0 → {b1}）");
        Assert.True(b1 <= seatB * (1 + 1e-9), $"空席の州が席を超えた（{b1} > {seatB}）");
        Assert.True(Math.Abs(t1 - t0) <= 1e-9 * t0, $"兵の総数が保存されない（{t0} → {t1}）");
    }

    /// <summary>量の型：A だけに兵を置く → 出る量＝min(率×(兵−席), Σ他の席)・受け手の増分÷席は全ての受け手で同じ（空席の比）。</summary>
    [Fact]
    public void T2_移す量は率と超過で決まり受け手は空席の比で受ける()
    {
        var (w, rs) = Board(1);
        Region a = rs[0];
        foreach (var r in w.Regions) if (Soldier(r) is { } s) s.Size = 0;
        Soldier(a)!.Size = 3 * Seat(w, a);
        var seat = new Dictionary<Region, double>();
        foreach (var r in w.Regions) if (Soldier(r) is not null) seat[r] = Seat(w, r);
        double over = Soldier(a)!.Size - seat[a], vacant = 0;
        foreach (var (r, v) in seat) if (!ReferenceEquals(r, a)) vacant += v;
        double expect = Math.Min(w.Coef.SoldierMigrationRatePerMonth.Value * over, vacant);
        Assert.True(over > 0 && expect > 0, "前提が崩れた：超過か空席が 0");
        double a0 = Soldier(a)!.Size;
        Military.Step(w, w.Coef, atWar: false);
        Assert.Equal(expect, a0 - Soldier(a)!.Size, expect * 1e-9);
        double ratio = expect / vacant; int receivers = 0;
        foreach (var (r, v) in seat)
        {
            if (ReferenceEquals(r, a) || !(v > 1e-9)) continue;
            receivers++;
            Assert.Equal(ratio, Soldier(r)!.Size / v, 1e-9);
        }
        Assert.True(receivers >= 2, $"受け手が {receivers}");
    }

    /// <summary>旗 0（旧の形）：同じ盤面で兵は 1 つの州からも動かない（志願兵制の平時＝徴募も無い）。</summary>
    [Fact]
    public void T3_旗0では兵は州の間を動かない()
    {
        var (w, rs) = Board(0);
        Region a = rs[0], b = rs[1];
        Soldier(a)!.Size = 3 * Seat(w, a);
        Soldier(b)!.Size = 0;
        var before = new Dictionary<Region, double>();
        foreach (var r in w.Regions) if (Soldier(r) is { } s) before[r] = s.Size;
        Military.Step(w, w.Coef, atWar: false);
        foreach (var (r, v) in before) Assert.Equal(v, Soldier(r)!.Size);
    }
}

/// <summary>(1271) c130gs の hook の番人：旗 1 の移しが popclose_s の garrisonOutK／InK に実際に移った量で載り、州の残差が閉じる（hook を外す突然変異で赤）。
/// 盤面＝英仏の 2 国・フランス（志願兵制・平時・定員は人口の側）で 1 つの州だけに兵を置き、Military.Step を 1 回。移した量は人口の差で独立に読む。</summary>
[Collection("Instruments")]
public class SoldierToVacantSeatHookTests
{
    private const ulong Seed = 1789UL;

    [Fact]
    public void T4_兵の移しが州の残差を閉じる()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();   // 歳入の実績を作る（定員＝払える予算の側が 0 にならないように）
        var w = sim.Nations[1].World;
        w.Coef.MilitaryEnvelopeShare = new Share(1e9);
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
        w.Coef.SoldierToVacantSeat = 1;
        Region? a = null;
        foreach (var r in w.Regions)
        {
            var s = r.Pops.Find(x => x.IsSoldier);
            if (s is null) continue;
            s.Size = 0;
            if (a is null && Military.RegionManpower(r, w.Coef) > 0) a = r;
        }
        Assert.NotNull(a);
        double nat = Military.EstablishmentOf(w, w.Coef, out double natMan);
        Assert.True(nat > 0 && natMan > 0, $"前提が崩れた：定員 {nat}・母数 {natMan}");
        a!.Pops.Find(x => x.IsSoldier)!.Size = 3 * nat * (Military.RegionManpower(a, w.Coef) / natMan);   // 席の 3 倍（席＝国の定員×州の兵役の母数の比）
        var pop0 = w.Regions.ToDictionary(r => r, r => r.SumPopulation());
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_gs_" + Guid.NewGuid().ToString("N"));
        Instruments.SelectTables("popclose_s");
        Instruments.Configure(dir, "*");
        string[] lines;
        try
        {
            Instruments.Phase(sim, Seed, "warm");
            Military.Step(w, w.Coef, atWar: false);
            Instruments.Quarter(sim, Seed, 0);
            Instruments.Close(sim);
            lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "popclose_s" + Seed + ".tsv"));
        }
        finally { Instruments.Configure(null, null); Instruments.SelectTables(null); }
        var h = lines[0].Split('\t');
        double Col(string[] row, string name) => double.Parse(row[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture);
        var rows = lines.Skip(1).Select(l => l.Split('\t')).Where(x => x[Array.IndexOf(h, "nation")] == "フランス").ToList();
        double outA = pop0[a] - a.SumPopulation();
        Assert.True(outA > 1.0, $"前提が崩れた：兵が動かない（{outA}）");
        var ra = rows.Single(x => x[Array.IndexOf(h, "region")] == a.Name);
        Assert.True(Math.Abs(outA - Col(ra, "garrisonOutK")) <= 1e-9 * Math.Max(1.0, Math.Abs(outA)), $"garrisonOutK {Col(ra, "garrisonOutK")} と人口の差 {outA}");   // 表は 10 桁の有効数字で丸めて書く
        double inSum = 0; int receivers = 0;
        foreach (var r in w.Regions)
        {
            if (ReferenceEquals(r, a)) continue;
            double gain = r.SumPopulation() - pop0[r];
            var row = rows.Single(x => x[Array.IndexOf(h, "region")] == r.Name);
            Assert.True(Math.Abs(gain - Col(row, "garrisonInK")) <= 1e-9 * Math.Max(1.0, Math.Abs(gain)), $"{r.Name} の garrisonInK {Col(row, "garrisonInK")} と人口の差 {gain}");
            Assert.True(Math.Abs(Col(row, "residualK")) < 1e-6, $"{r.Name} の残差 {Col(row, "residualK")}");
            if (gain > 1e-9) receivers++;
            inSum += Col(row, "garrisonInK");
        }
        Assert.True(receivers >= 2, $"受け手が {receivers}");
        Assert.Equal(outA, inSum, 6);   // 表の値は行ごとに丸めて書く＝行の和の許しは 1e-6
        Assert.True(Math.Abs(Col(ra, "residualK")) < 1e-6, $"送り手の残差 {Col(ra, "residualK")}");
    }
}
