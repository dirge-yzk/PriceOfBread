#!/usr/bin/env bash
# (801) c11fl 突然変異の腕の準備：測る木（旗の既定 1 の木）の写しに Mu1〜Mu5 を順に当てて prep（写しを取る）する。木そのものは変えない。
#   使い方: bash scripts/instruments/c11fl/mut_arms.sh <測る木>
#   腕は別に  POB_WORK=D:/PoB_work/c11fl_mut POB_REPO=<測る木> INS=1 bash scripts/instruments/batch.sh run c11flMu1 2  のように回す（run_in_background）。
#   機構の試験は  dotnet test <D:/PoB_work/c11fl_mut/src_c11flMu1>/PriceOfBread.Tests/PriceOfBread.Tests.csproj --filter FullyQualifiedName~CapitalDividendHostRentTests
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c11fl_mut PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
T="${1:?木}"; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
mkdir -p $POB_WORK
for m in Mu1 Mu2 Mu3 Mu4 Mu5; do
  MU=$m bash $D/../batch.sh prep c11fl$m "dir:$T" plain $D/mutate.py || exit 1
done
echo MUTPREPDONE
