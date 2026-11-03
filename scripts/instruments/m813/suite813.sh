#!/usr/bin/env bash
# (813)（(808) の写し・作業フォルダだけ m813） フルスイート（Release・Category!=FullLength と =FullLength を分けて回す）。10 種のバッチと並べない（README §1-2）。
#   bash suite813.sh <コミット>   git archive の写しで回す（main の作業ツリーに bin・obj を作らない）
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/m813; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted" $W/suite_nonfull.txt $W/suite_full.txt | tail -4
echo SUITEDONE
