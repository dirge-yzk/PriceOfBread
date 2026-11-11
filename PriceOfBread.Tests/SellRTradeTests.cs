using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1047) 線 c99ov：旗 <see cref="Coefficients.SellRCountsTrade"/>（案 A〈作りすぎの罰〉＝労働の重みの売れる量に州の外への売りと買いを両側で数える）の器の試験。
/// 盤面＝1789 年から 1 四半期回した世界で、発見の門が閉じて眠っている生産者を 1 本ずつ起こす（目覚めた月は労働の重み＝付加価値×売れる量の極限＝平滑を通らない）。
/// 月の頭に EconomyStep が LastExport・LastImport を PrevExport・PrevImport へ写す＝試験は Run の前に LastExport・LastImport へ置く。
/// 期待の値は試験が自分で書く式（事前登録 measurements/m1047_20261111_prereg_ov.txt [1]）：旗 0＝max(L, 床)・旗 1＝max(L＋X−Y, 床)（NaN と負は床）。</summary>
public class SellRTradeTests
{
    const double Tol = 1e-9;

    sealed record Case(string Name, double Flag, Func<double, double> X, Func<double, double> Y, Func<double, double, double, double, double> Expect);

    static double Floor(double v, double f) => v > f ? v : f;

    /// <summary>眠った生産者を起こし、LastExport＝X(L)・LastImport＝Y(L) を置いて 1 か月回し、労働の重みを期待と比べる。母集合＝付加価値が正で前の月の需要が 20 以上の市場。</summary>
    static void Check(Case k, int need)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789, _ => new UtilityAgent());
        sim.StepQuarter();
        int done = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; var c = w.Coef;
            foreach (var region in w.Regions)
            {
                if (done >= need) break;
                foreach (var s in region.Producers)
                {
                    if (done >= need) break;
                    if (EconomyStep.SleepState(region, s, c) != 1 || s.DiscoveryRow < 0 || w.Discovered[s.DiscoveryRow]) continue;
                    w.Discovered[s.DiscoveryRow] = true;
                    if (EconomyStep.SleepState(region, s, c) != 2) { w.Discovered[s.DiscoveryRow] = false; continue; }
                    s.SteamFill = 0; s.ElecFill = 0; s.EffFill = 0;
                    var m = region.Markets[s.GoodId];
                    double rent = m.Price - EconomyStep.InputCostPerUnit(s, region, region, c);
                    double L = m.LastDemand;
                    if (!(rent > 0) || !(L >= 20)) { w.Discovered[s.DiscoveryRow] = false; continue; }
                    double x = k.X(L), y = k.Y(L);
                    m.LastExport = x; m.LastImport = y;
                    double expect = rent * k.Expect(L, x, y, c.TargetStockFloor);
                    double flag0 = c.SellRCountsTrade;
                    c.SellRCountsTrade = k.Flag;
                    s.LaborWeight = 1e6;
                    EconomyStep.Run(region, c, 1.0, w.Bank, w);
                    c.SellRCountsTrade = flag0;
                    string who = $"{k.Name}／{w.SelfPower?.Name}／{region.Name}／{Catalog.Goods[s.GoodId].Name}";
                    Assert.True(double.IsFinite(s.LaborWeight), $"{who}：労働の重みが有限でない（{s.LaborWeight}）");
                    Assert.True(Math.Abs(s.LaborWeight - expect) <= Tol * Math.Max(1.0, Math.Abs(expect)),
                        $"{who}：労働の重み {s.LaborWeight}・期待 {expect}（付加価値 {rent}・L {L}・X {x}・Y {y}）");
                    done++;
                    break;   // 1 つの州から 1 本（同じ州を続けて回さない）
                }
            }
        }
        Assert.True(done >= need, $"{k.Name}：起こせた生産者が {done} 本（{need} 本以上が要る）＝ハーネスが空回りしている");
    }

    [Fact]
    public void T1_旗0は州の外への売り買いを読まない() =>
        Check(new Case("T1 旗0", 0, L => L + 5, L => 0.25 * L, (L, x, y, f) => Floor(L, f)), 3);

    [Fact]
    public void T2_旗1は州の外への売りの分だけ売れる量を上げる() =>
        Check(new Case("T2 上がる", 1, L => L + 5, L => 0, (L, x, y, f) => Floor(2 * L + 5, f)), 3);

    [Fact]
    public void T3_旗1は州の外からの買いの分だけ売れる量を下げる() =>
        Check(new Case("T3 下がる", 1, L => 0, L => 0.5 * L, (L, x, y, f) => Floor(0.5 * L, f)), 3);

    [Fact]
    public void T4_旗1で買いが需要を超えると床() =>
        Check(new Case("T4 床", 1, L => 0, L => L + 100, (L, x, y, f) => f), 3);

    [Fact]
    public void T5_旗1で売りがNaNなら床() =>
        Check(new Case("T5 NaN", 1, L => double.NaN, L => 0, (L, x, y, f) => f), 3);

    [Fact]
    public void T6_旗1で売りと買いの両方() =>
        Check(new Case("T6 両方", 1, L => 0.75 * L, L => 0.25 * L, (L, x, y, f) => Floor(1.5 * L, f)), 3);
}
