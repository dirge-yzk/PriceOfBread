using System.Reflection;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(613) 線 tl：技術水準（<c>w.TechLevel</c>・<c>WorldTechLevel</c>）の削除の番人＝機構と不変条件。事前登録 measurements/tl613_20261028_prereg.txt の §6 T0a〜T0d。
/// 段0（死んだ枝と計器・報告の〈技術〉の削除）で入れた。段1〜6 で読み手が減るたびに許可表を直す（増えたら赤・減ったら表を直す）。</summary>
public class TechLevelRemovalTests
{
    // ---- T0c 読み手の許可表（ラチェット）-----------------------------------------------------------------------------
    // SimCore と Runner の非コメント行（// 以降を除く）で、World の技術水準（TechLevel・WorldTechLevel）を読む・書く行の数のファイル別の許可表。
    // 段0 の後に残るのは 挙動に関わる読み手（段1〜5）と書き手（段6）と、旧の枝（D2・段6）と画面（Display・段6）。
    // 増えたら赤＝新しい読み手を足さない（盤面の量か発見を読む）。減らしたときは表を直す（減ったも赤＝表が嘘にならない）。段6 の後は空。
    private static readonly Regex Words = new(@"\b(World)?TechLevel\b", RegexOptions.Compiled);

    private static readonly (string Dir, string File, int Lines)[] Allowed =
    [
        ("PriceOfBread.SimCore", "Agents.cs", 1), ("PriceOfBread.SimCore", "ArmyShape.cs", 1), ("PriceOfBread.SimCore", "BilateralMarket.cs", 1),
        ("PriceOfBread.SimCore", "Colonial.cs", 1), ("PriceOfBread.SimCore", "Diplomacy.cs", 1), ("PriceOfBread.SimCore", "Discovery.cs", 2),
        ("PriceOfBread.SimCore", "DiscoveryTables.cs", 1), ("PriceOfBread.SimCore", "FiscalTables.cs", 1), ("PriceOfBread.SimCore", "InstrumentsScore.cs", 1),
        ("PriceOfBread.SimCore", "Megaprojects.cs", 3), ("PriceOfBread.SimCore", "Military.cs", 1), ("PriceOfBread.SimCore", "MultiNationSim.cs", 10),
        ("PriceOfBread.SimCore", "SelfPlay.cs", 5), ("PriceOfBread.SimCore", "TechEducation.cs", 8), ("PriceOfBread.SimCore", "Territory.cs", 1),
        ("PriceOfBread.SimCore", "Transport.cs", 1), ("PriceOfBread.SimCore", "World.cs", 5),
        ("PriceOfBread.Runner", "PlayHarness.cs", 1), ("PriceOfBread.Runner", "Program.cs", 6),
    ];

    [Fact]
    public void 技術水準を読む行の数はファイル別の許可表のとおり()
    {
        string root = GoodIdGuardTests.RepoRoot();
        var counts = new SortedDictionary<string, int>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.Runner" })
            foreach (var f in Directory.GetFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            {
                string rel = f.Replace('\\', '/');
                if (rel.Contains("/obj/") || rel.Contains("/bin/")) continue;
                int n = 0;
                foreach (var line in File.ReadAllLines(f))
                {
                    int c = line.IndexOf("//", StringComparison.Ordinal);
                    string code = c >= 0 ? line[..c] : line;
                    if (Words.IsMatch(code)) n++;
                }
                if (n > 0) counts[dir + "|" + Path.GetFileName(f)] = n;
            }
        var allowed = Allowed.ToDictionary(x => x.Dir + "|" + x.File, x => x.Lines);
        var bad = new List<string>();
        foreach (var (key, n) in counts)
        {
            int a = allowed.GetValueOrDefault(key);
            if (n > a) bad.Add($"{key}: {n} 行（許可 {a}）＝増えた。技術水準を読まず盤面の量か発見を読むこと");
            else if (n < a) bad.Add($"{key}: {n} 行（許可 {a}）＝減った。許可表を直すこと");
        }
        foreach (var (key, a) in allowed) if (!counts.ContainsKey(key)) bad.Add($"{key}: 0 行（許可 {a}）＝減った。許可表を直すこと");
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    // ---- T0a 計器の列に技術水準が無い ---------------------------------------------------------------------------------
    // 計器（INS=1 のときだけ出る生の表）の見出しと値に、技術水準の列 techLevel・techLevelIndex を戻さない。techLevelMean（投資の収益の技術の項＝別の量）は対象外。
    [Fact]
    public void 計器の列に技術水準が無い()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var rx = new Regex(@"\btechLevel(Index)?\b");
        var hits = new List<string>();
        foreach (var f in Directory.GetFiles(dir, "Instruments*.cs"))
        {
            int i = 0;
            foreach (var line in File.ReadAllLines(f))
            {
                i++;
                int c = line.IndexOf("//", StringComparison.Ordinal);
                string code = c >= 0 ? line[..c] : line;
                if (rx.IsMatch(code)) hits.Add($"{Path.GetFileName(f)}:{i}");
            }
        }
        Assert.True(hits.Count == 0, "計器に技術水準の列が戻った：" + string.Join(", ", hits));
        Assert.DoesNotContain("techLevel", Instruments.ScoreHeaderText.Split('\t'));
    }

    // ---- T0b 記録の型が技術水準を持たない -------------------------------------------------------------------------------
    [Fact]
    public void 記録の型と州が技術水準を持たない()
    {
        foreach (var (t, names) in new (Type, string[])[]
        {
            (typeof(NationStanding), ["Tech", "TechLevel"]),
            (typeof(NationMetrics), ["Tech", "TechLevel"]),
            (typeof(TurnSnapshot), ["Tech", "TechLevel"]),
            (typeof(Region), ["TechLevel", "VintageTechLevel"]),
        })
            foreach (var n in names)
            {
                Assert.Null(t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance));
                Assert.Null(t.GetField(n, BindingFlags.Public | BindingFlags.Instance));
            }
        foreach (var gone in new[] { "VintageMigrateRate", "VintageObsolescence" })
            Assert.Null(typeof(Coefficients).GetField(gone));
    }

    // ---- 段3 倍率の削除（旗 TechMultipliersOff）の番人 ------------------------------------------------------------------
    private static Simulation Britain(ulong seed = 41) => new(NationConfig.Britain().ToScenario(seed), new NullAgent());

    /// <summary>旗 TechMultipliersOff が正なら軍の質の技術の因子が技術水準に依らず 1。旗 0 では技術水準で動く（陽性対照・期待は係数の値から手で作る）。
    /// 突然変異＝旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段3_軍の質の技術の因子は旗が正なら技術水準に依らず1()
    {
        var w = Britain().World; var c = w.Coef;
        Assert.Equal(0.0, c.TechMultipliersOff);
        w.TechLevel = 0.25;
        double low = ArmyShape.Quality(w, c).Tech;
        double expect0 = c.TechMilLowFloor + (1 - c.TechMilLowFloor) * (0.25 / c.TechMilFullLevel);   // 手で作った期待（技術 0.25 は満額より下）
        Assert.Equal(expect0, low, 12);
        Assert.True(low < 1.0 - 1e-9, "対照：旗 0 で技術 0.25 の因子が 1 未満でない（この試験は何も言っていない）");
        c.TechMultipliersOff = 1;
        foreach (double t in new[] { 0.0, 0.25, 1.0, 5.0, double.NaN })
        {
            w.TechLevel = t;
            Assert.Equal(1.0, ArmyShape.Quality(w, c).Tech);
        }
    }

    /// <summary>旗が正なら近代化費用の遅れの割増が無い＝世界の先端との遅れが 0 の盤面と同じ予算。旗 0 では遅れがあると要求額が増え予算が大きい（陽性対照）。
    /// 突然変異＝costMul の旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段3_近代化費用の遅れの割増は旗が正なら無い()
    {
        var sim = Britain(); sim.Run(8);
        var w = sim.World; var c = w.Coef;
        c.ModernizationCostGdpShare = Math.Max(c.ModernizationCostGdpShare, 0.05);
        w.SetTreasuryRaw(1e9); w.ModernizationFrac = 0.3;
        double Budget(double world, double own) { w.WorldTechLevel = world; w.TechLevel = own; return w.ModernizationBudget(); }
        double noLag = Budget(2.0, 2.0);
        double lag = Budget(2.0, 0.0);   // 遅れ 1.0 ＝割増 ModernizationTechLagCost
        Assert.True(noLag > 0, "対照：予算が立たない（この試験は何も言っていない）");
        Assert.True(lag > noLag * (1 + 1e-9), $"対照：旗 0 で遅れの割増が要求額（予算）を上げない（{noLag:G9}→{lag:G9}）");
        c.TechMultipliersOff = 1;
        Assert.Equal(noLag, Budget(2.0, 0.0), 9);
        Assert.Equal(noLag, Budget(2.0, 2.0), 9);
    }
}
