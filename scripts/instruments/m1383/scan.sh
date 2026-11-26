#!/usr/bin/env bash
# (1383) 合流：写した台本の走査の番人（scripts/ を読む試験クラス）。bash scan.sh <作業フォルダ（D:/…）>
WK=${1:?作業フォルダ}; export TEMP=$WK/tmp TMP=$WK/tmp; mkdir -p $TEMP
cd "$(dirname "$0")/../../.."; A=$WK/art
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build_scan.txt 2>&1 || { echo BUILDFAIL; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~Dsc2InstrumentTests|FullyQualifiedName~PaySalesProducerBookTests|FullyQualifiedName~PolicyPoliticsTests|FullyQualifiedName~StateProcureLedgerTests" > $WK/t_scan.txt 2>&1
RC=$?; grep -E "合計|失敗 " $WK/t_scan.txt | head -10; exit $RC
