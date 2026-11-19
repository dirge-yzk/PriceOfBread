#!/usr/bin/env bash
# (1238) c125rd 問い 2 のプローブ：bash run_probe_rd.sh <コミット> <種の始め> <種の終わり> [月数=12]
#   コミットの SimCore を git archive で D:/PoB_work/r1238_work/probe/src へ出し、probe_rd_hook.py で呼び口と段ごとの数えを足して隔離ビルド。
#   腕 2 本＝旗 SellOfferExcessStock 0／1（B-1 の旗は 0）。
REV=${1:?}; S0=${2:?}; S1=${3:?}; MO=${4:-12}
R=/c/Users/dirge/Documents/PriceOfBread
W=D:/PoB_work/r1238_work/probe; SC=D:/PoB_work/r1238_work/scripts
export TEMP=D:/PoB_work/r1238_work/tmp TMP=D:/PoB_work/r1238_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $W/src $W/out D:/PoB_work/r1238_work/tmp
rm -rf $W/src/PriceOfBread.SimCore $W/src/probe
git -C $R archive $REV PriceOfBread.SimCore | tar -x -C $W/src || { echo ARCHFAIL; exit 1; }
python -I $SC/probe_rd_hook.py $W/src/PriceOfBread.SimCore/BilateralMarket.cs || { echo HOOKFAIL; exit 1; }
mkdir -p $W/src/probe
cp $R/scripts/instruments/r1224/Probe.csproj.txt $W/src/probe/Probe.csproj
cp $SC/probe_rd_Program.cs.txt $W/src/probe/Program.cs
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for F in ${PF_F:-0 1}; do
  POB_COEF="SellOfferExcessStock=$F;EndowmentCostFollowsAbundance=0" $EXE $S0 $S1 $MO $W/out/f${F} > $W/run_f${F}.txt 2>&1 || { echo RUNFAIL f$F; tail -5 $W/run_f${F}.txt; exit 1; }
done
echo PROBEDONE > $W/probe.done
echo PROBEDONE
