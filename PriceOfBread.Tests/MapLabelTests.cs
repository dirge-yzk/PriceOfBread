using PriceOfBread.App;
using PriceOfBread.SimCore;

namespace PriceOfBread.Tests;

/// <summary>地図のラベルは英語のまま（作者の指示）。地図に出す州（座標のある全ての場所）は英語名の表に載っている＝日本語の州名が地図に浮かない。</summary>
public class MapLabelTests
{
    [Fact]
    public void 地図に出す州は全て英語名を持つ()
    {
        var missing = new List<string>();
        foreach (var pl in Places.All)
            if (!MapData.CityEn.ContainsKey(pl.Name)) missing.Add(pl.Name);
        Assert.True(missing.Count == 0, "英語名の無い州：" + string.Join("、", missing));
    }

    [Fact]
    public void 英語名の表の値に日本語の文字が無い()
    {
        var bad = MapData.CityEn.Where(kv => kv.Value.Any(ch => ch >= '\u3040' && ch <= '\u30FF' || ch >= '\u4E00' && ch <= '\u9FFF')).Select(kv => kv.Key).ToList();
        Assert.True(bad.Count == 0, "英語名に日本語が混じる州：" + string.Join("、", bad));
    }
}
