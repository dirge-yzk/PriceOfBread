#!/usr/bin/env bash
# (1116) 合流の腕 Z（m1096/identz1096.sh の型・PRUNE=0 全表〔INSRAW を keep_1116/base_ins へ移すため〕・木を wt_m1116z〔合わせた木 di＋ha＋hb＋hc・挙動の線なし＝旗の切り替えなし〕へ）
#   宣言：足した表と列 0・テスト +3（di の DisplayUnitTypeTests 2・SourceScanCoverageTests 1）。C: の空きが少ない＝TEMP・TMP を D: へ。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1116_work/identz.mark bash scripts/instruments/m1116/identz1116.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
export TEMP='D:\PoB_work\m1116_work\tmp' TMP='D:\PoB_work\m1116_work\tmp'
cd D:/PoB_work/wt_m1116z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1116z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1116_work/arms_start.txt
bash D:/PoB_work/wt_m1116z/scripts/instruments/ident/ident.sh run m1116z --tree D:/PoB_work/wt_m1116z --work D:/PoB_work/ident_m1116z --saveleak --tests-delta 3 --ng --save 20261114
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1116_work/arms_start.txt
exit $RC
