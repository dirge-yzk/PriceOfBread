#!/usr/bin/env bash
# (895) c76ls フルスイート（(892) suite892.sh の写し・木は worktree そのまま・作業フォルダ c76ls_work）：Release・Category!=FullLength と =FullLength を分けて回し、--list-tests の数と突き合わせる。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c76ls_work/suite.mark bash scripts/instruments/c76ls/suite895.sh
#   savetest と leakhunt は ident（--saveleak）が回した。10 種のバッチと並べない。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W=D:/PoB_work/c76ls_work; A=$W/art_suite
cd "$REPO" || exit 9
echo "木 $(git rev-parse HEAD)"
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]\|成功!\|失敗!" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
rm -rf $A
echo SUITEDONE
