#!/bin/bash
export TEMP=D:/PoB_work/c123tc_work/tmp TMP=D:/PoB_work/c123tc_work/tmp
W=D:/PoB_work/wt_c123tc; O=D:/PoB_work/c123tc_work/oldtree; R=D:/PoB_work/c123tc_work; A=D:/PoB_work/c123tc_art; AO=D:/PoB_work/c123tc_art_old
python $R/mut2.py $O m4; python $R/mut2.py $W m4
(cd $O && dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $AO --filter "FullyQualifiedName~ProductionDiagTests3" > $R/m4_old.txt 2>&1)
(cd $W && dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path $A --filter "FullyQualifiedName~ProductionDiagTests3" > $R/m4_new.txt 2>&1)
(cd $O && git checkout PriceOfBread.SimCore/ProductionDiag.cs); (cd $W && git checkout PriceOfBread.SimCore/ProductionDiag.cs)
python $R/mut2.py $W m5
(cd $W && dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --filter "FullyQualifiedName~ColonialPopTests" > $R/m5_new.txt 2>&1)
(cd $W && git checkout PriceOfBread.SimCore/EconomyStep.cs)
python $R/mut2.py $W m6
(cd $W && dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --filter "FullyQualifiedName~ColonialPopTests" > $R/m6_new.txt 2>&1)
(cd $W && git checkout PriceOfBread.SimCore/EconomyStep.cs)
python $R/mut2.py $O m6
(cd $O && dotnet test PriceOfBread.Tests -c Release --artifacts-path $AO --filter "FullyQualifiedName~ColonialPopTests" > $R/m6_old.txt 2>&1)
(cd $O && git checkout PriceOfBread.SimCore/EconomyStep.cs)
echo done > $R/mut2.mark
