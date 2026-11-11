#!/usr/bin/env bash
# (869) c72ip（(863) の marms869.sh の写し・名だけ c72ip）突然変異の腕（計器つき 1 種＝種 1）：bash marms869.sh <コミット> <作業フォルダ> [型...]（既定 E1 E2 E3 E4）
#   型ごとに batch.sh prep（写しの木を mut869.py で書き換え）→ INS=1 batch.sh run（1 種）→ gates869.py を種 1 に当てる。腕は順に 1 本ずつ（枠を 1 つだけ使う）。
export PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; IS="$(cd "$SP/.." && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
H=$1; export POB_WORK=$2 POB_REPO=$REPO; shift 2; MODES="${*:-E1 E2 E3 E4}"
mkdir -p "$POB_WORK"
for m in $MODES; do
  A=c72ip$m
  bash "$IS/batch.sh" prep $A commit:$H plain "$SP/mut869.py $m" > "$POB_WORK/prep_$A.txt" 2>&1 || { echo "$m PREPFAIL"; continue; }
  INS=1 bash "$IS/batch.sh" run $A 1 > "$POB_WORK/run_$A.txt" 2>&1
  echo "$m run rc=$?"
  python -I "$SP/gates869.py" "$POB_WORK/out_${A}_1/ins" 1 1 > "$POB_WORK/gates_$A.txt" 2>&1
  rm -rf "$POB_WORK/src_$A"
done
echo MARMSDONE
