using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1423) 線 c152ms：軍国主義の軍の倍率 MilitarismStrengthBonus 1.3→1.0（作者 2026-11-11 の問い 10 の A＝消す・段 1＝既定の 1 行）の単体の機構の試験。
/// 事前登録 measurements/m1423_20261127_prereg_ms.txt。盤面は試験の中で置く（練度の入力＝識字・官僚制・機関車の行・徴兵の法）。
/// 期待値はエンジンの係数を読まずに書く：T1・T2 は「練度が戦力に掛からない」＝1 と等しさ。T3 は練度 0 の国の戦力の式（軍国の因子なし）を試験の中で組み直す。
/// 突然変異：M1＝係数を 1.3 に戻す（T1・T2 が赤）・M2＝1.15（T1・T2 が赤）・M3＝練度を cohesion の側へ掛ける（T2 が赤）。
/// </summary>
public class MilitarismBonusRemovalTests
{
    private const ulong Seed = 1789UL;

    private static MultiNationSim Sim()
        => new([NationConfig.France(), NationConfig.Prussia()], Seed, _ => new NullAgent());

    private static void Sync(MultiNationSim sim)
        => typeof(Diplomacy).GetMethod("SyncPowers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(sim.Diplomacy, [sim.Nations[0].World.Coef]);

    /// <summary>練度 1 の盤面＝識字 1・専門官僚制・機関車の行・志願兵（勤務の長さ 1）。</summary>
    private static void SetProfessional(World w)
    {
        foreach (var r in w.Regions) foreach (var p in r.Pops) p.Literacy = 1.0;
        w.Bureaucracy = BureaucracyLaw.Professional;
        w.Discovered[Discovery.LocomotiveRow] = true;
        w.ConscriptionLaw = ConscriptionLaw.Volunteer;
    }

    /// <summary>練度 0 の盤面＝猟官制・機関車の行なし（組織 0＝識字が何であっても練度 0）。</summary>
    private static void SetAmateur(World w)
    {
        w.Bureaucracy = BureaucracyLaw.Spoils;
        w.Discovered[Discovery.LocomotiveRow] = false;
    }

    [Fact]
    public void T1_練度1の国でも軍国の因子は1()
    {
        var w = Sim().Nations[0].World; var c = w.Coef;
        SetProfessional(w);
        Assert.True(PoliticalQuantities.ArmyProfessionalism(w) > 0.99, $"盤面の前提：練度 {PoliticalQuantities.ArmyProfessionalism(w)} が 1 でない");
        Assert.Equal(1.0, ArmyShape.Quality(w, c).Militarism);
    }

    [Fact]
    public void T2_兵数が同じで練度だけ違うと戦力が等しい()
    {
        var sim = Sim(); var w = sim.Nations[0].World; var p = w.SelfPower!;
        SetAmateur(w); Sync(sim);
        Assert.Equal(0.0, PoliticalQuantities.ArmyProfessionalism(w));
        double m0 = p.Military, land0 = p.LandPower;
        Assert.True(m0 > 1e-6, $"盤面の前提：兵が居ない（戦力 {m0}）");

        SetProfessional(w); Sync(sim);
        Assert.True(PoliticalQuantities.ArmyProfessionalism(w) > 0.99);
        double m1 = p.Military, land1 = p.LandPower;

        SetAmateur(w); Sync(sim);
        Assert.Equal(m0, p.Military);   // 対照：同期は同じ盤面で同じ値（練度の他に動いた入力が無い）

        Assert.Equal(m0, m1);
        Assert.Equal(land0, land1);
    }

    [Fact]
    public void T3_練度0の国の戦力は軍国の因子の無い式()
    {
        var sim = Sim(); var w = sim.Nations[0].World; var c = w.Coef; var p = w.SelfPower!;
        SetAmateur(w); Sync(sim);
        double cohesion = w.EthnicEnabled ? 1 + c.UnityMilitaryQuality * (w.EthnicUnity - 0.5) : 1.0;
        double quality = 1 + c.MilitaryQualityBonus * w.Region.MilitaryQualityFill + c.MilitaryTier3Bonus * w.Region.MilitaryTier3Fill;
        double want = w.SoldierSizeK() / c.SoldiersPerMilitary * w.MilitaryReadiness * quality * cohesion;
        Assert.True(want > 1e-6, $"盤面の前提：兵が居ない（{want}）");
        Assert.Equal(want, p.Military, 12);
    }
}
