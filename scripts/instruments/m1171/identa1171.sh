#!/usr/bin/env bash
# (1171) 合流の段 1 の ident（m1164/identz1164.sh の型・木は wt_m1171〔c115qc＋c115sb を入れた木〕）
#   宣言：消えた表 colqn_s（qc）・足した表 mintreg_s（sb）・テストの差 −1（qc −4・sb +3）・段 2・PRUNE は既定。NOINS1171=1 で --no-ins。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1171_work/identa.mark bash scripts/instruments/m1171/identa1171.sh
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1171_work/tmp TMP=D:/PoB_work/m1171_work/tmp
cd D:/PoB_work/wt_m1171 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identA start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1171_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1171a --tree D:/PoB_work/wt_m1171 --work D:/PoB_work/ident_m1171a --saveleak ${NOINS1171:+--no-ins} --tests-delta -1 --ng --save 20261116 --stage2 --removed-tables colqn_s --added-tables mintreg_s
RC=$?
echo "identA end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1171_work/arms_start.txt
exit $RC
