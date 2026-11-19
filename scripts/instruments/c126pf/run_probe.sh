#!/usr/bin/env bash
# (1244) c126pf 段 0 のプローブ：bash run_probe.sh <コミット> <種の始め> <種の終わり> [月数=12]
#   r1238/run_probe_rd.sh の写し＝コミットの SimCore を git archive で D:/PoB_work/c126pf_work/probe/src へ出し、r1238/probe_rd_hook.py（値を読むだけの呼び口と段ごとの数え）を足して隔離ビルド。
#   腕 2 本＝旗 CapSkipSellerPotential 0／1。表 T の candJam・jamPotential・jamCap・jamThrottle と D の死んだ市場を読む。
REV=${1:?}; S0=${2:?}; S1=${3:?}; MO=${4:-12}
R=/c/Users/dirge/Documents/PriceOfBread
SC=D:/PoB_work/wt_c126pf/scripts/instruments/r1238
W=D:/PoB_work/c126pf_work/probe
export TEMP=D:/PoB_work/c126pf_work/tmp TMP=D:/PoB_work/c126pf_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $W/src $W/out D:/PoB_work/c126pf_work/tmp
rm -rf $W/src/PriceOfBread.SimCore $W/src/probe
git -C $R archive $REV PriceOfBread.SimCore | tar -x -C $W/src || { echo ARCHFAIL; exit 1; }
python -I $SC/probe_rd_hook.py $W/src/PriceOfBread.SimCore/BilateralMarket.cs || { echo HOOKFAIL; exit 1; }
mkdir -p $W/src/probe
cp $R/scripts/instruments/r1224/Probe.csproj.txt $W/src/probe/Probe.csproj
cp $SC/probe_rd_Program.cs.txt $W/src/probe/Program.cs
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for F in ${PF_F:-0 1}; do
  POB_COEF="CapSkipSellerPotential=$F" $EXE $S0 $S1 $MO $W/out/f${F} > $W/run_f${F}.txt 2>&1 || { echo RUNFAIL f$F; tail -5 $W/run_f${F}.txt; exit 1; }
done
echo PROBEDONE
