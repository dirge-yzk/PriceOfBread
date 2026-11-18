#!/usr/bin/env bash
# (1226) 合流の腕 Z（m1219/identz1219.sh の型）：木 wt_m1226z（6df4ae27b＝cu＋tc＋楔の試験の直し＋k3 の器〔旗 HealthcareLiteracyGateOff の既定 0〕）。
#   宣言：cu の check_cmd.sh（measurements/m1220_20261119/check_cmd.sh）と同じ 31 列＋テストの差 +6（cu 2・tc 1・k3 3）。PRUNE は既定（1）＝新しい INSRAW は木 T の M の生。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1226_work/identz.mark bash scripts/instruments/m1226/identz1226.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1226_work/tmp TMP=D:/PoB_work/m1226_work/tmp
cd D:/PoB_work/wt_m1226z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1226z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1226_work/arms_start.txt
bash D:/PoB_work/wt_m1226z/scripts/instruments/ident/ident.sh run m1226z --tree D:/PoB_work/wt_m1226z --work D:/PoB_work/ident_m1226z --saveleak --ng --save 20261119 --tests-delta 6 --known-extra "rulepeace_s:greatCircleOnlyTakenCount,rulepeace_s:phiStarMedianIndex,rulepeace_s:takenCount,rulepeace_s:takenPopShare,rulepower_s:bestClass,rulepower_s:classSumShare,rulepower_s:fForeignRawIndex,rulepower_s:fForeignSumIndex,rulepower_s:fForeignTopIndex,rulepower_s:fResistIndex,rulepower_s:foreignSideShare,rulepower_s:foreignTopKey,rulepower_s:iProbeShare,rulepower_s:interShare,rulepower_s:localSideShare,rulepower_s:navalAllocIndex,rulepower_s:newRule0Share,rulepower_s:newRule1Share,rulepower_s:openDiffIndex,rulepower_s:openQualFlag,rulepower_s:ownerSideShare,rulepower_s:ownerSideShareR,rulepower_s:ownerWeightShare,rulepower_s:phiStarIndex,rulepower_s:pressureIndex,rulepower_s:ungovSideShare,rulepower_s:ungovSideShareR,rulepoweralloc_s:allocIndex,rulepoweralloc_s:bShare,rulepoweralloc_s:fForeignIndex,rulepoweralloc_s:wgtIndex"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1226_work/arms_start.txt
exit $RC
