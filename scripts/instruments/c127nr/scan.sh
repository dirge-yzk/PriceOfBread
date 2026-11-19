#!/usr/bin/env bash
# ㉘ 走査の番人＋OnDoubleReadScanTests＋NationalismUnificationTests（隔離ビルド）
export TEMP=D:/PoB_work/c127nr_work/tmp TMP=D:/PoB_work/c127nr_work/tmp AVALONIA_TELEMETRY_OPTOUT=1
cd D:/PoB_work/wt_c127nr || exit 9
A=D:/PoB_work/c127nr_work/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > D:/PoB_work/c127nr_work/build_scan.txt 2>&1 || { grep -E "error" D:/PoB_work/c127nr_work/build_scan.txt | head -20; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~NationalismUnification" > D:/PoB_work/c127nr_work/t_scan.txt 2>&1
RC=$?
grep -E "^\s+失敗 |合計|成功|Passed!|Failed!" D:/PoB_work/c127nr_work/t_scan.txt | tail -20
exit $RC
