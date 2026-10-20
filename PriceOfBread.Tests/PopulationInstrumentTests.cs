using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(361) 人口の作り替え 段A 計器＋段B 影の器（<c>pop_s</c>・読むだけ）の番人。
/// ①手の算術：子の価値 v・ψ*・g・m・c の影と 60 歳以後の余命を、分数で書いた期待値と突き合わせる（式は <c>PopShadow</c> の外で書き直す）。
/// ②hook：飢えの局地の盤面で、層ごとの死が平常＋飢えの総数に閉じ・出生と死が既存の台帳（<c>World.DiagBirths／DiagDeaths</c>）に合い・飢えの死が子供と老人に厚い。
/// ③走行：計器を出しても軌道が変わらない（全ビット一致）・全ての国に行が出る・出生と死の窓の和が台帳の累計に合う・値が入った行がある（何もしない番人にしない）。
/// </summary>
[Collection("Instruments")]
public class PopulationInstrumentTests
{
    [Fact]
    public void 手の算術で子の価値の影とmとcが出る()
    {
        // 稼ぎ 10・籠 2・半人前 0.5・児童労働 0.4・教育の必要量 0.25×値段 4・n=0.5・公費 0.25・S10=0.8・L60=12・Ns=2・π=0.2・K=0.5
        var x = new Instruments.PopShadowIn(10, 2, 0.5, 0.4, 0.25, 4, 0.5, 0.25, 0.8, 12, 2, 0.2, 0.5);
        var o = Instruments.PopShadow(x);
        Assert.Equal(10 * 0.5 * 0.4 * 60, o.E, 12);                                  // 120
        Assert.Equal(180 * 0.5 * 2, o.ChildBasketCost, 12);                           // 180
        Assert.Equal(120 * 0.25 * 4, o.EduCostFull, 12);                              // 120（n=1・公費 0）
        Assert.Equal(180 + 120 * 0.5 * 0.75, o.ChildCost, 12);                        // 225
        Assert.Equal(12.0 * 12 * 2 * 0.8 / 2, o.OldAge, 12);                          // 115.2
        Assert.Equal(1800, o.DirectUtility1, 12);                                     // 10×15×12
        Assert.Equal((0.8 * 120 + 115.2 - 225) / 225, o.V, 12);                       // −13.8/225
        Assert.Equal(13.8 / 1800, o.PsiBreakeven, 12);                                // v=0 になる ψ
        // ψ* を入れた v が 0 になる（式の別の経路）
        Assert.Equal(0, (0.8 * o.E + o.OldAge + o.PsiBreakeven * o.DirectUtility1 - o.ChildCost) / o.ChildCost, 12);
        double g = 1.0 / (1.0 + Math.Exp(13.8 / 225));                                // v=−13.8/225
        Assert.Equal(g, o.G, 12);
        Assert.Equal(1.0 - 0.5 * (1.0 - g), o.CKnown, 12);
        Assert.Equal(2.5, o.A, 12);                                                    // 10÷(2×(1+0.5×2))
        Assert.Equal(0.85 * 2.5 / 3.5, o.MNew, 12);
        Assert.Equal(7.5 * (0.85 * 2.5 / 3.5) * (1.0 - 0.5 * (1.0 - g)), o.TfrShadow, 12);
        // 児童労働を禁じる（割合 0）と E が 0 になり v が下がる（作者の鎖：児童労働の禁止→子の価値↓）
        var banned = Instruments.PopShadow(x with { ChildLaborRate = 0 });
        Assert.Equal(0, banned.E, 12);
        Assert.True(banned.V < o.V);
        // 教育が一般化する（n=1）と C が増え v が下がる（教育を受けさせられるだけの子供）
        Assert.True(Instruments.PopShadow(x with { NEdu = 1.0 }).V < o.V);
        // 年金が覆う（π↑）と O が下がり v が下がる
        Assert.True(Instruments.PopShadow(x with { Pi = 0.6 }).OldAge < o.OldAge);
        // 生き残る子が増える（Ns↑）と O が下がる（1人あたりの扶養が薄まる）・A が下がる（子の籠の期待が増える）
        var moreKids = Instruments.PopShadow(x with { Ns = 4 });
        Assert.Equal(12.0 * 12 * 2 * 0.8 / 4, moreKids.OldAge, 12);
        Assert.True(moreKids.A < o.A);
        // 費用が正でない・入力が NaN は全て NaN（否定形）
        var bad = Instruments.PopShadow(x with { Basket = double.NaN });
        Assert.True(double.IsNaN(bad.V) && double.IsNaN(bad.TfrShadow) && double.IsNaN(bad.CKnown));
    }

    [Fact]
    public void 六十歳以後の余命は閉じた形に合う()
    {
        // μ6=0.05・μ7=0.1：(1−e^−0.5)/0.05 + e^−0.5/0.1
        double s = Math.Exp(-0.5);
        Assert.Equal((1 - s) / 0.05 + s / 0.1, Instruments.PopL60(0.05, 0.1), 12);
        Assert.Equal(13.9346934, Instruments.PopL60(0.05, 0.1), 6);
        // 死の率が小さいほど長い・率が 0 や NaN は NaN
        Assert.True(Instruments.PopL60(0.02, 0.05) > Instruments.PopL60(0.05, 0.1));
        Assert.True(double.IsNaN(Instruments.PopL60(0, 0.1)) && double.IsNaN(Instruments.PopL60(0.05, double.NaN)));
    }

    private static Scenario Famine(ulong s) => new() { Seed = s, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() };

    [Fact]
    public void hookで層別の死が総数に閉じ_既存の台帳に合い_飢えの死が子供と老人に厚い()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pop_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new Simulation(Famine(1), new NullAgent());
            sim.Run(120);   // 30年（食料不足の州が飢える）
            var a = Instruments.PopAccOf(sim.World);
            double bandSum = 0; foreach (var d in a.BandDeaths) bandSum += d;
            Assert.True(a.DeathsFamine > 0, "飢えの死が 0＝hook が飢えの経路に来ていない（対照の盤面でない）");
            Assert.True(a.DeathsNormal > 0 && a.Births > 0);
            Assert.Equal(a.DeathsNormal + a.DeathsFamine, bandSum, 6);                                   // 層別の死の和＝平常＋飢え
            Assert.Equal(sim.World.DiagBirths, a.Births, 6);                                           // 出生＝既存の台帳（別の出所）
            Assert.Equal(sim.World.DiagDeaths, a.DeathsNormal + a.DeathsFamine, 6);                    // この盤面に疫病は無い＝死の台帳に閉じる
            Assert.True(a.Months == 360, $"人口の月の数 {a.Months}");   // Run(120)＝120 四半期＝360 月
            // 飢えの死は災害の重み（0〜9 と 60 以上が 2 倍）＝子供（層 0）の死の割合が平常のみの比より高い。平常のみの比は同じ盤面の平常の層別を持たないので、
            // 層 0 の死÷全層の死 が、層 0 の人年÷全層の人年 より大きい（子供は人口の割合以上に死ぬ）ことを見る。
            double expo = 0; foreach (var e in a.BandExposure) expo += e;
            int top = Pop.Layout.Bands - 1;   // 最上層（8層は 70 以上・11層は 71 以上）
            Assert.True(a.BandDeaths[0] / bandSum > a.BandExposure[0] / expo, "子供の死の割合が人口の割合以下（年齢の重みが hook に載っていない）");
            Assert.True(a.BandDeaths[top] / bandSum > a.BandExposure[top] / expo, "最上層の死の割合が人口の割合以下");
            // 15〜49 歳の女の人年は人年の一部
            Assert.InRange(a.FertileWomenMonths, 0.05 * a.PopMonths, 0.5 * a.PopMonths);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 飢えのhookは災害の重みで層を取る_手の算術()
    {
        // ★(363) 反証役 pa〔v〕低#2：上の盤面の番人は〈層 0・層 7 の死の割合＞人年の割合〉だけを見る＝平常の重みでも通る。
        // ここは hook を直に呼び、年齢の構成を一様（16 本×1/16）・超過 1.0（0〜9 と 60 以上が 2 倍）にして、層ごとの取り分を手の分数で突き合わせる。
        var sim = new Simulation(Famine(1), new NullAgent());
        var w = sim.World;
        var p = w.Regions.SelectMany(r => r.Pops).First(x => x.Size > 0);
        var lay = Pop.Layout; int nb = lay.Bands;
        for (int k = 0; k < p.Ages.Length; k++) p.Ages[k] = 1.0 / (2 * nb);
        Span<double> dw = stackalloc double[2 * nb];
        Pop.DisasterWeights(dw, 1.0);
        var a = Instruments.PopAccOf(w); a.Clear(w);
        double d = 1e-3 * p.Size;   // 小さい＝どの層も尽きない（配り直しが起きない）
        Instruments.DemoFamine(w, p, d, dw);
        // 重み 2 の層（下端が 10 歳未満か 60 歳以上＝8層は 0・6・7／11層は 0・1・6・61・71 以上）は 2 本×2＝4、ほかの層は 2 本×1＝2（年齢の述語で数える＝層の番号を直書きしない）
        double[] expect = new double[nb]; double tot = 0;
        for (int b = 0; b < nb; b++) { expect[b] = (lay.Lower[b] < 10.0 || lay.Lower[b] >= 60.0) ? 4.0 : 2.0; tot += expect[b]; }
        for (int b = 0; b < nb; b++) expect[b] /= tot;   // 8層は 合計 3×4＋5×2＝22
        for (int b = 0; b < nb; b++)
            Assert.True(Math.Abs(a.BandDeaths[b] - expect[b] * d) <= 1e-9 * d, $"層 {b} の飢えの死 {a.BandDeaths[b] / d:F6}×d が手の分数 {expect[b]:F6}×d に合わない（災害の重みが hook に載っていない）");
        Assert.Equal(d, a.DeathsFamine, 12);
        Assert.Equal(0, a.DeathsNormal);
    }

    [Fact]
    public void 飢えが無い盤面では層別の死の率が年齢の重みの比に合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pop3_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var c = new Coefficients(); c.FamineExcessMortality = 0;
            var sim = new Simulation(new Scenario { Seed = 1, Kind = ScenarioKind.RegionalFamine, Coef = c }, new NullAgent());
            sim.Run(60);
            var a = Instruments.PopAccOf(sim.World);
            Assert.Equal(0, a.DeathsFamine);
            Assert.True(a.DeathsNormal > 0);
            var w = Pop.MortalityWeights(c.CrudeBirthRatePerYear.Value);   // 外の出所：年齢の重み（1人あたりの相対の死亡率）
            double mu0 = a.BandDeaths[0] / a.BandExposure[0];
            Assert.True(mu0 > 0);
            for (int b = 1; b < Pop.Layout.Bands; b++)
            {
                double ratio = a.BandDeaths[b] / a.BandExposure[b] / mu0, expect = w[2 * b] / w[0];
                Assert.True(Math.Abs(ratio - expect) <= 0.05 * expect, $"層 {b} の死の率の比 {ratio:F4} が年齢の重みの比 {expect:F4} に合わない");
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void 戦死は窓ごとの差分で積まれ_窓の和が累計に合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pop4_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "フランス");
        try
        {
            var sim = Make();
            Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
            var fr = sim.Nations.First(n => Instruments.NameOf(n.World) == "フランス").World;
            double inject0 = fr.WarDeadCumulative;
            try
            {
                for (int q = 0; q < 6; q++)
                {
                    sim.StepQuarter();
                    if (q == 1) fr.WarDeadCumulative += 7;   // 戦死の台帳へ直接足す（診断の欄・動学は読まない）＝窓ごとの差分で拾われるか
                    if (q == 3) fr.WarDeadCumulative += 3;
                    Instruments.Quarter(sim, 1789UL, q);
                }
            }
            finally { Instruments.Close(sim); }
            var (h, rows) = Table(System.IO.Path.Combine(dir, "pop_s1789.tsv"));
            var mine = rows.Where(r => r[0] == "N" && r[4] == "フランス").ToList();
            Assert.Equal(6, mine.Count);
            Assert.Equal(fr.WarDeadCumulative - inject0, mine.Sum(r => Col(h, r, "warDeadK")), 9);                                   // 窓の和＝累計
            Assert.True(Col(h, mine[1], "warDeadK") >= 7 && Col(h, mine[3], "warDeadK") >= 3);
            Assert.True(Col(h, mine[2], "warDeadK") < 7, "窓 1 の増分が次の窓にも積まれた＝差分でなく累計を書いている");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static List<long> Digest(MultiNationSim sim)
    {
        var d = new List<long>();
        void A(double v) => d.Add(BitConverter.DoubleToInt64Bits(v));
        foreach (var n in sim.Nations)
        {
            var w = n.World; A(w.Treasury.Value); A(w.TotalMoney.Value); A(w.GovDebt.Value); A(w.PriceLevel); A(w.TotalPopulation); A(w.DiagBirths); A(w.DiagDeaths); A(w.PopulationDelta);
            foreach (var r in w.Regions) { A(r.SumPopulation()); A(r.SumSavings().Value); foreach (var m in r.Markets) { A(m.Price); A(m.Stock); } foreach (var p in r.Pops) { A(p.Savings.Value); A(p.Satisfaction); A(p.Size); A(p.Literacy); foreach (var ag in p.Ages) A(ag); } }
        }
        return d;
    }

    private static void RunMatch(MultiNationSim sim, int quarters, bool instr)
    {
        if (instr) Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        if (instr) Instruments.Phase(sim, 1789UL, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); if (instr) Instruments.Quarter(sim, 1789UL, q); } }
        finally { if (instr) Instruments.Close(sim); }
    }

    private static (string[] H, List<string[]> R) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "" => double.NaN, "nan" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    [Fact]
    public void 走行で全ての国に行が出て_窓の和が台帳に合い_計器を出しても軌道は変わらない()
    {
        const int quarters = 12;   // 3年
        var off = Make(); RunMatch(off, quarters, instr: false);
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_pop2_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "フランス");   // 挙げた国は 1 つ＝他の国は年末の行だけ
        try
        {
            var on = Make(); RunMatch(on, quarters, instr: true);
            Assert.Equal(Digest(off), Digest(on));   // 読むだけ＝全ビット一致（年齢の層も含む）
            var (h, rows) = Table(System.IO.Path.Combine(dir, "pop_s1789.tsv"));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            var nRows = rows.Where(r => r[0] == "N").ToList(); var cRows = rows.Where(r => r[0] == "C").ToList();
            Assert.NotEmpty(cRows);
            // 全ての国に行が出る：最後の年末（q=11）に、人口のある全ての国の N 行がある
            var alive = on.Nations.Where(n => n.World.TotalPopulation > 0).Select(n => Instruments.NameOf(n.World)).ToHashSet();
            var seen = nRows.Where(r => r[1] == "11").Select(r => r[4]).ToHashSet();
            Assert.True(alive.Count >= 4 && alive.SetEquals(seen), $"最後の年末の N 行の国 [{string.Join(',', seen)}] が人口のある国 [{string.Join(',', alive)}] と違う");
            // 挙げた国は毎四半期・他は年末だけ
            Assert.Equal(quarters, nRows.Count(r => r[4] == "フランス"));
            Assert.Equal(quarters / 4, nRows.Count(r => r[4] == "プロイセン"));
            // 窓の和＝台帳の累計（別の出所）：国ごとに N 行の出生・死・戦死の和が World の累計に合う
            foreach (var n in on.Nations)
            {
                string name = Instruments.NameOf(n.World); var mine = nRows.Where(r => r[4] == name).ToList();
                double b = mine.Sum(r => Col(h, r, "birthsK")), dn = mine.Sum(r => Col(h, r, "deathsNormalK") + Col(h, r, "deathsFamineK") + Col(h, r, "deathsOtherK")), war = mine.Sum(r => Col(h, r, "warDeadK"));
                Assert.True(Math.Abs(b - n.World.DiagBirths) <= 1e-7 * Math.Max(1, n.World.DiagBirths), $"{name} 出生の窓の和 {b} が台帳 {n.World.DiagBirths} に合わない");
                Assert.True(Math.Abs(dn - n.World.DiagDeaths) <= 1e-7 * Math.Max(1, n.World.DiagDeaths), $"{name} 死の窓の和 {dn} が台帳 {n.World.DiagDeaths} に合わない");
                Assert.True(Math.Abs(war - n.World.WarDeadCumulative) <= 1e-7 * Math.Max(1, n.World.WarDeadCumulative), $"{name} 戦死の窓の和が台帳に合わない");
            }
            int finiteV = 0, withDeaths = 0, finiteTfr = 0, l60Rows = 0; bool vNeg = false, mOk = false;
            foreach (var r in nRows)
            {
                // 層別の死の和＝平常＋飢え
                double bands = 0; for (int k = 0; k < Pop.Layout.Bands; k++) bands += Col(h, r, "deathsBand" + k + "K");
                double tot = Col(h, r, "deathsNormalK") + Col(h, r, "deathsFamineK");
                Assert.True(Math.Abs(bands - tot) <= 1e-9 * Math.Max(1, tot), $"層別の死の和 {bands} が平常＋飢え {tot} に合わない");
                Assert.True(Col(h, r, "deathsBand0K") <= tot + 1e-9);
                // ★(363) 反証役 pa〔v〕低#3：deathsOtherK は〈台帳の差−平常−飢え〉の残差＝窓の和の一致だけでは hook の数え過ぎが残差の負に隠れる＝負でないことを見る
                double oth = Col(h, r, "deathsOtherK");
                Assert.True(!(oth < -1e-9 * Math.Max(1, tot)), $"疫病ほかの死 {oth} が負＝hook が平常か飢えの死を数え過ぎている");
                double s10 = Col(h, r, "s10Share"); if (!double.IsNaN(s10)) Assert.InRange(s10, 0, 1 + 1e-12);
                Assert.Equal(double.IsNaN(Col(h, r, "mu0PerYear")), double.IsNaN(s10));
                // 10 歳まで生き残る確率＝e^(−Σ層の死の率×その層が 0〜10 歳に占める年数)（8層は 0〜9 歳の層 10 年＝e^(−10μ0)・11層は 0 歳 1 年・1〜5 歳 5 年・6〜10 歳 4 年〔10 歳の誕生日まで〕）＝年齢の境からの手の算術
                if (!double.IsNaN(s10)) { double ex = 0; var lay = Pop.Layout; for (int k = 0; k < lay.Bands - 1; k++) { double yrs = Math.Min(10.0, lay.Upper[k]) - Math.Max(0.0, lay.Lower[k]); if (yrs > 0) ex += Col(h, r, "mu" + k + "PerYear") * yrs; } Assert.True(Math.Abs(Math.Exp(-ex) - s10) <= 1e-8, "s10 が e^(−Σμ×年数) に合わない"); }
                double l60 = Col(h, r, "l60Years"); int oa = Pop.Layout.OldBandA, ob = Pop.Layout.OldBandB;   // 60 歳以後の余命を読む 2 層（8層は 6・7＝60〜69・70以上／11層は 9・10＝61〜70・71以上）
                if (!double.IsNaN(l60)) { double l60x = Instruments.PopL60(Col(h, r, "mu" + oa + "PerYear"), Col(h, r, "mu" + ob + "PerYear")); Assert.True(Math.Abs(l60x - l60) <= 1e-8 * Math.Abs(l60x), $"余命 {l60} が率から作り直した {l60x} に合わない"); l60Rows++; }
                else Assert.True(!(Col(h, r, "mu" + oa + "PerYear") > 0) || !(Col(h, r, "mu" + ob + "PerYear") > 0), "余命が NaN なのに老いた 2 層の死の率が正");
                double tfr = Col(h, r, "tfrProxy"); if (!double.IsNaN(tfr)) { Assert.InRange(tfr, 0, 15); finiteTfr++; }
                // ★(434) 粗出生率は窓の人口×月が 0 の行（その窓の途中で建国した国の最初の年末の行・InstrumentsPop の pm が NaN）では NaN と自己申告する＝NaN を許すのは出生も 0 のときだけ
                double cbrV = Col(h, r, "cbrPerMille");
                Assert.True((double.IsNaN(cbrV) ? Col(h, r, "birthsK") == 0 : cbrV >= 0) && Col(h, r, "popK") > 0, $"人口の行が壊れている {r[4]} q={r[1]} popK={Col(h, r, "popK")} cbrPerMille={cbrV} birthsK={Col(h, r, "birthsK")}");
                if (tot > 0) withDeaths++;
                double v = Col(h, r, "vNoUtilityIndex"); if (!double.IsNaN(v)) { finiteV++; if (v < 0) vNeg = true; }
                double m = Col(h, r, "marriedMNewShare"); if (!double.IsNaN(m)) { Assert.InRange(m, 0, 0.85); mOk = true; }
                double ts = Col(h, r, "tfrShadow"); if (!double.IsNaN(ts)) Assert.InRange(ts, 0, 7.5 + 1e-9);
            }
            // 対照（何もしない番人にしない）
            Assert.True(withDeaths > 10 && finiteTfr > 10, "死・出生の行が少ない");
            Assert.True(finiteV > 0 && mOk && l60Rows > 0, "影の列に値の入った行が無い＝籠か年齢の読みが来ていない");
            // C 行：影の列に値が入った行がある・流れの列は空
            Assert.True(cRows.Any(r => !double.IsNaN(Col(h, r, "vNoUtilityIndex"))), "C 行の影が全て NaN");
            Assert.All(cRows, r => Assert.True(double.IsNaN(Col(h, r, "birthsK")) && double.IsNaN(Col(h, r, "mu0PerYear"))));
            // ★(478) age_s（層ごとの行 B と国の行 N）：層の和＝人口・層ごとの死の和と出生が pop_s の N 行と合う・8層では乳児の列は nan・集計の年齢の範囲は pop_s と同じ
            var (ah, arows) = Table(System.IO.Path.Combine(dir, "age_s1789.tsv"));
            Assert.All(arows, r => Assert.Equal(ah.Length, r.Length));
            var aN = arows.Where(r => r[0] == "N").ToList(); var aB = arows.Where(r => r[0] == "B").ToList();
            int nbands = Pop.Layout.Bands; var layout = Pop.Layout;
            Assert.Equal(nRows.Count, aN.Count); Assert.Equal(nbands * aN.Count, aB.Count);
            var popByKey = nRows.ToDictionary(r => (r[1], r[4]));
            foreach (var r in aN)
            {
                var pr = popByKey[(r[1], r[4])];
                Assert.Equal((double)nbands, Col(ah, r, "bands"));
                double pk = Col(ah, r, "popK");
                Assert.True(Math.Abs(Col(ah, r, "sumBandsMinusPopK")) <= 1e-9 * pk, $"層の和が人口に合わない {r[4]} q={r[1]}");
                if (nbands == 8) Assert.True(double.IsNaN(Col(ah, r, "infantDeathsPerBirth")) && double.IsNaN(Col(ah, r, "infantShare")), "8層で乳児の列が nan でない");
                else Assert.True(!double.IsNaN(Col(ah, r, "infantShare")) && Col(ah, r, "infantShare") > 0, "11層で乳児の割合が出ていない");
                Assert.Equal(Col(h, pr, "share0to14"), Col(ah, r, "share0to14"), 9);
                Assert.Equal(Col(h, pr, "share60plus"), Col(ah, r, "old60plusShare"), 9);
                Assert.True(Col(ah, r, "work15to64Share") > 0.2 && Col(ah, r, "old65plusShare") >= 0);
                Assert.True(Col(ah, r, "popK") > 0 && Math.Abs(Col(ah, r, "popK") - Col(h, pr, "popK")) <= 1e-9 * pk, "popK が pop_s と違う");
            }
            foreach (var g in aB.GroupBy(r => (r[1], r[4])))
            {
                var pr = popByKey[g.Key]; var bs = g.ToList();
                Assert.Equal(nbands, bs.Count);
                Assert.Equal(Enumerable.Range(0, nbands).Select(i => (double)i), bs.Select(r => Col(ah, r, "bandIndex")));
                double dsum = bs.Sum(r => Col(ah, r, "deathsK")), dpop = Col(h, pr, "deathsNormalK") + Col(h, pr, "deathsFamineK");
                Assert.True(Math.Abs(dsum - dpop) <= 1e-9 * Math.Max(1, dpop), $"層の死の和 {dsum} が pop_s の平常＋飢え {dpop} に合わない");
                Assert.Equal(Col(h, pr, "birthsK"), Col(ah, bs[0], "birthsK"), 9);   // 出生は層 0 の行だけに載る
                Assert.All(bs.Skip(1), r => Assert.Equal(0.0, Col(ah, r, "birthsK")));
                for (int b = 0; b < nbands; b++)
                {
                    double m1 = Col(h, pr, "mu" + b + "PerYear"), m2 = Col(ah, bs[b], "muPerYear");
                    Assert.True(double.IsNaN(m1) ? double.IsNaN(m2) : Math.Abs(m1 - m2) <= 1e-9, $"層 {b} の死の率が pop_s と違う {m1} {m2}");
                }
                Assert.Equal(0.0, Col(ah, bs[0], "lowerAge")); Assert.Equal(layout.Lower[nbands - 1], Col(ah, bs[nbands - 1], "lowerAge")); Assert.True(double.IsPositiveInfinity(Col(ah, bs[nbands - 1], "upperAge")));
            }
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
