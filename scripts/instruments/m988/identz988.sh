#!/usr/bin/env bash
# (988) 合流の腕 Z（dd の TakeFloorPositive を 0 にした使い捨ての木 wt_m988z）の恒等の判定（identz981.sh の写し）。
#   宣言：表 landsale_s（ls の計器・新）・テスト +20（ls 7・dd 13・tb 0）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m988_work/identz.mark bash scripts/instruments/m988/identz988.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m988z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m988/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m988_work/arms_start.txt
bash D:/PoB_work/wt_m988/scripts/instruments/ident/ident.sh run m988z --tree D:/PoB_work/wt_m988z --work D:/PoB_work/ident_m988z --saveleak --tests-delta 20 --added-tables landsale --ng --save 20261109
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m988_work/arms_start.txt
exit $RC
