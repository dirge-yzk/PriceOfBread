#!/usr/bin/env bash
# (1404) 合流：合わせた木の絞った試験（WriterRatchet の取り直しの前の値の読みと、線の試験・番人・写した台本の GoodIdGuard）。
#   bash narrow.sh <作業フォルダ（D:/…）> <名>
WK=${1:?作業フォルダ}; N=${2:?名}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_$N.txt 2>&1 || { echo BUILDFAIL; exit 1; }
F="FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~PriceMonthStepTests|FullyQualifiedName~PriceStepDaysTests|FullyQualifiedName~RandomOnlyClimateEpidemicTests|FullyQualifiedName~MonopolyPushTests|FullyQualifiedName~WarInstrumentTests|FullyQualifiedName~WarExitFromCapacityTests|FullyQualifiedName~StateDigestToolTests|FullyQualifiedName~LandShareTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~InstrumentHookClosureTests|FullyQualifiedName~OnDoubleReadScanTests|FullyQualifiedName~TechLevelRemovalTests|FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~SaveRoundTripTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=$N.trx" --results-directory $WK/trx > $WK/t_$N.txt 2>&1
RC=$?; grep -E "合計|失敗 |Failed " $WK/t_$N.txt | head -20; exit $RC
