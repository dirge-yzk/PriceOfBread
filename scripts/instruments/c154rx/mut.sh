#!/usr/bin/env bash
# (1433) c154rx：突然変異 3 本を順に（使い捨ての木・足した試験だけ）。出力＝D:/PoB_work/c154rx_work/mut_<M>.txt
T=D:/PoB_work/c154rx_work/mutwt
for M in M1 M2 M3; do
  git -C $T checkout -q -- . 
  python -X utf8 -I D:/PoB_work/wt_c154rx/scripts/instruments/c154rx/mut.py $T $M || exit 1
  (cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c154rx_work/mut_art --filter "FullyQualifiedName~ReligionTensionRemovalTests" > D:/PoB_work/c154rx_work/mut_$M.txt 2>&1)
done
git -C $T checkout -q -- .
