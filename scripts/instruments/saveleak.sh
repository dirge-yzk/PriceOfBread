#!/usr/bin/env bash
# 使い方：POB_WORK=<作業フォルダ> SAVELEAK_SRC=<ソース木> bash saveleak.sh（savetest と leakhunt を隔離ビルドで）
export AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W="${POB_WORK:?POB_WORK を指定}"
SNAP_DIR=$W/snap_save bash ${SAVELEAK_SRC:?SAVELEAK_SRC（ソース木）を指定}/measure.sh $W/savetest.txt savetest 1789 300 12 > $W/saveleak.log 2>&1
SNAP_DIR=$W/snap_leak bash ${SAVELEAK_SRC:?SAVELEAK_SRC（ソース木）を指定}/measure.sh $W/leakhunt.txt leakhunt 1789 200 >> $W/saveleak.log 2>&1
rm -rf $W/snap_save $W/snap_leak
echo "SL-END $(date +%T)" >> $W/saveleak.log
