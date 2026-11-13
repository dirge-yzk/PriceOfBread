# InstrumentsLawgate2.cs の注の履歴

由来：1cd66aae5 の InstrumentsLawgate2.cs から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 InstrumentsLawgate2.cs#番号〕だけが残る。

## §1　形=full　元の行=5-16　次のメンバー=Instruments
`````text
// ---------------------------------------------------------------- (908) 施行の否決の理由の表 lawgate2_s（読むだけ・事前登録 measurements/m908_20261107_prereg_sm.txt [3]・素案 measurements/m900_20261106/s19/draft_00list_and_prereg900.md §3）----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない（pending は計器の側の表）。INS=1 のときだけ動く。
// 段の法の施行（Government.StepLevel）が〈前提で固定（符号 6）〉か〈不可逆で固定（符号 5）〉だった一歩を 1 行ずつ、四半期の末に書く（国×四半期×段の法の表の行）。
// 理由は計器が自分で導く＝Government.LawPrereq・GateInfo・UniversalConscriptionOpen を呼ばない。StepLevel の分岐の結果（符号）と、その時の盤面の欄（段・緊縮・官僚制度・参政権・政体・農奴解放）だけを読む。
//   LawPrereq の順序と同じ：第 1 の switch（社会保障の緊縮の上限）→第 2 の switch（参政権・農奴解放・官僚制度・政体・男性普選）。
// 列：quarter・year・nation・axis（PolicyAxis の名）・engineCode（5 不可逆／6 前提）・reasonCode・curStage・targetStage・austerityIndex・bureaucracyCode。
// reasonCode：8 緊縮の上限／9 参政権（制限選挙以上）／10 農奴解放（全面）／11 官僚制度（資格任用以上）／12 プロレタリア独裁のみ／13 男性普通選挙／14 女性の権利（選挙権）／
//   20 後退不能（符号 5 の農奴解放・土地改革・保護された法）／21 戦時のみ緩和可（符号 5 の労働時間・児童労働）／0 計器が導けなかった（導出の欠け＝試験が 0 行を要求する）。
//   素案の 1〜7（前段階・払えない請求・教育法・緊縮の下限・総動員・軍事費になし・戦時の解散）は StepLevel の呼び手（段の法の表 Government.LevelLaws の行）に届かない理由（税・支出・軍事費は Move）＝構造的に 0 行。
// ⚠限界（計器が自己申告する）：①理由は計器が自分で導く写し＝LawPrereq の前提が増えれば reasonCode 0 の行が立つ（0 の行数が欠けの検知）。②農奴解放の判定（農奴解放が全面でない）は
//   Privileges.SeigneurialDegree＞0 の写し（なし＝1・限定＝0.5・全面＝0）。③1 四半期に StepLevel が同じ軸へ複数回来れば最後の結果だけ（戦線の符号 openFrontStates と同じ規則）。
//   ④行は nation_s の戦線の符号と同じ述語（開いた戦線に数えられ、四半期の末にその符号が残る）で出す＝下側の門（行数＝戦線の数）は述語が同じなので、理由の導出の欠けだけを拾う。
`````
