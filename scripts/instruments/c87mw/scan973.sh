#!/usr/bin/env bash
# (973) c87mw 走査の番人（㉘）とこの線の試験 3 本を隔離ビルドで回す。使い方: bash scan973.sh <木> <出力フォルダ> <隔離ビルドのフォルダ>
T="${1:?木}"; O="${2:?出力}"; A="${3:?隔離ビルド}"
DOTNET="/c/Program Files/dotnet/dotnet"
mkdir -p "$O"; cd "$T" || exit 2
"$DOTNET" build PriceOfBread.Tests -c Release --artifacts-path "$A" > "$O/build.log" 2>&1 || { echo "build 失敗"; tail -20 "$O/build.log" | grep -E "error" ; exit 3; }
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~CivilWarArmsDemandTests|FullyQualifiedName~MilitaryWarMultContactTests|FullyQualifiedName~MilitaryTests"
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --filter "$F" --logger "trx;LogFileName=scan.trx" --results-directory "$(cygpath -w "$O")" > "$O/test.log" 2>&1
RC=$?
grep -o 'testName="[^"]*" [^>]*outcome="Failed"' "$O/scan.trx" 2>/dev/null | sed 's/testName="\([^"]*\)".*/\1/' > "$O/failed.txt"
grep -E "^(成功|失敗|Passed|Failed)!|合計|Total" "$O/test.log" | tail -3
exit $RC
