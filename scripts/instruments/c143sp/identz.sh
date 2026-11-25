#!/usr/bin/env bash
# (1357) c143sp 腕 Z（ident 段 2・器の木＝旗 0・c139nc/identz.sh の写し）。宣言：新しい表 stproc_s（--added-tables stproc_s）・テストの差 +7（StateProcureLedgerTests の T1・T2・T3×2・T4・T5×2）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c143sp_work/identz.mark bash scripts/instruments/c143sp/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c143sp_work/tmp TMP=D:/PoB_work/c143sp_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c143sp || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c143sp_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c143spz --tree D:/PoB_work/wt_c143sp --work D:/PoB_work/ident_c143sp --saveleak --ng --save 20261125 --stage2 --tests-delta 7 --added-tables stproc_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c143sp_work/arms_start.txt
exit $RC
