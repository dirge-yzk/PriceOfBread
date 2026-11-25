#!/usr/bin/env bash
# (1356) c143k59 腕 Z（ident 段 2・器の木＝旗 0・c139nc/identz.sh の写し）。宣言：計器の列は足さない・テストの差 +3（FemalePartLawFreeTests の T1〜T3）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c143k59_work/identz.mark bash scripts/instruments/c143k59/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c143k59_work/tmp TMP=D:/PoB_work/c143k59_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c143k59 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c143k59_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c143k59z --tree D:/PoB_work/wt_c143k59 --work D:/PoB_work/ident_c143k59 --saveleak --ng --save 20261125 --stage2 --tests-delta 3
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c143k59_work/arms_start.txt
exit $RC
