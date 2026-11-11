#!/usr/bin/env bash
# (913) 恒等の判定の 1 コマンド（宣言 0・テストの数の増減 0）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c79ar_work/ident.mark bash scripts/instruments/c79ar/ident913.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c79ar || exit 2
bash scripts/instruments/ident/ident.sh run c79ar --tree D:/PoB_work/wt_c79ar --work D:/PoB_work/c79ar_work/ident_c79ar --saveleak --tests-delta 0
