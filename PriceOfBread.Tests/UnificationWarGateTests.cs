using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1195) c119us：統一の宣戦の可否を AI とプレイヤーで同じ判定にした番人。
/// 旧の AI（Diplomacy.TryUnificationWars）は World.CanUnificationWar を通らず DeclareWar を直接呼んだ＝同盟国へも統一の宣戦ができた（プレイヤーは〈同盟中〉で不可）。
/// 盤面は試験の中で置く＝独の 6 邦・統一の門（ナショナリズムの取り分）だけを 0 に置き、他の係数は既定。AI の決定に入るのはライン諸邦だけ。
/// 期待値は手で書いた理由の表（本体の判定の関数から作らない）。旧の道では T1・T3 が赤（measurements/m1195_20261118_prereg_us.txt §5）。</summary>
public class UnificationWarGateTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    private static (MultiNationSim sim, Power rhine, Coefficients c, List<Power> sorted) Board()
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var c = new Coefficients { UnificationNatioShare = new Share(0.0) };
        var rhine = sim.Diplomacy.Powers.First(p => p.BaseName == "ライン諸邦");
        foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = p != rhine;
        rhine.Nation!.Coef.UnificationNatioShare = new Share(0.0);   // プレイヤーの動詞は自国の係数を読む＝同じ門を開ける
        var rule = Formations.UnificationCandidate(sim.Diplomacy, rhine, rhine.Nation!, c);
        Assert.NotNull(rule);   // 盤面の前提＝候補が立つ
        var sorted = Formations.UnificationTargets(sim.Diplomacy, rhine, rule!).OrderBy(t => t.Military).ThenBy(t => t.Id).ToList();
        Assert.True(sorted.Count >= 2, $"盤面の前提＝標的が 2 つ以上（{sorted.Count}）");
        return (sim, rhine, c, sorted);
    }

    private static void Ally(Diplomacy d, Power a, Power b)
    {
        var set = (HashSet<long>)typeof(Diplomacy).GetField("_alliance", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(d)!;
        set.Add((long)typeof(Diplomacy).GetMethod("Key", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [a, b])!);
    }

    [Fact]
    public void T1_AIは同盟国へ統一の宣戦をせず同盟でない次の標的へ宣戦する()
    {
        var (sim, rhine, c, sorted) = Board();
        var d = sim.Diplomacy;
        var ally = sorted[0];
        Ally(d, rhine, ally);
        Assert.True(d.HasAlliance(rhine, ally));   // 盤面の前提
        Assert.DoesNotContain(sorted, t => d.IsAtWar(rhine, t));
        d.TryUnificationWars(c);
        Assert.False(d.IsAtWar(rhine, ally), "最も弱い標的が同盟国なのに統一の宣戦をした（プレイヤーは〈同盟中〉で不可）");
        Assert.True(d.IsAtWar(rhine, sorted[1]), "同盟でない次に弱い標的へ宣戦していない");
    }

    [Fact]
    public void T2_プレイヤーの動詞の可否と理由は手で書いた表と同じ()
    {
        var (sim, rhine, _, sorted) = Board();
        var d = sim.Diplomacy;
        var w = rhine.Nation!;
        var allied = sorted[0];
        var warred = sorted[1];
        Ally(d, rhine, allied);
        Assert.True(d.DeclareWar(rhine, warred, w.Coef));   // 盤面の前提＝交戦中の標的を置く
        var nonTargets = d.Powers.Where(p => p != rhine && p.Nation is not null && !sorted.Contains(p)).ToList();
        var expect = new List<(Power t, bool ok, string reason)> { (allied, false, "同盟中"), (warred, false, "交戦中") };
        foreach (var t in sorted.Skip(2))
        {
            string rs = d.IsAtWar(rhine, t) ? "交戦中" : d.HasAlliance(rhine, t) ? "同盟中" : "";
            expect.Add((t, rs == "", rs));
        }
        foreach (var t in nonTargets) expect.Add((t, false, "必要州の保有国ではない"));
        expect.Add((rhine, false, "必要州の保有国ではない"));   // 自国は標的に入らない（UnificationTargets は自国を飛ばす）
        int checkedRows = 0;
        foreach (var (t, ok, reason) in expect)
        {
            bool got = w.CanUnificationWar(t, out string r);
            Assert.Equal(ok, got);
            if (!ok) Assert.Equal(reason, r); else Assert.Equal("", r);
            checkedRows++;
        }
        Assert.True(checkedRows >= 3, $"対照：調べた相手が少なすぎる（{checkedRows}）");
        Ally(d, rhine, warred);   // 交戦中かつ同盟中＝理由は交戦中が先
        Assert.False(w.CanUnificationWar(warred, out string both));
        Assert.Equal("交戦中", both);
        w.Coef.UnificationNatioShare = new Share(1.0001);   // 統一の門を閉じる
        Assert.False(w.CanUnificationWar(sorted[2 % sorted.Count], out string none));
        Assert.Equal("統一の機運なし", none);
    }

    [Fact]
    public void T3_AIが宣戦する相手はプレイヤーの動詞が許す最も弱い標的()
    {
        var (sim, rhine, c, sorted) = Board();
        var d = sim.Diplomacy;
        var w = rhine.Nation!;
        Ally(d, rhine, sorted[0]);
        var allowed = sorted.Where(t => w.CanUnificationWar(t, out _)).ToList();
        Assert.NotEmpty(allowed);   // 対照：動詞が許す相手がある
        Assert.True(allowed.Count < sorted.Count, "対照：動詞が許さない標的が無い＝判別しない");
        d.TryUnificationWars(c);
        var warred = sorted.Where(t => d.IsAtWar(rhine, t)).ToList();
        Assert.Equal([allowed[0]], warred);
    }
}
