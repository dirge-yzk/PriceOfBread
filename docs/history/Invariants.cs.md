# PriceOfBread.SimCore/Invariants.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `public sealed class InvariantViolationException(string message) : Exception(message);`
````
/// <summary>
/// 不変条件（§11.6）が破れたときに投げる例外。「シミュ上の破綻」を経済的危機と
/// 分離して即停止させるための信号（§2.7）。価格暴落や欠品そのものは破綻ではない。
/// </summary>
````

## L9-9 W 直後 `public interface IInvariant`
````
/// <summary>毎月検査する不変条件の共通形（§11.6）。</summary>
````

## L16-19 W 直後 `public sealed class MoneyConservation(World world) : IInvariant`
````
/// <summary>
/// 貨幣保存（§11.6・§A-7）：保存量は基礎貨幣（金）＝広義マネー − 与信残高。与信で広義は増減するが基礎は内部移転で一定、
/// 交易（金流）・鋳造・不換発行でのみ増減する（Δ基礎貨幣＝純貿易流入＋鋳造＋不換発行）。
/// </summary>
````

## L22-23 W 直後 `private readonly MoneyStock _baseline = world.BaseMoney - world.CumulativeTradeFlow - worl`
````
// 基準線は「交易を差し引いた基礎貨幣」で持つ＝期待値 = _baseline + 累積貿易 が捕捉時の基礎貨幣に一致。
// 構築時は累積貿易0なので従来と同値。開始盤面で国庫・与信を初期へ戻した直後に取り直しても整合する（相対基準）。
````

## L29 T `double expected = (_baseline + world.CumulativeTradeFlow + world.CumulativeGoldMinted + wo`
````
// 保存則の式は在庫の型のまま書く（台帳3本も在庫）
````

## L30 T `double bm = world.BaseMoney.Value;`
````
// 全pop走査＝1回だけ
````

## L31-32 W 直後 `double scale = Math.Max(Math.Abs(expected), Math.Max(Math.Abs(_baseline.Value), Math.Max(M`
````
// 許容誤差は「合算した量の桁」に比例させる（残差は浮動小数の再結合のみ）。expected は基準−累積貿易の**大きな量の差**になり得る（資本逃避で基礎貨幣がほぼ0へ収縮＝激しい桁落ち）ので、
// |expected| だけを基準にすると相対許容が過小になり float ノイズで誤検出する。基準・累積貿易・基礎貨幣の最大桁で規格化＝桁落ちに頑健（従来より緩むだけで厳しくはならない＝既存テスト不変）。
````

## L34-35 W 直後 `double tol = 1e-8 * Math.Max(1.0, scale);`
````
// 許容1e-8（2026-07-08に1e-9から緩和）：604四半期=54,360日×数百万演算のfloat再結合ノイズは相対1e-9をわずかに超え得る
// （実測の偽陽性：seed20江戸=2.5e-9・seed4タシュケント=1e-8）。実リーク（官僚Size≈0のDistribute消失=2e-7級）とは2桁の分離＝検出力は維持。
````

## L43-46 W 直後 `public sealed class PopulationConservation(World world) : IInvariant`
````
/// <summary>
/// 人口保存（§11.6・修正版 §A-3）：Δ総pop = ±移民 ＋ PopulationDelta（出生−死亡−戦死）。
/// 移民は再ラベルで総和を保つ。出生・死亡・戦死だけが総人口を規定値から動かし、符号付き台帳で追跡（人口転換）。
/// </summary>
````

## L49 T `private readonly double _baseline = world.TotalPopulation - world.PopulationDelta;`
````
// 相対基準（取り直し安全。構築時は増減0で従来と同値）
````

## L54-54 W 直後 `double expected = _baseline + world.PopulationDelta;`
````
// 規定値 ＋ 累積増減（出生−死亡−戦死）。これ以外の増減（意図しない人口漏れ）のみ検出する。
````

## L63-65 W 直後 `public sealed class NonNegativeBounds : IInvariant`
````
/// <summary>
/// 境界条件（§11.6）：価格≥0・在庫≥0・貯蓄≥0、および非 NaN／∞（§2.7 のシミュ破綻検出）。
/// </summary>
````

## L78-78 W 直後 `double sa = 0; bool neg = p.Ages.Length != 2 * Pop.Layout.Bands;`
````
// ★年齢の層（2026-09-30 段4e 後半）：16本の割合は各々0以上・合計1（許容1e-9・否定形＝NaN も赤）
````

## L90-90 W 直後 `if (double.IsNaN(world.Treasury.Value) || double.IsInfinity(world.Treasury.Value))`
````
// 国庫・国債・基礎貨幣は負値が正当（債務）なので非負は課さず NaN／∞ のみ検出（Pay は有限性を確かめず加算するため）。
````

## L108-114 W 直後 `public sealed class BondLedger : IInvariant`
````
/// <summary>
/// 国債の保有台帳（2026-09-21裁定）：国の全POP（Regions の全州＝Fiscal が利払い・償還を配る集合と同じ）の保有の和＝国内元本残高（GovDebt − ForeignDebt）。
/// 台帳は金を動かさない記録なので、両辺は別々の経路（Fiscal の起債・償還・デフォルト／Politics の否認／POPの移動と国外振替）で動く＝どこかで運び忘れると割れる。
/// 許容は相対1e-8（貨幣保存と同じ）。尺度は GovDebt・ForeignDebt・保有の和と、台帳を通った元本の流れ（起債と償還の累計）の最大＝国内元本が2つの大きな量の差になる国（対外債の多い国）の桁落ちと、
/// 大きかった国債を返し終えつつある国の丸めの残り（下の <see cref="Balanced"/>）に頑健。
/// 保有そのものも非負・有限であること（NaN は否定形で拾う）。状態を持たない＝基準線の取り直し不要。
/// </summary>
````

## L121 T `MoneyStock held = default;`
````
// (252) 保有の和も在庫の型＝右辺 GovDebt−ForeignDebt と型のまま比べる
````

## L134-140 W 直後 `internal static bool Balanced(World world, MoneyStock held)`
````
/// <summary>Σ保有と国内元本の一致（相対1e-8）。起票（<see cref="Fiscal.SeedBondHoldings"/>）も同じ判定を使う＝検査が割れる時だけ起票する（判定は1本）。
/// ★尺度に台帳を通った元本の流れ（国庫の台帳の起債 Borrow と償還 DebtRepay の累計）を入れる（2026-09-28・支払いの一元化 段2-1 の合流で見つけた偽陽性）。
///   ズレは浮動小数の丸めで、国債が最大のときの桁で積もる（相対1e-15級）。比例の償還（Σ保有×keep と 国債−元本）はズレを**絶対値のまま**保つ
///   ＝国債を返して小さくなるほどズレの相対が膨らむ（seed1789 の清＝国債の最大 842万・ズレ −1.35e-8〔最大時の相対 1.6e-15〕が、償還で 1948年に
///   国債 1.7 まで下がった所で相対 7.7e-9＝旧の尺度〔今の残高〕の許容 1e-8×max(1,1.7) を越えて全長の番人が止まった）。
///   貨幣保存（<see cref="MoneyConservation"/>）が累積貿易を尺度に入れるのと同じ形＝台帳を通った量の桁で丸めの許容を測る（新しい状態は持たない＝既存の累計を読む）。
///   運び忘れ（保有の一部が消える・湧く）は流れの桁の1e-8 より何桁も大きい＝検出力は保つ。</summary>
````

## L145 T `return Math.Abs((held - (world.GovDebt - world.ForeignDebt)).Value) <= 1e-8 * Math.Max(1.0`
````
// NaN は偽＝割れた側へ倒れる
````

## L149-152 W 直後 `public sealed class GoodsBalance : IInvariant`
````
/// <summary>
/// 財収支（§11.6）：各財 stock = prev_stock + produced − sold（単一地域は純輸出0）。
/// 取引フェーズの帳尻が合っているかの恒等式チェック。
/// </summary>
````

