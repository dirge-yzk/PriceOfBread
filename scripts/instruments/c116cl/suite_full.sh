#!/usr/bin/env bash
# (1175) c116cl FullLength だけ（バッチと並べない・bg.sh で走らせる）
cd /d/PoB_work/wt_c116cl
export TEMP=D:/PoB_work/c116cl_work TMP=D:/PoB_work/c116cl_work
W=D:/PoB_work/c116cl_work/suite; mkdir -p $W
A="-c Release --artifacts-path D:/PoB_work/c116cl_art"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=$W/f.trx" > $W/f.txt 2>&1
echo done
