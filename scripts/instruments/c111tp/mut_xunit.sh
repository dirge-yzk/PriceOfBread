#!/usr/bin/env bash
# (1138) 突然変異の木ごとに TreatyPortHomeOnlyTests だけを回す（使い捨て）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH" TEMP=D:/PoB_work/c111tp_work/tmp TMP=D:/PoB_work/c111tp_work/tmp
for m in Ma Mb Mc Md; do
  S=D:/PoB_work/c111tp_work/mut/src_c111tp$m
  echo "== $m"
  dotnet test $S/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c111tp_work/mut/art_$m --filter "FullyQualifiedName~TreatyPortHomeOnlyTests|FullyQualifiedName~RuleSharesFormTests.D3" -nologo 2>&1 | grep -E "^\s+(失敗|成功|Failed|Passed) |合格:|Failed!|Passed!|成功!|失敗!" 
done
