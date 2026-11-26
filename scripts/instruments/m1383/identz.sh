#!/usr/bin/env bash
# (1383) 合流の腕 Z（ident 段 2・合わせた木から wy・bk の旗の既定を 0 へ戻した木 wt_m1383z）。宣言：追加の表なし・テストの差 +21（kt 1・rf 12・wy 2・bk 6）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1383_work/identz.mark bash scripts/instruments/m1383/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1383_work/tmp TMP=D:/PoB_work/m1383_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1383z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1383_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1383z --tree D:/PoB_work/wt_m1383z --work D:/PoB_work/ident_m1383z --saveleak --ng --save 20261126 --stage2 --tests-delta 21
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1383_work/arms_start.txt
exit $RC
