#!/usr/bin/env bash
# (1130) c110lo の突然変異を xUnit（RailLocoDiscoveryTests の 3 本）で読む。使い捨ての木 1 本（git archive）で 1 つずつ：書き換え→隔離ビルド→試験→1 行。
#   bash runmuts_x.sh <コミット> <作業フォルダ> <出力の tsv> [変異...]
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
export TEMP=D:/PoB_work/c110lo_work/tmp TMP=D:/PoB_work/c110lo_work/tmp
R="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"; S="$R/scripts/instruments/c110lo"
H="${1:?コミット}"; W="${2:?作業フォルダ}"; OUT="${3:?出力}"; shift 3
T="$W/xtree"; ART="$W/xart"; mkdir -p "$W"
printf 'mu\tbuild\tT1\tT2\tT3\tsummary\n' > "$OUT"
for mu in "$@"; do
  rm -rf "$T"; mkdir -p "$T"; ( cd "$R" && git archive "$H" ) | tar -x -C "$T"
  python -X utf8 -I "$S/muts.py" "$T" "$mu" > "$W/x.$mu.apply.txt" 2>&1 || { printf '%s\tapply-fail\t-\t-\t-\t-\n' "$mu" >> "$OUT"; continue; }
  if ! dotnet build "$T/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --disable-build-servers -v q -nologo > "$W/x.$mu.build.txt" 2>&1; then printf '%s\tbuild-fail\t-\t-\t-\t-\n' "$mu" >> "$OUT"; continue; fi
  dotnet test "$T/PriceOfBread.Tests" -c Release --artifacts-path "$ART" --no-build --filter "FullyQualifiedName~RailLocoDiscoveryTests" > "$W/x.$mu.test.txt" 2>&1
  c() { grep -q "RailLocoDiscoveryTests.$1.*\[FAIL\]" "$W/x.$mu.test.txt" && echo 赤 || echo 緑; }
  sum=$(grep -E "合格|Passed!|Failed!" "$W/x.$mu.test.txt" | tail -1 | tr '\t' ' ' | cut -c1-120)
  printf '%s\tok\t%s\t%s\t%s\t%s\n' "$mu" "$(c T1_)" "$(c T2_)" "$(c T3_)" "$sum" >> "$OUT"
done
rm -rf "$T"
