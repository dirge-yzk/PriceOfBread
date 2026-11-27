#!/usr/bin/env bash
# (1425) 合流の腕 Z（ident 段 2・合わせた木から MilitarismStrengthBonus を 1.3 へ戻した木 wt_m1425z）。宣言：wr の trrail_s（末尾 12 列）・wexit_s・sinkgood_s（wr の検算の台本）・テストの差 +16（ht 0・wr +13・ms +3）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1425_work/identz.mark bash scripts/instruments/m1425/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1425_work/tmp TMP=D:/PoB_work/m1425_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1425z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1425_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1425z --tree D:/PoB_work/wt_m1425z --work D:/PoB_work/ident_m1425z --saveleak --ng --save 20261128 --stage2 --grown-ok trrail_s --rewritten-tables wexit_s@scripts/instruments/c152wr/wexit_check3.py,sinkgood_s@scripts/instruments/c152wr/sinkgood_check.py --tests-delta ${TD:-16}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1425_work/arms_start.txt
exit $RC
