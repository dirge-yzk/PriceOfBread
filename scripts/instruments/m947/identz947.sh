#!/usr/bin/env bash
# (947) 合流の腕 Z（T_Z＝c83tf と c83ic の器〔旗 0〕を入れた木 8ebe83e0＝wt_m947z）の恒等の判定（identz933.sh の型）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m947/identz.mark bash scripts/instruments/m947/identz947.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m947z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m947/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m947/arms_start.txt
bash D:/PoB_work/wt_m947/scripts/instruments/ident/ident.sh run m947z --tree D:/PoB_work/wt_m947z --work D:/PoB_work/ident_m947z --saveleak --tests-delta 5 --ng --save 20261108
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m947/arms_start.txt
exit $RC
