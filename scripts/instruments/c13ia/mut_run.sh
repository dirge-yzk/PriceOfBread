#!/usr/bin/env bash
# (811) c13ia 突然変異の実行：使い捨ての木（元の木の写し・1 本）に M1..M7 を 1 本ずつ当てて関係する試験の組を回し、毎回元のファイルへ戻す。出力＝<作業>/<M>.txt
# 使い方: bash mut_run.sh <元の木> <作業フォルダ> [M0 M1 M2 ...]（M0＝変異なしの対照）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH"
SRC="$1"; W="$2"; shift 2
MS="${@:-M0 M1 M2 M3 M4 M5 M6 M7}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
declare -A FILTER=(
 [M0]="FullyQualifiedName~MilitaryTests|FullyQualifiedName~IvfInitialValuesTests|FullyQualifiedName~PopScanTests|FullyQualifiedName~FullChainTests|FullyQualifiedName~ReformPathTests|FullyQualifiedName~ScholarshipTests|FullyQualifiedName~HouseholdProductionTests"
 [M1]="FullyQualifiedName~MilitaryTests"
 [M2]="FullyQualifiedName~IvfInitialValuesTests|FullyQualifiedName~PopScanTests"
 [M3]="FullyQualifiedName~IvfInitialValuesTests|FullyQualifiedName~FullChainTests"
 [M4]="FullyQualifiedName~ReformPathTests|FullyQualifiedName~ScholarshipTests"
 [M5]="FullyQualifiedName~FullChainTests"
 [M6]="FullyQualifiedName~FullChainTests"
 [M7]="FullyQualifiedName~HouseholdProductionTests|FullyQualifiedName~FullChainTests"
)
[ -n "$EXTRA_FILTER" ] && for k in "${!FILTER[@]}"; do FILTER[$k]="${FILTER[$k]}|$EXTRA_FILTER"; done
mkdir -p "$W"; T="$W/tree"; rm -rf "$T"; mkdir -p "$T"
for d in PriceOfBread.SimCore PriceOfBread.App PriceOfBread.Runner PriceOfBread.Tests; do
  mkdir -p "$T/$d"; (cd "$SRC/$d" && tar cf - --exclude=bin --exclude=obj .) | (cd "$T/$d" && tar xf -)
done
cp "$SRC"/*.slnx "$SRC"/*.json "$T"/ 2>/dev/null
SIMREL=PriceOfBread.SimCore/Simulation.cs
for M in $MS; do
  cp "$SRC/$SIMREL" "$T/$SIMREL"
  if [ "$M" = M0 ]; then echo "M0: 変異なしの対照" > "$W/$M.txt"; else
    python "$HERE/mut_apply.py" "$T" "$M" > "$W/$M.txt" 2>&1 || { echo "適用失敗" >> "$W/$M.txt"; continue; }
  fi
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art" --disable-build-servers --filter "${FILTER[$M]}" >> "$W/$M.txt" 2>&1
  echo "rc=$? M=$M 試験の組=${FILTER[$M]}" >> "$W/$M.txt"
  cp "$SRC/$SIMREL" "$T/$SIMREL"
done
