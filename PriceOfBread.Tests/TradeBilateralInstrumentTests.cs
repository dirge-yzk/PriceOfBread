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

    // (1139) T1 は軌道に依らない不変条件だけ。〈区分ごとに 1 件以上〉は Run(12) の軌道に依る下限だった（lo の旗 1 で ps1 が 0 件＝measurements/m1136_20261114/guards_f82bd18ac.txt）
    // ので T1b の手で置いた 6 盤面へ移した（★(743) ①・下限の置き場の移し＝番人を強める側）。
    [Fact]
    public void T1_6区分の和と行の数()
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
                // A-1 が閉じる行（B2 かつ X_old＞0）は 3 区分の下半分＝ps2 そのもの（区分の和の関係・軌道に依らない）
                Assert.True((long)t.D(r, "a1Close") == (long)t.D(r, "ps2"), $"a1Close {t.D(r, "a1Close")} ≠ ps2 {t.D(r, "ps2")}（{r[t.I("nation")]} {r[t.I("year")]}）");
                Assert.True(t.D(r, "a1CloseD") >= 0, "a1CloseD が負");
                rows += (long)t.D(r, "rows"); a1 += (long)t.D(r, "a1Close");
            }
            Assert.True(rows > 1000, "見た行が少ない " + rows);
        }
        finally { restore(); }
    }

    /// <summary>
    /// (1139) 6 区分ごとに 1 つ、盤面を試験の中で置いて TbClassify の区分を確かめる（軌道に依らない）。
    /// 添字＝(X_old＞0 なら 3) ＋ (B0:0・B1:1・B2:2)。盤面は式から作り、TbClassify の戻りは期待と照らすだけ。lp＝1・headPrice＝NaN → u_min＝1。
    /// </summary>
    [Fact]
    public void T1b_手で置いた6盤面が各区分に入る()
    {
        var (dir, sim, restore) = Run(4);
        try
        {
            double e = sim.Nations[0].World.Coef.PriceElasticity; Assert.True(e > 0, "弾力が正の前提");
            double rBig = System.Math.Pow(1e6, e), rSm = System.Math.Pow(0.25, e);
            Assert.True(rBig > 10 && rSm < 1, $"盤面の前提 {rBig}/{rSm}");
            // 盤面は 1 つの州×財の上に置く。P̂ は在庫の欄に依るので、欄を置いてから P̂ を測り、前提（式）を試験の中で確かめる。
            // sk1・ps1 だけは P̂ に合わせて D を探す（S＝TS にして在庫の調整を中立にする）。
            Region? RG = null; World? W = null; int G = -1; Market? M = null; Coefficients? C = null;
            double Ph(double ts, double d, double p, double s) { M!.TargetStock = ts; M.LastDemand = d; M.LastProduced = p; M.Stock = s; return Instruments.TbPHatSum(RG!, G, C!, W!, 1.0); }
            int Cls(double ts, double d, double p, double s, double rpT)
            {
                M!.TargetStock = ts; M.LastDemand = d; M.LastProduced = p; M.Stock = s;
                var x = Instruments.TbClassify(RG!, G, M, C!, W!, 1.0, rpT, double.NaN, 1.0);
                Assert.True(x.Ok, "区分に入らない盤面");
                int n = (x.B0 ? 1 : 0) + (x.B1 ? 1 : 0) + (x.B2 ? 1 : 0); Assert.Equal(1, n);   // 互いに素
                return (x.XOld > 0 ? 3 : 0) + (x.B0 ? 0 : x.B1 ? 1 : 2);
            }
            bool sk1 = false, ps1 = false;
            foreach (var (w, rg, g) in Boards(sim).Take(400))
            {
                RG = rg; W = w; G = g; M = rg.Markets[g]; C = w.Coef;
                const double T = 1000;
                foreach (double d in new[] { 1.0, 10, 100, 1e3, 1e4, 1e5, 1e6 })
                {
                    if (!sk1)
                    {   // UB_Bb＝−（P̂＋1）≦0・p を P̂ に置き換えると UB_A1＝D−P̂＞0
                        double p = Ph(T, d, 0, T) + d + 1; double ph = Ph(T, d, p, T);
                        if (p > ph && d > ph) { Assert.Equal(1, Cls(T, d, p, T, 1.0)); sk1 = true; }
                    }
                    if (!ps1)
                    {   // X_old＝δ＞0・UB_Bb＝−δ≦0（rpT＜u_min）・P̂＜D√r で UB_A1＞0
                        double p = d * (1 + rSm) / 2; double ph = Ph(T, d, p, T);
                        if (ph < p && ph < d * rSm) { Assert.Equal(4, Cls(T, d, p, T, 0.25)); ps1 = true; }
                    }
                }
                if (sk1 && ps1) break;
            }
            Assert.True(sk1, "sk1 の盤面が置けない"); Assert.True(ps1, "ps1 の盤面が置けない");
            {
                var (w, rg, g) = Boards(sim).First(); RG = rg; W = w; G = g; M = rg.Markets[g]; C = w.Coef;
                double dl = 2.0, delta = 0.5 * dl * (1 - rSm);
                // sk0：X_old＝−0.5＜0 だが rpT が大きく UB_Bb＞0
                Assert.Equal(0, Cls(1, 1, 2.5, 0, 1e6));
                // sk2：境（D＝0・p＝0・S＝TS ちょうど）
                Assert.Equal(2, Cls(5, 0, 0, 5, 1.0));
                // ps0：X_old＝2＞0 で UB_Bb＞X_old
                Assert.Equal(3, Cls(1, 1, 0, 0, 1e6));
                // ps2：X_old＝δ＞0・p＝0 で UB_A1＝UB_Bb＝−δ≦0
                Assert.Equal(5, Cls(5, dl, 0, 5 + dl - delta, 0.25));
            }
        }
        finally { restore(); }
    }

    /// <summary>
    /// (1139) NaN・無限の入力の区分を現状のまま固定する。rpT・lp が NaN／無限なら Ok＝false（表の unclassified）。
    /// 在庫の欄が NaN だと Ok＝true のまま 3 区分が全て偽になる＝TbRow は行を数えるのにどの区分にも入れない隙間
    /// （表の和が行の数と合わなくなる）。計器の変更が要るので、この線では直さず 00一覧へ申し送る。
    /// </summary>
    [Fact]
    public void T6_NaNの入力の区分()
    {
        var (dir, sim, restore) = Run(4);
        try
        {
            var (w, rg, g) = Boards(sim).First();
            var m = rg.Markets[g]; var c = w.Coef;
            m.TargetStock = 5; m.LastDemand = 1; m.LastProduced = 0; m.Stock = 5;
            double nan = double.NaN;
            Assert.False(Instruments.TbClassify(rg, g, m, c, w, 1.0, nan, nan, 1.0).Ok);
            Assert.False(Instruments.TbClassify(rg, g, m, c, w, nan, 1.0, nan, 1.0).Ok);
            Assert.False(Instruments.TbClassify(rg, g, m, c, w, double.PositiveInfinity, 1.0, nan, 1.0).Ok);
            Assert.True(Instruments.TbClassify(rg, g, m, c, w, 1.0, 1.0, nan, nan).Ok, "headPrice が NaN なら最安の売り手は無いものとして読む");
            foreach (var field in new[] { "Stock", "LastDemand", "TargetStock", "LastProduced" })
            {
                m.TargetStock = 5; m.LastDemand = 1; m.LastProduced = 0; m.Stock = 5;
                switch (field) { case "Stock": m.Stock = nan; break; case "LastDemand": m.LastDemand = nan; break; case "TargetStock": m.TargetStock = nan; break; default: m.LastProduced = nan; break; }
                var x = Instruments.TbClassify(rg, g, m, c, w, 1.0, 1.0, nan, 1.0);
                Assert.True(x.Ok && !x.B0 && !x.B1 && !x.B2 && !x.Eval, $"{field}＝NaN の区分 {x.Ok}/{x.B0}/{x.B1}/{x.B2}/{x.Eval}");
            }
        }
        finally { restore(); }
    }

    /// <summary>
    /// (1139) TbPHatSum の倒産の枝（エンジンは倒産・眠りの生産者を飛ばす＝今の産出 p.Output のまま足す）。
    /// 期待値は SupplyTargetAt から作る。眠りの枝（SleepState＝1）は盤面に置けない＝この試験の限界（倒産と同じ行にある）。
    /// </summary>
    [Fact]
    public void T7_倒産した生産者はOutputのまま足す()
    {
        var (dir, sim, restore) = Run(4);
        try
        {
            int tested = 0;
            foreach (var (w, rg, g) in Boards(sim).Take(400))
            {
                var m = rg.Markets[g]; var c = w.Coef;
                var ps = rg.Producers.Where(p => p.GoodId == g).ToList();
                double One(Producer p)
                {
                    bool isLabor = p.GoodId is Catalog.LaborId or Catalog.FarmLaborId;
                    bool baumol = Catalog.Goods[p.GoodId].Tier == GoodTier.Service && !isLabor;
                    return EconomyStep.SupplyTargetAt(p, rg, c, w, baumol, Discovery.Gate(rg, p), out _);
                }
                var victim = ps[0];
                double full = One(victim);
                if (System.Math.Abs(full - victim.Output) <= 1e-9 * System.Math.Max(1.0, full)) continue;   // 倒産の枝と通常の枝が同じ値になる盤面は区別できない
                double expect = ps.Where(p => p != victim).Sum(One) + victim.Output;
                victim.Bankrupt = true;
                double got;
                try { got = Instruments.TbPHatSum(rg, g, c, w, m.Price); } finally { victim.Bankrupt = false; }
                Assert.True(Near(got, expect, 1e-9), $"倒産の枝 {got} ≠ {expect}");
                tested++;
            }
            Assert.True(tested >= 5, "区別できる盤面が少ない " + tested);
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
                // 注：TbU1 の u′＝min(unitA, lp) は成約の価格。行の門の TbClassify の u_min＝min(lp, 最安の売り手×lbMulMin) は headS の時点の近似で、
                // 同じ値ではない（成約が headS 以外の売り手へ回る行では u′≠u_min）。この試験は TbU1 の式だけを固定する。
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
