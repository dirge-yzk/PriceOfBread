#!/usr/bin/env bash
# (1078) 合流の腕 Z（m1069/identz1069.sh の写し・木を wt_m1078z〔合わせた木 ui＋rp＋em・挙動の線なし＝旗の切り替えなし〕へ）
#   宣言：足した表 rulepower_s・rulepeace_s（rp）・テスト +20（ui 1・rp 4・em 15）・.cost の件数だけの行（em）。PRUNE=0（INSRAW の keep_1078 を作るため）・POB_INSTRUMENTS_TABLES は外す（全表）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1078_work/identz.mark bash scripts/instruments/m1078/identz1078.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
cd D:/PoB_work/wt_m1078z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1078z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1078_work/arms_start.txt
bash D:/PoB_work/wt_m1078z/scripts/instruments/ident/ident.sh run m1078z --tree D:/PoB_work/wt_m1078z --work D:/PoB_work/ident_m1078z --saveleak --tests-delta 20 --ng --save 20261112 --added-tables rulepower_s,rulepeace_s --cost-allow "中身の無い物：,消費：飛ばした,消費：人口 0"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1078_work/arms_start.txt
exit $RC
