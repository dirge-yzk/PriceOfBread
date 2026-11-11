#!/usr/bin/env bash
# (1017) 合流：直した 3 か所の試験だけを回す（ScalarDeletion・WriterRatchet・CorncfInstrument・GoodIdGuard）＝腕の前の確かめ（フルスイートは腕の後に最後の木で回す）。
#   bash quick1017.sh <コミット>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/m1017_work/quick; S=$W/src
rm -rf $S && mkdir -p $S && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $S || exit 1
cd $S
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art --disable-build-servers > $W/build.txt 2>&1 || { echo BUILDFAIL; tail -20 $W/build.txt; exit 1; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art --filter "FullyQualifiedName~ScalarDeletionTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~CorncfInstrumentTests|FullyQualifiedName~GoodIdGuardTests" > $W/quick.txt 2>&1; echo "quick rc=$?"
grep -h "合計\|中止\|\[FAIL\]" $W/quick.txt | tail -8
echo QUICKDONE
