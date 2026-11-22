using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PriceOfBread.App.Controls;

/// <summary>画像の共通の読み込み（avares:// の 1 本のパスを 1 回だけ読み使い回す）。無い・読めない時は null。財のアイコン・国旗・階級の絵が共有する。</summary>
public static class Pictures
{
    private static readonly Dictionary<string, IImage?> Cache = new();

    public static IImage? Load(string uriText)
    {
        if (Cache.TryGetValue(uriText, out var hit)) return hit;
        IImage? img = null;
        try
        {
            var uri = new Uri(uriText);
            if (AssetLoader.Exists(uri)) { using var s = AssetLoader.Open(uri); img = new Bitmap(s); }
        }
        catch { img = null; }
        return Cache[uriText] = img;
    }

    internal static string Of(string folder, string file) => "avares://PriceOfBread.App/Assets/" + folder + "/" + file + ".png";
}

/// <summary>財のアイコン（Assets/Goods/＜財の Id＞.png）。画像が無い財は null（空）。
/// パスは財の Id から実行時に組む（番号を数字で書かない＝財の削減で振り直されても名前の通りに引く）。</summary>
public static class GoodIcons
{
    public static string UriOf(int goodId) => Pictures.Of("Goods", goodId.ToString(CultureInfo.InvariantCulture));

    public static IImage? Of(int goodId) => goodId < 0 ? null : Pictures.Load(UriOf(goodId));

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

/// <summary>国旗（Assets/Flags/＜国名＞.png）。同名の画像が無い国（ゲーム中にできる国・反乱勢力など）は _unknown。国名はシミュが付けた名のまま引く。</summary>
public static class FlagIcons
{
    public const string UnknownFile = "_unknown";

    public static string UriOf(string nation) => Pictures.Of("Flags", nation);

    /// <summary>旗を探す名の順＝与えられた名そのまま→政体の語（○○王国・○○共和国など）を外した元の名。内戦の反乱勢力（頭書きの名）は親の旗を借りない。</summary>
    public static IEnumerable<string> Candidates(string nation)
    {
        yield return nation;
        string bare = PriceOfBread.SimCore.SecessionNames.StripPolityTitle(nation);
        if (bare != nation) yield return bare;
    }

    public static IImage? Of(string? nation)
    {
        if (!string.IsNullOrEmpty(nation))
            foreach (var c in Candidates(nation))
                if (Pictures.Load(UriOf(c)) is { } img) return img;
        return Pictures.Load(Pictures.Of("Flags", UnknownFile));
    }
}

/// <summary>階級の絵（Assets/PopClasses/＜PopClass の番号＞.png・番号は階級の番号そのまま＝全身の人物）。無ければ null（空）。</summary>
public static class ClassIcons
{
    public static string UriOf(int classId) => Pictures.Of("PopClasses", classId.ToString(CultureInfo.InvariantCulture));

    public static IImage? Of(int classId) => classId < 0 ? null : Pictures.Load(UriOf(classId));

    /// <summary>行の鍵「class.＜番号＞」「class.＜番号＞.＜党派＞」の番号の部分から。</summary>
    public static IImage? OfKey(string? rowKey)
    {
        if (rowKey is null || !rowKey.StartsWith("class.", StringComparison.Ordinal)) return null;
        var rest = rowKey.AsSpan(6);
        int dot = rest.IndexOf('.');
        return int.TryParse(dot < 0 ? rest : rest[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out int id) ? Of(id) : null;
    }
}
