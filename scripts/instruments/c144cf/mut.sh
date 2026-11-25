#!/bin/bash
# (1366) 突然変異 M1〜M4：1 本ずつ当てて CwendInstrumentTests を回し、赤の試験名を out へ。終わったら git checkout で戻す。
cd D:/PoB_work/wt_c144cf || exit 1
F=PriceOfBread.SimCore/Instruments/InstrumentsCwend.cs
OUT=D:/PoB_work/c144cf_work/mut.txt; : > $OUT
run() { dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c144cf_art --filter "FullyQualifiedName~CwendInstrumentTests" > D:/PoB_work/c144cf_work/mut_$1.log 2>&1
  echo "== $1 $2" >> $OUT; grep -E "^\s+失敗 |^失敗!|^成功!|error CS" D:/PoB_work/c144cf_work/mut_$1.log | cut -c1-170 >> $OUT; git checkout -- $F; }
python -I scripts/instruments/c144cf/mutate.py 1 && run M1 "endPath を旧の順（forced → collapse → …）"
python -I scripts/instruments/c144cf/mutate.py 2 && run M2 "reunify を capitalRising で書く"
python -I scripts/instruments/c144cf/mutate.py 3 && run M3 "entryExit を常に false"
python -I scripts/instruments/c144cf/mutate.py 4 && run M4 "collapseFlag を常に 0"
echo done >> $OUT
