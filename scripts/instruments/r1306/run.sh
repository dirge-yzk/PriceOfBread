#!/usr/bin/env bash
# (1306) 使い捨ての木で診断の試験を回す（読むだけ・SimCore は main 22c8d10bf のまま）
W=D:/PoB_work/r1306_work; T=$W/tree
export TEMP=$W/tmp TMP=$W/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1 R1306_OUT=$W/out
cp $W/scripts/R1306Diag.cs $W/scripts/R1306Mut.cs $T/PriceOfBread.Tests/ || exit 1
FILT=${1:-R1306}
dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $W/art -v q -nologo > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $W/art --no-build --filter "FullyQualifiedName~$FILT" > $W/test.txt 2>&1
echo TESTRC=$?
ls -la $W/out
