#!/usr/bin/env bash
# (874) c73tc 印 tc_ready.mark の中身：cearn_s の新しい列の試験と、ソースを走査する番人 7 組・TradeClearing 系・ImportPayerLive 系・InstrumentsTests を回して rc を返す（0＝c73ip が merge してよい）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c73tc_work/tc_ready.mark bash scripts/instruments/c73tc/ready874.sh
#   元は c72tc/ready868.sh（作業フォルダだけ c73tc・--disable-build-servers を足した＝VBCSCompiler を増やさない）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
ART=D:/PoB_work/c73tc_art
cd "$REPO" || exit 9
echo "HEAD $(git rev-parse HEAD)"
dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" --disable-build-servers --filter "FullyQualifiedName~TradeClearingTests|FullyQualifiedName~TradeClearingBoardTests|FullyQualifiedName~CearnTradeColumnsTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~TechLevelRemovalTests" 2>&1 | grep -E "失敗|成功!|失敗!|合計"
exit ${PIPESTATUS[0]}
