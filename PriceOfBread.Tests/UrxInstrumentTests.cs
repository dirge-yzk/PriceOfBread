using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(649) 都市と農村の規模の差の区間別の表（<c>urx_s</c>）と PopDecision の駆動の表（<c>urxpd_s</c>）の番人（読むだけの計器・恒等）。
/// ①駆動の写し：<see cref="Instruments.UrxPdDrive"/> の flow が、同じ盤面で本物の <see cref="PopDecision.Run"/> を走らせた前後の農村・都市の規模の差と一致する（期待値は本物の Run の前後の差から作る＝計器の式を再利用しない）。
///   農村→都市・都市→農村・天井で切れる・飢えの門・農奴の門の 5 通りの盤面。計器の呼びが <c>UrbanIndSmoothed</c> を書かない。
/// ②区間の検算（事前登録 C1・C2）：全ての区間の差の和＝最後の水準−最初の水準（国×群）・最後の水準が世界の POP の数え直しに合う。社会移動の区間が実際に切り出されている。
/// ③軌道が計器で変わらないことは InstrumentsTests の全ビット一致の番人が見る（挙げる国は *）。
/// </summary>
[Collection("Instruments")]
public class UrxInstrumentTests
{
    private static Scenario Famine(ulong s) => new() { Seed = s, Kind = ScenarioKind.RegionalFamine, Coef = new Coefficients() };

    private static (Region r, Pop rural, Pop urban, Coefficients c) Board(double incRural, double incUrban, double fillRural, double fillUrban, bool capital)
    {
        var sim = new Simulation(Famine(1), new NullAgent());
        var w = sim.World; var r = w.Regions[0];
        Pop Of(int cls) { foreach (var p in r.Pops) if (p.ClassId == cls) return p; throw new System.InvalidOperationException(); }
        var rural = Of(0); var urban = Of(1);
        rural.FoodNeed = 0.8 * rural.Size; rural.FoodMet = fillRural * rural.FoodNeed; rural.EarnedPerMonth = new MoneyPerMonth(incRural * rural.Size);
        urban.FoodNeed = 0.8 * urban.Size; urban.FoodMet = fillUrban * urban.FoodNeed; urban.EarnedPerMonth = new MoneyPerMonth(incUrban * urban.Size);
        r.CapitalEnabled = capital;
        return (r, rural, urban, w.Coef);
    }

    private static void Match(string what, double incRural, double incUrban, double fillRural, double fillUrban, bool capital, double serfFree = 1.0, Action<Coefficients>? tweak = null, bool expectFlowNonZero = true)
    {
        var (r, rural, urban, c) = Board(incRural, incUrban, fillRural, fillUrban, capital);
        tweak?.Invoke(c);
        double smoothed0 = r.UrbanIndSmoothed;
        Assert.True(Instruments.UrxPdDrive(c, r, serfFree, out var d), what + "：対が取れない");
        Assert.Equal(smoothed0, r.UrbanIndSmoothed);   // 計器の呼びは平滑値を書かない
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
        Match("天井で切れる", 0.1, 0.5, 0.9, 0.9, capital: true, tweak: c => { c.UrbanCapOff = 0; c.BaseUrbanCap = 0.05; c.UrbanPerInd = 0; });
        Match("天井なし（旗 1）", 0.1, 0.5, 0.9, 0.9, capital: true, tweak: c => { c.UrbanCapOff = 1; c.BaseUrbanCap = 0.05; c.UrbanPerInd = 0; });   // ★(694) 合流（反証役 urb2〔bv〕低）：新しい既定（旗 1）で首都の州の計器の写しと本物の Run が一致すること
        Match("飢えの門が閉じる", 0.1, 0.3, 0.9, 0.2, capital: false, expectFlowNonZero: false);
        Match("農奴の門が絞る", 0.1, 0.3, 0.9, 0.9, capital: false, serfFree: 0.25, tweak: c => c.SerfMobilityGate = 1.0);
    }

    [Fact]
    public void 駆動の写しは天井と門の印を正しく立てる()
    {
        var (r, _, _, c) = Board(0.1, 0.5, 0.9, 0.9, capital: true); c.UrbanCapOff = 0; c.BaseUrbanCap = 0.05; c.UrbanPerInd = 0;   // ★(687) 天井の枝（旗 0）の印の番人
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var d));
        c.UrbanCapOff = 1;   // 旗 1＝天井なし＝印が立たず目標は切られない
        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var dOff));
        Assert.False(dOff.CapBound, "旗 1 で天井の印が立った"); Assert.Equal(dOff.TargetPre, dOff.TargetCapped, 12);
        c.UrbanCapOff = 0;
        Assert.True(d.CapBound, "天井で切れた印が立たない"); Assert.Equal(0.05, d.Cap, 12); Assert.True(d.TargetCapped < d.TargetPre);
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
            Assert.Equal(21, ph.Length); Assert.True(prow.Count > 0 && prow.Any(r => D(r[3]) > 0), "urxpd の行が無い");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
