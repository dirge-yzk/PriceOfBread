# InstrumentsCrown.cs の注の履歴

由来：1cd66aae5 の InstrumentsCrown.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsCrown.cs#番号〕だけが残る。

## §1　形=full　元の行=3-17　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (889) 王権と議会の点検の恒等の計器 crownpc_s（読むだけ・恒等・事前登録 measurements/m889_20261106_prereg_cp.txt） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsCornint.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を作らない（計器の持ち物は ConditionalWeakTable だけ）。INS=1 のときだけ動く。
// 目的＝議会の統制力 ParliamentControl（PC）が四半期ごとにどの項で動いたかを、エンジン（CrownParliament.Step・Politics.cs:512 から）に 1 行も足さずに数え直す。
// 取り方＝前の四半期の末の断面（PC・政体・王冠・奪取・協力度・軍の帰属・政変の記憶・下賜の回数）を計器が貯め、今の四半期の末に Step の順で項を当て直す。
//   召集 → 巻き返し →（奪取なしのとき）① 議会の圧力 → ② 王権の圧力 → ②′ 軍の圧力 →（廃位・幕府の終わりの床・再ラベルは印だけ）。項＝走る値に当てた後−当てる前。
//   式（門・項・切り）は計器が書く（CrownParliament の関数を呼ばない）。思想の支持はエンジンの純関数（Ideologies.National・SupportAt・BasePolityCoord・PoliticalQuantities.DriftedPolityCoord）を読む＝世界を書かない。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・Flag＝0／1・Code＝整数の区分・PerMonth＝月あたりの流れ）：事前登録の〈列の式〉のとおり。
// ⚠限界（計器が自己申告する）：
//   ①四半期の末の断面＝Step の後の Meritocracy（社会移動）・共有層（講和・移民・内戦の決着・建国・思想の伝播）が動かす入力（正統性・思想の集計・兵士の規模・人口・参政権）は、Step の瞬間の値と違いうる
//     ＝①・②・②′・巻き返しの数え直しがずれうる。ずれた行は residualShare≠0・explainCode 9 と driftMaskCode で名乗る（隠さない）。召集は四半期の頭の Austerity と前の断面だけを読む＝ずれない。
//   ②最初の行（q0・建国の最初の四半期）は前の断面が無い＝項と残差は nan（explainCode 1）。
//   ③下賜（Politics.cs:680）の値はソースから数え直せない（挑戦の比が Transition の局所変数）＝grantFlag の印だけ（explainCode 6）。
//   ④政変（ApplyRegime）は RecentRegimeChanges の増え＞0.5 で印を付ける（1 回で +1・Step は ×(1−置き換わり率) だけ）。Step の前か後かは residualAlt と目標の一致で分ける（explainCode 3・4・5）。
//   ⑤(896) 旗 SummonByInterestShare＞0 の召集は四半期の末の利払い÷歳入と DebtGrip（計器が自分で書く式）を読む。エンジンは Politics.Step（QuarterEnd）で読む＝その後の共有層（割譲・講和・州の移り）で
//     国債か名目 GDP（getter）が動けば押す先がずれる＝旗 1 では召集の列もずれうる（〈召集はずれない〉は旗 0 だけ）。ずれた行は explainCode 9 と driftMaskCode の 16（旗 1 で召集の項≠0）で名乗る。
`````

## §2　形=full　元の行=31-34　次のメンバー=CrownSrcUnknown
`````text
    // ---- (928) 王冠の登録簿を最後に書いた口の符号 crownRegisterSourceCode（計器の持ち物・World に欄を足さない・セーブに載らない） ----
    // 符号：0 開幕または建国（Simulation の構築＝建国の子の国も同じ構築を通る・国の最初の行の quarter＞0 で分ける）・1 Transition 系の政体変更（Politics.ExecuteRegimeChange＝Transition と蜂起の決着のうち蜂起以外）・2 矯正講和・3 蜂起の決着（Politics の反乱の勝ち）・
    //   4 廃位（議会が王冠を廃す）・5 幕府の終わり・6 不明（口が印を付けていない国＝助走の後の復元〔GovernanceSnapshot〕など）。
    // ⚠限界：①符号は呼び口が付ける印＝口が増えても印が無ければ前の符号のまま。②ExecuteRegimeChange の呼び手のうち Transition と蜂起の決着だけ分けた（他に呼び手は無いと grep で確かめた）。
`````

## §3　形=trail　元の行=67-67　次のメンバー=Dg
`````text
// (896) 旗 1 の召集の入力：利払い÷歳入（歳入が正でなければ 0）・DebtGrip（計器の式）
`````

## §4　形=trail　元の行=121-121　次のメンバー=s
`````text
// (896) 旗 1：比例・押す先 DebtGrip・切りなし（SummonClamp は立たない）
`````
