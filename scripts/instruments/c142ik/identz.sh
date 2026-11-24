#!/usr/bin/env bash
# (1351) c142ik の腕 Z（段 2）。bash scripts/instruments/tools/bg.sh D:/PoB_work/c142ik_work/identz.mark bash scripts/instruments/c142ik/identz.sh
cd D:/PoB_work/wt_c142ik || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c142ik_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c142ikz --tree D:/PoB_work/wt_c142ik --work D:/PoB_work/ident_c142ik --saveleak --ng --save 20261125 --stage2 --tests-delta 5 --added-tables femp_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c142ik_work/arms_start.txt
exit $RC
