#!/usr/bin/env bash
# (880) c74id 走査の番人 7 組と線の試験の組を、渡した木で回す（Release・隔離ビルド・--disable-build-servers）。旗 1 の使い捨ての木にも同じ台本を当てる。
#   bash guards880.sh <木> <artifacts のフォルダ> [all|routekm]
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; ART=${2:?artifacts}; K=${3:-all}
cd "$T" || exit 9
echo "TREE $T HEAD $(git rev-parse --short HEAD) 未コミット $(git status --porcelain | wc -l)"
git diff --stat | tail -1
G="FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~TechLevelRemovalTests"
L="FullyQualifiedName~CearnTradeColumnsTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~TradeClearingBoardTests|FullyQualifiedName~TradeClearingTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~RouteKmTests"
case "$K" in all) F="$G|$L" ;; routekm) F="FullyQualifiedName~RouteKmTests|FullyQualifiedName~CearnTradeColumnsTests" ;; *) F="$K" ;; esac
dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" --disable-build-servers --filter "$F" > "$ART.test.txt" 2>&1
RC=$?
grep -E "^\s+失敗 |成功!|失敗!|合計|error CS|中止|aborted" "$ART.test.txt" | head -40
grep -A3 "エラー メッセージ" "$ART.test.txt" | head -60
echo "rc=$RC"
exit $RC
