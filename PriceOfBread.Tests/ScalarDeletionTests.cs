using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(373) 政治のスカラーの削除 段0 の形（休眠）の番人。事前登録 measurements/ps_20261016_prereg.txt。
/// 旗 2 本（DeleteCorruptionScalars・DeleteWarWearinessScalars）は既定 0＝旧（不採用・枝に残す）。制度疲労の旗は採用して削除した。期待値は本体の式から作らず、〈旗 1 なら 0〉をリテラルで書く。
/// 対照（旗 0 で 0 でない）を必ず置く＝何もしない機構が緑にならない。
/// 突然変異で赤になる箇所：Politics の 240／343／344／387 の旗の枝（旗を読まない）・削除した名前の復活（制度疲労の番人）。</summary>
[Collection("Instruments")]
public class ScalarDeletionTests
{
    private static MultiNationSim Board(double flagC, double flagW, ulong seed = 83)
    {
        var sim = new MultiNationSim([WorldData.ByName("清"), WorldData.ByName("ロシア")], seed: seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) { n.World.Coef.DeleteCorruptionScalars = flagC; n.World.Coef.DeleteWarWearinessScalars = flagW; }
        return sim;
    }

    private static World Of(MultiNationSim sim, string name) => sim.Nations.First(n => n.World.SelfPower!.Name == name).World;

    [Fact]
    public void 旗の既定は休眠()
    {
        var c = new Coefficients();
        Assert.Equal(0.0, c.DeleteCorruptionScalars);
        Assert.Equal(0.0, c.DeleteWarWearinessScalars);
    }

    [Fact]
    public void 制度疲労は物理削除済み_係数も状態も読み手に残らない()
    {
        // 腕 BD（制度疲労だけ）が H の線と止める線に通って採用した削除（2026-10-16）。復活を黙って許さない＝名前で存在を禁じる（期待値は本体の式から作らない）。
        string[] coefGone = ["DeleteInstitutionalDecay", "DecayReformResistWeight", "DecayTechAdoptWeight", "DecayMilitaryWeight", "DecayShockRelief", "DecayShockCooldownQuarters",
            "DecayReactionRegrowth", "DecayLockinIndustrialization", "DecayCeilingUniversal", "ModernizationDecayCost", "ModernizationDecayRelief", "DecayFromPrivilege"];
        foreach (var n in coefGone) Assert.Null(typeof(Coefficients).GetField(n));
        string[] worldGone = ["InstitutionalDecay", "InstitutionalDecayCeiling", "DecayForReaders", "PrivilegeDecayRaw", "WorldMaxPrivilegeDecayRaw"];
        const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        foreach (var n in worldGone) Assert.Null(typeof(World).GetMember(n, all).FirstOrDefault());
        // 初期値の入り口（NationConfig／Scenario）にも残さない
        Assert.Null(typeof(NationConfig).GetField("InstitutionalDecay"));
        Assert.Null(typeof(Scenario).GetProperty("InitialInstitutionalDecay"));
    }

    [Fact]
    public void 汚職の2項は旗1で0_旗0では天命の汚職の項が負()
    {
        var off = Board(0.0, 0.0); off.Run(2);
        double graft0 = off.Nations.Min(n => n.World.DiagDmGraft);
        Assert.True(graft0 < 0.0, $"対照：旗 0 の天命の汚職の項が負でない（{graft0}）＝汚職の項が何もしない器になっている");
        Assert.All(off.Nations, n => Assert.True(n.World.DiagCorrErosion >= 0.0));
        var on = Board(1.0, 0.0); on.Run(2);
        Assert.All(on.Nations, n => { Assert.Equal(0.0, n.World.DiagDmGraft); Assert.Equal(0.0, n.World.DiagCorrErosion); });
    }

    [Fact]
    public void 厭戦の2項は旗1で0_旗0では闘争度の目標項と天命の戦争の項が働く()
    {
        // 厭戦のストックを直に置く（戦争を待たない）。Politics.Step は四半期に 1 回＝Run(1) で通る。
        MultiNationSim Armed(double flagW)
        {
            var sim = Board(0.0, flagW);
            foreach (var n in sim.Nations) n.World.SelfPower!.WarExhaustion = 0.6;
            sim.Run(1);
            return sim;
        }
        double MilWarMax(MultiNationSim sim) => sim.Nations.Max(n => n.World.AllPops().Max(p => p.MilWar));
        var off = Armed(0.0);
        Assert.True(MilWarMax(off) > 0.0, "対照：旗 0 の厭戦の闘争度項 MilWar が 0＝何もしない器になっている");
        Assert.True(off.Nations.Min(n => n.World.DiagDmWar) < 0.0, "対照：旗 0 の天命の戦争の項が負でない");
        var on = Armed(1.0);
        Assert.Equal(0.0, MilWarMax(on));
        Assert.All(on.Nations, n => Assert.Equal(0.0, n.World.DiagDmWar));
    }
}
