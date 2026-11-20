#!/usr/bin/env bash
# (1274) 合流（波130）の腕 Z（m1267/identz1267.sh の写し）：木 wt_m1274z（枝 m1274z＝合わせた木 T から挙動の線 k15・rc・gs の旗を 0 へ戻した木〔各線の点火のコミットを戻した・WriterRatchetTests は基点 3febe202c の版〕）。
#   宣言：新しい表 0・テストの差 +16（ih 5・k15 3・rc 4・gs 4）。段 2（ih が計器のフックに触れる）。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1274_work/identz.mark bash scripts/instruments/m1274/identz1274.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1274_work/tmp TMP=D:/PoB_work/m1274_work/tmp
cd D:/PoB_work/wt_m1274z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1274z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1274_work/arms_start.txt
bash D:/PoB_work/wt_m1274z/scripts/instruments/ident/ident.sh run m1274z --tree D:/PoB_work/wt_m1274z --work D:/PoB_work/ident_m1274z --saveleak --ng --save 20261121 --stage2 --tests-delta 16
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1274_work/arms_start.txt
exit $RC
