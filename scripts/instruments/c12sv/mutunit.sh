#!/usr/bin/env bash
# (807) 機構の試験の突然変異（使い捨ての木）。M1u 票を 1 に固定／M2u 産出／M3u 二乗／M4u 和 0 で 1 を返す／M5u 平均を等重みへ
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
T=D:/PoB_work/c12sv_mutunit/tree; A=D:/PoB_work/c12sv_mutunit/art
OUT=D:/PoB_work/c12sv_mutunit/result2.txt; : > $OUT
for m in ${MUTS:-M3u M4u M5u}; do
  T=D:/PoB_work/c12sv_mutunit/tree_$m; rm -rf $T; mkdir -p $T && (cd D:/PoB_work/wt_c12sv && git archive HEAD) | tar -x -C $T
  python -X utf8 - $T $m <<'PY'
import sys
t,m=sys.argv[1],sys.argv[2]
p=t+'/PriceOfBread.SimCore/MultiNationSim.cs'
s=open(p,'rb').read().decode()
def rep(o,n):
    global s
    assert s.count(o)==1,o; s=s.replace(o,n)
if m=='M1u': rep("internal static double SilverVoteWeight(double demandAu, bool weighted) => !weighted ? 1.0 : (demandAu > 0 && demandAu < double.PositiveInfinity ? demandAu : 0.0);","internal static double SilverVoteWeight(double demandAu, bool weighted) => 1.0;")
if m=='M2u': rep("double wState = SilverVoteWeight(dAu, demandWeighted);","double wState = SilverVoteWeight(mAu.LastProduced, demandWeighted);")
if m=='M3u': rep("double wState = SilverVoteWeight(dAu, demandWeighted);","double wState = SilverVoteWeight(dAu * dAu, demandWeighted);")
if m=='M4u': rep("(weightSum > 1e-9 ? System.Math.Exp(weightedLnSum / weightSum) : 0)","(weightSum > 1e-9 ? System.Math.Exp(weightedLnSum / weightSum) : 1.0)")
if m=='M5u': rep("=> weighted ? (weightSum > 1e-9","=> false ? (weightSum > 1e-9")
open(p,'wb').write(s.encode())
PY
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers -v q > D:/PoB_work/c12sv_mutunit/build_$m.txt 2>&1 || { echo "$m BUILDFAIL" >> $OUT; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "FullyQualifiedName~SilverParityDemandWeighted" > D:/PoB_work/c12sv_mutunit/test_$m.txt 2>&1
  echo "$m $(grep -E '^(失敗|成功)!' D:/PoB_work/c12sv_mutunit/test_$m.txt | head -1) :: $(grep -E '^\s+失敗 ' D:/PoB_work/c12sv_mutunit/test_$m.txt | sed 's/ \[.*//' | tr '\n' ';')" >> $OUT
done
echo MUTUNITDONE >> $OUT
