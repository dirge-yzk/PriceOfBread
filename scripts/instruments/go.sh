#!/usr/bin/env bash
# 使い方：POB_WORK=<作業フォルダ> bash go.sh <腕> <種の数> [種の起点]（batch.sh run を前面で回しログを残す）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
bash "$(dirname "$0")/batch.sh" run "$1" "$2" "${3:-1}" > $POB_WORK/go_$1_$2_${3:-1}.log 2>&1 < /dev/null
echo "GO-END rc=$? $(date +%T)" >> $POB_WORK/go_$1_$2_${3:-1}.log
