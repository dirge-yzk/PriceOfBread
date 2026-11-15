#!/usr/bin/env bash
# (1153) c113qn フルスイート（Release・2 プロジェクト・Category!=FullLength と =FullLength を分ける・trx・--list-tests の和と突き合わせる）。10 種のバッチと並べない。
#   bash scripts/instruments/c113qn/suite.sh <木> <作業フォルダ>
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; W=${2:?作業フォルダ}; A=$W/art_suite; mkdir -p $W/trx
: > $W/suite_sum.txt
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
  dotnet build $T/$P/$P.csproj -c Release --artifacts-path $A --disable-build-servers > $W/build_$P.txt 2>&1 || { echo "BUILDFAIL $P" | tee -a $W/suite_sum.txt; exit 1; }
done
for P in PriceOfBread.Tests PriceOfBread.Tests.Ui; do
  for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
    F=${f%%:*}; K=${f##*:}
    dotnet test $T/$P/$P.csproj -c Release --no-build --artifacts-path $A --list-tests --filter "$F" > $W/list_${P}_$K.txt 2>&1
    N=$(grep -c '^    ' $W/list_${P}_$K.txt)
    dotnet test $T/$P/$P.csproj -c Release --no-build --artifacts-path $A --filter "$F" --logger "trx;LogFileName=$W/trx/${P}_$K.trx" > $W/run_${P}_$K.txt 2>&1
    RC=$?
    echo "$P $K rc=$RC list=$N $(grep -h '合計:\|Total tests\|中止\|aborted' $W/run_${P}_$K.txt | tail -1)" >> $W/suite_sum.txt
  done
done
cat $W/suite_sum.txt
