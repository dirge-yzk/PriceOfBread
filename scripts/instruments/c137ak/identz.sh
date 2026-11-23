#!/usr/bin/env bash
# (1318) c137ak 腕 Z（ident 段 2・器の木＝旗 0・c135nc/identz.sh の写し）。宣言：新しい表 sectreas_s・テストの差 +9（SecessionTreasuryShareTests の T1〜T7＋T8 の 2 件）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c137ak_work/identz.mark bash scripts/instruments/c137ak/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c137ak_work/tmp TMP=D:/PoB_work/c137ak_work/tmp
cd D:/PoB_work/wt_c137ak || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c137ak_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c137akz --tree D:/PoB_work/wt_c137ak --work D:/PoB_work/ident_c137ak --saveleak --ng --save 20261123 --stage2 --tests-delta 9 --added-tables sectreas_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c137ak_work/arms_start.txt
exit $RC
