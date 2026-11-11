#!/usr/bin/env bash
# (1045) 突然変異 6 本（使い捨ての木 c99cd_mut・1 本ずつ当てて、試験を回し、戻す）
cd /d/PoB_work/c99cd_mut || exit 2
T=PriceOfBread.SimCore/InstrumentsTradeskip.cs
run() { # 名前 ファイル perl式
  n="$1"; f="$2"; e="$3"
  git checkout -q -- . 
  perl -0pi -e "$e" "$f"
  if git diff --quiet; then echo "$n: パッチが当たらなかった" > /d/PoB_work/c99cd_work/mut_$n.log; return; fi
  git diff > /d/PoB_work/c99cd_work/mut_$n.diff
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c99cd_mutart --filter "FullyQualifiedName~TradeSkipC4dTests" > /d/PoB_work/c99cd_work/mut_$n.log 2>&1
  echo "$n rc=$?" >> /d/PoB_work/c99cd_work/mut_summary.txt
}
: > /d/PoB_work/c99cd_work/mut_summary.txt
run M1 $T 's/Math\.Pow\(rpT \/ p, eps\)/Math.Pow(rpT \/ p, 2.0 * eps)/'
run M2 $T 's/(\s+)if \(tr\[s\]\.access <= 1e-12\) Add\(sn, g, TsCol\.OgAcc0\);/$1if (!(mktOf[s].Stock - mktOf[s].TargetStock > 1e-12)) Add(sn, g, TsCol.OgStock);\n$1else if (tr[s].access <= 1e-12) Add(sn, g, TsCol.OgAcc0);/'
run M3 PriceOfBread.SimCore/EconomyStep.cs 's/costL\[CostLedger\.LaborPowerScans\] \+= np;/costL[CostLedger.LaborPowerScans] += np - 1;/'
run M4 PriceOfBread.SimCore/InstrumentsSellr.cs 's/RentRMulOf\(double rentR\) => rentR > 0 \? rentR : 0\.0;/RentRMulOf(double rentR) => rentR;/'
run M5 $T 's/on && r <= 1\.0 && m\.Stock/on \&\& m.Stock/'
run M6 $T 's/k = 3; v = budgetTerm;/k = 9; v = budgetTerm;/; s/k = 4; v = capTerm;/k = 3; v = capTerm;/; s/k = 9; v = budgetTerm;/k = 4; v = budgetTerm;/'
git checkout -q -- .
echo "all done" >> /d/PoB_work/c99cd_work/mut_summary.txt
echo "rc=0 $(date)" > /d/PoB_work/c99cd_work/mut.mark
