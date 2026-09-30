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
    private static readonly HashSet<string> SanctionedEarlyRecipes = ["ぼろ布製紙", "グアノ肥料", "堆肥", "樹皮の粉末", "坩堝鋼"];   // ★坩堝鋼（2026-10-01 段2A）＝転炉鋼（鋼の既定のレシピ・1850年代）の前の橋（旧＝器の旗が既定の Producer へ onset を焼いた＝既定の onset の表に出ず検査の外だった）   // ★堆肥（2026-09-06・E-13再開）＝厩肥は最古の肥料＝化学肥料（1842）より前から在る並存レシピ   // ★樹皮の粉末（2026-09-30 (129)）＝キニーネの単離（1820）の前に欧州の薬局が樹皮を挽いて飲ませた（17世紀〜）

    /// <summary>★2026-10-01 段2A（発見の表＝行はレシピ・年代）：既定のレシピに発見の行がある財（発明の財・近代の既定の製法）の他のレシピは、
    /// 既定の行より早い年代の行か表に無い（地理差＝常にある）なら迂回＝公認の橋（SanctionedEarlyRecipes）だけが許される。</summary>
    [Fact]
    public void 並存レシピは既定レシピの解禁を迂回しない()
    {
        var sim = new MultiNationSim([NationConfig.Britain()], seed: 1);
        var w = sim.Nations[0].World;
        var violations = new List<string>();
        int gatedGoods = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Producers)
            {
                int defRow = Catalog.DiscoveryRowOf(p.GoodId, "");
                if (defRow < 0) continue;
                gatedGoods++;
                int row = p.DiscoveryRow;
                if (row >= 0 && Catalog.Discoveries[row].Decade >= Catalog.Discoveries[defRow].Decade) continue;
                if (SanctionedEarlyRecipes.Contains(p.RecipeName ?? "")) continue;
                violations.Add($"{r.Name}: {Catalog.Goods[p.GoodId].Name}({p.GoodId}) レシピ'{p.RecipeName}' 行={(row >= 0 ? Catalog.Discoveries[row].Decade.ToString() : "地理差")} < 既定 {Catalog.Discoveries[defRow].Decade}");
            }
        Assert.True(gatedGoods > 10, $"既定のレシピに発見の行がある生産者が少なすぎる（{gatedGoods}）＝検査が空回りしている");
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
