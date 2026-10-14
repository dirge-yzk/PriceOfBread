using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (333) 初期値 段4 C2＝南アジアの人口（measurements/sa333_20261014_prereg.txt・spec.py）。
/// 史実＝Broadberry, Custodis &amp; Gupta 2015 Table 1（パキスタン・バングラデシュ込みの亜大陸）：1750=190・1801=207 百万。
/// 期待値の作り方＝この 2 数（表から転記した定数）だけ。world.json の倍率・開幕の総数の表は使わない（同語反復にしない）。
/// </summary>
public class SouthAsiaPopulationTests
{
    // 地図の窓（spec.py の REGION_NAMES と同じ 20 州）。国が変わっても名前で追う。
    static readonly HashSet<string> Names =
    [
        "プーナ", "ナーグプル", "グワーリヤル", "インドール", "バローダ", "バサイン",
        "シュリーランガパトナ", "バンガロール", "マンガロール",
        "ハイデラバード", "オーランガーバード", "ワランガル",
        "ラホール", "デリー", "ラクナウ",
        "中核ベンガル", "内陸ベンガル", "マドラス", "ボンベイ", "ポンディシェリ",
    ];

    [Fact]
    public void 回333_南アジアの開幕の総数はBCGの幅の内()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        double sum = 0; int found = 0;
        foreach (var n in sim.Nations)
            foreach (var r in n.World.Regions)
                if (Names.Contains(r.Name)) { sum += r.SumPopulation(); found++; }
        Assert.Equal(Names.Count, found);   // 20 州が全て見つかる（名前の取り違えで和が痩せて見えるのを防ぐ）
        const double Bcg1750 = 190e3, Bcg1801 = 207e3;   // Table 1（S）・単位は千人（SumPopulation と同じ）
        Assert.InRange(sum, Bcg1750, Bcg1801 * 1.02);
    }

    /// <summary>範囲外（ネパール・セイロン）と出典で置いた英領・仏領の州は触らない（(227) の値のまま）。</summary>
    [Fact]
    public void 回333_範囲外の勢力と英領は据え置き()
    {
        var np = WorldData.ByName("ネパール");
        Assert.Equal(3740.0, np.Nobility + np.Peasant + np.Worker + np.Bourgeois + np.Professional + np.Merchant + np.Artisan);
        double Col(string nation, string name) => WorldData.ByName(nation).InitialColonies.Single(c => c.Name == name).PopK;
        Assert.Equal(26000.0, Col("イギリス", "中核ベンガル") + Col("イギリス", "内陸ベンガル"));
        Assert.Equal(4000.0, Col("イギリス", "マドラス"));
        Assert.Equal(250.0, Col("イギリス", "ボンベイ"));
        Assert.Equal(170.0, Col("フランス", "ポンディシェリ"));
        Assert.Equal(817.0, Col("ネーデルラント", "セイロン"));
    }
}
