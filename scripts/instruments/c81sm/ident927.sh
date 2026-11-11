#!/usr/bin/env bash
# (927) 恒等の判定の 1 コマンド（宣言＝cearn_s:heirDebtSettledPerQuarter の値だけ・試験 +3＝HeirDebtSettledMovedTests 1・OnDoubleReadScanTests 2）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c81sm_work/ident.mark bash scripts/instruments/c81sm/ident927.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c81sm || exit 2
bash scripts/instruments/ident/ident.sh run c81sm --tree D:/PoB_work/wt_c81sm --work D:/PoB_work/c81sm_work/ident_c81sm --saveleak --tests-delta 3 --known-extra cearn_s:heirDebtSettledPerQuarter
