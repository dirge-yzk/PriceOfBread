#!/usr/bin/env bash
# (1056) 突然変異を 1 本ずつ当てて ImposeOncePerCollapseTests を回す（使い捨ての木 wt_mut）
T=D:/PoB_work/c100ic_work/wt_mut; O=/d/PoB_work/c100ic_work/mut_out; mkdir -p $O
for m in M1 M2 M3 M4; do
  git -C $T checkout -q -- PriceOfBread.SimCore/Diplomacy.cs
  python /d/PoB_work/c100ic_work/mutate.py $T/PriceOfBread.SimCore/Diplomacy.cs $m || { echo "$m 当てられない"; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c100ic_mart --disable-build-servers --filter "FullyQualifiedName~ImposeOncePerCollapseTests|FullyQualifiedName~ImposeCrownFromLoserTests" > $O/$m.txt 2>&1
  echo "$m rc=$? $(grep -E '合計:|失敗 ' $O/$m.txt | tr '\n' ' ' | cut -c1-400)"
done
git -C $T checkout -q -- PriceOfBread.SimCore/Diplomacy.cs
