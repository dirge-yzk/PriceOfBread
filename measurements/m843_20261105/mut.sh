#!/usr/bin/env bash
# (843) 突然変異：適用→ビルド→試験→復元。出力は mut_<名>.txt
export PATH="/c/Program Files/dotnet:$PATH" PYTHONUTF8=1 PYTHONPATH=D:/PoB_work/c68ui_work
cd /d/PoB_work/wt_c68ui
run() { # 名 ファイル 旧 新 フィルタ
  python - "$2" "$3" "$4" <<'PY'
import sys
p,o,n=sys.argv[1:4]
b=open(p,'rb').read().decode('utf-8')
assert b.count(o)==1,(p,b.count(o))
open(p,'wb').write(b.replace(o,n).encode('utf-8'))
PY
  dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c68ui_art --disable-build-servers > /dev/null 2>&1
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path D:/PoB_work/c68ui_art --filter "$5" > "D:/PoB_work/c68ui_work/mut_$1.txt" 2>&1
  git checkout -- "$2"
}
run M3 PriceOfBread.SimCore/Fiscal.cs 'w.InsolvencyMonths += 1.0 - payRate;' 'w.InsolvencyMonths += 3.0 * (1.0 - payRate);' "FullyQualifiedName~FiscalTests|FullyQualifiedName~TreasuryLedgerTests"
run M2 PriceOfBread.Tests/FullChainTests.cs 'rg.Markets[Catalog.GoldOreId].Price = Catalog.RefPrices[Catalog.GoldOreId] * 0.5;' 'rg.Markets[Catalog.GoldOreId].Price = Catalog.RefPrices[Catalog.GoldOreId] * 2.0;' "FullyQualifiedName~FullChainTests"
run M1 PriceOfBread.SimCore/MarketFlow.cs 'if (closedWage && ServiceClosed(' 'if (false && closedWage && ServiceClosed(' "FullyQualifiedName~WriterRatchetTests"
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c68ui_art --disable-build-servers > /dev/null 2>&1
echo done > D:/PoB_work/c68ui_work/marks/mut.done
