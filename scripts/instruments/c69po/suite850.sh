#!/usr/bin/env bash
# (850) c69po（(845) の suite845.sh の写し・作業フォルダだけ c69po_work・git archive の元は wt_c69po） 旗 1 の木のフルスイート（Release・Category!=FullLength と =FullLength を分けて回す）・続けて savetest と leakhunt（saveleak.sh）。
#   bash suite850.sh <コミット> [filter <式>]   filter を付けると、その式の試験だけを回す（赤の切り分け）
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; MODE=${2:-full}; W=D:/PoB_work/c69po_work; S=$W/suitesrc
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c69po && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
if [ "$MODE" = filter ]; then
  T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "$3" > $W/suite_filter.txt 2>&1; echo "filter rc=$? $(( $(date +%s) - T0 ))s"
  grep -h "合計\|中止\|Total tests\|aborted" $W/suite_filter.txt | tail -4
  grep -h -A3 "^\s*失敗 " $W/suite_filter.txt | head -60
  echo FILTERDONE; exit 0
fi
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]" $W/suite_nonfull.txt $W/suite_full.txt | tail -8
grep -h -A3 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -80
echo SUITEDONE
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash D:/PoB_work/wt_c69po/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
tail -3 $W/savetest.txt; tail -3 $W/leakhunt.txt; tail -2 $W/saveleak.log
echo SAVELEAKDONE
