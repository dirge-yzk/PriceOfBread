#!/usr/bin/env bash
# (1136) 合流の腕 Z（m1129/identz1129.sh の写し・木を wt_m1136z〔lo を外した合わせた木 tb＋sm を detached で＝旗の戻しは要らない〕へ）
#   宣言：足した表 tb_s（c110tb）・テストの差 +4（tb 5・sm −1）・段 2（--stage2）・PRUNE は既定（判定: 恒等なら一致した表を刈る）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1136_work/identz.mark bash scripts/instruments/m1136/identz1136.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1136_work/tmp TMP=D:/PoB_work/m1136_work/tmp
cd D:/PoB_work/wt_m1136z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1136z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1136_work/arms_start.txt
bash D:/PoB_work/wt_m1136z/scripts/instruments/ident/ident.sh run m1136z --tree D:/PoB_work/wt_m1136z --work D:/PoB_work/ident_m1136z --saveleak --tests-delta 4 --ng --save 20261114 --stage2 --added-tables tb_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1136_work/arms_start.txt
exit $RC
