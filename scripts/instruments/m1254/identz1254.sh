#!/usr/bin/env bash
# (1254) 合流の腕 Z（m1247/identz1247.sh の型）：木 wt_m1254z（枝 m1254z＝合わせた木の挙動の旗 3 本を 0）。
#   宣言：nr の新しい表 formation2_s・unifwar_s・tensionwhy_s＋テストの差 +24（ex 1・kc 2・bd 6・nr 15）。段 2。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1254_work/identz.mark bash scripts/instruments/m1254/identz1254.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1254_work/tmp TMP=D:/PoB_work/m1254_work/tmp
cd D:/PoB_work/wt_m1254z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1254z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1254_work/arms_start.txt
bash D:/PoB_work/wt_m1254z/scripts/instruments/ident/ident.sh run m1254z --tree D:/PoB_work/wt_m1254z --work D:/PoB_work/ident_m1254z --saveleak --ng --save 20261120 --stage2 --tests-delta 24 --added-tables formation2_s,unifwar_s,tensionwhy_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1254_work/arms_start.txt
exit $RC
