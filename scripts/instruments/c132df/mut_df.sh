#!/usr/bin/env bash
# (1284) c132df 突然変異：使い捨ての木（git archive）に 1 本ずつ入れて ArrearsOnePredicateTests を回す。出力 measurements/m1284_20261122/mut_*.txt。
#   bash scripts/instruments/c132df/mut_df.sh <コミット> [変異...]
C=${1:?コミット}; shift; LIST=${*:-M0 M1 M4 M6 F0}
cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c132df_work/mut; OUT=$R/measurements/m1284_20261122
export TEMP=D:/PoB_work/c132df_work/tmp TMP=D:/PoB_work/c132df_work/tmp AVALONIA_TELEMETRY_OPTOUT=1; mkdir -p $TEMP $OUT
for M in $LIST; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  python -X utf8 -I $R/scripts/instruments/c132df/mut_apply.py $W/$M $M > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  dotnet test $W/$M/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/${M}_art --filter "FullyQualifiedName~ArrearsOnePredicateTests" > $W/$M.log 2>&1
  echo "rc=$?" >> $OUT/mut_$M.txt
  grep -E '^\s+(失敗|成功|Failed|Passed) ' $W/$M.log | sed 's/\[[0-9 .]*m\?s\]//' >> $OUT/mut_$M.txt
  grep -E '合計|Total tests|error CS' $W/$M.log | head -3 >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
