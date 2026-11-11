#!/usr/bin/env bash
# (1044) 合流の腕 Z（m1027/identz1027.sh の写し・木を wt_m1044z〔LaborWeightsByPower 0〕へ）
#   宣言：足した表 sellr_s（lc）・lwk_s（lw）・列が増えた表 tradeskip_s（lc）・lordstat_s（da）・テスト +22（lc 9・da 6・lw 7）・.cost の件数の行 清算：着値の下界・清算：着値の充填（c97sp の台帳〔★(1035)〕と lc の台帳の定義の直し）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1044_work/identz.mark bash scripts/instruments/m1044/identz1044.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1044z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1044/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1044_work/arms_start.txt
bash D:/PoB_work/wt_m1044/scripts/instruments/ident/ident.sh run m1044z --tree D:/PoB_work/wt_m1044z --work D:/PoB_work/ident_m1044z --saveleak --tests-delta 22 --ng --save 20261111 --added-tables sellr_s,lwk_s --grown-ok tradeskip_s,lordstat_s --cost-allow "清算：着値の下界,清算：着値の充填"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1044_work/arms_start.txt
exit $RC
