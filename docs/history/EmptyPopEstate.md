# PriceOfBread.SimCore/EmptyPopEstate.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L4-8 W 直後 `internal static class EmptyPopEstate`
````
/// <summary>★(848) 線 c69eh：人の居ない POP の口座のお金の相続 R1（★(839) ② の (a)・作者 2026-11-05〈作者「全て推奨で」で推奨を採った〉・確証なし・事前登録 measurements/m848_20261105_prereg_eh.txt）。
/// 旗 <see cref="Coefficients.EmptyPopEstate"/> ＞ 0 のとき、国の全ての州で、人数 ≦1e-12（<see cref="Pop.Blend"/> の門と同じ）で在庫が正の POP の在庫の全額を、
/// 同じ州の人数 ＞1e-12 の POP へ人数の比で移す（移し方の 1 本 <see cref="World.Transfer"/>・払い手ごとに自分の在庫を全額・受け手は頭数・所得には書かない）。
/// 州に人の居る POP が無い（人数の和 ≦1e-9）ときは移さない。州の種類で分けない。呼び手＝<c>Simulation.DemographicStep</c> の相続の段の前（月に 1 回・助走は呼ばれない）。
/// 新しい状態なし・乱数なし。旗 0 は計器の呼びの後に早い return（リストを確保しない）。★(910) 旗 <see cref="Coefficients.EmptyPopDebtFirst"/> ＞0 では、人の居ない POP の債務を相続より先に返す（:22・事前登録 measurements/m910_20261107_prereg_ep.txt）。</summary>
````

## L11 T `internal static void Step(World w) { Core(w); if (Instruments.On) Instruments.EstateAfter(`
````
// (920) 計器（読むだけ）：相続の段の直後の断面。本体は下の Core（この 1 行に置く＝:31 の移しの行番号を動かさない）
````

## L13 T `if (Instruments.On) Instruments.EstateDue(w);`
````
// 計器（読むだけ）：その月に移るはずの額（計器が自分で数える）
````

## L22 T `if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Valu`
````
// ★(910) c78ep 案 B5（旗 EmptyPopDebtFirst・既定 0）：相続の前に人の居ない POP の債務を自分の貯蓄から返す（Bank.Lend の負の側＝決済 ① と同じ書き手・受け手の有無に依らない・比較が偽＝NaN は返さない）→ 貯蓄を読み直して残りを今の規則で相続人へ。この 1 行に置く＝:31 の移しの行番号（cearn_s の経路の名）を動かさない
````

## L28 T `int big = 0;`
````
// ★(848) §11：人数が最大の受け手を末尾へ（Pop.Distribute は端数を末尾の受け手へ寄せる＝末尾の人数がごく小さいと 1 人あたりの受け取りが 1 本にならない）
````

