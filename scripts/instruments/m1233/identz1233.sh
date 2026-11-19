#!/usr/bin/env bash
# (1233) 合流の腕 Z（m1226/identz1226.sh の型）：木 wt_m1233z（a1b31ac02＝kb＋bk の器と窓の直しと段 0 の追記〔旗 PaySalesToProducerBook の既定 0〕）。
#   宣言：bk の足した表 paysale_s＋テストの差 +7（kb 2・bk 5）。PRUNE は既定（1）＝新しい INSRAW は木 T の M の生。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1233_work/identz.mark bash scripts/instruments/m1233/identz1233.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1233_work/tmp TMP=D:/PoB_work/m1233_work/tmp
cd D:/PoB_work/wt_m1233z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1233z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1233_work/arms_start.txt
bash D:/PoB_work/wt_m1233z/scripts/instruments/ident/ident.sh run m1233z --tree D:/PoB_work/wt_m1233z --work D:/PoB_work/ident_m1233z --saveleak --ng --save 20261119 --tests-delta 7 --added-tables paysale_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1233_work/arms_start.txt
exit $RC
