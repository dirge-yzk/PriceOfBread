#!/bin/bash
# 突然変異を 1 本ずつ入れて PopCloseInstrumentTests を回す（使い捨ての木 wt_c129pc_mut）。結果は OUT へ。
T=/d/PoB_work/wt_c129pc_mut; OUT=/d/PoB_work/c129pc_work/mut; SC=/d/PoB_work/wt_c129pc/scripts/instruments/c129pc
mkdir -p $OUT; : > $OUT/summary.txt
for mu in base mu1 mu2 mu3 mu4 mu5 mu6 mu0; do
  python -X utf8 -I $SC/mutate.py $T reset
  [ $mu != base ] && python -X utf8 -I $SC/mutate.py $T $mu
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c129pc_art_mut --filter "FullyQualifiedName~PopCloseInstrumentTests" > $OUT/$mu.log 2>&1)
  echo "$mu: $(grep -E '失敗!|成功!' $OUT/$mu.log | head -1 | cut -c1-80) 赤=$(grep -E '^\s+失敗 ' $OUT/$mu.log | sed -e 's/.*Tests\.PopCloseInstrumentTests\.\(T[0-9]\).*/\1/' | tr '\n' ' ')" >> $OUT/summary.txt
done
python -X utf8 -I $SC/mutate.py $T reset
echo "rc=0 done" > $OUT/mark.txt
