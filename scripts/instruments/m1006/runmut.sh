#!/usr/bin/env bash
# (1006) 合流：c92tg の突然変異 M1〜M6 を合わせた木の使い捨ての写しに当てる（M0＝無変更の対照）
T=D:/PoB_work/m1006_work/mut_tree
OUT=D:/PoB_work/m1006_work/tg_mut_final.txt
: > $OUT
for M in M0 M1 M2 M3 M4 M5 M6; do
  git -C $T checkout -q -- .
  if [ $M != M0 ]; then python -X utf8 -I /c/Users/dirge/Documents/PriceOfBread/scripts/instruments/c92tg/mut.py $T $M || { echo "$M apply failed" >> $OUT; continue; }; fi
  echo "=== $M" >> $OUT
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/m1006_work/mutart --filter "FullyQualifiedName~TradeInstrumentTests" 2>&1 | grep -E "\[FAIL\]|成功!|失敗!|error CS|Passed!|Failed!" | cut -c1-260 >> $OUT)
done
git -C $T checkout -q -- .
