using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1105) c106im 計器 Instruments*.cs の置き場の番人。事前登録 measurements/m1105_20261113_prereg_im.txt。
/// 計器は PriceOfBread.SimCore/Instruments/ の下にある。SimCore の直下だけを走査する番人は〈直下＋Instruments/〉を
/// <see cref="TopAndInstruments"/> で数える（直下に計器を置くと走査から外れるので、直下の計器はこの番人が赤にする）。</summary>
public class InstrumentsPlacementTests
{
    /// <summary>SimCore の直下の *.cs と SimCore/Instruments/ の下の *.cs（Display など他の下のフォルダは含めない＝既存の直下の走査の母集合を保つ）。</summary>
    internal static IEnumerable<string> TopAndInstruments(string simCoreDir)
        => Directory.EnumerateFiles(simCoreDir, "*.cs").Concat(Directory.EnumerateFiles(Path.Combine(simCoreDir, "Instruments"), "*.cs"));

    [Fact]
    public void 計器はInstrumentsフォルダの下にあり_SimCoreの直下に無い()
    {
        string dir = Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        var strays = Directory.GetFiles(dir, "Instruments*.cs").Select(Path.GetFileName).ToList();
        Assert.True(strays.Count == 0, "SimCore の直下に計器がある（SimCore/Instruments/ へ置く）：" + string.Join("・", strays));
        int n = Directory.GetFiles(Path.Combine(dir, "Instruments"), "Instruments*.cs").Length;
        Assert.True(n >= 86, $"SimCore/Instruments/ の計器が 86 本未満（{n}）＝移した計器が消えた");
    }
}
