using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1408) 項 9：軌道に依る前提の試験のための種の選び方（README ★(1404)）。範囲（両端を含む）を前から順に見て、前提の述語が立つ最初の種を返す。
/// 述語は種ごとに呼ばれる（述語の中で規則の検査を全ての試した種に当てられる）。立つ種が無ければ Seed は null・Tried は試した数。
/// </summary>
internal static class SeedPick
{
    internal static (ulong? Seed, int Tried) FirstWhere(ulong first, ulong lastInclusive, Func<ulong, bool> premise)
    {
        int tried = 0;
        for (ulong s = first; s <= lastInclusive; s++)
        {
            tried++;
            if (premise(s)) return (s, tried);
            if (s == ulong.MaxValue) break;
        }
        return (null, tried);
    }
}

public class SeedPickTests
{
    [Fact]
    public void 前提が立つ最初の種を返し_立たなければ_null()
    {
        var seen = new List<ulong>();
        var (seed, tried) = SeedPick.FirstWhere(1789UL, 1800UL, s => { seen.Add(s); return s >= 1792UL; });
        Assert.Equal(1792UL, seed); Assert.Equal(4, tried);
        Assert.Equal([1789UL, 1790UL, 1791UL, 1792UL], seen);   // 立った所で止まる（範囲の終わりまで進まない）
        var (none, t2) = SeedPick.FirstWhere(1789UL, 1791UL, _ => false);
        Assert.Null(none); Assert.Equal(3, t2);
    }
}
