namespace PriceOfBread.SimCore;

/// <summary>
/// 決定論的な擬似乱数生成器（xoshiro256** ／ splitmix64 で種付け）。
/// .NET 既定の Random は実装がバージョン依存になりうるため、仕様 §11.5
/// 「同一シード ＋ 同一入力 → 同一 run」を保証する固定アルゴリズムを自前で持つ。
/// 系列は用途別に分離して持つ（各国の主系列＝豊凶・pop意思決定ロール・疫病／外交・天候・統一戦争・屈服・勢力変異の専用系列）
/// ＝<see cref="MultiNationSim"/> のコンストラクタと <see cref="World.FactionRng"/> を参照（§0-B／§11.5）。
/// </summary>
public sealed class DeterministicRng
{
    private ulong _s0, _s1, _s2, _s3;

    public DeterministicRng(ulong seed)
    {
        // splitmix64 でシード1語から内部状態4語へ展開する
        _s0 = SplitMix64(ref seed);
        _s1 = SplitMix64(ref seed);
        _s2 = SplitMix64(ref seed);
        _s3 = SplitMix64(ref seed);
    }

    private static ulong SplitMix64(ref ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        ulong z = x;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    /// <summary>次の 64bit 乱数を返し、内部状態を前進させる。</summary>
    public ulong NextULong()
    {
        ulong result = Rotl(_s1 * 5UL, 7) * 9UL;
        ulong t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 45);
        return result;
    }

    /// <summary>[0,1) の倍精度乱数。</summary>
    public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);
}
