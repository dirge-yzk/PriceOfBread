#!/usr/bin/env bash
# (868) c72tc フルスイート（Release・Category!=FullLength と =FullLength を分けて回し、--list-tests と突き合わせる）。元は m866/suite866.sh（作業フォルダと git archive の元だけ c72tc）。
#   bash suite868.sh <コミット>   savetest・leakhunt は ident（--saveleak）が済ませている。10 種のバッチと並べない。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/c72tc_work/suite; S=$W/src
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
rm -rf $S && mkdir -p $S && (cd "$REPO" && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art --disable-build-servers > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
echo SUITEDONE
