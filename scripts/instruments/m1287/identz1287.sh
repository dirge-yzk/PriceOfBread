#!/usr/bin/env bash
# (1287) 合流（波132・m1280/identz1280.sh の写し）の腕 Z：木 wt_m1287z（枝 m1287z＝合わせた木 T から挙動の線 fg・df の点火のコミット 7a8babcdd・0cca4e5de の SimCore と DormantIdentityTests の行を戻した木・WriterRatchetTests は基点 fa4653318 の版＝★(1274)）。
#   宣言：新しい表 arrears_s（df の計器）・テストの差 +2（kx −10・g2 +4・fg +4・df +4・ident.cfg の 3091 に対する差）。段 2（df・fg が計器 Instruments/ に触れる）。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1287_work/identz.mark bash scripts/instruments/m1287/identz1287.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1287_work/tmp TMP=D:/PoB_work/m1287_work/tmp
cd D:/PoB_work/wt_m1287z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1287z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1287_work/arms_start.txt
bash D:/PoB_work/wt_m1287z/scripts/instruments/ident/ident.sh run m1287z --tree D:/PoB_work/wt_m1287z --work D:/PoB_work/ident_m1287z --saveleak --ng --save 20261122 --stage2 --tests-delta 2 --added-tables arrears_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1287_work/arms_start.txt
exit $RC
