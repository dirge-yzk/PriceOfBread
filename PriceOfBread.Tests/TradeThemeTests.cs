using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// テーマI（国際貿易・植民地）の機能3項目（拿捕賞金の積荷連動・世界価格の費用アンカー・内陸港距離減衰）の dormant 配線検証。
/// 既定（全係数0）は現行恒等＝較正中立（134本体＋既存MultiNationTestsが担保）。有効化時の保存則を確認する。
/// </summary>
public class TradeThemeTests
{
    private static NationConfig[] FourNations() =>
        [NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()];

    [Fact]
    public void テーマI_既定係数が方針どおり()   // 休眠復旧2026-07-08：A復旧（トリアージ文書）で拿捕実積荷・内陸距離減衰は有効化＝新既定を番人化
    {
        var c = new Coefficients();
        Assert.Equal(1.0, c.PrizeCargoShare);          // A-5有効化（賞金は実積荷基準）
        Assert.Equal(1.0, c.InlandPortDistanceDecay);  // A-4有効化（港湾集約の創発）
    }

    [Fact]
    public void テーマI_全機能係数有効化でも保存則が保たれる()
    {
        var sim = new MultiNationSim(FourNations(), seed: 1789);
        sim.Coef.PrizeCargoShare = 1.0;             // 拿捕賞金の積荷連動
        sim.Coef.InlandPortDistanceDecay = 1.0;     // 内陸港距離減衰（NearestPortDistanceKm=0なら不発だが係数経路は走る）
        var ex = Record.Exception(() => sim.Run(200));   // §11.6（貨幣・人口保存と境界）が全ティック・全国で保持
        Assert.Null(ex);
        // 2026-07-26 不可侵協定の機能削除に伴い緩和：低緊張ペアの凍結が無くなり、4カ国閉世界の50年では
        // 戦争由来の分離独立（Nations増加）や全土割譲による人口0の国が創発しうる＝正当な帰結でありリスト自体からは消えない。
        // 守るべき保存則の番人＝負/NaNの検出と「世界の人口が消えない」に限定（貨幣・人口保存の本体は Run 内の§11.6検査＝上の Record.Exception が担保）。
        Assert.True(sim.Nations.Count >= 4, "国がリストから消えた");
        Assert.True(sim.Nations.TrueForAll(n => n.World.TotalPopulation >= 0 && double.IsFinite(n.World.TotalPopulation)), "人口が負または非有限（保存則破れ）");
        Assert.True(sim.Nations.Exists(n => n.World.TotalPopulation > 0), "世界の人口が消えた");
    }
}
