namespace PriceOfBread.SimCore;

/// <summary>州の気候帯（2026-07-05・主食の地理化）：代表都市の緯度・経度から「自給ぶんを作る主食」を切り替える。
/// 従来は麦・米・野菜を全州一律で自給免除（食料安全保障）としており、欧州の全州が米を自給する非現実があった。
/// 免除セットを気候帯で切替：麦（雑穀を含む穀物の総称）・野菜＝普遍／米＝モンスーン・熱帯のみ／ジャガイモ＝冷涼帯／トウモロコシ＝新大陸。
/// 適地外の米は生産者ごと除去（茶葉等の気候ゲートと同型）。需要側は主食内の交差弾力が安い地元主食へ逃がすため飢饉にはならない。</summary>
public static class Climate
{
    // 気候帯の緯度・経度しきい値（主食の自給免除セット。値は据え置き＝挙動不変）
    private const double RiceTropicLatMax = 32.0;    // 米＝熱帯・亜熱帯（|緯度|≤32）
    private const double RiceMonsoonMinLon = 60.0;   // 米＝アジア夏雨・灌漑帯の最小経度
    private const double RiceMonsoonMaxLat = 41.0;   // 米＝アジア夏雨・灌漑帯の最大緯度
    private const double PotatoBeltMinLat = 45.0;    // ジャガイモ自給帯＝北緯45以上（冷涼）
    private const double MaizeBeltMaxLon = -30.0;    // トウモロコシ自給帯＝新大陸（経度≤−30）

    /// <summary>米の適地＝熱帯・亜熱帯（|緯度|≤32＝インド・東南アジア・華南・マダガスカル）またはアジアの夏雨・灌漑帯（lon≥60・0≤lat≤41＝
    /// 江南・華北・朝鮮・日本のモンスーンと、中央アジア〜アフガンのオアシス灌漑稲作）。欧州・新大陸の温帯は不適＝賦存列挙州（ポー平原のノヴァーラ・バレンシア・チャールストン等）のみ産する。
    /// 乾燥度は未モデル＝アラビア等の乾燥帯低緯度州も適地に含まれる近似（旧・全州一律よりは狭い）。</summary>
    public static bool RiceSuitable(double lat, double lon) => System.Math.Abs(lat) <= RiceTropicLatMax || (lon >= RiceMonsoonMinLon && lat >= 0.0 && lat <= RiceMonsoonMaxLat);

    /// <summary>ジャガイモの自給帯＝冷涼（北緯45以上＝北欧・露・独・愛・加。1789年時点で定着済みの馬鈴薯前線）。帯外は従来どおり賦存列挙州のみ増産。</summary>
    public static bool PotatoBelt(double lat) => lat >= PotatoBeltMinLat;

    /// <summary>トウモロコシの自給帯＝新大陸（原産地＝アメリカ大陸では普遍的な主食）。旧世界はバルカン・ロンバルディア等の賦存列挙州のみ。</summary>
    public static bool MaizeBelt(double lon) => lon <= MaizeBeltMaxLon;

    /// <summary>A-中5（史実検証2026-07-22）：綿花の適地＝無霜期間180-200日の代理（おおむね|緯度|≤37＝米南部・インド・エジプト・華中以南）。
    /// 旧＝気候ゲートなし＝ロンドン51.5度・マンチェスター53.5度が綿花を産み、prodW=州需要×Scale の労働配分により
    /// 「綿工業化するほど本国綿花自給が増える」という史実と逆の因果（ランカシャー綿花飢饉が構造的に不能）だった。帯外は賦存列挙州のみ産する。</summary>
    public static bool CottonSuitable(double lat) => System.Math.Abs(lat) <= CottonBeltLatMax;
    private const double CottonBeltLatMax = 37.0;    // 第7段第1波①点火（2026-07-28・#17較正後の世界で再点火＝過去2次差し戻し#132は帯のみが理由・テスト側障壁は解消済み）。
    // 無霜期間180-200日の代理＝米南部・印・エジプト・華中以南のみ綿花適地。ランカシャー綿花飢饉が構造的に可能になり
    // 毛織/亜麻の代替レシピ・植民地の海上リードタイム（ColonialImportLagMonths）が存在理由を取り戻す。90.0で旧＝全球適地

    /// <summary>州の主食自給免除（食料安全保障）：この主食は賦存に乏しい州でも減産しない（自給ぶんは作る）。気候帯で主食セットが切り替わる。</summary>
    public static bool StapleSelfSufficient(int goodId, double lat, double lon) => goodId switch
    {
        Catalog.WheatId or Catalog.VegetableId => true,   // 麦・野菜＝普遍（麦は雑穀を含む穀物の総称＝封鎖しても主食のどれかは自給できる安全弁）
        Catalog.RiceId => RiceSuitable(lat, lon),         // 米
        Catalog.PotatoId => PotatoBelt(lat),              // ジャガイモ
        Catalog.MaizeId => MaizeBelt(lon),                // トウモロコシ
        _ => false,
    };

    /// <summary>冬に凍って使えなくなる港か＝その港が閉じる四半期の数（0＝通年・1＝Q1のみ・2＝Q4とQ1）。
    /// 作者裁定2026-08-11「一部の港（サンクトペテルブルクなど）を冬に使用不可にする。これでグレートゲームが創発できる」。
    ///
    /// ★緯度だけでは決められない＝**暖流があるかどうか**が効く。ノルウェーやムルマンスクは北緯70度でも凍らない一方、
    ///   ウラジオストク（北緯43度）は凍る。緯度から導出しようとすると必ずどちらかを間違えるので、
    ///   ここは**実在の海域の性質を書いた表**にする（Climate の座標表と同じ扱い＝データであって式ではない）。
    ///
    /// 対象＝閉ざされた寒い海：バルト海・白海/北極海・オホーツク海と日本海北部・セントローレンス湾。
    /// 対象外＝北大西洋（メキシコ湾流）・黒海・地中海・太平洋の温暖側。
    /// ★これがロシアの不凍港への渇望を生む＝黒海（オスマン）とペルシャ湾（ペルシャ・アフガン）へ南下する動機が
    ///   台本でなく**冬に港が閉じるという物理**から出る。</summary>
    public static int WinterFrozenQuarters(double lat, double lon)
    {
        // ⚠経度の下限12.5度＝デンマーク海峡より東（＝閉じたバルト海）。ここを緩めるとノルウェー沿岸（大西洋）を
        //   巻き込む＝実測でオスロとトロムソを凍らせてしまった（2026-08-11の取りこぼし）。
        // ⚠緯度の上限66度＝ボスニア湾の北端。これが無いと北緯69度のトロムソがバルトの経度帯に入ってしまう。
        // ⚠白海の経度は36度から＝ムルマンスク（東経33度・不凍）を巻き込まないため。北極海に面していても暖流は届く。
        if (lat >= 63 && lon >= 36 && lon <= 70) return 2;                  // 白海（アルハンゲリスク）＝半年近く閉じる
        if (lat is >= 59 and <= 66 && lon >= 12.5 && lon <= 31) return 2;   // バルト海の最奥（サンクトペテルブルク・ストックホルム・ヘルシンキ・ウメオ）
        if (lat is >= 54 and <= 66 && lon >= 12.5 && lon <= 31) return 1;   // バルト海（リガ・ケーニヒスベルク・コペンハーゲン）＝厳冬の1四半期
        if (lat >= 42 && lon >= 128 && lon <= 165) return 1; // オホーツク海・日本海北部（ウラジオストク）＝寒流で緯度の割に凍る
        if (lat >= 45 && lon >= -75 && lon <= -60) return 1; // セントローレンス湾（ケベック・モントリオール）
        return 0;                                            // メキシコ湾流の届く北大西洋（ノルウェー・ムルマンスク）は通年
    }

    /// <summary>この四半期にその港が凍っているか。Q1（1〜3月）が真冬・Q4（10〜12月）がその手前。
    /// 市場の清算は四半期に1度なので、季節はこの粒度で効く。</summary>
    public static bool PortFrozen(double lat, double lon, int quarterOfYear)
    {
        int n = WinterFrozenQuarters(lat, lon);
        return n >= 2 ? quarterOfYear is 0 or 3 : n == 1 && quarterOfYear == 0;
    }

    // ★一元化2026-08-26（作者指示(1)）：座標の実体は places.json（Places.cs）へ移した。
    //   旧＝ここに255件の switch があり、Geo.ColonyCoord（43件）と App.MapData.CityCoords（258件）に**同じ値の写し**が2つあった。
    //   実測で蝦夷地だけ値がずれていた（43.06,141.35 と 43.1,141.3）＝手で対にするしかない不変条件は守られていなかった。
    //   ⚠未登録が (0,0) になる挙動は**そのまま**（Places.Coord の既定）＝呼び出し側の意味は1ビットも変えていない。
    public static (double Lat, double Lon) CityCoord(string city) => Places.Coord(city);
}
