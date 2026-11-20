using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1258) c128uf：同じ統一がもう成立した規則を統一の候補から外す（旗 <see cref="Coefficients.UnificationSkipFormed"/>）の単体の機構の試験。
/// 盤面は試験の中で置く＝独の 6 邦・統一の門（ナショナリズムの取り分）だけを 0 に置き、他の係数は既定。成立は Formations.Check と同じ 2 行（RenameNation と FormationTier）で置く。
/// 期待値は手で書いた規則の名（〈ドイツ〉〈北ドイツ連邦〉）と null。旗 0 の対照を同じ盤面で並べる（旗 0 で赤＝旗を見ない実装は T1・T4 で落ちる）。
/// 突然変異（measurements/m1258_20261120/mut/）：μ0 旗を見ない→T1・T4／μ1 自分の名と比べる→T1・T4／μ2 札を見ず段だけ→T3／μ3 AI の呼び手だけ→T1・T4／μ4 札でなく規則の名だけ→T1・T4。</summary>
public class UnificationSkipFormedTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    private static (MultiNationSim sim, Power rhine, Power prussia) Board(double flag)
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var rhine = sim.Diplomacy.Powers.First(p => p.BaseName == "ライン諸邦");
        var prussia = sim.Diplomacy.Powers.First(p => p.BaseName == "プロイセン");
        foreach (var p in sim.Diplomacy.Powers)
        {
            p.IsPlayer = p != rhine;   // 決めるのはライン諸邦だけ（AI の道）
            if (p.Nation is null) continue;   // 抽象の勢力（国を持たない）
            p.Nation!.Coef.UnificationNatioShare = new Share(0.0);   // プレイヤーの動詞は自国の係数を読む＝同じ門を開ける
            p.Nation!.Coef.UnificationSkipFormed = flag;
        }
        return (sim, rhine, prussia);
    }

    private static Coefficients C(double flag) => new() { UnificationNatioShare = new Share(0.0), UnificationSkipFormed = flag };

    /// <summary>Formations.Check の成立の 2 行と同じ置き方（名を付け、段を上げる）。</summary>
    private static void Form(Diplomacy d, Power p, string name, int tier)
    {
        Assert.True(d.RenameNation(p, name), $"盤面の前提＝{name} を名乗れる");
        p.FormationTier = tier;
    }

    [Fact]
    public void T1_他の国がドイツ帝国を名乗ると資格のある邦の候補が消える_旗0では残る()
    {
        var (s1, r1, p1) = Board(1);
        Form(s1.Diplomacy, p1, "ドイツ帝国", 2);
        int checkedPowers = 0;
        foreach (var q in s1.Diplomacy.Powers.Where(q => q != p1 && q.Nation is not null))
        {
            Assert.Null(Formations.UnificationCandidate(s1.Diplomacy, q, q.Nation!, C(1)));
            checkedPowers++;
        }
        Assert.True(checkedPowers >= 5, $"対照：調べた邦が少なすぎる（{checkedPowers}）");

        var (s0, r0, p0) = Board(0);
        Form(s0.Diplomacy, p0, "ドイツ帝国", 2);
        Assert.Equal("ドイツ", Formations.UnificationCandidate(s0.Diplomacy, r0, r0.Nation!, C(0))?.NewName);   // 旗 0＝今の規則（候補が残る）
    }

    [Fact]
    public void T2_まだ誰も成立していない盤面では旗1と旗0の候補が同じ()
    {
        var (s1, _, _) = Board(1);
        var (s0, _, _) = Board(0);
        var expect = new Dictionary<string, string> { ["プロイセン"] = "北ドイツ連邦" };
        int nonNull = 0;
        foreach (var q1 in s1.Diplomacy.Powers.Where(q => q.Nation is not null))
        {
            var q0 = s0.Diplomacy.Powers.First(x => x.BaseName == q1.BaseName);
            var a = Formations.UnificationCandidate(s1.Diplomacy, q1, q1.Nation!, C(1))?.NewName;
            var b = Formations.UnificationCandidate(s0.Diplomacy, q0, q0.Nation!, C(0))?.NewName;
            Assert.Equal(b, a);
            Assert.Equal(expect.GetValueOrDefault(q1.BaseName, "ドイツ"), a);   // 手で書いた表（プロイセンは段 1 の北ドイツ連邦が先・他は段 2 のドイツ）
            if (a is not null) nonNull++;
        }
        Assert.Equal(6, nonNull);
    }

    [Fact]
    public void T3_別の札の成立や低い段の成立は候補を消さない()
    {
        var (s1, r1, p1) = Board(1);
        Form(s1.Diplomacy, p1, "ローマ", 3);   // 文化の無い別の規則（札＝ローマ）・段 3
        Assert.Equal("ドイツ", Formations.UnificationCandidate(s1.Diplomacy, r1, r1.Nation!, C(1))?.NewName);

        var (s2, r2, p2) = Board(1);
        Form(s2.Diplomacy, p2, "北ドイツ連邦", 1);   // 札は北ドイツ連邦・段 1＜ドイツの段 2
        Assert.Equal("ドイツ", Formations.UnificationCandidate(s2.Diplomacy, r2, r2.Nation!, C(1))?.NewName);
    }

    [Fact]
    public void T4_プレイヤーの動詞とAIの宣戦も同じ判定を通る()
    {
        // プレイヤーの道：World.UnificationRule と CanUnificationWar（自国の係数を読む）
        var (s1, r1, p1) = Board(1);
        Form(s1.Diplomacy, p1, "ドイツ帝国", 2);
        var w1 = r1.Nation!;
        Assert.Null(w1.UnificationRule);
        Assert.False(w1.CanUnificationWar(p1, out _));
        // AI の道：TryUnificationWars で宣戦しない
        s1.Diplomacy.TryUnificationWars(C(1));
        int wars1 = s1.Diplomacy.Powers.Count(t => t != r1 && s1.Diplomacy.IsAtWar(r1, t));
        Assert.Equal(0, wars1);

        // 対照（旗 0）：同じ盤面で候補が立ち、AI は宣戦する
        var (s0, r0, p0) = Board(0);
        Form(s0.Diplomacy, p0, "ドイツ帝国", 2);
        Assert.Equal("ドイツ", r0.Nation!.UnificationRule?.NewName);
        s0.Diplomacy.TryUnificationWars(C(0));
        int wars0 = s0.Diplomacy.Powers.Count(t => t != r0 && s0.Diplomacy.IsAtWar(r0, t));
        Assert.True(wars0 >= 1, $"対照：旗 0 で宣戦が無い（{wars0}）");
    }
}
