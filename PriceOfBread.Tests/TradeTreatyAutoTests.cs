using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>通商条約の自動化（作者指示2026-09-07「通商条約は私の指示ですか？これは自動化したい」）。
/// 旧＝AI同士だけが四半期の掃引で締結し、プレイヤー国は IsPlayer の2行で明示的に除外されていた（手動のボタンで提案する非対称）。
/// ここで測るのは**同一性**＝同じ世界を2本走らせ、片方だけ IsPlayer を立てても結果がビット同値であること（AI＝プレイヤー対称）。</summary>
public class TradeTreatyAutoTests
{
    /// <summary>全ての実国家を自由貿易側（州関税利害=−1）に置き、1国だけ低い関税目標を持たせた世界を作る。</summary>
    private static MultiNationSim Build(int lowIndex)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        var reals = sim.Diplomacy.Powers.Where(p => p.Nation is not null).ToList();
        for (int i = 0; i < reals.Count; i++)
        {
            var w = reals[i].Nation!;
            foreach (var r in w.Regions) r.TariffInterest = -1.0;   // 全州が自由貿易を望む＝国平均<0＝条約の条件を満たす
            w.Policy.TariffTarget = i == lowIndex ? 0.10 : 0.50;    // 1国だけ低い＝掃引でそこへ寄る
        }
        return sim;
    }

    private static List<double> Targets(MultiNationSim sim)
        => sim.Diplomacy.Powers.Where(p => p.Nation is not null).Select(p => p.Nation!.Policy.TariffTarget).ToList();

    [Fact]
    public void プレイヤー国もAIと同じ掃引で通商条約を結ぶ()
    {
        var c = new Coefficients();
        Assert.True(c.TradeTreatyEnabled > 0, "既定で点火している機構（0なら即return＝この検査は無意味）");

        // 腕1＝操作国なし（従来のAI同士の掃引）
        var simAi = Build(lowIndex: 0);
        simAi.Diplomacy.TradeTreaties(c);
        var afterAi = Targets(simAi);
        int moved = afterAi.Count(t => t < 0.49);
        Assert.True(moved > 1, $"掃引が実際に働いていること（下がった国 {moved}）");

        // 下がった国のうち1つを操作国に選ぶ（＝旧実装なら除外されて下がらなかった国）
        int idx = Enumerable.Range(1, afterAi.Count - 1).First(i => afterAi[i] < 0.49);

        // 腕2＝同じ世界の同じ国に IsPlayer を立てる
        var simPl = Build(lowIndex: 0);
        var realsPl = simPl.Diplomacy.Powers.Where(p => p.Nation is not null).ToList();
        realsPl[idx].IsPlayer = true;
        simPl.Diplomacy.TradeTreaties(c);
        var afterPl = Targets(simPl);

        Assert.Equal(afterAi, afterPl);                    // 操作国かどうかで世界が変わらない＝対称
        Assert.True(afterPl[idx] < 0.49, "操作国自身も関税目標が条約で下がる（旧実装では除外されて 0.50 のままだった）");
    }

    [Fact]
    public void 機構をOFFにすると誰も動かない()
    {
        var c = new Coefficients { TradeTreatyEnabled = 0 };
        var sim = Build(lowIndex: 0);
        var reals = sim.Diplomacy.Powers.Where(p => p.Nation is not null).ToList();
        reals[1].IsPlayer = true;
        var before = Targets(sim);
        sim.Diplomacy.TradeTreaties(c);
        Assert.Equal(before, Targets(sim));
    }
}
