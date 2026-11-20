#!/bin/bash
# フルスイート（Release・隔離ビルド）：Tests の非 FullLength・FullLength・Tests.Ui。ログは OUT・終わりに mark。
cd /d/PoB_work/wt_c129pc || exit 1
OUT=/d/PoB_work/c129pc_work/full; mkdir -p $OUT; rm -f $OUT/mark.txt
A=D:/PoB_work/c129pc_art
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A > $OUT/build.log 2>&1 || { echo "rc=1 build" > $OUT/mark.txt; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category!=FullLength" --logger "trx;LogFileName=full_nonfull.trx" > $OUT/nonfull.log 2>&1; R1=$?
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A --logger "trx;LogFileName=full_ui.trx" > $OUT/ui.log 2>&1; R3=$?
echo "rc=$((R1+R3)) nonfull=$R1 ui=$R3" > $OUT/mark.txt
