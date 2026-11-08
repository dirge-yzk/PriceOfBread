#!/usr/bin/env bash
# (973) c87mw ㊴ 計器の試験の束（旗 1 の木・フルスイートの前）。使い方: bash insbundle973.sh <木> <出力> <隔離ビルド>
T="${1:?木}"; O="${2:?出力}"; A="${3:?隔離ビルド}"
DOTNET="/c/Program Files/dotnet/dotnet"
mkdir -p "$O"; cd "$T" || exit 2
"$DOTNET" build PriceOfBread.Tests -c Release --artifacts-path "$A" > "$O/build.log" 2>&1 || exit 3
"$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --filter "FullyQualifiedName~Instrument" --logger "trx;LogFileName=ins.trx" --results-directory "$(cygpath -w "$O")" > "$O/test.log" 2>&1
RC=$?
grep -o 'testName="[^"]*" [^>]*outcome="Failed"' "$O/ins.trx" 2>/dev/null | sed 's/testName="\([^"]*\)".*/\1/' > "$O/failed.txt"
exit $RC
