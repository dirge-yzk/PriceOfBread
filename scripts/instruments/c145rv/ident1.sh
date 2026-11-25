#!/usr/bin/env bash
# (1371) c145rv 腕（ident 段 1・器の木＝旗 0）。宣言：計器の列は足さない・テストの差 +8（WarWearyPoliticsCutTests 4・SatReadersToDiscontentTests 4）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c145rv_work/ident1.mark bash scripts/instruments/c145rv/ident1.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c145rv_work/tmp TMP=D:/PoB_work/c145rv_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c145rv || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "ident1 start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c145rv_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c145rv --tree D:/PoB_work/wt_c145rv --work D:/PoB_work/ident_c145rv --saveleak --save 20261126 --tests-delta 8
RC=$?
echo "ident1 end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c145rv_work/arms_start.txt
exit $RC
