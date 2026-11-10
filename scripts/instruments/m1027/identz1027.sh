#!/usr/bin/env bash
# (1027) 合流の腕 Z（m1022/identz1022.sh の写し・木を wt_m1027z〔ResourceRowsFix 0〕へ）
#   宣言：足した表 warentry_s・warpair_s・warband_s（c96wn）・cscx_s・pxstep_s（c96kf）・列が増えた表 wars_s・invade_s（c96wn）・merch_s（c96kf）・テスト +35（wn 18・kf 10・rd 7）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1027_work/identz.mark bash scripts/instruments/m1027/identz1027.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1027z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1027/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1027_work/arms_start.txt
bash D:/PoB_work/wt_m1027/scripts/instruments/ident/ident.sh run m1027z --tree D:/PoB_work/wt_m1027z --work D:/PoB_work/ident_m1027z --saveleak --tests-delta 35 --ng --save 20261110 --added-tables warentry_s,warpair_s,warband_s,cscx_s,pxstep_s --grown-ok wars_s,invade_s,merch_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1027_work/arms_start.txt
exit $RC
