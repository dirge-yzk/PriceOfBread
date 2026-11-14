#!/bin/bash
# フルスイート（2 プロジェクト・trx・--list-tests の和）。TEMP は D: の作業フォルダへ。
export TEMP=/d/PoB_work/c110tb_work/tmp TMP=/d/PoB_work/c110tb_work/tmp
cd /d/PoB_work/wt_c110tb
A=D:/PoB_work/c110tb_art2; T=/d/PoB_work/c110tb_work/trx
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "Category!=FullLength" --logger "trx;LogFileName=t_nonfull.trx" --results-directory $T 2>&1 | grep -E "失敗|合格|成功!|失敗!" | tail -15
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A --logger "trx;LogFileName=t_ui.trx" --results-directory $T 2>&1 | grep -E "失敗|合格|成功!|失敗!" | tail -10
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=t_full.trx" --results-directory $T 2>&1 | grep -E "失敗|合格|成功!|失敗!" | tail -10
echo "--list-tests"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" 2>&1 | grep -cE "^    [A-Za-z_]"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --no-build --list-tests --filter "Category=FullLength" 2>&1 | grep -cE "^    [A-Za-z_]"
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A --no-build --list-tests 2>&1 | grep -cE "^    [A-Za-z_]"
