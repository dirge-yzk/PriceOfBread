#!/usr/bin/env bash
# (921) 恒等の判定の 1 コマンド（宣言 0・試験 +3＝CoupGateNanMarkTests 1・ForceReadGuardTests 2）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c80sm_work/ident.mark bash scripts/instruments/c80sm/ident921.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c80sm || exit 2
bash scripts/instruments/ident/ident.sh run c80sm --tree D:/PoB_work/wt_c80sm --work D:/PoB_work/c80sm_work/ident_c80sm --saveleak --tests-delta 3
