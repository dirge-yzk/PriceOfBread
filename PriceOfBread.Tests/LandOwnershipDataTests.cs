using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(909) 線 c78ls：土地の持ち分の段 B-2a（world.json の名簿 7 国の <see cref="NationConfig.LandOwnership"/>）の番人。
/// 手書きの表は事前登録 measurements/m909_20261107_prereg_ls.txt [1] から手で写した（world.json から読まない）。値は AI の推定（出典は研究書名のみ・幅の中点を和 1 へ割り直し、国家の枠を貴族へ畳んだ値・確証なし）。
/// 枠の順＝貴族・教会・農民の自作・市民の地主。突然変異（使い捨ての木）は measurements/m909_20261107/mutation.txt。</summary>
public class LandOwnershipDataTests
{
    static readonly Dictionary<string, double[]> Table = new()
    {
        ["フランス"] = [0.265, 0.087, 0.378, 0.270],
        ["イギリス"] = [0.745, 0.045, 0.175, 0.035],
        ["スペイン"] = [0.488, 0.152, 0.228, 0.132],
        ["プロイセン"] = [0.610, 0.020, 0.076, 0.0, 0.294],   // ★(978) B-2b：5 個目＝国家の枠（事前登録 measurements/m978_20261109_prereg_cs.txt [1] 8 から手で写した。0.610＝材料の割り直しの 0.609 に丸めの残差 0.001 を貴族の枠へ足して和をちょうど 1 にした値＝同 [1] d）
        ["ロシア"] = [0.530, 0.0, 0.0, 0.0, 0.470],
        ["デンマーク"] = [0.905, 0.020, 0.075, 0.0],
        ["日本"] = [0.987, 0.013, 0.0, 0.0],
        ["ポーランド"] = [0.872, 0.128, 0.0, 0.0],
    };

    [Fact]
    public void 名簿の国だけが表のとおりの持ち分を持つ()
    {
        var cfgs = WorldData.Nations();
        int with = 0;
        foreach (var c in cfgs)
        {
            if (!Table.TryGetValue(c.Name, out var t)) { Assert.True(c.LandOwnership is null, $"名簿の外の {c.Name} が landOwnership を持つ（増やすときは表も直す）"); continue; }
            with++;
            Assert.NotNull(c.LandOwnership);
            Assert.Equal(t.Length, c.LandOwnership!.Length);   // ★(978) 長さ 4（貴族・教会・農民の自作・市民の地主）か 5（＋国家の枠）＝表の長さ
            double sum = 0;
            for (int k = 0; k < t.Length; k++) { Assert.Equal(t[k], c.LandOwnership[k]); Assert.True(c.LandOwnership[k] >= 0, $"{c.Name} 枠 {k} が負"); sum += c.LandOwnership[k]; }
            Assert.True(System.Math.Abs(sum - 1.0) <= 1e-12, $"{c.Name} の和 {sum} が 1 でない");
        }
        Assert.Equal(Table.Count, with);
    }

    /// <summary>構築（Simulation.BuildProvince）の写し：名簿の国の本国の州の非賦存・持ち主が貴族の RGO だけが表の値（和で割った値）の欄を別の配列で持つ。
    /// 植民地格の州（賦存の大農園＝<see cref="Colonial.AbsenteeRgo"/> を含む）と賦存の RGO と工場は null。
    /// <see cref="EconomyStep.PayOwnerRent"/> は不在地主の大農園を除外しない（輸出の側 AddRentRecipients だけが除外する）＝植民地格に欄が付かないことをこの番人で守る。</summary>
    [Fact]
    public void 名簿の国の構築は本国の非賦存の貴族のRGOにだけ欄を置き植民地格と賦存は持たない()
    {
        int absenteeAll = 0;
        foreach (var (name, t) in Table)
        {
            double tsum = 0; foreach (var v in t) tsum += v;   // ★(978) 長さ 5 の表も和で割る
            var w = new MultiNationSim([WorldData.ByName(name)], seed: 1789UL) { DiplomacyFrozen = true }.Nations[0].World;
            var seen = new List<double[]>();
            foreach (var r in w.Regions)
                foreach (var s in r.Producers)
                {
                    if (r.Tier == RegionTier.Colonial)
                    {
                        if (Colonial.AbsenteeRgo(r, s)) absenteeAll++;
                        Assert.True(s.LandShares is null, $"{name} の植民地格の州 {r.Name} の生産者が欄を持つ");
                        continue;
                    }
                    bool endow = System.Array.IndexOf(r.Resources, s.GoodId) >= 0;
                    bool want = s.IsRgo && !endow && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < r.Pops.Count && r.Pops[s.OwnerPopIndex].ClassId == PopClass.Noble;
                    if (!want) { Assert.True(s.LandShares is null, $"{name} の {r.Name} で欄を持つべきでない生産者（賦存 {endow}・RGO {s.IsRgo}）が欄を持つ"); continue; }
                    Assert.NotNull(s.LandShares);
                    Assert.Equal(t.Length, s.LandShares!.Length);
                    for (int k = 0; k < t.Length; k++) Assert.Equal(t[k] / tsum, s.LandShares[k], 12);
                    Assert.DoesNotContain(seen, x => ReferenceEquals(x, s.LandShares));
                    seen.Add(s.LandShares);
                }
            Assert.True(seen.Count > 0, $"試験の前提：{name} に欄を持つ RGO が無い");
        }
        Assert.True(absenteeAll > 0, "試験の前提：名簿の国に不在地主の大農園（植民地格の賦存の RGO）が無い");
    }
}
