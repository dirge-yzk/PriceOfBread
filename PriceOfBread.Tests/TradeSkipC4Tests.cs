using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1036) 線 c98lc：貿易の計器 C4a（売り手の〈その他の門〉の分け）・C4b（xPass の組の行の終わりの理由）・C4c（sellr_s）の番人。事前登録 measurements/m1036_20261111_prereg_lc.txt [3]。
/// 期待は紙で置く（計器が足した列の和を、旧の列・Market の欄から別に作る）。何もしない計器は 0 で緑になる＝全ての門に〈正の行が 1 つ以上ある〉対照を置く。
/// </summary>
[Collection("Instruments")]
public class TradeSkipC4Tests
{
    private const ulong Seed = 1789UL;
    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
        public string S(string[] r, string n) => r[I(n)];
    }
    private static Tab Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }
    private static (string dir, Action restore) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_c4_" + Guid.NewGuid().ToString("N"));
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

    /// <summary>C4a・C4b：分けた列の和が旧の列と全行で等しい・0 でない区分の数が下限以上（配線の対照）・全ての行の列数が見出しと同じ。</summary>
    [Fact]
    public void C4a_C4b_分けた列の和は旧の列と全行で等しく区分は配線されている()
    {
        var (dir, restore) = Run(24);
        try
        {
            var t = Read(dir, "tradeskip"); int w = t.H.Length;
            string[] og = ["sogStockLeTarget", "sogSellAcc0", "sogMilitary", "sogOther"];
            string[] xe = ["xpEndPriceHigh", "xpEndXPriceSmall", "xpEndXNeedSmall", "xpEndYZero", "xpEndBudget", "xpEndCapMid", "xpEndDealt", "xpEndGuard"];
            var ogTot = new double[og.Length]; var xeTot = new double[xe.Length]; int rows = 0;
            foreach (var r in t.R)
            {
                Assert.True(r.Length == w, "列数が見出しと違う（ALL の行の幅を含む）");
                if (t.S(r, "nation") == "ALL") continue;
                rows++;
                double ogSum = 0; for (int k = 0; k < og.Length; k++) { double v = t.D(r, og[k]); ogSum += v; ogTot[k] += v; }
                Assert.Equal(t.D(r, "sellerOtherGate"), ogSum);   // 和は旧の列（売り手の表に載らない生きた州）
                double xeSum = 0; for (int k = 0; k < xe.Length; k++) { double v = t.D(r, xe[k]); xeSum += v; xeTot[k] += v; }
                Assert.Equal(t.D(r, "xPass"), xeSum);             // 和は xPass
            }
            Assert.True(rows > 100, "対照：行が少ない");
            Assert.True(ogTot[0] > 0, "C4a：在庫≦目標の区分が 0＝配線されていない " + string.Join(",", ogTot));   // 4 国の小さな盤面では軍需・access 0 の州が無い＝他の区分は下の紙の試験で見る（事前登録の ≧2 は 10 種の生で読む）
            Assert.True(xeTot.Count(v => v > 0) >= 3, "C4b：0 でない区分が 3 未満＝配線されていない " + string.Join(",", xeTot));
            Assert.True(xeTot.Sum() > 0);
        }
        finally { restore(); }
    }

    /// <summary>C4a の計器側（紙の値）：売り手の表に載らない生きた州を、在庫≦目標・access 0・軍需・その他の順に排他で数える。</summary>
    [Fact]
    public void C4a_売り手の表に載らない州は4区分に排他で数えられる()
    {
        var a = new Instruments.TsAcc { Nn = 2, Gc = 1, Names = ["x", "y"], C = new int[2 * (int)Instruments.TsCol.Count], NoSell = new int[1], D = new double[6] };
        // 州 0〜5：0 載る・1 在庫≦目標・2 access 0・3 軍需（国 1 の係数 0）・4 その他（載らない理由が計器に見えない）・5 連結なし
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[6];
        for (int i = 0; i < 6; i++) tr[i] = (i == 3 ? 1 : 0, null!, 0, 0, 0, i == 2 ? 0.0 : 1.0, 1, i != 5, 0, 0, true);
        var mk = new Market[6]; var lp = new double[6];
        for (int i = 0; i < 6; i++) { mk[i] = new Market { Stock = 10, TargetStock = i == 1 ? 10 : 2 }; lp[i] = 1.0; }
        a.GoodBegin(1); a.AddSeller(0, 1.0);
        a.SellerSide(tr, 6, lp, mk, 0, true, [1.0, 0.0]);   // 軍需の財・国 1 の軍需の出し手係数は 0
        int T(Instruments.TsCol c, int nat) => a.At(nat, 0, c);
        Assert.Equal(1, T(Instruments.TsCol.OgStock, 0));
        Assert.Equal(1, T(Instruments.TsCol.OgAcc0, 0));
        Assert.Equal(1, T(Instruments.TsCol.OgMil, 1));
        Assert.Equal(1, T(Instruments.TsCol.OgOther, 0));
        Assert.Equal(1, T(Instruments.TsCol.SUnlinked, 0));
        // 和＝生きた州（連結あり・値段と在庫あり）−載る州
        int live = T(Instruments.TsCol.SLive, 0) + T(Instruments.TsCol.SLive, 1), listed = T(Instruments.TsCol.SListed, 0);
        int og = 0; foreach (int n in new[] { 0, 1 }) og += T(Instruments.TsCol.OgStock, n) + T(Instruments.TsCol.OgAcc0, n) + T(Instruments.TsCol.OgMil, n) + T(Instruments.TsCol.OgOther, n);
        Assert.Equal(live - listed, og);
    }

    /// <summary>C4b の理由の振り分け：行の終わりで X≦0 になったとき、理由が XPriceSmall なら xPrice≦xNeed・XNeedSmall なら xNeed＜xPrice（取り違えは赤）。取り違えは bad が正になる。</summary>
    [Fact]
    public void C4b_買い手が満ちた行の理由はxPriceとxNeedの小さい側と合う()
    {
        int nP = 0, nN = 0, bad = 0;
        Instruments.TsExitProbe = (xp, xn, why) =>
        {
            if (why == Instruments.TsExit.XPriceSmall) { nP++; if (!(xp <= xn)) bad++; }
            else if (why == Instruments.TsExit.XNeedSmall) { nN++; if (!(xn < xp)) bad++; }
        };
        var (dir, restore) = Run(24);
        try { Assert.Equal(0, bad); Assert.True(nP > 0, $"対照：xPrice が小さい理由が現れない xPriceSmall {nP}・xNeedSmall {nN}"); }   // xNeed が小さい理由は、行の門が xNeed＞0 を要し成約の後は Dealt が先に付く＝出口が access 0 の行だけ（0 でよい・件数は記録）
        finally { Instruments.TsExitProbe = null; restore(); }
    }

    /// <summary>C4b の計器側：1 本の行の xPass の組の数が、行の終わりの理由の列へ足される（紙の値）。</summary>
    [Fact]
    public void C4b_行の終わりの理由は組の数だけ該当の列へ足される()
    {
        var a = new Instruments.TsAcc { Nn = 1, Gc = 1, Names = ["x"], C = new int[(int)Instruments.TsCol.Count], NoSell = new int[1], D = new double[3] };
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[3];
        for (int i = 0; i < 3; i++) tr[i] = (i == 0 ? 0 : 1, null!, 0, 0, 0, 1, 1, true, 0, 0, true);   // 買い手＝国 0・売り手の州 1・2＝国 1（越境）
        a.GoodBegin(2); a.AddSeller(1, 1.0); a.AddSeller(2, 1.0);
        a.Row(0, 0);
        a.Scan(tr, 0, 2.0, [1.0, 1.0], [1.0, 1.0, 1.0], 1.0, 0.0, 0.0, null);   // 売り手の値段 1.0 ×（1＋0）＜ 2.0＝2 組とも楔を通る（xPass＝2）
        Assert.Equal(2, a.At(0, 0, Instruments.TsCol.XPass));
        a.Exit(Instruments.TsExit.XNeedSmall);
        Assert.Equal(2, a.At(0, 0, Instruments.TsCol.XeXNeed));
        Assert.Equal(0, a.At(0, 0, Instruments.TsCol.XeXPrice));
        Assert.Equal(0, a.At(0, 0, Instruments.TsCol.XeHigh));
    }

    /// <summary>C4c：sellROld＝max(LastDemand, 下限)・sellRBoth＝max(LastDemand＋PrevExport−PrevImport, 下限)。紙の値（符号の取り違えは赤）。</summary>
    [Theory]
    [InlineData(5.0, 3.0, 2.0, 1.0, 5.0, 6.0)]     // 輸出が輸入を上回る＝両側は旧より大きい
    [InlineData(5.0, 2.0, 3.0, 1.0, 5.0, 4.0)]     // 輸入が輸出を上回る＝両側は旧より小さい
    [InlineData(0.0, 0.0, 4.0, 1.0, 1.0, 1.0)]     // 下限
    public void C4c_sellRの旧と両側は紙の値(double demand, double exp, double imp, double floor, double expectOld, double expectBoth)
    {
        var m = new Market { LastDemand = demand, PrevExport = exp, PrevImport = imp };
        var (o, b) = Instruments.SellrOf(m, floor);
        Assert.Equal(expectOld, o); Assert.Equal(expectBoth, b);
    }

    /// <summary>C4c の表：見出し・行の式（列から紙で再計算）・両側が旧と違う行が正・旧の式×rentR が laborWeight に一致する行の割合は記録。</summary>
    [Fact]
    public void C4c_sellr表の行は列から再計算でき両側は旧と違う行がある()
    {
        var (dir, restore) = Run(24);
        try
        {
            var t = Read(dir, "sellr");
            Assert.Equal(Instruments.SellrHeaderText, string.Join('\t', t.H));
            Assert.True(t.R.Count > 1000, "対照：行が少ない " + t.R.Count);
            double floor = new Coefficients().TargetStockFloor; int diff = 0, lwMatch = 0, lwRows = 0;
            foreach (var r in t.R)
            {
                double d = t.D(r, "lastDemand"), e = t.D(r, "prevExportAll"), i = t.D(r, "prevImportAll");
                double old = d > floor ? d : floor, both = Math.Max(d + e - i, floor);
                Assert.Equal(old, t.D(r, "sellROld")); Assert.Equal(both, t.D(r, "sellRBoth"));
                if (old != both) diff++;
                double lw = t.D(r, "laborWeight"); double prod = old * t.D(r, "rentR");
                if (lw > 0 || prod > 0) { lwRows++; if (Math.Abs(lw - prod) <= 1e-6 * Math.Max(lw, prod)) lwMatch++; }
            }
            Assert.True(diff > 0, "何もしない式＝両側が旧と全行で同じ");
            Assert.True(lwRows > 0);
            System.Console.WriteLine($"C4c 記録：行 {t.R.Count}・両側≠旧 {diff}・旧の式×rentR＝laborWeight の割合 {lwMatch}/{lwRows}");
        }
        finally { restore(); }
    }
}
