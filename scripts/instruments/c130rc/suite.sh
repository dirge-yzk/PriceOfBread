#!/usr/bin/env bash
# (1270) c130rc フルスイート（Release・隔離ビルド）＋ --list-tests の数＋savetest＋leakhunt。使い方: bash suite.sh <木> <出力フォルダ（/d/… の形）> <artifacts（D:/… の形）>
T=${1:?木}; OUT=${2:?出力}; A=${3:?artifacts}
mkdir -p "$OUT"; cd "$T" || exit 1
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A > $OUT/build.log 2>&1 || { echo "build rc=1"; exit 1; }
dotnet build PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path $A > $OUT/build_ui.log 2>&1 || { echo "build ui rc=1"; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --list-tests > $OUT/list.txt 2>&1
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --no-build --artifacts-path $A --list-tests > $OUT/list_ui.txt 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull.trx" --results-directory $OUT > $OUT/nonfull.log 2>&1; R1=$?
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --no-build --artifacts-path $A --logger "trx;LogFileName=ui.trx" --results-directory $OUT > $OUT/ui.log 2>&1; R2=$?
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $OUT > $OUT/fulllength.log 2>&1; R3=$?
bash measure.sh $OUT/savetest.txt savetest 1789 300 12 > $OUT/savetest.run 2>&1; R4=$?
bash measure.sh $OUT/leakhunt.txt leakhunt 1789 200 > $OUT/leakhunt.run 2>&1; R5=$?
echo "nonfull=$R1 ui=$R2 fulllength=$R3 savetest=$R4 leakhunt=$R5"
grep -hE "成功|失敗!" $OUT/nonfull.log $OUT/ui.log $OUT/fulllength.log
[ $((R1+R2+R3+R4+R5)) -eq 0 ]
