#!/usr/bin/env bash
# (1218) c122r2 段0：全体の世界のプローブを旗 0 と旗 1 で同じ種に回す（コミットしない・合流が写す）。
#   bash run_probe.sh <種の始め> <種の終わり> [月数=12]
#   プローブの本体は D:/PoB_work/d1218_work/probe（Program.cs・Probe.csproj）・SimCore は main 245780b63 の git archive の写し。
#   旗 1 は POB_COEF=EndowmentCostFollowsAbundance=1（国ごとの World.Coef の構築の時に置く＝播種で割る旗）。
S0=${1:?種の始め}; S1=${2:?種の終わり}; MO=${3:-12}
W=D:/PoB_work/d1218_work
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
dotnet build $W/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
unset POB_COEF
$EXE $S0 $S1 $MO $W/flag0_s${S0}_${S1}.tsv || exit 1
POB_COEF="EndowmentCostFollowsAbundance=1" $EXE $S0 $S1 $MO $W/flag1_s${S0}_${S1}.tsv || exit 1
echo RUNDONE
