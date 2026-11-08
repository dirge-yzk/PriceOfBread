#!/usr/bin/env bash
# (981) 合流の腕 Z（cs の LandShareCrownSlot を 0 にし world.json を 5e6adf98 へ戻した使い捨ての木 wt_m981z）の恒等の判定（identz975.sh の写し）。
#   宣言：landshare_s の末尾 20 列（cs の計器）・テスト +12（sm 2・cs 10・ts 0）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m981_work/identz.mark bash scripts/instruments/m981/identz981.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m981z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m981/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m981_work/arms_start.txt
bash D:/PoB_work/wt_m981/scripts/instruments/ident/ident.sh run m981z --tree D:/PoB_work/wt_m981z --work D:/PoB_work/ident_m981z --saveleak --tests-delta 12 --grown-ok landshare --ng --save 20261109
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m981_work/arms_start.txt
exit $RC
