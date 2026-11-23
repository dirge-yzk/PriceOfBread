#!/usr/bin/env bash
# (1332) c139nc 腕 Z（ident 段 2・器の木＝旗 0・c138lp/identz.sh の写し）。宣言：capcol_s の末尾に 8 列（--grown-ok capcol_s）・テストの差 +6（NextCapitalHomeFirstTests の T8・T9・T10・T12 と T11 の 2 件）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c139nc_work/identz.mark bash scripts/instruments/c139nc/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c139nc_work/tmp TMP=D:/PoB_work/c139nc_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c139nc || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c139nc_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c139ncz --tree D:/PoB_work/wt_c139nc --work D:/PoB_work/ident_c139nc --saveleak --ng --save 20261124 --stage2 --tests-delta 6 --grown-ok capcol_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c139nc_work/arms_start.txt
exit $RC
