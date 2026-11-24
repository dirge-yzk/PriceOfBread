#!/usr/bin/env bash
# (1335) c140hf 腕 Z（ident 段 2・器の木＝旗 0・c139nc/identz.sh の写し）。宣言：計器の列は足さない・テストの差 +4（HungerFlightTests の T1〜T4）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c140hf_work/identz.mark bash scripts/instruments/c140hf/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c140hf_work/tmp TMP=D:/PoB_work/c140hf_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c140hf || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c140hf_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c140hfz --tree D:/PoB_work/wt_c140hf --work D:/PoB_work/ident_c140hf --saveleak --ng --save 20261124 --stage2 --tests-delta 4
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c140hf_work/arms_start.txt
exit $RC
