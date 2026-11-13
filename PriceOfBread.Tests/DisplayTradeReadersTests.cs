using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1100) 線 c105ix：画面の試験 2 本（(1088) の反証役 v の中・★(790)）。事前登録 measurements/m1100_20261113_prereg_ix.txt [3]。
/// 試験 1＝金融の表の貿易収支の行が、国の輸出と輸入の配列から紙の式 (Σ輸出−Σ輸入)÷清算の月数 で作った額と一致する（符号と書式まで）。
/// 試験 2＝為替が 1 でない盤面で、輸入の表の世界価格比が 首都の相場×為替÷世界価格 になる。
/// 期待は配列と市場の値段から試験の中で作る（TradeTables.Totals や表の組み立てを呼ばない）。
/// </summary>
public class DisplayTradeReadersTests
{
    private static (MultiNationSim Sim, World W) Board(int quarters)
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia(), NationConfig.France()], seed: 1789);
        for (int q = 0; q < quarters; q++) sim.StepQuarter();
        return (sim, sim.Nations[0].World);
    }

    [Fact]
    public void 貿易収支の行は輸出と輸入の配列の差の月割と一致する()
    {
        var (sim, w) = Board(8);
        int rowsChecked = 0;
        foreach (var n in sim.Nations)
        {
            var wn = n.World;
            if (wn.ExportsByGood is not { } eg || wn.ImportsByGood is not { } ig) continue;
            double ex = 0, im = 0;
            for (int g = 0; g < Catalog.Goods.Length; g++)
            {
                if (g < eg.Length && eg[g] > 1e-9) ex += eg[g];
                if (g < ig.Length && ig[g] > 1e-9) im += ig[g];
            }
            double bal = (ex - im) / wn.Coef.MonthsPerClearing;
            var row = FiscalTables.Finance.Read(sim, wn).Rows.FirstOrDefault(r => r.Key == "tradeBalance");
            Assert.NotNull(row);
            string expect = $"{(bal >= 0 ? "＋" : "−")}{DisplayFormat.Money(Math.Abs(bal))}/月";
            Assert.Equal(expect, (string)row!.Cells[1]!);
            if (ex + im > 0) rowsChecked++;
        }
        Assert.True(rowsChecked >= 2, $"輸出入のある国が少ない {rowsChecked}");
    }

    [Fact]
    public void 為替が1でない盤面で世界価格比は首都の相場に為替を掛けて世界価格で割る()
    {
        var (sim, w) = Board(8);
        Assert.NotNull(w.ImportsByGood);
        foreach (double fx in new[] { 1.0, 2.0, 0.5 })
        {
            w.ExchangeRate = fx;
            var data = TradeTables.Imports.Read(sim, w);
            var wp = sim.Market.Price;
            int checkedRows = 0;
            foreach (var row in data.Rows)
            {
                if (!int.TryParse(row.Key, out int g) || g < 0) continue;   // 「その他」の行は財の列に入らない
                double p = Math.Max(wp[g], 1e-9);
                double expect = w.Region.Markets[g].Price * fx / p;
                double got = ((NumberCell)row.Cells[2]!).Value;
                Assert.True(Math.Abs(got - expect) <= 1e-9 * Math.Max(1.0, Math.Abs(expect)), $"財{g} 為替{fx}：期待 {expect}・表 {got}");
                checkedRows++;
            }
            Assert.True(checkedRows >= 3, $"為替{fx}：調べた行が少ない {checkedRows}");
        }
    }
}
