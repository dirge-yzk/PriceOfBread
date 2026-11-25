#!/usr/bin/env bash
# (1362) 合流の腕 Z（ident 段 2・合わせた木から k59・sp の旗の既定を 0 へ戻した木 wt_m1362z）。宣言：tsreg_s・cwend_s・stproc_s は腕にだけある・テストの差 +18（sg 3・ce 5・k59 3・sp 7）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1362_work/identz.mark bash scripts/instruments/m1362/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1362_work/tmp TMP=D:/PoB_work/m1362_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1362z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1362_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1362z --tree D:/PoB_work/wt_m1362z --work D:/PoB_work/ident_m1362z --saveleak --ng --save 20261125 --stage2 --tests-delta 18 --added-tables tsreg,cwend,stproc
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1362_work/arms_start.txt
exit $RC
