#!/usr/bin/env bash
# (1096) 合流の腕 Z（m1087/identz1087.sh の写し・木を wt_m1096z〔合わせた木 ui＋fb＋cp＋si・挙動の線なし＝旗の切り替えなし〕へ）
#   宣言：足した表 center_s・poppref_s（cp）・rulepoweralloc_s・minecap_s（si）・列の増えた表 rulepower_s（si の末尾 10 列）・テスト +22（fb 4・cp 13・si 5）。PRUNE=0・全表。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1096_work/identz.mark bash scripts/instruments/m1096/identz1096.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
cd D:/PoB_work/wt_m1096z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1096z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1096_work/arms_start.txt
bash D:/PoB_work/wt_m1096z/scripts/instruments/ident/ident.sh run m1096z --tree D:/PoB_work/wt_m1096z --work D:/PoB_work/ident_m1096z --saveleak --tests-delta 22 --ng --save 20261113 --added-tables center_s,poppref_s,rulepoweralloc_s,minecap_s --grown-ok rulepower_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1096_work/arms_start.txt
exit $RC
