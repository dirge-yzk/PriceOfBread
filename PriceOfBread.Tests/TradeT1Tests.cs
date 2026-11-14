using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1144) 線 c112t1：貿易の置き換え T1 の A-1〈清算の買いの来月の国内の見込みを着値で読む〉の機構の試験。事前登録 measurements/m1144_20261115_prereg_t1.txt。
/// M1＝A-1 の P̂ を供給①が書いた値段で読むと供給①の産出に戻る（写しの 3 値と Toward・床）。M2＝P̂ は値段に単調（行の門の上界が u_min で正しい根）。
/// M3＝写しは清算の後に 0 へ戻る（眠り・倒産の古い写しを読まない）。M4＝計器 t1buy_s で、旗 1 の成約は上界 TS＋D×(rpT÷u′)^ε−min(P, P̂(u′)) を越えない（U1＝0）・
///   エンジンと計器の P̂ と Q が一致・門の食い違い 0・W2(a) の母集合の越境の成約が正。旗 0 では W2(a) の母集合の成約は構成上 0。
/// 期待値は式から作る（計器は事前登録の式の書き直し・エンジンの A1PHat を呼ばない）。何もしない実装は 0 行で緑になる＝全ての検査に件数の下限を置く。
/// </summary>
[Collection("Instruments")]
public class TradeT1Tests
{
    private const ulong Seed = 1789UL;
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain()];
    private static bool Near(double a, double b, double tol) => System.Math.Abs(a - b) <= tol * System.Math.Max(1.0, System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)));

    private static MultiNationSim Make(double a1)
    {
        var sim = new MultiNationSim(Configs(), Seed, _ => new UtilityAgent());
        sim.DiplomacyFrozen = true;
        sim.Coef.TradeBuyBySupplyCurve = a1;
        foreach (var n in sim.Nations) n.World.Coef.TradeBuyBySupplyCurve = a1;
        return sim;
    }

    private static double PHatOne(Producer p, double price, Coefficients c)
    {
        long cnt = 0;
        return InternationalMarket.A1PHat([p], [0, 1], 0, price, c, ref cnt);
    }

    [Fact]
    public void M1_写しのPhatを供給の値段で読むと供給の産出に戻る()
    {
        var sim = Make(1.0);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        int rows = 0, partial = 0, uninv = 0, moved = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Producers) p.T1Base = p.T1OutPrev = p.T1Mc = 0;
                var outPrev = new Dictionary<Producer, double>();
                foreach (var p in r.Producers) outPrev[p] = p.Output;
                EconomyStep.Run(r, w.Coef, w.TechProductivity(r), w.Bank, w);
                foreach (var p in r.Producers)
                {
                    if (p.T1Base == 0 && p.T1OutPrev == 0 && p.T1Mc == 0) continue;   // 眠り・倒産＝供給①を通らない
                    if (p.GoodId < 0 || p.GoodId >= r.Markets.Length) continue;
                    double price = r.Markets[p.GoodId].Price;
                    double hat = PHatOne(p, price, w.Coef);
                    Assert.True(Near(hat, p.Output, 1e-12), $"P̂ {hat} ≠ 供給の産出 {p.Output}（{Catalog.Goods[p.GoodId].Name}・{r.Name}）");
                    Assert.Equal(outPrev[p], p.T1OutPrev);
                    rows++;
                    if (p.T1Base < 0) uninv++;
                    double pf = EconomyStep.ProfitFactor(p, price, p.T1Mc, w.Coef);
                    if (pf > 0 && pf < 1) partial++;
                    if (!Near(p.Output, p.T1OutPrev, 1e-9)) moved++;
                }
            }
        }
        Assert.True(rows > 1000, "調べた生産者が少ない " + rows);
        Assert.True(partial >= 20, "採算が境の生産者が少ない " + partial);
        Assert.True(moved >= 100, "産出が動いた生産者が少ない（Toward を通っていない）" + moved);
        Assert.True(uninv >= 10, "未発明の写しが少ない " + uninv);
    }

    [Fact]
    public void M2_Phatは値段に単調()
    {
        var sim = Make(1.0);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        int rows = 0, rose = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                EconomyStep.Run(r, w.Coef, w.TechProductivity(r), w.Bank, w);
                foreach (var p in r.Producers)
                {
                    if (p.T1Base == 0 && p.T1OutPrev == 0 && p.T1Mc == 0) continue;
                    if (p.GoodId < 0 || p.GoodId >= r.Markets.Length) continue;
                    double p0 = r.Markets[p.GoodId].Price, prev = double.NegativeInfinity;
                    foreach (double k in new[] { 0.0, 0.25, 0.5, 0.8, 1.0, 1.2, 1.5, 2.0, 4.0 })
                    {
                        double h = PHatOne(p, p0 * k, w.Coef);
                        Assert.True(h >= prev, $"P̂ が値段で下がった {prev}→{h}（{Catalog.Goods[p.GoodId].Name}・×{k}）");
                        if (h > prev && prev > double.NegativeInfinity) rose++;
                        prev = h;
                    }
                    rows++;
                }
            }
        }
        Assert.True(rows > 1000, "調べた生産者が少ない " + rows);
        Assert.True(rose >= 200, "値段で P̂ が上がった段が少ない（値段を読んでいない）" + rose);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void M3_写しは清算の後に0へ戻る(double a1)
    {
        var sim = Make(a1);
        sim.StartWarmup();
        sim.StepQuarter();
        int all = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
        {
            all++;
            Assert.True(p.T1Base == 0 && p.T1OutPrev == 0 && p.T1Mc == 0, $"清算の後に写しが残った（{r.Name}）");
        }
        Assert.True(all > 1000, "調べた生産者が少ない " + all);
    }

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double Sum(string n) { int i = I(n); double s = 0; foreach (var r in R) if (r[i] != "nan") s += double.Parse(r[i], System.Globalization.CultureInfo.InvariantCulture); return s; }
        public double Max(string n) { int i = I(n); double s = double.NegativeInfinity; foreach (var r in R) if (r[i] != "nan") s = System.Math.Max(s, double.Parse(r[i], System.Globalization.CultureInfo.InvariantCulture)); return s; }
    }

    private static Tab RunIns(double a1, int quarters, Action<MultiNationSim>? setup = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_t1_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = Make(a1);
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            try { for (int q = 0; q < quarters; q++) { setup?.Invoke(sim); sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
            finally { Instruments.Close(sim); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"t1buy_s{Seed}.tsv"));
            Assert.Equal(Instruments.T1buyHeaderText, lines[0]);
            return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void M4_旗1の成約は上界を越えず計器とエンジンが合う()
    {
        var t = RunIns(1.0, 12);
        Assert.True(t.Sum("u1Deals") >= 10000, "成約が少ない " + t.Sum("u1Deals"));
        Assert.Equal(0.0, t.Sum("u1Viol"));
        Assert.Equal(0.0, t.Sum("u1ViolAbs"));
        Assert.True(t.Sum("selfN") >= 10000, "照合した件数が少ない " + t.Sum("selfN"));
        Assert.Equal(0.0, t.Sum("selfDiff"));
        Assert.Equal(0.0, t.Sum("gateMismatch"));
        Assert.True(t.Sum("gateEval") >= 1000, "門で P̂ を評価した行が少ない " + t.Sum("gateEval"));
        Assert.True(t.Sum("popA") >= 10, "W2(a) の母集合が少ない " + t.Sum("popA"));
        // W2(a) の母集合（旧の門で飛ばし UB_Bb≦0＜UB_A1）はエンジンの門が開く（門の食い違い 0 と合わせて）＝成約まで行くかは値段の側と売り手で決まる（12 四半期の盤面では 0 件がありうる＝腕で数える）。
        Assert.True(t.Sum("oldSkip") > t.Sum("instSkip"), $"A-1 の門が旧の門より多くの行を開いていない oldSkip {t.Sum("oldSkip")} instSkip {t.Sum("instSkip")}");
        Assert.True(t.Sum("retreatDeals") >= 100, "退き（Q＜P）の効いた成約が少ない " + t.Sum("retreatDeals"));
    }

    /// <summary>M6＝売買方針〈高くても買う〉で楔が負の行（着値＜州の値段＜売り手の値段＝unitA＞lp）を作り、u′＝min(unitA, lp) をエンジンと計器が同じに読むこと（突然変異 M11〈u′ の代わりに unitA〉で照合が赤）。</summary>
    [Fact]
    public void M6_楔が負の行でもuPrimeは州の値段で切る()
    {
        var t = RunIns(1.0, 12, sim => { sim.Coef.TradeBiasStrength = 0.5; foreach (var n in sim.Nations) { n.World.Coef.TradeBiasStrength = 0.5; for (int g = 0; g < Catalog.Goods.Length; g++) n.World.SetTradeBias(g, 1); } });
        Assert.True(t.Sum("u1Deals") >= 10000, "成約が少ない " + t.Sum("u1Deals"));
        Assert.True(t.Sum("selfN") >= 10000, "照合した件数が少ない " + t.Sum("selfN"));
        Assert.Equal(0.0, t.Sum("selfDiff"));
        Assert.Equal(0.0, t.Sum("u1Viol"));
        Assert.Equal(0.0, t.Sum("gateMismatch"));
    }

    [Fact]
    public void M5_旗0ではW2aの母集合は成約しない()
    {
        var t = RunIns(0.0, 12);
        Assert.True(t.Sum("rows") >= 10000, "行が少ない " + t.Sum("rows"));
        Assert.True(t.Sum("popA") >= 10, "W2(a) の母集合が少ない " + t.Sum("popA"));
        Assert.Equal(0.0, t.Sum("popADealX"));
        Assert.Equal(0.0, t.Sum("selfN"));
        Assert.Equal(0.0, t.Sum("retreatDeals"));
        Assert.True(t.Sum("u1Deals") >= 10000, "成約が少ない " + t.Sum("u1Deals"));
    }
}
