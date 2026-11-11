#!/usr/bin/env bash
# (820) c65op 突然変異の写しを prep し（batch.sh prep＋mutate.py）、機構の試験 HabitLogisticTests・HabitTests を回す（腕は回さない＝mut_arms.sh の腕の段の前に）。
#   使い方: bash scripts/instruments/c65op/mut_unit.sh <測る木> <Mu1 Mu2 ...>（Bash の run_in_background で）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c65op_work/mut PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
T="${1:?木}"; shift; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
mkdir -p $POB_WORK
for m in "$@"; do
  MU=$m bash $D/../batch.sh prep c65op$m "dir:$T" plain $D/mutate.py || exit 1
  S=$POB_WORK/src_c65op$m
  dotnet build $S/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $POB_WORK/art_$m --disable-build-servers > $POB_WORK/tbuild_$m.txt 2>&1 || { echo "== $m BUILDFAIL"; continue; }
  dotnet test $S/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $POB_WORK/art_$m --filter "FullyQualifiedName~HabitLogisticTests|FullyQualifiedName~HabitTests" > $POB_WORK/tests_$m.txt 2>&1
  echo "== $m tests rc=$?"; grep -E "^\s+失敗 PriceOfBread|^\s+Failed PriceOfBread" $POB_WORK/tests_$m.txt | sed 's/\[.*//' | sort | uniq -c; grep -E "失敗:|合格:|合計:|Failed:|Passed:|Total" $POB_WORK/tests_$m.txt | tail -2
done
echo MUTUNITDONE
