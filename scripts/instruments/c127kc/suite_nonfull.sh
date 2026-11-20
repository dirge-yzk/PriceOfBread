#!/bin/bash
# (1251) ㉜ の書き換えの後のやり直し：Tests の Category!=FullLength（trx・--list-tests の数）
export TEMP=D:/PoB_work/c127kc_work/tmp TMP=D:/PoB_work/c127kc_work/tmp
cd /d/PoB_work/wt_c127kc
A=D:/PoB_work/c127kc_art2; T=D:/PoB_work/c127kc_work/trx
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "Category!=FullLength" --logger "trx;LogFileName=t_nonfull2.trx" --results-directory $T 2>&1 | grep -E "^\s+失敗 |成功!|失敗!" | tail -15
echo "--list-tests"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>&1 | grep -cE "^    [A-Za-z_]"
