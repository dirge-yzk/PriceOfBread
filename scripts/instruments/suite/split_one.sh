#!/usr/bin/env bash
# (1426) split_suite.sh が xargs -P 2 で呼ぶ内部の 1 本：split_one.sh <H|R> <作業フォルダ> <artifacts> <フィルタ>
N=$1; WK=$2; A=$3; F=$4
export AVALONIA_TELEMETRY_OPTOUT=1 TEMP=$WK/tmp$N TMP=$WK/tmp$N
cd "$(dirname "$0")/../../.."
dotnet test PriceOfBread.Tests -c Release --artifacts-path "$A" --no-build --filter "$F" --logger "trx;LogFileName=tests_$N.trx" --results-directory "$WK/trx" > "$WK/t_$N.txt" 2>&1
echo $? > "$WK/rc_$N.txt"; date +%s > "$WK/end_$N.txt"
