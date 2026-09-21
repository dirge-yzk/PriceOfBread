using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>国家選択画面の「おすすめと難易度」（docs/202608/おすすめ国家と難易度_2026-08-09_完了.md【完了】.md）の番人。
/// 固定するのは3点＝①3列×2カ国に必ず割れる（空の列を作らない）②地理の判定が陸塊テーブルの真値から出る
/// ③現時点の割り当て（＝ピン。係数が動いて順位が入れ替わったらここが赤くなる＝**ラベルが黙って変わらない**）。</summary>
public class NationBriefTests
{
    [Fact]
    public void おすすめ7カ国は簡単2普通4挑戦的1に割れる()
    {
        var all = WorldData.Nations();
        var d = NationBrief.Difficulties(all, new Coefficients());
        Assert.Equal(7, d.Count);
        foreach (var name in NationBrief.Recommended)
        {
            Assert.True(d.ContainsKey(name), $"おすすめの{name}が world.json に見つからない（改名？）");
            Assert.False(string.IsNullOrEmpty(NationBrief.Description(name)), $"{name}のカード説明が空");
        }
        int easy = 0, normal = 0, hard = 0;
        foreach (var v in d.Values) { if (v == "簡単") easy++; else if (v == "普通") normal++; else hard++; }
        Assert.Equal(2, easy); Assert.Equal(4, normal); Assert.Equal(1, hard);   // 簡単2/普通4/挑戦的1（作者指示2026-08-11）
    }

    /// <summary>★地理＝本国州が他国と陸続きでないか。植民地は数えない（英の北米植民地でアメリカが陸続きにならない）。</summary>
    [Theory]
    [InlineData("イギリス", true)]
    [InlineData("アメリカ", true)]
    [InlineData("日本", true)]
    [InlineData("プロイセン", false)]
    [InlineData("ロシア", false)]
    [InlineData("清", false)]
    public void 投射圏の外にいるかは陸塊テーブルから出る(string name, bool isolated)
    {
        var all = WorldData.Nations();
        var c = System.Array.Find(all, n => n.Name == name)!;
        Assert.Equal(isolated, NationBrief.IsIsolated(c, all));
    }

    /// <summary>現時点の割り当て（2026-08-11の係数での実測）。
    /// ⚠これは**固定値の宣言ではなくピン**＝AdminCostPerPop / SecessionHoldPerPop を機構へ置き換えると
    /// 清とロシアの難易度は下がりうる（同doc §5-2）。赤くなったら新しい順位が妥当かを確かめて、ここを取り直す。</summary>
    [Theory]
    [InlineData("アメリカ", "簡単")]
    [InlineData("ネーデルラント", "簡単")]
    [InlineData("プロイセン", "普通")]
    [InlineData("日本", "普通")]
    [InlineData("イギリス", "普通")]
    [InlineData("ロシア", "普通")]
    [InlineData("清", "挑戦的")]
    public void 難易度の現時点の割り当て(string name, string difficulty)
        => Assert.Equal(difficulty, NationBrief.Difficulties(WorldData.Nations(), new Coefficients())[name]);
}
