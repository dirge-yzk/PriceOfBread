#!/bin/bash
# usage: test.sh <filter> <outfile>   隔離ビルドで Release のテスト
cd /d/PoB_work/wt_c129pc || exit 1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c129pc_art --filter "$1" --logger "trx;LogFileName=$3" > "$2" 2>&1
echo "rc=$?" >> "$2"
