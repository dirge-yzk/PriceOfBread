#!/usr/bin/env bash
T=$1
cd /d/PoB_work/c88ts_work/wt$T || exit 9
F="FullyQualifiedName~RouteKmTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~DiplomacyPendingTests|FullyQualifiedName~RuleInstrumentTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c88ts_art_$T --filter "$F" 2>&1 | grep -E "^\s+失敗|成功!|失敗!|対照|前提"
dotnet test PriceOfBread.Tests -c Release --no-build --artifacts-path D:/PoB_work/c88ts_art_$T --filter "$F" --list-tests 2>&1 | grep -c "^    "
