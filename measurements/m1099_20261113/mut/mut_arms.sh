#!/usr/bin/env bash
# (1099) c105gp 突然変異の腕（門 G1〜G4 が赤になるかを読む）。使い方: bash mut_arms.sh <腕の名前...>（gpF1＝変異なし・gpM1〜gpM5）
# 木＝commit daea8a9f1（器・旗 0）を batch.sh prep で写し、mutapply.py で変異を当て、旗 1 は POB_COEF（coef_<腕>.txt）。M0（旗 0）は基線 keep_1096/base_ins で読む（ident で旗 0 が基線と全行一致）。
# 計器の表は sf_s・scalars_s・nation_s だけ・種 1〜2・9 国。作業 D:/PoB_work/c105gp_work/mw。結果は gate1099.py で読む。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c105gp_work/mw
B=D:/PoB_work/wt_c105gp/scripts/instruments/batch.sh
AP=D:/PoB_work/wt_c105gp/measurements/m1099_20261113/mut/mutapply.py
mkdir -p $POB_WORK
for arm in "$@"; do
  m=${arm#gp}
  echo "NonElectiveSeatsEmpty=1" > $POB_WORK/coef_$arm.txt
  if [ "$m" = F1 ]; then bash $B prep $arm commit:daea8a9f1 plain > $POB_WORK/prep_$arm.txt 2>&1
  else bash $B prep $arm commit:daea8a9f1 plain "$AP $m" > $POB_WORK/prep_$arm.txt 2>&1; fi
  grep -q PREPDONE $POB_WORK/prep_$arm.txt || { echo "$arm PREPFAIL"; continue; }
  POB_INSTRUMENTS_TABLES=sf_s,scalars_s,nation_s POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" INS=1 bash $B run $arm 2 > $POB_WORK/run_$arm.txt 2>&1
  echo "$arm rc=$?"
done
