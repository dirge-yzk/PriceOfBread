# InstrumentsLandshare.cs の注の履歴

由来：1cd66aae5 の InstrumentsLandshare.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLandshare.cs#番号〕だけが残る。

## §1　形=full　元の行=4-21　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (895) 土地の持ち分の段 B-0 の恒等の計器 landshare_s（読むだけ・恒等・事前登録 measurements/m895_20261106_prereg_ls.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCornint.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝土地の持ち分（Producer.LandShares・NationConfig.LandOwnership）の器が地代を持ち分どおりに受け手へ渡したかを、期待（計器が自分で掛けた値）と実際（POP の LandRentPerMonth）で比べる＋B-2a の予言の材料（非賦存の地代を今の持ち主の階級で分けた和）。
// 行＝国×四半期（全ての国）。額は国の通貨・四半期の月の和（…PerQuarter）。助走の月（World.WarmupFrozen）は数えない。階級の 5 区分＝Noble（PopClass.Noble）・Clergy（Clergy）・Peasant（Peasant）・Capitalist（Capitalist）・Other（その他）。
// エンジンの関数を呼ばない：受け手の枠（0 貴族＝持ち主・1 教会＝州の聖職者・2 農民＝担い手・3 市民＝州の投資家・範囲の外は持ち主へ寄せる）・賦存の判別（州の Resources に載る財）・不在地主の判別・データの正規化・掛け算を自分で書く
//   （EconomyStep.LandShareRecipient・PayOwnerRent・Colonial.AbsenteeRgo・Simulation.LandOwnershipShares と同じ規則の写し＝片方の誤りをもう片方で拾う）。エンジンの局所から受けるのは受け口の地代の総額 rent だけ（SbRgo の先例）。
// 受け口：EconomyStep の④（国営持分と赤字の枝の後・割る前の総額）＝LandshareRgo・InternationalMarket.PaySales（国営持分を引いた後の正の rent）＝LandshareExp。月の閉じ＝Instruments.MonthClose の頭＝LandshareMonth。
// 期待（expectedByClass）＝非植民地格の州の全ての RGO の rent>0×欄の重み（null＝持ち主に 1）→受け手の POP の ClassId。
//   ＋送金の項（事前登録 [3]・反証役の訂正 1 の (a)）：植民地格の州の輸出作物の大農園の地代は月末に首都の投資家の LandRent へ移る（Colonial.RemitAbsenteeRent）＝計器は植民地格の州の POP ごとに
//   その月の LandRent の積み pre（受け口の rent>0 を同じ受け手で足した値）を数え、月の閉じで残った LandRent post との差 max(0, pre−post) を送った額として首都の投資家の ClassId へ足す（期待と data の両方）。
// data（dataByClass）＝同じ集合で、欄の代わりに world.json の側（WorldData.Nations() の同じ名の国の LandOwnership を計器が自分で正規化）を、非賦存×持ち主が貴族の RGO にだけ掛けた期待（他とデータの無い国は持ち主に 1）。
// 実際（landRentActual）＝月の閉じの非植民地格の州の POP の LandRentPerMonth（階級ごと）。
// ⚠限界（計器が自己申告する）：①実際の側は国内だけ（輸出の地代は LandRent に載らない＝expRent・expSlotExpected は門にしない）②植民地格の州は外し送金だけを足す③rent≦0 は LandRent が立たない＝期待も rent>0 の分だけ
//   ④期待も実際も受け手の POP の ClassId で積む⑤昇格した植民地は Integrated＝本国の側・欄は null＝data は欄を持つ州だけの列か最初の四半期で読む⑥rentNonEndowByOwnerClass は受け口の時点の持ち主の階級
//   ⑦data は国の名で world.json の国を引く＝建国の国・名の変わった国はデータ無し⑧割譲で州が月の途中に動けば旧の国の受け口と新の国の LandRent が分かれる（外交は四半期の境＝見込み 0）⑨slotFallbackCount は計器が自分で引いた受け手の寄せの回数
//   ⑪（★(978)）鉱山の列：植民地格の州の鉱物は国内の列に入らない・造幣と輸出を分けない・受け口の rent は国営持分の後・国の和だけ。国家の枠の列：期待は data（world.json の 5 個目 c）×非賦存×持ち主が貴族×非植民地格・国有化の州（NationalizedMask≠0）は外して件数だけ
//   ＝後から立つ RGO（欄が無い）は期待に入って実際 0＝門は q0〜q11 で読む。basisx_s（InstrumentsBasisx）と landbase_s（InstrumentsLandbase）の枠の周回は長さで回す＝旗 1 の 5 枠では国家の枠を持ち主へ寄せて数える（持ち主の範囲の外・記録）。
//   ⑩持ち分に合わせた他の計器＝cornint_s（InstrumentsCornint の所有の値・按分・輸出の層）と cearn_s（InstrumentsCearn の地代の受け手の行）。sbr_s の自営の数（InstrumentsSb.SbRgo の owner==labor）は今のまま＝欄を持つ RGO の農民の枠は数えない。
`````

## §2　形=doc3　元の行=39-39　次のメンバー=LsNormalize
`````text
    /// <summary>データの正規化（計器の写し・純関数）：長さ 4 か 5（★(978) 5 個目＝国家の枠）・全て有限で 0 以上・和が正なら和で割った同じ長さの写し（長さ 5 で 5 個目が 1 以上なら null）、そうでなければ null。</summary>
`````

## §3　形=doc3　元の行=51-51　次のメンバー=LsSlots
`````text
    /// <summary>★(978) 枠の周回の数と重み（計器の写し）：旗 LandShareCrownSlot＞0 かつ長さ 5 なら私の 4 枠を ls[k]÷(1−ls[4])・それ以外は全ての枠を ls[k]（5 個目は受け手の寄せで持ち主へ＝旗 0 のエンジンと同じ）。</summary>
`````

## §4　形=doc3　元の行=55-55　次のメンバー=LsMineralIds
`````text
    /// <summary>★(978) 鉱物の財（計器が自分で持つ 14 個の表＝Catalog.IsMineral を呼ばない・番人 LandShareCrownTests.T_M1 が突き合わせる）。</summary>
`````

## §5　形=doc3　元の行=59-59　次のメンバー=LsBuiltShare
`````text
    /// <summary>★(978) 計器が自分で組む建てた容量の持分（World.StateProfitShare を呼ばない・国有化の下限は写さない＝その州は門の母集合から外す）。</summary>
`````

## §6　形=full　元の行=114-114　次のメンバー=MaxSlotTierRule
`````text
        // (904) 末尾の 3 列の積み：送金が期待へ入らなかった月（首都が植民地格・首都の投資家の添字が範囲の外）の額・首都が植民地格だった月数・その規則（Tier≠Colonial の月だけ送金を期待へ足す）での月ごとの相対差の最大
`````

## §7　形=full　元の行=116-116　次のメンバー=QExpNeNobleMin
`````text
        // ★(978) 鉱山の地代の列（四半期の和・助走の月は積まない）
`````

## §8　形=full　元の行=119-119　次のメンバー=MCrownXAct
`````text
        // ★(978) 国家の枠（crown）の列：月の和（月の閉じで国の相対差の最大を取り四半期へ足す）・四半期の和・件数
`````

## §9　形=full　元の行=141-141　次のメンバー=WarmupFrozen
`````text
        // ★(978) 国家の枠の積み（rent≦0 の赤字の月も数える＝下の rent>0 の返りより前）：stA＝国営工場の区分の台帳が受け口の前後で国庫へ動いた額（移した直後の値・⑮）
`````

## §10　形=trail　元の行=180-180　次のメンバー=WarmupFrozen
`````text
// ★(978) 鉱山の地代の列（受け口の時点の持ち主の階級で分ける）
`````

## §11　形=doc3　元の行=195-195　次のメンバー=LandshareExp
`````text
    /// <summary>受け口（InternationalMarket.PaySales・INS のときだけ）：国営持分を引いた後の正の輸出の地代 <paramref name="rent"/>。★(978) <paramref name="sc"/>＝同じ代金の国営持分（エンジンの局所＝写しの門 G-S2 の輸出の列だけが読む）。</summary>
`````

## §12　形=trail　元の行=201-201　次のメンバー=WarmupFrozen
`````text
// ★(978) 鉱山の列と国家の枠の輸出の写し（非植民地格・非賦存・持ち主が貴族＝data の側と同じ判別）
`````

## §13　形=trail　元の行=249-249　次のメンバー=expTier
`````text
// (904) 送金を足す前の期待＝下の Tier≠Colonial の規則の期待の元
`````

## §14　形=trail　元の行=256-256　次のメンバー=capIn
`````text
// (904) LsAbsenteeOwner と同じ範囲の確かめ（範囲の外は足さず末尾の列へ）
`````

## §15　形=full　元の行=272-272　次のメンバー=CrRel
`````text
            // ★(978) 国家の枠：月ごとの国の |Σ実際−Σ期待|÷Σ期待（期待 0 で実際も 0 なら 0・期待 0 で実際が 0 でなければ +∞）の四半期の最大
`````

## §16　形=full　元の行=340-340　次のメンバー=Add
`````text
            // (904) 末尾に足した 3 列（既にある列の値は変えない）：首都が植民地格だった月数・送金が期待へ入らなかった額・Tier≠Colonial の月だけ送金を期待へ足す規則での相対差の最大
`````

## §17　形=full　元の行=342-342　次のメンバー=Add
`````text
            // ★(978) 末尾に足した列（既にある列の値は変えない）：鉱山の地代の 10 列（§2-1）と国家の枠の 10 列（§2-2・事前登録 measurements/m978_20261109_prereg_cs.txt）
`````
