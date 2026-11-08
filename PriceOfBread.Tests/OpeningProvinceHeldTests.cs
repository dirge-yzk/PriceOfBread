using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(956) c85hs：開幕の州（植民地格でない州）は全て Core 格・支配度 1.0 で構築される
/// （heldSince のデータと HeldSinceFromData を消した後の固定）。期待値は定数（1.0・Core）で、構築の経路とは別の出所。</summary>
public class OpeningProvinceHeldTests
{
    [Fact]
    public void 開幕の植民地格でない州は全てCoreで支配度1()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1UL, _ => new UtilityAgent());
        int n = 0;
        foreach (var nation in sim.Nations)
            foreach (var r in nation.World.Regions.Where(r => r.Tier != RegionTier.Colonial))
            {
                Assert.True(r.Tier == RegionTier.Core, $"{nation.World.SelfPower!.Name}・{r.Name}：格 {r.Tier}");
                Assert.True(r.Dominance == 1.0, $"{nation.World.SelfPower!.Name}・{r.Name}：支配度 {r.Dominance}");
                n++;
            }
        Assert.True(n > 150);
    }
}
