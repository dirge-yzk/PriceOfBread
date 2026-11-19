#!/usr/bin/env bash
# (1228) c124bk 突然変異 μ1〜μ5：使い捨ての木（git archive）に 1 本ずつ入れて PaySalesProducerBookTests を回す。出力 measurements/m1228_20261119/mut/mut_<μ>.txt。
#   bash scripts/instruments/c124bk/mut_bk.sh <コミット>
C=${1:?コミット}; cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c124bk_work/mut; OUT=$R/measurements/m1228_20261119/mut
export TEMP=D:/PoB_work/c124bk_work/tmp TMP=D:/PoB_work/c124bk_work/tmp; mkdir -p $OUT $TEMP
for M in ${MUTS:-mu1 mu2 mu3 mu4 mu5}; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  echo "# (1228) 突然変異 $M コミット $C" > $OUT/mut_$M.txt
  python -X utf8 -I $R/scripts/instruments/c124bk/mut_apply.py $W/$M $M >> $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M" >> $OUT/mut_$M.txt; continue; }
  ART=$W/art FILTER="FullyQualifiedName~PaySalesProducerBookTests" bash D:/PoB_work/c124bk_work/scan.sh $W/$M $W/$M.res > /dev/null 2>&1
  echo "rc=$?" >> $OUT/mut_$M.txt
  cat $W/$M.res >> $OUT/mut_$M.txt
  grep -E "^\s+失敗 |^\s+Failed " $W/$M.res.log | sed 's/^\s*//' >> $OUT/mut_$M.txt
  rm -rf $W/$M
done
echo MUTDONE
