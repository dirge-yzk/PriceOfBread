#!/usr/bin/env bash
# (920) c80eb フルスイート：Category!=FullLength と Category=FullLength を分けて回す（合計を --list-tests と突き合わせる＝ident の腕 t の 2407／3）。
cd D:/PoB_work/wt_c80eb || exit 2
P=PriceOfBread.Tests/PriceOfBread.Tests.csproj; A=D:/PoB_work/c80eb_art; O=D:/PoB_work/c80eb_work
dotnet build $P -c Release --artifacts-path $A --disable-build-servers > $O/suite_build.txt 2>&1 || exit 1
dotnet test $P -c Release --no-build --artifacts-path $A --filter "Category!=FullLength" > $O/suite_nonfull.txt 2>&1
dotnet test $P -c Release --no-build --artifacts-path $A --filter "Category=FullLength" > $O/suite_full.txt 2>&1
