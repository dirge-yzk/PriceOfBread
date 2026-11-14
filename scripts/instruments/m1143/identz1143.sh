#!/usr/bin/env bash
# (1143) 合流の腕 Z（m1136/identz1136.sh の写し・木を wt_m1143z〔合わせた木 H＋旗 3 本 RailLocoNeedsDiscovery・RailNeedByOutput・TreatyPortHomeOnly を 0 へ戻す 1 コミット〕へ）
#   宣言：足した表 railspend_s（lo）・tport_s（tp）・列が増えた表 trrail_s（rb の 4 列）・テストの差 +14（tf 3・sm 2・lo 3・rb 3・tp 3）・段 2（--stage2）・PRUNE は既定（新しい INSRAW は M の生＝keep_1143/base_ins）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1143_work/identz.mark bash scripts/instruments/m1143/identz1143.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1143_work/tmp TMP=D:/PoB_work/m1143_work/tmp
cd D:/PoB_work/wt_m1143z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1143z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1143_work/arms_start.txt
bash D:/PoB_work/wt_m1143z/scripts/instruments/ident/ident.sh run m1143z --tree D:/PoB_work/wt_m1143z --work D:/PoB_work/ident_m1143z --saveleak --tests-delta 14 --ng --save 20261115 --stage2 --added-tables railspend_s,tport_s --grown-ok trrail_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1143_work/arms_start.txt
exit $RC
