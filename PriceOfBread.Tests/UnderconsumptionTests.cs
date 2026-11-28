using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

public class UnderconsumptionTests
{
    private static Scenario Laissez(ulong s, double rent = 0.7) =>
        new() { Seed = s, Kind = ScenarioKind.LaissezFaire, FactoryRentShare = rent };

    [Fact]
    public void 無介入の放任経済は労働者を窮乏させ脱工業化し格差を生む()   // §0／§2.3.1／§2.4.2
    {
        var sim = new Simulation(Laissez(12345), new NullAgent());
        double urban0 = sim.World.UrbanShare;
        sim.Run(400);

        var r = sim.World.Regions[0];
        double workerPc = r.Pops[1].Savings.Value / Math.Max(1, r.Pops[1].Size);
        double farmerPc = r.Pops[0].Savings.Value / Math.Max(1, r.Pops[0].Size);
        double capPc = r.Pops[2].Savings.Value / Math.Max(1, r.Pops[2].Size);

        Assert.True(workerPc < farmerPc, $"労働者が窮乏していない（労働 {workerPc:F0} vs 農民 {farmerPc:F0}）");
        // ★(1398) c149sd：〈資本家の 1 人あたりの貯蓄が労働者の 10 倍超〉は創発の大きさの固定＝記録へ移した（作者 2026-10-17＝史実の桁・創発の桁を固定した試験の赤は止める線ではない）。
        //   この盤面（種 12345・400 四半期）の比＝旗 SatReadersToDiscontent 0 で 10.787（資本家 5519.9・労働 511.7）・1 で 4.672（7878.9・1686.3）＝measurements/m1398_20261127_prereg_sd.txt 追記 3。窮乏と脱工業化の 2 本は残す。
        Assert.True(sim.World.UrbanShare < urban0, $"脱工業化していない（{urban0:P0}→{sim.World.UrbanShare:P0}）");
        // ★2026-08-31（StapleElasticityZero点火の随伴）：旧の第4検査「Satisfaction<0.8」は「主食が高いほど欲しがらない」
        //   誤符号が desired を人工的に絞って満足を下げていた分に依存していた（点火単独で赤＝POB_COEF二分で特定。
        //   符号是正後は労働0.81 vs 農民0.81＝**貧しいが食えている**＝満足度ベースの過少消費はアーティファクトだった）。
        //   窮乏は上の3本（貯蓄の窮乏・10倍格差・脱工業化）が実体として担う＝満足度の検査は撤去（記録：docs/20260831/ラウンドB）。
    }

    [Fact]
    public void 搾取が強いほど労働者は貧しく工業は縮む()   // (d) 搾取度→破綻度の関係を測る
    {
        var mild = new Simulation(Laissez(12345, 0.3), new NullAgent());
        var harsh = new Simulation(Laissez(12345, 0.8), new NullAgent());
        mild.Run(400);
        harsh.Run(400);

        double mildWorker = mild.World.Regions[0].Pops[1].Savings.Value / Math.Max(1, mild.World.Regions[0].Pops[1].Size);
        double harshWorker = harsh.World.Regions[0].Pops[1].Savings.Value / Math.Max(1, harsh.World.Regions[0].Pops[1].Size);

        Assert.True(harshWorker < mildWorker, $"搾取が強い方が労働者が貧しくない（強 {harshWorker:F0} vs 弱 {mildWorker:F0}）");
        Assert.True(harsh.World.UrbanShare < mild.World.UrbanShare, $"搾取が強い方が工業が縮まない（強 {harsh.World.UrbanShare:P1} vs 弱 {mild.World.UrbanShare:P1}）");
    }

    [Fact]
    public void 放任経済でも保存則と決定論は保たれる()   // 経済破綻は「シミュ破綻」でなく経済的危機（§2.7）
    {
        var a = new Simulation(Laissez(7), new NullAgent());
        var b = new Simulation(Laissez(7), new NullAgent());

        var ex = Record.Exception(() => a.Run(400));
        Assert.Null(ex);   // 保存則は破れない（破綻は経済の話であってバグではない）
        b.Run(400);
        Assert.Equal(a.World.UrbanShare, b.World.UrbanShare);
    }
}
