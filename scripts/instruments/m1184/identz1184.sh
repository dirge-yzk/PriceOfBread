#!/usr/bin/env bash
# (1184) 合流の腕 Z（m1178/identz1178.sh の型・木を wt_m1184z〔合わせた木 tg＋b0＋hd＋StockpilePanicWeight を 0.5 へ戻す 1 コミット・push しない〕へ）
#   宣言：足した表 mintcap_s（b0）・テストの差 +2（b0 の MintcapInstrumentTests 2 本・tg と hd は 0）・段 2・PRUNE は既定。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1184_work/identz.mark bash scripts/instruments/m1184/identz1184.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1184_work/tmp TMP=D:/PoB_work/m1184_work/tmp
cd D:/PoB_work/wt_m1184z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1184z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1184_work/arms_start.txt
bash D:/PoB_work/wt_m1184z/scripts/instruments/ident/ident.sh run m1184z --tree D:/PoB_work/wt_m1184z --work D:/PoB_work/ident_m1184z --saveleak --tests-delta 2 --ng --save 20261116 --stage2 --added-tables mintcap_s
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1184_work/arms_start.txt
exit $RC
