#!/usr/bin/env bash
# (1053) 合流の腕 Z（m1044/identz1044.sh の写し・木を wt_m1053z〔LordReligionStake 0・LordIntermediaryStake 0〕へ）
#   宣言：列が増えた表 tradeskip_s（cd +16）・sellr_s（cd +1）・lordstat_s（db +2）・cd の known-extra 3 本（C4a の区分の列＝値は不変の見込み）・テスト +16（cd 9・db 7）・.cost の例外は cd の台帳の行〈労働の重み〉。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1053_work/identz.mark bash scripts/instruments/m1053/identz1053.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_m1053z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1053/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1053_work/arms_start.txt
bash D:/PoB_work/wt_m1053/scripts/instruments/ident/ident.sh run m1053z --tree D:/PoB_work/wt_m1053z --work D:/PoB_work/ident_m1053z --saveleak --tests-delta 16 --ng --save 20261112 --grown-ok tradeskip_s,sellr_s,lordstat_s --known-extra tradeskip:sogStockLeTarget,tradeskip:sogSellAcc0,tradeskip:sogMilitary --cost-allow "労働の重み"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1053_work/arms_start.txt
exit $RC
