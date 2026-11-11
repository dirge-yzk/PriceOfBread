#!/usr/bin/env bash
# 直す前（旗 0/1 の木）に触る 6 クラスを回す。引数：木の名前
T=$1
cd /d/PoB_work/c88ts_work/wt$T || exit 9
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c88ts_art_$T --filter "FullyQualifiedName~RouteKmTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~DiplomacyPendingTests|FullyQualifiedName~RuleInstrumentTests" 2>&1 | tail -30
