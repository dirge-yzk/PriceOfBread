#!/usr/bin/env bash
# 段 0 の探り：ビルドして探りの試験を回す。引数＝出力のファイル
cd /d/PoB_work/wt_c127kc || exit 9
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c127kc_art -v q -nologo 2>&1 | grep -E "error|Warn.*CS|Build succeeded|ビルドに成功" | head -20
KC_PROBE_OUT="$1" dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c127kc_art --no-build --filter "FullyQualifiedName~ZzKcProbe|FullyQualifiedName~ColonialFreeImportTests|FullyQualifiedName~ServiceEconomyTests.労働市場" -v q -nologo 2>&1 | grep -E "失敗|Failed|合格|Passed|Total|合計|Assert|実測|Actual|Expected" | head -30
