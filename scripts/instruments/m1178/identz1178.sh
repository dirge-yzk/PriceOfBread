#!/usr/bin/env bash
# (1178) 合流の腕 Z（m1171/identz1171.sh の型・木を wt_m1178z〔合わせた木（cl＋t1＋k1・追記 1 で hd を外した）＋挙動の線 2 本の旗を 0 へ戻す 1 コミット・push しない〕へ）
#   宣言：列が増えた表 t1buy_s（t1 +13）・secession_s（k1 +5）・テストの差 +5（t1 +1・k1 +4）・段 2・PRUNE は既定。NOINSZ=1 で --no-ins。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1178_work/identz.mark bash scripts/instruments/m1178/identz1178.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1178_work/tmp TMP=D:/PoB_work/m1178_work/tmp
cd D:/PoB_work/wt_m1178z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1178z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1178_work/arms_start.txt
bash D:/PoB_work/wt_m1178z/scripts/instruments/ident/ident.sh run m1178z --tree D:/PoB_work/wt_m1178z --work D:/PoB_work/ident_m1178z --saveleak ${NOINSZ:+--no-ins} --tests-delta 5 --ng --save 20261116 --stage2 --grown-ok t1buy_s,secession_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1178_work/arms_start.txt
exit $RC
