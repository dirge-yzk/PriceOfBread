cd D:/PoB_work/c104fb_work/mut || exit 1
F='FullyQualifiedName~硫|FullyQualifiedName~タービン|FullyQualifiedName~堆肥|FullyQualifiedName~肥料|FullyQualifiedName~原油|FullyQualifiedName~石油|FullyQualifiedName~造幣|FullyQualifiedName~Mint|FullyQualifiedName~首都|FullyQualifiedName~中心|FullyQualifiedName~Capital|FullyQualifiedName~中核|FullyQualifiedName~民族|FullyQualifiedName~Ethnic|FullyQualifiedName~Secession|FullyQualifiedName~電力|FullyQualifiedName~Geography|FullyQualifiedName~Silver|FullyQualifiedName~銀|FullyQualifiedName~金本位'
echo "=== 変異なし（絞った試験）"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "^ *失敗|成功!|失敗!|error CS" | head -6 | cut -c1-200
for M in M1 M2 M3 M4 M7 M8 M9 M10; do
  python D:/PoB_work/c104fb_work/mut.py $M
  echo "=== $M"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "^ *失敗|成功!|失敗!|error CS" | head -6 | cut -c1-200
done
git checkout -q -- .
