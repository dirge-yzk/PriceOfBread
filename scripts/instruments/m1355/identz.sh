#!/usr/bin/env bash
# (1355) 合流の腕 Z（ident 段 2・合わせた木から ds・lr の旗の既定を 0 へ戻した木 wt_m1355z）。宣言：femp_s・migcap_s・lrlock_s は腕にだけある・テストの差 +20（ik 5・ds 5・lr 10）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1355_work/identz.mark bash scripts/instruments/m1355/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1355_work/tmp TMP=D:/PoB_work/m1355_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1355z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1355_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1355z --tree D:/PoB_work/wt_m1355z --work D:/PoB_work/ident_m1355z --saveleak --ng --save 20261125 --stage2 --tests-delta 20 --added-tables femp,migcap,lrlock
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1355_work/arms_start.txt
exit $RC
