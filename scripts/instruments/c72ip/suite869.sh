#!/usr/bin/env bash
# (869) c72ip（(866) 合流の suite866.sh の写し・作業フォルダを c72ip_work・git archive の元を wt_c72ip にしただけ・guards の型は外した）
#   フルスイート（Release・Category!=FullLength と =FullLength を分けて回す・--list-tests の数）・続けて savetest と leakhunt（saveleak.sh）。10 種のバッチと並べない。
#   bash suite869.sh <コミット>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/c72ip_work/suite; S=$W/suitesrc
mkdir -p $W
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_c72ip && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/suite_build.txt; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  echo "list ${f##*:} $(grep -c '^    ' $W/list_${f##*:}.txt)"
done
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]" $W/suite_nonfull.txt $W/suite_full.txt | tail -12
grep -h -A2 "^\s*失敗 " $W/suite_nonfull.txt $W/suite_full.txt | head -40
echo SUITEDONE
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash D:/PoB_work/wt_c72ip/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
tail -3 $W/savetest.txt; tail -3 $W/leakhunt.txt; tail -2 $W/saveleak.log
echo SAVELEAKDONE
