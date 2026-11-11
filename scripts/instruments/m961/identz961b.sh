#!/usr/bin/env bash
# (961) 合流の腕 Z（合わせた木の kp・lt の旗を 0 にした使い捨ての木 wt_m961z）の恒等の判定（identz961.sh の型）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m961_work2/identz.mark bash scripts/instruments/m961/identz961b.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m961z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m961/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m961_work2/arms_start.txt
bash D:/PoB_work/wt_m961/scripts/instruments/ident/ident.sh run m961z --tree D:/PoB_work/wt_m961z --work D:/PoB_work/ident_m961z --saveleak --tests-delta 6 --known-extra cpop_s:outOtherTop1Route --ng --save 20261108
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m961_work2/arms_start.txt
exit $RC
