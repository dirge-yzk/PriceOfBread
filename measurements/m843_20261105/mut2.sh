#!/usr/bin/env bash
export PATH="/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
cd /d/PoB_work/wt_c68ui
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c68ui_art --disable-build-servers > /dev/null 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path D:/PoB_work/c68ui_art --filter "FullyQualifiedName~FullChainTests" > D:/PoB_work/c68ui_work/mut_M2a.txt 2>&1
python - <<'PY'
p='PriceOfBread.Tests/FullChainTests.cs'
b=open(p,'rb').read().decode('utf-8')
o='        sim.World.MonetaryLaw = MonetaryStandardLaw.Gold;\n'
assert b.count(o)==1
open(p,'wb').write(b.replace(o,'').encode('utf-8'))
PY
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c68ui_art --disable-build-servers > /dev/null 2>&1
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path D:/PoB_work/c68ui_art --filter "FullyQualifiedName~FullChainTests" > D:/PoB_work/c68ui_work/mut_M2b.txt 2>&1
python - <<'PY'
p='PriceOfBread.Tests/FullChainTests.cs'
b=open(p,'rb').read().decode('utf-8')
k='        double base0 = sim.World.BaseMoney.Value;\n        var ex = Record.Exception(() => sim.Run(400));\n        Assert.Null(ex);   // §11.6（保存・境界）が鋳造を含めて'
assert b.count(k)==1
open(p,'wb').write(b.replace(k,'        sim.World.MonetaryLaw = MonetaryStandardLaw.Gold;\n'+k).encode('utf-8'))
PY
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c68ui_art --disable-build-servers > /dev/null 2>&1
