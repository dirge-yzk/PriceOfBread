#!/usr/bin/env bash
# (1098) c105mc の突然変異を使い捨ての木で 1 本ずつ：書き換え→隔離ビルド→単体の試験だけ→結果を 1 行。
#   bash runmuts.sh <使い捨ての木> <出力の tsv> [μ...]
M="$1"; OUT="$2"; shift 2
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="${M}_art"
printf 'mu\tbuild\ttest\tsummary\n' > "$OUT"
for mu in "$@"; do
  ( cd "$M" && git checkout -q -- PriceOfBread.SimCore/EconomyStep.cs )
  python -X utf8 -I "$S/muts.py" "$M" "$mu" > "$M.$mu.apply.txt" 2>&1 || { printf '%s\tapply-fail\t-\t-\n' "$mu" >> "$OUT"; continue; }
  if ! dotnet build "$M/PriceOfBread.Tests" -c Release --artifacts-path "$ART" -v q -nologo > "$M.$mu.build.txt" 2>&1; then printf '%s\tbuild-fail\t-\t-\n' "$mu" >> "$OUT"; continue; fi
  dotnet test "$M/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --no-build --filter "FullyQualifiedName~鉱山の天井の旗" > "$M.$mu.test.txt" 2>&1
  rc=$?
  sum=$(grep -E "合格|失敗|Passed!|Failed!" "$M.$mu.test.txt" | tail -1 | tr '\t' ' ' | cut -c1-160)
  printf '%s\tok\t%s\t%s\n' "$mu" "$([ $rc = 0 ] && echo 緑 || echo 赤)" "$sum" >> "$OUT"
done
( cd "$M" && git checkout -q -- PriceOfBread.SimCore/EconomyStep.cs )
