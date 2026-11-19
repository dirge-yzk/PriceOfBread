#!/usr/bin/env bash
# (1240) 合流の腕 Z（m1233/identz1233.sh の型）：木 wt_m1240（恒等の線 ts＋rk の項 1＋焼き直し・挙動の線は入れない＝旗の既定は全て基線のまま）。
#   宣言なし＋テストの差 +1（ts の T4）。PRUNE は既定（1）。基線は m1233 のまま。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1240_work/identz.mark bash scripts/instruments/m1240/identz1240.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1240_work/tmp TMP=D:/PoB_work/m1240_work/tmp
cd D:/PoB_work/wt_m1240 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1240/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1240_work/arms_start.txt
bash D:/PoB_work/wt_m1240/scripts/instruments/ident/ident.sh run m1240z --tree D:/PoB_work/wt_m1240 --work D:/PoB_work/ident_m1240z --saveleak --ng --save 20261119 --tests-delta 1
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1240_work/arms_start.txt
exit $RC
