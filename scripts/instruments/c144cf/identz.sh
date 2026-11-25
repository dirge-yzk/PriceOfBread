#!/usr/bin/env bash
# (1366) c144cf の腕 Z（段 2）。bash scripts/instruments/tools/bg.sh D:/PoB_work/c144cf_work/identz.mark bash scripts/instruments/c144cf/identz.sh
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c144cf_work/tmp TMP=D:/PoB_work/c144cf_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c144cf || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c144cf_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c144cfz --tree D:/PoB_work/wt_c144cf --work D:/PoB_work/ident_c144cf --saveleak --ng --save 20261125 --stage2 --tests-delta 7 --rewritten-tables cwend_s@scripts/instruments/c144cf/cwend_check.py
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c144cf_work/arms_start.txt
exit $RC
