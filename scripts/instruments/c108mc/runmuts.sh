#!/usr/bin/env bash
# (1119) c108mc の突然変異（c105mc/runmuts.sh の写し・単体の試験 2 本の赤と緑を別の列に）。使い捨ての木で 1 本ずつ：書き換え→隔離ビルド→単体の試験だけ→結果を 1 行。
#   bash runmuts.sh <使い捨ての木> <出力の tsv> [μ...]
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
export TEMP=D:/PoB_work/c108mc_work/tmp TMP=D:/PoB_work/c108mc_work/tmp
M="$1"; OUT="$2"; shift 2
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ART="${M}_art"
printf 'mu\tbuild\ttest\tunit1_floor\tunit2_built0\tsummary\n' > "$OUT"
for mu in "$@"; do
  ( cd "$M" && git checkout -q -- PriceOfBread.SimCore/EconomyStep.cs )
  python -X utf8 -I "$S/muts.py" "$M" "$mu" > "$M.$mu.apply.txt" 2>&1 || { printf '%s\tapply-fail\t-\t-\t-\t-\n' "$mu" >> "$OUT"; continue; }
  if ! dotnet build "$M/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --disable-build-servers -v q -nologo > "$M.$mu.build.txt" 2>&1; then printf '%s\tbuild-fail\t-\t-\t-\t-\n' "$mu" >> "$OUT"; continue; fi
  dotnet test "$M/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --no-build --filter "FullyQualifiedName~鉱山の天井の旗" > "$M.$mu.test.txt" 2>&1
  rc=$?
  u1=$(grep -c "旗1で建てた分の床.*\[FAIL\]" "$M.$mu.test.txt"); u2=$(grep -c "建てた分0の道.*\[FAIL\]" "$M.$mu.test.txt")
  sum=$(grep -E "合格|失敗|Passed!|Failed!" "$M.$mu.test.txt" | tail -1 | tr '\t' ' ' | cut -c1-160)
  printf '%s\tok\t%s\t%s\t%s\t%s\n' "$mu" "$([ $rc = 0 ] && echo 緑 || echo 赤)" "$([ "$u1" = 0 ] && echo 緑 || echo 赤)" "$([ "$u2" = 0 ] && echo 緑 || echo 赤)" "$sum" >> "$OUT"
done
( cd "$M" && git checkout -q -- PriceOfBread.SimCore/EconomyStep.cs )
