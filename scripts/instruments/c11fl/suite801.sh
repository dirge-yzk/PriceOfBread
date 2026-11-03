#!/usr/bin/env bash
# (801) c11fl フルスイート（Release・Category!=FullLength と =FullLength を分けて回す）。10 種のバッチと並べない（README §1-2）。測る木は腕 B の写し src_c11flB（コミット a965b990 の git archive）。
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=D:/PoB_work/c11fl/src_c11flB; W=D:/PoB_work/c11fl
cd $T
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_suite --disable-build-servers > $W/suite_build.txt 2>&1 || { echo BUILDFAIL; exit 1; }
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category!=FullLength" > $W/suite_nonfull.txt 2>&1; echo "nonfull rc=$? $(( $(date +%s) - T0 ))s"
T0=$(date +%s); dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/art_suite --filter "Category=FullLength" > $W/suite_full.txt 2>&1; echo "full rc=$? $(( $(date +%s) - T0 ))s"
grep -h "合計\|中止\|Total tests\|aborted" $W/suite_nonfull.txt $W/suite_full.txt | tail -4
