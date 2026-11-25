cd D:/PoB_work/wt_m1369
export AVALONIA_TELEMETRY_OPTOUT=1 TEMP=D:/PoB_work/m1369_work/suite/tmp TMP=D:/PoB_work/m1369_work/suite/tmp
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/m1369_work/suite/art --no-build --filter "FullyQualifiedName~GoodIdGuardTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~Dsc2InstrumentTests|FullyQualifiedName~PaySalesProducerBookTests|FullyQualifiedName~PolicyPoliticsTests|FullyQualifiedName~StateProcureLedgerTests" 2>&1 | grep -E '成功!|失敗!|\[FAIL\]'
