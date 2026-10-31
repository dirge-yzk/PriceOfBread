using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(704) 線 r1：旗 <see cref="Coefficients.WarmupStopClassShift"/>（助走の間は州の中の階級遷移 <see cref="PopDecision.Run"/> と社会移動 <see cref="Meritocracy.Step"/> を回さない）の番人＝機構と不変条件。
/// 既定 0（今のまま）。①既定は 0 ②旗 0 の普通の助走は 9 国の工場労働者の割合を world.json から動かす（対照＝何もしない旗が緑にならない前提）③旗 1 の普通の助走は動かさない（徴兵だけが残る）
/// ④片方の門だけでは足りない（突然変異の代わりの対照＝焼く試走の腕の片割れ〔社会移動だけ・階級遷移だけ止める〕は ③ の線を割る）。
/// 期待値の出所は構築直後の盤面（world.json）＝旗 1 の助走の結果と同じ出所ではない。
/// 突然変異の証拠（段0 の短いテスト・種 1・measurements/r1_704_20261031/probe_decomp.txt）：どちらかの門を落とすと 9 国の最大が 0.176（階級遷移の門を落とす）または 0.037（社会移動の門を落とす）＝旗 1 の 0.0010 の閾値 0.005 を超えて赤。</summary>
public class WarmupClassShiftTests
{
    static MultiNationSim Fresh() => new(WorldData.Nations(), 1UL, _ => new UtilityAgent());
    static readonly string[] Nine = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"];

    static Dictionary<string, double> WorkerShare(MultiNationSim s)
    {
        var d = new Dictionary<string, double>();
        foreach (var n in s.Nations)
        {
            string name = n.World.SelfPower!.BaseName;
            if (!Nine.Contains(name)) continue;
            double w = 0, t = 0;
            foreach (var r in n.World.Regions) foreach (var p in r.Pops) { t += p.Size; if (p.ClassId == PopClass.Worker) w += p.Size; }
            d[name] = w / t;
        }
        return d;
    }

    static double MaxDiff(Dictionary<string, double> a, Dictionary<string, double> b) => Nine.Max(k => Math.Abs(a[k] - b[k]));

    [Fact]
    public void 旗は既定0()
    {
        Assert.Equal(0.0, new Coefficients().WarmupStopClassShift);
    }

    [Fact]
    public void 旗0の助走は工場労働者の割合を動かし_旗1の助走は動かさない_片割れだけの停止は足りない()
    {
        var open = WorkerShare(Fresh());
        var off = Fresh(); foreach (var n in off.Nations) n.World.Coef.WarmupStopClassShift = 0;   // 旗 0 を明示（既定を点火しても対照が崩れない）
        off.StartWarmup();
        var on = Fresh(); foreach (var n in on.Nations) n.World.Coef.WarmupStopClassShift = 1;   // 国ごとに別の係数（World.Coef は国ごとのインスタンス）
        on.StartWarmup();
        var meritOnly = Fresh(); foreach (var n in meritOnly.Nations) { n.World.Coef.WarmupStopClassShift = 0; n.World.BakeRun = new OpeningBake.RunOptions { ThawCapacity = false, StopSocialMobility = true, StopPopDecision = false }; }
        meritOnly.StartWarmup();
        var popOnly = Fresh(); foreach (var n in popOnly.Nations) { n.World.Coef.WarmupStopClassShift = 0; n.World.BakeRun = new OpeningBake.RunOptions { ThawCapacity = false, StopSocialMobility = false, StopPopDecision = true }; }
        popOnly.StartWarmup();
        double dOff = MaxDiff(WorkerShare(off), open), dOn = MaxDiff(WorkerShare(on), open);
        double dMerit = MaxDiff(WorkerShare(meritOnly), open), dPop = MaxDiff(WorkerShare(popOnly), open);
        Assert.True(dOff > 10 * dOn, $"前提が崩れた：旗 0 の助走で 9 国の工場労働者の割合が動かない（最大 {dOff:F4}）＝対照にならない");
        Assert.True(dOn < 0.005, $"旗 1 の助走で工場労働者の割合が動いた（最大 {dOn:F4}・徴兵の分は 0.001 未満）");
        Assert.True(dMerit > 10 * dOn, $"社会移動だけ止めても階級遷移が動かす（最大 {dMerit:F4}）はずが動かない＝PopDecision の門の突然変異を拾えない");
        Assert.True(dPop > 10 * dOn, $"階級遷移だけ止めても社会移動が動かす（最大 {dPop:F4}）はずが動かない＝Meritocracy の門の突然変異を拾えない");
    }
}
