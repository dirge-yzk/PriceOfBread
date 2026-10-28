using System.Reflection;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(605) 線 ind4：工業化度の読み手を盤面の量へ置き換える旗の束の番人＝機構と不変条件。事前登録 measurements/ind4_605_20261027_prereg.txt の J1〜J11。
/// 旗は全て既定 0（倒さない）。期待は手計算のリテラルか、実装と別の出所（盤面に自分で置いた数・別の関数）から作る。史実の桁は固定しない。</summary>
public class IndustrializationReaderTests
{
    private static Simulation Britain(ulong seed = 41) => new(NationConfig.Britain().ToScenario(seed), new NullAgent());

    // ---- J1 読み手の許可表（ラチェット）----------------------------------------------------------------------------
    // SimCore の非コメント行（// 以降を除く）で、工業化度の語を読む行の数のファイル別の許可表。増えたら赤＝新しい読み手を足さない（盤面の量を読む）。
    // 減らしたときは表を直す（下の「減った」も赤＝表が嘘にならない）。〈済〉の削除（賃金の天井・独占の解体・冷蔵の傾斜）は 0 行。旗の枝の中の旧の式は残る。
    private static readonly Regex Words = new(
        @"\b(TotalIndustrialization|ProductiveIndustrialization|MeanIndustrialization|IndustrialFrontier|UrbanIndSmoothed|OldVintageCapital|StrandedVintage|IndustryInvestRegionCap|avgInd|indPopSum|indPopWeight)\b|\.Industrialization\b|\bIndustrialization\s*=|\bcfg\.Industrialization\b|\bc\.Industrialization\b",
        RegexOptions.Compiled);

    private static readonly (string File, int Lines)[] Allowed =
    [
        ("CapitalMarket.cs", 1), ("Coefficients.cs", 1), ("Credit.cs", 1), ("Diplomacy.cs", 1), ("Discovery.cs", 1), ("EconomyStep.cs", 8), ("Ideology.cs", 1),
        ("Instruments.cs", 1), ("InstrumentsCapacity.cs", 1), ("InstrumentsEquipment.cs", 2), ("InstrumentsLawGate.cs", 1), ("InstrumentsModernize.cs", 1), ("InstrumentsScore.cs", 2),
        ("Megaprojects.cs", 4), ("Meritocracy.cs", 1), ("Model.cs", 5), ("MultiNationSim.cs", 3), ("NationBrief.cs", 1), ("NationConfig.cs", 1), ("OpeningBake.cs", 3),
        ("PopDecision.cs", 4), ("SelfPlay.cs", 3), ("Simulation.cs", 16), ("Territory.cs", 1), ("Validation.cs", 2), ("World.cs", 9),
    ];

    [Fact]
    public void 工業化度を読む行の数はファイル別の許可表のとおり()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var counts = new SortedDictionary<string, int>();
        foreach (var f in Directory.GetFiles(dir, "*.cs"))
        {
            int n = 0;
            foreach (var line in File.ReadAllLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? line[..c] : line;
                if (Words.IsMatch(code)) n++;
            }
            if (n > 0) counts[Path.GetFileName(f)] = n;
        }
        var allowed = Allowed.ToDictionary(x => x.File, x => x.Lines);
        var bad = new List<string>();
        foreach (var (file, n) in counts)
        {
            int a = allowed.GetValueOrDefault(file);
            if (n > a) bad.Add($"{file}: {n} 行（許可 {a}）＝増えた。工業化度を読まず盤面の量を読むこと");
            else if (n < a) bad.Add($"{file}: {n} 行（許可 {a}）＝減った。許可表を直すこと");
        }
        foreach (var (file, a) in allowed) if (!counts.ContainsKey(file)) bad.Add($"{file}: 0 行（許可 {a}）＝減った。許可表を直すこと");
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void 削除した係数は戻っていない_旗は全て既定0()
    {
        var c = new Coefficients();
        foreach (var gone in new[] { "WageClampIndScale", "MonopolyDissolveInd", "RefrigerationIndOnset", "RefrigerationIndSpan" })
            Assert.Null(typeof(Coefficients).GetField(gone));
        foreach (var flag in Flags)
        {
            var f = typeof(Coefficients).GetField(flag);
            Assert.True(f is not null, $"旗 {flag} が無い");
            Assert.Equal(0.0, (double)f!.GetValue(c)!);
        }
    }

    private static readonly string[] Flags =
    [
        "EpidemicSanitFromBoard", "IdeologyDevFromUrban", "UrbanCapOff", "CreditDevOff", "CapitalHeadroomFromReturn", "TradeWeightOpeningEven", "CapacityIndTermsOff",
    ];

    // ---- J9 対称：旗を読む行が IsPlayer を読まない ---------------------------------------------------------------------
    [Fact]
    public void 旗を読む行は_プレイヤーかどうかを読まない()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in Directory.GetFiles(dir, "*.cs"))
            foreach (var line in File.ReadAllLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? line[..c] : line;
                if (code.Contains("IsPlayer") && Flags.Any(code.Contains))
                    Assert.Fail($"{Path.GetFileName(f)}: 旗と IsPlayer が同じ行にある（AI とプレイヤーは同じ式を通る）: {code.Trim()}");
            }
    }

    // ---- J2 疫病の衛生 ------------------------------------------------------------------------------------------------
    private static double EpidemicDeaths(double flag, double ind, double infraFill)
    {
        var sim = Britain(); var w = sim.World; var c = w.Coef; var r = w.Region;
        c.EpidemicSanitFromBoard = flag;
        r.Industrialization = ind; r.OldVintageCapital = 0; r.StrandedVintage = 0;
        r.InfraFill = infraFill; r.SoapFill = 0; r.ElecInfraFill = 0;
        var m = typeof(World).GetMethod("ApplyEpidemicDeaths", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (double)m.Invoke(w, [r, c, 1.0, 1.0])!;
    }

    [Fact]
    public void 疫病_旗1は工業化度を動かしても致死率が動かず_衛生財の充足で動く()
    {
        Assert.Equal(EpidemicDeaths(1, 0.0, 0.5), EpidemicDeaths(1, 5.0, 0.5), 9);   // 工業化度は読まれない
        double lo = EpidemicDeaths(1, 1.0, 0.0), hi = EpidemicDeaths(1, 1.0, 1.0);
        Assert.True(lo > 0 && hi < lo, $"衛生が満ちても死者が減らない（充足 0:{lo}・1:{hi}）");
        Assert.Equal(1.0 - 0.7, hi / lo, 6);   // 手計算：致死率 ∝ (1−0.7×衛生)・衛生 0→1 で 0.3 倍（EpidemicSanitationWeight に依らない＝旗 1 は実衛生財だけ）
    }

    [Fact]
    public void 疫病_旗0は旧のまま工業化度が致死率を下げる()
    {
        double d0 = EpidemicDeaths(0, 0.0, 0.0), d5 = EpidemicDeaths(0, 5.0, 0.0);
        Assert.True(d5 < d0, $"旗 0 で工業化度が致死率を下げない（0:{d0}・5:{d5}）");
    }

    // ---- J3 思想の発展度 ------------------------------------------------------------------------------------------
    [Fact]
    public void 思想の発展度_旗1は世界最先端の工業化度を読まない_旗0は読む()
    {
        double Dev(double flag, double frontier)
        {
            var w = Britain().World; w.Coef.IdeologyDevFromUrban = flag; w.IndustrialFrontier = frontier;
            return Ideologies.EraGates(w).Dev;
        }
        Assert.Equal(Dev(1, 0.0), Dev(1, 9.0));
        Assert.True(Dev(0, 9.0) > Dev(0, 0.0), "旗 0 で世界最先端の工業化度が発展度に効かない（旧の式）");
        // 旗 1 の dev は都市労働者の割合そのもの（0〜1）
        var w1 = Britain().World; w1.Coef.IdeologyDevFromUrban = 1;
        double tot = 0, urb = 0; foreach (var p in w1.AllPops()) { tot += p.Size; if (p.IsUrban) urb += p.Size; }
        Assert.Equal(Math.Clamp(urb / tot, 0, 1), Ideologies.EraGates(w1).Dev, 12);
    }

    [Fact]
    public void 思想の発展度_多国の四半期の同期は旗1で最先端の工業化度を注入しない_旗0は注入する()
    {
        double Frontier(double flag)
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], seed: 1789);
            foreach (var n in sim.Nations) n.World.Coef.IdeologyDevFromUrban = flag;
            sim.StepQuarter();
            return sim.Nations.Max(n => n.World.IndustrialFrontier);
        }
        Assert.Equal(0.0, Frontier(1));
        Assert.True(Frontier(0) > 0, "旗 0 で世界最先端の工業化度が注入されない（旧）");
    }

    // ---- J4 都市化の天井 ------------------------------------------------------------------------------------------
    private static double Headroom(Region r, Coefficients c)
        => (double)typeof(Meritocracy).GetMethod("UrbanHeadroom", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [r, c])!;

    [Fact]
    public void 都市化の天井_旗1は天井が無く平滑化の状態も書かない_旗0は旧のまま()
    {
        var sim = Britain(); var w = sim.World; var r = w.Region; var c = w.Coef;
        r.UrbanIndSmoothed = -1.0;
        Pop? rural = null, urban = null;
        foreach (var p in r.Pops) { if (p.Closed || p.IsCriminal) continue; if (p.IsUrban) urban ??= p; else rural ??= p; }
        Assert.True(rural is not null && urban is not null);
        rural!.Size = 900; urban!.Size = 100;   // 組の合計 1000・都市 100（手計算の期待の元）
        c.UrbanCapOff = 1; r.Industrialization = 0.0; r.OldVintageCapital = 0;
        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));
        r.Industrialization = 5.0;
        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));
        PopDecision.Run(r, c);
        Assert.Equal(-1.0, r.UrbanIndSmoothed);   // 旗 1 は平滑化の状態を書かない
        c.UrbanCapOff = 0;
        rural.Size = 900; urban.Size = 100;   // Run が動かした分を戻す
        r.Industrialization = 0.0; double h0 = Headroom(r, c);
        r.Industrialization = 5.0; double h5 = Headroom(r, c);
        Assert.Equal(c.BaseUrbanCap * 1000 - 100, h0, 9);                          // 手計算：BaseUrbanCap×組の 1000 − 都市 100（係数から作る）
        Assert.Equal((c.BaseUrbanCap + c.UrbanPerInd * 5.0) * 1000 - 100, h5, 9);  // 工業化度 5 の天井
        PopDecision.Run(r, c);
        Assert.True(r.UrbanIndSmoothed >= 0, "旗 0 で平滑化の状態が書かれない（旧）");
    }

    // ---- J5 信用の深さ ------------------------------------------------------------------------------------------------
    [Fact]
    public void 信用の深さ_旗1は工業化度の倍率が無い_旗0は手計算の旧の式()
    {
        var w = Britain().World; var c = w.Coef;
        double fl = c.CreditMultiplierDevFloor, sc = c.CreditDevScale;   // 係数から作る（較正が変わっても赤にしない）
        Assert.Equal(fl, Credit.DevFactor(w, c, 0.0), 12);                                  // 手計算：dev=0 → 床
        Assert.Equal(fl + (1.0 - fl) * 0.5, Credit.DevFactor(w, c, 0.5 * sc), 12);          // dev/scale=0.5
        Assert.Equal(1.0, Credit.DevFactor(w, c, sc), 12);
        Assert.Equal(1.0, Credit.DevFactor(w, c, 3.0 * sc), 12);                            // 満額で頭打ち（Clamp01）
        foreach (var rr in w.Regions) { rr.Industrialization = 0; rr.OldVintageCapital = 0; }
        Assert.Equal(fl, Credit.DevFactor(w, c, null), 12);                                 // 渡されなければ盤面の平均を読む（旧）
        c.CreditDevOff = 1;
        foreach (double m in new double?[] { 0.0, 0.2, 3.0, null }.Select(x => x ?? 0.0))
            Assert.Equal(1.0, Credit.DevFactor(w, c, m), 12);
        Assert.Equal(1.0, Credit.DevFactor(w, c, null), 12);
    }

    // ---- J6 外資の余地 ------------------------------------------------------------------------------------------------
    [Fact]
    public void 外資の余地_旗1は設備の収益の持ち分_旗0は目標から工業化度を引く旧の式()
    {
        var w = Britain().World; var c = w.Coef; var r = w.Region;
        r.Industrialization = 1.5; r.OldVintageCapital = 0.5;   // 総量 2.0（州が 1 つの世界で平均＝2.0）
        double mean = w.MeanIndustrialization;
        Assert.Equal(Math.Max(0, 4.0 - mean), CapitalMarket.Headroom(c, w), 12);                 // 旗 0＝旧（手計算：目標 4.0）
        c.CapitalHeadroomFromReturn = 1;
        Assert.Equal(c.CapitalIndTarget * w.ModernizeReturnShare(), CapitalMarket.Headroom(c, w), 12);   // 旗 1＝別の関数 ModernizeReturnShare の直読み
        // 旗 1 の向き：州の資本の在庫が増えると設備の収益が飽和して余地が下がる（間接の読み＝直読みは無い）
        r.Industrialization = 0.0; r.OldVintageCapital = 0;
        double hLow = CapitalMarket.Headroom(c, w);
        r.Industrialization = c.IndustryInvestRegionCap + 1.0;
        double hCap = CapitalMarket.Headroom(c, w);
        Assert.True(hCap >= 0 && hLow > hCap, $"余地は州の資本の在庫が増えると下がる（設備の収益の飽和）: 低 {hLow} 上限超 {hCap}");
    }

    // ---- J7 交易の比重 ------------------------------------------------------------------------------------------------
    [Fact]
    public void 交易の比重_旗1は入力データの工業化度で傾けない_旗0は手計算の旧の式()
    {
        var cfgs = new[] { NationConfig.France(), NationConfig.Britain(), NationConfig.Japan() };
        double baseW = (double)typeof(MultiNationSim).GetField("DefaultTradeWeightBase", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        double slope = (double)typeof(MultiNationSim).GetField("TradeWeightIndustrializationSlope", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        var s0 = new MultiNationSim(cfgs, seed: 1789);
        for (int i = 0; i < cfgs.Length; i++)
            Assert.Equal(baseW + slope * cfgs[i].Industrialization, s0.Nations[i].World.SelfPower!.TradeWeight, 12);   // 旧：基準＋傾き×入力（定数は MultiNationSim から読む）
        var s1 = new MultiNationSim(cfgs, seed: 1789, coef: new Coefficients { TradeWeightOpeningEven = 1 });
        for (int i = 0; i < cfgs.Length; i++) Assert.Equal(baseW, s1.Nations[i].World.SelfPower!.TradeWeight, 12);
    }

    // ---- J8 扶養力 ---------------------------------------------------------------------------------------------------
    private static double Capacity(double flag, double ind)
    {
        var sim = Britain(); var w = sim.World; var r = w.Region;
        w.Coef.CapacityIndTermsOff = flag;
        r.Industrialization = ind; r.OldVintageCapital = 0; r.StrandedVintage = 0;
        sim.DemographicStep();
        return w.DiagCapacity;
    }

    [Fact]
    public void 扶養力_旗1は工業化度の2項を外す_旗0は旧のまま()
    {
        Assert.True(Capacity(0, 4.0) != Capacity(0, 0.0), "旗 0 で扶養力が工業化度に依らない（旧の 2 項）");
        Assert.Equal(Capacity(1, 0.0), Capacity(1, 4.0), 9);
    }

    // ---- J11 表示：最終の表の物差し ---------------------------------------------------------------------------------
    [Fact]
    public void 鉄の産出の項の番号は国力の項の並びの鉄を指す()
    {
        var a = new NationalScore.ScoreTerms(0, 0, 7, 0, 0, 0);
        var b = new NationalScore.ScoreTerms(0, 0, 3, 5, 1, 1);
        Assert.Equal(7.0, a[NationalScore.ScoreTerms.IronIndex]);
        var shares = new[] { new double[NationalScore.ScoreTerms.Count], new double[NationalScore.ScoreTerms.Count] };
        NationalScore.SharesFromTerms([a, b], shares);
        Assert.Equal(0.7, shares[0][NationalScore.ScoreTerms.IronIndex], 12);
        Assert.Equal(0.3, shares[1][NationalScore.ScoreTerms.IronIndex], 12);
    }

    [Fact]
    public void 最終の表は鉄の産出の世界の割合を出す_工業化度ではない()
    {
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        var france = sim.Nations[0].World; var prussia = sim.Nations[1].World;
        foreach (var r in france.Regions) r.Industrialization = 9.0;   // 工業化度を大きくしても表の列は動かない
        foreach (var r in france.Regions) foreach (var m in r.Markets) m.LastProduced = 0;
        foreach (var r in prussia.Regions) foreach (var m in r.Markets) m.LastProduced = 0;
        france.Region.Markets[Catalog.IronId].LastProduced = 30; prussia.Region.Markets[Catalog.IronId].LastProduced = 10;
        var rows = PlayText.EndingStandings(sim.Nations.Select(n => n.World), france);
        var fr = rows.First(x => x.IsPlayer); var pr = rows.First(x => !x.IsPlayer);
        Assert.Equal(0.75, fr.IronShare, 12);   // 手計算：30 ÷ (30+10)
        Assert.Equal(0.25, pr.IronShare, 12);
    }

    [Fact]
    public void 報告の表示に_工業化度の列を残さない()
    {
        string path = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.Runner", "PlayHarness.cs");
        foreach (var line in File.ReadAllLines(path))
        {
            int c = line.IndexOf("//", StringComparison.Ordinal);
            string code = c >= 0 ? line[..c] : line;
            Assert.False(code.Contains("MeanIndustrialization") || code.Contains("TotalIndustrialization") || code.Contains("工業化度"),
                $"PlayHarness.cs に工業化度の列が残っている: {code.Trim()}");
        }
    }
}
