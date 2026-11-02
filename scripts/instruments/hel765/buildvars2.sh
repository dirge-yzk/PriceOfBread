set -e
W=/d/PoB_work/r765_work; T=$W/tree
for v in orig fix017 mut_north mut_whole; do
  cp $W/svg_$v.svg $T/PriceOfBread.App/Assets/worldmap.svg
  dotnet build $T/PriceOfBread.App/PriceOfBread.App.csproj -c Release --artifacts-path D:/PoB_work/r765_work/art2_$v -v q -nologo 2>&1 | tail -2
done
cp $W/svg_orig.svg $T/PriceOfBread.App/Assets/worldmap.svg
python - <<'PY'
p='D:/PoB_work/r765_work/tree/PriceOfBread.App/Views/MapPane.axaml.cs'
s=open(p,encoding='utf-8-sig').read()
old='raw[iy * Nx + ix] = row[ix * 4 + 2] > row[ix * 4] ? (byte)1 : (byte)0;'
assert s.count(old)==1
s=s.replace(old,'raw[iy * Nx + ix] = row[ix * 4 + 2] > 0 ? (byte)1 : (byte)0;')
open(p,'w',encoding='utf-8-sig').write(s)
PY
dotnet build $T/PriceOfBread.App/PriceOfBread.App.csproj -c Release --artifacts-path D:/PoB_work/r765_work/art2_mut_any -v q -nologo 2>&1 | tail -2
cp $W/mappane_orig.cs $T/PriceOfBread.App/Views/MapPane.axaml.cs
mkdir -p $W/bake
for v in orig fix017 mut_north mut_whole mut_any; do
  A=$W/art2_$v/bin/PriceOfBread.App/release/PriceOfBread.App.exe
  $A --bakemask D:/PoB_work/r765_work/bake/${v}_land.bin land || true
  $A --bakemask D:/PoB_work/r765_work/bake/${v}_sea.bin || true
done
ls -la $W/bake
echo ALLBAKED
