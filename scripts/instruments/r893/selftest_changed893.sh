#!/usr/bin/env bash
# (893) c76lg の自己試験：changed_rows.py（係数名の拾いを広げた版）を、何も変えない木と、Coefficients.cs の RevoltIgnitionIntensity の宣言の行だけを変えた木に当てる。
# 使い捨ての worktree は D:/PoB_work/r893_work/mut_c（731d3de1 から・detached・コミットしない・終わったら消す）。
# 比べとして (886) の旧の changed_rows.py（scripts/instruments/r886/）も同じ木に当てる。
set -u
R=/c/Users/dirge/Documents/PriceOfBread
W=/d/PoB_work/r893_work
L=$W/機能の台帳_after.md
S=$W/selftest
git -C $R worktree add --detach $W/mut_c 731d3de1 > $S/mut_c_add.txt 2>&1
python -X utf8 $W/tools/changed_rows.py D:/PoB_work/r893_work/mut_c $L > $S/changed_nochange.txt
python -X utf8 $R/scripts/instruments/r886/changed_rows.py D:/PoB_work/r893_work/mut_c $L > $S/changed_nochange_old886.txt
python -X utf8 $W/tools/mutate893.py D:/PoB_work/r893_work/mut_c > $S/mut_c_mutate.txt
git -C $W/mut_c diff -U0 > $S/mut_c_diff.txt
python -X utf8 $W/tools/changed_rows.py D:/PoB_work/r893_work/mut_c $L > $S/changed_ignition.txt
python -X utf8 $R/scripts/instruments/r886/changed_rows.py D:/PoB_work/r893_work/mut_c $L > $S/changed_ignition_old886.txt
git -C $R worktree remove --force $W/mut_c
git -C $R worktree prune
echo "worktrees_left=$(git -C $R worktree list | grep -c r893_work)"
for f in changed_nochange changed_nochange_old886 mut_c_mutate changed_ignition changed_ignition_old886; do echo "== $f"; cat $S/$f.txt; done
