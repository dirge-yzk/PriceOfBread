#!/usr/bin/env bash
export PYTHONUTF8=1
cd /d/PoB_work/wt_rs2
OUT=/d/PoB_work/rs2_round/mut/result.txt
: > $OUT
for m in 1 2 3 4 5 6; do
  python /d/PoB_work/rs2_round/mut/apply.py $m || { echo "mutation $m: APPLYFAIL" >> $OUT; continue; }
  r=$(dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/rs2_round/art --filter "FullyQualifiedName~Rs2LordOppositionTests" 2>&1 | grep -E "^(成功|失敗)!|  失敗 |  Failed |エラー" | head -8)
  echo "mutation $m: $r" >> $OUT
  git checkout -- PriceOfBread.SimCore/PolicyPreferences.cs
  touch PriceOfBread.SimCore/PolicyPreferences.cs
done
echo DONE >> $OUT
