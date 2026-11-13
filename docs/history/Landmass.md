# PriceOfBread.SimCore/Landmass.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-10 W 直後 `public static class Landmass`
````
/// <summary>
/// 陸塊テーブル（03地理 手順0・2026-07-28）：全地点（州＋植民地＋首都・重複除去）に陸塊IDを持たせ、
/// 「陸続きか」を矩形分岐でなく一次データで判定する。旧 Geo.ContinentOf（矩形5分岐）は島の概念が無く、
/// 旧世界の全島嶼（グレートブリテン・本州・マダガスカル等23陸塊）をユーラシア本土へ接着＝85.6%の国対が「陸続き」で
/// 船も制海権も不要という穴だった（英がボルネオへ11,260km陸路侵略・マダガスカルが大陸国へ船なし侵攻）。
/// データは埋め込み landmass.json が唯一の真実源（03裁定A1＝外出し・**C#側フォールバック定義を置かない**＝goods.jsonの二重管理の轍を踏まない）。
/// 照会は最近傍（ハバーサイン・同距離はファイル順＝name順で先勝ち＝決定論）＝登録点の全件走査。答えは座標の厳密なビットで記憶する（<see cref="Of"/>・2026-09-26）＝同じ照会点は2回目から辞書引き。
/// </summary>
````

## L13 T `public sealed record Point(string Name, string Kind, double Lat, double Lon, string Landma`
````
// Coastal＝海辺の陸横断棄却（SeaGraph）は内陸ノード（!Coastal）だけを見る（03手順3：沿岸州の近傍は定義上海がある＝沿岸航路を過剰棄却しない）
````

## L16 T `public static IReadOnlyList<Point> PointsAll => Points;`
````
// PathGraphがノード源として読む（03手順1）
````

## L17-17 W 直後 `private static readonly System.Collections.Concurrent.ConcurrentDictionary<(long, long), s`
````
// ★Of の記憶（2026-09-26・高速化）。鍵＝座標の厳密なビット。App の UI スレッドからも呼ばれる＝並行に安全な辞書。セーブには載らない（静的）。
````

## L44-45 W 直後 `public static bool IsRegistered(string name)`
````
/// <summary>名前が陸塊テーブルに登録されているか（2026-08-01・州や国を足したときの登録漏れの番人がこれを読む）。
/// ⚠未登録でも <see cref="Of"/> は最近傍で拾うので**たまたま正しい大陸に落ちることがある**＝運に依存する＝静かな穴になる。</summary>
````

## L52-53 W 直後 `public static bool CoastalOf(string name, bool fallback)`
````
/// <summary>★登録名→沿岸か（2026-08-28）。未登録は fallback を返す。
/// 読み手＝植民地の州昇格（PromoteColonyToRegion）＝**内陸植民地を港にしない**ための一次データ照会。</summary>
````

## L60-61 W 直後 `public static string LandmassOf(string name)`
````
/// <summary>登録名→陸塊ID（未登録は空文字）。座標からの最近傍（<see cref="Of"/>）でなく**テーブルの実体**を引く
/// ＝登録済みの地点では近似を挟まない。国家選択画面の「本国州が他国と陸続きか」の判定が読む。</summary>
````

## L68-68 W 直後 `public static (double Lat, double Lon) CoordOf(string name)`
````
/// <summary>登録座標（未登録なら (0,0)）。Climate.CityCoord との食い違いを検知する番人が読む。</summary>
````

## L75-79 W 直後 `public static string Of(double lat, double lon)`
````
/// <summary>座標→陸塊ID（最近傍の登録点を継ぐ）。照会点は首都/植民地の登録座標そのもの（種類が少ない）＝記憶で2回目から辞書引き（下）。
/// 未登録の新地点（例：分離建国後の座標微修正）も最近傍で妥当な陸塊に落ちる＝(0,0)のような静かな穴を作らない。
/// ★2026-09-26（高速化・厳密恒等）：答えを座標の**厳密なビット**を鍵に記憶する（<see cref="OfMemo"/>）。Points は型の初期化で
///   1回だけ読む不変の表＝この関数は座標の純関数なので、記憶は答えを1ビットも変えない（量子化しない＝鍵と値が同じ入力から出る）。
///   旧＝呼ぶたびに登録点を全件走査して大圏距離を三角関数で測っていた（全長で1,495万回・176秒＝1試合の18%。照会点は419種類だけ）。</summary>
````

## L88 T `if (d < bd) { bd = d; best = p; }`
````
// 同距離は先勝ち（name順ファイル）＝決定論
````

## L90 T `OfMemo.TryAdd(key, best.LandmassId);`
````
// 並行に同じ鍵を計算しても値は同一（純関数）＝先に入った方が残るだけ
````

## L94-94 W 直後 `public static bool Same(double lat1, double lon1, double lat2, double lon2) => Of(lat1, lo`
````
/// <summary>2点が同じ陸塊か（＝海を越えずに到達できるか）。IsSeaCrossing の基盤。</summary>
````

