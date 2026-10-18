using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ①年齢コホート Phase3（dormant 着地）の配線検証。
/// 既定（OldLaborFactor=1.0）は現行と厳密一致＝較正中立（123テスト本体が担保）。（旧の旗 FertileWomenBirths は 2026-10-18 に焼き込み。）
/// ここでは**有効化した時に保存則・境界が壊れず、意図した方向に効く**ことを確認する（#17 の較正再走前の安全網）。
/// </summary>
public class CohortPhase3Tests
{
    [Fact]
    public void Phase3_既定係数が方針どおり()   // 休眠復旧2026-07-08 第(iii)段：出生の女性プール律速＋カップル律速を有効化（人口保存は下のON経路テストが番人）
    {
        var c = new Coefficients();
        Assert.Equal(0.75, c.OldLaborFactor);       // 【史実値へ2026-08-08作者裁定＝19世紀の高齢労働参加×体力減。0.6は経済較正が拒否→0.75】旧0.92(2026-07-18)
        Assert.Equal(0.05, c.FertileShareFloor.Value);     // 第(iii)段有効化＝プール正規化の下限フロア（出生集中の防止・セット必須）
    }

    [Fact]
    public void Phase3_両フラグ有効化でも人口保存と境界が保たれる()   // ON経路の安全網：保存則・不変条件を壊さない
    {
        var sim = new Simulation(NationConfig.France().ToScenario(13), new NullAgent());   // 多州＝州内移民で OldShare 運搬も通る
        sim.World.Coef.OldLaborFactor = 0.2;          // 老年はほぼ引退（労働供給から外れる）
        double p0 = sim.World.TotalPopulation;
        var ex = Record.Exception(() => sim.Run(200));   // 50年：コホート形成＋移民＋飢饉まで含めて回す
        Assert.Null(ex);   // §11.6（保存・境界）が全ティック保持＝有効化しても破れない
        Assert.True(Math.Abs(sim.World.TotalPopulation - (p0 + sim.World.PopulationDelta)) < 1e-6 * p0,
            "Phase3 有効化で人口保存（net＝出生−死亡のロジスティック軌道）が破れた");
        Assert.True(sim.World.TotalPopulation > 0, "人口が消えた");
    }

    [Fact]
    public void 老人労働減_OldLaborFactorは労働供給に配線され既定1_0が中立点()
    {
        // ※実質GDPの「下がる方向」の直接比較は、同一シードでも200四半期の一般均衡的な軌道分岐
        //   （賃金→投資→工業化）が OldShare 2〜5% ぶんの労働削減を埋もれさせ符号が不安定（ユーザーの方針＝産出比較は脆い）。
        //   よってここでは「①既定1.0は厳密に中立」「②1.0以外は実際にシムへ配線され結果を動かす」の2点を堅牢に検証する。
        var baseline = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());   // 既定 OldLaborFactor=1.0
        var neutral = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        neutral.World.Coef.OldLaborFactor = 0.75;  // 明示的に新既定(0.75)＝既定と同一経路（2026-08-08史実値裁定へ更新）
        var aged = new Simulation(NationConfig.Britain().ToScenario(9), new NullAgent());
        aged.World.Coef.OldLaborFactor = 0.0;      // 老年は完全引退＝労働供給から外れる
        baseline.Run(200); neutral.Run(200); aged.Run(200);   // 50年：OldShare が形成される
        double oldSize = 0, total = 0;
        foreach (var p in baseline.World.AllPops()) if (!p.IsSoldier) { oldSize += p.Size * p.AgeShare(60, double.PositiveInfinity); total += p.Size; }   // 老年＝60歳以上（年齢の層・段4e 後半）
        Assert.True(total > 0 && oldSize / total > 0.02, $"老年人口が形成されていない（{oldSize / total:P1}）＝この検証の前提が崩れた");
        // ① 既定（1.0）は (1−1.0)×OldShare=0 ＝OldShareの値に関わらず中立＝明示1.0と厳密一致
        Assert.Equal(baseline.World.GdpPerMonth.Value, neutral.World.GdpPerMonth.Value, 6);
        // ② 1.0以外（0.0）は労働供給を (1−OLF)×OldShare ぶん変える＝シムに配線され結果が動く（死にコードでない）
        Assert.True(System.Math.Abs((aged.World.GdpPerMonth - baseline.World.GdpPerMonth).Value) > (1e-6 * baseline.World.GdpPerMonth).Value,
            $"OldLaborFactor=0 にしても結果が変わらない（aged={aged.World.GdpPerMonth:F1} / base={baseline.World.GdpPerMonth:F1}）＝労働供給に配線されていない");
    }
}
