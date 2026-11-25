#!/usr/bin/env bash
# (1372) c145ts の腕 Z（段 2）。bash scripts/instruments/tools/bg.sh D:/PoB_work/c145ts_work/ts.mark bash scripts/instruments/c145ts/identz.sh <試験の増え>
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c145ts_work/tmp TMP=D:/PoB_work/c145ts_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c145ts || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c145ts_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c145tsz --tree D:/PoB_work/wt_c145ts --work D:/PoB_work/ident_c145ts --saveleak --ng --save 20261126 --stage2 --tests-delta "$1" --added-tables lawstep_s --rewritten-tables tsreg_s@scripts/instruments/c145ts/tsreg_check.py
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c145ts_work/arms_start.txt
exit $RC
