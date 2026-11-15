#!/usr/bin/env bash
# (1157) 合流の腕 Z（m1150/identz1150.sh の写し・木を wt_m1157z〔合わせた木 b39fe1ce4＋RailLocoGateWorld を 0 へ戻す 1 コミット 8f7d415b3・push しない〕へ）
#   宣言：列が増えた表 railspend_s（lc の 2 列）・テストの差 +4（lc 5・sw −1）・段 2（--stage2）・PRUNE は既定。sw は宣言なし（dipcool_s は定数の移しだけ・trrail_s の見出しは同じ字）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1157_work/identz.mark bash scripts/instruments/m1157/identz1157.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1157_work/tmp TMP=D:/PoB_work/m1157_work/tmp
cd D:/PoB_work/wt_m1157z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1157z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1157_work/arms_start.txt
bash D:/PoB_work/wt_m1157z/scripts/instruments/ident/ident.sh run m1157z --tree D:/PoB_work/wt_m1157z --work D:/PoB_work/ident_m1157z --saveleak --tests-delta 4 --ng --save 20261115 --stage2 --grown-ok railspend_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1157_work/arms_start.txt
exit $RC
