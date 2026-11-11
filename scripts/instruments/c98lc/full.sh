#!/usr/bin/env bash
cd /d/PoB_work/wt_c98lc
W=/d/PoB_work/c98lc_work
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c98lc_art --filter "Category!=FullLength" -nologo -v q > $W/full_nonfull.txt 2>&1
echo nonfull > $W/full1.mark
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c98lc_art --no-build --filter "Category=FullLength" -nologo -v q > $W/full_fl.txt 2>&1
echo fl > $W/full2.mark
