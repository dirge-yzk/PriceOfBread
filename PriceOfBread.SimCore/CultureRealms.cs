namespace PriceOfBread.SimCore;

/// <summary>
/// 文化ごとの統一国（変名機構の一般化・docs/202607/20260731_変名機構の一般化_部分完了.md・器2026-08-01）。
///
/// ★狙い＝`Formations.cs` の**40ルール手書き＋州名の直書き**を、「文化ラベル → その文化の州の集合」という
/// **データから自動生成される規則**へ置き換える。⚠ 州名を1つも書かないので、州が増えても減っても規則が追随する
/// （アフリカ勢力の追加・内戦分裂の建国と**国名表を共有できる**のが本題）。
///
/// ★★規則は2段だけ：
///   Tier B（大統一）＝その文化の州を**全部**持つ  → `RequiredAll` に落ちる
///   Tier A（統一）  ＝その文化の州の**過半**を持つ → `AnyOfSet` ＋ `AnyOfMin` に落ちる
/// ⚠ **判定コードを1行も足さない**——既存の `Qualifies` が `RequiredAll` と `AnyOfSet/AnyOfMin` をそのまま評価する。
/// ⚠ doc の Tier A「他のどの国より多く持つ」は**実装しない**＝過半を持てば必ず他のどれより多い（過半＞残り全部）ので冗長。
///
/// ⚠⚠**`CultureFormationGate = 0` で `Formations.Enumerate` がこの型に一度も触れない**
/// ＝型初期化子（`Rules` の静的初期化）すら走らない＝world.json の追加走査も起きない＝**厳密恒等**。既定は 1.0（点火）。
/// </summary>
public static class CultureRealms
{
    /// <summary>文化ラベル→統一国の名前（段位2＝統一／段位3＝大統一）と首都。⚠ 国名は**データ**であって特例ではない（思想②）。</summary>
    public sealed record CultureRealm(string Culture, string Name, string GreatName, string Capital);

    internal const int TierA = 2, TierB = 3;   // 段位は係数でなく構造（Formations の既存段位と同じ意味）

    /// <summary>文化ラベルは world.json の州データ（ProvinceSpec.Culture）が真実源。ここに無い文化は統一国を持たない。</summary>
    public static readonly CultureRealm[] Table =
    [
        new("イタリア", "イタリア王国", "ローマ帝国", "ローマ"),
        new("ドイツ", "ドイツ帝国", "神聖ローマ帝国", "ベルリン"),
        new("アラブ", "アラブ王国", "カリフ国", "ダマスカス"),
        // ★2026-08-13の是正：「インド」「中国」は world.json に**存在しない文化キー**で、上のコメントが自ら宣言している
        //   「ここに無い文化は統一国を持たない」に自分で違反していた＝この2行は生成器を1度も通らない死文だった。
        //   ・中国 → 実在する「漢」（16州）へ。名前と首都はそのまま使える。
        //   ・インド → **インドは単一の文化ではない**（マラーター5・カンナダ3・テルグ3・パンジャブ1…の寄せ集め）。
        //     文化キーの機構では原理的に表現できないので、実在する「マラーター」（5州）の統一へ置き換える。
        //     「インド帝国」を出したいなら文化でなく **Formations の州名列挙**（ローマ帝国・神聖ローマ帝国と同じ作法）に属する。
        new("マラーター", "マラーター王国", "マラーター帝国", "プーナ"),
        new("漢", "中華", "中華帝国", "北京"),
    ];

    /// <summary>文化→その文化の州名（world.json の出現順＝決定論）。</summary>
    private static readonly Dictionary<string, List<string>> ByCulture = BuildByCulture();

    /// <summary>(国名, 文化)→開幕時にその国が直接領有していた同文化の州数。「開幕より増やしたか」の基準。</summary>
    private static readonly Dictionary<string, int> InitialHeld = BuildInitialHeld();

    /// <summary>自動生成された変名規則。★`static readonly` の静的初期化にする（遅延キャッシュはバッチの並列実行でデータ競合になる）。</summary>
    public static readonly FormationRule[] Rules = Build();

    private static Dictionary<string, List<string>> BuildByCulture()
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var n in WorldData.Nations())
            foreach (var p in n.Provinces)
            {
                if (string.IsNullOrEmpty(p.Culture)) continue;
                if (!map.TryGetValue(p.Culture, out var list)) map[p.Culture] = list = [];
                list.Add(p.City);   // world.json の出現順を保つ＝決定論
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
            if (!ByCulture.TryGetValue(realm.Culture, out var cities) || cities.Count < 2) continue;   // ★統一とは2つ以上を束ねること（1州文化は対象外＝数学的定義）
            int n = cities.Count;
            // Tier B（大統一）＝全部持つ
            rules.Add(new FormationRule(realm.GreatName, TierB, null, realm.Culture, null, null, [.. cities],
                NewCapital: realm.Capital, CultureOrigin: realm.Culture));
            int half = n / 2 + 1;
            // Tier A（統一）＝過半。⚠ n==2 のとき過半＝2＝全部なので Tier B と同条件＝生成しない
            if (half < n)
                rules.Add(new FormationRule(realm.Name, TierA, null, realm.Culture, null, null, [],
                    AnyOfSet: [.. cities], AnyOfMin: half, NewCapital: realm.Capital, CultureOrigin: realm.Culture));
        }
        return [.. rules];
    }

    /// <summary>その文化の州を**開幕より増やしたか**（＝実際に統一を進めたか）。
    /// ⚠ これが無いと「最初からその文化の州を過半持っている国」が開幕1ヶ月で改名する（清・オスマン等）。</summary>
    public static bool HasGained(Diplomacy d, Power p, string culture)
    {
        if (!ByCulture.TryGetValue(culture, out var cities)) return false;
        int now = 0;
        foreach (var city in cities) if (Formations.OwnsRegion(d, p, city)) now++;
        return now > InitialHeld.GetValueOrDefault((p.BaseName ?? p.Name) + "" + culture);
    }
}
