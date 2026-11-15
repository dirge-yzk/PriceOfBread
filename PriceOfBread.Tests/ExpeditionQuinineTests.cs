using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1167) 線 c115qc：遠征のキニーネ（熱帯の標的 RequiresQuinine への侵攻で要る分を首都の市場から買う・World.Invade）の機構の番人。
/// 旧 ColonialQuinineTests の Q3・Q4 の遠征の主張を、計器を使わない 1 本に移した。期待の数 40 は手で書いた数（缶詰の基礎 20＋標的の軍事力 1×20）＝同じ出所の式を写さない。
/// </summary>
public class ExpeditionQuinineTests
{
    [Fact]
    public void E1_熱帯の標的への遠征は首都のキニーネを求めの全量だけ買う()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 1789UL, coef: new Coefficients()) { DiplomacyFrozen = true };
        sim.StepQuarter();
        var w = sim.Nations[0].World; var me = w.SelfPower!;
        var target = sim.Diplomacy.Powers.Find(p => p.Nation is null && p.RequiresQuinine && p.ColonyList.Count > 0 && sim.Diplomacy.Suzerain(p) is null);
        Assert.NotNull(target);   // 前提：キニーネの標的（熱帯の抽象標的）がある
        w.ExpansionWill = 1e12; me.Military = 1e9; target!.Military = 1.0;
        w.FiscalFundingRatio = new Share(1.0);
        var m = w.Region.Markets;
        foreach (int g in new[] { Catalog.SailingShipId, Catalog.SteamshipId, Catalog.CannedFoodId }) m[g].Stock = 1e6;
        m[Catalog.QuinineId].Stock = 45.0; m[Catalog.QuinineId].Price = 1e-3;   // 資金の比 1・値段を安くして予算の制約を外す（旧 Board と同じ盤面）
        Assert.True(w.CanInvade(target, out string why), $"前提が崩れた：侵攻できない（{why}）");
        double q0 = m[Catalog.QuinineId].Stock;
        Assert.True(w.Invade(target), "前提が崩れた：侵攻しない");
        Assert.Equal(40.0, q0 - m[Catalog.QuinineId].Stock, 6);   // 求めの全量（在庫 45 の内）
    }
}
