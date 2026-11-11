#!/usr/bin/env bash
# (834)（(827) suite827.sh の写し・作業フォルダ c67sv_suite・git archive の元は wt_c67sv）フルスイート（Release・Category!=FullLength と =FullLength を分けて回す）と savetest・leakhunt。
#   bash scripts/instruments/c67sv/suite834.sh <コミット>   10 種のバッチと並べない（README §1-2）。
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
H=${1:?コミット}; W=D:/PoB_work/c67sv_suite; S=$W/suitesrc
IS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c67sv && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -hE "^(失敗|成功)!" $W/suite_nonfull.txt $W/suite_full.txt
grep -hE "^\s+失敗 " $W/suite_nonfull.txt $W/suite_full.txt | cut -d'[' -f1
grep -hiE "中止|aborted|クラッシュ" $W/suite_nonfull.txt $W/suite_full.txt | head -5
echo "== saveleak"
mkdir -p $W/sl; POB_WORK=$W/sl SAVELEAK_SRC=$S bash "$IS/saveleak.sh" > $W/saveleak.txt 2>&1; echo "saveleak rc=$?"
tail -4 $W/sl/savetest.txt; tail -6 $W/sl/leakhunt.txt
echo SUITEDONE
