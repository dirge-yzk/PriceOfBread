#!/usr/bin/env bash
PY=/c/Users/dirge/AppData/Local/Programs/Python/Python312/python
cd /d/PoB_work/wt_c102em
for M in BASE M0 M1 M2 M3 M5; do
  $PY -X utf8 -I /d/PoB_work/c102em_work/mut.py $M
  echo "=== $M" >> /d/PoB_work/c102em_work/mut_summary.txt
  dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c102em_art --disable-build-servers -v q > /d/PoB_work/c102em_work/mut_$M.build.txt 2>&1 || { echo "BUILD FAIL" >> /d/PoB_work/c102em_work/mut_summary.txt; continue; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path D:/PoB_work/c102em_art --filter "FullyQualifiedName~ZeroPopSkipTests" > /d/PoB_work/c102em_work/mut_$M.test.txt 2>&1
  grep -E "^\s+(失敗|X) |成功!|失敗!" /d/PoB_work/c102em_work/mut_$M.test.txt | cut -c1-200 >> /d/PoB_work/c102em_work/mut_summary.txt
done
cp /d/PoB_work/c102em_work/EconomyStep.skip.cs PriceOfBread.SimCore/EconomyStep.cs
echo restored >> /d/PoB_work/c102em_work/mut_summary.txt
