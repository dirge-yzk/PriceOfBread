set -e
W=/d/PoB_work/r765_work; T=$W/tree
for v in orig fix017 mut_north mut_whole mut_any; do
  cp $W/bake/${v}_land.bin $T/PriceOfBread.SimCore/landmask_land.bin
  cp $W/bake/${v}_sea.bin $T/PriceOfBread.SimCore/landmask.bin
  dotnet build $T/PriceOfBread.Runner/PriceOfBread.Runner.csproj -c Release --artifacts-path D:/PoB_work/r765_work/artr_$v -v q -nologo 2>&1 | tail -2
  R=$W/artr_$v/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
  $R edgedump765 D:/PoB_work/r765_work/edges_$v.tsv
  $R routedump D:/PoB_work/r765_work/rd_$v.tsv
done
cp $W/land_orig.bin $T/PriceOfBread.SimCore/landmask_land.bin
cp $W/sea_orig.bin $T/PriceOfBread.SimCore/landmask.bin
echo ALLRUN
