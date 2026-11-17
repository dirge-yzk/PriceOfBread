cd D:/PoB_work/wt_c118hcx
export TEMP=D:/PoB_work/c118hc_work TMP=D:/PoB_work/c118hc_work
T="dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c118hcx_art --filter FullyQualifiedName~TradeInstrumentTests.G4"
$T > D:/PoB_work/c118hc_work/g4_0.log 2>&1
cp PriceOfBread.Tests/TradeInstrumentTests.cs D:/PoB_work/c118hc_work/g4_good.cs
python - <<'PY'
p='PriceOfBread.Tests/TradeInstrumentTests.cs'
b=open(p,'rb').read().decode()
o='var (dir, sim, restore) = Run(24);\n        try\n        {\n            var t = Read(dir, "nationgood"); var ratios'
assert b.count(o)==1
open(p,'wb').write(b.replace(o,o.replace('Run(24)','Run(8)')).encode())
PY
$T > D:/PoB_work/c118hc_work/g4_m5.log 2>&1
cp D:/PoB_work/c118hc_work/g4_good.cs PriceOfBread.Tests/TradeInstrumentTests.cs
python - <<'PY'
p='PriceOfBread.Tests/TradeInstrumentTests.cs'
b=open(p,'rb').read().decode()
o='double q = t.D(r, "crossImportQty"), p = t.D(r, "priceMeanGold")'
assert b.count(o)==1
open(p,'wb').write(b.replace(o,'double q = t.D(r, "crossImportValue"), p = t.D(r, "priceMeanGold")').encode())
PY
$T > D:/PoB_work/c118hc_work/g4_m4.log 2>&1
cp D:/PoB_work/c118hc_work/g4_good.cs PriceOfBread.Tests/TradeInstrumentTests.cs
echo done > D:/PoB_work/c118hc_work/g4m.mark
