#!/usr/bin/env bash
# (1377) c146wy 腕 Z（ident 段 1・項 2 の木＝旗 0・別の worktree wt_c146wyz＝㉙ 走っている間に wt_c146wy へコミットしてよい）。宣言：計器の列は足さない・テストの差 +2（WarWearyPoliticsCutTests の T5 の 2 件）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c146wy_work/identz.mark bash scripts/instruments/c146wy/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c146wy_work/tmpz TMP=D:/PoB_work/c146wy_work/tmpz
mkdir -p $TEMP
cd D:/PoB_work/wt_c146wyz || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c146wy_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c146wyz --tree D:/PoB_work/wt_c146wyz --work D:/PoB_work/ident_c146wy --saveleak --ng --save 20261126 --tests-delta 2
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c146wy_work/arms_start.txt
exit $RC
