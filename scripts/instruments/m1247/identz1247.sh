#!/usr/bin/env bash
# (1247) 合流の腕 Z（m1247/identz1247.sh の型）：木 wt_m1247z（4fa75ffbc＝rk の全部＋pf の fbc703495 まで〔旗 CapSkipSellerPotential の既定 0〕）。
#   宣言：rk の足した表 reach_s・paysale2_s（paysale_s を消した）・列が増えた nation_s・rulepeace_s＋pf の capskip_s＋テストの差 +12（rk 8・pf 4）。段 2。PRUNE は既定（1）＝新しい INSRAW は木 T の M の生。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1247_work/identz.mark bash scripts/instruments/m1247/identz1247.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1247_work/tmp TMP=D:/PoB_work/m1247_work/tmp
cd D:/PoB_work/wt_m1247z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1247z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1247_work/arms_start.txt
bash D:/PoB_work/wt_m1247z/scripts/instruments/ident/ident.sh run m1247z --tree D:/PoB_work/wt_m1247z --work D:/PoB_work/ident_m1247z --saveleak --ng --save 20261120 --stage2 --tests-delta 12 --added-tables reach_s,paysale2_s,capskip_s --removed-tables paysale_s --grown-ok nation_s,rulepeace_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1247_work/arms_start.txt
exit $RC
