#!/usr/bin/env bash
# 突然変異（PopDecision.cs の例外の行と比べの関数・追記 3 の形＝自給を値段で足した所得）：番人 IwfFamineGateTests が赤になるか。台本は読むだけの木の外（D:/PoB_work/iwf_546b）
export PATH="/c/Program Files/dotnet:$PATH"
W=/d/PoB_work/iwf_546b
T=/d/PoB_work/wt_iwf
F=$T/PriceOfBread.SimCore/PopDecision.cs
cp $F $W/PopDecision.orig
OUT=$W/mut_log.txt
: > $OUT
run() { # <名前> <置換前> <置換後>
  cp $W/PopDecision.orig $F
  python $T/scripts/instruments/iwf546/mut_apply.py "$F" "$2" "$3" || { echo "$1 APPLYFAIL" >> $OUT; return; }
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/iwf_546b/art_test --filter "FullyQualifiedName~IwfFamineGate" 2>&1 | grep -E "失敗!|成功!" | head -1) > $W/mut_one.txt
  echo "$1: $(cat $W/mut_one.txt)" >> $OUT
}
run M1_drop_exempt_clause ' && !(coef.FamineGateIncomeExempt > 0 && IncomeAdvantage(region, urban, rural, incUrban, incRural))) flow = 0;' ') flow = 0;'
run M2_flip_income '> incRural + rural.Subsistence * basket;' '< incRural + rural.Subsistence * basket;'
run M3_ignore_flag '!(coef.FamineGateIncomeExempt > 0 && IncomeAdvantage(' '!(IncomeAdvantage('
run M4_ge_income '> incRural + rural.Subsistence * basket;' '>= incRural + rural.Subsistence * basket;'
run M5_flag_always_on 'FamineGateIncomeExempt > 0 && IncomeAdvantage' 'FamineGateIncomeExempt >= 0 && IncomeAdvantage'
run M6_drop_subsistence_term 'incRural + rural.Subsistence * basket;' 'incRural;'
run M7_price_ignored 'double basket = MinimumWageFloor.BasketPerPerson(region, MinimumWageLaw.Subsistence);' 'double basket = 1.0;'
cp $W/PopDecision.orig $F
echo RESTORED >> $OUT
echo ALLDONE >> $OUT
