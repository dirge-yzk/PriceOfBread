#!/usr/bin/env bash
# (944) c83hd 走査の番人（㉘）と相続の試験：器のコミットの後に回す。出力の頭に HEAD を書く。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c83hd_work/scan.mark bash scripts/instruments/c83hd/scan944.sh <出力の名>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
TAG="${1:-scan}"; W=D:/PoB_work/c83hd_work; A=$W/art_scan; O=$W/$TAG.txt; mkdir -p $W
cd "$REPO" || exit 9
echo "HEAD $(git rev-parse HEAD) 未コミット $(git status --porcelain -- PriceOfBread.SimCore PriceOfBread.Tests | wc -l)" > $O
F="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~EmptyPopDebtToHeirsTests|FullyQualifiedName~EmptyPopDebtFirstTests|FullyQualifiedName~EmptyPopEstateTests|FullyQualifiedName~BillBlockedColumnsTests|FullyQualifiedName~HeirDebtSettledMovedTests"
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "$F" > $W/${TAG}_raw.txt 2>&1
RC=$?
echo "rc=$RC" >> $O
grep -h "合計\|成功!\|失敗!\|中止\|Total tests\|Passed!\|Failed!" $W/${TAG}_raw.txt | tail -4 >> $O
grep -h "^\s*失敗 \|\[FAIL\]" $W/${TAG}_raw.txt | head -20 >> $O
cat $O
exit $RC
