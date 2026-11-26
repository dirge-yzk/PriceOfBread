#!/usr/bin/env bash
# (1390) 線の ident の生を要約してから消す（片付けの指示〈ident の生と腕の生＝帰属を読み終えたら消す〉）。
T=/c/Users/dirge/Documents/PriceOfBread/scripts/instruments/tools
M=/d/PoB_work/wt_m1390/measurements/m1390_20261126
for x in wb bb rk vs; do
  D=/d/PoB_work/ident_c147$x
  [ -d "$D" ] || continue
  for s in out_ib_10/ins; do
    if [ -d "$D/$s" ]; then python -I $T/keepsum.py "$D/$s" "$M/keepsum_ident_c147$x.tsv" || { echo "keepsum 失敗 $x"; exit 1; }; fi
  done
  bash $T/rmraw.sh "$D" || exit 1
done
df -h /d | tail -1
