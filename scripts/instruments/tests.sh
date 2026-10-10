#!/usr/bin/env bash
# 使い方：POB_WORK=<作業フォルダ> bash tests.sh <タグ> <ソース木> [フィルタ]（隔離ビルドでフルスイート・既定は Category!=FullLength）
export AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W="${POB_WORK:?POB_WORK を指定}"; T=$W/tree_t_$1; SRC=$2; FIL=${3:-Category!=FullLength}
rm -rf $T && mkdir -p $T
tar -cf - -C $SRC --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot . | tar -xf - -C $T
OUT=$W/tests_$1.txt
echo "START $(date +%T) filter=$FIL src=$SRC" > $OUT
dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path $W/art_t$1 --disable-build-servers >> $OUT 2>&1; echo "build rc=$?" >> $OUT
dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path $W/art_t$1 --filter "$FIL" >> $OUT 2>&1; echo "rc=$? END $(date +%T)" >> $OUT
