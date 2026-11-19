namespace PriceOfBread.SimCore;

public sealed record FormationRule(
    string NewName,
    int Tier,
    string[]? BaseNames,
    string? CultureIs,
    string? ReligionIs,
    string[]? ExcludeBaseNames,
    string[] RequiredAll,
    string[]? ReligionAnyOf = null,
    string[]? AnyOfSet = null,
    int AnyOfMin = 0,
    string? NewCapital = null,
    string? CultureOrigin = null
);

public static class Formations
{
    private const int NationalistShareIndex = 4;

    public static readonly FormationRule[] Rules =
    [
        new FormationRule("ローマ", Tier: 4,
            BaseNames: null, CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ローマ", "イスタンブール", "カイロ"],
            ReligionAnyOf: ["カトリック", "プロテスタント", "正教", "イスラム", "シーア派"],
            AnyOfSet: ["ナポリ", "パレルモ", "カリャリ", "ミラノ", "ヴェネツィア", "パドヴァ", "フィレンツェ",
                       "ボローニャ", "ジェノヴァ", "トリノ", "ノヴァーラ", "マルセイユ", "リヨン", "ボルドー",
                       "マドリード", "バルセロナ", "セビーリャ", "バレンシア", "リスボン", "ポルト",
                       "アテネ", "サロニカ", "スミルナ", "アンカラ", "エルズルム", "ダマスカス", "エルサレム",
                       "ベオグラード", "ソフィア", "ブカレスト"], AnyOfMin: 8,
            NewCapital: "ローマ"),

        new FormationRule("神聖ローマ", Tier: 3,
            BaseNames: ["オーストリア", "プロイセン"], CultureIs: "ドイツ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ウィーン", "プラハ", "ベルリン", "ミュンヘン", "ケルン",
                          "ドレスデン", "ハノーファー", "シュトゥットガルト", "ミラノ"]),

        new FormationRule("ビザンツ", Tier: 3,
            BaseNames: null, CultureIs: null, ReligionIs: "正教", ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["イスタンブール", "アンカラ", "スミルナ", "エルズルム"],
            AnyOfSet: ["アテネ", "サロニカ", "ソフィア", "ベオグラード", "ブカレスト"], AnyOfMin: 2,
            NewCapital: "イスタンブール"),

        new FormationRule("ドイツ", Tier: 2,
            BaseNames: ["プロイセン", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク", "ライン諸邦"],
            CultureIs: "ドイツ", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン",
                          "ドレスデン", "ハノーファー", "ミュンヘン", "シュトゥットガルト"]),

        new FormationRule("イタリア", Tier: 2,
            BaseNames: ["サルデーニャ＝ピエモンテ", "両シチリア", "教皇領", "トスカーナ", "ヴェネツィア", "ジェノヴァ"],
            CultureIs: "イタリア", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["トリノ", "ジェノヴァ", "ミラノ", "ヴェネツィア", "フィレンツェ", "ボローニャ", "ローマ", "ナポリ"],
            NewCapital: "ローマ"),

        new FormationRule("北ドイツ連邦", Tier: 1,
            BaseNames: ["プロイセン"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ベルリン", "シュテッティン", "ケーニヒスベルク", "ブレスラウ", "ケルン", "ドレスデン", "ハノーファー"]),


        new FormationRule("フランス帝国", Tier: 3,
            BaseNames: ["フランス"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["パリ", "ブリュッセル", "アムステルダム", "ミラノ"],
            AnyOfSet: ["ケルン", "ジェノヴァ", "トリノ", "バルセロナ", "マドリード", "ヴェネツィア"], AnyOfMin: 2,
            NewCapital: "パリ"),

        new FormationRule("大英帝国", Tier: 3,
            BaseNames: ["イギリス"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ロンドン", "ダブリン", "グラスゴー"],
            AnyOfSet: ["ニューヨーク", "フィラデルフィア", "チャールストン", "デリー", "ラクナウ",
                       "カイロ", "広州", "上海", "ラングーン"], AnyOfMin: 4),

        new FormationRule("ポーランド＝リトアニア", Tier: 2,
            BaseNames: ["ポーランド"], CultureIs: "ポーランド", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ワルシャワ", "ヴィルナ", "レンベルク"],
            AnyOfSet: ["キエフ", "ハリコフ", "リガ", "ブレスラウ", "ケーニヒスベルク"], AnyOfMin: 2,
            NewCapital: "ワルシャワ"),

        new FormationRule("スカンジナビア", Tier: 2,
            BaseNames: ["スウェーデン", "デンマーク"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ストックホルム", "コペンハーゲン", "クリスチャニア"],
            AnyOfSet: ["ウメオ", "ヘルシングフォシュ", "トロムソ"], AnyOfMin: 1,
            NewCapital: "ストックホルム"),

        new FormationRule("イベリア", Tier: 2,
            BaseNames: ["スペイン", "ポルトガル"], CultureIs: "スペイン", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["マドリード", "バルセロナ", "リスボン"],
            AnyOfSet: ["セビーリャ", "バレンシア", "ポルト"], AnyOfMin: 2,
            NewCapital: "マドリード"),

        new FormationRule("大ネーデルラント", Tier: 2,
            BaseNames: ["ネーデルラント"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["アムステルダム", "ブリュッセル", "リール"],
            AnyOfSet: ["ロッテルダム", "ハーグ", "ユトレヒト", "ケルン"], AnyOfMin: 2,
            NewCapital: "アムステルダム"),

        new FormationRule("ユーゴスラビア", Tier: 2,
            BaseNames: null, CultureIs: "セルビア", ReligionIs: null, ExcludeBaseNames: ["オスマン", "オーストリア"],
            RequiredAll: ["ベオグラード", "ザグレブ", "ソフィア"],
            AnyOfSet: ["ザラ", "サロニカ"], AnyOfMin: 1,
            NewCapital: "ベオグラード"),

        new FormationRule("大ブルガリア", Tier: 2,
            BaseNames: null, CultureIs: "ブルガリア", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["ソフィア", "サロニカ", "ベオグラード"],
            AnyOfSet: ["イスタンブール", "ブカレスト", "アテネ"], AnyOfMin: 1,
            NewCapital: "ソフィア"),

        new FormationRule("ギリシャ", Tier: 2,
            BaseNames: null, CultureIs: "ギリシャ", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["アテネ", "サロニカ", "スミルナ"],
            AnyOfSet: ["イスタンブール", "アンカラ"], AnyOfMin: 1,
            NewCapital: "アテネ"),

        new FormationRule("ハンガリー", Tier: 2,
            BaseNames: null, CultureIs: "ハンガリー", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["ブダペスト", "プレスブルク", "クラウゼンブルク"],
            AnyOfSet: ["ザグレブ", "ウィーン", "グラーツ"], AnyOfMin: 1,
            NewCapital: "ブダペスト"),

        new FormationRule("大ルーマニア", Tier: 2,
            BaseNames: null, CultureIs: "ルーマニア", ReligionIs: null, ExcludeBaseNames: ["オスマン", "オーストリア"],
            RequiredAll: ["ブカレスト", "クラウゼンブルク"],
            AnyOfSet: ["ソフィア", "ベオグラード", "サロニカ"], AnyOfMin: 1,
            NewCapital: "ブカレスト"),

        new FormationRule("チェコスロバキア", Tier: 1,
            BaseNames: null, CultureIs: "チェコ", ReligionIs: null, ExcludeBaseNames: ["オーストリア"],
            RequiredAll: ["プラハ", "ブリュン", "プレスブルク"],
            NewCapital: "プラハ"),

        new FormationRule("カリフ国", Tier: 3,
            BaseNames: null, CultureIs: "アラブ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ダマスカス", "バグダード", "カイロ"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["エルサレム", "バスラ", "ディルイーヤ", "マスカット", "サヌア", "アデン", "フェズ", "マラケシュ"], AnyOfMin: 3,
            NewCapital: "バグダード"),

        new FormationRule("トゥーラーン", Tier: 3,
            BaseNames: ["オスマン", "コーカンド", "ヒヴァ", "ブハラ"], CultureIs: "トルコ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["イスタンブール", "バクー", "タシュケント"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["カザン", "カシュガル", "アンカラ"], AnyOfMin: 2,
            NewCapital: "イスタンブール"),

        new FormationRule("マシュリク", Tier: 2,
            BaseNames: null, CultureIs: "アラブ", ReligionIs: null, ExcludeBaseNames: ["オスマン"],
            RequiredAll: ["ダマスカス", "エルサレム", "バグダード"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["バスラ", "カイロ"], AnyOfMin: 1,
            NewCapital: "ダマスカス"),

        new FormationRule("ペルシア帝国", Tier: 2,
            BaseNames: ["ペルシャ"], CultureIs: null, ReligionIs: "シーア派", ExcludeBaseNames: null,
            RequiredAll: ["テヘラン", "イスファハン", "タブリーズ"],
            AnyOfSet: ["ヘラート", "バクー", "バグダード", "カブール"], AnyOfMin: 2,
            NewCapital: "テヘラン"),

        new FormationRule("トルキスタン", Tier: 2,
            BaseNames: ["ブハラ", "ヒヴァ", "コーカンド"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ブハラ", "タシュケント"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["ヒヴァ", "カシュガル"], AnyOfMin: 1,
            NewCapital: "ブハラ"),

        new FormationRule("ドゥッラーニー帝国", Tier: 2,
            BaseNames: ["アフガニスタン"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["カブール", "カンダハール", "ヘラート"],
            AnyOfSet: ["ラホール", "メシェッド", "デリー"], AnyOfMin: 1,
            NewCapital: "カブール"),

        new FormationRule("サウード", Tier: 2,
            BaseNames: ["アラビア"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ディルイーヤ", "マスカット", "サヌア"],
            ReligionAnyOf: ["イスラム"],
            NewCapital: "ディルイーヤ"),

        new FormationRule("ルーム", Tier: 2,
            BaseNames: null, CultureIs: "トルコ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["アンカラ", "イスタンブール", "スミルナ", "エルズルム"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["バクー", "カザン", "カシュガル", "タブリーズ"], AnyOfMin: 1,
            NewCapital: "アンカラ"),

        new FormationRule("ムワッヒド", Tier: 2,
            BaseNames: ["モロッコ"], CultureIs: null, ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["フェズ", "マラケシュ", "セビーリャ"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["マドリード", "バレンシア", "バルセロナ", "リスボン"], AnyOfMin: 1,
            NewCapital: "マラケシュ"),


        new FormationRule("大モンゴル", Tier: 3,
            BaseNames: null, CultureIs: "蒙古", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ウルガ", "フフホト", "北京"],
            AnyOfSet: ["カシュガル", "ラサ", "カザン", "タシュケント"], AnyOfMin: 2,
            NewCapital: "北京"),

        new FormationRule("バーラト", Tier: 3,
            BaseNames: ["マラータ同盟", "マイソール", "ハイデラバード", "ネパール"], CultureIs: "マラーター", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["デリー", "プーナ", "ハイデラバード", "シュリーランガパトナ"],
            ReligionAnyOf: ["ヒンドゥー教"],
            AnyOfSet: ["ナーグプル", "グワーリヤル", "インドール", "バローダ", "ラクナウ", "ラホール", "カトマンズ"], AnyOfMin: 3,
            NewCapital: "デリー"),

        new FormationRule("ムガル帝国", Tier: 3,
            BaseNames: ["デリー", "アワド"], CultureIs: "ヒンドゥスタン", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["デリー", "ラクナウ", "ラホール"],
            ReligionAnyOf: ["イスラム"],
            AnyOfSet: ["カブール", "カンダハール", "ペシャーワル", "プーナ", "ハイデラバード"], AnyOfMin: 2,
            NewCapital: "デリー"),

        new FormationRule("インドシナ", Tier: 3,
            BaseNames: ["西山朝", "阮氏"], CultureIs: "ベトナム", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ハノイ", "フエ", "サイゴン", "プノンペン", "ヴィエンチャン"],
            AnyOfSet: ["バンコク", "ラングーン", "アマラプラ"], AnyOfMin: 1,
            NewCapital: "ハノイ"),

        new FormationRule("大日本", Tier: 3,
            BaseNames: ["日本"], CultureIs: "日本", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["江戸", "漢城", "平壌"],
            AnyOfSet: ["上海", "台北", "奉天", "北京", "ハルビン", "広州"], AnyOfMin: 2,
            NewCapital: "江戸"),

        new FormationRule("中華", Tier: 2,
            BaseNames: null, CultureIs: "漢", ReligionIs: null, ExcludeBaseNames: ["清"],
            RequiredAll: ["北京", "上海", "広州", "漢口", "西安"],
            AnyOfSet: ["済南", "成都", "開封", "長沙", "杭州", "福州", "桂林", "昆明", "台北"], AnyOfMin: 4,
            NewCapital: "北京"),

        new FormationRule("大チベット", Tier: 2,
            BaseNames: null, CultureIs: null, ReligionIs: "チベット仏教", ExcludeBaseNames: null,
            RequiredAll: ["ラサ", "フフホト", "ウルガ"],
            AnyOfSet: ["カシュガル", "カトマンズ", "北京"], AnyOfMin: 1,
            NewCapital: "ラサ"),

        new FormationRule("マラーター帝国", Tier: 2,
            BaseNames: ["マラータ同盟"], CultureIs: "マラーター", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["プーナ", "ナーグプル", "グワーリヤル", "インドール", "バローダ"],
            AnyOfSet: ["ハイデラバード", "シュリーランガパトナ", "ラクナウ"], AnyOfMin: 1,
            NewCapital: "プーナ"),

        new FormationRule("越南", Tier: 2,
            BaseNames: ["西山朝", "阮氏"], CultureIs: "ベトナム", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ハノイ", "フエ", "サイゴン"],
            NewCapital: "フエ"),

        new FormationRule("シャム帝国", Tier: 2,
            BaseNames: ["シャム"], CultureIs: "タイ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["バンコク"],
            AnyOfSet: ["プノンペン", "ヴィエンチャン", "ラングーン", "アマラプラ"], AnyOfMin: 3,
            NewCapital: "バンコク"),

        new FormationRule("ビルマ帝国", Tier: 2,
            BaseNames: ["ビルマ"], CultureIs: "ビルマ", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["ラングーン", "アマラプラ"],
            AnyOfSet: ["バンコク", "カトマンズ", "ヴィエンチャン"], AnyOfMin: 1,
            NewCapital: "アマラプラ"),

        new FormationRule("統一朝鮮", Tier: 2,
            BaseNames: ["朝鮮"], CultureIs: "朝鮮", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["漢城", "平壌", "奉天", "ハルビン"],
            NewCapital: "漢城"),

        new FormationRule("満洲国", Tier: 2,
            BaseNames: null, CultureIs: "満洲", ReligionIs: null, ExcludeBaseNames: null,
            RequiredAll: ["奉天", "ハルビン"],
            AnyOfSet: ["北京", "フフホト", "ウルガ", "漢城"], AnyOfMin: 1,
            NewCapital: "奉天"),
    ];

    public static void Check(Diplomacy d, Coefficients c)
    {
        foreach (var power in d.Powers)
        {
            if (power.Nation is not { } w) continue;
            FormationRule? best = null;
            foreach (var rule in Enumerate())
            {
                if (rule.Tier <= power.FormationTier) continue;
                if (best is not null && rule.Tier <= best.Tier) continue;
                if (!Qualifies(d, power, rule, c)) continue;
                best = rule;
            }
            if (best is null) continue;
            if (!d.RenameNation(power, best.NewName)) continue;
            power.FormationTier = best.Tier;
            if (best.NewCapital is { } capName)
            {
                var newCap = w.Regions.Find(r => r.Name == capName);
                if (newCap is not null && newCap.Id != 0)
                {
                    w.SetCapital(newCap);
                    d.Log.Add($"{w.Year} {power.Name}が{capName}へ遷都");
                }
            }
        }
    }

    private static bool Qualifies(Diplomacy d, Power power, FormationRule rule, Coefficients c)
    {
        if (!IdentityEligible(power, rule)) return false;
        if (rule.CultureOrigin is { } cul && !CultureRealms.HasGained(d, power, cul)) return false;
        foreach (var name in rule.RequiredAll) if (!Owns(d, power, name)) return false;
        if (rule.AnyOfSet is not null)
        {
            int have = 0;
            foreach (var name in rule.AnyOfSet) if (Owns(d, power, name)) have++;
            if (have < rule.AnyOfMin) return false;
        }
        return true;
    }

    public static bool IdentityEligible(Power power, FormationRule rule)
    {
        if (rule.ExcludeBaseNames is not null && System.Array.IndexOf(rule.ExcludeBaseNames, power.BaseName) >= 0) return false;
        if (rule.ReligionAnyOf is not null && System.Array.IndexOf(rule.ReligionAnyOf, power.Religion) < 0) return false;
        bool eligible = rule.BaseNames is null && rule.CultureIs is null && rule.ReligionIs is null;
        if (!eligible && rule.BaseNames is not null && System.Array.IndexOf(rule.BaseNames, power.BaseName) >= 0) eligible = true;
        if (!eligible && rule.CultureIs is not null && power.Culture == rule.CultureIs) eligible = true;
        if (!eligible && rule.ReligionIs is not null && power.Religion == rule.ReligionIs) eligible = true;
        return eligible;
    }

    public static IEnumerable<FormationRule> Enumerate()
    {
        foreach (var r in Rules) yield return r;
        foreach (var r in CultureRealms.Rules) yield return r;
    }

    public static bool OwnsRegion(Diplomacy d, Power power, string regionName) => Owns(d, power, regionName);

    public static readonly string[] UnificationV1 = ["北ドイツ連邦", "ドイツ", "イタリア"];

    public static FormationRule? UnificationCandidate(Diplomacy d, Power p, World w, Coefficients c)
    {
        if (p.Nation is null) return null;
        FormationRule? best = null;
        foreach (var r in Enumerate())
        {
            if (c.UnificationV1Only > 0 && System.Array.IndexOf(UnificationV1, r.NewName) < 0) continue;
            if (r.Tier <= p.FormationTier) continue;
            if (!IdentityEligible(p, r)) continue;
            if (Qualifies(d, p, r, c)) continue;
            if (best is null || r.Tier < best.Tier) best = r;
        }
        if (best is null) return null;
        double share = c.FormationCultureNationalism > 0 && best.CultureIs is { } cul
            ? Ideologies.NationalOfCulture(w, cul)[NationalistShareIndex]   // (1230) 旗 C：規則の文化の州の POP だけ（少数の民族主義を統一の燃料に混ぜない）
            : Ideologies.National(w)[NationalistShareIndex];
        return share >= c.UnificationNatioShare.Value ? best : null;
    }

    public static System.Collections.Generic.List<Power> UnificationTargets(Diplomacy d, Power p, FormationRule r)
    {
        var holders = new System.Collections.Generic.List<Power>();
        void AddHolder(string name)
        {
            foreach (var q in d.Powers)
            {
                if (q == p || q.Nation is null) continue;
                if (HasRegion(q, name)) { if (!holders.Contains(q)) holders.Add(q); break; }
            }
        }
        foreach (var name in r.RequiredAll)
            if (!Owns(d, p, name)) AddHolder(name);
        if (r.AnyOfSet is { Length: > 0 } set && r.AnyOfMin > 0)
        {
            int have = 0;
            foreach (var name in set) if (Owns(d, p, name)) have++;
            if (have < r.AnyOfMin)
                foreach (var name in set) if (!Owns(d, p, name)) AddHolder(name);
        }
        return holders;
    }

    /// <summary>(1195) 統一の宣戦を妨げる物。AI（Diplomacy.TryUnificationWars）とプレイヤー（World.CanUnificationWar）が同じ判定を呼ぶ。</summary>
    public enum UnificationBlock { None, NotTarget, AtWar, Allied, Weary }

    /// <summary>(1195) この標的へ統一の宣戦ができるか（targets＝UnificationTargets の結果）。理由の文字列はプレイヤーの側だけが持つ。
    /// (1230) 旗 B：攻め手の厭戦が WarWearyThreshold を超えたら Weary（他の入口と同じ述語・攻め手だけを読む）。</summary>
    public static UnificationBlock UnificationWarBlock(Diplomacy d, Power p, System.Collections.Generic.List<Power> targets, Power t, Coefficients c)
    {
        var b = !targets.Contains(t) ? UnificationBlock.NotTarget
              : d.IsAtWar(p, t) ? UnificationBlock.AtWar
              : d.HasAlliance(p, t) ? UnificationBlock.Allied
              : c.UnificationWarWeary > 0 && p.WarExhaustion > c.WarWearyThreshold ? UnificationBlock.Weary
              : UnificationBlock.None;
        if (Instruments.On) Instruments.UnifWarEval(d, c, p, t, targets, (int)b);   // 計器（読むだけ）
        return b;
    }

    private static bool Owns(Diplomacy d, Power power, string regionName)
    {
        if (HasRegion(power, regionName)) return true;
        foreach (var dep in d.DependentsOf(power)) if (HasRegion(dep, regionName)) return true;
        return false;
    }

    private static bool HasRegion(Power p, string name)
    {
        if (p.Nation is not { } w) return false;
        foreach (var r in w.Regions) if (r.Name == name) return true;
        return false;
    }
}
