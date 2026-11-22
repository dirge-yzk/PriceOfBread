#!/usr/bin/env bash
# (1308) 合流（波135・m1294/identz1294.sh の写し）の腕 Z：木 wt_m1308z（枝 m1308z＝合わせた木 m1308 から nc の点火 0e1b9e8bd と k47 の点火 9e54c5b37 を戻した木＝旗 2 本とも 0）。
#   宣言：新しい表 capcol_s（nc）・treasledger_s の行が増える（tl の _bornTreasury）・arrears_s は検算の台本で（ar）・テストの差 +10（nc 8・k47 2＝第 1 段の ident.cfg 3113 に対する差・★(1267)）。段 2・PRUNE=1。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1308_work/identz.mark bash scripts/instruments/m1308/identz1308.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1308_work/tmp TMP=D:/PoB_work/m1308_work/tmp
cd D:/PoB_work/wt_m1308z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1308z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1308_work/arms_start.txt
bash D:/PoB_work/wt_m1308z/scripts/instruments/ident/ident.sh run m1308z --tree D:/PoB_work/wt_m1308z --work D:/PoB_work/ident_m1308z --saveleak --ng --save 20261123 --stage2 --tests-delta 10 --added-tables capcol_s --rows-grown-ok treasledger_s --rewritten-tables arrears_s@scripts/instruments/c135ar/arrears_recheck.py
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1308_work/arms_start.txt
exit $RC
