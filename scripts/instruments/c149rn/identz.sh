#!/usr/bin/env bash
# (1401) c149rn 腕 Z（ident 段 2・器の木＝旗 0・別の worktree wt_c149rnz＝㉙ 走っている間に wt_c149rn へコミットしてよい）。宣言：計器の表 rnd を足した（--added-tables rnd）・テストの差 +6（RandomOnlyClimateEpidemicTests）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c149rn_work/identz.mark bash scripts/instruments/c149rn/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c149rn_work/tmpz TMP=D:/PoB_work/c149rn_work/tmpz
mkdir -p $TEMP
cd D:/PoB_work/wt_c149rnz || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c149rn_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c149rnz --tree D:/PoB_work/wt_c149rnz --work D:/PoB_work/ident_c149rn --saveleak --ng --save 20261127 --stage2 --added-tables rnd --tests-delta 6
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c149rn_work/arms_start.txt
exit $RC
