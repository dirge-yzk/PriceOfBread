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

    /// <summary>(1159) M3＝供給①は全ての生産者の写しを書くか 0 へ戻す（眠り・倒産は 0）＝清算が古い写しを読まない前提（清算は並べた生産者だけ 0 へ戻す＝段 T1-0 の漏れの直し）。
    /// 旗 0（計器なし）は写しに触れない（旗 0 の費用 0）。印の値を置いて EconomyStep.Run の後に残らないことを見る（突然変異 M5〈眠り・倒産の 0 へ戻しを外す〉で赤）。
    /// 旧（(1144)）の M3〈写しは清算の後に全て 0〉は規則が変わった＝この試験へ置き換えた（main に無い器の試験）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void M3_供給は全ての生産者の写しを書くか0へ戻す(double a1)
    {
        const double Mark = 12345.678;
        var sim = Make(a1);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        int all = 0, written = 0, zeroed = 0, untouched = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                foreach (var p in r.Producers) { p.T1OutPrev = Mark; p.T1Base = Mark; p.T1Mc = Mark; }
                EconomyStep.Run(r, w.Coef, w.TechProductivity(r), w.Bank, w);
                foreach (var p in r.Producers)
                {
                    all++;
                    bool mark = p.T1OutPrev == Mark && p.T1Base == Mark && p.T1Mc == Mark;
                    if (mark) { untouched++; continue; }
                    if (p.T1OutPrev == 0 && p.T1Base == 0 && p.T1Mc == 0) zeroed++; else written++;
                    Assert.False(p.T1OutPrev == Mark || p.T1Base == Mark || p.T1Mc == Mark, $"写しの一部だけ書いた（{r.Name}）");
                }
            }
        }
        Assert.True(all > 1000, "調べた生産者が少ない " + all);
        if (a1 > 0)
        {
            Assert.Equal(0, untouched);
            Assert.True(written > 1000, "書いた生産者が少ない " + written);
            Assert.True(zeroed >= 1, "眠り・倒産で 0 へ戻した生産者が無い（盤面が前提を作らない）" + zeroed);
        }
        else Assert.Equal(all, untouched);
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
        // (1159) W2(a)′ の母集合と退きの量：旗 0 は構成上 0（旧の門が閉じた行は成約の前に飛ばす・エンジンの Q は NaN）。母集合そのものは 0 ではない。
        Assert.True(t.Sum("popC") >= 10, "W2(a)′ の母集合 popC が少ない " + t.Sum("popC"));
        Assert.Equal(0.0, t.Sum("popCDealX"));
        Assert.Equal(0.0, t.Sum("retreatQtySum"));
        Assert.Equal(0.0, t.Sum("retreatDealsTol"));
        Assert.Equal(0.0, t.Sum("retreatDealsRaw"));
        Assert.Equal(0.0, t.Sum("uBelowMin"));
        // (1172) 段 T1-0′ の列：旗 0 は構成上 退き 0・影を越えた量 0（旧の天井≦影の天井）・誤り 0。行の数の下限＝束ねた行が盤面にある。
        Assert.True(t.Sum("rowsDealt") >= 1000, "成約のある行が少ない " + t.Sum("rowsDealt"));
        Assert.True(t.Sum("rowsDealtX") >= 100 && t.Sum("rowsDealtX") <= t.Sum("dealsX"), $"越境の成約のある行 {t.Sum("rowsDealtX")}・越境の成約 {t.Sum("dealsX")}");
        Assert.Equal(0.0, t.Sum("retreatRowN"));
        Assert.Equal(0.0, t.Sum("retreatRowSum"));
        Assert.Equal(0.0, t.Sum("excessQtyX"));
        Assert.Equal(0.0, t.Sum("excessQtyAll"));
        Assert.Equal(0.0, t.Sum("errNegQ"));
        Assert.Equal(0.0, t.Sum("errOverIns"));
        Assert.Equal(0.0, t.Sum("errOverBound"));
    }

    /// <summary>(1159) M7＝t1buy_s の段 T1-0 の列（旗 1・12 四半期）：母集合の入れ子 popC≧popCUb≧popCPart0・popCUb≧popCDealX≧popCPart0DealX・popC≦oldSkip／
    /// 下界の番人 uBelowMin＝0（突然変異 M3〈計器の u_min を lp に〉で赤）／越境の退きの量が正（突然変異 M2〈Q＝P〉で赤）・許しなしの件数≧許しつきの件数。</summary>
    [Fact]
    public void M7_旗1の母集合Cと退きの量と下界()
    {
        var t = RunIns(1.0, 12);
        Assert.True(t.Sum("u1Deals") >= 10000, "成約が少ない " + t.Sum("u1Deals"));
        Assert.Equal(0.0, t.Sum("uBelowMin"));
        double pc = t.Sum("popC"), pub = t.Sum("popCUb"), p0 = t.Sum("popCPart0"), dx = t.Sum("popCDealX"), d0 = t.Sum("popCPart0DealX");
        Assert.True(pc >= 10, "popC が少ない " + pc);
        Assert.True(pc <= t.Sum("oldSkip") && pub <= pc && p0 <= pub && dx <= pub && d0 <= dx && d0 <= p0, $"母集合の入れ子が崩れた popC {pc} popCUb {pub} popCPart0 {p0} popCDealX {dx} popCPart0DealX {d0}");
        Assert.True(t.Sum("retreatDealsTol") >= 100, "越境の退きの効いた成約が少ない " + t.Sum("retreatDealsTol"));
        Assert.True(t.Sum("retreatQtySum") > 0, "越境の退きの量が 0");
        Assert.True(t.Sum("retreatDealsRaw") >= t.Sum("retreatDealsTol"), "許しなしの件数が許しつきより少ない");
        // (1172) 段 T1-0′ の誤りの列（上側の門 G2）：旗 1 でも 0（突然変異 M6〈成約ごとに退きを重ねる〉で errOverBound・errOverIns が赤）。
        Assert.True(t.Sum("rowsDealt") >= 1000, "成約のある行が少ない " + t.Sum("rowsDealt"));
        Assert.Equal(0.0, t.Sum("errNegQ"));
        Assert.Equal(0.0, t.Sum("errOverIns"));
        Assert.Equal(0.0, t.Sum("errOverBound"));
    }

    /// <summary>(1159) M9＝tb_s の区分（TbClassify）は値段の単位に対して同次：金建ての値段（lp・rpT・先頭の売り手）を為替 fx 倍にして fx を渡すと、fx＝1 の区分と同じ
    /// （P̂ は州の名目の値段で読む＝段 T1-0 ⑤ の直し）。突然変異〈÷fx を外す〉で赤（為替を掛けた値段で P̂ を読む＝旧の食い違い）。件数の下限＝区分が変わる行が盤面にある。</summary>
    [Fact]
    public void M9_tbの区分は為替に対して同次()
    {
        var sim = Make(0.0);
        sim.StartWarmup();
        for (int q = 0; q < 4; q++) sim.StepQuarter();
        const double fx = 2.5;
        int rows = 0, sensitive = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (var r in w.Regions)
            {
                var seen = new HashSet<int>();
                foreach (var p in r.Producers)
                {
                    int g = p.GoodId;
                    if (g < 0 || g >= r.Markets.Length || !seen.Add(g)) continue;
                    var m = r.Markets[g];
                    if (!(m.LastProduced > 0) || !(m.Price > 0)) continue;
                    double lp = m.Price, rpT = lp * 0.9, head = lp * 0.8;
                    var a = Instruments.TbClassify(r, g, m, w.Coef, w, lp, rpT, head, 1.0);
                    var b = Instruments.TbClassify(r, g, m, w.Coef, w, lp * fx, rpT * fx, head * fx, 1.0, fx);
                    var old = Instruments.TbClassify(r, g, m, w.Coef, w, lp * fx, rpT * fx, head * fx, 1.0);   // 旧の読み（為替を掛けた値段で P̂）
                    if (!a.Eval) continue;
                    rows++;
                    Assert.True(a.Ok == b.Ok && a.B0 == b.B0 && a.B1 == b.B1 && a.B2 == b.B2 && a.Eval == b.Eval, $"区分が為替で変わった（{r.Name}・{Catalog.Goods[g].Name}）");
                    Assert.True(Near(a.UbA1, b.UbA1, 1e-12), $"UB_A1 が為替で変わった {a.UbA1}⇔{b.UbA1}");
                    if (!Near(old.UbA1, a.UbA1, 1e-9)) sensitive++;
                }
            }
        }
        Assert.True(rows >= 100, "P̂ を読んだ行が少ない " + rows);
        Assert.True(sensitive >= 10, "旧の読みと値が違う行が少ない（盤面が値段に鈍い）" + sensitive);
    }

    /// <summary>(1159) M8＝W2(a)′ の母集合の述語〈h_min＝先頭の売り手の値段×lbMulMin＜lp〉（突然変異 M4〈述語を逆に〉で赤）。期待値は手で置いた数（式を写さない）。</summary>
    [Fact]
    public void M8_母集合Cの述語は着値の下界が州の値段を下回る行()
    {
        Assert.True(Instruments.T1HBelow(0.5, 1.0, 1.0));
        Assert.True(Instruments.T1HBelow(0.8, 1.2, 1.0));    // 0.96＜1
        Assert.False(Instruments.T1HBelow(0.9, 1.2, 1.0));   // 1.08≧1
        Assert.False(Instruments.T1HBelow(1.0, 1.0, 1.0));   // 等しいは入らない
        Assert.False(Instruments.T1HBelow(double.NaN, 1.0, 1.0));   // 売り手が無い行
        Assert.False(Instruments.T1HBelow(0.5, 1.0, double.NaN));
    }
}
