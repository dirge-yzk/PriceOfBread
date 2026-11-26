using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1345) c141cn：Coercion の式を Politics.CoercionNow へ移した（式は同じ・掛ける順も同じ）。盤面を試験の中で置く（支払い率＜1・正統性＜1・戦時で疲弊＞0）。
/// 期待値は試験の中で手で書いた式（BaseCoercion×正統性×支払い率×(1＋戦時の結束−疲弊の代価)）＝エンジンの関数を呼ばない。
/// 突然変異：支払い率を落とす・warDrain を落とす・正統性を落とす＝どれも赤（実装ログ (1345)）。
/// (1385) c147wb：旗 WarWearyPoliticsCut を焼き込んだ＝旗 0 の側（戦時の乗数を係数で数える式）は消えた。期待値は BaseCoercion×正統性×支払い率だけ（戦時でも厭戦でも乗数 1）。
/// 旧の式の番人（旗 0 の行・戦時と平時の比の旗 0 の行）は、旧の規則そのものを固定していた＝旗と一緒に消した（★(826)②）。
/// 突然変異 M1（CoercionNow へ戦時の乗数を戻す）→戦時・厭戦＞0 の行が赤。</summary>
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

    private static double 手で数えた式(World w)
        => w.BaseCoercion * w.Legitimacy * w.FiscalFundingRatio.Value;   // 戦時の乗数は 1（厭戦も戦時も読まない）

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CoercionNowは手で数えた式と一致し_Stepの後のCoercionとも一致する(bool war)
    {
        const double exh = 0.2;
        var (sim, w) = 盤面(legitimacy: 0.6, funding: 0.7, baseCoercion: 0.5, exhaustion: exh, war: war);
        var c = w.Coef;
        // 前提：三つの量がどれも 1 でない・厭戦が正（旧の式なら乗数が 1 でなくなる盤面）
        Assert.True(w.Legitimacy < 1.0 && w.FiscalFundingRatio.Value < 1.0 && w.SelfPower!.WarExhaustion > 0);
        Assert.Equal(war, Government.AtWar(w));
        double now = Politics.CoercionNow(w, c);
        Assert.Equal(手で数えた式(w), now, 12);
        Politics.Step(w, c);
        Assert.Equal(手で数えた式(w), w.Coercion, 12);   // Step が書いた値＝Step の後の盤面で手で数えた式
        Assert.Equal(Politics.CoercionNow(w, c), w.Coercion, 12);
    }

    [Fact]
    public void 戦時と平時で保持力が同じ()
    {
        const double exh = 0.2;
        var (_, wWar) = 盤面(0.6, 0.7, 0.5, exh, war: true);
        var (_, wPeace) = 盤面(0.6, 0.7, 0.5, exh, war: false);
        Assert.Equal(1.0, Politics.CoercionNow(wWar, wWar.Coef) / Politics.CoercionNow(wPeace, wPeace.Coef), 12);
    }
}
