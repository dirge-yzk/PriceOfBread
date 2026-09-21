using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// 植民地POP（現地住民の実体）と独立：搾取の分け前が住民の蓄えになり（捕囚市場の財布）、
/// 搾取×識字（開発との接触）で不満が育つ。圧力（人口×不満）が本国の抑え（軍×即応度）を超えた大植民地は独立する。
/// 住民は本国の利益団体に属さない（台帳の外）。
/// </summary>
// ⚠2026-08-26：同上。植民地POP（住民・搾取・不満・独立）は Colony オブジェクトの機構なので、構築時の一括昇格を落として検証する。
public class ColonialPopTests
{
    [Fact]
    public void 植民地に住民が立ち搾取の分け前と不満が育つ()
    {
        // 外交を凍結＝戦争/侵略で植民地が移らない（装備された軍で植民地マップが動的になったため、本テストは植民地POP機構を戦争から隔離して検証する）。
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83, coef: new Coefficients { ColoniesAreRegions = 0 }) { DiplomacyFrozen = true };
        sim.Run(60);   // 15年＝採取が流れ不満が育ち始めるが、まだ独立線（0.7）には届かない頃
        var bengal = sim.Nations[0].World.SelfPower!.ColonyList.FirstOrDefault(c => c.Name == "中核ベンガル");
        Assert.NotNull(bengal);
        Assert.True(bengal!.PopSize > 0, "植民地に住民が居ない");
        Assert.True(bengal.Militancy > 0, "搾取されているのに不満が育たない");
        Assert.True(bengal.Savings >= 0, "住民の蓄えが負");
    }

    [Fact]
    public void 不満が極まり抑えを超えた大植民地は独立する()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 89, coef: new Coefficients { ColoniesAreRegions = 0 });
        var power = sim.Nations[0].World.SelfPower!;
        var bengal = power.ColonyList.First(c => c.Name == "中核ベンガル");
        bengal.PopSize = 50_000; bengal.Militancy = 1.0;   // 巨大植民地が沸騰している状態を注入
        int before = power.Colonies;
        sim.Run(4);                                         // 独立判定は月次（Colonial.Step）
        Assert.True(power.Colonies < before, "圧力が抑えを超えているのに独立が起きない");
        Assert.DoesNotContain(power.ColonyList, c => c.Name == "中核ベンガル");
        Assert.Contains(sim.Diplomacy.Log, l => l.Contains("独立"));
    }

    [Fact]
    public void 序盤は不満が独立線に届かず離反は起きない()   // 不満の蓄積は数十年単位（戦争での植民地の移転は起きてよい）
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 97, coef: new Coefficients { ColoniesAreRegions = 0 });
        sim.Run(40);   // 10年＝不満はまだ独立線（0.7）に届かない
        Assert.DoesNotContain(sim.Diplomacy.Log, l => l.Contains("独立"));
    }
}
