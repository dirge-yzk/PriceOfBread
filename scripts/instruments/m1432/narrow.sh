#!/usr/bin/env bash
# (1432) 合流：合わせた木の絞った試験（m1425/narrow.sh の写し・束を ㉘ の走査の番人と、tp・fb・rg・nx が変えた名を Tests/ で grep して当たったクラスへ替えた）。
#   bash narrow.sh <作業フォルダ（D:/…）> <名>
WK=${1:?作業フォルダ}; N=${2:?名}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; A=$WK/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_$N.txt 2>&1 || { echo BUILDFAIL; exit 1; }
F="FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~ValueCountRatchetTests|FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~DormantIdentityTests|FullyQualifiedName~InstrumentHookClosureTests|FullyQualifiedName~InstrumentColumnTests|FullyQualifiedName~SaveRoundTripTests|FullyQualifiedName~ArrearsOnePredicateTests|FullyQualifiedName~ArrearsOpenedRoomTests|FullyQualifiedName~ArrearsUnderLimitPrevMonthTests|FullyQualifiedName~FiscalTests|FullyQualifiedName~FsxTests|FullyQualifiedName~MilitaryTests|FullyQualifiedName~NoContactWarExitTests|FullyQualifiedName~ProtectorTests|FullyQualifiedName~ReligionTensionRemovalTests|FullyQualifiedName~RepayFloorOnlyTests|FullyQualifiedName~RouteTensionTests|FullyQualifiedName~TreasuryLedgerTests|FullyQualifiedName~WarExitFromCapacityTests"
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" --logger "trx;LogFileName=$N.trx" --results-directory $WK/trx > $WK/t_$N.txt 2>&1
RC=$?; grep -E "合計|失敗 |Failed " $WK/t_$N.txt | head -20; exit $RC
