namespace PriceOfBread.SimCore;

/// <summary>財1つの定義（§2.4・§11.7）。Inputs 空＝RGO（採取）。IsColonial＝気候ゲート財（賦存州のみ産する・Catalog.IsColonial と同値）。
/// NonTradeable＝役務（労働力・教育・医療・行政）。輸送できず地元市場のみ＝貿易・世界価格の対象外。
/// GrainChain＝穀物法の課税客体（A-高3）＝麦15・小麦粉47。史実の客体は穀粒と挽き割りで焼成パンの国際貿易は無い＝Tier==Staple ゲートだと小麦粉輸入→国内製パンで法が迂回される。</summary>
public readonly record struct GoodDef(
    int Id, string Name, GoodTier Tier, double BaseNeed, double Elasticity, double Productivity, Input[] Inputs, bool IsColonial = false, bool NonTradeable = false, bool GrainChain = false,
    double Nutrition = -1,   // ★塊1（2026-08-31）：食べ物か（1=食料としてFoodFillに数える）。-1＝Tier由来（Staple=1/他=0）＝旧goods.jsonも厳密恒等。GrainChainと同じ「Tierから独立した1本のフラグ」作法
    double WearRatePerMonth = double.NaN,   // ★(346)：在庫の月の損耗の率（旧 Catalog.SpoilageOf の switch の値をそのまま置いた＝この欄が唯一の実体）。耐久度 <see cref="Catalog.DurabilityOf"/>＝1−この値は読み口だけで出す。NaN＝書き漏れ（番人が止める）
    GoodUse Use = GoodUse.Consumed);   // ★(346)：使われ方（家計が買う財にだけ意味を持つ）。今は読み手なし（段F〔持って効く財〕が読む）

/// <summary>★(346) 財の使われ方（設計 docs/202610/20261015/20261015_消費の入口_設計.md §2-2）。
/// Consumed＝使って消える（買った月に効能を出して消える＝食料・酒・煙草・茶・砂糖・石鹸・灯油・薬・新聞・書籍ほか）／
/// Held＝持って効く（POP の手元の在庫へ足され毎月 d で減る＝家具・時計・宝飾・楽器・自転車・自動車・ラジオ・電化製品・冷蔵庫）。
/// ⚠今は**読み手が無い**（器だけ・挙動は変わらない）。家計が買わない財（素材・中間財・役務）は既定の Consumed のまま（意味を持たない）。</summary>
public enum GoodUse { Consumed, Held }

/// <summary>
/// サプライチェーン（1780–1880・ユーザー設計）。基本は ≤2入力・最終財の入力ペアは固有・深さ優先だが、
/// **意味のある所は3入力を許可**（例：缶詰＝肉+鉄+塩）。**複数レシピ＝同じ財に複数の生産者**
/// （例：電力＝石炭火力 or 石油火力、労働力＝人力 or 蒸気機関 or 電化）：市場が安いレシピを選ぶ
/// （採算の良い生産者が操業規模と売上シェアを奪う）＝「A+B+(C or D)」は燃料違いの並存レシピで表現。
/// ハブ財（石炭・鋼・硫酸）は相方を必ず変える（反モノカルチャー）。
/// 気候ゲート財（IsColonial）は賦存州（Resources に列挙）だけが産する＝供給の偏在が地理から創発。
/// 建設（工場/鉄道/艦隊）は財でなく束消費（別系統＝EconomyStep の工業化投資が鋼+機械+セメント+製材を食う）。
/// </summary>
public static class Catalog
{
    private static Input I(int g, double q) => new(g, q);

    private static GoodDef[] LoadGoods()
    {
        var asm = typeof(Catalog).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("goods.json", System.StringComparison.Ordinal));
        if (name is null) return BuiltinGoods;   // goods.json 未埋め込み時は C# 定義で動く（dump前・後方互換）
        using var s = asm.GetManifestResourceStream(name)!;
        return System.Text.Json.JsonSerializer.Deserialize<GoodDef[]>(s, WorldData.Options) ?? BuiltinGoods;
    }

    /// <summary>C# 定義（BuiltinGoods）を goods.json へ書き出す（dumpgoods 用・C#とビット等価の goods.json を生成）。</summary>
    public static string Serialize() => System.Text.Json.JsonSerializer.Serialize(BuiltinGoods, WorldData.Options);

    // ★(346) 月の損耗の率の欄（WearRatePerMonth）の読み方＝旧 SpoilageOf の switch の見出し（値は財ごとの欄へ移した・理由の注はここへ残す）：
    //   1.0＝役務（労働力・教育・医療・行政・兵力・高度/農業労働力・家事奉仕・高等教育136）＋電力(87)/送電網(99)＝NonTradeable の非貯蔵物理財＝貯蔵不能で当期生産→当期消費（余剰は消える）
    //        〔2026-07-19 バグ監査：既定 0.005 で電力在庫が滞留し電化採算を歪めていたのを是正／136 は 94「教育」と同型の Service 財なのに欠落（監査 2026-08-31）〕。耐久度 d＝0。
    //   0.25 パン（数日）／0.20 果実・漁獲・野菜・ジャガイモ（生鮮塊茎）／0.15 畜産・肉／0.10 サトウキビ
    //   0.06 麦・小麦粉・葉煙草・加工肉・ビール・飼料・トウモロコシ・米（乾燥穀粒）〔デ9：米(102)は従来 default に落ち 0.005＝鋼並みの保存性で米在庫が永久滞留し価格床を作りえた。乾燥穀粒 0.06 へ〕
    //   0.04 馬〔B6 レビュー 2026-07-19：生体在庫の飼養損耗＝金属既定 0.005 に落とすと馬余りが半永久滞留し価格床になる。革(0.02)と飼料(0.06)の間〕
    //   0.03 木・生糸・嗜好品原料・薬・化学・パルプ・チーズ・グアノ等（半生鮮）／0.02 繊維・木材・紙・革・砂糖・衣料・毛皮等
    //   0.005＝金属・鉱物・燃料・機械・武器・耐久奢侈財（最小）。だぶついた在庫が永久に滞留して価格を床へ固定するのを防ぐ（穀物は早く腐り、鉄は長く保つ＝ユーザー指定）。
    //   ⚠この欄は損耗の率のまま（読み手の式が `×(1−率)`・`÷率`・冷蔵の `率×(1−効き×冷蔵)` で率を直接使う＝`1−(1−率)` を作り直すと恒等が崩れる）。
    //   耐久度 d への切り替え（出典の耐用年数から入れ直す）は設計 §2-3 の挙動の段。
    private static readonly GoodDef[] BuiltinGoods =
    [
        // === 素材：国内採取（RGO）===
        new(IronOreId, "鉄鉱石",   GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CoalId, "石炭",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CopperOreId, "銅鉱",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(LeadOreId, "鉛鉱",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(GoldOreId, "金",       GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.005),   // 旧「貴金属」を金/銀に分割（銀=120）。金＝ブラジル/西アフリカ/コロンビア/シベリア。産地州はgen_worldで金/銀に振分
        new(SulfurId, "硫黄",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(SaltId, "塩",       GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(LimestoneId, "石灰石",   GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(SilicaSandId, "珪砂",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(ClayId, "粘土",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(CrudeOilId, "原油",     GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.005),
        new(TimberId, "木",       GoodTier.Intermediate, 0.0, 0.5, 3.0, [], WearRatePerMonth: 0.03),
        new(CottonId, "綿花",     GoodTier.Intermediate, 0.0, 0.5, 2.6, [], WearRatePerMonth: 0.02),
        new(HempId, "麻",       GoodTier.Intermediate, 0.0, 0.5, 2.6, [], WearRatePerMonth: 0.02),
        new(SilkId, "生糸",     GoodTier.Intermediate, 0.0, 0.5, 2.2, [], WearRatePerMonth: 0.03),
        new(WheatId, "麦",       GoodTier.Intermediate, 0.0, 0.4, 2.6, [], GrainChain: true, WearRatePerMonth: 0.06),   // A-高3：穀物法の課税客体（1815年 Importation Act の対象は穀粒）
        new(FruitId, "果実",     GoodTier.Intermediate, 0.0, 0.6, 2.4, [], WearRatePerMonth: 0.2),
        new(DyeId, "染料",     GoodTier.Intermediate, 0.0, 0.5, 2.5, [], WearRatePerMonth: 0.02),
        new(TobaccoLeafId, "葉煙草",   GoodTier.Intermediate, 0.0, 0.7, 2.3, [], WearRatePerMonth: 0.06),
        new(LivestockId, "畜産",     GoodTier.Intermediate, 0.0, 0.5, 2.4, [], WearRatePerMonth: 0.15),
        new(FishId, "漁獲",     GoodTier.Intermediate, 0.0, 0.6, 2.4, [], WearRatePerMonth: 0.2),

        // === 素材：植民地限定（第1段は国内採取で代用・IsColonial）===
        new(RawRubberId, "生ゴム",   GoodTier.Intermediate, 0.0,  0.5, 2.2, [], true, WearRatePerMonth: 0.02),
        new(PalmOilId, "パーム油", GoodTier.Intermediate, 0.0,  0.5, 2.2, [], true, WearRatePerMonth: 0.03),
        new(IvoryId, "象牙",     GoodTier.Intermediate, 0.0,  0.9, 1.8, [], true, WearRatePerMonth: 0.005),
        new(SugarCaneId, "サトウキビ", GoodTier.Intermediate, 0.0, 0.6, 2.4, [], true, WearRatePerMonth: 0.1),
        new(CocoaId, "カカオ",   GoodTier.Intermediate, 0.0,  0.8, 2.0, [], true, WearRatePerMonth: 0.03),
        new(SpiceId, "香辛料",   GoodTier.Luxury,       0.05, 0.9, 2.0, [], true, WearRatePerMonth: 0.005),   // 直接消費

        // === 一次加工（≤2入力）===
        new(IronId, "鉄",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(IronOreId,1.0), I(CoalId,0.824), I(LimestoneId,0.25)], WearRatePerMonth: 0.005),   // 鉄鉱石＋石炭（コークス炉の分を含む）＋石灰石(造滓剤フラックス＝脈石を除きスラグ化する高炉の必須投入)。製鉄三点セット(鉄鉱石+石炭+石灰石)の地理的共立が立つ。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：コークス（石炭1.0の素通し）を畳む＝旧 コークス0.5 を参照価格の比（0.89÷0.54）で石炭0.824へ＝参照価格は旧のまま（コークス1t≈石炭1.4〜1.6t の物の量とも合う）
        new(SteelId, "鋼",       GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(IronId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 コークス0.4→石炭0.659（参照価格を保つ換算）
        new(CopperId, "銅",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(CopperOreId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 コークス0.4→石炭0.659。⚠銅鉱は畳まない＝原料→製錬の行程（スウォンジーがコーンウォール・チリ・キューバの鉱石を製錬＝作者裁定 2026-09-29「加工は畳まない」）
        new(LeadId, "鉛",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(LeadOreId,1.0), I(CoalId,0.659)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 コークス0.4→石炭0.659（銅と同じ理由で鉛鉱は残す）
        new(CementId, "セメント", GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LimestoneId,0.8), I(ClayId,0.2), I(CoalId,0.4)], WearRatePerMonth: 0.005),   // ★(493) G15：ポルトランドセメントは石灰石＋粘土（石灰石だけでは生石灰）。粘土 0.2 は石灰石 1→0.8 と入れ替え（採取どうし＝参照価格は同じ）
        new(GlassId, "ガラス",   GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(SilicaSandId,0.7), I(LimestoneId,0.2028), I(SodaAshId,0.15)], WearRatePerMonth: 0.005),   // ★(493) G6：ソーダ石灰ガラスの融剤はソーダ灰。足した分は珪砂 1→0.7・石灰石 0.3→0.2028 で払う（費用中立＝旧の参照価格 1.052 を保つ）
        new(SulfuricAcidId, "硫酸", GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(SulfurId,0.8), I(SaltpeterId,0.1), I(CoalId,0.6)], WearRatePerMonth: 0.03),   // ★(493) 鉛室法（ローバック 1746）＝硫黄を焼き硝石を触媒にする。質量 1.5＝旧の化学薬品（硫黄 0.6＋石炭 0.5＋塩 0.4）と同じ＝参照価格 1.16 を保つ。旧は塩を含み酸の行からアルカリが出る形（作者 2026-10-21〜23 の最低ライン）。黄鉄鉱法は並存（Simulation）
        new(GunpowderId, "火薬",     GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(SaltpeterId,0.75), I(SulfurId,0.10), I(TimberId,0.15)], WearRatePerMonth: 0.03),   // ★(493) G5：黒色火薬の炭は木炭＝木から（鉱物の石炭は炭にならない）。採取どうし＝参照価格は同じ   // 黒色火薬＝硝石(75%)＋硫黄(10%)＋石炭(15%＝木炭の抽象)。史実配合比。総投入量1.0保存＝硝石/硫黄/石炭は同一RGO RefPriceのため火薬RefPriceは不変。化学薬品を要さない近世の火薬（1789から作れる）。無煙火薬は化学薬品ベースの並存レシピで晩期に。硝石を欠く国は輸入硝石に律される（英のベンガル独占）
        new(LumberId, "製材",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(TimberId,1.0)], WearRatePerMonth: 0.02),
        new(CottonClothId, "綿布",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(CottonId,1.0)], WearRatePerMonth: 0.02),
        new(CanvasId, "帆布",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(HempId,1.0)], WearRatePerMonth: 0.02),
        new(WoolId, "羊毛",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LivestockId,0.5)], WearRatePerMonth: 0.02),
        new(WoolenClothId, "毛織物",   GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(WoolId,1.0)], WearRatePerMonth: 0.02),
        new(SilkClothId, "絹織物",   GoodTier.Intermediate, 0.0, 0.4, 1.7, [I(SilkId,1.0)], WearRatePerMonth: 0.02),
        new(LeatherId, "革",       GoodTier.Intermediate, 0.0, 0.4, 2.0, [I(LivestockId,0.5), I(TimberId,0.15)], WearRatePerMonth: 0.02),   // タンニンなめし＝畜産(原皮)＋木(樹皮タンニンの抽象)。19世紀前半までの伝統的製革。クロムなめし(1858・少量で速い=効率的)は並存レシピで晩期に駆逐
        new(FlourId, "小麦粉",   GoodTier.Intermediate, 0.0, 0.4, 2.4, [I(WheatId,1.0)], GrainChain: true, WearRatePerMonth: 0.06),   // A-高3：挽き割り・小麦粉も課税客体（ここが抜けると小麦粉輸入→国内製パンで穀物法を迂回できる）
        // ★2026-09-04（作者提案「冷蔵庫という形にして、肉輸送+消費財でよいのでは」）：**生肉は輸送不可**。
        //   ⚠冷凍船（アルゼンチン→仏1877・NZ→ロンドン1882）以前、生肉は海を渡れなかったのに1783年から渡れていた。
        //   ★冷蔵以前の遠距離は既存の2財が担う＝**加工肉**（塩漬け・缶詰＝デンマークのベーコン）と**畜産**（生きた家畜＝アイルランドの牛）。
        //   ⇒ 全面禁止でも「近所の肉屋が止まる」ことはない（地元市場では従来どおり流通する）。冷蔵以後は下の冷蔵肉が担う。
        new(MeatId, "肉",       GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(LivestockId,0.6)], NonTradeable: true, WearRatePerMonth: 0.15),
        new(SugarId, "砂糖",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(SugarCaneId,1.0)], WearRatePerMonth: 0.02),
        new(RubberId, "ゴム",     GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(RawRubberId,1.0), I(SulfurId,0.2)], WearRatePerMonth: 0.02),

        // === 資本財・機械（≤2入力）===
        new(MachineryId, "機械",     GoodTier.Producer,     0.0, 0.4, 1.4, [I(SteelId,1.514), I(CopperId,0.570)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：機械部品（鋼1.0＋銅0.5）・工具（鋼0.6）・精密工作機械（機械0.8＋工具0.5）・発電機を機械1つへ畳む（B-1・B-2）。旧 機械部品1.0＋工具0.5 を鋼・銅へ展開し、消えた2段の付加価値ぶんを比例で上乗せ＝参照価格 4.39 を保つ。精密加工（鋼＋銅＋電力）の時代差のレシピは発見の回（段2A）
        new(SteamEngineId, "蒸気機関", GoodTier.Producer,     0.0, 0.4, 1.4, [I(MachineryId,0.726), I(CoalId,0.4)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品1.0→機械0.726（参照価格の比 3.19÷4.39＝コスト中立）。蒸気機関は残す（作者裁定 Q4＝単独で取引された資本財）
        new(RailId, "線路",     GoodTier.Producer,     0.0, 0.4, 1.8, [I(SteelId,1.0)], WearRatePerMonth: 0.005),

        // === 最終財：消費・医療（≤2入力・入力ペアは固有）===
        new(ClothingId, "衣料",     GoodTier.Daily,        0.30, 0.6, 1.8, [I(CottonClothId,0.6), I(DyeId,0.2)], WearRatePerMonth: 0.02),
        new(LuxuryClothingId, "高級衣料", GoodTier.Luxury,       0.10, 0.9, 1.5, [I(SilkClothId,0.4), I(WoolenClothId,0.3), I(FurId,0.3)], WearRatePerMonth: 0.02),   // 絹＋毛織＋毛皮（裏地/襟巻＝上流の冬季奢侈）。毛皮の最大の吸収先
        new(FurnitureId, "家具",     GoodTier.Daily,        0.16, 0.6, 1.7, [I(LumberId,0.6), I(LeatherId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        new(LuxuryFurnitureId, "高級家具", GoodTier.Luxury,       0.10, 0.9, 1.5, [I(LumberId,0.5), I(SilkClothId,0.3), I(FurId,0.2)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // 製材＋絹＋毛皮（敷物/椅子張り＝富裕邸宅の調度）
        new(PaperId, "紙",       GoodTier.Daily,        0.10, 0.6, 1.8, [I(TimberId,1.404), I(SulfuricAcidId,0.05), I(SodaAshId,0.0406)], WearRatePerMonth: 0.02),   // ★(493) 化学パルプ＝亜硫酸法（酸）とソーダ法・クラフト法（アルカリ）の両方（半々・費用中立）   // 既定＝木材パルプ製紙（解禁の表の onset 0.5）。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：パルプ（木0.8 の素通し）を畳む＝旧 パルプ1.0→木1.404（参照価格の比）
        new(BreadId, "パン",     GoodTier.Staple,       0.55, 0.3, 2.0, [I(FlourId,1.0)], WearRatePerMonth: 0.25),   // 野菜(108)の追加で総食料を概ね不変に保つため 0.80→0.55（差分は野菜へ）
        new(PreservedMeatId, "加工肉",   GoodTier.Daily,        0.12, 0.5, 1.8, [I(MeatId,0.6), I(FlourId,0.2688), I(SaltId,0.05)], Nutrition: 1.0, WearRatePerMonth: 0.06),   // ★(493) G11：塩漬けに塩が無かった。塩 0.05 は小麦粉 0.3→0.2688 で払う（費用中立）   // ★塊1：食卓の動物性タンパク（生肉48は中間財のまま＝塩漬け/干物/缶詰にしてから食べる19世紀）。FoodIncludesAnimalProtein=0では不読
        new(DriedFishId, "干物",     GoodTier.Daily,        0.10, 0.5, 1.9, [I(FishId,0.7), I(SaltId,0.2)], Nutrition: 1.0, WearRatePerMonth: 0.03),   // ★塊1：同上（魚の保存食）
        new(CannedFoodId, "缶詰",     GoodTier.Daily,        0.10, 0.6, 1.7, [I(MeatId,0.6), I(IronId,0.25), I(TinOreId,0.05), I(SaltId,0.2)], WearRatePerMonth: 0.005),   // 肉＋ブリキ(鉄に錫めっき=鉄0.25+錫0.05)＋塩。錫は缶の数%の薄膜＝低係数。錫の最大の出口
        new(BeerId, "ビール",   GoodTier.Daily,        0.12, 0.7, 1.9, [I(WheatId,0.5)], WearRatePerMonth: 0.06),
        new(WineId, "ワイン",   GoodTier.Luxury,       0.12, 0.9, 1.6, [I(FruitId,0.5), I(GlassId,0.2)], WearRatePerMonth: 0.005),
        new(LampOilId, "灯油",     GoodTier.Daily,        0.10, 0.6, 1.9, [I(FishId,0.8)], WearRatePerMonth: 0.005),   // 灯火用の油＝デフォルトは鯨油/魚油(漁獲を精製＝1789の主要灯火油)。石油ケロシン(原油由来・安価)は並存レシピで晩期(1859油井〜)に駆逐＝史実「ケロシンが鯨を救った」
        new(TobaccoId, "煙草",     GoodTier.Daily,        0.08, 0.7, 1.9, [I(TobaccoLeafId,1.0)], WearRatePerMonth: 0.03),
        new(LeatherGoodsId, "革製品",   GoodTier.Daily,        0.08, 0.6, 1.7, [I(LeatherId,0.5), I(FurId,0.2), I(DyeId,0.2)], WearRatePerMonth: 0.005),   // 革＋毛皮(裏地/防寒小物)＋染料。毛皮のDaily緩衝出口（不況時の暴落抑制）
        new(PorcelainId, "陶磁器",   GoodTier.Daily,        0.07, 0.6, 1.7, [I(ClayId,0.6), I(SilicaSandId,0.3)], WearRatePerMonth: 0.005),   // 粘土＋珪砂（長石/珪石）＝窯業の一次製品。旧ガラス(36)投入は誤り（ガラスと陶磁器は別系統）を是正
        new(SoapId, "石鹸",     GoodTier.Daily,        0.08, 0.6, 1.8, [I(PalmOilId,0.5), I(SodaAshId,0.2434)], WearRatePerMonth: 0.005),   // ★(493) 鹸化はアルカリ（苛性ソーダ＝ソーダ灰＋石灰）。量 0.2434＝0.3×旧の化学薬品 1.16÷ソーダ灰 1.43（費用中立）
        new(ChocolateId, "チョコレート", GoodTier.Luxury,   0.05, 0.9, 1.5, [I(CocoaId,0.5), I(SugarId,0.4)], WearRatePerMonth: 0.03),
        new(TeaId, "茶",       GoodTier.Daily,        0.07, 0.8, 2.2, [], true, WearRatePerMonth: 0.03),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：茶葉を畳む（A群・作者裁定 2026-09-29「茶は畳んでよい」）＝茶そのものを植民地の採取（RGO・賦存州だけが産する）に。生産性は茶葉の2.2（茶葉1.0＝茶1.0）
        new(CoffeeId, "コーヒー", GoodTier.Daily,        0.07, 0.8, 2.2, [], true, WearRatePerMonth: 0.03),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：コーヒー豆を畳む（同上）＝コーヒーを植民地の採取に。生産性はコーヒー豆の2.2
        new(WaterproofClothId, "防水布",   GoodTier.Daily,        0.05, 0.7, 1.6, [I(RubberId,0.5), I(CottonClothId,0.4)], WearRatePerMonth: 0.02),
        new(MedicineId, "医薬品",   GoodTier.Daily,        0.108, 0.6, 1.6, [I(SulfuricAcidId,0.25), I(SodaAshId,0.2028), I(FruitId,0.3)], WearRatePerMonth: 0.03),   // ★(493) 酸・塩基・その塩の全般＝両方（半々・費用中立。硫酸 0.25＝0.5×1/2、ソーダ灰 0.2028＝0.25×1.16÷1.43）   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：包帯（必要0.04・綿布0.5）を畳んで必要へ足す（作者裁定 Q8）＝0.08＋0.04×参照価格の比（0.76÷1.07）＝0.108（支出を保つ）
        new(QuinineId, "キニーネ", GoodTier.Daily,        0.04, 0.7, 1.6, [I(CinchonaBarkId,0.5), I(SulfuricAcidId,0.3)], WearRatePerMonth: 0.03),   // ★(493) 製品は硫酸キニーネ＝酸。仮（作者の推奨 2026-10-22＝キニーネは材から消して医薬品の発見へ・死の側の段3 の線の後）   // ★2026-09-30 (129)（作者裁定 2026-09-30＝財の削減 (123) で畳んだ金鶏樹皮を戻す・元の裁定 2026-09-29「加工は畳まない。植民地材→本国に輸送→加工の行程を飛ばすべきではない」）：キニーネは**本国の工場**で樹皮から抽出する（既定のレシピ＝単離・解禁の表の既定の行 0.5＝ペルティエとカヴァントゥ 1820 パリ）。樹皮はアンデスからカディス・ロンドン・アムステルダム（樹皮の競売）へ運ばれ、欧州の工場で硫酸キニーネにした（植民地側の工場はバンドン 1896）。単離の前は代替のレシピ「樹皮の粉末」（Simulation.AddAdvancedProducers）
        new(ApplianceId, "電化製品", GoodTier.Luxury,       0.10, 0.9, 1.3, [I(ElectricityId,0.5), I(MachineryId,0.363), I(CopperId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // 電力＋機械＋銅(巻線/配線/端子)＝電化製品の導体。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.5→機械0.363（コスト中立）

        // === 最終財：軍事・輸送・奢侈（≤2入力）===
        // ※電信機・海底電信は下流（消費先）が無い死蔵財だったため整理（削除）。必要なら将来「電信網インフラ」として固有の効果付きで再導入。
        new(ArtilleryId, "火砲",     GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteelId,1.0), I(MachineryId,0.363)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.5→機械0.363（コスト中立）
        new(RifleId, "小銃",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(SteelId,0.6), I(LumberId,0.4)], WearRatePerMonth: 0.005),
        new(AmmunitionId, "弾薬",     GoodTier.Producer,     0.0,  0.4, 1.5, [I(GunpowderId,0.6), I(LeadId,0.3), I(CopperId,0.2)], WearRatePerMonth: 0.005),   // 火薬＋鉛(弾頭)＋銅(黄銅薬莢)＝金属薬莢実包(1860s〜)。近代小火器の物量が銅供給に律される史実構造
        new(SailingShipId, "帆船",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(LumberId,1.0), I(CanvasId,0.5)], WearRatePerMonth: 0.005),
        new(SteamshipId, "汽船",     GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteamEngineId,0.6), I(LumberId,0.6)], WearRatePerMonth: 0.005),
        new(WarshipId, "軍艦",     GoodTier.Producer,     0.0,  0.4, 1.2, [I(SailingShipId,1.0), I(ArtilleryId,0.5)], WearRatePerMonth: 0.005),
        new(ElectricityId, "電力",     GoodTier.Producer,     0.0,  0.4, 1.4, [I(MachineryId,0.149), I(CoalId,0.9)], NonTradeable: true, WearRatePerMonth: 1.0),   // 石炭火力（既定レシピ）＝燃料主導（A-中1・2026-07-22／02断線走査2026-07-28）：燃料25-40%・資本30-50%＝自国炭の安さが電化に効く。電気は輸送できない＝地産地消。石油火力・水力の並存レシピはビルダーが追加。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：発電機を機械へ畳む（B-2）＝旧 発電機0.15→機械0.149（参照価格の比 4.37÷4.39）
        new(LocomotiveId, "機関車",   GoodTier.Producer,     0.0,  0.4, 1.3, [I(SteamEngineId,0.6), I(SteelId,0.5)], WearRatePerMonth: 0.005),
        new(JewelryId, "宝飾",     GoodTier.Luxury,       0.09, 1.0, 1.4, [I(GoldOreId,0.3), I(SilverId,0.3), I(GlassId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // 金＋銀＋ガラス。金鉱国/銀鉱国の双方に宝飾需要（総量0.6保存＝参照価格不変）
        new(WatchId, "時計",     GoodTier.Luxury,       0.09, 1.0, 1.4, [I(GoldOreId,0.2), I(SilverId,0.2), I(MachineryId,0.290)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // 金側ケース＋銀側ケース＋機械。銀無垢時計＝大衆量産。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.4→機械0.290（コスト中立）
        new(InstrumentId, "楽器",     GoodTier.Luxury,       0.04, 1.0, 1.4, [I(LumberId,0.5), I(IvoryId,0.3)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        // 植民地の固有資源（追加）。アヘン：本国の小さな悪徳消費（嗜好）が当面の消費先。本命は清への交易（将来段＝アヘン戦争）。
        new(OpiumId, "アヘン",   GoodTier.Luxury,       0.02, 0.8, 2.0, [], true, WearRatePerMonth: 0.005),

        // === 役務（サービス財）：POPが算出し、輸送できない（地元市場のみ・NonTradeable）。在庫も持てない（WearRatePerMonth=1.0＝耐久度 d＝0）===
        // 「工場を除き、生産が先にあり、売って収入を得る」設計：労働者→労働力、教師/聖職者→教育、医者→医療、官僚→行政。
        new(LaborId, "労働力",   GoodTier.Service,      0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),   // 工場労働者が算出し工場が買う（賃金＝労働市場の価格）。生産性・担い手はビルダーが設定
        new(EducationId, "教育",     GoodTier.Service,      0.06, 0.8, 2.0, [I(PaperId,0.05)], NonTradeable: true, WearRatePerMonth: 1.0),   // 教師（公教育）/聖職者（宗教学校）が算出。紙を消費（教材＝間接コスト）。識字の源泉
        new(HealthcareId, "医療",     GoodTier.Service,      0.05, 0.8, 3.0, [I(MedicineId,0.10), I(SoapId,0.05)], NonTradeable: true, WearRatePerMonth: 1.0),   // 医者が算出。医薬品＋石鹸（衛生）を消費。人口扶養力に効く
        new(AdminId, "行政",     GoodTier.Service,      0.0,  0.5, 3.0, [I(PaperId,0.10)], NonTradeable: true, WearRatePerMonth: 1.0),   // 官僚が算出し国家が買う。紙を消費（文書行政）。不足＝汚職
        new(MilitaryPowerId, "兵力",     GoodTier.Service,      0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),   // ★(145) 兵力の市場は削除（生産者も国家の需要も無い・兵士の給与は兵士POPの所得へ直接）＝財の番号をずらさないため一覧に残す（下流の無い財の検査は外す＝Validation）

        // === 追加鉱物 ===
        new(ChromeId, "クロム",   GoodTier.Intermediate, 0.0,  0.5, 2.4, [], WearRatePerMonth: 0.005),   // 産地が偏在（オスマン＝アナトリアが世界最大・次いでウラル）。消費は革（クロムなめし並存レシピ・tech0.55〜）のみ＝工具はクロム鋼でなく鋼へ是正済み（02断線走査2026-07-28コメント是正）

        // === 電化インフラ ===
        new(GridId, "送電網",   GoodTier.Producer,     0.0,  0.4, 1.6, [I(CopperId,0.6), I(MachineryId,0.299)], NonTradeable: true, WearRatePerMonth: 1.0),   // 銅線＋変電の機械。輸送不可（地域内の施設）。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 発電機0.3→機械0.299。⚠送電網は畳まない＝投資家が買って送電容量（在庫）を積む資本財・畳み先の電化インフラは国が買う役務（在庫を持てない）＝型が違う（設計 B-2 の後半は保留）

        // === 役務（続き）===
        new(AdvLaborId, "高度労働力", GoodTier.Service,   0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),   // 技師が算出（監督・保守・運転）。全工場が少量・エネルギーを使う工程ほど多く要求。鉄道・船舶の運行も消費
        new(FarmLaborId, "農業労働力", GoodTier.Service,   0.0,  0.5, 1.0, [],          NonTradeable: true, WearRatePerMonth: 1.0),   // 農民が算出し自家農家（RGO）が買う＝農業も工場と同一ロジック。残余の行き先（地主/自作）＝農制

        // === 固執資材（国民的こだわり財・§C 恐慌増幅）===
        // 米：日本の主食（文化財）。水田は単位人手あたりの収量が麦作を上回り（史実≈2倍）、製粉・製パンの
        // 加工段を経ず直接食べる＝連鎖が短く安い＝「麦より高性能」。ただし気候を選ぶ（日本以外は産出僅少）。
        // 主食tier＝不作が産出と自給を直撃（米騒動の素地）。固執財（cultural goods）廃止に伴い**普遍的な主食需要**を付与（旧 BaseNeed 0＝固執需要のみ→死蔵化したため）。
        // 米は世界的に取引された主食＝控えめな普遍需要（野菜0.25より低め・産地〔日本/伊ヴェルチェッリ/サルデーニャ等〕が大量に賄い余剰を輸出）。「安い順」消費で野菜の後・パンと並ぶ。
        new(RiceId, "米",      GoodTier.Staple,       0.20, 0.4, 2.6, [], WearRatePerMonth: 0.06),
        // 蒸留酒：麦から醸造（ロシアのウォッカ等）。万国で小さな日用需要＋固執国（露）は大量に要る。
        // 原料が麦＝不作の年はパンと酒が同時に消える＝凶作が二重に大衆を打つ。
        new(SpiritsId, "蒸留酒",  GoodTier.Daily,        0.05, 0.6, 1.8, [I(WheatId,0.6)], WearRatePerMonth: 0.005),

        // === 上位軍需（高価＝複雑な連鎖だが強い。装備の質的乗数＝Power.Military を増幅。深い入力で自然に工業化を要求＝史実ペーシング）===
        new(MachineGunId, "機関銃",  GoodTier.Producer,     0.0,  0.4, 1.2, [I(SteelId,0.5), I(MachineryId,0.645)], WearRatePerMonth: 0.005),   // 鋼＋機械（マキシム1884）。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 精密工作機械0.6→機械0.645（参照価格の比 4.72÷4.39）。時期は解禁の表（発明の門 0.7）
        new(IroncladId, "装甲艦",  GoodTier.Producer,     0.0,  0.4, 1.1, [I(SteamshipId,1.0), I(SteelId,1.0), I(ArtilleryId,0.5)], WearRatePerMonth: 0.005),   // 汽船(tier2輸送船)＋鋼(装甲)＋火砲＝鉄甲艦（tier1軍艦=帆船、tier2=汽船 の対称。1860s）
        new(CheeseId, "チーズ",   GoodTier.Daily,        0.06, 0.6, 1.8, [I(LivestockId,0.6)], WearRatePerMonth: 0.03),   // 畜産(乳)＝乳製品。蘭の執着財（ゴーダ/エダム）。畜産に新たな加工先を与える

        // === 最も安い食料（RGO・直接消費）===
        // 野菜：加工段を経ず直接食べる＝連鎖の深さ0＝参照価格が最安（パンは加工層ぶん、肉はさらに高い）。
        // 主食tier＝不作に晒される（穀物とは別作物＝飢饉リスクを分散）。同tier内「安い順」消費で、貧者は野菜から埋め、
        // 予算が尽きれば野菜だけ／富裕は野菜＋パン＋肉と多様＝野菜が貧困のシグナルになる。BaseNeedはパンから移管（総食料を概ね不変）。
        new(VegetableId, "野菜",     GoodTier.Staple,       0.25, 0.4, 3.0, [], WearRatePerMonth: 0.2),

        // === RGO効率化材（任意の増強材＝電化と同型。profit-gateで「費用＜増収」を構造的に保証）===
        // 肥料：作物RGO（麦・野菜・果実・綿花・麻・染料・葉煙草・米）の増収。化学薬品＋硫黄（過リン酸/硝酸系）。
        new(FertilizerId, "肥料",     GoodTier.Intermediate, 0.0, 0.4, 1.9, [I(SulfuricAcidId,0.2336), I(LivestockId,0.45)], WearRatePerMonth: 0.005),   // ★(493) G3：過リン酸石灰は骨（燐）を硫酸で溶かす。骨は畜産 0.45・硫酸 0.2336（旧の化学薬品 0.443 の価値 0.514 を保つ＝(0.514−0.45×0.54)÷1.16）   // ★重み＝コスト中立（2026-09-05）：0.35×RefPrice(37)1.16+0.2×RefPrice(5)0.54＝0.514 → 0.514/1.16＝0.443＝肥料の参照価格を旧のまま保つ（7/28較正＝1842年以降に馬を駆逐、を壊さない）。0.55（総重量据え置き）は◎18→13・清70%で差し戻し   // ★E-15b（2026-09-05・史実）：過リン酸石灰は硫酸（＝化学薬品）で作る＝元素硫黄を直接食わない（旧 I(37,0.35)+I(5,0.2)＝硫黄の地理ゲート下で肥料がゲート0の7%に落ちる主因）。入力の総重量0.55は据え置き   // 02断線走査（2026-07-28）：0.5/0.3→0.35/0.2＝参照価格1.07→約0.75で馬0.91を1842年以降に駆逐（型F是正・goods.json同時更新）
        // 飼料：畜産RGOの増産（濃厚飼料＝農業革命の核）。麦を原料＝麦に生産的な吸収先。漁業=帆船・鉱山=工具は既存財を再利用。
        new(FodderId, "飼料",     GoodTier.Intermediate, 0.0, 0.4, 2.2, [I(WheatId,0.6)], WearRatePerMonth: 0.06),
        // 上下水道（#4 施設化）：都市の衛生インフラ＝役務(流通不可)。資材(セメント+鋼)を消費する施設(工場)が産み、国家が行政費で調達。
        // 充足率が衛生＝過密の疫病を抑える（「衛生＝工業化proxy」hack を実財に置換）。労働力+高度労働力は producer 側で追加。
        new(InfrastructureId, "上下水道", GoodTier.Service,      0.0,  0.4, 2.5, [I(CementId,0.4), I(SteelId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),
        // 書籍（#4 学校の上位投入）：紙の一段上（印刷・製本）。公教育の学校が消費（教師＋書籍→教育）＋識字層の消費財（低需要財の出口）。
        new(BookId, "書籍",     GoodTier.Daily,        0.02, 0.6, 2.6, [I(PaperId,0.5)], WearRatePerMonth: 0.005),
        // ガス（石炭ガス）：照明・調理・暖房の日用＋上位インフラ(ガス灯)の素材。石炭から。1810s〜の都市ガス＝電化の前段（電灯に駆逐される）。
        new(GasId, "ガス",     GoodTier.Daily,        0.03, 0.6, 2.2, [I(CoalId,0.6)], WearRatePerMonth: 0.005),
        // 電化インフラ（上位インフラ＝近代都市インフラ・役務・流通不可）：街灯/電車/電動ポンプ。素材は送電網＋電化製品＋鋼（電灯式・既定レシピ）。
        // ガス灯式の並存レシピ（ガス＋鋼）はビルダーが追加＝早期はガス灯、電化が安くなると電灯が駆逐。国家が行政費で調達。満足度＋衛生追加＋送電容量↑。
        new(ElecInfraId, "電化インフラ", GoodTier.Service,   0.0,  0.4, 1.6, [I(GridId,0.4), I(ApplianceId,0.3), I(SteelId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),

        // === #30 Stage2 新材（栽培帯は州 Resources[] で表現＝産地州割当・気候はメタで連続化）===
        // ジャガイモ/トウモロコシ＝**産業作物(Intermediate・麦と同型)** として接続：主食baseNeedを与えると食料系の挙動テスト(聖職者の世俗化没落・距離による交易細り)を撹乱するため、
        // 直接の主食消費でなく**産業原料**＝ジャガイモ→蒸留酒(東欧のウォッカ経済)・トウモロコシ→飼料(畜産)の並存レシピ(ビルダー)で需要を立てる。露地作物なので天候露出1.0。直接食料化は食料系再均衡の別作業。
        // #11 食料再均衡：ジャガイモ/トウモロコシを産業原料から**主食**へ昇格（高単収＝産地で安価に供給→cheapest-firstで貧者の主食→人口扶養力の創発＝アイルランド/独/露の馬鈴薯依存・新大陸/バルカンのトウモロコシ）。
        // 産業原料としての用途（ジャガイモ蒸留酒・トウモロコシ飼料の並存レシピ）は tier 非依存で存続＝食べても醸造/飼料にもなる。普遍 BaseNeed＋産地生産＋cheapest-first で「安い産地で主食化」が創発（米と同型）。
        new(PotatoId, "ジャガイモ", GoodTier.Staple, 0.06, 0.4, 3.2, [], WearRatePerMonth: 0.2),
        new(MaizeId, "トウモロコシ", GoodTier.Staple, 0.04, 0.4, 2.8, [], WearRatePerMonth: 0.06),
        // 錫鉱：コーンウォール/マラヤ/ボリビアの戦略鉱物。缶詰(65)のブリキ（鉄に錫めっき）＝最大の出口。鉱物RGO（枯渇対象）。
        new(TinOreId, "錫鉱",     GoodTier.Intermediate, 0.0, 0.5, 2.4, [], WearRatePerMonth: 0.005),
        // 硝石：ベンガル/チリ硝石。火薬(38)の窒素・酸化剤源＝黒色火薬の主成分（戦略物資・英のベンガル独占）。鉱物RGO。
        new(SaltpeterId, "硝石",     GoodTier.Intermediate, 0.0, 0.5, 2.8, [], WearRatePerMonth: 0.005),
        // 毛皮：露シベリア/加の寒冷地交易品。高級衣料(58)・革製品(70)・高級家具(60)へ。再生RGO（天候依存は漁獲並み0.35）。
        new(FurId, "毛皮",     GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.02),
        // 銀：貴金属(4)を金/銀に分割（4=金へ転用・銀を新設）。銀＝メキシコ/ポトシ/中欧。宝飾(89)・時計(90)の銀器。鉱物RGO。金本位は Bank.Gold（抽象準備）で good参照なし＝分割は宝飾/時計のみに影響。
        new(SilverId, "銀",       GoodTier.Intermediate, 0.0, 0.5, 2.0, [], WearRatePerMonth: 0.005),
        // グアノ：ペルー沖チンチャ諸島の海鳥糞＝19世紀の高窒素肥料の主役(1840s-)。植民地RGO(賦存=ペルーのみ・isColonial)。肥料(109)の並存レシピ「グアノ肥料」で接続＝ペルー保有国のみ安価・他国は合成肥料(化学薬品+硫黄)＝非保有国を壊さない。
        new(GuanoId, "グアノ",   GoodTier.Intermediate, 0.0, 0.6, 2.2, [], true, WearRatePerMonth: 0.03),
        // #16 tier3 軍需（陸=戦車・海=弩級戦艦）：tier2(機関銃104/装甲艦105)を圧倒する質的乗数。解禁＝発見の表の行（2026-10-01 段2A＝戦車1910年代・弩級戦艦1900年代＝旧は連鎖の深さの門）・高RefPrice＝富裕/軍国国のみ装備。
        new(TankId, "戦車",     GoodTier.Producer, 0.0, 0.4, 1.1, [I(SteelId,1.0), I(CombustionEngineId,0.6), I(ArtilleryId,0.4)], WearRatePerMonth: 0.005),   // 鋼＋内燃機関＋火砲＝装軌装甲戦闘車（WWI末）。デ6：精密工作機械(106)を内燃機関(125)へ差し替え＝原動機の連鎖深に律速され1910s代まで解禁されない
        new(DreadnoughtId, "弩級戦艦", GoodTier.Producer, 0.0, 0.4, 1.0, [I(IroncladId,1.0), I(SteelId,1.0), I(MachineryId,0.537)], WearRatePerMonth: 0.005),   // 装甲艦＋鋼(全装甲)＋機械＝超弩級戦艦（最高コスト）。★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 精密工作機械0.5→機械0.537
        // #12 家事奉仕（役務）：家事使用人(ClassId12・女性がなりやすい)が算出し、予算に余裕のある富裕層(貴族/資本家)が雇う。Service tier＝消費は全段の後＝余剰のある層のみ＝「奉仕は富の余剰から創発」（特別な階級フィルタ不要）。輸送不可。
        new(DomesticServiceId, "家事奉仕", GoodTier.Service, 0.06, 0.7, 1.0, [], NonTradeable: true, WearRatePerMonth: 1.0),
        // デ6：内燃機関（石油時代の原動機＝ガソリン/ディーゼル機関・実用化1900s〜）。鋼＋精密工作機械＋原油（石油燃料系＝ドレーク油井1859の下流）。
        // 戦車(122)の入力に差し込む（上）ことで、戦車が内燃機関の深い連鎖に律速され WWI 期(1910s)まで解禁されない＝機関銃(1884)/装甲艦(1860)と同時解禁だった時代錯誤を構造から是正（★2026-10-01 段2A：時期は発見の表の年代・内燃機関を作っていない国は技術の押しが下がる）。id は末尾に追加（RefPriceは反復収束のため上流参照でも可）＝既存idの連番を崩さない。
        new(CombustionEngineId, "内燃機関", GoodTier.Producer, 0.0, 0.4, 1.1, [I(SteelId,1.0), I(MachineryId,0.5616)], WearRatePerMonth: 0.005),   // ★(493) G10：原油は機関の材料でなく燃料（燃料は使い手の側に既にある）。外した分は機械へ費用中立（0.537＋0.2×0.54÷4.394）   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 精密工作機械0.5→機械0.537
        // 住居（家賃＝家計の最大固定費・生活費テーマの核心）：2026-07-19ユーザー裁定「家を材にする」＝役務→**通常財（日用品）**。
        // 製材＋石灰（1783年から生産可能な建材＝木造漆喰）を消費する建設業が供給＝非役務財の標準生産者ループに乗る
        // （全州に生産者・工場建設/補助金/生産タブのグラフ・価格が上がれば供給が伸びる＝住宅不足が市場で自律解消）。
        // 需要は EconomyStep の特例（都市pop×HousingNeedPerUrban・都市率45%超ゲート）のまま。輸送不可＝地産地消（家は輸出入できない）。
        new(HousingId, "住居", GoodTier.Daily, 0.0, 0.4, 1.2, [I(LumberId,0.5), I(LimestoneId,0.2)], NonTradeable: true, WearRatePerMonth: 0.005),
        // === B6新財（2026-07-19ユーザー裁定）＝id末尾追加（RefPrice反復収束で安全・連番不変）===
        // 馬：畜産から育成する上位財（飼料＝穀物消費）。用途＝軍需バンドルの馬匹（騎兵・輜重）＋作物RGOの馬耕ブースター
        // （肥料はonset0.6=1842解禁＝序盤の作物ブースター空白を馬耕が埋め、晩期に安い化学肥料へ駆逐される転換が創発）。
        // ★2026-10-01 段2A：化学法の解禁は発見の表の過リン酸石灰（1840年代・2値）。⚠ 種1 の試走では誰も発見しない（堆肥・グアノより割高＝割高さが0以下）＝申し送り。
        new(HorseId, "馬",       GoodTier.Intermediate, 0.0, 0.5, 1.6, [I(LivestockId,0.6), I(FodderId,0.4)], WearRatePerMonth: 0.04),
        // ダイナマイト（ノーベル1867・発見の表の1860年代）：鉱山発破の第3ブースター候補＝工具/火薬より晩期に安い爆薬（無煙火薬0.85より早い）。
        // 化学薬品0.5→0.4（B6レビュー2026-07-19）：0.5ではRefPrice0.96で黒色火薬0.89に負け競争窓がほぼ無かった＝0.4で1867〜の勝ち筋を開く。
        new(DynamiteId, "ダイナマイト", GoodTier.Intermediate, 0.0, 0.5, 1.4, [I(SulfuricAcidId,0.4), I(SaltpeterId,0.1)], WearRatePerMonth: 0.005),   // ★(493) 混酸（硝酸＋硫酸）でニトログリセリン＝酸
        // 自転車（安全型1885〜・発見の表の1880年代）：労働者の移動手段＝日用財。ゴム(50・加硫1839ゲート)に民需の出口を与える。
        new(BicycleId, "自転車",   GoodTier.Daily, 0.04, 0.5, 1.2, [I(SteelId,0.5), I(RubberId,0.3), I(MachineryId,0.218)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.3→機械0.218
        // === 1934拡張E2（2026-07-19ユーザー裁定）＝戦間期の時代財。id末尾追加（RefPrice反復収束で安全・連番不変）===
        // 飛行機（木骨羽布張り＝1910s・発見の表の軍用機1910年代）：内燃機関＋製材＋防水布。空軍調達（偵察/輸送）＋戦闘機の機体。
        new(MilitaryAircraftId, "軍用機",   GoodTier.Producer, 0.0, 0.4, 1.16, [I(CombustionEngineId,0.567), I(LumberId,0.284), I(WaterproofClothId,0.189), I(MachineGunId,0.123)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：飛行機（偵察・輸送）と戦闘機（飛行機0.8＋機関銃0.3）を軍用機1つへ（B-3・作者裁定 Q5）＝調達の比（0.03：0.02）で投入を合わせ、参照価格は加重平均（4.18）を保つ。全金属機の時代差のレシピは段2A
        // 戦闘機（同調機銃＝1915フォッカー・onset=1.0）：飛行機＋機関銃。空軍tierの主装備。（★onset は旧＝2026-10-01 段2A から解禁は発見の表の行）
        // 自動車（T型量産1908〜・旧 生産onset=0.95/需要onset=1.0＝2026-10-01 段2A から発見の表の1890年代・需要は世界の解禁の和）：内燃機関＋鋼＋ゴム。富裕の耐久財＋軍の輜重（馬と併存＝WWIも馬匹依存）。
        new(AutomobileId, "自動車",   GoodTier.Luxury, 0.05, 0.9, 1.2, [I(CombustionEngineId,0.6), I(SteelId,0.4), I(RubberId,0.2)], WearRatePerMonth: 0.005, Use: GoodUse.Held),
        // 化学兵器（毒ガス弾＝1915イーペル・旧 onset=1.0＝2026-10-01 段2A から発見の表の1910年代〔陸〕）：化学薬品＋硝石。空軍tierで調達＝相互保有は厭戦の泥沼・片側保有は戦死優位。
        new(ChemicalWeaponId, "化学兵器", GoodTier.Producer, 0.0, 0.4, 1.2, [I(SaltId,0.7), I(ElectricityId,0.2025), I(SaltpeterId,0.2)], WearRatePerMonth: 0.005),   // ★(493) G8：塩素は食塩電解（塩＋電力）から。硫酸にもソーダ灰にも塩素は無い。費用中立（0.6×1.16＝0.7×0.54＋0.2025×1.571）
        // === 1934拡張E3（2026-07-19）＝国民意識の財化 ===
        // 新聞：紙＋機械部品（手引き印刷機は1783から・パルプ紙0.5で安価化）。需要は識字比例（EconomyStep特例）＝
        // 「読める者だけが新聞を買い結社に集う」（旧Fiscal需要駆動拠出の物理化）。NewsFill→国民意識＋政治意識（検閲で内容が痩せる）。
        new(NewspaperId, "新聞",     GoodTier.Daily, 0.06, 0.6, 1.8, [I(PaperId,0.6), I(MachineryId,0.036)], WearRatePerMonth: 0.005),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.05→機械0.036
        // ラジオ（受信機・1920s〜）：電化製品＋機械部品。識字不要＝文盲にも届く国民精神の増幅器（検閲の影響を受けない＝国営宣伝の物理）。
        new(RadioId, "ラジオ",   GoodTier.Daily, 0.03, 0.6, 1.4, [I(ApplianceId,0.6), I(MachineryId,0.145)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 機械部品0.2→機械0.145
        // 高等教育（役務・2026-08-17 作者裁定「近代化投資は研究者や技師を増やす補助金として／まず高等教育から順に」）：
        // 大学が算出し、**教育を既に買ったpopがさらに自費で買う**ことでのみ技師・研究者へ昇格する（無償化されない＝作者裁定2026-08-14）。
        // 投入＝書籍のみ。★ガラスは史実検討で却下（2026-08-17）：英はガラス税1746-1845で自国産業を1世紀抑圧しながら産業革命を達成し、
        // ボヘミア/ヴェネツィアは最高級のガラスを持ちながら工業化せず、そもそもガラスはソーダ灰（ルブラン1791/ソルベー1863）に依存する
        // 化学工業の**下流**＝因果が逆。加えて1783年の大学は神学・法学・医学・哲学で実験をしない（学生実験室はリービッヒのギーセン1824が最初）。
        // BaseNeed=0＝popの自発需要は立てない（幻需要を作らない）。生産者は段①-2で追加＝この時点では誰も作らず誰も買わない。
        new(HigherEducationId, "高等教育", GoodTier.Service, 0.0, 0.8, 3.0, [I(BookId,0.4)], NonTradeable: true, WearRatePerMonth: 1.0),
        // ★冷蔵（2026-09-04・作者提案）＝1つの財が2つ働く：**消費財**であり、同時に**生肉の遠距離輸送を開く**。
        //   ⚠入力を精密工作機械にするのは史実（圧縮式冷凍機は高精度の圧縮機が要る＝リンデ1876）であると同時に、
        //   **解禁時期を連鎖の深さから自動で出す**ため（浅い連鎖だと FreeDepth=6 に収まって自動ゲートが不発になる既知の穴）。
        //   ⚠電力を入力にしない＝初期の冷凍機は**蒸気駆動**。電力を入れると家庭用電気冷蔵庫（1920s）まで50年遅れる。
        new(RefrigeratorId, "冷蔵庫",   GoodTier.Luxury, 0.06, 0.9, 1.3, [I(MachineryId,0.430), I(CoalId,0.6444)], WearRatePerMonth: 0.005, Use: GoodUse.Held),   // ★(493) G9：冷媒のアンモニア（リンデ 1876）は塩基でガス液＝石炭から。費用中立（0.3×1.16÷0.54）   // ★段1（2026-09-30・財の削減＝設計 docs/202609/20260928 §3）：旧 精密工作機械0.4→機械0.430
        // ⚠冷蔵肉という財は**作らない**（2026-09-04・作者指摘で撤回）。
        //   ★代わりに**加工肉の代替レシピ**（肉＋冷蔵庫）を足す＝新しい財を増やさず、
        //   加工肉の需要は既に較正済みのまま使える。⇒ 実装は Simulation の代替レシピ注入（石油灯油と同じ作法）。
        //   ★副産物＝塩漬け（肉＋小麦粉）の設備が**座礁する**＝「乗り換えるか・捨てられる側が抵抗するか」の最初の実例になる。
        // === 2026-09-30 (129) 戻した財（作者裁定 2026-09-30＝財の削減 (123) で畳んだ金鶏樹皮を戻す）＝id末尾追加（既存の財の番号は変えない・B6 と E2 の前例）===
        // 金鶏樹皮：アンデス（ペルー・ボリビア・エクアドル）の森で剥いだキナの樹皮＝植民地の採取（気候ゲート）。本国へ運ばれ、欧州の工場でキニーネにした
        //   （元の裁定 2026-09-29「加工は畳まない。植民地材→本国に輸送→加工の行程を飛ばすべきではない」）。値は畳む前と同じ（生産性2.0・弾力0.6）。
        new(CinchonaBarkId, "金鶏樹皮", GoodTier.Intermediate, 0.0,  0.6, 2.0, [], true, WearRatePerMonth: 0.03),
        // === 2026-10-23 (493) 化学薬品を酸とアルカリに割った（作者 2026-10-21〜23「「硫酸」と「ソーダ灰」は2つに分けましょう。少し強引に見えます。」・材の粒度の最低ライン＝酸の材料からアルカリが生産されてはならない）＝id 末尾追加 ===
        // ソーダ灰：既定＝草木灰・海藻灰（木 2.0＝歩留まりの低い高価な灰。参照価格 1.43）。ルブラン法（塩＋硫酸＋石灰石＋石炭）・ソルベー法（塩＋石灰石＋石炭）・電解（塩＋電力）は発見の行の並存レシピ（Simulation）。
        //   順序（番人）：草木灰 1.430 ＞ ルブラン法 1.207 ＞ 電解 1.091 ＞ ソルベー法 0.971（参照価格の単位費用）。量は AI の推奨（measurements/gd_20261023_prereg.txt [2][11]）。
        new(SodaAshId, "ソーダ灰", GoodTier.Intermediate, 0.0, 0.4, 1.8, [I(TimberId,2.0)], WearRatePerMonth: 0.03),
    ];

    /// <summary>真実源＝埋め込みリソース <c>goods.json</c>（world.json と同型）。未生成時は上の C# 定義 <see cref="BuiltinGoods"/> にフォールバック。
    /// 材追加・レシピ編集は goods.json を直接編集→ビルドで反映（gen_world と同じ運用）。RefPrices はこの配列から自動派生。
    /// ※静的初期化順＝BuiltinGoods → Goods → RefPrices（textual order）。Goods は BuiltinGoods の後に置くこと。</summary>
    public static readonly GoodDef[] Goods = LoadGoods();

    /// <summary>RGOの効率化材（任意の増強材）の候補。市場が安い・profit-gateを通る物を毎期選ぶ（並存レシピと同じ「安い方を採る」思想）。
    /// 作物→肥料／漁業→帆船／畜産→飼料／鉱山→工具 or 火薬。空＝効率化材なし（工場・役務）。
    /// 電化(電力)のRGO版＝買えた率に比例して産出が増える。profit-gate（増収＞費用の時だけ買う）で消費財が産出物より高くならない。</summary>
    public static int[] RgoBoosterGoods(int id) => id switch
    {
        CottonId or HempId or WheatId or FruitId or DyeId or TobaccoLeafId or RiceId or VegetableId or PotatoId or MaizeId => [FertilizerId, HorseId],  // 作物 → 肥料 or 馬（馬耕・B6）。肥料はonset0.6=1842までゲート＝序盤は馬耕・晩期は安い化学肥料が駆逐（02断線走査2026-07-28：肥料の投入を軽量化＝旧・馬0.91<肥料1.07で駆逐が一度も起きない型Fを是正）
        FishId => [SailingShipId],                                                                  // 漁業（漁獲）→ 帆船（漁船団）
        LivestockId => [FodderId],                                                                 // 畜産 → 飼料
        IronOreId or CoalId or CopperOreId or LeadOreId or GoldOreId or SulfurId or LimestoneId or SilicaSandId or ClayId or CrudeOilId or ChromeId or TinOreId or SaltpeterId or SilverId => [IronId, DynamiteId],   // ★段1（2026-09-30・財の削減）：工具（鋼0.6 の素通し・参照価格1.63）を畳んだ＝鉱山の増強材は鉄（1.47＝鉄の工具＝つるはし・楔。機械 4.39 では割に合う条件 3.75×産出の価格を越え、1867年まで鉱山が増強できなくなる）。鉱山（＋錫・硝石・銀）→ 工具 or ダイナマイト（1867解禁・B6）。02断線走査（2026-07-28）：火薬38を外す＝工具1.63>火薬0.89で工具が一度も選ばれない型Fの是正（1789-1867は工具・以後ダイナマイトが駆逐＝B6の勝ち筋が実際に発火）。効率化は最低優先配給
        _ => [],
    };

    /// <summary>各一次産品の**天候依存度**∈[0,1]＝産出のうち露天の生物/気候収量に晒され不作(HarvestFactor)で振れる割合。
    /// 離散3値表(=0.4等)でなく各財固有の連続データ＝個別に調整可能・新RGOは自分の値を持つ。
    /// 露地作物(麦・果実・染料・葉煙草・綿花・麻・米・野菜)=1.0（全面露天）／畜産=0.45（飼料＝穀物高騰＋疫病で land harvest と強く連動）／
    /// 漁獲=0.35（不漁はあるが洋上＝land harvest から相対的に独立）／屋内養蚕・鉱物・製造・役務=0（天候非依存）。</summary>
    public static double WeatherExposure(int id) => id switch
    {
        CottonId or HempId or WheatId or FruitId or DyeId or TobaccoLeafId or RiceId or VegetableId or PotatoId or MaizeId => 1.0,   // 露地作物（綿花・麻・麦・果実・染料・葉煙草・米・野菜・ジャガイモ・トウモロコシ）
        // デ8：熱帯露地作物（サトウキビ25・カカオ26・茶葉27・コーヒー豆28・アヘン92＝ケシ露地）＝カリブのハリケーン・干魃・多雨に晒される露地栽培。0.8で高露出（完全露天の主食1.0よりやや低い＝多年生樹木は単年の天候に相対的に鈍い）。
        SugarCaneId or CocoaId or TeaId or CoffeeId or OpiumId => 0.8,   // ★段1（2026-09-30・財の削減）：茶葉・コーヒー豆→茶・コーヒー（採取の財へ畳んだ）
        // デ8：熱帯樹木プランテーション（生ゴム21＝パラゴムノキ樹液・パーム油23＝アブラヤシ）＝樹木は単年の天候に相対的に鈍いが露地。0.6で中露出。
        RawRubberId or PalmOilId => 0.6,
        LivestockId => 0.45,                                       // 畜産
        FishId or FurId => 0.35,                                // 漁獲・毛皮（野生個体数の変動・地表作物の不作からは相対的に独立）
        _ => 0.0,                                         // 屋内養蚕・鉱物・製造・役務
    };

    /// <summary>希少鉱物（2026-08-13・doc 20260812_財の3分類と地理ゲート／作者裁定⑥）＝**産地でしか採れない**財。
    /// 硫黄5・金4・銀120・クロム98・錫鉱117・硝石118 の6財。原油は対象外（裁定⑥）。
    /// 石炭・鉄のような遍在鉱物は含めない＝「どこでも少しは採れる」のが正しいため。</summary>
    /// ★測定用に対象を絞れる（作者裁定⑥「硫黄1財だけ単独で測る」）。ScarceMineralOnlySulfur>0 で硫黄5のみ。
    public static bool IsScarceMineral(int id) => id is GoldOreId or SulfurId or ChromeId or TinOreId or SaltpeterId or SilverId;
    /// ★段階（2026-09-06・G2 の実測）：1＝硫黄だけ／2＝硫黄＋硝石＋金＋銀（合金金属のクロム98・錫117を除く＝下流（合金鋼・ブリキ）が運搬不能で死に
    ///   ハーネス「下流の無い財」が2/10で赤）／0＝6鉱物すべて。クロム・錫は「運ばれる経路」か「劣った代替」を用意してから。
    public static bool IsScarceMineral(int id, Coefficients c)
        => c.ScarceMineralOnlySulfur >= 4 ? id is SulfurId or SilverId                 // 4＝硫黄＋銀（作者裁定2026-09-07 銀の暴落＝銀だけ産地化。金・合金金属は広いまま）
         : c.ScarceMineralOnlySulfur >= 3 ? id is SulfurId or SaltpeterId                 // 3＝硫黄＋硝石（貨幣金属を除く＝G2b で小国が勝つ挙動の切り分け）
         : c.ScarceMineralOnlySulfur >= 2 ? id is GoldOreId or SulfurId or SaltpeterId or SilverId     // 2＝＋金・銀（合金金属を除く）
         : c.ScarceMineralOnlySulfur > 0 ? id == SulfurId : IsScarceMineral(id);

    /// <summary>鉱物・採掘RGO（枯渇イベントの対象）。鉄鉱石・石炭・銅鉱・鉛鉱・金・硫黄・石灰石・珪砂・粘土・原油・クロム・錫鉱・硝石・銀。</summary>
    public static bool IsMineral(int id) => id is IronOreId or CoalId or CopperOreId or LeadOreId or GoldOreId or SulfurId or LimestoneId or SilicaSandId or ClayId or CrudeOilId or ChromeId or TinOreId or SaltpeterId or SilverId;

    /// <summary>RGO の施設名（プランテーションdoc D2・2026-07-30／★採取形態の是正 2026-08-10）。
    /// ★ホワイトリストではなく**採取形態ごとの腕＋既定**＝新しい財を足しても分類が自動で付く。電力の「石炭火力／石油火力／水力発電」と同じく
    /// 同一 <see cref="Producer"/> の <c>RecipeName</c> を変えるだけで、新フィールドも enum も要らない。
    /// 自家農家（誰も建てていない RGO）と大規模農場（建設動詞で建てたもの）の違いは**建てたかどうか**（BuiltScale）であって名前ではない。
    /// ⚠2026-08-10 の是正：旧＝鉱物と畜産以外は**全部プランテーション**で、漁獲が「プランテーション」、木・毛皮・象牙・生糸・グアノ、
    ///   さらに欧州の麦・ジャガイモまでもがプランテーションになっていた。採取形態（掘る／獲る／伐る／飼う／育てる）で分ける。
    /// ⚠2026-08-10 の作者指示：栽培の施設名は熱帯・温帯とも「農場」1本（「プランテーション」の表示は廃止）。
    ///   good id も気候ゲート <see cref="IsColonial"/> も据え置き＝変えたのは画面に出る語だけ。</summary>
    public static string RgoFacilityName(int id) => id switch
    {
        _ when IsMineral(id) => "鉱山",           // 鉄鉱石・石炭・原油ほか＝掘る
        GuanoId => "採掘場",                       // 島の堆積を掘る（栽培ではない）
        LivestockId => "牧場",                     // 飼う
        FishId => "漁場",                          // 獲る（洋上）
        FurId or IvoryId => "猟場",                // 獲る（陸上の野生個体＝罠猟・象狩り）
        TimberId => "伐採場",                      // 伐る
        SilkId => "養蚕場",                        // 屋内で育てる（露地の畑ではない）
        _ => "農場",                                // 育てる（熱帯の換金作物も温帯の露地作物・主食もこの1語）
    };

    /// <summary>病虫害に弱い作物（年次の胴枯れの対象＝作物固有の不作）。麦(さび病)・果実/葡萄(フィロキセラ)・米・ジャガイモ(胴枯病1845)・トウモロコシ・コーヒー(さび病1869)。
    /// 病害は good id 別にロールし、栽培州（賦存）で局地飢饉を起こす（地域独立ロールでなく作物単位＝空間相関が栽培地から創発）。
    /// デ8：コーヒー豆(28)にコーヒーさび病(Hemileia vastatrix・1869セイロン)を追加＝セイロンを茶へ転換させた大事件。病害は栽培州（コーヒー賦存州）でのみロール＝合成食料(id0)は非対象で較正中立。</summary>
    public static bool IsDiseaseProneCrop(int id) => id is WheatId or FruitId or RiceId or PotatoId or MaizeId or CoffeeId;   // ★段1（2026-09-30・財の削減）：コーヒー豆→コーヒー

    /// <summary>気候ゲート財（旧IsColonial＝熱帯/特定産地のみ・賦存州でのみ産する）。生ゴム・金鶏樹皮・パーム油・象牙・サトウキビ・カカオ・茶・コーヒー・香辛料・アヘン・グアノ。
    /// 287州世界では「植民地限定の輸入専用」でなく「賦存州（州が Resources に列挙）が産み、非賦存州は産めない」＝地理から供給の偏在が創発する（茶＝中国・ゴム＝東南ア…）。</summary>
    public static bool IsColonial(int id) => id is RawRubberId or PalmOilId or IvoryId or SugarCaneId or CocoaId or SpiceId or OpiumId or GuanoId or TeaId or CoffeeId or CinchonaBarkId;   // ★段1（2026-09-30・財の削減）：茶葉・コーヒー豆を畳んで 茶・コーヒーが採取の財に。★2026-09-30 (129)：金鶏樹皮を戻した＝採取の財は樹皮（キニーネは本国の工場）   // ＋グアノ（ペルー賦存の植民地RGO）

    // 役務財の good.Id（ビルダー・経済ステップ・UI が参照）
    public const int LaborId = 84;
    public const int EducationId = 85;
    public const int HigherEducationId = 125;  // 高等教育（役務・2026-08-17）：大学が算出。教育を買ったpopがさらに買うことでのみ技師・研究者へ昇格（無償化されない）
    public const int HealthcareId = 86;
    public const int AdminId = 87;
    public const int InfrastructureId = 101;   // 上下水道（#4 施設化＝都市の衛生インフラ・国家が行政費で調達）
    public const int BookId = 102;             // 書籍（#4 学校の上位投入＝紙の一段上。公教育の学校が消費）
    public const int GasId = 103;              // ガス（石炭ガス＝照明/調理の日用＋上位インフラ(ガス灯)の素材）
    public const int ElecInfraId = 104;        // 電化インフラ（上位インフラ＝近代都市インフラ・国家が行政費で調達。満足度＋衛生＋送電容量）
    public const int HousingId = 116;          // 都市住居（housing-urban-rent-absent）：NonTradeableな役務的必需。家賃が家計の最大固定費。**materialize済み**＝goods.jsonは136財でid126実在・HousingNeedPerUrban=0.03点火中＝都市国で家賃が家計に乗っている（02断線走査2026-07-28：陳腐化した「未materialize」注記を是正）
    public const int NewspaperId = 123;        // 新聞（1934拡張E3）：需要＝識字比例（EconomyStep特例）。NewsFill→国民意識＋政治意識（検閲で内容が痩せる）
    public const int RadioId = 124;            // ラジオ（E3・1920s〜）：識字不要の国民精神増幅器（検閲非依存＝国営宣伝）。RadioFill→国民意識
    public const int MilitaryPowerId = 88;
    public const int GridId = 90;
    public const int AdvLaborId = 91;
    public const int FarmLaborId = 92;
    public const int ElectricityId = 78;   // 電力（NonTradeable）。決済・revenue集計に絡むので生整数でなく名前付き定数で参照（貨幣保存の取りこぼし防止）
    public const int GoldOreId = 4;        // 金鉱（RGO）。産出が貨幣供給(Bank.Gold)に流入＝ゴールドラッシュ
    public const int SilverId = 110;       // 銀（銀本位doc G3・2026-08-13）。銀本位国の造幣局が買う本位金属＝法が変われば需要が現れ・消える
    public const int CoalId = 1;           // 石炭（RGO）。蒸気動力の燃料（蒸気→電力で代替）＝発明後(SteamScale>0)に蒸気化した工場が EnergyPerOutput で消費（電化と同型）
    public const int DomesticServiceId = 114;  // 家事奉仕（役務）。家事使用人(ClassId12)が算出・富裕層が消費（#12）
    // マジックナンバー一掃2026-07-20：各所に散在していた裸の good-id リテラルを命名（値は据え置き＝挙動不変）。
    public const int LivestockId = 19;         // 畜産（RGO・牧場）。派生需要のみ（BaseNeed=0）＝羊毛/革/肉/チーズ/馬の上流
    public const int FishId = 20;              // 漁獲（RGO・土地を占めない＝囲い込みの罠が働かない唯一のRGO）
    // ★RGOの施設名（2026-08-10）で要る採取形態の id（<see cref="RgoFacilityName"/>）＝作物でない一次産品
    public const int TimberId = 11;            // 木（伐採＝森林から採る。植えて育てるものではない）
    public const int SilkId = 14;              // 生糸（屋内養蚕＝露地の畑ではない。WeatherExposure も0＝天候非依存）
    public const int IvoryId = 23;             // 象牙（野生個体を獲る＝狩猟。栽培でない）
    public const int GuanoId = 111;            // グアノ（島の堆積を掘る＝チンチャ諸島の採掘。栽培でない）
    public const int FurId = 109;              // 毛皮（罠猟＝野生個体。WeatherExposure が漁獲と同じ0.35なのも同じ理由）
    public const int SaltId = 6;               // 塩（王室のgabelle＝法的独占の対象）
    public const int TeaId = 66;               // 茶（東インド会社独占＝法的独占の対象）
    public const int SulfuricAcidId = 33;      // 硫酸（★(493)：旧 化学薬品 を酸とアルカリに割った＝酸の側が 33 を引き継ぐ。作者 2026-10-21〜23「「硫酸」と「ソーダ灰」は2つに分けましょう。」）
    public const int CrudeOilId = 10;          // 原油（掘削1859〜）
    public const int WheatId = 15;             // 麦（乾燥穀粒）
    public const int RailId = 48;              // 線路（鉄道の軌条）
    public const int CannedFoodId = 57;        // 缶詰（アペール法1809〜）
    public const int QuinineId = 70;           // キニーネ（金鶏樹皮1820〜）
    public const int SailingShipId = 75;       // 帆船
    public const int SteamshipId = 76;         // 汽船
    public const int LocomotiveId = 79;        // 機関車
    public const int RiceId = 93;             // 米（乾燥穀粒・気候帯主食）
    public const int VegetableId = 98;        // 野菜
    public const int FertilizerId = 99;       // 肥料（過リン酸石灰1842〜）
    public const int PotatoId = 105;           // ジャガイモ（気候帯主食）
    public const int MaizeId = 106;            // トウモロコシ（気候帯主食）
    public const int HorseId = 117;            // 馬（生体在庫）
    // ★段0-1（2026-09-30・財の削減の回）：残りの財 id もすべて名前付きの定数にする＝数値の財 id をコードに書かない（番人＝WriterRatchetTests の「数値の財 id」）。
    //   財を畳む・振り直すときはここ（と上の既存の定数）の値だけを直す。値は goods.json の id と同じ（名前は goods.json の name）。
    public const int IronOreId = 0;              // 鉄鉱石
    public const int CopperOreId = 2;            // 銅鉱
    public const int LeadOreId = 3;              // 鉛鉱
    public const int SulfurId = 5;               // 硫黄
    public const int LimestoneId = 7;            // 石灰石
    public const int SilicaSandId = 8;           // 珪砂
    public const int ClayId = 9;                 // 粘土
    public const int CottonId = 12;              // 綿花
    public const int HempId = 13;                // 麻
    public const int FruitId = 16;               // 果実
    public const int DyeId = 17;                 // 染料
    public const int TobaccoLeafId = 18;         // 葉煙草
    public const int RawRubberId = 21;           // 生ゴム
    public const int PalmOilId = 22;             // パーム油
    public const int SugarCaneId = 24;           // サトウキビ
    public const int CocoaId = 25;               // カカオ
    public const int SpiceId = 26;               // 香辛料
    public const int IronId = 27;                // 鉄
    public const int SteelId = 28;               // 鋼
    public const int CopperId = 29;              // 銅
    public const int LeadId = 30;                // 鉛
    public const int CementId = 31;              // セメント
    public const int GlassId = 32;               // ガラス
    public const int GunpowderId = 34;           // 火薬
    public const int LumberId = 35;              // 製材
    public const int CottonClothId = 36;         // 綿布
    public const int CanvasId = 37;              // 帆布
    public const int WoolId = 38;                // 羊毛
    public const int WoolenClothId = 39;         // 毛織物
    public const int SilkClothId = 40;           // 絹織物
    public const int LeatherId = 41;             // 革
    public const int FlourId = 42;               // 小麦粉
    public const int MeatId = 43;                // 肉
    public const int SugarId = 44;               // 砂糖
    public const int RubberId = 45;              // ゴム
    public const int MachineryId = 46;           // 機械
    public const int SteamEngineId = 47;         // 蒸気機関
    public const int ClothingId = 49;            // 衣料
    public const int LuxuryClothingId = 50;      // 高級衣料
    public const int FurnitureId = 51;           // 家具
    public const int LuxuryFurnitureId = 52;     // 高級家具
    public const int PaperId = 53;               // 紙
    public const int BreadId = 54;               // パン
    public const int PreservedMeatId = 55;       // 加工肉
    public const int DriedFishId = 56;           // 干物
    public const int BeerId = 58;                // ビール
    public const int WineId = 59;                // ワイン
    public const int LampOilId = 60;             // 灯油
    public const int TobaccoId = 61;             // 煙草
    public const int LeatherGoodsId = 62;        // 革製品
    public const int PorcelainId = 63;           // 陶磁器
    public const int SoapId = 64;                // 石鹸
    public const int ChocolateId = 65;           // チョコレート
    public const int CoffeeId = 67;              // コーヒー
    public const int WaterproofClothId = 68;     // 防水布
    public const int MedicineId = 69;            // 医薬品
    public const int ApplianceId = 71;           // 電化製品
    public const int ArtilleryId = 72;           // 火砲
    public const int RifleId = 73;               // 小銃
    public const int AmmunitionId = 74;          // 弾薬
    public const int WarshipId = 77;             // 軍艦
    public const int JewelryId = 80;             // 宝飾
    public const int WatchId = 81;               // 時計
    public const int InstrumentId = 82;          // 楽器
    public const int OpiumId = 83;               // アヘン
    public const int ChromeId = 89;              // クロム
    public const int SpiritsId = 94;            // 蒸留酒
    public const int MachineGunId = 95;         // 機関銃
    public const int IroncladId = 96;           // 装甲艦
    public const int CheeseId = 97;             // チーズ
    public const int FodderId = 100;             // 飼料
    public const int TinOreId = 107;             // 錫鉱
    public const int SaltpeterId = 108;          // 硝石
    public const int TankId = 112;               // 戦車
    public const int DreadnoughtId = 113;        // 弩級戦艦
    public const int CombustionEngineId = 115;   // 内燃機関
    public const int DynamiteId = 118;           // ダイナマイト
    public const int BicycleId = 119;            // 自転車
    public const int MilitaryAircraftId = 120;   // 軍用機（★段1：飛行機と戦闘機を畳んだ＝旧 飛行機の番号を継ぐ）
    public const int AutomobileId = 121;         // 自動車
    public const int ChemicalWeaponId = 122;     // 化学兵器
    public const int RefrigeratorId = 126;       // 冷蔵庫
    public const int SodaAshId = 128;          // ソーダ灰（★(493)：旧 化学薬品 のアルカリの側＝末尾に足した新しい財・既存の財の番号は変えない。草木灰・ルブラン法・ソルベー法・電解）
    public const int CinchonaBarkId = 127;       // 金鶏樹皮（★2026-09-30 (129)：財の削減 (123) で畳んだのを戻した＝id は末尾に足した＝既存の財の番号は変えない）

    /// <summary>役務の参照価格（費用基準）。在庫を持てない純フロー市場は価格が発振しやすいので、
    /// 費用に錨を置き値幅を狭く取る（EconomyStep が ±3倍程度に制限）＝役務popの収入が暴落・暴騰で壊れない。</summary>
    public static double ServiceRefPrice(int id) => id switch
    {
        LaborId or MilitaryPowerId or FarmLaborId => 0.5,   // 労働は一律の錨。農村が安い・技師が高い等の**格差は需給から創発**させる（値幅は発振防止のみ）
        AdvLaborId => 0.8,                 // 技師の役務は高い（希少な技能）
        _ => 0.6,
    };

    /// <summary>工場の産出1単位あたりの高度労働力（技師の監督・保守・運転）。
    /// **石炭や電力を使う工程ほど技師の比率が上がる**＝機械は技師がいないと回らない（エネルギー集約度の勾配）。</summary>
    public static double AdvLaborPerOutput(int id) => id switch
    {
        ElectricityId => 0.15,                                              // 発電所＝技師の塊
        MachineryId or SteamEngineId or ApplianceId or SteamshipId or WarshipId or LocomotiveId or GridId or MachineGunId or IroncladId or TankId or DreadnoughtId or CombustionEngineId => 0.10,   // 機械部品・機械・蒸気機関・発電機・電化製品・汽船・軍艦・機関車・送電網・機関銃・装甲艦・精密工作機械・戦車・弩級戦艦・内燃機関
        IronId or SteelId or CopperId or LeadId or CementId or GlassId or SulfuricAcidId or SodaAshId or GunpowderId => 0.05,   // 製錬・窯業・化学（石炭を焚く重工業）
        _ => 0.02,                                               // 軽工業＝監督・保守の最小限
    };

    // 生産レシピの共通定数（ビルダーと参照価格計算の単一の真実）：
    public const double LaborPerOutput = 0.5;       // 工場の産出1単位あたりの労働力投入
    public const double FarmLaborPerOutput = 0.4;   // 自家農家（RGO）の産出1単位あたりの農業労働力
    public const double FactoryVa = 0.06;           // 工場の自己費用（管理・資本の付加価値＝MarginalCost）
    public const double RgoVa = 0.34;               // 自家農家の自己費用（土地・道具。＋農業労働0.16≈旧0.5＝損益分岐を維持）

    /// <summary>
    /// 各財の参照価格＝**労働込みの実費用連鎖**から算出（自己費用 ＋ Σ投入×投入の参照価格 ＋ 労働費 ＋ 技師費）。
    /// 原料は安く・深い連鎖ほど高い自然な価格勾配になり、均衡価格 ≈ 限界費用 ≈ 参照価格 に揃う（price/RefPrice≈1）
    /// ＝参照価格を一律 1.0 にしていたために生じていた偽の床/天井（価格二極化）と、
    /// 「深い連鎖の財（発電機・電力）が最初から安い」という時代錯誤の根を絶つ（外科的な時代スケールの撤去の前提）。
    /// 役務は ServiceRefPrice の固定錨。レシピの依存は厳密な ID 順でない（例：電化製品←電力）ので反復緩和で収束させる。
    /// </summary>
    public static readonly double[] RefPrices = ComputeRefPrices();

    // 連鎖（DAG）の反復緩和回数＝最大深さ（≈9）より十分多く回して収束させる（ComputeRefPrices・値は据え置き）。
    // ★段1（2026-09-30・財の削減＝作者裁定 Q2）：連鎖の深さ Complexity とその計算を削除＝読み手は発明の門（段0-2 で解禁の表へ焼いた）と
    //   工場の建設の機械の上乗せ FactoryTierMachinePerDepth（撤去）の2つだけだった＝深さで時代や難しさを表さない（発見が表す）。
    private const int ChainRelaxIterations = 24;

    /// <summary>冷蔵で腐敗が下がる生鮮（肉・畜産・魚・果実・加工肉）。1870s冷凍肉貿易革命の対象（B-2）。</summary>
    public static bool IsRefrigerable(int id) => id is FruitId or LivestockId or FishId or MeatId or PreservedMeatId;

    /// <summary>★(346) 財の月の損耗の率（旧 <c>SpoilageOf</c> の switch を財の表 <see cref="GoodDef.WearRatePerMonth"/> へ移した＝値は1つも変えていない）。
    /// 市場の在庫の腐敗・艦の隻数の割り算など、**率そのもの**を使う式の読み口（`1−d` を作り直すと double の最後の桁が動く＝<c>1−(1−0.06)≠0.06</c>）。</summary>
    public static double WearRatePerMonthOf(int id) => Goods[id].WearRatePerMonth;

    /// <summary>★(346) 耐久度 d（1か月あたり残る割合＝1−損耗の率）。**読み口だけで出す**（表には損耗の率を置く＝恒等）。
    /// 「腐敗率」は別の欄にしない＝腐りやすい財は d が0に近い財（役務は d＝0・貯蔵不能）。残る割合をそのまま掛ける式（持ち越し・艦の在庫）はこちらを読む。</summary>
    public static double DurabilityOf(int id) => 1.0 - Goods[id].WearRatePerMonth;

    /// <summary>★(346) 財の使われ方（<see cref="GoodUse"/>）。今は読み手なし。</summary>
    public static GoodUse UseOf(int id) => Goods[id].Use;

    /// <summary>財ごとの輸送重量（§5.1拡張）。嵩・重さが大きいほど輸送費が高い。鉄道/船/軍艦は自身の輸送が極めて困難＝極大。</summary>
    public static double TransportWeight(int id) => id switch
    {
        RailId or LocomotiveId or SailingShipId or SteamshipId or WarshipId or IroncladId or DreadnoughtId => 6.0,                            // 線路・機関車・帆船・汽船・軍艦・装甲艦・弩級戦艦＝極大（巨大構造物）
        MachineryId or SteamEngineId or ArtilleryId or MachineGunId or TankId or CombustionEngineId => 3.0,                     // 機械・蒸気機関・発電機・火砲・機関銃・精密工作機械・戦車・内燃機関＝大型機械
        // ★2026-08-28（輸送コスト是正・作者一任）：ジャガイモ115・トウモロコシ116を鉱石群から外す＝主食3財（麦1.0・米1.0・芋2.5）の
        //   あいだで従量輸送費が2.5倍割れる内部矛盾（RefPriceが同一0.54なので比がそのまま費用差になる）＝緯度帯主食の地域だけ主食輸入が構造的に不能だった。
        IronOreId or CoalId or CopperOreId or LeadOreId or LimestoneId or ClayId or CrudeOilId or TimberId or IronId or SteelId or CopperId or LeadId or CementId or LumberId or ChromeId or FertilizerId or TinOreId or SaltpeterId or GuanoId => 2.5,   // 鉄鉱石・石炭・銅鉱・鉛鉱・石灰・粘土・原油・木・鉄・鋼・銅・鉛・セメント・製材・クロム鉱・肥料・錫鉱・硝石・グアノ＝嵩高
        GoldOreId or SilkId or DyeId or SpiceId or GunpowderId or SilkClothId or MedicineId or QuinineId or SilverId => 0.4,                  // 金・生糸・染料・香辛料・火薬・絹織物・薬・キニーネ・銀＝小/高価値
        _ => 1.0,
    };

    /// <summary>工場の建設仕様（カテゴリ・建設費・建設四半期数）。高度な工場ほど高額・長期。</summary>
    public static (string Category, double Cost, int Quarters) FactorySpec(int id) => id switch
    {
        DreadnoughtId => ("超重軍需", 320_000, 28),   // 弩級戦艦＝最深連鎖・最高額/最長期（tier3海軍）
        TankId => ("超重軍需", 200_000, 16),   // 戦車＝高額/長期（tier3陸軍）
        IroncladId => ("先端軍需", 240_000, 22),   // 装甲艦＝最も高額/長期（鉄甲艦）
        MachineGunId => ("高級軍需", 150_000, 12),   // 機関銃＝高額（精密軍需）
        // ★施設の明示登録（LIST §0-9-C 段1・2026-09-12）：学校94／病院95／大学136 を既定の軽工業（1年・3万）へ落とさない。値は既存タプルの再利用だけ（新しい数字ゼロ）。
        //   役務は建てられない（World.CanBuildFactory/BuildFactory・SelfPlay の候補列挙が Tier==Service を先に弾く）ので、今はどの読み手もこの3 id で引かない＝厳密恒等。
        //   施設化（LIST 段2〜4）で役務の施設が建てられるようになった時に初めて効く。
        ElectricityId or ApplianceId or LocomotiveId or SteamshipId or WarshipId or GridId or HigherEducationId => ("先端", 180_000, 20),   // 発電機・電力・電化製品・機関車・汽船・軍艦・送電網・大学（高等教育136）＝高額/長期
        MachineryId or SteamEngineId or RailId or CombustionEngineId => ("機械", 110_000, 12),   // 機械部品・工具・機械・蒸気機関・線路・精密工作機械・内燃機関
        ArtilleryId or RifleId or AmmunitionId or SailingShipId             => ("軍需", 90_000,  8),    // 火砲・小銃・弾薬・帆船
        IronId or SteelId or CopperId or LeadId or CementId or GlassId or SulfuricAcidId or SodaAshId or GunpowderId or HealthcareId => ("重工業", 70_000, 8),   // 製鉄・化学・窯業・病院（医療95＝重工業相当）
        EducationId => ("軽工業", 30_000, 4),                                 // 学校（教育94）＝軽工業の組を明示（既定と同じ組＝値は不変）
        _ => ("軽工業", 30_000, 4),                                  // 繊維・食品・日用・奢侈＝安価/短期
    };

    /// <summary>★塊1（2026-08-31）：動物性食料の必要量/主食の必要量＝Σ(非主食のNutrition×BaseNeed)/Σ(主食のBaseNeed)。
    /// FoodIncludesAnimalProtein 点火時に主食の必要量をこの比で譲る（バスケットに肉が入った分だけ穀物の取り分が減る＝
    /// 総食料需要を中立に保つ。野菜(108)追加時にパンを0.80→0.55へ譲った前例と同じ処理をデータから導出）。現行データでは 0.22/1.10=0.2。</summary>
    public static readonly double AnimalNutritionNeedRatio = ComputeAnimalNutritionNeedRatio();
    private static double ComputeAnimalNutritionNeedRatio()
    {
        double animal = 0, staple = 0;
        foreach (var d in Goods)
        {
            if (d.Tier == GoodTier.Staple) staple += d.BaseNeed;
            else if (d.Nutrition > 0) animal += d.Nutrition * d.BaseNeed;
        }
        return staple > 1e-9 ? animal / staple : 0;
    }

    /// <summary>★発見の表（段2A・2026-10-01＝発見の作り替えの骨組み・設計 docs/202609/20260928/20260928_財の削減_連鎖の深さの撤去_設計 §1A・§9）＝**発見の単位はレシピ**。
    /// 旧＝解禁の表 OnsetTable（行＝財・レシピ・onset＝技術水準の時計の傾斜の始まり）と、財の門（発明の行＝旧 連鎖の深さ）・需要の門（需要の行）・器の既定の行。
    /// 新＝行＝（名前・財・レシピ・分野・年代）。解禁は**2値**（作者裁定 2026-09-28 §1A-2＝点数＝技術力＋必要性＋伝わり、一度でも100に届けば解禁・失われない）＝<see cref="Discovery.Step"/>。
    /// ・レシピ ""＝既定のレシピ（goods.json の Inputs）。代替のレシピは Simulation の注入と同じ名前（Producer.RecipeName）。表に無いレシピ＝地理差（常にある）。
    /// ・財の解禁＝その財のレシピの解禁の最大（地理差のレシピが1本でもあれば常に解禁）・需要＝財の解禁（<see cref="Discovery.GoodGate"/>・設計 §1-2）
    ///   ＝旧の財の門（発明の行）と需要の門（需要の行）は行ごと消えた（財の門だけを持っていた財は既定のレシピの行へ）。
    /// ・難易度＝年代（10年刻み・作者裁定 2026-09-28 ③）を1本の Y（<see cref="Coefficients.DiscoveryTechPerDecade"/>）で技術水準へ写す＝<see cref="Discovery.Difficulty"/>。
    ///   ⚠暦は読まない（年代は行のデータ・コードは暦の年を読まない＝§1A-5 の注）。年代は §1A-5 の表・無い行は Q11（発見の年と実用の年の中間）で置いた。
    /// ・分野＝作者裁定 2026-09-28 ⑤（制度・思想・製法・発明〔新兵器・新製品〕）。制度（特許・株式会社）と思想（社会主義・共産主義・帝国主義・ファシズム）は 2026-10-01 (152) に同じ表へ（設計 §1A-6）＝財を持たない行。
    /// ・場所に結びついた発見（大鉱床・植林）は <see cref="Sites"/>（同じ骨組み・2項）。</summary>
    /// <summary>★代替する物の欄（<see cref="DiscoveryRow.Replaces"/>・2026-10-02 (159)）の特別な値＝その行の財そのもの（製法の既定）と陸の輸送（機関車＝財ではない＝内陸の州の輸送の容量の律速）。
    /// 財の id は 0 以上＝特別な値は負（財を持たない行の <see cref="NoGood"/>＝−1 とは別の欄）。</summary>
    public const int ReplacesOwn = -1, LandFreight = -2;
    public const int DiscoveryBaseDecade = 1780;   // 難易度0の年代（1783年の開幕で既知の行＝坩堝鋼 1740年代・ワットの回転機関 1781＝設計 §1A-5 の注）
    public static readonly DiscoveryRow[] Discoveries =
    [
        // --- 製法（同じ財を別の投入で作る方法＝時代差のレシピ）---
        new("蒸気機関", LaborId, "蒸気機関", DiscoveryField.Process, 1780),              // 工場に据える原動機（ワットの回転機関1781）＝動力（労働力の財）の別のレシピ（必要性＝作者 2026-10-02 の一般則＝「(間接的に人が生み出している)動力が高いか」＝動力の平滑化した値段÷蒸気の動力を新しいレシピで作る費用〔★(169) 他の行と同じ規則＝自己費用＋蒸気機関の財と石炭〕・旧＝人件費の世界比は (159)(165) で削除）。開幕の既知＝world.json の knownDiscoveries（英＝1783年に工場の原動機として使っていた唯一の国・実装ログ (169)）
        new("甜菜糖", SugarId, "甜菜糖", DiscoveryField.Process, 1800),                  // アシャール1801・大陸封鎖1806〜（作者 2026-09-28 提案①の採用＝地理差でなく発見）
        new("魚の缶詰", CannedFoodId, "魚の缶詰", DiscoveryField.Process, 1810),          // 缶詰の第2レシピ（アペール法1809）
        new("キニーネの単離", QuinineId, "", DiscoveryField.Process, 1820),              // ペルティエ＝カヴァントゥ1820（単離の前は樹皮の粉末＝地理差）
        new("黄鉄鉱法", SulfuricAcidId, "黄鉄鉱法", DiscoveryField.Process, 1830),          // 1838年の硫黄危機の後（旧 SulfurFromPyriteOnset）
        new("木材パルプ製紙", PaperId, "", DiscoveryField.Process, 1840),                // 砕木1840s（その前はぼろ布製紙＝地理差）
        new("過リン酸石灰", FertilizerId, "", DiscoveryField.Process, 1840),             // ロース1842（その前は厩肥・グアノ＝地理差）
        new("転炉鋼", SteelId, "", DiscoveryField.Process, 1850),                        // ベッセマー1856（その前は坩堝鋼・錬鉄レール＝地理差）
        new("石油灯油", LampOilId, "石油灯油", DiscoveryField.Process, 1850),            // ドレーク油井1859
        new("石油ケロシン", LampOilId, "石油ケロシン", DiscoveryField.Process, 1850),    // 同上（石油灯油と同じ変換の2本目＝設計 §5 F4 の重複・畳むのは次の段）　★(149) 合流：(148) の F4（この行を畳んで安い方の数〔原油0.5・生産性1.6〕を石油灯油の行で開く）は差し戻した＝挙動を変える畳み方だった（同じ種1〜5 で exact −5.9・near −6.6）＝畳み方は 00一覧
        new("鉄筋コンクリート", HousingId, "鉄筋コンクリート", DiscoveryField.Process, 1860),   // モニエ1867（§1A-5 の表）
        new("クロムなめし", LeatherId, "クロムなめし", DiscoveryField.Process, 1880),     // 商用1884（§1A-5 の表）
        new("無煙火薬", GunpowderId, "無煙火薬", DiscoveryField.Process, 1880),          // ヴィエイユ1884
        new("水力発電", ElectricityId, "水力発電", DiscoveryField.Process, 1880),         // アップルトン1882・ナイアガラ1895
        new("蒸気タービン", ElectricityId, "蒸気タービン", DiscoveryField.Process, 1880), // パーソンズ1884
        new("電動機", LaborId, "電動", DiscoveryField.Process, 1880, EngineerParent: SteamEngineId),                    // 工場の電動機＝動力（労働力の財）の別のレシピ（必要性＝動力の値段÷電動の動力を新しいレシピで作る費用〔自己費用＋電力〕・蒸気機関の行と同じ規則・(165)(169)）   // ★(461) 時計の形（DiscoveryClockWorldPerMonth が正）で、さらに別の旗 DiscoveryClockMotorParent が正のときだけ（既定 0）、親の産業の技師＝蒸気機関の財（電動機が置き換えた工場の原動機の産業）。電力の産業は立つまで 0 で錠になる（確証なし・事前登録 dk_20261020_prereg.txt [2]）
        new("人絹", SilkClothId, "人絹", DiscoveryField.Process, 1890),                  // シャルドネ1891・ビスコース1892
        new("石油火力", ElectricityId, "石油火力", DiscoveryField.Process, 1900),        // §1A-5 の表
        new("窒素固定", FertilizerId, "ハーバーボッシュ", DiscoveryField.Process, 1910), // ハーバー＝ボッシュ1913（旧＝肥料の既定の onset の流用＝設計 §5 F2 の半分・硝石へ移すのは次の段）
        new("合成ゴム", RubberId, "合成ゴム", DiscoveryField.Process, 1910),             // メチルゴム1916（§1A-5 の表）
        // --- 発明・新製品（今までに無かった財＝既定のレシピの行）---
        //   ★代替する物（Replaces・2026-10-02 (159)）＝作者 2026-10-02「その発見が生み出すものが代替するものが高いか、で判定する」の「代替するもの」＝新製品が置き換えた既存の財（AI の対応づけ＝推奨で進めた）。
        //   機関車だけは財ではない（陸の輸送＝作者 2026-10-02「鉄道研究の必要性は、軍事力ではなく、輸送力の不足から導かれるべきだ」）。
        //   ⚠(169) 物の単位の換算（新しい財1単位が置き換える代替する物の量のデータ・(167) 反証役 vd1#1 の処置）の腕は入れていない＝10種で電力・送電網・冷蔵・ゴム・自動車がどの国でも見つからない
        //     （AI の対応づけ＝石炭火力と送電網→ガス・電化製品→家事奉仕のまま・作者への確認・パッチ measurements/m168_20261002_units.diff・実装ログ (169)）＝換算の比は参照価格の比のまま（Discovery.ReplacedUnits）。
        new("ガス灯", GasId, "", DiscoveryField.Product, 1810, Replaces: LampOilId),      // マードック1792・商用1812＝灯油（鯨油・菜種油のランプ）とろうそくの灯りを置き換えた
        new("缶詰", CannedFoodId, "", DiscoveryField.Product, 1810, Replaces: PreservedMeatId),   // アペール法1809・ブリキ缶1810＝塩漬けの肉（艦隊と軍の糧食・1795 の懸賞）を置き換えた
        new("汽船", SteamshipId, "", DiscoveryField.Product, 1820, Replaces: SailingShipId),   // クラーモント1807〜外洋1830s（Q11＝中間）＝帆船
        new("機関車", LocomotiveId, "", DiscoveryField.Product, 1820, Replaces: LandFreight),   // ストックトン＝ダーリントン1825・ロケット号1829＝陸の輸送（馬車・運河の荷船＝内陸の輸送の容量）
        new("加硫ゴム", RubberId, "", DiscoveryField.Product, 1830, Replaces: LeatherId),  // グッドイヤー1839（ゴムに地理差のレシピは無い＝新しい財）＝革（ベルト・ホース・靴底・防水）
        new("ダイナマイト", DynamiteId, "", DiscoveryField.Product, 1860, Replaces: GunpowderId),   // ノーベル1867＝発破の黒色火薬
        new("冷蔵", RefrigeratorId, "", DiscoveryField.Product, 1870, Replaces: PreservedMeatId),   // リンデ1870s・冷凍船1877〜82（冷蔵庫の財は冷蔵の加工肉の投入も兼ねる）＝塩漬けの加工肉
        new("石炭火力", ElectricityId, "", DiscoveryField.Product, 1880, Replaces: GasId),   // パール街1882（電力の既定のレシピ）＝ガス灯
        new("送電網", GridId, "", DiscoveryField.Product, 1880, Replaces: GasId),          // 配電（パール街1882）・交流送電1890s＝ガスの導管の網
        new("電化製品", ApplianceId, "", DiscoveryField.Product, 1880, Replaces: DomesticServiceId),   // §1A-5 の表＝家事奉仕（家事使用人）
        new("自転車", BicycleId, "", DiscoveryField.Product, 1880, Replaces: HorseId, After: "加硫ゴム"),     // 安全型1885＝馬（個人の移動）
        new("内燃機関", CombustionEngineId, "", DiscoveryField.Product, 1880, Replaces: SteamEngineId, After: "蒸気機関"),   // オットー1876・ダイムラー1885＝蒸気機関
        new("自動車", AutomobileId, "", DiscoveryField.Product, 1890, Replaces: HorseId, After: "内燃機関"),  // §1A-5 の表（自転車・自動車＝1880〜1890年代）＝馬（馬車・輜重）
        new("ラジオ", RadioId, "", DiscoveryField.Product, 1920, Replaces: NewspaperId, After: "電化製品"),   // 放送の実用化1920＝新聞
        // --- 発明・新兵器（軍事の圧力は陸・海・空で分ける＝作者裁定 2026-09-28 ⑤）---
        new("装甲艦", IroncladId, "", DiscoveryField.Weapon, 1850, WarDomain.Sea, After: "汽船"),        // ラ・グロワール1859
        new("機関銃", MachineGunId, "", DiscoveryField.Weapon, 1880, WarDomain.Land),     // マキシム1884
        new("弩級戦艦", DreadnoughtId, "", DiscoveryField.Weapon, 1900, WarDomain.Sea, After: "装甲艦"),   // ドレッドノート1906
        new("戦車", TankId, "", DiscoveryField.Weapon, 1910, WarDomain.Land, After: "内燃機関"),             // 1916
        new("軍用機", MilitaryAircraftId, "", DiscoveryField.Weapon, 1910, WarDomain.Air, After: "内燃機関"),
        new("化学兵器", ChemicalWeaponId, "", DiscoveryField.Weapon, 1910, WarDomain.Land), // イーペル1915
        // --- 制度（社会の仕組み）・思想（考え方）＝財を持たない行（財 NoGood）・設計 §1A-6（2026-10-01 (152)）---
        //   旧＝制度は中核エリートと工業化度の傾斜（特許＝中核エリート 0.35〜0.45・株式会社＝最大工業化度 0.25〜0.35・鉄道＝最大工業化度 0.8 の閾値）・
        //   思想は世界の発展度（世界最先端の工業化度と都市の割合）の傾斜＝全ての国がほぼ同時に開いた（計器 (152)＝社会主義・共産主義・株式会社が1786〜1806年に主要国の全てで開いていた）。
        //   必要性の強度は行ごとに読む量が違う（その制度が解く問題・その思想が答える社会の緊張＝Discovery.SpreadNeed）・伝わりは思想と制度の到達（World.SpreadReach＝MultiNationSim.StepDiffusion）。
        new("特許", NoGood, "", DiscoveryField.Institution, 1780),        // 英1624（専売条例）・米1790・仏1791＝設計 §1A-6「1780年代以前」
        new("株式会社", NoGood, "", DiscoveryField.Institution, 1850),    // 英1844・56（有限責任）・仏1867・普1870＝設計 §1A-6「1850年代」
        new("社会主義", NoGood, "", DiscoveryField.Idea, 1830),           // オーウェン1820s・サン＝シモン派・チャーティスト1838
        new("共産主義", NoGood, "", DiscoveryField.Idea, 1840),           // 共産党宣言1848
        new("帝国主義", NoGood, "", DiscoveryField.Idea, 1860),           // 新帝国主義1870s〜（ディズレーリ1872・ベルリン会議1884）
        new("ファシズム", NoGood, "", DiscoveryField.Idea, 1920),         // 伊1919〜22・独1933
        // --- 末尾に足した行（既存の行の添字を動かさない）---
        new("合成染料", DyeId, "合成染料", DiscoveryField.Process, 1850), // ★(415) アニリン（パーキン1856）＝染料の第2レシピ。旧＝行が無く1783年から作れた穴。係数 DiscoveryEngineerHalfPerMonth が0以下のあいだは開幕で解禁済み（Simulation）
        // --- ★(493) ソーダ灰の製法（草木灰が既定＝1783 年から。新しい製法は発見の行・量と順序は measurements/gd_20261023_prereg.txt [2]）---
        new("ルブラン法", SodaAshId, "ルブラン法", DiscoveryField.Process, 1790),               // ルブラン 1791（塩＋硫酸＋石灰石＋石炭）。ナトリウムは塩から＝硫酸は塩を分ける薬（線を割らない）
        new("ソルベー法", SodaAshId, "ソルベー法", DiscoveryField.Process, 1860),               // ソルベー 1861〜クイエ 1864・ブラナー・モンド 1874（塩＋石灰石＋石炭・硫黄なし）
        new("電解", SodaAshId, "電解", DiscoveryField.Process, 1890, After: "石炭火力"),          // 塩水の電解の工業生産 1892（塩＋電力・苛性ソーダと塩素。アルカリとして 1 財に畳む）
    ];
    /// <summary>財を持たない発見の行（制度と思想）の財の欄（<see cref="DiscoveryRowOf"/> は負の財を探さない＝名前で <see cref="DiscoveryRowNamed"/>）。</summary>
    public const int NoGood = -1;

    /// <summary>退場（2026-09-27 回2-1 の続き）＝前期のレシピは後継の発見で退く（旧＝TechOnset＝−後継の onset・能力に 1−後継の解禁度）。
    /// 後継＝既定のレシピ（鋼の後装施条砲・鋼の小銃）を実用にした発見＝転炉鋼。⚠2値＝後継を発見した四半期に退く（普及のなめらかさは市場＝既定のレシピの工場が建つ速さ）。</summary>
    private static readonly (int Good, string Recipe, int SuccessorGood, string SuccessorRecipe)[] Retirements =
    [
        (RifleId, "マスケット", SteelId, ""),
        (ArtilleryId, "鋳鉄砲", SteelId, ""),
        (SteamEngineId, "鋳鉄の機関", SteelId, ""),   // ★(169) 1780年代の蒸気機関（鋳鉄のシリンダーと梁・錬鉄と銅のボイラー）＝高圧・複式の鋼の機関（転炉鋼の後）で退く＝前期の軍需と同じ規則（性能で退く・Simulation.AddAlternateRecipes）
    ];

    /// <summary>★(336) 流れG ② 系統×段階：行ごとの前提の行の添字（−1＝前提なし＝系統の根）＝<see cref="DiscoveryRow.After"/> の名前を引く。
    /// 辺の表と前提の木は AI の推奨。作者 2026-10-02「すべて推奨でよい。」（(177)）が掛かるのは AI の推奨〈引きなし・判定は各系統の次の段だけ〉の一句まで。★(344) 辺の表 8 本（腕 B）に 作者 2026-10-15「特に違和感はないです。これでいきましょう。」＝表の承認（出典の頁は未確認）。
    /// ⚠辺を張る基準：子の投入が親の運び手の財（<see cref="Discovery.CarriersByRow"/>）の唯一の出口なら、子のゲートは親の伝わりの取引の道を作りで閉じる（機関車・汽船 ← 蒸気機関＝蒸気機関を未解禁の国は SteamEngineId の需要が0）
    /// ＝measurements/ln_20261015_prereg.txt [14-2]。★(344) 表は 8 本（(336) の 20 本から、機関車・汽船・石炭火力ほかの 12 本を外した＝腕 B）（旧の旗 DiscoveryTierGate は 2026-10-18 に焼き込み）。⚠機関車・汽船 ← 蒸気機関は伝わりの道（段2）を直してから再導入する（(338) [15]）。
    /// 構築で表の誤りを止める（前提の名前が表に無い・自分自身・輪）。⚠<see cref="Discoveries"/> の後ろに置く（静的初期化は書いた順）。</summary>
    public static readonly int[] DiscoveryAfter = BuildDiscoveryAfter();
    private static int[] BuildDiscoveryAfter()
    {
        var res = new int[Discoveries.Length];
        for (int i = 0; i < Discoveries.Length; i++)
        {
            string a = Discoveries[i].After;
            if (a.Length == 0) { res[i] = -1; continue; }
            int j = System.Array.FindIndex(Discoveries, x => x.Name == a);
            if (j < 0 || j == i) throw new System.InvalidOperationException($"発見の表の行〈{Discoveries[i].Name}〉の前提〈{a}〉が表に無いか自分自身");
            res[i] = j;
        }
        for (int i = 0; i < res.Length; i++)
        {
            int k = i, steps = 0;
            while (res[k] >= 0) { k = res[k]; if (++steps > res.Length) throw new System.InvalidOperationException($"発見の表の前提に輪がある（{Discoveries[i].Name} から）"); }
        }
        return res;
    }
    /// <summary>行の段（根＝1・前提の鎖の長さ＋1）。系統＝根の行（<see cref="DiscoveryLine"/>）。段と系統は前提の1本から導く（別に持たない）。</summary>
    public static int DiscoveryTier(int row)
    {
        int t = 1;
        while (DiscoveryAfter[row] >= 0) { row = DiscoveryAfter[row]; t++; }
        return t;
    }
    /// <summary>行の系統の根の行（前提のない行まで前提をさかのぼる）。</summary>
    public static int DiscoveryLine(int row)
    {
        while (DiscoveryAfter[row] >= 0) row = DiscoveryAfter[row];
        return row;
    }

    /// <summary>発見の表の行（財・レシピ）の添字。無ければ −1（地理差＝常にある）。⚠線形の走査＝構築と Producer の控え（<see cref="Producer.DiscoveryRow"/>）だけが呼ぶ。</summary>
    public static int DiscoveryRowOf(int good, string recipe)
    {
        if (good < 0) return -1;   // 財を持たない行（制度と思想）は財で探さない
        for (int i = 0; i < Discoveries.Length; i++) if (Discoveries[i].Good == good && Discoveries[i].Recipe == recipe) return i;
        return -1;
    }

    /// <summary>★(spg) 既定のレシピ（空の名前）の行＝<see cref="DiscoveryRowOf"/>(good, "") と同じ値を財ごとに前もって引いた表（発見の表は静的で変わらない）。
    /// 財の解禁（<see cref="Discovery.GoodGates"/>・<see cref="Discovery.GoodGate"/>）が生産者の居ない財ごとに毎月この線形の走査をしていた分（結果は同じ）。</summary>
    public static int DefaultDiscoveryRowOf(int good)
    {
        var t = _defaultRowOfGood ??= BuildDefaultRows();   // 並んで作っても同じ中身＝どちらが残っても同じ
        return (uint)good < (uint)t.Length ? t[good] : DiscoveryRowOf(good, "");
    }
    private static int[]? _defaultRowOfGood;
    private static int[] BuildDefaultRows()
    {
        var t = new int[Goods.Length];
        for (int g = 0; g < t.Length; g++) t[g] = DiscoveryRowOf(g, "");
        return t;
    }

    /// <summary>発見の表の行の添字を名前で（財を持たない制度と思想の行）。無ければ −1。</summary>
    public static int DiscoveryRowNamed(string name) => System.Array.FindIndex(Discoveries, d => d.Name == name);

    /// <summary>このレシピを退かせる発見の行（無ければ −1）。</summary>
    public static int RetiredByRowOf(int good, string recipe)
    {
        foreach (var (g, r, sg, sr) in Retirements) if (g == good && r == recipe) return DiscoveryRowOf(sg, sr);
        return -1;
    }

    /// <summary>★産地の発見の表（段2A の続き・2026-10-01 (148)・設計 §9-1 の提案③＝鉱床の発見も同じ骨組みへ〔2026-09-28 作者が採用した提案・文は AI の整理〕＋(129) の申し送り＝アジアの樹皮は植林の発見で〔AI の推奨〕）＝
    /// **場所に結びついた発見**＝1783年の地図に無かった産地（大鉱床・植林）。行＝（名前・場所〔州か植民地の名＝ProvinceSpec.City／Colony.Name〕・財・年代・分野）。
    /// ・その場所の生産者は通常の賦存（Simulation の ResourceEndowment）で作り、**発見まで門0**（<see cref="Discovery.Gate(Region, Producer)"/>＝Producer.SiteFound）。
    /// ・判定は発見の表と同じ骨組み（<see cref="Discovery.Step"/>）＝点数＝技術力（0〜70・難易度＝年代×Y）＋必要性（0〜70）が一度でも100に届けば開く・失われない。
    ///   伝わりの項は持たない（他国の発見は自分の土地の鉱床を見つけない＝設計 §9-2 の提案③の読み＝2項・最大の和 140≧100）。
    /// ・★(171) 必要性＝ln（その財の平滑化した値段÷その国で今のレシピで掘る費用）（作者 2026-10-02 の発見の必要性の一般則を産地の行に当てたのは AI の読み＝推奨で進めた・作者の承認は未確認〔元の形＝作者が採用した提案③〕・費用は国の今のレシピ＝産地ごとの富みを読まない＝富みを費用に読むまでは候補2〔「2でよい」の物差し〕と呼ばない＝(172) 反証役 vd4#3・vd4#4）・
    ///   技術力（見つける力）＝70×min(1, 技術水準÷難易度)×人の厚み N÷(N＋N0)（N0＝<see cref="Coefficients.DiscoverySiteFindHalfPop"/>）＝<see cref="Discovery.SiteTerms"/>。
    ///   鉱床＝入植者（その国の中核民族の人数＝POP の大きさ×CoreEthnicShare）・植林＝住民（樹を植えて樹皮を剥ぐ働き手）。
    ///   旧（(148)〜(169)）＝必要性＝人の厚み N÷(N＋ColonyPromotionMinPop)×その財の実質の値段÷参照価格（設計 §9-1 の提案③＝適性＝入植者×金の値・2026-09-28 作者が採用した提案＝文は AI の整理）。
    /// ・データは場所の一次データ（暦年ではない＝思想①②の「データはよい」）。旧＝ProvinceSpec.LateResources（西部の硫黄・銀・北九州の硫黄）＋技術水準の傾斜 DepositTechScale と、
    ///   年2.5%の抽選の鉱脈の発見（全ての発見が frontierRush＝州の全ての鉱物を5倍＝検討 §10-3 項目10(a)）とシルバーラッシュの枝＝この表の1本へ置き換えた。</summary>
    public enum SiteKind { Deposit, Plantation }
    public readonly record struct SiteRow(string Name, string Place, int Good, int Decade, SiteKind Kind);
    public static readonly SiteRow[] Sites =
    [
        // --- 鉱床（★(171) 入植者が見つける力・財の値段÷掘る費用が必要性）---
        new("カリフォルニアの金", "アルタ=カリフォルニア", GoldOreId, 1840, SiteKind.Deposit),   // サッターの製材所 1848
        new("ヴィクトリアの金", "オーストラリア", GoldOreId, 1850, SiteKind.Deposit),             // バララット・ベンディゴ 1851
        new("ラントの金", "南アフリカ", GoldOreId, 1880, SiteKind.Deposit),                       // ウィットウォーターズランド 1886
        new("クロンダイクの金", "フォートユーコン", GoldOreId, 1890, SiteKind.Deposit),           // ボナンザ・クリーク 1896
        new("コムストックの銀", "西部", SilverId, 1850, SiteKind.Deposit),                         // コムストック 1859（⚠SilverRushEnabled＝0 の間はこの行を置かない＝検討 Q13 (d)）
        new("ルイジアナの硫黄", "西部", SulfurId, 1890, SiteKind.Deposit),                         // フラッシュ法 1894（旧 LateResources＋SulfurLateDepositOnset）
        new("九州の硫黄", "北九州", SulfurId, 1880, SiteKind.Deposit),                             // 明治の近代採掘（旧 LateResources＋SulfurLateDepositOnset）
        // --- 植林（★(171) 住民が見つける力・財の値段÷植えて剥ぐ費用が必要性）＝キナの樹はアンデスの外へ種子で運ばれて植えられた ---
        new("ジャワのキナ", "バタヴィア", CinchonaBarkId, 1850, SiteKind.Plantation),               // ハスカール 1854
        new("ニルギリのキナ", "マドラス", CinchonaBarkId, 1860, SiteKind.Plantation),               // マーカム 1860〜
        new("セイロンのキナ", "セイロン", CinchonaBarkId, 1860, SiteKind.Plantation),               // 1860〜70年代（Q11＝中間）
    ];

    /// <summary>産地の発見の行（場所・財）の添字。無ければ −1＝開始時の賦存（または非産地）。⚠線形の走査＝構築だけが呼ぶ。
    /// 銀の行は <see cref="Coefficients.SilverRushEnabled"/>＝0 の間は無いものとして扱う（呼び手が読む）。</summary>
    public static int SiteRowOf(string place, int good)
    {
        for (int i = 0; i < Sites.Length; i++) if (Sites[i].Good == good && Sites[i].Place == place) return i;
        return -1;
    }

    private static double[] ComputeRefPrices()
    {
        var rp = new double[Goods.Length];                                       // id は 0..Length-1 で連続
        foreach (var g in Goods)
            rp[g.Id] = g.Tier == GoodTier.Service ? ServiceRefPrice(g.Id)
                     : g.Inputs.Length == 0 ? RgoVa + FarmLaborPerOutput * ServiceRefPrice(FarmLaborId)   // 採取＝自己費用＋農業労働
                     : FactoryVa;                                                                          // 工場＝初期値（投入は反復で積む）
        for (int iter = 0; iter < ChainRelaxIterations; iter++)                  // 連鎖の最大深さ（≈9）より十分多く回し収束
            foreach (var g in Goods)
            {
                if (g.Tier == GoodTier.Service || g.Inputs.Length == 0) continue;
                double c = FactoryVa + LaborPerOutput * ServiceRefPrice(LaborId) + AdvLaborPerOutput(g.Id) * ServiceRefPrice(AdvLaborId);
                foreach (var inp in g.Inputs) if (inp.GoodId >= 0 && inp.GoodId < rp.Length) c += inp.QtyPerOutput * rp[inp.GoodId];   // 境界ガード（goods.json 手編集で範囲外 id が混ざっても静的初期化を TypeInitializationException で殺さない）
                rp[g.Id] = c;
            }
        return rp;
    }
}
