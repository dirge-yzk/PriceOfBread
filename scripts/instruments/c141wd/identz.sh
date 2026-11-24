#!/usr/bin/env bash
# (1343) c141wd 腕 Z（ident 段 2・器の木＝旗 0・c140hf/identz.sh の写し）。宣言：計器の列は足さない・テストの差 +4（WarmupDecisionSymmetryTests の T1・T2×2・T3）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c141wd_work/identz.mark bash scripts/instruments/c141wd/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c141wd_work/tmp TMP=D:/PoB_work/c141wd_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c141wd || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c141wd_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c141wdz --tree D:/PoB_work/wt_c141wd --work D:/PoB_work/ident_c141wd --saveleak --ng --save 20261124 --stage2 --tests-delta 4
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c141wd_work/arms_start.txt
exit $RC
