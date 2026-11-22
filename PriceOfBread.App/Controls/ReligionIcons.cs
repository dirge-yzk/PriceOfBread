using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>州と POP の宗教名から、埋め込みの宗教アイコンを引く。</summary>
public static class ReligionIcons
{
    public static string? UriOf(string? religion)
    {
        string? file = religion switch
        {
            "ヒンドゥー" => "ヒンドゥー教",
            "イスラム" or "エチオピア正教" or "カトリック" or "ザイド派" or "シーア派" or "シク教"
                or "チベット仏教" or "ヒンドゥー教" or "プロテスタント" or "儒" or "儒仏"
                or "上座部仏教" or "神仏" or "正教" or "土着信仰" => religion,
            _ => null,
        };
        return file is null ? null : Pictures.Of("Religions", file);
    }

    public static IImage? Of(string? religion) => UriOf(religion) is { } uri ? Pictures.Load(uri) : null;
}
