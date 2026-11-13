# PriceOfBread.SimCore/AgeLayout.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-8 W 直後 `public sealed class AgeLayout`
````
/// <summary>★年齢の層の器（2026-10-21 (478)・死の側の段1・設計 docs/202610/20261015/20261015_人口の作り替え_出生と死亡_設計.md §2-2・事前登録 measurements/dz478_20261021_prereg.txt）
/// ＝層の数・下端・幅・標準のピラミッド・大人の境を1か所に持つ。層の番号を使う述語は全て年齢の述語として書く（この器の値から引く）。
/// <see cref="Bands8"/>＝今の10歳刻み8層（0〜9・10〜19・…・60〜69・70以上）／<see cref="Bands11"/>＝作者の刻み（0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71以上）。
/// どちらを使うかはプロセス全体で1つ（環境変数 <c>POB_AGE_BANDS</c>＝未設定か11→<see cref="Bands11"/>〔2026-10-21 に既定を作者の11層へ〕・8→<see cref="Bands8"/>〔旧の8層＝以前の成績表の再現用〕・ほかは例外）＝<see cref="Pop.Layout"/>。
/// 計算の読み口（<see cref="Pop.AgeShareOf"/> ほか）は配列の長さ（2×層の数）から器を引く＝グローバルを読まない（11層の算術を同じプロセスの番人が直接叩ける）。
/// ⚠8層の定数は旧の式と**同じ double**にしてある（下端 10.0×b・幅 10.0・幅×12＝120.0）＝旧の `ov / 10.0`・`band / 120.0` と1ビットも違わない（既定の恒等の根）。</summary>
````

## L11-11 W 直後 `public const int MaxBands = 11;`
````
/// <summary>層の数の最大（stackalloc の確保の大きさ＝定数で確保して 2×層の数に切る）。</summary>
````

## L13-13 W 直後 `public const int MaxSlots = 2 * MaxBands;`
````
/// <summary>層×性の配列の長さの最大。</summary>
````

## L17-17 W 直後 `public readonly double[] Lower;`
````
/// <summary>層の下端の年齢（最上層は開いた区間の下端）。</summary>
````

## L19-19 W 直後 `public readonly double[] Upper;`
````
/// <summary>層の上端の年齢（＝下端＋幅・最上層は使わない）。</summary>
````

## L21-21 W 直後 `public readonly double[] Width;`
````
/// <summary>層の幅（年・最上層は使わない＝0）。</summary>
````

## L23-23 W 直後 `public readonly double[] MonthsPerBand;`
````
/// <summary>幅×12（月）＝加齢の流れ（層÷これ／月）の分母。最上層は使わない＝0。</summary>
````

## L25-25 W 直後 `public readonly double[] Pyramid;`
````
/// <summary>標準の前近代ピラミッド（男女計・割合・和1）＝1783年の全ての POP の初期の構成。</summary>
````

## L27-27 W 直後 `public readonly int AdultFirstBand;`
````
/// <summary>20歳以上の最初の層（職業 POP の性比・性別の門が見る「大人」の最初の層）。</summary>
````

## L29-29 W 直後 `public readonly int EntryBand;`
````
/// <summary>大人に入る前の最後の層（この層から上へ出る流れ＝世代の置き換わり）。</summary>
````

## L31-31 W 直後 `public readonly double AdultAge;`
````
/// <summary>大人に入る年齢（世代の置き換わりの「大人」の下端・<see cref="EntryBand"/> の上端）。</summary>
````

## L33-33 W 直後 `public readonly int SoldierBandA, SoldierBandB;`
````
/// <summary>兵士の初期の構成（男の20〜39歳）の2層。</summary>
````

## L35-35 W 直後 `public readonly int OldBandA, OldBandB;`
````
/// <summary>60歳以後の余命の計器が読む2層（幅10年の層とその上の開いた層）。</summary>
````

## L46-46 W 直後 `public static readonly AgeLayout Bands8 = new(`
````
/// <summary>今の10歳刻み8層。標準のピラミッド＝設計書 §3-1 の表（平均寿命30前後・年0.5%増の安定人口・15歳未満 35.3%・60歳以上 7.5%）。</summary>
````

## L53-56 W 直後 `public static readonly AgeLayout Bands11 = new(`
````
/// <summary>作者の11層（0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71以上）。
/// 標準のピラミッド（データ）＝今の8層の累積分布（10歳刻みの境で厳密に通る）を単調な3次補間（Fritsch–Carlson）で新しい境へ引いた物。0歳の層だけ 0.030
/// （粗出生 0.035÷(1＋乳児の死亡率＋増加率 0.005)の安定人口の形・乳児死亡 約14%）。71以上は70の境の密度で0.7年延ばして切る。導出の script＝scripts/instruments/dz478/std_readers.py
/// （AI の決め〈確証なし〉＝出典を持つ生命表から導いた物ではない）。和＝1・死の年齢の重み μ は全層で正（最小 0.0102）。</summary>
````

## L63-63 W 直後 `public static AgeLayout OfLength(int length)`
````
/// <summary>配列（層×性の割合か人数・長さ 2×層の数）の長さから器を引く。</summary>
````

## L67-67 W 直後 `internal static AgeLayout FromEnvironment() => Parse(Environment.GetEnvironmentVariable("P`
````
/// <summary>環境変数 POB_AGE_BANDS を1回だけ読む（<see cref="Pop.Layout"/>）。未設定か空か 11 → 11層／8 → 旧の8層／ほかは例外。</summary>
````

## L70-70 W 直後 `internal static AgeLayout Parse(string? v)`
````
/// <summary>環境変数の値の読み（純関数＝番人がプロセスの環境を触らずに叩く）：null・空・"11" → 11層／"8" → 旧の8層／ほかは例外（空白つきも不可）。</summary>
````

## L74-74 W 直後 `internal double Overlap(int b, double lo, double hi)`
````
/// <summary>層 <paramref name="b"/> と [lo, hi) の重なり（層の幅に対する割合・層の中は一様）。最上層は分けない（lo が下端以下で hi が無限大のときだけ全部）。</summary>
````

## L82-83 W 直後 `internal double Outflow(int b, double band) => band / MonthsPerBand[b];`
````
/// <summary>★加齢の流れの1本（月次）＝層 <paramref name="b"/> から上の層へ1か月に出ていく人数＝層の人数 <paramref name="band"/> ÷（幅×12）（層の中は一様・設計書 §2-2）。
/// 加齢（StepAges・AgeOneMonth）・死の年齢の重みの導出（MortalityWeights）・世代の置き換わり（EnteringAdultsMonthlyOf）が同じ1本を読む。</summary>
````

