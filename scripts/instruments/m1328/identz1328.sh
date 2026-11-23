#!/usr/bin/env bash
# (1328) 合流（波138・m1321/identz1321.sh の写し）の腕 Z：木 wt_m1328z（枝 m1328z＝合わせた木 m1328 から lp の点火 af91be0ac と df の点火 f47147834 を戻した木＝旗 LawPrefSinglePeaked 0・ArrearsOnePredicate 0）。
#   宣言：新しい表 lawdir_s（lp）・列が末尾に増える capskip_s・region_s・arrears_s（sm）・テストの差 +20（sm 6・lp 14・df 0）。段 2・PRUNE=1。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1328_work/identz.mark bash scripts/instruments/m1328/identz1328.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1328_work/tmp TMP=D:/PoB_work/m1328_work/tmp
cd D:/PoB_work/wt_m1328z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1328z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1328_work/arms_start.txt
bash D:/PoB_work/wt_m1328z/scripts/instruments/ident/ident.sh run m1328z --tree D:/PoB_work/wt_m1328z --work D:/PoB_work/ident_m1328z --saveleak --ng --save 20261123 --stage2 --tests-delta 20 --added-tables lawdir_s --grown-ok capskip_s,region_s,arrears_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1328_work/arms_start.txt
exit $RC
