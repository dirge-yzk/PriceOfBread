#!/usr/bin/env bash
# (1022) 合流の腕 Z（m1017/identz1017.sh の写し・木を wt_m1022z〔UbiquitousMineralExempt 0〕へ）
#   宣言：足した表 prodcap_s（c95pc）・列が増えた表 recipeout_s（c95pc）・テスト +12（pc 5・ub 7）。c95wn は外した（予言の追記 1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1022_work/identz.mark bash scripts/instruments/m1022/identz1022.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1022z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1022/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1022_work/arms_start.txt
bash D:/PoB_work/wt_m1022/scripts/instruments/ident/ident.sh run m1022z --tree D:/PoB_work/wt_m1022z --work D:/PoB_work/ident_m1022z --saveleak --tests-delta 12 --ng --save 20261110 --added-tables prodcap_s --grown-ok recipeout_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1022_work/arms_start.txt
exit $RC
