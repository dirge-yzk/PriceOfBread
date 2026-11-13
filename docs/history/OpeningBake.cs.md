# PriceOfBread.SimCore/OpeningBake.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L8-16 W 直後 `public static class OpeningBake`
````
/// <summary>★(490) 開幕の焼き込みの道具（読む線 (464)(473) の形・設計は docs/202609/99_実装ログ_20260903.md の (490)）。
/// 焼く＝凍結を解いた試走（<see cref="RunOptions"/>＝能力の凍結を外す・社会移動を止める・州の中の階級遷移を止める＝(473) の腕 D）で落ち着いた盤面から、
/// 州×レシピの能力（<see cref="Producer.Scale"/>・<see cref="Producer.BuiltScale"/>・<see cref="Producer.SmoothedMargin"/>・<see cref="Producer.Bankrupt"/>）・州の資本（工業化度ほか）・
/// 州×財の在庫と値段・州の物価水準・国の発見の値段の基準・州×階級の貯蓄を、別の表（<c>opening_bake.json</c>・鍵は名前）へ書く。
/// 読み込み＝旗 <see cref="Coefficients.OpeningBakeEnabled"/> が正のとき（既定 0＝表を開きもしない＝厳密恒等）、<see cref="MultiNationSim.StartWarmup"/> の頭で 1 回（<see cref="Apply"/>）。
/// 表の鍵が今の世界の名前と**全て**一致しなければ何も書かずに例外（fail-closed）＝世界のデータ（州・レシピ・財・階級）を変えたら焼き直す。
/// 新しい状態は作らない（思想⑩）＝読み込みは既存の欄へ書くだけ。焼いた値は模型の出力で、world.json（手で直す史実のデータ）とは別の置き場。
/// ⚠焼かないもの：人口（総数・年齢の層）・階級の構成・政体・法・国庫（<c>RebaseOpeningFiscal</c> の規則のまま）・国債・技術・発見・暦・外交・市場の流れの欄（LastSold 等）・労働の留保賃金・生産者の産出と労働の重み。
/// 読み込んだ直後は貯蓄が変わる＝基礎貨幣（貯蓄＋国庫−貸出の導出量）が変わるので保存則の基準線を取り直す（助走の各四半期も取り直す）。</summary>
````

## L19-19 W 直後 `public sealed class RunOptions`
````
/// <summary>焼く試走の腕（焼く側だけが <see cref="World.BakeRun"/> に立てる・遊ぶ側と測る側は null＝何も変わらない）。</summary>
````

## L22-22 W 直後 `public bool ThawCapacity = true;`
````
/// <summary>助走の中で能力の凍結（<c>EconomyStep</c> の建った結果・減価・ヴィンテージ・国家建設・送電網）を外す。</summary>
````

## L24-24 W 直後 `public bool StopSocialMobility = true;`
````
/// <summary>助走の中で社会移動（<c>Meritocracy.Step</c>）を止める。</summary>
````

## L26-26 W 直後 `public bool StopPopDecision = true;`
````
/// <summary>助走の中で州の中の階級遷移（<c>PopDecision.Run</c>）を止める（階級の構成を world.json のまま保つ＝(473) の腕 D）。</summary>
````

## L32-32 W 直後 `internal static readonly string[] CapNames = ["Industrialization", "OldVintageCapital", "V`
````
/// <summary>州の資本の欄（<see cref="Region"/>）。並びは表の cap の並び。</summary>
````

## L34-34 W 直後 `internal static readonly string[] ProducerNames = ["Scale", "BuiltScale", "SmoothedMargin"`
````
/// <summary>生産者の欄（<see cref="Producer"/>）の並び（表の pr の各行は [財の添字, これら]）。</summary>
````

## L37-37 W 直後 `public sealed record Header(string SourceHash, string InputHash, int Seed, int QuartersRun`
````
/// <summary>表の頭書き（焼いた木の SOURCE-HASH は表を足す前の木＝表自体は SOURCE-HASH に入る）。</summary>
````

## L40-40 W 直後 `public static string InputHash()`
````
// ───────────────────────────── 入力のハッシュ ─────────────────────────────
````

## L42-44 W 直後 `public static string InputHash()`
````
/// <summary>世界のデータの入力のハッシュ＝SimCore の埋め込みの資源（world.json・goods.json・places.json ほか。opening_bake.json 自身を除く）を名前順に並べた sha256 の先頭 16 桁。
/// ★(499) 表の頭書きの inputHash と違えば <see cref="Validate"/> が赤にする（旗 1 の読み込み〔<see cref="Apply"/>〕と同梱の表の番人だけ＝旗 0 の助走は表を読まない）。
/// 鍵（州・生産者・財・階級の並びと名前）は同じまま値だけ変わる入力の直し（農奴制の初期値のような world.json の値の変更）は鍵では見つからず、表の値が古いまま黙って読まれるため。</summary>
````

## L60-60 W 直後 `internal static string NationKey(World w) => w.SelfPower?.BaseName ?? throw new InvalidOpe`
````
// ───────────────────────────── 鍵の名乗り（国・州・財・階級） ─────────────────────────────
````

## L62-62 W 直後 `internal static string NationKey(World w) => w.SelfPower?.BaseName ?? throw new InvalidOpe`
````
/// <summary>国の鍵＝構築時の名前（<see cref="Power.BaseName"/>・改名で変わらない）。</summary>
````

## L65-65 W 直後 `internal static string[] DiscoveryPriceNames() => [.. Catalog.Goods.Select(g => g.Name), "`
````
/// <summary>発見の値段の並び（財の名前＋末尾の 1 欄＝陸の輸送）。</summary>
````

## L77-77 W 直後 `public sealed record BakeResult(string Text, int Quarters, bool Converged);`
````
// ───────────────────────────── 試走（焼く側の入口） ─────────────────────────────
````

## L82-85 W 直後 `public static BakeResult Bake(ulong seed, int maxQuarters, Func<MultiNationSim, int, bool>`
````
/// <summary>凍結を解いた試走を回して表の本文を作る（Runner の bakeopen とテストの入口）。構築は selfplay と同じ（全国 <see cref="UtilityAgent"/>・既定の係数）。
/// 試走は <see cref="MultiNationSim.WarmupEconomy"/> を直に呼ぶ（技術の助走・開幕の軍の確定・ランクの取り直しは回さない＝取り出す盤面は〈凍結を解いた助走の直後〉だけ）。
/// <paramref name="converged"/>＝各四半期の終わり（国庫の戻しの後）に呼ばれ、true を返した四半期でその盤面を焼いて止める。上限 <paramref name="maxQuarters"/> まで true が出なければ上限の盤面を焼く（Converged＝false）。
/// 乱数は引かない（シミュの乱数をそのまま）＝同じ入力で 2 回焼くと本文がバイトで同じ。</summary>
````

## L100-100 W 直後 `public static string Capture(MultiNationSim sim, Header h)`
````
// ───────────────────────────── 焼く（盤面 → 表の本文） ─────────────────────────────
````

## L102-103 W 直後 `public static string Capture(MultiNationSim sim, Header h)`
````
/// <summary>盤面の今の状態を表の本文（JSON）にする。決定論：国・州は世界の並び（構築の順）・財は <see cref="Catalog.Goods"/> の順・数は "R"（往復で同じ）・LF・BOM なし。
/// 鍵（国の名前・州の名前）が重なれば例外（fail-closed）。</summary>
````

## L170 T `static double[] CapValues(Region r) => [r.Industrialization, r.OldVintageCapital, 0.0`
````
/* ★(613) 退役した枠 VintageTechLevel＝焼き直しまで位置を保つ */
````

## L172-172 W 直後 `static readonly Lazy<JsonDocument> Embedded = new(() =>`
````
// ───────────────────────────── 読む（表 → 盤面） ─────────────────────────────
````

## L183-183 W 直後 `internal static JsonDocument EmbeddedDoc => Embedded.Value;`
````
/// <summary>埋め込みの表（番人と焼き直しの比べが読む）。</summary>
````

## L186-186 W 直後 `internal static void Apply(MultiNationSim sim) => Apply(sim, Embedded.Value);`
````
/// <summary>埋め込みの表を盤面に書く（旗が正のとき <see cref="MultiNationSim.StartWarmup"/> が 1 回呼ぶ）。鍵が全て一致しなければ何も書かずに例外。</summary>
````

## L189-189 W 直後 `internal static void Apply(MultiNationSim sim, JsonDocument doc)`
````
/// <summary>表（<paramref name="doc"/>）を盤面に書く。①検証（鍵の一致・長さ・非有限）②書く、の 2 段＝①で例外なら盤面は 1 ビットも変わらない。</summary>
````

## L209 T `r.Industrialization = cap[0].GetDouble(); r.OldVintageCapital = cap[1].GetDouble();`
````
/* cap[2]＝退役した枠（読み捨て） */
````

## L223 T `sim.RebaselineInvariants();`
````
// 貯蓄が変わった＝基礎貨幣（貯蓄＋国庫−貸出）が変わる＝保存則の基準線を取り直す
````

## L233-233 W 直後 `internal static List<string> Validate(MultiNationSim sim, JsonDocument doc)`
````
/// <summary>表と盤面の鍵の突き合わせ（双方向）。空＝一致。盤面は書かない。</summary>
````

## L239-239 W 直後 `string inputNow = InputHash();`
````
// ★(499) 入力のハッシュ（反証役 hz4〔v〕低#3）：鍵が同じで値だけ違う入力（world.json の値の直し）で古い表が黙って読まれるのを止める＝焼き直しを強いる
````

## L243-243 W 直後 `if (!NamesMatch(root, "capNames", CapNames)) errs.Add($"資本の欄の並びが世界と違う（世界 {string.Join(',',`
````
// ★(491) 欄の並びも鍵（反証役 hz4〔v〕低#3＝Apply は添字で直書き＝同じ長さのまま欄を入れ替えると値が黙って別の欄に入る）
````

