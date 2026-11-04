#!/usr/bin/env bash
# (821) c65md 突然変異の試験の回し（使い捨ての木で 1 本ずつ当て、ForcedPeaceBlankTests と CollapsePeaceTests を回す）。
#   bash mutrun821.sh <使い捨ての木> <作業フォルダ> M1 M2 ...   出力 <作業フォルダ>/mut_<M>.txt と要約の行
export PATH="/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
T=${1:?木}; W=${2:?作業フォルダ}; shift 2
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
for M in "$@"; do
  (cd "$T" && git checkout -q -- PriceOfBread.SimCore/Diplomacy.cs) || exit 1
  python -I -X utf8 "$HERE/mutate821.py" "$T" "$M" || { echo "$M 置き換え失敗"; continue; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_mut" --disable-build-servers > "$W/mut_${M}_build.txt" 2>&1 || { echo "$M BUILDFAIL"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$W/art_mut" --filter "FullyQualifiedName~ForcedPeaceBlankTests|FullyQualifiedName~CollapsePeaceTests" > "$W/mut_${M}.txt" 2>&1
  echo "$M $(grep -h '合計:' "$W/mut_${M}.txt" | tail -1)"
  grep -h '^  失敗 ' "$W/mut_${M}.txt" | sed "s/^/  $M /"
done
(cd "$T" && git checkout -q -- PriceOfBread.SimCore/Diplomacy.cs)
echo MUTDONE
