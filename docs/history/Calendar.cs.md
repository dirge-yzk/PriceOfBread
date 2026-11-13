# PriceOfBread.SimCore/Calendar.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-7 W 直後 `public static class Calendar`
````
/// <summary>
/// 暦の構造定数（三層時計：日→月→四半期→年）。マジックナンバー一掃2026-07-20＝
/// 全域に散在していた 90/89/30/360/3/4/12 の裸リテラルを一元化（値は据え置き＝挙動不変・命名のみ）。
/// 除算など double 文脈では呼び出し側で <c>(double)Calendar.X</c> と明示キャストし整数除算化を防ぐ。
/// </summary>
````

## L10 T `public const int DaysPerQuarter = 90;`
````
// 四半期＝90日（日次刻みループ回数）
````

## L11 T `public const int QuarterEndDayIndex = DaysPerQuarter - 1;`
````
// 89＝四半期の締め日索引
````

## L12 T `public const int DaysPerMonth = 30;`
````
// 1か月＝30日（月次決算の境界）
````

## L13 T `public const int DaysPerYear = 360;`
````
// 1年＝360日（Tick→年の換算）
````

## L14 T `public const int MonthsPerQuarter = 3;`
````
// 1四半期＝3ヶ月（month%3 の四半期判定）
````

## L15 T `public const int QuartersPerYear = 4;`
````
// 1年＝4四半期（年送り境界）
````

## L16 T `public const int MonthsPerYear = 12;`
````
// 1年＝12ヶ月（年↔月換算）
````

## L17 T `public const int StartYear = 1783;`
````
// ゲーム開始暦年（Scenario.StartYear の既定と一致）
````

## L18 T `public const int EndYear = StartYear + Coefficients.GameQuarters / QuartersPerYear;`
````
// 終局暦年＝1950（1783+668/4＝167年＝1950年1月1日に終局）。1914は忠実度の主指標の断面であって終局ではない
````

## L20-25 W 直後 `public static int QuarterIndexOfTick(long tick) => (int)(tick / DaysPerQuarter);`
````
// === 時間の単位の換算（README §1-9・2026-10-10＝名前のある関数で換算するのは作者の承認。下の時間の規則〔月・四半期・年の使い分け〕も作者の承認済み＝作者 2026-10-10「時間単位は推奨でよい」）===
// 量の単位は月（市場の刻みだけ日）・四半期は「3か月ごと」の実行の刻み・年は史実との比較の入口と出口（係数の出典・計器・画面）。
// ⚠名前は「何あたりの量を何あたりへ直すか」＝割り算と掛け算を裸で書かない。中身は呼び元が書いていた式そのまま（1回の除算か乗算＝丸めを変えない）。
// ⚠合成するときは1本ずつ通す（PerYearToPerMonth(PerQuarterToPerYear(x)) は x×4÷12 であって x÷3 とビット一致するとは限らない）。
// ⚠流れ（円/月）にも率（/月）にも同じ関数を使う＝掛け算と割り算の向きが同じため。在庫（残高）は換算しない（月と年を掛け割りしない）。例外は <see cref="OneMonthOf"/>＝1か月分の流れを在庫の型へ直す1本（Pop の月末の精算が貯蓄へ足す所・掛け算も丸めも無い）。⚠double を受ける継ぎ目（World.Pay／Transfer の額・MoneyStock のコンストラクタ）は型で守っていない（(251) 反証役 vm#1）。
/// <summary>★(415) 暦の日（Tick）→開始からの四半期の通し番号（計器の間引き用）。量の換算ではない（率・流れの換算の関数とは別）。</summary>
````

## L36-36 W 直後 `public static RatePerMonth PerYearToPerMonth(RatePerYear perYear) => new(PerYearToPerMonth`
````
// 専用の値型の換算（型を受けて型を返す＝年と月を取り違えるとコンパイルが通らない。中身は上の double 版そのもの）。
````

## L39-39 W 直後 `public static RatePerMonth PerQuarterToPerMonth(RatePerQuarter perQuarter) => new(PerQuart`
````
// ★(317) 四半期率（RatePerQuarter）の換算：中身は double 版の 1 回の除算そのもの。★(318) 使い手の無い 2 本（月→四半期・四半期→年）は消した＝使い手が出たときに足す。
````

## L43-44 W 直後 `public static MoneyStock OneMonthOf(MoneyPerMonth perMonth) => new(perMonth.Value);`
````
/// <summary>月の決算：1か月分の流れ（円/月）を、在庫（残高）へ足す量（円）に直す。在庫＋流れの暗黙の演算子は無い＝Pop の月末の精算（SettleMonth）が貯蓄へ足すのはこの1本を通す（中身は値をそのまま包む＝掛け算も丸めも無い）。
/// ⚠型が守るのは演算子だけ＝double を受ける継ぎ目（World.Pay／Transfer の額・MoneyStock のコンストラクタ）は通らない：国家の月末の精算（World.SettleStateSpent）と未払いの利子の元本への繰り入れ（Fiscal）はそちらを通る（(251) 反証役 vm#1・コンストラクタの件数は UnitTypeTests のラチェット）。</summary>
````

## L46-46 W 直後 `public static MoneyStock OneYearOf(MoneyPerYear perYear) => new(perYear.Value);`
````
/// <summary>(332) 年の流れの1年ぶんの在庫（〈年GDP の何倍〉の在庫を作る口・<see cref="ToGdpYear"/> の逆＝比×この値）。月の流れを年に直してから渡す（<see cref="PerMonthToPerYear(MoneyPerMonth)"/>）。</summary>
````

## L48-49 W 直後 `public static RatioToGdpYear ToGdpYear(MoneyStock stock, MoneyPerYear gdpPerYear) => new(s`
````
/// <summary>年GDP 比（国債÷年の名目GDP など）を作る**唯一の口**＝在庫（MoneyStock）÷年の流れ（MoneyPerYear）。月の流れ（MoneyPerMonth）で割る取り違え（年GDP 比の12倍）は型が合わず渡せない＝月の流れは先に `PerMonthToPerYear` を通す。
/// 中身は `stock.Value / gdpPerYear.Value` の1回の除算（呼び元が書いていた式そのまま）。分母のゼロ・微小の守りは呼び手が分母（gdpPerYear）の側で書く（この関数は割るだけ）。</summary>
````

## L51-51 W 直後 `public static double YearsToMonths(double years) => years * MonthsPerYear;`
````
/// <summary>期間の換算（年数→月数）。割合の換算ではない。</summary>
````

## L53-54 W 直後 `public static double OverSteps(double rate, double steps) => 1.0 - Math.Pow(1.0 - rate, st`
````
/// <summary>刻みの換算（率を <paramref name="steps"/> 回に合成する）＝1 回あたりの率 <paramref name="rate"/> を毎回かけて steps 回進んだときの動いた割合 `1 − (1 − rate)^steps`。
/// 中身は呼び元（Credit の旬次の緩和・Meritocracy の四半期の離職）が書いていた式そのまま（Math.Pow を 1 回）。a*a*a と書いた Discovery.PriceSmoothingQ はビットが違うので通さない。</summary>
````

