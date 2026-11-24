using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1345) c141cn：Coercion の式を Politics.CoercionNow へ移した（式は同じ・掛ける順も同じ）。盤面を試験の中で置く（支払い率＜1・正統性＜1・戦時で疲弊＞0）。
/// 期待値は試験の中で手で書いた式（BaseCoercion×正統性×支払い率×(1＋戦時の結束−疲弊の代価)）＝エンジンの関数を呼ばない。
/// 突然変異：支払い率を落とす・warDrain を落とす・正統性を落とす＝どれも赤（実装ログ (1345)）。</summary>
public class CoercionNowTests
{
    private static (MultiNationSim sim, World w) 盤面(double legitimacy, double funding, double baseCoercion, double exhaustion, bool war)
    {
        var sim = new MultiNationSim([WorldData.ByName("フランス"), WorldData.ByName("スペイン")], seed: 1791UL) { DiplomacyFrozen = true };
        var w = sim.Nations[0].World; var es = sim.Nations[1].World;
        w.Coef.RegionalRevoltThreshold = 0;   // 一揆の鎮圧が正統性を削る経路を切る（Step の後の正統性を動かさない）
        w.PoliticsEnabled = true;
        w.BaseCoercion = baseCoercion;
        w.Legitimacy = legitimacy;
        w.FiscalFundingRatio = new Share(funding);
        w.SelfPower!.WarExhaustion = exhaustion;
        if (war) sim.Diplomacy.Wars.Add(new War { A = w.SelfPower!, B = es.SelfPower! });
        return (sim, w);
    }

    private static double 手で数えた式(World w, double exhaustion, bool war)
    {
        var c = w.Coef;
        double rally = war ? c.WarRallyCoercion * Math.Max(0, 1.0 - exhaustion / c.WarRallyDecayPoint) : 0;
        double mult = 1.0 + rally - c.WarCoercionDrain * exhaustion;
        return w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value * mult;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CoercionNowは手で数えた式と一致し_Stepの後のCoercionとも一致する(bool war)
    {
        const double exh = 0.2;
        var (sim, w) = 盤面(legitimacy: 0.6, funding: 0.7, baseCoercion: 0.5, exhaustion: exh, war: war);
        var c = w.Coef;
        // 前提：三つの乗数がどれも 1 でない＝どれを落としても式が変わる
        Assert.True(w.Legitimacy < 1.0 && w.FiscalFundingRatio.Value < 1.0 && w.SelfPower!.WarExhaustion > 0);
        Assert.Equal(war, Government.AtWar(w));
        double now = Politics.CoercionNow(w, c);
        Assert.Equal(手で数えた式(w, exh, war), now, 12);
        Assert.NotEqual(1.0, now / (w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value), 6);   // warDrain が 1 でない盤面
        Politics.Step(w, c);
        double exhAfter = w.SelfPower!.WarExhaustion;
        Assert.Equal(手で数えた式(w, exhAfter, war), w.Coercion, 12);   // Step が書いた値＝Step の後の盤面で手で数えた式
        Assert.Equal(Politics.CoercionNow(w, c), w.Coercion, 12);
    }

    [Fact]
    public void 戦時と平時で戦時の結束の項だけが違う()
    {
        const double exh = 0.2;
        var (_, wWar) = 盤面(0.6, 0.7, 0.5, exh, war: true);
        var (_, wPeace) = 盤面(0.6, 0.7, 0.5, exh, war: false);
        double ratio = Politics.CoercionNow(wWar, wWar.Coef) / Politics.CoercionNow(wPeace, wPeace.Coef);
        var c = wWar.Coef;
        Assert.Equal((1.0 + c.WarRallyCoercion * (1.0 - exh / c.WarRallyDecayPoint) - c.WarCoercionDrain * exh) / (1.0 - c.WarCoercionDrain * exh), ratio, 12);
    }
}
