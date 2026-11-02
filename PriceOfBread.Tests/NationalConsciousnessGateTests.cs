using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(798) c11dm：国民意識の蓄積（<see cref="CrownParliament.Step"/> の §5）に、以前は〈重みが正のときだけ走る〉門があった
/// （`NationalConsciousnessCorruptionWeight &gt; 0 || NationBuildMobilizationWeight &gt; 0`・左は既定 0.2 で常に真＝何もしない値・左の係数は他に読み手が無く一緒に消した）。門を消した。
/// 蓄積は戦時徴兵の重み NationBuildMobilizationWeight の値に依らず走る＝重みを 0 にしても POP の国民意識の初期化（負の未捕捉 → 0 以上）が起きる。
/// 期待値は本体から作らない：前提は構築直後に国民意識が捕捉済み（0 以上）の POP が 1 つも無いこと（試験側で POP を数える）・結果は 1 つ以上できること。
/// 突然変異：門 `if (c.NationBuildMobilizationWeight &gt; 0)` を足す → 赤。</summary>
public class NationalConsciousnessGateTests
{
    private static int Captured(World w) => w.AllPops().Count(p => p.Size > 0 && p.NationalConsciousness >= 0);

    [Fact]
    public void 戦時徴兵の重みが0でも国民意識の蓄積は走りPOPの国民意識が捕捉される()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        w.Coef.NationBuildMobilizationWeight = 0;
        Assert.Equal(0, Captured(w));   // 前提：構築直後は誰も捕捉されていない
        CrownParliament.Step(w, w.Coef);
        Assert.True(Captured(w) > 0, "重みが 0 でも蓄積の §5 は走るはず（門を消した）");
    }
}
