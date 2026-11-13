# PriceOfBread.SimCore/BudgetBindDiag.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-14 W 直後 `public static class BudgetBindDiag`
````
/// <summary>純テレメトリ（2026-08-21・「買い手の購買力は本当に枯れていないのか」の実測）。
///
/// なぜ要るか＝「需要Σ/生産Σ＝0.92」は**恒等式に近い合成指標**である。
///   demand = min(desired, affordable) と定義され、生産は前期の LastDemand を重みに労働が配分されて決まる。
///   したがって予算で需要が切られれば**生産も一緒に縮む**＝比は 1 付近に戻る。
///   「比が1に近い＝購買力は枯れていない」は**論理として成立しない**（切られた側と切られた結果を比べている）。
///   予算制約が binding かどうかを見る唯一の物差しは **desired（希望）と affordable（払える）の差**であり、
///   実現値どうしの比ではない。ここはその差を直接数える。
///
/// <see cref="On"/> が false の間は EconomyStep から**1命令も呼ばれない**（分岐の中）＝挙動はビット恒等。
/// 力学は一切読まない（ここに書いた値を清算が参照する箇所は存在しない）。
/// </summary>
````

## L19-21 W 直後 `public static readonly Dictionary<World, double[]> ByWorld = new();`
````
/// <summary>国（World）ごとの計数器。添字は <see cref="Idx"/>。
/// ⚠通常の Dictionary＝スレッド安全でない。★2026-09-26（sweep-25）：<see cref="On"/> の間は MultiNationSim が各国の日次を逐次へ倒す
/// （ConservationProbe・TradeReachDiag と同じ扱い）。1プロセスで試合を並列に回す selfplay-batch では On にしないこと（数が混ざる・World を抱えたまま残る）。</summary>
````

## L24 T `public const int Rows = 0;`
````
// (pop×財) の行数（marketNeed>0 のものだけ）
````

## L25 T `public const int BoundRows = 1;`
````
// うち affordable < desired ＝予算で切られた行
````

## L26 T `public const int DesiredVal = 2;`
````
// 希望額 Σ desired×buyerPrice（金額＝財をまたいで足せる）
````

## L27 T `public const int DemandVal = 3;`
````
// 実際に立った需要額 Σ min(desired,affordable)×buyerPrice
````

## L28 T `public const int DesiredValTrade = 4;`
````
// うち貿易可能財（NonTradeable/役務でない）
````

## L36 T `public const int BudgetStart = 12;`
````
// 月初の消費予算 Σ p.RemainingBudget（＝貯蓄）
````

## L37 T `public const int BudgetLeft = 13;`
````
// 消費ループを抜けた時点の使い残し Σ p.RemainingBudget
````

## L38 T `public const int PaidVal = 14;`
````
// 実際に払った額 Σ p.Paid
````

## L39 T `public const int PopDemVal = 15;`
````
// LastDemand の内訳（金額建て・m.Price）：pop消費（＝予算で切られる唯一の項）
````

## L40 T `public const int InputDemVal = 16;`
````
// 工場投入
````

## L41 T `public const int InvestDemVal = 17;`
````
// 建設投資
````

## L42 T `public const int MilDemVal = 18;`
````
// 軍需
````

## L43-43 W 直後 `public const int Count = 19;`
````
// 【削除2026-09-26（dorm-49・econ-15）】TradeQRaw・TradeQRawDesired（買い手の希望輸入量と、その scale を予算の前の需要へ置き換えた反実仮想）＝旧経路の中だけで書かれていた
````

## L54-54 W 直後 `public static void Row(World? w, int tier, bool tradeable, double desired, double demand, `
````
/// <summary>1行＝1 pop×1財。金額建て（buyerPrice）で積む。</summary>
````

## L72-73 W 直後 `public static void Compose(World? w, double pop, double input, double invest, double mil)`
````
/// <summary>LastDemand（＝交易の scaleT の素）の内訳。q ∝ scaleT ∝ LastDemand なので、
/// 予算で切られる pop 消費がこの中で何割かが「購買力→交易」チャネルの利得率そのものになる。</summary>
````

## L81-81 W 直後 `public static void Budget(World? w, double start, double left, double paid)`
````
/// <summary>州の消費ループを抜けた時点の予算（月初／使い残し／実支払い）。</summary>
````

