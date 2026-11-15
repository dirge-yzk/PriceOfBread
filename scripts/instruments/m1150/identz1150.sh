#!/usr/bin/env bash
# (1150) 合流の腕 Z（m1143/identz1143.sh の写し・木を wt_m1150z〔合わせた木 H＋DefeatShockTensionRelief を 2.0 へ戻す 1 コミット〕へ）
#   宣言：足した表 rponset_s（cm）・dipcool_s（hy）・列が増えた表 rule_s（cm の 10 列）・trrail_s（cm の 1 列）・既知の揺れ corncf_s の 7 列（cm の CfForeignPort）・テストの差 +12（tt 1・cm 8・hy 3）・段 2（--stage2）・PRUNE は既定。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1150_work/identz.mark bash scripts/instruments/m1150/identz1150.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1150_work/tmp TMP=D:/PoB_work/m1150_work/tmp
cd D:/PoB_work/wt_m1150z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1150z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1150_work/arms_start.txt
bash D:/PoB_work/wt_m1150z/scripts/instruments/ident/ident.sh run m1150z --tree D:/PoB_work/wt_m1150z --work D:/PoB_work/ident_m1150z --saveleak --tests-delta 12 --ng --save 20261115 --stage2 --added-tables rponset_s,dipcool_s --grown-ok rule_s,trrail_s --known-extra corncf_s:cfDutyNow,corncf_s:cfDutyDown,corncf_s:cfDutyUp,corncf_s:cfPinRatio0,corncf_s:cfCornPrefDown2,corncf_s:cfStakeDown2,corncf_s:cfStakeDownTimesM2
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1150_work/arms_start.txt
exit $RC
