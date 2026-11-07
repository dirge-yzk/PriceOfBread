#!/usr/bin/env bash
# (926) 合流の腕 Z（合わせた木＝wt_m926・挙動の線の旗なし）の恒等の判定（宣言：cearn_s・cpop_s の列が末尾に伸びる〔c80eb〕・テストの数 +8〔c80eb 5・c80sm 3〕・savetest と leakhunt・NG）。(919) identz919.sh の写し。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m926/identz.mark bash scripts/instruments/m919/identz919.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m926 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m926/arms_start.txt
bash D:/PoB_work/wt_m926/scripts/instruments/ident/ident.sh run m926z --tree D:/PoB_work/wt_m926 --work D:/PoB_work/ident_m926z --saveleak --tests-delta 8 --grown-ok cearn_s,cpop_s --ng --save 20261107
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m926/arms_start.txt
exit $RC
