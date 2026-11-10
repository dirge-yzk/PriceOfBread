#!/usr/bin/env bash
# (1025) c96rd 試験の束（隔離ビルド・Release）：bash t1025.sh <木> <作業フォルダ> <組 rows|bundle|instr>
#   rows＝ResourceRowsTests だけ・bundle＝㉘ の束＋ResourceRowsTests＋UbiquitousMineralTests・instr＝計器の束（㊴）＋ResourceRowsTests
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; W=${2:?作業}; MODE=${3:-rows}
mkdir -p "$W"
cd "$T" || exit 1
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path "$W/art" --disable-build-servers > "$W/build_$MODE.txt" 2>&1 || { echo BUILDFAIL; grep -m5 " error " "$W/build_$MODE.txt"; exit 1; }
case "$MODE" in
  rows) F="FullyQualifiedName~ResourceRowsTests" ;;
  bundle) F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~ScalarDeletionTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~DiplomacyPendingTests|FullyQualifiedName~ResourceRowsTests|FullyQualifiedName~UbiquitousMineralTests" ;;
  instr) F="FullyQualifiedName~Instrument|FullyQualifiedName~ResourceRowsTests" ;;
esac
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path "$W/art" --filter "$F" > "$W/test_$MODE.txt" 2>&1
RC=$?
grep -h "合計\|中止\|Total tests\|成功!\|失敗!\|Passed!\|Failed!" "$W/test_$MODE.txt" | tail -4
grep -h "^\s*失敗 \|\[FAIL\]" "$W/test_$MODE.txt" | head -20
exit $RC
