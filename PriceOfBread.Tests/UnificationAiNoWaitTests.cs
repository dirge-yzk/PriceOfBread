using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1188) c118ur：統一戦争の AI の根拠の無い待ち 2 本（宣戦の後の 3 年・見送りの後の 20 年）と AI だけの 30% の乱数を、
/// 既存の係数（UnificationAiPursueChance 1.0・UnificationAiWarGapYears 0）で外した番人。動詞（DeclareWar・CanUnificationWar）は AI とプレイヤーで共通のまま。
/// 盤面は試験の中で置く＝独の 6 邦・統一の門（ナショナリズムの取り分）だけを 0 に置き、他の係数は既定。
/// 旧の値（0.7・3）では T1・T2 が赤（measurements/m1188_20261117_prereg_ur.txt §4・§5）。</summary>
public class UnificationAiNoWaitTests
{
    private static readonly string[] German = ["ライン諸邦", "ヴュルテンベルク", "バイエルン", "ザクセン", "ハノーファー", "プロイセン"];

    private static (MultiNationSim sim, Power rhine, Coefficients c) Board(ulong unifSeed)
    {
        var sim = new MultiNationSim([.. WorldData.Nations().Where(n => German.Contains(n.Name))], 7UL, _ => new NullAgent());
        var c = new Coefficients { UnificationNatioShare = new Share(0.0) };   // 統一の門だけを開ける（この試験の題は門の後の AI の決定）
        sim.Diplomacy.UnificationRng = new DeterministicRng(unifSeed);
        var rhine = sim.Diplomacy.Powers.First(p => p.BaseName == "ライン諸邦");
        foreach (var p in sim.Diplomacy.Powers) p.IsPlayer = p != rhine;   // 他の邦（プロイセンほかも統一の候補を持つ）は AI の決定から外す＝統一の系列を引くのはライン諸邦だけ
        return (sim, rhine, c);
    }

    private static List<Power> Targets(MultiNationSim sim, Power p, Coefficients c)
    {
        var rule = Formations.UnificationCandidate(sim.Diplomacy, p, p.Nation!, c);
        Assert.NotNull(rule);   // 盤面の前提＝候補が立つ
        var t = Formations.UnificationTargets(sim.Diplomacy, p, rule!);
        Assert.NotEmpty(t);     // 盤面の前提＝標的がある
        return t;
    }

    [Fact]
    public void T1_候補と標的を持つAIは種によらず毎回宣戦する()
    {
        int oldWouldSkip = 0;
        for (ulong k = 1; k <= 12; k++)
        {
            if (new DeterministicRng(k).NextDouble() >= 0.7) oldWouldSkip++;   // 対照：旧の 0.7 では見送る種
            var (sim, rhine, c) = Board(k);
            var targets = Targets(sim, rhine, c);
            Assert.DoesNotContain(targets, t => sim.Diplomacy.IsAtWar(rhine, t));
            sim.Diplomacy.TryUnificationWars(c);
            Assert.True(targets.Any(t => sim.Diplomacy.IsAtWar(rhine, t)), $"種 {k}：候補と標的があるのに宣戦しなかった");
        }
        Assert.True(oldWouldSkip >= 1, "対照：種 1〜12 に旧の 0.7 で見送る種が無い＝試験が判別しない");
    }

    [Fact]
    public void T2_宣戦の後の四半期に待たずに次に弱い標的へ宣戦する()
    {
        var (sim, rhine, c) = Board(2UL);
        var d = sim.Diplomacy;
        var targets = Targets(sim, rhine, c);
        var weakest = targets.OrderBy(t => t.Military).ThenBy(t => t.Id).First();
        d.TryUnificationWars(c);
        Assert.True(d.IsAtWar(rhine, weakest), "1 回目：最も弱い標的（Military・Id の順）へ宣戦していない");
        int atWar1 = targets.Count(t => d.IsAtWar(rhine, t));
        Assert.True(atWar1 < targets.Count, "対照：1 回目の後にまだ交戦していない標的が残らない＝2 回目を判別できない");
        d.TryUnificationWars(c);   // 同じ年の次の四半期
        int atWar2 = targets.Count(t => d.IsAtWar(rhine, t));
        Assert.True(atWar2 > atWar1, $"2 回目：交戦中の標的が増えていない（{atWar1}→{atWar2}）＝宣戦の後の待ちが残っている");
    }

    [Fact]
    public void T3_プレイヤーの動詞は係数によらず同じでAIの標的は動詞が許す相手()
    {
        var (sim, rhine, c) = Board(2UL);
        var w = rhine.Nation!;
        w.Coef.UnificationNatioShare = new Share(0.0);
        var others = sim.Diplomacy.Powers.Where(p => p != rhine && p.Nation is not null).ToList();
        (bool, string) Verb(Power t) { bool ok = w.CanUnificationWar(t, out string r); return (ok, r); }
        w.Coef.UnificationAiPursueChance = 0.7; w.Coef.UnificationAiWarGapYears = 3;
        var oldV = others.Select(Verb).ToList();
        w.Coef.UnificationAiPursueChance = 1.0; w.Coef.UnificationAiWarGapYears = 0;
        var newV = others.Select(Verb).ToList();
        Assert.Equal(oldV, newV);
        Assert.Contains(newV, v => v.Item1);   // 対照：動詞が許す相手がある
        var allowed = others.Where((t, i) => newV[i].Item1).ToList();
        var weakest = Targets(sim, rhine, c).OrderBy(t => t.Military).ThenBy(t => t.Id).First();
        sim.Diplomacy.TryUnificationWars(c);
        Assert.True(sim.Diplomacy.IsAtWar(rhine, weakest), "AI が最も弱い標的へ宣戦していない");
        Assert.Contains(weakest, allowed);   // AI が宣戦した相手は、宣戦の前にプレイヤーの動詞が許した相手（同じ候補と標的の関数）
    }
}
