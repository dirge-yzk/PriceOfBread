#!/usr/bin/env bash
# (1129) 合流の腕 Z（m1122/identz1122.sh の写し・木を wt_m1129〔合わせた木 H＝4c6a75df3・挙動の旗は無い＝H そのもの〕へ）
#   宣言：足した表 s4cost_s・s7staple_s（t0）・末尾に列が増えた表 prodcap_s（t0）・値の変わる列 trrail_s の 3 列（rs）・テストの差 +7（t0 5・rs 2）・.cost の行は増えない。
#   PRUNE=0（keep_1129/base_ins を Z の生から作る）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1129_work/identz.mark bash scripts/instruments/m1129/identz1129.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1129_work/tmp TMP=D:/PoB_work/m1129_work/tmp PRUNE=0
cd D:/PoB_work/wt_m1129 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1129/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1129_work/arms_start.txt
bash D:/PoB_work/wt_m1129/scripts/instruments/ident/ident.sh run m1129z --tree D:/PoB_work/wt_m1129 --work D:/PoB_work/ident_m1129z --saveleak --tests-delta 7 --ng --save 20261114 --added-tables s4cost_s,s7staple_s --grown-ok prodcap_s --known-extra trrail_s:railFreightInsGoldPerYear,trrail_s:g4Moves,trrail_s:g4Viol
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1129_work/arms_start.txt
exit $RC
