# InstrumentsLandbase.cs の注の履歴

由来：1cd66aae5 の InstrumentsLandbase.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLandbase.cs#番号〕だけが残る。

## §1　形=full　元の行=4-18　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (894) 土地税の算定の基準 A1 の段 A-0 の恒等の計器 landbase_s・landbasereg_s（読むだけ・恒等・事前登録 measurements/m894_20261106_prereg_lb.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsSubst.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（出力の口と遅れの列の履歴は ConditionalWeakTable だけ＝セーブに載らない）。INS=1 のときだけ動く（Instruments.Quarter から呼ばれる）。
// 目的＝段 A-2（算定の基準の 2 値・定額の凍結・F_p）の材料：今の土地税の請求（Fiscal.cs:803〜:804 の独立の写し）・実際に取れた土地税・自給へ広げた請求の上界（J-a・J-b）・基数の遅れの比（凍結したときの重さ）・担い手の請求′と手持ちの差（F_p の母集合）を同じ盤面で数える。
// エンジンの関数を呼ばない（AssessPop・ForecastTax・RentShareOf・Privileges.TaxAssessment・Privileges.TitheDegree・Colonial.AbsenteeRgo を呼ばず、式を計器が自分で書く）。エンジンの状態は欄として読むだけ。
// 月の中の順（Simulation.cs）：Government.Step（:1789）→ EconomyStep.Run（:2429・EconomyStep.cs:806 で地代を 0 へ戻し :2747 で積み :2854 で Colonial.RemitAbsenteeRent が不在地主へ移す）→ Fiscal.Step（:2473）。
//   四半期の末（3 か月目の Fiscal.Step の後）の LandRentPerMonth はその月に課税された基数そのもの・LastTaxLandPerMonth（Fiscal.cs:121）はその月に実際に取れた土地税。
// R（国の基数）＝州→POP の Σ max(0, LandRentPerMonth)（犯罪者を含む・州 0 の国は行を書かない）＝cornint_s の rentDomTotalPerMonth と同じ集合（未帰属 rentDomUnattr はその部分集合＝足さない）。
//   baseRCheckRelDiff・baseRCheckBreakFlag＝同じ R を 2 通り（1 本の和・cornint_s と同じく 3 つのブロックに分けて足してから 3 本を足す）に double で数えた相対差と 1e-12 の旗（印字の桁の床で門を置かない）。
// 法の読み手の一覧（訂正 (6)・計器は写していない）：Fiscal.cs:85（Step が w.LandTax を AssessPop へ渡す）・Fiscal.cs:90（特権台帳 Privileges.Record・旗 PrivilegeLedgerEnabled＝0 で今は動かない）。
// ⚠限界（計器が自己申告する）：①四半期の末の月の値（3 か月目）だけ②輸出の地代（InternationalMarket.PaySales）は基数に入らない（cornint_s の rentExpPerMonth を読む）
//   ③請求′は所得税に課税への同意を掛けない上界・累進課税なし・仲介者の取り分なし・当月の土地税なし・月の請求′を四半期の末の貯蓄と比べる④PrivilegeFromCrownShare＞0 の木では免税の度合いが nan
//   ⑤自給へ広げた請求は LastSubsistenceOutput（σ を 1 で切った後の量×収穫係数）を使う＝作った量の余り E_p は入らない
//   ⑥遅れの列は計器が見た四半期だけで数える（建国・割譲で母集合が変わった四半期は比が跳ぶ＝nan にしない＝読み手が割譲の表と突き合わせる・州の履歴は Region を鍵にする＝割譲で持ち主が変わっても州に付いて行く）
//   ⑦州の基数は Colonial.RemitAbsenteeRent の後（訂正 (2)＝植民地格の州の輸出作物の大農園の地代は首都の州の投資家の POP に載る・国の R は変わらない）⑧法の読み手のうち Fiscal.cs:85・:90 は写していない。
//   ⑨(904) 計器の行は QuarterEnd（Simulation.cs:1896）の後に書く＝QuarterEnd の Politics.Step（:1908）・Meritocracy.Step（:1912）・割譲（外交は四半期の境）は課税（3 か月目の Fiscal.Step）の後で計器の前に走る＝四半期の末の LandRentPerMonth・州の POP は課税の時点とは別の盤面の物（社会移動・粛清・割譲で POP と持ち主が動きうる）。
`````

## §2　形=trail　元の行=200-200　次のメンバー=WriteLine
`````text
// (937) landfix_s（landbase_s と同じ行の集合）
`````

## §3　形=full　元の行=204-217　次のメンバー=LandfixHeaderText
`````text
    // ---------------------------------------------------------------- (937) c82lt 段 A-0′（限界 ⑨ の (i)＝課税の時点の写し）と段 A-2（定額の基数）の列＝表 landfix_s（国×四半期・landbase_s と同じ行の集合・読むだけ・事前登録 measurements/m937_20261107_prereg_lt.txt） ----------------------------------------------------------------
    // A-0′：月の決算（MonthClose・Fiscal.Step の後で QuarterEnd の前）の四半期の締め月に、国ごとに土地税の請求・免税・R・d′・取れた÷請求を写し、四半期の行の新しい列に書く（既にある列の値は変えない）。
    //   請求の写しは Fiscal.cs の AssessPop の土地税の分岐（landRate＞1e-9 かつ 基数＞0）を計器が自分で書く。基数＝旗 LandTaxFixedBasis＞0 なら地代＋差分（エンジンの欄を読むだけ）・0 なら地代。
    //   landClaimPropAtTaxPerMonth＝同じ盤面で旧の規則（基数＝地代）の請求＝新÷旧の分母。
    // A-2：計器は自分の名簿（WorldData.Nations() の同じ名の国の LandTaxBasis＝"Fixed"）・自分の凍結の規則（助走〔WarmupFrozen〕の月ごとに正の地代で写しを上書き・正でなければ写しなし・助走の後は上書きしない）・
    //   自分の不在地主の写し（LsAbsenteeRgo）・自分の割り方（LsRecipient）で凍結の写しと POP ごとの期待の差分を組み、エンジンの差分（Pop.LandTaxBasisAdjPerMonth）・凍結の欄（Producer.FrozenLandRentPerMonth）と比べる。
    //   旗 0 の盤面でも計器の写しは組む＝lbExpBaseRAtTaxPerMonth÷baseRAtTaxPerMonth が同じ盤面の B（定額の基数÷収穫比例の基数）。
    // ⚠限界（計器が自己申告する）：⑩期待の差分は EconomyStep の④の地代（PayOwnerRent の入力）だけから組む＝PayOwnerRent の入力そのものは計器の外（エンジンの局所の値）
    //   ⑪凍結は助走の月にしか立たない＝助走の無い木（StartWarmupQuarters＝0・単国の試験）は写しも凍結も無い（G7 の事象 0）
    //   ⑫名簿は国の名（NameOf）で引く＝建国の国が親の名を BaseName に持つと計器は名簿の国と見なす（助走の後に生まれた国は写しを持たない）
    //   ⑬G8b（凍結の不変）は四半期の締め月だけ数える・移転で他の国へ出た RGO の写しは数えない（移った先の国の行で〈写しの国≠今の国〉としてエンジンの欄が null かを数える）
    //   ⑭G7 は助走の月の照合（凍結＝当月の地代＝差分 0）と、助走の後の最初の月の締めの〈エンジンの凍結の数＞0〉＝事象。助走の最後の月が凍結の月（どの月が最後かは助走の後に分かる）
    //   ⑮(951) 写しは凍結した国の持ち物＝その国の月の締め（毎月・LandbaseMonth の頭）で、国の州の RGO の集合に無い写しを消す（割譲・建国・再統合のどれでも・戻っても写しは無い＝Territory.TransferRegion がエンジンの凍結を消すのと同じ集合を計器が自分で選ぶ）。
    //     四半期の締めの後の共有層（外交の 3 歩・再統合・建国）の中で出て戻る州は月の締めの口では見えない＝写しが残る（lbFrozenCopyNoEngine に数えられる）。眠った RGO（受け口が呼ばれない月）は差分を積まない（エンジンも同じ）
`````

## §4　形=doc3　元の行=242-242　次のメンバー=Held
`````text
        /// <summary>(951) この国が写しを持つ RGO の名簿（写しを作った順）と、月の締めで組む国の州の RGO の集合（毎月空にする）。</summary>
`````

## §5　形=trail　元の行=263-263　次のメンバー=BasisxRgoRent
`````text
// (970) 土地税の段 A-2b-0（basisx_s）：割る前の地代を受け手ごとに積む（読むだけ）
`````

## §6　形=trail　元の行=270-270　次のメンバー=ReferenceEquals
`````text
// (951) 他の国の写しを上書きする＝この国の名簿へ（前の国は自分の締めで名簿から外すだけ）
`````

## §7　形=doc3　元の行=289-289　次のメンバー=LbDropLeft
`````text
    /// <summary>(951) ⑮ 国 <paramref name="w"/> の写しの名簿のうち、今の国の州の RGO に無い物の写しを消し、名簿から外す（他の国の写しになった物は名簿から外すだけ）。</summary>
`````

## §8　形=trail　元の行=311-311　次のメンバー=LbDropLeft
`````text
// (951) ⑮ 国の州に無い写しを消す（毎月・助走の月も・下の照合と写しより前）
`````
