#!/usr/bin/env bash
# (880) c74id (d) の突然変異：ImportPayerLive＝1 の使い捨ての木で RouteKmTests の盤面を 60→40 へ戻して回す（:185 の前提が赤の見込み）。コミットしない。
#   bash rkmut880.sh <旗 1 の使い捨ての木> <出力>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; O=${2:?出力}; SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$T" || exit 9
git checkout -q -- PriceOfBread.Tests/RouteKmTests.cs
python -I "$SP/../tools/replace1.py" PriceOfBread.Tests/RouteKmTests.cs "for (int q = 0; q < 60; q++)" "for (int q = 0; q < 40; q++)" || exit 8
{ echo "突然変異 RK（木 $(git rev-parse --short HEAD)・ImportPayerLive＝1・盤面 60→40）"; git diff --stat; git diff | grep '^[-+][^-+]'
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c74id_work/art_g1 --disable-build-servers --filter "FullyQualifiedName~RouteKmTests" 2>&1 | grep -E "^\s+失敗 |成功!|失敗!|合計|error CS|エラー メッセージ" -A1 | head -20; } > "$O" 2>&1
git checkout -q -- PriceOfBread.Tests/RouteKmTests.cs
echo RKDONE
