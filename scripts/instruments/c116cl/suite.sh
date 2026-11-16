#!/usr/bin/env bash
# (1175) c116cl フルスイート（bg.sh で走らせる）：Tests・Tests.Ui の Category!=FullLength と Tests の FullLength を分けて回し、--list-tests の和と突き合わせる
cd /d/PoB_work/wt_c116cl
export TEMP=D:/PoB_work/c116cl_work TMP=D:/PoB_work/c116cl_work
W=D:/PoB_work/c116cl_work/suite; mkdir -p $W
A="-c Release --artifacts-path D:/PoB_work/c116cl_art"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj $A --filter "Category!=FullLength" --logger "trx;LogFileName=$W/t.trx" > $W/t.txt 2>&1
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj $A --logger "trx;LogFileName=$W/u.trx" > $W/u.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj $A --no-build --list-tests --filter "Category!=FullLength" > $W/lt.txt 2>&1
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj $A --no-build --list-tests > $W/lu.txt 2>&1
echo done
