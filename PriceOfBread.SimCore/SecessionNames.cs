namespace PriceOfBread.SimCore;

public static class SecessionNames
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["ダブリン"] = "アイルランド", ["グラスゴー"] = "スコットランド",
        ["ブリュッセル"] = "ベルギー", ["ブダペスト"] = "ハンガリー", ["プレスブルク"] = "スロバキア",
        ["プラハ"] = "ボヘミア", ["ブリュン"] = "モラヴィア", ["ザグレブ"] = "クロアチア",
        ["レンベルク"] = "ガリツィア", ["クラウゼンブルク"] = "トランシルヴァニア", ["ミラノ"] = "ロンバルディア",
        ["ベオグラード"] = "セルビア", ["ソフィア"] = "ブルガリア", ["アテネ"] = "ギリシャ",
        ["ブカレスト"] = "ルーマニア", ["サロニカ"] = "マケドニア", ["スミルナ"] = "イオニア",
        ["カイロ"] = "エジプト", ["ダマスカス"] = "シリア", ["バグダード"] = "イラク", ["バスラ"] = "バスラ",
        ["キエフ"] = "ウクライナ", ["ハリコフ"] = "ウクライナ", ["リガ"] = "ラトビア",
        ["バクー"] = "アゼルバイジャン", ["カザン"] = "タタールスタン", ["オムスク"] = "シベリア",
        ["ヴィルナ"] = "リトアニア",
        ["チャールストン"] = "アメリカ連合国",
        ["バルセロナ"] = "カタルーニャ", ["セビーリャ"] = "アンダルシア",
        ["クリスチャニア"] = "ノルウェー", ["トロムソ"] = "ノルウェー", ["ヘルシングフォシュ"] = "フィンランド",
        ["台北"] = "台湾", ["ラサ"] = "チベット", ["カシュガル"] = "東トルキスタン",
        ["ウルガ"] = "モンゴル", ["フフホト"] = "内モンゴル", ["ハルビン"] = "満洲国", ["奉天"] = "満洲国",
        ["タブリーズ"] = "タブリーズ", ["カンダハール"] = "カンダハール", ["ヘラート"] = "ヘラート",
        ["ブレスラウ"] = "シレジア", ["ザラ"] = "ダルマチア", ["パレルモ"] = "シチリア", ["カリャリ"] = "サルデーニャ",
    };

    public static string Resolve(string city, Polity polity, IReadOnlyList<Power> existing)
    {
        string fallback = city + (polity is Polity.Republic or Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship or Polity.Anarchy ? "共和国" : "王国");
        string name = Map.GetValueOrDefault(city, fallback);
        return Taken(name, existing) ? Unique(fallback, existing) : name;
    }

    public static string PolityPrefix(Polity p) => p switch
    {
        Polity.Absolutist => "王政",
        Polity.Despotic => "帝政",
        Polity.ConstitutionalMonarchy => "立憲王政",
        Polity.Shogunate => "幕府",
        Polity.Republic => "共和制",
        Polity.BourgeoisDictatorship => "統領制",
        Polity.ProletarianDictatorship => "人民",
        Polity.MilitaryDictatorship => "軍政",
        _ => "",
    };

    public static string CivilWarName(string parentName, Polity rebelPolity, Polity parentPolity, string? movement, IReadOnlyList<Power> existing)
    {
        string head = rebelPolity == parentPolity && movement is { Length: > 0 } ? movement + "・" : PolityPrefix(rebelPolity);
        return Unique(head + BareName(parentName), existing);
    }

    public static string DisplayName(Power p, Polity? shown, IReadOnlyList<War> wars)
        => GovernmentPrefix(p, wars) is { } head ? head + BareName(p.Name)
         : PrefixLength(p.Name) > 0 ? p.Name : TitledName(p.Name, shown);

    public static string WarName(Power p, IReadOnlyList<War> wars)
        => GovernmentPrefix(p, wars) is { } head ? head + BareName(p.Name) : p.Name;

    private static string? GovernmentPrefix(Power p, IReadOnlyList<War> wars)
    {
        if (p.Nation is not { } w) return null;
        foreach (var war in wars)
            if (war.CivilWarParent == p && !war.CivilWarSecession && war.CivilWarPolity != w.Polity && PolityPrefix(w.Polity) is { Length: > 0 } head)
                return head;
        return null;
    }

    private static readonly string[] PolityTitleSuffixes = { "人民共和国", "立憲王国", "統領政府", "軍事政権", "共和国", "王国", "帝国", "幕府" };
    public static string TitledName(string baseName, Polity? p) => StripPolityTitle(baseName) + (p switch
    {
        Polity.Absolutist => "王国",
        Polity.Despotic => "帝国",
        Polity.ConstitutionalMonarchy => "立憲王国",
        Polity.Shogunate => "幕府",
        Polity.Republic => "共和国",
        Polity.BourgeoisDictatorship => "統領政府",
        Polity.ProletarianDictatorship => "人民共和国",
        Polity.MilitaryDictatorship => "軍事政権",
        Polity.Anarchy => "(内乱)",
        _ => "",
    });

    public static string StripPolityTitle(string name)
    {
        foreach (var sfx in PolityTitleSuffixes)
            if (name.Length > sfx.Length && name.EndsWith(sfx, System.StringComparison.Ordinal)) return name[..^sfx.Length];
        return name;
    }

    private static readonly Polity[] PrefixOrder = [Polity.ConstitutionalMonarchy, Polity.Absolutist, Polity.Despotic, Polity.Shogunate,
                                                    Polity.Republic, Polity.BourgeoisDictatorship, Polity.ProletarianDictatorship, Polity.MilitaryDictatorship];
    private static int PrefixLength(string name)
    {
        foreach (var pol in PrefixOrder)
        {
            string head = PolityPrefix(pol);
            if (name.Length > head.Length && name.StartsWith(head, System.StringComparison.Ordinal)) return head.Length;
        }
        return 0;
    }

    private static string BareName(string name) => StripPolityTitle(name[PrefixLength(name)..]);

    private static bool Taken(string name, IReadOnlyList<Power> existing)
    {
        foreach (var p in existing) if (p.Name == name || p.BaseName == name) return true;
        return false;
    }

    private static string Unique(string name, IReadOnlyList<Power> existing)
    {
        if (!Taken(name, existing)) return name;
        for (int k = 2; ; k++) { string n = $"第{k}{name}"; if (!Taken(n, existing)) return n; }
    }
}
