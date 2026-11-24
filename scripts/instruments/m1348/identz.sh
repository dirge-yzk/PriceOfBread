#!/usr/bin/env bash
# (1348) 合流の腕 Z（ident 段 2・合わせた木から wd・lr の点火を戻した木 wt_m1348z＝旗 0）。宣言：lrlock_s は腕にだけある・テストの差 +17（cn 3・wd 4・lr 10）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1348_work/identz.mark bash scripts/instruments/m1348/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1348_work/tmp TMP=D:/PoB_work/m1348_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1348z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1348_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1348z --tree D:/PoB_work/wt_m1348z --work D:/PoB_work/ident_m1348z --saveleak --ng --save 20261125 --stage2 --tests-delta 17 --added-tables lrlock
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1348_work/arms_start.txt
exit $RC
