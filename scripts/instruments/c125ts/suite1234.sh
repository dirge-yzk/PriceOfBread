#!/usr/bin/env bash
# (1234) c125ts フルスイート（m1116/suite1119.sh の full の道の写し・作業フォルダを c125ts_work/suite へ）：コミットを git archive で写し、
#   PriceOfBread.Tests と PriceOfBread.Tests.Ui を建てて、Category!=FullLength と =FullLength を分けて回し（trx）、--list-tests の和と突き合わせ、savetest・leakhunt。
#   bash scripts/instruments/c108mc/suite1119.sh <コミット>   ＝ 10 種のバッチと並べない
export TEMP=D:/PoB_work/c125ts_work/tmp TMP=D:/PoB_work/c125ts_work/tmp
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/c125ts_work/suite; S=$W/suitesrc
rm -rf $S && mkdir -p $S $W/trx && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $S || exit 1
cd $S
echo "木 $H"
: > $W/suite_build.txt
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do dotnet build $P/$P.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers >> $W/suite_build.txt 2>&1 || { echo BUILDFAIL $P; tail -20 $W/suite_build.txt; exit 1; }; done
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --list-tests --filter "${f%%:*}" > $W/list_${P}_${f##*:}.txt 2>&1
  echo "list $P ${f##*:} $(grep -c '^    ' $W/list_${P}_${f##*:}.txt)"
done; done
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
T0=$(date +%s); dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" --logger "trx;LogFileName=$W/trx/nonfull_$P.trx" > $W/suite_nonfull_$P.txt 2>&1; echo "nonfull $P rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test $P/$P.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" --logger "trx;LogFileName=$W/trx/full_$P.trx" > $W/suite_full_$P.txt 2>&1; echo "full $P rc=$? $(( $(date +%s) - T0 ))s"
done
grep -h "合計\|中止\|Total tests\|aborted\|\[FAIL\]\|一致するテストがありません\|No test matches" $W/suite_nonfull_*.txt $W/suite_full_*.txt | tail -12
grep -h -A2 "^\s*失敗 " $W/suite_nonfull_*.txt $W/suite_full_*.txt | head -40
echo SUITEDONE
rm -rf $W/art_suite
T0=$(date +%s); POB_WORK=$W SAVELEAK_SRC=$S bash $S/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1; echo "saveleak rc=$? $(( $(date +%s) - T0 ))s"
tail -3 $W/savetest.txt; tail -3 $W/leakhunt.txt; tail -2 $W/saveleak.log
echo SAVELEAKDONE
