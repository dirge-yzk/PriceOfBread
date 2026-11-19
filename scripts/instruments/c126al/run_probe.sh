#!/usr/bin/env bash
# (1242) 段 0 のプローブ：器のコミットの木（git archive の写し）をビルド→旗 0 と旗 1 で種 1〜10。
# 使い方: bash run_probe.sh <写しの木（D:/ の形）> <作業フォルダ（D:/ の形）>
T=${1:?木}; W=${2:?作業}
mkdir -p $W/src/probe $W/out $W/tmp
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
cp $T/scripts/instruments/c126al/probe_Program.cs.txt $W/src/probe/Program.cs
cat > $W/src/probe/Probe.csproj <<XEOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable>
  <AssemblyName>PriceOfBread.Tests</AssemblyName><ServerGarbageCollection>true</ServerGarbageCollection><NoWarn>CS8632</NoWarn></PropertyGroup>
  <ItemGroup><ProjectReference Include="$T/PriceOfBread.SimCore/PriceOfBread.SimCore.csproj" /></ItemGroup>
</Project>
XEOF
dotnet build $W/src/probe/Probe.csproj -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
EXE=$W/art/bin/Probe/release/PriceOfBread.Tests.exe
for s in $(seq 1 10); do
  unset POB_COEF; $EXE $s $W/out/f0_s$s.tsv > /dev/null || echo FAIL0 $s >> $W/out/fail.txt
  POB_COEF=FoodProcessingNoEngineer=1 $EXE $s $W/out/f1_s$s.tsv > /dev/null || echo FAIL1 $s >> $W/out/fail.txt
done
echo PROBEALL
