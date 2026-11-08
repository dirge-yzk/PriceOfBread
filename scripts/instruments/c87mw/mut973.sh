#!/usr/bin/env bash
# (973) c87mw 突然変異の試験の台本：使い捨ての木 <木>（点火の状態）に突然変異を 1 本ずつ当て、この線の試験（MilitaryWarMultContactTests・CivilWarArmsDemandTests・MilitaryTests の双子）を回して試験ごとの合否を <出力>/<M>.txt へ。
#   使い方: bash mut973.sh <使い捨ての木> <出力フォルダ> [M0 M1 ...]（素の木の行も 1 本＝MS に base を入れると突然変異なしで回す）
T="${1:?木}"; O="${2:?出力}"; shift 2; MS="${*:-base M0 M1 M2 M3 M4 M5 M6}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET="/c/Program Files/dotnet/dotnet"
F="FullyQualifiedName~MilitaryWarMultContactTests|FullyQualifiedName~CivilWarArmsDemandTests|FullyQualifiedName~MilitaryTests.交戦中も装備の上限は戦時の倍率で広がらず選んだ段の器のまま"
mkdir -p "$O"
for M in $MS; do
  git -C "$T" checkout -q -- PriceOfBread.SimCore/Simulation.cs PriceOfBread.SimCore/Coefficients.cs PriceOfBread.SimCore/Diplomacy.cs
  if [ "$M" != base ]; then python "$HERE/mut973.py" "$T" "$M" > "$O/$M.txt" 2>&1 || { echo "当てられない" >> "$O/$M.txt"; continue; }; else echo "base（突然変異なし）" > "$O/$M.txt"; fi
  ( cd "$T" && "$DOTNET" test PriceOfBread.Tests -c Release --artifacts-path "${MUT_ART:-D:/PoB_work/c87mw_work/mutart}" --filter "$F" --logger "trx;LogFileName=$M.trx" --results-directory "$(cygpath -w "$O")" ) > "$O/$M.log" 2>&1
  echo "rc=$?" >> "$O/$M.txt"
  grep -o 'testName="[^"]*" [^>]*outcome="[A-Za-z]*"' "$O/$M.trx" 2>/dev/null | sed 's/testName="PriceOfBread.Tests.\([^"]*\)".*outcome="\([A-Za-z]*\)"/\1\t\2/' >> "$O/$M.txt"
done
git -C "$T" checkout -q -- PriceOfBread.SimCore/Simulation.cs PriceOfBread.SimCore/Coefficients.cs PriceOfBread.SimCore/Diplomacy.cs
