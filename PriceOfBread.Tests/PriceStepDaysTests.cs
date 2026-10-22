using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spg2) ① 国内価格だけの刻み（係数 <see cref="Coefficients.PriceOnlyStepDays"/>）の番人：入力が月の中で変わらない市場（需給・在庫は境界相だけが書く）で、
/// 1か月（30日）を 15 日×2 で進めた値段は、10 日×3（今の既定）と同じ積分になる（閉じた式の小刻み〔MarketFlow.SubStep〕が h の約数から刻みを選ぶ）。
/// 許しは 10 日×3 と 1 日×30（真値）の差を物差しにする（閉じた式は y が一定のときだけ厳密＝0 ではない）：15 日×2 と 1 日×30 の差が、10 日×3 と 1 日×30 の差の 3 倍＋相対 1e-3 以内。
/// 盤面＝参照の近く（1.2 倍）・割高（5 倍）・割安（0.3 倍）・帯の上の端の手前（15 倍）の財と、需要が供給を上回る財・下回る財。⚠突然変異「15 日を 1 歩で（小刻みしない）」で赤になる盤面を含む（割高の財）。</summary>
public class PriceStepDaysTests
{
    private static double[] Run(int days, int times, (double mult, double demand, double supply, double stock)[] cases)
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var r = sim.World.Region; var c = sim.World.Coef;
        var goods = r.Goods.Where(g => g.Tier != GoodTier.Service && g.RefPrice > 0).Take(cases.Length).ToArray();
        Assert.True(goods.Length == cases.Length, "前提が崩れた：財が足りない");
        foreach (var g in r.Goods) { var m0 = r.Markets[g.Id]; m0.LastDemand = 0; m0.LastProduced = 0; }   // 他の財は死んだ財（動かさない）
        for (int i = 0; i < cases.Length; i++)
        {
            var m = r.Markets[goods[i].Id];
            m.LastDemand = cases[i].demand; m.LastProduced = cases[i].supply; m.LastImport = 0; m.LastExport = 0; m.Stock = cases[i].stock;
            m.Price = cases[i].mult * goods[i].RefPrice * r.PriceLevel;
        }
        for (int t = 0; t < times; t++) MarketFlow.StepDay(r, c, days);
        return goods.Select(g => r.Markets[g.Id].Price / (g.RefPrice * r.PriceLevel)).ToArray();
    }

    [Fact]
    public void 国内価格の15日の旬は10日の旬と同じ積分になる()
    {
        var cases = new (double mult, double demand, double supply, double stock)[]
        {
            (1.2, 100, 100, 100),   // 参照の近く（需給は釣り合い・値段だけが離れている）
            (5.0, 100, 100, 100),   // 割高（小刻みが要る）
            (0.3, 100, 100, 100),   // 割安
            (15.0, 100, 100, 100),  // 帯の上の端の手前
            (1.0, 150, 100, 50),    // 需要＞供給（値上がり）
            (1.0, 60, 100, 400),    // 需要＜供給（値下がり）
        };
        var d1 = Run(1, 30, cases); var d10 = Run(10, 3, cases); var d15 = Run(15, 2, cases);
        for (int i = 0; i < cases.Length; i++)
        {
            double e10 = System.Math.Abs(d10[i] - d1[i]) / d1[i], e15 = System.Math.Abs(d15[i] - d1[i]) / d1[i];
            Assert.True(e15 <= 3 * e10 + 1e-3, $"盤面 {i}（参照の {cases[i].mult} 倍）：15 日×2 の差 {e15:E3} が 10 日×3 の差 {e10:E3} の 3 倍＋1e-3 を超えた（1日×30＝{d1[i]:F6}・10日＝{d10[i]:F6}・15日＝{d15[i]:F6}）");
        }
    }

    [Fact]
    public void 価格だけの刻みは30の約数だけを許す()
    {
        var a = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        a.World.Coef.PriceOnlyStepDays = 15;
        for (int d = 0; d < 30; d++) a.StepDay();   // 30 の約数＝例外なしで 1 か月進む
        var b = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        b.World.Coef.PriceOnlyStepDays = 7;          // 30 の約数でない＝止める
        Assert.ThrowsAny<System.InvalidOperationException>(() => { for (int d = 0; d < 30; d++) b.StepDay(); });
    }
}
