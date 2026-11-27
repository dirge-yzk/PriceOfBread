#!/usr/bin/env bash
# (1418) 合流の腕 Z（ident 段 2・合わせた木から WarLuck を 0.12・RepayFloorOnly を 0 へ戻した木 wt_m1418z）。宣言：表の差 0（rb は表の差 0・lk と fl は計器を足していない）・テストの差 -6（rb −14・lk +3・fl +5）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1418_work/identz.mark bash scripts/instruments/m1418/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1418_work/tmp TMP=D:/PoB_work/m1418_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1418z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1418_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1418z --tree D:/PoB_work/wt_m1418z --work D:/PoB_work/ident_m1418z --saveleak --ng --save 20261127 --stage2 --tests-delta ${TD:--6}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1418_work/arms_start.txt
exit $RC
