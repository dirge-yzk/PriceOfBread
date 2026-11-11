#!/usr/bin/env bash
# (940) 合流の腕 Z（合わせた木＝wt_m940・挙動の線の旗なし＝c82it・c82sm だけ）の恒等の判定（宣言なし・テストの数 +0・savetest と leakhunt・NG）。(926) identz926.sh の写し。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m940/identz.mark bash scripts/instruments/m940/identz940.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m940 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m940/arms_start.txt
bash D:/PoB_work/wt_m940/scripts/instruments/ident/ident.sh run m940z --tree D:/PoB_work/wt_m940 --work D:/PoB_work/ident_m940z --saveleak --tests-delta 0 --ng --save 20261107
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m940/arms_start.txt
exit $RC
