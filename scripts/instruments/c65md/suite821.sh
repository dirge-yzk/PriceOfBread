#!/usr/bin/env bash
# (821) c65md フルスイート（Release・Category!=FullLength と =FullLength を分けて回す・--list-tests の数と突き合わせる）。10 種のバッチと並べない（README §1-2）。
#   bash suite821.sh <コミット> <作業フォルダ>   git archive の写しで回す（worktree に bin・obj を作らない）
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=${2:?作業フォルダ}; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c65md && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/suite_list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/suite_list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted" $W/suite_nonfull.txt $W/suite_full.txt | tail -4
grep -h "^  失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -20
echo SUITEDONE
