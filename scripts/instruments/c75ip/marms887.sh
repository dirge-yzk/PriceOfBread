#!/usr/bin/env bash
# (887) c75ip（(876) の marms876.sh の写し・名だけ c75ip・mut887.py・gates887.py・既定の型を E2・D: の床 20GB）
# 元：(876) c73ip（(869) の marms869.sh の写し）突然変異の腕（計器つき 1 種＝種 1）：bash marms887.sh <コミット> <作業フォルダ> [型...]（既定 EU1）
#   型ごとに batch.sh prep（写しの木を mut876.py で書き換え）→ INS=1 batch.sh run（1 種）→ gates876.py を種 1 に当てる。腕は順に 1 本ずつ（枠を 1 つだけ使う）。
export PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; IS="$(cd "$SP/.." && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
H=$1; export POB_WORK=$2 POB_REPO=$REPO; shift 2; MODES="${*:-E2}"
mkdir -p "$POB_WORK"
python -c "import shutil,sys;f=shutil.disk_usage('D:/').free/1e9;print('D: の空き（起こす前）', round(f,1), 'GB');sys.exit(0 if f>=20 else 5)" || { echo 'D: の空きが 20GB 未満＝起こさない'; exit 5; }
for m in $MODES; do
  A=c75ip$m
  bash "$IS/batch.sh" prep $A commit:$H plain "$SP/mut887.py $m" > "$POB_WORK/prep_$A.txt" 2>&1 || { echo "$m PREPFAIL"; continue; }
  INS=1 bash "$IS/batch.sh" run $A 1 > "$POB_WORK/run_$A.txt" 2>&1
  echo "$m run rc=$?"
  python -I -X utf8 "$SP/gates887.py" "$POB_WORK/out_${A}_1/ins" 1 1 > "$POB_WORK/gates_$A.txt" 2>&1
  rm -rf "$POB_WORK/src_$A"
done
echo MARMSDONE
