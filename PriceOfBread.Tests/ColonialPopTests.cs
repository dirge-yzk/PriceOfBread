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

    /// <summary>★2026-09-28（植民地の費用の側・R2）：植民地の維持費（距離の物理消費）と海運占有は、行（州）の数でなく質量（人口ウェイト）で数える。
    /// 資源の無い遠い前哨（英の前哨の中で最も遠い州）は、それより近い最大の植民地より維持費も海運占有も小さい
    /// （旧＝行ごとに同じ重み＝遠い前哨の方が大きかった・英1783 の距離の71%が資源の無い前哨16州）。
    /// ⚠この番人は構築時の一括昇格（既定 ColoniesAreRegions=1）の盤面で測る＝植民地格の州。</summary>
    [Fact]
    public void 維持費と海運占有は行の数でなく質量で数える_資源の無い遠い前哨は近い大植民地より小さい()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.France()], seed: 83) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World;
        w.ShipSteamShareEma = 1.0;   // 補給線が石炭を焚く状態（帆走の時代は距離の物理消費が0＝比べられない）
        double Km(Region r) { var cc = Places.Coord(r.Name); return Geo.SeaDistanceKm(w.Latitude, w.Longitude, cc.Lat, cc.Lon, false, false); }
        var cols = w.Regions.Where(r => r.State == RegionState.Active && r.Tier == RegionTier.Colonial).ToList();
        var outpost = cols.Where(r => r.Resources.Length == 0).OrderByDescending(Km).First();
        var big = cols.Where(r => Km(r) < Km(outpost)).OrderByDescending(r => r.SumPopulation()).First();
        Assert.True(Km(outpost) > Km(big) && outpost.SumPopulation() < big.SumPopulation(),
            $"前提が崩れている（前哨 {outpost.Name} {Km(outpost):F0}km・{outpost.SumPopulation():F0}／大植民地 {big.Name} {Km(big):F0}km・{big.SumPopulation():F0}）");
        (double Upkeep, double Burden) Only(Region keep)
        {
            foreach (var r in cols) r.Tier = r == keep ? RegionTier.Colonial : RegionTier.Integrated;
            Colonial.Step(w, w.Coef);
            foreach (var r in cols) r.Tier = RegionTier.Colonial;
            return (w.LastColonialUpkeep, w.ColonialShipBurden);
        }
        var o = Only(outpost);
        var b = Only(big);
        Assert.True(o.Burden > 0 && o.Upkeep > 0, $"前哨の維持費か海運占有が0（{o}）＝比べられない");
        Assert.True(o.Burden < b.Burden, $"遠い前哨 {outpost.Name} の海運占有 {o.Burden:G4} が近い大植民地 {big.Name} の {b.Burden:G4} 以上＝行の数で数えている");
        Assert.True(o.Upkeep < b.Upkeep, $"遠い前哨 {outpost.Name} の維持費の請求 {o.Upkeep:G4} が近い大植民地 {big.Name} の {b.Upkeep:G4} 以上＝行の数で数えている");
    }
}
