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
        ("PriceOfBread.SimCore", "BilateralMarket.cs", 1),   // (843) c68ui：ArmyShape.cs・Diplomacy.cs・DiscoveryTables.cs・FiscalTables.cs・Megaprojects.cs（3→0）・PlayHarness.cs の行は段6 の旗の焼き込みで技術水準を読む行が無くなった（許可表の行を消す・MultiNationSim.cs 9→8・World.cs 5→4）
        ("PriceOfBread.SimCore", "Discovery.cs", 2),
        ("PriceOfBread.SimCore", "InstrumentsScalars.cs", 1), ("PriceOfBread.SimCore", "InstrumentsDsc.cs", 1), // (727) 計器 dsc2_s の N 行の techLevel の列（記録・POB_INSTRUMENTS_DSC=1 のときだけ・段0′ の再生の B）
        ("PriceOfBread.SimCore", "Military.cs", 1), ("PriceOfBread.SimCore", "MultiNationSim.cs", 8),
        ("PriceOfBread.SimCore", "TechEducation.cs", 8), ("PriceOfBread.SimCore", "Territory.cs", 1),
        ("PriceOfBread.SimCore", "World.cs", 4),
        ("PriceOfBread.Runner", "Program.cs", 2),
    ];

    [Fact]
    public void 技術水準を読む行の数はファイル別の許可表のとおり()
    {
        string root = GoodIdGuardTests.RepoRoot();
        var counts = new SortedDictionary<string, int>();
        foreach (var dir in new[] { "PriceOfBread.SimCore", "PriceOfBread.Display", "PriceOfBread.Runner" })
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
        foreach (var f in Directory.GetFiles(Path.Combine(dir, "Instruments"), "Instruments*.cs"))
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

    /// <summary>軍の質の技術の因子は技術水準に依らず 1（旗 TechMultipliersOff は (843) で焼き込み。旗 0 の陽性対照は旗とともに消した）。
    /// 突然変異＝技術水準の因子を戻す→赤。</summary>
    [Fact]
    public void 段3_軍の質の技術の因子は旗が正なら技術水準に依らず1()
    {
        var w = Britain().World; var c = w.Coef;
        foreach (double t in new[] { 0.0, 0.25, 1.0, 5.0, double.NaN })
        {
            w.TechLevel = t;
            Assert.Equal(1.0, ArmyShape.Quality(w, c).Tech);
        }
    }

    /// <summary>近代化費用の遅れの割増が無い＝世界の先端との遅れが 0 の盤面と同じ予算（旗 TechMultipliersOff は (843) で焼き込み。旗 0 の陽性対照は旗とともに消した）。
    /// 突然変異＝costMul に遅れの割増を戻す→赤。</summary>
    [Fact]
    public void 段3_近代化費用の遅れの割増は旗が正なら無い()
    {
        var sim = Britain(); sim.Run(8);
        var w = sim.World; var c = w.Coef;
        c.ModernizationCostGdpShare = Math.Max(c.ModernizationCostGdpShare, 0.05);
        w.SetTreasuryRaw(1e9); w.ModernizationFrac = 0.3;
        double Budget(double world, double own) { w.WorldTechLevel = world; w.TechLevel = own; return w.ModernizationBudget(); }
        double noLag = Budget(2.0, 2.0);
        Assert.True(noLag > 0, "対照：予算が立たない（この試験は何も言っていない）");
        Assert.Equal(noLag, Budget(2.0, 0.0), 9);
        Assert.Equal(noLag, Budget(2.0, 2.0), 9);
    }

    // ---- 段1 門を発見の行へ（旗 TechGateByDiscovery）の番人 ------------------------------------------------------------
    /// <summary>requires の表（world.json の chokepoints）の期待は、工法の根拠から手で作った対（Id と発見の行の名前）。(1081) 巨大建造物を消した＝運河の 2 対だけ（紙の推奨で決めた事 8）。
    /// 突然変異＝world.json の chokepoints の requires を 1 件消す／名前を変える→赤。</summary>
    [Fact]
    public void 段1_requiresの表は手で作った対と一致する()
    {
        foreach (var (id, name) in new[] { ("suez", "汽船"), ("panama", "ダイナマイト") })
            Assert.Equal(Catalog.DiscoveryRowNamed(name), Megaprojects.RequiresRowOf(id));
        foreach (var none in new[] { "gibraltar", "bosporus", "malacca", "oresund", "hormuz", "bab_el_mandeb" })
            Assert.Equal(-1, Megaprojects.RequiresRowOf(none));
        Assert.Equal(-1, Megaprojects.RequiresRowOf("存在しない個体"));
        // 物理の根拠のある行だけ（制度と思想の行を requires に使わない）
        foreach (var c in WorldData.Setup().Chokepoints)
            if (Megaprojects.RequiresRowOf(c.Id) is var r && r >= 0)
                Assert.True(Catalog.Discoveries[r].Field is DiscoveryField.Process or DiscoveryField.Product or DiscoveryField.Weapon, c.Id);
    }

    // ---- 段2a 信号の削除（旗 TechSignalOff は (824) で焼き込み）--------------------------------------------------------------
    /// <summary>技術の遅れは姿勢の効用に入らない：遅れがいくらあっても効用が遅れ 0 の盤面と同じ。突然変異＝遅れを効用に入れる→赤。（旗 0 の対照〔遅れが効用を上げる〕は旗の焼き込みで消した。）</summary>
    [Fact]
    public void 段2a_技術の遅れは姿勢の効用に入らない()
    {
        var w = Britain().World;
        w.TechLevel = 0.0; w.WorldTechLevel = 0.0;
        UtilityAgent.ChooseStance(w, out var level);
        w.WorldTechLevel = 3.0;
        UtilityAgent.ChooseStance(w, out var off);
        Assert.Equal(level.UMod, off.UMod, 12); Assert.Equal(level.UInd, off.UInd, 12);
    }

    /// <summary>外圧は全ての国で毎四半期 0 が書かれる（NationConfig の初期値 0.3 が残らない）。格差のある国（技術水準 0.1 と 2.0）でも 0。
    /// 突然変異＝書き込みごと飛ばす（0.3 が残る）／格差を外圧に入れる→赤。</summary>
    [Fact]
    public void 段2a_外圧は毎四半期0が書かれる()
    {
        var sim2 = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789, coef: new Coefficients());
        foreach (var n in sim2.Nations) n.World.TechLevel = 0.1;
        sim2.Nations[0].World.TechLevel = 2.0;
        sim2.StepQuarter();
        foreach (var n in sim2.Nations) Assert.Equal(0.0, n.World.ExternalPressure);
    }

    // ---- 段2b 閾値の削除（旗 TechThresholdOff は (824) で焼き込み）-------------------------------------------------------------
    /// <summary>技術水準 0 でも Modernize の法の目標（義務教育・制限選挙・女性財産権・女性選挙権）が出る（技術の閾値で出し惜しまない）。
    /// 突然変異＝閾値の 4 本のどれかを戻す→赤。（旗 0 の対照〔閾値の下だと出ない〕は旗の焼き込みで消した。）</summary>
    [Fact]
    public void 段2b_Modernizeの法の目標は技術の閾値で出し惜しまない()
    {
        var w = Britain().World; var p = w.Policy;
        w.ParliamentEnabled = false;   // 議会の与党の段（技術と無関係に参政権を上げる）に入らない盤面にして、技術の閾値だけを見る
        foreach (var pp in w.AllPops()) pp.Militancy = 0;   // 大衆の闘争度の譲歩（技術と無関係に参政権を男子普選へ上げる別の書き手）に入らない盤面にする＝制限選挙の目標が閾値なしで出ることだけを見る（突然変異 Mu6 が緑だった穴の番人）
        w.TechLevel = 0.0; w.Suffrage = Suffrage.None;   // 現行法が参政権なしの国（門の畳みが目標を現行法へ戻す形に隠れない）
        p.Suffrage = Suffrage.None; p.WomenRights = WomenRightsLaw.None;
        // 義務教育の目標：公教育が法で国庫が潤沢なら、技術水準 0 でも置く
        w.EducationLaw = EducationLaw.Public; w.SetTreasuryRaw(1e12);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(EducationLaw.Compulsory, p.EducationLaw);
        p.Suffrage = Suffrage.None; p.WomenRights = WomenRightsLaw.None;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.True(p.Suffrage >= Suffrage.Census);
        Assert.True(p.WomenRights >= WomenRightsLaw.Property, $"{p.WomenRights}");   // 後段の整形で選挙権の段は 1 段に畳まれうる＝財産権以上
        // C3（Agents の近代化派）：絶対王政で技術水準 0 でも制限選挙を置く
        var w2 = Britain().World; w2.Polity = Polity.Absolutist; w2.TechLevel = 0.0; w2.Policy.Suffrage = Suffrage.None;
        new ModernizerAgent().DecideQuarter(w2);
        Assert.Equal(Suffrage.Census, w2.Policy.Suffrage);
        // 女性選挙権の段（財産権が法・男子普通選挙が法）：技術水準 0 でも目標が選挙権の段に上がる
        var w3 = Britain().World; w3.ParliamentEnabled = false; w3.TechLevel = 0.0;
        w3.WomenRights = WomenRightsLaw.Education; w3.Suffrage = Suffrage.AdultMale; w3.Policy.WomenRights = WomenRightsLaw.Education;
        UtilityAgent.Apply(w3, UtilityAgent.Stance.Modernize);
        Assert.Equal(WomenRightsLaw.Suffrage, w3.Policy.WomenRights);
    }

    /// <summary>制度と思想の行の技術の比は 1（技術水準 0 でも点数が立つ）。製法の行は技術水準を読む（技師の旗が立たない比の式）。突然変異＝制度と思想の行に技術水準の比を戻す→赤。</summary>
    [Fact]
    public void 段2b_制度と思想の行の技術の比は1()
    {
        int idea = Discovery.SocialismRow;   // 十年が 1830 年＝技術水準 0 だと比 0
        int proc = Catalog.DiscoveryRowNamed("転炉鋼");
        var w = Britain().World; var c = w.Coef; w.TechLevel = 0.0;
        Assert.Equal(1.0, Discovery.TechRatioOf(w, idea, true, false, c));
        Assert.Equal(0.0, Discovery.TechRatioOf(w, proc, false, false, c));   // 製法の行は技術水準を読む
    }

    // ---- 段4 時代係数・運賃革命の削除（旗 EraOff）------------------------------------------------------------------------
    /// <summary>k が何でも 1.0・運賃革命も 1.0（旗 EraOff は (843) で焼き込み。旗 0 の陽性対照は旗とともに消した）。突然変異＝ランプの式を戻す→赤。</summary>
    [Fact]
    public void 段4_時代係数と運賃革命は1()
    {
        var c = new Coefficients();
        double hi = c.TransportRevolutionOnset + c.TransportRevolutionSpan + 1.0;
        foreach (double k in new[] { 1.0, -0.5, 3.0 }) Assert.Equal(1.0, c.EraFactor(hi, k));
        Assert.Equal(1.0, InternationalMarket.FreightRevolution(hi, c));
    }

    // ---- 段5 技術の括弧（旗 TechProductivityBracketOff）-----------------------------------------------------------------
    [Fact]
    public void 段5_技術の括弧は旗が正なら外れ_旗0は今のまま()
    {
        var w = Britain().World; var r = w.Regions[0];
        w.TechLevel = 1.0;
        double with = w.TechProductivity(r);
        w.TechLevel = 0.0;
        double zero = w.TechProductivity(r);
        Assert.True(with > zero * (1 + 1e-9), "対照：旗 0 で技術水準が生産性を上げない");
        w.Coef.TechProductivityBracketOff = 1;
        w.TechLevel = 1.0; double a = w.TechProductivity(r);
        w.TechLevel = 0.0; double b0 = w.TechProductivity(r);
        Assert.Equal(b0, a, 12);
        Assert.True(a > 0);
    }
}
