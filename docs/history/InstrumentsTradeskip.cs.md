# InstrumentsTradeskip.cs の注の履歴

由来：1cd66aae5 の InstrumentsTradeskip.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsTradeskip.cs#番号〕だけが残る。

## §1　形=full　元の行=5-21　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (1003) 貿易の計器 C2 tradeskip_s（成約 0 の理由・読むだけ・恒等・事前登録 measurements/m1003_20261110_prereg_tg.txt）
// 部分クラス。力学の値を 1 つも書かない・乱数を引かない・距離の表（KmOf・PairRisk）を呼ばない（表に副作用を作らない）。
// 読む清算＝調べる年（NgEveryYears の倍数と 1913）の第 4 四半期の**最初の清算**（全ての行が走る清算）。C1（nationgood_s）は四半期の最後の清算を読む＝別の清算。
// 行＝年×買い手の国×財（＋国＝ALL の財ごとの行と合計の行）。買い手の行（BilateralMarket の buyers の 1 行＝CostLedger.ClearingRows に数える行）ごとにちょうど 1 つの結果：
//   rowIdle（番の標本の飛ばし）・rowNoNeed（需要なし＝xNeed≦0・参照価格≦1e-9・需要 0）・rowBudget0（予算 0）・rowScan（走査に入った行）。
//   走査に入った行は rowDealtCross（越境の成約あり）・rowDealtDomOnly（国内の成約だけ）・rowNoDeal に分ける。
//   走査に入った行ごとに、その財の売り手の表の越境の売り手を最初に当たった門で 1 つに数える：xSoldOut（残り 0）・xCap（輸送の容量）・xWedge（楔で飛ばす）・xPass（楔を通った）。
//   xWedge は互いに排他の 4 区分：xWedgeA 関税を抜けば通る・xWedgeB A でなく運賃を抜けば通る・xWedgeC A でも B でもないが両方抜けば通る・xWedgeD 両方抜いても通らない。
//     式＝selP×(baseMul＋wS2)≧lp（現物）・A＝selP×(baseMul＋wS2−duty)＜lp・B＝selP×(1＋wS2)＜lp・C＝selP×(1＋wS2−duty)＜lp。距離・危険料は引かない。
//   ratioBin0〜9＝越境の売り手の selP÷lp（金建て）の度数（境 0.5・0.8・0.9・1.0・1.1・1.25・1.5・2・4）。domPairs＝国内の売り手の数。
//   売り手側（財ごとに 1 回・売り手の国の行）：sellerUnlinked（連結なし）・sellerNoStock（lp≦1e-9 か在庫≦1e-9）・sellerOtherGate（それ以外の門で売り手の表に載らない）・sellerListed。
//   国＝ALL・財＝名前の行：goodNoSellers（その財に売り手が 0 人＝ClearingRows に数えない）。国＝ALL・財＝ALL の行：rowsLedger＝この清算の Cost[ClearingRows] の増分（エンジンの台帳）。
// ⚠限界（計器が自己申告する）：呼び口の引数でエンジンの局所（lp・capLeft・left・売り手の表・baseMul・wS2・duty）を受け取る（ScopeSkip・ScopeDeal の先例）＝再導出ではない。
//   数え上げ（行の結果の排他・和・台帳との突き合わせ）は計器のコード。着値（距離）の判定はしない＝xPass は距離を含めた着値が lp 未満かを見ていない。
//   売り手の国の次元はこの表の行に持たない。国内の売り手は xPairs に数えない。
// (1073) 売り手の国の次元は別の表 tskpair_s（買い手の国×売り手の国×財・年 1870 と 1913・和は上の行と一致）に持つ。置き換えの影 tskshadow_s（売り手の州の級別・今の規則と案 B′ の両方・
//   着値は距離・危険料を含まない＝KmOf・PairRisk を呼ばない）も同じ年・同じ呼び口から書く。計器の RoRead（InstrumentsRecipeout.cs）を再利用し、エンジンの関数は呼ばない。
`````

## §2　形=trail　元の行=30-30　次のメンバー=tgdIterBudget
`````text
// (1045) C4d・ガード終わりの内訳（末尾に足す・旧の列は同じ順・同じバイト）   // (1008) 末尾の列（旧の列は同じ順・同じバイト）・(1036) C4a：売り手の〈その他の門〉の分け（在庫≦目標・sellAcc 0・軍需・その他＝和は sellerOtherGate）・C4b：xPass の組の行の終わりの理由（組の数で重みづけ＝和は xPass）
`````

## §3　形=trail　元の行=33-33　次のメンバー=DStride
`````text
// (1045) 走査した行の実数の和の幅（旧の 3＋C4d の 3）   // (1008) ALL の行を同じ幅で埋める数
`````

## §4　形=trail　元の行=37-37　次のメンバー=RpCtrlDiff
`````text
// (1045) 末尾の 13 列
`````

## §5　形=doc3　元の行=39-39　次のメンバー=TsExitProbe
`````text
    /// <summary>(1036) 試験の呼び口：買い手が満ちた行の終わり（X≦0）で、その行の xPrice・xNeed と計器が付けた理由を渡す（null＝何もしない）。</summary>
`````

## §6　形=doc3　元の行=41-41　次のメンバー=TsPairAllYears
`````text
    /// <summary>(1073) 試験の呼び口：国の組の内訳と置き換えの影を全ての調べる年で取る（本番は 1870 と 1913 だけ）。</summary>
`````

## §7　形=trail　元の行=50-50　次のメンバー=D
`````text
// (1008) D＝国×財×{duty・関税の分・穀物法の分}の和（走査した行）・国の値は清算の頭で蓄える
`````

## §8　形=full　元の行=54-54　次のメンバー=PairOn
`````text
        // (1073) 国の組の門の内訳（tskpair_s）と置き換えの影（tskshadow_s）：年が 1870 と 1913 の清算だけ蓄える（PairOn）。
`````

## §9　形=full　元の行=100-101　次のメンバー=access
`````text
                    // (1045) C4a の順をエンジンの門の順へ（BilateralMarket.cs の売り手の表の構築＝出し手の係数 sellAcc≦1e-12〔州の access → 軍需 → 鎖国・アヘン〕→ 在庫−目標≦1e-12）。
                    //   計器は鎖国とアヘンの係数を再導出しない（エンジンの関数を呼ばない）＝その 2 つで落ちた州は、在庫−目標≦1e-12 でもあれば OgStock、無ければ OgOther に数える（宣言した限界）。
`````

## §10　形=doc3　元の行=119-119　次のメンバー=Exit
`````text
        /// <summary>(1036) C4b：走査した行の終わりの理由。その行の xPass の組の数だけ該当の列へ足す（組ごとの追跡ではなく行の終わりの理由を組の数で重みづけ）。</summary>
`````

## §11　形=trail　元の行=122-122　次のメンバー=scanned
`````text
// (1045) ガードで終わった走査の行の反復の内訳（xPass が 0 の行も数える＝行の数 gdRows）
`````

## §12　形=full　元の行=131-131　次のメンバー=DStride
`````text
        // (1045) C4d・ガード終わりの内訳：三つの呼び口（計器は Market の欄と売り手の表 sPrice を自分で読む・エンジンの関数を呼ばない）。
`````

## §13　形=trail　元の行=189-189　次のメンバー=protect
`````text
// (1008) 関税の分＝エンジンの protect の写し（tar の局所から）・合成の盤面だけ差で出す
`````

## §14　形=trail　元の行=190-190　次のメンバー=GdLen
`````text
// (1045) 行ごとの反復の内訳を空にする
`````

## §15　形=doc3　元の行=226-229　次のメンバー=Shadow
`````text
        /// <summary>(1073) 置き換えの影：容量・売り切れを通った越境の組（楔で飛ばす組と通る組）ごとに、売り手の州の級（E/N/S/F/X/Z/U/-）別に、
        /// 着値÷買い手の自己費用＋投入費を今の規則と案 B′ の両方で数える。着値＝selP×(baseMul＋楔)＝**距離・危険料は含まない**（KmOf・PairRisk を呼ばない＝BilateralMarket に呼び口を足さない・完全な着値は波103 へ）。
        /// 案 B′ の売り手の値段＝selP×（売り手の最安の生産者の B′ 費用÷今の費用）・買い手の lp も同じ比で動かす（値段が費用に従う近似＝計器の読み・確証なし）。
        /// 不足の額＝max(0, 目標＋需要−生産−在庫)×lp（行ごとに級ごとに 1 回）。summary>/summary>
`````

## §16　形=trail　元の行=274-274　次のメンバー=DStride
`````text
// (1045) 3 → DStride（先頭の 3 つは旧のまま・後ろ 3 つは C4d の和）
`````

## §17　形=trail　元の行=275-275　次のメンバー=TarEff
`````text
// (1008) 国の値は清算の頭の写し（tar・exRate はエンジンの局所・他は World から計器が自分で写す）
`````

## §18　形=trail　元の行=283-283　次のメンバー=PairOn
`````text
// (1073) 国の組の内訳と置き換えの影は 2 年だけ（行が多い）
`````

## §19　形=trail　元の行=297-297　次のメンバー=FR
`````text
// (1008) 往復の桁
`````

## §20　形=trail　元の行=317-317　次のメンバー=ns
`````text
// (1008) 末尾の列：走査した行の平均（走査 0 の行は 0）・小数は往復の桁（門の 1e-12 のため）
`````

## §21　形=trail　元の行=322-322　次のメンバー=OgStock
`````text
// (1036) C4a・C4b・(1045) C4d の整数の列
`````

## §22　形=trail　元の行=323-323　次のメンバー=DStride
`````text
// (1045) C4d の和（ubD・rpSumXOld・rpSumXBb）
`````

## §23　形=doc3　元の行=338-338　次のメンバー=TradeskipPairTables
`````text
    /// <summary>(1073) tskpair_s（買い手の国×売り手の国×財の門の内訳・和は tradeskip_s の行と一致）と tskshadow_s（置き換えの影）。年が 1870 と 1913 の清算だけ。</summary>
`````
