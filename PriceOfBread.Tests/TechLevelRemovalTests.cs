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
        ("PriceOfBread.SimCore", "DiscoveryTables.cs", 1), ("PriceOfBread.SimCore", "FiscalTables.cs", 1), ("PriceOfBread.SimCore", "InstrumentsScalars.cs", 2), ("PriceOfBread.SimCore", "InstrumentsDsc.cs", 1), // (727) 計器 dsc2_s の N 行の techLevel の列（記録・POB_INSTRUMENTS_DSC=1 のときだけ・段0′ の再生の B）
        ("PriceOfBread.SimCore", "Megaprojects.cs", 3), ("PriceOfBread.SimCore", "Military.cs", 1), ("PriceOfBread.SimCore", "MultiNationSim.cs", 10),
        ("PriceOfBread.SimCore", "SelfPlay.cs", 5), ("PriceOfBread.SimCore", "TechEducation.cs", 8), ("PriceOfBread.SimCore", "Territory.cs", 1),
        ("PriceOfBread.SimCore", "Transport.cs", 1), ("PriceOfBread.SimCore", "World.cs", 5),
        ("PriceOfBread.Runner", "PlayHarness.cs", 1), ("PriceOfBread.Runner", "Program.cs", 2),
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
        c.TechMultipliersOff = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
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
        c.TechMultipliersOff = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
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

    // ---- 段1 門を発見の行へ（旗 TechGateByDiscovery）の番人 ------------------------------------------------------------
    private static (MultiNationSim sim, World w, Power me) Board3()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        var w = sim.Nations[0].World;
        return (sim, w, w.SelfPower!);
    }

    /// <summary>requires の表（megaprojects.json・world.json）の期待は、工法の根拠から手で作った対（Id と発見の行の名前）。表の読み込みが名前を添字へ落とすこと・無い個体は -1 であることを見る。
    /// 突然変異＝megaprojects.json の requires を 1 件消す／名前を変える→赤。</summary>
    [Fact]
    public void 段1_requiresの表は手で作った対と一致する()
    {
        foreach (var (id, name) in new[]
        {
            ("yawata_steel", "転炉鋼"), ("brooklyn_bridge", "転炉鋼"), ("haarlemmermeer", "蒸気機関"), ("dover_calais_cable", "汽船"),
            ("alpine_hydro", "水力発電"), ("zuiderzee", "鉄筋コンクリート"), ("daimler_works", "内燃機関"), ("abadan_refinery", "石油灯油"),
            ("suez", "汽船"), ("panama", "ダイナマイト"),
        })
            Assert.Equal(Catalog.DiscoveryRowNamed(name), Megaprojects.RequiresRowOf(id));
        foreach (var none in new[] { "budapest_chain_bridge", "eiffel_tower", "crystal_palace", "ganges_canal", "cologne_cathedral", "washington_dc" })
            Assert.Equal(-1, Megaprojects.RequiresRowOf(none));
        Assert.Equal(-1, Megaprojects.RequiresRowOf("存在しない個体"));
        // 物理の根拠のある行だけ（制度と思想の行を requires に使わない）
        foreach (var m in WorldData.Megaprojects())
            if (Megaprojects.RequiresRowOf(m.Id) is var r && r >= 0)
                Assert.True(Catalog.Discoveries[r].Field is DiscoveryField.Process or DiscoveryField.Product or DiscoveryField.Weapon, m.Id);
    }

    /// <summary>旗 0 は今の techGate（技術が足りなければ拒否）。旗が正なら技術水準を一切読まず、requires の無い個体は Q0 から着工でき、ある個体は自国がその行を開くまで拒否・開けば着工できる。
    /// 拒否の理由に〈技術〉が出ない。突然変異＝旗の分岐を外す／requires の判定を常に true にする→赤。</summary>
    [Fact]
    public void 段1_旗が正なら門は発見の行_旗0は今の技術の門()
    {
        var (sim, w, me) = Board3();
        w.Coef.TechGateByDiscovery = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        var free = sim.Diplomacy.Megaprojects.First(m => m.Host == "ロンドン" && Megaprojects.RequiresRowOf(m.Id) < 0);
        var gated = sim.Diplomacy.Megaprojects.First(m => m.Host == "ロンドン" && Megaprojects.RequiresRowOf(m.Id) >= 0);
        free.Owner = me; gated.Owner = me;
        w.TechLevel = 0.0;   // 開幕の技術水準では techGate（0.95 以上）を満たさない
        Assert.Equal("技術が足りない", Megaprojects.StartRefusal(w, free));   // 陽性対照：旗 0 は技術の門
        Assert.Equal("技術が足りない", Megaprojects.StartRefusal(w, gated));
        w.Coef.TechGateByDiscovery = 1;
        Assert.Null(Megaprojects.StartRefusal(w, free));   // requires が無い＝Q0 から着工できる
        int row = Megaprojects.RequiresRowOf(gated.Id);
        w.Discovered[row] = false;
        w.TechLevel = 99.0;   // 技術水準がいくらあっても、行を開いていなければ拒否（技術を読まない）
        string? why = Megaprojects.StartRefusal(w, gated);
        Assert.NotNull(why); Assert.DoesNotContain("技術", why);
        w.Discovered[row] = true; w.TechLevel = 0.0;   // 行を開けば技術水準 0 でも着工できる
        Assert.Null(Megaprojects.StartRefusal(w, gated));
        w.Coef.TechGateByDiscovery = double.NaN;   // 否定形：NaN は旗 0（技術の門）へ倒れる
        Assert.Equal("技術が足りない", Megaprojects.StartRefusal(w, free));
    }

    // ---- 段2a 信号の削除（旗 TechSignalOff）------------------------------------------------------------------------------
    /// <summary>旗 0 は技術の遅れが姿勢の効用に入る（陽性対照）。旗が正なら遅れがいくらあっても効用が遅れ 0 の盤面と同じ。突然変異＝旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段2a_技術の遅れは旗が正なら姿勢の効用に入らない()
    {
        var w = Britain().World; var c = w.Coef;
        c.TechSignalOff = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        w.TechLevel = 0.0; w.WorldTechLevel = 3.0;
        UtilityAgent.ChooseStance(w, out var lagged);
        w.WorldTechLevel = 0.0;
        UtilityAgent.ChooseStance(w, out var level);
        Assert.True(lagged.UMod > level.UMod + 1e-9 && lagged.UInd > level.UInd + 1e-9, "対照：旗 0 で遅れが効用を上げない（この試験は何も言っていない）");
        c.TechSignalOff = 1;
        w.WorldTechLevel = 3.0;
        UtilityAgent.ChooseStance(w, out var off);
        Assert.Equal(level.UMod, off.UMod, 12); Assert.Equal(level.UInd, off.UInd, 12);
    }

    /// <summary>外圧は旗 0 で技術の格差から正（陽性対照・格差のある国）。旗が正なら全ての国で毎四半期 0 が書かれる（NationConfig の初期値 0.3 が残らない）。
    /// 突然変異＝書き込みごと旗で飛ばす（0.3 が残る）／旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段2a_外圧は旗が正なら毎四半期0が書かれる()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789, coef: new Coefficients { TechSignalOff = 0 });   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        foreach (var n in sim.Nations) { n.World.TechLevel = 0.1; }
        sim.Nations[0].World.TechLevel = 2.0;
        sim.StepQuarter();
        Assert.True(sim.Nations.Skip(1).Any(n => n.World.ExternalPressure > 1e-6), "対照：旗 0 で格差のある国の外圧が 0（この試験は何も言っていない）");
        var coef = new Coefficients { TechSignalOff = 1 };
        var sim2 = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789, coef: coef);
        foreach (var n in sim2.Nations) n.World.TechLevel = 0.1;
        sim2.Nations[0].World.TechLevel = 2.0;
        sim2.StepQuarter();
        foreach (var n in sim2.Nations) Assert.Equal(0.0, n.World.ExternalPressure);
    }

    // ---- 段2b 閾値の削除（旗 TechThresholdOff）---------------------------------------------------------------------------
    /// <summary>旗 0 では技術水準が閾値の下だと Modernize の法の目標（制限選挙・女性財産権・女性選挙権）が出ない（陽性対照）。旗が正なら技術水準 0 でも出る。
    /// 突然変異＝4 本のどれかの旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段2b_Modernizeの法の目標は旗が正なら技術の閾値で出し惜しまない()
    {
        var w = Britain().World; var p = w.Policy;
        w.Coef.TechThresholdOff = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        w.ParliamentEnabled = false;   // 議会の与党の段（技術と無関係に参政権を上げる）に入らない盤面にして、技術の閾値だけを見る
        w.TechLevel = 0.0;
        p.Suffrage = Suffrage.None; p.WomenRights = WomenRightsLaw.None;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.True(p.WomenRights == WomenRightsLaw.None, $"対照：旗 0 で技術水準 0 でも女性の権利の目標が出る（この試験は何も言っていない）{p.WomenRights}");   // 参政権は他の書き手〔大衆の闘争度の譲歩ほか〕も Census を置くので対照に使わない
        // 義務教育の目標：公教育が法で国庫が潤沢なら、旗 0 は技術水準 0 だと置かず、旗が正は置く
        w.TechLevel = 0.0; w.Coef.TechThresholdOff = 0; w.EducationLaw = EducationLaw.Public; w.SetTreasuryRaw(1e12);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(EducationLaw.Public, p.EducationLaw);
        w.Coef.TechThresholdOff = 1;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(EducationLaw.Compulsory, p.EducationLaw);
        p.Suffrage = Suffrage.None; p.WomenRights = WomenRightsLaw.None;
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.True(p.Suffrage >= Suffrage.Census);
        Assert.True(p.WomenRights >= WomenRightsLaw.Property, $"{p.WomenRights}");   // 後段の整形で選挙権の段は 1 段に畳まれうる＝財産権以上
        // C3（Agents の近代化派）：絶対王政で技術水準 0 なら旗 0 は制限選挙を置かず、旗が正は置く
        var w2 = Britain().World; w2.Coef.TechThresholdOff = 0; w2.Polity = Polity.Absolutist; w2.TechLevel = 0.0; w2.Policy.Suffrage = Suffrage.None;
        new ModernizerAgent().DecideQuarter(w2);
        Assert.Equal(Suffrage.None, w2.Policy.Suffrage);
        w2.Coef.TechThresholdOff = 1;
        new ModernizerAgent().DecideQuarter(w2);
        Assert.Equal(Suffrage.Census, w2.Policy.Suffrage);
        // 女性選挙権の段（財産権が法・男子普通選挙が法）：旗が正なら技術水準 0 でも目標が選挙権の段に上がる（旗 0 は財産権のまま）
        var w3 = Britain().World; w3.ParliamentEnabled = false; w3.TechLevel = 0.0;
        w3.WomenRights = WomenRightsLaw.Education; w3.Suffrage = Suffrage.AdultMale; w3.Policy.WomenRights = WomenRightsLaw.Education;
        w3.Coef.TechThresholdOff = 0; UtilityAgent.Apply(w3, UtilityAgent.Stance.Modernize);
        Assert.Equal(WomenRightsLaw.Education, w3.Policy.WomenRights);
        w3.Coef.TechThresholdOff = 1; UtilityAgent.Apply(w3, UtilityAgent.Stance.Modernize);
        Assert.Equal(WomenRightsLaw.Suffrage, w3.Policy.WomenRights);
    }

    /// <summary>旗が正なら制度と思想の行の技術の比は 1（技術水準 0 でも点数が立つ）。旗 0 では技術水準 0 だと比 0（陽性対照）。製法の行は旗に依らず今のまま。突然変異＝旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段2b_制度と思想の行の技術の比は旗が正なら1()
    {
        int idea = Discovery.SocialismRow;   // 十年が 1830 年＝技術水準 0 だと比 0
        int proc = Catalog.DiscoveryRowNamed("転炉鋼");
        var w = Britain().World; var c = w.Coef; w.TechLevel = 0.0; c.TechThresholdOff = 0;   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        Assert.Equal(0.0, Discovery.TechRatioOf(w, idea, true, false, c));
        Assert.Equal(0.0, Discovery.TechRatioOf(w, proc, false, false, c));
        c.TechThresholdOff = 1;
        Assert.Equal(1.0, Discovery.TechRatioOf(w, idea, true, false, c));
        Assert.Equal(0.0, Discovery.TechRatioOf(w, proc, false, false, c));   // 製法の行は旗に依らず技術水準を読む
        c.TechThresholdOff = double.NaN;
        Assert.Equal(0.0, Discovery.TechRatioOf(w, idea, true, false, c));   // 否定形：NaN は旗 0
    }

    // ---- 段4 時代係数・運賃革命の削除（旗 EraOff）------------------------------------------------------------------------
    /// <summary>旗 0 は前線の技術水準のランプで 1 を外れる（陽性対照）。旗が正なら k が何でも 1.0。突然変異＝旗の分岐を外す→赤。</summary>
    [Fact]
    public void 段4_時代係数と運賃革命は旗が正なら1()
    {
        var c = new Coefficients { EraOff = 0 };   // 既定は 1（点火済み）＝陽性対照は旗 0 を明示する
        double hi = c.TransportRevolutionOnset + c.TransportRevolutionSpan + 1.0;
        Assert.True(Math.Abs(c.EraFactor(hi, 1.0) - 1.0) > 1e-9 && InternationalMarket.FreightRevolution(hi, c) < 1.0 - 1e-9, "対照：旗 0 で前線の技術水準が高くても 1 のまま");
        c.EraOff = 1;
        foreach (double k in new[] { 1.0, -0.5, 3.0 }) Assert.Equal(1.0, c.EraFactor(hi, k));
        Assert.Equal(1.0, InternationalMarket.FreightRevolution(hi, c));
        c.EraOff = double.NaN;
        Assert.True(InternationalMarket.FreightRevolution(hi, c) < 1.0 - 1e-9);
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
