#!/usr/bin/env bash
# (1137) c111rb：旗 1 の木の ㊴（計器の試験の束）→ フルスイート（Tests の Category!=FullLength・FullLength・Tests.Ui を分けて・trx）と --list-tests の数。
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 TEMP=D:/PoB_work/c111rb_work/tmp TMP=D:/PoB_work/c111rb_work/tmp
H=${1:?コミット}; STAGE=${2:-all}; W=D:/PoB_work/c111rb_work/suite; S=$W/src
if [ "$STAGE" != "full" ]; then
  rm -rf $S && mkdir -p $S && (cd /d/PoB_work/wt_c111rb && git archive $H) | tar -x -C $S || exit 1
  cd $S
  dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art --disable-build-servers > $W/build_tests.txt 2>&1 || { echo BUILDFAIL; exit 1; }
  dotnet build PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $W/art --disable-build-servers > $W/build_ui.txt 2>&1 || { echo BUILDFAIL_UI; exit 1; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "FullyQualifiedName~Instrument" > $W/t39.txt 2>&1; echo "t39 rc=$?"
  grep -h "成功!\|失敗!" $W/t39.txt | tail -1
  [ "$STAGE" = "t39" ] && exit 0
fi
cd $S
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --list-tests --filter "Category!=FullLength" > $W/list_nonfull.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --list-tests --filter "Category=FullLength" > $W/list_full.txt 2>&1
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --no-build --artifacts-path $W/art --list-tests > $W/list_ui.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "Category!=FullLength" --logger "trx;LogFileName=$W/nonfull.trx" > $W/nonfull.txt 2>&1; echo "nonfull rc=$?"
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --no-build --artifacts-path $W/art --logger "trx;LogFileName=$W/ui.trx" > $W/ui.txt 2>&1; echo "ui rc=$?"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "Category=FullLength" --logger "trx;LogFileName=$W/full.trx" > $W/full.txt 2>&1; echo "full rc=$?"
grep -h "成功!\|失敗!\|中止" $W/nonfull.txt $W/ui.txt $W/full.txt
echo SUITEDONE
