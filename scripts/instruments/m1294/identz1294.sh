#!/usr/bin/env bash
# (1294) 合流（波133・m1287/identz1287.sh の写し）の腕 Z：木 wt_m1294z（枝 m1294z＝合わせた木 m1294 から mr の点火 01d2e25da〔p08 の旗 1 行・DormantIdentityTests の 1 行〕を戻した木）。
#   宣言：新しい表 labeltier_s（mr）・cwmult_s の 5 列が消える（cl）・.cost の DiscoveryWorldScans（wa）・テストの差 +15（wa +4・cl +5・mr +6）。段 2（計器 Instruments/ に触れる）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1294_work/identz.mark bash scripts/instruments/m1294/identz1294.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1294_work/tmp TMP=D:/PoB_work/m1294_work/tmp
cd D:/PoB_work/wt_m1294z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1294z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1294_work/arms_start.txt
bash D:/PoB_work/wt_m1294z/scripts/instruments/ident/ident.sh run m1294z --tree D:/PoB_work/wt_m1294z --work D:/PoB_work/ident_m1294z --saveleak --ng --save 20261122 --stage2 --tests-delta 15 --added-tables labeltier_s --removed-cols cwmult_s:warMultOldRuleCivilSumIndex,cwmult_s:warMultNewRuleCivilSumIndex,cwmult_s:warMultNewRuleForeignSumIndex,cwmult_s:oldRuleMismatchMonthsCount,cwmult_s:newRuleMismatchMonthsCount --cost-allow DiscoveryWorldScans
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1294_work/arms_start.txt
exit $RC
