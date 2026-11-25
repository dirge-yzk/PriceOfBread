#!/usr/bin/env bash
# (1376) 合わせた木で ㉘ の走査の番人＋各線の試験＋表の番人を先に回す
W=D:/PoB_work/m1376_work; A=$W/art; export TEMP=$W/tmp TMP=$W/tmp; mkdir -p $TEMP
cd D:/PoB_work/wt_m1376 || exit 2
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $W/t28_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/t28_build.txt; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~SaveRoundTripTests|FullyQualifiedName~WarWearyPoliticsCutTests|FullyQualifiedName~SatReadersToDiscontentTests|FullyQualifiedName~TsregInstrumentTests|FullyQualifiedName~LawstepInstrumentTests|FullyQualifiedName~CwendInstrumentTests|FullyQualifiedName~CalendarTests|FullyQualifiedName~InstrumentHookClosureTests" > $W/t28.txt 2>&1
RC=$?; echo "rc=$RC"; grep -E "合計|失敗 |成功|Failed |Passed!" $W/t28.txt | tail -15
