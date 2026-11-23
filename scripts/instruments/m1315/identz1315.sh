#!/usr/bin/env bash
# (1315) 合流（波136・m1308/identz1308.sh の写し）の腕 Z：木 wt_m1315z（合わせた木 m1315＝sv・tc・kb・ur〔器 U0 だけ〕＋写し・旗は全て既定）。
#   宣言：urxpd_s の列が末尾に 21 増える（ur の器 U0）・テストの差 +5（sv 3・tc 1・ur 1・kb 0）。段 2・PRUNE=0（新しい INSRAW keep_1315/base_ins の元）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1315_work/identz.mark bash scripts/instruments/m1315/identz1315.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1315_work/tmp TMP=D:/PoB_work/m1315_work/tmp PRUNE=0
cd D:/PoB_work/wt_m1315z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1315z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1315_work/arms_start.txt
bash D:/PoB_work/wt_m1315z/scripts/instruments/ident/ident.sh run m1315z --tree D:/PoB_work/wt_m1315z --work D:/PoB_work/ident_m1315z --saveleak --ng --save 20261123 --stage2 --tests-delta 5 --grown-ok urxpd_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1315_work/arms_start.txt
exit $RC
