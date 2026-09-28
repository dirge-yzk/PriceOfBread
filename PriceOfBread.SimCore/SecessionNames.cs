namespace PriceOfBread.SimCore;

/// <summary>
/// 国名の表の1か所（2026-09-28 処理の一元化 段2 束3）。
/// ①分離独立した州の歴史的国名（州の代表都市名→建国名・2026-07-12）。対応が無い州は既定則（州名＋共和国/王国）。
///   州単位の静的表＝同文化複数州の同時独立の名前衝突を静的に回避（同名は一意性ガードが既定則へ退避）。州名は world.json の city と一字一句一致。
/// ②内戦の国名（作者承認 2026-09-28）＝政体をめぐる内戦の間は両陣営とも「政体の頭書き＋国名」（反乱側は目指す政体・政府側は今の政体）。
///   同じ政体の要求戦は反乱側に運動の表示名を頭に。分離独立戦争は①のまま。⚠国ごとの君主の称号（皇帝・国王・スルタン）のデータは入れない（作者：無数の例外が出る）。
/// ③画面の国号（政体で変わる国号＝イギリス王国／イギリス共和国）。旧＝App の GameViewModel.PolityTitleJa に別の表＝ここへ寄せた（表は SimCore に1か所）。
/// </summary>
public static class SecessionNames
{
    private static readonly Dictionary<string, string> Map = new()
    {
        // イギリス
        ["ダブリン"] = "アイルランド", ["グラスゴー"] = "スコットランド",
        // オーストリア（1783年開始＝ブリュッセルは墺領ネーデルラント。蘭が征服していれば「オランダからの独立」も同表で自動成立）
        ["ブリュッセル"] = "ベルギー", ["ブダペスト"] = "ハンガリー", ["プレスブルク"] = "スロバキア",
        ["プラハ"] = "ボヘミア", ["ブリュン"] = "モラヴィア", ["ザグレブ"] = "クロアチア",
        ["レンベルク"] = "ガリツィア", ["クラウゼンブルク"] = "トランシルヴァニア", ["ミラノ"] = "ロンバルディア",
        // オスマン（バルカン・アラブ）
        ["ベオグラード"] = "セルビア", ["ソフィア"] = "ブルガリア", ["アテネ"] = "ギリシャ",
        ["ブカレスト"] = "ルーマニア", ["サロニカ"] = "マケドニア", ["スミルナ"] = "イオニア",
        ["カイロ"] = "エジプト", ["ダマスカス"] = "シリア", ["バグダード"] = "イラク", ["バスラ"] = "バスラ",
        // ロシア
        ["キエフ"] = "ウクライナ", ["ハリコフ"] = "ウクライナ", ["リガ"] = "ラトビア",
        ["バクー"] = "アゼルバイジャン", ["カザン"] = "タタールスタン", ["オムスク"] = "シベリア",
        // ポーランド（ポーランド・リトアニア共和国の州）
        ["ヴィルナ"] = "リトアニア",
        // アメリカ（ボストンはStage B（§13州再配分・2026-08-09）でNYへ吸収＝州が消えたので掃除）
        ["チャールストン"] = "アメリカ連合国",
        // スペイン
        ["バルセロナ"] = "カタルーニャ", ["セビーリャ"] = "アンダルシア",
        // 北欧（デンマーク領ノルウェー・スウェーデン領フィンランド）
        ["クリスチャニア"] = "ノルウェー", ["トロムソ"] = "ノルウェー", ["ヘルシングフォシュ"] = "フィンランド",
        // 清
        ["台北"] = "台湾", ["ラサ"] = "チベット", ["カシュガル"] = "東トルキスタン",
        ["ウルガ"] = "モンゴル", ["フフホト"] = "内モンゴル", ["ハルビン"] = "満洲国", ["奉天"] = "満洲国",
        // ペルシャ・アフガニスタン
        ["タブリーズ"] = "タブリーズ", ["カンダハール"] = "カンダハール", ["ヘラート"] = "ヘラート",
        // プロイセン・ヴェネツィア・イタリア系
        ["ブレスラウ"] = "シレジア", ["ザラ"] = "ダルマチア", ["パレルモ"] = "シチリア", ["カリャリ"] = "サルデーニャ",
    };

    /// <summary>建国名の解決：対応表→既定則（政体で共和国/王国）。既存Powerの表示名/構築時名と重複したら既定則へ退避（名前の一意性）。
    /// ★2026-09-28：既定則も埋まっていれば序数を頭に（<see cref="Unique"/>＝旧は既定則を重複のまま返した＝内部の鍵 BaseName が一意でなくなりえた）。</summary>
    public static string Resolve(string city, Polity polity, IReadOnlyList<Power> existing)
    {
        string fallback = city + (polity is Polity.Republic or Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship or Polity.Anarchy ? "共和国" : "王国");
        string name = Map.GetValueOrDefault(city, fallback);
        return Taken(name, existing) ? Unique(fallback, existing) : name;
    }

    /// <summary>政体の頭書き（内戦の国名・作者承認 2026-09-28）。無政府は頭書きを持たない（目標の政体にはならない＝Ideologies.Polities の外）。</summary>
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

    /// <summary>内戦で立った反乱国の名（Politics の蜂起→MultiNationSim.SpawnNation）＝政体をめぐる内戦は目指す政体の頭書き＋親の国名（共和制フランス）／
    /// 同じ政体の要求戦は運動の表示名を頭に（<paramref name="movement"/>・無ければ頭書き）。親の国名は国号を剥がす（ハワイ王国→共和制ハワイ）。
    /// 名は内部の鍵（Power.BaseName）でもある＝埋まっていれば序数を頭に（分断が固まった先の共和制フランスが残っていれば第2共和制フランス）。</summary>
    public static string CivilWarName(string parentName, Polity rebelPolity, Polity parentPolity, string? movement, IReadOnlyList<Power> existing)
    {
        string head = rebelPolity == parentPolity && movement is { Length: > 0 } ? movement + "・" : PolityPrefix(rebelPolity);
        return Unique(head + BareName(parentName), existing);
    }

    /// <summary>画面の国号（外交タブ）＝政体をめぐる内戦の政府側は戦争の間だけ「今の政体の頭書き＋国名」（反乱側は名そのものが頭書きつき）／
    /// 内戦で頭書きを得た名（分断が固まった反乱国）はそのまま／他は政体で変わる国号（<see cref="TitledName"/>）。終戦で再併合されれば親は通常の国号へ戻る。</summary>
    public static string DisplayName(Power p, Polity? shown, IReadOnlyList<War> wars)
        => GovernmentPrefix(p, wars) is { } head ? head + BareName(p.Name)
         : PrefixLength(p.Name) > 0 ? p.Name : TitledName(p.Name, shown);

    /// <summary>戦争の相手の名（戦争タブ）＝国号を足さない名。政体をめぐる内戦の政府側だけ戦争の間「今の政体の頭書き＋国名」（<see cref="DisplayName"/> と同じ規則）。</summary>
    public static string WarName(Power p, IReadOnlyList<War> wars)
        => GovernmentPrefix(p, wars) is { } head ? head + BareName(p.Name) : p.Name;

    // 政府側の頭書き＝この国を親とする政体をめぐる内戦（分離独立戦争でも同じ政体の要求戦でもない）が進行中なら今の政体の頭書き。
    private static string? GovernmentPrefix(Power p, IReadOnlyList<War> wars)
    {
        if (p.Nation is not { } w) return null;
        foreach (var war in wars)
            if (war.CivilWarParent == p && !war.CivilWarSecession && war.CivilWarPolity != w.Polity && PolityPrefix(w.Polity) is { Length: > 0 } head)
                return head;
        return null;
    }

    // 政体で変わる国号（イギリス王国／イギリス共和国／…）。9政体すべてに固有の名。
    // ★2026-08-23：**元の名前が既に国号を含む場合は剥がしてから付ける**（旧＝「ハワイ王国」＋「王国」＝「ハワイ王国王国」。
    //   コンゴ/ダホメ/アシャンティ/マラーターも同型）。剥がすことで共和政になれば「ハワイ共和国」と政体に追随する。
    //   長い国号から先に照合する（人民共和国→共和国 の順でないと「人民」が残る）。
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

    // 頭書きで始まる名（内戦で立った国）＝国号を足さない（「共和制フランス共和国」にしない）。長い頭書きから照合（立憲王政→王政）。
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

    // 頭書きと国号を剥がした名（共和制フランス→フランス・ハワイ王国→ハワイ）＝分断で頭書きを得た国がまた内戦になっても頭書きを重ねない。
    private static string BareName(string name) => StripPolityTitle(name[PrefixLength(name)..]);

    private static bool Taken(string name, IReadOnlyList<Power> existing)
    {
        foreach (var p in existing) if (p.Name == name || p.BaseName == name) return true;
        return false;
    }

    // 埋まっていれば序数を頭に（第2共和制フランス・第2リヨン共和国）＝内部の鍵 BaseName の一意性。
    private static string Unique(string name, IReadOnlyList<Power> existing)
    {
        if (!Taken(name, existing)) return name;
        for (int k = 2; ; k++) { string n = $"第{k}{name}"; if (!Taken(n, existing)) return n; }
    }
}
