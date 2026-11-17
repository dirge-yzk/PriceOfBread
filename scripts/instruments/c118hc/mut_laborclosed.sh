cd D:/PoB_work/wt_c118hcx
export TEMP=D:/PoB_work/c118hc_work TMP=D:/PoB_work/c118hc_work
F="--filter FullyQualifiedName~LaborMarketFormTests.帯の床の撤去は労働3財"
git checkout PriceOfBread.SimCore/MarketFlow.cs
python - <<'PY'
p='PriceOfBread.SimCore/MarketFlow.cs'
b=open(p,'rb').read(); o='run == 1 && want < wrp && !(mwFloor'.encode(); assert b.count(o)==1
open(p,'wb').write(b.replace(o,'run == 1 && !(mwFloor'.encode()))
PY
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path D:/PoB_work/c118hcx_art $F > D:/PoB_work/c118hc_work/ma.log 2>&1
git checkout PriceOfBread.SimCore/MarketFlow.cs
python - <<'PY'
p='PriceOfBread.SimCore/MarketFlow.cs'
b=open(p,'rb').read(); o='            if (!(pe < wrp)) return false;\n'.encode(); assert b.count(o)==1
open(p,'wb').write(b.replace(o,b''))
PY
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path D:/PoB_work/c118hcx_art $F > D:/PoB_work/c118hc_work/mb.log 2>&1
git checkout PriceOfBread.SimCore/MarketFlow.cs
dotnet test PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj -c Release --artifacts-path D:/PoB_work/c118hcx_art $F > D:/PoB_work/c118hc_work/m0.log 2>&1
echo done > D:/PoB_work/c118hc_work/mabs.mark
