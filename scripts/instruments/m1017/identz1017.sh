#!/usr/bin/env bash
# (1017) 合流（identz1001.sh の写し）：腕 Z＝合わせた木で ColonyPettyEntryEmptyTarget を 0 にした使い捨ての木 wt_m1017z。
#   宣言：足した表 corncf_s・lordstat_s（c94cl）・recipeout_s（c94ro）・テスト +49（cl 39・ro 5・pe 5）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1017_work/identz.mark bash scripts/instruments/m1017/identz1017.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1017z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1017/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1017_work/arms_start.txt
bash D:/PoB_work/wt_m1017/scripts/instruments/ident/ident.sh run m1017z --tree D:/PoB_work/wt_m1017z --work D:/PoB_work/ident_m1017z --saveleak --tests-delta 49 --ng --save 20261110 --added-tables corncf_s,lordstat_s,recipeout_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1017_work/arms_start.txt
exit $RC
