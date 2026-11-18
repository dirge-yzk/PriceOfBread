#!/usr/bin/env bash
# (1205) 合流の腕 Z（m1198/identz1198.sh の型）：合わせた木 wt_m1205（nk＋k11＋焼き直した opening_bake.json＋写し）＝挙動の線は入れていない。
#   宣言：テストの差 +2（k11 の JobPoolTemperatureGuardTests 2 本・nk は 0）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1205_work/identz.mark bash scripts/instruments/m1205/identz1205.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1205_work/tmp TMP=D:/PoB_work/m1205_work/tmp
cd D:/PoB_work/wt_m1205 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1205/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1205_work/arms_start.txt
bash D:/PoB_work/wt_m1205/scripts/instruments/ident/ident.sh run m1205z --tree D:/PoB_work/wt_m1205 --work D:/PoB_work/ident_m1205z --saveleak --tests-delta 2 --ng --save 20261118
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1205_work/arms_start.txt
exit $RC
