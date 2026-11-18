#!/usr/bin/env bash
# (1215) c122bd 突然変異 M1〜M5：使い捨ての木（git archive）に 1 本ずつ入れて ArrearsOpenedRoomTests を回す。出力 measurements/m1215_20261118/mut_M*.txt。
#   bash scripts/instruments/c122bd/mut_bd.sh <コミット>
C=${1:?コミット}; cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c122bd_work/mut; OUT=$R/measurements/m1215_20261118
export TEMP=D:/PoB_work/c122bd_work/tmp TMP=D:/PoB_work/c122bd_work/tmp
for M in M1 M2 M3 M4 M5; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  python -X utf8 -I $R/scripts/instruments/c122bd/mut_apply.py $W/$M $M > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  TREE=$W/$M ART=$W/${M}_art LOG=$W/$M.log bash D:/PoB_work/c122bd_work/t1.sh >> $OUT/mut_$M.txt 2>&1
  echo "rc=$?" >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
