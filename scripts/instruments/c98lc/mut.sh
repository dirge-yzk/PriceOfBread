#!/usr/bin/env bash
# 使い捨ての木（git worktree・HEAD）で突然変異を 1 本試す：mut.sh <名前> <ファイル> <旧> <新> <試験の絞り込み>
set -u
N="$1"; F="$2"; OLD="$3"; NEW="$4"; FILT="$5"
SRC=/d/PoB_work/wt_c98lc; T=/d/PoB_work/c98lc_mut_$N
git -C "$SRC" worktree remove --force "$T" 2>/dev/null; rm -rf "$T"
git -C "$SRC" worktree add --detach "$T" HEAD >/dev/null 2>&1 || { echo "worktree 失敗"; exit 2; }
cd "$T"
MUT_F="$T/$F" MUT_O="$OLD" MUT_N="$NEW" python - <<'PY'
import os
p=os.environ['MUT_F']; b=open(p,'rb').read().decode('utf-8')
o=os.environ['MUT_O']; n=os.environ['MUT_N']
assert b.count(o)>=1,'no match'
open(p,'wb').write(b.replace(o,n,1).encode('utf-8'))
PY
dotnet test PriceOfBread.Tests -c Release --artifacts-path "D:/PoB_work/c98lc_mutart_$N" --filter "$FILT" -nologo -v q 2>&1 | grep -E "error|Failed|Passed|合格|失敗|Expected|Actual" | cut -c1-220 | head -8
echo "MUT $N 終了"
