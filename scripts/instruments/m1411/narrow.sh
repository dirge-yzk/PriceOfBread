#!/usr/bin/env bash
# (1411) 合流：合わせた木の絞った試験（m1404/narrow.sh の写し・束を c150wz・c150rp の試験と ㉘ の走査の番人へ替えた）。
#   bash narrow.sh <作業フォルダ（D:/…）> <名>
WK=${1:?作業フォルダ}; N=${2:?名}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_$N.txt 2>&1 || { echo BUILDFAIL; exit 1; }
F="FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~InstrumentHookClosureTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~SaveRoundTripTests|FullyQualifiedName~RailProcureNationalTests|FullyQualifiedName~SinkgoodInstrumentTests|FullyQualifiedName~DominanceSpeedSplitTests|FullyQualifiedName~SeedPickTests|FullyQualifiedName~WarPushSpeedTypeTests|FullyQualifiedName~WarExitFromCapacityTests|FullyQualifiedName~WarThemeTests|FullyQualifiedName~CoefficientRelationTests|FullyQualifiedName~TerritoryTests|FullyQualifiedName~RevoltIgnitionTests|FullyQualifiedName~RuleOnsetInstrumentTests|FullyQualifiedName~RuleOrderP1Tests|FullyQualifiedName~ScoreReaderTests|FullyQualifiedName~SecessionLaborLawInstrumentTests|FullyQualifiedName~ModernizeReturnAndScoreTests|FullyQualifiedName~StateDigestToolTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=$N.trx" --results-directory $WK/trx > $WK/t_$N.txt 2>&1
RC=$?; grep -E "合計|失敗 |Failed " $WK/t_$N.txt | head -20; exit $RC
