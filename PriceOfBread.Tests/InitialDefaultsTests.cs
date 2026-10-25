using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 初期値の既定値の一本化（リファクタ④・実装ログ (247)）の番人。国の欄の既定値は <see cref="NationConfig"/> の初期化子が唯一の場所で、
/// world.json の国は「既定と違う値」のキーだけを書く。同じ値のキーが残ると、既定を動かしたときに片方だけ効いたように見える（2か所の食い違い）。
/// </summary>
public class InitialDefaultsTests
{
    private static JsonDocument WorldJson()
    {
        var asm = typeof(WorldData).Assembly;
        string name = System.Array.Find(asm.GetManifestResourceNames(), x => x.EndsWith("world.json", System.StringComparison.Ordinal))!;
        return JsonDocument.Parse(asm.GetManifestResourceStream(name)!);
    }

    private static bool Same(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;
        return a.ValueKind switch
        {
            JsonValueKind.Number => a.GetDouble() == b.GetDouble(),
            JsonValueKind.String => a.GetString() == b.GetString(),
            JsonValueKind.Array => a.GetArrayLength() == b.GetArrayLength() && a.EnumerateArray().Zip(b.EnumerateArray(), Same).All(x => x),
            JsonValueKind.Object => a.GetRawText() == b.GetRawText(),
            _ => true,   // true・false・null は種類が同じなら同じ
        };
    }

    [Fact]
    public void 回247_world_jsonの国は既定と同じ値のキーを持たない()
    {
        var defaults = JsonSerializer.SerializeToElement(new NationConfig(), WorldData.Options);
        using var doc = WorldJson();
        var nations = doc.RootElement.GetProperty("nations");
        Assert.True(nations.GetArrayLength() >= 61);
        var redundant = new List<string>();
        foreach (var n in nations.EnumerateArray())
            foreach (var p in n.EnumerateObject())
                if (p.Name != "farmLordShare" && defaults.TryGetProperty(p.Name, out var dv) && Same(p.Value, dv))   // ★(545) farmLordShare は国ごとに必ず置く値（既定と同じ 0.6 の国〔ザクセン・教皇領・アワド〕も明示する）
                    redundant.Add($"{n.GetProperty("name").GetString()}.{p.Name}");
        Assert.True(redundant.Count == 0, $"既定と同じ値のキーが {redundant.Count} 件ある（書かない）： {string.Join("、", redundant.Take(12))}");
    }

    [Fact]
    public void 回247_既定は最頻の値_書くキーが最も少ない()
    {
        // 既定は世界の大半の国の値に置く（書くキーが最少）。位置・階級の規模・識字・国庫・貨幣化の段のように国ごとに必ず置く値は既定を持たない（除く）。
        //   ★(388) treasuryMonths＝開幕の国庫の規則（歳入×月数）。絶対額 treasury の置き換えなので国ごとに必ず置く値の側（既定を 2.5 にすると JSON に書かない建国・番人の国が規則を持ってしまう＝規則のない国の旧の経路が消える）。
        //   ★(248) broadMoneyToGdp も既定を最頻（0.1）へ動かした＝この番人の対象に入れた（旧＝除外・(247) の「建国が読む」は誤診）。★(273) broadMoneyToIncome に置き換えた〔(375) 置き直しの機構と一緒に削除した〕。
        var defaults = JsonSerializer.SerializeToElement(new NationConfig(), WorldData.Options);
        var perNation = new HashSet<string> { "name", "provinces", "rule", "latitude", "longitude", "literacy", "treasury", "treasuryMonths", "nobility", "peasant", "worker", "bourgeois", "professional", "merchant", "artisan", "farmLordShare" };   // ★(545) farmLordShare を足した（領主の取り分＝43 国の穴を埋め、全 61 国が必ず置く値へ。既定 0.6 は new NationConfig() で組む合成の試験の世界のためだけに残す）
        var cfgs = WorldData.Nations();
        Assert.True(cfgs.Length >= 61);
        using var doc = WorldJson();
        var nations = doc.RootElement.GetProperty("nations").EnumerateArray().ToArray();
        var notMode = new List<string>();
        foreach (var dp in defaults.EnumerateObject())
        {
            if (perNation.Contains(dp.Name)) continue;
            int usingDefault = nations.Count(n => !n.TryGetProperty(dp.Name, out _));
            var groups = nations.Where(n => n.TryGetProperty(dp.Name, out _)).GroupBy(n => n.GetProperty(dp.Name).GetRawText()).Select(g => g.Count()).ToArray();
            int bestOther = groups.Length == 0 ? 0 : groups.Max();
            if (bestOther > usingDefault) notMode.Add($"{dp.Name}（既定を使う国 {usingDefault}・他の最頻 {bestOther}）");
        }
        Assert.True(notMode.Count == 0, "既定が最頻でない欄（既定を最頻の値へ動かせばキーが減る）： " + string.Join("、", notMode));
    }

    [Fact]
    public void 回247_動かした既定の値の固定()
    {
        // ★(248) 名前を中身に合わせた（旧「既定を動かしてはならない欄_建国が既定を読む」＝反証役 vd1#5）。動かした既定の値を固定する（動かすなら world.json のキーと一緒に）。
        // 分離独立の建国（MultiNationSim）は NationConfig を手で組み、PrebuiltRegions を渡す＝BuildNation を通らず、Simulation が cfg から読むのは座標だけ（(375) 旧 BroadMoneyToIncome は置き直しの機構と一緒に削除した）。
        // 建国が明示しない欄のうち既定を動かした4欄（FarmLordShare・CulturalGoodId は BuildProvince だけが読む＝建国は読まない）
        var d = new NationConfig();
        Assert.Equal(0.6, d.FarmLordShare);
        Assert.Equal(Catalog.BreadId, d.CulturalGoodId);
        Assert.Equal(ReformLevel.Full, d.InitialCornLaws);
        Assert.Equal(ReformLevel.Full, d.InitialSerfEmancipation);   // ★(487) 既定を なし→全面 へ（61国のうち38国が全面・世界データは なし／限定 だけを明示する）
        Assert.Equal(0.4, d.InitialTariff);
        Assert.Equal(0.25, d.InitialInterestRate);
    }

    // ───────────────────────── (545) 領主の取り分（farmLordShare）の穴の補充 ─────────────────────────
    // 期待値は事前登録の表（measurements/fl545_20261025_prereg.txt [2]）から手で写した。world.json から読まない（同じ出所から期待値を作らない）。
    // 値はすべて AI の推定（出典は研究書名のみ・確証なし）。目盛り＝農場の残余のうち地主・徴収権者が取る割合（1.0 農奴制・0.8 領主農場・0.5 小作）。
    private static readonly Dictionary<string, double> FarmLordShareTable = new()
    {
        ["フランス"] = 0.5, ["日本"] = 0.5, ["イギリス"] = 0.5, ["アメリカ"] = 0, ["ロシア"] = 1, ["プロイセン"] = 0.8, ["オスマン"] = 0.8, ["オーストリア"] = 0.5,
        ["ネーデルラント"] = 0.5, ["スペイン"] = 0.5, ["サルデーニャ＝ピエモンテ"] = 0.5, ["清"] = 0.5, ["スウェーデン"] = 0.33, ["ポルトガル"] = 0.5, ["ジェノヴァ"] = 0.5,
        ["ソコト"] = 0.7, ["ボルヌ帝国"] = 0.7, ["ハワイ王国"] = 0.5,   // ここまでが (545) の前から一次のデータだった 18 国
        ["ポーランド"] = 1, ["デンマーク"] = 0.7, ["両シチリア"] = 0.7, ["ザクセン"] = 0.6, ["教皇領"] = 0.6, ["バイエルン"] = 0.5, ["ハノーファー"] = 0.5,
        ["ヴュルテンベルク"] = 0.5, ["ライン諸邦"] = 0.5, ["トスカーナ"] = 0.5, ["ヴェネツィア"] = 0.5, ["スイス"] = 0.3,
        ["朝鮮"] = 0.5, ["マラータ同盟"] = 0.4, ["マイソール"] = 0.5, ["ハイデラバード"] = 0.5, ["シク連邦"] = 0.4, ["ビルマ"] = 0.3, ["ネパール"] = 0.5, ["シャム"] = 0.3,
        ["西山朝"] = 0.3, ["阮氏"] = 0.3, ["カンボジア"] = 0.3, ["ラオス"] = 0.3, ["ペルシャ"] = 0.5, ["アラビア"] = 0.3, ["イエメン"] = 0.4, ["オマーン"] = 0.3,
        ["アフガニスタン"] = 0.4, ["ブハラ"] = 0.5, ["ヒヴァ"] = 0.5, ["コーカンド"] = 0.5, ["デリー"] = 0.5, ["アワド"] = 0.6, ["ラヘジ"] = 0.4,
        ["モロッコ"] = 0.4, ["エチオピア"] = 0.4, ["マダガスカル"] = 0.3, ["ダホメ王国"] = 0.7, ["アシャンティ王国"] = 0.3, ["コンゴ王国"] = 0.3, ["アルジェ摂政国"] = 0.5, ["オヨ帝国"] = 0.4,
    };

    [Fact]
    public void 回545_全国がfarmLordShareを明示する_既定0_6のまま残る国は無い()
    {
        // 既定 0.6 は出典のない値（61 国のうち 43 国が欠けてそれを使っていた）。欠けた国があれば赤＝新しい国を足すときにも穴が開かない。
        using var doc = WorldJson();
        var missing = doc.RootElement.GetProperty("nations").EnumerateArray()
            .Where(n => !n.TryGetProperty("farmLordShare", out _)).Select(n => n.GetProperty("name").GetString()).ToArray();
        Assert.True(missing.Length == 0, $"farmLordShare が欠けた国が {missing.Length} 件： {string.Join("、", missing.Take(12))}");
    }

    [Fact]
    public void 回545_農制の表と全61国のFarmLordShareが一致する()
    {
        // ★意図した二重の正本（(552) 合流・反証役 fl〔pv〕低）：world.json と下の手書きの表が同じ値を持つ＝データを直すときは表も直す（手で写した＝本体から期待値を作らない番人）。
        var cfgs = WorldData.Nations();
        Assert.Equal(61, cfgs.Length);
        Assert.Equal(61, FarmLordShareTable.Count);
        var bad = cfgs.Where(c => !FarmLordShareTable.TryGetValue(c.Name, out var v) || c.FarmLordShare != v)
            .Select(c => $"{c.Name}={c.FarmLordShare}（表 {(FarmLordShareTable.TryGetValue(c.Name, out var e) ? e.ToString() : "なし")}）").ToArray();
        Assert.True(bad.Length == 0, "表と食い違う国： " + string.Join("、", bad.Take(12)));
    }

    [Theory]
    [InlineData("ポーランド", 0.85)]   // 値 1.0 は農奴の市場参加の床 0.15 で 0.85 に切れる（上限 1.0 の国＝旧は欠けて 0.6）
    [InlineData("スイス", 0.3)]        // 床 max(0.3,·) と等しい最小の値（賦存財の RGO は大農園の下限 0.6 が上回る）
    [InlineData("デンマーク", 0.7)]    // 0.6 を超える値（賦存でも 0.7 のまま）
    public void 回545_組んだ世界の産地の取り分_床と切りを跨ぐ(string nation, double expected)
    {
        // 端から端：world.json → NationConfig → BuildProvince → CatalogEconomy が渡す max(0.3, min(値, 0.85))。賦存財の RGO は max(それ, PlantationRentShare 0.6)。
        var sim = new Simulation(WorldData.ByName(nation).ToScenario(1), new NullAgent());
        double plantation = sim.World.Coef.PlantationRentShare.Value;
        var rgos = sim.World.Regions.SelectMany(r => r.Producers).Where(p => p.IsRgo).ToArray();
        Assert.NotEmpty(rgos);
        foreach (var p in rgos)
            Assert.True(System.Math.Abs(p.RentShare.Value - expected) < 1e-12 || System.Math.Abs(p.RentShare.Value - System.Math.Max(expected, plantation)) < 1e-12,
                $"{nation}：RGO の RentShare {p.RentShare.Value} が 値 {expected} でも 賦存の下限 {System.Math.Max(expected, plantation)} でもない");
        Assert.Contains(rgos, p => System.Math.Abs(p.RentShare.Value - expected) < 1e-12);   // 値そのものを読んでいる RGO が居る
    }
}
