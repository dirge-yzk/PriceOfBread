#!/usr/bin/env bash
# (802) 合流の腕の前の煙試験：Release で Tests をビルドし、--list-tests の数と、関係するテストの組を回す（10 種のバッチと並べない）。
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
cd D:/PoB_work/wt_m802; W=D:/PoB_work/m802
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_smoke --disable-build-servers > $W/smoke_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/smoke_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_smoke --list-tests --filter "${f%%:*}" > $W/smoke_list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/smoke_list_${f##*:}.txt)"
done
F="FullyQualifiedName~WriterRatchet|FullyQualifiedName~OpeningBake|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~CapitalDividendHostRent|FullyQualifiedName~DormantIdentity|FullyQualifiedName~CollapsePeace|FullyQualifiedName~CurrencyMemoryBroken|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~TechLevelRemoval|FullyQualifiedName~IndustrializationReader|FullyQualifiedName~NationalConsciousnessGate|FullyQualifiedName~IdeaNeedBoard|FullyQualifiedName~ScoreReader|FullyQualifiedName~FiscalTests|FullyQualifiedName~CalendarTests"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_smoke --filter "($F)&Category!=FullLength" > $W/smoke_run.txt 2>&1; echo "run rc=$?"
grep -E "^\s+(失敗|Failed) |合計|Total tests|成功|Passed!|Failed!|中止" $W/smoke_run.txt | tail -20
