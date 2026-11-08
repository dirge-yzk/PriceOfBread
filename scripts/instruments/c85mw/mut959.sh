#!/usr/bin/env bash
# (959) c85mw 突然変異の試験の台本：使い捨ての木 <木> に突然変異を 1 本ずつ当て、新しい試験 MilitaryWarMultContactTests を回して試験ごとの合否を <出力>/<M>.txt へ。
#   使い方: bash mut959.sh <使い捨ての木（器のコミットの worktree）> <出力フォルダ> [M0 M1 ...]
T="${1:?木}"; O="${2:?出力}"; shift 2; MS="${*:-M0 M1 M2 M3 M4 M5 M6}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET="/c/Program Files/dotnet/dotnet"
mkdir -p "$O"
for M in $MS; do
  git -C "$T" checkout -q -- PriceOfBread.SimCore/Simulation.cs PriceOfBread.SimCore/Coefficients.cs PriceOfBread.SimCore/Diplomacy.cs
  python "$HERE/mut959.py" "$T" "$M" > "$O/$M.txt" 2>&1 || { echo "当てられない" >> "$O/$M.txt"; continue; }
  ( cd "$T" && "$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "${MUT_ART:-D:/PoB_work/c85mw_mutart}" --filter "FullyQualifiedName~MilitaryWarMultContactTests" --logger "trx;LogFileName=$M.trx" --results-directory "$(cygpath -w "$O")" ) > "$O/$M.log" 2>&1
  echo "rc=$?" >> "$O/$M.txt"
  grep -o 'testName="[^"]*" [^>]*outcome="[A-Za-z]*"' "$O/$M.trx" 2>/dev/null | sed 's/testName="PriceOfBread.Tests.MilitaryWarMultContactTests.\([^"]*\)".*outcome="\([A-Za-z]*\)"/\1\t\2/' >> "$O/$M.txt"
done
git -C "$T" checkout -q -- PriceOfBread.SimCore/Simulation.cs PriceOfBread.SimCore/Coefficients.cs PriceOfBread.SimCore/Diplomacy.cs
