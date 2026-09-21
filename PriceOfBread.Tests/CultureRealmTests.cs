using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>文化統一（CultureRealms）の番人（2026-08-13）。
///
/// 症状＝`Table` に world.json へ**存在しない文化キー**（「インド」「中国」）が2行あり、
/// 生成器（`Build`）の `ByCulture.TryGetValue` が外れて **1つも規則を作らないまま黙って通っていた**。
/// 同ファイルの冒頭コメントが「ここに無い文化は統一国を持たない」と宣言しているのに、それを自分で破っていた。
///
/// ここで固定するのは3つ：①表の文化キーが実在する ②首都が実在する州名 ③規則が実際に生成される。
/// 何もしない機構は何も壊さないので必ず緑になる＝③が無いと番人が空洞化する。</summary>
public class CultureRealmTests
{
    [Fact]
    public void 表の文化キーは全てworld_jsonに実在する()
    {
        var cultures = new HashSet<string>();
        var provinces = new HashSet<string>();
        foreach (var n in WorldData.Nations())
        {
            foreach (var p in n.Provinces) { if (!string.IsNullOrEmpty(p.Culture)) cultures.Add(p.Culture); provinces.Add(p.City); }
        }
        foreach (var realm in CultureRealms.Table)
        {
            Assert.True(cultures.Contains(realm.Culture),
                $"文化「{realm.Culture}」が world.json に無い＝この行は1つも規則を作らない死文（{realm.Name}）");
            Assert.True(provinces.Contains(realm.Capital),
                $"「{realm.Name}」の首都「{realm.Capital}」が州名として存在しない");
        }
        Assert.True(CultureRealms.Table.Length >= 4, "表が痩せすぎ＝ハーネスが空回りしている");
    }

    [Fact]
    public void 表の各文化が実際に規則を生む()
    {
        foreach (var realm in CultureRealms.Table)
        {
            bool any = false;
            foreach (var r in CultureRealms.Rules) if (r.CultureOrigin == realm.Culture) { any = true; break; }
            Assert.True(any, $"文化「{realm.Culture}」から統一規則が1つも生成されていない（{realm.Name}）");
        }
    }
}
