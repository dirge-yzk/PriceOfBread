#!/usr/bin/env bash
# (560) 合流の写し：rs3558 の突然変異の台本を合流の木（worktree D:/PoB_work/m560/wt・隔離ビルド D:/PoB_work/m560/art）へ向けた物。別の木で回すときはパスを直す。
export PYTHONUTF8=1
export PATH="/c/Program Files/dotnet:$PATH"
cd /d/PoB_work/m560/wt
OUT=/d/PoB_work/m560/mut_rs3_result.txt
: > $OUT
for m in 1 2 3 4 5 6 7 8; do
  python scripts/instruments/m560/mut_apply.py $m || { echo "mutation $m: APPLYFAIL" >> $OUT; continue; }
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/m560/art --filter "FullyQualifiedName~Rs3LordReverseGainTests" > /d/PoB_work/m560/mut_$m.log 2>&1
  r=$(grep -E "^(成功|失敗)!|^\s+(失敗|Failed) " /d/PoB_work/m560/mut_$m.log | head -12 | tr '\n' '|')
  echo "mutation $m: $r" >> $OUT
  git checkout -- PriceOfBread.SimCore/PolicyPreferences.cs PriceOfBread.SimCore/FactionSnapshot.cs
  touch PriceOfBread.SimCore/PolicyPreferences.cs PriceOfBread.SimCore/FactionSnapshot.cs
done
echo DONE >> $OUT
