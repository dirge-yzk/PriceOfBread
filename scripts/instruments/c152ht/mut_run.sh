#!/usr/bin/env bash
# (1420) μ1〜μ4 を 1 本ずつ当てて新しい試験だけ回し、必ず戻す。出力 D:/PoB_work/c152ht_work/mut<n>.log
cd D:/PoB_work/wt_c152ht
for n in 1 2 3 4; do
  python -I scripts/instruments/c152ht/mutate.py $n || { echo "mutate fail $n" > D:/PoB_work/c152ht_work/mut$n.log; git checkout -- PriceOfBread.SimCore; continue; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c152ht_art --filter "FullyQualifiedName~DiscoveryContactMechanismTests" --logger "console;verbosity=normal" > D:/PoB_work/c152ht_work/mut$n.log 2>&1
  echo "rc=$?" >> D:/PoB_work/c152ht_work/mut$n.log
  git checkout -- PriceOfBread.SimCore
done
echo done > D:/PoB_work/c152ht_work/mut.mark
