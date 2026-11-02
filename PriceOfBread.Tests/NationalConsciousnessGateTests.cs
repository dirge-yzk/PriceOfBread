using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(798) c11dm：国民意識の蓄積（<see cref="CrownParliament.Step"/> の §5）に、以前は〈両方の重みが 0 のとき一切走らない〉門があった
/// （`NationalConsciousnessCorruptionWeight &gt; 0 || NationBuildMobilizationWeight &gt; 0`・左は既定 0.2 で常に真＝何もしない値）。門を消した。
/// 蓄積は重みの値に依らず走る＝重みを 0 にしても POP の国民意識の初期化（負の未捕捉 → 0 以上）が起きる。
/// 期待値は本体から作らない：前提は構築直後に国民意識が捕捉済み（0 以上）の POP が 1 つも無いこと（試験側で POP を数える）・結果は 1 つ以上できること。
/// 突然変異：門 `if (c.NationalConsciousnessCorruptionWeight &gt; 0 || c.NationBuildMobilizationWeight &gt; 0)` を戻す → 赤。</summary>
public class NationalConsciousnessGateTests
{
    private static int Captured(World w) => w.AllPops().Count(p => p.Size > 0 && p.NationalConsciousness >= 0);

    [Fact]
    public void 両方の重みが0でも国民意識の蓄積は走りPOPの国民意識が捕捉される()
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1), new NullAgent());
        var w = sim.World;
        w.Coef.NationalConsciousnessCorruptionWeight = 0;
        w.Coef.NationBuildMobilizationWeight = 0;
        Assert.Equal(0, Captured(w));   // 前提：構築直後は誰も捕捉されていない
        CrownParliament.Step(w, w.Coef);
        Assert.True(Captured(w) > 0, "重みが両方 0 でも蓄積の §5 は走るはず（門を消した）");
    }
}
