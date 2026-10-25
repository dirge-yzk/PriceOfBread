using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(373)(377) 政治のスカラーの削除の番人。事前登録 measurements/ps_20261016_prereg.txt・実装ログ (373)(377)。
/// 制度疲労（(373)）は物理削除した＝名前で復活を禁じる（期待値は本体の式から作らない）。採らなかった旗 2 本と World の Diag 3 欄も main に入れない＝名前で禁じる。
/// 汚職の政治の 2 項（天命の汚職の項・腐敗→正統性の侵食）は (377) で消して測ったが止める線（移民による州の空洞化）に当たり戻した＝作者に聞く Q1 と一緒。戻した 2 項が働くことを対照つきで確かめる。
/// 厭戦の 2 項（闘争度の目標項 WarWearinessMilitancy・天命の戦争の項 DynMandateWarWeight）は残す＝外すと戦争→内政の経路が消える（(377) の読み）。
/// それが黙って消えないように、厭戦のストックを直に置いて〈闘争度の目標項が 0 でない〉〈天命が下がる〉を対照つきで確かめる（何もしない機構が緑にならない）。
/// 突然変異で赤になる箇所：削除した名前の復活・Politics の warWeariness／dmWar の項を外す・dmGraft／corruptionErosion の項を外す。</summary>
[Collection("Instruments")]
public class ScalarDeletionTests
{
    private static MultiNationSim Board(ulong seed = 83)
        => new([WorldData.ByName("清"), WorldData.ByName("ロシア")], seed: seed) { DiplomacyFrozen = true };

    private const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

    [Fact]
    public void 制度疲労は物理削除済み_係数も状態も読み手に残らない()
    {
        // 腕 BD（制度疲労だけ）が H の線と止める線に通って採用した削除（2026-10-16）。復活を黙って許さない＝名前で存在を禁じる（期待値は本体の式から作らない）。
        string[] coefGone = ["DeleteInstitutionalDecay", "DecayReformResistWeight", "DecayTechAdoptWeight", "DecayMilitaryWeight", "DecayShockRelief", "DecayShockCooldownQuarters",
            "DecayReactionRegrowth", "DecayLockinIndustrialization", "DecayCeilingUniversal", "ModernizationDecayCost", "ModernizationDecayRelief", "DecayFromPrivilege"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        string[] worldGone = ["InstitutionalDecay", "InstitutionalDecayCeiling", "DecayForReaders", "PrivilegeDecayRaw", "WorldMaxPrivilegeDecayRaw"];
        foreach (var n in worldGone) Assert.Null(typeof(World).GetMember(n, All).FirstOrDefault());
        // 初期値の入り口（NationConfig／Scenario）にも残さない
        Assert.Null(typeof(NationConfig).GetField("InstitutionalDecay"));
        Assert.Null(typeof(Scenario).GetProperty("InitialInstitutionalDecay"));
    }

    [Fact]
    public void 政治の束12項目は物理削除済み_係数も旗も復活しない()
    {
        // (384) 腕 B（独裁へ引く項・勅令の速さ・犯罪と出来事から闘争度／満足度への直の足し・恐慌から思想への直の足し）が止める線に掛からず採用した削除（2026-10-17）。
        // 復活を黙って許さない＝名前で存在を禁じる（期待値は本体の式から作らない）。ReformMilitancyRelief は Politics の 3 か所（(387) の木で :203・:823・:1200）の作者裁定（譲歩が鎮める）の実装なので残る。
        string[] coefGone = ["DominanceCoupWeight", "AutocraticPolicyAgility", "CrimeSatPenalty", "CrimeWealthFearWeight", "DefaultMilitancyHit", "DeposedMilitancySpike",
            "WarDefeatMilitancySpike", "SeizureMilitancyCost", "DepressionSocialism", "DepressionCommunism", "DepressionReaction", "DeleteRegimeChangeRelief"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        Assert.NotNull(typeof(Coefficients).GetField("ReformMilitancyRelief"));
        Assert.NotNull(typeof(Coefficients).GetField("DominanceInstabilitySatisfaction"));   // 正統性の侵食とブルジョア独裁の引力の門は残る
    }

    [Fact]
    public void 採らなかった旗2本とDiagの欄はmainに入れない()
    {
        // (373) の形の旗 2 本（既定 0）と純記帳の Diag 3 欄は枝 ps に残す（思想⑩・反証役 ps〔bv〕中#1）。
        string[] coefGone = ["DeleteCorruptionScalars", "DeleteWarWearinessScalars"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        string[] worldGone = ["DiagDmWar", "DiagDmGraft", "DiagCorrErosion"];
        foreach (var n in worldGone) Assert.Null(typeof(World).GetMember(n, All).FirstOrDefault());
    }

    [Fact]
    public void 恒等の削除_2026_10_18_の状態の欄は復活しない()
    {
        // 精査 doc §4 ①-a／①-b／Q2 の恒等の削除（書き手か読み手が試合で一度も回らなかった状態の欄）。復活を黙って許さない＝名前で存在を禁じる（期待値は本体の式から作らない）。
        // 係数の名前は DormantIdentityTests.RetiredFields が禁じる。
        foreach (var n in new[] { "EducationEma", "LastSatisfaction" }) Assert.Null(typeof(Pop).GetMember(n, All).FirstOrDefault());
        Assert.Null(typeof(Region).GetMember("EffFirms", All).FirstOrDefault());
        foreach (var n in new[] { "Expertise", "WorldFrontierExpertise", "ColonialInfra", "ColonialPop", "OfficialWelfareFill", "Meritocracy", "HouseholdCreditIssued", "HouseholdDebtRepaid" })
            Assert.Null(typeof(World).GetMember(n, All).FirstOrDefault());
        Assert.Null(typeof(Diplomacy).GetMember("IndependentColonies", All).FirstOrDefault());
        foreach (var n in new[] { "FreedYear", "Infra" }) Assert.Null(typeof(Colony).GetMember(n, All).FirstOrDefault());
        Assert.Null(typeof(NationPolicy).GetMember("Pension", All).FirstOrDefault());   // 死蔵の年金の目標（World.Pension は社会保障法の段から導く）
        // 型ごと削除したもの：外交コア（Q4）・単国の経路の較正（Calibration・Sweep・ModernizationPath）
        foreach (var t in new[] { "DiploCore", "Calibration", "Sweep", "SweepMetrics", "ModernizationPath" })
            Assert.Null(typeof(Coefficients).Assembly.GetType("PriceOfBread.SimCore." + t));
    }

    [Fact]
    public void 恒等の削除_2026_10_25_ゴミ変数の1回目の欄は復活しない()
    {
        // (551) 恒等の線 gc1：本番の盤面では 1 度も書かれず読まれなかった欄を名前で禁じる（期待値は削除の一覧を手で写した物で、本体の式から作らない）。係数の名前は DormantIdentityTests.RetiredFields が禁じる。
        Assert.Null(typeof(Region).GetMember("Infra", All).FirstOrDefault());          // 辺境の開発度（locked 州の解錠の駆動）。Region.InfraFill・InfraBudget は上下水道の調達で別物
        Assert.Null(typeof(World).GetMember("ForceUnlock", All).FirstOrDefault());     // 武力解錠のレバー（読み手は locked 州の解錠の道だけだった）
        Assert.Null(typeof(Scenario).GetMember("ForceUnlock", All).FirstOrDefault());
        Assert.Null(typeof(Pop).GetMember("MilLiving", All).FirstOrDefault());         // 闘争度の内訳の生活の欄（(431) の二量分離の後は恒久 0）
    }

    [Fact]
    public void 恒等の削除_2026_10_26_ゴミ変数の3回目の関数は復活しない()
    {
        // (568) 恒等の線 gc3：稼働中の州だけの走査と、rs3 が重ねた向きだけ違う関数 2 本を畳んだ（期待値は削除の一覧を手で写した物で、本体の式から作らない）。
        Assert.Null(typeof(World).GetMember("ActivePopsWithRegion", All).FirstOrDefault());                                   // 稼働中の州だけの POP の走査（本番では国家会計の走査と同じ列だった）
        var internalStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        foreach (var n in new[] { "EffectiveRentSharesBack", "PrevStage", "NextStage" })
            Assert.Null(typeof(PolicyPreferences).GetMember(n, internalStatic).FirstOrDefault());                              // 向きを bool の引数 1 本にした
    }

    /// <summary>★(568) 恒等の線 gc3：<c>Region.State</c>（稼働中か否か）を読む行は、本番では恒真だったので外した。ソースに〈RegionState を読む行〉が戻っていないことを、
    /// 許可表（ファイル → 許す行数の上限）で見る。許可表の中は枠と機械の線 ckf〔世界の時計の置き直し〕が書き直す所と、型の定義（Model.cs）だけ。
    /// 期待値は許可表（手で写した物）で、本体のソースから作らない。<see cref="GoodIdGuardTests.CsFiles"/> と同じ走査（SimCore・Runner・App・Tests）。</summary>
    [Fact]
    public void 稼働中の州の判定_RegionStateを読む行は許可表の外に無い()
    {
        var allow = new System.Collections.Generic.Dictionary<string, int>
        {
            ["PriceOfBread.SimCore/Model.cs"] = 2,                    // 型の定義と Region.State の欄（sm4 が隣に行を足す＝消すのは sm4 の合流の後）
            ["PriceOfBread.SimCore/Discovery.cs"] = 9,                // ckf が書き直す所（発見の表の走査）
            ["PriceOfBread.SimCore/InstrumentsClock.cs"] = 1,         // ckf（時計の計器）
            ["PriceOfBread.SimCore/MultiNationSim.cs"] = 1,           // ckf（時計の巡回のすぐ上）
            ["PriceOfBread.Runner/Program.cs"] = 2,                   // ckf（discprobe）
            ["PriceOfBread.Tests/DiscoveryClockTests.cs"] = 4,
            ["PriceOfBread.Tests/DiscoveryEngineerTests.cs"] = 5,
            ["PriceOfBread.Tests/DiscoveryScoreTests.cs"] = 19,
        };
        var code = new System.Text.RegularExpressions.Regex(@"RegionState");
        var bad = new System.Collections.Generic.List<string>();
        foreach (var (full, rel) in GoodIdGuardTests.CsFiles())
        {
            if (rel == "PriceOfBread.Tests/ScalarDeletionTests.cs") continue;   // この番人自身
            int n = 0;
            foreach (var line in System.IO.File.ReadLines(full))
            {
                string t = line; int cm = t.IndexOf("//", System.StringComparison.Ordinal); if (cm >= 0) t = t.Substring(0, cm);   // 注は読みではない
                if (code.IsMatch(t)) n++;
            }
            int cap = allow.TryGetValue(rel, out var a) ? a : 0;
            if (n > cap) bad.Add($"{rel}: {n} 行（許可 {cap}）");
        }
        Assert.True(bad.Count == 0, "Region.State を読む行が許可表の外に戻っている（本番の州は常に稼働中＝判定は恒真。必要なら理由を許可表に足す）：" + string.Join(" / ", bad));
    }

    [Fact]
    public void 汚職の政治の2項は物理削除されている_天命と正統性は実効汚職を読まない()
    {
        // (377) で一度消して止める線に当たり戻した 2 項（天命の汚職の項 dmGraft・腐敗→正統性の侵食）を、作者 2026-10-16「汚職→廃止」と 10-17「史実度の低下は差し戻し理由にはなりません」で
        // (383) が再び物理削除した。係数 2 本が型に無いこと＋行政の充足〔実効汚職の元〕を 0 にしても 1 にしても Politics.Step の天命と正統性は 1 ビットも変わらないこと＋説明台帳に項が無いこと。
        Assert.Null(typeof(Coefficients).GetField("DynMandateGraftWeight"));
        Assert.Null(typeof(Coefficients).GetField("CorruptionLegitimacyErosion"));
        (double Mandate, double Legit, bool HasGraft, bool HasErosion) Run(double fill)
        {
            var w = new Simulation(NationConfig.France().ToScenario(7), new NullAgent()).World;
            // ★(387) 反証役 ae〔bv〕低の確かめ：この盤面では満足の項で天命の目標が 1 に張り付き（Clamp01）、汚職の項を戻しても値が動かなかった（突然変異 M6 が緑）。
            //   満足の重みを 0 にして目標を張り付かない所（中立 0.5 の近く）へ置く＝充足に依る項があれば値が動く。
            w.Coef.DynMandateSatWeight = 0.0;
            foreach (var r in w.Regions) r.AdminFill = fill;
            w.Explain = new ExplainLedger();
            Politics.Step(w, w.Coef);
            return (w.DynasticMandate, w.Legitimacy,
                w.Explain.Get("DynasticMandate").Any(t => t.Reason == "汚職の体感"), w.Explain.Get("Legitimacy").Any(t => t.Reason.StartsWith("腐敗侵食")));
        }
        var weak = Run(0.0); var strong = Run(1.0);
        Assert.InRange(strong.Mandate, 0.46 + 1e-9, 0.54 - 1e-9);   // 前提：天命の目標が 0 と 1 に張り付かない（初期 0.5・EMA 0.08＝目標が (0,1) の内なら値は (0.46,0.54) の内）
        Assert.False(weak.HasGraft); Assert.False(weak.HasErosion);
        Assert.Equal(strong.Mandate, weak.Mandate);   // 実効汚職が 1 と 0 でも天命は同じ
        Assert.Equal(strong.Legit, weak.Legit);       // 同・正統性
    }

    [Fact]
    public void 厭戦の2項は残る_闘争度の目標項と天命の戦争の項が働く()
    {
        // 厭戦のストックを直に置く（戦争を待たない）。Politics.Step は四半期に 1 回＝Run(1) で通る。対照＝同じ盤面で厭戦 0。
        MultiNationSim Armed(double exhaustion)
        {
            var sim = Board();
            foreach (var n in sim.Nations) n.World.SelfPower!.WarExhaustion = exhaustion;
            sim.Run(1);
            return sim;
        }
        double MilWarMax(MultiNationSim sim) => sim.Nations.Max(n => n.World.AllPops().Max(p => p.MilWar));
        var calm = Armed(0.0);
        var tired = Armed(0.6);
        Assert.Equal(0.0, MilWarMax(calm));
        Assert.True(MilWarMax(tired) > 0.0, "厭戦 0.6 で闘争度の目標項 MilWar が 0＝厭戦→闘争度の経路が消えている");
        foreach (var name in new[] { "清", "ロシア" })
        {
            double m0 = calm.Nations.First(n => n.World.SelfPower!.Name == name).World.DynasticMandate;
            double m1 = tired.Nations.First(n => n.World.SelfPower!.Name == name).World.DynasticMandate;
            Assert.True(m1 < m0, $"{name}：厭戦 0.6 で天命が下がらない（{m0}→{m1}）＝天命の戦争の項が消えている");
        }
    }
}
