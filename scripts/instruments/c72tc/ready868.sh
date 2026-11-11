#!/usr/bin/env bash
# (868) c72tc 印 tc_ready.mark の中身：試験の盤面と cearn_s の列の試験を旗 0 と旗 1 で回し直して rc を返す（0＝c72ip が merge してよい）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c72tc_work/tc_ready.mark bash scripts/instruments/c72tc/ready868.sh
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
ART=D:/PoB_work/c72tc_art
cd "$REPO" || exit 9
echo "HEAD $(git rev-parse HEAD)"
dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" --filter "FullyQualifiedName~TradeClearingTests|FullyQualifiedName~TradeClearingBoardTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~TechLevelRemovalTests" 2>&1 | grep -E "失敗|成功!|失敗!|合計"
exit ${PIPESTATUS[0]}
