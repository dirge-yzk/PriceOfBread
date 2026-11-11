#!/usr/bin/env bash
# (1039) c98da の恒等の判定：旗 0 の木（この worktree の HEAD）を基線 m1027 の成績表と突き合わせる。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c98da_work/ident.mark bash scripts/instruments/c98da/ident1039.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c98da || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "ident start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c98da_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c98da --tree D:/PoB_work/wt_c98da --work D:/PoB_work/c98da_work/ident --saveleak --tests-delta __DELTA__ --ng --save 20261111 --grown-ok lordstat_s
RC=$?
echo "ident end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c98da_work/arms_start.txt
exit $RC
