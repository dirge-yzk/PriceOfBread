using System.Text;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(347) 清算の作業領域（<c>InternationalMarket.ClearScratch</c>・スレッドごとに持ち越す）の番人。
/// 作業領域は清算の頭で必ず初期化して使う約束＝<b>前の試合が大きな盤面で汚した後でも、同じ種の試合は同じ貿易の出力を出す</b>。
/// 期待値は同じ出所から作らない：同じ木の「汚れていない（または別の汚れ方の）作業領域」で回した1回目と、大きな盤面（国の数・州の数・財の使い方が違う）で汚した後の2回目を突き合わせる。
/// 突然変異：<c>ClearScratch.FitZero</c> の <c>Array.Clear</c> を消す、または <c>FitRows</c> の <c>Array.Clear</c> を消すと赤になる（2026-10-15 に確かめた）。</summary>
public class ClearScratchTests
{
    /// <summary>貿易の出力の指紋＝国ごとの輸出入額・財別の輸出入・相手の名前・取引の相手の本位・州の港の取扱高と距離・世界の平均価格と取引量・基礎貨幣。丸めずビット列で書く。</summary>
    private static string Signature(MultiNationSim sim)
    {
        var sb = new StringBuilder();
        static string B(double v) => BitConverter.DoubleToInt64Bits(v).ToString("x16");
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            sb.Append(w.Region.Name).Append('|').Append(B(w.LastImportValue)).Append('|').Append(B(w.LastExportValue)).Append('|')
              .Append(B(w.TradePartnerStandardBalance)).Append('|').Append(B(w.LastTariffRevenuePerClearing)).Append('|').Append(B(w.BaseMoney.Value)).Append('\n');
            if (w.ExportsByGood is { } eg) foreach (var v in eg) sb.Append(B(v)).Append(',');
            sb.Append('\n');
            if (w.ImportsByGood is { } ig) foreach (var v in ig) sb.Append(B(v)).Append(',');
            sb.Append('\n');
            if (w.ImportFromByGood is { } imf) foreach (var s in imf) sb.Append(s ?? "-").Append(',');
            sb.Append('\n');
            if (w.ExportToByGood is { } exf) foreach (var s in exf) sb.Append(s ?? "-").Append(',');
            sb.Append('\n');
            foreach (var r in w.Regions)
                sb.Append(r.Name).Append(':').Append(B(r.LastPortThroughput)).Append(':').Append(B(r.LastTradeThrottle)).Append(':').Append(B(r.LastTradePotential)).Append(';');
            sb.Append('\n');
        }
        foreach (var v in sim.Market.Price) sb.Append(B(v)).Append(',');
        sb.Append('\n');
        foreach (var v in sim.Market.Volume) sb.Append(B(v)).Append(',');
        return sb.ToString();
    }

    private static string Small()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.StepQuarter(); sim.StepQuarter();
        return Signature(sim);
    }

    private static void Big()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7);
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.StepQuarter();
    }

    [Fact]
    public void 大きな盤面で作業領域を汚した後でも同じ種は同じ貿易の出力を出す()
    {
        // ★(348) 反証役 rf〔v〕低1：xUnit はスレッドを使い回す＝1回目の前にこのスレッドの作業領域を空にして「きれいな作業領域」の1回目を保証する
        var fld = typeof(InternationalMarket).GetField("_clearScratch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(fld);
        fld!.SetValue(null, null);
        string first = Small();
        Big();                    // 国61・州300 超・財の使い方も多い盤面で、同じスレッドの作業領域を大きく汚す
        string second = Small();  // 汚れた作業領域で同じ盤面を回す
        Assert.True(first.Length > 1000, "指紋が空に近い＝貿易の出力を読めていない（主張が空洞）");
        Assert.Equal(first, second);
    }

    [Fact]
    public void 貿易が実際に起きている_作業領域の対照()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France(), NationConfig.Prussia()], seed: 1789);
        foreach (var n in sim.Nations) n.TelemetryEnabled = false;
        sim.StepQuarter(); sim.StepQuarter();
        Assert.Contains(sim.Nations, n => n.World.LastImportValue > 0 || n.World.LastExportValue > 0);   // 越境の取引が1件も無いなら上の一致は空洞
        Assert.Contains(sim.Market.Volume, v => v > 0);
    }
}
