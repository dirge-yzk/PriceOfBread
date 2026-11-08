#!/usr/bin/env bash
# (975) 合流の腕 Z（mt の MoTrendInitNeedsGdp と mw の MilitaryWarMultFromContact を 0 にした使い捨ての木 wt_m975z）の恒等の判定（identz968.sh の写し）。
# (968) 合流の腕 Z（合わせた木 oc＋sm＋lt の lt の旗 LandTaxFixedBasis を 0 にした使い捨ての木 wt_m968z）の恒等の判定（(961) の identz961.sh の型）。
#   宣言：basisx_s（bx の計器）・pinit_s（mt の計器）・cwmult_s の末尾 5 列（mw の器の計器）・テスト +18（bx 11・mt 5・mw 2）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m975_work/identz.mark bash scripts/instruments/m975/identz975.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m975z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m975/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m975_work/arms_start.txt
bash D:/PoB_work/wt_m975/scripts/instruments/ident/ident.sh run m975z --tree D:/PoB_work/wt_m975z --work D:/PoB_work/ident_m975z --saveleak --tests-delta 18 --grown-ok cwmult --added-tables basisx,pinit --ng --save 20261109
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m975_work/arms_start.txt
exit $RC
