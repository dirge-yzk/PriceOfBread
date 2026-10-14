#!/usr/bin/env bash
export PATH="/c/Program Files/dotnet:$PATH" PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
cd /d/PoB_work/wt_gp
W=/d/PoB_work/gp_329
dotnet build PriceOfBread.Runner -c Release --artifacts-path D:/PoB_work/gp_329/art > $W/can_build.log 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$(ls D:/PoB_work/gp_329/art/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.dll)
for arm in A B; do
  for sd in 1 2; do
    mkdir -p $W/can_$arm$sd/ins
    if [ $arm = B ]; then export POB_COEF="RecipeCostFollowsProductivity=1"; else export POB_COEF=""; fi
    POB_INSTRUMENTS=$W/can_$arm$sd/ins POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル dotnet $EXE discprobe $sd 12 > $W/can_$arm$sd/probe.txt 2>&1
  done
done
echo CANDONE
