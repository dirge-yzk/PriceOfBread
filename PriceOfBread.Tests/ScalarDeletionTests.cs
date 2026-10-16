using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(373)(377) 政治のスカラーの削除の番人。事前登録 measurements/ps_20261016_prereg.txt・実装ログ (373)(377)。
/// 制度疲労（(373)）と汚職の政治の 2 項（天命の汚職の項・腐敗→正統性の侵食＝(377)）は物理削除した＝名前で復活を禁じる（期待値は本体の式から作らない）。
/// 厭戦の 2 項（闘争度の目標項 WarWearinessMilitancy・天命の戦争の項 DynMandateWarWeight）は残す＝外すと戦争→内政の経路が消える（(377) の読み）。
/// それが黙って消えないように、厭戦のストックを直に置いて〈闘争度の目標項が 0 でない〉〈天命が下がる〉を対照つきで確かめる（何もしない機構が緑にならない）。
/// 突然変異で赤になる箇所：削除した名前の復活・Politics の warWeariness／dmWar の項を外す。</summary>
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
    public void 汚職の政治の2項は物理削除済み_旗とDiagの欄も残らない()
    {
        // (377)：作者 2026-10-16「汚職→廃止」。財政の漏れ（Fiscal の官僚への移転）は別＝作者に聞く物 Q1 なので、ここでは禁じない。
        string[] coefGone = ["DynMandateGraftWeight", "CorruptionLegitimacyErosion", "DeleteCorruptionScalars", "DeleteWarWearinessScalars"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        string[] worldGone = ["DiagDmWar", "DiagDmGraft", "DiagCorrErosion"];
        foreach (var n in worldGone) Assert.Null(typeof(World).GetMember(n, All).FirstOrDefault());
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
