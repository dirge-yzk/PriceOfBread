#!/usr/bin/env bash
# (1064) c101vs 突然変異の腕：使い捨ての木（git worktree の detach）で M1〜M5 を 1 本ずつ当て、DependencyCycleTests だけを回す（腕は回さない）。
#   bash mutate_all.sh <使い捨ての木> <成果の置き場> <出力の表>
set -u
T="$1"; ART="$2"; OUT="$3"
: > "$OUT"
for m in M1 M2 M3 M4 M5; do
  git -C "$T" checkout -q -- PriceOfBread.SimCore/Diplomacy.cs
  python "$(dirname "$0")/mutate.py" "$T" "$m" >> "$OUT"
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" -v q -nologo > "$OUT.$m.build.log" 2>&1 || { echo "$m build-failed" >> "$OUT"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$ART" --no-build --filter "FullyQualifiedName~DependencyCycleTests" > "$OUT.$m.test.log" 2>&1
  echo "$m rc=$?" >> "$OUT"
  grep -oE "DependencyCycleTests\.T[0-9][^ (]*(\([^)]*\))?" "$OUT.$m.test.log" | sort -u | sed "s/^/$m 赤 /" >> "$OUT"
  grep -E "合計:" "$OUT.$m.test.log" | sed "s/^/$m /" >> "$OUT"
done
git -C "$T" checkout -q -- PriceOfBread.SimCore/Diplomacy.cs
echo done >> "$OUT"
