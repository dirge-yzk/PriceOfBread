#!/usr/bin/env bash
# (558) 線 rs3 の突然変異：1 本ずつ入れて Rs3LordReverseGainTests を走らせ、git checkout で戻す。台本の前提＝worktree D:/PoB_work/wt_rs3・隔離ビルド D:/PoB_work/rs3_round/art。
export PYTHONUTF8=1
cd /d/PoB_work/wt_rs3
OUT=/d/PoB_work/rs3_round/mut_result.txt
: > $OUT
for m in 1 2 3 4 5 6 7; do
  python scripts/instruments/rs3558/mut_apply.py $m || { echo "mutation $m: APPLYFAIL" >> $OUT; continue; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/rs3_round/art --filter "FullyQualifiedName~Rs3LordReverseGainTests" > /d/PoB_work/rs3_round/mut_$m.log 2>&1
  r=$(grep -E "^(成功|失敗)!|^\s+(失敗|Failed) " /d/PoB_work/rs3_round/mut_$m.log | head -12 | tr '\n' '|')
  echo "mutation $m: $r" >> $OUT
  git checkout -- PriceOfBread.SimCore/PolicyPreferences.cs PriceOfBread.SimCore/FactionSnapshot.cs
  touch PriceOfBread.SimCore/PolicyPreferences.cs PriceOfBread.SimCore/FactionSnapshot.cs
done
echo DONE >> $OUT
