namespace PriceOfBread.SimCore;

public static class CultureRealms
{
    public sealed record CultureRealm(string Culture, string Name, string GreatName, string Capital);

    internal const int TierA = 2, TierB = 3;

    public static readonly CultureRealm[] Table =
    [
        new("イタリア", "イタリア王国", "ローマ帝国", "ローマ"),
        new("ドイツ", "ドイツ帝国", "神聖ローマ帝国", "ベルリン"),
        new("アラブ", "アラブ王国", "カリフ国", "ダマスカス"),
        new("マラーター", "マラーター王国", "マラーター帝国", "プーナ"),
        new("漢", "中華", "中華帝国", "北京"),
    ];

    private static readonly Dictionary<string, List<string>> ByCulture = BuildByCulture();

    private static readonly Dictionary<string, int> InitialHeld = BuildInitialHeld();

    public static readonly FormationRule[] Rules = Build();

    private static Dictionary<string, List<string>> BuildByCulture()
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var n in WorldData.Nations())
            foreach (var p in n.Provinces)
            {
                if (string.IsNullOrEmpty(p.Culture)) continue;
                if (!map.TryGetValue(p.Culture, out var list)) map[p.Culture] = list = [];
                list.Add(p.City);
            }
        return map;
    }

    private static Dictionary<string, int> BuildInitialHeld()
    {
        var map = new Dictionary<string, int>();
        foreach (var n in WorldData.Nations())
            foreach (var p in n.Provinces)
            {
                if (string.IsNullOrEmpty(p.Culture)) continue;
                string k = n.Name + "" + p.Culture;
                map[k] = map.GetValueOrDefault(k) + 1;
            }
        return map;
    }

    private static FormationRule[] Build()
    {
        var rules = new List<FormationRule>();
        foreach (var realm in Table)
        {
            if (!ByCulture.TryGetValue(realm.Culture, out var cities) || cities.Count < 2) continue;
            int n = cities.Count;
            rules.Add(new FormationRule(realm.GreatName, TierB, null, realm.Culture, null, null, [.. cities],
                NewCapital: realm.Capital, CultureOrigin: realm.Culture));
            int half = n / 2 + 1;
            if (half < n)
                rules.Add(new FormationRule(realm.Name, TierA, null, realm.Culture, null, null, [],
                    AnyOfSet: [.. cities], AnyOfMin: half, NewCapital: realm.Capital, CultureOrigin: realm.Culture));
        }
        return [.. rules];
    }

    public static bool HasGained(Diplomacy d, Power p, string culture)
    {
        if (!ByCulture.TryGetValue(culture, out var cities)) return false;
        int now = 0;
        foreach (var city in cities) if (Formations.OwnsRegion(d, p, city)) now++;
        return now > InitialHeld.GetValueOrDefault((p.BaseName ?? p.Name) + "" + culture);
    }
}
