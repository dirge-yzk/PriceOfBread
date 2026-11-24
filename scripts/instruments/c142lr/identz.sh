#!/usr/bin/env bash
# (1350) c142lr 腕 Z（ident 段 2・器の木＝旗 0・c142lr/identz.sh の写し）。宣言：計器の表 lrlock を足した（--added-tables lrlock）・テストの差 +TD（ProtectedLawReversalTests）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c142lr_work/identz.mark bash scripts/instruments/c142lr/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c142lr_work/tmp TMP=D:/PoB_work/c142lr_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c142lr || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c142lr_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c142lrz --tree D:/PoB_work/wt_c142lr --work D:/PoB_work/ident_c142lr --saveleak --ng --save 20261125 --stage2 --added-tables lrlock --tests-delta ${TD:?TD}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c142lr_work/arms_start.txt
exit $RC
