#!/usr/bin/env bash
# (1110) 合流の腕 Z（m1096/identz1096.sh の写し・木を wt_m1110z〔合わせた木 dc＋ui＋im＋cf・挙動の線なし＝旗の切り替えなし〕へ）
#   宣言：足した表と列 0・テスト +15（ui 14・im 1）。既定の刈り込み（INSRAW は keep_1103/base_ins のまま＝新しい keep を作らない）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1110_work/identz.mark bash scripts/instruments/m1110/identz1110.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
cd D:/PoB_work/wt_m1110z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1110z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1110_work/arms_start.txt
bash D:/PoB_work/wt_m1110z/scripts/instruments/ident/ident.sh run m1110z --tree D:/PoB_work/wt_m1110z --work D:/PoB_work/ident_m1110z --saveleak --tests-delta 15 --ng --save 20261113
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1110_work/arms_start.txt
exit $RC
