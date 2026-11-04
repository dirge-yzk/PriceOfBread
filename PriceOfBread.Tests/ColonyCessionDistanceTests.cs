using System.Linq;
using PriceOfBread.SimCore;
using Xunit;
using Xunit.Abstractions;

namespace PriceOfBread.Tests;

/// <summary>★(696) 線 hcd：講和の植民地格の州の複数割譲の費用に距離の項を戻す規則の番人（旗 HcdColonyCessionDistance は (817) で焼き込み＝常に距離の項を掛ける）。
/// 盤面は手で置いた艦の強さ（Military×NavalShare・凍結した外交では月次の同期が走らない）と戦勝点だけ＝期待値は実装の式を写さず、〈世界最大の艦を持つ勝者は距離の項が 0〉と〈遠く高い州ほど費用が増え予算で買えなくなる〉の向きで見る。</summary>
public class ColonyCessionDistanceTests
{
    private readonly ITestOutputHelper _out;
    public ColonyCessionDistanceTests(ITestOutputHelper o) { _out = o; }

    /// <summary>勝者＝フランス・敗者＝イギリス・第三者＝スペイン。艦の強さは 勝者 w・敗者 l・第三者 t を直接置く。講和（戦勝点 1.0＝割譲の線 1.2 未満・白紙でない）で勝者が受け取った敗者の植民地格の州の名前を返す。</summary>
    private (string[] moved, int regionsTotal, double popTotal) Peace(double w, double l, double t)
    {
        var sim = new MultiNationSim(System.Array.ConvertAll(new[] { "フランス", "イギリス", "スペイン" }, OpeningFixture.Named), seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
        var fr = sim.Nations[0].World; var uk = sim.Nations[1].World; var es = sim.Nations[2].World;
        foreach (var (p, nav) in new[] { (fr.SelfPower!, w), (uk.SelfPower!, l), (es.SelfPower!, t) })
        { p._fleetDriven = false; p.Military = 1000; p.NavalShare = new Share(nav / 1000.0); p.NavySkillMul = 1.0; }
        foreach (var p in sim.Diplomacy.Powers) if (p.Nation is null) p.Military = 0;   // 抽象標的は艦を持たない
        var ukCol = uk.Regions.Where(r => r.Tier == RegionTier.Colonial && r.Id != 0).Select(r => r.Name).ToHashSet();
        Assert.Contains("マドラス", ukCol);   // 前提：高い（人口単位 6 超）遠い（海路 16,000km 超）植民地格の州
        double pop0 = fr.Regions.Sum(r => r.SumPopulation()) + uk.Regions.Sum(r => r.SumPopulation());
        int n0 = fr.Regions.Count + uk.Regions.Count;
        var war = new War { A = fr.SelfPower!, B = uk.SelfPower!, Score = 1.0 };
        sim.Diplomacy.ResolvePeace(sim.Coef, war);
        var moved = fr.Regions.Where(r => ukCol.Contains(r.Name)).Select(r => r.Name).OrderBy(x => x, System.StringComparer.Ordinal).ToArray();
        return (moved, fr.Regions.Count + uk.Regions.Count - n0, fr.Regions.Sum(r => r.SumPopulation()) + uk.Regions.Sum(r => r.SumPopulation()) - pop0);
    }

    [Fact]
    public void 旗1は世界最大の艦を持つ勝者には距離の項が0で旗0と同じ州が移る()
    {
        var b = Peace(100, 10, 50);   // 勝者 100 が世界最大（第三者 50）
        Assert.Equal(new[] { "マドラス" }, b.moved);
    }

    [Theory]
    [InlineData(1000.0)]   // 勝者の艦は世界最大の 0.1
    [InlineData(300.0)]    // 0.33
    public void 旗1は艦が世界最大に及ばない勝者の遠く高い州を予算から外す(double third)
    {
        var (moved, dRegions, dPop) = Peace(100, 10, third);
        Assert.DoesNotContain("マドラス", moved);   // 海路 16,000km 超×(1−艦の世界比) の項で費用が予算 100 を超える
        Assert.NotEmpty(moved);                    // 予算に収まる別の州が移る（講和そのものは起きる）
        Assert.Equal(0, dRegions); Assert.Equal(0.0, dPop, 6);
    }

    [Fact]
    public void 旗1は距離込みの費用で予算を使い切り2件目が買えなくなる()
    {
        // 勝者 100・第三者 300（艦の世界比 1/3）。カナダ（基本 60・海路 6,600km 超）は距離込みで約 87 ＝予算 100 の大半を使う。
        // 残りは 13 弱＝基本費用 30 の州も買えない。予算を距離を除いた基本の費用で引くと、残り約 40 で海路の近い無資源の州（基本 30）が 2 件目として移る。
        var (moved, dRegions, dPop) = Peace(100, 10, 300);
        Assert.Equal(new[] { "カナダ" }, moved);
        Assert.Equal(0, dRegions); Assert.Equal(0.0, dPop, 6);
    }
}
