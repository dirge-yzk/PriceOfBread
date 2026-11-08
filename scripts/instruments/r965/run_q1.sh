#!/usr/bin/env bash
S=/c/Users/dirge/Documents/PriceOfBread/scripts/instruments
O=/d/PoB_work/r965_work/out
for a in "base_ins 1 base" "n1 1 n1" "n2 1 n2" "a11 11 a11"; do
  set -- $a
  python -X utf8 -I $S/r916/deadreg916.py D:/PoB_work/keep_961/$1 $2 > $O/dead_$3.txt || exit 1
  python -X utf8 -I $S/r931/read931a.py D:/PoB_work/keep_961/$1 $2 $3 > $O/read931a_$3.txt || exit 1
done
