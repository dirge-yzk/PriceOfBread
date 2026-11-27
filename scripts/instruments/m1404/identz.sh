#!/usr/bin/env bash
# (1404) 合流の腕 Z（ident 段 2・合わせた木から pm・rn の旗の既定を旧へ戻した木 wt_m1404z）。宣言：追加の表 wexit（wx）・rnd（rn）・テストの差 +22（wx 14・pm 2・rn 6＝合わせた木の --list-tests で数えた 3317）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1404_work/identz.mark bash scripts/instruments/m1404/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1404_work/tmp TMP=D:/PoB_work/m1404_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1404z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1404_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1404z --tree D:/PoB_work/wt_m1404z --work D:/PoB_work/ident_m1404z --saveleak --ng --save 20261127 --stage2 --added-tables wexit,rnd --tests-delta 22
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1404_work/arms_start.txt
exit $RC
