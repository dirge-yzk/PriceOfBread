#!/usr/bin/env bash
# (1390) 合流のフルスイート（Category!=FullLength・Ui・FullLength を分けて回し、--list-tests と突き合わせる）
cd D:/PoB_work/wt_m1390 || exit 2
O=D:/PoB_work/m1390_work/suite; mkdir -p $O
export TEMP=D:/PoB_work/m1390_work/tmp TMP=D:/PoB_work/m1390_work/tmp; mkdir -p $TEMP
A=D:/PoB_work/m1390_work/art
git log -1 --format='%H' > $O/tree.txt
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A --disable-build-servers > $O/build.txt 2>&1 || { echo build-fail; exit 1; }
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --list-tests --filter "Category!=FullLength" > $O/list_nonfull.txt 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category!=FullLength" --logger "trx;LogFileName=nonfull.trx" --results-directory $O > $O/nonfull.txt 2>&1
dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/m1390_work/art_ui --disable-build-servers --logger "trx;LogFileName=ui.trx" --results-directory $O > $O/ui.txt 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "Category=FullLength" --logger "trx;LogFileName=full.trx" --results-directory $O > $O/full.txt 2>&1
grep -hE "合格|失敗!|成功!|Passed!|Failed!" $O/nonfull.txt $O/ui.txt $O/full.txt | tail -5
