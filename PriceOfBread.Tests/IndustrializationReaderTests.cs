using System.Reflection;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(605) 線 ind4：工業化度の読み手を盤面の量へ置き換える旗の束の番人＝機構と不変条件。事前登録 measurements/ind4_605_20261027_prereg.txt の J1〜J11。
/// 残る旗は全て既定 0（倒さない）。期待は手計算のリテラルか、実装と別の出所（盤面に自分で置いた数・別の関数）から作る。史実の桁は固定しない。</summary>
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
        ("CapitalMarket.cs", 1), ("Coefficients.cs", 1), ("Diplomacy.cs", 1), ("Discovery.cs", 1), ("EconomyStep.cs", 8),
        ("Instruments.cs", 1), ("InstrumentsCapacity.cs", 1), ("InstrumentsEquipment.cs", 2), ("InstrumentsLawGate.cs", 1), ("InstrumentsModernize.cs", 1), ("InstrumentsScore.cs", 1),
        ("Model.cs", 4), ("MultiNationSim.cs", 2), ("NationBrief.cs", 1), ("NationConfig.cs", 1), ("OpeningBake.cs", 3),
        ("SelfPlay.cs", 3), ("Simulation.cs", 14), ("Territory.cs", 1), ("Validation.cs", 2), ("World.cs", 7),
    ];

    [Fact]
    public void 工業化度を読む行の数はファイル別の許可表のとおり()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var counts = new SortedDictionary<string, int>();
        foreach (var f in InstrumentsPlacementTests.TopAndInstruments(dir))
        {
            int n = 0;
            foreach (var line in File.ReadAllLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? line[..c] : line;
                if (Words.IsMatch(code)) n++;
            }
            string key = Path.GetFileName(f);
            if (key.StartsWith("Coefficients.")) key = "Coefficients.cs";   // (1106) 係数の部品は 1 つの持ち主として数える
            if (n > 0) counts[key] = counts.GetValueOrDefault(key) + n;
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
        "CapitalHeadroomFromReturn",   // (667) 疫病の衛生・信用の深さ・開幕の交易・扶養力の旗 4 本は既定 1 で採り旧の枝ごと削除した（下の J2・J5・J7・J8 は旗なしの番人）
    ];

    // ---- J9 対称：旗を読む行が IsPlayer を読まない ---------------------------------------------------------------------
    [Fact]
    public void 旗を読む行は_プレイヤーかどうかを読まない()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in InstrumentsPlacementTests.TopAndInstruments(dir))
            foreach (var line in File.ReadAllLines(f))
            {
                int c = line.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? line[..c] : line;
                if (code.Contains("IsPlayer") && Flags.Any(code.Contains))
                    Assert.Fail($"{Path.GetFileName(f)}: 旗と IsPlayer が同じ行にある（AI とプレイヤーは同じ式を通る）: {code.Trim()}");
            }
    }

    // ---- J2 疫病の衛生 ------------------------------------------------------------------------------------------------
    private static double EpidemicDeaths(double ind, double infraFill)
    {
        var sim = Britain(); var w = sim.World; var c = w.Coef; var r = w.Region;
        r.Industrialization = ind; r.OldVintageCapital = 0; r.StrandedVintage = 0;
        r.InfraFill = infraFill; r.SoapFill = 0; r.ElecInfraFill = 0;
        var m = typeof(World).GetMethod("ApplyEpidemicDeaths", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (double)m.Invoke(w, [r, c, 1.0, 1.0])!;
    }

    [Fact]
    public void 疫病_工業化度を動かしても致死率が動かず_衛生財の充足で動く()
    {
        Assert.Equal(EpidemicDeaths(0.0, 0.5), EpidemicDeaths(5.0, 0.5), 9);   // 工業化度は読まれない
        double lo = EpidemicDeaths(1.0, 0.0), hi = EpidemicDeaths(1.0, 1.0);
        Assert.True(lo > 0 && hi < lo, $"衛生が満ちても死者が減らない（充足 0:{lo}・1:{hi}）");
        Assert.Equal(1.0 - 0.7, hi / lo, 6);   // 手計算：致死率 ∝ (1−0.7×衛生)・衛生 0→1 で 0.3 倍
    }

    // ---- J3 思想の発展度 ------------------------------------------------------------------------------------------
    // ★(640) 線 idn：発展度 dev（世界最先端の工業化度の項と都市の割合の項）は読み手ごと消した＝旗 IdeologyDevFromUrban の 2 本の試験も消した（番人は IdeaNeedBoardTests）。

    // ---- J4 都市化の天井 ------------------------------------------------------------------------------------------
    private static double Headroom(Region r, Coefficients c)
        => (double)typeof(Meritocracy).GetMethod("UrbanHeadroom", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [r, c])!;

    [Fact]
    public void 都市化の天井は無く職の移しの残枠は無限()
    {
        var sim = Britain(); var w = sim.World; var r = w.Region; var c = w.Coef;
        Pop? rural = null, urban = null;
        foreach (var p in r.Pops) { if (p.Closed || p.IsCriminal) continue; if (p.IsUrban) urban ??= p; else rural ??= p; }
        Assert.True(rural is not null && urban is not null);
        rural!.Size = 900; urban!.Size = 100;   // 組の合計 1000・都市 100（手計算の期待の元）
        r.Industrialization = 0.0; r.OldVintageCapital = 0;
        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));
        r.Industrialization = 5.0;
        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));
    }

    /// <summary>焼き込み (812) で残した 3 つの早い return の確かめ：首都の州（CapitalEnabled）で、組の人口が 0 の州だけ残枠 0・人口のある州は無限。
    /// 突然変異（組の人口 0 の return を外す）で赤になる＝焼き込みの前は旗の前にあったこの順序を、天井の式が消えた後も守る。</summary>
    [Fact]
    public void 職の移しの残枠は組の人口が0の州だけ0_人口のある首都の州は無限()
    {
        var sim = Britain(); var r = sim.World.Region; var c = sim.World.Coef;
        r.CapitalEnabled = true;
        Pop? rural = null, urban = null;
        foreach (var p in r.Pops) { if (p.Closed || p.IsCriminal) continue; if (p.IsUrban) urban ??= p; else rural ??= p; }
        Assert.True(rural is not null && urban is not null);
        rural!.Size = 900; urban!.Size = 100;
        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));
        rural.Size = 0; urban.Size = 0;
        Assert.Equal(0.0, Headroom(r, c));
    }

    // ---- J5 信用の深さ ------------------------------------------------------------------------------------------------
    /// <summary>与信の上限（説明API の CreditTarget の 1 行目）を、州の工業化度だけを変えた英の盤面で出す（投資家の所得は十分に大きくして返せる額の側を外す）。</summary>
    private static (double Cap, double Gold, bool OnGold) CreditCapAt(double ind)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        sim.Run(1);
        var w = sim.World;
        foreach (var rr in w.Regions) { rr.Industrialization = ind; rr.OldVintageCapital = 0; rr.StrandedVintage = 0; }
        var inv = w.Region.Pops[w.Region.InvestorPopIndex];
        inv.LastEarnedPerMonth = new MoneyPerMonth(1e30);
        w.Explain = new ExplainLedger();
        double gold = w.Bank!.Gold.Value;
        Credit.Step(w, w.Coef);
        return (w.Explain.Get("CreditTarget")[0].Delta, gold, w.OnGoldStandard);
    }

    [Fact]
    public void 信用の深さ_準備の上限は工業化度に依らず_金かける信用乗数ひく1()
    {
        var (c0, g0, _) = CreditCapAt(0.0); var (c5, _, _) = CreditCapAt(5.0);
        Assert.True(c0 > 0, $"前提が崩れた：英の準備の上限が 0（{c0}）");
        Assert.Equal(c0, c5, 9);   // 工業化度は読まれない
        double full = g0 * (new Coefficients().CreditMultiplier - 1);   // 手計算：金の在庫×(信用乗数−1)（旧の発展度の倍率は 0.35〜1 で上限をこれより絞った）
        Assert.True(c0 <= full * (1 + 1e-9) && c0 >= 0.99 * full, $"上限 {c0} が 金×(乗数−1) {full} の近傍にない（カバー率防衛の絞りは 1% 未満の前提）");
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
    public void 交易の比重_開幕は入力データの工業化度で傾けず基準の均等()
    {
        var cfgs = new[] { NationConfig.France(), NationConfig.Britain(), NationConfig.Japan() };
        Assert.True(cfgs[1].Industrialization > cfgs[2].Industrialization, "前提が崩れた：入力の工業化度が国で違わない");
        double baseW = (double)typeof(MultiNationSim).GetField("DefaultTradeWeightBase", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
        var s1 = new MultiNationSim(cfgs, seed: 1789);
        for (int i = 0; i < cfgs.Length; i++) Assert.Equal(baseW, s1.Nations[i].World.SelfPower!.TradeWeight, 12);
    }

    // ---- J8 扶養力 ---------------------------------------------------------------------------------------------------
    private static (double Capacity, double Fertility) CapacityAt(double ind)
    {
        var sim = Britain(); var w = sim.World; var r = w.Region;
        r.Industrialization = ind; r.OldVintageCapital = 0; r.StrandedVintage = 0;
        sim.DemographicStep();
        return (w.DiagCapacity, w.DiagFertility);
    }

    [Fact]
    public void 扶養力は工業化度に依らず_出生率だけが工業化度で動く()
    {
        var (c0, f0) = CapacityAt(0.0); var (c4, f4) = CapacityAt(4.0);
        Assert.True(c0 > 0);
        Assert.Equal(c0, c4, 9);                                                   // 扶養力は工業化度の 2 項を持たない
        Assert.True(f4 < f0, $"陽性対照：出生の項（FertilityIncomeSlope×avgInd）は残る（工業化度 0:{f0}・4:{f4}）");
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
