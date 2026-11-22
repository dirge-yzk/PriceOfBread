#!/usr/bin/env bash
# (1304) c135nc 腕 Z（ident 段 2・器の木＝旗 0・m1301/identz1301.sh の写し）。宣言：新しい表 capcol_s・テストの差 +8（NextCapitalHomeFirstTests の T1〜T7＝T2 は 2 件）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c135nc_work/identz.mark bash scripts/instruments/c135nc/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c135nc_work/tmp TMP=D:/PoB_work/c135nc_work/tmp
cd D:/PoB_work/wt_c135nc || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c135nc_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c135ncz --tree D:/PoB_work/wt_c135nc --work D:/PoB_work/ident_c135nc --saveleak --ng --save 20261122 --stage2 --tests-delta 8 --added-tables capcol_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c135nc_work/arms_start.txt
exit $RC
