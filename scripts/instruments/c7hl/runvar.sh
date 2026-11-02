#!/usr/bin/env bash
# (776) c7hl：焼いた 2 枚（$W/bake/<変種>_land.bin・_sea.bin）を使い捨ての木の SimCore に置き、Runner（routedump・edgedump765 の動詞つき）を建てて 2 つの表を書く。
#   使い方：bash runvar.sh <変種名> [barriers.json の絶対パス]（barriers を渡すとその変種の barriers.json を木へ置く＝fix=-1 の行の削除の前後の比べ用）
set -u
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W="${C7HL_W:-/d/PoB_work/c7hl}"; T="$W/tree"
V="$1"; BJ="${2:-}"
mkdir -p "$W/out" "$W/logs"
[ -f "$W/land_orig.bin" ] || { cp "$T/PriceOfBread.SimCore/landmask_land.bin" "$W/land_orig.bin"; cp "$T/PriceOfBread.SimCore/landmask.bin" "$W/sea_orig.bin"; cp "$T/PriceOfBread.SimCore/barriers.json" "$W/barriers_orig.json"; }
cp "$W/bake/${V}_land.bin" "$T/PriceOfBread.SimCore/landmask_land.bin"
cp "$W/bake/${V}_sea.bin" "$T/PriceOfBread.SimCore/landmask.bin"
[ -n "$BJ" ] && cp "$BJ" "$T/PriceOfBread.SimCore/barriers.json"
dotnet build "$T/PriceOfBread.Runner/PriceOfBread.Runner.csproj" -c Release --artifacts-path "$W/art_run_$V" -v q -nologo > "$W/logs/build_run_$V.log" 2>&1
RC=$?
cp "$W/land_orig.bin" "$T/PriceOfBread.SimCore/landmask_land.bin"; cp "$W/sea_orig.bin" "$T/PriceOfBread.SimCore/landmask.bin"; cp "$W/barriers_orig.json" "$T/PriceOfBread.SimCore/barriers.json"
if [ $RC -ne 0 ]; then echo "BUILD-FAIL $V"; tail -5 "$W/logs/build_run_$V.log"; exit 1; fi
R="$W/art_run_$V/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe"
"$R" edgedump765 "D:/PoB_work/c7hl/out/edges_$V.tsv"
"$R" routedump "D:/PoB_work/c7hl/out/rd_$V.tsv"
echo "RAN $V"
