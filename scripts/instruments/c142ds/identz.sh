#!/usr/bin/env bash
# (1349) c142ds 腕 Z（ident 段 2・器の木＝旗 0・c139nc/identz.sh の写し）。宣言：計器の表 migcap_s を足す・テストの差 +5（DestFoodSymmetryTests の T1〜T4〔T2 は国内と越境の 2 本〕）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c142ds_work/identz.mark bash scripts/instruments/c142ds/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c142ds_work/tmp TMP=D:/PoB_work/c142ds_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c142ds || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c142ds_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c142dsz --tree D:/PoB_work/wt_c142ds --work D:/PoB_work/ident_c142ds --saveleak --ng --save 20261125 --stage2 --tests-delta 5 --added-tables migcap
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c142ds_work/arms_start.txt
exit $RC
