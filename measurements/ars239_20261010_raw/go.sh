#!/usr/bin/env bash
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
bash /d/PoB_work/ars239/batch.sh run "$1" "$2" "${3:-1}" > /d/PoB_work/ars239/go_$1_$2_${3:-1}.log 2>&1 < /dev/null
echo "GO-END rc=$? $(date +%T)" >> /d/PoB_work/ars239/go_$1_$2_${3:-1}.log
