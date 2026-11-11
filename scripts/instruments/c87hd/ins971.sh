#!/usr/bin/env bash
# (971) c87hd 計器の試験の束（㊴・旗 1 の木でフルスイートの前）。出力の頭に HEAD。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c87hd_work/ins.mark bash scripts/instruments/c87hd/ins971.sh <出力の名>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
TAG="${1:-ins}"; W=D:/PoB_work/c87hd_work; A=$W/art_ins; O=$W/$TAG.txt; mkdir -p $W
cd "$REPO" || exit 9
echo "HEAD $(git rev-parse HEAD) 未コミット $(git status --porcelain -- PriceOfBread.SimCore PriceOfBread.Tests | wc -l)" > $O
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "FullyQualifiedName~Instrument|FullyQualifiedName~BillGateHeirDebtTests|FullyQualifiedName~EmptyPopDebt|FullyQualifiedName~DormantIdentityTests" > $W/${TAG}_raw.txt 2>&1
RC=$?
echo "rc=$RC" >> $O
grep -h "合計\|成功!\|失敗!\|中止\|Total tests" $W/${TAG}_raw.txt | tail -3 >> $O
grep -h "^\s*失敗 " $W/${TAG}_raw.txt | head -20 >> $O
cat $O
exit $RC
