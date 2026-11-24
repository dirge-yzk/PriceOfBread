#!/usr/bin/env bash
# (1342) c141k59 腕 Z の腕 b（計器つき 10 種）だけを回し直して判定する（1 回目の腕 b は 20:40 に D: の空きが尽きて rc=127＝他の腕 a・t・s は済み）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c141k59_work/identb.mark bash scripts/instruments/c141k59/identb.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c141k59_work/tmp TMP=D:/PoB_work/c141k59_work/tmp IDENT_SP=D:/PoB_work/wt_c141k59/scripts/instruments/ident
W=D:/PoB_work/ident_c141k59; mkdir -p $TEMP
cd D:/PoB_work/wt_c141k59 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ b again start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c141k59_work/arms_start.txt
bash $W/ident_run.sh _arm b c141k59z /d/PoB_work/wt_c141k59 $W ""
echo "b: $(cat $W/marks/b.done)"
bash $W/ident_run.sh check c141k59z --work $W --tree /d/PoB_work/wt_c141k59 --saveleak --ng --save 20261124 --tests-delta 3 | tee $W/check_out.txt
RC=${PIPESTATUS[0]}
echo "identZ b again end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c141k59_work/arms_start.txt
exit $RC
