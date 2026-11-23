#!/usr/bin/env bash
# (1334) 合流（波139・m1328/identz1328.sh の写し）の腕 Z：木 wt_m1334z（枝 m1334z＝合わせた木 m1334 から nc の点火 20d8707f4 を戻した木＝旗 NextCapitalCoreFirst 0）。
#   宣言：列が末尾に増える wars_s（sl）・capcol_s（nc）・テストの差 +8（sm 1・sl 1・nc 6）。段 2・PRUNE=1。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1334_work/identz.mark bash scripts/instruments/m1334/identz1334.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1334_work/tmp TMP=D:/PoB_work/m1334_work/tmp
cd D:/PoB_work/wt_m1334z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1334z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1334_work/arms_start.txt
bash D:/PoB_work/wt_m1334z/scripts/instruments/ident/ident.sh run m1334z --tree D:/PoB_work/wt_m1334z --work D:/PoB_work/ident_m1334z --saveleak --ng --save 20261124 --stage2 --tests-delta 8 --grown-ok wars_s,capcol_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1334_work/arms_start.txt
exit $RC
