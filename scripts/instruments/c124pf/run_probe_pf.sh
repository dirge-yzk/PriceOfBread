#!/usr/bin/env bash
# (1229) c124pf 段 0 のプローブ：bash run_probe_pf.sh <木> <種の始め> <種の終わり> [月数=12]
#   木の作業ツリーの SimCore（旗 SellOfferExcessStock を入れた物）を D:/PoB_work/c124pf_work/probe/src へ写し、ProbeDeal を足して隔離ビルド。
#   腕 4 本＝旗 SellOfferExcessStock 0／1 × B-1 の旗 EndowmentCostFollowsAbundance 0／1。
TREE=${1:?木}; S0=${2:?}; S1=${3:?}; MO=${4:-12}
W=D:/PoB_work/c124pf_work/probe
export TEMP=D:/PoB_work/c124pf_work/tmp TMP=D:/PoB_work/c124pf_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $W/src $W/out D:/PoB_work/c124pf_work/tmp
rm -rf $W/src/PriceOfBread.SimCore $W/src/probe
cp -r $TREE/PriceOfBread.SimCore $W/src/PriceOfBread.SimCore
rm -rf $W/src/PriceOfBread.SimCore/bin $W/src/PriceOfBread.SimCore/obj
python -I $TREE/scripts/instruments/c124pf/probe_pf_hook.py $W/src/PriceOfBread.SimCore/BilateralMarket.cs || { echo HOOKFAIL; exit 1; }
mkdir -p $W/src/probe
cp $TREE/scripts/instruments/r1224/Probe.csproj.txt $W/src/probe/Probe.csproj
cp $TREE/scripts/instruments/c124pf/probe_pf_Program.cs.txt $W/src/probe/Program.cs
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for F in ${PF_F:-0 1}; do for B in ${PF_B:-0 1}; do
  POB_COEF="SellOfferExcessStock=$F;EndowmentCostFollowsAbundance=$B" $EXE $S0 $S1 $MO $W/out/f${F}b${B} > $W/run_f${F}b${B}.txt 2>&1 || { echo RUNFAIL f$F b$B; exit 1; }
done; done
echo PROBEDONE > $W/probe.done
echo PROBEDONE
