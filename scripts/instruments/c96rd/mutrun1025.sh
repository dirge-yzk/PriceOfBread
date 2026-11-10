#!/usr/bin/env bash
# (1025) c96rd 突然変異の駆動：使い捨ての木（git worktree・detached）で M0〜M4 を 1 本ずつ当て、ResourceRowsTests を回して出力を残す。
#   bash mutrun1025.sh <コミット> <作業フォルダ> <出力フォルダ（measurements/m1025_20261110）>
H=${1:?コミット}; W=${2:?作業}; O=${3:?出力}
R=C:/Users/dirge/Documents/PriceOfBread; S="$(cd "$(dirname "$0")" && pwd)"
T=$W/wt_mut
[ -d "$T" ] || git -C $R worktree add --detach "$T" "$H" > /dev/null 2>&1 || exit 1
for M in M0 M1 M2 M3 M4; do
  git -C "$T" checkout -q -- . && git -C "$T" checkout -q --detach "$H" || exit 1
  python "$S/mut1025.py" "$T" $M > "$O/mut_$M.txt" 2>&1 || { echo "$M apply failed" >> "$O/mut_$M.txt"; continue; }
  git -C "$T" diff --stat >> "$O/mut_$M.txt"
  bash "$S/t1025.sh" "$T" "$W/mut_$M" rows >> "$O/mut_$M.txt" 2>&1
  echo "rc=$?" >> "$O/mut_$M.txt"
  grep -h "エラー メッセージ:" -A1 "$W/mut_$M/test_rows.txt" | grep -v "エラー メッセージ:\|^--" | head -8 >> "$O/mut_$M.txt"
done
git -C "$T" checkout -q -- .
echo MUTDONE
