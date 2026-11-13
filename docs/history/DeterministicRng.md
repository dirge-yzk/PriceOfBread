# PriceOfBread.SimCore/DeterministicRng.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-9 W 直後 `public sealed class DeterministicRng`
````
/// <summary>
/// 決定論的な擬似乱数生成器（xoshiro256** ／ splitmix64 で種付け）。
/// .NET 既定の Random は実装がバージョン依存になりうるため、仕様 §11.5
/// 「同一シード ＋ 同一入力 → 同一 run」を保証する固定アルゴリズムを自前で持つ。
/// 系列は用途別に分離して持つ（各国の主系列＝豊凶・pop意思決定ロール・疫病／外交・天候・統一戦争・屈服・勢力変異の専用系列）
/// ＝<see cref="MultiNationSim"/> のコンストラクタと <see cref="World.FactionRng"/> を参照（§0-B／§11.5）。
/// </summary>
````

## L16-16 W 直後 `_s0 = SplitMix64(ref seed);`
````
// splitmix64 でシード1語から内部状態4語へ展開する
````

## L34-34 W 直後 `public ulong NextULong()`
````
/// <summary>次の 64bit 乱数を返し、内部状態を前進させる。</summary>
````

## L48-48 W 直後 `public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);`
````
/// <summary>[0,1) の倍精度乱数。</summary>
````

