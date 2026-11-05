#!/usr/bin/env bash
# (848) c69eh 旗 1 の木のフルスイート（㉜）：Category!=FullLength と FullLength を分けて回し、--list-tests の数と突き合わせる。続けて savetest と leakhunt（saveleak.sh）。
#   使い方: bash suite848.sh <作業フォルダ（D:/ の形）>
export AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
T=D:/PoB_work/wt_c69eh; W=${1:?作業フォルダ}; A=$W/art; mkdir -p $W
P=$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj
dotnet build $P -c Release --artifacts-path $A --disable-build-servers > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
  dotnet test $P -c Release --no-build --artifacts-path $A --list-tests --filter "${f%%:*}" > $W/list_${f##*:}.txt 2>&1
  dotnet test $P -c Release --no-build --artifacts-path $A --filter "${f%%:*}" > $W/run_${f##*:}.txt 2>&1
  echo "rc_${f##*:}=$?"
done
mkdir -p $W/saveleak
POB_WORK=$W/saveleak SAVELEAK_SRC=$T bash $T/scripts/instruments/saveleak.sh > $W/saveleak.txt 2>&1
echo "rc_saveleak=$?"
echo SUITEDONE
