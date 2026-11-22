#!/usr/bin/env bash
# (1301) 合流（波134・m1294/identz1294.sh の写し）の腕 Z：合わせた木 wt_m1301 そのもの（挙動の線を入れていない＝旗の戻しは無い）。
#   宣言：新しい表 treasledger_s（tk の項 A）・テストの差 +5（tk の T1〜T4・T6）。段 2（計器 Instruments/ に触れる）。PRUNE=0（生を新しい INSRAW keep_1301/base_ins に使う）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1301_work/identz.mark bash scripts/instruments/m1301/identz1301.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1301_work/tmp TMP=D:/PoB_work/m1301_work/tmp PRUNE=0
cd D:/PoB_work/wt_m1301 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1301/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1301_work/arms_start.txt
bash D:/PoB_work/wt_m1301/scripts/instruments/ident/ident.sh run m1301z --tree D:/PoB_work/wt_m1301 --work D:/PoB_work/ident_m1301z --saveleak --ng --save 20261122 --stage2 --tests-delta 5 --added-tables treasledger_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1301_work/arms_start.txt
exit $RC
