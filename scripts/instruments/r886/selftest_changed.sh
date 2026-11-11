#!/usr/bin/env bash
# (886) の自己試験：変わった行を選ぶ台本を、何も変えない木・持ち主のファイルを 1 つ変えた木・Coefficients.cs の係数を 1 つ変えた木に当てる。
# 使い捨ての worktree は D:/PoB_work/r886_work/mut_a・mut_b（コミットしない・終わったら消す）。
set -u
R=/c/Users/dirge/Documents/PriceOfBread
W=/d/PoB_work/r886_work
L=$W/機能の台帳.md
S=$W/selftest
H=11db605fd0773d232b2925b489960440c8801085
python -X utf8 $W/scripts/changed_rows.py C:/Users/dirge/Documents/PriceOfBread $L > $S/changed_main_nochange.txt
git -C $R worktree add $W/mut_a $H > /dev/null 2>&1
git -C $R worktree add $W/mut_b $H > /dev/null 2>&1
python -X utf8 $W/scripts/changed_rows.py D:/PoB_work/r886_work/mut_a $L > $S/changed_mut_a_before.txt
python -X utf8 $W/scripts/mutate_trees.py D:/PoB_work/r886_work
python -X utf8 $W/scripts/changed_rows.py D:/PoB_work/r886_work/mut_a $L > $S/changed_mut_a_factions.txt
python -X utf8 $W/scripts/changed_rows.py D:/PoB_work/r886_work/mut_b $L > $S/changed_mut_b_coef.txt
git -C $W/mut_a diff --name-only > $S/mut_a_diff_names.txt
git -C $W/mut_b diff -U0 > $S/mut_b_diff.txt
git -C $R worktree remove --force $W/mut_a
git -C $R worktree remove --force $W/mut_b
git -C $R worktree prune
echo "worktrees_left=$(git -C $R worktree list | grep -c r886_work)"
for f in changed_main_nochange changed_mut_a_before changed_mut_a_factions changed_mut_b_coef; do echo "== $f"; cat $S/$f.txt; done
