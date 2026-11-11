#!/usr/bin/env bash
# (877) c73ts の ident（器のコミットの木＝D:/PoB_work/c73ts_work/tree_f0〔detached の worktree〕を測る＝走行中に wt_c73ts へコミットしても判定の頭が動かない＝㉙）
#   bash ident877.sh   ⚠起こす前に arms.sh free（3 以上）と D: の空き（35GB 以上）を呼び手が確かめる
F=$(python -c "import shutil;print(int(shutil.disk_usage('D:/').free/1e9))")
[ "$F" -ge 35 ] || { echo "D: の空き ${F}GB＜床 35GB"; exit 9; }
echo "D: の空き ${F}GB（起こす時）"
bash /d/PoB_work/wt_c73ts/scripts/instruments/ident/ident.sh run c73tsz --tree D:/PoB_work/c73ts_work/tree_f0 --work D:/PoB_work/c73ts_work/ident --tests-delta 5
