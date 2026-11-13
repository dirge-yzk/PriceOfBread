# InstrumentsPxstep.cs の注の履歴

由来：1cd66aae5 の InstrumentsPxstep.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsPxstep.cs#番号〕だけが残る。

## §1　形=full　元の行=5-13　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1024) K2 価格の歩の内訳の計器 pxstep_s（読むだけ・恒等・事前登録 measurements/m1024_20261110_prereg_kf.txt） ----------------------------------------------------------------
// 部分クラス＝InstrumentsScope.cs と同じ作り（蓄えは ConditionalWeakTable・セーブに載らない）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 出すもの（1 試合＝pxstep_s<seed>.tsv）：年×国×格 tierCode×枝 branch の行（件数が 0 の枝は書かない）。
//   枝：labor＝労働の財（日次の写像を回した）・svcClosed＝役務の閉じた式・svcDaily＝役務の日次・goodsClosed＝財の閉じた式・pinnedClosed＝張り付いた財の閉じた式・goodsDaily＝財の日次・
//        dormant＝月の残りを止まっている財（歩に入れなかった）・dead＝需要も産出も無い財（歩に入れなかった）・other＝歩に入れたが上のどれでもない財（0 のはず＝枝の網羅の自己確認）。
//   goodsCount＝その枝の財の件数（財×州×歩）・dailyRuns＝日次の写像を回した日数（labor・svcDaily・goodsDaily だけ 0 でない）。
//   台帳との対応：Σ枝（dormant を除く）の goodsCount＝台帳 PriceGoods（live − dormant）・Σ dailyRuns＝台帳 PriceDailyRuns。
// 書き手は MarketFlow.StepDay の終わりの 1 か所。州は 1 本のスレッドが回す＝蓄えは州ごと（共有のリストを持たない）・集計は整数の和（順に依らない）。
// ⚠限界：①格は呼んだ時点の州の格。②年の終わりに数えるのは今の国が持つ州＝州ごと消えた州の蓄えは落ちる。
`````
