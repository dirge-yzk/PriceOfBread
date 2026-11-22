#!/usr/bin/env bash
# (1298) c134k49 税の鎖の番人の診断の数を出す（使い捨ての木で dMil の Assert を常に失敗させて文言を出すだけ・読むだけ）
#   bash run4.sh <コミット> [SupplySlope の揺らし]
W=D:/PoB_work/c134k49_work/basechk; export TEMP=D:/PoB_work/c134k49_work/tmp TMP=D:/PoB_work/c134k49_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
REV=${1:?}; PERT=${2:-}; TAG=$REV$PERT
T=$W/d_$TAG; rm -rf $T; mkdir -p $T
git -C D:/PoB_work/wt_c134k49 archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T
python -I $W/diagpatch.py "$T/PriceOfBread.Tests/PolicyPoliticsTests.cs" "$PERT" || exit 1
dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $T/art -v q -nologo > $W/dbuild_$TAG.txt 2>&1 || { echo BUILDFAIL; exit 1; }
dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $T/art --no-build --filter "FullyQualifiedName~PolicyPoliticsTests.実税が重い世界ほど生活が苦しく闘争度が高い" > $W/dtest_$TAG.txt 2>&1
grep -o "DIAG.*" $W/dtest_$TAG.txt | head -1 > $W/diag_$TAG.txt
rm -rf $T
echo RUN4DONE $TAG
