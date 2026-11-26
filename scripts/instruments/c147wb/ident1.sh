#!/usr/bin/env bash
# (1385) c147wb ident 段 1（旗 0 の木＝wy の焼き込みと器）。宣言：計器の列は足さない・テストの差は --tests-delta（最後の木の --list-tests で数える）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c147wb_work/ident1.mark bash scripts/instruments/c147wb/ident1.sh <テストの差>
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c147wb_work/tmp1 TMP=D:/PoB_work/c147wb_work/tmp1
mkdir -p $TEMP
cd D:/PoB_work/wt_c147wb || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "ident1 start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c147wb_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c147wb --tree D:/PoB_work/wt_c147wb --work D:/PoB_work/ident_c147wb --saveleak --tests-delta "$1"
RC=$?
echo "ident1 end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c147wb_work/arms_start.txt
exit $RC
