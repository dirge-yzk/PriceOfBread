using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(spb) 案 B 労働の閉じた式の番人（旗 LaborClosedForm は (843) で焼き込み。旗 0 の日次との比べは旗とともに消した＝閉じた式を使う条件の純関数だけが残る）。
/// ①参照より下で向きが変わらない旬（品薄で上がる・品余りで下がる）は、1日目を日次で回した後の残りを閉じた式で進め、日次の写像を h 回まわした値と相対 1e-12 以内（段0 の物差し＝最大 2.1e-15）。
///   閉じた式を使ったことは、計算量の台帳の日次の回（旗ありは 1 回・旗なしは h 回）で見る。
/// ②旬の途中で参照をまたぐ旬・法の床に届く旬・参照より上から出発する旬は閉じた式を使わず日次へ戻る＝旗の有無で1ビットも違わない。
///   突然変異：LaborClosed の〈最後の日が参照より下〉を外すと「参照をまたぐ」が赤・〈法の床の上〉を外すと「法の床に届く」が赤になることを確かめた（記録・measurements/spb_20261024_prereg.txt）。</summary>
public class LaborClosedTests
{
    [Fact]
    public void 閉じた式は端の日だけを確かめる()
    {
        // 品余り（adj＜0）：最後の日が法の床の下なら使わない・上なら使う（単調に下がる＝端の日だけ見ればよい）
        const double p1 = 0.5, adj = -0.008, fall = 0.25; const int n = 14;
        double pe = (p1 + (p1 * (1 + adj) - p1) * fall) * System.Math.Pow(1 + fall * adj, n - 1);
        Assert.True(MarketFlow.LaborClosed(p1, p1, adj, n, 1.0, pe * 0.999, 1.0, fall, out double p));
        Assert.Equal(pe, p, 15);
        Assert.False(MarketFlow.LaborClosed(p1, p1, adj, n, 1.0, pe * 1.001, 1.0, fall, out _));   // 旬の途中で床に届く
        // 品薄（adj≧0）：最後の日が参照の下なら使う・上なら使わない
        double up = p1 * System.Math.Pow(1.008, n);
        Assert.True(MarketFlow.LaborClosed(p1, p1, 0.008, n, up * 1.001, 0, 1.0, fall, out _));
        Assert.False(MarketFlow.LaborClosed(p1, p1, 0.008, n, up * 0.999, 0, 1.0, fall, out _));   // 旬の途中で参照をまたぐ
        Assert.False(MarketFlow.LaborClosed(p1, p1, 0.008, n, 1.0, 0, 0.9, fall, out _));          // Rise＜1 は別の形（留保賃金が遅れる）
    }
}
