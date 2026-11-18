#!/usr/bin/env bash
# (1212) 合流の腕 Z（m1205/identz1205.sh の型）：木 wt_m1212z（0e69a22c6＝ca＋uh＋b1 の記録）＝挙動の線 hs は入れていない。
#   宣言：テストの差 +4（ca +6・uh −2）・ca の計器（足した表 2・列の増えた表 2・行の増えた表 1）・PRUNE=0（生を keep_1212/base_ins へ移す）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1212_work/identz.mark bash scripts/instruments/m1212/identz1212.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1212_work/tmp TMP=D:/PoB_work/m1212_work/tmp
cd D:/PoB_work/wt_m1212z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1212z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1212_work/arms_start.txt
PRUNE=0 bash D:/PoB_work/wt_m1212z/scripts/instruments/ident/ident.sh run m1212z --tree D:/PoB_work/wt_m1212z --work D:/PoB_work/ident_m1212z --saveleak --tests-delta 4 --added-tables satneeds_s,warpolity_s --grown-ok soldiervac_s,nation_s --rows-grown-ok dep_s --ng --save 20261118
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1212_work/arms_start.txt
exit $RC
