# PriceOfBread.SimCore/GovernanceSnapshot.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-10 W 直後 `public sealed class GovernanceSnapshot`
````
/// <summary>
/// Runner の freshboard 診断専用：経済を助走で整えつつ、法律・財政・国庫を「開始年初期」へ戻すための退避/復元
/// （本番の <see cref="MultiNationSim.WarmupEconomy"/> は <see cref="World.WarmupFrozen"/> で暦・人口・識字・技術・政治を止めるので本クラスを呼ばない）。
/// 助走の直前に <see cref="Capture"/> し、助走の直後に <see cref="RestoreTo"/> する。
/// 政治（意識・闘争度・政体）は助走中 PoliticsEnabled=false で凍結、外交（戦争・緊張・植民地）は
/// 助走中 <see cref="MultiNationSim.DiplomacyFrozen"/> で凍結されるため、ここでは対象外（不変なので戻す必要がない）。
/// 経済（価格・在庫・生産能力・工業化・制度）は助走後の状態を保持する。
/// </summary>
````

## L13-13 W 直後 `private EducationLaw _eduLaw; private Suffrage _suffrage;   // ★親軍法（_militarism）は2026-08-1`
````
// 法律・政治（現在値）
````

## L14 T `private EducationLaw _eduLaw; private Suffrage _suffrage;`
````
// ★親軍法（_militarism）は2026-08-18に撤去＝軍国主義は PoliticalQuantities の4実体へ分解（法ではなくなったので退避対象そのものが消えた）
````

## L15-17 W 直後 `private CensorshipLaw _censorship; private LaborUnionLaw _laborUnion; private WomenRightsL`
````
// ~~⚠申し送り：4実体が読む ConscriptionLaw / Bureaucracy は**ここで退避していない**（助走で両者がドリフトすると練度と文民制度の弁が助走後も戻らない）。~~
//   → 2026-09-30 (119) 助走そのものが法を動かさなくなった（Government.Step の施行は World.WarmupFrozen の間は走らない）＝両者も助走で動かない＝穴は閉じた。
//   以後この退避の法の欄は助走の前後で同じ値を書き戻すだけ（目標＝Policy と国庫・銀行は助走で動く＝ここの復元に意味が残る）。
````

## L18 T `private CensorshipLaw _censorship; private LaborUnionLaw _laborUnion; private WomenRightsL`
````
// ★女性の権利（統合法・2026-08-10）＝旧・財産権/参政権の2boolを1段へ（教育権は旧は捕捉漏れ＝統合で自動的に含まれる）
````

## L19 T `private ReformLevel _cornLaws, _serfEmancipation, _landReform;`
````
// 改革法の現在値（穀物法・農奴解放・土地改革）＝初期の史実差別化を助走後も保持
````

## L20 T `private WorkHours _workHours;`
````
// 労働時間の現在値（A-1#2）＝助走後も初期値を保持
````

## L21 T `private bool _childLaborAllowed;`
````
// 児童労働法の目標（Policy）＝助走後の連続合法度の復元アンカー（倍率排除④）
````

## L23 T `private CrownForm _crown; private SeizureForm _seizure; private double _parliamentControl;`
````
// P2登録簿＋議会統制力（助走整合2026-07-11）
````

## L24-24 W 直後 `private Share _navalUpkeepShare;   // ★陸海の維持費の按分（2026-08-04）＝軍事費だけ戻すと海軍が消える`
````
// 財政（現在値）
````

## L25 T `private Share _navalUpkeepShare;`
````
// ★陸海の維持費の按分（2026-08-04）＝軍事費だけ戻すと海軍が消える
````

## L30-30 W 直後 `private double _treasury;`
````
// 国庫
````

## L32-32 W 直後 `private NationPolicy _policy = new();`
````
// 政策（スライダーの目標値）
````

## L59 T `w.WorkHoursValue = _workHours.Hours();`
````
// 実労働時間も法定上限のアンカーへ戻す（助走中は政治凍結で不動だが整合のため・倍率排除②）
````

## L60 T `w.WomenWageRetention = w.WomenPropertyLaw ? 1.0 : 0.0;`
````
// 賃金保持も財産権の段のアンカーへ戻す（倍率排除④・派生プロパティ＝上の復元で確定済み）
````

## L61 T `w.ChildLaborLegal = _childLaborAllowed ? 1.0 : 0.0;`
````
// 児童労働の合法度も法のアンカーへ戻す（倍率排除④）
````

## L64 T `if (Instruments.On) Instruments.CrownSource(w, Instruments.CrownSrcUnknown);`
````
// (928) 計器（読むだけ）：復元＝書き手は不明
````

## L68 T `w.RebaseTreasury(_treasury);`
````
// 助走で動いた分は台帳ごと捨てる（暦の外＝歴史に属さない）
````

## L70 T `if (w.Bank is { } bank) { bank.Loans = default; bank.HouseholdLoans = default; bank.Confid`
````
// 1783＝負債なし・健全な信用（HouseholdLoansも同じOutstandingLoans恒等式の片割れ＝wave2b）
````

## L71 T `foreach (var (_, p) in w.AllPopsWithRegion()) p.Debt = default;`
````
// 債務の記録も同じく（家計の与信＝POP の債務の和・S3b の輸入の為替手形）
````

