#!/usr/bin/env bash
# (1302) c135tl の段 2 の恒等（腕 Z）。宣言：treasledger_s の行の増え（_bornTreasury）・テストの差 +3（T7〜T9）。PRUNE=1（D: の空きが 40GB で足りない＝基準と生が一致した表は刈る・差のある表と sha1 は残す）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c135tl_work/ident.mark bash scripts/instruments/c135tl/ident_c135tl.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c135tl_work/tmp TMP=D:/PoB_work/c135tl_work/tmp PRUNE=1
cd D:/PoB_work/wt_c135tl || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "ident start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c135tl_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c135tl --tree D:/PoB_work/wt_c135tl --work D:/PoB_work/ident_c135tl --saveleak --ng --save 20261122 --stage2 --tests-delta 3 --rows-grown-ok treasledger_s
RC=$?
echo "ident end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c135tl_work/arms_start.txt
exit $RC
