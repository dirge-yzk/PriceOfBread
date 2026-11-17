#!/usr/bin/env bash
# (1191) 合流（m1184/identz1184.sh の写し・木と作業フォルダを wt_m1191・m1191_work へ）
# 腕 Z＝合わせた木 hc＋nc＋ur に、nc の 3 係数と ur の 2 係数を旧の既定へ戻す 1 コミット（wt_m1191z・push しない）。
#   宣言：テストの差 +6（nc の NationalConsciousnessReadersOffTests 3 本・ur の UnificationAiNoWaitTests 3 本・hc は 0）・段は stage.py・PRUNE は既定。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1191_work/identz.mark bash scripts/instruments/m1191/identz1191.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1191_work/tmp TMP=D:/PoB_work/m1191_work/tmp
cd D:/PoB_work/wt_m1191z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1191z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1191_work/arms_start.txt
bash D:/PoB_work/wt_m1191z/scripts/instruments/ident/ident.sh run m1191z --tree D:/PoB_work/wt_m1191z --work D:/PoB_work/ident_m1191z --saveleak --tests-delta 6 --ng --save 20261118
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1191_work/arms_start.txt
exit $RC
