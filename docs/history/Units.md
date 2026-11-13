# PriceOfBread.SimCore/Units.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-16 W 直後 `public interface IUnitValue`
````
// === 専用の値型（README §1-9・2026-10-10〔246〕線T後半）===
// 目的＝単位やスケールの違う量の混ぜ合わせ（年率と月率の加算・在庫と流れの比較・月と年の取り違え）を、コンパイルが通らない形にする。
// 中身は double 1つ（readonly record struct＝速度は変わらない）。許す演算は次だけ：
//   ・同じ型どうしの加減・符号反転／スカラー（double）倍と除算／同じ型どうしの比（無次元の double を返す）／同じ型どうしの比較
//   ・★(256) 割合 Share だけは型をまたぐ積を持つ＝割合×割合→割合・割合×お金（MoneyStock・MoneyPerMonth・MoneyPerYear）→同じお金の型（左右どちらからも）。
//     割合＋お金・割合と年GDP 比（RatioToGdpYear）の比較は書けない（UnitTypeTests の演算子の一覧が固定）。
//   ・月と年の換算は Calendar の名前のある関数（型を受けて型を返す多重定義）だけ。年GDP 比を作る口は Calendar.ToGdpYear の1本だけ。
//   ・暗黙の double への変換は作らない＝取り出しは明示の .Value（計算の順序を変えずに包む・中身の式は組み替えない）。
// ⚠演算子の中身は元の double の 1 演算そのもの（a*b と b*a、a+b と b+a の交換だけが結果を変えない）。
// ⚠== は double の == と同じ（NaN は等しくない）。record の既定の等値は double.Equals（NaN==NaN が真）なので Equals を上書きしている（record の == と != はそれを呼ぶ）。
// ⚠並べ替えと最大最小は double と同じ順序（IComparable＝OrderBy・Max は型を要求するだけでコンパイルは通るので、無いと実行時に落ちる）。
// ⚠書式は double と同じ（IFormattable＝$"{x:F0}" が書式を無視して型名を出す事故を防ぐ）。
// ⚠セーブは SaveGame の Tag.Unit の分岐（型名＋ double 1つ）。POB_COEF などの係数の名前引きは UnitValues を通す。
// 新しい型を足すときは必要性（取り違えた実例）を実装ログに書き、SaveGame の savetest と UnitTypeTests の一覧に載せる。
````

## L18-18 W 直後 `public interface IUnitValue`
````
/// <summary>専用の値型の共通の口（SaveGame・係数の名前引き・番人が double 1つを取り出す／包み直す）。</summary>
````

## L24-24 W 直後 `public static class UnitValues`
````
/// <summary>専用の値型と double の橋（反射する道具だけが使う＝計算の本体は .Value を直に書く）。</summary>
````

## L27-27 W 直後 `public static bool IsUnit(Type t) => t.IsValueType && typeof(IUnitValue).IsAssignableFrom(`
````
/// <summary>double を 1つ包む値型か。</summary>
````

## L29-29 W 直後 `public static bool TryToDouble(object? v, out double d)`
````
/// <summary>反射で読んだ値（double・int・専用の値型）を double にする。それ以外は false。</summary>
````

## L40-40 W 直後 `public static object FromDouble(Type t, double d) => t == typeof(double) ? d : Activator.C`
````
/// <summary>型 t（double か専用の値型）の値を double から作る（反射の書き込み用）。</summary>
````

## L44-44 W 直後 `public readonly record struct RatePerYear(double Value) : IUnitValue, IFormattable, ICompa`
````
/// <summary>年率（1年あたりの割合）。RatePerYear どうしの加減・スカラー倍・比だけが書ける。</summary>
````

## L53-53 W 直後 `public static double operator /(RatePerYear a, RatePerYear b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L59-59 W 直後 `public bool Equals(RatePerYear other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L62-62 W 直後 `public int CompareTo(RatePerYear other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L68-68 W 直後 `public readonly record struct RatePerMonth(double Value) : IUnitValue, IFormattable, IComp`
````
/// <summary>月率（1か月あたりの割合）。RatePerMonth どうしの加減・スカラー倍・比だけが書ける。</summary>
````

## L77-77 W 直後 `public static double operator /(RatePerMonth a, RatePerMonth b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L83-83 W 直後 `public bool Equals(RatePerMonth other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L86-86 W 直後 `public int CompareTo(RatePerMonth other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L92-92 W 直後 `public readonly record struct RatePerQuarter(double Value) : IUnitValue, IFormattable, ICo`
````
/// <summary>四半期率（3か月ごとの実行の刻みあたりの割合：社会移動・越境移民の上限など）。RatePerQuarter どうしの加減・スカラー倍・比だけが書ける。</summary>
````

## L101-101 W 直後 `public static double operator /(RatePerQuarter a, RatePerQuarter b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L107-107 W 直後 `public bool Equals(RatePerQuarter other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L110-110 W 直後 `public int CompareTo(RatePerQuarter other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L116-116 W 直後 `public readonly record struct MoneyPerMonth(double Value) : IUnitValue, IFormattable, ICom`
````
/// <summary>月の流れのお金（1か月あたりの金額）。MoneyPerMonth どうしの加減・スカラー倍・比だけが書ける。</summary>
````

## L125-125 W 直後 `public static double operator /(MoneyPerMonth a, MoneyPerMonth b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L131-131 W 直後 `public bool Equals(MoneyPerMonth other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L134-134 W 直後 `public int CompareTo(MoneyPerMonth other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L140-140 W 直後 `public readonly record struct MoneyPerYear(double Value) : IUnitValue, IFormattable, IComp`
````
/// <summary>年の流れのお金（1年あたりの金額）。MoneyPerYear どうしの加減・スカラー倍・比だけが書ける。</summary>
````

## L149-149 W 直後 `public static double operator /(MoneyPerYear a, MoneyPerYear b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L155-155 W 直後 `public bool Equals(MoneyPerYear other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L158-158 W 直後 `public int CompareTo(MoneyPerYear other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L164-165 W 直後 `public readonly record struct MoneyStock(double Value) : IUnitValue, IFormattable, ICompar`
````
/// <summary>在庫のお金（ある時点の残高：国庫・国債の残高・貯蓄）。MoneyStock どうしの加減・スカラー倍・比だけが書ける。流れ（MoneyPerMonth）との加減は書けない＝Pop の月末の精算は Calendar.OneMonthOf を通す。
/// ⚠コンストラクタは public＝double の継ぎ目（World.Pay／Transfer・国家の月末の精算 SettleStateSpent・利子の繰り入れ）は型で守っていない（(251) 反証役 vm#1・件数は UnitTypeTests のラチェット）。</summary>
````

## L174-174 W 直後 `public static double operator /(MoneyStock a, MoneyStock b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L180-180 W 直後 `public bool Equals(MoneyStock other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L183-183 W 直後 `public int CompareTo(MoneyStock other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L189-189 W 直後 `public readonly record struct RealGdpPerMonth(double Value) : IUnitValue, IFormattable, IC`
````
/// <summary>実質の月の産出（参照価格×量・貨幣ではない物差し。World.GdpPerMonth と Power の GDP）。RealGdpPerMonth どうしの加減・スカラー倍・比だけが書ける。名目の月の流れのお金（MoneyPerMonth）との加減は書けない＝名目への換算は World.NominalOf の1本だけを通す。</summary>
````

## L198-198 W 直後 `public static double operator /(RealGdpPerMonth a, RealGdpPerMonth b) => a.Value / b.Value`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L204-204 W 直後 `public bool Equals(RealGdpPerMonth other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L207-207 W 直後 `public int CompareTo(RealGdpPerMonth other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L213-216 W 直後 `public readonly record struct Share(double Value) : IUnitValue, IFormattable, IComparable<`
````
/// <summary>割合（0〜1 の無次元の割合：税率・国家の支払い率・歳入シェアなど）。Share どうしの加減・積（Share×Share は Share）・スカラー倍と除算・同じ型どうしの比・比較と、
/// お金（MoneyStock・MoneyPerMonth・MoneyPerYear）との積（お金のまま）が書ける。⚠割合＋お金・割合と年GDP 比（RatioToGdpYear＝在庫÷年の流れ・1 を超える）の比較はコンパイルが通らない。
/// ⚠0〜1 に収める上限・下限は型に持たない（設計思想⑧＝選んだ値を型で削らない）。⚠スカラー倍（double）は型を変えない＝`(share * flow.Value)` のような型を外した式は型では防げない。
/// 割合とお金の積の演算子はこの型の中に置く（お金の型の演算子の一覧は変えない）。</summary>
````

## L225-225 W 直後 `public static Share operator *(Share a, Share b) => new(a.Value * b.Value);`
````
/// <summary>割合×割合＝割合。</summary>
````

## L227-227 W 直後 `public static double operator /(Share a, Share b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L229-229 W 直後 `public static MoneyStock operator *(Share a, MoneyStock m) => new(a.Value * m.Value);`
````
/// <summary>割合×お金＝お金（同じ型のまま）。</summary>
````

## L240-240 W 直後 `public bool Equals(Share other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L243-243 W 直後 `public int CompareTo(Share other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

## L249-252 W 直後 `public readonly record struct RatioToGdpYear(double Value) : IUnitValue, IFormattable, ICo`
````
/// <summary>年GDP 比（在庫÷年の流れ：国債÷年の名目GDP など。単位は「年」で 1 を超える）。0〜1 の割合（<see cref="Share"/>）とは別の型で、足せないし比べられない。
/// RatioToGdpYear どうしの加減・スカラー倍・比だけが書ける。⚠作る口は <see cref="Calendar.ToGdpYear"/>（MoneyStock ÷ MoneyPerYear）の1本だけ
/// ＝在庫を月の流れで割る取り違え（年GDP 比の12倍・(220)(251)）は渡せない。⚠public のコンストラクタ（record の主コンストラクタ）は反射の書き込み・セーブの読みが使う＝
/// 計算から作る口は上の1本（`UnitTypeTests` が型を返す関数を数える）。</summary>
````

## L261-261 W 直後 `public static double operator /(RatioToGdpYear a, RatioToGdpYear b) => a.Value / b.Value;`
````
/// <summary>同じ型どうしの比＝無次元（double）。</summary>
````

## L267-267 W 直後 `public bool Equals(RatioToGdpYear other) => Value == other.Value;`
````
/// <summary>record の == と != はこの Equals を呼ぶ＝double の == と同じ（NaN は等しくない）。</summary>
````

## L270-270 W 直後 `public int CompareTo(RatioToGdpYear other) => Value.CompareTo(other.Value);`
````
/// <summary>並べ替え・最大最小（OrderBy・Max・Min が実行時に要る）。double と同じ順序。</summary>
````

