cd /d/PoB_work/wt_c145sv
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c145sv_art --filter "FullyQualifiedName~空の配列の共有の形" 2>&1 | grep -E "失敗|合格|error" | head -4
