#!/usr/bin/env bash
# (1243) c126k99 突然変異 μ0〜μ2：使い捨ての木（git archive）に 1 本ずつ入れて U1 を回す。出力 measurements/m1243_20261119/mut_μ*.txt。
#   bash scripts/instruments/c126k99/mut_k99.sh <コミット>
C=${1:?コミット}; cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c126k99_work/mut; OUT=$R/measurements/m1243_20261119
export TEMP=D:/PoB_work/c126k99_work/tmp TMP=D:/PoB_work/c126k99_work/tmp; mkdir -p $OUT
for M in μ0 μ1 μ2; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  { echo "# (1243) 突然変異 $M・木=$C"; python -X utf8 -I $R/scripts/instruments/c126k99/mut_apply.py $W/$M $M; } > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  TREE=$W/$M ART=$W/${M}_art LOG=$W/$M.log bash D:/PoB_work/c126k99_work/t1.sh >> $OUT/mut_$M.txt 2>&1
  echo "rc=$?（1＝U1 が赤）" >> $OUT/mut_$M.txt
  grep -E "闘争度が植民地の月で" $W/$M.log | head -2 >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
