#!/usr/bin/env bash
# (1420) フルスイート 1 回：Category!=FullLength（Tests・Ui）→ FullLength（Tests）。trx は D:/PoB_work/c152ht_work/suite/
cd D:/PoB_work/wt_c152ht; O=D:/PoB_work/c152ht_work/suite; mkdir -p $O
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c152ht_art --no-restore --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull_Tests.trx" --results-directory $O > $O/nonfull_Tests.log 2>&1; echo "rc=$?" > $O/nonfull_Tests.mark
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path D:/PoB_work/c152ht_art --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull_Ui.trx" --results-directory $O > $O/nonfull_Ui.log 2>&1; echo "rc=$?" > $O/nonfull_Ui.mark
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c152ht_art --no-restore --filter "Category=FullLength" --logger "trx;LogFileName=full_Tests.trx" --results-directory $O > $O/full_Tests.log 2>&1; echo "rc=$?" > $O/full_Tests.mark
echo done > $O/all.mark
