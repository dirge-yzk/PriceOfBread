#!/usr/bin/env bash
# (1222) c123k3 突然変異 M1〜M3：使い捨ての木（git archive）に 1 本ずつ入れて HealthcareLiteracyGateTests を回す。出力 measurements/m1222_20261119/mut_M*.txt。
#   bash scripts/instruments/c123k3/mut_k3.sh <コミット>
C=${1:?コミット}; cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c123k3_work/mut; OUT=$R/measurements/m1222_20261119
export TEMP=D:/PoB_work/c123k3_work/tmp TMP=D:/PoB_work/c123k3_work/tmp; mkdir -p $OUT
for M in M1 M2 M3; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  python -X utf8 -I $R/scripts/instruments/c123k3/mut_apply.py $W/$M $M > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  TREE=$W/$M ART=$W/${M}_art LOG=$W/$M.log bash D:/PoB_work/c123k3_work/t1.sh >> $OUT/mut_$M.txt 2>&1
  echo "rc=$?" >> $OUT/mut_$M.txt
  grep -E "^\s+失敗 |Assert" $W/$M.log | head -8 >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
