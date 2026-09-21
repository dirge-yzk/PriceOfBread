using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 02断線走査（2026-07-28）§3恒久対策：型F「宣言された動学が参照価格・解禁順で起きない」の検出器。
/// 缶詰の迂回（onset0の並存レシピが既定レシピ0.5を無意味化）と電力の件（C#だけ直してdumpgoodsを忘れる／その逆）を構造で再発防止する。
/// 診断の対＝Runner の refprice 動詞（順位・単位投入費の印字）。
/// </summary>
public class BreakDetectionTests
{
    // 早期供給を**意図した**公認の橋レシピ（既定レシピの近代法onsetより早いことが設計＝DefaultRecipeOnsetのコメントに明記）：
    // 紙のぼろ布製紙（早期は麻+綿布）・肥料のグアノ肥料（それ以前の施肥は厩肥/グアノ）。新しい早期橋はここに追記して意図を宣言する。
    private static readonly HashSet<string> SanctionedEarlyRecipes = ["ぼろ布製紙", "グアノ肥料", "堆肥"];   // ★堆肥（2026-09-06・E-13再開）＝厩肥は最古の肥料＝化学肥料（1842）より前から在る並存レシピ

    [Fact]
    public void 並存レシピは既定レシピの解禁を迂回しない()
    {
        var sim = new MultiNationSim([NationConfig.Britain()], seed: 1);
        var w = sim.Nations[0].World;
        var violations = new List<string>();
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                double onset = Catalog.DefaultRecipeOnset(p.GoodId, w.Coef);   // ★2026-09-06：係数対応の既定onset（OilNeedsMachinery=1 点火で原油の閾値0.6は外れ、掘削機＝機械の投入が物理ゲート）
                if (onset <= 0) continue;
                if (p.TechOnset >= onset - 1e-9) continue;
                if (SanctionedEarlyRecipes.Contains(p.RecipeName ?? "")) continue;
                violations.Add($"{r.Name}: {Catalog.Goods[p.GoodId].Name}({p.GoodId}) レシピ'{p.RecipeName}' onset={p.TechOnset:F2} < 既定 {onset:F2}");
            }
        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void 埋め込みgoodsJsonはBuiltinGoodsと内容一致する()
    {
        // 二重管理（C#のBuiltinGoods＝真実源の書き手／埋め込みgoods.json＝実行時の読み手）の同期検証。
        // 片方だけ直すと挙動が黙って乖離する（電力の型F筆頭がこの形だった）＝dumpgoods忘れの検出器。
        var asm = typeof(Catalog).Assembly;
        string? name = System.Array.Find(asm.GetManifestResourceNames(), n => n.EndsWith("goods.json", System.StringComparison.Ordinal));
        Assert.NotNull(name);
        using var st = asm.GetManifestResourceStream(name!)!;
        using var rd = new System.IO.StreamReader(st);
        string embedded = rd.ReadToEnd();
        // 書式差（整形・BOM）を無視して内容の一致だけを見る＝同一オプションで正規化再直列化して比較
        var parsed = System.Text.Json.JsonSerializer.Deserialize<GoodDef[]>(embedded, WorldData.Options);
        Assert.NotNull(parsed);
        string canonEmbedded = System.Text.Json.JsonSerializer.Serialize(parsed, WorldData.Options);
        Assert.Equal(Catalog.Serialize(), canonEmbedded);
    }
}
