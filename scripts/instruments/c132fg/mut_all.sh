#!/usr/bin/env bash
# (1283) c132fg 突然変異を使い捨ての木で 1 つずつ（元に戻してから当てる）。使い方: bash mut_all.sh <使い捨ての木> <出力ファイル> <台本の木> <artifacts>
T=${1:?木}; OUT=${2:?出力}; S=${3:?台本の木}; A=${4:?artifacts}
F="PriceOfBread.SimCore/PopDecision.cs PriceOfBread.SimCore/Instruments/InstrumentsUrx.cs PriceOfBread.Tests/FamineGateReturnsRuralTests.cs"
: > "$OUT"
for mu in none mu0 musign mugate musize muoff muinst; do
  (cd "$T" && git checkout -q -- $F)
  [ "$mu" != none ] && python -X utf8 -I "$S/scripts/instruments/c132fg/mutate.py" "$T" "$mu" >> "$OUT.detail" 2>&1
  (cd "$T" && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --filter "FullyQualifiedName~FamineGateReturnsRuralTests" --logger "console;verbosity=normal") > "$OUT.$mu.txt" 2>&1
  echo "== $mu rc=$?" >> "$OUT"
  grep -E "^\s+(合格|失敗|成功|Passed|Failed) " "$OUT.$mu.txt" | sed -E 's/\[[0-9 .a-z]+\]//' | sort -u >> "$OUT"
done
(cd "$T" && git checkout -q -- $F)
echo MUTDONE >> "$OUT"
