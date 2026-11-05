#!/usr/bin/env bash
# (868) c72tc 突然変異と旗 1 の木：bash mutrun868.sh <コミット> <作業フォルダ> <出力の表> [型...]（既定 F1 M1 M2 M3）
#   型ごとに git archive で写しの木を作り、mut868.py で書き換え、Tests をビルドして対象の試験だけを回す。1 型 1 行（rc・合否の行・赤の試験の名前）。
export PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
H=$1; W=$2; OUT=$3; shift 3; MODES="${*:-F1 M1 M2 M3}"
FILTER="FullyQualifiedName~TradeClearingTests|FullyQualifiedName~TradeClearingBoardTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~CearnInstrumentTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~DormantIdentityTests"
mkdir -p "$W"; : > "$OUT"
for m in $MODES; do
  T="$W/mut_$m"; rm -rf "$T" && mkdir -p "$T"
  (cd "$REPO" && git archive "$H") | tar -x -C "$T" || { echo "$m	ARCHIVEFAIL" >> "$OUT"; continue; }
  python -I "$SP/mut868.py" "$m" "$T" > "$W/mut_$m.edit.txt" 2>&1 || { echo "$m	$(cat "$W/mut_$m.edit.txt")" >> "$OUT"; continue; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_$m" --disable-build-servers > "$W/mut_$m.build.txt" 2>&1 || { echo "$m	BUILDFAIL" >> "$OUT"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$W/art_$m" --filter "$FILTER" > "$W/mut_$m.test.txt" 2>&1
  RC=$?
  RED=$(grep -E '^\s+失敗 PriceOfBread' "$W/mut_$m.test.txt" | sed 's/^\s*失敗 //; s/ \[.*$//' | paste -sd ',')
  echo "$m	rc=$RC	$(grep -E '合計:' "$W/mut_$m.test.txt" | tail -1)	赤の試験：${RED:-なし}" >> "$OUT"
  rm -rf "$W/art_$m"
done
echo "MUTRUNDONE $OUT"
