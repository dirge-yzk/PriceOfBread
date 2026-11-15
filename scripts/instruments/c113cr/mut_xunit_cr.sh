#!/usr/bin/env bash
# (1151) c113cr 試験の突然変異（事前登録 §3）：使い捨ての木に 1 本ずつ当て、RulePowerEngineTests だけを回して合格と失敗を書く（CTL0＝突然変異なし）。
# 使い方: bash mut_xunit_cr.sh <元の木> <作業フォルダ> <突然変異...>
SRCT="$1"; W="$2"; shift 2
export TEMP=D:/PoB_work/c113cr_work/tmp TMP=D:/PoB_work/c113cr_work/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$W" "$TEMP"
for M in "$@"; do
  T="$W/xtree_$M"; rm -rf "$T" && mkdir -p "$T"
  tar -cf - -C "$SRCT" --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs --exclude=PriceOfBread.App --exclude=PriceOfBread.Tests.Ui . | tar -xf - -C "$T"
  if [ "$M" != "CTL0" ]; then python -X utf8 -I "$S/mutate_cr.py" "$T" "$M" > "$W/xmut_$M.log" 2>&1 || { echo "$M MUTFAIL" >> "$W/xsummary.txt"; continue; }; fi
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --disable-build-servers --filter "FullyQualifiedName~RulePowerEngineTests" > "$W/xtest_$M.log" 2>&1
  echo "$M rc=$? $(grep -E '合格:|Passed!|Failed!' "$W/xtest_$M.log" | tail -1 | tr -s ' ')" >> "$W/xsummary.txt"
  grep -E '\[FAIL\]' "$W/xtest_$M.log" | sed 's/.*RulePowerEngineTests\./  FAIL /' >> "$W/xsummary.txt"
  rm -rf "$T"
done
echo "XEND $(date +%T)" >> "$W/xsummary.txt"
