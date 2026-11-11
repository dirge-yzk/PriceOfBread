#!/usr/bin/env bash
# (933) 合流の腕 Z（T_Z＝c81sm と c81im を入れた木 9a640102・挙動の線の変更を含まない＝wt_m933z）の恒等の判定（identz926.sh の型）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m933/identz.mark bash scripts/instruments/m933/identz933.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m933z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m933/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m933/arms_start.txt
bash D:/PoB_work/wt_m933/scripts/instruments/ident/ident.sh run m933z --tree D:/PoB_work/wt_m933z --work D:/PoB_work/ident_m933z --saveleak --added-tables imp_s --grown-ok crownpc_s --known-extra cearn_s:heirDebtSettledPerQuarter --tests-delta 6 --ng --save 20261107
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m933/arms_start.txt
exit $RC
