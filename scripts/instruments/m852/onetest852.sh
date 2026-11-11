#!/usr/bin/env bash
# (852) 合流：1 つのコミットの木で試験の組を 1 つだけ回す（Release・隔離ビルド）。WriterRatchetTests の計算量の表の取り直し（★(749)）の値を読むため。
#   bash onetest852.sh <コミット> <filter>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; F=${2:?filter}; W=D:/PoB_work/m852; S=$W/onesrc_$H
rm -rf $S && mkdir -p $S && (cd D:/PoB_work/wt_m852 && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_one_$H --disable-build-servers > $W/one_build_$H.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/one_build_$H.txt; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_one_$H --filter "$F" --logger "console;verbosity=detailed" > $W/one_test_$H.txt 2>&1
echo "test rc=$?"
grep -h "合計\|Passed!\|Failed!\|成功!\|失敗!" $W/one_test_$H.txt | tail -3
echo ONEDONE
