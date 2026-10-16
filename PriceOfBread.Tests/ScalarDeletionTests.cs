using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(373) 政治のスカラーの削除 段0 の形（休眠）の番人。事前登録 measurements/ps_20261016_prereg.txt。
/// 旗 3 本（DeleteInstitutionalDecay・DeleteCorruptionScalars・DeleteWarWearinessScalars）は既定 0＝旧。期待値は本体の式から作らず、世界のデータ（清 0.8・オスマン 0.6・露 0.4・
/// 天井の共通床 0.3）と〈旗 1 なら 0〉をリテラルで書く。対照（旗 0 で 0 でない）を必ず置く＝何もしない機構が緑にならない。
/// 突然変異で赤になる箇所：Simulation の初期値の三項（疲労の初期値に旗を掛けない）・Politics の 240／343／344／387 の旗の枝（旗を読まない）。</summary>
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
        Assert.Equal(0.0, c.DeleteInstitutionalDecay);
        Assert.Equal(0.0, c.DeleteCorruptionScalars);
        Assert.Equal(0.0, c.DeleteWarWearinessScalars);
    }

    private static World Built(double flagD, string nation)
    {
        var cfg = WorldData.ByName(nation);
        var coef = new Coefficients { DeleteInstitutionalDecay = flagD };
        var sc = new Scenario { Seed = 1, Kind = ScenarioKind.Nation, NationConfig = cfg, InitialInstitutionalDecay = cfg.InstitutionalDecay, Coef = coef, SharedWorld = true };   // ToScenario と同じ初期値の写し
        return new Simulation(sc, new UtilityAgent()).World;
    }

    [Fact]
    public void 疲労の初期値は旗0で世界のデータのまま_旗1で0()
    {
        // 旗 0：清 0.8・オスマン 0.6・露 0.4（world.json）・天井＝初期値∨共通床 0.3。日本（初期 0）は天井だけ 0.3。
        var qing = Built(0.0, "清"); Assert.Equal(0.8, qing.InstitutionalDecay, 12); Assert.Equal(0.8, qing.InstitutionalDecayCeiling, 12);
        var ott = Built(0.0, "オスマン"); Assert.Equal(0.6, ott.InstitutionalDecay, 12); Assert.Equal(0.6, ott.InstitutionalDecayCeiling, 12);
        var rus = Built(0.0, "ロシア"); Assert.Equal(0.4, rus.InstitutionalDecay, 12); Assert.Equal(0.4, rus.InstitutionalDecayCeiling, 12);
        var jpn = Built(0.0, "日本"); Assert.Equal(0.0, jpn.InstitutionalDecay, 12); Assert.Equal(0.3, jpn.InstitutionalDecayCeiling, 12);
        // 旗 1：全て 0・天井も 0（書き手の門に入らない）
        foreach (var n in new[] { "清", "オスマン", "ロシア", "日本" })
        {
            var w = Built(1.0, n);
            Assert.Equal(0.0, w.InstitutionalDecay); Assert.Equal(0.0, w.InstitutionalDecayCeiling); Assert.Equal(0.0, w.DecayForReaders);
        }
    }

    [Fact]
    public void 疲労が0で天井も0なら書き手は走らず0のまま()
    {
        // 旗 1 と同じ初期状態を後から置く（MultiNationSim の係数は国ごとの別のインスタンスで、構築の後では初期値の三項に届かない）。
        var sim = Board(0.0, 0.0);
        var qing = Of(sim, "清");
        Assert.Equal(0.8, qing.InstitutionalDecay, 12);   // 対照：旗 0 の清は疲労が 0 でない
        foreach (var n in sim.Nations) { n.World.InstitutionalDecay = 0; n.World.InstitutionalDecayCeiling = 0; }
        sim.Run(6);
        foreach (var n in sim.Nations)
        {
            Assert.Equal(0.0, n.World.InstitutionalDecay); Assert.Equal(0.0, n.World.InstitutionalDecayCeiling);
            Assert.Equal(0.0, n.World.DecayForReaders);
        }
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
