#!/bin/bash
export TEMP=D:/PoB_work/c123tc_work/tmp TMP=D:/PoB_work/c123tc_work/tmp
cd D:/PoB_work/c123tc_work/oldtree && dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/c123tc_art_old --filter "FullyQualifiedName~ProductionDiagTests" --logger "trx;LogFileName=mut_old.trx" --results-directory D:/PoB_work/c123tc_work/mut > D:/PoB_work/c123tc_work/mut_old.txt 2>&1
cd D:/PoB_work/wt_c123tc && dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/c123tc_art --filter "FullyQualifiedName~ProductionDiagTests" --logger "trx;LogFileName=mut_new.trx" --results-directory D:/PoB_work/c123tc_work/mut > D:/PoB_work/c123tc_work/mut_new.txt 2>&1
echo done > D:/PoB_work/c123tc_work/mut.mark
