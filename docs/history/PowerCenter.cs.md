# PriceOfBread.SimCore/PowerCenter.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `internal static class PowerCenter`
````
// ---------------------------------------------------------------- (1090) 王権と議会の段 P1：政府の中心（執政・議会・軍・革命勢力）の取り分の純関数 ----------------------------------------------------------------
// 設計＝docs/202611/20261112/ の王権の設計（下書き・方針は作者「それでよい」2026-11-12・式と段は AI の推奨・確証なし）§3・§4-2。事前登録 measurements/m1090_20261113_prereg_cp.txt。
// 入力は World でなく数の束（段 P2 がエンジンから同じ関数を呼ぶ・試験の期待値を紙の式から作る）。エンジンの状態を読まず・書かず・乱数を引かない。
// 今は計器 InstrumentsCenter だけが呼ぶ（エンジンの挙動 0）。倒し先（否定形）：質量の和が正でなければ ℓ＝1／G＋qM_外 が正でなければ執政＝1／歳入が正でなければ w＝0／NaN は 0 に倒す。
````

## L9-10 W 直後 `internal static (double Loyal, double Army, double Revolt) SoldierSplit(ReadOnlySpan<doubl`
````
/// <summary>兵の分かれ（ℓ＋d_A＋d_V＝1）。思想ごとの質量 mass[k] を、3 つの位置（体制 X・軍政 X_A・革命 X_V）のうち親和が一番高い所へ配る（同じなら体制→軍政→革命の順）。
/// affX／affA／affV[k]＝思想 k から各位置への親和（Ideologies.AffinityOf と同じ物差し）。質量の和が正でなければ ℓ＝1。NaN・負の質量は 0 とみなす（否定形）。</summary>
````

## L27-27 W 直後 `internal static double GOf(double l, double w) { double g = l - w; return g > 0 ? g : 0.0;`
````
/// <summary>国内に居る全ての兵 G＝L−W（前線に縛られた W だけが首都に居ない）。負・NaN は 0。</summary>
````

## L30-30 W 直後 `internal static double HOf(double l, double w, double popTimesQ) { double g = GOf(l, w); d`
````
/// <summary>比べるための旧の定義 H＝min(L−W, 人口×q)（支配の力の H と同じ形）。</summary>
````

## L33-34 W 直後 `internal static double Wallet(BudgetLaw k, double interestShare, double taxYearShare, bool`
````
/// <summary>財布 w（議会が止められる歳入の割合）。勅令＝i（利払い÷歳入）・部分承認＝確率の和 1−(1−t)(1−i)・議会承認＝1。歳入が正でなければ 0（否定形）。
/// interestShare＝利払い÷歳入・taxYearShare＝年ごとに査定する税÷歳入（呼び手が [0,1] に収めて渡す。範囲外・NaN は 0 に倒す）。</summary>
````

## L43-43 W 直後 `internal static BudgetLaw KFromPolity(Polity p) => p switch { Polity.ConstitutionalMonarch`
````
/// <summary>今の政体から K を導く写像（世界のデータに欄が無い間の開幕と途中の導出＝限界①）。</summary>
````

## L46-46 W 直後 `internal static Polity NameOf(BudgetLaw k, CrownForm crown, SeizureForm seizure)`
````
/// <summary>政体の名（K と王冠と奪取から）：奪取あり＝奪取の名／勅令＝王冠の形の名（王冠なし＝ブルジョア独裁）／部分・議会＝王冠ありは立憲・なしは共和。</summary>
````

## L61-61 W 直後 `internal readonly record struct Inputs(`
````
/// <summary>1 つの K の下の評価の入力（World を持たない）。</summary>
````

## L66-66 W 直後 `internal readonly record struct Result(`
````
/// <summary>評価の結果。F＝力（陸軍力の単位）・S＝取り分。</summary>
````

## L74-75 W 直後 `internal static Result Evaluate(in Inputs x, BudgetLaw k)`
````
/// <summary>K を与えたときの 4 つの力と取り分。M_外＝M_急＋M_改外。a_V＝d_V・a_A＝d_A×m_A・体制＝ℓ＋d_A×(1−m_A)・m_A＝押し×(1−弁)・弁＝0.5×((1−任免が執政)＋(1−s_革命))。
/// F_執政＝G×a_E×(1−w×c)・F_議会＝G×(a_E×w×c＋a_P)・F_軍＝G×a_A・F_革命＝G×d_V＋q×M_外（c＝議会のまとまり＝この段は 1）。ΣF＝G＋q×M_外。</summary>
````

## L83-83 W 直後 `double sRev0 = sum > 0 ? (g * dv + q * mExt) / sum : 0.0;`
````
// 革命の取り分は m_A に依らない（輪が無い）：弁は先にこれを読む
````

## L103-104 W 直後 `internal readonly record struct Verdict(bool Violent, bool Peaceful, bool Immediate, bool `
````
/// <summary>点火とクーデタの判定（体制＝執政＋議会と比べる）。F_革命＞F_体制 で点火：急進の側（G×d_V＋q×M_急）が改革の側（q×M_改外）より大きければ暴力・でなければ平和な転換。
/// ν＝F_革命÷F_体制（体制の力が 0 以下なら nan）。ν≧landslide で即時。クーデタ＝G×d_A×m_A＞G×ℓ（比 nuCoup＝d_A×m_A÷ℓ）。</summary>
````

