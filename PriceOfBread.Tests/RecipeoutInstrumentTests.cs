using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1013) 線 c94ro：貿易の計器 C3 <c>recipeout_s</c>（国×財×レシピ×賦存の級の産出と単位費用・読むだけ）の番人。事前登録 measurements/m1013_20261110_prereg_ro.txt [3]。
/// T1a＝(年,国,財) の Σ outputSum が同じ実行の nationgood_s の producedQty と合う（量の保存・1e-9）。T1b＝対照の件数。T2＝計器の生産者ごとの mc が ProductionDiag.ExplainCostByRecipe と合う（州ごとに値段を変えた盤面）。
/// T3＝級の規則値と Productivity÷(財の生産性×7.0) の食い違い 0 件（級を計器が自分で決める＝同語反復にしない）。T4＝旗 EndowmentCostFollowsAbundance=1 の盤面のエンジンの mc が旗 0 の盤面の計器の案 B′ の列（unitCostIfBprime）と合う（大鉱床 4.0×5.0 を含む）。B1＝案 B′ の列が紙の式（自己費用÷(ResourceEndowment×Scale)＋投入費）と合う。T5＝表の存在。
/// 期待は手で置く（T2 と T4 の相手はエンジンの別の経路・T1 は別の計器・T3 は Productivity と規則値の独立な 2 本）。何もしない計器は 0 で緑になる＝全ての門に〈正の行が 1 つ以上ある〉対照を置く。
/// </summary>
[Collection("Instruments")]
public class RecipeoutInstrumentTests
{
    private const ulong Seed = 1789UL;
    private static NationConfig Sicily() => WorldData.Nations().First(n => n.Provinces.Any(p => p.City == "パレルモ"));   // 希少鉱物（硫黄）の産地＝総量保存で膨らんだ賦存（E の倍率 10 超）の対照
    private static NationConfig[] Configs() => [NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria(), Sicily()];
    private static MultiNationSim Make(Coefficients? coef = null) => new(Configs(), Seed, _ => new UtilityAgent(), coef);

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
    }
    private static Tab Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }

    private static (string dir, MultiNationSim sim, Action restore) Run(int quarters, bool instrumentsOn = true)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_ro_" + Guid.NewGuid().ToString("N"));
        if (instrumentsOn) Instruments.Configure(dir, "*"); else Instruments.Configure(null, null);
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        var sim = Make();
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        if (instrumentsOn) Instruments.Phase(sim, Seed, "open");
        sim.StartWarmup();
        if (instrumentsOn) Instruments.Phase(sim, Seed, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instrumentsOn) Instruments.Quarter(sim, Seed, q); } }
        finally { if (instrumentsOn) Instruments.Close(sim); }
        return (dir, sim, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    private static bool Close(double a, double b, double rel) => Math.Abs(a - b) <= rel * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));

    /// <summary>T1a＋T1b：量の保存。(年,国,財) の Σ outputSum が nationgood_s の producedQty と合う（nationgood は G10 の丸め＝最大 5e-10）。下側＝書かないと対照の件数で赤・上側＝Capacity で書くと赤。</summary>
    [Fact]
    public void T1_量の保存は国と財ごとにnationgoodの産出と合う()
    {
        var (dir, _, restore) = Run(12);
        try
        {
            var ro = Read(dir, "recipeout"); var ng = Read(dir, "nationgood");
            Assert.Equal(RecipeoutHeaderExpected, string.Join('\t', ro.H));
            var sum = new Dictionary<(string, string, string), double>();
            int iy = ro.I("year"), inat = ro.I("nation"), ig = ro.I("good");
            foreach (var r in ro.R) { var k = (r[iy], r[inat], r[ig]); sum[k] = sum.GetValueOrDefault(k) + ro.D(r, "outputSum"); }
            int cmp = 0, pos = 0;
            foreach (var r in ng.R)
            {
                double want = ng.D(r, "producedQty"); var k = (r[ng.I("year")], r[ng.I("nation")], r[ng.I("good")]);
                double got = sum.GetValueOrDefault(k);
                Assert.True(Math.Abs(got - want) <= 1e-9 * Math.Max(Math.Abs(want), 1e-300) + 1e-12, $"{k}：計器 {got}・nationgood {want}");
                cmp++; if (want > 1e-6) pos++;
            }
            Assert.True(cmp >= 1000, $"調べた組が少ない {cmp}");
            Assert.True(pos >= 200, $"産出が正の組が少ない {pos}");
            Assert.Contains(ro.R, r => r[ro.I("class")] == "E"); Assert.Contains(ro.R, r => r[ro.I("class")] == "N");
            Assert.Contains(ro.R, r => r[ro.I("class")] == "S"); Assert.Contains(ro.R, r => r[ro.I("class")] == "F");
        }
        finally { restore(); }
    }
    private const string RecipeoutHeaderExpected = "year\tnation\tgood\trecipe\tclass\tproducers\toutputSum\tcapacitySum\tscaleSum\tunitCostOutW\tselfCostOutW\tunitCostIfAbundance\tendowFromProductivity\tsiteFound	unitCostIfBprime";

    /// <summary>T2：州ごとに値段を変えた盤面で、計器の生産者ごとの mc・自己費用が ExplainCostByRecipe の同じレシピ（名前と投入の財の列）の Mc・OwnCost と合う。</summary>
    [Fact]
    public void T2_費用はExplainCostByRecipeと合う()
    {
        var (dir, sim, restore) = Run(4);
        try
        {
            int checkedN = 0;
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                for (int ri = 0; ri < w.Regions.Count; ri++) foreach (var m in w.Regions[ri].Markets) m.Price *= 1.0 + 0.13 * ri;   // 州ごとに投入の値段を変える
                foreach (var r in w.Regions)
                {
                    var seen = new List<Producer>();
                    foreach (var p in r.Producers)
                    {
                        if (p.GoodId < 0 || p.GoodId >= r.Markets.Length) continue;
                        bool dup = false;
                        foreach (var s in seen) if (s.GoodId == p.GoodId && string.Equals(s.RecipeName, p.RecipeName, StringComparison.Ordinal) && s.Inputs.Select(i => i.GoodId).SequenceEqual(p.Inputs.Select(i => i.GoodId))) { dup = true; break; }
                        if (dup) continue;
                        seen.Add(p);
                        var cp = ProductionDiag.ExplainCostByRecipe(w, r, p.GoodId).FirstOrDefault(c => c.HasProducer && string.Equals(c.Recipe, p.RecipeName, StringComparison.Ordinal) && c.Items.Select(i => i.GoodId).SequenceEqual(p.Inputs.Select(i => i.GoodId)));
                        if (cp.Items is null) continue;
                        var x = Instruments.RoRead(w, r, p);
                        Assert.True(Close(x.Mc, cp.Mc, 1e-9), $"{w.Region.Name}/{r.Name}/{p.GoodId}/{p.RecipeName}：計器 {x.Mc}・エンジン {cp.Mc}");
                        Assert.True(Close(x.Self, cp.OwnCost, 1e-9), $"自己費用 {x.Self}・{cp.OwnCost}");
                        checkedN++;
                    }
                }
            }
            Assert.True(checkedN >= 500, $"突き合わせた生産者が少ない {checkedN}");
        }
        finally { restore(); }
    }

    /// <summary>T3：級の規則値（計器が州の resources・Sites・Climate から決める）と Productivity÷(財の生産性×7.0) の食い違いが 0 件。開いた大鉱床（4.0×5.0）も含む。</summary>
    [Fact]
    public void T3_級の規則値はProductivityの倍率と合う()
    {
        var sim = Make();
        int scarceE = 0; var cnt = new Dictionary<string, int>(); var badList = new List<string>(); int bad = 0; int ucount = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                foreach (var p in r.Producers)
                {
                    var x = Instruments.RoRead(n.World, r, p);
                    if (double.IsNaN(x.Endow)) continue;   // 播種の賦存を受けない生産者（職人・代替レシピ・役務）
                    cnt[x.Class] = cnt.GetValueOrDefault(x.Class) + 1;
                    if (x.Class == "E" && x.Rule > 10.0) scarceE++;
                    if (x.Class == "U") { ucount++; continue; }
                    if (!Close(x.Endow, x.Rule, 1e-9)) { bad++; if (badList.Count < 12) badList.Add($"{n.World.Region.Name}/{r.Name}/財{p.GoodId}:{Catalog.Goods[p.GoodId].Name}/{p.RecipeName}/RGO={p.IsRgo}/{x.Class} 規則 {x.Rule} 倍率 {x.Endow}"); }
                }
        Assert.True(bad == 0, $"食い違い {bad} 件：" + string.Join(" | ", badList));
        Assert.Equal(0, ucount);
        Assert.True(scarceE >= 1, "希少鉱物の産地（E で倍率 10 超）の対照が無い");
        foreach (var k in new[] { "E", "N", "S", "Z" }) Assert.True(cnt.GetValueOrDefault(k) >= 1, $"級 {k} の対照が無い");
        // 級 X：未発見（産出の門 0・倍率 4.0）→ 開く（4.0×5.0＝20）。同じ生産者を開く前と後で読む
        (World w, Region r, Producer p)? site = null;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
            if (site is null && p.IsRgo && p.SiteRow >= 0 && Catalog.Sites[p.SiteRow].Kind == Catalog.SiteKind.Deposit) site = (n.World, r, p);
        Assert.NotNull(site);
        var (sw, sr, sp) = site!.Value;
        var before = Instruments.RoRead(sw, sr, sp);
        Assert.Equal("X", before.Class); Assert.False(sp.SiteFound);
        Assert.True(Close(before.Endow, before.Rule, 1e-9));
        Discovery.OpenSite(sw, sr, sp, sw.Coef);
        var after = Instruments.RoRead(sw, sr, sp);
        Assert.Equal("X", after.Class); Assert.True(sp.SiteFound);
        Assert.True(Close(after.Rule, before.Rule * sw.Coef.GoldRushEndowment, 1e-9));
        Assert.True(Close(after.Endow, after.Rule, 1e-9));
        // 7.0 の写しの固定：級 S の生産者は Productivity＝財の生産性×7.0 ちょうど
        bool anyS = false;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
            if (p.IsRgo && Instruments.RoRead(n.World, r, p).Class == "S") { Assert.Equal(Catalog.Goods[p.GoodId].Productivity * 7.0, p.Productivity, 12); anyS = true; }
        Assert.True(anyS);
    }

    /// <summary>国の単体の Simulation を共有世界の形（sharedWorld=true・StepsDiplomacy=false）で作る。係数は構築の前に渡す（播種の賦存の割りが構築時に走る）。</summary>
    private static Simulation Solo(NationConfig cfg, Coefficients coef)
    {
        var sc = cfg.ToScenario(Seed, sharedWorld: true);
        typeof(Scenario).GetProperty("Coef")!.SetValue(sc, coef);
        var s = new Simulation(sc, new NullAgent());
        s.World.StepsDiplomacy = false;
        return s;
    }

    /// <summary>T4：旗 EndowmentCostFollowsAbundance=1 の盤面のエンジンの mc（MineralSelfCost＋InputCostPerUnit）が、旗 0 の盤面の計器の案 B′ の列（unitCostIfBprime）と合う。倍率>1 の全ての RGO と、同じ生産者を開いた大鉱床。旗 1 の盤面の計器は二重に割らない。</summary>
    [Fact]
    public void T4_旗1のエンジンの費用は旗0の計器の案Bプライムの列と合う()
    {
        int abund = 0, big = 0, bigE = 0, deposit = 0;
        foreach (var cfg in new[] { NationConfig.Britain(), NationConfig.Japan(), NationConfig.America(), NationConfig.France(), Sicily() })
        {
            var w0 = Solo(cfg, new Coefficients { EndowmentCostFollowsAbundance = 0.0 }).World; var w1 = Solo(cfg, new Coefficients { EndowmentCostFollowsAbundance = 1.0 }).World;   // (1144) 旗 0 の世界は明示の 0
            Assert.True(w1.Coef.EndowmentCostFollowsAbundance > 0 && !(w0.Coef.EndowmentCostFollowsAbundance > 0));
            Assert.Equal(w0.Regions.Count, w1.Regions.Count);
            for (int ri = 0; ri < w0.Regions.Count; ri++)
            {
                var r0 = w0.Regions[ri]; var r1 = w1.Regions[ri];
                Assert.Equal(r0.Producers.Count, r1.Producers.Count);
                for (int pi = 0; pi < r0.Producers.Count; pi++)
                {
                    var p0 = r0.Producers[pi]; var p1 = r1.Producers[pi];
                    if (p0.IsRgo && p0.SiteRow >= 0 && Catalog.Sites[p0.SiteRow].Kind == Catalog.SiteKind.Deposit && !p0.SiteFound)
                    { Discovery.OpenSite(w0, r0, p0, w0.Coef); Discovery.OpenSite(w1, r1, p1, w1.Coef); deposit++; }
                    var x0 = Instruments.RoRead(w0, r0, p0); var x1 = Instruments.RoRead(w1, r1, p1);
                    double engine1 = EconomyStep.MineralSelfCost(p1, r1, w1.Coef, w1) + EconomyStep.InputCostPerUnit(p1, r1, r1, w1.Coef);
                    Assert.True(Close(x0.IfBprime, engine1, 1e-9), $"{cfg.Name}/{r0.Name}/財{p0.GoodId}：旗0の計器（案 B′ の列）{x0.IfBprime}・旗1のエンジン {engine1}");   // (1073) R1：割る数が RE×Scale になった＝比べる列は B′ の列（旧の列は希少鉱物の逆算の倍率・富鉱の 5.0 で割る）
                    Assert.True(Close(x1.IfAbundance, x1.Mc, 1e-12), "旗 1 の盤面で二重に割っている");
                    if (x0.Rule > 1.0) { abund++; if (x0.Rule > 10.0) big++; if (x0.Class == "E" && x0.Rule > 10.0) bigE++; }
                }
            }
        }
        Assert.True(abund >= 20, $"倍率>1 の生産者が少ない {abund}");
        Assert.True(big >= 1, "倍率 10 超（希少鉱物か開いた大鉱床）の対照が無い");
        Assert.True(bigE >= 1, "希少鉱物の産地（E で倍率 10 超）の対照が無い");
        Assert.True(deposit >= 1, "大鉱床の盤面が無い");
    }

    /// <summary>
    /// B1（1079）：旗 0 の盤面で、計器の案 B′ の列 unitCostIfBprime が紙の式と合う。
    /// 紙の式＝自己費用 ÷ (ResourceEndowment × ResourceEndowmentScale) ＋ 投入費。割る数は級 E と、開いた後の級 X だけ（開く前の大鉱床・S・N・F は 1）。
    /// 期待は Producer の欄と州の値段から試験の中で作る（RE は鉱物 4.0・他 3.5 の値を置く・ScarceEndowment や計器の関数は呼ばない）。
    /// 能力の倍率（希少鉱物の逆算の倍率・大鉱床の 5.0）は割る数に入らない＝規則の倍率>10 の行でも割る数は RE×Scale だけ。
    /// </summary>
    [Fact]
    public void B1_案Bプライムの列は自己費用を賦存の倍率だけで割った値に投入費を足す()
    {
        int divided = 0, scarceDivided = 0, openedDeposit = 0, unopenedDeposit = 0, undivided = 0;
        foreach (var scale in new[] { 1.0, 1.5 })   // Scale≠1 の盤面でも割る数は RE×Scale（Scale を落とす誤りはここで赤になる）
        foreach (var cfg in new[] { NationConfig.Britain(), NationConfig.Japan(), NationConfig.America(), NationConfig.France(), Sicily() })
        {
            var w = Solo(cfg, new Coefficients { ResourceEndowmentScale = scale, EndowmentCostFollowsAbundance = 0.0 }).World;   // (1144) 旗 0 の盤面は明示の 0
            var c = w.Coef;
            Assert.True(!(c.EndowmentCostFollowsAbundance > 0) && !(c.MineralCostFollowsGrade > 0));
            foreach (var r in w.Regions)
            {
                // 大鉱床は開く前と開いた後の両方を見る（開く前の行は割らない・開いた後の行は割る）
                var deps = r.Producers.Where(p => p.IsRgo && p.SiteRow >= 0 && Catalog.Sites[p.SiteRow].Kind == Catalog.SiteKind.Deposit && !p.SiteFound).ToList();
                foreach (var p in deps) { CheckB(w, r, p); unopenedDeposit++; }
                foreach (var p in deps) Discovery.OpenSite(w, r, p, c);
                foreach (var p in r.Producers) CheckB(w, r, p);
                openedDeposit += deps.Count;
            }
        }
        void CheckB(World w, Region r, Producer p)
        {
            var c = w.Coef;
            var x = Instruments.RoRead(w, r, p);
            double self = p.MarginalCost * w.CostPriceLevel * r.LocalPriceMultiplier;
            double inputs = 0;
            foreach (var inp in p.Inputs)
            {
                double qty = inp.QtyPerOutput;
                if (inp.GoodId == Catalog.LaborId && p.BuysLabor) qty = inp.QtyPerOutput * (1.0 - c.ElecLaborSave * p.ElecFill) * (1.0 - c.LaborSaveRate * r.Mechanization);
                inputs += (inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? r.Markets[inp.GoodId].Price : 0.0) * qty;
            }
            double re = (Catalog.IsMineral(p.GoodId) ? 4.0 : 3.5) * c.ResourceEndowmentScale;
            // 割るかどうかは計器の級を借りず、州の資源・産地の行・発見の印・既定の生産者かどうかから決める
            bool eligible = p.IsRgo || (p.RecipeName.Length == 0 && p.GoodId >= 0 && p.GoodId < Catalog.Goods.Length && Catalog.Goods[p.GoodId].Tier != GoodTier.Service);
            bool inRes = Array.IndexOf(r.Resources, p.GoodId) >= 0;
            bool foundSite = p.IsRgo && p.SiteRow >= 0 && p.SiteFound;
            bool dividesNow = eligible && re > 1.0 && (inRes || foundSite);
            double expected = (dividesNow ? self / re : self) + inputs;
            Assert.True(Close(x.IfBprime, expected, 1e-9), $"{r.Name}/財{p.GoodId}/級{x.Class}：計器 {x.IfBprime}・紙の式 {expected}");
            if (dividesNow) { divided++; if (x.Rule > re * 1.5) scarceDivided++; } else undivided++;
        }
        Assert.True(divided >= 20, $"割る行が少ない {divided}");
        Assert.True(scarceDivided >= 1, "規則の倍率が RE×Scale を超える行（希少鉱物の産地か開いた大鉱床）の対照が無い");
        Assert.True(unopenedDeposit >= 1 && openedDeposit >= 1, "大鉱床の開く前と後の対照が無い");
        Assert.True(undivided >= 20, $"割らない行の対照が少ない {undivided}");
    }

    /// <summary>(1019) S1：siteFound の列。開く前の大鉱床の行は割合が下がり、OpenSite の後は上がる（Sites の Kind で決める変異では動かない）。級 X 以外は nan・級 X の行は 0〜1。</summary>
    [Fact]
    public void S1_siteFoundは産地の発見に従う()
    {
        var sim = Make();
        (World w, Region r, Producer p)? site = null;
        foreach (var n in sim.Nations) foreach (var r in n.World.Regions) foreach (var p in r.Producers)
            if (site is null && p.IsRgo && p.SiteRow >= 0 && Catalog.Sites[p.SiteRow].Kind == Catalog.SiteKind.Deposit && !p.SiteFound) site = (n.World, r, p);
        Assert.NotNull(site);
        var (sw, sr, sp) = site!.Value;
        int gc = Catalog.Goods.Length;
        double Frac()
        {
            string rn = sp.RecipeName.Length == 0 ? "(既定)" : sp.RecipeName;
            var row = Instruments.RoRows(sw, gc).Single(x => x.Good == sp.GoodId && x.Recipe == rn && x.Class == "X");
            return row.SiteF;
        }
        double before = Frac();
        Assert.True(before >= 0.0 && before < 1.0, $"開く前の割合 {before}");
        Discovery.OpenSite(sw, sr, sp, sw.Coef);
        double after = Frac();
        Assert.True(after > before && after <= 1.0, $"開いた後の割合 {after}・前 {before}");
        int xRows = 0;
        foreach (var n in sim.Nations)
            foreach (var row in Instruments.RoRows(n.World, gc))
            {
                if (row.Class == "X") { xRows++; Assert.True(row.SiteF >= 0.0 && row.SiteF <= 1.0); }
                else Assert.True(double.IsNaN(row.SiteF), $"級 {row.Class} の siteFound は nan");
            }
        Assert.True(xRows >= 1, "級 X の行が無い");
    }

    /// <summary>T5：INS を切ると表を書かない。</summary>
    [Fact]
    public void T5_計器が切れていれば表は無い()
    {
        var (dir, _, restore) = Run(4, instrumentsOn: false);
        try { Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, $"recipeout_s{Seed}.tsv"))); }
        finally { restore(); }
    }

    /// <summary>
    /// (1100) B′ の列の旗 1 の試験（(1079) の反証役 v）：旗 EndowmentCostFollowsAbundance=1 の盤面で、計器の案 B′ の列 unitCostIfBprime が**そのままエンジンの mc**（MineralSelfCost＋InputCostPerUnit）と合う
    /// ＝旗 1 の盤面で既に割ってある分を計器が二重に割らない（dividedNow の項）。T4 は旗 0 の計器の列を旗 1 のエンジンと比べるだけで、旗 1 の計器の列そのものは見ていなかった。
    /// 対照の件数＝旗 0 の盤面で割る数が 1 を超える（案 B′ の列が mc より小さい）生産者の数に下限を置く（何もしない計器・割る行の無い盤面で緑にならない）。
    /// </summary>
    [Fact]
    public void T6_旗1の盤面の案Bプライムの列はエンジンの費用そのままで二重に割らない()
    {
        int dividedRows = 0, rows = 0;
        foreach (var cfg in new[] { NationConfig.Britain(), NationConfig.Japan(), NationConfig.America(), NationConfig.France(), Sicily() })
        {
            var w0 = Solo(cfg, new Coefficients { EndowmentCostFollowsAbundance = 0.0 }).World; var w1 = Solo(cfg, new Coefficients { EndowmentCostFollowsAbundance = 1.0 }).World;   // (1144) 旗 0 の世界は明示の 0
            for (int ri = 0; ri < w1.Regions.Count; ri++)
            {
                var r0 = w0.Regions[ri]; var r1 = w1.Regions[ri];
                for (int pi = 0; pi < r1.Producers.Count; pi++)
                {
                    var p0 = r0.Producers[pi]; var p1 = r1.Producers[pi];
                    var x0 = Instruments.RoRead(w0, r0, p0); var x1 = Instruments.RoRead(w1, r1, p1);
                    double engine1 = EconomyStep.MineralSelfCost(p1, r1, w1.Coef, w1) + EconomyStep.InputCostPerUnit(p1, r1, r1, w1.Coef);
                    Assert.True(Close(x1.IfBprime, engine1, 1e-9), $"{cfg.Name}/{r1.Name}/財{p1.GoodId}：旗1の計器（案 B′ の列）{x1.IfBprime}・旗1のエンジン {engine1}");
                    rows++;
                    if (x0.IfBprime < x0.Mc * (1.0 - 1e-6)) dividedRows++;
                }
            }
        }
        Assert.True(rows >= 100, $"調べた行が少ない {rows}");
        Assert.True(dividedRows >= 20, $"割る数が 1 を超える対照の行が少ない {dividedRows}");
    }
}
