#!/usr/bin/env bash
# (1118) c108ps 突然変異の腕（門 G1〜U が赤になるかを読む）。呼び方：bash mut_arms.sh <変異...>（M2 M3 M6 M7）
# 木＝器のコミット 623750fd7 を batch.sh prep で写し、mutapply.py で変異を当て、旗 1 は POB_COEF（coef_<腕>.txt）。M1（旗 1 でも何もしない＝旗 0 と同じ挙動）は ident c108psz の ib の生で読む。
# 計器の表は parlseat_s・monlaw_s・prds_s だけ・種 1・9 国。作業 D:/PoB_work/c108ps_work/mw。結果は gate_ps.py で読む。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c108ps_work/mw TEMP=D:/PoB_work/c108ps_work/tmp TMP=D:/PoB_work/c108ps_work/tmp
B=D:/PoB_work/wt_c108ps/scripts/instruments/batch.sh
AP=D:/PoB_work/wt_c108ps/measurements/m1118_20261114/mut/mutapply.py
mkdir -p $POB_WORK
for m in "$@"; do
  arm=psmut$m
  echo "ParliamentSeatsWithoutGap=1" > $POB_WORK/coef_$arm.txt
  bash $B prep $arm commit:623750fd7 plain "$AP $m" > $POB_WORK/prep_$arm.txt 2>&1
  grep -q PREPDONE $POB_WORK/prep_$arm.txt || { echo "$arm PREPFAIL"; continue; }
  POB_INSTRUMENTS_TABLES=parlseat_s,monlaw_s,prds_s POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" INS=1 bash $B run $arm 1 > $POB_WORK/run_$arm.txt 2>&1
  echo "$arm rc=$?"
  rm -rf $POB_WORK/src_$arm
done
