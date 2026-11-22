using System.Runtime.CompilerServices;
using PriceOfBread.App.Controls;
using PriceOfBread.SimCore;

namespace PriceOfBread.Tests;

/// <summary>財のアイコンの番人：目録の全ての財に、財の Id から組んだ画像の資源がある（番号は書かず目録を回す）。</summary>
public class GoodIconTests
{
    private static string Here([CallerFilePath] string p = "") => p;

    [Fact]
    public void 目録の全ての財にアイコンの資源がある()
    {
        string root = Path.GetDirectoryName(Path.GetDirectoryName(Here()))!;
        Assert.Contains(@"Assets\**", File.ReadAllText(Path.Combine(root, "PriceOfBread.App", "PriceOfBread.App.csproj")));   // 資源として埋め込む宣言
        var missing = new List<string>();
        for (int g = 0; g < Catalog.Goods.Length; g++)
            if (!File.Exists(Path.Combine(root, "PriceOfBread.App", "Assets", "Goods", GoodIcons.UriOf(g).Split('/')[^1]))) missing.Add(g + ":" + Catalog.Goods[g].Name);
        Assert.True(missing.Count == 0, "アイコンの無い財: " + string.Join(", ", missing));
    }

    [Fact]
    public void 財の表の行の鍵から財の番号へ戻る()
    {
        Assert.True(GoodIcons.IsGoodTable("production.goods"));
        Assert.False(GoodIcons.IsGoodTable("pop.regions"));
    }

    [Fact]
    public void 開始国の全てに国旗の画像がある()
    {
        string root = Path.GetDirectoryName(Path.GetDirectoryName(Here()))!;
        var missing = new List<string>();
        foreach (var n in WorldData.Nations())
            if (!File.Exists(Path.Combine(root, "PriceOfBread.App", "Assets", "Flags", n.Name + ".png"))) missing.Add(n.Name);
        Assert.True(missing.Count == 0, "国旗の無い開始国: " + string.Join(", ", missing));
        Assert.True(File.Exists(Path.Combine(root, "PriceOfBread.App", "Assets", "Flags", FlagIcons.UnknownFile + ".png")));
    }

    [Fact]
    public void 階級の番号の全てに絵がある()
    {
        string root = Path.GetDirectoryName(Path.GetDirectoryName(Here()))!;
        var missing = new List<int>();
        for (int c = 0; c < PopClass.Count; c++)
            if (!File.Exists(Path.Combine(root, "PriceOfBread.App", "Assets", "PopClasses", ClassIcons.UriOf(c).Split('/')[^1]))) missing.Add(c);
        Assert.True(missing.Count == 0, "絵の無い階級の番号: " + string.Join(", ", missing));
    }
}
