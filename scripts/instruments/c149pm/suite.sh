#!/usr/bin/env bash
# (1400) c149pm フルスイート（Category!=FullLength・Ui・FullLength を分けて回し、--list-tests と突き合わせる）：bash suite.sh   出力 D:/PoB_work/c149pm_work/suite/
cd D:/PoB_work/wt_c149pm || exit 2
O=D:/PoB_work/c149pm_work/suite; mkdir -p $O
export TEMP=D:/PoB_work/c149pm_work/tmp TMP=D:/PoB_work/c149pm_work/tmp; mkdir -p $TEMP
A=D:/PoB_work/c149pm_art
echo "start $(date '+%F %T') head=$(git rev-parse --short HEAD)" > $O/times.txt
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A > $O/build.txt 2>&1 || exit 3
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests > $O/list.txt 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull.trx" --results-directory $O > $O/nonfull.txt 2>&1
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/c149pm_art_ui --logger "trx;LogFileName=ui.trx" --results-directory $O > $O/ui.txt 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $O > $O/full.txt 2>&1
echo "end $(date '+%F %T')" >> $O/times.txt
