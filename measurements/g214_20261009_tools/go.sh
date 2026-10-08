#!/usr/bin/env bash
# 分離起動の入口：go.sh <腕> <種の数> [POB_COEF]
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
bash /d/PoB_work/g214/tools/batch.sh run "$1" "$2" "${3:--}" > /d/PoB_work/g214/go_$1.log 2>&1 < /dev/null
echo "GO-END rc=$? $(date +%T)" >> /d/PoB_work/g214/go_$1.log
