using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PriceOfBread.App.Controls;

/// <summary>財のアイコン（Assets/Goods/＜財の Id＞.png）。財ごとに 1 回だけ読み使い回す。画像が無い財は null（空）。
/// パスは財の Id から実行時に組む（番号を数字で書かない＝財の削減で振り直されても名前の通りに引く）。</summary>
public static class GoodIcons
{
    private static readonly Dictionary<int, IImage?> Cache = new();

    public static string UriOf(int goodId) => "avares://PriceOfBread.App/Assets/Goods/" + goodId.ToString(CultureInfo.InvariantCulture) + ".png";

    public static IImage? Of(int goodId)
    {
        if (goodId < 0) return null;
        if (Cache.TryGetValue(goodId, out var hit)) return hit;
        IImage? img = null;
        try
        {
            var uri = new Uri(UriOf(goodId));
            if (AssetLoader.Exists(uri)) { using var s = AssetLoader.Open(uri); img = new Bitmap(s); }
        }
        catch { img = null; }
        return Cache[goodId] = img;
    }

    public static IImage? OfKey(string? rowKey) => int.TryParse(rowKey, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ? Of(id) : null;

    /// <summary>行の鍵が財の Id である表（先頭の列の名前の左にアイコンを置く）。</summary>
    public static bool IsGoodTable(string tableKey) => GoodTableKeys.Contains(tableKey);

    private static readonly HashSet<string> GoodTableKeys = ["market.prices", "production.goods", "production.equipment", "production.labor", "trade.imports", "trade.exports"];
}

/// <summary>財の Id（整数）から画像へ（axaml の束縛用）。</summary>
public sealed class GoodIconConverter : IValueConverter
{
    public static readonly GoodIconConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int id ? GoodIcons.Of(id) : null;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
