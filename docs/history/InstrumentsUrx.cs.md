# InstrumentsUrx.cs の注の履歴

由来：1cd66aae5 の InstrumentsUrx.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsUrx.cs#番号〕だけが残る。

## §1　形=full　元の行=3-15　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (649) 都市と農村の規模の差の区間別の表 urx_s と PopDecision の駆動の表 urxpd_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsEpi.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・シミュの状態を足さない・係数を足さない・CostLedger を触らない。
// 事前登録 measurements/urx_649_20261030_prereg.txt（[2]・追記 1 の [9]・追記 2 の [10] ⓔⓕ）。
// ■urx_s<種>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ）：国×群（Core／Other＝州の Tier の 2 群）×区間×四半期の、都市と農村の POP の規模の差（千人）。
//   印＝ProbeCheck の頭（UrxMark）。区間名＝その印の名前＝直前の印から今の印までの差。UrxEnter／UrxExit＝社会移動（Meritocracy.Step）と疫病（ApplyEpidemicDeaths）の前後で差を切り出す。
//   全ての区間の差の和＝（最後の水準−最初の水準）に構成上一致する（桁落ちの残差だけ）。助走（WarmupFrozen）の間は印を打たず、助走が明けた最初の印で据え直す（その印の差は書かない）。
//   列：quarter／year／nation／group／section／months（印の数）／dUrbanK／dRuralK。四半期の末に section＝_level（Fold の時点の水準）、最初の Fold に section＝_base（据え直した水準）。
// ■urxpd_s<種>.tsv：本国（Tier＝Core）の州の PopDecision.Run の直前の駆動（読むだけ・同じ式を組み直す）。列は UrxPdHeaderText。重みの付く列（w*）は total（pair の規模）で重みを付けた和＝読み手が sumTotal で割る。
// ■(706) urb3：urxpd_s の末尾に 4 列（既存の列の並びと値は変えない）。wSubV＝対の農村の 1 人あたりの自給の値（月・名目）×total の和／wHousU＝対の都市の 1 人あたりの住居の費用×total の和／wGsym＝〈移る値打ち〉の対称の相対差 g×total の和（式は本物の PopDecision.MigrationValues＝旗が 0 でも出す）／
//   flowFlipRegionMonths＝流れの向きが前の月と逆の州×月の数（前の月も今の月も流れが 0 でなく、符号が違う＝飢えの門で 0 になった月は数えず、その次の月の比べ先にもならない）。読み手が wX÷sumTotal で重みを付けた平均にする。
//   ■(713) urb4：その後ろに wAbsGsym＝|g|×total の和（wGsym は符号つきの和＝旗 1 で州×月の符号が混ざると打ち消し合い、差が閉じていなくても小さく出る＝W1 の門はこの列で読む）。
// ⚠限界（計器が自己申告する）：①印の区間は〈その区間の間に起きた全て〉の差（月次政治には PopDecision のほか外交の間に挟まる人の動きも入る＝urxpd_s の sumFlowPos−sumFlowNeg と比べる）
//   ②群は州の Tier の印＝印の間に Tier が変わった州の差は変わった側の群へ入る（国の合計では消える）③urxpd_s は PopDecision の式の写し＝式が変わったら番人（UrxInstrumentTests）が赤になる。
`````

## §2　形=trail　元の行=33-33　次のメンバー=PdWGsym
`````text
// (706) urb3：〈移る値打ち〉の列（total で重みを付けた和）
`````

## §3　形=trail　元の行=34-34　次のメンバー=PdFlipRegionMonths
`````text
// (706) urb3：流れの向きが前の月と逆の州×月の数
`````

## §4　形=trail　元の行=35-35　次のメンバー=PdWAbsGsym
`````text
// (713) urb4：|g|×total の和（W1 の門の分母の定義＝符号が混ざっても打ち消し合わない）
`````

## §5　形=trail　元の行=36-36　次のメンバー=PdPrevSign
`````text
// (706) urb3：州ごとの前の月の流れの向き（−1／0／+1）。四半期をまたいで持つ（累積と違い Fold で消さない）
`````

## §6　形=doc3　元の行=100-100　次のメンバー=UrxPd
`````text
    /// <summary>PopDecision.Run の直前：本国の州の駆動を PopDecision.Run と同じ式で組み直して累積する（読むだけ）。（都市化の天井と UrbanIndSmoothed は (812) で削除＝cap・capBound の列は定数。）</summary>
`````

## §7　形=trail　元の行=116-116　次のメンバー=SubsistenceValue
`````text
// (706) urb3
`````

## §8　形=trail　元の行=117-117　次のメンバー=Abs
`````text
// (713) urb4
`````

## §9　形=trail　元の行=118-118　次のメンバー=sgn
`````text
// (706) urb3：流れの向きの反転（前の月も今の月も 0 でなく、向きが逆）
`````

## §10　形=doc3　元の行=123-123　次のメンバー=UrxPdPeek
`````text
    /// <summary>番人用：今の四半期の urxpd_s の積みの写し（積みは消さない）。(706) 流れの向きの反転と〈移る値打ち〉の列を単体で確かめる口。</summary>
`````

## §11　形=full　元の行=147-148　次のメンバー=mv
`````text
        // (812) 旗 UrbanCapOff の焼き込み：天井なし＝cap は 1.0・capBound は false のまま（計器の列は残す）
        // (706) urb3：〈移る値打ち〉V の列は常に出す（本物の式 PopDecision.MigrationValues を呼ぶ＝写しではない）。流れは旗 UrbanFlowReal に従い Run と同じ式
`````

## §12　形=full　元の行=163-168　次のメンバー=UrxRtHeaderText
`````text
    // ---------------------------------------------------------------- (697) 往復の表 urxrt_s（読むだけ・恒等） ----------------------------------------------------------------
    // 人の移し方の 1 本（Pop.Blend）の呼び手のファイルごとに、国の都市 POP への入りと出の総量（千人）を数える。純（入−出）は urx_s の区間・urxpd_s の流れと同じ向きで、往復の総量＝入＋出。
    // 数え方：同じ国の中の移動は農村→都市が入り・都市→農村が出（都市どうし・農村どうしは数えない）。国境を越える移動（越境移民・招聘）は受け手の国の都市 POP への着が入り・送り手の国の都市 POP からの発が出。
    // route＝Blend の呼び手のファイル名（PopDecision＝州の中の農村↔都市・Meritocracy＝社会移動の職の移し・Territory＝州の間の移民と入植・Military＝徴兵と復員・MultiNationSim＝越境）。
    // ⚠限界：①挙げた国の POP だけ（PopOwner の登録は月初＝その月の途中に生まれた POP の移動は数えない）②助走（その国の月が Active でない間）は数えない③呼び手のファイル 1 本の中に別の動詞が混ざる（Territory など）ときは区別しない
    //   ④疫病・出生・死は人の移し方を通らない＝数えない（urx_s の区間の差とは別の物）。
`````

## §13　形=trail　元の行=222-222　次のメンバー=UrxRtFold
`````text
// (697) 往復の表（Armed でなくても書く）
`````

## §14　形=trail　元の行=254-254　次のメンバー=PdWSubV
`````text
// (706) urb3
`````

## §15　形=trail　元の行=255-255　次のメンバー=PdWAbsGsym
`````text
// (713) urb4
`````
