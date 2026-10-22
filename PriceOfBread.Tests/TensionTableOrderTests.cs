using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spt) 緊張の表（Diplomacy の _tension）を1回だけ引く形（恒等）の番人：表の挿入の順＝DriftTension が回す順は、旧の形と同じでなければならない。
/// ①増分が 0 に切られる書き込み（緊張 0 の組へ負の増分＝クランプで 0）でも、その時点で鍵が入る（旧＝GetValueOrDefault で読みインデクサで書いた＝増分に関わらず入った）。
/// ②冷ます書き込み（DriftTension）は鍵を足さない・並びを変えない。⚠突然変異「動いたときだけ入れる（now != old のときだけ書く）」で①が赤。</summary>
public class TensionTableOrderTests
{
    [Fact]
    public void 緊張の表の鍵は最初に書いた順に並び減衰で並びが変わらない()
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL);
        var d = sim.Diplomacy; var ps = d.Powers;
        Assert.True(ps.Count >= 6, "前提が崩れた：国が足りない");
        // まだ緊張の無い組を 3 つ選ぶ（表に無い鍵）
        var fresh = new System.Collections.Generic.List<(Power a, Power b)>();
        var have = new System.Collections.Generic.HashSet<long>(d.TensionKeysInOrder());
        for (int i = 0; i < ps.Count && fresh.Count < 3; i++)
            for (int j = i + 1; j < ps.Count && fresh.Count < 3; j++)
                if (!have.Contains(KeyOf(ps[i], ps[j])) && fresh.TrueForAll(f => f.a != ps[i] && f.b != ps[i] && f.a != ps[j] && f.b != ps[j])) fresh.Add((ps[i], ps[j]));
        Assert.True(fresh.Count == 3, "前提が崩れた：緊張の無い組が 3 つ無い");
        int n0 = d.TensionKeysInOrder().Count;
        d.AddTension(fresh[0].a, fresh[0].b, -5, TensionReason.Ideology);   // 0 へ切られる＝動かない書き込み（それでも入る）
        d.AddTension(fresh[1].a, fresh[1].b, 7, TensionReason.Ideology);
        d.AddTension(fresh[2].a, fresh[2].b, 3, TensionReason.Ideology);
        d.AddTension(fresh[0].a, fresh[0].b, 4, TensionReason.Ideology);   // 既にある鍵＝並びは変わらない
        var keys = d.TensionKeysInOrder();
        Assert.Equal(n0 + 3, keys.Count);
        Assert.Equal(new[] { KeyOf(fresh[0].a, fresh[0].b), KeyOf(fresh[1].a, fresh[1].b), KeyOf(fresh[2].a, fresh[2].b) }, keys.GetRange(n0, 3).ToArray());
        Assert.Equal(4.0, d.Tension(fresh[0].a, fresh[0].b));
        // 1か月の外交の歩（減衰を含む）の後も、それまでの鍵の並びは同じ（新しい鍵は後ろに足されるだけ）
        sim.Diplomacy.Step(sim.Coef, sim.Rng, 1);
        var after = d.TensionKeysInOrder();
        Assert.True(after.Count >= keys.Count);
        Assert.Equal(keys.ToArray(), after.GetRange(0, keys.Count).ToArray());
    }

    private static long KeyOf(Power a, Power b) { int x = System.Math.Min(a.Id, b.Id), y = System.Math.Max(a.Id, b.Id); return ((long)x << 32) | (uint)y; }
}
