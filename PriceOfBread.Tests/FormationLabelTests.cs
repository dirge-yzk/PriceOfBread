using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1316) c137sm 項 7 sb：Formations.FormedElsewhere は他の国の今の名（RenameNation が付けた規則の NewName）から規則を引き、札（文化・文化の無い規則は規則の名）と段で比べる。
/// 引く先の規則が名前で複数あっても同じ札である（＝どの規則を引いても札の比べの結果が同じ）ことを、規則の表から数えて固定する。
/// 段は同じとは限らない：マラーター帝国だけ Formations.Rules の Tier 2 と CultureRealms の大名の Tier 3 に分かれる（実測・名簿 <see cref="TierSplitNames"/> で固定＝増減したら赤）。
/// 突然変異：同じ NewName で札の違う規則を 1 本足す → 赤（試験の中で表を作る版で確かめる）。</summary>
public class FormationLabelTests
{
    private static readonly string[] TierSplitNames = ["マラーター帝国"];

    /// <summary>表（規則の列）から、同じ NewName の規則が札で食い違う名前の一覧を返す（本体と同じ札の式 CultureIs ?? NewName）。</summary>
    internal static List<string> Conflicts(IEnumerable<FormationRule> rules)
        => rules.GroupBy(r => r.NewName)
            .Where(g => g.Select(r => r.CultureIs ?? r.NewName).Distinct().Count() > 1)
            .Select(g => g.Key).ToList();

    /// <summary>同じ NewName の規則が段で食い違う名前の一覧。</summary>
    internal static List<string> TierSplits(IEnumerable<FormationRule> rules)
        => rules.GroupBy(r => r.NewName).Where(g => g.Select(r => r.Tier).Distinct().Count() > 1).Select(g => g.Key).OrderBy(x => x).ToList();

    [Fact]
    public void 同じ名の規則は札が同じ_段の食い違いは名簿のとおり()
    {
        var rules = Formations.Enumerate().ToList();
        Assert.True(rules.Count >= 5, $"対照：規則が少ない（{rules.Count}）");
        Assert.True(rules.GroupBy(r => r.NewName).Any(), "対照：名前が無い");
        Assert.Empty(Conflicts(rules));
        // 自己試験（検査の道具の突然変異）：札の違う同名の規則を足すと食い違いが出る
        var r0 = rules[0];
        var bad = rules.Append(r0 with { CultureIs = (r0.CultureIs ?? r0.NewName) + "_x" }).ToList();
        Assert.Equal(new[] { r0.NewName }, Conflicts(bad));
        Assert.Equal(TierSplitNames, TierSplits(rules));   // 名簿が古い（増えた・減った）なら赤
        var bad2 = rules.Append(r0 with { Tier = r0.Tier + 1 }).ToList();
        Assert.Contains(r0.NewName, TierSplits(bad2));
    }
}
