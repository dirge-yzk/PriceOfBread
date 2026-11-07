#!/usr/bin/env bash
# (923) c80lr フルスイート（(909) suite909.sh の写し・木は台本の置かれた worktree・作業フォルダ SUITE_W）：Release・Category!=FullLength と =FullLength を分けて回し、--list-tests の数と突き合わせる。
#   SUITE_W=D:/PoB_work/c80lr_work/i bash scripts/instruments/tools/bg.sh D:/PoB_work/c80lr_work/i.mark bash scripts/instruments/c80lr/suite923.sh
#   試験だけの線（SimCore 0 行）＝savetest と leakhunt は回さない。10 種のバッチと並べない。
#   MUT_FILTER を渡すと、フルスイートの代わりにその絞り込みだけを回す（突然変異の木）。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
W="${SUITE_W:-D:/PoB_work/c80lr_work/i}"; A=$W/art_suite; mkdir -p "$W"
cd "$REPO" || exit 9
echo "木 $(git rev-parse HEAD)"
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
if [ -n "$MUT_FILTER" ]; then
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "$MUT_FILTER" > $W/mut.txt 2>&1; echo "mut rc=$?"
  grep -h "合計\|中止\|成功!\|失敗!" $W/mut.txt | tail -4
  grep -h "^\s*失敗 " $W/mut.txt | head -20
  rm -rf $A; echo SUITEDONE; exit 0
fi
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
