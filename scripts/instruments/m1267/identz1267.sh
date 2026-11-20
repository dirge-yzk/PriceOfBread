#!/usr/bin/env bash
# (1267) 合流（m1267 の写し）の腕 Z（m1254/identz1254.sh の型）：木 wt_m1267z（枝 m1267z＝恒等の線 k12・sb・pc を合わせた木＝挙動の線 al を入れる前の頭・al の器の恒等は線の ident 67aa903b4 で測った）。
#   宣言：pc の新しい表 popclose_s＋テストの差 +11（pc 8・sb 3・k12 0）。段 2。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1267_work/identz.mark bash scripts/instruments/m1267/identz1267.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1267_work/tmp TMP=D:/PoB_work/m1267_work/tmp
cd D:/PoB_work/wt_m1267z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1267z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1267_work/arms_start.txt
bash D:/PoB_work/wt_m1267z/scripts/instruments/ident/ident.sh run m1267z --tree D:/PoB_work/wt_m1267z --work D:/PoB_work/ident_m1267z --saveleak --ng --save 20261121 --stage2 --tests-delta 11 --added-tables popclose_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1267_work/arms_start.txt
exit $RC
