#!/usr/bin/env bash
# (1006) 合流：合わせた木（wt_m1006）でフルスイートを Category!=FullLength と =FullLength に分けて回す
cd /d/PoB_work/wt_m1006
A=D:/PoB_work/m1006_work/suite_art; O=/d/PoB_work/m1006_work
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "Category!=FullLength" > $O/suite_nonfull.log 2>&1; R1=$?
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --filter "Category=FullLength" > $O/suite_full.log 2>&1; R2=$?
echo "nonfull rc=$R1 full rc=$R2"
[ $R1 = 0 ] && [ $R2 = 0 ]
