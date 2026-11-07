using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(921) 消した門と局所化した読みの番人。事前登録 measurements/m921_20261107_prereg_sm.txt [3]（★(790) 突然変異が緑のままなら機構の試験を足す）。
/// ①侵略の実効戦力は〈陸軍力×投射（床 0.05）〉そのもの＝遠征技術の門（旧 ExpeditionLift・常に 1.0）が値を運んでいない。期待は部品（LandPower・Projection）から別に組む。
/// ②Discovery.Step は Instruments.On を関数の中で 1 回しか読まない（確保と使用の読みが別だと並列の試験で食い違う＝(913) と同じ型）。</summary>
public class ForceReadGuardTests
{
    [Fact]
    public void 侵略の実効戦力は陸軍力かける投射の床つきそのもの()
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: 921UL) { DiplomacyFrozen = true };
        var c = sim.Nations[0].World.Coef;
        var ps = sim.Diplomacy.Powers.Take(8).ToList();
        int checkedPairs = 0, withReach = 0;
        foreach (var g in ps) foreach (var t in ps)
        {
            if (ReferenceEquals(g, t)) continue;
            double proj = sim.Diplomacy.Projection(g, t, c);
            double expected = g.LandPower * Math.Max(0.05, proj);
            Assert.Equal(expected, sim.Diplomacy.ColonizeForce(g, t, c));   // ビットまで同じ（0.999 などの係数が掛かると赤）
            checkedPairs++;
            if (g.LandPower > 0 && proj > 0.05) withReach++;
        }
        Assert.True(checkedPairs >= 20, $"組が少ない（{checkedPairs}）");
        Assert.True(withReach >= 1, "投射が床を超える組が 1 つも無い＝何もしない実装でも緑になる盤面");
    }

    [Fact]
    public void DiscoveryStepはInstrumentsOnを1回だけ読む()
    {
        string src = System.IO.File.ReadAllText(System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore", "Discovery.cs"));
        int a = src.IndexOf("internal static void Step(World w, WorldView? view, Coefficients c)", StringComparison.Ordinal);
        int b = src.IndexOf("w.LastDiscoveryView = view;", a, StringComparison.Ordinal);
        Assert.True(a > 0 && b > a, "Step の範囲が見つからない");
        string body = src[a..b];
        int n = 0, i = 0;
        while ((i = body.IndexOf("Instruments.On", i, StringComparison.Ordinal)) >= 0) { n++; i += 14; }
        Assert.Equal(1, n);
    }
}
