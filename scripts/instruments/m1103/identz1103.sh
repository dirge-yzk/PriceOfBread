#!/usr/bin/env bash
# (1103) 合流の腕 Z（m1087/identz1087.sh の写し・木を wt_m1103z〔合わせた木 H＋旗の既定を 0 へ戻す 1 コミット＝使い捨ての枝 m1103z・main へは入れない〕へ）
#   宣言：足した表 armsfill_s（ix）・trrail（rr）・列の増えた表 trnation（rr）・.cost の〈清算：距離〉の 2 行（rr）・テストの差は合わせた木の数（+18）。PRUNE=0・全表。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1103_work/identz.mark bash scripts/instruments/m1103/identz1103.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
cd D:/PoB_work/wt_m1103z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1103z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1103_work/arms_start.txt
bash D:/PoB_work/wt_m1103z/scripts/instruments/ident/ident.sh run m1103z --tree D:/PoB_work/wt_m1103z --work D:/PoB_work/ident_m1103z --saveleak --tests-delta 18 --ng --save 20261113 --added-tables armsfill_s,trrail --grown-ok trnation --cost-allow "清算：距離"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1103_work/arms_start.txt
exit $RC
