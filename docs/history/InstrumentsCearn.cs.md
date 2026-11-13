# InstrumentsCearn.cs の注の履歴

由来：1cd66aae5 の InstrumentsCearn.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCearn.cs#番号〕だけが残る。

## §1　形=full　元の行=4-20　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (822) 植民地の稼ぎの表 cearn_s（読むだけ・恒等・事前登録 measurements/m822_20261104_prereg_ce.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCpwd.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（計器の欄は ConditionalWeakTable と ThreadStatic だけ）。
// 目的＝(814) の凍結（植民地の州のお金が月 1.5〜2.3% で縮み、工場と採取の産出が 0 で止まる）の出口を、州の POP の貯蓄の台帳から読む。
// 行＝全ての国の全ての州×四半期（region_s と同じ母集合）。額は州の持ち主の国の通貨・四半期の和（…PerQuarter）。助走の月は数えない（Phase の warm で捨てる）。
// 台帳の出所＝貯蓄の書き手の 1 本の計器の口 Sav（Pop の中の書き手がそこを呼ぶ）。POP の州は計器が自分で持つ登録（月初の Begin で全ての国の全ての州の POP）。
//   列は書き手の種類・相手の側（P＝POP・T＝国庫・B／M／F／C／H＝世界の外）・国庫の区分（World.Transfer の頭で写す）・呼び手のファイル名で決める（行番号は列の鍵にしない）。
//   POP→POP は払い手の州で流出・受け手の州で流入。州の中の運搬（社会移動・徴兵・州の中の移動）は同じ州の出と入りが打ち消す。
// 流入 7 列：inMarket（月末の精算の所得の側）・inExport（世界の外〔B・M・F〕から・CapitalMarket.cs 以外）・inState（国庫から）・inCredit（与信 C・H から）・inFromPops（他の POP から）・
//   inMigration（運搬の受け取り）・inOther（CapitalMarket.cs の世界の外から・換算の正・どれでもない受け取り）。
// 流出 9 列（正の額）：outMarket（精算の支払いの側＝所得の側−精算の増分）・outTax（国庫へ・区分 Tax）・outBond（国庫へ・区分 Borrow）・outDues（Fiscal.cs の POP→POP＝十分の一税と仲介者の取り分）・
//   outColonialRent（Colonial.cs の POP→POP＝植民地の取り分）・outCapital（CapitalMarket.cs の POP→世界の外＝外資の配当・資本の送り出し・逃避）・outImport（BilateralMarket.cs の POP→世界の外＝輸入の代金と運賃）・
//   outMigration（運搬の送り出し）・outOther（それ以外）。
// 閉じ：residual＝savings−savingsStart−(Σ流入−Σ流出)。経路の上位＝精算と換算を除く書き込みの鍵（ファイル:行:種類:相手の側）の和の上位（行番号は読み手が測った木で名前へ引く）。
// 値段と床・凍結・人頭税の物差しの列は四半期の末の断面（州の生産者・市場・POP を計器が自分で読む・エンジンの関数を呼ばない・自己費用は読み手が MarginalCost×costPriceLevel×localPriceMultiplier で組む）。
// ⚠限界（計器が自分で申告する）：①登録は月初＝月の途中で作られた POP の書き込みは次の月まで台帳の外（残差と lateRegisteredPops に出る）②並列の国の日次の中で別の国の POP へ払う書き込みは無い前提（flow_s と同じ）
//   ③精算の所得と支払いは州の中の市場の循環を含む（州の外への純の払いは outMarket−inMarket）④国庫の区分は World.Transfer の頭で写した値（運搬の書き込みは区分を読まない）
//   ⑤格・乗数・値段は四半期の末の値（savingsPerCapitaOverNation＝全ての州の 1 人あたりで割る〔事前登録の定義〕・savingsPerCapitaOverMainland＝州の乗数の分母と同じ本土の 1 人あたりで割る〔腕の前の追記〕）⑥国家の市場を通る出入り（stateMarket*）は POP の台帳の外（StateSpent の月の和）⑦鉱脈の品位の器 MineralCostFollowsGrade（既定 0）は自己費用に掛けない。
`````

## §2　形=trail　元の行=31-31　次のメンバー=testateOutFromLivePerQuarter
`````text
// (848) 末尾 8 列＝相続 R1
`````

## §3　形=trail　元の行=32-32　次のメンバー=timportBudgetStartPerQuarter
`````text
// (863) 末尾 4 列＝輸入の払い手（CearnImportHead）
`````

## §4　形=trail　元の行=33-33　次のメンバー=timportOldRuleBudgetStartPerQuarter
`````text
// (868) 末尾 2 列＝旧の規則の予算（旗に依らない）・清算ごとの超過の最大
`````

## §5　形=trail　元の行=34-34　次のメンバー=timportBudgetExcessMaxPerClearingSub
`````text
// (874) 末尾 4 列＝超過の最大（補助は TreasuryCat.Subsidy だけ足す）・払った額を払いの時点の払い手の人数（≦1e-12／1e-12 超〜1e-9／1e-9 超）で分けた和
`````

## §6　形=trail　元の行=35-35　次のメンバー=timportBudgetExcessMaxPerClearingSubLive
`````text
// (880) 末尾 1 列＝Sub と同じ超過の最大を、人数 ≦1e-12 の払い手の 0 でない払いが無かった清算だけで畳む（旗 0 の人の居ない商人の手形の清算を外す）
`````

## §7　形=trail　元の行=36-36　次のメンバー=testateDebtSettledPerQuarter
`````text
// (910) 末尾 2 列＝四半期の末の商人の POP の債務（添字なし＝NaN）・相続の前の返しの段で貯蓄から世界の外へ移った額（台帳・旗を読まない）
`````

## §8　形=trail　元の行=37-37　次のメンバー=testateDebtMovedPerQuarter
`````text
// (920) 末尾 5 列＝手形を振り出せなかった清算の数・その中の塵・相続の段の直後の人の居ない債務者の数・相続の段で返し以外で債務が移った額・人の居る非商人 POP が決済 ① で返した額
`````

## §9　形=trail　元の行=38-38　次のメンバー=tbillWithHeirDebtClearings
`````text
// (963) 末尾 2 列＝清算の頭（決済 ① の直後）の人数 ＞1e-12 の POP の債務の和（清算ごとに足す）・手形を振り出した清算のうち和 ＞1e-9 の数（旗を読まない・読むだけ）
`````

## §10　形=trail　元の行=55-55　次のメンバー=EstMin
`````text
// (848) 相続 R1 の列
`````

## §11　形=trail　元の行=56-56　次のメンバー=ImpKind
`````text
// (863) 輸入の払い手の列（CearnImportHead・CearnImportPaid）
`````

## §12　形=trail　元の行=57-57　次のメンバー=ImpExMax
`````text
// (868) 旧の規則の予算・清算ごとの超過の最大（未定義は -∞）・今の清算の頭の在庫（NaN＝手形）・今の清算の払い・頭があるか
`````

## §13　形=trail　元の行=58-58　次のメンバー=ImpHeadStockSub
`````text
// (874) 補助を Subsidy だけに絞った頭の在庫と超過の最大・払った額の人数の帯別の和
`````

## §14　形=trail　元の行=59-59　次のメンバー=EmptyDebtAfter
`````text
// (920) 手形を振り出せなかった清算の数・その中の塵（0＜債務≦1e-12）・相続の段の直後に人数 ≦1e-12 で債務 ≠0 の POP の数（月ごとの和）
`````

## §15　形=trail　元の行=60-60　次のメンバー=EstSettledSnap
`````text
// (920) 相続の段で人の居ない POP の債務が返し以外で減った額・人の居る非商人 POP が決済 ① で返した額・相続の段の頭の断面（人の居ない POP の債務の和・返した額の累計）
`````

## §16　形=trail　元の行=61-61　次のメンバー=HeirDebtAfterSettleSum
`````text
// (963) 清算の頭の人の居る POP の債務の和（清算ごとに足す）・手形を振り出した清算のうち和 ＞1e-9 の数
`````

## §17　形=trail　元の行=62-62　次のメンバー=EstDebtSettled
`````text
// (910) 相続の前の返しの段で移った額（四半期の和）
`````

## §18　形=trail　元の行=63-63　次のメンバー=ImpExMaxSubLive
`````text
// (880) 人数 ≦1e-12 の払い手の払いが無かった清算だけの超過の最大・今の清算に人数 ≦1e-12 の払い手の 0 でない払いがあったか
`````

## §19　形=trail　元の行=65-65　次のメンバー=G
`````text
// (829) 階級の群の台帳（群の番号＝CgGroupOf）
`````

## §20　形=trail　元の行=70-70　次のメンバー=CearnSink
`````text
// G＝(829) cgrp_s・P＝(836) cpop_s
`````

## §21　形=trail　元の行=113-113　次のメンバー=CearnOn
`````text
// (829) 環境変数の門（POB_CEARN=0）
`````

## §22　形=trail　元の行=116-116　次のメンバー=ga
`````text
// (829) 同じ書き込みを POP の群の台帳にも足す
`````

## §23　形=trail　元の行=118-118　次のメンバー=InWin
`````text
// (829) 月の窓の中の書き込みの和
`````

## §24　形=trail　元の行=126-126　次のメンバー=null
`````text
// (836) POP の四半期の台帳と精算の内訳（同じ月の値）
`````

## §25　形=trail　元の行=141-141　次のメンバー=BilateralMarket
`````text
// (868) 国庫から払い手への入り（清算の頭の後・払いの前）
`````

## §26　形=trail　元の行=144-144　次のメンバー=CearnImportSubsidySubInto
`````text
// (874) 売買方針の補助だけ（輸出の関税の戻し〔TreasuryCat.Tariff・払いの後〕は足さない）
`````

## §27　形=trail　元の行=146-146　次のメンバー=BilateralMarket
`````text
// (863) 輸入の払い（関税・代金・運賃・港湾料・保険料＝清算の払いの段）
`````

## §28　形=trail　元の行=147-147　次のメンバー=EstDebtSettled
`````text
// (910) 相続の前の返しの段の中の貯蓄の出（Bank.Lend の負の側＝世界の外へ・台帳の書き込み＝移った額）
`````

## §29　形=trail　元の行=152-152　次のメンバー=null
`````text
// (836) POP の四半期の台帳（同じ列）と窓の中の税
`````

## §30　形=trail　元の行=155-155　次のメンバー=TryGetValue
`````text
// (920) outOther の経路別（cpop_s の outOtherTop1）
`````

## §31　形=trail　元の行=158-158　次のメンバー=EmptyPopEstate
`````text
// (848) 相続 R1 の列（実際に移った額＝台帳の書き込み）
`````

## §32　形=trail　元の行=179-179　次のメンバー=CearnOn
`````text
// (829) 環境変数の門
`````

## §33　形=trail　元の行=187-187　次のメンバー=cw
`````text
// (829) 月の窓の始まり＝月初の在庫を写す
`````

## §34　形=trail　元の行=189-189　次のメンバー=WinTax
`````text
// (836)
`````

## §35　形=full　元の行=204-209　次のメンバー=_impRegion
`````text
    // ---------------------------------------------------------------- (863) 輸入の払い手の列（cearn_s の末尾 4 列・読むだけ・恒等・事前登録 measurements/m863_20261105_prereg_ip.txt [2][4]） ----------------------------------------------------------------
    // 清算の頭（BilateralMarket の払い手の選びの直後）に州だけを受け取り、払い手と予算の局所変数は受け取らない＝州の状態と予算は計器が自分で読む（旗を読まない）。
    //   importPayerKind＝四半期の最後の清算の頭の州の状態（0 清算の頭なし・1 商人の POP が人数 ＞1e-12・2 商人の POP の人数 ≦1e-12・3 商人の添字なし）。
    //   importBudgetStartPerQuarter＝清算の頭ごとの予算の和（商人が人数 ＞1e-12 なら、債務 ＞0 でその商人の max(0, 貯蓄)・それ以外〔債務 0・NaN〕は ∞／それ以外の州は人数 ＞1e-12 の POP の Σmax(0, 貯蓄)）。
    //   importPaidPerQuarter＝その州の POP が BilateralMarket.cs の移し方で払った額（台帳の書き込み＝実際に移った額）。importCrossRegionPerQuarter＝払いの段の行の州と別の州に登録された POP がその行で払った額。
    // ⚠限界：①登録は月初＝月の途中に作られた POP の払いは数えない（cearn_s の限界①）②州の状態は清算の頭の 1 時点 ③cpop_s の U1 は植民地格の州だけ（cpop_s の母集合）。
`````

## §36　形=doc3　元の行=216-217　次のメンバー=CearnImportHeadInto
`````text
    /// <summary>(868) 頭の本体（試験の口）。ImpOldBudget＝旧の規則の予算（旗に依らない）：商人の POP があればその 1 POP〔債務 ＞0 なら max(0, 貯蓄)・債務が 0 や NaN なら手形＝+∞〕／商人の添字が無い州は全 POP の Σmax(0, 貯蓄)。
    /// 清算ごとの超過：頭で前の清算の〈払った額−頭の在庫〉を畳んでから今の清算を始める（手形〔在庫 ∞〕の清算は畳まない）。旗 0 の kind 2 の州では実際の払い手と計器の在庫が別（旗を読まない限界）。</summary>
`````

## §37　形=trail　元の行=227-227　次のメンバー=heirDebt
`````text
// (963) 決済 ① の直後の実際の残高（清算の頭は ① と同じ繰り返しの中・間に債務を動かす処理が無い）
`````

## §38　形=trail　元の行=229-229　次のメンバー=BillWithHeirDebtClearings
`````text
// (963) 手形を振り出した（BillBlocked の否定・旗を読まない＝ImportPayerLive 1 で人の居ない商人は手形を振り出さない清算も数える限界）
`````

## §39　形=trail　元の行=230-230　次のメンバー=BillBlockedDust
`````text
// (920) 手形を振り出せない（決済の後も商人の債務が正＝:580 の門が偽・旗を読まない）清算の頭・その中の塵
`````

## §40　形=trail　元の行=233-233　次のメンバー=ImpClrDead
`````text
// (880) 今の清算の印を戻す
`````

## §41　形=doc3　元の行=235-235　次のメンバー=CearnImportFoldInto
`````text
    /// <summary>(868) 今の清算の超過〈払った額−頭の在庫〉を最大に畳んで清算を閉じる（頭が無い・手形の清算は何もしない）。</summary>
`````

## §42　形=trail　元の行=243-243　次のメンバー=exSub
`````text
// (874) 補助を Subsidy だけに絞った超過（2 つの在庫は同じ頭から作る＝NaN も同じ清算）
`````

## §43　形=trail　元の行=245-245　次のメンバー=ImpExMaxSubLive
`````text
// (880) 人数 ≦1e-12 の払い手の払いが無かった清算だけ
`````

## §44　形=doc3　元の行=247-247　次のメンバー=CearnImportSubsidyInto
`````text
    /// <summary>(868) 清算の頭の後に国庫から払い手へ入った補助を、清算内の在庫に足す（手形の清算は足さない）。</summary>
`````

## §45　形=doc3　元の行=249-249　次のメンバー=CearnImportSubsidySubInto
`````text
    /// <summary>(874) 清算の頭の後に国庫から払い手へ入った額のうち、売買方針の補助（TreasuryCat.Subsidy）だけを、絞った在庫に足す（手形の清算は足さない）。</summary>
`````

## §46　形=doc3　元の行=251-251　次のメンバー=CearnImportPaidBandInto
`````text
    /// <summary>(874) 払いを払いの時点の払い手の人数で帯に分ける（≦1e-12／1e-12 超〜1e-9／1e-9 超）。試験の口。</summary>
`````

## §47　形=doc3　元の行=255-255　次のメンバー=EstateRepay
`````text
    /// <summary>(910) EmptyPopEstate.Step の返しの段の前後（true＝入る・false＝出る）：その間の貯蓄の出を estateDebtSettledPerQuarter へ畳む文脈の印（旗を読まない）。</summary>
`````

## §48　形=trail　元の行=258-258　次のメンバー=CearnImportPaidInto
`````text
// (868) 今の清算の払い（試験の口）
`````

## §49　形=doc3　元の行=259-259　次のメンバー=CearnImportPaidDeadInto
`````text
    /// <summary>(880) 払いの時点の払い手の人数が ≦1e-12（帯の列と同じ述語）で額が 0 でなければ、今の清算に印を付ける（その清算は importBudgetExcessMaxPerClearingSubLive に畳まない）。試験の口。</summary>
`````

## §50　形=trail　元の行=270-270　次のメンバー=CearnOn
`````text
// (829) 環境変数の門
`````

## §51　形=trail　元の行=372-372　次のメンバー=CearnOn
`````text
// (829) 環境変数の門（POB_CEARN=0 で cearn_s と cgrp_s を書かない）
`````

## §52　形=trail　元の行=398-398　次のメンバー=EstateFold
`````text
// (848) 相続 R1 の末尾 8 列
`````

## §53　形=trail　元の行=400-400　次のメンバー=CearnImportFoldInto
`````text
// (868) 行を書く前に最後の清算を畳む
`````

## §54　形=trail　元の行=401-401　次のメンバー=Add
`````text
// (863)
`````

## §55　形=trail　元の行=402-402　次のメンバー=Add
`````text
// (868)
`````

## §56　形=trail　元の行=403-403　次のメンバー=Add
`````text
// (874)
`````

## §57　形=trail　元の行=404-404　次のメンバー=Add
`````text
// (880)
`````

## §58　形=trail　元の行=405-405　次のメンバー=Add
`````text
// (910)
`````

## §59　形=trail　元の行=406-406　次のメンバー=Add
`````text
// (920)
`````

## §60　形=trail　元の行=407-407　次のメンバー=Add
`````text
// (963)
`````

## §61　形=trail　元の行=409-409　次のメンバー=RegionTier
`````text
// (829) 植民地格の州だけ群の行
`````

## §62　形=full　元の行=421-430　次のメンバー=CgrpHeaderText
`````text
    // ---------------------------------------------------------------- (829) 植民地格の州の階級の群の表 cgrp_s（読むだけ・恒等・事前登録 measurements/m829_20261104_prereg_cg.txt） ----------------------------------------------------------------
    // 目的＝仮説〈定額の請求（人頭税）が払う群の貯蓄を毎月空にする〉を、凍結の州の群ごとの台帳で読む（(822) の次の推奨）。cearn_s と同じ Sav の口で、同じ列へ POP の群の台帳にも足す。
    // 行＝四半期の末に格が Colonial の州×群（0 mass 平民の大衆＝!Closed・!IsSoldier・!IsModernElite〔犯罪者を含む〕・1 modernElite・2 closed〔Closed・!IsSoldier〕・3 soldier〔IsSoldier〕）。
    //   述語は計器の中で旗から書く（エンジンの関数を呼ばない）。人数・在庫・流れ・月の和が全て 0 の群の行は書かない。
    // 月の窓＝月初（Begin＝CearnRegister）に POP ごとに在庫を写し、月の終わり（MonthClose の頭＝Fiscal.Step の徴税の後）に月末の在庫を読む。窓の中の書き込みの和を POP ごとに持つ（閉じ＝monthResidualSum）。
    // 請求は計器が自分で書く式：人頭税＝PollTaxRate×段×Size（段が正・!Closed・!IsSoldier・犯罪者でない）・所得税の上界＝TaxRate×max(0, LastEarnedPerMonth)（身分の免税と課税への同意を掛けない）。
    // 空の印（POP×月）＝請求が正で、月末の在庫 ≦ CgEmptyShare×max(月初の在庫, 請求)。
    // 環境変数の門 POB_CEARN=0 で cearn_s と cgrp_s を書かない（既定＝書く＝基準の表は変わらない）。
    // ⚠限界（計器が自分で申告する）：①登録は月初＝月の途中で作られた POP は次の月まで群の台帳の外（lateRegisteredPops と残差に出る）②月の窓は月初に登録した POP だけ
    //   ③所得税は上界＝人頭税の割合は下界 ④国の段と率は月の終わりの値 ⑤州が月の途中で別の国へ移った月の窓は数えない ⑥月初の在庫は〈月初の予算〉の代わり（EconomyStep が RemainingBudget に写すのは Begin の後）。
`````

## §63　形=doc3　元の行=443-443　次のメンバー=CearnOn
`````text
    /// <summary>環境変数の門：POB_CEARN=0 で cearn_s と cgrp_s と cpop_s〔(836)〕を書かない（試験が切り替える）。</summary>
`````

## §64　形=trail　元の行=444-444　次のメンバー=CearnOn
`````text
// (1061) 3 表とも選ばれないときも切る
`````

## §65　形=doc3　元の行=455-455　次のメンバー=CgPop
`````text
    /// <summary>POP の月の窓（月初の在庫・窓の中の書き込みの和）。(836) POP の四半期の台帳（Q…・cpop_s）と窓の中の税と精算の所得（止まった月の振り分け）も持つ。</summary>
`````

## §66　形=trail　元の行=459-459　次のメンバー=WinEarned
`````text
// (836) 窓の中の outTax と精算の所得
`````

## §67　形=trail　元の行=464-464　次のメンバー=QEstIn
`````text
// (848) 相続 R1 の出と入り
`````

## §68　形=trail　元の行=465-465　次のメンバー=QDebtCarry
`````text
// (920) Pop.Carry で受けた債務の和・outOther の経路別の和（遅延確保）
`````

## §69　形=trail　元の行=488-488　次のメンバー=GetValue
`````text
// (836) POP の四半期の台帳も同じ時点で戻す
`````

## §70　形=full　元の行=517-517　次のメンバー=Begin
`````text
                // (836) POP の四半期の台帳：月の窓・請求・空の印・月初が正で begin≧claim の月と、その中の止まった月の振り分け
`````

## §71　形=full　元の行=530-535　次のメンバー=EstateSav
`````text
    // ---------------------------------------------------------------- (848) 相続 R1 の列（読むだけ・恒等・事前登録 measurements/m848_20261105_prereg_eh.txt §4 G2） ----------------------------------------------------------------
    // cearn_s の末尾 8 列（全ての国の全ての州×四半期）：emptyPopSavings＝四半期の末の断面の人数 ≦1e-12 で在庫が正の POP の在庫の和／estateDue・estateNoHeir＝EmptyPopEstate.Step の頭で計器が自分で数えた
    //   〈その月に移るはずの額〉（旗に依らない・受け手の人数の和 ＞1e-9 の州＝due・それ以外＝noHeir）の四半期の和／estateMoved・estateIn＝移し方の 1 本の呼び手が EmptyPopEstate.cs の
    //   書き込み（払い手の出・受け手の入り）／estateOutFromLive＝人数 ＞1e-12 の払い手の出／estateInToEmpty＝人数 ≦1e-12 の受け手の入り／estateRecvPerCapitaSpread＝月ごとの受けた額÷受け手の人数の
    //   (最大−最小)÷最大の四半期の最大。cpop_s の末尾 2 列＝POP ごとの出と入り（四半期の和）。
    // ⚠限界：①登録は月初＝月の途中で現れた POP の書き込みは台帳の外 ②広がりは受け手の書き込みの時点の人数で割る ③due は旗 0 でも数える（反実仮想）＝旗 1 の腕では前の月の移しの後に入った額。
`````

## §72　形=trail　元の行=572-572　次のメンバー=EstDebtBeforeSnap
`````text
// (920) 相続の段の頭の断面
`````

## §73　形=doc3　元の行=584-584　次のメンバー=EmptyDebtSum
`````text
    /// <summary>(920) 人数 ≦1e-12 の POP の債務の和（符号つき）と、債務 ≠0 の POP の数。</summary>
`````

## §74　形=doc3　元の行=592-593　次のメンバー=EstateAfter
`````text
    /// <summary>(920) 相続の段（<see cref="EmptyPopEstate.Step"/>）の直後：人数 ≦1e-12 で債務 ≠0 の POP の数（月ごとの和）と、段の頭から直後までに人の居ない POP の債務が減った額から返した額（estateDebtSettledPerQuarter の段の中の増分）を引いた残り＝返し以外で債務が移った額。
    /// 旗を読まない・エンジンの関数を呼ばない。今の実装では人の居ない POP の債務を動かすのは返しの段だけ＝estateDebtMovedPerQuarter は常に 0（債務を相続人へ移す案 B1 が入ったときに初めて正になる）。</summary>
`````

## §75　形=doc3　元の行=606-606　次のメンバー=CearnBillSettle
`````text
    /// <summary>(920) 決済 ①（BilateralMarket.cs の清算の頭）で、人数 ＞1e-12 の商人でない POP の債務が実際に減った額（Bank.Lend の前後の差・(927) 以前は返そうとした額＝取り立てが床で 0 を返した決済でも数えていた）。商人 POP と人の居ない POP は数えない。</summary>
`````

## §76　形=doc3　元の行=613-613　次のメンバー=CearnCarryDebt
`````text
    /// <summary>(920) Pop.Carry の 7 引数の形で受け手が受けた債務（受け手の通貨・cpop_s の debtInCarryPerQuarter）。Bank.Carry（銀行の与信の側）は数えない。</summary>
`````

## §77　形=full　元の行=627-637　次のメンバー=CpopHeaderText
`````text
    // ---------------------------------------------------------------- (836) 植民地格の州の POP の行 cpop_s（読むだけ・恒等・作業の事前登録 measurements/m836_20261105/plan.txt） ----------------------------------------------------------------
    // 目的＝〈人数がほぼ 0 の POP に在庫が止まる根〉と〈人の居る POP の所得が 0 の根〉（(829) の反証役の直し）を、群でなく POP の単位で読む。
    // 行＝四半期の末に格が Colonial の州×その州の POP（州の Pops の並び・人数 0 の POP も書く）。cgrp_s と同じ Sav の口で、同じ列へ POP の四半期の台帳にも足す。
    // 列：識別（popIndex＝州の Pops の添字・classId・className・group）・人数・四半期の始まりの在庫（その州の台帳が戻された時点の値・戻される前に現れた POP は nan）・末の在庫・流入 7・流出 9・残差・遅れの印・
    //   月の窓（cgrp_s と同じ定義の POP の値）・精算の内訳（所得・その中の地代〔LandRentPerMonth〕と工業レント〔IndustrialRentPerMonth〕・支払い＝所得−増分・自給の値〔SubsistenceValuePerMonth〕＝精算の時点の月の値の和）・
    //   請求（cgrp_s と同じ式）・月の数え（請求の立つ月・空の印・月初の在庫が正で begin≧claim の月・その中の空の印・その中の止まった月の 6 区分＝CpStuckKind）・
    //   四半期の末の断面（失業率・自給率・主食の充足・その POP が賃金の受け手〔LaborPopIndex〕の行と地代の受け手〔OwnerPopIndex〕の行）・群の和の確かめ（州×群の POP の和−群の台帳）。
    //   賃金の行の採算の率はエンジンの関数を呼ばず、供給の段が書いた DesiredOutput（＝Capacity×採算の率）と Capacity を写す（読み手が割る）。
    // 環境変数の門 POB_CEARN=0 で書かない（cearn_s・cgrp_s と同じ門）。
    // ⚠限界（計器が自分で申告する）：①cgrp_s の限界①〜⑥と同じ ②払った税の種類（人頭税と所得税）は口では分けられない（国庫への移しは 1 本）＝払った額は outTax・種類は請求の式だけ
    //   ③自給の〈量〉は POP ごとの欄が無い＝自給の値と自給率 ④DesiredOutput は供給の段を通った月の値（眠った生産者・倒産の行は古い値）⑤断面は四半期の末の 1 時点。
`````

## §78　形=trail　元の行=648-648　次のメンバー=townerRevenuePerMonth
`````text
// (848) 末尾 2 列＝相続 R1
`````

## §79　形=trail　元の行=649-649　次のメンバー=tdebt
`````text
// (863) 末尾 1 列＝四半期の末の Pop.Debt
`````

## §80　形=trail　元の行=650-650　次のメンバー=toutOtherTop1PerQuarter
`````text
// (920) 末尾 3 列＝outOther の上位の経路・その額・Pop.Carry で受けた債務
`````

## §81　形=trail　元の行=672-672　次のメンバー=cpop_s
`````text
// (1061) 読むだけ（sk 以外に書かない）
`````

## §82　形=trail　元の行=707-707　次のメンバー=LandShares
`````text
// ★(978) 旗 1 かつ長さ 5 は私の 4 枠だけ（LsSlots・売上の重みは ls[k] のまま）。★(895) 土地の持ち分：割合が正の枠の受け手のうち担い手でない者に行を 1（同じ受け手は 1 回）・売上×その受け手の割合の和（null は今の持ち主の 1 行）
`````

## §83　形=trail　元の行=747-747　次のメンバー=Add
`````text
// (848)
`````

## §84　形=trail　元の行=748-748　次のメンバー=Add
`````text
// (863)
`````

## §85　形=trail　元の行=750-750　次のメンバー=Add
`````text
// (920)
`````

## §86　形=full　元の行=786-786　次のメンバー=started
`````text
            // (842) 州の台帳が Started でない四半期（四半期の途中で州が足されて月初の登録の前）は、始まりの在庫 g.Start が既定の 0 のまま＝残差が在庫の全額に見える。cearn_s と同じに始まりと残差は nan で名乗る（行の数は変えない）。
`````

## §87　形=trail　元の行=798-798　次のメンバー=CpopRows
`````text
// (836) 同じ州の POP の行
`````
