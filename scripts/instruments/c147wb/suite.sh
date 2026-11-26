#!/usr/bin/env bash
# (1385) c147wb フルスイート（Category!=FullLength・Ui・FullLength を分けて回す）：bash suite.sh   出力 D:/PoB_work/c147wb_work/suite/
cd D:/PoB_work/wt_c147wb || exit 2
O=D:/PoB_work/c147wb_work/suite; mkdir -p $O
export TEMP=D:/PoB_work/c147wb_work/tmp TMP=D:/PoB_work/c147wb_work/tmp; mkdir -p $TEMP
A=D:/PoB_work/c147wb_art
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull.trx" --results-directory $O > $O/nonfull.txt 2>&1
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/c147wb_art_ui --logger "trx;LogFileName=ui.trx" --results-directory $O > $O/ui.txt 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $O > $O/full.txt 2>&1
