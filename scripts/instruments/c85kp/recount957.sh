#!/usr/bin/env bash
# (957) 基準とヌル腕を同じ台本（stoplines949.sh・RULE=pair・4 区分）で数え直す（⑰）
cd D:/PoB_work/wt_c85kp
for a in base_ins n1 n2; do
  RULE=pair NULLS="measurements/m947_20261108/null/stop906_n1.txt measurements/m947_20261108/null/stop906_n2.txt" bash scripts/instruments/c84gz/stoplines949.sh D:/PoB_work/keep_947/$a D:/PoB_work/c85kp_work/recount/stop949_$a.txt 1 > D:/PoB_work/c85kp_work/recount/$a.log 2>&1
  echo "$a rc=$?"
done
