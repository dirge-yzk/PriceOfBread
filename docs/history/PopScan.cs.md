# PriceOfBread.SimCore/PopScan.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-9 W 直後 `public readonly struct PopScan`
````
/// <summary>★州×POP の走査の名前のある入口（2026-10-15 (351)・案 A・結果を変えない）。
/// 旧＝州のループの中の POP のループを各所が書き写し、集合の定義が揃っていなかった（稼働中の州で絞る所と絞らない所・<see cref="World.AllPops"/> は絞らない）。
/// 今は集合が 1 つ・名前がある：
/// ・<see cref="World.AllPopsWithRegion"/>＝国家会計の集合（全ての州の全ての POP・州の並び→州の中の POP の並び・絞らない）＝人口保存・貯蓄の合計・国債の保有の台帳・州の価格水準・州の人口の目安が読む集合。
/// 稼働中の州だけの集合（旧 <c>ActivePopsWithRegion</c>）は (568) gc3 で畳んだ＝本番の盤面（2026-08-26 に辺境を廃止）には稼働していない州が無く、2 つは同じ列だった。
/// 兵士・規模 &gt; 1e-9・閉鎖階級（<c>Closed</c>）の除き方は呼び手ごとに違う＝この列挙には入れない（POP の述語 <c>IsSoldier</c>・<c>Closed</c> を呼び手が読む）。
/// 走査の順は旧の入れ子の <c>foreach</c> とまったく同じ＝和の順も変わらない。値型の列挙子＝割り当てなし。</summary>
````

## L27-28 W 直後 `if (_regions.Count != _regionCount || (_pops is not null && _pops.Count != _popCount)) thr`
````
// ★(352) 反証役〔pu v〕低：走査の途中で一覧の数が変わったら落とす（旧の入れ子の foreach は List の版の検査で落ちた＝黙って飛ばす・二重に読む形にしない）。
//   数だけを見る＝同じ数のままの入れ替えは見えない（旧の版の検査より弱い）。今の呼び手は走査の中で一覧を変えない（旧なら落ちていた）＝値は変わらない。
````

## L40-48 W 直後 `internal sealed class PopWindow`
````
/// <summary>★窓の中だけの和の使い回し（2026-10-15 (351)・案 A・結果を変えない・作業領域＝セーブに載らない）。
/// 窓＝<see cref="World.PopSums"/> が非 null の間。書き手のいない区間の中で、同じ入力から同じ順で足した和を 1 回だけ求めて配る（前例＝<see cref="Simulation.CreditMonth"/>・<c>Diplomacy.SyncGdp</c>）。
/// 窓の外（単独の呼び・試験の直呼び）は毎回求める＝旧と同じ。置く窓は四半期首の「近代化投資〜政府の反映の終わり」だけ（<see cref="Simulation"/> の <c>QuarterStart</c>）。
/// ・窓に入れない段：AI の決定（<c>DecideQuarter</c>＝着工・払い下げ〔<c>SetStateTarget</c>〕が資本家の貯蓄を動かす）と議会（<c>Parliament.Step</c>＝総選挙の公示への AI の答え
///   <c>World.ResolveElection</c> が国庫→有権者の貯蓄を動かす）。議会を窓に入れた最初の木は canary（selfplay 1 200）で軌道が変わった＝<see cref="Verify"/> が止めた。
/// ・窓の中の段（近代化投資・政府の反映）が書くのは国の欄・州の欄・法の欄・闘争度ほか＝POP の規模・貯蓄・地代・所得・年齢の層は書かない（実装ログ (351) の読みと、
///   <see cref="Verify"/>＝環境変数 <c>POB_POPSCAN_VERIFY=1</c> で使い回すたびに求め直して 1 ビットも違わないことを確かめる番人）。
/// 使い回す和は 4 つ：総人口・兵士の数・利益集団の力の分母（<see cref="InterestGroups.Blocs"/> の第 1 走査）・政策の立場の土台の POP 由来の和（<see cref="PolicyPreferences.Build"/> の第 3 走査）。
/// 国の入力（法・係数・国庫）に依る第 2 走査（力の重み）は使い回さない＝政府の反映の途中で法が動く。</summary>
````

## L51-51 W 直後 `internal static bool Verify = InitVerify();`
````
/// <summary>番人の旗（環境変数 <c>POB_POPSCAN_VERIFY=1</c> で既定が真）。真なら窓の中の読みごとに求め直し、ビットが違えば例外。既定は偽＝実行ゼロ。試験は立てて回す（<c>PopScanTests</c>）。</summary>
````

## L54-54 W 直後 `private static bool InitVerify()`
````
/// <summary>★(352) 反証役〔pu v〕中：旗が実行の中で立っていた証拠＝環境変数で立ったときだけ、終わりに照合の回数を標準エラーへ 1 行出す（旗が立たない既定の実行は何も出さない）。</summary>
````

## L62-62 W 直後 `internal static long Checks, Opened;`
````
/// <summary>番人の数え（<see cref="Verify"/> が真のときだけ増える）。</summary>
````

## L65-65 W 直後 `[System.ThreadStatic] internal static int Open;`
````
/// <summary>このスレッドで開いている窓の数（番人 <see cref="Verify"/> が窓の中の POP への書き込みを止めるために数える）。窓は四半期首の 1 スレッドの中で開閉する。</summary>
````

## L68-69 W 直後 `internal static void GuardWrite(string what)`
````
/// <summary>POP の規模・貯蓄の書き込み口（<c>Pop.Distribute／Take／Carry／SettleMonth／Blend</c>・<c>World.KillPops</c>）の頭が呼ぶ番人。
/// <see cref="Verify"/> が真で窓が開いている間の書き込みは例外（使い回しの前提が壊れる＝読みの照合を待たずに、書いた段で止める）。既定（偽）は旗を 1 回読むだけ。</summary>
````

## L101-101 W 直後 `internal PolicyPreferences.BuildSums BuildSums(World w)`
````
/// <summary>政策の立場の土台の POP 由来の和。呼び手が正規化で書き換えるので、呼び手へは**写し**を渡す。</summary>
````

