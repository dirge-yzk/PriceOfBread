#!/usr/bin/env bash
# (815) c14fl 機構の試験を突然変異の写しで回す（読むだけ）。使い方: bash mut_unit.sh <出力> <Mu1 Mu2 ...>（写しは e0518931 の git archive に mutate.py を当てた D:/PoB_work/c14fl_work/mutu/src_c14flMuN）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
OUT=${1:?出力}; shift
for m in "$@"; do
  T=D:/PoB_work/c14fl_work/mutu/src_c14fl$m
  echo "## $m" >> $OUT
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c14fl_work/mutu/art_unit_$m --disable-build-servers --filter "FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~CapitalDividendHostRentTests" 2>&1 | grep -E "^\s+(失敗|Failed) |合計|Total" >> $OUT
done
echo MUTUNITDONE >> $OUT
