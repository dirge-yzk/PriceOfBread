#!/usr/bin/env bash
# (776) c7hl：1 つの SVG の変種から App --bakemask で陸路用・海路用の 2 枚を焼く（使い捨ての木 $T の中で・木は worktree の写し）。
#   使い方：bash bakevar.sh <変種名> <svg の絶対パス> [mut]      mut=any のときは MapPane の陸の判定を〈赤>青〉から〈赤>0〉（少しでも赤なら陸）へ変える突然変異
#   出力：$W/bake/<変種名>_land.bin（陸路用）・<変種名>_sea.bin（海路用）。⚠App は焼いたあと Environment.Exit(0) で終わる。終わらなければ呼び手が PID で止める。
set -u
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W="${C7HL_W:-/d/PoB_work/c7hl}"; T="$W/tree"
V="$1"; SVG="$2"; MUT="${3:-}"
mkdir -p "$W/bake" "$W/logs"
cp "$SVG" "$T/PriceOfBread.App/Assets/worldmap.svg" || exit 1
MP="$T/PriceOfBread.App/Views/MapPane.axaml.cs"
cp "$MP" "$W/mappane_orig.cs"
if [ "$MUT" = "any" ]; then
python - <<'PY'
p='D:/PoB_work/c7hl/tree/PriceOfBread.App/Views/MapPane.axaml.cs'
b=open(p,'rb').read()
old=b'raw[iy * Nx + ix] = row[ix * 4 + 2] > row[ix * 4] ? (byte)1 : (byte)0;'
assert b.count(old)==1
open(p,'wb').write(b.replace(old,b'raw[iy * Nx + ix] = row[ix * 4 + 2] > 0 ? (byte)1 : (byte)0;'))
PY
fi
dotnet build "$T/PriceOfBread.App/PriceOfBread.App.csproj" -c Release --artifacts-path "$W/art_app_$V" -v q -nologo > "$W/logs/build_app_$V.log" 2>&1
RC=$?
cp "$W/mappane_orig.cs" "$MP"
if [ $RC -ne 0 ]; then echo "BUILD-FAIL $V"; tail -5 "$W/logs/build_app_$V.log"; exit 1; fi
APP="$W/art_app_$V/bin/PriceOfBread.App/release/PriceOfBread.App.exe"
rm -f "$W/bake/${V}_land.bin" "$W/bake/${V}_sea.bin"
"$APP" --bakemask "D:/PoB_work/c7hl/bake/${V}_land.bin" land
"$APP" --bakemask "D:/PoB_work/c7hl/bake/${V}_sea.bin"
ls -la "$W/bake/${V}_land.bin" "$W/bake/${V}_sea.bin"
echo "BAKED $V"
