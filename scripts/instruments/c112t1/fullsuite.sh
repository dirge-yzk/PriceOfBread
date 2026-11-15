#!/bin/bash
# (1144) フルスイート（2 プロジェクト・trx・--list-tests の和）。TEMP は D: の作業フォルダへ。
export TEMP=D:/PoB_work/c112t1_work/tmp TMP=D:/PoB_work/c112t1_work/tmp
cd /d/PoB_work/wt_c112t1
A=D:/PoB_work/c112t1_art2; T=D:/PoB_work/c112t1_work/trx
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "Category!=FullLength" --logger "trx;LogFileName=t_nonfull.trx" --results-directory $T 2>&1 | grep -E "^\s+失敗 |成功!|失敗!" | tail -15
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A --logger "trx;LogFileName=t_ui.trx" --results-directory $T 2>&1 | grep -E "^\s+失敗 |成功!|失敗!" | tail -10
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=t_full.trx" --results-directory $T 2>&1 | grep -E "^\s+失敗 |成功!|失敗!" | tail -10
echo "--list-tests"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>&1 | grep -cE "^    [A-Za-z_]"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength" 2>&1 | grep -cE "^    [A-Za-z_]"
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A --no-build --list-tests 2>&1 | grep -cE "^    [A-Za-z_]"
