#!/usr/bin/env bash
# (863) c71ip 突然変異の単体の段：bash mutunit863.sh <コミット> <作業フォルダ> <出力の表> [型...]（既定 F1 E1 E2 E3 E4）
#   型ごとに git archive で写しの木を作り、mut863.py で書き換え、Tests をビルドして ImportPayerLiveTests だけを回す。1 型 1 行（rc・合否の行・赤の試験の名前）。
export PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
H=$1; W=$2; OUT=$3; shift 3; MODES="${*:-F1 E1 E2 E3 E4}"
mkdir -p "$W"; : > "$OUT"
for m in $MODES; do
  T="$W/mut_$m"; rm -rf "$T" && mkdir -p "$T"
  (cd "$REPO" && git archive "$H") | tar -x -C "$T" || { echo "$m	ARCHIVEFAIL" >> "$OUT"; continue; }
  python -I "$SP/mut863.py" "$m" "$T" > "$W/mut_$m.edit.txt" 2>&1 || { echo "$m	$(cat "$W/mut_$m.edit.txt")" >> "$OUT"; continue; }
  dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_$m" --disable-build-servers > "$W/mut_$m.build.txt" 2>&1 || { echo "$m	BUILDFAIL" >> "$OUT"; continue; }
  dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$W/art_$m" --filter "FullyQualifiedName~ImportPayerLiveTests" > "$W/mut_$m.test.txt" 2>&1
  RC=$?
  RED=$(grep -E '^\s+失敗 PriceOfBread' "$W/mut_$m.test.txt" | sed 's/^\s*失敗 //; s/ \[.*$//' | paste -sd ',')
  echo "$m	rc=$RC	$(grep -E '合計:' "$W/mut_$m.test.txt" | tail -1)	赤の試験：${RED:-なし}" >> "$OUT"
done
echo "MUTUNITDONE $OUT"
