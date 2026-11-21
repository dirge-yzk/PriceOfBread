#!/usr/bin/env bash
# (1280) 合流（波131）の腕 Z（m1274/identz1274.sh の写し）：木 wt_m1280z（枝 m1280z＝合わせた木 T から挙動の線 pf・cr の旗を 0 へ戻した木〔各線の点火のコミットを戻した・WriterRatchetTests は第 1 段 9f22e90eb の版〕）。
#   宣言：新しい表 capskip_s（pf の計器）・テストの差 +13（pf 6・cr 7・第 1 段の ident.cfg 3078 に対する差＝★(1267)）。段 2（pf が計器 Instruments/ に触れる）。PRUNE は既定（1）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1280_work/identz.mark bash scripts/instruments/m1280/identz1280.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1280_work/tmp TMP=D:/PoB_work/m1280_work/tmp
cd D:/PoB_work/wt_m1280z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1280z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1280_work/arms_start.txt
bash D:/PoB_work/wt_m1280z/scripts/instruments/ident/ident.sh run m1280z --tree D:/PoB_work/wt_m1280z --work D:/PoB_work/ident_m1280z --saveleak --ng --save 20261121 --stage2 --tests-delta 13 --added-tables capskip_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1280_work/arms_start.txt
exit $RC
