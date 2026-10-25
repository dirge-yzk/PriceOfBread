#!/usr/bin/env bash
# 突然変異（PopDecision.cs の例外の行）：番人 IwfFamineGateTests が赤になるか。台本は読むだけの木の外（D:/PoB_work/iwf_546）
export PATH="/c/Program Files/dotnet:$PATH"
W=/d/PoB_work/iwf_546
T=/d/PoB_work/wt_iwf
F=$T/PriceOfBread.SimCore/PopDecision.cs
cp $F $W/PopDecision.orig
OUT=$W/mut_log.txt
: > $OUT
run() { # <名前> <置換前> <置換後>
  cp $W/PopDecision.orig $F
  python $W/mut_apply.py "$F" "$2" "$3" || { echo "$1 APPLYFAIL" >> $OUT; return; }
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/iwf_546/art_test --filter "FullyQualifiedName~IwfFamineGate" 2>&1 | grep -E "失敗!|成功!" | head -1) > $W/mut_one.txt
  echo "$1: $(cat $W/mut_one.txt)" >> $OUT
}
run M1_drop_exempt_clause ' && !(coef.FamineGateIncomeExempt > 0 && incUrban > incRural)) flow = 0;' ') flow = 0;'
run M2_flip_income 'incUrban > incRural)) flow = 0;' 'incUrban < incRural)) flow = 0;'
run M3_ignore_flag '!(coef.FamineGateIncomeExempt > 0 && incUrban > incRural)' '!(incUrban > incRural)'
run M4_ge_income 'incUrban > incRural)) flow = 0;' 'incUrban >= incRural)) flow = 0;'
run M5_flag_always_on '!(coef.FamineGateIncomeExempt > 0 && incUrban > incRural)' '!(coef.FamineGateIncomeExempt >= 0 && incUrban > incRural)'
cp $W/PopDecision.orig $F
echo RESTORED >> $OUT
echo ALLDONE >> $OUT
