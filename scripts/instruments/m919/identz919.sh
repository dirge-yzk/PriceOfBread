#!/usr/bin/env bash
# (919) 合流の腕 Z（合わせた木の旗 0＝wt_m919z）の恒等の判定（宣言なし・テストの数の増減 0・savetest と leakhunt・NG）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m919/identz.mark bash scripts/instruments/m919/identz919.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m919z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m919/arms_start.txt
bash D:/PoB_work/wt_m919/scripts/instruments/ident/ident.sh run m919z --tree D:/PoB_work/wt_m919z --work D:/PoB_work/ident_m919z --saveleak --tests-delta 0 --ng --save 20261107
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m919/arms_start.txt
exit $RC
