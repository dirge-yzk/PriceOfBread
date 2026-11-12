#!/usr/bin/env bash
# (1069) 合流の腕 Z（m1053/identz1053.sh の写し・木を wt_m1069z〔vs の点火 5c4409d47 を逆適用＝DependencyCycleBreak 0〕へ）
#   宣言：足した表 dep_s（vs）・テスト +60（sel 2・uf 50・vs 8）。PRUNE=0（hashZ の後に刈る）・POB_INSTRUMENTS_TABLES は外す（全表）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1069_work/identz.mark bash scripts/instruments/m1069/identz1069.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
cd D:/PoB_work/wt_m1069z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1069/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1069_work/arms_start.txt
bash D:/PoB_work/wt_m1069/scripts/instruments/ident/ident.sh run m1069z --tree D:/PoB_work/wt_m1069z --work D:/PoB_work/ident_m1069z --saveleak --tests-delta 60 --ng --save 20261112 --added-tables dep_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1069_work/arms_start.txt
exit $RC
