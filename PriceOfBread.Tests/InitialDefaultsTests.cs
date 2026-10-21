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
                if (defaults.TryGetProperty(p.Name, out var dv) && Same(p.Value, dv))
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
        var perNation = new HashSet<string> { "name", "provinces", "rule", "latitude", "longitude", "literacy", "treasury", "treasuryMonths", "nobility", "peasant", "worker", "bourgeois", "professional", "merchant", "artisan" };
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
}
