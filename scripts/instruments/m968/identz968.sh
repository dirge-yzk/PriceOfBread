#!/usr/bin/env bash
# (968) 合流の腕 Z（合わせた木 oc＋sm＋lt の lt の旗 LandTaxFixedBasis を 0 にした使い捨ての木 wt_m968z）の恒等の判定（(961) の identz961.sh の型）。
#   宣言：cearn_s の末尾 2 列（sm）・landfix_s（lt の計器）・cpop_s:outOtherTop1Route（経路の行番号）・テスト +23（sm 4・lt 19）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m968_work/identz.mark bash scripts/instruments/m968/identz968.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m968z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m968/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m968_work/arms_start.txt
bash D:/PoB_work/wt_m968/scripts/instruments/ident/ident.sh run m968z --tree D:/PoB_work/wt_m968z --work D:/PoB_work/ident_m968z --saveleak --tests-delta 23 --grown-ok cearn_s --added-tables landfix_s --known-extra cpop_s:outOtherTop1Route --ng --save 20261108
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m968_work/arms_start.txt
exit $RC
