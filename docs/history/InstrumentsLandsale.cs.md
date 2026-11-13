# InstrumentsLandsale.cs の注の履歴

由来：1cd66aae5 の InstrumentsLandsale.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLandsale.cs#番号〕だけが残る。

## §1　形=full　元の行=4-23　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (983) 土地の持ち分の段 B-1 の段 0＝恒等の計器 landsale_s（読むだけ・恒等・事前登録 measurements/m983_20261109_prereg_ls.txt） -------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsLandshare.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（INS のときだけの World ごとの積み＝ConditionalWeakTable）。
// 目的＝土地税が払えない分（POP の貯蓄が請求に足りない月の未払い）が、売り（B-1）の動機として十分な大きさか（no-go）と、土地税の写しがエンジンの取れた額と向きを違えないか（恒等）を、段 1 の器を入れる前に測る。
// 受け口＝Fiscal.Step の徴税の走査の直前（徴税の前の貯蓄・前の月の所得・州の届き Rule／AdminFill を計器が自分で読む）＝LandsaleStep。月の閉じ＝Instruments.MonthClose（LandshareMonth の前＝輸出の地代の月の積みが 0 へ戻る前）＝LandsaleMonth。
// 計器が自分で組む物（エンジンの合成を呼ばない）：届き（RuleDelivery の写し）・仲介者の取り分（TaxScan.Of の写し）・4 種の請求（AssessPop の合成の写し）・払える割合（PayRate の写し）・十分の一税の請求（TitheClaim の写し）・土地税の基数（LandTaxBase の写し）。
//   読むだけの純関数／状態として呼ぶ物：Privileges.TaxExemptionDegree（免税の度合い）・InterestGroups.FranchiseVoice／SuffrageInclusion（同意の声）・Privileges.TitheDegree・Politics.IsMassPop・PowerAffinity.Shares（王権の取り分）。
//   既存の写しの再利用：LsRecipient（枠の受け手の添字・寄せの回数）・LsClass（階級の 5 区分）・LsAbsenteeRgo（不在地主の RGO）・LsAccs（輸出の地代の月の積み MExp）。
// 売りの期待（段 1 が実装したら起きるはずの量＝計器が自分で数える）：売り手＝rp＜1 の請求があり土地税の未払い（kLand×届き×(1−rp)）が正の POP。買い手＝売り手の持ち分 h ごとに、枠 k＜4 の受け手が実在し（寄せの枠を外す）売り手と別の POP である枠。
//   値段の地代＝売り手の LandRent−土地税率×基数×（買い手の貯蓄で重み付けた免税の係数）の正の部。年に手放す割合の期待 saleExpectedSharePerYear＝Σ 未払い×国債金利÷Σ 純の地代（四半期の和の比）。
// ⚠限界（計器が自己申告する）：
//   ①rp は走査の開始時点の貯蓄で数える＝走査の途中で前の POP の仲介者の取り分が受け手（領主）の貯蓄に入る＝計器の rp は下界・未払いは上界（Fiscal.cs の (490) の注と同じ型）。
//   ②値段の地代は国内の地代（LandRent）だけ＝輸出の地代は入れない（課税の基数と同じ集合・分子と分母が同じ集合。輸出の地代の月の和は exportRentPerQuarter に記録だけ）＝輸出作物の産地の持ち分は期待の上で安く見積もられない側（純の地代が小さい側＝割合は上界）。
//   ③買い手の税の係数は買い手の貯蓄の加重平均を h をまたいで 1 つに畳む（h ごとの max(0,地代−税) の和でなく和の max）＝割合は下界側にずれうる。
//   ④欄が null の RGO は [1,0,0,0] として売り手の持ち分に数える（段 1 の決め事は構築の除外を守るか段 1 で決める＝低⑨）。不在地主の RGO は持ち分から外す。
//   ⑤純の地代が 0 以下（!(P>0)）か国債金利が正でない・買い手の貯蓄が 0 の売り手は期待に入れず saleBlocked* に別に積む（未払いが消える端＝低⑪）。
//   ⑥法の門（農奴解放 ≥ 限定的）は期待に掛けず saleLawOpenFlag（四半期の最後の月の値）に分けて記録。
//   ⑦助走の月（World.WarmupFrozen）・Fiscal が無効の月は数えない。
//   ⑧★(995) 公売の払いの写し（LandsaleSaleBegin）は買い手ごとの額の和にエンジンの床を入れる（保有が 1e-9 以下・額が床〔TakeFloorPositive＞0 なら 0・でなければ 1e-12〕以下は 0）。エンジンの関数は呼ばない。
//     写しの重み〔Closed && !IsSoldier ? 1−免税の度合い : 1〕は Privileges.TaxAssessment（同じ式）・写しの基数は Fiscal.LandTaxBase（同じ分岐）と今の盤面で同じ値（紙の確かめ＝事前登録 m995 [1]）。
//     既知の限界：免税の度合いは月の走査の頭の取り（走査の途中で入力が変わる盤面では不一致）・特権の免税（PrivilegeTaxDiscount＞0）や φ*≠0/1 の盤面は同値の外に出うる・買い手の列挙は SellOne と別実装。
`````

## §2　形=trail　元の行=36-36　次のメンバー=tsaleBuyerNonInvestorCount
`````text
// ★(991) 末尾の 11 列＝実際の公売（LandsaleSaleBegin／End）
`````

## §3　形=full　元の行=110-110　次のメンバー=Owed
`````text
        // ★(991) 実際の公売（受け口 LandsaleSaleBegin／End）：月の未払いの写し（売り手→V）・免税の度合い・Begin の写し・四半期の和
`````

## §4　形=trail　元の行=129-129　次のメンバー=Clear
`````text
// ★(991)
`````

## §5　形=trail　元の行=134-134　次のメンバー=ExemptDeg
`````text
// ★(991) 公売の写しの買い手の免税
`````

## §6　形=trail　元の行=182-182　次のメンバー=unpaid
`````text
// ★(991) 公売の写しの V（徴税の前の貯蓄で数えた未払い）
`````

## §7　形=trail　元の行=259-259　次のメンバー=QSaleActual
`````text
// ★(991)
`````

## §8　形=doc3　元の行=263-263　次のメンバー=LdHeldSnapshot
`````text
    /// <summary>★(991) 州 <paramref name="r"/> の RGO（不在地主を外す）の私の枠の和（null＝1）を <paramref name="slot"/> へ、POP ごとの持ち分（枠の割合の和・寄せは持ち主）を戻り値へ。</summary>
`````

## §9　形=doc3　元の行=280-280　次のメンバー=LandsaleSaleBegin
`````text
    /// <summary>★(991) 受け口（LandSale.Resolve の売り手ごとの最初・On の確かめは中）：計器が自分で公売の値段と払いを組み（売り手の V は LandsaleStep の写し・買い手は寄せの枠を外す）、持ち分を写す。</summary>
`````

## §10　形=full　元の行=297-297　次のメンバー=floor
`````text
        // ★(995) 払いの予言＝買い手ごとの額の和にエンジンの床を入れる（Pop.Take は保有が 1e-9 以下か額が床以下なら 0 を返す）。ΣS・φ・値段は塵込みのまま（SellOne と同じ）。床は払い手の国の係数（World.Transfer が from.W.Coef を渡す）。
`````

## §11　形=doc3　元の行=312-312　次のメンバー=LandsaleSaleEnd
`````text
    /// <summary>★(991) 受け口（売り手ごとの最後）：エンジンが実際に移した額 <paramref name="moved"/> を写しと比べ、持ち分の動き（枠の和・売り手の手放した割合・買い手）を数える。</summary>
`````

## §12　形=doc3　元の行=334-334　次のメンバー=LandsaleTestOwe
`````text
    /// <summary>★(995) 試験の窓（LdAcc は private）：売り手 <paramref name="p"/> の未払い V と免税の度合いを置く（LandsaleStep の代わり）。On のときだけ。</summary>
`````

## §13　形=doc3　元の行=342-342　次のメンバー=LandsaleTestRead
`````text
    /// <summary>★(995) 試験の窓：四半期の積みの実際の払い・写しの払い・最大の相対差。</summary>
`````
