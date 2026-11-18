#!/usr/bin/env bash
# (1219) 合流の腕 Z（m1212/identz1212.sh の型）：木 wt_m1219（ts＋T1 の戻し＋cr）＝挙動の線は入れていない。
#   宣言：cr の check_cmd.sh（measurements/m1214_20261118/ident/check_cmd.sh）と同じ＝テストの差 +3・消した列 7・列の増えた表 2・既知の揺れ 37 列。PRUNE=0（生を keep_1219/base_ins へ移す）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1219_work/identz.mark bash scripts/instruments/m1219/identz1219.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1219_work/tmp TMP=D:/PoB_work/m1219_work/tmp
cd D:/PoB_work/wt_m1219 || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1219/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1219_work/arms_start.txt
PRUNE=0 bash D:/PoB_work/wt_m1219/scripts/instruments/ident/ident.sh run m1219z --tree D:/PoB_work/wt_m1219 --work D:/PoB_work/ident_m1219z --saveleak --ng --save 20261119 --tests-delta 3 --removed-cols "rulepower_s:fPushbackIndex,soldiervac_s:drawConsHomeK,soldiervac_s:drawConsColonialK,soldiervac_s:drawConsMilPopHome,soldiervac_s:drawConsMilPopColonial,soldiervac_s:drawVolHomeK,soldiervac_s:drawVolColonialK" --grown-ok satneeds_s,soldiervac_s --known-extra "rulepower_s:bestClass,rulepower_s:fForeignRawIndex,rulepower_s:fForeignSumIndex,rulepower_s:fForeignTopIndex,rulepower_s:fInterIndex,rulepower_s:fResistIndex,rulepower_s:fStateIndex,rulepower_s:fUngovIndex,rulepower_s:foreignSideShare,rulepower_s:foreignTopKey,rulepower_s:iProbeShare,rulepower_s:interShare,rulepower_s:localSideShare,rulepower_s:navalAllocIndex,rulepower_s:newRule0Share,rulepower_s:newRule1Share,rulepower_s:openDiffIndex,rulepower_s:ownerSideShare,rulepower_s:ownerSideShareR,rulepower_s:ownerWeightShare,rulepower_s:phiStarIndex,rulepower_s:pressureIndex,rulepower_s:rhoNewIndex,rulepower_s:rhoNoFreezeIndex,rulepower_s:tauIndex,rulepower_s:tauNoFreezeIndex,rulepower_s:ungovSideShare,rulepower_s:ungovSideShareR,rulepeace_s:greatCircleOnlyTakenCount,rulepeace_s:phiStarMedianIndex,rulepeace_s:takenCount,rulepeace_s:takenPopShare,rulepoweralloc_s:allocIndex,rulepoweralloc_s:bShare,rulepoweralloc_s:fForeignIndex,rulepoweralloc_s:rhoF,rulepoweralloc_s:wgtIndex"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1219_work/arms_start.txt
exit $RC
