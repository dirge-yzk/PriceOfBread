using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(329) 事前登録 追記 2：奢侈品の所得弾力で f＝1 へ倒れた POP×月を数える計器（読むだけ・Instruments 側の累計）の番人。
/// 計器は静的な状態（<see cref="Instruments.On"/>）を触るので「Instruments」コレクション（並列にしない）に置く。
/// 期待値は盤面の POP の階級と人数を数えた手の算術（計器の式を呼ばない）。
/// 突然変異（実装を一時的に壊して赤を確かめる・結果は実装ログ (329)）：①非正の数えを無効の数えに混ぜる ②非正の Size の和を件数にする ③ε＝0 でも数える（呼び出しの条件を外す）。</summary>
[Collection("Instruments")]
public class LuxuryRatioDiagTests
{
    private static Dictionary<string, double> Run(double epsilon, double worldY, Func<Pop, double> rOf, out Region region)
    {
        string dir = Path.Combine(Path.GetTempPath(), "lux329_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, null);
        try
        {
            var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
            var w = sim.World;
            w.Coef.LuxuryIncomeElasticity = epsilon;
            w.WorldRealIncomePerCapitaPerMonth = worldY;
            w.PriceLevel = 2.0;
            foreach (var p in w.AllPops())
            {
                p.SetSavings(1e12);
                p.LastEarnedPerMonth = new MoneyPerMonth(rOf(p) * 10.0 * p.Size * 2.0);   // r＝(所得÷物価÷人数)÷Y ⇒ 所得＝r×Y×人数×物価（Y＝10・物価 2）
            }
            EconomyStep.Run(w.Region, w.Coef, 1.0, w.Bank, w);
            region = w.Region;
            return Instruments.Extras329(w).ToDictionary(e => e.Name, e => double.Parse(e.Value, System.Globalization.CultureInfo.InvariantCulture));
        }
        finally { Instruments.Configure(null, null); try { Directory.Delete(dir, true); } catch { } }
    }

    [Fact]
    public void r非正のPOPと無効のPOPを別々に数える_資本家のrを負にした盤面の手の算術()
    {
        // 資本家の r＝−1（粗利潤が負の月）・他の階級 r＝1。Size が正の資本家の POP が非正（r≤0）へ、Size が 0 以下の POP が無効へ。
        var m = Run(0.5, 10.0, p => p.ClassId == PopClass.Capitalist ? -1.0 : 1.0, out var region);
        var pops = region.Pops;
        double total = pops.Count;
        double nonPos = pops.Count(p => p.Size > 0 && p.ClassId == PopClass.Capitalist);
        double nonPosSize = pops.Where(p => p.Size > 0 && p.ClassId == PopClass.Capitalist).Sum(p => p.Size);
        double invalid = pops.Count(p => !(p.Size > 0));
        Assert.True(nonPos >= 1, "前提が崩れた：Size が正の資本家の POP が盤面に無い（主張が空洞）");
        Assert.Equal(total, m["luxuryRatioPopMonths"]);
        Assert.Equal(nonPos, m["luxuryRatioNonPositivePopMonths"]);
        Assert.Equal(invalid, m["luxuryRatioInvalidPopMonths"]);
        Assert.Equal(nonPosSize, m["luxuryRatioNonPositiveSizeSum"], 9);
    }

    [Fact]
    public void 世界の値が0なら全てのPOPが無効_非正は0()
    {
        var m = Run(0.5, 0.0, p => 1.0, out var region);
        Assert.Equal((double)region.Pops.Count, m["luxuryRatioPopMonths"]);
        Assert.Equal((double)region.Pops.Count, m["luxuryRatioInvalidPopMonths"]);
        Assert.Equal(0.0, m["luxuryRatioNonPositivePopMonths"]);
    }

    [Fact]
    public void ε0では数えない_全ての列が0()
    {
        var m = Run(0.0, 10.0, p => p.ClassId == PopClass.Capitalist ? -1.0 : 1.0, out _);
        Assert.Equal(0.0, m["luxuryRatioPopMonths"]);
        Assert.Equal(0.0, m["luxuryRatioNonPositivePopMonths"]);
        Assert.Equal(0.0, m["luxuryRatioInvalidPopMonths"]);
        Assert.Equal(0.0, m["luxuryRatioNonPositiveSizeSum"]);
    }
}
