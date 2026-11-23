#!/usr/bin/env bash
# (1323) c138lp 腕 Z（ident 段 2・器の木＝旗 0・c137ak/identz.sh の写し）。宣言：新しい表 lawdir_s・テストの差 +10（LawPreferenceDirectionTests の T1〜T8＋T9 の 2 件）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c138lp_work/identz.mark bash scripts/instruments/c138lp/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c138lp_work/tmp TMP=D:/PoB_work/c138lp_work/tmp
cd D:/PoB_work/wt_c138lp || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c138lp_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c138lpz --tree D:/PoB_work/wt_c138lp --work D:/PoB_work/ident_c138lp --saveleak --ng --save 20261123 --stage2 --tests-delta 10 --added-tables lawdir_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c138lp_work/arms_start.txt
exit $RC
