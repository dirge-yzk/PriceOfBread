#!/usr/bin/env bash
# (963) 恒等の判定の 1 コマンド（宣言＝cearn_s の末尾 2 列〔--grown-ok〕・試験 +4）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c86sm_work/ident.mark bash scripts/instruments/c86sm/ident963.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c86sm || exit 2
bash scripts/instruments/ident/ident.sh run c86sm --tree D:/PoB_work/wt_c86sm --work D:/PoB_work/c86sm_work/ident_c86sm --saveleak --tests-delta 4 --grown-ok cearn_s
