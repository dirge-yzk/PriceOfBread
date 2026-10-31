using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (725) 線 dsc2 段0′：計器 <c>dsc2_s</c>（<see cref="Instruments"/> の InstrumentsDsc.cs）の番人。
/// ①既定（POB_INSTRUMENTS_DSC を渡さない）では出さない＝専用の環境変数の入り切り（容量 1 種 300〜415 MB のため）。
/// ②見出しの列の名と順（読み手 scripts/instruments/dsc2_725/replay2.py が名前で引く 24 列）を手で書いた表と比べる。
/// 期待値は本体の定数から作らない（手で書いた表）。
/// </summary>
public class Dsc2InstrumentTests
{
    [Fact]
    public void Dsc2_OffByDefault()
    {
        Assert.NotEqual("1", System.Environment.GetEnvironmentVariable("POB_INSTRUMENTS_DSC"));   // テストの前提（渡していない）
        Assert.False(Instruments.DscOn);
    }

    [Fact]
    public void Dsc2_HeaderColumns()
    {
        string[] expected =
        [
            "kind", "quarter", "year", "nationUid", "nation", "col", "colName", "goodName", "isRgo", "qPerMonth", "valuePerMonth", "dgPerMonth",
            "engHomePerMonth", "homePopK", "gateMax", "techLevel", "bracket", "replacement3",
            "valueRefPerMonth", "steamFillMeanShare", "elecFillMeanShare", "popK", "nominalGdpPerMonth", "fxRate",
        ];
        Assert.Equal(expected, Instruments.DscHeaderText.Split('\t'));
    }
}
