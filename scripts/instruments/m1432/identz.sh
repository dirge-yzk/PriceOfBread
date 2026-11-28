#!/usr/bin/env bash
# (1432) 合流の腕 Z（ident 段 2・合わせた木から rg の 2 係数を 0.03・0.06 へ・NoContactCollapseBlank を 0 へ戻した木 wt_m1432z）。宣言：nx の wexit_s（末尾 7 列）・テストの差 +8（tp 0・fb −1・rg +4・nx +5）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1432_work/identz.mark bash scripts/instruments/m1432/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1432_work/tmp TMP=D:/PoB_work/m1432_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1432z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1432_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1432z --tree D:/PoB_work/wt_m1432z --work D:/PoB_work/ident_m1432z --saveleak --ng --save 20261128 --stage2 --grown-ok wexit_s --tests-delta ${TD:-8}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1432_work/arms_start.txt
exit $RC
