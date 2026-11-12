using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1091) 線 c104si：支配の力の影の計器の足し分 S1〜S4 の番人。事前登録 measurements/m1091_20261113_prereg_si.txt。
/// A1＝S1 rulepoweralloc_s の候補ごとの配りの和が rulepower_s の外国の力と州×四半期で一致・重みの式が列だけで成り立つ（期待は別の表の値と試験の中の式）。
/// A2＝S3 の丸め戻せる書式（R）が G10 の列と一致し、R と G10 で文字列の違う行がある。
/// A3＝S2 凍結を外した届き：凍った州は届きが上がり、凍っていない州は今の届きと同じ。
/// 突然変異（使い捨ての木）：S1 の rho を 2 回掛ける→A1・S3 の書式を G6 にする→A2。
/// </summary>
[Collection("Instruments")]
public class RulePowerAllocTests
{
    private const ulong Seed = 1789UL;

    private static string Run(string sel, int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_rpa_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables(sel);
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); }
        }
        finally { Instruments.Close(sim); }
        return dir;
    }

    private static void Reset() { Instruments.SelectTables(null); Instruments.Configure(null, null); }
    private static double D(string s) => s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    private static (string[] H, List<string[]> R) Read(string dir, string name)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{name}{Seed}.tsv"));
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    [Fact]
    public void A1_配りの和は外国の力に一致し_重みの式は列だけで成り立つ()
    {
        try
        {
            string dir = Run("rulepower_s,rulepoweralloc_s", 3);
            var (ha, ra) = Read(dir, "rulepoweralloc_s");
            Assert.Equal(Instruments.RpAllocHeaderText.Split('\t'), ha);
            int C(string c) { int i = Array.IndexOf(ha, c); Assert.True(i >= 0, c); return i; }
            var sum = new Dictionary<(string q, string reg), double>();
            int ctl = 0, wgtChecked = 0; var quarters = new HashSet<string>();
            foreach (var x in ra)
            {
                Assert.Equal(ha.Length, x.Length); quarters.Add(x[C("quarter")]);
                double y = D(x[C("yIndex")]), rho = D(x[C("rhoF")]), s = D(x[C("sShare")]), b = D(x[C("bShare")]), wgt = D(x[C("wgtIndex")]), al = D(x[C("allocIndex")]), f = D(x[C("fForeignIndex")]);
                double want = Math.Max(0, y * rho * (1 - Math.Min(1.0, s)) * (1 - b));
                Assert.True(Math.Abs(want - wgt) <= 1e-12 * Math.Max(1.0, Math.Abs(want)), $"重み {wgt} と式 {want}");
                wgtChecked++;
                var k = (x[C("quarter")], x[C("region")]);
                sum[k] = (sum.TryGetValue(k, out var v) ? v : 0) + f;
                if (al > 0 && rho < 1.0) ctl++;
                Assert.True(Math.Abs(f - (al > 0 ? al * rho : 0)) <= 1e-12 * Math.Max(1.0, f), "F_f＝配り×rho");
            }
            Assert.Equal(["0"], quarters.ToArray());   // 四半期 0 と 40 おきだけ（3 四半期の走りでは 0 だけ）
            Assert.True(ctl > 0, "対照：配りが正で rho<1 の行が無い");
            Assert.True(wgtChecked > 5, "重みを調べた行 " + wgtChecked);
            var (hp, rp) = Read(dir, "rulepower_s");
            int cq = Array.IndexOf(hp, "quarter"), cr = Array.IndexOf(hp, "region"), cf = Array.IndexOf(hp, "fForeignSumIndex");
            int matched = 0, positive = 0;
            foreach (var x in rp)
            {
                if (x[cq] != "0") continue;
                double fs = D(x[cf]);
                if (!sum.TryGetValue(("0", x[cr]), out var got)) { Assert.True(fs == 0 || double.IsNaN(fs), $"配りの表に無い州 {x[cr]} の外国の力 {fs}"); continue; }
                Assert.True(Math.Abs(got - fs) <= 1e-9 * Math.Max(Math.Abs(fs), 1e-300) + 1e-300, $"{x[cr]}: 配りの和 {got} と外国の力 {fs}");
                matched++; if (fs > 0) positive++;
            }
            Assert.True(matched > 5 && positive > 0, $"突き合わせた州 {matched}・正の州 {positive}");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void A2_丸め戻せる書式はG10の列と一致し_文字列の違う行がある()
    {
        try
        {
            string dir = Run("rulepower_s", 3);
            var (h, rows) = Read(dir, "rulepower_s");
            Assert.Equal(Instruments.RulePowerHeaderText.Split('\t'), h);
            int C(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, c); return i; }
            int differ = 0, n = 0;
            foreach (var (rc, gc) in new[] { ("ownerSideShareR", "ownerSideShare"), ("ungovSideShareR", "ungovSideShare") })
                foreach (var x in rows)
                {
                    Assert.Equal(h.Length, x.Length);
                    string r = x[C(rc)], g = x[C(gc)];
                    if (r == "nan") { Assert.Equal("nan", g); continue; }
                    double v = double.Parse(r, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.Equal(r, v.ToString("R", System.Globalization.CultureInfo.InvariantCulture));   // 丸め戻せる
                    Assert.True(Math.Abs(v - D(g)) <= 1e-9 * Math.Max(Math.Abs(v), 1e-300), $"{rc} {r} と {g}");
                    n++; if (r != g) differ++;
                }
            Assert.True(n > 100, "調べた値 " + n);
            Assert.True(differ > 0, "対照：R と G10 で文字列の違う値が 1 つも無い＝R の細かさを見ていない");
            // S4 の列：持ち主の重みは Rule[0]＋Rule[1]×i×κ なので 0 以上・i の試しは 0〜1
            foreach (var x in rows)
            {
                double i = D(x[C("iProbeShare")]); Assert.True(i >= 0 && i <= 1.0 + 1e-9, "i の試し " + i);
                Assert.True(D(x[C("ownerWeightShare")]) >= 0);
            }
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }

    [Fact]
    public void A3_凍結を外した届きは凍った州で上がり_凍っていない州では今と同じ()
    {
        try
        {
            string dir = Run("rulepower_s", 3);
            var (h, rows) = Read(dir, "rulepower_s");
            int C(string c) { int i = Array.IndexOf(h, c); Assert.True(i >= 0, c); return i; }
            int rho = C("rhoNewIndex"), nf = C("rhoNoFreezeIndex"), tau = C("tauNoFreezeIndex");
            int same = 0, up = 0;
            foreach (var x in rows)
            {
                double a = D(x[rho]), b = D(x[nf]);
                Assert.True(b >= a - 1e-12, $"凍結を外すと届きは下がらない {a} → {b}");
                if (b == a) same++; else up++;
                double t = D(x[tau]); Assert.True(double.IsFinite(t) && t >= 0, "tauNoFreeze " + x[tau]);
            }
            Assert.True(same > 100, "対照：同じ州 " + same);
            Assert.True(up > 0, "対照：凍結を外して届きが上がる州が 1 つも無い＝凍結の印を読んでいない");
            try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { }
        }
        finally { Reset(); }
    }
}
