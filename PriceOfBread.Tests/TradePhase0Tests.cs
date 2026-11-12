using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1073) 線 c102tr＝貿易の段 0：R1 (a)(b)（費用を割る数を能力の倍率から分ける・産地の発見の行は開いた時に割る）と、
/// 計器 tskpair_s（国の組の門の内訳）・tskshadow_s（置き換えの影）・recipeout_s の unitCostIfBprime。事前登録 measurements/m1073_20261112_prereg_tr.txt。
/// R1 の試験は旗 1（EndowmentCostFollowsAbundance＝1）で書く＝旗 0 では割り算を通らないので何も言えない（恒等は ident が見る）。</summary>
public class TradePhase0Tests
{
    private const ulong Seed = 1789UL;
    private static NationConfig Sicily() => WorldData.Nations().First(n => n.Provinces.Any(p => p.City == "パレルモ"));
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria(), Sicily()];
    private static MultiNationSim MakeBig() => new(Configs(), Seed, _ => new UtilityAgent());

    /// <summary>国の単体の Simulation を共有世界の形で作る。係数は構築の前に渡す（播種の賦存の割りが構築時に走る）。</summary>
    private static Simulation Solo(NationConfig cfg, Coefficients coef)
    {
        var sc = cfg.ToScenario(Seed, sharedWorld: true);
        typeof(Scenario).GetProperty("Coef")!.SetValue(sc, coef);
        var s = new Simulation(sc, new NullAgent());
        s.World.StepsDiplomacy = false;
        return s;
    }

    /// <summary>国ごとに、旗 0 の世界と旗 1（EndowmentCostFollowsAbundance＝1）の世界の対（係数は構築の前に渡す）。</summary>
    private static IEnumerable<(World off, World on)> OffOnWorlds(IEnumerable<NationConfig>? cfgs = null)
    {
        foreach (var cfg in cfgs ?? Configs())
            yield return (Solo(cfg, new Coefficients()).World, Solo(cfg, new Coefficients { EndowmentCostFollowsAbundance = 1.0 }).World);
    }

    /// <summary>R1 (a)：能力の倍率と費用を割る数は別の引数。126.8 のような逆算の倍率で費用を割らない（割る数を倍率に戻す変異で赤）。</summary>
    [Fact]
    public void R1a_費用を割る数は能力の倍率と別()
    {
        var c = new Coefficients { EndowmentCostFollowsAbundance = 1.0 };
        var pr = new Producer { Productivity = 2.0, MarginalCost = 8.0 };
        Simulation.ApplyEndowment(pr, 126.8, 4.0, c);
        Assert.Equal(2.0 * 126.8, pr.Productivity);
        Assert.Equal(8.0 / 4.0, pr.MarginalCost);
        var pr2 = new Producer { Productivity = 2.0, MarginalCost = 8.0 };
        Simulation.ApplyEndowment(pr2, 126.8, 1.0, c);   // 1.0＝割らない（産地の発見の行の播種）
        Assert.Equal(2.0 * 126.8, pr2.Productivity);
        Assert.Equal(8.0, pr2.MarginalCost);
        var off = new Producer { Productivity = 2.0, MarginalCost = 8.0 };
        Simulation.ApplyEndowment(off, 126.8, 4.0, new Coefficients());   // 旗 0＝割らない
        Assert.Equal(8.0, off.MarginalCost);
    }

    /// <summary>R1 (a) の実経路：共有世界の希少鉱物の産地（級 E・倍率 10 超）は、旗 1 で自己費用が 賦存×Scale で割られる（旗 0 の同じ生産者の費用÷4.0）・能力は旗によらず同じ。</summary>
    [Fact]
    public void R1a_希少鉱物の産地は通常の賦存で割られる_能力は変わらない()
    {
        int hits = 0;
        foreach (var (wo, wn) in OffOnWorlds())
            for (int ri = 0; ri < wo.Regions.Count; ri++)
            {
                var ro = wo.Regions[ri]; var rn = wn.Regions[ri];
                for (int pi = 0; pi < ro.Producers.Count; pi++)
                {
                    var po = ro.Producers[pi]; var pn = rn.Producers[pi];
                    var x = Instruments.RoRead(wo, ro, po);
                    if (x.Class != "E" || !(x.Rule > 10.0)) continue;
                    hits++;
                    double div = Simulation.ResourceEndowment(po.GoodId) * wo.Coef.ResourceEndowmentScale;
                    Assert.Equal(po.MarginalCost / div, pn.MarginalCost);
                    Assert.Equal(po.Productivity, pn.Productivity);
                }
            }
        Assert.True(hits >= 1, "対照：希少鉱物の産地（級 E・倍率 10 超）が無い");
    }

    /// <summary>R1 (b)：開始時の賦存に載らない産地の発見の行は、旗 1 でも播種では割らず、OpenSite で割る（播種で割る変異で赤）。能力（Productivity）は旗によらず同じ。</summary>
    [Fact]
    public void R1b_産地の発見の行は播種で割らず開いた時に割る()
    {
        // 産地の発見の行は植民地の昇格の経路（Simulation.PromoteColonyToRegion）で現れる。行ごとに、開始時の賦存に載る場合と載らない場合の両方を旗 0／旗 1 の対で作る。
        int unlisted = 0, listed = 0, plant = 0, deposit = 0;
        for (int si = 0; si < Catalog.Sites.Length; si++)
        {
            var site = Catalog.Sites[si];
            foreach (bool inRes in new[] { false, true })
            {
                var wOn = Solo(NationConfig.Britain(), new Coefficients { EndowmentCostFollowsAbundance = 1.0 }).World; var wOff = Solo(NationConfig.Britain(), new Coefficients()).World;
                Region Promote(World w) => Simulation.PromoteColonyToRegion(w, w.Coef, new Colony { Name = site.Place, Resources = inRes ? [site.Good] : [], PopSize = 50, Latitude = 10, Longitude = 10, Population = 1.0 }, RegionTier.Colonial);
                var rOn = Promote(wOn); var rOff = Promote(wOff);
                var pOn = rOn.Producers.Find(p => p.GoodId == site.Good && p.SiteRow >= 0); var pOff = rOff.Producers.Find(p => p.GoodId == site.Good && p.SiteRow >= 0);
                if (pOff is null) { Assert.Null(pOn); continue; }   // 銀の行は SilverRushEnabled＝0 の間は置かない
                Assert.NotNull(pOn);
                double div = Simulation.ResourceEndowment(site.Good) * wOff.Coef.ResourceEndowmentScale;
                Assert.Equal(inRes ? pOff.MarginalCost / div : pOff.MarginalCost, pOn!.MarginalCost);   // 載る行は昇格の播種で割る・載らない行は割らない
                Assert.Equal(pOff.Productivity, pOn.Productivity);
                Discovery.OpenSite(wOn, rOn, pOn, wOn.Coef); Discovery.OpenSite(wOff, rOff, pOff, wOff.Coef);
                Assert.Equal(pOff.MarginalCost / div, pOn.MarginalCost);   // どちらも合計で 1 回だけ割られる（鉱床の 5.0 では割らない・植林は割るだけの口）
                Assert.Equal(pOff.Productivity, pOn.Productivity);
                if (inRes) listed++; else unlisted++;
                if (site.Kind == Catalog.SiteKind.Plantation) plant++; else deposit++;
            }
        }
        Assert.True(unlisted >= 2 && listed >= 2 && plant >= 2 && deposit >= 2, $"対照：載らない行 {unlisted}・載る行 {listed}・植林 {plant}・鉱床 {deposit}");
    }

    /// <summary>recipeout_s の unitCostIfBprime：級 E の希少鉱物の産地は 自己費用÷(賦存×Scale)＋投入費（unitCostIfAbundance は逆算の倍率で割る＝より小さい）。</summary>
    [Fact]
    public void RecipeoutBprime_割る数は通常の賦存()
    {
        var sim = MakeBig(); int hits = 0;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
        {
            var x = Instruments.RoRead(n.World, r, p);
            if (x.Class != "E" || !(x.Rule > 10.0)) continue;
            hits++;
            double div = Simulation.ResourceEndowment(p.GoodId) * n.World.Coef.ResourceEndowmentScale;
            double expect = x.Self / div + (x.Mc - x.Self);
            Assert.True(System.Math.Abs(expect - x.IfBprime) <= 1e-9 * System.Math.Max(1.0, expect), $"B′ の費用 期待 {expect}・実際 {x.IfBprime}");
            Assert.True(x.IfBprime > x.IfAbundance, "逆算の倍率で割る旧の列より B′ は高い");
        }
        Assert.True(hits >= 1);
        Assert.Contains("unitCostIfBprime", Instruments.RecipeoutHeaderText);
    }

    // ---- 計器 tskpair_s・tskshadow_s
    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public double D(string[] r, string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); var s = r[i]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
        public string S(string[] r, string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return r[i]; }
    }
    private static Tab Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }

    private static (string dir, Action restore) RunInstr(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tp0_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*"); int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 2; Instruments.TsPairAllYears = true;
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria(), Sicily()], Seed, _ => new UtilityAgent());
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        Instruments.Phase(sim, Seed, "open"); sim.StartWarmup(); Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
        finally { Instruments.Close(sim); }
        return (dir, () => { Instruments.TsPairAllYears = false; Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    /// <summary>内訳の和＝tradeskip_s の行（買い手の国×財の越境の売り手の門の度数 8 列）・影の組の数＝楔で飛ばす組＋通る組・今の規則で通る組＝xPass・楔 D＝xWedgeD。
    /// 売り手の国の列を 1 つ落とす・数え損なう変異で赤。</summary>
    [Fact]
    public void Pair_内訳の和はtradeskipの行と一致し_影の組の数が合う()
    {
        var (dir, restore) = RunInstr(24);
        try
        {
            var ts = Read(dir, "tradeskip"); var pr = Read(dir, "tskpair"); var sh = Read(dir, "tskshadow");
            string[] cols = ["xPairs", "xSoldOut", "xCap", "xWedgeA", "xWedgeB", "xWedgeC", "xWedgeD", "xPass"];
            var sumPair = new Dictionary<(string, string, string), double[]>();
            foreach (var r in pr.R)
            {
                var k = (pr.S(r, "year"), pr.S(r, "nation"), pr.S(r, "good"));
                if (!sumPair.TryGetValue(k, out var a)) sumPair[k] = a = new double[8];
                for (int i = 0; i < 8; i++) a[i] += pr.D(r, cols[i]);
                Assert.NotEqual(pr.S(r, "nation"), pr.S(r, "seller"));   // 越境だけ
            }
            var sumSh = new Dictionary<(string, string, string), double[]>();
            foreach (var r in sh.R)
            {
                var k = (sh.S(r, "year"), sh.S(r, "nation"), sh.S(r, "good"));
                if (!sumSh.TryGetValue(k, out var a)) sumSh[k] = a = new double[3];
                a[0] += sh.D(r, "pairs"); a[1] += sh.D(r, "nowPass"); a[2] += sh.D(r, "nowWedgeD");
                Assert.True(sh.D(r, "dToBpPass") <= sh.D(r, "nowWedgeD") + 1e-9 && sh.D(r, "nowPass") <= sh.D(r, "pairs") + 1e-9);
            }
            int rows = 0, rowsWithPairs = 0; double wedgeD = 0, pass = 0, cross = 0;
            foreach (var r in ts.R)
            {
                string y = ts.S(r, "year"), nat = ts.S(r, "nation"), good = ts.S(r, "good");
                if (nat == "ALL") continue;
                var k = (y, nat, good); rows++;
                double xp = ts.D(r, "xPairs");
                sumPair.TryGetValue(k, out var a); a ??= new double[8];
                double[] want = [xp, ts.D(r, "xSoldOut"), ts.D(r, "xCap"), ts.D(r, "xWedgeA"), ts.D(r, "xWedgeB"), ts.D(r, "xWedgeC"), ts.D(r, "xWedgeD"), ts.D(r, "xPass")];
                for (int i = 0; i < 8; i++) Assert.True(want[i] == a[i], $"{y} {nat} {good} {cols[i]}：tradeskip {want[i]}・tskpair の和 {a[i]}");
                sumSh.TryGetValue(k, out var s); s ??= new double[3];
                Assert.True(s[0] == ts.D(r, "xWedge") + ts.D(r, "xPass") && s[1] == ts.D(r, "xPass") && s[2] == ts.D(r, "xWedgeD"), $"{y} {nat} {good} 影の組の数が tradeskip の楔と通るに合わない");
                if (xp > 0) rowsWithPairs++;
                wedgeD += ts.D(r, "xWedgeD"); pass += ts.D(r, "xPass"); cross += xp;
            }
            Assert.True(rows > 100 && rowsWithPairs >= 10, $"対照：調べた行 {rows}・越境の組のある行 {rowsWithPairs}");
            Assert.True(cross > 0 && pass > 0, "対照：越境の組と通る組がある");
            var tsKeys = ts.R.Select(r => (ts.S(r, "year"), ts.S(r, "nation"), ts.S(r, "good"))).ToHashSet();
            Assert.True(sumPair.Keys.All(tsKeys.Contains) && sumSh.Keys.All(tsKeys.Contains), "内訳と影の行は全て tradeskip_s の行に対応する");
        }
        finally { restore(); }
    }
}
