#!/usr/bin/env bash
# (1250) c127bd 突然変異 M1〜M7：使い捨ての木（git archive）に 1 本ずつ入れて ArrearsOpenedRoomTests を回す。出力 measurements/m1250_20261120/mut_M*.txt。
#   bash scripts/instruments/c127bd/mut_bd.sh <コミット> [変異...]
C=${1:?コミット}; shift; LIST=${*:-M1 M2 M3 M4 M5 M6 M7}
cd "$(dirname "$0")/../../.."; R=$(pwd); W=D:/PoB_work/c127bd_work/mut; OUT=$R/measurements/m1250_20261120
export TEMP=D:/PoB_work/c127bd_work/tmp TMP=D:/PoB_work/c127bd_work/tmp AVALONIA_TELEMETRY_OPTOUT=1; mkdir -p $TEMP
for M in $LIST; do
  rm -rf $W/$M; mkdir -p $W/$M; git archive $C | tar -x -C $W/$M
  python -X utf8 -I $R/scripts/instruments/c127bd/mut_apply.py $W/$M $M > $OUT/mut_$M.txt 2>&1 || { echo "APPLYFAIL $M"; continue; }
  dotnet test $W/$M/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/${M}_art --filter "FullyQualifiedName~ArrearsOpenedRoomTests" > $W/$M.log 2>&1
  echo "rc=$?" >> $OUT/mut_$M.txt
  grep -E '^\s+(失敗|成功|Failed|Passed) ' $W/$M.log | sed 's/\[[0-9 .]*m\?s\]//' >> $OUT/mut_$M.txt
  grep -E '合計|Total tests|error CS' $W/$M.log | head -3 >> $OUT/mut_$M.txt
  rm -rf $W/$M $W/${M}_art
done
echo MUTDONE
