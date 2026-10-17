using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>国家選択画面の「おすすめと難易度」（docs/202608/おすすめ国家と難易度_2026-08-09_完了.md【完了】.md）の番人。
/// 固定するのは2点＝①3列×2カ国に必ず割れる（空の列を作らない）（旧②＝地理の判定 IsIsolated はテストだけの呼び手で 2026-09-26 に削除＝x-04）
/// ②現時点の割り当て（＝ピン。係数が動いて順位が入れ替わったらここが赤くなる＝**ラベルが黙って変わらない**）。</summary>
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

    /// <summary>現時点の割り当て（2026-08-11の係数での実測）。
    /// ⚠これは**固定値の宣言ではなくピン**＝AdminCostPerPop / SecessionHoldPerPop を機構へ置き換えると
    /// 清とロシアの難易度は下がりうる（同doc §5-2）。赤くなったら新しい順位が妥当かを確かめて、ここを取り直す。
    /// ★2026-09-29 取り直し（開幕の国庫を史実の桁へ＝world.json の treasury）：資源の余裕（国庫が行政費だけで持つ月数）が
    /// 米 1,709→5ヶ月（年GDP の0.5%）・蘭 6,667→48ヶ月（年GDP の3%）・普 1,131→274ヶ月（フリードリヒ2世の戦時財宝＝年GDP の3割）・日 167→21ヶ月
    /// ＝簡単が 米・蘭→普・日（米・蘭は普通へ）。史実とも合う＝1783年の米は連合規約の下で課税権を持たない破産した政府・蘭は第4次英蘭戦争の敗戦の直後で
    /// 国債は年GDP の約1.6・普は列強で最大の蓄え・日は海に隔てられた安全。</summary>
    /// <summary>★(235) 安全の項は**構築した世界の兵の数**を読む（旧＝world.json の人口の階級〔農民＋都市労働者〕の写し）。読み手は渡された兵の数だけで決まり、config の人口は読まない。
    /// ⚠突然変異＝安全の項を c.Peasant＋c.Worker へ戻すと、兵の数を入れ替えても素点が動かず赤になる。</summary>
    [Fact]
    public void 安全の項は構築した世界の兵の数を読む()
    {
        var all = WorldData.Nations();
        var coef = new Coefficients();
        var prussia = System.Array.Find(all, n => n.Name == "プロイセン")!;
        var open = NationBrief.Opening(all, coef);
        Assert.Equal(all.Length, open.Soldiers.Count);
        foreach (var n in all) Assert.True(open.Soldiers[n.Name] > 0, $"{n.Name} の兵が0");   // 構築の播種は全ての国で兵を置く
        double real = NationBrief.Terms(prussia, all, coef, open.Soldiers).Safety;
        // 同じ config・同じ人口のまま、プロイセンの兵だけを 1000 倍にすると安全の項は上がる（人口を読んでいれば動かない）
        var boosted = new Dictionary<string, double>(open.Soldiers) { [prussia.Name] = open.Soldiers[prussia.Name] * 1000.0 };
        Assert.True(NationBrief.Terms(prussia, all, coef, boosted).Safety > real + 0.5, "兵の数を変えても安全の項が動かない＝人口を読んでいる");
        // 隣国の兵だけを 0 にすると（陸続きの脅威なし）安全の項は 1.0
        var calm = new Dictionary<string, double>(open.Soldiers);
        foreach (var n in all) if (n.Name != prussia.Name) calm[n.Name] = 0;
        Assert.Equal(1.0, NationBrief.Terms(prussia, all, coef, calm).Safety, 9);
        // 渡さなくても同じ世界を組んで同じ値（選択画面と診断が同じ1本）
        Assert.Equal(real, NationBrief.Terms(prussia, all, coef).Safety, 12);
    }

    /// <summary>★(388) 規則のある国（world.json の treasuryMonths）の難易度の資源の項は月数そのものを読む（絶対額 treasury は 0 の置き場＝読むと全ての国が 0 か月になる）。
    /// 期待値は表の値（measurements/e7_20261014_values.tsv＝英 2.5・プロイセン 28）。⚠壊し方＝規則を読まず絶対額だけ読む → 赤。</summary>
    [Fact]
    public void 開幕の国庫の月数は規則のある国では規則の月数を読む()
    {
        var coef = new Coefficients();
        Assert.Equal(2.5, NationBrief.TreasuryMonths(WorldData.ByName("イギリス"), coef));
        Assert.Equal(28.0, NationBrief.TreasuryMonths(WorldData.ByName("プロイセン"), coef));
        Assert.Equal(0.0, WorldData.ByName("イギリス").Treasury);   // 前提＝絶対額は置き場の 0（これを読む旧の式なら 0 か月になる）
    }

    // ★(388) 開幕の国庫が史実の比（歳入×月数・多くの国で 2.5 か月・プロイセンは 28 か月）で置かれるようになり、旧の絶対額（プロイセン 274 か月ほか）の資源の項が消えた＝順位が動いた
    //   （アメリカ 普通→簡単・プロイセン 簡単→普通）。これは選択画面の札の現時点の割り当て（創発する物の記録）で、機構の不変条件ではない＝期待値を現時点へ更新した。
    [Theory]
    [InlineData("アメリカ", "簡単")]
    [InlineData("ネーデルラント", "普通")]
    [InlineData("プロイセン", "普通")]
    [InlineData("日本", "簡単")]
    [InlineData("イギリス", "普通")]
    [InlineData("ロシア", "普通")]
    [InlineData("清", "挑戦的")]
    public void 難易度の現時点の割り当て(string name, string difficulty)
        => Assert.Equal(difficulty, NationBrief.Difficulties(WorldData.Nations(), new Coefficients())[name]);
}
