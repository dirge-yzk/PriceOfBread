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
        // 復活を黙って許さない＝名前で存在を禁じる（期待値は本体の式から作らない）。ReformMilitancyRelief は :203・:848・:1225 の作者裁定（譲歩が鎮める）の実装なので残る。
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
    public void 汚職の政治の2項は残る_天命と正統性を下げる()
    {
        // (377) で消して止める線に当たり戻した 2 項。対照＝同じ盤面で係数を 0 にした世界（World.Coef は国ごとの別インスタンス）。
        // 期待値は本体の式から作らない＝〈係数ありの方が天命も正統性も低い〉だけを見る。前提：開幕の清・ロシアは実効汚職が正（行政財が足りない）。
        // 天命の項は正統性の侵食の倍率（dynErodeMul）にも届く＝対照は 1 項ずつ 0 にする（両方を同時に 0 にすると正統性の差が天命の経路で出てしまう）。
        MultiNationSim Run(bool zeroGraft, bool zeroErosion)
        {
            var sim = Board();
            foreach (var n in sim.Nations)
            {
                if (zeroGraft) n.World.Coef.DynMandateGraftWeight = 0.0;
                if (zeroErosion) n.World.Coef.CorruptionLegitimacyErosion = 0.0;
            }
            sim.Run(1);
            return sim;
        }
        var both = Run(false, false);
        var noGraft = Run(true, false);
        var noErosion = Run(false, true);
        World Of(MultiNationSim s, string name) => s.Nations.First(n => n.World.SelfPower!.Name == name).World;
        foreach (var name in new[] { "清", "ロシア" })
        {
            var w1 = Of(both, name);
            Assert.True(w1.EffectiveCorruption > 0, $"前提：{name} の実効汚職が 0（{w1.EffectiveCorruption}）＝この盤面では項を確かめられない");
            Assert.True(w1.DynasticMandate < Of(noGraft, name).DynasticMandate, $"{name}：汚職の項があっても天命が下がらない（{Of(noGraft, name).DynasticMandate}→{w1.DynasticMandate}）＝天命の汚職の項が消えている");
            Assert.Equal(Of(noErosion, name).DynasticMandate, w1.DynasticMandate);   // 侵食の係数は天命に届かない（同じ四半期）
            Assert.True(w1.Legitimacy < Of(noErosion, name).Legitimacy, $"{name}：腐敗の侵食があっても正統性が下がらない（{Of(noErosion, name).Legitimacy}→{w1.Legitimacy}）＝腐敗→正統性の侵食が消えている");
        }
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
