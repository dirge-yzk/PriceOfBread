#!/usr/bin/env bash
# (1260) 合流の腕 Z（m1254/identz1254.sh の型）：木 wt_m1260z（枝 m1260z＝合わせた木の挙動の旗 UnificationSkipFormed を 0）。
#   宣言：ci の新しい表 capinv_s＋テストの差 +90（ci 4・ih 85・k20 −3・uf 4）。段 2。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1260_work/identz.mark bash scripts/instruments/m1260/identz1260.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1260_work/tmp TMP=D:/PoB_work/m1260_work/tmp
cd D:/PoB_work/wt_m1260z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1260z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1260_work/arms_start.txt
bash D:/PoB_work/wt_m1260z/scripts/instruments/ident/ident.sh run m1260z --tree D:/PoB_work/wt_m1260z --work D:/PoB_work/ident_m1260z --saveleak --ng --save 20261120 --stage2 --tests-delta 90 --added-tables capinv_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1260_work/arms_start.txt
exit $RC
