#!/usr/bin/env bash
# (1001) 合流（identz988.sh の写し・腕 Z＝合わせた木で LandShareTaxSale を 0 にした使い捨ての木 wt_m1001z・宣言はテスト +3〔c91lc の T8〜T10〕だけ・足した表 0）
# (988) 合流の腕 Z（dd の TakeFloorPositive を 0 にした使い捨ての木 wt_m1001z）の恒等の判定（identz981.sh の写し）。
#   宣言：表 landsale_s（ls の計器・新）・テスト +20（ls 7・dd 13・tb 0）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1001_work/identz.mark bash scripts/instruments/m988/identz988.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1001z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1001/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1001_work/arms_start.txt
bash D:/PoB_work/wt_m1001/scripts/instruments/ident/ident.sh run m1001z --tree D:/PoB_work/wt_m1001z --work D:/PoB_work/ident_m1001z --saveleak --tests-delta 3 --ng --save 20261109
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1001_work/arms_start.txt
exit $RC
