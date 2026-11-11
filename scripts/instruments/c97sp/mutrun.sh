#!/usr/bin/env bash
# (1029) 突然変異 M1〜M5 を使い捨ての木で順に適用し、ClearingLowerBoundTests を回して結果の全文を書く。使い方：bash mutrun.sh <使い捨ての木> <出力> <隔離ビルド>
T="$1"; OUT="$2"; ART="$3"
: > "$OUT"
for M in M1 M2 M3 M4 M5; do
  git -C "$T" checkout -q -- PriceOfBread.SimCore/BilateralMarket.cs
  python -I "$(dirname "$0")/mut.py" "$T" $M || { echo "$M 適用失敗" >> "$OUT"; continue; }
  echo "==== $M ====" >> "$OUT"
  (cd "$T" && dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" --filter "FullyQualifiedName~ClearingLowerBoundTests" -nologo) > "$OUT.$M.log" 2>&1
  echo "rc=$?" >> "$OUT"
  grep -E "失敗 |成功!|失敗!|error" "$OUT.$M.log" | cut -c1-200 >> "$OUT"
done
git -C "$T" checkout -q -- PriceOfBread.SimCore/BilateralMarket.cs
echo done >> "$OUT"
