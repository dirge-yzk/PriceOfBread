#!/usr/bin/env bash
# (803) c12lw 突然変異の実行：使い捨ての木（元の木の写し・1 本）に M1..M6 を 1 本ずつ当てて関係する試験の組を回し、毎回元のファイルへ戻す。出力＝<作業>/<M>.txt
# 使い方: bash mut_run.sh <元の木> <作業フォルダ> [M1 M2 ...]
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH"
SRC="$1"; W="$2"; shift 2
MS="${@:-M1 M2 M3 M4 M5 M6}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
declare -A FILE=( [M1]=PriceOfBread.SimCore/PolicyPreferences.cs [M2]=PriceOfBread.SimCore/PolicyPreferences.cs [M3]=PriceOfBread.SimCore/PolicyPreferences.cs [M4]=PriceOfBread.SimCore/Government.cs [M5]=PriceOfBread.SimCore/Politics.cs [M6]=PriceOfBread.SimCore/Politics.cs )
declare -A FILTER=( [M1]=Rs3LordReverseGainTests [M2]=Rs2LordOppositionTests [M3]=Rs3LordReverseGainTests [M4]=MandateWallStockCapTests [M5]=RevoltIgnitionTests [M6]=RevoltIgnitionTests )
mkdir -p "$W"; T="$W/tree"; rm -rf "$T"; mkdir -p "$T"
for d in PriceOfBread.SimCore PriceOfBread.App PriceOfBread.Runner PriceOfBread.Tests; do
  mkdir -p "$T/$d"; (cd "$SRC/$d" && tar cf - --exclude=bin --exclude=obj .) | (cd "$T/$d" && tar xf -)
done
cp "$SRC"/*.slnx "$SRC"/*.json "$T"/ 2>/dev/null
for M in $MS; do
  cp "$SRC/${FILE[$M]}" "$T/${FILE[$M]}"
  python "$HERE/mut_apply.py" "$T" "$M" > "$W/$M.txt" 2>&1 || { echo "適用失敗" >> "$W/$M.txt"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art" --disable-build-servers --filter "FullyQualifiedName~${FILTER[$M]}" >> "$W/$M.txt" 2>&1
  echo "rc=$? M=$M 試験の組=${FILTER[$M]}" >> "$W/$M.txt"
  cp "$SRC/${FILE[$M]}" "$T/${FILE[$M]}"
done
