#!/usr/bin/env bash
# (908) 恒等の判定の 1 コマンド：素の 5 種・計器つき 10 種・テストの数・savetest／leakhunt を m905 の基準（ident.cfg）と突き合わせる。
#   宣言：--tests-delta 4（Lawgate2 2・T3b 1・T7f 1）・--added-tables lawgate2_s（新しい表）・--grown-ok coupgate_s（末尾に nanBranchMark）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c78sm_work/ident.mark bash scripts/instruments/c78sm/ident908.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c78sm || exit 2
bash scripts/instruments/ident/ident.sh run c78sm --tree D:/PoB_work/wt_c78sm --work D:/PoB_work/c78sm_work/ident_c78sm --saveleak --tests-delta 4 --added-tables lawgate2_s --grown-ok coupgate_s
