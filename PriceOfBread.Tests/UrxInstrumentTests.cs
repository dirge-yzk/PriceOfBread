using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(649) 都市と農村の規模の差の区間別の表（<c>urx_s</c>）と PopDecision の駆動の表（<c>urxpd_s</c>）の番人（読むだけの計器・恒等）。
/// ①駆動の写し：<see cref="Instruments.UrxPdDrive"/> の flow が、同じ盤面で本物の <see cref="PopDecision.Run"/> を走らせた前後の農村・都市の規模の差と一致する（期待値は本物の Run の前後の差から作る＝計器の式を再利用しない）。
///   農村→都市・都市→農村・天井で切れる・飢えの門・農奴の門の 5 通りの盤面。
/// ②区間の検算（事前登録 C1・C2）：全ての区間の差の和＝最後の水準−最初の水準（国×群）・最後の水準が世界の POP の数え直しに合う。社会移動の区間が実際に切り出されている。
/// ③軌道が計器で変わらないことは InstrumentsTests の全ビット一致の番人が見る（挙げる国は *）。
/// </summary>
[Collection("Instruments")]
public class UrxInstrumentTests
{
    private static Scenario Famine(ulong s) => new() { Seed = s, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() };

    private static (Region r, Pop rural, Pop urban, Coefficients c) Board(double incRural, double incUrban, double fillRural, double fillUrban, bool capital)
    {
        var (_, r, rural, urban, c) = BoardW(incRural, incUrban, fillRural, fillUrban, capital);
        return (r, rural, urban, c);
    }

    private static (World w, Region r, Pop rural, Pop urban, Coefficients c) BoardW(double incRural, double incUrban, double fillRural, double fillUrban, bool capital)
    {
        var sim = new Simulation(Famine(1), new NullAgent());
        var w = sim.World; var r = w.Regions[0];
        Pop Of(int cls) { foreach (var p in r.Pops) if (p.ClassId == cls) return p; throw new System.InvalidOperationException(); }
        var rural = Of(0); var urban = Of(1);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = fillRural * rural.FoodNeed; rural.EarnedPerMonth = new MoneyPerMonth(incRural * rural.Size);
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = fillUrban * urban.FoodNeed; urban.EarnedPerMonth = new MoneyPerMonth(incUrban * urban.Size);
        r.CapitalEnabled = capital;
        return (w, r, rural, urban, w.Coef);
    }

    private static void Match(string what, double incRural, double incUrban, double fillRural, double fillUrban, bool capital, double serfFree = 1.0, Action<Coefficients>? tweak = null, bool expectFlowNonZero = true)
    {
        var (r, rural, urban, c) = Board(incRural, incUrban, fillRural, fillUrban, capital);
        tweak?.Invoke(c);
        Assert.True(Instruments.UrxPdDrive(c, r, serfFree, out var d), what + "：対が取れない");
        double u0 = urban.Size, r0 = rural.Size;
        PopDecision.Run(r, c, serfFree);
        double actual = urban.Size - u0;   // 農村→都市が正
        Assert.True(Math.Abs(actual - d.Flow) <= 1e-9 * Math.Max(1, Math.Abs(actual)), $"{what}：計器の flow {d.Flow} と本物の Run の都市の増分 {actual} が違う");
        Assert.True(Math.Abs((r0 - rural.Size) - actual) <= 1e-9 * Math.Max(1, Math.Abs(actual)), $"{what}：農村の減りと都市の増えが合わない（前提）");
        if (expectFlowNonZero) Assert.True(Math.Abs(actual) > 1e-9, $"{what}：flow が 0＝この盤面は何も検査していない");
    }

    [Fact]
    public void 駆動の写しは本物のPopDecisionの前後の差と一致する()
    {
        Match("農村→都市", 0.1, 0.3, 0.9, 0.9, capital: false);
        Match("都市→農村", 0.3, 0.1, 0.9, 0.9, capital: false);
        Match("天井なし", 0.1, 0.5, 0.9, 0.9, capital: true);
        Match("飢えの門が閉じる", 0.1, 0.3, 0.9, 0.2, capital: false, expectFlowNonZero: false);
        Match("農奴の門が絞る", 0.1, 0.3, 0.9, 0.9, capital: false, serfFree: 0.25, tweak: c => c.SerfMobilityGate = 1.0);
    }

    [Fact]
    public void 駆動の写しは天井と門の印を正しく立てる()
    {
        var (r, _, _, c) = Board(0.1, 0.5, 0.9, 0.9, capital: true);
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var dOff));
        Assert.False(dOff.CapBound, "天井の印が立った"); Assert.Equal(dOff.TargetPre, dOff.TargetCapped, 12);   // 天井なし＝印が立たず目標は切られない
        var (r2, _, _, c2) = Board(0.1, 0.3, 0.9, 0.2, capital: false);
        Assert.True(Instruments.UrxPdDrive(c2, r2, 1.0, out var d2));
        Assert.True(d2.FamineGated && d2.Flow == 0, "飢えの門の印が立たない");
        var (r3, _, _, c3) = Board(0.1, 0.3, 0.9, 0.9, capital: false); c3.SerfMobilityGate = 1.0;
        Assert.True(Instruments.UrxPdDrive(c3, r3, 0.25, out var d3));
        Assert.True(d3.SerfGated, "農奴の門の印が立たない");
        var (r4, _, _, c4) = Board(0.1, 0.3, 0.9, 0.9, capital: false);
        Assert.True(Instruments.UrxPdDrive(c4, r4, 1.0, out var d4));
        Assert.False(d4.CapBound || d4.SerfGated || d4.FamineGated, "門の印が立ちすぎ（対照）");
    }

    private static (string[] H, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double D(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void 区間の差の和は水準の変化に一致し最後の水準は世界の数え直しに合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urx_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
            foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            const int Q = 16;
            try { for (int q = 0; q < Q; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
            finally { Instruments.Close(sim); }
            var (h, rows) = Table(System.IO.Path.Combine(dir, "urx_s1789.tsv"));
            Assert.Equal("quarter,year,nation,group,section,months,dUrbanK,dRuralK", string.Join(',', h));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            var sections = new HashSet<string>(rows.Select(r => r[4]));
            Assert.Contains("月次政治", sections); Assert.Contains("月次人口", sections); Assert.Contains("社会移動", sections);
            int checkedNations = 0;
            foreach (var nat in rows.Select(r => r[2]).Distinct())
                foreach (var grp in new[] { "Core", "Other" })
                {
                    var mine = rows.Where(r => r[2] == nat && r[3] == grp).ToList();
                    var bas = mine.Single(r => r[4] == "_base");
                    var lastQ = mine.Where(r => r[4] == "_level").Max(r => int.Parse(r[0]));
                    var lvl = mine.Single(r => r[4] == "_level" && int.Parse(r[0]) == lastQ);
                    double sumU = mine.Where(r => !r[4].StartsWith('_')).Sum(r => D(r[6])), sumR = mine.Where(r => !r[4].StartsWith('_')).Sum(r => D(r[7]));
                    double dU = D(lvl[6]) - D(bas[6]), dR = D(lvl[7]) - D(bas[7]);
                    Assert.True(Math.Abs(sumU - dU) <= 1e-6 * Math.Max(1, Math.Abs(D(lvl[6]))), $"{nat}/{grp} 都市の区間の和 {sumU} と水準の変化 {dU} が違う");
                    Assert.True(Math.Abs(sumR - dR) <= 1e-6 * Math.Max(1, Math.Abs(D(lvl[7]))), $"{nat}/{grp} 農村の区間の和 {sumR} と水準の変化 {dR} が違う");
                    checkedNations++;
                }
            Assert.True(checkedNations >= 8, "国×群が少ない");
            // C2：最後の水準＝世界の POP の数え直し（国の Core／Other）
            foreach (var n in sim.Nations)
            {
                string nm = Instruments.NameOf(n.World);
                foreach (var (grp, core) in new[] { ("Core", true), ("Other", false) })
                {
                    double u = 0, r = 0;
                    foreach (var rg in n.World.Regions) { if ((rg.Tier == RegionTier.Core) != core) continue; u += rg.SumUrbanPopulation(); r += rg.SumRuralPopulation(); }
                    var lvl = rows.Last(x => x[2] == nm && x[3] == grp && x[4] == "_level");
                    Assert.True(Math.Abs(D(lvl[6]) - u) <= 1e-6 * Math.Max(1, u), $"{nm}/{grp} 最後の都市の水準 {lvl[6]} と数え直し {u}");
                    Assert.True(Math.Abs(D(lvl[7]) - r) <= 1e-6 * Math.Max(1, r), $"{nm}/{grp} 最後の農村の水準 {lvl[7]} と数え直し {r}");
                }
            }
            // urxpd_s：本国の州の月次の行があり、regionMonths が正
            var (ph, prow) = Table(System.IO.Path.Combine(dir, "urxpd_s1789.tsv"));
            Assert.Equal(33, ph.Length);   // (713) urb4：末尾に wAbsGsym（25→26）・(1296) F0 の 7 列（26→33）
            Assert.True(prow.Count > 0 && prow.Any(r => D(r[3]) > 0), "urxpd の行が無い");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    // ---------------------------------------------------------------- (697) 往復の表 urxrt_s と州の生産の列（region_s の末尾 6 列）----------------------------------------------------------------

    /// <summary>★(697) 往復の表の口（Pop.Blend の末尾）が PopDecision の流れに合う：期待値は本物の Run の前後の都市の規模の差（計器の外の読み）から作る。農村→都市は入り・都市→農村は出・反対側は 0。</summary>
    [Theory]
    [InlineData(0.1, 0.3, true)]
    [InlineData(5.0, 0.01, false)]
    public void 往復の表の口はPopDecisionの流れに合う(double incRural, double incUrban, bool toUrban)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urxrt_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, _, urban, c) = BoardW(incRural, incUrban, 0.9, 0.9, capital: false);
            Instruments.Begin(w);   // 月初の写し：POP を台帳の持ち主へ登録し Active を立てる
            double u0 = urban.Size;
            PopDecision.Run(r, c, 1.0);
            double actual = urban.Size - u0;   // 計器の外の読み＝農村→都市が正
            Assert.True(Math.Abs(actual) > 1e-9, "前提：この盤面で人が動いていない");
            var peek = Instruments.UrxRtPeek(w);
            Assert.True(peek.TryGetValue("PopDecision", out var pd), "PopDecision の路が積まれていない：" + string.Join(",", peek.Keys));
            Assert.True((actual > 0) == toUrban, $"前提：この盤面の向きが違う（都市の増分 {actual}）");
            double expIn = actual > 0 ? actual : 0, expOut = actual < 0 ? -actual : 0;
            Assert.True(Math.Abs(pd.In - expIn) <= 1e-9 * Math.Max(1, Math.Abs(actual)), $"入り {pd.In} と期待 {expIn}");
            Assert.True(Math.Abs(pd.Out - expOut) <= 1e-9 * Math.Max(1, Math.Abs(actual)), $"出 {pd.Out} と期待 {expOut}");
            Assert.Equal(1.0, pd.Calls);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }


    /// <summary>★(697) 社会移動の全ての人の移し（Meritocracy.Step）の前後で、往復の表の積みの純（入−出の全ての路の和）が国の都市の規模の差に一致する（期待値は計器の外の世界の POP の数え直し）。
    /// 動きが全く無い盤面で通らない（非空）こと。州の生産の列は factory_s の元（CapRow）と別の経路で数えた値に一致する。</summary>
    [Fact]
    public void 往復の積みの純は都市の規模の差に合い_州の生産の列は工場の表と合う()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_urxrt2_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent());
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            const int Q = 12;
            for (int q = 0; q < Q; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            var caps = sim.Nations.Select(n => Instruments.CapRow(n.World)).ToList();   // 最後の四半期の断面（下の社会移動の前に取る＝生産者は動かない）
            double moved = 0;
            foreach (var n in sim.Nations)
            {
                var w = n.World;
                double U() { double u = 0; foreach (var rg in w.Regions) u += rg.SumUrbanPopulation(); return u; }
                Instruments.Begin(w);   // 新しい POP を登録して Active を立て直す
                var before = Instruments.UrxRtPeek(w);
                double u0 = U();
                Meritocracy.Step(w, w.Coef);
                double u1 = U();
                var after = Instruments.UrxRtPeek(w);
                double net = 0, gross = 0;
                foreach (var kv in after)
                {
                    before.TryGetValue(kv.Key, out var b);
                    net += (kv.Value.In - b.In) - (kv.Value.Out - b.Out); gross += (kv.Value.In - b.In) + (kv.Value.Out - b.Out);
                }
                Assert.True(Math.Abs(net - (u1 - u0)) <= 1e-6 * Math.Max(1, Math.Abs(u1 - u0)), $"{Instruments.NameOf(w)}：積みの純 {net} と都市の規模の差 {u1 - u0}");
                moved += gross;
            }
            Assert.True(moved > 1e-6, "前提：社会移動で都市の POP に出入りが 1 件も無い＝この番人は何も検査していない");
            Instruments.Close(sim);
            // 州の生産の列：region_s の最後の四半期の行の和＝CapRow（factory_s の元）の工場の列
            var (h, rows) = Table(System.IO.Path.Combine(dir, "region_s1789.tsv"));
            Assert.EndsWith(Instruments.RegionProdHeaderText, string.Join('\t', h));
            Assert.All(rows, x => Assert.Equal(h.Length, x.Length));
            int ci(string c2) { int i = Array.IndexOf(h, c2); Assert.True(i >= 0, "列が無い " + c2); return i; }
            int lastQ = rows.Max(x => int.Parse(x[ci("quarter")]));
            var capHead = Instruments.CapHeaderLine.Split('\t');
            for (int ni = 0; ni < sim.Nations.Count; ni++)
            {
                var cap = caps[ni];
                var mine = rows.Where(x => int.Parse(x[ci("quarter")]) == lastQ && x[ci("nationIndex")] == ni.ToString()).ToList();
                Assert.NotEmpty(mine);
                double Sum(string col) => mine.Sum(x => D(x[ci(col)]));
                int at(string col) => Array.IndexOf(capHead, col) - 4;   // CapRow は quarter・year・nationUid・nation の 4 列を持たない
                Assert.Equal(cap[at("factoryActiveCount")], Sum("factoryActiveCount"));
                Assert.Equal(cap[at("factoryBankruptCount")], Sum("factoryBankruptCount"));
                Assert.True(Math.Abs(cap[at("outputRefValuePerMonth")] - Sum("factoryOutputRefValuePerMonth")) <= 1e-6 * Math.Max(1, Math.Abs(cap[at("outputRefValuePerMonth")])), "工場の産出が factory_s の元と合わない");
                Assert.True(Sum("factoryOutputRefValuePerMonth") > 0 && Sum("extractionOutputRefValuePerMonth") > 0 && Sum("extractionActiveCount") > 0, "前提：工場か採取の産出が 0＝この番人は何も検査していない");
                // 採取の列は factory_s に対の列が無い＝別の書き方（全生産者の絞り込み）で数え直す：労働を買わず・労働財でなく・倒産でなく・操業か産出が正
                var ext = sim.Nations[ni].World.Regions.SelectMany(rg => rg.Producers).Where(s => !s.BuysLabor && !Catalog.IsLaborGood(s.GoodId)).ToList();
                Assert.Equal(ext.Count(s => !s.Bankrupt && (s.Scale != 0 || s.Output != 0)), Sum("extractionActiveCount"));
                Assert.Equal(ext.Count(s => s.Bankrupt), Sum("extractionBankruptCount"));
                double extOut = ext.Where(s => !s.Bankrupt && (s.Scale != 0 || s.Output != 0)).Sum(s => s.Output * Catalog.RefPrices[s.GoodId]);
                Assert.True(Math.Abs(extOut - Sum("extractionOutputRefValuePerMonth")) <= 1e-6 * Math.Max(1, extOut), $"採取の産出 {Sum("extractionOutputRefValuePerMonth")} と数え直し {extOut}");
                Assert.All(mine, x => { foreach (var col in new[] { "factoryOutputRefValuePerMonth", "extractionOutputRefValuePerMonth", "factoryActiveCount", "factoryBankruptCount", "extractionActiveCount", "extractionBankruptCount" }) Assert.True(D(x[ci(col)]) >= 0); });
            }
            // urxrt_s の表：行があり、列が揃い、PopDecision の路が出ている
            var (rh, rrows) = Table(System.IO.Path.Combine(dir, "urxrt_s1789.tsv"));
            Assert.Equal(Instruments.UrxRtHeaderText, string.Join('\t', rh));
            Assert.All(rrows, x => Assert.Equal(rh.Length, x.Length));
            Assert.Contains(rrows, x => x[3] == "PopDecision" && (D(x[4]) > 0 || D(x[5]) > 0));
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    // ---------------------------------------------------------------- (1296) F0 の列（famineBackK・famineRegainK・famineRegainRegionMonths・oppSignRegionMonths・wShortVU・wShortVR・wGsymA） ----------------------------------------------------------------

    /// <summary>英国の開幕の州（本国格）の対の盤面。住居の門は閾値 0.9 で閉じる（家賃 0）。対の都市と農村の規模を同じにする（今の割合 0.5）。主食の値段を全て 2.0 に置く（p̄＝2.0 は重みに依らない）。</summary>
    private static (World w, Region r, Pop rural, Pop urban, Coefficients c) BoardF0(double earnedR, double subR, double earnedU, double fillR, double fillU)
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent()).World;
        var r = w.Region; var c = w.Coef;
        var (rural0, urban0) = PopDecision.MigrationPair(r);
        var rural = rural0!; var urban = urban0!;
        urban.Size = rural.Size;
        c.HousingUrbanThreshold = 0.9;
        Assert.True(r.SumUrbanPopulation() / r.SumPopulation() <= c.HousingUrbanThreshold, "前提：住居の門が閉じていない");
        Assert.Equal(RegionTier.Core, r.Tier);
        for (int g = 0; g < Math.Min(r.Markets.Length, Catalog.Goods.Length); g++)
            if (Catalog.Goods[g].Tier == GoodTier.Staple) r.Markets[g].Price = 2.0;
        rural.EarnedPerMonth = new MoneyPerMonth(earnedR * rural.Size); rural.SubsistenceValuePerMonth = new MoneyPerMonth(subR * rural.Size);
        urban.EarnedPerMonth = new MoneyPerMonth(earnedU * urban.Size);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = fillR * rural.FoodNeed;
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = fillU * urban.FoodNeed;
        r.CapitalEnabled = false;
        Assert.Equal(0.02, c.Mobility);
        c.UrbanFlowReal = 0;   // F0 の列は旧の式（シグモイド）の盤面で読む＝門と fg の枝に届く形
        c.FamineGateReturnsRural = 1;
        return (w, r, rural, urban, c);
    }

    private static double Gs(double vr, double vu) => vr + vu > 1e-9 ? 2.0 * (vu - vr) / (vr + vu) : 0.0;

    [Fact]
    public void F0_門の月の農村への流れが_famineBackK_に入り本物のRunの都市の減りと一致する()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_f0a_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            // 稼ぎは都市が高い（0.30＞0.10＝シグモイドの目標＞今＝流れ＞0＝門に入る）・自給 0.40 で V_r 0.50＞V_u 0.30＝g＝−0.5＝fg の枝で都市→農村
            var (w, r, rural, urban, c) = BoardF0(0.10, 0.40, 0.30, 0.9, 0.2);
            double us = urban.Size, g = Gs(0.10 + 0.40, 0.30);
            Assert.Equal(-0.5, g, 12);
            Instruments.UrxPd(w, r, 1.0);
            double u0 = urban.Size;
            PopDecision.Run(r, c, 1.0);
            double moved = u0 - urban.Size;   // 都市の減り（本物の Run）
            var f = Instruments.UrxPdPeekF0(w);
            Assert.Equal(0.02 * 0.5 * us, f.FamineBackK, 9);   // 手の算術：Mobility×|g|×都市の規模
            Assert.True(moved > 1e-6, "前提：門の月に都市から農村へ動いていない");
            Assert.Equal(moved, f.FamineBackK, 9);
            Assert.Equal(1, f.OppSignRegionMonths);   // シグモイドは都市へ・g は農村へ＝逆
            Assert.Equal(0, f.FamineRegainRegionMonths);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void F0_門の月から12か月の窓の都市への流れが_famineRegainK_に入り13か月目は入らない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_f0b_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, rural, urban, c) = BoardF0(0.10, 0.40, 0.30, 0.9, 0.2);
            long m0 = 100; w.Tick = m0 * Calendar.DaysPerMonth;
            Instruments.UrxPd(w, r, 1.0);   // 門の月
            // 門の後：都市も食べられる（門なし）・自給 0（g＞0＝シグモイドも g も都市へ）
            rural.SubsistenceValuePerMonth = new MoneyPerMonth(0.0); urban.FoodMet = 0.9 * urban.FoodNeed;
            double total = rural.Size + urban.Size;
            double target = 1.0 / (1.0 + Math.Exp(-(0.30 - 0.10) / 0.15));   // 手の算術：シグモイドの目標（温度 0.15）
            double perMonth = 0.02 * (target - 0.5) * total;                    // 旧の式の流れ（今の割合 0.5）
            Assert.Equal(0.15, c.MigrationTemperature);
            for (int k = 1; k <= 13; k++)
            {
                w.Tick = (m0 + k) * Calendar.DaysPerMonth;
                Instruments.UrxPd(w, r, 1.0);
                var f = Instruments.UrxPdPeekF0(w);
                int expectN = Math.Min(k, 12);
                Assert.Equal(expectN, f.FamineRegainRegionMonths);
                Assert.Equal(expectN * perMonth, f.FamineRegainK, 9);
            }
            Assert.True(perMonth > 1e-6, "前提：門の後の月に都市への流れが無い");
            Assert.Equal(1, Instruments.UrxPdPeekF0(w).OppSignRegionMonths);   // 門の月の 1 回だけ（門の後は両方都市へ＝同じ向き）
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void F0_oppSign_は符号が逆の月だけ数え_同じ向きは数えない()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_f0c_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var (w, r, rural, urban, c) = BoardF0(0.10, 0.0, 0.30, 0.9, 0.9);   // シグモイドも g も都市へ＝同じ向き
            Instruments.UrxPd(w, r, 1.0);
            Assert.Equal(0, Instruments.UrxPdPeekF0(w).OppSignRegionMonths);
            rural.EarnedPerMonth = new MoneyPerMonth(0.30 * rural.Size); urban.EarnedPerMonth = new MoneyPerMonth(0.10 * urban.Size);   // 両方農村へ＝同じ向き
            Instruments.UrxPd(w, r, 1.0);
            Assert.Equal(0, Instruments.UrxPdPeekF0(w).OppSignRegionMonths);
            rural.EarnedPerMonth = new MoneyPerMonth(0.10 * rural.Size); urban.EarnedPerMonth = new MoneyPerMonth(0.30 * urban.Size);
            rural.SubsistenceValuePerMonth = new MoneyPerMonth(0.40 * rural.Size);   // シグモイドは都市へ・g は農村へ＝逆
            Instruments.UrxPd(w, r, 1.0);
            Assert.Equal(1, Instruments.UrxPdPeekF0(w).OppSignRegionMonths);
            rural.SubsistenceValuePerMonth = new MoneyPerMonth(0.0);   // 自給を戻す＝両方都市へ＝同じ向き＝数えない
            Instruments.UrxPd(w, r, 1.0);
            Assert.Equal(1, Instruments.UrxPdPeekF0(w).OppSignRegionMonths);
            Assert.Equal(4, Instruments.UrxPdPeek(w).RegionMonths);   // 対照：4 か月とも数えた
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void F0_食べられなかった量の値と案Aのgは手の算術どおり_Vは0で切る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_f0d_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            // p̄＝2.0・u_r＝0.8×(1−0.9)＝0.08・u_u＝0.8×(1−0.2)＝0.64 → p̄u_r＝0.16・p̄u_u＝1.28
            var (w, r, rural, urban, c) = BoardF0(0.10, 0.40, 0.30, 0.9, 0.2);
            double total = rural.Size + urban.Size;
            Instruments.UrxPd(w, r, 1.0);
            var f = Instruments.UrxPdPeekF0(w);
            Assert.Equal(total * 0.16, f.WShortVR, 9);
            Assert.Equal(total * 1.28, f.WShortVU, 9);
            // V_r＝0.10＋0.40−0.16＝0.34・V_u＝max(0, 0.30−1.28)＝0 → g＝−2
            Assert.Equal(total * -2.0, f.WGsymA, 9);
            // 2 か月目：都市も食べられる（u_u＝0.08）→ V_u＝0.30−0.16＝0.14・V_r＝0.34
            urban.FoodMet = 0.9 * urban.FoodNeed;
            Instruments.UrxPd(w, r, 1.0);
            var f2 = Instruments.UrxPdPeekF0(w);
            Assert.Equal(total * (1.28 + 0.16), f2.WShortVU, 9);
            Assert.Equal(total * (0.16 + 0.16), f2.WShortVR, 9);
            Assert.Equal(total * (-2.0 + Gs(0.34, 0.14)), f2.WGsymA, 9);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
