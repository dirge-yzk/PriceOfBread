#!/usr/bin/env bash
# (1198) 合流の腕 Z（m1191/identz1191.sh の型）：木 wt_m1198z＝合わせた木から c119nd の段 B（41d0540c4）だけを戻した木（kf＋nd 段 A＋us・push しない）。
#   宣言：テストの差 +4（kf +2・nd 段 A −2・us +4）・mintcap_s:capImportAgSum（kf 項 3）。
#   引数 1：no-ins（D: が 35GB＋30GB に届かないとき＝素の 5 種とテストの数だけ）か full（計器つき 10 種まで）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1198_work/identz.mark bash scripts/instruments/m1198/identz1198.sh no-ins   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1198_work/tmp TMP=D:/PoB_work/m1198_work/tmp
MODE=${1:?no-ins|full}
cd D:/PoB_work/wt_m1198z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ($MODE) start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1198z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1198_work/arms_start.txt
if [ "$MODE" = no-ins ]; then
  bash D:/PoB_work/wt_m1198z/scripts/instruments/ident/ident.sh run m1198z --tree D:/PoB_work/wt_m1198z --work D:/PoB_work/ident_m1198z --no-ins --tests-delta 4
else
  bash D:/PoB_work/wt_m1198z/scripts/instruments/ident/ident.sh run m1198z --tree D:/PoB_work/wt_m1198z --work D:/PoB_work/ident_m1198z --saveleak --tests-delta 4 --known-extra mintcap_s:capImportAgSum --ng --save 20261118
fi
RC=$?
echo "identZ($MODE) end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1198_work/arms_start.txt
exit $RC
