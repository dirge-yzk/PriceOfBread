#!/usr/bin/env bash
# (854) c70id：木を隔離ビルド（--artifacts-path）して、試験の組だけを回す。変異の使い捨ての写しにも使う。
#   bash t854.sh <木のパス> <artifacts のパス> <フィルタ> [出力の印の名前]
#   出力：<artifacts>/../<木の名前>_t.txt は呼び手が bg.sh の .log で受ける。ここでは標準出力に件数と赤の名前を出す。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SRC=${1:?木}; ART=${2:?artifacts}; FILTER=${3:?フィルタ}
cd "$SRC" || exit 1
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path "$ART" --disable-build-servers > "$ART.build.txt" 2>&1 || { echo BUILDFAIL; grep -E "error " "$ART.build.txt" | sort -u | head -20; exit 1; }
T0=$(date +%s)
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path "$ART" --filter "$FILTER" > "$ART.test.txt" 2>&1
RC=$?
echo "test rc=$RC $(( $(date +%s) - T0 ))s"
grep -hE "合計|中止|Total tests|aborted|Passed!|Failed!|^\s*(失敗|Failed) " "$ART.test.txt" | head -40
grep -hE -A6 "^\s*(失敗|Failed) " "$ART.test.txt" | head -80
exit $RC
