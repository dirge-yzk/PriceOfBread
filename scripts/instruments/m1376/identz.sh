#!/usr/bin/env bash
# (1376) 合流の腕 Z（合わせた木・段 2・PRUNE=0＝生を keep_1376/base_ins へ移すため）。bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/m1376/identz.sh
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1376_work/tmp TMP=D:/PoB_work/m1376_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1376 || exit 2
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,1))")GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1376_work/arms_start.txt
PRUNE=0 bash scripts/instruments/ident/ident.sh run m1376z --tree D:/PoB_work/wt_m1376 --work D:/PoB_work/ident_m1376z --saveleak --ng --save 20261126 --stage2 --tests-delta 23 --added-tables lawstep_s --rewritten-tables tsreg_s@scripts/instruments/c145ts/tsreg_check.py
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1376_work/arms_start.txt
exit $RC
