#!/usr/bin/env bash
# (1171) 合流の腕 Z（m1164/identz1164.sh の型・木を wt_m1171z〔合わせた木 956a125ad＋SoldierVacancyFromZero を 0 へ戻す 1 コミット・push しない〕へ）
#   宣言：消えた表 colqn_s（qc）・足した表 mintreg_s（sb）・soldiervac_s（sv）・テストの差 +6（木の ident.cfg は段 1 の 2888＝sv の SoldierVacancyFromZeroTests 6）・段 2・PRUNE は既定。NOINSZ=1 で --no-ins。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1171_work/identz.mark bash scripts/instruments/m1171/identz1171.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1171_work/tmp TMP=D:/PoB_work/m1171_work/tmp
cd D:/PoB_work/wt_m1171z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1171z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1171_work/arms_start.txt
bash D:/PoB_work/wt_m1171z/scripts/instruments/ident/ident.sh run m1171z --tree D:/PoB_work/wt_m1171z --work D:/PoB_work/ident_m1171z --saveleak ${NOINSZ:+--no-ins} --tests-delta 6 --ng --save 20261116 --stage2 --removed-tables colqn_s --added-tables mintreg_s,soldiervac_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1171_work/arms_start.txt
exit $RC
