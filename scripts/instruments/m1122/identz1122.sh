#!/usr/bin/env bash
# (1122) 合流の腕 Z（m1116/identz1116.sh・m1103/identz1103.sh の写し・木を wt_m1122z〔合わせた木 H＋旗の既定を 0 へ戻す 1 コミット＝使い捨ての枝 m1122z・main へは入れない〕へ）
#   宣言：足した表 parlseat_s（ps）・テストの差は合わせた木の数（+7＝sm 3・ps 3・mc 1）・.cost の行は増えない。PRUNE は既定（判定: 恒等なら一致した表を刈る）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1122_work/identz.mark bash scripts/instruments/m1122/identz1122.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1122_work/tmp TMP=D:/PoB_work/m1122_work/tmp
cd D:/PoB_work/wt_m1122z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1122z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1122_work/arms_start.txt
bash D:/PoB_work/wt_m1122z/scripts/instruments/ident/ident.sh run m1122z --tree D:/PoB_work/wt_m1122z --work D:/PoB_work/ident_m1122z --saveleak --tests-delta 7 --ng --save 20261114 --added-tables parlseat_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1122_work/arms_start.txt
exit $RC
