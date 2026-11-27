#!/usr/bin/env bash
# (1411) 合流の腕 Z（ident 段 2・合わせた木から c150rp の旗 RailProcureNational の既定を 0 へ戻した木 wt_m1411z）。宣言：追加の表 sinkgood（c150wz）・中身を書き換えた表 wexit_s（c150wz の検算の台本）・テストの差 +16（wz 11・rp 5＝合わせた木の --list-tests で数える）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1411_work/identz.mark bash scripts/instruments/m1411/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1411_work/tmp TMP=D:/PoB_work/m1411_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1411z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1411_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1411z --tree D:/PoB_work/wt_m1411z --work D:/PoB_work/ident_m1411z --saveleak --ng --save 20261127 --stage2 --added-tables sinkgood --rewritten-tables wexit_s@scripts/instruments/c150wz/wexit_rewrite_check.py --tests-delta ${TD:-16}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1411_work/arms_start.txt
exit $RC
