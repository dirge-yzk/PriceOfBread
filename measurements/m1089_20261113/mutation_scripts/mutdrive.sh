#!/usr/bin/env bash
# 使い捨ての木 mut に突然変異を当て、1 種の素の腕で回す（3 本ずつ並列）。比べる先は m0（変異なし・同じ木）。
export POB_WORK=D:/PoB_work/c104fb_work/mw
cd D:/PoB_work/wt_c104fb || exit 1
LIST="$*"
for M in $LIST; do
  python D:/PoB_work/c104fb_work/mut.py $M || exit 1
  bash scripts/instruments/batch.sh prep $M dir:D:/PoB_work/c104fb_work/mut plain || exit 1
done
git -C D:/PoB_work/c104fb_work/mut checkout -q -- .
printf '%s\n' $LIST | xargs -P 3 -I{} bash scripts/instruments/batch.sh run {} 1
