#!/usr/bin/env bash
# (994) 合流の腕 Z＝合わせた木 wt_m994 そのもの（挙動の線の旗は全て 0）の恒等の判定（identz988.sh の写し）。
#   宣言：landsale_s の末尾 11 列（c90sl の計器）・テスト +16（px 5・sl 11・tr 0 の予言）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m994_work/identz.mark bash scripts/instruments/m994/identz994.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m994 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m994/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m994_work/arms_start.txt
bash D:/PoB_work/wt_m994/scripts/instruments/ident/ident.sh run m994z --tree D:/PoB_work/wt_m994 --work D:/PoB_work/ident_m994z --saveleak --tests-delta 16 --grown-ok landsale --ng --save 20261109
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m994_work/arms_start.txt
exit $RC
