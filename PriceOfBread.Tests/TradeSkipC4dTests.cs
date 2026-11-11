using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1045) 線 c99cd：貿易の計器 C4d（案 B-b の上界と旧と B-b の X）・C4a の区分の順（エンジンの門の順）・ガード終わりの反復の内訳・sellr_s の rentRMul・台帳の行 LaborPowerScans の番人。
/// 事前登録 measurements/m1045_20261111_prereg_cd.txt の [3]。期待は紙の値（試験が自分で書いた数）で置く＝エンジンの関数・計器の補助で期待を作らない。
/// 何もしない計器は 0 で緑になる＝紙の値の試験に加え、盤面の試験は〈正の行が 1 つ以上ある〉対照を置く。
/// </summary>
[Collection("Instruments")]
public class TradeSkipC4dTests
{
    private const ulong Seed = 1789UL;
    private const double Eps = 0.5;
    private static Instruments.TsAcc Acc(int nn = 1) => new() { Nn = nn, Gc = 1, Names = new string[nn], C = new int[nn * (int)Instruments.TsCol.Count], NoSell = new int[1], D = new double[nn * Instruments.TsAcc.DStride] };
    private static (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[] NoTr()
        => new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[0];
    private static Market Mk(double ts, double d, double p, double i, double s) => new Market { TargetStock = ts, LastDemand = d, LastProduced = p, LastImport = i, Stock = s };
    private static int At(Instruments.TsAcc a, Instruments.TsCol c) => a.At(0, 0, c);

    /// <summary>上界 TS＋D·(rpT/p)^ε−P−S：紙の値（ε の指数。2ε に取り違えると 2 行目が正に出る）。</summary>
    [Fact]
    public void C4d_上界は紙の値でイプシロン乗()
    {
        var a = Acc();
        a.GoodBegin(2); a.AddSeller(0, 1.0); a.AddSeller(1, 0.0);
        // rpT＝4・p＝1 → r＝4・r^ε＝2・r^2ε＝4。D＝100・TS＝10。
        a.Row(0, 0); a.SkipNeed(Mk(10, 100, 100, 0, 160), 2.0, 2.0, 1.0, 0, Eps);   // 10＋200−100−160＝−50（ε では負・2ε なら 10＋400−260＝150 で正）
        a.Row(0, 0); a.SkipNeed(Mk(10, 100, 100, 0, 60), 2.0, 2.0, 1.0, 0, Eps);    // 10＋200−160＝50＞0
        a.Row(0, 0); a.SkipNeed(Mk(10, 100, 100, 0, 60), 2.0, 2.0, 1.0, 1, Eps);    // 先頭の売り手の値段 0＝上界は ∞ と読む（ubInf）
        Assert.Equal(1, At(a, Instruments.TsCol.UbRows));
        Assert.Equal(1, At(a, Instruments.TsCol.UbInf));
        Assert.Equal(100.0, a.D[3]);   // ubD＝正の行の D の和
    }

    /// <summary>決まり直した反復の旧の X と B-b の X（紙の値）と、r≦1 かつ S−I＜TS の統制（X が一致する側）。</summary>
    [Fact]
    public void C4d_決まり直した反復の旧とBbのXは紙の値で統制は一致する()
    {
        var a = Acc();
        a.GoodBegin(0);   // Repick は売り手の表を読まない（Scan が売り手の表を引くので表は空）
        a.Row(0, 0); a.Scan(NoTr(), 0, 2.0, [], [1.0], 1.0, 0.0, 0.0, null);
        // r＝4（rpT＝4・unitA＝1）：xPrice＝100·4−20＝380・xNeed＝10＋100−20−15＝75・xNeedB＝10＋200−35＝175 → X 旧 75・B-b 175（access 1）
        a.Repick(Mk(10, 100, 20, 0, 15), 1.0, 2.0, 2.0, 1.0, 1.0, Eps);
        Assert.Equal(1, At(a, Instruments.TsCol.RpIter));
        Assert.Equal(1, At(a, Instruments.TsCol.RpDiff));
        Assert.Equal(0, At(a, Instruments.TsCol.RpCtrl));
        Assert.Equal(75.0, a.D[4]); Assert.Equal(175.0, a.D[5]);
        // r＝0.5（unitA＝8）・S＝5：xPrice＝100·0.5−20＝30・xNeed＝85・xNeedB＝10＋70.71−25＝55.7 → どちらも 30（統制の行・差 0）
        a.Repick(Mk(10, 100, 20, 0, 5), 1.0, 2.0, 2.0, 1.0, 8.0, Eps);
        Assert.Equal(2, At(a, Instruments.TsCol.RpIter));
        Assert.Equal(1, At(a, Instruments.TsCol.RpDiff));
        Assert.Equal(1, At(a, Instruments.TsCol.RpCtrl));
        Assert.Equal(0, At(a, Instruments.TsCol.RpCtrlDiff));
        Assert.Equal(75.0 + 30.0, a.D[4], 9); Assert.Equal(175.0 + 30.0, a.D[5], 9);
    }

    /// <summary>C4a の順はエンジンの門の順（出し手の係数〔access → 軍需〕→ 在庫−目標）。access 0 で在庫≦目標の州は access 0 に数える（旧の順なら在庫≦目標に数える）。</summary>
    [Fact]
    public void C4a_順はエンジンの門の順で出し手の係数が在庫より先()
    {
        var a = Acc(2);
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[3];
        // 州 0：載る（国 0）・州 1：access 0 かつ 在庫≦目標・州 2：軍需の係数 0（国 1）かつ 在庫≦目標
        for (int i = 0; i < 3; i++) tr[i] = (i == 2 ? 1 : 0, null!, 0, 0, 0, i == 1 ? 0.0 : 1.0, 1, true, 0, 0, true);
        var mk = new Market[3]; var lp = new double[3];
        for (int i = 0; i < 3; i++) { mk[i] = new Market { Stock = 10, TargetStock = i == 0 ? 2 : 10 }; lp[i] = 1.0; }
        a.GoodBegin(1); a.AddSeller(0, 1.0);
        a.SellerSide(tr, 3, lp, mk, 0, true, [1.0, 0.0]);
        Assert.Equal(1, a.At(0, 0, Instruments.TsCol.OgAcc0));
        Assert.Equal(0, a.At(0, 0, Instruments.TsCol.OgStock));
        Assert.Equal(1, a.At(1, 0, Instruments.TsCol.OgMil));
        Assert.Equal(0, a.At(1, 0, Instruments.TsCol.OgStock));
    }

    /// <summary>ガード終わりの行の反復の内訳：縛った項の分類（最小の項・同値は左）と、ガードで終わった行だけが gd 列へ足される（他の理由の行は足さない）。</summary>
    [Fact]
    public void ガード終わりの内訳は縛った項ごとに数えガードの行だけ足される()
    {
        var a = Acc();
        a.GoodBegin(0);
        a.Row(0, 0); a.Scan(NoTr(), 0, 2.0, [], [1.0], 1.0, 0.0, 0.0, null);
        a.Iter(1, 2, 3, 4, 5); a.Iter(5, 1, 3, 4, 5); a.Iter(5, 4, 1, 4, 5); a.Iter(5, 4, 3, 1, 5); a.Iter(5, 4, 3, 2, 1); a.Iter(5, 4, 3, 2, 1); a.IterRemove();
        a.Exit(Instruments.TsExit.Guard);
        Assert.Equal(1, At(a, Instruments.TsCol.GdRows));
        Assert.Equal(1, At(a, Instruments.TsCol.GdX)); Assert.Equal(1, At(a, Instruments.TsCol.GdY)); Assert.Equal(1, At(a, Instruments.TsCol.GdLeft));
        Assert.Equal(1, At(a, Instruments.TsCol.GdBudget)); Assert.Equal(2, At(a, Instruments.TsCol.GdCap)); Assert.Equal(1, At(a, Instruments.TsCol.GdRemove));
        a.Row(0, 0); a.Scan(NoTr(), 0, 2.0, [], [1.0], 1.0, 0.0, 0.0, null);
        a.Iter(1, 2, 3, 4, 5); a.Exit(Instruments.TsExit.Dealt);   // 別の理由の行は足さない・前の行の内訳は持ち越さない
        Assert.Equal(1, At(a, Instruments.TsCol.GdRows)); Assert.Equal(1, At(a, Instruments.TsCol.GdX));
    }

    private static (string dir, Action restore) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c4d_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 2;
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, Seed, "open"); sim.StartWarmup(); Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    /// <summary>盤面：新しい列が見出しにあり、全ての行の幅が見出しと同じ（ALL の行を含む）・配線の対照（旧と B-b の反復・上界の行が 0 でない）・統制の差 0・rentRMul＝max(rentR,0)。</summary>
    [Fact]
    public void C4d_盤面の新しい列は配線されていて統制の差は0でsellrの丸めは合う()
    {
        var (dir, restore) = Run(24);
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"tradeskip_s{Seed}.tsv"));
            var h = lines[0].Split('\t'); int I(string n) { int i = Array.IndexOf(h, n); Assert.True(i >= 0, "列が無い " + n); return i; }
            Assert.Equal("ubRows", h[Array.IndexOf(h, "xpEndGuard") + 1]);
            double rpIter = 0, rpDiff = 0, rpCtrl = 0, rpCtrlDiff = 0, ub = 0, ubInf = 0, ubD = 0, xo = 0, xb = 0;
            foreach (var l in lines.Skip(1))
            {
                var r = l.Split('\t'); Assert.Equal(h.Length, r.Length);
                if (r[I("nation")] == "ALL") continue;
                double D(string n) => double.Parse(r[I(n)], System.Globalization.CultureInfo.InvariantCulture);
                rpIter += D("rpIter"); rpDiff += D("rpDiff"); rpCtrl += D("rpCtrl"); rpCtrlDiff += D("rpCtrlDiff"); ub += D("ubRows"); ubInf += D("ubInf"); ubD += D("ubD"); xo += D("rpSumXOld"); xb += D("rpSumXBb");
            }
            Assert.True(rpIter > 0, "配線：決まり直した反復が 0");
            Assert.True(rpCtrl > 0, "対照：統制の行が 0");
            Assert.Equal(0.0, rpCtrlDiff);   // 紙：r≦1 かつ S−I＜TS の行で旧の X と B-b の X は一致
            Assert.True(ub + ubInf > 0, "配線：上界の行（正または ∞）が 0");
            Assert.True(ub == 0 || ubD > 0);
            Assert.True(xo >= 0 && xb >= 0);

            var sl = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"sellr_s{Seed}.tsv"));
            var sh = sl[0].Split('\t'); Assert.Equal("rentRMul", sh[^1]);
            int ri = Array.IndexOf(sh, "rentR"), mi = Array.IndexOf(sh, "rentRMul"); int neg = 0, n = 0;
            foreach (var l in sl.Skip(1))
            {
                var r = l.Split('\t'); Assert.Equal(sh.Length, r.Length); n++;
                double rent = double.Parse(r[ri], System.Globalization.CultureInfo.InvariantCulture), mul = double.Parse(r[mi], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(rent > 0 ? rent : 0.0, mul);
                if (!(rent > 0)) neg++;
            }
            Assert.True(n > 100, "対照：sellr の行が少ない");
            Assert.True(neg > 0, "対照：rentR が正でない行が現れない（丸めの検査が空振り）");
        }
        finally { restore(); }
    }

    [Fact]
    public void sellrの丸めは負とNaNを0にする()
    {
        Assert.Equal(0.0, Instruments.RentRMulOf(-3.0));
        Assert.Equal(0.0, Instruments.RentRMulOf(double.NaN));
        Assert.Equal(0.0, Instruments.RentRMulOf(0.0));
        Assert.Equal(2.5, Instruments.RentRMulOf(2.5));
    }

    /// <summary>台帳 LaborPowerScans：旗 0 は 0 件・旗 1 は 1 か月の州の生産者の数の和（紙の値＝州の生産者の数を試験が別に数える）。</summary>
    [Theory]
    [InlineData(ScenarioKind.France)]
    [InlineData(ScenarioKind.FullChain)]
    public void 台帳の労働の重みの走査は旗1で州の生産者の数だけ旗0で0(ScenarioKind kind)
    {
        long Cost(double flag, out int producers)
        {
            var c = new Coefficients { LaborWeightsByPower = flag };
            var s = new Simulation(new Scenario { Seed = 1, Kind = kind, Coef = c }, new NullAgent());
            producers = 0; foreach (var r in s.World.Regions) producers += r.Producers.Count;
            for (int d = 0; d < 30; d++) s.StepDay();
            return s.World.Cost[CostLedger.LaborPowerScans];
        }
        Assert.Equal(0L, Cost(0, out _));
        long n = Cost(1, out int total);
        Assert.True(total > 0);
        Assert.Equal((long)total, n);
    }
}
