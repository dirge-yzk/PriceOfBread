using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace PriceOfBread.App;

/// <summary>
/// 地図中心UIの地理データ（UI側リソース＝SimCore非接触）。州（都市）には座標が無いので、ここで実在都市の緯度経度を与える。
/// 等距円筒図法（プレートカレ）の仮想キャンバス（<see cref="MapWidth"/>×<see cref="MapHeight"/>）へ投影する。
/// 州の○はこの座標に置き、所有国の色（<see cref="NationColor"/>）で塗る。背景画像は後で差し替え可（投影規約だけ固定）。
/// </summary>
public static class MapData
{
    // 仮想地図の寸法。経度 -180..180 → X、緯度は下の LatTop..LatBottom → Y。
    // 2026-06-30：上下の空白（極域）を削るため緯度を [82, -56] にクロップ。高さはその範囲に合わせる（横360°は不変・アスペクトはほぼ実寸）。
    public const double MapWidth = 2000.0;
    public const double MapHeight = 780.0;

    // 仮想キャンバスの四隅が表す経度/緯度。経度はSVG実測較正（中心+13°E・全360°・継ぎ目は太平洋）。
    // 緯度は背景SVG自体は [98,-100]（極外に余白）だが、表示は空の極域を削って [82,-56] にクロップ（BuildLandBitmap が同じ範囲で背景を切り出す）。
    public const double LonLeft = -166.4, LonRight = 193.6, LatTop = 82.0, LatBottom = -56.0;

    /// <summary>緯度経度→仮想キャンバス座標（等距円筒）。範囲は上の定数で背景に合わせて較正可能。</summary>
    public static (double X, double Y) Project(double lat, double lon)
        => ((lon - LonLeft) / (LonRight - LonLeft) * MapWidth, (LatTop - lat) / (LatTop - LatBottom) * MapHeight);

    /// <summary>都市名→(緯度, 経度)。★一元化2026-08-26（作者指示(1)）：実体は SimCore の places.json（Places.cs）。
    /// ⚠旧＝ここに258件の写しがあり、SimCore 側の表と**手で対に保つ**約束だった。実害の記録は GameViewModel.Map.cs の
    ///   BuildPathGraphLayer（「陸グラフには居るのに この表だけに無かった15町」＝ボルドーからの線が途切れる）。写しを持たなければその病は起きない。</summary>
    public static readonly IReadOnlyDictionary<string, (double Lat, double Lon)> CityCoords = BuildCityCoords();

    private static Dictionary<string, (double Lat, double Lon)> BuildCityCoords()
    {
        var d = new Dictionary<string, (double Lat, double Lon)>();
        foreach (var pl in PriceOfBread.SimCore.Places.All) d[pl.Name] = (pl.Lat, pl.Lon);
        return d;
    }

    // 州（都市）名の英語表記（地図ラベル用）。未登録は日本語名のまま。
    public static readonly Dictionary<string, string> CityEn = new()
    {
        // ★2026-08-02：座標を足した15町の英語名（作者報告「一部の州名がカタカナになっている」）。
        // ⚠En() は対応表に無ければ**日本語をそのまま返す**。CityCoords は SimCore の Places.All から自動生成されるが、
        //   この英語表は手作業のまま＝新しい州を Places.All に足しても英語名は自動で付かず、この表が無ければ地名だけ和名で浮く。
        ["アボメイ"] = "Abomey", ["ウィダー"] = "Ouidah", ["クラクフ"] = "Krakow", ["トゥールーズ"] = "Toulouse",
        ["トビリシ"] = "Tbilisi", ["トリエステ"] = "Trieste", ["ルアーヴル"] = "Le Havre",
        ["京都"] = "Kyoto", ["全州"] = "Jeonju", ["北九州"] = "Kitakyushu", ["南京"] = "Nanjing",
        ["天津"] = "Tianjin", ["横浜"] = "Yokohama", ["釜山"] = "Busan", ["開城"] = "Kaesong",
        ["パリ"] = "Paris", ["リヨン"] = "Lyon", ["マルセイユ"] = "Marseille", ["ボルドー"] = "Bordeaux", ["リール"] = "Lille",
        ["江戸"] = "Edo", ["大阪"] = "Osaka", ["仙台"] = "Sendai", ["長崎"] = "Nagasaki",
        ["ロンドン"] = "London", ["マンチェスター"] = "Manchester", ["バーミンガム"] = "Birmingham", ["グラスゴー"] = "Glasgow",
        ["ニューヨーク"] = "New York", ["フィラデルフィア"] = "Philadelphia", ["ボストン"] = "Boston", ["チャールストン"] = "Charleston",
        ["モスクワ"] = "Moscow", ["サンクトペテルブルク"] = "St. Petersburg", ["リガ"] = "Riga", ["キエフ"] = "Kiev",
        ["ハリコフ"] = "Kharkov", ["カザン"] = "Kazan", ["ニジニ・ノヴゴロド"] = "Nizhny Novgorod", ["アルハンゲリスク"] = "Arkhangelsk",
        ["エカテリンブルク"] = "Yekaterinburg", ["ロストフ"] = "Rostov", ["オムスク"] = "Omsk",
        ["ベルリン"] = "Berlin", ["シュテッティン"] = "Stettin", ["ケーニヒスベルク"] = "Königsberg", ["ブレスラウ"] = "Breslau", ["ダンツィヒ"] = "Danzig",
        ["イスタンブール"] = "Istanbul", ["カイロ"] = "Cairo", ["スミルナ"] = "Smyrna", ["アンカラ"] = "Ankara",
        ["エルズルム"] = "Erzurum", ["ダマスカス"] = "Damascus", ["エルサレム"] = "Jerusalem", ["バグダード"] = "Baghdad", ["バスラ"] = "Basra",
        ["ベオグラード"] = "Belgrade", ["ソフィア"] = "Sofia", ["サロニカ"] = "Salonica", ["アテネ"] = "Athens", ["ブカレスト"] = "Bucharest",
        ["ウィーン"] = "Vienna", ["グラーツ"] = "Graz", ["プラハ"] = "Prague", ["ブリュン"] = "Brünn", ["ブダペスト"] = "Budapest",
        ["プレスブルク"] = "Pressburg", ["レンベルク"] = "Lemberg", ["ザグレブ"] = "Zagreb", ["クラウゼンブルク"] = "Klausenburg", ["ミラノ"] = "Milan",
        ["ブリュッセル"] = "Brussels", ["アムステルダム"] = "Amsterdam", ["ロッテルダム"] = "Rotterdam", ["ハーグ"] = "The Hague", ["ユトレヒト"] = "Utrecht",
        ["マドリード"] = "Madrid", ["バルセロナ"] = "Barcelona", ["セビーリャ"] = "Seville", ["バレンシア"] = "Valencia",
        ["カディス"] = "Cadiz", ["ビルバオ"] = "Bilbao",   // 経路グラフ第1段 Stage B（§13州再配分・2026-08-09）
        ["トリノ"] = "Turin", ["カリャリ"] = "Cagliari", ["ノヴァーラ"] = "Novara",
        ["上海"] = "Shanghai", ["済南"] = "Jinan", ["成都"] = "Chengdu", ["開封"] = "Kaifeng", ["北京"] = "Beijing",
        ["漢口"] = "Hankou", ["長沙"] = "Changsha", ["杭州"] = "Hangzhou", ["広州"] = "Guangzhou", ["西安"] = "Xi'an",
        ["福州"] = "Fuzhou", ["桂林"] = "Guilin", ["昆明"] = "Kunming", ["奉天"] = "Mukden", ["台北"] = "Taipei",
        ["フフホト"] = "Hohhot", ["ハルビン"] = "Harbin", ["ウルガ"] = "Urga", ["ラサ"] = "Lhasa", ["カシュガル"] = "Kashgar",
        ["漢城"] = "Hanseong", ["平壌"] = "Pyongyang",
        ["プーナ"] = "Pune", ["ナーグプル"] = "Nagpur", ["グワーリヤル"] = "Gwalior", ["インドール"] = "Indore", ["バローダ"] = "Baroda", ["バサイン"] = "Bassein",
        ["シュリーランガパトナ"] = "Srirangapatna",
        ["バンガロール"] = "Bangalore", ["マンガロール"] = "Mangalore",   // Stage B
        ["ハイデラバード"] = "Hyderabad",
        ["オーランガーバード"] = "Aurangabad", ["ワランガル"] = "Warangal",   // Stage B
        ["ラホール"] = "Lahore",
        ["ラングーン"] = "Rangoon", ["アマラプラ"] = "Amarapura",
        ["カトマンズ"] = "Kathmandu",
        ["バンコク"] = "Bangkok",
        ["ハノイ"] = "Hanoi", ["フエ"] = "Hue",
        ["プノンペン"] = "Phnom Penh",
        ["ヴィエンチャン"] = "Vientiane",
        ["テヘラン"] = "Tehran", ["イスファハン"] = "Isfahan", ["シーラーズ"] = "Shiraz", ["タブリーズ"] = "Tabriz", ["メシェッド"] = "Mashhad", ["ブーシェフル"] = "Bushehr",
        ["ディルイーヤ"] = "Diriyah", ["カティーフ"] = "Qatif",   // 地理データ実装（2026-08-16）
        ["サヌア"] = "Sana'a", ["モカ"] = "Mocha",
        ["マスカット"] = "Muscat",
        ["カブール"] = "Kabul", ["カンダハール"] = "Kandahar", ["ヘラート"] = "Herat", ["ペシャーワル"] = "Peshawar",
        ["ブハラ"] = "Bukhara", ["サマルカンド"] = "Samarkand",
        ["ヒヴァ"] = "Khiva",
        ["タシュケント"] = "Tashkent", ["コーカンド"] = "Kokand",
        ["フェズ"] = "Fez", ["マラケシュ"] = "Marrakesh", ["タンジェ"] = "Tangier",
        ["ゴンダール"] = "Gondar", ["マサワ"] = "Massawa",   // 地理データ実装（2026-08-16）
        ["アンタナナリヴ"] = "Antananarivo", ["トアマシナ"] = "Toamasina",
        ["ストックホルム"] = "Stockholm", ["ヨーテボリ"] = "Gothenburg", ["ウメオ"] = "Umeå", ["ヘルシングフォシュ"] = "Helsingfors",
        ["コペンハーゲン"] = "Copenhagen", ["クリスチャニア"] = "Christiania", ["トロムソ"] = "Tromsø",
        ["ベルン"] = "Bern",
        ["ワルシャワ"] = "Warsaw", ["ヴィルナ"] = "Vilna",
        ["ポズナン"] = "Poznań", ["ルブリン"] = "Lublin",   // Stage B
        ["リスボン"] = "Lisbon", ["ポルト"] = "Porto",
        ["ジェノヴァ"] = "Genoa",
        ["ケルン"] = "Cologne",
        ["ナポリ"] = "Naples", ["パレルモ"] = "Palermo",
        ["バーリ"] = "Bari", ["メッシーナ"] = "Messina",   // Stage B
        ["ローマ"] = "Rome", ["ボローニャ"] = "Bologna", ["チヴィタヴェッキア"] = "Civitavecchia",
        ["フィレンツェ"] = "Florence", ["リヴォルノ"] = "Livorno",
        ["ヴェネツィア"] = "Venice", ["パドヴァ"] = "Padua", ["ザラ"] = "Zara",
        ["ミュンヘン"] = "Munich",
        ["ドレスデン"] = "Dresden",
        ["ハノーファー"] = "Hanover", ["クックスハーフェン"] = "Cuxhaven",   // 地理データ実装（2026-08-16・E2裁定=与える）
        ["シュトゥットガルト"] = "Stuttgart",
        ["サイゴン"] = "Saigon",
        ["アデン"] = "Aden",
        ["デリー"] = "Delhi",
        ["ラクナウ"] = "Lucknow", ["ホノルル"] = "Honolulu",
        ["ダブリン"] = "Dublin", ["バクー"] = "Baku",
        ["蝦夷地"] = "Ezo", ["ウラル"] = "Ural", ["シベリア"] = "Siberia", ["ウラジオストク"] = "Vladivostok", ["西部"] = "The West",
        // ★植民地の積出港の英語名（州昇格2026-08-09）：CityCoords は自動生成なのでこの表だけ手作業で足す必要がある（無いと地名だけ和名で浮く）
        ["マルティニーク"] = "Martinique", ["ポンディシェリ"] = "Pondicherry", ["マドラス"] = "Madras", ["ボンベイ"] = "Bombay",
        ["ジャマイカ"] = "Jamaica", ["沿岸西アフリカ"] = "West African Coast", ["カナダ"] = "Canada", ["オーストラリア"] = "Australia",
        ["アラスカ"] = "Alaska", ["琉球"] = "Ryukyu", ["アルタ=カリフォルニア"] = "Alta California", ["ボルネオ"] = "Borneo",
        ["クマシ"] = "Kumasi", ["アノマブ"] = "Anomabu", ["コンゴ"] = "Congo", ["アルジェ"] = "Algiers", ["アシャンティ"] = "Ashanti", ["ソコト"] = "Sokoto",
        ["カノ"] = "Kano", ["ザリア"] = "Zaria", ["ンバンザ=コンゴ"] = "Mbanza-Kongo", ["ボマ"] = "Boma",
        ["コンスタンティーヌ"] = "Constantine", ["オラン"] = "Oran", ["オヨ=イレ"] = "Oyo-Ile", ["バダグリ"] = "Badagry", ["ンガザルガム"] = "Ngazargamu",
        ["ザンジバル"] = "Zanzibar", ["南アフリカ"] = "South Africa", ["バタヴィア"] = "Batavia", ["ケープタウン"] = "Cape Town",
        ["セイロン"] = "Ceylon", ["スリナム"] = "Suriname", ["カラカス"] = "Caracas", ["ペルー"] = "Peru",
        ["ブラジル"] = "Brazil", ["キューバ"] = "Cuba", ["フィリピン"] = "Philippines", ["小アンティル"] = "Lesser Antilles",
        ["ウィンドワード諸島"] = "Windward Islands", ["中核ベンガル"] = "Bengal Proper", ["内陸ベンガル"] = "Inland Bengal",
        ["北部サン=ドマング"] = "North Saint-Domingue", ["南部サン=ドマング"] = "South Saint-Domingue", ["マスカリン諸島"] = "Mascarene Islands",
        ["セネガル"] = "Senegal", ["ヌエバ・エスパーニャ銀地帯"] = "New Spain Silver Belt", ["中央メキシコ"] = "Central Mexico",
        ["ヌエバ・グラナダ"] = "New Granada", ["ラ・プラタ"] = "La Plata", ["プエルトリコ"] = "Puerto Rico",
        ["ルイジアナ・フロリダ"] = "Louisiana-Florida", ["マレー"] = "Malaya", ["ジブラルタル"] = "Gibraltar",
        ["チリ"] = "Chile", ["アマゾン"] = "Amazonia",   // R0b b-2 の追加植民地（2026-08-13）＝積出港はバルパライソ／ベレン
        ["パナマ"] = "Panama",   // ⚠「ボスポラス」は削除（2026-08-10）＝州イスタンブール（Istanbul）に統合
        // ★2026-09-02（作者指摘「州名が一部カタカナになっています」）：辺境・植民地標的の80件が未登録＝日本語のまま出ていた。
        ["アイグン"] = "Aigun", ["アクモラ"] = "Akmolinsk", ["アスンシオン"] = "Asuncion",
        ["アデレード"] = "Adelaide", ["アナディリ"] = "Anadyr", ["アベシェ"] = "Abeche",
        ["アリススプリングス"] = "Alice Springs", ["イカルイト"] = "Iqaluit", ["イキトス"] = "Iquitos",
        ["ウォルビスベイ"] = "Walvis Bay", ["ウジジ"] = "Ujiji", ["エドモントン"] = "Edmonton",
        ["エルパソ"] = "El Paso", ["オブドルスク"] = "Obdorsk", ["オホーツク"] = "Okhotsk",
        ["オークランド"] = "Auckland", ["カルメン・デ・パタゴネス"] = "Carmen de Patagones", ["カンパラ"] = "Kampala",
        ["キサンガニ"] = "Kisangani", ["キト"] = "Quito", ["クイアバ"] = "Cuiaba",
        ["クルマン"] = "Kuruman", ["グリエフ"] = "Guryev", ["ゴイアス"] = "Goias",
        ["サラーラ"] = "Salalah", ["サルヴァドール"] = "Salvador", ["サンタフェ"] = "Santa Fe",
        ["サンボアンガ"] = "Zamboanga", ["サンルイス"] = "Sao Luis", ["シンゲッティ"] = "Chinguetti",
        ["スレドネコリムスク"] = "Srednekolymsk", ["セントジョンズ"] = "St. John's", ["セントポール"] = "St. Paul",
        ["セントマイケル"] = "St. Michael", ["センナール"] = "Sennar", ["ソルトレークシティ"] = "Salt Lake City",
        ["タマンラセット"] = "Tamanrasset", ["ダニーデン"] = "Dunedin", ["ダーウィン"] = "Darwin",
        ["ダーバン"] = "Durban", ["チャーチル"] = "Churchill", ["テテ"] = "Tete",
        ["トゥクマン"] = "Tucuman", ["トゥルハンスク"] = "Turukhansk", ["トンブクトゥ"] = "Timbuktu",
        ["ドンゴラ"] = "Dongola", ["ナイン"] = "Nain", ["ヌーク"] = "Nuuk",
        ["ハミ"] = "Hami", ["バルナウル"] = "Barnaul", ["バロー"] = "Barrow",
        ["バンクーバー"] = "Vancouver", ["バンダアチェ"] = "Banda Aceh", ["パース"] = "Perth",
        ["ファショダ"] = "Fashoda", ["フォートウィリアム"] = "Fort William", ["フォートシンプソン"] = "Fort Simpson",
        ["フォートユーコン"] = "Fort Yukon", ["フリータウン"] = "Freetown", ["ブラワヨ"] = "Bulawayo",
        ["ブリスベン"] = "Brisbane", ["プンタアレナス"] = "Punta Arenas", ["ペトロパヴロフスク"] = "Petropavlovsk",
        ["ホバート"] = "Hobart", ["ホータン"] = "Khotan", ["ボゴタ"] = "Bogota",
        ["ポートモレスビー"] = "Port Moresby", ["マカッサル"] = "Makassar", ["マナウス"] = "Manaus",
        ["マノクワリ"] = "Manokwari", ["ムルズク"] = "Murzuk", ["モガディシュ"] = "Mogadishu",
        ["ヤウンデ"] = "Yaounde", ["ヤクーツク"] = "Yakutsk", ["ラパス"] = "La Paz",
        ["ルアンダ"] = "Luanda", ["レイキャヴィク"] = "Reykjavik", ["レオン"] = "Leon",
        ["ロレート"] = "Loreto", ["蘭州"] = "Lanzhou",
    };

    /// <summary>州（都市）名の英語表記。未登録は日本語名を返す。</summary>
    public static string En(string name) => CityEn.TryGetValue(name, out var en) ? en : name;

    // 国名の英語表記（2026-09-02作者指示＝州名が消えるまでズームを引いたら国名を出す）。
    // world.json の61カ国＋建国できる国名（Formations.cs のルール名）。⚠分離独立で生まれた国など未登録は日本語名のまま（En と同じ作法）。
    private static readonly Dictionary<string, string> NationEnMap = new()
    {
        ["フランス"] = "France", ["日本"] = "Japan", ["イギリス"] = "Britain", ["アメリカ"] = "United States",
        ["ロシア"] = "Russia", ["プロイセン"] = "Prussia", ["オスマン"] = "Ottoman Empire", ["オーストリア"] = "Austria",
        ["ネーデルラント"] = "Netherlands", ["スペイン"] = "Spain", ["サルデーニャ＝ピエモンテ"] = "Sardinia-Piedmont",
        ["清"] = "Qing", ["朝鮮"] = "Joseon", ["マラータ同盟"] = "Maratha Confederacy", ["マイソール"] = "Mysore",
        ["ハイデラバード"] = "Hyderabad", ["シク連邦"] = "Sikh Confederacy", ["ビルマ"] = "Burma", ["ネパール"] = "Nepal",
        ["シャム"] = "Siam", ["西山朝"] = "Tay Son", ["カンボジア"] = "Cambodia", ["ラオス"] = "Laos",
        ["ペルシャ"] = "Persia", ["アラビア"] = "Arabia", ["イエメン"] = "Yemen", ["オマーン"] = "Oman",
        ["アフガニスタン"] = "Afghanistan", ["ブハラ"] = "Bukhara", ["ヒヴァ"] = "Khiva", ["コーカンド"] = "Kokand",
        ["モロッコ"] = "Morocco", ["エチオピア"] = "Ethiopia", ["マダガスカル"] = "Madagascar", ["スウェーデン"] = "Sweden",
        ["デンマーク"] = "Denmark", ["スイス"] = "Switzerland", ["ポーランド"] = "Poland", ["ポルトガル"] = "Portugal",
        ["ジェノヴァ"] = "Genoa", ["ライン諸邦"] = "Rhine States", ["両シチリア"] = "Two Sicilies", ["教皇領"] = "Papal States",
        ["トスカーナ"] = "Tuscany", ["ヴェネツィア"] = "Venice", ["バイエルン"] = "Bavaria", ["ザクセン"] = "Saxony",
        ["ハノーファー"] = "Hanover", ["ヴュルテンベルク"] = "Wurttemberg", ["阮氏"] = "Nguyen", ["ラヘジ"] = "Lahej",
        ["デリー"] = "Delhi", ["アワド"] = "Awadh", ["ダホメ王国"] = "Dahomey", ["アシャンティ王国"] = "Ashanti",
        ["ソコト"] = "Sokoto", ["コンゴ王国"] = "Kongo", ["アルジェ摂政国"] = "Algiers", ["オヨ帝国"] = "Oyo",
        ["ボルヌ帝国"] = "Bornu", ["ハワイ王国"] = "Hawaii",
        ["ローマ"] = "Rome", ["神聖ローマ"] = "Holy Roman Empire", ["ビザンツ"] = "Byzantium", ["ドイツ"] = "Germany",
        ["イタリア"] = "Italy", ["北ドイツ連邦"] = "North German Confederation", ["フランス帝国"] = "French Empire",
        ["大英帝国"] = "British Empire", ["ポーランド＝リトアニア"] = "Poland-Lithuania", ["スカンジナビア"] = "Scandinavia",
        ["イベリア"] = "Iberia", ["大ネーデルラント"] = "Greater Netherlands", ["ユーゴスラビア"] = "Yugoslavia",
        ["大ブルガリア"] = "Greater Bulgaria", ["ギリシャ"] = "Greece", ["ハンガリー"] = "Hungary",
        ["大ルーマニア"] = "Greater Romania", ["チェコスロバキア"] = "Czechoslovakia", ["カリフ国"] = "Caliphate",
        ["トゥーラーン"] = "Turan", ["マシュリク"] = "Mashriq", ["ペルシア帝国"] = "Persian Empire",
        ["トルキスタン"] = "Turkestan", ["ドゥッラーニー帝国"] = "Durrani Empire", ["サウード"] = "Saud",
        ["ルーム"] = "Rum", ["ムワッヒド"] = "Almohad", ["大モンゴル"] = "Great Mongolia", ["バーラト"] = "Bharat",
        ["ムガル帝国"] = "Mughal Empire", ["インドシナ"] = "Indochina", ["大日本"] = "Greater Japan", ["中華"] = "China",
        ["大チベット"] = "Greater Tibet", ["マラーター帝国"] = "Maratha Empire", ["越南"] = "Vietnam",
        ["シャム帝国"] = "Siamese Empire", ["ビルマ帝国"] = "Burmese Empire", ["統一朝鮮"] = "Unified Korea",
        ["満洲国"] = "Manchuria",
    };

    /// <summary>国名の英語表記。未登録は日本語名を返す（分離独立で生まれた国など）。</summary>
    public static string EnNation(string name) => NationEnMap.TryGetValue(name, out var en) ? en : name;

    // 主要国の固有色（Paradox/史実風）：仏=青・英=赤・露=深緑・墺=ハプスブルク白・オスマン=緑・西=金・清=帝国黄・日=深紅・蘭=オレンジ 等。
    private static readonly Dictionary<string, uint> NationHex = new()
    {
        ["フランス"] = 0x2E50A0, ["イギリス"] = 0xC13A35, ["ロシア"] = 0x3C6E3A, ["プロイセン"] = 0x41506E,
        ["オーストリア"] = 0xD7D2BC, ["オスマン"] = 0x5E8C3C, ["スペイン"] = 0xD2A22C, ["清"] = 0xE2C84B,
        ["日本"] = 0x7A2230, ["アメリカ"] = 0x2563EB, ["ネーデルラント"] = 0xE0801C, ["スウェーデン"] = 0x4A82C4,
        ["デンマーク"] = 0xC0453F, ["ポルトガル"] = 0x1F8A6B, ["ポーランド"] = 0xB23A52,
        ["サルデーニャ=ピエモンテ"] = 0xA06B8A, ["朝鮮"] = 0x1F4E8C, ["ペルシャ"] = 0x8E44AD,
        ["教皇領"] = 0xD9C97A, ["スイス"] = 0x8A6D3B, ["マラータ同盟"] = 0xC0457A, ["シク連邦"] = 0xE0883C,
        ["モロッコ"] = 0xB03A3A, ["エチオピア"] = 0x6EC6E0, ["ビルマ"] = 0x4E8A82, ["シャム"] = 0x5B7AC0,
        ["バイエルン"] = 0x6E9AC8, ["両シチリア"] = 0xC07A3C, ["デリー"] = 0x8C5B8A, ["マイソール"] = 0x4F8C6A,
        ["ハイデラバード"] = 0x9C7A3C, ["オマーン"] = 0xB03A52, ["ライン諸邦"] = 0x7A8CA0,
        // ユーザー指定（2026-06-30）：トスカーナ↔サルデーニャ／ヴュルテンベルク↔スイスを入替・ジェノヴァ濃黄・マダガスカル濃緑・ベトナム(西山朝/阮氏)茶
        ["トスカーナ"] = 0x6A57B0, ["ヴュルテンベルク"] = 0xD04545, ["ジェノヴァ"] = 0xC8A800,
        ["マダガスカル"] = 0x1C4D2A, ["西山朝"] = 0x8B5A2B, ["阮氏"] = 0x6F4520,
    };

    // その他の国（小邦・各侯国）の識別パレット（落ち着いた中間色＝Paradox風）。国インデックスで割り当てる。
    private static readonly uint[] Palette =
    {
        0x8A6D3B, 0x6B8E4E, 0x4E7A8A, 0x9A5B6B, 0x7A6BA0, 0x5B8A6B, 0xA0794E, 0x6B6BA0,
        0x8A8A4E, 0xA05B5B, 0x4E8A7A, 0x8A5BA0, 0x6BA08A, 0xA08A4E, 0x5B6B8A, 0x8A4E6B,
        0x6B8AA0, 0x9A8A5B, 0x7AA05B, 0xA06B8A, 0x4E6B5B, 0x8A7A6B, 0x5BA0A0, 0xA05B7A,
    };

    /// <summary>国名→色（主要国は固有色・その他はパレットをインデックス割当）。所有国ごとに州○を塗り分ける。</summary>
    public static Color NationColor(string name, int fallbackIndex)
    {
        uint v = NationHex.TryGetValue(name, out var h) ? h : Palette[((fallbackIndex % Palette.Length) + Palette.Length) % Palette.Length];
        return Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);
    }
}
