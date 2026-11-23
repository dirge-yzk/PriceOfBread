#!/usr/bin/env bash
# (1325) c138ss 腕 Z（ident 段 2・器の木＝旗 0・c138ss/identz.sh の写し）。宣言：列が増えた表 subst_s（6 列）・テストの差は引数 TD（器の試験の数）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c138ss_work/identz.mark bash scripts/instruments/c138ss/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c138ss_work/tmp TMP=D:/PoB_work/c138ss_work/tmp
cd D:/PoB_work/wt_c138ss || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c138ss_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c138ssz --tree D:/PoB_work/wt_c138ss --work D:/PoB_work/ident_c138ss --saveleak --ng --save 20261123 --stage2 --tests-delta ${TD:?TD} --grown-ok subst_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c138ss_work/arms_start.txt
exit $RC
