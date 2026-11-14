using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1131) 線 c110tb：貿易の置き換え T0b の計器 <c>tb_s</c>（U1＝成約ごとの上界の越え・S2′＝行の門の 6 区分）の自己検査。事前登録 measurements/m1131_20261114_prereg_tb.txt。
/// 何もしない計器は 0 行で緑になる＝全ての検査に〈調べた行の数の下限〉を置く。期待値は試験の中で式から作り、計器の合計の列を鵜呑みにしない。
/// </summary>
[Collection("Instruments")]
public class TradeBilateralInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain()];

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    }
    private static Tab Read(string dir)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"tb_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }
    private static bool Near(double a, double b, double tol) => System.Math.Abs(a - b) <= tol * System.Math.Max(1.0, System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)));

    private static (string dir, MultiNationSim sim, Action restore) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tb_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        var sim = new MultiNationSim(Configs(), Seed, _ => new UtilityAgent());
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, Seed, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, sim, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    /// <summary>生産者が 1 本以上あり、眠りも倒産も無い州×財（手で作る盤面の土台）。</summary>
    private static IEnumerable<(World W, Region R, int G)> Boards(MultiNationSim sim)
    {
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions)
        {
            var byGood = r.Producers.Where(p => p.GoodId >= 0 && p.GoodId < r.Markets.Length).GroupBy(p => p.GoodId);
            foreach (var grp in byGood)
                if (grp.All(p => !p.Bankrupt && EconomyStep.SleepState(r, p, n.World.Coef) != 1 && p.Capacity > 0))
                    yield return (n.World, r, grp.Key);
        }
    }

    private static double EngineSum(World w, Region r, int g)
    {
        double s = 0; var c = w.Coef;
        foreach (var p in r.Producers.Where(p => p.GoodId == g))
        {
            bool isLabor = p.GoodId is Catalog.LaborId or Catalog.FarmLaborId;
            bool baumol = Catalog.Goods[p.GoodId].Tier == GoodTier.Service && !isLabor;
            s += EconomyStep.SupplyTargetAt(p, r, c, w, baumol, Discovery.Gate(r, p), out _);
        }
        return s;
    }

    [Fact]
    public void T1_6区分の和と境の盤面()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            var t = Read(dir);
            long rows = 0; var tot = new long[6]; long a1 = 0;
            string[] cn = ["sk0", "sk1", "sk2", "ps0", "ps1", "ps2"];
            foreach (var r in t.R)
            {
                long sum = (long)t.D(r, "unclassified"); for (int i = 0; i < 6; i++) { long v = (long)t.D(r, cn[i]); sum += v; tot[i] += v; }
                Assert.True(sum == (long)t.D(r, "rows"), $"6 区分の和 {sum} ≠ 行の数 {t.D(r, "rows")}（{r[t.I("nation")]} {r[t.I("year")]}）");
                rows += (long)t.D(r, "rows"); a1 += (long)t.D(r, "a1Close");
            }
            Assert.True(rows > 1000, "見た行が少ない " + rows);
            for (int i = 0; i < 6; i++) Assert.True(tot[i] > 0, $"区分 {cn[i]} が 0 件（{string.Join(',', tot)}）");
            Assert.True(a1 > 0, "A-1 が閉じる行が 0");
            // 境の盤面：D＝0・P＝0・S＝TS＝UB_Bb＝UB_A1＝0 ちょうど → 区分 2（互いに素な 3 述語のどれにも漏れない）。
            var (w, rg, g) = Boards(sim).First();
            var m = rg.Markets[g]; var c = w.Coef;
            m.TargetStock = 5; m.Stock = 5; m.LastDemand = 0; m.LastProduced = 0;
            var x = Instruments.TbClassify(rg, g, m, c, w, 1.0, 1.0, double.NaN, 1.0);
            Assert.True(x.Ok && !x.B0 && !x.B1 && x.B2, $"境の盤面が区分 2 でない {x.B0}/{x.B1}/{x.B2}");
            Assert.Equal(0.0, x.UbBb); Assert.Equal(0.0, x.UbA1);
        }
        finally { restore(); }
    }

    [Fact]
    public void T2_U1のEは成約の後の台帳のSで式と一致()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            int boards = 0, viaP = 0, viaHat = 0;
            foreach (var (w, rg, g) in Boards(sim).Take(60))
            {
                var m = rg.Markets[g]; var c = w.Coef;
                double lp = 2.0, unitA = 3.0, rpT = 2.5, qA = 2.0;     // u′＝min(unitA, lp)＝2
                double u = System.Math.Min(unitA, lp), root = System.Math.Sqrt(rpT / u);
                m.TargetStock = 10; m.LastDemand = 4; m.Stock = 7;   // 在庫の調整が読む欄を先に決める（S は成約の後の台帳の値）
                double price0 = m.Price; m.Price = u; double ph = EngineSum(w, rg, g); m.Price = price0;
                if (!(ph > 0)) continue;
                foreach (double pFac in new[] { 0.5, 2.0 })
                {
                    m.TargetStock = 10; m.LastDemand = 4; m.LastProduced = ph * pFac; m.Stock = 7;   // S は成約の後の台帳の値
                    double q = System.Math.Min(m.LastProduced, ph), ub = 10 + 4 * root - q, eAbs = 7 - ub, eRel = eAbs / System.Math.Max(4, 10);
                    var got = Instruments.TbU1(rg, g, m, c, w, lp, unitA, rpT, qA);
                    Assert.True(got.Ok && Near(got.EAbs, eAbs, 1e-9) && Near(got.ERel, eRel, 1e-9), $"E {got.EAbs}/{got.ERel} ≠ 式 {eAbs}/{eRel}");
                    if (pFac < 1) viaP++; else viaHat++;
                }
                boards++;
            }
            Assert.True(boards >= 10 && viaP >= 10 && viaHat >= 10, $"盤面が少ない {boards}/{viaP}/{viaHat}");
        }
        finally { restore(); }
    }

    [Fact]
    public void T3_Dの小さい行の分母はmax_D_TS()
    {
        var (dir, sim, restore) = Run(4);
        try
        {
            var (w, rg, g) = Boards(sim).First();
            var m = rg.Markets[g]; var c = w.Coef;
            Assert.True(0.1 < c.TargetStockFloor, "D＜床の前提");
            m.TargetStock = 1.0; m.LastDemand = 0.1; m.LastProduced = 0; m.Stock = 5;
            var got = Instruments.TbU1(rg, g, m, c, w, 1.0, 1.0, 1.0, 1.0);   // rpT＝u′ → √r＝1 → UB＝TS＋D＝1.1
            Assert.True(got.Ok && Near(got.EAbs, 5 - 1.1, 1e-12), "E_abs " + got.EAbs);
            Assert.True(Near(got.ERel, (5 - 1.1) / 1.0, 1e-12), $"E_rel {got.ERel}（分母は max(D, TS)＝1・D だけなら 39）");
        }
        finally { restore(); }
    }

    [Fact]
    public void T4_P_hatの値段の差し替えはエンジンの値段を書き換えた値と合う()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            int n = 0, moved = 0;
            foreach (var (w, rg, g) in Boards(sim).Take(400))
            {
                var m = rg.Markets[g]; double price0 = m.Price;
                double u = price0 * (0.5 + (n % 5) * 0.25);
                double hat = Instruments.TbPHatSum(rg, g, w.Coef, w, u), hat0 = Instruments.TbPHatSum(rg, g, w.Coef, w, price0);
                m.Price = u; double eng = EngineSum(w, rg, g); m.Price = price0;
                Assert.True(Near(hat, eng, 1e-9), $"P̂(u) {hat} ≠ エンジン {eng}");
                if (!Near(hat, hat0, 1e-12)) moved++;
                n++;
            }
            Assert.True(n >= 100 && moved >= 20, $"盤面が少ない {n}/{moved}");
        }
        finally { restore(); }
    }

    [Fact]
    public void T5_表の形と成約の数()
    {
        var (dir, sim, restore) = Run(12);
        try
        {
            var t = Read(dir);
            long deals = 0, rel = 0; double imp = 0, rex = 0;
            foreach (var r in t.R)
            {
                Assert.Equal(t.H.Length, r.Length);
                foreach (var h in t.H) if (h != "u1MaxRel" && h != "nation") Assert.False(double.IsNaN(t.D(r, h)), "nan の欄 " + h);
                Assert.True(t.D(r, "evalDone") <= t.D(r, "evalAll") && t.D(r, "evalSkip") <= t.D(r, "evalAll"), "評価の件数の順");
                Assert.True(t.D(r, "u1Rel") <= t.D(r, "u1Deals") && t.D(r, "u1SmallDRel") <= t.D(r, "u1SmallD"), "成約の数の順");
                deals += (long)t.D(r, "u1Deals"); rel += (long)t.D(r, "u1Rel"); imp += t.D(r, "impQ"); rex += t.D(r, "rexQ");
            }
            Assert.True(t.R.Count >= 36, "行が少ない " + t.R.Count);
            Assert.True(deals > 100, "成約が少ない " + deals);
            Assert.True(imp > 0 && rex <= imp + 1e-9, $"越境の輸入と再輸出 {imp}/{rex}");
        }
        finally { restore(); }
    }
}
