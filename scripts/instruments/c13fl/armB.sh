#!/usr/bin/env bash
# (810) c13fl 腕 B の駆動：bash armB.sh <コミット>（Bash の run_in_background で）。旗 1 の既定の木を git archive で写し、計器つき 10 種（種 1〜10・9 国）で回す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c13fl_work/armB POB_REPO=D:/PoB_work/wt_c13fl PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}; S=D:/PoB_work/wt_c13fl/scripts/instruments
mkdir -p $POB_WORK
echo "c13flB start $(date '+%F %T') HEAD=${H:0:8}" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep c13flB commit:$H plain || exit 1
INS=1 bash $S/batch.sh run c13flB 10
