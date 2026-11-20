#!/usr/bin/env bash
# (1269) c130k15 突然変異 μ0〜μ3：使い捨ての木（git archive）に 1 本ずつ入れて JobSearchLiteracyOffTests を回す。出力 measurements/m1269_20261121/mut_m*.txt。
#   bash scripts/instruments/c130k15/mut_k15.sh <コミット>
C=${1:?コミット}; cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c130k15_work/mut; OUT=$R/measurements/m1269_20261121
mkdir -p $OUT
for M in m0 m1 m2 m3; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  python -X utf8 -I $R/scripts/instruments/c130k15/mut_apply.py $W/$M $M > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  TREE=$W/$M ART=$W/${M}_art LOG=$W/$M.out bash D:/PoB_work/c130k15_work/t1.sh
  echo "rc=$?" >> $OUT/mut_$M.txt
  grep -E "^\s+失敗 |合計:" $W/$M.out | head -8 >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
