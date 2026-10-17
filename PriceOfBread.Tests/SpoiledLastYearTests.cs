using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(398) 廃棄量の12か月の和（<see cref="Market.SpoiledLastYear"/>）は、月次の決算が足し直す欄から、廃棄のリング（<see cref="Market.SpoilLog"/>）を読まれたときに足す性質へ移った。
/// 画面（生産タブの廃棄率）と診断（spoilprobe）が読む値が「直近12か月の廃棄量の和」のままであることを、リングの外の出所（月ごとの在庫×損耗の率）から数え直して確かめる。</summary>
public class SpoiledLastYearTests
{
    /// <summary>肉の在庫を月ごとに違う量に置いて月の経済を14回回す：直近12か月（3か月目から14か月目）の廃棄量の和＝性質の値。1か月目と2か月目の廃棄は落ちている。
    /// 廃棄量＝その月の始めの在庫×損耗の率（冷蔵の発見が無ければ率そのもの＝EconomyStep.Run の廃棄の段が最初に読む在庫）。
    /// ⚠壊し方＝リングの書き込み位置を進めない（同じ位置を上書き＝和が1か月ぶんに縮む）／和を13か月ぶん足す／和を12項の途中で打ち切る → 赤。</summary>
    [Fact]
    public void 廃棄量の12か月の和は直近12か月の廃棄を足した値で古い月は落ちる()
    {
        var s = new Simulation(NationConfig.France().ToScenario(21), new NullAgent());
        s.Run(8);
        var w = s.World; var r = w.Region;
        w.Discovered[Discovery.ReeferRow] = false;   // 冷蔵が無い＝損耗の率そのもの
        int gi = Catalog.MeatId;
        var m = r.Markets[gi];
        double wear = Catalog.Goods[gi].WearRatePerMonth;
        Assert.True(wear > 0, "前提が崩れた（肉が腐らない）");
        var lost = new List<double>();
        for (int k = 0; k < 14; k++)
        {
            double stock = 1000 + 37.5 * k * (k % 3 + 1);   // 月ごとに違う在庫（和の項が全部違う）
            m.Stock = stock;
            lost.Add(stock * wear);
            EconomyStep.Run(r, w.Coef, 1.0, w.Bank, w);
        }
        double expected = 0;
        for (int k = 2; k < 14; k++) expected += lost[k];   // 直近12か月＝3か月目から14か月目
        Assert.Equal(expected, m.SpoiledLastYear, 6);
        double withOldest = expected + lost[1];
        Assert.True(Math.Abs(m.SpoiledLastYear - withOldest) > 1.0, "2か月目の廃棄が和に残っている");
    }
}
