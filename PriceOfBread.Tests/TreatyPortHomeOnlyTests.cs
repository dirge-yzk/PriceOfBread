using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1138) c111tp 旗 TreatyPortHomeOnly の機構の番人（事前登録 measurements/m1138_20261114_prereg_tp.txt §4）。
/// 旗 1＝条約港の読み手（<c>Politics.ForeignPortShare</c>・<c>TreatyPortInForce</c>）が植民地格の港を数えない。Core と Integrated の港は数える。
/// 盤面は Concert の英（開幕に外国の取り分は無い＝前提で確かめる）に合成の取り分を置く。期待は試験の中で港の人口から組む。</summary>
public class TreatyPortHomeOnlyTests
{
    static World Britain()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        var w = sim.Diplomacy.Powers.First(p => p.Nation is { } && p.Name.Contains("イギリス")).Nation!;
        Assert.All(w.Regions, r => Assert.DoesNotContain(r.ForeignShares, x => x > 0));   // 前提：開幕の外国の取り分は無い
        return w;
    }

    [Fact]
    public void 植民地格の港だけの持分では旗1で条約港が立たず関税も目減りしない()
    {
        var w = Britain();
        var col = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Colonial && r.SumPopulation() > 0);
        Politics.AddForeignShare(col, "テスト国", 0.5);
        w.Coef.TreatyPortHomeOnly = 0;   // 同じ盤面の対照（旧の規則）
        Assert.True(w.UnequalTreatyInForce);
        Assert.True(Politics.ForeignPortShare(w) > 0);
        w.Coef.TreatyPortHomeOnly = 1;
        Assert.False(w.UnequalTreatyInForce);
        Assert.Equal(0.0, Politics.ForeignPortShare(w), 15);
        Assert.Equal(1.0, InternationalMarket.TreatyPortTariffScale(w), 15);
    }

    [Fact]
    public void 本国格と統合の港の持分なら旗1でも条約港が立つ()
    {
        var w = Britain();
        w.Coef.TreatyPortHomeOnly = 1;
        var core = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Core && r.SumPopulation() > 0);
        Politics.AddForeignShare(core, "テスト国", 0.5);
        Assert.True(w.UnequalTreatyInForce);
        core.ForeignShares[0] = 0;
        Assert.False(w.UnequalTreatyInForce);
        var integ = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Colonial && r.SumPopulation() > 0);
        integ.Tier = RegionTier.Integrated;   // 合成の盤面：開幕に Integrated の港が無くても規則を確かめる
        Politics.AddForeignShare(integ, "テスト国", 0.5);
        Assert.True(w.UnequalTreatyInForce);
    }

    [Fact]
    public void 旗1の関税の倍率は本国格と統合の港だけの人口加重()
    {
        var w = Britain();
        w.Coef.TreatyPortHomeOnly = 1;
        var core = w.Regions.First(r => r.IsPort && r.Tier == RegionTier.Core && r.SumPopulation() > 0);
        var cols = w.Regions.Where(r => r.IsPort && r.Tier == RegionTier.Colonial && r.SumPopulation() > 0).ToList();
        Assert.True(cols.Count >= 2, "前提：英は人の居る植民地格の港を 2 つ以上持つ");
        var integ = cols[0]; var col = cols[1];
        integ.Tier = RegionTier.Integrated;
        Politics.AddForeignShare(core, "テスト国", 0.5);
        Politics.AddForeignShare(integ, "テスト国", 0.5);
        Politics.AddForeignShare(col, "テスト国", 0.5);
        double homePop = w.Regions.Where(r => r.IsPort && r.Tier != RegionTier.Colonial && r.SumPopulation() > 0).Sum(r => r.SumPopulation());
        double want = 0.5 * (core.SumPopulation() + integ.SumPopulation()) / homePop;
        Assert.Equal(want, Politics.ForeignPortShare(w), 12);
        Assert.Equal(1.0 - want, InternationalMarket.TreatyPortTariffScale(w), 12);
        w.Coef.TreatyPortHomeOnly = 0;   // 同じ盤面の対照：旧の規則は植民地格の港も数える＝値が違う
        Assert.NotEqual(want, Politics.ForeignPortShare(w), 12);
    }
}
