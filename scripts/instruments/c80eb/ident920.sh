#!/usr/bin/env bash
# (920) 恒等の判定の 1 コマンド（宣言：cearn_s・cpop_s の列が末尾に伸びる・足した試験 5）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c80eb_work/ident.mark bash scripts/instruments/c80eb/ident920.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c80eb || exit 2
bash scripts/instruments/ident/ident.sh run c80eb --tree D:/PoB_work/wt_c80eb --work D:/PoB_work/c80eb_work/ident_c80eb --saveleak --tests-delta 5 --grown-ok cearn_s,cpop_s
