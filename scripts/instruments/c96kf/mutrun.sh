#!/usr/bin/env bash
# (1024) c96kf 突然変異 M1〜M4 を順に：適用→試験（ClearingFillInstrumentTests）→出力を保存→元へ戻す。使い方：bash mutrun.sh <使い捨ての木> <出力フォルダ> <M1 M2 ...>
T="$1"; O="$2"; shift 2
mkdir -p "$O"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
for M in "$@"; do
  (cd "$T" && git checkout -q -- . && python "$SP/mut.py" "$T" "$M" > "$O/$M.apply.txt" 2>&1)
  (cd "$T" && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c96kf_mutart --disable-build-servers --filter "FullyQualifiedName~ClearingFillInstrumentTests" > "$O/$M.test.txt" 2>&1; echo "rc=$?" >> "$O/$M.test.txt")
  (cd "$T" && git diff > "$O/$M.diff.txt"; git checkout -q -- .)
done
