#!/usr/bin/env bash
# (1321) 合流（波137・m1308/identz1308.sh の写し）の腕 Z：木 wt_m1321z（枝 m1321z＝合わせた木 m1321 から ak の点火 e391f1c2d を戻した木＝旗 SecessionTreasuryByMoney 0）。
#   宣言：新しい表 sectreas_s（ak）・列が末尾に増える subst_s（s3 の +13）・popclose_s（sm の +3）・テストの差 +21（sm 6・s3 6・ak 9）。段 2・PRUNE=1。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1321_work/identz.mark bash scripts/instruments/m1321/identz1321.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1321_work/tmp TMP=D:/PoB_work/m1321_work/tmp
cd D:/PoB_work/wt_m1321z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1321z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1321_work/arms_start.txt
bash D:/PoB_work/wt_m1321z/scripts/instruments/ident/ident.sh run m1321z --tree D:/PoB_work/wt_m1321z --work D:/PoB_work/ident_m1321z --saveleak --ng --save 20261123 --stage2 --tests-delta 21 --added-tables sectreas_s --grown-ok subst_s,popclose_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1321_work/arms_start.txt
exit $RC
